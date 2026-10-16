// Funkerbier – Terminsuche. Vanilla JS, kein Build. Quelle Ai/funkerbier/.
import { Dmd } from './dmd.js?v=1';
import { Funk, BAENDER } from './audio.js?v=2';
import { hintergrund, laufschrift, instrumente } from './fx.js?v=1';

const $ = (s) => document.querySelector(s);
const app = $('#app');
const funk = new Funk();
const dmd = new Dmd($('#dmd'), $('#dmd-text'));
const bg = hintergrund($('#hintergrund'));
laufschrift($('#scroller'), '*** FUNKERBIER QRV ***   CQ CQ CQ DE BIER.INGOK.CH   ***   TERMIN VORSCHLAGEN, LINK VERSCHICKEN, ALLE TRAGEN SICH EIN   ***   ANTENNE AUSRICHTEN, BIER KALT STELLEN   ***   GREETINGS TO ALL OMS, YLS AND XYLS ON THE BANDS   ***   599 FROM THE SHACK   ***   NO QRM, NO QRN, JUST QRV   ***   73 ES GL   ***        ');
instrumente($('#smeter'), $('#wasserfall'), () => ({ rauschen: funk.rauschAnalyse, master: funk.analyse }));

// ---- kleine Helfer --------------------------------------------------------
function h(tag, props, ...kinder) {
  const el = document.createElement(tag);
  for (const [k, v] of Object.entries(props || {})) {
    if (v == null || v === false) continue;
    if (k === 'class') el.className = v;
    else if (k.startsWith('on')) el.addEventListener(k.slice(2), v);
    else if (k === 'value') el.value = v;
    else el.setAttribute(k, v === true ? '' : v);
  }
  for (const kind of kinder.flat(Infinity)) {
    if (kind == null || kind === false) continue;
    el.append(kind instanceof Node ? kind : String(kind));
  }
  return el;
}

function fenster(titel, ...inhalt) {
  return h('section', { class: 'fenster' },
    h('div', { class: 'fenster-titel' }, h('span', { class: 'gadget' }), h('h2', {}, titel), h('span', { class: 'gadget tiefe' })),
    h('div', { class: 'fenster-inhalt' }, ...inhalt));
}

const speicher = {
  lesen(k, d) { try { const v = localStorage.getItem('bier:' + k); return v == null ? d : JSON.parse(v); } catch { return d; } },
  schreiben(k, v) { try { localStorage.setItem('bier:' + k, JSON.stringify(v)); } catch { /* privat */ } },
};

async function api(daten, methode = 'POST') {
  let url = 'api.php', opt = { method: methode, headers: {} };
  if (methode === 'GET') url += '?' + new URLSearchParams(Object.entries(daten).filter(([, v]) => v != null));
  else { opt.headers['Content-Type'] = 'application/json'; opt.body = JSON.stringify(daten); }
  let r;
  try { r = await fetch(url, opt); } catch { throw new Error('Keine Verbindung zum Shack (Netz weg?).'); }
  let j = {};
  try { j = await r.json(); } catch { /* leer */ }
  if (!r.ok) { const e = new Error(j.error || `Störung ${r.status}`); e.status = r.status; throw e; }
  return j;
}

const TAGE = ['SO', 'MO', 'DI', 'MI', 'DO', 'FR', 'SA'];
function zerlegen(s) {
  const [d, z] = s.split('T');
  const [y, m, t] = d.split('-').map(Number);
  return { tag: TAGE[new Date(y, m - 1, t).getDay()], datum: `${String(t).padStart(2, '0')}.${String(m).padStart(2, '0')}.`, jahr: y, zeit: z };
}
const kurz = (s) => { const z = zerlegen(s); return `${z.tag} ${z.datum} ${z.zeit}`; };

// ---- Punkte ----------------------------------------------------------------
let punkte = speicher.lesen('punkte', 0);
function punkteDazu(n) {
  punkte += n;
  speicher.schreiben('punkte', punkte);
  const el = $('#punkte');
  if (el) el.textContent = String(punkte).padStart(7, '0');
}
const punkteText = () => 'SCORE ' + String(punkte).padStart(7, '0');

