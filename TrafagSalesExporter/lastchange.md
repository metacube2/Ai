# Last Change

Stand: 2026-08-17

WARNUNG fuer neue Sitzungen: `docs/FINANCE_FELDLUECKEN_MAILS_2026-07-31.md` Abschnitt 3 und
`docs/mails/Build-RanVijayFollowup.ps1` bitten Indien um Pflege von 1'271 Artikeln. Das ist
seit 2026-08-05 ueberholt und darf NICHT versendet werden — gueltig ist
`docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md`.

Diese Datei ist fuer tokenarme RAG-Nutzung komprimiert.

## Spanien liefert das Buchungsdatum 2026-08-17 - Skript fertig, Live-Pruefung offen

- Die spanische Export-SQL selektiert neu `f.FechaAsiento AS PostingDate` und
  `f.Asiento AS PostingDocument` aus `dbo.FacturasTB`. Bis dahin war Spanien der
  einzige Standort ohne Buchungsdatum, alle TRES-Zeilen fielen auf das
  Rechnungsdatum zurueck.
- Geaendert an allen drei Stellen, damit Voll- und Range-Export nicht
  auseinanderlaufen: `SageSpainExportPackage/SageSpainFinalExportPackage/Export-SageSpainSalesCsv.ps1`,
  `.../Run-SpainRangeExportAndUpload-AllInOne.ps1` und die byte-identische
  Spiegelung `scripts/Export-SageSpainSalesCsv.ps1`.
- **Bewusst `OUTER APPLY` mit `TOP 1`, kein `JOIN`.** Am Auszug
  `SageSpainExportPackage/v2/Sage.dbo.FacturasTB.csv` nachgezaehlt: 3'788 Zeilen
  auf 3'642 Rechnungsschluessel, 70 Schluessel mehrfach, davon 6 mit
  unterschiedlichem `FechaAsiento`. Ein `JOIN` haette fuer diese Rechnungen jede
  Verkaufszeile vervielfacht und den spanischen Umsatz still erhoeht. `OUTER`
  statt `CROSS`, damit Zeilen ohne Buchung im Export bleiben.
- BELEGT IST NUR DIE SYNTAX: alle vier erzeugten SQL-Varianten, beide Skripte mal
  `DateFilter InvoiceDate` und `LineRegistrationDate`, wurden mit
  `Microsoft.SqlServer.TransactSql.ScriptDom` als gueltiges T-SQL geparst;
  Gegenprobe mit absichtlich kaputtem SQL wird abgelehnt. Alle drei PowerShell-
  Dateien parsen fehlerfrei. Trefferquote, Schluesselrichtigkeit und Gutschriften
  sind NICHT belegt, dafuer fehlt der Lauf gegen die spanische Datenbank.
- Der Join-Schluessel `CodigoEmpresa`, `Ejercicio`, `Serie`, `Factura` ist eine
  begruendete ANNAHME und im Skript sowie im Paket-README so gekennzeichnet. Beim
  ersten Lauf in Spanien pruefen: leere `PostingDate`, Abstand zum Rechnungsdatum,
  Gutschriften (`SerieFactura = 'REC'`, `StatusAbono <> 0`) und vor allem, dass die
  ZEILENZAHL gegenueber dem Vorlauf NICHT gestiegen ist.
- NEBENBEFUND: `SageSpainFinalExportPackage.zip` war seit dem rclone-Fix veraltet,
  ihr fehlte `Resolve-RcloneExecutable`. Neu gebaut, `10'087` auf `13'005` Bytes,
  alle fuenf enthaltenen Dateien nachgeprueft byte-identisch mit dem Ordner. Wer
  bisher aus der Zip installiert hat, hatte den rclone-Fix nicht.
- Danach in der Anwendung noetig: Spanien haengt als `MANUAL_EXCEL`-Standort an
  SharePoint und hat KEINE fest verdrahtete Spaltenzuordnung im Seed, anders als
  UK und DE. Die Spalte `PostingDate` muss in den Einstellungen beim Standort
  Spanien zugeordnet werden, danach Reimport und Jahresverteilung TRES neu messen.
- Kein Anwendungscode, keine Datenbank, kein Deploy, kein Testlauf noetig.
- Details: `docs/FINANCE_ES_BUCHUNGSDATUM_2026-08-03.md` Abschnitt 8. Issue ISS-004.2.

### Nachtrag 2026-08-17: Live-Pruefung in Spanien abgeschlossen, Schluessel bestaetigt

- Mit Ingo per RDP auf dem spanischen Sage-Server live geprueft (neues,
  read-only Diagnosewerkzeug `SageSpainExportPackage/SageSpainFinalExportPackage/Analyze-SpainPostingDateKey.ps1`).
- **Erster Testlauf (10.-13.08.):** `PostingDate` bei allen 58 Zeilen leer.
  Ursache war NICHT der Join-Schluessel, sondern Buchungsverzug: das letzte
  tatsaechlich gebuchte `FechaFactura` in `FacturasTB` war `2026-07-30`, die
  Testwoche war schlicht noch nicht gebucht.
- **Zweiter Test auf einem bereits gebuchten Fenster (23.-30.07.):**
  `53` von `53` Rechnungsschluesseln treffen (`100%`). Der Schluessel
  `CodigoEmpresa`/`Ejercicio`/`Serie`/`Factura` ist damit bestaetigt richtig.
- `FacturasTB` enthaelt zwei Bewegungstypen (`TipoIngreso`). Nur `TipoIngreso = 2`
  fuehrt `Serie` (3'539 von 3'540 Zeilen gefuellt); `TipoIngreso = 1` fast nie.
  Eine ungefilterte Stichprobe nach `TipoMov`/`TipoIngreso` haette faelschlich
  nach einem falschen Schluessel aussehen lassen.
- **Bug im Skript selbst gefunden und behoben:** `Resolve-RcloneExecutable` in
  `Run-SpainRangeExportAndUpload-AllInOne.ps1` nutzte
  `Split-Path -Parent $MyInvocation.MyCommand.Path` INNERHALB einer Funktion —
  dort ist dieser Wert in PowerShell zuverlaessig `$null`. Fix: `$PSScriptRoot`,
  das auch innerhalb von Funktionen den Skriptordner liefert. Der Fehler bestand
  bereits in der Vor-Version, wurde vorher nie produktiv ausgefuehrt.
- **Zeitfenster von 7 auf 35 Tage erweitert** (`Run-SpainRangeExportAndUpload-AllInOne.ps1`
  und README): bei ~2-3 Wochen Buchungsverzug fiel eine Rechnung sonst aus dem
  taeglichen Delta-Fenster, bevor sie ein `PostingDate` bekam, und blieb dauerhaft
  leer. Laut `docs/rag/MANUAL_IMPORT.md` dedupliziert die App Spanien-Zeilen ueber
  `SourceLineId`, die neuere Delta-Zeile gewinnt — ein breiteres, ueberlappendes
  Fenster erzeugt also KEINE Duplikate.
- **Einmaliger Nachtrag fuer die Vergangenheit:** Range-Export Januar bis Mai 2026
  lokal erzeugt (`1'571` Zeilen, `1'461'263.57 EUR`), `PostingDate` bei `100%`
  der Zeilen gefuellt, per `rclone copy` nach `Import/Finance/Spanien`
  hochgeladen.
- Mailtext an Santi Gomez (`Santi.Gomez@trafag.es`, siehe `docs/ANSPRECHPARTNER.md`)
  vorbereitet, NICHT von Claude versendet: er soll die alte `-7`-Tage-Version von
  `Run-SpainRangeExportAndUpload-AllInOne.ps1` auf dem Server durch die neue
  `-35`-Tage-Version mit `PostingDate`/`PostingDocument` und dem `$PSScriptRoot`-Fix
  ersetzen.
- Offen: Santi muss die Datei serverseitig ersetzen. Danach weiterhin noetig wie
  in Abschnitt 8 beschrieben: `PostingDate`-Spaltenzuordnung im Seed fuer Spanien
  ist NICHT verdrahtet, muss in den Einstellungen manuell gesetzt werden, danach
  Reimport und Jahresverteilung TRES neu messen.
- Kein Anwendungscode, keine Datenbank, kein Deploy, kein Testlauf noetig.

## Marktsegmente mit Jahr und 3D-Analyse 2026-08-14 - produktiv deployed 21:02

- Die Seite `/marktsegmente` hat oben einen Jahresfilter, der auf Ergebnissicht,
  Pflegeliste und die Kacheln unter `Stand der Pflege` wirkt. Voreingestellt ist
  das juengste Jahr, `alle Jahre` bleibt waehlbar.
- Das Jahr einer Zeile folgt derselben Regel wie das zentrale Excel:
  Buchungsdatum, sonst Rechnungsdatum, sonst Extraktionsdatum. Die Ergebnistabelle
  hat eine Spalte `Jahr`; Jahre werden so wenig addiert wie Waehrungen.
- Die Zuordnung selbst bekommt BEWUSST kein Jahr. Tabelle `CustomerMarketSegments`,
  `MarketSegmentResolver` und der Excel-Export sind unveraendert.
- Neu im Ergebnisreiter: eine drehbare 3D-Analyse auf Basis der vorhandenen Engine
  `wwwroot/js/finance3d.js`, ohne neue Bibliothek. X ist Standort oder Segment, Z
  ist das Jahr, Y ist Umsatz, Verkaufszeilen oder Kunden. Es wird immer genau eine
  Waehrung gezeigt, und die Zeitachse zeigt immer alle Jahre.
- **Layoutfehler mit Wirkung auf ALLE Seiten behoben:** `MudMainContent` trug in
  `Components/Layout/MainLayout.razor` die Klasse `pa-4`, die per `!important` den
  Abstand zur fest positionierten Kopfleiste ueberschrieb. Die obersten rund 48
  Pixel jeder Seite lagen dadurch unsichtbar unter der Kopfleiste, auch die
  Seitentitel. Jetzt `px-4 pb-4`.
- Ebenfalls behoben: ein `MudSelect` mit einem Eintrag vom Wert leer gilt fuer
  MudBlazor als unbefuellt, deshalb stand der Text `alle` direkt ueber der
  Beschriftung. Mit `Placeholder` korrigiert in der Marktumfrage (Land, Status),
  in der Pflege (Standort), beim neuen Jahresfilter und im Finance-Pivot des
  Management Cockpits (Jahr, TSC).
- `520/520` Release-Tests gruen. Angemeldet lokal gegen `trafag_exporter.db`
  sichtgeprueft.
- Produktiv deployed am 2026-08-14 21:02, Funktionscommit `7419473`, ohne Alarm.
  Vorher-Sicherung `trafag_exporter.db.before-segment-year-20260814-205358.bak`.
  `BiDashboard.dll` SHA256
  `D1FE3189A1C37401E8CF813134E0A882AAAC03D01F7996DA2D964B54A1613AE7`, lokaler
  Release-Build und Server bitgleich. Sechs Routen HTTPS `200`, `/marktsegmente`
  waechst von `66'785` auf `68'598` Bytes. Keine Migration, kein Schemawechsel.
- Details: `docs/MARKTSEGMENTE_RAILWAY_2026-08-13.md` Abschnitt 13 und
  `docs/rag/DEPLOYMENT.md`.

## Agentenkoordination beim Sitzungsstart 2026-08-12

- Neue Root-Datei `CLAUDE.md`: Claude muss vor jeder Arbeit
  `docs/AGENT_COORDINATION.md` lesen, den eigenen Bereich eintragen,
  Reservierungen respektieren und den Abschluss nachtragen.
- Neue Root-Datei `AGENTS.md`: dieselbe Pflicht gilt fuer Codex und andere
  Agenten.
- `docs/RAG_ROUTER.md` verweist nun ebenfalls verpflichtend auf
  `docs/AGENT_COORDINATION.md`, bevor Aenderungen, parallele Arbeit, Builds oder
  Deployments beginnen.
- Hintergrund: Ein neu gestarteter Claude hatte sich trotz Nutzeranweisung nicht
  in der Koordinationsdatei eingetragen, weil zuvor keine Root-`CLAUDE.md`
  existierte.
- Alle im Rahmen dieser Sitzung beanspruchten Einkaufs-/Koordinationsdateien sind
  wieder frei; aktuell ist laut `docs/AGENT_COORDINATION.md` kein anderer Agent
  aktiv an einer reservierten Datei.

## Projektmanagement verdichtet 2026-08-14 - nur Dokumentation

- `projektmanagement/kontext.txt`, ein ChatGPT-Rohprotokoll vom 2026-05-05 bis
  2026-08-10 mit 2025 Zeilen, wurde zu `projektmanagement/PROJEKTSTATUS.md`
  verdichtet. Diese Datei ist ab jetzt die fuehrende persoenliche Aufgabenliste
  mit den IDs `PM-01` ff.
- Inhalt: sechs offene Arbeitspakete, 21 erledigte Punkte, Personenregister,
  Arbeitsregeln und Pflegehinweise. Finance-Details bleiben ausdruecklich im
  Issue-Log und werden dort nicht dupliziert.
- `docs/RAG_ROUTER.md`: Themenzeile fuer `PROJEKTSTATUS.md` ergaenzt,
  `kontext.txt` unter „Weitere Navigation" als ABGELOEST markiert.
- **Zwei Statusangaben des Protokolls waren falsch** und wurden gegen das
  Repository korrigiert. Das Einkaufsdashboard `PM-04` war dort als offen
  gefuehrt, ist aber seit dem 2026-08-12 erledigt. `PM-01` ZLO03 stand als
  „Klaerung offen", hat aber den Transport als echten Blocker: die Transaktion
  startet `Z_ZLO03_TURBO2`, dort sind nur FIX 1, 2, 4, 5 enthalten, die Fixes 10
  bis 18 wirken nicht.
- `PM-02` ZC12: Codeanalyse nachgetragen. Kein Dump, sondern stilles
  Ueberspringen mit Eintrag im Fehlerlog, weil `fmt_quan` Trailing-Nullen
  entfernt und die nackte `0` von `CA02` abgewiesen wird. Die Verifikation ist
  blockiert, weil `trace_open` ein hartes `return.` enthaelt und `p_debug`
  auskommentiert ist, es existieren also keine Trace-Logs. Vorfrage in `SE93`:
  ist `ZC12` die Transaktion zu `Z_ABGLEICH_KTSCH`?
- `PM-06` PPWR neu aufgenommen und auf die vorhandene Codex-Dokumentation
  verlinkt: 21 Merkmale und die Klassen `ZPPWR_PACKMITTEL` und `ZCOMP_STOFF` am
  2026-08-13 in T76/090 angelegt. Pilotzuordnung und CL30N-Abnahme offen, P76
  und Massenpflege gesperrt.
