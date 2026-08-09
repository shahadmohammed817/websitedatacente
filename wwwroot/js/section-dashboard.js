/* ==========================================================================
   ألوان الهوية
   ========================================================================== */
const C = {
    dark:  "#0E6E61",
    mid:   "#3F8F87",
    light: "#5FA89D",
    lime:  "#8DC63F",
    red:   "#D6534A",
    text:  "#173B36",
    muted: "#6E8A86",
    grid:  "#E1ECEA",
    // لون موحّد وواضح لكل نصوص المحاور (أرقام/تسميات X و Y) بجميع الرسوم البيانية
    axisText: "#181818"
};

// نمط موحّد لكل تسميات المحاور بجميع الرسوم: أسود واضح، حجم 13-14px، وزن 600
const AXIS_LABEL_STYLE = { color: C.axisText, fontSize: 13, fontWeight: 600 };

function axisStyle(extra) {
    extra = extra || {};
    const base = {
        axisLine:  { lineStyle: { color: C.grid } },
        axisLabel: Object.assign({}, AXIS_LABEL_STYLE),
        splitLine: { lineStyle: { color: C.grid, type: "dashed" } }
    };
    const merged = Object.assign({}, base, extra);
    // دمج عميق لـ axisLabel حتى لا يفقد اللون/الحجم الافتراضي عند تمرير rotate أو formatter فقط
    merged.axisLabel = Object.assign({}, AXIS_LABEL_STYLE, extra.axisLabel || {});
    return merged;
}

function tooltipStyle(extra) {
    return Object.assign({
        backgroundColor: "#FFFFFF",
        borderColor: C.grid,
        borderWidth: 1,
        textStyle: { color: C.text, fontSize: 12 },
        extraCssText: "box-shadow:0 4px 16px rgba(0,0,0,0.1);border-radius:10px;"
    }, extra || {});
}

function formatNum(n) {
    if (!Number.isFinite(n)) return "0";
    return Math.round(n).toLocaleString("en-US");
}

function formatCompact(n) {
    if (Math.abs(n) >= 1e9) return (n / 1e9).toFixed(1) + "B";
    if (Math.abs(n) >= 1e6) return (n / 1e6).toFixed(1) + "M";
    if (Math.abs(n) >= 1e3) return (n / 1e3).toFixed(1) + "K";
    return n.toFixed(0);
}

/* أيقونات Bootstrap Icons الرسمية (بديلة للإيموجيات) */
const ICONS = {
    wallet:   '<i class="bi bi-wallet2"></i>',
    check:    '<i class="bi bi-check-circle"></i>',
    alert:    '<i class="bi bi-exclamation-triangle"></i>',
    trending: '<i class="bi bi-graph-up-arrow"></i>',
    search:   '<i class="bi bi-search"></i>'
};

/* ==========================================================================
   شريط التصفية (رقم العقد + الربع): يُبنى مرة واحدة فوق أي لوحة بيانات،
   ويُعاد تحميل اللوحة الحالية تلقائيًا (بدون تغيير العنوان/الأيقونة) عند أي تغيير
   ========================================================================== */
const QUARTER_OPTIONS = [
    { value: "", label: "كل الأرباع" },
    { value: "Q1", label: "الربع الأول (Q1)" },
    { value: "Q2", label: "الربع الثاني (Q2)" },
    { value: "Q3", label: "الربع الثالث (Q3)" },
    { value: "Q4", label: "الربع الرابع (Q4)" }
];

let currentBaseUrl = null;   // آخر رابط لوحة تم فتحه (بدون معاملات تصفية) — يُستخدم لإعادة البناء عند تغيير الفلاتر
let filterDebounce = null;

/* --------------------------------------------------------------------------
   عناصر مساعدة لبناء حقول الفلتر (قائمة اختيار / بحث نصي) بنفس التنسيق الموحّد
   -------------------------------------------------------------------------- */
function makeSelectWrap(id, iconClass) {
    const wrap = document.createElement("div");
    wrap.className = "filter-select-wrap";
    wrap.innerHTML = '<i class="bi ' + iconClass + ' filter-icon"></i>';
    const select = document.createElement("select");
    select.id = id;
    select.className = "filter-select";
    wrap.appendChild(select);
    wrap.insertAdjacentHTML("beforeend", '<i class="bi bi-chevron-down filter-chevron"></i>');
    return { wrap: wrap, select: select };
}

function fillSelect(select, options, placeholder) {
    select.innerHTML = "";
    const optAll = document.createElement("option");
    optAll.value = "";
    optAll.textContent = placeholder;
    select.appendChild(optAll);
    options.forEach(function (opt) {
        const o = document.createElement("option");
        o.value = opt.value;
        o.textContent = opt.label;
        select.appendChild(o);
    });
}

function makeSearchWrap(id, placeholder) {
    const wrap = document.createElement("div");
    wrap.className = "filter-search-wrap";
    wrap.innerHTML = '<i class="bi bi-search filter-icon"></i>';
    const input = document.createElement("input");
    input.type = "text";
    input.id = id;
    input.className = "filter-search-input";
    input.placeholder = placeholder;
    wrap.appendChild(input);
    return { wrap: wrap, input: input };
}

/* --------------------------------------------------------------------------
   الرئيسية (عام): الأمانة/البلديات ← البند الرئيسي ← البند الفرعي (متسلسل)
   يعتمد على window.AMANAH_SECTIONS / window.BALADIYAH_SECTIONS المضمّنة بالصفحة
   -------------------------------------------------------------------------- */
