# Erste Fiori-App (Harness für künftige Erfassungs-Apps)

Stand: 2026-10-15. Auftrag Ingo: „erstelle mal ein kleines Fiori-App, es geht darum, dass wir bereit sind, wenn
Erfassungsprogramme kommen, und du daraus lernst“. Erster Schritt laut Ingo: nur öffnen können, sehen wo und wie
Daten gezogen werden. Nur T76, `$TMP`, kein Transport, nur lesen.

## 1. Was eine Fiori-App ist und wo sie läuft

- Eine Fiori-App ist eine Webseite (SAPUI5: HTML, XML-Views, JavaScript), die **im Browser** läuft. Die Dateien liegen
  im SAP als BSP-Anwendung im UI5-Repository und werden vom SAP-Webserver (ICM) ausgeliefert.
- Die Daten holt sie über einen **OData-Dienst** auf demselben Server (SEGW, `/sap/opu/odata/sap/<DIENST>/`). Weil App
  und Dienst auf demselben Host liegen, gibt es kein CORS-Problem, und der Benutzer meldet sich mit seinem normalen
  SAP-Login im Browser an. Ein Skript-Login ist nicht nötig (und wegen Sperrgefahr verboten, siehe Memory „SAP Gateway Abfragen“).
- Öffnen ohne Launchpad, direkt per Link:
  `http://travt762.sap.trafag.com:8000/sap/bc/ui5_ui5/sap/zfi_erste_app/index.html?sap-client=100`
- Später mit Launchpad: Kachel über `/UI2/FLPD_CUST` (Katalog, Target Mapping) und Rolle, Aufruf `/UI2/FLP` bzw.
  `http://travt762.sap.trafag.com:8000/sap/bc/ui2/flp?sap-client=100`. Noch nicht eingerichtet.

## 2. Systemvoraussetzungen T76 (gemessen 2026-10-15, nur lesend per SapProbe)

| Prüfung | Ergebnis |
| --- | --- |
| Release (`CVERS`) | S4CORE 108 (S/4HANA 2023), SAP_BASIS 758, SAP_UI 758, SAP_GWFND 758, UIBAS001 758 |
| HTTP-Port (`icm/server_port_0`) | `PROT=HTTP,PORT=8000`, Host `travt762.sap.trafag.com` |
| ICF-Knoten (`ICFSERVLOC`) | `UI5_UI5` aktiv (unter `/sap/bc` und `/sap/public/bc`), `FLP` aktiv, `UI2` aktiv, `OPU` aktiv |
| Eigene BSP-Apps (`O2APPL` `Z%`) | keine (`TABLE_WITHOUT_DATA` heißt hier: leer, kein Fehler) |
| Launchpad-Transaktionen | `/UI2/FLP`, `/UI2/FLPD_CUST`, `/UI2/FLPD_CONF` vorhanden |
| Dienst `ZPOWERBI_EINKAUF_SRV` | registriert und aktiv (`/IWFND/I_MED_SRH`, `ZPOWERBI_EINKAUF_SRV_0001`, `A`) |
| Eclipse | `C:\Users\koi\eclipse` vorhanden (ob ADT installiert ist, nicht geprüft) |

## 3. Die App `ZFI_ERSTE_APP`

Quelle: `saptasks/fiori_erste_app/webapp/` (Component.js, manifest.json, index.html, view/Main.view.xml,
controller/Main.controller.js).

- UI5 wird vom SAP-Server geladen (`/sap/public/bc/ui5_ui5/resources/sap-ui-core.js`), nicht vom CDN.
- Datenquelle im `manifest.json`: `/sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/`, OData V2, Standardmodell.
- Liest `ProductDivisionRefSet` mit `$top=50` (Material und Produktsparte, keine Personendaten), nie ungefiltert.
- Spalten entstehen aus den gelieferten Feldern, damit nichts an geratenen Feldnamen hängt.
- Statuszeile oben: Anzahl Zeilen oder der SAP-Fehlertext aus `error.message.value`.
- Name: 13 Zeichen (die Grenze des UI5-Repositorys für BSP-Namen ist nicht nachgemessen; kurz halten).

## 4. Hochladen ins SAP: drei Wege

