using ExcelDashboard.Helpers;
using ExcelDashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExcelDashboard.Controllers
{
    /// <summary>صفحة "جميع العقود الاستثمارية" - مصدرها ملف "تقرير عقود" يُرفع من زر مخصص أعلى الصفحة.</summary>
    public class InvestmentContractsController : AppControllerBase
    {
        private readonly IInvestmentContractAnalyticsService _analytics;
        private readonly IInvestmentContractImportService _importService;

        public InvestmentContractsController(
            IInvestmentContractAnalyticsService analytics,
            IInvestmentContractImportService importService)
        {
            _analytics = analytics;
            _importService = importService;
        }

        [HttpGet("InvestmentContracts/{scope=Amanah}")]
        public IActionResult Index(string scope)
        {
            if (!string.Equals(scope, "Amanah", StringComparison.OrdinalIgnoreCase) && !string.Equals(scope, "Baladiyah", StringComparison.OrdinalIgnoreCase))
                return NotFound();
            ViewBag.Scope = scope;
            return View();
        }

        [HttpGet("InvestmentContracts/{scope}/Data")]
        public async Task<IActionResult> Data(string scope, int? year, string? validityStatus, string? baladiyah, string? hasIncrease, string? generalDepartment, string? portfolio, string? investorName)
        {
            generalDepartment = ScopedDepartment(generalDepartment);
            var data = await _analytics.GetDataAsync(scope, year, validityStatus, baladiyah, hasIncrease, generalDepartment, portfolio, investorName);
            if (IsDepartmentRestricted) data.GeneralDepartmentOptions = new List<string> { generalDepartment! };
            return Json(data);
        }

        [HttpPost("InvestmentContracts/Upload")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> Upload(IFormFile contractsFile)
        {
            if (AppMode.ReadOnly)
                return Json(new { success = false, message = "الموقع بوضع العرض فقط حاليًا - الرفع غير متاح." });

            if (contractsFile == null || contractsFile.Length == 0)
                return Json(new { success = false, message = "يرجى اختيار ملف صالح." });

            if (!contractsFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "يرجى رفع ملف بصيغة .xlsx فقط." });

            using var stream = contractsFile.OpenReadStream();
            var result = await _importService.ImportAsync(stream);

            return Json(new { success = result.Success, message = result.Message, rowsImported = result.RowsImported });
        }
    }
}
