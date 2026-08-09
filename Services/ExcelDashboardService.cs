using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ClosedXML.Excel;
using ExcelDashboard.Models;

namespace ExcelDashboard.Services
{
    public class ExcelDashboardService : IExcelDashboardService
    {
        private enum ColType { Numeric, Date, Text, Empty }

        // أقصى عدد صفوف يُعالَج من الملف (سقف أمان عالٍ يستوعب الملفات الضخمة مثل كشوف
        // الفواتير التفصيلية التي قد تتجاوز مئات الآلاف من الصفوف). رُفِع من 5000 لأن الحد
        // القديم كان يقتطع الملف ويستورد أول 5000 صف فقط (وغالبًا تكون كلها لنطاق واحد).
        private const int MaxRows = 500_000;

        // أقصى عدد صفوف يتم فحصها بحثًا عن صف العناوين الحقيقي
        private const int HeaderScanLimit = 20;

        // كلمات مفتاحية تُستخدم لاكتشاف عمود "الهدف" (Goal/Target) بشكل تقريبي
        private static readonly string[] GoalKeywords =
        {
            "هدف", "target", "goal", "كهدف", "المستهدف", "مستهدف"
        };

        public AnalyzeResult AnalyzeFile(Stream fileStream)
        {
            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                return new AnalyzeResult { Success = false, Message = "الملف لا يحتوي على أي ورقة عمل (Sheet)." };
            }

            return AnalyzeWorksheet(worksheet);
        }

        /// <summary>
        /// يحلّل كل أوراق العمل (Sheets) بالملف بنفس منطق AnalyzeFile لكل ورقة على حدة، ويعيد
        /// فقط الأوراق التي تحتوي فعلاً على بيانات صالحة (مع اسم كل ورقة). تُستخدم من الملفات
        /// التي تحتوي على أكثر من جدول مستقل بنفس المصنّف (مثل ورقة "المطالبات" وورقة
        /// "المديونيات" في نفس ملف Excel)، بدون تكرار منطق القراءة/اكتشاف الأعمدة أعلاه.
        /// </summary>
        public List<(string SheetName, AnalyzeResult Result)> AnalyzeAllSheets(Stream fileStream)
        {
            var results = new List<(string SheetName, AnalyzeResult Result)>();

            using var workbook = new XLWorkbook(fileStream);
            foreach (var worksheet in workbook.Worksheets)
            {
                var analyzed = AnalyzeWorksheet(worksheet);
                if (analyzed.Success)
                {
                    results.Add((worksheet.Name, analyzed));
                }
            }

            return results;
        }

