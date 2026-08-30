/* ==========================================================================
   صفحة "المديونيات لكل البنود" (بدون رسوم): رفع الملف + كرتا KPI (إجمالي القيمة/العدد) +
   دونات (القيمة حسب البند) + أعمدة أفقية (العدد حسب البند) + Lollipop (العدد حسب فترة تاريخ
   الإنشاء) + 3 كروت قيمة لكل فترة
   ========================================================================== */
const DFA_C = {
    dark: "#0E6E61", mid: "#3F8F87", light: "#5FA89D", lime: "#8DC63F", limeDk: "#5F9E1F",
    red: "#D6534A", text: "#173B36", muted: "#6E8A86", grid: "#E1ECEA", axisText: "#181818"
};
const DFA_AXIS_LABEL_STYLE = { color: DFA_C.axisText, fontSize: 13, fontWeight: 600 };
const DFA_PALETTE = [DFA_C.dark, DFA_C.mid, DFA_C.lime, DFA_C.light, DFA_C.limeDk, DFA_C.red];

function dfaAxisStyle(extra) {
    extra = extra || {};
    const merged = Object.assign({
        axisLine: { lineStyle: { color: DFA_C.grid } },
        splitLine: { lineStyle: { color: DFA_C.grid, type: "dashed" } }
    }, extra);
    merged.axisLabel = Object.assign({}, DFA_AXIS_LABEL_STYLE, extra.axisLabel || {});
    return merged;
}
function dfaTooltip(extra) {
    return Object.assign({
        backgroundColor: "#FFFFFF", borderColor: DFA_C.grid, borderWidth: 1,
        textStyle: { color: DFA_C.text, fontSize: 12 },
        extraCssText: "box-shadow:0 4px 16px rgba(0,0,0,0.1);border-radius:10px;"
    }, extra || {});
}
function dfaFormatNum(n) { return Number.isFinite(n) ? Math.round(n).toLocaleString("en-US") : "0"; }
// يفتّح لون hex بمقدار amount (0-255) - يُستخدم لتمييز البند المختار بالدونات بلون فاتح بدل حدّ أسود بارز
function dfaLighten(hex, amount) {
    hex = hex.replace("#", "");
    const num = parseInt(hex, 16);
    const r = Math.min(255, (num >> 16) + amount);
    const g = Math.min(255, ((num >> 8) & 0xff) + amount);
    const b = Math.min(255, (num & 0xff) + amount);
    return "#" + (0x1000000 + r * 0x10000 + g * 0x100 + b).toString(16).slice(1);
}
function dfaEscape(str) {
    const div = document.createElement("div");
    div.textContent = (str === null || str === undefined) ? "" : String(str);
    return div.innerHTML;
}

let dfaCharts = [];
function dfaDisposeCharts() {
    dfaCharts.forEach(function (c) { try { c.dispose(); } catch (e) {} });
    dfaCharts = [];
}

/* ==========================================================================
   شريط الفلاتر: السنة/الربع/الشهر (على عمود "تاريخ الإنشاء") + السنة الهجرية (عمود
   "تاريخ الهجري" الخام - نص وليس تاريخ .NET، فلا يمكن فلترته بربع/شهر ميلادي)
   ========================================================================== */
const DFA_QUARTER_LABELS = { "1": "الربع الأول", "2": "الربع الثاني", "3": "الربع الثالث", "4": "الربع الرابع" };
const DFA_MONTH_LABELS = {
    "1": "يناير", "2": "فبراير", "3": "مارس", "4": "أبريل", "5": "مايو", "6": "يونيو",
    "7": "يوليو", "8": "أغسطس", "9": "سبتمبر", "10": "أكتوبر", "11": "نوفمبر", "12": "ديسمبر"
};
const dfaFilters = { year: "", quarter: "", month: "", hijriYear: "" };

function dfaMakeSelectWrap(id, iconClass, allLabel, options, selected, labelMap) {
    const wrap = document.createElement("div");
    wrap.className = "filter-select-wrap";
    wrap.innerHTML = '<i class="bi ' + iconClass + ' filter-icon"></i>';
    const select = document.createElement("select");
    select.id = id;
    select.className = "filter-select";
    let html = '<option value="">' + dfaEscape(allLabel) + '</option>';
    (options || []).forEach(function (o) {
        const label = labelMap ? labelMap[String(o)] : o;
        html += '<option value="' + dfaEscape(o) + '"' + (String(o) === selected ? " selected" : "") + '>' + dfaEscape(label) + '</option>';
    });
    select.innerHTML = html;
    wrap.appendChild(select);
    wrap.insertAdjacentHTML("beforeend", '<i class="bi bi-chevron-down filter-chevron"></i>');
    return { wrap: wrap, select: select };
}