function buildHomeFilters(bar) {
    const scopeSel = makeSelectWrap("filterScope", "bi-diagram-3");
    fillSelect(scopeSel.select, [
        { value: "Amanah", label: "الأمانة" },
        { value: "Baladiyah", label: "البلديات" }
    ], "الأمانة والبلديات (الكل)");
    bar.appendChild(scopeSel.wrap);

    const mainSel = makeSelectWrap("filterMainItem", "bi-folder2");
    bar.appendChild(mainSel.wrap);

    const subSel = makeSelectWrap("filterSubItem", "bi-folder");
    fillSelect(subSel.select, [], "البند الفرعي");
    subSel.select.disabled = true;
    bar.appendChild(subSel.wrap);

    // يدمج شجرتي الأمانة والبلديات بنفس اسم البند الرئيسي/الفرعي - يُستخدم فقط حين لا يكون
    // نطاق محدد بعد (الحالة الافتراضية عند فتح الصفحة)، حتى تظهر البنود الرئيسية فورًا دون
    // إلزام المستخدم باختيار أمانة/بلديات أولاً
    function mergedTree() {
        const byName = {};
        const order = [];
        [window.AMANAH_SECTIONS || [], window.BALADIYAH_SECTIONS || []].forEach(function (list) {
            list.forEach(function (s) {
                if (!byName[s.name]) { byName[s.name] = { name: s.name, ids: [], subsByName: {}, subsOrder: [] }; order.push(s.name); }
                byName[s.name].ids.push(s.id);
                (s.subs || []).forEach(function (sub) {
                    var item = byName[s.name];
                    if (!item.subsByName[sub.name]) { item.subsByName[sub.name] = { name: sub.name, ids: [] }; item.subsOrder.push(sub.name); }
                    item.subsByName[sub.name].ids.push(sub.id);
                });
            });
        });
        return order.map(function (name) {
            var item = byName[name];
            return { name: item.name, ids: item.ids, subs: item.subsOrder.map(function (sn) { return item.subsByName[sn]; }) };
        });
    }

    function currentTree() {
        if (scopeSel.select.value === "Amanah") return window.AMANAH_SECTIONS || [];
        if (scopeSel.select.value === "Baladiyah") return window.BALADIYAH_SECTIONS || [];
        return mergedTree(); // لا نطاق محدد = الكل (دمج الأمانة والبلديات بالاسم)
    }

    function idsOf(node) { return (node.ids || [node.id]).join(","); }

    function populateMainSelect() {
        const tree = currentTree();
        fillSelect(mainSel.select, tree.map(function (s) { return { value: idsOf(s), label: s.name }; }), "البند الرئيسي");
        mainSel.select.disabled = tree.length === 0;
        fillSelect(subSel.select, [], "البند الفرعي");
        subSel.select.disabled = true;
    }

    populateMainSelect(); // تعبئة فورية بالبنود المدمجة فور فتح الصفحة، قبل اختيار أي نطاق

    scopeSel.select.addEventListener("change", function () {
        populateMainSelect();
        applyDashboardFilters();
    });

    mainSel.select.addEventListener("change", function () {
        const tree = currentTree();
        const node = tree.filter(function (s) { return idsOf(s) === mainSel.select.value; })[0];
        const subs = (node && node.subs) ? node.subs : [];
        fillSelect(subSel.select, subs.map(function (s) { return { value: idsOf(s), label: s.name }; }), "البند الفرعي");
        subSel.select.disabled = subs.length === 0;
        applyDashboardFilters();
    });

    subSel.select.addEventListener("change", applyDashboardFilters);
}

/* --------------------------------------------------------------------------
   بيانات الأمانة: الإدارة العامة (محافظ الاستثمارات) ← الإدارة الفرعية (متسلسل)
   يعتمد على window.AMANAH_DEPT_GROUPS المضمّنة بالصفحة
   -------------------------------------------------------------------------- */
function buildAmanahDeptFilters(bar) {
    const groups = window.AMANAH_DEPT_GROUPS || [];

    const deptSel = makeSelectWrap("filterDept", "bi-diagram-3");
    fillSelect(deptSel.select, groups.map(function (g) { return { value: g.name, label: g.name }; }), "الإدارة العامة");
    bar.appendChild(deptSel.wrap);

    const subDeptSel = makeSelectWrap("filterSubDept", "bi-diagram-2");
    fillSelect(subDeptSel.select, [], "الإدارة الفرعية");
    subDeptSel.select.disabled = true;
    bar.appendChild(subDeptSel.wrap);

    deptSel.select.addEventListener("change", function () {
        const group = groups.filter(function (g) { return g.name === deptSel.select.value; })[0];
        const subs = (group && group.subs) ? group.subs : [];
        fillSelect(subDeptSel.select, subs.map(function (s) { return { value: s, label: s }; }), "الإدارة الفرعية");
        subDeptSel.select.disabled = subs.length === 0;
        applyDashboardFilters();
    });

    subDeptSel.select.addEventListener("change", applyDashboardFilters);
}

/* --------------------------------------------------------------------------
   بيانات البلديات: قائمة اختيار البلدية
   يعتمد على window.BALADIYAH_ENTITIES المضمّنة بالصفحة
   -------------------------------------------------------------------------- */
function buildBaladiyahEntityFilter(bar) {
    const entities = window.BALADIYAH_ENTITIES || [];
    const entitySel = makeSelectWrap("filterEntity", "bi-buildings");
    fillSelect(entitySel.select, entities.map(function (e) { return { value: e, label: e }; }), "اختر البلدية");
    bar.appendChild(entitySel.wrap);
    entitySel.select.addEventListener("change", applyDashboardFilters);
}

function buildFilterBar() {
    if (document.getElementById("dashFilterBar")) return; // بُني مسبقًا
    const headerRow = document.querySelector(".dash-header-row");
    if (!headerRow) return;

    const bar = document.createElement("div");
    bar.id = "dashFilterBar";
    bar.className = "dash-filter-bar";

    const mode = window.FILTER_MODE || "";
    if (mode === "home") buildHomeFilters(bar);
    else if (mode === "amanah-scope") buildAmanahDeptFilters(bar);
    else if (mode === "baladiyah-scope") buildBaladiyahEntityFilter(bar);

    // رقم العقد + الربع: مشتركان بكل الصفحات، تُبقيان كما هما دائمًا
    const search = makeSearchWrap("filterContractNumber", "ابحث برقم العقد...");
    bar.appendChild(search.wrap);
    search.input.addEventListener("input", function () {
        clearTimeout(filterDebounce);
        filterDebounce = setTimeout(applyDashboardFilters, 400); // تأخير بسيط حتى لا يُرسل طلب مع كل حرف
    });

    const quarterSel = makeSelectWrap("filterQuarter", "bi-calendar3");
    QUARTER_OPTIONS.forEach(function (opt) {
        const o = document.createElement("option");
        o.value = opt.value;
        o.textContent = opt.label;
        quarterSel.select.appendChild(o);
    });
    bar.appendChild(quarterSel.wrap);
    quarterSel.select.addEventListener("change", applyDashboardFilters);

    headerRow.appendChild(bar);
}

