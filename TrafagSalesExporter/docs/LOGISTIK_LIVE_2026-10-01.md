# Logistik live: Kommissionierung und Produktion aus SAP

Stand: 2026-10-01, nachgefuehrt 2026-10-06 (Pruefbefund-Korrekturen produktiv 10:16, Abschnitt am Ende bzw. Nachtrag Review), ergänzt 2026-10-05 um die 3D-Lagerplatzansicht (Abschnitt unten). Auftrag Ingo: „bei Logistik wäre es mega cool, wenn man realtime sieht, wie die
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
`DEFINE` (MPC_EXT, `CM001`) neu geschrieben, *überholt 11:40:* mit Ingos Freigabe („option b, du darfst“) per GUI-Scripting **aktiviert**
(MPC_EXT, dann DPC_EXT), aktiver Quelltext per RFC gegengelesen, Cache `/IWFND/CACHE_CLEANUP` für `ZPOWERBI_EINKAUF_MDL`.
**Gateway Client T76:** `$metadata` 200, 355'759 (vorher 354'793); `LogTaSet` 26.03. 200, 1,6 s, 1,38 MB; `LogLiefSet` 26.03. 200,
1,1 s, 61 KB; `LogLiefSet` 08.10. (Zukunft) 200 leer (T76 ohne Zukunftsdaten), 30.10. (über 14 Tage) 200 leer; Gegenproben
`LogRueckSet`, `HrKpiSet` 200. Grundlage war der per `abap-read` gelesene
Live-Quelltext, nicht die Repo-Schnipsel; dieselben Ersetzungen stehen in `docs/abap/ZLOG_LIVE_*_ADD.abap`. Offen: beide
Klassen aktivieren, `/IWFND/CACHE_CLEANUP`, Gateway Client T76. Inhalt: `LogTaSet` + `BNAME` (TA angelegt von), `ENAME` (Entnahme quittiert von),
`QNAME` (quittiert von); `LogLiefSet` + `WADAT`, `WADAT_IST` und Uhrzeit der Warenbewegung aus `VBFA-ERZET`; Datum für
`LogLiefSet` bis heute + 14 Tage (weiterhin ein Tag je Abfrage). Felder in T76 per RFC belegt (2026-10-05).

**Teil C, Stand 2026-10-05 12:35:** Testreport `Z_LOG_KAP_TEST` (`$TMP`, nur lesend, Quelle `docs/abap/Z_LOG_KAP_TEST.abap`) in T76
ausgeführt: `CR_CAPACITY_AVAILABLE_PERIODS` liefert ohne vorbereiteten Puffer **0 Perioden**; MLE01 und MLE02 teilen sich die
Personalkapazität `10000262` „LAG00“ (Art 002, 8–17 Uhr, 1 h Pause, 100 %, 1 Kapazität = 8 h/Tag), daneben je eine eigene
Maschinenkapazität; `KAPA` (Intervalle) leer, also gilt das Standardangebot aus `KAKO`; `KBED` in Stunden, in T76 Reste 0 (erledigt),
Soll vorhanden; `KBED` hat den Sekundärindex 3 auf `KAPID`. **Neu entworfen: Set je Kapazität und Tag** (`docs/abap/ZLOG_KAP_ADD.abap`,
zweite Fassung): Bedarf `KBEAREST`+`KRUEREST` über den Index `KAPID`, Angebot `(ENDZT−BEGZT−PAUSE) × NGRAD % × AZNOR` an
Arbeitstagen des Fabrikkalenders (`KAKO-KALID`, sonst `T001W-FABKL`); Kapazitäten mit Intervallen bekommen kein Angebot
(`KeinStandard = X`) statt eines falschen. In T76 angelegt: Struktur **`ZSTR_LOG_KAP`** aktiv im neuen Transport **`T76K912660`**
(Paket ZPP, 12 Felder per DD03L geprüft). **Klassen noch nicht geändert:** `DPC_EXT`/`MPC_EXT` sind im nicht freigegebenen
`T76K912658` gesperrt; jede weitere Änderung liefe still in diesen Auftrag (beim Versuch gemessen, Sichern ohne Auftragsabfrage).
Der Versuch wurde zurückgenommen: `DEFINE` mit dem Teil-B-Stand neu gesichert und aktiviert, `$metadata` wieder 355'759.
*Erledigt 2026-10-05:* Ingo hat `T76K912658` freigegeben und nach P76 importiert (Cockpit 12:50: Vorschau Warenausgang
liefert P76-Zukunft, z. B. 07.10. 140 Lieferungen). **`T76K912660` war dabei mitfreigegeben** (nur die Struktur); die
Klassenänderung für Teil C liegt deshalb im neuen Auftrag **`T76K912662`** „Logistik live: LogKapSet Klassen“ (DPC_EXT
`GET_ENTITYSET` 1'214 Zeilen, MPC_EXT `DEFINE` 1'396 Zeilen, aktiv, per RFC gegengelesen; Cache geleert).
**Gateway Client T76:** `$metadata` 200, 357'957; `LogKapSet` Werk 1100 ab 05.10., 14 Tage: 200, 1,1 s, 30'688 Bytes; ab 10.09.:
200, 0,9 s, 73'581 Bytes; ab 02.03. (älter als 30 Tage) und ohne Filter: 200 leer (Schutz); Gegenproben `LogTaSet`, `LogLiefSet`,
`HrKpiSet` 200. **Import P76:** `T76K912660` (Struktur) muss vor oder mit `T76K912662` drin sein, danach `/IWFND/CACHE_CLEANUP`.
**Cockpit:** Umschalter Kapazität zeigt oben „Kapazität Produktion aus SAP, nächste 14 Tage“: je Kapazität (Name, Arbeitsplätze)
und Tag die Belastung in Prozent (Ampel), Tooltip Bedarf/Angebot/Vorgänge; ohne Angebot nur die Stunden; fehlt das Set in P76,
steht der Hinweis auf `T76K912662`. Ein Aufruf, höchstens alle 15 Minuten, Werk 1100.

