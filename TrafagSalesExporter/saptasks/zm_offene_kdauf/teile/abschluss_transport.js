// Abschluss Transportvorbereitung 2026-10-12. Aufruf aus der Repository-Wurzel.
const fs = require('fs');
function lese(f) { const s = fs.readFileSync(f, 'utf8'); return { s: s.replace(/\r\n/g, '\n'), crlf: s.includes('\r\n') }; }
function schreibe(f, o) { fs.writeFileSync(f, o.crlf ? o.s.replace(/\n/g, '\r\n') : o.s); }
function ers(f, a, b) { const o = lese(f); if (!o.s.includes(a)) throw new Error(f + ': ' + a.slice(0, 60)); o.s = o.s.replace(a, b); schreibe(f, o); }

const n = 'saptasks/zm_offene_kdauf/NOTES.md';
ers(n, 'Stand: 2026-10-12 11:40, in T76 fertig und verglichen; Transport nach P76 und Zurückkopieren offen',
  'Stand: 2026-10-12 11:55, in T76 eingesetzt: `ZM_OFFENE_KDAUF` (ZC23) hat den neuen Quelltext, Transport `T76K912728` bereit; Freigabe und Import nach P76 offen (Ingo)');
ers(n, '## Offen für P76\n',
  '## Transport nach P76 (vorbereitet 2026-10-12 11:50)\n\n' +
  'Auftrag **`T76K912728`** „ZM_OFFENE_KDAUF schnell (ZC23, ZKDAUF_PAR)“, Aufgabe `T76K912729`, beide offen:\n\n' +
  '| Objekt | Inhalt |\n| --- | --- |\n' +
  '| `R3TR FUGR ZKDAUF_PAR` | Funktionsgruppe mit `Z_KDAUF_PAR_RECHNEN` (RFC), Paket von `$TMP` nach `ZPP1` umgehängt (RSWBO052) |\n' +
  '| `LIMU REPS ZM_OFFENE_KDAUF` | Quelltext aus `ZM_OFFENE_KDAUF_OPT.abap`, in T76 aktiv |\n' +
  '| `LIMU ADIR ... SUSH Z_KDAUF_PAR_RECHNEN` | von SAP angelegte Berechtigungsvorschlagswerte des RFC-Bausteins |\n\n' +
  'Gegenprobe nach dem Einsetzen: `ZM_OFFENE_KDAUF` gegen REF, 01.04.–30.06.2026, „alte Logik“: identisch,\n' +
  '4.5 s statt 173 s. Rücksicherung: `ZM_OFFENE_KDAUF_original.abap` im Repo, P76-Stand von Ingo auf GitHub,\n' +
  '`ZM_OFFENE_KDAUF_REF` in T76. `ZC23` ruft weiter `ZM_OFFENE_KDAUF` (TSTC), Varianten, Layouts und Jobs\n' +
  'bleiben am Programmnamen.\n\n' +
  'Für P76 zu beachten:\n\n' +
  '* **Berechtigung `S_RFC`** für Funktionsgruppe `ZKDAUF_PAR` in den Rollen der ZC23-Benutzer. Fehlt sie,\n' +
  '  rechnet das Programm richtig, aber ohne Parallelrechnung (Rückfall auf Einzelaufrufe, etwa 1/3 statt\n' +
  '  1/35 der alten Laufzeit).\n' +
  '* **Andere Werte für Erfasser und Erstelldatum** (Vorgabe „korrigiert“); `ZPOWERBI_KDAUF` enthält sie nicht.\n' +
  '* **Job `BI UPDATE`** in P76 in SM37 ansehen: Mit dem alten Programm bricht er im Hintergrund in der\n' +
  '  ALV-Liste ab (in T76 belegt).\n\n' +
  '## Offen für P76\n');
ers(n, '1. **Transport**: Funktionsgruppe `ZKDAUF_PAR` (mit `Z_KDAUF_PAR_RECHNEN` und\n   `LZKDAUF_PARTOP`) in ein Paket und einen Transportauftrag, dazu `ZM_OFFENE_KDAUF` mit dem\n   Quelltext aus `ZM_OFFENE_KDAUF_OPT.abap`. Beides zusammen transportieren; ohne Baustein\n   ist das Verhalten nicht getestet (mit `P_PTASK = 0` rechnet OPT ohne ihn).\n',
  '1. **Transport `T76K912728` freigeben und nach P76 importieren** (Ingo). *Erledigt in T76 2026-10-12 11:50:* Paket und Auftrag vorbereitet, Quelltext eingesetzt.\n');

