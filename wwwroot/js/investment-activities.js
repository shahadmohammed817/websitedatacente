/* ==========================================================================
   صفحة "الأنشطة الاستثمارية" - كروت KPI (إجمالي + لكل منطقة)، عمودا القطاعات
   (عدد ثم قيمة)، خط الأجرة السنوية حسب سنة العقد، وخريطة Leaflet حقيقية بعلامات
   دائرية بلون الشعار (بلا أحمر) مع تلميح عند التمرير
   ========================================================================== */
const ACT_C = {
    dark: "#0E6E61", mid: "#3F8F87", light: "#5FA89D", lime: "#8DC63F", limeDk: "#5F9E1F",
    text: "#173B36", muted: "#6E8A86", grid: "#E1ECEA", axisText: "#181818"
};
const ACT_AXIS_LABEL_STYLE = { color: ACT_C.axisText, fontSize: 13, fontWeight: 600 };
const ACT_PALETTE = [ACT_C.dark, ACT_C.mid, ACT_C.lime, ACT_C.light, ACT_C.limeDk];

function actAxisStyle(extra) {
    extra = extra || {};
    const merged = Object.assign({
        axisLine: { lineStyle: { color: ACT_C.grid } },
        splitLine: { lineStyle: { color: ACT_C.grid, type: "dashed" } }
    }, extra);
    merged.axisLabel = Object.assign({}, ACT_AXIS_LABEL_STYLE, extra.axisLabel || {});
    return merged;
}
function actTooltip(extra) {
    return Object.assign({
        backgroundColor: "#FFFFFF", borderColor: ACT_C.grid, borderWidth: 1,
        textStyle: { color: ACT_C.text, fontSize: 12 },
        extraCssText: "box-shadow:0 4px 16px rgba(0,0,0,0.1);border-radius:10px;"
    }, extra || {});
}
function actFormatNum(n) { return Number.isFinite(n) ? Math.round(n).toLocaleString("en-US") : "0"; }
function actEscape(str) {
    const div = document.createElement("div");
    div.textContent = (str === null || str === undefined) ? "" : String(str);
    return div.innerHTML;
}

let actCharts = [];
function actDisposeCharts() {
    actCharts.forEach(function (c) { try { c.dispose(); } catch (e) {} });
    actCharts = [];
}

function actRenderChart(rowId, containerId, title, badge, option, heightPx) {
    const row = document.getElementById(rowId);
    if (!row) return null;
    const id = containerId + "_" + Date.now() + "_" + Math.floor(Math.random() * 1000);
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">' + actEscape(title) + '</div>' +
            (badge ? '<div class="chart-badge">' + actEscape(badge) + '</div>' : '') +
        '</div>' +
        '<div class="echart-box" id="' + id + '" style="height:' + (heightPx || 320) + 'px;"></div>';
    row.appendChild(div);
    const chart = echarts.init(document.getElementById(id));
    chart.setOption(option);
    actCharts.push(chart);
    requestAnimationFrame(function () { try { chart.resize(); } catch (e) {} });
    return chart;
}

/* ==========================================================================
   شريط الفلاتر: سنة العقد / الأنشطة
   ========================================================================== */
function actMakeSelectWrap(id, iconClass, allLabel, options, selected) {
    const wrap = document.createElement("div");
    wrap.className = "filter-select-wrap";
    wrap.innerHTML = '<i class="bi ' + iconClass + ' filter-icon"></i>';
    const select = document.createElement("select");
    select.id = id;
    select.className = "filter-select";
    let html = '<option value="">' + actEscape(allLabel) + '</option>';
    (options || []).forEach(function (o) {
        var val = (o === null || o === undefined) ? "" : String(o);
        html += '<option value="' + actEscape(val) + '"' + (val === selected ? " selected" : "") + '>' + actEscape(val) + '</option>';
    });
    select.innerHTML = html;
    wrap.appendChild(select);
    wrap.insertAdjacentHTML("beforeend", '<i class="bi bi-chevron-down filter-chevron"></i>');
    return { wrap: wrap, select: select };
}

