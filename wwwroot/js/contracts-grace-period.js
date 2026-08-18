/* ==========================================================================
   صفحة "العقود التي تنتهي فترة سماحها خلال 3 أشهر" - كروت عدد فقط (بلا قيمة مالية - الملف لا
   يحتوي عمود قيمة إطلاقًا)، توزيع العقود حسب النوع، رسمان مكدَّسان (إدارات عامة ← فرعية، حسب
   عمود "حالة العقد" الخماسي القيم، قابلا للضغط للترابط)، ومؤشر أخضر/أحمر للعقود اللي نهاية فترة
   تجهيزها بسنة 2026 بالضبط (نفس تصميم "الفواتير المتجاوزة 60 يوم" بصفحة المديونيات).
   ========================================================================== */
const CGP_C = {
    dark: "#0E6E61", mid: "#3F8F87", light: "#5FA89D", lime: "#8DC63F", limeDk: "#5F9E1F",
    red: "#D6534A", text: "#173B36", muted: "#6E8A86", grid: "#E1ECEA", axisText: "#181818"
};
const CGP_AXIS_LABEL_STYLE = { color: CGP_C.axisText, fontSize: 13, fontWeight: 600 };
const CGP_PALETTE = [CGP_C.dark, CGP_C.mid, CGP_C.lime, CGP_C.light, CGP_C.limeDk];

function cgpAxisStyle(extra) {
    extra = extra || {};
    const merged = Object.assign({
        axisLine: { lineStyle: { color: CGP_C.grid } },
        splitLine: { lineStyle: { color: CGP_C.grid, type: "dashed" } }
    }, extra);
    merged.axisLabel = Object.assign({}, CGP_AXIS_LABEL_STYLE, extra.axisLabel || {});
    return merged;
}
function cgpTooltip(extra) {
    return Object.assign({
        backgroundColor: "#FFFFFF", borderColor: CGP_C.grid, borderWidth: 1,
        textStyle: { color: CGP_C.text, fontSize: 12 },
        extraCssText: "box-shadow:0 4px 16px rgba(0,0,0,0.1);border-radius:10px;"
    }, extra || {});
}
function cgpFormatNum(n) { return Number.isFinite(n) ? Math.round(n).toLocaleString("en-US") : "0"; }
function cgpEscape(str) {
    const div = document.createElement("div");
    div.textContent = (str === null || str === undefined) ? "" : String(str);
    return div.innerHTML;
}

let cgpCharts = [];
function cgpDisposeCharts() {
    cgpCharts.forEach(function (c) { try { c.dispose(); } catch (e) {} });
    cgpCharts = [];
}

/* ==========================================================================
   شريط الفلاتر: الإدارة العامة / الإدارة الفرعية / نوع العقد / حالة العقد
   ========================================================================== */
function cgpMakeSelectWrap(id, iconClass, allLabel, options, selected) {
    const wrap = document.createElement("div");
    wrap.className = "filter-select-wrap";
    wrap.innerHTML = '<i class="bi ' + iconClass + ' filter-icon"></i>';
    const select = document.createElement("select");
    select.id = id;
    select.className = "filter-select";
    let html = '<option value="">' + cgpEscape(allLabel) + '</option>';
    (options || []).forEach(function (o) {
        html += '<option value="' + cgpEscape(o) + '"' + (o === selected ? " selected" : "") + '>' + cgpEscape(o) + '</option>';
    });
    select.innerHTML = html;
    wrap.appendChild(select);
    wrap.insertAdjacentHTML("beforeend", '<i class="bi bi-chevron-down filter-chevron"></i>');
    return { wrap: wrap, select: select };
}

const cgpFilters = { department: "", portfolio: "", contractType: "", contractStatus: "" };

function cgpBuildFilterBar(data) {
    const bar = document.getElementById("cgpFilterBar");
    if (!bar) return;
    bar.innerHTML = "";

    const dept = cgpMakeSelectWrap("cgpDeptFilter", "bi-diagram-3", "كل الإدارات العامة", data.departmentOptions || [], cgpFilters.department);
    const portfolio = cgpMakeSelectWrap("cgpPortfolioFilter", "bi-diagram-2", "كل الإدارات الفرعية", data.portfolioOptions || [], cgpFilters.portfolio);
    const type = cgpMakeSelectWrap("cgpTypeFilter", "bi-tag", "كل أنواع العقود", data.contractTypeOptions || [], cgpFilters.contractType);
    const status = cgpMakeSelectWrap("cgpStatusFilter", "bi-flag", "كل حالات العقد", data.contractStatusOptions || [], cgpFilters.contractStatus);

    bar.appendChild(dept.wrap);
    bar.appendChild(portfolio.wrap);
    bar.appendChild(type.wrap);
    bar.appendChild(status.wrap);

    dept.select.addEventListener("change", function () { cgpFilters.department = dept.select.value; cgpLoad(); });
    portfolio.select.addEventListener("change", function () { cgpFilters.portfolio = portfolio.select.value; cgpLoad(); });
    type.select.addEventListener("change", function () { cgpFilters.contractType = type.select.value; cgpLoad(); });
    status.select.addEventListener("change", function () { cgpFilters.contractStatus = status.select.value; cgpLoad(); });
}

