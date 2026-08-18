/* ==========================================================================
   رسوم صفحة "الإدارات العامة" (بندا الاستثمار + التأجير المؤقت) — نسخة مخصّصة
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
   1) 4 بطاقات KPI: إجمالي المفوتر / المحصل / غير المحصل / نسبة التحصيل (القيمة بالأعلى، العدد أسفلها)
   2) Donut: توزيع البنود حسب "نوع الاستثمار" + فلتر نوع الاستثمار أعلى الصفحة
   3) Donut/Bar: توزيع العقود حسب "حالة العقد" (قائم/جديد/... ديناميكي)
   4) Stacked Horizontal Bar: كل الإدارات الفرعية، مرتّبة تنازليًا حسب إجمالي القيمة
   5) بطاقة معلومات: أعلى إدارة في نسبة التحصيل
   6) Line Chart: اتجاه التحصيل الشهري (المحصل فقط)
   7) تصدير Excel/PDF بنفس الفلاتر الحالية
   ========================================================================== */

/* --------------------------------------------------------------------------
   جلب بيانات بند الاستثمار (مع Cache حسب الفلاتر + الأقسام المُختارة بالقائمة الجانبية)
   -------------------------------------------------------------------------- */
let aiDataCache = null;   // { key, data }
let aiDataPending = null; // { key, promise }

// قيمة فلتر "نوع الاستثمار" المختارة حاليًا (خاص بهذي الصفحة، مستقل عن فلاتر
// section-dashboard.js المشتركة) - يبقى محفوظًا بين إعادة الرسم حتى يتغيّر المستخدم اختياره
let aiInvestmentTypeFilter = "";

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
    const cacheKey = qs + "||" + (deptFilters ? deptFilters.deptGeneral + "|" + deptFilters.deptSub : "") + "||" + aiInvestmentTypeFilter;

    if (aiDataCache && aiDataCache.key === cacheKey) return aiDataCache.data;
    if (aiDataPending && aiDataPending.key === cacheKey) return aiDataPending.promise;

    const params = new URLSearchParams(qs);
    if (deptFilters) {
        params.set("deptGeneral", deptFilters.deptGeneral);
        if (deptFilters.deptSub) params.set("deptSub", deptFilters.deptSub);
    }
    if (aiInvestmentTypeFilter) params.set("investmentType", aiInvestmentTypeFilter);
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
// القيمة المالية هي الرقم الكبير البارز بالأعلى (بدل العدد سابقًا)، والعدد يصير سطرًا ثانويًا
// أسفلها بكلمة "عدد" (بدل "فاتورة")
function aiKpiCard(cls, icon, label, count, countSuffix, value) {
    return '<div class="kpi-card ' + cls + '">' +
        '<div class="icon">' + icon + '</div>' +
        '<div class="label">' + label + '</div>' +
        '<div class="value">' + formatNum(value) + ' <span class="value-sar">' + SAR_SYMBOL + '</span></div>' +
        '<div style="font-size:.82rem;color:var(--muted);font-weight:700;margin-top:4px;">' +
            countSuffix + ': ' + formatNum(count) +
        '</div>' +
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
            aiKpiCard("c1", ICONS.wallet, "إجمالي المفوتر", ai.totalInvoiceCount, "عدد", ai.totalValue) +
            aiKpiCard("c2", ICONS.check, "إجمالي المحصل", ai.paidInvoiceCount, "عدد", ai.paidValue) +
            aiKpiCard("c3", ICONS.alert, "إجمالي غير المحصل", ai.unpaidInvoiceCount, "عدد", ai.unpaidValue) +
            aiRateKpiCard("c4", ICONS.trending, "نسبة التحصيل", ai.collectionRatePercent);
        aiEnsureInvestmentTypeFilter(ai.investmentTypeOptions);
    });
}

/* ==========================================================================
   فلتر "نوع الاستثمار" - يُبنى مرة واحدة أعلى شريط الفلاتر المشترك (بنفس تنسيق فلاتر
   section-dashboard.js)، ويبقى قائمًا بين إعادة الرسم (buildFilterBar بالملف المشترك
   لا يعيد بناء الشريط لو كان موجودًا أصلاً)
   ========================================================================== */
