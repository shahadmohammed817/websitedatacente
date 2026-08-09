/* ==========================================================================
   صفحة "مستهدفات وزارة المالية" — مستهدف سنوي فقط (بلا تفصيل شهري) لكل من
   الأمانة والبلديات (كنطاق واحد مجمّع)، مقابل المحصّل من البيانات المستوردة
   سابقًا. بطاقتا حلقات (Radial Progress بنفس أسلوب صفحة "متابعة الأداء")
   + رسمتا أعمدة أفقية (فعلي/مستهدف لكل بند رئيسي) - واحدة للأمانة وواحدة للبلديات.
   ========================================================================== */
const FT_C = {
    dark: "#0E6E61", mid: "#3F8F87", light: "#5FA89D", lime: "#8DC63F", limeDk: "#5F9E1F",
    red: "#D6534A", target: "#E0A930", text: "#173B36", muted: "#6E8A86", grid: "#E1ECEA",
    axisText: "#181818"
};
const FT_AXIS_LABEL_STYLE = { color: FT_C.axisText, fontSize: 13, fontWeight: 600 };

function ftAxisStyle(extra) {
    extra = extra || {};
    const merged = Object.assign({
        axisLine: { lineStyle: { color: FT_C.grid } },
        splitLine: { lineStyle: { color: FT_C.grid, type: "dashed" } }
    }, extra);
    merged.axisLabel = Object.assign({}, FT_AXIS_LABEL_STYLE, extra.axisLabel || {});
    return merged;
}

function ftTooltipStyle(extra) {
    return Object.assign({
        backgroundColor: "#FFFFFF", borderColor: FT_C.grid, borderWidth: 1,
        textStyle: { color: FT_C.text, fontSize: 12 },
        extraCssText: "box-shadow:0 4px 16px rgba(0,0,0,0.1);border-radius:10px;"
    }, extra || {});
}

function ftFormatNum(n) { return Number.isFinite(n) ? Math.round(n).toLocaleString("en-US") : "0"; }
function ftFormatCompact(n) {
    if (Math.abs(n) >= 1e9) return (n / 1e9).toFixed(1) + "B";
    if (Math.abs(n) >= 1e6) return (n / 1e6).toFixed(1) + "M";
    if (Math.abs(n) >= 1e3) return (n / 1e3).toFixed(1) + "K";
    return (n || 0).toFixed(0);
}
function ftClamp(n, min, max) { return Math.max(min, Math.min(max, n)); }
function ftEscape(str) {
    const div = document.createElement("div");
    div.textContent = (str === null || str === undefined) ? "" : String(str);
    return div.innerHTML;
}

const FT_SAR_SYMBOL = '<svg class="kpi-sar" viewBox="0 0 1124.14 1256.39" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-label="ريال سعودي">' +
    '<path d="M699.62,1113.02h0c-20.06,44.48-33.32,92.75-38.4,143.37l424.51-90.24c20.06-44.47,33.31-92.75,38.4-143.37l-424.51,90.24Z"/>' +
    '<path d="M1085.73,895.8c20.06-44.47,33.32-92.75,38.4-143.37l-330.68,70.33v-135.2l292.27-62.11c20.06-44.47,33.32-92.75,38.4-143.37l-330.68,70.27V66.13c-50.67,28.45-95.67,66.32-132.25,110.99v403.35l-132.25,28.11V0c-50.67,28.44-95.67,66.32-132.25,110.99v525.69l-295.91,62.88c-20.06,44.47-33.33,92.75-38.42,143.37l334.33-71.05v170.26l-358.3,76.14c-20.06,44.47-33.32,92.75-38.4,143.37l375.04-79.7c30.53-6.35,56.77-24.4,73.83-49.24l68.78-101.97v-.02c7.14-10.55,11.3-23.27,11.3-36.97v-149.98l132.25-28.11v270.4l424.53-90.28Z"/></svg>';
function ftSarVal(n) { return ftFormatNum(n) + ' <span class="value-sar">' + FT_SAR_SYMBOL + '</span>'; }

let ftCharts = [];
function ftDisposeCharts() {
    ftCharts.forEach(function (c) { try { c.dispose(); } catch (e) {} });
    ftCharts = [];
}

/* ==========================================================================
   حلقات Radial Progress (نفس أسلوب صفحة "متابعة الأداء")
   ========================================================================== */
function ftGaugeRingSvg(percent, colorFrom, colorTo, gradId, label) {
    const r = 85, c = 2 * Math.PI * r;
    const pct = ftClamp(percent, 0, 100);
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
            '<div class="tc-gauge-percent-label">' + ftEscape(label || "") + '</div>' +
        '</div>' +
    '</div>';
}