function cgpQueryString() {
    const p = new URLSearchParams();
    if (cgpFilters.department) p.set("department", cgpFilters.department);
    if (cgpFilters.portfolio) p.set("portfolio", cgpFilters.portfolio);
    if (cgpFilters.contractType) p.set("contractType", cgpFilters.contractType);
    if (cgpFilters.contractStatus) p.set("contractStatus", cgpFilters.contractStatus);
    return p.toString();
}

/* ==========================================================================
   كروت الـKPI الثلاثة (عدد فقط - بلا قيمة مالية، الملف لا يحتوي عمود قيمة)
   ========================================================================== */
function cgpKpiCardHtml(icon, label, count, colorClass) {
    return '<div class="kpi-card ' + colorClass + '">' +
        '<div class="icon"><i class="bi ' + icon + '"></i></div>' +
        '<div class="label">' + cgpEscape(label) + '</div>' +
        '<div class="value">' + cgpFormatNum(count) + '</div>' +
        '<div style="font-size:.78rem;font-weight:700;color:var(--muted);margin-top:4px;">عقد</div>' +
    '</div>';
}

function cgpRenderKpiCards(data) {
    const grid = document.getElementById("cgpKpiGrid");
    if (!grid) return;
    grid.innerHTML =
        cgpKpiCardHtml("bi-file-earmark-text", "إجمالي العقود", data.totalCount, "c1") +
        cgpKpiCardHtml("bi-graph-up-arrow", "عقود الاستثمار", data.investmentCount, "c2") +
        cgpKpiCardHtml("bi-building", "عقود تأجير مباشر", data.directLeaseCount, "c4");
}

/* ==========================================================================
   الرسوم
   ========================================================================== */
function cgpRenderChart(rowId, containerId, title, badge, option, heightPx) {
    const row = document.getElementById(rowId);
    if (!row) return null;
    const id = containerId + "_" + Date.now() + "_" + Math.floor(Math.random() * 1000);
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">' + cgpEscape(title) + '</div>' +
            (badge ? '<div class="chart-badge">' + cgpEscape(badge) + '</div>' : '') +
        '</div>' +
        '<div class="echart-box" id="' + id + '" style="height:' + (heightPx || 300) + 'px;"></div>';
    row.appendChild(div);
    const chart = echarts.init(document.getElementById(id));
    chart.setOption(option);
    chart._cgpCard = div;
    cgpCharts.push(chart);
    requestAnimationFrame(function () { try { chart.resize(); } catch (e) {} });
    return chart;
}

// عمود مكدَّس بعدد العقود حسب حالة العقد (خارج/داخل فترة التجهيز/متعثر...) - عدد السلاسل ديناميكي
// حسب الحالات الحقيقية الموجودة بالبيانات (statusOptions)، بلا اختراع/دمج تصنيفات
function cgpBuildStatusStackedOption(labels, statusOptions, seriesData) {
    const series = statusOptions.map(function (status, i) {
        return {
            name: status, type: "bar", stack: "status", data: seriesData[i], barMaxWidth: 40, cursor: "pointer",
            itemStyle: { color: CGP_PALETTE[i % CGP_PALETTE.length] },
            label: { show: true, color: "#fff", fontWeight: 700, fontSize: 9, formatter: function (p) { return p.value > 0 ? cgpFormatNum(p.value) : ""; } }
        };
    });
    return {
        tooltip: cgpTooltip({ trigger: "axis", axisPointer: { type: "shadow" } }),
        legend: { top: 0, right: 8, textStyle: Object.assign({}, CGP_AXIS_LABEL_STYLE, { fontSize: 11 }), itemWidth: 12, itemHeight: 10, type: "scroll" },
        grid: { left: 10, right: 16, top: 56, bottom: 60, containLabel: true },
        xAxis: cgpAxisStyle({ type: "category", data: labels, axisLabel: { interval: 0, rotate: labels.length > 3 ? 20 : 0, fontSize: 11, width: 110, overflow: "break" } }),
        yAxis: cgpAxisStyle({ type: "value", min: 0 }),
        series: series
    };
}

// يحوّل قائمة مسطَّحة {groupLabel, status, count} إلى مصفوفة سلاسل [statusIndex][labelIndex]
function cgpPivotDept(deptBreakdown, labels, statusOptions) {
    return statusOptions.map(function (status) {
        return labels.map(function (label) {
            const row = (deptBreakdown || []).find(function (r) { return r.groupLabel === label && r.status === status; });
            return row ? row.count : 0;
        });
    });
}

