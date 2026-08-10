/* ==========================================================================
   صفحة "الطرح" - عقود الطرح: كروت أنواع العقود، توزيع الإدارات العامة، نسبة
   العقود المبرومة، البنود الفرعية (الأنشطة) ضمن إدارة معيّنة، والاتجاه الزمني
   للعقود المبرومة - قابلة للتصفية بالإدارة العامة والنشاط وحالة العقد ورقم العقد.
   ========================================================================== */
const TARH_C = {
    dark: "#0E6E61", mid: "#3F8F87", light: "#5FA89D", lime: "#8DC63F", limeDk: "#5F9E1F",
    red: "#D6534A", target: "#E0A930", text: "#173B36", muted: "#6E8A86", grid: "#E1ECEA",
    axisText: "#181818"
};
const TARH_AXIS_LABEL_STYLE = { color: TARH_C.axisText, fontSize: 13, fontWeight: 600 };
const TARH_PALETTE = [TARH_C.dark, TARH_C.lime, TARH_C.mid, TARH_C.target, TARH_C.red, TARH_C.light, TARH_C.limeDk, "#A9CFC9"];

function tarhAxisStyle(extra) {
    extra = extra || {};
    const merged = Object.assign({
        axisLine: { lineStyle: { color: TARH_C.grid } },
        splitLine: { lineStyle: { color: TARH_C.grid, type: "dashed" } }
    }, extra);
    merged.axisLabel = Object.assign({}, TARH_AXIS_LABEL_STYLE, extra.axisLabel || {});
    return merged;
}
function tarhTooltip(extra) {
    return Object.assign({
        backgroundColor: "#FFFFFF", borderColor: TARH_C.grid, borderWidth: 1,
        textStyle: { color: TARH_C.text, fontSize: 12 },
        extraCssText: "box-shadow:0 4px 16px rgba(0,0,0,0.1);border-radius:10px;"
    }, extra || {});
}
function tarhFormatNum(n) { return Number.isFinite(n) ? Math.round(n).toLocaleString("en-US") : "0"; }
function tarhFormatCompact(n) {
    if (Math.abs(n) >= 1e9) return (n / 1e9).toFixed(1) + "B";
    if (Math.abs(n) >= 1e6) return (n / 1e6).toFixed(1) + "M";
    if (Math.abs(n) >= 1e3) return (n / 1e3).toFixed(1) + "K";
    return (n || 0).toFixed(0);
}
function tarhEscape(str) {
    const div = document.createElement("div");
    div.textContent = (str === null || str === undefined) ? "" : String(str);
    return div.innerHTML;
}
const TARH_SAR_SYMBOL = '<svg class="kpi-sar" viewBox="0 0 1124.14 1256.39" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-label="ريال سعودي">' +
    '<path d="M699.62,1113.02h0c-20.06,44.48-33.32,92.75-38.4,143.37l424.51-90.24c20.06-44.47,33.31-92.75,38.4-143.37l-424.51,90.24Z"/>' +
    '<path d="M1085.73,895.8c20.06-44.47,33.32-92.75,38.4-143.37l-330.68,70.33v-135.2l292.27-62.11c20.06-44.47,33.32-92.75,38.4-143.37l-330.68,70.27V66.13c-50.67,28.45-95.67,66.32-132.25,110.99v403.35l-132.25,28.11V0c-50.67,28.44-95.67,66.32-132.25,110.99v525.69l-295.91,62.88c-20.06,44.47-33.33,92.75-38.42,143.37l334.33-71.05v170.26l-358.3,76.14c-20.06,44.47-33.32,92.75-38.4,143.37l375.04-79.7c30.53-6.35,56.77-24.4,73.83-49.24l68.78-101.97v-.02c7.14-10.55,11.3-23.27,11.3-36.97v-149.98l132.25-28.11v270.4l424.53-90.28Z"/></svg>';
function tarhSarVal(n) { return tarhFormatNum(n) + ' <span class="value-sar">' + TARH_SAR_SYMBOL + '</span>'; }

let tarhCharts = [];
function tarhDisposeCharts() {
    tarhCharts.forEach(function (c) { try { c.dispose(); } catch (e) {} });
    tarhCharts = [];
}