function aiEnsureInvestmentTypeFilter(options) {
    if (document.getElementById("filterInvestmentType")) return;
    const bar = document.getElementById("dashFilterBar");
    if (!bar) return;

    const wrap = document.createElement("div");
    wrap.className = "filter-select-wrap";
    wrap.innerHTML = '<i class="bi bi-tag filter-icon"></i>';
    const select = document.createElement("select");
    select.id = "filterInvestmentType";
    select.className = "filter-select";
    let html = '<option value="">كل أنواع الاستثمار</option>';
    (options || []).forEach(function (opt) {
        html += '<option value="' + escapeHtml(opt) + '"' + (opt === aiInvestmentTypeFilter ? " selected" : "") + '>' + escapeHtml(opt) + '</option>';
    });
    select.innerHTML = html;
    wrap.appendChild(select);
    wrap.insertAdjacentHTML("beforeend", '<i class="bi bi-chevron-down filter-chevron"></i>');
    bar.insertBefore(wrap, bar.firstChild);

    select.addEventListener("change", function () {
        aiInvestmentTypeFilter = select.value;
        if (typeof applyDashboardFilters === "function") applyDashboardFilters();
    });
}

/* ==========================================================================
   2) بطاقتا معلومات ذكية: أعلى إدارة في نسبة التحصيل / أكثر إدارة بعدد الفواتير
   تُعرَضان جنب رسم "الفوترة والتحصيل حسب الإدارة" (بدل مؤشر Gauge منفصل - نسبة
   التحصيل الإجمالية انتقلت لبطاقة KPI الرابعة أعلى الصفحة)
   ========================================================================== */
// الأيقونة على اليمين جنب النص (بدل فوق النص بمنتصف مربّع)، واسم الإدارة كاملاً بلا اقتصاص
// (كان يُقصّ بـ ellipsis) - كل شيء بحجم أكبر وأوضح
function aiInsightSquareHtml(icon, label, dept, metric) {
    return '<div class="ai-insight-card">' +
        '<div class="ai-insight-icon"><i class="bi ' + icon + '"></i></div>' +
        '<div class="ai-insight-text">' +
            '<div class="ai-insight-label">' + label + '</div>' +
            '<div class="ai-insight-dept">' + escapeHtml(dept || "—") + '</div>' +
            (metric ? '<div class="ai-insight-metric">' + metric + '</div>' : '') +
        '</div>' +
    '</div>';
}

// بطاقتان بصف مستقل بعرض كامل: أعلى إدارة وأقل إدارة تحصيلاً معًا (أفقيًا جنب بعض) - الترتيب
// والعرض بناءً على قيمة المحصّل الفعلية (ريال)، لا نسبة التحصيل (نفس مبدأ "أكثر/أقل بلدية
// تفاعلاً" بصفحة البلديات)
function renderAiInsightCards(row, ai) {
    const wrap = document.createElement("div");
    wrap.className = "ai-insight-wrap ai-insight-wrap-row";
    wrap.innerHTML =
        aiInsightSquareHtml("bi-trophy", "أعلى إدارة تحصيلاً", ai.topCollectionRateDept, ai.topCollectionRateDept ? formatNum(ai.topCollectionValue) + " " + SAR_SYMBOL : null) +
        aiInsightSquareHtml("bi-graph-down-arrow", "أقل إدارة تحصيلاً", ai.bottomCollectionRateDept, ai.bottomCollectionRateDept ? formatNum(ai.bottomCollectionValue) + " " + SAR_SYMBOL : null);
    row.appendChild(wrap);
}

/* ==========================================================================
   رسم "حالة العقد" - ديناميكي حسب القيم الفعلية الموجودة بعمود "حالة العقد" (قائم/جديد
   الآن، أي قيمة تُضاف لاحقًا للملف تظهر تلقائيًا بلا تعديل كود) - إجمالي عام (كل الإدارات)
   ========================================================================== */
const AI_STATUS_PALETTE = [C.dark, C.lime, C.light, "#5F9E1F"];

