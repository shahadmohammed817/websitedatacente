/* ==========================================================================
   صفحة "المطالبات والمديونيات" (نظرة عامة) - نفس هوية الألوان والمكوّنات
   البصرية المستخدمة ببقية النظام (بطاقات الإيراد الكبيرة من الرئيسية،
   وحلقات "Radial Progress" من صفحة مقارنة المستهدفات)
   ========================================================================== */
const CD_C = {
    dark: "#0E6E61", mid: "#3F8F87", light: "#5FA89D", lime: "#8DC63F", limeDk: "#5F9E1F",
    red: "#D6534A", warn: "#E0A930", text: "#173B36", muted: "#6E8A86", grid: "#E1ECEA",
    axisText: "#181818"
};
const CD_AXIS_LABEL_STYLE = { color: CD_C.axisText, fontSize: 13, fontWeight: 600 };

function cdAxisStyle(extra) {
    extra = extra || {};
    const merged = Object.assign({
        axisLine: { lineStyle: { color: CD_C.grid } },
        splitLine: { lineStyle: { color: CD_C.grid, type: "dashed" } }
    }, extra);
    merged.axisLabel = Object.assign({}, CD_AXIS_LABEL_STYLE, extra.axisLabel || {});
    return merged;
}
function cdTooltip(extra) {
    return Object.assign({
        backgroundColor: "#FFFFFF", borderColor: CD_C.grid, borderWidth: 1,
        textStyle: { color: CD_C.text, fontSize: 12 },
        extraCssText: "box-shadow:0 4px 16px rgba(0,0,0,0.1);border-radius:10px;"
    }, extra || {});
}
function cdFormatNum(n) { return Number.isFinite(n) ? Math.round(n).toLocaleString("en-US") : "0"; }
function cdFormatCompact(n) {
    if (Math.abs(n) >= 1e9) return (n / 1e9).toFixed(1) + "B";
    if (Math.abs(n) >= 1e6) return (n / 1e6).toFixed(1) + "M";
    if (Math.abs(n) >= 1e3) return (n / 1e3).toFixed(1) + "K";
    return Math.round(n).toString();
}
function cdClamp(n, min, max) { return Math.max(min, Math.min(max, n)); }
function cdEscape(str) {
    const div = document.createElement("div");
    div.textContent = str == null ? "" : String(str);
    return div.innerHTML;
}

const CD_SAR = '<svg class="kpi-sar" viewBox="0 0 1124.14 1256.39" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-label="ريال سعودي">' +
    '<path d="M699.62,1113.02h0c-20.06,44.48-33.32,92.75-38.4,143.37l424.51-90.24c20.06-44.47,33.31-92.75,38.4-143.37l-424.51,90.24Z"/>' +
    '<path d="M1085.73,895.8c20.06-44.47,33.32-92.75,38.4-143.37l-330.68,70.33v-135.2l292.27-62.11c20.06-44.47,33.32-92.75,38.4-143.37l-330.68,70.27V66.13c-50.67,28.45-95.67,66.32-132.25,110.99v403.35l-132.25,28.11V0c-50.67,28.44-95.67,66.32-132.25,110.99v525.69l-295.91,62.88c-20.06,44.47-33.33,92.75-38.42,143.37l334.33-71.05v170.26l-358.3,76.14c-20.06,44.47-33.32,92.75-38.4,143.37l375.04-79.7c30.53-6.35,56.77-24.4,73.83-49.24l68.78-101.97v-.02c7.14-10.55,11.3-23.27,11.3-36.97v-149.98l132.25-28.11v270.4l424.53-90.28Z"/></svg>';

let cdChartRegistry = [];

/* ==========================================================================
   البطاقتان الكبيرتان: إجمالي المطالبات (عدد + قيمة)، إجمالي المديونيات (عدد + قيمة)
   نفس تصميم "revenue-kpi-card" المستخدم بالصفحة الرئيسية
   ========================================================================== */
