# RAG Deployment

Stand: 2026-07-31

## Kurzstand

- Letzter produktiv verifizierter Deploy: 2026-07-31, Commit `4498bd4`
  (auch Lieferantenzeilen der Spend-Matrix fett und `1.05rem`), `346/346`
  Tests gruen. `BiDashboard.dll` `31.07.2026 11:43:06`, `3'226'624` Bytes,
  SHA256 `E64BF04327D3FD7668D424C0FA52EC78A00F076E9118E253D57601730F24A247`;
  Release-Build und Server bitgleich. Produktiv-DB unveraendert,
  `app_offline.htm` entfernt, Port 443 offen, authentifizierter HTTPS-Aufruf
  `200`.

## Deployment-Historie

- Ersetzte Deploy-Zwischenstaende stehen in `lastchange.md` und den Archiven unter
  `docs/raw_md_archive/`; diese Kurzdatei fuehrt nur den aktuell verifizierten Deploy.

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
- historischer lokaler Uebergangsserver: `docs/LOCAL_DEV_SERVER_UEBERGANG_2026-05-21.md`
