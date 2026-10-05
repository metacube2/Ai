# Logistik live: Kommissionierung und Produktion aus SAP

Stand: 2026-10-01, ergänzt 2026-10-05 um die 3D-Lagerplatzansicht (Abschnitt unten). Auftrag Ingo: „bei Logistik wäre es mega cool, wenn man realtime sieht, wie die
Kommissionierung oder die Produktion läuft … grafisch dargestellt, nutze auch das SAP-Tool“.
Seite `/logistik/live` (Menü Logistik > Logistik live).

## Entscheide

| Frage | Entscheid |
| --- | --- |
| Echtzeit-Technik | **Kein OData-Push.** Das Gateway kann nicht von sich aus senden; echtes Push ginge auf S/4HANA 2023 (`S4CORE 108`, `SAP_BASIS 758`) nur über ABAP Push Channel mit Code in den produktiven Buchungen. Stattdessen ein gemeinsamer Abruf alle 30 s, Blazor schickt die Änderung über die bestehende Verbindung an jeden Browser |
| Wann wird abgefragt | **Nur solange jemand die Seite offen hat** (Ingo). Erster Betrachter startet, letzter stoppt; ein Abruf für alle |
| Personen | **Keine** (Ingo, Empfehlung wegen ArGV 3 Art. 26): weder `AFRU-PERNR` noch Benutzer aus `LTAK/LTAP` werden gelesen |
| Umfang | Kommissionierung und Produktion auf einer Seite (Ingo) |
| Schutz P76 („wenn SAP steht, steht die Produktion“) | Siehe unten |

## Gemessen, bevor gebaut wurde

| Wo | Was | Ergebnis |
| --- | --- | --- |
| T76, 26.03.2026 | `LTAK` Lagernummer 110 | 902 Transportaufträge, Spitzen 7 und 11 Uhr, BWLVS 203/999/319/103/601 |
| T76, 26.03.2026 | `AFRU` | 3'946 Rückmeldungen, 217 Arbeitsplätze, 5 bis 23 Uhr |
| **P76, 01.10.2026 14:31** (SE16N, nur lesend, eigener Modus) | `AFRU` heute / letzte 15 Min. | **2'555 / 44** |
| P76, ebenso | `LTAK` heute / letzte 15 Min. | **477 / 13** |

P76 ist also wirklich live. T76 ist eine Kopie bis Ende März.

## SAP (T76, Transport `T76K912650`, Aufgabe `T76K912651`)

Im Service `ZPOWERBI_EINKAUF_SRV`, im Code wie `HrKpiSet` (`docs/abap/ZLOG_LIVE_*_ADD.abap`):

| Set | Struktur | Inhalt | Filter |
| --- | --- | --- | --- |
| `LogTaSet` | `ZSTR_LOG_TA` (20 Felder) | TA-Position: Lagernummer, TA, Position, Bewegungsart, Lieferung, Material, Werk, von/nach Lagertyp und Platz, Menge, angelegt, quittiert | `Datum` Pflicht, `Abzeit`, `Lgnum`: angelegt **oder** quittiert ab Abzeit |
| `LogLiefSet` | `ZSTR_LOG_LIEF` (13) | Lieferung mit geplantem Warenausgang am Datum, Kunde, Kommissionier- und WA-Status, Positionen gesamt/kommissioniert/teilweise | `Datum` Pflicht, `Lgnum` |
| `LogRueckSet` | `ZSTR_LOG_RUECK` (18) | Rückmeldung: Auftrag, Vorgang, Arbeitsplatz, Werk, Zeit, Gut-/Ausschussmenge, End-/Stornokennzeichen, Material, Soll und bisher zurückgemeldet des Auftrags | `Datum` Pflicht, `Abzeit`, `Werks` |

**Schutz:** nur lesend; Feldlisten statt `SELECT *`; `UP TO 5000/2000/10000 ROWS`; ohne Datum,
mit Datum in der Zukunft oder älter als ein Jahr **kein Datenbankzugriff** (leere Antwort);
immer genau ein Tag; ab dem zweiten Abruf nur die letzten Minuten. Die Altersgrenze stand zuerst
auf 7 Tagen und ist auf ein Jahr gelockert, damit T76 (Daten bis März) testbar ist; die eigentliche
Begrenzung ist der eine Tag.

