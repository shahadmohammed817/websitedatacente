using ClosedXML.Excel;
using ExcelDashboard.Helpers;
using ExcelDashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExcelDashboard.Controllers
{
    /// <summary>صفحة "لوحة المطالبات التنفيذية" - مصدرها ملف "تقرير التنفيذ القضائي" يُرفع من زر مخصص أعلى الصفحة.</summary>
    public class ClaimsExecutionController : Controller
    {
        private readonly IClaimsExecutionAnalyticsService _analytics;
        private readonly IClaimExecutionImportService _importService;

        public ClaimsExecutionController(
            IClaimsExecutionAnalyticsService analytics,
            IClaimExecutionImportService importService)
        {
            _analytics = analytics;
            _importService = importService;
        }

        [HttpGet("ClaimsExecution")]
        public IActionResult Index() => View();

        [HttpGet("ClaimsExecution/Data")]
        public async Task<IActionResult> Data(string? department, string? claimStatus, string? opportunityNumber, string? contractNumber, string? invoiceNumber)
        {
            var data = await _analytics.GetDataAsync(department, claimStatus, opportunityNumber, contractNumber, invoiceNumber);
            return Json(data);
        }

        [HttpPost("ClaimsExecution/Upload")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> Upload(IFormFile claimsFile)
        {
            if (AppMode.ReadOnly)
                return Json(new { success = false, message = "الموقع بوضع العرض فقط حاليًا - الرفع غير متاح." });

            if (claimsFile == null || claimsFile.Length == 0)
                return Json(new { success = false, message = "يرجى اختيار ملف صالح." });

            if (!claimsFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "يرجى رفع ملف بصيغة .xlsx فقط." });

            using var stream = claimsFile.OpenReadStream();
            var result = await _importService.ImportAsync(stream);

            return Json(new { success = result.Success, message = result.Message, rowsImported = result.RowsImported });
        }

        /// <summary>صفوف تصدير الصفحة بصيغة JSON (تُستخدَم لتعبئة جدول الطباعة المخفي قبل استدعاء window.print)</summary>
        [HttpGet("ClaimsExecution/ExportData")]
        public async Task<IActionResult> ExportData(string? department, string? claimStatus, string? opportunityNumber, string? contractNumber, string? invoiceNumber)
        {
            var rows = await _analytics.GetExportRowsAsync(department, claimStatus, opportunityNumber, contractNumber, invoiceNumber);
            return Json(rows.Select(r => new
            {
                contractNumber = r.ContractNumber,
                investorName = r.InvestorName,
                invoiceNumber = r.InvoiceNumber,
                claimStatusFull = r.ClaimStatusFull,
                generalDepartment = r.GeneralDepartment,
                portfolio = r.Portfolio,
                paymentValueWithTax = r.PaymentValueWithTax,
                paymentDueDate = r.PaymentDueDate
            }));
        }

        /// <summary>تصدير بيانات الصفحة كملف Excel (.xlsx)</summary>
        [HttpGet("ClaimsExecution/ExportExcel")]
        public async Task<IActionResult> ExportExcel(string? department, string? claimStatus, string? opportunityNumber, string? contractNumber, string? invoiceNumber)
        {
            var rows = await _analytics.GetExportRowsAsync(department, claimStatus, opportunityNumber, contractNumber, invoiceNumber);

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("المديونيات والمطالبات");
            sheet.RightToLeft = true;

            string[] headers = { "رقم العقد", "اسم المستثمر", "رقم الفاتورة", "حالة المطالبة", "الإدارة العامة", "المحفظة", "القيمة", "تاريخ استحقاق الدفعة" };
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
                sheet.Cell(row, 3).Value = r.InvoiceNumber ?? "";
                sheet.Cell(row, 4).Value = r.ClaimStatusFull ?? "";
                sheet.Cell(row, 5).Value = r.GeneralDepartment ?? "";
                sheet.Cell(row, 6).Value = r.Portfolio ?? "";
                if (r.PaymentValueWithTax.HasValue)
                {
                    sheet.Cell(row, 7).Value = r.PaymentValueWithTax.Value;
                    sheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
                }
                if (r.PaymentDueDate.HasValue)
                {
                    sheet.Cell(row, 8).Value = r.PaymentDueDate.Value;
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
                "المديونيات_والمطالبات.xlsx");
        }
    }
}
