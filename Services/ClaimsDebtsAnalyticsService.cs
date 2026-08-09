using System.Globalization;
using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    /// <summary>بيانات صفحة "المطالبات والمديونيات" (نظرة عامة)</summary>
    public class ClaimsOverviewData
    {
        public bool HasData { get; set; }

        public int TotalClaims { get; set; }
        public int TotalDebts { get; set; }
        public decimal TotalClaimsValue { get; set; }
        public decimal TotalDebtsValue { get; set; }
        public decimal CollectionRatePercent { get; set; }
        public int OverdueClaimsCount { get; set; }
        public decimal AvgProcessingDays { get; set; }

        /// <summary>عدد/نسبة المطالبات المُنجَزة (منتهية) مقابل المفتوحة - لعرض "نسبة المعالجة"</summary>
        public int ResolvedClaimsCount { get; set; }
        public int OpenClaimsCount { get; set; }
        public decimal ResolvedRatePercent { get; set; }

        /// <summary>محور شهري موحّد (اتحاد أشهر المطالبات والمديونيات معًا) لرسم مقارن واحد بدل رسمين منفصلين</summary>
        public List<string> MonthLabels { get; set; } = new();
        public List<int> MonthClaimsCounts { get; set; } = new();
        public List<int> MonthDebtsCounts { get; set; } = new();

        public List<string> TopClaimDeptLabels { get; set; } = new();
        public List<int> TopClaimDeptCounts { get; set; } = new();
        public List<string> TopDebtDeptLabels { get; set; } = new();
        public List<int> TopDebtDeptCounts { get; set; } = new();

        /// <summary>مقارنة المطالبات مقابل المديونيات لكل إدارة (نفس فئات الإدارات على المحورين)</summary>
        public List<string> DeptCompareLabels { get; set; } = new();
        public List<int> DeptCompareClaimsCounts { get; set; } = new();
        public List<int> DeptCompareDebtsCounts { get; set; } = new();

        public List<string> ClaimsStatusLabels { get; set; } = new();
        public List<int> ClaimsStatusCounts { get; set; } = new();
        public List<string> DebtsStatusLabels { get; set; } = new();
        public List<int> DebtsStatusCounts { get; set; } = new();

        /// <summary>توزيع قيمة المطالبات/المديونيات حسب البند (نفس فئات البنود على المحورين)</summary>
        public List<string> ItemLabels { get; set; } = new();
        public List<decimal> ItemClaimsValues { get; set; } = new();
        public List<decimal> ItemDebtsValues { get; set; } = new();

        // ==================== 1) حالة المديونيات حسب البند الفرعي (Stacked Bar) ====================
        // لكل مديونية: 🟢 تمت معالجتها (كل مطالباتها المرتبطة بنفس رقم العقد منتهية) /
        // 🟡 لديها مطالبات قيد المعالجة / 🔴 لا يوجد لها مطالبات مرتبطة إطلاقًا
        public List<string> DebtItemLabels { get; set; } = new();
        public List<int> DebtItemProcessedCounts { get; set; } = new();   // 🟢
        public List<int> DebtItemInProgressCounts { get; set; } = new(); // 🟡
        public List<int> DebtItemNoClaimsCounts { get; set; } = new();   // 🔴

        // ==================== 2) توزيع عدد المطالبات لكل مديونية (Histogram) + المديونيات التي لديها مطالبات ====================
        public List<string> ClaimsPerDebtBucketLabels { get; set; } = new();
        public List<int> ClaimsPerDebtBucketCounts { get; set; } = new();
        /// <summary>كل المديونيات التي لديها مطالبة واحدة على الأقل (مرتّبة تنازليًا)، حتى حد أقصى
        /// معقول يكفي للعرض التفاعلي (Top 10 افتراضيًا + بحث/تصفية على الباقي بالواجهة)</summary>
        public List<DebtClaimCountRow> DebtsWithClaims { get; set; } = new();

        // ==================== 3+4) حالة المطالبات حسب البند الفرعي (منجزة/متبقية) + توزيع عام ====================
        public List<string> ClaimItemLabels { get; set; } = new();
        public List<int> ClaimItemResolvedCounts { get; set; } = new();
        public List<int> ClaimItemOpenCounts { get; set; } = new();
    }

    /// <summary>صف مديونية واحدة في قائمة "المديونيات التي لديها مطالبات"</summary>
    public class DebtClaimCountRow
    {
        public string DebtNumber { get; set; } = string.Empty;
        public int ClaimCount { get; set; }
    }

    /// <summary>صف واحد بقسم "الحالات التي تحتاج متابعة عاجلة" - يجمع مطالبات ومديونيات معًا</summary>
    public class AttentionItem
    {
        public string Type { get; set; } = string.Empty; // مطالبة متأخرة / مديونية غير محصلة / مطالبة بلا إجراء
        public string? Number { get; set; }
        public string? Department { get; set; }
        public decimal? Value { get; set; }
        public int? DaysOpen { get; set; }
        public string AlertLevel { get; set; } = "yellow"; // green / yellow / red
        public string? Status { get; set; }
    }

    public class MonthComparison
    {
        public string CurrentMonthLabel { get; set; } = string.Empty;
        public string PreviousMonthLabel { get; set; } = string.Empty;
        public int CurrentClaimsCount { get; set; }
        public int PreviousClaimsCount { get; set; }
        public int CurrentCompletedCount { get; set; }
        public int PreviousCompletedCount { get; set; }
        public decimal CurrentCompletionRatePercent { get; set; }
        public decimal PreviousCompletionRatePercent { get; set; }
    }

    public class DepartmentSpeedRow
    {
        public string Department { get; set; } = string.Empty;
        public decimal AvgDurationDays { get; set; }
        public int CompletedCount { get; set; }
    }

    /// <summary>بيانات صفحة "متابعة الأداء"</summary>
    public class ClaimsFollowupData
    {
        public bool HasData { get; set; }

        public int TotalClaims { get; set; }
        public int OverdueClaimsCount { get; set; }
        public decimal AvgCompletionDays { get; set; }
        public string? FastestDepartment { get; set; }
        public string? SlowestDepartment { get; set; }
        public decimal OverallCompletionRatePercent { get; set; }

        public List<AttentionItem> AttentionItems { get; set; } = new();
        public MonthComparison Comparison { get; set; } = new();
        public List<DepartmentSpeedRow> DepartmentSpeedRanking { get; set; } = new();
    }

    public interface IClaimsDebtsAnalyticsService
    {
        Task<ClaimsOverviewData> GetOverviewDataAsync();
        Task<ClaimsFollowupData> GetFollowupDataAsync();
    }

    public class ClaimsDebtsAnalyticsService : IClaimsDebtsAnalyticsService
    {
        private readonly AppDbContext _db;

        private const int OverdueDaysThreshold = 30;
        private const int LateDaysThreshold = 60;

        private static readonly string[] CollectedStatusKeywords = { "محصل", "مسدد", "مسوى", "مدفوع", "سدد" };
        private static readonly string[] ProblemStatusKeywords = { "متأخر", "معلق", "مرفوض" };

        private static readonly string[] ArabicMonthNames =
        {
            "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
            "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"
        };

        public ClaimsDebtsAnalyticsService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// يزيل التشكيل العربي (شدة، فتحة، ضمة، كسرة، سكون...) من النص حتى تنجح مطابقة الكلمات
        /// المفتاحية حتى لو كتب المستخدم الحالة بتشكيل كامل (مثل "محصّلة" بدل "محصله")
        /// </summary>
        internal static string StripArabicDiacritics(string value)
        {
            var chars = value.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark);
            return new string(chars.ToArray());
        }

        private static bool IsDebtCollected(DebtRecord d)
        {
            if (d.CollectedValue.HasValue && d.Value.HasValue)
                return d.CollectedValue.Value >= d.Value.Value;

            if (!string.IsNullOrWhiteSpace(d.Status))
            {
                var status = StripArabicDiacritics(d.Status);
                if (status.Contains("غير")) return false;
                return CollectedStatusKeywords.Any(k => status.Contains(k));
            }

            return false;
        }

        /// <summary>
        /// تعريف "المطالبة المعالجة" حسب قاعدة العمل المعتمدة صراحةً من المستخدم: العمود الفعلي في
        /// ملف البيانات هو "حالة المطالبة" (وليس أي عمود آخر)، ولا توجد به أي قيمة نصها الحرفي
        /// "مرسلة لسند" - القيمة الفعلية الوحيدة التي أكّد المستخدم أنها تمثّل "معالجة" هي
        /// "مطالبة تم ارسالها لنظام القضايا بنجاح". أي حالة أخرى (بما فيها الفارغة و#N/A بعد
        /// معاملتها كفارغة عند الاستيراد) تُعتبر "متبقية" - لا افتراضات إضافية على قيم غير مؤكَّدة.
        /// </summary>
        private static bool IsClaimResolved(ClaimRecord c)
        {
            if (string.IsNullOrWhiteSpace(c.Status)) return false;
            var status = StripArabicDiacritics(c.Status);
            return status.Contains("ارسال") && status.Contains("بنجاح");
        }

        public async Task<ClaimsOverviewData> GetOverviewDataAsync()
        {
            var claims = await _db.ClaimRecords.ToListAsync();
            var debts = await _db.DebtRecords.ToListAsync();

            var result = new ClaimsOverviewData { HasData = claims.Count > 0 || debts.Count > 0 };
            if (!result.HasData) return result;

            result.TotalClaims = claims.Count;
            result.TotalDebts = debts.Count;
            result.TotalClaimsValue = claims.Sum(c => c.Value ?? 0);
            result.TotalDebtsValue = debts.Sum(d => d.Value ?? 0);

            var collectedDebtsValue = debts.Where(IsDebtCollected).Sum(d => d.CollectedValue ?? d.Value ?? 0);
            if (collectedDebtsValue > 0)
            {
                // جدول المديونيات نفسه يحمل مبالغ محصّلة صراحة (عمود "المحصل" أو حالة تدل على
                // التحصيل) - تُحسب النسبة مباشرة منها
                result.CollectionRatePercent = result.TotalDebtsValue > 0
                    ? Math.Round(collectedDebtsValue / result.TotalDebtsValue * 100, 1)
                    : 0;
            }
            else if (result.TotalClaimsValue > 0)
            {
                // الحالة الأشيع: "المديونيات" هي فقط الجزء غير المسدَّد من نفس عالم "المطالبات"
                // (كشف دفعات واحد)، فتُحسب نسبة التحصيل كنسبة ما تم سداده فعليًا من إجمالي
                // قيمة المطالبات (الإجمالي ناقص المديونيات القائمة حاليًا)
                var collected = Math.Max(0, result.TotalClaimsValue - result.TotalDebtsValue);
                result.CollectionRatePercent = Math.Round(collected / result.TotalClaimsValue * 100, 1);
            }

            var now = DateTime.Now;
            result.OverdueClaimsCount = claims.Count(c =>
                !IsClaimResolved(c) && c.ClaimDate.HasValue && (now - c.ClaimDate.Value).TotalDays > OverdueDaysThreshold);

            result.ResolvedClaimsCount = claims.Count(IsClaimResolved);
            result.OpenClaimsCount = result.TotalClaims - result.ResolvedClaimsCount;
            result.ResolvedRatePercent = result.TotalClaims > 0
                ? Math.Round((decimal)result.ResolvedClaimsCount / result.TotalClaims * 100, 1)
                : 0;

            var completedDurations = claims
                .Where(c => c.ClaimDate.HasValue && c.CompletionDate.HasValue)
                .Select(c => (c.CompletionDate!.Value - c.ClaimDate!.Value).TotalDays)
                .Where(d => d >= 0)
                .ToList();
            result.AvgProcessingDays = completedDurations.Count > 0 ? Math.Round((decimal)completedDurations.Average(), 1) : 0;

            // محور شهري موحّد (اتحاد أشهر المطالبات والمديونيات) حتى يمكن رسمهما معًا في رسم
            // واحد مقارن بدل رسمين منفصلين بمحاور مختلفة
            var claimsByMonthKey = claims
                .Where(c => c.ClaimDate.HasValue)
                .GroupBy(c => $"{c.ClaimDate!.Value.Year}-{c.ClaimDate!.Value.Month:D2}")
                .ToDictionary(g => g.Key, g => g.Count());
            var debtsByMonthKey = debts
                .Where(d => d.DebtDate.HasValue)
                .GroupBy(d => $"{d.DebtDate!.Value.Year}-{d.DebtDate!.Value.Month:D2}")
                .ToDictionary(g => g.Key, g => g.Count());

            var allMonthKeys = claimsByMonthKey.Keys.Concat(debtsByMonthKey.Keys).Distinct().OrderBy(k => k).ToList();
            result.MonthLabels = allMonthKeys
                .Select(k => $"{ArabicMonthNames[int.Parse(k.Substring(5, 2)) - 1]} {k.Substring(0, 4)}")
                .ToList();
            result.MonthClaimsCounts = allMonthKeys.Select(k => claimsByMonthKey.TryGetValue(k, out var v) ? v : 0).ToList();
            result.MonthDebtsCounts = allMonthKeys.Select(k => debtsByMonthKey.TryGetValue(k, out var v) ? v : 0).ToList();

            var topClaimDepts = claims
                .Where(c => !string.IsNullOrWhiteSpace(c.Department))
                .GroupBy(c => c.Department!.Trim())
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(8)
                .ToList();
            result.TopClaimDeptLabels = topClaimDepts.Select(x => x.Label).ToList();
            result.TopClaimDeptCounts = topClaimDepts.Select(x => x.Count).ToList();

            var topDebtDepts = debts
                .Where(d => !string.IsNullOrWhiteSpace(d.Department))
                .GroupBy(d => d.Department!.Trim())
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(8)
                .ToList();
            result.TopDebtDeptLabels = topDebtDepts.Select(x => x.Label).ToList();
            result.TopDebtDeptCounts = topDebtDepts.Select(x => x.Count).ToList();

            // مقارنة المطالبات مقابل المديونيات لكل إدارة على نفس فئات المحور (أعلى الإدارات
            // حسب إجمالي عدد المطالبات) - توضح "من كل مطالبات هذه الإدارة، كم منها لا يزال مديونية"
            var debtCountByDept = debts
                .Where(d => !string.IsNullOrWhiteSpace(d.Department))
                .GroupBy(d => d.Department!.Trim())
                .ToDictionary(g => g.Key, g => g.Count());
            var deptCompare = topClaimDepts.Take(8).ToList();
            result.DeptCompareLabels = deptCompare.Select(x => x.Label).ToList();
            result.DeptCompareClaimsCounts = deptCompare.Select(x => x.Count).ToList();
            result.DeptCompareDebtsCounts = deptCompare.Select(x => debtCountByDept.TryGetValue(x.Label, out var v) ? v : 0).ToList();

            var claimsStatus = claims
                .Where(c => !string.IsNullOrWhiteSpace(c.Status))
                .GroupBy(c => c.Status!.Trim())
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();
            result.ClaimsStatusLabels = claimsStatus.Select(x => x.Label).ToList();
            result.ClaimsStatusCounts = claimsStatus.Select(x => x.Count).ToList();

            var debtsStatus = debts
                .Where(d => !string.IsNullOrWhiteSpace(d.Status))
                .GroupBy(d => d.Status!.Trim())
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();
            result.DebtsStatusLabels = debtsStatus.Select(x => x.Label).ToList();
            result.DebtsStatusCounts = debtsStatus.Select(x => x.Count).ToList();

            // توزيع القيمة حسب البند: أعلى البنود حسب قيمة المطالبات، مع قيمة المديونيات
            // المقابلة لنفس البند (0 إن لم توجد) لعرض مقارنة مباشرة بينهما لكل بند
            var itemClaimValues = claims
                .Where(c => !string.IsNullOrWhiteSpace(c.ItemCategory))
                .GroupBy(c => c.ItemCategory!.Trim())
                .Select(g => new { Label = g.Key, Total = g.Sum(x => x.Value ?? 0) })
                .Where(x => x.Total > 0)
                .OrderByDescending(x => x.Total)
                .Take(8)
                .ToList();
            var itemDebtValuesByLabel = debts
                .Where(d => !string.IsNullOrWhiteSpace(d.ItemCategory))
                .GroupBy(d => d.ItemCategory!.Trim())
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Value ?? 0));

            result.ItemLabels = itemClaimValues.Select(x => x.Label).ToList();
            result.ItemClaimsValues = itemClaimValues.Select(x => x.Total).ToList();
            result.ItemDebtsValues = itemClaimValues.Select(x => itemDebtValuesByLabel.TryGetValue(x.Label, out var v) ? v : 0).ToList();

            // مطالبات كل عقد (رقم العقد) - المفتاح الرابط بين مديونية ومطالباتها
            var claimsByContract = claims
                .Where(c => !string.IsNullOrWhiteSpace(c.ContractNumber))
                .GroupBy(c => c.ContractNumber!.Trim())
                .ToDictionary(g => g.Key, g => g.ToList());

            // ==================== 1) حالة المديونيات حسب البند الفرعي ====================
            var debtItemStatus = debts
                .Where(d => !string.IsNullOrWhiteSpace(d.ItemCategory))
                .GroupBy(d => d.ItemCategory!.Trim())
                .Select(g =>
                {
                    int processed = 0, inProgress = 0, noClaims = 0;
                    foreach (var d in g)
                    {
                        List<ClaimRecord>? linked = (!string.IsNullOrWhiteSpace(d.ContractNumber) && claimsByContract.TryGetValue(d.ContractNumber.Trim(), out var list))
                            ? list : null;
                        if (linked == null || linked.Count == 0) noClaims++;
                        else if (linked.All(IsClaimResolved)) processed++;
                        else inProgress++;
                    }
                    return new { Item = g.Key, Processed = processed, InProgress = inProgress, NoClaims = noClaims, Total = g.Count() };
                })
                .OrderByDescending(x => x.Total)
                .Take(12)
                .ToList();
            result.DebtItemLabels = debtItemStatus.Select(x => x.Item).ToList();
            result.DebtItemProcessedCounts = debtItemStatus.Select(x => x.Processed).ToList();
            result.DebtItemInProgressCounts = debtItemStatus.Select(x => x.InProgress).ToList();
            result.DebtItemNoClaimsCounts = debtItemStatus.Select(x => x.NoClaims).ToList();

            // ==================== 2) توزيع عدد المطالبات لكل مديونية + أعلى العقود ====================
            var debtClaimCounts = debts
                .Select(d => (!string.IsNullOrWhiteSpace(d.ContractNumber) && claimsByContract.TryGetValue(d.ContractNumber.Trim(), out var list)) ? list.Count : 0)
                .Where(c => c > 0)
                .ToList();

            result.ClaimsPerDebtBucketLabels = new List<string> { "مطالبة واحدة", "مطالبتان", "3-5 مطالبات", "أكثر من 5" };
            result.ClaimsPerDebtBucketCounts = new List<int>
            {
                debtClaimCounts.Count(c => c == 1),
                debtClaimCounts.Count(c => c == 2),
                debtClaimCounts.Count(c => c >= 3 && c <= 5),
                debtClaimCounts.Count(c => c > 5)
            };

            // كل مديونية (رقم مديونية) لها مطالبة واحدة على الأقل، مع عدد المطالبات المرتبطة بها
            // عبر رقم العقد المشترك. تُرسَل قائمة أوسع من Top 10 (حتى 100) لتفعيل البحث/التصفية
            // بالواجهة دون تحميل آلاف المديونيات كاملة.
            result.DebtsWithClaims = debts
                .Where(d => !string.IsNullOrWhiteSpace(d.DebtNumber)
                    && !string.IsNullOrWhiteSpace(d.ContractNumber)
                    && claimsByContract.ContainsKey(d.ContractNumber.Trim()))
                .Select(d => new DebtClaimCountRow
                {
                    DebtNumber = d.DebtNumber!.Trim(),
                    ClaimCount = claimsByContract[d.ContractNumber!.Trim()].Count
                })
                .OrderByDescending(x => x.ClaimCount)
                .Take(100)
                .ToList();

            // ==================== 3+4) حالة المطالبات حسب البند الفرعي (منجزة/متبقية) ====================
            var claimItemStatus = claims
                .Where(c => !string.IsNullOrWhiteSpace(c.ItemCategory))
                .GroupBy(c => c.ItemCategory!.Trim())
                .Select(g => new
                {
                    Item = g.Key,
                    Resolved = g.Count(IsClaimResolved),
                    Open = g.Count(c => !IsClaimResolved(c))
                })
                .OrderByDescending(x => x.Resolved + x.Open)
                .Take(12)
                .ToList();
            result.ClaimItemLabels = claimItemStatus.Select(x => x.Item).ToList();
            result.ClaimItemResolvedCounts = claimItemStatus.Select(x => x.Resolved).ToList();
            result.ClaimItemOpenCounts = claimItemStatus.Select(x => x.Open).ToList();

            return result;
        }

        public async Task<ClaimsFollowupData> GetFollowupDataAsync()
        {
            var claims = await _db.ClaimRecords.ToListAsync();
            var debts = await _db.DebtRecords.ToListAsync();

            var result = new ClaimsFollowupData { HasData = claims.Count > 0 || debts.Count > 0 };
            if (!result.HasData) return result;

            result.TotalClaims = claims.Count;
            var now = DateTime.Now;

            var openClaims = claims.Where(c => !IsClaimResolved(c)).ToList();
            var overdueClaims = openClaims.Where(c => c.ClaimDate.HasValue && (now - c.ClaimDate.Value).TotalDays > OverdueDaysThreshold).ToList();
            result.OverdueClaimsCount = overdueClaims.Count;

            var resolvedClaims = claims.Where(IsClaimResolved).ToList();
            result.OverallCompletionRatePercent = claims.Count > 0
                ? Math.Round((decimal)resolvedClaims.Count / claims.Count * 100, 1)
                : 0;

            // مدة المعالجة وترتيب سرعة الإدارات تحتاج تاريخَي بداية ونهاية فعليين لحساب عدد
            // الأيام - يقتصران على المطالبات التي يتوفر لها تاريخ إنجاز صريح فقط (وليس كل
            // المطالبات "المنتهية" التي قد تعتمد على نص الحالة فقط بدون تاريخ)
            var completed = claims.Where(c => c.ClaimDate.HasValue && c.CompletionDate.HasValue).ToList();
            var durations = completed.Select(c => (decimal)(c.CompletionDate!.Value - c.ClaimDate!.Value).TotalDays).Where(d => d >= 0).ToList();
            result.AvgCompletionDays = durations.Count > 0 ? Math.Round(durations.Average(), 1) : 0;

            // ترتيب الإدارات حسب سرعة الإنجاز (متوسط أيام المعالجة تصاعديًا - الأسرع أولاً)
            var deptSpeed = completed
                .Where(c => !string.IsNullOrWhiteSpace(c.Department))
                .GroupBy(c => c.Department!.Trim())
                .Select(g => new DepartmentSpeedRow
                {
                    Department = g.Key,
                    AvgDurationDays = Math.Round((decimal)g.Average(c => (c.CompletionDate!.Value - c.ClaimDate!.Value).TotalDays), 1),
                    CompletedCount = g.Count()
                })
                .OrderBy(x => x.AvgDurationDays)
                .ToList();
            result.DepartmentSpeedRanking = deptSpeed;

            // بطاقتا "أسرع/أكثر إدارة تأخيرًا" تعتمدان فقط على إدارات لها عدد كافٍ من المطالبات
            // المنجزة (لا تُحسم بمطالبة منجزة واحدة قد تكون استثناءً غير ممثِّل للأداء الفعلي)،
            // بينما يبقى جدول/رسم الترتيب الكامل أعلاه شاملاً كل الإدارات مهما قلّ عددها
            const int MinSampleForRanking = 3;
            var reliableDeptSpeed = deptSpeed.Where(d => d.CompletedCount >= MinSampleForRanking).ToList();
            var rankingPool = reliableDeptSpeed.Count > 0 ? reliableDeptSpeed : deptSpeed;
            result.FastestDepartment = rankingPool.FirstOrDefault()?.Department;
            result.SlowestDepartment = rankingPool.LastOrDefault()?.Department;

            // ==================== الحالات التي تحتاج متابعة عاجلة ====================
            var attention = new List<AttentionItem>();

            foreach (var c in overdueClaims)
            {
                var days = (int)(now - c.ClaimDate!.Value).TotalDays;
                attention.Add(new AttentionItem
                {
                    Type = "مطالبة متأخرة",
                    Number = c.ClaimNumber,
                    Department = c.Department,
                    Value = c.Value,
                    DaysOpen = days,
                    AlertLevel = days > LateDaysThreshold ? "red" : "yellow",
                    Status = c.Status
                });
            }

            foreach (var d in debts.Where(d => !IsDebtCollected(d)))
            {
                int? days = d.DebtDate.HasValue ? (int)(now - d.DebtDate.Value).TotalDays : (int?)null;
                attention.Add(new AttentionItem
                {
                    Type = "مديونية غير محصّلة",
                    Number = d.DebtNumber,
                    Department = d.Department,
                    Value = d.Value,
                    DaysOpen = days,
                    AlertLevel = days.HasValue ? (days > LateDaysThreshold ? "red" : (days > OverdueDaysThreshold ? "yellow" : "green")) : "yellow",
                    Status = d.Status
                });
            }

            // "بلا إجراء" - تُطبَّق فقط إن كان عمود تاريخ الإجراء موجودًا فعليًا بالملف المستورد
            // (بعض قيمه معبأة)، وإلا تُتجاهل حتى لا تُكرِّر كل المطالبات المفتوحة بلا فائدة
            bool hasActionDateData = claims.Any(c => c.ActionDate.HasValue);
            if (hasActionDateData)
            {
                var alreadyListedNumbers = overdueClaims.Select(c => c.ClaimNumber).ToHashSet();
                foreach (var c in openClaims.Where(c => c.ActionDate == null && !alreadyListedNumbers.Contains(c.ClaimNumber)))
                {
                    int? days = c.ClaimDate.HasValue ? (int)(now - c.ClaimDate.Value).TotalDays : (int?)null;
                    attention.Add(new AttentionItem
                    {
                        Type = "مطالبة بلا إجراء",
                        Number = c.ClaimNumber,
                        Department = c.Department,
                        Value = c.Value,
                        DaysOpen = days,
                        AlertLevel = days.HasValue ? (days > LateDaysThreshold ? "red" : (days > OverdueDaysThreshold ? "yellow" : "green")) : "yellow",
                        Status = c.Status
                    });
                }
            }

            // أي حالة أخرى تستحق المتابعة وفق نص الحالة نفسه (متأخرة/معلقة/مرفوضة) ولم تُدرَج أعلاه
            var listedNumbers = attention.Select(a => a.Number).Where(n => n != null).ToHashSet();
            foreach (var c in claims.Where(c => !string.IsNullOrWhiteSpace(c.Status)
                && ProblemStatusKeywords.Any(k => StripArabicDiacritics(c.Status!).Contains(k))
                && !listedNumbers.Contains(c.ClaimNumber)))
            {
                int? days = c.ClaimDate.HasValue ? (int)(now - c.ClaimDate.Value).TotalDays : (int?)null;
                attention.Add(new AttentionItem
                {
                    Type = "مطالبة تستحق المتابعة",
                    Number = c.ClaimNumber,
                    Department = c.Department,
                    Value = c.Value,
                    DaysOpen = days,
                    AlertLevel = days.HasValue ? (days > LateDaysThreshold ? "red" : (days > OverdueDaysThreshold ? "yellow" : "green")) : "yellow",
                    Status = c.Status
                });
            }

            result.AttentionItems = attention
                .OrderByDescending(a => a.AlertLevel == "red" ? 2 : (a.AlertLevel == "yellow" ? 1 : 0))
                .ThenByDescending(a => a.DaysOpen ?? 0)
                .ToList();

            // ==================== مقارنة الشهر الحالي بالشهر السابق ====================
            var currentMonthStart = new DateTime(now.Year, now.Month, 1);
            var previousMonthStart = currentMonthStart.AddMonths(-1);

            var currentMonthClaims = claims.Where(c => c.ClaimDate.HasValue && c.ClaimDate.Value >= currentMonthStart && c.ClaimDate.Value < currentMonthStart.AddMonths(1)).ToList();
            var previousMonthClaims = claims.Where(c => c.ClaimDate.HasValue && c.ClaimDate.Value >= previousMonthStart && c.ClaimDate.Value < currentMonthStart).ToList();

            var currentCompleted = currentMonthClaims.Count(IsClaimResolved);
            var previousCompleted = previousMonthClaims.Count(IsClaimResolved);

            result.Comparison = new MonthComparison
            {
                CurrentMonthLabel = $"{ArabicMonthNames[currentMonthStart.Month - 1]} {currentMonthStart.Year}",
                PreviousMonthLabel = $"{ArabicMonthNames[previousMonthStart.Month - 1]} {previousMonthStart.Year}",
                CurrentClaimsCount = currentMonthClaims.Count,
                PreviousClaimsCount = previousMonthClaims.Count,
                CurrentCompletedCount = currentCompleted,
                PreviousCompletedCount = previousCompleted,
                CurrentCompletionRatePercent = currentMonthClaims.Count > 0 ? Math.Round((decimal)currentCompleted / currentMonthClaims.Count * 100, 1) : 0,
                PreviousCompletionRatePercent = previousMonthClaims.Count > 0 ? Math.Round((decimal)previousCompleted / previousMonthClaims.Count * 100, 1) : 0
            };

            return result;
        }
    }
}
