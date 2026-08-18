using ClosedXML.Excel;
using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    public interface IClaimExecutionImportService
    {
        /// <summary>
        /// يقرأ ملف "تقرير التنفيذ القضائي" (لوحة المطالبات التنفيذية) عبر
        /// IExcelDashboardService.AnalyzeFile - رأس الأعمدة بالصف 1 (عادي). يستبدل كل المطالبات
        /// القديمة بالكامل عند إعادة الرفع.
        /// </summary>
        Task<(bool Success, string Message, int RowsImported)> ImportAsync(Stream fileStream);
    }

    public class ClaimExecutionImportService : IClaimExecutionImportService
    {
        private readonly AppDbContext _db;
        private readonly IExcelDashboardService _excelService;

        public ClaimExecutionImportService(AppDbContext db, IExcelDashboardService excelService)
        {
            _db = db;
            _excelService = excelService;
        }

        // أسماء الأعمدة الحقيقية بالملف المعتمد بالضبط (تأكّدت منها بفحص مباشر للملف)
        private const string ColAmanah = "الأمانة";
        private const string ColBaladiyah = "البلدية";
        private const string ColInvestorName = "أسم المستثمر";
        private const string ColOpportunityNumber = "رقم الفرصة";
        private const string ColOpportunityDescription = "وصف الفرصة";
        private const string ColOpportunityType = "نوع الفرصة";
        private const string ColContractType = "نوع العقد";
        private const string ColContractStatus = "حالة العقد";
        private const string ColContractNumber = "رقم العقد";
        private const string ColContractSource = "مصدر العقد";
        private const string ColPaymentNumber = "رقم الدفعه";
        private const string ColPaymentStatus = "حالة الدفعه";
        private const string ColPaymentDueDate = "تاريخ إستحقاق الدفعه";
        private const string ColPaymentValueWithTax = "قيمة الدفعة شامله قيمة الضريبة المضافة";
        private const string ColPaymentValueWithoutTax = "قيمة الدفعة بدون الضريبة";
        private const string ColTaxValue = "قيمة الضريبة";
        private const string ColInvoiceNumber = "رقم الفاتورة";
        private const string ColClaimStatusFull = "حالة المطالبة";
        private const string ColClaimStatusShort = "حالة المطالبة مختصرة";
        private const string ColCancellationReason = "سبب الإلغاء";
        private const string ColCancellationNotes = "ملاحظات الإلغاء";
        private const string ColReturnedFromJudiciaryDate = "تاريخ الإرجاع من قضايا";
        private const string ColSentToExecutionDate = "تاريخ الإرسال الى تنفيذ";
        private const string ColCancellationDate = "تاريخ الإلغاء";
        private const string ColRequestNumber = "رقم الطلب";
        private const string ColFirstNoticeNumber = "رقم الإشعار الأول";
        private const string ColFirstNoticeDate = "تاريخ الإشعار الأول";
        private const string ColSecondNoticeNumber = "رقم الإشعار الثاني";
        private const string ColSecondNoticeDate = "تاريخ الإشعار الثاني";
        private const string ColPerformanceClaimNoticeNumber = "رقم إشعار المطالبة بالأداء";
        private const string ColPerformanceClaimNoticeDate = "تاريخ إشعار المطالبة بالأداء";
        private const string ColJudiciaryReturnReason = "سبب الإعادة من نظام القضايا";
        private const string ColCompanyName = "إسم الشركه / الجهة";
        private const string ColPortfolio = "المحفظة";
        private const string ColGeneralDepartment = "الإدارة العامة";
        private const string ColPeriod = "الفترة";

        // عمود "الفترة" يحوي قيمًا نصية ("2022-2025") وقيمًا رقمية بحتة ("2026") بنفس العمود -
        // AnalyzeFile المشترك يحدّد نوع كل عمود بالأغلبية (نص أو رقم) ثم يُصفّر أي قيمة لا
        // تطابق النوع الغالب، فتضيع كل قيم "2026" الرقمية لأن أغلبية العمود نصية ("2022-2025")
        // - لذا نقرأ هذا العمود تحديدًا مباشرة من الملف الخام (ClosedXML) بمعزل عن AnalyzeFile
        private static List<string?> ReadPeriodColumnRaw(Stream fileStream)
        {
            var result = new List<string?>();
            try
            {
                fileStream.Position = 0;
                using var workbook = new XLWorkbook(fileStream);
                var ws = workbook.Worksheets.First();
                var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;

                int headerRow = 0, periodCol = 0;
                for (int r = 1; r <= Math.Min(10, lastRow); r++)
                {
                    for (int c = 1; c <= lastCol; c++)
                    {
                        if (ws.Cell(r, c).GetString().Trim() == "الفترة") { headerRow = r; periodCol = c; break; }
                    }
                    if (headerRow > 0) break;
                }
                if (headerRow == 0) return result;

                for (int r = headerRow + 1; r <= lastRow; r++)
                {
                    var cell = ws.Cell(r, periodCol);
                    string? val = null;
                    if (!cell.IsEmpty())
                    {
                        val = cell.DataType == XLDataType.DateTime && cell.TryGetValue<DateTime>(out var dt)
                            ? dt.Year.ToString()
                            : cell.GetString().Trim();
                        if (string.IsNullOrEmpty(val)) val = null;
                    }
                    result.Add(val);
                }
            }
            catch { /* لو فشلت القراءة الاحتياطية، يبقى العمود فارغًا بدل تعطيل الاستيراد كله */ }
            finally
            {
                fileStream.Position = 0;
            }
            return result;
        }

        public async Task<(bool Success, string Message, int RowsImported)> ImportAsync(Stream fileStream)
        {
            // يُقرأ قبل AnalyzeFile وتُعاد الدفة لبداية التدفّق بعده (انظر تعليق ReadPeriodColumnRaw)
            var periodColumnRaw = ReadPeriodColumnRaw(fileStream);

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
            // بعض نسخ الملف تحمل عمود "حالة الدفعه" باسم مكرَّر ("حالة الدفعه2") لأن الملف
            // الأصلي (Excel Export) يحوي عمودين بنفس الاسم فيُضيف Excel لاحقة رقمية تلقائيًا -
            // نطابقه بالاحتواء (Contains) كحل احتياطي إن فشل التطابق الحرفي
            string? ResolveContains(string substring) =>
                allColumnNames.FirstOrDefault(c => DataImportService.Normalize(c).Contains(DataImportService.Normalize(substring)));

            var contractNumberCol = Resolve(ColContractNumber);
            var invoiceNumberCol = Resolve(ColInvoiceNumber);
            var opportunityNumberCol = Resolve(ColOpportunityNumber);
            if (contractNumberCol == null && invoiceNumberCol == null && opportunityNumberCol == null)
            {
                return (false,
                    "لم يتم العثور على أعمدة \"رقم العقد\"/\"رقم الفاتورة\"/\"رقم الفرصة\" بالملف - تأكدي أن تخطيط الأعمدة لم يتغيّر.",
                    0);
            }

            var amanahCol = Resolve(ColAmanah);
            var baladiyahCol = Resolve(ColBaladiyah);
            var investorNameCol = Resolve(ColInvestorName);
            var opportunityDescriptionCol = Resolve(ColOpportunityDescription);
            var opportunityTypeCol = Resolve(ColOpportunityType);
            var contractTypeCol = Resolve(ColContractType);
            var contractStatusCol = Resolve(ColContractStatus);
            var contractSourceCol = Resolve(ColContractSource);
            var paymentNumberCol = Resolve(ColPaymentNumber);
            var paymentStatusCol = Resolve(ColPaymentStatus) ?? ResolveContains(ColPaymentStatus);
            var paymentDueDateCol = Resolve(ColPaymentDueDate);
            var paymentValueWithTaxCol = Resolve(ColPaymentValueWithTax);
            var paymentValueWithoutTaxCol = Resolve(ColPaymentValueWithoutTax);
            var taxValueCol = Resolve(ColTaxValue);
            var claimStatusFullCol = Resolve(ColClaimStatusFull);
            var claimStatusShortCol = Resolve(ColClaimStatusShort);
            var cancellationReasonCol = Resolve(ColCancellationReason);
            var cancellationNotesCol = Resolve(ColCancellationNotes);
            var returnedFromJudiciaryDateCol = Resolve(ColReturnedFromJudiciaryDate);
            var sentToExecutionDateCol = Resolve(ColSentToExecutionDate);
            var cancellationDateCol = Resolve(ColCancellationDate);
            var requestNumberCol = Resolve(ColRequestNumber);
            var firstNoticeNumberCol = Resolve(ColFirstNoticeNumber);
            var firstNoticeDateCol = Resolve(ColFirstNoticeDate);
            var secondNoticeNumberCol = Resolve(ColSecondNoticeNumber);
            var secondNoticeDateCol = Resolve(ColSecondNoticeDate);
            var performanceClaimNoticeNumberCol = Resolve(ColPerformanceClaimNoticeNumber);
            var performanceClaimNoticeDateCol = Resolve(ColPerformanceClaimNoticeDate);
            var judiciaryReturnReasonCol = Resolve(ColJudiciaryReturnReason);
            var companyNameCol = Resolve(ColCompanyName);
            var portfolioCol = Resolve(ColPortfolio);
            // بعض نسخ الملف يظهر بها حرف زائد بعنوان هذا العمود تحديدًا ("الاإدارة العامة" بدل
            // "الإدارة العامة") - على الأرجح خلل بأداة قراءة العناوين لا بالملف نفسه (فحص مباشر
            // للخلية عبر ClosedXML يعيد النص الصحيح) - نطابقه بجزء ثابت غير متأثر بهذا الخلل
            var generalDepartmentCol = Resolve(ColGeneralDepartment) ?? ResolveContains("دارة العامة");

            var now = DateTime.UtcNow;
            var list = new List<ClaimExecutionRecord>();
            for (int rowIdx = 0; rowIdx < analyzed.Rows.Count; rowIdx++)
            {
                var row = analyzed.Rows[rowIdx];
                // عمود "الفترة" يُقرأ من العمود الخام (periodColumnRaw) لا من row - انظر تعليق
                // ReadPeriodColumnRaw. الفهرسان متطابقان لأن كلاهما يمرّان بنفس ترتيب صفوف
                // البيانات بالملف (صف العناوين + ما بعده)
                var periodValue = rowIdx < periodColumnRaw.Count ? periodColumnRaw[rowIdx] : null;

                list.Add(new ClaimExecutionRecord
                {
                    Amanah = amanahCol != null ? DataImportService.GetString(row, amanahCol)?.Trim() : null,
                    Baladiyah = baladiyahCol != null ? DataImportService.GetString(row, baladiyahCol)?.Trim() : null,
                    InvestorName = investorNameCol != null ? DataImportService.GetString(row, investorNameCol)?.Trim() : null,
                    OpportunityNumber = opportunityNumberCol != null ? DataImportService.GetString(row, opportunityNumberCol)?.Trim() : null,
                    OpportunityDescription = opportunityDescriptionCol != null ? DataImportService.GetString(row, opportunityDescriptionCol)?.Trim() : null,
                    OpportunityType = opportunityTypeCol != null ? DataImportService.GetString(row, opportunityTypeCol)?.Trim() : null,
                    ContractType = contractTypeCol != null ? DataImportService.GetString(row, contractTypeCol)?.Trim() : null,
                    ContractStatus = contractStatusCol != null ? DataImportService.GetString(row, contractStatusCol)?.Trim() : null,
                    ContractNumber = contractNumberCol != null ? DataImportService.GetString(row, contractNumberCol)?.Trim() : null,
                    ContractSource = contractSourceCol != null ? DataImportService.GetString(row, contractSourceCol)?.Trim() : null,
                    PaymentNumber = paymentNumberCol != null ? DataImportService.GetString(row, paymentNumberCol)?.Trim() : null,
                    PaymentStatus = paymentStatusCol != null ? DataImportService.GetString(row, paymentStatusCol)?.Trim() : null,
                    PaymentDueDate = paymentDueDateCol != null ? DataImportService.GetDate(row, paymentDueDateCol) : null,
                    PaymentValueWithTax = paymentValueWithTaxCol != null ? DataImportService.GetDecimal(row, paymentValueWithTaxCol) : null,
                    PaymentValueWithoutTax = paymentValueWithoutTaxCol != null ? DataImportService.GetDecimal(row, paymentValueWithoutTaxCol) : null,
                    TaxValue = taxValueCol != null ? DataImportService.GetDecimal(row, taxValueCol) : null,
                    InvoiceNumber = invoiceNumberCol != null ? DataImportService.GetString(row, invoiceNumberCol)?.Trim() : null,
                    ClaimStatusFull = claimStatusFullCol != null ? DataImportService.GetString(row, claimStatusFullCol)?.Trim() : null,
                    ClaimStatusShort = claimStatusShortCol != null ? DataImportService.GetString(row, claimStatusShortCol)?.Trim() : null,
                    CancellationReason = cancellationReasonCol != null ? DataImportService.GetString(row, cancellationReasonCol)?.Trim() : null,
                    CancellationNotes = cancellationNotesCol != null ? DataImportService.GetString(row, cancellationNotesCol)?.Trim() : null,
                    ReturnedFromJudiciaryDate = returnedFromJudiciaryDateCol != null ? DataImportService.GetDate(row, returnedFromJudiciaryDateCol) : null,
                    SentToExecutionDate = sentToExecutionDateCol != null ? DataImportService.GetDate(row, sentToExecutionDateCol) : null,
                    CancellationDate = cancellationDateCol != null ? DataImportService.GetDate(row, cancellationDateCol) : null,
                    RequestNumber = requestNumberCol != null ? DataImportService.GetString(row, requestNumberCol)?.Trim() : null,
                    FirstNoticeNumber = firstNoticeNumberCol != null ? DataImportService.GetString(row, firstNoticeNumberCol)?.Trim() : null,
                    FirstNoticeDate = firstNoticeDateCol != null ? DataImportService.GetDate(row, firstNoticeDateCol) : null,
                    SecondNoticeNumber = secondNoticeNumberCol != null ? DataImportService.GetString(row, secondNoticeNumberCol)?.Trim() : null,
                    SecondNoticeDate = secondNoticeDateCol != null ? DataImportService.GetDate(row, secondNoticeDateCol) : null,
                    PerformanceClaimNoticeNumber = performanceClaimNoticeNumberCol != null ? DataImportService.GetString(row, performanceClaimNoticeNumberCol)?.Trim() : null,
                    PerformanceClaimNoticeDate = performanceClaimNoticeDateCol != null ? DataImportService.GetDate(row, performanceClaimNoticeDateCol) : null,
                    JudiciaryReturnReason = judiciaryReturnReasonCol != null ? DataImportService.GetString(row, judiciaryReturnReasonCol)?.Trim() : null,
                    CompanyName = companyNameCol != null ? DataImportService.GetString(row, companyNameCol)?.Trim() : null,
                    Portfolio = portfolioCol != null ? DataImportService.GetString(row, portfolioCol)?.Trim() : null,
                    GeneralDepartment = generalDepartmentCol != null ? DataImportService.GetString(row, generalDepartmentCol)?.Trim() : null,
                    Period = periodValue,
                    ImportedAt = now
                });
            }

            await _db.ClaimExecutionRecords.ExecuteDeleteAsync();
            await _db.ClaimExecutionRecords.AddRangeAsync(list);
            await _db.SaveChangesAsync();

            return (true, $"تم استيراد {list.Count} مطالبة تنفيذية بنجاح.", list.Count);
        }
    }
}
