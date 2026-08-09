/* ==========================================================================
   صفحة "المستهدفات" — نفس هوية الألوان والأسلوب البصري المستخدم بباقي لوحات
   النظام (section-dashboard.js). 4 بطاقات KPI بسيطة + 5 رسوم بيانية:
     1) أداء شهري (أعمدة = فعلي غير تراكمي) + خط مستهدف شهري غير تراكمي
     2) أداء ربعي: كل ربع مقابل مستهدفه الخاص به (غير تراكمي، ليس المستهدف السنوي)
     3) أداء البنود الرئيسية: فعلي/مستهدف/نسبة الإنجاز (شريط أفقي متجمع)
     4) توزيع الإيرادات حسب البند الرئيسي (Donut)
     5) أعلى البنود من حيث قيمة الإيراد (تنازليًا)
   ========================================================================== */
const TC_C = {
    dark:   "#0E6E61",
    mid:    "#3F8F87",
    light:  "#5FA89D",
    lime:   "#8DC63F",
    limeDk: "#5F9E1F",
    red:    "#D6534A",
    target: "#E0A930",
    text:   "#173B36",
    muted:  "#6E8A86",
    grid:   "#E1ECEA",
    axisText: "#181818"
};

// نمط موحّد لتسميات المحاور بجميع رسوم الموقع: أسود واضح، حجم 13-14px، وزن 600
const TC_AXIS_LABEL_STYLE = { color: TC_C.axisText, fontSize: 13, fontWeight: 600 };

function tcAxisStyle(extra) {
    extra = extra || {};
    const merged = Object.assign({
        axisLine:  { lineStyle: { color: TC_C.grid } },
        axisLabel: Object.assign({}, TC_AXIS_LABEL_STYLE),
        splitLine: { lineStyle: { color: TC_C.grid, type: "dashed" } }
    }, extra);
    // دمج عميق لـ axisLabel حتى لا يفقد اللون/الحجم الافتراضي عند تمرير خصائص جزئية فقط
    merged.axisLabel = Object.assign({}, TC_AXIS_LABEL_STYLE, extra.axisLabel || {});
    return merged;
}

function tcTooltipStyle(extra) {
    return Object.assign({
        backgroundColor: "#FFFFFF",
        borderColor: TC_C.grid,
        borderWidth: 1,
        textStyle: { color: TC_C.text, fontSize: 12 },
        extraCssText: "box-shadow:0 4px 16px rgba(0,0,0,0.1);border-radius:10px;"
    }, extra || {});
}

function tcFormatNum(n) {
    if (!Number.isFinite(n)) return "0";
    return Math.round(n).toLocaleString("en-US");
}

function tcFormatCompact(n) {
    if (Math.abs(n) >= 1e9) return (n / 1e9).toFixed(1) + "B";
    if (Math.abs(n) >= 1e6) return (n / 1e6).toFixed(1) + "M";
    if (Math.abs(n) >= 1e3) return (n / 1e3).toFixed(1) + "K";
    return n.toFixed(0);
}

function tcClamp(n, min, max) { return Math.max(min, Math.min(max, n)); }

const TC_SAR_SYMBOL = '<svg class="kpi-sar" viewBox="0 0 1124.14 1256.39" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-label="ريال سعودي">' +
    '<path d="M699.62,1113.02h0c-20.06,44.48-33.32,92.75-38.4,143.37l424.51-90.24c20.06-44.47,33.31-92.75,38.4-143.37l-424.51,90.24Z"/>' +
    '<path d="M1085.73,895.8c20.06-44.47,33.32-92.75,38.4-143.37l-330.68,70.33v-135.2l292.27-62.11c20.06-44.47,33.32-92.75,38.4-143.37l-330.68,70.27V66.13c-50.67,28.45-95.67,66.32-132.25,110.99v403.35l-132.25,28.11V0c-50.67,28.44-95.67,66.32-132.25,110.99v525.69l-295.91,62.88c-20.06,44.47-33.33,92.75-38.42,143.37l334.33-71.05v170.26l-358.3,76.14c-20.06,44.47-33.32,92.75-38.4,143.37l375.04-79.7c30.53-6.35,56.77-24.4,73.83-49.24l68.78-101.97v-.02c7.14-10.55,11.3-23.27,11.3-36.97v-149.98l132.25-28.11v270.4l424.53-90.28Z"/></svg>';

let tcCharts = [];

