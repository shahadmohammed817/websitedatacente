using ClosedXML.Excel;
using ExcelDashboard.Helpers;
using ExcelDashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExcelDashboard.Controllers
{
    /// <summary>
    /// صفحتا "المطالبات والمديونيات" و"متابعة الأداء": مصدرهما الوحيد ملف Excel يُرفع من زر
    /// مخصص أعلى الصفحتين. لا يوجد تسجيل دخول حالياً - متاحتان مباشرة كبقية صفحات النظام.
    /// </summary>
    public class ClaimsController : AppControllerBase
    {
        private readonly IContractImportService _contractImport;
        private readonly IAnalyticsService _analyticsService;
        private readonly IContractPeriodImportService _contractPeriodImport;
        private readonly IContractPeriodAnalyticsService _contractPeriodAnalytics;
        private readonly IDebtforallImportService _debtforallImport;
        private readonly IDebtforallAnalyticsService _debtforallAnalytics;

        public ClaimsController(
            IContractImportService contractImport,
            IAnalyticsService analyticsService,
            IContractPeriodImportService contractPeriodImport,
            IContractPeriodAnalyticsService contractPeriodAnalytics,
            IDebtforallImportService debtforallImport,
            IDebtforallAnalyticsService debtforallAnalytics)
        {
            _contractImport = contractImport;
            _analyticsService = analyticsService;
            _contractPeriodImport = contractPeriodImport;
            _contractPeriodAnalytics = contractPeriodAnalytics;
            _debtforallImport = debtforallImport;
            _debtforallAnalytics = debtforallAnalytics;
        }

        // صفحة المديونيات لكل البنود
        [HttpGet("Claims/DebtforAll")]
        public IActionResult DebtforAll()
        {
            return View();
        }

        [HttpGet("Claims/DebtforAll/Data")]
        public async Task<IActionResult> DebtforAllData(int? year, int? quarter, int? month, int? hijriYear, string? scope)
        {
            var data = await _debtforallAnalytics.GetDataAsync(year, quarter, month, hijriYear, scope);
            return Json(data);
        }

        [HttpPost("Claims/DebtforAll/Upload")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> DebtforAllUpload(IFormFile debtforallFile)
        {
            if (debtforallFile == null || debtforallFile.Length == 0)
                return Json(new { success = false, message = "يرجى اختيار ملف صالح." });

            using var stream = debtforallFile.OpenReadStream();
            var result = await _debtforallImport.ImportAsync(stream);

            return Json(new { success = result.Success, message = result.Message, rowsImported = result.RowsImported });
        }










        /// <summary>صفحة "الطرح" (رغم اسم المسار "Followup" التاريخي)</summary>
        [HttpGet("Claims/Followup")]
        public IActionResult Followup()
        {
            return View();
        }

        /// <summary>صفحة "العقود التي تنتهي فترة سماحها خلال 3 أشهر" - مصدرها ملف "العقود.xlsx" يُرفع من زر مخصص أعلى الصفحة</summary>
        [HttpGet("Claims/GracePeriodExpiring")]
        public IActionResult GracePeriodExpiring()
        {
            return View();
        }

        [HttpGet("Claims/GracePeriodExpiring/Data")]
        public async Task<IActionResult> GracePeriodExpiringData(string? department, string? portfolio, string? contractType, string? contractStatus)
        {
            department = ScopedDepartment(department);
            var data = await _contractPeriodAnalytics.GetDataAsync(department, portfolio, contractType, contractStatus);
            if (IsDepartmentRestricted) data.DepartmentOptions = new List<string> { department! };
            return Json(data);
        }

        /// <summary>قائمة العقود لدرج تفاصيل قوس "العقود التي شارفت على الانتهاء" عند الضغط على لون (أحمر/أخضر)</summary>
        [HttpGet("Claims/GracePeriodExpiring/ContractEndingList")]
        public async Task<IActionResult> GracePeriodContractEndingList(bool isRed, string? department, string? portfolio, string? contractType, string? contractStatus)
        {
            department = ScopedDepartment(department);
            var rows = await _contractPeriodAnalytics.GetContractEndingListAsync(isRed, department, portfolio, contractType, contractStatus);
            return Json(rows);
        }

        /// <summary>قائمة العقود لدرج تفاصيل قوس "العقود التي تنتهي فترة سماحها خلال 2026" عند الضغط على لون (أحمر/أصفر/أخضر)</summary>
        [HttpGet("Claims/GracePeriodExpiring/PrepEndingList")]
        public async Task<IActionResult> GracePeriodPrepEndingList(string zone, string? department, string? portfolio, string? contractType, string? contractStatus)
        {
            department = ScopedDepartment(department);
            var rows = await _contractPeriodAnalytics.GetPrepEndingListAsync(zone, department, portfolio, contractType, contractStatus);
            return Json(rows);
        }

        /// <summary>رفع ملف "العقود" (زر مخصص أعلى الصفحة). يستبدل كل العقود القديمة بالكامل.</summary>
        [HttpPost("Claims/GracePeriodExpiring/Upload")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> GracePeriodExpiringUpload(IFormFile contractPeriodFile)
        {
            if (AppMode.ReadOnly)
                return Json(new { success = false, message = "الموقع بوضع العرض فقط حاليًا - الرفع غير متاح." });

            if (contractPeriodFile == null || contractPeriodFile.Length == 0)
                return Json(new { success = false, message = "يرجى اختيار ملف صالح." });

            if (!contractPeriodFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "يرجى رفع ملف بصيغة .xlsx فقط." });

            using var stream = contractPeriodFile.OpenReadStream();
            var result = await _contractPeriodImport.ImportAsync(stream);

            return Json(new { success = result.Success, message = result.Message, rowsImported = result.RowsImported });
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
            department = ScopedDepartment(department);
            var data = await _analyticsService.GetTarhDataAsync(department, status, contractNumber, year, quarter, forcedDepartment: HttpContext.Session.GetEmployeeDepartment());
            if (IsDepartmentRestricted) data.DepartmentOptions = new List<string> { department! };
            return Json(data);
        }

        /// <summary>قائمة الفرص (اللي عندها رقم عقد فعلي) لتعبئة مربع اختيار "تتبع الفرصة"</summary>
        [HttpGet("Claims/Followup/OpportunityOptions")]
        public async Task<IActionResult> TarhOpportunityOptions()
        {
            var options = await _analyticsService.GetTarhOpportunityOptionsAsync();
            return Json(options);
        }

        /// <summary>رحلة فرصة واحدة (فتح المظاريف ← الترسية ← توقيع العقد) برقم الفرصة</summary>
        [HttpGet("Claims/Followup/OpportunityTimeline")]
        public async Task<IActionResult> TarhOpportunityTimeline(string opportunityNumber)
        {
            if (string.IsNullOrWhiteSpace(opportunityNumber))
                return Json(new { found = false });

            var timeline = await _analyticsService.GetTarhOpportunityTimelineAsync(opportunityNumber);
            return Json(timeline);
        }

        /// <summary>قائمة الفرص التي فُتحت مظاريفها ضمن نطاق تاريخ معيّن (مثل "هذا الأسبوع") - قسم "الفرص التي فُتحت مظاريفها" بصفحة الطرح</summary>
        [HttpGet("Claims/Followup/EnvelopeOpenedData")]
        public async Task<IActionResult> TarhEnvelopeOpenedData(DateTime start, DateTime end)
        {
            var rows = await _analyticsService.GetTarhEnvelopeOpenedAsync(start, end, HttpContext.Session.GetEmployeeDepartment());
            return Json(rows);
        }

        /// <summary>
        /// صفوف تصدير صفحة "الطرح" بصيغة JSON (تُستخدَم لتعبئة جدول الطباعة المخفي قبل استدعاء
        /// window.print) - نفس تصفية TarhData بالضبط
        /// </summary>
        [HttpGet("Claims/Followup/ExportData")]
        public async Task<IActionResult> TarhExportData(string? department, string? status, string? contractNumber, int? year, int? quarter)
        {
            department = ScopedDepartment(department);
            var rows = await _analyticsService.GetTarhExportRowsAsync(department, status, contractNumber, year, quarter);
            return Json(rows);
        }

        /// <summary>تصدير بيانات صفحة "الطرح" كملف Excel (.xlsx) - نفس تصفية TarhData بالضبط</summary>
        [HttpGet("Claims/Followup/ExportExcel")]
        public async Task<IActionResult> TarhExportExcel(string? department, string? status, string? contractNumber, int? year, int? quarter)
        {
            department = ScopedDepartment(department);
            var rows = await _analyticsService.GetTarhExportRowsAsync(department, status, contractNumber, year, quarter);

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("عقود الطرح");
            sheet.RightToLeft = true;

            string[] headers = { "رقم الفرصة", "رقم العقد", "اسم الفرصة", "القيمة", "الإدارة العامة", "المحفظة الفرعية", "سنة الطرح", "تاريخ العقد" };
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
                sheet.Cell(row, 1).Value = r.OpportunityNumber ?? "";
                sheet.Cell(row, 2).Value = r.ContractNumber ?? "";
                sheet.Cell(row, 3).Value = r.OpportunityName ?? "";
                sheet.Cell(row, 4).Value = r.Value;
                sheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                sheet.Cell(row, 5).Value = r.GeneralDepartment ?? "";
                sheet.Cell(row, 6).Value = r.SubPortfolio ?? "";
                if (r.ContractYear.HasValue) sheet.Cell(row, 7).Value = r.ContractYear.Value;
                if (r.ContractDate.HasValue)
                {
                    sheet.Cell(row, 8).Value = r.ContractDate.Value;
                    sheet.Cell(row, 8).Style.DateFormat.Format = "yyyy-MM-dd";
                }
                row++;
            }

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "عقود_الطرح.xlsx");
        }
    }
}
