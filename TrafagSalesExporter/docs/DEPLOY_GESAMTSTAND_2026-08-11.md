# Produktivdeploy Gesamtstand 2026-08-11

Stand: 2026-08-11, 15:51 MESZ

## Ergebnis

Der gesamte aktuelle Anwendungsstand ist produktiv deployed. Enthalten sind insbesondere:

- Supplier-Fallback ueber den CH-Werkstamm `MARC`, Werk `1100`, mit persistentem
  Umschalter auf die alte MBEW-Variante;
- Einkaufs-Produktgruppen ausschliesslich aus SAP OData statt aus den beiden
  `zdispo*.xlsx`-Dateien;
- zusammengefuehrter aeusserer `Admin Bereich`;
- aktuelles FPV-Drohnenspiel unter `/pause`.

Der Nutzer hat den gemeinsamen Deploy trotz der noch nicht in SAP aktivierten
EntitySets `ZDISPO_GRP` und `ZDISPO_SPART` ausdruecklich freigegeben.

## Vorher-Sicherung und Release

- Release-Regression unmittelbar vor dem Deploy: `471/471` Tests gruen.
- Bekannte, unveraenderte Warnung: zwei `NU1903`-Hinweise fuer
  `Microsoft.AspNetCore.Authentication.Negotiate 8.0.24`.
- Konsistentes SQLite-Backup per `BackupDatabase`-API:
  `trafag_exporter.db.before-all-current-20260811-145332.bak`,
  `340'455'424` Bytes.
- Das Backup enthaelt wie der Vorherstand `96'298` Sales-Zeilen.
- Publish-Ziel:
  `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$`.
- `app_offline.htm` war nur waehrend Publish beziehungsweise kontrolliertem
  Neustart aktiv und liegt danach wieder als `app_offline.htm.disabled` vor.

Ausgelieferte Hauptbaugruppe:

```text
BiDashboard.dll
Groesse: 4'362'752 Bytes
SHA256: 2A5DBC034891F5B5D3FD1EE04C123A989CA987B5020CE04A0FE5161D037177F4
```

Lokaler Release-Build und Serverdatei sind bitgleich.

## Supplier-Fallback produktiv

Die neue Schema-Version ist aktiv:

- Tabelle `GroupMaterialMasters` vorhanden;
- `ExportSettings.SupplierFallbackMode = ChPlantMaster`;
- `66'049` unterschiedliche Materialien fuer Werk `1100` aus SAP MARC;
- `63'550` bestehende MBEW-/`GroupStandardCosts`-Materialien fuer 1100;
- `0` MBEW-Schluessel fehlen im neuen MARC-Bestand;
- `CentralSalesRecords` blieb vor und nach dem Deploy bei `96'298` Zeilen.

Der erste Backfill lief waehrend einer kurzzeitigen Laptop-/SMB-Netzunterbrechung.
Obwohl der Tool-Output den Commit meldete, war der ueber UNC geschriebene WAL danach
nicht dauerhaft sichtbar. Der leere Cache war fachlich sicher, weil die Anwendung in
diesem Zustand automatisch auf MBEW zurueckfaellt. Der Backfill wurde deshalb einmal
kontrolliert bei gestoppter Website wiederholt. `66'049` Zeilen wurden sowohl vor dem
Neustart als auch nach dem Neustart erneut read-only bestaetigt.

## HTTP-Nachweis

Nach dem finalen Neustart liefern alle geprueften Routen HTTP 200:

| Route | Status | Bytes |
| --- | ---: | ---: |
| `/` | 200 | 68'461 |
| `/admin/sessions` | 200 | 69'560 |
| `/settings` | 200 | 69'532 |
| `/pause` | 200 | 62'338 |
| `/js/pausegame.js` | 200 | 42'645 |
| `/management-cockpit` | 200 | 69'480 |
| `/einkauf/aufriss` | 200 | 137'247 |
| `/einkauf/lieferanten` | 200 | 101'740 |
| `/logistik/materialdisposition` | 200 | 81'050 |

Der erste Aufruf von `/einkauf/aufriss` traf nach der Netzunterbrechung das
60-Sekunden-Prueflimit. Die gezielte Wiederholung lieferte HTTP 200 in 7,92 Sekunden.

## Bekannter Einkaufsstatus

Der SAP-only-Code ist produktiv. Die Produktivdatenbank enthaelt noch `45` historische
Regeln mit Quelle `zdispo_grp.xlsx + zdispo_spart.xlsx`; der neue Code wertet diese
absichtlich nicht mehr aus. Das produktive SAP-Metadata enthielt beim letzten Live-Check
weiterhin keines der benoetigten Sets `ZDISPO_GRP`/`ZDISPO_SPART`.

Folge bis zur SAP-Aktivierung:

- Produktgruppennamen aus der alten Excel-Zuordnung werden nicht angezeigt;
- Einkauf-Full-Load und Nacht-Delta koennen beim fehlenden SAP-Set nicht erfolgreich
  abschliessen;
- Spend-, Bestell- und Materialdaten bleiben vorhanden; betroffen ist die neue
  Produktgruppen-Zuordnung.

Naechster Schritt: SAP-Sets aktivieren, Metadata und je eine Datenantwort pruefen,
danach einen Einkauf-Delta-Lauf starten. Erfolgsnachweis sind Regeln groesser null
mit `Source` beginnend mit `SAP OData:`.

## Wiederaufnahme

Bei Chatabbruch zuerst diese Datei lesen. Danach sind
`docs/FINANCE_SUPPLIER_FALLBACK_UMSCHALTER_2026-08-11.md` und
`docs/PURCHASING_PRODUCT_GROUP_SAP_DIRECT_2026-08-11.md` die Fachdokumente.
