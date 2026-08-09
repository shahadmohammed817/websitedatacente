/* ==========================================================================
   رسوم صفحة "بيانات الأمانة" (بند الاستثمار) — نسخة مخصّصة
   ========================================================================== */
   /*
   هذا الملف يُحمَّل فقط في صفحة Dashboard/Scope/Amanah (بعد section-dashboard.js)
   ولا يؤثر إطلاقًا على أي صفحة أخرى (البلدية، الصفحة الرئيسية، صفحات البنود الفردية).

   يعمل عبر إعادة تعريف الدوال الخمس التي يستدعيها section-dashboard.js من داخل
   loadDashboard() بنفس الأسماء والحاويات (kpiGrid/chartsRow1/chartsRow2)، دون المساس
   بأي شيء آخر بذلك الملف المشترك.

   Cross Filtering: كل رسم يجلب بياناته من Dashboard/AmanahInvestment/Data مع تمرير
   sectionIds المستخرجة من currentBaseUrl (المتغيّر العام الذي يضبطه section-dashboard.js
   عند كل اختيار من القائمة الجانبية) بالإضافة لفلترَي الربع/رقم العقد الحاليين - بحيث يتحدّث
   كل شيء تلقائيًا عند اختيار إدارة عامة أو فرعية، وعند تغيير أي فلتر، دون أي تعديل آخر.

   المحتوى:
   1) مؤشر دائري (Gauge): نسبة التحصيل الإجمالية
   2) 4 بطاقات KPI: إجمالي المفوتر / المحصل / غير المحصل / عدد الفواتير
   3) Stacked Column حسب الإدارة العامة (محصل/غير محصل) + Data Labels
   4) Line Chart: اتجاه التحصيل الشهري (محصل + مفوتر للمقارنة)
   5) Donut: توزيع البنود حسب التصنيف (استثمار / تأجير مؤقت)
   6) Stacked Horizontal Bar: كل الإدارات الفرعية، مرتّبة تنازليًا حسب إجمالي القيمة
   7) بطاقتا معلومات: أعلى إدارة في نسبة التحصيل / أكثر إدارة من حيث عدد الفواتير
   ========================================================================== */

/* --------------------------------------------------------------------------
   جلب بيانات بند الاستثمار (مع Cache حسب الفلاتر + الأقسام المُختارة بالقائمة الجانبية)
   -------------------------------------------------------------------------- */
let aiDataCache = null;   // { key, data }
let aiDataPending = null; // { key, promise }

// يستخرج deptGeneral/deptSub من currentBaseUrl (يضبطه صندوق الفلاتر بصفحة "بنود الأمانة"
// عبر computeBaseUrlFromFilters بصيغة /Dashboard/AmanahInvestment/Data?deptGeneral=...)
function aiExtractDeptFilters() {
    if (typeof currentBaseUrl !== "string" || !currentBaseUrl) return null;
    const qIndex = currentBaseUrl.indexOf("?");
    if (qIndex === -1) return null;
    const params = new URLSearchParams(currentBaseUrl.substring(qIndex + 1));
    const deptGeneral = params.get("deptGeneral");
    if (!deptGeneral) return null;
    return { deptGeneral: deptGeneral, deptSub: params.get("deptSub") || "" };
}

async function getAmanahInvestmentData() {
    const qs = (typeof currentFilterParams === "function") ? currentFilterParams().toString() : "";
    const deptFilters = aiExtractDeptFilters();
    const cacheKey = qs + "||" + (deptFilters ? deptFilters.deptGeneral + "|" + deptFilters.deptSub : "");

    if (aiDataCache && aiDataCache.key === cacheKey) return aiDataCache.data;
    if (aiDataPending && aiDataPending.key === cacheKey) return aiDataPending.promise;

    const params = new URLSearchParams(qs);
    if (deptFilters) {
        params.set("deptGeneral", deptFilters.deptGeneral);
        if (deptFilters.deptSub) params.set("deptSub", deptFilters.deptSub);
    }
    const finalQs = params.toString();
    const url = "/Dashboard/AmanahInvestment/Data" + (finalQs ? "?" + finalQs : "");

    const promise = fetch(url)
        .then(function (resp) { return resp.ok ? resp.json() : null; })
        .catch(function () { return null; });

    aiDataPending = { key: cacheKey, promise: promise };
    const result = await promise;
    aiDataCache = { key: cacheKey, data: result };
    aiDataPending = null;
    return result;
}