**Stand P76 Teil C (2026-10-05 13:45):** Ingo meldet `T76K912662` importiert; in der Importqueue (Screenshot Ingo) hat er aber
als einziger der vier Aufträge **keine grüne Nummer (359) und ein oranges Dreieck** (Warnung), `T76K912658` und `T76K912660`
sind grün mit Häkchen. Das Cockpit zeigte um 13:40 noch „Set fehlt“ (gemerkter Abruf von 13:35, 15 Minuten). *Erledigt 13:53:* trotz der Warnung ist `LogKapSet` in P76 da. Cockpit 13:53: rund 30 Kapazitäten mit Belastung je Tag,
z. B. LAG00 (u. a. MLE01, MLE02, MLE04) 05.10. 222 %, 06.10. 63 %, 07.10. 40 %; DSQ00 an allen Werktagen über 100 % (158 bis 275 %);
Wochenenden ohne Angebot (0.0 h). **Auffällig (offen, nicht geprüft):** Der laufende Tag ist bei vielen Kapazitäten sehr hoch
(DWM00 1323 %), ~~vermutlich Rückstand, der auf heute terminiert ist~~ **(überholt 2026-10-05, Review: Rückstand rollt nicht auf heute. Ursache ist, dass `KBED` den ganzen Restbedarf eines Vorgangs auf `FSTAD`, den Starttermin, legt; ein mehrtägiger Vorgang landet so an einem Tag. `ZLOG_KAP_ADD.abap` verteilt den Bedarf jetzt gleichmässig auf die Arbeitstage `FSTAD` bis `FENDD`, rechnet `KEINH` (H, MIN, S) in Stunden um und zählt verschiedene Vorgänge. Rückstand, dessen Zeitraum vor dem Fenster endet, steht nicht mehr auf dem ersten Tag. Wirkt erst nach dem nächsten Transport; Feldnamen `FENDD`/`KBEDID` vorher in SE11 prüfen.)**. Mit der Logistik oder AV gegenprüfen. Das Warnprotokoll von
`T76K912662` hat Ingo nicht geschickt; ohne Wirkung auf die Funktion.

**Schwäche im Cockpit (erledigt im Code 2026-10-05, Review; noch nicht deployed): Fehler werden nur 1 Minute gemerkt, ein HTTP-Timeout wird als Fehler gezeigt statt den Schaltkreis zu stören. Ursprünglich offen:** Ein Fehler beim Abruf von Kapazität oder Vorschau wird wie ein Erfolg 15 Minuten gemerkt. Nach
einem Import zeigt die Seite deshalb bis zu 15 Minuten weiter „fehlt“. Besser: Fehler nur 1 Minute merken.