function dfaBuildFilterBar(data) {
    const bar = document.getElementById("dfaFilterBar");
    if (!bar) return;
    bar.innerHTML = "";

    const year = dfaMakeSelectWrap("dfaYearFilter", "bi-calendar-event", "كل السنوات", data.yearOptions || [], dfaFilters.year);
    const quarter = dfaMakeSelectWrap("dfaQuarterFilter", "bi-calendar3", "كل الأرباع", ["1", "2", "3", "4"], dfaFilters.quarter, DFA_QUARTER_LABELS);
    const month = dfaMakeSelectWrap("dfaMonthFilter", "bi-calendar-month", "كل الأشهر", data.monthOptions || [], dfaFilters.month, DFA_MONTH_LABELS);
    const hijriYear = dfaMakeSelectWrap("dfaHijriYearFilter", "bi-moon-stars", "كل السنوات الهجرية", data.hijriYearOptions || [], dfaFilters.hijriYear);

    bar.appendChild(year.wrap);
    bar.appendChild(quarter.wrap);
    bar.appendChild(month.wrap);
    bar.appendChild(hijriYear.wrap);

    year.select.addEventListener("change", function () { dfaFilters.year = year.select.value; dfaLoad(); });
    quarter.select.addEventListener("change", function () { dfaFilters.quarter = quarter.select.value; dfaLoad(); });
    month.select.addEventListener("change", function () { dfaFilters.month = month.select.value; dfaLoad(); });
    hijriYear.select.addEventListener("change", function () { dfaFilters.hijriYear = hijriYear.select.value; dfaLoad(); });
}

function dfaQueryString() {
    const p = new URLSearchParams();
    if (dfaFilters.year) p.set("year", dfaFilters.year);
    if (dfaFilters.quarter) p.set("quarter", dfaFilters.quarter);
    if (dfaFilters.month) p.set("month", dfaFilters.month);
    if (dfaFilters.hijriYear) p.set("hijriYear", dfaFilters.hijriYear);
    return p.toString();
}

/* ==========================================================================
   كرتا KPI: إجمالي المديونيات (قيمة) وعدد المديونيات (عدد)
   ========================================================================== */
function dfaKpiCardHtml(icon, label, value, colorClass) {
    return '<div class="kpi-card ' + colorClass + '">' +
        '<div class="icon"><i class="bi ' + icon + '"></i></div>' +
        '<div class="label">' + dfaEscape(label) + '</div>' +
        '<div class="value">' + value + '</div>' +
    '</div>';
}

function dfaRenderKpis(data) {
    const grid = document.getElementById("dfaKpiGrid");
    if (!grid) return;
    grid.innerHTML =
        dfaKpiCardHtml("bi-cash-stack", "إجمالي المديونيات", dfaFormatNum(data.totalValue), "c1") +
        dfaKpiCardHtml("bi-receipt", "عدد المديونيات", dfaFormatNum(data.totalCount), "c2");
}

/* ==========================================================================
   الصف الأول: دونات (قيمة المديونيات حسب البند) + أعمدة أفقية (عدد المديونيات حسب البند)
   ========================================================================== */
function dfaRenderChart(rowId, containerId, title, option, heightPx) {
    const row = document.getElementById(rowId);
    if (!row) return null;
    const id = containerId + "_" + Date.now() + "_" + Math.floor(Math.random() * 1000);
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header"><div class="chart-title">' + dfaEscape(title) + '</div></div>' +
        '<div class="echart-box" id="' + id + '" style="height:' + (heightPx || 320) + 'px;"></div>';
    row.appendChild(div);
    const chart = echarts.init(document.getElementById(id));
    chart.setOption(option);
    dfaCharts.push(chart);
    requestAnimationFrame(function () { try { chart.resize(); } catch (e) {} });
    return chart;
}

