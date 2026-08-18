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

// نفس formatNum لكن بلا أي تقريب (منزلتان عشريتان كاملتان) - تُستخدم فقط لكروت الـKPI الرئيسية
// (إجمالي/محصّل/غير محصّل) حتى تطابق الرقم بالضبط لما تُجمَع القيم مباشرة بإكسل، بناءً على طلب صريح
function formatNumExact(n) {
    if (!Number.isFinite(n)) return "0.00";
    return n.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

// كانت تختصر الأرقام الكبيرة (150.3M بدل 150,286,045) لتوفير مساحة داخل الرسوم - بناءً على
// طلب المستخدمة تعرض الأرقام كاملة بلا أي اختصار أو تقريب بكل مكان بالموقع، فصارت تُطابق
// formatNum تمامًا (الاسم أُبقي كما هو لتفادي تعديل كل نقطة استدعاء بكل الملفات المشتركة)
function formatCompact(n) {
    return formatNum(n);
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

const MONTH_OPTIONS = [
    { value: "", label: "كل الأشهر" },
    { value: "1", label: "يناير" }, { value: "2", label: "فبراير" }, { value: "3", label: "مارس" },
    { value: "4", label: "أبريل" }, { value: "5", label: "مايو" }, { value: "6", label: "يونيو" },
    { value: "7", label: "يوليو" }, { value: "8", label: "أغسطس" }, { value: "9", label: "سبتمبر" },
    { value: "10", label: "أكتوبر" }, { value: "11", label: "نوفمبر" }, { value: "12", label: "ديسمبر" }
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

    const monthSel = makeSelectWrap("filterMonth", "bi-calendar-event");
    MONTH_OPTIONS.forEach(function (opt) {
        const o = document.createElement("option");
        o.value = opt.value;
        o.textContent = opt.label;
        monthSel.select.appendChild(o);
    });
    bar.appendChild(monthSel.wrap);
    monthSel.select.addEventListener("change", applyDashboardFilters);

    // مسح الفلاتر: يعيد رقم العقد/الربع/الشهر (ونوع الاستثمار بصفحة بنود الأمانة إن وُجد) لحالتها
    // الافتراضية بلا التأثير على النطاق نفسه (الإدارة/البلدية المختارة) - نفس الصفحة تبقى مفتوحة
    const clearBtn = document.createElement("button");
    clearBtn.type = "button";
    clearBtn.className = "filter-clear-btn";
    clearBtn.innerHTML = '<i class="bi bi-x-circle"></i> مسح الفلاتر';
    clearBtn.addEventListener("click", function () {
        if (search.input) search.input.value = "";
        if (quarterSel.select) quarterSel.select.value = "";
        if (monthSel.select) monthSel.select.value = "";
        const investmentTypeSel = document.getElementById("filterInvestmentType");
        if (investmentTypeSel) {
            investmentTypeSel.value = "";
            if (typeof aiInvestmentTypeFilter !== "undefined") aiInvestmentTypeFilter = "";
        }
        applyDashboardFilters();
    });
    bar.appendChild(clearBtn);

    headerRow.appendChild(bar);
}

function currentFilterParams() {
    const contractEl = document.getElementById("filterContractNumber");
    const quarterEl = document.getElementById("filterQuarter");
    const monthEl = document.getElementById("filterMonth");
    const params = new URLSearchParams();
    const contract = contractEl ? contractEl.value.trim() : "";
    const quarter = quarterEl ? quarterEl.value : "";
    const month = monthEl ? monthEl.value : "";
    if (contract) params.set("contract", contract);
    if (quarter) params.set("quarter", quarter);
    if (month) params.set("month", month);
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
let charts = { line: null, subItemsBar: null, entityBar: null, topItems: null, quarter: null };

/* ==========================================================================
   تفريغ/تدمير كل الرسوم الحالية استعدادًا لتحميل لوحة جديدة (تفاعل القائمة الجانبية)
   ========================================================================== */
function disposeCharts() {
    chartRegistry.forEach(function (c) { try { c.dispose(); } catch (e) {} });
    chartRegistry = [];
    charts = { line: null, subItemsBar: null, entityBar: null, topItems: null, quarter: null };
    activeDashboardData = null;
}

function clearContainers() {
    ["kpiGrid", "chartsRow1", "chartsRow2", "chartsRowDonuts", "chartsRowInsights", "chartsRow3"].forEach(function (id) {
        const el = document.getElementById(id);
        if (el) el.innerHTML = "";
    });
    removeFilterBanner();
    if (typeof removeRevenuePerfSection === "function") removeRevenuePerfSection();
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

        renderKPIs(data);
        // قسم "أداء الإيرادات حسب نوع الإيراد" - يُدرَج مباشرة بعد بطاقات الـKPI وقبل بقية الرسوم
        if (typeof renderRevenuePerformanceSection === "function") renderRevenuePerformanceSection(data);
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
   التصفية التفاعلية بين الرسوم (Cross-Filter بأسلوب Power BI) - عند الضغط على بند بـ"أعلى
   بنود الإيرادات"، تُعاد حساب البطاقات والرسوم الأخرى فورًا من نفس البيانات المحمّلة أصلًا
   دون أي طلب إضافي للخادم - راجع applyTopItemFilter/clearTopItemFilter بالأسفل
   ========================================================================== */

// بانر "مُصفّى حسب: <البند> ×" فوق بطاقات الـKPI - يظهر عند الضغط على بند بـ"أعلى بنود
// الإيرادات"، والضغط على × يلغي التصفية ويرجّع كل شيء (الكروت + الرسمتين) لوضعه الأصلي
function updateFilterBanner(label) {
    const grid = document.getElementById("kpiGrid");
    if (!grid) return;
    removeFilterBanner();
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
        clearTopItemFilter();
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
        { cls: "c1", icon: ICONS.wallet,   label: "إجمالي الإيرادات",       value: formatNumExact(data.totalRevenue),       money: true },
        { cls: "c2", icon: ICONS.check,    label: "الإيرادات المحصّلة",     value: formatNumExact(data.collectedRevenue),   money: true },
        { cls: "c3", icon: ICONS.alert,    label: "الإيرادات غير المحصّلة", value: formatNumExact(data.uncollectedRevenue), money: true },
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
    var titleText = escapeHtml(data.entityChartTitle || "المحصّل ربع السنوي لكل جهة (أمانة/بلدية)");
    var badgeText = escapeHtml(data.entityChartBadge || "اضغط لعرض الجهة");
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">' + titleText + '</div>' +
            '<div class="chart-badge">' + badgeText + '</div>' +
        '</div>' +
        '<div class="echart-box" id="' + id + '" style="height:280px;"></div>';
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
        // boundaryGap:true يترك هامشًا قبل أول نقطة وبعد آخر نقطة بدل التصاقهما بحافتَي منطقة
        // الرسم - يمنع تراكب تسمية القيمة الأولى/الأخيرة مع تسميات محور Y
        grid: { left: 60, right: 24, top: 20, bottom: 56 },
        xAxis: axisStyle({ type: "category", boundaryGap: true, data: data.entityMonthLabels, axisLabel: { rotate: data.entityMonthLabels.length > 6 ? 35 : 0 } }),
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
   Top Items Chart — أعلى بنود الإيرادات (ترتيب تنازلي حسب إجمالي الإيراد)
   ========================================================================== */
// بند مختار حاليًا برسم "أعلى بنود الإيرادات" (يُصفّي رسم "البنود الفرعية" المجاورة) - null يعني
// لم يُضغَط على أي بند بعد، فتعرض رسمة البنود الفرعية افتراضيًا تفصيل أعلى بند قيمةً (من الخادم)
let selectedTopItemLabel = null;

function renderTopItemsChart(data) {
    const row1 = document.getElementById("chartsRow1");
    // يعتمد فقط على عمود "تصنيف البند" (البنود الرئيسية مثل: استثمار، رسوم، جزاءات...)
    // ولا يعرض أي بند فرعي، بصرف النظر عن مستوى اللوحة الحالية
    const hasTopItems = data.topItemLabels && data.topItemLabels.length;
    if (!row1 || !hasTopItems) return;

    selectedTopItemLabel = null; // إعادة ضبط عند كل تحميل لوحة جديدة

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
            '<div class="chart-badge">اضغط لعرض بنوده الفرعية</div>' +
        '</div>' +
        '<div class="echart-box" id="' + id + '"></div>';
    row1.appendChild(div);

    const chart = echarts.init(document.getElementById(id));

    function makeOption() {
        return {
            tooltip: tooltipStyle({
                trigger: "axis",
                axisPointer: { type: "shadow" },
                formatter: function (params) {
                    const p = params[0];
                    return '<div style="font-weight:700;margin-bottom:4px;">' + p.name + '</div>' +
                           '<div>الإيرادات: <strong>' + formatNum(p.value) + '</strong></div>';
                }
            }),
            grid: { left: 10, right: 60, top: 10, bottom: 10, containLabel: true },
            xAxis: axisStyle({ type: "value", axisLabel: { show: false }, splitLine: { show: false } }),
            yAxis: axisStyle({
                type: "category",
                data: combined.map(function (x) { return x.label; }),
                axisLabel: { width: 220, overflow: "truncate", fontSize: 12 }
            }),
            series: [{
                name: "الإيرادات", type: "bar", barMaxWidth: 22, cursor: "pointer",
                data: combined.map(function (x, i) {
                    const selected = selectedTopItemLabel === x.label;
                    return {
                        value: x.total,
                        itemStyle: {
                            color: ITEM_PALETTE[i % ITEM_PALETTE.length], borderRadius: [0, 4, 4, 0],
                            borderColor: selected ? C.text : "transparent", borderWidth: selected ? 2 : 0
                        },
                        label: {
                            show: true, position: "right", distance: 6,
                            formatter: function (p) { return formatCompact(p.value); },
                            color: C.axisText, fontWeight: 700, fontSize: 12
                        }
                    };
                })
            }]
        };
    }

    chart.setOption(makeOption());
    chart.__makeOption = makeOption; // متاحة لـ clearTopItemFilter لإعادة رسم الشكل بلا تحديد
    chart.on("click", function (params) {
        if (!params.name) return;
        if (params.name === selectedTopItemLabel) { clearTopItemFilter(); return; }
        selectedTopItemLabel = params.name;
        chart.setOption(makeOption());
        applyTopItemFilter(activeDashboardData, params.name);
    });

    charts.topItems = chart;
    chartRegistry.push(chart);
}

/* عند الضغط على بند برسم "أعلى بنود الإيرادات": تحديث كل شيء متعلّق بذلك البند فورًا (بلا أي
   طلب شبكة، كل البيانات موجودة أصلاً مع اللوحة المحمّلة) -
   1) رسم "البنود الفرعية" المجاورة يبدّل لتفصيل ذلك البند
   2) بطاقات الـKPI تتحدّث لأرقام ذلك البند فقط (من data.revenueTypePerformance)
   3) رسم "أداء الإيرادات" (الصفحة الرئيسية فقط) يبرز عمود ذلك البند ويعتّم الباقي
   4) بانر "مُصفّى حسب: <البند> ×" يظهر أعلى الكروت */
function applyTopItemFilter(data, classificationLabel) {
    if (!data) return;

    if (data.topItemSubItems && charts.subItemsBar) {
        const entry = data.topItemSubItems.filter(function (x) { return x.classificationLabel === classificationLabel; })[0];
        if (entry) {
            charts.subItemsBar.setOption(buildSubItemsBarOption({ subItemLabels: entry.subLabels, subItemValues: entry.subValues }), true);
            const card = charts.subItemsBar.__cardEl;
            if (card) {
                const badge = card.querySelector(".chart-badge");
                if (badge) badge.textContent = classificationLabel;
            }
            try { charts.subItemsBar.resize(); } catch (e) {}
        }
    }

    if (data.revenueTypePerformance) {
        const row = data.revenueTypePerformance.filter(function (r) { return r.itemLabel === classificationLabel; })[0];
        if (row) {
            renderKPIs({
                totalRevenue: row.invoiced,
                collectedRevenue: row.collected,
                uncollectedRevenue: Math.max(row.invoiced - row.collected, 0),
                collectionRatePercent: row.invoiced > 0 ? (row.collected / row.invoiced * 100) : 0
            });
        }
    }

    // إبراز عمود البند المختار برسم "أداء الإيرادات" (الرئيسية فقط - المتغيّرات معرَّفة
    // بملف revenue-performance.js المحمَّل بنفس الصفحة) وتعتيم بقية الأعمدة
    if (typeof revenuePerfChart !== "undefined" && revenuePerfChart && typeof revenuePerfData !== "undefined" && revenuePerfData) {
        const idx = revenuePerfData.findIndex(function (r) { return r.itemLabel === classificationLabel; });
        if (idx > -1) {
            try {
                revenuePerfChart.dispatchAction({ type: "downplay" });
                revenuePerfChart.dispatchAction({ type: "highlight", dataIndex: idx });
            } catch (e) {}
        }
    }

    updateFilterBanner(classificationLabel);
}

// إلغاء تصفية "أعلى بنود الإيرادات": يرجّع الكروت + رسم "أداء الإيرادات" + رسم "البنود
// الفرعية" لوضعها الأصلي (بلا أي بند مختار)، ويزيل البانر
function clearTopItemFilter() {
    selectedTopItemLabel = null;
    removeFilterBanner();

    if (activeDashboardData) renderKPIs(activeDashboardData);

    if (typeof revenuePerfChart !== "undefined" && revenuePerfChart) {
        try { revenuePerfChart.dispatchAction({ type: "downplay" }); } catch (e) {}
    }

    if (charts.topItems && charts.topItems.__makeOption) {
        try { charts.topItems.setOption(charts.topItems.__makeOption()); } catch (e) {}
    }

    if (activeDashboardData && activeDashboardData.topItemSubItems && activeDashboardData.topItemSubItems.length && charts.subItemsBar) {
        const first = activeDashboardData.topItemSubItems[0];
        charts.subItemsBar.setOption(buildSubItemsBarOption({ subItemLabels: first.subLabels, subItemValues: first.subValues }), true);
        const card = charts.subItemsBar.__cardEl;
        if (card) {
            const badge = card.querySelector(".chart-badge");
            if (badge) badge.textContent = first.classificationLabel;
        }
        try { charts.subItemsBar.resize(); } catch (e) {}
    }
}

/* ==========================================================================
   Horizontal Bar Chart — توزيع الإيرادات حسب البنود الفرعية (عمود "الفئة/الصنف")
   يعرض فقط أعلى البنود الفرعية حسب الإيراد (والباقي مجمّع تحت "أخرى" من الخادم)
   بدلاً من Donut كان يزدحم بعشرات الشرائح عند كثرة البنود الفرعية
   ========================================================================== */
// تدرّجات هوية الشعار فقط (أخضر داكن/متوسط/لايم/فاتح) بلا أي لون خارج العائلة الخضراء/التركوازية
const ITEM_PALETTE = [C.dark, C.mid, C.lime, C.light, "#0A5A4F"];

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
                const color = x.label === "أخرى" ? C.muted : ITEM_PALETTE[i % ITEM_PALETTE.length];
                return {
                    value: x.value, itemStyle: { color: color, borderRadius: [0, 4, 4, 0] },
                    label: {
                        show: true, position: "right", distance: 6,
                        formatter: function (p) { return formatCompact(p.value); },
                        color: C.axisText, fontWeight: 700, fontSize: 12
                    }
                };
            })
        }]
    };
}

