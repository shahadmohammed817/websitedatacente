using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    public interface IInvestmentContractImportService
    {
        /// <summary>
        /// يقرأ ملف "تقرير عقود" (صفحة "جميع العقود الاستثمارية") عبر IExcelDashboardService.AnalyzeFile
        /// - رأس الأعمدة بالصف 1 (عادي). يستبدل كل العقود القديمة بالكامل عند إعادة الرفع.
        /// </summary>
        Task<(bool Success, string Message, int RowsImported)> ImportAsync(Stream fileStream);
    }

    public class InvestmentContractImportService : IInvestmentContractImportService
    {
        private readonly AppDbContext _db;
        private readonly IExcelDashboardService _excelService;

        public InvestmentContractImportService(AppDbContext db, IExcelDashboardService excelService)
        {
            _db = db;
            _excelService = excelService;
        }

        // أسماء الأعمدة الحقيقية بالملف المعتمد بالضبط (تأكّدت منها بفحص مباشر للملف)
        private const string ColAmanah = "الأمانة";
        private const string ColBaladiyah = "البلدية";
        private const string ColOpportunityNumber = "رقم الفرصة";
        private const string ColOpportunityName = "اسم الفرصة";
        private const string ColInvestorName = "اسم المستثمر";
        private const string ColContractNumber = "رقم العقد";
        private const string ColContractType = "نوع العقد";
        private const string ColContractStatus = "حالة العقد";
        private const string ColContractDurationDays = "مدة العقد (أيام)";
        private const string ColAnnualValue = "القيمة السنوية للعقد";
        private const string ColAnnualValueWithTax = "القيمة السنوية للعقد شاملة ضريبة القيمة المضافة";
        private const string ColHasAnnualIncrease = "هل يوجد زيادة سنوية";
        private const string ColContractStartDate = "تاريخ بداية العقد";
        private const string ColContractEndDate = "تاريخ نهاية العقد";
        private const string ColContractValidityStatus = "حالة سريان العقد";
        private const string ColTotalPaymentsCount = "عدد الدفعات الكلي";
        private const string ColDuePaymentsCount = "عدد الدفعات المستحقة";
        private const string ColDuePaymentsValue = "قيمة الدُفعات المستحقة";
        private const string ColDuePaidCount = "عدد الدفعات المستحقة المدفوعة";
        private const string ColDuePaidValue = "قيمة الدفعات المستحقة المدفوعة";
        private const string ColDueUnpaidCount = "عدد الدفعات المستحقة غير المدفوعة";
        private const string ColDueUnpaidValue = "قيمة الدفعات المستحقة غير المدفوعة";
        private const string ColRemainingNotDueCount = "عدد الدفعات المتبقية غير المستحقة";
        private const string ColRemainingNotDueValue = "قيمة الدفعات المتبقية غير المستحقة";
        private const string ColStalledCount = "المدفوعات المتعثرة";
        private const string ColStalledValue = "قيمة المدفوعات المتعثرة";
        private const string ColSentToExecutionCount = "عدد الدفعات اللي تم ارسالها الى تنفيذ";
        private const string ColSentToExecutionValue = "قيمة الدفعات المرسلة الى تنفيذ";
        private const string ColGeneralDepartment = "الإدارة العامة";
        private const string ColPortfolio = "المحفظة";

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
            // بعض الأعمدة (خصوصًا "الإدارة العامة") قد يظهر بها حرف زائد بسبب خلل بأداة قراءة
            // العناوين المشتركة (لوحظ سابقًا بملف المطالبات: "الاإدارة العامة" بدل "الإدارة العامة")
            // - نطابقها بجزء ثابت غير متأثر بهذا الخلل كحل احتياطي
            string? ResolveContains(string substring) =>
                allColumnNames.FirstOrDefault(c => DataImportService.Normalize(c).Contains(DataImportService.Normalize(substring)));

            var contractNumberCol = Resolve(ColContractNumber);
            if (contractNumberCol == null)
            {
                return (false,
                    "لم يتم العثور على عمود \"رقم العقد\" بالملف - تأكدي أن تخطيط الأعمدة لم يتغيّر.",
                    0);
            }

            var amanahCol = Resolve(ColAmanah);
            var baladiyahCol = Resolve(ColBaladiyah);
            var opportunityNumberCol = Resolve(ColOpportunityNumber);
            var opportunityNameCol = Resolve(ColOpportunityName);
            var investorNameCol = Resolve(ColInvestorName) ?? Resolve("إسم المستثمر") ?? Resolve("المستثمر");
            var contractTypeCol = Resolve(ColContractType);
            var contractStatusCol = Resolve(ColContractStatus);
            var contractDurationDaysCol = Resolve(ColContractDurationDays);
            var annualValueCol = Resolve(ColAnnualValue);
            var annualValueWithTaxCol = Resolve(ColAnnualValueWithTax);
            var hasAnnualIncreaseCol = Resolve(ColHasAnnualIncrease);
            var contractStartDateCol = Resolve(ColContractStartDate);
            var contractEndDateCol = Resolve(ColContractEndDate);
            var contractValidityStatusCol = Resolve(ColContractValidityStatus);
            var totalPaymentsCountCol = Resolve(ColTotalPaymentsCount);
            var duePaymentsCountCol = Resolve(ColDuePaymentsCount);
            var duePaymentsValueCol = Resolve(ColDuePaymentsValue) ?? ResolveContains("قيمة الدفعات المستحقة");
            var duePaidCountCol = Resolve(ColDuePaidCount);
            var duePaidValueCol = Resolve(ColDuePaidValue);
            var dueUnpaidCountCol = Resolve(ColDueUnpaidCount);
            var dueUnpaidValueCol = Resolve(ColDueUnpaidValue);
            var remainingNotDueCountCol = Resolve(ColRemainingNotDueCount);
            var remainingNotDueValueCol = Resolve(ColRemainingNotDueValue);
            var stalledCountCol = Resolve(ColStalledCount);
            var stalledValueCol = Resolve(ColStalledValue);
            var sentToExecutionCountCol = Resolve(ColSentToExecutionCount);
            var sentToExecutionValueCol = Resolve(ColSentToExecutionValue);
            var generalDepartmentCol = Resolve(ColGeneralDepartment) ?? ResolveContains("دارة العامة");
            var portfolioCol = Resolve(ColPortfolio);

            var now = DateTime.UtcNow;
            var list = new List<InvestmentContractRecord>();
            foreach (var row in analyzed.Rows)
            {
                var contractNumber = DataImportService.GetString(row, contractNumberCol)?.Trim();
                if (string.IsNullOrWhiteSpace(contractNumber)) continue; // صف بلا رقم عقد = غير صالح

                list.Add(new InvestmentContractRecord
                {
                    Amanah = amanahCol != null ? DataImportService.GetString(row, amanahCol)?.Trim() : null,
                    Baladiyah = baladiyahCol != null ? DataImportService.GetString(row, baladiyahCol)?.Trim() : null,
                    OpportunityNumber = opportunityNumberCol != null ? DataImportService.GetString(row, opportunityNumberCol)?.Trim() : null,
                    OpportunityName = opportunityNameCol != null ? DataImportService.GetString(row, opportunityNameCol)?.Trim() : null,
                    InvestorName = investorNameCol != null ? DataImportService.GetString(row, investorNameCol)?.Trim() : null,
                    ContractNumber = contractNumber,
                    ContractType = contractTypeCol != null ? DataImportService.GetString(row, contractTypeCol)?.Trim() : null,
                    ContractStatus = contractStatusCol != null ? DataImportService.GetString(row, contractStatusCol)?.Trim() : null,
                    ContractDurationDays = contractDurationDaysCol != null ? (int?)DataImportService.GetDecimal(row, contractDurationDaysCol) : null,
                    AnnualValue = annualValueCol != null ? DataImportService.GetDecimal(row, annualValueCol) : null,
                    AnnualValueWithTax = annualValueWithTaxCol != null ? DataImportService.GetDecimal(row, annualValueWithTaxCol) : null,
                    HasAnnualIncrease = hasAnnualIncreaseCol != null ? DataImportService.GetString(row, hasAnnualIncreaseCol)?.Trim() : null,
                    ContractStartDate = contractStartDateCol != null ? DataImportService.GetDate(row, contractStartDateCol) : null,
                    ContractEndDate = contractEndDateCol != null ? DataImportService.GetDate(row, contractEndDateCol) : null,
                    ContractValidityStatus = contractValidityStatusCol != null ? DataImportService.GetString(row, contractValidityStatusCol)?.Trim() : null,
                    TotalPaymentsCount = totalPaymentsCountCol != null ? (int?)DataImportService.GetDecimal(row, totalPaymentsCountCol) : null,
                    DuePaymentsCount = duePaymentsCountCol != null ? (int?)DataImportService.GetDecimal(row, duePaymentsCountCol) : null,
                    DuePaymentsValue = duePaymentsValueCol != null ? DataImportService.GetDecimal(row, duePaymentsValueCol) : null,
                    DuePaidCount = duePaidCountCol != null ? (int?)DataImportService.GetDecimal(row, duePaidCountCol) : null,
                    DuePaidValue = duePaidValueCol != null ? DataImportService.GetDecimal(row, duePaidValueCol) : null,
                    DueUnpaidCount = dueUnpaidCountCol != null ? (int?)DataImportService.GetDecimal(row, dueUnpaidCountCol) : null,
                    DueUnpaidValue = dueUnpaidValueCol != null ? DataImportService.GetDecimal(row, dueUnpaidValueCol) : null,
                    RemainingNotDueCount = remainingNotDueCountCol != null ? (int?)DataImportService.GetDecimal(row, remainingNotDueCountCol) : null,
                    RemainingNotDueValue = remainingNotDueValueCol != null ? DataImportService.GetDecimal(row, remainingNotDueValueCol) : null,
                    StalledCount = stalledCountCol != null ? (int?)DataImportService.GetDecimal(row, stalledCountCol) : null,
                    StalledValue = stalledValueCol != null ? DataImportService.GetDecimal(row, stalledValueCol) : null,
                    SentToExecutionCount = sentToExecutionCountCol != null ? (int?)DataImportService.GetDecimal(row, sentToExecutionCountCol) : null,
                    SentToExecutionValue = sentToExecutionValueCol != null ? DataImportService.GetDecimal(row, sentToExecutionValueCol) : null,
                    GeneralDepartment = generalDepartmentCol != null ? DataImportService.GetString(row, generalDepartmentCol)?.Trim() : null,
                    Portfolio = portfolioCol != null ? DataImportService.GetString(row, portfolioCol)?.Trim() : null,
                    ImportedAt = now
                });
            }

            if (list.Count == 0)
                return (false, "لم يتم العثور على أي عقود صالحة (عمود \"رقم العقد\" فارغ بكل الصفوف).", 0);

            await _db.InvestmentContractRecords.ExecuteDeleteAsync();
            await _db.InvestmentContractRecords.AddRangeAsync(list);
            await _db.SaveChangesAsync();

            return (true, $"تم استيراد {list.Count} عقد بنجاح.", list.Count);
        }
    }
}
