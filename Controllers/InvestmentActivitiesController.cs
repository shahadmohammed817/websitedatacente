using ExcelDashboard.Helpers;
using ExcelDashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExcelDashboard.Controllers
{
    /// <summary>صفحة "الأنشطة الاستثمارية" - مصدرها ملف "الأنشطة الاستثمارية" يُرفع من زر مخصص أعلى الصفحة.</summary>
    public class InvestmentActivitiesController : Controller
    {
        private readonly IInvestmentActivityAnalyticsService _analytics;
        private readonly IInvestmentActivityImportService _importService;

        public InvestmentActivitiesController(
            IInvestmentActivityAnalyticsService analytics,
            IInvestmentActivityImportService importService)
        {
            _analytics = analytics;
            _importService = importService;
        }

        [HttpGet("InvestmentActivities")]
        public IActionResult Index() => View();

        [HttpGet("InvestmentActivities/Data")]
        public async Task<IActionResult> Data(int? year, string? activity, string? region)
        {
            var data = await _analytics.GetDataAsync(year, activity, region);
            return Json(data);
        }

        [HttpPost("InvestmentActivities/Upload")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> Upload(IFormFile activitiesFile)
        {
            if (AppMode.ReadOnly)
                return Json(new { success = false, message = "الموقع بوضع العرض فقط حاليًا - الرفع غير متاح." });

            if (activitiesFile == null || activitiesFile.Length == 0)
                return Json(new { success = false, message = "يرجى اختيار ملف صالح." });

            if (!activitiesFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "يرجى رفع ملف بصيغة .xlsx فقط." });

            using var stream = activitiesFile.OpenReadStream();
            var result = await _importService.ImportAsync(stream);

            return Json(new { success = result.Success, message = result.Message, rowsImported = result.RowsImported });
        }
    }
}
