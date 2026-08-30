using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    public class InvestmentActivityPageData
    {
        public bool HasData { get; set; }

        // ==================== كروت KPI ====================
        public int TotalCount { get; set; }
        public decimal TotalAnnualRent { get; set; }
        public List<InvActTypeRow> ContractTypeSummary { get; set; } = new();

        // ==================== رسم 1+2: عدد/قيمة العقود حسب القطاع ====================
        public List<InvActSectorRow> SectorSummary { get; set; } = new();

        // ==================== رسم 3: الأجرة السنوية حسب سنة العقد (خط) ====================
        public List<InvActYearRow> YearlyRent { get; set; } = new();

        // ==================== بطاقة: العقد صاحب أعلى أجرة سنوية ====================
        public InvActTopContractRow? TopContract { get; set; }

        // ==================== الخريطة ====================
        public List<InvActMapPoint> MapPoints { get; set; } = new();

        // ==================== الفلاتر ====================
        public List<int> YearOptions { get; set; } = new();
        public List<string> ActivityOptions { get; set; } = new();
        public List<string> RegionOptions { get; set; } = new();
    }

    public class InvActTypeRow
    {
        public string ContractType { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAnnualRent { get; set; }
    }

    public class InvActTopContractRow
    {
        public string? OpportunityName { get; set; }
        public string? ContractNumber { get; set; }
        public decimal AnnualRent { get; set; }
        public string? Region { get; set; }
        public string? Sector { get; set; }
    }

    public class InvActSectorRow
    {
        public string Sector { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAnnualRent { get; set; }
    }

    public class InvActYearRow
    {
        public int Year { get; set; }
        public decimal TotalAnnualRent { get; set; }
    }

    public class InvActMapPoint
    {
        public string? OpportunityName { get; set; }
        public DateTime? ContractDate { get; set; }
        public decimal? AnnualRent { get; set; }
        public string? Activity { get; set; }
        public string? Region { get; set; }
        public string? ContractType { get; set; }
        public decimal X { get; set; }
        public decimal Y { get; set; }
    }

    public interface IInvestmentActivityAnalyticsService
    {
        Task<InvestmentActivityPageData> GetDataAsync(int? year, string? activity, string? region);
    }

    public class InvestmentActivityAnalyticsService : IInvestmentActivityAnalyticsService
    {
        private readonly AppDbContext _db;

        public InvestmentActivityAnalyticsService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<InvestmentActivityPageData> GetDataAsync(int? year, string? activity, string? region)
        {
            var result = new InvestmentActivityPageData();

            var allRecords = await _db.InvestmentActivityRecords.AsNoTracking().ToListAsync();
            result.HasData = allRecords.Count > 0;
            if (!result.HasData) return result;

            result.YearOptions = allRecords
                .Where(r => r.ContractYear.HasValue)
                .Select(r => r.ContractYear!.Value).Distinct().OrderByDescending(y => y).ToList();
            result.ActivityOptions = allRecords
                .Where(r => !string.IsNullOrWhiteSpace(r.Activity))
                .Select(r => r.Activity!).Distinct().OrderBy(x => x).ToList();
            result.RegionOptions = allRecords
                .Where(r => !string.IsNullOrWhiteSpace(r.Region))
                .Select(r => r.Region!).Distinct().OrderBy(x => x).ToList();

            IEnumerable<InvestmentActivityRecord> rows = allRecords;
            if (year.HasValue) rows = rows.Where(r => r.ContractYear == year.Value);
            if (!string.IsNullOrWhiteSpace(activity)) rows = rows.Where(r => r.Activity == activity);
            if (!string.IsNullOrWhiteSpace(region)) rows = rows.Where(r => r.Region == region);
            var filteredRows = rows.ToList();

            // ==================== كروت KPI ====================
            result.TotalCount = filteredRows.Count;
            result.TotalAnnualRent = filteredRows.Sum(r => r.AnnualRent ?? 0);

            result.ContractTypeSummary = filteredRows
                .Where(r => !string.IsNullOrWhiteSpace(r.ContractType))
                .GroupBy(r => r.ContractType!)
                .Select(g => new InvActTypeRow { ContractType = g.Key, Count = g.Count(), TotalAnnualRent = g.Sum(r => r.AnnualRent ?? 0) })
                .OrderByDescending(x => x.TotalAnnualRent)
                .ToList();

            // ==================== بطاقة: العقد صاحب أعلى أجرة سنوية ====================
            var topRecord = filteredRows
                .Where(r => r.AnnualRent.HasValue && r.AnnualRent.Value > 0)
                .OrderByDescending(r => r.AnnualRent!.Value)
                .FirstOrDefault();
            if (topRecord != null)
            {
                result.TopContract = new InvActTopContractRow
                {
                    OpportunityName = topRecord.OpportunityName,
                    ContractNumber = topRecord.ContractNumber,
                    AnnualRent = topRecord.AnnualRent!.Value,
                    Region = topRecord.Region,
                    Sector = topRecord.Sector
                };
            }

            // ==================== رسم 1+2: عدد/قيمة العقود حسب القطاع ====================
            result.SectorSummary = filteredRows
                .Where(r => !string.IsNullOrWhiteSpace(r.Sector))
                .GroupBy(r => r.Sector!)
                .Select(g => new InvActSectorRow { Sector = g.Key, Count = g.Count(), TotalAnnualRent = g.Sum(r => r.AnnualRent ?? 0) })
                .OrderByDescending(x => x.Count)
                .ToList();

            // ==================== رسم 3: الأجرة السنوية حسب سنة العقد ====================
            result.YearlyRent = filteredRows
                .Where(r => r.ContractYear.HasValue)
                .GroupBy(r => r.ContractYear!.Value)
                .Select(g => new InvActYearRow { Year = g.Key, TotalAnnualRent = g.Sum(r => r.AnnualRent ?? 0) })
                .OrderBy(x => x.Year)
                .ToList();

            // ==================== الخريطة ====================
            result.MapPoints = filteredRows
                .Where(r => r.X.HasValue && r.Y.HasValue)
                .Select(r => new InvActMapPoint
                {
                    OpportunityName = r.OpportunityName,
                    ContractDate = r.ContractDate,
                    AnnualRent = r.AnnualRent,
                    Activity = r.Activity,
                    Region = r.Region,
                    ContractType = r.ContractType,
                    X = r.X!.Value,
                    Y = r.Y!.Value
                })
                .ToList();

            return result;
        }
    }
}
