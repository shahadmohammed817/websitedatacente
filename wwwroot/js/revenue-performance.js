/* ==========================================================================
   قسم "أداء الإيرادات حسب نوع الإيراد" - يُضاف مباشرة بعد بطاقات الـKPI وقبل باقي رسوم
   اللوحة المشتركة (لا يعدّل أو يحذف أي رسم موجود). يعتمد على الدوال المشتركة المعرّفة أصلاً
   بـ section-dashboard.js (formatNum, formatCompact, escapeHtml, C, chartRegistry) - يُحمَّل
   بعده بنفس الصفحة، ولا يظهر إلا بالصفحة الرئيسية (Views/Home/Index.cshtml هي الوحيدة التي
   ما زالت تُحمّل هذا الملف).

   المحتوى:
   0) 3 بطاقات صغيرة أعلى القسم: المحصل الحالي (آخر سنة بالبيانات) / المحصل السابق (السنة
      التي قبلها) / نسبة نمو الإيرادات بينهما.
   1) عمود واحد لكل بند، مقسوم لنصفين (محصل/مفوتر + المتبقي من المستهدف)، بالنسبة المئوية،
      مع خط "المستهدف" فوق كل عمود يعرض قيمة المستهدف الفعلية لذلك البند.
      - مصدر "وزارة المالية": المستهدف يُقارَن بـ"المحصّل"
      - مصدر "وزارة البلديات والإسكان": المستهدف يُقارَن بـ"المفوتر"
   ========================================================================== */

// أيقونات Bootstrap لكل نوع إيراد (تصنيف البند) - افتراضي محفظة نقدية لأي تصنيف غير مُدرَج
const REVENUE_TYPE_ICONS = {
    "استثمار": "bi-building",
    "تأجير مؤقت": "bi-key",
    "رسوم": "bi-receipt",
    "جزاءات": "bi-exclamation-triangle",
    "رسوم المبيعات-الايواء": "bi-door-open",
    "رسوم المبيعات-التبغ": "bi-shop",
    "إيرادات مختلفة": "bi-collection"
};
function revenueTypeIcon(label) {
    return REVENUE_TYPE_ICONS[label] || "bi-cash-stack";
}

// formatNum المشتركة (section-dashboard.js) تقرّب لأقرب رقم صحيح (Math.round) - هذا القسم تحديدًا
// يعرض القيمة كاملة بلا أي تقريب (كما تُجمَع بإكسل مباشرة)، بمنزلتين عشريتين، بناءً على طلب صريح
function rpFormatNum(n) {
    return Number.isFinite(n) ? n.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : "0.00";
}

// نفس الأسماء الرسمية المستخدمة بصفحتَي "مستهدفات وزارة المالية"/"مستهدفات وزارة البلديات
// والإسكان" (Views/Dashboard/TargetsComparisonFinance.cshtml / TargetsComparisonBaladiyah.cshtml)
const RP_SOURCE_LABELS = { finance: "وزارة المالية", baladiyah: "وزارة البلديات والإسكان" };
let revenuePerfSource = "finance"; // "finance" | "baladiyah" - يبقى محفوظًا بين إعادة الرسم
let revenuePerfData = null;
let revenuePerfChart = null;          // شكل 1: عمود مقسوم (محصل/مفوتر + متبقي) + خط المستهدف

function removeRevenuePerfSection() {
    const el = document.getElementById("revenuePerfSection");
    if (el) el.remove();
}

// اللون الفاتح لعمود "المتبقي من المستهدف" (رمادي فاتح محايد فوق عمود "المحصّل" الداكن)
const RP_REMAINING_COLOR = "#E3E9E7";

/* ==========================================================================
   شكل 1: عمود واحد لكل بند مقسوم لنصفين (محصل/مفوتر + المتبقي من المستهدف) بالنسبة
   ========================================================================== */