function currentFilterParams() {
    const contractEl = document.getElementById("filterContractNumber");
    const quarterEl = document.getElementById("filterQuarter");
    const params = new URLSearchParams();
    const contract = contractEl ? contractEl.value.trim() : "";
    const quarter = quarterEl ? quarterEl.value : "";
    if (contract) params.set("contract", contract);
    if (quarter) params.set("quarter", quarter);
    return params;
}

function appendFilterParams(url) {
    const qs = currentFilterParams().toString();
    if (!qs) return url;
    return url + (url.indexOf("?") > -1 ? "&" : "?") + qs;
}

/* --------------------------------------------------------------------------
   يحسب رابط اللوحة الأساسي (بدون رقم العقد/الربع) من حالة فلاتر البند/النطاق/البلدية
   الحالية حسب وضع الصفحة (window.FILTER_MODE) - يُستدعى عند تغيّر أي فلتر من هذا النوع
   -------------------------------------------------------------------------- */
function computeBaseUrlFromFilters() {
    const mode = window.FILTER_MODE || "";

    if (mode === "home") {
        // /Dashboard/Sections/Data?ids= يقبل معرّفًا واحدًا أو عدّة معرّفات مفصولة بفواصل (تُستخدم
        // عند دمج بند بنفس الاسم من الأمانة والبلديات معًا حين لا يكون نطاق محدد بعد)
        const scopeEl = document.getElementById("filterScope");
        const mainEl = document.getElementById("filterMainItem");
        const subEl = document.getElementById("filterSubItem");
        if (subEl && subEl.value) return "/Dashboard/Sections/Data?ids=" + subEl.value;
        if (mainEl && mainEl.value) return "/Dashboard/Sections/Data?ids=" + mainEl.value;
        if (scopeEl && scopeEl.value) return "/Dashboard/Scope/" + scopeEl.value + "/Data";
        return window.DEFAULT_DASHBOARD_URL;
    }

    if (mode === "amanah-scope") {
        const deptEl = document.getElementById("filterDept");
        const subDeptEl = document.getElementById("filterSubDept");
        const dept = deptEl ? deptEl.value : "";
        const sub = subDeptEl ? subDeptEl.value : "";
        if (!dept) return window.DEFAULT_DASHBOARD_URL;
        const params = new URLSearchParams({ deptGeneral: dept });
        if (sub) params.set("deptSub", sub);
        return "/Dashboard/AmanahInvestment/Data?" + params.toString();
    }

    if (mode === "baladiyah-scope") {
        const entityEl = document.getElementById("filterEntity");
        return (entityEl && entityEl.value)
            ? "/Dashboard/Entity/Data?name=" + encodeURIComponent(entityEl.value)
            : window.DEFAULT_DASHBOARD_URL;
    }

    return currentBaseUrl;
}

/* --------------------------------------------------------------------------
   عنوان/أيقونة رأس اللوحة حسب فلاتر البند/النطاق/البلدية الحالية
   -------------------------------------------------------------------------- */
function currentFilterMeta() {
    const mode = window.FILTER_MODE || "";

    function selectedLabel(id) {
        const el = document.getElementById(id);
        return (el && el.value && el.selectedIndex >= 0) ? el.options[el.selectedIndex].textContent : null;
    }

    if (mode === "home") {
        return { nameAr: selectedLabel("filterSubItem") || selectedLabel("filterMainItem") || selectedLabel("filterScope") || "عام", nameEn: "" };
    }
    if (mode === "amanah-scope") {
        return { nameAr: selectedLabel("filterSubDept") || selectedLabel("filterDept") || "بنود الأمانة", nameEn: "" };
    }
    if (mode === "baladiyah-scope") {
        return { nameAr: selectedLabel("filterEntity") || "بنود البلدية", nameEn: "" };
    }
    return null;
}

function applyDashboardFilters() {
    const mode = window.FILTER_MODE || "";
    if (mode === "home" || mode === "amanah-scope" || mode === "baladiyah-scope") {
        loadDashboard(computeBaseUrlFromFilters(), currentFilterMeta(), true);
        return;
    }
    if (!currentBaseUrl) return;
    loadDashboard(appendFilterParams(currentBaseUrl), null, false);
}

// رمز الريال السعودي الرسمي الجديد (يَرِث لون النص) — يُلحق بالقيم المالية
const SAR_SYMBOL = '<svg class="kpi-sar" viewBox="0 0 1124.14 1256.39" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-label="ريال سعودي">' +
    '<path d="M699.62,1113.02h0c-20.06,44.48-33.32,92.75-38.4,143.37l424.51-90.24c20.06-44.47,33.31-92.75,38.4-143.37l-424.51,90.24Z"/>' +
    '<path d="M1085.73,895.8c20.06-44.47,33.32-92.75,38.4-143.37l-330.68,70.33v-135.2l292.27-62.11c20.06-44.47,33.32-92.75,38.4-143.37l-330.68,70.27V66.13c-50.67,28.45-95.67,66.32-132.25,110.99v403.35l-132.25,28.11V0c-50.67,28.44-95.67,66.32-132.25,110.99v525.69l-295.91,62.88c-20.06,44.47-33.33,92.75-38.42,143.37l334.33-71.05v170.26l-358.3,76.14c-20.06,44.47-33.32,92.75-38.4,143.37l375.04-79.7c30.53-6.35,56.77-24.4,73.83-49.24l68.78-101.97v-.02c7.14-10.55,11.3-23.27,11.3-36.97v-149.98l132.25-28.11v270.4l424.53-90.28Z"/></svg>';

let chartRegistry = [];

// حالة اللوحة الحالية + التصفية التفاعلية (Cross-Filter بأسلوب Power BI)
let activeDashboardData = null;
let activeCategoryIndex = null;
let charts = { bar: null, line: null, subItemsBar: null, gauge: null, entityBar: null, topItems: null, quarter: null };

/* ==========================================================================
   تفريغ/تدمير كل الرسوم الحالية استعدادًا لتحميل لوحة جديدة (تفاعل القائمة الجانبية)
   ========================================================================== */
function disposeCharts() {
    chartRegistry.forEach(function (c) { try { c.dispose(); } catch (e) {} });
    chartRegistry = [];
    charts = { bar: null, line: null, subItemsBar: null, gauge: null, entityBar: null, topItems: null, quarter: null };
    activeDashboardData = null;
    activeCategoryIndex = null;
}

function clearContainers() {
    ["kpiGrid", "chartsRow1", "chartsRow2"].forEach(function (id) {
        const el = document.getElementById(id);
        if (el) el.innerHTML = "";
    });
    removeFilterBanner();
}

