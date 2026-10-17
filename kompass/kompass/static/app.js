// Kompass – Oberflaeche (Vanilla-JS, ein Modul). Daten immer frisch vom Server.
'use strict';

const $ = (s, el = document) => el.querySelector(s);
const h = (tag, attr = {}, ...kinder) => {
  const el = document.createElement(tag);
  for (const [k, v] of Object.entries(attr || {})) {
    if (v == null || v === false) continue;
    if (k.startsWith('on')) el.addEventListener(k.slice(2), v);
    else if (k === 'class') el.className = v;
    else if (k === 'text') el.textContent = v;
    else el.setAttribute(k, v === true ? '' : v);
  }
  for (const kind of kinder.flat(Infinity)) if (kind != null && kind !== false) el.append(kind.nodeType ? kind : String(kind));
  return el;
};
const svg = (pfad) => {
  const s = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
  s.setAttribute('viewBox', '0 0 24 24'); s.setAttribute('aria-hidden', 'true');
  s.innerHTML = pfad;
  return s;
};
const HAKEN = '<path d="M5 12.5l4.5 4.5L19 7.5"/>';
const CSRF = $('meta[name=csrf]').content;

async function api(pfad, daten) {
  const r = await fetch(pfad, daten === undefined ? { headers: { Accept: 'application/json' } }
    : { method: 'POST', headers: { 'Content-Type': 'application/json', 'X-CSRF': CSRF }, body: JSON.stringify(daten) });
  if (r.status === 401) { location.href = '/login'; throw new Error('Bitte neu anmelden.'); }
  const j = await r.json().catch(() => ({}));
  if (!r.ok) { const e = new Error(j.error || `Störung (${r.status})`); e.daten = j; e.status = r.status; throw e; }
  return j;
}

let toastZeit;
function toast(text) {
  const t = $('#toast'); t.textContent = text; t.hidden = false;
  clearTimeout(toastZeit); toastZeit = setTimeout(() => { t.hidden = true; }, 3200);
}
const fehler = (e) => toast(e.message || String(e));

