using ClosedXML.Excel;
using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    public interface IMonthlyTargetImportService
    {
        /// <summary>
        /// يقرأ ملف "المستهدف الشهري للبلديات والأمانة": ورقة واحدة تحوي 3 أنواع كتل:
        /// (1) 17 كتلة بلدية - اسم البلدية بصف مستقل، يليه صف عناوين "البند الرئيسي/البند/
        /// 1..12/الاجمالي/المحصل"، ثم صفوف البنود (الأشهر بالأعمدة C-N).
        /// (2) جدولا إجمالي سنوي فقط بلا تفصيل شهري ("الأمانة"/"البلديات") - تُتجاهَل بالكامل
        /// لأن قيمها مشتقة حسابيًا وتتطابق مع مجموع كتلة (3) أدناه.
        /// (3) كتلة "التوزيع الشهري للأمانة" - نفس فكرة كتلة البلدية لكن بعمود واحد للبند
        /// (بلا "بند رئيسي" منفصل) والأشهر بالأعمدة B-M والإجمالي بعمود N.
        /// يستبدل كل البيانات القديمة بالكامل بالجديدة (بلا تراكم عند إعادة الرفع).
        /// </summary>
        Task<(bool Success, string Message, int RowsImported, int EntitiesFound)> ImportAsync(Stream fileStream);
    }

    public class MonthlyTargetImportService : IMonthlyTargetImportService
    {
        private readonly AppDbContext _db;

        public MonthlyTargetImportService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<(bool Success, string Message, int RowsImported, int EntitiesFound)> ImportAsync(Stream fileStream)
        {
            List<MonthlyBaladiyahTarget> rows;
            try
            {
                rows = ParseWorkbook(fileStream);
            }
            catch (Exception ex)
            {
                return (false, "تعذّرت قراءة الملف: " + ex.Message, 0, 0);
            }

            if (rows.Count == 0)
            {
                return (false,
                    "لم يتم العثور على أي بيانات صالحة بالملف. تأكد أن الورقة الأولى تحتوي على " +
                    "كتل البلديات (اسم البلدية بصف مستقل يليه صف عناوين وأرقام الأشهر)، وكتلة " +
                    "\"التوزيع الشهري للأمانة\".",
                    0, 0);
            }

            // التحقق: مجموع الأشهر الاثني عشر لكل صف يجب أن يطابق عمود "الاجمالي" بالملف
            // (بفارق تقريبي بسيط للتقريب العشري) - إن اختل التطابق نرفض الاستيراد بدل حفظ
            // بيانات قد تكون قُرئت من أعمدة خاطئة.
            var badRows = rows.Where(r =>
                Math.Abs((r.M1 + r.M2 + r.M3 + r.M4 + r.M5 + r.M6 + r.M7 + r.M8 + r.M9 + r.M10 + r.M11 + r.M12) - r.AnnualTotal) > 1m)
                .ToList();
            if (badRows.Count > 0)
            {
                return (false,
                    $"فشل التحقق من صحة البيانات: {badRows.Count} صف/صفوف لا يطابق مجموع أشهرها عمود " +
                    "\"الاجمالي\" بالملف. تأكد أن ترتيب الأعمدة لم يتغيّر.",
                    0, 0);
            }

            // استبدال كامل: حذف كل المستهدفات الشهرية القديمة قبل حفظ الملف الجديد
            var existing = await _db.MonthlyBaladiyahTargets.ToListAsync();
            if (existing.Count > 0) _db.MonthlyBaladiyahTargets.RemoveRange(existing);

            await _db.MonthlyBaladiyahTargets.AddRangeAsync(rows);
            await _db.SaveChangesAsync();

            var entitiesFound = rows.Select(r => r.Entity).Distinct().Count();
            return (true, $"تم استيراد المستهدف الشهري بنجاح: {rows.Count} صف لـ {entitiesFound} جهة.", rows.Count, entitiesFound);
        }

        // عناوين وصفوف إجمالي معروفة داخل كتلة مُتعرَّف عليها بالفعل (بلدية أو الأمانة) - تُتجاهل
        private static readonly HashSet<string> HeaderOrTotalLabels = new()
        {
            "البند الرئيسي", "البند", "البند / الشهر",
            "الإجمالي الربعي", "الإجمالي الشهري", "الإجمالي الكلي", "الإجمالي"
        };

        // أسماء 4 بلديات مكتوبة بصيغة مختلفة قليلًا بين ملف المستهدفات وقاعدة البيانات (المصدر
        // الفعلي لأسماء الجهات هو عمود "الجهة" بملف الإيرادات Book112.xlsx) - نوحّدها هنا وقت
        // الاستيراد حتى تبقى مطابقة المستهدف بالمفوتر لاحقًا مطابقة نصية مباشرة بلا أي تعقيد إضافي
        private static readonly Dictionary<string, string> EntityAliases = new()
        {
            ["بلدية محافظة ينبع"] = "بلدية ينبع",
            ["بلدية وادي الفرع"] = "بلدية وادى الفرع",
            ["بلدية المسيجيد"] = "بلدية المسيجيد والقاحة",
            ["بلدية سلسلة جهينة"] = "بلدية سليلة جهينة والمربع",
        };

        /// <summary>
        /// كتلة الأمانة الشهرية تكتب عنوانها بصيغة "التوزيع الشهري للأمانة" - نستخرج منها
        /// الاسم الصافي "الأمانة" (وأي كتلة مشابهة مستقبلاً بنفس الصيغة "التوزيع الشهري لـ...").
        /// </summary>
        private static string NormalizeEntityName(string raw)
        {
            var t = raw.Trim();
            const string marker = "التوزيع الشهري";
            var idx = t.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0) return t;

            t = t.Substring(idx + marker.Length).Trim();
            if (t.StartsWith("لل")) t = "ال" + t.Substring(2);
            else if (t.StartsWith("ل") && t.Length > 1) t = t.Substring(1);
            return t.Trim();
        }

        private enum BlockMode { None, Skip, Municipality, AmanahMonthly }

        private static List<MonthlyBaladiyahTarget> ParseWorkbook(Stream fileStream)
        {
            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null) throw new InvalidOperationException("الملف لا يحتوي على أي ورقة عمل.");

            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            var result = new List<MonthlyBaladiyahTarget>();

            // (بند فرعي مُطبَّع) ← (بند رئيسي) - يُبنى من كتل البلديات أثناء نفس المرور، ثم
            // يُستخدَم لتعبئة "البند الرئيسي" لصفوف الأمانة (التي تأتي لاحقًا بالملف وتفتقر
            // لعمود "بند رئيسي" منفصل، لكنها تستخدم نفس مفردات البنود الفرعية بالضبط)
            var subToMain = new Dictionary<string, string>();

            string? currentEntity = null;
            string? currentMainItem = null;
            var mode = BlockMode.None;
            var now = DateTime.UtcNow;

            for (int r = 1; r <= lastRow; r++)
            {
                var row = worksheet.Row(r);
                var a = row.Cell(1).GetString().Trim();
                var b = row.Cell(2).GetString().Trim();

                // صف عنوان كتلة جديدة: عمود "أ" فيه نص وكل الأعمدة من "ب" فأكثر فارغة تمامًا
                bool restEmpty = row.Cells(2, 16).All(c => c.IsEmpty());
                if (restEmpty && !string.IsNullOrEmpty(a))
                {
                    if (a.StartsWith("بلدية"))
                    {
                        currentEntity = EntityAliases.TryGetValue(a, out var alias) ? alias : a;
                        currentMainItem = null;
                        mode = BlockMode.Municipality;
                        continue;
                    }
                    if (a.Contains("التوزيع الشهري"))
                    {
                        currentEntity = NormalizeEntityName(a);
                        currentMainItem = null;
                        mode = BlockMode.AmanahMonthly;
                        continue;
                    }
                    // أي عنوان كتلة آخر (مثل "الأمانة"/"البلديات": جدولا إجمالي سنوي بلا تفصيل
                    // شهري - قيمها مشتقة ومطابقة لمجموع كتلة "التوزيع الشهري للأمانة" أدناه)
                    mode = BlockMode.Skip;
                    currentEntity = null;
                    continue;
                }

                if (mode == BlockMode.Skip || mode == BlockMode.None) continue;

                // صف عناوين الأعمدة أو صف إجمالي الكتلة - تجاهله
                if (HeaderOrTotalLabels.Contains(a) || HeaderOrTotalLabels.Contains(b)) continue;

                if (mode == BlockMode.Municipality)
                {
                    // تخطيط الأعمدة: أ=البند الرئيسي (اختياري، يتكرر آخر قيمة غير فارغة) | ب=البند
                    // | ج..ن (3-14)=الأشهر 1-12 | س(15)=الاجمالي | ع(16)=المحصل
                    if (string.IsNullOrEmpty(currentEntity) || string.IsNullOrEmpty(b)) continue;
                    if (!string.IsNullOrEmpty(a)) currentMainItem = a;
                    if (string.IsNullOrEmpty(currentMainItem)) continue;

                    var months = ReadMonths(row, 3);
                    var total = ReadDecimalOrDefault(row.Cell(15), months.Sum());
                    decimal? collected = IsNumericCell(row.Cell(16)) ? row.Cell(16).GetValue<decimal>() : null;

                    subToMain[NormalizeForLookup(b)] = currentMainItem;

                    result.Add(new MonthlyBaladiyahTarget
                    {
                        Entity = currentEntity,
                        MainItem = currentMainItem,
                        RevenueType = b,
                        M1 = months[0], M2 = months[1], M3 = months[2], M4 = months[3],
                        M5 = months[4], M6 = months[5], M7 = months[6], M8 = months[7],
                        M9 = months[8], M10 = months[9], M11 = months[10], M12 = months[11],
                        AnnualTotal = total,
                        Collected2025 = collected,
                        UploadedAt = now
                    });
                }
                else if (mode == BlockMode.AmanahMonthly)
                {
                    // تخطيط الأعمدة (مختلف عن كتل البلديات): أ=البند مباشرة | ب..م (2-13)=الأشهر
                    // 1-12 | ن(14)=الإجمالي الكلي - بلا عمود "بند رئيسي" منفصل ولا عمود "المحصل"
                    if (string.IsNullOrEmpty(currentEntity) || string.IsNullOrEmpty(a)) continue;

                    var months = ReadMonths(row, 2);
                    var total = ReadDecimalOrDefault(row.Cell(14), months.Sum());
                    var mainItem = subToMain.TryGetValue(NormalizeForLookup(a), out var mapped) ? mapped : a;

                    result.Add(new MonthlyBaladiyahTarget
                    {
                        Entity = currentEntity,
                        MainItem = mainItem,
                        RevenueType = a,
                        M1 = months[0], M2 = months[1], M3 = months[2], M4 = months[3],
                        M5 = months[4], M6 = months[5], M7 = months[6], M8 = months[7],
                        M9 = months[8], M10 = months[9], M11 = months[10], M12 = months[11],
                        AnnualTotal = total,
                        Collected2025 = null,
                        UploadedAt = now
                    });
                }
            }

            return result;
        }

        private static decimal[] ReadMonths(IXLRow row, int firstCol)
        {
            var months = new decimal[12];
            for (int m = 0; m < 12; m++)
            {
                var cell = row.Cell(firstCol + m);
                months[m] = IsNumericCell(cell) ? cell.GetValue<decimal>() : 0m;
            }
            return months;
        }

        private static decimal ReadDecimalOrDefault(IXLCell cell, decimal fallback) =>
            IsNumericCell(cell) ? cell.GetValue<decimal>() : fallback;

        private static bool IsNumericCell(IXLCell cell)
        {
            if (cell.IsEmpty()) return false;
            return cell.DataType == XLDataType.Number || decimal.TryParse(cell.GetString(), out _);
        }

        // تطبيع بسيط لمطابقة أسماء البنود الفرعية بين كتل البلديات وكتلة الأمانة (فراغات زائدة فقط،
        // الأسماء متطابقة حرفيًا بين الكتلتين حسب فحص الملف الفعلي)
        private static string NormalizeForLookup(string value) =>
            System.Text.RegularExpressions.Regex.Replace(value.Trim(), @"\s+", " ");
    }
}
