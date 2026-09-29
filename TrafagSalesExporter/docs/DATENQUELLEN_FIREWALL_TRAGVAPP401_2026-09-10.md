# Datenquellen und Firewall-Ziele fuer `tragvapp401`

Stand: 2026-09-10. Quellserver der Anwendung:
`tragvapp401.trafagch.local` / `10.120.1.17`.

Die Endpunkte wurden am 10.09.2026 rein lesend aus der produktiven Konfiguration und
aus den im Produktivcode fest verdrahteten Diensten ermittelt. Zugangsdaten, Tenant-ID
und Client-ID sind bewusst nicht enthalten.

## Externe Ziele: fuer Firewall-/Bandbreitenausnahme relevant

| Ziel / FQDN | Protokoll / Port | Verwendung | Prioritaet |
|---|---:|---|---|
| `login.microsoftonline.com` | HTTPS / TCP 443 | OAuth-Anmeldung der Anwendung fuer Microsoft Graph | erforderlich fuer SharePoint |
| `graph.microsoft.com` | HTTPS / TCP 443 | Dateien in SharePoint auflisten, herunterladen und hochladen | erforderlich fuer SharePoint |
| `trafagag.sharepoint.com` | HTTPS / TCP 443 | Produktiver Standort `WorldwideBIPlatform`; Import- und Exportdateien | erforderlich fuer SharePoint |
| `*.sharepoint.com` | HTTPS / TCP 443 | Von Microsoft verwendete SharePoint-Content- und Weiterleitungsziele | Microsoft-empfohlene Freigabe |
| `*.sharepointonline.com` | HTTPS / TCP 443 | SharePoint-Online-Dienste | Microsoft-empfohlene Freigabe |
| `www.ecb.europa.eu` | HTTPS / TCP 443 | Taegliche EZB-Wechselkurse, Datei `/stats/eurofxref/eurofxref-daily.xml` | erforderlich fuer Kursimport |
| `20.197.20.60` | TCP 30015 | HANA-Datenquelle Indien, Schema `TRAFAG_LIVE` | erforderlich fuer TRIN |

Bei einer Firewall, die Microsoft-365-Zielnetze ueber IP-Listen statt FQDN fuehrt,
sollten die aktuellen Microsoft-365-Endpunkte automatisiert aus dem offiziellen
Microsoft-Webservice bezogen werden. Microsoft aktualisiert diese Liste regelmaessig;
eine statische IP-Liste in dieser Projektdokumentation waere schnell veraltet.

## Interne Ziele

| Ziel / FQDN | Protokoll / Port | Verwendung |
|---|---:|---|
| `travtrp0.TRAFAGCH.local` / `10.194.65.22` | TCP 30015 | Zentrales SAP-B1/HANA fuer TRFR, TRIT und TRUS |
| `travp762.sap.trafag.com` / `10.194.64.30` | HTTP / TCP 8000 | SAP-Gateway/OData `ZPOWERBI_EINKAUF_SRV` fuer CH/AT und Einkauf |

Produktive SAP-URL:
`http://travp762.sap.trafag.com:8000/sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/`

Produktive SharePoint-Site:
`https://trafagag.sharepoint.com/sites/WorldwideBIPlatform/`

Verwendete Unterordner sind unter anderem:

- `/Import/Finance/Deutschland/AlphaplanRaw`
- `/Import/Finance/Spanien`
- `/Import/Finance/UK_B1`
- `/Import/Finance/Alle`

## Was eine Ausnahme voraussichtlich beschleunigt

Die Ausnahme betrifft den Aufbau zu den Quellsystemen und den Transfer grosser Dateien:

- HANA-Abfragen aus Indien sowie aus den B1-Gesellschaften,
- SAP-OData-Abfragen,
- SharePoint-Downloads und -Uploads,
- Abruf der EZB-Wechselkurse.

## Was getrennt gemessen werden muss

Normale Dashboardauswertungen lesen nach dem Import ueberwiegend aus der zentralen
SQLite-Datenbank auf `tragvapp401`. Wenn bereits geladene Seiten, Filter oder lokale
Aggregationen langsam sind, laeuft diese Zeit nicht ueber die Internetdatenquellen.
Dann muessen CPU, RAM, Datentraeger-I/O, SQLite-Abfrageplan und Anwendungs-Timings auf
dem Server getrennt gemessen werden.

Ein bereits belegter Sonderfall ist ein Auswertungswerkzeug vom Arbeitsplatz ueber die
SMB-Freigabe: derselbe Lauf brauchte direkt ueber SMB mehr als zehn Minuten, mit lokaler
Datenbankkopie rund vier Minuten. Das belegt eine Netzwerkgrenze fuer SMB, sagt aber noch
nicht, ob eine langsame Web-Dashboardabfrage dieselbe Ursache hat.

## Vorschlag fuer den Nachweis nach der Firewall-Aenderung

Vorher und nachher jeweils vom Server messen:

1. DNS-Aufloesung und TCP-Verbindungsaufbau je Ziel,
2. Dauer eines kleinen Quelltests (`SELECT 1`, OData-Metadaten, Graph-Sitezugriff,
   EZB-Datei),
3. Dauer eines echten Standortimports mit gleicher Datenmenge,
4. Dauer einer bereits geladenen Dashboardabfrage ohne Quellzugriff.

Damit ist sichtbar, ob die Ausnahme den Quelltransfer verbessert und ob daneben eine
lokale Serverressource limitiert.

## Quellen

- Produktive Tabellen `HanaServers`, `Sites`, `SourceSystemDefinitions` und
  `SharePointConfigs` in `trafag_exporter.db`, gelesen am 10.09.2026.
- `Services/SharePointUploadService.cs` fuer Microsoft Graph.
- `Services/ExchangeRateImportService.cs` fuer den EZB-Endpunkt.
- Microsoft: `https://learn.microsoft.com/microsoft-365/enterprise/urls-and-ip-address-ranges`
  fuer die laufend aktualisierten Microsoft-365-/SharePoint-Ziele.