function tcDisposeCharts() {
    tcCharts.forEach(function (c) { try { c.dispose(); } catch (e) {} });
    tcCharts = [];
}

function tcEscapeHtml(str) {
    const div = document.createElement("div");
    div.textContent = str == null ? "" : String(str);
    return div.innerHTML;
}

function tcSarVal(n) {
    return tcFormatNum(n) + ' <span class="value-sar">' + TC_SAR_SYMBOL + '</span>';
}

/* ==========================================================================
   بطاقات KPI الأربع: إجمالي الإيرادات / إجمالي المستهدف / نسبة تحقيق المستهدف /
   الفرق بين الإيرادات والمستهدف
   ========================================================================== */
function tcRenderKpiCards(data, mode) {
    const grid = document.getElementById("tcKpiGrid");
    if (!grid) return;

    var isFinance = mode === "finance";
    var revenueLabel = isFinance ? "إجمالي الإيرادات (المحصّل)" : "إجمالي الإيرادات (المفوتر)";
    var revenueTotal = isFinance ? data.financeCollectedTotal : data.baladiyahInvoicedTotal;
    var targetTotal = isFinance ? data.financeTargetTotal : data.baladiyahTargetTotal;
    var achievementPct = isFinance ? data.financeAchievementPercent : data.baladiyahAchievementPercent;
    var diff = revenueTotal - targetTotal;

    var cards = [
        { cls: "c1", icon: "bi-cash-stack", label: revenueLabel, value: tcSarVal(revenueTotal) },
        { cls: "c4", icon: "bi-bullseye", label: "إجمالي المستهدف", value: tcSarVal(targetTotal) },
        { cls: "c2", icon: "bi-graph-up-arrow", label: "نسبة تحقيق المستهدف", value: achievementPct.toFixed(1) + "%" },
        {
            cls: diff >= 0 ? "c2" : "c3", icon: diff >= 0 ? "bi-arrow-up-circle" : "bi-arrow-down-circle",
            label: "الفرق بين الإيرادات والمستهدف", value: tcSarVal(diff)
        }
    ];

    grid.innerHTML = cards.map(function (c) {
        return '<div class="kpi-card ' + c.cls + '">' +
            '<div class="icon"><i class="bi ' + c.icon + '"></i></div>' +
            '<div class="label">' + c.label + '</div>' +
            '<div class="value">' + c.value + '</div>' +
        '</div>';
    }).join("");
}

/* ==========================================================================
   أدوات مشتركة لتلميحات (tooltip) رسوم الأعمدة + الخط
   ========================================================================== */
function tcComboTooltipFormatter(params) {
    var lines = '<div style="font-weight:700;margin-bottom:6px;">' + params[0].axisValue + '</div>';
    params.forEach(function (p) {
        var marker = p.seriesType === "line"
            ? '<span style="display:inline-block;width:14px;height:2px;background:' + p.color + ';margin-left:6px;vertical-align:middle;"></span>'
            : '<span style="display:inline-block;width:10px;height:10px;border-radius:3px;background:' + p.color + ';margin-left:6px;vertical-align:middle;"></span>';
        lines += '<div style="display:flex;align-items:center;">' + marker +
            '<span>' + tcEscapeHtml(p.seriesName) + ': <strong>' + tcFormatNum(p.value) + '</strong></span></div>';
    });
    return lines;
}

/* ==========================================================================
   الرسم 1: أداء شهري - أعمدة (فعلي شهري غير تراكمي) + خط (مستهدف شهري غير
   تراكمي)، محور قيم واحد لأن الفعلي والمستهدف بنفس رتبة الحجم تقريبًا
   ========================================================================== */
function tcBuildMonthlyLineColumnOption(labels, actualMonthly, targetMonthly, actualName, targetName, actualColor) {
    return {
        tooltip: tcTooltipStyle({ trigger: "axis", axisPointer: { type: "shadow" }, formatter: tcComboTooltipFormatter }),
        legend: { top: 0, right: 8, textStyle: Object.assign({}, TC_AXIS_LABEL_STYLE), itemWidth: 12, itemHeight: 10 },
        grid: { left: 60, right: 24, top: 56, bottom: 36 },
        xAxis: tcAxisStyle({ type: "category", data: labels }),
        yAxis: tcAxisStyle({ type: "value", axisLabel: { formatter: tcFormatCompact } }),
        series: [
            {
                name: actualName, type: "bar", data: actualMonthly, barMaxWidth: 26, z: 1,
                itemStyle: { color: actualColor, borderRadius: [6, 6, 0, 0] },
                label: {
                    show: true, position: "top", color: actualColor, fontWeight: 700, fontSize: 10,
                    formatter: function (p) { return tcFormatCompact(p.value); }
                }
            },
            {
                name: targetName, type: "line", data: targetMonthly, smooth: true, z: 2,
                lineStyle: { color: TC_C.target, width: 2.5, type: "dashed" },
                itemStyle: { color: TC_C.target },
                symbol: "circle", symbolSize: 6,
                label: {
                    show: true, position: "top", color: TC_C.target, fontWeight: 700, fontSize: 10,
                    formatter: function (p) { return tcFormatCompact(p.value); }
                }
            }
        ]
    };
}

