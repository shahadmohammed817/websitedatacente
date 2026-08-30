/* ==========================================================================
   صفحة "الطلبات المفتوحة" - 4 كروت KPI (إجمالي/طلبات 2026/متأخرة +90 يوم/ضمن المدة)، Timeline
   أفقي للمراحل (اضغط مرحلة لعرض حالاتها التفصيلية أسفله)، عمود أفقي لتوزيع السلة، وقوس
   أحمر/أخضر لعدد الأيام منذ آخر تحديث (أحمر إن تجاوز 90 يومًا) لطلبات المرحلة المختارة فقط
   بالـTimeline - تتحدّث القوس فورًا عند اختيار مرحلة أخرى، والضغط على جزء لوني بها يفتح درج
   تفاصيل منزلق (Data Drawer) بقائمة الطلبات المطابقة. الألوان التصنيفية (5 ألوان هوية الموقع
   فقط) ثابتة لكل اسم عبر كل الرسوم - لا تُستخدم ألوان الحالة (أحمر/أصفر) إلا لمعناها الفعلي
   (متأخر/غير محدد).
   ========================================================================== */
const ORQ_C = {
    dark: "#0E6E61", mid: "#3F8F87", light: "#5FA89D", lime: "#8DC63F", limeDk: "#5F9E1F",
    red: "#D6534A", warn: "#E0A930", text: "#173B36", muted: "#6E8A86", grid: "#E1ECEA", axisText: "#181818"
};
const ORQ_AXIS_LABEL_STYLE = { color: ORQ_C.axisText, fontSize: 13, fontWeight: 600 };
// نفس 5 ألوان الهوية المستخدمة بكل صفحات الموقع (CLX_PALETTE/CGP_PALETTE) - الأحمر/الأصفر
// محجوزان لحالة "متأخر/غير محدد" فقط ولا يُستخدَمان كألوان تصنيف عامة (حتى لا يفقدا معناهما)
const ORQ_PALETTE = [ORQ_C.dark, ORQ_C.mid, ORQ_C.lime, ORQ_C.light, ORQ_C.limeDk];

// لون ثابت لكل اسم (مرحلة) بدل الاعتماد على ترتيبه بالقائمة المفروزة - حتى تبقى نفس المرحلة
// بنفس اللون دائمًا عبر كل الرسوم وبعد تغيير الفلاتر (فرز مختلف)، بدل أن "يتبادل" لونين
// مرحلتين مكانهما لمجرد تغيّر ترتيبهما
function orqColorForName(name) {
    var h = 0;
    for (var i = 0; i < (name || "").length; i++) h = (h * 31 + name.charCodeAt(i)) | 0;
    return ORQ_PALETTE[Math.abs(h) % ORQ_PALETTE.length];
}

function orqAxisStyle(extra) {
    extra = extra || {};
    const merged = Object.assign({
        axisLine: { lineStyle: { color: ORQ_C.grid } },
        splitLine: { lineStyle: { color: ORQ_C.grid, type: "dashed" } }
    }, extra);
    merged.axisLabel = Object.assign({}, ORQ_AXIS_LABEL_STYLE, extra.axisLabel || {});
    return merged;
}
function orqTooltip(extra) {
    return Object.assign({
        backgroundColor: "#FFFFFF", borderColor: ORQ_C.grid, borderWidth: 1,
        textStyle: { color: ORQ_C.text, fontSize: 12 },
        extraCssText: "box-shadow:0 4px 16px rgba(0,0,0,0.1);border-radius:10px;max-width:320px;white-space:normal;"
    }, extra || {});
}
function orqFormatNum(n) { return Number.isFinite(n) ? Math.round(n).toLocaleString("en-US") : "0"; }
function orqEscape(str) {
    const div = document.createElement("div");
    div.textContent = (str === null || str === undefined) ? "" : String(str);
    return div.innerHTML;
}

let orqData = null; // آخر بيانات مُحمَّلة من الخادم - تُستخدم لإعادة حساب القوس/الدرج عند تغيّر اختيار المرحلة بلا طلب شبكة جديد
let orqCharts = [];
function orqDisposeCharts() {
    orqCharts.forEach(function (c) { try { c.dispose(); } catch (e) {} });
    orqCharts = [];
}

/* ==========================================================================
   شريط الفلاتر: مراحل (شيك بوكس منسدل) + بحث برقم الطلب
   ========================================================================== */
// قائمة منسدلة بشيك بوكسات (بدل قائمة اختيار واحد) - زر يعرض عدد المختار، يفتح لوحة صغيرة
// فيها شيك بوكس لكل خيار. onChange(value, checked) يُستدعى عند كل تبديل
function orqMakeCheckboxDropdown(id, iconClass, allLabel, options, selectedArr, onChange) {
    const wrap = document.createElement("div");
    wrap.className = "filter-select-wrap orq-checkbox-dropdown";

    const btn = document.createElement("button");
    btn.type = "button";
    btn.className = "orq-checkbox-dropdown-btn";
    const labelSpan = document.createElement("span");
    labelSpan.id = id + "Label";
    labelSpan.textContent = selectedArr.length ? (selectedArr.length + " مختارة") : allLabel;
    btn.innerHTML = '<i class="bi ' + iconClass + ' filter-icon"></i>';
    btn.appendChild(labelSpan);
    btn.insertAdjacentHTML("beforeend", '<i class="bi bi-chevron-down filter-chevron"></i>');

    const panel = document.createElement("div");
    panel.className = "orq-checkbox-dropdown-panel";
    panel.id = id + "Panel";
    panel.innerHTML = (options || []).map(function (o) {
        var checked = selectedArr.indexOf(o) !== -1 ? " checked" : "";
        return '<label class="orq-year-check"><input type="checkbox" value="' + orqEscape(o) + '"' + checked + '><span>' + orqEscape(o) + '</span></label>';
    }).join("");

    wrap.appendChild(btn);
    wrap.appendChild(panel);

    btn.addEventListener("click", function (e) {
        e.stopPropagation();
        var open = panel.classList.toggle("open");
        btn.classList.toggle("open", open);
    });
    document.addEventListener("click", function (e) {
        if (!wrap.contains(e.target)) { panel.classList.remove("open"); btn.classList.remove("open"); }
    });
    panel.querySelectorAll('input[type="checkbox"]').forEach(function (cb) {
        cb.addEventListener("change", function () {
            onChange(cb.value, cb.checked);
            labelSpan.textContent = selectedArr.length ? (selectedArr.length + " مختارة") : allLabel;
        });
    });
    return { wrap: wrap, btn: btn, panel: panel };
}