// التصنيف المختار حاليًا برسمة البنود الفرعية (تُحدَّث عند الضغط على قطعة بالدونات) - null
// يعني لسه ما ضُغط على شي، فتُعرض افتراضيًا بيانات أعلى تصنيف قيمةً (نفس فكرة "أعلى بنود
// الإيرادات" بالصفحة الرئيسية)
let dfaSelectedClassification = null;
let dfaDonutChart = null;
let dfaBarChart = null;
let dfaSubItemsByClassification = [];

function dfaBuildDonutOption(byClassification, totalValue) {
    return {
        tooltip: dfaTooltip({
            formatter: function (p) {
                return '<div style="font-weight:700;">' + dfaEscape(p.name) + '</div>' +
                    '<div>القيمة: <strong>' + dfaFormatNum(p.value) + '</strong> ريال</div>' +
                    '<div>عدد المديونيات: <strong>' + dfaFormatNum(p.data.count) + '</strong></div>';
            }
        }),
        legend: {
            orient: "vertical", right: 10, top: "middle",
            itemWidth: 13, itemHeight: 13, icon: "circle", itemGap: 16,
            textStyle: Object.assign({}, DFA_AXIS_LABEL_STYLE, { fontSize: 14 }),
            formatter: function (name) {
                var item = byClassification.filter(function (c) { return c.classification === name; })[0];
                return name + "   " + (item ? dfaFormatNum(item.totalValue) : "");
            }
        },
        series: [{
            type: "pie",
            radius: ["42%", "66%"],
            center: ["28%", "50%"],
            avoidLabelOverlap: false,
            itemStyle: { borderColor: "#fff", borderWidth: 2 },
            label: { show: false },
            labelLine: { show: false },
            cursor: "pointer",
            emphasis: { scale: false },
            hoverAnimation: false,
            data: byClassification.map(function (c, i) {
                var baseColor = DFA_PALETTE[i % DFA_PALETTE.length];
                var selected = dfaSelectedClassification === c.classification;
                return {
                    name: c.classification, value: c.totalValue, count: c.count,
                    itemStyle: { color: selected ? dfaLighten(baseColor, 70) : baseColor, borderColor: "#fff", borderWidth: 2 }
                };
            })
        }],
        graphic: [
            {
                type: "text", left: "28%", top: "44%",
                style: { text: dfaFormatNum(totalValue), fontSize: 18, fontWeight: 800, fill: DFA_C.text, fontFamily: "Cairo", textAlign: "center" }
            },
            {
                type: "text", left: "28%", top: "56%",
                style: { text: "إجمالي القيمة", fontSize: 10, fontWeight: 700, fill: DFA_C.muted, textAlign: "center" }
            }
        ]
    };
}

// رسمة الأعمدة الأفقية المجاورة للدونات: تعرض البنود الفرعية ("البند") ضمن التصنيف الرئيسي
// المختار حاليًا - طول العمود = القيمة، والعدد يظهر بنهاية كل عمود وبالتلميح معًا
function dfaBuildSubItemsBarOption(subItems) {
    const sorted = (subItems || []).slice().sort(function (a, b) { return a.totalValue - b.totalValue; }); // الأصغر فوق، الأكبر تحت
    return {
        tooltip: dfaTooltip({
            trigger: "axis", axisPointer: { type: "shadow" },
            formatter: function (params) {
                const p = params[0];
                return '<div style="font-weight:700;margin-bottom:4px;">' + dfaEscape(p.name) + '</div>' +
                    '<div>القيمة: <strong>' + dfaFormatNum(p.value) + '</strong> ريال</div>' +
                    '<div>عدد المديونيات: <strong>' + dfaFormatNum(p.data.count) + '</strong></div>';
            }
        }),
        grid: { left: 10, right: 70, top: 10, bottom: 10, containLabel: true },
        xAxis: dfaAxisStyle({ type: "value", axisLabel: { show: false }, splitLine: { show: false } }),
        yAxis: dfaAxisStyle({
            type: "category",
            data: sorted.map(function (c) { return c.category; }),
            axisLabel: { width: 150, overflow: "truncate", fontSize: 12 }
        }),
        series: [{
            type: "bar", barMaxWidth: 20,
            data: sorted.map(function (c, i) {
                return { value: c.totalValue, count: c.count, itemStyle: { color: DFA_PALETTE[i % DFA_PALETTE.length], borderRadius: [0, 4, 4, 0] } };
            }),
            label: {
                show: true, position: "right", distance: 6, color: DFA_C.axisText, fontWeight: 700, fontSize: 11,
                formatter: function (p) { return dfaFormatNum(p.value) + " (" + dfaFormatNum(p.data.count) + ")"; }
            }
        }]
    };
}