/* ==========================================================================
   شريط الفلاتر: الإدارة العامة / الأنشطة / حالة العقد / رقم العقد
   ========================================================================== */
function tarhMakeSelectWrap(id, iconClass, allLabel, options, selected) {
    const wrap = document.createElement("div");
    wrap.className = "filter-select-wrap";
    wrap.innerHTML = '<i class="bi ' + iconClass + ' filter-icon"></i>';
    const select = document.createElement("select");
    select.id = id;
    select.className = "filter-select";
    let html = '<option value="">' + tarhEscape(allLabel) + '</option>';
    (options || []).forEach(function (o) {
        html += '<option value="' + tarhEscape(o) + '"' + (o === selected ? " selected" : "") + '>' + tarhEscape(o) + '</option>';
    });
    select.innerHTML = html;
    wrap.appendChild(select);
    wrap.insertAdjacentHTML("beforeend", '<i class="bi bi-chevron-down filter-chevron"></i>');
    return { wrap: wrap, select: select };
}

function tarhMakeSearchWrap(id, placeholder, value) {
    const wrap = document.createElement("div");
    wrap.className = "filter-search-wrap";
    wrap.innerHTML = '<i class="bi bi-search filter-icon"></i>';
    const input = document.createElement("input");
    input.type = "text";
    input.id = id;
    input.className = "filter-search-input";
    input.placeholder = placeholder;
    if (value) input.value = value;
    wrap.appendChild(input);
    return { wrap: wrap, input: input };
}

const TARH_QUARTER_LABELS = { "1": "الربع الأول", "2": "الربع الثاني", "3": "الربع الثالث", "4": "الربع الرابع" };

function tarhMakeQuarterWrap(id, selected) {
    const wrap = document.createElement("div");
    wrap.className = "filter-select-wrap";
    wrap.innerHTML = '<i class="bi bi-calendar3 filter-icon"></i>';
    const select = document.createElement("select");
    select.id = id;
    select.className = "filter-select";
    let html = '<option value="">كل الأرباع</option>';
    ["1", "2", "3", "4"].forEach(function (q) {
        html += '<option value="' + q + '"' + (q === selected ? " selected" : "") + '>' + TARH_QUARTER_LABELS[q] + '</option>';
    });
    select.innerHTML = html;
    wrap.appendChild(select);
    wrap.insertAdjacentHTML("beforeend", '<i class="bi bi-chevron-down filter-chevron"></i>');
    return { wrap: wrap, select: select };
}

// السنة تُختار افتراضيًا 2026 أول ما تُفتح الصفحة فقط (لا تُفرَض لاحقًا عند تغيير المستخدمة للفلاتر)
const tarhCurrentFilters = { department: "", status: "", contractNumber: "", year: "2026", quarter: "" };
let tarhSearchDebounce = null;
let tarhFilterBarBuilt = false;

function tarhBuildFilterBar(data) {
    const bar = document.getElementById("tarhFilterBar");
    if (!bar) return;
    bar.innerHTML = "";

    // لا نفرض 2026 إن لم تكن ضمن السنوات المتوفرة فعليًا بالبيانات المستوردة
    if (!tarhFilterBarBuilt && tarhCurrentFilters.year && (data.yearOptions || []).indexOf(2026) === -1) {
        tarhCurrentFilters.year = "";
    }
    tarhFilterBarBuilt = true;

    const yearOptions = (data.yearOptions || []).map(function (y) { return String(y); });
    const year = tarhMakeSelectWrap("tarhYearFilter", "bi-calendar-event", "كل السنوات", yearOptions, tarhCurrentFilters.year);
    const dept = tarhMakeSelectWrap("tarhDeptFilter", "bi-building", "كل الإدارات العامة", data.departmentOptions, tarhCurrentFilters.department);
    const quarter = tarhMakeQuarterWrap("tarhQuarterFilter", tarhCurrentFilters.quarter);
    const status = tarhMakeSelectWrap("tarhStatusFilter", "bi-flag", "كل الحالات", data.statusOptions, tarhCurrentFilters.status);
    const search = tarhMakeSearchWrap("tarhContractNumberFilter", "ابحث برقم العقد...", tarhCurrentFilters.contractNumber);

    bar.appendChild(year.wrap);
    bar.appendChild(dept.wrap);
    bar.appendChild(quarter.wrap);
    bar.appendChild(status.wrap);
    bar.appendChild(search.wrap);

    year.select.addEventListener("change", function () { tarhCurrentFilters.year = year.select.value; tarhLoad(); });
    dept.select.addEventListener("change", function () { tarhCurrentFilters.department = dept.select.value; tarhLoad(); });
    quarter.select.addEventListener("change", function () { tarhCurrentFilters.quarter = quarter.select.value; tarhLoad(); });
    status.select.addEventListener("change", function () { tarhCurrentFilters.status = status.select.value; tarhLoad(); });
    search.input.addEventListener("input", function () {
        tarhCurrentFilters.contractNumber = search.input.value.trim();
        clearTimeout(tarhSearchDebounce);
        tarhSearchDebounce = setTimeout(tarhLoad, 400);
    });
}