function cdRenderHeroKpis(data) {
    const grid = document.getElementById("cdHeroKpis");
    if (!grid) return;

    function card(cls, iconSvg, tag, label, count, valueLabel, value) {
        return '<div class="revenue-kpi-card ' + cls + '">' +
            '<span class="rk-blob rk-blob-1"></span><span class="rk-blob rk-blob-2"></span>' +
            '<div class="rk-top">' +
                '<div class="rk-icon">' + iconSvg + '</div>' +
                '<span class="rk-tag">' + tag + '</span>' +
            '</div>' +
            '<div class="rk-label">' + label + '</div>' +
            '<div class="rk-value-row"><span class="rk-value" data-count="' + count + '">0</span></div>' +
            '<div class="rk-sub-row"><span>' + valueLabel + ':</span> <strong class="rk-subvalue" data-count="' + value + '">0</strong> ' + CD_SAR + '</div>' +
            '<div class="rk-bar"><span class="rk-bar-fill"></span></div>' +
        '</div>';
    }

    const claimsIcon = '<svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">' +
        '<path d="M7 3h8l4 4v14H7V3Z" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round"/>' +
        '<path d="M15 3v4h4M9 12h6M9 15.5h6M9 8.5h3" stroke="currentColor" stroke-width="1.7" stroke-linecap="round"/></svg>';
    const debtsIcon = '<svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">' +
        '<path d="M12 2 3 6.5V11c0 5.2 3.6 9.9 9 11 5.4-1.1 9-5.8 9-11V6.5L12 2Z" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round"/>' +
        '<path d="M12 8v5M12 16h.01" stroke="currentColor" stroke-width="1.9" stroke-linecap="round"/></svg>';

    grid.innerHTML =
        card("rk-amanah", claimsIcon, "المطالبات", "إجمالي عدد المطالبات", data.totalClaims, "إجمالي القيمة", data.totalClaimsValue) +
        card("rk-baladiyah", debtsIcon, "المديونيات", "إجمالي عدد المديونيات", data.totalDebts, "إجمالي القيمة", data.totalDebtsValue);

    animateHeroValues();
}

function animateHeroValues() {
    const els = document.querySelectorAll("#cdHeroKpis .rk-value, #cdHeroKpis .rk-subvalue");
    const bars = document.querySelectorAll("#cdHeroKpis .rk-bar-fill");
    function animate(el, target, duration) {
        const start = 0;
        let startTime = null;
        function step(ts) {
            if (!startTime) startTime = ts;
            const progress = Math.min((ts - startTime) / duration, 1);
            const eased = 1 - Math.pow(1 - progress, 3);
            el.textContent = cdFormatNum(start + (target - start) * eased);
            if (progress < 1) requestAnimationFrame(step);
            else el.textContent = cdFormatNum(target);
        }
        requestAnimationFrame(step);
    }
    els.forEach(function (el, i) {
        const target = parseFloat(el.getAttribute("data-count")) || 0;
        setTimeout(function () { animate(el, target, 1200); }, i * 100);
    });
    setTimeout(function () { bars.forEach(function (b) { b.style.width = "100%"; }); }, 120);
}

/* ==========================================================================
   بطاقة رسم عامة
   ========================================================================== */
function cdMakeChartCard(containerId, title, badge, extraClass) {
    const row = document.getElementById(containerId);
    if (!row) return null;
    const id = "cdChart_" + Math.random().toString(36).slice(2);
    const div = document.createElement("div");
    div.className = "chart-card" + (extraClass ? " " + extraClass : "");
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">' + title + '</div>' +
            (badge ? '<div class="chart-badge">' + badge + '</div>' : '') +
        '</div>' +
        '<div class="echart-box" id="' + id + '"></div>';
    row.appendChild(div);
    const chart = echarts.init(document.getElementById(id));
    cdChartRegistry.push(chart);
    return chart;
}

/* ==========================================================================
   1) حالة المديونيات حسب البند الفرعي (Stacked Bar)
   🟢 تمت معالجتها | 🟡 لديها مطالبات قيد المعالجة | 🔴 لا يوجد لها مطالبات
   ========================================================================== */
function cdRenderDebtStatusChart(data) {
    if (!data.debtItemLabels || !data.debtItemLabels.length) return;
    const chart = cdMakeChartCard("cdDebtStatusRow", "حالة المديونيات حسب البند الفرعي", "عدد المديونيات");
    if (!chart) return;

    const labels = data.debtItemLabels;

    function dataLabel() {
        return {
            show: true, position: "inside", color: "#fff", fontWeight: 800, fontSize: 11,
            formatter: function (p) { return p.value > 0 ? p.value : ""; }
        };
    }

    chart.setOption({
        tooltip: cdTooltip({ trigger: "axis", axisPointer: { type: "shadow" } }),
        legend: { bottom: 0, textStyle: Object.assign({}, CD_AXIS_LABEL_STYLE), itemWidth: 10, itemHeight: 10 },
        grid: { left: 45, right: 16, top: 20, bottom: 70 },
        xAxis: cdAxisStyle({
            type: "category", data: labels,
            axisLabel: { interval: 0, rotate: labels.length > 5 ? 30 : 0, fontSize: 11, width: 110, overflow: "break", lineHeight: 14 }
        }),
        yAxis: cdAxisStyle({ type: "value" }),
        series: [
            { name: "🟢 تمت معالجتها", type: "bar", stack: "s", data: data.debtItemProcessedCounts, barMaxWidth: 70, itemStyle: { color: CD_C.dark }, label: dataLabel() },
            { name: "🟡 لديها مطالبات قيد المعالجة", type: "bar", stack: "s", data: data.debtItemInProgressCounts, barMaxWidth: 70, itemStyle: { color: CD_C.warn }, label: dataLabel() },
            { name: "🔴 لا يوجد لها مطالبات", type: "bar", stack: "s", data: data.debtItemNoClaimsCounts, barMaxWidth: 70, itemStyle: { color: CD_C.red }, label: dataLabel() }
        ]
    });
}

