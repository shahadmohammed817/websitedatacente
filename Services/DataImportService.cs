using System.Globalization;
using System.Text.Json;
using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    public interface IDataImportService
    {
        Task<(bool Success, string Message, int RowsImported)> ImportAsync(
            AnalyzeResult analyzed, int sectionId, int userId, string fileName);

        /// <summary>
        /// يستورد ملف واحد ويوزّع صفوفه تلقائيًا على عدة بنود دفعة واحدة، بالاعتماد على عمود
        /// بالملف يحدد اسم البند/الإدارة لكل صف (بدل اختيار بند واحد يدويًا لكل الملف).
        /// </summary>
        Task<(bool Success, string Message, int RowsImported)> ImportDistributedAsync(
            AnalyzeResult analyzed, int userId, string fileName);

        /// <summary>
        /// وضع الرفع الوحيد المعتمد بالنظام: زر رفع واحد بدون اختيار بند.
        /// يقرأ عمود "تصنيف البند" تلقائيًا من الملف، ويطابق كل قيمة فيه باسم بند مسجّل بالنظام
        /// (مثل "استثمار" أو "تأجير مؤقت")، ثم يحدّث قاعدة البيانات بالكامل لكل بند تأثّر بالملف
        /// (حذف بياناته القديمة واستبدالها بالبيانات الجديدة دون تراكم عند إعادة الرفع).
        /// </summary>
        Task<(bool Success, string Message, int RowsImported)> ImportByClassificationAsync(
            AnalyzeResult analyzed, int userId, string fileName);
    }

    /// <summary>
    /// يحوّل نتيجة تحليل Excel (AnalyzeResult) إلى سجلات DashboardRecord مرتبطة ببند (Section) معين،
    /// عبر اكتشاف تقريبي لأعمدة: الفئة، الإجمالي، المحصّل، غير المحصّل، الهدف، والتاريخ.
    /// </summary>
    public class DataImportService : IDataImportService
    {
        private readonly AppDbContext _db;

        private static readonly string[] CollectedKeywords = { "محصل", "collected", "actual", "received" };
        private static readonly string[] UncollectedKeywords = { "غير محصل", "uncollect", "remain", "باقي", "متبق" };
        private static readonly string[] TotalKeywords = { "إجمالي", "total", "revenue", "إيراد" };
        private static readonly string[] TargetKeywords = { "هدف", "target", "goal", "مستهدف" };

        // كلمات مفتاحية تدل على أن العمود يمثل اسم البند/الإدارة (للتوزيع التلقائي)
        private static readonly string[] SectionColumnKeywords =
        {
            "بند", "قسم", "إدارة", "الإدارة", "section", "department", "dept"
        };

        // اسم العمود المعتمد رسميًا لتحديد البند تلقائيًا في وضع الرفع الوحيد
        private const string ClassificationColumnName = "تصنيف البند";
        private static readonly string[] ClassificationColumnKeywords = { "تصنيف البند", "تصنيف" };

        // عمود المنطقة (أمانة / بلدية) - يميّز بين البند بنفس الاسم عند الأمانة أو عند البلدية
        // (لمطابقة نطاق البند وقت الاستيراد فقط - مستقل تمامًا عن عمود "الأمانة/بلدى" أدناه رغم
        // تشابه الغرض الظاهري: المستخدمة أكّدت أنهما عمودان منفصلان فعليًا بالملف)
        private const string RegionColumnName = "المنطقة";
        private static readonly string[] RegionColumnKeywords = { "المنطقة", "النطاق" };

        // عمود "الأمانة/بلدى" - أول عمود بالملف حسب تأكيد المستخدمة، مستقل تمامًا عن عمود
        // "المنطقة" أعلاه - يُستخدم حصريًا لدونات "الأمانة/البلديات" بالصفحة الرئيسية
        private const string AmanahBaladiColumnName = "الأمانة/بلدى";

        // عمود الجهة (امانة منطقة المدينة المنورة / بلدية الحناكية / بلدية العلا ...)
        // يُخزَّن مع كل سجل حتى تُعرض لوحة مستقلة لكل بلدية
        private const string EntityColumnName = "الجهة";
        private static readonly string[] EntityColumnKeywords = { "الجهة", "الجهه", "البلدية", "entity" };

        private const string PreferredCategoryColumnName = "البند";
        private static readonly string[] CategoryColumnKeywords = { "البند", "نوع البند" };

        // عمود "نوع البند" (ملزم/غير ملزم) - مستقل تمامًا عن عمود "البند" أعلاه رغم تشابه الاسم
        // (مطابقة دقيقة فقط لتفادي أي التباس مع "البند")، يُستخدم بدونات "ملزم/غير ملزم" بالصفحة الرئيسية
        private const string ItemTypeColumnName = "نوع البند";

        // عمود "رقم البند" - معرّف مستقر للبند الفرعي، يُستخدم لمطابقة البند الفرعي الصحيح بدل
        // الاعتماد فقط على نص عمود "البند" (الذي قد يتغيّر بين رفعة وأخرى لنفس البند فعليًا)
        private const string ItemNumberColumnName = "رقم البند";
        private static readonly string[] ItemNumberColumnKeywords = { "رقم البند", "كود البند", "رمز البند" };

        private const string PreferredValueColumnName = "القيمة";
        // نضيف "الإجمالي/إجمالي" لأن بعض الكشوف تسمّي عمود المبلغ "الإجمالي" بدل "القيمة"،
        // وبدون ذلك كان النظام يقع على أول عمود رقمي (مثل "رقم العرض") ويحسبه كقيمة مالية بالخطأ.
        private static readonly string[] ValueColumnKeywords =
            { "القيمة", "قيمة", "المبلغ", "الإجمالي", "إجمالي", "اجمالي", "amount", "value" };

        private const string PreferredDateColumnName = "تاريخ الدفع"; // يُستخدم لرسم اتجاه الإيرادات الشهري
        private const string FallbackDateColumnName = "تاريخ الإنشاء"; // احتياطي إن لم يوجد عمود "تاريخ الدفع" بالملف

        // عمود رقم العقد - يُستخدم لاحقًا بمربع البحث التفاعلي بالداشبورد
        private const string ContractNumberColumnName = "رقم العقد";
        private static readonly string[] ContractNumberColumnKeywords = { "رقم العقد", "contract" };

        // عمود اسم العقد - يُستخدم بصفحة "البحث برقم العقد" (احتياطي: عمود "المستثمر" إن كان اسم
        // العقد فارغًا، لأن أغلب صفوف الاستثمار لا تملأ "اسم العقد" صراحة)
        private const string ContractNameColumnName = "اسم العقد";
        private static readonly string[] ContractNameColumnKeywords = { "اسم العقد", "contract name" };
        private const string InvestorColumnName = "المستثمر";
        private static readonly string[] InvestorColumnKeywords = { "المستثمر", "investor" };

        // نوع الاستثمار / حالة العقد / رقم الدفعة - أعمدة صفحة "الإدارات العامة" (بند الاستثمار
        // بالأمانة): دونات "نوع الاستثمار" وفلترها، رسم "حالة العقد"، وتصدير Excel/PDF
        private const string InvestmentTypeColumnName = "نوع الاستثمار";
        private static readonly string[] InvestmentTypeColumnKeywords = { "نوع الاستثمار" };
        private const string ContractStatusColumnName = "حالة العقد";
        private static readonly string[] ContractStatusColumnKeywords = { "حالة العقد" };
        // لا يوجد عمود اسمه "رقم الدفعة" حرفيًا بالملف الحالي - أقرب عمود بنفس المعنى هو "رقم
        // إيفاء" (رقم التسوية/الدفع) - نبحث عن كليهما احتياطًا لتغيّر اسم العمود مستقبلاً
        private static readonly string[] PaymentNumberColumnKeywords = { "رقم إيفاء", "رقم الدفعة", "رقم ايفاء" };

        // عمود رقم الفاتورة - يُستخدم بصفحة "البحث برقم العقد"
        private const string InvoiceNumberColumnName = "رقم الفاتوره";
        private static readonly string[] InvoiceNumberColumnKeywords = { "رقم الفاتوره", "رقم الفاتورة", "invoice" };

        // عمود تاريخ الإنشاء بالضبط (مستقل عن عمود التاريخ المستخدم بالرسم الشهري) - لصفحة
        // "البحث برقم العقد"
        private const string CreatedDateColumnName = "تاريخ الإنشاء";

        // عمودا الإدارة العامة/الفرعية - لتوجيه صفوف "استثمار" لمستوى الإدارات في القائمة الجانبية
        // (مثل "الإدارة العامة لمحافظ الاستثمارات التنموية" ثم "الاستثمارات الصحية" تحتها)
        private const string DeptGeneralColumnName = "الإدارة العامة";
        private static readonly string[] DeptGeneralColumnKeywords = { "الإدارة العامة" };
        private const string DeptSubColumnName = "الإدارة الفرعية";
        private static readonly string[] DeptSubColumnKeywords = { "الإدارة الفرعية" };

        private static readonly string[] PaymentStatusColumnKeywords = { "حالة السداد", "حالة الدفع", "السداد", "الدفع" };
        private static readonly string[] CollectedStatusValues = { "مسدد", "مسوى", "محصل", "مدفوع", "سدد" };

        // "مسدد/مسوى/محصلة/مدفوعة" = محصّل، أما أي حالة تحتوي "غير" (مثل "غير مسدد") فتُعتبر غير محصّلة
        internal static bool IsCollectedStatus(string? status) =>
            !string.IsNullOrWhiteSpace(status)
            && !status.Contains("غير")
            && CollectedStatusValues.Any(k => status.Contains(k));

        // عمود "نوع الإيراد (تصنيف المستهدف)" - لصفحة "مقارنة المستهدفات" (يُستخدم لتجميع
        // المستهدف السنوي لكل جهة/نوع إيراد دون تكراره لكل صف عند حساب إجمالي المستهدف)
        private static readonly string[] RevenueTypeColumnKeywords =
            { "نوع الإيراد", "نوع الايراد", "تصنيف المستهدف", "تصنيف الإيراد", "تصنيف الايراد", "نوع البند المستهدف" };

        // عمودا المستهدف السنوي حسب الجهة المشرفة (وزارة المالية / وزارة البلديات والإسكان) -
        // أسماء الأعمدة بالملف تنتهي بسنة الهدف (مثال: "...وزارة المالية 2026") فتُطابق تقريبيًا
        private static readonly string[] TargetFinanceColumnKeywords = { "المستهدف السنوي - وزارة المالية", "وزارة المالية" };
        private static readonly string[] TargetBaladiyahColumnKeywords = { "المستهدف السنوي - وزارة البلديات والإسكان", "وزارة البلديات", "البلديات والاسكان", "البلديات والإسكان" };

        public DataImportService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<(bool Success, string Message, int RowsImported)> ImportAsync(
            AnalyzeResult analyzed, int sectionId, int userId, string fileName)
        {
            var section = await _db.Sections.FindAsync(sectionId);
            if (section == null)
                return (false, "البند المحدد غير موجود.", 0);

            var numericCols = analyzed.Columns.Where(c => c.Type == ColumnTypes.Numeric).Select(c => c.Name).ToList();
            var textCols = analyzed.Columns.Where(c => c.Type == ColumnTypes.Text).Select(c => c.Name).ToList();
            var dateCols = analyzed.Columns.Where(c => c.Type == ColumnTypes.Date).Select(c => c.Name).ToList();

            string? totalCol = FindColumn(numericCols, TotalKeywords) ?? numericCols.FirstOrDefault();
            string? collectedCol = FindColumn(numericCols, CollectedKeywords);
            string? uncollectedCol = FindColumn(numericCols, UncollectedKeywords);
            string? targetCol = FindColumn(numericCols, TargetKeywords);
            string? categoryCol = textCols.FirstOrDefault();
            string? dateCol = dateCols.FirstOrDefault();

            var records = new List<DashboardRecord>();

            foreach (var row in analyzed.Rows)
            {
                var record = new DashboardRecord
                {
                    SectionId = sectionId,
                    ImportedByUserId = userId,
                    Category = categoryCol != null ? GetString(row, categoryCol) : null,
                    RecordDate = dateCol != null ? GetDate(row, dateCol) : null,
                    TotalValue = totalCol != null ? GetDecimal(row, totalCol) : null,
                    CollectedValue = collectedCol != null ? GetDecimal(row, collectedCol) : null,
                    UncollectedValue = uncollectedCol != null ? GetDecimal(row, uncollectedCol) : null,
                    TargetValue = targetCol != null ? GetDecimal(row, targetCol) : null,
                    RawJson = JsonSerializer.Serialize(row)
                };

                // إذا لم يتوفر عمود "غير محصّل" صريح، نحسبه من الفرق
                if (record.UncollectedValue == null && record.TotalValue.HasValue && record.CollectedValue.HasValue)
                {
                    record.UncollectedValue = record.TotalValue - record.CollectedValue;
                }

                records.Add(record);
            }

            await _db.DashboardRecords.AddRangeAsync(records);

            _db.ImportLogs.Add(new ImportLog
            {
                FileName = fileName,
                SectionId = sectionId,
                RowsImported = records.Count,
                ImportedByUserId = userId
            });

            await _db.SaveChangesAsync();

            return (true, $"تم استيراد {records.Count} سجل بنجاح إلى بند \"{section.NameAr}\".", records.Count);
        }

        public async Task<(bool Success, string Message, int RowsImported)> ImportDistributedAsync(
            AnalyzeResult analyzed, int userId, string fileName)
        {
            var sections = await _db.Sections.Where(s => s.IsActive).ToListAsync();
            if (sections.Count == 0)
                return (false, "لا توجد بنود مُعرّفة بالنظام حالياً.", 0);

            // فهرس أسماء البنود (عربي/إنجليزي) لمطابقة سريعة ومرنة
            var sectionByName = new Dictionary<string, Section>();
            foreach (var s in sections)
            {
                sectionByName[Normalize(s.NameAr)] = s;
                if (!string.IsNullOrWhiteSpace(s.NameEn))
                    sectionByName.TryAdd(Normalize(s.NameEn), s);
            }

            var textCols = analyzed.Columns.Where(c => c.Type == ColumnTypes.Text).Select(c => c.Name).ToList();
            var numericCols = analyzed.Columns.Where(c => c.Type == ColumnTypes.Numeric).Select(c => c.Name).ToList();
            var dateCols = analyzed.Columns.Where(c => c.Type == ColumnTypes.Date).Select(c => c.Name).ToList();

            if (textCols.Count == 0 || analyzed.Rows.Count == 0)
                return (false, "لم يتم العثور على عمود نصي يحدد اسم البند/الإدارة لكل صف.", 0);

            // 1) نحاول أولاً إيجاد عمود بعنوان صريح يدل على "البند/القسم/الإدارة"
            string? sectionCol = FindColumn(textCols, SectionColumnKeywords);

            // 2) إن لم نجد، نختبر كل عمود نصي ونحسب كم قيمة فيه تطابق اسم بند موجود فعليًا،
            //    ونعتمد العمود صاحب أعلى نسبة تطابق (بشرط أن يطابق صف واحد على الأقل)
            if (sectionCol == null)
            {
                string? bestCol = null;
                int bestMatches = 0;
                foreach (var col in textCols)
                {
                    int matches = analyzed.Rows.Count(r =>
                    {
                        var v = GetString(r, col);
                        return v != null && sectionByName.ContainsKey(Normalize(v));
                    });
                    if (matches > bestMatches)
                    {
                        bestMatches = matches;
                        bestCol = col;
                    }
                }
                if (bestMatches > 0) sectionCol = bestCol;
            }

            if (sectionCol == null)
            {
                return (false, "لم يتم العثور على عمود بالملف يحدد اسم البند/الإدارة يطابق أسماء البنود المعرّفة بالنظام. " +
                                "يرجى إضافة عمود (مثلاً باسم \"البند\") يحتوي على الاسم بالضبط كما هو مسجّل بالنظام، أو استخدم وضع الرفع اليدوي لبند واحد.", 0);
            }

            var otherTextCols = textCols.Where(c => c != sectionCol).ToList();
            string? categoryCol = otherTextCols.FirstOrDefault();
            string? totalCol = FindColumn(numericCols, TotalKeywords) ?? numericCols.FirstOrDefault();
            string? collectedCol = FindColumn(numericCols, CollectedKeywords);
            string? uncollectedCol = FindColumn(numericCols, UncollectedKeywords);
            string? targetCol = FindColumn(numericCols, TargetKeywords);
            string? dateCol = dateCols.FirstOrDefault();

            var recordsBySection = new Dictionary<int, List<DashboardRecord>>();
            var unmatchedValues = new HashSet<string>();
            int unmatchedCount = 0;

            foreach (var row in analyzed.Rows)
            {
                var rawSectionName = GetString(row, sectionCol);
                Section? matchedSection = rawSectionName != null && sectionByName.TryGetValue(Normalize(rawSectionName), out var s)
                    ? s
                    : null;

                if (matchedSection == null)
                {
                    unmatchedCount++;
                    if (!string.IsNullOrWhiteSpace(rawSectionName)) unmatchedValues.Add(rawSectionName);
                    continue;
                }

                var record = new DashboardRecord
                {
                    SectionId = matchedSection.Id,
                    ImportedByUserId = userId,
                    Category = categoryCol != null ? GetString(row, categoryCol) : null,
                    RecordDate = dateCol != null ? GetDate(row, dateCol) : null,
                    TotalValue = totalCol != null ? GetDecimal(row, totalCol) : null,
                    CollectedValue = collectedCol != null ? GetDecimal(row, collectedCol) : null,
                    UncollectedValue = uncollectedCol != null ? GetDecimal(row, uncollectedCol) : null,
                    TargetValue = targetCol != null ? GetDecimal(row, targetCol) : null,
                    RawJson = JsonSerializer.Serialize(row)
                };

                if (record.UncollectedValue == null && record.TotalValue.HasValue && record.CollectedValue.HasValue)
                {
                    record.UncollectedValue = record.TotalValue - record.CollectedValue;
                }

                if (!recordsBySection.TryGetValue(matchedSection.Id, out var list))
                {
                    list = new List<DashboardRecord>();
                    recordsBySection[matchedSection.Id] = list;
                }
                list.Add(record);
            }

            if (recordsBySection.Count == 0)
            {
                return (false, "لم يتم مطابقة أي صف بأي بند معروف بالنظام. تأكد أن أسماء البنود بعمود \"" + sectionCol + "\" مطابقة تمامًا لأسماء البنود المسجّلة.", 0);
            }

            int totalImported = 0;
            foreach (var (sectionId, list) in recordsBySection)
            {
                await _db.DashboardRecords.AddRangeAsync(list);
                _db.ImportLogs.Add(new ImportLog
                {
                    FileName = fileName,
                    SectionId = sectionId,
                    RowsImported = list.Count,
                    ImportedByUserId = userId
                });
                totalImported += list.Count;
            }

            await _db.SaveChangesAsync();

            var sectionNames = recordsBySection.Keys
                .Select(id => sections.First(s => s.Id == id).NameAr)
                .ToList();

            var msg = $"تم توزيع {totalImported} سجل تلقائيًا على {recordsBySection.Count} بند: {string.Join("، ", sectionNames)}.";
            if (unmatchedCount > 0)
            {
                var sample = unmatchedValues.Take(3);
                msg += $" تم تجاهل {unmatchedCount} صف لم تتطابق قيمة عمود \"{sectionCol}\" فيها مع أي بند معروف" +
                       (unmatchedValues.Count > 0 ? $" (مثال: {string.Join("، ", sample)})." : ".");
            }

            return (true, msg, totalImported);
        }

        /// <summary>
        /// وضع الرفع الوحيد: بدون اختيار بند يدويًا. يقرأ عمود "تصنيف البند" تلقائيًا،
        /// يوزّع كل صف إلى بنده الصحيح، ثم يستبدل بيانات كل بند تأثّر بالملف بالكامل (تحديث كامل
        /// لقاعدة البيانات بدل التراكم) بحيث تعكس اللوحات دائمًا آخر نسخة من الملف المرفوع.
        /// </summary>
        public async Task<(bool Success, string Message, int RowsImported)> ImportByClassificationAsync(
            AnalyzeResult analyzed, int userId, string fileName)
        {
            if (analyzed.Rows.Count == 0)
                return (false, "الملف لا يحتوي على أي بيانات قابلة للاستيراد.", 0);

            var sections = await _db.Sections.Where(s => s.IsActive).ToListAsync();
            if (sections.Count == 0)
                return (false, "لا توجد بنود مُعرّفة بالنظام حالياً.", 0);

            var sectionByName = new Dictionary<string, Section>();
            var sectionByScopeAndName = new Dictionary<(ScopeType Scope, string Name), Section>();
            foreach (var s in sections)
            {
                // فهرس عام (بدون منطقة) - يُستخدم فقط كحل احتياطي إن تعذّر تحديد المنطقة
                sectionByName[Normalize(s.NameAr)] = s;
                if (!string.IsNullOrWhiteSpace(s.NameEn))
                    sectionByName.TryAdd(Normalize(s.NameEn), s);

                // فهرس دقيق يفرّق بين بند "رسوم" بالأمانة وبند "رسوم" بالبلدية مثلاً
                // (الأمانة والبلدية عندهما بنود رئيسية بنفس الاسم، فلازم نميّز بالمنطقة أيضًا)
                sectionByScopeAndName.TryAdd((s.Scope, Normalize(s.NameAr)), s);
                if (!string.IsNullOrWhiteSpace(s.NameEn))
                    sectionByScopeAndName.TryAdd((s.Scope, Normalize(s.NameEn)), s);
            }

            // فهرس البنود الفرعية (مثل "جمع النفايات" تحت "رسوم") لمطابقة عمود "البند" مباشرة
            // بالبند الفرعي الصحيح بدل تخزين كل الصفوف تحت البند الرئيسي (تصنيف البند) فقط
            var childSectionByParentAndName = new Dictionary<(int ParentId, string Name), Section>();
            foreach (var s in sections.Where(s => s.ParentSectionId.HasValue))
            {
                var key = (s.ParentSectionId!.Value, Normalize(s.NameAr));
                childSectionByParentAndName.TryAdd(key, s);
            }

            // فهرس (بند أب ← أبناؤه) لمطابقة "الإدارة العامة"/"الإدارة الفرعية" تقريبيًا (Contains)
            // لأن قيم هذين العمودين بالملف غالبًا فيها بادئة زخرفية ("محفظة ...") غير موجودة
            // بأسماء الإدارات المسجّلة بالنظام بالضبط
            var childrenByParent = sections
                .Where(s => s.ParentSectionId.HasValue)
                .GroupBy(s => s.ParentSectionId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            Section? TryMatchChildFuzzy(int parentId, string? rawValue)
            {
                if (string.IsNullOrWhiteSpace(rawValue)) return null;
                var norm = Normalize(rawValue);

                if (childSectionByParentAndName.TryGetValue((parentId, norm), out var exact))
                    return exact;

                if (!childrenByParent.TryGetValue(parentId, out var children))
                    return null;

                return children.FirstOrDefault(c =>
                {
                    var childNorm = Normalize(c.NameAr);
                    return norm.Contains(childNorm) || childNorm.Contains(norm);
                });
            }

            var allColumnNames = analyzed.Columns.Select(c => c.Name).ToList();
            var textCols = analyzed.Columns.Where(c => c.Type == ColumnTypes.Text).Select(c => c.Name).ToList();
            var numericCols = analyzed.Columns.Where(c => c.Type == ColumnTypes.Numeric).Select(c => c.Name).ToList();
            var dateCols = analyzed.Columns.Where(c => c.Type == ColumnTypes.Date).Select(c => c.Name).ToList();

            // 1) عمود "تصنيف البند" - يُقرأ تلقائيًا (مطابقة دقيقة أولاً، ثم تقريبية عبر كلمات مفتاحية)
            string? classificationCol = allColumnNames.FirstOrDefault(c => Normalize(c) == Normalize(ClassificationColumnName))
                ?? FindColumn(textCols, ClassificationColumnKeywords);

            if (classificationCol == null)
            {
                return (false, $"لم يتم العثور على عمود \"{ClassificationColumnName}\" في الملف. " +
                                "يرجى التأكد من وجود هذا العمود بالملف ليتم تحديد البند المناسب لكل صف تلقائيًا.", 0);
            }

            // 1.ب) عمود "المنطقة" (أمانة / بلدية) - ضروري للتفريق بين بند مثل "رسوم" بالأمانة
            // و"رسوم" بالبلدية، لأن الاثنين لهما نفس الاسم بالضبط. المطابقة التقريبية تُطبَّع
            // الأحرف العربية (أ/إ/آ→ا، ة→ه، ى→ي) قبل المقارنة - عكس FindColumn العادية (Contains
            // حرفي فقط) - لأن عنوان هذا العمود قد يُكتب بأكثر من شكل بملفات مختلفة
            string? regionCol = allColumnNames.FirstOrDefault(c => Normalize(c) == Normalize(RegionColumnName))
                ?? textCols.Where(c => c != classificationCol)
                    .FirstOrDefault(c => RegionColumnKeywords.Any(k => Normalize(c).Contains(Normalize(k))));

            // 1.ب.٢) عمود "الأمانة/بلدى" - عمود مستقل تمامًا عن "المنطقة" أعلاه (تأكيد صريح من
            // المستخدمة: عمودان منفصلان بالملف، هذا تحديدًا أول عمود بالبيانات). مطابقة دقيقة
            // بالاسم أولاً، ثم مطابقة تقريبية (تحتوي "امانه" و"بلد" معًا - تتحمّل فروق المسافات
            // حول "/" واختلاف كتابة "بلدي/بلدى")، وإلا أول عمود بالملف (غير "تصنيف البند") كاحتياطي
            string? amanahBaladiCol = allColumnNames.FirstOrDefault(c => Normalize(c) == Normalize(AmanahBaladiColumnName) && c != classificationCol)
                ?? allColumnNames.FirstOrDefault(c => c != classificationCol
                    && Normalize(c).Contains("امانه") && Normalize(c).Contains("بلد"))
                ?? allColumnNames.FirstOrDefault(c => c != classificationCol);

            // 1.ج) عمود "الجهة" (امانة المدينة / بلدية الحناكية...) - يُخزَّن مع كل سجل
            // حتى يمكن عرض لوحة بيانات مستقلة لكل بلدية
            string? entityCol = allColumnNames.FirstOrDefault(c => Normalize(c) == Normalize(EntityColumnName))
                ?? FindColumn(textCols.Where(c => c != classificationCol && c != regionCol).ToList(), EntityColumnKeywords);

            // 1.د) عمود "نوع البند" (ملزم/غير ملزم) - مطابقة دقيقة فقط، مستقل عن عمود "البند" أدناه
            string? itemTypeCol = allColumnNames.FirstOrDefault(c => Normalize(c) == Normalize(ItemTypeColumnName) && c != classificationCol);

            // 2) عمود الفئة/البند التفصيلي (لعرضه بالرسوم البيانية والتصفية التفاعلية) - يُستبعَد
            // عمود "نوع البند" أعلاه من هذا البحث حتى لا يُعاد استخدام نفس العمود لغرضين مختلفين
            string? categoryCol = allColumnNames.FirstOrDefault(c => Normalize(c) == Normalize(PreferredCategoryColumnName) && c != classificationCol)
                ?? FindColumn(textCols.Where(c => c != classificationCol && c != itemTypeCol).ToList(), CategoryColumnKeywords)
                ?? textCols.FirstOrDefault(c => c != classificationCol && c != itemTypeCol);

            // 2.ب) عمود "رقم البند" - قد يكون نصيًا أو رقميًا حسب الملف، فنبحث بكل الأعمدة
            string? itemNumberCol = allColumnNames.FirstOrDefault(c => Normalize(c) == Normalize(ItemNumberColumnName))
                ?? FindColumn(allColumnNames, ItemNumberColumnKeywords);

            // 3) عمود القيمة المالية
            string? valueCol = allColumnNames.FirstOrDefault(c => Normalize(c) == Normalize(PreferredValueColumnName) && numericCols.Contains(c))
                ?? FindColumn(numericCols, ValueColumnKeywords)
                ?? numericCols.FirstOrDefault();

            // 4) عمود حالة السداد (لتحديد المحصّل من غير المحصّل)
            string? statusCol = FindColumn(textCols, PaymentStatusColumnKeywords);

            // 5) عمود الهدف إن وُجد (اكتشاف تقريبي كما بالوضع اليدوي)
            string? targetCol = FindColumn(numericCols, TargetKeywords);

            // 6) عمود التاريخ - نُفضّل "تاريخ الدفع" (يعكس وقت تحصيل الإيراد فعليًا)، ثم نرجع
            // لـ"تاريخ الإنشاء" إن لم يوجد عمود دفع بالملف، ثم أي عمود تاريخ آخر كحل أخير
            string? dateCol = allColumnNames.FirstOrDefault(c => Normalize(c) == Normalize(PreferredDateColumnName) && dateCols.Contains(c))
                ?? allColumnNames.FirstOrDefault(c => Normalize(c) == Normalize(FallbackDateColumnName) && dateCols.Contains(c))
                ?? dateCols.FirstOrDefault();

            // 7) عمود رقم العقد (لمربع البحث بالداشبورد) - يُحتمل أن يكون العمود رقميًا بالملف
            // (أرقام عقود بلا حروف) فيُصنَّف ضمن numericCols لا textCols، لذا نبحث بكليهما.
            // بعض الملفات (مثل بيانات "استثمار") فيها عمودان بنفس الاسم "رقم العقد" - الأول عام
            // وفارغ غالبًا، والأخير هو الفعلي المعبّأ بالقيم الحقيقية (تأكيد صريح من المستخدمة:
            // "في البداية عندنا رقم عقد ولا تشوفها، فقط شوف آخر واحد") - LastOrDefault بدل
            // FirstOrDefault يضمن أخذ العمود الأخير المطابق بالاسم بالضبط
            string? contractCol = allColumnNames.LastOrDefault(c => Normalize(c) == Normalize(ContractNumberColumnName))
                ?? FindColumn(textCols, ContractNumberColumnKeywords)
                ?? FindColumn(numericCols, ContractNumberColumnKeywords);

            // 9) عمودا الإدارة العامة/الفرعية (لتوجيه صفوف "استثمار" لمستوى الإدارات بالقائمة الجانبية)
            string? deptGeneralCol = allColumnNames.FirstOrDefault(c => Normalize(c) == Normalize(DeptGeneralColumnName))
                ?? FindColumn(textCols, DeptGeneralColumnKeywords);
            string? deptSubCol = allColumnNames.FirstOrDefault(c => Normalize(c) == Normalize(DeptSubColumnName))
                ?? FindColumn(textCols, DeptSubColumnKeywords);

            // 10) عمود اسم العقد (لصفحة "البحث برقم العقد") - يُقرأ كما هو بالملف بدون أي بديل تلقائي
            string? contractNameCol = allColumnNames.FirstOrDefault(c => Normalize(c) == Normalize(ContractNameColumnName))
                ?? FindColumn(textCols, ContractNameColumnKeywords);

            // 11) عمود رقم الفاتورة (لصفحة "البحث برقم العقد")
            string? invoiceNumberCol = allColumnNames.FirstOrDefault(c => Normalize(c) == Normalize(InvoiceNumberColumnName))
                ?? FindColumn(textCols, InvoiceNumberColumnKeywords);

            // 12) عمود تاريخ الإنشاء بالضبط (منفصل عن dateCol أعلاه المستخدَم بالرسم الشهري،
            // والذي قد يكون "تاريخ الدفع" بدلاً من "تاريخ الإنشاء")
            string? createdDateCol = allColumnNames.FirstOrDefault(c => Normalize(c) == Normalize(CreatedDateColumnName) && dateCols.Contains(c));

            // 13) عمود "نوع الإيراد (تصنيف المستهدف)" لصفحة "مقارنة المستهدفات"
            string? revenueTypeCol = FindColumn(textCols, RevenueTypeColumnKeywords);

            // 14) عمودا المستهدف السنوي حسب الجهة المشرفة (أرقام - نبحث عن "وزارة البلديات" أولاً
            // لتفادي التقاط عمود المالية بالخطأ ضمن نفس المطابقة التقريبية)
            string? targetBaladiyahCol = FindColumn(numericCols, TargetBaladiyahColumnKeywords);
            string? targetFinanceCol = FindColumn(numericCols.Where(c => c != targetBaladiyahCol).ToList(), TargetFinanceColumnKeywords);

            // 15) أعمدة صفحة "الإدارات العامة" (نوع الاستثمار / حالة العقد / اسم المستثمر / رقم الدفعة)
            string? investmentTypeCol = allColumnNames.FirstOrDefault(c => Normalize(c) == Normalize(InvestmentTypeColumnName))
                ?? FindColumn(textCols, InvestmentTypeColumnKeywords);
            string? contractStatusCol = allColumnNames.FirstOrDefault(c => Normalize(c) == Normalize(ContractStatusColumnName))
                ?? FindColumn(textCols, ContractStatusColumnKeywords);
            string? investorCol = allColumnNames.FirstOrDefault(c => Normalize(c) == Normalize(InvestorColumnName))
                ?? FindColumn(textCols, InvestorColumnKeywords);
            // قد يُصنَّف "رقم إيفاء" كعمود رقمي حسب محتواه بالملف - نبحث بكلا القائمتين احتياطًا
            string? paymentNumberCol = FindColumn(textCols, PaymentNumberColumnKeywords)
                ?? FindColumn(numericCols, PaymentNumberColumnKeywords);

            var recordsBySection = new Dictionary<int, List<DashboardRecord>>();
            var unmatchedValues = new HashSet<string>();
            int unmatchedCount = 0;

            // بنود رئيسية (بلا أب) مجمّعة حسب المنطقة - تُستخدم بالمطابقة التقريبية أدناه
            var topLevelSectionsByScope = sections
                .Where(s => s.ParentSectionId == null)
                .GroupBy(s => s.Scope)
                .ToDictionary(g => g.Key, g => g.ToList());

            /// <summary>يجد أطول اسم بند رئيسي حالي (بنفس المنطقة) يكون بادئة للقيمة الخام، لمطابقة
            /// قيم تصنيف مثل "رسوم المبيعات-الايواء"/"رسوم المبيعات-التبغ" ببند "رسوم المبيعات"
            /// الموجود فعليًا (بدل تجاهل الصف بالكامل لعدم تطابق حرفي تام). الطول الأطول يُفضَّل
            /// عمدًا حتى لا يلتقط "رسوم" (4 أحرف) بدل "رسوم المبيعات" (13 حرفًا) بالخطأ.</summary>
            Section? FindLongestPrefixMatch(ScopeType scope, string normalizedValue)
            {
                if (!topLevelSectionsByScope.TryGetValue(scope, out var candidates)) return null;
                return candidates
                    .Where(s => normalizedValue.StartsWith(Normalize(s.NameAr), StringComparison.Ordinal))
                    .OrderByDescending(s => s.NameAr.Length)
                    .FirstOrDefault();
            }

            // نفس منطق تحديد "البند الرئيسي" (تصنيف البند + منطقة) بالحلقة الرئيسية أدناه بالضبط -
            // يُستخدم هنا مرة واحدة فقط لتجميع البنود الفرعية حسب رقم البند قبل توزيع السجلات فعليًا
            Section? ResolveTopSection(string? rawClassificationValue, string? rawRegionValue)
            {
                if (string.IsNullOrWhiteSpace(rawClassificationValue)) return null;
                var normClassification = Normalize(rawClassificationValue);
                Section? matched = null;
                ScopeType? rowScope = null;

                if (!string.IsNullOrWhiteSpace(rawRegionValue))
                {
                    var normRegion = Normalize(rawRegionValue);
                    rowScope = normRegion.Contains("بلد") ? ScopeType.Baladiyah
                        : (normRegion.Contains("امانه") ? ScopeType.Amanah : (ScopeType?)null);
                    if (rowScope.HasValue)
                        sectionByScopeAndName.TryGetValue((rowScope.Value, normClassification), out matched);
                }
                if (matched == null)
                    sectionByName.TryGetValue(normClassification, out matched);

                // احتياطي أخير: تطابق تقريبي بأطول بادئة (مثل "رسوم المبيعات-الايواء" → "رسوم المبيعات")
                if (matched == null && rowScope.HasValue)
                    matched = FindLongestPrefixMatch(rowScope.Value, normClassification);

                return matched;
            }

            // (البند الأب، رقم البند) -> Section المطابق النهائي - يُستخدم بالحلقة الرئيسية أدناه
            var resolvedByNumber = new Dictionary<(int ParentId, string ItemNumber), Section>();

            // ==================================================================
            // تعلّم/إنشاء البنود الفرعية تلقائيًا بالاعتماد على "رقم البند" (معرّف مستقر) بدل
            // الاعتماد فقط على تطابق نص عمود "البند" الذي قد يتغيّر بين رفعة وأخرى لنفس البند
            // فعليًا (مثال حقيقي بالبيانات: "تأجير مباني" و"تأجير مباني تجارية" بنفس رقم البند).
            // لكل مجموعة (بند أب، رقم بند): يُعاد استخدام بند فرعي حالي مسجّل عليه هذا الرقم من
            // رفعة سابقة أولاً، وإلا بند فرعي حالي يطابق اسمه أكثر الأسماء تكرارًا بهذه المجموعة
            // (ويُحفَظ الرقم عليه للرفعات القادمة)، وإلا يُنشأ بند فرعي جديد بالاسم الأعلى تكرارًا.
            // صفوف رقم البند الفارغ (مثل "أوامر التنفيذ") تُجمَّع وتُطابَق بالاسم فقط (مع تسامح
            // إملائي بسيط)، وتُنشئ بندًا فرعيًا جديدًا إن لم يوجد أي تطابق.
            // ==================================================================
            if (categoryCol != null)
            {
                var numberGroups = new Dictionary<(int ParentId, string ItemNumber), Dictionary<string, int>>();
                var nameOnlyGroups = new Dictionary<(int ParentId, string NormName), Dictionary<string, int>>();

                foreach (var row in analyzed.Rows)
                {
                    var topSection = ResolveTopSection(GetString(row, classificationCol), regionCol != null ? GetString(row, regionCol) : null);
                    if (topSection == null) continue;

                    var rawDetailItem = GetString(row, categoryCol)?.Trim();
                    if (string.IsNullOrWhiteSpace(rawDetailItem)) continue;

                    var rawItemNumber = itemNumberCol != null ? GetString(row, itemNumberCol)?.Trim() : null;

                    if (!string.IsNullOrWhiteSpace(rawItemNumber))
                    {
                        var key = (topSection.Id, rawItemNumber);
                        if (!numberGroups.TryGetValue(key, out var names)) { names = new Dictionary<string, int>(); numberGroups[key] = names; }
                        names[rawDetailItem] = names.GetValueOrDefault(rawDetailItem) + 1;
                    }
                    else
                    {
                        var key = (topSection.Id, Normalize(rawDetailItem));
                        if (!nameOnlyGroups.TryGetValue(key, out var names)) { names = new Dictionary<string, int>(); nameOnlyGroups[key] = names; }
                        names[rawDetailItem] = names.GetValueOrDefault(rawDetailItem) + 1;
                    }
                }

                var sectionsToCreate = new List<(Section Section, string ItemNumber)>();
                var itemNumbersToPersist = new Dictionary<int, HashSet<string>>();

                void AddNumberToPersist(int sectionId, string number)
                {
                    if (!itemNumbersToPersist.TryGetValue(sectionId, out var set)) { set = new HashSet<string>(); itemNumbersToPersist[sectionId] = set; }
                    set.Add(number);
                }

                foreach (var kv in numberGroups)
                {
                    var (parentId, itemNumber) = kv.Key;
                    var namesByFreq = kv.Value.OrderByDescending(x => x.Value).Select(x => x.Key).ToList();

                    var existingByNumber = childrenByParent.TryGetValue(parentId, out var siblings)
                        ? siblings.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.ItemNumbers)
                            && s.ItemNumbers.Split(',', StringSplitOptions.TrimEntries).Contains(itemNumber))
                        : null;
                    if (existingByNumber != null)
                    {
                        resolvedByNumber[kv.Key] = existingByNumber;
                        continue;
                    }

                    Section? matchByName = null;
                    foreach (var name in namesByFreq)
                    {
                        if (childSectionByParentAndName.TryGetValue((parentId, Normalize(name)), out var s))
                        {
                            matchByName = s;
                            break;
                        }
                    }
                    if (matchByName != null)
                    {
                        resolvedByNumber[kv.Key] = matchByName;
                        AddNumberToPersist(matchByName.Id, itemNumber);
                        continue;
                    }

                    var parentSection = sections.First(s => s.Id == parentId);
                    var newSection = new Section
                    {
                        NameAr = namesByFreq[0],
                        NameEn = namesByFreq[0],
                        Slug = "auto-" + Guid.NewGuid().ToString("N").Substring(0, 10),
                        Scope = parentSection.Scope,
                        ParentSectionId = parentId,
                        IsActive = true,
                        IsDashboardEntry = false,
                        SortOrder = (childrenByParent.TryGetValue(parentId, out var sib2) ? sib2.Count : 0) + 1
                    };
                    sectionsToCreate.Add((newSection, itemNumber));
                }

                foreach (var kv in nameOnlyGroups)
                {
                    var (parentId, normName) = kv.Key;
                    if (childSectionByParentAndName.ContainsKey((parentId, normName))) continue;

                    var fuzzyMatch = childrenByParent.TryGetValue(parentId, out var siblings2)
                        ? siblings2.FirstOrDefault(s => IsFuzzyNameMatch(Normalize(s.NameAr), normName))
                        : null;
                    if (fuzzyMatch != null) continue; // ستُطابَق بالحلقة الرئيسية أدناه تلقائيًا

                    var namesByFreq = kv.Value.OrderByDescending(x => x.Value).Select(x => x.Key).ToList();
                    var parentSection = sections.First(s => s.Id == parentId);
                    var newSection = new Section
                    {
                        NameAr = namesByFreq[0],
                        NameEn = namesByFreq[0],
                        Slug = "auto-" + Guid.NewGuid().ToString("N").Substring(0, 10),
                        Scope = parentSection.Scope,
                        ParentSectionId = parentId,
                        IsActive = true,
                        IsDashboardEntry = false,
                        SortOrder = (childrenByParent.TryGetValue(parentId, out var sib3) ? sib3.Count : 0) + 1
                    };
                    sectionsToCreate.Add((newSection, ""));
                }

                if (sectionsToCreate.Count > 0)
                {
                    foreach (var (s, _) in sectionsToCreate) await _db.Sections.AddAsync(s);
                    await _db.SaveChangesAsync();

                    foreach (var (s, itemNumber) in sectionsToCreate)
                    {
                        sections.Add(s);
                        childSectionByParentAndName.TryAdd((s.ParentSectionId!.Value, Normalize(s.NameAr)), s);
                        if (!childrenByParent.TryGetValue(s.ParentSectionId!.Value, out var list))
                        { list = new List<Section>(); childrenByParent[s.ParentSectionId!.Value] = list; }
                        list.Add(s);

                        if (!string.IsNullOrWhiteSpace(itemNumber))
                        {
                            resolvedByNumber[(s.ParentSectionId!.Value, itemNumber)] = s;
                            AddNumberToPersist(s.Id, itemNumber);
                        }
                    }
                }

                if (itemNumbersToPersist.Count > 0)
                {
                    var idsToUpdate = itemNumbersToPersist.Keys.ToList();
                    var sectionsToUpdate = await _db.Sections.Where(s => idsToUpdate.Contains(s.Id)).ToListAsync();
                    foreach (var s in sectionsToUpdate)
                    {
                        var existingNumbers = string.IsNullOrWhiteSpace(s.ItemNumbers)
                            ? new HashSet<string>()
                            : s.ItemNumbers.Split(',', StringSplitOptions.TrimEntries).ToHashSet();
                        existingNumbers.UnionWith(itemNumbersToPersist[s.Id]);
                        s.ItemNumbers = string.Join(",", existingNumbers);
                    }
                    await _db.SaveChangesAsync();
                }
            }

            foreach (var row in analyzed.Rows)
            {
                var rawClassification = GetString(row, classificationCol);
                if (string.IsNullOrWhiteSpace(rawClassification))
                    continue; // صف فارغ من ناحية التصنيف (مثل صفوف زائدة بلا بيانات) - يُتجاهل بصمت

                // نحدّد منطقة الصف (أمانة/بلدية) من عمود "المنطقة" إن وُجد، لأن بعض البنود
                // الرئيسية (رسوم، جزاءات...) لها نفس الاسم بالضبط عند الأمانة وعند البلدية
                Section? matchedSection = null;
                var rawRegion = regionCol != null ? GetString(row, regionCol) : null;
                ScopeType? rowScope = null;
                if (!string.IsNullOrWhiteSpace(rawRegion))
                {
                    var normRegion = Normalize(rawRegion);
                    rowScope = normRegion.Contains("بلد") ? ScopeType.Baladiyah
                        : (normRegion.Contains("امانه") ? ScopeType.Amanah : (ScopeType?)null);

                    if (rowScope.HasValue)
                        sectionByScopeAndName.TryGetValue((rowScope.Value, Normalize(rawClassification)), out matchedSection);
                }

                // احتياطي: لو ما قدرنا نحدد المنطقة (عمود غير موجود أو قيمة غير معروفة)، نرجع للمطابقة العامة
                if (matchedSection == null)
                    sectionByName.TryGetValue(Normalize(rawClassification), out matchedSection);

                // احتياطي أخير: تطابق تقريبي بأطول بادئة (نفس منطق ResolveTopSection أعلاه) - يلتقط
                // قيم تصنيف مثل "رسوم المبيعات-الايواء"/"رسوم المبيعات-التبغ" ببند "رسوم المبيعات"
                // الموجود فعليًا بدل إسقاط الصف بالكامل لعدم تطابق حرفي تام
                if (matchedSection == null && rowScope.HasValue)
                    matchedSection = FindLongestPrefixMatch(rowScope.Value, Normalize(rawClassification));

                if (matchedSection == null)
                {
                    unmatchedCount++;
                    unmatchedValues.Add(rawClassification);
                    continue;
                }

                // اسم البند الرئيسي المُطابَق فعليًا بالنظام (وليس نص "تصنيف البند" الخام بالملف) -
                // يُخزَّن بحقل Classification أدناه حتى تعرض كل الرسوم/الجداول التي تعتمد عليه
                // (أعلى بنود الإيرادات، تحقيق المستهدف حسب البنود) البند الرئيسي الموحّد دائمًا
                var resolvedTopLevelName = matchedSection.NameAr;

                // نوجّه السجل مباشرة لبنده الفرعي الصحيح (مثل "جمع النفايات" تحت "رسوم") بدل البند
                // الرئيسي فقط - عبر رقم البند أولاً (أكثر استقرارًا من نص "البند" الذي قد يتغيّر
                // بين رفعة وأخرى لنفس البند فعليًا)، ثم تطابق الاسم الحرفي، ثم تطابق تقريبي متسامح
                // مع خطأ إملائي بسيط كحل أخير
                var rawDetailItem = categoryCol != null ? GetString(row, categoryCol)?.Trim() : null;
                var rawItemNumber = itemNumberCol != null ? GetString(row, itemNumberCol)?.Trim() : null;

                if (!string.IsNullOrWhiteSpace(rawItemNumber)
                    && resolvedByNumber.TryGetValue((matchedSection.Id, rawItemNumber), out var numberSection))
                {
                    matchedSection = numberSection;
                }
                else if (!string.IsNullOrWhiteSpace(rawDetailItem))
                {
                    if (childSectionByParentAndName.TryGetValue((matchedSection.Id, Normalize(rawDetailItem)), out var childSection))
                    {
                        matchedSection = childSection;
                    }
                    else
                    {
                        var fuzzyChild = childrenByParent.TryGetValue(matchedSection.Id, out var siblingSections)
                            ? siblingSections.FirstOrDefault(s => IsFuzzyNameMatch(Normalize(s.NameAr), Normalize(rawDetailItem)))
                            : null;
                        if (fuzzyChild != null) matchedSection = fuzzyChild;
                    }
                }

                // نص "الإدارة العامة"/"الإدارة الفرعية" الخام - يُخزَّن على السجل مباشرة (يُستخدم
                // بصفحة "بنود الأمانة" لبند الاستثمار) بصرف النظر عن نجاح التوجيه لبند فرعي أدناه
                var rawDeptGeneral = deptGeneralCol != null ? GetString(row, deptGeneralCol)?.Trim() : null;
                var rawDeptSub = deptSubCol != null ? GetString(row, deptSubCol)?.Trim() : null;

                // توجيه إضافي (اختياري): لو تحت البند الحالي إدارات عامة/فرعية مسجّلة كبنود فرعية
                // فعلية بشجرة البنود، نوجّه الصف لإدارته الفعلية بدل بقائه على مستوى البند فقط
                var deptSection = TryMatchChildFuzzy(matchedSection.Id, rawDeptGeneral);
                if (deptSection != null)
                {
                    matchedSection = deptSection;

                    var subDeptSection = TryMatchChildFuzzy(matchedSection.Id, rawDeptSub);
                    if (subDeptSection != null)
                        matchedSection = subDeptSection;
                }

                decimal? value = valueCol != null ? GetDecimal(row, valueCol) : null;
                string? status = statusCol != null ? GetString(row, statusCol) : null;

                bool isCollected = IsCollectedStatus(status);

                // اسم العقد: نأخذه فقط من عمود "اسم العقد" الصريح كما هو بالملف. إن كان فارغًا
                // لهذا الصف يبقى فارغًا (بدون أي بديل تلقائي مثل اسم المستثمر أو غيره)
                string? contractName = contractNameCol != null ? GetString(row, contractNameCol) : null;

                DateTime? recordDateVal = dateCol != null ? GetDate(row, dateCol) : null;
                DateTime? createdDateVal = createdDateCol != null ? GetDate(row, createdDateCol) : null;

                // "الربع"/"الشهر" يُحسبان دائمًا من الكود (لا يُقرآن كنص خام من الملف): للفواتير
                // المحصّلة من "تاريخ الدفع" (متى فعليًا دخل المبلغ)، وغير المحصّلة من "تاريخ
                // الإنشاء" (لا يوجد تاريخ دفع فعلي لها بعد) - حتى تبقى الفلترة موثوقة بصرف النظر
                // عمّا كُتب يدويًا بعمود "الربع" بالملف الأصلي (إن وُجد أصلاً)
                DateTime? quarterBasisDate = isCollected ? recordDateVal : createdDateVal;
                int? quarterBasisMonth = quarterBasisDate?.Month;

                var record = new DashboardRecord
                {
                    SectionId = matchedSection.Id,
                    ImportedByUserId = userId,
                    Category = categoryCol != null ? GetString(row, categoryCol) : null,
                    ItemType = itemTypeCol != null ? GetString(row, itemTypeCol)?.Trim() : null,
                    AmanahBaladiRaw = amanahBaladiCol != null ? GetString(row, amanahBaladiCol)?.Trim() : null,
                    Entity = entityCol != null ? CleanEntityName(GetString(row, entityCol)) : null,
                    ContractNumber = contractCol != null ? GetString(row, contractCol) : null,
                    ContractName = string.IsNullOrWhiteSpace(contractName) ? null : contractName,
                    InvoiceNumber = invoiceNumberCol != null ? GetString(row, invoiceNumberCol) : null,
                    Classification = resolvedTopLevelName,
                    ItemNumber = rawItemNumber,
                    DeptGeneral = string.IsNullOrWhiteSpace(rawDeptGeneral) ? null : rawDeptGeneral,
                    DeptSub = string.IsNullOrWhiteSpace(rawDeptSub) ? null : rawDeptSub,
                    InvestmentType = investmentTypeCol != null ? GetString(row, investmentTypeCol)?.Trim() : null,
                    ContractStatus = contractStatusCol != null ? GetString(row, contractStatusCol)?.Trim() : null,
                    InvestorName = investorCol != null ? GetString(row, investorCol)?.Trim() : null,
                    PaymentNumber = paymentNumberCol != null ? GetString(row, paymentNumberCol)?.Trim() : null,
                    Quarter = quarterBasisMonth.HasValue ? "Q" + (((quarterBasisMonth.Value - 1) / 3) + 1) : null,
                    Month = quarterBasisMonth,
                    RecordDate = recordDateVal,
                    CreatedDate = createdDateVal,
                    TotalValue = value,
                    CollectedValue = isCollected ? value : (status != null ? 0 : null),
                    UncollectedValue = !isCollected ? value : (status != null ? 0 : null),
                    TargetValue = targetCol != null ? GetDecimal(row, targetCol) : null,
                    RevenueType = revenueTypeCol != null ? GetString(row, revenueTypeCol)?.Trim() : null,
                    TargetFinanceValue = targetFinanceCol != null ? GetDecimal(row, targetFinanceCol) : null,
                    TargetBaladiyahValue = targetBaladiyahCol != null ? GetDecimal(row, targetBaladiyahCol) : null,
                    // RawJson غير مُستخدَم في أي مكان بالعرض؛ نتركه فارغًا لتوفير الذاكرة ومساحة
                    // القرص عند استيراد ملفات ضخمة (مئات آلاف الصفوف).
                    RawJson = null
                };

                if (!recordsBySection.TryGetValue(matchedSection.Id, out var list))
                {
                    list = new List<DashboardRecord>();
                    recordsBySection[matchedSection.Id] = list;
                }
                list.Add(record);
            }

            if (recordsBySection.Count == 0)
            {
                return (false, $"لم يتم مطابقة أي صف بأي بند معروف بالنظام عبر عمود \"{classificationCol}\". " +
                                "تأكد أن القيم مطابقة تمامًا لأسماء البنود المسجّلة (مثل: \"استثمار\"، \"تأجير مؤقت\").", 0);
            }

            // تحديث كامل لقاعدة البيانات: تُستبدل بيانات كل بند تأثّر بالملف بالكامل بدل الإضافة فوقها،
            // بحيث تعكس اللوحات دائمًا آخر نسخة مرفوعة من الملف عند إعادة الرفع
            var affectedSectionIds = recordsBySection.Keys.ToList();
            await _db.DashboardRecords.Where(r => affectedSectionIds.Contains(r.SectionId)).ExecuteDeleteAsync();
            await _db.ImportLogs.Where(l => affectedSectionIds.Contains(l.SectionId)).ExecuteDeleteAsync();

            // إدخال على دفعات مع تعطيل الكشف التلقائي للتغييرات وتفريغ المتتبّع بعد كل دفعة،
            // حتى لا تتضخم الذاكرة ويبطؤ الإدخال مع مئات آلاف السجلات.
            const int BatchSize = 5000;
            bool prevAutoDetect = _db.ChangeTracker.AutoDetectChangesEnabled;
            _db.ChangeTracker.AutoDetectChangesEnabled = false;
            int totalImported = 0;
            try
            {
                foreach (var (sectionId, list) in recordsBySection)
                {
                    for (int i = 0; i < list.Count; i += BatchSize)
                    {
                        var count = Math.Min(BatchSize, list.Count - i);
                        await _db.DashboardRecords.AddRangeAsync(list.GetRange(i, count));
                        await _db.SaveChangesAsync();
                        _db.ChangeTracker.Clear();
                    }
                    totalImported += list.Count;
                }

                // سجلات الاستيراد (ImportLogs) تُضاف بعد إدخال كل الدفعات ثم تُحفظ مرة أخيرة
                foreach (var (sectionId, list) in recordsBySection)
                {
                    _db.ImportLogs.Add(new ImportLog
                    {
                        FileName = fileName,
                        SectionId = sectionId,
                        RowsImported = list.Count,
                        ImportedByUserId = userId
                    });
                }
                await _db.SaveChangesAsync();
            }
            finally
            {
                _db.ChangeTracker.AutoDetectChangesEnabled = prevAutoDetect;
            }

            var sectionNames = recordsBySection.Keys
                .Select(id => sections.First(s => s.Id == id).NameAr)
                .ToList();

            var msg = $"تم تحديث قاعدة البيانات بالكامل: {totalImported} سجل عبر {recordsBySection.Count} بند " +
                       $"({string.Join("، ", sectionNames)})، بناءً على عمود \"{classificationCol}\".";

            if (unmatchedCount > 0)
            {
                var sample = unmatchedValues.Take(3);
                msg += $" تم تجاهل {unmatchedCount} صف لم تُطابق قيمة \"{classificationCol}\" فيها أي بند معروف" +
                       (unmatchedValues.Count > 0 ? $" (مثال: {string.Join("، ", sample)})." : ".");
            }

            return (true, msg, totalImported);
        }

        // داخلية (internal) بدل خاصة حتى تُعاد استخدامها من خدمات الاستيراد الأخرى (مثل
        // DebtInvoiceImportService/ClaimExecutionImportService) دون تكرار نفس منطق تطبيع النصوص العربية
        internal static string Normalize(string value)
        {
            return System.Text.RegularExpressions.Regex.Replace(value.Trim(), @"\s+", " ")
                        .Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا")
                        .Replace("ة", "ه")
                        .Replace("ى", "ي")
                        .ToLowerInvariant();
        }

        // تنظيف اسم الجهة: بعض الملفات فيها مسافات مزدوجة ("بلدية  الحناكية")
        // فنوحّدها لمسافة واحدة حتى يتطابق الاسم دائمًا عند التصفية والعرض
        private static string? CleanEntityName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return System.Text.RegularExpressions.Regex.Replace(value.Trim(), @"\s+", " ");
        }

        // داخلية (internal) بدل خاصة حتى تُعاد استخدامها من خدمات الاستيراد الأخرى (مثل
        // DebtInvoiceImportService/ClaimExecutionImportService) دون تكرار نفس منطق قراءة قيم الصف الخام
        internal static string? FindColumn(List<string> columns, string[] keywords)
        {
            return columns.FirstOrDefault(c =>
                keywords.Any(k => c.ToLower().Contains(k.ToLower())));
        }

        internal static string? GetString(Dictionary<string, object?> row, string col)
        {
            return row.TryGetValue(col, out var val) ? val?.ToString() : null;
        }

        internal static decimal? GetDecimal(Dictionary<string, object?> row, string col)
        {
            if (!row.TryGetValue(col, out var val) || val == null) return null;
            return val switch
            {
                double d => (decimal)d,
                decimal dec => dec,
                _ => decimal.TryParse(val.ToString(), out var parsed) ? parsed : null
            };
        }

        internal static DateTime? GetDate(Dictionary<string, object?> row, string col)
        {
            if (!row.TryGetValue(col, out var val) || val == null) return null;
            // InvariantCulture إلزامي (راجع الشرح بـExcelDashboardService.cs عند بناء rowDict) - بدونها
            // يفشل التحليل بالكامل على أنظمة التشغيل بثقافة عربية سعودية (تقويم هجري افتراضيًا)
            return DateTime.TryParse(val.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : null;
        }

        /// <summary>مسافة Levenshtein القياسية بين نصّين (عدد أقل عمليات إدراج/حذف/استبدال حرف واحد لتحويل أحدهما للآخر).</summary>
        private static int LevenshteinDistance(string a, string b)
        {
            var dp = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) dp[0, j] = j;
            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    dp[i, j] = Math.Min(
                        Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                        dp[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
                }
            }
            return dp[a.Length, b.Length];
        }

        /// <summary>
        /// مطابقة اسمين (مُطبَّعين مسبقًا) متسامحة مع خطأ إملائي بسيط بحرف أو حرفين (مثل "أوامر
        /// التمفيذ" بدل "أوامر التنفيذ" بالملف الفعلي) - عتبة مرنة نسبةً لطول النص لتفادي مطابقة
        /// أسماء مختلفة فعليًا بمحض الصدفة عند النصوص القصيرة جدًا.
        /// </summary>
        private static bool IsFuzzyNameMatch(string a, string b)
        {
            if (a == b) return true;
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            var maxLen = Math.Max(a.Length, b.Length);
            if (maxLen < 6) return false; // نصوص قصيرة جدًا - المطابقة التقريبية غير موثوقة
            var threshold = Math.Max(1, maxLen / 8);
            return LevenshteinDistance(a, b) <= threshold;
        }
    }
}