// ---- Audio-Bedienung -------------------------------------------------------
const pegel = speicher.lesen('pegel', { musik: 60, rauschen: 35, sfx: 70 });
for (const k of ['musik', 'rauschen', 'sfx']) {
  const r = $('#regler-' + k);
  r.value = pegel[k];
  funk.pegel[k] = pegel[k] / 100;
  r.addEventListener('input', () => { pegel[k] = Number(r.value); speicher.schreiben('pegel', pegel); funk.setze(k, pegel[k] / 100); });
}
const power = $('#power');
function powerAnzeigen() { power.setAttribute('aria-pressed', String(funk.an)); power.textContent = funk.an ? '⏻ TON AN' : '⏻ TON AUS'; }
power.addEventListener('click', async () => {
  if (funk.an) funk.pause(); else await funk.starten();
  speicher.schreiben('ton', funk.an);
  powerAnzeigen();
});
let bandIndex = speicher.lesen('band70', 5) % BAENDER.length;
function vfoAnzeigen() {
  const b = BAENDER[bandIndex];
  const khz = Math.round((b.qrg % 1) * 1000);
  $('.vfo-band').textContent = b.band;
  $('.vfo-mode').textContent = b.mode;
  $('.vfo-qrg').textContent = `${Math.floor(b.qrg)}.${String(khz).padStart(3, '0')} MHz`;
}
$('#vfo').addEventListener('click', () => {
  bandIndex = (bandIndex + 1) % BAENDER.length;
  speicher.schreiben('band70', bandIndex);
  funk.bandWechseln(BAENDER[bandIndex]);
  vfoAnzeigen();
  dmd.zeige('QSY ' + BAENDER[bandIndex].band, { gross: true, dauer: 1000, sofort: true });
});
funk.band = BAENDER[bandIndex];
vfoAnzeigen();
setInterval(vfoAnzeigen, 4000);

let schlag = 0;
funk.beiZeile = ({ ev, row }) => {
  if (ev.includes(0) && row % 8 === 0) { bg.takt(); if (++schlag % 2 === 0) dmd.pulsieren(); }
};

// TILT: fuenfmal schnell aufs Display hauen
let tiltKlicks = [];
$('#dmd').addEventListener('click', () => {
  const t = Date.now();
  tiltKlicks = tiltKlicks.filter((k) => t - k < 1500).concat(t);
  funk.bumper(0);
  if (tiltKlicks.length >= 5) {
    tiltKlicks = [];
    funk.tilt();
    dmd.zeige('TILT', { art: 'blink', gross: true, dauer: 2200, sofort: true });
    $('.backbox').classList.remove('wackeln');
    void $('.backbox').offsetWidth;
    $('.backbox').classList.add('wackeln');
    punkte = 0; punkteDazu(0);
  }
});

// ---- Boot ------------------------------------------------------------------
let gebootet = false;
async function booten(mitTon) {
  if (gebootet) return;
  gebootet = true;
  const boot = $('#boot');
  const log = $('#boot-log');
  $('.diskette').classList.add('rein');
  if (mitTon) { await funk.starten(); funk.diskette(); speicher.schreiben('ton', true); }
  powerAnzeigen();
  const zeilen = ['DF0: FUNKERBIER', 'LOADING FUNKERBIER.MOD ...... OK', 'ANTENNE AUSRICHTEN .... SWR 1.2:1', 'ZAPFHAHN KALIBRIERT ... OK', 'QRV.'];
  const ruhig = matchMedia('(prefers-reduced-motion: reduce)').matches;
  for (const z of zeilen) {
    log.textContent += z + '\n';
    if (!ruhig) await new Promise((r) => setTimeout(r, 230));
  }
  boot.classList.add('weg');
  setTimeout(() => boot.remove(), 500);
  dmd.zeige('QRV', { art: 'blink', gross: true, dauer: 1400, sofort: true });
}
$('#boot-ton').addEventListener('click', () => booten(true));
$('#boot-still').addEventListener('click', () => booten(false));
addEventListener('keydown', function los(e) {
  if (!$('#boot') || $('#boot').classList.contains('weg')) return removeEventListener('keydown', los);
  if (e.key === 'Escape') booten(false);
});

// ---- Meine Runden -----------------------------------------------------------
function merkeRunde(id, titel, rolle) {
  const liste = speicher.lesen('runden', []).filter((r) => r.id !== id);
  const alt = speicher.lesen('runden', []).find((r) => r.id === id);
  liste.unshift({ id, titel, rolle: rolle === 'admin' || alt?.rolle === 'admin' ? 'admin' : rolle || alt?.rolle || 'gast' });
  speicher.schreiben('runden', liste.slice(0, 20));
}

