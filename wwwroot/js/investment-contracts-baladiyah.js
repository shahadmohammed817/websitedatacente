/* ==========================================================================
   صفحة "جميع العقود الاستثمارية - بلدية" - كروت KPI (نفس صفحة الأمانة)، عمود بسيط
   للأجرة السنوية حسب حالة العقد (بلا تجميع إدارة/محفظة - غير مطبَّقة على البلديات)،
   Funnel بالقيمة (الأجرة السنوية) + دونات توزيع الدفعات حسب الحالة، ورسم مقارنة
   "متبقي غير مستحق" مقابل "تم إرساله للتنفيذ القضائي"
   ========================================================================== */
const INVB_C = {
    dark: "#0E6E61", mid: "#3F8F87", light: "#5FA89D", lime: "#8DC63F", limeDk: "#5F9E1F",
    text: "#173B36", muted: "#6E8A86", grid: "#E1ECEA", axisText: "#181818", gray: "#9AA3A1"
};
const INVB_AXIS_LABEL_STYLE = { color: INVB_C.axisText, fontSize: 13, fontWeight: 600 };
const INVB_PALETTE = [INVB_C.dark, INVB_C.mid, INVB_C.lime, INVB_C.light, INVB_C.limeDk];

function invbAxisStyle(extra) {
    extra = extra || {};
    const merged = Object.assign({
        axisLine: { lineStyle: { color: INVB_C.grid } },
        splitLine: { lineStyle: { color: INVB_C.grid, type: "dashed" } }
    }, extra);
    merged.axisLabel = Object.assign({}, INVB_AXIS_LABEL_STYLE, extra.axisLabel || {});
    return merged;
}
function invbTooltip(extra) {
    return Object.assign({
        backgroundColor: "#FFFFFF", borderColor: INVB_C.grid, borderWidth: 1,
        textStyle: { color: INVB_C.text, fontSize: 12 },
        extraCssText: "box-shadow:0 4px 16px rgba(0,0,0,0.1);border-radius:10px;"
    }, extra || {});
}
function invbFormatNum(n) { return Number.isFinite(n) ? Math.round(n).toLocaleString("en-US") : "0"; }
function invbEscape(str) {
    const div = document.createElement("div");
    div.textContent = (str === null || str === undefined) ? "" : String(str);
    return div.innerHTML;
}

let invbCharts = [];
function invbDisposeCharts() {
    invbCharts.forEach(function (c) { try { c.dispose(); } catch (e) {} });
    invbCharts = [];
}

function invbRenderChart(rowId, containerId, title, badge, option, heightPx) {
    const row = document.getElementById(rowId);
    if (!row) return null;
    const id = containerId + "_" + Date.now() + "_" + Math.floor(Math.random() * 1000);
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">' + invbEscape(title) + '</div>' +
            (badge ? '<div class="chart-badge">' + invbEscape(badge) + '</div>' : '') +
        '</div>' +
        '<div class="echart-box" id="' + id + '" style="height:' + (heightPx || 320) + 'px;"></div>';
    row.appendChild(div);
    const chart = echarts.init(document.getElementById(id));
    chart.setOption(option);
    invbCharts.push(chart);
    requestAnimationFrame(function () { try { chart.resize(); } catch (e) {} });
    return chart;
}

/* ==========================================================================
   شريط الفلاتر: تاريخ بداية العقد / حالة سريان العقد / يوجد زيادة سنوية
   ========================================================================== */
function invbMakeSelectWrap(id, iconClass, allLabel, options, selected) {
    const wrap = document.createElement("div");
    wrap.className = "filter-select-wrap";
    wrap.innerHTML = '<i class="bi ' + iconClass + ' filter-icon"></i>';
    const select = document.createElement("select");
    select.id = id;
    select.className = "filter-select";
    let html = '<option value="">' + invbEscape(allLabel) + '</option>';
    (options || []).forEach(function (o) {
        var val = (o === null || o === undefined) ? "" : String(o);
        html += '<option value="' + invbEscape(val) + '"' + (val === selected ? " selected" : "") + '>' + invbEscape(val) + '</option>';
    });
    select.innerHTML = html;
    wrap.appendChild(select);
    wrap.insertAdjacentHTML("beforeend", '<i class="bi bi-chevron-down filter-chevron"></i>');
    return { wrap: wrap, select: select };
}

