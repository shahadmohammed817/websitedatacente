using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    /// <summary>صف إدارة فرعية واحدة (عدد الفواتير + القيمة، مقسّمة محصل/غير محصل)</summary>
    public class SubDeptRow
    {
        public string Name { get; set; } = string.Empty;
        public int InvoiceCount { get; set; }
        public decimal TotalValue { get; set; }
        public decimal PaidValue { get; set; }
        public decimal UnpaidValue { get; set; }
    }

    /// <summary>بيانات صفحة "بيانات الأمانة" (بند الاستثمار)</summary>
    public class AmanahInvestmentDashboardData
    {
        public bool HasData { get; set; }

        // ===== 1) مؤشر التحصيل الدائري + 2) بطاقات KPI =====
        public int TotalInvoiceCount { get; set; }
        public decimal TotalValue { get; set; }
        public int PaidInvoiceCount { get; set; }
        public decimal PaidValue { get; set; }
        public int UnpaidInvoiceCount { get; set; }
        public decimal UnpaidValue { get; set; }
        public decimal CollectionRatePercent { get; set; }

        // ===== 3) Stacked Column حسب الإدارة العامة (محصل/غير محصل) =====
        public List<string> DeptGeneralLabels { get; set; } = new();
        public List<decimal> DeptGeneralPaidValues { get; set; } = new();
        public List<decimal> DeptGeneralUnpaidValues { get; set; } = new();
        public List<int> DeptGeneralInvoiceCounts { get; set; } = new();

        // ===== 4) اتجاه التحصيل الشهري =====
        public List<string> MonthLabels { get; set; } = new();
        public List<decimal> MonthCollectedValues { get; set; } = new();
        public List<decimal> MonthTotalValues { get; set; } = new();

        // ===== 5) Donut: توزيع البنود حسب التصنيف (استثمار / تأجير مؤقت) =====
        public int InvestmentInvoiceCount { get; set; }
        public decimal InvestmentValue { get; set; }
        public int TempLeaseInvoiceCount { get; set; }
        public decimal TempLeaseValue { get; set; }

        // ===== 6) Stacked Horizontal Bar: كل الإدارات الفرعية (قائمة واحدة مسطّحة، مرتّبة تنازليًا) =====
        public List<SubDeptRow> SubDepartments { get; set; } = new();

        // ===== 7) بطاقتا المعلومات الذكية =====
        public string? TopCollectionRateDept { get; set; }
        public decimal TopCollectionRatePercent { get; set; }
        public string? TopInvoiceCountDept { get; set; }
        public int TopInvoiceCountAmount { get; set; }
    }

    public interface IAmanahInvestmentDashboardService
    {
        /// <summary>
        /// deptGeneral/deptSub: عند تمريرهما (من صندوق الفلاتر بصفحة "بنود الأمانة") تُقيَّد كل
        /// المؤشرات والرسوم على هذه الإدارة العامة/الفرعية فقط (Cross Filtering) - مطابقة تامة
        /// مع عمودي DeptGeneral/DeptSub الخام المخزَّنين على كل سجل. sectionIds مُبقًى للتوافق
        /// فقط (غير مستخدَم من الواجهة الحالية). null للكل يعني كل بند الاستثمار بالأمانة دون تقييد.
        /// </summary>
        Task<AmanahInvestmentDashboardData> GetDataAsync(string? quarter = null, string? contractNumber = null, List<int>? sectionIds = null, string? deptGeneral = null, string? deptSub = null);
    }

    /// <summary>
    /// يبني لوحة "بيانات الأمانة" حصريًا من DashboardRecord حيث المنطقة = أمانة
    /// (Section.Scope = Amanah) وتصنيف البند = "استثمار"، معتمدًا على عمودي "الإدارة العامة"/
    /// "الإدارة الفرعية" الخام المُخزَّنين مباشرة على كل سجل (DeptGeneral/DeptSub) لتحديد
    /// إدارته - وليس على شجرة الأقسام (Section)، لأن البنود الفرعية الفعلية المستوردة من الملف
    /// (مثل "تأجير مباني تجارية") لا تحمل بنودًا فرعية خاصة بها بشجرة الأقسام. وعلى
    /// UncollectedValue لتحديد حالة السداد (UncollectedValue > 0 يعني غير محصّل).
    /// </summary>
    public class AmanahInvestmentDashboardService : IAmanahInvestmentDashboardService
    {
        private readonly AppDbContext _db;

        private static readonly string[] ArabicMonthNames =
        {
            "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
            "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"
        };

        public AmanahInvestmentDashboardService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<AmanahInvestmentDashboardData> GetDataAsync(string? quarter = null, string? contractNumber = null, List<int>? sectionIds = null, string? deptGeneral = null, string? deptSub = null)
        {
            var result = new AmanahInvestmentDashboardData();

            var allAmanahSections = await _db.Sections
                .Where(s => s.Scope == ScopeType.Amanah)
                .Select(s => new { s.Id, s.NameAr, s.ParentSectionId })
                .ToListAsync();

            var amanahSectionIds = allAmanahSections.Select(s => s.Id).ToHashSet();

            // ==================== نطاق الأقسام الفعلي (Cross Filtering) ====================
            // إن حُدِّدت أقسام معيّنة (اختيار من القائمة الجانبية)، يُقيَّد الاستعلام عليها
            // وعلى كل أبنائها (حتى تشمل إدارة عامة كل إداراتها الفرعية تلقائيًا)
            HashSet<int>? scopeSectionIds = null;
            if (sectionIds != null && sectionIds.Count > 0)
            {
                var childrenByParent = allAmanahSections
                    .Where(s => s.ParentSectionId.HasValue)
                    .ToLookup(s => s.ParentSectionId!.Value);

                scopeSectionIds = new HashSet<int>();
                var queue = new Queue<int>(sectionIds.Where(amanahSectionIds.Contains));
                foreach (var id in sectionIds) scopeSectionIds.Add(id);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    foreach (var child in childrenByParent[current])
                    {
                        if (scopeSectionIds.Add(child.Id)) queue.Enqueue(child.Id);
                    }
                }
            }

            IQueryable<DashboardRecord> query = _db.DashboardRecords
                .Where(r => r.Classification == "استثمار" && amanahSectionIds.Contains(r.SectionId));

            if (scopeSectionIds != null)
                query = query.Where(r => scopeSectionIds.Contains(r.SectionId));

            if (!string.IsNullOrWhiteSpace(deptGeneral))
                query = query.Where(r => r.DeptGeneral == deptGeneral);

            if (!string.IsNullOrWhiteSpace(deptSub))
                query = query.Where(r => r.DeptSub == deptSub);

            if (!string.IsNullOrWhiteSpace(quarter))
                query = query.Where(r => r.Quarter == quarter);

            if (!string.IsNullOrWhiteSpace(contractNumber))
            {
                var term = contractNumber.Trim();
                query = query.Where(r => r.ContractNumber != null && r.ContractNumber.Contains(term));
            }

            var records = await query
                .Select(r => new { r.SectionId, r.TotalValue, r.CollectedValue, r.UncollectedValue, r.RecordDate, r.DeptGeneral, r.DeptSub })
                .ToListAsync();

            result.HasData = records.Count > 0;
            if (!result.HasData) return result;

            // ==================== تحديد الإدارة العامة/الفرعية لكل سجل ====================
            // مباشرة من عمودي "الإدارة العامة"/"الإدارة الفرعية" الخام المُخزَّنين على السجل نفسه
            // وقت الاستيراد (DataImportService) - قيم صالحة فقط تحوي "الإدارة" (تستثني قيم غير
            // حقيقية بالملف مثل "0" أو ملاحظات نصية عامة لا تمثّل اسم إدارة فعلي)
            static string? CleanDeptGeneral(string? raw) =>
                !string.IsNullOrWhiteSpace(raw) && raw.Contains("الإدارة") ? raw.Trim() : null;

            bool IsUnpaid(decimal? uncollected) => uncollected.HasValue && uncollected.Value > 0;

            // ==================== 1+2) مؤشر التحصيل + بطاقات KPI ====================
            result.TotalInvoiceCount = records.Count;
            result.TotalValue = records.Sum(r => r.TotalValue ?? 0);

            var paidRecords = records.Where(r => !IsUnpaid(r.UncollectedValue)).ToList();
            var unpaidRecords = records.Where(r => IsUnpaid(r.UncollectedValue)).ToList();

            result.PaidInvoiceCount = paidRecords.Count;
            result.PaidValue = paidRecords.Sum(r => r.CollectedValue ?? r.TotalValue ?? 0);
            result.UnpaidInvoiceCount = unpaidRecords.Count;
            result.UnpaidValue = unpaidRecords.Sum(r => r.UncollectedValue ?? 0);

            result.CollectionRatePercent = result.TotalValue > 0
                ? Math.Round(result.PaidValue / result.TotalValue * 100, 1)
                : 0;

            // ==================== تصنيف كل سجل ====================
            var classified = records.Select(r =>
            {
                var deptGeneral = CleanDeptGeneral(r.DeptGeneral);
                var subDept = deptGeneral != null && !string.IsNullOrWhiteSpace(r.DeptSub) ? r.DeptSub!.Trim() : null;
                var isUnpaid = IsUnpaid(r.UncollectedValue);
                return new
                {
                    DeptGeneral = deptGeneral,
                    SubDept = subDept,
                    IsTempLease = deptGeneral == null,
                    Value = r.TotalValue ?? 0,
                    PaidValue = isUnpaid ? 0 : (r.CollectedValue ?? r.TotalValue ?? 0),
                    UnpaidValue = isUnpaid ? (r.UncollectedValue ?? 0) : 0,
                    r.RecordDate
                };
            }).ToList();

            // ==================== 3) Stacked Column حسب الإدارة العامة ====================
            var knownDeptOrder = new[] { "تجاري", "تنمو", "خدما" };
            var byDeptGeneral = classified
                .Where(x => x.DeptGeneral != null)
                .GroupBy(x => x.DeptGeneral!)
                .OrderBy(g => Array.FindIndex(knownDeptOrder, k => g.Key.Contains(k)) is var idx && idx >= 0 ? idx : 99)
                .ToList();

            foreach (var g in byDeptGeneral)
            {
                result.DeptGeneralLabels.Add(g.Key);
                result.DeptGeneralPaidValues.Add(g.Sum(x => x.PaidValue));
                result.DeptGeneralUnpaidValues.Add(g.Sum(x => x.UnpaidValue));
                result.DeptGeneralInvoiceCounts.Add(g.Count());
            }

            // ==================== 4) اتجاه التحصيل الشهري ====================
            var byMonth = classified
                .Where(x => x.RecordDate.HasValue)
                .GroupBy(x => new { x.RecordDate!.Value.Year, x.RecordDate!.Value.Month })
                .Select(g => new
                {
                    SortKey = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Label = ArabicMonthNames[g.Key.Month - 1],
                    Collected = g.Sum(x => x.PaidValue),
                    Total = g.Sum(x => x.Value)
                })
                .OrderBy(x => x.SortKey)
                .ToList();
            result.MonthLabels = byMonth.Select(x => x.Label).ToList();
            result.MonthCollectedValues = byMonth.Select(x => x.Collected).ToList();
            result.MonthTotalValues = byMonth.Select(x => x.Total).ToList();

            // ==================== 5) Donut: استثمار (له إدارة) مقابل تأجير مؤقت (بلا إدارة) ====================
            var investmentRows = classified.Where(x => !x.IsTempLease).ToList();
            var tempLeaseRows = classified.Where(x => x.IsTempLease).ToList();
            result.InvestmentInvoiceCount = investmentRows.Count;
            result.InvestmentValue = investmentRows.Sum(x => x.Value);
            result.TempLeaseInvoiceCount = tempLeaseRows.Count;
            result.TempLeaseValue = tempLeaseRows.Sum(x => x.Value);

            // ==================== 6) الإدارات الفرعية (قائمة مسطّحة واحدة) ====================
            result.SubDepartments = classified
                .Where(x => !string.IsNullOrWhiteSpace(x.SubDept))
                .GroupBy(x => x.SubDept!)
                .Select(g => new SubDeptRow
                {
                    Name = g.Key,
                    InvoiceCount = g.Count(),
                    TotalValue = g.Sum(x => x.Value),
                    PaidValue = g.Sum(x => x.PaidValue),
                    UnpaidValue = g.Sum(x => x.UnpaidValue)
                })
                .OrderByDescending(x => x.TotalValue)
                .ToList();

            // ==================== 7) بطاقتا المعلومات الذكية (بين الإدارات العامة المعروفة) ====================
            var deptStats = byDeptGeneral.Select(g =>
            {
                var total = g.Sum(x => x.Value);
                var paid = g.Sum(x => x.PaidValue);
                return new
                {
                    Name = g.Key,
                    InvoiceCount = g.Count(),
                    CollectionRate = total > 0 ? Math.Round(paid / total * 100, 1) : 0
                };
            }).ToList();

            if (deptStats.Count > 0)
            {
                var topRate = deptStats.OrderByDescending(x => x.CollectionRate).First();
                result.TopCollectionRateDept = topRate.Name;
                result.TopCollectionRatePercent = topRate.CollectionRate;

                var topCount = deptStats.OrderByDescending(x => x.InvoiceCount).First();
                result.TopInvoiceCountDept = topCount.Name;
                result.TopInvoiceCountAmount = topCount.InvoiceCount;
            }

            return result;
        }
    }
}
