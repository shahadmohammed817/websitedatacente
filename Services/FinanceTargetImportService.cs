using ClosedXML.Excel;
using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    public interface IFinanceTargetImportService
    {
        /// <summary>
        /// يقرأ ملف "مستهدف المالية": ورقة واحدة بكتلتين فقط - "الأمانة" ثم "البلديات التابعة"
        /// (مجموع كل البلديات كنطاق واحد، بلا تفصيل لكل بلدية على حدة). كل كتلة: عمود "البند
        /// الرئيسي" (قد يتكرر آخر قيمة غير فارغة لعدة أسطر فرعية)، عمود "البند الفرعي" (اختياري)،
        /// وعمود "الإجمالي" = المستهدف السنوي (مستهدف سنوي فقط، بلا تفصيل شهري). يستبدل كل
        /// البيانات القديمة بالكامل عند إعادة الرفع.
        /// </summary>
        Task<(bool Success, string Message, int RowsImported)> ImportAsync(Stream fileStream);
    }

    public class FinanceTargetImportService : IFinanceTargetImportService
    {
        private readonly AppDbContext _db;

        public FinanceTargetImportService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<(bool Success, string Message, int RowsImported)> ImportAsync(Stream fileStream)
        {
            List<FinanceItemTarget> rows;
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
                    "كتلتَي \"الأمانة\" و\"البلديات التابعة\" بنفس تخطيط الأعمدة المعتاد.",
                    0);
            }

            var existing = await _db.FinanceItemTargets.ToListAsync();
            if (existing.Count > 0) _db.FinanceItemTargets.RemoveRange(existing);

            await _db.FinanceItemTargets.AddRangeAsync(rows);
            await _db.SaveChangesAsync();

            var amanahTotal = rows.Where(r => r.Scope == "Amanah").Sum(r => r.AnnualTarget);
            var baladiyahTotal = rows.Where(r => r.Scope == "Baladiyah").Sum(r => r.AnnualTarget);
            return (true,
                $"تم استيراد مستهدف وزارة المالية بنجاح: {rows.Count} صف (الأمانة: " +
                $"{amanahTotal:N0} ﷼، البلديات: {baladiyahTotal:N0} ﷼).",
                rows.Count);
        }

        private static readonly HashSet<string> TotalLabels = new() { "الإجمالي" };

        private enum BlockScope { None, Amanah, Baladiyah }

        private static List<FinanceItemTarget> ParseWorkbook(Stream fileStream)
        {
            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null) throw new InvalidOperationException("الملف لا يحتوي على أي ورقة عمل.");

            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            var result = new List<FinanceItemTarget>();

            var scope = BlockScope.None;
            string? currentMainItem = null;
            var now = DateTime.UtcNow;

            for (int r = 1; r <= lastRow; r++)
            {
                var row = worksheet.Row(r);
                var a = row.Cell(1).GetString().Trim();
                var b = row.Cell(2).GetString().Trim();
                var cCell = row.Cell(3);
                bool bEmpty = string.IsNullOrEmpty(b);
                bool cEmpty = cCell.IsEmpty();

                // صف عنوان كتلة نطاق جديد: عمود "أ" فقط، بلا بند فرعي ولا قيمة
                if (bEmpty && cEmpty)
                {
                    if (a == "الأمانة") { scope = BlockScope.Amanah; currentMainItem = null; continue; }
                    if (a == "البلديات التابعة") { scope = BlockScope.Baladiyah; currentMainItem = null; continue; }
                    if (string.IsNullOrEmpty(a)) continue; // صف فارغ فاصل بين الكتلتين
                }

                // صف عناوين الأعمدة ("نوع الإيراد ...")
                if (a.StartsWith("نوع الإيراد")) continue;

                // صف إجمالي الكتلة أو الإجمالي الكلي (أمانة + بلديات) - قيم مشتقة، تُتجاهل
                if (TotalLabels.Contains(a) || a.Contains("أمانة و بلديات")) continue;

                if (scope == BlockScope.None) continue;

                if (!string.IsNullOrEmpty(a)) currentMainItem = a;
                if (string.IsNullOrEmpty(currentMainItem)) continue;

                var target = IsNumericCell(cCell) ? cCell.GetValue<decimal>() : 0m;

                result.Add(new FinanceItemTarget
                {
                    Scope = scope == BlockScope.Amanah ? "Amanah" : "Baladiyah",
                    MainItem = currentMainItem,
                    SubItem = bEmpty ? null : b,
                    AnnualTarget = target,
                    UploadedAt = now
                });
            }

            return result;
        }

        private static bool IsNumericCell(IXLCell cell)
        {
            if (cell.IsEmpty()) return false;
            return cell.DataType == XLDataType.Number || decimal.TryParse(cell.GetString(), out _);
        }
    }
}
