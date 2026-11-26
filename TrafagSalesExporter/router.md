# Router — globaler Einstieg

Stand: 2026-11-26 (Qdrant-Absatz: Suchseite im Browser `Tools/Qdrant/Start-Suche.ps1`); davor 2026-11-26 (Zeile „Wo stehen wir gerade?“ nennt `projektmanagement/Wochen_Todo_Archiv.tsv`); davor 2026-11-19 (neuer Abschnitt „Vorrang 0: Token sparen“ oben, Wunsch Ingo); davor 2026-11-12 (Doku-Pruefung: Kopfstand nachgefuehrt, `projektmanagement/Wochen_Todo.tsv` in der Zeile „Wo stehen wir gerade?“ ergaenzt); davor 2026-11-05 (Absatz Agenten, feste Definitionen `.claude/agents/`, `f8752f4`); davor 2026-10-22 (Regel 6: Historie und Archiv `docs/archiv/`)

Dies ist der **einzige** globale Einstieg in die Dokumentation. Von hier fuehrt genau ein
Schritt in einen Themenast, von dort genau ein Schritt in die Detaildatei.

## Vorrang 0: Token sparen, das Werkzeug rechnet, Claude liest nur das Ergebnis

Wunsch Ingo vom 2026-11-19 (wieder auf dem kleinen Plan): **so wenig Token wie moeglich.** Diese Regeln gehen jeder
anderen Arbeitsgewohnheit vor, auch in Agenten.

1. **Suchen mit Werkzeugen, nicht mit Lesen.** Grep/Glob, `git log -S`, `qdrant-find`; Dateien nur mit `offset`/`limit`
   lesen. Nie ganze Ordner, Logs, Exporte oder grosse Doku-Dateien in den Kontext holen, um darin zu suchen.
2. **Grosse Ausgaben in eine Datei, dann filtern oder zaehlen.** `table-read ... --format csv --quiet > datei`,
   SE16/SE16N-Export, cscript-Ausgabe, Excel/CSV, Protokolle: danach `Select-String`, `Measure-Object`, `grep -c`,
   `diff`, ein kurzes Python (uv) oder `SapListeZeilen.py`. In den Chat kommt nur Treffer, Zahl oder Abweichung.
3. **Vergleichen mit `diff`/`cmp`/Pruefsumme**, nicht beide Fassungen lesen (so am 19.11. Fabios Paket geprueft).
4. **Wiederkehrende Pruefungen als Skript ablegen**, statt sie jedes Mal neu von Claude auswerten zu lassen. Vorbild:
   Fabios IC-Wochenlauf (seit 12.11.) ersetzt die woechentlichen claude.ai-Laeufe durch Skripte mit festen Regeln,
   ohne KI im Lauf.
5. **Erst Plan, dann genau den Plan ausfuehren** (Fabio 11.11.); nach einer Ablehnung nicht mit Varianten weiterprobieren.
6. **Agenten nur fuer echte Parallelarbeit**, mit knappem Auftrag; sie liefern das Ergebnis, keine Datei-Auszuege.
7. **Knapp antworten**, Statusfragen im Chat, keine Wiederholung schon gelesener Inhalte.

Die Fehler, aus denen das entstanden ist (Einzelheiten in `docs/rag/LEARNINGS.md` und
`saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md` Abschnitt 6 „Auswerten“):

| Datum | Fehler | Werkzeug statt erneuter KI-Abfrage |
| --- | --- | --- |
| 2026-11-17 | cscript-Ausgabe mit `Select-Object -First` gekappt, Exitcode 255 statt Ergebnis (Wiederholung) | Ausgabe in Datei umleiten, dann gezielt lesen |
| 2026-11-11 | `SapGuiT76Hilfe.vbs grid` kuerzt auf 400 Zeichen, liest nur geladene Zeilen; BSEG-Grid >5 Min. | `gridcols <ses> <grid> F1,F2` (nur gewaehlte Spalten) |
| 2026-11-05 | SE16 zeigt bei breiten Tabellen nur die Bildschirmspalten, Lesen Zeile fuer Zeile | `P76Se16nExport.vbs ... AUS=F1,F2` bzw. `P76Se16Export.vbs`: Tab-Datei auf dem PC, dann filtern |
| 2026-11-09 | ALV-Liste per Scripting nur Kopfzeile | `%PC` in Datei, dann auswerten |
| 2026-09-11 | `table-read` druckt nur 100 Zeilen, eine Periode sah nach genau 100 Belegen aus | `--rowcount 99999 --format csv --quiet` und `Measure-Object`/`Select-String` |
| 2026-11-05 | Feldnamen geraten, `TABLE_WITHOUT_DATA` mehrfach wiederholt | Feldnamen einmal per `DD03L` ziehen statt probieren |

