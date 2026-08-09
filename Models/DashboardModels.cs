using System.Collections.Generic;

namespace ExcelDashboard.Models
{
    /// <summary>
    /// نوع العمود المكتشف ديناميكيًا
    /// </summary>
    public static class ColumnTypes
    {
        public const string Numeric = "numeric";
        public const string Text = "text";
        public const string Date = "date";
    }

    /// <summary>
    /// معلومات عمود واحد (الاسم + النوع المكتشف)
    /// </summary>
    public class ColumnMeta
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = ColumnTypes.Text;
    }

    /// <summary>
    /// النتيجة الكاملة التي تُرجع للواجهة بعد تحليل ملف Excel.
    /// لاحظ أنه لا يوجد أي تجميع/إحصاء مسبق هنا (Statistics/Charts) -
    /// كل ذلك يتم حسابه في الواجهة (JavaScript) من Rows مباشرة،
    /// لأن هذا ما يُمكّن الفلترة التفاعلية (Cross-Filtering) بين الرسوم دون أي طلب جديد للخادم.
    /// </summary>
    public class AnalyzeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public int TotalRows { get; set; }
        public bool Truncated { get; set; }

        public List<ColumnMeta> Columns { get; set; } = new();

        // كل عنصر هو صف كامل: المفتاح = اسم العمود، القيمة = double أو string أو null
        // (التاريخ يُرسل كنص بصيغة yyyy-MM-dd لسهولة التعامل معه في JavaScript)
        public List<Dictionary<string, object?>> Rows { get; set; } = new();

        // اسم العمود الذي يبدو أنه يمثل "هدف" (Goal/Target) إن وُجد، ليُستخدم في رسم Gauge
        public string? GoalColumnName { get; set; }
    }
}
