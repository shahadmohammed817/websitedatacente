using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    public class DebtsPageData
    {
        public bool HasData { get; set; }

        public int TotalCount { get; set; }
        public decimal TotalValue { get; set; }

        // "متبقية" = عمود "حالة المطالبة في التنفيذ القضائي" ∈ RemainingLegalClaimStatuses
        // (3 حالات محدَّدة صراحةً من المستخدمة)، "معالجة" = كل الباقي بما فيها الفواتير غير
        // المرتبطة أصلاً بمطالبة (عمود فارغ) - راجع RemainingLegalClaimStatuses للتفاصيل
        public int ProcessedCount { get; set; }
        public decimal ProcessedValue { get; set; }
        public decimal ProcessedPercent { get; set; }

        public int RemainingCount { get; set; }
        public decimal RemainingValue { get; set; }
        public decimal RemainingPercent { get; set; }

        public int LegalExecutionLinkedCount { get; set; }
        public decimal LegalExecutionLinkedValue { get; set; }
        public int LegalExecutionNotLinkedCount { get; set; }
        public decimal LegalExecutionNotLinkedValue { get; set; }

        // "ملغي" - حالة ثالثة مستقلة عن معالجة/متبقية (لا تدخل في أي منهما)
        public int CancelledCount { get; set; }
        public decimal CancelledValue { get; set; }
        public decimal CancelledPercent { get; set; }

        public int OverdueCount { get; set; }
        public decimal OverdueValue { get; set; }
        public decimal OverduePercent { get; set; }

        // الفواتير ضمن 60 يوم (غير متجاوزة) - نفس عمود DaysElapsed، للمقارنة البصرية بجانب المتجاوزة
        public int WithinCount { get; set; }
        public decimal WithinValue { get; set; }

        // حسب الإدارة العامة - أعمدة مكدَّسة: قيمة/عدد محصل/غير محصل/ملغي، قابلة للضغط لاختيار
        // إدارة تُفلتِر رسوم الإدارات الفرعية (التالية) بالمتصفح بلا أي طلب إضافي
        public List<string> DeptLabels { get; set; } = new();
        public List<int> DeptCollectedCounts { get; set; } = new();
        public List<int> DeptNotCollectedCounts { get; set; } = new();
        public List<int> DeptCancelledCounts { get; set; } = new();
        public List<decimal> DeptCollectedValues { get; set; } = new();
        public List<decimal> DeptNotCollectedValues { get; set; } = new();
        public List<decimal> DeptCancelledValues { get; set; } = new();

        // حسب الإدارة العامة - مرتبطة/غير مرتبطة بالتنفيذ القضائي (قيمة + عدد، للرسم كنسبة 100%)
        public List<decimal> DeptLegalLinkedValues { get; set; } = new();
        public List<decimal> DeptLegalNotLinkedValues { get; set; } = new();
        public List<int> DeptLegalLinkedCounts { get; set; } = new();
        public List<int> DeptLegalNotLinkedCounts { get; set; } = new();

        // حسب المحفظة الفرعية داخل كل إدارة عامة - محصل/غير محصل/ملغي (عدد) + مرتبطة/غير
        // مرتبطة بالتنفيذ القضائي (قيمة) لكل محفظة - قائمة مسطّحة تحمل اسم الإدارة العامة
        // الأصلية، تُفلتَر بالمتصفح حسب الإدارة المختارة برسم الإدارات العامة
        public List<PortfolioStatusRow> PortfolioBreakdown { get; set; } = new();

        // حسب الفترة (سنويًا) - عمود (قيمة) + خط (عدد فواتير) معًا
        public List<string> PeriodLabels { get; set; } = new();
        public List<decimal> PeriodValues { get; set; } = new();
        public List<int> PeriodCounts { get; set; } = new();

        public List<int> YearOptions { get; set; } = new();
        public List<string> EntityOptions { get; set; } = new();
        public List<string> DepartmentOptions { get; set; } = new();
        public List<string> PortfolioOptions { get; set; } = new();
        public List<string> ClassificationOptions { get; set; } = new();
        public List<string> CategoryOptions { get; set; } = new();
        public List<string> PaymentStatusOptions { get; set; } = new();
    }

    public class PortfolioStatusRow
    {
        public string DepartmentLabel { get; set; } = string.Empty;
        public string PortfolioLabel { get; set; } = string.Empty;
        public int CollectedCount { get; set; }
        public decimal CollectedValue { get; set; }
        public int NotCollectedCount { get; set; }
        public decimal NotCollectedValue { get; set; }
        public int CancelledCount { get; set; }
        public decimal CancelledValue { get; set; }
        public decimal LegalLinkedValue { get; set; }
        public decimal LegalNotLinkedValue { get; set; }
        public int LegalLinkedCount { get; set; }
        public int LegalNotLinkedCount { get; set; }
    }

    public class OverdueInvoiceRow
    {
        public int Id { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? ContractNumber { get; set; }
        public decimal? Value { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? DaysElapsed { get; set; }
        public string? PaymentStatus { get; set; }
        public string? GeneralDepartment { get; set; }
        public string? Portfolio { get; set; }
        public string? Category { get; set; }
        public string? LegalExecutionClaimStatus { get; set; }
    }

    public interface IDebtsAnalyticsService
    {
        Task<DebtsPageData> GetDataAsync(
            int? year, string? entity, string? department, string? portfolio,
            string? classification, string? category, string? paymentStatus, string? isLegalExecution,
            string? contractNumber = null, string? invoiceNumber = null);

        /// <summary>statusFilter: "over" (متجاوزة 60 يوم، الافتراضي) أو "within" (ضمن 60 يوم)</summary>
        Task<List<OverdueInvoiceRow>> GetOverdueInvoicesAsync(
            int? year, string? entity, string? department, string? portfolio,
            string? classification, string? category, string? paymentStatus, string? isLegalExecution,
            string? contractNumber = null, string? invoiceNumber = null, string statusFilter = "over");

        Task<DebtInvoiceRecord?> GetInvoiceDetailAsync(int id);

        Task<List<DebtInvoiceRecord>> FindByContractNumberAsync(string contractNumber);
    }

    public class DebtsAnalyticsService : IDebtsAnalyticsService
    {
        private readonly AppDbContext _db;

        // "المعالجة/المتبقية" تعتمد على عمود "حالة المطالبة في التنفيذ القضائي" (وليس "حالة
        // السداد") - هذه الحالات الثلاث فقط تُعتبر "متبقية" (تحقّقت من القيم الحقيقية الموجودة
        // بالعمود: 146 من 18,329 فاتورة). الباقي (يشمل بقية الحالات المكتوبة + كل الفواغ/الفراغ -
        // فراغ يعني الفاتورة غير مرتبطة أصلاً بمطالبة، تأكيد صريح من المستخدمة) يُعتبر "معالجة"
        private static readonly HashSet<string> RemainingLegalClaimStatuses = new()
        {
            "بانتظار اعتماد الغاء المطالبة",
            "مطالبة تم رفضها عن طريق نظام القضايا",
            "مطالبة غير مكتملة البيانات"
        };

        public DebtsAnalyticsService(AppDbContext db)
        {
            _db = db;
        }

        private IQueryable<DebtInvoiceRecord> ApplyFilters(
            IQueryable<DebtInvoiceRecord> query,
            int? year, string? entity, string? department, string? portfolio,
            string? classification, string? category, string? paymentStatus, string? isLegalExecution,
            string? contractNumber = null, string? invoiceNumber = null)
        {
            if (year.HasValue) query = query.Where(r => r.CreatedDate.HasValue && r.CreatedDate.Value.Year == year.Value);
            if (!string.IsNullOrWhiteSpace(entity)) query = query.Where(r => r.Entity == entity);
            if (!string.IsNullOrWhiteSpace(department)) query = query.Where(r => r.GeneralDepartment == department);
            if (!string.IsNullOrWhiteSpace(portfolio)) query = query.Where(r => r.Portfolio == portfolio);
            if (!string.IsNullOrWhiteSpace(classification)) query = query.Where(r => r.Classification == classification);
            if (!string.IsNullOrWhiteSpace(category)) query = query.Where(r => r.Category == category);
            if (!string.IsNullOrWhiteSpace(paymentStatus)) query = query.Where(r => r.PaymentStatus == paymentStatus);
            if (!string.IsNullOrWhiteSpace(isLegalExecution)) query = query.Where(r => r.IsLegalExecution == isLegalExecution);
            if (!string.IsNullOrWhiteSpace(contractNumber)) query = query.Where(r => r.ContractNumber != null && r.ContractNumber.Contains(contractNumber));
            if (!string.IsNullOrWhiteSpace(invoiceNumber)) query = query.Where(r => r.InvoiceNumber != null && r.InvoiceNumber.Contains(invoiceNumber));
            return query;
        }

        public async Task<DebtsPageData> GetDataAsync(
            int? year, string? entity, string? department, string? portfolio,
            string? classification, string? category, string? paymentStatus, string? isLegalExecution,
            string? contractNumber = null, string? invoiceNumber = null)
        {
            var result = new DebtsPageData();

            var baseQuery = _db.DebtInvoiceRecords.AsNoTracking();
            var allRecords = await baseQuery.ToListAsync();
            result.HasData = allRecords.Count > 0;
            if (!result.HasData) return result;

            // خيارات الفلاتر تُبنى من كل البيانات (بلا تصفية) حتى تبقى القوائم كاملة دومًا
            result.YearOptions = allRecords.Where(r => r.CreatedDate.HasValue).Select(r => r.CreatedDate!.Value.Year).Distinct().OrderByDescending(y => y).ToList();
            result.EntityOptions = allRecords.Where(r => !string.IsNullOrWhiteSpace(r.Entity)).Select(r => r.Entity!).Distinct().OrderBy(x => x).ToList();
            result.DepartmentOptions = allRecords.Where(r => !string.IsNullOrWhiteSpace(r.GeneralDepartment)).Select(r => r.GeneralDepartment!).Distinct().OrderBy(x => x).ToList();
            result.PortfolioOptions = allRecords.Where(r => !string.IsNullOrWhiteSpace(r.Portfolio)).Select(r => r.Portfolio!).Distinct().OrderBy(x => x).ToList();
            result.ClassificationOptions = allRecords.Where(r => !string.IsNullOrWhiteSpace(r.Classification)).Select(r => r.Classification!).Distinct().OrderBy(x => x).ToList();
            result.CategoryOptions = allRecords.Where(r => !string.IsNullOrWhiteSpace(r.Category)).Select(r => r.Category!).Distinct().OrderBy(x => x).ToList();
            result.PaymentStatusOptions = allRecords.Where(r => !string.IsNullOrWhiteSpace(r.PaymentStatus)).Select(r => r.PaymentStatus!).Distinct().OrderBy(x => x).ToList();

            var records = ApplyFilters(baseQuery, year, entity, department, portfolio, classification, category, paymentStatus, isLegalExecution, contractNumber, invoiceNumber);
            var rows = await records.ToListAsync();

            result.TotalCount = rows.Count(r => !string.IsNullOrWhiteSpace(r.InvoiceNumber));
            result.TotalValue = rows.Sum(r => r.Value ?? 0);

            bool IsRemaining(DebtInvoiceRecord r) => r.LegalExecutionClaimStatus != null && RemainingLegalClaimStatuses.Contains(r.LegalExecutionClaimStatus);
            bool IsProcessed(DebtInvoiceRecord r) => !IsRemaining(r);

            var processed = rows.Where(IsProcessed).ToList();
            result.ProcessedCount = processed.Count;
            result.ProcessedValue = processed.Sum(r => r.Value ?? 0);
            result.ProcessedPercent = result.TotalCount > 0 ? Math.Round((decimal)result.ProcessedCount / result.TotalCount * 100, 1) : 0;

            var remaining = rows.Where(IsRemaining).ToList();
            result.RemainingCount = remaining.Count;
            result.RemainingValue = remaining.Sum(r => r.Value ?? 0);
            result.RemainingPercent = result.TotalCount > 0 ? Math.Round((decimal)result.RemainingCount / result.TotalCount * 100, 1) : 0;

            var cancelled = rows.Where(r => r.PaymentStatus == "ملغي").ToList();
            result.CancelledCount = cancelled.Count;
            result.CancelledValue = cancelled.Sum(r => r.Value ?? 0);
            result.CancelledPercent = result.TotalCount > 0 ? Math.Round((decimal)result.CancelledCount / result.TotalCount * 100, 1) : 0;

            var legalLinked = rows.Where(r => r.IsLegalExecution == "نعم").ToList();
            result.LegalExecutionLinkedCount = legalLinked.Count;
            result.LegalExecutionLinkedValue = legalLinked.Sum(r => r.Value ?? 0);

            var legalNotLinked = rows.Where(r => r.IsLegalExecution != "نعم").ToList();
            result.LegalExecutionNotLinkedCount = legalNotLinked.Count;
            result.LegalExecutionNotLinkedValue = legalNotLinked.Sum(r => r.Value ?? 0);

            bool IsCancelled(DebtInvoiceRecord r) => r.PaymentStatus == "ملغي";

            // القيمة الموجبة بعمود DaysElapsed تعني "لم تستحق بعد"، والسالبة تعني "متجاوزة بهذا
            // العدد من الأيام" (تأكيد صريح من المستخدمة بعد فحص البيانات الفعلية: 417 من 418
            // فاتورة بها قيمة كانت سالبة) - لذا "متجاوزة 60 يوم" = القيمة أصغر من -60
            var overdue = rows.Where(r => r.DaysElapsed.HasValue && r.DaysElapsed.Value < -60).ToList();
            result.OverdueCount = overdue.Count;
            result.OverdueValue = overdue.Sum(r => r.Value ?? 0);
            result.OverduePercent = rows.Count > 0 ? Math.Round((decimal)overdue.Count / rows.Count * 100, 1) : 0;

            var within = rows.Where(r => r.DaysElapsed.HasValue && r.DaysElapsed.Value >= -60).ToList();
            result.WithinCount = within.Count;
            result.WithinValue = within.Sum(r => r.Value ?? 0);

            // حسب الإدارة العامة: عمود مكدَّس بعدد الفواتير (محصل/غير محصل/ملغي)، بلا نسبة
            var byDept = rows.Where(r => !string.IsNullOrWhiteSpace(r.GeneralDepartment))
                .GroupBy(r => r.GeneralDepartment!)
                .Select(g => new
                {
                    Label = g.Key,
                    Total = g.Count(),
                    Collected = g.Count(IsProcessed),
                    NotCollected = g.Count(IsRemaining),
                    Cancelled = g.Count(IsCancelled),
                    CollectedValue = g.Where(IsProcessed).Sum(x => x.Value ?? 0),
                    NotCollectedValue = g.Where(IsRemaining).Sum(x => x.Value ?? 0),
                    CancelledValue = g.Where(IsCancelled).Sum(x => x.Value ?? 0),
                    LegalLinked = g.Where(x => x.IsLegalExecution == "نعم").Sum(x => x.Value ?? 0),
                    LegalNotLinked = g.Where(x => x.IsLegalExecution != "نعم").Sum(x => x.Value ?? 0),
                    LegalLinkedCount = g.Count(x => x.IsLegalExecution == "نعم"),
                    LegalNotLinkedCount = g.Count(x => x.IsLegalExecution != "نعم")
                })
                .OrderByDescending(x => x.Total)
                .ToList();
            result.DeptLabels = byDept.Select(x => x.Label).ToList();
            result.DeptCollectedCounts = byDept.Select(x => x.Collected).ToList();
            result.DeptNotCollectedCounts = byDept.Select(x => x.NotCollected).ToList();
            result.DeptCancelledCounts = byDept.Select(x => x.Cancelled).ToList();
            result.DeptCollectedValues = byDept.Select(x => x.CollectedValue).ToList();
            result.DeptNotCollectedValues = byDept.Select(x => x.NotCollectedValue).ToList();
            result.DeptCancelledValues = byDept.Select(x => x.CancelledValue).ToList();
            result.DeptLegalLinkedValues = byDept.Select(x => x.LegalLinked).ToList();
            result.DeptLegalNotLinkedValues = byDept.Select(x => x.LegalNotLinked).ToList();
            result.DeptLegalLinkedCounts = byDept.Select(x => x.LegalLinkedCount).ToList();
            result.DeptLegalNotLinkedCounts = byDept.Select(x => x.LegalNotLinkedCount).ToList();

            // حسب المحفظة الفرعية داخل كل إدارة عامة: محصل/غير محصل/ملغي (عدد) + مرتبطة/غير
            // مرتبطة بالتنفيذ القضائي (قيمة) - يحمل اسم الإدارة العامة الأصلية لتصفيتها بالمتصفح
            result.PortfolioBreakdown = rows.Where(r => !string.IsNullOrWhiteSpace(r.GeneralDepartment) && !string.IsNullOrWhiteSpace(r.Portfolio))
                .GroupBy(r => new { Dept = r.GeneralDepartment!, Portfolio = r.Portfolio! })
                .Select(g => new PortfolioStatusRow
                {
                    DepartmentLabel = g.Key.Dept,
                    PortfolioLabel = g.Key.Portfolio,
                    CollectedCount = g.Count(IsProcessed),
                    CollectedValue = g.Where(IsProcessed).Sum(x => x.Value ?? 0),
                    NotCollectedCount = g.Count(IsRemaining),
                    NotCollectedValue = g.Where(IsRemaining).Sum(x => x.Value ?? 0),
                    CancelledCount = g.Count(IsCancelled),
                    CancelledValue = g.Where(IsCancelled).Sum(x => x.Value ?? 0),
                    LegalLinkedValue = g.Where(x => x.IsLegalExecution == "نعم").Sum(x => x.Value ?? 0),
                    LegalNotLinkedValue = g.Where(x => x.IsLegalExecution != "نعم").Sum(x => x.Value ?? 0),
                    LegalLinkedCount = g.Count(x => x.IsLegalExecution == "نعم"),
                    LegalNotLinkedCount = g.Count(x => x.IsLegalExecution != "نعم")
                })
                .OrderByDescending(x => x.CollectedValue + x.NotCollectedValue + x.CancelledValue)
                .ToList();

            // فترتان فقط: "قبل [أحدث سنة بالبيانات]" مقابل أحدث سنة نفسها - أحدث سنة تُحسَب
            // ديناميكيًا من البيانات (وليست 2026 مثبّتة بالكود) حتى تبقى صحيحة مستقبلاً
            var recordsWithYear = rows.Where(r => r.CreatedDate.HasValue).ToList();
            if (recordsWithYear.Count > 0)
            {
                var currentYear = recordsWithYear.Max(r => r.CreatedDate!.Value.Year);
                var beforeCurrent = recordsWithYear.Where(r => r.CreatedDate!.Value.Year < currentYear).ToList();
                var currentYearRows = recordsWithYear.Where(r => r.CreatedDate!.Value.Year == currentYear).ToList();

                result.PeriodLabels = new List<string> { $"قبل {currentYear}", currentYear.ToString() };
                result.PeriodValues = new List<decimal> { beforeCurrent.Sum(r => r.Value ?? 0), currentYearRows.Sum(r => r.Value ?? 0) };
                result.PeriodCounts = new List<int> { beforeCurrent.Count, currentYearRows.Count };
            }

            return result;
        }

        public async Task<List<OverdueInvoiceRow>> GetOverdueInvoicesAsync(
            int? year, string? entity, string? department, string? portfolio,
            string? classification, string? category, string? paymentStatus, string? isLegalExecution,
            string? contractNumber = null, string? invoiceNumber = null, string statusFilter = "over")
        {
            var filtered = ApplyFilters(_db.DebtInvoiceRecords.AsNoTracking(), year, entity, department, portfolio, classification, category, paymentStatus, isLegalExecution, contractNumber, invoiceNumber);
            var query = statusFilter == "within"
                ? filtered.Where(r => r.DaysElapsed.HasValue && r.DaysElapsed.Value >= -60)
                : filtered.Where(r => r.DaysElapsed.HasValue && r.DaysElapsed.Value < -60);

            // الترتيب تصاعديًا لكلا الحالتين: للمتجاوزة يعرض الأكثر تجاوزًا أولاً (أصغر قيمة سالبة)،
            // ولضمن 60 يوم يعرض الأقرب لتجاوز الـ60 يوم أولاً بدل الفواتير غير المستحقة بعد (قيم
            // موجبة كبيرة كـ883 يومًا) التي كانت تظهر أولاً سابقًا وتبدو للمستخدمة وكأنها "متجاوزة"
            var ordered = query.OrderBy(r => r.DaysElapsed);

            return await ordered
                .Select(r => new OverdueInvoiceRow
                {
                    Id = r.Id,
                    InvoiceNumber = r.InvoiceNumber,
                    ContractNumber = r.ContractNumber,
                    Value = r.Value,
                    CreatedDate = r.CreatedDate,
                    DaysElapsed = statusFilter == "within" ? r.DaysElapsed : -r.DaysElapsed, // القيمة المطلقة للعرض للمتجاوزة (خام سالب اصطلاحًا)
                    PaymentStatus = r.PaymentStatus,
                    GeneralDepartment = r.GeneralDepartment,
                    Portfolio = r.Portfolio,
                    Category = r.Category,
                    LegalExecutionClaimStatus = r.LegalExecutionClaimStatus
                })
                .ToListAsync();
        }

        public async Task<DebtInvoiceRecord?> GetInvoiceDetailAsync(int id) =>
            await _db.DebtInvoiceRecords.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

        // الربط مع ملف المطالبات يتم عبر CONTRACT_ID (ContractIdRaw) لا "رقم العقد" - راجع
        // التعليق بـ ClaimsExecutionAnalyticsService.BuildDeptByContractAsync لتفاصيل التأكيد
        public async Task<List<DebtInvoiceRecord>> FindByContractNumberAsync(string contractNumber) =>
            await _db.DebtInvoiceRecords.AsNoTracking().Where(r => r.ContractIdRaw == contractNumber).ToListAsync();
    }
}
