# RAG Deployment

Stand: 2026-06-10

## Kurzstand

- `TrafagSalesExporter` wird als ASP.NET/IIS-Webanwendung im bisherigen `BiDashboard`-Schema publiziert.
- Letzter dokumentierter Deploy: 2026-06-10 Produktsparten-Fallback `ProductDivisionMapSet`.
- Publish-Ziel: `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`.
- Letzter Deploy-Zeitstempel: `BiDashboard.dll` am `10.06.2026 16:09:44`.
- Produktive CH/AT-DB-Konfiguration nach Seed: `ZSCHWEIZ` Quellen `Z:FinanzdataSchweizOeSet`, `P:ProductDivisionRefSet`, `M:ProductDivisionMapSet`; Joins `Z.Matnr=P.Matnr` und `Z.Prodh=M.Paph1`.
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