/* ==========================================================================
   الرسم 2: أداء ربعي - كل ربع مقابل مستهدفه الخاص به فقط (غير تراكمي)، مبني من
   تجميع القيم الشهرية غير التراكمية لكل 3 أشهر (وليس مقارنة بالمستهدف السنوي)
   ========================================================================== */
function tcQuarterlyTotalsFromMonthly(monthlyArr) {
    var q = [0, 0, 0, 0];
    for (var m = 0; m < 12; m++) q[Math.floor(m / 3)] += (monthlyArr && monthlyArr[m]) || 0;
    return q;
}

// (صفحة مستهدفات البلديات والإسكان فقط) تراكمي: الربع1=أشهر1-3، الربع2=أشهر1-6،
// الربع3=أشهر1-9، الربع4=أشهر1-12 (=الإجمالي السنوي كاملاً)
function tcQuarterlyCumulativeFromMonthly(monthlyArr) {
    var q = [0, 0, 0, 0];
    var running = 0;
    for (var m = 0; m < 12; m++) {
        running += (monthlyArr && monthlyArr[m]) || 0;
        if (m === 2) q[0] = running;
        else if (m === 5) q[1] = running;
        else if (m === 8) q[2] = running;
        else if (m === 11) q[3] = running;
    }
    return q;
}

function tcBuildQuarterlyTotalsOption(quarterLabels, actualByQuarter, targetByQuarter, actualName, targetName, actualColor) {
    return {
        tooltip: tcTooltipStyle({ trigger: "axis", axisPointer: { type: "shadow" }, formatter: tcComboTooltipFormatter }),
        legend: { top: 0, right: 8, textStyle: Object.assign({}, TC_AXIS_LABEL_STYLE), itemWidth: 12, itemHeight: 10 },
        grid: { left: 60, right: 24, top: 56, bottom: 36 },
        xAxis: tcAxisStyle({ type: "category", data: quarterLabels }),
        yAxis: tcAxisStyle({ type: "value", axisLabel: { formatter: tcFormatCompact } }),
        series: [
            {
                name: targetName, type: "bar", data: targetByQuarter, barMaxWidth: 46, barGap: "16%", z: 1,
                itemStyle: { color: TC_C.target, borderRadius: [6, 6, 0, 0] },
                label: { show: true, position: "top", color: TC_C.target, fontWeight: 700, fontSize: 12, formatter: function (p) { return tcFormatCompact(p.value); } }
            },
            {
                name: actualName, type: "bar", data: actualByQuarter, barMaxWidth: 46, z: 1,
                itemStyle: { color: actualColor, borderRadius: [6, 6, 0, 0] },
                label: { show: true, position: "top", color: actualColor, fontWeight: 700, fontSize: 12, formatter: function (p) { return tcFormatCompact(p.value); } }
            }
        ]
    };
}

/* ==========================================================================
   قائمة "البنود" الموحّدة (فعلي/مستهدف) - مصدر مشترك لرسوم 3، 4، 5. الفعلي =
   مجموع أرباع السنة الأربعة (غير تراكمية أصلاً بكل ربع)، والمستهدف = آخر عنصر
   بمصفوفة المستهدف الربعية التراكمية (= المستهدف السنوي كاملاً من ملف المستهدف
   الشهري المرفوع لهذا البند إن توفّر).
   ========================================================================== */
function tcBuildItemsList(itemsQuarterly, valueField, targetField) {
    return (itemsQuarterly || []).map(function (it) {
        var actual = (it[valueField] || []).reduce(function (a, b) { return a + (b || 0); }, 0);
        var targetArr = it[targetField];
        var target = Array.isArray(targetArr) ? (targetArr[3] || 0) : (targetArr || 0);
        return { itemLabel: it.itemLabel, actual: actual, target: target };
    });
}

