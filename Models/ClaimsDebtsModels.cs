using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExcelDashboard.Models
{
    /// <summary>
    /// فاتورة مديونية واحدة - مستوردة من ملف "جميع الفواتير" ("لوحة المديونيات"). حقل صريح لكل
    /// عمود بالملف مباشرة (لا RawJson - كل عمود له مقابل). إعادة رفع الملف تستبدل كل الفواتير
    /// القديمة بالكامل (بلا تراكم).
    /// </summary>
    public class DebtInvoiceRecord
    {
        public int Id { get; set; }

        [MaxLength(50)]
        public string? Scope { get; set; } // الأمانة/بلدى

        [MaxLength(150)]
        public string? Entity { get; set; } // الجهة

        [MaxLength(150)]
        public string? Classification { get; set; } // تصنيف البند

        [MaxLength(250)]
        public string? Category { get; set; } // البند

        [MaxLength(50)]
        public string? ItemNumber { get; set; } // رقم البند

        [MaxLength(50)]
        public string? PaymentStatus { get; set; } // حالة السداد

        [MaxLength(100)]
        public string? ItemType { get; set; } // نوع البند

        public decimal? Value { get; set; } // القيمة

        public DateTime? CreatedDate { get; set; } // تاريخ الإنشاء
        public DateTime? PaymentDate { get; set; } // تاريخ الدفع
        public DateTime? SettlementDate { get; set; } // تاريخ التسوية
        public DateTime? CancellationDate { get; set; } // تاريخ الإلغاء

        [MaxLength(250)]
        public string? CustomerName { get; set; } // إسم العميل

        [MaxLength(100)]
        public string? InvestorNumber { get; set; } // رقم المستثمر

        [MaxLength(250)]
        public string? InvestorName { get; set; } // إسم المستثمر

        [MaxLength(100)]
        public string? ContractNumber { get; set; } // رقم العقد - مفتاح الربط مع المطالبات

        [MaxLength(100)]
        public string? ContractIdRaw { get; set; } // CONTRACT_ID

        public DateTime? ContractDate { get; set; } // تاريخ العقد

        [MaxLength(100)]
        public string? InvoiceNumber { get; set; } // رقم الفاتوره - مفتاح الربط مع المطالبات

        [MaxLength(500)]
        public string? InvoiceDescription { get; set; } // وصف الفاتوره

        [MaxLength(100)]
        public string? RulingNumber { get; set; } // رقم الحكم

        [MaxLength(100)]
        public string? ExecutionNumber { get; set; } // رقم التنفيذ

        [MaxLength(100)]
        public string? PaymentEntryNumber { get; set; } // رقم إيفاء

        [MaxLength(100)]
        public string? BatchStatusAr { get; set; } // BATCH_STATUS_AR

        [MaxLength(100)]
        public string? BatchStatusEn { get; set; } // BATCH_STATUS_EN

        [MaxLength(50)]
        public string? BatchNumber { get; set; } // رقم الدفعة

        // عدد الأيام المنقضية من اصدار الفاتورة - أساس رسم "الفواتير المتجاوزة 60 يوم"
        public int? DaysElapsed { get; set; }

        [MaxLength(100)]
        public string? ContractType { get; set; } // نوع العقد

        [MaxLength(100)]
        public string? ContractStatus { get; set; } // حالة العقد

        [MaxLength(20)]
        public string? IsLegalExecution { get; set; } // هل الفاتورة ضمن التنفيذ القضائي (نعم/لا)

        [MaxLength(250)]
        public string? LegalExecutionClaimStatus { get; set; } // حالة المطالبة في التنفيذ القضائي

        [MaxLength(500)]
        public string? StatementUpdate { get; set; } // تحديث الإفادة (الفواتير)

        [MaxLength(250)]
        public string? PreviousPortfolio { get; set; } // المحفظة السابقة

        [MaxLength(250)]
        public string? GeneralDepartment { get; set; } // الإدارة العامة

        [MaxLength(250)]
        public string? Portfolio { get; set; } // المحفظة

        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    }
    //جدول المديونيات لكل البنود
         public class DebtforallRecord
    {
        public int Id { get; set; }

        [MaxLength(50)]
        public string? Scope { get; set; } // الأمانة/بلدى

        [MaxLength(150)]
        public string? Entity { get; set; } // الجهة

        [MaxLength(150)]
        public string? Classification { get; set; } // تصنيف البند

        [MaxLength(250)]
        public string? Category { get; set; } // البند

        [MaxLength(50)]
        public string? ItemNumber { get; set; } // رقم البند

        [MaxLength(50)]
        public string? PaymentStatus { get; set; } // حالة السداد

        [MaxLength(100)]
        public string? ItemType { get; set; } // نوع البند

        public decimal? Value { get; set; } // القيمة

        public DateTime? CreatedDate { get; set; } // تاريخ الإنشاء
        public DateTime? PaymentDate { get; set; } // تاريخ الدفع

        [MaxLength(250)]
        public string? CustomerName { get; set; } // إسم العميل

        [MaxLength(100)]
        public string? InvestorNumber { get; set; } // رقم المستثمر

        [MaxLength(250)]
        public string? InvestorName { get; set; } // إسم المستثمر

        [MaxLength(100)]
        public string? ContractNumber { get; set; } // رقم العقد - مفتاح الربط مع المطالبات

        public DateTime? ContractDate { get; set; } // تاريخ العقد

        [MaxLength(100)]
        public string? InvoiceNumber { get; set; } // رقم الفاتوره

        [MaxLength(500)]
        public string? InvoiceDescription { get; set; } // وصف الفاتوره

        [MaxLength(100)]
        public string? RulingNumber { get; set; } // رقم الحكم

        [MaxLength(100)]
        public string? ExecutionNumber { get; set; } // رقم التنفيذ

        [MaxLength(100)]
        public string? PaymentEntryNumber { get; set; } // رقم إيفاء

        [MaxLength(100)]
        public string? BatchStatusAr { get; set; } // BATCH_STATUS_AR

        [MaxLength(100)]
        public string? BatchStatusEn { get; set; } // BATCH_STATUS_EN

        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    }




































    /// <summary>
    /// مطالبة تنفيذية واحدة - مستوردة من ملف "تقرير التنفيذ القضائي" ("لوحة المطالبات
    /// التنفيذية"). حقل صريح لكل عمود بالملف مباشرة. إعادة رفع الملف تستبدل كل المطالبات
    /// القديمة بالكامل (بلا تراكم).
    /// </summary>
    public class ClaimExecutionRecord
    {
        public int Id { get; set; }

        [MaxLength(150)]
        public string? Amanah { get; set; } // الأمانة

        [MaxLength(150)]
        public string? Baladiyah { get; set; } // البلدية

        [MaxLength(250)]
        public string? InvestorName { get; set; } // أسم المستثمر

        [MaxLength(100)]
        public string? OpportunityNumber { get; set; } // رقم الفرصة - حقل بحث مباشر

        [MaxLength(500)]
        public string? OpportunityDescription { get; set; } // وصف الفرصة

        [MaxLength(100)]
        public string? OpportunityType { get; set; } // نوع الفرصة

        [MaxLength(100)]
        public string? ContractType { get; set; } // نوع العقد

        [MaxLength(150)]
        public string? ContractStatus { get; set; } // حالة العقد

        [MaxLength(100)]
        public string? ContractNumber { get; set; } // رقم العقد - مفتاح الربط مع المديونيات، حقل بحث مباشر

        [MaxLength(150)]
        public string? ContractSource { get; set; } // مصدر العقد

        [MaxLength(50)]
        public string? PaymentNumber { get; set; } // رقم الدفعه

        [MaxLength(50)]
        public string? PaymentStatus { get; set; } // حالة الدفعه

        public DateTime? PaymentDueDate { get; set; } // تاريخ إستحقاق الدفعه

        public decimal? PaymentValueWithTax { get; set; } // قيمة الدفعة شامله قيمة الضريبة المضافة
        public decimal? PaymentValueWithoutTax { get; set; } // قيمة الدفعة بدون الضريبة
        public decimal? TaxValue { get; set; } // قيمة الضريبة

        [MaxLength(100)]
        public string? InvoiceNumber { get; set; } // رقم الفاتورة - مفتاح الربط مع المديونيات، حقل بحث مباشر

        [MaxLength(500)]
        public string? ClaimStatusFull { get; set; } // حالة المطالبة

        [MaxLength(250)]
        public string? ClaimStatusShort { get; set; } // حالة المطالبة مختصرة - أساس رسم/كروت الحالة

        [MaxLength(500)]
        public string? CancellationReason { get; set; } // سبب الإلغاء

        [MaxLength(1000)]
        public string? CancellationNotes { get; set; } // ملاحظات الإلغاء

        public DateTime? ReturnedFromJudiciaryDate { get; set; } // تاريخ الإرجاع من قضايا

        // تاريخ الإرسال الى تنفيذ - الأساس الزمني لرسم "تطور المطالبات عبر الزمن"
        public DateTime? SentToExecutionDate { get; set; }

        public DateTime? CancellationDate { get; set; } // تاريخ الإلغاء

        [MaxLength(100)]
        public string? RequestNumber { get; set; } // رقم الطلب

        [MaxLength(100)]
        public string? FirstNoticeNumber { get; set; } // رقم الإشعار الأول
        public DateTime? FirstNoticeDate { get; set; } // تاريخ الإشعار الأول

        [MaxLength(100)]
        public string? SecondNoticeNumber { get; set; } // رقم الإشعار الثاني
        public DateTime? SecondNoticeDate { get; set; } // تاريخ الإشعار الثاني

        [MaxLength(100)]
        public string? PerformanceClaimNoticeNumber { get; set; } // رقم إشعار المطالبة بالأداء
        public DateTime? PerformanceClaimNoticeDate { get; set; } // تاريخ إشعار المطالبة بالأداء

        [MaxLength(1000)]
        public string? JudiciaryReturnReason { get; set; } // سبب الإعادة من نظام القضايا

        [MaxLength(250)]
        public string? CompanyName { get; set; } // إسم الشركه / الجهة

        [MaxLength(250)]
        public string? Portfolio { get; set; } // المحفظة

        [MaxLength(250)]
        public string? GeneralDepartment { get; set; } // الإدارة العامة

        [MaxLength(50)]
        public string? Period { get; set; } // الفترة (2022-2025 / 2026)

        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// سجل "عقد" واحد بصفحة "الطرح" - مستورد من ورقة "الطرح" بملف "ملف الطرح...xlsx".
    /// إعادة رفع الملف تستبدل كل العقود القديمة بالكامل (بلا تراكم).
    /// </summary>
    public class ContractRecord
    {
        public int Id { get; set; }

        [MaxLength(250)]
        public string? InvestorName { get; set; } // اسم المستثمر (إن وُجد بملف الطرح)

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

        public DateTime? AwardDate { get; set; } // الترسية / الإلغاء (عند كونه تاريخًا فعليًا لا نص إلغاء) - مرحلة "الترسية" برحلة الفرصة

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

    /// <summary>
    /// عقد واحد بصفحة "العقود التي تنتهي فترة سماحها خلال 3 أشهر" - مستورد من ملف
    /// "العقود.xlsx" (مختلف كليًا عن ملف عقود "الطرح" أعلاه رغم تشابه بعض الأسماء). حقل صريح
    /// لكل عمود بالملف مباشرة. إعادة رفع الملف تستبدل كل العقود القديمة بالكامل (بلا تراكم).
    /// </summary>
    public class ContractPeriodRecord
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string? OpportunityNumber { get; set; } // رقم الفرصة

        [MaxLength(400)]
        public string? OpportunityName { get; set; } // اسم الفرصة

        [MaxLength(1000)]
        public string? OpportunityDescription { get; set; } // وصف الفرصة

        [MaxLength(100)]
        public string? ContractNumber { get; set; } // رقم العقد (CN-xxxxx)

        [MaxLength(100)]
        public string? PaperContractNumber { get; set; } // رقم العقد الورقي

        [MaxLength(100)]
        public string? ContractType { get; set; } // نوع العقد (استثمار / تأجير مباشر)

        [MaxLength(100)]
        public string? ContractSource { get; set; } // مصدر العقد

        [MaxLength(150)]
        public string? ContractStatus { get; set; } // حالة العقد (تم الموافقة على العقد / ملحق / تمديد)

        public int? ContractDurationYears { get; set; } // مدة العقد

        public DateTime? ContractEndDate { get; set; } // تاريخ نهاية العقد
        public DateTime? SiteDeliveryDate { get; set; } // تاريخ تسليم الموقع

        [MaxLength(50)]
        public string? ContractValidityStatus { get; set; } // حالة سريان العقد

        public decimal? PrepPeriodPercent { get; set; } // نسبة فترة التجهيز والانشاء
        public int? PrepPeriodDurationDays { get; set; } // مدة فترة التجهيز بالأيام

        // نهاية فترة التجهيز - أساس رسم "العقود اللي شارفت على الانتهاء" (أحمر/أخضر)
        public DateTime? PrepPeriodEndDate { get; set; }

        public DateTime? PaymentDueStartDate { get; set; } // تاريخ بداية استحقاق الدفعة
        public DateTime? FirstPaymentAfterPrepDate { get; set; } // تاريخ أول دفعة بعد فترة التجهيز

        // حالة العقد (خارج فترة التجهيز - داخل فترة التجهيز - متعثر) - أساس الرسم المكدَّس
        [MaxLength(150)]
        public string? PrepPeriodStatus { get; set; }

        [MaxLength(250)]
        public string? GeneralDepartment { get; set; } // الادارة العامة

        [MaxLength(250)]
        public string? Portfolio { get; set; } // الادارة الفرعية

        // تاريخ التوقيع - أساس "تاريخ العقد" بدرج تفاصيل قوس "العقود اللي شارفت على الانتهاء"
        public DateTime? SigningDate { get; set; }

        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
        
        
    }

    /// <summary>
    /// سجل "عقد استثماري" واحد بصفحة "جميع العقود الاستثمارية" - مستورد من ملف "تقرير عقود"
    /// (66 عمودًا بالملف الخام، منها هذي الحقول المستخدَمة فعليًا بالصفحة). إعادة الرفع تستبدل
    /// كل العقود القديمة بالكامل (بلا تراكم).
    /// </summary>
    public class InvestmentContractRecord
    {
        public int Id { get; set; }

        [MaxLength(150)]
        public string? Amanah { get; set; } // الأمانة

        [MaxLength(150)]
        public string? Baladiyah { get; set; } // البلدية

        [MaxLength(100)]
        public string? OpportunityNumber { get; set; } // رقم الفرصة

        [MaxLength(400)]
        public string? OpportunityName { get; set; } // اسم الفرصة

        [MaxLength(250)]
        public string? InvestorName { get; set; } // اسم المستثمر

        [MaxLength(100)]
        public string? ContractNumber { get; set; } // رقم العقد

        [MaxLength(100)]
        public string? ContractType { get; set; } // نوع العقد

        [MaxLength(150)]
        public string? ContractStatus { get; set; } // حالة العقد

        public int? ContractDurationDays { get; set; } // مدة العقد (أيام)

        public decimal? AnnualValue { get; set; } // القيمة السنوية للعقد
        public decimal? AnnualValueWithTax { get; set; } // القيمة السنوية للعقد شاملة الضريبة

        [MaxLength(5)]
        public string? HasAnnualIncrease { get; set; } // هل يوجد زيادة سنوية (Y/N)

        public DateTime? ContractStartDate { get; set; } // تاريخ بداية العقد
        public DateTime? ContractEndDate { get; set; } // تاريخ نهاية العقد

        [MaxLength(50)]
        public string? ContractValidityStatus { get; set; } // حالة سريان العقد

        public int? TotalPaymentsCount { get; set; } // عدد الدفعات الكلي

        public int? DuePaymentsCount { get; set; } // عدد الدفعات المستحقة
        public decimal? DuePaymentsValue { get; set; } // قيمة الدفعات المستحقة

        public int? DuePaidCount { get; set; } // عدد الدفعات المستحقة المدفوعة
        public decimal? DuePaidValue { get; set; } // قيمة الدفعات المستحقة المدفوعة

        public int? DueUnpaidCount { get; set; } // عدد الدفعات المستحقة غير المدفوعة
        public decimal? DueUnpaidValue { get; set; } // قيمة الدفعات المستحقة غير المدفوعة

        public int? RemainingNotDueCount { get; set; } // عدد الدفعات المتبقية غير المستحقة
        public decimal? RemainingNotDueValue { get; set; } // قيمة الدفعات المتبقية غير المستحقة

        public int? StalledCount { get; set; } // المدفوعات المتعثرة (عدد)
        public decimal? StalledValue { get; set; } // قيمة المدفوعات المتعثرة

        public int? SentToExecutionCount { get; set; } // عدد الدفعات المرسلة إلى تنفيذ
        public decimal? SentToExecutionValue { get; set; } // قيمة الدفعات المرسلة إلى تنفيذ

        [MaxLength(250)]
        public string? GeneralDepartment { get; set; } // الإدارة العامة

        [MaxLength(250)]
        public string? Portfolio { get; set; } // المحفظة

        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// نشاط استثماري واحد - مستوردة من ورقة "الانشطة " بملف "الأنشطة الاستثمارية" (رأس الأعمدة
    /// بالصف 4 لا الصف 1، تُقرأ مباشرة عبر ClosedXML بدل IExcelDashboardService.AnalyzeFile).
    /// إعادة رفع الملف تستبدل كل السجلات القديمة بالكامل.
    /// </summary>
    public class InvestmentActivityRecord
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string? OpportunityNumber { get; set; } // رقم الفرصة

        [MaxLength(500)]
        public string? OpportunityName { get; set; } // اسم الفرصة

        [MaxLength(100)]
        public string? ContractNumber { get; set; } // رقم العقد (إلزامي)

        public DateTime? ContractDate { get; set; } // تاريخ العقد
        public int? ContractYear { get; set; } // سنة العقد
        public decimal? AnnualRent { get; set; } // الأجرة السنوية

        [MaxLength(250)]
        public string? Activity { get; set; } // الأنشطة

        [MaxLength(250)]
        public string? Sector { get; set; } // القطاعات

        [MaxLength(150)]
        public string? Region { get; set; } // المنطقة

        [MaxLength(100)]
        public string? ContractType { get; set; } // نوع العقد

        public decimal? X { get; set; } // X الإحداثي
        public decimal? Y { get; set; } // Y الإحداثي

        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    }



}


