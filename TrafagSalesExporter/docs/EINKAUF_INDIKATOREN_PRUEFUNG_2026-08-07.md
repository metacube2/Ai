# Einkauf-Dashboard: Indikatoren durchgesehen

Stand: 2026-08-07

Status: produktiv deployed und verifiziert am 2026-08-07 08:40 MESZ,
Funktionscommit `eef6374`, `449/449` Tests. Nachweis in Abschnitt 5.

Anlass: Frage von Ingo — sind die Indikatoren der einzelnen Einkauf-Reiter korrekt,
oder „einfach mal da"? Spend und Spend-Aufriss galten als belastbar.

## 1. Ergebnis der Durchsicht

Die Reiter zerfallen in **drei** Gruppen, nicht zwei.

### A — rechnet echt auf echten Daten (nicht angefasst)

Spend, Spend-Aufriss (inkl. Produktgruppe und ABC/XYZ), Offene Bestellungen,
Liefertermin-Risiko, Preisentwicklung, Spend-Konzentration, Datenqualitaet.

Beim Reiter `Offene Bestellungen` sieht die Zeitraumunabhaengigkeit des offenen
Werts wie ein Fehler aus, ist aber Absicht und durch zwei Tests gepinnt
(`PurchasingDashboardServiceTests` `LoadAsync_OpenValue_Is_Period_Independent_...`
und `..._Includes_Future_Schedules_...`).

### B — Logik korrekt, Datenbasis produktiv duenn oder fehlend

Materialdisposition, Materialabhaengigkeit, Dispositionspruefung und
Bestellbedarf laufen produktiv auf **`105` ZLO03-Zeilen mit Disponent**.
Lieferperformance hat kein Ist-Wareneingangsdatum und weist das bereits ehrlich
aus (`Ist-Termin-Abdeckung 0 %`, `Quelle fehlt`).

Diese Reiter sind nicht falsch, sie sind leer. Die Aktion dafuer ist ein
ZLO03-Full-Load, kein Codefix — steht als offener Punkt in
`docs/PURCHASING_PRODUKTGRUPPEN_ABCXYZ_2026-08-06.md` § Noch offen.

### C — zeigte eine erfundene oder falsch beschriftete Zahl

Das war der eigentliche „einfach mal da"-Teil. Sechs Punkte, alle im Code
verifiziert und in diesem Stand behoben (Abschnitt 2).

## 2. Was geaendert wurde

Leitregel ist die, die das Projekt sich selbst gegeben hat: **fehlende
Datenbasis wird sichtbar gemacht, nicht geschaetzt.** Belege dafuer sind der
bestehende Umgang mit dem Ist-Wareneingangsdatum
(`docs/EINKAUF_LOGISTIK_SUPPLY_CHAIN_REITER_2026-08-06.md` § Lieferperformance),
mit `DISPO D5` ohne `DESCR` und mit dem Deckungsbeitrag in Finance, sowie
`docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md`: „Ohne EKBE nicht
serioes berechenbar — nicht naehern/simulieren."

Keine neue Kennzahl, keine neue Datenquelle.

### 2a. Reiter `Lieferanten`, Kachel `Performance Score`

Der Wert war `Purchasing3dBaseRows.Average(x => x.SupplierScore)` — der
Mittelwert ueber **zwoelf fest einprogrammierte Simulationszeilen**
(`Components/Pages/PurchasingDashboard.razor`, Liste `Purchasing3dBaseRows`).
Das ergab eine Konstante, die unabhaengig von SAP, Cachezustand und
Zeitraumfilter immer denselben Prozentwert zeigte. `SupplierKpis` war die
einzige KPI-Liste der Seite ohne Live-Zweig — es gibt keine Datenquelle, auf die
sie umschalten koennte: kein Dienst aggregiert einen Score je Lieferant, der
`SupplyChainAnalysisService` aggregiert je **Material**.

Neu: Wert `-`, Untertitel `Bewertungsdaten (EKBE/QM) nicht angebunden`.
Der alte Untertitel „Simulation bis Bewertungsdaten kommen" ist damit
gegenstandslos und wurde im Katalog durch den neuen Text ersetzt (Abschnitt 3).

`Purchasing3dBaseRows` bleibt bestehen — der Reiter `3D Simulation` ist
ausdruecklich eine Simulation und darf die Zeilen weiter verwenden.

### 2b. Reiter `Lieferanten`, Kachel `Preisindikator`