// يستخرج ويُرتّب صفوف الإدارات الفرعية الخاصة بإدارة عامة واحدة من subDeptStatusBreakdown
function cgpPortfoliosForDept(subDeptBreakdown, deptLabel, statusOptions) {
    const rows = (subDeptBreakdown || []).filter(function (r) { return r.departmentLabel === deptLabel; });
    const totals = {};
    rows.forEach(function (r) { totals[r.portfolioLabel] = (totals[r.portfolioLabel] || 0) + r.count; });
    const labels = Object.keys(totals).sort(function (a, b) { return totals[b] - totals[a]; });
    const seriesData = statusOptions.map(function (status) {
        return labels.map(function (label) {
            const row = rows.find(function (r) { return r.portfolioLabel === label && r.status === status; });
            return row ? row.count : 0;
        });
    });
    return { labels: labels, seriesData: seriesData };
}

let cgpSelectedDept = null;

function cgpRenderCharts(data) {
    const deptLabels = data.deptLabels || [];
    const statusOptions = data.statusOptions || [];
    cgpSelectedDept = deptLabels.length ? deptLabels[0] : null;

    const deptSeriesData = cgpPivotDept(data.deptStatusBreakdown || [], deptLabels, statusOptions);
    const deptChart = cgpRenderChart("cgpChartsRow2", "cgpDeptChart", "العقود حسب الإدارة العامة", "اضغط إدارة",
        cgpBuildStatusStackedOption(deptLabels, statusOptions, deptSeriesData), 360);

    const initialSub = cgpPortfoliosForDept(data.subDeptStatusBreakdown || [], cgpSelectedDept, statusOptions);
    const subChart = cgpRenderChart("cgpChartsRow2", "cgpSubDeptChart", "العقود حسب الإدارة الفرعية", cgpSelectedDept || "—",
        cgpBuildStatusStackedOption(initialSub.labels, statusOptions, initialSub.seriesData), 360);

    function selectDept(name) {
        if (!name || name === cgpSelectedDept) return;
        cgpSelectedDept = name;
        const sub = cgpPortfoliosForDept(data.subDeptStatusBreakdown || [], cgpSelectedDept, statusOptions);
        if (subChart) {
            subChart.setOption(cgpBuildStatusStackedOption(sub.labels, statusOptions, sub.seriesData), true);
            const badge = subChart._cgpCard && subChart._cgpCard.querySelector(".chart-badge");
            if (badge) badge.textContent = cgpSelectedDept;
            requestAnimationFrame(function () { try { subChart.resize(); } catch (e) {} });
        }
    }

    if (deptChart) {
        deptChart.on("click", function (params) {
            if (params.componentType !== "series" || !params.name) return;
            selectDept(params.name);
        });
    }

    cgpRenderExpiringArc(data);
    cgpRenderContractEndingArc(data);
    cgpRenderDeptQuarterChart(data);
}

/* ==========================================================================
   "العقود اللي شارفت على الانتهاء" - نفس تصميم "الفواتير المتجاوزة 60 يوم" بصفحة المديونيات
   (وهو بدوره نسخة من "تحت إجراءات الترسية" بصفحة الطرح) - أحمر=نهاية فترة التجهيز بسنة 2026
   بالضبط، أخضر=الباقي. بلا درج تفاصيل قابل للضغط (لم يُطلَب لهذه الصفحة)
   ========================================================================== */
function cgpBuildExpiringArcOption(greenPct, greenCount, redCount) {
    const total = greenCount + redCount;
    return {
        tooltip: cgpTooltip({
            formatter: function (p) {
                if (p.data.name === "gap") return "";
                const label = p.data.name === "green" ? "غير عاجلة" : "تنتهي فترة التجهيز 2026";
                return '<div style="font-weight:700;">' + label + '</div><div>' + cgpFormatNum(p.data.value) + ' عقد</div>';
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
                { value: greenCount, name: "green", itemStyle: { color: CGP_C.lime } },
                { value: redCount, name: "red", itemStyle: { color: CGP_C.red } },
                { value: total * 0.5, name: "gap", itemStyle: { color: "transparent" }, label: { show: false }, tooltip: { show: false }, silent: true }
            ]
        }],
        graphic: [
            {
                type: "text", left: "center", top: "48%",
                style: { text: greenPct.toFixed(1) + "%", fontSize: 22, fontWeight: 800, fill: CGP_C.text, fontFamily: "Cairo", textAlign: "center" }
            },
            {
                type: "text", left: "center", top: "62%",
                style: { text: "غير عاجلة", fontSize: 10, fontWeight: 700, fill: CGP_C.muted, textAlign: "center" }
            }
        ]
    };
}

