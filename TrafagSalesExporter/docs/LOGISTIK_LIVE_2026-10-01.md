# Logistik live: Kommissionierung und Produktion aus SAP

Stand: 2026-10-01, ergänzt 2026-10-05 um die 3D-Lagerplatzansicht (Abschnitt unten). Auftrag Ingo: „bei Logistik wäre es mega cool, wenn man realtime sieht, wie die
Kommissionierung oder die Produktion läuft … grafisch dargestellt, nutze auch das SAP-Tool“.
Seite `/logistik/live` (Menü Logistik > Logistik live).

## Entscheide

| Frage | Entscheid |
| --- | --- |
| Echtzeit-Technik | **Kein OData-Push.** Das Gateway kann nicht von sich aus senden; echtes Push ginge auf S/4HANA 2023 (`S4CORE 108`, `SAP_BASIS 758`) nur über ABAP Push Channel mit Code in den produktiven Buchungen. Stattdessen ein gemeinsamer Abruf alle 30 s, Blazor schickt die Änderung über die bestehende Verbindung an jeden Browser |
| Wann wird abgefragt | **Nur solange jemand die Seite offen hat** (Ingo). Erster Betrachter startet, letzter stoppt; ein Abruf für alle |
| Personen | *Überholt 2026-10-05:* zuerst **keine** (Ingo, Empfehlung wegen ArGV 3 Art. 26). Seit 2026-10-05 **Namen der TA-Benutzer nur nach Anmeldung** (Passwort wie HR KPI), HR-Freigabe laut Ingo 2026-10-05; ohne Anmeldung anonym wie bisher. Siehe Abschnitt „Produktivität und Kapazität“ |
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
*Überholt 09:55:* Die Annahme „Platzschema ungeprüft“ ist erledigt. **Produktiv 09:21 (`98146c6`), um 09:52 mit echten Daten angesehen:** 318 TA-Positionen,
Lagertypen 100, 1BP, 1G1, 1P1, 2BP, 2G1, ML4, QM2, UGK, ABT, CZ; Zonen 901, 902, 911, 916, 922, 999. Echte Platznamen wie
`EL100`, `DL20004`, `EDB0102`, `1-008-B`, `3-162-B`, `BP-MLE04-2`, `TR5_KERAM`, `SPEDITION`. Befund: Die ersten Regeln
erkannten `1-008-B`, `BP-MLE04-2` und `DL20004` nicht (alles im Gang `~`), und 24 Gänge in einer Reihe machten die Halle
winzig. **Nachgeschärft:** Ebene auch als Buchstabe (B = 2), Feld mit Buchstaben vor der Nummer (`MLE04` → 4), Buchstaben +
beliebig viele Ziffern (letzte zwei = Ebene, bei drei Ziffern die letzte); Gänge in bis zu vier Spalten nebeneinander.
**Produktiv 10:23 (`71981b6`)**, 10:24 angesehen: 24 Gänge, davon 2G1 in acht Gängen mit Ebenen, 1G1 in vier, im Gang `~`
nur noch wenige Plätze (100, 1BP, ABT, QM2); Zonen 901 bis 999 darunter.

## Produktivität und Kapazität (2026-10-05)

Wunsch der Logistik nach der Vorstellung am 2026-10-05 (über Ingo): Kapazitätsplanung, Produktivität vom Rüsten bis zum
Warenausgang, Namen der Leute nur mit Login, sonst anonym. Entscheide Ingo am selben Tag:

| Frage | Entscheid |
| --- | --- |
| Kapazitätsplanung | alle drei: Arbeitsvorrat Kommissionierung, Arbeitsplätze Produktion, Warenausgang nach Termin |
| Produktivität | beides: Durchlaufzeit je Lieferung und Leistung je Stunde |
| Namen | **HR-Freigabe liegt vor (laut Ingo, 2026-10-05)**; Anzeige nur nach Anmeldung mit eigenem Passwort wie HR KPI, das Ingo setzt; nur im Speicher für den laufenden Tag, keine Speicherung, kein Export |
| SAP | zuerst T76, P76 erst nach Ingos Import; neue Transportaufträge beim ersten Speichern |

**Teil A, Cockpit, anonym (ohne SAP-Änderung):** Umschalter **Produktivität** neben Original und 3D-Lager. Rüstzeit je
Lieferung = erster TA angelegt bis letzte Position quittiert (Median, 8 von 10 darunter), offene Rüstvorgänge älteste
zuerst (rot, wenn länger offen als 80 % der fertigen), quittierte Positionen je Stunde und Lagernummer, Positionen je aktive
Stunde. Rechnet nur aus dem gemeinsamen Abruf, kein zusätzlicher SAP-Zugriff. Code `LogisticsProductivity` (Tests
`LogisticsProductivityTests`), `Components/Logistics/ProductivityPanel.razor`. Deploystand siehe `docs/rag/DEPLOYMENT.md`.

**Teil A produktiv 2026-10-05 10:31 (`9fdae38`)**, angesehen: 50 von 53 Lieferungen gerüstet, Median 4 min, 8 von 10 unter
8 min, 64 Positionen je aktive Stunde (nur Lager 110 quittiert).

