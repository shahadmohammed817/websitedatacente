using ExcelDashboard.Data;
using ExcelDashboard.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ===== MVC =====
builder.Services.AddControllersWithViews();

// ===== قاعدة البيانات (SQLite عبر Entity Framework Core) =====
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== الخدمات (Dependency Injection) =====
// ملاحظة: تم إلغاء صفحة تسجيل الدخول بالكامل - يدخل المستخدم مباشرة للصفحة الرئيسية
// ويرى كل بيانات الأمانة والبلدية دون أي قيود صلاحيات.
builder.Services.AddScoped<IExcelDashboardService, ExcelDashboard.Services.ExcelDashboardService>();
builder.Services.AddScoped<IDataImportService, DataImportService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IMonthlyTargetImportService, MonthlyTargetImportService>();
builder.Services.AddScoped<IFinanceTargetImportService, FinanceTargetImportService>();
builder.Services.AddScoped<IRevenueBaselineImportService, RevenueBaselineImportService>();
builder.Services.AddScoped<IDebtInvoiceImportService, DebtInvoiceImportService>();
builder.Services.AddScoped<IClaimExecutionImportService, ClaimExecutionImportService>();
builder.Services.AddScoped<IDebtsAnalyticsService, DebtsAnalyticsService>();
builder.Services.AddScoped<IClaimsExecutionAnalyticsService, ClaimsExecutionAnalyticsService>();
builder.Services.AddScoped<IContractImportService, ContractImportService>();
builder.Services.AddScoped<IAmanahInvestmentDashboardService, AmanahInvestmentDashboardService>();
builder.Services.AddScoped<IContractPeriodImportService, ContractPeriodImportService>();
builder.Services.AddScoped<IContractPeriodAnalyticsService, ContractPeriodAnalyticsService>();

ExcelDashboard.Helpers.AppMode.ReadOnly = builder.Configuration.GetValue<bool>("READONLY_MODE");

var app = builder.Build();

