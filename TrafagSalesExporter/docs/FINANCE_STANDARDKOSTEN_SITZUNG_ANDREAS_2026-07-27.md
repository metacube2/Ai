# Sitzung Andreas — Standardkosten/Gruppenmarge

Stand: 2026-07-27 (Mitschrift aus Audio-Transkript, Whisper large-v3)

Teilnehmer: Ingo, Andreas. Thema: Gemeinsame Durchsicht `Sales_All`/Gruppenmarge Details
(Bildschirmfreigabe) und Klaerung der Standardkosten-Architektur fuer die drei internen
Trafag-Konzerngesellschaften.

## 1. Befund waehrend der Durchsicht: Supplier Country jetzt meist gefuellt

Andreas hat `Supplier Country` in den aktuellen Gruppenmarge Details geprueft: Feld ist
inzwischen "meistens gefuellt". **Das widerspricht dem dokumentierten Stand vom
2026-07-17** (`docs/FINANCE_GRUPPENMARGE_2026-06-16.md`, Nachtrag 2026-07-17): dort war
belegt, dass CH/AT (`ZSCHWEIZ`) und UK **strukturell gar kein** Supplier-Mapping haben
(kein Feld im Seed-Mapping vorhanden), ES ebenfalls nicht. Zwei moegliche Erklaerungen,
keine verifiziert:
- Andreas hat eine andere/neuere Datei/Spalte angeschaut als die im Code strukturell
  leere `SupplierCountry` (z.B. eine bereits angereicherte Kopie, oder ein anderes Feld
  wurde im Gespraech mit "Supplier Country" gemeint).
- Zwischen 2026-07-17 und heute wurde tatsaechlich ein Mapping ergaenzt, das noch nicht
  in `docs/rag/FINANCE.md`/`FINANCE_GRUPPENMARGE_2026-06-16.md` nachgezogen ist.

**TODO Verifikation:** Vor jeder weiteren Aussage zu Supplier-Feldern den aktuellen
Code-Stand von `DatabaseSeedService` (Mapping fuer `SupplierNumber/Name/Country` je
Quelle) gegen eine frische `Sales_All`-Datei pruefen, nicht die Doku vom 07-17 als
weiterhin gueltig annehmen.

## 2. Neuer, separater Befund: SupplierNumber fehlt bei sehr vielen Zeilen

Filter "kein SupplierNumber" (im Transkript unklar als Zahl protokolliert, zwischen
60'000 und 79'000 Zeilen je nach Zaehlung/Filterstand) — Andreas: das ist "zu viel",
"muesste ja auch immer ein Supplier haben". Ursache im Gespraech nicht geklaert
("kann ich jetzt nicht sagen, was der Grund ist").

**Zusage:** Andreas prueft das auf seiner Seite ("dann kontrolliere ich das"); Ingo
startet parallel eine eigene Analyse anhand der aktuellen `Sales_All`/Finance-Details-Daten
(Pivot-Tabelle, Land-/Namensfilter).

Hinweis zur Datenquelle waehrend der Sitzung: `Sales_All` im SharePoint-Ordner
`Import/Finance/Alle` war vom 23., ein aktueller Lauf war zum Sitzungszeitpunkt gerade
am Laufen (Start ca. 12 Uhr). Reiter mit vollstaendigen Daten inkl. Waehrungsumrechnung
und Filterbeschreibung: **`Finance Details`** (laut Andreas "da hast du alles").

## 3. Entscheid: Standardkosten-Architektur fuer interne Lieferanten — genau 3 Tabellen

Bestaetigt/final (deckt sich mit der bereits im Code vorhandenen Aufteilung
`GroupStandardCostEntities.TrAg/TrIt/TrIn`, s. `Services/GroupMarginSupplierClassifier.cs`):

> "Trafag, das ist ja die drei — weiter wollen wir nicht gehen." (Andreas)

Es werden **genau drei** eigene Konzern-Standardkosten-Tabellen benoetigt:

1. **Trafag AG** (bereits umgesetzt: `GroupStandardCosts`, MBEW-STPRS Bewertungskreis 1100)
2. **Trafag Italien**
3. **Trafag Indien**

Magnetic Sense/GFS wurde im Gespraech kurz angesprochen, aber **explizit nicht** in den
Kreis der Konzern-Standardkosten-Tabellen aufgenommen — deckt sich mit dem Code-Befund
von heute (`GroupMarginSupplierClassifier`: Magnetic Sense zaehlt fuer die
Kunden-Intercompany-Diagnose als intern, aber NICHT als eigene
Konzern-Standardkosten-Quelle fuer die Gruppenmarge).

### Verlinkungslogik (bestaetigt, unveraendert zur bisherigen Annahme)

```
Ist Lieferant = Trafag AG?       -> Standardkosten aus TR-AG-Tabelle
Ist Lieferant = Trafag Italien?  -> Standardkosten aus TR-IT-Tabelle
Ist Lieferant = Trafag Indien?   -> Standardkosten aus TR-IN-Tabelle
Sonst (externer Lieferant)       -> Standardkosten der verkaufenden Landesgesellschaft
                                     (unveraendert, heutiges Q2-Verhalten)
```

### Format der 3 Tabellen — noch offen

