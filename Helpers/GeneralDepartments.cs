namespace ExcelDashboard.Helpers
{
    /// <summary>
    /// الإدارات العامة الثلاث - نفس النص الحرفي المستخدم فعليًا بعمود "الإدارة العامة" بملفات
    /// الإكسل المستوردة (ContractRecords/ClaimExecutionRecords/ContractPeriodRecords/
    /// InvestmentContractRecords) وعمود "الإدارة العامة" بـ DashboardRecords.DeptGeneral - تأكّدنا
    /// من التطابق الحرفي بالاستعلام المباشر على قاعدة البيانات الحية قبل إضافة هذا الملف.
    /// </summary>
    public static class GeneralDepartments
    {
        public const string Commercial = "الإدارة العامة لمحافظ الاستثمارات التجارية";
        public const string Development = "الإدارة العامة لمحافظ الاستثمارات التنموية";
        public const string Services = "الإدارة العامة لمحافظ الخدمات";

        public static readonly string[] All = { Commercial, Development, Services };
    }
}
