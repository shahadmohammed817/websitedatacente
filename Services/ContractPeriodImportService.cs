using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    public interface IContractPeriodImportService
    {
        /// <summary>
        /// يقرأ ملف "العقود" (صفحة "العقود التي تنتهي فترة سماحها خلال 3 أشهر") عبر
        /// IExcelDashboardService.AnalyzeFile - رأس الأعمدة بالصف 1 (عادي). يستبدل كل العقود
        /// القديمة بالكامل عند إعادة الرفع.
        /// </summary>
        Task<(bool Success, string Message, int RowsImported)> ImportAsync(Stream fileStream);
    }

    public class ContractPeriodImportService : IContractPeriodImportService
    {
        private readonly AppDbContext _db;
        private readonly IExcelDashboardService _excelService;

        public ContractPeriodImportService(AppDbContext db, IExcelDashboardService excelService)
        {
            _db = db;
            _excelService = excelService;
        }

        // أسماء الأعمدة الحقيقية بالملف المعتمد بالضبط (تأكّدت منها بفحص مباشر للملف)
        private const string ColOpportunityNumber = "رقم الفرصة";
        private const string ColOpportunityName = "اسم الفرصة";
        private const string ColOpportunityDescription = "وصف الفرصة";
        private const string ColContractNumber = "رقم العقد";
        private const string ColPaperContractNumber = "رقم العقد الورقي";
        private const string ColContractType = "نوع العقد";
        private const string ColContractSource = "مصدر العقد";
        private const string ColContractStatus = "حالة العقد";
        private const string ColContractDurationYears = "مدة العقد";
        private const string ColContractEndDate = "تاريخ نهاية العقد";
        // العمود يظهر بأسماء مختلفة قليلاً بين نسخ الملف (أحيانًا "تاريخ تسليم الموقع" وحدها،
        // وأحيانًا بلاحقة إضافية مثل "تاريخ تسليم الموقع تقرير التسليم") - نطابقه بالاحتواء
        // (Contains) بدل التطابق الحرفي فقط، ليبقى يُقرأ صحيحًا مهما اختلفت اللاحقة
        private const string ColSiteDeliveryDate = "تاريخ تسليم الموقع";
        private const string ColSigningDate = "تاريخ التوقيع";
        private const string ColContractValidityStatus = "حالة سريان العقد";
        private const string ColPrepPeriodPercent = "نسبة فترة التجهيز و الانشاء";
        private const string ColPrepPeriodDurationDays = "مدة فترة التجهيز بالأيام";
        private const string ColPrepPeriodEndDate = "نهاية فترة التجهيز";
        private const string ColPaymentDueStartDate = "تاريخ بداية استحقاق الدفعة";
        private const string ColFirstPaymentAfterPrepDate = "تاريخ أول دفعة بعد فترة التجهيز";
        private const string ColPrepPeriodStatus = "حالة العقد (خارج فترة التجهيز - داخل فترة الجهيز - متعثر )";
        private const string ColGeneralDepartment = "الادارة العامة";
        private const string ColPortfolio = "الادارة الفرعية";

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
            string? ResolveContains(string substring) =>
                allColumnNames.FirstOrDefault(c => DataImportService.Normalize(c).Contains(DataImportService.Normalize(substring)));

            var contractNumberCol = Resolve(ColContractNumber);
            if (contractNumberCol == null)
            {
                return (false,
                    "لم يتم العثور على عمود \"رقم العقد\" بالملف - تأكدي أن تخطيط الأعمدة لم يتغيّر.",
                    0);
            }

            var opportunityNumberCol = Resolve(ColOpportunityNumber);
            var opportunityNameCol = Resolve(ColOpportunityName);
            var opportunityDescriptionCol = Resolve(ColOpportunityDescription);
            var paperContractNumberCol = Resolve(ColPaperContractNumber);
            var contractTypeCol = Resolve(ColContractType);
            var contractSourceCol = Resolve(ColContractSource);
            var contractStatusCol = Resolve(ColContractStatus);
            var contractDurationYearsCol = Resolve(ColContractDurationYears);
            var contractEndDateCol = Resolve(ColContractEndDate);
            var siteDeliveryDateCol = Resolve(ColSiteDeliveryDate) ?? ResolveContains(ColSiteDeliveryDate);
            var signingDateCol = Resolve(ColSigningDate);
            var contractValidityStatusCol = Resolve(ColContractValidityStatus);
            var prepPeriodPercentCol = Resolve(ColPrepPeriodPercent);
            var prepPeriodDurationDaysCol = Resolve(ColPrepPeriodDurationDays);
            var prepPeriodEndDateCol = Resolve(ColPrepPeriodEndDate);
            var paymentDueStartDateCol = Resolve(ColPaymentDueStartDate);
            var firstPaymentAfterPrepDateCol = Resolve(ColFirstPaymentAfterPrepDate);
            var prepPeriodStatusCol = Resolve(ColPrepPeriodStatus);
            var generalDepartmentCol = Resolve(ColGeneralDepartment);
            var portfolioCol = Resolve(ColPortfolio);

            var now = DateTime.UtcNow;
            var list = new List<ContractPeriodRecord>();
            foreach (var row in analyzed.Rows)
            {
                var contractNumber = DataImportService.GetString(row, contractNumberCol)?.Trim();
                if (string.IsNullOrWhiteSpace(contractNumber)) continue; // صف فارغ/غير صالح

                int? durationYears = null;
                if (contractDurationYearsCol != null)
                {
                    var d = DataImportService.GetDecimal(row, contractDurationYearsCol);
                    if (d.HasValue) durationYears = (int)Math.Round(d.Value);
                }

                int? prepDurationDays = null;
                if (prepPeriodDurationDaysCol != null)
                {
                    var d = DataImportService.GetDecimal(row, prepPeriodDurationDaysCol);
                    if (d.HasValue) prepDurationDays = (int)Math.Round(d.Value);
                }

                list.Add(new ContractPeriodRecord
                {
                    OpportunityNumber = opportunityNumberCol != null ? DataImportService.GetString(row, opportunityNumberCol)?.Trim() : null,
                    OpportunityName = opportunityNameCol != null ? DataImportService.GetString(row, opportunityNameCol)?.Trim() : null,
                    OpportunityDescription = opportunityDescriptionCol != null ? DataImportService.GetString(row, opportunityDescriptionCol)?.Trim() : null,
                    ContractNumber = contractNumber,
                    PaperContractNumber = paperContractNumberCol != null ? DataImportService.GetString(row, paperContractNumberCol)?.Trim() : null,
                    ContractType = contractTypeCol != null ? DataImportService.GetString(row, contractTypeCol)?.Trim() : null,
                    ContractSource = contractSourceCol != null ? DataImportService.GetString(row, contractSourceCol)?.Trim() : null,
                    ContractStatus = contractStatusCol != null ? DataImportService.GetString(row, contractStatusCol)?.Trim() : null,
                    ContractDurationYears = durationYears,
                    ContractEndDate = contractEndDateCol != null ? DataImportService.GetDate(row, contractEndDateCol) : null,
                    SiteDeliveryDate = siteDeliveryDateCol != null ? DataImportService.GetDate(row, siteDeliveryDateCol) : null,
                    ContractValidityStatus = contractValidityStatusCol != null ? DataImportService.GetString(row, contractValidityStatusCol)?.Trim() : null,
                    PrepPeriodPercent = prepPeriodPercentCol != null ? DataImportService.GetDecimal(row, prepPeriodPercentCol) : null,
                    PrepPeriodDurationDays = prepDurationDays,
                    PrepPeriodEndDate = prepPeriodEndDateCol != null ? DataImportService.GetDate(row, prepPeriodEndDateCol) : null,
                    PaymentDueStartDate = paymentDueStartDateCol != null ? DataImportService.GetDate(row, paymentDueStartDateCol) : null,
                    FirstPaymentAfterPrepDate = firstPaymentAfterPrepDateCol != null ? DataImportService.GetDate(row, firstPaymentAfterPrepDateCol) : null,
                    PrepPeriodStatus = prepPeriodStatusCol != null ? DataImportService.GetString(row, prepPeriodStatusCol)?.Trim() : null,
                    GeneralDepartment = generalDepartmentCol != null ? DataImportService.GetString(row, generalDepartmentCol)?.Trim() : null,
                    Portfolio = portfolioCol != null ? DataImportService.GetString(row, portfolioCol)?.Trim() : null,
                    SigningDate = signingDateCol != null ? DataImportService.GetDate(row, signingDateCol) : null,
                    ImportedAt = now
                });
            }

            if (list.Count == 0)
                return (false, "لم يتم العثور على أي عقود صالحة (عمود \"رقم العقد\" فارغ بكل الصفوف).", 0);

            await _db.ContractPeriodRecords.ExecuteDeleteAsync();
            await _db.ContractPeriodRecords.AddRangeAsync(list);
            await _db.SaveChangesAsync();

            return (true, $"تم استيراد {list.Count} عقد بنجاح.", list.Count);
        }
    }
}
