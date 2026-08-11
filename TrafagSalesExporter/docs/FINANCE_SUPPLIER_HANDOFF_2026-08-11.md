# Handoff: Supplier-Länderstatus und CH/AT-Prüfung

**Stand:** 11.08.2026  
**Status:** Analyse und Word-Bericht abgeschlossen; fachliche Restentscheidung bei Andreas offen  
**Autor dieser Runde:** Codex  
**Zweck:** Vollständige Wiederaufnahme nach Chatabbruch oder Kontextverlust

## 1. Auftrag und fachlicher Kontext

Ausgangspunkt ist Issue 7 „Sales Database – Lieferant wird bei vielen Gesellschaften nicht
angezeigt“. Gewünscht waren:

1. den aktuellen Supplier-Stand aller Länder prüfen;
2. CH/AT soweit technisch möglich selbst untersuchen, damit Andreas nur noch die fachliche
   Restfrage prüfen muss;
3. klären, ob die CH/AT-Regel eine Zirkularreferenz erzeugt;
4. einen kopierfertigen nächsten Schritt und Kommentar für Issue 7 formulieren;
5. am Schluss ein gut gestaltetes Word-Dokument mit allen Ländern und dem Thema Supplier
   erstellen.

Hinweis zur parallelen Arbeit: Claude hat in derselben Zeit den UK-2025-Finance-Fall geprüft.
Die Bereiche wurden über `docs/AGENT_COORDINATION.md` getrennt. Claudes Werkzeug
`.tmp_tools/CheckUk2025Result/**` und seine Finance-Änderungen wurden nicht berührt.

## 2. Ergebnis in einem Satz

CH/AT sind **kein Zirkularreferenzproblem**. Die TSC-Regel bestimmt die liefernde
Konzerngesellschaft, während `WAVWR_DC / FKIMG` beziehungsweise `STPRS_HC` getrennt die
Kostenbasis bestimmen. Offen ist nur, ob Materialien mit einem nachweisbaren externen
Einkaufsbeleg eine Ausnahme von der Herstellerregel bilden sollen.

### Klarstellung zum Materialnummer-Fallback

Der Code-Stand vor dem Entscheid verglich bei fehlendem Supplier **nicht** gegen eine Liste
„von TRCH nachweislich produzierte Materialien“. Die tatsaechliche Reihenfolge ist:

1. `TRCH` und `TRAT` werden allein aufgrund der TSC als `Intern / TR_AG`
   klassifiziert;
2. bei anderen Standorten gewinnt ein vorhandener Supplier;
3. ohne Supplier entscheidet, falls vorhanden, der Sales Type (`FFM`, `CM`, `LRD`);
4. erst danach gilt ein Materialtreffer in `GroupStandardCosts`, aktuell
   Bewertungskreis `1100` der Trafag AG, als provisorischer Intern-Fallback.

Ein Eintrag in `MBEW`/Bewertungskreis 1100 belegt eine Schweizer Bewertung und
Kostenbasis, aber nicht automatisch eine Schweizer Eigenfertigung. Die Regel
„gleiche Materialnummer in einer verlaesslichen TRCH-Produktionsliste = in TRCH
eigenproduziert“ waere deshalb eine neue, strengere Fachregel. Dafuer sollte eine
echte Produktionsinformation wie `BESKZ E`, Fertigungsauftrag oder belastbare
Stuecklisten-/Produktionshistorie fuehrend sein, nicht die blosse Existenz der
Materialnummer in CH.

**Fachliche Praezisierung durch Ingo am 2026-08-11:** Gewuenscht ist kein
Produktionsnachweis, sondern ein Konzern-Stammdaten-Fallback. Wenn bei einem
Fremdstandort alle Supplier-Felder fehlen und die normalisierte Materialnummer in
den Stammdaten der Trafag AG CH vorkommt, gilt die Zeile als konzernintern von
`TR_AG` beliefert. Ein explizit gepflegter Supplier hat weiterhin Vorrang; ohne
CH-Treffer bleibt die Zeile `Unklar`.

Der bis zur Umsetzung aktive Code bildete dies weitgehend ueber einen Treffer in
`GroupStandardCosts`, aktuell Schweizer Bewertungskreis `1100`, ab. Er prueft
damit die CH-Bewertungs-/Kostentabelle und nicht die vollstaendige MARA-/MARC-
Materialstammliste. CH-Materialien ohne Eintrag in `GroupStandardCosts` werden
vom aktuellen Fallback folglich nicht erfasst.

