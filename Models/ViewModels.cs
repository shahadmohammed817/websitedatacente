using System.ComponentModel.DataAnnotations;
using ExcelDashboard.Models;

namespace ExcelDashboard.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "الرقم الوظيفي مطلوب")]
        [Display(Name = "الرقم الوظيفي")]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// صفحة اختيار النطاق بعد الدخول: الأمانة / البلدية / الكل
    /// </summary>
    public class ScopeSelectionViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool CanSeeAmanah { get; set; }
        public bool CanSeeBaladiyah { get; set; }

        public List<KpiSummaryViewModel> OverviewKpis { get; set; } = new();

        /// <summary>
        /// شجرة بنود الأمانة (تُستخدم في القائمة الجانبية للوحة البيانات التفاعلية)
        /// </summary>
        public List<SectionNodeViewModel> AmanahSections { get; set; } = new();

        /// <summary>
        /// شجرة بنود البلدية (تُستخدم في القائمة الجانبية للوحة البيانات التفاعلية)
        /// </summary>
        public List<SectionNodeViewModel> BaladiyahSections { get; set; } = new();
    }

    public class KpiSummaryViewModel
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = "c1";
    }

    /// <summary>
    /// عرض شجرة البنود (الأمانة أو البلدية) مع البنود الفرعية
    /// </summary>
    public class SectionListViewModel
    {
        public ScopeType Scope { get; set; }
        public List<SectionNodeViewModel> Sections { get; set; } = new();
        public bool IsAdmin { get; set; }

        /// <summary>
        /// معرّف البند الأب الحالي (null يعني أننا في المستوى الأول: بنود الأمانة/البلدية مباشرة)
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// اسم البند الأب الحالي (يُستخدم كعنوان للصفحة عند التنقّل لمستوى أعمق، مثل: "الاستثمار")
        /// </summary>
        public string? ParentNameAr { get; set; }
        public string? ParentNameEn { get; set; }

        /// <summary>
        /// سلسلة الأجداد من الجذر إلى الأب الحالي - لبناء شريط التنقّل (Breadcrumb)
        /// </summary>
        public List<SectionNodeViewModel> Breadcrumbs { get; set; } = new();
    }

    public class SectionNodeViewModel
    {
        public int Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public bool IsDashboardEntry { get; set; } = false;
        public List<SectionNodeViewModel> SubSections { get; set; } = new();
    }

    /// <summary>
    /// داشبورد النطاق الكامل (بنود الأمانة/البلدية): القائمة الجانبية تعرض
    /// مجموعات الإدارات العامة (مجمّعة بالاسم عبر كل البنود التي تتكرر تحتها)
    /// </summary>
    public class ScopeDashboardViewModel
    {
        public ScopeType Scope { get; set; }

        /// <summary>
        /// الإدارات العامة المجمّعة بالاسم (مثل: محافظ الاستثمارات التجارية/التنموية/الخدمات)
        /// </summary>
        public List<DeptGroupViewModel> DeptGroups { get; set; } = new();

        /// <summary>
        /// البنود العليا للنطاق — بديل للقائمة الجانبية عندما لا توجد إدارات عامة (كالبلدية)
        /// </summary>
        public List<SectionNodeViewModel> TopSections { get; set; } = new();

        /// <summary>
        /// أسماء الجهات (البلديات) التي لها بيانات مستوردة — تُعرض كقائمة جانبية
        /// في نطاق البلدية، وكل جهة تفتح لوحة بياناتها الخاصة
        /// </summary>
        public List<string> Entities { get; set; } = new();

        /// <summary>
        /// خيارات فلتر "الإدارة العامة/الإدارة الفرعية" لبند الاستثمار بصفحة "بنود الأمانة" -
        /// مبنية مباشرة من عمودي DeptGeneral/DeptSub الخام على DashboardRecord (نطاق الأمانة فقط)
        /// </summary>
        public List<AmanahDeptOption> AmanahDeptOptions { get; set; } = new();
    }

    /// <summary>إدارة عامة واحدة (باسمها الخام كما بالملف) مع أسماء إداراتها الفرعية المتاحة</summary>
    public class AmanahDeptOption
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Subs { get; set; } = new();
    }

    /// <summary>
    /// مجموعة إدارات تحمل نفس الاسم عبر أكثر من بند (تُجمع بياناتها معاً في لوحة واحدة)
    /// </summary>
    public class DeptGroupViewModel
    {
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;

        /// <summary>معرّفات كل النسخ المكررة لهذه الإدارة (تُجمع بياناتها معاً)</summary>
        public List<int> SectionIds { get; set; } = new();

        public List<DeptGroupViewModel> Children { get; set; } = new();
    }

    /// <summary>
    /// بيانات صفحة الـ Dashboard الخاصة ببند معين (تُرسل كـ JSON للرسوم البيانية في JS)
    /// </summary>
    public class SectionDashboardViewModel
    {
        public int SectionId { get; set; }
        public string SectionNameAr { get; set; } = string.Empty;
        public string SectionNameEn { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public bool IsAdmin { get; set; }
        public bool HasData { get; set; }

        /// <summary>
        /// شجرة الإدارات العامة والإدارات الفرعية التابعة لهذا البند (مثل: تأجير المباني)
        /// إن وُجدت، تُعرض قائمة جانبية للتنقل بينها دون مغادرة الصفحة
        /// </summary>
        public List<SectionNodeViewModel> SubSections { get; set; } = new();

        public bool HasChildren => SubSections.Any();
    }
}