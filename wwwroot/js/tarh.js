/* ==========================================================================
   صفحة "الطرح" - عقود الطرح: كروت أنواع العقود، توزيع الإدارات العامة، نسبة
   العقود المبرومة، البنود الفرعية (الأنشطة) ضمن إدارة معيّنة، والاتجاه الزمني
   للعقود المبرومة - قابلة للتصفية بالإدارة العامة والنشاط وحالة العقد ورقم العقد.
   ========================================================================== */
const TARH_C = {
    dark: "#0E6E61", mid: "#3F8F87", light: "#5FA89D", lime: "#8DC63F", limeDk: "#5F9E1F",
    red: "#D6534A", text: "#173B36", muted: "#6E8A86", grid: "#E1ECEA",
    axisText: "#181818"
};
const TARH_AXIS_LABEL_STYLE = { color: TARH_C.axisText, fontSize: 13, fontWeight: 600 };
// تدرّجات ألوان الشعار فقط (بلا أي لون خارج عائلة الأخضر/التركواز الرسمية) - تُستخدَم لتلوين
// كل عمود بلون مختلف بدل لون واحد موحّد لكل الأعمدة
const TARH_PALETTE = [TARH_C.dark, TARH_C.mid, TARH_C.lime, TARH_C.light, TARH_C.limeDk];

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
    const quarter = tarhMakeQuarterWrap("tarhQuarterFilter", tarhCurrentFilters.quarter);
    const search = tarhMakeSearchWrap("tarhContractNumberFilter", "ابحث برقم العقد...", tarhCurrentFilters.contractNumber);

    bar.appendChild(year.wrap);
    bar.appendChild(quarter.wrap);
    bar.appendChild(search.wrap);

    year.select.addEventListener("change", function () { tarhCurrentFilters.year = year.select.value; tarhLoad(); });
    quarter.select.addEventListener("change", function () { tarhCurrentFilters.quarter = quarter.select.value; tarhLoad(); });
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
   رسمان 1+2: قيمة وعدد العقود المبرمة حسب الإدارة العامة / حسب الإدارة الفرعية -
   نفس شكل أعمدة (عدد) + خط (قيمة) لكليهما؛ رسم 1 قابل للضغط لتصفية رسم 2
   ========================================================================== */
function tarhComboItemsFromDept(rows) {
    return (rows || []).map(function (d) { return { label: d.departmentLabel, count: d.count, totalValue: d.totalValue }; });
}
function tarhComboItemsFromSubDept(rows, dept) {
    return (rows || [])
        .filter(function (r) { return r.departmentLabel === dept; })
        .map(function (r) { return { label: r.portfolioLabel, count: r.count, totalValue: r.totalValue }; });
}

function tarhBuildComboBarLineOption(items, highlightLabel) {
    const sorted = items.slice().sort(function (a, b) { return b.totalValue - a.totalValue; });
    const categories = sorted.map(function (d) { return d.label; });
    const values = sorted.map(function (d) { return d.totalValue; });
    const isNarrow = window.innerWidth < 700;
    const barData = sorted.map(function (d, i) {
        var selected = !!highlightLabel && d.label === highlightLabel;
        return {
            value: d.count,
            itemStyle: {
                color: TARH_PALETTE[i % TARH_PALETTE.length], borderRadius: [6, 6, 0, 0],
                borderColor: selected ? TARH_C.text : "transparent", borderWidth: selected ? 2 : 0
            }
        };
    });

    return {
        tooltip: tarhTooltip({
            trigger: "axis", axisPointer: { type: "shadow" },
            formatter: function (params) {
                var i = params[0].dataIndex;
                var lines = '<div style="font-weight:700;margin-bottom:6px;">' + tarhEscape(categories[i]) + '</div>';
                lines += '<div>عدد العقود: <strong>' + tarhFormatNum(sorted[i].count) + '</strong></div>';
                lines += '<div>القيمة: <strong>' + tarhFormatNum(sorted[i].totalValue) + '</strong></div>';
                return lines;
            }
        }),
        legend: { top: 0, right: 8, textStyle: Object.assign({}, TARH_AXIS_LABEL_STYLE), itemWidth: 12, itemHeight: 10 },
        grid: { left: 16, right: 16, top: 56, bottom: isNarrow ? 130 : 110, containLabel: true },
        xAxis: tarhAxisStyle({
            type: "category", data: categories,
            axisLabel: { interval: 0, rotate: 0, fontSize: 11, width: 118, overflow: "break", lineHeight: 15, margin: 12 },
            axisTick: { alignWithLabel: true }
        }),
        yAxis: [
            tarhAxisStyle({ type: "value", axisLabel: { formatter: tarhFormatCompact } }),
            tarhAxisStyle({ type: "value", splitLine: { show: false }, axisLabel: { formatter: tarhFormatCompact } })
        ],
        series: [
            {
                name: "عدد العقود", type: "bar", data: barData, yAxisIndex: 0, barMaxWidth: 34, barCategoryGap: "40%", z: 1,
                cursor: "pointer",
                label: { show: true, position: "top", color: TARH_C.dark, fontWeight: 800, fontSize: 12, formatter: function (p) { return tarhFormatNum(p.value); } }
            },
            {
                name: "القيمة", type: "line", data: values, yAxisIndex: 1, smooth: true, z: 2,
                lineStyle: { color: TARH_C.lime, width: 2.5 },
                itemStyle: { color: TARH_C.lime }, symbol: "circle", symbolSize: 7,
                label: { show: true, position: "top", color: TARH_C.limeDk, fontWeight: 800, fontSize: 12, formatter: function (p) { return tarhFormatCompact(p.value); } }
            }
        ]
    };
}