function toggleNoData(show) {
    const grids = document.getElementById("dashboardGrids");
    const noData = document.getElementById("noDataBox");
    if (grids) grids.style.display = show ? "none" : "";
    if (noData) noData.style.display = show ? "" : "none";
}

function updateDashHeader(meta) {
    if (!meta) return;
    const iconEl = document.getElementById("dashIconBadge");
    const titleAr = document.getElementById("dashTitleAr");
    const titleEn = document.getElementById("dashTitleEn");
    if (iconEl && meta.icon) iconEl.innerHTML = meta.icon; // أيقونة SVG/Bootstrap للخيار المختار
    if (titleAr && meta.nameAr) titleAr.textContent = meta.nameAr;
    if (titleEn && meta.nameEn) titleEn.textContent = meta.nameEn;
}

/* ==========================================================================
   تحميل لوحة بيانات من رابط معيّن وعرضها مكان اللوحة الحالية دون أي تنقّل صفحات
   ========================================================================== */
async function loadDashboard(url, meta, isBaseNavigation) {
    if (isBaseNavigation === undefined) isBaseNavigation = true;
    if (isBaseNavigation) currentBaseUrl = url; // نتذكر الرابط الأساسي (بدون فلاتر) لإعادة استخدامه عند تغيّر التصفية
    const finalUrl = isBaseNavigation ? appendFilterParams(url) : url;

    try {
        const resp = await fetch(finalUrl);
        if (!resp.ok) { toggleNoData(true); return; }
        const data = await resp.json();

        buildFilterBar();
        disposeCharts();
        clearContainers();
        updateDashHeader(meta);

        if (!data.hasData) {
            toggleNoData(true);
            return;
        }

        toggleNoData(false);
        activeDashboardData = data;
        activeCategoryIndex = null;

        renderKPIs(data);
        // الصف الأول: أعلى بنود الإيرادات جنب توزيع البنود الفرعية (نفس زوج البند الرئيسي/الفرعي
        // بصندوق الفلاتر)، ثم توزيع الإيرادات حسب الأرباع
        renderTopItemsChart(data);
        renderSubItemsBarChart(data);
        renderQuarterChart(data);
        // الصف الثاني: الإيرادات الشهرية لكل جهة (لاين شارت) بعرض كامل وطول أكبر
        renderEntityMonthChart(data);
    } catch (err) {
        console.error("تعذر تحميل بيانات اللوحة:", err);
        toggleNoData(true);
    }
}

document.addEventListener("DOMContentLoaded", async function () {
    // صفحة بند فردي (Dashboard/Section): تحميل مباشر لبيانات ذلك البند بلا فلاتر بند
    if (window.SECTION_DATA_URL) {
        await loadDashboard(window.SECTION_DATA_URL, null);
    }

    if (window.DEFAULT_DASHBOARD_URL) {
        await loadDashboard(window.DEFAULT_DASHBOARD_URL, null);
    }

    window.addEventListener("resize", function () {
        chartRegistry.forEach(function (c) { try { c.resize(); } catch (e) {} });
    });
});

/* ==========================================================================
   التصفية التفاعلية بين الرسوم (Cross-Filter بأسلوب Power BI)
   عند الضغط على أي بند/فئة بالرسم البياني، تُعاد حساب كل البطاقات والرسوم
   الأخرى فورًا من نفس البيانات المحمّلة أصلًا دون أي طلب إضافي للخادم.
   ========================================================================== */
function buildFilteredView(data, idx) {
    if (idx === null || idx === undefined || !data.categoryDetails || !data.categoryDetails[idx]) {
        return data;
    }
    const d = data.categoryDetails[idx];
    const collectionRate = d.total > 0 ? (d.collected / d.total * 100) : 0;
    const targetPercent = d.target > 0 ? (d.collected / d.target * 100) : collectionRate;

    const hasOwnTrend = d.trendLabels && d.trendLabels.length;
    return {
        hasData: true,
        totalRevenue: d.total,
        collectedRevenue: d.collected,
        uncollectedRevenue: d.uncollected,
        collectionRatePercent: collectionRate,
        targetAchievementPercent: targetPercent,
        trendLabels: hasOwnTrend ? d.trendLabels : data.trendLabels,
        trendValues: hasOwnTrend ? d.trendValues : data.trendValues,
        trendCollected: hasOwnTrend ? d.trendCollected : data.trendCollected,
        trendUncollected: hasOwnTrend ? d.trendUncollected : data.trendUncollected,
        categoryLabels: data.categoryLabels,
        categoryCollected: data.categoryCollected,
        categoryUncollected: data.categoryUncollected
    };
}

function applyCategoryFilter(idx) {
    if (!activeDashboardData) return;
    activeCategoryIndex = idx;
    const view = buildFilteredView(activeDashboardData, idx);

    renderKPIs(view);
    if (charts.subItemsBar) charts.subItemsBar.setOption(buildSubItemsBarOption(view));
    if (charts.gauge) charts.gauge.setOption(buildGaugeOption(view));
    if (charts.line) refreshLineChart(view);

    updateFilterBanner(idx);
}

function updateFilterBanner(idx) {
    const grid = document.getElementById("kpiGrid");
    if (!grid) return;
    removeFilterBanner();

    if (idx === null || idx === undefined || !activeDashboardData || !activeDashboardData.categoryLabels) return;

    const label = activeDashboardData.categoryLabels[idx];
    if (!label) return;

    const banner = document.createElement("div");
    banner.id = "crossFilterBanner";
    banner.style.cssText = "display:flex;align-items:center;gap:8px;background:rgba(14,110,97,0.08);" +
        "border:1px solid rgba(14,110,97,0.25);color:var(--brand-dark);border-radius:10px;" +
        "padding:8px 14px;font-size:.85rem;font-weight:700;margin-bottom:12px;width:fit-content;";
    banner.innerHTML = '<span class="cf-search">' + ICONS.search + '</span><span>مُصفّى حسب: ' + escapeHtml(label) + '</span>' +
        '<button type="button" id="clearCrossFilterBtn" style="border:none;background:transparent;' +
        'color:var(--brand-dark);font-weight:800;cursor:pointer;font-size:1rem;line-height:1;">×</button>';

    grid.parentElement.insertBefore(banner, grid);

    document.getElementById("clearCrossFilterBtn").addEventListener("click", function () {
        if (charts.bar) charts.bar.__clearHighlight && charts.bar.__clearHighlight();
        applyCategoryFilter(null);
    });
}

