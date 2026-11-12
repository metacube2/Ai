# Operations KPI: Liefertreue OTD / OTIF / OTP, Stand 2026-10-14

Auftrag: Mail Fabio Palma vom 2026-10-14 mit Spezifikation und Referenzpaket (`docs/operations_kpi/`). Menue nach Wunsch Ingo:
**Operations** mit den Unterreitern **KPI** (darunter **OTD / OTIF / OTP**) und **Shopfloor** (der bisherige Reiter).

Stand: 2026-11-12 (Doku-Pruefung: Zugriffssatz in Abschnitt 1 als ueberholt markiert); davor Nachtrag 2026-11-02; Grundstand
2026-10-14.

**Nachtrag 2026-11-02:** Rubrik **Kunden** gebaut (Abschnitt 5, `aa12a39`), **produktiv seit 13:57 (`187f8a0`), Abnahme bestanden**
(Oktober um 14:02 neu abgerufen, alle Pruefwerte Fabios exakt gleich). Monatsabruf `202610` am 02.11. um
00:26 ok (Summen gleich Fabios Referenz), die Berechtigung von `POWERBI` reicht also weiterhin (3b).

Stand 2026-10-14 10:35: **produktiv und live aus SAP, Abnahme bestanden (3b)** (Deploy 10:04 `f51bdeb`, Korrektur 10:11 `982e193`, gepusht im manuellen Modus nach
Freigabe Ingo). Seiten und Excel-Export zeigen seit 10:33 den SAP-Abzug. *Ueberholt (10:30):* „zeigen noch den Referenzabzug,
Live-Abruf scheitert an der Berechtigung“ (Abschnitt 3a).
*Ueberholt:* „nicht deployed und nicht gepusht“ (Stand vormittags; der Auto-Modus hatte Push und Deploy-Pruefung abgelehnt,
weil das Referenzpaket Kundennamen und Betraege enthaelt).

## 1. Was gebaut ist

| Teil | Datei |
| --- | --- |
| Berechnung (Port von `extract_liefertreue.js`), Seite und Excel (Port von `build_liefertreue.py`) | `Services/Shopfloor/KpiLiefertreue.cs` |
| Abruf aus SAP, Monatsplan, Speicher, Ausschlusstabelle | `Services/Shopfloor/KpiLiefertreueSync.cs` |
| Endpunkte | `Services/Shopfloor/ShopfloorEndpoints.cs` (`/shopfloor/api/kpi/...`, Seite `/shopfloor/kpi/liefertreue.html`) |
| Uebersichtsseite KPI (Kacheln, Abruf, Ausschluesse) | `wwwroot/shopfloor/kpi/index.html`, Cockpit-Seite `/operations/kpi` (`OperationsKpi.razor`) |
| Report OTD / OTIF / OTP | Vorlage `wwwroot/shopfloor/kpi/board_template.html` (Fabios Vorlage, angepasst), Cockpit-Seite `/operations/kpi/liefertreue` (`OperationsKpiDelivery.razor`) |
| Startstand | `wwwroot/shopfloor/kpi/seed_liefertreue_2026-09.json` = Fabios Abnahmedaten, auf der Seite als „Referenzabzug Fabio (Fiori), Stand 13.10.2026“ angeschrieben |
| Menue | `DatabaseSeedService`: Gruppe `operations` mit `operations-kpi` (Uebersicht, OTD / OTIF / OTP) und `operations-shopfloor`; bestehende Installationen werden umgestellt, solange `operations` noch der alte Link ist |

Zugriff wie der Shopfloor: Login `operations` oder freigegebenes Windows-Konto (`Shopfloor:AllowedUsers`), alles unter `/shopfloor`.
*(Ueberholt am 2026-10-16: Operations ist fuer alle Windows-Benutzer ohne Login offen, `Shopfloor:OpenForAll = true`; ein Login
gilt nur, wenn der Admin-Schalter „Operations: Login nötig“ gesetzt ist, `docs/SHOPFLOOR_2026-10-07.md` Abschnitt 3.)*

**Endpunkte** (unter `/shopfloor/api`): `GET kpi/liefertreue` (Kennzahlen des juengsten oder `?cut=` Stands, letzter Abruf, Staende,
Ausschluesse), `GET kpi/liefertreue/export.xlsx?cut=`, `POST kpi/liefertreue/sync` (Body `{cut}`, startet im Hintergrund),
`POST kpi/exclusions`, `DELETE kpi/exclusions/{id}`.