// ---- Formatierung -------------------------------------------------------------------
const uhrzeit = (iso) => new Date(iso).toLocaleTimeString('de-CH', { hour: '2-digit', minute: '2-digit' });
const tagKurz = (tag) => new Date(tag + 'T12:00').toLocaleDateString('de-CH', { weekday: 'short', day: 'numeric', month: 'short' });
const heuteText = () => new Date().toLocaleDateString('sv-SE');  // JJJJ-MM-TT lokal
function plusTage(n) { const d = new Date(); d.setDate(d.getDate() + n); return d.toLocaleDateString('sv-SE'); }
function tageBis(tag) { return Math.round((new Date(tag + 'T12:00') - new Date(heuteText() + 'T12:00')) / 864e5); }
function relativ(tag) {
  const n = tageBis(tag);
  if (n === 0) return 'heute';
  if (n === 1) return 'morgen';
  if (n === 2) return 'übermorgen';
  if (n > 2) return n < 7 ? `in ${n} Tagen (${tagKurz(tag)})` : tagKurz(tag);
  return n === -1 ? 'seit gestern' : `seit ${-n} Tagen`;
}
function dauerText(min) {
  if (min <= 0) return 'jetzt';
  if (min < 60) return `in ${min} min`;
  const s = Math.floor(min / 60), m = min % 60;
  return `in ${s} h${m ? ` ${m} min` : ''}`;
}
const mmss = (s) => `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`;
const franken = (r) => (r / 100).toLocaleString('de-CH', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const KATEGORIE = { rechnung: 'Rechnung', kuendigung: 'Kündigung', versicherung: 'Versicherung', amt: 'Amt', steuern: 'Steuern', sonstiges: 'Sonstiges' };

// ---- Dialog -------------------------------------------------------------------------
// dialog(titel, inhalt, knoepfe) -> Promise mit dem Wert des gedrueckten Knopfs (oder null)
function dialog(titel, inhalt, knoepfe = [['Schliessen', null]]) {
  const d = $('#dialog'), f = $('#dialog-form');
  return new Promise((fertig) => {
    let wert = null;
    f.replaceChildren(h('h3', { text: titel }), ...[inhalt].flat().filter(Boolean),
      h('div', { class: 'knopfreihe' }, knoepfe.map(([text, w, art]) => h('button', {
        class: `knopf ${art || (w === null ? 'zart' : '')}`, type: w === 'absenden' ? 'submit' : 'button', value: String(w),
        onclick: (ev) => { if (w === 'absenden') return; ev.preventDefault(); wert = w; d.close(); },
      }, text))));
    f.onsubmit = (ev) => { ev.preventDefault(); if (!f.reportValidity()) return; wert = 'absenden'; d.close(); };
    d.onclose = () => fertig(wert);
    d.showModal();
    const erstes = f.querySelector('input:not([type=hidden]), textarea, select');
    if (erstes) erstes.focus();
  });
}
const offenerDialog = () => $('#dialog').open;

// ---- Zustand und Router ---------------------------------------------------------------
const zustand = { ohne: new Set(), ohneTag: heuteText(), heute: null, fokus: null, einst: null, auswahl: 0, fokusEintrag: null };
const ANSICHTEN = { heute: ansichtHeute, eingang: ansichtEingang, routinen: ansichtRoutinen, papier: ansichtPapier, fokus: ansichtFokus, einstellungen: ansichtEinstellungen };
const aktuell = () => (location.hash.slice(1).split('?')[0] || 'heute');

async function zeigen(still = false) {
  const name = ANSICHTEN[aktuell()] ? aktuell() : 'heute';
  for (const a of document.querySelectorAll('.leiste a')) a.classList.toggle('aktiv', a.dataset.ziel === name);
  if (zustand.ohneTag !== heuteText()) { zustand.ohne.clear(); zustand.ohneTag = heuteText(); }
  const ziel = $('#ansicht');
  try {
    const inhalt = await ANSICHTEN[name]();
    // Beim stillen Auffrischen nichts ersetzen, waehrend jemand in der Ansicht tippt
    if (still && ziel.contains(document.activeElement) && document.activeElement.matches('input, textarea, select')) return;
    ziel.replaceChildren(...[inhalt].flat());
    if (!still) { ziel.focus({ preventScroll: true }); window.scrollTo(0, 0); }
  } catch (e) { if (!still) ziel.replaceChildren(h('p', { class: 'leer', text: e.message })); }
  seiteMalen();
}
addEventListener('hashchange', () => zeigen());

// ---- Erfassen -----------------------------------------------------------------------
$('#erfassen').addEventListener('submit', async (ev) => {
  ev.preventDefault();
  const feld = $('#erfassen-feld'), text = feld.value.trim();
  if (!text) return;
  feld.disabled = true;
  try {
    await api('/api/eintrag', { text });
    feld.value = '';
    toast('Im Eingang. Sortieren kannst du später.');
    eingangZahl((zustand.heute?.eingang || 0) + 1);
    if (aktuell() === 'eingang') zeigen(true);
  } catch (e) { fehler(e); }
  feld.disabled = false; feld.focus();
});
function eingangZahl(n) {
  if (zustand.heute) zustand.heute.eingang = n;
  const z = $('#zahl-eingang'); z.hidden = !n; z.textContent = n;
}

// ---- Aktionen auf Eintraegen ------------------------------------------------------------
async function aktion(id, daten, meldung) {
  try {
    const r = await api(`/api/eintrag/${id}`, daten);
    if (meldung) toast(meldung);
    await zeigen(true);
    return r;
  } catch (e) {
    if (e.daten?.voll) {
      const w = await dialog('Heute ist schon voll', h('p', { text: 'Es sind schon drei wichtige Aufgaben für heute geplant. Soll diese einfach „auch heute“ dazukommen?' }),
        [['Abbrechen', null], ['Auch heute', 'auch', 'haupt']]);
      if (w === 'auch') return aktion(id, { aktion: 'heute', haupt: false }, 'Für heute eingeplant.');
      return null;
    }
    fehler(e);
    return null;
  }
}
const erledigt = (id) => aktion(id, { aktion: 'erledigt' }, 'Erledigt. Gut gemacht.');

async function tagWaehlen(eintrag) {
  const datum = h('input', { type: 'date', name: 'tag', min: plusTage(1), value: plusTage(1) });
  const montag = plusTage(((8 - new Date().getDay()) % 7) || 7);
  const w = await dialog('Wann passt es?', [
    h('p', { class: 'leise', text: eintrag.text }),
    h('div', { class: 'knopfreihe' },
      h('button', { class: 'knopf klein', type: 'button', onclick: () => { datum.value = plusTage(1); } }, 'Morgen'),
      h('button', { class: 'knopf klein', type: 'button', onclick: () => { datum.value = plusTage(2); } }, 'Übermorgen'),
      h('button', { class: 'knopf klein', type: 'button', onclick: () => { datum.value = montag; } }, 'Nächsten Montag')),
    h('label', {}, 'Datum', datum),
  ], [['Abbrechen', null], ['Irgendwann', 'irgendwann'], ['Einplanen', 'absenden', 'haupt']]);
  if (w === 'absenden') return aktion(eintrag.id, { aktion: 'tag', tag: datum.value }, `Eingeplant für ${relativ(datum.value)}.`);
  if (w === 'irgendwann') return aktion(eintrag.id, { aktion: 'irgendwann' }, 'Liegt bei „Irgendwann“.');
  return null;
}

async function mehrMenue(eintrag) {
  const w = await dialog(eintrag.text, h('p', { class: 'leise', text: 'Was soll damit passieren?' }), [
    ['Schliessen', null], ['Loslassen', 'los'], ['Anderer Tag', 'tag'],
    eintrag.heute ? ['Nicht mehr wichtig', 'weg'] : ['Wichtig heute', 'haupt'],
  ]);
  if (w === 'los') return aktion(eintrag.id, { aktion: 'losgelassen' }, 'Losgelassen. Darf auch mal sein.');
  if (w === 'tag') return tagWaehlen(eintrag);
  if (w === 'weg') return aktion(eintrag.id, { aktion: 'haupt_weg' }, 'Bleibt für heute, aber ohne Druck.');
  if (w === 'haupt') return aktion(eintrag.id, { aktion: 'heute', haupt: true }, 'Wichtig für heute.');
  return null;
}

function fokusFuer(id) { zustand.fokusEintrag = id; location.hash = 'fokus'; }

// ---- Zerleger -----------------------------------------------------------------------
async function zerlegen(eintrag) {
  const liste = h('div', {});
  const zeileNeu = (text = '', min = '') => {
    const z = h('div', { class: 'vorschlag' },
      h('input', { value: text, maxlength: 160, placeholder: 'Kleiner Schritt mit Verb, z. B. „Ordner öffnen“', 'aria-label': 'Schritt' }),
      h('button', { class: 'mini', type: 'button', title: 'nach oben', 'aria-label': 'nach oben', onclick: () => { if (z.previousElementSibling) liste.insertBefore(z, z.previousElementSibling); } }, '↑'),
      h('button', { class: 'mini', type: 'button', title: 'entfernen', 'aria-label': 'entfernen', onclick: () => z.remove() }, '×'));
    z.dataset.min = min;
    liste.append(z);
  };
  const status = h('p', { class: 'leise klein', text: 'Claude schlägt 5–8 kleine Schritte vor. Gesendet wird nur dieser Text.' });
  const holen = h('button', { class: 'knopf klein', type: 'button', onclick: async () => {
    holen.disabled = true; status.textContent = 'Denke nach …';
    try {
      const r = await api('/api/zerlegen', { eintrag_id: eintrag.id });
      liste.replaceChildren(); r.schritte.forEach((s) => zeileNeu(s.text, s.min));
      status.textContent = 'Vorschlag: anpassen, streichen, umsortieren.';
    } catch (e) { status.textContent = e.message; if (!liste.children.length) { zeileNeu(); zeileNeu(); zeileNeu(); } }
    holen.disabled = false;
  } }, 'Vorschlag holen');
  zeileNeu(); zeileNeu(); zeileNeu();
  const w = await dialog(`Zerlegen: ${eintrag.text}`, [status, h('div', { class: 'knopfreihe' }, holen), liste,
    h('button', { class: 'knopf zart klein', type: 'button', onclick: () => zeileNeu() }, '＋ Schritt')],
  [['Abbrechen', null], ['Übernehmen', 'uebernehmen', 'haupt']]);
  if (w !== 'uebernehmen') return;
  const schritte = [...liste.children].map((z) => ({ text: z.querySelector('input').value.trim(), min: Number(z.dataset.min) || undefined })).filter((s) => s.text);
  if (!schritte.length) return toast('Keine Schritte eingetragen.');
  try { await api(`/api/eintrag/${eintrag.id}/schritte`, { schritte }); toast(`${schritte.length} Schritte. Der erste ist ganz klein.`); zeigen(true); } catch (e) { fehler(e); }
}

// ---- Heute --------------------------------------------------------------------------
async function ansichtHeute() {
  const ohne = [...zustand.ohne].join(',');
  const d = zustand.heute = await api(`/api/heute?ohne=${encodeURIComponent(ohne)}`);
  zustand.fokus = d.fokus;
  eingangZahl(d.eingang);
  const teile = [erstPasswortHinweis(), h('h1', { text: `${d.wochentag}, ${new Date(d.datum + 'T12:00').toLocaleDateString('de-CH', { day: 'numeric', month: 'long' })}` }),
    d.erledigt_heute ? h('p', { class: 'datum' }, h('span', { class: 'geschafft', text: `${d.erledigt_heute} geschafft heute` })) : null];

  teile.push(terminKarte(d));
  teile.push(h('h2', { text: 'Das ist dran' }), jetztKarte(d.jetzt));

  if (d.neu_entscheiden.length) {
    teile.push(h('h2', { text: 'Heute neu entscheiden' }),
      h('ul', { class: 'liste' }, d.neu_entscheiden.map((e) => h('li', { class: 'zeile' },
        h('div', { class: 'text' }, e.text, h('small', { text: `geplant war ${relativ(e.tag)}` })),
        h('div', { class: 'mini-knoepfe' },
          h('button', { class: 'mini', onclick: () => aktion(e.id, { aktion: 'heute', haupt: false }, 'Für heute eingeplant.') }, 'Heute'),
          h('button', { class: 'mini', onclick: () => tagWaehlen(e) }, 'Anderer Tag'),
          h('button', { class: 'mini', onclick: () => aktion(e.id, { aktion: 'losgelassen' }, 'Losgelassen.') }, 'Loslassen'))))));
  }

  teile.push(h('h2', { text: `Heute wichtig · ${d.haupt.length} von 3` }));
  teile.push(d.haupt.length ? aufgabenListe(d.haupt) : h('p', { class: 'leer', text: 'Noch nichts gewählt. Im Eingang oder unten bei „Irgendwann“ auf „Heute“ tippen.' }));
  if (d.auch_heute.length) teile.push(h('h2', { text: 'Auch heute' }), aufgabenListe(d.auch_heute));
  if (d.geplant.length) teile.push(h('details', { class: 'klappe' }, h('summary', { text: `Geplant (${d.geplant.length})` }), aufgabenListe(d.geplant, true)));
  if (d.irgendwann.length) teile.push(h('details', { class: 'klappe' }, h('summary', { text: `Irgendwann (${d.irgendwann.length})` }), aufgabenListe(d.irgendwann, true)));
  return teile;
}

function terminKarte(d) {
  const t = d.termine;
  const kinder = [];
  if (t.ganztags.length) kinder.push(h('div', { class: 'ganztags' }, t.ganztags.map((g) => h('span', { class: 'chip warm', text: g }))));
  if (t.naechster) {
    const min = Math.round((new Date(t.naechster.beginn) - Date.now()) / 60000);
    const laeuft = min <= 0;
    const anteil = laeuft ? 1 : Math.max(0, 1 - min / 120);  // fuellt sich in den letzten 2 Stunden
    const balken = h('div', { class: `balken${min <= 20 ? ' warm' : ''}`, role: 'progressbar', 'aria-label': 'Zeit bis zum Termin', 'aria-valuenow': Math.round(anteil * 100) }, h('i'));
    requestAnimationFrame(() => { balken.firstChild.style.width = `${anteil * 100}%`; });
    kinder.push(h('div', { class: 'oben' },
      h('div', {}, h('div', { class: 'titel', text: t.naechster.titel }),
        h('div', { class: 'leise klein', text: `${tageBis(new Date(t.naechster.beginn).toLocaleDateString('sv-SE')) === 0 ? '' : tagKurz(new Date(t.naechster.beginn).toLocaleDateString('sv-SE')) + ', '}${uhrzeit(t.naechster.beginn)}${t.naechster.ort ? ` · ${t.naechster.ort}` : ''}` })),
      h('div', { class: 'bis', text: laeuft ? 'läuft' : dauerText(min) })), balken);
    const weitere = t.heute.filter((x) => x.beginn !== t.naechster.beginn);
    if (weitere.length) kinder.push(h('div', { class: 'leise klein', text: 'Danach: ' + weitere.map((x) => `${uhrzeit(x.beginn)} ${x.titel}`).join(' · ') }));
  } else if (d.kalender.eingerichtet) {
    kinder.push(h('div', { class: 'leise', text: 'Heute keine Termine mehr.' }));
  } else {
    kinder.push(h('div', { class: 'leise klein' }, 'Kalender noch nicht verbunden. ', h('a', { href: '#einstellungen', text: 'Einrichten' })));
  }
  if (d.kalender.fehler_seit) kinder.push(h('div', { class: 'leise klein', text: `Kalender gerade nicht erreichbar (seit ${uhrzeit(d.kalender.fehler_seit)}).` }));
  return h('section', { class: 'karte termin', 'aria-label': 'Nächster Termin' }, kinder);
}

function jetztKarte(k) {
  if (!k) {
    return h('div', { class: 'leer' }, 'Gerade nichts dran. ', zustand.ohne.size
      ? h('button', { class: 'knopf zart klein', onclick: () => { zustand.ohne.clear(); zeigen(true); } }, 'Weggeklickte wieder zeigen')
      : h('a', { href: '#eingang', text: 'Etwas aus dem Eingang für heute wählen?' }));
  }
  const nichtJetzt = h('button', { class: 'knopf zart', onclick: () => { zustand.ohne.add(k.schluessel); zeigen(true); } }, 'Nicht jetzt');
  const knoepfe = [];
  if (k.art === 'routine') {
    knoepfe.push(h('button', { class: 'knopf haupt', onclick: async () => {
      const r = (await api('/api/routinen')).routinen.find((x) => x.id === k.routine_id);
      const offen = r?.schritte.find((s) => !s.erledigt);
      if (offen) { await api(`/api/routine/${k.routine_id}/haken`, { schritt_id: offen.id }).catch(fehler); toast('Abgehakt.'); }
      zeigen(true);
    } }, 'Abhaken'), h('a', { class: 'knopf', href: '#routinen' }, 'Liste öffnen'));
  } else if (k.art === 'papier') {
    knoepfe.push(h('button', { class: 'knopf haupt', onclick: () => papierSchritt({ id: k.eintrag_id, text: k.titel }) }, 'Schritt erledigt'),
      h('button', { class: 'knopf', onclick: () => fokusFuer(k.eintrag_id) }, 'Starten'));
  } else {
    knoepfe.push(h('button', { class: 'knopf haupt', onclick: () => fokusFuer(k.eintrag_id) }, 'Starten'),
      h('button', { class: 'knopf', onclick: () => erledigt(k.eintrag_id) }, 'Erledigt'));
    if (k.art === 'aufgabe') knoepfe.push(h('button', { class: 'knopf', onclick: () => zerlegen({ id: k.eintrag_id, text: k.text }) }, 'Zerlegen'));
  }
  knoepfe.push(nichtJetzt);
  const art = { routine: 'Routine', papier: k.tage <= 0 ? 'Papierkram · heute' : `Papierkram · ${relativ(k.frist)}`, schritt: 'Nächster kleiner Schritt', aufgabe: 'Wichtig heute' }[k.art];
  return h('section', { class: 'karte jetzt', 'aria-label': 'Das ist dran' },
    h('div', { class: 'art', text: art }),
    k.art === 'aufgabe' ? null : h('p', { class: 'worum', text: k.titel }),
    h('p', { class: 'was', text: k.text }),
    h('div', { class: 'knopfreihe' }, knoepfe));
}

function aufgabenListe(liste, mitHeute = false) {
  return h('ul', { class: 'liste' }, liste.map((e) => h('li', {},
    h('div', { class: `zeile${e.status === 'erledigt' ? ' erledigt' : ''}` },
      h('button', { class: 'haken', 'aria-label': `${e.text} erledigt`, title: 'Erledigt', onclick: () => erledigt(e.id) }, svg(HAKEN)),
      h('div', { class: 'text' }, e.text, e.tag && mitHeute ? h('small', { text: relativ(e.tag) }) : null,
        e.schritte.length ? h('small', { text: `${e.schritte.filter((s) => s.status === 'erledigt').length} von ${e.schritte.length} Schritten` }) : null),
      h('div', { class: 'mini-knoepfe' },
        mitHeute ? h('button', { class: 'mini', onclick: () => aktion(e.id, { aktion: 'heute', haupt: true }, 'Für heute gewählt.') }, 'Heute') : null,
        h('button', { class: 'mini', title: 'Fokus-Timer starten', 'aria-label': 'Starten', onclick: () => fokusFuer(e.id) }, '▶'),
        e.schritte.length ? null : h('button', { class: 'mini', title: 'In kleine Schritte zerlegen', 'aria-label': 'Zerlegen', onclick: () => zerlegen(e) }, '✂'),
        h('button', { class: 'mini', title: 'Mehr', 'aria-label': 'Mehr', onclick: () => mehrMenue(e) }, '⋯'))),
    e.schritte.length ? h('ul', { class: 'schritte' }, e.schritte.map((s) => h('li', { class: s.status === 'erledigt' ? 'erledigt' : '' }, s.text))) : null)));
}

// ---- Eingang ------------------------------------------------------------------------
const EINGANG_TASTEN = { h: 'heute', t: 'tag', p: 'papier', z: 'zerlegen', l: 'los', e: 'erledigt' };
async function eingangAktion(e, was) {
  if (was === 'heute') return aktion(e.id, { aktion: 'heute' }, 'Für heute eingeplant.');
  if (was === 'tag') return tagWaehlen(e);
  if (was === 'papier') return papierDialog({ eintrag_id: e.id, text: e.text });
  if (was === 'zerlegen') return zerlegen(e);
  if (was === 'los') return aktion(e.id, { aktion: 'losgelassen' }, 'Losgelassen.');
  if (was === 'erledigt') return erledigt(e.id);
  return null;
}
async function ansichtEingang() {
  const d = await api('/api/eingang');
  zustand.eingang = d.eintraege;
  eingangZahl(d.eintraege.length);
  zustand.auswahl = Math.min(zustand.auswahl, Math.max(0, d.eintraege.length - 1));
  const teile = [h('h1', { text: 'Eingang' }), h('p', { class: 'leise', text: 'Alles, was dir einfällt. Einsortieren, wenn es passt.' })];
  if (d.eintraege.length > 10) teile.push(h('p', { class: 'meldung', text: 'Eingang leeren dauert meist zwei Minuten.' }));
  if (!d.eintraege.length) teile.push(h('p', { class: 'leer', text: 'Leer. Der Kopf ist frei.' }));
  const knopf = (e, was, text, taste) => h('button', { class: 'mini', onclick: () => eingangAktion(e, was) }, text, h('kbd', { class: 'nur-tastatur', text: taste }));
  teile.push(h('ul', { class: 'liste' }, d.eintraege.map((e, i) => h('li', { class: `zeile${i === zustand.auswahl ? ' gewaehlt' : ''}`, 'data-i': i, onclick: (ev) => { if (ev.target.closest('button')) return; zustand.auswahl = i; auswahlMalen(); } },
    h('div', { class: 'text', text: e.text }),
    h('div', { class: 'mini-knoepfe' }, knopf(e, 'heute', 'Heute', 'H'), knopf(e, 'tag', 'Tag', 'T'), knopf(e, 'papier', 'Papier', 'P'),
      knopf(e, 'zerlegen', 'Zerlegen', 'Z'), knopf(e, 'los', 'Loslassen', 'L'), knopf(e, 'erledigt', 'Erledigt', 'E'))))));
  return teile;
}
function auswahlMalen() {
  document.querySelectorAll('#ansicht .zeile[data-i]').forEach((z) => z.classList.toggle('gewaehlt', Number(z.dataset.i) === zustand.auswahl));
  $(`#ansicht .zeile[data-i="${zustand.auswahl}"]`)?.scrollIntoView({ block: 'nearest' });
}

// ---- Routinen -----------------------------------------------------------------------
async function ansichtRoutinen() {
  const { routinen } = await api('/api/routinen');
  const teile = [h('h1', { text: 'Routinen' }), h('p', { class: 'leise', text: 'Abhaken statt merken.' })];
  for (const r of routinen) {
    const fertig = r.schritte.filter((s) => s.erledigt).length;
    const balken = h('div', { class: 'balken', role: 'progressbar', 'aria-valuenow': fertig, 'aria-valuemax': r.schritte.length }, h('i'));
    requestAnimationFrame(() => { balken.firstChild.style.width = `${r.schritte.length ? (fertig / r.schritte.length) * 100 : 0}%`; });
    teile.push(h('section', { class: `karte routine${r.fertig ? ' fertig' : ''}${r.aktiv ? '' : ' fertig'}`, id: `routine-${r.id}` },
      h('h3', {}, r.name, h('span', { class: 'chip', text: r.fertig ? 'fertig' : (r.zeitfenster ? r.zeitfenster.replace('-', '–') : (r.mehrfach ? 'jederzeit' : '')) })),
      r.aktiv ? null : h('p', { class: 'leise klein', text: 'pausiert' }),
      balken,
      h('ul', { class: 'liste' }, r.schritte.map((s) => h('li', { class: `zeile${s.erledigt ? ' erledigt' : ''}`, role: 'checkbox', 'aria-checked': s.erledigt ? 'true' : 'false', tabindex: 0,
        onclick: () => haken(r, s), onkeydown: (ev) => { if (ev.key === ' ' || ev.key === 'Enter') { ev.preventDefault(); haken(r, s); } } },
      h('span', { class: `haken${s.erledigt ? ' an' : ''}` }, svg(HAKEN)), h('div', { class: 'text', text: s.text })))),
      h('div', { class: 'knopfreihe' },
        r.mehrfach || fertig ? h('button', { class: 'knopf klein', onclick: async () => { await api(`/api/routine/${r.id}/neu`, {}).catch(fehler); zeigen(true); } }, r.mehrfach && r.fertig ? 'Neuer Durchgang' : 'Von vorn') : null,
        h('button', { class: 'knopf zart klein', onclick: () => routineDialog(r) }, 'Bearbeiten'))));
  }
  teile.push(h('div', { class: 'knopfreihe' }, h('button', { class: 'knopf', onclick: () => routineDialog(null) }, '＋ Neue Routine')));
  return teile;
}
async function haken(r, s) {
  try {
    const st = await api(`/api/routine/${r.id}/haken`, { schritt_id: s.id, an: !s.erledigt });
    if (st.fertig && !r.fertig) toast(`${r.name}: fertig.`);
    zeigen(true);
  } catch (e) { fehler(e); }
}
async function routineDialog(r) {
  const f = (name, label, wert, attr = {}) => h('label', {}, label, h('input', { name, value: wert || '', ...attr }));
  const schritte = h('textarea', { name: 'schritte', required: true }, (r?.schritte || []).map((s) => s.text).join('\n'));
  const aktiv = h('input', { type: 'checkbox', name: 'aktiv', checked: r ? r.aktiv : true });
  const mehrfach = h('input', { type: 'checkbox', name: 'mehrfach', checked: r?.mehrfach });
  const [von, bis] = (r?.zeitfenster || '-').split('-');
  const inhalt = [f('name', 'Name', r?.name, { required: true, maxlength: 60 }),
    h('div', { class: 'zwei' }, f('von', 'Zeitfenster von', von, { type: 'time' }), f('bis', 'bis', bis, { type: 'time' })),
    f('um', 'Erinnerung um (Push)', r?.erinnern_um, { type: 'time' }),
    h('label', {}, 'Schritte, einer pro Zeile', schritte),
    h('label', { class: 'reihe' }, mehrfach, 'Mehrmals am Tag (wie „Haus verlassen“)'),
    h('label', { class: 'reihe' }, aktiv, 'Aktiv')];
  const w = await dialog(r ? 'Routine bearbeiten' : 'Neue Routine', inhalt, [['Abbrechen', null], r ? ['Löschen', 'loeschen'] : null, ['Speichern', 'absenden', 'haupt']].filter(Boolean));
  const form = $('#dialog-form');
  try {
    if (w === 'loeschen') { await api(`/api/routine/${r.id}/loeschen`, {}); toast('Routine entfernt.'); return zeigen(true); }
    if (w !== 'absenden') return null;
    const v = form.elements.von.value, b = form.elements.bis.value;
    await api('/api/routine', { id: r?.id, name: form.elements.name.value, zeitfenster: v && b ? `${v}-${b}` : null, erinnern_um: form.elements.um.value || null,
      mehrfach: mehrfach.checked, aktiv: aktiv.checked, schritte: schritte.value.split('\n').map((x) => x.trim()).filter(Boolean) });
    toast('Gespeichert.');
    return zeigen(true);
  } catch (e) { return fehler(e); }
}

// ---- Papierkram ---------------------------------------------------------------------
async function ansichtPapier() {
  const d = await api('/api/papier');
  const teile = [h('h1', { text: 'Papierkram & Fristen' }), h('p', { class: 'leise', text: 'Immer nur der nächste kleine Handgriff.' }),
    h('div', { class: 'knopfreihe' }, h('button', { class: 'knopf', onclick: () => papierDialog({}) }, '＋ Papierkram'))];
  const sonntag = plusTage((7 - new Date().getDay()) % 7);
  const naechsterSonntag = plusTage(((7 - new Date().getDay()) % 7) + 7);
  const gruppen = [['Diese Woche', (e) => e.papier.frist && e.papier.frist <= sonntag], ['Nächste Woche', (e) => e.papier.frist && e.papier.frist > sonntag && e.papier.frist <= naechsterSonntag],
    ['Später', (e) => e.papier.frist && e.papier.frist > naechsterSonntag], ['Ohne Frist', (e) => !e.papier.frist]];
  if (!d.offen.length) teile.push(h('p', { class: 'leer', text: 'Nichts offen. Neues Papier gleich hier erfassen, dann ist es aus dem Kopf.' }));
  for (const [titel, passt] of gruppen) {
    const liste = d.offen.filter(passt);
    if (!liste.length) continue;
    teile.push(h('h2', { text: titel }), ...liste.map((e) => h('section', { class: 'karte papier' },
      h('div', { class: 'oben' }, h('b', { text: e.text })),
      h('div', { class: 'leise klein' }, h('span', { class: 'chip', text: KATEGORIE[e.papier.kategorie] || 'Sonstiges' }), ' ',
        e.papier.frist ? h('span', { class: 'frist', text: `Frist ${relativ(e.papier.frist)}` }) : null,
        e.papier.betrag_rappen != null ? ` · CHF ${franken(e.papier.betrag_rappen)}` : null),
      h('div', { class: 'schritt' }, h('b', { text: 'Nächster Schritt' }), e.papier.naechster_schritt),
      h('div', { class: 'knopfreihe' },
        h('button', { class: 'knopf haupt klein', onclick: () => papierSchritt(e) }, 'Schritt erledigt'),
        h('button', { class: 'knopf klein', onclick: () => fokusFuer(e.id) }, 'Starten'),
        h('button', { class: 'knopf zart klein', onclick: () => papierDialog({ eintrag_id: e.id, text: e.text, ...e.papier }) }, 'Bearbeiten')))));
  }
  if (d.erledigt.length) teile.push(h('details', { class: 'klappe' }, h('summary', { text: `Zuletzt erledigt (${d.erledigt.length})` }),
    h('ul', { class: 'liste' }, d.erledigt.map((e) => h('li', { class: 'zeile erledigt' }, h('div', { class: 'text', text: e.text }))))));
  return teile;
}
async function papierDialog(p) {
  const kat = h('select', { name: 'kategorie' }, Object.entries(KATEGORIE).map(([k, t]) => h('option', { value: k, selected: (p.kategorie || 'sonstiges') === k }, t)));
  const inhalt = [h('label', {}, 'Worum geht es?', h('input', { name: 'text', value: p.text || '', required: true, maxlength: 500, placeholder: 'z. B. Rechnung Zahnarzt' })),
    h('div', { class: 'zwei' }, h('label', {}, 'Art', kat), h('label', {}, 'Frist', h('input', { name: 'frist', type: 'date', value: p.frist || '' }))),
    h('div', { class: 'zwei' }, h('label', {}, 'Betrag CHF (optional)', h('input', { name: 'betrag', inputmode: 'decimal', value: p.betrag_rappen != null ? (p.betrag_rappen / 100).toFixed(2) : '' })),
      h('label', {}, 'Erinnern Tage vorher', h('input', { name: 'vorher', type: 'number', min: 0, max: 30, value: p.erinnern_tage_vorher ?? 3 }))),
    h('label', {}, 'Was ist der kleinste erste Handgriff?', h('input', { name: 'schritt', value: p.naechster_schritt || '', required: true, maxlength: 200, placeholder: 'z. B. PDF im Postfach öffnen' }))];
  const w = await dialog(p.eintrag_id && p.naechster_schritt ? 'Papierkram bearbeiten' : 'Papierkram', inhalt, [['Abbrechen', null], ['Speichern', 'absenden', 'haupt']]);
  if (w !== 'absenden') return;
  const f = $('#dialog-form').elements;
  try {
    await api('/api/papier', { eintrag_id: p.eintrag_id, text: f.text.value, kategorie: f.kategorie.value, frist: f.frist.value || null, betrag: f.betrag.value,
      erinnern_tage_vorher: Number(f.vorher.value || 3), naechster_schritt: f.schritt.value });
    toast('Gespeichert. Erinnerung kommt rechtzeitig.');
    zeigen(true);
  } catch (e) { fehler(e); }
}
async function papierSchritt(e) {
  const feld = h('input', { name: 'naechster', maxlength: 200, placeholder: 'Nächster kleiner Handgriff' });
  const w = await dialog('Gut gemacht. Gibt es noch einen Schritt?', [h('p', { class: 'leise', text: e.text }), h('label', {}, 'Nächster Schritt', feld)],
    [['Abbrechen', null], ['Nein, fertig', 'fertig'], ['Speichern', 'weiter', 'haupt']]);
  try {
    if (w === 'fertig') { await api(`/api/papier/${e.id}/schritt`, { fertig: true }); toast('Erledigt. Ein Ding weniger.'); }
    else if (w === 'weiter') {
      if (!feld.value.trim()) return toast('Bitte den nächsten Schritt eintragen.');
      await api(`/api/papier/${e.id}/schritt`, { naechster_schritt: feld.value }); toast('Nächster Schritt gespeichert.');
    } else return null;
    return zeigen(true);
  } catch (err) { return fehler(err); }
}

// ---- Fokus-Timer --------------------------------------------------------------------
let tonGespielt = null;
function fokusInhalt(gross) {
  const f = zustand.fokus;
  if (!f) {
    const eintrag = zustand.fokusEintrag ? [...(zustand.heute?.haupt || []), ...(zustand.heute?.auch_heute || [])].find((e) => e.id === zustand.fokusEintrag) : null;
    const frei = h('input', { type: 'number', min: 1, max: 240, value: 20, 'aria-label': 'Minuten', class: 'frei' });
    const start = async (min) => {
      try { zustand.fokus = (await api('/api/fokus', { aktion: 'start', minuten: min, eintrag_id: zustand.fokusEintrag || undefined })).fokus; zustand.fokusEintrag = null; seiteMalen(); if (aktuell() === 'fokus') zeigen(true); } catch (e) { fehler(e); }
    };
    return h('div', { class: 'fokus' },
      gross ? h('h1', { text: 'Fokus' }) : h('h2', { text: 'Fokus' }),
      h('p', { class: 'worauf', text: eintrag ? eintrag.text : (zustand.fokusEintrag ? 'Für die gewählte Aufgabe' : 'Wie lange willst du dranbleiben?') }),
      h('div', { class: 'dauer' }, [5, 15, 25, 45].map((m) => h('button', { class: `knopf${m === 25 ? ' haupt' : ''}`, onclick: () => start(m) }, `${m} min`))),
      h('div', { class: 'knopfreihe' }, h('label', { class: 'reihe' }, frei, 'Minuten'), h('button', { class: 'knopf klein', onclick: () => start(Number(frei.value)) }, 'Start')),
      gross ? h('p', { class: 'leise klein', text: 'Tipp: Leertaste startet 25 Minuten.' }) : null);
  }
  const ring = h('div', { class: `ring${f.abgelaufen ? ' ende' : ''}`, id: gross ? 'ring-gross' : 'ring-klein' });
  ring.innerHTML = '<svg viewBox="0 0 120 120" aria-hidden="true"><circle class="spur" cx="60" cy="60" r="52"/><circle class="rest" cx="60" cy="60" r="52" stroke-dasharray="326.7" stroke-dashoffset="0"/></svg><div class="zeit">–</div>';
  const knopf = (text, aktion2, art = '') => h('button', { class: `knopf ${art}`, onclick: async () => {
    try { zustand.fokus = (await api('/api/fokus', { aktion: aktion2, minuten: 10 })).fokus; if (aktion2 === 'erledigt') toast('Erledigt. Kurz durchatmen.'); seiteMalen(); zeigen(true); } catch (e) { fehler(e); }
  } }, text);
  return h('div', { class: 'fokus' },
    gross ? h('h1', { text: f.abgelaufen ? 'Zeit um' : 'Fokus' }) : h('h2', { text: f.abgelaufen ? 'Zeit um' : 'Fokus' }),
    ring,
    h('p', { class: 'worauf', text: f.text || 'Freie Fokuszeit' }),
    f.kette_min >= 30 ? h('p', { class: 'leise klein', text: `Seit ${f.kette_min} min am Stück` }) : null,
    h('div', { class: 'dauer' }, f.abgelaufen
      ? [knopf('Erledigt', 'erledigt', 'haupt'), knopf('Noch 10 min', 'verlaengern'), knopf('Pause', 'pause')]
      : [knopf('Pause', 'pause'), knopf('Erledigt', 'erledigt', 'haupt'), knopf('＋10 min', 'verlaengern')]));
}
async function ansichtFokus() {
  zustand.fokus = (await api('/api/fokus')).fokus;
  return fokusInhalt(true);
}
function seiteMalen() {
  const seite = $('#seite');
  if (aktuell() === 'fokus') seite.replaceChildren(); else seite.replaceChildren(h('section', { class: 'karte' }, fokusInhalt(false)));
  ticken();
}
function piep() {
  if (zustand.einst && !zustand.einst.ton) return;
  try {
    const ctx = new AudioContext();
    [0, 0.35, 0.7].forEach((t, i) => {
      const o = ctx.createOscillator(), g = ctx.createGain();
      o.frequency.value = [660, 784, 988][i]; g.gain.setValueAtTime(0.0001, ctx.currentTime + t);
      g.gain.exponentialRampToValueAtTime(0.2, ctx.currentTime + t + 0.02); g.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + t + 0.3);
      o.connect(g).connect(ctx.destination); o.start(ctx.currentTime + t); o.stop(ctx.currentTime + t + 0.32);
    });
  } catch { /* ohne Ton */ }
}
function ticken() {
  const f = zustand.fokus, chip = $('#timer-chip');
  if (!f) { chip.hidden = true; document.title = 'Kompass'; return; }
  const rest = Math.max(0, Math.round((new Date(f.ziel) - Date.now()) / 1000));
  const text = rest > 0 ? mmss(rest) : 'Zeit um';
  chip.hidden = false; chip.textContent = `⏱ ${text}`; chip.classList.toggle('ende', rest === 0);
  document.title = `${text} · Kompass`;
  for (const ring of document.querySelectorAll('.ring')) {
    ring.querySelector('.zeit').textContent = text;
    ring.querySelector('.rest').style.strokeDashoffset = String(326.7 * (1 - rest / (f.geplant_min * 60)));
    ring.classList.toggle('ende', rest === 0);
  }
  if (rest === 0 && !f.abgelaufen) {
    f.abgelaufen = true;
    if (tonGespielt !== f.id) { tonGespielt = f.id; piep(); }
    seiteMalen(); if (aktuell() === 'fokus') zeigen(true);
  }
}
setInterval(ticken, 1000);