/* ==========================================================================
   رسم 3: نسبة الطرح ونسبة الإبرام حسب السنة (Line Chart تراكمي) - بلا فلاتر،
   كل السنوات دائمًا. المرجع (100%) لكلا الخطين = إجمالي الفرص المطروحة بكل
   السنوات، حتى تبقى النسبتان قابلتين للمقارنة المباشرة على نفس المقياس
   ========================================================================== */
function tarhBuildYearlyRateOption(rows) {
    const years = rows.map(function (r) { return String(r.year); });
    const tendered = rows.map(function (r) { return r.tenderedPercent; });
    const finalized = rows.map(function (r) { return r.finalizedPercent; });

    return {
        tooltip: tarhTooltip({
            trigger: "axis",
            formatter: function (params) {
                var lines = '<div style="font-weight:700;margin-bottom:4px;">' + params[0].axisValue + '</div>';
                params.forEach(function (p) { lines += '<div>' + p.marker + ' ' + p.seriesName + ': <strong>' + p.value + '%</strong></div>'; });
                return lines;
            }
        }),
        legend: { top: 0, right: 8, textStyle: Object.assign({}, TARH_AXIS_LABEL_STYLE), itemWidth: 12, itemHeight: 10 },
        grid: { left: 50, right: 30, top: 56, bottom: 40 },
        xAxis: tarhAxisStyle({ type: "category", data: years }),
        yAxis: tarhAxisStyle({ type: "value", min: 0, max: 100, axisLabel: { formatter: "{value}%" } }),
        series: [
            {
                name: "نسبة الطرح (تراكمي)", type: "line", data: tendered, smooth: true, z: 2,
                lineStyle: { color: TARH_C.dark, width: 3 }, itemStyle: { color: TARH_C.dark },
                symbol: "circle", symbolSize: 8,
                label: { show: true, position: "top", color: TARH_C.dark, fontWeight: 700, fontSize: 11, formatter: "{c}%" }
            },
            {
                name: "نسبة الإبرام (تراكمي)", type: "line", data: finalized, smooth: true, z: 2,
                lineStyle: { color: TARH_C.lime, width: 3 }, itemStyle: { color: TARH_C.limeDk },
                symbol: "circle", symbolSize: 8,
                label: { show: true, position: "bottom", color: TARH_C.limeDk, fontWeight: 700, fontSize: 11, formatter: "{c}%" }
            }
        ]
    };
}

/* ==========================================================================
   رسم 4: عدد الفرص حسب السنة (كل السنوات، بلا فلاتر) + تفاصيل حالات فتح
   المظاريف للسنة المحددة من فلتر السنة بالصفحة (تتحدّث تلقائيًا معه)
   ========================================================================== */
