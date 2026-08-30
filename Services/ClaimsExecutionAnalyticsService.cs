using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    public class ClaimsExecutionPageData
    {
        public bool HasData { get; set; }

        // ==================== 4 كروت KPI ====================
        // 1) إجمالي المطالبات: كل الحالات ما عدا "أكتملت/لم تكتمل المدة النظامية"
        public int TotalCount { get; set; }
        public decimal TotalValue { get; set; }

        // 2) إجمالي المعالج: 3 حالات محدَّدة (إلغاء بطلب الأمانة / إلغاء بدفع الفاتورة / إرسال ناجح لقضايا)
        public int ProcessedCount { get; set; }
        public decimal ProcessedValue { get; set; }

        // 3) إجمالي المتبقي: 4 حالات محدَّدة (غير مكتملة / إعادة من قضايا لملاحظات / بانتظار اعتماد الإلغاء / رفض من قضايا)
        public int RemainingCount { get; set; }
        public decimal RemainingValue { get; set; }

        // 4) نسبة المطالبات = المتبقي ÷ الإجمالي × 100
        public decimal RemainingPercent { get; set; }

        // ==================== رسم "المسار الصحيح / أكتملت المدة / لم تكتمل المدة" ====================
        // "المسار الصحيح" = نفس تعريف TotalCount/TotalValue أعلاه (كل الحالات ما عدا الحالتين
        // التاليتين)، والعمودان الآخران لكل حالة استثناء لحالها
        public int CompletedPeriodCount { get; set; }
        public decimal CompletedPeriodValue { get; set; }
        public int IncompletePeriodCount { get; set; }
        public decimal IncompletePeriodValue { get; set; }

        // ==================== رسم عمودي: معالج/متبقي حسب الإدارة العامة (رسم 1) والفرعية (رسم 2) ====================
        public List<string> DeptLabels { get; set; } = new();
        public List<int> DeptProcessedCounts { get; set; } = new();
        public List<int> DeptRemainingCounts { get; set; } = new();
        public List<ClxPortfolioProcessedRow> PortfolioBreakdown { get; set; } = new();

        // ==================== دونات: توزيع حالات "المتبقي" الأربع ====================
        public List<StatusCountRow> RemainingStatusBreakdown { get; set; } = new();

        // ==================== "المديونيات حسب الفترة" - من عمود "الفترة" (2022-2025 / 2026)، لصفوف حالة الدفعة = غير محصلة فقط ====================
        public List<string> PeriodLabels { get; set; } = new();
        public List<int> PeriodCounts { get; set; } = new();
        public List<decimal> PeriodValues { get; set; } = new();

        public List<string> DepartmentOptions { get; set; } = new();
    }

    public class StatusCountRow
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class ClxPortfolioProcessedRow
    {
        public string DepartmentLabel { get; set; } = string.Empty;
        public string PortfolioLabel { get; set; } = string.Empty;
        public int ProcessedCount { get; set; }
        public int RemainingCount { get; set; }
    }

    public interface IClaimsExecutionAnalyticsService
    {
        Task<ClaimsExecutionPageData> GetDataAsync(
            string? department, string? claimStatus,
            string? opportunityNumber = null, string? contractNumber = null, string? invoiceNumber = null);

        Task<List<ClaimExecutionRecord>> FindByContractNumberAsync(string contractNumber);

        /// <summary>صفوف تصدير الصفحة (Excel/PDF) - نفس تصفية GetDataAsync بالضبط</summary>
        Task<List<ClaimExecutionRecord>> GetExportRowsAsync(string? department, string? claimStatus, string? opportunityNumber, string? contractNumber, string? invoiceNumber);
    }

    public class ClaimsExecutionAnalyticsService : IClaimsExecutionAnalyticsService
    {
        private readonly AppDbContext _db;

        public ClaimsExecutionAnalyticsService(AppDbContext db)
        {
            _db = db;
        }

        // تصنيف حالات "حالة المطالبة" (ClaimStatusFull) الثلاثة حسب طلب المستخدمة - بالاسم الحرفي
        // الموجود فعليًا بالملف (تأكّدت منه بفحص مباشر)، مطابَق بتطبيع (Normalize) يتسامح مع
        // اختلاف الهمزة (أ/إ/آ/ا) بين نسخ الملف المختلفة
        private const string CompletedPeriodStatus = "أكتملت المدة النظامية لرفع المطالبة";
        // اسم هذي الحالة بالملف الفعلي فيه أخطاء إملائية ("النضامية" بدل "النظامية"، و"الطالبات"
        // بدل "الطلبات/المطالبات" - تأكّدنا منها بفحص مباشر لقيم العمود) لا يلتقطها Normalize
        // العادي (يطبّع الهمزة فقط، لا الظاء/الضاد) - نطابق بجزء مختصر قبل منطقة الأخطاء تمامًا
        private const string IncompletePeriodStableSubstring = "لم تكتمل المده";
        private static readonly string[] ProcessedStatuses =
        {
            "مطالبة تم الغاؤها بطلب من موظف الامانة",
            "مطالبة تم الغاؤها بسبب دفع الفاتورة",
            "مطالبة تم ارسالها لنظام القضايا بنجاح"
        };
        private static readonly string[] RemainingStatuses =
        {
            "مطالبة غير مكتملة البيانات",
            "مطالبة تم اعادتها من نظام القضايا لوجود ملاحظات",
            "بانتظار اعتماد الغاء المطالبة",
            "مطالبة تم رفضها عن طريق نظام القضايا"
        };

        private static readonly HashSet<string> ProcessedStatusesNorm = ProcessedStatuses.Select(DataImportService.Normalize).ToHashSet();
        private static readonly HashSet<string> RemainingStatusesNorm = RemainingStatuses.Select(DataImportService.Normalize).ToHashSet();

        private static readonly string CompletedPeriodNorm = DataImportService.Normalize(CompletedPeriodStatus);
        private static readonly string IncompletePeriodSubstringNorm = DataImportService.Normalize(IncompletePeriodStableSubstring);
        private static bool IsCompletedPeriod(string? status) => status != null && DataImportService.Normalize(status) == CompletedPeriodNorm;
        private static bool IsIncompletePeriod(string? status) => status != null && DataImportService.Normalize(status).Contains(IncompletePeriodSubstringNorm);
        private static bool IsExcluded(string? status) => IsCompletedPeriod(status) || IsIncompletePeriod(status);
        private static bool IsProcessed(string? status) => status != null && ProcessedStatusesNorm.Contains(DataImportService.Normalize(status));
        private static bool IsRemaining(string? status) => status != null && RemainingStatusesNorm.Contains(DataImportService.Normalize(status));

        // حالة الدفعة (PaymentStatus) التي تعني "غير محصّلة" (=مديونية) - أساس رسم "حسب الفترة"
        private static bool IsUnpaid(string? paymentStatus) =>
            paymentStatus != null && DataImportService.Normalize(paymentStatus).Contains(DataImportService.Normalize("غير محصلة"));

        public async Task<ClaimsExecutionPageData> GetDataAsync(
            string? department, string? claimStatus,
            string? opportunityNumber = null, string? contractNumber = null, string? invoiceNumber = null)
        {
            var result = new ClaimsExecutionPageData();

            var allRecords = await _db.ClaimExecutionRecords.AsNoTracking().ToListAsync();
            result.HasData = allRecords.Count > 0;
            if (!result.HasData) return result;

            result.DepartmentOptions = allRecords
                .Where(r => !string.IsNullOrWhiteSpace(r.GeneralDepartment))
                .Select(r => r.GeneralDepartment!).Distinct().OrderBy(x => x).ToList();

            IEnumerable<ClaimExecutionRecord> rows = allRecords;
            if (!string.IsNullOrWhiteSpace(department)) rows = rows.Where(r => r.GeneralDepartment == department);
            if (!string.IsNullOrWhiteSpace(claimStatus)) rows = rows.Where(r => r.ClaimStatusFull == claimStatus);
            if (!string.IsNullOrWhiteSpace(opportunityNumber)) rows = rows.Where(r => r.OpportunityNumber != null && r.OpportunityNumber.Contains(opportunityNumber));
            if (!string.IsNullOrWhiteSpace(contractNumber)) rows = rows.Where(r => r.ContractNumber != null && r.ContractNumber.Contains(contractNumber));
            if (!string.IsNullOrWhiteSpace(invoiceNumber)) rows = rows.Where(r => r.InvoiceNumber != null && r.InvoiceNumber.Contains(invoiceNumber));
            var filteredRows = rows.ToList();

            decimal Value(IEnumerable<ClaimExecutionRecord> recs) => recs.Sum(r => r.PaymentValueWithoutTax ?? 0);

            // ==================== كروت KPI ====================
            var totalRows = filteredRows.Where(r => !IsExcluded(r.ClaimStatusFull)).ToList();
            result.TotalCount = totalRows.Count;
            result.TotalValue = Value(totalRows);

            var processedRows = filteredRows.Where(r => IsProcessed(r.ClaimStatusFull)).ToList();
            result.ProcessedCount = processedRows.Count;
            result.ProcessedValue = Value(processedRows);

            var remainingRows = filteredRows.Where(r => IsRemaining(r.ClaimStatusFull)).ToList();
            result.RemainingCount = remainingRows.Count;
            result.RemainingValue = Value(remainingRows);

            result.RemainingPercent = result.TotalCount > 0
                ? Math.Round((decimal)result.RemainingCount / result.TotalCount * 100, 1) : 0;

            // ==================== رسم "المسار الصحيح / أكتملت المدة / لم تكتمل المدة" ====================
            var completedPeriodRows = filteredRows.Where(r => IsCompletedPeriod(r.ClaimStatusFull)).ToList();
            result.CompletedPeriodCount = completedPeriodRows.Count;
            result.CompletedPeriodValue = Value(completedPeriodRows);

            var incompletePeriodRows = filteredRows.Where(r => IsIncompletePeriod(r.ClaimStatusFull)).ToList();
            result.IncompletePeriodCount = incompletePeriodRows.Count;
            result.IncompletePeriodValue = Value(incompletePeriodRows);

            // ==================== رسم 1+2: معالج/متبقي حسب الإدارة العامة/الفرعية ====================
            var byDept = totalRows.Where(r => !string.IsNullOrWhiteSpace(r.GeneralDepartment))
                .GroupBy(r => r.GeneralDepartment!)
                .Select(g => new
                {
                    Label = g.Key,
                    Processed = g.Count(r => IsProcessed(r.ClaimStatusFull)),
                    Remaining = g.Count(r => IsRemaining(r.ClaimStatusFull))
                })
                .OrderByDescending(x => x.Processed + x.Remaining)
                .ToList();
            result.DeptLabels = byDept.Select(x => x.Label).ToList();
            result.DeptProcessedCounts = byDept.Select(x => x.Processed).ToList();
            result.DeptRemainingCounts = byDept.Select(x => x.Remaining).ToList();

            result.PortfolioBreakdown = totalRows
                .Where(r => !string.IsNullOrWhiteSpace(r.GeneralDepartment) && !string.IsNullOrWhiteSpace(r.Portfolio))
                .GroupBy(r => new { Dept = r.GeneralDepartment!, Portfolio = r.Portfolio! })
                .Select(g => new ClxPortfolioProcessedRow
                {
                    DepartmentLabel = g.Key.Dept,
                    PortfolioLabel = g.Key.Portfolio,
                    ProcessedCount = g.Count(r => IsProcessed(r.ClaimStatusFull)),
                    RemainingCount = g.Count(r => IsRemaining(r.ClaimStatusFull))
                })
                .OrderByDescending(x => x.ProcessedCount + x.RemainingCount)
                .ToList();

            // ==================== دونات: توزيع حالات "المتبقي" الأربع ====================
            result.RemainingStatusBreakdown = remainingRows
                .Where(r => !string.IsNullOrWhiteSpace(r.ClaimStatusFull))
                .GroupBy(r => r.ClaimStatusFull!)
                .Select(g => new StatusCountRow { Status = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            // ==================== "المديونيات حسب الفترة" - حالة الدفعة = غير محصّلة، حالة المطالبة
            // ليست ضمن حالتَي "أكتملت/لم تكتمل المدة النظامية" (بطلب صريح من المستخدمة)،
            // مصنَّفة بعمود "الفترة" - يظهر البُعدان (2022-2025 و2026) دائمًا ولو بصفر ====================
            var unpaidRows = filteredRows.Where(r => IsUnpaid(r.PaymentStatus) && !IsExcluded(r.ClaimStatusFull) && !string.IsNullOrWhiteSpace(r.Period)).ToList();
            var periodOrder = new[] { "2022-2025", "2026" };
            var byPeriod = unpaidRows.GroupBy(r => r.Period!.Trim())
                .ToDictionary(g => g.Key, g => new { Count = g.Count(), Value = Value(g) });
            result.PeriodLabels = periodOrder.ToList();
            result.PeriodCounts = periodOrder.Select(p => byPeriod.TryGetValue(p, out var v) ? v.Count : 0).ToList();
            result.PeriodValues = periodOrder.Select(p => byPeriod.TryGetValue(p, out var v) ? v.Value : 0m).ToList();

            return result;
        }

        public async Task<List<ClaimExecutionRecord>> FindByContractNumberAsync(string contractNumber) =>
            await _db.ClaimExecutionRecords.AsNoTracking().Where(r => r.ContractNumber == contractNumber).ToListAsync();

        public async Task<List<ClaimExecutionRecord>> GetExportRowsAsync(string? department, string? claimStatus, string? opportunityNumber, string? contractNumber, string? invoiceNumber)
        {
            var allRecords = await _db.ClaimExecutionRecords.AsNoTracking().ToListAsync();
            IEnumerable<ClaimExecutionRecord> rows = allRecords;
            if (!string.IsNullOrWhiteSpace(department)) rows = rows.Where(r => r.GeneralDepartment == department);
            if (!string.IsNullOrWhiteSpace(claimStatus)) rows = rows.Where(r => r.ClaimStatusFull == claimStatus);
            if (!string.IsNullOrWhiteSpace(opportunityNumber)) rows = rows.Where(r => r.OpportunityNumber != null && r.OpportunityNumber.Contains(opportunityNumber));
            if (!string.IsNullOrWhiteSpace(contractNumber)) rows = rows.Where(r => r.ContractNumber != null && r.ContractNumber.Contains(contractNumber));
            if (!string.IsNullOrWhiteSpace(invoiceNumber)) rows = rows.Where(r => r.InvoiceNumber != null && r.InvoiceNumber.Contains(invoiceNumber));
            return rows.Where(r => !IsExcluded(r.ClaimStatusFull)).OrderBy(r => r.GeneralDepartment).ToList();
        }
    }
}