// قائمة العقود المفلترة حاليًا لقوس "العقود التي تنتهي فترة سماحها خلال 2026" - نفس أسلوب
// قوس "العقود التي شارفت على الانتهاء" أدناه بالضبط (قائمة قابلة للضغط ← تفاصيل عقد واحد)
let cgpPrepEndingListCache = [];

async function cgpShowPrepEndingList(isRed) {
    var placeholder = document.getElementById("cgpPrepEndingTrackPlaceholder");
    var listWrap = document.getElementById("cgpPrepEndingTrackList");
    var titleEl = document.getElementById("cgpPrepEndingTrackTitle");
    if (!listWrap) return;

    if (titleEl) {
        titleEl.innerHTML = '<i class="bi bi-signpost-split"></i> ' + (isRed ? "العقود التي تنتهي فترة تجهيزها 2026" : "العقود غير العاجلة");
    }
    if (placeholder) placeholder.style.display = "none";
    listWrap.style.display = "";
    listWrap.innerHTML = '<div class="no-data-box" style="margin:0;">جارِ التحميل...</div>';

    try {
        var params = new URLSearchParams(cgpQueryString());
        params.set("isRed", isRed ? "true" : "false");
        var resp = await fetch(window.CGP_PREP_ENDING_URL + "?" + params.toString());
        cgpPrepEndingListCache = await resp.json();
        cgpRenderPrepEndingList();
    } catch (err) {
        console.error("تعذّر تحميل قائمة العقود:", err);
        listWrap.innerHTML = '<div class="no-data-box" style="margin:0;">تعذّر تحميل القائمة</div>';
    }
}

function cgpRenderPrepEndingList() {
    var listWrap = document.getElementById("cgpPrepEndingTrackList");
    if (!listWrap) return;

    if (!cgpPrepEndingListCache || !cgpPrepEndingListCache.length) {
        listWrap.innerHTML = '<div class="no-data-box" style="margin:0;">لا توجد عقود مطابقة</div>';
        return;
    }

    listWrap.innerHTML = '<div class="tarh-opp-list" style="max-height:320px;">' +
        cgpPrepEndingListCache.map(function (r, i) {
            return '<div class="tarh-opp-item" data-idx="' + i + '">' +
                '<span class="tarh-opp-item-text"><span class="tarh-opp-item-number">' + cgpEscape(r.contractNumber || "—") + '</span>' +
                    (r.opportunityName ? ' — ' + cgpEscape(r.opportunityName) : '') + '</span>' +
            '</div>';
        }).join("") +
    '</div>';

    listWrap.querySelectorAll(".tarh-opp-item").forEach(function (item) {
        item.addEventListener("click", function () {
            cgpShowPrepEndingDetail(parseInt(item.getAttribute("data-idx"), 10));
        });
    });
}

function cgpShowPrepEndingDetail(idx) {
    var listWrap = document.getElementById("cgpPrepEndingTrackList");
    if (!listWrap) return;
    var r = cgpPrepEndingListCache[idx];
    if (!r) return;

    var html = '<button type="button" class="tarh-timeline-back" id="cgpPrepEndingBackBtn"><i class="bi bi-arrow-right"></i><span>رجوع للقائمة</span></button>';
    html += '<div style="margin-top:16px;">' +
        '<div style="font-weight:800;color:var(--brand-dark);">' + cgpEscape(r.opportunityName || r.contractNumber || "—") + '</div>' +
        '<div style="font-size:.82rem;color:var(--muted);margin-top:2px;">رقم العقد: <strong>' + cgpEscape(r.contractNumber || "—") + '</strong></div>' +
    '</div>';
    html += '<div style="margin-top:14px;display:flex;flex-direction:column;gap:12px;">' +
        '<div><div style="color:var(--muted);font-size:.78rem;font-weight:700;">اسم الفرصة</div><div style="font-weight:700;margin-top:2px;">' + cgpEscape(r.opportunityName || "—") + '</div></div>' +
        '<div><div style="color:var(--muted);font-size:.78rem;font-weight:700;">تاريخ نهاية فترة التجهيز</div><div style="font-weight:700;margin-top:2px;">' + cgpFormatDate(r.contractEndDate) + '</div></div>' +
        '<div><div style="color:var(--muted);font-size:.78rem;font-weight:700;">تاريخ التوقيع</div><div style="font-weight:700;margin-top:2px;">' + cgpFormatDate(r.signingDate) + '</div></div>' +
    '</div>';
    listWrap.innerHTML = html;

    var backBtn = document.getElementById("cgpPrepEndingBackBtn");
    if (backBtn) backBtn.addEventListener("click", function (e) {
        e.stopPropagation();
        cgpRenderPrepEndingList();
    });
}

