/* ==========================================================================
   صفحة "الملخص التنفيذي" - تصميم صف-وراء-صف ثابت (بلا أكورديون):
     شريط فلاتر (بند رئيسي/فرعي + مصدر مستهدف + سنة - يشتغل على صف 2 فقط)
     صف 1: الفرص/العقود المطروحة 2026 (كروت + دونات حسب النوع + بار قيمة/نسبة إبرام)
     صف 2: تحقيق المستهدف (Gauge + كروت مستهدف/محصل/مفوتر) وجنبه كروت نمو الإيرادات
     صف 3: المديونيات لجميع البنود (دونات + كرتا عدد/قيمة + بار حسب البند)
     صف 4: المديونيات والمطالبات (عمود معالج/متبقٍ + دونات نسبة متبقية + كرتا قيمة/عدد)
     صف 5: الإدارات العامة (دونات نسبة معالجة لكل إدارة + بار الإدارات الفرعية عند الضغط)
   ========================================================================== */
const EXS_C = {
    dark: "#0E6E61", mid: "#3F8F87", light: "#5FA89D", lime: "#8DC63F", limeDk: "#5F9E1F",
    red: "#D6534A", warn: "#E0A930", text: "#173B36", muted: "#6E8A86", grid: "#E1ECEA", axisText: "#181818"
};
const EXS_PALETTE = [EXS_C.dark, EXS_C.mid, EXS_C.lime, EXS_C.light, EXS_C.limeDk];

function exsColorForName(name) {
    var h = 0;
    for (var i = 0; i < (name || "").length; i++) h = (h * 31 + name.charCodeAt(i)) | 0;
    return EXS_PALETTE[Math.abs(h) % EXS_PALETTE.length];
}

function exsFormatNum(n) { return Number.isFinite(n) ? Math.round(n).toLocaleString("en-US") : "0"; }
// صيغة مختصرة (ألف/مليون/مليار) - تُستخدم فقط بنص وسط الدونات الضيقة (المساحة لا تكفي لرقم
// كامل)، بينما تبقى الأرقام الكاملة بالقوائم/التلميحات/الكروت المجاورة
function exsFormatCompact(n) {
    if (!Number.isFinite(n)) return "0";
    var abs = Math.abs(n);
    if (abs >= 1e9) return (n / 1e9).toFixed(1) + " مليار";
    if (abs >= 1e6) return (n / 1e6).toFixed(1) + " م";
    if (abs >= 1e3) return (n / 1e3).toFixed(1) + " ألف";
    return exsFormatNum(n);
}
function exsEscape(str) {
    const div = document.createElement("div");
    div.textContent = (str === null || str === undefined) ? "" : String(str);
    return div.innerHTML;
}
function exsAxisStyle(extra) {
    extra = extra || {};
    const merged = Object.assign({
        axisLine: { lineStyle: { color: EXS_C.grid } },
        splitLine: { lineStyle: { color: EXS_C.grid, type: "dashed" } }
    }, extra);
    merged.axisLabel = Object.assign({ color: EXS_C.axisText, fontSize: 12, fontWeight: 600 }, extra.axisLabel || {});
    return merged;
}
function exsTooltip(extra) {
    return Object.assign({
        backgroundColor: "#FFFFFF", borderColor: EXS_C.grid, borderWidth: 1,
        textStyle: { color: EXS_C.text, fontSize: 12 },
        extraCssText: "box-shadow:0 4px 16px rgba(0,0,0,0.1);border-radius:10px;max-width:320px;white-space:normal;"
    }, extra || {});
}

// رمز الريال السعودي الرسمي - نفس SVG المستخدم بكل صفحات الموقع
function exsSarSvg(cls) {
    return '<svg class="' + cls + '" viewBox="0 0 1124.14 1256.39" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-label="ريال سعودي">' +
        '<path d="M699.62,1113.02h0c-20.06,44.48-33.32,92.75-38.4,143.37l424.51-90.24c20.06-44.47,33.31-92.75,38.4-143.37l-424.51,90.24Z"/>' +
        '<path d="M1085.73,895.8c20.06-44.47,33.32-92.75,38.4-143.37l-330.68,70.33v-135.2l292.27-62.11c20.06-44.47,33.32-92.75,38.4-143.37l-330.68,70.27V66.13c-50.67,28.45-95.67,66.32-132.25,110.99v403.35l-132.25,28.11V0c-50.67,28.44-95.67,66.32-132.25,110.99v525.69l-295.91,62.88c-20.06,44.47-33.33,92.75-38.42,143.37l334.33-71.05v170.26l-358.3,76.14c-20.06,44.47-33.32,92.75-38.4,143.37l375.04-79.7c30.53-6.35,56.77-24.4,73.83-49.24l68.78-101.97v-.02c7.14-10.55,11.3-23.27,11.3-36.97v-149.98l132.25-28.11v270.4l424.53-90.28Z"/></svg>';
}
function exsSarVal(n) { return exsFormatNum(n) + ' <span class="value-sar">' + exsSarSvg("kpi-sar") + '</span>'; }

let exsCharts = [];

function exsRenderChart(rowId, title, badge, option, heightPx) {
    const row = document.getElementById(rowId);
    if (!row) return null;
    const id = rowId + "_" + Date.now() + "_" + Math.floor(Math.random() * 1000);
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">' + exsEscape(title) + '</div>' +
            (badge ? '<div class="chart-badge">' + exsEscape(badge) + '</div>' : '') +
        '</div>' +
        '<div class="echart-box" id="' + id + '" style="height:' + (heightPx || 300) + 'px;"></div>';
    row.appendChild(div);
    const chart = echarts.init(document.getElementById(id));
    chart.setOption(option);
    exsCharts.push(chart);
    requestAnimationFrame(function () { try { chart.resize(); } catch (e) {} });
    return chart;
}

