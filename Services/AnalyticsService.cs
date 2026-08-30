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
        public string EntityChartTitle { get; set; } = "المحصّل ربع السنوي لكل جهة";

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
        /// تفصيل البنود الفرعية (عمود "الفئة/الصنف") لكل بند رئيسي (Classification) على حدة -
        /// تُستخدم لتبديل رسم "توزيع الإيرادات حسب البنود الفرعية" فورًا بالمتصفح عند الضغط
        /// على أي بند برسم "أعلى بنود الإيرادات" (بلا طلب إضافي للخادم)
        /// </summary>
        public List<TopItemSubBreakdown> TopItemSubItems { get; set; } = new();

        /// <summary>بيانات رسم "أداء الإيرادات حسب نوع الإيراد" - صف واحد لكل تصنيف بند رئيسي</summary>
        public List<RevenueTypePerformanceRow> RevenueTypePerformance { get; set; } = new();

        // ===== بطاقات "المحصل الحالي/السابق ونسبة النمو" (سنة بسنة، حسب أحدث سنة بالبيانات) =====
        public int RevenueCurrentYear { get; set; }
        public int RevenuePreviousYear { get; set; }
        public decimal RevenueCurrentYearCollected { get; set; }
        public decimal RevenuePreviousYearCollected { get; set; }
        public decimal RevenueGrowthPercent { get; set; }

        /// <summary>
        /// تفصيل المحصّل حسب أساس الاستحقاق مقارنةً بتاريخ الدفع (RecordDate) وتاريخ الإنشاء
        /// (CreatedDate) - "أساس استحقاق" (أُنشئت بنفس السنة الحالية)، "محصل من مديونيات"
        /// (دُفعت بالسنة الحالية لكن أُنشئت بسنة أسبق)، "محصل أوامر تنفيذ" (لا تاريخ إنشاء مسجَّل
        /// أصلاً). تُملأ من DashboardRecords الحية (لا علاقة لها بملف سنة الأساس 2025).
        /// </summary>
        public List<RevenueBasisBreakdownRow> RevenueBasisBreakdown { get; set; } = new();
        public decimal RevenueBasisBreakdownTotal { get; set; }

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

        /// <summary>
        /// أكثر/أقل بلدية تفاعلاً (حسب إجمالي "المحصّل" باسمها بعمود "الجهة") — تُملأ
        /// حصريًا من GetScopeDataAsync عند scope=Baladiyah بلا فلترة على بلدية محددة (لا تؤثر
        /// على أي صفحة أخرى)، لبطاقتَي صفحة "بيانات البلديات"
        /// </summary>
        public string? TopActivityEntity { get; set; }
        public decimal TopActivityCollected { get; set; }
        public string? BottomActivityEntity { get; set; }
        public decimal BottomActivityCollected { get; set; }

        /// <summary>
        /// ترتيب البلديات حسب إجمالي الإيرادات + نسبة التحصيل لكل بلدية — تُملأ حصريًا من
        /// GetScopeDataAsync عند scope=Baladiyah، لرسم "أعلى البلديات إيرادًا" بصفحة "بيانات
        /// البلديات" (مرتّبة تنازليًا حسب الإجمالي)
        /// </summary>
        public List<EntityRevenueRow> EntityRevenueBreakdown { get; set; } = new();

        /// <summary>
        /// توزيع السجلات حسب عمود "نوع البند" الخام (Category) - "ملزم"/"غير ملزم" -
        /// قيمة إجمالية وعدد سجلات لكل نوع. تُملأ حصريًا من GetOverviewDataAsync (الصفحة
        /// الرئيسية) لدونات "ملزم/غير ملزم" الجديدة.
        /// </summary>
        public List<NameValueCountRow> ItemTypeBreakdown { get; set; } = new();

        /// <summary>
        /// توزيع السجلات حسب النطاق (الأمانة/البلديات) - قيمة إجمالية وعدد سجلات لكل نطاق.
        /// تُملأ حصريًا من GetOverviewDataAsync (الصفحة الرئيسية) لدونات "الأمانة/البلديات" الجديدة.
        /// </summary>
        public List<NameValueCountRow> ScopeBreakdown { get; set; } = new();
    }

    /// <summary>صف بلدية واحدة: إجمالي إيراداتها + نسبة تحصيلها — لرسم "أعلى البلديات إيرادًا"</summary>
    public class EntityRevenueRow
    {
        public string Entity { get; set; } = string.Empty;
        public decimal TotalValue { get; set; }
        public decimal CollectionRatePercent { get; set; }
    }

    /// <summary>صف واحد {اسم، قيمة إجمالية، عدد} - لدونات "ملزم/غير ملزم" و"الأمانة/البلديات" بالصفحة الرئيسية</summary>
    public class NameValueCountRow
    {
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public int Count { get; set; }
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
    /// تفصيل البنود الفرعية (عمود "الفئة/الصنف") التابعة لبند رئيسي واحد (Classification) -
    /// يُستخدم لتبديل رسم "البنود الفرعية" فورًا عند الضغط على بند برسم "أعلى بنود الإيرادات"
    /// </summary>
    public class TopItemSubBreakdown
    {
        public string ClassificationLabel { get; set; } = string.Empty;
        public List<string> SubLabels { get; set; } = new();
        public List<decimal> SubValues { get; set; } = new();
        public decimal Total { get; set; }
    }

    /// <summary>
    /// صف نوع إيراد واحد (Classification) برسم "أداء الإيرادات حسب نوع الإيراد" - المفوتر
    /// والمحصّل من بيانات الإيرادات الفعلية (بنفس تصفية اللوحة الحالية)، والمستهدف السنوي من
    /// صفحتَي "مستهدفات المالية"/"مستهدفات البلديات" (كلا المصدرين معًا، يُختار بينهما
    /// بالمتصفح بلا طلب إضافي)، مُشتقًا تلقائيًا للجهات الفعلية الموجودة بهذا الصف فقط.
    /// </summary>
    public class RevenueTypePerformanceRow
    {
        public string ItemLabel { get; set; } = string.Empty;
        public decimal Invoiced { get; set; }
        public decimal Collected { get; set; }
        public decimal TargetFinance { get; set; }
        public decimal TargetBaladiyah { get; set; }
    }

    /// <summary>صف حالة واحدة (أساس استحقاق / محصل من مديونيات / محصل أوامر تنفيذ) بتفصيل المحصّل حسب أساس الاستحقاق</summary>
    public class RevenueBasisBreakdownRow
    {
        public string CaseLabel { get; set; } = string.Empty;
        public decimal CollectedValue { get; set; }
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

        /// <summary>المحصّل الفعلي لكل ربع (Q1..Q4) - يقارَن بربع المستهدف السنوي (÷4) لعدم
        /// توفر مستهدف ربعي حقيقي لوزارة المالية (مستهدف سنوي فقط بالملف المصدر)</summary>
        public List<decimal> AmanahQuarterlyCollected { get; set; } = new();
        public List<decimal> BaladiyahQuarterlyCollected { get; set; } = new();
    }

    /// <summary>بيانات قسم "تحقيق المستهدف" بصفحة "الملخص التنفيذي" لبند/مصدر مستهدف/سنة محدَّدة</summary>
    public class TargetAchievementData
    {
        public bool HasData { get; set; }
        public decimal Target { get; set; }
        public decimal Collected { get; set; }
        public decimal Invoiced { get; set; }
        public decimal AchievementPercent { get; set; }
    }

    /// <summary>قوائم فلاتر قسم "تحقيق المستهدف" بصفحة "الملخص التنفيذي"</summary>
    public class TargetFilterOptionsData
    {
        public List<string> MainItems { get; set; } = new();
        public Dictionary<string, List<string>> SubItemsByMainItem { get; set; } = new();
        public List<int> Years { get; set; } = new();
    }

    /// <summary>صف نوع عقد واحد (كرت) بصفحة "الطرح" - عدد وقيمة العقود من هذا النوع</summary>
    public class TarhTypeSummaryRow
    {
        public string TypeLabel { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
    }

    /// <summary>صف نوع عقد واحد - إجمالي عدد/قيمة هذا النوع بكل البيانات (بلا فلتر سنة/حالة) مقابل
    /// عدد/قيمة ما أُبرم منه فعليًا بسنة 2026 (TarhTypeSummaryRow). يُستخدم بصفحة "الملخص
    /// التنفيذي" الجديدة: دونات "عدد الفرص حسب النوع" + بار "القيمة ونسبة الإبرام حسب النوع" -
    /// منفصل عن TarhTypeSummaryRow حتى لا يغيّر معنى الحقول المستخدَمة أصلًا بصفحة "الطرح" نفسها.
    /// نفس الأربعة أنواع الظاهرة بصفحة "الطرح" بالضبط (يُبنى من TypeSummary نفسها).</summary>
    public class TarhTypeTenderedRow
    {
        public string TypeLabel { get; set; } = string.Empty;
        public int TenderedCount { get; set; }
        public decimal TenderedValue { get; set; }
        public int FinalizedCount { get; set; }
        public decimal FinalizedValue { get; set; }
        public decimal FinalizedPercent { get; set; }
    }

    /// <summary>صف سنة واحدة بصفحة "الطرح" - عدد العقود المبرومة بهذه السنة</summary>
    public class TarhYearRow
    {
        public int Year { get; set; }
        public int Count { get; set; }
    }

    /// <summary>صف إدارة عامة واحدة بصفحة "الطرح" - قيمة وعدد العقود المبرومة منها (رسم 1)</summary>
    public class TarhDeptSummaryRow
    {
        public string DepartmentLabel { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
    }

    /// <summary>صف محفظة فرعية واحدة (ضمن إدارة عامة محدَّدة) بصفحة "الطرح" - قيمة وعدد العقود
    /// المبرومة منها (رسم 2) - يحمل اسم الإدارة العامة الأصلية حتى تُفلتَر القائمة بالواجهة
    /// فورًا حسب الإدارة المختارة برسم 1 بلا أي طلب إضافي للسيرفر</summary>
    public class TarhFinalizedByPortfolioRow
    {
        public string DepartmentLabel { get; set; } = string.Empty;
        public string PortfolioLabel { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
    }

    /// <summary>صف سنة واحدة بصفحة "الطرح" - نسبتا الطرح والإبرام التراكميتان حتى هذه السنة (رسم 3)</summary>
    public class TarhYearlyRateRow
    {
        public int Year { get; set; }

        /// <summary>نسبة الفرص المطروحة (فُتحت مظاريفها) تراكميًا حتى هذه السنة، من إجمالي الفرص المطروحة بكل السنوات</summary>
        public decimal TenderedPercent { get; set; }

        /// <summary>نسبة العقود المبرومة تراكميًا حتى هذه السنة، من نفس إجمالي الفرص المطروحة (لتبقى النسبتان قابلتين للمقارنة المباشرة)</summary>
        public decimal FinalizedPercent { get; set; }

        /// <summary>نسبة الفرص الملغاة (حالة "إلغاء المنافسة" - تشمل أيضًا "لم يتقدم أحد" المدمجة بها عند الاستيراد) تراكميًا حتى هذه السنة، من نفس إجمالي الفرص المطروحة</summary>
        public decimal CancelledPercent { get; set; }
    }

    /// <summary>صف (سنة + حالة) واحد بصفحة "الطرح" - عدد الفرص التي فُتحت مظاريفها بهذه السنة وبهذه الحالة</summary>
    public class TarhEnvelopeStatusRow
    {
        public int Year { get; set; }
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>صف تصدير واحد بصفحة "الطرح" (تصدير Excel/PDF)</summary>
    public class TarhExportRow
    {
        public string? OpportunityNumber { get; set; }
        public string? ContractNumber { get; set; }
        public string? OpportunityName { get; set; }
        public decimal Value { get; set; }
        public string? GeneralDepartment { get; set; }
        public string? SubPortfolio { get; set; }
        public int? ContractYear { get; set; }
        public DateTime? ContractDate { get; set; }
    }

    /// <summary>
    /// "تحت إجراءات الترسية" بصفحة "الطرح": من بين الفرص بإحدى الحالات الثلاث قيد الإجراء
    /// (لم تُبرَم ولم تُلغَ بعد) التي فُتحت مظاريفها بسنة 2026 بالضبط - كم منها تجاوزت 30 يوم
    /// منذ فتح المظاريف (أحمر) وكم ضمن 30 يوم (أخضر)، ونسبة المتجاوزة من إجمالي الفرص التي
    /// فُتحت مظاريفها بـ2026 (بصرف النظر عن حالتها)
    /// </summary>
    public class TarhDurationStatus
    {
        /// <summary>إجمالي الفرص التي فُتحت مظاريفها بسنة 2026 (المرجع/المقام بحساب النسبة)</summary>
        public int TotalCount { get; set; }
        /// <summary>عدد الفرص (بالحالات الثلاث) اللي تجاوزت 30 يوم منذ فتح المظاريف - أحمر</summary>
        public int ExceededCount { get; set; }
        /// <summary>عدد الفرص (بالحالات الثلاث) اللي ضمن 30 يوم منذ فتح المظاريف - أخضر</summary>
        public int WithinCount { get; set; }
        /// <summary>نسبة المتجاوزة 30 يوم = ExceededCount ÷ TotalCount × 100</summary>
        public decimal ExceededPercent { get; set; }
        /// <summary>عدد الفرص لكل حالة من الحالات الثلاث قيد الإجراء (TarhPendingStatuses) - يُعرَض كقائمة أسماء+أعداد أسفل القوس</summary>
        public List<NameCountRow> StatusBreakdown { get; set; } = new();
    }

    /// <summary>3 قطاعات دونات "الفرص المطروحة القادمة" بصفحة الطرح - الأسبوع القادم، الشهر القادم (كلاهما من تاريخ اليوم فصاعدًا)، وإجمالي سنة 2026 بأكملها</summary>
    public class TarhUpcomingTenders
    {
        public int NextWeekCount { get; set; }
        public int NextMonthCount { get; set; }
        public int Year2026Count { get; set; }
    }

    /// <summary>خيار واحد بقائمة اختيار الفرصة (زر "تتبع الفرصة")</summary>
    /// <summary>صف فرصة واحدة بقائمة "الفرص التي فُتحت مظاريفها" (فلتر نطاق تاريخ بصفحة الطرح)</summary>
    public class TarhEnvelopeOpenedRow
    {
        public string? OpportunityNumber { get; set; }
        public string? OpportunityName { get; set; }
        public string? InvestorName { get; set; }
        public string? GeneralDepartment { get; set; }
        public string? SubPortfolio { get; set; }
        public string? Status { get; set; }
        public DateTime? EnvelopeOpeningDate { get; set; }
        public decimal? Value { get; set; }
    }

    public class TarhOpportunityOption
    {
        public string OpportunityNumber { get; set; } = string.Empty;
        public string? OpportunityName { get; set; }
        public string? InvestorName { get; set; }
        public string? ContractNumber { get; set; }

        /// <summary>"ضمن 30 يوم" أو "تجاوزت 30 يوم" أو null إن تعذّر حساب المدة - لتلوين نتائج البحث فورًا بلا طلب إضافي</summary>
        public string? DurationStatus { get; set; }
    }

    /// <summary>رحلة فرصة واحدة عبر مراحلها الثلاث: فتح المظاريف ← الترسية ← توقيع العقد</summary>
    public class TarhOpportunityTimeline
    {
        public bool Found { get; set; }
        public string? OpportunityNumber { get; set; }
        public string? OpportunityName { get; set; }
        public string? InvestorName { get; set; }
        public string? ContractNumber { get; set; }
        public string? Status { get; set; }

        public DateTime? EnvelopeOpeningDate { get; set; }
        public DateTime? AwardDate { get; set; }
        public DateTime? ContractDate { get; set; }

        /// <summary>المدة بالأيام بين فتح المظاريف والترسية (إن توفّر تاريخ الترسية)</summary>
        public int? EnvelopeToAwardDays { get; set; }

        /// <summary>المدة بالأيام بين الترسية وتوقيع العقد (إن توفّر تاريخ الترسية)</summary>
        public int? AwardToContractDays { get; set; }

        /// <summary>إجمالي مدة الفرصة بالأيام (فتح المظاريف إلى توقيع العقد)</summary>
        public int? TotalDays { get; set; }

        /// <summary>"ضمن 30 يوم" أو "تجاوزت 30 يوم" - يتطلب توفر تاريخَي فتح المظاريف والعقد معًا</summary>
        public string? DurationStatus { get; set; }
    }

    /// <summary>
    /// بيانات صفحة "الطرح": كروت أنواع العقود، قيمة وعدد العقود المبرومة حسب المحفظة الفرعية،
    /// الاتجاه الزمني لتوقيع العقود المبرومة، وتوزيع فتح المظاريف حسب السنة والحالة - كل ذلك
    /// قابل للتصفية بالسنة والربع والإدارة العامة وحالة العقد ورقم العقد معًا.
    /// </summary>
    public class TarhPageData
    {
        public bool HasData { get; set; }

        public List<TarhTypeSummaryRow> TypeSummary { get; set; } = new();

        public int TotalCount { get; set; }
        public int FinalizedCount { get; set; }
        public decimal FinalizedPercent { get; set; }

        /// <summary>إجمالي عدد الفرص (رقم الفرصة) ضمن التصفية الحالية</summary>
        public int TotalOpportunities { get; set; }

        /// <summary>إجمالي قيمة الفرص التي طُرحت (فُتحت مظاريفها) بسنة 2026 بالضبط - بغض النظر
        /// عن حالتها (نفس نطاق DeptSummary/TypeTenderedSummary)، يغذّي كرت "إجمالي القيمة"
        /// بصفحة "الملخص التنفيذي"</summary>
        public decimal TotalOpportunitiesValue { get; set; }

        /// <summary>عدد وقيمة الفرص التي طُرحت (فُتحت مظاريفها) بسنة 2026 بالضبط، حسب نوع العقد،
        /// مقابل عدد/قيمة ما أُبرم منها فعليًا - يغذّي دونات وبار "الملخص التنفيذي" الجديدة</summary>
        public List<TarhTypeTenderedRow> TypeTenderedSummary { get; set; } = new();

        /// <summary>عدد وقيمة الفرص التي طُرحت (فُتحت مظاريفها) بسنة 2026 بالضبط، حسب الإدارة العامة (رسم 1)</summary>
        public List<TarhDeptSummaryRow> DeptSummary { get; set; } = new();

        /// <summary>عدد وقيمة العقود المبرومة 2026 بالضبط حسب الإدارة العامة - يغذّي صف 1 بصفحة "الملخص التنفيذي"</summary>
        public List<TarhDeptSummaryRow> FinalizedByDepartment { get; set; } = new();

        /// <summary>نفس فكرة DeptSummary حسب المحفظة الفرعية، مع اسم الإدارة العامة لكل صف (رسم 2)</summary>
        public List<TarhFinalizedByPortfolioRow> FinalizedByPortfolio { get; set; } = new();

        /// <summary>نسبتا الطرح والإبرام التراكميتان حسب السنة (رسم 3) - بلا أي فلاتر، كل السنوات دائمًا</summary>
        public List<TarhYearlyRateRow> YearlyRates { get; set; } = new();

        /// <summary>الاتجاه الزمني لتوقيع العقود المبرومة (عدد لكل سنة)</summary>
        public List<TarhYearRow> YearlyFinalizedTrend { get; set; } = new();

        /// <summary>عدد الفرص التي فُتحت مظاريفها لكل (سنة، حالة) - لرسم الأعمدة المكدّسة</summary>
        public List<TarhEnvelopeStatusRow> EnvelopeByYearStatus { get; set; } = new();

        /// <summary>"تحت إجراءات الترسية" - عدد الفرص قيد الإجراء ونسبتها من إجمالي فتح المظاريف</summary>
        public TarhDurationStatus DurationStatus { get; set; } = new();

        /// <summary>دونات "الفرص المطروحة القادمة" - أسبوع/شهر قادم + إجمالي 2026 (رسم 1، بجانب رسم الإدارة العامة)</summary>
        public TarhUpcomingTenders UpcomingTenders { get; set; } = new();

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

        /// <summary>
        /// قسم "تحقيق المستهدف" بصفحة "الملخص التنفيذي": المستهدف (من FinanceItemTarget) مقابل
        /// المحصّل/المفوتر الفعلي (من DashboardRecords)، مُفلترة بالنطاق (أمانة/بلديات/الكل)
        /// والبند الرئيسي/الفرعي/مصدر المستهدف (مالية/بلديات وإسكان/الكل) والسنة معًا
        /// </summary>
        Task<TargetAchievementData> GetTargetAchievementAsync(string? scope, string? mainItem, string? subItem, string? targetSource, int? year);

        /// <summary>قوائم فلاتر قسم "تحقيق المستهدف": البنود الرئيسية وبنودها الفرعية (من جدول Sections، حسب النطاق) والسنوات المتاحة (من DashboardRecords)</summary>
        Task<TargetFilterOptionsData> GetTargetFilterOptionsAsync(string? scope);

        /// <summary>
        /// بيانات صفحة "الطرح" (كروت أنواع العقود، القيمة حسب المحفظة الفرعية، الاتجاه الزمني، فتح المظاريف حسب الحالة).
        /// forcedDepartment: لموظف مقيّد بإدارة عامة واحدة فقط - يقيّد رسمَي "الفرص المطروحة حسب
        /// الإدارة العامة/الفرعية" أيضًا (هذان الرسمان عمدًا لا يتأثران بـ department العادي، لأن
        /// الغرض منهما مقارنة الإدارات ببعضها لمستخدم غير مقيّد) بدل إظهار كل الإدارات لموظف
        /// مفروض يشوف إدارته فقط. null لمستخدم غير مقيّد (يبقى السلوك القديم كما هو).
        /// </summary>
        Task<TarhPageData> GetTarhDataAsync(string? department, string? status, string? contractNumber, int? year, int? quarter, string? forcedDepartment = null);

        /// <summary>صفوف تصدير صفحة "الطرح" (Excel/PDF) - بنفس تصفية GetTarhDataAsync</summary>
        Task<List<TarhExportRow>> GetTarhExportRowsAsync(string? department, string? status, string? contractNumber, int? year, int? quarter);

        /// <summary>قائمة الفرص (اللي عندها رقم عقد فعلي) لتعبئة مربع اختيار "تتبع الفرصة"</summary>
        Task<List<TarhOpportunityOption>> GetTarhOpportunityOptionsAsync();

        /// <summary>رحلة فرصة واحدة (فتح المظاريف ← الترسية ← توقيع العقد) برقم الفرصة</summary>
        Task<TarhOpportunityTimeline> GetTarhOpportunityTimelineAsync(string opportunityNumber);

        /// <summary>
        /// قائمة الفرص التي فُتحت مظاريفها ضمن نطاق تاريخ معيّن (مثل "هذا الأسبوع") - مرتّبة
        /// بالأحدث أولاً. forcedDepartment: لموظف مقيّد بإدارة عامة واحدة يقيّد القائمة على
        /// إدارته فقط، null لمستخدم غير مقيّد (يشوف كل الإدارات).
        /// </summary>
        Task<List<TarhEnvelopeOpenedRow>> GetTarhEnvelopeOpenedAsync(DateTime start, DateTime end, string? forcedDepartment = null);

        Task<SectionDashboardData> GetSectionDataAsync(int sectionId, bool includeSubSections = true, string? quarter = null, string? contractNumber = null, int? month = null);

        /// <summary>
        /// بيانات مجمّعة لعدة بنود معاً (مع كل بنودها الفرعية) — تُستخدم لمجموعات
        /// الإدارات العامة المكررة بنفس الاسم تحت أكثر من بند
        /// </summary>
        Task<SectionDashboardData> GetSectionsDataAsync(IEnumerable<int> sectionIds, string? quarter = null, string? contractNumber = null, int? month = null);

        /// <summary>
        /// بيانات لوحة كاملة لجهة واحدة (مثل: بلدية الحناكية) عبر كل بنودها
        /// </summary>
        Task<SectionDashboardData> GetEntityDataAsync(string entityName, string? quarter = null, string? contractNumber = null, int? month = null);

        Task<decimal> GetScopeTotalRevenueAsync(ScopeType scope);

        /// <summary>
        /// بيانات لوحة تفاعلية مجمّعة لكل بنود نطاق معين (الأمانة أو البلدية) بالكامل
        /// </summary>
        Task<SectionDashboardData> GetScopeDataAsync(ScopeType scope, string? quarter = null, string? contractNumber = null, int? month = null);

        /// <summary>
        /// بيانات لوحة عامة مجمّعة لعدة نطاقات معاً (تُستخدم كبديل عن نطاق "الكل" القديم)
        /// </summary>
        Task<SectionDashboardData> GetOverviewDataAsync(IEnumerable<ScopeType> scopes, string? quarter = null, string? contractNumber = null, int? month = null);
    }

    public class AnalyticsService : IAnalyticsService
    {
        private readonly AppDbContext _db;

        // سنة "محصل سنة الأساس" ببطاقتَي رسم "أداء الإيرادات" بالرئيسية - مصدرها جدول
        // RevenueBaselineCollectedItems المنفصل (ملف خارجي)، وليس DashboardRecords
        private const int RevenueBaselineYear = 2025;

        public AnalyticsService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// يطبّق تصفية اختيارية بحسب رقم العقد (بحث جزئي) والربع (تطابق تام) على استعلام السجلات.
        /// تُستخدم من مربع البحث وقائمة "الربع" التفاعلية أعلى كل لوحة بيانات.
        /// </summary>
        private static IQueryable<DashboardRecord> ApplyFilters(IQueryable<DashboardRecord> query, string? quarter, string? contractNumber, int? month = null)
        {
            if (!string.IsNullOrWhiteSpace(quarter))
            {
                query = query.Where(r => r.Quarter == quarter);
            }

            if (month.HasValue)
            {
                query = query.Where(r => r.Month == month);
            }

            if (!string.IsNullOrWhiteSpace(contractNumber))
            {
                var term = contractNumber.Trim();
                query = query.Where(r => r.ContractNumber != null && r.ContractNumber.Contains(term));
            }

            return query;
        }

        /// <summary>
        /// يقلّص الاستعلام لأعمدة DashboardRecord التي يقرأها BuildResult فعليًا فقط (10 من أصل
        /// ~30 عمودًا) بدل سحب الصف كاملاً - فرق ملموس بجدول بمئات آلاف الصفوف (قِيست تجريبيًا:
        /// ~4.7 ثانية للصف كامل مقابل ~2.8 ثانية لهذا الإسقاط على 269,612 صف). يُستخدم فقط بمسارات
        /// تُمرَّر فيها النتيجة إلى BuildResult حصرًا بلا أي استهلاك آخر لبقية أعمدة السجل
        /// (لا يُستخدم بـ GetScopeDataAsync لأنها تُعيد استخدام نفس records لاحقًا لتفصيل
        /// الإدارات العامة/الفرعية ببند الاستثمار، والتي تحتاج أعمدة إضافية)
        /// </summary>
        private static IQueryable<DashboardRecord> ProjectForBuildResult(IQueryable<DashboardRecord> query) =>
            query.Select(r => new DashboardRecord
            {
                SectionId = r.SectionId,
                TotalValue = r.TotalValue,
                CollectedValue = r.CollectedValue,
                UncollectedValue = r.UncollectedValue,
                TargetValue = r.TargetValue,
                RecordDate = r.RecordDate,
                CreatedDate = r.CreatedDate,
                Category = r.Category,
                ItemType = r.ItemType,
                AmanahBaladiRaw = r.AmanahBaladiRaw,
                Classification = r.Classification,
                Quarter = r.Quarter,
                Entity = r.Entity
            });

        public async Task<SectionDashboardData> GetSectionDataAsync(int sectionId, bool includeSubSections = true, string? quarter = null, string? contractNumber = null, int? month = null)
        {
            var sectionIds = new List<int> { sectionId };

            if (includeSubSections)
            {
                sectionIds.AddRange(await GetDescendantIdsAsync(sectionId));
            }

            var query = _db.DashboardRecords.AsNoTracking().Where(r => sectionIds.Contains(r.SectionId));
            var records = await ProjectForBuildResult(ApplyFilters(query, quarter, contractNumber, month)).ToListAsync();

            return await BuildResult(records);
        }

        public async Task<SectionDashboardData> GetEntityDataAsync(string entityName, string? quarter = null, string? contractNumber = null, int? month = null)
        {
            var query = _db.DashboardRecords.AsNoTracking().Where(r => r.Entity == entityName);
            var records = await ProjectForBuildResult(ApplyFilters(query, quarter, contractNumber, month)).ToListAsync();

            return await BuildResult(records);
        }

        public async Task<SectionDashboardData> GetSectionsDataAsync(IEnumerable<int> sectionIds, string? quarter = null, string? contractNumber = null, int? month = null)
        {
            var allIds = new List<int>();
            foreach (var id in sectionIds.Distinct())
            {
                allIds.Add(id);
                allIds.AddRange(await GetDescendantIdsAsync(id));
            }
            allIds = allIds.Distinct().ToList();

            var query = _db.DashboardRecords.AsNoTracking().Where(r => allIds.Contains(r.SectionId));
            var records = await ProjectForBuildResult(ApplyFilters(query, quarter, contractNumber, month)).ToListAsync();

            return await BuildResult(records);
        }

        /// <summary>
        /// يجمع كل معرّفات البنود التابعة لبند معين بأي عمق (إدارات عامة ← إدارات فرعية ← ...)
        /// حتى تُحتسب بيانات كل المستويات الأعمق ضمن لوحة البند الأعلى تلقائياً
        /// </summary>
        private async Task<List<int>> GetDescendantIdsAsync(int sectionId)
        {
            var all = await _db.Sections.AsNoTracking()
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
            var sectionIds = await _db.Sections.AsNoTracking()
                .Where(s => s.Scope == scope && s.IsActive)
                .Select(s => s.Id)
                .ToListAsync();

            return await _db.DashboardRecords.AsNoTracking()
                .Where(r => sectionIds.Contains(r.SectionId))
                .SumAsync(r => r.TotalValue ?? 0);
        }

        /// <summary>
        /// يجمّع بيانات كل البنود (الرئيسية والفرعية) التابعة لنطاق واحد (الأمانة أو البلدية)
        /// ليُعرض كلوحة بيانات عامة عند اختيار النطاق من القائمة الجانبية مباشرة
        /// </summary>
        public async Task<SectionDashboardData> GetScopeDataAsync(ScopeType scope, string? quarter = null, string? contractNumber = null, int? month = null)
        {
            var sectionIds = await _db.Sections.AsNoTracking()
                .Where(s => s.Scope == scope && s.IsActive)
                .Select(s => s.Id)
                .ToListAsync();

            var query = _db.DashboardRecords.AsNoTracking().Where(r => sectionIds.Contains(r.SectionId));
            var records = await ApplyFilters(query, quarter, contractNumber, month).ToListAsync();

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
                entityChartTitle = "الإيرادات ربع السنوية لإجمالي البلديات";
                entityChartBadge = "اختر بلدية من القائمة لعرض تفاصيلها";
            }

            var result = await BuildResult(records, rootNameMap, entityGroupOverride, entityChartTitle, entityChartBadge);

            // صفحة "بنود الأمانة" فقط: نُرفق تفصيل الإدارات العامة/الفرعية الخاص ببند
            // الاستثمار (لا يؤثر إطلاقًا على نطاق البلدية أو أي صفحة أخرى)
            if (scope == ScopeType.Amanah)
            {
                result.InvestmentDepts = await BuildInvestmentDeptBreakdownAsync(records);
            }

            // صفحة "بنود البلدية" فقط: أكثر/أقل بلدية تفاعلاً (حسب إجمالي المحصّل) + ترتيب
            // البلديات حسب إجمالي الإيرادات ونسبة التحصيل (رسم "أعلى البلديات إيرادًا")
            if (scope == ScopeType.Baladiyah)
            {
                var byEntity = records
                    .Where(r => !string.IsNullOrWhiteSpace(r.Entity))
                    .GroupBy(r => r.Entity!.Trim())
                    .Select(g => new
                    {
                        Entity = g.Key,
                        Total = g.Sum(x => x.TotalValue ?? 0),
                        Collected = g.Sum(x => x.CollectedValue ?? 0)
                    })
                    .ToList();

                if (byEntity.Count > 0)
                {
                    var top = byEntity.OrderByDescending(x => x.Collected).First();
                    result.TopActivityEntity = top.Entity;
                    result.TopActivityCollected = top.Collected;

                    var bottom = byEntity.OrderBy(x => x.Collected).First();
                    result.BottomActivityEntity = bottom.Entity;
                    result.BottomActivityCollected = bottom.Collected;

                    result.EntityRevenueBreakdown = byEntity
                        .OrderByDescending(x => x.Total)
                        .Select(x => new EntityRevenueRow
                        {
                            Entity = x.Entity,
                            TotalValue = x.Total,
                            CollectionRatePercent = x.Total > 0 ? Math.Round(x.Collected / x.Total * 100, 1) : 0
                        })
                        .ToList();
                }
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
            var allSections = await _db.Sections.AsNoTracking()
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
        public async Task<SectionDashboardData> GetOverviewDataAsync(IEnumerable<ScopeType> scopes, string? quarter = null, string? contractNumber = null, int? month = null)
        {
            var scopeList = scopes.ToList();
            var sections = await _db.Sections.AsNoTracking()
                .Where(s => scopeList.Contains(s.Scope) && s.IsActive)
                .Select(s => new { s.Id, s.Scope })
                .ToListAsync();
            var sectionIds = sections.Select(s => s.Id).ToList();

            var query = _db.DashboardRecords.AsNoTracking().Where(r => sectionIds.Contains(r.SectionId));
            var records = await ProjectForBuildResult(ApplyFilters(query, quarter, contractNumber, month)).ToListAsync();

            // مستوى "عام": الرسم يعرض البنود الأساسية فقط، ويتم دمج بيانات الأمانة
            // والبلدية تلقائياً إذا كان اسم البند الأساسي نفسه (التجميع هنا يتم حسب
            // اسم البند الأساسي وليس حسب معرّف القسم أو النطاق)
            var rootNameMap = await BuildRootNameMapAsync(sectionIds);

            // رسم الأعمدة الأول في لوحة "عام" يجب أن يعرض إجمالي الأمانة مقابل
            // إجمالي البلديات ككل فقط، دون تفصيل كل بلدية على حدة (بحسب الطلب)
            var scopeGroupMap = sections.ToDictionary(
                s => s.Id,
                s => s.Scope == ScopeType.Amanah ? "الأمانة" : "البلديات");

            var result = await BuildResult(
                records,
                rootNameMap,
                entityGroupOverride: scopeGroupMap,
                entityChartTitle: "المحصّل ربع السنوي: الأمانة مقابل البلديات",
                entityChartBadge: "اضغط لعرض تفاصيل النطاق");

            if (!result.HasData) return result;

            // دونات "ملزم/غير ملزم": عمود "نوع البند" الخام (ItemType) لكل سجل - قيمة وعدد لكل نوع
            result.ItemTypeBreakdown = records
                .Where(r => !string.IsNullOrWhiteSpace(r.ItemType))
                .GroupBy(r => r.ItemType!.Trim())
                .Select(g => new NameValueCountRow { Name = g.Key, Value = g.Sum(x => x.TotalValue ?? 0), Count = g.Count() })
                .OrderByDescending(x => x.Value)
                .ToList();

            // دونات "الأمانة/البلديات": تُصنَّف من عمود "الأمانة/بلدى" الخام لكل صف مباشرةً (عمود
            // مستقل تمامًا عن "المنطقة" المستخدَم لمطابقة نطاق البند)، ولو كان العمود فارغًا بذلك
            // الصف نرجع لنطاق البند المطابَق (scopeGroupMap) كاحتياطي حتى لا يسقط أي سجل من الدونات
            static string? ClassifyAmanahBaladiRaw(string? raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return null;
                var norm = raw.Trim();
                var lower = norm.ToLowerInvariant();
                // القيم قد تكون عربية ("أمانة"/"بلدية") أو مُحوّلة ("AMANA"/"BALADY")
                if (norm.Contains("بلد") || lower.Contains("balad") || lower.Contains("baldy")) return "البلديات";
                if (norm.Contains("امانه") || norm.Contains("أمانة") || norm.Contains("امانة") || lower.Contains("aman")) return "الأمانة";
                return null;
            }

            result.ScopeBreakdown = records
                .Select(r => new
                {
                    Label = ClassifyAmanahBaladiRaw(r.AmanahBaladiRaw) ?? (scopeGroupMap.TryGetValue(r.SectionId, out var sc) ? sc : null),
                    Record = r
                })
                .Where(x => x.Label != null)
                .GroupBy(x => x.Label!)
                .Select(g => new NameValueCountRow { Name = g.Key, Value = g.Sum(x => x.Record.TotalValue ?? 0), Count = g.Count() })
                .OrderByDescending(x => x.Value)
                .ToList();

            return result;
        }

        /// <summary>
        /// يبني خريطة (معرّف أي بند فرعي) ← (اسم البند الأساسي/تصنيف البند الذي يتبع له)
        /// عبر تتبّع سلسلة الأب (ParentSectionId) حتى الجذر. تُستخدم لتجميع الرسوم على
        /// مستوى "البنود الأساسية" بدل "البنود الفرعية" في لوحتي عام/الأمانة/البلدية.
        /// </summary>
        private async Task<Dictionary<int, string>> BuildRootNameMapAsync(IEnumerable<int> sectionIds)
        {
            var allSections = await _db.Sections.AsNoTracking()
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
        private async Task<SectionDashboardData> BuildResult(
            List<DashboardRecord> records,
            Dictionary<int, string>? groupByRootName = null,
            Dictionary<int, string>? entityGroupOverride = null,
            string? entityChartTitle = null,
            string? entityChartBadge = null)
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

            // توزيع الإيرادات حسب البنود الفرعية (عمود "الفئة/الصنف") مُقسَّم حسب البند الرئيسي
            // (Classification) الذي يتبع له كل بند فرعي - يبني تفصيلاً كاملاً واحدًا لكل بند
            // رئيسي (بدل قائمة عالمية واحدة مقسورة سابقًا على "استثمار" فقط)، حتى تستطيع الواجهة
            // تبديل رسمة "البنود الفرعية" فورًا عند الضغط على أي بند برسمة "أعلى بنود الإيرادات"
            // (بلا أي طلب إضافي للخادم) - تمامًا كتصفية الإدارة/الإدارة الفرعية بصفحة "الطرح".
            // التجميع يتم حسب اسم "البند الفرعي" الرسمي من جدول Sections (نفس مصدر قائمة
            // "البند الفرعي" بشريط الفلاتر) وليس نص عمود "الفئة/الصنف" الخام - النص الخام قد
            // يحمل عدة صيغ كتابة مختلفة لنفس البند الفعلي (كانت تُظهر شرائح مكررة بالرسم لا تطابق
            // قائمة الفلاتر النظيفة)، بينما استيراد البيانات نفسه يوحّد كل الصيغ المتشابهة تحت نفس
            // معرّف القسم (مطابقة برقم البند أو مطابقة تقريبية للاسم - راجع DataImportService)
            var sectionIdsInRecords = records.Select(r => r.SectionId).Distinct().ToList();
            var subItemNameBySectionId = await _db.Sections.AsNoTracking()
                .Where(s => sectionIdsInRecords.Contains(s.Id))
                .Select(s => new { s.Id, s.NameAr })
                .ToDictionaryAsync(s => s.Id, s => s.NameAr);

            string SubItemLabelFor(DashboardRecord r) =>
                subItemNameBySectionId.TryGetValue(r.SectionId, out var name) && !string.IsNullOrWhiteSpace(name)
                    ? name
                    : (r.Category ?? string.Empty).Trim();

            const int topSubItemCount = 6;
            var subItemsByClassification = records
                .Where(r => !string.IsNullOrWhiteSpace(r.Classification) && !string.IsNullOrWhiteSpace(SubItemLabelFor(r)))
                .GroupBy(r => r.Classification!.Trim())
                .Select(g =>
                {
                    var groups = g.GroupBy(r => SubItemLabelFor(r))
                        .Select(cg => new { Label = cg.Key, Total = cg.Sum(x => x.TotalValue ?? 0) })
                        .Where(x => x.Total > 0)
                        .OrderByDescending(x => x.Total)
                        .ToList();

                    var top = groups.Take(topSubItemCount).ToList();
                    var otherTotal = groups.Skip(topSubItemCount).Sum(x => x.Total);
                    var labels = top.Select(x => x.Label).ToList();
                    var values = top.Select(x => x.Total).ToList();
                    if (otherTotal > 0) { labels.Add("أخرى"); values.Add(otherTotal); }

                    return new TopItemSubBreakdown
                    {
                        ClassificationLabel = g.Key,
                        SubLabels = labels,
                        SubValues = values,
                        Total = g.Sum(x => x.TotalValue ?? 0)
                    };
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            result.TopItemSubItems = subItemsByClassification;

            // العرض الافتراضي لرسمة "البنود الفرعية" (قبل أي ضغط من المستخدمة) = تفصيل أعلى بند
            // رئيسي قيمةً تلقائيًا
            if (subItemsByClassification.Count > 0)
            {
                result.SubItemLabels = subItemsByClassification[0].SubLabels;
                result.SubItemValues = subItemsByClassification[0].SubValues;
            }

            // ==================================================================
            // "أداء الإيرادات حسب نوع الإيراد": مفوتر/محصّل من بيانات الإيرادات الفعلية (نفس
            // تصفية اللوحة الحالية)، والمستهدف السنوي من صفحتَي "مستهدفات المالية"/"مستهدفات
            // البلديات" (سنويًا فقط، كلا المصدرين معًا يُرسَلان دومًا - الاختيار بينهما يتم
            // بالمتصفح بلا طلب إضافي). "الجهة" اللازمة لاختيار حصة المستهدف الصحيحة تُشتق
            // تلقائيًا من الجهات الفعلية الموجودة بسجلات كل بند (بلا قائمة اختيار منفصلة):
            // لو وُجدت سجلات أمانة تُضاف حصة الأمانة من المصدر، ولو وُجدت سجلات بلديات تُضاف
            // حصة "كل البلديات" أيضًا (فتُجمَعان معًا تلقائيًا باللوحات المجمّعة كالرئيسية)
            // ==================================================================
            // بند "إيرادات استثمار العقارات البلدية" فقط ينقسم فعليًا إلى تصنيفين مختلفين بكلا
            // ملفَي المستهدف (المالية بعمود "البند الفرعي"، البلديات بعمود "البند"): "العقود
            // الاستثمارية"/"التأجير المباشر" = تصنيف "استثمار"، و"التأجير المؤقت" = تصنيف "تأجير
            // مؤقت" - بقية البنود الرئيسية تطابق تصنيفًا واحدًا فقط (تفاصيلها الفرعية كلها تندرج
            // تحت نفس التصنيف، فتُجمَع معًا بلا تفريق). لازم التفريق هنا بالتحديد لأن التجميع
            // القديم (حسب "البند الرئيسي" فقط) كان يعطي "استثمار" و"تأجير مؤقت" نفس المستهدف
            // المُجمَّع للاثنين معًا (خطأ اكتشفته المستخدمة: مستهدف "استثمار" وحده للأمانة من
            // ملف المالية = 307,717,614 فقط، وليس 313 مليون المجمّعة مع تأجير مؤقت أيضًا)
            var allFinanceItemTargets = await _db.FinanceItemTargets.AsNoTracking().ToListAsync();

            var financeTargetByScope = allFinanceItemTargets
                .Where(t => t.TargetSource == "Finance")
                .GroupBy(t => t.Scope)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(t => ResolveFinanceClassification(t.MainItem, t.SubItem))
                          .Where(cg => cg.Key != null)
                          .ToDictionary(cg => cg.Key!, cg => cg.Sum(t => t.AnnualTarget)));

            // مستهدف وزارة البلديات والإسكان (سنوي فقط) - من نفس ملف "المستهدف السنوي"، نفس
            // أسماء البنود الرئيسية بالضبط المستخدَمة بمصدر المالية أعلاه (لأنهما من نفس صفوف
            // نفس الملف)، فيُستخدَم ResolveFinanceClassification نفسه بدل خريطة/تسميات منفصلة
            // كانت تُنتج مطابقة جزئية غير موثوقة (بعض البنود تُحسَب صح وبعضها لا)
            var housingTargetByScope = allFinanceItemTargets
                .Where(t => t.TargetSource == "BaladiyahHousing")
                .GroupBy(t => t.Scope)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(t => ResolveFinanceClassification(t.MainItem, t.SubItem))
                          .Where(cg => cg.Key != null)
                          .ToDictionary(cg => cg.Key!, cg => cg.Sum(t => t.AnnualTarget)));

            result.RevenueTypePerformance = records
                .Where(r => !string.IsNullOrWhiteSpace(r.Classification))
                .GroupBy(r => r.Classification!.Trim())
                .Select(g =>
                {
                    var classification = g.Key;
                    var hasAmanah = g.Any(x => IsAmanah(x.Entity));
                    var hasBaladiyah = g.Any(x => !IsAmanah(x.Entity) && !string.IsNullOrWhiteSpace(x.Entity));

                    decimal targetFinance = 0;
                    if (hasAmanah && financeTargetByScope.TryGetValue("Amanah", out var fAmanah) && fAmanah.TryGetValue(classification, out var fa))
                        targetFinance += fa;
                    if (hasBaladiyah && financeTargetByScope.TryGetValue("Baladiyah", out var fBaladiyah) && fBaladiyah.TryGetValue(classification, out var fb))
                        targetFinance += fb;

                    decimal targetBaladiyah = 0;
                    if (hasAmanah && housingTargetByScope.TryGetValue("Amanah", out var hAmanah) && hAmanah.TryGetValue(classification, out var ha))
                        targetBaladiyah += ha;
                    if (hasBaladiyah && housingTargetByScope.TryGetValue("Baladiyah", out var hBaladiyah) && hBaladiyah.TryGetValue(classification, out var hb))
                        targetBaladiyah += hb;

                    return new RevenueTypePerformanceRow
                    {
                        ItemLabel = classification,
                        Invoiced = g.Sum(x => x.TotalValue ?? 0),
                        Collected = g.Sum(x => x.CollectedValue ?? 0),
                        TargetFinance = targetFinance,
                        TargetBaladiyah = targetBaladiyah
                    };
                })
                .Where(x => x.Invoiced > 0)
                .OrderByDescending(x => x.Invoiced)
                .ToList();

            // ==================== بطاقات "المحصل الحالي/سنة الأساس ونسبة النمو" ====================
            // "الحالية" = أحدث سنة موجودة فعليًا ببيانات DashboardRecords (لا تتغيّر). "سنة الأساس"
            // (RevenueBaselineYear) تُقرَأ من جدول منفصل تمامًا (RevenueBaselineCollectedItems)
            // بدل بيانات تلك السنة داخل DashboardRecords نفسها - مصدرها أدق حسب تأكيد المستخدمة.
            // يُصفّى مجموع سنة الأساس حسب نفس تصنيفات البند الظاهرة بالنطاق الحالي (records) حتى
            // يستجيب لفلتر البند بالفلاتر تلقائيًا بلا أي طلب/منطق إضافي (تمامًا كأي بطاقة أخرى
            // مبنية من records). إن لم تُرفَع بيانات سنة الأساس بعد، يُستخدم احتياطيًا نفس حساب
            // DashboardRecords القديم (سنة سابقة = سنة حالية - 1) حتى لا تظهر بطاقة فارغة.
            var recordsWithYear = records.Where(r => r.RecordDate.HasValue).ToList();
            if (recordsWithYear.Count > 0)
            {
                var currentYear = recordsWithYear.Max(r => r.RecordDate!.Value.Year);
                result.RevenueCurrentYear = currentYear;
                var currentYearRecords = recordsWithYear.Where(r => r.RecordDate!.Value.Year == currentYear).ToList();
                result.RevenueCurrentYearCollected = currentYearRecords.Sum(r => r.CollectedValue ?? 0);

                // آخر شهر متوفّر فعليًا بالسنة الحالية (2026) - نُقيَّد بطاقة "محصل 2025" بنفس عدد
                // الأشهر هذا (بدل سنة الأساس كاملة) حتى تبقى المقارنة عادلة بين سنة كاملة (2025)
                // وسنة جزئية لسه بمنتصفها (2026) - بطلب صريح من المستخدمة
                var currentYearMaxMonth = currentYearRecords.Max(r => r.RecordDate!.Value.Month);

                var classificationsInScope = records
                    .Where(r => !string.IsNullOrWhiteSpace(r.Classification))
                    .Select(r => r.Classification!.Trim())
                    .Distinct()
                    .ToList();
                var baselineItems = await _db.RevenueBaselineCollectedItems.AsNoTracking()
                    .Where(b => classificationsInScope.Contains(b.Classification) && b.Month <= currentYearMaxMonth)
                    .ToListAsync();

                if (baselineItems.Count > 0)
                {
                    result.RevenuePreviousYear = RevenueBaselineYear;
                    result.RevenuePreviousYearCollected = baselineItems.Sum(b => b.CollectedValue);
                }
                else
                {
                    var previousYear = currentYear - 1;
                    result.RevenuePreviousYear = previousYear;
                    result.RevenuePreviousYearCollected = recordsWithYear.Where(r => r.RecordDate!.Value.Year == previousYear).Sum(r => r.CollectedValue ?? 0);
                }

                result.RevenueGrowthPercent = result.RevenuePreviousYearCollected > 0
                    ? Math.Round((result.RevenueCurrentYearCollected - result.RevenuePreviousYearCollected) / result.RevenuePreviousYearCollected * 100, 1)
                    : 0;

                // ==================== تفصيل المحصّل حسب أساس الاستحقاق ====================
                // "أساس استحقاق": المحصّل من سجلات أُنشئت (CreatedDate) بنفس السنة الحالية.
                // "محصل من مديونيات": المحصّل من سجلات دُفعت (RecordDate/تاريخ الدفع) بالسنة
                // الحالية لكن أُنشئت بسنة أسبق (مديونية سابقة حُصّلت هذه السنة).
                // "محصل أوامر تنفيذ": لا تاريخ إنشاء مسجَّل أصلاً لهذه السجلات، فيُعتمَد تاريخ
                // الدفع فقط لحساب مجموعها المحصَّل.
                var accrualCollected = records
                    .Where(r => r.CreatedDate.HasValue && r.CreatedDate.Value.Year == currentYear)
                    .Sum(r => r.CollectedValue ?? 0);
                var fromDebtsCollected = records
                    .Where(r => r.RecordDate.HasValue && r.RecordDate.Value.Year == currentYear
                        && r.CreatedDate.HasValue && r.CreatedDate.Value.Year < currentYear)
                    .Sum(r => r.CollectedValue ?? 0);
                var executionOrdersCollected = records
                    .Where(r => !r.CreatedDate.HasValue)
                    .Sum(r => r.CollectedValue ?? 0);

                result.RevenueBasisBreakdown = new List<RevenueBasisBreakdownRow>
                {
                    new() { CaseLabel = "أساس استحقاق", CollectedValue = accrualCollected },
                    new() { CaseLabel = "محصل من مديونيات", CollectedValue = fromDebtsCollected },
                    new() { CaseLabel = "محصل أوامر تنفيذ", CollectedValue = executionOrdersCollected }
                };
                result.RevenueBasisBreakdownTotal = accrualCollected + fromDebtsCollected + executionOrdersCollected;
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

            // الإيرادات ربع السنوية لكل جهة (أمانة المدينة المنورة نفسها + كل بلدية) بالاعتماد
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
                    // الرسم يعرض "المحصّل" فقط (لا الإجمالي) بناءً على طلب صريح
                    Quarterly = g.GroupBy(x => new { x.RecordDate!.Value.Year, Quarter = ((x.RecordDate!.Value.Month - 1) / 3) + 1 })
                        .Select(qg => new
                        {
                            Label = $"{qg.Key.Year}-Q{qg.Key.Quarter}",
                            Collected = qg.Sum(x => x.CollectedValue ?? 0)
                        })
                        .ToList()
                })
                .OrderByDescending(x => x.Total)
                .Take(20) // سقف يمنع ازدحام الرسم إن وُجدت جهات كثيرة جداً بالبيانات
                .ToList();

            var entityQuarters = byEntity
                .SelectMany(e => e.Quarterly.Select(q => q.Label))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            // تسميات محور الأرباع تُعرض "Q1".."Q4" (بلا سنة) — نفس تسميات رسمة "توزيع الإيرادات
            // حسب الأرباع" الموجودة أصلاً بنفس الصفحة، بينما يبقى الترتيب الزمني الصحيح معتمدًا
            // على المفتاح الداخلي "yyyy-Qn"
            result.EntityMonthLabels = entityQuarters
                .Select(q => "Q" + q.Substring(q.IndexOf('Q') + 1))
                .ToList();
            result.EntityMonthlySeries = byEntity
                .Select(e =>
                {
                    var quarterlyMap = e.Quarterly.ToDictionary(q => q.Label, q => q.Collected);
                    return new EntityMonthlySeries
                    {
                        Entity = e.Entity,
                        Values = entityQuarters.Select(q => quarterlyMap.TryGetValue(q, out var v) ? v : 0m).ToList()
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
            IQueryable<DashboardRecord> query = _db.DashboardRecords.AsNoTracking();
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
                var invoicedByEntity = await _db.DashboardRecords.AsNoTracking()
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
            // من "البند الرئيسي" إلى قيم "تصنيف البند" الموازية له بـ Book112.xlsx (BaladiyahMainItemToClassifications أدناه).
            // ==================================================================
            var annualTargetByMainItem = monthlyRows
                .Where(t => !string.IsNullOrWhiteSpace(t.MainItem))
                .GroupBy(t => t.MainItem!)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.M1 + t.M2 + t.M3 + t.M4 + t.M5 + t.M6 + t.M7 + t.M8 + t.M9 + t.M10 + t.M11 + t.M12));

            result.ItemsMainAnnual = BaladiyahMainItemToClassifications.Select(kv =>
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

        // بند "إيرادات استثمار العقارات البلدية" فقط ينقسم فعليًا إلى تصنيفين مختلفين بكلا
        // ملفَي المستهدف (المالية بعمود "البند الفرعي"، البلديات بعمود "البند"): "العقود
        // الاستثمارية"/"التأجير المباشر" = تصنيف "استثمار"، و"التأجير المؤقت" = تصنيف "تأجير
        // مؤقت" - بقية البنود الرئيسية تطابق تصنيفًا واحدًا فقط. مستخدَمة بكل من
        // GetOverviewDataAsync وGetTargetAchievementAsync (صفحة "الملخص التنفيذي" الجديدة)
        private const string InvestmentMainItemFinance = "إيرادات استثمار العقارات البلدية";
        private const string TemporaryLeaseSubItem = "التأجير المؤقت";

        // يُستخدَم لكلا مصدرَي المستهدف السنوي (المالية + البلديات والإسكان) لأن كلاهما من نفس
        // صفوف نفس ملف "المستهدف السنوي" بنفس تسميات البند الرئيسي بالضبط
        private static string? ResolveFinanceClassification(string mainItem, string? subItem)
        {
            if (mainItem == InvestmentMainItemFinance)
                return subItem != null && subItem.Trim() == TemporaryLeaseSubItem ? "تأجير مؤقت" : "استثمار";
            return FinanceMainItemToClassifications.TryGetValue(mainItem, out var arr) && arr.Length > 0 ? arr[0] : null;
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

        // خريطة "البند الرئيسي" كما بملف "المستهدف الشهري" للبلديات (Book112.xlsx) ← قيم
        // "تصنيف البند" الموازية له - نفس فكرة الخريطة أعلاه لكن بصياغة عربية مختلفة قليلاً
        // (مصدر بيانات مختلف). مُستخدَمة بـ GetTargetsComparisonDataAsync (ItemsMainAnnual)
        // وبـ BuildResult (RevenueTypePerformance - مصدر مستهدف "البلديات")
        private static readonly Dictionary<string, string[]> BaladiyahMainItemToClassifications = new()
        {
            ["إيرادات استثمارات العقارات البلدية"] = new[] { "استثمار", "تأجير مؤقت" },
            ["إيرادات الرسوم والمقابلات المالية"] = new[] { "رسوم" },
            ["إيرادات رسم إشغال مرافق الإيواء"] = new[] { "رسوم المبيعات-الايواء" },
            ["إيرادات الجزاءات والغرمات"] = new[] { "جزاءات" },
            ["إيرادات رسوم تقديم منتجات التبغ"] = new[] { "رسوم المبيعات-التبغ" },
            ["الإيرادات المختلفة"] = new[] { "إيرادات مختلفة" },
        };

        public async Task<FinanceTargetsPageData> GetFinanceTargetsPageDataAsync()
        {
            var targetRows = await _db.FinanceItemTargets.AsNoTracking()
                .Where(t => t.TargetSource == "Finance")
                .ToListAsync();
            var result = new FinanceTargetsPageData { HasData = targetRows.Count > 0 };
            if (!result.HasData) return result;

            var records = await _db.DashboardRecords.AsNoTracking()
                .Select(r => new { r.Entity, r.Classification, r.CollectedValue, r.Quarter })
                .ToListAsync();

            var amanahRecords = records.Where(r => IsAmanah(r.Entity)).ToList();
            var baladiyahRecords = records.Where(r => !IsAmanah(r.Entity)).ToList();

            List<decimal> QuarterlyCollected(IEnumerable<(string? Quarter, decimal? CollectedValue)> recs)
            {
                var byQuarter = recs
                    .Where(r => !string.IsNullOrWhiteSpace(r.Quarter))
                    .GroupBy(r => r.Quarter!.Trim().ToUpperInvariant())
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.CollectedValue ?? 0));
                return new[] { "Q1", "Q2", "Q3", "Q4" }
                    .Select(q => byQuarter.TryGetValue(q, out var v) ? v : 0m)
                    .ToList();
            }

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

            result.AmanahQuarterlyCollected = QuarterlyCollected(amanahRecords.Select(r => (r.Quarter, r.CollectedValue)));
            result.BaladiyahQuarterlyCollected = QuarterlyCollected(baladiyahRecords.Select(r => (r.Quarter, r.CollectedValue)));

            return result;
        }

        /// <summary>
        /// قسم "تحقيق المستهدف" بصفحة "الملخص التنفيذي": البند الرئيسي/الفرعي هنا هما نفس عمودي
        /// "تصنيف البند"/"البند" (Classification/Category) المستخدَمين بفلتر الصفحة الرئيسية
        /// (البند الرئيسي = "استثمار" مثلاً، الفرعي = "ريع الأراضي" تحته) - لا علاقة لهما بأسماء
        /// FinanceItemTarget.MainItem/SubItem الأطول (تُستخدَم داخليًا فقط عبر ResolveFinanceClassification
        /// لتحديد أي صفوف FinanceItemTarget تخص "تصنيف البند" المختار). المحصّل/المفوتر من
        /// DashboardRecords تُفلتَر مباشرة بـ Classification/Category (+ Entity حسب النطاق
        /// المختار عبر IsAmanah). قوائم البند الرئيسي/الفرعي بالفلتر مصدرها جدول Sections نفسه
        /// المستخدَم بشجرة فلتر الصفحة الرئيسية (البند الرئيسي = قسم بلا أب، الفرعي = أبناؤه
        /// المباشرون) - Section.NameAr يطابق نص Classification/Category بالضبط.
        /// </summary>
        public async Task<TargetAchievementData> GetTargetAchievementAsync(string? scope, string? mainItem, string? subItem, string? targetSource, int? year)
        {
            var records = await _db.DashboardRecords.AsNoTracking()
                .Where(r => r.Classification != null && r.Classification != "")
                .Select(r => new { r.Entity, r.Classification, r.Category, r.CollectedValue, r.TotalValue, r.RecordDate, r.CreatedDate })
                .ToListAsync();

            var matching = records.Where(r =>
                (string.IsNullOrWhiteSpace(scope) || scope == "All" || (scope == "Amanah" ? IsAmanah(r.Entity) : !IsAmanah(r.Entity))) &&
                (string.IsNullOrWhiteSpace(mainItem) || string.Equals(r.Classification!.Trim(), mainItem.Trim(), StringComparison.Ordinal)) &&
                (string.IsNullOrWhiteSpace(subItem) || string.Equals((r.Category ?? "").Trim(), subItem.Trim(), StringComparison.Ordinal))
            ).ToList();

            var result = new TargetAchievementData { HasData = matching.Count > 0 };

            var allTargetRows = await _db.FinanceItemTargets.AsNoTracking().ToListAsync();
            var targetRows = allTargetRows.Where(t =>
                (string.IsNullOrWhiteSpace(scope) || scope == "All" || t.Scope == scope) &&
                (string.IsNullOrWhiteSpace(targetSource) || targetSource == "All" || t.TargetSource == targetSource) &&
                (string.IsNullOrWhiteSpace(mainItem) || ResolveFinanceClassification(t.MainItem, t.SubItem) == mainItem.Trim())
            ).ToList();
            result.Target = targetRows.Sum(t => t.AnnualTarget);
            if (result.Target > 0) result.HasData = true;
            if (!result.HasData) return result;

            if (year.HasValue)
            {
                result.Collected = matching.Where(r => r.RecordDate.HasValue && r.RecordDate.Value.Year == year.Value).Sum(r => r.CollectedValue ?? 0);
                result.Invoiced = matching.Where(r => r.CreatedDate.HasValue && r.CreatedDate.Value.Year == year.Value).Sum(r => r.TotalValue ?? 0);
            }
            else
            {
                result.Collected = matching.Sum(r => r.CollectedValue ?? 0);
                result.Invoiced = matching.Sum(r => r.TotalValue ?? 0);
            }

            var achievedValue = targetSource switch
            {
                "Finance" => result.Collected,
                "BaladiyahHousing" => result.Invoiced,
                _ => result.Collected + result.Invoiced
            };
            result.AchievementPercent = result.Target > 0 ? Math.Round(achievedValue / result.Target * 100, 1) : 0;

            return result;
        }

        /// <summary>
        /// البند الرئيسي = قسم (Section) بلا أب ضمن النطاق المختار، الفرعي = أبناؤه المباشرون -
        /// نفس شجرة فلتر الصفحة الرئيسية (buildHomeFilters بـ section-dashboard.js) بالضبط، بما
        /// فيها دمج البنود متطابقة الاسم بين الأمانة والبلديات باسم واحد عند عدم تحديد نطاق
        /// (بدل ظهورها مكرَّرة مرتين) - Section.NameAr هو نص Classification/Category نفسه.
        /// </summary>
        public async Task<TargetFilterOptionsData> GetTargetFilterOptionsAsync(string? scope)
        {
            var sectionsQuery = _db.Sections.AsNoTracking().Where(s => s.IsActive);
            if (scope == "Amanah") sectionsQuery = sectionsQuery.Where(s => s.Scope == ScopeType.Amanah);
            else if (scope == "Baladiyah") sectionsQuery = sectionsQuery.Where(s => s.Scope == ScopeType.Baladiyah);
            var sections = await sectionsQuery.Select(s => new { s.Id, s.NameAr, s.ParentSectionId }).ToListAsync();

            var mainSections = sections.Where(s => s.ParentSectionId == null).ToList();
            var mainNames = mainSections.Select(s => s.NameAr.Trim()).Distinct().OrderBy(n => n).ToList();

            var result = new TargetFilterOptionsData { MainItems = mainNames };
            foreach (var mainName in mainNames)
            {
                var mainIds = mainSections.Where(s => s.NameAr.Trim() == mainName).Select(s => s.Id).ToHashSet();
                var subs = sections
                    .Where(s => s.ParentSectionId.HasValue && mainIds.Contains(s.ParentSectionId.Value))
                    .Select(s => s.NameAr.Trim())
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();
                if (subs.Count > 0) result.SubItemsByMainItem[mainName] = subs;
            }

            var yearRecords = await _db.DashboardRecords.AsNoTracking()
                .Where(r => r.RecordDate.HasValue)
                .Select(r => new { r.Entity, r.RecordDate })
                .ToListAsync();
            var yearsFiltered = scope == "Amanah" ? yearRecords.Where(r => IsAmanah(r.Entity))
                : scope == "Baladiyah" ? yearRecords.Where(r => !IsAmanah(r.Entity))
                : yearRecords;
            result.Years = yearsFiltered
                .Select(r => r.RecordDate!.Value.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            return result;
        }

        // حالة العقد التي تعني "مبرومة" بلغة صفحة الطرح
        private const string FinalizedStatus = "تم إبرام العقد";

        // حالة العقد التي تعني "ملغاة" بلغة صفحة الطرح (تشمل عمليًا "لم يتقدم أحد" أيضًا -
        // تُدمَج بهذه الحالة عند الاستيراد، انظر ContractImportService.MergedIntoCancelled)
        private const string CancelledStatus = "إلغاء المنافسة";

        public async Task<TarhPageData> GetTarhDataAsync(string? department, string? status, string? contractNumber, int? year, int? quarter, string? forcedDepartment = null)
        {
            var all = await _db.ContractRecords.AsNoTracking().ToListAsync();
            var result = new TarhPageData { HasData = all.Count > 0 };
            if (!result.HasData) return result;

            result.DepartmentOptions = all.Select(r => r.GeneralDepartment).Where(d => !string.IsNullOrWhiteSpace(d)).Distinct().OrderBy(d => d).ToList()!;
            result.StatusOptions = all.Select(r => r.Status).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s).ToList()!;
            result.YearOptions = all.Select(r => r.ContractYear).Where(y => y.HasValue).Select(y => y!.Value).Distinct().OrderByDescending(y => y).ToList();

            var filtered = TarhFilter(all, department, status, contractNumber, year, quarter);

            // بلا فلترَي الحالة والسنة - يُستخدم للاتجاه الزمني لتوقيع العقود المبرومة عبر
            // السنوات (يفرض حالة "مبرومة" دائمًا)؛ فلترته بسنة واحدة يُفرغ الرسم من معناه (نقطة
            // واحدة بلا خط يصل بينها)، لذا يبقى معروضًا لكل السنوات مهما كانت السنة المختارة
            var yearlyTrendBase = TarhFilter(all, department, null, contractNumber, null, quarter);

            // "إجمالي عدد الفرص" (كرت KPI الأول): إجمالي ثابت دائمًا (بلا أي تأثير لفلاتر الصفحة
            // - سنة/ربع/بحث) بطلب صريح من المستخدمة، لأنها تتحقق منه بعدّ عمود "رقم الفرصة"
            // كاملاً بإكسل مباشرة بلا أي فلترة
            result.TotalOpportunities = all
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

            // قيمة وعدد العقود المبرومة حسب الإدارة العامة (رسم 1) والمحفظة الفرعية ضمن كل إدارة
            // (رسم 2)، وكروت أنواع العقود (استثمار/الأبراج/تأجير مباشر/الممرات) - عقود مبرومة
            // فقط ("رقم العقد"/"سنة العقد" لا تتوفر إلا لهذه الحالة أصلاً)، وبسنة 2026 دائمًا
            // (مقفولة، بصرف النظر عن فلتر السنة المختار بأعلى الصفحة) بطلب المستخدمة - فلتر
            // الربع يبقى يؤثر عليها كالمعتاد
            var deptBase = TarhFilter(all, null, FinalizedStatus, contractNumber, 2026, quarter);

            // كروت أنواع العقود: القيمة = "الأجرة السنوية" حصرًا (بلا أي احتمال للرجوع لـ"قيمة
            // العقد") - بطلب صريح من المستخدمة، بعد ما وضّحت طريقتها بإكسل: تفلتر نوع العقد ثم
            // الحالة "تم إبرام العقد" ثم السنة 2026 ثم تجمع عمود "الأجرة السنوية" حرفيًا
            result.TypeSummary = deptBase
                .Where(r => !string.IsNullOrWhiteSpace(r.ContractType))
                .GroupBy(r => r.ContractType!)
                .Select(g => new TarhTypeSummaryRow { TypeLabel = g.Key, Count = DistinctCount(g), TotalValue = g.Sum(r => r.AnnualRent ?? 0) })
                .OrderByDescending(x => x.Count)
                .ToList();

            result.TotalCount = DistinctCount(filtered);
            result.FinalizedCount = DistinctCount(filtered.Where(r => r.Status == FinalizedStatus));
            result.FinalizedPercent = result.TotalCount > 0
                ? Math.Round((decimal)result.FinalizedCount / result.TotalCount * 100, 1) : 0;

            // رسما 1+2 (توزيع الإدارات العامة/الفرعية): عدد وقيمة "الفرص" (لا العقود) التي فُتحت
            // مظاريفها (=طُرحت) بسنة 2026 بالضبط - بلا قيد الحالة "تم إبرام العقد" (فرصة لسه
            // بالإجراءات أو حتى ملغاة تُحسَب أيضًا، طالما طُرحت بـ2026) - بطلب صريح من المستخدمة
            // بعد ما وضّحت إنها تقصد "الفرص اللي انطرحت بعام 2026" لا "العقود المبرومة بعام 2026"
            // (deptBase أعلاه يبقى كما هو، يُستخدم فقط لكروت أنواع العقود ونسبة الإبرام)
            var tenderedDeptBase = all.Where(r => r.EnvelopeOpeningDate.HasValue && r.EnvelopeOpeningDate.Value.Year == 2026);
            if (quarter.HasValue) tenderedDeptBase = tenderedDeptBase.Where(r => r.Quarter == quarter);
            if (!string.IsNullOrWhiteSpace(contractNumber))
                tenderedDeptBase = tenderedDeptBase.Where(r => (r.OpportunityNumber ?? "").Contains(contractNumber, StringComparison.OrdinalIgnoreCase));
            var tenderedDeptBaseList = tenderedDeptBase.ToList();

            // موظف مقيّد بإدارة عامة واحدة: يقيّد رسمَي "حسب الإدارة العامة/الفرعية" على إدارته
            // فقط (خلاف department العادي أعلاه، هذان الرسمان مبنيان عمدًا على all بلا تصفية
            // إدارة لإتاحة مقارنة الإدارات لمستخدم غير مقيّد - forcedDepartment يكسر هذا التجاوز
            // لموظف مقيّد تحديدًا)
            if (!string.IsNullOrWhiteSpace(forcedDepartment))
                tenderedDeptBaseList = tenderedDeptBaseList.Where(r => r.GeneralDepartment == forcedDepartment).ToList();

            int DistinctOpportunityCount(IEnumerable<ContractRecord> recs) =>
                recs.Select(r => r.OpportunityNumber).Where(o => !string.IsNullOrWhiteSpace(o)).Distinct().Count();

            result.DeptSummary = tenderedDeptBaseList
                .Where(r => !string.IsNullOrWhiteSpace(r.GeneralDepartment))
                .GroupBy(r => r.GeneralDepartment!)
                .Select(g => new TarhDeptSummaryRow { DepartmentLabel = g.Key, Count = DistinctOpportunityCount(g), TotalValue = g.Sum(EffectiveValue) })
                .OrderByDescending(x => x.TotalValue)
                .ToList();

            // قيمة العقود المبرومة 2026 حسب الإدارة العامة (منفصل عن DeptSummary أعلاه، اللي
            // مبني من الفرص المطروحة لا المبرومة تحديدًا) - يغذّي رسم "الملخص التنفيذي" الجديد
            result.FinalizedByDepartment = deptBase
                .Where(r => !string.IsNullOrWhiteSpace(r.GeneralDepartment))
                .GroupBy(r => r.GeneralDepartment!)
                .Select(g => new TarhDeptSummaryRow { DepartmentLabel = g.Key, Count = DistinctCount(g), TotalValue = g.Sum(r => r.AnnualRent ?? 0) })
                .OrderByDescending(x => x.TotalValue)
                .ToList();

            result.TotalOpportunitiesValue = tenderedDeptBaseList.Sum(EffectiveValue);

            // نسبة الإبرام لكل نوع عقد = مبروم 2026 من هذا النوع (TypeSummary أعلاه) ÷ إجمالي هذا
            // النوع بكل البيانات بلا أي فلتر سنة/حالة (نفس فلسفة TotalOpportunities: "تاريخ فتح
            // المظاريف" غير مُعبَّأ لكل الأنواع، فلو اعتمدنا فتح المظاريف بسنة 2026 حصرًا كمرجع
            // تختفي الأنواع غير الاستثمارية من الرسم بالكامل). القائمة مبنية من TypeSummary نفسها
            // (لا تُنشئ أنواعًا جديدة) حتى تبقى نفس الأنواع الأربعة الظاهرة بصفحة "الطرح" بالضبط
            // (استثمار/الأبراج/الممرات/تأجير مباشر).
            // نوع العقد "استثمار" فقط يمرّ فعليًا بمرحلة طرح/فتح مظاريف قبل الإبرام (بيانات
            // "تاريخ فتح المظاريف" معبّأة له حصرًا) - بقية الأنواع (الأبراج/الممرات/تأجير مباشر)
            // كل صفوفها بالبيانات الحالية حالتها "تم إبرام العقد" مباشرة بلا مرحلة طرح منفصلة
            // (تأكّدتُ منها بفحص البيانات: HasEnvelope=0 لكل صفوف هذه الأنواع الثلاثة). لذا:
            // - النوع اللي له فتح مظاريف: "عدد الفرص" = فُتح مظروفها بسنة 2026 بالضبط
            // - بقية الأنواع: "الفرصة" = "العقد المبرم" نفسه (نفس TypeSummary أعلاه، 2026 بالضبط)
            // كل هذا يبقي صف 1 بصفحة "الملخص التنفيذي" بيانات 2026 فقط بكل الأنواع الأربعة معًا
            // بطلب صريح من المستخدمة (بخلاف TotalOpportunities كل السنوات المستخدَم بصفحة الطرح نفسها)
            var typesWithTendering = all
                .Where(r => !string.IsNullOrWhiteSpace(r.ContractType) && r.EnvelopeOpeningDate.HasValue)
                .Select(r => r.ContractType!)
                .ToHashSet();

            result.TypeTenderedSummary = result.TypeSummary.Select(t =>
            {
                if (typesWithTendering.Contains(t.TypeLabel))
                {
                    var tendered2026 = all.Where(r => r.ContractType == t.TypeLabel
                        && r.EnvelopeOpeningDate.HasValue && r.EnvelopeOpeningDate.Value.Year == 2026).ToList();
                    var tenderedCount = DistinctOpportunityCount(tendered2026);
                    var tenderedValue = tendered2026.Sum(EffectiveValue);
                    return new TarhTypeTenderedRow
                    {
                        TypeLabel = t.TypeLabel,
                        TenderedCount = tenderedCount,
                        TenderedValue = tenderedValue,
                        FinalizedCount = t.Count,
                        FinalizedValue = t.TotalValue,
                        FinalizedPercent = tenderedCount > 0 ? Math.Round((decimal)t.Count / tenderedCount * 100, 1) : 0
                    };
                }
                return new TarhTypeTenderedRow
                {
                    TypeLabel = t.TypeLabel,
                    TenderedCount = t.Count,
                    TenderedValue = t.TotalValue,
                    FinalizedCount = t.Count,
                    FinalizedValue = t.TotalValue,
                    FinalizedPercent = 100
                };
            })
            .OrderByDescending(x => x.TenderedCount)
            .ToList();

            result.FinalizedByPortfolio = tenderedDeptBaseList
                .Where(r => !string.IsNullOrWhiteSpace(r.GeneralDepartment) && !string.IsNullOrWhiteSpace(r.SubPortfolio))
                .GroupBy(r => new { r.GeneralDepartment, r.SubPortfolio })
                .Select(g => new TarhFinalizedByPortfolioRow
                {
                    DepartmentLabel = g.Key.GeneralDepartment!, PortfolioLabel = g.Key.SubPortfolio!,
                    Count = DistinctOpportunityCount(g), TotalValue = g.Sum(EffectiveValue)
                })
                .OrderBy(x => x.DepartmentLabel).ThenByDescending(x => x.TotalValue)
                .ToList();

            // نسب الطرح والإبرام والإلغاء التراكمية حسب السنة (رسم 3) - اتجاه زمني شامل بلا أي فلاتر.
            // المرجع (100%) = إجمالي عدد الفرص التي فُتحت مظاريفها (=طُرحت) عبر كل السنوات؛ نسبتا
            // الإبرام والإلغاء تُحسَبان من نفس المرجع (لا من إجمالياتهما الخاصة) حتى تبقى النسب الثلاث
            // قابلة للمقارنة المباشرة على نفس المقياس (كم من إجمالي المطروح آل لكل مصير).
            // نسبة الإلغاء تُبنى بتاريخ فتح المظاريف (لا سنة العقد، لأن الفرصة الملغاة أصلًا لم يُبرَم
            // لها عقد) وتشمل حالة "إلغاء المنافسة" فقط، التي تُدمَج بها "لم يتقدم أحد" عند الاستيراد
            var tenderedRows = all.Where(r => r.EnvelopeOpeningDate.HasValue).ToList();
            var totalTendered = DistinctCount(tenderedRows);
            var rateYears = tenderedRows.Select(r => r.EnvelopeOpeningDate!.Value.Year)
                .Union(all.Where(r => r.Status == FinalizedStatus && r.ContractYear.HasValue).Select(r => r.ContractYear!.Value))
                .Distinct().OrderBy(y => y).ToList();

            decimal cumTendered = 0, cumFinalized = 0, cumCancelled = 0;
            result.YearlyRates = rateYears.Select(y =>
            {
                cumTendered += DistinctCount(tenderedRows.Where(r => r.EnvelopeOpeningDate!.Value.Year == y));
                cumFinalized += DistinctCount(all.Where(r => r.Status == FinalizedStatus && r.ContractYear == y));
                cumCancelled += DistinctCount(tenderedRows.Where(r => r.Status == CancelledStatus && r.EnvelopeOpeningDate!.Value.Year == y));
                return new TarhYearlyRateRow
                {
                    Year = y,
                    TenderedPercent = totalTendered > 0 ? Math.Round(cumTendered / totalTendered * 100, 1) : 0,
                    FinalizedPercent = totalTendered > 0 ? Math.Round(cumFinalized / totalTendered * 100, 1) : 0,
                    CancelledPercent = totalTendered > 0 ? Math.Round(cumCancelled / totalTendered * 100, 1) : 0
                };
            }).ToList();

            // الاتجاه الزمني لتوقيع العقود المبرومة (بلا فلترَي حالة وسنة - مفروضة "مبرومة" هنا دومًا)
            result.YearlyFinalizedTrend = yearlyTrendBase
                .Where(r => r.Status == FinalizedStatus && r.ContractYear.HasValue)
                .GroupBy(r => r.ContractYear!.Value)
                .Select(g => new TarhYearRow { Year = g.Key, Count = DistinctCount(g) })
                .OrderBy(x => x.Year)
                .ToList();

            // فتح المظاريف حسب السنة والحالة: يُبنى من "فتح المظاريف" (متوفر لكل الحالات تقريبًا،
            // بخلاف "سنة العقد" المتوفرة للعقود المبرومة فقط) - بلا فلترَي سنة/حالة لأن السنة
            // والحالة هما بُعدا الرسم نفسه؛ يُطبَّق فلترا الإدارة/رقم العقد فقط
            var envelopeBase = TarhFilter(all, department, null, contractNumber, null, null);
            result.EnvelopeByYearStatus = envelopeBase
                .Where(r => r.EnvelopeOpeningDate.HasValue && !string.IsNullOrWhiteSpace(r.Status))
                .GroupBy(r => new { Year = r.EnvelopeOpeningDate!.Value.Year, r.Status })
                .Select(g => new TarhEnvelopeStatusRow { Year = g.Key.Year, Status = g.Key.Status!, Count = DistinctCount(g) })
                .OrderBy(x => x.Year).ThenBy(x => x.Status)
                .ToList();

            // "تحت إجراءات الترسية": من بين الفرص بإحدى الحالات الثلاث قيد الإجراء التي فُتحت
            // مظاريفها بسنة 2026 بالضبط - كم تجاوز 30 يوم منذ فتح المظاريف (أحمر) وكم ضمن 30 يوم
            // (أخضر)، والنسبة بالمنتصف = المتجاوزة ÷ إجمالي الفرص التي فُتحت مظاريفها بـ2026 كلها
            // (بصرف النظر عن حالتها) × 100. نفس نطاق GetTarhOpportunityOptionsAsync بالضبط
            // (الحالات الثلاث + سنة فتح المظاريف 2026) حتى يبقى المعروض بالقوس وقائمة "تتبع
            // الفرصة" متطابقًا (بطلب صريح من المستخدمة، رجوعًا لحساب المدة باليوم بدل الحالة)
            var pendingBase = TarhFilter(all, department, null, contractNumber, null, null);
            var tenderedThisYear = pendingBase.Where(r => r.EnvelopeOpeningDate.HasValue && r.EnvelopeOpeningDate.Value.Year == 2026).ToList();
            var tenderedThisYearTotal = DistinctCount(tenderedThisYear);
            var pendingRows = tenderedThisYear.Where(r => r.Status != null && TarhPendingStatuses.Contains(r.Status)).ToList();
            var today = DateTime.UtcNow.Date;
            var exceededRows = pendingRows.Where(r => (today - r.EnvelopeOpeningDate!.Value.Date).TotalDays > 30).ToList();
            var withinRows = pendingRows.Where(r => (today - r.EnvelopeOpeningDate!.Value.Date).TotalDays <= 30).ToList();
            var exceededCount = DistinctCount(exceededRows);
            result.DurationStatus = new TarhDurationStatus
            {
                TotalCount = tenderedThisYearTotal,
                ExceededCount = exceededCount,
                WithinCount = DistinctCount(withinRows),
                ExceededPercent = tenderedThisYearTotal > 0 ? Math.Round((decimal)exceededCount / tenderedThisYearTotal * 100, 1) : 0,
                StatusBreakdown = pendingRows
                    .GroupBy(r => r.Status!)
                    .Select(g => new NameCountRow { Name = g.Key, Count = DistinctCount(g) })
                    .OrderByDescending(x => x.Count)
                    .ToList()
            };

            // دونات "الفرص المطروحة القادمة" - أسبوع/شهر قادم (من اليوم فصاعدًا، بصرف النظر عن
            // الحالة، بنفس نطاق pendingBase - فلترا الإدارة/رقم العقد فقط) + إجمالي 2026 (نفس
            // tenderedThisYearTotal أعلاه)
            var weekEnd = today.AddDays(7);
            var monthEnd = today.AddMonths(1);
            var upcomingBase = pendingBase.Where(r => r.EnvelopeOpeningDate.HasValue && r.EnvelopeOpeningDate.Value.Date >= today).ToList();
            result.UpcomingTenders = new TarhUpcomingTenders
            {
                NextWeekCount = DistinctCount(upcomingBase.Where(r => r.EnvelopeOpeningDate!.Value.Date <= weekEnd)),
                NextMonthCount = DistinctCount(upcomingBase.Where(r => r.EnvelopeOpeningDate!.Value.Date <= monthEnd)),
                Year2026Count = tenderedThisYearTotal
            };

            return result;
        }

        /// <summary>قائمة الفرص لتعبئة مربع اختيار "تتبع الفرصة" - بنفس نطاق رسمة "حالة مدة الفرص
        /// قيد الإجراء (2026)" بالضبط (الحالات الأربع + سنة فتح المظاريف 2026 فقط)، لأن الغرض من
        /// الزر تتبع نفس الفرص المعروضة بتلك الرسمة وليس كل الفرص بصرف النظر عن حالتها</summary>
        public async Task<List<TarhOpportunityOption>> GetTarhOpportunityOptionsAsync()
        {
            var all = await _db.ContractRecords.AsNoTracking().ToListAsync();
            // الربط مع DebtInvoiceRecords يتم عبر ContractIdRaw ("CONTRACT_ID") لا ContractNumber
            // ("رقم العقد") - عمود "رقم العقد" بملف المديونيات أرقام داخلية صغيرة لا تطابق صيغة
            // "CN-XXXXX" المستخدمة هنا (راجع تعليق DebtsAnalyticsService.FindByContractNumberAsync)
            var investorsByContract = await _db.DebtInvoiceRecords.AsNoTracking()
                .Where(r => !string.IsNullOrWhiteSpace(r.ContractIdRaw) && !string.IsNullOrWhiteSpace(r.InvestorName))
                .GroupBy(r => r.ContractIdRaw!)
                .ToDictionaryAsync(g => g.Key, g => g.Select(r => r.InvestorName!).First());
            var today = DateTime.UtcNow.Date;
            return all
                .Where(r => !string.IsNullOrWhiteSpace(r.OpportunityNumber) && r.Status != null &&
                            TarhPendingStatuses.Contains(r.Status) &&
                            r.EnvelopeOpeningDate.HasValue && r.EnvelopeOpeningDate.Value.Year == 2026)
                .Select(r =>
                {
                    var days = (today - r.EnvelopeOpeningDate!.Value.Date).TotalDays;
                    var durationStatus = days <= 30 ? "ضمن 30 يوم" : "تجاوزت 30 يوم";
                    return new TarhOpportunityOption
                    {
                        OpportunityNumber = r.OpportunityNumber!,
                        OpportunityName = r.OpportunityName,
                        InvestorName = r.InvestorName ?? (r.ContractNumber != null && investorsByContract.TryGetValue(r.ContractNumber, out var investorName) ? investorName : null),
                        ContractNumber = r.ContractNumber,
                        DurationStatus = durationStatus
                    };
                })
                .OrderBy(x => x.OpportunityNumber)
                .ToList();
        }

        /// <summary>رحلة فرصة واحدة (فتح المظاريف ← الترسية) برقم الفرصة. الفرص المعروضة بقائمة
        /// "تتبع الفرصة" كلها قيد الإجراء (بلا رقم عقد بعد) فلا نشترط وجوده هنا</summary>
        public async Task<TarhOpportunityTimeline> GetTarhOpportunityTimelineAsync(string opportunityNumber)
        {
            var record = await _db.ContractRecords.AsNoTracking().FirstOrDefaultAsync(r => r.OpportunityNumber == opportunityNumber);
            if (record == null)
                return new TarhOpportunityTimeline { Found = false };

            var investorName = record.InvestorName;
            if (string.IsNullOrWhiteSpace(investorName) && !string.IsNullOrWhiteSpace(record.ContractNumber))
                // الربط عبر ContractIdRaw ("CONTRACT_ID") لا ContractNumber ("رقم العقد") - راجع
                // تعليق DebtsAnalyticsService.FindByContractNumberAsync
                investorName = await _db.DebtInvoiceRecords.AsNoTracking()
                    .Where(r => r.ContractIdRaw == record.ContractNumber && !string.IsNullOrWhiteSpace(r.InvestorName))
                    .Select(r => r.InvestorName).FirstOrDefaultAsync();

            var timeline = new TarhOpportunityTimeline
            {
                Found = true,
                OpportunityNumber = record.OpportunityNumber,
                OpportunityName = record.OpportunityName,
                InvestorName = investorName,
                ContractNumber = record.ContractNumber,
                Status = record.Status,
                EnvelopeOpeningDate = record.EnvelopeOpeningDate,
                AwardDate = record.AwardDate,
                ContractDate = record.ContractDate
            };

            if (record.EnvelopeOpeningDate.HasValue && record.AwardDate.HasValue)
                timeline.EnvelopeToAwardDays = (int)(record.AwardDate.Value.Date - record.EnvelopeOpeningDate.Value.Date).TotalDays;

            if (record.AwardDate.HasValue && record.ContractDate.HasValue)
                timeline.AwardToContractDays = (int)(record.ContractDate.Value.Date - record.AwardDate.Value.Date).TotalDays;

            // إذا الفرصة أُبرِم عقدها فعلًا: المدة حتى تاريخ العقد. إذا لسه قيد الإجراء (بإحدى
            // الحالات الأربع): المدة حتى تاريخ اليوم - بنفس منطق رسمة "حالة مدة الفرص قيد الإجراء"
            if (record.EnvelopeOpeningDate.HasValue)
            {
                DateTime? endDate = record.ContractDate ??
                    (record.Status != null && TarhPendingStatuses.Contains(record.Status) ? DateTime.UtcNow.Date : (DateTime?)null);
                if (endDate.HasValue)
                {
                    timeline.TotalDays = (int)(endDate.Value.Date - record.EnvelopeOpeningDate.Value.Date).TotalDays;
                    timeline.DurationStatus = timeline.TotalDays <= 30 ? "ضمن 30 يوم" : "تجاوزت 30 يوم";
                }
            }

            return timeline;
        }

        /// <summary>قائمة الفرص التي فُتحت مظاريفها ضمن نطاق تاريخ معيّن - راجع توثيق الواجهة</summary>
        public async Task<List<TarhEnvelopeOpenedRow>> GetTarhEnvelopeOpenedAsync(DateTime start, DateTime end, string? forcedDepartment = null)
        {
            var startDate = start.Date;
            var endDate = end.Date;

            IQueryable<ContractRecord> query = _db.ContractRecords.AsNoTracking()
                .Where(r => r.EnvelopeOpeningDate.HasValue
                         && r.EnvelopeOpeningDate.Value.Date >= startDate
                         && r.EnvelopeOpeningDate.Value.Date <= endDate);

            if (!string.IsNullOrWhiteSpace(forcedDepartment))
                query = query.Where(r => r.GeneralDepartment == forcedDepartment);

            var rows = await query
                .OrderByDescending(r => r.EnvelopeOpeningDate)
                .Select(r => new TarhEnvelopeOpenedRow
                {
                    OpportunityNumber = r.OpportunityNumber,
                    OpportunityName = r.OpportunityName,
                    InvestorName = r.InvestorName,
                    GeneralDepartment = r.GeneralDepartment,
                    SubPortfolio = r.SubPortfolio,
                    Status = r.Status,
                    EnvelopeOpeningDate = r.EnvelopeOpeningDate,
                    Value = r.ContractValue ?? r.AnnualRent
                })
                .ToListAsync();

            return rows;
        }

        // الحالات الثلاث اللي تُعتبر "تحت إجراءات الترسية" - نطاق مشترك بين رسمة حالة مدة الفرص
        // وقائمة/رحلة "تتبع الفرصة" حتى يبقى المعروض بالاثنين متطابقًا. "تم فتح المظاريف" أُزيلت
        // بطلب صريح من المستخدمة - هي مرحلة سابقة لإجراءات الترسية نفسها، لا تُحسَب ضمنها
        private static readonly HashSet<string> TarhPendingStatuses = new()
            { "بإنتظار إعتماد العقد", "بإنتظار دفع الفاتورة", "تحت إجراءات الترسية" };

        // فلترة موحّدة لعقود صفحة "الطرح" حسب الإدارة العامة/الحالة/رقم العقد/السنة/الربع - يُستخدم
        // بكل من GetTarhDataAsync وGetTarhExportRowsAsync حتى تبقى نفس نتائج الصفحة والتصدير متطابقة
        // ملاحظة: الباراميتر اسمه "contractNumber" تاريخيًا (بكل الاستدعاءات/الـ endpoints) لكن
        // المطابقة الفعلية صارت على "رقم الفرصة" (OpportunityNumber) بدل رقم العقد، بطلب
        // المستخدمة - أبقينا الاسم لتقليل رقعة التعديل بكل نقاط الاستدعاء
        private static List<ContractRecord> TarhFilter(
            IEnumerable<ContractRecord> source, string? department, string? status, string? contractNumber, int? year, int? quarter) =>
            source.Where(r =>
                (string.IsNullOrWhiteSpace(department) || r.GeneralDepartment == department) &&
                (string.IsNullOrWhiteSpace(status) || r.Status == status) &&
                (string.IsNullOrWhiteSpace(contractNumber) || (r.OpportunityNumber ?? "").Contains(contractNumber, StringComparison.OrdinalIgnoreCase)) &&
                (!year.HasValue || r.ContractYear == year) &&
                (!quarter.HasValue || r.Quarter == quarter))
            .ToList();

        /// <summary>صفوف تصدير صفحة "الطرح" (Excel/PDF) - نفس تصفية GetTarhDataAsync بالضبط</summary>
        public async Task<List<TarhExportRow>> GetTarhExportRowsAsync(string? department, string? status, string? contractNumber, int? year, int? quarter)
        {
            var all = await _db.ContractRecords.AsNoTracking().ToListAsync();
            return TarhFilter(all, department, status, contractNumber, year, quarter)
                .OrderBy(r => r.OpportunityNumber)
                .Select(r => new TarhExportRow
                {
                    OpportunityNumber = r.OpportunityNumber,
                    ContractNumber = r.ContractNumber,
                    OpportunityName = r.OpportunityName,
                    Value = r.ContractValue ?? r.AnnualRent ?? 0,
                    GeneralDepartment = r.GeneralDepartment,
                    SubPortfolio = r.SubPortfolio,
                    ContractYear = r.ContractYear,
                    ContractDate = r.ContractDate
                })
                .ToList();
        }
    }
}
