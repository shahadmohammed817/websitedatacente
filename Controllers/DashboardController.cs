using ExcelDashboard.Data;
using ExcelDashboard.Helpers;
using ExcelDashboard.Models;
using ExcelDashboard.Models.ViewModels;
using ExcelDashboard.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Controllers
{
    /// <summary>
    /// لا يوجد تسجيل دخول حالياً - كل الصفحات متاحة مباشرة دون قيود صلاحيات.
    /// </summary>
    public class DashboardController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IPermissionService _permissions;
        private readonly IAnalyticsService _analytics;
        private readonly IMonthlyTargetImportService _monthlyTargetImport;
        private readonly IFinanceTargetImportService _financeTargetImport;
        private readonly IAmanahInvestmentDashboardService _amanahInvestment;

        private const int CurrentUserId = 0;

        public DashboardController(
            AppDbContext db,
            IPermissionService permissions,
            IAnalyticsService analytics,
            IMonthlyTargetImportService monthlyTargetImport,
            IFinanceTargetImportService financeTargetImport,
            IAmanahInvestmentDashboardService amanahInvestment)
        {
            _db = db;
            _permissions = permissions;
            _analytics = analytics;
            _monthlyTargetImport = monthlyTargetImport;
            _financeTargetImport = financeTargetImport;
            _amanahInvestment = amanahInvestment;
        }

        /// <summary>
        /// بيانات الرسوم المخصّصة ببند الاستثمار داخل صفحة "بنود الأمانة" (Dashboard/Scope/Amanah)
        /// - المنطقة=أمانة + تصنيف البند=استثمار حصريًا، مع دعم فلترَي الربع/رقم العقد المشتركين
        /// بالصفحة، بالإضافة لـ deptGeneral/deptSub (اختيار إدارة عامة/فرعية من صندوق الفلاتر)
        /// لتفعيل الترابط بين الرسوم (Cross Filtering).
        /// </summary>
        [HttpGet("Dashboard/AmanahInvestment/Data")]
        public async Task<IActionResult> AmanahInvestmentData(string? quarter, string? contract, string? sectionIds, string? deptGeneral, string? deptSub)
        {
            List<int>? ids = null;
            if (!string.IsNullOrWhiteSpace(sectionIds))
            {
                ids = sectionIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(x => int.TryParse(x, out var n) ? n : (int?)null)
                    .Where(n => n.HasValue)
                    .Select(n => n!.Value)
                    .ToList();
            }

            var data = await _amanahInvestment.GetDataAsync(quarter, contract, ids, deptGeneral, deptSub);
            return Json(data);
        }

        /// <summary>
        /// صفحة الـ Dashboard الخاصة ببند معين (مثل: استثمار، تأجير مؤقت، الجزاءات...)
        /// </summary>
        [HttpGet("Dashboard/{sectionId:int}")]
        public async Task<IActionResult> Section(int sectionId)
        {
            var section = await _db.Sections
                .Include(s => s.SubSections.Where(d => d.IsActive))
                    .ThenInclude(d => d.SubSections.Where(sd => sd.IsActive))
                .FirstOrDefaultAsync(s => s.Id == sectionId);

            if (section == null || !section.IsActive) return NotFound();

            var data = await _analytics.GetSectionDataAsync(sectionId);

            var vm = new SectionDashboardViewModel
            {
                SectionId = section.Id,
                SectionNameAr = section.NameAr,
                SectionNameEn = section.NameEn,
                Icon = section.Icon,
                IsAdmin = true,
                HasData = data.HasData,
                SubSections = section.SubSections
                    .OrderBy(d => d.SortOrder)
                    .Select(MapSection)
                    .ToList()
            };

            return View(vm);
        }

        private static SectionNodeViewModel MapSection(Section s) => new()
        {
            Id = s.Id,
            NameAr = s.NameAr,
            NameEn = s.NameEn,
            Slug = s.Slug,
            Icon = s.Icon,
            SubSections = s.SubSections
                .OrderBy(sub => sub.SortOrder)
                .Select(MapSection)
                .ToList()
        };

        /// <summary>
        /// داشبورد نطاق كامل (بنود الأمانة أو بنود البلدية): يعرض كل بيانات النطاق فورًا.
        /// القائمة الجانبية = الإدارات العامة (مثل: محافظ الاستثمارات التجارية/التنموية/الخدمات)
        /// مجمّعة بالاسم عبر كل البنود المكررة تحتها، وكل إدارة تنسدل عن إداراتها الفرعية.
        /// إن لم توجد إدارات عامة في النطاق (كالبلدية) تُعرض البنود العليا بدلاً منها.
        /// </summary>
        [HttpGet("Dashboard/Scope/{scope}")]
        public async Task<IActionResult> Scope(string scope)
        {
            if (!Enum.TryParse<ScopeType>(scope, true, out var scopeType))
                return NotFound();

            var scopeSections = await _db.Sections
                .Where(s => s.Scope == scopeType && s.IsActive)
                .ToListAsync();

            // الإدارات العامة: أبناء البنود المعلّمة IsDashboardEntry (تتكرر بنفس الاسم
            // تحت تأجير المباني/تأجير المرافق/ريع الأراضي...) — تُجمع بالاسم في خيار واحد
            var entryIds = scopeSections.Where(s => s.IsDashboardEntry).Select(s => s.Id).ToHashSet();
            var deptSections = scopeSections
                .Where(s => s.ParentSectionId.HasValue && entryIds.Contains(s.ParentSectionId.Value))
                .ToList();

            var deptGroups = deptSections
                .GroupBy(d => d.NameAr)
                .OrderBy(g => g.Min(x => x.SortOrder)).ThenBy(g => g.Min(x => x.Id))
                .Select(g =>
                {
                    var memberIds = g.Select(x => x.Id).ToHashSet();
                    var children = scopeSections
                        .Where(c => c.ParentSectionId.HasValue && memberIds.Contains(c.ParentSectionId.Value))
                        .GroupBy(c => c.NameAr)
                        .OrderBy(cg => cg.Min(x => x.SortOrder)).ThenBy(cg => cg.Min(x => x.Id))
                        .Select(cg => new DeptGroupViewModel
                        {
                            NameAr = cg.Key,
                            NameEn = cg.First().NameEn,
                            Slug = cg.First().Slug,
                            SectionIds = cg.Select(x => x.Id).ToList()
                        })
                        .ToList();

                    return new DeptGroupViewModel
                    {
                        NameAr = g.Key,
                        NameEn = g.First().NameEn,
                        Slug = g.First().Slug,
                        SectionIds = g.Select(x => x.Id).ToList(),
                        Children = children
                    };
                })
                .ToList();

            var vm = new ScopeDashboardViewModel
            {
                Scope = scopeType,
                DeptGroups = deptGroups
            };

            // فلتر "الإدارة العامة/الإدارة الفرعية" لبند الاستثمار: يُبنى مباشرة من عمودي
            // DeptGeneral/DeptSub الخام على DashboardRecord (نفس مصدر بيانات AmanahInvestmentDashboardService)
            // بدل شجرة البنود، لأن البنود الفرعية الفعلية المستوردة (تأجير مباني تجارية...) لا
            // تحمل شجرة إدارات خاصة بها
            if (scopeType == ScopeType.Amanah)
            {
                var amanahSectionIds = scopeSections.Select(s => s.Id).ToList();
                var deptPairs = await _db.DashboardRecords
                    .Where(r => r.Classification == "استثمار"
                        && amanahSectionIds.Contains(r.SectionId)
                        && r.DeptGeneral != null && r.DeptGeneral.Contains("الإدارة"))
                    .Select(r => new { r.DeptGeneral, r.DeptSub })
                    .Distinct()
                    .ToListAsync();

                vm.AmanahDeptOptions = deptPairs
                    .GroupBy(x => x.DeptGeneral!)
                    .Select(g => new AmanahDeptOption
                    {
                        Name = g.Key,
                        Subs = g.Where(x => !string.IsNullOrWhiteSpace(x.DeptSub))
                                .Select(x => x.DeptSub!)
                                .Distinct()
                                .OrderBy(x => x)
                                .ToList()
                    })
                    .OrderBy(g => g.Name)
                    .ToList();
            }

            // نطاق البلدية: القائمة الجانبية = البلديات نفسها (من عمود "الجهة" بالبيانات المستوردة)،
            // كل بلدية تفتح لوحة بياناتها الخاصة مباشرة
            if (scopeType == ScopeType.Baladiyah)
            {
                var scopeSectionIds = scopeSections.Select(s => s.Id).ToList();
                var entities = await _db.DashboardRecords
                    .Where(r => r.Entity != null && scopeSectionIds.Contains(r.SectionId))
                    .Select(r => r.Entity!)
                    .Distinct()
                    .ToListAsync();

                vm.Entities = EntityOrderHelper.SortEntities(entities);
            }

            // لا إدارات عامة ولا جهات؟ تُعرض البنود العليا كقائمة بديلة
            if (!deptGroups.Any() && !vm.Entities.Any())
            {
                var topSections = await _permissions.GetVisibleTopSectionsAsync(CurrentUserId, scopeType);
                vm.TopSections = topSections.Select(MapSection).ToList();
            }

            return View(vm);
        }

        /// <summary>
        /// نقطة نهاية JSON تُرجع لوحة بيانات كاملة لجهة واحدة (مثل: بلدية الحناكية)
        /// مثال: Dashboard/Entity/Data?name=بلدية الحناكية
        /// </summary>
        [HttpGet("Dashboard/Entity/Data")]
        public async Task<IActionResult> EntityData(string name, string? quarter, string? contract)
        {
            if (string.IsNullOrWhiteSpace(name)) return NotFound();

            var data = await _analytics.GetEntityDataAsync(name.Trim(), quarter, contract);
            return Json(data);
        }

        /// <summary>
        /// نقطة نهاية JSON تُرجع بيانات مجمّعة لعدة بنود معاً (مجموعة إدارات مكررة بنفس الاسم)
        /// مثال: Dashboard/Sections/Data?ids=301,401,501
        /// </summary>
        [HttpGet("Dashboard/Sections/Data")]
        public async Task<IActionResult> SectionsData(string ids, string? quarter, string? contract)
        {
            var idList = (ids ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => int.TryParse(x, out var n) ? n : (int?)null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .Distinct()
                .ToList();

            if (!idList.Any()) return NotFound();

            var data = await _analytics.GetSectionsDataAsync(idList, quarter, contract);
            return Json(data);
        }

        /// <summary>
        /// نقطة نهاية JSON تُستخدم من JavaScript لجلب بيانات الرسوم البيانية لبند معين
        /// </summary>
        [HttpGet("Dashboard/{sectionId:int}/Data")]
        public async Task<IActionResult> SectionData(int sectionId, string? quarter, string? contract)
        {
            var data = await _analytics.GetSectionDataAsync(sectionId, quarter: quarter, contractNumber: contract);
            return Json(data);
        }

        /// <summary>
        /// نقطة نهاية JSON تُرجع لوحة بيانات مجمّعة لكل بنود نطاق كامل (الأمانة أو البلدية)
        /// </summary>
        [HttpGet("Dashboard/Scope/{scope}/Data")]
        public async Task<IActionResult> ScopeData(string scope, string? quarter, string? contract)
        {
            if (!Enum.TryParse<ScopeType>(scope, true, out var scopeType))
                return NotFound();

            var data = await _analytics.GetScopeDataAsync(scopeType, quarter, contract);
            return Json(data);
        }

        /// <summary>
        /// نقطة نهاية JSON تُرجع لوحة بيانات عامة مجمّعة لكل النطاقات (الأمانة + البلدية معاً)
        /// </summary>
        [HttpGet("Dashboard/Overview/Data")]
        public async Task<IActionResult> OverviewData(string? quarter, string? contract)
        {
            var scopes = new List<ScopeType> { ScopeType.Amanah, ScopeType.Baladiyah };
            var data = await _analytics.GetOverviewDataAsync(scopes, quarter, contract);
            return Json(data);
        }

        /// <summary>
        /// صفحة "مقارنة المستهدفات": تعرض المستهدفات السنوية (وزارة المالية / وزارة البلديات
        /// والإسكان)، ومقارنة شهرية بين المحصّل والمستهدف - المالية، وبين المفوتر والمستهدف -
        /// البلديات والإسكان.
        /// </summary>
        [HttpGet("Dashboard/TargetsComparison")]
        public IActionResult TargetsComparison()
        {
            return View();
        }

        /// <summary>صفحة مستهدفات وزارة البلديات والإسكان وحدها (بطاقة مستهدف + رسمان + رفع ملف المستهدف الشهري)</summary>
        [HttpGet("Dashboard/TargetsComparison/Baladiyah")]
        public IActionResult TargetsComparisonBaladiyah()
        {
            return View();
        }

        /// <summary>صفحة مستهدفات وزارة المالية وحدها (بطاقة مستهدف + رسمان)</summary>
        [HttpGet("Dashboard/TargetsComparison/Finance")]
        public IActionResult TargetsComparisonFinance()
        {
            return View();
        }

        /// <summary>
        /// نقطة نهاية JSON تُرجع بيانات صفحة "مقارنة المستهدفات" (اختياريًا مُصفّاة بجهة واحدة)
        /// </summary>
        [HttpGet("Dashboard/TargetsComparison/Data")]
        public async Task<IActionResult> TargetsComparisonData(string? entity)
        {
            var data = await _analytics.GetTargetsComparisonDataAsync(entity);
            return Json(data);
        }

        /// <summary>
        /// رفع ملف "المستهدف الشهري للبلديات" (زر مخصص بصفحة مقارنة المستهدفات). يستبدل كل
        /// المستهدفات الشهرية القديمة بالكامل بمحتوى الملف الجديد، وتُستخدَم فورًا لبناء خط
        /// "المستهدف الشهري" الحقيقي بدل التوزيع الافتراضي (السنوي ÷ 12).
        /// </summary>
        [HttpPost("Dashboard/TargetsComparison/UploadMonthlyTarget")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> UploadMonthlyTarget(IFormFile monthlyTargetFile)
        {
            if (AppMode.ReadOnly)
                return Json(new { success = false, message = "الموقع بوضع العرض فقط حاليًا - الرفع غير متاح." });

            if (monthlyTargetFile == null || monthlyTargetFile.Length == 0)
                return Json(new { success = false, message = "يرجى اختيار ملف صالح." });

            if (!monthlyTargetFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "يرجى رفع ملف بصيغة .xlsx فقط." });

            using var stream = monthlyTargetFile.OpenReadStream();
            var result = await _monthlyTargetImport.ImportAsync(stream);

            return Json(new
            {
                success = result.Success,
                message = result.Message,
                rowsImported = result.RowsImported,
                entitiesFound = result.EntitiesFound
            });
        }

        /// <summary>
        /// نقطة نهاية JSON تُرجع بيانات صفحة "مستهدفات وزارة المالية" (محصّل/مستهدف/نسبة تحقيق
        /// لكل من الأمانة والبلديات مجتمعة، + تفصيل حسب البند الرئيسي لكل نطاق)
        /// </summary>
        [HttpGet("Dashboard/TargetsComparison/FinanceData")]
        public async Task<IActionResult> TargetsComparisonFinanceData()
        {
            var data = await _analytics.GetFinanceTargetsPageDataAsync();
            return Json(data);
        }

        /// <summary>
        /// رفع ملف "مستهدف المالية" (زر مخصص بصفحة مستهدفات وزارة المالية). يستبدل كل مستهدفات
        /// وزارة المالية القديمة بالكامل بمحتوى الملف الجديد.
        /// </summary>
        [HttpPost("Dashboard/TargetsComparison/UploadFinanceTarget")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> UploadFinanceTarget(IFormFile financeTargetFile)
        {
            if (AppMode.ReadOnly)
                return Json(new { success = false, message = "الموقع بوضع العرض فقط حاليًا - الرفع غير متاح." });

            if (financeTargetFile == null || financeTargetFile.Length == 0)
                return Json(new { success = false, message = "يرجى اختيار ملف صالح." });

            if (!financeTargetFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "يرجى رفع ملف بصيغة .xlsx فقط." });

            using var stream = financeTargetFile.OpenReadStream();
            var result = await _financeTargetImport.ImportAsync(stream);

            return Json(new
            {
                success = result.Success,
                message = result.Message,
                rowsImported = result.RowsImported
            });
        }
    }
}