/* ==========================================================================
   2) توزيع عدد المطالبات لكل مديونية (Histogram) + أعلى العقود من حيث عدد المطالبات
   ========================================================================== */
function cdRenderHistogramChart(data) {
    if (!data.claimsPerDebtBucketLabels || !data.claimsPerDebtBucketLabels.length) return;
    const chart = cdMakeChartCard("cdHistogramRow", "توزيع عدد المطالبات لكل مديونية", "Histogram");
    if (!chart) return;

    const palette = [CD_C.dark, CD_C.mid, CD_C.warn, CD_C.red];
    chart.setOption({
        tooltip: cdTooltip({ trigger: "axis", axisPointer: { type: "shadow" } }),
        grid: { left: 45, right: 16, top: 16, bottom: 40 },
        xAxis: cdAxisStyle({ type: "category", data: data.claimsPerDebtBucketLabels, axisLabel: { fontSize: 11 } }),
        yAxis: cdAxisStyle({ type: "value" }),
        series: [{
            name: "عدد المديونيات", type: "bar", data: data.claimsPerDebtBucketCounts, barMaxWidth: 60,
            itemStyle: { color: function (p) { return palette[p.dataIndex % palette.length]; }, borderRadius: [6, 6, 0, 0] },
            label: { show: true, position: "top", color: CD_C.text, fontWeight: 700, fontSize: 12 }
        }]
    });
}

let cdDebtsWithClaims = [];

function cdRenderDebtsWithClaimsList(filterText) {
    const listEl = document.getElementById("cdDebtsWithClaimsList");
    if (!listEl) return;

    const term = (filterText || "").trim().toLowerCase();
    let rows = cdDebtsWithClaims;
    if (term) {
        rows = rows.filter(function (r) { return r.debtNumber.toLowerCase().indexOf(term) > -1; });
    } else {
        rows = rows.slice(0, 10); // بدون بحث: أعلى 10 فقط
    }

    listEl.innerHTML = rows.length
        ? rows.map(function (r) {
            return '<div class="tc-tb-item"><div class="tc-tb-name">' + cdEscape(r.debtNumber) + '</div>' +
                '<span class="tc-pct-badge tc-pct-mid">' + cdFormatNum(r.claimCount) + ' مطالبة</span></div>';
        }).join("")
        : '<div class="tc-tb-empty">' + (term ? "لا توجد نتائج مطابقة" : "لا توجد مديونيات لها مطالبات") + '</div>';
}

function cdRenderTopContractsCard(data) {
    const row = document.getElementById("cdHistogramRow");
    if (!row) return;
    cdDebtsWithClaims = data.debtsWithClaims || [];

    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title"><i class="bi bi-trophy"></i> المديونيات التي لديها مطالبات</div>' +
            '<div class="chart-badge">أعلى 10</div>' +
        '</div>' +
        '<div class="filter-search-wrap" style="margin-bottom:10px;">' +
            '<i class="bi bi-search filter-icon"></i>' +
            '<input type="text" id="cdDebtSearchInput" class="filter-search-input" placeholder="ابحثي برقم المديونية..." style="width:100%;" />' +
        '</div>' +
        '<div class="tc-tb-list" id="cdDebtsWithClaimsList"></div>';
    row.appendChild(div);

    cdRenderDebtsWithClaimsList("");
    document.getElementById("cdDebtSearchInput").addEventListener("input", function (e) {
        cdRenderDebtsWithClaimsList(e.target.value);
    });
}

/* ==========================================================================
   3) حالة المطالبات حسب البند الفرعي (Column Chart - منجزة/متبقية)
   ========================================================================== */
