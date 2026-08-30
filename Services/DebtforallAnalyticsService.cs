using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ExcelDashboard.Services
{
    /// <summary>صف توزيع واحد حسب "تصنيف البند" (Classification - البنود الرئيسية: استثمار،
    /// رسوم، جزاءات... بعكس "البند" (Category) التفصيلي اللي فيه عشرات القيم) - يُستخدم لكل من
    /// رسمة الدونات (بالقيمة) ورسمة الأعمدة الأفقية (بالعدد) بنفس الوقت، حتى لا نكرر نفس التجميع
    /// مرتين. بطلب صريح من المستخدمة بعد ما لاحظت زحمة 42 قيمة مختلفة بعمود "البند"</summary>
    public class DebtforallClassificationRow
    {
        public string Classification { get; set; } = string.Empty;
        public decimal TotalValue { get; set; }
        public int Count { get; set; }
    }

    /// <summary>صف فترة واحدة من فترات "تاريخ الإنشاء" الثلاث - لرسمة الـLollipop وكروت القيمة</summary>
    public class DebtforallPeriodRow
    {
        public string PeriodLabel { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
    }

    /// <summary>صف بند فرعي واحد ("البند" - Category) ضمن تصنيف رئيسي واحد - يُجمَّع فعليًا حسب
    /// "رقم البند" (ItemNumber) لا نص "البند" نفسه، لأن نفس الرقم قد يتكرر بنصوص "بند" مختلفة
    /// قليلاً عبر الصفوف (نفس فكرة مطابقة "أعلى بنود الإيرادات" بالصفحة الرئيسية عبر معرّف ثابت
    /// بدل النص الخام) - Category هنا هو أكثر نص تكرارًا لنفس رقم البند، يُستخدم للعرض فقط</summary>
    public class DebtforallSubItemRow
    {
        public string Category { get; set; } = string.Empty;
        public decimal TotalValue { get; set; }
        public int Count { get; set; }
    }

    /// <summary>توزيع البنود الفرعية ("البند") ضمن تصنيف رئيسي واحد ("تصنيف البند") - يُستخدم
    /// برسمة الأعمدة الأفقية المجاورة للدونات: الضغط على تصنيف بالدونات يبدّل هذا التفصيل
    /// المعروض بالأعمدة (نفس فكرة "أعلى بنود الإيرادات ← البنود الفرعية" بالصفحة الرئيسية)</summary>
    public class DebtforallSubItemBreakdown
    {
        public string ClassificationLabel { get; set; } = string.Empty;
        public List<DebtforallSubItemRow> SubItems { get; set; } = new();
    }

    public class DebtforallPageData
    {
        public bool HasData { get; set; }

        /// <summary>إجمالي قيمة كل المديونيات (بطاقة KPI الأولى)</summary>
        public decimal TotalValue { get; set; }
        /// <summary>عدد كل المديونيات (بطاقة KPI الثانية)</summary>
        public int TotalCount { get; set; }

        /// <summary>توزيع حسب "تصنيف البند" (Classification) - مرتّب تنازليًا حسب القيمة</summary>
        public List<DebtforallClassificationRow> ByClassification { get; set; } = new();

        /// <summary>توزيع البنود الفرعية لكل تصنيف رئيسي على حدة - بنفس ترتيب ByClassification
        /// تنازليًا (العنصر الأول = نفس التصنيف الأعلى قيمةً بـByClassification[0])، يُستخدم
        /// كتفصيل تلقائي افتراضي قبل أي ضغط، وكمصدر التفصيل عند الضغط على أي تصنيف بالدونات</summary>
        public List<DebtforallSubItemBreakdown> SubItemsByClassification { get; set; } = new();

        /// <summary>توزيع حسب فترة "تاريخ الإنشاء" الثلاث - بترتيب زمني ثابت (قبل 2020، ثم
        /// 2020-2024، ثم 2025-2026) بصرف النظر عن وجود بيانات بكل فترة أو لا</summary>
        public List<DebtforallPeriodRow> ByPeriod { get; set; } = new();

        // خيارات فلاتر الصفحة - تُبنى دائمًا من كامل البيانات (بلا فلترة) حتى لا تتقلّص القوائم
        // نفسها عند اختيار فلتر
        public List<int> YearOptions { get; set; } = new();
        public List<int> QuarterOptions { get; set; } = new() { 1, 2, 3, 4 };
        public List<int> MonthOptions { get; set; } = new();
        /// <summary>السنوات الهجرية المحسوبة من عمود "تاريخ الإنشاء" (تقويم أم القرى) - عمود
        /// "تاريخ الهجري" بالملف نفسه لا يُعتمَد عليه لأنه يخزَّن داخليًا كتاريخ ميلادي عادي (نفس
        /// لحظة "تاريخ الإنشاء" بالضبط، فقط بتنسيق عرض هجري داخل إكسل)، فحسابه هنا بتقويم أم
        /// القرى مباشرة من "تاريخ الإنشاء" أدق وأوثق من أي محاولة لقراءته كنص خام</summary>
        public List<int> HijriYearOptions { get; set; } = new();
    }

    public interface IDebtforallAnalyticsService
    {
        Task<DebtforallPageData> GetDataAsync(int? year, int? quarter, int? month, int? hijriYear);
    }

    public class DebtforallAnalyticsService : IDebtforallAnalyticsService
    {
        private readonly AppDbContext _db;

        public DebtforallAnalyticsService(AppDbContext db)
        {
            _db = db;
        }

        // فترات "تاريخ الإنشاء" الثلاث بطلب صريح من المستخدمة: بلا تاريخ أو حتى 2019 = "فترة ما
        // قبل 2020"، من 2020 إلى 2024، ومن 2025 إلى 2026 - بترتيب زمني ثابت بغض النظر عن البيانات
        private const string PeriodBefore2020 = "فترة ما قبل 2020";
        private const string Period2020To2024 = "2020-2024";
        private const string Period2025To2026 = "2025-2026";

        private static string ResolvePeriod(DateTime? createdDate)
        {
            if (!createdDate.HasValue || createdDate.Value.Year <= 2019) return PeriodBefore2020;
            if (createdDate.Value.Year <= 2024) return Period2020To2024;
            return Period2025To2026;
        }

        // "رقم البند" نفسه قد يظهر بنصوص "البند" مختلفة قليلاً عبر الصفوف (مسافات/صياغة) - نستخدم
        // الرقم كمفتاح التجميع الفعلي، ونعرض أكثر نص "البند" تكرارًا لنفس الرقم كاسم للعرض فقط.
        // الصفوف بلا رقم بند (نادرة) تُجمَّع كل واحدة بمفردها عبر نص "البند" نفسه كمفتاح احتياطي
        private static string SubItemGroupKey(DebtforallRecord r) =>
            !string.IsNullOrWhiteSpace(r.ItemNumber) ? "num:" + r.ItemNumber!.Trim() : "cat:" + r.Category!.Trim();

        // تقويم أم القرى الرسمي بالسعودية - نفس التقويم اللي يعتمده تنسيق العرض الهجري بإكسل
        // (تأكّدنا: 2026-08-18 ميلادي ⇐ 1448-03-05 هجري، يطابق النص الظاهر بالملف الأصلي بالضبط)
        private static readonly UmAlQuraCalendar HijriCalendar = new();
        private static int? HijriYearOf(DateTime? gregorianDate) =>
            gregorianDate.HasValue ? HijriCalendar.GetYear(gregorianDate.Value) : (int?)null;

        public async Task<DebtforallPageData> GetDataAsync(int? year, int? quarter, int? month, int? hijriYear)
        {
            var all = await _db.DebtforallRecords.AsNoTracking().ToListAsync();
            var result = new DebtforallPageData { HasData = all.Count > 0 };
            if (!result.HasData) return result;

            // خيارات الفلاتر تُبنى من كامل البيانات دائمًا (بلا فلترة) حتى لا تتقلّص القوائم نفسها
            // عند اختيار فلتر آخر
            result.YearOptions = all.Where(r => r.CreatedDate.HasValue).Select(r => r.CreatedDate!.Value.Year)
                .Distinct().OrderByDescending(y => y).ToList();
            result.MonthOptions = Enumerable.Range(1, 12).ToList();
            result.HijriYearOptions = all.Where(r => r.CreatedDate.HasValue).Select(r => HijriYearOf(r.CreatedDate)!.Value)
                .Distinct().OrderByDescending(y => y).ToList();

            var filtered = all.AsEnumerable();
            if (year.HasValue) filtered = filtered.Where(r => r.CreatedDate.HasValue && r.CreatedDate.Value.Year == year.Value);
            if (quarter.HasValue) filtered = filtered.Where(r => r.CreatedDate.HasValue && ((r.CreatedDate.Value.Month - 1) / 3) + 1 == quarter.Value);
            if (month.HasValue) filtered = filtered.Where(r => r.CreatedDate.HasValue && r.CreatedDate.Value.Month == month.Value);
            if (hijriYear.HasValue) filtered = filtered.Where(r => HijriYearOf(r.CreatedDate) == hijriYear.Value);
            var filteredList = filtered.ToList();

            result.TotalValue = filteredList.Sum(r => r.Value ?? 0);
            result.TotalCount = filteredList.Count;

            result.ByClassification = filteredList
                .Where(r => !string.IsNullOrWhiteSpace(r.Classification))
                .GroupBy(r => r.Classification!.Trim())
                .Select(g => new DebtforallClassificationRow
                {
                    Classification = g.Key,
                    TotalValue = g.Sum(r => r.Value ?? 0),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.TotalValue)
                .ToList();

            result.SubItemsByClassification = filteredList
                .Where(r => !string.IsNullOrWhiteSpace(r.Classification) && !string.IsNullOrWhiteSpace(r.Category))
                .GroupBy(r => r.Classification!.Trim())
                .Select(g => new DebtforallSubItemBreakdown
                {
                    ClassificationLabel = g.Key,
                    SubItems = g.GroupBy(SubItemGroupKey)
                        .Select(sg =>
                        {
                            var displayLabel = sg.GroupBy(r => r.Category!.Trim())
                                .OrderByDescending(cg => cg.Count())
                                .Select(cg => cg.Key)
                                .First();
                            return new DebtforallSubItemRow
                            {
                                Category = displayLabel,
                                TotalValue = sg.Sum(r => r.Value ?? 0),
                                Count = sg.Count()
                            };
                        })
                        .OrderByDescending(x => x.TotalValue)
                        .ToList()
                })
                .OrderByDescending(x => x.SubItems.Sum(s => s.TotalValue))
                .ToList();

            var periodGroups = filteredList
                .GroupBy(r => ResolvePeriod(r.CreatedDate))
                .ToDictionary(g => g.Key, g => new { Count = g.Count(), Value = g.Sum(r => r.Value ?? 0) });

            result.ByPeriod = new List<string> { PeriodBefore2020, Period2020To2024, Period2025To2026 }
                .Select(label => new DebtforallPeriodRow
                {
                    PeriodLabel = label,
                    Count = periodGroups.TryGetValue(label, out var g) ? g.Count : 0,
                    TotalValue = periodGroups.TryGetValue(label, out var g2) ? g2.Value : 0
                })
                .ToList();

            return result;
        }
    }
}