/* ==========================================================================
   كروت أنواع العقود (عدد + قيمة لكل نوع)
   ========================================================================== */
function tarhRenderKpiCards(typeSummary, totalOpportunities) {
    const grid = document.getElementById("tarhKpiGrid");
    if (!grid) return;

    const cls = ["c1", "c2", "c4", "c3", "c5"];
    const icons = ["bi-file-earmark-ruled", "bi-building", "bi-signpost-split", "bi-key", "bi-briefcase"];

    var html = '<div class="kpi-card c2">' +
        '<div class="icon"><i class="bi bi-lightbulb"></i></div>' +
        '<div class="label">إجمالي عدد الفرص</div>' +
        '<div class="value">' + tarhFormatNum(totalOpportunities || 0) + '</div>' +
    '</div>';

    html += (typeSummary || []).map(function (t, i) {
        return '<div class="kpi-card ' + cls[i % cls.length] + '">' +
            '<div class="icon"><i class="bi ' + icons[i % icons.length] + '"></i></div>' +
            '<div class="label">' + tarhEscape(t.typeLabel) + '</div>' +
            '<div class="value">' + tarhSarVal(t.totalValue) + '</div>' +
            '<div style="font-size:.78rem;font-weight:700;color:var(--muted);margin-top:4px;">' + tarhFormatNum(t.count) + ' عقد</div>' +
        '</div>';
    }).join("");

    grid.innerHTML = html;
}

/* ==========================================================================
   رسم 1: توزيع الإدارات العامة - أعمدة أفقية (قيمة كطول العمود + عدد كتسمية)،
   بنفس أسلوب رسمة "البنود الفرعية" (Power BI) لسهولة القراءة
   ========================================================================== */
function tarhBuildDeptChartOption(deptChart) {
    const sorted = deptChart.slice().sort(function (a, b) { return a.totalValue - b.totalValue; });
    const categories = sorted.map(function (d) { return d.deptLabel; });
    const values = sorted.map(function (d) { return d.totalValue; });
    const counts = sorted.map(function (d) { return d.count; });
    const barData = sorted.map(function (d, i) {
        return { value: d.totalValue, itemStyle: { color: TARH_PALETTE[i % TARH_PALETTE.length], borderRadius: [0, 4, 4, 0] } };
    });

    return {
        tooltip: tarhTooltip({
            trigger: "axis", axisPointer: { type: "shadow" },
            formatter: function (params) {
                var i = params[0].dataIndex;
                var lines = '<div style="font-weight:700;margin-bottom:6px;">' + tarhEscape(categories[i]) + '</div>';
                lines += '<div>عدد العقود: <strong>' + tarhFormatNum(counts[i]) + '</strong></div>';
                lines += '<div>القيمة: <strong>' + tarhFormatNum(values[i]) + '</strong></div>';
                return lines;
            }
        }),
        grid: { left: 200, right: 110, top: 20, bottom: 20 },
        xAxis: tarhAxisStyle({ type: "value", axisLabel: { formatter: tarhFormatCompact } }),
        yAxis: tarhAxisStyle({ type: "category", data: categories, axisLabel: { fontSize: 11, width: 190, overflow: "break" } }),
        series: [{
            name: "قيمة العقود", type: "bar", data: barData, barMaxWidth: 26, barCategoryGap: "35%", z: 1,
            label: {
                show: true, position: "right", color: TARH_C.text, fontWeight: 700, fontSize: 11,
                formatter: function (p) { return tarhFormatCompact(p.value) + " (" + tarhFormatNum(counts[p.dataIndex]) + " عقد)"; }
            }
        }]
    };
}

