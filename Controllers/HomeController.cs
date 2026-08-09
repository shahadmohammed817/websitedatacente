using ExcelDashboard.Data;
using ExcelDashboard.Models;
using ExcelDashboard.Models.ViewModels;
using ExcelDashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExcelDashboard.Controllers
{
    /// <summary>
    /// لا يوجد تسجيل دخول حالياً - يدخل الزائر مباشرة للصفحة الرئيسية ويرى كل البيانات.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IPermissionService _permissions;
        private readonly IAnalyticsService _analytics;

        // لا يوجد تسجيل دخول، فهذه القيمة غير مستخدمة فعلياً (أُبقيت لتوافق توقيعات الخدمات)
        private const int CurrentUserId = 0;

        public HomeController(AppDbContext db, IPermissionService permissions, IAnalyticsService analytics)
        {
            _db = db;
            _permissions = permissions;
            _analytics = analytics;
        }

        /// <summary>
        /// الصفحة الرئيسية: بطاقتا الأمانة والبلدية + لوحة بيانات عامة تفاعلية مع قائمة جانبية
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var vm = new ScopeSelectionViewModel
            {
                FullName = "",
                CanSeeAmanah = true,
                CanSeeBaladiyah = true
            };

            var amanahTotal = await _analytics.GetScopeTotalRevenueAsync(ScopeType.Amanah);
            vm.OverviewKpis.Add(new KpiSummaryViewModel { Label = "إجمالي إيرادات الأمانة", Value = amanahTotal, Icon = "", ColorClass = "c1" });

            var amanahTopSections = await _permissions.GetVisibleTopSectionsAsync(CurrentUserId, ScopeType.Amanah);
            vm.AmanahSections = amanahTopSections.Select(MapSection).ToList();

            var baladiyahTotal = await _analytics.GetScopeTotalRevenueAsync(ScopeType.Baladiyah);
            vm.OverviewKpis.Add(new KpiSummaryViewModel { Label = "إجمالي إيرادات البلدية", Value = baladiyahTotal, Icon = "", ColorClass = "c2" });

            var baladiyahTopSections = await _permissions.GetVisibleTopSectionsAsync(CurrentUserId, ScopeType.Baladiyah);
            vm.BaladiyahSections = baladiyahTopSections.Select(MapSection).ToList();

            return View(vm);
        }

        /// <summary>
        /// أُلغيت صفحة اختيار البنود الوسيطة: أي رابط قديم يُحوَّل مباشرة إلى
        /// داشبورد النطاق الكامل (بنود الأمانة/البلدية) حيث الاختيار من القائمة الجانبية
        /// </summary>
        [HttpGet("Home/Sections/{scope}")]
        public IActionResult Sections(string scope, int? parentId)
        {
            if (string.Equals(scope, "All", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index");

            if (!Enum.TryParse<ScopeType>(scope, true, out var scopeType))
                return NotFound();

            return RedirectToAction("Scope", "Dashboard", new { scope = scopeType });
        }

        private static SectionNodeViewModel MapSection(Section s) => new()
        {
            Id = s.Id,
            NameAr = s.NameAr,
            NameEn = s.NameEn,
            Slug = s.Slug,
            Icon = s.Icon,
            IsDashboardEntry = s.IsDashboardEntry,
            SubSections = s.SubSections
                .OrderBy(sub => sub.SortOrder)
                .Select(MapSection)
                .ToList()
        };
    }
}