### Messung des neuen CH-Stammdaten-Fallbacks am 2026-08-11

Als fachlich passende CH-Stammdatenmenge wurde `MARC`, Werk `1100`, verwendet.
Die mandantenweite `MARA` waere weiter gefasst und wuerde nicht belegen, dass ein
Material im Werk der Trafag AG Schweiz gefuehrt wird. Verglichen wurden nur Zeilen
von Fremdstandorten, bei denen alle drei Supplier-Felder leer sind und auch kein
erkannter Sales Type (`FFM`, `CM`, `LRD`) entscheidet. Das entspricht genau der
Stelle, an der der Material-Fallback im bisherigen Code griff.

Datenstand der read-only Messung: produktive Datenbank mit **96.298 Sales-Zeilen**
und aktueller SAP-OData-Stamm am 2026-08-11.

| Messwert | Aktuell: MBEW/GroupStandardCosts 1100 | Neu: MARC Werk 1100 | Differenz |
| --- | ---: | ---: | ---: |
| Materialien im CH-Vergleichsbestand | 63.550 | 66.047 | +2.497 |
| als intern erkannte Fallback-Zeilen | 10.097 | 10.817 | **+720** |
| unterschiedliche neu getroffene Materialien in Verkaufszeilen | - | 392 | **+392** |
| bisherige Intern-Treffer, die entfallen | - | - | **0** |

Die neue Regel erweitert den Trefferbestand somit um **720 von 22.840 relevanten
Fallback-Kandidaten (3,2 %)**. Bezogen auf alle 96.298 Sales-Zeilen sind es **0,7 %**.
Gegenueber den heute 10.097 Material-Fallback-Treffern ist dies eine Zunahme um
**7,1 %**. Die noch unklaren Kandidaten sinken von 12.743 auf 12.023 Zeilen.

| TSC | relevante Kandidaten | zusaetzlich intern mit MARC 1100 |
| --- | ---: | ---: |
| TRDE | 7.332 | 8 |
| TRES | 5.712 | 0 |
| TRFR | 2.454 | 28 |
| TRIN | 114 | 0 |
| TRIT | 5.740 | 674 |
| TRUS | 1.488 | 10 |
| **Gesamt** | **22.840** | **720** |

Der Unterschied konzentriert sich damit fast vollstaendig auf Italien. `MARC 1100`
enthaelt alle 63.550 aktuellen MBEW-1100-Materialien; die Umstellung wuerde deshalb
keine bisherige Intern-Zuordnung verlieren. Fachlich bleibt die Grenze bestehen:
Ein Werkstamm-Treffer belegt die Pflege des Materials in CH, aber fuer sich allein
keine konkrete Warenbewegung von CH an den Fremdstandort. Diese Interpretation ist
die von Ingo am 2026-08-11 vorgegebene Fallback-Regel.

### Umsetzungsstand nach dem Entscheid

Der neue MARC-1100-Fallback ist implementiert und unter `Admin Bereich > Settings`
gegen die bisherige MBEW-Regel umschaltbar; Default ist die neue Variante. Der
Werkstamm wird separat in `GroupMaterialMasters` gespeichert, damit ein reiner
Stammdatentreffer keine Kosten erfindet. Bei leerem MARC-Cache greift automatisch
der alte Pfad. Nachweis und aktueller Deploymentstatus stehen vollstaendig in
`docs/FINANCE_SUPPLIER_FALLBACK_UMSCHALTER_2026-08-11.md`.

### Produktivnachtrag 2026-08-11

Der neue Stand wurde nach Nutzerfreigabe produktiv deployed. Live bestaetigt sind
`SupplierFallbackMode=ChPlantMaster` und `66.049` unterschiedliche MARC-Materialien
fuer Werk 1100 vor und nach dem finalen App-Neustart. Alle `63.550` bisherigen
MBEW-Schluessel sind enthalten; der Sales-Bestand blieb bei `96.298` Zeilen.
Backup, DLL-Hash, Routen und der weiterhin offene Einkaufs-ZDISPO-Blocker stehen in
`docs/DEPLOY_GESAMTSTAND_2026-08-11.md`.