// ===== إنشاء قاعدة البيانات تلقائياً عند بدء التشغيل (تتضمن كل بيانات الـ Seed الثابتة) =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // ترقية خفيفة لقاعدة بيانات موجودة مسبقًا: EnsureCreated لا يُعدّل جدول DashboardRecords
    // إذا كانت القاعدة موجودة أصلاً (كما هو الحال بعد أي تحديث لاحق للنظام)، فنضيف الأعمدة
    // الجديدة يدويًا عبر ALTER TABLE فقط إن لم تكن موجودة، دون أي حذف أو فقدان للبيانات الحالية.
    var conn = db.Database.GetDbConnection();
    conn.Open();
    var existingCols = new HashSet<string>();
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "PRAGMA table_info(DashboardRecords);";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            existingCols.Add(reader.GetString(reader.GetOrdinal("name")));
        }
    }

    if (!existingCols.Contains("ContractNumber"))
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "ALTER TABLE DashboardRecords ADD COLUMN ContractNumber TEXT NULL;";
        cmd.ExecuteNonQuery();
    }

    if (!existingCols.Contains("Quarter"))
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "ALTER TABLE DashboardRecords ADD COLUMN Quarter TEXT NULL;";
        cmd.ExecuteNonQuery();
    }

    // أعمدة صفحة "البحث برقم العقد" (اسم العقد، رقم الفاتورة، تصنيف البند، تاريخ الإنشاء)
    if (!existingCols.Contains("ContractName"))
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "ALTER TABLE DashboardRecords ADD COLUMN ContractName TEXT NULL;";
        cmd.ExecuteNonQuery();
    }

    if (!existingCols.Contains("InvoiceNumber"))
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "ALTER TABLE DashboardRecords ADD COLUMN InvoiceNumber TEXT NULL;";
        cmd.ExecuteNonQuery();
    }

    if (!existingCols.Contains("Classification"))
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "ALTER TABLE DashboardRecords ADD COLUMN Classification TEXT NULL;";
        cmd.ExecuteNonQuery();
    }

    if (!existingCols.Contains("CreatedDate"))
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "ALTER TABLE DashboardRecords ADD COLUMN CreatedDate TEXT NULL;";
        cmd.ExecuteNonQuery();
    }

    // أعمدة صفحة "مقارنة المستهدفات" (نوع الإيراد + مستهدف وزارة المالية + مستهدف وزارة البلديات والإسكان)
    if (!existingCols.Contains("RevenueType"))
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "ALTER TABLE DashboardRecords ADD COLUMN RevenueType TEXT NULL;";
        cmd.ExecuteNonQuery();
    }

    if (!existingCols.Contains("TargetFinanceValue"))
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "ALTER TABLE DashboardRecords ADD COLUMN TargetFinanceValue REAL NULL;";
        cmd.ExecuteNonQuery();
    }

    if (!existingCols.Contains("TargetBaladiyahValue"))
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "ALTER TABLE DashboardRecords ADD COLUMN TargetBaladiyahValue REAL NULL;";
        cmd.ExecuteNonQuery();
    }

    // عمودا "الإدارة العامة"/"الإدارة الفرعية" لصفحة "بنود الأمانة" (بند الاستثمار)
    if (!existingCols.Contains("DeptGeneral"))
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "ALTER TABLE DashboardRecords ADD COLUMN DeptGeneral TEXT NULL;";
        cmd.ExecuteNonQuery();
    }

    if (!existingCols.Contains("DeptSub"))
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "ALTER TABLE DashboardRecords ADD COLUMN DeptSub TEXT NULL;";
        cmd.ExecuteNonQuery();
    }

    // جدول "المستهدف الشهري للبلديات" (رفع صفحة مقارنة المستهدفات) - قد لا يكون موجودًا في
    // قاعدة بيانات قديمة (EnsureCreated لا يُنشئ جداول جديدة إن كانت القاعدة موجودة أصلاً)
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS MonthlyBaladiyahTargets (
            Id INTEGER NOT NULL CONSTRAINT PK_MonthlyBaladiyahTargets PRIMARY KEY AUTOINCREMENT,
            Entity TEXT NOT NULL,
            MainItem TEXT NULL,
            RevenueType TEXT NOT NULL,
            M1 REAL NOT NULL, M2 REAL NOT NULL, M3 REAL NOT NULL, M4 REAL NOT NULL,
            M5 REAL NOT NULL, M6 REAL NOT NULL, M7 REAL NOT NULL, M8 REAL NOT NULL,
            M9 REAL NOT NULL, M10 REAL NOT NULL, M11 REAL NOT NULL, M12 REAL NOT NULL,
            AnnualTotal REAL NOT NULL,
            Collected2025 REAL NULL,
            UploadedAt TEXT NOT NULL
        );";
        cmd.ExecuteNonQuery();
    }
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "CREATE INDEX IF NOT EXISTS IX_MonthlyBaladiyahTargets_Entity_RevenueType ON MonthlyBaladiyahTargets (Entity, RevenueType);";
        cmd.ExecuteNonQuery();
    }

    // جدول "مستهدف وزارة المالية السنوي" (صفحة مستهدفات وزارة المالية) - قد لا يكون موجودًا
    // في قاعدة بيانات قديمة (EnsureCreated لا يُنشئ جداول جديدة إن كانت القاعدة موجودة أصلاً)
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS FinanceItemTargets (
            Id INTEGER NOT NULL CONSTRAINT PK_FinanceItemTargets PRIMARY KEY AUTOINCREMENT,
            Scope TEXT NOT NULL,
            MainItem TEXT NOT NULL,
            SubItem TEXT NULL,
            AnnualTarget REAL NOT NULL,
            UploadedAt TEXT NOT NULL
        );";
        cmd.ExecuteNonQuery();
    }
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "CREATE INDEX IF NOT EXISTS IX_FinanceItemTargets_Scope_MainItem ON FinanceItemTargets (Scope, MainItem);";
        cmd.ExecuteNonQuery();
    }

    // جدولا "المطالبات" و"المديونيات" (صفحتا "المطالبات والمديونيات" و"متابعة الأداء") - قد لا
    // يكونا موجودين في قاعدة بيانات قديمة (EnsureCreated لا يُنشئ جداول جديدة إن كانت القاعدة
    // موجودة أصلاً)
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS ClaimRecords (
            Id INTEGER NOT NULL CONSTRAINT PK_ClaimRecords PRIMARY KEY AUTOINCREMENT,
            ClaimNumber TEXT NULL,
            ContractNumber TEXT NULL,
            Department TEXT NULL,
            Status TEXT NULL,
            ItemCategory TEXT NULL,
            Value REAL NULL,
            ClaimDate TEXT NULL,
            ActionDate TEXT NULL,
            CompletionDate TEXT NULL,
            RawJson TEXT NULL,
            ImportedAt TEXT NOT NULL
        );";
        cmd.ExecuteNonQuery();
    }
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "CREATE INDEX IF NOT EXISTS IX_ClaimRecords_Department ON ClaimRecords (Department);";
        cmd.ExecuteNonQuery();
    }
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "CREATE INDEX IF NOT EXISTS IX_ClaimRecords_ClaimDate ON ClaimRecords (ClaimDate);";
        cmd.ExecuteNonQuery();
    }
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS DebtRecords (
            Id INTEGER NOT NULL CONSTRAINT PK_DebtRecords PRIMARY KEY AUTOINCREMENT,
            DebtNumber TEXT NULL,
            ContractNumber TEXT NULL,
            Department TEXT NULL,
            Status TEXT NULL,
            ItemCategory TEXT NULL,
            Value REAL NULL,
            CollectedValue REAL NULL,
            DebtDate TEXT NULL,
            DueDate TEXT NULL,
            CollectionDate TEXT NULL,
            RawJson TEXT NULL,
            ImportedAt TEXT NOT NULL
        );";
        cmd.ExecuteNonQuery();
    }
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "CREATE INDEX IF NOT EXISTS IX_DebtRecords_Department ON DebtRecords (Department);";
        cmd.ExecuteNonQuery();
    }
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "CREATE INDEX IF NOT EXISTS IX_DebtRecords_DebtDate ON DebtRecords (DebtDate);";
        cmd.ExecuteNonQuery();
    }

    // جدول "عقود الطرح" (صفحة "الطرح") - قد لا يكون موجودًا في قاعدة بيانات قديمة
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS ContractRecords (
            Id INTEGER NOT NULL CONSTRAINT PK_ContractRecords PRIMARY KEY AUTOINCREMENT,
            OpportunityNumber TEXT NULL,
            OpportunityName TEXT NULL,
            Status TEXT NULL,
            ContractNumber TEXT NULL,
            ContractDate TEXT NULL,
            ContractYear INTEGER NULL,
            Quarter INTEGER NULL,
            EnvelopeOpeningDate TEXT NULL,
            AwardDate TEXT NULL,
            AnnualRent REAL NULL,
            ContractValue REAL NULL,
            GeneralDepartment TEXT NULL,
            Activity TEXT NULL,
            SubPortfolio TEXT NULL,
            ContractType TEXT NULL,
            GracePeriod TEXT NULL,
            ImportedAt TEXT NOT NULL
        );";
        cmd.ExecuteNonQuery();
    }
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "CREATE INDEX IF NOT EXISTS IX_ContractRecords_GeneralDepartment ON ContractRecords (GeneralDepartment);";
        cmd.ExecuteNonQuery();
    }
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "CREATE INDEX IF NOT EXISTS IX_ContractRecords_ContractType ON ContractRecords (ContractType);";
        cmd.ExecuteNonQuery();
    }
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "CREATE INDEX IF NOT EXISTS IX_ContractRecords_Status ON ContractRecords (Status);";
        cmd.ExecuteNonQuery();
    }

    // جدول "المحصل الفعلي لسنة الأساس 2025" لكل تصنيف بند (بطاقتا "محصل 2025"/"نسبة نمو
    // الإيرادات" أعلى رسم "أداء الإيرادات" بالرئيسية) - قد لا يكون موجودًا في قاعدة بيانات قديمة
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS RevenueBaselineCollectedItems (
            Id INTEGER NOT NULL CONSTRAINT PK_RevenueBaselineCollectedItems PRIMARY KEY AUTOINCREMENT,
            Classification TEXT NOT NULL,
            CollectedValue REAL NOT NULL,
            Month INTEGER NOT NULL DEFAULT 0,
            UploadedAt TEXT NOT NULL
        );";
        cmd.ExecuteNonQuery();
    }
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "CREATE INDEX IF NOT EXISTS IX_RevenueBaselineCollectedItems_Classification ON RevenueBaselineCollectedItems (Classification);";
        cmd.ExecuteNonQuery();
    }

    // جدول "فواتير المديونيات" (صفحة "لوحة المديونيات") - قد لا يكون موجودًا في قاعدة بيانات قديمة
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS DebtInvoiceRecords (
            Id INTEGER NOT NULL CONSTRAINT PK_DebtInvoiceRecords PRIMARY KEY AUTOINCREMENT,
            Scope TEXT NULL,
            Entity TEXT NULL,
            Classification TEXT NULL,
            Category TEXT NULL,
            ItemNumber TEXT NULL,
            PaymentStatus TEXT NULL,
            ItemType TEXT NULL,
            Value REAL NULL,
            CreatedDate TEXT NULL,
            PaymentDate TEXT NULL,
            SettlementDate TEXT NULL,
            CancellationDate TEXT NULL,
            CustomerName TEXT NULL,
            InvestorNumber TEXT NULL,
            InvestorName TEXT NULL,
            ContractNumber TEXT NULL,
            ContractIdRaw TEXT NULL,
            ContractDate TEXT NULL,
            InvoiceNumber TEXT NULL,
            InvoiceDescription TEXT NULL,
            RulingNumber TEXT NULL,
            ExecutionNumber TEXT NULL,
            PaymentEntryNumber TEXT NULL,
            BatchStatusAr TEXT NULL,
            BatchStatusEn TEXT NULL,
            BatchNumber TEXT NULL,
            DaysElapsed INTEGER NULL,
            ContractType TEXT NULL,
            ContractStatus TEXT NULL,
            IsLegalExecution TEXT NULL,
            LegalExecutionClaimStatus TEXT NULL,
            StatementUpdate TEXT NULL,
            PreviousPortfolio TEXT NULL,
            GeneralDepartment TEXT NULL,
            Portfolio TEXT NULL,
            ImportedAt TEXT NOT NULL
        );";
        cmd.ExecuteNonQuery();
    }
    foreach (var col in new[] { "ContractNumber", "InvoiceNumber", "GeneralDepartment", "Portfolio", "PaymentStatus", "CreatedDate" })
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE INDEX IF NOT EXISTS IX_DebtInvoiceRecords_{col} ON DebtInvoiceRecords ({col});";
        cmd.ExecuteNonQuery();
    }

    // جدول "المطالبات التنفيذية" (صفحة "لوحة المطالبات التنفيذية") - قد لا يكون موجودًا في قاعدة بيانات قديمة
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS ClaimExecutionRecords (
            Id INTEGER NOT NULL CONSTRAINT PK_ClaimExecutionRecords PRIMARY KEY AUTOINCREMENT,
            Amanah TEXT NULL,
            Baladiyah TEXT NULL,
            InvestorName TEXT NULL,
            OpportunityNumber TEXT NULL,
            OpportunityDescription TEXT NULL,
            OpportunityType TEXT NULL,
            ContractType TEXT NULL,
            ContractStatus TEXT NULL,
            ContractNumber TEXT NULL,
            ContractSource TEXT NULL,
            PaymentNumber TEXT NULL,
            PaymentStatus TEXT NULL,
            PaymentDueDate TEXT NULL,
            PaymentValueWithTax REAL NULL,
            PaymentValueWithoutTax REAL NULL,
            TaxValue REAL NULL,
            InvoiceNumber TEXT NULL,
            ClaimStatusFull TEXT NULL,
            ClaimStatusShort TEXT NULL,
            CancellationReason TEXT NULL,
            CancellationNotes TEXT NULL,
            ReturnedFromJudiciaryDate TEXT NULL,
            SentToExecutionDate TEXT NULL,
            CancellationDate TEXT NULL,
            RequestNumber TEXT NULL,
            FirstNoticeNumber TEXT NULL,
            FirstNoticeDate TEXT NULL,
            SecondNoticeNumber TEXT NULL,
            SecondNoticeDate TEXT NULL,
            PerformanceClaimNoticeNumber TEXT NULL,
            PerformanceClaimNoticeDate TEXT NULL,
            JudiciaryReturnReason TEXT NULL,
            CompanyName TEXT NULL,
            Portfolio TEXT NULL,
            GeneralDepartment TEXT NULL,
            Period TEXT NULL,
            ImportedAt TEXT NOT NULL
        );";
        cmd.ExecuteNonQuery();
    }
    foreach (var col in new[] { "ContractNumber", "InvoiceNumber", "OpportunityNumber", "ClaimStatusShort" })
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE INDEX IF NOT EXISTS IX_ClaimExecutionRecords_{col} ON ClaimExecutionRecords ({col});";
        cmd.ExecuteNonQuery();
    }

    // جدول "العقود التي تنتهي فترة سماحها خلال 3 أشهر" - قد لا يكون موجودًا في قاعدة بيانات قديمة
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS ContractPeriodRecords (
            Id INTEGER NOT NULL CONSTRAINT PK_ContractPeriodRecords PRIMARY KEY AUTOINCREMENT,
            OpportunityNumber TEXT NULL,
            OpportunityName TEXT NULL,
            OpportunityDescription TEXT NULL,
            ContractNumber TEXT NULL,
            PaperContractNumber TEXT NULL,
            ContractType TEXT NULL,
            ContractSource TEXT NULL,
            ContractStatus TEXT NULL,
            ContractDurationYears INTEGER NULL,
            ContractEndDate TEXT NULL,
            SiteDeliveryDate TEXT NULL,
            ContractValidityStatus TEXT NULL,
            PrepPeriodPercent REAL NULL,
            PrepPeriodDurationDays INTEGER NULL,
            PrepPeriodEndDate TEXT NULL,
            PaymentDueStartDate TEXT NULL,
            FirstPaymentAfterPrepDate TEXT NULL,
            PrepPeriodStatus TEXT NULL,
            GeneralDepartment TEXT NULL,
            Portfolio TEXT NULL,
            SigningDate TEXT NULL,
            ImportedAt TEXT NOT NULL
        );";
        cmd.ExecuteNonQuery();
    }
    foreach (var col in new[] { "ContractNumber", "GeneralDepartment", "Portfolio", "ContractType", "PrepPeriodStatus" })
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE INDEX IF NOT EXISTS IX_ContractPeriodRecords_{col} ON ContractPeriodRecords ({col});";
        cmd.ExecuteNonQuery();
    }

    // عمود "البند" (ItemCategory) أُضيف لاحقًا لجدولَي ClaimRecords/DebtRecords - نضيفه يدويًا
    // إن كانت القاعدة أُنشئت قبل إضافته (بنفس أسلوب ترقية DashboardRecords أعلاه)
    void AddColumnIfMissing(string table, string column, string sqlType)
    {
        var cols = new HashSet<string>();
        using (var infoCmd = conn.CreateCommand())
        {
            infoCmd.CommandText = $"PRAGMA table_info({table});";
            using var infoReader = infoCmd.ExecuteReader();
            while (infoReader.Read()) cols.Add(infoReader.GetString(infoReader.GetOrdinal("name")));
        }
        if (cols.Contains(column)) return;

        using var alterCmd = conn.CreateCommand();
        alterCmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {sqlType};";
        alterCmd.ExecuteNonQuery();
    }

    AddColumnIfMissing("ContractRecords", "SubPortfolio", "TEXT NULL");
    AddColumnIfMissing("ContractRecords", "Quarter", "INTEGER NULL");
    AddColumnIfMissing("ContractRecords", "EnvelopeOpeningDate", "TEXT NULL");
    AddColumnIfMissing("ContractRecords", "AwardDate", "TEXT NULL");
    AddColumnIfMissing("ClaimRecords", "ItemCategory", "TEXT NULL");
    AddColumnIfMissing("DebtRecords", "ItemCategory", "TEXT NULL");
    AddColumnIfMissing("ClaimRecords", "ContractNumber", "TEXT NULL");
    AddColumnIfMissing("DebtRecords", "ContractNumber", "TEXT NULL");
    AddColumnIfMissing("RevenueBaselineCollectedItems", "Month", "INTEGER NOT NULL DEFAULT 0");
    AddColumnIfMissing("ContractPeriodRecords", "SigningDate", "TEXT NULL");
    AddColumnIfMissing("ClaimExecutionRecords", "GeneralDepartment", "TEXT NULL");
    AddColumnIfMissing("ClaimExecutionRecords", "Period", "TEXT NULL");
    AddColumnIfMissing("DashboardRecords", "ItemNumber", "TEXT NULL");
    AddColumnIfMissing("DashboardRecords", "Month", "INTEGER NULL");
    AddColumnIfMissing("DashboardRecords", "InvestmentType", "TEXT NULL");
    AddColumnIfMissing("DashboardRecords", "ContractStatus", "TEXT NULL");
    AddColumnIfMissing("DashboardRecords", "InvestorName", "TEXT NULL");
    AddColumnIfMissing("DashboardRecords", "PaymentNumber", "TEXT NULL");
    AddColumnIfMissing("Sections", "ItemNumbers", "TEXT NULL");
    // TargetSource يميّز صفوف "مستهدف المالية" عن صفوف "مستهدف البلديات والإسكان" الآن أنه صار
    // بإمكان جدول واحد (FinanceItemTargets) يحمل كلا المصدرين من نفس ملف "المستهدف السنوي" -
    // DEFAULT 'Finance' يُصنّف الصفوف الموجودة مسبقًا (كلها كانت من مصدر المالية) تلقائيًا
    AddColumnIfMissing("FinanceItemTargets", "TargetSource", "TEXT NOT NULL DEFAULT 'Finance'");

    // إعادة حساب "الربع"/"الشهر" لكل السجلات الموجودة مسبقًا بنفس منطق الاستيراد الجديد (تاريخ
    // الدفع للمحصّلة، تاريخ الإنشاء لغير المحصّلة) بدل قيمة "الربع" الخام القديمة من الملف، وتعبئة
    // عمود "الشهر" الجديد الذي كان فارغًا بالكامل قبل هذا التحديث - تُنفَّذ في كل إقلاع (استعلام
    // واحد رخيص) حتى تبقى صحيحة دون الحاجة لإعادة رفع أي ملف
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"
            UPDATE DashboardRecords
            SET
                Month = CAST(strftime('%m',
                    CASE WHEN COALESCE(CollectedValue, 0) > 0 THEN RecordDate ELSE CreatedDate END
                ) AS INTEGER),
                Quarter = 'Q' || CAST(
                    ((CAST(strftime('%m',
                        CASE WHEN COALESCE(CollectedValue, 0) > 0 THEN RecordDate ELSE CreatedDate END
                    ) AS INTEGER) - 1) / 3) + 1 AS TEXT);";
        cmd.ExecuteNonQuery();
    }

    // تصحيح ترتيب كلمات إدارة "التأجير المباشر والمؤقت" لتطابق النص الفعلي المستخدم بملفات
    // الإكسل ("التأجير المؤقت والمباشر")، حتى تنجح المطابقة التلقائية عند الاستيراد.
    // الشرط على النص القديم تحديدًا يمنع الكتابة فوق أي تعديل يدوي لاحق قام به المدير.
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "UPDATE Sections SET NameAr = 'التأجير المؤقت والمباشر' " +
                           "WHERE Id IN (308, 408, 508) AND NameAr = 'التأجير المباشر والمؤقت';";
        cmd.ExecuteNonQuery();
    }

    // بند رئيسي جديد "تأجير مؤقت": الملف الحقيقي المرفوع يستخدم "تأجير مؤقت" كقيمة مستقلة بعمود
    // "تصنيف البند" (وليس بندًا فرعيًا تحت "استثمار" كما كان مسجّلاً سابقًا). يُنشأ مرة واحدة فقط
    // إن لم يكن موجودًا، ثم يُعاد ربط البند الفرعي الحالي "تأجير العقارات لأغراض مؤقته" (Id=14
    // للأمانة، Id=112 للبلدية) ليصبح تابعًا له بدل "استثمار"، بدل إنشاء بند مكرر.
    void EnsureSection(int id, string nameAr, string nameEn, string slug, int scope, string icon, int sortOrder)
    {
        using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = $"SELECT COUNT(*) FROM Sections WHERE Id = {id};";
        var exists = Convert.ToInt64(checkCmd.ExecuteScalar()) > 0;
        if (exists) return;

        using var insertCmd = conn.CreateCommand();
        insertCmd.CommandText =
            "INSERT INTO Sections (Id, NameAr, NameEn, Slug, Scope, Icon, SortOrder, IsActive, IsDashboardEntry, ParentSectionId, ItemNumbers) " +
            $"VALUES ({id}, '{nameAr}', '{nameEn}', '{slug}', {scope}, '{icon}', {sortOrder}, 1, 0, NULL, NULL);";
        insertCmd.ExecuteNonQuery();
    }

    EnsureSection(6, "تأجير مؤقت", "Temporary Leasing", "temp-lease-amanah", scope: 1, icon: "🗓️", sortOrder: 6);
    EnsureSection(105, "تأجير مؤقت", "Temporary Leasing", "temp-lease-baladiyah", scope: 2, icon: "🗓️", sortOrder: 6);

    using (var reparentCmd = conn.CreateCommand())
    {
        reparentCmd.CommandText = "UPDATE Sections SET ParentSectionId = 6 WHERE Id = 14 AND ParentSectionId = 1;" +
                                   "UPDATE Sections SET ParentSectionId = 105 WHERE Id = 112 AND ParentSectionId = 100;";
        reparentCmd.ExecuteNonQuery();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