**Speicher** in `shopfloor.db` (taegliche Shopfloor-Sicherung): `kpi_snapshot (kind, cut, data, source, created_at, created_by)`, ein
Stand je Berichtsmonat, ein SAP-Abzug ersetzt den Referenzabzug desselben Monats; `kpi_exclusion` mit den acht Kunden der Spezifikation.

## 2. Berechnung: was massgebend ist

Massgebend fuer die Abnahme ist Fabios Referenzlogik, nicht der Spezifikationstext. Unterschiede, die bewusst wie die Referenz gebaut sind:

- **Arbeitstage Mo-Fr ohne Feiertagskalender.** Die Spezifikation sagt „ohne Feiertage“, die Referenz rechnet nur Wochenenden heraus.
  *Entschieden 2026-10-14 (Mail Fabio):* richtig so, Feiertage werden bewusst nicht abgezogen; Fabio hat die Spezifikation korrigiert.
- Median = oberes Mittelelement (`s[floor(n/2)]`), kein Mittelwert. Betraege wie `Math.round` (halbe nach oben).
- Fehlt bei geliefertem Status der Ist-WA, gilt die SAP-Kennzahl `DelivToCmtdDelivDelayInDays <= 0`.
- Fehlt ein Termin trotz Ist-WA, gilt die Position wie in JavaScript (`null <= 0`) als puenktlich.
- Kundengruppe „Trafag-Gesellschaft“: Name enthaelt „rafag“/„RAFAG“ (Value-Help) oder Ausschluss mit „konzern“ im Grund
  (Referenz: alle Ausschlusskunden ausser 19914 GFS).
- Zertifikate: Materialtext mit „EN10204“ oder „EN 10204“ (Value-Help), dazu Materialien aus der Ausschlusstabelle.

