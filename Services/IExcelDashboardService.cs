using System.Collections.Generic;
using System.IO;
using ExcelDashboard.Models;

namespace ExcelDashboard.Services
{
    public interface IExcelDashboardService
    {
        /// <summary>
        /// يقرأ ملف Excel ديناميكيًا: يكتشف صف العناوين الحقيقي (حتى لو كان هناك صفوف عنوان/دمج قبله)،
        /// يكتشف نوع كل عمود، وينظف القيم، ثم يرجّع البيانات الخام كاملة (Columns + Rows) للواجهة.
        /// يقرأ الورقة الأولى فقط.
        /// </summary>
        AnalyzeResult AnalyzeFile(Stream fileStream);

        /// <summary>
        /// نفس منطق AnalyzeFile لكن لكل أوراق العمل (Sheets) بالملف دفعة واحدة - تُعاد فقط
        /// الأوراق التي تحتوي على بيانات صالحة، مع اسم كل ورقة.
        /// </summary>
        List<(string SheetName, AnalyzeResult Result)> AnalyzeAllSheets(Stream fileStream);
    }
}
