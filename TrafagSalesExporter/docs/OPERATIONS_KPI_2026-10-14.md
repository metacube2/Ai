# Operations KPI: Liefertreue OTD / OTIF / OTP, Stand 2026-10-14

Auftrag: Mail Fabio Palma vom 2026-10-14 mit Spezifikation und Referenzpaket (`docs/operations_kpi/`). Menue nach Wunsch Ingo:
**Operations** mit den Unterreitern **KPI** (darunter **OTD / OTIF / OTP**) und **Shopfloor** (der bisherige Reiter).

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

## 3. Zugriff auf SAP

- Dieselben zwei CDS-Abfragen wie die Referenz, `sap-client=100`, Paging 10'000:
  `C_SLSORDDELIVPERFANLYTSQRY_CDS` (zwei Filter wie die Referenz) und `C_DELIVITMPERFANLYTSQRY_CDS` (Ist-WA ab 1. September Vorjahr),
  dazu die Value-Helps `Product`, `SoldToParty`, `ProductHierarchyNode`.
- Gateway-Wurzel: alles vor `/sap/opu/odata/` der Einkauf-Verbindung (Site Einkauf, technischer Benutzer wie Einkauf/Shopfloor), oder
  `Shopfloor:KpiGatewayRoot`. Mandant `Shopfloor:KpiSapClient` (100).
- **Offen und erst nach dem Deploy pruefbar:** ob der technische Benutzer beide Services aufrufen darf (S_SERVICE) und ob die
  CDS-Zugriffskontrolle (DCL) Zeilen wegfiltert. 403 meldet die Seite als fehlende Berechtigung; ein 200 mit deutlich weniger als
  16'134 faelligen Positionen fuer Jan-Sep 2026 deutet auf DCL, nicht auf die Logik.
- Zeitplan: `Shopfloor:KpiSyncEnabled` (true), ab Kalendertag `KpiSyncDay` (2) fuer den Vormonat, solange fuer diesen Monat noch kein
  SAP-Abzug vorliegt, hoechstens ein Versuch pro Tag, erster Versuch 3 Minuten nach dem Start. Nach dem Deploy am 14.10. laeuft also
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

- *Erledigt 10:33:* Rolle fuer `POWERBI`, Abruf `202609`, Abnahme (3b).
- *Erledigt 2026-10-14:* Feiertage nicht abziehen (Fabio, Spezifikation korrigiert).
- Inhalt der KPI-Uebersicht (Fabio, separat).
- Transitzeit bei Export-Positionen (Fabio, aus der Spezifikation).
- Shopfloor-Feldmapping (`b613f7c`) geht mit demselben Deploy hinaus, siehe `docs/SHOPFLOOR_2026-10-07.md` Abschnitt 12.