const actFilters = { year: "", activity: "", region: "" };

function actBuildFilterBar(data) {
    const bar = document.getElementById("actFilterBar");
    if (!bar) return;
    bar.innerHTML = "";

    const year = actMakeSelectWrap("actYearFilter", "bi-calendar3", "كل سنوات العقد", data.yearOptions || [], actFilters.year);
    const activity = actMakeSelectWrap("actActivityFilter", "bi-diagram-3", "كل الأنشطة", data.activityOptions || [], actFilters.activity);
    const region = actMakeSelectWrap("actRegionFilter", "bi-geo-alt", "كل المناطق", data.regionOptions || [], actFilters.region);

    bar.appendChild(year.wrap);
    bar.appendChild(activity.wrap);
    bar.appendChild(region.wrap);

    year.select.addEventListener("change", function () { actFilters.year = year.select.value; actLoad(); });
    activity.select.addEventListener("change", function () { actFilters.activity = activity.select.value; actLoad(); });
    region.select.addEventListener("change", function () { actFilters.region = region.select.value; actLoad(); });
}

function actQueryString() {
    const p = new URLSearchParams();
    if (actFilters.year) p.set("year", actFilters.year);
    if (actFilters.activity) p.set("activity", actFilters.activity);
    if (actFilters.region) p.set("region", actFilters.region);
    return p.toString();
}

/* ==========================================================================
   كروت الـKPI: إجمالي (عدد+أجرة سنوية)، ثم كرت لكل نوع عقد (عدد+قيمة)
   ========================================================================== */
const ACT_SAR = '<svg class="kpi-sar" viewBox="0 0 1124.14 1256.39" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-label="ريال سعودي">' +
    '<path d="M699.62,1113.02h0c-20.06,44.48-33.32,92.75-38.4,143.37l424.51-90.24c20.06-44.47,33.31-92.75,38.4-143.37l-424.51,90.24Z"/>' +
    '<path d="M1085.73,895.8c20.06-44.47,33.32-92.75,38.4-143.37l-330.68,70.33v-135.2l292.27-62.11c20.06-44.47,33.32-92.75,38.4-143.37l-330.68,70.27V66.13c-50.67,28.45-95.67,66.32-132.25,110.99v403.35l-132.25,28.11V0c-50.67,28.44-95.67,66.32-132.25,110.99v525.69l-295.91,62.88c-20.06,44.47-33.33,92.75-38.42,143.37l334.33-71.05v170.26l-358.3,76.14c-20.06,44.47-33.32,92.75-38.4,143.37l375.04-79.7c30.53-6.35,56.77-24.4,73.83-49.24l68.78-101.97v-.02c7.14-10.55,11.3-23.27,11.3-36.97v-149.98l132.25-28.11v270.4l424.53-90.28Z"/></svg>';
function actSarVal(n) { return actFormatNum(n) + ' <span class="value-sar">' + ACT_SAR + '</span>'; }

function actRenderKpiCards(data) {
    const grid = document.getElementById("actKpiGrid");
    if (!grid) return;

    const cls = ["c1", "c2", "c4", "c3", "c5"];
    const icons = ["bi-geo-alt", "bi-building", "bi-signpost-split", "bi-key", "bi-briefcase"];

    var html = '<div class="kpi-card c2">' +
        '<div class="icon"><i class="bi bi-journal-text"></i></div>' +
        '<div class="label">إجمالي الأجرة السنوية</div>' +
        '<div class="value">' + actSarVal(data.totalAnnualRent || 0) + '</div>' +
        '<div style="font-size:.78rem;font-weight:700;color:var(--muted);margin-top:4px;">' + actFormatNum(data.totalCount || 0) + ' عقد</div>' +
    '</div>';

    html += (data.contractTypeSummary || []).map(function (t, i) {
        return '<div class="kpi-card ' + cls[i % cls.length] + '">' +
            '<div class="icon"><i class="bi ' + icons[i % icons.length] + '"></i></div>' +
            '<div class="label">' + actEscape(t.contractType) + '</div>' +
            '<div class="value">' + actSarVal(t.totalAnnualRent) + '</div>' +
            '<div style="font-size:.78rem;font-weight:700;color:var(--muted);margin-top:4px;">' + actFormatNum(t.count) + ' عقد</div>' +
        '</div>';
    }).join("");

    grid.innerHTML = html;
}

