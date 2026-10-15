# Erste Fiori-App (Harness für künftige Erfassungs-Apps)

Stand: 2026-10-15, App in T76 hochgeladen und im Browser geprueft: zeigt 50 Zeilen (Schritt 1 erledigt). Auftrag Ingo: „erstelle mal ein kleines Fiori-App, es geht darum, dass wir bereit sind, wenn
Erfassungsprogramme kommen, und du daraus lernst“. Erster Schritt laut Ingo: nur öffnen können, sehen wo und wie
Daten gezogen werden. Nur T76, `$TMP`, kein Transport, nur lesen.

## 1. Was eine Fiori-App ist und wo sie läuft

- Eine Fiori-App ist eine Webseite (SAPUI5: HTML, XML-Views, JavaScript), die **im Browser** läuft. Die Dateien liegen
  im SAP als BSP-Anwendung im UI5-Repository und werden vom SAP-Webserver (ICM) ausgeliefert.
- Die Daten holt sie über einen **OData-Dienst** auf demselben Server (SEGW, `/sap/opu/odata/sap/<DIENST>/`). Weil App
  und Dienst auf demselben Host liegen, gibt es kein CORS-Problem. Ein Skript-Login ist nicht nötig (und wegen
  Sperrgefahr verboten, siehe Memory „SAP Gateway Abfragen“).
- **Login (Korrektur Ingo 2026-10-15):** Im Browser meldet man sich mit dem **OData-Login** an, **nicht** mit dem
  SAP-GUI-Login von T76. Meine frühere Aussage „KOI mit T76-Passwort“ war falsch. Laut Gateway-Fehlerprotokoll (`/IWFND/SU_ERRLOG`, 2026-10-15) ist das der
  Benutzer **`POWERBI`** (`/sap/bc/ui2/start_up` gibt fuer ihn 403).
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
| B. RFC-Baustein `/UI5/REPO_LOAD_FROM_ZIP` (**benutzt, 2026-10-15**) | RFC-fähig, nimmt `IV_ZIP_ARCHIVE` (XSTRING), `IV_REPOSITORY_NAME`, `IV_REPOSITORY_DESCRIPTION`, `IV_PACKAGE`, `IV_TRANSPORT_REQUEST`, `IV_CODE_PAGE`, `IV_TEST_MODE`, `IV_SAFE_MODE`, `IV_DIALOG_MODE`; gibt `ES_OUTCOME`, `ET_LOG`, `ET_LOG_MESSAGES` | skriptbar ohne Dialog und ohne HTTP-Login; SapProbe `rfc-call` übergibt Dateien als XSTRING mit `--set "NAME=@datei:<Pfad>"` (nur mit `--confirm-write` oder `--dry-run`) |
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
   Ingo gab Weg B frei („mach weg b“). Der Auto-Modus lehnte den echten Upload trotzdem ab („Remote Shell Writes“);
   Ingo schaltete auf den manuellen Modus, danach lief er.
6. SapProbe erweitert (`@datei:`) und neu gebaut. Der Build im Ordner `.tmp_sap_probe` scheitert an alten Resten
   (`obj/Release` und `obj/x86/Release` werden beide mitkompiliert, CS0579 doppelte Attribute); gebaut in einem sauberen
   Ordner (nur `SapProbe.csproj` und `Program.cs`), Exe ersetzt, alte als `SapProbe.exe.bak_20261015` gesichert.
7. Upload in drei Stufen: `--dry-run` (nichts an SAP), dann `IV_TEST_MODE=X` (SAP prüft, schreibt nicht, Ergebnis S),
   dann echt. Ergebnis `S /UI5/UI5_REP_LOAD 001`, 5 Dateien, Repository neu in `$TMP`, Anwendungsindex
   `apps/trafag.erste/app/sap/zfi_erste_app/` geschrieben. ICF-Knoten `ZFI_ERSTE_APP` aktiv (`ICFSERVLOC`).
   `O2APPL` zeigt den Namen nicht (UI5-Repositorys stehen dort offenbar nicht; kein Fehler).
8. Vorher öffnete Ingo den Link schon und bekam „ICF Node NOT found“: das heißt nur „noch nicht hochgeladen“.
9. Erster Browser-Test (Ingo, dann ich in Chrome mit seiner Sitzung): nur Kopfzeile, Inhalt leer. Ursachen per
   JavaScript in der Seite gemessen:
   - Inhaltsbereich der `sap.m.Page` 0 px hoch: Page ohne `sap.m.App` und ComponentContainer ohne `data-height="100%"`
     (Container 48 px). Behoben: `<App>` um die Page, `data-height="100%"`, `html, body { height: 100% }`.
   - `ProductDivisionRefSet` ignoriert `$top`: 48'838 Zeilen statt 50. Die App kuerzt jetzt selbst und zeigt eine Warnung.
   - SAP liefert UI5-Repository-Dateien mit `Cache-Control: max-age=31536000, public` (ein Jahr). Nach dem neuen Upload
     holte Chrome trotz Strg+Umschalt+R die alte View aus dem Cache (transferSize 0). Geloest mit
     `fetch(<datei>, {cache: "reload"})` je Datei und Neuladen.
