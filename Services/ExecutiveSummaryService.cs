using ExcelDashboard.Models;

namespace ExcelDashboard.Services
{
    /// <summary>
    /// بيانات صفحة "الملخص التنفيذي" - نظرة سريعة مجمّعة من أهم أربع صفحات بالنظام (الرئيسية،
    /// المديونيات والمطالبات، المديونيات لجميع البنود، الطرح) دون أي حساب جديد: كل رقم هنا مسحوب
    /// مباشرة من نفس خدمات تلك الصفحات حتى يبقى مطابقًا لها تمامًا.
    /// </summary>
    public class ExecutiveSummaryData
    {
        /// <summary>نفس رسم "أداء الإيرادات حسب نوع الإيراد" بالصفحة الرئيسية (مفوتر/محصّل لكل نوع إيراد) - كل النطاقين معًا</summary>
        public List<RevenueTypePerformanceRow> RevenueTypePerformance { get; set; } = new();

        // ===== بطاقات "المحصل الحالي/السابق ونسبة النمو" أعلى رسم "أداء الإيرادات" بالرئيسية =====
        public int RevenueCurrentYear { get; set; }
        public int RevenuePreviousYear { get; set; }
        public decimal RevenueCurrentYearCollected { get; set; }
        public decimal RevenuePreviousYearCollected { get; set; }
        public decimal RevenueGrowthPercent { get; set; }

        /// <summary>معالج/متبقي لكل (إدارة عامة، محفظة فرعية) - نفس بيانات
        /// ClaimsExecutionPageData.PortfolioBreakdown بالضبط (3 إدارات عامة فعلية: محافظ
        /// الاستثمارات التجارية/التنموية والخدمات)، تُجمَّع بالمتصفح حسب الإدارة العامة لحساب
        /// نسبة المعالجة، ثم تُفلتَر حسب الإدارة المختارة لعرض محافظها الفرعية</summary>
        public List<ClxPortfolioProcessedRow> GeneralDeptPortfolios { get; set; } = new();

        // ===== المديونيات والمطالبات =====
        public bool ClaimsExecutionHasData { get; set; }
        public int ClaimsTotalCount { get; set; }
        public decimal ClaimsTotalValue { get; set; }
        public decimal ClaimsProcessedValue { get; set; }
        public decimal ClaimsRemainingValue { get; set; }
        public decimal ClaimsRemainingPercent { get; set; }
        public List<StatusCountRow> ClaimsRemainingStatusBreakdown { get; set; } = new();

        // ===== المديونيات لجميع البنود =====
        public bool DebtforallHasData { get; set; }
        public decimal DebtforallTotalValue { get; set; }
        public int DebtforallTotalCount { get; set; }
        public List<DebtforallClassificationRow> DebtforallByClassification { get; set; } = new();

        /// <summary>البنود الفرعية ("البند") لكل بند رئيسي على حدة - الضغط على بند بدونات
        /// "توزيع المديونيات حسب البند" يبدّل هذا التفصيل المعروض بالرسم المجاور</summary>
        public List<DebtforallSubItemBreakdown> DebtforallSubItemsByClassification { get; set; } = new();

        // ===== الطرح =====
        public bool TarhHasData { get; set; }
        public int TarhTotalOpportunities { get; set; }
        public int TarhFinalizedCount { get; set; }
        public decimal TarhFinalizedPercent { get; set; }
        public List<TarhTypeSummaryRow> TarhTypeSummary { get; set; } = new();

        /// <summary>إجمالي قيمة الفرص المطروحة 2026 (بغض النظر عن حالتها) - كرت "إجمالي القيمة" بصف 1 من الصفحة الجديدة</summary>
        public decimal TarhTotalOpportunitiesValue { get; set; }

        /// <summary>عدد/قيمة الفرص المطروحة 2026 حسب نوع العقد مقابل ما أُبرم منها - دونات وبار صف 1 من الصفحة الجديدة</summary>
        public List<TarhTypeTenderedRow> TarhTypeTenderedSummary { get; set; } = new();