function tarhRenderYearStatusChart(rowId, envelopeByYearStatus, selectedYearStr) {
    const row = document.getElementById(rowId);
    if (!row) return;

    const totalsByYear = {};
    envelopeByYearStatus.forEach(function (r) { totalsByYear[r.year] = (totalsByYear[r.year] || 0) + r.count; });
    const years = Object.keys(totalsByYear).map(Number).sort(function (a, b) { return a - b; });

    var targetYear = selectedYearStr ? parseInt(selectedYearStr, 10) : null;
    if (!targetYear || years.indexOf(targetYear) === -1) targetYear = years.length ? years[years.length - 1] : null;

    const id = "tarhYearStatusChart_" + Date.now();
    const div = document.createElement("div");
    div.className = "chart-card tarh-yearstatus-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">عدد الفرص حسب السنة</div>' +
            '<div class="chart-badge">سنويًا</div>' +
        '</div>' +
        '<div class="tarh-yearstatus-body">' +
            '<div class="echart-box" id="' + id + '"></div>' +
            '<div class="tarh-yearstatus-detail" id="tarhYearStatusDetail"></div>' +
        '</div>';
    row.appendChild(div);

    const box = document.getElementById(id);
    box.style.height = "360px";
    const chart = echarts.init(box);
    chart.setOption({
        tooltip: tarhTooltip({
            trigger: "axis",
            formatter: function (params) {
                return '<div style="font-weight:700;">' + params[0].axisValue + '</div><div>عدد الفرص: <strong>' + tarhFormatNum(params[0].value) + '</strong></div>';
            }
        }),
        grid: { left: 55, right: 20, top: 20, bottom: 36 },
        xAxis: tarhAxisStyle({ type: "category", data: years.map(String) }),
        yAxis: tarhAxisStyle({ type: "value", axisLabel: { formatter: tarhFormatCompact } }),
        series: [{
            type: "bar", data: years.map(function (y) { return totalsByYear[y]; }), barMaxWidth: 40, cursor: "pointer",
            itemStyle: {
                color: function (params) { return years[params.dataIndex] === targetYear ? TARH_C.limeDk : TARH_C.dark; },
                borderRadius: [6, 6, 0, 0]
            },
            label: { show: true, position: "top", color: TARH_C.dark, fontWeight: 700, fontSize: 12, formatter: function (p) { return tarhFormatNum(p.value); } }
        }]
    });
    tarhCharts.push(chart);
    tarhForceResize(chart);

    tarhRenderYearStatusDetail(envelopeByYearStatus, targetYear);
}

function tarhRenderYearStatusDetail(envelopeByYearStatus, year) {
    const wrap = document.getElementById("tarhYearStatusDetail");
    if (!wrap) return;
    if (year === null) { wrap.innerHTML = '<div class="no-data-box" style="margin:0;">لا توجد بيانات</div>'; return; }

    // لا تُعرض الحالات اللي عددها صفر (أصلًا لا تصل هنا من الـ API، لكن نتأكد صراحة)
    const rows = envelopeByYearStatus.filter(function (r) { return r.year === year && r.count > 0; });
    const total = rows.reduce(function (s, r) { return s + r.count; }, 0);
    const sorted = rows.slice().sort(function (a, b) { return b.count - a.count; });

    var html = '<div class="tarh-yearstatus-detail-title">تفاصيل حالات فتح المظاريف - ' + year + '</div>';
    if (sorted.length === 0) {
        html += '<div class="no-data-box" style="margin:0;">لا توجد بيانات لهذه السنة</div>';
    } else {
        html += sorted.map(function (r) {
            var pct = total > 0 ? Math.round((r.count / total) * 1000) / 10 : 0;
            return '<div class="tarh-yearstatus-row">' +
                '<span class="tarh-yearstatus-row-label">' + tarhEscape(r.status) + '</span>' +
                '<span class="tarh-yearstatus-row-value">' + tarhFormatNum(r.count) + ' <span class="tarh-yearstatus-row-pct">(' + pct + '%)</span></span>' +
            '</div>';
        }).join("");
    }
    wrap.innerHTML = html;
}

function tarhRenderChart(rowId, containerId, title, badge, option, extraClass, heightPx, subtitle, captionText) {
    const row = document.getElementById(rowId);
    if (!row) return;
    const id = containerId + "_" + Date.now() + "_" + Math.floor(Math.random() * 1000);
    const div = document.createElement("div");
    div.className = "chart-card" + (extraClass ? " " + extraClass : "");
    div.innerHTML =
        '<div class="chart-header">' +
            '<div>' +
                '<div class="chart-title">' + tarhEscape(title) + '</div>' +
                (subtitle ? '<div class="chart-subtitle">' + tarhEscape(subtitle) + '</div>' : '') +
            '</div>' +
            '<div class="chart-badge">' + tarhEscape(badge) + '</div>' +
        '</div>' +
        (captionText ? '<div class="tarh-chart-caption">' + tarhEscape(captionText) + '</div>' : '') +
        '<div class="echart-box" id="' + id + '"></div>';
    row.appendChild(div);
    const box = document.getElementById(id);
    // ارتفاع مضبوط مباشرة (Inline) بدل الاعتماد فقط على صنف CSS - يضمن أن echarts.init يقرأ
    // الحجم الصحيح فورًا بلا أي التباس بترتيب أو توقيت تطبيق أنماط CSS الخارجية
    if (heightPx) box.style.height = heightPx + "px";
    const chart = echarts.init(box);
    chart.setOption(option);
    chart._tarhCard = div;
    tarhCharts.push(chart);
    tarhForceResize(chart);
    return chart;
}

