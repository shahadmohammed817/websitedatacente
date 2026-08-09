using ExcelDashboard.Data;
using ExcelDashboard.Helpers;
using ExcelDashboard.Models;
using ExcelDashboard.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Controllers
{
    /// <summary>
    /// لوحة رفع ملفات Excel - لا يوجد تسجيل دخول حالياً، فهذه الصفحة متاحة مباشرة للجميع
    /// </summary>
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IExcelDashboardService _excelService;
        private readonly IDataImportService _importService;

        // لا يوجد تسجيل دخول، فيُستخدم مستخدم النظام الافتراضي كمرجع لعمليات الاستيراد
        private const int CurrentUserId = 1;

        public AdminController(AppDbContext db, IExcelDashboardService excelService, IDataImportService importService)
        {
            _db = db;
            _excelService = excelService;
            _importService = importService;
        }

        public IActionResult Upload()
        {
            // زر رفع واحد فقط، بدون اختيار بند: لا حاجة لتمرير قائمة البنود للواجهة بعد الآن.
            return View();
        }

        /// <summary>
        /// نقطة الرفع الوحيدة بالنظام: ملف واحد بدون اختيار بند. يقرأ عمود "تصنيف البند" تلقائيًا
        /// من الملف، يوزّع كل صف إلى بنده الصحيح، ويحدّث قاعدة البيانات بالكامل لكل بند تأثّر بالملف.
        /// </summary>
        [HttpPost]
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
            var analyzed = _excelService.AnalyzeFile(stream);

            if (!analyzed.Success)
                return Json(new { success = false, message = analyzed.Message });

            var result = await _importService.ImportByClassificationAsync(analyzed, CurrentUserId, excelFile.FileName);

            return Json(new
            {
                success = result.Success,
                message = result.Message,
                rowsImported = result.RowsImported
            });
        }
    }
}