- Kein Anwendungscode, kein Build, kein Test, kein Deploy.

## Marktumfrage in der Anwendung 2026-08-13 11:58 - PRODUKTIV mit Daten

- Commit `1371260` deployed, `517/517` Tests. DLL `4.560.384` Bytes, SHA256
  `24B007AC818A247046FDC6B73A44C0B0FB3AF50A5C4C72B2736CD7ACFABA0416`, bitgleich. Backup
  `trafag_exporter.db.before-market-segments-20260813-114437.bak`.
- Neue Tabelle `MarketSurveyEntries` und dritter Reiter `Marktumfrage` unter
  `Finance Cockpit > Marktsegmente`. Alle Umfragespalten sind in der App pflegbar, damit
  `Railway_MarketSurvey_TSC_2026_05.xlsx` entfallen kann.
- Menge und Preis sind bewusst TEXT: die Quelle enthaelt `500-600 pcs` und `15k EUR` neben
  `CHF 45`, obwohl die Spalte "In CHF" heisst.
- Verknuepfung zu einem Verkaufskunden ist OPTIONAL. 90 der 269 Zeilen sind Interessenten
  ohne Umsatz; ein Pflichtfeld haette sie verworfen.
- **Import AUSGEFUEHRT am 2026-08-13 nach Freigabe durch Ingo: 269 Umfragezeilen
  geschrieben**, read-only nachgeprueft `269` Zeilen, `179` verknuepft, `13` Laender,
  `240` Kunden. Statusverteilung: leer 142, `Existing Customer` 71, `No Potential` 25,
  `Opportunity` 19, `New` 12.
- Die 56 Zeilen mit `No Potential`, `Opportunity` oder `New` belegen die Notwendigkeit der
  optionalen Verknuepfung: ein Pflichtfeld haette genau diese Interessenten verworfen.
- Hinweis zu zwei Zaehlarten: der Prueflauf meldete 236 Kunden und 12 Laender, die Datenbank
  240 und 13. Die Datenbank zaehlt einen leeren Landeswert als eigene Gruppe und gruppiert
  Kunden ohne Beachtung der Gross-/Kleinschreibung anders. Kein Datenverlust.
- Der Import bricht ab, wenn fuer dieselbe Umfrage schon Zeilen existieren, damit ein
  zweiter Lauf keine Doppel erzeugt und keine in der App gepflegten Aenderungen verdeckt.
- `Railway_MarketSurvey_TSC_2026_05.xlsx` bleibt bis zur Gegenpruefung in der Anwendung
  liegen und wird erst danach archiviert.
- Fachdokument: `docs/MARKTSEGMENTE_RAILWAY_2026-08-13.md`.

## Marktsegmente Vorschlag gegen Bestaetigung 2026-08-13 11:14 - PRODUKTIV

- Commit `ecaae3d` deployed, `507/507` Tests. DLL `4.479.488` Bytes, SHA256
  `0A3EF0C563C69705AE46059AD72FCE5CD98FA069E87F6FA1B4E337C23910A87C`.
- Nur BESTAETIGTE Zuordnungen erscheinen im zentralen Excel; `MarketSegmentResolver` filtert
  per Standard auf `IsConfirmed`. Unbestaetigte sind maschinelle Vorschlaege und damit keine
  Aussage.
- **Danach importiert: 173 unbestaetigte Vorschlaege** ueber
  `.tmp_tools/ImportRailwayProposals --apply`, read-only nachgeprueft
  `IsConfirmed=0: 173 Zeilen` ueber acht Standorte. Groesste Brocken: Faiveley Transport
  Italia TRCH mit 693 Verkaufszeilen, RICA TRIT 164, CAF TRES 144, Medha 141.
- Neuer Reiter `Ergebnis` mit Umsatz je Segment, Land und Waehrung plus Pflegestand.
  Waehrungen werden bewusst nicht ueber Laender addiert.
- BEHOBEN, echter Fehler: der Filter "nur zugeordnete" holte erst die obersten 2.000 Kunden
  nach Zeilenzahl und filterte danach; ein zugeordneter kleiner Kunde von rund 4.900 fiel
  still aus der Liste. Regressionstest deckt den Fall ab.
- BEHOBEN: die Filterauswahl startete auf einem Wert, der leer sein kann. Ein leerer
  Standardfilter sieht wie ein Defekt aus, auch wenn er richtig rechnet.

## Marktsegment Railway 2026-08-13 09:00 - PRODUKTIV

- Commits `488cc42` (Code) und `07356a9` (Doku) sind deployed. `500/500` Release-Tests.
- DLL `4.431.360` Bytes, SHA256
  `9B5A3039414C12679C0AB8DF3C837C6C2EA7953B29516F036118365E68174854`, lokal und Server
  bitgleich. Backup `trafag_exporter.db.before-market-segments-20260813-084731.bak`.
- Wirknachweis mit Vorher-Messung: `MarketSegmentResolver`, `CustomerMarketSegment`,
  `MarketSegmentPageService`, `Market Segment Source` und `marktsegmente` fehlten im
  Prueflauf und sind danach enthalten.
- **`/marktsegmente` liefert 329.931 Bytes.** Das ist mehr als Erreichbarkeit: Seiten hinter
  dem Finance-Unlock liefern sonst rund 69.000 Bytes Passwortpanel, die Seite rendert also
  wirklich. Sie liegt bewusst unter Finance und ist NICHT auf Admins beschraenkt.
- Additive Migration produktiv bestaetigt: Tabelle `CustomerMarketSegments` und Index
  `UX_CustomerMarketSegments_Tsc_Customer` vorhanden, 0 Zuordnungen, Menueeintrag
  `market-segments` vorhanden, `CentralSalesRecords` unveraendert 97.537 Zeilen.
- Solange niemand zuordnet, bleiben die beiden Excel-Spalten leer. Das ist beabsichtigt.
- Anleitung fuer den Vertrieb: `docs/Anleitung_Marktsegmente_Vertrieb_2026-08-13.docx`.
- OFFEN: angemeldeter Sichtprueflauf und die erste echte Zuordnung durch Patrik.

## Marktsegment Railway im zentralen Excel 2026-08-13 - Code fertig, Deploy siehe oben

- Neue Tabelle `CustomerMarketSegments` mit Schluessel TSC plus Kundennummer und eindeutigem
  Index. `CustomerNumber` ist produktiv in allen neun Standorten zu 100 % gefuellt (4.888
  verschiedene), taugt also als Schluessel; Namen kollabieren beim Abgleich.
- `Services/MarketSegmentResolver.cs` ist rein und statisch. BEWUSST kein Rueckfall auf
  `CustomerIndustry`: das Feld ist bei CH/AT/DE/ES/UK/US zu 0 % gefuellt und nutzt sonst je
  Standort eine eigene Taxonomie. Ohne Zuordnung bleibt die Spalte leer.
- Zwei neue Excel-Spalten `Market Segment` und `Market Segment Source` als Position 50/51
  AM ENDE. Ein Einschub in der Mitte waere still toedlich, weil der Nachweis Blattformeln
  auf Spaltenpositionen enthaelt. Ein Kopfzeilentest prueft vier Ankerpositionen.
- `491/491` Release-Tests gruen (vorher 478, 13 neue).
- Entscheid Segment am KUNDEN, nicht am Produkt: von 105 zugeordneten Bahnkunden kaufen 91
  hoechstens drei Produktfamilien. Die Produktkuerzel der Umfrage sind dagegen
  Standardfamilien quer durch alle Branchen (`NAT` 7.417 Zeilen, `8252` 6.217).
- Vorschlagslisten erzeugt: `docs/Railway_Segment_Vorschlag_2026-08-12.xlsx` mit 312 Zeilen
  und `docs/Railway_Kundenpruefung_Patrik_2026-08-13.xlsx` mit den 30 mengenstaerksten.
  Die Top 30 decken 2.819 von 4.481 Zeilen ab (63 %). Vertrauen korreliert NICHT mit Menge:
  die 21 sicheren Treffer decken nur 5,9 % ab.
- OFFEN: Pflegemaske, Uebernahme der bestaetigten Liste, Deploy.

## Datenzufluss und Marktsegment 2026-08-12 nachmittags

- **CH/AT Zufluss behoben.** Ursache war NICHT der Export, sondern SAP: der Report
  `Z_TRAFAG_DACH_EXPORT` lief nur manuell, deshalb lieferte travp762 seit dem 28.07.
  unveraendert 48.932 Zeilen. Ingo hat den Batchjob ZSCHWEIZ auf taeglich gestellt;
  `all8.xlsx` von 14:39 belegt die Wirkung am selben Tag mit CH 48.276 und AT 1.839,
  zusammen 50.115, August 2026 erstmals vorhanden. Weder Verdoppelung noch Verlust von 2025.
- **FR Zufluss: Quelle ist leer, nicht unsere Kette.** `FR01_P` zeigt Rechnungen und
  Lieferscheine nur bis 30.07., Auftraege bis 07.08., Entwuerfe bis 11.08. und die sind
  alle ObjType 20, also Wareneingaenge. Frankreich arbeitet, liefert und fakturiert aber
  nicht. Kein zweites Schema aus dem B1-Upgrade, `SYS.SCHEMAS` kennt nur `FR01_P`.
  Die taeglich frischen TRFR-Dateien in SharePoint sind UNSERE Exportausgaben.
- **FALLE: `ReplaceForSiteAsync` loescht alle Zeilen des Standorts und schreibt, was die
  Quelle liefert, ohne Mindestmengenpruefung.** Eine leere Antwort mit HTTP 200 wuerde
  CH/AT leeren. Plausibilitaetsgrenze ist vorgeschlagen, aber NICHT gebaut.
- **Legendenzeile praezise lokalisiert.** DB und alle acht Standort-CSVs haben 97.537
  Zeilen und sind sauber; die zentrale Audit-CSV und `all8.xlsx` haben 97.538 und
  enthalten die Zeile mit `extraction date 28.07.2026`. Sie entsteht erst im
  Konsolidierungsschritt, `Finance | Include = FALSE`, wertmaessig harmlos, erscheint aber
  als zehnter Phantom-Standort. Pruefstelle `Services/ConsolidatedExportService.cs`.
- **Gesamtexport ohne Sales Type.** `all8.xlsx` hat keine Spalte fuer `SalesType` und keine
  Trafag-Sachnummer. Folge: Indien sieht im Excel nach 11,8 % Lieferantenpflege aus,
  waehrend die App 94,1 % ueber den Sales Type klassifiziert. Genau diese Fehleinschaetzung
  fuehrte am 05.08. zur gegenstandslosen Pflegebitte an Indien.
- **Neue Anforderung Railway.** `Railway_MarketSurvey_TSC_2026_05.xlsx` ist eine
  Marktumfrage, keine Mappingtabelle: `Material Number` 0 von 269 gefuellt, keine
  Kundennummer, Produktkuerzel nur 90 von 269. Namensabgleich trifft 151 von 234 Kunden,
  produziert aber Fehltreffer wie BROT auf K.S. & BROTHERS. Die vorhandene Spalte
  `Customer Industry` ist fast leer, CH/AT/DE/ES/UK/US bei 0 %, `Railway` steht auf 6
  Zeilen. Vorgehen und offene Fachentscheide: `docs/Issue_Log_Konsolidiert_2026-08-12.tsv`
  Zeilen ISS-014 und ISS-014.1.

## Issue-Log konsolidiert und MD-Bereinigung 2026-08-12

- Neue einzige Statusquelle: `docs/Issue_Log_Konsolidiert_2026-08-12.tsv`, 12 Issues mit
  Unterpunkten in den Spalten des Issue-Logs. Begruendung und Fallen:
  `docs/FINANCE_OFFENE_PUNKTE_2026-08-12.md`.
- Live-Messung korrigiert drei Angaben aus aelteren MDs: die Laendercode-Normalisierung ist
  deployt UND reimportiert (Spanien zeigt ISO-2), die verwaiste Legendenzeile ist aus den
  Produktivdaten verschwunden (genau neun TSC-Werte), und die Supplier-Quote lautet
  18.241 von 96.298 statt 17.930 von 95.396.
- NEU AUFGENOMMEN, hoch: **Datenzufluss TR AT, TR CH und TR FR steht seit 2026-07-31.** Am
  2026-08-12 unveraendert: AT 1.790 Zeilen / letzter Beleg 28.07., CH 47.142 / 29.07.,
  FR 2.598 / 30.07. Naechster Schritt ist die Exportlauf-Spur im Daten-Heartbeat, nicht eine
  externe Anfrage.
- FALLE fuer eigene Abfragen: `StandardCost` und `PostingDate` sind TEXT-Spalten. `> 0`
  liefert in SQLite fuer jede Zeile wahr und damit falsche 100 %. Richtig mit CAST gemessen:
  FR 51,7 %, DE 68,7 %, ES 81,0 %, US 90,0 %, UK 93,5 %, IT 95,7 %, CH 96,6 %, IN 99,4 %,
  AT 99,9 %.
- UEBERHOLT-Blöcke gesetzt in `docs/FINANCE_FELDLUECKEN_MAILS_2026-07-31.md` (nicht
  versenden), `docs/FINANCE_BACKFILL_UK_ES_2026-07-28.md` (UK-Teil),
  `docs/FINANCE_ISSUE_LOG_ANDREAS_2026-07-28.md` (vier Statusangaben) und im
  Supplier-Abschnitt von `docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md`.

## Einkauf Produktgruppen SAP-only 2026-08-12 - produktiv abgeschlossen

- `ZDISPO_GRPSet` und `ZDISPO_SPARTSet` liefern produktiv HTTP 200 mit `45`
  beziehungsweise `22` Zeilen.
- Der ueber die produktive Anwendung gestartete Einkauf-Delta endete um
  10:03:42 MESZ mit `Success` und `SAP-Produktgruppen=45`.
- Read-only DB-Nachweis: `45` Mappingregeln insgesamt, `45` mit
  `Source = SAP OData: ...`, `0` Nicht-SAP-/Excel-Regeln.
- Spend-Aufriss und Materialdisposition liefern HTTP 200. Excel ist weder
  Laufzeitquelle noch Fallback noch aktive Cachequelle.
- SAP-Nacharbeit ohne Betriebsblockade: Texte fuer `D1`/`D5` und
  SEGW-Composite-Key `DISPO_KZ + DISPO`.
- Details: `docs/PURCHASING_PRODUCT_GROUP_SAP_DIRECT_2026-08-11.md`.

## Andreas-Nachtrag lokale Standardkosten 2026-08-12 10:23 - PRODUKTIV