## 3. Verbindliche Ergebnisdateien

| Datei | Inhalt |
| --- | --- |
| `docs/SUPPLIER_LAENDERSTATUS_CH_AT_PRUEFUNG_2026-08-11.md` | vollständiger prüfbarer Bericht in Markdown |
| `docs/Supplier_Laenderstatus_CH_AT_Pruefung_2026-08-11.docx` | gestalteter Word-Bericht für Finance/Andreas |
| `docs/Supplier_Laenderstatus_CH_AT_Pruefung_mit_Fallback_2026-08-11.docx` | aktualisierter, OpenXML-valider Word-Bericht inklusive Alt/Neu-Messung, Umschalter und Deploymentstatus |
| `docs/FINANCE_SUPPLIER_HANDOFF_2026-08-11.md` | dieser Wiederaufnahmestand |
| `docs/AGENT_COORDINATION.md` | Agentenabgrenzung und kurze Übergabe |

Der ursprüngliche Word-Bericht wurde mit `OpenXmlValidator` geprüft. Die aktualisierte
Fallback-Fassung ist ebenfalls **valide**. Da die Ursprungsdatei beim Aktualisieren in
Word geöffnet und gesperrt war, wurde sie bewusst nicht überschrieben, sondern unter
dem zusätzlichen Namen mit `_mit_Fallback_` gespeichert.

## 4. Verwendete Daten – ausschließlich read-only

### Aktueller zentraler Export

- Datei: `neu.xlsx` im Repository-Stamm
- Blatt: `Sales`
- Stand beim Lauf: 11.08.2026, 09:45 Uhr
- Umfang: **96.233** gültige Sales-Zeilen
- Die Datei wurde nur gelesen und weder ersetzt noch verändert.

### Produktive SQLite-Datenbank

```text
\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\trafag_exporter.db
```

- Stand beim Lauf: 11.08.2026, 09:28 Uhr
- Verbindung ausschließlich mit `SqliteOpenMode.ReadOnly`
- Keine Inserts, Updates, Deletes, Migrationen oder Deployments

## 5. Aktueller Supplier-Stand aller Länder

„Supplier vollständig“ bezeichnet die vollständig vorhandenen drei Felder Supplier number,
Supplier name und Supplier country. In den gemessenen Daten treten diese gemeinsam auf.

| Land | TSC | Zeilen | Supplier vollständig | Quote | Leer |
| --- | --- | ---: | ---: | ---: | ---: |
| Schweiz | TRCH | 47.142 | 0 | 0,0 % | 47.142 |
| Österreich | TRAT | 1.790 | 0 | 0,0 % | 1.790 |
| Deutschland | TRDE | 7.332 | 0 | 0,0 % | 7.332 |
| Spanien | TRES | 5.697 | 0 | 0,0 % | 5.697 |
| Frankreich | TRFR | 2.598 | 135 | 5,2 % | 2.463 |
| Indien | TRIN | 7.116 | 828 | 11,6 % | 6.288 |
| Italien | TRIT | 19.955 | 14.208 | 71,2 % | 5.747 |
| Vereinigtes Königreich | TRUK | 3.064 | 3.064 | 100,0 % | 0 |
| USA | TRUS | 1.539 | 6 | 0,4 % | 1.533 |
| **Gesamt** |  | **96.233** | **18.241** | **19,0 %** | **77.992** |

Wichtige Einordnung:

- UK ist abgeschlossen.
- Italien ist weit fortgeschritten; die verbleibenden 5.747 Zeilen sind gezielt nach Ursache
  zu segmentieren.
- Indien darf nicht anhand der physischen Supplier-Quote allein bewertet werden. In der
  aktuellen Produktiv-DB tragen **6.686 von 7.116 Zeilen (94,0 %)** einen Sales Type:
  `FFM = 5.944`, `LRD = 719`, `CM = 23`, leer = `430`.
- Für Indien ist deshalb keine pauschale Supplier-Massenpflege sinnvoll.
- Bei DE, ES, FR und US fehlt weiterhin ein ausreichendes Quellfeld beziehungsweise Mapping.

## 6. CH/AT-Grundmenge und Kostenprüfung