Vorschlag Andreas: alle drei Tabellen einmal monatlich aktualisiert ziehen. Eine bereits
verschickte Beispieltabelle war unbrauchbar lang, weil sie die **komplette Zeithistorie**
je Position enthielt statt nur des aktuellen Stands — Andreas' Vorschlag: **nur der
letzte Stand je Material** (aehnlich wie das bestehende WAVWR/STPRS-Prinzip), das waere
"die richtige Tabelle".

**Nicht geklaert:** Ob TR IT/TR IN per **direktem SAP-B1-Zugriff** angebunden werden
(Andreas: "ich weiss nicht, ob man auf SAP B1 direkt zugreifen kann und die
Standardkosten ziehen kann") oder ob Andreas die drei Tabellen manuell/skriptgestuetzt
monatlich liefert ("ich haette wahrscheinlich diese drei Tabellen gezogen, einmal
monatlich aktualisiert"). Das ist eine Aenderung/Praezisierung gegenueber dem bisher
dokumentierten Stand, wonach TR IT ueber SAP B1 (`OITM.PrdStdCst`/`AvgPrice`, beide 0)
und TR IN (vom Entwicklungsrechner nicht erreichbar) als live angebundene Quellen
gedacht waren — siehe `docs/rag/FINANCE.md` Kurzstand 2026-07-15 (Teil 2).

## 4. Datenqualitaets-Caveat: Bewertungsmethode nicht garantiert einheitlich

Andreas: Standardkosten koennten sich zwischen den drei Gesellschaften unterscheiden
(Wechselkurs-/Bewertungseffekte). Vorgabe ist konzernweit **Moving Average**, aber
"es halten sich nicht alle daran" — manche Gesellschaften nutzen (noch) LIFO o.ae.,
Umstellung dauert laut Andreas "noch ein paar Jahre". **Bei Abweichungen zwischen den
drei Tabellen also zunaechst Bewertungsmethode pruefen, bevor ein Datenfehler vermutet
wird.**

## 5. Aktionspunkte

| Wer | Was | Bis wann |
| --- | --- | --- |
| Ingo | SupplierNumber-Luecke analysieren (Ursache, warum ~60-79 Tsd. Zeilen ohne SupplierNumber) | diese Woche (Sync-Termin) |
| Ingo | Verlinkungslogik Lieferant -> Standardkosten-Tabelle (TR AG/IT/IN, sonst lokale Kosten) bauen | diese Woche zugesagt — **von Andreas als "sehr optimistisch" eingestuft**, keine feste Deadline |
| Andreas | Prueft SupplierNumber-Luecke auf seiner Seite | offen |
| Andreas | Liefert 3 Standardkosten-Tabellen (TR AG/IT/IN), Format/Update-Weg noch offen (SAP-B1-Direktzugriff vs. monatlicher Andreas-Export), nur letzter Stand je Material | monatlich, sobald Format geklaert |
| Beide | Sync-Call | Ende dieser Woche |
| ~~Ingo~~ | ~~Paola (TR IT) kontaktieren~~ | **erledigt 2026-07-28** — Antwort erhalten, s. Abschnitt 5d |
| Paola (TR IT) | Technische Beurteilung mit SAP-Team: rechnet SAP den Durchschnittspreis nach Umstellung automatisch fort, oder braucht es eine einmalige Bewertungsaktion fuer den Bestand? | **Ende August 2026** (B1-Upgrade Go-Live 2026-08-03, danach 2 Wochen Ferien) |
| Ingo/Andreas | Paola zurueckmelden, ob Ende August passt (sie hat explizit danach gefragt) | kurzfristig |
| Ingo | NACHSORGE B1-Upgrade 2026-08-03: Importlaeufe FR/IT/US/IN pruefen, `StandardCost`-Fuellgrad und `EvalSystem`-Verteilung neu erheben | ab 2026-08-03 |
| Ingo | TR-IT-Kostenbasis ueber `INV1.StockPrice` (Belegebene) technisch umsetzen (Andreas hat freigegeben) | offen, Teil des zugesagten Wochen-Scopes |
| ~~Andreas~~ | ~~Standardkosten-Tabelle Magnetic Sense~~ | **erledigt/entfaellt** — Andreas bestaetigt: keine Daten noetig |

## 5c. Antwort Andreas per Teams (2026-07-27, Nachmittag)

Andreas hat auf die per Teams gestellten Fragen (s. 5b Befund 6) geantwortet:

- **Entscheid (Frage 1 beantwortet):** Der vorgeschlagene Weg — TR ITs eigene
  Verkaufszeilen-Kosten (`INV1.StockPrice`) als Kostenbasis — ist fuer den ersten Schritt
  im „Data Lake" freigegeben. Wortlaut: „Die aus deiner Sicht einfachste Loesung wuerde
  ich im ersten Schritt umsetzen. Eine zusaetzlich kalkulierte Groesse benoetigen wir
  vorerst nicht." Damit ist Befund 6 (Abschnitt 5b) freigegeben zur Umsetzung — Frage 2
  (welcher Stand je Material: letzter Verkauf/Durchschnitt/Stichtag) und Frage 3
  (Materialien ohne eigenen TR-IT-Verkauf) sind damit noch NICHT explizit entschieden,
  nur implizit als Detail des „einfachsten Wegs" mitgemeint.
- **Magnetic Sense endgueltig geklaert:** „Fuer Magnetic Sense benoetigen wir aus meiner
  Sicht keine Daten." Bestaetigt den Datenbefund aus 5b Befund 5 (Magnetic Sense ist nie
  Lieferant, nur Kunde) — keine vierte Konzern-Standardkosten-Tabelle noetig.
- **NEUER Auftrag an Ingo:** Paola (TR IT) direkt kontaktieren und das Problem aus
  IT-/Systemsicht schildern. Erklaergrafik dafuer (englisch, fuer Paola aufbereitet):
  `docs/TRIT_B1_VALUATION_EXPLAINED_2026-07-28.svg`. Sie soll pruefen:
  1. Welche Umstellung in SAP B1 noetig ist, damit Italien konzernweit auf
     **Moving-Average-Bewertung** wechselt (aktuell `EvalSystem` bei 97.8 % der Artikel
     auf Chargenbewertung `'B'`, nur 1.4 % auf Moving Average `'A'` — s. 5b Befund 6).
  2. Ob dafuer ein **einmaliger Cost Run** (Bestandsneubewertung in B1) erforderlich ist.
  Andreas bietet Unterstuetzung bei der Loesung an.

**Einordnung/Vorsicht fuer die Paola-Anfrage (korrigiert 2026-07-28):** Ein erster Entwurf
argumentierte mit „nur 40.6 % (228/562) der Moving-Average-Artikel haben einen gefuellten
`AvgPrice`". Diese Zahl war durch Nicht-Lagerartikel verzerrt. Auf der richtigen Basis
(aktive Lagerartikel mit `EvalSystem='A'`) sind es **224 von 296 = 75.7 %**. Die
Schlussfolgerung „eine Methodenumstellung allein reicht sicher nicht" ist damit **nicht
mehr belegbar** — rund ein Viertel ohne Wert ist ein Hinweis, kein Beweis. Gegenueber
Paola daher als offene Frage formulieren (Andreas' Cost-Run-Frage weitergeben), nicht als
Befund behaupten. Was gesichert ist: bei den 31'600 chargenbewerteten Artikeln ist
`AvgPrice` ausnahmslos 0.

**Beide Wege laufen parallel, nicht als Alternative:** Kurzfristig (freigegeben) laeuft
die Kostenbasis ueber die eigene Verkaufszeile (`INV1.StockPrice`, 97 % gefuellt,
funktioniert unabhaengig von der Bewertungsmethode). Mittelfristig soll TR IT zusaetzlich
auf saubere Moving-Average-Bewertung umgestellt werden — das betrifft dann eher
Konzern-Reporting-Konsistenz (Andreas nannte in der Sitzung, dass „nicht alle sich an
Moving Average halten") als die unmittelbare Gruppenmarge-Luecke.

## 5d. Antwort Paola (TR IT) per Mail, 2026-07-28

**Kontakt und Abgrenzung (ergaenzt 2026-07-30):** Paola Castagna, `Paola.Castagna@trafag.com`.
Sie ist ab 2026-07-30 zusaetzlich Adressatin einer ZWEITEN, unabhaengigen Bitte — *Preferred
Vendor* am Artikelstamm (`OITM.CardCode`), 939 von 3'280 TR-IT-Artikeln, siehe
`FINANCE_FELDLUECKEN_STANDORTE_2026-07-30.md` Abschnitt 5/6. Die beiden Vorgaenge NICHT in
einer Mail mischen: hier geht es um die Bewertungsmethode mit Zusage Ende August, dort um
Stammdatenpflege. Eine gemischte Mail gefaehrdet den klaren Termin dieses Vorgangs.

### Inhaltliche Rueckmeldung

- **Unsere Analyse wurde bestaetigt** („Thank you for the detailed background, it's very
  clear"). Paola bestaetigt fachlich: Die Umstellung der Bewertungsmethode von Charge auf
  Moving Average fuer die ~31'600 Artikel ist aus ihrer Sicht **technisch als Massenupdate
  machbar**.
- **Die offene Frage ist exakt die, die wir nicht aus den Daten beantworten konnten:**
  Rechnet SAP nach der Umstellung den Durchschnittspreis automatisch fort (z. B. ab der
  naechsten Zugangsbuchung), oder muss der Wert fuer den Bestand ueber eine einmalige
  manuelle Bewertungsaktion gefuellt werden? Genau Andreas' Cost-Run-Frage. Paola will das
  mit ihrem SAP-Technikteam klaeren, bevor sie sich auf naechste Schritte festlegt — weil
  es sowohl den Aufwand als auch die Verlaesslichkeit der resultierenden Kostendaten
  bestimmt. (Bestaetigt rueckblickend, dass die Korrektur in 5c richtig war: die Frage als
  offen zu stellen statt als Befund zu behaupten.)

### Terminierung — Analyse verschiebt sich auf Ende August 2026

Paola ist aktuell voll durch ein **B1-Upgrade ueber ALLE Tochtergesellschaften** gebunden:

| Datum | Ereignis |
| --- | --- |
| 2026-08-02 (Sonntag) | Final Tests B1-Upgrade |
| 2026-08-03 (Montag) | **Go-Live B1-Upgrade, alle Tochtergesellschaften** |
| ca. 2026-08-03 bis 2026-08-17 | Paola in den Ferien (zwei Wochen) |
| Ende August 2026 | Paola liefert die technische Beurteilung |

Paola haelt es ausdruecklich fuer **nicht ratsam**, die Bewertungsanalyse parallel zum
B1-Upgrade zu starten. Sie fragt zurueck, ob Ende August fuer Andreas passt.

**Bewertung fuer uns:** Unproblematisch. Der kurzfristige Reporting-Bedarf ist durch den
von Andreas freigegebenen Belegebenen-Weg (`INV1.StockPrice`) bereits gedeckt und
funktioniert **unabhaengig von der Bewertungsmethode**. Die Moving-Average-Umstellung ist
Konzern-Richtlinienkonformitaet, kein Blocker fuer die Gruppenmarge.

### WICHTIG, separates Thema: B1-Upgrade betrifft unsere taegliche Datenstrecke

Das B1-Upgrade am **2026-08-03** wurde uns vorher nicht angekuendigt, betrifft aber direkt
den Finance-Import: `HanaQueryService` liest fuer **FR (`fr01_p`), IT (`it01_p`), US
(`us01_p`)** — und ueber denselben Adapter auch IN (`TRAFAG_LIVE`) — aus den B1-Tabellen
`OINV`/`INV1`/`ORIN`/`RIN1` plus `OADM`/`OITM`/`OITB`/`OCRD`/`CRD1`/`OOND`/`OSLP`/`ORDR`.

Risiken, die vor/nach dem Go-Live zu pruefen sind:
- **Downtime am Wochenende 2026-08-02/03** → Importlaeufe schlagen fehl; der
  Daten-Heartbeat wird Luecken zeigen (das ist dann erwartungskonform, kein Datenverlust).
- **Schema-/Feldaenderungen** durch die neue B1-Version → gelesene Spalten koennten sich
  aendern oder wegfallen. Besonders zu pruefen: `INV1.StockPrice` (unsere neue
  TR-IT-Kostenquelle!), `OITM.EvalSystem`, `OADM.MainCurncy`.
- **`OITM.EvalSystem`-Verteilung** koennte sich durch das Upgrade selbst veraendern —
  die Zahlen aus 5b/Befund 6 sind ein Stand VOR dem Upgrade.

NACHSORGE (Ingo, ab 2026-08-03): Nach dem Go-Live Importlaeufe fuer FR/IT/US/IN pruefen
(`ExportLogs`/`AppEventLogs`, Daten-Heartbeat), Fuellgrad `StandardCost` je TSC
gegenpruefen, und die `EvalSystem`-Verteilung fuer `it01_p` neu erheben (Werkzeug
`.tmp_tools/HanaQ` ist dafuer vorhanden).

## 5e. Eskalation aus Italien 2026-07-28: Umstellung soll erst 2027 starten

Nach Paolas Antwort (5d) kam eine weitere Mail aus Italien an Andreas — nicht von Paola
selbst, sondern von uebergeordneter Stelle (Bezug auf „Paola's workload", den
Area Sales Manager und den externen B1-Partner **VARONE**). Kernaussage: Man stimmt der
Verschiebung zu und bittet, die neue Bewertungspolitik fuer Trafag Italia **erst ab 2027**
zu starten.

Fuenf genannte Punkte, die vor einer Umstellung von Chargen- auf
Moving-Average-Bewertung geklaert werden sollen:

1. **Kosten VARONE** fuer die Softwareanpassung — nicht im Budget enthalten, muss geprueft werden.
2. **Auswirkung auf Paolas Arbeitslast**, insbesondere ob manuelle Importe noetig werden.
3. **Verifikation des neuen Bestandswerts**, der sich aus der neuen Methode ergibt (Konsistenz/Verlaesslichkeit).
4. **Auswirkung auf die TRIT-Marge** — soll vor dem Vorgehen quantifiziert werden.
5. **Neue interne Prozesse** definieren, gemeinsam mit Area Sales Manager und weiteren Funktionen.

Begruendung fuer 2027: Zeit fuer alle Pruefungen und einen geordneten Uebergang, ohne
laufende Daten und Prozesse zu stoeren. Ausserdem soll die Umstellung nicht mit dem
B1-Upgrade (Go-Live 2026-08-03) ueberlappen.

### Wichtige Einordnung: Das blockiert das Reporting-Projekt NICHT

Der von Andreas am 2026-07-27 freigegebene Weg fuer die TR-IT-Konzernkostenbasis ist
`INV1.StockPrice` auf **Belegebene** — und der funktioniert **vollstaendig unabhaengig von
der Bewertungsmethode** (Fuellgrad 97 % unter der heutigen Chargenbewertung, siehe 5b
Befund 6). Eine Verschiebung der Moving-Average-Umstellung auf 2027 hat daher **keinen
Einfluss** auf:

- die Gruppenmarge-Kostenbasis fuer TR IT,
- den Zeitplan des Data-Lake-Schritts,
- die Datenqualitaet der heutigen TR-IT-Zahlen.

Andreas kann dem 2027-Termin also zustimmen, ohne im Reporting etwas zu verlieren. Das ist
der zentrale Punkt fuer seine Antwort — sonst entsteht der Eindruck, das Projekt haenge an
Italiens Bewertungsmethode.

Wofuer die Moving-Average-Umstellung weiterhin relevant bleibt: **Konzernweite
Vergleichbarkeit der Bewertung** (Andreas' eigener Punkt aus der Sitzung: „standardmaessig
vorgegeben, dass eigentlich nur Moving Average gilt, aber es halten sich nicht alle dran").
Das ist ein Bilanzierungs-/Governance-Thema, kein Reporting-Blocker.

### Zu Punkt 4 (Margenauswirkung) koennen wir beitragen

Die Bitte, die Auswirkung auf die TRIT-Marge vor der Umstellung zu quantifizieren, ist
sachlich berechtigt und mit unseren Daten teilweise beantwortbar: Wir haben je Material die
tatsaechlichen Chargenkosten aus den Belegzeilen (`INV1.StockPrice`, 2'019 von 2'082
Materialien in 2026). Ein Vergleich gegen einen simulierten Moving-Average-Wert waere
moeglich, sobald definiert ist, wie dieser gebildet wuerde.

Abgrenzung, die dabei sauber bleiben muss: Die Bewertungsmethode veraendert die
**Bestandsbewertung und damit die bilanzielle COGS** — das ist nicht identisch mit der
Reporting-Marge im Dashboard. Diese Unterscheidung gehoert in jede Antwort an Italien,
sonst reden beide Seiten von verschiedenen Zahlen.

### Status

Terminentscheid (2027 vs. frueher) liegt bei Andreas/HQ, nicht bei IT. Aus IT-Sicht ist
2027 unproblematisch. Aktionspunkt „Paola kontaktieren" ist damit abgeschlossen; ein
weiterer Aktionspunkt fuer Ingo entsteht daraus **nicht**.

## 5b. Nachpruefung 2026-07-27: „Ist die Standardkosten-Spalte in B1 wirklich 0?"

Anlass: Rueckfrage Ingo, ob der dokumentierte Befund „TR IT hat in SAP B1 keine
Standardkosten" belastbar ist. Nachgeprueft wurde read-only gegen die **lokale
SQLite-Momentaufnahme** `trafag_exporter.db` (Stand 2026-07-02), kein Live-HANA-Zugriff.

### Befund 1: Die Doku-Aussage hatte keine Primaerquelle (inzwischen live nachgeholt, s. Befund 6)

Die Aussage „`OITM.PrdStdCst`/`OITM/OITW.AvgPrice` sind durchgaengig 0" stand an drei
Stellen (`docs/FINANCE_GRUPPENMARGE_2026-06-16.md` Zeile 118, `Models/GroupStandardCost.cs`
Zeile 14, `.tmp_tools/FinanceStdCostTodoExcel/Program.cs` Zeile 20) — alle drei zitierten
aber **denselben einen Doku-Eintrag vom 2026-07-15**, ohne Materialnummern, Zeilenzahlen
oder gespeichertes Abfrageergebnis; in `.tmp_tools/` existierte kein Werkzeug, das
`OITM`/`OITW` fuer `it01_p` jemals abgefragt haette. Eine dreifach zitierte Einzelaussage,
keine dreifache Bestaetigung.

**Nachgeholt am 2026-07-27 (Befund 6): Die Zahl 0 stimmt — die daraus gezogene
Schlussfolgerung „TR IT pflegt keine Kosten" war aber falsch.** Der Wert ist 0, weil
97.8 % der Artikel Serien-/Chargenbewertung nutzen und SAP B1 die Kosten dann gar nicht im
Artikelstamm ablegt. Die Kosten existieren — auf Belegebene, zu 97 % gefuellt. Lehre fuer
die Doku-Praxis: Ein Nullwert ohne notierte Bewertungsmethode/Ursache ist kein Befund,
sondern eine offene Frage.

### Befund 2: Die App liest B1-Kosten gar nicht aus `OITM`

`Services/HanaQueryService.cs` (Zeilen 391/454) mappt `StandardCost` aus
**`INV1.StockPrice` / `RIN1.StockPrice`** — Kostenwert auf der **Belegposition**, nicht aus
dem Artikelstamm. Der 07-15-Check zielte also auf eine andere Ebene als die, die
produktiv verwendet wird. `INV1.StockPrice` ist konzeptionell das B1-Pendant zu
`VBRP-WAVWR` (eingefrorener Kostenwert der Verkaufszeile) — genau das Prinzip, das fuer
CH/AT bereits als fuehrende Quelle gewaehlt wurde.

### Befund 3: `StockPrice` ist bei B1 sehr gut gefuellt (Zahlen, Snapshot 2026-07-02)

| TSC | Zeilen | davon `StandardCost > 0` | Anteil |
| --- | --- | --- | --- |
| TRIT | 18'544 | 17'739 | **95.7 %** |
| TRIN | 6'384 | 6'349 | **99.5 %** |
| TRUS | 1'382 | 1'280 | 92.6 % |
| TRES | 4'977 | 4'017 | 80.7 % |
| TRDE | 4'534 | 3'155 | 69.6 % |
| TRFR | 2'445 | 1'258 | 51.5 % (deckt sich mit dem dokumentierten FR-Stammdatenproblem) |
| TRCH/TRAT | 30'642 | 0 | 0 % — erwartet, Snapshot ist VOR dem WAVWR-Fix vom 2026-07-16 |
| TRUK | 5 | 0 | 0 % — erwartet, Sage liefert keine Kostenspalte |

Auch speziell fuer Zeilen mit **internem** Trafag-Lieferanten ist die Kostenspalte
gefuellt: TRIT/Lieferant „Trafag AG" 6'108 von 6'144 (99.4 %), TRIT/Lieferant „Trafag
Italia S.r.l." 81/81 (100 %), TRFR/„Trafag Italia S.r.l." 41/42 (97.6 %).

**Aussage „SAP B1 pflegt keinen Standardkosten-Wert je Material" ist damit in dieser
Pauschalitaet widerlegt** — sie gilt (unverifiziert) fuer den Artikelstamm, nicht fuer die
Belegposition.

### Befund 4: WICHTIGE Einschraenkung — `StockPrice` loest die Gruppenmarge NICHT automatisch

Kauft z. B. TRFR von Trafag Italia, ist TRFRs `StockPrice` der **IC-Verrechnungspreis**,
den TRFR bezahlt hat — genau der Wert, den die Gruppenmarge laut `Mappe1.xlsx` ersetzen
soll. Ein hoher Fuellgrad bedeutet also NICHT, dass das Gruppenmarge-Problem geloest ist.

**Aber:** Verkauft Trafag Italia dasselbe Material selbst (TRIT-Zeilen), dann ist TRITs
eigener `StockPrice` **Trafag Italias eigene Kostenbasis** — genau die gesuchte Groesse,
und zu 95.7 % vorhanden. Damit gibt es einen moeglichen Weg zur TR-IT-Konzernkostenbasis
**aus bereits importierten Daten**, ohne dass Andreas monatlich eine Tabelle liefern muss.
Offene Punkte dieses Wegs: Abdeckung nur fuer Materialien, die TR IT auch selbst
verkauft; Stichtags-/Periodenabgrenzung; fachliche Freigabe durch Andreas.

### Befund 5: „Magnetic Sense" existiert nicht als Lieferant

Snapshot-Abfrage: `SupplierName LIKE '%MAGNET%'` -> **0 Zeilen**. `CustomerName LIKE
'%MAGNET%'` -> **101 Zeilen, alle TRDE, „Magnetic Sense GmbH"**. Magnetic Sense ist in den
Daten also ausschliesslich **Kunde**, nie Lieferant. Ein `MAGNET*`-Marker auf der
**Lieferantenseite** (Gruppenmarge/Standardkosten-Verlinkung) haette damit aktuell keinerlei
Wirkung; auf der **Kundenseite** ist Magnetic Sense bereits als IC-Marker gesetzt
(`FinanceIntercompanyRule`) und wird fuer DE zusaetzlich per `FinanceRuleEngine`
ausgeschlossen. Deckt sich mit Andreas' Formulierung „die drei" (= TR AG/IT/IN).

### Befund 6: Live-Verifikation gegen `it01_p` — die 0-Aussage stimmt, die Schlussfolgerung war falsch

Read-only Live-Read am 2026-07-27 gegen **BI1-HANA `travtrp0:30015`, Schema `it01_p`**,
User `TRAFAG_ALL` (Werkzeug: `.tmp_tools/HanaQ`, nur `SELECT`, per Guardrail auf
SELECT/WITH begrenzt).

**Der Artikelstamm ist tatsaechlich leer — und zwar strukturell, nicht als Datenluecke:**

| Feld | Ergebnis |
| --- | --- |
| `OITM.PrdStdCst` | **0 bei allen 40'478 Artikeln** — auch bei den 338 Artikeln mit Standardpreis-Bewertung. Feld ist in dieser Installation komplett unbenutzt. |
| `OITM.AvgPrice` | > 0 bei nur **248 von 40'478** Artikeln |
| `OITW.AvgPrice` | **0 bei allen 1'902'456 Lagerzeilen** — komplett unbenutzt |
| `OITM.LstEvlPric` | > 0 bei 215 Artikeln |
| `OITM.ByWh = 'Y'` | nur **2 Artikel** -> lagerplatzbezogene Bewertung ist NICHT die Erklaerung |

**Die Ursache ist die Bewertungsmethode** (`OITM.EvalSystem`), alle Artikel:

| EvalSystem | Artikel | Bedeutung |
| --- | --- | --- |
| `B` | **39'582 (97.8 %)** | Serien-/Chargenbewertung |
| `A` | 562 | Moving Average |
| `S` | 338 | Standardpreis |

**Praezisierung 2026-07-28 (Grundgesamtheit korrigiert):** Die obige Verteilung enthaelt
auch Nicht-Lagerartikel (Dienstleistungen etc.) und inaktive Artikel. Auf der fachlich
richtigen Basis — **aktive Lagerartikel** (`InvntItem = 'Y'` UND `validFor = 'Y'`) — ist
das Bild noch deutlicher:

| EvalSystem | Aktive Lagerartikel | Anteil | davon `AvgPrice` > 0 |
| --- | --- | --- | --- |
| `B` (Charge/Serie) | **31'600** | **99.1 %** | **0** |
| `A` (Moving Average) | 296 | 0.9 % | 224 (75.7 %) |
| `S` (Standardpreis) | 6 | 0.02 % | 0 |

**`EvalSystem = 'B'` = Chargen-/Seriennummernbewertung ist empirisch bestaetigt**, nicht
nur aus der SAP-Doku abgeleitet: Alle 39'582 `B`-Artikel sind chargen- bzw.
seriennummerngefuehrt (`ManBtchNum = 'Y'` bei 39'581, `ManSerNum = 'Y'` bei 1) — 100 %
Korrelation, keine Ausnahme. Umgekehrt gilt das nicht: 215 der `A`-Artikel sind ebenfalls
chargengefuehrt, nutzen aber Moving-Average-Bewertung — Chargenfuehrung und
Chargenbewertung sind also zwei verschiedene Dinge.

Hinweis: Die Artikelzahlen schwanken zwischen zwei Abfragen um wenige Stueck (39'578 vs.
39'582) — es ist ein Produktivsystem, in dem laufend Artikel angelegt werden. Fuer
Aussagen immer runden/„ca." verwenden.

Bei **Serien-/Chargenbewertung** fuehrt SAP B1 die Kosten **je Charge/Seriennummer**, nicht
im Artikelstamm. `AvgPrice`/`PrdStdCst` = 0 ist damit **architektonisch erwartungskonform**
und wird sich auch nie fuellen. Andreas' Annahme „konzernweit Moving Average" trifft fuer
TR IT also nicht zu — 97.8 % laufen ueber Chargenbewertung.

**Konsequenz (wichtig fuer den Freitagstermin):** Eine „TR-IT-Standardkostentabelle aus dem
B1-Artikelstamm" kann es **nicht geben** — nicht weil sie ungepflegt ist, sondern weil die
Bewertungsmethode dort keine Werte ablegt. Ein monatlicher Export dieser Felder wuerde
dauerhaft Nullen liefern.

**Aber die Kosten sind da — auf Belegebene.** Fuer 2026 verkaufte Materialien:

| Kennzahl | Wert |
| --- | --- |
| Verkaufte Materialien (2026, `CANCELED='N'`) | 2'082 |
| davon mit `INV1.StockPrice` > 0 | **2'019 (97.0 %)** |
| davon mit `OITM.AvgPrice` > 0 | 40 (1.9 %) |
| davon mit `OITM.PrdStdCst` > 0 | **0** |
| davon mit `OITM.LstEvlPric` > 0 | 23 (1.1 %) |

Beispiel Artikel `56746`: 86 Fakturapositionen, alle 86 mit `StockPrice`, Durchschnitt
`27.91 EUR` — waehrend der Artikelstamm `AvgPrice/PrdStdCst/LstEvlPric` alle auf 0 zeigt.
(Die 1'050 Positionen mit `ItemCode = NULL` sind Text-/Servicezeilen ohne Material,
erwartungskonform ohne Kosten.)

**Damit ist der gangbare Weg fuer die TR-IT-Konzernkostenbasis:** Trafag Italias eigene
Verkaufszeilen (`INV1.StockPrice` aus TRIT-Zeilen) als Kostenquelle je Material verwenden —
dasselbe Prinzip wie `VBRP-WAVWR` bei CH/AT, mit 97 % Abdeckung, aus bereits taeglich
importierten Daten. Kein Datenlieferungsprozess von Andreas notwendig.

Offene Punkte dieses Wegs (fachlich, an Andreas):
- Ist der B1-Bestandswert (= tatsaechliche COGS der Charge) fachlich als „Konzern-Herstellkosten" akzeptiert, oder braucht es eine kalkulierte Groesse?
- Abdeckung nur fuer Materialien, die TR IT auch selbst verkauft; fuer rein weitergelieferte Materialien fehlt der Wert.
- Periodenabgrenzung: welcher Stand gilt (letzter Verkauf je Material, Jahresdurchschnitt, o.a.)?
- Alternative, noch nicht geprueft: Chargen-/Bewertungsledger (`OINM`/`OIVL`) fuer einen echten Stichtagswert je Material statt Belegdurchschnitt.

### Befund 7: TR IN (Indien) — Live-Check am 2026-07-28 erneut am Netzwerk gescheitert

Derselbe Test wurde fuer TR IN (`TRAFAG_LIVE`, Host `20.197.20.60:30015`, User
`TRAFAGCONTROLS`) am 2026-07-28 versucht und schlug mit Verbindungs-Timeout fehl:

```text
Connection failed (RTE:[89006] System call 'connect' failed, rc=10060 ...)
{20.197.20.60:30015}
```

Damit ist der bereits am 2026-07-15 dokumentierte Zustand bestaetigt: Die Indien-Quelle ist
vom Entwicklungsrechner aus **nicht erreichbar** (VPN/Firewall). Der Produktivserver
erreicht sie dagegen taeglich fuer den normalen Import — es ist kein Quellsystem-Problem.

**Fuer die Umsetzung ist das trotzdem kein Blocker.** Aus dem lokalen Snapshot:

| Kennzahl | TR IN | TR IT (Vergleich) |
| --- | --- | --- |
| Zeilen gesamt | 6'384 | 18'544 |
| davon `StandardCost` > 0 | **6'349 (99.5 %)** | 17'739 (95.7 %) |
| intern „Trafag AG" als Lieferant, mit Kosten | 592/597 (99.2 %) | 6'108/6'144 (99.4 %) |

Indien ist auf Belegebene sogar besser gefuellt als Italien. Da der freigegebene Ansatz
genau dieser Belegebenen-Weg ist, braucht die Umsetzung fuer TR IN **keinen
Artikelstamm-Check**. Ein `EvalSystem`-Check waere nur noetig, wenn man TR IN analog zu
Paola/TR IT auf Moving-Average-Bewertung ansprechen will — dann braucht es vorher
Netzwerkzugang.

## 6. Offene Fachfragen

- Widerspruch Supplier-Country-Fuellgrad (s. Abschnitt 1) — Ursache noch nicht verifiziert.
- ~~Technischer Anbindungsweg TR-IT-/TR-IN-Standardkosten~~ — **entschieden 2026-07-27**:
  Belegebene (`INV1.StockPrice`), kein Extract von Andreas noetig (s. 5c).
- Ob lokale Standardkosten "ueberall vorhanden" sind (Fallback-Pfad fuer externe Lieferanten), von Andreas noch zu pruefen.
- Welcher Stand je Material gilt (letzter Verkauf / Durchschnitt / Stichtag) — von Andreas
  nur implizit als Teil des „einfachsten Wegs" mitgemeint, nicht explizit entschieden.
- Materialien, die TR IT/TR IN nur weiterliefern aber nie selbst verkaufen, haben keinen
  eigenen Kostenwert — Behandlung offen.
- Antwort an Paola, ob Ende August 2026 fuer Andreas passt (sie hat explizit gefragt) —
  steht noch aus, Stand 2026-07-28.

## 7. Offene Umsetzungsschritte (Code)

Stand 2026-07-28, alles noch NICHT umgesetzt:

1. **`GroupStandardCostAreas.ByEntity`/`CurrencyByEntity`** (`Services/GroupMarginSupplierClassifier.cs`
   Zeilen 113-124) enthalten nur `TrAg`. `ResolveDeliveringEntity` erkennt TR IT/TR IN korrekt
   am Namen, aber der anschliessende `TryGetValue` schlaegt fehl -> die Zeile faellt **still**
   auf die lokale Kostenbasis zurueck. Fuer den freigegebenen Weg braucht es hier Eintraege
   plus eine Kostenquelle je Entitaet.
2. **Kostenquelle TR IT/TR IN aus Belegzeilen ableiten**: `INV1.StockPrice` je Material aus
   den eigenen TRIT-/TRIN-Zeilen aggregieren (analog `SapGatewayDataSourceAdapter.PersistGroupStandardCostsAsync`,
   die aktuell nur TR AG befuellt). Offener Fachpunkt: welcher Stand je Material (s. Abschnitt 6).
3. **Kommentar-Korrektur** in `Models/GroupStandardCost.cs` Zeilen 12-16 — transportiert noch
   die ueberholte Schlussfolgerung „TR IT hat keinen befuellten Standardkosten-Wert".
4. **SupplierNumber-Luecke analysieren** (~60-79 Tsd. Zeilen ohne Supplier, Ursache ungeklaert,
   Zusage aus der Sitzung, s. Abschnitt 2/5).
5. **NACHSORGE B1-Upgrade ab 2026-08-03** (s. Abschnitt 5d).

## 8. Anhang: Korrespondenz im Wortlaut

Zur Nachvollziehbarkeit, weil die fachlichen Entscheide daraus abgeleitet sind.

### Andreas an Ingo, 2026-07-27 (Teams)

> Hallo Ingo
> Vielen Dank für die Abklärung und die Rückmeldung.
> Für den ersten Schritt im Data Lake ist der von dir vorgeschlagene Ansatz für mich
> plausibel. Die aus deiner Sicht einfachste Lösung würde ich im ersten Schritt umsetzen.
> Eine zusätzlich kalkulierte Grösse benötigen wir vorerst nicht.
> Könntest du bitte auch Paola / TR IT kontaktieren und das Problem aus IT Sicht beschreiben?
> Ich kann gerne bei der Lösugn unterstützen. Sie soll prüfen, welche Umstellung in B1
> notwendig ist, damit die konzernweite Vorgabe zur Moving-Average-Bewertung auch in Italien
> umgesetzt wird. Falls dafür ein einmaliger Cost Run erforderlich ist, soll sie dies bitte
> ebenfalls prüfen.
> Für Magnetic Sense benötigen wir aus meiner Sicht keine Daten.
> Vielen Dank nochmals.
> VG, Andreas

### Paola (TR IT) an Ingo, 2026-07-28 (Mail)

> Hi Ingo,
> Thank you for the detailed background, it's very clear.
> Regarding your two questions, I need to review this together with our SAP technical team, as
> it requires a proper technical assessment before I can give you a reliable answer. However,
> at the moment we are fully engaged with the B1 upgrade across all subsidiaries: we will run
> the final tests this coming Sunday, and the upgrade should go live from Monday. Right after
> that, I will be out of office for two weeks of holidays.
> Given this timing, I'm afraid a proper analysis of the valuation method change will most
> likely have to wait until end of August, when I'm back and we have closed out the upgrade
> activities. I don't think it's advisable to start this evaluation in parallel with the B1
> upgrade.
> On the substance of the question, my understanding is that switching the valuation method
> from batch to Moving Average for the ~31,600 items should technically be feasible as a mass
> update. What I need to verify with the technical team is whether, once the method is
> switched, SAP automatically calculates the average cost going forward (e.g. from the next
> incoming transaction), or whether the average cost needs to be populated through a
> manual/one-time valuation exercise for the existing stock. This is exactly the point I want
> to clarify before committing to next steps, since it affects both the effort required and
> the reliability of the resulting cost data.
> I will come back to you with a proper assessment once I'm back from holidays. In the
> meantime, please let me know if end of August works for Andreas
> Thanks for your patience, and happy to jump on a call once I have more clarity from our
> technical team.
> Best regards,
> Paola

Hinweis: Der an Paola gesendete Ausgangstext lag als Entwurf vor (englisch, mit den
korrigierten Zahlen aus 5b/Befund 6 und der Erklaergrafik
`docs/TRIT_B1_VALUATION_EXPLAINED_2026-07-28.svg`); die exakt versendete Fassung ist hier
nicht archiviert.