/* ==========================================================================
   كروت KPI عامة (نفس تخطيط .kpi-card بكل الصفحات)
   ========================================================================== */
function exsKpiCardHtml(cls, icon, label, value, sub) {
    return '<div class="kpi-card ' + cls + '">' +
        '<div class="icon"><i class="bi ' + icon + '"></i></div>' +
        '<div class="label">' + exsEscape(label) + '</div>' +
        '<div class="value">' + value + '</div>' +
        (sub ? '<div style="font-size:.78rem;font-weight:700;color:var(--muted);margin-top:4px;">' + exsEscape(sub) + '</div>' : '') +
    '</div>';
}
function exsPercentCardHtml(cls, icon, label, percent) {
    return '<div class="kpi-card ' + cls + '">' +
        '<div class="icon"><i class="bi ' + icon + '"></i></div>' +
        '<div class="label">' + exsEscape(label) + '</div>' +
        '<div class="value">' + (percent || 0).toFixed(1) + '%</div>' +
    '</div>';
}
// بطاقة إحصاء أفقية مصغّرة (أيقونة يمين + تسمية/قيمة يسار) - نفس حجم وشكل بطاقات "المحصل
// الحالي/السابق ونسبة النمو" أعلى رسم "أداء الإيرادات" بالصفحة الرئيسية (.rp-growth-*
// الجاهزة بـ site.css) بدل .kpi-card الأكبر حجمًا - تُستخدم بكل كروت الإحصاء بهذه الصفحة
function exsRpGrowthCardHtml(icon, label, value, colorClass) {
    return '<div class="rp-growth-card' + (colorClass ? ' ' + colorClass : '') + '">' +
        '<div class="rp-growth-icon"><i class="bi ' + icon + '"></i></div>' +
        '<div><div class="rp-growth-label">' + exsEscape(label) + '</div><div class="rp-growth-value">' + value + '</div></div>' +
    '</div>';
}

/* ==========================================================================
   دونات بلا Legend داخلي (المجموع بنص الدائرة) + قائمة اسم/لون/قيمة على جنبها
   (.ai-donut-split/.ai-donut-list الجاهزة) - تُستخدم لتوزيع الفرص حسب النوع والمديونيات حسب البند
   ========================================================================== */
function exsBuildPlainDonutOption(items, total, centerLabel, tooltipUnit) {
    tooltipUnit = tooltipUnit || "القيمة";
    const data = items.map(function (r) {
        return { name: r.name, value: r.value, itemStyle: { color: exsColorForName(r.name) } };
    });
    return {
        tooltip: exsTooltip({
            trigger: "item",
            formatter: function (p) {
                var pct = total > 0 ? Math.round((p.value / total) * 1000) / 10 : 0;
                return '<div style="font-weight:700;">' + exsEscape(p.name) + '</div><div>' + tooltipUnit + ': <strong>' + exsFormatNum(p.value) + '</strong> (' + pct + '%)</div>';
            }
        }),
        series: [{
            type: "pie", radius: ["46%", "76%"], center: ["50%", "50%"],
            itemStyle: { borderColor: "#fff", borderWidth: 2 },
            label: { show: false }, labelLine: { show: false },
            data: data
        }],
        graphic: [
            { type: "text", left: "center", top: "39%", style: { text: exsFormatCompact(total), fontSize: 18, fontWeight: 800, fill: EXS_C.text, fontFamily: "Cairo", textAlign: "center" } },
            { type: "text", left: "center", top: "55%", style: { text: centerLabel || "الإجمالي", fontSize: 10, fontWeight: 700, fill: EXS_C.muted, fontFamily: "Cairo", textAlign: "center" } }
        ]
    };
}

function exsAiDonutListHtml(rows, valueSuffix) {
    valueSuffix = valueSuffix === undefined ? "" : valueSuffix;
    return rows.map(function (r) {
        var color = exsColorForName(r.name);
        return '<div class="ai-donut-list-item">' +
            '<span class="ai-donut-dot" style="background:' + color + ';"></span>' +
            '<span class="ai-donut-list-name">' + exsEscape(r.name) + '</span>' +
            '<span class="ai-donut-list-count">' + exsFormatNum(r.value) + valueSuffix + '</span>' +
        '</div>';
    }).join("");
}

function exsRenderDonutWithList(rowId, title, badge, rows, total, centerLabel, tooltipUnit, valueSuffix) {
    const row = document.getElementById(rowId);
    if (!row) return null;
    const chartId = rowId + "_donut_" + Date.now() + "_" + Math.floor(Math.random() * 1000);
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">' + exsEscape(title) + '</div>' +
            (badge ? '<div class="chart-badge">' + exsEscape(badge) + '</div>' : '') +
        '</div>' +
        '<div class="ai-donut-split">' +
            '<div class="echart-box" id="' + chartId + '" style="height:230px;"></div>' +
            '<div class="ai-donut-list">' + exsAiDonutListHtml(rows, valueSuffix) + '</div>' +
        '</div>';
    row.appendChild(div);
    const chart = echarts.init(document.getElementById(chartId));
    chart.setOption(exsBuildPlainDonutOption(rows, total, centerLabel, tooltipUnit));
    exsCharts.push(chart);
    requestAnimationFrame(function () { try { chart.resize(); } catch (e) {} });
    return chart;
}

/* ==========================================================================
   Gauge نصف دائري (عقارب/تارقت حقيقي عبر echarts) - نسبة تحقيق المستهدف
   ========================================================================== */
