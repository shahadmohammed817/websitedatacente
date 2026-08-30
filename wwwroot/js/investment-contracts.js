/* ==========================================================================
   صفحة "جميع العقود الاستثمارية" - كروت KPI (إجمالي + لكل نوع عقد)، رسم مكدَّس
   للأجرة السنوية حسب حالة العقد (إدارة عامة/فرعية قابل للترابط)، رسم مركّب
   (مستحق مدفوع/غير مدفوع/متبقي + خط الإجمالي المستحق) حسب الإدارة العامة،
   Funnel للدفعات، ورسمَي أعمدة أفقية مجمّعة (متعثر / تم الإرسال لتنفيذ)
   ========================================================================== */
const INV_C = {
    dark: "#0E6E61", mid: "#3F8F87", light: "#5FA89D", lime: "#8DC63F", limeDk: "#5F9E1F",
    text: "#173B36", muted: "#6E8A86", grid: "#E1ECEA", axisText: "#181818", gray: "#9AA3A1"
};
const INV_AXIS_LABEL_STYLE = { color: INV_C.axisText, fontSize: 13, fontWeight: 600 };
const INV_PALETTE = [INV_C.dark, INV_C.mid, INV_C.lime, INV_C.light, INV_C.limeDk];

function invAxisStyle(extra) {
    extra = extra || {};
    const merged = Object.assign({
        axisLine: { lineStyle: { color: INV_C.grid } },
        splitLine: { lineStyle: { color: INV_C.grid, type: "dashed" } }
    }, extra);
    merged.axisLabel = Object.assign({}, INV_AXIS_LABEL_STYLE, extra.axisLabel || {});
    return merged;
}
function invTooltip(extra) {
    return Object.assign({
        backgroundColor: "#FFFFFF", borderColor: INV_C.grid, borderWidth: 1,
        textStyle: { color: INV_C.text, fontSize: 12 },
        extraCssText: "box-shadow:0 4px 16px rgba(0,0,0,0.1);border-radius:10px;"
    }, extra || {});
}
function invFormatNum(n) { return Number.isFinite(n) ? Math.round(n).toLocaleString("en-US") : "0"; }
function invEscape(str) {
    const div = document.createElement("div");
    div.textContent = (str === null || str === undefined) ? "" : String(str);
    return div.innerHTML;
}

let invCharts = [];
function invDisposeCharts() {
    invCharts.forEach(function (c) { try { c.dispose(); } catch (e) {} });
    invCharts = [];
}

function invRenderChart(rowId, containerId, title, badge, option, heightPx) {
    const row = document.getElementById(rowId);
    if (!row) return null;
    const id = containerId + "_" + Date.now() + "_" + Math.floor(Math.random() * 1000);
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">' + invEscape(title) + '</div>' +
            (badge ? '<div class="chart-badge">' + invEscape(badge) + '</div>' : '') +
        '</div>' +
        '<div class="echart-box" id="' + id + '" style="height:' + (heightPx || 320) + 'px;"></div>';
    row.appendChild(div);
    const chart = echarts.init(document.getElementById(id));
    chart.setOption(option);
    chart._invCard = div;
    invCharts.push(chart);
    requestAnimationFrame(function () { try { chart.resize(); } catch (e) {} });
    return chart;
}

/* ==========================================================================
   شريط الفلاتر: سنة بداية العقد / حالة سريان العقد / البلدية / يوجد زيادة سنوية
   ========================================================================== */
function invMakeSelectWrap(id, iconClass, allLabel, options, selected) {
    const wrap = document.createElement("div");
    wrap.className = "filter-select-wrap";
    wrap.innerHTML = '<i class="bi ' + iconClass + ' filter-icon"></i>';
    const select = document.createElement("select");
    select.id = id;
    select.className = "filter-select";
    let html = '<option value="">' + invEscape(allLabel) + '</option>';
    (options || []).forEach(function (o) {
        var val = (o === null || o === undefined) ? "" : String(o);
        html += '<option value="' + invEscape(val) + '"' + (val === selected ? " selected" : "") + '>' + invEscape(val) + '</option>';
    });
    select.innerHTML = html;
    wrap.appendChild(select);
    wrap.insertAdjacentHTML("beforeend", '<i class="bi bi-chevron-down filter-chevron"></i>');
    return { wrap: wrap, select: select };
}

const invFilters = { year: "", validityStatus: "", baladiyah: "", hasIncrease: "", generalDepartment: "", portfolio: "", investorName: "" };
const invStatusFilter = { status: "" };

