# Einkaufsdashboard 2026-06-05

Nachtrag 2026-06-18: Das Einkaufsdashboard wurde fuer die Management-/Einkaufssicht nachgezogen und deployed. Schwerpunkt war die Excel-aehnliche Lieferant/Jahr-Kaskadierung analog Referenzbild `einkauf.png`, Zeitraum 2020 bis aktuelles Jahr, Spend aktuelles Jahr je Lieferant, offene Bestellungen/Zulauf, Filter fuer Loeschkennzeichen und MARA-MSTAE sowie echte Lieferantennamen statt Platzhalter.

Nachtrag 2026-07-31, finaler Praesentationsstand der Spend-Matrix (Commits
`4a3271b`, `f740eb9`, `4498bd4`): dunkler Primaertext und deutlichere
Ebenenhintergruende; Tabellenkopf, Lieferanten, Warengruppen und Materialien
fett (`700`); Lieferanten/Warengruppen `1.05rem`, Materialien `1rem`.
Produktiv veroeffentlicht und verifiziert.

## Ziel

Der neue Bereich `Einkauf` soll die vorhandene Power-BI-Vorlage `x.pbix` aufnehmen und um weitere SAP-Einkaufsanalysen ergaenzen.

## Aus `x.pbix` uebernommene Struktur

Analysierte PBIX-Seiten:

- Beschaffungsvolumen CHF je Lieferant.
- Einkaufsvolumen CHF je Lieferant als Kuchenansicht.
- Balkenansicht Volumen je Lieferant und Warengruppe.
- Diagramm Volumen je Warengruppe.
- Einkaufsvolumen CHF je Region.
- Preisentwicklung CHF.
- Matrix Volumen je Warengruppe.

Sichtbare PBIX-Felder:

- `EKPOSet.Netwr CHF`
- `EKPOSet.Netwr CHF/Stk`
- `EKKOSet.Bedat`
- `Data.Name`
- `Data (2).WG komplett`
- `EKPOSet.Matnr`
- `EKPOSet.Txz01`

## Zusaetzlich aufgenommene SAP-Themen

Das Dashboard wurde fachlich um diese Bereiche erweitert:

- Spend total vergangen nach Jahr, Lieferant, Warengruppe und Artikel.
- Offene Bestellwerte und offene Mengen nach Lieferant, Warengruppe und Artikel.
- Offene Verpflichtungen / Mengenkontrakte nach Lieferant, Warengruppe und Artikel.
- Lieferantenbewertungen und Performance nach Lieferant, Warengruppe und Artikel.

## Aktueller Implementierungsstand

- Route: `/einkauf`.
- Hauptnavigation: eigener Punkt `Einkauf` mit Einkaufswagen-Icon.
- Tabs im Einkaufsdashboard:
  - Die frueheren Tabs wurden in echte linke Navigationspunkte unter `Einkauf` umgebaut.
- `Einkauf Dashboard`: Uebersicht, SAP-Datenfluss, Live-Status und Analyseachsen.
- `Spend`: Spend total vergangen nach Jahr, Lieferant, Warengruppe und Artikel.
- `Offene Bestellungen`: offene Werte, Mengen und Faelligkeiten.
- `Kontrakte`: offene Verpflichtungen und Kontrakt-Restwerte.
- `Lieferanten`: Lieferantenbasis, Performance und Datenstatus.
- `Ideen`: aufklappbarer Navigationspunkt fuer die naechsten Umsetzungsbausteine.
  - `Uebersicht`.
  - `Einkauf-Datenservice`.
  - `Liefertermin-Risiko`.
  - `Preisabweichung`.
  - `Spend-Konzentration`.
  - `Datenqualitaet`.
- `Kennzahlen-Katalog`: fachlicher KPI-Katalog fuer den naechsten Ausbau.
  - `PBIX Vorlage`: aus `x.pbix` uebernommene Seiten/Visuals.
  - `3D Simulation`: drehbare 3D-What-if-Analyse.
- Unterpunkt `Einkauf > Datenquellen` fuer SAP/OData-Verbindung, Quellen, Join-Fluss und Zielmappings.
- Die Seite ist als Cockpit-Struktur umgesetzt und ueber den vorhandenen UI-Sprachservice mehrsprachig vorbereitet.
- EKKO, EKPO und EKET werden per SAP/OData in lokale Cache-Tabellen geladen.
- Das Cockpit liest zuerst den Cache und nutzt nur noch als Fallback eine begrenzte Live-Probe, falls noch kein Cache vorhanden ist.
- Seit 2026-06-18 ist der Zeitraumfilter standardmaessig auf 2020 bis aktuelles Jahr ausgerichtet.
- Seit 2026-06-18 gibt es eine Excel-aehnliche Kaskadierungstabelle Lieferant x Jahr mit Jahresspalten, Gesamtsumme und Top-down-Sortierung.
- Spend im aktuellen Jahr wird pro Lieferant separat analysiert.
- Bereits beschafft/gebucht und offene Bestellungen/Zulauf werden getrennt visualisiert.
- Geloeschte Positionen (`LOEKZ`) und Materialstatus (`MARA-MSTAE`) sind als Filterdimensionen vorgesehen; `MSTAE` wirkt, sobald das Feld im Cache gefuellt ist.
- Aktive Lieferanten werden aus echten Einkaufsbewegungen abgeleitet; generische Lieferantenplatzhalter werden nicht mehr erzeugt.

## Mehrsprachigkeit Stand 2026-06-11

Commit `1dbaa66 Add purchasing translations` hat die fehlenden UI-Texte fuer den Einkaufsbereich im zentralen `UiTextService` nachgezogen.

Abgedeckt:

- Hauptnavigation: `Einkauf`, `Einkauf Dashboard`, `Einkauf Datenquellen`.
- Einkaufsdashboard: Uebersicht, SAP-Datenfluss, Live-Status, Zeitraumfilter, KPI-Karten, Detailbereiche, Ideen, Kennzahlen-Katalog, PBIX-Vorlage und 3D-Simulation.
- `Einkauf > Datenquellen`: Verbindung, Quellen, Join-Fluss, Mapping, aktuelle Basis, Buttons, Hilfstexte und Speicher-/Reset-Meldungen.
- Sprachen: Spanisch, Italienisch und Hindi.

Bewusst nicht uebersetzt:

- Technische Namen und Feldnamen wie `EKKO`, `EKPO`, `EKET`, `EKKOSet`, `EKPOSet`, `eketSet`, SAP-Felder, Aliasnamen, TSC und Dateimuster.
- Power-BI-Seitentitel aus der importierten PBIX-Vorlage bleiben als fachliche Referenz sichtbar.

Deploy:

- Publiziert am 2026-06-11 auf `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`.
- `BiDashboard.dll` Zeitstempel nach Deploy: `11.06.2026 12:30:27`.
- Validierung vor Publish: `dotnet test TrafagSalesExporter.sln --verbosity minimal`, Ergebnis `92/92` Tests gruen.

## Navigation und Admin-Steuerung

Stand 2026-06-05: Die Einkaufsbereiche sind nicht mehr als obere Tabs im Dashboard versteckt, sondern als eigene URLs umgesetzt:

- `/einkauf`
- `/einkauf/spend`
- `/einkauf/offene-bestellungen`
- `/einkauf/kontrakte`
- `/einkauf/lieferanten`
- `/einkauf/ideen`
- `/einkauf/ideen/datenservice`
- `/einkauf/ideen/liefertermin-risiko`
- `/einkauf/ideen/preisabweichung`
- `/einkauf/ideen/spend-konzentration`
- `/einkauf/ideen/datenqualitaet`
- `/einkauf/kennzahlen`
- `/einkauf/pbix`
- `/einkauf/3d`
- `/einkauf/verbindungen`

Die Defaults werden ueber `NavigationMenuItems` geseedet. Dadurch kann der Admin in `Admin > Menuestruktur` einzelne Einkaufs-Unterpunkte ausblenden, sortieren oder umhaengen.

## SAP/OData-Konfiguration

Vorbefuellte Quellen:

- `EKKO -> EKKOSet`
- `EKPO -> EKPOSet`
- `EKET -> eketSet`
- `LIEF -> Data`
- `WG -> Data2`

Vorbefuellte Joins:

- `EKKO.Ebeln = EKPO.Ebeln`
- `EKPO.Ebeln,Ebelp = EKET.Ebeln,Ebelp`
- `EKKO.Lifnr = LIEF.Lifnr`
- `EKPO.Matkl = WG.Matkl`

Die Seite verwendet dieselben Grundtabellen wie die Finance-/Standorte-Quellenpflege: `Sites`, `SapSourceDefinitions`, `SapJoinDefinitions`, `SapFieldMappings`.

## SAP/OData Live-Stand 2026-06-05

Der SAP-Test hat bestaetigt, dass die Einkaufstabellen Daten enthalten:

- `EKKO` ab `01.01.2026`: 2'748 Koepfe.
- `EKPO` gesamt: 233'920 Positionen.
- `EKET` gesamt: 242'571 Einteilungen.
- Join `EKKO -> EKPO` ab `01.01.2026`: 3'464 Zeilen.
- Join `EKKO -> EKET` ab `01.01.2026`: 3'458 Zeilen.

Nach Aktivierung der angepassten SAP-Methoden liefern die OData-Services:

- `EKPOSet?$top=5`: HTTP 200 mit Daten.
- `eketSet?$top=5`: HTTP 200 mit Daten.
- `EKPOSet?$filter=Ebeln eq '45148366'`: 1 Zeile.
- `eketSet?$filter=Ebeln eq '45148366'`: 1 Zeile.

Wichtig: Die OData-Property heisst `Ebeln`. Ein Filter mit `EBELN` liefert HTTP 400.

## Full Load / Delta Stand 2026-06-05

Der erste vollstaendige SAP-Load wurde am 2026-06-05 ausgefuehrt.

Geladene Cache-Zeilen:

- `PurchasingEkkoCache`: 172'874 EKKO-Koepfe.
- `PurchasingEkpoCache`: 233'921 EKPO-Positionen.
- `PurchasingEketCache`: 242'572 EKET-Einteilungen.

Technische Logik:

- SAP liefert pro OData-Seite maximal 1'000 Zeilen.
- Der Loader liest deshalb mit `$top=1000`, `$skip` und stabiler Sortierung:
  - `EKKOSet`: `$orderby=Ebeln`.
  - `EKPOSet`: `$orderby=Ebeln,Ebelp`.
  - `eketSet`: `$orderby=Ebeln,Ebelp,Etenr`.
- Nicht vorhandene OData-Felder wurden entfernt:
  - `EKKOSet.Bsart` existiert in diesem Service nicht.
  - `EKPOSet.Meins` existiert in diesem Service nicht.
- Nach dem Full Load kann `Delta aktualisieren` genutzt werden. Delta liest geaenderte EKKO-Belege ab `Aedat` und laedt die zugehoerigen EKPO/EKET-Zeilen je Beleg nach.

## Live-Kennzahlen im Dashboard

Die Seite `/einkauf` zeigt nun echte Werte aus dem SAP-Cache:

- `Spend total`: Summe `EKPOSet.Netwr` aus dem Cache, begrenzt auf den gewaehlten Zeitraum.
- `Offene Bestellungen`: Anzahl EKKO-Belege im gewaehlten Zeitraum.
- `Kontrakte`: offener Restwert aus `EKET.Menge - EKET.Wemng` bewertet mit EKPO-Netto-Stueckwert.
- `Offener Bestellwert`: berechnet aus EKET-Offenmenge und EKPO-Netto-Stueckwert.
- `Offene Menge`: Summe offener EKET-Mengen.
- Top-Lieferant, Top-Warengruppe und Top-Artikel werden aus EKPO gruppiert.
- Top-Artikel zeigt nun Artikel, Lieferant und Bestellmonat, damit ein Wert wie `C42698: CHF 1` fachlich nachvollziehbar ist.
- Die Verpflichtungs-/Kontraktseite zeigt Top-Restverpflichtungen nach Lieferant, Artikel und Faelligkeitsmonat, nicht nur den Monatsverlauf.
- Offene Verpflichtungen werden nicht mehr primaer als reine Vergangenheits-Zeitreihe interpretiert; fuer Einkauf ist die Zukunfts-/Faelligkeitssicht nach Lieferant und Artikel fachlich aussagekraeftiger.
- Spend-, Offenwert- und Kontrakt-Diagramme verwenden Cache-Gruppierungen, sofern der Cache gefuellt ist.
- Ist der Cache leer oder nicht erreichbar, faellt das Dashboard auf eine begrenzte SAP-Live-Probe zurueck.
- Der Standardzeitraum ist seit 2026-06-18 auf 2020 bis heute ausgerichtet. Die Datumsabgrenzung erfolgt im Dashboard ueber `Von Monat` und `Bis Monat`.

## PowerBI-Abgleich

Das Einkaufsdashboard wurde gegen die sichtbaren Auswertungen aus `x.pbix` abgeglichen:

- `Besch.Volumen CHF/Lieferant`: `Sum(EKPOSet.Netwr CHF)` nach Jahr, Lieferant, Warengruppe und Artikel.
- `Eink.Vol. CHF / Lieferant Kuchen`: `Sum(EKPOSet.Netwr CHF)` nach Lieferant.
- `Balken Vol./Lief/WG`: `Sum(EKPOSet.Netwr CHF)` nach Jahr und Lieferant.
- `Diagramm Vol./WG`: `Sum(EKPOSet.Netwr CHF)` nach Jahr und Warengruppe.
- `Eink.Vol. CHF / Region`: `Sum(EKPOSet.Netwr CHF)` nach Region.
- `Preisentwicklung CHF`: `Min(EKPOSet.Netwr CHF/Stk)` nach Artikel und Jahr.
- `Matrix Vol./WG`: `Sum(EKPOSet.Netwr CHF)` nach Warengruppe, Lieferant und Artikel.

Umgesetzt ist die gleiche Kernaggregation:

- Spend und Volumen verwenden `SUM(EKPO.Netwr)` mit Zeitraumfilter auf `EKKO.Bedat`.
- Preisentwicklung verwendet `MIN(EKPO.Netwr / EKPO.Menge)` je Artikel und Jahr mit Zeitraumfilter auf `EKKO.Bedat`.
- Offene Werte verwenden `MAX(EKET.Menge - EKET.Wemng, 0) * (EKPO.Netwr / EKPO.Menge)`.

Noch nicht final 1:1 ist die Namensauflösung:

- PowerBI nutzt fuer Lieferanten- und Warengruppennamen `Data.Name`, `Data.Lieferant`, `Data (2).Warengruppe` und `Data (2).WG komplett`.
- Der aktuelle SAP-OData-Service liefert produktiv `EKKOSet`, `EKPOSet` und `eketSet`; die Cache-Tabellen sind seit 2026-06-18 um optionale Felder fuer `SupplierName` und `Mstae` erweitert.
- Tests auf `Data`, `Data2`, `DataSet` und `Data2Set` liefern aktuell `404 Resource not found`.
- Bis diese Mapping-Quelle angebunden ist, verwendet das Dashboard vorhandene Lieferantennamen aus Payload bzw. Cache. Fehlt der Name, bleibt die Lieferantennummer sichtbar; es werden keine erfundenen Lieferantenlabels verwendet.

## Nachtrag 2026-06-18 Excel-Matrix und Einkaufsfilter

Umgesetzt:

- Neue Matrix `Kaskadierung Lieferant / Jahr` in der Einkaufssicht.
- Jahresachse aus den tatsaechlichen Spend-Jahren, im Standard 2020 bis aktuelles Jahr.
- Lieferanten werden Top-down nach Gesamt-Spend sortiert.
- Aktuelles Jahr: Spend pro Lieferant als separate Analyse.
- Gebuchter/beschaffter Wert und offener Zulauf werden in der Uebersicht getrennt dargestellt.
- Standardfilter fuer `LOEKZ` und vorbereiteter Filter fuer `MARA-MSTAE`.
- Lieferantennamen werden aus dem echten Einkaufsdaten-Payload gelesen, sofern SAP/OData sie liefert.
- Schema-Maintenance ergaenzt fehlende Cache-Spalten automatisch:
  - `PurchasingEkkoCache.SupplierName`
  - `PurchasingEkpoCache.Mstae`

Validierung:

- Testlauf: `dotnet test TrafagSalesExporter.sln --verbosity minimal`
- Ergebnis: `101/101` Tests gruen.
- Commit: `4f45805 Improve purchasing dashboard matrix`.

Deploy:

- Publiziert am 2026-06-18 auf `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`.
- `app_offline.htm` wurde fuer den Publish gesetzt und danach entfernt.
- Produktive Datei: `BiDashboard.dll`, Zeitstempel `18.06.2026 09:29:11`.
- Servercheck: Port 443 erreichbar, `app_offline.htm` nicht mehr vorhanden.

## Nachtrag 2026-06-19 MARA-MSTAE Loeschkennzeichen

Ausgangslage:

- Das Loeschkennzeichen sollte fuer das Einkaufs-Cockpit ueber `MARA-MSTAE = 98` oder `99` ausgewertet werden.
- Frueher war `MARA-MSTAE` ueber OData nicht erreichbar (`Data/Data2/DataSet/Data2Set -> 404`); der Schalter `MARA-MSTAE raus` war daher wirkungslos.
- Neu: MARA ist ueber das OData-EntitySet `MARA001Set` verfuegbar (Felder `Matnr`, `Mstae`).

Umgesetzt:

- `PurchasingDataRefreshService` laedt `MARA001Set` (`Matnr,Mstae`) bei Full Load und Delta in eine Status-Map.
- Beim EKPO-Upsert wird `Mstae` ueber den normalisierten Join `EKPO.Matnr -> MARA.Matnr` aufgeloest und in `PurchasingEkpoCache.Mstae` geschrieben.
- Matnr-Normalisierung: Whitespace entfernen, `ToUpperInvariant`, fuehrende Nullen entfernen. Damit matcht SAP-18-stellig mit fuehrenden Nullen gegen lokale Nummern.
- Filterlogik in `PurchasingDashboardService.ActiveItemFilterSql`: `ExcludeDeletedItems` schliesst jetzt `EKPO.Loekz <> ''` ODER `Mstae in ('98','99')` aus.
- Der bisher separate, wirkungslose Schalter `ExcludeBlockedMaterials` wurde mit dem Loeschkennzeichen zusammengelegt und aus `PurchasingDashboardFilter`, Filter-SQL und Razor-UI entfernt.
- UI: eine Checkbox `Loeschkennzeichen raus (inkl. MARA-MSTAE 98/99)`; Statuszeile entsprechend angepasst.
- Datenquellen-Pflege ergaenzt um Quelle `MARA -> MARA001Set`, Join `EKPO.Matnr = MARA.Matnr` und Mapping `MaterialStatus -> MARA.Mstae` in `DatabaseSeedService` und `PurchasingDataSourcePageService`.

Wichtig:

- Die Quellen-Defaults werden nur fuer eine leere Quellenliste geseedet; die produktive DB behaelt ihre bestehenden Quellen. Der Filter funktioniert trotzdem, weil der Refresh-Service `MARA001Set` fest laedt.
- Damit `Mstae` real gefuellt ist, muss nach dem Deploy ein Einkauf-Full-Load oder Delta laufen.

Validierung:

- `dotnet test TrafagSalesExporter.sln --verbosity minimal`
- Ergebnis: `103/103` Tests gruen, inkl. neuem `PurchasingDashboardServiceTests` (Filter aktiv/inaktiv).

## Nachtrag 2026-07-02 Lieferantennamen aus LFA1

Ausgangslage:

- Der Spend-Reiter (und alle Einkauf-Tabs) zeigte nur Lieferantennummern (z.B. `66952`, `70369`), keine Namen.
- Grund: `PurchasingEkkoCache.SupplierName` wurde nie befuellt. `EKKOSet` liefert nur `Lifnr`, keinen Namen; der fruehere Versuch `FirstNonEmpty(SupplierName, Name1, Name)` aus der EKKO-Zeile lief immer leer.
- Es war keine Lieferantenstamm-Quelle (LFA1) angebunden. `SupplierLabelSql` faellt bei leerem Namen auf `Lifnr` zurueck, daher die Nummer.

Metadaten-Befund `ZPOWERBI_EINKAUF_SRV/$metadata`:

- EntitySet `LFA1Set` existiert und liefert Daten; Felder u.a. `Lifnr` und `Name1`.
- Verifiziert: `LFA1Set('66952')` -> `Name1 = BEPRO AG`.
- EKKO und LFA1 liefern `Lifnr` im selben Format (ohne fuehrende Nullen).
- Kein SAP-/Gateway-Change noetig; der Service liefert die Namen bereits.

Umgesetzt in `PurchasingDataRefreshService`:

- Neue `LoadSupplierNameMapAsync` liest `LFA1Set` (`Lifnr,Name1`) bei Full Load und Delta in eine Namens-Map (analog zur bestehenden MARA-Status-Map).
- `UpsertEkkoAsync` loest `SupplierName` ueber `ResolveSupplierName(map, Lifnr, fallback)` auf: LFA1-Name bevorzugt, Fallback auf einen etwaigen Zeilenwert (rueckwaertskompatibel).
- Neue `NormalizeLifnr` (Whitespace entfernen, `ToUpperInvariant`, fuehrende Nullen entfernen) sichert den Join `EKKO.Lifnr -> LFA1.Lifnr`.
- Die Full-Load-Statusmeldung zeigt zusaetzlich `LFA1-Namen=<Anzahl>`.

Wichtig:

- Keine Schema-Aenderung noetig; die Spalte `PurchasingEkkoCache.SupplierName` existierte bereits.
- Die Anzeige (`SupplierLabelSql`) wurde nicht angefasst; Namen erscheinen automatisch, sobald `SupplierName` gefuellt ist. Das gilt fuer alle Einkauf-Tabs.
- Damit die Namen real erscheinen, muss nach dem Deploy einmal ein Einkauf-Full-Load laufen (`Einkauf > Ideen > Einkauf-Datenservice`).

Nebenbefund:

- Der Service liefert auch `mbew` (MBEW-STPRS) und `KNA1`. `mbew` ist die noch fehlende Standardkosten-Quelle fuer die offene Gruppenmarge.

Validierung:

- `dotnet test TrafagSalesExporter.sln --verbosity minimal`
- Ergebnis: `130/130` Tests gruen.

Deploy:

- Publiziert am 2026-07-02 auf `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`.
- `app_offline.htm` gesetzt und danach entfernt.
- Produktive Datei: `BiDashboard.dll`, Zeitstempel `02.07.2026 09:24:51`, Laenge `2'748'928`.
- Servercheck: Port 443 erreichbar.
- Commit: `d5f329b Resolve purchasing supplier names from LFA1`.