function dfaRenderCategoryCharts(data) {
    const row = document.getElementById("dfaChartsRow1");
    if (!row) return;
    const byClassification = data.byClassification || [];
    if (!byClassification.length) return;

    dfaSubItemsByClassification = data.subItemsByClassification || [];
    const defaultBreakdown = dfaSubItemsByClassification[0];
    dfaSelectedClassification = defaultBreakdown ? defaultBreakdown.classificationLabel : null;

    dfaDonutChart = dfaRenderChart("dfaChartsRow1", "dfaDonut", "قيمة المديونيات حسب تصنيف البند", dfaBuildDonutOption(byClassification, data.totalValue));

    const barHeight = Math.min(520, Math.max(320, ((defaultBreakdown ? defaultBreakdown.subItems.length : 0) * 28)));
    const barDiv = document.createElement("div");
    barDiv.className = "chart-card";
    barDiv.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">قيمة وعدد المديونيات حسب البند الفرعي</div>' +
            '<div class="chart-badge" id="dfaBarBadge">' + dfaEscape(dfaSelectedClassification || "") + '</div>' +
        '</div>' +
        '<div class="echart-box" id="dfaBarBox" style="height:' + barHeight + 'px;"></div>';
    row.appendChild(barDiv);
    dfaBarChart = echarts.init(document.getElementById("dfaBarBox"));
    dfaBarChart.setOption(dfaBuildSubItemsBarOption(defaultBreakdown ? defaultBreakdown.subItems : []));
    dfaCharts.push(dfaBarChart);
    requestAnimationFrame(function () { try { dfaBarChart.resize(); } catch (e) {} });

    if (dfaDonutChart) {
        dfaDonutChart.on("click", function (params) {
            if (!params.name || params.name === dfaSelectedClassification) return;
            dfaSelectedClassification = params.name;
            dfaDonutChart.setOption(dfaBuildDonutOption(byClassification, data.totalValue), true);

            const breakdown = dfaSubItemsByClassification.filter(function (b) { return b.classificationLabel === params.name; })[0];
            if (dfaBarChart) dfaBarChart.setOption(dfaBuildSubItemsBarOption(breakdown ? breakdown.subItems : []), true);
            const badge = document.getElementById("dfaBarBadge");
            if (badge) badge.textContent = params.name;
        });
    }
}

/* ==========================================================================
   الصف الثاني: Lollipop أفقي (عدد المديونيات حسب فترة "تاريخ الإنشاء") + 3 كروت قيمة لكل فترة
   ========================================================================== */
const DFA_PERIOD_COLORS = [DFA_C.mid, DFA_C.limeDk, DFA_C.dark];