function invBuildFilterBar(data) {
    const bar = document.getElementById("invFilterBar");
    if (!bar) return;
    bar.innerHTML = "";

    const year = invMakeSelectWrap("invYearFilter", "bi-calendar3", "كل سنوات بداية العقد", data.yearOptions || [], invFilters.year);
    const validity = invMakeSelectWrap("invValidityFilter", "bi-shield-check", "كل حالات سريان العقد", data.contractValidityStatusOptions || [], invFilters.validityStatus);
    const baladiyah = invMakeSelectWrap("invBaladiyahFilter", "bi-geo-alt", "كل البلديات", data.baladiyahOptions || [], invFilters.baladiyah);
    const generalDepartment = invMakeSelectWrap("invGeneralDepartmentFilter", "bi-building", "كل الإدارات العامة", data.generalDepartmentOptions || [], invFilters.generalDepartment);
    const portfolio = invMakeSelectWrap("invPortfolioFilter", "bi-diagram-3", "كل الإدارات الفرعية", data.portfolioOptions || [], invFilters.portfolio);
    const investor = invMakeSelectWrap("invInvestorFilter", "bi-person", "كل المستثمرين", data.investorNameOptions || [], invFilters.investorName);
    const increase = invMakeSelectWrap("invIncreaseFilter", "bi-graph-up", "هل يوجد زيادة سنوية", ["Y", "N"], invFilters.hasIncrease);
    increase.select.querySelectorAll("option").forEach(function (opt) {
        if (opt.value === "Y") opt.textContent = "يوجد زيادة سنوية";
        if (opt.value === "N") opt.textContent = "لا يوجد زيادة سنوية";
    });

    bar.appendChild(year.wrap);
    bar.appendChild(validity.wrap);
    bar.appendChild(baladiyah.wrap);
    bar.appendChild(generalDepartment.wrap);
    bar.appendChild(portfolio.wrap);
    bar.appendChild(investor.wrap);
    bar.appendChild(increase.wrap);

    year.select.addEventListener("change", function () { invFilters.year = year.select.value; invLoad(); });
    validity.select.addEventListener("change", function () { invFilters.validityStatus = validity.select.value; invLoad(); });
    baladiyah.select.addEventListener("change", function () { invFilters.baladiyah = baladiyah.select.value; invLoad(); });
    generalDepartment.select.addEventListener("change", function () {
        invFilters.generalDepartment = generalDepartment.select.value;
        invFilters.portfolio = "";
        invLoad();
    });
    portfolio.select.addEventListener("change", function () { invFilters.portfolio = portfolio.select.value; invLoad(); });
    investor.select.addEventListener("change", function () { invFilters.investorName = investor.select.value; invLoad(); });
    increase.select.addEventListener("change", function () { invFilters.hasIncrease = increase.select.value; invLoad(); });
}

function invQueryString() {
    const p = new URLSearchParams();
    if (invFilters.year) p.set("year", invFilters.year);
    if (invFilters.validityStatus) p.set("validityStatus", invFilters.validityStatus);
    if (invFilters.baladiyah) p.set("baladiyah", invFilters.baladiyah);
    if (invFilters.hasIncrease) p.set("hasIncrease", invFilters.hasIncrease);
    if (invFilters.generalDepartment) p.set("generalDepartment", invFilters.generalDepartment);
    if (invFilters.portfolio) p.set("portfolio", invFilters.portfolio);
    if (invFilters.investorName) p.set("investorName", invFilters.investorName);
    return p.toString();
}

/* ==========================================================================
   كروت الـKPI: إجمالي العقود+القيمة، ثم كرت لكل نوع عقد
   ========================================================================== */
const INV_SAR = '<svg class="kpi-sar" viewBox="0 0 1124.14 1256.39" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-label="ريال سعودي">' +
    '<path d="M699.62,1113.02h0c-20.06,44.48-33.32,92.75-38.4,143.37l424.51-90.24c20.06-44.47,33.31-92.75,38.4-143.37l-424.51,90.24Z"/>' +
    '<path d="M1085.73,895.8c20.06-44.47,33.32-92.75,38.4-143.37l-330.68,70.33v-135.2l292.27-62.11c20.06-44.47,33.32-92.75,38.4-143.37l-330.68,70.27V66.13c-50.67,28.45-95.67,66.32-132.25,110.99v403.35l-132.25,28.11V0c-50.67,28.44-95.67,66.32-132.25,110.99v525.69l-295.91,62.88c-20.06,44.47-33.33,92.75-38.42,143.37l334.33-71.05v170.26l-358.3,76.14c-20.06,44.47-33.32,92.75-38.4,143.37l375.04-79.7c30.53-6.35,56.77-24.4,73.83-49.24l68.78-101.97v-.02c7.14-10.55,11.3-23.27,11.3-36.97v-149.98l132.25-28.11v270.4l424.53-90.28Z"/></svg>';
function invSarVal(n) { return invFormatNum(n) + ' <span class="value-sar">' + INV_SAR + '</span>'; }