function orqMakeSearchWrap(id, placeholder, value) {
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

const orqFilters = { stages: [], search: "", years: [] };

// يُبنى مرة واحدة فقط (لا يُعاد بناؤه مع كل orqLoad) حتى لا تُغلَق لوحة الشيك بوكس المفتوحة
// بعد كل تبديل (خيارات المراحل نفسها لا تتغيّر مع الفلاتر أصلًا - مبنية من كل السجلات دومًا)
let orqFilterBarBuilt = false;
function orqBuildFilterBar(data) {
    const bar = document.getElementById("orqFilterBar");
    if (!bar || orqFilterBarBuilt) return;
    bar.innerHTML = "";

    const stage = orqMakeCheckboxDropdown("orqStageFilter", "bi-signpost-split", "كل المراحل", data.stageOptions || [], orqFilters.stages, function (value, checked) {
        if (checked) { if (orqFilters.stages.indexOf(value) === -1) orqFilters.stages.push(value); }
        else { orqFilters.stages = orqFilters.stages.filter(function (s) { return s !== value; }); }
        orqLoad();
    });
    const search = orqMakeSearchWrap("orqSearchFilter", "ابحث برقم الطلب...", orqFilters.search);

    bar.appendChild(stage.wrap);
    bar.appendChild(search.wrap);

    let debounce = null;
    search.input.addEventListener("input", function () {
        orqFilters.search = search.input.value.trim();
        clearTimeout(debounce);
        debounce = setTimeout(orqLoad, 400);
    });
    orqFilterBarBuilt = true;
}

// صندوق "السنوات" (شيك بوكس متعدد الاختيار) - يُبنى مرة واحدة فقط أول تحميل (لا يُعاد بناؤه
// مع كل orqLoad حتى لا يفقد المستخدم اختياره الحالي)، ويُملأ بخيارات YearOptions القادمة
// من الخادم
let orqYearBoxBuilt = false;
function orqBuildYearBox(data) {
    const box = document.getElementById("orqYearBox");
    if (!box || orqYearBoxBuilt) return;
    const years = data.yearOptions || [];
    if (!years.length) return;
    box.innerHTML =
        '<div class="orq-year-box-title">السنوات</div>' +
        '<div class="orq-year-box-options">' +
        years.map(function (y) {
            return '<label class="orq-year-check"><input type="checkbox" value="' + y + '"><span>' + y + '</span></label>';
        }).join("") +
        '</div>';
    box.querySelectorAll('input[type="checkbox"]').forEach(function (cb) {
        cb.addEventListener("change", function () {
            const val = parseInt(cb.value, 10);
            if (cb.checked) { if (orqFilters.years.indexOf(val) === -1) orqFilters.years.push(val); }
            else { orqFilters.years = orqFilters.years.filter(function (y) { return y !== val; }); }
            orqLoad();
        });
    });
    orqYearBoxBuilt = true;
}

function orqQueryString() {
    const p = new URLSearchParams();
    orqFilters.stages.forEach(function (s) { p.append("stages", s); });
    if (orqFilters.search) p.set("search", orqFilters.search);
    orqFilters.years.forEach(function (y) { p.append("years", y); });
    return p.toString();
}

/* ==========================================================================
   كروت الـKPI الأربعة
   ========================================================================== */
function orqKpiCardHtml(cls, icon, label, count, sub) {
    return '<div class="kpi-card ' + cls + '">' +
        '<div class="icon"><i class="bi ' + icon + '"></i></div>' +
        '<div class="label">' + orqEscape(label) + '</div>' +
        '<div class="value">' + orqFormatNum(count) + '</div>' +
        (sub ? '<div style="font-size:.78rem;font-weight:700;color:var(--muted);margin-top:4px;">' + orqEscape(sub) + '</div>' : '') +
    '</div>';
}

function orqRenderKpiCards(data) {
    const grid = document.getElementById("orqKpiGrid");
    if (!grid) return;
    grid.innerHTML =
        orqKpiCardHtml("c1", "bi-inbox", "إجمالي الطلبات المفتوحة", data.totalCount, "طلب") +
        orqKpiCardHtml("c4", "bi-calendar-event", "طلبات عام 2026", data.total2026Count, "طلب") +
        orqKpiCardHtml("c3", "bi-exclamation-triangle", "طلبات متأخرة (+90 يوم)", data.overdueCount, (data.overduePercent || 0).toFixed(1) + "% من الإجمالي") +
        orqKpiCardHtml("c2", "bi-check-circle", "طلبات ضمن المدة", data.onTimeCount, "طلب");
}

/* ==========================================================================
   أداة عامة لإضافة بطاقة رسم لأي صف
   ========================================================================== */
function orqRenderChart(rowId, containerId, title, badge, option, heightPx) {
    const row = document.getElementById(rowId);
    if (!row) return null;
    const id = containerId + "_" + Date.now() + "_" + Math.floor(Math.random() * 1000);
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header">' +
            '<div class="chart-title">' + orqEscape(title) + '</div>' +
            (badge ? '<div class="chart-badge">' + orqEscape(badge) + '</div>' : '') +
        '</div>' +
        '<div class="echart-box" id="' + id + '" style="height:' + (heightPx || 320) + 'px;"></div>';
    row.appendChild(div);
    const chart = echarts.init(document.getElementById(id));
    chart.setOption(option);
    chart._orqCard = div;
    orqCharts.push(chart);
    requestAnimationFrame(function () { try { chart.resize(); } catch (e) {} });
    return chart;
}

/* ==========================================================================
   1+3) Timeline أفقي للمراحل: خط واحد وعقدة دائرية لكل مرحلة (اسم + عدد)، مرتّبة حسب
   سير عملية الطلب الفعلي (وليس حسب العدد) - من "مراجعة الإعلان" حتى "إعداد العقد".
   الترتيب نفسه ثابت (منطق العملية لا يتغيّر)، لكن أي مراحل/أعداد/حالات تُعرَض كلها تُقرأ من
   البيانات الفعلية القادمة من الملف. الضغط على عقدة يعرض تحت الخط حالات الطلب التفصيلية
   لتلك المرحلة فقط (بحركة ظهور بسيطة)، وتتحدّث تلقائيًا عند اختيار مرحلة أخرى.
   ========================================================================== */
const ORQ_STAGE_PROCESS_ORDER = [
    "مراجعة واعتماد الإعلان",
    "لجنة فتح المظاريف",
    "اعتماد/الغاء الترسية",
    "لجنة الاستثمار",
    "تسليم الموقع  للمستثمر",
    "إعداد العقد"
];

// يرتّب صفوف المرحلة حسب سير العملية أعلاه، وأي مرحلة جديدة غير معروفة بالترتيب الثابت
// (لو أُضيفت مستقبلاً بالملف) تُلحَق بالنهاية بدل أن تختفي
function orqOrderStagesByProcess(stageRows) {
    const known = ORQ_STAGE_PROCESS_ORDER
        .map(function (name) { return (stageRows || []).find(function (r) { return r.name === name; }); })
        .filter(Boolean);
    const extra = (stageRows || []).filter(function (r) { return ORQ_STAGE_PROCESS_ORDER.indexOf(r.name) === -1; });
    return known.concat(extra);
}

function orqStatusRowsForStage(stageStatusBreakdown, stage) {
    return (stageStatusBreakdown || [])
        .filter(function (r) { return r.stage === stage; })
        .map(function (r) { return { status: r.status, count: r.count }; })
        .sort(function (a, b) { return b.count - a.count; });
}

// selectedStatus: اسم الحالة المختارة حاليًا لتمييزها (خلفية + مؤشر) - null يعني لا شيء مختار
function orqBuildMiniStatusRows(rows, selectedStatus) {
    const max = rows.length ? rows[0].count : 0;
    return rows.map(function (r) {
        var pct = max > 0 ? Math.round((r.count / max) * 100) : 0;
        var selected = r.status === selectedStatus;
        return '<div class="orq-multiple-row' + (selected ? " selected" : "") + '" data-status="' + orqEscape(r.status) + '" title="' + orqEscape(r.status) + '">' +
            '<span class="status-label">' + orqEscape(r.status) + '</span>' +
            '<span class="bar-track"><span class="bar-fill" style="width:' + pct + '%;"></span></span>' +
            '<span class="status-count">' + orqFormatNum(r.count) + '</span>' +
        '</div>';
    }).join("");
}

let orqSelectedStage = null;
let orqSelectedStatus = null; // null = كل حالات المرحلة المختارة (بلا تحديد حالة بعينها)

function orqRenderStageTimeline(data) {
    const wrap = document.getElementById("orqStageTimeline");
    if (!wrap) return;

    const stageRows = orqOrderStagesByProcess(data.stageBreakdown || []);
    if (!stageRows.length) { wrap.innerHTML = ""; return; }
    if (!orqSelectedStage || !stageRows.some(function (r) { return r.name === orqSelectedStage; })) {
        orqSelectedStage = stageRows[0].name;
        orqSelectedStatus = null;
    }

    function nodesHtml() {
        return stageRows.map(function (r) {
            var active = r.name === orqSelectedStage;
            return '<button type="button" class="orq-timeline-node' + (active ? " active" : "") + '" data-name="' + orqEscape(r.name) + '">' +
                '<span class="orq-timeline-dot"></span>' +
                '<span class="orq-timeline-label">' + orqEscape(r.name) + '</span>' +
                '<span class="orq-timeline-count">' + orqFormatNum(r.count) + ' طلب</span>' +
            '</button>';
        }).join("");
    }

    // ملاحظة: id="orqTimelineDetail" جزء من النص المُرجَع نفسه (لا غلاف خارجي إضافي) حتى
    // يبقى قابلاً للعثور عليه بعد كل استبدال عبر outerHTML (الاستبدال يُنشئ عنصرًا DOM جديدًا
    // فيُعاد تشغيل حركة الظهور CSS تلقائيًا في كل مرة - وهذا هو الغرض من outerHTML هنا بدل innerHTML)
    function detailHtml() {
        var stageRow = stageRows.find(function (r) { return r.name === orqSelectedStage; });
        var statusRows = orqStatusRowsForStage(data.stageStatusBreakdown, orqSelectedStage);
        return '<div id="orqTimelineDetail" class="orq-timeline-detail">' +
            '<div class="orq-timeline-connector" id="orqTimelineConnector"></div>' +
            '<div class="orq-timeline-detail-header">' +
                '<div class="orq-timeline-detail-title">حالات الطلب: ' + orqEscape(orqSelectedStage) + '</div>' +
                '<div class="orq-timeline-detail-total">' + orqFormatNum(stageRow ? stageRow.count : 0) + ' طلب</div>' +
            '</div>' +
            (statusRows.length ? orqBuildMiniStatusRows(statusRows, orqSelectedStatus) : '<div style="font-size:.8rem;color:var(--muted);">لا توجد حالات مسجّلة</div>') +
            '<div class="orq-timeline-detail-hint">اضغط حالة لعرض توزيعها على المسؤولين بالرسم أسفل، أو اضغطها ثانية للرجوع لكل حالات المرحلة</div>' +
        '</div>';
    }

    // إعادة ربط الضغط على صفوف الحالات بعد كل رسم/استبدال لتفاصيل المرحلة (عناصر DOM جديدة
    // في كل مرة) - الضغط على حالة مختارة فعلاً يلغي التحديد (رجوع لعرض كل حالات المرحلة)
    function wireStatusRowClicks() {
        var detailWrap = document.getElementById("orqTimelineDetail");
        if (!detailWrap) return;
        detailWrap.querySelectorAll(".orq-multiple-row").forEach(function (row) {
            row.addEventListener("click", function () {
                var status = row.getAttribute("data-status");
                orqSelectedStatus = (orqSelectedStatus === status) ? null : status;
                var oldDetail = document.getElementById("orqTimelineDetail");
                if (oldDetail) oldDetail.outerHTML = detailHtml();
                wireStatusRowClicks();
                orqRenderOfficerChart(data);
            });
        });
    }

    // سهم صغير يربط بصريًا عقدة المرحلة المختارة بصندوق حالاتها أسفلها - موضعه (بالبكسل) نسبةً
    // لصندوق التفاصيل نفسه (لا الغلاف الخارجي) حتى تبقى القيمة صحيحة بغض النظر عن ترقيم/عرض
    // العناصر الأخرى، ويُعاد حسابه عند كل تغيير مرحلة لأن عرض كل عقدة يختلف باختلاف اسمها
    function positionConnector() {
        var activeNode = wrap.querySelector(".orq-timeline-node.active");
        var detailBox = document.getElementById("orqTimelineDetail");
        var connector = document.getElementById("orqTimelineConnector");
        if (!activeNode || !detailBox || !connector) return;
        var boxRect = detailBox.getBoundingClientRect();
        var nodeRect = activeNode.getBoundingClientRect();
        connector.style.left = (nodeRect.left + nodeRect.width / 2 - boxRect.left) + "px";
    }

    wrap.innerHTML =
        '<div class="chart-card">' +
            '<div class="chart-header"><div class="chart-title">مراحل الطلب</div><div class="chart-badge">اضغط مرحلة لعرض حالاتها</div></div>' +
            '<div class="orq-timeline-body" id="orqTimelineBody">' +
                '<div class="orq-timeline-track"><div class="orq-timeline-line"></div>' + nodesHtml() + '</div>' +
                detailHtml() +
            '</div>' +
        '</div>';
    wireStatusRowClicks();
    requestAnimationFrame(positionConnector);
    window.addEventListener("resize", positionConnector);

    wrap.querySelectorAll(".orq-timeline-node").forEach(function (el) {
        el.addEventListener("click", function () {
            var name = el.getAttribute("data-name");
            if (!name || name === orqSelectedStage) return;
            orqSelectedStage = name;
            orqSelectedStatus = null;
            wrap.querySelectorAll(".orq-timeline-node").forEach(function (n) {
                n.classList.toggle("active", n.getAttribute("data-name") === orqSelectedStage);
            });
            var detailWrap = document.getElementById("orqTimelineDetail");
            if (detailWrap) detailWrap.outerHTML = detailHtml();
            wireStatusRowClicks();
            positionConnector();
            orqRenderOfficerChart(data);
            orqUpdateOverdueArc(data);
        });
    });

    orqRenderOfficerChart(data);
    orqUpdateOverdueArc(data);
}

/* ==========================================================================
   2) Lollipop أفقي مستقل: توزيع الطلبات على المسؤولين (عمود "أسم السلة") - مرتبط بالـTimeline
   أعلاه بدل تضمين تفاصيله بداخله (حتى لا يزدحم): يعرض توزيع المرحلة المختارة على كل السلة،
   أو توزيع حالة واحدة بعينها إن اختارها المستخدم من قائمة الحالات تحت الـTimeline. عمود
   واحد بلون واحد (سلة واحدة = بلا حاجة لتلوين تصنيفي)، عدا "غير محددة" (بلا سلة/مسؤول بعد)
   التي تُبرَز بلون تنبيه لأنها حالة تستحق المتابعة. عنصر ECharts واحد يُحدَّث بـsetOption
   بدل إعادة إنشائه في كل ضغطة (أداء أفضل وحركة انتقال أنعم بين البيانات)
   ========================================================================== */
function orqBasketRowsFor(triples, stage, status) {
    const totals = {};
    (triples || []).forEach(function (r) {
        if (r.stage !== stage) return;
        if (status && r.status !== status) return;
        totals[r.basket] = (totals[r.basket] || 0) + r.count;
    });
    return Object.keys(totals)
        .map(function (name) { return { name: name, count: totals[name] }; })
        .sort(function (a, b) { return b.count - a.count; });
}

function orqBuildOfficerLollipopOption(rows) {
    const labels = rows.map(function (r) { return r.name; });
    const values = rows.map(function (r) { return r.count; });
    const colors = rows.map(function (r) { return r.name === "غير محددة" ? ORQ_C.warn : ORQ_C.mid; });
    return {
        tooltip: orqTooltip({
            trigger: "item",
            formatter: function (p) {
                var i = p.dataIndex;
                return '<div style="font-weight:700;">' + orqEscape(labels[i]) + '</div><div>العدد: <strong>' + orqFormatNum(values[i]) + '</strong></div>';
            }
        }),
        grid: { left: 10, right: 50, top: 6, bottom: 6, containLabel: true },
        yAxis: orqAxisStyle({ type: "category", data: labels, inverse: true, axisLabel: { fontSize: 11, width: 120, overflow: "break" } }),
        xAxis: orqAxisStyle({ type: "value", min: 0, axisLabel: { show: false }, splitLine: { show: false } }),
        series: [
            {
                name: "العصا", type: "bar", data: values, barWidth: 3,
                itemStyle: { color: function (p) { return colors[p.dataIndex]; } },
                silent: true, tooltip: { show: false }
            },
            {
                name: "العدد", type: "scatter", data: values, symbolSize: 18,
                itemStyle: { color: function (p) { return colors[p.dataIndex]; } },
                label: { show: true, position: "right", formatter: function (p) { return orqFormatNum(p.value); }, color: ORQ_C.text, fontWeight: 800, fontSize: 11 }
            }
        ]
    };
}

let orqOfficerChart = null;

function orqRenderOfficerChart(data) {
    const container = document.getElementById("orqOfficerChart");
    if (!container) return;

    const rows = orqBasketRowsFor(data.stageStatusBasketBreakdown, orqSelectedStage, orqSelectedStatus);
    const boxHeight = Math.max(110, rows.length * 24);

    if (!orqOfficerChart) {
        container.innerHTML =
            '<div class="chart-card">' +
                '<div class="chart-header"><div class="chart-title">توزيع الطلبات على المسؤولين</div><div class="chart-badge" id="orqOfficerBadge"></div></div>' +
                '<div class="echart-box" id="orqOfficerBox" style="height:' + boxHeight + 'px;"></div>' +
            '</div>';
        orqOfficerChart = echarts.init(document.getElementById("orqOfficerBox"));
        orqCharts.push(orqOfficerChart);
    } else {
        var box = document.getElementById("orqOfficerBox");
        if (box) box.style.height = boxHeight + "px";
    }

    orqOfficerChart.setOption(orqBuildOfficerLollipopOption(rows), true);
    requestAnimationFrame(function () { try { orqOfficerChart.resize(); } catch (e) {} });

    const badge = document.getElementById("orqOfficerBadge");
    if (badge) badge.textContent = orqSelectedStatus ? orqSelectedStatus : (orqSelectedStage || "");
}

/* ==========================================================================
   دونات: عدد الطلبات حسب المرحلة - لون ثابت لكل مرحلة (orqColorForName، نفس ألوان عقد
   الـTimeline أعلاه) بلا Legend داخلي (أسماء المراحل طويلة)، بدلها قائمة نصية أسفل الدونات
   ========================================================================== */
function orqBuildStageDonutOption(rows, total) {
    const items = rows.map(function (r) {
        return { name: r.name, value: r.count, itemStyle: { color: orqColorForName(r.name) } };
    });
    return {
        tooltip: orqTooltip({
            trigger: "item",
            formatter: function (p) {
                var pct = total > 0 ? Math.round((p.value / total) * 1000) / 10 : 0;
                return '<div style="font-weight:700;">' + orqEscape(p.name) + '</div><div>العدد: <strong>' + orqFormatNum(p.value) + '</strong> (' + pct + '%)</div>';
            }
        }),
        series: [{
            type: "pie", radius: ["46%", "76%"], center: ["50%", "50%"],
            itemStyle: { borderColor: "#fff", borderWidth: 2 },
            label: { show: false }, labelLine: { show: false },
            data: items
        }],
        graphic: [
            { type: "text", left: "center", top: "39%", style: { text: orqFormatNum(total), fontSize: 20, fontWeight: 800, fill: ORQ_C.text, fontFamily: "Cairo", textAlign: "center" } },
            { type: "text", left: "center", top: "55%", style: { text: "إجمالي الطلبات", fontSize: 10, fontWeight: 700, fill: ORQ_C.muted, fontFamily: "Cairo", textAlign: "center" } }
        ]
    };
}

function orqRenderNameCountDetail(wrapId, rows, total) {
    const wrap = document.getElementById(wrapId);
    if (!wrap) return;
    wrap.innerHTML = (rows || []).map(function (r) {
        var pct = total > 0 ? Math.round((r.count / total) * 1000) / 10 : 0;
        var color = orqColorForName(r.name);
        return '<div style="display:flex;align-items:center;justify-content:space-between;gap:10px;padding:7px 0;border-bottom:1px solid var(--border);">' +
            '<span style="display:flex;align-items:center;gap:8px;font-size:.92rem;font-weight:700;color:var(--text);">' +
                '<span style="width:12px;height:12px;border-radius:4px;flex:none;background:' + color + ';"></span> ' + orqEscape(r.name) +
            '</span>' +
            '<span style="font-size:.92rem;font-weight:800;white-space:nowrap;">' + orqFormatNum(r.count) + ' <span style="font-weight:600;color:var(--muted);font-size:.82rem;">(' + pct + '%)</span></span>' +
        '</div>';
    }).join("");
}

function orqRenderStageDonut(data) {
    const rows = data.stageBreakdown || [];
    const total = rows.reduce(function (s, r) { return s + r.count; }, 0);
    const row = document.getElementById("orqOverviewRow");
    if (!row) return;
    const id = "orqStageDonut_" + Date.now();
    const div = document.createElement("div");
    div.className = "chart-card";
    div.innerHTML =
        '<div class="chart-header"><div class="chart-title">توزيع الطلبات حسب المرحلة</div></div>' +
        '<div class="echart-box" id="' + id + '" style="height:230px;max-width:300px;margin:0 auto;"></div>' +
        '<div id="orqStageDonutDetail" style="margin-top:8px;"></div>';
    row.appendChild(div);
    const chart = echarts.init(document.getElementById(id));
    chart.setOption(orqBuildStageDonutOption(rows, total));
    orqCharts.push(chart);
    requestAnimationFrame(function () { try { chart.resize(); } catch (e) {} });
    orqRenderNameCountDetail("orqStageDonutDetail", rows, total);
}

function orqFormatDate(iso) {
    if (!iso) return "لا يوجد تاريخ";
    var d = new Date(iso);
    return isNaN(d.getTime()) ? "لا يوجد تاريخ" : d.toLocaleDateString("en-GB");
}

/* ==========================================================================
   نافذة تفاصيل مركزية (Data Modal): تظهر كمربع في نص الصفحة (overlay + مربع) بدل إضافة قائمة
   ضمن الصفحة نفسها أو درج جانبي - مكوّن .orq-modal بالموقع. يُستخدم هنا لعرض قائمة الطلبات
   عند الضغط على أي جزء لوني بقوس "+90 يوم"، والضغط على طلب من القائمة يعرض تفاصيله مع زر رجوع
   ========================================================================== */
let orqDrawerBuilt = false;
let orqDrawerOpen = false;
let orqDrawerZone = null; // "red" | "green" | null (لإعادة بناء نفس القائمة عند تغيّر المرحلة والنافذة مفتوحة)
let orqDrawerRows = [];

function orqEnsureDrawer() {
    if (orqDrawerBuilt) return;
    const wrap = document.createElement("div");
    wrap.innerHTML =
        '<div class="orq-modal-overlay" id="orqDrawerOverlay">' +
            '<div class="orq-modal" id="orqDrawer">' +
                '<div class="data-drawer-header">' +
                    '<div><div class="data-drawer-title" id="orqDrawerTitle"></div><div class="data-drawer-subtitle" id="orqDrawerSubtitle"></div></div>' +
                    '<button type="button" class="data-drawer-close" id="orqDrawerCloseBtn" aria-label="إغلاق"><i class="bi bi-x-lg"></i></button>' +
                '</div>' +
                '<div class="data-drawer-body" id="orqDrawerBody"></div>' +
            '</div>' +
        '</div>';
    document.body.appendChild(wrap);
    // المربع أصبح داخل الـoverlay (لتوسيطه بالفليكس) - الإغلاق بالضغط على الخلفية فقط لا المربع نفسه
    document.getElementById("orqDrawerOverlay").addEventListener("click", function (e) {
        if (e.target === e.currentTarget) orqCloseDrawer();
    });
    document.getElementById("orqDrawerCloseBtn").addEventListener("click", orqCloseDrawer);
    document.addEventListener("keydown", function (e) { if (e.key === "Escape" && orqDrawerOpen) orqCloseDrawer(); });
    orqDrawerBuilt = true;
}

function orqCloseDrawer() {
    orqDrawerOpen = false;
    orqDrawerZone = null;
    var overlay = document.getElementById("orqDrawerOverlay");
    var drawer = document.getElementById("orqDrawer");
    if (overlay) overlay.classList.remove("open");
    if (drawer) drawer.classList.remove("open");
}

function orqDrawerRenderList() {
    var body = document.getElementById("orqDrawerBody");
    if (!body) return;
    if (!orqDrawerRows.length) {
        body.innerHTML = '<div class="data-drawer-empty">لا توجد طلبات مطابقة</div>';
        return;
    }
    body.innerHTML = orqDrawerRows.map(function (r, i) {
        return '<div class="data-drawer-list-item" data-idx="' + i + '">' +
            '<div class="main">' +
                '<div class="line1">' + orqEscape(r.requestNumber || "—") + (r.opportunityName ? ' — ' + orqEscape(r.opportunityName) : '') + '</div>' +
                '<div class="line2">آخر تحديث: ' + orqFormatDate(r.lastUpdateDate) + '</div>' +
            '</div>' +
            '<div class="days-badge">' + orqFormatNum(r.daysSinceUpdate) + ' يوم</div>' +
        '</div>';
    }).join("");
    body.querySelectorAll(".data-drawer-list-item").forEach(function (item) {
        item.addEventListener("click", function () { orqDrawerRenderDetail(parseInt(item.getAttribute("data-idx"), 10)); });
    });
}

function orqDrawerRenderDetail(idx) {
    var body = document.getElementById("orqDrawerBody");
    var r = orqDrawerRows[idx];
    if (!body || !r) return;
    body.innerHTML =
        '<button type="button" class="data-drawer-back" id="orqDrawerBackBtn"><i class="bi bi-arrow-right"></i><span>رجوع للقائمة</span></button>' +
        '<div class="data-drawer-detail-row"><span class="label">اسم الفرصة</span><span class="value">' + orqEscape(r.opportunityName || "—") + '</span></div>' +
        '<div class="data-drawer-detail-row"><span class="label">رقم الطلب</span><span class="value">' + orqEscape(r.requestNumber || "—") + '</span></div>' +
        '<div class="data-drawer-detail-row"><span class="label">رقم الفرصة</span><span class="value">' + orqEscape(r.opportunityNumber || "—") + '</span></div>' +
        '<div class="data-drawer-detail-row"><span class="label">تاريخ آخر تحديث</span><span class="value">' + orqFormatDate(r.lastUpdateDate) + '</span></div>' +
        '<div class="data-drawer-detail-row danger"><span class="label">عدد أيام التأخر</span><span class="value">' + orqFormatNum(r.daysSinceUpdate) + ' يوم</span></div>';
    var back = document.getElementById("orqDrawerBackBtn");
    if (back) back.addEventListener("click", orqDrawerRenderList);
}

function orqOpenOverdueDrawer(isRed, data) {
    orqEnsureDrawer();
    orqDrawerZone = isRed ? "red" : "green";
    var rows = orqOverdueRowsForSelectedStage(data || orqData)
        .filter(function (r) { return r.isOverdue === isRed; })
        .sort(function (a, b) { return b.daysSinceUpdate - a.daysSinceUpdate; });
    orqDrawerRows = rows;

    var titleEl = document.getElementById("orqDrawerTitle");
    var subtitleEl = document.getElementById("orqDrawerSubtitle");
    if (titleEl) titleEl.textContent = isRed ? "الطلبات المتأخرة (+90 يوم)" : "الطلبات ضمن المدة";
    if (subtitleEl) subtitleEl.textContent = (orqSelectedStage ? orqSelectedStage + " — " : "") + orqFormatNum(rows.length) + " طلب";

    orqDrawerRenderList();

    document.getElementById("orqDrawerOverlay").classList.add("open");
    document.getElementById("orqDrawer").classList.add("open");
    orqDrawerOpen = true;
}

/* ==========================================================================
   قوس أحمر/أخضر: عدد الأيام من تاريخ التحديث (تجاوز 90 يوم = أحمر) لطلبات المرحلة المختارة
   حاليًا بالـTimeline أعلاه (تتحدّث القوس فورًا عند اختيار مرحلة أخرى) - الضغط على أي جزء لوني
   بالقوس يفتح درج التفاصيل (orqOpenOverdueDrawer) بقائمة الطلبات المطابقة
   ========================================================================== */
function orqOverdueRowsForSelectedStage(data) {
    var rows = (data && data.overdueDetailRows) || [];
    if (orqSelectedStage) rows = rows.filter(function (r) { return r.stage === orqSelectedStage; });
    return rows;
}

function orqBuildOverdueArcOption(redPct, redCount, greenCount) {
    const total = redCount + greenCount;
    return {
        tooltip: orqTooltip({
            formatter: function (p) {
                if (p.data.name === "gap") return "";
                var label = p.data.name === "red" ? "تجاوز 90 يومًا منذ آخر تحديث" : "ضمن 90 يومًا";
                return '<div style="font-weight:700;">' + label + '</div><div>' + orqFormatNum(p.data.value) + ' طلب</div>';
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
                { value: redCount, name: "red", itemStyle: { color: ORQ_C.red } },
                { value: greenCount, name: "green", itemStyle: { color: ORQ_C.limeDk } },
                { value: total * 0.5, name: "gap", itemStyle: { color: "transparent" }, label: { show: false }, tooltip: { show: false }, silent: true }
            ]
        }],
        graphic: [
            { type: "text", left: "center", top: "48%", style: { text: redPct.toFixed(1) + "%", fontSize: 22, fontWeight: 800, fill: ORQ_C.text, fontFamily: "Cairo", textAlign: "center" } },
            { type: "text", left: "center", top: "62%", style: { text: "تجاوزت 90 يوم", fontSize: 10, fontWeight: 700, fill: ORQ_C.muted, textAlign: "center" } }
        ]
    };
}

let orqOverdueGauge = null;

function orqRenderOverdueArc(data) {
    const gaugeRow = document.getElementById("orqOverviewRow");
    if (!gaugeRow) return;

    const div = document.createElement("div");
    div.className = "chart-card tarh-duration-card";
    const gaugeId = "orqOverdueGauge_" + Date.now();
    div.innerHTML =
        '<div class="tarh-duration-header"><div class="tarh-duration-title" id="orqOverdueTitle"><i class="bi bi-hourglass-split"></i> الطلبات المتأخرة عن التحديث (+90 يوم)</div></div>' +
        '<div class="echart-box" id="' + gaugeId + '"></div>' +
        '<div class="tarh-duration-legend" id="orqOverdueLegend"></div>';
    gaugeRow.appendChild(div);

    const gaugeBox = document.getElementById(gaugeId);
    gaugeBox.style.height = "230px";
    orqOverdueGauge = echarts.init(gaugeBox);
    orqCharts.push(orqOverdueGauge);

    orqOverdueGauge.on("click", function (params) {
        if (!params.data || params.data.name === "gap") return;
        orqOpenOverdueDrawer(params.data.name === "red", data);
    });

    orqUpdateOverdueArc(data);
}

// يُعاد استدعاؤها عند كل تغيير لاختيار المرحلة بالـTimeline أعلاه (بلا إعادة إنشاء الرسم) حتى
// يعكس القوس فورًا طلبات المرحلة المختارة فقط - وإن كان درج التفاصيل مفتوحًا حاليًا يُحدَّث
// محتواه أيضًا ليطابق المرحلة الجديدة بدل أن يبقى معروضًا لمرحلة سابقة
function orqUpdateOverdueArc(data) {
    if (!orqOverdueGauge) return;

    const rows = orqOverdueRowsForSelectedStage(data);
    const redCount = rows.filter(function (r) { return r.isOverdue; }).length;
    const greenCount = rows.length - redCount;
    const redPct = rows.length > 0 ? (redCount / rows.length) * 100 : 0;

    orqOverdueGauge.setOption(orqBuildOverdueArcOption(redPct, redCount, greenCount), true);
    requestAnimationFrame(function () { try { orqOverdueGauge.resize(); } catch (e) {} });

    const titleEl = document.getElementById("orqOverdueTitle");
    if (titleEl) {
        titleEl.innerHTML = '<i class="bi bi-hourglass-split"></i> الطلبات المتأخرة عن التحديث (+90 يوم)' +
            (orqSelectedStage ? ' — ' + orqEscape(orqSelectedStage) : '');
    }
    const legend = document.getElementById("orqOverdueLegend");
    if (legend) {
        legend.innerHTML =
            '<div class="tarh-duration-legend-item"><span class="tarh-duration-dot" style="background:' + ORQ_C.red + ';"></span> تجاوزت 90 يوم — ' + orqFormatNum(redCount) + '</div>' +
            '<div class="tarh-duration-legend-item"><span class="tarh-duration-dot" style="background:' + ORQ_C.limeDk + ';"></span> ضمن 90 يوم — ' + orqFormatNum(greenCount) + '</div>';
    }

    if (orqDrawerOpen && orqDrawerZone) orqOpenOverdueDrawer(orqDrawerZone === "red", data);
}

/* ==========================================================================
   رفع الملف
   ========================================================================== */
function orqSetUploadStatus(text, isError) {
    const el = document.getElementById("orqUploadStatus");
    if (!el) return;
    el.textContent = text || "";
    el.className = "tc-upload-status" + (isError ? " tc-upload-status-error" : text ? " tc-upload-status-ok" : "");
}

function orqInitUpload() {
    const btn = document.getElementById("orqUploadBtn");
    const input = document.getElementById("orqUploadInput");
    if (!btn || !input) return;

    btn.addEventListener("click", function () { input.click(); });
    input.addEventListener("change", async function () {
        const file = input.files && input.files[0];
        if (!file) return;

        btn.disabled = true;
        orqSetUploadStatus("جارِ رفع الملف...", false);
        try {
            const formData = new FormData();
            formData.append("openRequestsFile", file);
            const resp = await fetch(window.ORQ_UPLOAD_URL, { method: "POST", body: formData });
            const result = await resp.json();
            if (result.success) {
                orqSetUploadStatus(result.message, false);
                orqLoad();
            } else {
                orqSetUploadStatus(result.message || "تعذّر رفع الملف.", true);
            }
        } catch (err) {
            console.error("تعذّر رفع ملف الطلبات المفتوحة:", err);
            orqSetUploadStatus("تعذّر رفع الملف، تحقق من الاتصال وحاول مجددًا.", true);
        } finally {
            btn.disabled = false;
            input.value = "";
        }
    });
}

/* ==========================================================================
   التحميل الرئيسي
   ========================================================================== */
async function orqLoad() {
    const contentEl = document.getElementById("orqContent");
    const noDataEl = document.getElementById("orqNoDataBox");

    try {
        const resp = await fetch(window.ORQ_DATA_URL + "?" + orqQueryString());
        const data = await resp.json();
        orqData = data;

        orqBuildFilterBar(data);

        if (!data.hasData) {
            if (contentEl) contentEl.style.display = "none";
            if (noDataEl) noDataEl.style.display = "";
            return;
        }

        if (contentEl) contentEl.style.display = "";
        if (noDataEl) noDataEl.style.display = "none";

        orqDisposeCharts();
        orqOfficerChart = null; // ديسبوز أعلاه أتلف نسخة ECharts القديمة - لازم إعادة إنشائها لا setOption عليها
        orqOverdueGauge = null;
        orqCloseDrawer();
        ["orqStageTimeline", "orqOfficerChart", "orqOverviewRow"].forEach(function (id) {
            const el = document.getElementById(id);
            if (el) el.innerHTML = "";
        });

        orqRenderKpiCards(data);
        orqBuildYearBox(data);
        orqRenderStageTimeline(data);
        orqRenderStageDonut(data);
        orqRenderOverdueArc(data);
    } catch (err) {
        console.error("تعذّر تحميل بيانات الطلبات المفتوحة:", err);
        if (contentEl) contentEl.style.display = "none";
        if (noDataEl) noDataEl.style.display = "";
    }
}

document.addEventListener("DOMContentLoaded", function () {
    orqInitUpload();
    if (window.ORQ_DATA_URL) orqLoad();
});