function cgpRenderExpiringArc(data) {
    const row = document.getElementById("cgpChartsRow3");
    if (!row) return;

    const totalWithDate = (data.prepEndingGreenCount || 0) + (data.prepEndingRedCount || 0);
    const div = document.createElement("div");
    div.className = "chart-card tarh-duration-card";

    if (totalWithDate === 0) {
        div.innerHTML =
            '<div class="tarh-duration-header"><div class="tarh-duration-title"><i class="bi bi-hourglass-split"></i> العقود التي تنتهي فترة سماحها خلال 2026</div></div>' +
            '<div class="no-data-box" style="margin:0;"><span class="nd-icon"><i class="bi bi-inbox"></i></span>لا توجد عقود بعمود "نهاية فترة التجهيز" ضمن الفلاتر الحالية.</div>';
        row.appendChild(div);
        return;
    }

    const greenPct = (data.prepEndingGreenCount / totalWithDate) * 100;
    const gaugeId = "cgpExpiringGauge_" + Date.now();
    div.innerHTML =
        '<div class="tarh-duration-header"><div class="tarh-duration-title"><i class="bi bi-hourglass-split"></i> العقود التي تنتهي فترة سماحها خلال 2026</div></div>' +
        '<div class="echart-box" id="' + gaugeId + '"></div>' +
        '<div class="tarh-duration-legend">' +
            '<div class="tarh-duration-legend-item"><span class="tarh-duration-dot within"></span> غير عاجلة — ' + cgpFormatNum(data.prepEndingGreenCount) + '</div>' +
            '<div class="tarh-duration-legend-item"><span class="tarh-duration-dot over"></span> تنتهي فترة التجهيز 2026 — ' + cgpFormatNum(data.prepEndingRedCount) + '</div>' +
        '</div>';
    row.appendChild(div);

    const trackDiv = document.createElement("div");
    trackDiv.className = "chart-card tarh-track-card";
    trackDiv.innerHTML =
        '<div class="tarh-duration-header"><div class="tarh-duration-title" id="cgpPrepEndingTrackTitle"><i class="bi bi-signpost-split"></i> تفاصيل العقود</div></div>' +
        '<div class="tarh-track-body">' +
            '<div class="no-data-box" style="margin:0;" id="cgpPrepEndingTrackPlaceholder"><span class="nd-icon"><i class="bi bi-hand-index-thumb"></i></span>اضغط على القوس (أخضر/أحمر) لعرض العقود هنا</div>' +
            '<div id="cgpPrepEndingTrackList" style="display:none;"></div>' +
        '</div>';
    row.appendChild(trackDiv);

    const gaugeBox = document.getElementById(gaugeId);
    gaugeBox.style.height = "230px";
    const gauge = echarts.init(gaugeBox);
    gauge.setOption(cgpBuildExpiringArcOption(greenPct, data.prepEndingGreenCount, data.prepEndingRedCount));
    cgpCharts.push(gauge);
    requestAnimationFrame(function () { try { gauge.resize(); } catch (e) {} });

    gauge.on("click", function (params) {
        if (!params.data || params.data.name === "gap") return;
        cgpShowPrepEndingList(params.data.name === "red");
    });
}

/* ==========================================================================
   "العقود التي شارفت على الانتهاء" - نفس فكرة القوس أعلاه لكن على عمود "تاريخ نهاية العقد"
   بدلاً من "نهاية فترة التجهيز": أحمر=ينتهي بسنة 2026 بالضبط، أخضر=بعد 2026 - قابل للضغط
   (نفس أسلوب "حالة مدة الفرص" بصفحة الطرح) لعرض قائمة العقود بالتفصيل بجانبه
   ========================================================================== */
function cgpBuildContractEndingArcOption(redPct, redCount, greenCount) {
    var total = redCount + greenCount;
    return {
        tooltip: cgpTooltip({
            formatter: function (p) {
                if (p.data.name === "gap") return "";
                var label = p.data.name === "red" ? "تنتهي 2026" : "تنتهي بعد 2026";
                return '<div style="font-weight:700;">' + label + '</div><div>' + cgpFormatNum(p.data.value) + ' عقد - اضغط لعرضها</div>';
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
                { value: redCount, name: "red", itemStyle: { color: CGP_C.red } },
                { value: greenCount, name: "green", itemStyle: { color: CGP_C.lime } },
                { value: total * 0.5, name: "gap", itemStyle: { color: "transparent" }, label: { show: false }, tooltip: { show: false }, silent: true }
            ]
        }],
        graphic: [
            {
                type: "text", left: "center", top: "48%",
                style: { text: redPct.toFixed(1) + "%", fontSize: 22, fontWeight: 800, fill: CGP_C.text, fontFamily: "Cairo", textAlign: "center" }
            },
            {
                type: "text", left: "center", top: "62%",
                style: { text: "تنتهي 2026", fontSize: 10, fontWeight: 700, fill: CGP_C.muted, textAlign: "center" }
            }
        ]
    };
}