function invRenderKpiCards(data) {
    const grid = document.getElementById("invKpiGrid");
    if (!grid) return;

    const cls = ["c1", "c2", "c4", "c3", "c5"];
    const icons = ["bi-file-earmark-ruled", "bi-building", "bi-signpost-split", "bi-key", "bi-briefcase"];

    var html = '<div class="kpi-card c2">' +
        '<div class="icon"><i class="bi bi-journal-text"></i></div>' +
        '<div class="label">إجمالي عدد العقود</div>' +
        '<div class="value">' + invSarVal(data.totalAnnualValue || 0) + '</div>' +
        '<div style="font-size:.78rem;font-weight:700;color:var(--muted);margin-top:4px;">' + invFormatNum(data.totalCount || 0) + ' عقد</div>' +
    '</div>';

    html += (data.typeSummary || []).map(function (t, i) {
        return '<div class="kpi-card ' + cls[i % cls.length] + '">' +
            '<div class="icon"><i class="bi ' + icons[i % icons.length] + '"></i></div>' +
            '<div class="label">' + invEscape(t.typeLabel) + '</div>' +
            '<div class="value">' + invSarVal(t.totalValue) + '</div>' +
            '<div style="font-size:.78rem;font-weight:700;color:var(--muted);margin-top:4px;">' + invFormatNum(t.count) + ' عقد</div>' +
        '</div>';
    }).join("");

    grid.innerHTML = html;
}

/* ==========================================================================
   رسم 1: عمودان (Column Chart عادي) جنب بعض - الأجرة السنوية حسب الإدارة العامة
   والمحفظة الفرعية، بفلتر "حالة العقد" فوق (ضمن شريط الفلاتر الرئيسي - بلا فلتر
   = إجمالي كل الحالات) - أعمدة عريضة بمسافات واضحة، وتسمية محور Xمستقيمة (بلا ميلان)،
   والقيمة مكتوبة فوق كل عمود. الضغط على عمود بالرسم الأول (إدارة) يحدّث رسم المحفظة
   ليعرض محافظ تلك الإدارة (افتراضيًا: أول إدارة)
   ========================================================================== */
function invEntityValue(row, status) {
    if (!row || !row.statusValues) return 0;
    if (status) return row.statusValues[status] || 0;
    return Object.values(row.statusValues).reduce(function (s, v) { return s + v; }, 0);
}

// rates: مصفوفة اختيارية (نسبة تحصيل %) بنفس ترتيب values - إذا مرّرت، تُضاف دائرة صغيرة
// ملوّنة فوق تسمية القيمة بكل عمود (بألوان الشعار حسب مستوى النسبة - بلا أحمر)
function invBuildValueColumnOption(labels, values, highlightIndex, rates) {
    return {
        tooltip: invTooltip({
            trigger: "axis", axisPointer: { type: "shadow" },
            formatter: function (params) {
                var p = params[0];
                var idx = p.dataIndex;
                var html = '<div style="font-weight:700;margin-bottom:4px;">' + invEscape(p.name) + '</div><div>القيمة: <strong>' + invFormatNum(p.value) + '</strong></div>';
                if (rates && rates[idx] !== undefined && rates[idx] !== null) html += '<div>نسبة التحصيل: <strong>' + rates[idx] + '%</strong></div>';
                return html;
            }
        }),
        grid: { left: 16, right: 16, top: rates ? 56 : 30, bottom: 40, containLabel: true },
        xAxis: invAxisStyle({ type: "category", data: labels, axisLabel: { interval: 0, rotate: 0, fontSize: 11, width: 100, overflow: "break" } }),
        yAxis: invAxisStyle({ type: "value", min: 0, axisLabel: { show: false }, splitLine: { show: false } }),
        series: [{
            type: "bar", barMaxWidth: 70, barCategoryGap: "45%", cursor: "pointer",
            data: values.map(function (v, i) {
                var selected = highlightIndex !== undefined && highlightIndex !== null && i === highlightIndex;
                return { value: v, itemStyle: { color: INV_C.dark, borderColor: selected ? INV_C.lime : "transparent", borderWidth: selected ? 3 : 0, borderRadius: [4, 4, 0, 0] } };
            }),
            label: {
                show: true, position: "top", color: INV_C.axisText, fontWeight: 800, fontSize: 14,
                formatter: function (p) {
                    if (!rates || rates[p.dataIndex] === undefined || rates[p.dataIndex] === null) return invFormatNum(p.value);
                    var rate = rates[p.dataIndex];
                    var badgeKey = rate >= 70 ? "badgeGood" : (rate >= 40 ? "badgeMid" : "badgeLow");
                    return '{' + badgeKey + '|' + rate + '%}\n{val|' + invFormatNum(p.value) + '}';
                },
                rich: {
                    val: { fontSize: 14, fontWeight: 800, color: INV_C.axisText, lineHeight: 20 },
                    badgeGood: { fontSize: 10, fontWeight: 800, color: "#fff", backgroundColor: INV_C.mid, borderRadius: 17, width: 34, height: 34, align: "center", lineHeight: 34 },
                    badgeMid: { fontSize: 10, fontWeight: 800, color: "#fff", backgroundColor: INV_C.lime, borderRadius: 17, width: 34, height: 34, align: "center", lineHeight: 34 },
                    badgeLow: { fontSize: 10, fontWeight: 800, color: "#fff", backgroundColor: INV_C.light, borderRadius: 17, width: 34, height: 34, align: "center", lineHeight: 34 }
                }
            }
        }]
    };
}

let invSelectedDeptA = null;
let invDeptChartRef = null;
let invPortfolioChartRef = null;

