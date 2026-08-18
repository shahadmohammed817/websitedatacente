using ClosedXML.Excel;
using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    public interface IFinanceTargetImportService
    {
        /// <summary>
        /// يقرأ ملف "المستهدف السنوي": ورقة واحدة بكتلتين ("نوع الإيراد  الأمانة" ثم "نوع
        /// الإيراد  البلديات")، كل كتلة بعمود "البند الرئيسي" (يتكرر آخر قيمة غير فارغة لعدة
        /// أسطر فرعية)، عمود "البند الفرعي" (اختياري)، وعمودَي قيمة جنب بعض: مستهدف وزارة
        /// البلديات والإسكان السنوي ثم مستهدف وزارة المالية السنوي. يستخرج كلا المصدرين معًا
        /// من نفس الملف، ويستبدل كل البيانات القديمة لكلا المصدرين بالكامل عند إعادة الرفع.
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
                    "كتلتَي \"نوع الإيراد  الأمانة\" و\"نوع الإيراد  البلديات\" بنفس تخطيط الأعمدة المعتاد.",
                    0);
            }

            // الملف الواحد يغذّي كلا المصدرين معًا، فإعادة الرفع تستبدل كليهما دفعة واحدة
            var existing = await _db.FinanceItemTargets.ToListAsync();
            if (existing.Count > 0) _db.FinanceItemTargets.RemoveRange(existing);

            await _db.FinanceItemTargets.AddRangeAsync(rows);
            await _db.SaveChangesAsync();

            var financeAmanah = rows.Where(r => r.TargetSource == "Finance" && r.Scope == "Amanah").Sum(r => r.AnnualTarget);
            var financeBaladiyah = rows.Where(r => r.TargetSource == "Finance" && r.Scope == "Baladiyah").Sum(r => r.AnnualTarget);
            var housingAmanah = rows.Where(r => r.TargetSource == "BaladiyahHousing" && r.Scope == "Amanah").Sum(r => r.AnnualTarget);
            var housingBaladiyah = rows.Where(r => r.TargetSource == "BaladiyahHousing" && r.Scope == "Baladiyah").Sum(r => r.AnnualTarget);
            return (true,
                $"تم استيراد المستهدف السنوي بنجاح: {rows.Count} صف. " +
                $"وزارة المالية (الأمانة: {financeAmanah:N0} ﷼، البلديات: {financeBaladiyah:N0} ﷼) - " +
                $"وزارة البلديات والإسكان (الأمانة: {housingAmanah:N0} ﷼، البلديات: {housingBaladiyah:N0} ﷼).",
                rows.Count);
        }

        private static readonly HashSet<string> TotalLabels = new() { "الإجمالي" };

        private enum BlockScope { None, Amanah, Baladiyah }

        /// <summary>
        /// عمود C = مستهدف وزارة البلديات والإسكان (TargetSource="BaladiyahHousing")، عمود D =
        /// مستهدف وزارة المالية (TargetSource="Finance") - كلاهما بنفس البند الرئيسي/الفرعي
        /// بالضبط لأنهما من نفس صفوف نفس الملف، فيبقى ربط "تصنيف البند" متطابقًا للمصدرين.
        /// </summary>
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
                var housingCell = row.Cell(3);
                var financeCell = row.Cell(4);
                bool bEmpty = string.IsNullOrEmpty(b);

                // صف عنوان كتلة نطاق جديد ("نوع الإيراد  الأمانة" / "نوع الإيراد  البلديات")
                if (a.StartsWith("نوع الإيراد"))
                {
                    if (a.Contains("الأمانة")) { scope = BlockScope.Amanah; currentMainItem = null; }
                    else if (a.Contains("البلديات")) { scope = BlockScope.Baladiyah; currentMainItem = null; }
                    continue;
                }

                // صف فارغ فاصل بين الكتل/البنود الفرعية
                if (string.IsNullOrEmpty(a) && bEmpty && housingCell.IsEmpty() && financeCell.IsEmpty()) continue;

                // صف إجمالي الكتلة أو الإجمالي الكلي (أمانة + بلديات) - قيم مشتقة، تُتجاهل
                if (TotalLabels.Contains(a) || a.Contains("أمانة و بلديات")) continue;

                if (scope == BlockScope.None) continue;

                if (!string.IsNullOrEmpty(a)) currentMainItem = a;
                if (string.IsNullOrEmpty(currentMainItem)) continue;

                var scopeValue = scope == BlockScope.Amanah ? "Amanah" : "Baladiyah";
                var subItem = bEmpty ? null : b;

                var housingTarget = IsNumericCell(housingCell) ? housingCell.GetValue<decimal>() : 0m;
                result.Add(new FinanceItemTarget
                {
                    Scope = scopeValue, TargetSource = "BaladiyahHousing",
                    MainItem = currentMainItem, SubItem = subItem,
                    AnnualTarget = housingTarget, UploadedAt = now
                });

                var financeTarget = IsNumericCell(financeCell) ? financeCell.GetValue<decimal>() : 0m;
                result.Add(new FinanceItemTarget
                {
                    Scope = scopeValue, TargetSource = "Finance",
                    MainItem = currentMainItem, SubItem = subItem,
                    AnnualTarget = financeTarget, UploadedAt = now
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