/* ==========================================================================
   رسم 1+2: عدد/قيمة العقود حسب القطاع - كل رسمة بصف مستقل بعرض كامل، أعمدة عريضة
   بمسافة واضحة بينها، وأسماء محور Xمرتّبة بمسافة كافية بينها بلا تراكب
   ========================================================================== */
// التلميح يعرض العدد (ارتفاع العمود) والأجرة السنوية معًا لكل قطاع - بدل رسمة ثانية منفصلة
// لنفس البيانات (كانت مكرِّرة لنفس تصنيف القطاعات بمقياس مختلف بس)
function actBuildSectorColumnOption(rows) {
    const labels = rows.map(function (r) { return r.sector; });
    return {
        tooltip: actTooltip({
            trigger: "axis", axisPointer: { type: "shadow" },
            formatter: function (params) {
                var p = params[0];
                var r = rows[p.dataIndex];
                return '<div style="font-weight:700;margin-bottom:4px;">' + actEscape(p.name) + '</div>' +
                    '<div>العدد: <strong>' + actFormatNum(r.count) + '</strong></div>' +
                    '<div>الأجرة السنوية: <strong>' + actFormatNum(r.totalAnnualRent) + '</strong></div>';
            }
        }),
        grid: { left: 16, right: 16, top: 30, bottom: 60, containLabel: true },
        xAxis: actAxisStyle({ type: "category", data: labels, axisLabel: { interval: 0, rotate: 0, fontSize: 12, fontWeight: 700, width: 130, overflow: "break" } }),
        yAxis: actAxisStyle({ type: "value", min: 0, axisLabel: { show: false }, splitLine: { show: false } }),
        series: [{
            type: "bar", barMaxWidth: 110, barCategoryGap: "55%",
            data: rows.map(function (r, i) { return { value: r.count, itemStyle: { color: ACT_PALETTE[i % ACT_PALETTE.length], borderRadius: [4, 4, 0, 0] } }; }),
            label: { show: true, position: "top", color: ACT_C.axisText, fontWeight: 800, fontSize: 14, formatter: function (p) { return actFormatNum(p.value); } }
        }]
    };
}

function actRenderSectorCharts(data) {
    const byCount = (data.sectorSummary || []).slice().sort(function (a, b) { return b.count - a.count; });
    actRenderChart("actChartsRow1a", "actSectorCountChart", "عدد العقود حسب القطاع", null,
        actBuildSectorColumnOption(byCount), 360);
}

/* ==========================================================================
   رسم 3: الأجرة السنوية حسب سنة العقد (خط)
   ========================================================================== */
function actBuildYearLineOption(rows) {
    const labels = rows.map(function (r) { return String(r.year); });
    const values = rows.map(function (r) { return r.totalAnnualRent; });
    return {
        tooltip: actTooltip({ trigger: "axis" }),
        grid: { left: 55, right: 22, top: 30, bottom: 40 },
        xAxis: actAxisStyle({ type: "category", boundaryGap: true, data: labels }),
        yAxis: actAxisStyle({ type: "value", axisLabel: { show: false }, splitLine: { show: false } }),
        series: [{
            name: "الأجرة السنوية", type: "line", data: values, smooth: false,
            symbol: "circle", symbolSize: 6, lineStyle: { color: ACT_C.dark, width: 2.5 },
            itemStyle: { color: ACT_C.dark, borderWidth: 2, borderColor: "#fff" },
            areaStyle: { color: ACT_C.dark, opacity: 0.08 },
            label: { show: true, position: "top", color: ACT_C.dark, fontWeight: 800, fontSize: 11, formatter: function (p) { return actFormatNum(p.value); } }
        }]
    };
}