**Gateway Client T76** (nach `/IWFND/CACHE_CLEANUP`): `$metadata` 200 (354'793); TA 26.03. ganzer Tag
1,7 s / 728 KB, ab 14:00 0,9 s / 196 KB, ab 23:00 nur Lager 110 0,8 s / 4,6 KB; Lieferungen 0,9 s /
28 KB; Rückmeldungen ganzer Tag 2,9 s / 2,4 MB, ab 14:00 1,5 s / 1 MB, ab 23:50 0,7 s leer; ohne
Datum und Datum 2099 je 0,6 s leer. Gegenproben `HrKpiSet`, `FinanzJournalSet` 200. Die Werte selbst
sind nicht zeilenweise geprüft (Rumpf im Gateway Client nicht lesbar).

## Cockpit (`2be8623`)

- `SapGatewayLogisticsLiveReader`: drei Sets, Seiten zu 2'000, höchstens 15 Seiten.
- `LogisticsLiveService` (Singleton): `Subscribe`/`Unsubscribe` je Seite; Abruf alle 30 s
  (`LogisticsLive:IntervalSeconds`, 15 bis 300); erster Abruf des Tages ab 00:00, danach ab letztem
  Abruf minus 2 Minuten, zusammengeführt über den Schlüssel; Tageswechsel leert alles; Timeout 20 s;
  **nach Fehler oder Antwort über 10 s fünf Minuten Pause** mit Eintrag im Ereignisprotokoll
  (Kategorie Logistik); **Notschalter `LogisticsLive:Enabled` in `appsettings.json`** wirkt ohne
  Neustart (wird beim nächsten Deploy wieder `true`).
- Seite: Kacheln (Lagerpositionen quittiert/offen, Lieferungen fertig kommissioniert, Rückmeldungen,
  aktive Arbeitsplätze), Lagerpositionen je Stunde (angelegt/quittiert), Lieferungen mit
  Fortschrittsbalken, Laufband „Zuletzt quittiert“ im Abfahrtstafel-Stil; Arbeitsplätze farbig nach
  letzter Rückmeldung (bis 15 Min. grün, bis 60 gelb, sonst ruhig), Aufträge mit Gutmenge gegen Soll,
  Laufband der letzten Rückmeldungen. Abonniert erst im interaktiven Durchgang, damit das
  Vorab-Rendern keinen Abruf auslöst.
- Tests `LogisticsLiveTests`, 825/825. Texte in sechs Sprachen und Berndeutsch.

## Produktiv seit 2026-10-01 15:01

*Erledigt 2026-10-01 15:01:* Ingo hat `T76K912650` nach P76 importiert. **Gateway Client P76** (heute, 14:59):
`$metadata` 354'793 (gleich T76, Cache aktuell); `LogTaSet` ganzer Tag 1,3 s / 402 KB, letzte 10 Min. 0,8 s / 11 KB;
`LogLiefSet` 0,8 s / 24 KB; `LogRueckSet` ganzer Tag zwei Seiten 2,0 s + 1,2 s / 1,6 MB, letzte 10 Min. 0,9 s / 17 KB;
ohne Datum 0,7 s leer; Gegenproben `HrKpiSet`, `FinanzJournalSet` 200. Deploy `6b4bf21` (810/810, DLL bitgleich,
SHA256 `226536E1...82C3`, fuenf Routen und `/logistik/live` 200, Alarm nur WAL/SHM), Menuepunkt `logistics-live` angelegt.

## 3D-Lagerplatzansicht (2026-10-05)

Wunsch Ingo: „die Lagerplätze mit Umschalter als 3D, sicher die Paletten-Anzeige oder wie ein Platz aussieht,
Zurückschaltung auf Originalsicht möglich“. Umschalter oben rechts **Original | 3D-Lager**; Original ist unverändert.

| Teil | Umsetzung |
| --- | --- |
| Datenbasis | dieselben TA-Positionen wie die Seite (kein zusätzlicher SAP-Abruf): je Position Von-Platz (Entnahme) und Nach-Platz (Einlagerung), offen oder quittiert |
| Lage im Regal | aus dem Platznamen: `01-02-03`, `A-01-2`, `A-07`, `A0102`, `010203` → Gang, Feld, Ebene; belegte Felder und Ebenen werden verdichtet. Plätze ohne Muster liegen der Reihe nach im Gang `~` (10 Felder je Ebene). Höchstens 24 Gänge (die aktivsten) |
| Schnittstellen | Lagertypen `9xx` (z. B. 916 Versandzone, Platz = Lieferung) sind keine Regale und stehen als Zonen unter der Grafik |
| Darstellung | Regale mit Pfosten und Fachböden, je Platz eine Palette mit Kiste: orange blinkend = offene Position, grün = quittiert, Höhe nach Anzahl Bewegungen; Blickwinkel per Regler oder Drehen |
| Ein Platz | Klick auf eine Palette: Platz gross (Regalfach, Palette, je Bewegung eine Kiste, höchstens 9), Entnahmen und Einlagerungen, letzte 15 Bewegungen mit Material, Menge, Richtung, Status |

Code `LogisticsBinLayout` (reine Logik, Tests `LogisticsBinLayoutTests`), `Components/Logistics/WarehouseBins3D.razor`, CSS `wh3d-`.
**Annahme:** Das tatsächliche Platzschema in P76 ist hier nicht geprüft; passt die Zerlegung nicht, landen die Plätze im Gang `~` und die Ansicht bleibt brauchbar. Nach dem ersten produktiven Blick nachschärfen.

## Offen

1. *Erledigt 2026-10-01 15:13:* Sichtpruefung durch Ingo („logistik live sieht super aus“). Werte gegen SAP (ein TA, eine Lieferung, eine Rueckmeldung) noch nicht einzeln abgeglichen.
2. *Erledigt (siehe oben).*
3. Werte gegen SAP prüfen (ein TA, eine Lieferung, eine Rückmeldung per Screenshot).
4. Ideen für später: *Lagerplatzansicht 2026-10-05 als 3D umgesetzt.* Auftragsfortschritt je Arbeitsplatz über den Tag, Warnung
   bei Arbeitsplätzen ohne Rückmeldung während der Schicht.
