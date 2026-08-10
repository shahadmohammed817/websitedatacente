using ClosedXML.Excel;
using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    public interface IContractImportService
    {
        /// <summary>
        /// يقرأ ورقة "الطرح" من ملف عقود "الطرح" (عناوين الأعمدة بالصف 5، البيانات من الصف 6).
        /// يستبدل كل العقود القديمة بالكامل عند إعادة الرفع (بلا تراكم).
        /// </summary>
        Task<(bool Success, string Message, int RowsImported)> ImportAsync(Stream fileStream);
    }

    public class ContractImportService : IContractImportService
    {
        private readonly AppDbContext _db;

        public ContractImportService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<(bool Success, string Message, int RowsImported)> ImportAsync(Stream fileStream)
        {
            List<ContractRecord> rows;
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
                    "لم يتم العثور على أي بيانات صالحة بالملف. تأكد أن الملف يحتوي على ورقة باسم \"الطرح\" " +
                    "بنفس تخطيط الأعمدة المعتاد (عناوين الأعمدة بالصف الخامس).",
                    0);
            }

            var existing = await _db.ContractRecords.ToListAsync();
            if (existing.Count > 0) _db.ContractRecords.RemoveRange(existing);

            await _db.ContractRecords.AddRangeAsync(rows);
            await _db.SaveChangesAsync();

            return (true, $"تم استيراد بيانات الطرح بنجاح: {rows.Count} عقد.", rows.Count);
        }

        // قيم نصية بالملف تعني عمليًا "بلا قيمة" (خانة فارغة معروضة كصفر، أو خطأ صيغة إكسل، أو
        // شرطة بديلة لرقم العقد بالعقود التي لم تُبرَم بعد) - "--" تحديدًا هي الشكل الفعلي
        // لعمود "رقم العقد" بكل الصفوف التي حالتها ليست "تم إبرام العقد"
        private static readonly HashSet<string> PlaceholderEmptyValues = new() { "0", "#N/A", "-", "--" };

        private static string? NormalizeText(string raw)
        {
            var t = raw.Trim();
            if (string.IsNullOrEmpty(t) || PlaceholderEmptyValues.Contains(t)) return null;
            return t;
        }

        // "لم يتقدم أحد" و"تم إلغاء المنافسة" حالتان منفصلتان بالملف لكنهما تعنيان عمليًا نفس
        // الشيء (المنافسة لم تُكمَل) - تُدمَجان بحالة واحدة بطلب المستخدمة
        private static readonly HashSet<string> MergedIntoCancelled = new() { "لم يتقدم أحد", "تم إلغاء المنافسة" };
        private const string CancelledStatus = "إلغاء المنافسة";

        private static string? NormalizeStatus(string raw)
        {
            var t = NormalizeText(raw);
            if (t == null) return null;
            return MergedIntoCancelled.Contains(t) ? CancelledStatus : t;
        }

        private static List<ContractRecord> ParseWorkbook(Stream fileStream)
        {
            using var workbook = new XLWorkbook(fileStream);
            if (!workbook.Worksheets.TryGetWorksheet("الطرح", out var worksheet))
                throw new InvalidOperationException("لم يتم العثور على ورقة باسم \"الطرح\" بالملف.");

            const int headerRow = 5;
            const int firstDataRow = 6;

            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

            var colByHeader = new Dictionary<string, int>();
            for (int c = 1; c <= lastCol; c++)
            {
                var h = worksheet.Cell(headerRow, c).GetString().Trim();
                if (!string.IsNullOrEmpty(h) && !colByHeader.ContainsKey(h)) colByHeader[h] = c;
            }

            int Col(string name) => colByHeader.TryGetValue(name, out var c) ? c : 0;

            var colStatus = Col("الحالة");
            var colOppNumber = Col("رقم الفرصة");
            var colOppName = Col("اسم الفرصة");
            var colContractNumber = Col("رقم العقد");
            var colContractDate = Col("تاريخ العقد");
            var colContractYear = Col("سنة العقد");
            var colEnvelope = Col("فتح المظاريف");
            var colAnnualRent = Col("الأجرة السنوية");
            var colContractValue = Col("قيمة العقد");
            var colGeneralDept = Col("الإدارة العامة");
            var colActivity = Col("الأنشطة");
            var colSubPortfolio = Col("المحفظة الفرعية");
            var colContractType = Col("نوع العقد");
            var colGracePeriod = Col("فترة السماح");

            if (colContractType == 0 || colStatus == 0)
                throw new InvalidOperationException("لم يتم العثور على عمودَي \"نوع العقد\" و\"الحالة\" بالملف.");

            var result = new List<ContractRecord>();
            var now = DateTime.UtcNow;

            for (int r = firstDataRow; r <= lastRow; r++)
            {
                var row = worksheet.Row(r);

                // صف بلا رقم فرصة ولا رقم عقد ولا نوع عقد يُعتبر صفًا فارغًا (فاصل/نهاية بيانات)
                var oppNumber = colOppNumber > 0 ? row.Cell(colOppNumber).GetString().Trim() : "";
                var contractNumber = colContractNumber > 0 ? row.Cell(colContractNumber).GetString().Trim() : "";
                var contractType = colContractType > 0 ? NormalizeText(row.Cell(colContractType).GetString()) : null;
                if (string.IsNullOrEmpty(oppNumber) && string.IsNullOrEmpty(contractNumber) && contractType == null)
                    continue;

                DateTime? contractDate = null;
                if (colContractDate > 0)
                {
                    var cell = row.Cell(colContractDate);
                    if (!cell.IsEmpty() && cell.TryGetValue<DateTime>(out var dt)) contractDate = dt;
                }

                int? contractYear = null;
                if (colContractYear > 0)
                {
                    var cell = row.Cell(colContractYear);
                    if (!cell.IsEmpty() && cell.TryGetValue<int>(out var y)) contractYear = y;
                }

                int? quarter = contractDate.HasValue ? ((contractDate.Value.Month - 1) / 3) + 1 : null;

                DateTime? envelopeDate = null;
                if (colEnvelope > 0)
                {
                    var cell = row.Cell(colEnvelope);
                    if (!cell.IsEmpty() && cell.TryGetValue<DateTime>(out var ed)) envelopeDate = ed;
                }

                decimal? annualRent = null;
                if (colAnnualRent > 0)
                {
                    var cell = row.Cell(colAnnualRent);
                    if (!cell.IsEmpty() && cell.TryGetValue<decimal>(out var v)) annualRent = v;
                }

                decimal? contractValue = null;
                if (colContractValue > 0)
                {
                    var cell = row.Cell(colContractValue);
                    if (!cell.IsEmpty() && cell.TryGetValue<decimal>(out var v)) contractValue = v;
                }

                result.Add(new ContractRecord
                {
                    OpportunityNumber = string.IsNullOrEmpty(oppNumber) ? null : oppNumber,
                    OpportunityName = colOppName > 0 ? NormalizeText(row.Cell(colOppName).GetString()) : null,
                    Status = NormalizeStatus(row.Cell(colStatus).GetString()),
                    ContractNumber = NormalizeText(contractNumber),
                    ContractDate = contractDate,
                    ContractYear = contractYear,
                    Quarter = quarter,
                    EnvelopeOpeningDate = envelopeDate,
                    AnnualRent = annualRent,
                    ContractValue = contractValue,
                    GeneralDepartment = colGeneralDept > 0 ? NormalizeText(row.Cell(colGeneralDept).GetString()) : null,
                    Activity = colActivity > 0 ? NormalizeText(row.Cell(colActivity).GetString()) : null,
                    SubPortfolio = colSubPortfolio > 0 ? NormalizeText(row.Cell(colSubPortfolio).GetString()) : null,
                    ContractType = contractType,
                    GracePeriod = colGracePeriod > 0 ? NormalizeText(row.Cell(colGracePeriod).GetString()) : null,
                    ImportedAt = now
                });
            }

            return result;
        }
    }
}
