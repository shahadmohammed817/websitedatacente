namespace ExcelDashboard.Services
{
    /// <summary>
    /// ترتيب موحّد لعرض الجهات (الأمانة + كل البلديات) في كل مكان بالنظام (القائمة الجانبية
    /// بنطاق البلدية، وقائمة تصفية "مقارنة المستهدفات")، بدل وجود نسختين مختلفتين قد تتعارضا.
    /// الترتيب الرسمي المعتمد للبلديات مبني على نفس الأسماء بالضبط كما بملف "المستهدف الشهري
    /// للبلديات" المعتمد (تم التحقق من الأسماء السبعة عشر مطابقة صفًا بصف).
    /// </summary>
    public static class EntityOrderHelper
    {
        public static readonly string[] BaladiyahDisplayOrder =
        {
            "بلدية محافظة ينبع", "بلدية بدر", "بلدية الحناكية", "بلدية المهد", "بلدية وادي الفرع",
            "بلدية خيبر", "بلدية العيص", "بلدية ينبع النخل", "بلدية المسيجيد", "بلدية السويرقية",
            "بلدية النخيل", "بلدية الصلصلة", "بلدية الحسو", "بلدية العشاش", "بلدية سلسلة جهينة",
            "بلدية ثرب", "بلدية العلا"
        };

        /// <summary>
        /// يرتّب قائمة الجهات: الأمانة أولاً (أي قيمة تحتوي كلمة "أمانة")، ثم البلديات حسب
        /// الترتيب الرسمي أعلاه، ثم أي جهة غير معروفة أبجديًا بآخر القائمة. يزيل التكرار
        /// والمسافات الزائدة أولاً حتى لا تظهر نفس الجهة مرتين بفروقات تنسيق بسيطة.
        /// </summary>
        public static List<string> SortEntities(IEnumerable<string> entities)
        {
            var cleaned = entities
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .Distinct()
                .ToList();

            return cleaned
                .OrderBy(e => e.Contains("أمانة") ? 0 : 1)
                .ThenBy(e =>
                {
                    var idx = Array.IndexOf(BaladiyahDisplayOrder, e);
                    return idx >= 0 ? idx : int.MaxValue;
                })
                .ThenBy(e => e)
                .ToList();
        }
    }
}