**Nachtrag 2026-07-17: Fix erst jetzt produktiv wirksam geworden.** Der Full Load direkt nach
diesem Deploy (02.07., siehe `PurchasingSyncState` Id 4/5) scheiterte am `MARA001Set`-404
(SAP hatte das MARA-Set umgebaut, siehe Nachtrag 2026-07-17 im Hauptdokument) — der Lauf brach
ab, BEVOR er ueberhaupt LFA1 laden konnte. Zwischen 02.07. und 17.07. blieb dadurch der Stand
vom letzten erfolgreichen Load (07.06., vor diesem Fix) aktiv: `SupplierName` war produktiv
0/172'874 gefuellt, die Spend-Matrix zeigte nur Lieferantennummern. Nach dem `maracalcSet`-Fix
lief der Full Load am 17.07. erfolgreich durch: `SupplierName` jetzt 172'898/172'914 (99.99 %)
gefuellt, verifiziert u.a. `66952 -> BEPRO AG`, `70369 -> CPT Praezisionstechnik GmbH`,
`66715 -> GFS`, `65058 -> HEITZ GMBH`. Der urspruengliche Code-Fix war also die ganze Zeit
korrekt, konnte aber wegen des unabhaengigen SAP-Umbaus nie zum Zug kommen.

## Nachtrag 2026-07-08 Review Einkauf mit Power BI

Kontext: Ingo und ein Kollege aus dem Einkauf haben das neu gebaute Einkaufsdashboard gemeinsam gegen Power BI/SAP-Erwartungen geprueft. Ziel war Zugriff, Navigation, Inhalte, Zahlen und fehlende Auswertungen abzugleichen.

Zugriff und Navigation:

- Der Kollege konnte den Navigationspunkt `Einkauf` anfangs nicht zuverlaessig aufklappen; nach mehrmaligem Versuch ging es. Netz-/Screen-Sharing-Qualitaet war zeitweise schlecht.
- Struktur wurde erklaert: `Einkauf > Dashboard`, `Spend`, `Offene Bestellungen`, `Lieferanten`, `Kontrakte/Verpflichtungen`.
- Anfangs wirkte es so, als ob in den Registern immer dasselbe angezeigt wird; Ursache: die Kopfdaten/KPI-Karten sind gleich, der Aufriss unten unterscheidet sich je Register.
- Datumsfilter war zunaechst nicht gesetzt bzw. nicht sauber abgegrenzt. Nach Live-Anpassung aenderten sich die Zahlen deutlich; die Auswertung muss fuer Abnahmen immer mit explizitem Zeitraum gelesen werden.

Beobachtete Werte:

- `Offene Bestellungen` bzw. offene nicht geloeste Positionen: offener Wert ca. `18 Mio.`; im Review von beiden bestaetigt. Dieser Wert ist ein Abgleichswert fuer die naechste SAP-Pruefung.

Offene fachliche Klaerungen:

- `Offene Verpflichtungen` / Kontrakte: aktuell ist fachlich zu klaeren, ob Mengen-Kontrakte, offene Bestellungen oder nur offene Kontrakte einfliessen sollen.
- Gewuenschte Trennung: Bei offenen Bestellungen nur Bestellungen; bei Kontrakten nur offene Kontrakte. Keine Vermischung der Logiken.
- Zentrale Klaerung durch Ingo: zugrundeliegende SAP-Tabelle und Belegart/Quelle fuer Kontrakte (EKPO vs. Kontrakt-Beleg, ggf. `ECCO`/passende Einkaufs-Transaktion). Ingo konnte das im Termin nicht aus dem Stegreif bestaetigen.
- Der technische Stand nach dem Formel-Review trennt offene Bestellwerte und Kontrakt-Restwerte ueber `EKKO.Konnr`, aber die fachliche SAP-Definition muss mit Einkauf/SAP noch bestaetigt und gegen Sollwerte geprueft werden.

Bekannter Bug / produktiver Pruefpunkt:

- Im Lieferanten-Register wird weiterhin eine Zahl statt Lieferantenname gesehen. Technisch wurde die LFA1-Namensaufloesung bereits implementiert; wenn produktiv noch Nummern sichtbar sind, ist wahrscheinlich ein Full Load/Delta mit LFA1-Namensbefuellung oder ein weiterer Mapping-/Band-Fix noetig.
- Ingo arbeitet weiter am Aufriss/Band fuer die Lieferantendimension.

Lieferanten-Performance:

- Performance Score ist vorhanden.
- Offen ist, ob der Einkauf diese Kennzahl tatsaechlich braucht; Kollege prueft dies im Kontext eines Memos zur Lieferantenbewertung.

Datenanbindung / Aktualisierung:

- Analogie QM: Fuer Florian Waechters Power-BI-Dashboard wurden Daten aus SAP-QM per automatisiertem CSV-Export bereitgestellt.
- Ingo bietet fuer Einkauf denselben pragmatischen Weg an: passende Einkaufs-Transaktion nennen, automatisierter Export, taegliche Aktualisierung des Dashboards.
- Voraussetzung: Einkauf benennt die fachlich richtige Transaktion/Quelle und die Soll-Spalten.

Naechste Schritte:

- Einkauf/Kollege: Soll-Daten und erwartete Zahlen fuer Gegenpruefung definieren; fehlende benoetigte Auswertungen auflisten.
- Ingo: Zahlen gegen SAP verifizieren, Review-Inputs einarbeiten, Lieferanten-Anzeige-Bug klaeren/fixen, Kontrakt-/Bestellungslogik fachlich und technisch abgrenzen.
- Abnahme: 18-Mio.-Offenwert, Lieferantenname statt Nummer, Zeitraumfilter und getrennte Bestell-/Kontraktlogik als konkrete Pruefpunkte verwenden.
## Deploy 2026-07-10

Alle Einkaufs-Aenderungen der Sessions 2026-07-09/10 (Beleg-Mix-Trennung, Elikz, neue Felder,
Marco-Review-Korrekturen) wurden deployed. Commit `335907c`, `157/157` Tests gruen, produktive
`BiDashboard.dll` `10.07.2026 14:17:01` (`2'782'208`), DB unveraendert, Port 443 erreichbar.
RISIKO/NACHSORGE: Kein Einkauf-Full-/Delta-Load gegen travp762, solange `Bstyp`/`Bsart`/`Elikz`
dort nicht im OData-Modell sind (sonst schlaegt der Loader-`$select` fehl / leert den Cache).
Siehe `docs/rag/DEPLOYMENT.md` und `docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md` (A0).

## Nachtrag 2026-07-10 Review-Mail Marco und Sofort-Korrekturen

Marco (Einkaufs-Koordinator) hat das produktive Cockpit durchgesehen; vollstaendiges Mapping in
`docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md`. Sofort umgesetzt (157/157 Tests gruen,
kein Deploy):

- **Verpflichtungen Stand heute:** Offene Positionen sind jetzt komplett zeitraumunabhaengig
  (Von-Untergrenze entfernt); die Kachel `Verpflichtungen` zeigt den offenen
  Bestell-/Abrufwert Stand heute (`OpenValueSample`) statt des Konnr-Restwerts im Zeitraum.
- **Loeschkennzeichen-Split:** MARA-MSTAE 98/99 filtert den historischen Spend nicht mehr
  (heutiger Status vs. 2023er Einkauf); Storno (`Loekz`) bleibt im Spend draussen. Offene
  Werte/Zulauf schliessen weiterhin Loekz UND MSTAE 98/99 aus. Getrennte Filter
  `SpendItemFilterSql` / `ActiveItemFilterSql`, Checkbox-Label praezisiert.
- **Kachel-Beschriebe:** EKPO = "Anzahl Bestellpositionen im Zeitraum", EKET = "Anzahl
  Termineinteilungen im Zeitraum".
- **Lieferanten-Register:** Chart folgt jetzt dem gewaehlten Zeitraum (vorher hart aktuelles
  Jahr — Ursache fuer "Zeitraum wirkt nicht").

Geplant aus dem Review (siehe Mapping-Doku, Abschnitt C): Termintreue-Kachel via EKBE
(Bewertungsformel von Marco noetig), Spend-Drilldown-Selektoren inkl. Disponenten-Produktgruppe
(MARC), "Lieferdatum bis"-Filter fuer offene Bestellungen, echte Mengenkontrakte
(`Bstyp='K'`, `Kdate` fehlt noch im P-Modell), Lieferanten-Factsheet und -Vergleich.

## Nachtrag 2026-07-09 Ergebnisse Analyse-Report (Z_PURCHASING_ANALYSE)

Ingo hat `sap_purchasing_analyse_report.abap` (T76/100, Einkauf ab 2020) laufen lassen. Die
Datenprofilierung bestaetigt mehrere Review-Punkte mit echten Zahlen und deckt einen neuen,
fachlich wichtigen Befund auf (Beleg-Mix).

**K1 Waehrung — bestaetigt kritisch und Richtung verifiziert:**

- Belegverteilung: EUR 30'746 (65%), CHF 14'130 (30%), USD 2'277, GBP 25, leer 47. Die Mehrheit
  ist NICHT CHF; das fruehere ungeprueft-CHF-Summieren war real falsch, nicht nur theoretisch.
- BUKRS quasi nur `1100` (CH, Hauswaehrung CHF), wenige `1200`.
- WKURS fuer EUR = `1.10000` (positiv). Damit ist die implementierte Regel
  `WKURS > 0 => multiplizieren` korrekt (1 EUR = 1.10 CHF). K1-Code gilt als validiert; die
  EUR-Belege werden nach CHF hochbewertet.
- Nuance: WKURS ist der Bestellkurs zum Belegdatum (historisch), nicht der Tages-/Stichtagskurs.
  Fuer Spend-Bewertung zum Bestellwert fachlich richtig; beim Power-BI-Abgleich beachten, falls
  dort mit Stichtags- oder Monatskursen gerechnet wurde.

**Neuer Befund — Beleg-Mix (Bestellung/Anfrage/Kontrakt/Umlagerung vermischt):**

- BSTYP: `F`=41'342 (Bestellung), `A`=3'117 (Anfrage), `K`=2'766 (Kontrakt).
- BSART: `NB`=41'326, `AN`=3'117 (Anfrage), `MK`=2'766 (Mengenkontrakt), `UB`=16 (Umlagerung).
- Spend/offene Werte mischen aktuell alle diese Belegarten. Anfragen (A/AN) sind keine echten
  Bestellungen; Kontrakte (K/MK) und Umlagerungen (UB) gehoeren nicht in den Bestell-Spend.
  Das ist genau Marcos Forderung nach Trennung. **Erfordert Persistenz von `Bstyp`/`Bsart`.**
- `Konnr` gesetzt bei 19'514/47'225 (41%) -> Kontraktabruf-Abgrenzung (K4) ist substanziell.

**M7 Elikz — Impact bestaetigt gross:**

- Offene Einteilungen: 14'840 mit `Elikz=''`, 2'672 mit `Elikz='X'` (endgeliefert).
- Offener Wert gesamt 18'422'518 (Belegwaehrung, roh) = deckt Marcos "~18 Mio" aus dem Review.
  Davon ueberfaellig 17'386'311; davon auf `Elikz='X'` **7'463'886** (40% -> zaehlt faelschlich
  als offen). Nach M7 (und K1) verschiebt sich der Offenwert deutlich -> Abnahme-Sollwert mit
  Marco neu baselinen.

**Sofort nutzbar (Daten vorhanden und sauber):**

