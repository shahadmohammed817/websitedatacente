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

            // تشخيص مؤقت: عرض أسماء أعمدة الملف كاملة لتحديد عمودي الأمانة/البلدية الفعليين - يُزال بعد التأكد
            var debugColumns = " | أعمدة الملف: " + string.Join(" ، ", analyzed.Columns.Select(c => c.Name));

            return Json(new
            {
                success = result.Success,
                message = result.Message + debugColumns,
                rowsImported = result.RowsImported
            });
        }

        // ==========================================================================
        // ===== إدارة الموظفين (رقم وظيفي ← إدارة عامة) - محمية بدور المدير فقط =====
        // ==========================================================================

        [HttpGet("Admin/Employees")]
        public async Task<IActionResult> Employees()
        {
            if (!HttpContext.Session.IsAdmin())
                return RedirectToAction("Index", "Home");

            ViewBag.Departments = GeneralDepartments.All;
            var employees = await _db.Users.OrderBy(u => u.EmployeeNumber).ToListAsync();
            return View(employees);
        }

        [HttpPost("Admin/Employees/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmployeeCreate(string employeeNumber, string fullName, string? generalDepartment, UserRole role)
        {
            if (!HttpContext.Session.IsAdmin())
                return RedirectToAction("Index", "Home");

            employeeNumber = (employeeNumber ?? "").Trim();
            fullName = (fullName ?? "").Trim();

            if (string.IsNullOrWhiteSpace(employeeNumber) || string.IsNullOrWhiteSpace(fullName))
            {
                TempData["EmployeeError"] = "يرجى إدخال الرقم الوظيفي والاسم.";
                return RedirectToAction(nameof(Employees));
            }

            if (await _db.Users.AnyAsync(u => u.EmployeeNumber == employeeNumber))
            {
                TempData["EmployeeError"] = "هذا الرقم الوظيفي مسجَّل مسبقًا.";
                return RedirectToAction(nameof(Employees));
            }

            _db.Users.Add(new AppUser
            {
                EmployeeNumber = employeeNumber,
                FullName = fullName,
                Email = $"{employeeNumber}@local.internal",
                GeneralDepartment = string.IsNullOrWhiteSpace(generalDepartment) ? null : generalDepartment,
                Role = role,
                IsActive = true
            });
            await _db.SaveChangesAsync();

            TempData["EmployeeSuccess"] = "تمت إضافة الموظف بنجاح.";
            return RedirectToAction(nameof(Employees));
        }

        [HttpPost("Admin/Employees/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmployeeEdit(int id, string fullName, string? generalDepartment, UserRole role, bool isActive)
        {
            if (!HttpContext.Session.IsAdmin())
                return RedirectToAction("Index", "Home");

            var user = await _db.Users.FindAsync(id);
            if (user == null)
            {
                TempData["EmployeeError"] = "الموظف غير موجود.";
                return RedirectToAction(nameof(Employees));
            }

            user.FullName = (fullName ?? "").Trim();
            user.GeneralDepartment = string.IsNullOrWhiteSpace(generalDepartment) ? null : generalDepartment;
            user.Role = role;
            user.IsActive = isActive;
            await _db.SaveChangesAsync();

            TempData["EmployeeSuccess"] = "تم تحديث بيانات الموظف.";
            return RedirectToAction(nameof(Employees));
        }

        [HttpPost("Admin/Employees/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmployeeDelete(int id)
        {
            if (!HttpContext.Session.IsAdmin())
                return RedirectToAction("Index", "Home");

            var user = await _db.Users.FindAsync(id);
            if (user != null)
            {
                _db.Users.Remove(user);
                await _db.SaveChangesAsync();
            }

            TempData["EmployeeSuccess"] = "تم حذف الموظف.";
            return RedirectToAction(nameof(Employees));
        }
    }
}