/* ==========================================================================
   1) بطاقات KPI: إجمالي المفوتر / المحصل / غير المحصل / عدد الفواتير
   ========================================================================== */
function aiKpiCard(cls, icon, label, count, countSuffix, value) {
    return '<div class="kpi-card ' + cls + '">' +
        '<div class="icon">' + icon + '</div>' +
        '<div class="label">' + label + '</div>' +
        '<div class="value">' + formatNum(count) + '<span style="font-size:.55em;font-weight:700;color:var(--muted);"> ' + countSuffix + '</span></div>' +
        (value !== null ? '<div style="font-size:.82rem;color:var(--brand-dark);font-weight:700;margin-top:4px;display:flex;align-items:center;gap:3px;">' +
            formatNum(value) + ' ' + SAR_SYMBOL + '</div>' : '') +
    '</div>';
}

function aiRateKpiCard(cls, icon, label, percent) {
    return '<div class="kpi-card ' + cls + '">' +
        '<div class="icon">' + icon + '</div>' +
        '<div class="label">' + label + '</div>' +
        '<div class="value">' + (percent || 0).toFixed(1) + '%</div>' +
    '</div>';
}

function renderKPIs() {
    const grid = document.getElementById("kpiGrid");
    if (!grid) return;

    getAmanahInvestmentData().then(function (ai) {
        if (!document.getElementById("kpiGrid") || !ai) return;
        document.getElementById("kpiGrid").innerHTML =
            aiKpiCard("c1", ICONS.wallet, "إجمالي المفوتر", ai.totalInvoiceCount, "فاتورة", ai.totalValue) +
            aiKpiCard("c2", ICONS.check, "إجمالي المحصل", ai.paidInvoiceCount, "فاتورة", ai.paidValue) +
            aiKpiCard("c3", ICONS.alert, "إجمالي غير المحصل", ai.unpaidInvoiceCount, "فاتورة", ai.unpaidValue) +
            aiRateKpiCard("c4", ICONS.trending, "نسبة التحصيل", ai.collectionRatePercent);
    });
}

/* ==========================================================================
   2) بطاقتا معلومات ذكية: أعلى إدارة في نسبة التحصيل / أكثر إدارة بعدد الفواتير
   تُعرَضان جنب رسم "الفوترة والتحصيل حسب الإدارة" (بدل مؤشر Gauge منفصل - نسبة
   التحصيل الإجمالية انتقلت لبطاقة KPI الرابعة أعلى الصفحة)
   ========================================================================== */
function aiInsightSquareHtml(icon, label, dept, metric) {
    return '<div style="flex:1 1 0;min-width:0;background:var(--surface);border:1px solid var(--border);' +
            'border-radius:var(--radius-md);box-shadow:var(--shadow-xs);padding:12px;' +
            'display:flex;flex-direction:column;align-items:center;justify-content:center;text-align:center;gap:6px;aspect-ratio:1;">' +
        '<div style="width:34px;height:34px;border-radius:10px;background:rgba(14,110,97,0.08);color:var(--brand-dark);' +
            'display:flex;align-items:center;justify-content:center;flex-shrink:0;font-size:16px;"><i class="bi ' + icon + '"></i></div>' +
        '<div style="font-size:.7rem;color:var(--muted);font-weight:700;">' + label + '</div>' +
        '<div style="font-weight:800;color:var(--text);font-size:.82rem;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;max-width:100%;">' + escapeHtml(dept || "—") + '</div>' +
        (metric ? '<div style="font-size:.76rem;color:var(--brand-dark);font-weight:700;">' + metric + '</div>' : '') +
    '</div>';
}

