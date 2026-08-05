# RAG Deployment

Stand: 2026-08-05

## Kurzstand

- Letzter produktiv verifizierter Deploy: **2026-08-05, Server-Analyse**, Commit `cc72e6d`
  auf Branch `feature/server-analyse-trin-salestype`
  (`ServerAnalysisBackgroundService`: lesende Diagnoseabfragen gegen Standort-B1,
  ausgeloest ueber eine Triggerdatei in `_analysis`), `385/385` Tests gruen.
  `BiDashboard.dll` `05.08.2026 10:59:50`, `4'037'632` Bytes, SHA256
  `56AFD5AF156CD496A0EF42DFC5CF2E1FA724299BB632F3202FE0132131161B41`;
  Release-Build und Server bitgleich. Produktiv-DB in Laenge und Schreibzeit
  unveraendert (`338'472'960` Bytes, `03.08.2026 12:26:05`), `app_offline.htm`
  gesetzt und wieder entfernt, `https://…/BiDashboard/` liefert HTTP `200`
  (64'755 Bytes). Wirknachweis: Triggerlauf 11:13 und 11:20 haben Ergebnisdateien
  erzeugt, Protokollkategorie `Server-Analyse` zeigt Start/Ende ohne Fehler.
  Details: `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md`.

- Deploy davor: 2026-08-03, Commit `9e28086`
  (`Logistik > Stuecklistenanalyse`: Top-Down-/Bottom-Up-Dashboard), `353/353`
  Tests gruen. `BiDashboard.dll` `03.08.2026 06:59:38`, `4'024'832` Bytes,
  SHA256 `8D5586E5536C83A9EDB409472C332D190488898C3FE8E8DB2097C3131779B554`;
  Release-Build und Server bitgleich. Produktiv-DB in Laenge, Schreibzeit und
  SHA256 unveraendert, `app_offline.htm` entfernt, Port 443 offen und der
  authentifizierte Aufruf von `/BiDashboard/logistik/stuecklistenanalyse`
  liefert HTTP `200`.

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
