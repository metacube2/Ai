# RAG Deployment

Stand: 2026-05-27

## Kurzstand

- `TrafagSalesExporter` wird als ASP.NET/IIS-Webanwendung im bisherigen `BiDashboard`-Schema publiziert.
- Lokaler Uebergangsserver: `http://172.16.9.185:5000` im Trafag-Netz, IP kann wechseln.
- Lokale URLs bleiben `https://localhost:55415` und `http://localhost:55416`.
- Fuer andere PCs nutzt der Uebergang bewusst HTTP auf Port `5000`.

## Serverproblem

- IIS/HTTPS blockiert vor der App.
- Dokumentierter Befund: TLS fordert Client-Zertifikat.
- IT soll IIS SSL Settings pruefen: Client certificates `Ignore` oder hoechstens `Accept`, nicht `Require`.

## Rohquellen Nur Bei Bedarf

- IIS-Handoff: `docs/DEPLOYMENT_IIS_HANDOFF_2026-05-19.md`
- lokaler Server: `docs/LOCAL_DEV_SERVER_UEBERGANG_2026-05-21.md`

