/* ==========================================================================
   صفحة "متابعة الأداء" - حلقات Radial Progress + رسوم احترافية بدل الجدول
   ========================================================================== */
const CF_C = {
    dark: "#0E6E61", mid: "#3F8F87", light: "#5FA89D", lime: "#8DC63F", limeDk: "#5F9E1F",
    red: "#D6534A", warn: "#E0A930", text: "#173B36", muted: "#6E8A86", grid: "#E1ECEA",
    axisText: "#181818"
};
const CF_AXIS_LABEL_STYLE = { color: CF_C.axisText, fontSize: 13, fontWeight: 600 };

function cfAxisStyle(extra) {
    extra = extra || {};
    const merged = Object.assign({
        axisLine: { lineStyle: { color: CF_C.grid } },
        splitLine: { lineStyle: { color: CF_C.grid, type: "dashed" } }
    }, extra);
    merged.axisLabel = Object.assign({}, CF_AXIS_LABEL_STYLE, extra.axisLabel || {});
    return merged;
}
function cfTooltip(extra) {
    return Object.assign({
        backgroundColor: "#FFFFFF", borderColor: CF_C.grid, borderWidth: 1,
        textStyle: { color: CF_C.text, fontSize: 12 },
        extraCssText: "box-shadow:0 4px 16px rgba(0,0,0,0.1);border-radius:10px;"
    }, extra || {});
}
function cfFormatNum(n) { return Number.isFinite(n) ? Math.round(n).toLocaleString("en-US") : "0"; }
function cfClamp(n, min, max) { return Math.max(min, Math.min(max, n)); }
function cfEscape(str) {
    const div = document.createElement("div");
    div.textContent = (str === null || str === undefined) ? "" : String(str);
    return div.innerHTML;
}

let cfChartRegistry = [];

/* ==========================================================================
   حلقات Radial Progress (نفس أسلوب صفحة مقارنة المستهدفات)
   ========================================================================== */
function cfGaugeRingSvg(percent, colorFrom, colorTo, gradId, label) {
    const r = 85, c = 2 * Math.PI * r;
    const pct = cfClamp(percent, 0, 100);
    const offset = c - (pct / 100) * c;
    return '<div class="tc-gauge-ring-wrap">' +
        '<svg viewBox="0 0 220 220">' +
            '<defs><linearGradient id="' + gradId + '" x1="0%" y1="0%" x2="100%" y2="100%">' +
                '<stop offset="0%" stop-color="' + colorFrom + '"/>' +
                '<stop offset="100%" stop-color="' + colorTo + '"/>' +
            '</linearGradient></defs>' +
            '<circle class="tc-gauge-track" cx="110" cy="110" r="' + r + '"></circle>' +
            '<circle class="tc-gauge-progress" cx="110" cy="110" r="' + r + '" stroke="url(#' + gradId + ')" ' +
                'stroke-dasharray="' + c.toFixed(2) + '" stroke-dashoffset="' + c.toFixed(2) + '" ' +
                'data-target-offset="' + offset.toFixed(2) + '"></circle>' +
        '</svg>' +
        '<div class="tc-gauge-center">' +
            '<div class="tc-gauge-percent">' + percent.toFixed(1) + '%</div>' +
            '<div class="tc-gauge-percent-label">' + cfEscape(label || "") + '</div>' +
        '</div>' +
    '</div>';
}

function cfGaugeCardHtml(opts) {
    const remaining = cfClamp(100 - opts.percent, 0, 100);
    return '<div class="tc-gauge-card" style="--tc-accent:' + opts.accent + ';--tc-icon-bg:' + opts.iconBg + ';--tc-glow:' + opts.glow + ';">' +
        '<div class="tc-gauge-header">' +
            '<div class="tc-gauge-icon"><i class="bi ' + opts.icon + '"></i></div>' +
            '<div>' +
                '<div class="tc-gauge-title">' + cfEscape(opts.title) + '</div>' +
                '<div class="tc-gauge-subtitle">' + cfEscape(opts.subtitle) + '</div>' +
            '</div>' +
        '</div>' +
        '<div class="tc-gauge-rings-row">' +
            cfGaugeRingSvg(opts.percent, opts.gradFrom, opts.gradTo, opts.gradId, opts.ringLabel1 || "النسبة") +
            cfGaugeRingSvg(remaining, CF_C.muted, opts.remainingColor || CF_C.red, opts.gradId + "R", opts.ringLabel2 || "المتبقي") +
        '</div>' +
        '<div class="tc-gauge-stats">' +
            '<div class="tc-gauge-stat"><div class="lbl">' + opts.stat1Label + '</div><div class="val">' + opts.stat1Value + '</div></div>' +
            '<div class="tc-gauge-stat"><div class="lbl">' + opts.stat2Label + '</div><div class="val">' + opts.stat2Value + '</div></div>' +
        '</div>' +
    '</div>';
}

