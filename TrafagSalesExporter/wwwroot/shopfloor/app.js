/* COCKPIT-ANPASSUNGEN (2026-10-07, Integration in das Trafag Cockpit, siehe docs/SHOPFLOOR_2026-10-07.md):
   1. Alle API-Aufrufe sind relativ ("api/..." statt "/api/..."), damit sie unter /BiDashboard/shopfloor/ aufloesen.
   2. Das Kuerzel kommt vom Windows-Konto (GET api/whoami). Der Server verwendet ohnehin das Windows-Konto.
   3. Der Header X-User wird immer gesendet (Schutz gegen Fremdaufrufe), Mail-Links zeigen auf diese Seite.
   Der Rest ist unveraendert gegenueber PPA-Shopfloor vom 2026-10-05. */
/* PPA Shop-Floor – Oberfläche (ohne externe Bibliotheken) */
"use strict";

/* ---------------------------------------------------------------- Cockpit-Design (2026-10-08)
   themeSync: uebernimmt Hell/Dunkel, Skin (ci/classic) und die echten MudBlazor-Farben des Cockpits.
   Die Seite laeuft im iframe derselben Herkunft, daher ist window.parent.document lesbar.
   - data-theme / data-skin am <html> des Cockpits (Quelle: App.razor / theme.js), sonst localStorage
     "trafag-theme" / "trafag-skin", sonst dunkel + ci.
   - Die berechneten --mud-palette-* Werte des Cockpits (inkl. Dimmer) werden auf <html> dieser Seite kopiert;
     fehlen sie (Direktaufruf), gelten die Rueckfallwerte in style.css.
   - Reaktion auf Aenderungen: MutationObserver auf <html> und <head> des Cockpits (MudBlazor schreibt sein
     Theme in ein <style>), dazu ein Poll alle 1,5 s als Absicherung.
   - Eingebettet (window.parent !== window) bekommt <html> die Klasse "embedded": der eigene Logo-Block
     entfaellt, das Cockpit zeigt das Logo schon in der Kopfleiste. */
(function themeSync() {
  const root = document.documentElement;
  const VARS = ["background", "surface", "text-primary", "text-secondary", "lines-default", "primary"].map(n => "--mud-palette-" + n);
  let last = "";
  let embedded = false;
  try { embedded = window.parent !== window; } catch (e) { embedded = true; }
  root.classList.toggle("embedded", embedded);
  function readParent() {
    try { return embedded ? window.parent.document.documentElement : null; } catch (e) { return null; }
  }
  function apply() {
    const par = readParent();
    let theme = null, skin = null;
    if (par) { theme = par.getAttribute("data-theme"); skin = par.getAttribute("data-skin"); }
    if (!theme) { try { theme = localStorage.getItem("trafag-theme"); } catch (e) { } }
    if (!skin) { try { skin = localStorage.getItem("trafag-skin"); } catch (e) { } }
    theme = theme === "light" ? "light" : "dark"; skin = skin === "classic" ? "classic" : "ci";
    const vals = {};
    if (par) {
      const cs = par.ownerDocument.defaultView.getComputedStyle(par);
      VARS.forEach(v => { const x = cs.getPropertyValue(v).trim(); if (x) vals[v] = x; });
    }
    const sig = theme + "|" + skin + "|" + VARS.map(v => vals[v] || "").join("|");
    if (sig === last) return;
    last = sig;
    root.setAttribute("data-theme", theme); root.setAttribute("data-skin", skin);
    VARS.forEach(v => { if (vals[v]) root.style.setProperty(v, vals[v]); else root.style.removeProperty(v); });
  }
  apply();
  try {
    const par = readParent();
    if (par) {
      const mo = new MutationObserver(apply);
      mo.observe(par, { attributes: true, attributeFilter: ["data-theme", "data-skin", "style", "class"] });
      mo.observe(par.ownerDocument.head, { childList: true, subtree: true, characterData: true });
    }
  } catch (e) { }
  setInterval(apply, 1500);
  window.addEventListener("storage", apply);
})();

const S = {
  cfg: null, mods: {}, user: "", route: null,
  cache: {},                 // module -> records
  daily: {},                 // module -> {day: {data, meta}}
  dirty: false, drawerOpen: false,
  meeting: null,             // {start, idx}
  week: null,                // Montag der angezeigten Woche (Date)
  activeDay: null,           // für schmale Bildschirme
  list: {},                  // pro Modul: {q, status, dept, sort, dir, limit}
};

/* ---------------------------------------------------------------- Speicher
   In abgeschotteten Ansichten (z. B. SharePoint-/Teams-Vorschau) ist localStorage gesperrt.
   Dann wird nur im Arbeitsspeicher gehalten, statt abzustürzen. */
function safeStore(kind) {
  const mem = {};
  try { const st = window[kind]; const t = "__sf_test"; st.setItem(t, "1"); st.removeItem(t); return st; }
  catch (e) { window.SF_NO_STORAGE = true; }
  return { getItem: k => (k in mem ? mem[k] : null), setItem: (k, v) => { mem[k] = String(v); }, removeItem: k => { delete mem[k]; } };
}
const LS = safeStore("localStorage"), SS = safeStore("sessionStorage");

/* ---------------------------------------------------------------- Helfer */
const $ = (s, el = document) => el.querySelector(s);
const esc = s => String(s ?? "").replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
const iso = d => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
const parseIso = s => { const [y, m, d] = s.split("-").map(Number); return new Date(y, m - 1, d); };
const todayIso = () => iso(new Date());
const addDaysD = (d, n) => { const x = new Date(d); x.setDate(x.getDate() + n); return x; };
const WD = ["So", "Mo", "Di", "Mi", "Do", "Fr", "Sa"];
const WD_LONG = ["Sonntag", "Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag", "Samstag"];
const MONTHS = ["Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember"];