// لون ثابت لكل حالة عقد بالاسم (قائم=داكن، جديد=لايم دومًا) بدل ترتيب الظهور - كان اللون
// يُحدَّد سابقًا بترتيب المفاتيح بالكائن القادم من الخادم (GroupBy)، وهذا الترتيب غير مضمون
// (قد يتغيّر بين إدارة وأخرى أو بعد إعادة استيراد)، فكانت بعض الإدارات تُظهر عمود "جديد" بلون
// "قائم" الداكن (أو العكس) رغم صحة التسمية والقيمة تمامًا - يضلّل القارئ بصريًا
const AI_STATUS_COLOR_MAP = { "قائم": C.dark, "جديد": C.lime };
function aiStatusColor(name, fallbackIndex) {
    return AI_STATUS_COLOR_MAP[name] || AI_STATUS_PALETTE[fallbackIndex % AI_STATUS_PALETTE.length];
}

function buildAiContractStatusOption(rows) {
    const total = rows.reduce(function (s, r) { return s + r.count; }, 0);
    const items = rows.map(function (r, i) {
        return { name: r.name, value: r.count, itemStyle: { color: aiStatusColor(r.name, i) } };
    });

    return {
        tooltip: tooltipStyle({
            trigger: "item",
            formatter: function (p) {
                const pct = total > 0 ? Math.round((p.value / total) * 1000) / 10 : 0;
                return '<div style="font-weight:700;margin-bottom:4px;">' + p.name + '</div>' +
                    '<div>العدد: <strong>' + formatNum(p.value) + '</strong> (' + pct + '%)</div>';
            }
        }),
        legend: { bottom: 0, textStyle: Object.assign({}, AXIS_LABEL_STYLE), itemWidth: 10, itemHeight: 10 },
        series: [{
            type: "pie", radius: ["38%", "65%"], center: ["50%", "42%"],
            itemStyle: { borderColor: "#fff", borderWidth: 2 },
            label: {
                show: true, fontSize: 12, fontWeight: 700, color: C.axisText,
                formatter: function (p) { return p.name + "  " + p.percent.toFixed(1) + "%"; }
            },
            labelLine: { show: true, length: 8, length2: 8 },
            data: items
        }]
    };
}

function renderAiContractStatusChart(row) {
    if (!row) return;
    const id = "aiStatusChart_" + Date.now();
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">حالة العقد</div>' +
            '<div class="chart-badge">قائم / جديد</div>' +
        '</div>' +
        '<div class="echart-box" id="' + id + '" style="height:190px;"></div>';
    row.appendChild(div);

    const container = document.getElementById(id);
    const chart = echarts.init(container);
    chartRegistry.push(chart);

    getAmanahInvestmentData().then(function (ai) {
        if (!document.body.contains(container) || !ai || !ai.contractStatusBreakdown || !ai.contractStatusBreakdown.length) return;
        try { chart.setOption(buildAiContractStatusOption(ai.contractStatusBreakdown)); } catch (e) {}
    });
}

/* ==========================================================================
   "الشكل الأول": بطاقة مستقلة لكل إدارة عامة - عمودان (قائم/جديد) بارتفاع القيمة،
   وعدد العقود مكتوب أسفل القيمة داخل تسمية كل عمود - يطابق الصورة المرجعية بالضبط
   (3 بطاقات جنب بعض بدل رسم واحد مجمّع)
   ========================================================================== */
function aiDeptIcon(label) {
    if (label.indexOf("خدمات") > -1) return "bi-gear";
    if (label.indexOf("تنمو") > -1) return "bi-graph-up-arrow";
    if (label.indexOf("تجار") > -1) return "bi-building";
    return "bi-diagram-3";
}

