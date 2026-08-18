/* ==========================================================================
   صفحة "لوحة المطالبات التنفيذية" - 4 كروت KPI (إجمالي/معالج/متبقي/نسبة)، عمودان
   مكدَّسان (معالج/متبقي) للإدارة العامة والفرعية قابلان للترابط، دونات توزيع حالات
   "المتبقي"، ورسم "المديونيات حسب الفترة" (حالة الدفعة=غير محصّلة، من عمود "الفترة")
   ========================================================================== */
const CLX_C = {
    dark: "#0E6E61", mid: "#3F8F87", light: "#5FA89D", lime: "#8DC63F", limeDk: "#5F9E1F",
    red: "#D6534A", text: "#173B36", muted: "#6E8A86", grid: "#E1ECEA", axisText: "#181818"
};
const CLX_AXIS_LABEL_STYLE = { color: CLX_C.axisText, fontSize: 13, fontWeight: 600 };
const CLX_PALETTE = [CLX_C.dark, CLX_C.mid, CLX_C.lime, CLX_C.light, CLX_C.limeDk];

function clxAxisStyle(extra) {
    extra = extra || {};
    const merged = Object.assign({
        axisLine: { lineStyle: { color: CLX_C.grid } },
        splitLine: { lineStyle: { color: CLX_C.grid, type: "dashed" } }
    }, extra);
    merged.axisLabel = Object.assign({}, CLX_AXIS_LABEL_STYLE, extra.axisLabel || {});
    return merged;
}
function clxTooltip(extra) {
    return Object.assign({
        backgroundColor: "#FFFFFF", borderColor: CLX_C.grid, borderWidth: 1,
        textStyle: { color: CLX_C.text, fontSize: 12 },
        extraCssText: "box-shadow:0 4px 16px rgba(0,0,0,0.1);border-radius:10px;"
    }, extra || {});
}
function clxFormatNum(n) { return Number.isFinite(n) ? Math.round(n).toLocaleString("en-US") : "0"; }
function clxEscape(str) {
    const div = document.createElement("div");
    div.textContent = (str === null || str === undefined) ? "" : String(str);
    return div.innerHTML;
}

let clxCharts = [];
function clxDisposeCharts() {
    clxCharts.forEach(function (c) { try { c.dispose(); } catch (e) {} });
    clxCharts = [];
}

/* ==========================================================================
   شريط الفلاتر: الإدارة العامة / حالة المطالبة + بحث برقم الفرصة/العقد/الفاتورة
   ========================================================================== */
function clxMakeSelectWrap(id, iconClass, allLabel, options, selected) {
    const wrap = document.createElement("div");
    wrap.className = "filter-select-wrap";
    wrap.innerHTML = '<i class="bi ' + iconClass + ' filter-icon"></i>';
    const select = document.createElement("select");
    select.id = id;
    select.className = "filter-select";
    let html = '<option value="">' + clxEscape(allLabel) + '</option>';
    (options || []).forEach(function (o) {
        html += '<option value="' + clxEscape(o) + '"' + (o === selected ? " selected" : "") + '>' + clxEscape(o) + '</option>';
    });
    select.innerHTML = html;
    wrap.appendChild(select);
    wrap.insertAdjacentHTML("beforeend", '<i class="bi bi-chevron-down filter-chevron"></i>');
    return { wrap: wrap, select: select };
}

