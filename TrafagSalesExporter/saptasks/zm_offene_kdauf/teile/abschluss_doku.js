// Abschluss 2026-10-12: Learnings, SAP-Anleitung, Router, Koordination, Wochen_Todo.
// Aufruf aus der Repository-Wurzel: node saptasks/zm_offene_kdauf/teile/abschluss_doku.js
const fs = require('fs');
function lese(f) { const s = fs.readFileSync(f, 'utf8'); return { s: s.replace(/\r\n/g, '\n'), crlf: s.includes('\r\n') }; }
function schreibe(f, o) { fs.writeFileSync(f, o.crlf ? o.s.replace(/\n/g, '\r\n') : o.s); }
function ers(f, a, b) { const o = lese(f); if (!o.s.includes(a)) throw new Error(f + ': ' + a.slice(0, 60)); o.s = o.s.replace(a, b); schreibe(f, o); }

// LEARNINGS: SAP-Abschnitt und Werkzeuge
ers('docs/rag/LEARNINGS.md',
  "| Datum | Was passiert ist | Regel | Detail |\n| --- | --- | --- | --- |\n| 2026-10-12 | ZM_OFFENE_KDAUF: Das Original lieferte",
  "| Datum | Was passiert ist | Regel | Detail |\n| --- | --- | --- | --- |\n" +
  "| 2026-10-12 | ZM_OFFENE_KDAUF bricht als Hintergrundjob in der ALV-Liste ab (GETWA_NOT_ASSIGNED, SAPLKKBL GEN_FIELD_OUT2), weil der Feldkatalog das nicht vorhandene Feld NETWR enthaelt; die Fortschreibung von ZPOWERBI_KDAUF kommt danach nie an die Reihe | Feldkatalog-Eintraege gegen die Struktur pruefen; einen Report mit Fortschreibung nach der Liste auch einmal im Hintergrund laufen lassen | `saptasks/zm_offene_kdauf/NOTES.md` Korrekturen |\n" +
  "| 2026-10-12 | `SapGuiSetFunctionSourceFromFile.vbs` oeffnete eine zweite SE37-Sitzung, weil Sitzung 1 nicht in SE37 stand; der Baustein blieb dort gesperrt, die Aktivierung meldete „Benutzer KOI bearbeitet bereits“ | Vorher Sitzung 1 mit `/nSE37` vorbereiten; sonst in der zweiten Sitzung aktivieren und sie mit `/i` schliessen | `saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md` Nachtrag 2026-10-12 |\n" +
  "| 2026-10-12 | Jobstatus per `TBTCO` nur ueber JOBCOUNT abgefragt; die Nummer gab es auch an anderen Tagen, die Ausgabe mischte fremde Jobs | TBTCO immer mit JOBNAME, JOBCOUNT und STRTDATE abfragen | diese Zeile |\n" +
  "| 2026-10-12 | ZM_OFFENE_KDAUF: Das Original lieferte");
ers('docs/rag/LEARNINGS.md',
  "| 2026-10-12 | ABAP: `WRITE ls-wert(40)` auf einen String",
  "| 2026-10-12 | Wiederholt: Bash-Heredoc mit `'...'` und `\"`-Zeichen in node-Skripten brach ab (unexpected EOF, falsche Escapes) | Aenderungsskripte immer mit dem Write-Werkzeug als Datei anlegen und mit `node datei` ausfuehren | `saptasks/zm_offene_kdauf/teile/aendern_*.js` |\n" +
  "| 2026-10-12 | ABAP: `WRITE ls-wert(40)` auf einen String");