function exsBuildTargetGaugeOption(percent) {
    const p = Math.max(0, Math.min(100, percent || 0));
    return {
        series: [{
            type: "gauge",
            startAngle: 210, endAngle: -30,
            min: 0, max: 100,
            radius: "92%", center: ["50%", "62%"],
            progress: { show: true, width: 18, itemStyle: { color: EXS_C.dark } },
            axisLine: { lineStyle: { width: 18, color: [[1, EXS_C.grid]] } },
            axisTick: { distance: -18, length: 6, lineStyle: { color: "#fff", width: 1 } },
            splitLine: { distance: -22, length: 12, lineStyle: { width: 2, color: "#fff" } },
            axisLabel: { distance: -30, color: EXS_C.muted, fontSize: 11, formatter: "{value}%" },
            pointer: { icon: "path://M2,0 L-2,0 L-4,-92 L0,-100 L4,-92 Z", length: "58%", width: 7, itemStyle: { color: EXS_C.dark } },
            anchor: { show: true, showAbove: true, size: 12, itemStyle: { borderWidth: 3, borderColor: EXS_C.dark, color: "#fff" } },
            title: { show: true, offsetCenter: [0, "50%"], fontSize: 11, fontWeight: 700, color: EXS_C.muted, fontFamily: "Cairo" },
            detail: {
                valueAnimation: true, fontSize: 26, fontWeight: 800, color: EXS_C.text, fontFamily: "Cairo",
                offsetCenter: [0, "72%"], formatter: "{value}%"
            },
            data: [{ value: p, name: "نسبة التحصيل" }]
        }]
    };
}

/* ==========================================================================
   صف 1: الفرص/العقود المطروحة 2026 - كروت + دونات "عدد الفرص حسب النوع" (مع قيمة العقود
   المبرومة بجانبها) + لوحة "عدد الفرص ونسبة الإبرام حسب النوع" (عمود لكل نوع + حلقة نسبة
   الإبرام فوقه) + بار "قيمة العقود المبرومة حسب الإدارة العامة" - كل ذلك 2026 فقط بطلب صريح
   من المستخدمة، بلا فلاتر (نفس منطق صفحة "الطرح")
   ========================================================================== */
// اسم النوع فوق قيمته (بدل جنبه بنفس السطر) - العمود ضيّق (ثلث الصف) فلا تكفي المساحة لعرض
// الاسم والقيمة الطويلة بسطر واحد بلا اختفاء أحدهما
function exsTarhDonutListHtml(rows) {
    return rows.map(function (r) {
        var color = exsColorForName(r.typeLabel);
        return '<div class="ai-donut-list-item" style="align-items:flex-start;">' +
            '<span class="ai-donut-dot" style="margin-top:3px;background:' + color + ';"></span>' +
            '<span style="display:flex;flex-direction:column;gap:1px;min-width:0;">' +
                '<span class="ai-donut-list-name" style="white-space:normal;">' + exsEscape(r.typeLabel) + '</span>' +
                '<span class="ai-donut-list-count" style="font-size:.72rem;">' + exsFormatNum(r.finalizedCount) + ' عقد • ' + exsFormatNum(r.finalizedValue) + ' ر.س</span>' +
            '</span>' +
        '</div>';
    }).join("");
}

// دونات "قيمة العقود المبرومة حسب النوع" - القيمة (حجم القطعة) + العدد (بالقائمة والتلميح)، صف 1 (2026 فقط)
function exsRenderTarhDonut(rowId, tendered) {
    const row = document.getElementById(rowId);
    if (!row) return;
    const donutRows = tendered.map(function (r) { return { name: r.typeLabel, value: r.finalizedValue }; });
    const total = donutRows.reduce(function (s, r) { return s + r.value; }, 0);
    const chartId = rowId + "_tarhDonut_" + Date.now();
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header"><div class="chart-title">قيمة العقود المبرومة 2026 حسب النوع</div></div>' +
        '<div class="ai-donut-split">' +
            '<div class="echart-box" id="' + chartId + '" style="height:230px;"></div>' +
            '<div class="ai-donut-list">' + exsTarhDonutListHtml(tendered) + '</div>' +
        '</div>';
    row.appendChild(div);
    const chart = echarts.init(document.getElementById(chartId));
    const donutOption = exsBuildPlainDonutOption(donutRows, total, "إجمالي القيمة", "القيمة");
    // tooltip يعرض القيمة (والنسبة) والعدد معًا - بطلب المستخدمة
    donutOption.tooltip.formatter = function (p) {
        var trow = tendered.filter(function (t) { return t.typeLabel === p.name; })[0];
        var pct = total > 0 ? Math.round((p.value / total) * 1000) / 10 : 0;
        return '<div style="font-weight:700;">' + exsEscape(p.name) + '</div>' +
            '<div>القيمة: <strong>' + exsFormatNum(p.value) + '</strong> (' + pct + '%)</div>' +
            (trow ? '<div>عدد العقود: <strong>' + exsFormatNum(trow.finalizedCount) + '</strong></div>' : '');
    };
    chart.setOption(donutOption);
    exsCharts.push(chart);
    requestAnimationFrame(function () { try { chart.resize(); } catch (e) {} });
}