- Commit `fc5ae75` ist deployed. `478/478` Release-Tests am Deploytag gruen.
- `BiDashboard.dll` `4'364'800` Bytes, SHA256
  `BC566BB9AF27805524583E293D604481E560FD5D3DDEA8D8F75DC76B19D0BAF4`, lokal und
  Server bitgleich. Backup
  `trafag_exporter.db.before-andreas-local-20260812-101429.bak`, `345'202'688` Bytes.
- Wirknachweis mit Vorher-Messung: `IsConfirmedLocalMaterial`, `LocalSupplierRows`
  und `Standardkosten der lokalen Gesellschaft` fehlten vor dem Publish und sind
  danach enthalten. Das Wort `Lokal` allein waere ein Falschtreffer gewesen, weil
  `Lokaler Standardpreis` schon vorher in der DLL stand.
- Produktivbedingung geprueft: `SupplierFallbackMode=ChPlantMaster`, sonst greift die
  Regel nicht. Wirkung nach dem Deploy reproduziert: 12.023 lokale Zeilen, davon
  6.749 mit Standardpreis.
- Offen: angemeldeter Sichtprueflauf im Cockpit; die Route liegt hinter dem
  Finance-Unlock. Detail:
  `docs/FINANCE_ANDREAS_BESCHLUSS_LOKALE_STANDARDKOSTEN_2026-08-11.md`.

## Andreas-Nachtrag lokale Standardkosten 2026-08-11 - committed, Deploy siehe oben

- Baseline vor der Aenderung: Commit `369d675`.
- Einzelbestaetigter Beschluss aus dem Meeting, Transkript 06:31-07:16:
  CH-MARC-Treffer = intern/TR_AG; sicherer Nichttreffer = `Lokal` und verwendet
  die Standardkosten der jeweiligen Gesellschaft.
- Schutzgrenzen: expliziter Supplier und Sales Type behalten Vorrang; fehlende
  Materialnummer, fehlende TSC oder leerer MARC-Cache bleiben `Unklar`; Alt-Modus
  MBEW bleibt unveraendert.
- Produktiv read-only: 12.023 lokale Nichttreffer, davon 6.749 mit positivem
  Standardpreis; 5.274 ohne Standardpreis und 110 ohne Materialschluessel.
- Cockpit zeigt Lokal separat; Excel, Cockpit und Finance-Training verwenden dieselbe
  Regel. `87/87` gezielte Margentests, Lokalisierungstest und `478/478`
  Gesamttests im Release-Lauf gruen.
- Separater Commit nach Baseline `369d675`; deployed am 2026-08-12 10:23, siehe den
  Abschnitt oben. Detail:
  `docs/FINANCE_ANDREAS_BESCHLUSS_LOKALE_STANDARDKOSTEN_2026-08-11.md`.

## Gesamtdeploy 2026-08-11 15:51 - produktiv

- Gesamter aktueller Anwendungsstand produktiv deployed; `471/471` Release-Tests.
- Konsistentes Vorher-Backup:
  `trafag_exporter.db.before-all-current-20260811-145332.bak`, 340.455.424 Bytes.
- Server-DLL `4.362.752` Bytes, SHA256
  `2A5DBC034891F5B5D3FD1EE04C123A989CA987B5020CE04A0FE5161D037177F4`, lokal/server
  bitgleich.
- Supplier-Fallback produktiv: `ChPlantMaster`, 66.049 MARC-Materialien Werk 1100,
  alle 63.550 MBEW-Schluessel enthalten; 96.298 Sales-Zeilen unveraendert.
- Neun Routen liefern HTTP 200, darunter Settings, Management, Einkauf und Logistik.
- Einkaufs-SAP-only-Code ist ebenfalls live. Bekannter externer Blocker bleibt:
  `ZDISPO_GRP`/`ZDISPO_SPART` fehlen in SAP. Die 45 alten Excel-Regeln sind noch
  gespeichert, werden aber nicht mehr verwendet; Namen/Refresh bleiben bis zur
  SAP-Aktivierung eingeschraenkt.
- Vollnachweis: `docs/DEPLOY_GESAMTSTAND_2026-08-11.md`.

## Supplier-Fallback CH-Werkstamm 2026-08-11 - produktiv

- Neuer Default: Fremdstandort ohne Supplier/Sales Type prueft die normalisierte
  Trafag-Materialnummer gegen `MARC`, Werk 1100. Treffer = `Intern / TR_AG`.
- Unter `Admin Bereich > Settings` auf den alten MBEW-1100-Fallback umschaltbar;
  Einstellung ist DB-persistent und Teil des Konfigurationstransfers.
- Eigener atomarer Cache `GroupMaterialMasters`; bei leerem Cache automatischer
  Rueckfall auf MBEW. Dashboard, Pruefbuch und Excel rechnen identisch.
- Messung vor Deploy: +720 Zeilen / +392 Materialien intern, davon 674 Zeilen TRIT;
  0 bisherige Treffer gehen verloren. Produktiver Cache nach Deploy: MARC 66.049,
  MBEW 63.550.
- Nachweis: 471/471 Tests gruen; Details in
  `docs/FINANCE_SUPPLIER_FALLBACK_UMSCHALTER_2026-08-11.md`.
- Produktiv deployed; die fehlenden SAP-Sets blockieren weiterhin nur die
  Einkaufs-Produktgruppennamen und deren Refresh, nicht den Supplier-Fallback.

## Aktueller Deploy 2026-08-11 11:23

- **Admin-Menues produktiv zusammengefuehrt:** Es gibt genau eine aeussere Root-Gruppe
  `Admin Bereich`. Darunter liegen Aktive Logins, Standorte, Transformationen, Finance Regeln,
  Settings, Menuestruktur und Logs. Der alte Unterpunkt `Finance Cockpit > Admin` ist weg.
  Neue DBs, Reset und die gezielte Migration der alten Standardstruktur sind durch zwei neue
  Tests abgesichert; produktive `NavigationMenuItems` nach dem Deploy read-only bestaetigt.
- **FPV-Pausenspiel produktiv aktualisiert:** Der fruehere Artilleriekern ist durch direkte
  FPV-Drohnensteuerung ersetzt. Der Code ist deployed, der Reiter bleibt gemaess
  `Pause:Enabled=false` standardmaessig ausgeblendet. Manueller Spielgefuehl-Test bleibt offen.
- **Deploynachweis:** `461/461` Release-Tests, `28/28` FPV- und `18/18` MOD-Probes gruen.
  DLL `4'332'032` Bytes, SHA256
  `D1A82215B25A3D5A86E74EDFBD11F7E5E810E2A2B77A739C5C550B74D19FD7AB`, lokaler Build und
  Server bitgleich. Sechs HTTPS-Routen liefern 200, keine Datei verschwunden. Konsistentes
  Vorher-Backup `trafag_exporter.db.before-admin-menu-merge-20260811-112250.bak` angelegt.
  Details: `docs/rag/DEPLOYMENT.md`, `docs/ADMIN_MENUE_ZUSAMMENFUEHRUNG_2026-08-11.md`,
  `docs/PAUSENSPIEL_STUFE1_2026-08-07.md`.

## Offene Punkte (nicht erledigt)

- **EINKAUF-PRODUKTGRUPPEN DIREKT AUS SAP: PRODUKTIV DEPLOYED,
  SAP-AKTIVIERUNG FEHLT (2026-08-11).** App-Start importiert keine `zdispo*.xlsx` mehr;
  Full Load und Delta lesen `ZDISPO_GRP`/`ZDISPO_SPART` direkt aus SAP und
  ersetzen den Cache atomar. Spend-Aufriss und Supply Chain akzeptieren nur
  Quelle `SAP OData: ...`; manuelle/Excel-Altzeilen sind wirkungslos. `464/464`
  Tests im Teilstand, `471/471` im Gesamtrelease gruen. Produktives `$metadata`:
  HTTP 200, 60 EntitySets, aber die beiden ZDISPO-Sets fehlen. Der Nutzer hat den
  Deploy trotzdem freigegeben; Produktgruppennamen fehlen daher bis zur Aktivierung
  und Nacht-Deltas koennen daran scheitern. SAP-Methodenruempfe,
  SEGW-Schritte und Abschlussreihenfolge:
  `docs/PURCHASING_PRODUCT_GROUP_SAP_DIRECT_2026-08-11.md`.

- **VERWAISTE LEGENDENZEILE STEHT WEITER IN DEN PRODUKTIVDATEN** (Nebenbefund aus dem
  UK-Wertfix, am 2026-08-11 im neuen Export erneut bestaetigt): eine Zeile mit
  `TSC = "Subsidiary abbreviation / company identifier"`, Jahr 2026, Wert `0.00`. Der Codefix
  `9c0451e` verhindert nur den NEUIMPORT. Entfernen wird er sie nie, weil Manual-Importe den
  Bestand je TSC ersetzen und diese TSC kein Import je anfasst. Wertmaessig harmlos, aber sie
  taucht in JEDER TSC-Gruppierung als eigener „Standort" auf. Aufraeumen braucht ein gezieltes
  `DELETE` auf `CentralSalesRecords` und ist bewusst ein eigener Schritt, weil er die
  Produktivdatenbank beruehrt.
- **PAUSENREITER STANDARDMAESSIG AUS, deployt 2026-08-10 07:05** (Commit `8e09774`,
  `459/459`). `Pause:Enabled` startet auf `false`: kein Menueintrag links, und `/pause`
  zeigt nur den Hinweis statt das Spiel zu laden. Einschalten unter **Admin > Settings**,
  der Schalter schreibt das Kennzeichen nach `appsettings.json` (Muster von
  `ShowWalkingLabFigure`). Die Navigation nutzt den vorhandenen `_hiddenKeys`-Haken aus
  `NavMenu.razor`, der Menueintrag bleibt also in der Datenbank und laesst sich
  zusaetzlich ueber die Menuestruktur-Seite dauerhaft ausblenden.
  Produktiv belegt: Startseite ohne `Pause` (mit `Einkauf` als Gegenprobe), `/pause` mit
  Ausgeschaltet-Hinweis und ohne Szenen-Element. NICHT belegt: dass der Schalter unter
  Settings rendert — die Seite liegt hinter dem Admin-Passwortpanel.
- **FALLE: was ein Admin-Schalter nach `appsettings.json` schreibt, ist beim naechsten
  Deploy weg.** `appsettings.json` ist Build-Ausgabe und wird vom Publish durch den
  Repository-Stand ersetzt (gemessen 2026-08-10: Server stand auf `Pause:Enabled = true`,
  danach `false`). Betrifft `Pause:Enabled` und `LandingPage:ShowWalkingLabFigure`.
  Dauerhaft gewollte Einstellungen gehoeren ins Repository. Vor dem Publish lohnt der
  Vergleich Server gegen Repo — dieselbe Fehlerklasse wie die ZDISPO-XLSX.
- **FALLE BEIM DEPLOY 2026-08-07, selbst hineingelaufen: `dotnet publish` NIE ueber das
  Bash-Werkzeug auf den UNC-Pfad.** Git Bash macht aus
  `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$` still das lokale Verzeichnis
  `C:\trch-webapp-bidashboard.trafagch.local\BiDashboard$`. Der Publish meldete Erfolg,
  legte 120 Dateien auf der lokalen Platte ab, und der Server bekam NICHTS — waehrend
  `app_offline.htm` dort bereits gesetzt war, die Anwendung also stillstand.
  Erkennungsmerkmal: die letzte Publish-Zeile nennt `C:\trch-webapp-...` statt
  `\\trch-webapp-...`. Aufgefallen ist es am SHA256-Vergleich Server gegen
  `bin/Release/net8.0/BiDashboard.dll`; ohne diesen Vergleich waere ein Deploy gemeldet
  worden, der nie stattgefunden hat. Publish IMMER aus PowerShell. Streuverzeichnis
  entfernt (enthielt keine Datenbank), Produktion war zu keinem Zeitpunkt veraendert.
- **Historischer Pausenspiel-Stand vom 07.08.; am 11.08. durch FPV ersetzt:** Stufe 1 war
  gebaut und deployed, aber nie in einem Browser gelaufen. Rundenkampf mit Drohnen in 3D
  (three.js, liegt bereits global),
  Hotseat oder gegen den Rechner, Namen im Startbildschirm. Rein additiv: neue Route,
  eigenes JS-Modul, kein Datenzugriff, kein Serverzustand — Namen und Bestenliste
  liegen im `localStorage` und verlassen den Browser nicht. Ausblendbar ueber
  `Pause:Enabled` in `appsettings.json` oder `IsVisible` am Menueintrag `pause-game`.
  Ton ist standardmaessig AUS; Geraeusche sind synthetisiert, Musik spielt eine
  `.mod`-Datei ueber `wwwroot/js/modplayer.js` (selbst geschriebener ProTracker-
  Abspieler — es liegt bewusst KEINE Bibliothek und KEINE Musikdatei im Repo).
  Geprueft kopflos in Node: `Tools/PauseGame.Probe/probe.mjs` und `modprobe.mjs`,
  je 18 Pruefungen gruen; `dotnet test` unveraendert `455/455`.
  Deploy verifiziert: DLL `4'325'376` Bytes / SHA256 `7F4FAB94...`, bitgleich mit dem
  lokalen Release-Build; Produktiv-DB und alle 16 `.bak` unveraendert; `/pause` liefert
  HTTPS `200` mit dem Host-Element der Szene und ohne den Ausgeschaltet-Hinweis; beide
  Spielmodule werden ausgeliefert.
  WAS DAS NICHT BELEGT: es gibt hier keine Browser-Automatisierung, also ist NICHT
  geprueft, ob die Szene erscheint, die Kamera brauchbar ist oder die Musik
  unterbrechungsfrei laeuft. Der erste Aufruf ist ein echter Test.
  Vier echte Fehler haben die Pruefsonden vorher gefunden — u. a. spiegelverkehrte
  Explosionskrater und ein Rechnergegner, der nie lief und deshalb aus unerreichbarer
  Entfernung ins Leere schoss. Details: `docs/PAUSENSPIEL_STUFE1_2026-08-07.md`.
- **Deploy-Konsole `Tools/DeployConsole` gebaut (2026-08-07), erstmals produktiv erfolgreich
  gelaufen am 11.08.2026.** Zuvor gegen einen nachgebauten Share verifiziert
  (`Tools/DeployConsole.Probe`, 25 Pruefungen gruen, echter `dotnet publish`). Der erste
  Produktivlauf erfolgte kopflos ueber `.tmp_tools/DeployHeadless` und endete ohne Alarm.
  ZWEI FALLEN DABEI GEMESSEN, die
  unabhaengig vom Werkzeug gelten: (1) ein erfolgreicher `dotnet publish` kann
  `BiDashboard.dll` STILL ueberspringen, wenn die Datei im Ziel neuer ist als der frische
  Build (PreserveNewest) — die alte Version laeuft dann weiter; Gegenprobe ist der
  SHA256-Vergleich gegen `bin/Release/net8.0/BiDashboard.dll`, der nach einem Publish
  uebereinstimmen MUSS. (2) `check.xlsx`, `zdispo_grp.xlsx`, `zdispo_spart.xlsx` im
  Publish-Verzeichnis sind Build-Ausgabe (`CopyToPublishDirectory="Always"`) und werden bei
  jedem Deploy mit dem Repo-Stand ueberschrieben — auf dem Share bearbeiten ist sinnlos.
  Details: `docs/DEPLOY_KONSOLE_2026-08-07.md`. Einzige verfolgte Aenderung dabei:
  `TrafagSalesExporter.sln` (zwei Projekte aufgenommen); `dotnet test` weiterhin `455/455`.