function cfRenderGauges(data) {
    const row = document.getElementById("cfGaugeRow");
    if (!row) return;

    row.innerHTML =
        cfGaugeCardHtml({
            accent: CF_C.dark, iconBg: "rgba(14,110,97,0.12)", glow: "rgba(14,110,97,0.18)",
            icon: "bi-check2-circle", title: "نسبة الإنجاز العامة", subtitle: "المطالبات المُنجزة من إجمالي المطالبات",
            percent: data.overallCompletionRatePercent || 0, gradFrom: CF_C.dark, gradTo: CF_C.mid, gradId: "cfGradDone",
            ringLabel1: "منجزة", ringLabel2: "متبقية", remainingColor: CF_C.warn,
            stat1Label: "متوسط مدة الإنجاز", stat1Value: (data.avgCompletionDays || 0).toFixed(1) + " يوم",
            stat2Label: "أسرع إدارة", stat2Value: cfEscape(data.fastestDepartment) || "—"
        }) +
        cfGaugeCardHtml({
            accent: CF_C.red, iconBg: "rgba(214,83,74,0.12)", glow: "rgba(214,83,74,0.18)",
            icon: "bi-exclamation-triangle", title: "المطالبات المتأخرة", subtitle: "تجاوزت 30 يومًا بلا إنجاز",
            percent: data.overdueSharePercent || 0, gradFrom: CF_C.red, gradTo: "#B9463D", gradId: "cfGradOverdue",
            ringLabel1: "متأخرة", ringLabel2: "غير متأخرة", remainingColor: CF_C.dark,
            stat1Label: "عدد المطالبات المتأخرة", stat1Value: cfFormatNum(data.overdueClaimsCount),
            stat2Label: "الأكثر تأخيرًا", stat2Value: cfEscape(data.slowestDepartment) || "—"
        });

    requestAnimationFrame(function () {
        requestAnimationFrame(function () {
            row.querySelectorAll(".tc-gauge-progress").forEach(function (el) {
                el.style.strokeDashoffset = el.getAttribute("data-target-offset");
            });
        });
    });
}

/* ==========================================================================
   بطاقة رسم عامة
   ========================================================================== */
function cfMakeChartCard(containerId, title, badge) {
    const row = document.getElementById(containerId);
    if (!row) return null;
    const id = "cfChart_" + Math.random().toString(36).slice(2);
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">' + title + '</div>' +
            (badge ? '<div class="chart-badge">' + badge + '</div>' : '') +
        '</div>' +
        '<div class="echart-box" id="' + id + '"></div>';
    row.appendChild(div);
    const chart = echarts.init(document.getElementById(id));
    cfChartRegistry.push(chart);
    return chart;
}

function cfRenderComparisonChart(cmp) {
    if (!cmp) return;
    const chart = cfMakeChartCard("cfChartsRow1", "مقارنة أداء الشهر الحالي بالشهر السابق", "مطالبات");
    if (!chart) return;
    chart.setOption({
        tooltip: cfTooltip({ trigger: "axis", axisPointer: { type: "shadow" } }),
        legend: { bottom: 0, textStyle: Object.assign({}, CF_AXIS_LABEL_STYLE), itemWidth: 10, itemHeight: 10 },
        grid: { left: 50, right: 16, top: 24, bottom: 46 },
        xAxis: cfAxisStyle({ type: "category", data: ["عدد المطالبات", "عدد المنجزة"] }),
        yAxis: cfAxisStyle({ type: "value" }),
        series: [
            {
                name: cmp.previousMonthLabel, type: "bar", barMaxWidth: 42,
                data: [cmp.previousClaimsCount, cmp.previousCompletedCount],
                itemStyle: { color: CF_C.light, borderRadius: [6, 6, 0, 0] }
            },
            {
                name: cmp.currentMonthLabel, type: "bar", barMaxWidth: 42,
                data: [cmp.currentClaimsCount, cmp.currentCompletedCount],
                itemStyle: { color: CF_C.dark, borderRadius: [6, 6, 0, 0] }
            }
        ]
    });
}

