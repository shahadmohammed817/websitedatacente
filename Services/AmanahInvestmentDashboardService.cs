using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    /// <summary>صف إدارة فرعية واحدة (عدد الفواتير + القيمة، مقسّمة محصل/غير محصل + قائم/جديد)</summary>
    public class SubDeptRow
    {
        public string Name { get; set; } = string.Empty;
        /// <summary>الإدارة العامة التابعة لها - لتصفية "الإدارات الفرعية" بالمتصفح عند الضغط على إدارة عامة بلا طلب شبكة إضافي</summary>
        public string? DeptGeneral { get; set; }
        public int InvoiceCount { get; set; }
        public decimal TotalValue { get; set; }
        public decimal PaidValue { get; set; }
        public decimal UnpaidValue { get; set; }
        /// <summary>قيمة العقود لكل حالة عقد (قائم/جديد...) داخل هذه الإدارة الفرعية</summary>
        public Dictionary<string, decimal> ContractStatusValues { get; set; } = new();
    }

    /// <summary>صف نوع استثمار واحد (عمود "نوع الاستثمار") - لدونات صفحة "الإدارات العامة"</summary>
    public class InvestmentTypeRow
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Value { get; set; }
    }

    /// <summary>صف حالة عقد واحدة (عمود "حالة العقد"، مثل "قائم"/"جديد") - لرسم "حالة العقد"</summary>
    public class ContractStatusRow
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
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

        // ===== أسماء الإدارات العامة (لا رسم خاص بها بعد الآن - تبقى فقط لتحديد الإدارة
        // الافتراضية لرسم "الإدارات الفرعية" عند عدم اختيار فلتر إدارة صراحةً) =====
        public List<string> DeptGeneralLabels { get; set; } = new();

        // ===== "الشكل الأول": لكل إدارة عامة - عدد وقيمة العقود حسب حالة العقد (قائم/جديد...):
        // ارتفاع الأعمدة = القيمة، والعدد يُكتب داخل كل عمود كتسمية - كلاهما بنفس ترتيب
        // DeptGeneralLabels =====
        public List<Dictionary<string, int>> DeptGeneralContractStatusCounts { get; set; } = new();
        public List<Dictionary<string, decimal>> DeptGeneralContractStatusValues { get; set; } = new();
        public List<decimal> DeptGeneralCollectionRatePercent { get; set; } = new();

        // ===== اتجاه التحصيل الشهري =====
        public List<string> MonthLabels { get; set; } = new();
        public List<decimal> MonthCollectedValues { get; set; } = new();
        public List<decimal> MonthTotalValues { get; set; } = new();

        // ===== Donut: توزيع البنود حسب "نوع الاستثمار" =====
        public List<InvestmentTypeRow> InvestmentTypeBreakdown { get; set; } = new();
        /// <summary>كل قيم "نوع الاستثمار" الموجودة (بصرف النظر عن الفلاتر الحالية) - لتعبئة قائمة الفلتر</summary>
        public List<string> InvestmentTypeOptions { get; set; } = new();

        // ===== رسم "حالة العقد" =====
        public List<ContractStatusRow> ContractStatusBreakdown { get; set; } = new();

        // ===== Stacked Horizontal Bar: كل الإدارات الفرعية (قائمة واحدة مسطّحة، مرتّبة تنازليًا) =====
        public List<SubDeptRow> SubDepartments { get; set; } = new();

        // ===== بطاقتا المعلومات الذكية: أعلى/أقل إدارة تحصيلاً - بناءً على قيمة المحصّل فعليًا
        // (وليس نسبة التحصيل)، بنفس مبدأ "أكثر/أقل بلدية تفاعلاً" بصفحة البلديات =====
        public string? TopCollectionRateDept { get; set; }
        public decimal TopCollectionRatePercent { get; set; }
        public decimal TopCollectionValue { get; set; }
        public string? BottomCollectionRateDept { get; set; }
        public decimal BottomCollectionRatePercent { get; set; }
        public decimal BottomCollectionValue { get; set; }
    }

    public interface IAmanahInvestmentDashboardService
    {
        /// <summary>
        /// deptGeneral/deptSub: عند تمريرهما (من صندوق الفلاتر بصفحة "بنود الأمانة") تُقيَّد كل
        /// المؤشرات والرسوم على هذه الإدارة العامة/الفرعية فقط (Cross Filtering) - مطابقة تامة
        /// مع عمودي DeptGeneral/DeptSub الخام المخزَّنين على كل سجل. investmentType: نفس فكرة
        /// deptGeneral لكن على عمود "نوع الاستثمار" (يغذّي دونات ذلك الرسم). sectionIds مُبقًى
        /// للتوافق فقط (غير مستخدَم من الواجهة الحالية). null للكل يعني كل بند الاستثمار بالأمانة
        /// دون تقييد.
        /// </summary>
        Task<AmanahInvestmentDashboardData> GetDataAsync(string? quarter = null, string? contractNumber = null, List<int>? sectionIds = null, string? deptGeneral = null, string? deptSub = null, string? investmentType = null);

        /// <summary>صفوف تصدير صفحة "الإدارات العامة" (Excel/PDF) - نفس تصفية GetDataAsync بالضبط</summary>
        Task<List<AmanahInvestmentExportRow>> GetExportRowsAsync(string? quarter = null, string? contractNumber = null, List<int>? sectionIds = null, string? deptGeneral = null, string? deptSub = null, string? investmentType = null);
    }

    /// <summary>صف تصدير واحد لصفحة "الإدارات العامة" - رقم العقد/اسم المستثمر/تاريخ الإنشاء/تاريخ الدفع/رقم الدفعة/القيمة/المحفظة الفرعية</summary>
    public class AmanahInvestmentExportRow
    {
        public string? ContractNumber { get; set; }
        public string? InvestorName { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? RecordDate { get; set; }
        public string? PaymentNumber { get; set; }
        public decimal? TotalValue { get; set; }
        public string? SubPortfolio { get; set; }
    }

    /// <summary>
    /// يبني لوحة "الإدارات العامة" من DashboardRecord حيث المنطقة = أمانة (Section.Scope = Amanah)
    /// وتصنيف البند = "استثمار" أو "تأجير مؤقت"، معتمدًا على عمودي "الإدارة العامة"/
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

        /// <summary>
        /// يبني استعلام DashboardRecords مُصفّى حسب نفس معايير GetDataAsync/GetExportRowsAsync
        /// (النطاق + الأقسام المُختارة + الإدارة العامة/الفرعية + نوع الاستثمار + الربع + رقم
        /// العقد) - يُستخدم من كلتا الدالتين حتى تبقى نتائج اللوحة والتصدير متطابقة تمامًا.
        /// </summary>
        private async Task<IQueryable<DashboardRecord>> BuildFilteredQueryAsync(
            string? quarter, string? contractNumber, List<int>? sectionIds,
            string? deptGeneral, string? deptSub, string? investmentType)
        {
            var allAmanahSections = await _db.Sections.AsNoTracking()
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

            IQueryable<DashboardRecord> query = _db.DashboardRecords.AsNoTracking()
                .Where(r => (r.Classification == "استثمار" || r.Classification == "تأجير مؤقت") && amanahSectionIds.Contains(r.SectionId));

            if (scopeSectionIds != null)
                query = query.Where(r => scopeSectionIds.Contains(r.SectionId));

            if (!string.IsNullOrWhiteSpace(deptGeneral))
                query = query.Where(r => r.DeptGeneral == deptGeneral);

            if (!string.IsNullOrWhiteSpace(deptSub))
                query = query.Where(r => r.DeptSub == deptSub);

            if (!string.IsNullOrWhiteSpace(investmentType))
                query = query.Where(r => r.InvestmentType == investmentType);

            if (!string.IsNullOrWhiteSpace(quarter))
                query = query.Where(r => r.Quarter == quarter);

            if (!string.IsNullOrWhiteSpace(contractNumber))
            {
                var term = contractNumber.Trim();
                query = query.Where(r => r.ContractNumber != null && r.ContractNumber.Contains(term));
            }

            return query;
        }

        public async Task<AmanahInvestmentDashboardData> GetDataAsync(string? quarter = null, string? contractNumber = null, List<int>? sectionIds = null, string? deptGeneral = null, string? deptSub = null, string? investmentType = null)
        {
            var result = new AmanahInvestmentDashboardData();

            var query = await BuildFilteredQueryAsync(quarter, contractNumber, sectionIds, deptGeneral, deptSub, investmentType);

            // قائمة "نوع الاستثمار" لتعبئة الفلتر: من نفس النطاق (الأمانة + تصنيف "استثمار" +
            // الأقسام المختارة) لكن بلا تصفية نوع الاستثمار/الإدارة/الربع نفسها، حتى تبقى القائمة
            // ثابتة الخيارات بصرف النظر عن الفلتر النشط حاليًا
            var unfilteredForOptions = await BuildFilteredQueryAsync(null, null, sectionIds, null, null, null);
            result.InvestmentTypeOptions = await unfilteredForOptions
                .Where(r => r.InvestmentType != null && r.InvestmentType != "")
                .Select(r => r.InvestmentType!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var records = await query
                .Select(r => new { r.SectionId, r.TotalValue, r.CollectedValue, r.UncollectedValue, r.RecordDate, r.DeptGeneral, r.DeptSub, r.InvestmentType, r.ContractStatus })
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
                    r.RecordDate,
                    InvestmentType = !string.IsNullOrWhiteSpace(r.InvestmentType) ? r.InvestmentType!.Trim() : null,
                    ContractStatus = !string.IsNullOrWhiteSpace(r.ContractStatus) ? r.ContractStatus!.Trim() : null
                };
            }).ToList();

            // ==================== أسماء الإدارات العامة (لتحديد الإدارة الافتراضية برسم
            // "الإدارات الفرعية" فقط - لا رسم عمود مكدَّس مخصّص لها بعد الآن، لكن التجميع نفسه
            // يبقى لازمًا لبطاقة "أعلى إدارة في نسبة التحصيل" أدناه) ====================
            var knownDeptOrder = new[] { "تجاري", "تنمو", "خدما" };
            var byDeptGeneral = classified
                .Where(x => x.DeptGeneral != null)
                .GroupBy(x => x.DeptGeneral!)
                .OrderBy(g => Array.FindIndex(knownDeptOrder, k => g.Key.Contains(k)) is var idx && idx >= 0 ? idx : 99)
                .ToList();
            result.DeptGeneralLabels = byDeptGeneral.Select(g => g.Key).ToList();

            // ==================== اتجاه التحصيل الشهري ====================
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

            // ==================== Donut: توزيع البنود حسب "نوع الاستثمار" ====================
            result.InvestmentTypeBreakdown = classified
                .Where(x => x.InvestmentType != null)
                .GroupBy(x => x.InvestmentType!)
                .Select(g => new InvestmentTypeRow { Name = g.Key, Count = g.Count(), Value = g.Sum(x => x.Value) })
                .OrderByDescending(x => x.Value)
                .ToList();

            // ==================== رسم "حالة العقد" ====================
            // ديناميكي حسب القيم الفعلية الموجودة بالعمود (قائم/جديد الآن، أي قيمة تُضاف لاحقًا
            // للملف تظهر تلقائيًا بلا تعديل كود) - تُستبعد السجلات بلا قيمة (لم تُعبَّأ بعد)
            result.ContractStatusBreakdown = classified
                .Where(x => x.ContractStatus != null)
                .GroupBy(x => x.ContractStatus!)
                .Select(g => new ContractStatusRow { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            // ==================== الإدارات الفرعية (قائمة مسطّحة، بالإدارة العامة التابعة لها) ====================
            result.SubDepartments = classified
                .Where(x => !string.IsNullOrWhiteSpace(x.SubDept))
                .GroupBy(x => new { x.DeptGeneral, SubDept = x.SubDept! })
                .Select(g => new SubDeptRow
                {
                    Name = g.Key.SubDept,
                    DeptGeneral = g.Key.DeptGeneral,
                    InvoiceCount = g.Count(),
                    TotalValue = g.Sum(x => x.Value),
                    PaidValue = g.Sum(x => x.PaidValue),
                    UnpaidValue = g.Sum(x => x.UnpaidValue),
                    ContractStatusValues = g
                        .Where(x => x.ContractStatus != null)
                        .GroupBy(x => x.ContractStatus!)
                        .ToDictionary(sg => sg.Key, sg => sg.Sum(x => x.Value))
                })
                .OrderByDescending(x => x.TotalValue)
                .ToList();

            // ==================== بطاقة المعلومات الذكية (بين الإدارات العامة المعروفة) ====================
            var deptStats = byDeptGeneral.Select(g =>
            {
                var total = g.Sum(x => x.Value);
                var paid = g.Sum(x => x.PaidValue);
                return new
                {
                    Name = g.Key,
                    Paid = paid,
                    CollectionRate = total > 0 ? Math.Round(paid / total * 100, 1) : 0
                };
            }).ToList();

            if (deptStats.Count > 0)
            {
                // الترتيب بناءً على قيمة المحصّل الفعلية (مو نسبة التحصيل) - طلب صريح من
                // المستخدمة حتى تعكس البطاقة حجم الأعمال الحقيقي، لا نسبة قد تتصدّرها إدارة
                // بحجم أعمال ضئيل لمجرد أن كل سجلاتها القليلة مسدَّدة بالكامل
                var topByValue = deptStats.OrderByDescending(x => x.Paid).First();
                result.TopCollectionRateDept = topByValue.Name;
                result.TopCollectionRatePercent = topByValue.CollectionRate;
                result.TopCollectionValue = topByValue.Paid;

                var bottomByValue = deptStats.OrderBy(x => x.Paid).First();
                result.BottomCollectionRateDept = bottomByValue.Name;
                result.BottomCollectionRatePercent = bottomByValue.CollectionRate;
                result.BottomCollectionValue = bottomByValue.Paid;
            }

            // ==================== "الشكل الأول": حالة العقد (قائم/جديد) عدد + قيمة، ونسبة التحصيل لكل إدارة عامة ====================
            result.DeptGeneralContractStatusCounts = byDeptGeneral
                .Select(g => g
                    .Where(x => x.ContractStatus != null)
                    .GroupBy(x => x.ContractStatus!)
                    .ToDictionary(sg => sg.Key, sg => sg.Count()))
                .ToList();
            result.DeptGeneralContractStatusValues = byDeptGeneral
                .Select(g => g
                    .Where(x => x.ContractStatus != null)
                    .GroupBy(x => x.ContractStatus!)
                    .ToDictionary(sg => sg.Key, sg => sg.Sum(x => x.Value)))
                .ToList();
            result.DeptGeneralCollectionRatePercent = deptStats.Select(x => x.CollectionRate).ToList();

            return result;
        }

        /// <summary>صفوف تصدير صفحة "الإدارات العامة" (Excel/PDF) - نفس تصفية GetDataAsync بالضبط</summary>
        public async Task<List<AmanahInvestmentExportRow>> GetExportRowsAsync(string? quarter = null, string? contractNumber = null, List<int>? sectionIds = null, string? deptGeneral = null, string? deptSub = null, string? investmentType = null)
        {
            var query = await BuildFilteredQueryAsync(quarter, contractNumber, sectionIds, deptGeneral, deptSub, investmentType);
            return await query
                .Select(r => new AmanahInvestmentExportRow
                {
                    ContractNumber = r.ContractNumber,
                    InvestorName = r.InvestorName,
                    CreatedDate = r.CreatedDate,
                    RecordDate = r.RecordDate,
                    PaymentNumber = r.PaymentNumber,
                    TotalValue = r.TotalValue,
                    SubPortfolio = r.DeptSub
                })
                .ToListAsync();
        }
    }   
}