// "العقود المبرومة 2026 حسب النوع" - عمود واحد لكل نوع، عدده فوقه (بطلب المستخدمة: بلا
// عمود الفرص المطروحة وبلا نسبة إبرام)
function exsBuildTypeValueRateOption(rows) {
    const labels = rows.map(function (r) { return r.typeLabel; });
    const finalized = rows.map(function (r) { return r.finalizedCount; });
    const maxVal = Math.max.apply(null, finalized) || 1;

    return {
        tooltip: exsTooltip({
            trigger: "axis", axisPointer: { type: "shadow" },
            formatter: function (params) {
                const i = params[0].dataIndex;
                return '<div style="font-weight:700;margin-bottom:4px;">' + exsEscape(labels[i]) + '</div>' +
                    '<div>العقود المبرومة 2026: <strong>' + exsFormatNum(finalized[i]) + '</strong></div>';
            }
        }),
        grid: { left: 10, right: 20, top: 20, bottom: 10, containLabel: true },
        xAxis: exsAxisStyle({ type: "category", data: labels, axisLabel: { interval: 0, fontSize: 12, width: 90, overflow: "break" } }),
        yAxis: exsAxisStyle({ type: "value", max: Math.ceil(maxVal * 1.15) }),
        series: [
            {
                name: "العقود المبرومة 2026", type: "bar", barMaxWidth: 34,
                itemStyle: { color: EXS_C.dark, borderRadius: [4, 4, 0, 0] },
                label: { show: true, position: "top", formatter: function (p) { return exsFormatNum(p.value); }, color: EXS_C.axisText, fontWeight: 700, fontSize: 11 },
                data: finalized
            }
        ]
    };
}

function exsBuildDeptFinalizedValueOption(rows, total) {
    const labels = rows.map(function (r) { return r.departmentLabel; });
    const values = rows.map(function (r) { return r.totalValue; });
    return {
        tooltip: exsTooltip({
            trigger: "axis", axisPointer: { type: "shadow" },
            formatter: function (params) {
                const i = params[0].dataIndex;
                const pct = total > 0 ? Math.round((values[i] / total) * 1000) / 10 : 0;
                return '<div style="font-weight:700;margin-bottom:4px;">' + exsEscape(labels[i]) + '</div>' +
                    '<div>القيمة: <strong>' + exsFormatNum(values[i]) + '</strong> (' + pct + '%)</div>';
            }
        }),
        grid: { left: 10, right: 20, top: 10, bottom: 10, containLabel: true },
        xAxis: exsAxisStyle({ type: "value", axisLabel: { formatter: function (v) { return (v / 1e6).toFixed(0) + "م"; } } }),
        yAxis: exsAxisStyle({ type: "category", data: labels, axisLabel: { width: 210, overflow: "break", fontSize: 10.5, lineHeight: 13 } }),
        series: [{
            type: "bar", barMaxWidth: 22,
            data: values.map(function (v, i) { return { value: v, itemStyle: { color: EXS_PALETTE[i % EXS_PALETTE.length], borderRadius: [0, 4, 4, 0] } }; }),
            label: { show: true, position: "right", color: EXS_C.axisText, fontWeight: 700, fontSize: 11, formatter: function (p) { return exsFormatNum(p.value); } }
        }]
    };
}

function exsRenderTarhRow(data) {
    const kpiWrap = document.getElementById("exsRow1Kpis");
    const chartsWrap = document.getElementById("exsRow1Charts");
    const noData = document.getElementById("exsRow1NoData");

    if (!data.tarhHasData) {
        if (kpiWrap) kpiWrap.innerHTML = "";
        if (chartsWrap) chartsWrap.innerHTML = "";
        if (noData) noData.style.display = "";
        return;
    }
    if (noData) noData.style.display = "none";

    const finalizedValue = (data.tarhTypeSummary || []).reduce(function (s, r) { return s + r.totalValue; }, 0);

    if (kpiWrap) {
        kpiWrap.innerHTML =
            exsKpiCardHtml("c1", "bi-cash-stack", "إجمالي القيمة", exsSarVal(data.tarhTotalOpportunitiesValue), "عدد الفرص: " + exsFormatNum(data.tarhTotalOpportunities)) +
            exsKpiCardHtml("c4", "bi-file-earmark-check", "إجمالي العقود المبرومة", exsSarVal(finalizedValue), exsFormatNum(data.tarhFinalizedCount) + " عقد") +
            exsPercentCardHtml("c2", "bi-graph-up-arrow", "نسبة الإبرام", data.tarhFinalizedPercent);
    }

    if (chartsWrap) {
        chartsWrap.innerHTML = "";
        const tendered = data.tarhTypeTenderedSummary || [];
        if (tendered.length) {
            exsRenderTarhDonut("exsRow1Charts", tendered);
        }
        const byDept = data.tarhFinalizedByDepartment || [];
        if (byDept.length) {
            const deptTotal = byDept.reduce(function (s, r) { return s + r.totalValue; }, 0);
            exsRenderChart("exsRow1Charts", "قيمة العقود المبرومة حسب الإدارة العامة (2026)", null, exsBuildDeptFinalizedValueOption(byDept, deptTotal), 300);
        }
    }
}

/* ==========================================================================
   صف 2: تحقيق المستهدف (يسار، مُفلتر بشريط الفلاتر) + نمو الإيرادات (يمين، بلا فلاتر)
   ========================================================================== */
let exsGaugeChart = null;

// أزرار "مصدر المستهدف" (وزارة المالية / وزارة البلديات والإسكان) داخل بطاقة "نسبة تحقيق
// المستهدف" فوق الدائرة مباشرة - نفس شكل زر الصفحة الرئيسية، بلا خيار "الكل"
function exsSourceToggleHtml() {
    return '<div class="rp-source-toggle" style="margin:2px 0 10px;">' +
        '<span class="rp-source-label"><i class="bi bi-flag"></i> مصدر المستهدف</span>' +
        '<div class="rp-toggle-group">' +
            '<button type="button" data-src="Finance" class="rp-toggle-btn' + (exsTargetSource === "Finance" ? " active" : "") + '">وزارة المالية</button>' +
            '<button type="button" data-src="BaladiyahHousing" class="rp-toggle-btn' + (exsTargetSource === "BaladiyahHousing" ? " active" : "") + '">وزارة البلديات والإسكان</button>' +
        '</div>' +
    '</div>';
}
function exsWireSourceToggle(card) {
    card.querySelectorAll(".rp-toggle-btn[data-src]").forEach(function (btn) {
        btn.addEventListener("click", function () {
            const val = btn.getAttribute("data-src");
            if (exsTargetSource === val) return;
            exsTargetSource = val;
            exsLoadTargetAchievement();
        });
    });
}

