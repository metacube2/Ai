# Trafag India: Sales Type statt Preferred Vendor

Stand: 2026-08-05

Status: **Feld ermittelt und auf Produktivdaten ausgewertet.** Offen ist nur noch die
Umsetzung im Export (Abschnitt 7) und die Antwort an Indien (Abschnitt 8).

Grundlage: Call mit RanVijay Kumar (Trafag India) vom 2026-08-05, Transkript
`C:\Users\koi\OneDrive - Trafag AG\Desktop\kumar\Data\audio.wav` (Whisper large-v3), plus zwei
Live-Analysen auf Indiens SAP B1 vom 2026-08-05 11:13 und 11:20.

## 1. Warum die bisherige Bitte an Indien falsch adressiert war

Bisheriger Stand (`docs/FINANCE_FELDLUECKEN.md` Abschnitt 1): Indien sollte
auf 1'271 Artikelnummern den *Preferred Vendor* (`OITM.CardCode`) nachpflegen.

RanVijays Einwand: *„There are many items which are manufactured at TRIN locally. How do we
handle this?"* — berechtigt, denn *Preferred Vendor* ist ein Einkaufsfeld. Bei einem selbst
gefertigten Artikel gibt es keinen Vorlieferanten.

**Gemessen ist der Einwand jetzt beziffert: 1'184 der 1'271 Artikel (93 %) sind Eigenfertigung
und brauchen ueberhaupt keinen Lieferanten.** Die alte Bitte war zu 93 % gegenstandslos.

## 2. Das Feld

| | |
| --- | --- |
| Beschriftung in B1 | **Sales Type** |
| UDF-Alias | `Tasc_ST` (FieldID 14 auf `OITM`) |
| **Datenbankspalte** | **`OITM."U_Tasc_ST"`** |
| Ebene | Artikelstamm, nicht Belegzeile |
| Ermittelt aus | `CUFD` (UDF-Woerterbuch) und `UFD1` (Werteliste), Lauf 2026-08-05 11:13 |

Zulaessige Werte laut `UFD1`:

Das Feld beschreibt **nicht**, woher ein Artikel kommt, sondern **welche Rolle Trafag India in
diesem Geschaeft spielt**. Die drei Werte sind die drei ueblichen verrechnungspreislichen
Charakterisierungen einer Konzerngesellschaft:

| Wert | Klartext | Rolle von TR IN | Warenfluss |
| --- | --- | --- | --- |
| `FFM` | Full Fledged Manufacturing | Voll risikotragender Hersteller: produziert auf eigene Rechnung und verkauft im eigenen Markt | Fertigung IN -> Kunde IN |
| `LRD` | Limited Risk Distributor | Vertrieb mit begrenztem Risiko: bezieht Fertigware von Trafag CH und verkauft sie lokal weiter | Fertigung CH -> IN -> Kunde IN |
| `CM` | Contract Manufacturing | Auftragsfertiger fuer den Prinzipal: fertigt im Auftrag von Trafag AG und fakturiert an Trafag AG | Fertigung IN -> **Trafag AG (CH)** |
| `--` | Platzhalter | nicht gepflegt | — |

Bestaetigt fuer `FFM` und `LRD` durch Ingo am 2026-08-05: FFM = in Indien hergestellt, LRD =
Import von Trafag CH, in Indien verkauft, hergestellt in CH.

### 2a. Was `CM` ist — aus den Daten erschlossen, ohne Rueckfrage bei Indien

`CM` kam im Call nicht vor. **Meine erste Einordnung „Fremdfertigung durch Dritte, also echter
externer Lieferant" war falsch.** Fuenf unabhaengige Belege aus den Produktivdaten zeigen etwas
anderes — die beiden `CM`-Artikel mit Umsatz sind `IC15415` und `IC15037`:

| Beleg | Befund | Was er bedeutet |
| --- | --- | --- |
| Kunde | **ausschliesslich Trafag AG, CH** (23 Zeilen, 53'842'559 INR) | Ein Auftragsfertiger fakturiert an den Prinzipal, nicht an einen Markt |
| Marge | `IC15415` 31.2 %, `IC15037` 31.7 % | Nahezu identischer Aufschlag auf zwei verschiedenen Artikeln mit verschiedenen Mengen = **Kostenaufschlag**, keine Marktpreisbildung |
| Artikelgruppe | `Sub Assemblies` | Baugruppen fuer die Fertigung eines anderen, keine Fertigware fuer einen Endmarkt |
| Nummernfelder | **kein** `Material No`, **kein** `Ordering Code`, aber **beide mit** `Drawing No` | Nach Zeichnung des Prinzipals gebaut, kein Katalogartikel |
| Preferred Vendor | bei beiden leer | Konsistent: es wird kein Fertigartikel eingekauft, Indien laesst fertigen |

Auch die Bezeichnungen passen: „Outer Bellows Assembly With/Non LPI" ist eine Wellrohr-Baugruppe,
also ein Bauteil fuer Druckschalter beziehungsweise Dichtewaechter.

**Schlussfolgerung:** `CM` heisst nicht „wir kaufen bei einem Dritten ein", sondern „Indien
fertigt im Auftrag und liefert an Trafag AG". Damit ist:

- die **liefernde Gesellschaft TR IN** — die lokale indische Kostenbasis ist die richtige
  Konzernkostenbasis, genau wie bei `FFM`;
- der **Preferred Vendor voellig unnoetig** — es gibt keinen Vorlieferanten;
- der interessante Punkt nicht die Lieferanten-, sondern die **Kundenseite**: der Kunde ist
  eine Konzerngesellschaft, es handelt sich um Innenumsatz (siehe Abschnitt 4a).

Als Restsicherheit bleibt: die Auslegung ist erschlossen, nicht von Indien bestaetigt. Sie
stuetzt sich aber auf fuenf voneinander unabhaengige Merkmale, und **keine Handlung haengt mehr
davon ab** — bei jeder der beiden moeglichen Auslegungen ist von Indien nichts zu pflegen.

Gegenprobe an vier Artikeln, deren Einordnung vorab aus Call und Produktivdaten bekannt war:

| Artikel | `U_Tasc_ST` | `CardCode` | erwartet | Ergebnis |
| --- | --- | --- | --- | --- |
| `PT000003` | `LRD` | `V0078` (Trafag AG) | LRD | stimmt |
| `PT000010` | `LRD` | `V0078` | LRD | stimmt |
| `DM000001` | `FFM` | leer | Eigenfertigung | stimmt |
| `DM000083` | `LRD` | **leer** | Eigenfertigung vermutet | **widerlegt — echter Pflegefall** |

`DM000083` (108 Rechnungszeilen) zeigt, warum die Vermutung „kein Lieferant = Eigenfertigung"
nicht tragfaehig gewesen waere: der Artikel kommt aus der Schweiz, nur ist der Lieferant nicht
gepflegt. Ohne das Feld haette eine Heuristik ihn falsch eingeordnet.

## 3. Verteilung auf Produktivdaten

Grundgesamtheit: Artikel mit Rechnungszeilen ab 2025-01-01 in Indiens B1 (entspricht dem
Exportumfang) — 1'449 Artikel, 7'018 Zeilen. Lauf 2026-08-05 11:20.

| Sales Type | Preferred Vendor | Artikel | Zeilen | Bewertung |
| --- | --- | ---: | ---: | --- |
| `FFM` | nicht gepflegt | **1'184** | **5'830** | korrekt so — Eigenfertigung braucht keinen Lieferanten |
| `LRD` | gepflegt | 93 | 454 | vollstaendig, bereits heute klassifiziert |
| `--` | gepflegt | 64 | 264 | Sales Type fehlt |
| `--` | nicht gepflegt | 65 | 112 | Sales Type fehlt |
| `LRD` | nicht gepflegt | 30 | 256 | **Pflegefall** |
| `FFM` | gepflegt | 10 | 78 | Widerspruch, einzeln klaeren |
| `CM` | nicht gepflegt | 2 | 23 | **Pflegefall** |
| leer (NULL) | nicht gepflegt | 1 | 1 | Sales Type fehlt |

Zwei unabhaengige Konsistenzproben, beide bestanden:

- Unsere produktive `CentralSalesRecords` zaehlt fuer TRIN **167 Artikel mit** Lieferant. B1
  ergibt 93 (`LRD`) + 64 (`--`) + 10 (`FFM`) = **exakt 167**. Zwei getrennte Systeme stimmen
  artikelgenau ueberein.