**Teil B, SAP-Transport 1 (in Arbeit, Stand 2026-10-05 11:00):** Transport **`T76K912658`** „Logistik live: TA-Benutzer und
Warenausgang“, Paket `ZPP`. In T76 erledigt: `ZSTR_LOG_TA` + `BNAME`/`ENAME`/`QNAME` aktiv (DD03L geprüft),
`ZSTR_LOG_LIEF` + `WADAT`/`WADAT_IST`/`WA_ZEIT` (CHAR8/CHAR8/CHAR6) aktiv; `GET_ENTITYSET` (DPC_EXT, Include `CM01W`) und
`DEFINE` (MPC_EXT, `CM001`) neu geschrieben und **gesichert, noch nicht aktiviert**. Grundlage war der per `abap-read` gelesene
Live-Quelltext, nicht die Repo-Schnipsel; dieselben Ersetzungen stehen in `docs/abap/ZLOG_LIVE_*_ADD.abap`. Offen: beide
Klassen aktivieren, `/IWFND/CACHE_CLEANUP`, Gateway Client T76. Inhalt: `LogTaSet` + `BNAME` (TA angelegt von), `ENAME` (Entnahme quittiert von),
`QNAME` (quittiert von); `LogLiefSet` + `WADAT`, `WADAT_IST` und Uhrzeit der Warenbewegung aus `VBFA-ERZET`; Datum für
`LogLiefSet` bis heute + 14 Tage (weiterhin ein Tag je Abfrage). Felder in T76 per RFC belegt (2026-10-05).

**Teil C, SAP-Transport 2 (geplant):** neues Set für Kapazität je Arbeitsplatz und Tag (Bedarf aus `KBED`, Angebot über den
SAP-Standardbaustein für verfügbare Kapazität statt selbst aus `KAPA` gerechnet), Pflichtfilter Werk, höchstens 14 Tage.

**Umbenennung 2026-10-05:** „Rüstzeit“ heisst jetzt **„Durchlaufzeit“**. Nevas Unterlage „Logistikkennzahlen bei Trafag“
(über Ingo, 2026-10-05) hält fest: „Auftragserstellung bis Quittierung ist nicht automatisch Arbeitszeit.“ Die Seite sagt das jetzt.

**Teil D, Kapazität je Bereich (Übergangslösung aus Nevas Unterlage):** Umschalter **Kapazität**. Je Tag und Bereich
(Wareneingang, Versorgung Abteilungen, Vorverpackung, MLE01, MLE02, MLE04, Rüsten Kundenaufträge, Sammellisten): verfügbare
und eingesetzte Personenstunden, erledigt, offen, Planzeit je Einheit; daraus Produktivität, gemessene Zeit je Einheit,
Bedarf, Belastung (grün bis 85 %, orange bis 100 %, rot darüber) und Lücke, wie in Nevas Kennzahlentabelle. Beim Rüsten am
laufenden Tag zählt das Cockpit offene und quittierte TA-Positionen mit Lieferung selbst. Planzeiten werden vom letzten
erfassten Tag übernommen. Tabelle `LogisticsCapacityDay` (Summen je Bereich, keine Personen), Code `LogisticsCapacity.cs`,
`CapacityPanel.razor`, Tests `LogisticsCapacityTests`. **Erfassen darf jeder, der die Seite öffnet** (wie die übrige
Seite ohne Anmeldung); bei Bedarf hinter den geplanten Logistik-Login legen.

**SAP-Befund zu Nevas Fragen (T76, nur lesend, 2026-10-05):**

| Feld | Befund |
| --- | --- |
| `LTAK-STDAT/STUZT/ENDAT/ENUZT`, `ISTWM`, `SOLWM` (Start, Ende, Ist- und Sollzeit je TA) | vorhanden, 2026 **nie gefüllt**: WM-Zeiterfassung ist aus (Nevas „Zeitfelder vorhanden, aber nicht aktiv“). Einschalten ist Customizing mit der SAP-Beratung |
| `LTAK-REFNR` (WM-Gruppe) | nie gefüllt: Sammellisten laufen nicht über WM-Gruppen; wo sie entstehen, ist offen |
| `LTAK-PERNR` | leer; Namen kommen über `BNAME`/`QNAME` (Teil B) |
| `AFRU` Start/Ende | Start = Ende, keine echte Dauer |
| `AFRU-ISM01` | teilweise gefüllt (z. B. 12 h, 10 min): rückgemeldete Leistung als Personenzeit für MLE und Produktion nutzbar (Teil C) |

T76-Daten enden im März; P76 vor dem Bau von Teil C gegenprüfen.

**Grenze T76:** Daten in T76 enden im März; Abfragen in die Zukunft und Kapazitätsbedarf werden dort voraussichtlich leer
sein. T76 belegt Felder und Schutz, nicht den Inhalt.

## Offen

1. *Erledigt 2026-10-01 15:13:* Sichtpruefung durch Ingo („logistik live sieht super aus“). Werte gegen SAP (ein TA, eine Lieferung, eine Rueckmeldung) noch nicht einzeln abgeglichen.
2. *Erledigt (siehe oben).*
3. Werte gegen SAP prüfen (ein TA, eine Lieferung, eine Rückmeldung per Screenshot).
4. Ideen für später: *Lagerplatzansicht 2026-10-05 als 3D umgesetzt.* Auftragsfortschritt je Arbeitsplatz über den Tag, Warnung
   bei Arbeitsplätzen ohne Rückmeldung während der Schicht.