// بطاقتان صغيرتان مربعتان جنب بعض، ضمن حاوية واحدة بنفس صف رسم الفوترة/التحصيل (بدل بطاقتين
// كاملتي العرض كل واحدة تأخذ عمودًا مستقلاً بالشبكة)
function renderAiInsightCards(row, ai) {
    const wrap = document.createElement("div");
    wrap.style.cssText = "display:flex;gap:10px;grid-column:1 / -1;max-width:340px;";
    wrap.innerHTML =
        aiInsightSquareHtml("bi-trophy", "أعلى إدارة في نسبة التحصيل", ai.topCollectionRateDept, ai.topCollectionRateDept ? ai.topCollectionRatePercent.toFixed(1) + "%" : null) +
        aiInsightSquareHtml("bi-receipt", "أكثر إدارة من حيث عدد الفواتير", ai.topInvoiceCountDept, ai.topInvoiceCountDept ? (formatNum(ai.topInvoiceCountAmount) + " فاتورة") : null);
    row.appendChild(wrap);
}

/* ==========================================================================
   3) Stacked Column حسب الإدارة العامة (محصل/غير محصل) + Data Labels
   ========================================================================== */
function aiShortDeptLabel(name) {
    if (!name) return name;
    if (name.indexOf("تجار") > -1) return "الاستثمارات التجارية";
    if (name.indexOf("تنمو") > -1) return "الاستثمارات التنموية";
    if (name.indexOf("خدم") > -1) return "الخدمات";
    return name;
}

function buildAiMainChartOption(labels, paidValues, unpaidValues) {
    function dataLabel() {
        return {
            show: true, position: "inside", color: "#fff", fontWeight: 800, fontSize: 11,
            formatter: function (p) { return p.value > 0 ? formatCompact(p.value) : ""; }
        };
    }
    return {
        tooltip: tooltipStyle({
            trigger: "axis", axisPointer: { type: "shadow" },
            formatter: function (params) {
                let html = '<div style="font-weight:700;margin-bottom:6px;">' + params[0].axisValue + '</div>';
                let total = 0;
                params.forEach(function (p) {
                    total += p.value;
                    html += '<div style="display:flex;align-items:center;gap:6px;">' +
                        '<span style="display:inline-block;width:10px;height:10px;border-radius:50%;background:' + p.color + '"></span>' +
                        '<span>' + p.seriesName + ': <strong>' + formatNum(p.value) + '</strong></span></div>';
                });
                html += '<div style="margin-top:4px;border-top:1px solid ' + C.grid + ';padding-top:4px;">الإجمالي: <strong>' + formatNum(total) + '</strong></div>';
                return html;
            }
        }),
        legend: { bottom: 0, textStyle: Object.assign({}, AXIS_LABEL_STYLE), itemWidth: 10, itemHeight: 10 },
        grid: { left: 50, right: 16, top: 16, bottom: 46 },
        xAxis: axisStyle({ type: "category", data: labels, axisLabel: { interval: 0, fontSize: 11 } }),
        yAxis: axisStyle({ type: "value", axisLabel: { formatter: formatCompact } }),
        series: [
            { name: "محصل", type: "bar", stack: "s", data: paidValues, barMaxWidth: 70, itemStyle: { color: C.dark }, label: dataLabel() },
            { name: "غير محصل", type: "bar", stack: "s", data: unpaidValues, barMaxWidth: 70, itemStyle: { color: C.red }, label: dataLabel() }
        ]
    };
}

function renderAiMainChart(row) {
    const id = "aiMainChart_" + Date.now();
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">الفوترة والتحصيل حسب الإدارة العامة</div>' +
            '<div class="chart-badge">محصل / غير محصل</div>' +
        '</div>' +
        '<div class="echart-box" id="' + id + '"></div>';
    row.appendChild(div);

    const container = document.getElementById(id);
    const chart = echarts.init(container);
    charts.entityBar = chart;
    chartRegistry.push(chart);

    getAmanahInvestmentData().then(function (ai) {
        if (!document.body.contains(container) || !ai || !ai.deptGeneralLabels || !ai.deptGeneralLabels.length) return;
        const labels = ai.deptGeneralLabels.map(aiShortDeptLabel);
        try { chart.setOption(buildAiMainChartOption(labels, ai.deptGeneralPaidValues, ai.deptGeneralUnpaidValues)); } catch (e) {}
    });
}