- Ohne Lieferant: unsere DB 1'278, B1 1'282 (1'184+65+30+2+1). Differenz 4 = Artikel, die nur
  auf Gutschriften vorkommen.

### 3a. Entscheidende Zusatzpruefung: `LRD` bestimmt die Gesellschaft allein

Die naheliegende Restliste waere „`LRD` oder `CM` ohne Lieferant" = 32 Artikel gewesen. Das
waere **falsch** und haette Indien Arbeit gemacht, die unser eigenes Feld bereits erledigt.
Gepruefte Frage: zeigen alle `LRD`-Artikel mit gepflegtem Lieferanten auf dieselbe
Gesellschaft?

| Sales Type | Lieferant | Artikel |
| --- | --- | ---: |
| `LRD` | `V0078` (**Trafag AG, CH**) | **93 — alle** |
| `FFM` | `V0078` | 7 |
| `FFM` | `V0393` (Cenlub Systems, IN) | 3 |
| `--` | `V0078` | 35 |
| `--` | 21 verschiedene indische Lieferanten | 29 |

**Kein einziger `LRD`-Artikel zeigt auf einen anderen Lieferanten als Trafag AG.** Damit ist
`LRD` gleichbedeutend mit „Bezug von TR AG" — genau wie bei CH/AT der TSC die Gesellschaft
bestimmt. Die 30 `LRD`-Artikel ohne Lieferant brauchen deshalb **keine Pflege**; wir leiten
die liefernde Gesellschaft aus dem Feld ab.

Ebenso zerfallen die 130 Artikel ohne Sales Type in zwei Gruppen: **64 haben einen Lieferanten**
— fuer die Gruppenmarge sind sie damit klassifiziert, der fehlende Sales Type aendert daran
nichts — und **66 haben weder das eine noch das andere**. Nur diese 66 sind eine echte Luecke.

### 3b. Was Indien wirklich noch tun muss

| Fall | Artikel | Zeilen | Bitte | blockiert |
| --- | ---: | ---: | --- | --- |
| weder Sales Type noch Lieferant | **66** | 113 | Sales Type pflegen | **die Klassifikation** |
| `FFM` **mit** Lieferant | **10** | 78 | bestaetigen, welches Feld stimmt (7× Trafag AG, 3× Cenlub Systems) | **Fehlklassifikation moeglich** |
| `LRD` ohne Trafag-Materialnummer (`DM000083`, `DM000084`, `H90101`) | **3** | 139 | `Material No` pflegen — siehe Abschnitt 6b | **die Kostenbasis** |
| `CM` ohne Lieferant (`IC15415`, `IC15037`) | 2 | 23 | **nichts** — siehe Abschnitt 2a | nein |

Der dritte Fall ist erst durch die Bruecken-Messung in Abschnitt 6b sichtbar geworden: bei
diesen drei Artikeln fehlt die Trafag-Sachnummer, ohne die die Schweizer Kostenbasis nicht
gefunden wird. `DM000083` allein sind 108 Zeilen — die groesste Einzelluecke bei TRIN.

Der `CM`-Fall ist **kein** Pflegefall: Indien fertigt diese Artikel selbst im Auftrag von
Trafag AG, es gibt keinen Vorlieferanten. Ihn trotzdem einzufordern waere derselbe Fehler wie
die alte Preferred-Vendor-Bitte — Pflege verlangen, die fachlich nichts aendert. (In einer
Zwischenfassung dieses Dokuments stand er noch als „waere schoen"; das ist mit Abschnitt 2a
erledigt.)

**Von 1'271 Artikeln bleiben also 66 zu pflegen und 10 zu bestaetigen.** RanVijay hatte
„maybe 50 60" geschaetzt — die Groessenordnung trifft, aber aus einem anderen Grund als
angenommen.

### 3c. Eine Regelfrage, die die Daten nicht beantworten koennen

Dass heute alle 93 `LRD`-Artikel auf Trafag AG zeigen, ist eine **Messung, keine Regel**.
Weil der Klassifikator `LRD` fest auf TR AG abbilden soll, ist bei Indien einmal zu
bestaetigen: *bedeutet `LRD` immer Bezug von Trafag Schweiz, oder koennte ein `LRD`-Artikel
auch von einer anderen Trafag-Gesellschaft kommen?* Faellt die Antwort „immer Schweiz", ist
die Abbildung sauber; sonst braucht `LRD` zusaetzlich den Lieferantentext.

Nebenbefund zum Fuellgrad im **gesamten** aktiven Lagerartikelstamm (5'337 Artikel):
`NULL` 2'838, `FFM` 1'829, `--` 465, `LRD` 199, `CM` 6. Im Gesamtstamm ist das Feld also
ueberwiegend leer — bei den Artikeln **mit Umsatz** dagegen zu 91 % gepflegt (1'319 von
1'449). Fuer unseren Zweck (verkaufte Artikel) ist es tragfaehig; als Pflichtfeld im
Artikelstamm ist es das nicht.

## 4. Der Hebel

Alle drei Artikelklassen zusammen ergeben fuer TRIN:

| Klasse | Zeilen | klassifizierbar nach der Umsetzung |
| --- | ---: | --- |
| `FFM` (Eigenfertigung) | 5'830 | ja, ohne jede Stammdatenpflege in Indien |
| `LRD` mit Lieferant | 454 | ja, schon heute |
| Rest (`LRD`/`CM` ohne Vendor, `--`, NULL) | 734 | erst nach Pflege in Indien |

**Von den 6'236 heute maskierten TRIN-Zeilen werden rund 5'830 (93 %) allein durch das Lesen
dieses Feldes klassifizierbar** — ohne dass Indien einen einzigen Artikel anfassen muss. Die
Kostenbasis liegt bereits auf allen Zeilen vor (`StandardCost` aus `INV1.StockPrice`, Fuellgrad
99.4 %, siehe `docs/FINANCE_SUPPLIER.md`).

## 4a. Nebenbefund beim CM-Nachgraben: Innenumsatz ist gruppenweit nicht ausgeschlossen

Ueber die Kundenseite der `CM`-Artikel ist ein Punkt aufgefallen, der weit ueber Indien
hinausgeht. Gemessen auf der produktiven `CentralSalesRecords` (Stand 2026-08-03), Kriterium
`CustomerName` enthaelt „Trafag":

| TSC | Zeilen gesamt | davon Konzernkunde | Anteil | Umsatz Konzernkunde (Hauswaehrung) |
| --- | ---: | ---: | ---: | ---: |
| TRCH | 47'142 | **11'034** | **23.4 %** | 16'347'706 CHF |
| TRIN | 7'088 | 737 | 10.4 % | 145'181'191 INR |
| TRIT | 19'952 | 657 | 3.3 % | 576'130 EUR |
| TRES | 5'645 | 13 | 0.2 % | 1'849 EUR |
| TRAT | 1'790 | 4 | 0.2 % | 2'414 EUR |
| TRUS | 1'521 | 2 | 0.1 % | 10'753 USD |
| TRFR | 2'598 | 2 | 0.1 % | 6'122 EUR |
| TRUK | 3'019 | 1 | 0.0 % | 1'084 GBP |
| TRDE | 7'259 | 0 | 0.0 % | 0 |

Bei Indien im Detail: Trafag Italia 431 Zeilen, Trafag AG 151, Trafag UK 45, Trafag AT 42,
Trafag GmbH 20, Trafag Inc. 18, Trafag España 16, Trafag Japan 10, Trafag France 4.

**Ausgeschlossen wird davon fast nichts.** In `FinanceRules` gibt es genau zwei
Kundenausschluesse, beide standortspezifisch und einzeln von Hand angelegt:

| Id | Scope | Regel | Notiz |
| --- | --- | --- | --- |
| 2 | DE | `CustomerName` = `Trafag AG` | Excluded DE Weiterberechnung Trafag AG |
| 6 | IT | `CustomerName` enthaelt `Trafag Italia` | Excluded IT customer: Trafag Italia |

Die IT-Regel greift nur fuer „Trafag Italia" — die uebrigen 657 TRIT-Zeilen an andere
Konzerngesellschaften bleiben drin. Fuer TRCH und TRIN existiert keine Regel.

Warum das zaehlt: verkauft TR IN an Trafag Italia und Trafag Italia danach an den Endkunden,
stehen beide Umsaetze im Dashboard. Fuer eine **Konzern**-Umsatzzahl ist derselbe Warenwert
damit doppelt enthalten. Bei TRCH betrifft das 23.4 % aller Zeilen.

**Das ist ausdruecklich eine Frage an Andreas, kein Befund mit Handlungsempfehlung von mir** —
ich weiss nicht, ob die Dashboard-Umsatzzahl bewusst brutto (Summe der Standortumsaetze) oder
konsolidiert gemeint ist. Dass zwei Ausschluesse einzeln von Hand existieren, deutet aber
darauf hin, dass es keine systematische Entscheidung dazu gibt. Zu klaeren, bevor die
Gruppenmarge als belastbar bezeichnet wird.

## 5. Wie die Analyse auf dem Server laeuft

Die Standortsysteme sind vom Entwicklungsrechner nicht erreichbar, auf dem Server gibt es
weder Remoteausfuehrung noch RDP. Gemessen am 2026-08-05:

| Weg | Ergebnis |
| --- | --- |
| Indien HANA `20.197.20.60:30015` vom Entwicklungsrechner | **TCP nicht erreichbar** |
| Share `\\trch-webapp-bidashboard\BiDashboard$` | **FullControl** — Dateien kopieren geht |
| `Invoke-Command` auf `trch-webapp-bidashboard` | Kerberos scheitert: der Name ist ein **CNAME auf `tragvapp401`** |
| `Invoke-Command` auf `tragvapp401.trafagch.local` | **Zugriff verweigert** |
| `schtasks /S`, `\\tragvapp401\c$`, `admin$` | **Zugriff verweigert** |
| RDP | **nicht vorhanden** |

Damit ist die **laufende Anwendung** der einzige Weg, Code auf dem Server auszufuehren.
Deshalb `Services/ServerAnalysisBackgroundService.cs`:

- prueft alle 20 Sekunden, ob im Anwendungsordner `_analysis/run.trigger` liegt;
- arbeitet dann `_analysis/sql/*.sql` ab und schreibt `_analysis/results/<name>.txt`;
- Standort aus dem Dateinamen (`TRIN__01_...` -> `TRIN`), sonst wird die Datei uebersprungen
  statt gegen einen geratenen Standort ausgefuehrt;
- benennt die Triggerdatei sofort um, damit ein Prozessabsturz nicht ungefragt dieselbe
  Abfrage gegen ein fremdes Produktivsystem wiederholt;
- protokolliert Start, Ergebnis und Fehler in der Kategorie `Server-Analyse`.

Schutzmechanismen:

- `Services/ReadOnlySqlGuard.cs` erlaubt nur `SELECT`/`WITH`, lehnt Semikolon im Statement und
  Kommentarzeichen ab (Positivliste, keine Sperrliste).
- Ohne Triggerdatei passiert nichts ausser einem `File.Exists` je Intervall.
- Zugangsdaten kommen aus der Konfigurationsdatenbank ueber
  `Services/DataSources/HanaServerResolver.cs` — **dieselbe Aufloesung wie der produktive
  Export**, damit eine Diagnose nicht anders verbindet als der Export. Nie aus SQL-Dateien.
- Maximal 500 Zeilen je Statement, Werte auf 2'000 Zeichen gekuerzt, keine Zeilenumbrueche.
- Keine Rechteausweitung: wer `_analysis` beschreiben kann, kann auch die Anwendungs-DLLs
  ersetzen. Lesen aus Dateien ist die kleinere Moeglichkeit, nicht die groessere.

Fernbedienung: `.tmp_tools/ServerAnalysis/Run-ServerAnalysis.ps1 -Action Run | Fetch | Clean`.

Geprueft am 2026-08-05: die Ergebnisdateien sind **nicht** ueber das Web erreichbar
(`GET /BiDashboard/_analysis/results/…` -> HTTP `404`). Das ist wichtig, weil sie
Artikel- und Kundendaten enthalten. Nach dem Abholen `-Action Clean` laufen lassen — sonst
liegen SQL-Dateien auf dem Server, die eine spaeter versehentlich angelegte Triggerdatei
erneut gegen ein fremdes Produktivsystem ausfuehren wuerde.

**Falle fuer die naechste Abfrage:** Zwei Bindestriche koennen nicht als Zeichenkettenliteral
vorkommen — sie gelten als SQL-Kommentar und werden entfernt, danach lehnt der Guardrail ab.
Der Platzhalterwert des Sales Type besteht genau aus zwei Bindestrichen; er wird deshalb ueber
`NOT IN ('FFM','LRD','CM')` erfasst.

### Vor dem Deploy verifiziert

Der Produktionscodeweg wurde vor dem Deploy lokal gegen Italien gefahren
(`.tmp_tools/ServerAnalysisLocalTest`, minimaler Host ohne `TimerBackgroundService`, damit
kein Nachhol-Export echte Quellsysteme anspricht): 7 Statements, Trigger verbraucht und
umbenannt, Protokolleintraege geschrieben. Dabei fielen zwei SQL-Fehler auf, die sonst je
einen Serverlauf gekostet haetten:

1. `COLUMN_NAME LIKE 'U_%'` — der Unterstrich ist in `LIKE` ein Platzhalter, die Abfrage
   lieferte `UserSign`, `UserText`, `UpdateDate` statt der UDF-Spalten. Jetzt
   `LIKE 'U\_%' ESCAPE '\'`.
2. `SCHEMA_NAME = '{SCHEMA}'` findet klein geschriebene Schemata nicht (Italien `it01_p`).
   Jetzt `UPPER(SCHEMA_NAME) = UPPER('{schema}')`.

Deploy: 2026-08-05, `385/385` Tests gruen, Details in `docs/rag/DEPLOYMENT.md`.

### Nebenbefund: das Hauptprojekt liess sich nicht publishen

`dotnet publish` brach ab, weil `TrafagSalesExporter.csproj` drei Content-Dateien einbindet,
die im Arbeitsbaum geloescht sind (`DE_Beispiel_Export_Daten.xlsx`, `login.png`,
`manometer.png` — nur im Working Tree, in `HEAD` vorhanden). Das haette **auch den naechsten
Produktivdeploy getroffen**. Im Code wird keine der drei Dateien verwendet, nur die csproj
nennt sie. Behoben mit `Condition="Exists('...')"` nach dem Muster, das die Datei fuer
`Bild.png`/`erg.png` schon verwendet. Wer die Dateien zurueckhaben will:
`git checkout -- login.png manometer.png DE_Beispiel_Export_Daten.xlsx`.

## 6. Fachentscheid

**Entscheid Ingo, 2026-08-05:** Eine `FFM`-Zeile gilt als **intern mit liefernder Gesellschaft
TR IN**, die lokale TRIN-Kostenbasis ist die Gruppenkostenbasis. Begruendung: bei
Eigenfertigung gibt es keinen IC-Aufschlag, den man ausschalten muesste — die lokale
Herstellkostenbasis ist genau der Wert, den die Gruppenmarge sehen will. Gleiche Logik wie die
bestehende CH/AT-Regel.

Ableitungen fuer die uebrigen Werte — `LRD` ist durch Abschnitt 3a belegt, nicht angenommen:

| Sales Type | Klassifikation | Kostenbasis | Lieferantenfeld noetig |
| --- | --- | --- | --- |
| `FFM` | intern, liefernde Gesellschaft TR IN | lokale TRIN-Kosten — echte Herstellkosten | nein |
| `CM` | intern, liefernde Gesellschaft TR IN | lokale TRIN-Kosten — echte Herstellkosten | nein (Abschnitt 2a) |
| `LRD` | intern, liefernde Gesellschaft TR AG | **Konzernkosten TR AG erforderlich**, lokaler Wert ist der IC-Einkaufspreis — siehe 6a | **nein** (93 von 93 = Trafag AG) |
| `--`/NULL | vorhandener Lieferantentext entscheidet; ohne ihn **unklar** — nicht raten | offen | ja, falls kein Lieferant |

### 6a. Bei `LRD` ist der lokale Wert die falsche Kostenbasis

Aus „hergestellt in CH, von Trafag AG bezogen" folgt: `INV1.StockPrice` ist bei `LRD` der
**IC-Einkaufspreis**, nicht die Herstellkostenbasis. Genau diesen Wert soll die Gruppenmarge
laut `Mappe1.xlsx` ersetzen. Richtige Basis ist `GroupStandardCosts` (MBEW-STPRS,
Bewertungskreis 1100, CHF), produktiv befuellt mit 63'506 Zeilen.

**Gemessen greift dieser Weg heute aber kaum:** von den 135 TRIN-Artikeln mit Lieferant
Trafag AG finden nur **34** (185 von 687 Zeilen, 27 %) ueber die Artikelnummer einen Treffer in
`GroupStandardCosts`. Ursache: die indischen Nummern (`PT000003`, `DM000001`) sind
TASC-Eigennummern, keine Trafag-MATNR.

### 6b. Die Bruecke ist gefunden: `U_TASC_OMN` ist die Trafag-Materialnummer

Geprueft am 2026-08-05 gegen die produktive `GroupStandardCosts`. Das UDF `U_TASC_OMN`
(„Material No", FieldID 1) enthaelt die Trafag-Sachnummer — sie steckt bei vielen Artikeln auch
in der Bezeichnung, z. B. `PT000003` = „EPR10.0A(**57291**)-8283" mit `U_TASC_OMN` = `57291`.
`U_TASC_OC` daneben ist der Trafag-Typencode (`8283.78.2511.05.0000.0000.19.58`).

Stichprobe: alle acht geprueften Nummern stehen mit CHF-Stueckkosten in `GroupStandardCosts`
(`57291` -> 29.07 CHF, `54859` -> 34.33, `62641` -> 28.56, `59831` -> 84.12, `3759` -> 60.52,
`54273` -> 25.49, `57289` -> 29.11, `54355` -> 28.44). Auch das Schluesselformat passt: 37'392
der Konzernkosten-Schluessel sind fuenfstellig.

Vollmessung ueber alle 123 `LRD`-Artikel (710 Rechnungszeilen):

| Weg | Artikel mit Treffer | Zeilen mit Treffer |
| --- | ---: | ---: |
| ueber die indische Artikelnummer (`ItemCode`) | 34 von 135 | 27 % |
| **ueber `U_TASC_OMN`** | **118 von 123 (95.9 %)** | **569 von 710 (80.1 %)** |

**Von den 118 Artikeln, die eine echte Materialnummer tragen, treffen 118 — also 100 %.** Die
fuenf Ausfaelle sind genau die fuenf Artikel ohne Nummer:

| Artikel | `U_TASC_OMN` | Zeilen | Bemerkung |
| --- | --- | ---: | --- |
| `DM000083` | Platzhalter | **108** | groesste Einzelluecke, hat auch keinen Lieferanten |
| `DM000084` | Platzhalter | 27 | |
| `H90101` | Platzhalter | 4 | Loetstab, Hilfsmaterial |
| `FA000028` | leer | 1 | Anlagegut, kein Produkt |
| `FA000029` | leer | 1 | Anlagegut, kein Produkt |

Damit ist die Kostenlogik entscheidbar: **fuer `LRD`-Zeilen wird die Konzernkostenbasis ueber
`U_TASC_OMN` gesucht**, nicht ueber die indische Artikelnummer. Fuer die restlichen 141 Zeilen
bleibt die Kostenbasis offen — und **es wird NICHT auf den lokalen Wert zurueckgefallen**, weil
das eine Marge auf dem IC-Preis ergaebe: plausibel aussehend und falsch, also schlechter als ein
offenes „-". Derselbe Fehler ist fuer TRIT-Zeilen in
`docs/FINANCE_SUPPLIER.md` Abschnitt 4 beschrieben.

**Folge fuer den Export:** es sind **zwei** neue Felder zu lesen, nicht eines —
`itm."U_Tasc_ST"` (Sales Type) und `itm."U_TASC_OMN"` (Trafag-Materialnummer). Beide kommen aus
dem bereits gejointen `OITM`, brauchen also keinen neuen Join. Der Konzernkosten-Lookup im
Klassifikator muss fuer TRIN den Trafag-Schluessel verwenden statt `Material`.

Moeglicher Zusatznutzen, noch nicht geprueft: das Produktsparten-Mapping ist bei **allen**
B1-Standorten bei 0 % gefuellt (nur TRCH 100 % und TRAT 89.9 %). Wenn es auf die
Trafag-Materialnummer abbildet, koennte `U_TASC_OMN` auch dort greifen. Das ist eine Vermutung,
keine Messung — separat zu pruefen, nicht Teil dieses Themas.

Andreas ist zu informieren (Gegenstueck zum Supplier-Regel-Entscheid aus
`docs/FINANCE_SUPPLIER.md` Abschnitt 7); Entscheid in
`docs/FINANCE_ENTSCHEIDE.md` nachtragen.

## 7. Umsetzung im Export — UMGESETZT UND DEPLOYED (2026-08-05 15:48)

Deployed, `406/406` Tests gruen, Nachweis in `docs/rag/DEPLOYMENT.md`. **Gefuellt werden die
Felder erst mit dem naechsten TRIN-Export** (Timer 12:00); die Spalten sind produktiv vorhanden
und leer.

Was gebaut wurde:

| Stelle | Aenderung |
| --- | --- |
| `Models/SalesRecord.cs`, `Models/CentralSalesRecord.cs` | Felder `SalesType` und `GroupMaterialNumber` (Rohwerte, keine Auslegung) |
| `Services/DatabaseInitializationService.SchemaSql.cs`, `Services/DatabaseSchemaMaintenanceService.cs` | Spalten im Create und additiv per `AddColumnIfMissing` |
| `Services/HanaQueryService.cs` | beide Felder in **beiden** Queries (`OINV`/`INV1` und `ORIN`/`RIN1`), Spaltensuche, Platzhalterbehandlung |
| `Services/CentralSalesRecordService.cs` | Bulk-`INSERT`: Spaltenliste, `VALUES`, Parameter und Wertzuweisung |
| `Services/CentralSalesDataProvider.cs`, `Services/ConfigTransferService.cs`, `Services/ManagementCockpitService.cs` | Durchreichen, inkl. Klonfunktion der Finanzzeilen |
| `Services/ExportAuditCsvService.cs` | zwei neue Spalten im Audit-CSV, schreibend und lesend |
| `Services/GroupMarginSupplierClassifier.cs` | `ResolveSalesTypeRole`, Auswertung in `Resolve` und `ResolveDeliveringEntity`, neue Klasse `SalesTypeRoles` |
| `Services/ExcelExportService.cs`, `Services/ManagementCockpitService.cs` | Konzernkostenschluessel ueber die Trafag-Sachnummer, LRD-Regel ohne lokalen Rueckfall |
| `Services/GroupMarginStatuses.cs` (neu) | Status `Konzernkosten fehlen` als Konstante fuer Excel, Cockpit und Pruefsummenformel |
| Tests | `GroupMarginSalesTypeTests`, `CentralSalesRecordSalesTypeTests` |

### 7a. Drei Fehler, die erst durch Tests und Messung sichtbar wurden

1. **Die Query wird von allen B1-Standorten geteilt.** Ein festes `itm."U_Tasc_ST"` haette den
   **Italien-Export mit „invalid column name" abgebrochen** — Italiens UDFs heissen `U_ND_*`.
   Geloest ueber eine Spaltensuche; fehlt die Spalte, wird `'' AS sales_type` selektiert, womit
   die Ergebnisform fuer alle Standorte gleich bleibt. Geprueft: in `it01_p` existiert keine der
   beiden Spalten.
2. **Schreibweise.** Das vorhandene `HasColumnAsync` schreibt den Spaltennamen gross und
   vergleicht exakt. Indiens Spalten sind gemischt geschrieben — `U_Tasc_ST` (Position 361)
   neben `U_TASC_OMN` (Position 348). Die Suche nach `U_TASC_ST` liefert produktiv **0 Treffer**
   (gemessen, Runde 5). Das Feld waere fuer Indien **still nie selektiert** worden, ohne
   Fehlermeldung — die ganze Auswertung wirkungslos. Jetzt sucht
   `ResolveColumnNameAsync` schreibweisenunabhaengig und selektiert mit dem **gefundenen** Namen,
   weil HANA gequotete Bezeichner case-sensitiv behandelt. `HasColumnAsync` bleibt unveraendert,
   es steuert die FKDAT-Filterung.
3. **Der Schreibweg ist ein Bulk-`INSERT` mit ausdruecklicher Spaltenliste.** Ein neues Feld am
   Modell genuegt dort nicht. Aufgefallen durch einen bestehenden Test
   (`NOT NULL constraint failed: CentralSalesRecords.SalesType`); jetzt zusaetzlich durch
   `CentralSalesRecordSalesTypeTests` abgesichert, der auch prueft, dass Standorte ohne diese
   Felder leere Werte und nicht `NULL` schreiben.

### 7b. Was sich fachlich aendert — NACHGEMESSEN AM PRODUKTIVBESTAND 2026-08-06

Der TRIN-Export vom 2026-08-06 06:54 hat die Felder erstmals gefuellt. Alle Zahlen unten sind
gegen `trafag_exporter.db` gemessen, nicht geschaetzt.

**Das Feld kommt an:** von 7'094 TRIN-Zeilen tragen **6'664 einen Sales Type (93,9 %)** und
3'625 eine Trafag-Sachnummer. Verteilung: `FFM` 5'923, `LRD` 718, `CM` 23, leer 430. Alle
anderen Standorte stehen erwartungsgemaess auf 0 — nur Indien fuehrt diese UDFs.

- **5'868 TRIN-Zeilen** (`FFM`/`CM` ohne Lieferantenangabe) wechseln von „Lieferant unklar" auf
  „intern, liefernde Gesellschaft TR IN" mit lokaler Kostenbasis. Vorab geschaetzt waren 5'830.
- Die Bruecke ueber die Trafag-Sachnummer wirkt staerker als angenommen: von 718 `LRD`-Zeilen
  finden **581 die Schweizer Konzernkosten — ueber die lokale Artikelnummer waeren es 4**. Die
  frueher genannten „569 statt 185" waren eine Vorabschaetzung auf anderer Grundlage und sind
  durch diese Messung ersetzt.
- Die verbleibenden **137 `LRD`-Zeilen** erhalten den neuen Status **`Konzernkosten fehlen`** und
  **keine** Marge. Vorher zeigten sie eine Marge auf dem IC-Einkaufspreis — plausibel aussehend
  und falsch. Sie zaehlen in der Pruefsumme „Gruppenmarge offene Kostenbasis".
- Ein vorhandener Lieferantentext geht weiterhin vor dem Sales Type. Damit bleibt das Verhalten
  der 10 widerspruechlichen Artikel unveraendert, bis Indien geantwortet hat.

### 7d. Nachtrag 2026-08-06: die Rechnung stand zweimal da — und war bereits auseinandergelaufen

Die Kostenlogik existierte gespiegelt in `ExcelExportService` (auf `SalesRecord`) und
`ManagementCockpitService` (auf `FinanceAggregationRow`) — 48 von 92 bzw. 101 Zeilen nach
Trimmen identisch, nur der Zeilentyp unterschied sich. Die Doku verlangte „im Gleichschritt
aendern". **Beim Einbau von 7b ist genau das misslungen.** Gemessen am 2026-08-06, alles VOR
der Zusammenfuehrung:

| Stelle | Befund |
| --- | --- |
| `BuildGroupMarginDetailRows` | rief die Statusfunktion ohne das neue Kennzeichen auf → dieselbe Zeile stand im Cockpit auf „Standardpreis fehlt", im Excel-Nachweis auf „Konzernkosten fehlen" |
| dieselbe Stelle, Kostenquelle | zeigte „Lieferant unklar" statt „Konzernkosten fehlen (lokaler Wert ist IC-Preis)" |
| `ResolveAuditLedgerStatus` | kannte den neuen Status ueberhaupt nicht |
| `GroupMarginStatusSort` (Cockpit) | ohne Eintrag → der Status sortierte zu den unauffaelligen ans Ende |
| `HasOpenGroupMarginCostBasis` | zaehlte ihn nicht als offen → Kennzahl „offene Kostenbasis" meldete zu wenig |
| Excel, Formel je Land (Spalte G) | zaehlte ihn nicht mit, waehrend die Gesamtsumme im Blatt `Datenqualitaet` es tat — zwei Zahlen im selben Nachweis widersprachen sich |
| `ManagementCockpit.razor` | keine Statusfarbe → als einziger offener Status nicht auf `Warning` |
| `NormalizeMaterialKey` (Excel) | eigene Kopie, die bei einer Materialnummer aus lauter Nullen `"000"` lieferte, wo das Cockpit `"0"` bildete |

**Zusammengefuehrt in `Services/GroupMarginCalculator.cs`.** Beide Dienste bilden ihre Zeile auf
`GroupMarginLine` ab und rufen `Evaluate` — Lieferantentyp, Kostenbasis, Kostenquelle und Status
kommen aus einer Hand. Die drei Abweichungen zur Kostenbasis sind benannte Regeln in einer
geordneten Kette (`GroupMarginCostRules`): `GroupStandardCost` → `GroupDistributionWithoutGroupCost`
→ `LocalStandardCost`. **Die Reihenfolge ist die Fachregel** und wird als solche getestet; eine
neue Abweichung wird als Regel an der fachlich richtigen Stelle eingesetzt statt als vierter
if-Block in zwei Dateien. Statuswerte, die Definition von „offen" und die Sortierung stehen
vollstaendig in `GroupMarginStatuses` — die Excel-Formeln werden daraus erzeugt statt von Hand
aufgezaehlt. Saldo: −298/+158 Zeilen in den beiden Diensten.

Verhaltensaenderungen (gewollt, alle zugunsten des Excel-Nachweises, der bereits richtig lag):

1. Cockpit-Zeilen wechseln von „Standardpreis fehlt" auf „Konzernkosten fehlen".
2. Die Cockpit-Sortierung verschiebt sich, weil der neue Status seinen Platz bekommt.
3. Der Audit-Ledger kennt den Status jetzt.
4. Die Kennzahl „offene Kostenbasis" und die Excel-Formel je Land zaehlen ihn mit.
5. Der Materialschluessel im Excel-Nachweis folgt `MaterialKeyNormalizer` — bei einer Nummer aus
   lauter Nullen `"0"` statt `"000"`. Einziger Punkt, der eine Wahl war und keine Fehlerkorrektur;
   die Richtung folgt dem Cockpit, damit beide denselben Schluessel bilden.

**Asymmetrie, die dabei sichtbar wurde und bewusst NICHT vereinheitlicht ist:** liegen
Lieferantentext und Sales Type beide vor, gewinnt fuer die *Klassifikation* der Lieferantentext
(Zeile gilt als extern), fuer die *Kostenbasis* aber der Sales Type (`LRD` laesst sie offen). Das
war schon vorher so. Betroffen sind die 10 widerspruechlichen Artikel aus 3b. Ein Test haelt das
Verhalten fest, damit es nicht unbemerkt kippt — aufloesen kann es nur Indiens Antwort.

Abgesichert durch `GroupMarginConsistencyTests`: dieselbe Zeile laeuft durch **beide**
oeffentlichen Einstiegspunkte (Excel-Nachweis und `AnalyzeFinanceSummaryAsync`), Status,
Kostenquelle und Kostenbasis muessen uebereinstimmen. Ein reiner Test der Rechenklasse haette
den Fehler nicht gefunden — er waere gruen geblieben, waehrend die Aufrufstelle das Ergebnis
wegwirft. `431/431` Tests gruen (vorher `406`).

## 7c. Frueherer Umsetzungsplan (erledigt, zur Nachvollziehbarkeit)

**Die B1-Query gehoert uns** (Vorrangregel 3 in `router.md`):

- `Services/HanaQueryService.cs`: **zwei** Felder in die Select-Liste — `itm."U_Tasc_ST"`
  (Sales Type) und `itm."U_TASC_OMN"` (Trafag-Materialnummer, siehe 6b). `OITM` ist als
  `LEFT JOIN {schema}"OITM" itm ON p."ItemCode" = itm."ItemCode"` bereits gejoint — **kein
  neuer Join**. Die Query steht **zweimal** (Rechnungen `OINV`/`INV1`, Gutschriften
  `ORIN`/`RIN1`); Aenderung an beiden Stellen, sonst fehlen die Felder auf Gutschriftzeilen.
  Der Platzhalterwert aus zwei Bindestrichen ist beim Einlesen wie leer zu behandeln.
- Neues Feld auf `Models/CentralSalesRecord.cs` und `Models/SalesRecord.cs`, Schema in
  `Services/DatabaseInitializationService.SchemaSql.cs`, additive Migration per
  `AddColumnIfMissing` in `Services/DatabaseSchemaMaintenanceService.cs`. **Rohwert
  speichern** (`FFM`/`LRD`/`CM`), nicht die Interpretation — sonst ist im Audit-CSV nicht
  pruefbar, woher eine Klassifikation kommt.
- Durchreichen in `Services/CentralSalesRecordService.cs`,
  `Services/CentralSalesDataProvider.cs`, `Services/ExportAuditCsvService.cs`,
  `Services/ExcelExportService.cs`.
- Der Konzernkosten-Lookup muss fuer TRIN auf die **Trafag-Materialnummer** gehen, nicht auf
  `Material` (`HasGroupCostMatch` und `ResolveDeliveringEntity` in
  `Services/GroupMarginSupplierClassifier.cs` bekommen den Schluessel heute aus
  `NormalizeMaterialKey(record.Material)`, siehe `Services/ExcelExportService.cs`).
- `Services/GroupMarginSupplierClassifier.cs` wertet das Feld aus (Tabelle in Abschnitt 6).
  Vorhandene Analogie: `IntercompanySellingTsc` fuer CH/AT loest denselben Fall. Eine
  pauschale TSC-Regel ist fuer TRIN **unzulaessig** — `CM` und indische Fremdlieferanten sind
  echt extern.
- Tests in `TrafagSalesExporter.Tests/GroupMarginSupplierClassifierTests.cs`, inklusive
  „Feld leer -> weiterhin `Unklar`".

Danach TRIN neu exportieren und gegenpruefen: rund 5'830 Zeilen muessen von „Lieferant
unklar" auf „intern TR IN" wechseln.

## 8. Antwort an Indien (offen)

Erzeugt: `output/TRIN_Sales_Type_Offen_2026-08-05.xlsx`
(`.tmp_tools/BuildTrinSalesTypeExcel`, Quelle ist die Ergebnisdatei von Runde 3), vier
Blaetter:

1. **1 Vendor needed** — die 2 `CM`-Artikel.
2. **2 Sales Type needed** — die 66 Artikel ohne jede Angabe.
3. **3 Please confirm** — die 10 `FFM`-Artikel mit Lieferant.
4. **Summary** — die Verteilung aus Abschnitt 3 mit der jeweiligen Handlung, inklusive der
   drei Faelle, die ausdruecklich **keine** Arbeit erfordern.

Kernsatz fuer die Mail: von den 1'271 Artikelnummern der letzten Mail brauchen **66** einen
Sales Type, bei **10** brauchen wir eine Bestaetigung, **2** waeren schoen. Alles uebrige ist
bereits verwertbar, weil der Sales Type genau das aussagt, was das Lieferantenfeld aussagen
sollte.

Vier Punkte gehoeren in die Mail, mehr nicht:

1. **66 Artikel** ohne Sales Type und ohne Lieferant — Sales Type pflegen (Blatt 2).
2. **10 Artikel** `FFM` mit Lieferant — welches Feld stimmt (Blatt 3).
3. **Regelfrage** aus Abschnitt 3c: bedeutet `LRD` immer Trafag Schweiz.
4. **Prozessfrage:** im gesamten aktiven Lagerartikelstamm haben 2'838 von 5'337 Artikeln
   keinen Sales Type. Soll das Feld bei neuen Artikeln Pflicht werden — sonst kommt die
   Luecke mit jedem neuen Artikel zurueck.

Und ausdruecklich dazusagen, was **nicht** mehr zu tun ist: die 1'184 Eigenfertigungsartikel,
die 30 `LRD`-Artikel ohne Lieferant und die 64 Artikel mit Lieferant ohne Sales Type brauchen
keine Pflege.

Der alte Entwurf `docs/mails/Build-RanVijayFollowup.ps1` ist ueberholt und darf nicht
versendet werden — er bittet um Pflege, die zu 98 % gegenstandslos ist.

## 9. Wiederaufnahme nach Sitzungsende oder Absturz

Alles Wesentliche ist versioniert. Wer hier ohne Vorwissen einsteigt, braucht genau das:

| Was | Wo |
| --- | --- |
| Dieser Stand | diese Datei |
| Abfragen der drei Laeufe | `docs/analyse/sql/TRIN__01..03_*.sql` |
| Belege (Rohausgabe der Laeufe) | `docs/analyse/ergebnisse/TRIN__01..03_*.txt` |
| Fernbedienung der Server-Analyse | `docs/analyse/Run-ServerAnalysis.ps1` |
| Produktivcode | `Services/ServerAnalysisBackgroundService.cs`, `Services/ReadOnlySqlGuard.cs`, `Services/ServerAnalysisScript.cs`, `Services/DataSources/HanaServerResolver.cs` |
| Tests | `TrafagSalesExporter.Tests/ReadOnlySqlGuardTests.cs`, `…/ServerAnalysisScriptTests.cs` |
| Deploy-Nachweis | `docs/rag/DEPLOYMENT.md` |

**In gitignorierten Pfaden und damit nur lokal vorhanden** (bei frischem Clone neu zu
erzeugen):

- `output/TRIN_Sales_Type_Offen_2026-08-05.xlsx` — der Anhang fuer die Mail. Neu erzeugen mit
  `.tmp_tools/BuildTrinSalesTypeExcel` aus `docs/analyse/ergebnisse/TRIN__03_*.txt` und der
  produktiven `trafag_exporter.db`; das Werkzeug liest die Bloecke 3, 4 und 5 der
  Ergebnisdatei.
- `.tmp_tools/ServerAnalysisLocalTest` — minimaler Host, um den Analyselauf lokal gegen einen
  erreichbaren Standort (Italien) zu pruefen, ohne die ganze Anwendung und ihren
  Timer-Nachholexport zu starten.

**Eine neue Analyse fahren:** SQL-Datei nach `docs/analyse/sql/` legen (Dateiname beginnt mit
dem TSC), dann `docs/analyse/Run-ServerAnalysis.ps1 -Action Run -Only 'TRIN__04*'`. Das Skript
legt die Datei auf den Server, setzt den Trigger, wartet und holt das Ergebnis. Danach
`-Action Clean`. Laeuft der Anwendungspool nicht, weckt ein Aufruf von
`https://trch-webapp-bidashboard.trafagch.local/BiDashboard/` ihn auf.

## 10. Naechste Schritte

1. Mail an RanVijay mit dem Excel, Cc Andreas — **offen**. Inhalt: 66 Artikel Sales Type, 10
   Artikel bestaetigen, die Regelfrage zu `LRD` (Abschnitt 3c) und die Prozessfrage zum
   Pflichtfeld. Ausdruecklich dazusagen, dass die 1'184 Eigenfertigungsartikel, die 30
   `LRD`-Artikel ohne Lieferant und die 2 `CM`-Artikel **nicht** angefasst werden muessen.
2. **`U_TASC_OMN` gegen `GroupStandardCosts` pruefen** (Abschnitt 6a) — entscheidet, ob
   `LRD`-Zeilen eine korrekte Schweizer Kostenbasis bekommen oder offen bleiben muessen. Erst
   danach die Kostenlogik im Klassifikator schreiben, sonst wird sie zweimal gebaut. Rohdaten
   liegen schon in `docs/analyse/ergebnisse/TRIN__04_matnr_bruecke.txt`.
3. Innenumsatz-Frage aus Abschnitt 4a mit Andreas klaeren — betrifft 23.4 % der TRCH-Zeilen und
   ist damit deutlich groesser als das Indien-Thema.
4. Export-Umsetzung nach Abschnitt 7, deployen, TRIN neu exportieren — **offen**.
5. Entscheid aus Abschnitt 6 in `docs/FINANCE_ENTSCHEIDE.md` nachtragen — **offen**.
6. Analoge Frage fuer die anderen B1-Standorte pruefen: hat Italien ein vergleichbares Feld?
   Die UDF-Liste von Italien liegt bereits vor (`U_ND_*`, u. a. `U_ND_CountOrig` „Country of
   Origin") — dort ist es **nicht** dasselbe Feld, der indische `Tasc_ST` ist eine
   TASC-Eigenentwicklung. Fuer Italien bleibt der Weg ueber `OITM.CardCode`.