function cfRenderDeptSpeedChart(rows) {
    if (!rows || !rows.length) return;
    const chart = cfMakeChartCard("cfChartsRow1", "ترتيب الإدارات حسب سرعة الإنجاز", "الأسرع أعلى");
    if (!chart) return;
    const top = rows.slice(0, 10);
    const labels = top.map(function (r) { return r.department; }).reverse();
    const values = top.map(function (r) { return r.avgDurationDays; }).reverse();
    chart.setOption({
        tooltip: cfTooltip({
            trigger: "axis", axisPointer: { type: "shadow" },
            formatter: function (p) { return p[0].name + ": " + p[0].value + " يوم بالمتوسط"; }
        }),
        grid: { left: 130, right: 24, top: 10, bottom: 10 },
        xAxis: cfAxisStyle({ type: "value", name: "متوسط الأيام" }),
        yAxis: cfAxisStyle({ type: "category", data: labels }),
        series: [{
            type: "bar", data: values, barMaxWidth: 18,
            itemStyle: {
                color: function (params) {
                    const v = params.value;
                    return v <= 30 ? CF_C.dark : (v <= 60 ? CF_C.warn : CF_C.red);
                },
                borderRadius: [0, 6, 6, 0]
            }
        }]
    });
}

/* ==========================================================================
   توزيع الحالات العاجلة حسب النوع (دونات بمركز نصي)
   ========================================================================== */
function cfRenderAttentionTypeDonut(items) {
    if (!items.length) return;
    const chart = cfMakeChartCard("cfChartsRow2", "أنواع الحالات العاجلة", "توزيع حسب النوع");
    if (!chart) return;

    const counts = {};
    items.forEach(function (it) { counts[it.type] = (counts[it.type] || 0) + 1; });
    const labels = Object.keys(counts);
    const palette = [CF_C.red, CF_C.warn, CF_C.mid, CF_C.dark, CF_C.light];
    const data = labels.map(function (l, i) { return { name: l, value: counts[l], itemStyle: { color: palette[i % palette.length] } }; });

    chart.setOption({
        tooltip: cfTooltip({ trigger: "item", formatter: "{b}: {c} ({d}%)" }),
        legend: { type: "scroll", bottom: 0, textStyle: Object.assign({}, CF_AXIS_LABEL_STYLE), itemWidth: 10, itemHeight: 10 },
        title: {
            text: cfFormatNum(items.length), subtext: "إجمالي الحالات",
            left: "center", top: "38%",
            textStyle: { fontFamily: "Cairo, sans-serif", fontSize: 22, fontWeight: 800, color: CF_C.text },
            subtextStyle: { fontSize: 12, color: CF_C.muted, fontWeight: 700 }
        },
        series: [{
            type: "pie", radius: ["48%", "72%"], center: ["50%", "42%"],
            avoidLabelOverlap: true,
            itemStyle: { borderColor: "#fff", borderWidth: 2 },
            label: { show: false },
            data: data
        }]
    });
}

/* ==========================================================================
   التنبيهات حسب مستوى الخطورة (شريط واحد مقسّم بالألوان 🟢🟡🔴)
   ========================================================================== */
