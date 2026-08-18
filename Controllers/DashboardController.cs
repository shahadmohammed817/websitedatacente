using ClosedXML.Excel;
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
        private readonly IRevenueBaselineImportService _revenueBaselineImport;

        private const int CurrentUserId = 0;

        public DashboardController(
            AppDbContext db,
            IPermissionService permissions,
            IAnalyticsService analytics,
            IMonthlyTargetImportService monthlyTargetImport,
            IFinanceTargetImportService financeTargetImport,
            IAmanahInvestmentDashboardService amanahInvestment,
            IRevenueBaselineImportService revenueBaselineImport)
        {
            _db = db;
            _permissions = permissions;
            _analytics = analytics;
            _monthlyTargetImport = monthlyTargetImport;
            _financeTargetImport = financeTargetImport;
            _amanahInvestment = amanahInvestment;
            _revenueBaselineImport = revenueBaselineImport;
        }

        /// <summary>
        /// بيانات الرسوم المخصّصة ببند الاستثمار داخل صفحة "بنود الأمانة" (Dashboard/Scope/Amanah)
        /// - المنطقة=أمانة + تصنيف البند=استثمار حصريًا، مع دعم فلترَي الربع/رقم العقد المشتركين
        /// بالصفحة، بالإضافة لـ deptGeneral/deptSub (اختيار إدارة عامة/فرعية من صندوق الفلاتر)
        /// لتفعيل الترابط بين الرسوم (Cross Filtering).
        /// </summary>
        [HttpGet("Dashboard/AmanahInvestment/Data")]
        public async Task<IActionResult> AmanahInvestmentData(string? quarter, string? contract, string? sectionIds, string? deptGeneral, string? deptSub, string? investmentType)
        {
            var ids = ParseSectionIds(sectionIds);
            var data = await _amanahInvestment.GetDataAsync(quarter, contract, ids, deptGeneral, deptSub, investmentType);
            return Json(data);
        }

        private static List<int>? ParseSectionIds(string? sectionIds)
        {
            if (string.IsNullOrWhiteSpace(sectionIds)) return null;
            return sectionIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => int.TryParse(x, out var n) ? n : (int?)null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .ToList();
        }

        /// <summary>
        /// صفوف تصدير صفحة "الإدارات العامة" بصيغة JSON (تُستخدَم لتعبئة جدول الطباعة المخفي قبل
        /// استدعاء window.print) - نفس تصفية AmanahInvestmentData بالضبط
        /// </summary>
        [HttpGet("Dashboard/AmanahInvestment/ExportData")]
        public async Task<IActionResult> AmanahInvestmentExportData(string? quarter, string? contract, string? sectionIds, string? deptGeneral, string? deptSub, string? investmentType)
        {
            var ids = ParseSectionIds(sectionIds);
            var rows = await _amanahInvestment.GetExportRowsAsync(quarter, contract, ids, deptGeneral, deptSub, investmentType);
            return Json(rows);
        }

        /// <summary>تصدير بيانات صفحة "الإدارات العامة" كملف Excel (.xlsx) - نفس تصفية AmanahInvestmentData بالضبط</summary>
        [HttpGet("Dashboard/AmanahInvestment/ExportExcel")]
        public async Task<IActionResult> AmanahInvestmentExportExcel(string? quarter, string? contract, string? sectionIds, string? deptGeneral, string? deptSub, string? investmentType)
        {
            var ids = ParseSectionIds(sectionIds);
            var rows = await _amanahInvestment.GetExportRowsAsync(quarter, contract, ids, deptGeneral, deptSub, investmentType);

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("الإدارات العامة");
            sheet.RightToLeft = true;

            string[] headers = { "رقم العقد", "اسم المستثمر", "تاريخ الإنشاء", "تاريخ الدفع", "رقم الدفعة", "القيمة", "المحفظة الفرعية" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0E6E61");
                cell.Style.Font.FontColor = XLColor.White;
            }

            int row = 2;
            foreach (var r in rows)
            {
                sheet.Cell(row, 1).Value = r.ContractNumber ?? "";
                sheet.Cell(row, 2).Value = r.InvestorName ?? "";
                if (r.CreatedDate.HasValue)
                {
                    sheet.Cell(row, 3).Value = r.CreatedDate.Value;
                    sheet.Cell(row, 3).Style.DateFormat.Format = "yyyy-MM-dd";
                }
                if (r.RecordDate.HasValue)
                {
                    sheet.Cell(row, 4).Value = r.RecordDate.Value;
                    sheet.Cell(row, 4).Style.DateFormat.Format = "yyyy-MM-dd";
                }
                sheet.Cell(row, 5).Value = r.PaymentNumber ?? "";
                if (r.TotalValue.HasValue)
                {
                    sheet.Cell(row, 6).Value = r.TotalValue.Value;
                    sheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
                }
                sheet.Cell(row, 7).Value = r.SubPortfolio ?? "";
                row++;
            }

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "الإدارات_العامة.xlsx");
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

            // فلتر "الإدارة العامة/الإدارة الفرعية" لصفحة "الإدارات العامة": يُبنى مباشرة من عمودي
            // DeptGeneral/DeptSub الخام على DashboardRecord (نفس مصدر بيانات AmanahInvestmentDashboardService)
            // بدل شجرة البنود، لأن البنود الفرعية الفعلية المستوردة (تأجير مباني تجارية...) لا
            // تحمل شجرة إدارات خاصة بها
            if (scopeType == ScopeType.Amanah)
            {
                var amanahSectionIds = scopeSections.Select(s => s.Id).ToList();
                var deptPairs = await _db.DashboardRecords
                    .Where(r => (r.Classification == "استثمار" || r.Classification == "تأجير مؤقت")
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

        [HttpGet("Dashboard/Debug/DebtStatuses")]
        public async Task<IActionResult> DebugDebtStatuses([FromServices] AppDbContext db)
        {
            var years = await db.DebtInvoiceRecords.Where(r => r.CreatedDate != null).Select(r => r.CreatedDate!.Value.Year).ToListAsync();
            var yearCounts = years.GroupBy(y => y).Select(g => new { year = g.Key, count = g.Count() }).OrderBy(x => x.year).ToList();
            return Json(new { yearCounts });
        }


        /// <summary>
        /// رفع ملف "المحصل الفعلي لسنة الأساس 2025" (بطاقتا "محصل 2025"/"نسبة نمو الإيرادات"
        /// أعلى رسم "أداء الإيرادات" بالرئيسية). يستبدل بيانات سنة الأساس القديمة بالكامل.
        /// </summary>
        [HttpPost("Dashboard/UploadRevenueBaseline")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> UploadRevenueBaseline(IFormFile revenueBaselineFile)
        {
            if (AppMode.ReadOnly)
                return Json(new { success = false, message = "الموقع بوضع العرض فقط حاليًا - الرفع غير متاح." });

            if (revenueBaselineFile == null || revenueBaselineFile.Length == 0)
                return Json(new { success = false, message = "يرجى اختيار ملف صالح." });

            if (!revenueBaselineFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "يرجى رفع ملف بصيغة .xlsx فقط." });

            using var stream = revenueBaselineFile.OpenReadStream();
            var result = await _revenueBaselineImport.ImportAsync(stream);

            return Json(new
            {
                success = result.Success,
                message = result.Message,
                classificationsImported = result.ClassificationsImported
            });
        }

        /// <summary>
        /// نقطة نهاية JSON تُرجع لوحة بيانات كاملة لجهة واحدة (مثل: بلدية الحناكية)
        /// مثال: Dashboard/Entity/Data?name=بلدية الحناكية
        /// </summary>
        [HttpGet("Dashboard/Entity/Data")]
        public async Task<IActionResult> EntityData(string name, string? quarter, string? contract, int? month)
        {
            if (string.IsNullOrWhiteSpace(name)) return NotFound();

            var data = await _analytics.GetEntityDataAsync(name.Trim(), quarter, contract, month);
            return Json(data);
        }

        /// <summary>
        /// نقطة نهاية JSON تُرجع بيانات مجمّعة لعدة بنود معاً (مجموعة إدارات مكررة بنفس الاسم)
        /// مثال: Dashboard/Sections/Data?ids=301,401,501
        /// </summary>
        [HttpGet("Dashboard/Sections/Data")]
        public async Task<IActionResult> SectionsData(string ids, string? quarter, string? contract, int? month)
        {
            var idList = (ids ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => int.TryParse(x, out var n) ? n : (int?)null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .Distinct()
                .ToList();

            if (!idList.Any()) return NotFound();

            var data = await _analytics.GetSectionsDataAsync(idList, quarter, contract, month);
            return Json(data);
        }

        /// <summary>
        /// نقطة نهاية JSON تُستخدم من JavaScript لجلب بيانات الرسوم البيانية لبند معين
        /// </summary>
        [HttpGet("Dashboard/{sectionId:int}/Data")]
        public async Task<IActionResult> SectionData(int sectionId, string? quarter, string? contract, int? month)
        {
            var data = await _analytics.GetSectionDataAsync(sectionId, quarter: quarter, contractNumber: contract, month: month);
            return Json(data);
        }

        /// <summary>
        /// نقطة نهاية JSON تُرجع لوحة بيانات مجمّعة لكل بنود نطاق كامل (الأمانة أو البلدية)
        /// </summary>
        [HttpGet("Dashboard/Scope/{scope}/Data")]
        public async Task<IActionResult> ScopeData(string scope, string? quarter, string? contract, int? month)
        {
            if (!Enum.TryParse<ScopeType>(scope, true, out var scopeType))
                return NotFound();

            var data = await _analytics.GetScopeDataAsync(scopeType, quarter, contract, month);
            return Json(data);
        }

        /// <summary>
        /// نقطة نهاية JSON تُرجع لوحة بيانات عامة مجمّعة لكل النطاقات (الأمانة + البلدية معاً)
        /// </summary>
        [HttpGet("Dashboard/Overview/Data")]
        public async Task<IActionResult> OverviewData(string? quarter, string? contract, int? month)
        {
            var scopes = new List<ScopeType> { ScopeType.Amanah, ScopeType.Baladiyah };
            var data = await _analytics.GetOverviewDataAsync(scopes, quarter, contract, month);
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

    }
}