function buildRevenuePerfChartOption(rows) {
    const isFinance = revenuePerfSource === "finance";
    const targetKey = isFinance ? "targetFinance" : "targetBaladiyah";
    const basisKey = isFinance ? "collected" : "invoiced";
    const basisLabel = isFinance ? "المحصل" : "المفوتر";

    const labels = rows.map(function (r) { return r.itemLabel; });
    const targets = rows.map(function (r) { return r[targetKey] || 0; });
    const basisValues = rows.map(function (r) { return r[basisKey] || 0; });

    // نسبة الأساس من المستهدف = الأساس (محصل أو مفوتر حسب المصدر) ÷ المستهدف × 100
    const pct = rows.map(function (r, i) {
        return targets[i] > 0 ? Math.round((basisValues[i] / targets[i]) * 1000) / 10 : null;
    });
    // ارتفاع العمود الداكن داخل الشريط المكدَّس = النسبة نفسها، محدود بحد أقصى 100% حتى يمثّل
    // الشريط بكامل ارتفاعه (100%) المستهدف نفسه، حتى لو تجاوز الأساس المستهدف
    const basisOfTargetPct = pct.map(function (v) { return v === null ? 0 : Math.min(v, 100); });
    const remainingOfTargetPct = basisOfTargetPct.map(function (v) { return Math.max(Math.round((100 - v) * 10) / 10, 0); });

    // موضع خط "المستهدف" (شريط علوي منفصل فوق كل الأعمدة، بنطاق 105%-125%) يعكس الحجم
    // النسبي الحقيقي لمستهدف كل بند مقارنة ببقية البنود (بند بمستهدف أكبر = نقطة أعلى) -
    // بدل خط مستقيم بلا معنى بصري، مع بقاء الرقم الحقيقي مكتوبًا فوق كل نقطة دائمًا
    const positiveTargets = targets.filter(function (t) { return t > 0; });
    const minTarget = positiveTargets.length ? Math.min.apply(null, positiveTargets) : 0;
    const maxTarget = positiveTargets.length ? Math.max.apply(null, positiveTargets) : 0;
    const targetRange = maxTarget - minTarget;
    const targetLineY = targets.map(function (t) {
        if (t <= 0) return null;
        if (targetRange === 0) return 115;
        return Math.round((105 + ((t - minTarget) / targetRange) * 20) * 10) / 10;
    });

    return {
        tooltip: tooltipStyle({
            trigger: "axis", axisPointer: { type: "shadow" },
            formatter: function (params) {
                const idx = params[0].dataIndex;
                const r = rows[idx];
                let html = '<div style="font-weight:700;margin-bottom:6px;">' + escapeHtml(labels[idx]) + '</div>';
                html += '<div>المستهدف: <strong>' + rpFormatNum(targets[idx]) + '</strong></div>';
                html += '<div>' + basisLabel + ': <strong>' + rpFormatNum(r[basisKey] || 0) + '</strong></div>';
                html += '<div style="margin-top:4px;border-top:1px solid ' + C.grid + ';padding-top:4px;">';
                html += 'نسبة تحقيق المستهدف: <strong>' + (pct[idx] !== null ? pct[idx] + "%" : "—") + '</strong>';
                html += '</div>';
                return html;
            }
        }),
        legend: { bottom: 0, data: [basisLabel, "المتبقي من المستهدف", "المستهدف"], textStyle: Object.assign({}, AXIS_LABEL_STYLE), itemWidth: 12, itemHeight: 12 },
        grid: { left: 50, right: 46, top: 40, bottom: 76 },
        xAxis: axisStyle({ type: "category", data: labels, axisLabel: { interval: 0, rotate: labels.length > 5 ? 25 : 0, fontSize: 11 } }),
        yAxis: axisStyle({ type: "value", max: 130, interval: 20, axisLabel: { formatter: function (v) { return v <= 100 ? v + "%" : ""; } } }),
        series: [
            {
                // التسمية داخل لون الشريط الداكن نفسه - اسم المؤشّر (محصل/مفوتر) + نسبته من
                // المستهدف معًا، حتى يتضح المقصود بكل لون بلا رجوع للأسفل (Legend)
                name: basisLabel, type: "bar", stack: "target", data: basisOfTargetPct, barMaxWidth: 46, z: 1,
                itemStyle: { color: C.dark },
                label: {
                    show: true, position: "inside", fontSize: 10, fontWeight: 700, color: "#fff", lineHeight: 14,
                    formatter: function (p) {
                        const real = pct[p.dataIndex];
                        if (real === null || p.value < 6) return "";
                        const pctText = real + "%";
                        return p.value >= 18 ? basisLabel + "\n" + pctText : pctText;
                    }
                }
            },
            {
                // نفس الفكرة - "المتبقي من المستهدف" مكتوبة داخل لونها الرمادي الفاتح، كنسبة
                name: "المتبقي من المستهدف", type: "bar", stack: "target", data: remainingOfTargetPct, barMaxWidth: 46, z: 1,
                itemStyle: { color: RP_REMAINING_COLOR, borderRadius: [4, 4, 0, 0] },
                label: {
                    show: true, position: "inside", fontSize: 10, fontWeight: 700, color: C.axisText, lineHeight: 14,
                    formatter: function (p) {
                        if (p.value < 6) return "";
                        return p.value >= 18 ? "المتبقي\n" + p.value + "%" : p.value + "%";
                    }
                }
            },
            {
                // خط "المستهدف" - شريط علوي منفصل، ارتفاع كل نقطة يعكس حجم مستهدف ذلك البند
                // نسبةً لبقية البنود (بند بمستهدف أكبر = نقطة أعلى)، مع قيمة المستهدف الحقيقية
                // مكتوبة فوق كل نقطة دائمًا
                name: "المستهدف", type: "line", data: targetLineY, z: 5, connectNulls: true,
                symbol: "circle", symbolSize: 6,
                lineStyle: { color: C.mid, type: "dashed", width: 2 },
                itemStyle: { color: C.mid, borderWidth: 2, borderColor: "#fff" },
                label: {
                    show: true, position: "top", fontSize: 10, fontWeight: 800, color: C.mid,
                    formatter: function (p) { return targets[p.dataIndex] > 0 ? rpFormatNum(targets[p.dataIndex]) : ""; }
                }
            }
        ]
    };
}