function cfRenderSeverityBar(items) {
    if (!items.length) return;
    const chart = cfMakeChartCard("cfChartsRow2", "التنبيهات حسب مستوى الخطورة", "🟢 🟡 🔴");
    if (!chart) return;

    const green = items.filter(function (i) { return i.alertLevel === "green"; }).length;
    const yellow = items.filter(function (i) { return i.alertLevel === "yellow"; }).length;
    const red = items.filter(function (i) { return i.alertLevel === "red"; }).length;

    chart.setOption({
        tooltip: cfTooltip({ trigger: "axis", axisPointer: { type: "shadow" } }),
        grid: { left: 16, right: 16, top: 16, bottom: 40 },
        legend: { bottom: 0, textStyle: Object.assign({}, CF_AXIS_LABEL_STYLE), itemWidth: 10, itemHeight: 10 },
        xAxis: cfAxisStyle({ type: "value", show: false }),
        yAxis: cfAxisStyle({ type: "category", data: ["الحالات"], axisLabel: { show: false }, axisLine: { show: false }, splitLine: { show: false } }),
        series: [
            { name: "🟢 أقل من 30 يوم", type: "bar", stack: "s", data: [green], barWidth: 60, itemStyle: { color: CF_C.limeDk }, label: { show: true, formatter: "{c}", color: "#fff", fontWeight: 700 } },
            { name: "🟡 من 30 إلى 60 يوم", type: "bar", stack: "s", data: [yellow], barWidth: 60, itemStyle: { color: CF_C.warn }, label: { show: true, formatter: "{c}", color: "#fff", fontWeight: 700 } },
            { name: "🔴 أكثر من 60 يوم", type: "bar", stack: "s", data: [red], barWidth: 60, itemStyle: { color: CF_C.red, borderRadius: [0, 8, 8, 0] }, label: { show: true, formatter: "{c}", color: "#fff", fontWeight: 700 } }
        ]
    });
}

/* ==========================================================================
   أكثر الإدارات التي تحتاج متابعة عاجلة
   ========================================================================== */
function cfRenderTopAttentionDepts(items) {
    if (!items.length) return;
    const chart = cfMakeChartCard("cfChartsRow3", "أكثر الإدارات التي تحتاج متابعة عاجلة", "أعلى 10 إدارات");
    if (!chart) return;

    const counts = {};
    items.forEach(function (it) {
        if (!it.department) return;
        counts[it.department] = (counts[it.department] || 0) + 1;
    });
    const sorted = Object.keys(counts).map(function (k) { return { label: k, count: counts[k] }; })
        .sort(function (a, b) { return b.count - a.count; }).slice(0, 10);

    const labels = sorted.map(function (x) { return x.label; }).reverse();
    const values = sorted.map(function (x) { return x.count; }).reverse();

    chart.setOption({
        tooltip: cfTooltip({ trigger: "axis", axisPointer: { type: "shadow" } }),
        grid: { left: 150, right: 24, top: 10, bottom: 10 },
        xAxis: cfAxisStyle({ type: "value" }),
        yAxis: cfAxisStyle({ type: "category", data: labels }),
        series: [{
            type: "bar", data: values, barMaxWidth: 18,
            itemStyle: { color: CF_C.red, borderRadius: [0, 6, 6, 0] }
        }]
    });
}

/* ==========================================================================
   دورة التحميل
   ========================================================================== */
function cfToggleNoData(show) {
    const content = document.getElementById("claimsFollowupContent");
    const box = document.getElementById("cfNoDataBox");
    if (content) content.style.display = show ? "none" : "";
    if (box) box.style.display = show ? "" : "none";
}

function cfDisposeCharts() {
    cfChartRegistry.forEach(function (c) { try { c.dispose(); } catch (e) {} });
    cfChartRegistry = [];
    ["cfChartsRow1", "cfChartsRow2", "cfChartsRow3"].forEach(function (id) {
        const el = document.getElementById(id);
        if (el) el.innerHTML = "";
    });
}

async function cfLoad() {
    if (!window.CD_FOLLOWUP_DATA_URL) return;
    try {
        const resp = await fetch(window.CD_FOLLOWUP_DATA_URL);
        const data = await resp.json();

        if (!data.hasData) {
            cfToggleNoData(true);
            return;
        }
        cfToggleNoData(false);
        cfDisposeCharts();

        const items = data.attentionItems || [];
        // نسبة المطالبات المتأخرة من إجمالي المطالبات (تُستخدم بحلقة "المطالبات المتأخرة")
        data.overdueSharePercent = data.totalClaims > 0 ? (data.overdueClaimsCount / data.totalClaims * 100) : 0;

        cfRenderGauges(data);

        cfRenderComparisonChart(data.comparison);
        cfRenderDeptSpeedChart(data.departmentSpeedRanking);

        cfRenderAttentionTypeDonut(items);
        cfRenderSeverityBar(items);

        cfRenderTopAttentionDepts(items);
    } catch (err) {
        console.error("تعذر تحميل بيانات متابعة الأداء:", err);
        cfToggleNoData(true);
    }
}

document.addEventListener("DOMContentLoaded", function () {
    cfLoad();
    window.addEventListener("resize", function () {
        cfChartRegistry.forEach(function (c) { try { c.resize(); } catch (e) {} });
    });
});