- Region: `LAND1` 730/730 gefuellt (27 Laender: CH 495, DE 152, IT 16, AT 12, CN 8, US 8...).
  `REGIO` nur 24 -> Beschaffungsregion ueber Land, nicht Regio.
- Warengruppen: nur 20 Codes, alle mit Text (T023T). Vollstaendig erfasst (Seed moeglich).
- Disponenten: 3'682 Materialien mit `Dispo`; Gruppen u.a. `001 rot/Einkauf` (1568),
  `003 mso/Einkauf` (1281), `004 Betriebsmat` (542).
- MBEW: `STPRS` 3'725/3'727 gefuellt, `SALK3`>0 bei 2'762 -> Standardkosten + Bestand verfuegbar.
- EKBE: 97'193 WE-Zeilen (BEWTP=E), WE-BUDAT vs. Plan-EINDT vergleichbar -> Termintreue rechenbar.

**Wichtigste offene SAP-Aktion (Modell-Erweiterung, blockiert die korrekten Zahlen):**

- Der OData-Service muss `EKKO-BSTYP`, `EKKO-BSART`, `EKPO-ELIKZ` (und moeglichst `EKPO-KTMNG`)
  als Properties fuehren. `Waers`/`Wkurs`/`Konnr` sind bereits im Modell (kein 400). `Bsart`/`Meins`
  warfen frueher 400 -> Modell (MPC) muss ergaenzt werden. Ohne diese Felder lassen sich
  Anfragen/Kontrakte/Umlagerungen nicht ausschliessen (Beleg-Mix) und Elikz=X nicht abziehen.
- Zusaetzlich einmal `ZPOWERBI_EINKAUF_SRV/$metadata` liefern, um die exakten Property-Namen der
  bereits verfuegbaren Sets (MARC/MBEW/EKBE/LFA1/QM) fuer die Loader-`$select` zu kennen.

## Nachtrag 2026-07-09 Beleg-Mix-Trennung + Elikz + neue Felder persistiert

Nach dem Analyse-Report wurden EKKO um `Bstyp`/`Bsart` und EKPO um `Elikz` (und `Ktmng`) im
OData-Modell auf P ergaenzt. Der Code zieht diese Felder nun durch und wertet sie aus.

Umgesetzt:

- **Persistenz:** Schema + Schema-Maintenance (mit RawJson-Backfill) fuer
  `PurchasingEkkoCache.Bstyp`/`Bsart` und `PurchasingEkpoCache.Elikz`/`Ktmng`. Loader-`$select`
  erweitert (`EKKOSet` + `Bstyp,Bsart`; `EKPOSet` + `Elikz`; `Ktmng` war bereits im Select, wird
  jetzt geschrieben) und in beiden Upserts (Full + Delta) gefuellt.
- **Beleg-Mix-Trennung (Marcos Forderung):** Neuer Filter `OrdersOnly` (Default an). Spend/offene
  KPIs zaehlen nur echte Bestellungen (`Bstyp='F'` ohne `Bsart='UB'`); Anfragen (A/AN), Kontrakte
  (K/MK) und Umlagerungen (UB) fallen raus. Zentral in `activeItemFilter` eingehaengt (wirkt auf
  alle Spend-/Offen-Queries). Leerer `Bstyp` (Bestandsdaten vor Full Load) wird bewusst
  eingeschlossen -> keine Null-Werte beim Rollout.
- **M7 Elikz:** Neuer Filter `ExcludeEndDelivered` (Default an). Endgelieferte Positionen
  (`Elikz='X'`) zaehlen nicht mehr als offen; zentral in `eketOpenPeriod` eingehaengt (wirkt auf
  offenen Wert/Menge, Ueberfaellig, Zulauf, Kontrakt-Restwert, Liefertermin-Risiko).

Validierung:

- `dotnet test TrafagSalesExporter.sln --verbosity minimal` -> `155/155` gruen, inkl. neuer Tests:
  Beleg-Mix (nur F/NB zaehlt; A/K/UB raus), `OrdersOnly=false` (alles zaehlt), Elikz-Ausschluss.
- Kein Deploy. Offen bei Ingo: OData-Auth/Test auf travp762 (Basic-Auth gab 401), danach
  URL-Wechsel travt762->travp762 und ein Einkauf-Full-Load, damit `Bstyp/Bsart/Elikz/Ktmng`
  real gefuellt sind (Backfill deckt nur, was schon im RawJson liegt).
- Offen fachlich: echte "offene Kontrakte" (Bstyp='K' mit Restzielmenge) vs. jetzige
  Konnr-Abruf-Naeherung; Abrufquote ueber `Ktmng` (Feld jetzt vorhanden). UI-Schalter fuer
  `OrdersOnly`/`ExcludeEndDelivered` noch nicht gebaut (Default an; spaeter fuer Transparenz).

## Nachtrag 2026-07-09 Umsetzung Phase 1 (Ueberfaellig, Preisentwicklung je Artikel, Kontrakt-Label)

Grundlage: `docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md`. Umgesetzt wurde der
code-seitig ohne externe Inputs machbare Teil von Phase 1; alles Uebrige (Referenzlisten,
SAP-Metadaten-Checks, neue SAP-Objekte) ist in
`docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md` als Vorbereitungsauftrag beschrieben.

- **Phase 1.1 Ueberfaellige Lieferpositionen:** Neue KPIs `OverdueValueSample`,
  `OverdueQuantitySample`, `OverduePositionCount` und Drilldown `OverduePositionRows` im
  Cache-Pfad (EKET-Einteilung mit `date(Eindt) < heute` und offener Menge > 0, gleiche
  Join-/Loeschkennzeichen-Struktur wie der offene Wert). Sichtbar in `Offene Bestellungen`
  (Ueberfaelliger Wert + Anzahl) und `Ideen > Liefertermin-Risiko`.
- **Phase 1.2 Preisentwicklung je Artikel:** `ExecuteArticlePriceTrendRowsAsync` liefert die
  Top-8-Artikel nach Spend mit mengengewichtetem Ø-Stueckpreis (CHF) je Jahr und YoY-Trend
  (Vergleich der beiden letzten Jahre mit Daten; Severity High = > +2%, Low = < -2%). Die
  Idee-Seite `Preisabweichung` zeigt jetzt diesen Artikel-Trend statt des fachlich schwachen
  Min-Stueckpreis-Rankings. Der mengengewichtete Jahres-Index-Chart bleibt.
- **Phase 1.5 Kontrakt-KPI:** `Offene Verpflichtungen` und die Restverpflichtungs-Zeile sind als
  Naeherung gekennzeichnet ("nur Abrufe mit EKKO.Konnr"), inkl. Hinweis, dass echte
  Mengenkontrakte mit Ablaufdatum noch Kontraktbelege aus SAP brauchen.

Validierung:

- `dotnet test TrafagSalesExporter.sln --verbosity minimal`
- Ergebnis: `152/152` Tests gruen, inkl. neuer Tests fuer Ueberfaellig-Abgrenzung und
  Artikel-Preistrend (YoY).
- Kein Deploy (Deploy-Entscheid inkl. Phase-0-Full-Load offen, siehe Vorbereitungs-MD).

Noch offen / vorzubereiten (siehe `docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md`):

- Phase 1.3/1.4: Warengruppen-Text-CSV (T023T) und Disponenten-CSV (ZC23) von Ingo.
- Phase 2: OData-Proben LFA1-Adresse/Elikz/MBEW/Kontraktbelege.
- Phase 3: EKBE (Termintreue), QM-Export (Reklamation), RESB/MARC (Lager).

## Nachtrag 2026-07-09 Anforderungs-Mail Marco / Umsetzungsplan

Marco (Einkauf) hat nach dem Review vom 2026-07-08 die Anforderungen der Hauptanspruchsgruppen
schriftlich umrissen (Echtzeit-Uebersicht Einkaufstransaktionen, 7 Aufrisse, KPIs zu
Beschaffungstransaktionen/Lager/Lieferantenperformance). Die Anforderungen wurden gegen den
Code- und Datenstand gemappt und in einen Phasenplan uebersetzt:

- Arbeitsauftrag: `docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md`
- Kernaussage: Phase 0 = Deploy Korrektur-Stand + Full Load + Soll-Abgleich; Phase 1 = Ausbau
  mit vorhandenen Daten (Ueberfaellige Positionen, Preisentwicklung je Artikel, Warengruppen-
  und Disponenten-Referenzlisten); Phase 2/3 = gezielte SAP-Erweiterungen (LFA1-Adresse, Elikz,
  MBEW, Kontraktbelege, MARC, EKBE, RESB, QM).

## Ideen und Kennzahlen-Katalog

Der Ideenbereich wurde fuer den Einkauf erweitert:

- Lieferantenrisiko.
- Preisentwicklung CHF.
- Maverick Buying.
- Rahmenvertragsnutzung.
- Working Capital.
- Datenqualitaet.
- Liefertermin-Risiko.
- Spend-Konzentration.
- Savings Tracker.
- Bestellrhythmus.

Stand nach Ausbau: Unter `/einkauf/ideen` ist jede Idee als aufklappbarer Baustein beschrieben. Pro Idee sind Ziel, Datenbasis, Kennzahlen, Berechnungslogik, Visualisierung und naechster Umsetzungsschritt hinterlegt.

Der separate Kennzahlen-Katalog enthaelt nun konkrete Ausbau-KPIs mit Dimension und Datenbasis, darunter:

- Spend CHF.
- Top-10-Lieferantenanteil.
- Risiko-Score 0-100.
- Min. Netto-Stueckpreis nach Artikel und Jahr.
- Preisentwicklung analog PowerBI.
- Anteil ausserhalb Vertrag.
- Abrufquote.
- Ueberfaelliger offener Wert.
- Offene Menge faellig in 30 Tagen.
- Cash Forecast.
- Kleinstbestellungen.
- Realisierte Einsparung.
- Mapping-Abdeckung.
- Fehlende Warengruppe / fehlender Artikeltext.

## 3D Simulation

Das Einkaufsdashboard hat eine eigene 3D-Simulation fuer wichtige Einkaufsindikatoren:

- Spend CHF.
- Offener Bestellwert.
- Offene Menge.
- Kontrakt-Restwert.
- Lieferantenperformance.

Die Simulation nutzt feste Canvas-Groessen, sichtbare Achsen, waehlbare Diagrammarten, Labelgroesse und einen Szenario-Slider fuer Preis-/Wechselkurswirkung.

## Naechster Schritt fuer Live-Daten

Die technische Vollbasis ist geladen. Fuer fachlich finale Management-Sichten muessen noch diese Abgrenzungen abgestimmt werden:

- Mapping-Quelle fuer Lieferantennamen, Region und Warengruppentexte final bereitstellen oder als eigene Cache-Tabelle laden. Falls `SupplierName` und `Mstae` nicht im bestehenden OData-Payload kommen, muessen Data/LFA1/MARA-Quelle und EntitySet-Namen fachlich/technisch geklaert werden.
- PowerBI-Zielwerte mit Marco/Finanzen anhand eines konkreten Monats und Lieferanten gegenpruefen.
- Kontrakte und offene Verpflichtungen, inkl. fachlicher Abgrenzung von normalen Bestellungen und Umlagerungen.
- Lieferantenbewertung / Performance, falls im SAP-System als OData- oder HANA-Quelle verfuegbar.

Der Delta-/Refresh-Prozess ist technisch vorbereitet und im Dashboard unter `Einkauf > Ideen > Einkauf-Datenservice` bedienbar.