function clxMakeSearchWrap(id, placeholder, value) {
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

const clxFilters = { department: "", claimStatus: "", contractNumber: "" };

function clxBuildFilterBar(data) {
    const bar = document.getElementById("clxFilterBar");
    if (!bar) return;
    bar.innerHTML = "";

    const dept = clxMakeSelectWrap("clxDeptFilter", "bi-diagram-3", "كل الإدارات العامة", data.departmentOptions || [], clxFilters.department);
    const search = clxMakeSearchWrap("clxSearchFilter", "ابحث برقم العقد...", clxFilters.contractNumber);

    bar.appendChild(dept.wrap);
    bar.appendChild(search.wrap);

    dept.select.addEventListener("change", function () { clxFilters.department = dept.select.value; clxLoad(); });
    let debounce = null;
    search.input.addEventListener("input", function () {
        clxFilters.contractNumber = search.input.value.trim();
        clearTimeout(debounce);
        debounce = setTimeout(clxLoad, 400);
    });
}

function clxQueryString() {
    const p = new URLSearchParams();
    if (clxFilters.department) p.set("department", clxFilters.department);
    if (clxFilters.claimStatus) p.set("claimStatus", clxFilters.claimStatus);
    if (clxFilters.contractNumber) p.set("contractNumber", clxFilters.contractNumber);
    return p.toString();
}

/* ==========================================================================
   كروت الـKPI الأربعة
   ========================================================================== */
const CLX_SAR = '<svg class="kpi-sar" viewBox="0 0 1124.14 1256.39" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-label="ريال سعودي">' +
    '<path d="M699.62,1113.02h0c-20.06,44.48-33.32,92.75-38.4,143.37l424.51-90.24c20.06-44.47,33.31-92.75,38.4-143.37l-424.51,90.24Z"/>' +
    '<path d="M1085.73,895.8c20.06-44.47,33.32-92.75,38.4-143.37l-330.68,70.33v-135.2l292.27-62.11c20.06-44.47,33.32-92.75,38.4-143.37l-330.68,70.27V66.13c-50.67,28.45-95.67,66.32-132.25,110.99v403.35l-132.25,28.11V0c-50.67,28.44-95.67,66.32-132.25,110.99v525.69l-295.91,62.88c-20.06,44.47-33.33,92.75-38.42,143.37l334.33-71.05v170.26l-358.3,76.14c-20.06,44.47-33.32,92.75-38.4,143.37l375.04-79.7c30.53-6.35,56.77-24.4,73.83-49.24l68.78-101.97v-.02c7.14-10.55,11.3-23.27,11.3-36.97v-149.98l132.25-28.11v270.4l424.53-90.28Z"/></svg>';
function clxSarVal(n) { return clxFormatNum(n) + ' <span class="value-sar">' + CLX_SAR + '</span>'; }

function clxKpiCardHtml(cls, icon, label, count, value) {
    return '<div class="kpi-card ' + cls + '">' +
        '<div class="icon"><i class="bi ' + icon + '"></i></div>' +
        '<div class="label">' + clxEscape(label) + '</div>' +
        '<div class="value">' + clxSarVal(value) + '</div>' +
        '<div style="font-size:.78rem;font-weight:700;color:var(--muted);margin-top:4px;">' + clxFormatNum(count) + ' مطالبة</div>' +
    '</div>';
}
function clxPercentCardHtml(cls, icon, label, percent) {
    return '<div class="kpi-card ' + cls + '">' +
        '<div class="icon"><i class="bi ' + icon + '"></i></div>' +
        '<div class="label">' + clxEscape(label) + '</div>' +
        '<div class="value">' + (percent || 0).toFixed(1) + '%</div>' +
    '</div>';
}

function clxRenderKpiCards(data) {
    const grid = document.getElementById("clxKpiGrid");
    if (!grid) return;
    grid.innerHTML =
        clxKpiCardHtml("c1", "bi-file-earmark-text", "إجمالي المطالبات", data.totalCount, data.totalValue) +
        clxKpiCardHtml("c2", "bi-check-circle", "إجمالي المعالج", data.processedCount, data.processedValue) +
        clxKpiCardHtml("c4", "bi-hourglass-split", "إجمالي المتبقي", data.remainingCount, data.remainingValue) +
        clxPercentCardHtml("c3", "bi-graph-up-arrow", "نسبة المطالبات المتبقية", data.remainingPercent);
}

/* ==========================================================================
   رسم 1+2: معالج/متبقي حسب الإدارة العامة والفرعية (عمودان مكدَّسان، قابلان للترابط)
   ========================================================================== */
function clxRenderChart(rowId, containerId, title, badge, option, heightPx) {
    const row = document.getElementById(rowId);
    if (!row) return null;
    const id = containerId + "_" + Date.now() + "_" + Math.floor(Math.random() * 1000);
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">' + clxEscape(title) + '</div>' +
            (badge ? '<div class="chart-badge">' + clxEscape(badge) + '</div>' : '') +
        '</div>' +
        '<div class="echart-box" id="' + id + '" style="height:' + (heightPx || 320) + 'px;"></div>';
    row.appendChild(div);
    const chart = echarts.init(document.getElementById(id));
    chart.setOption(option);
    chart._clxCard = div;
    clxCharts.push(chart);
    requestAnimationFrame(function () { try { chart.resize(); } catch (e) {} });
    return chart;
}

// عمودان مكدَّسان رأسيًا (معالج/متبقي) - للإدارة العامة
function clxBuildProcessedRemainingOption(labels, processedCounts, remainingCounts, highlightLabel) {
    function itemStyle(i, color) {
        var selected = !!highlightLabel && labels[i] === highlightLabel;
        return { color: color, borderColor: selected ? CLX_C.text : "transparent", borderWidth: selected ? 2 : 0 };
    }
    return {
        tooltip: clxTooltip({ trigger: "axis", axisPointer: { type: "shadow" } }),
        legend: { top: 0, right: 8, textStyle: Object.assign({}, CLX_AXIS_LABEL_STYLE, { fontSize: 11 }), itemWidth: 12, itemHeight: 10 },
        grid: { left: 10, right: 16, top: 56, bottom: 60, containLabel: true },
        xAxis: clxAxisStyle({ type: "category", data: labels, axisLabel: { interval: 0, rotate: labels.length > 3 ? 20 : 0, fontSize: 11, width: 110, overflow: "break" } }),
        yAxis: clxAxisStyle({ type: "value", min: 0, axisLabel: { show: false }, splitLine: { show: false } }),
        series: [
            {
                name: "معالج", type: "bar", stack: "s", cursor: "pointer", barMaxWidth: 46,
                data: processedCounts.map(function (v, i) { return { value: v, itemStyle: itemStyle(i, CLX_C.lime) }; }),
                label: { show: true, color: "#fff", fontWeight: 700, fontSize: 10, formatter: function (p) { return p.value > 0 ? clxFormatNum(p.value) : ""; } }
            },
            {
                name: "متبقي", type: "bar", stack: "s", cursor: "pointer", barMaxWidth: 46,
                data: remainingCounts.map(function (v, i) { return { value: v, itemStyle: itemStyle(i, CLX_C.mid) }; }),
                label: { show: true, color: "#fff", fontWeight: 700, fontSize: 10, formatter: function (p) { return p.value > 0 ? clxFormatNum(p.value) : ""; } }
            }
        ]
    };
}

// عمودان أفقيان مكدَّسان (معالج/متبقي) - للإدارة الفرعية: التسمية الإجمالية دومًا خارج الشريط
// (يمين) فتبقى واضحة حتى للمحافظ صغيرة القيمة جدًا (مثل محفظة بقيمة 5) اللي كانت تختفي تسميتها
// داخل عمود رأسي قصير جدًا - طول كل شريط يبقى نسبيًا حقيقيًا (بلا تطبيع 100%)
function clxBuildSubDeptHorizontalOption(labels, processedCounts, remainingCounts) {
    const totals = labels.map(function (l, i) { return (processedCounts[i] || 0) + (remainingCounts[i] || 0); });
    const isLastIndex = labels.length - 1;
    return {
        tooltip: clxTooltip({
            trigger: "axis", axisPointer: { type: "shadow" },
            formatter: function (params) {
                var i = params[0].dataIndex;
                var lines = '<div style="font-weight:700;margin-bottom:4px;">' + clxEscape(labels[i]) + '</div>';
                lines += '<div>معالج: <strong>' + clxFormatNum(processedCounts[i] || 0) + '</strong></div>';
                lines += '<div>متبقي: <strong>' + clxFormatNum(remainingCounts[i] || 0) + '</strong></div>';
                return lines;
            }
        }),
        grid: { left: 190, right: 60, top: 10, bottom: 10 },
        xAxis: clxAxisStyle({ type: "value", show: false }),
        yAxis: clxAxisStyle({ type: "category", data: labels, axisLabel: { width: 180, overflow: "break", fontSize: 11 } }),
        series: [
            {
                name: "معالج", type: "bar", stack: "s", barMaxWidth: 22,
                itemStyle: { color: CLX_C.lime },
                data: processedCounts
            },
            {
                name: "متبقي", type: "bar", stack: "s", barMaxWidth: 22,
                itemStyle: { color: CLX_C.mid, borderRadius: [0, 4, 4, 0] },
                data: remainingCounts.map(function (v, i) {
                    return {
                        value: v,
                        label: {
                            show: true, position: "right", fontSize: 11, fontWeight: 800, color: CLX_C.axisText,
                            formatter: function () { return clxFormatNum(totals[i]); }
                        }
                    };
                })
            }
        ]
    };
}

// يستخرج صفوف الإدارات الفرعية الخاصة بإدارة عامة واحدة من portfolioBreakdown
function clxPortfoliosForDept(portfolioBreakdown, deptLabel) {
    var rows = (portfolioBreakdown || []).filter(function (r) { return r.departmentLabel === deptLabel; });
    return {
        labels: rows.map(function (r) { return r.portfolioLabel; }),
        processed: rows.map(function (r) { return r.processedCount; }),
        remaining: rows.map(function (r) { return r.remainingCount; })
    };
}

let clxSelectedDept = null;

function clxRenderDeptCharts(data) {
    const deptLabels = data.deptLabels || [];
    clxSelectedDept = deptLabels.length ? deptLabels[0] : null;

    const deptChart = clxRenderChart("clxChartsRow1", "clxDeptChart", "معالج/متبقي حسب الإدارة العامة", "اضغط إدارة",
        clxBuildProcessedRemainingOption(deptLabels, data.deptProcessedCounts || [], data.deptRemainingCounts || [], clxSelectedDept));

    const initialSub = clxPortfoliosForDept(data.portfolioBreakdown || [], clxSelectedDept);
    const subChart = clxRenderChart("clxChartsRow1", "clxSubDeptChart", "معالج/متبقي حسب الإدارة الفرعية", clxSelectedDept || "—",
        clxBuildSubDeptHorizontalOption(initialSub.labels, initialSub.processed, initialSub.remaining));

    function selectDept(name) {
        if (!name || name === clxSelectedDept) return;
        clxSelectedDept = name;
        if (deptChart) deptChart.setOption(clxBuildProcessedRemainingOption(deptLabels, data.deptProcessedCounts || [], data.deptRemainingCounts || [], clxSelectedDept), true);
        const sub = clxPortfoliosForDept(data.portfolioBreakdown || [], clxSelectedDept);
        if (subChart) {
            subChart.setOption(clxBuildSubDeptHorizontalOption(sub.labels, sub.processed, sub.remaining), true);
            const badge = subChart._clxCard && subChart._clxCard.querySelector(".chart-badge");
            if (badge) badge.textContent = clxSelectedDept;
            requestAnimationFrame(function () { try { subChart.resize(); } catch (e) {} });
        }
    }

    if (deptChart) {
        deptChart.on("click", function (params) {
            if (params.componentType !== "series" || !params.name) return;
            selectDept(params.name);
        });
    }
}

/* ==========================================================================
   "المديونيات حسب الفترة" - عمود (قيمة) + خط (عدد)، نفس تصميم صفحة المديونيات بالضبط -
   من صفوف حالة الدفعة=غير محصّلة فقط، مصنّفة بعمود "الفترة" (2022-2025 / 2026)
   ========================================================================== */
function clxBuildPeriodOption(labels, values, counts) {
    return {
        tooltip: clxTooltip({ trigger: "axis" }),
        legend: { top: 0, right: 8, textStyle: Object.assign({}, CLX_AXIS_LABEL_STYLE), itemWidth: 12, itemHeight: 10 },
        grid: { left: 16, right: 16, top: 56, bottom: 30, containLabel: true },
        xAxis: clxAxisStyle({ type: "category", data: labels, axisLabel: { fontSize: 11 } }),
        yAxis: [
            clxAxisStyle({ type: "value", axisLabel: { show: false }, splitLine: { show: false } }),
            clxAxisStyle({ type: "value", axisLabel: { show: false }, splitLine: { show: false } })
        ],
        series: [
            {
                name: "القيمة", type: "bar", data: values, yAxisIndex: 0, barMaxWidth: 46, z: 1,
                itemStyle: { color: CLX_C.dark, borderRadius: [4, 4, 0, 0] },
                label: { show: true, position: "top", offset: [0, -4], color: CLX_C.axisText, fontWeight: 700, fontSize: 10, formatter: function (p) { return clxFormatNum(p.value); } }
            },
            {
                name: "عدد المطالبات", type: "line", data: counts, yAxisIndex: 1, z: 2, smooth: true,
                symbol: "circle", symbolSize: 7,
                lineStyle: { color: CLX_C.lime, width: 2.5 },
                itemStyle: { color: CLX_C.lime },
                label: { show: true, position: "top", offset: [0, -22], color: CLX_C.limeDk, fontWeight: 800, fontSize: 10, formatter: function (p) { return clxFormatNum(p.value); } }
            }
        ]
    };
}

/* ==========================================================================
   دونات: توزيع حالات "المتبقي" الأربع - بلا Legend داخلي (كان يقصّ أسماء الحالات الطويلة)،
   بدلها قائمة نصية HTML كاملة العرض جنب الدونات (نفس أسلوب "تفاصيل حالات فتح المظاريف" بصفحة الطرح)
   ========================================================================== */
function clxBuildRemainingDonutOption(rows, total) {
    const items = rows.map(function (r, i) {
        return { name: r.status, value: r.count, itemStyle: { color: CLX_PALETTE[i % CLX_PALETTE.length] } };
    });
    return {
        tooltip: clxTooltip({
            trigger: "item",
            formatter: function (p) {
                var pct = total > 0 ? Math.round((p.value / total) * 1000) / 10 : 0;
                return '<div style="font-weight:700;">' + clxEscape(p.name) + '</div><div>العدد: <strong>' + clxFormatNum(p.value) + '</strong> (' + pct + '%)</div>';
            }
        }),
        series: [{
            type: "pie", radius: ["42%", "70%"], center: ["50%", "50%"],
            itemStyle: { borderColor: "#fff", borderWidth: 2 },
            label: { show: false }, labelLine: { show: false },
            data: items
        }]
    };
}

// قائمة نصية أسفل الدونات مباشرة (بعرض البطاقة الكامل، لا عمود جانبي ثابت) - تفادي مشكلة
// استخدام تخطيط بعمود ثابت العرض (400px) بطاقة نصف العرض هنا (كان يضغط الدونات لعرض شبه
// معدوم فتختفي بصريًا) - وبخط أكبر حتى تكون أسماء الحالات الطويلة واضحة تمامًا
function clxRenderRemainingDonutDetail(rows, total) {
    const wrap = document.getElementById("clxRemainingDetail");
    if (!wrap) return;
    const sorted = (rows || []).slice().sort(function (a, b) { return b.count - a.count; });
    wrap.innerHTML = sorted.map(function (r, i) {
        var pct = total > 0 ? Math.round((r.count / total) * 1000) / 10 : 0;
        var color = CLX_PALETTE[i % CLX_PALETTE.length];
        return '<div style="display:flex;align-items:center;justify-content:space-between;gap:10px;padding:8px 0;border-bottom:1px solid var(--border);">' +
            '<span style="display:flex;align-items:center;gap:8px;font-size:1rem;font-weight:700;color:var(--text);">' +
                '<span style="width:12px;height:12px;border-radius:4px;flex:none;background:' + color + ';"></span> ' + clxEscape(r.status) +
            '</span>' +
            '<span style="font-size:1rem;font-weight:800;white-space:nowrap;">' + clxFormatNum(r.count) + ' <span style="font-weight:600;color:var(--muted);font-size:.88rem;">(' + pct + '%)</span></span>' +
        '</div>';
    }).join("");
}

function clxRenderPeriodAndDonut(data) {
    clxRenderChart("clxChartsRow2", "clxPeriodChart", "المديونيات حسب الفترة", null,
        clxBuildPeriodOption(data.periodLabels || [], data.periodValues || [], data.periodCounts || []), 320);

    const total = (data.remainingStatusBreakdown || []).reduce(function (s, r) { return s + r.count; }, 0);
    const row = document.getElementById("clxChartsRow2");
    if (row) {
        const id = "clxRemainingDonut_" + Date.now();
        const div = document.createElement("div");
        div.className = "chart-card";
        div.innerHTML =
            '<div class="chart-header"><div class="chart-title">توزيع حالات المطالبات المتبقية</div></div>' +
            '<div class="echart-box" id="' + id + '" style="height:170px;max-width:220px;margin:0 auto;"></div>' +
            '<div id="clxRemainingDetail" style="margin-top:10px;"></div>';
        row.appendChild(div);
        const chart = echarts.init(document.getElementById(id));
        chart.setOption(clxBuildRemainingDonutOption(data.remainingStatusBreakdown || [], total));
        clxCharts.push(chart);
        requestAnimationFrame(function () { try { chart.resize(); } catch (e) {} });
        clxRenderRemainingDonutDetail(data.remainingStatusBreakdown || [], total);
    }
}

/* ==========================================================================
   رفع الملف
   ========================================================================== */
function clxSetUploadStatus(text, isError) {
    const el = document.getElementById("clxUploadStatus");
    if (!el) return;
    el.textContent = text || "";
    el.className = "tc-upload-status" + (isError ? " tc-upload-status-error" : text ? " tc-upload-status-ok" : "");
}

function clxInitUpload() {
    const btn = document.getElementById("clxUploadBtn");
    const input = document.getElementById("clxUploadInput");
    if (!btn || !input) return;

    btn.addEventListener("click", function () { input.click(); });
    input.addEventListener("change", async function () {
        const file = input.files && input.files[0];
        if (!file) return;

        btn.disabled = true;
        clxSetUploadStatus("جارِ رفع الملف...", false);
        try {
            const formData = new FormData();
            formData.append("claimsFile", file);
            const resp = await fetch(window.CLX_UPLOAD_URL, { method: "POST", body: formData });
            const result = await resp.json();
            if (result.success) {
                clxSetUploadStatus(result.message, false);
                clxLoad();
            } else {
                clxSetUploadStatus(result.message || "تعذّر رفع الملف.", true);
            }
        } catch (err) {
            console.error("تعذّر رفع ملف المطالبات:", err);
            clxSetUploadStatus("تعذّر رفع الملف، تحقق من الاتصال وحاول مجددًا.", true);
        } finally {
            btn.disabled = false;
            input.value = "";
        }
    });
}

/* ==========================================================================
   التحميل الرئيسي
   ========================================================================== */
async function clxLoad() {
    const contentEl = document.getElementById("clxContent");
    const noDataEl = document.getElementById("clxNoDataBox");

    try {
        const resp = await fetch(window.CLX_DATA_URL + "?" + clxQueryString());
        const data = await resp.json();

        clxBuildFilterBar(data);

        if (!data.hasData) {
            if (contentEl) contentEl.style.display = "none";
            if (noDataEl) noDataEl.style.display = "";
            return;
        }

        if (contentEl) contentEl.style.display = "";
        if (noDataEl) noDataEl.style.display = "none";

        clxDisposeCharts();
        ["clxChartsRow1", "clxChartsRow2"].forEach(function (id) {
            const el = document.getElementById(id);
            if (el) el.innerHTML = "";
        });

        clxRenderKpiCards(data);
        clxRenderDeptCharts(data);
        clxRenderPeriodAndDonut(data);
    } catch (err) {
        console.error("تعذّر تحميل بيانات المطالبات:", err);
        if (contentEl) contentEl.style.display = "none";
        if (noDataEl) noDataEl.style.display = "";
    }
}

/* ==========================================================================
   تصدير Excel/PDF - بنفس فلاتر الصفحة الحالية
   ========================================================================== */
function clxFormatExportDate(iso) {
    if (!iso) return "";
    var d = new Date(iso);
    return isNaN(d.getTime()) ? "" : d.toLocaleDateString("en-GB");
}

function clxInitExport() {
    const excelBtn = document.getElementById("clxExportExcelBtn");
    const pdfBtn = document.getElementById("clxExportPdfBtn");
    if (!window.CLX_EXPORT_EXCEL_URL && !window.CLX_EXPORT_DATA_URL) return;

    if (excelBtn && window.CLX_EXPORT_EXCEL_URL) {
        excelBtn.addEventListener("click", function () {
            var qs = clxQueryString();
            window.location.href = window.CLX_EXPORT_EXCEL_URL + (qs ? "?" + qs : "");
        });
    }

    if (pdfBtn && window.CLX_EXPORT_DATA_URL) {
        pdfBtn.addEventListener("click", async function () {
            pdfBtn.disabled = true;
            try {
                var qs = clxQueryString();
                const resp = await fetch(window.CLX_EXPORT_DATA_URL + (qs ? "?" + qs : ""));
                const rows = await resp.json();

                const tbody = document.getElementById("clxExportTableBody");
                if (tbody) {
                    tbody.innerHTML = rows.map(function (r) {
                        return '<tr>' +
                            '<td>' + clxEscape(r.contractNumber || "") + '</td>' +
                            '<td>' + clxEscape(r.investorName || "") + '</td>' +
                            '<td>' + clxEscape(r.invoiceNumber || "") + '</td>' +
                            '<td>' + clxEscape(r.claimStatusFull || "") + '</td>' +
                            '<td>' + clxEscape(r.generalDepartment || "") + '</td>' +
                            '<td>' + clxEscape(r.portfolio || "") + '</td>' +
                            '<td>' + clxFormatNum(r.paymentValueWithTax || 0) + '</td>' +
                            '<td>' + clxFormatExportDate(r.paymentDueDate) + '</td>' +
                        '</tr>';
                    }).join("");
                }

                const dateEl = document.getElementById("clxPrintDate");
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
    clxInitUpload();
    clxInitExport();
    if (window.CLX_DATA_URL) clxLoad();
});