/* ==========================================================================
   رسم 2: نسبة العقود المبرومة (Gauge)
   ========================================================================== */
function tarhBuildFinalizedGaugeOption(percent, finalizedCount, totalCount) {
    return {
        series: [{
            type: "gauge",
            startAngle: 200, endAngle: -20,
            min: 0, max: 100,
            radius: "85%",
            axisLine: { lineStyle: { width: 16, color: [[percent / 100, TARH_C.lime], [1, TARH_C.grid]] } },
            pointer: { show: false },
            axisTick: { show: false }, splitLine: { show: false }, axisLabel: { show: false },
            anchor: { show: false },
            title: { show: false },
            detail: {
                valueAnimation: true,
                offsetCenter: [0, "-10%"],
                formatter: function () { return percent.toFixed(1) + "%"; },
                fontSize: 30, fontWeight: 800, color: TARH_C.text, fontFamily: "Cairo"
            },
            data: [{ value: percent }]
        }],
        graphic: [{
            type: "text",
            left: "center", top: "68%",
            style: {
                text: tarhFormatNum(finalizedCount) + " من أصل " + tarhFormatNum(totalCount) + " عقد مبرومة",
                fontSize: 13, fontWeight: 600, fill: TARH_C.muted
            }
        }]
    };
}

/* ==========================================================================
   رسم 3: البنود الفرعية (الأنشطة) ضمن الإدارة المختارة - أعمدة أفقية
   ========================================================================== */
function tarhBuildActivityChartOption(activityChart) {
    const categories = activityChart.map(function (a) { return a.activityLabel; });
    const values = activityChart.map(function (a) { return a.totalValue; });
    const counts = activityChart.map(function (a) { return a.count; });

    return {
        tooltip: tarhTooltip({
            trigger: "axis", axisPointer: { type: "shadow" },
            formatter: function (params) {
                var i = params[0].dataIndex;
                var lines = '<div style="font-weight:700;margin-bottom:6px;">' + tarhEscape(categories[i]) + '</div>';
                lines += '<div>القيمة: <strong>' + tarhFormatNum(values[i]) + '</strong></div>';
                lines += '<div>عدد العقود: <strong>' + tarhFormatNum(counts[i]) + '</strong></div>';
                return lines;
            }
        }),
        grid: { left: 170, right: 90, top: 20, bottom: 20 },
        xAxis: tarhAxisStyle({ type: "value", axisLabel: { formatter: tarhFormatCompact } }),
        yAxis: tarhAxisStyle({ type: "category", data: categories, axisLabel: { fontSize: 11, width: 160, overflow: "break" } }),
        series: [{
            name: "قيمة العقود", type: "bar", data: values, barMaxWidth: 18, barCategoryGap: "40%", z: 1,
            itemStyle: { color: TARH_C.mid, borderRadius: [0, 4, 4, 0] },
            label: {
                show: true, position: "right", color: TARH_C.mid, fontWeight: 700, fontSize: 10,
                formatter: function (p) { return tarhFormatCompact(p.value) + " (" + tarhFormatNum(counts[p.dataIndex]) + " عقد)"; }
            }
        }]
    };
}

/* ==========================================================================
   رسم 4: الاتجاه الزمني للعقود المبرومة (خط)
   ========================================================================== */