function invUpdateDeptAndPortfolioCharts(data) {
    const deptRows = data.deptStatusBreakdown || [];
    const deptLabels = deptRows.map(function (r) { return r.departmentLabel; });
    const deptValues = deptRows.map(function (r) { return invEntityValue(r, invStatusFilter.status); });
    const selectedIdx = deptLabels.indexOf(invSelectedDeptA);
    if (invDeptChartRef) invDeptChartRef.setOption(invBuildValueColumnOption(deptLabels, deptValues, selectedIdx), true);

    const portfolioRows = (data.portfolioStatusBreakdown || []).filter(function (r) { return r.departmentLabel === invSelectedDeptA; });
    const portfolioLabels = portfolioRows.map(function (r) { return r.portfolioLabel; });
    const portfolioValues = portfolioRows.map(function (r) { return invEntityValue(r, invStatusFilter.status); });
    if (invPortfolioChartRef) {
        invPortfolioChartRef.setOption(invBuildValueColumnOption(portfolioLabels, portfolioValues), true);
        const badge = invPortfolioChartRef._invCard && invPortfolioChartRef._invCard.querySelector(".chart-badge");
        if (badge) badge.textContent = invSelectedDeptA || "—";
    }
}

function invRenderDeptStatusCharts(data) {
    const deptRows = data.deptStatusBreakdown || [];
    const deptLabels = deptRows.map(function (r) { return r.departmentLabel; });
    invSelectedDeptA = deptLabels.length ? deptLabels[0] : null;
    const deptValues = deptRows.map(function (r) { return invEntityValue(r, invStatusFilter.status); });

    invDeptChartRef = invRenderChart("invChartsRow1", "invDeptStatusChart", "الأجرة السنوية - الإدارة العامة", "اضغط إدارة",
        invBuildValueColumnOption(deptLabels, deptValues, 0), 380);

    const portfolioRows = (data.portfolioStatusBreakdown || []).filter(function (r) { return r.departmentLabel === invSelectedDeptA; });
    const portfolioLabels = portfolioRows.map(function (r) { return r.portfolioLabel; });
    const portfolioValues = portfolioRows.map(function (r) { return invEntityValue(r, invStatusFilter.status); });
    invPortfolioChartRef = invRenderChart("invChartsRow1", "invSubDeptStatusChart", "الأجرة السنوية - المحفظة الفرعية", invSelectedDeptA || "—",
        invBuildValueColumnOption(portfolioLabels, portfolioValues), 380);

    if (invDeptChartRef) {
        invDeptChartRef.on("click", function (params) {
            if (params.componentType !== "series" || !params.name || params.name === invSelectedDeptA) return;
            invSelectedDeptA = params.name;
            invUpdateDeptAndPortfolioCharts(data);
        });
    }
}

// نفس رسم أول صف بصفحة «العقود الاستثمارية - بلدية»: إجمالي الأجرة السنوية لكل حالة عقد.
function invBuildStatusColumnOption(rows) {
    const labels = rows.map(function (r) { return r.statusLabel; });
    const values = rows.map(function (r) { return r.totalValue; });
    return {
        tooltip: invTooltip({
            trigger: "axis", axisPointer: { type: "shadow" },
            formatter: function (params) {
                var p = params[0];
                return '<div style="font-weight:700;margin-bottom:4px;">' + invEscape(p.name) + '</div><div>القيمة: <strong>' + invFormatNum(p.value) + '</strong></div>';
            }
        }),
        grid: { left: 16, right: 16, top: 30, bottom: 40, containLabel: true },
        xAxis: invAxisStyle({ type: "category", data: labels, axisLabel: { interval: 0, rotate: 0, fontSize: 11, width: 100, overflow: "break" } }),
        yAxis: invAxisStyle({ type: "value", min: 0, axisLabel: { show: false }, splitLine: { show: false } }),
        series: [{
            type: "bar", barMaxWidth: 70, barCategoryGap: "45%",
            data: values.map(function (v, i) { return { value: v, itemStyle: { color: INV_PALETTE[i % INV_PALETTE.length], borderRadius: [4, 4, 0, 0] } }; }),
            label: { show: true, position: "top", color: INV_C.axisText, fontWeight: 800, fontSize: 13, formatter: function (p) { return invFormatNum(p.value); } }
        }]
    };
}

function invRenderStatusTotals(data) {
    invRenderChart("invChartsRow1", "invStatusTotalsChart", "الأجرة السنوية حسب حالة العقد", null,
        invBuildStatusColumnOption(data.statusValueTotals || []), 380);
}

/* ==========================================================================
   رسم 2: عمودان مكدَّسان (Stacked) جنب بعض - مدفوع/غير مدفوع فقط، حسب الإدارة
   العامة والمحفظة الفرعية. "متبقي غير مستحق" له رسم منفصل بمحوره الخاص (رسم 2ب)
   لأن قيمته ضخمة جدًا مقارنة بمدفوع/غير مدفوع (يشمل كل الدفعات المستقبلية لعقود
   طويلة المدى) فيطغى بصريًا لو ضُمّ بنفس المحور - بطلب صريح من المستخدمة
   ========================================================================== */
