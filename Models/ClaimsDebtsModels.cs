using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExcelDashboard.Models
{
    /// <summary>
    /// سجل "مطالبة" واحد - مستورد من ملف Excel (صفحتا "المطالبات والمديونيات" و"متابعة الأداء").
    /// إعادة رفع الملف تستبدل كل المطالبات القديمة بالكامل (بلا تراكم)، بنفس أسلوب
    /// DashboardRecord في بقية النظام.
    /// </summary>
    public class ClaimRecord
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string? ClaimNumber { get; set; } // رقم المطالبة

        // رقم العقد المرتبط بالمطالبة - يربط المطالبة بمديونيّتها (نفس رقم العقد) لبناء رسوم
        // "حالة المديونيات حسب البند" و"عدد المطالبات لكل مديونية"
        [MaxLength(100)]
        public string? ContractNumber { get; set; }

        [MaxLength(150)]
        public string? Department { get; set; } // الإدارة/الجهة المسؤولة

        [MaxLength(150)]
        public string? Status { get; set; } // حالة المطالبة كما هي بالملف (منجزة، قيد المعالجة، متأخرة...)

        [MaxLength(250)]
        public string? ItemCategory { get; set; } // البند/تصنيف البند المرتبط بالمطالبة (إن وُجد)

        public decimal? Value { get; set; } // قيمة المطالبة

        public DateTime? ClaimDate { get; set; } // تاريخ المطالبة/الإنشاء - أساس حساب مدة المعالجة والتأخر

        public DateTime? ActionDate { get; set; } // تاريخ اتخاذ أول إجراء (إن وُجد عمود صريح بالملف)

        public DateTime? CompletionDate { get; set; } // تاريخ الإنجاز/الإغلاق - null يعني أنها لا تزال مفتوحة

        [Column(TypeName = "TEXT")]
        public string? RawJson { get; set; }

        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// سجل "مديونية" واحد - مستورد من ملف Excel. إعادة الرفع تستبدل كل المديونيات القديمة بالكامل.
    /// </summary>
    public class DebtRecord
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string? DebtNumber { get; set; } // رقم المديونية

        // رقم العقد المرتبط بالمديونية - نفس مفتاح الربط المستخدم بـ ClaimRecord.ContractNumber
        [MaxLength(100)]
        public string? ContractNumber { get; set; }

        [MaxLength(150)]
        public string? Department { get; set; } // الإدارة/الجهة المسؤولة

        [MaxLength(150)]
        public string? Status { get; set; } // حالة السداد كما هي بالملف (محصّلة، غير محصّلة...)

        [MaxLength(250)]
        public string? ItemCategory { get; set; } // البند/تصنيف البند المرتبط بالمديونية (إن وُجد)

        public decimal? Value { get; set; } // إجمالي قيمة المديونية

        public decimal? CollectedValue { get; set; } // الجزء المحصّل من المديونية (إن وُجد صراحة بالملف)

        public DateTime? DebtDate { get; set; } // تاريخ نشوء المديونية

        public DateTime? DueDate { get; set; } // تاريخ الاستحقاق

        public DateTime? CollectionDate { get; set; } // تاريخ التحصيل الفعلي (إن حُصّلت)

        [Column(TypeName = "TEXT")]
        public string? RawJson { get; set; }

        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// سجل "عقد" واحد بصفحة "الطرح" - مستورد من ورقة "الطرح" بملف "ملف الطرح...xlsx".
    /// إعادة رفع الملف تستبدل كل العقود القديمة بالكامل (بلا تراكم).
    /// </summary>
    public class ContractRecord
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string? OpportunityNumber { get; set; } // رقم الفرصة

        [MaxLength(400)]
        public string? OpportunityName { get; set; } // اسم الفرصة

        [MaxLength(150)]
        public string? Status { get; set; } // الحالة (حالة العقد) - "تم إبرام العقد" تعني مبرومة

        [MaxLength(100)]
        public string? ContractNumber { get; set; } // رقم العقد

        public DateTime? ContractDate { get; set; } // تاريخ العقد

        public int? ContractYear { get; set; } // سنة العقد

        public int? Quarter { get; set; } // الربع (1-4) - محسوب من تاريخ العقد وقت الاستيراد

        public DateTime? EnvelopeOpeningDate { get; set; } // فتح المظاريف - لرصد الفرص المتأخرة بلا عقد بعد 30 يومًا

        public decimal? AnnualRent { get; set; } // الأجرة السنوية - قيمة عقود الأبراج/الممرات/التأجير المباشر

        public decimal? ContractValue { get; set; } // قيمة العقد - قيمة عقود الاستثمار غالبًا

        [MaxLength(250)]
        public string? GeneralDepartment { get; set; } // الإدارة العامة (فارغ/"0" بالملف لعقود الأبراج/الممرات/التأجير المباشر)

        [MaxLength(250)]
        public string? Activity { get; set; } // الأنشطة (غير مستخدم حاليًا بصفحة الطرح)

        [MaxLength(250)]
        public string? SubPortfolio { get; set; } // المحفظة الفرعية - "البند الفرعي" بلغة صفحة الطرح

        [MaxLength(100)]
        public string? ContractType { get; set; } // نوع العقد (استثمار / الأبراج / الممرات / تأجير مباشر)

        [MaxLength(100)]
        public string? GracePeriod { get; set; } // فترة السماح

        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    }
}