function tarhBuildYearlyTrendOption(yearlyTrend) {
    const years = yearlyTrend.map(function (y) { return String(y.year); });
    const counts = yearlyTrend.map(function (y) { return y.count; });

    return {
        tooltip: tarhTooltip({ trigger: "axis", formatter: function (params) {
            return '<div style="font-weight:700;">' + params[0].axisValue + '</div><div>عدد العقود المبرومة: <strong>' + tarhFormatNum(params[0].value) + '</strong></div>';
        } }),
        grid: { left: 60, right: 30, top: 30, bottom: 40 },
        xAxis: tarhAxisStyle({ type: "category", data: years }),
        yAxis: tarhAxisStyle({ type: "value", axisLabel: { formatter: tarhFormatCompact } }),
        series: [{
            name: "عقود مبرومة", type: "line", data: counts, smooth: true, z: 2,
            areaStyle: { color: "rgba(14,110,97,0.12)" },
            lineStyle: { color: TARH_C.dark, width: 3 },
            itemStyle: { color: TARH_C.dark }, symbol: "circle", symbolSize: 8,
            label: { show: true, position: "top", color: TARH_C.dark, fontWeight: 700, fontSize: 12 }
        }]
    };
}

/* ==========================================================================
   الفرص المتأخرة: فتحت مظاريفها منذ 30 يومًا فأكثر ولا يوجد لها عقد بعد - جدول
   أحمر يعرض أقدم الفرص أولاً
   ========================================================================== */
function tarhRenderAtRiskTable(rows) {
    const section = document.getElementById("tarhAtRiskSection");
    if (!section) return;

    rows = rows || [];
    if (rows.length === 0) {
        section.innerHTML = "";
        return;
    }

    var rowsHtml = rows.map(function (r) {
        var d = new Date(r.envelopeOpeningDate);
        var dateStr = isNaN(d.getTime()) ? "" : d.toLocaleDateString("en-GB");
        return '<tr>' +
            '<td>' + tarhEscape(r.opportunityNumber || "—") + '</td>' +
            '<td>' + tarhEscape(r.opportunityName || "—") + '</td>' +
            '<td>' + tarhEscape(r.generalDepartment || "غير محدد") + '</td>' +
            '<td>' + tarhEscape(r.status || "—") + '</td>' +
            '<td>' + dateStr + '</td>' +
            '<td style="color:' + TARH_C.red + ';font-weight:800;">' + tarhFormatNum(r.daysSinceEnvelope) + ' يوم</td>' +
        '</tr>';
    }).join("");

    section.innerHTML =
        '<div class="chart-card" style="border-color:' + TARH_C.red + ';">' +
            '<div class="chart-header">' +
                '<div class="chart-title" style="color:' + TARH_C.red + ';">' +
                    '<i class="bi bi-exclamation-triangle-fill"></i> الفرص المتأخرة - فتح المظاريف منذ 30 يومًا فأكثر بلا عقد' +
                '</div>' +
                '<div class="chart-badge">' + tarhFormatNum(rows.length) + ' فرصة</div>' +
            '</div>' +
            '<div class="contract-table-wrap" style="max-height:420px;">' +
                '<table class="contract-table">' +
                    '<thead><tr>' +
                        '<th>رقم الفرصة</th><th>اسم الفرصة</th><th>الإدارة العامة</th><th>الحالة</th><th>تاريخ فتح المظاريف</th><th>عدد الأيام</th>' +
                    '</tr></thead>' +
                    '<tbody>' + rowsHtml + '</tbody>' +
                '</table>' +
            '</div>' +
        '</div>';
}

function tarhRenderChart(rowId, containerId, title, badge, option, extraClass) {
    const row = document.getElementById(rowId);
    if (!row) return;
    const id = containerId + "_" + Date.now() + "_" + Math.floor(Math.random() * 1000);
    const div = document.createElement("div");
    div.className = "chart-card" + (extraClass ? " " + extraClass : "");
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">' + tarhEscape(title) + '</div>' +
            '<div class="chart-badge">' + tarhEscape(badge) + '</div>' +
        '</div>' +
        '<div class="echart-box" id="' + id + '"></div>';
    row.appendChild(div);
    const chart = echarts.init(document.getElementById(id));
    chart.setOption(option);
    tarhCharts.push(chart);
}

/* ==========================================================================
   التحميل الرئيسي
   ========================================================================== */
