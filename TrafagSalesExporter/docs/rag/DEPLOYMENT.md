# RAG Deployment

Stand: 2026-06-26

## Kurzstand

- `TrafagSalesExporter` wird als ASP.NET/IIS-Webanwendung im bisherigen `BiDashboard`-Schema publiziert.
- Letzter dokumentierter Deploy: 2026-06-26, Commits `6943a66`–`3d5a23d`.
- Publish-Ziel: `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`.
- Letzte Validierung vor Deploy: `dotnet test TrafagSalesExporter.sln --verbosity minimal`, Ergebnis `103/103` Tests gruen.
- Deploy-Ablauf 2026-06-26: `app_offline.htm` gesetzt, `dotnet publish TrafagSalesExporter.csproj -c Release -o \\trch-webapp-bidashboard.trafagch.local\BiDashboard$ --verbosity minimal`, danach `app_offline.htm` entfernt.
- Servercheck nach Deploy: `Test-Path ...\app_offline.htm` -> `False`; `Test-NetConnection trch-webapp-bidashboard.trafagch.local -Port 443` -> `TcpTestSucceeded : True`.
- Produktive DLL nach Deploy: `BiDashboard.dll`, Zeitstempel `26.06.2026 07:47:25`.
- Deployede Aenderungen: Einkauf MARA-MSTAE-Loeschkennzeichen; Alphaplan-PSCredential-Fix + datierte ZIPs; HomeRedirect NotFound-Handler; Schnellübersicht Sparten-Abdeckung (inkl. Uebrige) + Datenstand-Zeitzonen-Fix.
- Vorheriger Deploy: 2026-06-18 Einkaufsdashboard-Matrix und Einkaufsfilter, Commit `4f45805`, DLL `18.06.2026 09:29:11`.
- Vorheriger Deploy 2026-06-17: zentraler Finance-Audit-/Nachweisexport, Commit `65f2ded Upload central finance audit exports`.
- Vorheriger Deploy 2026-06-16: HR-Admin, Finance-3D-Spartenkreis und Gruppenmarge.
- Vorheriger Deploy 2026-06-11: Finance-Schulung/Dashboard-UI, Commit `f751295`, `BiDashboard.dll` `11.06.2026 12:04:53`.
- Naechster lokaler Deploy-Kandidat: neues Produktsparten-Mapping fuer den vollstaendigen SAP-OData-Referenzservice. Seed-Ziel: `ZSCHWEIZ` Quellen `Z:FinanzdataSchweizOeSet`, `P:ProductDivisionRefSet` aktiv, `M:ProductDivisionMapSet` inaktiv; aktiver Join nur `Z.Matnr=P.Matnr`, mit beidseitiger Matnr-Normalisierung im Import.
- OData nach SAP-Fix geprueft: `ZPOWERBI_EINKAUF_SRV/ProductDivisionRefSet` auf `travp762` liefert `48'897` Zeilen, `48'895` assigned, `8'715` Uebrige/`0008`, `2` UNASS. `FinanzdataSchweizOeSet` liefert `30'642` Zeilen fuer 2025 und `0` fuer 2026.
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