function dfaFormatMillions(n) {
    return (Number(n) / 1e6).toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function dfaBuildLollipopOption(byPeriod) {
    const labels = byPeriod.map(function (p) { return p.periodLabel; });
    const counts = byPeriod.map(function (p) { return p.count; });
    return {
        tooltip: dfaTooltip({
            trigger: "axis",
            formatter: function (params) {
                const p = params[params.length - 1];
                return '<div style="font-weight:700;">' + dfaEscape(p.name) + '</div><div>العدد: <strong>' + dfaFormatNum(p.value) + '</strong></div>';
            }
        }),
        grid: { left: 10, right: 50, top: 20, bottom: 10, containLabel: true },
        yAxis: dfaAxisStyle({ type: "category", data: labels, inverse: true, axisLabel: { fontSize: 12 } }),
        xAxis: dfaAxisStyle({ type: "value", min: 0 }),
        series: [
            {
                name: "العصا", type: "bar", data: counts, barWidth: 4,
                itemStyle: { color: function (p) { return DFA_PERIOD_COLORS[p.dataIndex % DFA_PERIOD_COLORS.length]; } },
                silent: true, tooltip: { show: false }
            },
            {
                name: "العدد", type: "scatter", data: counts, symbolSize: 22,
                itemStyle: { color: function (p) { return DFA_PERIOD_COLORS[p.dataIndex % DFA_PERIOD_COLORS.length]; } },
                label: { show: true, position: "right", formatter: function (p) { return dfaFormatNum(p.value); }, color: DFA_C.text, fontWeight: 800, fontSize: 12 }
            }
        ]
    };
}

function dfaPeriodCardsHtml(byPeriod) {
    return '<div class="chart-card">' +
        '<div class="chart-header"><div class="chart-title">قيمة المديونيات حسب الفترات</div></div>' +
        '<div style="display:flex;gap:10px;padding:4px 2px;flex-wrap:wrap;">' +
            byPeriod.map(function (p, i) {
                const color = DFA_PERIOD_COLORS[i % DFA_PERIOD_COLORS.length];
                return '<div class="kpi-card" style="margin:0;flex:1 1 0;min-width:130px;">' +
                    '<div class="icon" style="background:' + color + '22;color:' + color + ';"><i class="bi bi-calendar3"></i></div>' +
                    '<div class="label">' + dfaEscape(p.periodLabel) + '</div>' +
                    '<div class="value">' + dfaFormatMillions(p.totalValue) + '</div>' +
                    '<div style="font-size:.75rem;font-weight:700;color:var(--muted);margin-top:2px;">مليون ريال</div>' +
                '</div>';
            }).join("") +
        '</div>' +
    '</div>';
}

function dfaRenderPeriodSection(data) {
    const row = document.getElementById("dfaChartsRow2");
    if (!row) return;
    const byPeriod = data.byPeriod || [];
    if (!byPeriod.length) return;

    dfaRenderChart("dfaChartsRow2", "dfaLollipop", "عدد المديونيات حسب الفترات", dfaBuildLollipopOption(byPeriod), 260);

    const div = document.createElement("div");
    div.innerHTML = dfaPeriodCardsHtml(byPeriod);
    row.appendChild(div.firstElementChild);
}

/* ==========================================================================
   التحميل الرئيسي: يجلب البيانات ويعرضها، أو يظهر صندوق "لا توجد بيانات"
   ========================================================================== */
async function dfaLoad() {
    if (!window.DFA_DATA_URL) return;
    const content = document.getElementById("dfaContent");
    const noData = document.getElementById("dfaNoDataBox");

    try {
        const qs = dfaQueryString();
        const resp = await fetch(window.DFA_DATA_URL + (qs ? "?" + qs : ""));
        const data = await resp.json();

        dfaBuildFilterBar(data);

        dfaDisposeCharts();
        const grid1 = document.getElementById("dfaChartsRow1");
        const grid2 = document.getElementById("dfaChartsRow2");
        if (grid1) grid1.innerHTML = "";
        if (grid2) grid2.innerHTML = "";

        if (!data.hasData) {
            if (content) content.style.display = "none";
            if (noData) noData.style.display = "";
            return;
        }

        if (content) content.style.display = "";
        if (noData) noData.style.display = "none";

        dfaRenderKpis(data);
        dfaRenderCategoryCharts(data);
        dfaRenderPeriodSection(data);
    } catch (err) {
        console.error("تعذّر تحميل بيانات الصفحة:", err);
    }
}

window.addEventListener("resize", function () {
    dfaCharts.forEach(function (c) { try { c.resize(); } catch (e) {} });
});

/* ==========================================================================
   زر رفع الملف
   ========================================================================== */
document.addEventListener("DOMContentLoaded", function () {
    dfaLoad();

    const btn = document.getElementById("dfaUploadBtn");
    const input = document.getElementById("dfaUploadInput");
    const status = document.getElementById("dfaUploadStatus");
    if (!btn || !input) return;

    btn.addEventListener("click", function () {
        input.click();
    });

    input.addEventListener("change", async function () {
        const file = input.files[0];
        if (!file) return;

        status.textContent = "جارِ الرفع...";

        const formData = new FormData();
        formData.append("debtforallFile", file);

        try {
            const resp = await fetch(window.DFA_UPLOAD_URL, {
                method: "POST",
                body: formData
            });
            const result = await resp.json();
            status.textContent = result.message || (result.success ? "تم الرفع بنجاح" : "فشل الرفع");
            if (result.success) dfaLoad();
        } catch (err) {
            console.error("تعذّر رفع الملف:", err);
            status.textContent = "تعذّر رفع الملف.";
        }

        input.value = "";
    });
});