        /// <summary>قيمة العقود المبرومة 2026 حسب الإدارة العامة - رسم ثالث بصف 1 من الصفحة الجديدة</summary>
        public List<TarhDeptSummaryRow> TarhFinalizedByDepartment { get; set; } = new();
    }

    public interface IExecutiveSummaryService
    {
        /// <summary>رسم "أداء الإيرادات حسب نوع الإيراد" بالصفحة الرئيسية - تُستخدم هنا فقط لسحب كروت نمو الإيرادات (محصل حالي/سابق ونسبة النمو)</summary>
        Task<ExecutiveSummaryData> GetRevenuePerfAsync();

        /// <summary>باقي الأقسام تُحمَّل كل واحد بطلب مستقل - forcedDepartment: لموظف مقيّد بإدارة عامة واحدة، null لمستخدم غير مقيّد</summary>
        Task<ExecutiveSummaryData> GetDeptDataAsync(string? forcedDepartment = null);
        Task<ExecutiveSummaryData> GetDebtDataAsync();
        Task<ExecutiveSummaryData> GetClaimsDataAsync(string? forcedDepartment = null);

        /// <summary>قسم "الفرص المطروحة 2026" (صف 1 من التصميم الجديد) - عدد/قيمة الفرص وحسب النوع، بلا فلاتر</summary>
        Task<ExecutiveSummaryData> GetTarhOpportunitiesSummaryAsync();

        /// <summary>قسم "تحقيق المستهدف" (صف 2) - مُفلتر بالنطاق (أمانة/بلديات) والبند الرئيسي/الفرعي/مصدر المستهدف/السنة</summary>
        Task<TargetAchievementData> GetTargetAchievementAsync(string? scope, string? mainItem, string? subItem, string? targetSource, int? year);

        /// <summary>قوائم فلاتر قسم "تحقيق المستهدف" (بنود رئيسية/فرعية) - تعتمد على النطاق المختار</summary>
        Task<TargetFilterOptionsData> GetTargetFilterOptionsAsync(string? scope);
    }

    /// <summary>
    /// بيانات كل قسم من صفحة "الملخص التنفيذي" تُحمَّل بطلب مستقل عن بقية الأقسام (بدل طلب واحد
    /// ضخم ينتظر كل الحسابات معًا): قسم الإيرادات فقط يُحمَّل فور فتح الصفحة، وبقية الأقسام
    /// (الإدارات العامة/المديونيات لجميع البنود/المديونيات والمطالبات/الطرح) تُحمَّل كل واحد
    /// عند فتح المستخدم له فعليًا (accordion) - يجعل أهم رقم بالصفحة (الإيرادات) يظهر بأسرع وقت
    /// ممكن، ويوفّر حساب الأقسام الأخرى كليًا إن لم يفتحها المستخدم
    /// </summary>
    public class ExecutiveSummaryService : IExecutiveSummaryService
    {
        private readonly IAnalyticsService _analytics;
        private readonly IClaimsExecutionAnalyticsService _claimsExecution;
        private readonly IDebtforallAnalyticsService _debtforall;

        public ExecutiveSummaryService(
            IAnalyticsService analytics,
            IClaimsExecutionAnalyticsService claimsExecution,
            IDebtforallAnalyticsService debtforall)
        {
            _analytics = analytics;
            _claimsExecution = claimsExecution;
            _debtforall = debtforall;
        }

        public async Task<ExecutiveSummaryData> GetRevenuePerfAsync()
        {
            var result = new ExecutiveSummaryData();
            var overview = await _analytics.GetOverviewDataAsync(new[] { ScopeType.Amanah, ScopeType.Baladiyah });
            result.RevenueTypePerformance = overview.RevenueTypePerformance;
            result.RevenueCurrentYear = overview.RevenueCurrentYear;
            result.RevenuePreviousYear = overview.RevenuePreviousYear;
            result.RevenueCurrentYearCollected = overview.RevenueCurrentYearCollected;
            result.RevenuePreviousYearCollected = overview.RevenuePreviousYearCollected;
            result.RevenueGrowthPercent = overview.RevenueGrowthPercent;
            return result;
        }