| Kennzahl | Ergebnis |
| --- | ---: |
| CH/AT-Zeilen | 48.932 |
| unterschiedliche Materialien | 8.557 |
| Zeilen ohne Materialnummer | 0 |
| Standardkosten > 0 | 47.350 / 48.932 = 96,8 % |
| Produktsparte zugeordnet | 48.752 / 48.932 = 99,6 % |

Nach TSC:

| TSC | Zeilen | Standardkosten > 0 |
| --- | ---: | ---: |
| TRCH | 47.142 | 45.561 |
| TRAT | 1.790 | 1.789 |

### Kostenwährung und Fallback

Für TRCH wurden **18.723 Fremdwährungszeilen** gefunden:

- **18.068** verwenden die Belegwährung als Kostenwährung. Das entspricht dem
  `WAVWR_DC / FKIMG`-Pfad.
- **655** verwenden CHF als Kostenwährung und sind damit Fallback-Kandidaten.
- Bei **104** dieser Fallback-Fälle ist der aufgelöste CHF-Standardpreis positiv.

Geprüfte Beispiele:

| Material | Beleg/Position | Stückkosten | Kostenwährung | Belegwährung | Pfad |
| --- | --- | ---: | --- | --- | --- |
| 61645 | 90369725/10 | 218,4032 | EUR | EUR | WAVWR_DC / FKIMG |
| 64941 | 90380614/10 | 147,2964 | EUR | EUR | WAVWR_DC / FKIMG |
| 42703 | 90362380/10 | 243,7517 | USD | USD | WAVWR_DC / FKIMG |
| B99999 | 90381743/10 | 0,23 | CHF | EUR | STPRS_HC-Fallback |

Messgrenze: `CentralSalesRecords` speichert nur den bereits aufgelösten Stückpreis und seine
Währung. Der ursprüngliche Rohwert `WAVWR_DC` wird dort nicht gespeichert. Die lokale Prüfung
bestätigt deshalb den verwendeten Währungspfad, kann aber den SAP-Rohwert nicht erneut
ausrechnen. Der Originalnachweis steht in
`docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md`.

## 7. CH/AT-Beschaffungsprüfung

Verwendete Indizien:

1. gleicher normalisierter Materialschlüssel an anderen TSCs mit Trafag/GFS-Supplier;
2. Stücklisten-/BOM-Kopf im `MaterialUsageCache`;
3. `BESKZ E` als Eigenfertigungsindiz;
4. `BESKZ F` oder echter, nicht gelöschter EKPO/EKKO-Einkaufsbeleg als Fremdbezugsindiz.

Konservatives kombiniertes Ergebnis:

| Arbeitsklasse | Materialien | CH/AT-Zeilen | Anteil |
| --- | ---: | ---: | ---: |
| intern gut gestützt | 734 | 8.045 | 16,4 % |
| Fremdbezugs-Prüfliste | 1.191 | 5.910 | 12,1 % |
| ohne direkten Nachweis | 6.632 | 34.977 | 71,5 % |

Zusätzliche Kreuzstandortmessung:

- 756 Materialien / 8.432 CH/AT-Zeilen haben an anderen Standorten ausschließlich
  Trafag/GFS-Supplier als Hinweis.
- Genau ein Material besitzt ausschließlich einen externen Kreuzstandort-Hinweis:
  `R13025`, vier TRCH-Zeilen; an TRIN Supplier `Somax Enterprise Co. Ltd.`.
- Es gab keinen Kreuzstandortfall mit gleichzeitig internem und externem Supplier.

Wichtige Grenze: `MaterialUsageCache` enthielt beim Lauf nur **105 Zeilen**. Fehlende
BOM-Evidenz darf daher keinesfalls als fehlende Eigenfertigung interpretiert werden.

## 8. Priorisierte Stichprobe für Andreas

Andreas soll keine 6.632 oder 1.191 Materialien einzeln prüfen. Für die fachliche Entscheidung
reicht zunächst folgende Auswahl:

| Material | Bezeichnung | CH/AT-Zeilen | Supplier-/Einkaufsnachweis | Prüfgrund |
| --- | --- | ---: | --- | --- |
| E11221 | ASIC TRAFAG TX2a MLPQ32 | 71 | Presto Engineering France | zugekaufte Elektronik |
| E01389 | Schnappschalter Marquardt | 30 | Omni Ray / Marquardt | zugekauftes Bauteil |
| C13614 | Diagnostic Valve Block | 56 | Sole Solution / Hwajin | möglicher Handels-/Baugruppenfall |
| E11155 | ASIC TRAFAG TR5 | 18 | Aptasic u. a.; BESKZ F | stärkstes Fremdbezugsindiz |
| D34604 | Cover with opening coated | 35 | Fuchia Electron | zugekauftes mechanisches Teil |
| E11228 | ASIC TRAFAG TX2D | 22 | Presto Engineering France | zugekaufte Elektronik |
| F88103 | 8854 Transmitter EX | 65 | STS Sensor Technik Sirnach | fertiges Produkt, besonders relevant |
| D85031 | Metallbalg NG36 | 12 | Heitz GmbH | zugekauftes Teil |
| E11220 | ASIC TRAFAG TX1b | 13 | Presto / Aptasic | zugekaufte Elektronik |
| C15414 | Vessel Flange | 46 | Plattner / CPT | zugekauftes mechanisches Teil |
| R13025 | Gehäuse-Unterteil Industat | 4 | an TRIN: Somax Enterprise | einziger externer Kreuzstandort-Hinweis |

## 9. Einzige noch offene Fachentscheidung

Andreas soll beantworten:

> Gilt für Verkäufe von TRCH/TRAT die Herstellerregel `Intern / liefernde Gesellschaft TR_AG`
> auch dann, wenn zum verkauften Material ein externer Einkaufsbeleg existiert, oder sollen
> solche Materialien als Ausnahme nach `Extern` klassifiziert werden?

Folgen:

- **Einkaufsbeleg ist keine Ausnahme:** Die bestehende CH/AT-Regel bleibt. Die 6.632
  Materialien ohne Cache-Nachweis benötigen keine Einzelprüfung.
- **Einkaufsbeleg ist eine Ausnahme:** Nicht blind alle 1.191 Materialien umklassifizieren.
  Zuerst eine Zusatzregel definieren, beispielsweise anhand Materialart, Produktrolle oder
  bestätigter Handelsware.
- Der beste erste fachliche Test sind `F88103` und `R13025`.

## 10. Copy-ready für Issue 7

### Nächster Schritt

> UK ist vollständig; Italien und Indien sind nachgezogen. Für CH/AT die Herstellerregel als
> Standard beibehalten und mit Andreas nur anhand der priorisierten SAP-Stichprobe entscheiden,
> ob Materialien mit externem Einkaufsbeleg als Ausnahme gelten. Für DE/ES sowie FR/US
> anschließend Quellfelder beziehungsweise Mapping prüfen und die Supplier-Quote neu messen.

### Kommentar

> Supplier-Felder sind weiterhin nur in 18.241 von 96.233 Sales-Zeilen vollständig (19,0 %),
> die Situation ist je Land jedoch unterschiedlich: UK 100,0 %, IT 71,2 %, IN 11,6 %
> physisch, aber 94,0 % über Sales Type klassifizierbar; CH/AT, DE und ES 0,0 %, FR 5,2 %,
> US 0,4 %. CH/AT sind kein Zirkularreferenzproblem: Die TSC klassifiziert die liefernde
> Konzerngesellschaft, während WAVWR_DC/FKIMG beziehungsweise STPRS_HC separat die Kostenbasis
> liefern. Offenes Risiko sind nur mögliche Fremdbezugs-Ausnahmen. Die SAP-Vorprüfung markiert
> dafür 1.191 Materialien; Andreas erhält eine priorisierte Stichprobe statt einer Vollprüfung.

## 11. Analyse- und Berichtswerkzeuge

Die Werkzeuge sind lokal unter `.tmp_tools/` angelegt und ändern keine Produktivdaten.

| Werkzeug | Zweck |
| --- | --- |
| `.tmp_tools/CheckChAtOrigin` | `neu.xlsx`, Kreuzstandortdaten, BOM/BESKZ und Einkaufshistorie auswerten |
| `.tmp_tools/CheckChAtCosts` | CH/AT-Kostenabdeckung, Kostenwährungen und Indien-Sales-Type messen |
| `.tmp_tools/BuildSupplierReport` | das Word-Dokument erzeugen und mit OpenXML validieren |
| `.tmp_tools/CompareSupplierFallback` | aktuellen MBEW-1100-Fallback read-only gegen den vorgeschlagenen MARC-Werk-1100-Fallback messen |
| `.tmp_tools/RefreshChPlantMaterialMaster` | MARC-1100-Bestand validieren und nach dem Deploy mit `--apply` atomar backfillen |