// إعادة قياس مؤكدة: الحاوية داخل صف Flex/Grid قد لا يكون حجمها محسوبًا نهائيًا لحظة
// echarts.init فيرسم الرسم بحجمه الافتراضي الأصغر - نكرر resize() على أكثر من دورة رسم
// ومؤقّت احتياطي حتى يلتقط الحجم الصحيح مهما تأخر استقرار التخطيط
function tarhForceResize(chart) {
    requestAnimationFrame(function () {
        try { chart.resize(); } catch (e) {}
        requestAnimationFrame(function () {
            try { chart.resize(); } catch (e) {}
        });
    });
    setTimeout(function () { try { chart.resize(); } catch (e) {} }, 150);
}

/* ==========================================================================
   "حالة مدة الفرص" - قوس أخضر/أحمر قابل للضغط مباشرة (بلا زر "تتبع الفرصة" منفصل) +
   نسب + متوسط الأيام
   ========================================================================== */
function tarhRenderDurationStatus(d) {
    const row = document.getElementById("tarhChartsRow3");
    if (!row) return;

    const withinPct = d.totalCount > 0 ? d.withinPercent : 0;

    const div = document.createElement("div");
    div.className = "chart-card tarh-duration-card";

    if (d.totalCount === 0) {
        div.innerHTML =
            '<div class="tarh-duration-header">' +
                '<div class="tarh-duration-title"><i class="bi bi-stopwatch"></i> حالة مدة الفرص قيد الإجراء (2026)</div>' +
            '</div>' +
            '<div class="no-data-box" style="margin:0;"><span class="nd-icon"><i class="bi bi-inbox"></i></span>لا توجد فرص قيد الإجراء بسنة 2026 ضمن الفلاتر الحالية.</div>';
        row.appendChild(div);
        return;
    }

    const gaugeId = "tarhDurationGauge_" + Date.now();
    div.innerHTML =
        '<div class="tarh-duration-header">' +
            '<div class="tarh-duration-title"><i class="bi bi-stopwatch"></i> حالة مدة الفرص قيد الإجراء (2026)</div>' +
        '</div>' +
        '<div class="echart-box" id="' + gaugeId + '"></div>' +
        '<div class="tarh-duration-legend">' +
            '<div class="tarh-duration-legend-item"><span class="tarh-duration-dot within"></span> ضمن 40 يومًا — ' + tarhFormatNum(d.withinCount) + '</div>' +
            '<div class="tarh-duration-legend-item"><span class="tarh-duration-dot over"></span> تجاوزت 40 يومًا — ' + tarhFormatNum(d.overCount) + '</div>' +
        '</div>' +
        '<div class="tarh-duration-avg">متوسط الأيام منذ فتح المظاريف: <strong>' + d.averageDays.toFixed(1) + '</strong> يوم</div>';

    row.appendChild(div);

    const gaugeBox = document.getElementById(gaugeId);
    gaugeBox.style.height = "230px";
    const gauge = echarts.init(gaugeBox);
    gauge.setOption(tarhBuildDurationGaugeOption(withinPct, d.withinCount, d.overCount));
    tarhCharts.push(gauge);
    tarhForceResize(gauge);

    // الضغط على الجزء الأخضر من القوس يفتح "تتبع الفرصة" مفلترة على الفرص ضمن 40 يومًا فقط،
    // والجزء الأحمر يفتحها مفلترة على المتجاوزة فقط - بديل مباشر عن زر "تتبع الفرصة" العام
    gauge.on("click", function (params) {
        if (!params.data || params.data.name === "gap") return;
        tarhOpenTrackModal(params.data.name === "within" ? "within" : "over");
    });
}

/* ==========================================================================
   قوس "حالة مدة الفرص" - نسبة الفرص ضمن 40 يومًا (أخضر) مقابل المتجاوزة (أحمر).
   مبني كـ Donut (Pie) بدل Gauge حتى تكون كل قطعة لونية عنصر بيانات مستقل قابل
   للضغط بحدث click قياسي (الـ gauge الأصلي قطعة تلوين واحدة بلا نقاط بيانات
   منفصلة يمكن تمييزها بالضغط)؛ قطعة "gap" شفافة تصنع فتحة نصف الدائرة بالأسفل
   بنفس شكل القوس السابق تمامًا (فتحة 120° من إجمالي 360°)
   ========================================================================== */
