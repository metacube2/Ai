# Journal-EntitySet fuer CH/AT: Messung und Bauplan

Stand: 2026-09-11. System `T76/100` (travt762), das ist eine rund sechs Monate alte
Kopie der Produktion.

Zugehoerig: `docs/FINANCE_JOURNAL.md` (Feld-Mapping und Leser),
`saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md` (Werkzeuge und Fallen).

## 1. Warum es das braucht

Der Finance-Journalleser fuer CH/AT ist seit dem 2026-09-08 produktiv. Es fehlt nur die
OData-Quelle: keines der vorhandenen EntitySets taugt als Volljournal.

| EntitySet | warum nicht |
| --- | --- |
| `FinanzdataSchweizOeSet` | Faktura mit `Vbeln`, `Posnr`, `Matnr` — keine Buchhaltung |
| `bkpfSet` | nur Belegkoepfe, ohne `Buzei` kein Journal |
| `bsisSet` | nur **offene** Posten, damit stimmt kein Saldo; ohne `Dmbtr`, `Shkzg`, Kontotext, Profitcenter, Buchungstext, Faelligkeitsdatum |

## 2. Fachliche Vorgabe Andreas, 2026-09-10

> „ch hat viele line items mit zeitbuchungen sind es ueber 5 mio pro jahr. da ch aehnlich
> zu at laufen wird, wuerde ich fuer den ersten entwurf ch weglassen (oder potentiell ohne
> zeitbuchungen umsetzen)"

Umgesetzt: **zuerst nur Oesterreich, Buchungskreis 1200.** Der Pruefreport hat `1200` als
Vorgabe und laesst Belegart `CO` standardmaessig weg.

## 3. Gemessene Grundlagen

Alles rein lesend, ueber SapProbe (RFC) und den Report `ZFIN_JOURNAL_PRUEFUNG`.

### Buchungskreise

| BUKRS | Gesellschaft | Land | Hauswaehrung | Kontenplan |
| --- | --- | --- | --- | --- |
| 1100 | Trafag AG | CH | **CHF** | 1000 |
| 1200 | Trafag Ges.m.b.H. | AT | **EUR** | 1000 |

Zwei verschiedene Hauswaehrungen. Der Leser bildet das ueber `Hwaer` ab.
Bestaetigt aus dem Quelltext von `Z_TRAFAG_DACH_EXPORT`: „Buchungskreise 1100 (CH) und
1200 (AT)".

### DDIC-Felder

Alle 25 benoetigten Felder existieren (`field-exists` je Feld): `BKPF` mit `BUKRS`,
`BELNR`, `GJAHR`, `BUDAT`, `MONAT`, `BLART`, `XBLNR`, `STBLG`, `HWAER`, `WAERS`; `BSEG`
mit `BUZEI`, `HKONT`, `SHKZG`, `DMBTR`, `WRBTR`, `KOSTL`, `PRCTR`, `SGTXT`, `ZFBDT`,
`AUGDT`, `AUGBL`; `SKAT` mit `SAKNR`, `TXT50`, `SPRAS`, `KTOPL`.

### Mengengeruest Oesterreich, 2025 bis 2026, ohne CO-Belege

| | |
| --- | ---: |
| Belegkoepfe | 6'275 |
| Belegpositionen | 17'364 |
| Soll / Haben | 8'608 / 8'756 |

Belegarten: `RV` 1'207, `KR` 1'175, `WL` 1'179, `WA` 1'068, `DZ` 827, `KZ` 306, `SA` 267,
`AB` 110, `PR` 51, `KG` 31, `WE` 21, `RE` 18, `WI` 6, `DA` 4, `KA` 4, `DG` 1.

**Das ist eine handhabbare Menge.** Andreas' Sorge betrifft die Schweiz, nicht Oesterreich.

### Fuellgrade, 17'364 Positionen

| Feld | gefuellt | Anteil | Bedeutung fuer das Dashboard |
| --- | ---: | ---: | --- |
| `WRBTR` | 17'108 | 98,5 % | Betrag in Transaktionswaehrung, unkritisch |
| `SGTXT` | 3'903 | 22,5 % | Buchungstext, oft leer — normal |
| `AUGDT` | 3'844 | **22,1 %** | Ausgleichsdatum |
| `AUGBL` | 3'844 | **22,1 %** | Ausgleichsbeleg, identischer Fuellgrad |
| `ZFBDT` | 3'604 | **20,8 %** | Basis fuer das Faelligkeitsdatum |
| `KOSTL` | 2'360 | 13,6 % | Kostenstelle |
| `ZBD1T` | 2'257 | 13,0 % | Zahlungsziel in Tagen |
| **`PRCTR`** | **0** | **0,0 %** | **Profitcenter, durchgehend leer** |