## Server-Restore und Full Load 2026-06-08

Beim Publish wurde frueher die Runtime-Datei `trafag_exporter.db` mitpubliziert. Dadurch war die Server-DB zeitweise wieder leer. Das ist im Projektfile korrigiert: `trafag_exporter.db`, `trafag_exporter.db-wal` und `trafag_exporter.db-shm` werden nicht mehr in das Publish-Paket kopiert.

Wiederherstellung am Server:

- Server-DB zuerst aus der lokalen Haupt-DB wiederhergestellt, damit Finance-Daten, Navigation und SAP-Credentials wieder vorhanden sind.
- Backup vor Restore:
  - `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\trafag_exporter.db.before-restore-20260605-144709.bak`
- Danach Einkauf-Full-Load nicht direkt ueber die UNC-Server-DB ausgefuehrt, sondern lokal gegen eine DB-Kopie:
  - Arbeitsordner: `C:\TMP\purchasing-fullload-20260607-205623`
  - Grund: langer SAP-Abruf plus SQLite ueber UNC ist fragil.
- Lokaler Full Load erfolgreich abgeschlossen:
  - `PurchasingEkkoCache`: 172'874
  - `PurchasingEkpoCache`: 233'921
  - `PurchasingEketCache`: 242'572
- Die fertig geladene DB wurde anschliessend auf den Server kopiert.
- Backup vor dem Zurueckkopieren der Full-Load-DB:
  - `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\trafag_exporter.db.before-purchasing-fullload-20260608-061149.bak`

Wichtiger Fix nach dem Kopieren:

- Auf dem Server lagen noch alte SQLite-Sidecar-Dateien neben der neuen Haupt-DB:
  - `trafag_exporter.db-wal`
  - `trafag_exporter.db-shm`
- Diese passten nicht mehr zur neuen Hauptdatei und verursachten beim App-Start `SQLite Error 11: database disk image is malformed`.
- Beide Sidecar-Dateien wurden gesichert und entfernt:
  - `trafag_exporter.db-wal.before-cleanup-20260608-065012.bak`
  - `trafag_exporter.db-shm.before-cleanup-20260608-065012.bak`

Verifizierter Serverstand nach Cleanup:

- HTTP-Check `https://trch-webapp-bidashboard.trafagch.local/BiDashboard/`: Status 200.
- Server-DB:
  - `SourceSystemDefinitions`: 5
  - `Sites`: 9
  - `SapSourceDefinitions`: 8
  - `SapJoinDefinitions`: 5
  - `SapFieldMappings`: 47
  - `NavigationMenuItems`: 47
  - `CentralSalesRecords`: 75'089
  - `PurchasingEkkoCache`: 172'874
  - `PurchasingEkpoCache`: 233'921
  - `PurchasingEketCache`: 242'572
  - SAP-Credentials vorhanden.
  - Neueste EKKO-Bestelldaten: `2026-06-05`.
  - Neueste EKET-Einteilung: `2027-04-20`.

Empfehlung fuer kuenftige grosse Einkauf-Ladevorgaenge:

- Full Load immer lokal gegen eine Kopie der produktiven DB ausfuehren.
- Erst nach erfolgreichem Abschluss die fertige DB auf den Server kopieren.
- Beim Ersetzen der SQLite-Hauptdatei immer `trafag_exporter.db-wal` und `trafag_exporter.db-shm` passend mitsichern/entfernen.
- Danach HTTP-Start und Cache-Counts pruefen.

## Geaenderte Programmstellen

- `Components/Pages/PurchasingDashboard.razor`
  - KPI-Karten, Detailtabellen und Diagramme lesen jetzt Live-Werte aus `PurchasingDashboardLiveState`.
  - Fallback-Simulation bleibt sichtbar, falls SAP/OData nicht antwortet.
  - Die alten Tabs wurden in routenbasierte Seiten unter `/einkauf/...` umgebaut.
  - Ideen und Kennzahlen-Katalog sind getrennte Seiten.
- `Services/DatabaseSeedService.cs`
  - Neue Einkaufs-Unterpunkte werden in `NavigationMenuItems` geseedet.
  - Admins koennen die Unterpunkte ueber die Menuestruktur ausblenden, sortieren oder umhaengen.
- `Services/IPurchasingDashboardService.cs`
  - Live-State um Spend, offene Menge, offenen Wert, Kontraktwert und Live-Diagrammzeilen erweitert.
  - Seit 2026-06-18: Live-State um Jahresachsen, Lieferant/Jahr-Matrix und Spend aktuelles Jahr je Lieferant erweitert.
- `Services/PurchasingDashboardService.cs`
  - Liest EKKO, EKPO und EKET aus dem Einkauf-Cache und nutzt SAP-Live nur als Fallback.
  - Berechnet Spend aus EKPO.
  - Berechnet offene Mengen/Werte aus EKET minus Wareneingangsmenge, bewertet mit EKPO-Netto-Stueckwert.
  - Erstellt Top-Gruppierungen fuer Lieferant, Warengruppe und Artikel.
  - Seit 2026-06-18: filtert geloeschte Positionen und optional Materialstatus, erzeugt die Lieferant/Jahr-Matrix und vermeidet kuenstliche Lieferanten-Platzhalter.
- `Services/PurchasingDataRefreshService.cs`
  - Fuehrt Full Load und Delta-Refresh fuer EKKO/EKPO/EKET aus.
  - Beruecksichtigt das SAP-Seitenlimit von 1'000 Zeilen.
  - Seit 2026-06-18: schreibt optionale Payload-Felder fuer Lieferantennamen und `Mstae`, falls SAP/OData sie liefert.
- `Services/DatabaseInitializationService.SchemaSql.cs`
  - Erstellt `PurchasingEkkoCache`, `PurchasingEkpoCache`, `PurchasingEketCache` und `PurchasingSyncState`.
  - Seit 2026-06-18: Schema kennt `SupplierName` in `PurchasingEkkoCache` und `Mstae` in `PurchasingEkpoCache`; bestehende Datenbanken werden ueber Schema-Maintenance ergaenzt.

## Nachtrag 2026-07-06 Formel-/Logik-Korrekturen (Review)

Grundlage: Formel-Review in `docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md`. Umgesetzte
Korrekturen (Prioritaet in Klammern):

- K1 (kritisch) Waehrungsbewertung nach CHF: `EKPO.Netwr` ist Belegwaehrung, wurde bisher 1:1 als
  CHF summiert. Neu werden `EKKO.Waers` und `EKKO.Wkurs` persistiert (Schema + Upsert + einmaliger
  Backfill aus `RawJson`) und alle Spend-/Stueckwert-/Preis-Queries bewerten ueber einen zentralen
  Ausdruck: CHF/leer unveraendert, Fremdwaehrung mit positivem Wkurs multipliziert, mit negativem
  Wkurs dividiert (SAP-Konvention indirekte Notierung). **Offen/zu verifizieren:** die WKURS-Richtung
  gegen echte Fremdwaehrungsbelege; solange alle Belege CHF sind, aendern sich die Zahlen nicht.
- K2 (kritisch) Delta veraltete offene Werte: `EKKO.Aedat` ist Anlage-, kein Aenderungsdatum;
  Wareneingaenge (nur `EKET.Wemng`) wurden nie nachgezogen. Das Delta laedt jetzt zusaetzlich alle
  Belege mit offener Menge aus dem Cache nach. Zugleich Batching (`$filter=Ebeln eq 'A' or ...`,
  20 Belege je Request) statt eines Requests je Beleg.
- K3 (kritisch) Zukunfts-Zulauf: Der Zeitraumfilter (`Bis Monat`) schnitt zukuenftige EKET-Termine
  ab; offener Wert/Menge und Liefertermin-Risiko zeigten nur den Rueckstand. Offene Positionen
  verwenden jetzt eine eigene Periode mit nur Untergrenze (`Von`), ohne Obergrenze auf heute.
  Damit fuellen sich auch die Risiko-Buckets `0-7 Tage` / `8-30 Tage` / `Spaeter`.
- K4 (hoch) Kontrakt-Restwert war eine 1:1-Kopie des offenen Bestellwerts. Neu: `EKKO.Konnr` wird
  persistiert und `ContractValueSample` zaehlt nur offene Positionen mit gesetztem `Konnr` (Abrufe
  zu Rahmenkontrakten). **Hinweis:** ohne Konnr-Daten ist der Wert 0 (fachlich korrekt: keine
  Kontrakte abgegrenzt); der offene Bestellwert bleibt separat sichtbar.
- K5 (hoch) KPI-Karte `Offene Bestellungen` zaehlte alle Bestellungen im Zeitraum -> umbenannt zu
  `Bestellungen im Zeitraum` (konsistent mit den uebrigen Anzeigen).
- K6 (hoch) Jahresachse war hart auf `<= 2026` codiert und haette am 1.1.2027 das aktuelle Jahr
  still verloren -> Obergrenze dynamisch (`max(heute, Bis-Jahr)`), Untergrenze 2020 bleibt.
- M8 (mittel) `Offene Menge` hatte keinen Positionsfilter und war inkonsistent zum offenen Wert ->
  gleiche Join-/Loeschkennzeichen-Struktur.
- M9 (mittel) Preisentwicklungs-Chart zeigte das Minimum ueber alle Artikel (praktisch immer ein
  Cent-Artikel) -> jetzt mengengewichteter Durchschnitts-Stueckpreis (CHF) je Jahr.
- M10 (klein) `GetDecimal`-Fallback auf `CurrentCulture` entfernt (SAP/OData ist invariant).

Nach Deploy noetig: einmal Einkauf-Full-Load laufen lassen, damit `Waers`/`Wkurs`/`Konnr` real
gefuellt sind (Backfill deckt Bestandsdaten aus `RawJson` bereits ab).

Noch offen (braucht SAP-Metadaten-Check, nicht ohne Live-Zugriff umsetzbar):

- M7 Endlieferungskennzeichen `EKPO.Elikz`: endgelieferte Positionen mit `Wemng < Menge` zaehlen
  weiter als offen. `Elikz` erst nach Pruefung in `$metadata`/`$top=1` in `$select` aufnehmen
  (analog dem frueheren 400-Fehler bei `Bsart`/`Meins`).
- K4-Zusatz: Belegart `EKKO.Bsart` (u.a. zur Abgrenzung von Umlagerungen) liefert der Service
  aktuell nicht; Feld bleibt leer, bis SAP es bereitstellt.
- PowerBI-Zielwerte weiterhin mit Marco/Finanzen an einem konkreten Monat + Lieferant gegenpruefen.

Validierung:

- `dotnet test TrafagSalesExporter.sln --verbosity minimal`
- Ergebnis: `139/139` Tests gruen, inkl. neuer Tests fuer CHF-Umrechnung, Zukunfts-Zulauf und
  Kontrakt-Abgrenzung ueber `Konnr`.
- Noch kein Deploy (Modellwechsel-Session); Deploy-Entscheid mit Ingo offen.

## Nachtrag 2026-07-17 Feedback-Runde Marco/Armin: Spend-Drilldown + MARA-Umbau-Befund

Feedback-Runde zum Purchasing-Dashboard (Marco/Armin). Kernwunsch: nicht nur Gesamtuebersicht
(Spend pro Lieferant ab 2020), sondern Aufriss/Drilldown ueber mehrere Stufen — konzeptuell wie
ein Pivot mit Auf-/Zuklappen. Marcos Leitplanke bestaetigt: **ein Punkt nach dem anderen fertig
machen** — zuerst der Reiter `Spend`, erst nach dessen Abnahme der naechste Reiter.