function tarhBuildDurationGaugeOption(withinPct, withinCount, overCount) {
    var total = withinCount + overCount;
    return {
        tooltip: tarhTooltip({
            formatter: function (p) {
                if (p.data.name === "gap") return "";
                var label = p.data.name === "within" ? "ضمن 40 يومًا" : "تجاوزت 40 يومًا";
                return '<div style="font-weight:700;">' + label + '</div><div>' + tarhFormatNum(p.data.value) + ' فرصة - اضغطي لعرضها</div>';
            }
        }),
        series: [{
            type: "pie",
            startAngle: 210, clockwise: true,
            radius: ["80%", "92%"],
            center: ["50%", "62%"],
            avoidLabelOverlap: false,
            label: { show: false }, labelLine: { show: false },
            emphasis: { scaleSize: 6 },
            data: [
                { value: withinCount, name: "within", itemStyle: { color: TARH_C.dark } },
                { value: overCount, name: "over", itemStyle: { color: TARH_C.red } },
                { value: total * 0.5, name: "gap", itemStyle: { color: "transparent" }, label: { show: false }, tooltip: { show: false }, silent: true }
            ]
        }],
        graphic: [
            {
                type: "text", left: "center", top: "48%",
                style: { text: withinPct.toFixed(1) + "%", fontSize: 22, fontWeight: 800, fill: TARH_C.text, fontFamily: "Cairo", textAlign: "center" }
            },
            {
                type: "text", left: "center", top: "62%",
                style: { text: "ضمن 40 يومًا", fontSize: 10, fontWeight: 700, fill: TARH_C.muted, textAlign: "center" }
            }
        ]
    };
}

/* ==========================================================================
   نافذة "تتبع الفرصة": بحث عن فرصة ثم عرض رحلتها Timeline
   ========================================================================== */
let tarhOpportunityOptionsLoaded = false;
let tarhOpportunityOptionsCache = [];

async function tarhLoadOpportunityOptions() {
    if (tarhOpportunityOptionsLoaded || !window.TARH_OPPORTUNITY_OPTIONS_URL) return;
    try {
        const resp = await fetch(window.TARH_OPPORTUNITY_OPTIONS_URL);
        tarhOpportunityOptionsCache = await resp.json();
        tarhOpportunityOptionsLoaded = true;
    } catch (err) {
        console.error("تعذر تحميل قائمة الفرص:", err);
    }
}

function tarhOppDotClass(durationStatus) {
    if (durationStatus === "ضمن 40 يوم") return "within";
    if (durationStatus === "تجاوزت 40 يوم") return "over";
    return "unknown";
}

// null = كل الفرص، "within" = ضمن 40 يومًا فقط، "over" = المتجاوزة فقط - يُضبَط عند فتح
// النافذة (من زر "تتبع الفرصة" العام أو من الضغط على جزء لوني بقوس "حالة مدة الفرص")
let tarhModalStatusFilter = null;

// تُعرض القائمة دائمًا كاملة لكل الفرص المطابقة للون المختار (بلا بحث نصي) - تُبنى من جديد
// في كل مرة تُفتح فيها النافذة أو يُضغط "رجوع" حتى تبقى موثوقة مهما تكرّر الفتح والإغلاق
function tarhRenderOpportunityList() {
    const list = document.getElementById("tarhOpportunityList");
    if (!list) return;

    var matches = tarhOpportunityOptionsCache;
    if (tarhModalStatusFilter === "within") {
        matches = matches.filter(function (o) { return o.durationStatus === "ضمن 40 يوم"; });
    } else if (tarhModalStatusFilter === "over") {
        matches = matches.filter(function (o) { return o.durationStatus === "تجاوزت 40 يوم"; });
    }

    if (matches.length === 0) {
        list.innerHTML = '<div class="tarh-opp-empty">لا توجد فرص مطابقة</div>';
    } else {
        list.innerHTML = matches.map(function (o) {
            return '<div class="tarh-opp-item ' + tarhOppDotClass(o.durationStatus) + '" data-number="' + tarhEscape(o.opportunityNumber) + '">' +
                '<span class="tarh-opp-item-text"><span class="tarh-opp-item-number">' + tarhEscape(o.opportunityNumber) + '</span>' +
                    (o.opportunityName ? ' — ' + tarhEscape(o.opportunityName) : '') + '</span>' +
            '</div>';
        }).join("");
    }
    list.style.display = "";

    list.querySelectorAll(".tarh-opp-item").forEach(function (item) {
        item.addEventListener("click", function () {
            tarhSearchOpportunity(item.getAttribute("data-number"));
        });
    });
}

