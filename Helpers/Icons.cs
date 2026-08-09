using Microsoft.AspNetCore.Html;

namespace ExcelDashboard.Helpers
{
    /// <summary>
    /// أيقونات رسمية جاهزة من مكتبة Bootstrap Icons (احترافية وموحّدة)، تُستخدم بحجم صغير
    /// بجانب النصوص بدل الإيموجيات. كل أيقونة تَرِث لونها وحجمها من CSS.
    /// </summary>
    public static class Icons
    {
        private static readonly Dictionary<string, string> Map = new()
        {
            ["building"] = "bi-bank",              // الأمانة
            ["city"]     = "bi-buildings",         // البلدية
            ["wallet"]   = "bi-wallet2",           // إجمالي الإيرادات
            ["check"]    = "bi-check-circle",      // المحصّل
            ["alert"]    = "bi-exclamation-triangle", // غير المحصّل
            ["trending"] = "bi-graph-up-arrow",    // نسبة التحصيل / الاستثمار
            ["report"]   = "bi-clipboard-data",    // لوحة بند
            ["chart"]    = "bi-bar-chart",
            ["scale"]    = "bi-hammer",            // الجزاءات والغرامات
            ["card"]     = "bi-credit-card",       // الرسوم المالية
            ["receipt"]  = "bi-receipt",           // رسوم المبيعات
            ["document"] = "bi-file-earmark-text", // التراخيص
            ["tools"]    = "bi-tools",             // النظافة والصيانة
            ["factory"]  = "bi-building-gear",     // المرافق
            ["globe"]    = "bi-geo-alt",           // الأراضي
            ["briefcase"]= "bi-briefcase",         // الإدارات
            ["folder"]   = "bi-folder",            // افتراضي
            ["inbox"]    = "bi-inbox",             // لا بيانات
            ["lock"]     = "bi-lock",              // لا صلاحية
            ["upload"]   = "bi-cloud-arrow-up",    // رفع
            ["file"]     = "bi-file-earmark-excel",// ملف
            ["info"]     = "bi-info-circle",       // تلميح
            ["search"]   = "bi-search",            // تصفية
            ["print"]    = "bi-printer",           // طباعة
            ["pdf"]      = "bi-file-earmark-pdf",  // ملف PDF
            ["excel"]    = "bi-file-earmark-excel",// ملف Excel
            ["contract"] = "bi-file-earmark-ruled",// عقد
            ["back"]     = "bi-chevron-right",     // رجوع (RTL)
            ["arrow"]    = "bi-chevron-left"       // إجراء للأمام (RTL)
        };

        // رمز الريال السعودي الرسمي الجديد (2025) كـ SVG مضمّن يَرِث لون النص.
        private const string SarPaths =
            "<path d=\"M699.62,1113.02h0c-20.06,44.48-33.32,92.75-38.4,143.37l424.51-90.24c20.06-44.47,33.31-92.75,38.4-143.37l-424.51,90.24Z\"/>" +
            "<path d=\"M1085.73,895.8c20.06-44.47,33.32-92.75,38.4-143.37l-330.68,70.33v-135.2l292.27-62.11c20.06-44.47,33.32-92.75,38.4-143.37l-330.68,70.27V66.13c-50.67,28.45-95.67,66.32-132.25,110.99v403.35l-132.25,28.11V0c-50.67,28.44-95.67,66.32-132.25,110.99v525.69l-295.91,62.88c-20.06,44.47-33.33,92.75-38.42,143.37l334.33-71.05v170.26l-358.3,76.14c-20.06,44.47-33.32,92.75-38.4,143.37l375.04-79.7c30.53-6.35,56.77-24.4,73.83-49.24l68.78-101.97v-.02c7.14-10.55,11.3-23.27,11.3-36.97v-149.98l132.25-28.11v270.4l424.53-90.28Z\"/>";

        /// <summary>رمز الريال السعودي الرسمي الجديد (يَرِث لون النص).</summary>
        public static IHtmlContent Riyal(string cssClass = "sar-symbol")
        {
            var cls = string.IsNullOrEmpty(cssClass) ? "" : $" class=\"{cssClass}\"";
            return new HtmlString(
                $"<svg{cls} viewBox=\"0 0 1124.14 1256.39\" fill=\"currentColor\" xmlns=\"http://www.w3.org/2000/svg\" role=\"img\" aria-label=\"ريال سعودي\">{SarPaths}</svg>");
        }

        /// <summary>يُعيد أيقونة Bootstrap بالاسم المطلوب، مع إمكانية تمرير صنف CSS إضافي.</summary>
        public static IHtmlContent Svg(string name, string cssClass = "")
        {
            var key = Map.ContainsKey(name) ? name : "folder";
            var extra = string.IsNullOrEmpty(cssClass) ? "" : " " + cssClass;
            return new HtmlString($"<i class=\"bi {Map[key]}{extra}\"></i>");
        }

        /// <summary>يختار الأيقونة المناسبة لبند معيّن حسب الـ slug.</summary>
        public static string ForSection(string? slug)
        {
            slug ??= "";
            bool Has(string s) => slug.Contains(s, StringComparison.OrdinalIgnoreCase);

            if (Has("dept-commercial")) return "briefcase";
            if (Has("dept-development")) return "trending";
            if (Has("dept-services")) return "tools";
            if (Has("dept")) return "briefcase";
            if (Has("buildings")) return "building";
            if (Has("facilities")) return "factory";
            if (Has("land")) return "globe";
            if (Has("temporary-lease")) return "document";
            if (Has("investment")) return "trending";
            if (Has("penalties")) return "scale";
            if (Has("sales")) return "receipt";
            if (Has("fees")) return "card";
            if (Has("licenses")) return "document";
            if (Has("cleaning") || Has("maintenance")) return "tools";
            return "report";
        }
    }
}