async function tarhLoad() {
    const contentEl = document.getElementById("tarhContent");
    const noDataEl = document.getElementById("tarhNoDataBox");

    try {
        var url = window.TARH_DATA_URL + "?department=" + encodeURIComponent(tarhCurrentFilters.department) +
            "&status=" + encodeURIComponent(tarhCurrentFilters.status) +
            "&contractNumber=" + encodeURIComponent(tarhCurrentFilters.contractNumber) +
            "&year=" + encodeURIComponent(tarhCurrentFilters.year) +
            "&quarter=" + encodeURIComponent(tarhCurrentFilters.quarter);

        const resp = await fetch(url);
        if (!resp.ok) throw new Error("network");
        const data = await resp.json();

        tarhDisposeCharts();
        ["tarhChartsRow1", "tarhChartsRow2", "tarhChartsRow3"].forEach(function (id) {
            const el = document.getElementById(id);
            if (el) el.innerHTML = "";
        });
        var atRiskEl = document.getElementById("tarhAtRiskSection");
        if (atRiskEl) atRiskEl.innerHTML = "";

        tarhBuildFilterBar(data);

        if (!data.hasData) {
            if (contentEl) contentEl.style.display = "none";
            if (noDataEl) noDataEl.style.display = "";
            return;
        }

        if (contentEl) contentEl.style.display = "";
        if (noDataEl) noDataEl.style.display = "none";

        tarhRenderKpiCards(data.typeSummary, data.totalOpportunities);

        tarhRenderChart("tarhChartsRow1", "tarhDeptChart", "قيمة وعدد العقود حسب الإدارة العامة", "الإدارات", tarhBuildDeptChartOption(data.deptChart), "ft-items-card");
        tarhRenderChart("tarhChartsRow1", "tarhFinalizedGauge", "نسبة العقود المبرومة", "الإبرام", tarhBuildFinalizedGaugeOption(data.finalizedPercent, data.finalizedCount, data.totalCount), "ft-items-card");

        tarhRenderChart(
            "tarhChartsRow2", "tarhActivityChart",
            "البنود الفرعية (المحفظة الفرعية) - " + data.activitiesDepartment,
            "حسب البند الفرعي",
            tarhBuildActivityChartOption(data.activityChart),
            "ft-items-card"
        );

        tarhRenderChart("tarhChartsRow3", "tarhYearlyTrend", "الاتجاه الزمني للعقود المبرومة", "سنويًا", tarhBuildYearlyTrendOption(data.yearlyFinalizedTrend));

        tarhRenderAtRiskTable(data.atRiskOpportunities);
    } catch (err) {
        console.error("تعذر تحميل بيانات الطرح:", err);
        if (contentEl) contentEl.style.display = "none";
        if (noDataEl) noDataEl.style.display = "";
    }
}

/* ==========================================================================
   رفع ملف "الطرح"
   ========================================================================== */
function tarhSetUploadStatus(text, isError) {
    const el = document.getElementById("tarhUploadStatus");
    if (!el) return;
    el.textContent = text || "";
    el.className = "tc-upload-status" + (isError ? " tc-upload-status-error" : text ? " tc-upload-status-ok" : "");
}

function tarhInitUpload() {
    const btn = document.getElementById("tarhUploadBtn");
    const input = document.getElementById("tarhUploadInput");
    if (!btn || !input || !window.TARH_UPLOAD_URL) return;

    btn.addEventListener("click", function () { input.click(); });

    input.addEventListener("change", async function () {
        const file = input.files && input.files[0];
        if (!file) return;

        btn.disabled = true;
        tarhSetUploadStatus("جارِ رفع الملف...", false);

        try {
            const formData = new FormData();
            formData.append("contractsFile", file);

            const resp = await fetch(window.TARH_UPLOAD_URL, { method: "POST", body: formData });
            const result = await resp.json();

            if (result.success) {
                tarhSetUploadStatus(result.message, false);
                tarhLoad();
            } else {
                tarhSetUploadStatus(result.message || "تعذّر رفع الملف.", true);
            }
        } catch (err) {
            console.error("تعذّر رفع ملف الطرح:", err);
            tarhSetUploadStatus("تعذّر رفع الملف، تحقق من الاتصال وحاول مجددًا.", true);
        } finally {
            btn.disabled = false;
            input.value = "";
        }
    });
}

document.addEventListener("DOMContentLoaded", function () {
    if (!window.TARH_DATA_URL) return;

    tarhLoad();
    tarhInitUpload();

    window.addEventListener("resize", function () {
        tarhCharts.forEach(function (c) { try { c.resize(); } catch (e) {} });
    });
});