/* ==========================================================================
   الرسم 3: أداء البنود الرئيسية - فعلي/مستهدف/نسبة الإنجاز (شريط أفقي متجمع)
   ========================================================================== */
function tcBuildItemsHorizontalBarOption(items, actualName, targetName, actualColor) {
    const categories = items.map(function (it) { return it.itemLabel; });
    const actuals = items.map(function (it) { return it.actual; });
    const targets = items.map(function (it) { return it.target; });
    const pcts = items.map(function (it) { return it.target > 0 ? Math.round(it.actual / it.target * 1000) / 10 : null; });

    return {
        tooltip: tcTooltipStyle({
            trigger: "axis", axisPointer: { type: "shadow" },
            formatter: function (params) {
                var i = params[0].dataIndex;
                var lines = '<div style="font-weight:700;margin-bottom:6px;">' + tcEscapeHtml(params[0].axisValue) + '</div>';
                params.forEach(function (p) {
                    lines += '<div>' + tcEscapeHtml(p.seriesName) + ': <strong>' + tcFormatNum(p.value) + '</strong></div>';
                });
                lines += '<div style="margin-top:4px;">نسبة الإنجاز: <strong>' +
                    (pcts[i] === null ? "لا يوجد مستهدف لهذا البند" : pcts[i].toFixed(1) + "%") + '</strong></div>';
                return lines;
            }
        }),
        legend: { top: 0, right: 8, textStyle: Object.assign({}, TC_AXIS_LABEL_STYLE), itemWidth: 12, itemHeight: 10 },
        grid: { left: 150, right: 70, top: 40, bottom: 20 },
        xAxis: tcAxisStyle({ type: "value", axisLabel: { formatter: tcFormatCompact } }),
        yAxis: tcAxisStyle({ type: "category", data: categories, axisLabel: { fontSize: 11, width: 140, overflow: "break" } }),
        series: [
            {
                name: actualName, type: "bar", data: actuals, barMaxWidth: 16, z: 1,
                itemStyle: { color: actualColor, borderRadius: [0, 4, 4, 0] },
                label: {
                    show: true, position: "right", color: actualColor, fontWeight: 700, fontSize: 10,
                    formatter: function (p) {
                        var pct = pcts[p.dataIndex];
                        return tcFormatCompact(p.value) + (pct !== null ? " (" + pct.toFixed(0) + "%)" : "");
                    }
                }
            },
            {
                name: targetName, type: "bar", data: targets, barMaxWidth: 16, z: 1,
                itemStyle: { color: TC_C.target, opacity: 0.75, borderRadius: [0, 4, 4, 0] },
                label: {
                    show: true, position: "right", color: TC_C.target, fontWeight: 700, fontSize: 10,
                    formatter: function (p) { return p.value > 0 ? tcFormatCompact(p.value) : ""; }
                }
            }
        ]
    };
}

/* ==========================================================================
   الرسم 4: توزيع الإيرادات حسب البند الرئيسي (Donut)
   ========================================================================== */
function tcBuildItemsDonutOption(items) {
    var palette = [TC_C.dark, TC_C.lime, TC_C.mid, TC_C.target, TC_C.red, TC_C.light, TC_C.limeDk, "#A9CFC9"];
    var pieData = items.map(function (it, i) {
        return { name: it.itemLabel, value: it.actual, itemStyle: { color: palette[i % palette.length] } };
    });

    return {
        tooltip: tcTooltipStyle({
            trigger: "item",
            formatter: function (p) {
                return '<div style="font-weight:700;margin-bottom:4px;">' + tcEscapeHtml(p.name) + '</div>' +
                    '<div>' + tcFormatNum(p.value) + ' (' + p.percent + '%)</div>';
            }
        }),
        legend: { bottom: 0, textStyle: Object.assign({}, TC_AXIS_LABEL_STYLE, { fontSize: 11 }), itemWidth: 10, itemHeight: 10 },
        series: [
            {
                name: "الإيرادات حسب البند", type: "pie", radius: ["45%", "72%"], center: ["50%", "45%"],
                avoidLabelOverlap: true,
                itemStyle: { borderColor: "#fff", borderWidth: 2 },
                label: { formatter: "{b}\n{d}%", fontSize: 10, fontWeight: 700, color: TC_C.text },
                labelLine: { length: 6, length2: 6 },
                data: pieData
            }
        ]
    };
}