// تُستدعى أولاً بترتيب تحميل اللوحة - رسم الفوترة/التحصيل يظهر قبل البطاقتين بنفس الصف
function renderTopItemsChart() {
    const row1 = document.getElementById("chartsRow1");
    if (!row1) return;
    renderAiMainChart(row1);
}

// تُستدعى لاحقًا - البطاقتان تُضافان بعد رسم الفوترة/التحصيل بنفس الصف
function renderEntityMonthChart() {
    const row1 = document.getElementById("chartsRow1");
    if (!row1) return;

    getAmanahInvestmentData().then(function (ai) {
        if (!document.getElementById("chartsRow1") || !ai) return;
        renderAiInsightCards(row1, ai);
    });
}

/* ==========================================================================
   5) Donut: توزيع البنود حسب التصنيف (استثمار / تأجير مؤقت) - تُعرض أولاً بالصف الثاني
   6) Stacked Horizontal Bar: كل الإدارات الفرعية - جنب الدونت
   ========================================================================== */
function renderSubItemsBarChart() {
    const row2 = document.getElementById("chartsRow2");
    if (!row2) return;

    getAmanahInvestmentData().then(function (ai) {
        if (!document.getElementById("chartsRow2") || !ai) return;
        renderAiDonut(row2, ai);
        renderAiSubDeptBar(row2, ai);
    });
}

/* ==========================================================================
   4) Line Chart: اتجاه التحصيل الشهري (محصل + مفوتر للمقارنة) - أسفل الصف الثاني، بعرض
   كامل وطول أكبر، مع أرقام ثابتة الظهور على كل نقطة
   ========================================================================== */
function renderQuarterChart() {
    const row2 = document.getElementById("chartsRow2");
    if (!row2) return;

    const id = "aiTrend_" + Date.now();
    const div = document.createElement("div");
    div.className = "chart-card";
    div.style.cssText = "grid-column:1 / -1;"; // عرض كامل الصف
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">اتجاه التحصيل الشهري</div>' +
            '<div class="chart-badge">محصل / مفوتر</div>' +
        '</div>' +
        '<div class="echart-box" id="' + id + '" style="height:400px;"></div>';
    row2.appendChild(div);

    const container = document.getElementById(id);
    const chart = echarts.init(container);
    charts.line = chart;
    chartRegistry.push(chart);

    getAmanahInvestmentData().then(function (ai) {
        if (!document.body.contains(container) || !ai || !ai.monthLabels || !ai.monthLabels.length) return;
        function lineLabel(color) {
            return {
                show: true, position: "top", distance: 6,
                formatter: function (p) { return p.value ? formatCompact(p.value) : ""; },
                color: color, fontSize: 11, fontWeight: 700
            };
        }
        try {
            chart.setOption({
                tooltip: tooltipStyle({ trigger: "axis" }),
                legend: { bottom: 0, textStyle: Object.assign({}, AXIS_LABEL_STYLE), itemWidth: 10, itemHeight: 10 },
                grid: { left: 50, right: 16, top: 30, bottom: 46 },
                xAxis: axisStyle({ type: "category", boundaryGap: false, data: ai.monthLabels, axisLabel: { rotate: ai.monthLabels.length > 6 ? 35 : 0 } }),
                yAxis: axisStyle({ type: "value", axisLabel: { formatter: formatCompact } }),
                series: [
                    {
                        name: "المحصل", type: "line", data: ai.monthCollectedValues, smooth: false,
                        symbol: "circle", symbolSize: 6, lineStyle: { color: C.dark, width: 3 },
                        itemStyle: { color: C.dark, borderWidth: 2, borderColor: "#fff" },
                        areaStyle: { color: C.dark, opacity: 0.08 },
                        label: lineLabel(C.dark)
                    },
                    {
                        name: "المفوتر", type: "line", data: ai.monthTotalValues, smooth: false,
                        symbol: "circle", symbolSize: 6, lineStyle: { color: C.mid, width: 2, type: "dashed" },
                        itemStyle: { color: C.mid, borderWidth: 2, borderColor: "#fff" },
                        label: lineLabel(C.mid)
                    }
                ]
            });
        } catch (e) {}
    });
}