        private static AnalyzeResult AnalyzeWorksheet(IXLWorksheet worksheet)
        {
            var result = new AnalyzeResult();

            // نستخدم Firs/LastRowUsed مباشرة بدل RangeUsed() لتحديد حدود الجدول: RangeUsed() يمر
            // داخليًا على كل الخلايا المستخدَمة ويرتّبها (GetUsedCells + OrderBy) لبناء المدى، وهو
            // ما يستهلك ذاكرة ضخمة جدًا مع ملفات كبيرة (مئات آلاف الصفوف × عشرات الأعمدة = ملايين
            // الخلايا) ويؤدي لخطأ OutOfMemoryException. الطريقتان أدناه تعتمدان على فهرس الصفوف/
            // الأعمدة المستخدَمة مباشرة بدون أي فرز، بنفس النتيجة تمامًا وبتكلفة أقل بكثير.
            var firstRowUsed = worksheet.FirstRowUsed();
            var lastRowUsed = worksheet.LastRowUsed();
            var firstColUsed = worksheet.FirstColumnUsed();
            var lastColUsed = worksheet.LastColumnUsed();
            if (firstRowUsed == null || lastRowUsed == null || firstColUsed == null || lastColUsed == null)
            {
                result.Success = false;
                result.Message = "الملف فارغ ولا يحتوي على بيانات.";
                return result;
            }

            int firstRow = firstRowUsed.RowNumber();
            int lastRow = lastRowUsed.RowNumber();
            int firstCol = firstColUsed.ColumnNumber();
            int lastCol = lastColUsed.ColumnNumber();
            int columnCount = lastCol - firstCol + 1;

            // ---------- 1) اكتشاف صف العناوين الحقيقي ----------
            // (يحل مشكلة الملفات التي تحتوي على عنوان/شعار شركة/خلايا مدمجة قبل الجدول الفعلي)
            int headerRow = DetectHeaderRow(worksheet, firstRow, lastRow, firstCol, lastCol, columnCount);

            if (headerRow >= lastRow)
            {
                result.Success = false;
                result.Message = "تم العثور على صف عناوين لكن لا توجد بيانات أسفله.";
                return result;
            }

            // ---------- 2) قراءة أسماء الأعمدة مع ضمان عدم تكرارها ----------
            var headers = new List<string>();
            var usedNames = new HashSet<string>();
            for (int c = firstCol; c <= lastCol; c++)
            {
                string name = worksheet.Cell(headerRow, c).GetString().Trim();
                if (string.IsNullOrWhiteSpace(name)) name = $"عمود {c - firstCol + 1}";

                string finalName = name;
                int suffix = 2;
                while (!usedNames.Add(finalName))
                {
                    finalName = $"{name} ({suffix})";
                    suffix++;
                }
                headers.Add(finalName);
            }

            int dataFirstRow = headerRow + 1;
            int totalDataRows = lastRow - dataFirstRow + 1;
            int rowsToProcess = Math.Min(totalDataRows, MaxRows);
            bool truncated = totalDataRows > MaxRows;

            // ---------- 3) قراءة الخلايا في جدول وسيط ----------
            var raw = new object?[rowsToProcess, columnCount];
            var numberCounts = new int[columnCount];
            var dateCounts = new int[columnCount];
            var textCounts = new int[columnCount];

            // نقرأ صفًا صفًا عبر مُعداد الصفوف (بدل الوصول لكل خلية عبر worksheet.Cell(r,c))
            // لأن الوصول على مستوى الصف أسرع بكثير مع الملفات الكبيرة (مئات آلاف الصفوف).
            int lastDataRow = dataFirstRow + rowsToProcess - 1;
            int rIndex = 0;
            foreach (var xlRow in worksheet.Rows(dataFirstRow, lastDataRow))
            {
                for (int c = 0; c < columnCount; c++)
                {
                    var cell = xlRow.Cell(firstCol + c);
                    var (type, value) = ReadCell(cell);

                    raw[rIndex, c] = value;
                    switch (type)
                    {
                        case ColType.Numeric: numberCounts[c]++; break;
                        case ColType.Date: dateCounts[c]++; break;
                        case ColType.Text: textCounts[c]++; break;
                    }
                }
                rIndex++;
            }

            // ---------- 4) تحديد نوع كل عمود حسب الأغلبية ----------
            var columnTypes = new ColType[columnCount];
            for (int c = 0; c < columnCount; c++)
            {
                int n = numberCounts[c], d = dateCounts[c], t = textCounts[c];
                if (n == 0 && d == 0 && t == 0) { columnTypes[c] = ColType.Empty; continue; }

                if (n >= d && n >= t) columnTypes[c] = ColType.Numeric;
                else if (d >= n && d >= t) columnTypes[c] = ColType.Date;
                else columnTypes[c] = ColType.Text;
            }

            // ---------- 5) إعادة تطبيع القيم: أي قيمة لا تطابق نوع عمودها تصبح null ----------
            for (int c = 0; c < columnCount; c++)
            {
                for (int r = 0; r < rowsToProcess; r++)
                {
                    var val = raw[r, c];
                    if (val == null) continue;

                    bool matches = columnTypes[c] switch
                    {
                        ColType.Numeric => val is double,
                        ColType.Date => val is DateTime,
                        ColType.Text => val is string,
                        _ => false
                    };
                    if (!matches) raw[r, c] = null;
                }
            }

            // ---------- 6) بناء قائمة الأعمدة المعتمدة (تجاهل الأعمدة الفارغة بالكامل) ----------
            var keptColumns = new List<int>();
            for (int c = 0; c < columnCount; c++)
            {
                if (columnTypes[c] != ColType.Empty) keptColumns.Add(c);
            }

            if (keptColumns.Count == 0)
            {
                result.Success = false;
                result.Message = "لم يتم العثور على أي بيانات صالحة في الملف.";
                return result;
            }

            foreach (var c in keptColumns)
            {
                string typeStr = columnTypes[c] switch
                {
                    ColType.Numeric => ColumnTypes.Numeric,
                    ColType.Date => ColumnTypes.Date,
                    _ => ColumnTypes.Text
                };
                result.Columns.Add(new ColumnMeta { Name = headers[c], Type = typeStr });
            }

            // ---------- 7) بناء الصفوف كقواميس (Dictionary) جاهزة لإرسالها كـ JSON ----------
            for (int r = 0; r < rowsToProcess; r++)
            {
                var rowDict = new Dictionary<string, object?>();
                foreach (var c in keptColumns)
                {
                    object? cellVal = raw[r, c];
                    if (cellVal is DateTime dt)
                    {
                        rowDict[headers[c]] = dt.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        rowDict[headers[c]] = cellVal;
                    }
                }
                result.Rows.Add(rowDict);
            }

            // ---------- 8) اكتشاف عمود "الهدف" تقريبيًا (للاستخدام في مؤشر Gauge) ----------
            var numericHeaders = result.Columns.Where(c => c.Type == ColumnTypes.Numeric).Select(c => c.Name).ToList();
            result.GoalColumnName = numericHeaders.FirstOrDefault(h =>
                GoalKeywords.Any(k => h.ToLower().Contains(k)));

            result.TotalRows = rowsToProcess;
            result.Truncated = truncated;
            result.Success = true;

            var msg = new StringBuilder();
            msg.Append($"تم تحليل الملف بنجاح. عدد السجلات: {rowsToProcess}.");
            if (truncated) msg.Append($" (تم الاقتصار على أول {MaxRows} سجل من أصل {totalDataRows} لضبط الأداء).");
            result.Message = msg.ToString();

            return result;
        }