/* ==========================================================================
   الرسم 5: أعلى البنود من حيث قيمة الإيراد (تنازليًا)
   ========================================================================== */
function tcBuildTopItemsBarOption(items, actualName, actualColor) {
    const categories = items.map(function (it) { return it.itemLabel; });
    const actuals = items.map(function (it) { return it.actual; });
    var isNarrow = window.innerWidth < 700;

    return {
        tooltip: tcTooltipStyle({ trigger: "axis", axisPointer: { type: "shadow" }, formatter: tcComboTooltipFormatter }),
        grid: { left: 60, right: 24, top: 30, bottom: isNarrow ? 90 : 70 },
        xAxis: tcAxisStyle({
            type: "category", data: categories,
            axisLabel: { interval: 0, rotate: isNarrow ? 40 : 20, fontSize: 11 }
        }),
        yAxis: tcAxisStyle({ type: "value", axisLabel: { formatter: tcFormatCompact } }),
        series: [
            {
                name: actualName, type: "bar", data: actuals, barMaxWidth: 40, z: 1,
                itemStyle: { color: actualColor, borderRadius: [6, 6, 0, 0] },
                label: {
                    show: true, position: "top", color: actualColor, fontWeight: 700, fontSize: 11,
                    formatter: function (p) { return tcFormatCompact(p.value); }
                }
            }
        ]
    };
}

/* ==========================================================================
   عندما يحتوي الصف على بطاقة رسم واحدة فقط - يأخذ الصف عرض الصفحة كاملاً بدل
   أن ينحشر بنص العرض ويترك فراغاً
   ========================================================================== */
function tcFixSingleChartRows(rowIds) {
    rowIds.forEach(function (rowId) {
        const row = document.getElementById(rowId);
        if (!row) return;
        const count = row.querySelectorAll(".chart-card").length;
        row.classList.toggle("tc-row-single", count === 1);
    });
}

/* ==========================================================================
   رسم رسم بياني داخل بطاقة ضمن صف مُحدد
   ========================================================================== */
function tcRenderChart(rowId, containerId, title, badge, option, extraClass) {
    const row = document.getElementById(rowId);
    if (!row) return;

    const id = containerId + "_" + Date.now() + "_" + Math.floor(Math.random() * 1000);
    const div = document.createElement("div");
    div.className = "chart-card" + (extraClass ? " " + extraClass : "");
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">' + tcEscapeHtml(title) + '</div>' +
            '<div class="chart-badge">' + tcEscapeHtml(badge) + '</div>' +
        '</div>' +
        '<div class="echart-box" id="' + id + '"></div>';
    row.appendChild(div);

    const chart = echarts.init(document.getElementById(id));
    chart.setOption(option);
    tcCharts.push(chart);
}

/* ==========================================================================
   تعبئة قائمة تصفية الجهات
   ========================================================================== */
function tcPopulateEntityFilter(entities, selected) {
    const select = document.getElementById("tcEntityFilter");
    if (!select) return;
    const current = selected || "";

    var html = '<option value="">كل الجهات (الأمانة + كل البلديات)</option>';
    (entities || []).forEach(function (e) {
        html += '<option value="' + tcEscapeHtml(e) + '"' + (e === current ? " selected" : "") + '>' + tcEscapeHtml(e) + '</option>';
    });
    select.innerHTML = html;
}

/* ==========================================================================
   التحميل الرئيسي
   ========================================================================== */