**MES (Frage Ingo 2026-10-05):** Antwort Ingo: **Eigenentwicklung**, der Entwickler hat wenig Zeit; Ingo klärt das später. Grundsatz: Daten des MES sieht das Cockpit nur, wenn das MES sie nach SAP zurückmeldet
(dann stehen sie in `AFRU` und kommen über `LogRueckSet` und die Kapazität schon an). Hat das MES eine eigene Datenbank oder
Schnittstelle, braucht es Name, Hersteller, Zugang und eine Firewall-Freigabe; ungeprüft.
Ursprünglicher Entwurf: Quellen in T76 per RFC belegt: Bedarf
`KBED-KBEAREST` + `KRUEREST` (Einheit `KEINH`, Datum `FSTAD`, Arbeitsplatz `ARBID` → `CRHD`), Angebot über den Standardbaustein
`CR_CAPACITY_AVAILABLE_PERIODS` (Tabelle `RC65K`: `KAPID`, `DATUV`/`DATUB`, `ANGEB`, `EINZT`, `KEINH`). Drei Punkte vor dem Anlegen in
SE37 prüfen (stehen am Ende des Entwurfs). Ursprünglicher Plan: neues Set für Kapazität je Arbeitsplatz und Tag (Bedarf aus `KBED`, Angebot über den
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

**Cockpit zu Teil B (2026-10-05):** liest `Bname`, `Ename`, `Qname`, `Wadat`, `WadatIst`, `WaZeit`; fehlen sie (P76 vor dem Import),
bleiben sie leer, nichts bricht (Test). **Namen:** ohne Anmeldung bekommt jede Darstellung (Original, 3D, Produktivität, Kapazität)
die anonymisierte Liste (`LogisticsPeople.Anonymize`, Test). Nach Anmeldung (Abschnitt „Leistung je Person“ unter Produktivität)
je Benutzer: quittierte Positionen, angelegte TA, erste/letzte Quittierung, geschätzte Stunden (Abschnitte mit Lücken bis 15 Minuten,
einzelne Quittierung 2 Minuten), Positionen je Stunde. Nur im Speicher, kein Export. **Warenausgang nach Termin:** eigene Abfrage
der nächsten 14 Tage (je Tag ein Aufruf), höchstens alle 15 Minuten, gemeinsam für alle, nur wenn der Umschalter Kapazität offen ist.
**Bis Warenausgang:** Median erster TA angelegt bis Warenbewegung, sobald `WadatIst`/`WaZeit` kommen.

**Passwort für die Namen setzen (Ingo):** In PowerShell den Hash erzeugen und ihn (nicht das Passwort) in
`appsettings.json` unter `LogisticsPeopleAccess:PasswordHash` eintragen lassen; Benutzer ist `logistik`:
```powershell
$p = Read-Host "Passwort" -AsSecureString; $t = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($p)); [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($t)))
```
Ohne Hash zeigt die Seite „noch nicht eingerichtet“ und bleibt anonym.

**Prüfliste P76 nach Ingos Import von `T76K912658`:** `$metadata` muss auf rund 355'759 wachsen, sonst `/IWFND/CACHE_CLEANUP` für
`ZPOWERBI_EINKAUF_MDL` in P76; dann `LogTaSet` heute 200 mit Benutzern, `LogLiefSet` heute und morgen 200, Gegenproben `HrKpiSet`,
`FinanzJournalSet` gefiltert. Danach im Cockpit: Kapazität zeigt die Vorschau, Produktivität nach Anmeldung die Personen.

**Grenze T76:** Daten in T76 enden im März; Abfragen in die Zukunft und Kapazitätsbedarf werden dort voraussichtlich leer
sein. T76 belegt Felder und Schutz, nicht den Inhalt.

## Offen

1. *Erledigt 2026-10-01 15:13:* Sichtpruefung durch Ingo („logistik live sieht super aus“). Werte gegen SAP (ein TA, eine Lieferung, eine Rueckmeldung) noch nicht einzeln abgeglichen.
2. *Erledigt (siehe oben).*
3. Werte gegen SAP prüfen (ein TA, eine Lieferung, eine Rückmeldung per Screenshot).
4. Ideen für später: *Lagerplatzansicht 2026-10-05 als 3D umgesetzt.* Auftragsfortschritt je Arbeitsplatz über den Tag, Warnung
   bei Arbeitsplätzen ohne Rückmeldung während der Schicht.

**Nachtrag Review 2026-10-05 (C#-Teil produktiv 2026-10-06 10:16 mit `f582814`; ABAP im naechsten Transport, vorher in SE11 `FENDD`, `KBEDID`, `KEINH` in KBED pruefen und `STZHL`/`UEBERF` in die Strukturen):** Timeout als Fehler, Fehler 1 Minute gemerkt; Vorschau Warenausgang mit Lgnum 110, laufendem Tag und Eimer ueberfaellig (Filter `Ueberf`, nur wenn P76 ihn kennt); Kommissionierung zaehlt nur Lieferungen mit Positionen; Bis-Warenausgang nennt Lieferungen ohne Uhrzeit und widerspruechliche; Leistung nur vom Tag selbst; Storno ueber `Stzhl` (Original wird ueber Rueck/Rmzhl entwertet); Gaenge nach Lagernummer getrennt, doppelte Felder zusammengefasst, "+N weitere Gaenge"; Gutmenge je Auftrag aus dem letzten Vorgang; Stand von gestern gilt nach Mitternacht als erster Abruf; Last ohne verfuegbare Stunden als unendlich. ABAP: Warenausgangszeit aus VBFA (ERDAT = WADAT_IST) oder MKPF, Auswahl nach WADAT oder WADAT_IST, offene TA der letzten 14 Tage, Obergrenzen mit Warnung, Kapazitaet verteilt und mit Paging.