function removeFilterBanner() {
    const el = document.getElementById("crossFilterBanner");
    if (el) el.remove();
}

function escapeHtml(str) {
    const div = document.createElement("div");
    div.textContent = str;
    return div.innerHTML;
}

/* ==========================================================================
   KPI Cards
   ========================================================================== */
function renderKPIs(data) {
    const grid = document.getElementById("kpiGrid");
    if (!grid) return;

    const uncollectedPercent = data.totalRevenue > 0
        ? (data.uncollectedRevenue / data.totalRevenue * 100)
        : 0;

    const cards = [
        { cls: "c1", icon: ICONS.wallet,   label: "إجمالي الإيرادات",       value: formatNum(data.totalRevenue),       money: true },
        { cls: "c2", icon: ICONS.check,    label: "الإيرادات المحصّلة",     value: formatNum(data.collectedRevenue),   money: true },
        { cls: "c3", icon: ICONS.alert,    label: "الإيرادات غير المحصّلة", value: formatNum(data.uncollectedRevenue), money: true },
        { cls: "c4", icon: ICONS.trending, label: "نسبة التحصيل",           value: data.collectionRatePercent.toFixed(1) + "%", money: false },
        { cls: "c5", icon: ICONS.alert,    label: "نسبة المتبقي (غير المحصّل)", value: uncollectedPercent.toFixed(1) + "%", money: false }
    ];

    grid.innerHTML = cards.map(function (c) {
        var val = c.money ? c.value + ' <span class="value-sar">' + SAR_SYMBOL + '</span>' : c.value;
        return '<div class="kpi-card ' + c.cls + '">' +
            '<div class="icon">' + c.icon + '</div>' +
            '<div class="label">' + c.label + '</div>' +
            '<div class="value">' + val + '</div>' +
            '</div>';
    }).join("");
}

/* ==========================================================================
   Entity Month Chart — الإيرادات الشهرية لكل جهة (أمانة المدينة + كل بلدية)
   مبني على عمود "تاريخ الدفع"، والضغط على أي عمود/جهة بالتبويب يفتح لوحة تلك الجهة
   ========================================================================== */
const ENTITY_BASE_PALETTE = [C.dark, C.mid, C.lime, C.light, C.red, "#0A5A4F", "#B9821F", "#3E6FA8"];

function entityColor(i) {
    if (i < ENTITY_BASE_PALETTE.length) return ENTITY_BASE_PALETTE[i];
    // ألوان إضافية موزّعة على عجلة الألوان لضمان تمييز أي عدد من الجهات
    const hue = (i * 47) % 360;
    return "hsl(" + hue + ", 52%, 42%)";
}

function renderEntityMonthChart(data) {
    const row1 = document.getElementById("chartsRow2");
    if (!row1 || !data.entityMonthLabels || !data.entityMonthLabels.length || !data.entityMonthlySeries || !data.entityMonthlySeries.length) return;

    const id = "entityChart_" + Date.now();
    const div = document.createElement("div");
    div.className = "chart-card";
    div.style.cssText = "grid-column:1 / -1;"; // عرض كامل الصف
    var titleText = escapeHtml(data.entityChartTitle || "الإيرادات الشهرية لكل جهة (أمانة/بلدية)");
    var badgeText = escapeHtml(data.entityChartBadge || "اضغط لعرض الجهة");
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">' + titleText + '</div>' +
            '<div class="chart-badge">' + badgeText + '</div>' +
        '</div>' +
        '<div class="echart-box" id="' + id + '" style="height:400px;"></div>';
    row1.appendChild(div);

    const chart = echarts.init(document.getElementById(id));
    const isAggregated = !!data.entityChartIsAggregated;
    // رسم خطي حاد (Sharp Line): كل جهة خط مستقل بلون هويته، بدون تنعيم للزوايا
    // (smooth: false) حتى تبقى نقاط التغيّر الشهري واضحة عند المقارنة بين الأمانة والبلديات
    const series = data.entityMonthlySeries.map(function (s, i) {
        // لوحة "عام": لونان واضحان ومتباينان (الأمانة/البلديات) بدل تدرّج الألوان
        // متعدد الجهات المخصص للوحات التفصيلية
        const color = isAggregated
            ? (s.entity.indexOf("البلد") === -1 ? C.dark : C.lime)
            : entityColor(i);
        return {
            name: s.entity, type: "line", smooth: false,
            data: s.values,
            symbol: "circle", symbolSize: 7,
            lineStyle: { color: color, width: 3 },
            itemStyle: { color: color, borderWidth: 2, borderColor: "#fff" },
            emphasis: { focus: "series", scale: true },
            // الأرقام تظهر دائمًا فوق كل نقطة (بدل الاعتماد على تلميح hover فقط)
            label: {
                show: true, position: "top", distance: 6,
                formatter: function (p) { return p.value ? formatCompact(p.value) : ""; },
                color: color, fontSize: 11, fontWeight: 700
            }
        };
    });

    chart.setOption({
        tooltip: tooltipStyle({
            trigger: "axis",
            formatter: function (params) {
                let html = '<div style="font-weight:700;margin-bottom:6px;">' + params[0].axisValue + '</div>';
                let sum = 0;
                params.forEach(function (p) {
                    if (!p.value) return;
                    sum += p.value;
                    html += '<div style="display:flex;align-items:center;gap:6px;">' +
                        '<span style="display:inline-block;width:10px;height:10px;border-radius:50%;background:' + p.color + '"></span>' +
                        '<span>' + p.seriesName + ': <strong>' + formatNum(p.value) + '</strong></span></div>';
                });
                html += '<div style="margin-top:4px;border-top:1px solid ' + C.grid + ';padding-top:4px;">الإجمالي: <strong>' + formatNum(sum) + '</strong></div>';
                return html;
            }
        }),
        legend: { type: "scroll", bottom: 0, textStyle: Object.assign({}, AXIS_LABEL_STYLE), itemWidth: 10, itemHeight: 10 },
        grid: { left: 55, right: 16, top: 20, bottom: 64 },
        xAxis: axisStyle({ type: "category", boundaryGap: false, data: data.entityMonthLabels, axisLabel: { rotate: data.entityMonthLabels.length > 6 ? 35 : 0 } }),
        yAxis: axisStyle({ type: "value", axisLabel: { formatter: formatCompact } }),
        series: series
    });

    chart.on("click", function (params) {
        const entityName = params.seriesName;
        if (!entityName) return;

        // لوحة "عام": كل عمود هو إجمالي نطاق كامل (الأمانة/البلديات) وليس جهة فعلية،
        // فنوجّه الضغط لصفحة النطاق نفسها (نفس رابط القائمة الجانبية) بدل البحث عن
        // جهة بهذا الاسم الحرفي في قاعدة البيانات
        if (activeDashboardData && activeDashboardData.entityChartIsAggregated) {
            // لوحة "عام": كل عمود إجمالي نطاق كامل (الأمانة/البلديات) - ننقل المستخدم فعليًا
            // لصفحة ذلك النطاق بدل محاكاة نقرة على رابط قائمة جانبية لم يعد له وجود
            const wantAmanah = entityName.indexOf("البلد") === -1;
            window.location.href = "/Dashboard/Scope/" + (wantAmanah ? "Amanah" : "Baladiyah");
            return;
        }

        const url = "/Dashboard/Entity/Data?name=" + encodeURIComponent(entityName);
        const isAmanah = entityName.indexOf("بلدية") === -1;
        loadDashboard(url, {
            icon: isAmanah ? '<i class="bi bi-building"></i>' : '<i class="bi bi-buildings"></i>',
            nameAr: entityName,
            nameEn: ""
        });

        // إن وُجد مربع اختيار البلدية بصندوق الفلاتر (صفحة بيانات البلديات)، نُزامن قيمته
        // مع الجهة المضغوط عليها بالرسم دون إعادة تحميل إضافية
        const entitySelect = document.getElementById("filterEntity");
        if (entitySelect) entitySelect.value = entityName;
    });

    charts.entityBar = chart;
    chartRegistry.push(chart);
}