const invbFilters = { year: "", validityStatus: "", hasIncrease: "", baladiyah: "" };

function invbBuildFilterBar(data) {
    const bar = document.getElementById("invFilterBar");
    if (!bar) return;
    bar.innerHTML = "";

    const year = invbMakeSelectWrap("invbYearFilter", "bi-calendar3", "كل سنوات بداية العقد", data.yearOptions || [], invbFilters.year);
    const validity = invbMakeSelectWrap("invbValidityFilter", "bi-shield-check", "كل حالات سريان العقد", data.contractValidityStatusOptions || [], invbFilters.validityStatus);
    const increase = invbMakeSelectWrap("invbIncreaseFilter", "bi-graph-up", "هل يوجد زيادة سنوية", ["Y", "N"], invbFilters.hasIncrease);
    increase.select.querySelectorAll("option").forEach(function (opt) {
        if (opt.value === "Y") opt.textContent = "يوجد زيادة سنوية";
        if (opt.value === "N") opt.textContent = "لا يوجد زيادة سنوية";
    });
    const baladiyah = invbMakeSelectWrap("invbBaladiyahFilter", "bi-geo-alt", "كل البلديات", data.baladiyahOptions || [], invbFilters.baladiyah);

    bar.appendChild(year.wrap);
    bar.appendChild(validity.wrap);
    bar.appendChild(increase.wrap);
    bar.appendChild(baladiyah.wrap);

    year.select.addEventListener("change", function () { invbFilters.year = year.select.value; invbLoad(); });
    validity.select.addEventListener("change", function () { invbFilters.validityStatus = validity.select.value; invbLoad(); });
    increase.select.addEventListener("change", function () { invbFilters.hasIncrease = increase.select.value; invbLoad(); });
    baladiyah.select.addEventListener("change", function () { invbFilters.baladiyah = baladiyah.select.value; invbLoad(); });
}

function invbQueryString() {
    const p = new URLSearchParams();
    if (invbFilters.year) p.set("year", invbFilters.year);
    if (invbFilters.validityStatus) p.set("validityStatus", invbFilters.validityStatus);
    if (invbFilters.hasIncrease) p.set("hasIncrease", invbFilters.hasIncrease);
    if (invbFilters.baladiyah) p.set("baladiyah", invbFilters.baladiyah);
    return p.toString();
}

/* ==========================================================================
   كروت الـKPI: إجمالي العقود+القيمة، ثم كرت لكل نوع عقد (نفس صفحة الأمانة بالضبط)
   ========================================================================== */
const INVB_SAR = '<svg class="kpi-sar" viewBox="0 0 1124.14 1256.39" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-label="ريال سعودي">' +
    '<path d="M699.62,1113.02h0c-20.06,44.48-33.32,92.75-38.4,143.37l424.51-90.24c20.06-44.47,33.31-92.75,38.4-143.37l-424.51,90.24Z"/>' +
    '<path d="M1085.73,895.8c20.06-44.47,33.32-92.75,38.4-143.37l-330.68,70.33v-135.2l292.27-62.11c20.06-44.47,33.32-92.75,38.4-143.37l-330.68,70.27V66.13c-50.67,28.45-95.67,66.32-132.25,110.99v403.35l-132.25,28.11V0c-50.67,28.44-95.67,66.32-132.25,110.99v525.69l-295.91,62.88c-20.06,44.47-33.33,92.75-38.42,143.37l334.33-71.05v170.26l-358.3,76.14c-20.06,44.47-33.32,92.75-38.4,143.37l375.04-79.7c30.53-6.35,56.77-24.4,73.83-49.24l68.78-101.97v-.02c7.14-10.55,11.3-23.27,11.3-36.97v-149.98l132.25-28.11v270.4l424.53-90.28Z"/></svg>';