async function tcLoad(entity) {
    const contentEl = document.getElementById("tcContent");
    const noDataEl = document.getElementById("tcNoDataBox");

    try {
        var url = window.TC_DATA_URL;
        if (entity) url += "?entity=" + encodeURIComponent(entity);

        const resp = await fetch(url);
        if (!resp.ok) throw new Error("network");
        const data = await resp.json();

        tcDisposeCharts();
        const row1 = document.getElementById("tcChartsRow");
        const row2 = document.getElementById("tcItemsAnnualRow");
        const row3 = document.getElementById("tcChartsRow2");
        if (row1) row1.innerHTML = "";
        if (row2) row2.innerHTML = "";
        if (row3) row3.innerHTML = "";

        if (!data.hasData) {
            if (contentEl) contentEl.style.display = "none";
            if (noDataEl) noDataEl.style.display = "";
            return;
        }

        if (contentEl) contentEl.style.display = "";
        if (noDataEl) noDataEl.style.display = "none";

        const mode = window.TC_MODE || "baladiyah";
        const isFinance = mode === "finance";
        const actualColor = isFinance ? TC_C.dark : TC_C.lime;
        const actualMonthly = isFinance ? data.financeCollectedMonthly : data.baladiyahInvoicedMonthly;
        const targetMonthly = isFinance ? data.financeTargetLine : data.baladiyahTargetLine;
        const actualName = isFinance ? "المحصّل" : "المفوتر";
        const targetName = "المستهدف";
        const valueField = isFinance ? "financeCollectedByQuarter" : "baladiyahInvoicedByQuarter";
        const targetField = isFinance ? "targetFinanceByQuarter" : "targetBaladiyahByQuarter";

        // 1) بطاقات KPI الأربع
        tcRenderKpiCards(data, mode);

        // ملاحظة: window.TC_MODE بلا افتراض هنا عمدًا (بخلاف "mode" أعلاه) - صفحة "مقارنة
        // المستهدفات" العامة لا تُعرِّف TC_MODE إطلاقًا وتُعامَل كـ"baladiyah" ضمنيًا لعرض
        // البيانات، لكنها يجب ألا تحصل على إعادة الترتيب/الحذف الخاصة بصفحة البلديات المخصصة
        const isBaladiyahPage = window.TC_MODE === "baladiyah";

        // أداء شهري: أعمدة (فعلي) + خط (مستهدف شهري) - نفس البيانات لكل الأوضاع
        const monthlyChartOption = tcBuildMonthlyLineColumnOption(data.monthLabels, actualMonthly, targetMonthly, actualName, targetName, actualColor);

        // أداء ربعي: تراكمي (Q1=1-3، Q2=1-6، Q3=1-9، Q4=1-12) بصفحة البلديات فقط، وإلا كل ربع بمفرده كسابقًا
        const quarterlyActual = isBaladiyahPage ? tcQuarterlyCumulativeFromMonthly(actualMonthly) : tcQuarterlyTotalsFromMonthly(actualMonthly);
        const quarterlyTarget = isBaladiyahPage ? tcQuarterlyCumulativeFromMonthly(targetMonthly) : tcQuarterlyTotalsFromMonthly(targetMonthly);
        const quarterlyTitle = isBaladiyahPage
            ? "الأداء الربعي (تراكمي) - " + actualName + " مقابل المستهدف"
            : "الأداء الربعي - كل ربع مقابل مستهدفه الخاص";
        const quarterlyChartOption = tcBuildQuarterlyTotalsOption(data.quarterLabels, quarterlyActual, quarterlyTarget, actualName, targetName, actualColor);

        // قائمة البنود (فعلي/مستهدف): بصفحة البلديات من "البند الرئيسي" مباشرة (itemsMainAnnual)،
        // وإلا من "نوع الإيراد" كسابقًا (itemsQuarterly)
        var itemsByRevenueDesc;
        if (isBaladiyahPage) {
            const itemsListBaladiyah = (data.itemsMainAnnual || []).map(function (it) {
                return { itemLabel: it.entity, actual: it.invoicedTotal, target: it.annualTarget };
            });
            itemsByRevenueDesc = itemsListBaladiyah.slice().sort(function (a, b) { return b.actual - a.actual; });
        } else {
            const itemsList = tcBuildItemsList(data.itemsQuarterly, valueField, targetField);
            itemsByRevenueDesc = itemsList.slice().sort(function (a, b) { return b.actual - a.actual; });
        }
        const itemsChartOption = tcBuildItemsHorizontalBarOption(itemsByRevenueDesc, actualName, targetName, actualColor);

        if (isBaladiyahPage) {
            // صف 1: الأداء الربعي (تراكمي) + البنود (مستهدف سنوي مقابل مفوتر لكل بند رئيسي)
            tcRenderChart("tcChartsRow", "tcQuarterlyChart", quarterlyTitle, "ربعي", quarterlyChartOption);
            tcRenderChart(
                "tcChartsRow", "tcItemsHorizontalChart",
                "أداء البنود الرئيسية - " + actualName + " مقابل المستهدف",
                "حسب البند",
                itemsChartOption, "tc-quarterly-card"
            );

            // صف 2: الأداء الشهري وحده بعرض كامل (مكان "أداء البنود الرئيسية" سابقًا)
            tcRenderChart(
                "tcItemsAnnualRow", "tcMonthlyChart",
                "الأداء الشهري - " + actualName + " مقابل المستهدف الشهري",
                "شهري",
                monthlyChartOption
            );

            // صف 3 (توزيع الإيرادات + أعلى البنود) محذوف بالكامل بهذه الصفحة
            if (row3) row3.style.display = "none";
        } else {
            // نفس الترتيب القديم بلا تغيير لصفحتَي "المالية"/"العامة"
            tcRenderChart("tcChartsRow", "tcMonthlyChart", "الأداء الشهري - " + actualName + " مقابل المستهدف الشهري", "شهري", monthlyChartOption);
            tcRenderChart("tcChartsRow", "tcQuarterlyChart", quarterlyTitle, "ربعي", quarterlyChartOption);
            tcRenderChart(
                "tcItemsAnnualRow", "tcItemsHorizontalChart",
                "أداء البنود الرئيسية - " + actualName + " مقابل المستهدف",
                "حسب البند",
                itemsChartOption, "tc-quarterly-card"
            );
            tcRenderChart("tcChartsRow2", "tcItemsDonutChart", "توزيع الإيرادات حسب البند الرئيسي", "توزيع", tcBuildItemsDonutOption(itemsByRevenueDesc));
            tcRenderChart("tcChartsRow2", "tcTopItemsChart", "أعلى البنود من حيث قيمة الإيراد", "الأعلى قيمة", tcBuildTopItemsBarOption(itemsByRevenueDesc, actualName, actualColor));
            if (row3) row3.style.display = "";
        }

        tcPopulateEntityFilter(data.entities, entity);

        // توسيع أي صف يحتوي على رسم واحد فقط ليأخذ عرض الصفحة كاملاً
        tcFixSingleChartRows(["tcChartsRow", "tcItemsAnnualRow", "tcChartsRow2"]);
    } catch (err) {
        console.error("تعذر تحميل بيانات مقارنة المستهدفات:", err);
        if (contentEl) contentEl.style.display = "none";
        if (noDataEl) noDataEl.style.display = "";
    }
}