function ftGaugeCardHtml(opts) {
    const remaining = ftClamp(100 - opts.percent, 0, 100);
    return '<div class="tc-gauge-card" style="--tc-accent:' + opts.accent + ';--tc-icon-bg:' + opts.iconBg + ';--tc-glow:' + opts.glow + ';">' +
        '<div class="tc-gauge-header">' +
            '<div class="tc-gauge-icon"><i class="bi ' + opts.icon + '"></i></div>' +
            '<div>' +
                '<div class="tc-gauge-title">' + ftEscape(opts.title) + '</div>' +
                '<div class="tc-gauge-subtitle">' + ftEscape(opts.subtitle) + '</div>' +
            '</div>' +
        '</div>' +
        '<div class="tc-gauge-rings-row">' +
            ftGaugeRingSvg(opts.percent, opts.gradFrom, opts.gradTo, opts.gradId, "نسبة التحقيق") +
            ftGaugeRingSvg(remaining, FT_C.muted, FT_C.target, opts.gradId + "R", "المتبقي") +
        '</div>' +
        '<div class="tc-gauge-stats">' +
            '<div class="tc-gauge-stat"><div class="lbl">إجمالي المحصّل</div><div class="val">' + ftSarVal(opts.collected) + '</div></div>' +
            '<div class="tc-gauge-stat"><div class="lbl">إجمالي المستهدف</div><div class="val">' + ftSarVal(opts.target) + '</div></div>' +
        '</div>' +
    '</div>';
}

function ftRenderGauges(data) {
    const row = document.getElementById("ftGaugeRow");
    if (!row) return;

    row.innerHTML =
        ftGaugeCardHtml({
            accent: FT_C.dark, iconBg: "rgba(14,110,97,0.12)", glow: "rgba(14,110,97,0.18)",
            icon: "bi-bank", title: "الأمانة", subtitle: "المحصّل مقابل المستهدف السنوي",
            percent: data.amanahAchievementPercent || 0, gradFrom: FT_C.dark, gradTo: FT_C.mid, gradId: "ftGradAmanah",
            collected: data.amanahCollectedTotal || 0, target: data.amanahTargetTotal || 0
        }) +
        ftGaugeCardHtml({
            accent: FT_C.lime, iconBg: "rgba(141,198,63,0.14)", glow: "rgba(141,198,63,0.2)",
            icon: "bi-buildings", title: "البلديات", subtitle: "المحصّل مقابل المستهدف السنوي",
            percent: data.baladiyahAchievementPercent || 0, gradFrom: FT_C.limeDk, gradTo: FT_C.lime, gradId: "ftGradBaladiyah",
            collected: data.baladiyahCollectedTotal || 0, target: data.baladiyahTargetTotal || 0
        });

    requestAnimationFrame(function () {
        requestAnimationFrame(function () {
            row.querySelectorAll(".tc-gauge-progress").forEach(function (el) {
                el.style.strokeDashoffset = el.getAttribute("data-target-offset");
            });
        });
    });
}

// أسماء بعض البنود بملف "مستهدف المالية" طويلة وتحمل تفصيلاً فرعيًا (مثال: "إيرادات استثمار
// العقارات البلدية" يضم "العقود الاستثمارية" + "التأجير المؤقت")، لكن بهذه الرسمة يظهر البند
// باسمه العام المختصر فقط بلا تفصيل فرعي - عرض فقط، لا يغيّر التجميع الفعلي بالبيانات
const FT_ITEM_LABEL_SHORT = {
    "إيرادات استثمار العقارات البلدية": "استثمار"
};
function ftShortLabel(label) { return FT_ITEM_LABEL_SHORT[label] || label; }

/* ==========================================================================
   رسم أعمدة أفقية: محصّل/مستهدف لكل بند رئيسي
   ========================================================================== */
function ftBuildItemsHorizontalBarOption(items, actualColor) {
    const categories = items.map(function (it) { return ftShortLabel(it.itemLabel); });
    const actuals = items.map(function (it) { return it.collected; });
    const targets = items.map(function (it) { return it.target; });
    const pcts = items.map(function (it) { return it.target > 0 ? Math.round(it.collected / it.target * 1000) / 10 : null; });

    return {
        tooltip: ftTooltipStyle({
            trigger: "axis", axisPointer: { type: "shadow" },
            formatter: function (params) {
                var i = params[0].dataIndex;
                var lines = '<div style="font-weight:700;margin-bottom:6px;">' + ftEscape(params[0].axisValue) + '</div>';
                params.forEach(function (p) {
                    lines += '<div>' + ftEscape(p.seriesName) + ': <strong>' + ftFormatNum(p.value) + '</strong></div>';
                });
                lines += '<div style="margin-top:4px;">نسبة التحقيق: <strong>' +
                    (pcts[i] === null ? "لا يوجد مستهدف لهذا البند" : pcts[i].toFixed(1) + "%") + '</strong></div>';
                return lines;
            }
        }),
        legend: { top: 0, right: 8, textStyle: Object.assign({}, FT_AXIS_LABEL_STYLE), itemWidth: 12, itemHeight: 10 },
        grid: { left: 130, right: 90, top: 40, bottom: 20 },
        xAxis: ftAxisStyle({ type: "value", axisLabel: { show: false }, splitLine: { show: false } }),
        yAxis: ftAxisStyle({
            type: "category", data: categories,
            axisLabel: { fontSize: 12, width: 120, overflow: "break" },
            axisTick: { alignWithLabel: true }
        }),
        series: [
            {
                name: "المحصّل", type: "bar", data: actuals, barMaxWidth: 18, barGap: "30%", barCategoryGap: "45%", z: 1,
                itemStyle: { color: actualColor, borderRadius: [0, 4, 4, 0] },
                label: {
                    show: true, position: "right", color: actualColor, fontWeight: 700, fontSize: 11,
                    formatter: function (p) {
                        var pct = pcts[p.dataIndex];
                        return ftFormatCompact(p.value) + (pct !== null ? " (" + pct.toFixed(0) + "%)" : "");
                    }
                }
            },
            {
                name: "المستهدف", type: "bar", data: targets, barMaxWidth: 18, barCategoryGap: "45%", z: 1,
                itemStyle: { color: FT_C.target, opacity: 0.75, borderRadius: [0, 4, 4, 0] },
                label: {
                    show: true, position: "right", color: FT_C.target, fontWeight: 700, fontSize: 11,
                    formatter: function (p) { return p.value > 0 ? ftFormatCompact(p.value) : ""; }
                }
            }
        ]
    };
}

