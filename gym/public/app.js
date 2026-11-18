'use strict';
// Gym Cockpit – Offline-first: Zustand im localStorage, Abgleich mit api.php.

const TYPEN = [
  ['beinpresse', 'Beinpresse', '🦵'], ['beinstrecker', 'Beinstrecker', '🦵'], ['beinbeuger', 'Beinbeuger', '🦵'],
  ['wade', 'Wadenmaschine', '🦶'], ['adduktor', 'Adduktor / Abduktor', '🦵'],
  ['brustpresse', 'Brustpresse', '💪'], ['butterfly', 'Butterfly', '🦋'], ['schulterpresse', 'Schulterpresse', '🏋️'],
  ['latzug', 'Latzug', '⬇️'], ['rudern', 'Rudern', '🚣'], ['ruecken', 'Rückenstrecker', '🧍'],
  ['bauch', 'Bauchmaschine', '🎯'], ['bizeps', 'Bizeps', '💪'], ['trizeps', 'Trizeps', '💪'],
  ['kabelzug', 'Kabelzug', '🔗'], ['multipresse', 'Multipresse', '🏗️'], ['freihantel', 'Freihantel', '🏋️'],
  ['cardio', 'Cardio', '🚴'], ['sonst', 'Sonstiges', '⚙️'],
];
const TYP = Object.fromEntries(TYPEN.map(([k, n, i]) => [k, { name: n, icon: i }]));

const LS_STATE = 'gym_state_v1';
const LS_TOKEN = 'gym_token';
const LS_BASE = 'gym_base';
const LS_DIRTY = 'gym_dirty';