- **Statustext `"OK"` steht als Zeichenkette in der Excel-Formel** des Nachweises
  (`Services/ExcelExportService.cs`, Blatt „Gruppenmarge Details", Spalten 19 und 20:
  `IF(B{Zeile}="OK",Q-R,"")`). Eine Umbenennung von `GroupMarginStatuses.Ok` liesse dort
  STILL alle Margen leer — der Compiler sieht nur einen String, und die Tests werten
  Formeln nicht aus. Kein aktueller Defekt, aber eine Falle fuer die naechste Umbenennung.
  Fix, Nachweisidee und Begruendung: `docs/FINANCE_ANZEIGE_PRUEFUNG_2026-08-06.md`
  Abschnitt 5a. Soll mit dem naechsten Deploy mitgehen.
- Waehrungsmaskierung (`status == OK && conversion.IsMasked`) steht an drei Aufrufstellen
  einzeln statt im Rechner — letzte gespiegelte Stelle der Rechnung.
- Mail an RanVijay (Cc Andreas) zu den offenen TRIN-Artikeln: Artikel-Liste vor dem Versand
  gegen den Datenstand 2026-08-06 neu ableiten, nicht die Datei vom 2026-08-05 wiederverwenden.
- Innenumsatz-Frage an Andreas: `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md` Abschnitt 4a.

## Aktueller Kurzstand

- 2026-08-11, UK 2025 WERTFEHLER BEHOBEN UND ABGENOMMEN. Ingo hat den UK-Standortexport
  gestartet und das zentrale File neu erzeugt (`neu.xlsx`, 2026-08-11 09:45, 96'234
  Datenzeilen). Alle drei Abnahmekriterien aus
  `docs/FINANCE_UK2025_WERTFEHLER_2026-08-10.md` sind erfuellt: UK 2025 steht bei
  **`3'529'861.80 GBP`** gegen den Finance-Sollwert `3'538'972` = **99.7 %** (vorher
  `394'439` = 11 %), die **Marge dreht von −502.7 % auf +33.8 %** und liegt damit in der
  Bandbreite der anderen Standorte (TRCH 15.7, TRIT 34.3, TRES 38.6, TRAT 38.7, TRDE 41.2,
  TRUS 45.2, TRIN 49.2, TRFR 49.3 %), die **Zeilenzahl bleibt bei `1'867`**. Der Ist-Wert
  trifft auf den Rappen die unabhaengige Rekonstruktion vom 2026-08-10 (`3'529'862`), die vor
  dem Fix aus der Quelldatei gerechnet wurde. Damit ist der Themenlistenpunkt „Daten TR UK
  fuer 2025" erledigt. Geprueft mit dem neuen read-only Werkzeug
  `.tmp_tools/CheckUk2025Result`, das die Spalten ueber die KOPFZEILE aufloest und
  ausschliesslich gegen den Finance-Sollwert vergleicht, nie gegen die hochgeladene
  Importdatei — genau dieser Selbstvergleich hatte den Fehler urspruenglich durchgelassen.
  ZWEI NEBENBEFUNDE, beide gemessen: (1) **Zeile 2 des zentralen Exports ist eine echte
  TRAT-DATENZEILE, keine Beschreibungszeile** — das feste `Skip(2)` aus der Analyse vom
  2026-08-10 verliert dort still eine Verkaufszeile; richtig sind `96'234` Datenzeilen und
  `682` statt `681` Zeilen fuer TRAT 2026. Die Konvention „96'233 Datenzeilen" aus dem
  UK-Dokument gilt fuer diesen Export NICHT. (2) Im ZENTRALEN Export enthaelt
  `Sales Price/Value` den bereits gemappten Zeilenwert und ist deshalb identisch mit
  `Finance | Net Sales Actual`; der Stueckpreis steht nur in der Importdatei. NICHT MOEGLICH
  war ein Vorher-Nachher-Vergleich mit demselben Werkzeug, weil `all.xlsx` nicht mehr im
  Wurzelverzeichnis liegt — die Ausgangswerte stammen aus dem Dokument vom 2026-08-10.
  Weiterhin offen: die verwaiste Legendenzeile (siehe offene Punkte).
  **DIE LEHRE AUS DEM GANZEN VORGANG, gilt ueber UK hinaus: eine Kontrollrechnung gegen die
  eigene Quelle ist keine Kontrolle.** Der Fehler ueberlebte zwei Pruefungen, weil beide nur
  belegten, dass wir die Importdatei reproduzieren (`395'605.82 = 395'605.82`) bzw. dass die
  Zeilen vollstaendig sind. Erst ein UNABHAENGIGER Sollwert hat ihn sichtbar gemacht. Die
  Werkzeuge dazu sind versioniert: `Tools/UkBackfillFile` prueft, welche Lesart einer Spalte
  den Sollwert trifft, und schreibt bei „keine Lesart trifft" oder „beide treffen" NICHTS;
  `Tools/ManualImportUpload` sichert vor dem Ueberschreiben die alte Fassung.

- 2026-08-07, FINANCE-INDIKATOREN DURCHGESEHEN — DEPLOYED UND VERIFIZIERT
  (10:22 MESZ, Funktionscommits `0c8cff5` und `b2e7c4f`, `455/455` Tests im
  Release-Lauf VOR dem Publish, vorher `449`; `BiDashboard.dll` `4'320'768`
  Bytes, SHA256 `B43A9E4B…`, lokal und Server bitgleich; Produktiv-DB
  unveraendert `339'210'240` / `07.08.2026 08:49:20`). Die Durchsicht vom
  2026-08-06 deckte NUR den Kostenbasis-Strang ab; hier sind die anderen
  Fehlerklassen. GROESSTER FUND, produktiv read-only gemessen:
  `FinanceReferences` enthaelt AUSSCHLIESSLICH Zeilen fuer `2025` (17 Zeilen, 14
  mit Wert), das Standardjahr der Seite ist aber das juengste Jahr der Daten,
  also `2026` (35'841 Zeilen). Damit standen `Laender OK` und `Zu pruefen` beim
  Standardaufruf beide auf `0` — ununterscheidbar von „alles sauber", obwohl
  NICHTS geprueft war; die Abweichungen-Sicht war leer und die Finance-Aeste des
  Entscheidungsradars fehlten ganz. Ursache: `BuildFinanceStatus` liefert VIER
  Status, die Schnelluebersicht zaehlte zwei. Neu dritte Kachel `Nicht geprueft`
  plus Warnhinweis; Statustexte in der neuen Klasse `FinanceCountryStatuses`
  statt als Literale. SEPARAT DAVON, ebenfalls gemessen: fuer `CH`, `CN` und
  `RU` fehlt der Sollwert auch in 2025 — CH ist mit `17'608` Zeilen der groesste
  Standort und wird gegen nichts geprueft (Datenluecke fuer Andreas, kein
  Codefehler). WEITERE ACHT: `Net Sales Actual` und die sieben
  Gruppenmarge-Kacheln addieren CHF+EUR+GBP+INR+USD numerisch und schreiben
  `Mixed` dahinter — der Sparten-Reiter warnte davor schon, diese nicht (jetzt
  ueberall, Zahl bleibt, Entscheid Ingo); der Finance-Pivot rechnete auf den
  UNGEFILTERTEN Zeilen, sodass ein Landfilter `Net Sales Actual` bewegte und die
  Pivotkacheln daneben nicht (jetzt `scopedRows`, Entscheid Ingo), verlor Zeilen
  ohne CHF-Kurs und ohne TSC still (jetzt gezaehlt und gemeldet), nannte einen
  Jahreswert `YTD` mit Untertitel „Alle Jahre" (jetzt `Jahresumsatz` + echtes
  Jahr) und trug eine tote Zweitimplementierung `YtdSalesChf`/`MtdSalesChf` mit
  abweichender Jahreswahl (entfernt); die Kachel `Ausgeschlossen` und die zwei
  Datenqualitaets-Pruefpunkte konnten Regelausschluss und echten Nullwert nicht
  unterscheiden, weil `ResolveNetSalesActual` fuer ausgeschlossene Zeilen `0`
  liefert — dieselben Zeilen zaehlten doppelt, auch im Entscheidungsradar (neu
  `IsExcludedByRule`); der `Soll/Ist Vergleich` stand an drei Stellen fest auf
  `2025` (jetzt Jahresauswahl aus `FinanceReferences`) und behauptete fuer FR,
  IN und US unbedingt „Passt gegen Soll" — eine Ergebnisbehauptung aus einer
  fest verdrahteten Liste, direkt neben einem gerechneten Statuschip, der
  `Pruefen` sagen kann; `Materialien` zaehlte Gruppen aus Material x Land x TSC
  x Quelle x Waehrung (jetzt `Pruefzeilen` mit echter Materialzahl daneben, und
  der Sparten-Rollup vereinigt Materialschluessel statt distinkte Zaehlungen zu
  addieren); eine gemessene `0` im Pivot rendete als `-` wie fehlende Daten; die
  1000er-Kappung beider Detailtabellen war unsichtbar. Sechs neue Tests, DREI
  davon per Gegenprobe nachweislich rot ohne Fix — die anderen drei pinnen
  Vertraege unter Razor-Aenderungen und sagen das im Kommentar; die GUI-Seite
  dieser beiden Punkte ist nur per Sichtpruefung abgedeckt.
  ZWEI GRENZEN, ausdruecklich: (1) beide Finance-Routen liegen hinter dem
  Finance-Unlock und liefern von hier aus das PASSWORTPANEL — der HTTPS `200`
  belegt Erreichbarkeit, NICHT dass die Kacheln rendern; ein angemeldeter
  Sichtprueflauf durch Ingo steht aus. (2) Nach dem ersten Publish stand
  `Passt gegen Soll` noch als verwaister Uebersetzungsschluessel in der DLL —
  Commit `b2e7c4f` entfernt ihn, danach zweiter Publish. NUR BERICHTET, nicht
  geaendert: SAP-Proformabelegarten `F5`/`F8` laufen in den Umsatz (TRCH `F8`
  `1'902` Zeilen / `+6'049'560.28`, `F5` `194` / `+497'752.51`, keine
  F2-Dubletten) und koennen wegen des fehlenden CH-Sollwerts im Soll/Ist gar
  nicht auffallen; die Toleranz `<= 1` ist waehrungsblind und steht dreifach;
  die Gutschrift-Schluesselwortliste erfasst `CRN`/`G2`/`S1`/`S2` nicht, `1'522`
  von `1'674` Gutschriftzeilen haengen allein an `Value < 0`. FALLE bei der
  Lokalisierung: ein PowerShell-Skript mit nicht-ASCII-Text im Quelltext wird
  von PS 5.1 als Windows-1252 gelesen und scheitert am Parser — Uebersetzungen
  als UTF-8-JSON auslagern, Skript rein ASCII. Details:
  `docs/FINANCE_INDIKATOREN_PRUEFUNG_2026-08-07.md`.

- 2026-08-07, EINKAUF-INDIKATOREN DURCHGESEHEN — DEPLOYED UND VERIFIZIERT
  (08:40 MESZ, Funktionscommit `eef6374`, `449/449` Tests im Release-Lauf VOR
  dem Publish, vorher `446`; `BiDashboard.dll` `4'293'632` Bytes, SHA256
  `214C51E3…`, lokal und Server bitgleich; Startseite und alle vier geaenderten
  Routen HTTPS `200` mit Inhalt; `HasUnitCost`, `ApplyScopeFilter` und die neuen
  Literale in der ausgelieferten DLL belegt, `Simulation bis Bewertungsdaten
  kommen` verschwunden; Produktiv-DB unveraendert `339'210'240` /
  `07.08.2026 08:00:54`): Frage war, ob die
  Indikatoren der einzelnen Einkauf-Reiter rechnen oder nur da sind. Ergebnis
  sind DREI Gruppen. (A) Rechnet echt: Spend, Spend-Aufriss, Offene
  Bestellungen, Liefertermin-Risiko, Preisentwicklung, Spend-Konzentration,
  Datenqualitaet. (B) Logik korrekt, Datenbasis produktiv duenn: die vier
  ZLO03-gestuetzten Reiter laufen auf `105` Zeilen mit Disponent,
  Lieferperformance hat kein Ist-Wareneingangsdatum und sagt das bereits —
  Aktion ist ein ZLO03-Full-Load, kein Codefix. (C) SECHS Indikatoren zeigten
  eine erfundene oder falsch beschriftete Zahl und sind jetzt behoben:
  `Performance Score` war der Mittelwert von ZWOELF fest einprogrammierten
  Simulationszeilen — eine Konstante unabhaengig von SAP, Cache und Filter,
  jetzt `-` mit „Bewertungsdaten (EKBE/QM) nicht angebunden"; `Preisindikator`
  zeigte den Gesamt-Spend unter einem Stueckpreis-Label, jetzt der
  mengengewichtete Ø-Stueckpreis des juengsten Jahres; `Qualitaet` `"offen"` ->
  `-`; die Idee `Lieferantenrisiko` stand ohne Implementierung auf
  „berechenbar", jetzt „Konzept" wie die Nachbareintraege; im Reiter
  `Kontrakte` filterte die Kachel `Restwert` auf `EKKO.Konnr`, Diagramm und
  `Top Verpflichtung` daneben NICHT — zwei Grundmengen im selben Reiter, jetzt
  dieselbe (inkl. Wegfall des Rueckfalls auf alle offenen Bestellungen), und
  `Faelligkeit` heisst `Letztes Bestelldatum`, weil der Wert `MAX(EKKO.Bedat)`
  ist; der gruene Balken `Ohne akuten Hinweis` stand auf ALLEN FUENF
  Supply-Chain-Reitern beim Standardaufruf garantiert auf `0`, weil
  `Nur Handlungsbedarf` genau die OK-Zeilen entfernte, BEVOR die Balken
  gezaehlt wurden — Umfangs- und Handlungsbedarfsfilter sind jetzt getrennt.
  Dazu `Fehlwert CHF`: fehlte der Stueckkostensatz, lief die Luecke als
  bewertete `0` in die Summe, ohne dass ein P-Code darauf hinwies; neu traegt
  `HasUnitCost` die Luecke bis in die Zeile (Tabelle `-`, Kachel nennt die
  Zahl der unbewerteten Materialien) — dieselbe Bauform wie das bestehende
  `HasFinalStock` direkt daneben. Drei neue Tests, jeder vor dem Fix
  nachweislich rot (Gegenprobe durchgefuehrt). VIER Punkte bewusst NICHT
  angefasst und ausdruecklich NICHT gemessen: `MAX()`-Deduplizierung bei
  ZLO03, `Menge = 0` -> offener Wert `0`, `MinSpendYear`-Abweichung,
  WKURS-Richtung. FALLE beim Deploy, die kuenftig gilt: NICHT ueber
  `/p:PublishProfile=FolderProfile` publishen — das Profil hat
  `DeleteExistingFiles=true`, und im Zielverzeichnis liegen die Produktiv-DB
  (`339` MB) und alle `.bak`-Sicherungen. Richtig ist
  `dotnet publish -c Release -o <UNC>`. Details:
  `docs/EINKAUF_INDIKATOREN_PRUEFUNG_2026-08-07.md`.

