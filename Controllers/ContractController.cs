using ClosedXML.Excel;
using ExcelDashboard.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Controllers
{
    /// <summary>
    /// صفحة "البحث برقم العقد": يُدخل المستخدم رقم عقد فتُعرض كل الفواتير المرتبطة به
    /// (اسم العقد، رقم الفاتورة، القيمة، تاريخ الإنشاء، الجهة، تصنيف البند)، مع إمكانية
    /// تصدير النتائج كملف Excel أو طباعتها كـ PDF مباشرة من المتصفح.
    /// لا يوجد تسجيل دخول حالياً - الصفحة متاحة مباشرة كبقية صفحات النظام.
    /// </summary>
    public class ContractController : Controller
    {
        private readonly AppDbContext _db;

        public ContractController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>صفحة البحث نفسها (بدون بيانات مبدئية - البحث يتم عبر JavaScript)</summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// نقطة نهاية JSON: تُرجع كل سجلات (الفواتير) المطابقة لرقم عقد معيّن (بحث جزئي).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Search(string number)
        {
            if (string.IsNullOrWhiteSpace(number))
                return Json(new { success = false, message = "يرجى إدخال رقم العقد." });

            var results = await FindByContractAsync(number);

            if (results.Count == 0)
            {
                return Json(new
                {
                    success = false,
                    message = $"لم يتم العثور على أي بيانات لرقم العقد \"{number.Trim()}\"."
                });
            }

            return Json(new { success = true, results });
        }

        /// <summary>
        /// نقطة نهاية JSON: تُرجع كل سجلات (الفواتير) الخاصة بجميع العقود الموجودة (لخيار "طباعة كل العقود").
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchAll()
        {
            var results = await FindAllContractsAsync();

            if (results.Count == 0)
            {
                return Json(new { success = false, message = "لا توجد أي بيانات عقود مسجّلة حالياً." });
            }

            return Json(new { success = true, results });
        }

        /// <summary>
        /// تصدير نتائج البحث كملف Excel (.xlsx) عبر ClosedXML - نفس المكتبة المستخدمة أصلاً
        /// بقراءة ملفات الاستيراد بالنظام.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportExcel(string? number, bool all = false)
        {
            List<ContractRowResult> results;
            string term;

            if (all)
            {
                results = await FindAllContractsAsync();
                term = "جميع_العقود";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(number))
                    return BadRequest("يرجى إدخال رقم العقد.");

                results = await FindByContractAsync(number);
                term = number.Trim();
            }

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("بيانات العقد");
            sheet.RightToLeft = true;

            string[] headers =
            {
                "رقم العقد", "اسم العقد", "رقم الفاتورة", "القيمة (شامل الضريبة)", "تاريخ الإنشاء", "الجهة", "تصنيف البند"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0E6E61");
                cell.Style.Font.FontColor = XLColor.White;
            }

            int row = 2;
            foreach (var r in results)
            {
                sheet.Cell(row, 1).Value = r.ContractNumber ?? "";
                sheet.Cell(row, 2).Value = r.ContractName ?? "";
                sheet.Cell(row, 3).Value = r.InvoiceNumber ?? "";
                sheet.Cell(row, 4).Value = r.Value ?? 0;
                sheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                if (r.CreatedDate.HasValue)
                {
                    sheet.Cell(row, 5).Value = r.CreatedDate.Value;
                    sheet.Cell(row, 5).Style.DateFormat.Format = "yyyy-MM-dd";
                }
                sheet.Cell(row, 6).Value = r.Entity ?? "";
                sheet.Cell(row, 7).Value = r.Classification ?? "";
                row++;
            }

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var safeName = string.Join("_", term.Split(Path.GetInvalidFileNameChars()));
            var fileName = all ? $"{safeName}.xlsx" : $"بيانات_العقد_{safeName}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        private async Task<List<ContractRowResult>> FindByContractAsync(string number)
        {
            var term = number.Trim();

            return await _db.DashboardRecords
                .Where(r => r.ContractNumber != null && r.ContractNumber.Contains(term))
                .OrderByDescending(r => r.CreatedDate ?? r.RecordDate)
                .Select(r => new ContractRowResult
                {
                    ContractNumber = r.ContractNumber,
                    ContractName = r.ContractName,
                    InvoiceNumber = r.InvoiceNumber,
                    Value = r.TotalValue,
                    CreatedDate = r.CreatedDate ?? r.RecordDate,
                    Entity = r.Entity,
                    Classification = r.Classification
                })
                .ToListAsync();
        }

        /// <summary>
        /// يرجع كل السجلات التي لها رقم عقد (بدون تصفية)، لخيار "طباعة كل العقود".
        /// </summary>
        private async Task<List<ContractRowResult>> FindAllContractsAsync()
        {
            return await _db.DashboardRecords
                .Where(r => r.ContractNumber != null && r.ContractNumber != "")
                .OrderBy(r => r.ContractNumber)
                .ThenByDescending(r => r.CreatedDate ?? r.RecordDate)
                .Select(r => new ContractRowResult
                {
                    ContractNumber = r.ContractNumber,
                    ContractName = r.ContractName,
                    InvoiceNumber = r.InvoiceNumber,
                    Value = r.TotalValue,
                    CreatedDate = r.CreatedDate ?? r.RecordDate,
                    Entity = r.Entity,
                    Classification = r.Classification
                })
                .ToListAsync();
        }

        private class ContractRowResult
        {
            public string? ContractNumber { get; set; }
            public string? ContractName { get; set; }
            public string? InvoiceNumber { get; set; }
            public decimal? Value { get; set; }
            public DateTime? CreatedDate { get; set; }
            public string? Entity { get; set; }
            public string? Classification { get; set; }
        }
    }
}