const $ = (s, el = document) => el.querySelector(s);
const $$ = (s, el = document) => [...el.querySelectorAll(s)];
const jetzt = () => Date.now();
const uid = () => (crypto.randomUUID ? crypto.randomUUID() : Math.random().toString(36).slice(2) + jetzt().toString(36));
const esc = (s) => String(s ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
const num = (v, d = 0) => { const n = parseFloat(String(v).replace(',', '.')); return Number.isFinite(n) ? n : d; };
const kg = (n) => (Math.round(n * 10) / 10).toLocaleString('de-CH');

function lsGet(k) { try { return localStorage.getItem(k); } catch { return null; } }
function lsSet(k, v) { try { localStorage.setItem(k, v); } catch { /* privat */ } }

function leererZustand() {
  return {
    v: 1,
    profile: { name: '', weight: null, mod: 0 },
    settings: { up: 2.5, holdTop: 0, down: 2.5, holdBottom: 0, sound: true, vibrate: true, mod: 0 },
    machines: [],
    sessions: [],
  };
}

let S = leererZustand();
let token = lsGet(LS_TOKEN);
let base = num(lsGet(LS_BASE), 0);
let dirty = lsGet(LS_DIRTY) === '1';

try { const roh = lsGet(LS_STATE); if (roh) S = Object.assign(leererZustand(), JSON.parse(roh)); } catch { /* defekt */ }

// Token aus Link (#k=…) übernehmen: anderes Gerät koppeln
(function linkToken() {
  const m = location.hash.match(/k=([a-f0-9]{32})/);
  if (!m) return;
  if (m[1] !== token) {
    token = m[1]; base = 0; dirty = false; S = leererZustand();
    lsSet(LS_TOKEN, token); lsSet(LS_BASE, '0'); lsSet(LS_DIRTY, '0'); lsSet(LS_STATE, JSON.stringify(S));
  }
  history.replaceState(null, '', location.pathname);
})();

function speichern() {
  dirty = true;
  lsSet(LS_STATE, JSON.stringify(S));
  lsSet(LS_DIRTY, '1');
  planeSync();
}

// ---------- Abgleich ----------
function mergeListe(a = [], b = [], tief) {
  const m = new Map();
  for (const x of a) m.set(x.id, x);
  for (const y of b) {
    const x = m.get(y.id);
    if (!x) m.set(y.id, y);
    else if (tief) m.set(y.id, tief(x, y));
    else m.set(y.id, (y.mod || 0) > (x.mod || 0) ? y : x);
  }
  return [...m.values()];
}
function mergeSession(x, y) {
  const neuer = (y.mod || 0) > (x.mod || 0) ? y : x;
  return { ...neuer, entries: mergeListe(x.entries, y.entries) };
}
function merge(lokal, server) {
  const r = leererZustand();
  r.profile = (server.profile?.mod || 0) > (lokal.profile?.mod || 0) ? server.profile : lokal.profile;
  r.settings = (server.settings?.mod || 0) > (lokal.settings?.mod || 0) ? server.settings : lokal.settings;
  r.machines = mergeListe(lokal.machines, server.machines);
  r.sessions = mergeListe(lokal.sessions, server.sessions, mergeSession);
  return r;
}

let syncTimer = null;
let syncLaeuft = false;
function planeSync(ms = 1200) { clearTimeout(syncTimer); syncTimer = setTimeout(sync, ms); }
function syncAnzeige(ok) {
  const el = $('#sync');
  el.className = 'sync ' + (ok ? 'ok' : 'off');
  el.title = ok ? 'Gespeichert auf dem Server' : 'Offline – wird später abgeglichen';
}

async function api(a, opt = {}, q = '') {
  const r = await fetch(`api.php?a=${a}${q}`, { cache: 'no-store', ...opt });
  const j = await r.json().catch(() => ({}));
  return { status: r.status, j };
}

async function sync() {
  if (syncLaeuft) return planeSync(800);
  if (!token && !S.profile.name) return;
  syncLaeuft = true;
  try {
    if (!token) {
      const { status, j } = await api('create', { method: 'POST', body: JSON.stringify(S) });
      if (status !== 200) throw new Error('create ' + status);
      token = j.token; base = j.updated; dirty = false;
      lsSet(LS_TOKEN, token); lsSet(LS_BASE, String(base)); lsSet(LS_DIRTY, '0');
      zeigeLink();
    } else if (dirty) {
      for (let i = 0; i < 3; i++) {
        const { status, j } = await api('put', { method: 'POST', body: JSON.stringify(S) }, `&k=${token}&base=${base}`);
        if (status === 200) { base = j.updated; dirty = false; break; }
        if (status === 409) { S = merge(S, j.data); base = j.updated; continue; }
        throw new Error('put ' + status);
      }
      lsSet(LS_BASE, String(base)); lsSet(LS_DIRTY, dirty ? '1' : '0'); lsSet(LS_STATE, JSON.stringify(S));
      render();
    } else {
      const { status, j } = await api('get', {}, `&k=${token}`);
      if (status === 200 && j.updated !== base) {
        S = merge(leererZustand(), j.data); base = j.updated;
        lsSet(LS_BASE, String(base)); lsSet(LS_STATE, JSON.stringify(S));
        render();
      } else if (status !== 200) throw new Error('get ' + status);
    }
    syncAnzeige(true);
  } catch (e) {
    console.warn('sync', e);
    syncAnzeige(false);
    if (dirty) planeSync(15000);
  } finally {
    syncLaeuft = false;
  }
}

// ---------- Abfragen ----------
const lebendig = (l) => l.filter((x) => !x.deleted);
const machines = () => lebendig(S.machines).sort((a, b) => (a.order ?? a.created) - (b.order ?? b.created));
const sessions = () => lebendig(S.sessions).sort((a, b) => b.start - a.start);
const aktiveSession = () => sessions().find((s) => !s.end) || null;
const machineById = (id) => S.machines.find((m) => m.id === id);

function letzteSaetze(machineId) {
  for (const s of sessions()) {
    const e = lebendig(s.entries || []).filter((x) => x.machineId === machineId && x.sets?.length);
    if (e.length) return { session: s, sets: e.flatMap((x) => x.sets) };
  }
  return null;
}

// ---------- Zeit ----------
function dauer(ms, sek = true) {
  const t = Math.max(0, Math.floor(ms / 1000));
  const h = Math.floor(t / 3600), m = Math.floor((t % 3600) / 60), s = t % 60;
  const p = (n) => String(n).padStart(2, '0');
  return sek ? `${p(h)}:${p(m)}:${p(s)}` : (h ? `${h} h ${m} min` : `${m} min`);
}
const datum = (ts) => new Date(ts).toLocaleDateString('de-CH', { weekday: 'short', day: '2-digit', month: '2-digit', year: 'numeric' });
const uhr = (ts) => new Date(ts).toLocaleTimeString('de-CH', { hour: '2-digit', minute: '2-digit' });

// ---------- Session ----------
function startSession() {
  const s = { id: uid(), start: jetzt(), end: null, bodyWeight: S.profile.weight ?? null, entries: [], mod: jetzt() };
  S.sessions.push(s);
  speichern(); render(); wakeLock(true);
  toast('Training gestartet');
  return s;
}
function endSession() {
  const s = aktiveSession();
  if (!s) return;
  s.end = jetzt(); s.mod = jetzt();
  speichern(); render(); wakeLock(false);
  toast(`Training beendet · ${dauer(s.end - s.start, false)}`);
}

// ---------- Rendering ----------
function render() {
  $('#hello').textContent = S.profile.name ? `Hallo ${S.profile.name}` : 'Gym Cockpit';
  renderHero(); renderTrainList(); renderMachines(); renderHistory(); renderProfil();
  tick();
}

function renderHero() {
  const s = aktiveSession();
  const el = $('#hero');
  if (s) {
    const e = lebendig(s.entries);
    const saetze = e.reduce((n, x) => n + (x.sets?.length || 0), 0);
    const vol = e.reduce((n, x) => n + (x.sets || []).reduce((m, y) => m + y.weight * y.reps, 0), 0);
    el.innerHTML = `
      <h1>Training läuft</h1>
      <p>Seit ${uhr(s.start)} Uhr im Gym</p>
      <div class="kpis">
        <div class="kpi"><b id="hero-time">${dauer(jetzt() - s.start)}</b><span>Zeit</span></div>
        <div class="kpi"><b>${saetze}</b><span>Sätze</span></div>
        <div class="kpi"><b>${kg(vol)}</b><span>kg bewegt</span></div>
      </div>
      <button class="btn wide" id="end-session">Training beenden</button>`;
    $('#end-session').onclick = () => { if (confirm('Training beenden?')) endSession(); };
  } else {
    const letzte = sessions()[0];
    el.innerHTML = `
      <h1>Bereit?</h1>
      <p>${letzte ? `Letztes Training: ${datum(letzte.start)}` : 'Lege zuerst deine Geräte an, dann geht es los.'}</p>
      <button class="btn primary wide big" id="start-session">Training starten</button>`;
    $('#start-session').onclick = () => startSession();
  }
}

function zusammenfassung(sets) {
  if (!sets?.length) return '';
  return sets.map((x) => `${x.reps}×${kg(x.weight)}`).join(' · ');
}

function renderTrainList() {
  const el = $('#train-list');
  const ms = machines();
  if (!ms.length) {
    el.innerHTML = `<div class="empty">Noch keine Geräte.<br><br><button class="btn primary" id="first-machine">+ Erstes Gerät anlegen</button></div>`;
    $('#first-machine').onclick = () => oeffneMachine();
    return;
  }
  const s = aktiveSession();
  el.innerHTML = ms.map((m) => {
    const heute = s ? lebendig(s.entries).filter((e) => e.machineId === m.id).flatMap((e) => e.sets || []) : [];
    const last = letzteSaetze(m.id);
    const sub = [kg(m.weight || 0) + ' kg', m.note].filter(Boolean).map(esc).join(' · ');
    const info = heute.length
      ? `<div class="done">✓ ${heute.length}/${m.targetSets || 3} Sätze · ${esc(zusammenfassung(heute))}</div>`
      : last ? `<div class="sub">Zuletzt ${datum(last.session.start).slice(4)}: ${esc(zusammenfassung(last.sets))}</div>` : '';
    return `<div class="item">
      <div class="badge">${TYP[m.type]?.icon || '⚙️'}</div>
      <div class="meta"><div class="name">${esc(m.name)}</div><div class="sub">${sub}</div>${info}</div>
      <button class="btn primary go" data-start="${m.id}">Start</button>
    </div>`;
  }).join('');
  $$('[data-start]', el).forEach((b) => (b.onclick = () => starteCockpit(b.dataset.start)));
}

function renderMachines() {
  const el = $('#machine-list');
  const ms = machines();
  el.innerHTML = ms.length ? ms.map((m) => `<div class="item" data-edit="${m.id}" role="button">
      <div class="badge">${TYP[m.type]?.icon || '⚙️'}</div>
      <div class="meta"><div class="name">${esc(m.name)}</div>
        <div class="sub">${esc(TYP[m.type]?.name || 'Sonstiges')} · ${kg(m.weight || 0)} kg · ${m.targetSets || 3} Sätze${m.note ? ' · ' + esc(m.note) : ''}</div></div>
      <span class="muted">›</span></div>`).join('')
    : `<div class="empty">Füge die Geräte aus deinem Studio hinzu: Name, Art, Gewicht und die Einstellung am Sitz.</div>`;
  $$('[data-edit]', el).forEach((b) => (b.onclick = () => oeffneMachine(b.dataset.edit)));
}

function sparkline(werte) {
  if (werte.length < 2) return '';
  const w = 300, h = 64, p = 6;
  const min = Math.min(...werte), max = Math.max(...werte), sp = max - min || 1;
  const pts = werte.map((v, i) => [p + (i * (w - 2 * p)) / (werte.length - 1), h - p - ((v - min) / sp) * (h - 2 * p)]);
  const d = pts.map((q, i) => `${i ? 'L' : 'M'}${q[0].toFixed(1)},${q[1].toFixed(1)}`).join(' ');
  const last = pts[pts.length - 1];
  return `<svg viewBox="0 0 ${w} ${h}" preserveAspectRatio="none" aria-hidden="true">
    <path d="${d} L${last[0]},${h} L${pts[0][0]},${h} Z" fill="rgba(182,255,59,0.12)"/>
    <path d="${d}" fill="none" stroke="#b6ff3b" stroke-width="2.5" stroke-linejoin="round" vector-effect="non-scaling-stroke"/>
    <circle cx="${last[0]}" cy="${last[1]}" r="4" fill="#b6ff3b"/></svg>`;
}

function renderHistory() {
  const st = $('#stats');
  const ss = sessions().slice().reverse();
  const karten = machines().map((m) => {
    const proTag = ss.map((s) => lebendig(s.entries).filter((e) => e.machineId === m.id).flatMap((e) => e.sets || []))
      .filter((x) => x.length);
    if (!proTag.length) return '';
    const maxGew = proTag.map((x) => Math.max(...x.map((y) => y.weight)));
    const best = Math.max(...maxGew);
    return `<div class="card stat"><div class="head"><b>${esc(m.name)}</b><span class="best">${kg(best)} kg</span></div>
      <div class="muted" style="margin:2px 0 0">${proTag.length} Trainingstage · zuletzt ${esc(zusammenfassung(proTag[proTag.length - 1]))}</div>
      ${sparkline(maxGew)}</div>`;
  }).join('');
  st.innerHTML = karten || '<div class="empty">Sobald du Sätze einträgst, siehst du hier deinen Fortschritt.</div>';

  const h = $('#history');
  const liste = sessions();
  h.innerHTML = liste.length ? liste.map((s) => {
    const zeilen = lebendig(s.entries).filter((e) => e.sets?.length).map((e) => {
      const m = machineById(e.machineId);
      return `<li><span>${uhr(e.start)} · ${esc(m?.name || 'Gerät')}</span><span>${e.sets.length} Sätze · ${esc(zusammenfassung(e.sets))}</span></li>`;
    }).join('');
    const d = (s.end || jetzt()) - s.start;
    return `<div class="card day"><div class="head"><span class="date">${datum(s.start)}</span>
      <span class="dur">${s.end ? dauer(d, false) : 'läuft'} · ${uhr(s.start)}–${s.end ? uhr(s.end) : '…'}</span></div>
      ${s.bodyWeight ? `<div class="muted" style="margin:4px 0 0">Körpergewicht ${kg(s.bodyWeight)} kg</div>` : ''}
      <ul>${zeilen || '<li><span class="muted">Keine Sätze</span></li>'}</ul>
      ${s.end ? `<button class="btn small danger del" data-del="${s.id}">Tag löschen</button>` : ''}</div>`;
  }).join('') : '<div class="empty">Noch keine Trainingstage.</div>';
  $$('[data-del]', h).forEach((b) => (b.onclick = () => {
    if (!confirm('Diesen Trainingstag löschen?')) return;
    const s = S.sessions.find((x) => x.id === b.dataset.del);
    s.deleted = true; s.mod = jetzt(); speichern(); render();
  }));
}

function renderProfil() {
  const f = $('#profile-form');
  if (document.activeElement?.form !== f) {
    f.name.value = S.profile.name || '';
    f.weight.value = S.profile.weight ?? '';
  }
  const t = $('#tempo-form');
  if (document.activeElement?.form !== t) {
    for (const k of ['up', 'holdTop', 'down', 'holdBottom']) t[k].value = S.settings[k];
    t.sound.checked = !!S.settings.sound;
    t.vibrate.checked = !!S.settings.vibrate;
  }
  zeigeLink();
}

function zeigeLink() {
  $('#share-link').textContent = token ? `${location.origin}/#k=${token}` : 'Wird nach dem ersten Speichern erstellt …';
}

// ---------- Uhr ----------
function tick() {
  const s = aktiveSession();
  $('#gymclock').hidden = !s;
  if (s) {
    const t = dauer(jetzt() - s.start);
    $('#gymtime').textContent = t;
    const h = $('#hero-time'); if (h) h.textContent = t;
  }
  if (CK.offen) $('#ck-clock').textContent = s ? dauer(jetzt() - s.start).replace(/^00:/, '') : '';
}
setInterval(tick, 1000);

// ---------- Geräte-Dialog ----------
const dlgM = $('#dlg-machine');
$('select[name=type]', dlgM).innerHTML = TYPEN.map(([k, n, i]) => `<option value="${k}">${i} ${n}</option>`).join('');
let editId = null;
function oeffneMachine(id) {
  editId = id || null;
  const m = id ? machineById(id) : null;
  const f = $('#machine-form');
  $('#machine-dlg-title').textContent = m ? 'Gerät bearbeiten' : 'Neues Gerät';
  f.name.value = m?.name || '';
  f.type.value = m?.type || 'beinpresse';
  f.weight.value = m?.weight ?? '';
  f.targetSets.value = m?.targetSets ?? 3;
  f.note.value = m?.note || '';
  $('#machine-delete').hidden = !m;
  dlgM.showModal();
}
$('#add-machine').onclick = () => oeffneMachine();
dlgM.addEventListener('close', () => {
  const f = $('#machine-form');
  const v = dlgM.returnValue;
  if (v === 'delete' && editId) {
    if (!confirm('Gerät löschen? Die Trainingsdaten bleiben erhalten.')) return;
    const m = machineById(editId); m.deleted = true; m.mod = jetzt();
  } else if (v === 'save') {
    const daten = {
      name: f.name.value.trim(), type: f.type.value, weight: num(f.weight.value),
      targetSets: Math.max(1, Math.round(num(f.targetSets.value, 3))), note: f.note.value.trim(), mod: jetzt(),
    };
    if (!daten.name) return;
    if (editId) Object.assign(machineById(editId), daten);
    else S.machines.push({ id: uid(), created: jetzt(), ...daten });
  } else return;
  speichern(); render();
});

// ---------- Profil ----------
$('#profile-form').onsubmit = (e) => {
  e.preventDefault();
  const f = e.target;
  S.profile = { name: f.name.value.trim(), weight: f.weight.value ? num(f.weight.value) : null, mod: jetzt() };
  speichern(); render(); toast('Profil gespeichert');
};
$('#tempo-form').onsubmit = (e) => {
  e.preventDefault();
  const f = e.target;
  S.settings = {
    up: Math.max(0.5, num(f.up.value, 2.5)), holdTop: Math.max(0, num(f.holdTop.value)),
    down: Math.max(0.5, num(f.down.value, 2.5)), holdBottom: Math.max(0, num(f.holdBottom.value)),
    sound: f.sound.checked, vibrate: f.vibrate.checked, mod: jetzt(),
  };
  speichern(); render(); toast('Tempo gespeichert');
};
$('#copy-link').onclick = async () => {
  if (!token) return toast('Noch kein Link – kurz warten');
  const link = `${location.origin}/#k=${token}`;
  try {
    if (navigator.share) await navigator.share({ title: 'Mein Gym Cockpit', url: link });
    else { await navigator.clipboard.writeText(link); toast('Link kopiert'); }
  } catch { /* abgebrochen */ }
};

// ---------- Tabs ----------
$$('#tabs button').forEach((b) => (b.onclick = () => {
  $$('#tabs button').forEach((x) => x.classList.toggle('on', x === b));
  $$('.view').forEach((v) => (v.hidden = v.id !== 'view-' + b.dataset.view));
  window.scrollTo({ top: 0 });
}));

// ---------- Toast ----------
let toastT;
function toast(t) {
  const el = $('#toast'); el.textContent = t; el.hidden = false;
  clearTimeout(toastT); toastT = setTimeout(() => (el.hidden = true), 2200);
}

// ---------- Wake Lock ----------
let lock = null;
async function wakeLock(an) {
  try {
    if (an && 'wakeLock' in navigator && !lock) { lock = await navigator.wakeLock.request('screen'); lock.addEventListener('release', () => (lock = null)); }
    if (!an && lock && !aktiveSession() && !CK.offen) { await lock.release(); lock = null; }
  } catch { /* nicht erlaubt */ }
}

// ---------- Ton ----------
let ac = null;
function piep(freq = 880, dauerMs = 90) {
  if (!S.settings.sound) return;
  try {
    ac = ac || new (window.AudioContext || window.webkitAudioContext)();
    const o = ac.createOscillator(), g = ac.createGain();
    o.frequency.value = freq; o.type = 'sine';
    g.gain.setValueAtTime(0.0001, ac.currentTime);
    g.gain.exponentialRampToValueAtTime(0.25, ac.currentTime + 0.01);
    g.gain.exponentialRampToValueAtTime(0.0001, ac.currentTime + dauerMs / 1000);
    o.connect(g).connect(ac.destination); o.start(); o.stop(ac.currentTime + dauerMs / 1000 + 0.02);
  } catch { /* kein Ton */ }
}
function vib(ms) { if (S.settings.vibrate && navigator.vibrate) navigator.vibrate(ms); }

// ---------- Cockpit ----------
const CK = { offen: false, entry: null, session: null, machine: null, t0: 0, pausiert: false, pauseAb: 0, reps: 0, phase: -1, raf: 0 };

function phasen() {
  const st = S.settings;
  return [
    { key: 'hin', label: 'HIN', d: st.up * 1000, von: 0, nach: 1, cls: '' },
    { key: 'oben', label: 'HALTEN', d: st.holdTop * 1000, von: 1, nach: 1, cls: 'hold' },
    { key: 'zurueck', label: 'ZURÜCK', d: st.down * 1000, von: 1, nach: 0, cls: 'back' },
    { key: 'unten', label: 'HALTEN', d: st.holdBottom * 1000, von: 0, nach: 0, cls: 'hold' },
  ].filter((p) => p.d > 0);
}

function starteCockpit(machineId) {
  const s = aktiveSession() || startSession();
  const m = machineById(machineId);
  const entry = { id: uid(), machineId, start: jetzt(), sets: [], mod: jetzt() };
  s.entries.push(entry); s.mod = jetzt();
  speichern();
  Object.assign(CK, { offen: true, entry, session: s, machine: m, reps: 0, phase: -1, pausiert: false });
  $('#cockpit').hidden = false;
  document.body.style.overflow = 'hidden';
  wakeLock(true);
  ckKopf();
  ckStart();
}

function ckKopf() {
  const satz = CK.entry.sets.length + 1;
  $('#ck-machine').textContent = CK.machine.name;
  $('#ck-sub').textContent = `Satz ${satz} von ${CK.machine.targetSets || 3} · ${kg(CK.machine.weight || 0)} kg${CK.machine.note ? ' · ' + CK.machine.note : ''}`;
  $('#ck-sets').innerHTML = CK.entry.sets.map((x, i) => `<span>${i + 1}: ${x.reps}×${kg(x.weight)}</span>`).join('');
  $('#ck-reps').textContent = CK.reps;
  $('#ck-pause').textContent = CK.pausiert ? 'Weiter' : 'Pause';
}

function ckStart() {
  CK.reps = 0; CK.phase = -1; CK.pausiert = false;
  CK.t0 = performance.now();
  cancelAnimationFrame(CK.raf);
  CK.raf = requestAnimationFrame(ckFrame);
  ckKopf();
}

function ckFrame(now) {
  if (!CK.offen) return;
  const ph = phasen();
  const zyklus = ph.reduce((n, p) => n + p.d, 0);
  const t = CK.pausiert ? CK.pauseAb - CK.t0 : now - CK.t0;
  const reps = Math.floor(t / zyklus);
  let r = t % zyklus, i = 0;
  while (r >= ph[i].d) { r -= ph[i].d; i++; }
  const p = ph[i];
  const k = r / p.d;
  const ease = k < 0.5 ? 2 * k * k : 1 - Math.pow(-2 * k + 2, 2) / 2;
  const pos = p.von + (p.nach - p.von) * ease;

  if (!CK.pausiert && (i !== CK.phase || reps !== CK.reps)) {
    if (CK.phase !== -1) {
      if (p.key === 'zurueck') { piep(660); vib(60); }
      else if (p.key === 'hin') { piep(990); vib(120); }
    }
    CK.phase = i;
    if (reps !== CK.reps) { CK.reps = reps; $('#ck-reps').textContent = reps; }
    const ck = $('#cockpit');
    ck.classList.toggle('back', p.cls === 'back');
    ck.classList.toggle('hold', p.cls === 'hold');
    $('#ck-phase').textContent = p.label;
  }

  const track = $('#ck-track'), bar = $('#ck-bar');
  const weg = track.clientWidth - bar.offsetWidth;
  bar.style.transform = `translateX(${(pos * weg).toFixed(1)}px)`;
  $('#ck-fill').style.width = `${(pos * 100).toFixed(2)}%`;
  CK.raf = requestAnimationFrame(ckFrame);
}

$('#ck-pause').onclick = () => {
  const now = performance.now();
  if (CK.pausiert) { CK.t0 += now - CK.pauseAb; CK.pausiert = false; }
  else { CK.pauseAb = now; CK.pausiert = true; $('#ck-phase').textContent = 'PAUSE'; }
  $('#cockpit').classList.toggle('paused', CK.pausiert);
  ckKopf();
};

const dlgSet = $('#dlg-set');
$('#ck-done').onclick = () => {
  if (!CK.pausiert) $('#ck-pause').click();
  const f = $('#set-form');
  f.weight.value = CK.machine.weight || 0;
  f.reps.value = CK.reps;
  dlgSet.showModal();
};
$$('[data-step]', dlgSet).forEach((b) => (b.onclick = () => {
  const [feld, d] = b.dataset.step.split(':');
  const inp = $('#set-form')[feld];
  inp.value = Math.max(0, Math.round((num(inp.value) + num(d)) * 10) / 10);
}));
dlgSet.addEventListener('close', () => {
  if (dlgSet.returnValue !== 'save') { ckStart(); $('#cockpit').classList.remove('paused'); return; }
  const f = $('#set-form');
  const set = { t: jetzt(), weight: num(f.weight.value), reps: Math.round(num(f.reps.value)) };
  CK.entry.sets.push(set); CK.entry.mod = jetzt(); CK.session.mod = jetzt();
  if (set.weight !== CK.machine.weight) { CK.machine.weight = set.weight; CK.machine.mod = jetzt(); }
  speichern();
  piep(1200, 160);
  toast(`Satz ${CK.entry.sets.length} gespeichert`);
  $('#cockpit').classList.remove('paused');
  if (CK.entry.sets.length >= (CK.machine.targetSets || 3)) { ckSchliessen(); return; }
  ckStart();
});

function ckSchliessen() {
  CK.offen = false;
  cancelAnimationFrame(CK.raf);
  if (CK.entry && !CK.entry.sets.length) { CK.entry.deleted = true; CK.entry.mod = jetzt(); speichern(); }
  $('#cockpit').hidden = true;
  $('#cockpit').className = 'cockpit';
  document.body.style.overflow = '';
  render();
}
$('#ck-finish').onclick = ckSchliessen;
$('#ck-close').onclick = () => {
  if (CK.entry.sets.length || confirm('Ohne gespeicherten Satz schliessen?')) ckSchliessen();
};

// ---------- Start ----------
const dlgW = $('#dlg-welcome');
dlgW.addEventListener('cancel', (e) => e.preventDefault());
dlgW.addEventListener('close', () => {
  const f = $('#welcome-form');
  const name = f.name.value.trim();
  if (!name) return dlgW.showModal();
  S.profile = { name, weight: f.weight.value ? num(f.weight.value) : null, mod: jetzt() };
  speichern(); render(); sync();
});

document.addEventListener('visibilitychange', () => {
  if (document.visibilityState === 'visible') { sync(); if (aktiveSession() || CK.offen) { lock = null; wakeLock(true); } }
});
window.addEventListener('online', () => sync());
setInterval(() => { if (document.visibilityState === 'visible') sync(); }, 30000);

if ('serviceWorker' in navigator) navigator.serviceWorker.register('sw.js').catch(() => {});

render();
(async () => {
  if (token && !S.profile.name) await sync();
  if (!S.profile.name) dlgW.showModal();
  else sync();
  if (aktiveSession()) wakeLock(true);
})();
