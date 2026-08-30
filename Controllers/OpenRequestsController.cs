using ExcelDashboard.Helpers;
using ExcelDashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExcelDashboard.Controllers
{
    /// <summary>صفحة "الطلبات المفتوحة" - مصدرها ملف "الطلبات المفتوحة" يُرفع من زر مخصص أعلى الصفحة.</summary>
    public class OpenRequestsController : Controller
    {
        private readonly IOpenRequestAnalyticsService _analytics;
        private readonly IOpenRequestImportService _importService;

        public OpenRequestsController(IOpenRequestAnalyticsService analytics, IOpenRequestImportService importService)
        {
            _analytics = analytics;
            _importService = importService;
        }

        [HttpGet("OpenRequests")]
        public IActionResult Index() => View();

        [HttpGet("OpenRequests/Data")]
        public async Task<IActionResult> Data([FromQuery] List<string>? stages, string? search, [FromQuery] List<int>? years)
        {
            var data = await _analytics.GetDataAsync(stages, search, years);
            return Json(data);
        }

        [HttpPost("OpenRequests/Upload")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> Upload(IFormFile openRequestsFile)
        {
            if (AppMode.ReadOnly)
                return Json(new { success = false, message = "الموقع بوضع العرض فقط حاليًا - الرفع غير متاح." });

            if (openRequestsFile == null || openRequestsFile.Length == 0)
                return Json(new { success = false, message = "يرجى اختيار ملف صالح." });

            if (!openRequestsFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "يرجى رفع ملف بصيغة .xlsx فقط." });

            using var stream = openRequestsFile.OpenReadStream();
            var result = await _importService.ImportAsync(stream);

            return Json(new { success = result.Success, message = result.Message, rowsImported = result.RowsImported });
        }
    }
}