// ---- Einstellungen ------------------------------------------------------------------
async function ansichtEinstellungen() {
  const { einstellungen: e, kalender: k } = await api('/api/einstellungen');
  zustand.einst = e;
  const feld = (name, label, attr = {}) => h('label', {}, label, h('input', { name, value: e[name] ?? '', ...attr }));
  const schalter = (name, label) => h('label', { class: 'reihe' }, h('input', { type: 'checkbox', name, checked: e[name] }), label);
  const speichern = (form, felder, nachher) => async (ev) => {
    ev.preventDefault();
    const daten = {};
    for (const n of felder) {
      const el = form.elements[n];
      daten[n] = el.type === 'checkbox' ? el.checked : el.value;
    }
    try {
      const r = await api('/api/einstellungen', daten);
      zustand.einst = r.einstellungen;
      toast(r.meldung || 'Gespeichert.');
      if (nachher) nachher();
      zeigen(true);
    } catch (err) { fehler(err); }
  };
  const abschnitt = (titel, kinder, felder, nachher) => {
    const f = h('form', { class: 'karte einst' }, h('h3', { text: titel }), kinder, felder ? h('div', { class: 'knopfreihe' }, h('button', { class: 'knopf haupt klein', type: 'submit' }, 'Speichern')) : null);
    if (felder) f.addEventListener('submit', speichern(f, felder, nachher));
    return f;
  };
  const thema = (() => { try { return localStorage.getItem('kompass-thema') || 'system'; } catch { return 'system'; } })();
  return [erstPasswortHinweis(), h('h1', { text: 'Einstellungen' }),
    abschnitt('Erinnerungen aufs Handy (ntfy)', [
      h('p', { class: 'leise klein' }, 'In der ntfy-App den Server ', h('code', { text: e.ntfy_server }), ' hinzufügen und das Thema ', h('code', { text: e.ntfy_thema }),
        ' abonnieren. Unterwegs braucht das Handy dafür Tailscale.'),
      h('div', { class: 'knopfreihe' }, h('button', { class: 'knopf klein', type: 'button', onclick: async () => {
        try { await api('/api/einstellungen/testpush', {}); toast('Test-Push gesendet. Kam er an?'); } catch (err) { fehler(err); }
      } }, 'Test-Push senden')),
      schalter('push_routine', 'Routinen'), schalter('push_papier', 'Papierkram-Fristen'), schalter('push_termin', 'Termine'), schalter('push_fokus', 'Timer und Hyperfokus-Pause'),
      h('div', { class: 'zwei' }, feld('ruhe_von', 'Ruhe ab', { type: 'time' }), feld('ruhe_bis', 'Ruhe bis', { type: 'time' })),
      h('div', { class: 'zwei' }, feld('termin_vorlauf_min', 'Termin-Vorlauf (min)', { type: 'number', min: 1, max: 600 }),
        feld('hyperfokus_min', 'Pause-Hinweis nach (min)', { type: 'number', min: 1, max: 600 })),
      feld('ntfy_thema', 'ntfy-Thema', { maxlength: 64 }),
    ], ['push_routine', 'push_papier', 'push_termin', 'push_fokus', 'ruhe_von', 'ruhe_bis', 'termin_vorlauf_min', 'hyperfokus_min', 'ntfy_thema']),
    abschnitt('Google-Kalender', [
      h('p', { class: 'leise klein', text: 'Google Kalender → Einstellungen → dein Kalender → „Privatadresse im iCal-Format“ kopieren. Kompass liest nur, alle 10 Minuten.' }),
      h('label', {}, 'iCal-Adresse', h('input', { name: 'ical_url', placeholder: e.ical_url ? `gespeichert (${e.ical_url}), leer lassen = behalten` : 'https://calendar.google.com/calendar/ical/…/basic.ics', autocomplete: 'off' })),
      h('p', { class: 'leise klein', text: e.ical_url ? (k.letzter_abruf ? `Zuletzt geholt ${uhrzeit(k.letzter_abruf)}: ${k.anzahl} Termine.` : 'Noch nicht abgeglichen.') + (k.fehler_seit ? ` Nicht erreichbar seit ${uhrzeit(k.fehler_seit)}.` : '') : 'Nicht verbunden.' }),
      e.ical_url ? h('div', { class: 'knopfreihe' },
        h('button', { class: 'knopf klein', type: 'button', onclick: async () => { try { const r = await api('/api/kalender/abgleichen', {}); toast(`${r.anzahl} Termine geholt.`); zeigen(true); } catch (err) { fehler(err); } } }, 'Jetzt abgleichen'),
        h('button', { class: 'knopf zart klein', type: 'button', onclick: async () => { await api('/api/einstellungen', { ical_url: 'loeschen' }).catch(fehler); zeigen(true); } }, 'Trennen')) : null,
    ], ['ical_url']),
    abschnitt('Zerleger (Claude)', [
      h('p', { class: 'leise klein', text: 'Nur auf Knopfdruck. Gesendet wird nur der Text der Aufgabe.' }),
      h('label', {}, 'API-Schlüssel', h('input', { name: 'claude_schluessel', type: 'password', autocomplete: 'off', placeholder: e.claude_schluessel ? `gespeichert (${e.claude_schluessel}), leer lassen = behalten` : 'sk-ant-…' })),
      feld('claude_modell', 'Modell'),
    ], ['claude_schluessel', 'claude_modell']),
    abschnitt('Darstellung', [
      h('label', {}, 'Farben', h('select', { name: 'thema', onchange: (ev) => themaSetzen(ev.target.value) },
        [['system', 'Wie das Gerät'], ['light', 'Hell'], ['dark', 'Dunkel']].map(([w, t]) => h('option', { value: w, selected: thema === w }, t)))),
      schalter('ton', 'Ton am Timer-Ende'),
    ], ['ton']),
    passwortKarte(),
    h('div', { class: 'knopfreihe' }, h('button', { class: 'knopf zart', onclick: () => $('#logout').submit() }, 'Abmelden'))];
}
function erstPasswortHinweis() {
  if (document.body.dataset.erstpasswort !== '1') return null;
  return h('p', { class: 'meldung' }, 'Noch das Start-Passwort aktiv. Bitte unter ', h('a', { href: '#einstellungen', text: 'Einstellungen' }), ' ein eigenes setzen.');
}
function passwortKarte() {
  const f = h('form', { class: 'karte einst' }, h('h3', { text: 'Passwort' }),
    h('label', {}, 'Bisheriges Passwort', h('input', { name: 'alt', type: 'password', autocomplete: 'current-password', required: true })),
    h('label', {}, 'Neues Passwort (mind. 12 Zeichen)', h('input', { name: 'neu', type: 'password', autocomplete: 'new-password', minlength: 12, required: true })),
    h('label', {}, 'Wiederholen', h('input', { name: 'neu2', type: 'password', autocomplete: 'new-password', minlength: 12, required: true })),
    h('div', { class: 'knopfreihe' }, h('button', { class: 'knopf haupt klein', type: 'submit' }, 'Passwort ändern')));
  f.addEventListener('submit', async (ev) => {
    ev.preventDefault();
    if (f.elements.neu.value !== f.elements.neu2.value) return toast('Die neuen Passwörter sind verschieden.');
    try { await api('/api/passwort', { alt: f.elements.alt.value, neu: f.elements.neu.value }); f.reset(); toast('Passwort geändert.'); document.body.dataset.erstpasswort = '0'; zeigen(true); } catch (err) { fehler(err); }
    return null;
  });
  return f;
}
function themaSetzen(w) {
  if (w === 'system') document.documentElement.removeAttribute('data-theme'); else document.documentElement.dataset.theme = w;
  try { localStorage.setItem('kompass-thema', w); } catch { /* egal */ }
}
try { const t = localStorage.getItem('kompass-thema'); if (t && t !== 'system') document.documentElement.dataset.theme = t; } catch { /* egal */ }