function cgpFormatDate(iso) {
    if (!iso) return "لا يوجد تاريخ";
    var d = new Date(iso);
    return isNaN(d.getTime()) ? "لا يوجد تاريخ" : d.toLocaleDateString("en-GB");
}

// قائمة العقود المفلترة حاليًا (مخزَّنة مؤقتًا بعد الجلب) - نستخدمها محليًا عند الضغط على عقد
// من القائمة لعرض تفاصيله بلا طلب شبكة إضافي، ولإعادة بناء القائمة عند الضغط على "رجوع"
let cgpEndingListCache = [];

// نفس تصميم "تتبع الفرصة" بصفحة الطرح بالضبط: قائمة عقود قابلة للضغط ← عند اختيار عقد تظهر
// تفاصيله (رقم العقد، اسم الفرصة، تاريخ نهاية العقد، تاريخ العقد) بدل القائمة، مع زر "رجوع"
async function cgpShowContractEndingList(isRed) {
    var placeholder = document.getElementById("cgpEndingTrackPlaceholder");
    var listWrap = document.getElementById("cgpEndingTrackList");
    var titleEl = document.getElementById("cgpEndingTrackTitle");
    if (!listWrap) return;

    if (titleEl) {
        titleEl.innerHTML = '<i class="bi bi-signpost-split"></i> ' + (isRed ? "العقود التي تنتهي 2026" : "العقود التي تنتهي بعد 2026");
    }
    if (placeholder) placeholder.style.display = "none";
    listWrap.style.display = "";
    listWrap.innerHTML = '<div class="no-data-box" style="margin:0;">جارِ التحميل...</div>';

    try {
        var params = new URLSearchParams(cgpQueryString());
        params.set("isRed", isRed ? "true" : "false");
        var resp = await fetch(window.CGP_CONTRACT_ENDING_URL + "?" + params.toString());
        cgpEndingListCache = await resp.json();
        cgpRenderContractEndingList();
    } catch (err) {
        console.error("تعذّر تحميل قائمة العقود:", err);
        listWrap.innerHTML = '<div class="no-data-box" style="margin:0;">تعذّر تحميل القائمة</div>';
    }
}

function cgpRenderContractEndingList() {
    var listWrap = document.getElementById("cgpEndingTrackList");
    if (!listWrap) return;

    if (!cgpEndingListCache || !cgpEndingListCache.length) {
        listWrap.innerHTML = '<div class="no-data-box" style="margin:0;">لا توجد عقود مطابقة</div>';
        return;
    }

    listWrap.innerHTML = '<div class="tarh-opp-list" style="max-height:320px;">' +
        cgpEndingListCache.map(function (r, i) {
            return '<div class="tarh-opp-item" data-idx="' + i + '">' +
                '<span class="tarh-opp-item-text"><span class="tarh-opp-item-number">' + cgpEscape(r.contractNumber || "—") + '</span>' +
                    (r.opportunityName ? ' — ' + cgpEscape(r.opportunityName) : '') + '</span>' +
            '</div>';
        }).join("") +
    '</div>';

    listWrap.querySelectorAll(".tarh-opp-item").forEach(function (item) {
        item.addEventListener("click", function () {
            cgpShowContractEndingDetail(parseInt(item.getAttribute("data-idx"), 10));
        });
    });
}

function cgpShowContractEndingDetail(idx) {
    var listWrap = document.getElementById("cgpEndingTrackList");
    if (!listWrap) return;
    var r = cgpEndingListCache[idx];
    if (!r) return;

    var html = '<button type="button" class="tarh-timeline-back" id="cgpEndingBackBtn"><i class="bi bi-arrow-right"></i><span>رجوع للقائمة</span></button>';
    html += '<div style="margin-top:16px;">' +
        '<div style="font-weight:800;color:var(--brand-dark);">' + cgpEscape(r.opportunityName || r.contractNumber || "—") + '</div>' +
        '<div style="font-size:.82rem;color:var(--muted);margin-top:2px;">رقم العقد: <strong>' + cgpEscape(r.contractNumber || "—") + '</strong></div>' +
    '</div>';
    html += '<div style="margin-top:14px;display:flex;flex-direction:column;gap:12px;">' +
        '<div><div style="color:var(--muted);font-size:.78rem;font-weight:700;">اسم الفرصة</div><div style="font-weight:700;margin-top:2px;">' + cgpEscape(r.opportunityName || "—") + '</div></div>' +
        '<div><div style="color:var(--muted);font-size:.78rem;font-weight:700;">تاريخ نهاية العقد</div><div style="font-weight:700;margin-top:2px;">' + cgpFormatDate(r.contractEndDate) + '</div></div>' +
        '<div><div style="color:var(--muted);font-size:.78rem;font-weight:700;">تاريخ العقد (التوقيع)</div><div style="font-weight:700;margin-top:2px;">' + cgpFormatDate(r.signingDate) + '</div></div>' +
    '</div>';
    listWrap.innerHTML = html;

    var backBtn = document.getElementById("cgpEndingBackBtn");
    if (backBtn) backBtn.addEventListener("click", function (e) {
        e.stopPropagation();
        cgpRenderContractEndingList();
    });
}