10. Ergebnis: Tabelle mit 50 Zeilen (Matnr, Maktx, Paph1, Paph1Text, Wwpfa, WwpfaText, Wwpsp, WwpspText, IsAssigned),
    gelbe Warnung zu `$top`. Upload zweimal im Deltamodus wiederholt (nur geaenderte Dateien).

Befehl zum erneuten Hochladen (aus PowerShell; ZIP mit Python bauen, siehe oben):

```powershell
& powershell -ExecutionPolicy Bypass -File .tmp_sap_probe\RunSapProbe.ps1 rfc-call /UI5/REPO_LOAD_FROM_ZIP `
  --set "IV_ZIP_ARCHIVE=@datei:<zip>" --set "IV_REPOSITORY_NAME=ZFI_ERSTE_APP" `
  --set "IV_REPOSITORY_DESCRIPTION=Trafag Fiori Test" --set "IV_PACKAGE=`$TMP" --set "IV_CODE_PAGE=UTF-8" `
  --set "IV_TEST_MODE=X" --set "IV_DIALOG_MODE= " --confirm-write --max-table-rows 60 --quiet
```

Zuerst mit `IV_TEST_MODE=X`, dann mit `IV_TEST_MODE= `. Für einen Transport `IV_PACKAGE` und `IV_TRANSPORT_REQUEST` setzen.

## 6. Für die Erfassungs-Apps später (Harness)

Wo man selbst entwickelt (Übersicht für Ingo, 2026-10-15):

| Was | Wo |
| --- | --- |
| App-Oberfläche | VS Code mit „SAP Fiori tools“ (Generator aus einem OData-Dienst) oder SAP Business Application Studio (BTP) |
| OData-Dienst klassisch | SEGW; registrieren in `/IWFND/MAINT_SERVICE`; Fehler `/IWFND/ERROR_LOG`; testen `/IWFND/GW_CLIENT` |
| OData-Dienst modern (RAP) | Eclipse mit ADT: CDS, Behavior Definition, Service Binding |
| App hochladen | `/UI5/UI5_REPOSITORY_LOAD` (SE38), VS Code „Deploy“ oder RFC `/UI5/REPO_LOAD_FROM_ZIP` |
| App im SAP ansehen | SE80, BSP-Applikation `ZFI_ERSTE_APP`; Webknoten in SICF unter `/sap/bc/ui5_ui5/sap/` |
| Launchpad | `/UI2/FLPD_CUST` (Katalog, Kachel, Target Mapping), Rolle PFCG, Start `/UI2/FLP` |

- Schreiben braucht im SEGW-Dienst `CREATE_ENTITY` / `UPDATE_ENTITY` in `DPC_EXT`, im UI5 `oModel.create/update`
  bzw. `submitChanges`; CSRF-Token holt das ODataModel selbst.
- Für S/4 2023 wäre RAP (CDS, Behavior Definition, Service Binding, OData V4, Fiori Elements) der moderne Weg; der
  braucht Eclipse mit ADT. Ob ADT installiert ist und ob Ingo damit arbeiten will, ist offen.
- Berechtigung: Nutzer brauchen `S_SERVICE` für den Dienst und für die Kachel einen Katalog in einer Rolle.
  Rollen ordne ich nur zu (Memory „SAP Rollen nur zuordnen“).
- Nach jedem neuen Upload: Browser-Cache leeren (F12 offen, Rechtsklick auf Neuladen, „Cache leeren und vollständig
  neu laden“), sonst sieht man den alten Stand. Für echte Apps einen Cache-Buster vorsehen (Launchpad macht das über
  den Anwendungsindex; bei Direktlink z. B. Versionsparameter oder `Component-preload.js` mit Build).
- Seitenrahmen immer mit `sap.m.App` (oder `Shell`) und Container `data-height="100%"`, sonst ist der Inhalt 0 px hoch.
- Eigene SEGW-Dienste: `$top/$skip` in `GET_ENTITYSET` selbst umsetzen (`io_tech_request_context->get_top/get_skip`);
  `ProductDivisionRefSet` tut es nicht.
- Prüfreihenfolge, wenn eine App nicht startet: Link direkt öffnen → F12-Konsole (404 auf Ressourcen = BSP-Name/Pfad,
  401/403 = Login/Berechtigung) → Dienst-URL `.../$metadata` im selben Browser → `/IWFND/ERROR_LOG`.