function isoWeek(d) {
  const t = new Date(Date.UTC(d.getFullYear(), d.getMonth(), d.getDate()));
  const day = t.getUTCDay() || 7; t.setUTCDate(t.getUTCDate() + 4 - day);
  const y0 = new Date(Date.UTC(t.getUTCFullYear(), 0, 1));
  return Math.ceil(((t - y0) / 86400000 + 1) / 7);
}
const monday = d => { const x = new Date(d.getFullYear(), d.getMonth(), d.getDate()); const wd = x.getDay() || 7; return addDaysD(x, 1 - wd); };
const fmtDate = s => { if (!s) return ""; const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(s); return m ? `${m[3]}.${m[2]}.${m[1]}` : String(s); };
const f2 = v => (v == null || !isFinite(v)) ? "–" : new Intl.NumberFormat("de-CH", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(v);
const nf0 = new Intl.NumberFormat("de-CH", { maximumFractionDigits: 0 });
const nf2 = new Intl.NumberFormat("de-CH", { maximumFractionDigits: 2 });
function fmtNum(v, digits) {
  if (v === null || v === undefined || v === "" || (typeof v === "number" && !isFinite(v))) return "";
  if (typeof v !== "number") return String(v);
  if (digits !== undefined) return new Intl.NumberFormat("de-CH", { maximumFractionDigits: digits, minimumFractionDigits: 0 }).format(v);
  return Math.abs(v) >= 1000 ? nf0.format(v) : nf2.format(v);
}
function parseNum(s) {
  if (s === null || s === undefined) return null;
  const t = String(s).trim().replace(/['’\s]/g, "").replace(",", ".");
  if (t === "") return null;
  const n = Number(t); return isNaN(n) ? String(s).trim() : n;
}
// Eingabe "5.10." / "5.10.26" / "05.10.2026" -> ISO
function parseDateInput(s) {
  s = String(s || "").trim(); if (!s) return null;
  if (/^\d{4}-\d{2}-\d{2}$/.test(s)) return s;
  const m = /^(\d{1,2})\.(\d{1,2})\.?(\d{2,4})?$/.exec(s);
  if (!m) return s;
  let y = m[3] ? Number(m[3]) : new Date().getFullYear(); if (y < 100) y += 2000;
  return `${y}-${String(m[2]).padStart(2, "0")}-${String(m[1]).padStart(2, "0")}`;
}
const isClosed = (st, m) => { const s = String(st || "").trim().toLowerCase(); return s.startsWith("erl") || ((m && m.closed) || S.cfg.closed_status).includes(s); };
function statusClass(st, m) {
  const s = String(st || "").toLowerCase();
  if (!s) return "";
  if (isClosed(s, m)) return "ok";
  if (/fehlmat|gesperrt|q-problem|engpass|hängt|stillstand/.test(s)) return "bad";
  if (/offen|abklärung|warten|kapazität|fehlt|48h/.test(s)) return "warn";
  return "info";
}
function toast(msg, bad) {
  const t = $("#toast"); t.textContent = msg; t.className = "toast show" + (bad ? " bad" : "");
  clearTimeout(toast._t); toast._t = setTimeout(() => t.className = "toast", 2600);
}

/* ---------------------------------------------------------------- API */
async function api(method, url, body) {
  if (window.SF_DEMO) return window.SF_DEMO.handle(method, url, body, S.user);
  const r = await fetch(url, {
    method, headers: { "Content-Type": "application/json", "X-User": encodeURIComponent(S.user || "-") },
    body: body ? JSON.stringify(body) : undefined,
  });
  if (!r.ok) { let m = r.statusText; try { m = (await r.json()).error || m; } catch (e) { } throw new Error(m); }
  return r.json();
}
const setSync = txt => { $("#syncState").textContent = txt; };

/* ---------------------------------------------------------------- Berechnungen */
const calcCache = {};
function calcFn(expr) {
  if (!calcCache[expr]) {
    try { calcCache[expr] = new Function("v", "ref", "prev", "addDays", `"use strict"; return (${expr});`); }
    catch (e) { calcCache[expr] = () => null; }
  }
  return calcCache[expr];
}
const usedKeys = expr => [...expr.matchAll(/v\.(\w+)/g)].map(m => m[1]);

/** Berechnet alle calc-Felder eines Datensatzes. ctx: {module, day} für ref()/prev() */
function computeRecord(mod, data, ctx) {
  const v = Object.assign({}, data);
  const proxy = new Proxy(v, { get: (o, k) => { const x = o[k]; return (x === undefined || x === null || x === "") ? 0 : (typeof x === "number" ? x : (isNaN(Number(x)) ? x : Number(x))); } });
  const ref = (m, k) => { const d = ctx && S.daily[m] && S.daily[m][ctx.day]; const x = d && d.data[k]; return typeof x === "number" ? x : null; };
  const prev = k => {
    if (!ctx) return 0; const all = S.daily[ctx.module] || {};
    let d = parseIso(ctx.day);
    for (let i = 0; i < 10; i++) { d = addDaysD(d, -1); const r = all[iso(d)]; if (r && typeof r.data[k] === "number") return r.data[k]; }
    return 0;
  };
  const addDays = (s, n) => { if (!s || typeof s !== "string" || !/^\d{4}-\d{2}-\d{2}/.test(s)) return null; return iso(addDaysD(parseIso(s.slice(0, 10)), n)); };
  for (const f of mod.fields) {
    if (f.type !== "calc") continue;
    if (f.need && f.need.some(k => v[k] === undefined || v[k] === null || v[k] === "")) { v[f.key] = null; continue; }
    const keys = usedKeys(f.calc);
    const hasRef = /ref\(/.test(f.calc);
    if (keys.length && !keys.some(k => v[k] !== undefined && v[k] !== null && v[k] !== "") && !hasRef) { v[f.key] = null; continue; }
    if (!keys.length && !hasRef && !/^\d/.test(f.calc)) { v[f.key] = null; continue; }
    if (!keys.length && !hasRef && Object.keys(data).length === 0) { v[f.key] = null; continue; }
    let r = null;
    try { r = calcFn(f.calc)(proxy, ref, prev, addDays); } catch (e) { r = null; }
    if (typeof r === "number" && !isFinite(r)) r = null;
    v[f.key] = r;
  }
  return v;
}

/* ---------------------------------------------------------------- Start */
async function boot() {
  S.cfg = await api("GET", "api/config");
  S.cfg.modules.forEach(m => S.mods[m.key] = m);
  S.user = LS.getItem("sf_user") || "";
  try { const w = await api("GET", "api/whoami"); if (w && w.user) S.user = w.user; } catch (e) { }   // Cockpit: Windows-Konto
  $("#brandYear").textContent = `${new Date().getFullYear()} · Bubikon`;
  renderUser();
  buildNav();
  $("#menuBtn").onclick = () => $("#rail").classList.toggle("open");
  $("#userChip").title = "Angemeldet als Windows-Benutzer"; if (!S.user) $("#userChip").onclick = askUser;   // Cockpit: Kuerzel = Windows-Konto
  window.addEventListener("hashchange", route);
  window.addEventListener("beforeunload", e => { if (S.dirty) { e.preventDefault(); e.returnValue = ""; } });
  const mt = SS.getItem("sf_meeting"); if (mt) S.meeting = JSON.parse(mt);
  if (!S.user) askUser();
  route();
  setInterval(poll, 30000);
  setInterval(renderMeetingBar, 1000);
}

function renderUser() { $("#userChip").innerHTML = S.user ? `<span class="av">${esc(S.user.slice(0, 2))}</span>${esc(S.user)}` : "Kürzel setzen"; }
function askUser() {
  const back = document.createElement("div"); back.className = "modal-back";
  back.innerHTML = `<form class="modal"><h2>Wer erfasst?</h2>
    <p>Dein Kürzel wird bei jeder Änderung gespeichert (z. B. <b>tda</b>, <b>gag</b>). Es bleibt in diesem Browser gespeichert.</p>
    <input class="field-in" name="u" maxlength="20" required placeholder="Kürzel" value="${esc(S.user)}" autocomplete="off">
    <div style="display:flex;gap:8px;justify-content:flex-end;margin-top:16px">
      ${S.user ? '<button type="button" class="btn" data-x>Abbrechen</button>' : ""}
      <button class="btn primary">Speichern</button></div></form>`;
  document.body.appendChild(back);
  const inp = $("input", back); inp.focus(); inp.select();
  $("form", back).onsubmit = e => { e.preventDefault(); S.user = inp.value.trim().toLowerCase(); LS.setItem("sf_user", S.user); renderUser(); back.remove(); };
  const x = $("[data-x]", back); if (x) x.onclick = () => back.remove();
}

function navLabel(it) {
  const m = /^((?:IN|F)?\d+(?:\.\d+)?\.?)\s/.exec(it.label);
  return (m ? m[1] + " " : "") + S.mods[it.module].short;
}
function buildNav() {
  const a = S.cfg.agenda;
  let h = a.map(it => `<a href="#/m/${it.module}" data-m="${it.module}" class="${it.sub ? "sub" : ""}">
      <span class="dot ${it.must ? "must" : ""}"></span><span>${esc(navLabel(it))}${it.friday ? ' <span class="muted">(Fr)</span>' : ""}</span><span class="cnt" data-cnt="${it.module}"></span></a>`).join("");
  $("#nav").innerHTML = `<a href="#/" data-m="home"><span class="dot must"></span><span><b>Shop-Floor heute</b></span></a><div class="sep">Ablauf</div>` + h;
  updateNavCounts();
}
async function updateNavCounts() {
  try {
    const st = await api("GET", "api/agenda?day=" + todayIso());
    S.agendaToday = st;
    for (const [k, v] of Object.entries(st)) {
      const el = document.querySelector(`[data-cnt="${k}"]`); if (!el) continue;
      el.textContent = v.open != null ? (v.open || "") : "";
      el.title = v.open != null ? `${v.open} offen` : "";
    }
  } catch (e) { }
}

/* ---------------------------------------------------------------- Routing */
async function route() {
  if (S.dirty && !confirm("Ungespeicherte Änderungen verwerfen?")) { return; }
  closeDrawer(true);
  $("#rail").classList.remove("open");
  const h = (location.hash.replace(/^#/, "") || "/").split("?")[0];
  const parts = h.split("/").filter(Boolean);
  S.route = parts;
  document.querySelectorAll("#nav a").forEach(a => a.classList.toggle("active",
    (parts[0] === "m" && a.dataset.m === parts[1]) || (!parts.length && a.dataset.m === "home")));
  const v = $("#view"); v.innerHTML = '<div class="empty">Lade…</div>'; window.scrollTo(0, 0);
  try {
    if (!parts.length) await viewHome();
    else if (parts[0] === "m") {
      const m = S.mods[parts[1]]; if (!m) throw new Error("Modul nicht gefunden");
      if (m.kind === "list") await viewList(m, parts[2]);
      else if (m.kind === "daily") await viewDaily(m);
      else if (m.key === "grafik") await viewGrafik();
      else if (m.key === "zd05") await viewZd05();
      else if (m.key === "forecast") await viewForecast();
    }
    else if (parts[0] === "schnittstelle") await viewSchema();
    else if (parts[0] === "mail") await viewMail();
    else v.innerHTML = '<div class="empty">Seite nicht gefunden.</div>';
  } catch (e) { v.innerHTML = `<div class="empty">Fehler: ${esc(e.message)}</div>`; }
  v.focus({ preventScroll: true });
  renderMeetingBar();
}

async function poll() {
  if (S.dirty || S.drawerOpen || document.hidden) return;
  if (document.activeElement && /INPUT|TEXTAREA|SELECT/.test(document.activeElement.tagName)) return;
  const p = S.route || [];
  try {
    if (!p.length) await viewHome(true);
    else if (p[0] === "m" && S.mods[p[1]]) {
      const m = S.mods[p[1]];
      if (m.kind === "list") { await loadRecords(m.key); renderListBody(m); }
      else if (m.kind === "daily") { await loadWeek(m); renderDailyGrid(m); }
    }
    updateNavCounts();
    setSync("aktualisiert " + new Date().toLocaleTimeString("de-CH", { hour: "2-digit", minute: "2-digit" }));
  } catch (e) { setSync("⚠ keine Verbindung zum Server"); }
}

/* ---------------------------------------------------------------- Grafik-Bausteine (2026-10-08)
   Reines Inline-SVG / CSS, keine Bibliothek, laeuft offline. Farben kommen aus style.css (Klassen .ring,
   .mb, .stack, .spark ...) und folgen damit Hell/Dunkel des Cockpits. */
/* Fortschrittsring; pct 0..100, text = Beschriftung in der Mitte, cls "ok" | "bad" */
function gxRing(pct, text, cls, size = 62) {
  const r = 26, c = 2 * Math.PI * r, d = c * Math.max(0, Math.min(100, pct)) / 100;
  return `<svg class="ring ${cls || ""}" width="${size}" height="${size}" viewBox="0 0 64 64" role="img" aria-label="${esc(text)}">
    <circle class="rt" cx="32" cy="32" r="${r}"/><circle class="ra" cx="32" cy="32" r="${r}" stroke-dasharray="${d.toFixed(1)} ${c.toFixed(1)}" transform="rotate(-90 32 32)"/>
    <text x="32" y="37" text-anchor="middle">${esc(text)}</text></svg>`;
}
/* Sparkline ueber vals (Zahl oder null); o.ref = Referenzlinie (z. B. Ziel), o.cls = "ok" | "bad" */
function gxSpark(vals, o = {}) {
  const W = o.w || 180, H = o.h || 44, p = 4;
  const pts = vals.map((v, i) => ({ v, i })).filter(q => q.v != null && isFinite(q.v));
  if (pts.length < 2) return `<div class="muted" style="font-size:11.5px;line-height:${H}px">zu wenige Werte für einen Verlauf</div>`;
  let mn = Math.min(...pts.map(q => q.v)), mx = Math.max(...pts.map(q => q.v));
  if (o.ref != null) { mn = Math.min(mn, o.ref); mx = Math.max(mx, o.ref); }
  if (mx === mn) { mx += 1; mn -= 1; }
  const x = i => p + (W - 2 * p) * i / Math.max(1, vals.length - 1), y = v => p + (H - 2 * p) * (1 - (v - mn) / (mx - mn));
  const line = pts.map(q => `${x(q.i).toFixed(1)},${y(q.v).toFixed(1)}`).join(" ");
  const a = pts[0], z = pts[pts.length - 1];
  return `<svg class="spark ${o.cls || ""}" viewBox="0 0 ${W} ${H}" role="img" aria-label="${esc(o.label || "Verlauf")}">
    ${o.ref != null ? `<line class="sp-ref" x1="${p}" x2="${W - p}" y1="${y(o.ref).toFixed(1)}" y2="${y(o.ref).toFixed(1)}"/>` : ""}
    <polygon class="sp-area" points="${x(a.i).toFixed(1)},${H - p} ${line} ${x(z.i).toFixed(1)},${H - p}"/>
    <polyline class="sp-line" points="${line}"/><circle class="sp-dot" cx="${x(z.i).toFixed(1)}" cy="${y(z.v).toFixed(1)}" r="3"/></svg>`;
}
/* Mini-Balken: items [{lab, val, od, title}], od = davon ueberfaellig (rot) */
function gxMiniBars(items) {
  const max = Math.max(1, ...items.map(i => i.val));
  return `<div class="mbars">${items.map(i => `<div class="mb" title="${esc(i.title || "")}"><span>${esc(i.lab)}</span>
    <span class="t"><i style="width:${((i.val - (i.od || 0)) / max * 100).toFixed(1)}%"></i><i class="od" style="width:${((i.od || 0) / max * 100).toFixed(1)}%"></i></span><b>${i.val}</b></div>`).join("")}</div>`;
}
/* Statusband: parts [{cls: s-open|s-od|s-done|s-new, n, lab}] mit Legende */
function gxStack(parts, legend = true) {
  const sum = parts.reduce((t, q) => t + q.n, 0);
  if (!sum) return `<div class="stack"></div>`;
  return `<div class="stack" role="img" aria-label="${esc(parts.map(q => q.n + " " + q.lab).join(", "))}">${parts.filter(q => q.n).map(q => `<i class="${q.cls}" style="flex:${q.n}" title="${q.n} ${esc(q.lab)}"></i>`).join("")}</div>` +
    (legend ? `<div class="stack-leg">${parts.map(q => `<span><i class="${q.cls}"></i><b>${q.n}</b> ${esc(q.lab)}</span>`).join("")}</div>` : "");
}

/* ---------------------------------------------------------------- Startseite */
function homeDay() { return S.homeDay || todayIso(); }
async function viewHome(silent) {
  const day = homeDay(); const d = parseIso(day);
  const isFri = d.getDay() === 5;
  const st = await api("GET", "api/agenda?day=" + day);
  S.agendaStatus = st;
  let zh = null; try { zh = await api("GET", "api/zd05/history"); } catch (e) { }   // nur fuer die Kennzahl-Sparkline, optional
  const items = S.cfg.agenda;
  let must = 0, mustDone = 0; const mustDots = [];   // mustDots: je Pflichtpunkt erledigt ja/nein (Chips unter dem Ring)
  const rows = items.map((it, i) => {
    const m = S.mods[it.module]; const s = st[it.module] || {};
    const skip = it.friday && !isFri;
    let stat = "";
    if (m.kind === "daily") {
      if (it.must && !skip) { must++; if (s.filled) mustDone++; mustDots.push({ lab: m.short, done: !!s.filled }); }
      stat = s.filled
        ? `<span class="pill ok">erfasst</span><span class="who">${esc(s.by || "")} ${s.at ? s.at.slice(11, 16) : ""}</span>`
        : (skip ? `<span class="muted">nur am Freitag</span>` : `<span class="pill ${it.must ? "bad" : ""}">noch nicht erfasst</span>`);
    } else if (m.kind === "list") {
      const bits = [];
      if (s.open != null) bits.push(`<span class="pill ${s.open ? "warn" : "ok"}">${s.open} offen</span>`);
      if (s.overdue) bits.push(`<span class="pill bad pill-link" data-go="#/m/${it.module}?f=overdue" title="Überfällige anzeigen">${s.overdue} überfällig</span>`);
      if (s.new_today) bits.push(`<span class="pill info">${s.new_today} neu</span>`);
      if (s.open == null) bits.push(`<span class="muted">${s.total} Einträge</span>`);
      stat = bits.join("");
    } else if (it.module === "zd05") {
      const z = s || {};
      if (z.imported) {
        const k = z.kpi || {}; const bad = k.kennzahl != null && k.kennzahl > 1.2;
        stat = `<span class="pill ${bad ? "bad" : "ok"}">Kennzahl ${f2(k.kennzahl)}</span>${k.code2 ? `<span class="pill warn">${k.code2} × Code 2</span>` : ""}<span class="who">${esc(z.by || "")} ${z.at ? z.at.slice(11, 16) : ""}</span>`;
      } else { if (it.must) { must++; mustDots.push({ lab: m.short, done: false }); } stat = `<span class="pill ${it.must ? "bad" : ""}">ZD05 noch nicht importiert</span>`; }
      if (z.imported && it.must) { must++; mustDone++; mustDots.push({ lab: m.short, done: true }); }
    } else stat = `<span class="muted">Übersicht</span>`;
    const cur = S.meeting && S.meeting.idx === i ? " current" : "";
    return `<li class="${it.must ? "must" : ""} ${it.sub ? "sub" : ""} ${skip ? "skip" : ""}${cur}">
      <a href="#/m/${it.module}${m.kind === "daily" ? "?day=" + day : ""}"><span class="mark"></span><span class="lbl">${esc(it.label)}</span><span class="st">${stat}</span></a></li>`;
  }).join("");
  const pct = must ? Math.round(mustDone / must * 100) : 0;
  const rsOpen = ["rs_seh", "rs_tr5", "rs_tx", "rs_dw"].reduce((t, k) => t + ((st[k] || {}).open || 0), 0);
  const overdue = Object.values(st).reduce((t, x) => t + (x.overdue || 0), 0);
  const pend = st.pendenzen || {};
  // Grafiken der Kacheln (2026-10-08): Ring, Statusband, Mini-Balken je Abteilung, Verteilung der Ueberfaelligen
  const rsDepts = [["rs_seh", "SEH"], ["rs_tr5", "TR5"], ["rs_tx", "TX"], ["rs_dw", "DW"]].map(([k, l]) => ({ lab: l, val: (st[k] || {}).open || 0, od: (st[k] || {}).overdue || 0, title: `${l}: ${(st[k] || {}).open || 0} offen, ${(st[k] || {}).overdue || 0} überfällig` }));
  const odList = Object.entries(st).filter(([k, x]) => x && x.overdue && S.mods[k]).sort((a, b) => b[1].overdue - a[1].overdue).slice(0, 4)
    .map(([k, x]) => ({ lab: S.mods[k].short, val: x.overdue, od: x.overdue, title: `${S.mods[k].title}: ${x.overdue} überfällig` }));
  const zk = zh ? (zh.days || []).filter(x => x.kpi && x.kpi.kennzahl != null).slice(-30) : [];
  const zTarget = (zh && zh.target) || 1.2;
  const zLast = zk.length ? zk[zk.length - 1].kpi.kennzahl : null;
  const html = `
  <div class="hero">
    <div class="dayline"><span class="kw">KW ${isoWeek(d)} · ${d.getFullYear()}</span><h1>${WD_LONG[d.getDay()]}, ${d.getDate()}. ${MONTHS[d.getMonth()]}</h1></div>
    <div class="dayctl">
      <button class="btn small" data-dd="-1" aria-label="Vortag">‹</button>
      <input type="date" id="homeDate" value="${day}">
      <button class="btn small" data-dd="1" aria-label="Folgetag">›</button>
      ${day !== todayIso() ? '<button class="btn small" data-dd="0">Heute</button>' : ""}
    </div>
  </div>
  <div class="kpis">
    <div class="kpi"><div class="kpi-top"><div><div class="v">${mustDone}<span class="muted" style="font-size:18px"> / ${must}</span></div><div class="l">Pflichtpunkte erfasst</div></div>
      ${gxRing(pct, pct + " %", pct >= 100 ? "ok" : "")}</div>
      <div class="dots">${mustDots.map(d => `<span class="dt ${d.done ? "ok" : ""}" title="${esc(d.lab)}: ${d.done ? "erfasst" : "noch nicht erfasst"}">${esc(d.lab)}</span>`).join("")}</div></div>
    <a class="kpi" href="#/m/pendenzen"><div><div class="v">${pend.open || 0}</div><div class="l">offene Pendenzen${pend.new_today ? ` · ${pend.new_today} neu` : ""}</div></div>
      ${gxStack([{ cls: "s-open", n: Math.max(0, (pend.open || 0) - (pend.overdue || 0)), lab: "im Termin" }, { cls: "s-od", n: pend.overdue || 0, lab: "überfällig" }])}</a>
    <a class="kpi" href="#/m/rs_tx"><div><div class="v">${rsOpen}</div><div class="l">offene Rückstände (SEH, TR5, TX, DW)</div></div>${gxMiniBars(rsDepts)}</a>
    <div class="kpi ${overdue ? "alarm" : ""}"><div><div class="v">${overdue}</div><div class="l">überfällige Punkte total</div></div>
      ${overdue ? gxMiniBars(odList) : '<span class="pill ok" style="align-self:flex-start">alles im Termin</span>'}</div>
  </div>
  <div class="home">
    <section class="panel-card">
      <header><h2>Ablauf Shop-Floor</h2><span class="muted" style="font-size:13px">Orange Punkte müssen besprochen werden · Ziel 10–20 Min.</span></header>
      <ul class="order">${rows}</ul>
    </section>
    <aside>
      <div class="side-card start">
        <h3>${S.meeting ? "Shop-Floor läuft" : "Bereit für den Shop-Floor"}</h3>
        <p>${mustDone} von ${must} Abteilungen haben ihre Werte für heute erfasst.</p>
        <div class="progress"><i style="width:${pct}%"></i></div>
        ${S.meeting ? '<button class="btn big-start" id="stopMeeting">Shop-Floor beenden</button>'
                    : '<button class="btn signal big-start" id="startMeeting">Shop-Floor starten</button>'}
      </div>
      ${zk.length > 1 ? `<a class="side-card" href="#/m/zd05" style="display:block;text-decoration:none;color:inherit">
        <h3>Einkauf ZD05: Bewertungskennzahl</h3>
        <div class="zdspark"><div class="tl"><span>letzte ${zk.length} Importtage</span><span>aktuell <b>${f2(zLast)}</b> · Ziel ≤ ${f2(zTarget)}</span></div>
        ${gxSpark(zk.map(x => x.kpi.kennzahl), { ref: zTarget, cls: zLast > zTarget ? "bad" : "ok", h: 54, label: "Verlauf Bewertungskennzahl" })}</div></a>` : ""}
      <div class="side-card">
        <h3>Teilnehmerkreis ${isFri ? "· erweitert (Freitag)" : "· Standard (Mo–Do)"}</h3>
        <p>${esc(isFri ? S.cfg.participants.friday : S.cfg.participants.standard)}</p>
      </div>
      <div class="side-card">
        <h3>So funktioniert die Erfassung</h3>
        <p>Jede Abteilung trägt ihre Werte vor dem Meeting direkt auf ihrer Seite ein. Gespeichert wird automatisch beim Verlassen eines Feldes. Felder mit <span class="src sap">SAP</span> oder <span class="src mes">MES</span> werden später automatisch befüllt.</p>
      </div>
    </aside>
  </div>`;
  if (silent && document.activeElement && document.activeElement.id === "homeDate") return;
  $("#view").innerHTML = html;
  $("#homeDate").onchange = e => { S.homeDay = e.target.value || todayIso(); viewHome(); };
  document.querySelectorAll("[data-dd]").forEach(b => b.onclick = () => {
    const n = Number(b.dataset.dd); S.homeDay = n === 0 ? todayIso() : iso(addDaysD(parseIso(homeDay()), n)); viewHome();
  });
  document.querySelectorAll("[data-go]").forEach(el => el.onclick = e => { e.preventDefault(); e.stopPropagation(); location.hash = el.dataset.go; });
  const sm = $("#startMeeting"); if (sm) sm.onclick = startMeeting;
  const xm = $("#stopMeeting"); if (xm) xm.onclick = stopMeeting;
}

/* ---------------------------------------------------------------- Moderationsmodus */
function meetingItems() {
  const isFri = parseIso(homeDay()).getDay() === 5;
  return S.cfg.agenda.map((it, i) => ({ ...it, i })).filter(it => !(it.friday && !isFri));
}
function startMeeting() {
  S.meeting = { start: Date.now(), idx: meetingItems()[0].i };
  SS.setItem("sf_meeting", JSON.stringify(S.meeting));
  location.hash = "#/m/" + S.cfg.agenda[S.meeting.idx].module;
}
function stopMeeting() {
  const min = S.meeting ? Math.round((Date.now() - S.meeting.start) / 60000) : 0;
  S.meeting = null; SS.removeItem("sf_meeting"); renderMeetingBar();
  toast(`Shop-Floor beendet nach ${min} Minuten`); location.hash = "#/";
}
function stepMeeting(dir) {
  const its = meetingItems(); let pos = its.findIndex(it => it.i === S.meeting.idx);
  pos = Math.max(0, Math.min(its.length - 1, pos + dir));
  S.meeting.idx = its[pos].i; SS.setItem("sf_meeting", JSON.stringify(S.meeting));
  location.hash = "#/m/" + its[pos].module;
}
function renderMeetingBar() {
  const bar = $("#meetingBar");
  if (!S.meeting) { bar.hidden = true; return; }
  bar.hidden = false;
  const sec = Math.floor((Date.now() - S.meeting.start) / 1000);
  const mm = String(Math.floor(sec / 60)).padStart(2, "0"), ss = String(sec % 60).padStart(2, "0");
  const cls = sec >= 1200 ? "late" : sec >= 600 ? "mid" : "";
  const its = meetingItems(); const pos = its.findIndex(it => it.i === S.meeting.idx);
  const cur = its[pos];
  const html = `<span class="timer ${cls}" title="Ziel 10–20 Minuten">${mm}:${ss}</span>
    <button class="btn small" data-ms="-1" ${pos <= 0 ? "disabled" : ""}>‹ Zurück</button>
    <span class="step">${pos + 1}/${its.length} · ${esc(cur ? cur.label : "")}</span>
    <button class="btn small primary" data-ms="1" ${pos >= its.length - 1 ? "disabled" : ""}>Weiter ›</button>
    <button class="btn small" data-ms="x">Beenden</button>`;
  if (bar.dataset.k !== `${pos}|${cls}`) {
    bar.innerHTML = html; bar.dataset.k = `${pos}|${cls}`;
    bar.querySelectorAll("[data-ms]").forEach(b => b.onclick = () => b.dataset.ms === "x" ? stopMeeting() : stepMeeting(Number(b.dataset.ms)));
  } else $(".timer", bar).textContent = `${mm}:${ss}`;
}

/* ---------------------------------------------------------------- Listen-Module */
async function loadRecords(key) { S.cache[key] = await api("GET", "api/records?module=" + key); return S.cache[key]; }
const FILTER_KEYS = ["abteilung", "prio", "verantwortlich", "eintrag", "disponent", "ursache", "von", "an", "bearbeitet"];
function listState(m) {
  if (!S.list[m.key]) S.list[m.key] = { q: "", status: m.status_field ? "open" : "all", f: {}, st: "", due: "", sort: "_id", dir: -1, limit: 150 };
  return S.list[m.key];
}
function dueOf(m, r) {
  if (!m.due_field) return null;
  const f = m.fields.find(x => x.key === m.due_field);
  const v = f && f.type === "calc" ? computeRecord(m, r.data)[m.due_field] : r.data[m.due_field];
  return typeof v === "string" && /^\d{4}-\d{2}-\d{2}/.test(v) ? v.slice(0, 10) : null;
}
const isOpenRec = (m, r) => m.status_field ? !isClosed(r.data[m.status_field], m) : true;
const isOverdue = (m, r, today) => { const d = dueOf(m, r); return isOpenRec(m, r) && d && d < today; };
async function viewList(m, openNr) {
  await loadRecords(m.key);
  const ls = listState(m);
  const qs = new URLSearchParams(location.hash.split("?")[1] || "");
  if (qs.get("f") === "overdue") { ls.status = "overdue"; ls.sort = "_due"; ls.dir = 1; }
  const recs = S.cache[m.key];
  const fkeys = FILTER_KEYS.filter(k => m.fields.some(f => f.key === k)).filter(k => new Set(recs.map(r => r.data[k]).filter(Boolean)).size > 1).slice(0, 4);
  const statusVals = m.status_field ? [...new Set(recs.map(r => String(r.data[m.status_field] || "").trim()).filter(Boolean))].sort((a, b) => a.localeCompare(b, "de")) : [];
  const opts = k => [...new Set(recs.map(r => String(r.data[k]).trim()).filter(x => x && x !== "undefined"))].sort((a, b) => a.localeCompare(b, "de", { numeric: true }));
  const lab = k => (m.fields.find(f => f.key === k) || {}).label || k;
  const dueLbl = m.due_field ? lab(m.due_field) : "";
  $("#view").innerHTML = `
    <div class="head"><div class="grow"><h1>${esc(m.title)}</h1><p class="lede" id="listCount"></p></div>
      <button class="btn" data-export="${m.key}">Export (CSV)</button>
      <button class="btn primary" id="newBtn">+ Neuer Eintrag</button></div>
    <div class="listbar" id="listBar" hidden></div>
    <div class="toolbar">
      <input class="field-in search" id="q" type="search" placeholder="Suchen (Nr., Material, Kunde, Text …)" value="${esc(ls.q)}">
      ${m.status_field ? `<div class="seg" role="group" aria-label="Status">
        <button data-st="open" aria-pressed="${ls.status === "open"}">Offen</button>
        ${m.due_field ? `<button data-st="overdue" aria-pressed="${ls.status === "overdue"}" class="seg-alarm">Überfällig <span id="odCnt"></span></button>` : ""}
        <button data-st="closed" aria-pressed="${ls.status === "closed"}">Abgeschlossen</button>
        <button data-st="all" aria-pressed="${ls.status === "all"}">Alle</button></div>` : ""}
      <button class="btn small" id="fReset" hidden>Filter zurücksetzen</button>
    </div>
    <div class="toolbar filters">
      ${m.status_field ? `<label class="fsel"><span>Status</span><select class="field-in" data-fst><option value="">alle</option>${statusVals.map(x => `<option ${x === ls.st ? "selected" : ""}>${esc(x)}</option>`).join("")}</select></label>` : ""}
      ${m.due_field ? `<label class="fsel"><span>${esc(dueLbl)}</span><select class="field-in" data-fdue>
        ${[["", "alle"], ["today", "heute fällig"], ["tom", "morgen fällig"], ["week", "diese Woche"], ["next", "nächste Woche"], ["later", "später"], ["none", "ohne Termin"]].map(([v, l]) => `<option value="${v}" ${ls.due === v ? "selected" : ""}>${l}</option>`).join("")}</select></label>` : ""}
      ${fkeys.map(k => `<label class="fsel"><span>${esc(lab(k).replace(/:$/, ""))}</span><select class="field-in" data-fk="${k}"><option value="">alle</option>${opts(k).map(x => `<option ${x === (ls.f[k] || "") ? "selected" : ""}>${esc(x)}</option>`).join("")}</select></label>`).join("")}
    </div>
    <div class="tbl-wrap"><table class="list"><thead id="lh"></thead><tbody id="lb"></tbody></table></div>
    <button class="btn more" id="moreBtn" hidden>Weitere anzeigen</button>`;
  let t; $("#q").oninput = e => { clearTimeout(t); t = setTimeout(() => { ls.q = e.target.value; ls.limit = 150; renderListBody(m); }, 180); };
  document.querySelectorAll("[data-st]").forEach(b => b.onclick = () => {
    ls.status = b.dataset.st; ls.limit = 150;
    if (ls.status === "overdue") { ls.sort = "_due"; ls.dir = 1; } else if (ls.sort === "_due") { ls.sort = "_id"; ls.dir = -1; }
    document.querySelectorAll("[data-st]").forEach(x => x.setAttribute("aria-pressed", x === b)); renderListBody(m);
  });
  document.querySelectorAll("[data-fk]").forEach(sl => sl.onchange = () => { ls.f[sl.dataset.fk] = sl.value; ls.limit = 150; renderListBody(m); });
  const fst = $("[data-fst]"); if (fst) fst.onchange = () => { ls.st = fst.value; if (fst.value) { ls.status = "all"; document.querySelectorAll("[data-st]").forEach(x => x.setAttribute("aria-pressed", x.dataset.st === "all")); } renderListBody(m); };
  const fd = $("[data-fdue]"); if (fd) fd.onchange = () => { ls.due = fd.value; renderListBody(m); };
  $("#fReset").onclick = () => { S.list[m.key] = null; history.replaceState(null, "", "#/m/" + m.key); viewList(m); };
  $("#newBtn").onclick = () => openRecord(m, null);
  $("#moreBtn").onclick = () => { ls.limit += 300; renderListBody(m); };
  renderListBody(m);
  if (openNr) { const r = S.cache[m.key].find(x => x.nr === decodeURIComponent(openNr)); if (r) openRecord(m, r); }
}
function listCols(m) { return m.fields.filter(f => f.list); }
function renderListBody(m) {
  const ls = listState(m); const cols = listCols(m); const today = todayIso();
  let rows = S.cache[m.key] || [];
  const total = rows.length; const sf = m.status_field;
  const openCount = sf ? rows.filter(r => !isClosed(r.data[sf], m)).length : null;
  const odCount = m.due_field ? rows.filter(r => isOverdue(m, r, today)).length : 0;
  const oc = $("#odCnt"); if (oc) oc.textContent = odCount ? `(${odCount})` : "";
  // Statusband ueber alle Eintraege des Moduls (2026-10-08): im Termin offen / ueberfaellig / erledigt
  const lbar = $("#listBar");
  if (lbar) {
    const done = sf ? total - openCount : 0;
    lbar.hidden = !sf || !total;
    if (sf && total) lbar.innerHTML = gxStack([{ cls: "s-open", n: openCount - odCount, lab: m.due_field ? "offen im Termin" : "offen" }, ...(m.due_field ? [{ cls: "s-od", n: odCount, lab: "überfällig" }] : []), { cls: "s-done", n: done, lab: "erledigt" }]);
  }
  if (sf && ls.status === "open") rows = rows.filter(r => !isClosed(r.data[sf], m));
  if (sf && ls.status === "closed") rows = rows.filter(r => isClosed(r.data[sf], m));
  if (ls.status === "overdue") rows = rows.filter(r => isOverdue(m, r, today));
  if (ls.st) rows = rows.filter(r => String(r.data[sf] || "").trim() === ls.st);
  Object.entries(ls.f || {}).forEach(([k, v]) => { if (v) rows = rows.filter(r => String(r.data[k] ?? "").trim() === v); });
  if (ls.due) {
    const mon = monday(new Date()); const sun = iso(addDaysD(mon, 6)), nmon = iso(addDaysD(mon, 7)), nsun = iso(addDaysD(mon, 13));
    rows = rows.filter(r => { const d = dueOf(m, r);
      if (ls.due === "none") return !d; if (!d) return false;
      if (ls.due === "today") return d === today; if (ls.due === "tom") return d === iso(addDaysD(parseIso(today), 1)); if (ls.due === "week") return d >= iso(mon) && d <= sun;
      if (ls.due === "next") return d >= nmon && d <= nsun; if (ls.due === "later") return d > nsun; return true; });
  }
  const active = ls.status !== (sf ? "open" : "all") || ls.st || ls.due || Object.values(ls.f || {}).some(Boolean) || ls.q;
  const rb = $("#fReset"); if (rb) rb.hidden = !active;
  if (ls.q) {
    const q = ls.q.toLowerCase();
    rows = rows.filter(r => (r.nr + " " + Object.values(r.data).join(" ")).toLowerCase().includes(q));
  }
  const key = ls.sort, dir = ls.dir;
  rows = rows.slice().sort((a, b) => {
    let x, y;
    if (key === "_id") { x = a.id; y = b.id; }
    else if (key === "_due") { x = dueOf(m, a); y = dueOf(m, b); }
    else if (key === "_nr") { x = a.nr; y = b.nr; return x.localeCompare(y, "de", { numeric: true }) * dir; }
    else { x = a.data[key]; y = b.data[key]; }
    if (x == null || x === "") return 1; if (y == null || y === "") return -1;
    if (typeof x === "number" && typeof y === "number") return (x - y) * dir;
    return String(x).localeCompare(String(y), "de", { numeric: true }) * dir;
  });
  $("#listCount").innerHTML = `${total} Einträge${openCount != null ? ` · ${openCount} offen` : ""}${odCount ? ` · <span style="color:var(--alarm);font-weight:600">${odCount} überfällig</span>` : ""}${rows.length !== total ? ` · ${rows.length} angezeigt` : ""}`;
  $("#lh").innerHTML = `<tr><th data-s="_nr" class="${key === "_nr" ? "sorted" + (dir > 0 ? " asc" : "") : ""}">Nr.</th>${cols.map(f => `<th data-s="${f.key}" class="${key === f.key ? "sorted" + (dir > 0 ? " asc" : "") : ""}">${esc(f.label)}</th>`).join("")}</tr>`;
  $("#lh").querySelectorAll("th").forEach(th => th.onclick = () => { if (ls.sort === th.dataset.s) ls.dir *= -1; else { ls.sort = th.dataset.s; ls.dir = 1; } renderListBody(m); });
  const shown = rows.slice(0, ls.limit);
  $("#lb").innerHTML = shown.length ? shown.map(r => {
    const v = computeRecord(m, r.data); const od = isOverdue(m, r, today);
    return `<tr data-id="${r.id}" class="${od ? "od" : ""}"><td class="nr">${esc(r.nr)}</td>${cols.map(f => cellHtml(f, v[f.key], f.key === m.due_field && isOverdue(m, r, today), today, m)).join("")}</tr>`;
  }).join("") : `<tr><td colspan="${cols.length + 1}" class="empty">${ls.q || ls.dept ? "Keine Treffer. Suche oder Filter anpassen." : "Noch keine offenen Einträge. Mit „+ Neuer Eintrag“ erfassen."}</td></tr>`;
  $("#lb").querySelectorAll("tr[data-id]").forEach(tr => tr.onclick = () => openRecord(m, S.cache[m.key].find(x => x.id === Number(tr.dataset.id))));
  $("#moreBtn").hidden = rows.length <= ls.limit;
}
function cellHtml(f, val, open, today, m) {
  if (val === undefined || val === null || val === "") return "<td></td>";
  if (f.key === "status" || f.type === "select" && /status/i.test(f.label)) return `<td><span class="pill ${statusClass(val, m)}">${esc(val)}</span></td>`;
  if (f.type === "date" || f.fmt === "date") {
    const od = open && /^\d{4}-/.test(val) && val < today;
    return `<td class="${od ? "overdue" : ""}" style="white-space:nowrap">${esc(fmtDate(val))}</td>`;
  }
  if (f.type === "num" || f.type === "calc") return `<td class="n">${esc(fmtNum(val, f.digits))}</td>`;
  if (f.type === "flags") return `<td>${esc((val || []).join(", "))}</td>`;
  return `<td><div class="clip">${esc(val)}</div></td>`;
}

/* ---------- Formular (Drawer) */
function groupFields(fields) {
  const g = []; const idx = {};
  fields.forEach(f => { const k = f.group || ""; if (!(k in idx)) { idx[k] = g.length; g.push({ name: k, fields: [] }); } g[idx[k]].fields.push(f); });
  return g;
}
function optsOf(f) { return Array.isArray(f.opts) ? f.opts : (S.cfg.lists[f.opts] || []); }
function inputHtml(f, val, id) {
  const v = val ?? "";
  if (f.type === "long") return `<textarea class="field-in" id="${id}" data-k="${f.key}">${esc(v)}</textarea>`;
  if (f.type === "date") {
    const isIso = /^\d{4}-\d{2}-\d{2}$/.test(v);
    return isIso || !v ? `<input class="field-in" type="date" id="${id}" data-k="${f.key}" value="${esc(v)}">`
      : `<input class="field-in" id="${id}" data-k="${f.key}" value="${esc(v)}" title="Kein gültiges Datum (aus Excel übernommen)">`;
  }
  if (f.type === "num") return `<input class="field-in" id="${id}" data-k="${f.key}" autocomplete="off" inputmode="decimal" value="${esc(typeof v === "number" ? fmtNum(v) : v)}" style="text-align:right">`;
  if (f.type === "time") return `<input class="field-in" id="${id}" data-k="${f.key}" value="${esc(v)}" placeholder="hh:mm">`;
  if (f.type === "select") {
    const o = optsOf(f);
    if (f.free) return `<input class="field-in" id="${id}" data-k="${f.key}" list="dl-${f.key}" value="${esc(v)}" autocomplete="off"><datalist id="dl-${f.key}">${o.map(x => `<option value="${esc(x)}">`).join("")}</datalist>`;
    return `<select class="field-in" id="${id}" data-k="${f.key}"><option value=""></option>${o.map(x => `<option ${x === v ? "selected" : ""}>${esc(x)}</option>`).join("")}${v && !o.includes(v) ? `<option selected>${esc(v)}</option>` : ""}</select>`;
  }
  if (f.type === "flags") {
    const sel = Array.isArray(val) ? val : [];
    return `<div class="flags" data-k="${f.key}" data-flags>${optsOf(f).map(x => `<label><input type="checkbox" value="${esc(x)}" ${sel.includes(x) ? "checked" : ""}>${esc(x)}</label>`).join("")}</div>`;
  }
  return `<input class="field-in" id="${id}" data-k="${f.key}" value="${esc(v)}">`;
}
function readForm(m, root) {
  const out = {};
  m.fields.forEach(f => {
    if (f.type === "calc") return;
    if (f.type === "flags") { const el = root.querySelector(`[data-k="${f.key}"][data-flags]`); out[f.key] = [...el.querySelectorAll("input:checked")].map(i => i.value); return; }
    const el = root.querySelector(`[data-k="${f.key}"]`); if (!el) return;
    let v = el.value;
    if (f.type === "num") v = parseNum(v);
    else if (f.type === "date") v = parseDateInput(v);
    else v = v.trim() === "" ? null : v;
    out[f.key] = v;
  });
  return out;
}
function srcTag(f, meta) {
  const m = meta && meta[f.key];
  if (m) return ` <span class="src ${m.src.toLowerCase() === "mes" ? "mes" : "sap"}" title="Automatisch geliefert ${esc(m.ts)}">${esc(m.src)}</span>`;
  if (f.src) return ` <span class="src ${f.src}" title="Wird später automatisch aus ${f.src.toUpperCase()} befüllt">${f.src.toUpperCase()}</span>`;
  return "";
}

async function openRecord(m, rec) {
  const isNew = !rec;
  const data = rec ? rec.data : {};
  const init = {};
  if (isNew) m.fields.forEach(f => { if (f.default === "today") init[f.key] = todayIso(); else if (f.default === "user") init[f.key] = S.user; else if (f.default) init[f.key] = f.default; });
  const vals = Object.assign({}, init, data);
  const computed = computeRecord(m, vals);
  const groups = groupFields(m.fields);
  const d = $("#drawer");
  d.innerHTML = `<div class="panel" role="document">
    <div class="panel-head"><div style="flex:1"><h2>${isNew ? "Neuer Eintrag" : esc(rec.nr)}</h2>
      <div class="meta">${esc(m.title)}${rec ? ` · zuletzt geändert ${esc(fmtDate(rec.updated_at))} ${esc((rec.updated_at || "").slice(11, 16))} von ${esc(rec.updated_by || "")}` : " · Nummer wird beim Speichern vergeben"}</div></div>
      <button class="icon-btn" data-close aria-label="Schliessen">✕</button></div>
    <form class="panel-body" id="recForm" autocomplete="off">
      ${groups.map(g => `<fieldset>${g.name ? `<legend>${esc(g.name)}</legend>` : ""}<div class="fgrid">${g.fields.map(f => {
        const id = "f_" + f.key; const wide = ["long", "flags"].includes(f.type);
        if (f.type === "calc") return `<div class="fld"><label>${esc(f.label)}</label><div class="calcval" data-calc="${f.key}">${esc(f.fmt === "date" ? fmtDate(computed[f.key]) : fmtNum(computed[f.key], f.digits))}</div></div>`;
        return `<div class="fld ${wide ? "wide" : ""}"><label for="${id}">${esc(f.label)}${f.required ? ' <span class="req">*</span>' : ""}${f.unit ? ` <span class="muted">[${esc(f.unit)}]</span>` : ""}${srcTag(f, rec && rec.meta)}</label>${inputHtml(f, vals[f.key], id)}</div>`;
      }).join("")}</div></fieldset>`).join("")}
      ${(S.cfg.notify_rules || {})[m.key] && (S.cfg.notify_rules[m.key].notify || []).length ? `<div class="notify-box" id="ntBox"></div>` : ""}
      ${rec ? `<details class="hist" id="histBox"><summary>Änderungsverlauf</summary><div>Lade…</div></details>` : ""}
    </form>
    <div class="panel-foot">
      <button class="btn primary" id="saveBtn">${isNew ? "Eintrag anlegen" : "Änderungen speichern"}</button>
      <button class="btn" data-close>Abbrechen</button>
      <span style="flex:1"></span>
      ${rec ? '<button class="btn danger" id="delBtn">Löschen</button>' : ""}
    </div></div>`;
  d.hidden = false; S.drawerOpen = true; S.dirty = false;
  const form = $("#recForm");
  form.addEventListener("input", () => {
    S.dirty = true;
    const v = computeRecord(m, readForm(m, form));
    form.querySelectorAll("[data-calc]").forEach(el => { const f = m.fields.find(x => x.key === el.dataset.calc); el.textContent = f.fmt === "date" ? fmtDate(v[f.key]) : fmtNum(v[f.key], f.digits); });
  });
  form.onsubmit = e => { e.preventDefault(); save(); };
  $("#saveBtn").onclick = save;
  const nt = $("#ntBox");
  const renderNt = () => {
    if (!nt) return;
    const vals = readForm(m, form); const rule = S.cfg.notify_rules[m.key];
    const changed = isNew || rule.notify.some(f => JSON.stringify(vals[f] ?? null) !== JSON.stringify((rec.data || {})[f] ?? null));
    const { hits, missing } = resolveAll(rule.notify.map(f => vals[f]));
    const keep = nt.querySelector("#ntOn"); const on = keep ? keep.checked : true;
    nt.innerHTML = `<div class="nt-head"><b>Zuständige per Mail informieren</b>
      <label class="switch"><input type="checkbox" id="ntOn" ${on ? "checked" : ""}><span></span></label></div>
      <div class="nt-to">${hits.length ? hits.map(p => `<span class="chip ${p.email ? "" : "noaddr"}" title="${esc(p.email || "Keine E-Mail im Verzeichnis")}">${esc(p.name)}${p.email ? "" : " ⚠"}</span>`).join("") : '<span class="muted">Noch niemand zuständig eingetragen</span>'}
      ${missing.length ? `<span class="muted"> · nicht im Verzeichnis: ${missing.map(esc).join(", ")} – <a href="#/mail">ergänzen</a></span>` : ""}</div>
      <div class="nt-foot muted">${changed ? "Beim Speichern wird eine Mail an die Zuständigen gesendet." : "Zuständigkeit unverändert – es wird keine Mail gesendet."}
      ${rec && hits.some(p => p.email) ? ' <button type="button" class="btn small" id="ntResend">Jetzt erneut senden</button>' : ""}
      ${hits.some(p => p.email) ? ` <a class="btn small" href="${esc(mailtoFor(m, Object.assign({ nr: rec ? rec.nr : "(neu)" }, { data: vals }), hits))}">In Outlook öffnen</a>` : ""}</div>`;
    const rs = $("#ntResend"); if (rs) rs.onclick = async () => { try { const r = await api("POST", "api/mail/send", { id: rec.id }); toast(`Mail an ${r.to.map(x => x.name).join(", ")} ${r.status === "wartend" ? "wird gesendet" : "– " + r.status}`); } catch (e) { toast(e.message, true); } };
  };
  if (nt) { await ensurePeople(); renderNt(); form.addEventListener("change", renderNt); }
  d.querySelectorAll("[data-close]").forEach(b => b.onclick = () => closeDrawer());
  d.onclick = e => { if (e.target === d) closeDrawer(); };
  d.onkeydown = e => { if (e.key === "Escape") closeDrawer(); if ((e.ctrlKey || e.metaKey) && e.key === "s") { e.preventDefault(); save(); } };
  const first = form.querySelector("input,textarea,select"); if (first && isNew) first.focus();
  if (rec) {
    $("#histBox").ontoggle = async e => {
      if (!e.target.open) return;
      const h = await api("GET", `api/history?module=${m.key}&ref=${encodeURIComponent(rec.nr)}`);
      e.target.querySelector("div").innerHTML = histHtml(m, h);
    };
    $("#delBtn").onclick = async () => {
      if (!confirm(`${rec.nr} wirklich löschen? (bleibt in der Datenbank als gelöscht markiert)`)) return;
      await api("DELETE", "api/records/" + rec.id); S.dirty = false; closeDrawer(true); toast(`${rec.nr} gelöscht`);
      await loadRecords(m.key); renderListBody(m); updateNavCounts();
    };
  }
  async function save() {
    const vals = readForm(m, form);
    const miss = m.fields.filter(f => f.required && (vals[f.key] == null || vals[f.key] === ""));
    if (miss.length) { toast("Bitte ausfüllen: " + miss.map(f => f.label).join(", "), true); form.querySelector(`[data-k="${miss[0].key}"]`).focus(); return; }
    let changes = vals;
    if (rec) { changes = {}; for (const [k, v] of Object.entries(vals)) if (JSON.stringify(v ?? null) !== JSON.stringify(rec.data[k] ?? null) && !(v == null && rec.data[k] === undefined) && !(Array.isArray(v) && !v.length && rec.data[k] === undefined)) changes[k] = v; }
    else { for (const k of Object.keys(changes)) if (changes[k] == null || (Array.isArray(changes[k]) && !changes[k].length)) delete changes[k]; }
    // Status "erledigt" -> Abschlussdatum automatisch setzen
    if (m.status_field && isClosed(vals[m.status_field], m) && m.fields.some(f => f.key === "abschluss") && !vals.abschluss) changes.abschluss = todayIso();
    if (m.fields.some(f => f.key === "letzte_aenderung") && rec && Object.keys(changes).length && !("letzte_aenderung" in changes)) changes.letzte_aenderung = todayIso();
    $("#saveBtn").disabled = true;
    try {
      const ntOn = $("#ntOn");
      const r = await api("POST", "api/records", { module: m.key, id: rec ? rec.id : null, changes, notify: ntOn ? ntOn.checked : true });
      S.dirty = false; closeDrawer(true);
      const mails = (r.mails || []).flatMap(x => x.to.map(t => t.name));
      toast((isNew ? `${r.nr} angelegt` : `${r.nr} gespeichert`) + (mails.length ? ` · Mail an ${mails.join(", ")}` : ""));
      await loadRecords(m.key); renderListBody(m); updateNavCounts();
    } catch (e) { toast("Speichern fehlgeschlagen: " + e.message, true); $("#saveBtn").disabled = false; }
  }
}
function histHtml(m, h) {
  if (!h.length) return '<p class="muted">Keine Änderungen.</p>';
  const lab = k => (m.fields.find(f => f.key === k) || { label: k }).label;
  const show = v => Array.isArray(v) ? v.join(", ") : (v == null || v === "" ? "–" : (typeof v === "string" && /^\d{4}-\d{2}-\d{2}$/.test(v) ? fmtDate(v) : v));
  return `<ul>${h.map(x => `<li><span class="when">${esc(fmtDate(x.ts))} ${esc(x.ts.slice(11, 16))} · ${esc(x.user)}${x.source && x.source !== "manuell" ? ` (${esc(x.source)})` : ""}</span><br>
    ${Object.entries(x.changes).map(([k, [a, b]]) => `${esc(lab(k))}: <span class="muted">${esc(show(a))}</span> → <b>${esc(show(b))}</b>`).join("<br>")}</li>`).join("")}</ul>`;
}
function closeDrawer(force) {
  if (!force && S.dirty && !confirm("Ungespeicherte Änderungen verwerfen?")) return;
  const d = $("#drawer"); d.hidden = true; d.innerHTML = ""; S.drawerOpen = false; S.dirty = false;
}

/* ---------------------------------------------------------------- Tageswerte */
function refModules(m) {
  const set = new Set([m.key]);
  m.fields.forEach(f => { if (f.calc) for (const x of f.calc.matchAll(/ref\('(\w+)'/g)) set.add(x[1]); });
  return [...set];
}
async function loadWeek(m) {
  const from = iso(addDaysD(S.week, -10)), to = iso(addDaysD(S.week, 6));
  const r = await api("GET", `api/daily?module=${refModules(m).join(",")}&from=${from}&to=${to}`);
  Object.assign(S.daily, r);
}
async function loadRange(keys, from, to) { return api("GET", `api/daily?module=${keys.join(",")}&from=${from}&to=${to}`); }

async function viewDaily(m) {
  const q = new URLSearchParams(location.hash.split("?")[1] || "");
  const focusDay = q.get("day") || todayIso();
  S.week = monday(parseIso(focusDay));
  S.activeDay = focusDay;
  await loadWeek(m);
  const days = [...Array(7)].map((_, i) => addDaysD(S.week, i));
  const kw = isoWeek(S.week);
  $("#view").innerHTML = `
    <div class="head"><div class="grow"><h1>${esc(m.title)}</h1>
      <p class="lede">Werte direkt in die Tabelle eintragen – gespeichert wird beim Verlassen des Feldes. Enter springt zum nächsten Feld.</p></div>
      <button class="btn" data-export="${m.key}">Export (CSV)</button></div>
    <div id="trendBox"></div>
    <div id="chartBox"></div>
    <div class="toolbar">
      <div class="weeknav"><button class="btn small" data-w="-7" aria-label="Vorwoche">‹</button>
        <span class="wk">KW ${kw} · ${fmtDate(iso(days[0])).slice(0, 6)}–${fmtDate(iso(days[6]))}</span>
        <button class="btn small" data-w="7" aria-label="Folgewoche">›</button>
        <button class="btn small" data-w="0">Diese Woche</button></div>
      <div class="weeknav daypick"><button class="btn small" data-d="-1">‹ Tag</button><b id="dayLbl"></b><button class="btn small" data-d="1">Tag ›</button></div>
      ${m.copy_prev ? '<button class="btn small" id="copyPrev" title="Leere Felder des gewählten Tages mit der letzten Erfassung füllen">Wie letzte Erfassung ausfüllen</button>' : ""}
      <span class="muted" style="font-size:13px"><span class="src sap">SAP</span> <span class="src mes">MES</span> = wird später automatisch geliefert</span>
    </div>
    <div class="grid-wrap" id="gridBox"></div>`;
  document.querySelectorAll("[data-w]").forEach(b => b.onclick = async () => {
    const n = Number(b.dataset.w); S.week = n === 0 ? monday(new Date()) : addDaysD(S.week, n);
    S.activeDay = n === 0 ? todayIso() : iso(addDaysD(parseIso(S.activeDay), n));
    history.replaceState(null, "", "#/m/" + m.key); await viewDailyRefresh(m);
  });
  document.querySelectorAll("[data-d]").forEach(b => b.onclick = async () => {
    const nd = addDaysD(parseIso(S.activeDay), Number(b.dataset.d)); S.activeDay = iso(nd);
    if (monday(nd).getTime() !== S.week.getTime()) { S.week = monday(nd); await viewDailyRefresh(m); } else renderDailyGrid(m);
  });
  const cp = $("#copyPrev"); if (cp) cp.onclick = () => copyPrev(m);
  renderDailyGrid(m);
  renderDailyTrend(m);
  renderDailyChart(m);
}
/* Verlaufs-Kacheln (2026-10-08): bis zu 4 Hauptkennzahlen der Abteilung als Sparkline ueber die letzten ~20
   Erfassungen. Auswahl: zuerst das Balkenfeld des bestehenden Diagramms, dann die uebrigen Zahlen- und
   Rechenfelder in Reihenfolge der Konfiguration, ohne Personalfelder. Eigene Abfrage api/daily (letzte 45 Tage). */
async function renderDailyTrend(m) {
  const box = $("#trendBox"); if (!box) return;
  try {
    const to = todayIso(), from = iso(addDaysD(new Date(), -45));
    const r = await loadRange(refModules(m), from, to);
    const own = r[m.key] || {};
    const days = Object.keys(own).sort().filter(d => Object.values(own[d].data || {}).some(v => typeof v === "number")).slice(-20);
    if (days.length < 3) { box.innerHTML = ""; return; }
    const save = S.daily; S.daily = Object.assign({}, S.daily, r);
    const rows = days.map(d => computeRecord(m, own[d].data || {}, { module: m.key, day: d }));
    S.daily = save;
    const cand = m.fields.filter(f => (f.type === "num" || f.type === "calc") && !/personal/i.test(f.group || "") && f.key !== "frei" && f.key !== "krank");
    if (m.chart && m.chart.bar) cand.sort((a, b) => (b.key === m.chart.bar) - (a.key === m.chart.bar));
    const tiles = [];
    for (const f of cand) {
      const vals = rows.map(v => num(v[f.key]));
      if (vals.filter(x => x != null).length < 5) continue;
      const last = vals.filter(x => x != null).slice(-2);
      const cur = last[last.length - 1], prev = last.length > 1 ? last[0] : null;
      const dlt = prev != null ? cur - prev : null;
      tiles.push(`<div class="tt"><div class="tl" title="${esc(f.label)}">${esc(f.label)}</div>
        <div class="tv"><b>${fmtNum(cur, f.digits)}</b><small class="${dlt > 0 ? "up" : dlt < 0 ? "dn" : ""}">${f.unit ? esc(f.unit) : ""}${dlt != null && dlt !== 0 ? ` ${dlt > 0 ? "▲" : "▼"} ${fmtNum(Math.abs(dlt), f.digits)}` : ""}</small></div>
        ${gxSpark(vals, { h: 38, label: f.label })}
        <div class="tf"><span>${fmtDate(days[0]).slice(0, 6)}</span><span>letzte ${days.length} Erfassungen</span><span>${fmtDate(days[days.length - 1]).slice(0, 6)}</span></div></div>`);
      if (tiles.length >= 4) break;
    }
    box.innerHTML = tiles.length ? `<div class="trend">${tiles.join("")}</div>` : "";
  } catch (e) { box.innerHTML = ""; }
}
async function copyPrev(m) {
  const day = S.activeDay || todayIso();
  const r = await api("GET", `api/daily?module=${m.key}&from=${iso(addDaysD(parseIso(day), -21))}&to=${iso(addDaysD(parseIso(day), -1))}`);
  const days = Object.keys(r[m.key] || {}).sort(); if (!days.length) { toast("Keine frühere Erfassung gefunden", true); return; }
  const last = r[m.key][days[days.length - 1]].data;
  const cur = ((S.daily[m.key] || {})[day] || { data: {} }).data;
  const changes = {}; for (const [k, v] of Object.entries(last)) if (cur[k] === undefined) changes[k] = v;
  if (!Object.keys(changes).length) { toast("Alle Felder sind bereits ausgefüllt"); return; }
  const res = await api("POST", "api/daily", { module: m.key, day, changes });
  S.daily[m.key][day] = { data: res.data, meta: res.meta, updated_by: res.updated_by, updated_at: res.updated_at };
  renderDailyGrid(m); toast(`${Object.keys(changes).length} Felder für ${fmtDate(day)} übernommen (Stand ${fmtDate(days[days.length - 1])})`);
}
async function viewDailyRefresh(m) {
  await loadWeek(m);
  const days = [...Array(7)].map((_, i) => addDaysD(S.week, i));
  $(".weeknav .wk").textContent = `KW ${isoWeek(S.week)} · ${fmtDate(iso(days[0])).slice(0, 6)}–${fmtDate(iso(days[6]))}`;
  renderDailyGrid(m);
}

function renderDailyGrid(m) {
  const narrow = window.matchMedia("(max-width:760px)").matches;
  const today = todayIso();
  let days = [...Array(7)].map((_, i) => iso(addDaysD(S.week, i)));
  if (narrow) days = [S.activeDay];
  const dl = $("#dayLbl"); if (dl) { const d = parseIso(S.activeDay); dl.textContent = `${WD[d.getDay()]} ${fmtDate(S.activeDay).slice(0, 6)}`; }
  const recs = S.daily[m.key] || {};
  const comp = {}; days.forEach(d => comp[d] = computeRecord(m, (recs[d] || {}).data || {}, { module: m.key, day: d }));
  const groups = groupFields(m.fields);
  const head = `<colgroup><col class="lab">${days.map(d => { const wd = parseIso(d).getDay(); return `<col class="${wd === 0 || wd === 6 ? "we" : "day"}">`; }).join("")}</colgroup>
    <thead><tr><th>Kennzahl</th>${days.map(d => { const x = parseIso(d); const r = recs[d];
      return `<th class="${d === today ? "today" : ""}" title="${r ? `zuletzt ${esc(r.updated_by || "")} ${esc((r.updated_at || "").slice(11, 16))}` : "noch nichts erfasst"}">${WD[x.getDay()]} ${fmtDate(d).slice(0, 6)}<small>${r ? esc(r.updated_by || "") : "–"}</small></th>`; }).join("")}</tr></thead>`;
  let body = "";
  for (const g of groups) {
    if (g.name) body += `<tr class="grp"><th colspan="${days.length + 1}">${esc(g.name)}</th></tr>`;
    for (const f of g.fields) {
      body += `<tr class="${f.type === "calc" ? "calc" : ""}"${f.type === "calc" ? ` data-ck="${f.key}"` : ""}><th class="lab">${esc(f.label)}${f.unit ? `<span class="u">${esc(f.unit)}</span>` : ""} ${f.src ? `<span class="src ${f.src}">${f.src.toUpperCase()}</span>` : ""}</th>`;
      for (const d of days) {
        const wd = parseIso(d).getDay(); const cls = [d === today ? "today" : "", wd === 0 || wd === 6 ? "we" : ""];
        const r = recs[d]; const meta = r && r.meta && r.meta[f.key];
        if (meta) cls.push("ext", meta.src.toLowerCase() === "mes" ? "mesv" : "");
        if (f.type === "calc") {
          const v = comp[d][f.key];
          let c = ""; if (f.signed && typeof v === "number") c = v < 0 ? "neg" : v > 0 ? "pos" : "";
          if (f.limit && typeof v === "number" && v > f.limit) c = "over";
          body += `<td class="${cls.join(" ")}"><div class="out ${c}">${esc(f.fmt === "date" ? fmtDate(v) : fmtNum(v, f.digits))}</div></td>`;
        } else {
          const v = r ? r.data[f.key] : undefined;
          const over = f.limit && typeof v === "number" && v > f.limit ? " over" : "";
          const t = meta ? ` title="Geliefert von ${esc(meta.src)} am ${esc(fmtDate(meta.ts))} ${esc(meta.ts.slice(11, 16))}"` : "";
          body += `<td class="${cls.join(" ")}"${t}>${gridInput(f, v, d, over)}</td>`;
        }
      }
      body += "</tr>";
    }
  }
  const lists = m.fields.filter(f => f.type === "select" && f.free).map(f => `<datalist id="gdl-${f.key}">${optsOf(f).map(x => `<option value="${esc(x)}">`).join("")}</datalist>`).join("");
  $("#gridBox").innerHTML = `<table class="grid">${head}<tbody>${body}</tbody></table>${lists}`;
  const box = $("#gridBox");
  box.querySelectorAll("[data-k]").forEach(el => {
    el.addEventListener("change", () => saveCell(m, el));
    el.addEventListener("keydown", e => {
      if (e.key === "Enter" && el.tagName !== "TEXTAREA") { e.preventDefault(); moveFocus(box, el, 1); }
      if (e.key === "Enter" && el.tagName === "TEXTAREA" && e.ctrlKey) { e.preventDefault(); moveFocus(box, el, 1); }
    });
  });
}
function gridInput(f, v, day, over) {
  const a = `data-k="${f.key}" data-day="${day}" autocomplete="off" aria-label="${esc(f.label)} ${fmtDate(day)}"`;
  const val = v ?? "";
  if (f.type === "num") return `<input ${a} inputmode="decimal" class="${over}" value="${esc(typeof val === "number" ? fmtNum(val) : val)}">`;
  if (f.type === "long") return `<textarea ${a} rows="1">${esc(val)}</textarea>`;
  if (f.type === "select") {
    const o = optsOf(f);
    if (f.free) return `<input ${a} class="t" list="gdl-${f.key}" value="${esc(val)}" autocomplete="off">`;
    return `<select ${a}><option value=""></option>${o.map(x => `<option ${x === val ? "selected" : ""}>${esc(x)}</option>`).join("")}</select>`;
  }
  return `<input ${a} class="t" value="${esc(val)}">`;
}
function moveFocus(box, el, dir) {
  const all = [...box.querySelectorAll(`[data-day="${el.dataset.day}"]`)];
  const i = all.indexOf(el); const n = all[i + dir]; if (n) { n.focus(); if (n.select) n.select(); }
}
async function saveCell(m, el) {
  const f = m.fields.find(x => x.key === el.dataset.k); const day = el.dataset.day;
  let v = el.value;
  if (f.type === "num") v = parseNum(v); else v = v.trim() === "" ? null : v;
  const cur = S.daily[m.key][day] && S.daily[m.key][day].data[f.key];
  if ((cur ?? null) === v) return;
  const td = el.closest("td"); td.classList.remove("saved", "err");
  try {
    const r = await api("POST", "api/daily", { module: m.key, day, changes: { [f.key]: v } });
    S.daily[m.key][day] = { data: r.data, meta: r.meta, updated_by: r.updated_by, updated_at: r.updated_at };
    if (f.type === "num" && typeof v === "number") el.value = fmtNum(v);
    td.classList.add("saved"); setTimeout(() => td.classList.remove("saved"), 1500);
    // berechnete Felder dieser Spalte nachführen
    const comp = computeRecord(m, r.data, { module: m.key, day });
    const col = [...el.closest("tr").children].indexOf(td);
    el.closest("tbody").querySelectorAll("tr.calc").forEach(tr => {
      const key = tr.dataset.ck;
      const ff = m.fields.find(x => x.key === key); const out = tr.children[col] && tr.children[col].querySelector(".out");
      if (out) out.textContent = ff.fmt === "date" ? fmtDate(comp[key]) : fmtNum(comp[key], ff.digits);
    });
    setSync("gespeichert " + new Date().toLocaleTimeString("de-CH", { hour: "2-digit", minute: "2-digit" }));
  } catch (e) { td.classList.add("err"); toast("Nicht gespeichert: " + e.message, true); }
}

async function renderDailyChart(m) {
  const c = m.chart; const box = $("#chartBox"); if (!c || !box) return;
  const to = todayIso(), from = iso(addDaysD(new Date(), -55));
  const mods = refModules(m);
  const r = await loadRange(mods, from, to);
  const pts = [];
  for (let d = parseIso(from); iso(d) <= to; d = addDaysD(d, 1)) {
    const k = iso(d); const wd = d.getDay(); if (wd === 0 || wd === 6) continue;
    const save = S.daily; S.daily = Object.assign({}, S.daily, r);
    const v = computeRecord(m, (r[m.key][k] || {}).data || {}, { module: m.key, day: k }); S.daily = save;
    pts.push({ day: k, bar: num(v[c.bar]), line: c.line ? num(v[c.line]) : null });
  }
  box.innerHTML = `<div class="chart"><h3>${esc(c.label)} – letzte 8 Wochen (Arbeitstage)</h3>
    <div class="legend"><span><i style="background:var(--brand)"></i>${esc((m.fields.find(f => f.key === c.bar) || {}).label || c.bar)}</span>
    ${c.line ? `<span><i style="background:var(--ink)"></i>${esc((m.fields.find(f => f.key === c.line) || {}).label || c.line)}</span>` : ""}</div>
    ${barLineSvg(pts)}</div>`;
}
const num = x => (typeof x === "number" && isFinite(x)) ? x : null;
function barLineSvg(pts, h = 170) {
  const narrow = window.matchMedia("(max-width:760px)").matches;
  const W = narrow ? 420 : 1000, H = narrow ? 200 : h, pl = 54, pb = 22, pt = 8;
  const vals = pts.flatMap(p => [p.bar, p.line]).filter(x => x != null);
  if (!vals.length) return '<p class="muted">Noch keine Daten im Zeitraum.</p>';
  const max = Math.max(...vals, 0) * 1.08 || 1, min = Math.min(0, ...vals);
  const y = v => pt + (H - pt - pb) * (1 - (v - min) / (max - min));
  const bw = (W - pl) / pts.length;
  let s = `<svg viewBox="0 0 ${W} ${H}" role="img" aria-label="Diagramm">`;
  for (let i = 0; i <= 4; i++) { const v = min + (max - min) * i / 4; s += `<line x1="${pl}" x2="${W}" y1="${y(v)}" y2="${y(v)}" class="gx-grid"/><text x="${pl - 6}" y="${y(v) + 4}" text-anchor="end" font-size="11" class="gx-ax">${fmtNum(Math.round(v))}</text>`; }
  pts.forEach((p, i) => {
    const x = pl + i * bw;
    if (p.bar != null) s += `<rect x="${x + bw * .15}" y="${Math.min(y(p.bar), y(0))}" width="${bw * .7}" height="${Math.abs(y(0) - y(p.bar))}" class="gx-bar" rx="2"><title>${fmtDate(p.day)}: ${fmtNum(p.bar)}</title></rect>`;
    if (parseIso(p.day).getDay() === 1) s += `<text x="${x + 2}" y="${H - 6}" font-size="11" class="gx-ax">KW${isoWeek(parseIso(p.day))}</text>`;
  });
  const lp = pts.map((p, i) => p.line != null ? `${pl + i * bw + bw / 2},${y(p.line)}` : null).filter(Boolean);
  if (lp.length) s += `<polyline points="${lp.join(" ")}" class="gx-ln"/>`;
  return s + "</svg>";
}
function multiLineSvg(series, days, h = 220, W = 1000) {
  const H = h, pl = 64, pb = 22, pt = 8;
  const vals = series.flatMap(s => s.values).filter(x => x != null);
  if (!vals.length) return '<p class="muted">Noch keine Daten.</p>';
  const max = Math.max(...vals) * 1.05, min = Math.min(...vals) * 0.95;
  const y = v => pt + (H - pt - pb) * (1 - (v - min) / (max - min || 1));
  const x = i => pl + (W - pl - 10) * i / Math.max(1, days.length - 1);
  let s = `<svg viewBox="0 0 ${W} ${H}" role="img">`;
  for (let i = 0; i <= 4; i++) { const v = min + (max - min) * i / 4; s += `<line x1="${pl}" x2="${W}" y1="${y(v)}" y2="${y(v)}" class="gx-grid"/><text x="${pl - 6}" y="${y(v) + 4}" text-anchor="end" font-size="11" class="gx-ax">${fmtNum(Math.round(v))}</text>`; }
  days.forEach((d, i) => { if (i % 4 === 0) s += `<text x="${x(i)}" y="${H - 6}" font-size="11" class="gx-ax" text-anchor="middle">${esc(d)}</text>`; });
  series.forEach(se => {
    const p = se.values.map((v, i) => v != null ? `${x(i)},${y(v)}` : null).filter(Boolean);
    s += `<polyline points="${p.join(" ")}" class="gx-ln" style="stroke:${se.color}"><title>${esc(se.name)}</title></polyline>`;
  });
  return s + "</svg>";
}

/* ---------------------------------------------------------------- 7.1 PPA-Grafik */
async function viewGrafik() {
  const depts = [["seh", "SEH", "var(--c1)"], ["tr5", "TR5", "var(--c2)"], ["tx", "TX", "var(--c3)"], ["dw", "DW", "var(--c4)"], ["cz", "CZ", "var(--c5)"]];
  const from = iso(addDaysD(new Date(), -7 * 26)), to = todayIso();
  const r = await loadRange(depts.map(d => d[0]), from, to);
  // Wochenwerte: letzter erfasster Auftragsvorrat und Summe produziert
  const weeks = []; for (let d = monday(parseIso(from)); iso(d) <= to; d = addDaysD(d, 7)) weeks.push(d);
  const lbl = weeks.map(w => "KW" + isoWeek(w));
  const series = (key, field, agg) => weeks.map(w => {
    let last = null, sum = null;
    for (let i = 0; i < 7; i++) { const k = iso(addDaysD(w, i)); const rec = r[key][k]; if (!rec) continue;
      const v = computeRecord(S.mods[key], rec.data, { module: key, day: k })[field];
      if (typeof v === "number") { last = v; sum = (sum || 0) + v; } }
    return agg === "sum" ? sum : last;
  });
  const vor = depts.map(([k, n, c]) => ({ name: n, color: c, values: series(k, "vorrat_total", "last") }));
  const prod = depts.filter(d => d[0] !== "cz").map(([k, n, c]) => ({ name: n, color: c, values: series(k, "prod", "sum") }));
  const legend = list => `<div class="legend">${list.map(s => `<span><i style="background:${s.color}"></i>${s.name}</span>`).join("")}</div>`;
  $("#view").innerHTML = `<div class="head"><div class="grow"><h1>PPA-Grafik / Übersicht der Auslastung</h1>
    <p class="lede">Wird automatisch aus den Tageswerten der Abteilungen berechnet – letzte 26 Wochen.</p></div></div>
    <div class="trend">${vor.map(s => { const vv = s.values.filter(x => x != null); const cur = vv.length ? vv[vv.length - 1] : null; const pv = vv.length > 1 ? vv[vv.length - 2] : null;
      return `<div class="tt"><div class="tl">Auftragsvorrat ${s.name}</div><div class="tv"><b style="color:${s.color}">${cur != null ? fmtNum(Math.round(cur)) : "–"}</b><small>${cur != null && pv != null && cur !== pv ? (cur > pv ? "▲ " : "▼ ") + fmtNum(Math.abs(Math.round(cur - pv))) : ""}</small></div>
      <div style="--brand:${s.color};--brand-soft:color-mix(in srgb,${s.color} 14%,transparent)">${gxSpark(s.values, { h: 36, label: "Auftragsvorrat " + s.name })}</div><div class="tf"><span>${lbl[0]}</span><span>Stand Ende Woche</span><span>${lbl[lbl.length - 1]}</span></div></div>`; }).join("")}</div>
    <div class="chart"><h3>Auftragsvorrat total pro Abteilung (Stand Ende Woche)</h3>${legend(vor.filter(s => s.name !== "CZ"))}${multiLineSvg(vor.filter(s => s.name !== "CZ"), lbl)}</div>
    <div class="charts2">
      <div class="chart"><h3>Produzierte Wochenmenge (abgeschlossene Wochen)</h3>${legend(prod)}${multiLineSvg(prod.map(s => ({ ...s, values: s.values.slice(0, -1) })), lbl.slice(0, -1), 300, 560)}</div>
      <div class="chart"><h3>Auftragsvorrat CZ</h3>${legend(vor.filter(s => s.name === "CZ"))}${multiLineSvg(vor.filter(s => s.name === "CZ"), lbl, 300, 560)}</div>
    </div>`;
}

/* ---------------------------------------------------------------- Schnittstelle */
async function viewSchema() {
  const sc = await api("GET", "api/integration/schema");
  const rows = sc.flatMap(m => m.fields.filter(f => f.planned_source !== "manuell").map(f => ({ m, f })));
  $("#view").innerHTML = `<div class="head"><div class="grow"><h1>Schnittstelle SAP / MES</h1>
    <p class="lede">Diese ${rows.length} Felder sind für die automatische Befüllung vorgesehen. Bis die Anbindung steht, werden sie von Hand erfasst. Geliefert wird über <code>POST /api/integration/push</code> (Details in <code>connectors/README.md</code>).</p></div></div>
    <div class="tbl-wrap"><table class="schema"><thead><tr><th>Quelle</th><th>Modul</th><th>Feld</th><th>Technischer Name</th><th>Einheit</th></tr></thead>
    <tbody>${rows.map(({ m, f }) => `<tr><td><span class="src ${f.planned_source}">${f.planned_source.toUpperCase()}</span></td><td>${esc(m.title)}</td><td>${esc(f.label)}</td><td><code>${esc(m.module)}.${esc(f.key)}</code></td><td>${esc(f.unit || "")}</td></tr>`).join("")}</tbody></table></div>`;
}

document.addEventListener("click", e => {
  const b = e.target.closest("[data-export]"); if (!b) return;
  if (window.SF_DEMO) window.SF_DEMO.exportCsv(b.dataset.export);
  else location.href = "api/export.csv?module=" + b.dataset.export;
});
window.addEventListener("scroll", () => $(".top").classList.toggle("scrolled", window.scrollY > 4), { passive: true });
window.addEventListener("resize", () => { const p = S.route || []; if (p[0] === "m" && S.mods[p[1]] && S.mods[p[1]].kind === "daily" && !(document.activeElement && document.activeElement.closest && document.activeElement.closest("#gridBox"))) renderDailyGrid(S.mods[p[1]]); });
boot().catch(e => { $("#view").innerHTML = `<div class="empty">${window.SF_DEMO ? "Demo konnte nicht gestartet werden" : "Server nicht erreichbar"}: ${esc(e.message)}</div>`; });

/* ================================================================ 10. Einkauf – Fehlteile ZD05 */
const ZD = { data: null, hist: null, filter: { q: "", only: "all", lt: "", ltDate: "" }, visible: [] };
// Liefertermin-Filter: Bezugstag = heute, ausser man schaut einen älteren Stand an
function zdRef() { const d = ZD.data; return d.day === d.days[0] ? todayIso() : d.day; }
const ZD_LT = [["over", "Überfällig"], ["today", "Heute"], ["tom", "Morgen"], ["next", "Nächster Arbeitstag"], ["week", "Diese Woche"], ["nweek", "Nächste Woche"], ["none", "Ohne Datum"]];
function zdLtMatch(r, mode, ref) {
  const lt = r.liefertermin; const d = /^\d{4}-\d{2}-\d{2}$/.test(lt || "") ? lt : null;
  if (mode === "none") return !d; if (!d) return false;
  const R = parseIso(ref); const tom = iso(addDaysD(R, 1));
  let nwd = addDaysD(R, 1); while (nwd.getDay() === 0 || nwd.getDay() === 6) nwd = addDaysD(nwd, 1);
  const mon = monday(R), sun = iso(addDaysD(mon, 6)), nmon = iso(addDaysD(mon, 7)), nsun = iso(addDaysD(mon, 13));
  switch (mode) {
    case "over": return d < ref; case "today": return d === ref; case "tom": return d === tom; case "next": return d === iso(nwd);
    case "week": return d >= iso(mon) && d <= sun; case "nweek": return d >= nmon && d <= nsun; case "date": return d === ZD.filter.ltDate;
  } return true;
}
const ZD_CODE = { 0: "0 Pt. – z. B. Eingabefehler, Lieferantenbeistellmaterial", 1: "1 Pt. – Fehlmaterial für SIBE / Vorproduktion", 2: "2 Pt. – Kundenaufträge sind betroffen" };

async function viewZd05(day) {
  const q = new URLSearchParams(location.hash.split("?")[1] || "");
  day = day || q.get("day") || "";
  const [d, h] = await Promise.all([api("GET", "api/zd05" + (day ? "?day=" + day : "")), api("GET", "api/zd05/history")]);
  ZD.data = d; ZD.hist = h;
  renderZd05();
}
function zdKpi() { const d = ZD.data; return d.kpi_fixed ? d.kpi : (d.kpi_live || d.kpi || {}); }
function renderZd05() {
  const d = ZD.data; const k = zdKpi();
  const idx = d.days.indexOf(d.day);
  const target = d.target || 1.2;
  const bad = k.kennzahl != null && k.kennzahl > target;
  $("#view").innerHTML = `
  <div class="head"><div class="grow"><h1>Einkauf – Fehlteile ZD05</h1>
    <p class="lede">${d.day ? `Stand ${fmtDate(d.day)} · importiert ${esc(fmtDate(d.imported_at))} ${esc((d.imported_at || "").slice(11, 16))} von ${esc(d.imported_by || "")}` : "Noch kein ZD05-Export importiert."}</p></div>
    <a class="btn" href="#/m/einkauf">Kommunikation Einkauf ↔ Produktion</a>
    <button class="btn" id="zdCsv" ${d.day ? "" : "disabled"}>Export (CSV)</button>
    <label class="btn signal" style="cursor:pointer">ZD05-Export importieren<input type="file" id="zdFile" accept=".xlsx,.csv,.txt,.tsv" hidden></label>
  </div>
  <div class="dropzone" id="zdDrop">SAP-Export (ZD05 als .xlsx oder .csv) hier hineinziehen – Bemerkungen, Liefertermine und Codes vom Vortag werden pro Material übernommen.</div>
  ${d.day ? `
  <div class="toolbar">
    <div class="weeknav"><button class="btn small" id="zdPrev" ${idx >= d.days.length - 1 ? "disabled" : ""}>‹ Vortag</button>
      <select class="field-in" id="zdDay" style="width:auto">${d.days.map(x => `<option value="${x}" ${x === d.day ? "selected" : ""}>${WD[parseIso(x).getDay()]} ${fmtDate(x)}</option>`).join("")}</select>
      <button class="btn small" id="zdNext" ${idx <= 0 ? "disabled" : ""}>Folgetag ›</button></div>
  </div>
  <div class="kpis">
    <div class="kpi ${bad ? "alarm" : ""}"><div class="v">${f2(k.kennzahl)}</div><div class="l">Bewertungskennzahl · Ziel ≤ ${f2(target)}${d.kpi_fixed ? " · Wert aus Excel" : ""}</div>
      <div class="bar"><i style="width:${Math.min(100, (k.kennzahl || 0) / 2 * 100)}%;background:${bad ? "var(--alarm)" : "var(--ok)"}"></i></div>
      ${gxSpark((ZD.hist.days || []).filter(x => x.kpi && x.kpi.kennzahl != null).slice(-20).map(x => x.kpi.kennzahl), { ref: target, cls: bad ? "bad" : "ok", h: 34, label: "Verlauf Kennzahl" })}</div>
    <div class="kpi"><div class="kpi-top"><div><div class="v">${k.anzahl ?? "–"}</div><div class="l">Positionen bewertet${k.horizon ? ` (Unterdeckung bis ${fmtDate(k.horizon)})` : ""} · Summe ${k.summe ?? "–"} Pt.</div></div>
      ${k.anzahl != null && d.rows.length ? gxRing(k.anzahl / d.rows.length * 100, Math.round(k.anzahl / d.rows.length * 100) + " %") : ""}</div>
      ${k.anzahl != null && d.rows.length ? `<div class="muted" style="font-size:11.5px">${k.anzahl} von ${d.rows.length} Positionen im Horizont</div>` : ""}</div>
    <div class="kpi ${k.code2 ? "alarm" : ""}"><div><div class="v">${k.code2 ?? 0}</div><div class="l">Fehlmaterial Kundenauftrag (Code 2)${k.c2_A != null ? ` · A ${k.c2_A} / B ${k.c2_B} / C ${k.c2_C}` : ""}</div></div>
      ${k.c2_A != null ? gxMiniBars([{ lab: "A", val: k.c2_A || 0, od: k.c2_A || 0 }, { lab: "B", val: k.c2_B || 0, od: k.c2_B || 0 }, { lab: "C", val: k.c2_C || 0, od: k.c2_C || 0 }]) : ""}</div>
    <div class="kpi"><div><div class="v">${d.rows.filter(r => r.neu).length}<span class="muted" style="font-size:18px"> neu · ${d.resolved.length} erledigt</span></div><div class="l">gegenüber ${d.prev_day ? fmtDate(d.prev_day) : "Vortag"}${(d.kpi_live || {}).ohne_code ? ` · <b style="color:var(--alarm)">${d.kpi_live.ohne_code} ohne Code</b>` : ""}</div></div>
      ${gxStack([{ cls: "s-new", n: d.rows.filter(r => r.neu).length, lab: "neu" }, { cls: "s-open", n: d.rows.filter(r => !r.neu).length, lab: "unverändert" }, { cls: "s-done", n: d.resolved.length, lab: "erledigt" }])}</div>
  </div>
  <div class="charts2">
    <div class="chart"><h3>Bewertungskennzahl pro Tag</h3><div class="legend"><span><i style="background:var(--brand)"></i>Kennzahl</span><span><i style="background:var(--alarm)"></i>Ziel ${f2(target)}</span></div>${zdTrendSvg()}</div>
    <div class="chart"><h3>Pro KW: Materialverfügbarkeit und offene Bestellungen</h3><div class="legend">${(ZD.hist.weeks || []).some(w => w.mat_verf != null) ? '<span><i style="background:var(--ok)"></i>Materialverfügbarkeit %</span>' : ""}<span><i style="background:var(--ink);opacity:.25"></i>offene Bestellungen</span></div>${zdWeekSvg()}</div>
  </div>
  <div class="toolbar">
    <input class="field-in search" id="zdQ" type="search" placeholder="Material, Text, Bemerkung …" value="${esc(ZD.filter.q)}">
    <div class="seg" role="group">${[["all", "Alle"], ["h", "Im Horizont"], ["c2", "Code 2"], ["nc", "Ohne Code"], ["neu", "Neu"]].map(([v, l]) => `<button data-zf="${v}" aria-pressed="${ZD.filter.only === v}">${l}</button>`).join("")}</div>
  </div>
  <div class="toolbar ltbar"><span class="ltlab">Liefertermin</span><div id="zdLt" class="ltchips"></div>
    <label class="fsel"><span>am</span><input type="date" class="field-in" id="zdLtDate" value="${esc(ZD.filter.ltDate)}"></label>
    <button class="btn small" id="zdLtReset" ${ZD.filter.lt ? "" : "hidden"}>alle Termine</button></div>
  <p class="muted" style="font-size:12.5px;margin:-6px 0 12px">Code: ${Object.values(ZD_CODE).map(esc).join(" · ")}</p>
  <div class="tbl-wrap"><table class="zd"><thead><tr>
    <th>Material</th><th>Klasse</th><th>Unterdeckung</th><th class="r">Verbr. WBZ</th><th class="r">SIBE opt / akt</th><th class="r">Ant. Plan</th>
    <th>Code</th><th>Liefertermin</th><th style="min-width:200px;width:26%">Bemerkung Einkauf</th></tr></thead><tbody id="zdBody"></tbody></table></div>
  ${d.resolved.length ? `<h2>Seit ${fmtDate(d.prev_day)} nicht mehr auf der Liste (${d.resolved.length})</h2><div class="chips">${d.resolved.map(r => `<span class="chip" title="${esc(r.bemerkung || "")}"><b>${esc(r.material)}</b> ${esc((r.text || "").slice(0, 30))}</span>`).join("")}</div>` : ""}
  <h2>Kennzahlen pro KW</h2>
  <div class="tbl-wrap" style="max-width:640px"><table class="zdw"><thead><tr><th>KW</th><th class="r">Materialverfügbarkeit %</th><th class="r">offene Bestellungen</th></tr></thead><tbody>${zdWeekRows()}</tbody></table></div>
  <p class="muted" style="font-size:13px;margin-top:18px">ABC/XYZ-Klassifizierung: <label class="link-like">Liste aktualisieren (Excel/CSV mit Spalten Material, ABC-Kennzeichen, XYZ-Kennzeichen)<input type="file" id="abcFile" accept=".xlsx,.csv,.txt" hidden></label></p>
  ` : ""}`;
  // Ereignisse
  $("#zdFile").onchange = e => e.target.files[0] && zdImportFile(e.target.files[0]);
  const drop = $("#zdDrop");
  if (!window._zdDrag) { window._zdDrag = 1; ["dragenter", "dragover"].forEach(t => document.addEventListener(t, zdDragOn)); }
  drop.ondragleave = () => drop.classList.remove("on");
  drop.ondrop = e => { e.preventDefault(); drop.classList.remove("on"); const f = e.dataTransfer.files[0]; if (f) zdImportFile(f); };
  if (!d.day) return;
  $("#zdDay").onchange = e => zdGo(e.target.value);
  $("#zdPrev").onclick = () => zdGo(d.days[idx + 1]);
  $("#zdNext").onclick = () => zdGo(d.days[idx - 1]);
  $("#zdCsv").onclick = zdCsv;
  let t; $("#zdQ").oninput = e => { clearTimeout(t); t = setTimeout(() => { ZD.filter.q = e.target.value; zdRenderBody(); }, 150); };
  document.querySelectorAll("[data-zf]").forEach(b => b.onclick = () => { ZD.filter.only = b.dataset.zf; document.querySelectorAll("[data-zf]").forEach(x => x.setAttribute("aria-pressed", x === b)); zdRenderBody(); });
  document.querySelectorAll(".zdw input").forEach(inp => inp.onchange = () => zdSaveWeek(inp));
  $("#abcFile").onchange = e => e.target.files[0] && zdImportAbc(e.target.files[0]);
  $("#zdLtDate").onchange = e => { ZD.filter.ltDate = e.target.value; ZD.filter.lt = e.target.value ? "date" : ""; zdRenderBody(); };
  $("#zdLtReset").onclick = () => { ZD.filter.lt = ""; ZD.filter.ltDate = ""; $("#zdLtDate").value = ""; zdRenderBody(); };
  zdRenderBody();
}
function zdDragOn(e) { const dz = $("#zdDrop"); if (!dz) return; e.preventDefault(); dz.classList.add("on"); }
function zdGo(day) { history.replaceState(null, "", "#/m/zd05?day=" + day); viewZd05(day); }
function zdRenderBody() {
  const d = ZD.data; const k = d.kpi_live || {}; const hz = k.horizon || "9999";
  const q = ZD.filter.q.toLowerCase(); const f = ZD.filter.only;
  let rows = d.rows.slice().sort((a, b) => String(a.unterdeck || "9").localeCompare(String(b.unterdeck || "9")) || a.material.localeCompare(b.material));
  rows = rows.filter(r => {
    if (q && !(r.material + " " + (r.text || "") + " " + (r.bemerkung || "")).toLowerCase().includes(q)) return false;
    const inH = !r.unterdeck || String(r.unterdeck).slice(0, 10) <= hz;
    if (f === "h" && !inH) return false; if (f === "c2" && Number(r.code) !== 2) return false;
    if (f === "nc" && (r.code === 0 || r.code === 1 || r.code === 2)) return false; if (f === "neu" && !r.neu) return false;
    return true;
  });
  // Liefertermin-Chips (Anzahl bezogen auf die übrigen Filter)
  const ref = zdRef(); const lf = ZD.filter.lt;
  const R = parseIso(ref); let nwd = addDaysD(R, 1); while (nwd.getDay() === 0 || nwd.getDay() === 6) nwd = addDaysD(nwd, 1);
  const tomIsWorkday = iso(nwd) === iso(addDaysD(R, 1));
  const chips = ZD_LT.filter(([v]) => v !== (tomIsWorkday ? "next" : "tom"));
  $("#zdLt").innerHTML = chips.map(([v, l]) => {
    const n = rows.filter(r => zdLtMatch(r, v, ref)).length;
    const lbl = v === "today" ? `Heute ${fmtDate(ref).slice(0, 6)}` : v === "tom" ? `Morgen ${fmtDate(iso(addDaysD(R, 1))).slice(0, 6)}` : v === "next" ? `${WD_LONG[nwd.getDay()]} ${fmtDate(iso(nwd)).slice(0, 6)}` : l;
    return `<button data-lt="${v}" aria-pressed="${lf === v}" class="${v === "over" && n ? "warnc" : ""}" ${n ? "" : "disabled"}>${esc(lbl)} <b>${n}</b></button>`;
  }).join("");
  $("#zdLt").querySelectorAll("[data-lt]").forEach(b => b.onclick = () => { ZD.filter.lt = ZD.filter.lt === b.dataset.lt ? "" : b.dataset.lt; if (ZD.filter.lt !== "date") { ZD.filter.ltDate = ""; $("#zdLtDate").value = ""; } zdRenderBody(); });
  $("#zdLtReset").hidden = !lf;
  if (lf) { rows = rows.filter(r => zdLtMatch(r, lf, ref)); rows.sort((a, b) => String(a.liefertermin || "9").localeCompare(String(b.liefertermin || "9")) || a.material.localeCompare(b.material)); }
  ZD.visible = rows;
  const today = todayIso();
  $("#zdBody").innerHTML = rows.length ? rows.map(r => {
    const inH = !r.unterdeck || String(r.unterdeck).slice(0, 10) <= hz;
    const lt = r.liefertermin; const ltIso = /^\d{4}-\d{2}-\d{2}$/.test(lt || "");
    const late = ltIso && r.unterdeck && lt > String(r.unterdeck).slice(0, 10);
    const past = ltIso && lt < today;
    const c = (r.code === 0 || r.code === 1 || r.code === 2) ? r.code : null;
    return `<tr data-m="${esc(r.material)}" class="${inH ? "inh" : ""}">
      <td><b class="tab">${esc(r.material)}</b>${r.neu ? ' <span class="pill info">neu</span>' : ""}<div class="sub">${esc(r.text || "")}</div><div class="sub">${[r.dmk, r.lzcode].filter(Boolean).map(esc).join(" · ")}</div></td>
      <td>${r.abc || r.xyz ? `<span class="cls cls-${esc(r.abc || "")}">${esc(r.abc || "–")}${esc(r.xyz || "")}</span>` : '<span class="muted">–</span>'}</td>
      <td class="tab" style="white-space:nowrap">${esc(fmtDate(r.unterdeck))}${inH ? '<div class="sub" style="color:var(--brand-strong)">im Horizont</div>' : ""}</td>
      <td class="n">${fmtNum(r.verbr_wbz)}</td><td class="n">${fmtNum(r.opt_sibe)} <span class="muted">/</span> ${fmtNum(r.akt_sibe)}</td><td class="n">${fmtNum(r.ant_plan)}</td>
      <td><div class="codes" role="group" aria-label="Code">${[0, 1, 2].map(n => `<button data-code="${n}" class="c${n}" aria-pressed="${c === n}" title="${esc(ZD_CODE[n])}">${n}</button>`).join("")}</div></td>
      <td>${ltIso || !lt ? `<input type="date" class="field-in zin ${past ? "past" : ""}" data-k="liefertermin" value="${esc(lt || "")}" title="${past ? "Liefertermin ist überschritten – bitte nachfassen" : late ? "Liefertermin liegt nach der Unterdeckung" : ""}">${late && !past ? '<div class="sub">nach Unterdeckung</div>' : ""}`
        : `<input class="field-in zin" data-k="liefertermin" value="${esc(lt)}" placeholder="TT.MM.JJJJ">`}</td>
      <td><textarea class="field-in zin" data-k="bemerkung" rows="2">${esc(r.bemerkung || "")}</textarea></td></tr>`;
  }).join("") : `<tr><td colspan="9" class="empty">${ZD.filter.lt ? "Für diesen Liefertermin sind keine Lieferungen eingetragen." : "Keine Positionen für diesen Filter."}</td></tr>`;
  $("#zdBody").querySelectorAll("tr[data-m]").forEach(tr => {
    const m = tr.dataset.m;
    tr.querySelectorAll("[data-code]").forEach(b => b.onclick = () => {
      const cur = tr.querySelector('[aria-pressed="true"]'); const v = cur === b ? null : Number(b.dataset.code);
      zdSave(m, { code: v }, tr);
    });
    tr.querySelectorAll(".zin").forEach(el => el.onchange = () => {
      let v = el.value.trim(); if (el.dataset.k === "liefertermin") v = parseDateInput(v);
      zdSave(m, { [el.dataset.k]: v || null }, tr);
    });
  });
}
async function zdSave(material, changes, tr) {
  try {
    const r = await api("POST", "api/zd05/row", { day: ZD.data.day, material, changes });
    const i = ZD.data.rows.findIndex(x => x.material === material);
    ZD.data.rows[i] = Object.assign({}, ZD.data.rows[i], { bemerkung: r.bemerkung, liefertermin: r.liefertermin, code: r.code });
    const fresh = await api("GET", "api/zd05?day=" + ZD.data.day);
    ZD.data.kpi = fresh.kpi; ZD.data.kpi_live = fresh.kpi_live; ZD.data.kpi_fixed = fresh.kpi_fixed;
    if ("code" in changes) { zdRenderBody(); zdUpdateKpi(); }
    else if (tr) { tr.classList.add("saved-row"); setTimeout(() => tr.classList.remove("saved-row"), 1200); }
    setSync("gespeichert " + new Date().toLocaleTimeString("de-CH", { hour: "2-digit", minute: "2-digit" }));
  } catch (e) { toast("Nicht gespeichert: " + e.message, true); }
}
function zdUpdateKpi() { const y = window.scrollY; renderZd05(); window.scrollTo(0, y); }
function zdTrendSvg() {
  const days = (ZD.hist.days || []).filter(x => x.kpi && x.kpi.kennzahl != null).slice(-80);
  if (!days.length) return '<p class="muted">Noch keine Daten.</p>';
  const W = 560, H = 210, pl = 40, pb = 22, pt = 8; const tg = ZD.hist.target || 1.2;
  const vals = days.map(x => x.kpi.kennzahl); const max = Math.max(1.6, ...vals) * 1.05, min = Math.min(0.6, ...vals) * 0.95;
  const y = v => pt + (H - pt - pb) * (1 - (v - min) / (max - min)); const x = i => pl + (W - pl - 8) * i / Math.max(1, days.length - 1);
  let s = `<svg viewBox="0 0 ${W} ${H}" role="img" aria-label="Verlauf Bewertungskennzahl">`;
  [min, (min + max) / 2, max].forEach(v => s += `<line x1="${pl}" x2="${W}" y1="${y(v)}" y2="${y(v)}" class="gx-grid"/><text x="${pl - 6}" y="${y(v) + 4}" text-anchor="end" font-size="11" class="gx-ax">${fmtNum(v, 1)}</text>`);
  s += `<line x1="${pl}" x2="${W}" y1="${y(tg)}" y2="${y(tg)}" class="gx-target"/><text x="${W - 4}" y="${y(tg) - 5}" text-anchor="end" font-size="11" class="gx-ax" style="fill:var(--alarm)">Ziel ${fmtNum(tg, 2)}</text>`;
  days.forEach((dd, i) => { if (parseIso(dd.day).getDay() === 1 && i % 2 === 0) s += `<text x="${x(i)}" y="${H - 6}" font-size="11" class="gx-ax" text-anchor="middle">KW${isoWeek(parseIso(dd.day))}</text>`; });
  s += `<polyline points="${days.map((dd, i) => `${x(i)},${y(dd.kpi.kennzahl)}`).join(" ")}" class="gx-ln brand"/>`;
  days.forEach((dd, i) => { if (dd.kpi.kennzahl > tg) s += `<circle cx="${x(i)}" cy="${y(dd.kpi.kennzahl)}" r="2.6" class="gx-dot-bad"><title>${fmtDate(dd.day)}: ${fmtNum(dd.kpi.kennzahl, 2)}</title></circle>`; });
  return s + "</svg>";
}
function zdWeekSvg() {
  const w = (ZD.hist.weeks || []).filter(x => x.mat_verf != null || x.off_best != null).slice(-30);
  if (!w.length) return '<p class="muted">Noch keine Wochenwerte.</p>';
  const W = 560, H = 210, pl = 40, pr = 40, pb = 22, pt = 8;
  const mv = w.map(x => x.mat_verf).filter(v => v != null), ob = w.map(x => x.off_best).filter(v => v != null);
  const m1 = Math.min(...mv, 99) - 0.1, M1 = 100; const M2 = Math.max(...ob, 10) * 1.1;
  const x = i => pl + (W - pl - pr) * i / Math.max(1, w.length - 1);
  const y1 = v => pt + (H - pt - pb) * (1 - (v - m1) / (M1 - m1)); const y2 = v => pt + (H - pt - pb) * (1 - v / M2);
  let s = `<svg viewBox="0 0 ${W} ${H}" role="img">`;
  [m1, (m1 + M1) / 2, M1].forEach(v => s += `<line x1="${pl}" x2="${W - pr}" y1="${y1(v)}" y2="${y1(v)}" class="gx-grid"/>${mv.length ? `<text x="${pl - 6}" y="${y1(v) + 4}" text-anchor="end" font-size="11" class="gx-ok-t">${fmtNum(v, 1)}</text>` : ""}`);
  [0, M2 / 2, M2].forEach(v => s += `<text x="${W - pr + 6}" y="${y2(v) + 4}" font-size="11" class="gx-in-t">${fmtNum(Math.round(v))}</text>`);
  w.forEach((ww, i) => { const bw = (W - pl - pr) / w.length * 0.6; if (ww.off_best != null) s += `<rect x="${x(i) - bw / 2}" y="${y2(ww.off_best)}" width="${bw}" height="${y2(0) - y2(ww.off_best)}" class="gx-bar mute" rx="2"><title>${ww.kw}: ${ww.off_best} offene Bestellungen</title></rect>`;
    if (i % 4 === 0) s += `<text x="${x(i)}" y="${H - 6}" font-size="11" class="gx-ax" text-anchor="middle">${ww.kw.slice(5)}</text>`; });
  s += `<polyline points="${w.map((ww, i) => ww.mat_verf != null ? `${x(i)},${y1(ww.mat_verf)}` : null).filter(Boolean).join(" ")}" class="gx-ln ok"/>`;
  return s + "</svg>";
}
function zdWeekRows() {
  const map = {}; (ZD.hist.weeks || []).forEach(w => map[w.kw] = w);
  const out = []; let d = monday(new Date());
  for (let i = 0; i < 10; i++) {
    const kwDate = addDaysD(d, -7 * i); const kw = `${kwDate.getFullYear()}-W${String(isoWeek(kwDate)).padStart(2, "0")}`;
    const w = map[kw] || {};
    out.push(`<tr><td><b>KW ${isoWeek(kwDate)}</b> <span class="muted">${fmtDate(iso(kwDate)).slice(0, 6)}</span></td>
      <td class="n"><input class="field-in" data-kw="${kw}" data-k="mat_verf" inputmode="decimal" value="${w.mat_verf != null ? fmtNum(w.mat_verf, 2) : ""}" style="text-align:right;max-width:120px"></td>
      <td class="n"><input class="field-in" data-kw="${kw}" data-k="off_best" inputmode="decimal" value="${w.off_best != null ? fmtNum(w.off_best) : ""}" style="text-align:right;max-width:120px"></td></tr>`);
  }
  return out.join("");
}
async function zdSaveWeek(inp) {
  try { await api("POST", "api/zd05/week", { kw: inp.dataset.kw, changes: { [inp.dataset.k]: inp.value } }); ZD.hist = await api("GET", "api/zd05/history"); toast("Wochenwert gespeichert"); }
  catch (e) { toast("Nicht gespeichert: " + e.message, true); }
}
function zdCsv() {
  const d = ZD.data; const H = ["Material", "Materialkurztext", "Unterdeck.", "DMk", "Lz-Code", "Verbr. WBZ", "opt. SIBE", "akt. SIBE", "Ant. Plan", "Bemerkung", "Liefertermin", "Code", "ABC", "XYZ"];
  const K = ["material", "text", "unterdeck", "dmk", "lzcode", "verbr_wbz", "opt_sibe", "akt_sibe", "ant_plan", "bemerkung", "liefertermin", "code", "abc", "xyz"];
  const cell = v => { if (v == null) return ""; v = String(v); if (/^\d{4}-\d{2}-\d{2}$/.test(v)) v = fmtDate(v); return /[";\n]/.test(v) ? `"${v.replace(/"/g, '""')}"` : v; };
  const list = ZD.visible && ZD.visible.length ? ZD.visible : d.rows;
  const csv = "\ufeff" + [H.join(";")].concat(list.map(r => K.map(k => cell(r[k])).join(";"))).join("\r\n");
  const a = document.createElement("a"); a.href = URL.createObjectURL(new Blob([csv], { type: "text/csv;charset=utf-8" })); a.download = `ZD05_${d.day}.csv`; a.click();
}

/* ---------- Datei lesen: XLSX (ohne Bibliothek, über DecompressionStream) und CSV */
async function readTable(file) {
  const buf = await file.arrayBuffer();
  const u8 = new Uint8Array(buf);
  if (u8[0] === 0x50 && u8[1] === 0x4b) return readXlsx(u8);
  let text = new TextDecoder("utf-8").decode(u8);
  if (text.includes("\ufffd")) text = new TextDecoder("windows-1252").decode(u8);
  return readCsv(text);
}
function readCsv(text) {
  text = text.replace(/^\ufeff/, "");
  const first = text.split(/\r?\n/).slice(0, 15).join("\n");
  const delim = ["\t", ";", ",", "|"].map(c => [c, first.split(c).length]).sort((a, b) => b[1] - a[1])[0][0];
  const rows = []; let row = [], cur = "", inQ = false;
  for (let i = 0; i < text.length; i++) {
    const ch = text[i];
    if (inQ) { if (ch === '"') { if (text[i + 1] === '"') { cur += '"'; i++; } else inQ = false; } else cur += ch; continue; }
    if (ch === '"') inQ = true;
    else if (ch === delim) { row.push(cur); cur = ""; }
    else if (ch === "\n" || ch === "\r") { if (ch === "\r" && text[i + 1] === "\n") i++; row.push(cur); rows.push(row); row = []; cur = ""; }
    else cur += ch;
  }
  if (cur || row.length) { row.push(cur); rows.push(row); }
  return rows.map(r => r.map(c => c.trim()));
}
async function inflateRaw(data) {
  const ds = new DecompressionStream("deflate-raw");
  const out = await new Response(new Blob([data]).stream().pipeThrough(ds)).arrayBuffer();
  return new Uint8Array(out);
}
async function unzip(u8) {
  const dv = new DataView(u8.buffer, u8.byteOffset, u8.byteLength);
  let eocd = -1; for (let i = u8.length - 22; i >= Math.max(0, u8.length - 70000); i--) if (dv.getUint32(i, true) === 0x06054b50) { eocd = i; break; }
  if (eocd < 0) throw new Error("Keine gültige Excel-Datei");
  const n = dv.getUint16(eocd + 10, true); let p = dv.getUint32(eocd + 16, true);
  const files = {};
  for (let i = 0; i < n; i++) {
    const method = dv.getUint16(p + 10, true), csize = dv.getUint32(p + 20, true);
    const nlen = dv.getUint16(p + 28, true), elen = dv.getUint16(p + 30, true), clen = dv.getUint16(p + 32, true), off = dv.getUint32(p + 42, true);
    const name = new TextDecoder().decode(u8.subarray(p + 46, p + 46 + nlen));
    files[name] = { method, csize, off }; p += 46 + nlen + elen + clen;
  }
  return async name => {
    const f = files[name]; if (!f) return null;
    const lnl = dv.getUint16(f.off + 26, true), lel = dv.getUint16(f.off + 28, true);
    const data = u8.subarray(f.off + 30 + lnl + lel, f.off + 30 + lnl + lel + f.csize);
    const raw = f.method === 0 ? data : await inflateRaw(data);
    return new TextDecoder().decode(raw);
  };
}
async function readXlsx(u8) {
  if (typeof DecompressionStream === "undefined") throw new Error("Dieser Browser kann Excel-Dateien nicht lesen – bitte als CSV exportieren oder Edge/Chrome verwenden.");
  const get = await unzip(u8); const P = new DOMParser();
  const ss = []; const sst = await get("xl/sharedStrings.xml");
  if (sst) P.parseFromString(sst, "application/xml").querySelectorAll("si").forEach(si => ss.push([...si.querySelectorAll("t")].map(t => t.textContent).join("")));
  // erstes Tabellenblatt ermitteln
  let path = "xl/worksheets/sheet1.xml";
  const wbx = await get("xl/workbook.xml"), rels = await get("xl/_rels/workbook.xml.rels");
  if (wbx && rels) {
    const s1 = P.parseFromString(wbx, "application/xml").querySelector("sheet");
    const rid = s1 && (s1.getAttribute("r:id") || s1.getAttributeNS("http://schemas.openxmlformats.org/officeDocument/2006/relationships", "id"));
    const rel = [...P.parseFromString(rels, "application/xml").querySelectorAll("Relationship")].find(r => r.getAttribute("Id") === rid);
    if (rel) { const t = rel.getAttribute("Target"); path = t.startsWith("/") ? t.slice(1) : "xl/" + t.replace(/^\.\//, ""); }
  }
  const xml = await get(path); if (!xml) throw new Error("Tabellenblatt nicht gefunden");
  const doc = P.parseFromString(xml, "application/xml");
  const colIdx = ref => { let n = 0; for (const ch of ref.replace(/\d+/g, "")) n = n * 26 + ch.charCodeAt(0) - 64; return n - 1; };
  const rows = [];
  doc.querySelectorAll("sheetData > row").forEach(r => {
    const out = [];
    r.querySelectorAll("c").forEach(c => {
      const t = c.getAttribute("t"); const v = c.querySelector("v"); let val = v ? v.textContent : "";
      if (t === "s") val = ss[Number(val)] ?? ""; else if (t === "inlineStr") val = [...c.querySelectorAll("t")].map(x => x.textContent).join("");
      else if (t !== "str" && t !== "b" && val !== "" && !isNaN(Number(val))) val = Number(val);
      out[colIdx(c.getAttribute("r"))] = val;
    });
    rows.push(Array.from(out, x => x ?? ""));
  });
  return rows;
}
const ZD_MAP = {
  material: ["material", "materialnummer", "matnr", "material nr"], text: ["materialkurztext", "kurztext", "materialtext", "bezeichnung", "benennung"],
  unterdeck: ["unterdeck", "unterdeckung", "datum unterdeckung", "unterd", "unterdeckungsdatum"], dmk: ["dmk", "dispomerkmal", "dispositionsmerkmal"],
  lzcode: ["lz-code", "lzcode", "lieferzeitcode", "lz code"], verbr_wbz: ["verbr. wbz", "verbr wbz", "verbrauch wbz", "verbrauch in wbz"],
  opt_sibe: ["opt. sibe", "opt sibe", "optimaler sicherheitsbestand", "optimaler sibe"], akt_sibe: ["akt. sibe", "akt sibe", "aktueller sicherheitsbestand", "sicherheitsbestand", "sibe"],
  ant_plan: ["ant. plan", "ant plan", "anteil plan", "anteil planung"],
  bemerkung: ["bemerkung"], liefertermin: ["liefertermin"], code: ["code"],
  abc: ["abc-kennzeichen", "abc", "abc kennzeichen"], xyz: ["xyz-kennzeichen", "xyz", "xyz kennzeichen"], art: ["materialart"],
};
const normH = s => String(s || "").toLowerCase().replace(/[\s.\-_:]+/g, "");
function mapTable(rows, keys) {
  const MAPN = Object.fromEntries(Object.entries(ZD_MAP).map(([k, v]) => [k, v.map(normH)]));
  let hi = rows.findIndex(r => r.some(c => MAPN.material.includes(normH(c))));
  if (hi < 0) throw new Error("Keine Spalte \"Material\" gefunden");
  const head = rows[hi].map(normH); const idx = {};
  for (const k of keys) { const i = head.findIndex(h => MAPN[k].includes(h)); if (i >= 0) idx[k] = i; }
  const out = [];
  for (const r of rows.slice(hi + 1)) {
    const m = String(r[idx.material] ?? "").trim(); if (!m || !/[A-Za-z0-9]/.test(m) || /^(summe|total)/i.test(m)) continue;
    const o = {}; for (const [k, i] of Object.entries(idx)) o[k] = r[i];
    out.push(o);
  }
  return { rows: out, found: Object.keys(idx), missing: keys.filter(k => !(k in idx)) };
}
function excelDate(v) {
  if (v === "" || v == null) return null;
  if (typeof v === "number" && v > 20000 && v < 80000) { const d = new Date(Math.round((v - 25569) * 86400000)); return iso(new Date(d.getUTCFullYear(), d.getUTCMonth(), d.getUTCDate())); }
  const s = String(v).trim(); const iso2 = parseDateInput(s.split(" ")[0]); return /^\d{4}-\d{2}-\d{2}$/.test(iso2) ? iso2 : s;
}
function sapNum(v) {
  if (v === "" || v == null) return null; if (typeof v === "number") return v;
  let s = String(v).trim().replace(/['’\s]/g, ""); let neg = false;
  if (/-$/.test(s)) { neg = true; s = s.slice(0, -1); }   // SAP: "123-" = negativ
  if (/^-?\d{1,3}(\.\d{3})+(,\d+)?$/.test(s)) s = s.replace(/\./g, "").replace(",", ".");
  else if (/^-?\d{1,3}(,\d{3})+(\.\d+)?$/.test(s)) s = s.replace(/,/g, "");
  else s = s.replace(",", ".");
  const n = Number(s); return isNaN(n) ? String(v) : (neg ? -n : n);
}
async function zdImportFile(file) {
  let table;
  try { table = mapTable(await readTable(file), ["material", "text", "unterdeck", "dmk", "lzcode", "verbr_wbz", "opt_sibe", "akt_sibe", "ant_plan"]); }
  catch (e) { toast("Datei konnte nicht gelesen werden: " + e.message, true); return; }
  const rows = table.rows.map(r => ({ ...r, unterdeck: excelDate(r.unterdeck), verbr_wbz: sapNum(r.verbr_wbz), opt_sibe: sapNum(r.opt_sibe), akt_sibe: sapNum(r.akt_sibe), ant_plan: sapNum(r.ant_plan), material: String(r.material).trim(), text: r.text != null ? String(r.text).trim() : r.text }));
  const lbl = { text: "Kurztext", unterdeck: "Unterdeckung", dmk: "DMk", lzcode: "Lz-Code", verbr_wbz: "Verbr. WBZ", opt_sibe: "opt. SIBE", akt_sibe: "akt. SIBE", ant_plan: "Ant. Plan" };
  const back = document.createElement("div"); back.className = "modal-back";
  back.innerHTML = `<form class="modal" style="width:min(520px,94vw)"><h2>ZD05 importieren</h2>
    <p><b>${rows.length} Positionen</b> in „${esc(file.name)}“ erkannt.${table.missing.length ? `<br><span style="color:var(--alarm)">Nicht gefunden: ${table.missing.map(k => lbl[k] || k).join(", ")}</span>` : " Alle Spalten erkannt."}</p>
    <label class="fld"><label>Stand (Datum)</label><input class="field-in" type="date" name="d" value="${todayIso()}" required></label>
    <p class="muted" style="font-size:13px;margin-top:12px">Bemerkung, Liefertermin und Code werden pro Material vom letzten Import übernommen. Ein bestehender Import für dieses Datum wird ersetzt (bereits erfasste Bemerkungen bleiben).</p>
    <div style="display:flex;gap:8px;justify-content:flex-end;margin-top:16px"><button type="button" class="btn" data-x>Abbrechen</button><button class="btn primary">Importieren</button></div></form>`;
  document.body.appendChild(back);
  $("[data-x]", back).onclick = () => back.remove();
  $("form", back).onsubmit = async e => {
    e.preventDefault(); const day = $("input[name=d]", back).value;
    try {
      const r = await api("POST", "api/zd05/import", { day, rows, filename: file.name });
      back.remove(); toast(`${r.positionen} Positionen importiert · ${r.neu} neu · ${r.erledigt.length} nicht mehr auf der Liste`);
      zdGo(day); updateNavCounts();
    } catch (err) { toast("Import fehlgeschlagen: " + err.message, true); }
  };
}
async function zdImportAbc(file) {
  try {
    const t = mapTable(await readTable(file), ["material", "text", "abc", "xyz", "art"]);
    if (!t.found.includes("abc")) throw new Error("Spalte ABC-Kennzeichen fehlt");
    if (!confirm(`${t.rows.length} Materialien als neue ABC/XYZ-Liste übernehmen? Die bisherige Liste wird ersetzt.`)) return;
    const r = await api("POST", "api/zd05/abc", { rows: t.rows });
    toast(`ABC/XYZ-Liste mit ${r.anzahl} Materialien gespeichert`); viewZd05(ZD.data.day);
  } catch (e) { toast("ABC/XYZ-Liste nicht übernommen: " + e.message, true); }
}

/* ================================================================ 7.5 Forecast nächste Woche */
const FC_DEPTS = [
  { dept: "SEH", mod: "seh", shifts: ["ma1", "ma2"] }, { dept: "TR5", mod: "tr5", shifts: ["ma1", "ma2"] },
  { dept: "TX", mod: "tx", shifts: ["ma1", "ma2", "ma3"] }, { dept: "DW", mod: "dw", shifts: ["ma1", "ma2", "ma3"] },
  { dept: "CZ", mod: "cz", shifts: ["ma1", "ma2", "ma3"] }];
const SHIFT_LBL = { ma1: "Schicht 1 Früh", ma2: "Schicht 2 Spät", ma3: "Schicht 3 Nacht" };
const FC = { sel: "TX", data: null, hist: null, monday: null, showOrders: false };
const f1 = v => (v == null || !isFinite(v)) ? "–" : new Intl.NumberFormat("de-CH", { minimumFractionDigits: 1, maximumFractionDigits: 1 }).format(v);
const median = a => { const b = a.filter(x => isFinite(x)).sort((x, y) => x - y); if (!b.length) return null; const m = Math.floor(b.length / 2); return b.length % 2 ? b[m] : (b[m - 1] + b[m]) / 2; };
const half = v => Math.round(v * 2) / 2;

async function viewForecast() {
  const q = new URLSearchParams(location.hash.split("?")[1] || "");
  FC.monday = q.get("monday") || iso(addDaysD(monday(new Date()), 7));
  const histFrom = iso(addDaysD(parseIso(FC.monday), -63));
  const [d, h] = await Promise.all([
    api("GET", "api/forecast?monday=" + FC.monday),
    api("GET", `api/daily?module=${FC_DEPTS.map(x => x.mod).join(",")}&from=${histFrom}&to=${todayIso()}`)]);
  FC.data = d; FC.hist = h;
  renderForecast();
}

function fcCompute(def) {
  const d = FC.data, hist = FC.hist[def.mod] || {}, mod = S.mods[def.mod];
  const set = d.settings[def.dept] || {}, plan = d.plan[def.dept] || {};
  // Historie: Leistung (Stk pro FTE und Tag) und typische Besetzung pro Wochentag
  const samples = [], byWd = {};
  Object.keys(hist).sort().forEach(day => {
    const v = computeRecord(mod, hist[day].data, { module: def.mod, day });
    const fte = def.shifts.reduce((t, k) => t + (typeof v[k] === "number" ? v[k] : 0), 0);
    if (typeof v.prod === "number" && v.prod > 0 && fte > 0) samples.push({ day, r: v.prod / fte, prod: v.prod, fte });
    if (fte > 0) { const wd = parseIso(day).getDay(); (byWd[wd] = byWd[wd] || []).push(v); }
  });
  const recent = samples.slice(-40);
  const leistungHist = median(recent.map(x => x.r));
  const leistung = set.leistung || leistungHist;
  const lastRec = Object.keys(hist).sort().reverse().map(k => hist[k].data).find(x => typeof x.vorrat_1m === "number");
  const est = lastRec ? lastRec.vorrat_1m / 20 : median(recent.map(x => x.prod));
  // Tage der Woche
  const orders = d.orders.filter(o => o.dept === def.dept);
  let days = [0, 1, 2, 3, 4].map(i => iso(addDaysD(parseIso(d.monday), i)));
  const sat = iso(addDaysD(parseIso(d.monday), 5));
  if (orders.some(o => o.day === sat) || plan[sat] || (byWd[6] || []).length > 3) days.push(sat);
  const hasOrders = orders.length > 0;
  const rows = days.map(day => {
    const wd = parseIso(day).getDay(); const p = plan[day] || {};
    const od = orders.filter(o => o.day === day);
    const fromOrders = od.reduce((t, o) => t + (Number(o.menge) || 0), 0);
    let menge, src;
    if (p.plan != null) { menge = p.plan; src = "manuell"; }
    else if (hasOrders) { menge = fromOrders; src = "SAP"; }
    else { menge = wd === 6 ? 0 : Math.round(est || 0); src = "Schätzung"; }
    const hw = (byWd[wd] || []).slice(-4);
    const staff = {}; def.shifts.forEach(k => {
      const dflt = hw.length ? half(hw.reduce((t, v) => t + (typeof v[k] === "number" ? v[k] : 0), 0) / hw.length) : 0;
      staff[k] = p[k] != null ? p[k] : dflt; staff[k + "_d"] = dflt; staff[k + "_m"] = p[k] != null;
    });
    const fte = def.shifts.reduce((t, k) => t + staff[k], 0);
    const kap = leistung ? fte * leistung : null;
    const need = leistung ? menge / leistung : null;
    const delta = need != null ? fte - need : null;
    const ausl = kap ? menge / kap : (menge ? Infinity : null);
    return { day, wd, menge, src, nOrders: od.length, staff, fte, kap, need, delta, ausl, notiz: p.notiz };
  });
  // Empfehlungen pro Tag
  const over = rows.filter(r => r.ausl > 1.05), under = rows.filter(r => r.ausl != null && r.ausl < 0.85 && r.menge > 0 && r.wd !== 6);
  rows.forEach(r => {
    if (!leistung) { r.tip = "Leistung unbekannt – bitte Stk/FTE eintragen"; r.cls = ""; r.ausl = null; return; }
    if (r.menge === 0) { r.cls = r.fte > 0 ? "res" : "ok"; r.tip = r.fte > 0 ? `Keine Menge geplant – ${f1(r.fte)} FTE frei für andere Abteilungen oder Schicht streichen` : "Keine Menge geplant"; return; }
    if (r.ausl > 1.05) {
      r.cls = "bad"; const miss = -r.delta; const tips = [];
      if (def.shifts.includes("ma2") && r.staff.ma2 === 0) tips.push("2. Schicht öffnen");
      else tips.push(`Besetzung um ${f1(Math.ceil(miss * 2) / 2)} FTE erhöhen`);
      const rv = under.filter(u => u.day !== r.day).sort((a, b) => b.delta - a.delta)[0];
      if (rv) tips.push(`oder ${fmtNum(Math.round(Math.min(r.menge - r.kap, rv.delta * leistung)))} Stk auf ${WD[rv.wd]} verschieben`);
      r.tip = `Es fehlen ${f1(miss)} FTE (≈ ${fmtNum(Math.round(r.menge - r.kap))} Stk): ${tips.join(" ")}`;
    } else if (r.ausl != null && r.ausl < 0.85) {
      r.cls = "res"; const ov = over.filter(o => o.day !== r.day)[0];
      r.tip = `Reserve ${f1(r.delta)} FTE (≈ ${fmtNum(Math.round(r.kap - r.menge))} Stk)` + (ov ? ` – Menge von ${WD[ov.wd]} vorziehen` : " – Vorziehen aus nächster Woche oder MA ausleihen");
    } else { r.cls = "ok"; r.tip = "Plan und Kapazität passen"; }
  });
  const tot = rows.reduce((t, r) => ({ menge: t.menge + r.menge, kap: t.kap + (r.kap || 0), fte: t.fte + r.fte, need: t.need + (r.need || 0) }), { menge: 0, kap: 0, fte: 0, need: 0 });
  tot.ausl = tot.kap && leistung ? tot.menge / tot.kap : null; tot.delta = tot.fte - tot.need; tot.avgDelta = rows.length ? tot.delta / rows.filter(r => r.wd !== 6).length : 0;
  return { def, rows, tot, leistung, leistungHist, nSamples: recent.length, hasOrders, orders, est, set };
}
const auslCls = a => a == null ? "" : a > 1.05 ? "bad" : a < 0.85 ? "res" : "ok";

function renderForecast() {
  const d = FC.data; const mon = parseIso(d.monday); const kw = isoWeek(mon);
  const all = FC_DEPTS.map(fcCompute);
  const unassigned = d.orders.filter(o => !FC_DEPTS.some(x => x.dept === o.dept));
  const days = [0, 1, 2, 3, 4].map(i => iso(addDaysD(mon, i)));
  // abteilungsübergreifende Hinweise
  const tips = [];
  const short = all.filter(c => c.leistung && c.tot.avgDelta < -0.5).sort((a, b) => a.tot.avgDelta - b.tot.avgDelta);
  const spare = all.filter(c => c.leistung && c.tot.avgDelta > 0.5).sort((a, b) => b.tot.avgDelta - a.tot.avgDelta);
  short.forEach(s => {
    const sp = spare.find(x => x !== s);
    tips.push(`<b>${s.def.dept}</b> ist über die Woche mit Ø ${f1(-s.tot.avgDelta)} FTE pro Tag unterbesetzt (${fmtNum(Math.round(s.tot.menge - s.tot.kap))} Stk über Kapazität).` +
      (sp ? ` <b>${sp.def.dept}</b> hat Ø ${f1(sp.tot.avgDelta)} FTE Reserve – Ausgleich prüfen (Qualifikation beachten).` : " Keine andere Abteilung hat Reserve – Überzeit, Zusatzschicht oder Terminverschiebung prüfen."));
  });
  // Reserve-Abteilungen gezielt den Engpass-Tagen anderer Abteilungen gegenüberstellen
  spare.filter(sp => sp.tot.avgDelta > 1).forEach(sp => {
    const need = all.filter(c => c !== sp && c.leistung).flatMap(c => c.rows.filter(r => r.ausl > 1.05).map(r => `${c.def.dept} ${WD[r.wd]} (${f1(-r.delta)} FTE)`));
    tips.push(`<b>${sp.def.dept}</b> hat über die Woche Ø ${f1(sp.tot.avgDelta)} FTE pro Tag Reserve (Auslastung ${fmtNum(Math.round(sp.tot.ausl * 100))} %).` +
      (need.length ? ` Engpässe anderswo: ${need.join(", ")} – Ausleihen prüfen.` : " Menge vorziehen oder Personal anders einsetzen."));
  });
  all.filter(c => !c.leistung && c.hasOrders).forEach(c => tips.push(`<b>${c.def.dept}</b>: Aufträge vorhanden, aber keine Leistung bekannt – bitte Stk pro FTE eintragen.`));
  all.forEach(c => c.rows.filter(r => r.ausl > 1.2).forEach(r => tips.push(`<b>${c.def.dept} ${WD[r.wd]} ${fmtDate(r.day).slice(0, 6)}</b>: Auslastung ${fmtNum(Math.round(r.ausl * 100))} % – ${esc(r.tip)}`)));
  if (unassigned.length) tips.push(`<b>${unassigned.length} Aufträge</b> konnten keiner Abteilung zugeordnet werden (Disponent unbekannt) – siehe unten.`);
  const sel = all.find(c => c.def.dept === FC.sel) || all[0];
  const src = d.import ? `${fmtNum(d.orders.length)} Aufträge aus SAP-Import vom ${fmtDate(d.import.ts)} ${d.import.ts.slice(11, 16)} (${esc(d.import.user)}${d.import.filename ? ", " + esc(d.import.filename) : ""})`
    : "Noch keine SAP-Planaufträge für diese Woche – Menge geschätzt aus Auftragsvorrat 1 Mte ÷ 20 Arbeitstage";
  $("#view").innerHTML = `
  <div class="head"><div class="grow"><h1>Forecast KW ${kw}</h1><p class="lede">${fmtDate(days[0]).slice(0, 6)}–${fmtDate(days[4])} · ${src}</p></div>
    <div class="weeknav"><button class="btn small" data-fw="-7">‹ Woche</button><button class="btn small" data-fw="0">Nächste Woche</button><button class="btn small" data-fw="7">Woche ›</button></div>
    <label class="btn signal" style="cursor:pointer">Planaufträge importieren<input type="file" id="fcFile" accept=".xlsx,.csv,.txt" hidden></label></div>
  <div class="dropzone" id="fcDrop">SAP-Export der Plan-/Fertigungsaufträge (z. B. COOIS / MD04, .xlsx oder .csv) hier hineinziehen. Benötigte Spalten: Auftrag, Material, Menge, Eckend- oder Eckstarttermin, Disponent.</div>
  <div class="tbl-wrap"><table class="fcov"><thead><tr><th>Abteilung</th>${days.map(x => `<th>${WD[parseIso(x).getDay()]} ${fmtDate(x).slice(0, 6)}</th>`).join("")}<th>Woche</th></tr></thead><tbody>
  ${all.map(c => `<tr data-fd="${c.def.dept}" class="${c.def.dept === sel.def.dept ? "sel" : ""}"><th>${c.def.dept}<div class="sub">${c.leistung ? `${fmtNum(Math.round(c.leistung))} Stk/FTE` : "Leistung fehlt"}${!c.hasOrders ? " · Schätzung" : ""}</div></th>
    ${days.map(x => { const r = c.rows.find(y => y.day === x); if (!r) return "<td></td>";
      return `<td class="heat ${auslCls(r.ausl)}"><b>${r.ausl == null ? "–" : isFinite(r.ausl) ? fmtNum(Math.round(r.ausl * 100)) + " %" : "∞"}</b><div class="sub">${c.leistung ? `${fmtNum(Math.round(r.menge))} / ${r.kap != null ? fmtNum(Math.round(r.kap)) : "–"}` : "keine Daten"}</div></td>`; }).join("")}
    <td class="heat ${auslCls(c.tot.ausl)}"><b>${c.tot.ausl != null ? fmtNum(Math.round(c.tot.ausl * 100)) + " %" : "–"}</b><div class="sub">${c.leistung ? `${fmtNum(Math.round(c.tot.menge))} / ${fmtNum(Math.round(c.tot.kap))}` : "keine Daten"}</div></td></tr>`).join("")}
  </tbody></table></div>
  <p class="muted" style="font-size:12.5px;margin:6px 2px 0">Auslastung = geplante Menge ÷ Kapazität (geplante FTE × Leistung). <span class="heat-key ok">85–105 %</span> <span class="heat-key bad">über 105 %</span> <span class="heat-key res">unter 85 % = Reserve</span></p>
  <div class="chart" style="margin-top:14px"><h3>Auslastung der Woche pro Abteilung: Bedarf (Menge) gegen Kapazität</h3>
    <div class="legend"><span><i style="background:var(--ok)"></i>85–105 %</span><span><i style="background:var(--alarm)"></i>über 105 %</span><span><i style="background:var(--info)"></i>unter 85 % (Reserve)</span><span>Strich = 100 % Kapazität</span></div>
    <div class="fcbars">${all.map(c => { const a = c.tot.ausl; const w = a == null ? 0 : Math.min(150, isFinite(a) ? a * 100 : 150) / 150 * 100;
      return `<div class="fb" data-fd="${c.def.dept}" style="cursor:pointer"><span class="nm">${c.def.dept}</span><div class="tr"><i class="${auslCls(a)}" style="width:${w.toFixed(1)}%"></i><u style="left:${(100 / 150 * 100).toFixed(2)}%"></u></div>
      <span class="vv">${a != null ? fmtNum(Math.round(a * 100)) + " %" : "–"}${c.leistung ? ` · ${fmtNum(Math.round(c.tot.menge))} / ${fmtNum(Math.round(c.tot.kap))}` : ""}</span></div>`; }).join("")}</div></div>
  ${tips.length ? `<div class="side-card tips"><h3>Hinweise für die Planung</h3><ul>${tips.map(t => `<li>${t}</li>`).join("")}</ul></div>` : `<div class="side-card tips"><h3>Hinweise für die Planung</h3><p>Alle Abteilungen sind ausgeglichen geplant.</p></div>`}
  <div class="tabs">${all.map(c => `<button data-ft="${c.def.dept}" aria-pressed="${c.def.dept === sel.def.dept}">${c.def.dept}</button>`).join("")}</div>
  <div id="fcDetail"></div>
  ${unassigned.length ? `<h2>Nicht zugeordnete Aufträge (${unassigned.length})</h2><p class="muted" style="font-size:13px">Zuordnung über den Disponenten: ${Object.entries(d.prefix || S.cfg.forecast_prefix || {}).map(([k, v]) => `${k} = ${v.join(", ")}…`).join(" · ")}. Anpassbar in <code>forecast.py</code>.</p>${fcOrderTable(unassigned)}` : ""}`;
  document.querySelectorAll("[data-fw]").forEach(b => b.onclick = () => { const n = Number(b.dataset.fw); const m = n === 0 ? iso(addDaysD(monday(new Date()), 7)) : iso(addDaysD(parseIso(FC.monday), n)); history.replaceState(null, "", "#/m/forecast?monday=" + m); viewForecast(); });
  document.querySelectorAll("[data-ft],[data-fd]").forEach(b => b.onclick = () => { FC.sel = b.dataset.ft || b.dataset.fd; renderForecast(); });
  $("#fcFile").onchange = e => e.target.files[0] && fcImportFile(e.target.files[0]);
  const drop = $("#fcDrop");
  drop.ondragover = e => { e.preventDefault(); drop.classList.add("on"); }; drop.ondragleave = () => drop.classList.remove("on");
  drop.ondrop = e => { e.preventDefault(); drop.classList.remove("on"); const f = e.dataTransfer.files[0]; if (f) fcImportFile(f); };
  fcRenderDetail(sel);
}

function fcRenderDetail(c) {
  const def = c.def;
  const cell = (r, inner, cls = "") => `<td class="${cls} ${r.day === todayIso() ? "today" : ""}">${inner}</td>`;
  const inp = (r, k, v, manual, title) => `<input data-fk="${k}" data-fday="${r.day}" inputmode="decimal" class="${manual ? "man" : ""}" value="${v != null ? fmtNum(v) : ""}" title="${esc(title || "")}" autocomplete="off">`;
  const rows = c.rows;
  $("#fcDetail").innerHTML = `
  <div class="fc-head">
    <div><h2 style="margin:0">${def.dept} · Plan und Kapazität</h2>
      <p class="muted" style="margin:4px 0 0;font-size:13px">Menge: ${c.hasOrders ? "aus SAP-Aufträgen" : "geschätzt aus Auftragsvorrat 1 Mte ÷ 20"} · Besetzung: Vorschlag aus Ø der letzten 4 gleichen Wochentage, überschreibbar</p></div>
    <label class="fld" style="min-width:250px"><label>Leistung [Stk pro FTE und Tag]</label>
      <input class="field-in" id="fcLeistung" inputmode="decimal" value="${c.set.leistung != null ? fmtNum(c.set.leistung) : ""}" placeholder="${c.leistungHist ? fmtNum(Math.round(c.leistungHist)) + " (Historie)" : "eintragen"}">
      <div class="sub muted" style="font-size:12px;margin-top:3px">${c.leistungHist ? `Historie: ${fmtNum(Math.round(c.leistungHist))} Stk/FTE (Median aus ${c.nSamples} Tagen)` : "Keine Historie mit Produktion und Personal vorhanden"}</div></label>
  </div>
  <div class="grid-wrap"><table class="grid fcg"><colgroup><col class="lab">${rows.map(() => '<col class="day">').join("")}<col class="day"></colgroup>
  <thead><tr><th>Kennzahl</th>${rows.map(r => `<th>${WD[r.wd]} ${fmtDate(r.day).slice(0, 6)}</th>`).join("")}<th>Woche</th></tr></thead><tbody>
  <tr class="grp"><th colspan="${rows.length + 2}">Geplante Menge</th></tr>
  <tr><th class="lab">Menge <span class="u">Stk.</span> <span class="src sap">SAP</span></th>${rows.map(r => cell(r, inp(r, "plan", Math.round(r.menge), r.src === "manuell", r.src === "SAP" ? `${r.nOrders} Aufträge aus SAP – überschreiben für manuelle Planung` : r.src === "Schätzung" ? "Schätzung aus Auftragsvorrat" : "manuell überschrieben"))).join("")}<td><div class="out"><b>${fmtNum(Math.round(c.tot.menge))}</b></div></td></tr>
  <tr class="calc"><th class="lab">Quelle / Anzahl Aufträge</th>${rows.map(r => cell(r, `<div class="out">${r.src === "SAP" ? r.nOrders + " Auftr." : r.src}</div>`)).join("")}<td><div class="out">${c.orders.length || ""}</div></td></tr>
  <tr class="grp"><th colspan="${rows.length + 2}">Geplante Besetzung [FTE]</th></tr>
  ${def.shifts.map(k => `<tr><th class="lab">${SHIFT_LBL[k]} <span class="u">FTE</span></th>${rows.map(r => cell(r, inp(r, k, r.staff[k], r.staff[k + "_m"], `Vorschlag ${fmtNum(r.staff[k + "_d"])} FTE`))).join("")}<td><div class="out">${f1(rows.reduce((t, r) => t + r.staff[k], 0))}</div></td></tr>`).join("")}
  <tr class="calc"><th class="lab">FTE geplant total</th>${rows.map(r => cell(r, `<div class="out">${f1(r.fte)}</div>`)).join("")}<td><div class="out"><b>${f1(c.tot.fte)}</b></div></td></tr>
  <tr class="grp"><th colspan="${rows.length + 2}">Ergebnis</th></tr>
  <tr class="calc"><th class="lab">Kapazität <span class="u">Stk.</span></th>${rows.map(r => cell(r, `<div class="out">${r.kap != null ? fmtNum(Math.round(r.kap)) : "–"}</div>`)).join("")}<td><div class="out"><b>${fmtNum(Math.round(c.tot.kap))}</b></div></td></tr>
  <tr class="calc"><th class="lab">Benötigte FTE</th>${rows.map(r => cell(r, `<div class="out">${f1(r.need)}</div>`)).join("")}<td><div class="out">${f1(c.tot.need)}</div></td></tr>
  <tr class="calc"><th class="lab">Über / Unterdeckung <span class="u">FTE</span></th>${rows.map(r => cell(r, `<div class="out ${r.delta < -0.25 ? "neg" : r.delta > 0.25 ? "pos" : ""}">${r.delta != null ? (r.delta > 0 ? "+" : "") + f1(r.delta) : "–"}</div>`)).join("")}<td><div class="out ${c.tot.delta < 0 ? "neg" : "pos"}">${(c.tot.delta > 0 ? "+" : "") + f1(c.tot.delta)}</div></td></tr>
  <tr class="calc"><th class="lab">Auslastung <span class="u">%</span></th>${rows.map(r => cell(r, `<div class="out"><span class="pill ${r.cls === "bad" ? "bad" : r.cls === "res" ? "info" : "ok"}">${r.ausl != null && isFinite(r.ausl) ? fmtNum(Math.round(r.ausl * 100)) + " %" : "–"}</span></div>`)).join("")}<td><div class="out"><span class="pill ${auslCls(c.tot.ausl) === "bad" ? "bad" : auslCls(c.tot.ausl) === "res" ? "info" : "ok"}">${c.tot.ausl != null ? fmtNum(Math.round(c.tot.ausl * 100)) + " %" : "–"}</span></div></td></tr>
  <tr><th class="lab">Empfehlung</th>${rows.map(r => cell(r, `<div class="tip ${r.cls}">${esc(r.tip || "")}</div>`)).join("")}<td></td></tr>
  </tbody></table></div>
  <div class="chart" style="margin-top:18px"><h3>${def.dept}: Menge vs. Kapazität</h3><div class="legend"><span><i style="background:var(--brand)"></i>Geplante Menge</span><span><i style="background:var(--ink)"></i>Kapazität</span></div>
    ${barLineSvg(rows.map(r => ({ day: r.day, bar: r.menge, line: r.kap })), 150)}</div>
  ${c.orders.length ? `<details class="hist" ${FC.showOrders ? "open" : ""} id="fcOrd"><summary>Aufträge ${def.dept} (${c.orders.length})</summary>${fcOrderTable(c.orders)}</details>` : ""}`;
  $("#fcDetail").querySelectorAll("[data-fk]").forEach(el => el.onchange = () => fcSavePlan(def.dept, el.dataset.fday, el.dataset.fk, el.value));
  $("#fcLeistung").onchange = async e => { await api("POST", "api/forecast/settings", { dept: def.dept, changes: { leistung: parseNum(e.target.value) } }); await fcReload(); toast("Leistung gespeichert"); };
  const od = $("#fcOrd"); if (od) od.ontoggle = () => FC.showOrders = od.open;
}
function fcOrderTable(list) {
  const shown = list.slice(0, 300);
  return `<div class="tbl-wrap" style="margin-top:10px"><table><thead><tr><th>Tag</th><th>Auftrag</th><th>Material</th><th>Kurztext</th><th class="r">Menge</th><th>Disponent</th><th>Arbeitsplatz</th></tr></thead><tbody>
    ${shown.map(o => `<tr><td>${WD[parseIso(o.day).getDay()]} ${fmtDate(o.day).slice(0, 6)}</td><td class="tab">${esc(o.auftrag || "")}</td><td><b>${esc(o.material || "")}</b></td><td>${esc(o.text || "")}</td><td class="n">${fmtNum(Number(o.menge))}</td><td>${esc(o.disponent || "")}</td><td>${esc(o.arbeitsplatz || "")}</td></tr>`).join("")}
    </tbody></table></div>${list.length > shown.length ? `<p class="muted">… und ${list.length - shown.length} weitere</p>` : ""}`;
}
async function fcSavePlan(dept, day, k, v) {
  try { await api("POST", "api/forecast/plan", { dept, day, changes: { [k]: v === "" ? null : parseNum(v) } }); await fcReload(); setSync("gespeichert " + new Date().toLocaleTimeString("de-CH", { hour: "2-digit", minute: "2-digit" })); }
  catch (e) { toast("Nicht gespeichert: " + e.message, true); }
}
async function fcReload() { const y = window.scrollY; FC.data = await api("GET", "api/forecast?monday=" + FC.monday); renderForecast(); window.scrollTo(0, y); }

Object.assign(ZD_MAP, {
  auftrag: ["planauftrag", "fertigungsauftrag", "auftrag", "auftragsnummer", "plauf", "plaufnr", "order"],
  menge: ["menge", "planmenge", "gesamtmenge", "auftragsmenge", "sollmenge", "offene menge", "offenemenge", "quantity"],
  start: ["eckstarttermin", "eckstart", "starttermin", "basisstarttermin", "start"],
  ende: ["eckendtermin", "eckende", "endtermin", "basisendtermin", "ende", "fertigstellung"],
  disponent: ["disponent", "fertigungssteuerer", "dispo", "mrpcontroller", "fertsteuerer"],
  arbeitsplatz: ["arbeitsplatz", "workcenter"], abteilung: ["abteilung", "bereich"], kunde: ["kunde", "kundenname"],
});
async function fcImportFile(file) {
  let t;
  try { t = mapTable(await readTable(file), ["auftrag", "material", "text", "menge", "start", "ende", "disponent", "arbeitsplatz", "abteilung", "kunde"]); }
  catch (e) { toast("Datei konnte nicht gelesen werden: " + e.message, true); return; }
  if (!t.found.includes("menge") || !(t.found.includes("ende") || t.found.includes("start"))) { toast("Spalten Menge und Eck-/Endtermin werden benötigt", true); return; }
  const back = document.createElement("div"); back.className = "modal-back";
  back.innerHTML = `<form class="modal" style="width:min(520px,94vw)"><h2>Planaufträge importieren</h2>
    <p><b>${t.rows.length} Aufträge</b> in „${esc(file.name)}“ erkannt.</p>
    <div class="fld"><label>Produktionstag ist</label><select class="field-in" name="dt">${t.found.includes("ende") ? '<option value="ende">Eckendtermin (Fertigstellung)</option>' : ""}${t.found.includes("start") ? '<option value="start">Eckstarttermin</option>' : ""}</select></div>
    <p class="muted" style="font-size:13px;margin-top:12px">Die Abteilung wird über den Disponenten zugeordnet. Bestehende Aufträge im gleichen Datumsbereich werden ersetzt, manuelle Planwerte bleiben.</p>
    <div style="display:flex;gap:8px;justify-content:flex-end;margin-top:16px"><button type="button" class="btn" data-x>Abbrechen</button><button class="btn primary">Importieren</button></div></form>`;
  document.body.appendChild(back);
  $("[data-x]", back).onclick = () => back.remove();
  $("form", back).onsubmit = async e => {
    e.preventDefault(); const dt = $("select[name=dt]", back).value;
    const rows = t.rows.map(r => ({ ...r, day: excelDate(r[dt] || r.start || r.ende), menge: sapNum(r.menge), material: r.material != null ? String(r.material).trim() : "", auftrag: r.auftrag != null ? String(r.auftrag).trim() : "" }))
      .filter(r => /^\d{4}-\d{2}-\d{2}$/.test(r.day || ""));
    try {
      const res = await api("POST", "api/forecast/import", { rows, filename: file.name });
      back.remove();
      toast(`${res.anzahl} Aufträge importiert (${fmtDate(res.von)}–${fmtDate(res.bis)}) · ${Object.entries(res.pro_abteilung).map(([k, v]) => `${k} ${v}`).join(", ")}`);
      const m = iso(monday(parseIso(res.von))); const target = FC.monday >= m && FC.monday <= res.bis ? FC.monday : m;
      history.replaceState(null, "", "#/m/forecast?monday=" + target); viewForecast();
    } catch (err) { toast("Import fehlgeschlagen: " + err.message, true); }
  };
}


/* ================================================================ Personen & Mail */
async function ensurePeople(force) { if (!S.people || force) S.people = await api("GET", "api/people"); return S.people; }
function resolveAll(values) {
  const hits = [], missing = [];
  values.forEach(v => String(v || "").split(/[,;/+&\n()]| und /).map(t => t.trim()).filter(Boolean).forEach(t => {
    const tl = t.toLowerCase();
    const p = (S.people || []).find(p => p.aktiv !== 0 && ([p.name, p.kuerzel, ...(p.aliases || [])].filter(Boolean).map(x => x.toLowerCase()).includes(tl) || (p.typ === "Person" && (p.name || "").split(" ")[0].toLowerCase() === tl)));
    const p2 = p || (S.people || []).find(p => p.aktiv !== 0 && (p.aliases || []).some(a => a.endsWith("*") && tl.startsWith(a.slice(0, -1).toLowerCase())));
    if (p2) { if (!hits.includes(p2)) hits.push(p2); } else if (!missing.includes(t)) missing.push(t);
  }));
  return { hits, missing };
}
function mailtoFor(m, rec, hits) {
  const rule = S.cfg.notify_rules[m.key] || {}; const d = rec.data || {};
  const title = String(d[rule.title] || "").slice(0, 70);
  const lines = m.fields.filter(f => f.type !== "calc" && d[f.key] != null && d[f.key] !== "").map(f => `${f.label}: ${Array.isArray(d[f.key]) ? d[f.key].join(", ") : (/^\d{4}-\d{2}-\d{2}$/.test(d[f.key]) ? fmtDate(d[f.key]) : d[f.key])}`);
  const url = location.origin && location.origin.startsWith("http") ? `${location.origin}${location.pathname}#/m/${m.key}/${encodeURIComponent(rec.nr)}` : "";
  const body = `Hallo\n\nbitte beachten: ${rec.nr}${title ? " – " + title : ""}\n\n${lines.join("\n")}${url ? "\n\nÖffnen: " + url : ""}\n\nGruss ${S.user || ""}`;
  return `mailto:${hits.filter(p => p.email).map(p => p.email).join(";")}?subject=${encodeURIComponent(`[Shop-Floor] Zuständigkeit: ${rec.nr}${title ? " – " + title : ""}`)}&body=${encodeURIComponent(body).slice(0, 1800)}`;
}
async function viewMail() {
  const [info, ppl] = await Promise.all([api("GET", "api/mail"), ensurePeople(true)]);
  // Unbekannte Zuständige aus offenen Einträgen sammeln
  const rules = S.cfg.notify_rules || {}; const unknown = {};
  for (const [key, rule] of Object.entries(rules)) {
    if (!S.mods[key] || !rule.notify.length) continue;
    const recs = S.cache[key] || await loadRecords(key);
    recs.filter(r => isOpenRec(S.mods[key], r)).forEach(r => resolveAll(rule.notify.map(f => r.data[f])).missing.forEach(t => unknown[t] = (unknown[t] || 0) + 1));
  }
  const unk = Object.entries(unknown).sort((a, b) => b[1] - a[1]).slice(0, 30);
  const st = info.status; const modeTxt = { aus: "nicht eingerichtet – Mails werden nur protokolliert", graph: "Microsoft Graph (Outlook / Exchange Online)", smtp: "SMTP (Exchange-Relay)", demo: "Demo – es werden keine Mails versendet" }[st.modus] || st.modus;
  $("#view").innerHTML = `
  <div class="head"><div class="grow"><h1>Personen &amp; Mail</h1><p class="lede">Wer bei Zuständigkeiten per Mail informiert wird. Einträge wie „Unterhalt/PT“ oder „Samuel, Kujtim“ werden in einzelne Empfänger aufgelöst.</p></div></div>
  <div class="kpis" style="grid-template-columns:repeat(3,minmax(0,1fr))">
    <div class="kpi"><div class="l">Versand</div><div style="font-weight:650;margin-top:4px">${esc(modeTxt)}</div><div class="l">${st.absender ? "Absender: " + esc(st.absender) : ""}</div></div>
    <div class="kpi"><div class="v">${ppl.filter(p => p.email).length}<span class="muted" style="font-size:18px"> / ${ppl.length}</span></div><div class="l">Einträge mit E-Mail-Adresse</div></div>
    <div class="kpi"><div class="l">Testmail</div><div style="display:flex;gap:6px;margin-top:6px"><input class="field-in" id="testTo" placeholder="name@trafag.com"><button class="btn small" id="testBtn">Senden</button></div></div>
  </div>
  ${unk.length ? `<div class="side-card tips"><h3>Zuständige ohne Verzeichniseintrag (in offenen Einträgen)</h3><div class="chips">${unk.map(([t, n]) => `<button class="chip addp" data-add="${esc(t)}">+ ${esc(t)} <span class="muted">${n}×</span></button>`).join("")}</div></div>` : ""}
  <h2>Verzeichnis</h2>
  <div class="tbl-wrap"><table class="ppl"><thead><tr><th>Name / Gruppe</th><th>Kürzel</th><th>E-Mail</th><th>Typ</th><th>weitere Schreibweisen</th><th title="Tägliche Mail mit überfälligen und bald fälligen Punkten">Tages­übersicht</th><th>aktiv</th><th></th></tr></thead><tbody id="pplBody"></tbody></table></div>
  <div style="display:flex;gap:8px;margin-top:12px"><button class="btn" id="pplAdd">+ Zeile</button><button class="btn primary" id="pplSave">Verzeichnis speichern</button></div>
  <h2>Postausgang</h2>
  <div class="tbl-wrap"><table><thead><tr><th>Zeit</th><th>Empfänger</th><th>Betreff</th><th>Status</th><th></th></tr></thead><tbody>
  ${info.outbox.length ? info.outbox.map(o => `<tr><td style="white-space:nowrap">${esc(fmtDate(o.ts))} ${esc(o.ts.slice(11, 16))}</td><td>${o.rcpt.map(r => esc(r.name) + (r.email ? ` <span class="muted">&lt;${esc(r.email)}&gt;</span>` : ' <span style="color:var(--alarm)">⚠</span>')).join(", ")}</td>
    <td>${esc(o.subject)}</td><td><span class="pill ${o.status === "gesendet" ? "ok" : o.status === "wartend" ? "warn" : o.status === "Demo" ? "info" : "bad"}">${esc(o.status)}</span>${o.error ? `<div class="sub muted" style="font-size:12px">${esc(o.error)}</div>` : ""}</td>
    <td style="white-space:nowrap">${window.SF_DEMO ? "" : `<a class="btn small" href="api/mail/preview?id=${o.id}" target="_blank">Ansehen</a>`}${o.status === "Fehler" ? ` <button class="btn small" data-retry="${o.id}">Erneut</button>` : ""}</td></tr>`).join("") : '<tr><td colspan="5" class="empty">Noch keine Mails.</td></tr>'}
  </tbody></table></div>
  <details class="hist"><summary>Einrichtung durch die IT</summary><div style="font-size:13.5px;line-height:1.6">
  <p><b>Variante A – Outlook / Exchange Online über Microsoft Graph (empfohlen):</b> In Entra ID eine App registrieren, Anwendungsberechtigung <code>Mail.Send</code> erteilen (Admin-Zustimmung) und idealerweise per Application Access Policy auf das Postfach <code>shopfloor@…</code> beschränken. Dann in <code>settings.ini</code> unter <code>[mail]</code>: <code>modus = graph</code>, <code>tenant_id</code>, <code>client_id</code>, <code>client_secret</code>, <code>absender</code>.</p>
  <p><b>Variante B – SMTP-Relay:</b> <code>modus = smtp</code>, <code>smtp_host</code> (z. B. interner Exchange-Relay), <code>smtp_port</code>, optional Benutzer/Passwort, <code>absender</code>.</p>
  <p><code>base_url</code> = Adresse des Shop-Floor (z. B. <code>http://shopfloor:8080</code>), damit die Mail einen „Öffnen“-Knopf enthält. Danach Server neu starten.</p>
  <p>Ohne Einrichtung funktioniert jederzeit der Knopf <b>„In Outlook öffnen“</b> im Eintrag: Er öffnet eine vorbereitete Mail im eigenen Outlook.</p></div></details>`;
  const body = $("#pplBody");
  const rowHtml = p => `<tr><td><input class="field-in" data-p="name" value="${esc(p.name || "")}"></td><td><input class="field-in" data-p="kuerzel" value="${esc(p.kuerzel || "")}" style="max-width:90px"></td>
    <td><input class="field-in" data-p="email" type="email" value="${esc(p.email || "")}" placeholder="name@trafag.com"></td>
    <td><select class="field-in" data-p="typ">${["Person", "Gruppe", "Abteilung"].map(t => `<option ${t === (p.typ || "Person") ? "selected" : ""}>${t}</option>`).join("")}</select></td>
    <td><input class="field-in" data-p="aliases" value="${esc((p.aliases || []).join(", "))}" placeholder="z. B. Instandhaltung, UH"></td>
    <td style="text-align:center"><input type="checkbox" data-p="digest" ${p.digest ? "checked" : ""}></td><td style="text-align:center"><input type="checkbox" data-p="aktiv" ${p.aktiv !== 0 ? "checked" : ""}></td>
    <td><button class="icon-btn" data-del title="Zeile entfernen">✕</button></td></tr>`;
  body.innerHTML = ppl.map(rowHtml).join("");
  const bindDel = () => body.querySelectorAll("[data-del]").forEach(b => b.onclick = () => b.closest("tr").remove());
  bindDel();
  $("#pplAdd").onclick = () => { body.insertAdjacentHTML("beforeend", rowHtml({ typ: "Person", aktiv: 1 })); bindDel(); body.lastElementChild.querySelector("input").focus(); };
  document.querySelectorAll("[data-add]").forEach(b => b.onclick = () => { body.insertAdjacentHTML("afterbegin", rowHtml({ name: b.dataset.add, typ: /^[a-z]{2,4}$/.test(b.dataset.add) ? "Person" : "Gruppe", kuerzel: /^[a-z]{2,4}$/.test(b.dataset.add) ? b.dataset.add : "", aktiv: 1 })); bindDel(); b.remove(); body.firstElementChild.querySelector('[data-p="email"]').focus(); });
  $("#pplSave").onclick = async () => {
    const rows = [...body.querySelectorAll("tr")].map(tr => { const o = {}; tr.querySelectorAll("[data-p]").forEach(el => o[el.dataset.p] = el.type === "checkbox" ? el.checked : el.value.trim()); return o; });
    S.people = await api("POST", "api/people", { rows }); toast(`Verzeichnis gespeichert (${S.people.length} Einträge)`); viewMail();
  };
  $("#testBtn").onclick = async () => { const to = $("#testTo").value.trim(); if (!to) return; await api("POST", "api/mail/test", { to }); toast("Testmail in den Postausgang gelegt"); setTimeout(viewMail, 800); };
  document.querySelectorAll("[data-retry]").forEach(b => b.onclick = async () => { await api("POST", "api/mail/retry", { id: Number(b.dataset.retry) }); toast("Wird erneut versucht"); viewMail(); });
}
