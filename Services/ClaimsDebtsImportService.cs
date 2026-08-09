using System.Text.Json;
using ExcelDashboard.Data;
using ExcelDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDashboard.Services
{
    public interface IClaimsDebtsImportService
    {
        /// <summary>
        /// يقرأ ملف Excel (ورقة واحدة أو أكثر) ويستورد منه المطالبات والمديونيات معًا، ثم يستبدل
        /// كل البيانات القديمة بالكامل بالجديدة (بلا تراكم عند إعادة الرفع) - بنفس أسلوب استيراد
        /// بقية النظام. يستخدم AnalyzeAllSheets نفسه المستخدم بباقي أنواع الملفات (لا يُعاد كتابة
        /// أي منطق قراءة Excel).
        /// </summary>
        Task<(bool Success, string Message, int ClaimsImported, int DebtsImported)> ImportAsync(Stream fileStream);
    }

    /// <summary>
    /// يحوّل نتيجة تحليل Excel (AnalyzeResult لكل ورقة) إلى سجلات ClaimRecord/DebtRecord.
    ///
    /// الشكل الفعلي لملف "المطالبات والمديونيات" المعتمد بالنظام هو كشف دفعات/فواتير عقود
    /// الاستثمار (نفس نطاق بقية النظام)، حيث كل صف = دفعة واحدة تحمل بيانات دورة المطالبة
    /// الكاملة معًا (تاريخ استحقاق، إشعار أول/ثاني، مطالبة أداء، إلغاء) بالإضافة لحالة السداد:
    /// - "مطالبة" = كل صف (سجل تاريخي لدورة التحصيل)، حالتها = "حالة المطالبة مختصرة"
    ///   (أو "حالة المطالبة")، وتاريخ إنجازها = "تاريخ الإلغاء" (إغلاق/تحصيل المطالبة).
    /// - "مديونية" = الصفوف التي حالة الدفعة فيها لا تزال "غير مدفوع" فقط (المبلغ لا يزال مستحقًا).
    ///
    /// أسماء الأعمدة الفعلية تُحدَّد أولاً بمطابقة حرفية دقيقة (Exact) مع مرشحين مرتّبين حسب
    /// الأولوية، ويُختار من بينهم العمود صاحب أعلى نسبة تعبئة فعلية بالبيانات. إن لم يتطابق أي
    /// مرشح حرفي (ملف بأسماء أعمدة مختلفة)، يُستخدم اكتشاف تقريبي بالكلمات المفتاحية كحل احتياطي
    /// (بنفس أسلوب DataImportService)، حتى يبقى الاستيراد صالحًا لأي ملف مختلف الشكل مستقبلاً.
    /// </summary>
    public class ClaimsDebtsImportService : IClaimsDebtsImportService
    {
        private readonly AppDbContext _db;
        private readonly IExcelDashboardService _excelService;

        private enum SheetKind { Claims, Debts, Ambiguous }

        private static readonly string[] ClaimSheetKeywords = { "مطالب", "claim" };
        private static readonly string[] DebtSheetKeywords = { "مديون", "دين", "debt" };

        // ===== مرشحات أسماء الأعمدة الحرفية (Exact) - مرتّبة حسب الأولوية =====
        private static readonly string[] ClaimNumberExact = { "رقم الطلب", "رقم المطالبة", "رقم إشعار المطالبة بالأداء", "مطالبات" };
        private static readonly string[] DebtNumberExact = { "رقم الفاتورة", "رقم الفاتوره", "مديونيات", "CONTRACT_ID", "رقم العقد" };
        // رقم العقد - مفتاح الربط بين مطالبة ومديونيتها (يُحجز أولاً بـ preferOrder حتى لا يلتقطه
        // رقم المطالبة/المديونية بدلاً منه). بعض الملفات تسمّيه "رقم العقد" وأخرى "CONTRACT_ID".
        private static readonly string[] ContractNumberExact = { "رقم العقد", "CONTRACT_ID" };
        private static readonly string[] DepartmentExact = { "البلدية", "الجهة", "الأمانة" };
        // البند: أولوية للاسم التفصيلي الحرفي "البند" (أدق دلاليًا) حتى لو تغطيته أقل من "نوع العقد"
        private static readonly string[] ItemCategoryExact = { "البند", "تصنيف البند", "نوع العقد", "نوع الفرصة" };
        // حالة المطالبة: بعض الملفات يكون عمودها معطوبًا بالكامل (قيم #N/A ناتجة عن معادلات
        // مكسورة بالملف المصدر) - نتخطّى أي عمود كهذا تلقائيًا (انظر IsUsableColumn) وننتقل
        // للمرشح التالي، لذا نضيف بدائل نصية سليمة كحلول احتياطية بنفس القائمة
        private static readonly string[] ClaimStatusExact = { "حالة المطالبة مختصرة", "حالة المطالبة", "BATCH_STATUS_AR", "حالة الدفعه", "حالة السداد" };
        // حالة الدفع نفسها ("غير مدفوع") تكون قيمة واحدة ثابتة لكل صفوف المديونيات (لأنها هي شرط
        // اعتبار الصف مديونية أصلاً)، فلا تصلح لرسم "توزيع حالات المديونيات". نُفضّل بدلاً منها
        // حالة المطالبة (مرحلة التحصيل الحالية: أول إشعار/مرسلة لمنصة سند/مرفوضة...) إن وُجدت،
        // فهي أكثر دلالة، مع إبقاء حالة الدفع كحل احتياطي أخير.
        private static readonly string[] DebtStatusExact = { "حالة المطالبة مختصرة", "حالة المطالبة", "حالة الدفعه", "BATCH_STATUS_AR", "حالة السداد" };
        private static readonly string[] ValueExact = { "قيمة الدفعة شامله قيمة الضريبة المضافة", "قيمة الدفعة بدون الضريبة", "القيمة", "المبلغ" };
        private static readonly string[] ClaimDateExact = { "تاريخ إستحقاق الدفعه", "تاريخ استحقاق الدفعه", "تاريخ المطالبة" };
        private static readonly string[] ActionDateExact = { "تاريخ الإشعار الأول", "تاريخ الاشعار الاول" };
        private static readonly string[] CompletionDateExact = { "تاريخ الإلغاء", "تاريخ الالغاء", "تاريخ الإنجاز", "تاريخ الإرسال الى تنفيذ" };
        private static readonly string[] DebtDateExact = { "تاريخ إستحقاق الدفعه", "تاريخ استحقاق الدفعه" };
        private static readonly string[] PaymentStatusColumnExact = { "حالة الدفعه", "BATCH_STATUS_AR", "حالة السداد" };

        // كلمات تدل على أن الدفعة/المديونية لم تُسدَّد بعد (تُطبَّق بعد إزالة التشكيل العربي)
        private static readonly string[] UnpaidKeywords = { "غير مدفوع", "غير مسدد", "غير محصل" };

        // ===== كلمات مفتاحية تقريبية (Fallback) - تُستخدم فقط إن لم يتطابق أي مرشح حرفي أعلاه =====
        private static readonly string[] ClaimNumberKeywords = { "رقم مطالبة", "claim no", "claim number", "الرقم المرجعي", "رقم المرجع", "reference" };
        private static readonly string[] DebtNumberKeywords = { "رقم مديونية", "رقم الدين", "debt no", "debt number" };
        private static readonly string[] DepartmentKeywords = { "الإدارة", "الادارة", "القسم", "department", "dept" };
        private static readonly string[] ClaimStatusKeywords = { "status" };
        private static readonly string[] ValueKeywords = { "قيمة", "المبلغ", "الإجمالي", "إجمالي", "amount", "value" };
        private static readonly string[] ClaimDateKeywords = { "تاريخ الطلب", "تاريخ الإنشاء", "تاريخ التقديم", "claim date" };
        private static readonly string[] CompletionDateKeywords = { "تاريخ الإغلاق", "تاريخ الانتهاء", "تاريخ الرد", "completion date", "close date" };
        private static readonly string[] DueDateKeywords = { "تاريخ الاستحقاق", "due date" };
        private static readonly string[] CollectionDateKeywords = { "تاريخ التحصيل", "تاريخ السداد", "collection date", "payment date" };

        private static readonly string[] TypeColumnKeywords = { "نوع البيان", "النوع", "نوع السجل", "type" };
        private static readonly string[] ClaimTypeValues = { "مطالب", "claim" };
        private static readonly string[] DebtTypeValues = { "مديون", "دين", "debt" };

        public ClaimsDebtsImportService(AppDbContext db, IExcelDashboardService excelService)
        {
            _db = db;
            _excelService = excelService;
        }

        public async Task<(bool Success, string Message, int ClaimsImported, int DebtsImported)> ImportAsync(Stream fileStream)
        {
            List<(string SheetName, AnalyzeResult Result)> sheets;
            try
            {
                sheets = _excelService.AnalyzeAllSheets(fileStream);
            }
            catch (Exception ex)
            {
                return (false, "تعذّرت قراءة الملف: " + ex.Message, 0, 0);
            }

            if (sheets.Count == 0)
                return (false, "الملف لا يحتوي على أي بيانات قابلة للاستيراد.", 0, 0);

            var claims = new List<ClaimRecord>();
            var debts = new List<DebtRecord>();

            foreach (var (sheetName, analyzed) in sheets)
            {
                var allColumnNames = analyzed.Columns.Select(c => c.Name).ToList();
                var kind = ClassifySheetByName(sheetName);

                // 1) ورقة باسم يحدد نوعًا واحدًا صراحة ("المطالبات"/"المديونيات") - أقوى إشارة ممكنة
                if (kind == SheetKind.Claims)
                {
                    claims.AddRange(BuildClaims(analyzed, analyzed.Rows));
                    continue;
                }
                if (kind == SheetKind.Debts)
                {
                    debts.AddRange(BuildDebts(analyzed, analyzed.Rows));
                    continue;
                }

                // 2) ورقة واحدة تحتوي عمود "النوع" يفصل صراحة بين مطالبة/مديونية
                var typeCol = DataImportService.FindColumn(allColumnNames, TypeColumnKeywords);
                if (typeCol != null)
                {
                    var claimRows = analyzed.Rows.Where(r => MatchesAny(DataImportService.GetString(r, typeCol), ClaimTypeValues)).ToList();
                    var debtRows = analyzed.Rows.Where(r => MatchesAny(DataImportService.GetString(r, typeCol), DebtTypeValues)).ToList();

                    if (claimRows.Count > 0) claims.AddRange(BuildClaims(analyzed, claimRows));
                    if (debtRows.Count > 0) debts.AddRange(BuildDebts(analyzed, debtRows));

                    var unclassified = analyzed.Rows.Except(claimRows).Except(debtRows).ToList();
                    if (unclassified.Count > 0) claims.AddRange(BuildClaims(analyzed, unclassified));
                    continue;
                }

                // 3) الحالة الأشيع: ورقة واحدة موحّدة (كشف دفعات/فواتير) بلا عمود نوع صريح، لكن بها
                // عمود "حالة سداد" لكل صف - كل الصفوف تُعتبر مطالبات (سجل تاريخي لدورة التحصيل)،
                // والصفوف غير المسدَّدة فقط تُعتبر أيضًا مديونيات قائمة حاليًا
                var paymentStatusCol = FindPaymentStatusColumn(allColumnNames);
                if (paymentStatusCol != null)
                {
                    claims.AddRange(BuildClaims(analyzed, analyzed.Rows));

                    var unpaidRows = analyzed.Rows.Where(r => IsUnpaid(DataImportService.GetString(r, paymentStatusCol))).ToList();
                    if (unpaidRows.Count > 0) debts.AddRange(BuildDebts(analyzed, unpaidRows));
                    continue;
                }

                // 4) لا يوجد عمود حالة سداد: نستدل من وجود عمود رقم مديونية/مطالبة حصريًا
                bool hasDebtNumberCol = ExactMatch(allColumnNames, DebtNumberExact) != null;
                bool hasClaimNumberCol = ExactMatch(allColumnNames, ClaimNumberExact) != null;
                if (hasDebtNumberCol && !hasClaimNumberCol)
                {
                    debts.AddRange(BuildDebts(analyzed, analyzed.Rows));
                    continue;
                }

                // 5) لا يوجد أي مؤشر واضح: تُعتبر الورقة بأكملها مطالبات (البيانات الأساسية الأولى بالطلب)
                claims.AddRange(BuildClaims(analyzed, analyzed.Rows));
            }

            if (claims.Count == 0 && debts.Count == 0)
            {
                return (false, "تعذّر التعرف على أي بيانات مطالبات أو مديونيات صالحة بالملف.", 0, 0);
            }

            // استبدال كامل: تعكس اللوحات دائمًا آخر نسخة مرفوعة من الملف
            await _db.ClaimRecords.ExecuteDeleteAsync();
            await _db.DebtRecords.ExecuteDeleteAsync();

            if (claims.Count > 0) await _db.ClaimRecords.AddRangeAsync(claims);
            if (debts.Count > 0) await _db.DebtRecords.AddRangeAsync(debts);
            await _db.SaveChangesAsync();

            var msg = $"تم الاستيراد بنجاح: {claims.Count} مطالبة و{debts.Count} مديونية.";
            return (true, msg, claims.Count, debts.Count);
        }

        /// <summary>يصنّف الورقة فقط بالاعتماد على اسمها الصريح ("المطالبات"/"المديونيات") - أي إشارة
        /// أخرى (عمود نوع، عمود حالة سداد، أسماء أعمدة) تُفحص لاحقًا في الحلقة الرئيسية.</summary>
        private static SheetKind ClassifySheetByName(string sheetName)
        {
            var normalizedName = sheetName.Trim();
            if (ClaimSheetKeywords.Any(k => normalizedName.Contains(k, StringComparison.OrdinalIgnoreCase))
                && !DebtSheetKeywords.Any(k => normalizedName.Contains(k, StringComparison.OrdinalIgnoreCase)))
                return SheetKind.Claims;

            if (DebtSheetKeywords.Any(k => normalizedName.Contains(k, StringComparison.OrdinalIgnoreCase)))
                return SheetKind.Debts;

            return SheetKind.Ambiguous;
        }

        private static string? FindPaymentStatusColumn(List<string> allColumnNames)
            => PickBestColumnName(allColumnNames, PaymentStatusColumnExact);

        private static bool IsUnpaid(string? statusValue)
        {
            if (string.IsNullOrWhiteSpace(statusValue)) return false;
            if (statusValue.Trim().Equals("Not Paid", StringComparison.OrdinalIgnoreCase)) return true;
            var stripped = ClaimsDebtsAnalyticsService.StripArabicDiacritics(statusValue);
            return UnpaidKeywords.Any(k => stripped.Contains(k));
        }

        private static bool MatchesAny(string? value, string[] keywords)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return keywords.Any(k => value.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>يبحث عن أول اسم عمود فعلي يطابق أحد المرشحين الحرفيين تمامًا (بعد التطبيع)، بلا اعتبار للتغطية.</summary>
        private static string? ExactMatch(List<string> allColumnNames, string[] exactCandidates)
        {
            foreach (var candidate in exactCandidates)
            {
                var col = allColumnNames.FirstOrDefault(c => DataImportService.Normalize(c) == DataImportService.Normalize(candidate));
                if (col != null) return col;
            }
            return null;
        }

        private static string? PickBestColumnName(List<string> allColumnNames, string[] exactCandidates)
            => ExactMatch(allColumnNames, exactCandidates);

        // قيم خطأ إكسل الشائعة (ناتجة عادةً عن معادلات VLOOKUP/INDEX مكسورة بالملف المصدر) -
        // تُعامَل كأنها فارغة عند اختيار الأعمدة، حتى لا يُختار عمود قيمته بالكامل تقريبًا "#N/A"
        // فقط لأنه يطابق اسم المرشح المفضّل حرفيًا
        private static readonly HashSet<string> ExcelErrorTokens = new(StringComparer.OrdinalIgnoreCase)
        {
            "#N/A", "#REF!", "#VALUE!", "#DIV/0!", "#NAME?", "#NULL!", "#NUM!"
        };

        private static bool IsUsableValue(string? v)
            => !string.IsNullOrWhiteSpace(v) && !ExcelErrorTokens.Contains(v.Trim());

        /// <summary>يحوّل قيمة الحالة إلى null إن كانت خطأ إكسل (#N/A...) أو فارغة، بدلاً من تخزين
        /// النص الحرفي للخطأ - يُطبَّق على مستوى كل قيمة (وليس فقط عند اختيار العمود) لأن عمودًا
        /// "موثوقًا" إجمالاً قد تتخلله بعض قيم #N/A ضمن الأقلية غير الموثوقة.</summary>
        private static string? SanitizeStatusValue(string? v)
        {
            if (string.IsNullOrWhiteSpace(v)) return null;
            var trimmed = v.Trim();
            return ExcelErrorTokens.Contains(trimmed) ? null : trimmed;
        }

        /// <summary>
        /// عمود "موثوق" لو نصف قيمه المعبّأة على الأقل ليست أخطاء إكسل. يرفض أعمدة مثل عمود حالة
        /// معطوب جزئيًا بمعادلة VLOOKUP مكسورة (تكون فيه بعض القيم الحقيقية وسط أغلبية #N/A) والتي
        /// كانت تمر سابقًا لمجرد وجود قيمة صالحة واحدة على الأقل.
        /// </summary>
        private static bool IsReliableColumn(List<Dictionary<string, object?>> rows, string col)
        {
            int usable = 0, nonNull = 0;
            foreach (var r in rows)
            {
                var v = DataImportService.GetString(r, col);
                if (string.IsNullOrWhiteSpace(v)) continue;
                nonNull++;
                if (!ExcelErrorTokens.Contains(v.Trim())) usable++;
            }
            return nonNull == 0 || usable >= nonNull / 2.0;
        }

        /// <summary>يختار أفضل عمود لحقل معيّن: من بين المرشحين الحرفيين الموجودين فعليًا بالملف، يُختار
        /// صاحب أعلى نسبة تعبئة بالصفوف المعطاة (حتى لو تعدد الأعمدة المرشحة المتطابقة بالاسم
        /// تقريبيًا). إن لم يوجد أي مرشح حرفي، يُستخدم البحث التقريبي بالكلمات المفتاحية كحل أخير.
        /// عند preferOrder=true يُختار أول مرشح مطابق حسب ترتيب الأولوية المعطى بصرف النظر عن
        /// نسبة التعبئة (يُستخدم لحقول مثل "الحالة" حيث الترتيب اليدوي أهم من مجرد اكتمال البيانات:
        /// عمود مثل "حالة الدفعه" قد يكون معبّأ بنسبة 100% بالتعريف نفسه (لأنه شرط اختيار الصفوف
        /// أصلاً) رغم أن عمودًا آخر أدق دلاليًا مثل "حالة المطالبة مختصرة" هو المقصود فعليًا) - لكن
        /// أي مرشح كل قيمه أخطاء إكسل (#N/A) يُتخطّى تلقائيًا للمرشح التالي بصرف النظر عن preferOrder.
        /// عند requireReliable=false يُتخطّى شرط نسبة الموثوقية (٪50 قيم سليمة) مع إبقاء شرط وجود
        /// قيمة صالحة واحدة على الأقل فقط - يُستخدم لعمود حالة المطالبة تحديدًا، حيث قيم #N/A
        /// الكثيرة فيه لها معنى فعلي (بلا حالة/بلا مطالبة) وليست عمودًا يجب تجاهله بالكامل.
        /// </summary>
        private static string? PickBestColumn(
            List<string> allColumnNames,
            List<Dictionary<string, object?>> rows,
            string[] exactCandidates,
            string[]? fallbackKeywords,
            HashSet<string> excludeCols,
            bool preferOrder = false,
            bool requireReliable = true)
        {
            var matched = exactCandidates
                .Select(candidate => allColumnNames.FirstOrDefault(c =>
                    !excludeCols.Contains(c) && DataImportService.Normalize(c) == DataImportService.Normalize(candidate)))
                .Where(c => c != null)
                .Select(c => c!)
                .Distinct()
                .Where(c => rows.Count == 0 || (rows.Any(r => IsUsableValue(DataImportService.GetString(r, c))) && (!requireReliable || IsReliableColumn(rows, c))))
                .ToList();

            if (matched.Count > 0)
            {
                if (preferOrder) return matched.First();

                return matched
                    .OrderByDescending(c => rows.Count(r => IsUsableValue(DataImportService.GetString(r, c))))
                    .First();
            }

            if (fallbackKeywords != null)
            {
                var remaining = allColumnNames.Where(c => !excludeCols.Contains(c)).ToList();
                return DataImportService.FindColumn(remaining, fallbackKeywords);
            }

            return null;
        }

        private static List<ClaimRecord> BuildClaims(AnalyzeResult analyzed, List<Dictionary<string, object?>> rows)
        {
            var allColumnNames = analyzed.Columns.Select(c => c.Name).ToList();
            var used = new HashSet<string>();

            string? Pick(string[] exact, string[]? fallback, bool preferOrder = false, bool requireReliable = true)
            {
                var col = PickBestColumn(allColumnNames, rows, exact, fallback, used, preferOrder, requireReliable);
                if (col != null) used.Add(col);
                return col;
            }

            string? contractCol = Pick(ContractNumberExact, null, preferOrder: true);
            string? numberCol = Pick(ClaimNumberExact, ClaimNumberKeywords);
            string? deptCol = Pick(DepartmentExact, DepartmentKeywords);
            string? statusCol = Pick(ClaimStatusExact, ClaimStatusKeywords, preferOrder: true, requireReliable: false);
            string? itemCol = Pick(ItemCategoryExact, null, preferOrder: true);
            string? valueCol = Pick(ValueExact, ValueKeywords);
            string? claimDateCol = Pick(ClaimDateExact, ClaimDateKeywords);
            string? actionDateCol = Pick(ActionDateExact, null);
            string? completionDateCol = Pick(CompletionDateExact, CompletionDateKeywords);

            var list = new List<ClaimRecord>();
            foreach (var row in rows)
            {
                list.Add(new ClaimRecord
                {
                    ClaimNumber = numberCol != null ? DataImportService.GetString(row, numberCol) : null,
                    ContractNumber = contractCol != null ? DataImportService.GetString(row, contractCol)?.Trim() : null,
                    Department = deptCol != null ? DataImportService.GetString(row, deptCol)?.Trim() : null,
                    Status = statusCol != null ? SanitizeStatusValue(DataImportService.GetString(row, statusCol)) : null,
                    ItemCategory = itemCol != null ? DataImportService.GetString(row, itemCol)?.Trim() : null,
                    Value = valueCol != null ? DataImportService.GetDecimal(row, valueCol) : null,
                    ClaimDate = claimDateCol != null ? DataImportService.GetDate(row, claimDateCol) : null,
                    ActionDate = actionDateCol != null ? DataImportService.GetDate(row, actionDateCol) : null,
                    CompletionDate = completionDateCol != null ? DataImportService.GetDate(row, completionDateCol) : null,
                    RawJson = JsonSerializer.Serialize(row)
                });
            }
            return list;
        }

        private static List<DebtRecord> BuildDebts(AnalyzeResult analyzed, List<Dictionary<string, object?>> rows)
        {
            var allColumnNames = analyzed.Columns.Select(c => c.Name).ToList();
            var used = new HashSet<string>();

            string? Pick(string[] exact, string[]? fallback, bool preferOrder = false)
            {
                var col = PickBestColumn(allColumnNames, rows, exact, fallback, used, preferOrder);
                if (col != null) used.Add(col);
                return col;
            }

            string? contractCol = Pick(ContractNumberExact, null, preferOrder: true);
            string? numberCol = Pick(DebtNumberExact, DebtNumberKeywords);
            string? deptCol = Pick(DepartmentExact, DepartmentKeywords);
            string? statusCol = Pick(DebtStatusExact, null, preferOrder: true);
            string? itemCol = Pick(ItemCategoryExact, null, preferOrder: true);
            string? valueCol = Pick(ValueExact, ValueKeywords);
            string? collectedCol = Pick(new[] { "المبلغ المحصل", "المحصل" }, null);
            string? debtDateCol = Pick(DebtDateExact, null);
            string? dueDateCol = Pick(new[] { "تاريخ الاستحقاق" }, DueDateKeywords);
            string? collectionDateCol = Pick(new[] { "تاريخ التحصيل", "تاريخ السداد" }, CollectionDateKeywords);

            var list = new List<DebtRecord>();
            foreach (var row in rows)
            {
                list.Add(new DebtRecord
                {
                    DebtNumber = numberCol != null ? DataImportService.GetString(row, numberCol) : null,
                    ContractNumber = contractCol != null ? DataImportService.GetString(row, contractCol)?.Trim() : null,
                    Department = deptCol != null ? DataImportService.GetString(row, deptCol)?.Trim() : null,
                    Status = statusCol != null ? SanitizeStatusValue(DataImportService.GetString(row, statusCol)) : null,
                    ItemCategory = itemCol != null ? DataImportService.GetString(row, itemCol)?.Trim() : null,
                    Value = valueCol != null ? DataImportService.GetDecimal(row, valueCol) : null,
                    CollectedValue = collectedCol != null ? DataImportService.GetDecimal(row, collectedCol) : null,
                    DebtDate = debtDateCol != null ? DataImportService.GetDate(row, debtDateCol) : null,
                    DueDate = dueDateCol != null ? DataImportService.GetDate(row, dueDateCol) : (debtDateCol != null ? DataImportService.GetDate(row, debtDateCol) : null),
                    CollectionDate = collectionDateCol != null ? DataImportService.GetDate(row, collectionDateCol) : null,
                    RawJson = JsonSerializer.Serialize(row)
                });
            }
            return list;
        }
    }
}
