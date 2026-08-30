using ExcelDashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExcelDashboard.Controllers
{
    /// <summary>صفحة "الملخص التنفيذي": شريط فلاتر (بند رئيسي/فرعي + مصدر مستهدف + سنة - يشتغل
    /// على قسم "تحقيق المستهدف" فقط) + 5 صفوف ثابتة (فرص/عقود الطرح، تحقيق المستهدف، المديونيات
    /// لجميع البنود، المديونيات والمطالبات، الإدارات العامة) كل صف يُحمَّل ببيانات مستقلة.</summary>
    public class ExecutiveSummaryController : AppControllerBase
    {
        private readonly IExecutiveSummaryService _summary;

        public ExecutiveSummaryController(IExecutiveSummaryService summary)
        {
            _summary = summary;
        }

        [HttpGet("ExecutiveSummary")]
        public IActionResult Index() => View();

        [HttpGet("ExecutiveSummary/RevenuePerf")]
        public async Task<IActionResult> RevenuePerf() => Json(await _summary.GetRevenuePerfAsync());

        [HttpGet("ExecutiveSummary/DeptData")]
        public async Task<IActionResult> DeptData() => Json(await _summary.GetDeptDataAsync(ScopedDepartment(null)));

        [HttpGet("ExecutiveSummary/DebtData")]
        public async Task<IActionResult> DebtData() => Json(await _summary.GetDebtDataAsync());

        [HttpGet("ExecutiveSummary/ClaimsData")]
        public async Task<IActionResult> ClaimsData() => Json(await _summary.GetClaimsDataAsync(ScopedDepartment(null)));

        [HttpGet("ExecutiveSummary/TarhOpportunitiesSummary")]
        public async Task<IActionResult> TarhOpportunitiesSummary() => Json(await _summary.GetTarhOpportunitiesSummaryAsync());

        [HttpGet("ExecutiveSummary/TargetAchievement")]
        public async Task<IActionResult> TargetAchievement(string? scope, string? mainItem, string? subItem, string? targetSource, int? year)
            => Json(await _summary.GetTargetAchievementAsync(scope, mainItem, subItem, targetSource, year));

        [HttpGet("ExecutiveSummary/TargetFilterOptions")]
        public async Task<IActionResult> TargetFilterOptions(string? scope) => Json(await _summary.GetTargetFilterOptionsAsync(scope));
    }
}
