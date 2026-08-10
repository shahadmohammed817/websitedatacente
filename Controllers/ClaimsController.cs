using ExcelDashboard.Helpers;
using ExcelDashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExcelDashboard.Controllers
{
    /// <summary>
    /// صفحتا "المطالبات والمديونيات" و"متابعة الأداء": مصدرهما الوحيد ملف Excel يُرفع من زر
    /// مخصص أعلى الصفحتين. لا يوجد تسجيل دخول حالياً - متاحتان مباشرة كبقية صفحات النظام.
    /// </summary>
    public class ClaimsController : Controller
    {
        private readonly IClaimsDebtsImportService _importService;
        private readonly IClaimsDebtsAnalyticsService _analytics;
        private readonly IContractImportService _contractImport;
        private readonly IAnalyticsService _analyticsService;

        public ClaimsController(
            IClaimsDebtsImportService importService,
            IClaimsDebtsAnalyticsService analytics,
            IContractImportService contractImport,
            IAnalyticsService analyticsService)
        {
            _importService = importService;
            _analytics = analytics;
            _contractImport = contractImport;
            _analyticsService = analyticsService;
        }

        /// <summary>صفحة "المطالبات والمديونيات" (نظرة عامة)</summary>
        [HttpGet("Claims/Overview")]
        public IActionResult Overview()
        {
            return View();
        }

        /// <summary>صفحة "متابعة الأداء"</summary>
        [HttpGet("Claims/Followup")]
        public IActionResult Followup()
        {
            return View();
        }

        /// <summary>صفحة رفع ملف Excel الخاص بالمطالبات والمديونيات</summary>
        [HttpGet("Claims/Upload")]
        public IActionResult Upload()
        {
            return View();
        }

        /// <summary>صفحة "العقود التي تنتهي فترة سماحها خلال 3 أشهر" - فارغة حاليًا بانتظار ربطها بمصدر بيانات لاحقًا</summary>
        [HttpGet("Claims/GracePeriodExpiring")]
        public IActionResult GracePeriodExpiring()
        {
            return View();
        }

        [HttpPost("Claims/UploadExcel")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> UploadExcel(IFormFile excelFile)
        {
            if (AppMode.ReadOnly)
                return Json(new { success = false, message = "الموقع بوضع العرض فقط حاليًا - الرفع غير متاح." });

            if (excelFile == null || excelFile.Length == 0)
                return Json(new { success = false, message = "يرجى اختيار ملف صالح." });

            if (!excelFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "يرجى رفع ملف بصيغة .xlsx فقط." });

            using var stream = excelFile.OpenReadStream();
            var result = await _importService.ImportAsync(stream);

            return Json(new
            {
                success = result.Success,
                message = result.Message,
                claimsImported = result.ClaimsImported,
                debtsImported = result.DebtsImported
            });
        }

        [HttpGet("Claims/Overview/Data")]
        public async Task<IActionResult> OverviewData()
        {
            var data = await _analytics.GetOverviewDataAsync();
            return Json(data);
        }

        [HttpGet("Claims/Followup/Data")]
        public async Task<IActionResult> FollowupData()
        {
            var data = await _analytics.GetFollowupDataAsync();
            return Json(data);
        }

        /// <summary>رفع ملف عقود "الطرح" (زر مخصص أعلى صفحة "الطرح"). يستبدل كل العقود القديمة بالكامل.</summary>
        [HttpPost("Claims/UploadContracts")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> UploadContracts(IFormFile contractsFile)
        {
            if (AppMode.ReadOnly)
                return Json(new { success = false, message = "الموقع بوضع العرض فقط حاليًا - الرفع غير متاح." });

            if (contractsFile == null || contractsFile.Length == 0)
                return Json(new { success = false, message = "يرجى اختيار ملف صالح." });

            if (!contractsFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "يرجى رفع ملف بصيغة .xlsx فقط." });

            using var stream = contractsFile.OpenReadStream();
            var result = await _contractImport.ImportAsync(stream);

            return Json(new
            {
                success = result.Success,
                message = result.Message,
                rowsImported = result.RowsImported
            });
        }

        /// <summary>
        /// نقطة نهاية JSON تُرجع بيانات صفحة "الطرح" (كروت أنواع العقود، توزيع الإدارات، نسبة
        /// الإبرام، البنود الفرعية حسب الإدارة، والاتجاه الزمني) - قابلة للتصفية بالإدارة
        /// العامة والنشاط وحالة العقد ورقم العقد
        /// </summary>
        [HttpGet("Claims/Followup/TarhData")]
        public async Task<IActionResult> TarhData(string? department, string? status, string? contractNumber, int? year, int? quarter)
        {
            var data = await _analyticsService.GetTarhDataAsync(department, status, contractNumber, year, quarter);
            return Json(data);
        }
    }
}
