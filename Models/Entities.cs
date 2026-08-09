using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExcelDashboard.Models
{
    /// <summary>
    /// نطاق عمل المستخدم: الأمانة، البلدية، أو الكل (يرى الاثنين)
    /// </summary>
    public enum ScopeType
    {
        Amanah = 1,     // الأمانة
        Baladiyah = 2,  // البلدية
        All = 3         // الكل
    }

    /// <summary>
    /// دور المستخدم في النظام
    /// </summary>
    public enum UserRole
    {
        Admin = 1,      // المدير - له صلاحية رفع الملفات واستيراد البيانات
        Viewer = 2       // مستخدم عادي - يستعرض فقط حسب صلاحياته
    }

    /// <summary>
    /// جدول المستخدمين - يتم التحقق من الدخول بالرقم الوظيفي + البريد الإلكتروني
    /// </summary>
    public class AppUser
    {
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string EmployeeNumber { get; set; } = string.Empty; // الرقم الوظيفي

        [Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty; // البريد الإلكتروني الخاص بالأمانة

        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Viewer;

        /// <summary>
        /// النطاق الافتراضي للمستخدم بعد الدخول (الأمانة / البلدية / الكل)
        /// يحدد ما إذا كانت تظهر له صفحة الاختيار أو يوجه مباشرة
        /// </summary>
        public ScopeType DefaultScope { get; set; } = ScopeType.All;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        public ICollection<UserSectionPermission> Permissions { get; set; } = new List<UserSectionPermission>();
    }

    /// <summary>
    /// بند رئيسي ضمن الأمانة أو البلدية (مثل: الاستثمار، الجزاءات والغرامات، الرسوم...)
    /// </summary>
    public class Section
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string NameAr { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string NameEn { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Slug { get; set; } = string.Empty; // مستخدم في الـ Route مثل: investment, penalties

        public ScopeType Scope { get; set; } = ScopeType.Amanah; // ينتمي للأمانة أو البلدية

        [MaxLength(50)]
        public string? Icon { get; set; } // اسم أيقونة (emoji أو CSS class)

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        // إذا كان true: عند الضغط على البند يفتح مباشرة صفحة الداشبورد (حتى لو له إدارات فرعية)،
        // وتظهر الإدارات العامة/الفرعية كقائمة جانبية داخل الداشبورد نفسه بدل صفحة قائمة بنود وسيطة.
        public bool IsDashboardEntry { get; set; } = false;

        public int? ParentSectionId { get; set; } // للبنود الفرعية (مثل: استثمار / تأجير مؤقت تحت الاستثمار)
        public Section? ParentSection { get; set; }
        public ICollection<Section> SubSections { get; set; } = new List<Section>();

        /// <summary>
        /// أرقام البند (عمود "رقم البند" بالملف) المطابقة لهذا البند الفرعي تحديدًا، مفصولة بفواصل
        /// (قد يتعدد الرقم لنفس البند عبر ملفات مختلفة). تُستخدم كمطابقة أساسية وأكثر استقرارًا من
        /// اسم البند وقت الاستيراد (نص عمود "البند" قد يتغيّر بين رفعة وأخرى لنفس البند الفرعي
        /// فعليًا)، وتُتعلَّم تلقائيًا أول مرة يُطابَق فيها الصف بالاسم ثم تُحفَظ هنا للرفعات القادمة.
        /// </summary>
        [MaxLength(300)]
        public string? ItemNumbers { get; set; }

        public ICollection<UserSectionPermission> Permissions { get; set; } = new List<UserSectionPermission>();
        public ICollection<DashboardRecord> Records { get; set; } = new List<DashboardRecord>();
    }

    /// <summary>
    /// صلاحية وصول مستخدم معين لبند معين (يستخدم لو احتجنا صلاحيات دقيقة لكل بند مستقبلاً)
    /// إن لم يوجد سجل صلاحية صريح، يُعتمد على DefaultScope للمستخدم
    /// </summary>
    public class UserSectionPermission
    {
        public int Id { get; set; }

        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;

        public int SectionId { get; set; }
        public Section Section { get; set; } = null!;

        public bool CanView { get; set; } = true;
    }

    /// <summary>
    /// سجل بيانات تم استيراده من ملف Excel لبند معين (الاستثمار، الجزاءات، ...)
    /// تصميم مرن: قيم رقمية + نصية + تاريخية عامة بحيث يخدم جميع البنود المختلفة
    /// </summary>
    public class DashboardRecord
    {
        public int Id { get; set; }

        public int SectionId { get; set; }
        public Section Section { get; set; } = null!;

        [MaxLength(250)]
        public string? Category { get; set; } // الفئة / الصنف (عمود نصي رئيسي)

        [MaxLength(150)]
        public string? Entity { get; set; } // الجهة (أمانة المدينة / بلدية الحناكية / بلدية العلا ...)

        [MaxLength(100)]
        public string? ContractNumber { get; set; } // رقم العقد - يُستخدم بمربع البحث بالداشبورد

        [MaxLength(300)]
        public string? ContractName { get; set; } // اسم العقد (أو اسم المستثمر كاحتياطي إن لم يتوفر اسم صريح) - لصفحة البحث برقم العقد

        [MaxLength(150)]
        public string? InvoiceNumber { get; set; } // رقم الفاتورة - لصفحة البحث برقم العقد

        /// <summary>
        /// تصنيف البند (القيمة الخام من عمود "تصنيف البند" وقت الاستيراد، مثل "استثمار"، "رسوم"...)
        /// يُخزَّن مستقلاً عن شجرة البنود (Section) لأن السجل قد يُوجَّه لبند فرعي عميق بالشجرة،
        /// بينما نحتاج هنا التصنيف الرئيسي فقط لعرضه بسرعة بصفحة البحث برقم العقد.
        /// </summary>
        [MaxLength(150)]
        public string? Classification { get; set; }

        /// <summary>
        /// رقم البند (عمود "رقم البند" بالملف) - معرّف مستقر للبند الفرعي لا يتغيّر بين رفعات
        /// الملف حتى لو تغيّر نص عمود "البند" نفسه (مثل "تأجير مباني" مقابل "تأجير مباني تجارية").
        /// يُستخدم لمطابقة البند الفرعي الصحيح عند الاستيراد بدل الاعتماد على تطابق الاسم فقط.
        /// </summary>
        [MaxLength(50)]
        public string? ItemNumber { get; set; }

        [MaxLength(20)]
        public string? Quarter { get; set; } // الربع (Q1/Q2/Q3/Q4) - يُستخدم بقائمة التصفية بالداشبورد

        /// <summary>
        /// الإدارة العامة (عمود "الإدارة العامة" بالملف، مثل "الإدارة العامة لمحافظ الاستثمارات
        /// التجارية") - نص خام كما هو بالملف، يُستخدم لتفصيل صفحة "بنود الأمانة" لبند الاستثمار
        /// مباشرةً (بدل الاعتماد على شجرة البنود التي لا تعكس هذا التصنيف حاليًا)
        /// </summary>
        [MaxLength(250)]
        public string? DeptGeneral { get; set; }

        /// <summary>الإدارة الفرعية (عمود "الإدارة الفرعية" بالملف) - نص خام، تابع لـ DeptGeneral أعلاه</summary>
        [MaxLength(250)]
        public string? DeptSub { get; set; }

        public DateTime? RecordDate { get; set; } // التاريخ (للرسم الزمني - عادة "تاريخ الدفع")

        /// <summary>
        /// تاريخ الإنشاء الفعلي من الملف المستورد (عمود "تاريخ الإنشاء" بالضبط)، مستقل عن RecordDate
        /// الذي قد يُؤخذ من "تاريخ الدفع" لغرض الرسم البياني الشهري. يُستخدم بصفحة البحث برقم العقد.
        /// </summary>
        public DateTime? CreatedDate { get; set; }

        public decimal? TotalValue { get; set; }       // إجمالي الإيراد/القيمة
        public decimal? CollectedValue { get; set; }    // المحصّل
        public decimal? UncollectedValue { get; set; }  // غير المحصّل
        public decimal? TargetValue { get; set; }        // الهدف السنوي (للـ Gauge)

        /// <summary>
        /// نوع الإيراد (تصنيف المستهدف) - عمود "نوع الإيراد (تصنيف المستهدف)" بالملف،
        /// يُستخدم لتجميع المستهدفات السنوية بصفحة "مقارنة المستهدفات" لكل جهة/نوع إيراد
        /// </summary>
        [MaxLength(250)]
        public string? RevenueType { get; set; }

        /// <summary>
        /// المستهدف السنوي - وزارة المالية (عمود "المستهدف السنوي - وزارة المالية" بالملف، مع سنته)
        /// يُقارَن بصفحة "مقارنة المستهدفات" بإجمالي الإيرادات المحصّلة شهريًا
        /// </summary>
        public decimal? TargetFinanceValue { get; set; }

        /// <summary>
        /// المستهدف السنوي - وزارة البلديات والإسكان (عمود "المستهدف السنوي - وزارة البلديات
        /// والإسكان" بالملف، مع سنته) يُقارَن بصفحة "مقارنة المستهدفات" بإجمالي المفوتر شهريًا
        /// </summary>
        public decimal? TargetBaladiyahValue { get; set; }

        [MaxLength(50)]
        public string? ExtraLabel1 { get; set; }
        public decimal? ExtraValue1 { get; set; }

        [MaxLength(50)]
        public string? ExtraLabel2 { get; set; }
        public decimal? ExtraValue2 { get; set; }

        [Column(TypeName = "TEXT")]
        public string? RawJson { get; set; } // تخزين الصف الأصلي بالكامل كـ JSON للمرونة

        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

        public int ImportedByUserId { get; set; }
    }

    /// <summary>
    /// سجل استيراد ملف Excel - لتتبع من رفع الملف ومتى
    /// </summary>
    public class ImportLog
    {
        public int Id { get; set; }

        [Required, MaxLength(250)]
        public string FileName { get; set; } = string.Empty;

        public int SectionId { get; set; }
        public Section Section { get; set; } = null!;

        public int RowsImported { get; set; }

        public int ImportedByUserId { get; set; }
        public AppUser? ImportedByUser { get; set; }

        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// المستهدف الشهري لكل بلدية (مستورد من ملف "المستهدف الشهري للبلديات") - سطر واحد لكل
    /// (جهة + بند رئيسي + نوع إيراد)، يحمل قيمة المستهدف لكل شهر من 1 إلى 12 كما هي بالملف
    /// تمامًا (بلا افتراض توزيع خطي)، تُستخدم لبناء خط "المستهدف الشهري" الحقيقي بصفحة
    /// "مقارنة المستهدفات" بدل التوزيع الافتراضي (السنوي ÷ 12). إعادة رفع الملف تستبدل كل
    /// البيانات القديمة بالكامل (بلا تراكم).
    /// </summary>
    public class MonthlyBaladiyahTarget
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Entity { get; set; } = string.Empty; // الجهة (اسم البلدية كما بالملف بالضبط)

        [MaxLength(250)]
        public string? MainItem { get; set; } // البند الرئيسي (تصنيف عمود "البند الرئيسي")

        [Required, MaxLength(300)]
        public string RevenueType { get; set; } = string.Empty; // نوع الإيراد (عمود "البند")

        public decimal M1 { get; set; }
        public decimal M2 { get; set; }
        public decimal M3 { get; set; }
        public decimal M4 { get; set; }
        public decimal M5 { get; set; }
        public decimal M6 { get; set; }
        public decimal M7 { get; set; }
        public decimal M8 { get; set; }
        public decimal M9 { get; set; }
        public decimal M10 { get; set; }
        public decimal M11 { get; set; }
        public decimal M12 { get; set; }

        public decimal AnnualTotal { get; set; }
        public decimal? Collected2025 { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// مستهدف وزارة المالية السنوي (بلا تفصيل شهري) - من ملف "مستهدف المالية" الذي يقسّم
    /// المستهدف بحسب النطاق (الأمانة مقابل مجموع البلديات) ثم البند الرئيسي وبنده الفرعي.
    /// </summary>
    public class FinanceItemTarget
    {
        public int Id { get; set; }

        /// <summary>"Amanah" أو "Baladiyah" (مجموع كل البلديات كنطاق واحد، لا تفصيل لكل بلدية)</summary>
        [Required, MaxLength(20)]
        public string Scope { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string MainItem { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? SubItem { get; set; }

        public decimal AnnualTarget { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
