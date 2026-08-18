using ClosedXML.Excel;
using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    public interface IRevenueBaselineImportService
    {
        /// <summary>
        /// يقرأ ملف "المحصل الفعلي لسنة الأساس 2025" (جدول تفصيلي بعمود "تصنيف البند" وعمود
        /// "القيمة" لكل معاملة محصَّلة)، يجمّع القيمة حسب تصنيف البند، ويستبدل كل بيانات سنة
        /// الأساس القديمة بالكامل عند إعادة الرفع. مصدر منفصل تمامًا عن DashboardRecords -
        /// يُستخدم فقط لبطاقتَي "محصل 2025"/"نسبة نمو الإيرادات" برسم "أداء الإيرادات" بالرئيسية.
        /// </summary>
        Task<(bool Success, string Message, int ClassificationsImported)> ImportAsync(Stream fileStream);
    }

    public class RevenueBaselineImportService : IRevenueBaselineImportService
    {
        private readonly AppDbContext _db;

        public RevenueBaselineImportService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<(bool Success, string Message, int ClassificationsImported)> ImportAsync(Stream fileStream)
        {
            List<RevenueBaselineCollectedItem> rows;
            try
            {
                rows = ParseWorkbook(fileStream);
            }
            catch (Exception ex)
            {
                return (false, "تعذّرت قراءة الملف: " + ex.Message, 0);
            }

            if (rows.Count == 0)
            {
                return (false,
                    "لم يتم العثور على أي بيانات صالحة بالملف. تأكد أن الورقة الأولى تحتوي على " +
                    "عمود \"تصنيف البند\" وعمود \"القيمة\".",
                    0);
            }

            var existing = await _db.RevenueBaselineCollectedItems.ToListAsync();
            if (existing.Count > 0) _db.RevenueBaselineCollectedItems.RemoveRange(existing);

            await _db.RevenueBaselineCollectedItems.AddRangeAsync(rows);
            await _db.SaveChangesAsync();

            var total = rows.Sum(r => r.CollectedValue);
            return (true,
                $"تم استيراد بيانات المحصل لسنة الأساس بنجاح: {rows.Count} تصنيف بإجمالي {total:N0} ﷼.",
                rows.Count);
        }

        // مواقع الأعمدة ثابتة حسب تخطيط الملف المعتمد (جدول Excel باسم "الجدول2"):
        // 3 = تصنيف البند، 7 = القيمة، 9 = تاريخ الدفع
        private const int ClassificationColIndex = 3;
        private const int ValueColIndex = 7;
        private const int PaymentDateColIndex = 9;

        // تُقرَأ فقط معاملات سنة الأساس نفسها (2025) من عمود "تاريخ الدفع" - الملف قد يحوي
        // صفوفًا من سنوات أخرى، ونحتاج شهر كل معاملة (لا مجموع سنوي واحد فقط) حتى تقدر بطاقة
        // "محصل 2025" بالرئيسية تُقيَّد لاحقًا بنفس عدد الأشهر المتوفرة فعليًا بالسنة الحالية
        // (انظر AnalyticsService.GetOverviewDataAsync) - بطلب صريح من المستخدمة حتى تبقى
        // المقارنة بين السنتين عادلة (نفس عدد الأشهر لكلتيهما لا سنة كاملة مقابل سنة جزئية)
        private const int BaselineYear = 2025;

        private static List<RevenueBaselineCollectedItem> ParseWorkbook(Stream fileStream)
        {
            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null) throw new InvalidOperationException("الملف لا يحتوي على أي ورقة عمل.");

            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            var sums = new Dictionary<(string Classification, int Month), decimal>();

            for (int r = 2; r <= lastRow; r++)
            {
                var row = worksheet.Row(r);
                var rawClassification = row.Cell(ClassificationColIndex).GetString().Trim();
                if (string.IsNullOrEmpty(rawClassification)) continue;

                var valueCell = row.Cell(ValueColIndex);
                if (valueCell.IsEmpty()) continue;
                if (!decimal.TryParse(valueCell.GetString(), out var value)) continue;

                var dateCell = row.Cell(PaymentDateColIndex);
                if (dateCell.IsEmpty() || !dateCell.TryGetValue<DateTime>(out var paymentDate)) continue;
                if (paymentDate.Year != BaselineYear) continue;

                var classification = ResolveCanonicalClassification(rawClassification);
                var key = (classification, paymentDate.Month);
                sums[key] = sums.GetValueOrDefault(key) + value;
            }

            var now = DateTime.UtcNow;
            return sums.Select(kv => new RevenueBaselineCollectedItem
            {
                Classification = kv.Key.Classification,
                Month = kv.Key.Month,
                CollectedValue = kv.Value,
                UploadedAt = now
            }).ToList();
        }

        // نص "تصنيف البند" بهذا الملف قد يختلف شكليًا عن نفس التصنيف بجدول DashboardRecords
        // (مسافات حول الشرطة، حروف همزة، أو دمج بادئة "استثمار - " أمام "تأجير مؤقت") رغم أنه
        // نفس البند فعليًا - نوحّد النص عند الاستيراد لنفس النص الرسمي المستخدَم بالجدول الرئيسي
        // حتى تُطابَق مباشرة (تطابق نصي بسيط) عند حساب بطاقتَي "محصل 2025"/"نسبة نمو الإيرادات"
        // بحسب البند المختار بالفلاتر، بلا أي منطق تقريبي وقت العرض
        private static string AliasKey(string s) => DataImportService.Normalize(s).Replace(" ", "").Replace("-", "");

        private static readonly Dictionary<string, string> ClassificationAliasToCanonical = BuildAliasMap();

        private static Dictionary<string, string> BuildAliasMap()
        {
            var canonical = new[] { "استثمار", "تأجير مؤقت", "رسوم", "جزاءات", "رسوم المبيعات-الايواء", "رسوم المبيعات-التبغ", "إيرادات مختلفة" };
            var map = new Dictionary<string, string>();
            foreach (var c in canonical) map[AliasKey(c)] = c;
            map[AliasKey("استثمار - تأجير مؤقت")] = "تأجير مؤقت";
            return map;
        }

        private static string ResolveCanonicalClassification(string raw) =>
            ClassificationAliasToCanonical.TryGetValue(AliasKey(raw), out var canonical) ? canonical : raw;
    }
}