function invBuildStackedPaymentOption(labels, rows) {
    const paid = rows.map(function (r) { return r.duePaidValue; });
    const unpaid = rows.map(function (r) { return r.dueUnpaidValue; });
    const totals = rows.map(function (r, i) { return paid[i] + unpaid[i]; });
    const rates = rows.map(function (r) { return r.ratePercent || 0; });

    function lbl(color) {
        return { show: true, color: color, fontWeight: 700, fontSize: 10, formatter: function (p) { return p.value > 0 ? invFormatNum(p.value) : ""; } };
    }

    return {
        tooltip: invTooltip({
            trigger: "axis", axisPointer: { type: "shadow" },
            formatter: function (params) {
                var idx = params[0].dataIndex;
                return '<div style="font-weight:700;margin-bottom:4px;">' + invEscape(params[0].axisValue) + '</div>' +
                    params.map(function (p) { return '<div>' + p.marker + p.seriesName + ': <strong>' + invFormatNum(p.value) + '</strong></div>'; }).join("") +
                    '<div style="margin-top:4px;border-top:1px solid ' + INV_C.grid + ';padding-top:4px;">نسبة التحصيل: <strong>' + rates[idx] + '%</strong></div>';
            }
        }),
        legend: { top: 0, right: 8, textStyle: Object.assign({}, INV_AXIS_LABEL_STYLE, { fontSize: 11 }), itemWidth: 12, itemHeight: 10 },
        grid: { left: 16, right: 16, top: 70, bottom: 40, containLabel: true },
        xAxis: invAxisStyle({ type: "category", data: labels, axisLabel: { interval: 0, rotate: 0, fontSize: 11, width: 100, overflow: "break" } }),
        yAxis: invAxisStyle({ type: "value", min: 0, axisLabel: { show: false }, splitLine: { show: false } }),
        series: [
            { name: "مدفوع", type: "bar", stack: "s", barMaxWidth: 70, barCategoryGap: "45%", data: paid, itemStyle: { color: INV_C.dark }, label: lbl("#fff") },
            {
                name: "غير مدفوع", type: "bar", stack: "s", data: unpaid, itemStyle: { color: INV_C.mid, borderRadius: [4, 4, 0, 0] },
                label: {
                    show: true, position: "top", color: INV_C.axisText, fontWeight: 800, fontSize: 13,
                    formatter: function (p) {
                        var rate = rates[p.dataIndex];
                        var badgeKey = rate >= 70 ? "badgeGood" : (rate >= 40 ? "badgeMid" : "badgeLow");
                        return '{' + badgeKey + '|' + rate + '%}\n{val|' + invFormatNum(totals[p.dataIndex]) + '}';
                    },
                    rich: {
                        val: { fontSize: 13, fontWeight: 800, color: INV_C.axisText, lineHeight: 20 },
                        badgeGood: { fontSize: 10, fontWeight: 800, color: "#fff", backgroundColor: INV_C.mid, borderRadius: 17, width: 34, height: 34, align: "center", lineHeight: 34 },
                        badgeMid: { fontSize: 10, fontWeight: 800, color: "#fff", backgroundColor: INV_C.lime, borderRadius: 17, width: 34, height: 34, align: "center", lineHeight: 34 },
                        badgeLow: { fontSize: 10, fontWeight: 800, color: "#fff", backgroundColor: INV_C.light, borderRadius: 17, width: 34, height: 34, align: "center", lineHeight: 34 }
                    }
                }
            }
        ]
    };
}

let invSelectedDeptPay = null;
let invPayDeptChartRef = null;
let invPayPortfolioChartRef = null;

function invUpdatePaymentCharts(data) {
    const deptRows = data.deptPaymentBreakdown || [];
    const deptLabels = deptRows.map(function (r) { return r.departmentLabel; });
    if (invPayDeptChartRef) invPayDeptChartRef.setOption(invBuildStackedPaymentOption(deptLabels, deptRows), true);

    const portfolioRows = (data.portfolioPaymentBreakdown || []).filter(function (r) { return r.departmentLabel === invSelectedDeptPay; });
    const portfolioLabels = portfolioRows.map(function (r) { return r.portfolioLabel; });
    if (invPayPortfolioChartRef) {
        invPayPortfolioChartRef.setOption(invBuildStackedPaymentOption(portfolioLabels, portfolioRows), true);
        const badge = invPayPortfolioChartRef._invCard && invPayPortfolioChartRef._invCard.querySelector(".chart-badge");
        if (badge) badge.textContent = invSelectedDeptPay || "—";
    }
}