function exsRenderTargetAchievement(data) {
    const card = document.getElementById("exsRow2GaugeCard");
    if (!card) return;
    exsGaugeChart = null;
    if (!data || !data.hasData) {
        card.innerHTML =
            '<div class="chart-header"><div class="chart-title">نسبة تحقيق المستهدف</div></div>' +
            exsSourceToggleHtml() +
            '<div class="no-data-box" style="padding:24px 10px;"><div class="nd-icon"><i class="bi bi-bullseye"></i></div>لا توجد بيانات مستهدفات لهذا البند</div>';
        exsWireSourceToggle(card);
        return;
    }
    card.innerHTML =
        '<div class="chart-header"><div class="chart-title">نسبة تحقيق المستهدف</div></div>' +
        exsSourceToggleHtml() +
        '<div class="echart-box" id="exsTargetGaugeBox" style="height:220px;"></div>' +
        '<div class="rp-growth-cards" style="margin-top:6px;">' +
            exsRpGrowthCardHtml("bi-bullseye", "المستهدف", exsSarVal(data.target)) +
            exsRpGrowthCardHtml("bi-cash-stack", "المحصل", exsSarVal(data.collected), "rp-growth-up") +
            exsRpGrowthCardHtml("bi-receipt", "المفوتر", exsSarVal(data.invoiced)) +
        '</div>';
    exsWireSourceToggle(card);
    const box = document.getElementById("exsTargetGaugeBox");
    if (box) {
        exsGaugeChart = echarts.init(box);
        exsGaugeChart.setOption(exsBuildTargetGaugeOption(data.achievementPercent));
        exsCharts.push(exsGaugeChart);
        requestAnimationFrame(function () { try { exsGaugeChart.resize(); } catch (e) {} });
    }
}

function exsPeriodCardHtml(color, icon, label, value, isPercent) {
    const valueText = isPercent ? (value > 0 ? "+" : "") + value.toFixed(1) + "%" : exsFormatNum(value) + " ر.س";
    return '<div class="kpi-card" style="margin:0;flex:1 1 0;min-width:130px;">' +
        '<div class="icon" style="background:' + color + '22;color:' + color + ';"><i class="bi ' + icon + '"></i></div>' +
        '<div class="label">' + exsEscape(label) + '</div>' +
        '<div class="value" style="font-size:1.1rem;">' + valueText + '</div>' +
    '</div>';
}

function exsRenderRevenueGrowthPeriodCard(data) {
    const card = document.getElementById("exsRow2PeriodCard");
    if (!card) return;
    if (!data.revenueCurrentYear || !data.revenuePreviousYear) { card.innerHTML = ""; return; }
    const isPositive = data.revenueGrowthPercent >= 0;
    card.innerHTML =
        '<div class="chart-header"><div class="chart-title">نمو الإيرادات بين السنتين</div></div>' +
        '<div style="display:flex;gap:10px;padding:4px 2px;flex-wrap:wrap;">' +
            exsPeriodCardHtml(EXS_C.mid, "bi-calendar-check", "المحصل " + data.revenueCurrentYear, data.revenueCurrentYearCollected) +
            exsPeriodCardHtml(EXS_C.dark, "bi-calendar3", "المحصل " + data.revenuePreviousYear, data.revenuePreviousYearCollected) +
            exsPeriodCardHtml(isPositive ? EXS_C.lime : EXS_C.red, isPositive ? "bi-graph-up-arrow" : "bi-graph-down-arrow", "نسبة نمو الإيرادات", data.revenueGrowthPercent, true) +
        '</div>';
}

/* ==========================================================================
   شريط فلاتر صف 2 (بند رئيسي/فرعي + مصدر مستهدف + سنة)
   ========================================================================== */
let exsTargetFilterOptions = { mainItems: [], subItemsByMainItem: {}, years: [] };

function exsUpdateSubItemOptions(mainItem) {
    const subSel = document.getElementById("exsSubItemFilter");
    if (!subSel) return;
    const subs = (exsTargetFilterOptions.subItemsByMainItem && exsTargetFilterOptions.subItemsByMainItem[mainItem]) || [];
    subSel.innerHTML = '<option value="">كل البنود الفرعية</option>' +
        subs.map(function (s) { return '<option value="' + exsEscape(s) + '">' + exsEscape(s) + '</option>'; }).join("");
    subSel.disabled = subs.length === 0;
}

// البند الرئيسي/الفرعي مصدرهما جدول الأقسام (Sections) نفسه المستخدَم بشجرة فلتر الصفحة
// الرئيسية - يعتمد على النطاق المختار (أمانة/بلديات/الكل)، لذا يُعاد تحميله كل مرة يتغيّر
// فيها فلتر النطاق (بخلاف مصدر المستهدف/السنة الثابتَين)
async function exsLoadTargetFilterOptions() {
    const scopeSel = document.getElementById("exsScopeFilter");
    const scope = scopeSel ? scopeSel.value : "";
    try {
        const resp = await fetch(window.EXEC_TARGET_FILTER_OPTIONS_URL + "?scope=" + encodeURIComponent(scope));
        exsTargetFilterOptions = await resp.json();
    } catch (err) {
        console.error("تعذّر تحميل قوائم فلاتر المستهدف:", err);
        return;
    }
    const mainSel = document.getElementById("exsMainItemFilter");
    if (mainSel) {
        mainSel.innerHTML = '<option value="">كل البنود الرئيسية</option>' +
            (exsTargetFilterOptions.mainItems || []).map(function (m) { return '<option value="' + exsEscape(m) + '">' + exsEscape(m) + '</option>'; }).join("");
    }
    exsUpdateSubItemOptions("");
}

