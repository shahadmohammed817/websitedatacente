using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    public class ContractPeriodPageData
    {
        public bool HasData { get; set; }

        public int TotalCount { get; set; }
        public int InvestmentCount { get; set; } // نوع العقد = استثمار
        public int DirectLeaseCount { get; set; } // نوع العقد = تأجير مباشر

        // حسب الإدارة العامة/الفرعية - حالة العقد (خارج/داخل فترة التجهيز/متعثر...) - رسمان مكدَّسان
        public List<string> DeptLabels { get; set; } = new();
        public List<string> StatusOptions { get; set; } = new(); // القيم الحقيقية الخمس، مرتّبة تنازليًا حسب التكرار
        public List<ContractStatusGroupRow> DeptStatusBreakdown { get; set; } = new();
        public List<ContractSubDeptStatusGroupRow> SubDeptStatusBreakdown { get; set; } = new();

        // "العقود التي تنتهي فترة سماحها خلال 2026" - النطاق = العقود اللي "نهاية فترة التجهيز"
        // وقعت بسنة 2026 بالضبط فقط، مصنّفة حسب عدد الأيام المنقضية منذ ذلك التاريخ: 90 يوم
        // فأكثر = أحمر (حرجة)، من 31 إلى 89 يوم = أصفر (تحذير)، 30 يوم فأقل = أخضر (آمنة) -
        // بطلب صريح من المستخدمة (كانت مبنية على سنة النهاية فقط بلا حساب أيام سابقًا)
        public int PrepEndingRedCount { get; set; }
        public int PrepEndingYellowCount { get; set; }
        public int PrepEndingGreenCount { get; set; }
        /// <summary>نسبة الأحمر = PrepEndingRedCount ÷ إجمالي الثلاثة × 100</summary>
        public decimal PrepEndingRedPercent { get; set; }

        // "العقود التي شارفت على الانتهاء" - نفس فكرة PrepEnding لكن على عمود "تاريخ نهاية العقد"
        // بدلاً من "نهاية فترة التجهيز" - أحمر=ينتهي بسنة 2026 بالضبط، أخضر=بعد 2026 - بطلب
        // صريح من المستخدمة (كانت مبنية على نهاية فترة التجهيز سابقًا)
        public int ContractEndingRedCount { get; set; }
        public int ContractEndingGreenCount { get; set; }
        public int ContractEndingNullCount { get; set; }

        // كل إدارة عامة خلال 2026 - كم عقد تنتهي فترة تجهيزه بكل ربع (Q1-Q4)، من عمود
        // "نهاية فترة التجهيز" (عمود Column Chart، تسمية العدد فوق كل عمود)
        public List<DepartmentQuarterEndingRow> DepartmentQuarterEnding { get; set; } = new();

        public List<string> DepartmentOptions { get; set; } = new();
        public List<string> PortfolioOptions { get; set; } = new();
        public List<string> ContractTypeOptions { get; set; } = new();
        public List<string> ContractStatusOptions { get; set; } = new(); // من عمود "حالة العقد" البسيط
    }

    /// <summary>صف (إدارة عامة، ربع) واحد - عدد العقود اللي تنتهي فترة تجهيزها بذلك الربع من سنة 2026</summary>
    public class DepartmentQuarterEndingRow
    {
        public string DepartmentLabel { get; set; } = string.Empty;
        public int Quarter { get; set; } // 1-4
        public int Count { get; set; }
    }

    /// <summary>صف عقد واحد بدرج تفاصيل قوس "العقود التي شارفت على الانتهاء" (عند الضغط على لون)</summary>
    public class ContractEndingDetailRow
    {
        public string? ContractNumber { get; set; }
        public string? OpportunityName { get; set; }
        public string? InvestorName { get; set; }
        public DateTime? ContractEndDate { get; set; }
        public DateTime? SigningDate { get; set; }
    }

    public class ContractStatusGroupRow
    {
        public string GroupLabel { get; set; } = string.Empty; // اسم الإدارة العامة
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class ContractSubDeptStatusGroupRow
    {
        public string DepartmentLabel { get; set; } = string.Empty;
        public string PortfolioLabel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public interface IContractPeriodAnalyticsService
    {
        Task<ContractPeriodPageData> GetDataAsync(string? department, string? portfolio, string? contractType, string? contractStatus);

        /// <summary>قائمة العقود لدرج تفاصيل قوس "العقود التي شارفت على الانتهاء" - isRed=true يعني
        /// تاريخ نهاية العقد بسنة 2026 بالضبط، false يعني بعد 2026 - بنفس فلاتر GetDataAsync</summary>
        Task<List<ContractEndingDetailRow>> GetContractEndingListAsync(bool isRed, string? department, string? portfolio, string? contractType, string? contractStatus);

        /// <summary>قائمة العقود لدرج تفاصيل قوس "العقود التي تنتهي فترة سماحها خلال 2026" - zone
        /// "red"=تجاوزت 90 يوم، "yellow"=من 31 إلى 89 يوم، "green"=30 يوم فأقل (دائمًا ضمن نطاق
        /// نهاية فترة التجهيز بسنة 2026) - بنفس فلاتر GetDataAsync</summary>
        Task<List<ContractEndingDetailRow>> GetPrepEndingListAsync(string zone, string? department, string? portfolio, string? contractType, string? contractStatus);
    }

    public class ContractPeriodAnalyticsService : IContractPeriodAnalyticsService
    {
        // "اللي في 2026 أحمر واللي قبل 2026 أخضر" + "اللي بعد 2026 أخضر" (تأكيد صريح من
        // المستخدمة عبر رسالتين) - فقط سنة 2026 بالضبط تُعتبر عاجلة
        private const int RedYear = 2026;

        private readonly AppDbContext _db;

        public ContractPeriodAnalyticsService(AppDbContext db)
        {
            _db = db;
        }

        // فلترة موحّدة حسب الإدارة العامة/الفرعية/نوع العقد/حالة العقد - يُستخدم من GetDataAsync
        // وGetContractEndingListAsync حتى تبقى قائمة الدرج متطابقة تمامًا مع فلاتر الصفحة الحالية
        private static IEnumerable<ContractPeriodRecord> ApplyFilters(
            IEnumerable<ContractPeriodRecord> rows, string? department, string? portfolio, string? contractType, string? contractStatus)
        {
            if (!string.IsNullOrWhiteSpace(department)) rows = rows.Where(r => r.GeneralDepartment == department);
            if (!string.IsNullOrWhiteSpace(portfolio)) rows = rows.Where(r => r.Portfolio == portfolio);
            if (!string.IsNullOrWhiteSpace(contractType)) rows = rows.Where(r => r.ContractType == contractType);
            if (!string.IsNullOrWhiteSpace(contractStatus)) rows = rows.Where(r => r.ContractStatus == contractStatus);
            return rows;
        }

        public async Task<ContractPeriodPageData> GetDataAsync(string? department, string? portfolio, string? contractType, string? contractStatus)
        {
            var result = new ContractPeriodPageData();

            var allRecords = await _db.ContractPeriodRecords.AsNoTracking().ToListAsync();
            result.HasData = allRecords.Count > 0;
            if (!result.HasData) return result;

            result.DepartmentOptions = allRecords.Where(r => !string.IsNullOrWhiteSpace(r.GeneralDepartment)).Select(r => r.GeneralDepartment!).Distinct().OrderBy(x => x).ToList();
            result.PortfolioOptions = allRecords.Where(r => !string.IsNullOrWhiteSpace(r.Portfolio)).Select(r => r.Portfolio!).Distinct().OrderBy(x => x).ToList();
            result.ContractTypeOptions = allRecords.Where(r => !string.IsNullOrWhiteSpace(r.ContractType)).Select(r => r.ContractType!).Distinct().OrderBy(x => x).ToList();
            result.ContractStatusOptions = allRecords.Where(r => !string.IsNullOrWhiteSpace(r.ContractStatus)).Select(r => r.ContractStatus!).Distinct().OrderBy(x => x).ToList();

            var filteredRows = ApplyFilters(allRecords, department, portfolio, contractType, contractStatus).ToList();

            result.TotalCount = filteredRows.Count;
            result.InvestmentCount = filteredRows.Count(r => r.ContractType == "استثمار");
            result.DirectLeaseCount = filteredRows.Count(r => r.ContractType == "تأجير مباشر");

            // القيم الحقيقية الموجودة بالملف فقط، بلا اختراع/دمج تصنيفات
            result.StatusOptions = filteredRows.Where(r => !string.IsNullOrWhiteSpace(r.PrepPeriodStatus))
                .GroupBy(r => r.PrepPeriodStatus!)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Select(x => x.Status)
                .ToList();

            var byDept = filteredRows.Where(r => !string.IsNullOrWhiteSpace(r.GeneralDepartment))
                .GroupBy(r => r.GeneralDepartment!)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();
            result.DeptLabels = byDept.Select(x => x.Label).ToList();

            result.DeptStatusBreakdown = filteredRows.Where(r => !string.IsNullOrWhiteSpace(r.GeneralDepartment) && !string.IsNullOrWhiteSpace(r.PrepPeriodStatus))
                .GroupBy(r => new { Dept = r.GeneralDepartment!, Status = r.PrepPeriodStatus! })
                .Select(g => new ContractStatusGroupRow { GroupLabel = g.Key.Dept, Status = g.Key.Status, Count = g.Count() })
                .ToList();

            result.SubDeptStatusBreakdown = filteredRows.Where(r => !string.IsNullOrWhiteSpace(r.GeneralDepartment) && !string.IsNullOrWhiteSpace(r.Portfolio) && !string.IsNullOrWhiteSpace(r.PrepPeriodStatus))
                .GroupBy(r => new { Dept = r.GeneralDepartment!, Portfolio = r.Portfolio!, Status = r.PrepPeriodStatus! })
                .Select(g => new ContractSubDeptStatusGroupRow { DepartmentLabel = g.Key.Dept, PortfolioLabel = g.Key.Portfolio, Status = g.Key.Status, Count = g.Count() })
                .ToList();

            var today = DateTime.UtcNow.Date;
            var prepEnding2026 = filteredRows.Where(r => r.PrepPeriodEndDate.HasValue && r.PrepPeriodEndDate.Value.Year == RedYear).ToList();
            result.PrepEndingRedCount = prepEnding2026.Count(r => (today - r.PrepPeriodEndDate!.Value.Date).TotalDays >= 90);
            result.PrepEndingYellowCount = prepEnding2026.Count(r =>
            {
                var days = (today - r.PrepPeriodEndDate!.Value.Date).TotalDays;
                return days >= 31 && days < 90;
            });
            result.PrepEndingGreenCount = prepEnding2026.Count(r => (today - r.PrepPeriodEndDate!.Value.Date).TotalDays <= 30);
            result.PrepEndingRedPercent = prepEnding2026.Count > 0
                ? Math.Round((decimal)result.PrepEndingRedCount / prepEnding2026.Count * 100, 1) : 0;

            // "العقود التي شارفت على الانتهاء" - نفس المنطق لكن على "تاريخ نهاية العقد"
            var withContractEnd = filteredRows.Where(r => r.ContractEndDate.HasValue).ToList();
            result.ContractEndingRedCount = withContractEnd.Count(r => r.ContractEndDate!.Value.Year == RedYear);
            result.ContractEndingGreenCount = withContractEnd.Count(r => r.ContractEndDate!.Value.Year != RedYear);
            result.ContractEndingNullCount = filteredRows.Count - withContractEnd.Count;

            // كل إدارة عامة خلال 2026: كم عقد تنتهي فترة تجهيزه بكل ربع (Q1-Q4)، من "نهاية فترة
            // التجهيز" - بطلب صريح من المستخدمة (عمود Column Chart لكل إدارة عامة)
            result.DepartmentQuarterEnding = filteredRows
                .Where(r => !string.IsNullOrWhiteSpace(r.GeneralDepartment) && r.PrepPeriodEndDate.HasValue && r.PrepPeriodEndDate.Value.Year == RedYear)
                .GroupBy(r => new { Dept = r.GeneralDepartment!, Quarter = ((r.PrepPeriodEndDate!.Value.Month - 1) / 3) + 1 })
                .Select(g => new DepartmentQuarterEndingRow { DepartmentLabel = g.Key.Dept, Quarter = g.Key.Quarter, Count = g.Count() })
                .ToList();

            return result;
        }

        /// <summary>قائمة العقود لدرج تفاصيل قوس "العقود التي شارفت على الانتهاء" - نفس فلاتر
        /// GetDataAsync، مقيّدة بلون العنصر المضغوط (أحمر=2026 بالضبط، أخضر=بعد 2026)</summary>
        public async Task<List<ContractEndingDetailRow>> GetContractEndingListAsync(bool isRed, string? department, string? portfolio, string? contractType, string? contractStatus)
        {
            var allRecords = await _db.ContractPeriodRecords.AsNoTracking().ToListAsync();
            var filteredRows = ApplyFilters(allRecords, department, portfolio, contractType, contractStatus);

            var result = filteredRows
                .Where(r => r.ContractEndDate.HasValue && (isRed ? r.ContractEndDate.Value.Year == RedYear : r.ContractEndDate.Value.Year != RedYear))
                .OrderBy(r => r.ContractEndDate)
                .Select(r => new ContractEndingDetailRow
                {
                    ContractNumber = r.ContractNumber,
                    OpportunityName = r.OpportunityName,
                    ContractEndDate = r.ContractEndDate,
                    SigningDate = r.SigningDate
                })
                .ToList();
            await FillInvestorNamesAsync(result);
            return result;
        }

        public async Task<List<ContractEndingDetailRow>> GetPrepEndingListAsync(string zone, string? department, string? portfolio, string? contractType, string? contractStatus)
        {
            var allRecords = await _db.ContractPeriodRecords.AsNoTracking().ToListAsync();
            var filteredRows = ApplyFilters(allRecords, department, portfolio, contractType, contractStatus);
            var today = DateTime.UtcNow.Date;

            var result = filteredRows
                .Where(r =>
                {
                    if (!r.PrepPeriodEndDate.HasValue || r.PrepPeriodEndDate.Value.Year != RedYear) return false;
                    var days = (today - r.PrepPeriodEndDate.Value.Date).TotalDays;
                    return zone == "red" ? days >= 90 : zone == "yellow" ? (days >= 31 && days < 90) : days <= 30;
                })
                .OrderBy(r => r.PrepPeriodEndDate)
                .Select(r => new ContractEndingDetailRow
                {
                    ContractNumber = r.ContractNumber,
                    OpportunityName = r.OpportunityName,
                    ContractEndDate = r.PrepPeriodEndDate,
                    SigningDate = r.SigningDate
                })
                .ToList();
            await FillInvestorNamesAsync(result);
            return result;
        }

        private async Task FillInvestorNamesAsync(List<ContractEndingDetailRow> rows)
        {
            var numbers = rows.Where(r => !string.IsNullOrWhiteSpace(r.ContractNumber)).Select(r => r.ContractNumber!).Distinct().ToList();
            if (numbers.Count == 0) return;
            // الربط مع DebtInvoiceRecords يتم عبر ContractIdRaw ("CONTRACT_ID") لا ContractNumber
            // ("رقم العقد") - عمود "رقم العقد" بملف المديونيات أرقام داخلية صغيرة لا تطابق صيغة
            // "CN-XXXXX" المستخدمة هنا (راجع تعليق DebtsAnalyticsService.FindByContractNumberAsync)
            var investors = await _db.DebtInvoiceRecords.AsNoTracking()
                .Where(r => r.ContractIdRaw != null && numbers.Contains(r.ContractIdRaw) && !string.IsNullOrWhiteSpace(r.InvestorName))
                .GroupBy(r => r.ContractIdRaw!)
                .ToDictionaryAsync(g => g.Key, g => g.Select(r => r.InvestorName!).First());
            foreach (var row in rows)
                if (row.ContractNumber != null && investors.TryGetValue(row.ContractNumber, out var investorName)) row.InvestorName = investorName;
        }
    }
}
