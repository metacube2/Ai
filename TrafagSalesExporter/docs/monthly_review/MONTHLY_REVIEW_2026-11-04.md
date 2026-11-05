# Verkauf: Monthly Review Gesellschaften (Auftrag 2026-11-04)

Stand: 2026-11-05. **Phase 1 und Abschnitt 4 (Q-Faelle live aus SAP) gebaut und produktiv seit 2026-11-05 10:33 (`47abb30`, im Browser geprueft)** *(ueberholt: „committet, nicht deployed“)* (Abschnitt
„Umsetzung 2026-11-05“ unten). Der SAP-Abruf der Q-Faelle ist ausgeschaltet, bis der Transport mit `QmMeldungSet` in P76 ist.
*Ueberholt (Stand 2026-11-04 13:10): „Nicht gebaut, pausiert. Nur Auftrag, Entscheide, Spezifikation und Befunde; kein Code, kein Deploy.“*

## Auftrag und Entscheide

Ingo am 2026-11-04: „kannst du eine zusaetzliche Maske im Verkauf einbauen? Hier der Draft. maske.html“.
Entscheide (AskUserQuestion, 04.11.):

| Frage | Entscheid |
| --- | --- |
| Statische Kopie oder Nachbau? | **Gleich live nachbauen** (Blazor, Zahlen aus den vorhandenen Diensten statt aus dem Datenblock der Datei) |
| Sichtbarkeit | **Wie Verkauf: alle** (ohne Passwort, wie der Reiter Verkauf seit 2026-10-02) |

Folgt auf `docs/VERKAUF_2026-10-02.md`. Dort steht „Marge nicht im Reiter Verkauf“; fuer die Monthly Review gilt das nicht mehr,
sie zeigt Margen je Gesellschaft (Phase 2) aus der Gruppenmarge.

## Unterlagen