        /// <summary>
        /// يحاول العثور على صف العناوين الحقيقي بدلًا من افتراض أنه دائمًا أول صف.
        /// يبحث عن أول صف "ممتلئ" أغلب خلاياه نصية، يليه مباشرة صف فيه قيمة رقمية أو تاريخ واحدة على الأقل.
        /// </summary>
        private static int DetectHeaderRow(IXLWorksheet worksheet, int firstRow, int lastRow, int firstCol, int lastCol, int columnCount)
        {
            int scanLimit = Math.Min(firstRow + HeaderScanLimit - 1, lastRow - 1);

            for (int r = firstRow; r <= scanLimit; r++)
            {
                int nonEmptyInRow = 0;
                for (int c = firstCol; c <= lastCol; c++)
                {
                    if (!worksheet.Cell(r, c).IsEmpty()) nonEmptyInRow++;
                }

                // صف العناوين المرشح يجب أن يكون ممتلئًا بنسبة معقولة (وليس عنوانًا في خلية واحدة فقط)
                if (nonEmptyInRow < Math.Max(2, columnCount / 2)) continue;

                // الصف التالي يجب أن يحتوي على بيانات حقيقية (رقم أو تاريخ على الأقل)
                int nextRow = r + 1;
                bool nextRowHasData = false;
                for (int c = firstCol; c <= lastCol; c++)
                {
                    var (type, _) = ReadCell(worksheet.Cell(nextRow, c));
                    if (type == ColType.Numeric || type == ColType.Date)
                    {
                        nextRowHasData = true;
                        break;
                    }
                }

                if (nextRowHasData) return r;
            }

            // لم يتم العثور على مرشح واضح -> الافتراضي هو أول صف (كما في الملفات البسيطة)
            return firstRow;
        }

