namespace ExcelDashboard.Helpers
{
    /// <summary>
    /// موظف الإدارة (مقيّد بإدارة عامة واحدة عبر AppUser.GeneralDepartment) لا يرى إلا 5 صفحات
    /// محددة بالنظام - كل شي ثاني (الرئيسية، بيانات البلديات، الجزاءات/الرسوم، الإدارة...)
    /// محجوب عنه بالكامل، مو بس مخفي من القائمة. تُستخدم هذي القائمة من مكانين: بوابة الوصول
    /// بـ Program.cs (تمنع فتح أي صفحة غير مسموحة برابط مباشر) و_PageNavSidebar (تُخفي روابط
    /// الصفحات الممنوعة). كل صف = (Controller, Action, القيمة المطلوبة لـ route value "scope"
    /// إن وُجد قيد - null يعني بلا قيد على scope).
    /// </summary>
    public static class RestrictedEmployeeAccess
    {
        public static readonly (string Controller, string Action, string? RequiredScope)[] AllowedActions =
        {
            // 1) الإدارات العامة (بند الاستثمار بالأمانة فقط - لا يوجد مفهوم إدارة عامة بالبلدية)
            ("Dashboard", "Scope", "Amanah"),
            ("Dashboard", "ScopeData", "Amanah"),
            ("Dashboard", "AmanahInvestmentData", null),
            ("Dashboard", "AmanahInvestmentExportData", null),
            ("Dashboard", "AmanahInvestmentExportExcel", null),

            // 2) المديونيات والمطالبات (لوحة المطالبات التنفيذية)
            ("ClaimsExecution", "Index", null),
            ("ClaimsExecution", "Data", null),
            ("ClaimsExecution", "ExportData", null),
            ("ClaimsExecution", "ExportExcel", null),

            // 3) الطرح
            ("Claims", "Followup", null),
            ("Claims", "TarhData", null),
            ("Claims", "TarhOpportunityOptions", null),
            ("Claims", "TarhOpportunityTimeline", null),
            ("Claims", "TarhExportData", null),
            ("Claims", "TarhExportExcel", null),
            ("Claims", "TarhEnvelopeOpenedData", null),

            // 4) جميع العقود الاستثمارية (الأمانة فقط)
            ("InvestmentContracts", "Index", "Amanah"),
            ("InvestmentContracts", "Data", "Amanah"),

            // 5) العقود التي تنتهي فترة سماحها خلال 3 أشهر
            ("Claims", "GracePeriodExpiring", null),
            ("Claims", "GracePeriodExpiringData", null),
            ("Claims", "GracePeriodContractEndingList", null),
            ("Claims", "GracePeriodPrepEndingList", null),
        };

        /// <summary>الصفحة اللي يدخل عليها الموظف المقيّد مباشرة بعد تسجيل الدخول (أول صفحة بالقائمة)</summary>
        public const string FirstAllowedPath = "/Dashboard/Scope/Amanah";

        public static bool IsAllowed(string? controller, string? action, string? scope)
        {
            foreach (var (c, a, requiredScope) in AllowedActions)
            {
                if (!string.Equals(c, controller, System.StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(a, action, System.StringComparison.OrdinalIgnoreCase)) continue;
                if (requiredScope != null && !string.Equals(requiredScope, scope, System.StringComparison.OrdinalIgnoreCase)) continue;
                return true;
            }
            return false;
        }
    }
}