| Datei | Inhalt |
| --- | --- |
| `docs/monthly_review/maske_entwurf_2026-11-04.html` | Ingos Entwurf, Kopie von `maske.html` aus der Repo-Wurzel (204 KB, 3'164 Zeilen, Datenblock Stand Oktober 2026) |
| `docs/monthly_review/MASKE_SPEZIFIKATION_2026-11-04.md` | Vollstaendige Spezifikation des Entwurfs (742 Zeilen): Datenmodell, drei Ansichten `review`/`gaps`/`pbi`, Berechnungen mit Erwartungswerten, Diagramme, alle deutschen Texte, Abschnitt 6 mit hart codierten Annahmen und Fehlern des Entwurfs |

## Geplanter Bau (drei Phasen)

1. **Phase 1, live aus dem internen BI:** neuer Unterreiter im Verkauf (Route unter `verkauf/`, Eintrag in
   `Components/Sales/SalesTabs.razor`), eigener CSS-Block `mrv-`, Ansicht je Gesellschaft und Gruppe aus
   `SalesAnalytics.Monthly`, `.Forecast`, `.Customers`, `.Movements` und `ControllingAnalytics.Projection`.
   Vorgesehene Dateien laut Koordination: `Components/Sales/MonthlyReview/`, `Services/Sales/MonthlyReview*.cs`,
   Tests `TrafagSalesExporter.Tests/MonthlyReview*`, Uebersetzungen.
2. **Phase 2, Margen live:** DB je Gesellschaft aus `GroupMarginCalculator`.
3. **Phase 3, Quellen ausserhalb des BI:** TSC-Gesellschaften, Power BI (Sales Cockpit, RMA), Budget, Auftragseingang
   und die Lueckenliste „gaps“ zunaechst aus einer manuell gepflegten Datei.
   *Ueberholt am 2026-11-05 fuer die Q-Faelle (RMA): Abschnitt 4 kommt nicht in Phase 3 aus Power BI „RMA Statistic“,
   sondern schon mit Phase 1 live aus SAP (`QmMeldungSet`, Auftrag Ingo 05.11.). Der Rest von Phase 3 bleibt.*

Vor dem Bau Abschnitt 6 der Spezifikation lesen: der Entwurf rechnet mit festen Monaten (Oktober) und hat einzelne
Rechenfehler, die nicht nachgebaut werden duerfen.

## Befund: T&P-Dateien der Tochtergesellschaften (Frage Ingo 04.11.)

Frage: „Sind die T&P-Files der Tochtergesellschaften auch im Data Lake?“ Antwort nach Doku und Code (nur gelesen):

- **Nein, nicht im Dashboard bzw. internen BI.** Verkauf und Finance lesen nur TRCH, TRIN, TRIT, TRUS, TRUK, TRES, TRAT,
  TRDE, TRFR. Fuer die TSC-Gesellschaften TRJP, TRCZ, M/S, M/S A und GFS gibt es keinen Import; GFS kommt nur als fester
  Finance-Vergleichswert 2025 (`DatabaseSeedService`) und in der Erkennung interner Lieferanten (Gruppenmarge) vor.
- Der Entwurf selbst kennzeichnet sie als „Werte aus der TSC-Datei (Excel), nicht im internen BI“, Luecke **G17**
  („Zahlen kommen aus Excel – Anbindung ans BI klaeren“).
- **Offen:** ob Andreas' groesseres Data-Lake-Vorhaben (`docs/SITZUNG_ABSCHLUSSRUNDE_DASHBOARD_2026-11-03.md`) sie schon
  sammelt, und wo die T&P-Dateien liegen (z. B. SharePoint). Mit dem Ablageort liesse sich ein automatischer Import pruefen
  (Phase 3).

## Naechster Schritt

*Ueberholt am 2026-11-05: „Phase 1 bauen, wenn Ingo es wieder aufnimmt. Wochen_Todo Zeile 150.“* Phase 1 ist gebaut
(unten). Offen: Sichtpruefung und Deploy durch die Hauptsitzung, Schalter `MonthlyReview:QmEnabled` nach dem P76-Import von
`QmMeldungSet`, dann die Folgearbeit unten.

## Umsetzung 2026-11-05: Phase 1 und Abschnitt 4 (Q-Faelle aus SAP)

Auftrag Ingo 05.11. (ueber die Hauptsitzung): Phase 1 laut Plan und Abschnitt 4 „Qualitaet · offene Q-Faelle“ gleich mit,
live aus SAP statt aus Power BI. Gebaut in einem Agenten-Worktree auf `feature/supplier-overrides-trit` (Basis `52bf2ce`).

### Was gebaut ist

| Teil | Umsetzung |
| --- | --- |
| Unterreiter | Route `/verkauf/monthly-review`, Gesellschaft ueber `?co=TRCH` (ohne = Gruppe); Eintrag in `SalesTabs.razor` nach „Saison und Prognose“, Menuepunkt `sales-monthly-review` (Seed, Icon `Summarize`); sichtbar fuer alle wie Verkauf |
| Kopf | Berichtsmonat, YTD, Datenstand aus den Daten; Chips Gruppe und je Gesellschaft der Verkaufszeilen; TSC-Chip „Phase 3“ ohne Ziel |
| Abschnitt 1 Gruppe | Kacheln Umsatz Berichtsmonat (Sparkline, Vorjahresmonat), YTD, Prognose Jahresende (statistisch), Kunden mit Umsatz 12 Monate (mehrere Gesellschaften, juengstes Quartal mit bekannten Verlusten), Gap „Budget & Auftragseingang“; Monatsgrafik Berichtsjahr gegen Vorjahr mit Prognosemonaten; Wachstum YTD je Gesellschaft mit Gruppenlinie; Scorecard Gesellschaften (YTD, Vorjahr, Δ mit Divergenzbalken, Monat, Δ, Prognose Jahresende, Prognose 12 Monate, Gruppenmarge „Phase 2“, Q1) mit Gruppenzeile, Zeile oeffnet die Gesellschaft |
| Abschnitt 1 Gesellschaft | Kopfkarte, Kacheln Monat, YTD, Prognose Jahresende, Prognose 12 Monate, Gap Budget; Monatsgrafik; Einordnung (12 Monate, Vorjahr ganz, Anteil am Gruppen-YTD, bester und schwaechster Monat) |
| Abschnitte 2 und 3 | Hinweisfelder „noch nicht angebunden“: Gruppenmarge je Gesellschaft (Phase 2), Pipeline/CRM |
| Abschnitt 4 | Q-Faelle aus SAP, siehe unten |
| Abschnitt 5 | Gruppe: neue und verlorene Kunden je Quartal (6 Quartale, Verluste erst 6 Monate nach Quartalsende), Top 10 Kunden; Gesellschaft: Top 10 Kunden mit Beteiligung |

Rechnung: `Services/Sales/MonthlyReviewAnalytics.cs` auf `SalesDataService` (dieselben Zeilen wie Verkauf: Finance-Regeln, ohne
Konzernkunden, CHF zum Belegdatum). Jahresende = Ist bis Berichtsmonat + Restmonate aus `ControllingAnalytics.Projection`
(Vorjahresmonat × Wachstum YTD, 0.5 bis 2); 12 Monate aus `SalesAnalytics.Forecast`; Kunden aus `SalesAnalytics.Customers`,
Kundenbewegung aus `SalesAnalytics.Movements`. Seite `Components/Sales/MonthlyReview/MonthlyReviewPage.razor` mit Bausteinen
`Mrv*.razor`, CSS-Block `mrv-` in `wwwroot/css/app.css`, 109 neue Texte in `UiTextGeneratedTranslations.cs` (6 Sprachen) und
`UiTextBerneseTranslations.cs`.

### Fehler des Entwurfs, die bewusst nicht nachgebaut sind (Spezifikation Abschnitt 6)

| Nr. | Entwurf | Hier |
| --- | --- | --- |
| 6.1 Nr. 1 bis 3 | feste Indizes und Texte fuer Oktober (YTD 12–21, Prognose 22/23, „Okt. 26“, „Nov./Dez.“, `mmYY` immer 2026) | Berichtsmonat = letzter vollstaendiger Monat (`SalesDataset.ReferenceEnd`), alle Zeitraeume und Spaltenkoepfe daraus; Dezember ohne Prognosemonate, Januar beginnt das neue Jahr (Tests) |
| 6.1 Nr. 4 | Kachel und Diskussionspunkt fest auf „2026 Q1“ | juengstes Quartal mit bekannten Verlusten aus den Daten |
| 6.1 Nr. 9 | Q-Zahlen TRCZ 11 und TRJP 5 fest im Text | alle Q-Zahlen aus den Faellen |
| 6.2 Nr. 14 und 15 | `pct(a, 0)` zeigte „0.0 %“, negative Basis ergab ein sinnloses Vorzeichen | Prozent nur gegen positive Basis, sonst „–“ (`MonthlyReviewAnalytics.Pct`) |
| 6.2 Nr. 16 | Δ YTD aus auf 2 Dezimalen gerundeten Mio-Feldern (bis 0.4 Pp. daneben) | aus den exakten Monatssummen |
| 6.2 Nr. 19 | Churn-Achse mit negativen Zahlen fuer verlorene Kunden | Anzahlen ohne Minuszeichen |
| 6.3 Nr. 23 | Q-Summen aus drei Power-BI-Sichten (131/133/135) passten nicht zusammen | alle Grafiken und Kacheln aus derselben Fallliste |
| LEARNINGS 2026-10-02 | — | Vorjahresmonate vor dem Datenbeginn sind fehlend („–“, Linie unterbrochen), nicht 0 |

Nicht uebernommen, weil hart codiert oder ohne Quelle: Diskussionspunkte (6.1 Nr. 5 bis 10, 6.2 Nr. 18), GFS-Germany-Hinweis
(TRCH-Note), TRUK-/TRDE-Hinweise, Kacheln „Uebergabe an Verkauf“ und „Detail-Analyse CSQ“ (Queues aus Power BI, im SAP-Set
nicht vorhanden).

### Abschnitt 4: Q-Faelle live aus SAP

- Quelle `GET /sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/QmMeldungSet`, **in T76 gebaut am 2026-11-05, Auftrag `T76K912837`
  (nicht freigegeben, Import P76 durch Ingo)**; Feld-Mapping, Definition „offen“ und Belege in `saptasks/qm_meldungen/NOTES.md`
  (Abschnitt „SAP-Seite“ unten). Verbindung wie
  Shopfloor/Logistik live/Einkauf (`ShopfloorSapSync.ResolveConnectionAsync`). Client `Services/Sales/MonthlyReviewQmService.cs`,
  Rechnung `Services/Sales/MonthlyReviewQm.cs`, Seite `MrvQuality.razor` und `MrvCaseList.razor`.
- Konfiguration Abschnitt `MonthlyReview` (`appsettings.json`, Registrierung `builder.Services.AddMonthlyReview(...)`):
  `QmEnabled` (**Vorgabe false**), `QmEntitySet` (`QmMeldungSet`), `QmFilter` (optionaler `$filter`, leer), `QmCacheMinutes` (15),
  `QmErrorCacheSeconds` (60), `QmTimeoutSeconds` (60), `QmNotificationTypes` (`Q1`; leer = alle), `QmExcludeSystemStatus`
  (`MMAB`, `LÖVM`, `NOCO`, `DLFL`; *korrigiert 2026-11-05: vorher `MEAB`/`DLFL`, das Set liefert aber deutsche Kurztexte*), `QmCompanyCustomers` (zusaetzliche Zuordnungen, gehen den Vorgaben vor).
  Einschalten heisst `QmEnabled: true` in der versionierten `appsettings.json` committen und deployen; eine Aenderung nur auf
  dem Server ueberschreibt der naechste Deploy (LEARNINGS 2026-08-10). Listen in `appsettings.json` ergaenzen die Vorgaben
  (der ConfigurationBinder haengt an), sie ersetzen sie nicht: `QmNotificationTypes: ["Q2"]` ergibt Q1 und Q2.
- Ein Abruf fuer alle Betrachter, Erfolg 15 Minuten gemerkt, Fehler 1 Minute (LEARNINGS 2026-10-05). Ausgeschaltet kein
  SAP-Aufruf. Ausgeschaltet, Fehler (mit SAP-Text, 404-Hinweis „Set noch nicht in diesem System oder Gateway-Cache“) oder
  leer: Hinweisfeld „Einzelfall-Liste noch nicht geladen“ bzw. „Keine offenen Q1-Meldungen“ statt Fehler. Die Q-Faelle laden
  nach den Umsatzzahlen und halten die Seite nicht auf.
- Seitenweise mit `$top=5000`/`$skip`; ignoriert das Set `$top` (LEARNINGS 2026-10-15) oder kommt keine neue Meldung, ist
  Schluss; doppelte Meldungsnummern zaehlen einmal. `Menge` (Edm.Decimal) wird als Text oder Zahl gelesen, Datumsfelder
  `JJJJMMTT`, leer oder `00000000` = kein Datum.
- „Offen seit“ = Tage von `Angelegt` bis zum Abrufdatum, rot ueber 90 Tage; Altersklassen ≤ 30, 31–60, 61–90, 91–180, > 180.
  Gruppe: Kacheln offene Q1-Meldungen (mit Ruecksendung offen), Offen seit Ø/Median, laenger als 90 Tage (aeltester Fall);
  Grafiken Status, Alter, Gesellschaft/Kunde, Produktlinie; Tabelle Offen seit je Status; Drilldown mit Filter Gesellschaft,
  Status, Bearbeiter, Suche, Sortierung je Spalte (Leerwerte am Ende), 150 Zeilen plus „weitere“, CSV (UTF-8 mit BOM, `;`,
  Spalten wie Spezifikation 2.2 plus Meldungsart, Systemstatus, Kundennummer, Produktlinie). Gesellschaft: eigene Faelle
  (TRCH = Endkunden und Trafag AG intern), Ø/Median gegen Gruppe, Ruecksendung offen der Gruppe mit eigenem Anteil, Gap
  „Endkunden-Faelle im Markt“, bei TRCH die groessten Endkunden. Scorecard-Spalte Q1 aus denselben Faellen.

**Zuordnung Kunde -> Gesellschaft** (`MonthlyReviewQm.DefaultCompanyCustomers`): zuerst Kundennummer (fuehrende Nullen egal),
dann Namensanfang, sonst „Endkunde (TRCH)“.

| Gesellschaft | Kundennummern | Namensanfang | Beleg |
| --- | --- | --- | --- |
| TRIN | 500731, 301940, 302215, 301873 | trafag controls india | Kundennummern aus `KpiLiefertreueSync.DefaultExclusions` (Liefertreue-Spezifikation Fabio, `docs/OPERATIONS_KPI_2026-10-14.md`); Name aus dem Entwurf |
| TRCZ | 302906, 301497, 24687 | – | `KpiLiefertreueSync.DefaultExclusions` |
| GFS | 19914 | – | `KpiLiefertreueSync.DefaultExclusions` |
| TRAT, TRUK, TRFR, TRIT, TRUS, TRES, TRJP | **keine belegt** | trafag gesellschaft, trafag (uk), trafag s.a.r.l, trafag italia, trafag inc, trafag españa/espana, trafag japan | nur Entwurf (`RMA_CO`, Spezifikation 3.5) |
| TRCH intern | – | trafag ag | Entwurf |
| TRDE | – | trafag gmbh | Entwurf, **ungeprueft** (Spezifikation 6.6) |

**Luecke:** Fuer TRAT, TRUK, TRFR, TRIT, TRUS, TRES, TRDE und TRJP gibt es im Repo keine SAP-Kundennummer; die Zuordnung haengt
am Namen in SAP QM. Keine Nummern erfunden. Belegte Nummern lassen sich ohne Code ueber `MonthlyReview:QmCompanyCustomers`
ergaenzen (`{ "Company": "TRAT", "CustomerNumbers": [ "..." ] }`). Nach dem ersten Abruf in P76 die Verteilung „Gesellschaft /
Kunde“ gegen die Kunden in QM pruefen.

**Annahmen, nach dem ersten Abruf zu pruefen:** das Set liefert nur offene Meldungen (Schutz: Systemstatus `MMAB`/`LÖVM`
faellt weg), `Qmart` heisst fuer Kundenreklamationen `Q1` (in T76 belegt), `Status` ist der Anwenderstatus-Text (Ruecksendung
offen wird am Text „Ruecksendung“/„Rücksendung“ erkannt).

### SAP-Seite (T76, 2026-11-05)

Befund des SAP-Agenten, Einzelheiten und Belege in `saptasks/qm_meldungen/NOTES.md`:

- `T76K912837` (Aufgabe `T76K912838`): Struktur `ZSTR_QM_MELDUNG` (Paket ZPP, 17 Felder), Block in
  `ZCL_ZPOWERBI_EINKAUF_MPC_EXT->DEFINE` und Zweig in `ZCL_ZPOWERBI_EINKAUF_DPC_EXT->GET_ENTITYSET`; zurueckgelesen, nur der
  eigene Block neu, andere Sets laufen weiter. Caches `/IWBEP`, dann `/IWFND` geleert. `$metadata` 200, ganzes Set 313 Zeilen
  in 1,4 s.
- **„Offen“** = `QMART = 'Q1'` (TQ80_T „Kundenreklamation“), `KZLOESCH` leer, in JEST weder I0072 MMAB noch I0076 LÖVM aktiv.
  T76 (Datenkopie bis etwa April 2026): 15'229 Q1, davon 313 offen (205 MOFN, 92 MRST zurueckgestellt, 16 MIAR), darunter
  81 MOFN aus 2019, aelteste offene von 2013. Power BI zeigte in P76 131.
- Mapping: `Fehlerbild` = Kurztext QMTXT (Fehlerpositionen nur bei 99 von 313, meist „zur Analyse“), `Menge` = RKMNG,
  `Ruecklieferung` = RKDAT, `Produktion` = PRODDAT, `Massnahme` = fruehestes QMSM-ERDAT, `Bearbeiter` = Partnerrolle VW,
  Name aus PA0001 (ohne HR-Berechtigungspruefung).
- **Offen:** (1) `Status` bleibt in T76 leer, weil Q1 dort kein Statusschema hat (TQ80-STSMA leer); in P76 lesend pruefen
  (TQ80, JSTO, TJ30T), sonst mit Patrick klaeren, woher Power BI den Status nimmt. (2) Ob MRST und alte MOFN als offen zaehlen,
  entscheidet Patrick; filterbar ueber `Sysstat`/`Angelegt` ohne Transport. (3) `Produktlinie` bewusst leer (Power-BI-Linien
  entsprechen keinem SAP-Feld); Vorschlag: Zuordnung im Dashboard ueber `Matnr`. (4) Namen oder Kuerzel bei Bearbeiter
  (Entscheid Ingo). (5) Freigabe und Import `T76K912837` nach P76, dann `/IWBEP`, `/IWFND` leeren, `QmEnabled: true` committen
  und deployen.

### Tests

`TrafagSalesExporter.Tests/MonthlyReviewAnalyticsTests.cs` (Berichtsmonat, Jahreswechsel, Prozent ohne positive Basis, exakte
YTD, Prognose nach dem Berichtsmonat, Vorjahr vor Datenbeginn, bester/schwaechster Monat, Gruppe = Summe, Kunden) und
`MonthlyReviewQmTests.cs` (Lesen mit Beispiel-JSON, Datum, Zuordnung, Filter, Alter, Kennzahlen, Altersklassen, TRCH,
Liste, CSV, Dienst ausgeschaltet/Erfolg/Fehler/leer mit Cache, Seitenlesen bei ignoriertem `$top`). Ganze Suite: 1443/1443 gruen (Debug, Worktree).
Keine Sichtpruefung im Browser (Agent kann die App lokal nicht starten, LEARNINGS 2026-10-29); sie gehoert zum Deploy. *Nachtrag 2026-11-05: nach dem Deploy 10:33 im Chrome geprueft (Gruppe, Q-Hinweis bei ausgeschaltetem Abruf).*

### Folgearbeit (bewusst offen)

1. Sichtpruefung produktiv nach dem Deploy (Gruppe, eine Gesellschaft, Hinweisfeld Q-Faelle ausgeschaltet).
2. Nach dem P76-Import von `QmMeldungSet`: Caches `/IWBEP` dann `/IWFND` (LEARNINGS 2026-11-02), `QmEnabled` auf `true`
   committen und deployen, Zuordnung und Zahlen gegen RMA Statistic bzw. QM pruefen, belegte Kundennummern nachtragen.
3. Phase 2: Gruppenmarge je Gesellschaft (Abschnitt 2, Scorecard-Spalte) aus `GroupMarginCalculator`.
4. Diskussionspunkte (Abschnitt 6) nur mit Regeln aus Daten; Gap-Register und Ansicht „Analyse Power BI“ des Entwurfs.
5. Phase 3: TSC-Gesellschaften, Budget, Auftragseingang, Pipeline.

