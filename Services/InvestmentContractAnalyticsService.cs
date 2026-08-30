using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    public class InvestmentContractsPageData
    {
        public bool HasData { get; set; }

        // ==================== كروت KPI ====================
        public int TotalCount { get; set; }
        public decimal TotalAnnualValue { get; set; }
        public List<InvTypeSummaryRow> TypeSummary { get; set; } = new();

        // ==================== رسم 1: الأجرة السنوية حسب حالة العقد - إدارة/محفظة ====================
        public List<string> StatusLabels { get; set; } = new(); // لفلتر "حالة العقد" فوق الرسمين
        public List<InvDeptStatusRow> DeptStatusBreakdown { get; set; } = new();
        public List<InvPortfolioStatusRow> PortfolioStatusBreakdown { get; set; } = new();

        // ==================== صفحة البلدية - رسم 1: الأجرة السنوية حسب حالة العقد (بلا تجميع إدارة/محفظة) ====================
        public List<InvStatusValueRow> StatusValueTotals { get; set; } = new();

        // ==================== صفحة البلدية - دونات توزيع الدفعات حسب الحالة ====================
        public List<InvDonutRow> PaymentStatusDonut { get; set; } = new();

        // ==================== صفحة البلدية - متبقي غير مستحق مقابل تم إرساله للتنفيذ القضائي - حسب البلدية ====================
        public List<InvBaladiyahRemainingRow> BaladiyahRemainingVsExecution { get; set; } = new();

        // ==================== رسم 2: مستحق مدفوع/غير مدفوع/متبقي غير مستحق (Stacked) - إدارة/محفظة ====================
        public List<InvDeptPaymentRow> DeptPaymentBreakdown { get; set; } = new();
        public List<InvPortfolioPaymentRow> PortfolioPaymentBreakdown { get; set; } = new();

        // ==================== رسم 3: Funnel (إجمالي / مستحق / مدفوع / غير مدفوع+متبقي+متعثر) ====================
        public List<InvFunnelStage> FunnelStages { get; set; } = new();

        // ==================== رسم 4: متعثر / تم الإرسال لتنفيذ - إدارة/محفظة ====================
        public List<InvDeptStalledRow> DeptStalledVsExecution { get; set; } = new();
        public List<InvPortfolioStalledRow> PortfolioStalledVsExecution { get; set; } = new();

        // ==================== الفلاتر ====================
        public List<int> YearOptions { get; set; } = new();
        public List<string> ContractValidityStatusOptions { get; set; } = new();
        public List<string> BaladiyahOptions { get; set; } = new();
        public List<string> GeneralDepartmentOptions { get; set; } = new();
        public List<string> PortfolioOptions { get; set; } = new();
        public List<string> InvestorNameOptions { get; set; } = new();
    }

    public class InvTypeSummaryRow
    {
        public string TypeLabel { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class InvDeptStatusRow
    {
        public string DepartmentLabel { get; set; } = string.Empty;
        public Dictionary<string, decimal> StatusValues { get; set; } = new();
    }

    public class InvPortfolioStatusRow
    {
        public string DepartmentLabel { get; set; } = string.Empty;
        public string PortfolioLabel { get; set; } = string.Empty;
        public Dictionary<string, decimal> StatusValues { get; set; } = new();
    }

    public class InvDeptPaymentRow
    {
        public string DepartmentLabel { get; set; } = string.Empty;
        public decimal DuePaidValue { get; set; }
        public decimal DueUnpaidValue { get; set; }
        public decimal RemainingNotDueValue { get; set; }
        public decimal RatePercent { get; set; }
    }

    public class InvPortfolioPaymentRow
    {
        public string DepartmentLabel { get; set; } = string.Empty;
        public string PortfolioLabel { get; set; } = string.Empty;
        public decimal DuePaidValue { get; set; }
        public decimal DueUnpaidValue { get; set; }
        public decimal RemainingNotDueValue { get; set; }
        public decimal RatePercent { get; set; }
    }

    public class InvFunnelStage
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Value { get; set; }
    }

    public class InvStatusValueRow
    {
        public string StatusLabel { get; set; } = string.Empty;
        public decimal TotalValue { get; set; }
    }

    public class InvDonutRow
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    public class InvBaladiyahRemainingRow
    {
        public string BaladiyahLabel { get; set; } = string.Empty;
        public decimal RemainingNotDueValue { get; set; }
        public decimal SentToExecutionValue { get; set; }
    }

    public class InvDeptStalledRow
    {
        public string DepartmentLabel { get; set; } = string.Empty;
        public int StalledCount { get; set; }
        public int SentToExecutionCount { get; set; }
    }

    public class InvPortfolioStalledRow
    {
        public string DepartmentLabel { get; set; } = string.Empty;
        public string PortfolioLabel { get; set; } = string.Empty;
        public int StalledCount { get; set; }
        public int SentToExecutionCount { get; set; }
    }

    public interface IInvestmentContractAnalyticsService
    {
        Task<InvestmentContractsPageData> GetDataAsync(string scope, int? year, string? validityStatus, string? baladiyah, string? hasIncrease, string? generalDepartment, string? portfolio, string? investorName);
    }

    public class InvestmentContractAnalyticsService : IInvestmentContractAnalyticsService
    {
        private readonly AppDbContext _db;

        public InvestmentContractAnalyticsService(AppDbContext db)
        {
            _db = db;
        }

        // عمود "البلدية" يحوي قيمة "أمانة منطقة المدينة المنورة" للعقود التابعة للأمانة مباشرة،
        // وأسماء البلديات الحقيقية لبقية العقود - نطابق كلمة "أمانة" بتطبيع يتسامح مع اختلاف الهمزة
        private static bool IsAmanahRow(string? baladiyah) =>
            baladiyah != null && DataImportService.Normalize(baladiyah).Contains(DataImportService.Normalize("أمانة"));

        public async Task<InvestmentContractsPageData> GetDataAsync(string scope, int? year, string? validityStatus, string? baladiyah, string? hasIncrease, string? generalDepartment, string? portfolio, string? investorName)
        {
            var result = new InvestmentContractsPageData();

            var allRecords = await _db.InvestmentContractRecords.AsNoTracking().ToListAsync();
            // مصدر أسماء المستثمرين للاستدلال: DebtInvoiceRecords، لكن الربط عبر عمود
            // ContractIdRaw ("CONTRACT_ID") لا ContractNumber ("رقم العقد") - عمود "رقم العقد"
            // بملف المديونيات أرقام داخلية صغيرة لا تطابق صيغة "CN-XXXXX" المستخدمة هنا إطلاقًا،
            // بينما ContractIdRaw هو المطابِق فعليًا (نفس ملاحظة DebtsAnalyticsService.FindByContractNumberAsync)
            var investorsByContract = await _db.DebtInvoiceRecords.AsNoTracking()
                .Where(r => !string.IsNullOrWhiteSpace(r.ContractIdRaw) && !string.IsNullOrWhiteSpace(r.InvestorName))
                .GroupBy(r => r.ContractIdRaw!)
                .ToDictionaryAsync(g => g.Key, g => g.Select(r => r.InvestorName!).First());
            foreach (var record in allRecords.Where(r => string.IsNullOrWhiteSpace(r.InvestorName) && !string.IsNullOrWhiteSpace(r.ContractNumber)))
                if (investorsByContract.TryGetValue(record.ContractNumber!, out var inferredInvestorName)) record.InvestorName = inferredInvestorName;
            bool isBaladiyahScope = string.Equals(scope, "Baladiyah", StringComparison.OrdinalIgnoreCase);
            var scopedRecords = allRecords.Where(r => IsAmanahRow(r.Baladiyah) != isBaladiyahScope).ToList();

            result.HasData = scopedRecords.Count > 0;
            if (!result.HasData) return result;

            result.YearOptions = scopedRecords
                .Where(r => r.ContractStartDate.HasValue)
                .Select(r => r.ContractStartDate!.Value.Year).Distinct().OrderByDescending(y => y).ToList();
            result.ContractValidityStatusOptions = scopedRecords
                .Where(r => !string.IsNullOrWhiteSpace(r.ContractValidityStatus))
                .Select(r => r.ContractValidityStatus!).Distinct().OrderBy(x => x).ToList();
            result.BaladiyahOptions = scopedRecords
                .Where(r => !string.IsNullOrWhiteSpace(r.Baladiyah))
                .Select(r => r.Baladiyah!).Distinct().OrderBy(x => x).ToList();
            result.GeneralDepartmentOptions = scopedRecords
                .Where(r => !string.IsNullOrWhiteSpace(r.GeneralDepartment))
                .Select(r => r.GeneralDepartment!).Distinct().OrderBy(x => x).ToList();
            result.PortfolioOptions = scopedRecords
                .Where(r => !string.IsNullOrWhiteSpace(r.Portfolio) &&
                            (string.IsNullOrWhiteSpace(generalDepartment) || r.GeneralDepartment == generalDepartment))
                .Select(r => r.Portfolio!).Distinct().OrderBy(x => x).ToList();
            result.InvestorNameOptions = scopedRecords
                .Where(r => !string.IsNullOrWhiteSpace(r.InvestorName))
                .Select(r => r.InvestorName!).Distinct().OrderBy(x => x).ToList();

            IEnumerable<InvestmentContractRecord> rows = scopedRecords;
            if (year.HasValue) rows = rows.Where(r => r.ContractStartDate.HasValue && r.ContractStartDate.Value.Year == year.Value);
            if (!string.IsNullOrWhiteSpace(validityStatus)) rows = rows.Where(r => r.ContractValidityStatus == validityStatus);
            if (!string.IsNullOrWhiteSpace(baladiyah)) rows = rows.Where(r => r.Baladiyah == baladiyah);
            if (!string.IsNullOrWhiteSpace(hasIncrease)) rows = rows.Where(r => r.HasAnnualIncrease == hasIncrease);
            if (!string.IsNullOrWhiteSpace(generalDepartment)) rows = rows.Where(r => r.GeneralDepartment == generalDepartment);
            if (!string.IsNullOrWhiteSpace(portfolio)) rows = rows.Where(r => r.Portfolio == portfolio);
            if (!string.IsNullOrWhiteSpace(investorName)) rows = rows.Where(r => r.InvestorName == investorName);
            var filteredRows = rows.ToList();

            // ==================== كروت KPI ====================
            result.TotalCount = filteredRows.Count;
            result.TotalAnnualValue = filteredRows.Sum(r => r.AnnualValue ?? 0);

            result.TypeSummary = filteredRows
                .Where(r => !string.IsNullOrWhiteSpace(r.ContractType))
                .GroupBy(r => r.ContractType!)
                .Select(g => new InvTypeSummaryRow { TypeLabel = g.Key, Count = g.Count(), TotalValue = g.Sum(r => r.AnnualValue ?? 0) })
                .OrderByDescending(x => x.TotalValue)
                .ToList();

            // ==================== رسم 1: الأجرة السنوية حسب حالة العقد - إدارة/محفظة ====================
            var statusRows = filteredRows.Where(r => !string.IsNullOrWhiteSpace(r.ContractStatus)).ToList();
            result.StatusLabels = statusRows.Select(r => r.ContractStatus!).Distinct()
                .OrderByDescending(s => statusRows.Count(r => r.ContractStatus == s))
                .ToList();

            result.DeptStatusBreakdown = statusRows
                .Where(r => !string.IsNullOrWhiteSpace(r.GeneralDepartment))
                .GroupBy(r => r.GeneralDepartment!)
                .Select(g => new InvDeptStatusRow
                {
                    DepartmentLabel = g.Key,
                    StatusValues = g.GroupBy(r => r.ContractStatus!)
                        .ToDictionary(sg => sg.Key, sg => sg.Sum(r => r.AnnualValue ?? 0))
                })
                .OrderByDescending(x => x.StatusValues.Values.Sum())
                .ToList();

            result.PortfolioStatusBreakdown = statusRows
                .Where(r => !string.IsNullOrWhiteSpace(r.GeneralDepartment) && !string.IsNullOrWhiteSpace(r.Portfolio))
                .GroupBy(r => new { Dept = r.GeneralDepartment!, Portfolio = r.Portfolio! })
                .Select(g => new InvPortfolioStatusRow
                {
                    DepartmentLabel = g.Key.Dept,
                    PortfolioLabel = g.Key.Portfolio,
                    StatusValues = g.GroupBy(r => r.ContractStatus!)
                        .ToDictionary(sg => sg.Key, sg => sg.Sum(r => r.AnnualValue ?? 0))
                })
                .OrderByDescending(x => x.StatusValues.Values.Sum())
                .ToList();

            // ==================== صفحة البلدية - رسم 1: الأجرة السنوية حسب حالة العقد (إجمالي بلا تجميع إدارة) ====================
            result.StatusValueTotals = statusRows
                .GroupBy(r => r.ContractStatus!)
                .Select(g => new InvStatusValueRow { StatusLabel = g.Key, TotalValue = g.Sum(r => r.AnnualValue ?? 0) })
                .OrderByDescending(x => x.TotalValue)
                .ToList();

            // ==================== رسم 2: مستحق مدفوع/غير مدفوع/متبقي غير مستحق (Stacked) - إدارة/محفظة ====================
            // نسبة التحصيل = الدفعات المستحقة المدفوعة ÷ الدفعات المستحقة (مدفوعة+غير مدفوعة) × 100
            result.DeptPaymentBreakdown = filteredRows
                .Where(r => !string.IsNullOrWhiteSpace(r.GeneralDepartment))
                .GroupBy(r => r.GeneralDepartment!)
                .Select(g =>
                {
                    var paid = g.Sum(r => r.DuePaidValue ?? 0);
                    var due = g.Sum(r => r.DuePaymentsValue ?? 0);
                    return new InvDeptPaymentRow
                    {
                        DepartmentLabel = g.Key,
                        DuePaidValue = paid,
                        DueUnpaidValue = g.Sum(r => r.DueUnpaidValue ?? 0),
                        RemainingNotDueValue = g.Sum(r => r.RemainingNotDueValue ?? 0),
                        RatePercent = due > 0 ? Math.Round(paid / due * 100, 1) : 0
                    };
                })
                .OrderByDescending(x => x.DuePaidValue + x.DueUnpaidValue + x.RemainingNotDueValue)
                .ToList();

            result.PortfolioPaymentBreakdown = filteredRows
                .Where(r => !string.IsNullOrWhiteSpace(r.GeneralDepartment) && !string.IsNullOrWhiteSpace(r.Portfolio))
                .GroupBy(r => new { Dept = r.GeneralDepartment!, Portfolio = r.Portfolio! })
                .Select(g =>
                {
                    var paid = g.Sum(r => r.DuePaidValue ?? 0);
                    var due = g.Sum(r => r.DuePaymentsValue ?? 0);
                    return new InvPortfolioPaymentRow
                    {
                        DepartmentLabel = g.Key.Dept,
                        PortfolioLabel = g.Key.Portfolio,
                        DuePaidValue = paid,
                        DueUnpaidValue = g.Sum(r => r.DueUnpaidValue ?? 0),
                        RemainingNotDueValue = g.Sum(r => r.RemainingNotDueValue ?? 0),
                        RatePercent = due > 0 ? Math.Round(paid / due * 100, 1) : 0
                    };
                })
                .OrderByDescending(x => x.DuePaidValue + x.DueUnpaidValue + x.RemainingNotDueValue)
                .ToList();

            // ==================== رسم 3: Funnel ====================
            // "غير المدفوع" و"المتعثر" مرحلتان منفصلتان الآن (بدل مرحلة مجمّعة) - بدون
            // "المتبقي غير المستحق" (يشمل كل الدفعات المستقبلية بعقود طويلة المدى فيكسر
            // تنازلية الفانل - استُبعد بطلب صريح من المستخدمة سابقًا)
            var totalPayments = filteredRows.Sum(r => r.TotalPaymentsCount ?? 0);
            var duePayments = filteredRows.Sum(r => r.DuePaymentsCount ?? 0);
            var duePaid = filteredRows.Sum(r => r.DuePaidCount ?? 0);
            var dueUnpaid = filteredRows.Sum(r => r.DueUnpaidCount ?? 0);
            var stalled = filteredRows.Sum(r => r.StalledCount ?? 0);
            // نُسخة القيمة (لصفحة البلدية): المرحلة الأولى = إجمالي الأجرة السنوية للعقود، وبقية
            // المراحل من أعمدة القيمة المقابلة مباشرة (لا يوجد عمود "قيمة" لعدد الدفعات الكلي)
            var duePaymentsValue = filteredRows.Sum(r => r.DuePaymentsValue ?? 0);
            var duePaidValue = filteredRows.Sum(r => r.DuePaidValue ?? 0);
            var dueUnpaidValue = filteredRows.Sum(r => r.DueUnpaidValue ?? 0);
            var stalledValue = filteredRows.Sum(r => r.StalledValue ?? 0);
            result.FunnelStages = new List<InvFunnelStage>
            {
                new() { Label = "إجمالي الدفعات", Count = totalPayments, Value = result.TotalAnnualValue },
                new() { Label = "الدفعات المستحقة", Count = duePayments, Value = duePaymentsValue },
                new() { Label = "الدفعات المدفوعة", Count = duePaid, Value = duePaidValue },
                new() { Label = "غير المدفوع", Count = dueUnpaid, Value = dueUnpaidValue },
                new() { Label = "المتعثر", Count = stalled, Value = stalledValue }
            };

            // ==================== صفحة البلدية - دونات توزيع الدفعات حسب الحالة ====================
            result.PaymentStatusDonut = new List<InvDonutRow>
            {
                new() { Label = "مدفوع", Value = duePaidValue },
                new() { Label = "غير مدفوع", Value = dueUnpaidValue },
                new() { Label = "متبقي غير مستحق", Value = filteredRows.Sum(r => r.RemainingNotDueValue ?? 0) },
                new() { Label = "متعثر", Value = stalledValue },
                new() { Label = "تم الإرسال للتنفيذ القضائي", Value = filteredRows.Sum(r => r.SentToExecutionValue ?? 0) }
            };

            // ==================== صفحة البلدية - متبقي غير مستحق مقابل تم إرساله للتنفيذ القضائي - حسب البلدية ====================
            result.BaladiyahRemainingVsExecution = filteredRows
                .Where(r => !string.IsNullOrWhiteSpace(r.Baladiyah))
                .GroupBy(r => r.Baladiyah!)
                .Select(g => new InvBaladiyahRemainingRow
                {
                    BaladiyahLabel = g.Key,
                    RemainingNotDueValue = g.Sum(r => r.RemainingNotDueValue ?? 0),
                    SentToExecutionValue = g.Sum(r => r.SentToExecutionValue ?? 0)
                })
                .OrderByDescending(x => x.RemainingNotDueValue + x.SentToExecutionValue)
                .ToList();

            // ==================== رسم 4: متعثر / تم الإرسال لتنفيذ - إدارة/محفظة ====================
            result.DeptStalledVsExecution = filteredRows
                .Where(r => !string.IsNullOrWhiteSpace(r.GeneralDepartment))
                .GroupBy(r => r.GeneralDepartment!)
                .Select(g => new InvDeptStalledRow
                {
                    DepartmentLabel = g.Key,
                    StalledCount = g.Sum(r => r.StalledCount ?? 0),
                    SentToExecutionCount = g.Sum(r => r.SentToExecutionCount ?? 0)
                })
                .OrderByDescending(x => x.StalledCount + x.SentToExecutionCount)
                .ToList();

            result.PortfolioStalledVsExecution = filteredRows
                .Where(r => !string.IsNullOrWhiteSpace(r.GeneralDepartment) && !string.IsNullOrWhiteSpace(r.Portfolio))
                .GroupBy(r => new { Dept = r.GeneralDepartment!, Portfolio = r.Portfolio! })
                .Select(g => new InvPortfolioStalledRow
                {
                    DepartmentLabel = g.Key.Dept,
                    PortfolioLabel = g.Key.Portfolio,
                    StalledCount = g.Sum(r => r.StalledCount ?? 0),
                    SentToExecutionCount = g.Sum(r => r.SentToExecutionCount ?? 0)
                })
                .OrderByDescending(x => x.StalledCount + x.SentToExecutionCount)
                .ToList();

            return result;
        }
    }
}