function cgpRenderContractEndingArc(data) {
    const row = document.getElementById("cgpChartsRow4");
    if (!row) return;

    const totalWithDate = (data.contractEndingRedCount || 0) + (data.contractEndingGreenCount || 0);
    const div = document.createElement("div");
    div.className = "chart-card tarh-duration-card";

    if (totalWithDate === 0) {
        div.innerHTML =
            '<div class="tarh-duration-header"><div class="tarh-duration-title"><i class="bi bi-hourglass-split"></i> العقود التي شارفت على الانتهاء</div></div>' +
            '<div class="no-data-box" style="margin:0;"><span class="nd-icon"><i class="bi bi-inbox"></i></span>لا توجد عقود بعمود "تاريخ نهاية العقد" ضمن الفلاتر الحالية.</div>';
        row.appendChild(div);
        return;
    }

    const redPct = (data.contractEndingRedCount / totalWithDate) * 100;
    const gaugeId = "cgpContractEndingGauge_" + Date.now();
    div.innerHTML =
        '<div class="tarh-duration-header"><div class="tarh-duration-title"><i class="bi bi-hourglass-split"></i> العقود التي شارفت على الانتهاء</div></div>' +
        '<div class="echart-box" id="' + gaugeId + '"></div>' +
        '<div class="tarh-duration-legend">' +
            '<div class="tarh-duration-legend-item"><span class="tarh-duration-dot over"></span> تنتهي 2026 — ' + cgpFormatNum(data.contractEndingRedCount) + '</div>' +
            '<div class="tarh-duration-legend-item"><span class="tarh-duration-dot within"></span> تنتهي بعد 2026 — ' + cgpFormatNum(data.contractEndingGreenCount) + '</div>' +
        '</div>';
    row.appendChild(div);

    const trackDiv = document.createElement("div");
    trackDiv.className = "chart-card tarh-track-card";
    trackDiv.innerHTML =
        '<div class="tarh-duration-header"><div class="tarh-duration-title" id="cgpEndingTrackTitle"><i class="bi bi-signpost-split"></i> تفاصيل العقود</div></div>' +
        '<div class="tarh-track-body">' +
            '<div class="no-data-box" style="margin:0;" id="cgpEndingTrackPlaceholder"><span class="nd-icon"><i class="bi bi-hand-index-thumb"></i></span>اضغط على القوس (أحمر/أخضر) لعرض العقود هنا</div>' +
            '<div id="cgpEndingTrackList" style="display:none;"></div>' +
        '</div>';
    row.appendChild(trackDiv);

    const gaugeBox = document.getElementById(gaugeId);
    gaugeBox.style.height = "230px";
    const gauge = echarts.init(gaugeBox);
    gauge.setOption(cgpBuildContractEndingArcOption(redPct, data.contractEndingRedCount, data.contractEndingGreenCount));
    cgpCharts.push(gauge);
    requestAnimationFrame(function () { try { gauge.resize(); } catch (e) {} });

    gauge.on("click", function (params) {
        if (!params.data || params.data.name === "gap") return;
        cgpShowContractEndingList(params.data.name === "red");
    });
}

/* ==========================================================================
   "كل إدارة عامة خلال 2026" - عمود Column Chart: كل إدارة عامة على المحور، وداخلها 4 أعمدة
   مجمَّعة (ربع1-ربع4)، وعدد العقود مكتوب فوق كل عمود - من عمود "نهاية فترة التجهيز"
   ========================================================================== */
const CGP_QUARTER_NAMES = ["الربع الأول", "الربع الثاني", "الربع الثالث", "الربع الرابع"];