function buildAiDeptCardOption(statusValues, statusCounts) {
    const keys = Object.keys(statusValues || {});
    const data = keys.map(function (key, i) {
        const color = aiStatusColor(key, i);
        return {
            value: statusValues[key] || 0, name: key, aiCount: (statusCounts || {})[key] || 0,
            itemStyle: { color: color, borderRadius: [4, 4, 0, 0] }
        };
    });

    return {
        tooltip: tooltipStyle({
            trigger: "axis", axisPointer: { type: "shadow" },
            formatter: function (params) {
                const p = params[0];
                return '<div style="font-weight:700;">' + p.name + '</div><div>القيمة: <strong>' + formatNum(p.value) + '</strong></div><div>العدد: <strong>' + formatNum(p.data.aiCount) + '</strong></div>';
            }
        }),
        grid: { left: 16, right: 16, top: 40, bottom: 26 },
        xAxis: axisStyle({ type: "category", data: keys, axisLabel: { fontSize: 12, fontWeight: 700 } }),
        // بلا أرقام على محور واي (توفّر مساحة للتسمية فوق كل عمود) - المحور نفسه يبقى قائمًا
        // بمقياسه الحقيقي (بلا تطبيع) فيبقى ارتفاع كل عمود معبّرًا فعليًا عن قيمته
        yAxis: axisStyle({ type: "value", axisLabel: { show: false }, splitLine: { show: false } }),
        series: [{
            type: "bar", data: data, barMaxWidth: 64,
            label: {
                show: true, position: "top", fontSize: 11, lineHeight: 16,
                formatter: function (p) {
                    return '{val|' + formatNum(p.value) + '}\n{cnt|(' + formatNum(p.data.aiCount) + ')}';
                },
                rich: {
                    val: { fontSize: 14, fontWeight: 800, color: C.axisText, lineHeight: 17 },
                    cnt: { fontSize: 12, fontWeight: 700, color: C.muted, lineHeight: 14 }
                }
            }
        }]
    };
}

// اسم الإدارة العامة المختارة حاليًا (تتحكم بمحتوى الشكل الثاني - بطاقة الإدارات الفرعية
// المفردة أدناه) + مرجع بطاقة/رسمة الشكل الثاني حتى يقدر رسم الشكل الأول يحدّثها مباشرة
let aiSelectedDeptLabel = null;
let aiSubDeptSingleChart = null;
let aiSubDeptSingleCardEl = null;
let aiSubDeptSingleBoxId = null;

function renderAiDeptCards(row, ai) {
    if (!row || !ai.deptGeneralLabels || !ai.deptGeneralLabels.length) return;
    row.classList.add("ai-dept-cards-row");
    aiSelectedDeptLabel = ai.deptGeneralLabels[0];

    ai.deptGeneralLabels.forEach(function (label, i) {
        const statusValues = (ai.deptGeneralContractStatusValues || [])[i] || {};
        const statusCounts = (ai.deptGeneralContractStatusCounts || [])[i] || {};
        const id = "aiDeptCard_" + i + "_" + Date.now();
        const div = document.createElement("div");
        div.className = "chart-card ai-dept-card" + (i === 0 ? " ai-dept-card-selected" : "");
        div.innerHTML =
            '<div class="chart-header ai-header-green">' +
                '<div class="chart-title"><i class="bi ' + aiDeptIcon(label) + '"></i> ' + escapeHtml(label) + '</div>' +
                '<span class="ai-header-dash">—</span>' +
            '</div>' +
            '<div class="echart-box" id="' + id + '" style="height:210px;"></div>';
        row.appendChild(div);

        const chart = echarts.init(document.getElementById(id));
        chartRegistry.push(chart);
        try { chart.setOption(buildAiDeptCardOption(statusValues, statusCounts)); } catch (e) {}

        // الضغط على أي بطاقة إدارة عامة يحدّث الشكل الثاني (الإدارات الفرعية) فورًا لتلك
        // الإدارة، ويميّز البطاقة المختارة بإطار بارز
        div.addEventListener("click", function () {
            if (label === aiSelectedDeptLabel) return;
            aiSelectedDeptLabel = label;
            row.querySelectorAll(".ai-dept-card").forEach(function (c) { c.classList.remove("ai-dept-card-selected"); });
            div.classList.add("ai-dept-card-selected");
            aiUpdateSubDeptSingle(ai, label);
        });
    });
}

/* ==========================================================================
   توزيع البنود حسب "نوع الاستثمار" - شريط أفقي (بدل دونات) لأن التوزيع غير متوازن بشدة
   بالبيانات الفعلية (بند واحد يستحوذ على أغلب القيمة وبقية الأنواع شرائح ضئيلة جدًا لا
   تظهر بوضوح بشكل دائري) - الشريط الأفقي يعرض كل الأنواع بوضوح مهما تفاوتت قيمها
   ========================================================================== */
