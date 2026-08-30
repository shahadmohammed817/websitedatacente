using ClosedXML.Excel;
using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    public interface IInvestmentActivityImportService
    {
        /// <summary>
        /// يقرأ ملف "الأنشطة الاستثمارية" مباشرة عبر ClosedXML (بدل IExcelDashboardService.
        /// AnalyzeFile - غير مناسب هنا: يقرأ أول ورقة فقط بينما بياناتنا بالورقة الثالثة "الانشطة "،
        /// وآلية تطبيعه الداخلية تُسقِط قيم الأعمدة مختلطة الأنواع بصمت). رأس الأعمدة بالصف 4،
        /// البيانات تبدأ الصف 5. يستبدل كل السجلات القديمة بالكامل عند إعادة الرفع.
        /// </summary>
        Task<(bool Success, string Message, int RowsImported)> ImportAsync(Stream fileStream);
    }

    public class InvestmentActivityImportService : IInvestmentActivityImportService
    {
        private readonly AppDbContext _db;

        public InvestmentActivityImportService(AppDbContext db)
        {
            _db = db;
        }

        private const string SheetName = "الانشطة";
        private const int HeaderRow = 4;

        // أرقام الأعمدة الثابتة بورقة "الانشطة " (تأكّدت منها بفحص مباشر للملف)
        private const int ColOpportunityNumber = 2;
        private const int ColOpportunityName = 3;
        private const int ColContractNumber = 8;
        private const int ColContractDate = 9;
        private const int ColContractYear = 11;
        private const int ColAnnualRent = 12;
        private const int ColActivity = 20;
        private const int ColSector = 22;
        private const int ColRegion = 24;
        private const int ColContractType = 26;
        private const int ColX = 27;
        private const int ColY = 28;

        public async Task<(bool Success, string Message, int RowsImported)> ImportAsync(Stream fileStream)
        {
            IXLWorksheet? worksheet;
            int lastRow;
            try
            {
                using var workbook = new XLWorkbook(fileStream);
                worksheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name.Trim() == SheetName);
                if (worksheet == null)
                {
                    return (false, $"لم يتم العثور على ورقة \"{SheetName}\" بالملف.", 0);
                }

                lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                if (lastRow <= HeaderRow)
                {
                    return (false, "لم يتم العثور على أي بيانات صالحة بالملف.", 0);
                }

                var now = DateTime.UtcNow;
                var list = new List<InvestmentActivityRecord>();

                for (int r = HeaderRow + 1; r <= lastRow; r++)
                {
                    var row = worksheet.Row(r);
                    var contractNumber = row.Cell(ColContractNumber).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(contractNumber)) continue; // صف بلا رقم عقد = غير صالح

                    list.Add(new InvestmentActivityRecord
                    {
                        OpportunityNumber = NullIfEmpty(row.Cell(ColOpportunityNumber).GetString()),
                        OpportunityName = NullIfEmpty(row.Cell(ColOpportunityName).GetString()),
                        ContractNumber = contractNumber,
                        ContractDate = ReadDate(row.Cell(ColContractDate)),
                        ContractYear = ReadInt(row.Cell(ColContractYear)),
                        AnnualRent = ReadDecimal(row.Cell(ColAnnualRent)),
                        Activity = NullIfEmpty(row.Cell(ColActivity).GetString()),
                        Sector = NullIfEmpty(row.Cell(ColSector).GetString()),
                        Region = NullIfZero(row.Cell(ColRegion).GetString()),
                        ContractType = NullIfEmpty(row.Cell(ColContractType).GetString()),
                        X = ReadDecimal(row.Cell(ColX)),
                        Y = ReadDecimal(row.Cell(ColY)),
                        ImportedAt = now
                    });
                }

                if (list.Count == 0)
                    return (false, "لم يتم العثور على أي أنشطة صالحة (عمود \"رقم العقد\" فارغ بكل الصفوف).", 0);

                await _db.InvestmentActivityRecords.ExecuteDeleteAsync();
                await _db.InvestmentActivityRecords.AddRangeAsync(list);
                await _db.SaveChangesAsync();

                return (true, $"تم استيراد {list.Count} نشاط بنجاح.", list.Count);
            }
            catch (Exception ex)
            {
                return (false, "تعذّرت قراءة الملف: " + ex.Message, 0);
            }
        }

        private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        // بعض صفوف عمود "المنطقة" فارغة فعليًا بالملف لكن تُقرَأ كـ"0" (تنسيق خلية رقمي على خلية
        // فارغة) - نتعامل معها كقيمة فارغة حتى لا تظهر كمنطقة وهمية خامسة زائدة عن الـ5 الحقيقية
        private static string? NullIfZero(string s)
        {
            var trimmed = NullIfEmpty(s);
            return trimmed == "0" ? null : trimmed;
        }

        private static DateTime? ReadDate(IXLCell cell)
        {
            if (cell.DataType == XLDataType.DateTime)
            {
                try { return cell.GetDateTime(); } catch { return null; }
            }
            return null;
        }

        private static int? ReadInt(IXLCell cell) =>
            cell.TryGetValue(out double d) ? (int)d : null;

        private static decimal? ReadDecimal(IXLCell cell) =>
            cell.TryGetValue(out double d) ? (decimal)d : null;
    }
}
