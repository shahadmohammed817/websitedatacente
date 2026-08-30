using ExcelDashboard.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace ExcelDashboard.Controllers
{
    /// <summary>
    /// Controller أساسي للصفحات التي تدعم الفلترة بالإدارة العامة - يوفّر ScopedDepartment التي
    /// تفرض إدارة الموظف المُسجَّل (إن كان مقيّدًا) على أي قيمة فلتر يرسلها العميل، بصرف النظر
    /// عمّا بالـ query string (حماية على مستوى الخادم لا الواجهة فقط).
    /// </summary>
    public abstract class AppControllerBase : Controller
    {
        /// <summary>يُرجع إدارة الموظف الحالي إن كان مقيّدًا بإدارة واحدة، وإلا يُرجع القيمة المطلوبة كما هي (بلا قيد)</summary>
        protected string? ScopedDepartment(string? requested)
        {
            var mine = HttpContext.Session.GetEmployeeDepartment();
            return string.IsNullOrEmpty(mine) ? requested : mine;
        }

        /// <summary>true إن كان الموظف الحالي مقيّدًا بإدارة عامة واحدة (وليس مديرًا/بلا قيد)</summary>
        protected bool IsDepartmentRestricted => !string.IsNullOrEmpty(HttpContext.Session.GetEmployeeDepartment());
    }
}
