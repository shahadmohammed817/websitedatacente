using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    /// <summary>صف واحد {اسم، عدد} - لدونات "المرحلة"</summary>
    public class NameCountRow
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>صف واحد {المرحلة، حالة الطلب، عدد} - لرسم "حالة الطلب حسب المرحلة" المكدَّس</summary>
    public class StageStatusRow
    {
        public string Stage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>صف واحد {المرحلة، حالة الطلب، السلة، عدد} - لرسم Lollipop "توزيع المسؤولين"
    /// المرتبط بالـTimeline: بعض قيم "حالة الطلب" (مثل "طلب جديد") تتكرر بأكثر من مرحلة وتوزيعها
    /// على السلة يختلف بين مرحلة وأخرى، فلا يكفي رابط status→basket وحده لتحديد سلة/مسؤول
    /// دقيقين عند اختيار مرحلة معيّنة + حالة معيّنة معًا</summary>
    public class StageStatusBasketRow
    {
        public string Stage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Basket { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>صف طلب واحد ضمن قائمة تفاصيل قوس "الأيام منذ آخر تحديث" - كل الطلبات (لا سقف)
    /// حتى تُفلتَر أحمر/أخضر من الواجهة عند الضغط على القوس، بنفس أسلوب صفحة "الطرح"</summary>
    public class OverdueRequestRow
    {
        public string? RequestNumber { get; set; }
        public string? Stage { get; set; }
        public string? BasketName { get; set; }
        public string? RequestStatus { get; set; }
        public string? EmployeeName { get; set; }
        public string? OpportunityNumber { get; set; }
        public string? OpportunityName { get; set; }
        public DateTime? LastUpdateDate { get; set; }
        public int DaysSinceUpdate { get; set; }
        public bool IsOverdue { get; set; }
    }

    /// <summary>بيانات صفحة "الطلبات المفتوحة"</summary>
    public class OpenRequestDashboardData
    {
        public bool HasData { get; set; }

        // ===== كروت الـKPI =====
        public int TotalCount { get; set; }
        public int Total2026Count { get; set; }
        public int OverdueCount { get; set; }   // عدد الأيام من تاريخ التحديث > 90
        public int OnTimeCount { get; set; }    // عدد الأيام من تاريخ التحديث <= 90
        public decimal OverduePercent { get; set; }

        // ===== خيارات الفلاتر =====
        public List<string> StageOptions { get; set; } = new();
        public List<int> YearOptions { get; set; } = new();

        // ===== 1) دونات: عدد الطلبات حسب المرحلة =====
        public List<NameCountRow> StageBreakdown { get; set; } = new();

        // ===== 3) حالات الطلب داخل كل مرحلة (Small Multiples - كل المراحل ظاهرة معًا دفعة واحدة) =====
        public List<string> StatusOptions { get; set; } = new();
        public List<StageStatusRow> StageStatusBreakdown { get; set; } = new();

        // ===== Lollipop "توزيع المسؤولين" - مرتبط بالـTimeline (مرحلة، أو مرحلة+حالة محدَّدتان) =====
        public List<StageStatusBasketRow> StageStatusBasketBreakdown { get; set; } = new();

        // ===== قوس أحمر/أخضر (تجاوز 90 يوم من آخر تحديث) - كل الطلبات لعرضها عند الضغط على القوس =====
        public List<OverdueRequestRow> OverdueDetailRows { get; set; } = new();
    }

    public interface IOpenRequestAnalyticsService
    {
        Task<OpenRequestDashboardData> GetDataAsync(List<string>? stages = null, string? search = null, List<int>? years = null);
    }

    /// <summary>
    /// يبني لوحة "الطلبات المفتوحة" من OpenRequestRecords. كل مؤشرات ورسوم الصفحة تُحسَب من
    /// نفس مجموعة السجلات المفلترة (المرحلة + بحث رقم الطلب)، عدا خيارات الفلاتر نفسها
    /// التي تُبنى من كل السجلات بلا تصفية حتى تبقى قوائمها ثابتة.
    /// </summary>
    public class OpenRequestAnalyticsService : IOpenRequestAnalyticsService
    {
        private const int OverdueThresholdDays = 90;
        private readonly AppDbContext _db;

        public OpenRequestAnalyticsService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<OpenRequestDashboardData> GetDataAsync(List<string>? stages = null, string? search = null, List<int>? years = null)
        {
            var result = new OpenRequestDashboardData();

            var allRecords = await _db.OpenRequestRecords.AsNoTracking().ToListAsync();
            result.HasData = allRecords.Count > 0;
            if (!result.HasData) return result;

            result.StageOptions = allRecords
                .Where(r => !string.IsNullOrWhiteSpace(r.Stage))
                .Select(r => r.Stage!.Trim())
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            result.YearOptions = allRecords
                .Where(r => r.RequestCreatedDate.HasValue)
                .Select(r => r.RequestCreatedDate!.Value.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            IEnumerable<OpenRequestRecord> query = allRecords;

            if (stages != null && stages.Count > 0)
                query = query.Where(r => r.Stage != null && stages.Contains(r.Stage));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(r => r.RequestNumber != null && r.RequestNumber.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (years != null && years.Count > 0)
                query = query.Where(r => r.RequestCreatedDate.HasValue && years.Contains(r.RequestCreatedDate.Value.Year));

            var records = query.ToList();
            if (records.Count == 0)
            {
                result.HasData = false;
                return result;
            }

            // ==================== كروت الـKPI ====================
            result.TotalCount = records.Count;
            result.Total2026Count = records.Count(r => r.RequestCreatedDate.HasValue && r.RequestCreatedDate.Value.Year == 2026);

            result.OverdueCount = records.Count(r => (r.DaysSinceUpdate ?? 0) > OverdueThresholdDays);
            result.OnTimeCount = records.Count - result.OverdueCount;
            result.OverduePercent = result.TotalCount > 0
                ? Math.Round(result.OverdueCount * 100m / result.TotalCount, 1)
                : 0;

            // ==================== 1) دونات المرحلة ====================
            result.StageBreakdown = records
                .Where(r => !string.IsNullOrWhiteSpace(r.Stage))
                .GroupBy(r => r.Stage!.Trim())
                .Select(g => new NameCountRow { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            // ==================== 3) حالة الطلب لكل مرحلة (عمود مكدَّس) ====================
            result.StatusOptions = records
                .Where(r => !string.IsNullOrWhiteSpace(r.RequestStatus))
                .Select(r => r.RequestStatus!.Trim())
                .Distinct()
                .ToList();

            result.StageStatusBreakdown = records
                .Where(r => !string.IsNullOrWhiteSpace(r.Stage) && !string.IsNullOrWhiteSpace(r.RequestStatus))
                .GroupBy(r => new { Stage = r.Stage!.Trim(), Status = r.RequestStatus!.Trim() })
                .Select(g => new StageStatusRow { Stage = g.Key.Stage, Status = g.Key.Status, Count = g.Count() })
                .ToList();

            // بلا سلة = "غير محددة" حتى يتطابق الإجمالي مع عدد الطلبات الفعلي - لا تُستبعد
            // الطلبات التي لم تُسنَد لسلة بعد
            static string BasketLabel(string? raw) => string.IsNullOrWhiteSpace(raw) ? "غير محددة" : raw.Trim();

            // ==================== Lollipop "توزيع المسؤولين" - ثلاثي (مرحلة، حالة، سلة) كامل حتى
            // يُحسَب توزيع السلة بدقّة لأي اختيار (مرحلة فقط، أو مرحلة+حالة معًا) من الواجهة دون
            // طلب شبكة إضافي - راجع تعليق StageStatusBasketRow لسبب الحاجة لثلاثي لا رابط ثنائي ====================
            result.StageStatusBasketBreakdown = records
                .Where(r => !string.IsNullOrWhiteSpace(r.Stage) && !string.IsNullOrWhiteSpace(r.RequestStatus))
                .GroupBy(r => new { Stage = r.Stage!.Trim(), Status = r.RequestStatus!.Trim(), Basket = BasketLabel(r.BasketName) })
                .Select(g => new StageStatusBasketRow { Stage = g.Key.Stage, Status = g.Key.Status, Basket = g.Key.Basket, Count = g.Count() })
                .ToList();

            // ==================== قوس "الأيام منذ آخر تحديث" - كل الطلبات (بلا سقف) لعرضها
            // أحمر/أخضر عند الضغط على القوس بالواجهة ====================
            result.OverdueDetailRows = records
                .OrderByDescending(r => r.DaysSinceUpdate ?? 0)
                .Select(r => new OverdueRequestRow
                {
                    RequestNumber = r.RequestNumber,
                    Stage = r.Stage,
                    BasketName = r.BasketName,
                    RequestStatus = r.RequestStatus,
                    EmployeeName = r.EmployeeName,
                    OpportunityNumber = r.OpportunityNumber,
                    OpportunityName = r.OpportunityName,
                    LastUpdateDate = r.LastUpdateDate,
                    DaysSinceUpdate = r.DaysSinceUpdate ?? 0,
                    IsOverdue = (r.DaysSinceUpdate ?? 0) > OverdueThresholdDays
                })
                .ToList();

            return result;
        }
    }
}