/* ==========================================================================
   بناء رسمَي الدونت والإدارات الفرعية (تُستدعيان من renderSubItemsBarChart أعلاه)
   ========================================================================== */
function renderAiDonut(row2, ai) {
    const id = "aiDonut_" + Date.now();
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">توزيع البنود حسب التصنيف</div>' +
            '<div class="chart-badge">استثمار / تأجير مؤقت</div>' +
        '</div>' +
        '<div class="echart-box" id="' + id + '"></div>';
    row2.appendChild(div);

    const chart = echarts.init(document.getElementById(id));
    charts.donut = chart;
    chartRegistry.push(chart);

    const items = [
        { name: "استثمار", value: ai.investmentValue, count: ai.investmentInvoiceCount, color: C.dark },
        { name: "تأجير مؤقت", value: ai.tempLeaseValue, count: ai.tempLeaseInvoiceCount, color: C.lime }
    ];
    const pieData = items.map(function (it) { return { name: it.name, value: it.value, count: it.count, itemStyle: { color: it.color } }; });

    try {
        chart.setOption({
            tooltip: tooltipStyle({ trigger: "item", formatter: "{b}: {c} ({d}%)" }),
            legend: { bottom: 0, textStyle: Object.assign({}, AXIS_LABEL_STYLE), itemWidth: 10, itemHeight: 10 },
            series: [{
                type: "pie", radius: ["38%", "65%"], center: ["50%", "42%"],
                itemStyle: { borderColor: "#fff", borderWidth: 2 },
                label: {
                    show: true, fontSize: 12, fontWeight: 700, color: C.axisText,
                    formatter: function (p) {
                        return p.name + "  " + p.percent.toFixed(1) + "%";
                    }
                },
                labelLine: { show: true, length: 8, length2: 8 },
                data: pieData
            }]
        });
    } catch (e) {}
}

function renderAiSubDeptBar(row2, ai) {
    if (!ai.subDepartments || !ai.subDepartments.length) return;

    const id = "aiSubBar_" + Date.now();
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">الإدارات الفرعية</div>' +
            '<div class="chart-badge">إجمالي القيمة</div>' +
        '</div>' +
        '<div class="echart-box" id="' + id + '"></div>';
    row2.appendChild(div);

    const chart = echarts.init(document.getElementById(id));
    charts.subItemsBar = chart;
    chartRegistry.push(chart);

    // مرتّبة تنازليًا من الخادم أصلاً حسب إجمالي القيمة؛ نعكسها هنا فقط لأن محور الفئات يرسم من الأسفل للأعلى
    const rows = ai.subDepartments.slice().reverse();
    const labels = rows.map(function (r) { return r.name; });
    const totals = rows.map(function (r) { return r.totalValue; });

    try {
        chart.setOption({
            tooltip: tooltipStyle({
                trigger: "axis", axisPointer: { type: "shadow" },
                formatter: function (params) {
                    const p = params[0];
                    return '<div style="font-weight:700;margin-bottom:4px;">' + p.name + '</div>' +
                        '<div>الإجمالي: <strong>' + formatNum(p.value) + '</strong></div>';
                }
            }),
            grid: { left: 130, right: 70, top: 10, bottom: 10 },
            xAxis: axisStyle({ type: "value", show: false }),
            yAxis: axisStyle({ type: "category", data: labels, axisLabel: { width: 120, overflow: "break", lineHeight: 14, fontSize: 11 } }),
            series: [{
                name: "الإجمالي", type: "bar", data: totals, barMaxWidth: 20,
                itemStyle: { color: C.dark, borderRadius: [0, 6, 6, 0] },
                label: {
                    show: true, position: "right", color: C.axisText, fontWeight: 700, fontSize: 11,
                    formatter: function (p) { return formatCompact(p.value); }
                }
            }]
        });
    } catch (e) {}
}