### Vollständige CH/AT-Auswertung erneut starten

Aus dem Repository-Stamm in PowerShell:

```powershell
dotnet run --project '.tmp_tools\CheckChAtOrigin\CheckChAtOrigin.csproj' -- `
  'neu.xlsx' `
  'Sales' `
  '\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\trafag_exporter.db'
```

Laufzeit beim letzten Lauf: ungefähr 171 Sekunden.

### Kosten- und Sales-Type-Messung erneut starten

```powershell
dotnet run --project '.tmp_tools\CheckChAtCosts\CheckChAtCosts.csproj' -- `
  '\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\trafag_exporter.db'
```

### Word-Dokument erneut erzeugen

```powershell
dotnet run --project '.tmp_tools\BuildSupplierReport\BuildSupplierReport.csproj' -- `
  'docs\Supplier_Laenderstatus_CH_AT_Pruefung_2026-08-11.docx'
```

Erwartetes Ergebnis:

```text
OpenXML : valide
```

## 12. Relevante bestehende Code- und Fachdokumentation

- `Services/GroupMarginSupplierClassifier.cs`
  - CH/AT-TSCs werden unabhängig von Supplier-Feldern als intern klassifiziert.
  - Das ist eine explizite Fachregel und keine rekursive Berechnung.
- `Services/GroupMarginCalculator.cs`
  - Die Kostenregel ist eine endliche Prioritätskette: Gruppenstandardkosten,
    Distribution ohne Gruppenkosten, lokale Standardkosten.
- `Services/SapCompositionService.cs`
  - CH/AT: `WAVWR_DC / FKIMG` ist führend, `STPRS_HC` ist Fallback.
- `docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md`
  - SAP-Rohwert- und Währungsnachweise für CH/AT.
- `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md`
  - Indien: Sales Type und Trafag-Materialnummer als belastbare Ersatzklassifikation.
- `docs/Issue_Log_Kommentare_2026-08-11.tsv`
  - bestehende Issue-Log-Zusammenfassung.

## 13. Was bewusst nicht gemacht wurde

- keine Änderung an Produktivdaten;
- keine Änderung an Klassifikations- oder Finance-Anwendungscode;
- keine Änderung an `neu.xlsx`;
- kein Deployment;
- keine E-Mail an Andreas oder andere Gesellschaften;
- keine automatische Umklassifizierung der 1.191 Materialien mit Einkaufsbelegen;
- keine Übernahme oder Rücksetzung von Claudes parallelen Änderungen.

## 14. Wiederaufnahme nach Chatabbruch

Ein neuer Agent oder Chat soll in dieser Reihenfolge vorgehen:

1. diese Datei vollständig lesen;
2. `docs/AGENT_COORDINATION.md` lesen und parallele Bereiche prüfen;
3. den Bericht `docs/SUPPLIER_LAENDERSTATUS_CH_AT_PRUEFUNG_2026-08-11.md` öffnen;
4. nur dann neu messen, wenn `neu.xlsx` oder die produktive DB einen neueren Stand besitzt;
5. nach Andreas' Antwort die Entscheidung dokumentieren;
6. Anwendungscode nur auf ausdrücklichen Auftrag ändern;
7. bei einer Codeänderung neue Tests und eine Vorher-/Nachher-Messung ergänzen.

Der nächste sinnvolle fachliche Schritt ist **nicht** weitere technische Analyse, sondern die
Antwort von Andreas auf die Ausnahmefrage aus Abschnitt 9.

## 15. Nachfolgende UI-Änderung in derselben Sitzung

Nach Abschluss des Supplier-Themas wurden die zwei linken Admin-Einstiege zu einem äußeren
`Admin Bereich` zusammengeführt. Der vollständige Stand, die neue Menüstruktur, Migration und
Tests stehen in `docs/ADMIN_MENUE_ZUSAMMENFUEHRUNG_2026-08-11.md`. Diese UI-Änderung und der
neue FPV-Spielstand wurden am 11.08.2026 um 11:23 Uhr produktiv deployed und technisch
verifiziert. Die Supplier-Analyse selbst blieb read-only und erforderte keine
Produktivänderung.