const AI_DONUT_PALETTE = [C.dark, C.mid, C.lime, C.light, "#5F9E1F", C.red, "#0A5A4F"];

function buildAiInvestmentTypeBarOption(rows) {
    // تنازليًا من الخادم أصلاً؛ نعكسها هنا فقط لأن محور الفئات يرسم من الأسفل للأعلى
    const sorted = rows.slice().reverse();
    const n = sorted.length;
    const labels = sorted.map(function (r) { return r.name; });
    const barData = sorted.map(function (r, i) {
        return { value: r.value, count: r.count, itemStyle: { color: AI_DONUT_PALETTE[(n - 1 - i) % AI_DONUT_PALETTE.length], borderRadius: [0, 6, 6, 0] } };
    });

    return {
        tooltip: tooltipStyle({
            trigger: "axis", axisPointer: { type: "shadow" },
            formatter: function (params) {
                const p = params[0];
                return '<div style="font-weight:700;margin-bottom:4px;">' + p.name + '</div>' +
                    '<div>القيمة: <strong>' + formatNum(p.value) + '</strong></div>' +
                    '<div>العدد: <strong>' + formatNum(p.data.count) + '</strong></div>';
            }
        }),
        grid: { left: 130, right: 90, top: 10, bottom: 10 },
        xAxis: axisStyle({ type: "value", show: false }),
        yAxis: axisStyle({ type: "category", data: labels, axisLabel: { width: 120, overflow: "break", lineHeight: 14, fontSize: 11 } }),
        series: [{
            name: "القيمة", type: "bar", data: barData, barMaxWidth: 22,
            label: {
                show: true, position: "right", color: C.axisText, fontWeight: 700, fontSize: 11,
                formatter: function (p) { return formatCompact(p.value) + '  (' + formatNum(p.data.count) + ')'; }
            }
        }]
    };
}

function renderAiDonut(row, ai) {
    const rows = ai.investmentTypeBreakdown;
    if (!rows || !rows.length) return;
    const sorted = rows.slice().sort(function (a, b) { return b.value - a.value; });

    const id = "aiInvestType_" + Date.now();
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">توزيع نوع الاستثمار</div>' +
            '<div class="chart-badge">القيمة (العدد)</div>' +
        '</div>' +
        '<div class="echart-box" id="' + id + '" style="height:190px;"></div>';
    row.appendChild(div);

    const chart = echarts.init(document.getElementById(id));
    charts.topItems = chart;
    chartRegistry.push(chart);
    try { chart.setOption(buildAiInvestmentTypeBarOption(sorted)); } catch (e) {}
}

// الصف الأول: "الشكل الأول" - بطاقة مستقلة لكل إدارة عامة (قائم/جديد قيمةً وعددًا)
function renderTopItemsChart() {
    const row1 = document.getElementById("chartsRow1");
    if (!row1) return;

    getAmanahInvestmentData().then(function (ai) {
        if (!document.getElementById("chartsRow1") || !ai) return;
        renderAiDeptCards(row1, ai);
    });
}

// الصف الثاني: "الشكل الثاني" - بطاقة مستقلة لكل إدارة عامة (إداراتها الفرعية قائم/جديد
// بالقيمة + نسبة تحصيل) - مباشرة تحت الصف الأول
function renderSubItemsBarChart() {
    const row2 = document.getElementById("chartsRow2");
    if (!row2) return;

    getAmanahInvestmentData().then(function (ai) {
        if (!document.getElementById("chartsRow2") || !ai) return;
        renderAiSubDeptSingle(row2, ai);
    });
}

// صف الدونات: "حالة العقد" جنب دونات "نوع الاستثمار" - نظرة إجمالية سريعة
function renderAiDonutsRow() {
    const rowDonuts = document.getElementById("chartsRowDonuts");
    if (!rowDonuts) return;

    getAmanahInvestmentData().then(function (ai) {
        if (!document.getElementById("chartsRowDonuts") || !ai) return;
        renderAiContractStatusChart(rowDonuts);
        renderAiDonut(rowDonuts, ai);
    });
}