        public async Task<ExecutiveSummaryData> GetDeptDataAsync(string? forcedDepartment = null)
        {
            var result = new ExecutiveSummaryData();
            var claims = await _claimsExecution.GetDataAsync(forcedDepartment, null);
            result.GeneralDeptPortfolios = claims.PortfolioBreakdown;
            return result;
        }

        public async Task<ExecutiveSummaryData> GetDebtDataAsync()
        {
            var result = new ExecutiveSummaryData();
            var debtforall = await _debtforall.GetDataAsync(null, null, null, null, null);
            result.DebtforallHasData = debtforall.HasData;
            result.DebtforallTotalValue = debtforall.TotalValue;
            result.DebtforallTotalCount = debtforall.TotalCount;
            result.DebtforallByClassification = debtforall.ByClassification;
            result.DebtforallSubItemsByClassification = debtforall.SubItemsByClassification;
            return result;
        }

        public async Task<ExecutiveSummaryData> GetClaimsDataAsync(string? forcedDepartment = null)
        {
            var result = new ExecutiveSummaryData();
            var claims = await _claimsExecution.GetDataAsync(forcedDepartment, null);
            result.ClaimsExecutionHasData = claims.HasData;
            result.ClaimsTotalCount = claims.TotalCount;
            result.ClaimsTotalValue = claims.TotalValue;
            result.ClaimsProcessedValue = claims.ProcessedValue;
            result.ClaimsRemainingValue = claims.RemainingValue;
            result.ClaimsRemainingPercent = claims.RemainingPercent;
            result.ClaimsRemainingStatusBreakdown = claims.RemainingStatusBreakdown;
            return result;
        }

        public async Task<ExecutiveSummaryData> GetTarhOpportunitiesSummaryAsync()
        {
            var result = new ExecutiveSummaryData();
            var tarh = await _analytics.GetTarhDataAsync(null, null, null, null, null, null);
            result.TarhHasData = tarh.HasData;
            result.TarhTypeSummary = tarh.TypeSummary;
            result.TarhTypeTenderedSummary = tarh.TypeTenderedSummary;
            result.TarhFinalizedByDepartment = tarh.FinalizedByDepartment;

            // صف 1 بصفحة "الملخص التنفيذي" يعرض بيانات 2026 فقط بطلب صريح من المستخدمة - بخلاف
            // TarhPageData.TotalOpportunities/FinalizedCount/FinalizedPercent الأصلية (كل السنوات
            // معًا، هكذا عمدًا بصفحة "الطرح" نفسها) - فنُعيد حسابها هنا من TypeTenderedSummary/
            // TypeSummary (كلاهما 2026 فقط، بكل الأنواع الأربعة معًا)
            result.TarhTotalOpportunities = tarh.TypeTenderedSummary.Sum(t => t.TenderedCount);
            result.TarhTotalOpportunitiesValue = tarh.TypeTenderedSummary.Sum(t => t.TenderedValue);
            result.TarhFinalizedCount = tarh.TypeSummary.Sum(t => t.Count);
            result.TarhFinalizedPercent = result.TarhTotalOpportunities > 0
                ? Math.Round((decimal)result.TarhFinalizedCount / result.TarhTotalOpportunities * 100, 1) : 0;
            return result;
        }

        public Task<TargetAchievementData> GetTargetAchievementAsync(string? scope, string? mainItem, string? subItem, string? targetSource, int? year)
            => _analytics.GetTargetAchievementAsync(scope, mainItem, subItem, targetSource, year);

        public Task<TargetFilterOptionsData> GetTargetFilterOptionsAsync(string? scope)
            => _analytics.GetTargetFilterOptionsAsync(scope);
    }
}