/* ==========================================================================
   0) بطاقات "المحصل الحالي/السابق ونسبة النمو" - سنة بسنة (أحدث سنة بالبيانات مقابل
   السنة التي قبلها مباشرة)
   ========================================================================== */
function rpGrowthCardHtml(icon, label, value, isPercent, isPositive) {
    const valueText = isPercent
        ? (value > 0 ? "+" : "") + value.toFixed(1) + "%"
        : rpFormatNum(value);
    const colorClass = isPercent ? (isPositive ? "rp-growth-up" : "rp-growth-down") : "";
    return '<div class="rp-growth-card ' + colorClass + '">' +
        '<div class="rp-growth-icon"><i class="bi ' + icon + '"></i></div>' +
        '<div>' +
            '<div class="rp-growth-label">' + label + '</div>' +
            '<div class="rp-growth-value">' + valueText + (!isPercent ? ' <span class="rp-growth-sar">' + SAR_SYMBOL + '</span>' : '') + '</div>' +
        '</div>' +
    '</div>';
}

function renderRevenueGrowthCards(data) {
    const box = document.getElementById("rpGrowthCards");
    if (!box) return;
    if (!data.revenueCurrentYear || !data.revenuePreviousYear) { box.innerHTML = ""; return; }

    const isPositive = data.revenueGrowthPercent >= 0;
    box.innerHTML =
        rpGrowthCardHtml("bi-calendar-check", "إجمالي المحصل " + data.revenueCurrentYear, data.revenueCurrentYearCollected, false, true) +
        rpGrowthCardHtml("bi-calendar3", "إجمالي المحصل " + data.revenuePreviousYear, data.revenuePreviousYearCollected, false, true) +
        rpGrowthCardHtml(isPositive ? "bi-graph-up-arrow" : "bi-graph-down-arrow", "نسبة نمو الإيرادات", data.revenueGrowthPercent, true, isPositive);
}

function renderRevenuePerfChart() {
    const box = document.getElementById("revenuePerfBody");
    if (!box || !revenuePerfData) return;

    if (!revenuePerfChart) {
        revenuePerfChart = echarts.init(box);
        chartRegistry.push(revenuePerfChart);
    }
    try { revenuePerfChart.setOption(buildRevenuePerfChartOption(revenuePerfData), true); } catch (e) {}

    const financeBtn = document.getElementById("rpSourceFinance");
    const baladiyahBtn = document.getElementById("rpSourceBaladiyah");
    if (financeBtn) financeBtn.classList.toggle("active", revenuePerfSource === "finance");
    if (baladiyahBtn) baladiyahBtn.classList.toggle("active", revenuePerfSource === "baladiyah");

    const subtitleBasis = document.getElementById("rpSubtitleBasis");
    if (subtitleBasis) subtitleBasis.textContent = RP_SOURCE_LABELS[revenuePerfSource];
}