// بطاقتا أعلى/أقل إدارة في نسبة التحصيل - صف مستقل بعرض كامل، بالإضافة لصف الدونات
// (حالة العقد + نوع الاستثمار) اللي ما له نقطة استدعاء مستقلة بملف section-dashboard.js
function renderEntityMonthChart() {
    renderAiDonutsRow();

    const rowInsights = document.getElementById("chartsRowInsights");
    if (!rowInsights) return;

    getAmanahInvestmentData().then(function (ai) {
        if (!document.getElementById("chartsRowInsights") || !ai) return;
        renderAiInsightCards(rowInsights, ai);
    });
}

/* ==========================================================================
   Line Chart: اتجاه التحصيل الشهري (المحصل فقط) - الصف الثالث بعرض كامل لحاله،
   مع أرقام ثابتة الظهور على كل نقطة
   ========================================================================== */
function renderQuarterChart() {
    const row3 = document.getElementById("chartsRow3");
    if (!row3) return;

    const id = "aiTrend_" + Date.now();
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">اتجاه التحصيل الشهري</div>' +
            '<div class="chart-badge">المحصل</div>' +
        '</div>' +
        '<div class="echart-box" id="' + id + '" style="height:260px;"></div>';
    row3.appendChild(div);

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
                // boundaryGap:true يترك هامشًا قبل أول نقطة وبعد آخر نقطة بدل التصاقهما بحافتَي
                // منطقة الرسم - يمنع تراكب تسمية القيمة الأولى/الأخيرة مع تسميات محور Y
                grid: { left: 55, right: 22, top: 30, bottom: 46 },
                xAxis: axisStyle({ type: "category", boundaryGap: true, data: ai.monthLabels, axisLabel: { rotate: ai.monthLabels.length > 6 ? 35 : 0 } }),
                yAxis: axisStyle({ type: "value", axisLabel: { formatter: formatCompact } }),
                series: [
                    {
                        name: "المحصل", type: "line", data: ai.monthCollectedValues, smooth: false,
                        symbol: "circle", symbolSize: 5, lineStyle: { color: C.dark, width: 2.5 },
                        itemStyle: { color: C.dark, borderWidth: 2, borderColor: "#fff" },
                        areaStyle: { color: C.dark, opacity: 0.08 },
                        label: lineLabel(C.dark)
                    }
                ]
            });
        } catch (e) {}
    });
}

/* ==========================================================================
   "الشكل الثاني": بطاقة مستقلة لكل إدارة عامة، تعرض إداراتها الفرعية كشريط أفقي مكدَّس
   (قائم/جديد بالقيمة) + بادج نسبة تحصيل عند نهاية كل شريط - يطابق الصورة المرجعية بالضبط
   ========================================================================== */