function tarhOpenTrackModal(statusFilter) {
    tarhModalStatusFilter = statusFilter || null;
    const overlay = document.getElementById("tarhTrackModalOverlay");
    if (!overlay) return;

    // إعادة ضبط كاملة في كل فتحة (بدل الاعتماد على أي حالة سابقة بالـ DOM) حتى تبقى القائمة
    // موثوقة دائمًا مهما تكرّر الفتح/الإغلاق أو التنقّل بين الألوان
    var result = document.getElementById("tarhTimelineResult");
    if (result) result.innerHTML = "";
    var list = document.getElementById("tarhOpportunityList");
    if (list) list.style.display = "";

    var titleEl = document.querySelector(".tarh-modal-title");
    if (titleEl) {
        var label = tarhModalStatusFilter === "within" ? "الفرص ضمن 40 يومًا"
            : tarhModalStatusFilter === "over" ? "الفرص المتجاوزة 40 يومًا"
            : "تتبع الفرصة";
        titleEl.innerHTML = '<i class="bi bi-signpost-split"></i> ' + tarhEscape(label);
    }

    overlay.style.display = "flex";
    tarhLoadOpportunityOptions().then(tarhRenderOpportunityList);
}

function tarhCloseTrackModal() {
    const overlay = document.getElementById("tarhTrackModalOverlay");
    if (overlay) overlay.style.display = "none";
}

function tarhFormatDate(iso) {
    if (!iso) return null;
    var d = new Date(iso);
    return isNaN(d.getTime()) ? null : d.toLocaleDateString("en-GB");
}

function tarhRenderTimelineStep(label, iso, isPending) {
    var dateStr = tarhFormatDate(iso);
    return '<div class="tarh-timeline-step' + (isPending ? ' pending' : '') + '">' +
        '<div class="dot"><i class="bi bi-check-lg"></i></div>' +
        '<div class="label">' + tarhEscape(label) + '</div>' +
        '<div class="date">' + (dateStr || 'لا يوجد تاريخ') + '</div>' +
    '</div>';
}

function tarhRenderTimelineConnector(days) {
    return '<div class="tarh-timeline-connector">' +
        (days !== null && days !== undefined ? '<div class="days">' + days + ' يوم</div>' : '') +
    '</div>';
}

var TARH_BACK_BTN_HTML =
    '<button type="button" class="tarh-timeline-back" id="tarhTimelineBack">' +
        '<i class="bi bi-arrow-right"></i><span>رجوع لقائمة الفرص</span>' +
    '</button>';

function tarhWireBackButton() {
    var backBtn = document.getElementById("tarhTimelineBack");
    if (backBtn) backBtn.addEventListener("click", function (e) {
        // نوقف انتشار الحدث حتى لا يصطدم بمستمع "إغلاق القائمة عند الضغط خارجها" على
        // مستوى document ويُغلق القائمة فورًا بعد ما فتحناها بنفس الضغطة
        e.stopPropagation();
        tarhBackToOpportunityList();
    });
}

function tarhBackToOpportunityList() {
    var result = document.getElementById("tarhTimelineResult");
    if (result) result.innerHTML = "";
    var list = document.getElementById("tarhOpportunityList");
    if (list) list.style.display = "";
    tarhRenderOpportunityList();
}