function invbSarVal(n) { return invbFormatNum(n) + ' <span class="value-sar">' + INVB_SAR + '</span>'; }

function invbRenderKpiCards(data) {
    const grid = document.getElementById("invKpiGrid");
    if (!grid) return;

    const cls = ["c1", "c2", "c4", "c3", "c5"];
    const icons = ["bi-file-earmark-ruled", "bi-building", "bi-signpost-split", "bi-key", "bi-briefcase"];

    var html = '<div class="kpi-card c2">' +
        '<div class="icon"><i class="bi bi-journal-text"></i></div>' +
        '<div class="label">إجمالي عدد العقود</div>' +
        '<div class="value">' + invbSarVal(data.totalAnnualValue || 0) + '</div>' +
        '<div style="font-size:.78rem;font-weight:700;color:var(--muted);margin-top:4px;">' + invbFormatNum(data.totalCount || 0) + ' عقد</div>' +
    '</div>';

    html += (data.typeSummary || []).map(function (t, i) {
        return '<div class="kpi-card ' + cls[i % cls.length] + '">' +
            '<div class="icon"><i class="bi ' + icons[i % icons.length] + '"></i></div>' +
            '<div class="label">' + invbEscape(t.typeLabel) + '</div>' +
            '<div class="value">' + invbSarVal(t.totalValue) + '</div>' +
            '<div style="font-size:.78rem;font-weight:700;color:var(--muted);margin-top:4px;">' + invbFormatNum(t.count) + ' عقد</div>' +
        '</div>';
    }).join("");

    grid.innerHTML = html;
}

/* ==========================================================================
   رسم 1: الأجرة السنوية حسب حالة العقد - عمود واحد لكل حالة (9 حالات)، بلا تجميع
   إدارة/محفظة - نفس تنسيق الأعمدة وترتيب محور Xالمستخدَم برسم "الأجرة السنوية -
   الإدارة العامة" بصفحة الأمانة (invBuildValueColumnOption) بالضبط
   ========================================================================== */
function invbBuildStatusColumnOption(rows) {
    const labels = rows.map(function (r) { return r.statusLabel; });
    const values = rows.map(function (r) { return r.totalValue; });
    return {
        tooltip: invbTooltip({
            trigger: "axis", axisPointer: { type: "shadow" },
            formatter: function (params) {
                var p = params[0];
                return '<div style="font-weight:700;margin-bottom:4px;">' + invbEscape(p.name) + '</div><div>القيمة: <strong>' + invbFormatNum(p.value) + '</strong></div>';
            }
        }),
        grid: { left: 16, right: 16, top: 30, bottom: 40, containLabel: true },
        xAxis: invbAxisStyle({ type: "category", data: labels, axisLabel: { interval: 0, rotate: 0, fontSize: 11, width: 100, overflow: "break" } }),
        yAxis: invbAxisStyle({ type: "value", min: 0, axisLabel: { show: false }, splitLine: { show: false } }),
        series: [{
            type: "bar", barMaxWidth: 70, barCategoryGap: "45%",
            data: values.map(function (v, i) { return { value: v, itemStyle: { color: INVB_PALETTE[i % INVB_PALETTE.length], borderRadius: [4, 4, 0, 0] } }; }),
            label: { show: true, position: "top", color: INVB_C.axisText, fontWeight: 800, fontSize: 13, formatter: function (p) { return invbFormatNum(p.value); } }
        }]
    };
}

function invbRenderRow1(data) {
    invbRenderChart("invBRow1", "invbStatusChart", "الأجرة السنوية حسب حالة العقد", null,
        invbBuildStatusColumnOption(data.statusValueTotals || []), 380);
}

/* ==========================================================================
   رسم 2: Funnel بالقيمة (الأجرة السنوية) + دونات توزيع الدفعات حسب الحالة
   ========================================================================== */
