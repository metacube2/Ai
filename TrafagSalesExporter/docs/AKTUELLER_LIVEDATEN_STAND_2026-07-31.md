# Aktueller Livedaten-Stand

Stand der Pruefung: 2026-07-31, 10:21 Uhr MESZ

Status: **Kanonischer aktueller Stand fuer die unten behandelten Streitpunkte.**

Diese Datei klaert Aussagen, die in aelteren Arbeitsnotizen oder RAG-Kurzdateien
widerspruechlich vorkommen. Fuer UK-2025, Supplier-Felder, Konzern-Standardkosten und
Einkauf-Delta gilt diese Datei vor den aelteren datierten Zwischenstaenden.

## 1. Pruefgrundlage

Direkt und read-only geprueft wurden:

- produktive SQLite-Datenbank
  `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\trafag_exporter.db`;
- Tabellen `CentralSalesRecords`, `GroupStandardCosts`, `PurchasingSyncState`,
  `PurchasingEkkoCache`, `Sites` und `ExportSettings`;
- produktive `BiDashboard.dll`, Zeitstempel `2026-07-30 14:51:54 MESZ`,
  Groesse `3'223'552` Bytes;
- ausgelieferter Code-Fix `66a34da` fuer das Einkauf-Delta;
- Repository-Stand und `lastchange.md` bis 2026-07-31.

Abfragen liefen mit `.tmp_tools/SqlQ`, das nur `SELECT`, `WITH` und `PRAGMA`
akzeptiert und SQLite zusaetzlich im Modus `ReadOnly` oeffnet. Es gab keinen
Schreibzugriff auf die Produktivdatenbank.

## 2. Was jetzt fachlich gilt

### 2.1 UK-2025 ist vorhanden und erledigt

| Jahr | Zeilen | SalesPriceValue | Supplier vollstaendig | Standardkosten ungleich 0 |
| --- | ---: | ---: | ---: | ---: |
| 2025 | 1'867 | 394'439.16 GBP | 1'867 | 1'759 |
| 2026 | 1'090 | 2'765'684.34 GBP | 1'090 | 1'024 |

Damit gilt:

- Die Aussage **„UK-2025 fehlt“ ist falsch und ueberholt**.
- Fuer alle aktuell 2'957 TRUK-Zeilen sind `SupplierNumber`, `SupplierName` und
  `SupplierCountry` gemeinsam gefuellt.
- Fuer UK ist wegen des Jahres 2025 kein weiterer Export anzufordern.
- Der UK-Backfill ist produktiv wirksam.

### 2.2 Aktueller Datenbestand je Standort

| TSC | Zeilen | fruehestes Datum | neuestes Datum |
| --- | ---: | --- | --- |
| TRAT | 1'790 | 2025-01-07 | 2026-07-28 |
| TRCH | 47'142 | 2025-01-06 | 2026-07-29 |
| TRDE | 7'171 | 2025-01-02 | 2026-07-27 |
| TRES | 5'548 | 2025-01-02 | 2026-07-30 |
| TRFR | 2'598 | 2025-01-07 | 2026-07-30 |
| TRIN | 7'026 | 2025-01-06 | 2026-07-30 |
| TRIT | 19'654 | 2025-01-07 | 2026-07-30 |
| TRUK | 2'957 | 2025-01-03 | 2026-07-30 |
| TRUS | 1'510 | 2025-01-01 | 2026-07-29 |

Gesamt: **95'396 Zeilen**. Alle neun Verkaufsstandorte enthalten produktiv Daten
ab 2025. Das beweist die technische Jahresabdeckung, aber nicht automatisch die
fachliche Vollstaendigkeit jedes Monats oder Betrags.

### 2.3 Supplier-Felder: UK geloest, globale Luecke bleibt

| TSC | Zeilen | alle 3 Supplier-Felder gefuellt | alle 3 leer | Standardkosten ungleich 0 |
| --- | ---: | ---: | ---: | ---: |
| TRAT | 1'790 | 0 | 1'790 | 1'789 |
| TRCH | 47'142 | 0 | 47'142 | 45'561 |
| TRDE | 7'171 | 0 | 7'171 | 4'921 |
| TRES | 5'548 | 0 | 5'548 | 4'497 |
| TRFR | 2'598 | 135 | 2'463 | 1'343 |
| TRIN | 7'026 | 818 | 6'208 | 6'986 |
| TRIT | 19'654 | 14'014 | 5'640 | 18'815 |
| TRUK | 2'957 | 2'957 | 0 | 2'783 |
| TRUS | 1'510 | 6 | 1'504 | 1'364 |

Aktuell sind **17'930 von 95'396 Zeilen** in allen drei Supplier-Feldern
vollstaendig; **77'466 Zeilen** sind in allen drei Feldern leer. Es wurde kein
Zwischenzustand festgestellt, bei dem nur ein Teil der drei Felder gefuellt ist.