### Umgesetzt: Drilldown Lieferant -> Warengruppe im Spend-Reiter

- Die Matrix `Kaskadierung Lieferant / Jahr` (Reiter `Spend`) hat jetzt eine zweite Ebene:
  Lieferant aufklappen (Pfeil-Button) zeigt den Spend des Lieferanten je **Warengruppe** und
  Jahr; die Drilldown-Summen entsprechen exakt der Lieferantenzeile (Pivot-Eigenschaft, per
  Test abgesichert). Zeitraumfilter wirkt unveraendert auf beide Ebenen.
- Datenbasis: neue Aggregation `ExecuteSupplierGroupYearRowsAsync` in
  `PurchasingDashboardService` (`GROUP BY Supplier, MaterialGroup, Year`), Modell
  `PurchasingSpendGroupYearRow`, UI in `PurchasingSection.razor` (Toggle je Lieferant,
  eingerueckte Drill-Zeilen).
- **Warengruppen-Quelle nach Marcos Vorgabe:** massgeblich ist die AKTUELLE Warengruppe aus dem
  Materialstamm (`MARA-MATKL`), nicht der Vergangenheitswert aus dem Beleg (alte Belege tragen
  nur die Dummy-Warengruppe). Dafuer neue additive Cache-Spalte
  `PurchasingEkpoCache.MaraMatkl`; der Drilldown nutzt
  `COALESCE(MaraMatkl, Matkl, 'ohne Warengruppe')`. Seit dem verifizierten
  Full Load vom 24.07. ist `MaraMatkl` zu 80,7 % gefuellt; fuer den Rest bleibt
  der transparente Fallback auf die Beleg-Warengruppe.

### Erledigter Zwischenstand vom 17.07

Der damalige Umbau des SAP-MARA-EntitySets und die voruebergehend fehlenden
Felder `Mstae`/`Matkl` sind nicht mehr aktuell: Seit 23./24.07. liefert
`MARA001Set` beide Felder, der Loader verwendet wieder dieses Set und der Full
Load ist produktiv verifiziert. ABC/XYZ ist im separaten Spend-Aufriss
umgesetzt und seit dem Full Load mit echten Daten gefuellt. Der damalige
Fehler-/Blockertext wurde entfernt, damit er nicht mehr als aktueller Auftrag
gelesen wird.

### Validierung

- `dotnet test TrafagSalesExporter.sln --verbosity minimal`: `257/257` Tests gruen
  (2 neue Drilldown-Tests: Warengruppen-Aufriss inkl. MaraMatkl-Vorrang und
  Zeitraumfilter-Wirkung auf die Drill-Ebene).
- Noch nicht deployed. NACH Deploy: Einkauf Full Load laufen lassen (fuellt `Mstae` wieder aus
  `maracalcSet`; `MaraMatkl` bleibt leer bis zur SAP-Erweiterung).

## Nachtrag 2026-08-18 Materialtext (MAKTX) fuer den Spend-Drilldown

Auftrag von Ingo: Im Spend-Drilldown `Lieferant > Warengruppe > Material` soll neben der
Materialnummer der Materialtext stehen. Heute zeigt die Materialebene nur
`COALESCE(EKPO.Matnr, EKPO.Txz01, 'ohne Artikel')` — also die Nummer, ersatzweise den
Beleg-Kurztext, aber nie den aktuellen Materialstamm-Text.

**Wichtig zur Quelle:** `MARA` hat KEIN Textfeld. Der Materialtext liegt im SAP-Standard in
`MAKT` und ist **sprachabhaengig** (Schluessel `MATNR + SPRAS`). Die urspruengliche Formulierung
„MAKTX aus MARA" trifft also nicht die Tabelle, wohl aber die Absicht.

### Live-Messung 2026-08-18 (Report `docs/abap/Z_PURCHASING_MAKTX_ANALYSE.abap`, T76/100)

Der Gateway-Weg war blockiert (Basic-Auth lieferte auf travp762 UND travt762 `401`), deshalb
wurde direkt auf der Datenbank gemessen. Grundgesamtheit: alle Materialien aus `EKPO` mit
`EKKO.BEDAT >= 2020-01-01`.

| Messgroesse | Wert |
| --- | --- |
| Distinkte Materialien im Einkauf | `3'682` |
| Materialien mit mindestens einem `MAKT`-Satz | `3'682` (**100 %**) |
| Materialien ganz ohne Text | `0` |
| `MAKT`-Zeilen gesamt (alle Sprachen) | `6'390` |
| Sprachverteilung | `DE 3'681`, `EN 1'426`, `FR 1'275`, `IT 8` |
| Materialien mit Text in `DE` | `3'681` von `3'682` |
| **Mehrsprachig gepflegte Materialien** | **`1'425`** |

Stichprobe der beiden Materialien aus der BEPRO-AG-Zeile (Warengruppe `10.08.00`):

- `B64880` -> `PCBA HYBRID DENSITY 6.5...20mA 56KG/m3`
- `B64336` -> `PCBA NAT TR5 MODUL CURRENT STD COLDB Rei`

### Was daraus fuer die Umsetzung folgt

1. **Der Join MUSS auf `SPRAS` filtern.** `1'425` von `3'682` Materialien (rund 39 %) haben
   mehr als eine Sprachzeile. Ein Join nur ueber `MATNR` wuerde genau diese Materialien im
   Drilldown vervielfachen und damit die Spend-Summe verfaelschen — die Pivot-Eigenschaft
   „Drilldown-Summe gleich Lieferantenzeile" waere gebrochen. Das ist der kritische Punkt.
2. **`DE` ist die richtige Leitsprache**, mit `3'681` von `3'682` praktisch vollstaendig.
   Genau ein Material hat keinen deutschen Text; dafuer genuegt ein Fallback.
3. **Ein leerer Text ist der Ausnahmefall, nicht die Regel.** Trotzdem bleibt die Anzeige
   beim vorhandenen Muster: fehlt der Text, wird die Materialnummer allein gezeigt, nie ein
   erfundener Platzhalter.
4. **Vorbehalt Systemstand:** Gemessen wurde auf `T76/100` (Test). Der produktive Loader liest
   den Gateway auf `travp762`. Die Struktur (Tabelle, Sprachabhaengigkeit) ist
   systemunabhaengig, die Fuellgrade sind nach dem ersten produktiven Load gegenzupruefen.

### SAP-Quelle: `MAKTSet` (live geprueft 2026-08-18 an travp762)

Ingo hat die Antwort des produktiven Gateways geliefert. Damit ist nichts geraten:

- EntityType `ZPOWERBI_EINKAUF_SRV.MAKT`, Felder `Mandt`, `Matnr`, `Spras`, `Maktx`, `Maktg`.
  Verwendet werden `Matnr`, `Spras` und `Maktx`.
- Das Set existiert auf PROD (`travp762`) identisch wie auf TEST (`travt762`).
- **Es ignoriert `$top`/`$skip` und liefert immer den Vollbestand** — dasselbe Verhalten wie
  `MARA001Set`. Der Loader macht deshalb bewusst EINEN ungepagten Request; ein Paging-Lauf wuerde
  bei jedem „Blatt" den kompletten Bestand erneut holen.
- Es liefert **alle Sprachen**, filtert also serverseitig nicht. Die Sprachauswahl muss im Loader
  passieren.

### Umgesetzt 2026-08-18

- **Schema additiv:** neue Spalte `PurchasingEkpoCache.Maktx` (`CREATE`-Statement und
  Schema-Maintenance). Bestehende Datenbanken bekommen sie beim Start ergaenzt; sie bleibt leer,
  bis ein Full Load oder Delta gelaufen ist.
- **Loader:** neue `LoadMaterialTextMapAsync` liest `MAKTSet` (`$select=Matnr,Spras,Maktx`) und
  mischt das Ergebnis in dieselbe Materialstamm-Map, mit der schon `Mstae` und `Matkl` verteilt
  werden. Dadurch bleiben alle nachgelagerten Signaturen unveraendert.
  `LoadMaterialStatusMapAsync` heisst jetzt `LoadMaterialMasterMapAsync`, weil sie drei Quellen
  fuehrt und der alte Name nur noch den Status nannte.
- **Sprachauswahl** in der eigens herausgezogenen, testbaren `SelectMaterialTexts`: je Material
  genau EIN Text, Deutsch vor Englisch vor allem anderen; bei gleichrangigen Sprachen gewinnt der
  zuerst gelesene Text, damit das Ergebnis nicht von der Antwortreihenfolge abhaengt. Ein- und
  zweistellige Sprachschluessel (`D` wie `DE`) gelten gleich.
- **Nachzug auf den ganzen Cache:** `Maktx` laeuft in `ApplyMaterialMasterToWholeCacheAsync` mit.
  Ohne das haetten Materialien, die nur auf alten abgeschlossenen Bestellungen liegen, dauerhaft
  keinen Text bekommen — dieselbe Falle wie seinerzeit bei der Warengruppe.