/* ==========================================================================
   رفع ملف "المستهدف الشهري للبلديات" - زر مخصص أعلى الصفحة
   ========================================================================== */
function tcSetUploadStatus(text, isError) {
    const el = document.getElementById("tcUploadStatus");
    if (!el) return;
    el.textContent = text || "";
    el.className = "tc-upload-status" + (isError ? " tc-upload-status-error" : text ? " tc-upload-status-ok" : "");
}

function tcInitMonthlyTargetUpload() {
    const btn = document.getElementById("tcMonthlyTargetBtn");
    const input = document.getElementById("tcMonthlyTargetInput");
    if (!btn || !input || !window.TC_UPLOAD_MONTHLY_TARGET_URL) return;

    btn.addEventListener("click", function () { input.click(); });

    input.addEventListener("change", async function () {
        const file = input.files && input.files[0];
        if (!file) return;

        btn.disabled = true;
        tcSetUploadStatus("جارِ رفع الملف...", false);

        try {
            const formData = new FormData();
            formData.append("monthlyTargetFile", file);

            const resp = await fetch(window.TC_UPLOAD_MONTHLY_TARGET_URL, { method: "POST", body: formData });
            const result = await resp.json();

            if (result.success) {
                tcSetUploadStatus(result.message, false);
                const filterSelect = document.getElementById("tcEntityFilter");
                tcLoad(filterSelect ? filterSelect.value : "");
            } else {
                tcSetUploadStatus(result.message || "تعذّر رفع الملف.", true);
            }
        } catch (err) {
            console.error("تعذّر رفع ملف المستهدف الشهري:", err);
            tcSetUploadStatus("تعذّر رفع الملف، تحقق من الاتصال وحاول مجددًا.", true);
        } finally {
            btn.disabled = false;
            input.value = "";
        }
    });
}

document.addEventListener("DOMContentLoaded", function () {
    if (!window.TC_DATA_URL) return;

    tcLoad("");
    tcInitMonthlyTargetUpload();

    const filterSelect = document.getElementById("tcEntityFilter");
    if (filterSelect) {
        filterSelect.addEventListener("change", function () {
            tcLoad(filterSelect.value);
        });
    }

    window.addEventListener("resize", function () {
        tcCharts.forEach(function (c) { try { c.resize(); } catch (e) {} });
    });
});