| Weg | Wie | Bewertung |
| --- | --- | --- |
| A. Standardreport `/UI5/UI5_REPOSITORY_LOAD` | SE38, Name `ZFI_ERSTE_APP`, „Hochladen“, F8, dann Windows-Ordnerdialog: `saptasks\fiori_erste_app\webapp` wählen, Paket `$TMP`, Codepage `UTF-8` | einfach von Hand; Ordnerdialog ist Windows, nicht per SAP-GUI-Scripting bedienbar |
| B. RFC-Baustein `/UI5/REPO_LOAD_FROM_ZIP` | RFC-fähig, nimmt `IV_ZIP_ARCHIVE` (XSTRING), `IV_REPOSITORY_NAME`, `IV_REPOSITORY_DESCRIPTION`, `IV_PACKAGE`, `IV_TRANSPORT_REQUEST`, `IV_CODE_PAGE`, `IV_TEST_MODE`, `IV_SAFE_MODE`, `IV_DIALOG_MODE`; gibt `ES_OUTCOME`, `ET_LOG`, `ET_LOG_MESSAGES` | skriptbar ohne Dialog und ohne HTTP-Login; braucht in SapProbe eine Übergabe von Dateiinhalt als XSTRING (noch nicht eingebaut) |
| C. `/UI5/UI5_REPOSITORY_LOAD_HTTP` | holt ein ZIP von einer URL | braucht einen Webserver, den SAP erreicht; verworfen |

Die ZIP muss Pfade mit `/` enthalten. `[IO.Compression.ZipFile]::CreateFromDirectory` aus Windows PowerShell 5.1
(.NET Framework) schreibt `\`; deshalb ZIP mit Python `zipfile` bauen (siehe Protokoll).

Klassen dahinter, falls ein eigener Report nötig wird: `/UI5/CL_REPOSITORY_LOAD` (ENUM `operation`: upload, download,
delete; `constructor( operation adapter dialog_mode safe_mode )`, `init( repository_name ) -> is_new package`,
`prepare_upload`, `execute( transport_request code_page )`) mit Adapter `/UI5/CL_REPOSITORY_LOAD_ZIP( zip TYPE REF TO cl_abap_zip )`.

## 5. Protokoll 2026-10-15

1. Voraussetzungen gemessen (Abschnitt 2).
2. `/UI5/UI5_REPOSITORY_LOAD` in eigener Sitzung 2 über SE38 geöffnet und das Selektionsbild gelesen, nicht ausgeführt:
   Felder `UI5REP`, Radio `UPLOAD/DOWNLOAD/DELETE`, `ADJLNNDS`; Hinweis „Standard-Codepage Cp1252, bei Problemen UTF-8“.
3. App-Dateien geschrieben und geprüft: manifest mit Node `JSON.parse`, View als XML, JS mit `node --check`.
4. ZIP gebaut (3'030 Byte, 5 Dateien).
5. Weg B angefangen: SapProbe `rfc-call` um Dateiwerte erweitert. Der Auto-Modus hat die zweite Änderung als
   „Remote Shell Writes“ abgelehnt; die erste Änderung wurde zurückgenommen, SapProbe ist unverändert.
   **Hochladen wartet auf Ingo**: entweder Weg A von Hand oder ausdrückliche Freigabe für Weg B.

## 6. Für die Erfassungs-Apps später (Harness)

- Schreiben braucht im SEGW-Dienst `CREATE_ENTITY` / `UPDATE_ENTITY` in `DPC_EXT`, im UI5 `oModel.create/update`
  bzw. `submitChanges`; CSRF-Token holt das ODataModel selbst.
- Für S/4 2023 wäre RAP (CDS, Behavior Definition, Service Binding, OData V4, Fiori Elements) der moderne Weg; der
  braucht Eclipse mit ADT. Ob ADT installiert ist und ob Ingo damit arbeiten will, ist offen.
- Berechtigung: Nutzer brauchen `S_SERVICE` für den Dienst und für die Kachel einen Katalog in einer Rolle.
  Rollen ordne ich nur zu (Memory „SAP Rollen nur zuordnen“).
- Prüfreihenfolge, wenn eine App nicht startet: Link direkt öffnen → F12-Konsole (404 auf Ressourcen = BSP-Name/Pfad,
  401/403 = Login/Berechtigung) → Dienst-URL `.../$metadata` im selben Browser → `/IWFND/ERROR_LOG`.
