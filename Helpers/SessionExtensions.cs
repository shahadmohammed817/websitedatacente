using ExcelDashboard.Models;
using Microsoft.AspNetCore.Http;

namespace ExcelDashboard.Helpers
{
    /// <summary>
    /// تخزين/قراءة هوية الموظف المُسجَّل بالجلسة (Session) بعد الدخول برقمه الوظيفي - لا كلمة
    /// مرور ولا Cookie Auth Scheme، مجرد تعريف بسيط يحدّد إدارته العامة لفرض الفلترة عليها.
    /// </summary>
    public static class SessionExtensions
    {
        private const string KeyEmployeeId = "EmployeeId";
        private const string KeyEmployeeNumber = "EmployeeNumber";
        private const string KeyFullName = "EmployeeFullName";
        private const string KeyGeneralDepartment = "EmployeeGeneralDepartment";
        private const string KeyRole = "EmployeeRole";

        public static void SignInEmployee(this ISession session, AppUser user)
        {
            session.SetInt32(KeyEmployeeId, user.Id);
            session.SetString(KeyEmployeeNumber, user.EmployeeNumber);
            session.SetString(KeyFullName, user.FullName);
            session.SetString(KeyGeneralDepartment, user.GeneralDepartment ?? "");
            session.SetString(KeyRole, user.Role.ToString());
        }

        public static bool IsSignedIn(this ISession session) => session.GetInt32(KeyEmployeeId).HasValue;

        public static int? GetEmployeeId(this ISession session) => session.GetInt32(KeyEmployeeId);

        public static string? GetEmployeeNumber(this ISession session) => session.GetString(KeyEmployeeNumber);

        public static string? GetEmployeeFullName(this ISession session) => session.GetString(KeyFullName);

        /// <summary>null/فارغ يعني بلا قيد (يرى كل الإدارات) - أي قيمة أخرى تُفرض على كل الاستعلامات المدعومة</summary>
        public static string? GetEmployeeDepartment(this ISession session)
        {
            var value = session.GetString(KeyGeneralDepartment);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        public static bool IsAdmin(this ISession session) =>
            session.GetString(KeyRole) == UserRole.Admin.ToString();
    }
}