function cdRenderClaimStatusChart(data) {
    if (!data.claimItemLabels || !data.claimItemLabels.length) return;
    const chart = cdMakeChartCard("cdClaimStatusRow", "حالة المطالبات حسب البند الفرعي", "منجزة / متبقية");
    if (!chart) return;

    const labels = data.claimItemLabels;
    chart.setOption({
        tooltip: cdTooltip({ trigger: "axis", axisPointer: { type: "shadow" } }),
        legend: { bottom: 0, textStyle: Object.assign({}, CD_AXIS_LABEL_STYLE), itemWidth: 10, itemHeight: 10 },
        grid: { left: 45, right: 16, top: 20, bottom: 70 },
        xAxis: cdAxisStyle({
            type: "category", data: labels,
            axisLabel: { interval: 0, rotate: labels.length > 5 ? 30 : 0, fontSize: 11, width: 110, overflow: "break", lineHeight: 14 }
        }),
        yAxis: cdAxisStyle({ type: "value" }),
        series: [
            {
                name: "🟢 تم معالجتها", type: "bar", data: data.claimItemResolvedCounts, barMaxWidth: 34,
                itemStyle: { color: CD_C.dark, borderRadius: [4, 4, 0, 0] },
                label: { show: true, position: "top", color: CD_C.text, fontWeight: 700, fontSize: 11 }
            },
            {
                name: "🔴 متبقية", type: "bar", data: data.claimItemOpenCounts, barMaxWidth: 34,
                itemStyle: { color: CD_C.red, borderRadius: [4, 4, 0, 0] },
                label: { show: true, position: "top", color: CD_C.text, fontWeight: 700, fontSize: 11 }
            }
        ]
    });
}

/* ==========================================================================
   4) توزيع حالات المطالبات إجمالاً (معالجة "مرسلة لسند" مقابل متبقية) - Donut
   ========================================================================== */
function cdRenderClaimPercentChart(data) {
    const resolved = data.resolvedClaimsCount || 0;
    const open = data.openClaimsCount || 0;
    const total = resolved + open;
    if (total <= 0) return;

    const chart = cdMakeChartCard("cdClaimPercentRow", "توزيع حالات المطالبات", "إجمالي");
    if (!chart) return;

    const items = [
        { name: "تمت معالجتها (مرسلة لسند)", value: resolved, itemStyle: { color: CD_C.dark } },
        { name: "متبقية", value: open, itemStyle: { color: CD_C.red } }
    ];

    chart.setOption({
        tooltip: cdTooltip({ trigger: "item", formatter: function (p) {
            return '<div style="font-weight:700;margin-bottom:4px;">' + p.name + '</div>' +
                '<div>العدد: <strong>' + cdFormatNum(p.value) + '</strong> (' + p.percent + '%)</div>';
        } }),
        legend: { bottom: 0, textStyle: Object.assign({}, CD_AXIS_LABEL_STYLE), itemWidth: 10, itemHeight: 10 },
        series: [
            {
                name: "حالة المطالبة", type: "pie", radius: ["45%", "72%"], center: ["50%", "45%"],
                avoidLabelOverlap: true,
                itemStyle: { borderColor: "#fff", borderWidth: 2 },
                label: { formatter: "{b}\n{d}%", fontSize: 11, fontWeight: 700, color: CD_C.text },
                labelLine: { length: 8, length2: 8 },
                data: items
            }
        ]
    });
}

/* ==========================================================================
   دورة التحميل
   ========================================================================== */
function cdToggleNoData(show) {
    const content = document.getElementById("claimsOverviewContent");
    const box = document.getElementById("cdNoDataBox");
    if (content) content.style.display = show ? "none" : "";
    if (box) box.style.display = show ? "" : "none";
}

function cdDisposeCharts() {
    cdChartRegistry.forEach(function (c) { try { c.dispose(); } catch (e) {} });
    cdChartRegistry = [];
    ["cdDebtStatusRow", "cdHistogramRow", "cdClaimStatusRow", "cdClaimPercentRow"].forEach(function (id) {
        const el = document.getElementById(id);
        if (el) el.innerHTML = "";
    });
}

async function cdLoad() {
    if (!window.CD_OVERVIEW_DATA_URL) return;
    try {
        const resp = await fetch(window.CD_OVERVIEW_DATA_URL);
        const data = await resp.json();

        if (!data.hasData) {
            cdToggleNoData(true);
            return;
        }
        cdToggleNoData(false);
        cdDisposeCharts();

        cdRenderHeroKpis(data);

        cdRenderDebtStatusChart(data);

        cdRenderHistogramChart(data);
        cdRenderTopContractsCard(data);

        cdRenderClaimStatusChart(data);
        cdRenderClaimPercentChart(data);
    } catch (err) {
        console.error("تعذر تحميل بيانات المطالبات والمديونيات:", err);
        cdToggleNoData(true);
    }
}

document.addEventListener("DOMContentLoaded", function () {
    cdLoad();
    window.addEventListener("resize", function () {
        cdChartRegistry.forEach(function (c) { try { c.resize(); } catch (e) {} });
    });
});