function invbBuildFunnelOption(stages) {
    const maxVal = stages.length ? stages[0].value : 0;
    return {
        tooltip: invbTooltip({
            trigger: "item",
            formatter: function (p) { return '<div style="font-weight:700;">' + invbEscape(p.name) + '</div><div>القيمة: <strong>' + invbFormatNum(p.value) + '</strong></div>'; }
        }),
        series: [{
            type: "funnel", left: "4%", top: 10, bottom: 4, width: "92%", min: 0, max: maxVal || 1,
            sort: "none", gap: 3,
            label: { show: true, position: "inside", color: "#fff", fontWeight: 700, fontSize: 12, formatter: function (p) { return p.name + "\n" + invbFormatNum(p.value); } },
            itemStyle: { borderColor: "#fff", borderWidth: 2 },
            data: stages.map(function (s, i) { return { name: s.label, value: s.value, itemStyle: { color: INVB_PALETTE[i % INVB_PALETTE.length] } }; })
        }]
    };
}

function invbBuildDonutOption(rows) {
    const items = rows.map(function (r, i) {
        return { name: r.label, value: r.value, itemStyle: { color: INVB_PALETTE[i % INVB_PALETTE.length] } };
    });
    return {
        tooltip: invbTooltip({
            trigger: "item",
            formatter: function (p) { return '<div style="font-weight:700;">' + invbEscape(p.name) + '</div><div>القيمة: <strong>' + invbFormatNum(p.value) + '</strong> (' + p.percent + '%)</div>'; }
        }),
        series: [{
            type: "pie", radius: ["42%", "70%"], center: ["50%", "50%"],
            itemStyle: { borderColor: "#fff", borderWidth: 2 },
            label: { show: false }, labelLine: { show: false },
            data: items
        }]
    };
}

// قائمة نصية أسفل الدونات (بعرض البطاقة الكامل) بدل Legend داخلي يقصّ الأسماء الطويلة -
// نفس حل مشكلة انضغاط الدونات داخل كارد نصف العرض (charts-grid-3) المكتشَفة سابقًا بصفحة
// "المديونيات والمطالبات"
function invbRenderDonutDetail(elId, rows, total) {
    const wrap = document.getElementById(elId);
    if (!wrap) return;
    wrap.innerHTML = rows.map(function (r, i) {
        var pct = total > 0 ? Math.round((r.value / total) * 1000) / 10 : 0;
        var color = INVB_PALETTE[i % INVB_PALETTE.length];
        return '<div style="display:flex;align-items:center;justify-content:space-between;gap:10px;padding:7px 0;border-bottom:1px solid var(--border);">' +
            '<span style="display:flex;align-items:center;gap:8px;font-size:.86rem;font-weight:700;color:var(--text);">' +
                '<span style="width:11px;height:11px;border-radius:3px;flex:none;background:' + color + ';"></span> ' + invbEscape(r.label) +
            '</span>' +
            '<span style="font-size:.86rem;font-weight:800;white-space:nowrap;">' + invbFormatNum(r.value) + ' <span style="font-weight:600;color:var(--muted);font-size:.76rem;">(' + pct + '%)</span></span>' +
        '</div>';
    }).join("");
}

function invbRenderFunnelAndDonut(data) {
    invbRenderChart("invBRow2", "invbFunnelChart", "مسار الدفعات (الأجرة السنوية)", null,
        invbBuildFunnelOption(data.funnelStages || []), 340);

    const row = document.getElementById("invBRow2");
    if (!row) return;
    const rows = data.paymentStatusDonut || [];
    const total = rows.reduce(function (s, r) { return s + r.value; }, 0);
    const id = "invbDonut_" + Date.now();
    const listId = "invbDonutDetail_" + Date.now();
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header"><div class="chart-title">توزيع الدفعات حسب الحالة</div></div>' +
        '<div class="echart-box" id="' + id + '" style="height:170px;max-width:220px;margin:0 auto;"></div>' +
        '<div id="' + listId + '" style="margin-top:10px;"></div>';
    row.appendChild(div);
    const chart = echarts.init(document.getElementById(id));
    chart.setOption(invbBuildDonutOption(rows));
    invbCharts.push(chart);
    requestAnimationFrame(function () { try { chart.resize(); } catch (e) {} });
    invbRenderDonutDetail(listId, rows, total);
}