// ---- Tastatur -----------------------------------------------------------------------
addEventListener('keydown', (ev) => {
  if (offenerDialog() || ev.metaKey || ev.ctrlKey || ev.altKey) return;
  if (ev.target.closest('input, textarea, select, [contenteditable]')) {
    if (ev.key === 'Escape') ev.target.blur();
    return;
  }
  const k = ev.key.toLowerCase();
  if (k === 'n') { ev.preventDefault(); $('#erfassen-feld').focus(); return; }
  const ziele = { 1: 'heute', 2: 'eingang', 3: 'routinen', 4: 'papier', 5: 'fokus' };
  if (ziele[k]) { location.hash = ziele[k]; return; }
  if (k === '?') { hilfe(); return; }
  if (aktuell() === 'eingang' && zustand.eingang?.length) {
    if (k === 'arrowdown' || k === 'j') { ev.preventDefault(); zustand.auswahl = Math.min(zustand.auswahl + 1, zustand.eingang.length - 1); auswahlMalen(); return; }
    if (k === 'arrowup' || k === 'k') { ev.preventDefault(); zustand.auswahl = Math.max(zustand.auswahl - 1, 0); auswahlMalen(); return; }
    if (EINGANG_TASTEN[k]) { ev.preventDefault(); eingangAktion(zustand.eingang[zustand.auswahl], EINGANG_TASTEN[k]); return; }
  }
  if (aktuell() === 'fokus' && k === ' ') {
    ev.preventDefault();
    const knopf = zustand.fokus ? [...document.querySelectorAll('#ansicht .dauer .knopf')].find((b) => b.textContent === 'Pause') : [...document.querySelectorAll('#ansicht .dauer .knopf')].find((b) => b.textContent === '25 min');
    knopf?.click();
  }
});
function hilfe() {
  const zeile = (t, w) => h('div', { class: 'zeile' }, h('kbd', { text: t }), h('div', { class: 'text', text: w }));
  dialog('Tastenkürzel', h('div', { class: 'liste' }, zeile('N', 'Gedanken erfassen'), zeile('1 – 5', 'Heute, Eingang, Routinen, Papier, Fokus'),
    zeile('↑ ↓ / J K', 'Im Eingang auswählen'), zeile('H T P Z L E', 'Heute, Tag, Papier, Zerlegen, Loslassen, Erledigt'),
    zeile('Leertaste', 'Fokus: 25 min starten / Pause'), zeile('?', 'Diese Hilfe')));
}

// ---- Auffrischen und Start ------------------------------------------------------------
setInterval(() => { if (!document.hidden && !offenerDialog()) zeigen(true); }, 60000);
document.addEventListener('visibilitychange', () => { if (!document.hidden && !offenerDialog()) zeigen(true); });
api('/api/einstellungen').then((r) => { zustand.einst = r.einstellungen; }).catch(() => {});
api('/api/fokus').then((r) => { zustand.fokus = r.fokus; seiteMalen(); }).catch(() => {});
zeigen();
if ('serviceWorker' in navigator) navigator.serviceWorker.register('/sw.js').catch(() => {});