// مصدر المستهدف: أزرار تبديل فوق شكل "نسبة تحقيق المستهدف" (بدل قائمة منسدلة) - نفس شكل
// زر "مصدر المستهدف" برسم "أداء الإيرادات" بالصفحة الرئيسية
let exsTargetSource = "Finance";

async function exsLoadTargetAchievement() {
    const scope = document.getElementById("exsScopeFilter");
    const mainItem = document.getElementById("exsMainItemFilter");
    const subItem = document.getElementById("exsSubItemFilter");
    const params = new URLSearchParams();
    if (scope && scope.value) params.set("scope", scope.value);
    if (mainItem && mainItem.value) params.set("mainItem", mainItem.value);
    if (subItem && subItem.value) params.set("subItem", subItem.value);
    if (exsTargetSource && exsTargetSource !== "All") params.set("targetSource", exsTargetSource);
    try {
        const resp = await fetch(window.EXEC_TARGET_ACHIEVEMENT_URL + "?" + params.toString());
        exsRenderTargetAchievement(await resp.json());
    } catch (err) {
        console.error("تعذّر تحميل بيانات تحقيق المستهدف:", err);
    }
}

function exsInitFilterBar() {
    const scopeSel = document.getElementById("exsScopeFilter");
    const mainSel = document.getElementById("exsMainItemFilter");
    const subSel = document.getElementById("exsSubItemFilter");
    if (scopeSel) scopeSel.addEventListener("change", function () {
        exsLoadTargetFilterOptions().then(exsLoadTargetAchievement);
    });
    if (mainSel) mainSel.addEventListener("change", function () { exsUpdateSubItemOptions(mainSel.value); exsLoadTargetAchievement(); });
    if (subSel) subSel.addEventListener("change", exsLoadTargetAchievement);
}

/* ==========================================================================
   صف 3: المديونيات لجميع البنود - بطاقة "توزيع المديونيات حسب البند" (دونات + قائمة أسماء
   البنود وقيمها، وتحتها كرتا عدد/قيمة المديونيات)، وبطاقة مجاورة تعرض البنود الفرعية للبند
   المختار - الضغط على بند بالدونات أو بالقائمة يبدّل التفصيل المعروض بالبطاقة المجاورة
   ========================================================================== */
// أعمدة أفقية للبنود الفرعية ("البند") ضمن البند الرئيسي المختار - القيمة والعدد بنهاية العمود
function exsBuildDebtSubItemsBarOption(subItems) {
    const sorted = (subItems || []).slice().sort(function (a, b) { return a.totalValue - b.totalValue; });
    return {
        tooltip: exsTooltip({
            trigger: "axis", axisPointer: { type: "shadow" },
            formatter: function (params) {
                const p = params[0];
                return '<div style="font-weight:700;margin-bottom:4px;">' + exsEscape(p.name) + '</div>' +
                    '<div>القيمة: <strong>' + exsFormatNum(p.value) + '</strong></div>' +
                    '<div>عدد المديونيات: <strong>' + exsFormatNum(p.data.count) + '</strong></div>';
            }
        }),
        grid: { left: 10, right: 70, top: 10, bottom: 10, containLabel: true },
        xAxis: exsAxisStyle({ type: "value", axisLabel: { show: false }, splitLine: { show: false } }),
        yAxis: exsAxisStyle({ type: "category", data: sorted.map(function (c) { return c.category; }), axisLabel: { width: 150, overflow: "truncate", fontSize: 11 } }),
        series: [{
            type: "bar", barMaxWidth: 20,
            data: sorted.map(function (c, i) { return { value: c.totalValue, count: c.count, itemStyle: { color: EXS_PALETTE[i % EXS_PALETTE.length], borderRadius: [0, 4, 4, 0] } }; }),
            label: {
                show: true, position: "right", distance: 6, color: EXS_C.axisText, fontWeight: 700, fontSize: 11,
                formatter: function (p) { return exsFormatNum(p.value) + " (" + exsFormatNum(p.data.count) + ")"; }
            }
        }]
    };
}

let exsDebtSubItems = [];
let exsDebtSelectedClassification = null;
let exsDebtBarChart = null;

function exsSelectDebtClassification(name) {
    if (!name || name === exsDebtSelectedClassification) return;
    exsDebtSelectedClassification = name;
    const breakdown = exsDebtSubItems.filter(function (b) { return b.classificationLabel === name; })[0];
    if (exsDebtBarChart) exsDebtBarChart.setOption(exsBuildDebtSubItemsBarOption(breakdown ? breakdown.subItems : []), true);
    const badge = document.getElementById("exsDebtBarBadge");
    if (badge) badge.textContent = name;
}

