# Einkauf: Funktionspruefung im Produktivdashboard (2026-11-05)

Stand: 2026-11-05 (Nachtrag am selben Tag: F2, F3, F4, F6, F8, F9 und Teile von Punkt 10 im Code behoben, Commit `6ab94a7` als `01ab4b9` uebernommen und **deployed 2026-11-05 11:48 (`a17d623`)**, im Browser bestaetigt (F3 Stueckpreis CHF 1.13 mit Hinweis ohne Vorjahr, F8 „SAP-Daten geladen“) *(ueberholt: „nicht deployed“)*; F5 geprueft, Befund unten; F1 und F7 offen). Auftrag Ingo: „kannst du mal die funktionen pruefen wie gestern hr ... das auch fuer einkauf“.
Geprueft am 2026-11-05 um 10:28 im Chrome auf `https://trch-webapp-bidashboard.trafagch.local/BiDashboard/einkauf...`,
nur gelesen und Ansichtsfilter bedient (keine Laeufe, keine Einstellungen). Datenstand: Delta 2026-11-04 14:58 erfolgreich,
Standardzeitraum 2026-01 bis 2026-11. Keine Konsolenfehler. **Nichts davon ist behoben**, ausser wo vermerkt.

## Ergebnis je Ansicht

| Ansicht | Status | Werte / Befund |
| --- | --- | --- |
| `/einkauf` | ok | Bestellwert 32'997'456, 6'681 Bestellungen, offen 9'014'591, Kontrakte 18'231'422, 333 Lieferanten, Lagerwert 9'189'770; Disponentenspalten KW 45 = Kachel. Disponent 006 ueberall 0 (Handbuch sagt noch 001 bis 005; *Handbuch nachgefuehrt `6ab94a7`*) |
| `/einkauf/offene-bestellungen` | ok | ueberfaellig 778'700 (94 Pos.), Altposten 169 Pos. 3'084'784; offene Menge 6'403'927 addiert verschiedene Einheiten |
| `/einkauf/kontrakte` | ok | Umschalter stimmen (45 abgelaufene Positionen 131'158, „vermutlich ersetzt“ 2 Pos. 522'711); Kachel 44 Kontrakte vs. 45 Positionen |
| `/einkauf/lieferanten` | Auffaelligkeit | Preisindikator CHF 1.13 ohne Vorjahresvergleich, Text „braucht EKPO“ ueberholt; *behoben `6ab94a7`* |
| `/einkauf/aufriss`, 11 Einstiege | ok | Schalter an 30'510'805, aus 32'997'456 (= Kachel); 2020 bis heute 211'069'693; Einstiege untereinander stimmig |
| Aufriss, Matrix Lieferant / Jahr | **Fehler** | 2020 bis heute 211'051'808 statt 211'069'693 (-17'885), siehe F2; *behoben `01ab4b9`, deployed 05.11. 11:48* |
| Aufriss, Materialanteil je Produktgruppe | **Fehler** | HW_ZUKAUFSORT 957'060'506 bei Umsatz 3'643'696 (26'266 %), Total 1'226 %; FP_THERMOST. 615 %, siehe F1 |
| `/einkauf/lieferperformance` | Auffaelligkeit | Kachel 494 „Faelle“ = Materialien, Balken P1+P2 = 555; „offener Bestellwert“ 7'315'040 ist nur der ZLO03-Umfang; ZLO03/ZMD04-Stand 02.09.2026 (*Korrektur: nicht ZLO03-Umfang, sondern ohne OK-Faelle; behoben `6ab94a7`, siehe F6*) |
| `/einkauf/ideen/*` | ok bis Auffaelligkeit | Liefertermin-Risiko summiert auf 9'014'591; Spend-Konzentration Top 10 = 54,7 %; Preisentwicklung siehe F3; Datenqualitaet mischt Bestand (240'780) und Zeitraum (7'427) |
| `/einkauf/3d` | Fehler (Beschriftung) | „-10 %“ = -1'507'099 auf Basis der 6 gezeigten Balken (15'070'990), nicht auf 32'997'456; *behoben `6ab94a7`* |
| `/einkauf/verbindungen` | Auffaelligkeit | „Einkaufsquelle aktivieren“ steht auf aus, Text bindet den Nachtlauf daran; letzter Lauf 04.11. 14:58 (siehe F5) |
| `/einkauf/kennzahlen`, `/einkauf/pbix`, `/einkauf/spend`, `/einkauf/interaktiv` | ok | Statustext „Spend CHF: Live-Probe“ ueberholt; *behoben `6ab94a7`* |

Nicht geprueft: Stuecklisten-Netz „Gehirn“ (Einfriergefahr), Materialanteil nur fuer Warengruppen ab CHF 10'000 aufgeklappt,
Filter der Lieferperformance.

## Fehlerliste (priorisiert)

1. **F1 Materialanteil, Wafer (Ursache belegt; FIX 26 in T76 umgesetzt, Auftrag `T76K912839`, Import P76 und Einkauf-Lauf offen):** Stuecklisten-Basismenge `STKO-BMENG` (3'738 bis 4'750 ST je Wafer) wird in
   `Z_ZLO03_TURBO2` ignoriert, ZLO03 rechnet 1 Rolle Wafer je Chip. Befund und Vorschlag FIX 26:
   `zlo03/BEFUND_EINHEIT_WAFER_2026-11-05.md` (T76 lesend). Richtig waeren rund CHF 214'000 bis 220'000 statt 956 Mio.
   **Weiterer Ausreisser FP_THERMOST. (615 %):** H20201 Kapillarrohr 4'329 KGM, H17500 Bimetall 2'040 MTR fuer 24'155 Umsatz;
   *Nachtrag 2026-11-05 (FIX-26-Agent, T76 belegt): andere Ursache. H20201/H17500 sind Selbststuecklisten (00045280/00045289, BMENG 1), dazu wird bei H20201 MM als KG gezaehlt; Beleg 63759: 116 KG statt 0.003 KG. Nicht Teil von FIX 26, Vorschlag in `zlo03/FIX26_BASISMENGE_2026-11-05.md` Abschnitt 6.* (Urspruenglich: „vermutlich dieselbe Ursachenfamilie, nicht belegt“.) FP_FROSTI 84 % mit auffaellig gleichen Mengen
   (20'802) bei mehreren Teilen, Verdacht Doppelzaehlung ueber Stufen oder Alternativstuecklisten. Dazu G4 (Mehrstufigkeit).
2. **F2 Matrix Lieferant / Jahr verliert Spend bei Namen, die sich nur in Gross-/Kleinschreibung unterscheiden:** „THUBA AG“
   und „Thuba AG“ werden zu einer Zeile, je Jahr gewinnt ein Wert statt Summe (-17'885). Gleiche Dubletten bei Trafag AG /
   TRAFAG AG und SFS unimarket / SFS Unimarket (dort ohne Verlust). Der Aufriss zeigt sie doppelt (724 statt 721 Zeilen).
   *Behoben 2026-11-05 (`6ab94a7`):* Ursache `Services/PurchasingDashboardService.cs` `ExecuteSupplierYearSpendRowsAsync`
   (vorher Zeile 1157/1186) und `ExecuteSupplierGroupYearRowsAsync` (1219/1247/1257/1275): SQLite gruppiert das Label
   gross/klein getrennt, die C#-Dictionaries waren `OrdinalIgnoreCase`, und `values[year] = value` ueberschrieb. Jetzt
   `Ordinal` wie das `GROUP BY` und der Aufriss (`BuildCascadeLevel`), Jahreswerte werden addiert, Aufklapp-Sets der
   Matrix ebenfalls `Ordinal`. Die Matrix zeigt damit dieselben 724 Zeilen wie der Aufriss und dieselbe Summe; keine
   andere Zahl aendert sich. Gruppierung nach Lieferantennummer statt Name wurde **nicht** umgesetzt: sie wuerde auch
   Lieferanten mit gleichem Namen und verschiedener Nummer trennen, die heute in beiden Sichten zusammenstehen, also
   fremde Zahlen aendern; Entscheid Ingo. Ob THUBA AG / Thuba AG zwei Lieferantennummern sind oder eine Nummer mit
   altem Namen in alten Belegen (der Delta-Lauf schreibt `SupplierName` nur fuer geaenderte Belege neu), ist nicht
   geprueft. Gleiches Muster behoben in `ExecuteArticlePriceTrendRowsAsync` (Preise je Artikel, `OrdinalIgnoreCase` mit
   Ueberschreiben). `QuantityContractSupplierRows` gruppiert ebenfalls ohne Gross-/Kleinschreibung, summiert aber
   (kein Wertverlust), unveraendert. Test `SupplierYearMatrix_Keeps_Names_Differing_Only_In_Case_And_Matches_The_Spend_Breakdown`.
3. **F3 Preisentwicklung:** „Stueckpreis nach Jahr“ zeigt fuer alle Jahre „CHF 1“ (Rundung, Wert um 1.13); seit dem
   Standardzeitraum „laufendes Jahr“ kein Vorjahresvergleich mehr (auch Preisindikator Lieferanten).
   *Behoben 2026-11-05 (`6ab94a7`):* Ursache `Components/Pages/PurchasingDashboard.razor` `SelectedIdeaChartRows`
   (vorher Zeile 2023): das Diagramm nutzte `FormatChf` mit `N0` (Zeile 2289). Jetzt `FormatUnitPriceChf` mit `N2`.
   Vorjahr: Code und Handbuch rechnen nur im gewaehlten Zeitraum (Handbuch „Veraenderung gegenueber dem letzten Vorjahr
   mit Daten“); ein automatisches Einbeziehen des Vorjahres stand nirgends und haette die Hotlist veraendert. Gewaehlt
   ist deshalb der Hinweis: unter dem Diagrammtitel („Kein Vorjahresvergleich … Knopf „2020 bis heute““), im
   Preisindikator „(kein Vorjahr im Zeitraum)“ und in der Hotlist „Jahr 2026, kein Vorjahr im Zeitraum“. Nebenbefund
   behoben: Seitentext und Handbuch sagten, die Hotlist zeige das Power-BI-Minimum; der Code rechnet den
   mengengewichteten Durchschnitt (`ExecuteArticlePriceTrendRowsAsync`). Kachel-Untertitel „braucht EKPO“ ersetzt.
   Test `PriceTrend_Keeps_Decimals_And_Says_When_The_Period_Has_No_Prior_Year`.
4. **F4 3D-Simulation:** Delta-Beschriftung nennt die Basis nicht.
   *Behoben 2026-11-05 (`6ab94a7`):* Ursache `PurchasingDashboard.razor` `FormatScenarioDelta` summierte die gezeigten
   Balken (`ResolvePurchasing3dLiveRows`, z. B. Top-Lieferanten) ohne das zu sagen. Gewaehlt ist die kleinere, ehrliche
   Loesung: die Beschriftung nennt die Basis („-1'507'099 CHF auf Basis der 6 gezeigten Balken (zusammen 15'070'990
   CHF)“). Eine Hochrechnung auf den ganzen Spend haette je Indikator eine eigene Gesamtgroesse gebraucht, die die
   Grafik gar nicht zeigt. Preisentwicklung: Delta auf den Ø-Stueckpreis des juengsten Jahres (Durchschnitte sind nicht
   addierbar); ohne Livedaten „auf Beispielwerte (Simulation), keine Trafag-Zahl“. Kein Unit-Test (Razor-Logik), Build
   und Uebersetzungstest gruen; im Browser noch nicht gesehen.
5. **F5 Nachtlauf:** Einkaufsquelle „aus“, kein Lauf seit 04.11. 14:58 (Pruefung 05.11. 10:28). Entweder Text ueberholt
   (laut `docs/rag/PURCHASING.md` seit `66a34da` nicht mehr an IsActive gebunden) oder der Nachtlauf lief nicht; Laufprotokoll pruefen.
   *Geprueft 2026-11-05 (nur Code, keine Produktiv-DB, nichts geaendert):* Der Lauf haengt **nicht** am Haken
   „Einkaufsquelle fuer Import aktivieren“ (`Sites.IsActive`). `Services/TimerBackgroundService.cs`
   `RunPurchasingDeltaAsync` (Zeile 166 bis 194) prueft nur, dass die Site `PurchasingTsc` existiert; ebenso der
   Nachholweg `CheckPurchasingCatchUpAsync` (Zeile 204 bis 255). Er ist auch kein Nachtlauf: er startet im Timer-Slot
   `ExportSettings.TimerHour:TimerMinute` nach dem Verkaufsexport (Zeile 88 bis 95), sofern `TimerEnabled`. Der Slot steht
   in der DB (Seed 03:00, `DatabaseSeedService.cs` Zeile 63); produktiv laut `docs/EINKAUF_LAGERWERT_2026-08-18.md`
   12.2 und 13.7 **12:00**. Nachgeholt wird nur nach dem heutigen Slot und wenn der letzte Erfolg mindestens 20 Stunden
   alt ist (`TimerSchedule.IsPurchasingCatchUpDue`, `PurchasingCatchUpMinAge`). Folge: Bei der Pruefung um 10:28 lag der
   Slot des 05.11. noch vor uns, ein Lauf war nicht faellig; die Luecke seit 04.11. 14:58 ist erwartet, kein Ausfall.
   Nicht belegt (nur in der DB/im Protokoll pruefbar): `TimerEnabled` = an, Slot wirklich 12:00, und ob 04.11. 14:58 ein
   Slot-, Nachhol- oder Handlauf war. Pruefen nach 12:15: `/einkauf/ideen/datenservice` bzw. `PurchasingSyncState`.
   **Ueberholte Texte (nicht geaendert, Auftrag „nur pruefen“):** `Components/Pages/PurchasingDataSources.razor`
   Zeile 95 „Nachts laeuft automatisch ein Delta, sofern die Einkaufsquelle aktiv ist“ (zweimal falsch) und Handbuch
   `02_einkauf.md` Zeile 5 „Nachts läuft ein Delta-Lauf“ und Abschnitt Datenquellen (Zeile 590) „Nachts läuft
   automatisch ein Delta, solange die Einkaufsquelle aktiv ist“. Vorschlag: „Taeglich nach dem Verkaufsexport (Timer-Slot)
   laeuft ein Delta, unabhaengig von diesem Haken“. **Der Haken soll aus bleiben:** laut `TimerBackgroundService.cs`
   Zeile 153 bis 158 wuerde `IsActive = 1` den Verkaufsexport (`ExportAllAsync` ueber alle aktiven Sites) dazu bringen,
   die Einkaufs-Pseudo-Site `PURCHASING_SAP` als Verkaufsstandort zu exportieren (Entscheid Ingo 2026-07-30).
6. **F6 Lieferperformance:** Kachel zaehlt Materialien, nicht Faelle; Beschriftung „offener Bestellwert“ irrefuehrend; Datenstand alt.
   *Behoben 2026-11-05 (`6ab94a7`):* Ursache `Services/SupplyChainAnalysisService.cs` `BuildKpis` (vorher Zeile 379):
   Wert `affected` = eindeutige Materialien, Untertitel „Material-/Lieferantenfaelle“; eine Zeile ist aber ein Fall
   Material x Lieferant (`LoadDeliveryRowsAsync`, `GROUP BY` Material, Lieferant). Jetzt zaehlt die Kachel die Faelle
   (= Balken P1 + P2) und nennt die Materialien im Untertitel. **Korrektur zum Befund oben:** der offene Wert ist nicht
   der ZLO03-Umfang; Materialien ohne ZLO03-Eintrag zaehlen mit (`EmptyRow`). Kleiner als 9'014'591 ist er, weil
   „Nur Handlungsbedarf“ (Vorgabe an) die OK-Faelle mit spaeterem Termin aus Kacheln und Tabelle nimmt; Untertitel sagt
   das jetzt („nicht der ganze offene Bestellwert“). Datenstand: ZLO03/ZMD04 (02.09.) liefert hier nur Disponent und
   Produktgruppe; der Kasten beschriftet das und trennt den Stand der Bestellbelege. Test
   `DeliveryPerformance_Kpi_Counts_Cases_Like_The_Priority_Bars_And_Names_The_Materials`.
7. **F7 Kontrakte „vermutlich ersetzt“:** C24503 -> C24504 (46009867/00010, CHF 12'711) vermutlich Fehltreffer ueber gleichen Text.
8. **F8 Muster „Wartet auf SAP“-Badge** trotz geladener Daten auf acht Seiten (vermutlich alte Live-Probe-Logik).
   *Behoben 2026-11-05 (`6ab94a7`):* Ursache `Components/Pages/PurchasingSection.razor` (vorher Zeile 134 bis 153): Farbe
   und Marke verglichen die Spalte „Quelle“ mit dem Literal „SAP live“. Commit `d5bc321` (2026-09-10) hatte es in
   `PurchasingDashboard.razor` durch `DataSourceLabel` („SAP-Datenbestand“/„SAP-Stichprobe“, uebersetzt) ersetzt, seither
   fiel jede Section auf „Wartet auf SAP“. Neu `PurchasingSectionSource` (`Models/PurchasingSectionModels.cs`): geladen
   ist jede Quelle, die weder der Wartehinweis noch „Simulation“ ist; Marke „SAP-Daten geladen“, Quell-Chip
   „Wartet auf SAP“ wird uebersetzt. Gilt fuer alle Sections (Offene Bestellungen, Kontrakte, Lieferanten, Spend-Block,
   vier Ideen-Seiten). Test `PurchasingDisplayFixTests`.
9. **F9 Materialanteil-Spalte „Anteil“** hat je Stufe eine andere Basis (Produktgruppe: Anteil am Umsatz, Materialgruppe:
   Anteil an der Produktgruppe, Teil: Anteil an der Materialgruppe); kleine Anteile erscheinen als „0 %“. Frage Ingo 05.11.
   *Behoben 2026-11-05 (`6ab94a7`), nur Darstellung:* `Components/Pages/PurchasingMaterialShare.razor` `Percent` (vorher
   Zeile 206) formatierte `0.#`. Jede Zelle nennt jetzt ihre Basis („… % vom Umsatz“, „… % der Produktgruppe“,
   „… % der Materialgruppe“), ein Satz unter dem Titel erklaert es, Werte unter 0.05 % stehen als „< 0.1 %“
   (`PurchasingMaterialShareQuery.FormatShare`). Rechnung unveraendert; die falschen Werte (F1, FIX 26) bleiben, bis
   ZLO03 korrigiert ist. Test `PurchasingDisplayFixTests`.
10. **Kleinigkeiten:** Zeitstempel 13:58 vs. 14:58 (UTC/Ortszeit), Kommaformat und englische Ampel in Ideen-Hotlists, ISO-Einheiten
    (PCE, KGM, RO) neben „ST“, ueberholte Statustexte (Kennzahlen, Ideen „Bestellrhythmus berechenbar“, Handbuch „001 bis 005“),
    Delta-Protokoll „geaendert = gesamt = 178'117“.
    *Teilweise behoben 2026-11-05 (`6ab94a7`):* Ideen-Hotlists zeigen die Ampel deutsch („hoch“, „mittel“, „tief“ statt
    High/Medium/Low) und Betraege mit Apostroph statt Komma (`printf('%,.0f')` in drei Abfragen von
    `PurchasingDashboardService.cs`); Kennzahlen-Katalog „Live-Probe“ -> „bereit“; Handbuch „001 bis 005“ -> „001 bis
    006“ und alle geaenderten Beschriftungen nachgefuehrt. Offen: Zeitstempel UTC/Ortszeit, ISO-Einheiten, Ideen-Text
    „Bestellrhythmus berechenbar“, Delta-Protokoll-Zaehlung, Datenqualitaet Bestand/Zeitraum.