- **Anzeige:** neuer zentraler Ausdruck `MaterialLabelSql()` liefert `Materialnummer - Text`.
  Eingesetzt in der dritten Ebene der Spend-Matrix und in der Aufriss-Dimension `Material`, damit
  beide Sichten dasselbe Label zeigen. Fehlt der Text, bleibt die bisherige Anzeige unveraendert
  (Nummer, ersatzweise Bestelltext, sonst „ohne Artikel"); ein Trennstrich ohne Text entsteht nie.
- **Summen bleiben unberuehrt:** Das Label ist eine reine Funktion der Materialnummer, weil der
  Text am Materialstamm haengt und nicht am Beleg. Die Gruppierung erzeugt daher weder neue noch
  zusammengelegte Zeilen; ein Test sichert die Pivot-Eigenschaft ab.
- **Full-Load-Meldung** weist zusaetzlich `MAKT-Texte=<Anzahl>` aus.

Validierung: `dotnet test TrafagSalesExporter.sln` -> `531/531` gruen, davon neu sieben Tests zur
Sprachauswahl (`PurchasingMaterialTextTests`), zwei zur Anzeige in beiden Sichten und zwei zum
Cache-Nachzug.

### Ausfallsicherheit des Textabrufs

Der `MAKTSet`-Read wirft bewusst NICHT, anders als die uebrigen Reads. Der Materialtext ist ein
reines Anzeigefeld und darf den Einkauf-Lauf nicht abbrechen. Genau dieses Muster hat schon
einmal zwei Wochen Datenstillstand gekostet (Nachtrag 2026-07-17 weiter oben: der Full Load vom
2026-07-02 lief in einen `MARA001Set`-404 und brach ab, bevor er LFA1 laden konnte). Faellt
`MAKTSet` aus, wird eine Warnung protokolliert und der Lauf geht ohne Text weiter.

Ergaenzend schreibt `ApplyMaterialMasterToWholeCacheAsync` den Text nur, wenn ueberhaupt einer
geladen wurde. Sonst wuerde ein Ausfall alle bereits vorhandenen Texte leeren, was schlechter
waere als ein veralteter Text. Das entspricht dem bestehenden Schutz fuer die Stammdaten
insgesamt.

### Deploy 2026-08-18

- Commits `6f62fda` (Funktion) und `dbfe364` (Ausfallsicherheit), `532/532` Tests gruen.
- `BiDashboard.dll` Zeitstempel `18.08.2026 11:26:04`, Laenge `4'602'880`, lokal und auf dem
  Server per SHA256 bitgleich.
- Vorher-Sicherung `trafag_exporter.db.before-material-text-20260818-111501.bak`,
  groessengleich mit `346'734'592` Bytes.
- Nachweis in der produktiven Datenbank (read-only, `.tmp_tools/CheckMaktxColumn`): Spalte
  `Maktx` vorhanden, `237'883` EKPO-Zeilen, `7'329` distinkte Materialien, Fuellgrad `0` —
  korrekt, solange kein Load gelaufen ist.
- App startet sauber (`Application started`, Hosting environment Production), keine Fehler im
  Startlog. Der HTTPS-Smoketest vom Arbeitsplatz scheiterte an einem TLS-Handshake des Clients,
  nicht am Server; Port 443 ist offen und der Prozess laeuft.

### Load gelaufen und produktiv nachgemessen 2026-08-18

Der Einkauf-Delta lief von `12:25:35` bis `13:15:31` (rund 50 Minuten) mit Status `Success`.
Ergebnis in der produktiven Datenbank:

| Messgroesse | Wert |
| --- | --- |
| EKPO-Zeilen gesamt | `238'073` |
| Zeilen mit Materialtext | `192'115` (80.7 %) |
| Distinkte Materialnummern | `7'329` |
| **Materialien mit Text** | **`7'327` (100.0 %)** |

Die 45'958 Zeilen ohne Text sind erklaert und kein Fehler: `45'956` davon sind gekontierte
Bestellpositionen **ohne Materialnummer**, die per Definition keinen Materialstamm-Text haben
koennen; genau `2` Zeilen tragen eine Materialnummer, zu der im Stamm kein Text existiert.

Stichprobe des fertigen Drilldown-Labels direkt aus der Produktivdatenbank:

- `B64880 - PCBA HYBRID DENSITY 6.5...20mA 56KG/m3` (BEPRO AG, `10.08.00`)
- `B64336 - PCBA NAT TR5 MODUL CURRENT STD COLDB Rei` (BEPRO AG, `10.08.00`)
- `C17237 - MESSWERK 005.027 1:14.29 NIRO /  NEUSIL` (HB-Feinmechanik GmbH, `40.03.00`)
- `C37836 - MESSWERK BG 87x9 RADIAL` (HB-Feinmechanik GmbH, `40.03.00`)

**Wichtig fuer die Sichtpruefung:** `IPurchasingDashboardService` ist `AddScoped`, der
Seitenzustand haengt also am Blazor-Circuit. Eine Seite, die vor dem Delta-Ende geoeffnet wurde,
zeigt weiterhin den alten Stand ohne Text, bis sie neu geladen wird. Das ist kein Datenfehler.

### Nebenbefund: ueberlappende Delta-Laeufe

`PurchasingSyncState` enthaelt vom 2026-08-18 drei Zeilen mit Status `Running`, die nie
abgeschlossen wurden (Ids 26, 27, 28), neben zwei `Success`-Zeilen (Ids 29, 30). Zwei der
`Running`-Zeilen haben exakt dieselbe Startzeit wie je eine `Success`-Zeile, gehoeren also zum
selben Lauf: der Status wird pro Lauf als NEUE Zeile geschrieben statt die bestehende zu
aktualisieren. Die Zeile von `11:33:53` hat dagegen keinen Abschluss und blieb haengen.
Im Anwendungslog stehen dazu `SQLite Error 5: 'database is locked'`. Ursache sind mehrere
gleichzeitig angestossene Laeufe (manuell plus Nachtlauf). Das ist bestehendes Verhalten, nicht
Teil dieser Aenderung, aber es macht die Statusanzeige schwer lesbar und sollte eigenstaendig
angesehen werden. Auffaellig ausserdem: der Delta laedt `176'003` Belege, praktisch den ganzen
Bestand, und ist damit faktisch ein Full Load.
- Fuellgrad nach dem ersten produktiven Load gegenpruefen. Die 100 % stammen aus `T76/100`.
- Weitere Stellen zeigen die Materialnummer weiterhin ohne Text, bewusst noch nicht angefasst
  (Marcos Leitplanke „ein Punkt nach dem anderen"): Kachel `Top-Artikel`, `Liefertermin-Risiko`,
  die Kontrakt-Detailzeilen und der Artikel-Preistrend. Beim Preistrend dient der Artikel
  zusaetzlich als Join-Schluessel zwischen zwei CTEs, das braucht eine eigene Pruefung.

## Nachtrag 2026-08-27 Sichtpruefung Ingo: produktiv nachgemessen

Ingo hat das produktive Einkaufsdashboard angesehen und mehrere Auffaelligkeiten gemeldet.
Alle Zahlen hier sind am 2026-08-27 **read-only gegen die Produktivdatenbank** gemessen
(`.tmp_tools/SqlQ`, SQLite im ReadOnly-Modus gegen die Serverfreigabe), mit denselben Filtern,
die der Code als Default setzt: `OrdersOnly`, `ExcludeDeletedItems`, `ExcludeEndDelivered`.
Es wurde **nichts geaendert**.

### 1. Der offene Zulauf ist rechnerisch richtig und trotzdem irrefuehrend

Gesamter offener Wert: rund **`26.6` Mio CHF**. Das deckt sich mit Ingos Beobachtung
(„wirkt viel zu hoch, maximal `10` Mio"). Die Aufteilung nach dem Jahr des geplanten
Liefertermins `EKET.EINDT` zeigt, woher der Betrag kommt:

| Liefertermin | offener Wert CHF |
| --- | ---: |
| 2003 bis 2025 | `16.8` Mio |
| 2026 | `9.2` Mio |
| 2027 | `0.7` Mio |

**Rund zwei Drittel des Betrags sind Einteilungen mit einem Liefertermin vor 2026**, die
aelteste stammt aus `2003`. Ingos erwartete Groessenordnung von `10` Mio entspricht fast genau
`2026` plus `2027`. Die Kennzahl rechnet also korrekt, sie zaehlt nur einen Altbestand mit, den
niemand als „Zulauf" liest.

Der fehlende Zeitfilter ist eine bewusste Entscheidung aus dem Marco-Review vom 2026-07-10
(„keine Untergrenze, sonst verschwinden alte ueberfaellige Rueckstaende"). Die Entscheidung ist
fuer die Ueberfaellig-Sicht richtig und fuer die Zulauf-Sicht falsch: derselbe Wert beantwortet
beide Fragen, und fuer die zweite ist er unbrauchbar. Fachlich zu klaeren ist nicht die Formel,
sondern ob diese Altpositionen in SAP je geschlossen werden — `EKPO.ELIKZ` ist dort nicht
gesetzt und `EKET.WEMNG` bleibt unter `EKET.MENGE`.

### 2. Der ueberfaellige Wert ist gross, die Liste darunter ist gedeckelt

Gemessen: **`7'448` ueberfaellige Positionen**, offener Wert **`18.3` Mio CHF**, davon nur
`1.5` Mio mit einem Termin ab `2026`.

Definition, damit sie im Dashboard steht: eine Einteilung zaehlt als ueberfaellig, wenn
`EKET.EINDT` vor heute liegt, die offene Menge `EKET.MENGE - EKET.WEMNG` groesser null ist, die
Position nicht storniert (`EKPO.LOEKZ`) und nicht endgeliefert (`EKPO.ELIKZ = 'X'`) ist, das
Material nicht `MARA-MSTAE` 98/99 traegt und der Beleg eine echte Bestellung ist
(`EKKO.BSTYP = 'F'`, ohne Umlagerung `BSART = 'UB'`).

Ingos Eindruck „der Wert ist viel zu wenig" bezieht sich auf die Liste unter der Kachel: die
Abfrage `OverduePositionRows` endet auf `LIMIT 10`. Gezeigt werden die zehn groessten
Lieferant/Material-Kombinationen, ohne Restzeile und ohne Hinweis. Die Summe der sichtbaren
Zeilen ist deshalb systematisch viel kleiner als die Kachel darueber.

### 3. Die Waehrungsumrechnung ist in Ordnung — anders als vermutet

Geprueft wurde, ob `EKKO.WKURS` fehlt und die Rechnung still auf 1:1 zurueckfaellt. Ergebnis:

| Waehrung | Positionen | Kurs = 0 | Kurs < 0 | Kursspanne | Netto in Belegwaehrung |
| --- | ---: | ---: | ---: | --- | ---: |
| EUR | `30'987` | `0` | `0` | `0.94` bis `1.10` | `143.5` Mio |
| CHF | `15'183` | `0` | `0` | `1.00` | `46.8` Mio |
| USD | `2'594` | `0` | `0` | `0.80` bis `1.00` | `12.4` Mio |
| GBP | `27` | `0` | `0` | `1.10` bis `1.30` | `0.012` Mio |

**Keine einzige Position faellt auf 1:1 zurueck**, es gibt keine negativen Kurse und keine
Waehrung mit TCURR-Kursfaktor. Der theoretisch offene Punkt (fehlender Kursfaktor, stiller
Fallback) trifft mit diesem Datenbestand nicht zu.

Was bleibt, ist eine **Lesefalle, kein Rechenfehler**: Die Perspektive „Waehrung" im
Spend-Aufriss gruppiert nach der **Belegwaehrung** `EKKO.WAERS`, zeigt darin aber
**CHF-Betraege**. Und der Kurs ist der auf dem Bestellbeleg festgeschriebene Kurs zum
Bestellzeitpunkt, nicht der heutige. Beides gehoert an die Kachel geschrieben.

### 4. Die Lieferantenmatrix schneidet stillschweigend ab

`ExecuteSupplierYearRowsAsync` endet auf `.Take(40)` — **ohne** „uebrige"-Restzeile. Der
Aufriss daneben macht es richtig und sammelt den Rest. Die Matrixsumme ist dadurch kleiner als
der Spend total, ohne dass es irgendwo steht. Der Hinweistext unter der Tabelle erklaert nur
die Deckelung auf `25` Materialien je Warengruppe, nicht die Deckelung der Lieferanten.

Ingo sieht produktiv `20` Lieferanten, im Repository stehen `40`. Der Unterschied ist noch
nicht erklaert; wahrscheinlich laeuft produktiv ein aelterer Stand.

### 5. Was die Kennzahlen bedeuten — Kurzfassung fuer die Beschreibungstexte

| Kachel | Was genau gerechnet wird | Was sie NICHT ist |
| --- | --- | --- |
| Spend total | `SUM(EKPO.NETWR)` nach CHF, Belege im Zeitraum ueber `EKKO.BEDAT` | kein Wareneingang, keine Rechnung, keine Zahlung |
| Spend nach Jahr | dasselbe, Jahr aus `EKKO.BEDAT` (Bestelldatum) | nicht das Liefer- oder Buchungsjahr |
| Offener Zulauf | `SUM((EKET.MENGE - EKET.WEMNG) x CHF-Stueckpreis)`, zeitraumunabhaengig | kein Forecast, keine Faelligkeitssicht |
| Ueberfaelliger Wert | wie oben, zusaetzlich `EKET.EINDT < heute` | keine Verzugsstrafe, kein Fehlteilrisiko |
| Kontrakt-Restwert | wie offener Zulauf, aber nur Belege mit gesetztem `EKKO.KONNR` | nicht die Kontraktmenge selbst |

Der Belegbestand trennt sauber: `151'249` Bestellungen (`BSTYP = 'F'`), `10'948` Kontrakte
(`K`), `14'006` Anfragen (`A`), `64'059` Belege mit Kontraktbezug `KONNR`. `BSTYP` ist bei
**null** Belegen leer, die Belegtyptrennung greift produktiv also vollstaendig.


### Abgleich mit der Sitzung Marco/Ingo vom 2026-08-27

Marco hat dieselben Kacheln live durchgesprochen. Seine abgelesenen Werte bestaetigen die
Messung oben **auf den Franken genau** und beantworten damit seine eigene offene Frage:

| Kachel im Dashboard | Marco liest ab | hier gemessen |
| --- | ---: | ---: |
| Spend total / „Bereits beschafft" | `24'895'000` | `EKPO.NETWR` in CHF ueber `EKKO.BEDAT` |
| Verpflichtungen / „Disponierter Zulauf" | `26'627'000` | `26.6` Mio, davon `16.8` Mio mit Termin vor 2026 |
| Ueberfaelliger Wert | `18` Mio, `7'533` Einteilungen | `18.3` Mio, `7'448` Positionen |

**Marcos Kernfrage ist damit beantwortet.** Er vermutete, der offene Bestellwert enthalte
neben Normalbestellungen auch Mengenkontrakte (Nummernkreis `46`) und sei deshalb so hoch.
Das trifft **nicht** zu: Die Messung oben lief ausschliesslich auf `EKKO.BSTYP = 'F'` und
traf den Dashboardwert exakt. Kontrakte sind also bereits ausgeschlossen. Der Grund fuer die
Hoehe ist ein anderer und liegt in den Altterminen. Marcos eigene Plausibilisierung
(„maximal drei Monate im Voraus bestellt, also hoechstens rund `10` Mio") passt genau auf die
`9.2` Mio aus 2026 plus `0.7` Mio aus 2027.

**Zwei Kachelbeschriftungen fuehren in die Irre und gehoeren korrigiert:**

1. „Bereits beschafft" beziehungsweise „Bereits beschafft / gebucht" traegt den
   Bestellwert `EKPO.NETWR`. Marco hat es im Gespraech als „Bestellungen, wo wir schon
   Wareneingaenge haben" gelesen — genau die falsche Bedeutung. Ein Wareneingangsbezug
   braeuchte `EKBE`/`MSEG` und existiert nirgends im Dashboard.
2. „Verpflichtungen" und „Disponierter Zulauf" sind **dieselbe Zahl**
   (`OpenValueSample`), nur zweimal beschriftet. Marco hat das selbst vermutet
   („das ist irgendwie das Gleiche, oder?").

**Zur Waehrung, Rueckmeldung von Armin ueber Marco.** Armin hat zwei Punkte gemeldet: die
Balken starten nicht am gleichen Ort, und die Umrechnung stimme nicht. Marco hat
gegengerechnet und kommt auf rund `0.94` CHF je EUR und `0.80` CHF je USD, was er fuer
plausibel haelt; die Messung oben bestaetigt genau diese Spannen. Marcos Verdacht ist
vermutlich richtig: gemeint ist wahrscheinlich der **interne Umrechnungskurs**. Die Antwort
darauf steht fest: Das Dashboard rechnet mit `EKKO.WKURS`, also dem **auf dem Bestellbeleg
festgeschriebenen Kurs zum Bestellzeitpunkt**, nicht mit einem internen Planungs- oder
Budgetkurs und nicht mit dem Tageskurs. Welcher Kurs fachlich gelten soll, ist ein Entscheid
und keine Fehlersuche.

**Zur Lieferantenkaskadierung** hat Marco die Anforderung praezisiert: Er will die
**vollstaendige Liste** ohne Abschneiden, ausdruecklich auch die kleinen Lieferanten, „gerade
wenn du die Kleinen mal aufraeumen willst". Eine lange Liste stoert ihn nicht, gescrollt wird
ohnehin. Ein Limitschieber waere nett, ist aber ausdruecklich **nicht** verlangt.

**Nicht zu aendern, ausdruecklich bestaetigt:** Der Lagerwert ist als Stichtagsbetrachtung
richtig verstanden, ein woechentlicher Lauf reicht Marco. Der Materialtext-Drilldown ist
abgenommen („wirklich recht cool"). Marcos Lesehilfe dazu, die im Dashboard fehlt: Eintraege
**mit** Bindestrich sind echte Artikel mit Materialnummer, Eintraege **ohne** Bindestrich sind
Textbestellungen ohne Artikelstamm.

### Offen

- Anzeigefehler (Balkenstart, Balkenfarbe, verschobene Balken) sind noch nicht geprueft; dafuer
  muss die Oberflaeche selbst angesehen werden.
- Die Beschreibungstexte im Dashboard sind noch nicht angepasst.
- Deckelung der Lieferantenmatrix und `LIMIT 10` der Ueberfaellig-Liste sind unveraendert.

## Nachtrag 2026-08-27 Umsetzung: Deckelungen weg, Balken ausgerichtet, Kacheln erklaert

Umgesetzt nach der Sichtpruefung oben, **noch nicht deployed**. `633/633` Tests gruen.

### 1. Keine Deckelung mehr im Spend-Aufriss und in der Matrix

Alle vier Perspektiven laufen jetzt auf `NoCap`, ebenso die Lieferanten-Jahres-Matrix und die
Artikelebene darunter. Vorher waren es Lieferant `40`/`15`/`10`, Region `12`/`15`/`10`/`8`,
Warengruppe `20`/`15`/`10`, Waehrung `8`/`15`/`10`/`8` und `25` Artikel je Warengruppe in der
Matrix; der Rest verschwand in einer „uebrige"-Zeile, in der Matrix sogar ersatzlos.

Die Deckelung stammte aus der Sorge, der serverseitig gerenderte Baum koenne bei ueber
`230'000` Positionen explodieren. Produktiv nachgemessen am 2026-08-27: `707` Lieferanten,
`1'244` Lieferant/Warengruppe-Paare, `17'064` Blattknoten ueber alle Jahre. Das traegt die
Oberflaeche, zumal Kinder erst beim Aufklappen gerendert werden. Die „uebrige"-Buendelung
bleibt im Code stehen und greift nur nicht mehr.

### 2. Die Balken starten wieder an derselben Kante

Ursache war kein Rechenfehler, sondern das Raster. `display: grid` sass auf der **einzelnen**
Balkenzeile, jede Zeile war also ihr eigenes Raster. Die dritte Spalte ist `auto` und damit so
breit wie ihr Text — und der ist je Zeile verschieden lang, etwa
`CHF 145'068'141 (143'041'648 EUR)` gegen `CHF 14'403 (12'033 GBP)`. Dadurch bekamen die
`1fr`- und `2fr`-Spalten in jeder Zeile eine andere Breite, und die Balken standen treppenartig
versetzt. Genau das hat Armin gemeldet.

Behoben in **beiden** Komponenten, nicht nur an der auffaelligen Stelle: `PurchasingSection`
und `PurchasingSpendExplorer` tragen das Raster jetzt am Container, die Zeile steht auf
`display: contents`. Damit richten sich alle Zeilen an denselben Spaltenkanten aus.

### 3. Kacheltexte sagen jetzt, was gezaehlt wird

Jede KPI-Kachel hat eine zweite, erklaerende Zeile bekommen (`ExplainDe`/`ExplainEn`). Die
bisherige Kurzzeile nannte nur die Datenquelle, nicht die fachliche Bedeutung. Bewusst kurz
gehalten, zwei Saetze je Kachel: ein Absatz in einer Kachel liest sich nicht, und jeder
deutsche Text braucht sechs Uebersetzungen.

Zwei Beschriftungen waren nachweislich irrefuehrend und sind umbenannt:

| vorher | jetzt | warum |
| --- | --- | --- |
| `Bereits beschafft`, `Bereits beschafft / gebucht` | `Bestellwert im Zeitraum` | Marco hat es als „Bestellungen, wo wir schon Wareneingaenge haben" gelesen. Es ist der Bestellwert; einen Wareneingangsbezug gibt es im Dashboard nirgends. |
| `Offener Bestellwert` **und** `Disponierter Zulauf` als zwei Zeilen | eine Zeile `Offener Bestellwert (= disponierter Zulauf)` | Beide zeigten denselben Wert `OpenValueSample`. Marco hat es selbst bemerkt. |

Die Abschnittsbeschreibungen von „Spend total vergangen" und „Offene Bestellwerte und Mengen"
nennen jetzt ausdruecklich, dass am Bestelldatum gezaehlt wird, dass der offene Wert
zeitraumunabhaengig ist und deshalb Alttermine enthaelt, und dass keine Mengenkontrakte
mitzaehlen. Die Matrix-Bildunterschrift nennt zusaetzlich Marcos Lesehilfe: Materialeintrag
**mit** Bindestrich ist ein echter Artikel, **ohne** Bindestrich eine Textbestellung.

### Weiterhin offen

- Die `LIMIT 10`-Liste unter „Ueberfaellige Positionen" ist unveraendert. Sie ist nicht
  Gegenstand des Auftrags gewesen, faellt aber in dieselbe Klasse wie die entfernten
  Deckelungen.
- Die Balkenfarbe (Armins „nicht gruen") ist nicht angefasst. Die Farbe kommt aus der Zeile
  selbst und ist keine Statusfarbe; ob das gewollt ist, ist eine Gestaltungsfrage.
- Der produktive Sichtnachweis fehlt: die Chrome-Erweiterung war nicht verbunden. Nach dem
  Deploy gehoert die Waehrungskachel angesehen, bevor der Punkt als erledigt gilt.

## Nachtrag 2026-09-03: gemeinsamer Snapshot-Cache produktiv

Die direkten Einkaufs-Unterseiten verwendeten dieselbe teure Berechnung, fuehrten sie aber
bei jedem Seitenwechsel erneut aus. Produktiv gemessen vor der Aenderung: `/einkauf` rund
`9.2 s`, `/einkauf/aufriss` rund `9.8-11.1 s` auch bei wiederholten Aufrufen.

`PurchasingDashboardSnapshotCache` haelt den vollstaendigen Snapshot jetzt filterabhaengig
fuer 15 Minuten, begrenzt den Bestand auf 32 Filter und buendelt parallele Erstaufrufe per
Single-Flight. Eine erfolgreiche Full- oder Delta-Aktualisierung invalidiert alle Snapshots.
Die aufrufseitige Abbruchanforderung beendet nur das Warten des einzelnen Aufrufers, nicht
die gemeinsam laufende Berechnung. Eine Generation verhindert ausserdem, dass ein waehrend
einer Invalidierung bereits laufender Alt-Load anschliessend wieder als aktuell gespeichert
wird.

Beim ersten Publish machte das SQLite-Update einen Query-Planer-Unterschied sichtbar: Die
CTE `article_spend` in der Artikelpreistrend-Abfrage lief mit SQLite `3.53.3` ohne feste
Materialisierung in das Proxy-Timeout. `AS MATERIALIZED` stabilisiert den Plan. Gegen eine
Kopie der Produktionsdatenbank lief die Komplettberechnung danach in rund `18-30 s`; auf dem
Produktivserver benoetigte der erste Aufruf direkt nach Neustart `83.17 s`, blieb aber unter
der 120-Sekunden-Grenze. Danach lieferten alle 14 weiteren Einkaufsrouten HTTPS `200` in
`0.05-0.14 s`.

Nachweis: Commits `756e931` und `2444731`, vier gezielte Cachetests, **674/674** Release-
Tests, NuGet-Audit ohne bekannte Schwachstelle sowie produktiver Deploy am 2026-09-03.