**Zuerst `docs/rag/LEARNINGS.md` lesen**: alle bisher gemachten Fehler mit der Regel daraus, an
einer Stelle statt verstreut (Wunsch Ingo, 2026-10-01). Neue Fehler dort eintragen, siehe Regel 9.
*Seit 2026-11-19: die Datei hat ueber 350 Zeilen; nach dem Thema greppen (`grep -i sap docs/rag/LEARNINGS.md`) statt sie
ganz zu lesen, ausser vor einem Deploy oder SAP-Schreibschritt.*

## Auto-Modus: so bleibt er an (Wunsch Ingo 2026-10-14, nicht vergessen)

Erlaubnisregeln: `.claude/settings.local.json` (u. a. `git *`, `git push *`, `dotnet run *`, `dotnet test *`),
Umgebungsbeschreibung: `~/.claude/settings.json` (`autoMode.environment`). Sie greifen nur, wenn der Befehl genau passt:

1. Heikle Befehle einzeln absetzen: kein `;`, `&&`, `|`, kein vorangestelltes `cd`/`Set-Location`. `> datei 2>&1` geht.
2. Push nur als `git push origin <branch>` allein (2026-10-14 abgelehnt, weil mit `| tail`, PowerShell und `ls` gekoppelt).
3. Deploy nur als `dotnet run --project .tmp_tools/DeployHeadless -c Release` (Bash, Projektordner, Umleitung ins Protokoll).
4. Geschaeftsdaten von aussen (Dateien von Kollegen) vor dem Commit Ingo nennen, sonst gilt der Push als Datenweitergabe.
5. Produktiv-DB, P76-Geschaeftsdaten, AD-Personendaten nur auf ausdruecklichen Auftrag lesen.
6. Nach einer Ablehnung nicht mit Varianten weiterprobieren; anhalten und Ingo den Befehl nennen (`! <befehl>`).

**Ladeweg fuer eine Aufgabe: `router.md` -> ein Unterrouter -> eine Detaildatei.**
Mehr soll nicht noetig sein. Wer den ganzen Bestand sucht, nimmt `baum.md` — das ist ein
Vollstaendigkeitsindex zur Pruefung, keine Lesereihenfolge.

**Agenten (seit 2026-11-05, Wunsch Ingo):** Wer einen Teilauftrag an einen Agenten gibt, nimmt die passende feste
Definition aus `.claude/agents/` (`sap-t76`, `einkauf`, `finance-verkauf`, `hr`). Sie enthalten Pflichtlektuere,
fachliches Kernwissen und die Grundregeln (kein SAP-Passwort, nur T76 schreiben, keine Fachaussage ohne Beleg), damit
Agenten nicht ohne Projektwissen starten. Ergebnisse von Agenten prueft die Hauptsitzung vor der Weitergabe an Ingo;
aendert sich eine Regel in den RAG-Dateien, die Definition im selben Commit nachfuehren.

Ist dagegen unklar, in welcher Datei eine Aussage ueberhaupt steht, hilft vorab das
MCP-Werkzeug `qdrant-find`: eine semantische Suche ueber alle Markdown-Dateien,
eingerichtet in `Tools/Qdrant/`. Sie nennt Datei und Abschnitt und ersetzt weder diesen
Ladeweg noch das Lesen der Datei, denn ihre Treffer sind Ausschnitte aus dem letzten
Indexlauf. Antwortet sie gar nicht, laeuft Qdrant nicht; `Tools/Qdrant/Start-Qdrant.ps1`
startet es. Fuer Menschen gibt es dieselbe Suche im Browser: `Tools/Qdrant/Start-Suche.ps1` startet
sie unter <http://127.0.0.1:6340/> (seit 2026-11-26; die Qdrant-Oberflaeche `/dashboard` fehlt bei
dieser Installation, `Tools/Qdrant/README.md` Abschnitt 8).

## Vorrangregeln

Diese Regeln gelten vor jeder Detaildatei. Sie sind aus echten Fehlern entstanden.

1. **Direkt gepruefte Live-Fakten schlagen jede Notiz.** Eine datierte Arbeitsnotiz ist
   ein Beleg fuer den Tag ihrer Messung, nicht fuer heute.