ers('saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md', '* `BP_JOBLOG_READ` und `RSPO_RETURN_ABAP_SPOOLJOB` sind nicht RFC-faehig;',
  '* **Paket eines `$TMP`-Objekts aendern**: SE38 `RSWBO052` (`SapGuiReportSelektion.vbs`), Objekt ankreuzen und ausfuehren (`SapGuiTadirFugr.vbs` fuer Funktionsgruppen), Cursor auf die Zeile und \"Objektkatalog aendern\" (`SapGuiListeCursorTaste.vbs ... tbar[1]/btn[19]`), Paket setzen (`SapGuiPaketAendern.vbs`), neuen Auftrag anlegen (`SapGuiAuftragNeu.vbs`).\n' +
  '* `SapGuiSetReportSourceAuftrag.vbs` liest die Quelldatei seit 2026-10-12 als UTF-8 (vorher ANSI: Umlaute aus UTF-8-Dateien waeren verstuemmelt worden).\n' +
  '* `BP_JOBLOG_READ` und `RSPO_RETURN_ABAP_SPOOLJOB` sind nicht RFC-faehig;');

ers('docs/router/sap.md', 'alles in T76 ($TMP); Transport nach P76 offen** |', 'in T76 eingesetzt, Transport `T76K912728` bereit, Import P76 offen** |');

const k = lese('docs/AGENT_COORDINATION.md');
const z = k.s.split('\n').find(l => l.startsWith('| Claude | SAP T76: Transport ZM_OFFENE_KDAUF vorbereiten'));
if (!z) throw new Error('Koordination');
k.s = k.s.replace(z + '\n', '');
const kopf = '| Agent | Bereich | Letztes Datum | Ergebnis in Kurzform |\n|---|---|---|---|\n';
k.s = k.s.replace(kopf, kopf + '| Claude | SAP T76: Transport ZM_OFFENE_KDAUF vorbereitet | 2026-10-12 11:55 | **Abgeschlossen, Reservierung frei; Freigabe/Import P76 offen (Ingo).** Auftrag T76K912728 (Aufgabe T76K912729): FUGR ZKDAUF_PAR (Paket $TMP -> ZPP1), REPS ZM_OFFENE_KDAUF mit OPT-Quelltext, SUSH-Eintrag. Gegenprobe ZM_OFFENE_KDAUF gegen REF identisch, Q2 2026 4.5 s statt 173 s. Hinweise P76: S_RFC fuer ZKDAUF_PAR, ERNAM/ERDAT korrigiert, Job BI UPDATE pruefen. Kein Deploy |\n');
schreibe('docs/AGENT_COORDINATION.md', k);

const w = lese('projektmanagement/Wochen_Todo.tsv');
const alt = w.s.split('\n').find(l => l.startsWith('93\t'));
const sp = alt.split('\t');
sp[9] = '12.10.: In T76 eingesetzt: ZM_OFFENE_KDAUF (ZC23) hat den neuen Quelltext, identisch zum Original, Q2 2026 4.5 s statt 173 s, BI UPDATE 24 s statt 20 min, läuft als Job. Transport T76K912728 (FG ZKDAUF_PAR + Report) bereit. Offen: Freigabe und Import P76 (Ingo), S_RFC für ZKDAUF_PAR in den ZC23-Rollen, Job BI UPDATE in P76 prüfen. saptasks/zm_offene_kdauf/NOTES.md';
w.s = w.s.replace(alt, sp.join('\t'));
schreibe('projektmanagement/Wochen_Todo.tsv', w);

ers('lastchange.md', '## SAP T76: ZM_OFFENE_KDAUF (offene Kundenaufträge) schnell und geprüft, 2026-10-12 (nur T76, Transport offen)',
  '## SAP T76: ZM_OFFENE_KDAUF (offene Kundenaufträge) schnell und geprüft, 2026-10-12 (in T76 eingesetzt, Transport T76K912728 bereit)');
console.log('ok');