### Kontotexte

98 bebuchte Sachkonten, **alle 98 mit Text** in `SKAT`, Kontenplan 1000, Sprache `D`.
Kein Textproblem.

## 4. Drei Befunde, die den Bauplan bestimmen

1. **`PRCTR` ist in Oesterreich zu 0 Prozent gefuellt.** Das Feld ist im Leser als
   `Dimension2` vorgesehen. Es bleibt leer, und das ist kein Ladefehler, sondern der
   Quellzustand. Deckt sich mit dem Befund vom 2026-09-08, dass `ProfitCode` und
   `OcrCode2-5` auch bei B1 in FR/IT/US/IN leer sind.
2. **`AUGDT` und `AUGBL` haben denselben Fuellgrad, 22,1 Prozent.** Sie gehoeren zusammen
   und sind konsistent gepflegt. Leer heisst hier **nicht ausgeglichen**, nicht
   ungeladen — genau der Vorbehalt, den Andreas am 2026-09-10 bestaetigt hat.
3. **`Faedt` ist kein Tabellenfeld.** Das Nettofaelligkeitsdatum muss aus `ZFBDT` plus
   Zahlungsbedingung (`ZBD1T`/`ZBD2T`/`ZBD3T`) berechnet werden. Bei nur 20,8 Prozent
   `ZFBDT` bleibt `DueDate` fuer rund vier Fuenftel der Zeilen leer. Das ist zu erwarten,
   weil Faelligkeit nur bei offenen Posten fachlich sinnvoll ist, sollte aber niemanden
   ueberraschen, der die Spalte in `Finance_All` sieht.

## 5. Was das EntitySet liefern muss

Service `ZPOWERBI_EINKAUF_SRV`, Set `FinanzJournalSet` (dann ist keine Konfiguration
noetig, der Leser nimmt diesen Namen als Vorgabe). Jeder andere Name muss in
`Sites.SapEntitySet` fuer `ZSCHWEIZ` eingetragen werden.

**20 Pflichtfelder**, sonst verweigert der Leser mit Klartextmeldung:

```
Bukrs  Belnr  Gjahr  Buzei  Budat  Monat  Blart  Xblnr  Stblg  Hwaer
Waers  Hkont  HkontTxt  Shkzg  Dmbtr  Wrbtr  Kostl  Prctr  Sgtxt  Faedt
```

**Zwei optionale**, die den Ausgleich bringen: `Augdt`, `Augbl`. Fehlen sie, laeuft der
Import und die zwei Spalten bleiben leer.

Vier Punkte, die beim Bauen zaehlen:

1. **Eine Zeile je `Buzei`**, nicht je Beleg — daran scheiterte `bkpfSet`.
2. **Alle Belege, nicht nur offene** — daran scheiterte `bsisSet`.
3. **Kein Filter auf `Bukrs`** im Set; der Leser trennt CH und AT selbst. Fuer den ersten
   Entwurf kann die Datenmenge trotzdem ueber Oesterreich begrenzt werden, siehe
   Abschnitt 2.
4. **`HkontTxt` aus `SKAT`** mit Sprache und Kontenplan.

## 6. Was im System steht

| | |
| --- | --- |
| Report | `ZFIN_JOURNAL_PRUEFUNG`, aktiv |
| Paket | `ZPP` |
| Transportauftrag | **`T76K912530`** „Journal CH/AT: EntitySet fuer Finance Dashboard", **nicht freigegeben** |
| Quelle im Repository | `docs/abap/ZFIN_JOURNAL_PRUEFUNG.abap` |

Der Report ist rein lesend, nur `SELECT` und `WRITE`.

## 6a. Stand der Umsetzung