// SAP-Anleitung: Nachtrag
const ana = lese('saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md');
ana.s = ana.s.replace(/\n*$/, '\n') +
  "\n### Nachtrag 2026-10-12 (ZM_OFFENE_KDAUF)\n\n" +
  "* **Report kopieren** (mit Textelementen, Oberflaeche, Dynpros): `SapGuiReportKopieren.vbs <Sitzung> start <Quelle> <Ziel>`, dann `... bestaetigen los`, dann sofort `SapGuiLokalesObjekt.vbs`. Das Zielfeld im Kopierdialog heisst `txtPROGRAM_GLOBAL`.\n" +
  "* **Funktionsgruppe und -baustein anlegen**: `SapGuiFbAnlegen.vbs <Sitzung> gruppe|baustein ...`, danach `SapGuiLokalesObjekt.vbs`; Parameter mit `SapGuiFbParameter.vbs <Sitzung> import|export <Name> <Typ>` (Wertuebergabe wird gesetzt), RFC-faehig mit `SapGuiFbRemote.vbs` (fest Sitzung 1).\n" +
  "* **Aktivieren** mit Liste der inaktiven Objekte: `SapGuiAktivierenListe.vbs <Sitzung>`, dann `SapGuiInaktiveLokalAktivieren.vbs <Sitzung> <Name...>`; ein Name darf mehrere Zeilen haben (REPS, CUA, Texte), das Skript verlangt seit 2026-10-12 nur noch mindestens eine.\n" +
  "* **Vor `SapGuiSetFunctionSourceFromFile.vbs` Sitzung 1 mit `/nSE37` vorbereiten**, sonst oeffnet das Skript eine zweite Sitzung, und der Baustein bleibt dort gesperrt.\n" +
  "* **Ganze Liste lesen**: `SapGuiListeAlleSeiten.vbs <Sitzung> [MaxSeiten]` blaettert mit Bild ab; `Get-SapList.ps1` sieht nur den sichtbaren Ausschnitt.\n" +
  "* **Dialoggrenze 600 s** (`rdisp/scheduler/prio_high/max_runtime`): grosse Laeufe als Job. Jobstatus per `TBTCO` immer mit JOBNAME, JOBCOUNT und STRTDATE abfragen.\n" +
  "* **Kurzdumps lesen**: `/nST22`, `wnd[0]/usr/btnTODAY` druecken (`SapGuiDruecken.vbs`), Liste mit `SapGuiGridLesen.vbs ST22 <GridId>`, Zeile oeffnen mit `SapGuiGridZeileOeffnen.vbs`, Langtext mit `SapGuiListeAlleSeiten.vbs`.\n" +
  "* `BP_JOBLOG_READ` und `RSPO_RETURN_ABAP_SPOOLJOB` sind nicht RFC-faehig; Joblisten deshalb vom Job selbst ablegen lassen (Beispiel: CMP schreibt nach `INDX(ZK)`).\n";
schreibe('saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md', ana);

// Router
ers('docs/router/sap.md',
  "| **`ZM_OFFENE_KDAUF` beschleunigen: schnelle Kopie `_OPT`, Referenzkopie `_REF`, Vergleichsreport `_CMP` in T76 ($TMP), Befund Zufallsreihenfolge im Original, Entscheide offen** |",
  "| **`ZM_OFFENE_KDAUF` beschleunigen: `_OPT` (etwa 1/35 bis 1/50 der Laufzeit, identisch), Parallelbaustein `Z_KDAUF_PAR_RECHNEN`, Korrekturschalter, Vergleich `_CMP`, alles in T76 ($TMP); Transport nach P76 offen** |");

// Koordination: Eintrag aus "Aktive Bereiche" in den Kurzindex
const k = lese('docs/AGENT_COORDINATION.md');
const zeile = k.s.split('\n').find(l => l.startsWith('| Claude | SAP T76: Report `ZM_OFFENE_KDAUF` beschleunigen'));
if (!zeile) throw new Error('Koordinationszeile');
k.s = k.s.replace(zeile + '\n', '');
const kopf = '| Agent | Bereich | Letztes Datum | Ergebnis in Kurzform |\n|---|---|---|---|\n';
if (!k.s.includes(kopf)) throw new Error('Kurzindex');
k.s = k.s.replace(kopf, kopf +
  "| Claude | SAP T76: ZM_OFFENE_KDAUF beschleunigen (Kopie OPT, Vergleich gegen REF, Parallelbaustein, Korrekturen, Nummernvergabe) | 2026-10-12 11:45 | **Abgeschlossen in T76, Reservierung frei; Transport nach P76 offen (Ingo).** OPT gegen REF identisch: Q2 2026 5.1 s statt 187 s, BI UPDATE 23.6 s statt 1227 s; Original nicht reproduzierbar (Zufallsreihenfolge, ETENR ergaenzt) und im Hintergrund nicht lauffaehig (ALV-Feldkatalog NETWR). Objekte $TMP: ZM_OFFENE_KDAUF_OPT/_REF/_CMP, FG ZKDAUF_PAR mit Z_KDAUF_PAR_RECHNEN, Z_KDAUF_NR_TEST. Repo: `saptasks/zm_offene_kdauf/`, Learnings, SAP-Anleitung, Router, Wochen_Todo. Kein Deploy |\n");
schreibe('docs/AGENT_COORDINATION.md', k);

// Wochen_Todo Zeile 93
const w = lese('projektmanagement/Wochen_Todo.tsv');
const alt = w.s.split('\n').find(l => l.startsWith('93\t'));
const sp = alt.split('\t');
sp[7] = 'Läuft';
sp[9] = "12.10.: In T76 fertig ($TMP): ZM_OFFENE_KDAUF_OPT liefert dieselbe Liste wie das Original in etwa 1/35 bis 1/50 der Zeit (Q2 2026 5 s statt 187 s, BI UPDATE 24 s statt 20 min), läuft als Job (Original bricht im Hintergrund in der ALV-Liste ab), Korrekturschalter (Vorgabe korrigiert), ETENR-Sortierung, Sammel-IDs für ZPOWERBI_KDAUF. Offen: Transport nach P76 (FG ZKDAUF_PAR + Report), Dialogprozesse P76 prüfen. saptasks/zm_offene_kdauf/NOTES.md";
w.s = w.s.replace(alt, sp.join('\t'));
schreibe('projektmanagement/Wochen_Todo.tsv', w);
console.log('ok');