function invRenderPaymentBreakdown(data) {
    const deptRows = data.deptPaymentBreakdown || [];
    const deptLabels = deptRows.map(function (r) { return r.departmentLabel; });
    invSelectedDeptPay = deptLabels.length ? deptLabels[0] : null;

    invPayDeptChartRef = invRenderChart("invChartsRow2", "invPayDeptChart", "مستحق مدفوع / غير مدفوع - الإدارة العامة", "اضغط إدارة",
        invBuildStackedPaymentOption(deptLabels, deptRows), 380);

    const portfolioRows = (data.portfolioPaymentBreakdown || []).filter(function (r) { return r.departmentLabel === invSelectedDeptPay; });
    const portfolioLabels = portfolioRows.map(function (r) { return r.portfolioLabel; });
    invPayPortfolioChartRef = invRenderChart("invChartsRow2", "invPayPortfolioChart", "مستحق مدفوع / غير مدفوع - المحفظة الفرعية", invSelectedDeptPay || "—",
        invBuildStackedPaymentOption(portfolioLabels, portfolioRows), 380);

    if (invPayDeptChartRef) {
        invPayDeptChartRef.on("click", function (params) {
            if (params.componentType !== "series" || !params.name || params.name === invSelectedDeptPay) return;
            invSelectedDeptPay = params.name;
            invUpdatePaymentCharts(data);
        });
    }
}

/* ==========================================================================
   رسم 2ب: عمودان جنب بعض - "متبقي غير مستحق" فقط، حسب الإدارة العامة والمحفظة
   الفرعية (نفس نمط الترابط، بمحوره الخاص المنفصل عن رسم 2 - قيمته ضخمة جدًا)
   ========================================================================== */
let invSelectedDeptRemaining = null;
let invRemainDeptChartRef = null;
let invRemainPortfolioChartRef = null;

function invUpdateRemainingCharts(data) {
    const deptRows = data.deptPaymentBreakdown || [];
    const deptLabels = deptRows.map(function (r) { return r.departmentLabel; });
    const deptValues = deptRows.map(function (r) { return r.remainingNotDueValue; });
    const selectedIdx = deptLabels.indexOf(invSelectedDeptRemaining);
    if (invRemainDeptChartRef) invRemainDeptChartRef.setOption(invBuildValueColumnOption(deptLabels, deptValues, selectedIdx), true);

    const portfolioRows = (data.portfolioPaymentBreakdown || []).filter(function (r) { return r.departmentLabel === invSelectedDeptRemaining; });
    const portfolioLabels = portfolioRows.map(function (r) { return r.portfolioLabel; });
    const portfolioValues = portfolioRows.map(function (r) { return r.remainingNotDueValue; });
    if (invRemainPortfolioChartRef) {
        invRemainPortfolioChartRef.setOption(invBuildValueColumnOption(portfolioLabels, portfolioValues), true);
        const badge = invRemainPortfolioChartRef._invCard && invRemainPortfolioChartRef._invCard.querySelector(".chart-badge");
        if (badge) badge.textContent = invSelectedDeptRemaining || "—";
    }
}

function invRenderRemainingCharts(data) {
    const deptRows = data.deptPaymentBreakdown || [];
    const deptLabels = deptRows.map(function (r) { return r.departmentLabel; });
    invSelectedDeptRemaining = deptLabels.length ? deptLabels[0] : null;
    const deptValues = deptRows.map(function (r) { return r.remainingNotDueValue; });

    invRemainDeptChartRef = invRenderChart("invChartsRow2b", "invRemainDeptChart", "متبقي غير مستحق - الإدارة العامة", "اضغط إدارة",
        invBuildValueColumnOption(deptLabels, deptValues, 0), 380);

    const portfolioRows = (data.portfolioPaymentBreakdown || []).filter(function (r) { return r.departmentLabel === invSelectedDeptRemaining; });
    const portfolioLabels = portfolioRows.map(function (r) { return r.portfolioLabel; });
    const portfolioValues = portfolioRows.map(function (r) { return r.remainingNotDueValue; });
    invRemainPortfolioChartRef = invRenderChart("invChartsRow2b", "invRemainPortfolioChart", "متبقي غير مستحق - المحفظة الفرعية", invSelectedDeptRemaining || "—",
        invBuildValueColumnOption(portfolioLabels, portfolioValues), 380);

    if (invRemainDeptChartRef) {
        invRemainDeptChartRef.on("click", function (params) {
            if (params.componentType !== "series" || !params.name || params.name === invSelectedDeptRemaining) return;
            invSelectedDeptRemaining = params.name;
            invUpdateRemainingCharts(data);
        });
    }
}

/* ==========================================================================
   رسم 3: دونات "مسار الدفعات" (إجمالي الدفعات ← المستحقة ← المدفوعة ← غير المدفوع ←
   المتعثر - بالعدد) + رسم أعمدة أفقية مجمّعة: متعثر / تم الإرسال لتنفيذ - حسب الإدارة العامة
   ========================================================================== */
