namespace ExcelDashboard.Helpers
{
    /// <summary>
    /// وضع "عرض فقط" (Read-Only) - يُفعَّل عبر متغير بيئة READONLY_MODE=true على النسخة
    /// المنشورة للفريق (بدون أزرار رفع ملفات)، بينما تبقى النسخة المحلية للمطوّرة كاملة
    /// الصلاحيات كالمعتاد (المتغيّر غير مضبوط محليًا = false افتراضيًا).
    /// </summary>
    public static class AppMode
    {
        public static bool ReadOnly { get; set; }
    }
}