// ---- Startseite: neue Runde --------------------------------------------------
function heute(plusTage = 0) {
  const d = new Date();
  d.setDate(d.getDate() + plusTage);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

function zeigeStart() {
  document.title = 'Funkerbier QRV';
  dmd.setzeIdle(['FUNKERBIER', 'CQ CQ CQ DE BIER.INGOK.CH', punkteText, 'TERMIN ERFASSEN!', 'INSERT COIN']);
  let termine = [];
  const chips = h('div', { class: 'chips', 'aria-label': 'Vorgeschlagene Termine' });
  const datum = h('input', { type: 'date', value: heute(7), 'aria-label': 'Datum' });
  const zeit = h('input', { type: 'time', value: '19:00', 'aria-label': 'Uhrzeit' });
  const meldung = h('div', { role: 'status' });

  function chipsMalen() {
    termine.sort();
    chips.replaceChildren(...(termine.length ? termine.map((t) => h('span', { class: 'chip' }, kurz(t),
      h('button', { type: 'button', 'aria-label': 'Termin entfernen', onclick: () => { termine = termine.filter((x) => x !== t); funk.squelch(0.15); chipsMalen(); } }, '×')))
      : [h('span', { class: 'leise mini' }, 'Noch keine Termine. Datum und Zeit wählen, dann + TERMIN.')]));
  }
  function dazu(t) {
    if (!t || termine.includes(t)) return false;
    if (termine.length >= 30) { meldungZeigen(meldung, 'Höchstens 30 Termine.', 'fehler'); return false; }
    termine.push(t);
    return true;
  }
  function wochentag(wt) {
    const d = new Date();
    let n = 0;
    for (let i = 1; i < 60 && n < 4; i++) {
      const x = new Date(d.getFullYear(), d.getMonth(), d.getDate() + i);
      if (x.getDay() === wt) { dazu(`${x.getFullYear()}-${String(x.getMonth() + 1).padStart(2, '0')}-${String(x.getDate()).padStart(2, '0')}T${zeit.value || '19:00'}`); n++; }
    }
    funk.bumper(1); punkteDazu(100); chipsMalen();
  }
  chipsMalen();

  const f = {
    titel: h('input', { name: 'titel', value: 'Funkerbier', maxlength: 80, required: true }),
    ort: h('input', { name: 'ort', maxlength: 120, placeholder: 'z. B. Restaurant Sonne, Stammtisch-Ecke' }),
    notiz: h('textarea', { name: 'notiz', maxlength: 1000, placeholder: 'Handfunke mitbringen, QRG 438.650 MHz für die Anfahrt …' }),
    wer: h('input', { name: 'wer', maxlength: 40, required: true, autocomplete: 'name', value: speicher.lesen('name', '') }),
    mail: h('input', { name: 'mail', type: 'email', maxlength: 120, required: true, autocomplete: 'email', value: speicher.lesen('mail', '') }),
    dauer: h('select', { name: 'dauer' }, ...[[120, '2 Stunden'], [180, '3 Stunden'], [240, '4 Stunden'], [360, 'Open End (6 h)']].map(([v, t]) => h('option', { value: v, selected: v === 180 }, t))),
    website: h('input', { name: 'website', tabindex: '-1', autocomplete: 'off' }),
  };
  const knopf = h('button', { class: 'knopf knopf-gross', type: 'submit' }, '► TERMIN ERFASSEN');
  const abschuss = h('div', { class: 'abschuss' }, h('span', { class: 'flipper' }), knopf, h('span', { class: 'flipper rechts' }));

  const form = h('form', { novalidate: true, onsubmit: async (e) => {
    e.preventDefault();
    abschuss.classList.add('zack');
    setTimeout(() => abschuss.classList.remove('zack'), 160);
    if (!f.wer.value.trim() || !f.mail.value.includes('@')) { fehlerZeigen(meldung, 'Name und Mailadresse braucht es, sonst kommt keine QSL-Karte an.'); return; }
    if (!termine.length && datum.value) dazu(`${datum.value}T${zeit.value || '19:00'}`);
    if (!termine.length) { fehlerZeigen(meldung, 'Mindestens einen Termin vorschlagen.'); return; }
    knopf.disabled = true;
    try {
      const r = await api({ a: 'anlegen', titel: f.titel.value, ort: f.ort.value, notiz: f.notiz.value, wer: f.wer.value,
        mail: f.mail.value, dauer: Number(f.dauer.value), termine, website: f.website.value });
      speicher.schreiben('name', f.wer.value.trim());
      speicher.schreiben('mail', f.mail.value.trim());
      speicher.schreiben('admin:' + r.id, r.admin);
      merkeRunde(r.id, f.titel.value.trim(), 'admin');
      funk.roger(); punkteDazu(10000);
      dmd.zeige('RUNDE AUSGERUFEN', { art: 'blink', dauer: 2200, sofort: true });
      dmd.zeige(r.mail ? 'ADMIN-LINK PER MAIL UNTERWEGS' : 'MAIL KLEMMT - LINK UNTEN SPEICHERN', { art: 'lauf' });
      location.hash = `#/r/${r.id}/admin/${r.admin}`;
    } catch (err) {
      fehlerZeigen(meldung, err.message);
    } finally { knopf.disabled = false; }
  } },
  h('label', { class: 'feld' }, h('span', {}, 'TITEL'), f.titel),
  h('div', { class: 'reihe' },
    h('label', { class: 'feld' }, h('span', {}, 'QTH / ORT'), f.ort),
    h('label', { class: 'feld' }, h('span', {}, 'DAUER'), f.dauer)),
  h('label', { class: 'feld' }, h('span', {}, 'NOTIZ'), f.notiz),
  h('div', { class: 'feld' }, h('span', {}, 'TERMINE VORSCHLAGEN'),
    h('div', { class: 'zeile-eingabe' }, datum, zeit,
      h('button', { type: 'button', class: 'knopf', onclick: () => { if (dazu(`${datum.value}T${zeit.value || '19:00'}`)) { funk.bumper(2); punkteDazu(100); } chipsMalen(); } }, '+ TERMIN')),
    h('div', { class: 'zeile-eingabe' },
      h('button', { type: 'button', class: 'knopf knopf-klein', onclick: () => wochentag(4) }, '+ 4 DONNERSTAGE'),
      h('button', { type: 'button', class: 'knopf knopf-klein', onclick: () => wochentag(5) }, '+ 4 FREITAGE'),
      h('button', { type: 'button', class: 'knopf knopf-klein', onclick: () => { termine = []; chipsMalen(); funk.squelch(); } }, 'ALLE WEG')),
    chips),
  h('div', { class: 'reihe' },
    h('label', { class: 'feld' }, h('span', {}, 'DEIN NAME / CALL'), f.wer),
    h('label', { class: 'feld' }, h('span', {}, 'DEINE MAIL'), f.mail)),
  h('label', { class: 'honig', 'aria-hidden': 'true' }, 'Website', f.website),
  h('p', { class: 'leise mini' }, 'An deine Mail geht der geheime Admin-Link. Damit legst du später den Termin fest.'),
  abschuss, meldung);

  const runden = speicher.lesen('runden', []);
  app.replaceChildren(
    h('div', { class: 'zwei-spalten' },
      fenster('TERMIN ERFASSEN', form),
      h('div', { style: 'display:grid;gap:18px' },
        fenster('SO LÄUFT DAS QSO',
          h('ol', { class: 'schritte' },
            h('li', {}, 'Termine vorschlagen und erfassen.'),
            h('li', {}, 'Den Link an die Runde schicken.'),
            h('li', {}, 'Alle drücken ihre Bumper: grün = JA, gelb = VIELLEICHT, rot = NEIN. Dafür gibt es eine QSL-Mail.'),
            h('li', {}, 'Du legst den Termin fest. Alle bekommen eine Mail mit Kalendereintrag. JACKPOT!')),
          h('p', { class: 'punktzahl' }, 'SCORE ', h('span', { id: 'punkte' }, String(punkte).padStart(7, '0')))),
        runden.length ? fenster('MEIN LOGBUCH', h('ul', { class: 'liste' }, runden.map((r) =>
          h('li', {}, h('a', { href: r.rolle === 'admin' && speicher.lesen('admin:' + r.id, null) ? `#/r/${r.id}/admin/${speicher.lesen('admin:' + r.id, '')}` : `#/r/${r.id}` },
            h('span', {}, r.titel || r.id), h('small', {}, r.rolle === 'admin' ? 'ADMIN' : 'QRV')))))) : null)));
}

function meldungZeigen(el, text, art = 'gut') {
  el.replaceChildren(h('p', { class: 'meldung ' + art }, text));
}
function fehlerZeigen(el, text) {
  meldungZeigen(el, text, 'fehler');
  funk.tilt();
  dmd.zeige('TILT', { art: 'blink', gross: true, dauer: 1200, sofort: true });
  dmd.zeige(text, { art: 'lauf' });
}

// ---- Runde ------------------------------------------------------------------
const WERTE = { 0: 'NEIN', 1: 'VIELL.', 2: 'JA' };
const NAECHSTER = { 0: 2, 2: 1, 1: 0 };

async function zeigeRunde(id, tok) {
  const admin = tok.admin || speicher.lesen('admin:' + id, null);
  const ich = tok.ich || speicher.lesen('ich:' + id, null);
  if (tok.admin) speicher.schreiben('admin:' + id, tok.admin);
  if (tok.ich) speicher.schreiben('ich:' + id, tok.ich);
  app.replaceChildren(fenster('EMPFANG …', h('p', { class: 'blinken' }, 'Suche Signal auf der Runde ' + id + ' …')));
  let d;
  try {
    d = await api({ a: 'runde', id, admin, ich }, 'GET');
  } catch (err) {
    app.replaceChildren(fenster('KEIN SIGNAL', h('p', {}, err.message), h('p', {}, h('a', { href: '#/' }, '← Termin erfassen'))));
    dmd.zeige('NO SIGNAL', { art: 'blink', gross: true, sofort: true });
    return;
  }
  if (tok.ich && !d.ich) speicher.schreiben('ich:' + id, null);
  merkeRunde(d.id, d.titel, d.admin ? 'admin' : d.ich ? 'qrv' : 'gast');
  malen(d, d.admin ? admin : null, d.ich ? ich : null);
}

function malen(d, adminToken, ichToken) {
  document.title = `${d.titel} · bier.ingok.ch`;
  const linkAlle = `${location.origin}${location.pathname}#/r/${d.id}`;
  const zaehlen = (tid) => {
    let ja = 0, viel = 0;
    for (const f of d.funker) { const w = f.stimmen[tid]; if (w === 2) ja++; else if (w === 1) viel++; }
    return { ja, viel, wert: ja * 2 + viel };
  };
  const best = Math.max(0, ...d.termine.map((t) => zaehlen(t.id).wert));
  const finalT = d.termine.find((t) => t.id === d.final);
  const n = d.funker.length;

  dmd.setzeIdle([
    d.titel,
    finalT ? 'JACKPOT ' + kurz(finalT.beginn) : `${n} OP${n === 1 ? '' : 'S'} IM LOG`,
    ...(best > 0 && !finalT ? ['HIGHSCORE ' + kurz(d.termine.find((t) => zaehlen(t.id).wert === best).beginn)] : []),
    punkteText,
    'CQ CQ ' + d.titel + ' QRV',
  ]);

  // eigener Eintrag (neu oder per Token)
  const meins = {
    name: d.ich?.name ?? speicher.lesen('name', ''),
    rufzeichen: d.ich?.rufzeichen ?? speicher.lesen('ruf', ''),
    mail: d.ich?.mail ?? speicher.lesen('mail', ''),
    stimmen: { ...(d.ich?.stimmen || {}) },
  };
  for (const t of d.termine) if (meins.stimmen[t.id] == null) meins.stimmen[t.id] = 0;

  const kopf = [];
  if (finalT) {
    const z = zerlegen(finalT.beginn);
    kopf.push(h('div', { class: 'jackpot-banner', role: 'status' }, '★ JACKPOT ★', h('strong', {}, `${z.tag} ${z.datum}${z.jahr} · ${z.zeit}`),
      d.ort ? h('div', {}, 'QTH: ' + d.ort) : null,
      h('a', { class: 'knopf', href: `api.php?a=ics&id=${encodeURIComponent(d.id)}`, style: 'display:inline-block;margin-top:10px;text-decoration:none' }, '📅 IN DEN KALENDER')));
  }

  const meldung = h('div', { role: 'status' });
  const terminListe = h('div', { class: 'termine' }, d.termine.map((t) => {
    const z = zerlegen(t.beginn);
    const c = zaehlen(t.id);
    const ja = d.funker.filter((f) => f.stimmen[t.id] === 2);
    const viel = d.funker.filter((f) => f.stimmen[t.id] === 1);
    const bumper = h('button', { type: 'button', class: `bumper s${meins.stimmen[t.id]}`, 'aria-label': `Deine Antwort für ${kurz(t.beginn)}: ${WERTE[meins.stimmen[t.id]]}` }, WERTE[meins.stimmen[t.id]]);
    bumper.addEventListener('click', () => {
      const w = (meins.stimmen[t.id] = NAECHSTER[meins.stimmen[t.id]]);
      bumper.className = `bumper s${w} pop`;
      bumper.textContent = WERTE[w];
      bumper.setAttribute('aria-label', `Deine Antwort für ${kurz(t.beginn)}: ${WERTE[w]}`);
      funk.bumper(w); punkteDazu(w === 2 ? 1000 : w === 1 ? 500 : 100);
      dmd.zeige(w === 2 ? 'JA!' : w === 1 ? 'VIELLEICHT' : 'NEIN', { gross: true, dauer: 700, sofort: true, art: w === 2 ? 'blink' : 'mitte' });
    });
    const aktionen = [bumper];
    if (d.admin) {
      const ist = d.final === t.id;
      const fk = h('button', { type: 'button', class: 'knopf knopf-klein' }, ist ? 'AUFHEBEN' : 'FESTLEGEN');
      let scharf = false;
      fk.addEventListener('click', async () => {
        if (!scharf) { scharf = true; fk.textContent = ist ? 'WIRKLICH?' : 'SICHER? NOCHMAL'; funk.ton(660, 0.1); setTimeout(() => { scharf = false; fk.textContent = ist ? 'AUFHEBEN' : 'FESTLEGEN'; }, 3000); return; }
        fk.disabled = true;
        try {
          const r = await api({ a: 'final', id: d.id, admin: adminToken, termin: ist ? null : t.id });
          if (r.final) {
            funk.jackpot(); punkteDazu(50000);
            dmd.zeige('JACKPOT', { art: 'blink', gross: true, dauer: 2600, sofort: true });
            dmd.zeige(r.gebremst ? 'RUNDMAIL ERST NACH 10 MIN WIEDER' : `RUNDMAIL AN ${r.gesendet} OPS`, { art: 'lauf' });
          } else { funk.squelch(); dmd.zeige('TERMIN OFFEN', { gross: false, sofort: true }); }
          zeigeRunde(d.id, {});
        } catch (err) { fehlerZeigen(meldung, err.message); fk.disabled = false; }
      });
      aktionen.push(fk);
    }
    return h('div', { class: `termin${c.wert === best && best > 0 ? ' best' : ''}${d.final === t.id ? ' final' : ''}` },
      h('div', { class: 'datum' }, h('b', {}, z.tag), h('span', {}, z.datum), h('em', {}, z.zeit)),
      h('div', { class: 'tally' },
        h('div', { class: 'balken', title: `${c.ja} ja, ${c.viel} vielleicht` },
          h('i', { class: 'ja', style: `width:${n ? (c.ja / n) * 100 : 0}%` }), h('i', { class: 'viel', style: `width:${n ? (c.viel / n) * 100 : 0}%` })),
        h('div', { class: 'namen' }, `${c.ja}× JA · ${c.viel}× VIELL.`, ja.length || viel.length ? ' — ' : '',
          ja.map((f, i) => [i ? ', ' : '', h('span', { class: 'j' }, f.rufzeichen || f.name)]),
          ja.length && viel.length ? ', ' : '',
          viel.map((f, i) => [i ? ', ' : '', h('span', { class: 'v' }, (f.rufzeichen || f.name) + '?')])),
        h('div', { class: 'marken' },
          d.final === t.id ? h('span', { class: 'marke-klein jackpot' }, 'JACKPOT') : null,
          c.wert === best && best > 0 && d.final !== t.id ? h('span', { class: 'marke-klein' }, 'HIGHSCORE') : null)),
      h('div', { class: 'aktionen' }, aktionen));
  }));

  // Eintragsformular
  const f = {
    name: h('input', { value: meins.name, maxlength: 40, required: true, autocomplete: 'name' }),
    rufzeichen: h('input', { value: meins.rufzeichen, maxlength: 12, placeholder: 'optional, z. B. HB9XYZ', autocapitalize: 'characters' }),
    mail: h('input', { value: meins.mail, type: 'email', maxlength: 120, required: true, autocomplete: 'email' }),
    website: h('input', { tabindex: '-1', autocomplete: 'off' }),
  };
  const knopf = h('button', { class: 'knopf knopf-gross', type: 'submit' }, d.ich ? '► ÄNDERUNG FUNKEN' : '► INS LOG EINTRAGEN');
  const abschuss = h('div', { class: 'abschuss' }, h('span', { class: 'flipper' }), knopf, h('span', { class: 'flipper rechts' }));
  const form = h('form', { novalidate: true, onsubmit: async (e) => {
    e.preventDefault();
    abschuss.classList.add('zack');
    setTimeout(() => abschuss.classList.remove('zack'), 160);
    funk.bumper(2);
    if (!f.name.value.trim() || !f.mail.value.includes('@')) { fehlerZeigen(meldung, 'Name und Mail braucht es für die QSL-Karte.'); return; }
    knopf.disabled = true;
    try {
      const r = await api({ a: 'eintragen', id: d.id, token: ichToken, name: f.name.value, rufzeichen: f.rufzeichen.value,
        mail: f.mail.value, stimmen: meins.stimmen, website: f.website.value });
      speicher.schreiben('ich:' + d.id, r.token);
      speicher.schreiben('name', f.name.value.trim());
      speicher.schreiben('ruf', f.rufzeichen.value.trim().toUpperCase());
      speicher.schreiben('mail', f.mail.value.trim());
      funk.roger(); punkteDazu(r.neu ? 5000 : 1500);
      dmd.zeige(r.neu ? 'QSO IM LOG' : 'LOG AKTUALISIERT', { art: 'blink', dauer: 1800, sofort: true });
      if (r.neu) dmd.zeige(r.mail ? 'QSL-KARTE IST PER MAIL UNTERWEGS' : 'MAIL KLEMMT - EINTRAG IST TROTZDEM IM LOG', { art: 'lauf' });
      await zeigeRunde(d.id, {});
      const m = $('#eintrag-meldung');
      if (m) meldungZeigen(m, r.neu ? (r.mail ? 'Du bist im Log. Die Bestätigung mit deinem Bearbeiten-Link ist per Mail unterwegs.' : 'Du bist im Log. Die Mail ging leider nicht raus. Dieser Browser merkt sich deinen Eintrag.') : 'Änderung gespeichert.');
    } catch (err) {
      if (err.status === 409) linkAnbieten(d.id, f.mail.value, meldung);
      else fehlerZeigen(meldung, err.message);
    } finally { knopf.disabled = false; }
  } },
  h('div', { class: 'reihe' },
    h('label', { class: 'feld' }, h('span', {}, 'NAME'), f.name),
    h('label', { class: 'feld' }, h('span', {}, 'RUFZEICHEN'), f.rufzeichen)),
  h('label', { class: 'feld' }, h('span', {}, 'MAIL (sieht nur der Organisator)'), f.mail),
  h('label', { class: 'honig', 'aria-hidden': 'true' }, 'Website', f.website),
  abschuss, meldung, h('div', { id: 'eintrag-meldung', role: 'status' }));

  // Logbuch-Tabelle
  const tabelle = n ? h('div', { class: 'logbuch-rahmen' }, h('table', { class: 'logbuch' },
    h('thead', {}, h('tr', {}, h('th', {}, 'OP'), d.termine.map((t) => { const z = zerlegen(t.beginn); return h('th', {}, z.tag, h('br'), z.datum, h('br'), z.zeit); }),
      d.admin ? h('th', {}, '') : null)),
    h('tbody', {}, d.funker.map((p) => h('tr', {},
      h('td', {}, p.name, p.rufzeichen ? ` (${p.rufzeichen})` : '', d.admin && p.mail ? h('div', { class: 'leise mini' }, p.mail) : null),
      d.termine.map((t) => { const w = p.stimmen[t.id] ?? 0; return h('td', { class: 'w' + w }, w === 2 ? '✔' : w === 1 ? '~' : '✘'); }),
      d.admin ? h('td', {}, h('button', { class: 'weg-knopf', title: 'Eintrag löschen', 'aria-label': `${p.name} löschen`, onclick: async (e) => {
        const b = e.currentTarget;
        if (b.dataset.scharf !== '1') { b.dataset.scharf = '1'; b.textContent = 'SICHER?'; return; }
        try { await api({ a: 'loeschen', id: d.id, admin: adminToken, funker: p.id }); funk.squelch(); zeigeRunde(d.id, {}); } catch (err) { fehlerZeigen(meldung, err.message); }
      } }, '✕')) : null)),
    h('tr', { class: 'summe' }, h('td', {}, 'SUMME'), d.termine.map((t) => { const c = zaehlen(t.id); return h('td', {}, `${c.ja}${c.viel ? '+' + c.viel : ''}`); }), d.admin ? h('td') : null))))
    : h('p', { class: 'leise' }, 'Noch niemand im Log. Sei die erste Station!');

  const teilen = d.admin ? fenster('ADMIN · LINK FÜR DIE RUNDE',
    h('p', {}, 'Diesen Link an alle schicken:'),
    h('div', { class: 'teilen' }, h('input', { readonly: true, value: linkAlle, onfocus: (e) => e.target.select(), 'aria-label': 'Link für alle' }),
      h('button', { class: 'knopf', type: 'button', onclick: async (e) => {
        try { await navigator.clipboard.writeText(linkAlle); e.target.textContent = 'KOPIERT ✔'; funk.punkte(); } catch { e.target.textContent = 'MARKIEREN + KOPIEREN'; }
      } }, 'KOPIEREN'),
      navigator.share ? h('button', { class: 'knopf', type: 'button', onclick: () => navigator.share({ title: d.titel, text: `${d.titel}: Trag dich ein!`, url: linkAlle }).catch(() => {}) }, 'TEILEN') : null),
    h('p', { class: 'leise mini' }, `Du bist Admin dieser Runde (Admin-Link in deiner Mail an ${d.mail_admin}). Mit FESTLEGEN bei einem Termin geht eine Rundmail mit Kalendereintrag an alle.`)) : null;

  app.replaceChildren(
    ...kopf,
    fenster(d.titel,
      h('p', {}, d.ort ? `QTH: ${d.ort} · ` : '', `Ausgerufen von ${d.wer}`, ` · ${d.dauer >= 360 ? 'Open End' : d.dauer / 60 + ' h'}`),
      d.notiz ? h('p', { class: 'leise', style: 'white-space:pre-wrap' }, d.notiz) : null,
      h('p', { class: 'leise mini' }, d.ich ? `Du bist im Log als ${d.ich.name}. Drück die Bumper und funk die Änderung.` : 'Drück bei jedem Termin den Bumper: grün = JA, gelb = VIELLEICHT, rot = NEIN.'),
      terminListe,
      h('h3', { class: 'punktzahl', style: 'margin:18px 0 10px' }, d.ich ? 'DEIN EINTRAG' : 'DEIN EINTRAG · QSL PER MAIL'),
      form),
    teilen,
    fenster('LOGBUCH · ' + n + ' OP' + (n === 1 ? '' : 'S'), tabelle,
      !d.ich ? h('p', { class: 'leise mini', style: 'margin-top:12px' }, 'Schon eingetragen, aber anderer Browser? ',
        h('a', { href: '#', onclick: (e) => { e.preventDefault(); linkAnbieten(d.id, speicher.lesen('mail', ''), meldung); meldung.scrollIntoView({ block: 'center' }); } }, 'Bearbeiten-Link per Mail anfordern')) : null),
  );
}

function linkAnbieten(id, mail, ziel) {
  const eingabe = h('input', { type: 'email', value: mail || '', 'aria-label': 'Mailadresse' });
  ziel.replaceChildren(h('div', { class: 'meldung' },
    h('p', {}, 'Diese Mailadresse ist schon im Log. Soll ich dir den Bearbeiten-Link nochmals schicken?'),
    h('div', { class: 'teilen' }, eingabe, h('button', { class: 'knopf', type: 'button', onclick: async (e) => {
      e.target.disabled = true;
      try { await api({ a: 'link', id, mail: eingabe.value }); meldungZeigen(ziel, 'Falls die Adresse im Log steht, ist der Link jetzt unterwegs.'); funk.roger(); }
      catch (err) { fehlerZeigen(ziel, err.message); }
    } }, 'LINK SENDEN'))));
}

// ---- Router -----------------------------------------------------------------
function route() {
  const teile = location.hash.replace(/^#\/?/, '').split('/').filter(Boolean);
  if (teile[0] === 'r' && teile[1]) {
    zeigeRunde(teile[1], { admin: teile[2] === 'admin' ? teile[3] : null, ich: teile[2] === 'ich' ? teile[3] : null });
  } else {
    zeigeStart();
  }
  scrollTo({ top: 0 });
  if (funk.bereit) funk.squelch(0.2);
}
addEventListener('hashchange', route);
route();
powerAnzeigen();