function actRenderYearChart(data) {
    actRenderChart("actChartsRow2", "actYearChart", "الأجرة السنوية حسب سنة العقد", null,
        actBuildYearLineOption(data.yearlyRent || []), 320);
}

/* ==========================================================================
   بطاقة معلومات: العقد صاحب أعلى أجرة سنوية - صف مستقل نحيف جدًا (شريط أفقي واحد)
   بعد رسم الخط مباشرة - أيقونة "خط بياني صاعد" (عكس bi-graph-down-arrow المستخدَمة
   لبطاقات "الأقل" بصفحات أخرى)
   ========================================================================== */
function actRenderTopContractInsight(data) {
    const el = document.getElementById("actTopContractCard");
    if (!el) return;
    const top = data.topContract;

    el.className = "act-top-strip";
    el.innerHTML =
        '<div class="act-top-strip-icon"><i class="bi bi-graph-up-arrow"></i></div>' +
        '<div class="act-top-strip-item"><span class="lbl">أعلى عقد أجرة سنوية:</span><span class="val">' + actEscape(top ? top.contractNumber : "—") + '</span></div>' +
        '<div class="act-top-strip-name">' + actEscape(top ? top.opportunityName : "—") + '</div>' +
        '<div class="act-top-strip-item"><span class="lbl">القيمة:</span><span class="val brand">' + actFormatNum(top ? top.annualRent : 0) + ' ' + ACT_SAR + '</span></div>';
}

/* ==========================================================================
   الخريطة: Leaflet + OpenStreetMap، علامات دائرية بلون الشعار مجمّعة، تلميح
   عند تمرير الماوس يعرض بيانات الفرصة
   ========================================================================== */
let actMap = null;
let actMarkerCluster = null;

function actFormatDate(iso) {
    if (!iso) return "—";
    var d = new Date(iso);
    return isNaN(d.getTime()) ? "—" : d.toLocaleDateString("en-GB");
}

function actRenderMap(data) {
    const points = data.mapPoints || [];
    const box = document.getElementById("actMapBox");
    if (!box) return;

    if (!actMap) {
        actMap = L.map(box, { scrollWheelZoom: false }).setView([24.47, 39.61], 11);
        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 19,
            attribution: "© OpenStreetMap contributors"
        }).addTo(actMap);
    }

    if (actMarkerCluster) {
        actMap.removeLayer(actMarkerCluster);
    }
    actMarkerCluster = L.markerClusterGroup({
        iconCreateFunction: function (cluster) {
            return L.divIcon({
                html: '<div style="background:' + ACT_C.dark + ';color:#fff;border-radius:50%;width:38px;height:38px;display:flex;align-items:center;justify-content:center;font-weight:800;font-size:12px;border:2px solid #fff;box-shadow:0 2px 6px rgba(0,0,0,.25);">' + cluster.getChildCount() + '</div>',
                className: "act-cluster-icon",
                iconSize: [38, 38]
            });
        }
    });

    points.forEach(function (p) {
        if (!Number.isFinite(p.y) || !Number.isFinite(p.x)) return;
        const marker = L.circleMarker([p.y, p.x], {
            radius: 8, color: "#fff", weight: 2, fillColor: ACT_C.dark, fillOpacity: 0.9
        });
        const html =
            '<div style="font-family:inherit;min-width:200px;">' +
                '<div style="font-weight:800;color:' + ACT_C.dark + ';margin-bottom:6px;font-size:.9rem;">' + actEscape(p.opportunityName || "—") + '</div>' +
                '<div style="font-size:.82rem;line-height:1.9;">' +
                    '<div>تاريخ العقد: <strong>' + actFormatDate(p.contractDate) + '</strong></div>' +
                    '<div>الأجرة السنوية: <strong>' + actFormatNum(p.annualRent || 0) + '</strong></div>' +
                    '<div>الأنشطة: <strong>' + actEscape(p.activity || "—") + '</strong></div>' +
                    '<div>المنطقة: <strong>' + actEscape(p.region || "—") + '</strong></div>' +
                    '<div>نوع العقد: <strong>' + actEscape(p.contractType || "—") + '</strong></div>' +
                '</div>' +
            '</div>';
        marker.bindTooltip(html, { direction: "top", offset: [0, -6], opacity: 1, className: "act-map-tooltip" });
        actMarkerCluster.addLayer(marker);
    });

    actMap.addLayer(actMarkerCluster);
    requestAnimationFrame(function () { try { actMap.invalidateSize(); } catch (e) {} });
}