function buildAiSubDeptBarOption(rows) {
    const keySet = {};
    rows.forEach(function (r) { Object.keys(r.contractStatusValues || {}).forEach(function (k) { keySet[k] = true; }); });
    const keys = Object.keys(keySet);

    // مرتّبة تنازليًا من الخادم أصلاً حسب إجمالي القيمة؛ نعكسها هنا فقط لأن محور الفئات يرسم من الأسفل للأعلى
    const sorted = rows.slice().reverse();
    const labels = sorted.map(function (r) { return r.name; });
    const rates = sorted.map(function (r) { return r.totalValue > 0 ? Math.round((r.paidValue / r.totalValue) * 1000) / 10 : 0; });

    const barSeries = keys.map(function (key, i) {
        const color = aiStatusColor(key, i);
        const isLast = i === keys.length - 1;
        return {
            name: key, type: "bar", stack: "status", barMaxWidth: 20,
            itemStyle: { color: color },
            data: sorted.map(function (r) { return (r.contractStatusValues || {})[key] || 0; }),
            // البادج (نسبة التحصيل) يُكتب فقط على آخر سلسلة بالتكديس (نهاية الشريط) حتى لا يتكرر،
            // بلون يتغيّر حسب النسبة (أخضر مرتفعة/كهرماني متوسطة/أحمر منخفضة) عبر مفاتيح rich
            // مختلفة يختارها formatter لكل صف على حدة
            label: !isLast ? { show: false } : {
                show: true, position: "right", fontSize: 11, distance: 10,
                formatter: function (p) {
                    const rate = rates[p.dataIndex];
                    const total = sorted[p.dataIndex].totalValue;
                    const badgeKey = rate >= 70 ? "badgeGood" : (rate >= 40 ? "badgeMid" : "badgeLow");
                    return '{total|' + formatNum(total) + '}  {' + badgeKey + '| ' + rate + '% }';
                },
                rich: {
                    total: { fontSize: 12, fontWeight: 700, color: C.axisText },
                    badgeGood: { fontSize: 10, fontWeight: 800, color: "#fff", backgroundColor: C.dark, borderRadius: 8, padding: [4, 8] },
                    badgeMid: { fontSize: 10, fontWeight: 800, color: "#fff", backgroundColor: "#C98A1F", borderRadius: 8, padding: [4, 8] },
                    badgeLow: { fontSize: 10, fontWeight: 800, color: "#fff", backgroundColor: C.red, borderRadius: 8, padding: [4, 8] }
                }
            }
        };
    });

    return {
        tooltip: tooltipStyle({
            trigger: "axis", axisPointer: { type: "shadow" },
            formatter: function (params) {
                const idx = params[0].dataIndex;
                let html = '<div style="font-weight:700;margin-bottom:4px;">' + escapeHtml(labels[idx]) + '</div>';
                params.forEach(function (p) {
                    html += '<div>' + p.marker + p.seriesName + ': <strong>' + formatNum(p.value) + '</strong></div>';
                });
                html += '<div style="margin-top:4px;border-top:1px solid ' + C.grid + ';padding-top:4px;">نسبة التحصيل: <strong>' + rates[idx] + '%</strong></div>';
                return html;
            }
        }),
        legend: { bottom: 0, textStyle: Object.assign({}, AXIS_LABEL_STYLE), itemWidth: 10, itemHeight: 10 },
        // مساحة أوسع لأسماء البنود الفرعية عند بداية الشكل (يمين) حتى لا تتزاحم مع الأشرطة،
        // ومساحة أكبر يمينًا للنسبة والقيمة النهائية
        grid: { left: 150, right: 110, top: 10, bottom: 34 },
        xAxis: axisStyle({ type: "value", show: false }),
        yAxis: axisStyle({ type: "category", data: labels, axisLabel: { width: 140, overflow: "break", lineHeight: 16, fontSize: 12, fontWeight: 600 } }),
        series: barSeries
    };
}

// الشكل الثاني: بطاقة واحدة فقط تعرض الإدارات الفرعية لإدارة عامة واحدة، تتحدّث فورًا عند
// الضغط على أي بطاقة بالشكل الأول (بدل بطاقة مستقلة لكل إدارة عامة)
function renderAiSubDeptSingle(row, ai) {
    if (!row || !ai.deptGeneralLabels || !ai.deptGeneralLabels.length || !ai.subDepartments) return;
    row.classList.add("tc-row-single");

    const firstLabel = ai.deptGeneralLabels[0];
    aiSelectedDeptLabel = firstLabel;
    const rows = ai.subDepartments.filter(function (r) { return r.deptGeneral === firstLabel; });
    if (!rows.length) return;

    aiSubDeptSingleBoxId = "aiSubDeptSingle_" + Date.now();
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title"><i class="bi ' + aiDeptIcon(firstLabel) + '" id="aiSubDeptIcon"></i> <span id="aiSubDeptTitle">' + escapeHtml(firstLabel) + '</span></div>' +
        '</div>' +
        '<div class="chart-subtitle" style="margin-bottom:8px;">تفصيل محصل القائم والجديد</div>' +
        '<div class="echart-box" id="' + aiSubDeptSingleBoxId + '" style="height:' + Math.max(220, rows.length * 60) + 'px;"></div>';
    row.appendChild(div);

    aiSubDeptSingleCardEl = div;
    aiSubDeptSingleChart = echarts.init(document.getElementById(aiSubDeptSingleBoxId));
    chartRegistry.push(aiSubDeptSingleChart);
    try { aiSubDeptSingleChart.setOption(buildAiSubDeptBarOption(rows)); } catch (e) {}
}

