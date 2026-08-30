using ExcelDashboard.Data;
using ExcelDashboard.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Controllers
{
    /// <summary>
    /// تعريف الموظف بالرقم الوظيفي فقط (بلا كلمة مرور) - مجرد بحث عن الرقم بجدول Users وتعبئة
    /// الجلسة بإدارته العامة، ليُوجَّه بعدها لنفس صفحات النظام لكنها تُفلتَر تلقائيًا على إدارته.
    /// </summary>
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;

        public AccountController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// يقبل الرقم الوظيفي إما من نموذج الدخول العادي (POST) أو كجزء من الرابط نفسه
        /// (?employeeNumber=xxxxx بالـ GET) - يتيح لأصحاب رابط ثابت (كالمدير/الوكيل) الدخول
        /// بضغطة واحدة من رابط محفوظ (Bookmark) بدون كتابة الرقم كل مرة، بينما موظفو الإدارات
        /// يستمرون بكتابته يدويًا من نفس الصفحة (مناسب لأجهزة مشتركة بين أكثر من موظف).
        /// </summary>
        [HttpGet("Account/Login")]
        [ActionName("Login")]
        public async Task<IActionResult> LoginGet(string? returnUrl, string? employeeNumber)
        {
            if (HttpContext.Session.IsSignedIn())
                return Redirect(SafeReturnUrl(returnUrl));

            if (!string.IsNullOrWhiteSpace(employeeNumber))
            {
                var (user, error) = await TrySignInAsync(employeeNumber);
                if (user != null)
                    return Redirect(SafeReturnUrl(returnUrl));

                ViewBag.Error = error;
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost("Account/Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string employeeNumber, string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            var (user, error) = await TrySignInAsync(employeeNumber);
            if (user == null)
            {
                ViewBag.Error = error;
                return View();
            }

            return Redirect(SafeReturnUrl(returnUrl));
        }

        private async Task<(ExcelDashboard.Models.AppUser? User, string? Error)> TrySignInAsync(string? employeeNumber)
        {
            if (string.IsNullOrWhiteSpace(employeeNumber))
                return (null, "يرجى إدخال الرقم الوظيفي.");

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.EmployeeNumber == employeeNumber.Trim() && u.IsActive);

            if (user == null)
                return (null, "الرقم الوظيفي غير مسجَّل بالنظام.");

            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            HttpContext.Session.SignInEmployee(user);
            return (user, null);
        }

        [HttpGet("Account/Logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        private static string SafeReturnUrl(string? returnUrl) =>
            !string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith("/") && !returnUrl.StartsWith("//")
                ? returnUrl
                : "/";
    }
}
