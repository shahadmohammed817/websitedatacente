using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ExcelDashboard.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<AppUser> Users => Set<AppUser>();
        public DbSet<Section> Sections => Set<Section>();
        public DbSet<UserSectionPermission> UserSectionPermissions => Set<UserSectionPermission>();
        public DbSet<DashboardRecord> DashboardRecords => Set<DashboardRecord>();
        public DbSet<ImportLog> ImportLogs => Set<ImportLog>();
        public DbSet<MonthlyBaladiyahTarget> MonthlyBaladiyahTargets => Set<MonthlyBaladiyahTarget>();
        public DbSet<FinanceItemTarget> FinanceItemTargets => Set<FinanceItemTarget>();
        public DbSet<RevenueBaselineCollectedItem> RevenueBaselineCollectedItems => Set<RevenueBaselineCollectedItem>();
        public DbSet<ContractRecord> ContractRecords => Set<ContractRecord>();
        public DbSet<DebtInvoiceRecord> DebtInvoiceRecords => Set<DebtInvoiceRecord>();
        public DbSet<DebtforallRecord> DebtforallRecords => Set<DebtforallRecord>();
        public DbSet<ClaimExecutionRecord> ClaimExecutionRecords => Set<ClaimExecutionRecord>();
        public DbSet<ContractPeriodRecord> ContractPeriodRecords => Set<ContractPeriodRecord>();
        public DbSet<InvestmentContractRecord> InvestmentContractRecords => Set<InvestmentContractRecord>();
        public DbSet<InvestmentActivityRecord> InvestmentActivityRecords => Set<InvestmentActivityRecord>();
        public DbSet<OpenRequestRecord> OpenRequestRecords => Set<OpenRequestRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // SQLite لا يدعم دوال التجميع (Sum/Avg) على النوع decimal. نحوّل كل الأعمدة
            // العشرية إلى double على مستوى التخزين فقط ليتمكن SQLite من التجميع خادميًا،
            // مع بقاء النوع decimal في الكود C#.
            var decimalToDouble = new ValueConverter<decimal, double>(v => (double)v, v => (decimal)v);
            var nullableDecimalToDouble = new ValueConverter<decimal?, double?>(
                v => v.HasValue ? (double?)(double)v.Value : null,
                v => v.HasValue ? (decimal?)(decimal)v.Value : null);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(decimal))
                        property.SetValueConverter(decimalToDouble);
                    else if (property.ClrType == typeof(decimal?))
                        property.SetValueConverter(nullableDecimalToDouble);
                }
            }

            modelBuilder.Entity<AppUser>(e =>
            {
                e.HasIndex(u => new { u.EmployeeNumber, u.Email }).IsUnique();
            });

            modelBuilder.Entity<Section>(e =>
            {
                e.HasIndex(s => s.Slug).IsUnique();

                e.HasOne(s => s.ParentSection)
                    .WithMany(s => s.SubSections)
                    .HasForeignKey(s => s.ParentSectionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserSectionPermission>(e =>
            {
                e.HasIndex(p => new { p.AppUserId, p.SectionId }).IsUnique();

                e.HasOne(p => p.AppUser)
                    .WithMany(u => u.Permissions)
                    .HasForeignKey(p => p.AppUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(p => p.Section)
                    .WithMany(s => s.Permissions)
                    .HasForeignKey(p => p.SectionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<MonthlyBaladiyahTarget>(e =>
            {
                e.HasIndex(t => new { t.Entity, t.RevenueType });
            });

            modelBuilder.Entity<FinanceItemTarget>(e =>
            {
                e.HasIndex(t => new { t.Scope, t.MainItem });
            });

            modelBuilder.Entity<ContractRecord>(e =>
            {
                e.HasIndex(r => r.GeneralDepartment);
                e.HasIndex(r => r.ContractType);
                e.HasIndex(r => r.Status);
            });

            modelBuilder.Entity<DebtInvoiceRecord>(e =>
            {
                e.HasIndex(r => r.ContractNumber);
                e.HasIndex(r => r.InvoiceNumber);
                e.HasIndex(r => r.GeneralDepartment);
                e.HasIndex(r => r.Portfolio);
                e.HasIndex(r => r.PaymentStatus);
                e.HasIndex(r => r.CreatedDate);
            });

            modelBuilder.Entity<ClaimExecutionRecord>(e =>
            {
                e.HasIndex(r => r.ContractNumber);
                e.HasIndex(r => r.InvoiceNumber);
                e.HasIndex(r => r.OpportunityNumber);
                e.HasIndex(r => r.ClaimStatusShort);
            });

            modelBuilder.Entity<DashboardRecord>(e =>
            {
                e.HasOne(r => r.Section)
                    .WithMany(s => s.Records)
                    .HasForeignKey(r => r.SectionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================================================================
            // ===== Seed: بنود الأمانة والبلدية - مبنية بالكامل من ملف "ترتيب_الموقع" =====
            // ==========================================================================
            // هيكل البيانات في الملف: عمود "المنطقة" (أمانة / البلديات) يحدد الانتماء (Scope)،
            // عمود "تصنيف البند" هو البند الرئيسي الذي يظهر في القائمة (استثمار، جزاءات، رسوم...)
            // وهو نفسه ما يُطابقه استيراد البيانات لاحقًا (ImportByClassificationAsync)،
            // وعمود "البند" هو التفاصيل الفرعية داخل كل تصنيف، وتظهر كبنود فرعية قابلة للدخول
            // مباشرة إلى لوحتها الخاصة (IsDashboardEntry = true) بنفس أسلوب "تأجير المباني" سابقًا.

            // ===== 1) بنود الأمانة الرئيسية (تصنيف البند) =====
            modelBuilder.Entity<Section>().HasData(
                new Section { Id = 1, NameAr = "استثمار", NameEn = "Investment", Slug = "investment", Scope = ScopeType.Amanah, Icon = "💼", SortOrder = 1, IsActive = true },
                new Section { Id = 2, NameAr = "جزاءات", NameEn = "Penalties & Fines", Slug = "penalties", Scope = ScopeType.Amanah, Icon = "⚖️", SortOrder = 2, IsActive = true },
                new Section { Id = 3, NameAr = "رسوم", NameEn = "Fees", Slug = "fees-amanah", Scope = ScopeType.Amanah, Icon = "🧮", SortOrder = 3, IsActive = true },
                new Section { Id = 4, NameAr = "رسوم المبيعات", NameEn = "Sales Fees", Slug = "sales-fees", Scope = ScopeType.Amanah, Icon = "🧾", SortOrder = 4, IsActive = true },
                new Section { Id = 5, NameAr = "إيرادات مختلفة", NameEn = "Miscellaneous Revenues", Slug = "misc-revenues-amanah", Scope = ScopeType.Amanah, Icon = "💰", SortOrder = 5, IsActive = true }
            );

            // ===== 2) البنود الفرعية (البند) تحت "استثمار" - الأمانة =====
            // "تأجير المباني" و"تأجير المرافق" و"ريع الأراضي" تفتح لوحتها + شجرة الإدارات
            // العامة/الفرعية كقائمة جانبية (كما كانت سابقًا)، أما "تأجير العقارات لأغراض مؤقته"
            // فبند فرعي عادي بلا شجرة إدارات.
            modelBuilder.Entity<Section>().HasData(
                new Section { Id = 11, NameAr = "تأجير المباني", NameEn = "Buildings Leasing", Slug = "am-inv-buildings-lease", Scope = ScopeType.Amanah, Icon = "🏢", SortOrder = 1, IsActive = true, ParentSectionId = 1, IsDashboardEntry = true },
                new Section { Id = 12, NameAr = "تأجير المرافق", NameEn = "Facilities Leasing", Slug = "am-inv-facilities-lease", Scope = ScopeType.Amanah, Icon = "🏭", SortOrder = 2, IsActive = true, ParentSectionId = 1, IsDashboardEntry = true },
                new Section { Id = 13, NameAr = "ريع الأراضي", NameEn = "Land Revenue", Slug = "am-inv-land-revenue", Scope = ScopeType.Amanah, Icon = "🌍", SortOrder = 3, IsActive = true, ParentSectionId = 1, IsDashboardEntry = true },
                new Section { Id = 14, NameAr = "تأجير العقارات لأغراض مؤقته", NameEn = "Temporary Property Leasing", Slug = "am-inv-temp-lease", Scope = ScopeType.Amanah, Icon = "🗓️", SortOrder = 4, IsActive = true, ParentSectionId = 1, IsDashboardEntry = true }
            );

            // ===== 3) البنود الفرعية تحت "جزاءات" - الأمانة =====
            modelBuilder.Entity<Section>().HasData(
                new Section { Id = 21, NameAr = "جزاءات وغرامات", NameEn = "Penalties & Fines", Slug = "am-pen-general", Scope = ScopeType.Amanah, Icon = "⚖️", SortOrder = 1, IsActive = true, ParentSectionId = 2, IsDashboardEntry = true },
                new Section { Id = 22, NameAr = "غرامات ومخالفات - المكاتب الهندسية", NameEn = "Fines - Engineering Offices", Slug = "am-pen-eng-offices", Scope = ScopeType.Amanah, Icon = "📐", SortOrder = 2, IsActive = true, ParentSectionId = 2, IsDashboardEntry = true },
                new Section { Id = 23, NameAr = "جزاءات وغرامات مفروضة بموجب أنظمة الجهة", NameEn = "Penalties Under Entity Regulations", Slug = "am-pen-regulations", Scope = ScopeType.Amanah, Icon = "📜", SortOrder = 3, IsActive = true, ParentSectionId = 2, IsDashboardEntry = true }
            );

            // ===== 4) البنود الفرعية تحت "رسوم" - الأمانة (14 بند) =====
            modelBuilder.Entity<Section>().HasData(
                new Section { Id = 31, NameAr = "اصدار التراخيص بتأسيس أو تشغيل ابراج الاتصالات", NameEn = "Telecom Towers Licenses", Slug = "am-fee-telecom-towers", Scope = ScopeType.Amanah, Icon = "📡", SortOrder = 1, IsActive = true, ParentSectionId = 3, IsDashboardEntry = true },
                new Section { Id = 32, NameAr = "اصدار التراخيص بتأسيس أو تشغيل الصراف الآلي", NameEn = "ATM Licenses", Slug = "am-fee-atm", Scope = ScopeType.Amanah, Icon = "🏧", SortOrder = 2, IsActive = true, ParentSectionId = 3, IsDashboardEntry = true },
                new Section { Id = 33, NameAr = "اصدار القرارات المساحية و فرز الاراضي و العقارات و الوحدات السكنية و تجزئتها", NameEn = "Land Survey & Subdivision Decisions", Slug = "am-fee-survey-subdivision", Scope = ScopeType.Amanah, Icon = "📏", SortOrder = 3, IsActive = true, ParentSectionId = 3, IsDashboardEntry = true },
                new Section { Id = 34, NameAr = "اصدار تراخيص استخدام املاك خاصة لاقامة فعاليات", NameEn = "Private Property Event Licenses", Slug = "am-fee-private-events", Scope = ScopeType.Amanah, Icon = "🎪", SortOrder = 4, IsActive = true, ParentSectionId = 3, IsDashboardEntry = true },
                new Section { Id = 35, NameAr = "اصدار تراخيص اشغال الارصفة العامة و الطرق", NameEn = "Sidewalks & Roads Occupancy Licenses", Slug = "am-fee-sidewalks-roads", Scope = ScopeType.Amanah, Icon = "🚧", SortOrder = 5, IsActive = true, ParentSectionId = 3, IsDashboardEntry = true },
                new Section { Id = 36, NameAr = "اصدار تراخيص المباني و الاسوار", NameEn = "Buildings & Fences Licenses", Slug = "am-fee-buildings-fences", Scope = ScopeType.Amanah, Icon = "🏗️", SortOrder = 6, IsActive = true, ParentSectionId = 3, IsDashboardEntry = true },
                new Section { Id = 37, NameAr = "اصدار تراخيص حفر الشوارع", NameEn = "Street Excavation Licenses", Slug = "am-fee-street-excavation", Scope = ScopeType.Amanah, Icon = "⛏️", SortOrder = 7, IsActive = true, ParentSectionId = 3, IsDashboardEntry = true },
                new Section { Id = 38, NameAr = "اصدار رخص الانشطة التجارية", NameEn = "Commercial Activity Licenses", Slug = "am-fee-commercial-activity", Scope = ScopeType.Amanah, Icon = "🛍️", SortOrder = 8, IsActive = true, ParentSectionId = 3, IsDashboardEntry = true },
                new Section { Id = 39, NameAr = "الشهادات الصحية )إصدار الشهادات الصحية(", NameEn = "Health Certificates", Slug = "am-fee-health-certs", Scope = ScopeType.Amanah, Icon = "🩺", SortOrder = 9, IsActive = true, ParentSectionId = 3, IsDashboardEntry = true },
                new Section { Id = 40, NameAr = "الكشفية )القيام بالمعاينة أو الكشفية(", NameEn = "Inspection Fees", Slug = "am-fee-inspection", Scope = ScopeType.Amanah, Icon = "🔍", SortOrder = 10, IsActive = true, ParentSectionId = 3, IsDashboardEntry = true },
                new Section { Id = 41, NameAr = "اللوحـات الإعـلانيـة", NameEn = "Advertising Billboards", Slug = "am-fee-billboards", Scope = ScopeType.Amanah, Icon = "🪧", SortOrder = 11, IsActive = true, ParentSectionId = 3, IsDashboardEntry = true },
                new Section { Id = 42, NameAr = "المقابل المالي للسماح للأنشطة التجارية بالعمل (24) ساعة", NameEn = "24-Hour Commercial Activity Fee", Slug = "am-fee-24h-activity", Scope = ScopeType.Amanah, Icon = "🕛", SortOrder = 12, IsActive = true, ParentSectionId = 3, IsDashboardEntry = true },
                new Section { Id = 43, NameAr = "جمع النفايات", NameEn = "Waste Collection", Slug = "am-fee-waste-collection", Scope = ScopeType.Amanah, Icon = "🗑️", SortOrder = 13, IsActive = true, ParentSectionId = 3, IsDashboardEntry = true },
                new Section { Id = 44, NameAr = "لوحات محلات", NameEn = "Shop Signage", Slug = "am-fee-shop-signage", Scope = ScopeType.Amanah, Icon = "🏪", SortOrder = 14, IsActive = true, ParentSectionId = 3, IsDashboardEntry = true }
            );

            // ===== 5) البنود الفرعية تحت "رسوم المبيعات" - الأمانة (7 بنود) =====
            modelBuilder.Entity<Section>().HasData(
                new Section { Id = 51, NameAr = "رسوم حفر الشوارع", NameEn = "Street Excavation Fees", Slug = "am-sales-street-excavation", Scope = ScopeType.Amanah, Icon = "⛏️", SortOrder = 1, IsActive = true, ParentSectionId = 4, IsDashboardEntry = true },
                new Section { Id = 52, NameAr = "رسوم رفع مساحى", NameEn = "Survey Removal Fees", Slug = "am-sales-survey-removal", Scope = ScopeType.Amanah, Icon = "📐", SortOrder = 2, IsActive = true, ParentSectionId = 4, IsDashboardEntry = true },
                new Section { Id = 53, NameAr = "رسوم كشفية", NameEn = "Inspection Fees", Slug = "am-sales-inspection", Scope = ScopeType.Amanah, Icon = "🔍", SortOrder = 3, IsActive = true, ParentSectionId = 4, IsDashboardEntry = true },
                new Section { Id = 54, NameAr = "فرز الأراضي", NameEn = "Land Subdivision", Slug = "am-sales-land-subdivision", Scope = ScopeType.Amanah, Icon = "🗺️", SortOrder = 4, IsActive = true, ParentSectionId = 4, IsDashboardEntry = true },
                new Section { Id = 55, NameAr = "اشغال مرافق الايواء", NameEn = "Shelter Facilities Occupancy", Slug = "am-sales-shelter-occupancy", Scope = ScopeType.Amanah, Icon = "🏕️", SortOrder = 5, IsActive = true, ParentSectionId = 4, IsDashboardEntry = true },
                new Section { Id = 56, NameAr = "رسم ترخيص نشاط تقديم منتجات التبغ", NameEn = "Tobacco Activity License Fee", Slug = "am-sales-tobacco-license", Scope = ScopeType.Amanah, Icon = "🚬", SortOrder = 6, IsActive = true, ParentSectionId = 4, IsDashboardEntry = true },
                new Section { Id = 57, NameAr = "رسم مبيعات محلات تقديم منتجات التبغ", NameEn = "Tobacco Shops Sales Fee", Slug = "am-sales-tobacco-shops", Scope = ScopeType.Amanah, Icon = "🏬", SortOrder = 7, IsActive = true, ParentSectionId = 4, IsDashboardEntry = true }
            );

            // ===== 6) البنود الفرعية تحت "إيرادات مختلفة" - الأمانة (7 بنود) =====
            modelBuilder.Entity<Section>().HasData(
                new Section { Id = 61, NameAr = "إيرادات متنوعة", NameEn = "Miscellaneous Revenues", Slug = "am-misc-general", Scope = ScopeType.Amanah, Icon = "💰", SortOrder = 1, IsActive = true, ParentSectionId = 5, IsDashboardEntry = true },
                new Section { Id = 62, NameAr = "فرق قيمة التعويض بين العقار والارض البديلة لصالح الامانة", NameEn = "Compensation Value Difference", Slug = "am-misc-compensation-diff", Scope = ScopeType.Amanah, Icon = "⚖️", SortOrder = 2, IsActive = true, ParentSectionId = 5, IsDashboardEntry = true },
                new Section { Id = 63, NameAr = "مبيعات اخرى", NameEn = "Other Sales", Slug = "am-misc-other-sales", Scope = ScopeType.Amanah, Icon = "🛒", SortOrder = 3, IsActive = true, ParentSectionId = 5, IsDashboardEntry = true },
                new Section { Id = 64, NameAr = "مبيعات أراضى ( زوائد )", NameEn = "Land Sales (Surplus)", Slug = "am-misc-land-surplus-sales", Scope = ScopeType.Amanah, Icon = "🌍", SortOrder = 4, IsActive = true, ParentSectionId = 5, IsDashboardEntry = true },
                new Section { Id = 65, NameAr = "مبيعات تيار كهربائي", NameEn = "Electricity Sales", Slug = "am-misc-electricity-sales", Scope = ScopeType.Amanah, Icon = "⚡", SortOrder = 5, IsActive = true, ParentSectionId = 5, IsDashboardEntry = true },
                new Section { Id = 66, NameAr = "إيرادات متنوعة غير مصنفة في مكان آخر", NameEn = "Unclassified Miscellaneous Revenues", Slug = "am-misc-unclassified", Scope = ScopeType.Amanah, Icon = "📦", SortOrder = 6, IsActive = true, ParentSectionId = 5, IsDashboardEntry = true },
                new Section { Id = 67, NameAr = "بيع الوثائق والأنظمة", NameEn = "Sale of Documents & Regulations", Slug = "am-misc-documents-sale", Scope = ScopeType.Amanah, Icon = "📄", SortOrder = 7, IsActive = true, ParentSectionId = 5, IsDashboardEntry = true }
            );

            // ===== 7) بنود البلدية الرئيسية (تصنيف البند) =====
            modelBuilder.Entity<Section>().HasData(
                new Section { Id = 100, NameAr = "استثمار", NameEn = "Investment", Slug = "investment-baladiyah", Scope = ScopeType.Baladiyah, Icon = "💼", SortOrder = 1, IsActive = true },
                new Section { Id = 101, NameAr = "جزاءات", NameEn = "Penalties & Fines", Slug = "penalties-baladiyah", Scope = ScopeType.Baladiyah, Icon = "⚖️", SortOrder = 2, IsActive = true },
                new Section { Id = 102, NameAr = "رسوم", NameEn = "Fees", Slug = "fees-baladiyah", Scope = ScopeType.Baladiyah, Icon = "🧮", SortOrder = 3, IsActive = true },
                new Section { Id = 103, NameAr = "رسوم المبيعات", NameEn = "Sales Fees", Slug = "sales-fees-baladiyah", Scope = ScopeType.Baladiyah, Icon = "🧾", SortOrder = 4, IsActive = true },
                new Section { Id = 104, NameAr = "إيرادات مختلفة", NameEn = "Miscellaneous Revenues", Slug = "misc-revenues-baladiyah", Scope = ScopeType.Baladiyah, Icon = "💰", SortOrder = 5, IsActive = true }
            );

            // ===== 8) البنود الفرعية تحت "استثمار" - البلدية (3 بنود) =====
            modelBuilder.Entity<Section>().HasData(
                new Section { Id = 111, NameAr = "ريع الأراضي", NameEn = "Land Revenue", Slug = "bl-inv-land-revenue", Scope = ScopeType.Baladiyah, Icon = "🌍", SortOrder = 1, IsActive = true, ParentSectionId = 100, IsDashboardEntry = true },
                new Section { Id = 112, NameAr = "تأجير العقارات لأغراض مؤقته", NameEn = "Temporary Property Leasing", Slug = "bl-inv-temp-lease", Scope = ScopeType.Baladiyah, Icon = "🗓️", SortOrder = 2, IsActive = true, ParentSectionId = 100, IsDashboardEntry = true },
                new Section { Id = 113, NameAr = "تأجير المباني", NameEn = "Buildings Leasing", Slug = "bl-inv-buildings-lease", Scope = ScopeType.Baladiyah, Icon = "🏢", SortOrder = 3, IsActive = true, ParentSectionId = 100, IsDashboardEntry = true }
            );

            // ===== 9) البنود الفرعية تحت "جزاءات" - البلدية (3 بنود) =====
            modelBuilder.Entity<Section>().HasData(
                new Section { Id = 121, NameAr = "جزاءات وغرامات مفروضة بموجب أنظمة الجهة", NameEn = "Penalties Under Entity Regulations", Slug = "bl-pen-regulations", Scope = ScopeType.Baladiyah, Icon = "📜", SortOrder = 1, IsActive = true, ParentSectionId = 101, IsDashboardEntry = true },
                new Section { Id = 122, NameAr = "غرامات وجزاءات", NameEn = "Fines & Penalties", Slug = "bl-pen-fines", Scope = ScopeType.Baladiyah, Icon = "⚖️", SortOrder = 2, IsActive = true, ParentSectionId = 101, IsDashboardEntry = true },
                new Section { Id = 123, NameAr = "جزاءات وغرامات", NameEn = "Penalties & Fines", Slug = "bl-pen-general", Scope = ScopeType.Baladiyah, Icon = "⚖️", SortOrder = 3, IsActive = true, ParentSectionId = 101, IsDashboardEntry = true }
            );

            // ===== 10) البنود الفرعية تحت "رسوم" - البلدية (14 بند) =====
            modelBuilder.Entity<Section>().HasData(
                new Section { Id = 131, NameAr = "اصدار التراخيص بتأسيس أو تشغيل ابراج الاتصالات", NameEn = "Telecom Towers Licenses", Slug = "bl-fee-telecom-towers", Scope = ScopeType.Baladiyah, Icon = "📡", SortOrder = 1, IsActive = true, ParentSectionId = 102, IsDashboardEntry = true },
                new Section { Id = 132, NameAr = "اصدار التراخيص بتأسيس أو تشغيل الصراف الآلي", NameEn = "ATM Licenses", Slug = "bl-fee-atm", Scope = ScopeType.Baladiyah, Icon = "🏧", SortOrder = 2, IsActive = true, ParentSectionId = 102, IsDashboardEntry = true },
                new Section { Id = 133, NameAr = "اصدار القرارات المساحية و فرز الاراضي و العقارات و الوحدات السكنية و تجزئتها", NameEn = "Land Survey & Subdivision Decisions", Slug = "bl-fee-survey-subdivision", Scope = ScopeType.Baladiyah, Icon = "📏", SortOrder = 3, IsActive = true, ParentSectionId = 102, IsDashboardEntry = true },
                new Section { Id = 134, NameAr = "اصدار تراخيص اشغال الارصفة العامة و الطرق", NameEn = "Sidewalks & Roads Occupancy Licenses", Slug = "bl-fee-sidewalks-roads", Scope = ScopeType.Baladiyah, Icon = "🚧", SortOrder = 4, IsActive = true, ParentSectionId = 102, IsDashboardEntry = true },
                new Section { Id = 135, NameAr = "اصدار تراخيص المباني و الاسوار", NameEn = "Buildings & Fences Licenses", Slug = "bl-fee-buildings-fences", Scope = ScopeType.Baladiyah, Icon = "🏗️", SortOrder = 5, IsActive = true, ParentSectionId = 102, IsDashboardEntry = true },
                new Section { Id = 136, NameAr = "اصدار تراخيص حفر الشوارع", NameEn = "Street Excavation Licenses", Slug = "bl-fee-street-excavation", Scope = ScopeType.Baladiyah, Icon = "⛏️", SortOrder = 6, IsActive = true, ParentSectionId = 102, IsDashboardEntry = true },
                new Section { Id = 137, NameAr = "اصدار رخص الانشطة التجارية", NameEn = "Commercial Activity Licenses", Slug = "bl-fee-commercial-activity", Scope = ScopeType.Baladiyah, Icon = "🛍️", SortOrder = 7, IsActive = true, ParentSectionId = 102, IsDashboardEntry = true },
                new Section { Id = 138, NameAr = "الشهادات الصحية )إصدار الشهادات الصحية(", NameEn = "Health Certificates", Slug = "bl-fee-health-certs", Scope = ScopeType.Baladiyah, Icon = "🩺", SortOrder = 8, IsActive = true, ParentSectionId = 102, IsDashboardEntry = true },
                new Section { Id = 139, NameAr = "الكشفية )القيام بالمعاينة أو الكشفية(", NameEn = "Inspection Fees", Slug = "bl-fee-inspection", Scope = ScopeType.Baladiyah, Icon = "🔍", SortOrder = 9, IsActive = true, ParentSectionId = 102, IsDashboardEntry = true },
                new Section { Id = 140, NameAr = "اللوحـات الإعـلانيـة", NameEn = "Advertising Billboards", Slug = "bl-fee-billboards", Scope = ScopeType.Baladiyah, Icon = "🪧", SortOrder = 10, IsActive = true, ParentSectionId = 102, IsDashboardEntry = true },
                new Section { Id = 141, NameAr = "المقابل المالي للسماح للأنشطة التجارية بالعمل (24) ساعة", NameEn = "24-Hour Commercial Activity Fee", Slug = "bl-fee-24h-activity", Scope = ScopeType.Baladiyah, Icon = "🕛", SortOrder = 11, IsActive = true, ParentSectionId = 102, IsDashboardEntry = true },
                new Section { Id = 142, NameAr = "جمع النفايات", NameEn = "Waste Collection", Slug = "bl-fee-waste-collection", Scope = ScopeType.Baladiyah, Icon = "🗑️", SortOrder = 12, IsActive = true, ParentSectionId = 102, IsDashboardEntry = true },
                new Section { Id = 143, NameAr = "لوحات محلات", NameEn = "Shop Signage", Slug = "bl-fee-shop-signage", Scope = ScopeType.Baladiyah, Icon = "🏪", SortOrder = 13, IsActive = true, ParentSectionId = 102, IsDashboardEntry = true },
                new Section { Id = 144, NameAr = "اصدار تراخيص استخدام املاك خاصة لاقامة فعاليات", NameEn = "Private Property Event Licenses", Slug = "bl-fee-private-events", Scope = ScopeType.Baladiyah, Icon = "🎪", SortOrder = 14, IsActive = true, ParentSectionId = 102, IsDashboardEntry = true }
            );

            // ===== 11) البنود الفرعية تحت "رسوم المبيعات" - البلدية (3 بنود) =====
            modelBuilder.Entity<Section>().HasData(
                new Section { Id = 151, NameAr = "اشغال مرافق الايواء", NameEn = "Shelter Facilities Occupancy", Slug = "bl-sales-shelter-occupancy", Scope = ScopeType.Baladiyah, Icon = "🏕️", SortOrder = 1, IsActive = true, ParentSectionId = 103, IsDashboardEntry = true },
                new Section { Id = 152, NameAr = "رسم مبيعات محلات تقديم منتجات التبغ", NameEn = "Tobacco Shops Sales Fee", Slug = "bl-sales-tobacco-shops", Scope = ScopeType.Baladiyah, Icon = "🏬", SortOrder = 2, IsActive = true, ParentSectionId = 103, IsDashboardEntry = true },
                new Section { Id = 153, NameAr = "رسم ترخيص نشاط تقديم منتجات التبغ", NameEn = "Tobacco Activity License Fee", Slug = "bl-sales-tobacco-license", Scope = ScopeType.Baladiyah, Icon = "🚬", SortOrder = 3, IsActive = true, ParentSectionId = 103, IsDashboardEntry = true }
            );

            // ===== 12) البنود الفرعية تحت "إيرادات مختلفة" - البلدية (3 بنود) =====
            modelBuilder.Entity<Section>().HasData(
                new Section { Id = 161, NameAr = "بيع الوثائق والأنظمة", NameEn = "Sale of Documents & Regulations", Slug = "bl-misc-documents-sale", Scope = ScopeType.Baladiyah, Icon = "📄", SortOrder = 1, IsActive = true, ParentSectionId = 104, IsDashboardEntry = true },
                new Section { Id = 162, NameAr = "إيرادات متنوعة", NameEn = "Miscellaneous Revenues", Slug = "bl-misc-general", Scope = ScopeType.Baladiyah, Icon = "💰", SortOrder = 2, IsActive = true, ParentSectionId = 104, IsDashboardEntry = true },
                new Section { Id = 163, NameAr = "إيرادات متنوعة غير مصنفة في مكان آخر", NameEn = "Unclassified Miscellaneous Revenues", Slug = "bl-misc-unclassified", Scope = ScopeType.Baladiyah, Icon = "📦", SortOrder = 3, IsActive = true, ParentSectionId = 104, IsDashboardEntry = true }
            );

            // ==========================================================================
            // ===== Seed: الإدارات العامة والإدارات الفرعية (شجرة الإدارات) =====
            // ==========================================================================
            // نفس الهيكل التنظيمي (3 إدارات عامة × 3 إدارات فرعية) يتكرر تحت كل من:
            // "تأجير المباني" (Id=11) و"تأجير المرافق" (Id=12) و"ريع الأراضي" (Id=13) بالأمانة،
            // بحيث لكل إدارة لوحة بيانات خاصة بها تظهر كقائمة جانبية داخل لوحة البند نفسه.
            foreach (var item in new[]
            {
                new { ParentId = 11, Prefix = "bldg", BaseId = 301 }, // تحت "تأجير المباني"
                new { ParentId = 12, Prefix = "fac",  BaseId = 401 }, // تحت "تأجير المرافق"
                new { ParentId = 13, Prefix = "land", BaseId = 501 }  // تحت "ريع الأراضي"
            })
            {
                int baseId = item.BaseId;

                // الإدارة العامة الأولى: محافظ الاستثمارات التجارية
                modelBuilder.Entity<Section>().HasData(
                    new Section { Id = baseId,     NameAr = "الإدارة العامة لمحافظ الاستثمارات التجارية", NameEn = "GD - Commercial Investments Portfolio", Slug = $"{item.Prefix}-dept-commercial", Scope = ScopeType.Amanah, Icon = "🏬", SortOrder = 1, IsActive = true, ParentSectionId = item.ParentId },
                    new Section { Id = baseId + 1, NameAr = "الاستثمارات النوعية", NameEn = "Specialized Investments", Slug = $"{item.Prefix}-dept-commercial-specialized", Scope = ScopeType.Amanah, Icon = "✨", SortOrder = 1, IsActive = true, ParentSectionId = baseId },
                    new Section { Id = baseId + 2, NameAr = "الاستثمارات التجارية", NameEn = "Commercial Investments", Slug = $"{item.Prefix}-dept-commercial-trade", Scope = ScopeType.Amanah, Icon = "🛍️", SortOrder = 2, IsActive = true, ParentSectionId = baseId },
                    new Section { Id = baseId + 3, NameAr = "الاستثمارات السياحية والترفيهية", NameEn = "Tourism & Entertainment Investments", Slug = $"{item.Prefix}-dept-commercial-tourism", Scope = ScopeType.Amanah, Icon = "🎡", SortOrder = 3, IsActive = true, ParentSectionId = baseId }
                );

                // الإدارة العامة الثانية: محافظ الاستثمارات  التنموية
                modelBuilder.Entity<Section>().HasData(
                    new Section { Id = baseId + 4, NameAr = "الإدارة العامة لمحافظ الاستثمارات التنموية", NameEn = "GD - Development Investments Portfolio", Slug = $"{item.Prefix}-dept-development", Scope = ScopeType.Amanah, Icon = "🏗️", SortOrder = 2, IsActive = true, ParentSectionId = item.ParentId },
                    new Section { Id = baseId + 5, NameAr = "الاستثمارات التعليمية والرياضية والمجتمعية", NameEn = "Education, Sports & Community Investments", Slug = $"{item.Prefix}-dept-development-education", Scope = ScopeType.Amanah, Icon = "🎓", SortOrder = 1, IsActive = true, ParentSectionId = baseId + 4 },
                    new Section { Id = baseId + 6, NameAr = "الاستثمارات الصحية", NameEn = "Health Investments", Slug = $"{item.Prefix}-dept-development-health", Scope = ScopeType.Amanah, Icon = "🏥", SortOrder = 2, IsActive = true, ParentSectionId = baseId + 4 },
                    new Section { Id = baseId + 7, NameAr = "التأجير المؤقت والمباشر", NameEn = "Direct & Temporary Lease", Slug = $"{item.Prefix}-dept-development-lease", Scope = ScopeType.Amanah, Icon = "📑", SortOrder = 3, IsActive = true, ParentSectionId = baseId + 4 }
                );

                // الإدارة العامة الثالثة: محافظ الخدمات
                modelBuilder.Entity<Section>().HasData(
                    new Section { Id = baseId + 8,  NameAr = "الإدارة العامة لمحافظ الخدمات", NameEn = "GD - Services Portfolio", Slug = $"{item.Prefix}-dept-services", Scope = ScopeType.Amanah, Icon = "🛠️", SortOrder = 3, IsActive = true, ParentSectionId = item.ParentId },
                    new Section { Id = baseId + 9,  NameAr = "المدينة الذكية", NameEn = "Smart City", Slug = $"{item.Prefix}-dept-services-smart", Scope = ScopeType.Amanah, Icon = "🏙️", SortOrder = 1, IsActive = true, ParentSectionId = baseId + 8 },
                    new Section { Id = baseId + 10, NameAr = "خدمات البيئة والطاقة", NameEn = "Environment & Energy Services", Slug = $"{item.Prefix}-dept-services-energy", Scope = ScopeType.Amanah, Icon = "🌿", SortOrder = 2, IsActive = true, ParentSectionId = baseId + 8 },
                    new Section { Id = baseId + 11, NameAr = "الخدمات اللوجستية والصناعية", NameEn = "Logistics & Industrial Services", Slug = $"{item.Prefix}-dept-services-logistics", Scope = ScopeType.Amanah, Icon = "🚚", SortOrder = 3, IsActive = true, ParentSectionId = baseId + 8 }
                );
            }

            // ===== Seed: مستخدمون تجريبيون =====
            modelBuilder.Entity<AppUser>().HasData(
                new AppUser
                {
                    Id = 1,
                    EmployeeNumber = "10001",
                    Email = "admin@madinah.gov.sa",
                    FullName = "مدير النظام",
                    Role = UserRole.Admin,
                    DefaultScope = ScopeType.All,
                    IsActive = true
                },
                new AppUser
                {
                    Id = 2,
                    EmployeeNumber = "10002",
                    Email = "amanah.user@madinah.gov.sa",
                    FullName = "مستخدم الأمانة",
                    Role = UserRole.Viewer,
                    DefaultScope = ScopeType.Amanah,
                    IsActive = true
                },
                new AppUser
                {
                    Id = 3,
                    EmployeeNumber = "10003",
                    Email = "baladiyah.user@madinah.gov.sa",
                    FullName = "مستخدم البلدية",
                    Role = UserRole.Viewer,
                    DefaultScope = ScopeType.Baladiyah,
                    IsActive = true
                },
                new AppUser
                {
                    Id = 4,
                    EmployeeNumber = "10004",
                    Email = "viewer.all@madinah.gov.sa",
                    FullName = "مستخدم عام",
                    Role = UserRole.Viewer,
                    DefaultScope = ScopeType.All,
                    IsActive = true
                }
            );
        }
    }
}