2. **Statusfragen („ist X noch offen?") nie aus einer Arbeitsnotiz beantworten.** Gueltig
   ist `docs/Issue_Log_Konsolidiert_2026-08-12.tsv`. Am 2026-08-12 waren zwei Punkte in
   Markdown-Dateien als offen gefuehrt, die produktiv laengst erledigt waren, und ein
   hoher Punkt fehlte ganz.
3. **Bevor ein Standort um Daten oder Pflege gebeten wird: pruefen, ob die Information
   schon vorliegt oder unsere eigene Export-SQL sie nur nicht liest.** Die Queries in
   `AlphaplanExportPackage/` und `SageSpainExportPackage/` sind unsere. Das ist zweimal in
   einer Woche schiefgegangen (DE 2026-08-03, IN 2026-08-05). Ein Standort, der
   ueberfluessige Pflege geliefert bekommt, nimmt die naechste Bitte nicht mehr ernst.
4. **Fuellgrade nie mit `Spalte > 0` messen.** `StandardCost` und `PostingDate` sind
   TEXT-Spalten; in SQLite ist Text groesser als jede Zahl, das ergibt falsche 100 %.
   `CAST(... AS REAL)` verwenden und die Grundgesamtheit fachlich filtern.
5. **SAP- und HANA-Fakten nie aus Erinnerung ableiten.** Live-Werkzeuge verwenden und das
   Ergebnis nachdokumentieren. Keine Tabellen- oder Feldnamen erfinden — genau dieser
   Fehler hat bei UK-2025 und beim IT-Superlativ zugeschlagen.

   **Wer SAP bedienen soll, liest vorher `saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md`.**
   Dort steht, was ein Agent im System selbst tun kann — lesen, schreiben, aktivieren,
   Reports ausfuehren, OData pruefen — und welche Fallen dabei Stunden kosten. Ohne
   diese Datei faengt jede SAP-Aufgabe wieder bei der Frage an, ob Fernsteuerung
   ueberhaupt geht. Sie geht, und zwar ohne einen einzigen Klick.
6. **Vor jeder Aenderung, parallelen Arbeit, jedem Build und Deploy:**
   `docs/AGENT_COORDINATION.md` lesen, den eigenen Bereich eintragen und beim Abschluss
   mit Status und Nachweis aktualisieren. Eintraege mit `abgeschlossen`, `deployed`,
   `frei` oder `Historie` sind keine laufende Arbeit. Die Datei fuehrt seit dem
   2026-09-09 nur laufende Arbeit und einen Kurzindex der letzten sieben Tage; alles
   Abgeschlossene steht in `docs/AGENT_COORDINATION_HISTORIE.md` (Volltext bis 2026-09-29,
   danach die verschobenen Kurzindex-Zeilen) und ist Nachschlagewerk, nicht Pflichtlektuere.
   Verschobene Historie der Arbeitsdateien (`lastchange.md`, `rag/DEPLOYMENT.md`) liegt seit
   dem 2026-10-22 in `docs/archiv/`. Eine reine Auskunft ohne jede Aenderung
   braucht diesen Schritt nicht, siehe `CLAUDE.md` Abschnitt 3a.
7. **Arbeitsregeln, Tests und fachliche Grenzen:** `persona.md`.
8. **Ingo testet immer auf dem deployten Produktivstand, nie lokal.** Eine leere oder
   abweichende lokale `trafag_exporter.db` oder ein lokal gestarteter Dev-Server sind kein
   Beleg fuer den produktiven Zustand. Bei UI-/Datenfragen den produktiven Stand pruefen
   (Browser, Admin-Logs, `docs/rag/DEPLOYMENT.md`), nicht die lokale Kopie im Repo.
9. **Fehler werden in `docs/rag/LEARNINGS.md` protokolliert, nicht nur verstreut.** Vor jeder
   Aenderung, jedem SAP-Schritt und jedem Deploy die Datei lesen. Wer einen Fehler macht oder
   wiederholt, traegt ihn im selben Commit wie die Korrektur dort ein: eine Zeile mit Datum,
   was passiert ist, Regel daraus und Verweis auf die Fachdatei. Die Einzelheiten bleiben in der
   Fachdatei (etwa `rag/DEPLOYMENT.md`, SAP-Arbeitsweise), die Gesamtsicht steht nur dort.
10. **Erledigtes bekommt Datum und Uhrzeit, aber nur dort, wo es abgeschlossen wird** (Wunsch Ingo
   2026-10-01, damit sichtbar ist, wann was gemacht wurde, ohne die Dateien aufzublaehen): Spalte
   „Erledigt am“ in `Wochen_Todo.tsv` als `JJJJ-MM-TT HH:MM`, „Letztes Update“ im Issue-Log, in der
   Fachdatei ein Vermerk `*Erledigt JJJJ-MM-TT HH:MM:*` am betroffenen Absatz, Deploys mit Uhrzeit
   in `rag/DEPLOYMENT.md`. Keine Zeitstempel an unveraenderten Absaetzen, alte Eintraege nicht nachruesten.
11. **Deploy (Stand 2026-11-24):** Die **App sichert die Datenbank selbst** samstags 03:00 (`DatabaseBackupService`, Admin >
   Aktive Logins); das Deploy-Werkzeug sichert nur noch, wenn die neueste Sicherung aelter als sieben Tage ist (Rueckfall);
   `--backup` erzwingt, `--no-backup` unterdrueckt sie. *Davor (2026-10-13):* nur der Runner sicherte, einmal pro Woche. Zu jedem Deploy das **Monitor-Fenster** oeffnen:
   `.tmp_tools/DeployMonitor/` (Start ueber `explorer.exe ...\ZeigeLetztenDeploy.cmd`, sonst unsichtbar) zeigt
   Schritt, Fortschrittsbalken und die letzten Zeilen. Befehle und Fallen in `docs/rag/DEPLOYMENT.md` oben („Deploy-Ablauf seit 2026-10-13“).

## Themenaeste

| Ast | Wofuer | Unterrouter |
| --- | --- | --- |
| **Finance** | Finance Cockpit, Soll/Ist, Formeln, Marge, Standardkosten, Supplier, Journal, Marktsegmente, Controlling, Reiter Verkauf | `docs/router/finance.md` |
| **Standortdaten** | Exporte und Importe je Land (ES, DE, UK, IT, IN, CH/AT), Feldluecken, Ansprechpartner | `docs/router/standortdaten.md` |
| **Einkauf** | Spend, Bestellungen, Kontrakte, Supply Chain, Einkauf Interaktiv, Logistik und Logistik live, Produktgruppen, ABC/XYZ | `docs/router/einkauf.md` |
| **HR** | HR-KPI-Cockpit, Fluktuation, Absenzen | `docs/router/hr.md` |
| **Plattform** | Architektur, Deployment, Admin, Requirements, Werkzeuge, Serveranalyse, Outlook-Grenzen, Arbeitsplatzleistung und WLAN, Tempo der Webapp, Benutzerhandbuch, Reiter Netzwerk, Weltlage, Operations (Shopfloor), Trafag Projekte, Trafag Reddit | `docs/router/plattform.md` |
| **SAP** | **SAP selbst bedienen (Werkzeuge, Grenzen, Fallen)**, ABAP, OData/Gateway, ZLO03, ZZPRDAT, PPWR, Produktsparten, SAP-Kalkulation | `docs/router/sap.md` |
| **Projekt** | Agentenkoordination, Projektstatus, Roadmap, Arbeitsregeln, Aenderungsstand | `docs/router/projekt.md` |
| **Wo stehen wir gerade?** | Gesamtstand je Bereich, offene Punkte, Arbeitspakete | Ast Projekt: `projektmanagement/Status_kurz.tsv`, `projektmanagement/Wochen_Todo.tsv` (offene und in den letzten 14 Tagen erledigte Aufgaben mit Status; aeltere erledigte seit 2026-11-26 in `projektmanagement/Wochen_Todo_Archiv.tsv`), `projektmanagement/PROJEKTSTATUS.md`, fuer Finance-Punkte das Issue-Log. `docs/UEBERGABE_2026-09-08.md` ist der Stand vom 2026-09-09 (Finance-Ladelauf) und nicht mehr der aktuelle Einstieg |

## Wenn der Ast nicht klar ist

| Frage | Ast |
| --- | --- |
| „Stimmt diese Zahl im Dashboard?" | Finance |
| „Warum fehlt Feld X bei Land Y?" | Standortdaten |
| „Woran arbeite ich gerade, was ist noch offen?" | Projekt |
| „Wie bringe ich das auf den Server?" | Plattform |
| „Warum ist mein Arbeitsplatz oder die Anzeige zaeh?" | Plattform |
| „Warum ist die Webapp langsam oder zeigt lange nichts?" | Plattform, `docs/PLATTFORM_TEMPO_2026-09-28.md` |
| „Wie laufen Kunden und Umsaetze im Reiter Verkauf?" | Finance |
| „Was macht Lager oder Kommissionierung gerade?" | Einkauf (Logistik live) |
| „Was zeigt Netzwerk, Operations, Trafag Projekte oder Trafag Reddit?" | Plattform |
| „Was liefert SAP und wie?" | SAP, bei Verkaufszahlen Standortdaten |
| „Kann ich in SAP selbst etwas anlegen, aendern oder messen?" | **Ja.** `saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md`, ohne Umweg ueber einen Unterrouter |

Hilft auch das nicht weiter, ist `qdrant-find` aus dem Kopf dieser Datei der naechste
Schritt.

Vollstaendiger Dateibestand mit Einordnung: `baum.md`.