function invBuildDonutOption(rows) {
    const items = rows.map(function (r, i) {
        return { name: r.label, value: r.value, itemStyle: { color: INV_PALETTE[i % INV_PALETTE.length] } };
    });
    // مراحل المسار متداخلة؛ «إجمالي الدفعات» (أول مرحلة) هو العدد الكلي الحقيقي، لا مجموع الشرائح.
    const totalPayments = rows.length ? rows[0].value : 0;
    return {
        tooltip: invTooltip({
            trigger: "item",
            formatter: function (p) { return '<div style="font-weight:700;">' + invEscape(p.name) + '</div><div>العدد: <strong>' + invFormatNum(p.value) + '</strong> (' + p.percent + '%)</div>'; }
        }),
        series: [{
            type: "pie", radius: ["42%", "70%"], center: ["50%", "50%"],
            itemStyle: { borderColor: "#fff", borderWidth: 2 },
            label: { show: false }, labelLine: { show: false },
            data: items
        }],
        graphic: [
            {
                type: "text", left: "center", top: "39%",
                style: { text: invFormatNum(totalPayments), fill: INV_C.text, fontFamily: "Cairo", fontSize: 20, fontWeight: 800, textAlign: "center" }
            },
            {
                type: "text", left: "center", top: "55%",
                style: { text: "إجمالي الدفعات", fill: INV_C.muted, fontFamily: "Cairo", fontSize: 10, fontWeight: 700, textAlign: "center" }
            }
        ]
    };
}

// قائمة نصية أسفل الدونات (بعرض البطاقة الكامل) بدل Legend داخلي يقصّ الأسماء الطويلة
function invRenderDonutDetail(elId, rows, total) {
    const wrap = document.getElementById(elId);
    if (!wrap) return;
    wrap.innerHTML = rows.map(function (r, i) {
        var pct = total > 0 ? Math.round((r.value / total) * 1000) / 10 : 0;
        var color = INV_PALETTE[i % INV_PALETTE.length];
        return '<div style="display:flex;align-items:center;justify-content:space-between;gap:10px;padding:7px 0;border-bottom:1px solid var(--border);">' +
            '<span style="display:flex;align-items:center;gap:8px;font-size:.86rem;font-weight:700;color:var(--text);">' +
                '<span style="width:11px;height:11px;border-radius:3px;flex:none;background:' + color + ';"></span> ' + invEscape(r.label) +
            '</span>' +
            '<span style="font-size:.86rem;font-weight:800;white-space:nowrap;">' + invFormatNum(r.value) + ' <span style="font-weight:600;color:var(--muted);font-size:.76rem;">(' + pct + '%)</span></span>' +
        '</div>';
    }).join("");
}

// نفس تخطيط "الشكل الثاني" بصفحة الإدارات العامة (شريط أفقي مكدَّس بدل مجمَّع) لكن بلا
// بادج نسبة - الرقم يبقى دومًا ظاهرًا داخل كل قطعة من الشريط (مو بس عند مرور الماوس)
function invBuildGroupedBarOption(labels, stalledCounts, executionCounts) {
    // مقلوبة لأن محور الفئات يرسم من الأسفل للأعلى، والقيم أصلاً مرتّبة تنازليًا من الخادم
    const revLabels = labels.slice().reverse();
    const revStalled = stalledCounts.slice().reverse();
    const revExecution = executionCounts.slice().reverse();

    function segLabel() {
        return { show: true, position: "inside", color: "#fff", fontWeight: 700, fontSize: 11, formatter: function (p) { return p.value > 0 ? invFormatNum(p.value) : ""; } };
    }

    return {
        tooltip: invTooltip({ trigger: "axis", axisPointer: { type: "shadow" } }),
        legend: { bottom: 0, textStyle: Object.assign({}, INV_AXIS_LABEL_STYLE, { fontSize: 11 }), itemWidth: 10, itemHeight: 10 },
        grid: { left: 190, right: 40, top: 10, bottom: 34 },
        xAxis: invAxisStyle({ type: "value", show: false }),
        yAxis: invAxisStyle({ type: "category", data: revLabels, axisLabel: { width: 180, overflow: "break", lineHeight: 16, fontSize: 12, fontWeight: 600 } }),
        series: [
            { name: "متعثر", type: "bar", stack: "s", barMaxWidth: 26, cursor: "pointer", itemStyle: { color: INV_C.mid }, data: revStalled, label: segLabel() },
            { name: "تم الإرسال لتنفيذ", type: "bar", stack: "s", barMaxWidth: 26, cursor: "pointer", itemStyle: { color: INV_C.lime, borderRadius: [0, 4, 4, 0] }, data: revExecution, label: segLabel() }
        ]
    };
}

let invSelectedDeptB = null;

function invRenderFunnelAndStalled(data) {
    const row3 = document.getElementById("invChartsRow3");
    if (row3) row3.classList.add("inv-funnel-row");

    const donutRows = (data.funnelStages || []).map(function (s) { return { label: s.label, value: s.count }; });
    const total = donutRows.reduce(function (s, r) { return s + r.value; }, 0);
    const donutId = "invFunnelDonut_" + Date.now();
    const donutListId = "invFunnelDonutDetail_" + Date.now();
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header"><div class="chart-title">مسار الدفعات</div></div>' +
        '<div class="echart-box" id="' + donutId + '" style="height:220px;max-width:285px;margin:0 auto;"></div>' +
        '<div id="' + donutListId + '" style="margin-top:10px;"></div>';
    document.getElementById("invChartsRow3").appendChild(div);
    const donutChart = echarts.init(document.getElementById(donutId));
    donutChart.setOption(invBuildDonutOption(donutRows));
    invCharts.push(donutChart);
    requestAnimationFrame(function () { try { donutChart.resize(); } catch (e) {} });
    invRenderDonutDetail(donutListId, donutRows, total);

    const deptRows = data.deptStalledVsExecution || [];
    const deptLabels = deptRows.map(function (r) { return r.departmentLabel; });
    invSelectedDeptB = deptLabels.length ? deptLabels[0] : null;

    const deptChart = invRenderChart("invChartsRow3", "invDeptStalledChart", "متعثر / تم الإرسال لتنفيذ - حسب الإدارة العامة", "اضغط إدارة",
        invBuildGroupedBarOption(deptLabels, deptRows.map(function (r) { return r.stalledCount; }), deptRows.map(function (r) { return r.sentToExecutionCount; })), 400);

    invRenderPortfolioStalled(data, invSelectedDeptB);

    if (deptChart) {
        deptChart.on("click", function (params) {
            if (params.componentType !== "series" || !params.name) return;
            if (params.name === invSelectedDeptB) return;
            invSelectedDeptB = params.name;
            invRenderPortfolioStalled(data, invSelectedDeptB, true);
        });
    }
}

