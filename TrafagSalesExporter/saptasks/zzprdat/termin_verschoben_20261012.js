// ZZPRDAT: Abnahmetest vom 12.10.2026 um zwei Wochen verschoben (Ingo 2026-10-12). Aufruf aus der Repository-Wurzel.
const fs = require('fs');
function lese(f) { const s = fs.readFileSync(f, 'utf8'); return { s: s.replace(/\r\n/g, '\n'), crlf: s.includes('\r\n') }; }
function schreibe(f, o) { fs.writeFileSync(f, o.crlf ? o.s.replace(/\n/g, '\r\n') : o.s); }
function ers(f, a, b) { const o = lese(f); if (!o.s.includes(a)) throw new Error(f + ': ' + a.slice(0, 60)); o.s = o.s.replace(a, b); schreibe(f, o); }

const hinweis = 'Abnahmetest mit Lucas, Marco, Adil und Ingo: war auf 12.10.2026 16:00 gelegt, am 12.10. um zwei Wochen verschoben (voraussichtlich Woche ab 26.10.2026, genauer Termin offen). Stand in T76 am 12.10. live geprüft: seit 07.09. unverändert, T76K912490/T76K912491 offen, BAdI Z_ZZPRDAT_UPDATE aktiv, Z_ZZPRDAT_SET als V2.';

// Transportplan
ers('saptasks/ZZPRDAT_TRANSPORTPLAN.md',
  'Stand: 2026-09-07. **Der Transport ist angelegt',
  'Stand: 2026-10-12 (Technik unverändert seit 2026-09-07). **Abnahmetest verschoben:** ' + hinweis + '\n\nStand der Technik: 2026-09-07. **Der Transport ist angelegt');

// Kontext
ers('saptasks/zzprdat-kontext.md',
  'Arbeitsstand für die Fortsetzung im CLI. Stand: 07.09.2026.\n',
  'Arbeitsstand für die Fortsetzung im CLI. Stand: 12.10.2026 (Technik unverändert seit 07.09.2026).\n\n## Nachtrag 12.10.2026: Abnahmetest verschoben\n\n' + hinweis + ' Vorbereitung für den neuen Termin: `saptasks/ZZPRDAT_TRANSPORTPLAN.md` und `docs/ZZPRDAT_Loesung_2026-09-03.docx` Kapitel „So testen Sie es selbst“.\n');

// RAG PROJECT
ers('docs/rag/PROJECT.md',
  '  V2. Befund und Messungen: `saptasks/ZZPRDAT_TRANSPORTPLAN.md` Abschnitt 6a. Einstieg bleibt\n  dieselbe Datei.\n',
  '  V2. Befund und Messungen: `saptasks/ZZPRDAT_TRANSPORTPLAN.md` Abschnitt 6a. Einstieg bleibt\n  dieselbe Datei. **Stand 2026-10-12:** Der Abnahmetest (geplant 12.10. 16:00) ist um zwei\n  Wochen verschoben, voraussichtlich Woche ab 26.10.2026; Technik in T76 unverändert.\n');

// Router
ers('docs/router/sap.md',
  '| **ZZPRDAT: Transportauftrag `T76K912490`, Objektliste, Nachtest und was vor der Freigabe offen ist** |',
  '| **ZZPRDAT: Transportauftrag `T76K912490`, Objektliste, Nachtest und was vor der Freigabe offen ist; Abnahmetest vom 12.10. um zwei Wochen verschoben** |');

// lastchange
ers('lastchange.md', 'Stand: 2026-10-12\n\n',
  'Stand: 2026-10-12\n\n## SAP ZZPRDAT: Abnahmetest verschoben, 2026-10-12\n\n- ' + hinweis + '\n- Nachgeführt: `saptasks/ZZPRDAT_TRANSPORTPLAN.md`, `saptasks/zzprdat-kontext.md`, `docs/rag/PROJECT.md`,\n  `docs/router/sap.md`, `projektmanagement/Wochen_Todo.tsv` Zeile 56 (fällig 2026-10-26).\n\n');

// Koordination
const k = lese('docs/AGENT_COORDINATION.md');
const kopf = '| Agent | Bereich | Letztes Datum | Ergebnis in Kurzform |\n|---|---|---|---|\n';
if (!k.s.includes(kopf)) throw new Error('Kurzindex');
k.s = k.s.replace(kopf, kopf + '| Claude | SAP ZZPRDAT: Vorbereitung Abnahmetest, Termin verschoben | 2026-10-12 | **Abgeschlossen, Reservierung frei.** Stand T76 live geprüft (unverändert seit 07.09.), Test vom 12.10. 16:00 um zwei Wochen verschoben (voraussichtlich ab 26.10.). Nur Doku: Transportplan, Kontext, RAG PROJECT, Router, lastchange, Wochen_Todo Zeile 56. Keine SAP-Änderung, kein Deploy |\n');
schreibe('docs/AGENT_COORDINATION.md', k);

// Wochen_Todo Zeile 56
const w = lese('projektmanagement/Wochen_Todo.tsv');
const alt = w.s.split('\n').find(l => l.startsWith('56\t'));
if (!alt) throw new Error('Zeile 56');
const sp = alt.split('\t');
sp[4] = 'ZZPRDAT: Ergebnis des Abnahmetests mit Lucas festhalten';
sp[8] = '2026-10-26';
sp[9] = '12.10.: Test (geplant 12.10. 16:00, Lucas, Marco, Adil, Ingo) um zwei Wochen verschoben, voraussichtlich Woche ab 26.10.2026, genauer Termin offen. Technik in T76 unverändert seit 07.09. (T76K912490 offen). Danach Ergebnis festhalten. saptasks/ZZPRDAT_TRANSPORTPLAN.md';
w.s = w.s.replace(alt, sp.join('\t'));
schreibe('projektmanagement/Wochen_Todo.tsv', w);
console.log('ok');