/* ==========================================================================
   Bar Chart — Collected vs Uncollected per Category (Power BI cross-filter)
   ========================================================================== */
function renderBarChart(data) {
    const row1 = document.getElementById("chartsRow1");
    if (!row1 || !data.categoryLabels || !data.categoryLabels.length) return;

    const id = "barChart_" + Date.now();
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">المحصّل مقابل غير المحصّل حسب الفئة</div>' +
            '<div class="chart-badge">اضغط على أي عمود للتصفية</div>' +
        '</div>' +
        '<div class="echart-box" id="' + id + '"></div>';
    row1.appendChild(div);

    const chart = echarts.init(document.getElementById(id));
    let highlighted = null;

    function makeOption(hl) {
        return {
            tooltip: tooltipStyle({
                trigger: "axis",
                axisPointer: { type: "shadow" },
                formatter: function (params) {
                    let html = '<div style="font-weight:700;margin-bottom:6px;">' + params[0].axisValue + '</div>';
                    params.forEach(function (p) {
                        html += '<div style="display:flex;align-items:center;gap:6px;">' +
                            '<span style="display:inline-block;width:10px;height:10px;border-radius:50%;background:' + p.color + '"></span>' +
                            '<span>' + p.seriesName + ': <strong>' + formatNum(p.value) + '</strong></span></div>';
                    });
                    return html;
                }
            }),
            legend: { bottom: 0, textStyle: Object.assign({}, AXIS_LABEL_STYLE), itemWidth: 12, itemHeight: 12 },
            grid: { left: 55, right: 16, top: 28, bottom: 48 },
            xAxis: axisStyle({ type: "category", data: data.categoryLabels, axisLabel: { rotate: data.categoryLabels.length > 6 ? 30 : 0 } }),
            yAxis: axisStyle({ type: "value", axisLabel: { formatter: formatCompact } }),
            series: [
                {
                    name: "المحصّل", type: "bar", barGap: "10%",
                    data: data.categoryCollected.map(function (v, i) {
                        const on = hl === null || hl === i;
                        return {
                            value: v,
                            itemStyle: { color: C.dark, opacity: on ? 1 : 0.25, borderRadius: [4, 4, 0, 0] },
                            label: {
                                show: true, position: "top", distance: 4,
                                formatter: function (p) { return formatCompact(p.value); },
                                color: C.axisText, fontSize: 12, fontWeight: 700,
                                opacity: on ? 1 : 0.35
                            }
                        };
                    })
                },
                {
                    name: "غير المحصّل", type: "bar",
                    data: data.categoryUncollected.map(function (v, i) {
                        const on = hl === null || hl === i;
                        return {
                            value: v,
                            itemStyle: { color: C.red, opacity: on ? 1 : 0.25, borderRadius: [4, 4, 0, 0] },
                            label: {
                                show: true, position: "top", distance: 4,
                                formatter: function (p) { return formatCompact(p.value); },
                                color: C.axisText, fontSize: 12, fontWeight: 700,
                                opacity: on ? 1 : 0.35
                            }
                        };
                    })
                }
            ]
        };
    }

    chart.setOption(makeOption(null));
    chart.on("click", function (params) {
        const idx = params.dataIndex;
        highlighted = highlighted === idx ? null : idx;
        chart.setOption(makeOption(highlighted));
        applyCategoryFilter(highlighted);
    });

    // يسمح لزر "إلغاء التصفية" بإعادة تعيين تظليل الأعمدة أيضًا
    chart.__clearHighlight = function () {
        highlighted = null;
        chart.setOption(makeOption(null));
    };

    charts.bar = chart;
    chartRegistry.push(chart);
}

/* ==========================================================================
   Top Items Chart — أعلى بنود الإيرادات (ترتيب تنازلي حسب إجمالي الإيراد)
   ========================================================================== */