let invPortfolioStalledChart = null;

function invRenderPortfolioStalled(data, deptLabel, isUpdate) {
    const rows = (data.portfolioStalledVsExecution || []).filter(function (r) { return r.departmentLabel === deptLabel; });
    const labels = rows.map(function (r) { return r.portfolioLabel; });
    const option = invBuildGroupedBarOption(labels, rows.map(function (r) { return r.stalledCount; }), rows.map(function (r) { return r.sentToExecutionCount; }));

    if (isUpdate && invPortfolioStalledChart) {
        invPortfolioStalledChart.setOption(option, true);
        const badge = invPortfolioStalledChart._invCard && invPortfolioStalledChart._invCard.querySelector(".chart-badge");
        if (badge) badge.textContent = deptLabel || "—";
        requestAnimationFrame(function () { try { invPortfolioStalledChart.resize(); } catch (e) {} });
        return;
    }

    invPortfolioStalledChart = invRenderChart("invChartsRow4", "invPortfolioStalledChart", "متعثر / تم الإرسال لتنفيذ - حسب المحفظة الفرعية", deptLabel || "—", option, 360);
}

/* ==========================================================================
   رفع الملف
   ========================================================================== */
function invSetUploadStatus(text, isError) {
    const el = document.getElementById("invUploadStatus");
    if (!el) return;
    el.textContent = text || "";
    el.className = "tc-upload-status" + (isError ? " tc-upload-status-error" : text ? " tc-upload-status-ok" : "");
}

function invInitUpload() {
    const btn = document.getElementById("invUploadBtn");
    const input = document.getElementById("invUploadInput");
    if (!btn || !input) return;

    btn.addEventListener("click", function () { input.click(); });
    input.addEventListener("change", async function () {
        const file = input.files && input.files[0];
        if (!file) return;

        btn.disabled = true;
        invSetUploadStatus("جارِ رفع الملف...", false);
        try {
            const formData = new FormData();
            formData.append("contractsFile", file);
            const resp = await fetch(window.INV_UPLOAD_URL, { method: "POST", body: formData });
            const result = await resp.json();
            if (result.success) {
                invSetUploadStatus(result.message, false);
                invLoad();
            } else {
                invSetUploadStatus(result.message || "تعذّر رفع الملف.", true);
            }
        } catch (err) {
            console.error("تعذّر رفع ملف العقود:", err);
            invSetUploadStatus("تعذّر رفع الملف، تحقق من الاتصال وحاول مجددًا.", true);
        } finally {
            btn.disabled = false;
            input.value = "";
        }
    });
}

/* ==========================================================================
   التحميل الرئيسي
   ========================================================================== */
async function invLoad() {
    const contentEl = document.getElementById("invContent");
    const noDataEl = document.getElementById("invNoDataBox");

    try {
        const resp = await fetch(window.INV_DATA_URL + "?" + invQueryString());
        const data = await resp.json();

        invBuildFilterBar(data);

        if (!data.hasData) {
            if (contentEl) contentEl.style.display = "none";
            if (noDataEl) noDataEl.style.display = "";
            return;
        }

        if (contentEl) contentEl.style.display = "";
        if (noDataEl) noDataEl.style.display = "none";

        invDisposeCharts();
        invPortfolioStalledChart = null;
        ["invChartsRow1", "invChartsRow2", "invChartsRow2b", "invChartsRow3", "invChartsRow4"].forEach(function (id) {
            const el = document.getElementById(id);
            if (el) el.innerHTML = "";
        });

        invRenderKpiCards(data);
        invRenderStatusTotals(data);
        invRenderPaymentBreakdown(data);
        invRenderRemainingCharts(data);
        invRenderFunnelAndStalled(data);
    } catch (err) {
        console.error("تعذّر تحميل بيانات العقود الاستثمارية:", err);
        if (contentEl) contentEl.style.display = "none";
        if (noDataEl) noDataEl.style.display = "";
    }
}

document.addEventListener("DOMContentLoaded", function () {
    invInitUpload();
    if (window.INV_DATA_URL) invLoad();
});
