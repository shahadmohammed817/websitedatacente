using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    public interface IOpenRequestImportService
    {
        /// <summary>
        /// يقرأ ملف "الطلبات المفتوحة" عبر IExcelDashboardService.AnalyzeFile - رأس الأعمدة
        /// بالصف 1 (عادي). يستبدل كل الطلبات القديمة بالكامل عند إعادة الرفع.
        /// </summary>
        Task<(bool Success, string Message, int RowsImported)> ImportAsync(Stream fileStream);
    }

    public class OpenRequestImportService : IOpenRequestImportService
    {
        private readonly AppDbContext _db;
        private readonly IExcelDashboardService _excelService;

        public OpenRequestImportService(AppDbContext db, IExcelDashboardService excelService)
        {
            _db = db;
            _excelService = excelService;
        }

        // أسماء الأعمدة الحقيقية بالملف المعتمد بالضبط
        private const string ColStage = "المرحلة";
        private const string ColRequestNumber = "رقم الطلب";
        private const string ColRequestCreatedDate = "تاريخ إنشاء الطلب";
        private const string ColCommitteeNumber = "رقم اللجنة";
        private const string ColAmanah = "الأمانة";
        private const string ColBaladiyah = "البلدية";
        private const string ColDaysSinceCreation = "عدد الايام من تاريخ الإنشاء";
        private const string ColDaysSinceUpdate = "عدد الأيام من تاريخ التحديث";
        private const string ColLastUpdateDate = "تاريخ اخر التحديث";
        private const string ColSla = "SLA";
        private const string ColBasketName = "أسم السلة";
        private const string ColRequestStatus = "حالة الطلب";
        private const string ColOpportunityNumber = "رقم الفرصة";
        private const string ColOpportunityName = "اسم الفرصة";
        private const string ColEmployeeId = "هوية الموظف";
        private const string ColEmployeeName = "اسم الموظف";
        private const string ColMobileNumber = "رقم الجوال";
        private const string ColEmployeeEmail = "إيميل الموظف";
        private const string ColContractValue = "قيمة العقد";

        public async Task<(bool Success, string Message, int RowsImported)> ImportAsync(Stream fileStream)
        {
            AnalyzeResult analyzed;
            try
            {
                analyzed = _excelService.AnalyzeFile(fileStream);
            }
            catch (Exception ex)
            {
                return (false, "تعذّرت قراءة الملف: " + ex.Message, 0);
            }

            if (!analyzed.Success || analyzed.Rows.Count == 0)
                return (false, "لم يتم العثور على أي بيانات صالحة بالملف.", 0);

            var allColumnNames = analyzed.Columns.Select(c => c.Name).ToList();
            string? Resolve(string exactName) =>
                allColumnNames.FirstOrDefault(c => DataImportService.Normalize(c) == DataImportService.Normalize(exactName));

            var stageCol = Resolve(ColStage);
            var requestNumberCol = Resolve(ColRequestNumber);
            if (stageCol == null && requestNumberCol == null)
            {
                return (false,
                    "لم يتم العثور على أعمدة \"المرحلة\"/\"رقم الطلب\" بالملف - تأكدي أن تخطيط الأعمدة لم يتغيّر.",
                    0);
            }

            var requestCreatedDateCol = Resolve(ColRequestCreatedDate);
            var committeeNumberCol = Resolve(ColCommitteeNumber);
            var amanahCol = Resolve(ColAmanah);
            var baladiyahCol = Resolve(ColBaladiyah);
            var daysSinceCreationCol = Resolve(ColDaysSinceCreation);
            var daysSinceUpdateCol = Resolve(ColDaysSinceUpdate);
            var lastUpdateDateCol = Resolve(ColLastUpdateDate);
            var slaCol = Resolve(ColSla);
            var basketNameCol = Resolve(ColBasketName);
            var requestStatusCol = Resolve(ColRequestStatus);
            var opportunityNumberCol = Resolve(ColOpportunityNumber);
            var opportunityNameCol = Resolve(ColOpportunityName);
            var employeeIdCol = Resolve(ColEmployeeId);
            var employeeNameCol = Resolve(ColEmployeeName);
            var mobileNumberCol = Resolve(ColMobileNumber);
            var employeeEmailCol = Resolve(ColEmployeeEmail);
            var contractValueCol = Resolve(ColContractValue);

            // عمود "اسم الفرصة" غالبًا نتيجة VLOOKUP بالملف الأصلي - يظهر "#N/A" لأي طلب لم
            // يُطابَق بعد بدل قيمة فارغة، فنعامله كعدم وجود اسم فرصة أصلاً
            static string? CleanOpportunityName(string? raw)
            {
                var trimmed = raw?.Trim();
                return string.IsNullOrWhiteSpace(trimmed) || trimmed == "#N/A" ? null : trimmed;
            }

            var now = DateTime.UtcNow;
            var list = new List<OpenRequestRecord>();
            foreach (var row in analyzed.Rows)
            {
                list.Add(new OpenRequestRecord
                {
                    Stage = stageCol != null ? DataImportService.GetString(row, stageCol)?.Trim() : null,
                    RequestNumber = requestNumberCol != null ? DataImportService.GetString(row, requestNumberCol)?.Trim() : null,
                    RequestCreatedDate = requestCreatedDateCol != null ? DataImportService.GetDate(row, requestCreatedDateCol) : null,
                    CommitteeNumber = committeeNumberCol != null ? DataImportService.GetString(row, committeeNumberCol)?.Trim() : null,
                    Amanah = amanahCol != null ? DataImportService.GetString(row, amanahCol)?.Trim() : null,
                    Baladiyah = baladiyahCol != null ? DataImportService.GetString(row, baladiyahCol)?.Trim() : null,
                    DaysSinceCreation = daysSinceCreationCol != null ? (int?)DataImportService.GetDecimal(row, daysSinceCreationCol) : null,
                    DaysSinceUpdate = daysSinceUpdateCol != null ? (int?)DataImportService.GetDecimal(row, daysSinceUpdateCol) : null,
                    LastUpdateDate = lastUpdateDateCol != null ? DataImportService.GetDate(row, lastUpdateDateCol) : null,
                    Sla = slaCol != null ? DataImportService.GetString(row, slaCol)?.Trim() : null,
                    BasketName = basketNameCol != null ? DataImportService.GetString(row, basketNameCol)?.Trim() : null,
                    RequestStatus = requestStatusCol != null ? DataImportService.GetString(row, requestStatusCol)?.Trim() : null,
                    OpportunityNumber = opportunityNumberCol != null ? DataImportService.GetString(row, opportunityNumberCol)?.Trim() : null,
                    OpportunityName = opportunityNameCol != null ? CleanOpportunityName(DataImportService.GetString(row, opportunityNameCol)) : null,
                    EmployeeId = employeeIdCol != null ? DataImportService.GetString(row, employeeIdCol)?.Trim() : null,
                    EmployeeName = employeeNameCol != null ? DataImportService.GetString(row, employeeNameCol)?.Trim() : null,
                    MobileNumber = mobileNumberCol != null ? DataImportService.GetString(row, mobileNumberCol)?.Trim() : null,
                    EmployeeEmail = employeeEmailCol != null ? DataImportService.GetString(row, employeeEmailCol)?.Trim() : null,
                    ContractValue = contractValueCol != null ? DataImportService.GetDecimal(row, contractValueCol) : null,
                    ImportedAt = now
                });
            }

            await _db.OpenRequestRecords.ExecuteDeleteAsync();
            await _db.OpenRequestRecords.AddRangeAsync(list);
            await _db.SaveChangesAsync();

            return (true, $"تم استيراد {list.Count} طلب بنجاح.", list.Count);
        }
    }
}