- 2026-08-06, NEUE EINKAUF-/LOGISTIK-REITER DEPLOYED UND VERIFIZIERT
  (15:11 MESZ, Commit `01af1b8`, `446/446` Tests vor dem Commit nachgerechnet;
  `BiDashboard.dll` `4'291'072` Bytes, SHA256 `29B9DFC6…`; Startseite und alle
  fuenf neuen Routen liefern HTTPS `200` mit Inhalt; Typen
  `SupplyChainAnalysisService`/`SupplyChainAnalysisKind` in der ausgelieferten
  DLL belegt; der Dienst ist rein lesend): fuenf getrennte additive Routen fuer
  Materialdisposition/Fehlteile, Bestellbedarf/Deckung, Materialabhaengigkeit,
  Dispositionspruefung und Lieferperformance-Datenstatus. Bestehende Spend-,
  Bestell-, Lieferanten- und Stuecklistenreiter sowie deren Berechnungen wurden
  nicht ersetzt. Filter wirken vor Kennzahlen, Prioritaetsbalken und Details.
  Echte OTIF bleibt wegen fehlendem Ist-Wareneingangsdatum sichtbar als
  Datenluecke. Details:
  `docs/EINKAUF_LOGISTIK_SUPPLY_CHAIN_REITER_2026-08-06.md`.

- 2026-08-06, HR-KRANKENQUOTE UND FILTERVERTRAG DEPLOYED UND VERIFIZIERT
  (14:24 MESZ, Commit `9435a5d`, `438/438` Tests): Der
  Arbeitstage-Nenner zieht neu die neun gesetzlichen Feiertage des Kantons
  Zuerich ab (inkl. dynamischem Ostertermin; lokale/nicht gesetzliche Tage
  bewusst nicht). Bei nicht periodengenau eingrenzbaren Rexx-Absenzen zeigt
  jetzt auch die Uebersicht keine scheinbar genaue Absenzquote mehr; die Ampel
  bleibt gelb statt aus einem unzuverlaessigen Wert Rot/Gruen abzuleiten. Neuer
  Regressionstest prueft 128 Kombinationen aus Organisation, Kostenstelle,
  Mitarbeitertyp, Eintrittsjahr, GLZ, Restferien und Suche ueber alle sichtbaren
  HR-Ergebnisbloecke; ein weiterer Test kombiniert Zeitraum, Jahr,
  Fluktuationsfilter und alle Personenfilter. Details:
  `docs/HR_KPI_FEIERTAGE_FILTERTEST_2026-08-06.md`.

- 2026-08-06, ZDISPO NUR IM EINKAUF-SPEND-AUFRISS ERGAENZT, DEPLOYED UND
  VERIFIZIERT (13:57 MESZ, Commit `0a8a4c9`): `zdispo_grp.xlsx` ordnet
  Disponenten/-muster den DISPO-Gruppen zu, `zdispo_spart.xlsx` liefert die
  Produktnamen. Die Daten landen in der separaten Tabelle
  `PurchasingSpendDisponentRule`; die bestehende manuelle
  `PurchasingProductGroupMap` wird weder geloescht noch ueberschrieben und hat
  bei Treffern Vorrang. Exakte Regeln gewinnen vor Sternmustern. Doppelte
  Zuordnungen (`016`, `DS1`, `DS2`) bleiben getrennt und laufen in dieselbe
  summenerhaltende `1/n`-Allokation. Aenderung wirkt ausschliesslich in
  `Einkauf > Spend-Aufriss > Produktgruppe`, nicht in anderen Einkaufs- oder
  Finance-Sichten. Produktiv: `45` Regeln aus `42` Mustern, manuelle Map weiter
  `0`, `105` ZLO03-Zeilen mit Disponent; `D5` hat in der gelieferten Textdatei
  keinen Namen und erscheint deshalb als Code. `435/435` Tests, Startseite und
  direkter Aufriss HTTPS `200`; DLL `4'136'448` Bytes, SHA256
  `0F1CB29F6F766C8CB71903D45B78DB48B3AB94FE58638837F5376E9D2A9B01C1`.
  Details: `docs/PURCHASING_PRODUKTGRUPPEN_ABCXYZ_2026-08-06.md`.

- 2026-08-06, EINKAUF PRODUKTGRUPPEN UND ABC/XYZ DEPLOYED UND VERIFIZIERT
  (12:31 MESZ, Funktionscommit `bb009bf`): Spend-Aufriss hat neu die Perspektive `Produktgruppe -> Lieferant ->
  Material`. `VknrDispo` wird aus ZLO03 persistiert; eine optionale
  `PurchasingProductGroupMap` bildet Disponent auf ZC23-Code/-Text ab. Fehlt die
  Referenz, zeigt die GUI ehrlich `Disponent <Code>`, unzugeordneter Spend bleibt
  als `ohne Produktgruppe` sichtbar. Mehrfach verwendete Komponenten werden
  gleichmaessig `1/n` auf unterschiedliche Gruppen verteilt, sodass die Summe
  erhalten bleibt. ABC/XYZ ist nun eine gemeinsame Massnahmenmatrix mit
  konkreten Pruefauftraegen, Spend, Materialien und Lieferanten. Gesamte Suite
  `435/435` gruen (darin `47/47` Einkauf/Schema und `6/6` Lokalisierung).
  Produktivartefakt: `BiDashboard.dll` `4'120'064` Bytes, SHA256
  `B5C72496A7A4E11AC38675D840A5DF9DBABA6999517DD70FE3D7C0CE07BAEC3C`;
  Startseite und `/einkauf/aufriss` HTTP `200`, `app_offline.htm` nicht aktiv.
  Produktivschema: `VknrDispo` und `PurchasingProductGroupMap` vorhanden,
  `105` ZLO03-Zeilen mit Disponent, manuelle ZC23-Map noch `0` Zeilen. Dieser
  Grundstand wurde um 13:57 durch die separate ZDISPO-Zusatzquelle ergaenzt;
  aktueller Stand siehe Eintrag direkt darueber. Details:
  `docs/PURCHASING_PRODUKTGRUPPEN_ABCXYZ_2026-08-06.md`.

- 2026-08-06, FINANCE-PRUEFBUCH WIES DEN VOLLEN UMSATZ ALS MARGE AUS — DEPLOYED UND VERIFIZIERT
  (Deploy 11:06, `BiDashboard.dll` `4'057'600` Bytes, SHA256 `E6CCF3C4…`, HTTP 200,
  `IsCostBasisKnown` in der ausgelieferten DLL belegt, Produktiv-DB unveraendert):
  beim Durchgehen der Finance-Anzeige gefunden. `BuildFinanceAuditLedgerRows` liess die Marge
  nur bei der Waehrungsmaske leer. Eine FEHLENDE Kostenbasis laeuft aber als 0 durch, also
  ergab „Umsatz minus Kosten" den vollen Umsatz — Spalte `Marge CHF` und `MarginPercent`
  zeigten 100 %, direkt neben dem Status, der „Lieferant unklar" bzw. „Konzernkosten fehlen"
  sagte. Betroffen sind die Pruefbuch-Tabelle im Cockpit UND der Excel-Export
  `Finance_Pruefbuch`. Der zentrale Excel-Nachweis war NICHT betroffen: dort steht die Marge
  als Blattformel mit `WENN(Status=OK)`. Naeherung ueber alle Jahre und ohne den
  `Include`-Filter (in SQL nicht nachbildbar): rund **71'900 von 96'059 Zeilen (~75 %)** haben
  keine belastbare Kostenbasis, im Wesentlichen `Lieferant unklar` bei TRCH/TRDE/TRES/TRAT.
  Neu entscheidet `GroupMarginStatuses.IsCostBasisKnown`. Die Unterscheidung ist noetig, weil
  `IsOpen` dafuer zu grob ist: bei „Kostenwaehrung abweichend" IST die Kostenbasis bekannt,
  nur in anderer Waehrung — die CHF-Marge bleibt dort korrekt rechenbar und wird weiter
  gezeigt (durch einen bestehenden Test gepinnt).