async function tarhSearchOpportunity(rawValue) {
    const result = document.getElementById("tarhTimelineResult");
    if (!result) return;

    // نحاول استخراج رقم الفرصة من القيمة المُختارة بصيغة "رقم — اسم"، وإلا نستخدم القيمة كما هي
    var opportunityNumber = rawValue.indexOf(" — ") > -1 ? rawValue.split(" — ")[0].trim() : rawValue.trim();
    if (!opportunityNumber) { result.innerHTML = ""; return; }

    // إخفاء القائمة فور اختيار فرصة - النتيجة (رحلة الفرصة) تحل محلها بنفس مساحة النافذة
    var list = document.getElementById("tarhOpportunityList");
    if (list) list.style.display = "none";

    result.innerHTML = '<div style="text-align:center;padding:20px;color:var(--muted);">جارِ البحث...</div>';

    try {
        const resp = await fetch(window.TARH_OPPORTUNITY_TIMELINE_URL + "?opportunityNumber=" + encodeURIComponent(opportunityNumber));
        const t = await resp.json();

        if (!t.found) {
            result.innerHTML = TARH_BACK_BTN_HTML +
                '<div class="no-data-box" style="margin-top:14px;"><span class="nd-icon"><i class="bi bi-inbox"></i></span>لم يتم العثور على فرصة بهذا الرقم أو الاسم لها رقم عقد فعلي.</div>';
            tarhWireBackButton();
            return;
        }

        var isWithinNum = t.durationStatus === "ضمن 40 يوم";
        var numColor = t.durationStatus ? (isWithinNum ? TARH_C.limeDk : TARH_C.red) : "inherit";
        var html = TARH_BACK_BTN_HTML;
        html += '<div style="margin-top:16px;">' +
            '<div style="font-weight:800;color:var(--brand-dark);">' + tarhEscape(t.opportunityName || t.opportunityNumber) + '</div>' +
            '<div style="font-size:.82rem;color:var(--muted);margin-top:2px;">رقم الفرصة: ' +
                '<strong style="color:' + numColor + ';">' + tarhEscape(t.opportunityNumber) + '</strong>' +
                ' • رقم العقد: ' + tarhEscape(t.contractNumber) + ' • الحالة: ' + tarhEscape(t.status || "—") + '</div>' +
        '</div>';

        html += '<div class="tarh-timeline">' +
            tarhRenderTimelineStep("فتح المظاريف", t.envelopeOpeningDate, !t.envelopeOpeningDate) +
            tarhRenderTimelineConnector(t.envelopeToAwardDays) +
            tarhRenderTimelineStep("الترسية", t.awardDate, !t.awardDate) +
        '</div>';

        if (t.totalDays !== null && t.totalDays !== undefined) {
            var isWithin = t.durationStatus === "ضمن 40 يوم";
            html += '<div class="tarh-timeline-summary">' +
                '<div>إجمالي مدة الفرصة</div>' +
                '<div class="total">' + t.totalDays + ' يوم</div>' +
                '<div class="tarh-timeline-badge ' + (isWithin ? 'within' : 'over') + '">' +
                    (isWithin ? '🟢 ' : '🔴 ') + tarhEscape(t.durationStatus) +
                '</div>' +
            '</div>';
        } else {
            html += '<div class="no-data-box" style="margin-top:14px;">لا يمكن حساب إجمالي المدة - يلزم توفّر تاريخَي فتح المظاريف وتوقيع العقد معًا.</div>';
        }

        result.innerHTML = html;
        tarhWireBackButton();
    } catch (err) {
        console.error("تعذر تحميل رحلة الفرصة:", err);
        result.innerHTML = TARH_BACK_BTN_HTML +
            '<div class="no-data-box" style="margin-top:14px;">تعذّر تحميل بيانات الفرصة.</div>';
        tarhWireBackButton();
    }
}

