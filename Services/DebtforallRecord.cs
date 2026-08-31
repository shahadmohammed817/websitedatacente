using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ExcelDashboard.Services
{
    public interface IDebtforallImportService
    {
        /// <summary>
        /// يقرأ ملف "المديونيات لكل البنود" عبر IExcelDashboardService.AnalyzeFile - يكتشف صف
        /// الرأس الحقيقي تلقائيًا. يستبدل كل السجلات القديمة بالكامل عند إعادة الرفع.
        /// </summary>
        Task<(bool Success, string Message, int RowsImported)> ImportAsync(Stream fileStream);
    }

    public class DebtforallImportService : IDebtforallImportService
    {
        private readonly AppDbContext _db;
        private readonly IExcelDashboardService _excelService;
        private readonly IMemoryCache _cache;

        public DebtforallImportService(AppDbContext db, IExcelDashboardService excelService, IMemoryCache cache)
        {
            _db = db;
            _excelService = excelService;
            _cache = cache;
        }

        // أسماء الأعمدة الحقيقية بالملف بالضبط
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
        private const string ColCustomerName = "إسم العميل";
        private const string ColInvestorNumber = "رقم المستثمر";
        private const string ColInvestorName = "إسم المستثمر";
        private const string ColContractNumber = "رقم العقد";
        private const string ColContractDate = "تاريخ العقد";
        private const string ColInvoiceNumber = "رقم الفاتوره";
        private const string ColInvoiceDescription = "وصف الفاتوره";
        private const string ColRulingNumber = "رقم الحكم";
        private const string ColExecutionNumber = "رقم التنفيذ";
        private const string ColPaymentEntryNumber = "رقم إيفاء";
        private const string ColBatchStatusAr = "BATCH_STATUS_AR";
        private const string ColBatchStatusEn = "BATCH_STATUS_EN";

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
            var customerNameCol = Resolve(ColCustomerName);
            var investorNumberCol = Resolve(ColInvestorNumber);
            var investorNameCol = Resolve(ColInvestorName);
            var contractNumberCol = Resolve(ColContractNumber);
            var contractDateCol = Resolve(ColContractDate);
            var invoiceNumberCol = Resolve(ColInvoiceNumber);
            var invoiceDescriptionCol = Resolve(ColInvoiceDescription);
            var rulingNumberCol = Resolve(ColRulingNumber);
            var executionNumberCol = Resolve(ColExecutionNumber);
            var paymentEntryNumberCol = Resolve(ColPaymentEntryNumber);
            var batchStatusArCol = Resolve(ColBatchStatusAr);
            var batchStatusEnCol = Resolve(ColBatchStatusEn);

            var now = DateTime.UtcNow;
            var list = new List<DebtforallRecord>();
            foreach (var row in analyzed.Rows)
            {
                var classification = DataImportService.GetString(row, colClassification)?.Trim();
                if (string.IsNullOrWhiteSpace(classification)) continue; // صف فارغ/غير صالح

                list.Add(new DebtforallRecord
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
                    CustomerName = customerNameCol != null ? DataImportService.GetString(row, customerNameCol)?.Trim() : null,
                    InvestorNumber = investorNumberCol != null ? DataImportService.GetString(row, investorNumberCol)?.Trim() : null,
                    InvestorName = investorNameCol != null ? DataImportService.GetString(row, investorNameCol)?.Trim() : null,
                    ContractNumber = contractNumberCol != null ? DataImportService.GetString(row, contractNumberCol)?.Trim() : null,
                    ContractDate = contractDateCol != null ? DataImportService.GetDate(row, contractDateCol) : null,
                    InvoiceNumber = invoiceNumberCol != null ? DataImportService.GetString(row, invoiceNumberCol)?.Trim() : null,
                    InvoiceDescription = invoiceDescriptionCol != null ? DataImportService.GetString(row, invoiceDescriptionCol)?.Trim() : null,
                    RulingNumber = rulingNumberCol != null ? DataImportService.GetString(row, rulingNumberCol)?.Trim() : null,
                    ExecutionNumber = executionNumberCol != null ? DataImportService.GetString(row, executionNumberCol)?.Trim() : null,
                    PaymentEntryNumber = paymentEntryNumberCol != null ? DataImportService.GetString(row, paymentEntryNumberCol)?.Trim() : null,
                    BatchStatusAr = batchStatusArCol != null ? DataImportService.GetString(row, batchStatusArCol)?.Trim() : null,
                    BatchStatusEn = batchStatusEnCol != null ? DataImportService.GetString(row, batchStatusEnCol)?.Trim() : null,
                    ImportedAt = now
                });
            }

            if (list.Count == 0)
                return (false, "لم يتم العثور على أي سجلات صالحة (عمود \"تصنيف البند\" فارغ بكل الصفوف).", 0);

            await _db.DebtforallRecords.ExecuteDeleteAsync();
            await _db.DebtforallRecords.AddRangeAsync(list);
            await _db.SaveChangesAsync();
            _cache.Remove(DebtforallCache.RecordsKey);

            return (true, $"تم استيراد {list.Count} سجل بنجاح.", list.Count);
        }
    }
}