/* ==========================================================================
   رفع الملف
   ========================================================================== */
function actSetUploadStatus(text, isError) {
    const el = document.getElementById("actUploadStatus");
    if (!el) return;
    el.textContent = text || "";
    el.className = "tc-upload-status" + (isError ? " tc-upload-status-error" : text ? " tc-upload-status-ok" : "");
}

function actInitUpload() {
    const btn = document.getElementById("actUploadBtn");
    const input = document.getElementById("actUploadInput");
    if (!btn || !input) return;

    btn.addEventListener("click", function () { input.click(); });
    input.addEventListener("change", async function () {
        const file = input.files && input.files[0];
        if (!file) return;

        btn.disabled = true;
        actSetUploadStatus("جارِ رفع الملف...", false);
        try {
            const formData = new FormData();
            formData.append("activitiesFile", file);
            const resp = await fetch(window.ACT_UPLOAD_URL, { method: "POST", body: formData });
            const result = await resp.json();
            if (result.success) {
                actSetUploadStatus(result.message, false);
                actLoad();
            } else {
                actSetUploadStatus(result.message || "تعذّر رفع الملف.", true);
            }
        } catch (err) {
            console.error("تعذّر رفع ملف الأنشطة:", err);
            actSetUploadStatus("تعذّر رفع الملف، تحقق من الاتصال وحاول مجددًا.", true);
        } finally {
            btn.disabled = false;
            input.value = "";
        }
    });
}

/* ==========================================================================
   التحميل الرئيسي
   ========================================================================== */
async function actLoad() {
    const contentEl = document.getElementById("actContent");
    const noDataEl = document.getElementById("actNoDataBox");

    try {
        const resp = await fetch(window.ACT_DATA_URL + "?" + actQueryString());
        const data = await resp.json();

        actBuildFilterBar(data);

        if (!data.hasData) {
            if (contentEl) contentEl.style.display = "none";
            if (noDataEl) noDataEl.style.display = "";
            return;
        }

        if (contentEl) contentEl.style.display = "";
        if (noDataEl) noDataEl.style.display = "none";

        actDisposeCharts();
        ["actChartsRow1a", "actChartsRow2"].forEach(function (id) {
            const el = document.getElementById(id);
            if (el) el.innerHTML = "";
        });

        actRenderKpiCards(data);
        actRenderSectorCharts(data);
        actRenderYearChart(data);
        actRenderTopContractInsight(data);
        actRenderMap(data);
    } catch (err) {
        console.error("تعذّر تحميل بيانات الأنشطة الاستثمارية:", err);
        if (contentEl) contentEl.style.display = "none";
        if (noDataEl) noDataEl.style.display = "";
    }
}

document.addEventListener("DOMContentLoaded", function () {
    actInitUpload();
    if (window.ACT_DATA_URL) actLoad();
});