function renderSubItemsBarChart(data) {
    const row2 = document.getElementById("chartsRow1");
    if (!row2) return;

    // العنوان الافتراضي = اسم أعلى بند رئيسي قيمةً (نفس التفصيل الافتراضي المُرسَل بـ
    // subItemLabels/Values من الخادم) حتى تعرف المستخدمة أي بند تعرضه الرسمة قبل أي ضغط
    var defaultLabel = (data.topItemSubItems && data.topItemSubItems.length)
        ? data.topItemSubItems[0].classificationLabel
        : "اضغطي بندًا بالرسمة المجاورة";

    const id = "subItemsBarChart_" + Date.now();
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">توزيع الإيرادات حسب البنود الفرعية</div>' +
            '<div class="chart-badge">' + escapeHtml(defaultLabel) + '</div>' +
        '</div>' +
        '<div class="echart-box" id="' + id + '"></div>';
    row2.appendChild(div);

    const chart = echarts.init(document.getElementById(id));
    chart.setOption(buildSubItemsBarOption(data));
    chart.__cardEl = div;
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
    // الدونات "توزيع الإيرادات حسب الأرباع" اختفت من الصفحة الرئيسية تحديدًا (بطلب المستخدمة)
    // بعد إضافة رسم "أداء الإيرادات حسب نوع الإيراد" العمودي فوقها - تبقى كما هي ببقية الصفحات
    // المشتركة (صفحات البنود الفردية) التي لم يُطلَب فيها أي تعديل
    if (window.FILTER_MODE === "home") return;

    const row2 = document.getElementById("chartsRow1");
    if (!row2) return;

    // صفحة "بيانات البلديات": الدونات كانت تكرّر نفس معلومة الرسم الخطي بالأسفل (كلاهما
    // توزيع زمني/ربعي) - استبدلناها برسم "أعلى البلديات إيرادًا" (معلومة جديدة غير موجودة
    // بأي رسم آخر بالصفحة) + بطاقتا أكثر/أقل بلدية تفاعلاً
    if (window.FILTER_MODE === "baladiyah-scope") {
        renderBaladiyahEntityRevenueChart(row2, data);
        renderBaladiyahActivityCards(row2, data);
        return;
    }

    if (!data.quarterLabels || !data.quarterLabels.length || !(data.quarterValues || []).some(function (v) { return v > 0; })) return;

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
   "أعلى البلديات إيرادًا" - Horizontal Bar لأعلى 8 بلديات حسب إجمالي الإيرادات، مع نسبة
   التحصيل بتلوين العمود (غامق = تحصيل مرتفع) - صفحة "بيانات البلديات" فقط
   ========================================================================== */
function renderBaladiyahEntityRevenueChart(row, data) {
    const rows = (data.entityRevenueBreakdown || []).slice(0, 8);
    if (!rows.length) return;

    const id = "baladiyahEntityRevenue_" + Date.now();
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">أعلى البلديات إيرادًا</div>' +
            '<div class="chart-badge">مع نسبة التحصيل</div>' +
        '</div>' +
        '<div class="echart-box" id="' + id + '"></div>';
    row.appendChild(div);

    const sorted = rows.slice().reverse(); // أعلى بلدية تظهر بالأعلى (المحور يرسم من الأسفل للأعلى)
    const labels = sorted.map(function (r) { return r.entity; });
    const values = sorted.map(function (r) { return r.totalValue; });

    const chart = echarts.init(document.getElementById(id));
    chart.setOption({
        tooltip: tooltipStyle({
            trigger: "axis", axisPointer: { type: "shadow" },
            formatter: function (params) {
                const p = params[0];
                const r = sorted[p.dataIndex];
                return '<div style="font-weight:700;margin-bottom:4px;">' + escapeHtml(r.entity) + '</div>' +
                    '<div>الإجمالي: <strong>' + formatNum(r.totalValue) + '</strong></div>' +
                    '<div>نسبة التحصيل: <strong>' + r.collectionRatePercent + '%</strong></div>';
            }
        }),
        grid: { left: 130, right: 70, top: 10, bottom: 10 },
        xAxis: axisStyle({ type: "value", show: false }),
        yAxis: axisStyle({ type: "category", data: labels, axisLabel: { width: 120, overflow: "break", lineHeight: 14, fontSize: 11 } }),
        series: [{
            name: "الإجمالي", type: "bar", barMaxWidth: 20,
            data: values.map(function (v, i) {
                return { value: v, itemStyle: { color: ITEM_PALETTE[i % ITEM_PALETTE.length], borderRadius: [0, 6, 6, 0] } };
            }),
            label: {
                show: true, position: "right", color: C.axisText, fontWeight: 700, fontSize: 11,
                formatter: function (p) { return formatCompact(p.value) + "  (" + sorted[p.dataIndex].collectionRatePercent + "%)"; }
            }
        }]
    });
    chartRegistry.push(chart);
}

function baladiyahActivitySquareHtml(icon, label, entity, collected) {
    return '<div class="ai-insight-card">' +
        '<div class="ai-insight-icon"><i class="bi ' + icon + '"></i></div>' +
        '<div class="ai-insight-text">' +
            '<div class="ai-insight-label">' + label + '</div>' +
            '<div class="ai-insight-dept">' + escapeHtml(entity || "—") + '</div>' +
            (entity ? '<div class="ai-insight-metric">' + formatNum(collected) + ' ' + SAR_SYMBOL + '</div>' : '') +
        '</div>' +
    '</div>';
}

function renderBaladiyahActivityCards(row, data) {
    if (!data.topActivityEntity && !data.bottomActivityEntity) return;
    const div = document.createElement("div");
    div.className = "ai-insight-wrap";
    div.innerHTML =
        baladiyahActivitySquareHtml("bi-trophy", "أكثر بلدية تفاعلاً (محصّل)", data.topActivityEntity, data.topActivityCollected) +
        baladiyahActivitySquareHtml("bi-graph-down-arrow", "أقل بلدية تفاعلاً (محصّل)", data.bottomActivityEntity, data.bottomActivityCollected);
    row.appendChild(div);
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
