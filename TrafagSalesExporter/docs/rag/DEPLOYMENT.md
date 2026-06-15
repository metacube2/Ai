# RAG Deployment

Stand: 2026-06-15

## Kurzstand

- `TrafagSalesExporter` wird als ASP.NET/IIS-Webanwendung im bisherigen `BiDashboard`-Schema publiziert.
- Letzter dokumentierter Deploy: 2026-06-11 Einkaufs-Uebersetzungen, Commit `1dbaa66 Add purchasing translations`.
- Publish-Ziel: `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`.
- Letzter Deploy-Zeitstempel: `BiDashboard.dll` am `11.06.2026 12:30:27`.
- Letzte Validierung vor Deploy: sauberer Worktree `C:\TMP\trafag-translation-test-20260611\TrafagSalesExporter`, `dotnet test TrafagSalesExporter.sln --verbosity minimal`, Ergebnis `92/92` Tests gruen.
- Deploy-Ablauf: `app_offline.htm` gesetzt, `dotnet publish TrafagSalesExporter.csproj -c Release -o \\trch-webapp-bidashboard.trafagch.local\BiDashboard$ --no-restore`, danach `app_offline.htm` entfernt.
- Vorheriger Deploy 2026-06-11: Finance-Schulung/Dashboard-UI, Commit `f751295`, `BiDashboard.dll` `11.06.2026 12:04:53`.
- Naechster lokaler Deploy-Kandidat: neues Produktsparten-Mapping fuer den vollstaendigen SAP-OData-Referenzservice. Seed-Ziel: `ZSCHWEIZ` Quellen `Z:FinanzdataSchweizOeSet`, `P:ProductDivisionRefSet` aktiv, `M:ProductDivisionMapSet` inaktiv; aktiver Join nur `Z.Matnr=P.Matnr`, mit beidseitiger Matnr-Normalisierung im Import.
- Nicht ohne URL-Pruefung refreshen: Live-Check 2026-06-15 gegen die aktuell konfigurierte alte URL `ZPOWERBI_EINKAUF_SRV/ProductDivisionRefSet` ergab 42'501 Zeilen, alle `UNASS`, 0 `0008`. Erst neue SAP-Service-URL setzen/verifizieren.
- Nach Deploy dieses Stands und nach korrekter SAP-Service-URL muss `ZSCHWEIZ` neu exportiert/importiert werden, damit `CentralSalesRecords` die neuen direkten `P.*`-Produktfelder und Status `Übrige` erhaelt.
- Schutz im Code: SAP-Import bricht ab, wenn `ProductDivisionRefSet` eine grosse Referenz mit 0 zugeordneten Sparten liefert oder wenn ein SAP-Standort 0 Umsatzzeilen liefert; bestehende Dashboard-Daten werden dann nicht ueberschrieben.
- CH/AT-Import nach Deploy: `FetchedRecords=40'292`, `Assigned=36'953`, `UnassignedWithReference=0`.
- DB-Backup vor Produktsparten-Seed/Import: `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\trafag_exporter.db.before-productdivision-map-20260610-161022.bak`.
- Produktive India-DB-Konfiguration nach Seed: `TRIN -> SAGE -> 20.197.20.60:30015`, Schema `TRAFAG_LIVE`, User-Override `TRAFAGCONTROLS`.
- DB-Backup vor India-Seed: `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\trafag_exporter.db.before-india-sage-20260610-0825.bak`.
- Lokaler Uebergangsserver: `http://172.16.9.185:5000` im Trafag-Netz, IP kann wechseln.
- Lokale URLs bleiben `https://localhost:55415` und `http://localhost:55416`.
- Fuer andere PCs nutzt der Uebergang bewusst HTTP auf Port `5000`.

## Serverproblem

- Lokaler HTTPS-Smoke-Test per `Invoke-WebRequest` scheitert weiterhin mit Empfangs-/TLS-Fehler; Publish und Share-/DB-Pruefungen sind davon getrennt.
- Aelterer dokumentierter Befund: TLS fordert Client-Zertifikat.
- IT soll IIS SSL Settings pruefen: Client certificates `Ignore` oder hoechstens `Accept`, nicht `Require`.

## Upgreat Firewall

- Upgreat muss den neuen Webserver freischalten, nicht den lokalen Entwicklungs-PC.
- Webserver / Source:
  - `trch-webapp-bidashboard.trafagch.local`
  - `tragvapp401.trafagch.local`
  - `10.120.1.17`
- Bekannte Ziele:
  - HANA Internal / BI1: `10.194.65.22:30015`
  - India HANA: `20.197.20.60:30015`
  - SAP OData / ZSCHWEIZ: `10.194.64.29:8000`
  - SharePoint / Graph: `trafagag.sharepoint.com:443`
- Offen: vollstaendige Standortliste aus produktiver App-Konfiguration exportieren/pruefen.

## Rohquellen Nur Bei Bedarf

- IIS-Handoff: `docs/DEPLOYMENT_IIS_HANDOFF_2026-05-19.md`
- lokaler Server: `docs/LOCAL_DEV_SERVER_UEBERGANG_2026-05-21.md`