**Nachweis (Tests `KpiLiefertreueTests`):** Aus den Abnahmedaten ergeben sich genau OTD 90.6 / 80.7 % (16'134), OTIF 89.9 / 78.4 %
(9'047), OTP 73.0 / 57.8 % (15'975), zu frueh 2'236. Die Seite aus `board_template.html` ist zeichengleich mit
`board_beispiel_2026-09.html` (Datenbloecke nach dem Parsen verglichen). Dazu Einzeltests fuer Arbeitstage, Median, Datumsformen,
Rundung, Flags und den Excel-Export, ein Endpunkttest und ein Menue-Migrationstest.

**Nicht nachgewiesen:** die Rechnung auf echten SAP-Rohdaten. Die Abnahmedaten sind bereits verdichtet; erst der erste Abruf aus SAP
fuer `202609` zeigt, ob die Portierung auf Rohdaten dieselben Werte liefert (Toleranz laut Spezifikation 1 Prozentpunkt).
*Ueberholt am 2026-10-14 10:33: auf SAP-Rohdaten nachgewiesen, Abnahme bestanden (3b).*

## 3. Zugriff auf SAP

- Dieselben zwei CDS-Abfragen wie die Referenz, `sap-client=100`, Paging 10'000:
  `C_SLSORDDELIVPERFANLYTSQRY_CDS` (zwei Filter wie die Referenz) und `C_DELIVITMPERFANLYTSQRY_CDS` (Ist-WA ab 1. September Vorjahr),
  dazu die Value-Helps `Product`, `SoldToParty`, `ProductHierarchyNode`.
- Gateway-Wurzel: alles vor `/sap/opu/odata/` der Einkauf-Verbindung (Site Einkauf, technischer Benutzer wie Einkauf/Shopfloor), oder
  `Shopfloor:KpiGatewayRoot`. Mandant `Shopfloor:KpiSapClient` (100).
- **Offen und erst nach dem Deploy pruefbar:** ob der technische Benutzer beide Services aufrufen darf (S_SERVICE) und ob die
  CDS-Zugriffskontrolle (DCL) Zeilen wegfiltert. 403 meldet die Seite als fehlende Berechtigung; ein 200 mit deutlich weniger als
  16'134 faelligen Positionen fuer Jan-Sep 2026 deutet auf DCL, nicht auf die Logik.
  *(Geklaert am 2026-10-14: fehlende Analyse-Berechtigung, mit `ZF_BR_SALES_MANAGER` fuer `POWERBI` behoben, kein DCL-Verlust, 3a/3b)*
- Zeitplan: `Shopfloor:KpiSyncEnabled` (true), ab Kalendertag `KpiSyncDay` (2) fuer den Vormonat, solange fuer diesen Monat noch kein
  SAP-Abzug vorliegt, hoechstens ein Versuch pro Tag *(ueberholt: seit `55b42d9` alle zwei Stunden, 3c)*, erster Versuch 3 Minuten nach dem Start. Nach dem Deploy am 14.10. laeuft also
  sofort der Abruf fuer `202609`, der Abnahmemonat.
- Tabellenfelder der Spezifikation (`VBAP-CMTD_DELIV_DATE` usw.) werden fuer diesen Weg nicht gebraucht und sind **nicht geprueft**.

## 3a. Erster Live-Abruf: Berechtigung (2026-10-14)

- 10:07 und 10:14: `C_SLSORDDELIVPERFANLYTSQRY` antwortet **HTTP 400**, SAP-Text (seit `982e193` im Status sichtbar):
  **„No authority for query 2CCSDSLSORDDLVPRFQ“**. Gateway-Service und Value-Helps gehen durch (kein 403), es fehlt die
  Analyse-Berechtigung fuer die Query.
- P76 lesend (SE16 `AGR_1251`, Freigabe Ingo): die Query steht nur in Rolle **`ZF_BR_SALES_MANAGER`**, Objekte
  `S_RS_COMP` (ACTVT 16, RSZCOMPID `2CCSDSLSORDDLVPRFQ`, InfoProvider `2CISDSLSORDDLVPRFC`) und `S_RS_COMP1`
  (RSZCOMPTP REP, RSZOWNER *). `ZF_BR_INTERNAL_SALES_REP` deckt andere Verkaufs-Queries ab, nicht diese.
  Die Rolle ist breit (279 Aktivitaetswerte, auch 01/02/06) und fuer einen technischen Benutzer nur befristet sinnvoll;
  dauerhaft eine schmale Rolle (nur `S_RS_COMP`/`S_RS_COMP1` mit 16 fuer die zwei Queries) beim Hauptadmin bestellen.
- 10:25: Ingo hat `ZF_BR_SALES_MANAGER` dem Benutzer `KOI` zugeordnet. `AGR_USERS` zeigt: **KOI hatte die Rolle schon seit
  19.10.2023**. Das Cockpit meldet sich mit dem OData-Benutzer **`POWERBI`** an (Angabe Ingo 2026-10-14, in P76 geprueft: Rollen `Z_POWERBI_SERVICES`, `Z_FLP_USER`; Site Einkauf bzw.
  zentraler SAP-Benutzer, gilt fuer alle Gateway-Abrufe des Cockpits). `POWERBI` braucht die Rolle; die doppelte Zeile bei KOI kann weg.
- Die Query hinter `C_DELIVITMPERFANLYTSQRY` ist noch unbekannt (unter „DELIV“, „DLV“, „PERF“ keine Rolle in `AGR_1251`);
  der naechste Abruf nennt sie, falls auch dort eine Berechtigung fehlt.

## 3b. Live-Abruf und Abnahme (*Erledigt 2026-10-14 10:33*)

Ingo hat `ZF_BR_SALES_MANAGER` dem Benutzer **`POWERBI`** zugeordnet (gueltig bis 31.10.2026). Abruf `202609` danach ok:
22'175 Auftragspositionen, 30'358 Lieferpositionen, 155 s. Die Liefer-Abfrage brauchte keine weitere Berechtigung.

| Kennzahl | Abnahme (Fabio, 13.10.) | Live SAP (14.10. 10:33) |
| --- | --- | --- |
| OTD mit / ohne Toleranz | 90.6 % / 80.7 %, 16'134 Pos. | 90.6 % / 80.7 %, 16'132 Pos. |
| OTIF mit / ohne Toleranz | 89.9 % / 78.4 %, 9'047 Auftraege | 89.9 % / 78.4 %, 9'045 Auftraege |
| OTP mit / ohne Toleranz | 73.0 % / 57.8 %, 15'975 Pos. | 73.0 % / 57.8 %, 15'974 Pos. |
| Zu frueh >= 5 AT | 2'236 | 2'236 |

Abnahme bestanden (Toleranz 1 Prozentpunkt; die zwei bis drei Positionen Unterschied sind Nachbuchungen seit dem 13.10.).
Uebersicht und Report zeigen jetzt „Quelle: SAP 14.10.2026 10:33“. **Offen:** `ZF_BR_SALES_MANAGER` laeuft am 31.10. aus;
bis dahin eine schmale Rolle (nur `S_RS_COMP`/`S_RS_COMP1`, ACTVT 16, Query `2CCSDSLSORDDLVPRFQ`, InfoProvider
`2CISDSLSORDDLVPRFC`) beim Hauptadmin bestellen und `POWERBI` zuordnen, sonst scheitert der Monatsabruf am 02.11.

## 3c. Monatsabruf abgesichert (Fabio 2026-10-14, *Erledigt 2026-10-14, `55b42d9`, produktiv*)

Fabio fragte, ob der Abruf einen Neustart uebersteht und ob jemand bei einem Fehler eine Meldung bekommt. Umgesetzt:

1. Jeder Versuch steht in `shopfloor.db`, Tabelle `kpi_run` (Monat, Start, Ende, ok, Ergebnis, Benutzer). Die Statuszeile liest
   nach einem Neustart den juengsten Versuch; ohne Versuch steht neutral „Noch kein Abruf gespeichert“.
   *Ueberholt:* „Letzter Abruf seit dem Start der App: seit dem Start noch kein Abruf“ in Rot nach jedem Neustart.
2. Ab dem Tag nach dem Stichtag (`KpiSyncDay` + 1, also dem 3.) erscheint auf Uebersicht und Report ein rotes Banner, solange
   fuer den Vormonat kein SAP-Stand vorliegt, mit dem letzten Versuch und seinem Grund.
3. Jeder Versuch geht in die Admin-Logs (Kategorie „Operations KPI“): Erfolg als Info, Fehler als Warnung mit SAP-Text.
4. Faellig ist ein Abruf ab dem Stichtag, solange der Vormonat fehlt und der letzte Versuch fuer diesen Monat mindestens zwei
   Stunden zurueckliegt (`KpiLiefertreueSync.IsDue`). *Ueberholt:* einmal pro Tag.

Ein Mailversand ist nicht eingerichtet (braucht eine App-Registrierung bei der IT). Der Dauerbetrieb des App-Pools bleibt ISS-017.

## 4. Offene Punkte

- **Schmale Rolle fuer `POWERBI` vor dem 31.10.** (siehe 3b), sonst scheitert der Abruf vom 02.11.
  *Stand 2026-11-02:* Der Abruf vom 02.11. 00:26 lief durch (23'031 Auftrags-, 31'647 Lieferpositionen). Ob die Rolle verlaengert
  wurde oder eine schmale Rolle zugeordnet ist, ist nicht geprueft; vor dem 02.12. in P76 (`AGR_USERS` fuer `POWERBI`) nachsehen.

- *Erledigt 10:33:* Rolle fuer `POWERBI`, Abruf `202609`, Abnahme (3b).
- *Erledigt 2026-10-14:* Feiertage nicht abziehen (Fabio, Spezifikation korrigiert).
- Inhalt der KPI-Uebersicht (Fabio, separat).
- Transitzeit bei Export-Positionen (Fabio, aus der Spezifikation).
- Shopfloor-Feldmapping (`b613f7c`) geht mit demselben Deploy hinaus, siehe `docs/SHOPFLOOR_2026-10-07.md` Abschnitt 12.

## 5. Rubrik Kunden (Spezifikation Fabio 2026-11-02, Stand 2026-11-02)

Auftrag: Mail Fabio vom 02.11. mit Tab „Erweiterung Kundensicht“ im Spezifikationsdokument (claude.ai-Dokument, Text von Ingo in den
Chat kopiert) und Referenzdatei `Liefertreue_1100_Kunden_2026-10.xlsx` (**nicht im Repo, nicht vorliegend**; die Excel-Blaetter sind
deshalb nach dem Spezifikationstext gebaut). Neu ist nur die Gruppierung nach Auftraggeber (SoldToParty); Definitionen, Ausschluesse
und Abruf bleiben.

**Berechnung** (`KpiLiefertreue.Compute`, im selben Durchlauf wie die Monatswerte): je Kunde und Monat die Zaehler n, puenktlich streng,
puenktlich mit Toleranz, zu frueh (OTD nach Monat des bestaetigten Termins), Auftraege, komplett streng/mit Toleranz (OTIF nach Monat
des spaetesten Termins, dem Auftraggeber des Auftrags zugeordnet, bei zwei Auftraggebern der groessere wie `MAX(SoldToParty)`), Positionen
Wunsch, puenktlich streng/mit Toleranz (OTP nach Wunschmonat). Gespeichert im Stand als `kunden` (eine Zeile je Kunde und Monat).
OTP ohne Toleranz gab es schon. Namen: `SoldToPartyText` fuer **alle** Kunden der Rubrik (vorher nur die der Abweichungsliste);
das Protokoll im Stand (`log`) nennt die Zahl der Kunden und wie viele davon einen Namen haben.

**Seite** (`wwwroot/shopfloor/kpi/board_template.html`, Abschnitt direkt unter den Monatswerten): Suchfeld (Ziffern = Kundennummer genau
oder Anfang, sonst Teil des Namens, mehrere mit Komma), Mindestanzahl Positionen (alle / ab 10 / ab 50), Zuruecksetzen, drei Kacheln
(YTD mit Toleranz, darunter Anzahl, ohne Toleranz, Berichtsmonat), Monatstabelle mit Summenzeile, Trefferliste (sortierbar, Standard
Positionen absteigend, Klick waehlt einen Kunden, zweiter Klick hebt auf), Knopf „Abweichungen dieser Kunden“ (filtert die
Abweichungsliste auf die Kunden, Art „zu spaet“, mit sichtbarem, loeschbarem Kundenfilter) und Excel (`GET kpi/liefertreue/kunden.xlsx?cut=&q=`,
Blaetter „Kunden YTD“, „Kunden pro Monat“, „Definition“; mit Suche nur die Treffer). Kacheln und Monatstabelle summieren die Zaehler
der Trefferliste und rechnen das Prozent danach. Der Filter Mindestanzahl gilt auch fuer die Summe (was in der Liste steht, wird gezaehlt).

**Alte Staende** (vor dem 02.11. geholt, ohne `kunden`) zeigen einen Hinweis statt der Rubrik. **Der Stand `202610` vom 02.11. 00:26 hat
die Kundensicht noch nicht; nach dem Deploy muss Oktober einmal neu abgerufen werden** (Knopf auf der KPI-Uebersicht oder
`POST kpi/liefertreue/sync` mit `{"cut":"202610"}`), der automatische Abruf holt einen vorhandenen SAP-Stand nicht erneut.

**Tests** (`KpiLiefertreueKundenTests`, Suite 1358/1358): Pruefregel 1 auf eigenen Rohzeilen (Summe ueber alle Kunden = jede
Monatszahl), OTIF beim Auftraggeber, Kunde nur mit OTP-Positionen erscheint, Ausschluesse fehlen, Suche, Excel, Seite mit und ohne
Kundensicht. Seitenlogik zusaetzlich mit Node und Beispieldaten durchgespielt (Summen, Suche, Sprung in die Abweichungsliste).

**Abnahme: bestanden am 2026-11-02 14:05** (Deploy 13:57, Oktober neu abgerufen 14:02, Excel der Rubrik nachgerechnet, Seitenskript
der ausgelieferten Seite mit den echten Daten in Node durchgespielt). Alle Werte exakt wie Fabios Tabelle: alle Kunden 17'238 / 92.3 %
(82.8 %), 9'943 / 90.4 % (79.0 %), 17'041 / 75.8 % (60.6 %), Summe ueber alle Kunden = Seite (Pruefregel 1, auch im Monatsblatt);
Hitachi 32 Kunden mit denselben Werten wie unten; 682, 24157, 9204, 24940 je auf die Kommastelle; 682 Oktober gleich. 3'576 Zeilen
Kunde/Monat, 1'123 Kunden, alle mit Namen. *Ueberholt:* die folgende Zeile „offen“. Vorgabe war: Gesamtsummen 17'238 / 9'943 / 17'041 (die Seite trifft sie schon), Suche „Hitachi“
32 Kunden, 2'462 Positionen, OTD 94.2 % (86.3 %), OTIF 93.9 % (85.4 %) aus 1'204 Auftraegen, OTP 82.7 % (71.0 %) aus 2'432; Kunde 682
im Oktober 86 / 89.5 %, 41 / 82.9 %, 94 / 80.9 %. Weitere Werte je Kunde im Spezifikationsdokument und in Fabios Excel.

**Offene Punkte aus der Spezifikation (Entscheid Fabio):** Konzernzuordnung Kunde -> Kundengruppe statt Namenssuche; ausgeschlossene
Kunden mit Hinweis „nicht in der Auswertung“ zeigen.