**Nachgefuehrt am 2026-09-11.** Der Stand vom Vorabend („Redefinitionen offen")
ist damit ueberholt.

| Schritt | Stand |
| --- | --- |
| DDIC-Struktur `ZSTR_FIN_JOURNAL` | **aktiv**, 22 Felder, Paket `ZPP` |
| Pruefreport `ZFIN_JOURNAL_PRUEFUNG` | **aktiv**, gelaufen, Messwerte in Abschnitt 3 |
| Methodenrumpf Modell | `docs/abap/ZFIN_JOURNAL_MPC_DEFINE.abap` |
| Methodenrumpf Daten | `docs/abap/ZFIN_JOURNAL_DPC_GET_ENTITYSET.abap` |
| Redefinition `DEFINE` im MPC_EXT | **aktiv**, ueber RFC gegengelesen |
| Redefinition `/IWBEP/IF_MGW_APPL_SRV_RUNTIME~GET_ENTITYSET` im DPC_EXT | **aktiv**, Include `ZCL_ZPOWERBI_EINKAUF_DPC_EXT==CM01W`, ueber RFC gegengelesen |
| `/IWFND/CACHE_CLEANUP` fuer `ZPOWERBI_EINKAUF_MDL` | gelaufen |
| `$metadata` | **HTTP 200**, `341'032` Zeichen gegenueber `320'752` vorher |
| `FinanzJournalSet`, Originalanfrage des Lesers mit `$orderby`, `$filter` und Paging | **HTTP 200**; monatsweise und mit `$top=20000` rund zwei Minuten je Jahr, siehe 6d bis 6e |
| Werte zeilenweise gegengelesen | **erledigt**, Abschnitt 6c1; ein Fehler gefunden und behoben |
| Gegenprobe bestehende Sets `MAKTSet`, `ZSP_CODESSet` | **HTTP 200**, unveraendert |

Alles auf `T76/100`, Transport **`T76K912530`, nicht freigegeben**.

**Seit 2026-09-30 enthaelt derselbe Transport auch das HR-EntitySet `HrKpiSet`** (Struktur `ZSTR_HR_KPI`, Einschuebe in `DEFINE` und `GET_ENTITYSET`), weil beide Methoden durch diesen Auftrag gesperrt sind; Entscheid Ingo fuer denselben Service. Journal und HR gehen deshalb nur **gemeinsam** nach P76. Gesamtquellen: `docs/abap/ZPOWERBI_EINKAUF_MPC_EXT_DEFINE.abap` und `docs/abap/ZPOWERBI_EINKAUF_DPC_EXT_GET_ENTITYSET.abap`; die Journal-Dateien `ZFIN_JOURNAL_*.abap` zeigen nur noch den Journal-Teil. Details `docs/HR_KPI.md` 8.6. Die App liest
produktiv `travp762`; dort gibt es das EntitySet erst nach Transport.

### Wie geprueft wurde, und warum nicht von aussen

OData von aussen scheitert an der Anmeldung: Basic-Auth liefert `401`
(2026-08-18), `-UseDefaultCredentials` ebenfalls `401` (2026-09-11). Gemessen
wurde deshalb mit **`/IWFND/GW_CLIENT`** innerhalb der bestehenden SAP-Sitzung.
Der Statuscode steht im rechten Kopfzeilen-Grid
(`cntlGUI_AREA/.../shellcont[1]/shell`, Zeile `~status_code`) und ist per
Scripting lesbar; der **Antwortrumpf ist es nicht**. Der Menuepunkt
„Auf PC herunterladen" sichert den **Testfall**, nicht die Antwort.

### Der Fehler, der einen halben Anlauf gekostet hat

Die erste Fassung von `DEFINE` hat sich auf `bind_structure( )` verlassen und
danach `get_property( 'Bukrs' )` gerufen. **`bind_structure` legt keine
Properties an**, es verknuepft nur vorhandene mit den Strukturfeldern.
`$metadata` lief deshalb auf `HTTP 500`:

```
Eigenschaft (externer Name) 'Bukrs' fuer Entitaet 'FinanzJournal' nicht gefunden.
```

Sichtbar wurde das erst in `/IWFND/ERROR_LOG`; die Antwort selbst liess sich
nicht lesen. Die zweite Fassung legt jede der 22 Properties einzeln mit
`create_property( )` an, genau wie das von SEGW generierte
`DEFINE_FINANZDATASCHWEIZOE` in derselben Klasse. Typen und Laengen stammen aus
`DD03L` zu `ZSTR_FIN_JOURNAL`.

**Merksatz:** bei Gateway-Code nicht die SAP-Dokumentation aus dem Gedaechtnis
nachbauen, sondern die generierte Schwestermethode derselben Klasse lesen. Sie
zeigt die im System tatsaechlich gueltige Schreibweise, einschliesslich der
Eigenheit, dass `CURR 15,2` als `set_precison( 3 )` mit
`set_maxlength( 16 )` erzeugt wird.

**Entscheid Ingo, 2026-09-10:** statt SEGW der Codeweg ueber MPC_EXT und
DPC_EXT, weil schneller. Der Preis ist festgehalten: dieses eine EntitySet steht
dann nicht im SEGW-Baum, waehrend die uebrigen rund 25 dort gepflegt sind.

**Zwei Stellen, an denen ein Fehler den ganzen Service trifft:**

1. `super->define( )` muss im MPC_EXT als **erstes** laufen. Fehlt es, faellt das
   komplette in SEGW gepflegte Modell weg.
2. Der `WHEN OTHERS`-Zweig im DPC_EXT muss jeden fremden Aufruf mit **allen elf**
   Importing- und beiden Exporting-Parametern an `super->` durchreichen. Die
   Signatur steht in `/IWBEP/IF_MGW_APPL_SRV_RUNTIMEiu` und ist von dort
   uebernommen, nicht erinnert.

Nach den Redefinitionen: Klassen aktivieren, `/IWFND/CACHE_CLEANUP`, dann
`$metadata` von aussen pruefen **und ein bestehendes EntitySet abrufen**, damit
belegt ist, dass die uebrigen Sets unveraendert laufen.

## 6b. Was der Leser tatsaechlich anfragt, und was daraus folgt

Aus `Services/SapGatewayFinancialJournalReader.cs` gelesen, nicht angenommen.
Die Anfrage je Seite sieht so aus:

```
FinanzJournalSet?$format=json&$top=1000&$skip=<n>
  &$orderby=Bukrs,Gjahr,Belnr,Buzei
  &$filter=Budat ge datetime'JJJJ-MM-TTT00:00:00'
```

**Das ist die Anfrage, die es zu testen gilt.** Ein einzelner Aufruf in
`/IWFND/GW_CLIENT` mit genau dieser URL beantwortet alles Offene auf einmal.

Drei Punkte, die dabei auffallen:

1. **Gross- und Kleinschreibung ist unkritisch.** Der Leser verlangt `HkontTxt`,
   das EntitySet heisst die Property `Hkonttxt`. Beide Pruefungen im Leser
   arbeiten mit `StringComparer.OrdinalIgnoreCase`: die Pflichtfeldpruefung
   `FindMissingRequiredFields` und das Zeilen-Dictionary aus `ParseRows`. Der
   Kontotext kommt also an. Alle uebrigen 21 Namen stimmen ohnehin wortgleich.
2. **`$orderby` ist unkritisch.** Belegt an `EKKOSet`: dort steht auf `Ebeln`
   sowohl `set_sortable( abap_false )` als auch `set_filterable( abap_false )`,
   und das Set selbst ist `set_pageable( abap_false )`. Trotzdem liest der
   produktive Einkaufslader es seit Monaten mit `$orderby=Ebeln`, `$filter` und
   `$top`/`$skip`, ohne Fehler.

   **Daraus folgt die eigentliche Regel dieses Service:** diese Kennzeichen sind
   reine Metadaten. Das Gateway weist eine Anfrage deswegen **nicht** ab, es
   reicht die Option nur nicht an den Data Provider durch. Genau daher kommt der
   seit 2026-08-18 dokumentierte Satz, `MARA001Set` und `mbewSet` wuerden
   `$top`, `$skip` und `$orderby` „ignorieren" — sie ignorieren sie nicht aus
   Eigensinn, sie bekommen sie gar nicht erst zu sehen.

   Fuer `FinanzJournalSet` ist das der Unterschied ums Ganze: weil `pageable`
   auf `abap_true` steht und `Bukrs`, `Gjahr`, `Budat`, `Blart` als filterbar
   gekennzeichnet sind, kommen Paging und Filter beim Data Provider wirklich an,
   und der wertet sie aus. `it_order` wertet er nicht aus, sondern sortiert fest
   nach `bukrs gjahr belnr buzei` — genau die Reihenfolge, die der Leser
   verlangt.
3. **Es gibt keinen `Bukrs`-Filter, und das kostet.** Der Leser holt CH und AT
   zusammen und trennt sie selbst; so ist es in Abschnitt 5 vorgesehen. Der
   Data Provider liest dann aber **je Seite** den gesamten Bestand neu aus
   `BKPF` und `BSEG` und wirft alles ausser 1000 Zeilen weg. Siehe die Messung
   in Abschnitt 6d.

## 6c1. Der Abgleich am 2026-09-11 und was er gefunden hat

Ingo hat die Antwort aus dem Gateway Client geliefert. Erste zwei Zeilen,
Buchungskreis `1200`, Jahr `2026`, sortiert wie der Leser sortiert:

```json
"Bukrs":"1200", "Belnr":"14010889", "Gjahr":"2026", "Buzei":"001",
"Budat":"/Date(1769817600000)/", "Monat":"01", "Blart":"DZ",
"Xblnr":"202600001", "Hwaer":"EUR", "Waers":"", "Hkont":"10230",
"Hkonttxt":"", "Shkzg":"S", "Dmbtr":"6092.46", "Faedt":null, "Augdt":null
```

**Vier Dinge waren richtig:**

* Das Datum kommt als `/Date(1769817600000)/`, also 2026-01-31, passend zu
  `Monat` `01`. Der Leser kennt dieses Format, `ParseSapDate` behandelt
  `/Date(...)/` ausdruecklich.
* `Faedt` und `Augdt` sind `null`, `Augbl` leer. Richtig, dieser Beleg hat kein
  `ZFBDT` und ist nicht ausgeglichen.
* `Belnr` und `Hkont` kommen **ohne fuehrende Nullen** an, weil beide den
  Konvertierungsbaustein `ALPHA` tragen. Unkritisch: der Leser schneidet
  fuehrende Nullen ohnehin selbst ab (`GetText(...).TrimStart('0')`).
* Der Beleg ist vollstaendig. Die Gegenprobe in `BSEG` zeigt vier Zeilen, die
  aufgehen: `6092.46` + `124.34` + `0.00` im Soll gegen `6216.80` im Haben.
  `$top=2` hatte nur die ersten beiden gezeigt.

**Ein Fehler war drin: `Hkonttxt` war leer.**

Ursache: der Data Provider las `SKAT` mit `spras = sy-langu`. Bei einem
OData-Aufruf ist die Anmeldesprache **nicht** zwangslaeufig Deutsch, und in
diesem System stehen die Kontotexte ausschliesslich auf `D` — Konto `10230` hat
genau einen Satz, „611 170 309 BA-AUSTR", Konto `30901` einen, „Skontoabzuege".
Die Selektion lief also ins Leere, ohne Fehler, und lieferte durchgehend einen
leeren Kontotext.

Behoben: `SKAT` wird jetzt mit `spras = sy-langu OR spras = 'D'` gelesen, der
Schluessel der internen Tabelle enthaelt `SPRAS`, und beim Lesen wird die
Anmeldesprache bevorzugt mit Deutsch als Rueckfall. Nach Aktivierung und
`/IWFND/CACHE_CLEANUP` liefert dieselbe Anfrage `1'448` statt `1'415` Byte,
also **genau 33 Byte mehr** — die Laenge der beiden fehlenden Texte
(`20` + `13` Byte in UTF-8). Die Methode ist ueber RFC gegengelesen.

**Lehre fuer jeden weiteren Data Provider:** `sy-langu` ist im Gateway kein
verlaesslicher Wert. Wer sprachabhaengige Texte liest, braucht einen Rueckfall,
sonst bleibt die Spalte still leer statt zu scheitern.

## 6c. Erwartete Werte fuer den Abgleich

**Hinweis nachgetragen am 2026-09-11:** die folgende Tabelle war als „erste
Zeilen" gedacht, trifft aber einen anderen Beleg. Ursache ist die
`ALPHA`-Konvertierung: intern heisst der Beleg oben `0014010889` und sortiert
damit vor `0049018498`. `RFC_READ_TABLE` hatte mir eine andere Reihenfolge
geliefert. Die Tabelle bleibt als zweiter Stichprobenbeleg gueltig.

Ueber RFC aus `BKPF`, `BSEG` und `SKAT` gelesen. Beleg `0049018498`,
Buchungskreis `1200`, Jahr `2026`. So muss das EntitySet diese zwei Zeilen
liefern:

| Property | Zeile 1 | Zeile 2 |
| --- | --- | --- |
| `Bukrs` / `Belnr` / `Gjahr` | 1200 / 0049018498 / 2026 | gleich |
| `Buzei` | 001 | 002 |
| `Budat` / `Monat` / `Blart` | 2026-01-14 / 01 / WA | gleich |
| `Hwaer` | EUR | EUR |
| `Waers` | **leer** | **leer** |
| `Hkont` | 0000012021 | 0000012300 |
| `Hkonttxt` | Fertigfabrikate | WE/RE Verrechnungskonto |
| `Shkzg` | S | H |
| `Dmbtr` / `Wrbtr` | 2440.00 / 2440.00 | 2440.00 / 2440.00 |
| `Kostl`, `Prctr`, `Sgtxt`, `Xblnr`, `Stblg` | leer | leer |
| `Faedt`, `Augdt`, `Augbl` | leer | leer |

`Waers` ist absichtlich leer: der Data Provider gibt die Transaktionswaehrung nur
aus, wenn sie von der Hauswaehrung abweicht. **Diese Regel steht doppelt**, denn
der Leser blankt in `TransactionCurrency` noch einmal selbst. Das Ergebnis ist in
beiden Faellen dasselbe, aber eine der beiden Stellen ist ueberfluessig; wer den
Data Provider das naechste Mal anfasst, sollte dort das rohe `WAERS` durchreichen
und die Regel allein dem Leser lassen.

`Faedt` leer ist ebenfalls richtig: `ZFBDT` ist bei diesem Beleg nicht gefuellt,
und das gilt fuer rund vier Fuenftel aller Zeilen (Abschnitt 4, Befund 3).

## 6d. Laufzeitmessung am 2026-09-11

Die Originalanfrage des Lesers, gegen `T76/100` im Gateway Client. Belegart `CO`
ist im Data Provider immer ausgeschlossen.

| Anfrage | Dauer | Status | Nutzlast |
| --- | ---: | --- | ---: |
| `$top=1000&$skip=0`, Filter nur `Budat ge 2026-01-01` | 7,5 s | 200 | 766'556 |
| dieselbe mit `$skip=20000` | 7,6 s | 200 | 720'647 |
| dieselbe mit `$skip=200000` | 7,2 s | 200 | 685'845 |
| dieselbe mit `$skip=600000` | 7,3 s | 200 | 708'051 |
| dieselbe mit `$skip=700000` | 6,8 s | 200 | 20 (leer) |
| `$top=2`, zusaetzlich `Bukrs eq '1200' and Gjahr eq '2026'` | **1,5 s** | 200 | 1'415 |

**Zwei Aussagen dazu.**

Erstens: `$orderby`, `$filter` und Paging werden alle angenommen und wirken. Die
Nutzlast aendert sich je `$skip`, das Ende liegt zwischen `600'000` und
`700'000` Zeilen. Damit ist belegt, was Abschnitt 6b Punkt 2 vorhergesagt hat.

Zweitens, und das ist der Haken: **ein voller Jahreslauf sind rund 650 Seiten zu
je gut sieben Sekunden, also etwa 80 Minuten** — und bei jeder einzelnen Seite
liest der Data Provider den kompletten Bestand erneut. Zum Vergleich: der
Einkaufs-Delta braucht rund 50 Minuten und gilt schon als faktischer Full Load.

Dass die Zeit fast ausschliesslich am Lesen haengt und nicht am Paging, zeigt die
letzte Zeile: mit `Bukrs` und `Gjahr` im Filter faellt dieselbe Anfrage auf
`1,5` Sekunden. Der Filter erreicht den Data Provider also und wirkt, nur schickt
der Leser ihn nicht mit.

## 6e1. Wichtig zum Lesen dieser Messungen: was in T76 ueberhaupt drinsteht

`T76/100` ist eine rund sechs Monate alte Kopie. Im Geschaeftsjahr `2026` gibt es
dort **nur die Perioden 01 bis 04**, und `04` ist angeschnitten:

| Periode | Belegkoepfe ohne `CO` |
| --- | ---: |
| 01 | 22'688 |
| 02 | 29'428 |
| 03 | 33'877 |
| 04 | 14'006 |
| 06 | rund 100 |

Wer eine Messung an Periode `06` macht, misst deshalb fast nichts. Das ist am
2026-09-11 genau einmal passiert: 2,2 Sekunden sahen nach einem grossen Erfolg
aus und waren nur ein leerer Monat. Fuer Laufzeitmessungen ist **`2026/03`** die
richtige Periode, sie hat rund `196'000` Journalzeilen.

## 6e. Monatsweises Lesen, und die Falle dabei

**Entscheid Ingo, 2026-09-11: „lese monatsweise".** Umgesetzt in
`SapGatewayFinancialJournalReader`: der Ladezeitraum wird in Kalendermonate
zerlegt, je Monat wird mit `Budat ge <Monatsanfang> and Budat lt <Folgemonat>`
gefiltert und innerhalb des Monats geblaettert. Die Fenster sind halboffen,
damit kein Beleg zweimal gelesen wird; das letzte endet am Tag nach heute,
damit heutige Buchungen mitkommen.

**Das allein haette den Service umgebracht.** Ein zweiseitiger Datumsfilter
kommt im Data Provider als **zwei** Eintraege in `it_filter_select_options` an,
und ein ABAP-Bereich ist **ODER**-verknuepft. „ab dem 1.6. ODER vor dem 1.7."
ist der gesamte Bestand, inklusive aller Altjahre der Schweiz. Gemessen am
2026-09-11:

| Filter | Dauer | Ergebnis |
| --- | ---: | --- |
| `Bukrs eq '1200' and Budat ge 2026-06-01` | 2 s | `HTTP 200`, ab Zeile 3000 leer |
| dieselbe Anfrage plus `Budat lt 2026-07-01` | 241 s | Sitzung abgerissen |
| ohne `Bukrs`, beide Datumsgrenzen | 105 s | `HTTP 500` |

Deshalb fasst der Data Provider die Grenzen jetzt selbst zu **einem**
`BT`-Bereich zusammen: `GE`/`GT` setzen die Untergrenze, `LE`/`LT` die
Obergrenze, `EQ` und `BT` beide; danach entsteht genau eine Bereichszeile.
Unbekannte Optionen werden unveraendert durchgereicht statt still verschluckt.

**Diese Zusammenfassung gilt nur fuer `Budat`.** Bei `Bukrs`, `Gjahr` und
`Blart` sind mehrere Optionen echte Alternativen (`Bukrs eq '1100' or Bukrs eq
'1200'`), dort ist ODER richtig. Wer spaeter einen Bereichsfilter auf `Gjahr`
schickt, laeuft in dieselbe Falle und muss dort dasselbe tun.

**Der Filter wirkt jetzt**, gemessen an `2026/03`: `HTTP 200` in `3,2` Sekunden
statt `HTTP 500` nach 97 Sekunden.

### Der eigentliche Hebel war aber die Seitengroesse

Dieselbe Periode, dieselbe Anfrage, nur `$top` veraendert:

| `$top` | Dauer | Antwort |
| ---: | ---: | ---: |
| 1'000 | 3,2 s | 753 KB |
| 5'000 | 5,6 s | 3,7 MB |
| 20'000 | **3,0 s** | 14,3 MB |
| 20'000, zweiter Lauf | **2,8 s** | 14,3 MB |

**Die Zeilenzahl einer Seite kostet fast nichts, die Anzahl der Seiten kostet
alles.** Das folgt direkt daraus, dass der Data Provider je Anfrage ohnehin den
gesamten gefilterten Bestand liest. `PageSize` im Leser steht deshalb auf
`20'000` statt `1'000`.

Damit rechnet sich der Jahreslauf neu: rund `650'000` Zeilen ergeben `33` Seiten
zu je etwa drei Sekunden, also **unter zwei Minuten** statt der gemessenen 80.
`2026/03` allein sind zehn Seiten, rund 30 Sekunden.

Wer `PageSize` wieder kleiner dreht, macht den Import vielfach langsamer, nicht
sparsamer. Die aufwendigeren Wege (Selektion um `is_paging` herum bauen) bleiben
Reserve, falls in der Produktion eine einzelne Periode der Schweiz zu gross wird.

## 7. Offen

- **Transport `T76K912530` freigeben und nach `P76` importieren.** Bis dahin gibt
  es das Set nur auf Test, und der produktive Journalimport kann es nicht sehen.
  SapProbe ist bewusst nur fuer `T76/100` eingerichtet, nicht fuer `P76`.
- Fachentscheid: `IsManual = Blart 'SA'` ist bisher eine **Annahme**. In Oesterreich gibt
  es 267 `SA`-Belege. Ob weitere Belegarten als manuell gelten, entscheidet Andreas.
- Ob `PRCTR` mangels Pflege ganz aus dem Set fliegt oder als leere Spalte mitlaeuft.
