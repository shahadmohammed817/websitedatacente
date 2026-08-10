using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    public class SectionDashboardData
    {
        public decimal TotalRevenue { get; set; }
        public decimal CollectedRevenue { get; set; }
        public decimal UncollectedRevenue { get; set; }
        public decimal CollectionRatePercent { get; set; }
        public decimal TargetAchievementPercent { get; set; }

        public List<string> CategoryLabels { get; set; } = new();
        public List<decimal> CategoryCollected { get; set; } = new();
        public List<decimal> CategoryUncollected { get; set; } = new();

        public List<string> TrendLabels { get; set; } = new();
        public List<decimal> TrendValues { get; set; } = new();

        /// <summary>
        /// نفس الاتجاه الشهري أعلاه لكن مقسّم إلى محصّل/غير محصّل (لرسم "اتجاه الإيرادات
        /// الشهري" بثلاثة خطوط: محصّل، غير محصّل، وإجمالي الإيرادات)
        /// </summary>
        public List<decimal> TrendCollected { get; set; } = new();
        public List<decimal> TrendUncollected { get; set; } = new();

        /// <summary>
        /// تفاصيل كاملة لكل فئة/بند (إجمالي، محصّل، غير محصّل، هدف، واتجاه شهري خاص بها)
        /// تُرسَل للواجهة كاملة حتى يمكن عمل "تصفية تفاعلية" (Cross-Filter بأسلوب Power BI):
        /// عند الضغط على أي فئة في الرسم البياني تُعاد حساب كل البطاقات والرسوم الأخرى فورًا
        /// من هذه البيانات نفسها في المتصفح دون أي طلب إضافي للخادم.
        /// </summary>
        public List<CategoryDetail> CategoryDetails { get; set; } = new();

        /// <summary>
        /// الإيرادات الشهرية لكل جهة (أمانة المدينة المنورة نفسها + كل بلدية) مبنية على
        /// عمود "تاريخ الدفع" — تُستخدم في رسم الأعمدة الأول أعلى اللوحة
        /// </summary>
        public List<string> EntityMonthLabels { get; set; } = new();
        public List<EntityMonthlySeries> EntityMonthlySeries { get; set; } = new();

        /// <summary>
        /// عنوان رسم الأعمدة الأول أعلى اللوحة — يختلف بين لوحة "عام" (إجمالي الأمانة
        /// مقابل إجمالي البلديات) ولوحات النطاق/البند (تفصيل حسب كل جهة على حدة)
        /// </summary>
        public string EntityChartTitle { get; set; } = "الإيرادات الشهرية لكل جهة";

        /// <summary>
        /// نص الشارة أعلى رسم الأعمدة الأول (يوضّح للمستخدم ماذا يفعل الضغط على أي عمود)
        /// </summary>
        public string EntityChartBadge { get; set; } = "اضغط لعرض الجهة";

        /// <summary>
        /// true فقط في لوحة "عام" حيث يكون كل عمود إجمالي نطاق كامل (الأمانة/البلديات)
        /// وليس جهة فعلية واحدة — تحتاج الواجهة لمعرفة هذا لتوجيه الضغط لصفحة النطاق
        /// الصحيحة بدل البحث عن جهة باسم "الأمانة" أو "البلديات" في قاعدة البيانات
        /// </summary>
        public bool EntityChartIsAggregated { get; set; }

        /// <summary>
        /// توزيع الإيرادات حسب البنود الفرعية (عمود "الفئة/الصنف" لكل سجل) — تُستخدم برسم
        /// "توزيع الإيرادات حسب البنود الفرعية" بالصفحة الرئيسية، بمستوى أدق من
        /// CategoryLabels التي تُجمَّع على مستوى البند الرئيسي فقط بلوحة "عام".
        /// أعلى البنود الفرعية فقط، والباقي مجمّع تحت "أخرى" حتى يبقى الرسم واضحًا.
        /// </summary>
        public List<string> SubItemLabels { get; set; } = new();
        public List<decimal> SubItemValues { get; set; } = new();

        /// <summary>
        /// أعلى بنود الإيرادات — تُبنى دائمًا مباشرة من عمود "تصنيف البند" الخام
        /// (Classification) لكل سجل، بصرف النظر عن مستوى اللوحة (عام/نطاق/جهة/بند)،
        /// حتى تعرض البنود الرئيسية فقط (استثمار، رسوم، جزاءات...) دون أي بند فرعي.
        /// تُستخدم برسم "أعلى بنود الإيرادات".
        /// </summary>
        public List<string> TopItemLabels { get; set; } = new();
        public List<decimal> TopItemValues { get; set; } = new();

        /// <summary>
        /// توزيع الإيرادات حسب الربع (Q1..Q4) بالاعتماد على عمود "الربع" بالملف
        /// </summary>
        public List<string> QuarterLabels { get; set; } = new();
        public List<decimal> QuarterValues { get; set; } = new();

        public bool HasData { get; set; }

        /// <summary>
        /// تفصيل "الإدارات العامة" (محافظ الاستثمارات التجارية/التنموية/الخدمات) وإداراتها
        /// الفرعية التابعة لبند الاستثمار بالأمانة فقط — تُملأ حصريًا من GetScopeDataAsync
        /// عند scope=Amanah (بدون فلترة على إدارة/بند فرعي محدد) لخدمة رسوم صفحة "بنود
        /// الأمانة" الأربعة (لا تؤثر على أي صفحة أخرى). مرتّبة تنازليًا حسب الإجمالي.
        /// يستخدمها الواجهة الأمامية لتثبيت رسمَي "الإيرادات حسب الإدارات العامة" و"المحصّل
        /// مقابل غير المحصّل حسب الإدارات العامة" على مستوى الإدارات العامة دائمًا، بصرف
        /// النظر عن أي إدارة فرعية يتم اختيارها من القائمة الجانبية.
        /// </summary>
        public List<DeptBreakdownDetail> InvestmentDepts { get; set; } = new();
    }

    /// <summary>
    /// تفصيل إدارة عامة واحدة (باسمها المجمّع عبر كل نسخها المكررة) + توزيع إداراتها
    /// الفرعية — يُستخدم برسوم صفحة "بنود الأمانة" (الإيرادات حسب الإدارات العامة،
    /// توزيع الإيرادات حسب الإدارات الفرعية، توزيع الإيرادات بين الإدارات العامة)
    /// </summary>
    public class DeptBreakdownDetail
    {
        public string Name { get; set; } = string.Empty;
        public decimal Total { get; set; }
        /// <summary>
        /// إجمالي الإيرادات المحصّلة/غير المحصّلة لهذه الإدارة العامة (بكل نسخها المكررة
        /// وإداراتها الفرعية) — تُستخدم برسم "المحصّل مقابل غير المحصّل حسب الإدارات العامة"
        /// </summary>
        public decimal Collected { get; set; }
        public decimal Uncollected { get; set; }
        public List<string> SubLabels { get; set; } = new();
        public List<decimal> SubValues { get; set; } = new();
    }


    /// <summary>
    /// تفصيل بيانات فئة/بند واحد (تُستخدم للتصفية التفاعلية بين الرسوم عند الضغط على أي بند)
    /// </summary>
    public class CategoryDetail
    {
        public string Label { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public decimal Collected { get; set; }
        public decimal Uncollected { get; set; }
        public decimal Target { get; set; }
        public List<string> TrendLabels { get; set; } = new();
        public List<decimal> TrendValues { get; set; } = new();
        public List<decimal> TrendCollected { get; set; } = new();
        public List<decimal> TrendUncollected { get; set; } = new();
    }

    /// <summary>
    /// سلسلة الإيرادات الشهرية لجهة واحدة (أمانة المدينة أو إحدى البلديات)
    /// القيم مُحاذاة مع مصفوفة EntityMonthLabels (صفر للأشهر التي لا يوجد بها بيانات لهذه الجهة)
    /// </summary>
    public class EntityMonthlySeries
    {
        public string Entity { get; set; } = string.Empty;
        public List<decimal> Values { get; set; } = new();
    }

    /// <summary>
    /// صف مستهدف واحد (جهة + نوع إيراد) لصفحة "مقارنة المستهدفات" — يظهر بجدول المستهدفات
    /// </summary>
    public class TargetBreakdownRow
    {
        public string Entity { get; set; } = string.Empty;
        public string RevenueType { get; set; } = string.Empty;
        public decimal TargetFinance { get; set; }
        public decimal TargetBaladiyah { get; set; }
    }

    /// <summary>
    /// صف واحد لكل جهة (الأمانة أو بلدية) بجدول ورسم "المستهدف السنوي مقابل المفوتر حسب
    /// الجهة" — المستهدف السنوي هنا دائمًا = مجموع الأشهر الاثني عشر من ملف "المستهدف
    /// الشهري" المرفوع لنفس الجهة (وليس من أي عمود سنوي منفصل)، بغض النظر عن تصفية الجهة
    /// المختارة بالصفحة (تُحسب دائمًا لكل الجهات معًا لتغذية الرسم والجدول).
    /// </summary>
    public class EntityAnnualRow
    {
        public string Entity { get; set; } = string.Empty;
        public decimal AnnualTarget { get; set; }
        public decimal InvoicedTotal { get; set; }
        public decimal AchievementPercent { get; set; }
    }

    /// <summary>
    /// صف بند واحد (من عمود "تصنيف البند") لرسم "تحقيق المستهدف حسب البنود والأرباع" — يحمل
    /// قيم كل ربع (المحصّل لوزارة المالية والمفوتر لوزارة البلديات والإسكان) بالإضافة للمستهدف
    /// السنوي الخاص بهذا البند (مجموع مستهدفات كل أنواع الإيراد التابعة له)
    /// </summary>
    public class ItemQuarterlyRow
    {
        /// <summary>اسم البند كما هو بعمود "تصنيف البند" بالملف (استثمار، رسوم، جزاءات...)</summary>
        public string ItemLabel { get; set; } = string.Empty;

        /// <summary>المحصّل لكل ربع (Q1..Q4) — وزارة المالية</summary>
        public List<decimal> FinanceCollectedByQuarter { get; set; } = new();

        /// <summary>المفوتر (إجمالي الإيرادات) لكل ربع (Q1..Q4) — وزارة البلديات والإسكان</summary>
        public List<decimal> BaladiyahInvoicedByQuarter { get; set; } = new();

        public decimal TargetFinance { get; set; }
        public decimal TargetBaladiyah { get; set; }

        /// <summary>
        /// المستهدف "التراكمي" لكل ربع، بنفس منطق تراكم البيانات الفعلية (المفوتر/المحصّل):
        /// الربع الأول = مستهدف الأشهر 1-3، الثاني = 1-6، الثالث = 1-9، الرابع = 1-12 (=المستهدف
        /// السنوي كاملاً). المالية: تقدير خطي (السنوي × عدد الأشهر ÷ 12) لعدم توفر ملف مستهدف
        /// شهري خاص بها. البلديات: من ملف "المستهدف الشهري" الفعلي إن توفّر لهذا البند، وإلا
        /// نفس التقدير الخطي.
        /// </summary>
        public List<decimal> TargetFinanceByQuarter { get; set; } = new();
        public List<decimal> TargetBaladiyahByQuarter { get; set; } = new();
    }

    /// <summary>
    /// بيانات صفحة "مقارنة المستهدفات": مقارنة شهرية بين المُحصَّل والمستهدف السنوي - وزارة
    /// المالية، ومقارنة شهرية أخرى بين المفوتر (إجمالي القيمة) والمستهدف السنوي - وزارة
    /// البلديات والإسكان. القيم التراكمية تُقارَن بخط مستهدف تراكمي بنفس المنطق، بينما القيم
    /// الشهرية (غير التراكمية) تُستخدم كأعمدة برسم الأداء الشهري. كما تحتوي على تفصيل ربع سنوي
    /// حسب كل بند (نوع إيراد) لرسم "تحقيق المستهدف حسب البنود والأرباع".
    /// </summary>
    public class TargetsComparisonData
    {
        public bool HasData { get; set; }

        public List<string> MonthLabels { get; set; } = new();

        // مقارنة "المحصّل" مقابل "المستهدف السنوي - وزارة المالية"
        public decimal FinanceTargetTotal { get; set; }
        public decimal FinanceCollectedTotal { get; set; }
        public decimal FinanceAchievementPercent { get; set; }
        public List<decimal> FinanceCollectedCumulative { get; set; } = new();
        public List<decimal> FinanceCollectedMonthly { get; set; } = new();
        public List<decimal> FinanceTargetLine { get; set; } = new();

        // مقارنة "المفوتر" (إجمالي القيمة بصرف النظر عن التحصيل) مقابل "المستهدف السنوي -
        // وزارة البلديات والإسكان"
        public decimal BaladiyahTargetTotal { get; set; }
        public decimal BaladiyahInvoicedTotal { get; set; }
        public decimal BaladiyahAchievementPercent { get; set; }
        public List<decimal> BaladiyahInvoicedCumulative { get; set; } = new();
        public List<decimal> BaladiyahInvoicedMonthly { get; set; } = new();
        public List<decimal> BaladiyahTargetLine { get; set; } = new();

        /// <summary>هل خط مستهدف البلديات مبني على ملف "المستهدف الشهري" الفعلي المرفوع؟ (وإلا فهو توزيع تقديري: السنوي ÷ 12)</summary>
        public bool BaladiyahTargetIsActualMonthly { get; set; }

        // تحقيق المستهدف حسب البنود والأرباع
        public List<string> QuarterLabels { get; set; } = new() { "الربع الأول", "الربع الثاني", "الربع الثالث", "الربع الرابع" };
        public List<ItemQuarterlyRow> ItemsQuarterly { get; set; } = new();

        public List<TargetBreakdownRow> TargetsBreakdown { get; set; } = new();
        public List<string> Entities { get; set; } = new();

        /// <summary>
        /// المستهدف السنوي مقابل المفوتر لكل جهة (الأمانة + كل بلدية) - يُحسب دائمًا لكل
        /// الجهات معًا (بصرف النظر عن تصفية الجهة المختارة) لتغذية رسم/جدول المقارنة بين
        /// الجهات أعلى الصفحة. المستهدف هنا دائمًا = مجموع الأشهر الاثني عشر بملف "المستهدف
        /// الشهري" المرفوع.
        /// </summary>
        public List<EntityAnnualRow> EntityAnnualTargets { get; set; } = new();

        /// <summary>
        /// (صفحة "مستهدفات البلديات والإسكان" فقط) المستهدف السنوي مقابل المفوتر لكل بند
        /// رئيسي، مبنية مباشرة من عمود "البند الرئيسي" بملف "المستهدف الشهري" (وليس من
        /// "نوع الإيراد"/"تصنيف البند" كما بـ <see cref="ItemsQuarterly"/>) — تُصفَّى بنفس
        /// الجهة المختارة (Entity field يحمل هنا اسم البند، لا اسم الجهة).
        /// </summary>
        public List<EntityAnnualRow> ItemsMainAnnual { get; set; } = new();
    }

    /// <summary>صف بند واحد بصفحة "مستهدفات وزارة المالية": محصّل (بيانات سابقة) مقابل مستهدف سنوي (ملف "مستهدف المالية")</summary>
    public class FinanceTargetItemRow
    {
        public string ItemLabel { get; set; } = string.Empty;
        public decimal Collected { get; set; }
        public decimal Target { get; set; }
        public decimal AchievementPercent { get; set; }
    }

    /// <summary>
    /// بيانات صفحة "مستهدفات وزارة المالية": إجمالي محصّل/مستهدف/نسبة تحقيق لكل من الأمانة
    /// والبلديات (كنطاق واحد مجمّع، لا تفصيل لكل بلدية)، بالإضافة لتفصيل حسب البند الرئيسي
    /// لكل نطاق على حدة. المستهدف من ملف "مستهدف المالية" (<see cref="FinanceItemTarget"/>)،
    /// والمحصّل من DashboardRecords كما هو (بيانات الإيرادات المستوردة سابقًا).
    /// </summary>
    public class FinanceTargetsPageData
    {
        public bool HasData { get; set; }

        public decimal AmanahCollectedTotal { get; set; }
        public decimal AmanahTargetTotal { get; set; }
        public decimal AmanahAchievementPercent { get; set; }

        public decimal BaladiyahCollectedTotal { get; set; }
        public decimal BaladiyahTargetTotal { get; set; }
        public decimal BaladiyahAchievementPercent { get; set; }

        public List<FinanceTargetItemRow> AmanahItems { get; set; } = new();
        public List<FinanceTargetItemRow> BaladiyahItems { get; set; } = new();
    }

    /// <summary>صف نوع عقد واحد (كرت) بصفحة "الطرح" - عدد وقيمة العقود من هذا النوع</summary>
    public class TarhTypeSummaryRow
    {
        public string TypeLabel { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
    }

    /// <summary>صف إدارة عامة واحدة بصفحة "الطرح" - عدد وقيمة عقودها</summary>
    public class TarhDeptRow
    {
        public string DeptLabel { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
    }

    /// <summary>صف نشاط واحد (بند فرعي) بصفحة "الطرح" - عدد وقيمة عقوده ضمن إدارة معيّنة</summary>
    public class TarhActivityRow
    {
        public string ActivityLabel { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
    }

    /// <summary>صف سنة واحدة بصفحة "الطرح" - عدد العقود المبرومة بهذه السنة</summary>
    public class TarhYearRow
    {
        public int Year { get; set; }
        public int Count { get; set; }
    }

    /// <summary>فرصة فتحت مظاريفها منذ 30 يومًا فأكثر ولا يوجد لها عقد بعد - بصفحة "الطرح"</summary>
    public class TarhAtRiskRow
    {
        public string? OpportunityNumber { get; set; }
        public string? OpportunityName { get; set; }
        public DateTime EnvelopeOpeningDate { get; set; }
        public int DaysSinceEnvelope { get; set; }
        public string? GeneralDepartment { get; set; }
        public string? Status { get; set; }
    }

    /// <summary>
    /// بيانات صفحة "الطرح": كروت أنواع العقود، توزيع الإدارات العامة، نسبة العقود المبرومة،
    /// البنود الفرعية (المحفظة الفرعية) ضمن إدارة معيّنة، والاتجاه الزمني للعقود المبرومة،
    /// والفرص المتأخرة (فتح مظاريف قبل 30 يومًا فأكثر بلا عقد) - كل ذلك قابل للتصفية بالسنة
    /// والربع والإدارة العامة وحالة العقد ورقم العقد معًا.
    /// </summary>
    public class TarhPageData
    {
        public bool HasData { get; set; }

        public List<TarhTypeSummaryRow> TypeSummary { get; set; } = new();
        public List<TarhDeptRow> DeptChart { get; set; } = new();

        public int TotalCount { get; set; }
        public int FinalizedCount { get; set; }
        public decimal FinalizedPercent { get; set; }

        /// <summary>إجمالي عدد الفرص (رقم الفرصة) ضمن التصفية الحالية</summary>
        public int TotalOpportunities { get; set; }

        /// <summary>الإدارة العامة التي تُعرَض بنودها الفرعية حاليًا (المُختارة بالفلتر، أو "الإدارة العامة لمحافظ الاستثمارات التجارية" افتراضيًا)</summary>
        public string ActivitiesDepartment { get; set; } = string.Empty;
        public List<TarhActivityRow> ActivityChart { get; set; } = new();

        public List<TarhYearRow> YearlyFinalizedTrend { get; set; } = new();

        /// <summary>الفرص التي فتحت مظاريفها منذ 30 يومًا فأكثر ولا يوجد لها رقم عقد بعد (الأقدم أولاً)</summary>
        public List<TarhAtRiskRow> AtRiskOpportunities { get; set; } = new();

        public List<string> DepartmentOptions { get; set; } = new();
        public List<string> StatusOptions { get; set; } = new();
        public List<int> YearOptions { get; set; } = new();
    }

    public interface IAnalyticsService
    {
        /// <summary>
        /// بيانات صفحة "مقارنة المستهدفات" — يمكن تصفيتها اختياريًا بجهة واحدة (بلدية/الأمانة)
        /// </summary>
        Task<TargetsComparisonData> GetTargetsComparisonDataAsync(string? entity = null);

        /// <summary>بيانات صفحة "مستهدفات وزارة المالية" (أمانة/بلديات، محصّل مقابل مستهدف سنوي لكل بند)</summary>
        Task<FinanceTargetsPageData> GetFinanceTargetsPageDataAsync();

        /// <summary>بيانات صفحة "الطرح" (كروت أنواع العقود، توزيع الإدارات، نسبة الإبرام، البنود الفرعية، الاتجاه الزمني، الفرص المتأخرة)</summary>
        Task<TarhPageData> GetTarhDataAsync(string? department, string? status, string? contractNumber, int? year, int? quarter);

        Task<SectionDashboardData> GetSectionDataAsync(int sectionId, bool includeSubSections = true, string? quarter = null, string? contractNumber = null);

        /// <summary>
        /// بيانات مجمّعة لعدة بنود معاً (مع كل بنودها الفرعية) — تُستخدم لمجموعات
        /// الإدارات العامة المكررة بنفس الاسم تحت أكثر من بند
        /// </summary>
        Task<SectionDashboardData> GetSectionsDataAsync(IEnumerable<int> sectionIds, string? quarter = null, string? contractNumber = null);

        /// <summary>
        /// بيانات لوحة كاملة لجهة واحدة (مثل: بلدية الحناكية) عبر كل بنودها
        /// </summary>
        Task<SectionDashboardData> GetEntityDataAsync(string entityName, string? quarter = null, string? contractNumber = null);

        Task<decimal> GetScopeTotalRevenueAsync(ScopeType scope);

        /// <summary>
        /// بيانات لوحة تفاعلية مجمّعة لكل بنود نطاق معين (الأمانة أو البلدية) بالكامل
        /// </summary>
        Task<SectionDashboardData> GetScopeDataAsync(ScopeType scope, string? quarter = null, string? contractNumber = null);

        /// <summary>
        /// بيانات لوحة عامة مجمّعة لعدة نطاقات معاً (تُستخدم كبديل عن نطاق "الكل" القديم)
        /// </summary>
        Task<SectionDashboardData> GetOverviewDataAsync(IEnumerable<ScopeType> scopes, string? quarter = null, string? contractNumber = null);
    }

    public class AnalyticsService : IAnalyticsService
    {
        private readonly AppDbContext _db;

        public AnalyticsService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// يطبّق تصفية اختيارية بحسب رقم العقد (بحث جزئي) والربع (تطابق تام) على استعلام السجلات.
        /// تُستخدم من مربع البحث وقائمة "الربع" التفاعلية أعلى كل لوحة بيانات.
        /// </summary>
        private static IQueryable<DashboardRecord> ApplyFilters(IQueryable<DashboardRecord> query, string? quarter, string? contractNumber)
        {
            if (!string.IsNullOrWhiteSpace(quarter))
            {
                query = query.Where(r => r.Quarter == quarter);
            }

            if (!string.IsNullOrWhiteSpace(contractNumber))
            {
                var term = contractNumber.Trim();
                query = query.Where(r => r.ContractNumber != null && r.ContractNumber.Contains(term));
            }

            return query;
        }

        public async Task<SectionDashboardData> GetSectionDataAsync(int sectionId, bool includeSubSections = true, string? quarter = null, string? contractNumber = null)
        {
            var sectionIds = new List<int> { sectionId };

            if (includeSubSections)
            {
                sectionIds.AddRange(await GetDescendantIdsAsync(sectionId));
            }

            var query = _db.DashboardRecords.Where(r => sectionIds.Contains(r.SectionId));
            var records = await ApplyFilters(query, quarter, contractNumber).ToListAsync();

            return BuildResult(records);
        }

        public async Task<SectionDashboardData> GetEntityDataAsync(string entityName, string? quarter = null, string? contractNumber = null)
        {
            var query = _db.DashboardRecords.Where(r => r.Entity == entityName);
            var records = await ApplyFilters(query, quarter, contractNumber).ToListAsync();

            return BuildResult(records);
        }

        public async Task<SectionDashboardData> GetSectionsDataAsync(IEnumerable<int> sectionIds, string? quarter = null, string? contractNumber = null)
        {
            var allIds = new List<int>();
            foreach (var id in sectionIds.Distinct())
            {
                allIds.Add(id);
                allIds.AddRange(await GetDescendantIdsAsync(id));
            }
            allIds = allIds.Distinct().ToList();

            var query = _db.DashboardRecords.Where(r => allIds.Contains(r.SectionId));
            var records = await ApplyFilters(query, quarter, contractNumber).ToListAsync();

            return BuildResult(records);
        }

        /// <summary>
        /// يجمع كل معرّفات البنود التابعة لبند معين بأي عمق (إدارات عامة ← إدارات فرعية ← ...)
        /// حتى تُحتسب بيانات كل المستويات الأعمق ضمن لوحة البند الأعلى تلقائياً
        /// </summary>
        private async Task<List<int>> GetDescendantIdsAsync(int sectionId)
        {
            var all = await _db.Sections
                .Where(s => s.IsActive)
                .Select(s => new { s.Id, s.ParentSectionId })
                .ToListAsync();

            var result = new List<int>();
            var queue = new Queue<int>();
            queue.Enqueue(sectionId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var children = all.Where(s => s.ParentSectionId == current).Select(s => s.Id);
                foreach (var childId in children)
                {
                    result.Add(childId);
                    queue.Enqueue(childId);
                }
            }

            return result;
        }

        public async Task<decimal> GetScopeTotalRevenueAsync(ScopeType scope)
        {
            var sectionIds = await _db.Sections
                .Where(s => s.Scope == scope && s.IsActive)
                .Select(s => s.Id)
                .ToListAsync();

            return await _db.DashboardRecords
                .Where(r => sectionIds.Contains(r.SectionId))
                .SumAsync(r => r.TotalValue ?? 0);
        }

        /// <summary>
        /// يجمّع بيانات كل البنود (الرئيسية والفرعية) التابعة لنطاق واحد (الأمانة أو البلدية)
        /// ليُعرض كلوحة بيانات عامة عند اختيار النطاق من القائمة الجانبية مباشرة
        /// </summary>
        public async Task<SectionDashboardData> GetScopeDataAsync(ScopeType scope, string? quarter = null, string? contractNumber = null)
        {
            var sectionIds = await _db.Sections
                .Where(s => s.Scope == scope && s.IsActive)
                .Select(s => s.Id)
                .ToListAsync();

            var query = _db.DashboardRecords.Where(r => sectionIds.Contains(r.SectionId));
            var records = await ApplyFilters(query, quarter, contractNumber).ToListAsync();

            // مستوى النطاق (الأمانة / البلدية): الرسم يعرض البنود الأساسية فقط
            // (تصنيف البند)، وليس البنود الفرعية
            var rootNameMap = await BuildRootNameMapAsync(sectionIds);

            // نطاق البلدية بدون اختيار بلدية معينة: يحتوي على عشرات البلديات (كل واحدة
            // "جهة" مستقلة بعمود "الجهة")، فلو عرضنا كل بلدية بخط مستقل بالرسم الخطي
            // (Line Chart) سيصبح مزدحمًا جدًا. لذا نجمّع الكل هنا في خط واحد يمثل إجمالي
            // "البلديات" (بحسب عمود "المنطقة")، تمامًا كما يظهر بالصفحة الرئيسية، على أن
            // يظل استعراض كل بلدية على حدة متاحًا من القائمة الجانبية (يفتح GetEntityDataAsync
            // فيعرض خطًا خاصًا بتلك البلدية فقط من عمود "الجهة")
            Dictionary<int, string>? entityGroupOverride = null;
            string? entityChartTitle = null;
            string? entityChartBadge = null;
            if (scope == ScopeType.Baladiyah)
            {
                entityGroupOverride = sectionIds.Distinct().ToDictionary(id => id, _ => "البلديات");
                entityChartTitle = "الإيرادات الشهرية لإجمالي البلديات";
                entityChartBadge = "اختر بلدية من القائمة لعرض تفاصيلها";
            }

            var result = BuildResult(records, rootNameMap, entityGroupOverride, entityChartTitle, entityChartBadge, subItemsRootFilter: "استثمار");

            // صفحة "بنود الأمانة" فقط: نُرفق تفصيل الإدارات العامة/الفرعية الخاص ببند
            // الاستثمار (لا يؤثر إطلاقًا على نطاق البلدية أو أي صفحة أخرى)
            if (scope == ScopeType.Amanah)
            {
                result.InvestmentDepts = await BuildInvestmentDeptBreakdownAsync(records);
            }

            return result;
        }

        /// <summary>
        /// يبني تفصيل "الإدارات العامة" (محافظ الاستثمارات التجارية/التنموية/الخدمات) وإداراتها
        /// الفرعية لبند الاستثمار بالأمانة، بالاعتماد على شجرة البنود (Section) مباشرة بدلاً من
        /// عمود "الفئة" الخام بالسجل، حتى يكون التجميع دقيقًا بصرف النظر عن أي بند فرعي
        /// (تأجير مباني/تأجير مرافق/ريع أراضي) يتبعه السجل فعليًا. تُستخدم حصريًا برسوم صفحة
        /// "بنود الأمانة" الأربعة (الإيرادات حسب الإدارات العامة / أعلى الإدارات الفرعية
        /// إيرادًا / المحصّل مقابل غير المحصّل حسب الإدارات العامة / اتجاه الإيرادات).
        /// </summary>
        private async Task<List<DeptBreakdownDetail>> BuildInvestmentDeptBreakdownAsync(List<DashboardRecord> records)
        {
            var allSections = await _db.Sections
                .Where(s => s.Scope == ScopeType.Amanah && s.IsActive)
                .Select(s => new { s.Id, s.NameAr, s.ParentSectionId })
                .ToListAsync();

            var byParent = allSections.ToLookup(s => s.ParentSectionId);

            List<int> Descendants(int id)
            {
                var descendants = new List<int>();
                var queue = new Queue<int>();
                queue.Enqueue(id);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    foreach (var child in byParent[current])
                    {
                        descendants.Add(child.Id);
                        queue.Enqueue(child.Id);
                    }
                }
                return descendants;
            }

            // الإدارات العامة الثلاث (تجارية/تنموية/خدمات) تتكرر بنفس الاسم تحت "تأجير المباني"
            // (11) و"تأجير المرافق" (12) و"ريع الأراضي" (13) — تُجمع هنا بالاسم في إدارة واحدة
            var leaseParentIds = new[] { 11, 12, 13 };
            var deptSections = allSections
                .Where(s => s.ParentSectionId.HasValue && leaseParentIds.Contains(s.ParentSectionId.Value))
                .ToList();

            var deptGroups = deptSections
                .GroupBy(d => d.NameAr)
                .Select(g => new { Name = g.Key, SectionIds = g.Select(x => x.Id).ToList() })
                .ToList();

            var details = new List<DeptBreakdownDetail>();
            foreach (var dept in deptGroups)
            {
                var deptAllIds = new List<int>();
                foreach (var id in dept.SectionIds)
                {
                    deptAllIds.Add(id);
                    deptAllIds.AddRange(Descendants(id));
                }

                var deptRecords = records.Where(r => deptAllIds.Contains(r.SectionId)).ToList();
                var deptTotal = deptRecords.Sum(r => r.TotalValue ?? 0);
                if (deptTotal <= 0) continue;

                var deptCollected = deptRecords.Sum(r => r.CollectedValue ?? 0);
                var deptUncollected = deptRecords.Sum(r => r.UncollectedValue ?? (r.TotalValue ?? 0) - (r.CollectedValue ?? 0));

                // الإدارات الفرعية: أبناء نسخ الإدارة العامة الثلاث، مُجمّعة بالاسم أيضًا
                var subSections = allSections
                    .Where(s => s.ParentSectionId.HasValue && dept.SectionIds.Contains(s.ParentSectionId.Value))
                    .ToList();

                var subDetails = subSections
                    .GroupBy(s => s.NameAr)
                    .Select(g =>
                    {
                        var ids = g.Select(x => x.Id).ToHashSet();
                        return new { Name = g.Key, Total = records.Where(r => ids.Contains(r.SectionId)).Sum(r => r.TotalValue ?? 0) };
                    })
                    .Where(x => x.Total > 0)
                    .OrderByDescending(x => x.Total)
                    .ToList();

                details.Add(new DeptBreakdownDetail
                {
                    Name = dept.Name,
                    Total = deptTotal,
                    Collected = deptCollected,
                    Uncollected = deptUncollected,
                    SubLabels = subDetails.Select(x => x.Name).ToList(),
                    SubValues = subDetails.Select(x => x.Total).ToList()
                });
            }

            return details.OrderByDescending(d => d.Total).ToList();
        }

        /// <summary>
        /// يجمّع بيانات عدة نطاقات معاً (الأمانة + البلدية) لعرض لوحة عامة شاملة
        /// دون الحاجة لأيقونة "الكل" المستقلة
        /// </summary>
        public async Task<SectionDashboardData> GetOverviewDataAsync(IEnumerable<ScopeType> scopes, string? quarter = null, string? contractNumber = null)
        {
            var scopeList = scopes.ToList();
            var sections = await _db.Sections
                .Where(s => scopeList.Contains(s.Scope) && s.IsActive)
                .Select(s => new { s.Id, s.Scope })
                .ToListAsync();
            var sectionIds = sections.Select(s => s.Id).ToList();

            var query = _db.DashboardRecords.Where(r => sectionIds.Contains(r.SectionId));
            var records = await ApplyFilters(query, quarter, contractNumber).ToListAsync();

            // مستوى "عام": الرسم يعرض البنود الأساسية فقط، ويتم دمج بيانات الأمانة
            // والبلدية تلقائياً إذا كان اسم البند الأساسي نفسه (التجميع هنا يتم حسب
            // اسم البند الأساسي وليس حسب معرّف القسم أو النطاق)
            var rootNameMap = await BuildRootNameMapAsync(sectionIds);

            // رسم الأعمدة الأول في لوحة "عام" يجب أن يعرض إجمالي الأمانة مقابل
            // إجمالي البلديات ككل فقط، دون تفصيل كل بلدية على حدة (بحسب الطلب)
            var scopeGroupMap = sections.ToDictionary(
                s => s.Id,
                s => s.Scope == ScopeType.Amanah ? "الأمانة" : "البلديات");

            return BuildResult(
                records,
                rootNameMap,
                entityGroupOverride: scopeGroupMap,
                entityChartTitle: "إجمالي الإيرادات الشهرية: الأمانة مقابل البلديات",
                entityChartBadge: "اضغط لعرض تفاصيل النطاق",
                subItemsRootFilter: "استثمار");
        }

        /// <summary>
        /// يبني خريطة (معرّف أي بند فرعي) ← (اسم البند الأساسي/تصنيف البند الذي يتبع له)
        /// عبر تتبّع سلسلة الأب (ParentSectionId) حتى الجذر. تُستخدم لتجميع الرسوم على
        /// مستوى "البنود الأساسية" بدل "البنود الفرعية" في لوحتي عام/الأمانة/البلدية.
        /// </summary>
        private async Task<Dictionary<int, string>> BuildRootNameMapAsync(IEnumerable<int> sectionIds)
        {
            var allSections = await _db.Sections
                .Select(s => new { s.Id, s.ParentSectionId, s.NameAr })
                .ToListAsync();

            var byId = allSections.ToDictionary(s => s.Id, s => s);
            var rootNameCache = new Dictionary<int, string>();

            string ResolveRootName(int startId)
            {
                if (rootNameCache.TryGetValue(startId, out var cached)) return cached;

                var current = startId;
                var visited = new HashSet<int>();
                while (byId.TryGetValue(current, out var node) && node.ParentSectionId.HasValue && visited.Add(current))
                {
                    current = node.ParentSectionId.Value;
                }

                var rootName = byId.TryGetValue(current, out var rootNode) ? rootNode.NameAr : "غير محدد";
                rootNameCache[startId] = rootName;
                return rootName;
            }

            var map = new Dictionary<int, string>();
            foreach (var id in sectionIds.Distinct())
            {
                if (byId.ContainsKey(id))
                    map[id] = ResolveRootName(id);
            }
            return map;
        }

        /// <summary>
        /// منطق التجميع المشترك (KPIs + الرسوم البيانية) المستخدم في كل أنواع اللوحات
        /// (بند واحد / نطاق كامل / لوحة عامة)
        /// </summary>
        private static SectionDashboardData BuildResult(
            List<DashboardRecord> records,
            Dictionary<int, string>? groupByRootName = null,
            Dictionary<int, string>? entityGroupOverride = null,
            string? entityChartTitle = null,
            string? entityChartBadge = null,
            string? subItemsRootFilter = null)
        {

            var result = new SectionDashboardData { HasData = records.Count > 0 };
            if (!result.HasData) return result;

            result.TotalRevenue = records.Sum(r => r.TotalValue ?? 0);
            result.CollectedRevenue = records.Sum(r => r.CollectedValue ?? 0);
            result.UncollectedRevenue = records.Sum(r => r.UncollectedValue ?? (r.TotalValue ?? 0) - (r.CollectedValue ?? 0));
            result.CollectionRatePercent = result.TotalRevenue > 0
                ? Math.Round(result.CollectedRevenue / result.TotalRevenue * 100, 1)
                : 0;

            var targetSum = records.Sum(r => r.TargetValue ?? 0);
            result.TargetAchievementPercent = targetSum > 0
                ? Math.Round(result.CollectedRevenue / targetSum * 100, 1)
                : result.CollectionRatePercent;

            // groupByRootName != null  ➜  التجميع على مستوى "البند الأساسي" (عام / الأمانة / البلدية)
            //                              ويتم دمج الأمانة والبلدية تلقائياً لأن المفتاح هو الاسم فقط
            // groupByRootName == null  ➜  التجميع على مستوى "البند الفرعي" (داخل بند أساسي محدد)، كالسابق
            var byCategory = records
                .Where(r => groupByRootName != null
                    ? groupByRootName.ContainsKey(r.SectionId)
                    : !string.IsNullOrEmpty(r.Category))
                .GroupBy(r => groupByRootName != null ? groupByRootName[r.SectionId] : r.Category!)
                .Select(g =>
                {
                    var collected = g.Sum(x => x.CollectedValue ?? 0);
                    var uncollected = g.Sum(x => x.UncollectedValue ?? (x.TotalValue ?? 0) - (x.CollectedValue ?? 0));
                    var total = g.Sum(x => x.TotalValue ?? 0);
                    var target = g.Sum(x => x.TargetValue ?? 0);

                    var categoryTrend = g.Where(x => x.RecordDate.HasValue)
                        .GroupBy(x => new { x.RecordDate!.Value.Year, x.RecordDate!.Value.Month })
                        .Select(tg => new
                        {
                            Label = $"{tg.Key.Year}-{tg.Key.Month:D2}",
                            Total = tg.Sum(x => x.TotalValue ?? 0),
                            Collected = tg.Sum(x => x.CollectedValue ?? 0),
                            Uncollected = tg.Sum(x => x.UncollectedValue ?? (x.TotalValue ?? 0) - (x.CollectedValue ?? 0))
                        })
                        .OrderBy(x => x.Label)
                        .ToList();

                    return new CategoryDetail
                    {
                        Label = g.Key,
                        Collected = collected,
                        Uncollected = uncollected,
                        Total = total,
                        Target = target,
                        TrendLabels = categoryTrend.Select(x => x.Label).ToList(),
                        TrendValues = categoryTrend.Select(x => x.Total).ToList(),
                        TrendCollected = categoryTrend.Select(x => x.Collected).ToList(),
                        TrendUncollected = categoryTrend.Select(x => x.Uncollected).ToList()
                    };
                })
                .OrderByDescending(x => x.Collected + x.Uncollected)
                .Take(15)
                .ToList();

            result.CategoryLabels = byCategory.Select(x => x.Label).ToList();
            result.CategoryCollected = byCategory.Select(x => x.Collected).ToList();
            result.CategoryUncollected = byCategory.Select(x => x.Uncollected).ToList();
            result.CategoryDetails = byCategory;

            // توزيع الإيرادات حسب البنود الفرعية (عمود "الفئة/الصنف" الخام لكل سجل، بصرف
            // النظر عن تجميع البند الأساسي) — أدق من CategoryLabels بلوحة "عام".
            // نعرض فقط أعلى البنود الفرعية حسب الإيراد، ونجمع الباقي (إن وُجد) تحت "أخرى"
            // حتى لا يزدحم الرسم بعشرات البنود الصغيرة.
            // subItemsRootFilter (لوحة "عام"/نطاق كامل فقط): يقتصر هذا الرسم تحديدًا على بند
            // رئيسي واحد (افتراضيًا "استثمار") بدل خلط كل البنود الرئيسية معًا في نفس الرسم -
            // باقي الرسوم (CategoryLabels، TopItemLabels...) تبقى تعكس كل البيانات كالمعتاد
            var subItemsSource = (subItemsRootFilter != null && groupByRootName != null)
                ? records.Where(r => groupByRootName.TryGetValue(r.SectionId, out var rn) && rn == subItemsRootFilter).ToList()
                : records;

            const int topSubItemCount = 6;
            var subItemGroups = subItemsSource
                .Where(r => !string.IsNullOrWhiteSpace(r.Category))
                .GroupBy(r => r.Category!.Trim())
                .Select(g => new { Label = g.Key, Total = g.Sum(x => x.TotalValue ?? 0) })
                .Where(x => x.Total > 0)
                .OrderByDescending(x => x.Total)
                .ToList();

            var topSubItems = subItemGroups.Take(topSubItemCount).ToList();
            var otherSubItemsTotal = subItemGroups.Skip(topSubItemCount).Sum(x => x.Total);

            result.SubItemLabels = topSubItems.Select(x => x.Label).ToList();
            result.SubItemValues = topSubItems.Select(x => x.Total).ToList();
            if (otherSubItemsTotal > 0)
            {
                result.SubItemLabels.Add("أخرى");
                result.SubItemValues.Add(otherSubItemsTotal);
            }

            // أعلى بنود الإيرادات: تُبنى دائمًا من عمود "تصنيف البند" الخام (Classification) —
            // يضمن عرض البنود الرئيسية فقط. عند التصفّح لبند رئيسي محدد، كل سجلاته تحمل نفس
            // Classification (اسم ذلك البند)، فيظهر هذا الرسم شريطًا واحدًا فقط باسم البند
            // المختار (يعكس التصفية دون تبديل المستوى المعروض)؛ توزيع البنود الفرعية تحديدًا
            // مسؤولية رسم "توزيع الإيرادات حسب البنود الفرعية" أدناه (عمود Category)
            var byTopItem = records
                .Where(r => !string.IsNullOrWhiteSpace(r.Classification))
                .GroupBy(r => r.Classification!.Trim())
                .Select(g => new { Label = g.Key, Total = g.Sum(x => x.TotalValue ?? 0) })
                .Where(x => x.Total > 0)
                .OrderByDescending(x => x.Total)
                .Take(8)
                .ToList();
            result.TopItemLabels = byTopItem.Select(x => x.Label).ToList();
            result.TopItemValues = byTopItem.Select(x => x.Total).ToList();

            // توزيع الإيرادات حسب الربع (Q1..Q4) بالاعتماد على عمود "الربع"
            var quarterTotals = records
                .Where(r => !string.IsNullOrWhiteSpace(r.Quarter))
                .GroupBy(r => r.Quarter!.Trim().ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalValue ?? 0));
            var quarterDefs = new (string Key, string Label)[]
            {
                ("Q1", "Q1"), ("Q2", "Q2"), ("Q3", "Q3"), ("Q4", "Q4")
            };
            foreach (var q in quarterDefs)
            {
                result.QuarterLabels.Add(q.Label);
                result.QuarterValues.Add(quarterTotals.TryGetValue(q.Key, out var qv) ? qv : 0m);
            }

            // تسميات محور الاتجاه الشهري تُعرض كاسم الشهر العربي فقط (بدون سنة أو رقم شهر)،
            // بينما يبقى الترتيب الزمني الصحيح معتمدًا على مفتاح الفرز الداخلي "yyyy-MM"
            // (نفس أسلوب EntityMonthLabels أدناه) — مبني على عمود "تاريخ الدفع" (RecordDate)
            var byDate = records
                .Where(r => r.RecordDate.HasValue)
                .GroupBy(r => new { r.RecordDate!.Value.Year, r.RecordDate!.Value.Month })
                .Select(g => new
                {
                    SortKey = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Label = ArabicMonthNames[g.Key.Month - 1],
                    Total = g.Sum(x => x.TotalValue ?? 0),
                    Collected = g.Sum(x => x.CollectedValue ?? 0),
                    Uncollected = g.Sum(x => x.UncollectedValue ?? (x.TotalValue ?? 0) - (x.CollectedValue ?? 0))
                })
                .OrderBy(x => x.SortKey)
                .ToList();

            result.TrendLabels = byDate.Select(x => x.Label).ToList();
            result.TrendValues = byDate.Select(x => x.Total).ToList();
            result.TrendCollected = byDate.Select(x => x.Collected).ToList();
            result.TrendUncollected = byDate.Select(x => x.Uncollected).ToList();

            // الإيرادات الشهرية لكل جهة (أمانة المدينة المنورة نفسها + كل بلدية) بالاعتماد
            // على عمود "الجهة" وعمود "تاريخ الدفع" — لرسم الأعمدة الأول أعلى اللوحة
            var byEntity = records
                .Where(r => entityGroupOverride != null
                    ? entityGroupOverride.ContainsKey(r.SectionId)
                    : !string.IsNullOrWhiteSpace(r.Entity))
                .Where(r => r.RecordDate.HasValue)
                .GroupBy(r => entityGroupOverride != null ? entityGroupOverride[r.SectionId] : r.Entity!)
                .Select(g => new
                {
                    Entity = g.Key,
                    Total = g.Sum(x => x.TotalValue ?? 0),
                    Monthly = g.GroupBy(x => new { x.RecordDate!.Value.Year, x.RecordDate!.Value.Month })
                        .Select(mg => new
                        {
                            Label = $"{mg.Key.Year}-{mg.Key.Month:D2}",
                            Total = mg.Sum(x => x.TotalValue ?? 0)
                        })
                        .ToList()
                })
                .OrderByDescending(x => x.Total)
                .Take(20) // سقف يمنع ازدحام الرسم إن وُجدت جهات كثيرة جداً بالبيانات
                .ToList();

            var entityMonths = byEntity
                .SelectMany(e => e.Monthly.Select(m => m.Label))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            // تسميات محور الأشهر تُعرض كاسم الشهر العربي فقط (بدون سنة أو رقم شهر)،
            // بينما يبقى الترتيب الزمني الصحيح معتمدًا على المفتاح الداخلي "yyyy-MM"
            result.EntityMonthLabels = entityMonths
                .Select(m => ArabicMonthNames[int.Parse(m.Substring(5, 2)) - 1])
                .ToList();
            result.EntityMonthlySeries = byEntity
                .Select(e =>
                {
                    var monthlyMap = e.Monthly.ToDictionary(m => m.Label, m => m.Total);
                    return new EntityMonthlySeries
                    {
                        Entity = e.Entity,
                        Values = entityMonths.Select(m => monthlyMap.TryGetValue(m, out var v) ? v : 0m).ToList()
                    };
                })
                .ToList();

            result.EntityChartIsAggregated = entityGroupOverride != null;
            result.EntityChartTitle = entityChartTitle ?? result.EntityChartTitle;
            result.EntityChartBadge = entityChartBadge ?? result.EntityChartBadge;

            return result;
        }

        /// <summary>
        /// أسماء الأشهر العربية بترتيب السنة الميلادية (تُستخدم كتسميات محور X بصفحة
        /// "مقارنة المستهدفات" بدل صيغة "2026-01" الفنية)
        /// </summary>
        private static readonly string[] ArabicMonthNames =
        {
            "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
            "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"
        };

        /// <summary>
        /// مطابقة اسم "الأمانة" بين مصدرين مختلفين للنص (DashboardRecords.Entity المستورَد من
        /// Book112.xlsx يكتبها بلا همزة "امانة منطقة المدينة المنورة"، بينما ملف المستهدف
        /// الشهري يكتبها بهمزة "الأمانة") - المقارنة الحرفية ".Contains("أمانة")" تفشل دائمًا
        /// لأن الألف بهمزة (أ) والألف العادية (ا) حرفان مختلفان بيونيكود. نُطبّع النص أولاً
        /// عبر DataImportService.Normalize (تُوحّد أ/إ/آ إلى ا) المستخدَمة أصلاً بالمشروع لنفس الغرض.
        /// </summary>
        private static bool IsAmanah(string? name) =>
            !string.IsNullOrEmpty(name) && DataImportService.Normalize(name).Contains("امانه");

        public async Task<TargetsComparisonData> GetTargetsComparisonDataAsync(string? entity = null)
        {
            IQueryable<DashboardRecord> query = _db.DashboardRecords;
            if (!string.IsNullOrWhiteSpace(entity))
                query = query.Where(r => r.Entity == entity);

            var records = await query
                .Select(r => new
                {
                    r.Entity,
                    r.RevenueType,
                    r.Classification,
                    r.TargetFinanceValue,
                    r.TargetBaladiyahValue,
                    r.CollectedValue,
                    r.TotalValue,
                    r.RecordDate,
                    r.CreatedDate,
                    r.Quarter
                })
                .ToListAsync();

            var result = new TargetsComparisonData { HasData = records.Count > 0 };
            if (!result.HasData) return result;

            // كل جهة تملك بيانات (لقائمة التصفية بالواجهة) - نفس الترتيب المعتمد بالقائمة
            // الجانبية (الأمانة أولاً ثم البلديات بترتيبها الرسمي) بدل ترتيب أبجدي بسيط
            result.Entities = EntityOrderHelper.SortEntities(
                records.Where(r => !string.IsNullOrWhiteSpace(r.Entity)).Select(r => r.Entity!));

            // المستهدف السنوي مُكرَّر بنفس القيمة لكل صف تابع لنفس (الجهة + نوع الإيراد)،
            // لذا نجمعه مرة واحدة فقط لكل تركيبة فريدة حتى لا يتضاعف الإجمالي بعدد الصفوف
            var targetCombos = records
                .Where(r => !string.IsNullOrWhiteSpace(r.Entity) && !string.IsNullOrWhiteSpace(r.RevenueType))
                .GroupBy(r => new { Entity = r.Entity!, RevenueType = r.RevenueType! })
                .Select(g => new TargetBreakdownRow
                {
                    Entity = g.Key.Entity,
                    RevenueType = g.Key.RevenueType,
                    TargetFinance = g.FirstOrDefault(x => x.TargetFinanceValue.HasValue)?.TargetFinanceValue ?? 0,
                    TargetBaladiyah = g.FirstOrDefault(x => x.TargetBaladiyahValue.HasValue)?.TargetBaladiyahValue ?? 0
                })
                .OrderBy(x => x.Entity).ThenBy(x => x.RevenueType)
                .ToList();

            result.TargetsBreakdown = targetCombos;
            result.FinanceTargetTotal = targetCombos.Sum(x => x.TargetFinance);
            result.BaladiyahTargetTotal = targetCombos.Sum(x => x.TargetBaladiyah);

            // إجمالي المحصّل والمفوتر (بصرف النظر عن وجود تاريخ) - تُستخدم لحساب نسبة التحقق الكلية
            result.FinanceCollectedTotal = records.Sum(r => r.CollectedValue ?? 0);
            result.BaladiyahInvoicedTotal = records.Sum(r => r.TotalValue ?? 0);

            result.FinanceAchievementPercent = result.FinanceTargetTotal > 0
                ? Math.Round(result.FinanceCollectedTotal / result.FinanceTargetTotal * 100, 1)
                : 0;
            result.BaladiyahAchievementPercent = result.BaladiyahTargetTotal > 0
                ? Math.Round(result.BaladiyahInvoicedTotal / result.BaladiyahTargetTotal * 100, 1)
                : 0;

            // السنة المعتمدة لمحور الأشهر (الأكثر تكرارًا بين تواريخ الدفع والإنشاء معًا)
            var yearCounts = records
                .SelectMany(r => new[] { r.RecordDate?.Year, r.CreatedDate?.Year })
                .Where(y => y.HasValue)
                .GroupBy(y => y!.Value)
                .OrderByDescending(g => g.Count())
                .ToList();
            int year = yearCounts.Any() ? yearCounts.First().Key : DateTime.Now.Year;

            result.MonthLabels = ArabicMonthNames.ToList();

            // المحصّل الشهري (حسب "تاريخ الدفع") — يُراكَم شهرًا بعد شهر لمقارنته بخط المستهدف التراكمي
            var collectedByMonth = records
                .Where(r => r.RecordDate.HasValue && r.RecordDate.Value.Year == year)
                .GroupBy(r => r.RecordDate!.Value.Month)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.CollectedValue ?? 0));

            // المفوتر الشهري (حسب "تاريخ الإنشاء") — إجمالي القيمة لكل الفواتير سواء حُصِّلت أم لا
            var invoicedByMonth = records
                .Where(r => r.CreatedDate.HasValue && r.CreatedDate.Value.Year == year)
                .GroupBy(r => r.CreatedDate!.Value.Month)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalValue ?? 0));

            // المستهدف الشهري الحقيقي للبلديات (من ملف "المستهدف الشهري" المرفوع) - إن وُجد،
            // نجمع قيمة كل شهر (مُصفّاة بنفس الجهة إن حُدِّدت) بدل افتراض توزيع خطي متساوٍ.
            // نجلب كل الصفوف أولاً (جدول صغير) ثم نُصفّي بالذاكرة لأن IsAmanah (تطبيع نصي) لا
            // يمكن ترجمته إلى SQL عبر EF Core.
            var allTargetRowsRaw = await _db.MonthlyBaladiyahTargets.ToListAsync();
            var monthlyRows = string.IsNullOrWhiteSpace(entity)
                ? allTargetRowsRaw
                : (IsAmanah(entity)
                    // مطابقة مرنة للأمانة: قد يختلف الاسم الكامل بين ملف الإيرادات ("أمانة
                    // منطقة المدينة المنورة" مثلاً) وملف المستهدف الشهري ("الأمانة")
                    ? allTargetRowsRaw.Where(t => IsAmanah(t.Entity)).ToList()
                    : allTargetRowsRaw.Where(t => t.Entity == entity).ToList());
            decimal[]? baladiyahMonthlyActual = null;
            if (monthlyRows.Count > 0)
            {
                baladiyahMonthlyActual = new decimal[12];
                foreach (var t in monthlyRows)
                {
                    baladiyahMonthlyActual[0] += t.M1; baladiyahMonthlyActual[1] += t.M2;
                    baladiyahMonthlyActual[2] += t.M3; baladiyahMonthlyActual[3] += t.M4;
                    baladiyahMonthlyActual[4] += t.M5; baladiyahMonthlyActual[5] += t.M6;
                    baladiyahMonthlyActual[6] += t.M7; baladiyahMonthlyActual[7] += t.M8;
                    baladiyahMonthlyActual[8] += t.M9; baladiyahMonthlyActual[9] += t.M10;
                    baladiyahMonthlyActual[10] += t.M11; baladiyahMonthlyActual[11] += t.M12;
                }
                result.BaladiyahTargetIsActualMonthly = true;

                // المستهدف السنوي - وزارة البلديات والإسكان: بمجرّد رفع ملف "المستهدف الشهري"
                // يُصبح المستهدف السنوي دائمًا = مجموع الأشهر الاثني عشر من نفس الملف (بدل
                // العمود السنوي القديم بملف الإيرادات) حتى تتطابق بطاقة النسبة مع الرسوم
                // والجدول أسفل الصفحة تمامًا لنفس تصفية الجهة المختارة.
                result.BaladiyahTargetTotal = baladiyahMonthlyActual.Sum();
                result.BaladiyahAchievementPercent = result.BaladiyahTargetTotal > 0
                    ? Math.Round(result.BaladiyahInvoicedTotal / result.BaladiyahTargetTotal * 100, 1)
                    : 0;
            }

            // ==================================================================
            // المستهدف السنوي مقابل المفوتر لكل جهة (الأمانة + كل بلدية) معًا - بصرف النظر عن
            // تصفية الجهة الحالية - لتغذية رسم/جدول "المستهدف السنوي مقابل المفوتر حسب الجهة"
            // أعلى الصفحة. المستهدف لكل جهة = مجموع أشهرها الاثني عشر بملف "المستهدف الشهري".
            // ==================================================================
            var allMonthlyRows = allTargetRowsRaw;
            if (allMonthlyRows.Count > 0)
            {
                var invoicedByEntity = await _db.DashboardRecords
                    .Where(r => r.Entity != null && r.Entity != "")
                    .GroupBy(r => r.Entity!)
                    .Select(g => new { Entity = g.Key, Invoiced = g.Sum(x => x.TotalValue ?? 0) })
                    .ToListAsync();

                decimal InvoicedFor(string entityName) => IsAmanah(entityName)
                    // مطابقة مرنة للأمانة (اسم كامل بملف الإيرادات مقابل "الأمانة" بملف المستهدف)
                    ? invoicedByEntity.Where(x => IsAmanah(x.Entity)).Sum(x => x.Invoiced)
                    : invoicedByEntity.Where(x => x.Entity == entityName).Sum(x => x.Invoiced);

                var annualByEntity = allMonthlyRows
                    .GroupBy(t => t.Entity)
                    .Select(g => new
                    {
                        Entity = g.Key,
                        Annual = g.Sum(t => t.M1 + t.M2 + t.M3 + t.M4 + t.M5 + t.M6 + t.M7 + t.M8 + t.M9 + t.M10 + t.M11 + t.M12)
                    })
                    .ToList();

                decimal AnnualFor(string entityName) => IsAmanah(entityName)
                    ? annualByEntity.Where(x => IsAmanah(x.Entity)).Sum(x => x.Annual)
                    : annualByEntity.Where(x => x.Entity == entityName).Sum(x => x.Annual);

                // اسم الأمانة "الرسمي" لعرضه بالجدول/الرسم هو نفس الاسم المستخدم بقائمة تصفية
                // الجهات (من ملف الإيرادات، مثال: "أمانة منطقة المدينة المنورة")، لا الاسم
                // المختصر "الأمانة" بملف المستهدف الشهري - وإلا ظهرت الأمانة كصفَّين مكرَّرين
                var amanahCanonicalName =
                    result.Entities.FirstOrDefault(IsAmanah) ??
                    annualByEntity.Select(x => x.Entity).FirstOrDefault(IsAmanah) ??
                    "الأمانة";

                var nonAmanahNames = annualByEntity.Select(x => x.Entity).Where(e => !IsAmanah(e))
                    .Concat(result.Entities.Where(e => !IsAmanah(e)));

                var allEntityNames = EntityOrderHelper.SortEntities(nonAmanahNames.Append(amanahCanonicalName));

                result.EntityAnnualTargets = allEntityNames.Select(name =>
                {
                    var annual = AnnualFor(name);
                    var invoiced = InvoicedFor(name);
                    return new EntityAnnualRow
                    {
                        Entity = name,
                        AnnualTarget = annual,
                        InvoicedTotal = invoiced,
                        AchievementPercent = annual > 0 ? Math.Round(invoiced / annual * 100, 1) : 0
                    };
                }).ToList();
            }

            decimal collectedRunning = 0, invoicedRunning = 0;
            for (int m = 1; m <= 12; m++)
            {
                var cv = collectedByMonth.TryGetValue(m, out var cvVal) ? cvVal : 0;
                var iv = invoicedByMonth.TryGetValue(m, out var ivVal) ? ivVal : 0;

                collectedRunning += cv;
                invoicedRunning += iv;

                // القيمة الشهرية الفعلية (غير التراكمية) — تُستخدم كأعمدة برسم الأداء الشهري
                result.FinanceCollectedMonthly.Add(cv);
                result.BaladiyahInvoicedMonthly.Add(iv);

                result.FinanceCollectedCumulative.Add(collectedRunning);
                result.BaladiyahInvoicedCumulative.Add(invoicedRunning);

                // خط "المستهدف الشهري": قيمة كل شهر بمفرده (غير تراكمية) لمقارنتها مباشرة بعمود
                // الأداء الشهري بنفس الرسم. المالية: توزيع تقديري (السنوي ÷ 12) لعدم توفر ملف
                // شهري خاص بها بعد. البلديات: القيم الفعلية من ملف "المستهدف الشهري" إن رُفع.
                result.FinanceTargetLine.Add(Math.Round(result.FinanceTargetTotal / 12m, 2));
                result.BaladiyahTargetLine.Add(
                    baladiyahMonthlyActual != null
                        ? Math.Round(baladiyahMonthlyActual[m - 1], 2)
                        : Math.Round(result.BaladiyahTargetTotal / 12m, 2));
            }

            // ==================================================================
            // تحقيق المستهدف حسب البنود والأرباع: البنود هنا = قيم عمود "تصنيف البند" كما
            // هي بالملف (استثمار، رسوم، جزاءات...). لكل بند نجمع المحصّل/المفوتر في كل ربع
            // (Q1..Q4)، والمستهدف السنوي = مجموع مستهدفات كل "أنواع الإيراد" التابعة لهذا
            // البند (نفس المستهدفات المعروضة بالجدول أسفل الصفحة، مجمّعة حسب البند الأعلى)
            // ==================================================================
            var quarterKeys = new[] { "Q1", "Q2", "Q3", "Q4" };

            // خريطة "نوع الإيراد" → "تصنيف البند" (كل نوع إيراد ينتمي لبند واحد بالملف)
            var revenueTypeToItem = records
                .Where(r => !string.IsNullOrWhiteSpace(r.RevenueType) && !string.IsNullOrWhiteSpace(r.Classification))
                .GroupBy(r => r.RevenueType!)
                .ToDictionary(g => g.Key, g => g.First().Classification!.Trim());

            var itemTargets = targetCombos
                .Where(x => revenueTypeToItem.ContainsKey(x.RevenueType))
                .GroupBy(x => revenueTypeToItem[x.RevenueType])
                .ToDictionary(
                    g => g.Key,
                    g => new { TargetFinance = g.Sum(x => x.TargetFinance), TargetBaladiyah = g.Sum(x => x.TargetBaladiyah) });

            var byItemQuarter = records
                .Where(r => !string.IsNullOrWhiteSpace(r.Classification) && !string.IsNullOrWhiteSpace(r.Quarter))
                .GroupBy(r => new { Item = r.Classification!.Trim(), Quarter = r.Quarter!.Trim().ToUpperInvariant() })
                .ToDictionary(
                    g => g.Key,
                    g => new { Collected = g.Sum(x => x.CollectedValue ?? 0), Invoiced = g.Sum(x => x.TotalValue ?? 0) });

            // كل البنود الموجودة فعليًا بالبيانات (سواء كان لها مستهدف أم لا)
            var allItems = records
                .Where(r => !string.IsNullOrWhiteSpace(r.Classification))
                .Select(r => r.Classification!.Trim())
                .Distinct()
                .ToList();

            var itemOrder = allItems
                .OrderByDescending(item => itemTargets.TryGetValue(item, out var t) ? (t.TargetFinance + t.TargetBaladiyah) : 0)
                .ToList();

            // تجميع المستهدف الشهري الفعلي (من monthlyRows المستوردة أعلاه، مُصفّاة بنفس الجهة
            // إن حُدِّدت) على مستوى "تصنيف البند" بربط "نوع الإيراد" بنفس خريطة revenueTypeToItem
            var monthlyByItem = new Dictionary<string, decimal[]>();
            foreach (var t in monthlyRows)
            {
                if (!revenueTypeToItem.TryGetValue(t.RevenueType, out var item)) continue;
                if (!monthlyByItem.TryGetValue(item, out var arr))
                {
                    arr = new decimal[12];
                    monthlyByItem[item] = arr;
                }
                arr[0] += t.M1; arr[1] += t.M2; arr[2] += t.M3; arr[3] += t.M4;
                arr[4] += t.M5; arr[5] += t.M6; arr[6] += t.M7; arr[7] += t.M8;
                arr[8] += t.M9; arr[9] += t.M10; arr[10] += t.M11; arr[11] += t.M12;
            }

            foreach (var item in itemOrder)
            {
                var row = new ItemQuarterlyRow { ItemLabel = item };
                if (itemTargets.TryGetValue(item, out var tgt))
                {
                    row.TargetFinance = tgt.TargetFinance;
                    row.TargetBaladiyah = tgt.TargetBaladiyah;
                }

                var hasMonthlyForItem = monthlyByItem.TryGetValue(item, out var monthlyArr);
                for (int qi = 0; qi < 4; qi++)
                {
                    var q = quarterKeys[qi];
                    byItemQuarter.TryGetValue(new { Item = item, Quarter = q }, out var vals);
                    row.FinanceCollectedByQuarter.Add(vals?.Collected ?? 0);
                    row.BaladiyahInvoicedByQuarter.Add(vals?.Invoiced ?? 0);

                    var monthsThrough = 3 * (qi + 1); // 3, 6, 9, 12

                    // المالية: تقدير خطي تراكمي (لا يوجد ملف مستهدف شهري خاص بالمالية بعد)
                    row.TargetFinanceByQuarter.Add(Math.Round(row.TargetFinance * monthsThrough / 12m, 2));

                    // البلديات: مجموع الأشهر الفعلية من 1 حتى نهاية هذا الربع إن توفرت بالملف
                    // المرفوع لهذا البند، وإلا نفس التقدير الخطي التراكمي
                    row.TargetBaladiyahByQuarter.Add(
                        hasMonthlyForItem
                            ? Math.Round(monthlyArr!.Take(monthsThrough).Sum(), 2)
                            : Math.Round(row.TargetBaladiyah * monthsThrough / 12m, 2));
                }

                result.ItemsQuarterly.Add(row);
            }

            // ==================================================================
            // (مستهدفات البلديات والإسكان فقط) رسمة "البنود": المستهدف السنوي مقابل المفوتر لكل
            // بند رئيسي، من عمود "البند الرئيسي" بملف "المستهدف الشهري" مباشرة (monthlyRows،
            // مُصفّاة أصلاً بنفس الجهة أعلاه) - لا علاقة لها بـ RevenueType/Classification لأن
            // Book112.xlsx لا يحوي عمود "نوع الإيراد" فتبقى تلك الخريطة فارغة دومًا للبيانات
            // الحالية. المفوتر (records، مُصفّاة أصلاً بنفس الجهة) يُطابَق عبر تعيين ثابت صغير
            // من "البند الرئيسي" إلى قيم "تصنيف البند" الموازية له بـ Book112.xlsx.
            // ==================================================================
            var mainItemToClassifications = new Dictionary<string, string[]>
            {
                ["إيرادات استثمارات العقارات البلدية"] = new[] { "استثمار", "تأجير مؤقت" },
                ["إيرادات الرسوم والمقابلات المالية"] = new[] { "رسوم" },
                ["إيرادات رسم إشغال مرافق الإيواء"] = new[] { "رسوم المبيعات-الايواء" },
                ["إيرادات الجزاءات والغرمات"] = new[] { "جزاءات" },
                ["إيرادات رسوم تقديم منتجات التبغ"] = new[] { "رسوم المبيعات-التبغ" },
                ["الإيرادات المختلفة"] = new[] { "إيرادات مختلفة" },
            };

            var annualTargetByMainItem = monthlyRows
                .Where(t => !string.IsNullOrWhiteSpace(t.MainItem))
                .GroupBy(t => t.MainItem!)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.M1 + t.M2 + t.M3 + t.M4 + t.M5 + t.M6 + t.M7 + t.M8 + t.M9 + t.M10 + t.M11 + t.M12));

            result.ItemsMainAnnual = mainItemToClassifications.Select(kv =>
            {
                var annual = annualTargetByMainItem.TryGetValue(kv.Key, out var a) ? a : 0;
                var invoiced = records.Where(r => !string.IsNullOrWhiteSpace(r.Classification) && kv.Value.Contains(r.Classification!.Trim()))
                    .Sum(r => r.TotalValue ?? 0);
                return new EntityAnnualRow
                {
                    Entity = kv.Key,
                    AnnualTarget = annual,
                    InvoicedTotal = invoiced,
                    AchievementPercent = annual > 0 ? Math.Round(invoiced / annual * 100, 1) : 0
                };
            })
            .OrderByDescending(x => x.AnnualTarget)
            .ToList();

            return result;
        }

        // خريطة "البند الرئيسي" كما بملف "مستهدف المالية" ← قيم "تصنيف البند" الموازية له
        // بـ Book112.xlsx (نفس فكرة الخريطة أعلاه لصفحة البلديات، لكن بنص مختلف قليلاً لأن
        // ملف مستهدف المالية يستخدم صياغة عربية مختلفة عن ملف المستهدف الشهري للبلديات)
        private static readonly Dictionary<string, string[]> FinanceMainItemToClassifications = new()
        {
            ["إيرادات استثمار العقارات البلدية"] = new[] { "استثمار", "تأجير مؤقت" },
            ["إيراد الجزاءات والغرامات"] = new[] { "جزاءات" },
            ["إيراد الرسوم والمقابلات المالية الأخرى"] = new[] { "رسوم" },
            ["إشغال الفنادق والشقق المفروشة والمنتجعات السياحية (الإيواء)"] = new[] { "رسوم المبيعات-الايواء" },
            ["رسم تقديم منتجات التبغ"] = new[] { "رسوم المبيعات-التبغ" },
            ["الإيرادات المختلفة"] = new[] { "إيرادات مختلفة" },
        };

        public async Task<FinanceTargetsPageData> GetFinanceTargetsPageDataAsync()
        {
            var targetRows = await _db.FinanceItemTargets.ToListAsync();
            var result = new FinanceTargetsPageData { HasData = targetRows.Count > 0 };
            if (!result.HasData) return result;

            var records = await _db.DashboardRecords
                .Select(r => new { r.Entity, r.Classification, r.CollectedValue })
                .ToListAsync();

            var amanahRecords = records.Where(r => IsAmanah(r.Entity)).ToList();
            var baladiyahRecords = records.Where(r => !IsAmanah(r.Entity)).ToList();

            var amanahTargetByMainItem = targetRows.Where(t => t.Scope == "Amanah")
                .GroupBy(t => t.MainItem).ToDictionary(g => g.Key, g => g.Sum(t => t.AnnualTarget));
            var baladiyahTargetByMainItem = targetRows.Where(t => t.Scope == "Baladiyah")
                .GroupBy(t => t.MainItem).ToDictionary(g => g.Key, g => g.Sum(t => t.AnnualTarget));

            List<FinanceTargetItemRow> BuildItems(
                IEnumerable<(string? Classification, decimal? CollectedValue)> recs,
                Dictionary<string, decimal> targetByMainItem)
            {
                var recsList = recs.ToList();
                return FinanceMainItemToClassifications.Select(kv =>
                {
                    var target = targetByMainItem.TryGetValue(kv.Key, out var t) ? t : 0m;
                    var collected = recsList
                        .Where(r => r.Classification != null && kv.Value.Contains(r.Classification!.Trim()))
                        .Sum(r => r.CollectedValue ?? 0m);
                    return new FinanceTargetItemRow
                    {
                        ItemLabel = kv.Key,
                        Collected = collected,
                        Target = target,
                        AchievementPercent = target > 0 ? Math.Round(collected / target * 100, 1) : 0
                    };
                })
                .OrderByDescending(x => x.Target)
                .ToList();
            }

            result.AmanahItems = BuildItems(amanahRecords.Select(r => (r.Classification, r.CollectedValue)), amanahTargetByMainItem);
            result.BaladiyahItems = BuildItems(baladiyahRecords.Select(r => (r.Classification, r.CollectedValue)), baladiyahTargetByMainItem);

            result.AmanahCollectedTotal = amanahRecords.Sum(r => r.CollectedValue ?? 0);
            result.AmanahTargetTotal = amanahTargetByMainItem.Values.Sum();
            result.AmanahAchievementPercent = result.AmanahTargetTotal > 0
                ? Math.Round(result.AmanahCollectedTotal / result.AmanahTargetTotal * 100, 1) : 0;

            result.BaladiyahCollectedTotal = baladiyahRecords.Sum(r => r.CollectedValue ?? 0);
            result.BaladiyahTargetTotal = baladiyahTargetByMainItem.Values.Sum();
            result.BaladiyahAchievementPercent = result.BaladiyahTargetTotal > 0
                ? Math.Round(result.BaladiyahCollectedTotal / result.BaladiyahTargetTotal * 100, 1) : 0;

            return result;
        }

        // حالة العقد التي تعني "مبرومة" بلغة صفحة الطرح
        private const string FinalizedStatus = "تم إبرام العقد";

        // الإدارة العامة الافتراضية للبنود الفرعية (الأنشطة) عند عدم اختيار إدارة من الفلتر
        private const string DefaultActivitiesDepartment = "الإدارة العامة لمحافظ الاستثمارات التجارية";

        public async Task<TarhPageData> GetTarhDataAsync(string? department, string? status, string? contractNumber, int? year, int? quarter)
        {
            var all = await _db.ContractRecords.ToListAsync();
            var result = new TarhPageData { HasData = all.Count > 0 };
            if (!result.HasData) return result;

            result.DepartmentOptions = all.Select(r => r.GeneralDepartment).Where(d => !string.IsNullOrWhiteSpace(d)).Distinct().OrderBy(d => d).ToList()!;
            result.StatusOptions = all.Select(r => r.Status).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s).ToList()!;
            result.YearOptions = all.Select(r => r.ContractYear).Where(y => y.HasValue).Select(y => y!.Value).Distinct().OrderByDescending(y => y).ToList();

            bool MatchesBase(ContractRecord r) =>
                (string.IsNullOrWhiteSpace(department) || r.GeneralDepartment == department) &&
                (string.IsNullOrWhiteSpace(contractNumber) || (r.ContractNumber ?? "").Contains(contractNumber, StringComparison.OrdinalIgnoreCase)) &&
                (!year.HasValue || r.ContractYear == year) &&
                (!quarter.HasValue || r.Quarter == quarter);

            // بلا فلتر الحالة - يُستخدم لخط الاتجاه الزمني الذي يفرض حالة "مبرومة" دائمًا بصرف
            // النظر عن فلتر الحالة المختار بالصفحة
            var baseFiltered = all.Where(MatchesBase).ToList();

            // مع فلتر الحالة - يُستخدم لبقية عناصر الصفحة
            var filtered = baseFiltered.Where(r => string.IsNullOrWhiteSpace(status) || r.Status == status).ToList();

            result.TotalOpportunities = filtered
                .Select(r => r.OpportunityNumber)
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Distinct()
                .Count();

            decimal EffectiveValue(ContractRecord r) => r.ContractValue ?? r.AnnualRent ?? 0;

            // رقم العقد لا يُعبَّأ بالملف إلا للعقود "تم إبرام العقد" - لبقية الحالات كل صف يُعتبر
            // عقدًا مستقلاً بحد ذاته. لذا يُحسَب "عدد العقود" دائمًا بعدّ مفاتيح مميّزة: رقم العقد
            // نفسه إن وُجد (لتفادي عدّ نفس العقد المُبرَم أكثر من مرة إن تكرّر بعدة صفوف)، وإلا
            // معرّف الصف نفسه (كل صف بلا رقم عقد = عقد مستقل).
            string DedupKey(ContractRecord r) => !string.IsNullOrWhiteSpace(r.ContractNumber) ? r.ContractNumber! : "row-" + r.Id;
            int DistinctCount(IEnumerable<ContractRecord> recs) => recs.Select(DedupKey).Distinct().Count();

            result.TypeSummary = filtered
                .Where(r => !string.IsNullOrWhiteSpace(r.ContractType))
                .GroupBy(r => r.ContractType!)
                .Select(g => new TarhTypeSummaryRow { TypeLabel = g.Key, Count = DistinctCount(g), TotalValue = g.Sum(EffectiveValue) })
                .OrderByDescending(x => x.Count)
                .ToList();

            result.DeptChart = filtered
                .GroupBy(r => string.IsNullOrWhiteSpace(r.GeneralDepartment) ? "غير محدد" : r.GeneralDepartment!)
                .Select(g => new TarhDeptRow { DeptLabel = g.Key, Count = DistinctCount(g), TotalValue = g.Sum(EffectiveValue) })
                .OrderByDescending(x => x.TotalValue)
                .ToList();

            result.TotalCount = DistinctCount(filtered);
            result.FinalizedCount = DistinctCount(filtered.Where(r => r.Status == FinalizedStatus));
            result.FinalizedPercent = result.TotalCount > 0
                ? Math.Round((decimal)result.FinalizedCount / result.TotalCount * 100, 1) : 0;

            result.ActivitiesDepartment = string.IsNullOrWhiteSpace(department) ? DefaultActivitiesDepartment : department;
            result.ActivityChart = filtered
                .Where(r => r.GeneralDepartment == result.ActivitiesDepartment && !string.IsNullOrWhiteSpace(r.SubPortfolio))
                .GroupBy(r => r.SubPortfolio!)
                .Select(g => new TarhActivityRow { ActivityLabel = g.Key, Count = DistinctCount(g), TotalValue = g.Sum(EffectiveValue) })
                .OrderByDescending(x => x.Count)
                .ToList();

            result.YearlyFinalizedTrend = baseFiltered
                .Where(r => r.Status == FinalizedStatus && r.ContractYear.HasValue)
                .GroupBy(r => r.ContractYear!.Value)
                .Select(g => new TarhYearRow { Year = g.Key, Count = DistinctCount(g) })
                .OrderBy(x => x.Year)
                .ToList();

            // الفرص المتأخرة: فتحت مظاريفها منذ 30 يومًا فأكثر ولا يوجد لها رقم عقد بعد. "سنة
            // العقد"/"الربع" غير مُعبَّأين لهذه الصفوف أصلاً (لا عقد = لا تاريخ عقد)، فتُصفَّى هنا
            // بسنة/ربع "فتح المظاريف" نفسها بدل سنة/ربع العقد حتى تبقى الفلاتر مفيدة لهذه القائمة
            var todayUtc = DateTime.UtcNow.Date;
            var envelopeThreshold = todayUtc.AddDays(-30);
            result.AtRiskOpportunities = all
                .Where(r =>
                    (string.IsNullOrWhiteSpace(department) || r.GeneralDepartment == department) &&
                    (string.IsNullOrWhiteSpace(contractNumber) || (r.ContractNumber ?? "").Contains(contractNumber, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrWhiteSpace(status) || r.Status == status) &&
                    string.IsNullOrWhiteSpace(r.ContractNumber) &&
                    r.EnvelopeOpeningDate.HasValue &&
                    r.EnvelopeOpeningDate.Value.Date <= envelopeThreshold &&
                    (!year.HasValue || r.EnvelopeOpeningDate.Value.Year == year) &&
                    (!quarter.HasValue || (((r.EnvelopeOpeningDate.Value.Month - 1) / 3) + 1) == quarter))
                .OrderBy(r => r.EnvelopeOpeningDate)
                .Select(r => new TarhAtRiskRow
                {
                    OpportunityNumber = r.OpportunityNumber,
                    OpportunityName = r.OpportunityName,
                    EnvelopeOpeningDate = r.EnvelopeOpeningDate!.Value,
                    DaysSinceEnvelope = (int)(todayUtc - r.EnvelopeOpeningDate.Value.Date).TotalDays,
                    GeneralDepartment = r.GeneralDepartment,
                    Status = r.Status
                })
                .ToList();

            return result;
        }
    }
}