# Einkaufsdashboard 2026-06-05

Nachtrag 2026-06-18: Das Einkaufsdashboard wurde fuer die Management-/Einkaufssicht nachgezogen und deployed. Schwerpunkt war die Excel-aehnliche Lieferant/Jahr-Kaskadierung analog Referenzbild `einkauf.png`, Zeitraum 2020 bis aktuelles Jahr, Spend aktuelles Jahr je Lieferant, offene Bestellungen/Zulauf, Filter fuer Loeschkennzeichen und MARA-MSTAE sowie echte Lieferantennamen statt Platzhalter.

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
Siehe `docs/rag/DEPLOYMENT.md` und `docs/PURCHASING_DASHBOARD_VORBEREITUNG_INGO_2026-07-09.md` (A0).

## Nachtrag 2026-07-10 Review-Mail Marco und Sofort-Korrekturen

Marco (Einkaufs-Koordinator) hat das produktive Cockpit durchgesehen; vollstaendiges Mapping in
`docs/PURCHASING_DASHBOARD_REVIEW_MARCO_2026-07-10.md`. Sofort umgesetzt (157/157 Tests gruen,
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

Grundlage: `docs/PURCHASING_DASHBOARD_UMSETZUNGSPLAN_MARCO_2026-07-09.md`. Umgesetzt wurde der
code-seitig ohne externe Inputs machbare Teil von Phase 1; alles Uebrige (Referenzlisten,
SAP-Metadaten-Checks, neue SAP-Objekte) ist in
`docs/PURCHASING_DASHBOARD_VORBEREITUNG_INGO_2026-07-09.md` als Vorbereitungsauftrag beschrieben.

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

Noch offen / vorzubereiten (siehe `PURCHASING_DASHBOARD_VORBEREITUNG_INGO_2026-07-09.md`):

- Phase 1.3/1.4: Warengruppen-Text-CSV (T023T) und Disponenten-CSV (ZC23) von Ingo.
- Phase 2: OData-Proben LFA1-Adresse/Elikz/MBEW/Kontraktbelege.
- Phase 3: EKBE (Termintreue), QM-Export (Reklamation), RESB/MARC (Lager).

## Nachtrag 2026-07-09 Anforderungs-Mail Marco / Umsetzungsplan

Marco (Einkauf) hat nach dem Review vom 2026-07-08 die Anforderungen der Hauptanspruchsgruppen
schriftlich umrissen (Echtzeit-Uebersicht Einkaufstransaktionen, 7 Aufrisse, KPIs zu
Beschaffungstransaktionen/Lager/Lieferantenperformance). Die Anforderungen wurden gegen den
Code- und Datenstand gemappt und in einen Phasenplan uebersetzt:

- Arbeitsauftrag: `docs/PURCHASING_DASHBOARD_UMSETZUNGSPLAN_MARCO_2026-07-09.md`
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

Grundlage: Formel-Review in `docs/PURCHASING_DASHBOARD_KORREKTUREN_2026-07-06.md`. Umgesetzte
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
  `COALESCE(MaraMatkl, Matkl, 'ohne Warengruppe')` — solange SAP `Matkl` im Materialstamm-Set
  noch nicht liefert (siehe unten), faellt die Anzeige transparent auf die Beleg-Warengruppe
  zurueck; ein UI-Hinweis kennzeichnet das.

### Wichtiger Nebenbefund: SAP hat das MARA-EntitySet umgebaut (Prod-Fix noetig)

Live-Probe gegen `travp762` (Tool `.tmp_tools/ProbePurchasingMara`):

- `MARA001Set` (EntityType `MARA`) exponiert **`Mstae` NICHT mehr** — der bisherige produktive
  Read `MARA001Set?$select=Matnr,Mstae` antwortet mit `404 Resource not found for the segment
  'Mstae'`. Der bestehende Full Load/Delta waere damit beim naechsten Lauf FEHLGESCHLAGEN.
- Ersatzquelle: neues EntitySet **`maracalcSet`** (EntityType `maracalc`) enthaelt `Mstae`
  (verifiziert: 68'094 Zeilen, 33'242 mit Status, u.a. 27'929 x `99`, 1'609 x `98`).
- FIX umgesetzt: `LoadMaterialStatusMapAsync` liest jetzt `maracalcSet`. Achtung: das Set
  ignoriert `$top`/`$skip` (gleiches Verhalten wie `mbewSet`) und liefert immer den vollen
  Bestand — deshalb bewusst EIN ungepagter Request statt des Paging-Helpers, sonst wuerde jede
  "Seite" erneut ~68'000 Zeilen laden.
- `Matkl` ist in KEINEM MARA-EntityType des Service vorhanden -> **SAP-Erweiterungsanfrage:
  `Matkl` in `maracalc` aufnehmen.** App-Seite ist fertig vorbereitet (Cache-Spalte, Map,
  Write-Pfad); nach der SAP-Erweiterung ist nur das `$select` um `,Matkl` zu ergaenzen.

### ABC/XYZ: von "geparkt" zu konkretem Weg (spaeterer Punkt, nicht jetzt)

Neue Info aus der Feedback-Runde: ABC-Kennzeichen = `MARC-MAABC` (Sicht O2); XYZ liegt in einer
separaten Tabelle; ein vorhandener SAP-Report kann beides bereits extrahieren. Damit ist der
Weg klar (MARC-Anbindung oder Report-Export als Referenzliste) — wird aber gemaess "ein Punkt
nach dem anderen" erst nach Abnahme des Spend-Reiters angegangen.

### Validierung

- `dotnet test TrafagSalesExporter.sln --verbosity minimal`: `257/257` Tests gruen
  (2 neue Drilldown-Tests: Warengruppen-Aufriss inkl. MaraMatkl-Vorrang und
  Zeitraumfilter-Wirkung auf die Drill-Ebene).
- Noch nicht deployed. NACH Deploy: Einkauf Full Load laufen lassen (fuellt `Mstae` wieder aus
  `maracalcSet`; `MaraMatkl` bleibt leer bis zur SAP-Erweiterung).