function cgpBuildDeptQuarterColumnOption(rows) {
    const totals = {};
    rows.forEach(function (r) { totals[r.departmentLabel] = (totals[r.departmentLabel] || 0) + r.count; });
    const depts = Object.keys(totals).sort(function (a, b) { return totals[b] - totals[a]; });

    const series = CGP_QUARTER_NAMES.map(function (qName, qi) {
        const color = CGP_PALETTE[qi % CGP_PALETTE.length];
        const data = depts.map(function (d) {
            const row = rows.find(function (r) { return r.departmentLabel === d && r.quarter === qi + 1; });
            return row ? row.count : 0;
        });
        return {
            name: qName, type: "bar", data: data, barMaxWidth: 30,
            itemStyle: { color: color, borderRadius: [4, 4, 0, 0] },
            label: { show: true, position: "top", color: CGP_C.axisText, fontWeight: 700, fontSize: 10, formatter: function (p) { return p.value > 0 ? cgpFormatNum(p.value) : ""; } }
        };
    });

    return {
        tooltip: cgpTooltip({
            trigger: "axis", axisPointer: { type: "shadow" },
            formatter: function (params) {
                var lines = '<div style="font-weight:700;margin-bottom:4px;">' + params[0].axisValue + '</div>';
                params.forEach(function (p) {
                    if (p.value > 0) lines += '<div>' + p.marker + p.seriesName + ': <strong>' + cgpFormatNum(p.value) + '</strong></div>';
                });
                return lines;
            }
        }),
        legend: { top: 0, right: 8, textStyle: Object.assign({}, CGP_AXIS_LABEL_STYLE, { fontSize: 11 }), itemWidth: 12, itemHeight: 10, type: "scroll" },
        grid: { left: 16, right: 16, top: 56, bottom: 60, containLabel: true },
        xAxis: cgpAxisStyle({ type: "category", data: depts, axisLabel: { interval: 0, fontSize: 11, width: 150, overflow: "break" } }),
        yAxis: cgpAxisStyle({ type: "value", min: 0 }),
        series: series
    };
}

function cgpRenderDeptQuarterChart(data) {
    const rows = data.departmentQuarterEnding || [];
    if (!rows.length) return;
    cgpRenderChart("cgpChartsRow5", "cgpDeptQuarter", "كل إدارة عامة خلال 2026 - نهاية فترة التجهيز حسب الربع", "الربع 1 - الربع 4",
        cgpBuildDeptQuarterColumnOption(rows), 380);
}

/* ==========================================================================
   رفع الملف
   ========================================================================== */
function cgpSetUploadStatus(text, isError) {
    const el = document.getElementById("cgpUploadStatus");
    if (!el) return;
    el.textContent = text || "";
    el.className = "tc-upload-status" + (isError ? " tc-upload-status-error" : text ? " tc-upload-status-ok" : "");
}

function cgpInitUpload() {
    const btn = document.getElementById("cgpUploadBtn");
    const input = document.getElementById("cgpUploadInput");
    if (!btn || !input) return;

    btn.addEventListener("click", function () { input.click(); });
    input.addEventListener("change", async function () {
        const file = input.files && input.files[0];
        if (!file) return;

        btn.disabled = true;
        cgpSetUploadStatus("جارِ رفع الملف...", false);
        try {
            const formData = new FormData();
            formData.append("contractPeriodFile", file);
            const resp = await fetch(window.CGP_UPLOAD_URL, { method: "POST", body: formData });
            const result = await resp.json();
            if (result.success) {
                cgpSetUploadStatus(result.message, false);
                cgpLoad();
            } else {
                cgpSetUploadStatus(result.message || "تعذّر رفع الملف.", true);
            }
        } catch (err) {
            console.error("تعذّر رفع ملف العقود:", err);
            cgpSetUploadStatus("تعذّر رفع الملف، تحقق من الاتصال وحاول مجددًا.", true);
        } finally {
            btn.disabled = false;
            input.value = "";
        }
    });
}

/* ==========================================================================
   التحميل الرئيسي
   ========================================================================== */
async function cgpLoad() {
    const contentEl = document.getElementById("cgpContent");
    const noDataEl = document.getElementById("cgpNoDataBox");

    try {
        const resp = await fetch(window.CGP_DATA_URL + "?" + cgpQueryString());
        const data = await resp.json();

        cgpBuildFilterBar(data);

        if (!data.hasData) {
            if (contentEl) contentEl.style.display = "none";
            if (noDataEl) noDataEl.style.display = "";
            return;
        }

        if (contentEl) contentEl.style.display = "";
        if (noDataEl) noDataEl.style.display = "none";

        cgpDisposeCharts();
        ["cgpChartsRow2", "cgpChartsRow3", "cgpChartsRow4", "cgpChartsRow5"].forEach(function (id) {
            const el = document.getElementById(id);
            if (el) el.innerHTML = "";
        });

        cgpRenderKpiCards(data);
        cgpRenderCharts(data);
    } catch (err) {
        console.error("تعذّر تحميل بيانات العقود:", err);
        if (contentEl) contentEl.style.display = "none";
        if (noDataEl) noDataEl.style.display = "";
    }
}

document.addEventListener("DOMContentLoaded", function () {
    cgpInitUpload();
    cgpLoad();
});