function ftRenderChart(rowId, containerId, title, badge, option, extraClass) {
    const row = document.getElementById(rowId);
    if (!row) return;

    const id = containerId + "_" + Date.now() + "_" + Math.floor(Math.random() * 1000);
    const div = document.createElement("div");
    div.className = "chart-card" + (extraClass ? " " + extraClass : "");
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">' + ftEscape(title) + '</div>' +
            '<div class="chart-badge">' + ftEscape(badge) + '</div>' +
        '</div>' +
        '<div class="echart-box" id="' + id + '"></div>';
    row.appendChild(div);

    const chart = echarts.init(document.getElementById(id));
    chart.setOption(option);
    ftCharts.push(chart);
}

/* ==========================================================================
   التحميل الرئيسي
   ========================================================================== */
async function ftLoad() {
    const contentEl = document.getElementById("ftContent");
    const noDataEl = document.getElementById("ftNoDataBox");

    try {
        const resp = await fetch(window.FT_DATA_URL);
        if (!resp.ok) throw new Error("network");
        const data = await resp.json();

        ftDisposeCharts();
        const row = document.getElementById("ftItemsRow");
        if (row) row.innerHTML = "";

        if (!data.hasData) {
            if (contentEl) contentEl.style.display = "none";
            if (noDataEl) noDataEl.style.display = "";
            return;
        }

        if (contentEl) contentEl.style.display = "";
        if (noDataEl) noDataEl.style.display = "none";

        ftRenderGauges(data);

        ftRenderChart(
            "ftItemsRow", "ftAmanahItemsChart",
            "بنود الأمانة - المحصّل مقابل المستهدف السنوي",
            "الأمانة",
            ftBuildItemsHorizontalBarOption(data.amanahItems || [], FT_C.dark),
            "ft-items-card"
        );
        ftRenderChart(
            "ftItemsRow", "ftBaladiyahItemsChart",
            "بنود البلديات - المحصّل مقابل المستهدف السنوي",
            "البلديات",
            ftBuildItemsHorizontalBarOption(data.baladiyahItems || [], FT_C.lime),
            "ft-items-card"
        );
    } catch (err) {
        console.error("تعذر تحميل بيانات مستهدفات وزارة المالية:", err);
        if (contentEl) contentEl.style.display = "none";
        if (noDataEl) noDataEl.style.display = "";
    }
}

/* ==========================================================================
   رفع ملف "مستهدف المالية"
   ========================================================================== */
function ftSetUploadStatus(text, isError) {
    const el = document.getElementById("ftUploadStatus");
    if (!el) return;
    el.textContent = text || "";
    el.className = "tc-upload-status" + (isError ? " tc-upload-status-error" : text ? " tc-upload-status-ok" : "");
}

function ftInitUpload() {
    const btn = document.getElementById("ftUploadBtn");
    const input = document.getElementById("ftUploadInput");
    if (!btn || !input || !window.FT_UPLOAD_URL) return;

    btn.addEventListener("click", function () { input.click(); });

    input.addEventListener("change", async function () {
        const file = input.files && input.files[0];
        if (!file) return;

        btn.disabled = true;
        ftSetUploadStatus("جارِ رفع الملف...", false);

        try {
            const formData = new FormData();
            formData.append("financeTargetFile", file);

            const resp = await fetch(window.FT_UPLOAD_URL, { method: "POST", body: formData });
            const result = await resp.json();

            if (result.success) {
                ftSetUploadStatus(result.message, false);
                ftLoad();
            } else {
                ftSetUploadStatus(result.message || "تعذّر رفع الملف.", true);
            }
        } catch (err) {
            console.error("تعذّر رفع ملف مستهدف المالية:", err);
            ftSetUploadStatus("تعذّر رفع الملف، تحقق من الاتصال وحاول مجددًا.", true);
        } finally {
            btn.disabled = false;
            input.value = "";
        }
    });
}

document.addEventListener("DOMContentLoaded", function () {
    if (!window.FT_DATA_URL) return;

    ftLoad();
    ftInitUpload();

    window.addEventListener("resize", function () {
        ftCharts.forEach(function (c) { try { c.resize(); } catch (e) {} });
    });
});