/* ==========================================================================
   شكل 2: تفصيل المحصّل حسب أساس الاستحقاق - دونات (القيمة ككل مقسومة لـ3 حالات: أساس
   استحقاق / محصل من مديونيات / محصل أوامر تنفيذ)، من بيانات DashboardRecords الحية
   (لا علاقة له بملف "محصل سنة الأساس 2025")
   ========================================================================== */
const RP_BASIS_COLORS = [C.dark, C.lime, C.mid];
const RP_BASIS_ICONS = { "أساس استحقاق": "bi-calendar-check", "محصل من مديونيات": "bi-arrow-repeat", "محصل أوامر تنفيذ": "bi-file-earmark-text" };
let rpBasisChart = null;

function removeRevenueBasisSection() {
    const el = document.getElementById("revenueBasisSection");
    if (el) el.remove();
}

function buildRevenueBasisDonutOption(rows, total) {
    const items = rows.map(function (r, i) {
        return { name: r.caseLabel, value: r.collectedValue, itemStyle: { color: RP_BASIS_COLORS[i % RP_BASIS_COLORS.length] } };
    });

    return {
        tooltip: tooltipStyle({
            trigger: "item",
            formatter: function (p) {
                return '<div style="font-weight:700;">' + escapeHtml(p.name) + '</div><div>المحصّل: <strong>' + rpFormatNum(p.value) + '</strong></div>';
            }
        }),
        legend: { bottom: 0, textStyle: Object.assign({}, AXIS_LABEL_STYLE), itemWidth: 12, itemHeight: 12 },
        series: [{
            type: "pie", radius: ["46%", "72%"], center: ["50%", "44%"],
            itemStyle: { borderColor: "#fff", borderWidth: 2 },
            label: {
                show: true, fontSize: 11, fontWeight: 700, color: C.axisText,
                formatter: function (p) { return p.name + "\n" + rpFormatNum(p.value); }
            },
            labelLine: { show: true, length: 8, length2: 8 },
            data: items
        }]
    };
}

function renderRevenueBasisSection(data) {
    removeRevenueBasisSection();
    rpBasisChart = null;

    const rows = data.revenueBasisBreakdown;
    const total = data.revenueBasisBreakdownTotal;
    const perfSection = document.getElementById("revenuePerfSection");
    if (!perfSection || !rows || !rows.length || !total) return;

    const section = document.createElement("div");
    section.id = "revenueBasisSection";
    section.className = "rp-card";
    section.innerHTML =
        '<div class="rp-header">' +
            '<div>' +
                '<div class="rp-title">مصدر المحصّل</div>' +
                '<div class="rp-subtitle">إجمالي المحصّل: <strong>' + rpFormatNum(total) + '</strong> ' + SAR_SYMBOL + '</div>' +
            '</div>' +
        '</div>' +
        '<div class="rp-growth-cards" id="rpBasisCards"></div>' +
        '<div class="echart-box" id="revenueBasisBody" style="height:300px;margin-top:10px;"></div>';

    perfSection.insertAdjacentElement("afterend", section);

    const cardsBox = document.getElementById("rpBasisCards");
    if (cardsBox) {
        cardsBox.innerHTML = rows.map(function (r) {
            return rpGrowthCardHtml(RP_BASIS_ICONS[r.caseLabel] || "bi-cash-stack", r.caseLabel, r.collectedValue, false, true);
        }).join("");
    }

    const box = document.getElementById("revenueBasisBody");
    if (box) {
        rpBasisChart = echarts.init(box);
        chartRegistry.push(rpBasisChart);
        rpBasisChart.setOption(buildRevenueBasisDonutOption(rows, total));
    }
}