- 2026-08-06, ANZEIGE NACHGEZOGEN (im Deploy 11:06 enthalten): die Statusfarbe im Cockpit stand als
  eigene Aufzaehlung neben `GroupMarginStatuses.Open` und kannte „Kostenwaehrung abweichend"
  nicht — der Status wurde blau statt orange gezeigt, obwohl die Kennzahl „offene Kostenbasis"
  ihn mitzaehlt. Die Farbe folgt jetzt `IsOpen`, also der Statusdefinition selbst. Die
  Schulungsseite `Finance > Grundlagen` erklaerte „Konzernkosten fehlen" ueberhaupt nicht,
  obwohl der Status seit heute 137 indische Zeilen betrifft; die Tabelle fuehrt ihn jetzt
  mit der Abgrenzung zu „Standardpreis fehlt". Der Hinweistext im Gruppenmarge-Tab beschrieb
  noch die MVP-Regel von vor dem Konzernkosten-Umbau und ist jetzt die tatsaechliche
  Regelkette; derselbe veraltete Stand stand als Hinweis im Finance-Ergebnis
  („echte Konzern-Standardkosten sind noch nicht angebunden" — seit 2026-08-05 falsch).
  Kachel „Kostenbasis" heisst wie die Tabellenspalte „Bekannte Kostenbasis" —
  die Summe enthaelt offene Zeilen mit 0. `433/433` Tests gruen.
- 2026-08-06, DURCHGESEHEN UND IN ORDNUNG (Finance): Laenderstatus und die Kacheln
  „Laender OK"/„Zu pruefen" (Literale passen zum Erzeuger `BuildFinanceStatus`), Datenqualitaet,
  Gutschriftkandidaten, Sparten-/Produktfinanzen, Finance-Pivot (keine Kostenlogik enthalten),
  `BuildFinanceSummaryRow` (ausgeschlossene Zeilen tragen Wert 0, Summe ueber alle Zeilen ist
  daher gleich der Summe ueber die eingeschlossenen). Deckungsbeitrag ist ueberall „-", weil
  KEIN Standort einen fix/variabel-Split liefert (0 von 96'059 Zeilen gemessen) — korrekt
  angezeigt, das Feature ist heute aber wirkungslos. `EstimatedMarginTotal` im aelteren
  Cockpit-Teil rechnet Umsatz minus geschaetzte Kosten, wird aber nirgends angezeigt oder
  exportiert (toter Code). Vollstaendiges Pruefprotokoll:
  `docs/FINANCE_ANZEIGE_PRUEFUNG_2026-08-06.md`.
- 2026-08-06, DAS FELD IST PRODUKTIV ANGEKOMMEN: der TRIN-Export 06:54 fuellt Sales Type auf
  **6'664 von 7'094 Zeilen (93,9 %)** (`FFM` 5'923, `LRD` 718, `CM` 23, leer 430), Trafag-
  Sachnummer auf 3'625. **5'868** `FFM`/`CM`-Zeilen wechseln von „Lieferant unklar" auf intern.
  Von 718 `LRD`-Zeilen finden **581 die Schweizer Konzernkosten — ueber die lokale Artikelnummer
  waeren es 4**; die uebrigen 137 stehen auf `Konzernkosten fehlen` und weisen bewusst keine
  Marge aus. Andere Standorte 0 (nur Indien fuehrt diese UDFs).
- 2026-08-06, GRUPPENMARGE JETZT IN EINER KLASSE — DEPLOYED UND VERIFIZIERT (`515ab9d`,
  Deploy 09:41, SHA256 `CF750722…`, HTTP 200, neue Typen in der ausgelieferten DLL belegt).
  Die Kostenlogik stand doppelt da
  — `ExcelExportService` auf `SalesRecord`, `ManagementCockpitService` auf
  `FinanceAggregationRow`, 48 von rund 95 Zeilen identisch — und war beim Einbau von
  „Konzernkosten fehlen" bereits AUSEINANDERGELAUFEN: das Cockpit rief die Statusfunktion ohne
  das neue Kennzeichen auf und zeigte fuer dieselbe Zeile „Standardpreis fehlt", der Audit-Ledger
  kannte den Status gar nicht, Sortierung, Offen-Zaehler und Statusfarbe uebergingen ihn, und die
  Excel-Formel je Land widersprach der Gesamtsumme im selben Nachweis. Jetzt rechnet nur noch
  `Services/GroupMarginCalculator.cs`; beide Dienste bilden ihre Zeile auf `GroupMarginLine` ab.
  Die drei Abweichungen zur Kostenbasis sind benannte Regeln in einer geordneten Kette
  (`GroupStandardCost` → `GroupDistributionWithoutGroupCost` → `LocalStandardCost`), die
  Reihenfolge ist die Fachregel und wird getestet. Statuswerte, Offen-Definition und Sortierung
  stehen vollstaendig in `GroupMarginStatuses`, die Excel-Formeln werden daraus erzeugt.
  `GroupMarginConsistencyTests` schickt dieselbe Zeile durch BEIDE oeffentlichen Einstiegspunkte
  und verlangt gleiche Ergebnisse — ein reiner Test der Rechenklasse waere gruen geblieben,
  waehrend die Aufrufstelle das Ergebnis wegwirft. `431/431` Tests gruen (vorher `406`), Saldo
  −298/+158 Zeilen in den beiden Diensten. Details:
  `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md` Abschnitt 7d.
- 2026-08-06, DEUTSCHLAND-MAIL WAR FALSCH ADRESSIERT (`0f15b1e`): die Alphaplan-Export-SQL ist
  UNSERE (`AlphaplanExportPackage/scripte/alphaplanExport.ps1`), Lieferant und Kundenname fehlen,
  weil unsere Query sie nicht liest. Einzige echte Bitte an Rohail ist ein Schemaauszug. Gilt
  genauso fuer Spanien (`PostingDate` auf allen 5'504 Zeilen leer). Siehe `docs/rag/MANUAL_IMPORT.md`
  Abschnitt „Skripthoheit".
- CALL 2026-08-05, INDIEN: DAS FELD HEISST „SALES TYPE", PREFERRED-VENDOR-BITTE IST UEBERHOLT.
  RanVijay hatte eingewandt, dass viele Artikel bei TR IN lokal gefertigt werden — bei
  Eigenfertigung gibt es keinen Vorlieferanten, `OITM.CardCode` waere dort sachlich falsch
  gepflegt. Ergebnis des Calls: im indischen Artikelstamm gibt es das Feld „Sales Type" mit
  „full-fledged manufacturing" (Produktion im indischen Werk) und „LRD" (Import von Trafag
  Schweiz, Weiterverkauf; dort sind Lieferant Schweiz und Einkaufspreis laut RanVijay bereits
  gepflegt). Er erwartet, dass die 1'271 offenen Artikel damit auf „maybe 50 60" schrumpfen, und
  pflegt den Rest sofort selbst. PRODUKTIVDATEN STUETZEN DAS: `PT0` (laut Call LRD) hat 319
  Zeilen, davon nur 37 ohne Lieferant; `PS0`/`DM0`/`TS0` (2'469/2'223/1'590 Zeilen) sind fast
  durchgaengig ohne. TRIN gesamt: 6'236 Zeilen ohne Lieferant (1'278 Artikel, 1'057'121'097
  INR), ALLE mit Kostenbasis — es fehlt nur die Klassifikation. Die exportierte Artikelgruppe
  ersetzt das Feld NICHT (trennt nach Materialart, nicht nach Fertigungsort). OFFEN ist nur der
  technische Spaltenname; er wird ERMITTELT, nicht geraten. ENTSCHEID INGO: Eigenfertigung gilt
  als intern mit liefernder Gesellschaft TR IN, lokale Kostenbasis = Gruppenkostenbasis (kein
  IC-Aufschlag, gleiche Logik wie CH/AT). Umsetzung landet in UNSERER Query
  `Services/HanaQueryService.cs` (OITM bereits gejoint, Query steht ZWEIMAL: OINV/INV1 und
  ORIN/RIN1) und in `Services/GroupMarginSupplierClassifier.cs`; pauschale TSC-Regel wie CH/AT
  ist bei TRIN unzulaessig, 141 Zeilen sind echte indische Fremdlieferanten. Der Entwurf
  `docs/mails/Build-RanVijayFollowup.ps1` darf nicht mehr raus. Details:
  `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md`.

- BEFUND 2026-08-05, FELD GEFUNDEN UND AUSGEWERTET: Das Feld heisst **`OITM."U_Tasc_ST"`**
  (UDF `Tasc_ST`, FieldID 14, Beschriftung „Sales Type"), ermittelt aus `CUFD`/`UFD1` — nicht
  geraten. Werte laut `UFD1`: `FFM` Full Fledged Manufacturing, `LRD` Limited Risk Distributor,
  `CM` Contract Manufacturing, `--` ungepflegt. **`CM` kam im Call nicht vor** und ist echt
  extern (Fremdfertigung). VERTEILUNG auf Artikeln mit Rechnungszeilen ab 2025 (1'449 Artikel,
  7'018 Zeilen): `FFM` ohne Vendor **1'184 Artikel / 5'830 Zeilen** (korrekt so, brauchen keinen
  Lieferanten), `LRD` mit Vendor 93/454 (fertig), `LRD` ohne Vendor 30/256 + `CM` ohne Vendor
  2/23, Sales Type ungepflegt 130 Artikel/377 Zeilen (zweite, neu entdeckte Baustelle), `FFM`
  MIT Vendor 10/78 (Widerspruch). HEBEL: rund 5'830 der 6'236 maskierten TRIN-Zeilen (93 %)
  werden allein durch das Lesen des Feldes klassifizierbar, ohne jede Stammdatenpflege in
  Indien. **ENTSCHEIDENDE ZUSATZPRUEFUNG (Runde 3):** ALLE 93 `LRD`-Artikel mit Vendor zeigen
  auf `V0078` = Trafag AG/CH, ohne Ausnahme. `LRD` bestimmt die liefernde Gesellschaft damit
  ALLEIN — die 30 `LRD`-Artikel ohne Vendor brauchen KEINE Pflege. Ebenso haben 64 der 130
  Artikel ohne Sales Type schon einen Vendor und sind dadurch klassifiziert. **RESTLISTE damit
  nicht 32, sondern: 66 Artikel Sales Type pflegen (Blocker), 10 Artikel `FFM`-mit-Vendor
  bestaetigen (Fehlklassifikationsrisiko), 2 `CM`-Artikel (IC15415, IC15037) nur „waere schoen"
  — `CM` heisst schon extern, fuer die Marge fehlt dort nichts.** Dazu zwei Fragen ohne
  Datenbezug: bedeutet `LRD` IMMER Trafag Schweiz (heute 93/93, aber Messung ist keine Regel),
  und soll der Sales Type bei neuen Artikeln Pflicht werden (im Gesamtstamm 2'838 von 5'337
  ohne Wert). Ohne diese Pruefung waere Indien um Pflege gebeten worden, die unser
  eigenes Feld schon leistet — derselbe Fehlertyp wie die ueberholte Preferred-Vendor-Bitte.
  GEGENPROBE bestanden: `PT000003`/`PT000010` = `LRD` mit `V0078`, `DM000001` = `FFM`;
  `DM000083` ist `LRD` OHNE Vendor und widerlegt die Heuristik „kein Lieferant =
  Eigenfertigung". KONSISTENZ artikelgenau: unsere DB zaehlt 167 TRIN-Artikel MIT Lieferant,
  B1 ergibt 93+64+10 = exakt 167; ohne Lieferant 1'278 vs. 1'282 (Differenz = nur auf
  Gutschriften). Liefergegenstand: `output/TRIN_Sales_Type_Offen_2026-08-05.xlsx` (4 Blaetter)
  via `.tmp_tools/BuildTrinSalesTypeExcel`. NOCH OFFEN: Mail an RanVijay, Export-Umsetzung
  (`U_Tasc_ST` in `Services/HanaQueryService.cs`, ZWEIMAL — OINV/INV1 und ORIN/RIN1 —, Feld auf
  `CentralSalesRecord` per `AddColumnIfMissing`, Auswertung im
  `GroupMarginSupplierClassifier`). Details: `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md`.

- DEPLOY 2026-08-05 15:48, SALES TYPE UND TRAFAG-SACHNUMMER IM EXPORT UMGESETZT: `SalesType` und
  `GroupMaterialNumber` werden aus dem Artikelstamm gelesen, gespeichert, im Audit-CSV
  ausgewiesen und in der Gruppenmarge ausgewertet. `FFM`/`CM` -> intern mit liefernder
  Gesellschaft TR IN und lokaler Kostenbasis, `LRD` -> intern TR AG mit Konzernkosten ueber die
  Trafag-Sachnummer. NEUER STATUS `Konzernkosten fehlen`: LRD-Zeilen ohne Konzernkostentreffer
  zeigen KEINE Marge mehr (vorher eine Marge auf dem IC-Einkaufspreis — plausibel aussehend und
  falsch); als Konstante in `Services/GroupMarginStatuses.cs`, weil Excel, Cockpit und
  Pruefsummenformel denselben Text brauchen. WIRKUNG erst mit dem naechsten TRIN-Export
  (Timer 12:00): dann wechseln rund 5'830 Zeilen von „Lieferant unklar" auf intern, und 569
  statt 185 LRD-Zeilen bekommen eine Schweizer Kostenbasis. Spalten sind produktiv angelegt.
  406/406 Tests gruen, `BiDashboard.dll` 4'045'824 Bytes / SHA256 `0C65C997…`, bitgleich.
  DREI FEHLER, die erst durch Tests und Messung sichtbar wurden: (1) die B1-Query ist von ALLEN
  Standorten geteilt — ein festes `itm."U_Tasc_ST"` haette den ITALIEN-EXPORT mit „invalid
  column name" abgebrochen, jetzt Spaltensuche mit `'' AS sales_type` als Rueckfall; (2) das
  vorhandene `HasColumnAsync` schreibt Spaltennamen GROSS, Indiens Spalte heisst aber gemischt
  `U_Tasc_ST` — die Suche nach `U_TASC_ST` liefert produktiv 0 Treffer, das Feld waere fuer
  Indien STILL nie selektiert worden; jetzt `ResolveColumnNameAsync`, schreibweisenunabhaengig
  und mit dem GEFUNDENEN Namen im SELECT (HANA quotet case-sensitiv); (3) der Schreibweg ist ein
  Bulk-INSERT mit ausdruecklicher Spaltenliste — ein Feld am Modell genuegt nicht, aufgefallen
  durch `NOT NULL constraint failed`. Details:
  `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md` Abschnitt 7.

- BEFUND 2026-08-05, WAS `CM` IST — OHNE RUECKFRAGE BEI INDIEN ERSCHLOSSEN, UND MEINE ERSTE
  EINORDNUNG WAR FALSCH: `Sales Type` beschreibt NICHT die Herkunft, sondern die
  verrechnungspreisliche ROLLE von TR IN. `FFM` = voll risikotragender Hersteller (produziert
  und verkauft auf eigene Rechnung), `LRD` = Vertrieb mit begrenztem Risiko (bezieht Fertigware
  aus CH, verkauft lokal weiter), `CM` = Auftragsfertiger fuer den Prinzipal. Ich hatte `CM` als
  „Fremdfertigung durch Dritte, also extern" eingeordnet — falsch. FUENF unabhaengige Belege:
  (1) Kunde der beiden CM-Artikel `IC15415`/`IC15037` ist AUSSCHLIESSLICH Trafag AG/CH (23
  Zeilen, 53'842'559 INR); (2) Marge 31.2 % und 31.7 % — nahezu identischer Aufschlag auf zwei
  verschiedenen Artikeln = Kostenaufschlag, keine Marktpreisbildung; (3) Artikelgruppe
  `Sub Assemblies`; (4) beide MIT `Drawing No`, aber OHNE `Material No` und OHNE `Ordering Code`
  — nach Zeichnung des Prinzipals gebaut; (5) kein Preferred Vendor, konsistent mit
  Eigenfertigung. FOLGE: `CM` ist intern mit liefernder Gesellschaft TR IN und lokaler
  Kostenbasis wie `FFM`, der Preferred Vendor ist UNNOETIG. Die Bitte an Indien schrumpft damit
  auf 66 Artikel (Sales Type) + 10 Bestaetigungen; kein Vendor-Pflegefall mehr.

- BEFUND 2026-08-05, BEI `LRD` IST DER LOKALE WERT DIE FALSCHE KOSTENBASIS: Weil LRD-Artikel in
  CH hergestellt und von Trafag AG bezogen werden (Bestaetigung Ingo), ist `INV1.StockPrice`
  dort der IC-EINKAUFSPREIS, nicht die Herstellkostenbasis — genau der Wert, den die
  Gruppenmarge laut `Mappe1.xlsx` ersetzen soll. Richtige Basis waere `GroupStandardCosts`
  (Bewertungskreis 1100, CHF, 63'506 Zeilen). GEMESSEN greift der Weg aber kaum: nur 34 von 135
  TRIN-Artikeln mit Lieferant Trafag AG (185 von 687 Zeilen, 27 %) finden ueber die
  Artikelnummer einen Treffer — die indischen Nummern sind TASC-Eigennummern, keine Trafag-MATNR.
  BRUECKE GEFUNDEN UND GEMESSEN: das UDF **`U_TASC_OMN` („Material No") IST die
  Trafag-Sachnummer** — sie steckt bei vielen Artikeln auch in der Bezeichnung (`PT000003` =
  „EPR10.0A(**57291**)-8283", `U_TASC_OMN` = `57291`), und alle acht Stichproben stehen mit
  CHF-Stueckkosten in `GroupStandardCosts`; das Schluesselformat passt (37'392 der Schluessel
  sind fuenfstellig). VOLLMESSUNG ueber alle 123 LRD-Artikel: ueber `ItemCode` 34 Artikel /
  27 % der Zeilen, ueber `U_TASC_OMN` **118 von 123 Artikeln (95.9 %) und 569 von 710 Zeilen
  (80.1 %)** — von den 118 Artikeln MIT echter Nummer treffen 118, also 100 %. Die fuenf
  Ausfaelle sind genau die ohne Nummer: `DM000083` (108 Zeilen, groesste Einzelluecke),
  `DM000084` (27), `H90101` (4), `FA000028`/`FA000029` (je 1, Anlagegueter). FOLGE FUER DEN
  EXPORT: ZWEI neue Felder lesen, nicht eines — `U_Tasc_ST` und `U_TASC_OMN`; der
  Konzernkosten-Lookup muss fuer TRIN auf den Trafag-Schluessel gehen statt auf `Material`
  (heute `NormalizeMaterialKey(record.Material)`). Der Platzhalter aus zwei Bindestrichen ist
  wie leer zu behandeln. REGEL: bei LRD-Zeilen ohne Konzernkostentreffer NICHT auf den lokalen
  Wert zurueckfallen — das ergaebe eine plausibel aussehende, falsche Marge (derselbe Fehler wie
  bei TRIT, siehe `docs/FINANCE_SUPPLIER_LUECKE_ANALYSE_2026-07-28.md` Abschnitt 7a). NEUE
  BITTE AN INDIEN dadurch: 3 LRD-Artikel brauchen die `Material No` (139 Zeilen) — das blockiert
  die Kostenbasis, nicht die Klassifikation.

- OFFENE FRAGE AN ANDREAS 2026-08-05, INNENUMSATZ IST GRUPPENWEIT NICHT AUSGESCHLOSSEN (beim
  CM-Nachgraben aufgefallen, deutlich groesser als das Indien-Thema): Zeilen mit einer
  Trafag-Gesellschaft als KUNDE — TRCH 11'034 von 47'142 (23.4 %, 16'347'706 CHF), TRIN 737
  (10.4 %, 145'181'191 INR), TRIT 657 (3.3 %, 576'130 EUR), Rest je unter 15 Zeilen. In
  `FinanceRules` gibt es dazu nur ZWEI von Hand angelegte Kundenausschluesse: Id 2 (DE,
  `CustomerName` = `Trafag AG`) und Id 6 (IT, enthaelt `Trafag Italia`). Die IT-Regel greift nur
  fuer Trafag Italia, die uebrigen 657 TRIT-Zeilen bleiben drin; fuer TRCH und TRIN existiert
  keine Regel. Verkauft TR IN an Trafag Italia und Italia danach an den Endkunden, stehen beide
  Umsaetze im Dashboard — fuer eine KONZERN-Umsatzzahl ist derselbe Warenwert doppelt enthalten.
  KEINE Empfehlung von mir: ob die Umsatzzahl brutto oder konsolidiert gemeint ist, ist eine
  Finanzentscheidung. Zu klaeren, bevor die Gruppenmarge als belastbar bezeichnet wird.

- DEPLOY 2026-08-05, SERVER-ANALYSE PRODUKTIV: `Services/ServerAnalysisBackgroundService.cs`
  fuehrt lesende Diagnoseabfragen gegen Standort-B1 aus — auf dem Server, weil einzelne
  Standortsysteme nur von dort erreichbar sind. ZUGRIFFSLAGE GEMESSEN: Share = FullControl,
  aber `Invoke-Command`/`schtasks`/`C$` auf `tragvapp401` = Zugriff verweigert, KEIN RDP
  vorhanden; der DNS-Name `trch-webapp-bidashboard` ist ein CNAME auf `tragvapp401`, mit dem
  Aliasnamen scheitert schon Kerberos. Deshalb ist die LAUFENDE ANWENDUNG der einzige Weg,
  Code auf dem Server auszufuehren: alle 20 s Pruefung auf `_analysis/run.trigger`, dann
  `_analysis/sql/*.sql` -> `_analysis/results`. Guardrail `Services/ReadOnlySqlGuard.cs` (nur
  SELECT/WITH, Positivliste), Zugangsdaten ueber den neuen gemeinsamen
  `Services/DataSources/HanaServerResolver.cs` — dieselbe Aufloesung wie der Export.
  Fernbedienung `docs/analyse/Run-ServerAnalysis.ps1 -Action Run|Fetch|Clean` (VERSIONIERT,
  bewusst nicht unter `.tmp_tools/` — das ist gitignoriert, und die Abfragen sind der Nachweis
  fuer eine fachliche Entscheidung; Abfragen in `docs/analyse/sql/`, Belege in
  `docs/analyse/ergebnisse/`).
  385/385 Tests gruen, `BiDashboard.dll` 4'037'632 Bytes / SHA256 `56AFD5AF…`, bitgleich mit
  dem Release-Build, Produktiv-DB unveraendert, HTTP 200. FALLE: zwei Bindestriche koennen
  nicht als Zeichenkettenliteral in einer Analyseabfrage stehen (gelten als Kommentar).
  Vor dem Deploy lokal gegen Italien verifiziert (`.tmp_tools/ServerAnalysisLocalTest`) und
  dabei zwei SQL-Fehler gefunden: `LIKE 'U_%'` matcht wegen des Platzhalter-Unterstrichs auch
  `UserSign`/`UserText` (jetzt `ESCAPE '\'`), und `SCHEMA_NAME = '{SCHEMA}'` findet klein
  geschriebene Schemata nicht (jetzt `UPPER(...)`). NEBENBEFUND, betraf auch den naechsten
  Produktivdeploy: `dotnet publish` des Hauptprojekts brach ab, weil die csproj drei im Working
  Tree geloeschte Content-Dateien einbindet (`DE_Beispiel_Export_Daten.xlsx`, `login.png`,
  `manometer.png`); behoben mit `Condition="Exists('...')"` nach dem vorhandenen Muster.

- BEFUND 2026-08-03, SPANIEN HAT KEIN BUCHUNGSDATUM (Prio von Andreas): `PostingDate` ist auf
  ALLEN 5'504 TRES-Zeilen leer — Spanien ist der einzige Standort ohne Buchungsdatum, alle
  anderen haben es zu 100 % gefuellt (TRUK 6 Ausnahmen). Die bisherige Doku und der
  Mailentwurf an Santi nannten nur „231 Zeilen ohne jedes Datum" — das ist die TEILMENGE, in
  der zusaetzlich das Rechnungsdatum fehlt, nicht das Problem. Folge: alle 5'504 Zeilen fallen
  auf `InvoiceDate` zurueck (Rechnungsdatum ist nicht Buchungsdatum -> ueber einen Jahreswechsel
  still falsche Periode), 231 Zeilen eine Stufe weiter auf `ExtractionDate` und zaehlen damit
  pauschal im Exportjahr — 140'598.19 EUR. KEIN akuter Jahresfehler, weil alle 231 ein
  `OrderDate` in 2026 haben und der Export 2026 lief; `OrderDate` ist gefuellt, wird von der
  Fallback-Kette aber nicht genutzt. URSACHE WIE BEI DE: unsere eigene Query.
  `Export-SageSpainSalesCsv.ps1` Z. 184-186 selektiert `FechaFactura`/`FechaAlbaran`/
  `FechaRegistro`, aber kein Buchungsdatum, und liest `CabeceraAlbaranCliente` +
  `LineasAlbaranCliente`, nicht die Buchhaltungstabellen. Query steht ZWEIMAL (auch in
  `Run-SpainRangeExportAndUpload-AllInOne.ps1` Z. 233-235) — Aenderungen immer an beiden
  Stellen. KANDIDAT, NICHT BELEGT: `FacturasTB.FechaAsiento` ist der einzige brauchbare Treffer
  im Schema-Auszug, aber die Tabelle hat `NumeroFacturaInicial_`/`NumeroFacturaFinal_` (riecht
  nach Sammelbuchung ueber Nummernbereich), `CabeceraFacturaCliente` fehlt im Auszug ganz, und
  der Auszug ist bei 80 Objekten abgeschnitten — gemeinsame Spalten mit dem Lieferscheinkopf
  sind nur `CodigoEmpresa` und `FechaFactura`, der Join ist also NICHT ableitbar. Erst live
  pruefen. Sofort additiv moeglich ohne neuen Join: `SerieFactura`, `NumeroFactura`,
  `EjercicioFactura`, `StatusContabilizado` liegen schon in der gelesenen Tabelle. Offen
  fuer Finance: darf `OrderDate` Fallback-Stufe werden, reicht `EjercicioFactura` als
  Jahresanker. Details: `docs/FINANCE_ES_BUCHUNGSDATUM_2026-08-03.md`.

- DOKU 2026-08-03, RAG-Luecke geschlossen, die den DE-Fehlgriff ueberhaupt ermoeglicht hat: Die
  Export-Pakete `AlphaplanExportPackage/` und `SageSpainExportPackage/` standen in NEUN bzw.
  SIEBEN Markdown-Dateien, aber in KEINER auf dem RAG-Einstiegspfad — nicht im
  `RAG_ROUTER.md`, nicht im `RAG_DETAIL_INDEX.md`, nicht in `docs/rag/MANUAL_IMPORT.md`. Wer
  ueber Router -> `lastchange.md` -> Kurzdatei einstieg, lernte „DE liefert kein Supplier-Feld"
  und schloss daraus „Standort fragen", ohne je zu erfahren, dass die Query uns gehoert. Jetzt
  ergaenzt: neue Vorrangregel 7 im Router („bei fehlendem Feld in DE/ES ZUERST die eigene
  Export-SQL pruefen"), zwei Themenzeilen im Router, drei Zeilen im Detailindex (Export-SQL DE,
  Export-SQL ES, Schema-Discovery) und ein neuer Abschnitt „Skripthoheit" in
  `docs/rag/MANUAL_IMPORT.md` mit Skript, gelesenen Tabellen und Konsequenz je Standort.

- BEFUND + MAIL 2026-08-03, DE/Alphaplan war die falsche Bitte an die falsche Stelle: Die alte
  DE-Mail bat Rohail um drei Export-Erweiterungen (Lieferant, Kundenname/-land, RTF-Muell).
  FALSCH — die Export-SQL ist UNSERE: `AlphaplanExportPackage/scripte/alphaplanExport.ps1`
  Zeilen 143-202 und `alphaplandeltaexport.ps1` mit identischer Query, geschrieben in diesem
  Repo, lesen nur `dbo.Belege` + `dbo.BelegePositionen`. Drei der vier DE-Luecken sind Spalten,
  die unsere Query nicht liest; `RechnungsAdressenID` wird sogar selektiert, aber nie auf einen
  Namen aufgeloest. Nur `ArtikelNummer` vs. TR-AG-/SAP-`MATNR` ist eine echte Fachfrage an DE
  (offen seit 2026-06-01) — und die ist heikel, weil der Standard-Vorspann aller Standortmails
  „Produktsparte ist egal, solange die Materialnummer passt" behauptet, was fuer DE gerade
  unbelegt ist; die DE-Mail hat deshalb eine eigene Kastenfassung ohne diesen Satz. ECHTER
  BLOCKER ist das fehlende Alphaplan-Schema fuer `ApDaten`: `candidate_objects.csv` im Repo-Root
  ist nur eine Kopfzeile, `obj/candidate_objects.csv` ist Sage Spanien, die DB liegt auf
  `localhost\SQL2012` des DE-Servers hinter DPAPI-Credential. DESHALB KEINE TABELLENNAMEN RATEN —
  ein erfundenes `JOIN dbo.Adressen` im ausgelieferten Skript waere derselbe Fehlertyp wie
  UK-2025 und das IT-Superlativ. Neue DE-Mail bittet nur noch um einen read-only
  `INFORMATION_SCHEMA.COLUMNS`-Auszug und stellt die `ArtikelNummer`-Frage. Sie ist als EINZIGE
  der sieben Standortmails auf DEUTSCH (Rohail sitzt bei der Trafag GmbH), Betreff
  „BI Dashboard - Alphaplan-Export: eine Schemaliste und eine Frage zu den Artikelnummern";
  alle englischen DE-Entwuerfe in Outlook sind Loeschkandidaten. FALLE dabei:
  `Build-StandortMails.ps1` ist reines ASCII ohne BOM, PowerShell 5.1 liest so eine Datei als
  Windows-1252 — echte Umlaute wuerden als Mojibake in der Mail landen, deshalb stehen alle
  Umlaute als HTML-Entities (`&uuml;` etc.). Alle vier DE-Zahlen
  am 2026-08-03 neu gemessen und exakt bestaetigt: 7'171 Zeilen, Supplier 7'171 leer,
  CustomerName/-Country 7'171 leer bei 7'171 gefuellter CustomerNumber, 2'903 Bezeichnungen mit
  Font-Muell, Material 0 leer. Details:
  `docs/FINANCE_FELDLUECKEN_MAILS_2026-07-31.md` Abschnitt „Korrektur Deutschland, 2026-08-03".

- VERSAND 2026-08-03, Indien-Nachfassung: RanVijay hat auf die Mail vom 31.07. geantwortet, dass
  er die Frage nicht versteht, und um einen Teams-Call gebeten. Ursache mutmasslich die
  Doppelbenennung — SAP nennt das Feld UI-seitig `Preferred Vendor` (Reiter `Purchasing Data`),
  unser Datenmodell nennt dasselbe Feld `Supplier`/`OITM.CardCode`. Antwortentwurf liegt in
  Outlook (an RanVijay, Cc Andreas), erklaert die Gleichsetzung in einem Satz und haengt die
  konkrete Artikelliste an: `output/TRIN_Fehlende_Preferred_Vendor_2026-08-03.xlsx`, erzeugt von
  `.tmp_tools/BuildTrinSupplierGapExcel` aus `Finance_Dashboard_Audit_All_2026-07-29.csv` mit der
  in `FINANCE_FELDLUECKEN_STANDORTE_2026-07-30.md` Abschnitt 7 dokumentierten Gruppierung —
  1'271 von 1'437 Artikeln, 6'154 betroffene Zeilen, deckungsgleich mit der bereits gesendeten
  Zahl. Skript fuer die Mail: `docs/mails/Build-RanVijayFollowup.ps1` (`-Mode Preview` aendert
  nichts, `-Mode Draft` legt den Entwurf an, sendet nie).

- DEPLOYED 2026-08-03, Commit `9e28086`: Logistik > Stuecklistenanalyse hat ein neues
  richtungsabhaengiges Dashboard fuer Top-Down und Bottom-Up mit vier
  Kennzahlen, Top-12-Verwendungsbreite, Bestandsklassen und LZ-Code-Verteilung.
  Die Aggregate verwenden den gesamten gefilterten Cache; nur die bestehende
  Rohdatentabelle bleibt auf 200 Zeilen begrenzt. Gemeinsam verwendete
  Komponenten werden bei der Bestandslage genau einmal klassifiziert und
  Bestandswerte nicht ueber Stuecklisten summiert. Alle acht UI-Sprachen sind
  abgedeckt. Live-Cache-Snapshot und fachliche Grenzen:
  `docs/LOGISTIK_STUECKLISTEN_DASHBOARD_2026-08-01.md`. Release-Test:
  353/353 bestanden. Produktive `BiDashboard.dll` `03.08.2026 06:59:38`,
  `4'024'832` Bytes, SHA256
  `8D5586E5536C83A9EDB409472C332D190488898C3FE8E8DB2097C3131779B554`;
  Release und Server bitgleich. Produktiv-DB in Laenge, Schreibzeit und SHA256
  unveraendert, `app_offline.htm` entfernt, Port 443 offen und authentifizierter
  Aufruf von `/BiDashboard/logistik/stuecklistenanalyse` mit HTTP `200`.

- DOKU 2026-07-31, RAG-Inhaltsverzeichnis konsolidiert: `docs/RAG_ROUTER.md`
  von 289 auf 51 Zeilen reduziert und auf Vorrangregeln/Themenrouting
  beschraenkt. Detailquellen, Live-Werkzeuge und Suchbegriffe stehen jetzt in
  `docs/RAG_DETAIL_INDEX.md`. Der vorherige Router ist vollstaendig und
  zeilengleich in
  `docs/raw_md_archive/RAG_ROUTER_ARCHIV_2026-07-31.md` erhalten.

- DEPLOYED 2026-07-31, finaler Stand der Spend-Matrix fuer
  Praesentations-Screenshots (Code-Commits `4a3271b`, `f740eb9`, `4498bd4`):
  dunkler Primaertext und deutlichere Ebenenhintergruende; Tabellenkopf,
  Lieferanten, Warengruppen und Materialien fett (`700`); Lieferanten und
  Warengruppen `1.05rem`, Materialien `1rem`. `346/346` Tests gruen.
  Produktive `BiDashboard.dll` `31.07.2026 11:43:06`, `3'226'624` Bytes,
  SHA256 `E64BF04327D3FD7668D424C0FA52EC78A00F076E9118E253D57601730F24A247`;
  Release und Server bitgleich, Produktiv-DB unveraendert, Port 443 offen,
  authentifizierter HTTPS-Aufruf `200`.

- VERSAND 2026-07-31, Stand bei Chatende (Detail: `docs/FINANCE_FELDLUECKEN_MAILS_2026-07-31.md`
  Abschnitt „Stand bei Chatende"): INDIEN IST RAUS - gesendet 09:56 an `RanVijay.Kumar@trafag.com`,
  Betreff von Ingo ergaenzt auf „... (Trafag India) -> Supplier Name", NICHT an `tasc.co.in`, damit
  ist die Fremddomain-Frage fuer diesen Versand erledigt. In Entwuerfen liegen sechs: DE, ES, UK, IT
  versandfertig, FR und US mit LEERER An-Zeile. ZWEITE KORREKTUR an der Italien-Mail: sie nannte
  TR IT „the best-performing site on supplier data" - FALSCH, TRUK hat 100 % gegen TRIT 71 %.
  Eingeschraenkt auf „of our SAP Business One sites" und der zweite Balken ersetzt, weil Artikel-
  und Zeilenebene bei IT beide auf 71 % fielen und zwei identische Balken wie ein Copy-Paste-Fehler
  aussahen; jetzt Standortvergleich UK/IT/IN/FR/US. MUSTER HINTER BEIDEN FEHLERN (UK-2025 und
  IT-Superlativ): eine Behauptung war aus aelterer Doku uebernommen statt gemessen. Vor jedem
  Mailversand die Zahlen gegen das Audit-CSV nachrechnen, auch die scheinbar harmlosen Nebensaetze.
  NICHT ANGEFASST: Ingos eigener Entwurf „Missing supplier information in sales export data"
  (30.07., 48'708 B) - ueberholte Sammelfassung mit der falschen UK-Aussage, Loeschkandidat.
- PRUEFUNG 2026-07-31, alle sieben Standort-Entwuerfe gegen `Finance_Dashboard_Audit_All_2026-07-29.csv`
  nachgemessen. SECHS stimmen, EINER war falsch: die UK-Mail behauptete „the UK data we hold starts
  in January 2026, so 2025 is absent". FALSCH - TRUK hat **1'867 Zeilen fuer 2025** und 1'082 fuer
  2026 bis 27.07., der UK-2025-Backfill ist gelaufen. Fehlerkette: die Spalte „weitere
  Auffaelligkeit" in `FINANCE_FELDLUECKEN_STANDORTE_2026-07-30.md` Abschnitt 1 ist die EINZIGE
  Spalte, die nicht aus dem Reproduktionsskript stammt - dort war „2025 fehlt komplett" aus der
  ueberholten 28.07.-Analyse uebernommen und nie nachgemessen. Der Entwurf mit der Falschaussage
  ist geloescht und ersetzt (`-Only TRUK` am Skript ergaenzt), fuer TRUK ist damit NICHTS offen.
  ZWEITER BEFUND, gegen eine Fremdauswertung die fuer TRUK `0` Lieferanten zeigte: es gibt in dieser
  Quelle KEINEN Fall „SupplierNumber gepflegt, SupplierName leer" - beide Felder sind immer
  gemeinsam gefuellt (TRUK 2'955/2'955, TRIT 13'925/13'925, TRIN 809/809, TRFR 134/134, TRUS 6/6,
  CH/AT/DE/ES 0/0). Jene Tabelle stimmt in JEDER anderen Zelle mit dem Audit-CSV ueberein, nur die
  UK-Zeile weicht ab und traegt noch den alten Statustext „Mapping jetzt da - braucht noch den
  Reimport": mutmasslich eine unveraendert uebernommene Zeile, keine Messung. Trafag-Erkennung
  gegengeprueft: TRFR 83 intern / 51 extern, TRIN 677/132, TRIT 6'848/7'077, TRUK 2'803/152,
  TRUS 2/4 - der Regex greift ueberall, wo ein Lieferant steht; das fehlende Feld ist der Engpass.
- WERKZEUG 2026-07-31, grafische Mailfassung: `docs/mails/Build-StandortMails.ps1` baut die sieben
  Standortmails mit Outlook-taugliche Grafiken (Balken Artikelstamm/Rechnungszeilen, Feld-Schema
  `Purchasing Data` -> `Preferred Vendor`, Statustabelle DE, Vorher/Nachher-Kasten zum RTF-Muell,
  Monatsstreifen ES 2026, Standort- und Jahresvergleich UK). `-Mode Preview` (Default) schreibt
  `.tmp_standort_mails/Vorschau_Standortmails.html`, `-Mode Draft` legt Outlook-Entwuerfe an
  (schreibt ins Postfach, sendet nichts). Ausgabeordner ist gitignored - enthaelt Empfaengeradressen.
  BEFUND, WICHTIG FUER KUENFTIGE VERSUCHE: `MailItem.SaveAs` ist auf diesem Arbeitsplatz GESPERRT -
  `.msg`, `.oft` und `.txt`, jeder Zielordner, immer `E_ABORT` (0x80004004). Endpoint-Security/DLP,
  kein Skriptfehler; `MailItem.Save()` in Entwuerfe geht. `Word.Application`-COM haengt ebenfalls,
  daher kein automatisches .docx - Vorschau-HTML von Hand in Word oeffnen. Zwei Darstellungsregeln:
  Artikelbalken mit EXAKTEN Stueckzahlen, Zeilenbalken NUR in Prozent (die Zeilenzahlen je Kategorie
  waeren aus gerundeten Prozenten abgeleitet und wuerden Scheingenauigkeit erzeugen); keine Bilder,
  nur Tabellen mit `bgcolor`, weil Outlook externe Bilder beim Empfaenger blockiert.
- DOKU 2026-07-31, versandfertige Einzelmails je Standort (kein Code):
  `docs/FINANCE_FELDLUECKEN_MAILS_2026-07-31.md`. Der Sammeltext aus
  `FINANCE_FELDLUECKEN_STANDORTE_2026-07-30.md` Abschnitt 6 ist in SIEBEN Einzelmails aufgeteilt,
  jede mit To/Subject/Anrede; Zahlen unveraendert (Messung 29.07.2026, 95'168 Zeilen). Aenderungen
  an Zahlen kuenftig ZUERST im Sammeltext, dann uebernehmen. UK-EMPFAENGER NACHGETRAGEN:
  `Cornell.Williams@trafag.com` (von Ingo geliefert) - damit sind 5 von 7 versandfertig.
  NOCH BLOCKIERT: TRFR und TRUS, es fehlt AUSSCHLIESSLICH die Adresse, die Mails stehen.
  Drei bewusste Textentscheide: (1) Italien-Mail beginnt mit der ausdruecklichen Abgrenzung
  „a separate topic from the inventory valuation discussion ... no deadline attached", sonst liest
  Paola sie als Erinnerung an ihre Zusage Ende August; Verweis auf B1-Upgrade 03.08. (2) Indien geht
  per To NUR an RanVijay, `tasc.co.in` bleibt Cc-Option nach Klaerung, und das Listenangebot ist auf
  „I can send you the list directly" umformuliert, damit die 1'271 Artikelnummern nicht in einen
  Fremddomain-Cc laufen. (3) Spanien-Mail zieht den Range-Export von Punkt 3 auf Punkt 1 vor (das ist
  die sichtbare 2026-Luecke) und verweist auf den bereits dokumentierten Befehl in
  `FINANCE_BACKFILL_UK_ES_2026-07-28.md`. UK-Mail ohne den „was wir nicht brauchen"-Vorspann, weil
  reine Rueckfrage nach 2025. Am Dateiende eine Versandtabelle - ohne die ist spaeter nicht
  unterscheidbar, ob ein Standort nicht geantwortet oder nie eine Mail bekommen hat.
- DOKU 2026-07-31, zentrales Ansprechpartner-Register angelegt (kein Code): `docs/ANSPRECHPARTNER.md`,
  im `RAG_ROUTER.md` als eigenes Thema und ueber Suchwoerter verlinkt. Anlass: Kontaktangaben lagen
  in DREI Dokumenten verstreut - Rollen in der Ticket-Beteiligtenliste `saptasks/zzprdat-kontext.md:189`,
  Domaenenzustaendigkeiten in `docs/INGO_TODOS_180_TAGE_2026-06-18.md:23-25`/`:159-166`, Adressen nur
  in `docs/FINANCE_FELDLUECKEN_STANDORTE_2026-07-30.md:137`. Beim BLP-Vorfall 2026-07-30 war dadurch
  klar WER zustaendig ist, aber nicht wie er zu erreichen ist. Inhalt: Standortempfaenger (Abschnitt 1),
  interne Rollen (2), externe Partner (3), Eskalationspfad plus Paola-Doppelvorgang und
  `tasc.co.in`-Einschraenkung (4), Verwechslungsgefahren (5), offene Luecken (6).
  REGEL DER DATEI: keine aus Namensmustern abgeleiteten Adressen - was nicht belegt ist, steht als
  `offen`. GROESSTE LUECKE: fuer KEINEN internen Ansprechpartner (Lucas, Adil, Fabio, Andreas, Sonja,
  Alex, Ramon) ist eine Adresse dokumentiert, und Andreas/Sonja/Alex/Ramon haben nicht einmal einen
  belegten Nachnamen. Neu festgehaltene Verwechslungsgefahr: ZWEI Marcos - Marco Di Menco (Etiketten/PP)
  vs. Marco Widmer (Einkauf, Autor `PURCHASING_DASHBOARD_REVIEW_MARCO_2026-07-10.md`).
- DOKU 2026-07-30, Empfaenger fuer die Standort-Mail (kein Code): `docs/FINANCE_FELDLUECKEN_STANDORTE_2026-07-30.md`
  hat einen neuen Abschnitt 5 `Empfaenger` (alter Abschnitt 5 Mailtext -> 6, Reproduzierbar -> 7).
  Anlass: In KEINEM `.md` des Repos stand vorher eine Mailadresse (Regex ueber alle Markdown-Dateien:
  null Treffer) - benannt war als einziger Standortkontakt Paola (TR IT), und zwar nur fuer das
  Bewertungsthema. Von Ingo recherchiert und jetzt festgehalten: ES `Santi.Gomez@trafag.es`,
  DE `Rohail.Munir@trafag.de`, IT `Paola.Castagna@trafag.com`, IN `RanVijay.Kumar@trafag.com` plus
  `agupta@tasc.co.in` und `chandra.s@tasc.co.in`. DREI PUNKTE, die beim Versand zaehlen:
  (1) **FR und US fehlen weiterhin** - genau die zwei Standorte mit einer echten Bitte im Text
  (FR 374 von 433, US 518 von 521 Artikeln ohne `OITM.CardCode`), FR ist sogar der kleinste Aufwand
  aller Standorte. (2) **Paola laeuft doppelt**: sie ist bereits Adressatin des Standardkosten-/
  Bewertungsthemas mit Zusage Ende August (B1-Go-Live 2026-08-03 + 2 Wochen Ferien) - die Bitte hier
  ist Stammdatenpflege und ein anderer Vorgang; getrennt verschicken, sonst kostet es den klaren
  Termin des Bewertungsthemas. Querverweis dazu jetzt auch in
  `docs/FINANCE_STANDARDKOSTEN_SITZUNG_ANDREAS_2026-07-27.md` Abschnitt 5d. (3) **`tasc.co.in` ist
  keine Trafag-Domain** - zwei der drei indischen Adressen liegen bei einem externen Dienstleister.
  Der Mailtext selbst ist unkritisch, aber er enthaelt das Angebot "we can send the item list"
  (1'271 Artikelnummern des TRIN-Stamms); vor dem Versand einer solchen Liste an eine Fremddomain
  klaeren, ob das gedeckt ist, im Zweifel nur an die Trafag-Adresse und von dort weiterverteilen.
  Nicht verwechseln: `Hugo Cuesta` in Abschnitt 3 ist ein Artikeltext aus den Spanien-Daten
  (`- Entregado por Hugo Cuesta`, 377 Zeilen), kein Ansprechpartner.
- GEFIXT 2026-07-30 (Entscheid Ingo, Variante B): Das naechtliche Einkauf-Delta haengt nicht mehr an
  `Sites.IsActive`, sondern nur noch daran, DASS die Site `PURCHASING_SAP` konfiguriert ist. Damit
  bleibt `IsActive = 0` und der Sales-Export unveraendert - die Variante mit dem Ausfiltern in
  `ExportAllAsync` wurde bewusst NICHT genommen, weil sie die Strecke anfasst, die Andreas'
  Finanzzahlen fuettert. Zusaetzlich wird das Ueberspringen jetzt als `Warning` geloggt: der stille
  Aussteiger war der eigentliche Grund, warum der Ausfall sechs Tage unentdeckt blieb. Fehlende
  Zugangsdaten meldet `RunDeltaAsync` selbst als `Error`-Status, statt vorab geprueft zu werden -
  dann ist die Ursache im Refresh-Status sichtbar statt unsichtbar. NACHSORGE: Delta-Button im
  Einkaufs-Dashboard einmal druecken, damit nicht bis zum Nachtlauf gewartet werden muss; danach
  muss in `PurchasingSyncState` ein `Delta`-Eintrag stehen und die Meldung die Zahl der
  nachklassifizierten Cachezeilen nennen.
## Offene Punkte aus aelteren Eintraegen (Original im Archiv)

- Server/IIS (seit 2026-07-08, nur direkt am Server moeglich, WinRM gesperrt): App-Pool `startMode=AlwaysRunning` + `processModel.idleTimeout=00:00:00` setzen, damit der 12:00-Timer ohne vorherigen HTTP-Request laeuft. Bis dahin holt `CatchUpMissedRunAsync` verpasste Tageslaeufe beim naechsten Prozessstart nach.
- Betriebshinweis DE/Alphaplan (seit 2026-07-03): Der Alphaplan-Upload nach SharePoint muss VOR dem 12:00-Timer laufen, sonst verwendet der Tagesexport noch den vorherigen ZIP-Stand.

## Aeltere Eintraege / Historie

- Kurzstand-Eintraege 2026-06-04 bis 2026-07-08 und alle Nachtrag-Abschnitte (Mai/Juni 2026): verbatim in `docs/raw_md_archive/LASTCHANGE_ARCHIV_bis_2026-07-12.md`.
- Kurzstand-Eintraege 2026-07-13 bis 2026-07-30: verbatim in `docs/raw_md_archive/LASTCHANGE_ARCHIV_2026-07-13_bis_2026-07-30.md`.
- Kanonische Detailhistorie davor: `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`; Original-Volltexte: `docs/raw_md_archive/original_history_raws.zip` (nur zur Wiederherstellung).

## Einstieg / Router

- Themenrouter (zuerst laden): `docs/RAG_ROUTER.md`.
- Fuehrender Kurzkontext: `docs/rag/PROJECT.md`.
- Naechster Chat: `docs/RAG_ROUTER.md` -> diese Datei -> passende Themen-Kurzdatei aus `docs/rag/`.