// يستدعيها الضغط على بطاقة إدارة عامة بالشكل الأول (renderAiDeptCards) لتحديث الشكل الثاني
function aiUpdateSubDeptSingle(ai, deptLabel) {
    if (!aiSubDeptSingleChart || !ai.subDepartments) return;
    const rows = ai.subDepartments.filter(function (r) { return r.deptGeneral === deptLabel; });
    if (!rows.length) return;

    const box = document.getElementById(aiSubDeptSingleBoxId);
    if (box) box.style.height = Math.max(220, rows.length * 60) + "px";
    try { aiSubDeptSingleChart.setOption(buildAiSubDeptBarOption(rows), true); } catch (e) {}
    try { aiSubDeptSingleChart.resize(); } catch (e) {}

    if (aiSubDeptSingleCardEl) {
        const titleEl = aiSubDeptSingleCardEl.querySelector("#aiSubDeptTitle");
        if (titleEl) titleEl.textContent = deptLabel;
        const iconEl = aiSubDeptSingleCardEl.querySelector("#aiSubDeptIcon");
        if (iconEl) iconEl.className = "bi " + aiDeptIcon(deptLabel);
    }
}

/* ==========================================================================
   تصدير Excel/PDF - بنفس فلاتر الصفحة الحالية (الربع/الشهر/رقم العقد/الإدارة العامة
   والفرعية/نوع الاستثمار)، بنفس نمط تصدير صفحة "الطرح" (wwwroot/js/tarh.js)
   ========================================================================== */
function aiExportQueryString() {
    const qs = (typeof currentFilterParams === "function") ? currentFilterParams().toString() : "";
    const params = new URLSearchParams(qs);
    const deptFilters = aiExtractDeptFilters();
    if (deptFilters) {
        params.set("deptGeneral", deptFilters.deptGeneral);
        if (deptFilters.deptSub) params.set("deptSub", deptFilters.deptSub);
    }
    if (aiInvestmentTypeFilter) params.set("investmentType", aiInvestmentTypeFilter);
    const s = params.toString();
    return s ? "?" + s : "";
}

function aiFormatExportDate(iso) {
    if (!iso) return "";
    const d = new Date(iso);
    return isNaN(d.getTime()) ? "" : d.toLocaleDateString("en-GB");
}

function aiInitExport() {
    const excelBtn = document.getElementById("aiExportExcelBtn");
    const pdfBtn = document.getElementById("aiExportPdfBtn");
    if (!window.AI_EXPORT_EXCEL_URL && !window.AI_EXPORT_DATA_URL) return;

    if (excelBtn && window.AI_EXPORT_EXCEL_URL) {
        excelBtn.addEventListener("click", function () {
            window.location.href = window.AI_EXPORT_EXCEL_URL + aiExportQueryString();
        });
    }

    if (pdfBtn && window.AI_EXPORT_DATA_URL) {
        pdfBtn.addEventListener("click", async function () {
            pdfBtn.disabled = true;
            try {
                const resp = await fetch(window.AI_EXPORT_DATA_URL + aiExportQueryString());
                const rows = await resp.json();

                const tbody = document.getElementById("aiExportTableBody");
                if (tbody) {
                    tbody.innerHTML = rows.map(function (r) {
                        return '<tr>' +
                            '<td>' + escapeHtml(r.contractNumber || "") + '</td>' +
                            '<td>' + escapeHtml(r.investorName || "") + '</td>' +
                            '<td>' + aiFormatExportDate(r.createdDate) + '</td>' +
                            '<td>' + aiFormatExportDate(r.recordDate) + '</td>' +
                            '<td>' + escapeHtml(r.paymentNumber || "") + '</td>' +
                            '<td>' + formatNum(r.totalValue || 0) + '</td>' +
                            '<td>' + escapeHtml(r.subPortfolio || "") + '</td>' +
                        '</tr>';
                    }).join("");
                }

                const dateEl = document.getElementById("aiPrintDate");
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

document.addEventListener("DOMContentLoaded", aiInitExport);
