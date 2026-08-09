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
}