/* ==========================================================================
   رسم 3: متبقي غير مستحق مقابل تم إرساله للتنفيذ القضائي (عمودان، نفس تنسيق أعمدة رسم 1) -
   رجّعناها للشكل البسيط (إجمالي عام بلا تفصيل بلديات) بطلب صريح من المستخدمة
   ========================================================================== */
function invbRenderRemainingVsExecution(data) {
    const byBaladiyah = data.baladiyahRemainingVsExecution || [];
    const remainingTotal = byBaladiyah.reduce(function (s, r) { return s + r.remainingNotDueValue; }, 0);
    const executionTotal = byBaladiyah.reduce(function (s, r) { return s + r.sentToExecutionValue; }, 0);
    const rows = [
        { statusLabel: "متبقي غير مستحق", totalValue: remainingTotal },
        { statusLabel: "تم الإرسال للتنفيذ القضائي", totalValue: executionTotal }
    ];
    invbRenderChart("invBRow3", "invbRemainVsExecChart", "متبقي غير مستحق مقابل تم إرساله للتنفيذ القضائي", null,
        invbBuildStatusColumnOption(rows), 340);
}

/* ==========================================================================
   رفع الملف
   ========================================================================== */
function invbSetUploadStatus(text, isError) {
    const el = document.getElementById("invUploadStatus");
    if (!el) return;
    el.textContent = text || "";
    el.className = "tc-upload-status" + (isError ? " tc-upload-status-error" : text ? " tc-upload-status-ok" : "");
}

function invbInitUpload() {
    const btn = document.getElementById("invUploadBtn");
    const input = document.getElementById("invUploadInput");
    if (!btn || !input) return;

    btn.addEventListener("click", function () { input.click(); });
    input.addEventListener("change", async function () {
        const file = input.files && input.files[0];
        if (!file) return;

        btn.disabled = true;
        invbSetUploadStatus("جارِ رفع الملف...", false);
        try {
            const formData = new FormData();
            formData.append("contractsFile", file);
            const resp = await fetch(window.INV_UPLOAD_URL, { method: "POST", body: formData });
            const result = await resp.json();
            if (result.success) {
                invbSetUploadStatus(result.message, false);
                invbLoad();
            } else {
                invbSetUploadStatus(result.message || "تعذّر رفع الملف.", true);
            }
        } catch (err) {
            console.error("تعذّر رفع ملف العقود:", err);
            invbSetUploadStatus("تعذّر رفع الملف، تحقق من الاتصال وحاول مجددًا.", true);
        } finally {
            btn.disabled = false;
            input.value = "";
        }
    });
}

/* ==========================================================================
   التحميل الرئيسي
   ========================================================================== */
async function invbLoad() {
    const contentEl = document.getElementById("invContent");
    const noDataEl = document.getElementById("invNoDataBox");

    try {
        const resp = await fetch(window.INV_DATA_URL + "?" + invbQueryString());
        const data = await resp.json();

        invbBuildFilterBar(data);

        if (!data.hasData) {
            if (contentEl) contentEl.style.display = "none";
            if (noDataEl) noDataEl.style.display = "";
            return;
        }

        if (contentEl) contentEl.style.display = "";
        if (noDataEl) noDataEl.style.display = "none";

        invbDisposeCharts();
        ["invBRow1", "invBRow2", "invBRow3"].forEach(function (id) {
            const el = document.getElementById(id);
            if (el) el.innerHTML = "";
        });

        invbRenderKpiCards(data);
        invbRenderRow1(data);
        invbRenderFunnelAndDonut(data);
        invbRenderRemainingVsExecution(data);
    } catch (err) {
        console.error("تعذّر تحميل بيانات العقود الاستثمارية (بلدية):", err);
        if (contentEl) contentEl.style.display = "none";
        if (noDataEl) noDataEl.style.display = "";
    }
}

document.addEventListener("DOMContentLoaded", function () {
    invbInitUpload();
    if (window.INV_DATA_URL) invbLoad();
});