function tarhInitTrackModal() {
    var closeBtn = document.getElementById("tarhTrackModalClose");
    var overlay = document.getElementById("tarhTrackModalOverlay");

    if (closeBtn) closeBtn.addEventListener("click", tarhCloseTrackModal);
    if (overlay) overlay.addEventListener("click", function (e) { if (e.target === overlay) tarhCloseTrackModal(); });
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

        tarhBuildFilterBar(data);

        if (!data.hasData) {
            if (contentEl) contentEl.style.display = "none";
            if (noDataEl) noDataEl.style.display = "";
            return;
        }

        if (contentEl) contentEl.style.display = "";
        if (noDataEl) noDataEl.style.display = "none";

        tarhRenderKpiCards(data.typeSummary, data.totalOpportunities);

        // رسم 1: قيمة وعدد العقود المبرمة حسب الإدارة العامة - قابل للضغط لتصفية رسم 2
        var deptItems = tarhComboItemsFromDept(data.deptSummary);
        var deptSortedByValue = deptItems.slice().sort(function (a, b) { return b.totalValue - a.totalValue; });
        var selectedDept = deptSortedByValue.length ? deptSortedByValue[0].label : null;

        var deptChart = tarhRenderChart(
            "tarhChartsRow1", "tarhDeptChart",
            "قيمة وعدد العقود المبرمة حسب الإدارة العامة",
            "اضغطي إدارة",
            tarhBuildComboBarLineOption(deptItems, selectedDept),
            "tarh-combo-card",
            400
        );

        var subDeptChart = tarhRenderChart(
            "tarhChartsRow1", "tarhSubDeptChart",
            "قيمة وعدد العقود المبرمة حسب الإدارة الفرعية",
            selectedDept || "—",
            tarhBuildComboBarLineOption(tarhComboItemsFromSubDept(data.finalizedByPortfolio, selectedDept), null),
            "tarh-combo-card",
            400
        );

        if (deptChart) {
            deptChart.on("click", function (params) {
                if (params.componentType !== "series" || !params.name || params.name === selectedDept) return;
                selectedDept = params.name;
                deptChart.setOption(tarhBuildComboBarLineOption(deptItems, selectedDept), true);
                if (subDeptChart) {
                    subDeptChart.setOption(tarhBuildComboBarLineOption(tarhComboItemsFromSubDept(data.finalizedByPortfolio, selectedDept), null), true);
                    var badgeEl = subDeptChart._tarhCard && subDeptChart._tarhCard.querySelector(".chart-badge");
                    if (badgeEl) badgeEl.textContent = selectedDept;
                    tarhForceResize(subDeptChart);
                }
                tarhForceResize(deptChart);
            });
        }

        // رسم 2 (الصف الثاني): عدد الفرص حسب السنة + تفاصيل حالات فتح المظاريف للسنة المختارة
        tarhRenderYearStatusChart("tarhChartsRow2", data.envelopeByYearStatus, tarhCurrentFilters.year);

        // رسم 3 (الصف الثالث): نسبة الطرح ونسبة الإبرام حسب السنة، وجنبها حالة مدة الفرص
        // قيد الإجراء (قوس قابل للضغط) بنفس الصف
        tarhRenderChart(
            "tarhChartsRow3", "tarhYearlyRateChart",
            "نسبة الطرح ونسبة الإبرام حسب السنة",
            "تراكمي",
            tarhBuildYearlyRateOption(data.yearlyRates),
            "tarh-rate-card tarh-line-card",
            280
        );
        tarhRenderDurationStatus(data.durationStatus);
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

/* ==========================================================================
   تصدير Excel / طباعة PDF - بنفس الفلاتر الحالية بالضبط
   ========================================================================== */
function tarhExportQueryString() {
    return "?department=" + encodeURIComponent(tarhCurrentFilters.department) +
        "&status=" + encodeURIComponent(tarhCurrentFilters.status) +
        "&contractNumber=" + encodeURIComponent(tarhCurrentFilters.contractNumber) +
        "&year=" + encodeURIComponent(tarhCurrentFilters.year) +
        "&quarter=" + encodeURIComponent(tarhCurrentFilters.quarter);
}

function tarhInitExport() {
    const excelBtn = document.getElementById("tarhExportExcelBtn");
    const pdfBtn = document.getElementById("tarhExportPdfBtn");

    if (excelBtn && window.TARH_EXPORT_EXCEL_URL) {
        excelBtn.addEventListener("click", function () {
            window.location.href = window.TARH_EXPORT_EXCEL_URL + tarhExportQueryString();
        });
    }

    if (pdfBtn && window.TARH_EXPORT_DATA_URL) {
        pdfBtn.addEventListener("click", async function () {
            pdfBtn.disabled = true;
            try {
                const resp = await fetch(window.TARH_EXPORT_DATA_URL + tarhExportQueryString());
                const rows = await resp.json();

                const tbody = document.getElementById("tarhExportTableBody");
                if (tbody) {
                    tbody.innerHTML = rows.map(function (r) {
                        var dateStr = "";
                        if (r.contractDate) {
                            var d = new Date(r.contractDate);
                            if (!isNaN(d.getTime())) dateStr = d.toLocaleDateString("en-GB");
                        }
                        return '<tr>' +
                            '<td>' + tarhEscape(r.opportunityNumber || "") + '</td>' +
                            '<td>' + tarhEscape(r.contractNumber || "") + '</td>' +
                            '<td>' + tarhEscape(r.opportunityName || "") + '</td>' +
                            '<td>' + tarhFormatNum(r.value) + '</td>' +
                            '<td>' + tarhEscape(r.generalDepartment || "") + '</td>' +
                            '<td>' + tarhEscape(r.subPortfolio || "") + '</td>' +
                            '<td>' + (r.contractYear || "") + '</td>' +
                            '<td>' + dateStr + '</td>' +
                        '</tr>';
                    }).join("");
                }

                var dateEl = document.getElementById("tarhPrintDate");
                if (dateEl) dateEl.textContent = new Date().toLocaleDateString("ar-SA");

                window.print();
            } catch (err) {
                console.error("تعذّر تجهيز بيانات الطباعة:", err);
            } finally {
                pdfBtn.disabled = false;
            }
        });
    }
}

document.addEventListener("DOMContentLoaded", function () {
    if (!window.TARH_DATA_URL) return;

    tarhLoad();
    tarhInitUpload();
    tarhInitExport();
    tarhInitTrackModal();

    window.addEventListener("resize", function () {
        tarhCharts.forEach(function (c) { try { c.resize(); } catch (e) {} });
    });
});