function renderRevenuePerformanceSection(data) {
    removeRevenuePerfSection();
    removeRevenueBasisSection();
    revenuePerfChart = null;

    const rows = data.revenueTypePerformance;
    const kpiGrid = document.getElementById("kpiGrid");
    if (!kpiGrid || !rows || !rows.length) return;

    revenuePerfData = rows;

    const section = document.createElement("div");
    section.id = "revenuePerfSection";
    section.className = "rp-card";
    section.innerHTML =
        '<div class="rp-header">' +
            '<div>' +
                '<div class="rp-title">أداء الإيرادات حسب نوع الإيراد</div>' +
                '<div class="rp-subtitle">المستهدف السنوي مقارنة بـ<span id="rpSubtitleBasis">' + RP_SOURCE_LABELS[revenuePerfSource] + '</span> لكل نوع إيراد</div>' +
            '</div>' +
            '<div class="rp-source-toggle">' +
                '<span class="rp-source-label"><i class="bi bi-flag"></i> مصدر المستهدف</span>' +
                '<div class="rp-toggle-group">' +
                    '<button type="button" id="rpSourceFinance" class="rp-toggle-btn">' + RP_SOURCE_LABELS.finance + '</button>' +
                    '<button type="button" id="rpSourceBaladiyah" class="rp-toggle-btn">' + RP_SOURCE_LABELS.baladiyah + '</button>' +
                '</div>' +
            '</div>' +
            '<div class="tc-upload-wrap read-only-hide">' +
                '<input type="file" id="rpBaselineUploadInput" accept=".xlsx" hidden />' +
                '<button type="button" id="rpBaselineUploadBtn" class="tc-upload-btn" title="رفع بيانات محصل سنة الأساس">' +
                    '<i class="bi bi-upload"></i>' +
                '</button>' +
                '<span id="rpBaselineUploadStatus" class="tc-upload-status"></span>' +
            '</div>' +
        '</div>' +
        '<div class="rp-growth-cards" id="rpGrowthCards"></div>' +
        '<div class="echart-box" id="revenuePerfBody" style="height:340px;margin-top:10px;"></div>';

    kpiGrid.insertAdjacentElement("afterend", section);

    renderRevenueGrowthCards(data);
    renderRevenuePerfChart();

    const financeBtn = document.getElementById("rpSourceFinance");
    const baladiyahBtn = document.getElementById("rpSourceBaladiyah");
    if (financeBtn) financeBtn.addEventListener("click", function () { revenuePerfSource = "finance"; renderRevenuePerfChart(); });
    if (baladiyahBtn) baladiyahBtn.addEventListener("click", function () { revenuePerfSource = "baladiyah"; renderRevenuePerfChart(); });

    rpInitBaselineUpload();
    renderRevenueBasisSection(data);
}

/* ==========================================================================
   رفع ملف "محصل سنة الأساس 2025" (يغذّي بطاقة "محصل 2025" وحدها أعلى هذا القسم فقط)
   ========================================================================== */
function rpSetBaselineUploadStatus(text, isError) {
    const el = document.getElementById("rpBaselineUploadStatus");
    if (!el) return;
    el.textContent = text || "";
    el.className = "tc-upload-status" + (isError ? " tc-upload-status-error" : text ? " tc-upload-status-ok" : "");
}

function rpInitBaselineUpload() {
    const btn = document.getElementById("rpBaselineUploadBtn");
    const input = document.getElementById("rpBaselineUploadInput");
    if (!btn || !input) return;

    btn.addEventListener("click", function () { input.click(); });

    input.addEventListener("change", async function () {
        const file = input.files && input.files[0];
        if (!file) return;

        btn.disabled = true;
        rpSetBaselineUploadStatus("جارِ رفع الملف...", false);

        try {
            const formData = new FormData();
            formData.append("revenueBaselineFile", file);

            const resp = await fetch("/Dashboard/UploadRevenueBaseline", { method: "POST", body: formData });
            const result = await resp.json();

            if (result.success) {
                rpSetBaselineUploadStatus(result.message, false);
                if (typeof applyDashboardFilters === "function") applyDashboardFilters();
            } else {
                rpSetBaselineUploadStatus(result.message || "تعذّر رفع الملف.", true);
            }
        } catch (err) {
            console.error("تعذّر رفع ملف محصل سنة الأساس:", err);
            rpSetBaselineUploadStatus("تعذّر رفع الملف، تحقق من الاتصال وحاول مجددًا.", true);
        } finally {
            btn.disabled = false;
            input.value = "";
        }
    });
}