        /// <summary>
        /// يقرأ خلية واحدة ويحدد نوعها الفعلي + القيمة المُطبّعة (double / DateTime / string)
        /// مع دعم تنسيقات العملة والفواصل والأرقام العربية.
        /// </summary>
        private static (ColType type, object? value) ReadCell(IXLCell cell)
        {
            if (cell.IsEmpty()) return (ColType.Empty, null);

            if (cell.DataType == XLDataType.Number)
            {
                return (ColType.Numeric, cell.GetDouble());
            }

            if (cell.DataType == XLDataType.DateTime)
            {
                return (ColType.Date, cell.GetDateTime());
            }

            if (cell.DataType == XLDataType.Boolean)
            {
                return (ColType.Text, cell.GetBoolean().ToString());
            }

            string text = cell.GetString().Trim();
            if (string.IsNullOrWhiteSpace(text)) return (ColType.Empty, null);

            if (TryParseFlexibleNumber(text, out double numVal))
            {
                return (ColType.Numeric, numVal);
            }

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateVal))
            {
                return (ColType.Date, dateVal);
            }

            return (ColType.Text, text);
        }

        // كلمات عملة عربية شائعة يتم تجاهلها عند تحليل الأرقام النصية
        private static readonly string[] ArabicCurrencyWords = { "ريال", "ر.س", "جنيه", "درهم", "دينار", "ر س" };

        /// <summary>
        /// يحاول تحويل نص إلى رقم بعد تنظيفه من رموز العملة والفواصل ومسافات التنسيق،
        /// وتحويل الأرقام العربية (٠-٩) إلى أرقام لاتينية إن وُجدت.
        /// يرفض فورًا أي نص يحتوي على حروف لاتينية (لتجنب تحويل أكواد مثل "A1" أو "INV-001" إلى أرقام خطأً).
        /// </summary>
        private static bool TryParseFlexibleNumber(string text, out double result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;

            // رفض أي نص يحتوي على حروف لاتينية (أكواد/نصوص عادية وليست أرقامًا)
            if (text.Any(ch => (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z'))) return false;

            string working = text.Trim();

            // إزالة كلمات العملة العربية الشائعة
            foreach (var word in ArabicCurrencyWords)
            {
                working = working.Replace(word, string.Empty);
            }

            // تحويل الأرقام العربية (٠-٩) إلى أرقام لاتينية
            var sb = new StringBuilder(working.Length);
            foreach (char ch in working)
            {
                sb.Append(ch >= '٠' && ch <= '٩' ? (char)('0' + (ch - '٠')) : ch);
            }
            working = sb.ToString().Trim();

            // صيغة المحاسبة للأرقام السالبة: (1,234.56) -> -1234.56
            bool isAccountingNegative = working.StartsWith("(") && working.EndsWith(")");
            if (isAccountingNegative) working = working.Substring(1, working.Length - 2);

            // إزالة رموز التنسيق: فواصل الآلاف، النسبة، العملات الرمزية، المسافات
            working = working.Replace(",", "")
                              .Replace("٬", "")
                              .Replace("\u00A0", "")
                              .Replace(" ", "")
                              .Replace("%", "")
                              .Replace("$", "")
                              .Replace("€", "")
                              .Replace("£", "")
                              .Replace("¥", "");

            if (isAccountingNegative) working = "-" + working;

            // التحقق النهائي: يجب أن يكون الناتج رقمًا صحيحًا بالكامل (وليس مزيجًا مع رموز غير معروفة)
            if (string.IsNullOrWhiteSpace(working)) return false;
            if (!System.Text.RegularExpressions.Regex.IsMatch(working, @"^[+-]?\d+(\.\d+)?$")) return false;

            return double.TryParse(working, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result);
        }
    }
}