Folgerung:

- UK ist vollstaendig und kein offener Supplier-Fall.
- CH, AT, DE und ES sind weiterhin strukturell leer.
- FR, IN, IT und US sind teilweise gefuellt; IT hat innerhalb dieser Gruppe die
  hoechste Abdeckung, aber nicht 100 Prozent.
- Die Supplier-Luecke bleibt der zentrale Engpass fuer belastbare
  Lieferantenklassifikation und Gruppenmarge.

### 2.4 Konzern-Standardkosten TR AG sind produktiv gefuellt

`GroupStandardCosts` enthaelt produktiv:

| Bewertungskreis | Waehrung | Zeilen | Refresh |
| --- | --- | ---: | --- |
| 1100 | CHF | 63'506 | 2026-07-30 12:04 MESZ |

Damit ist die alte Aussage `GroupStandardCosts = 0` ueberholt. Die Tabelle fuer
TR AG ist vorhanden und aktuell befuellt. Dass eine Verkaufszeile diese Kosten
tatsaechlich fuer die Gruppenmarge verwenden kann, haengt weiterhin von
Materialtreffer und Lieferantenklassifikation ab.

## 3. Einkauf-Delta: Code-Fix vorhanden, Live-Wirkung noch offen

Hier muessen drei Ebenen getrennt werden:

1. **Konfiguration:** `PURCHASING_SAP` existiert produktiv und hat weiterhin
   `Sites.IsActive = 0`. Das ist beabsichtigt, damit die Einkaufs-Pseudo-Site
   nicht in den Sales-Export gelangt.
2. **Produktiver Code:** Commit `66a34da` ist in der produktiven DLL vom
   2026-07-30 14:51:54 enthalten. Der Nachtlauf prueft nur noch, ob
   `PURCHASING_SAP` konfiguriert ist; er haengt nicht mehr an `IsActive`.
3. **Live-Wirkung:** Zum Pruefzeitpunkt gibt es in `PurchasingSyncState`
   weiterhin **keinen einzigen `Delta`-Eintrag**. Der letzte erfolgreiche Lauf
   ist der manuelle Full Load vom 2026-07-24; `MAX(PurchasingEkkoCache.Bedat)`
   ist ebenfalls `2026-07-24`.

Der Produktivtimer steht auf `12:00` Uhr lokal, `TimerEnabled = 1`. Der letzte
Timerlauf war am 2026-07-30 etwa 12:20 Uhr und damit **vor** dem Delta-Deploy um
14:51 Uhr. Bei der Live-Pruefung am 2026-07-31 um 10:21 Uhr war der erste
planmaessige Timerlauf nach dem Deploy noch nicht faellig.

Deshalb gilt aktuell:

- **Der Fehler im Code ist behoben und deployed.**
- **Noch nicht durch einen echten Delta-Lauf bewiesen ist, dass der Lauf
  produktiv erfolgreich endet und den Cache aktualisiert.**
- Nach dem naechsten manuellen oder planmaessigen Lauf muessen mindestens ein
  `PurchasingSyncState.Mode = Delta`, Status/Meldung und ein neueres
  `MAX(EKKO.Bedat)` geprueft werden.

Die Aussage „das Delta laeuft produktiv“ darf bis zu diesem Nachweis nicht
verwendet werden. Korrekt ist: „Delta-Fix deployed, erster Wirknachweis offen“.

## 4. Bereits umgesetzte Einkaufsfunktion

Die waehbare Einstiegsdimension im Reiter `Spend-Aufriss` ist umgesetzt und
deployed. Die aeltere Aussage „flexible Einstiegsdimension nicht umgesetzt“ in
einem historischen Kurzstand ist ueberholt.

## 5. Offene Punkte

- Ersten Einkauf-Delta-Lauf nach Deploy pruefen und dieses Dokument mit
  Laufzeit, Status, Zeilenzahlen und neuestem Belegdatum ergaenzen.
- Supplier-Quellen fuer CH/AT, DE und ES fachlich/technisch festlegen.
- Supplier-Fuellung fuer FR, IN, IT und US verbessern.
- Die technische Jahresabdeckung nicht mit einer fachlichen
  Monats-/Betragsvollstaendigkeit gleichsetzen.

## 6. Vorrangregel fuer KI und Menschen

Fuer die in dieser Datei behandelten Themen gilt folgende Reihenfolge:

1. neuere, direkt dokumentierte Live-Pruefung;
2. diese Datei;
3. `lastchange.md`;
4. thematische Dateien unter `docs/rag/`;
5. datierte Analyse-, Sitzungs- und Arbeitsnotizen.

Wenn eine aeltere Datei beispielsweise „UK-2025 fehlt“, `GroupStandardCosts = 0`
oder „Einkauf-Delta laeuft bereits“ sagt, darf diese Aussage nicht ohne erneute
Live-Pruefung uebernommen werden.