Der Wert war `FormatChf(_liveState.SpendChfSample)` — der **Gesamt-Spend des
Zeitraums**, unter einem Label und einem Untertitel („Netwr CHF/Stk braucht
EKPO"), die einen Stueckpreis versprechen. Der Reiter zeigte damit zweimal
dieselbe Zahl wie `Spend`.

Neu: mengengewichteter Durchschnitts-Stueckpreis des juengsten Jahres aus
`PriceVarianceChartRows` — dieselbe Reihe, die der Reiter `Preisentwicklung`
zeigt — plus die Veraenderung gegenueber dem Vorjahr, z. B. `CHF 47.32 (+3.2%)`.
Der Preisstand allein mischt Schrauben und Baugruppen und ist als Niveau wenig
wert; erst die Richtung ist eine Aussage. Ohne EKPO-Daten `-`, ohne Vorjahr nur
der Stand.

### 2c. Reiter `Lieferanten`, Kachel `Qualitaet`

Wert war der Literal `"offen"`, jetzt `-` wie die uebrigen Datenluecken der
Anwendung. Der Untertitel („Reklamationsquelle noch nicht angebunden") war
bereits korrekt und bleibt.

### 2d. Idee `Lieferantenrisiko` stand auf `berechenbar`

Der Statuschip wurde gruen und `berechenbar`, sobald EKPO und EKET geladen
waren — obwohl keine Implementierung existiert. Die Nachbareintraege
`Maverick Buying` und `Working Capital` verwenden dafuer korrekt `Konzept`.
Jetzt ebenfalls `Konzept` / `Color.Info`.

### 2e. Reiter `Kontrakte`: KPI und Diagramm waren nicht abstimmbar

`ContractValueSample` filtert seit K4 korrekt auf
`COALESCE(k.Konnr,'') <> ''`, also nur Abrufe zu Rahmenkontrakten.
`CommitmentDetailChartRows` hatte diesen Filter **nicht** — und daraus speisen
sich das Kontraktdiagramm und die Kachel `Top Verpflichtung`. Im selben Reiter
standen damit zwei verschiedene Grundmengen nebeneinander: `Restwert` zaehlte
nur Kontraktabrufe, `Top Verpflichtung` alle offenen Bestellungen.

Neu: dieselbe `Konnr`-Bedingung in der Diagramm-Abfrage. Zusaetzlich entfaellt
der Rueckfall `ContractChartRows = OpenValueChartRows`, wenn keine
Kontraktabrufe gefunden werden — genau dann zeigte das Diagramm alle offenen
Bestellungen unter der Ueberschrift Kontrakte. Ein leeres Diagramm neben dem
Restwert `0` ist die richtige Aussage.

Beim Nachziehen fiel eine zweite Stelle derselben Art auf: die GUI schaltete bei
leerem `ContractChartRows` auf `BuildPurchasingChartRows(x => x.ContractValue)`
um, also auf die **Simulationszeilen**. Ohne Kontraktabrufe haette der Reiter
damit erfundene Balken neben einem korrekten Restwert `0` gezeigt — der Fix im
Dienst waere eine Ebene hoeher wieder aufgehoben worden. Jetzt gilt: sobald EKET
geladen ist, gilt das echte Ergebnis, auch das leere.

Kachel `Faelligkeit` heisst jetzt `Letztes Bestelldatum`: der Wert ist
`MAX(EKKO.Bedat)`, also das juengste Bestelldatum im Cache — kein
Faelligkeitsdatum. Der Untertitel („letztes bekanntes EKKO-Datum") sagte das
bereits, die Ueberschrift widersprach ihm.

### 2f. Gruener Prioritaetsbalken der fuenf Supply-Chain-Reiter

`ApplyFilter` entfernte bei `OnlyActionable` alle `OK`-Zeilen, und **danach**
zaehlte `BuildRiskBuckets` die Balken. Der Schalter `Nur Handlungsbedarf` ist
per Default an. Beim Standardaufruf jedes der fuenf Reiter stand der Balken
`Ohne akuten Hinweis` deshalb garantiert auf `0` — das sah aus wie eine Messung
(„kein Material ist in Ordnung"), war aber der Filter selbst.

Neu getrennt: Suche, Disponent und Produktgruppe grenzen den Umfang ein und
gelten fuer alles. `Nur Handlungsbedarf` wirkt nur noch auf Kennzahlen und
Detailtabelle. Die vier Balken zaehlen damit ueber dieselbe Grundmenge; P1, P2
und P3 aendern sich nicht, weil der Schalter ohnehin nur `OK` entfernt hatte.

### 2g. `Fehlwert CHF` wurde still `0`, wenn Stueckkosten fehlen

`ShortageValueChf = shortage * fact.UnitCost`, und `UnitCost` faellt bei
ungepflegten `Stueckkosten` auf `0`. Ein Material mit echter Fehlmenge, aber
ohne Stueckkosten zeigte `Fehlmenge 500` und `Fehlwert CHF 0`, und **kein
P-Code wies darauf hin** — `ClassifyDisposition` prueft `UnitCost` nicht.

Die richtige Bauform stand direkt daneben: `HasFinalStock` trennt „keine Daten"
von „Bestand null" und erzeugt einen eigenen P3-Hinweis. Auf der Kostenseite
derselben Multiplikation fehlte sie.

Neu: `HasUnitCost` wird analog im SQL mitgefuehrt und bis in die Zeile
durchgereicht. Die Tabelle zeigt statt einer bewerteten `0` ein `-`, und die
Kachel `Fehlwert CHF` nennt im Untertitel, wie viele Materialien mangels
Stueckkosten nicht bewertet sind.

Die Risikoklassifikation wurde **nicht** geaendert: es kommt kein neuer P-Code
dazu. Ein fehlender Stueckkostensatz ist ein Bewertungs-, kein Dispositionsfall.

## 3. Lokalisierung

Ein Katalogschluessel wurde ersetzt, nicht ergaenzt:
`Simulation bis Bewertungsdaten kommen` ->
`Bewertungsdaten (EKBE/QM) nicht angebunden`, in `PurchasingUiTextCatalog`,
in allen sechs Bloecken von `PurchasingUiTextGeneratedTranslations` und in
`PurchasingKlingonOverrides`. Die Katalogzahl bleibt damit unveraendert.

Neu aufgenommen: `Letztes Bestelldatum` in allen sechs Sprachen. Der
Literal-Scan des Lokalisierungstests bildet ausserdem aus benachbarten
Zeichenkettenargumenten Paare; durch den unbedingten `Konzept`-Status in 2d
entsteht das Kunstpaar `see shortages and supplier dependency early` ->
`Konzept`. Es ist nach dem im Repo bereits vorhandenen Muster eingetragen
(vgl. `improve compliance and bundling` in `UiTextGeneratedTranslations`).

Die Balkenbeschriftungen und KPI-Untertitel des `SupplyChainAnalysisService`
laufen nicht ueber den Katalog; dort waren keine Anpassungen noetig.

## 4. Tests

`449/449` gruen (vorher `446`). Drei neue Tests, jeder vor dem Einbau des
zugehoerigen Fixes nachweislich rot:

| Test | Datei | Sichert |
|---|---|---|
| `MissingUnitCost_LeavesShortageValueUnknown_InsteadOfCountingAValuedZero` | `SupplyChainAnalysisServiceTests` | 2g — Gegenstueck zum bestehenden `MissingFinalStock_...` |
| `RiskBuckets_KeepCountingOkRows_EvenWhenOnlyActionableHidesThemFromTheTable` | `SupplyChainAnalysisServiceTests` | 2f, inkl. Gegenprobe, dass Suche/Disponent die Balken sehr wohl eingrenzen |
| `LoadAsync_ContractChart_And_TopCommitment_Use_The_Same_Konnr_Basis_As_The_Kpi` | `PurchasingDashboardServiceTests` | 2e — Gegenstueck zum bestehenden `LoadAsync_ContractValue_Counts_Only_Positions_With_Konnr` |

Gegenprobe durchgefuehrt: mit zurueckgebautem Fix scheitern die beiden
strukturellen Tests (`2` Fehler), mit Fix laufen alle `449` durch.

## 5. Deploy-Nachweis (2026-08-07 08:40 MESZ)

- Funktionscommit `eef6374`, Release-Build und Release-Testlauf `449/449`
  erfolgreich VOR dem Publish, nicht aus dem Debug-Lauf uebernommen.
- `app_offline.htm` vor dem Publish gesetzt und danach auf
  `app_offline.htm.disabled` umbenannt.
- Publish ueber `dotnet publish -c Release -o \\trch-webapp-bidashboard...\BiDashboard$`
  (bewusst nicht ueber `FolderProfile`, weil dessen `DeleteExistingFiles=true`
  die Produktiv-DB und die Sicherungen im selben Verzeichnis treffen wuerde).
- `BiDashboard.dll` `07.08.2026 08:40:33`, `4'293'632` Bytes, SHA256
  `214C51E3D08479847813D49B04ED754D6AE5DA614CF458E806BE4AF256BD093A`;
  lokaler Release-Build und Server bitgleich. Zur Aussagekraft des SHA siehe
  den Hinweis in `docs/rag/DEPLOYMENT.md` — der inhaltliche Nachweis ist die
  Typen-/Literalpruefung unten.
- Wirknachweis in der ausgelieferten DLL: `HasUnitCost`, `ApplyScopeFilter` und
  `LatestAverageUnitPriceLabel` enthalten; die Zeichenketten
  `Bewertungsdaten (EKBE/QM) nicht angebunden`, `Letztes Bestelldatum` und
  `ohne Stueckkosten nicht bewertet` sind im UTF-16-Literalbereich vorhanden,
  `Simulation bis Bewertungsdaten kommen` ist verschwunden. (UTF-8-Suche in der
  DLL findet nur Member-, keine Literalnamen — Literale liegen als UTF-16 im
  `#US`-Heap und muessen byteweise gesucht werden.)
- Produktiv-DB in Laenge und Schreibzeit unveraendert: `339'210'240` Bytes,
  `07.08.2026 08:00:54`, vor und nach dem Deploy identisch.
- HTTPS `200` mit Inhalt, authentifiziert:

  | Route | Bytes | Zeit |
  |---|---:|---:|
  | `/BiDashboard/` | `68'466` | `6.93 s` |
  | `/einkauf/lieferanten` | `101'751` | `85.58 s` kalt, `8.46 s` warm |
  | `/einkauf/kontrakte` | `102'019` | `8.00 s` |
  | `/einkauf/bestellbedarf` | `92'159` | `4.26 s` |
  | `/logistik/materialdisposition` | `81'070` | `0.96 s` |

  Die `85.58 s` sind der erste Aufruf nach dem Prozessstart (Aufbau des
  Einkaufs-Caches); der Warmaufruf derselben Route liegt bei `8.46 s`.

## 6. Ausdruecklich NICHT gemessen

Diese Punkte sind aus der Codedurchsicht als strukturell moeglich
hervorgegangen. Ihre **Auswirkung auf die Produktivzahlen ist nicht gemessen**
und wird hier nicht behauptet; sie sind bewusst nicht mitgeaendert worden.

- **`MAX()`-Deduplizierung mehrfach verwendeter ZLO03-Komponenten**
  (`SupplyChainAnalysisService`, `LoadMaterialFactsAsync`): bei negativen
  Bestaenden waehlt `MAX` den am wenigsten negativen Wert. Weichen die Zeilen
  je Elternmaterial voneinander ab, waere die Fehlmenge untertrieben. Der
  bestehende Deduplizierungstest setzt beide Zeilen auf denselben Wert (`-5`,
  `-5`) und kann das nicht aufdecken. Vor einer Aenderung erst gegen echte
  ZLO03-Daten messen — der Cache traegt heute nur `105` Zeilen mit Disponent.
- **`Menge = 0` in EKPO** fuehrt zu offenem Wert `0` bei offener Menge `> 0`
  (`ChfUnitPriceSql` und die drei gespiegelten Stellen im
  `SupplyChainAnalysisService`). Dass solche Zeilen existieren, zeigt der
  Zaehler `Nullmenge` im Reiter `Datenqualitaet`; die Hoehe ist unbekannt.
- **`MinSpendYear = 2020`** gilt fuer Kaskade und Matrix, nicht fuer
  `SpendChfSample`. Durch den Standardfilter (`Math.Max(2020, Jahr - 6)`)
  verdeckt; sichtbar erst bei einem Von-Datum vor 2020.
- **WKURS-Richtung** bleibt unverifiziert, siehe `docs/rag/PURCHASING.md`
  § Offene Punkte.
- **Notweg ohne Cache**: faellt eine der drei Cachetabellen auf `0` Zeilen,
  rechnen die Kacheln von Spend, Offene Bestellungen, Kontrakte und Lieferanten
  aus `Purchasing3dBaseRows`, und `ContractValueSample = OpenValueSample` lebt
  wieder — der Zustand, den K4 beseitigt hat. Unterschied nur im Untertitel.
  Seit dem Full Load 2026-07-24 nicht erreichbar; im Code ist die Stelle jetzt
  als solche kommentiert.

## 7. Nachgezogene Doku

`docs/EINKAUF_LOGISTIK_SUPPLY_CHAIN_REITER_2026-08-06.md` und
`docs/rag/PURCHASING.md` fuehrten die fuenf neuen Reiter noch als „noch nicht
deployed". Das war taggleich ueberholt: der Deploy lief am 2026-08-06 15:11
MESZ (Commit `01af1b8`), nachgewiesen in `docs/rag/DEPLOYMENT.md`. Beide
Stellen sind korrigiert.

## 8. Offen

- Fachlich zu klaeren mit Marco: braucht der Einkauf den `Performance Score`
  ueberhaupt? Die Frage steht seit 2026-07-08 offen
  (`docs/PURCHASING_DASHBOARD_2026-06-05.md` § Nachtrag 2026-07-08). Solange
  sie offen ist, kostet die Kachel nichts — sie zeigt jetzt `-` statt einer
  Zahl, die keine ist.
- Fuer eine echte Lieferantenbewertung braucht es `EKBE` (Termintreue), eine
  QM-Quelle (Qualitaet) und von Marco die Bewertungsformel der bestehenden
  Lieferantenbewertung (Toleranzklassen/Punkteschema), siehe
  `docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md` § C1.