function renderTopItemsChart(data) {
    const row1 = document.getElementById("chartsRow1");
    // يعتمد فقط على عمود "تصنيف البند" (البنود الرئيسية مثل: استثمار، رسوم، جزاءات...)
    // ولا يعرض أي بند فرعي، بصرف النظر عن مستوى اللوحة الحالية
    const hasTopItems = data.topItemLabels && data.topItemLabels.length;
    if (!row1 || !hasTopItems) return;

    const combined = data.topItemLabels.map(function (label, i) {
        return { label: label, total: data.topItemValues[i] || 0 };
    }).sort(function (a, b) { return b.total - a.total; }).slice(0, 8)
      .reverse(); // أعلى بند يظهر بالأعلى في الرسم الأفقي

    const id = "topItemsChart_" + Date.now();
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">أعلى بنود الإيرادات</div>' +
            '<div class="chart-badge">Top Items</div>' +
        '</div>' +
        '<div class="echart-box" id="' + id + '"></div>';
    row1.appendChild(div);

    const chart = echarts.init(document.getElementById(id));
    chart.setOption({
        tooltip: tooltipStyle({
            trigger: "axis",
            axisPointer: { type: "shadow" },
            formatter: function (params) {
                const p = params[0];
                return '<div style="font-weight:700;margin-bottom:4px;">' + p.name + '</div>' +
                       '<div>الإيرادات: <strong>' + formatNum(p.value) + '</strong></div>';
            }
        }),
        grid: { left: 10, right: 20, top: 10, bottom: 10, containLabel: true },
        xAxis: axisStyle({ type: "value", axisLabel: { show: false }, splitLine: { show: false } }),
        yAxis: axisStyle({
            type: "category",
            data: combined.map(function (x) { return x.label; }),
            axisLabel: { width: 220, overflow: "truncate", fontSize: 12 }
        }),
        series: [{
            name: "الإيرادات", type: "bar", barMaxWidth: 22,
            data: combined.map(function (x, i) {
                return { value: x.total, itemStyle: { color: DONUT_PALETTE[i % DONUT_PALETTE.length], borderRadius: [0, 4, 4, 0] } };
            })
        }]
    });

    charts.topItems = chart;
    chartRegistry.push(chart);
}

/* ==========================================================================
   Line Chart — Revenue Trend
   ========================================================================== */
function buildLineOption(data) {
    const total = data.trendValues || [];
    return {
        tooltip: tooltipStyle({
            trigger: "axis",
            formatter: function (params) {
                const p = params[0];
                return '<div style="font-weight:700;margin-bottom:6px;">' + p.axisValue + '</div>' +
                    '<div style="display:flex;align-items:center;gap:6px;">' +
                    '<span style="display:inline-block;width:10px;height:10px;border-radius:50%;background:' + p.color + '"></span>' +
                    '<span>إجمالي الإيرادات: <strong>' + formatNum(p.value) + '</strong></span></div>';
            }
        }),
        grid: { left: 60, right: 16, top: 20, bottom: 36 },
        xAxis: axisStyle({ type: "category", data: data.trendLabels, axisLabel: { rotate: data.trendLabels.length > 8 ? 35 : 0 } }),
        yAxis: axisStyle({ type: "value", axisLabel: { formatter: formatCompact } }),
        series: [
            {
                name: "إجمالي الإيرادات", type: "line", smooth: true, data: total,
                lineStyle: { color: C.dark, width: 3 },
                itemStyle: { color: C.dark },
                areaStyle: { color: { type: "linear", x: 0, y: 0, x2: 0, y2: 1,
                    colorStops: [{ offset: 0, color: "rgba(14,110,97,0.18)" }, { offset: 1, color: "rgba(14,110,97,0)" }] } },
                symbol: "circle", symbolSize: 6
            }
        ]
    };
}

function renderLineChart(data) {
    const row1 = document.getElementById("chartsRow1");
    if (!row1 || !data.trendLabels || !data.trendLabels.length) return;

    const id = "lineChart_" + Date.now();
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">اتجاه الإيرادات الشهري</div>' +
            '<div class="chart-badge">Line Chart</div>' +
        '</div>' +
        '<div class="echart-box" id="' + id + '"></div>';
    row1.appendChild(div);

    const chart = echarts.init(document.getElementById(id));
    chart.setOption(buildLineOption(data));
    charts.line = chart;
    chartRegistry.push(chart);
}

// عند التصفية على فئة بلا اتجاه شهري خاص بها، لا يوجد رسم لتحديثه فيُترك كما هو
function refreshLineChart(view) {
    if (!charts.line || !view.trendLabels || !view.trendLabels.length) return;
    charts.line.setOption(buildLineOption(view));
}

/* ==========================================================================
   Horizontal Bar Chart — توزيع الإيرادات حسب البنود الفرعية (عمود "الفئة/الصنف")
   يعرض فقط أعلى البنود الفرعية حسب الإيراد (والباقي مجمّع تحت "أخرى" من الخادم)
   بدلاً من Donut كان يزدحم بعشرات الشرائح عند كثرة البنود الفرعية
   ========================================================================== */
const DONUT_PALETTE = [C.dark, C.mid, C.lime, C.light, C.red, "#0A5A4F", "#B9821F", "#6E8A86"];

function buildSubItemsBarOption(data) {
    const labels = (data.subItemLabels && data.subItemLabels.length) ? data.subItemLabels : (data.categoryLabels || []);
    const values = (data.subItemValues && data.subItemValues.length)
        ? data.subItemValues
        : (data.categoryLabels || []).map(function (_, i) {
            return (data.categoryCollected[i] || 0) + (data.categoryUncollected[i] || 0);
        });

    const grandTotal = values.reduce(function (a, b) { return a + b; }, 0);

    const combined = labels.map(function (label, i) {
        return { label: label, value: values[i] || 0 };
    }).sort(function (a, b) { return b.value - a.value; }).reverse(); // أعلى بند يظهر بالأعلى بالرسم الأفقي

    const percentByLabel = {};
    combined.forEach(function (it) {
        percentByLabel[it.label] = grandTotal > 0 ? Math.round((it.value / grandTotal) * 1000) / 10 : 0;
    });

    return {
        tooltip: tooltipStyle({
            trigger: "axis",
            axisPointer: { type: "shadow" },
            formatter: function (params) {
                const p = params[0];
                const pct = percentByLabel.hasOwnProperty(p.name) ? percentByLabel[p.name] : 0;
                return '<div style="font-weight:700;margin-bottom:4px;">' + p.name + '</div>' +
                       '<div>الإيراد: <strong>' + formatNum(p.value) + '</strong></div>' +
                       '<div>النسبة من الإجمالي: <strong>' + pct + '%</strong></div>';
            }
        }),
        grid: { left: 10, right: 20, top: 10, bottom: 10, containLabel: true },
        xAxis: axisStyle({ type: "value", axisLabel: { show: false }, splitLine: { show: false } }),
        yAxis: axisStyle({
            type: "category",
            data: combined.map(function (x) { return x.label; }),
            axisLabel: { width: 220, overflow: "truncate", fontSize: 12 }
        }),
        series: [{
            name: "الإيراد", type: "bar", barMaxWidth: 22,
            data: combined.map(function (x, i) {
                // "أخرى" بلون محايد مميّز عن بقية البنود الفرعية
                const color = x.label === "أخرى" ? C.muted : DONUT_PALETTE[i % DONUT_PALETTE.length];
                return { value: x.value, itemStyle: { color: color, borderRadius: [0, 4, 4, 0] } };
            })
        }]
    };
}

