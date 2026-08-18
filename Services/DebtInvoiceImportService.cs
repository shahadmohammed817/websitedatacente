using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    public interface IDebtInvoiceImportService
    {
        /// <summary>
        /// يقرأ ملف "جميع الفواتير" (لوحة المديونيات) عبر IExcelDashboardService.AnalyzeFile -
        /// يكتشف صف الرأس الحقيقي تلقائيًا (الملف المعتمد رأسه بالصف 3، الصفان 1-2 نص "Applied
        /// filters" تعريفي). يستبدل كل الفواتير القديمة بالكامل عند إعادة الرفع.
        /// </summary>
        Task<(bool Success, string Message, int RowsImported)> ImportAsync(Stream fileStream);
    }

    public class DebtInvoiceImportService : IDebtInvoiceImportService
    {
        private readonly AppDbContext _db;
        private readonly IExcelDashboardService _excelService;

        public DebtInvoiceImportService(AppDbContext db, IExcelDashboardService excelService)
        {
            _db = db;
            _excelService = excelService;
        }

        // أسماء الأعمدة الحقيقية بالملف المعتمد بالضبط (تأكّدت منها بفحص مباشر للملف)
        private const string ColScope = "الأمانة/بلدى";
        private const string ColEntity = "الجهة";
        private const string ColClassification = "تصنيف البند";
        private const string ColCategory = "البند";
        private const string ColItemNumber = "رقم البند";
        private const string ColPaymentStatus = "حالة السداد";
        private const string ColItemType = "نوع البند";
        private const string ColValue = "القيمة";
        private const string ColCreatedDate = "تاريخ الإنشاء";
        private const string ColPaymentDate = "تاريخ الدفع";
        private const string ColSettlementDate = "تاريخ التسوية";
        private const string ColCancellationDate = "تاريخ الإلغاء";
        private const string ColCustomerName = "إسم العميل";
        private const string ColInvestorNumber = "رقم المستثمر";
        private const string ColInvestorName = "إسم المستثمر";
        private const string ColContractNumber = "رقم العقد";
        private const string ColContractIdRaw = "CONTRACT_ID";
        private const string ColContractDate = "تاريخ العقد";
        private const string ColInvoiceNumber = "رقم الفاتوره";
        private const string ColInvoiceDescription = "وصف الفاتوره";
        private const string ColRulingNumber = "رقم الحكم";
        private const string ColExecutionNumber = "رقم التنفيذ";
        private const string ColPaymentEntryNumber = "رقم إيفاء";
        private const string ColBatchStatusAr = "BATCH_STATUS_AR";
        private const string ColBatchStatusEn = "BATCH_STATUS_EN";
        private const string ColBatchNumber = "رقم الدفعة";
        private const string ColDaysElapsed = "عدد الأيام المنقضية من اصدار الفاتورة";
        private const string ColContractType = "نوع العقد";
        private const string ColContractStatus = "حالة العقد";
        private const string ColIsLegalExecution = "هل الفاتورة ضمن التنفيذ القضائي";
        private const string ColLegalExecutionClaimStatus = "حالة المطالبة في التنفيذ القضائي";
        private const string ColStatementUpdate = "تحديث الإفادة  (الفواتير)";
        private const string ColPreviousPortfolio = "المحفظة السابقة";
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

            var colClassification = Resolve(ColClassification);
            if (colClassification == null)
            {
                return (false,
                    "لم يتم العثور على عمود \"تصنيف البند\" بالملف - تأكدي أن تخطيط الأعمدة لم يتغيّر.",
                    0);
            }

            var scopeCol = Resolve(ColScope);
            var entityCol = Resolve(ColEntity);
            var categoryCol = Resolve(ColCategory);
            var itemNumberCol = Resolve(ColItemNumber);
            var paymentStatusCol = Resolve(ColPaymentStatus);
            var itemTypeCol = Resolve(ColItemType);
            var valueCol = Resolve(ColValue);
            var createdDateCol = Resolve(ColCreatedDate);
            var paymentDateCol = Resolve(ColPaymentDate);
            var settlementDateCol = Resolve(ColSettlementDate);
            var cancellationDateCol = Resolve(ColCancellationDate);
            var customerNameCol = Resolve(ColCustomerName);
            var investorNumberCol = Resolve(ColInvestorNumber);
            var investorNameCol = Resolve(ColInvestorName);
            var contractNumberCol = Resolve(ColContractNumber);
            var contractIdRawCol = Resolve(ColContractIdRaw);
            var contractDateCol = Resolve(ColContractDate);
            var invoiceNumberCol = Resolve(ColInvoiceNumber);
            var invoiceDescriptionCol = Resolve(ColInvoiceDescription);
            var rulingNumberCol = Resolve(ColRulingNumber);
            var executionNumberCol = Resolve(ColExecutionNumber);
            var paymentEntryNumberCol = Resolve(ColPaymentEntryNumber);
            var batchStatusArCol = Resolve(ColBatchStatusAr);
            var batchStatusEnCol = Resolve(ColBatchStatusEn);
            var batchNumberCol = Resolve(ColBatchNumber);
            var daysElapsedCol = Resolve(ColDaysElapsed);
            var contractTypeCol = Resolve(ColContractType);
            var contractStatusCol = Resolve(ColContractStatus);
            var isLegalExecutionCol = Resolve(ColIsLegalExecution);
            var legalExecutionClaimStatusCol = Resolve(ColLegalExecutionClaimStatus);
            var statementUpdateCol = Resolve(ColStatementUpdate);
            var previousPortfolioCol = Resolve(ColPreviousPortfolio);
            var generalDepartmentCol = Resolve(ColGeneralDepartment);
            var portfolioCol = Resolve(ColPortfolio);

            var now = DateTime.UtcNow;
            var list = new List<DebtInvoiceRecord>();
            foreach (var row in analyzed.Rows)
            {
                var classification = DataImportService.GetString(row, colClassification)?.Trim();
                if (string.IsNullOrWhiteSpace(classification)) continue; // صف فارغ/غير صالح

                int? daysElapsed = null;
                if (daysElapsedCol != null)
                {
                    var d = DataImportService.GetDecimal(row, daysElapsedCol);
                    if (d.HasValue) daysElapsed = (int)Math.Round(d.Value);
                }

                list.Add(new DebtInvoiceRecord
                {
                    Scope = scopeCol != null ? DataImportService.GetString(row, scopeCol)?.Trim() : null,
                    Entity = entityCol != null ? DataImportService.GetString(row, entityCol)?.Trim() : null,
                    Classification = classification,
                    Category = categoryCol != null ? DataImportService.GetString(row, categoryCol)?.Trim() : null,
                    ItemNumber = itemNumberCol != null ? DataImportService.GetString(row, itemNumberCol)?.Trim() : null,
                    PaymentStatus = paymentStatusCol != null ? DataImportService.GetString(row, paymentStatusCol)?.Trim() : null,
                    ItemType = itemTypeCol != null ? DataImportService.GetString(row, itemTypeCol)?.Trim() : null,
                    Value = valueCol != null ? DataImportService.GetDecimal(row, valueCol) : null,
                    CreatedDate = createdDateCol != null ? DataImportService.GetDate(row, createdDateCol) : null,
                    PaymentDate = paymentDateCol != null ? DataImportService.GetDate(row, paymentDateCol) : null,
                    SettlementDate = settlementDateCol != null ? DataImportService.GetDate(row, settlementDateCol) : null,
                    CancellationDate = cancellationDateCol != null ? DataImportService.GetDate(row, cancellationDateCol) : null,
                    CustomerName = customerNameCol != null ? DataImportService.GetString(row, customerNameCol)?.Trim() : null,
                    InvestorNumber = investorNumberCol != null ? DataImportService.GetString(row, investorNumberCol)?.Trim() : null,
                    InvestorName = investorNameCol != null ? DataImportService.GetString(row, investorNameCol)?.Trim() : null,
                    ContractNumber = contractNumberCol != null ? DataImportService.GetString(row, contractNumberCol)?.Trim() : null,
                    ContractIdRaw = contractIdRawCol != null ? DataImportService.GetString(row, contractIdRawCol)?.Trim() : null,
                    ContractDate = contractDateCol != null ? DataImportService.GetDate(row, contractDateCol) : null,
                    InvoiceNumber = invoiceNumberCol != null ? DataImportService.GetString(row, invoiceNumberCol)?.Trim() : null,
                    InvoiceDescription = invoiceDescriptionCol != null ? DataImportService.GetString(row, invoiceDescriptionCol)?.Trim() : null,
                    RulingNumber = rulingNumberCol != null ? DataImportService.GetString(row, rulingNumberCol)?.Trim() : null,
                    ExecutionNumber = executionNumberCol != null ? DataImportService.GetString(row, executionNumberCol)?.Trim() : null,
                    PaymentEntryNumber = paymentEntryNumberCol != null ? DataImportService.GetString(row, paymentEntryNumberCol)?.Trim() : null,
                    BatchStatusAr = batchStatusArCol != null ? DataImportService.GetString(row, batchStatusArCol)?.Trim() : null,
                    BatchStatusEn = batchStatusEnCol != null ? DataImportService.GetString(row, batchStatusEnCol)?.Trim() : null,
                    BatchNumber = batchNumberCol != null ? DataImportService.GetString(row, batchNumberCol)?.Trim() : null,
                    DaysElapsed = daysElapsed,
                    ContractType = contractTypeCol != null ? DataImportService.GetString(row, contractTypeCol)?.Trim() : null,
                    ContractStatus = contractStatusCol != null ? DataImportService.GetString(row, contractStatusCol)?.Trim() : null,
                    IsLegalExecution = isLegalExecutionCol != null ? DataImportService.GetString(row, isLegalExecutionCol)?.Trim() : null,
                    LegalExecutionClaimStatus = legalExecutionClaimStatusCol != null ? DataImportService.GetString(row, legalExecutionClaimStatusCol)?.Trim() : null,
                    StatementUpdate = statementUpdateCol != null ? DataImportService.GetString(row, statementUpdateCol)?.Trim() : null,
                    PreviousPortfolio = previousPortfolioCol != null ? DataImportService.GetString(row, previousPortfolioCol)?.Trim() : null,
                    GeneralDepartment = generalDepartmentCol != null ? DataImportService.GetString(row, generalDepartmentCol)?.Trim() : null,
                    Portfolio = portfolioCol != null ? DataImportService.GetString(row, portfolioCol)?.Trim() : null,
                    ImportedAt = now
                });
            }

            if (list.Count == 0)
                return (false, "لم يتم العثور على أي فواتير صالحة (عمود \"تصنيف البند\" فارغ بكل الصفوف).", 0);

            await _db.DebtInvoiceRecords.ExecuteDeleteAsync();
            await _db.DebtInvoiceRecords.AddRangeAsync(list);
            await _db.SaveChangesAsync();

            return (true, $"تم استيراد {list.Count} فاتورة مديونية بنجاح.", list.Count);
        }
    }
}