function exsRenderDebtRow(data) {
    const wrap = document.getElementById("exsRow3");
    const noData = document.getElementById("exsRow3NoData");
    if (!wrap) return;
    wrap.innerHTML = "";

    if (!data.debtforallHasData) {
        if (noData) noData.style.display = "";
        return;
    }
    if (noData) noData.style.display = "none";

    const rows = (data.debtforallByClassification || []).map(function (r) { return { name: r.classification, value: r.totalValue }; });
    const total = rows.reduce(function (s, r) { return s + r.value; }, 0);

    // البطاقة الأولى: دونات + قائمة أسماء البنود وقيمها، وتحتها كرتا عدد/قيمة المديونيات
    const comboDiv = document.createElement("div");
    comboDiv.className = "chart-card";
    const donutId = "exsDebtDonut_" + Date.now();
    comboDiv.innerHTML =
        '<div class="chart-header"><div class="chart-title">توزيع المديونيات حسب البند</div></div>' +
        '<div class="ai-donut-split">' +
            '<div class="echart-box" id="' + donutId + '" style="height:230px;"></div>' +
            '<div class="ai-donut-list" id="exsDebtDonutList">' + exsAiDonutListHtml(rows, "") + '</div>' +
        '</div>' +
        '<div class="rp-growth-cards" style="grid-template-columns:repeat(2,1fr);margin-top:10px;">' +
            exsRpGrowthCardHtml("bi-list-ol", "إجمالي عدد المديونيات", exsFormatNum(data.debtforallTotalCount)) +
            exsRpGrowthCardHtml("bi-cash-stack", "إجمالي قيمة المديونيات", exsSarVal(data.debtforallTotalValue)) +
        '</div>';
    wrap.appendChild(comboDiv);
    const donutChart = echarts.init(document.getElementById(donutId));
    donutChart.setOption(exsBuildPlainDonutOption(rows, total, "إجمالي القيمة", "القيمة"));
    exsCharts.push(donutChart);
    requestAnimationFrame(function () { try { donutChart.resize(); } catch (e) {} });

    // البطاقة الثانية: البنود الفرعية للبند المختار (تبدأ بالبند الأعلى قيمةً، وتتبدّل بالضغط)
    exsDebtSubItems = data.debtforallSubItemsByClassification || [];
    const firstName = rows.length ? rows[0].name : null;
    const defaultBreakdown = exsDebtSubItems.filter(function (b) { return b.classificationLabel === firstName; })[0] || exsDebtSubItems[0];
    exsDebtSelectedClassification = defaultBreakdown ? defaultBreakdown.classificationLabel : firstName;

    const barDiv = document.createElement("div");
    barDiv.className = "chart-card";
    barDiv.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">البنود الفرعية للبند المختار</div>' +
            '<div class="chart-badge" id="exsDebtBarBadge">' + exsEscape(exsDebtSelectedClassification || "") + '</div>' +
        '</div>' +
        '<div class="echart-box" id="exsDebtBarBox" style="height:300px;"></div>';
    wrap.appendChild(barDiv);
    exsDebtBarChart = echarts.init(document.getElementById("exsDebtBarBox"));
    exsDebtBarChart.setOption(exsBuildDebtSubItemsBarOption(defaultBreakdown ? defaultBreakdown.subItems : []));
    exsCharts.push(exsDebtBarChart);
    requestAnimationFrame(function () { try { exsDebtBarChart.resize(); } catch (e) {} });

    donutChart.on("click", function (params) { exsSelectDebtClassification(params.name); });

    // الضغط على اسم بالقائمة الجانبية = نفس أثر الضغط على الدونات
    comboDiv.querySelectorAll("#exsDebtDonutList .ai-donut-list-item").forEach(function (el) {
        el.style.cursor = "pointer";
        el.addEventListener("click", function () {
            const nm = el.querySelector(".ai-donut-list-name");
            exsSelectDebtClassification(nm ? nm.textContent : "");
        });
    });
}

/* ==========================================================================
   المديونيات والمطالبات - بطاقة واحدة لكل إدارة عامة (شبكة 3 أعمدة)، بنفس هوية ألوان
   وحجم الصفحة (chart-card/chart-header الأخضر + عداد tc-gauge الجاهزين). كل بطاقة مضغوطة
   بنفس الحجم دائمًا (عداد + إحصاءات فقط)؛ زر "التفاصيل" بالعنوان يفتح/يطوي محافظها الفرعية
   بدل عرضها دائمًا حتى لا تختلف أطوال البطاقات حسب عدد محافظ كل إدارة
   ========================================================================== */
let exsDepts = [];
let exsPortfolios = [];

function exsGroupDeptPortfolios(portfolios) {
    const byDept = {};
    const order = [];
    (portfolios || []).forEach(function (p) {
        if (!byDept[p.departmentLabel]) { byDept[p.departmentLabel] = { name: p.departmentLabel, processed: 0, remaining: 0 }; order.push(p.departmentLabel); }
        byDept[p.departmentLabel].processed += p.processedCount || 0;
        byDept[p.departmentLabel].remaining += p.remainingCount || 0;
    });
    return order.map(function (name) { return byDept[name]; });
}

function exsPortfoliosForDept(name) {
    return (exsPortfolios || []).filter(function (p) { return p.departmentLabel === name; });
}

// عداد دائري صغير - نفس نمط .tc-gauge-* الجاهز بصفحة "مقارنة المستهدفات" (بلون البراند)
function exsEdpGaugeSvg(percent, sizePx) {
    const r = 85, c = 2 * Math.PI * r;
    const p = Math.max(0, Math.min(100, percent || 0));
    const offset = c - (p / 100) * c;
    const size = sizePx || 80;
    return '<div class="tc-gauge-ring-wrap" style="width:' + size + 'px;height:' + size + 'px;">' +
        '<svg viewBox="0 0 220 220">' +
            '<circle class="tc-gauge-track" cx="110" cy="110" r="' + r + '"></circle>' +
            '<circle class="tc-gauge-progress" cx="110" cy="110" r="' + r + '" stroke="' + EXS_C.dark + '" ' +
                'stroke-dasharray="' + c.toFixed(2) + '" stroke-dashoffset="' + offset.toFixed(2) + '"></circle>' +
        '</svg>' +
        '<div class="tc-gauge-center"><div class="tc-gauge-percent" style="font-size:.92rem;">' + p.toFixed(0) + '%</div></div>' +
    '</div>';
}

function exsEdpStatCardHtml(color, icon, label, valueText) {
    return '<div class="kpi-card" style="margin:0;flex:1 1 0;min-width:150px;">' +
        '<div class="icon" style="background:' + color + '22;color:' + color + ';"><i class="bi ' + icon + '"></i></div>' +
        '<div class="label">' + exsEscape(label) + '</div>' +
        '<div class="value" style="font-size:1.1rem;">' + valueText + '</div>' +
    '</div>';
}