function renderSubItemsBarChart(data) {
    const row2 = document.getElementById("chartsRow1");
    if (!row2) return;

    const id = "subItemsBarChart_" + Date.now();
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">توزيع الإيرادات حسب البنود الفرعية</div>' +
            '<div class="chart-badge">Horizontal Bar Chart</div>' +
        '</div>' +
        '<div class="echart-box" id="' + id + '"></div>';
    row2.appendChild(div);

    const chart = echarts.init(document.getElementById(id));
    chart.setOption(buildSubItemsBarOption(data));
    charts.subItemsBar = chart;
    chartRegistry.push(chart);
}

/* ==========================================================================
   Quarter Chart — توزيع الإيرادات حسب الأرباع (Q1..Q4)
   ========================================================================== */
const QUARTER_PALETTE = [C.dark, C.mid, C.lime, C.light];

function buildQuarterOption(data) {
    const labels = data.quarterLabels || [];
    const values = data.quarterValues || [];
    const grandTotal = values.reduce(function (a, b) { return a + b; }, 0);

    const items = labels.map(function (label, i) {
        return { name: label, value: values[i] || 0, itemStyle: { color: QUARTER_PALETTE[i % QUARTER_PALETTE.length] } };
    });
    const percentByName = {};
    items.forEach(function (it) {
        percentByName[it.name] = grandTotal > 0 ? Math.round((it.value / grandTotal) * 1000) / 10 : 0;
    });

    return {
        tooltip: tooltipStyle({
            trigger: "item",
            formatter: function (p) {
                const pct = percentByName.hasOwnProperty(p.name) ? percentByName[p.name] : 0;
                return '<div style="font-weight:700;margin-bottom:4px;">' + p.name + '</div>' +
                       '<div>الإيراد: <strong>' + formatNum(p.value) + '</strong></div>' +
                       '<div>النسبة من الإجمالي: <strong>' + pct + '%</strong></div>';
            }
        }),
        legend: {
            orient: "vertical", right: 8, top: "middle", type: "scroll",
            itemWidth: 12, itemHeight: 12, itemGap: 14,
            textStyle: Object.assign({}, AXIS_LABEL_STYLE),
            formatter: function (name) {
                const pct = percentByName.hasOwnProperty(name) ? percentByName[name] : 0;
                return name + "  " + pct + "%";
            }
        },
        series: [{
            type: "pie", radius: ["45%", "72%"], center: ["38%", "50%"],
            avoidLabelOverlap: true,
            data: items,
            label: { show: false },
            labelLine: { show: false },
            itemStyle: { borderColor: "#fff", borderWidth: 2 },
            emphasis: { scale: true, scaleSize: 6 }
        }]
    };
}

function renderQuarterChart(data) {
    const row2 = document.getElementById("chartsRow1");
    if (!row2 || !data.quarterLabels || !data.quarterLabels.length || !(data.quarterValues || []).some(function (v) { return v > 0; })) return;

    const id = "quarterChart_" + Date.now();
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">توزيع الإيرادات حسب الأرباع</div>' +
            '<div class="chart-badge">Quarterly</div>' +
        '</div>' +
        '<div class="echart-box" id="' + id + '"></div>';
    row2.appendChild(div);

    const chart = echarts.init(document.getElementById(id));
    chart.setOption(buildQuarterOption(data));
    charts.quarter = chart;
    chartRegistry.push(chart);
}

/* ==========================================================================
   Gauge Chart — Annual Target Achievement
   ========================================================================== */
function buildGaugeOption(data) {
    const percent = data.targetAchievementPercent || 0;
    const gaugeColor = percent >= 80 ? C.dark : percent >= 50 ? C.lime : C.red;
    return {
        series: [{
            type: "gauge", min: 0, max: 150, splitNumber: 5,
            startAngle: 210, endAngle: -30,
            progress:  { show: true, width: 16, itemStyle: { color: gaugeColor } },
            axisLine:  { lineStyle: { width: 16, color: [[1, C.grid]] } },
            axisLabel: { color: C.axisText, fontSize: 12, fontWeight: 600, distance: 18 },
            axisTick:  { lineStyle: { color: "#C8DFDC" }, length: 6 },
            splitLine: { lineStyle: { color: "#C8DFDC", width: 2 }, length: 10 },
            pointer:   { length: "65%", width: 4, itemStyle: { color: gaugeColor } },
            anchor:    { show: true, showAbove: true, size: 14, itemStyle: { borderWidth: 3, borderColor: gaugeColor, color: "#fff" } },
            detail:    { valueAnimation: true, fontSize: 28, fontWeight: 800, color: gaugeColor, formatter: "{value}%", offsetCenter: [0, "45%"] },
            data: [{ value: Math.min(percent, 150), name: "نسبة تحقيق الهدف" }]
        }]
    };
}

function renderGauge(data) {
    const row2 = document.getElementById("chartsRow2");
    if (!row2) return;

    const id = "gaugeChart_" + Date.now();
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">نسبة تحقيق الهدف السنوي</div>' +
            '<div class="chart-badge">Gauge</div>' +
        '</div>' +
        '<div class="gauge-box" id="' + id + '"></div>';
    row2.appendChild(div);

    const chart = echarts.init(document.getElementById(id));
    chart.setOption(buildGaugeOption(data));
    charts.gauge = chart;
    chartRegistry.push(chart);
}

/* ==========================================================================
   Linear Trend
   ========================================================================== */
function linearTrend(vals) {
    const n = vals.length;
    if (n < 2) return vals.slice();
    let sx = 0, sy = 0, sxy = 0, sxx = 0;
    for (let i = 0; i < n; i++) { sx += i; sy += vals[i]; sxy += i * vals[i]; sxx += i * i; }
    const d = n * sxx - sx * sx;
    const slope = d ? (n * sxy - sx * sy) / d : 0;
    const b = (sy - slope * sx) / n;
    return Array.from({ length: n }, function (_, i) { return Math.round(b + slope * i); });
}