function exsEdpSubrowHtml(portfolio) {
    const processed = portfolio.processedCount || 0;
    const remaining = portfolio.remainingCount || 0;
    const total = processed + remaining;
    const pct = total > 0 ? Math.round((processed / total) * 1000) / 10 : 0;
    return '<div class="exs-dept-subrow">' +
        '<div class="sname"><span>' + exsEscape(portfolio.portfolioLabel) + '</span>' +
            '<span class="tag">' + exsFormatNum(processed) + ' معالج / ' + exsFormatNum(remaining) + ' متبقي</span></div>' +
        '<div class="exs-dept-track">' +
            '<div class="exs-dept-track-fill" style="width:' + pct + '%;"></div>' +
            '<div class="exs-dept-track-label">' + pct + '%</div>' +
        '</div>' +
    '</div>';
}

function exsEdpDeptBlockHtml(dept) {
    const total = dept.processed + dept.remaining;
    const pct = total > 0 ? Math.round((dept.processed / total) * 1000) / 10 : 0;
    const subrows = exsPortfoliosForDept(dept.name)
        .slice()
        .sort(function (a, b) { return ((b.processedCount || 0) + (b.remainingCount || 0)) - ((a.processedCount || 0) + (a.remainingCount || 0)); })
        .map(exsEdpSubrowHtml).join("");
    return '<div class="chart-card">' +
        '<div class="chart-header">' +
            '<div class="chart-title">' + exsEscape(dept.name) + '</div>' +
        '</div>' +
        '<div class="exs-dept-gauge-row">' +
            exsEdpGaugeSvg(pct, 80) +
            '<div class="exs-dept-gauge-stats">' +
                '<div class="np">' + exsFormatNum(dept.processed) + ' معالج</div>' +
                '<div class="nr">' + exsFormatNum(dept.remaining) + ' متبقي</div>' +
            '</div>' +
        '</div>' +
        '<div class="exs-dept-subrows">' + subrows + '</div>' +
    '</div>';
}

function exsRenderDeptProSection(claimsData, depts) {
    const wrap = document.getElementById("exsDeptProSection");
    const noData = document.getElementById("exsDeptProNoData");
    if (!wrap) return;
    wrap.innerHTML = "";

    if (!claimsData.claimsExecutionHasData || !depts.length) {
        if (noData) noData.style.display = "";
        return;
    }
    if (noData) noData.style.display = "none";

    const totals = depts.reduce(function (acc, d) { acc.processed += d.processed; acc.remaining += d.remaining; return acc; }, { processed: 0, remaining: 0 });
    const totalAll = totals.processed + totals.remaining;
    const collectedPct = totalAll > 0 ? Math.round((totals.processed / totalAll) * 1000) / 10 : 0;

    const kpiRow = document.createElement("div");
    kpiRow.style.cssText = "display:flex;gap:10px;padding:4px 2px;flex-wrap:wrap;margin-bottom:12px;";
    kpiRow.innerHTML =
        exsEdpStatCardHtml(EXS_C.dark, "bi-file-earmark-text", "إجمالي عدد المطالبات", exsFormatNum(claimsData.claimsTotalCount)) +
        exsEdpStatCardHtml(EXS_C.mid, "bi-cash-stack", "إجمالي قيمة المطالبات", exsSarVal(claimsData.claimsTotalValue)) +
        exsEdpStatCardHtml(EXS_C.lime, "bi-check-circle", "نسبة التحصيل الإجمالية", collectedPct.toFixed(1) + "%");
    wrap.appendChild(kpiRow);

    const grid = document.createElement("div");
    grid.className = "exs-dept-grid";
    grid.innerHTML = depts.slice()
        .sort(function (a, b) { return (b.processed + b.remaining) - (a.processed + a.remaining); })
        .map(exsEdpDeptBlockHtml).join("");
    wrap.appendChild(grid);
}

/* ==========================================================================
   التحميل الرئيسي: كل الصفوف تُحمَّل بالتوازي عند فتح الصفحة (بلا انتظار ضغط المستخدم)
   ========================================================================== */
document.addEventListener("DOMContentLoaded", function () {
    exsInitFilterBar();
    exsLoadTargetFilterOptions().then(exsLoadTargetAchievement);

    fetch(window.EXEC_TARH_OPPORTUNITIES_URL).then(function (r) { return r.json(); }).then(exsRenderTarhRow)
        .catch(function (err) { console.error("تعذّر تحميل صف الفرص/العقود:", err); });

    fetch(window.EXEC_REVENUE_PERF_URL).then(function (r) { return r.json(); }).then(exsRenderRevenueGrowthPeriodCard)
        .catch(function (err) { console.error("تعذّر تحميل كروت نمو الإيرادات:", err); });

    fetch(window.EXEC_DEBT_URL).then(function (r) { return r.json(); }).then(exsRenderDebtRow)
        .catch(function (err) { console.error("تعذّر تحميل صف المديونيات:", err); });

    Promise.all([
        fetch(window.EXEC_CLAIMS_URL).then(function (r) { return r.json(); }),
        fetch(window.EXEC_DEPT_URL).then(function (r) { return r.json(); })
    ]).then(function (results) {
        const claimsData = results[0], deptData = results[1];
        exsPortfolios = deptData.generalDeptPortfolios || [];
        exsDepts = exsGroupDeptPortfolios(exsPortfolios);
        exsRenderDeptProSection(claimsData, exsDepts);
    }).catch(function (err) { console.error("تعذّر تحميل قسم المديونيات والمطالبات:", err); });

    window.addEventListener("resize", function () {
        exsCharts.forEach(function (c) { try { c.resize(); } catch (e) {} });
    });
});
