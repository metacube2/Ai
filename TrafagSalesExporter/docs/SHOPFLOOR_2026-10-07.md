# Operations (Shopfloor), Stand 2026-10-07

Das PPA-Shopfloor (Ersatz fuer das Excel "PPA-Shop-Floor-File-2026") laeuft jetzt im Cockpit als oberster Reiter
"Operations (Shopfloor)" (Route `/operations`). Das Python-Backend wurde in C# nachgebaut, die Oberflaeche
(HTML/JS/CSS) ist unveraendert uebernommen. Auf dem Server wird kein Python benoetigt.

Stand 2026-10-08: **produktiv seit 06:39** (`59d0531`), angesehen. Zugriff nur fuer `koi` (Ingo), bis die Leitung Produktion/Operations
freigibt (Abschnitt 3). SAP-Abgleich gebaut, aber ausgeschaltet bis der Transport `T76K912718` in P76 ist (Abschnitt 8).
*Ueberholt:* „nicht committet, Zugriff fuer niemanden" war der Stand des Worktrees.

## 1. Was uebernommen wurde

| Original (PPA-Shopfloor vom 2026-10-05) | Jetzt im Cockpit |
| --- | --- |
| `public/index.html`, `app.js`, `style.css`, `logo.png` | `wwwroot/shopfloor/` (4 kleine Anpassungen in `app.js`, siehe 5) |
| `server.py` (Listen, Tageswerte, Verlauf, Uebersicht, CSV, Schnittstelle) | `Services/Shopfloor/ShopfloorStore.cs`, Endpunkte in `ShopfloorEndpoints.cs` |
| `zd05.py` (Einkauf Fehlteile, Uebernahme Bemerkung/Liefertermin/Code, Kennzahl) | `ShopfloorZd05.cs` |
| `forecast.py` (Planauftraege, Planwerte, Einstellungen) | `ShopfloorForecast.cs` |
| `notify.py` (Verzeichnis, Empfaenger-Aufloesung, Postausgang, Mailtext) | `ShopfloorMail.cs` (ohne Versand, siehe 7) |
| `modules_config.py` (Module, Felder, Listen, Ablauf) | `Services/Shopfloor/shopfloor_config.json` |
| `data/shopfloor.db` | Startstand `Services/Shopfloor/seed/shopfloor.seed.db` |

Excel und CSV (ZD05-Drag-and-drop, ABC/XYZ, Planauftraege) liest weiterhin der Browser (`app.js`, XLSX ohne Bibliothek).
Der Server bekommt nur fertige Zeilen als JSON. Deshalb braucht es keinen Excel-Import in C#.

## 2. Endpunkte (alle unter `/shopfloor/api`, gleiche JSON-Formen wie `server.py`)

Lesen: `GET config`, `records?module=`, `daily?module=a,b&from=&to=`, `agenda?day=`, `history?module=&ref=`,
`export.csv?module=`, `zd05?day=`, `zd05/history`, `people`, `mail`, `mail/preview?id=`, `forecast?monday=`,
`integration/schema`. Neu: `whoami` (Windows-Konto fuer die Oberflaeche).

Schreiben: `POST records` (mit `id` = aendern, ohne = neu, vergibt die Nummer), `DELETE records/{id}` (setzt `deleted=1`),
`POST daily`, `POST zd05/import|row|week|abc`, `POST forecast/import|plan|settings`, `POST people`,
`POST mail/test|send|retry`, `POST integration/push` (Bearer-Token).

Fehler kommen wie im Original als `{"error": "..."}` mit 400 (fachlich), 401 (Token), 403 (Zugriff) oder 500.

## 3. Zugriffsregel (Freigabe durch die Leitung Produktion)

*Geaendert 2026-10-08 (Entscheid Ingo): Der Reiter ist fuer alle sichtbar, der Zugang laeuft ueber ein eigenes Login analog Finance/HR.*
Benutzer `Shopfloor:LoginUsername` (`operations`), Passwort nur als SHA-256-Hash in `Shopfloor:LoginPasswordHash` (wie HR KPI, Passwort kennt Ingo).
Nach der Anmeldung (`wwwroot/shopfloor/login.html`, `POST /shopfloor/api/login`) setzt der Server ein HttpOnly-Cookie `TrafagShopfloor`
(Data Protection, SameSite Strict, Pfad `/BiDashboard/shopfloor`, `LoginHours` 12 h). Ohne Zugriff leiten Seiten auf `login.html` um, die API
antwortet 403. Windows-Konten in `AllowedUsers` (`koi`) brauchen kein Login. Abmelden: `POST api/logout`. Code `Services/Shopfloor/ShopfloorLogin.cs`.
Die Regel unten (Freigabe Leitung Produktion vor Sichtbarkeit) ist damit fuer diesen Reiter durch Ingo entschieden; die Daten bleiben hinter dem Login.


Regel: Neue Reiter sind erst nach Freigabe durch die Leitung Produktion/Operations sichtbar. Deshalb ist der Standard
**gesperrt fuer alle**.

| Schluessel (appsettings, Abschnitt `Shopfloor`) | Standard | Bedeutung |
| --- | --- | --- |
| `AllowedUsers` | leer | Windows-Konten mit Zugriff. Eintrag `koi` passt auf jede Domaene, `TRAFAG\koi` nur auf diese. Gross/Klein egal. |
| `OpenForAll` | `false` | `true` = alle angemeldeten Benutzer. Erst nach der Freigabe setzen. |
| `ApiToken` | leer | Bearer-Token fuer `POST integration/push`. Leer = Schnittstelle gesperrt (immer 401). |
| `DatabasePath` | leer | Leer = `<ContentRoot>\shopfloor\shopfloor.db`. Relative Pfade gelten ab ContentRoot. |
| `BackupDirectory` | leer | Leer = `backups` neben der Datenbank. |
| `BackupKeepDays` | 60 | Aufbewahrung der Tagessicherungen. |
| `BaseUrl` | leer | Adresse der Oberflaeche fuer Links in Mails, z. B. `https://<server>/BiDashboard/shopfloor/index.html`. |

Wo die Sperre greift:

1. `UseShopfloorAccess()` (Middleware vor `UseStaticFiles`) liefert fuer **alle** Pfade unter `/shopfloor` (Dateien und API)
   `403`, solange das Konto nicht freigegeben ist. Sie authentifiziert selbst, weil `UseAuthentication` erst danach laeuft.
2. Jeder API-Endpunkt prueft nochmals im Endpoint-Filter (`Guard`).
3. Die Seite `/operations` zeigt statt des Iframes den Hinweis "Kein Zugriff ... Freigabe ... steht noch aus".
4. Der Menueeintrag selbst bleibt fuer alle sichtbar (das Menue kennt nur Richtlinien, keine Benutzerlisten). Wer noch
   keinen Zugriff hat, sieht beim Anklicken den Hinweis. Soll der Eintrag ganz verschwinden, muss die Menueanzeige um
   eine Pruefung auf `IShopfloorAccess` erweitert werden (nicht gemacht).

Ausnahme Schnittstelle: `POST /shopfloor/api/integration/push` braucht **keinen** freigegebenen Windows-Benutzer, sondern
das Bearer-Token (`Authorization: Bearer <ApiToken>`). Sonst koennte weder SAP noch das MES liefern. Ein falsches oder
fehlendes Token ergibt 401. Dafuer traegt die Gruppe `/shopfloor/api/integration` `AllowAnonymous()`, sonst wuerde die
`FallbackPolicy` des Cockpits ("angemeldet") den Aufruf vor der Token-Pruefung abweisen (im Endpunkt-Test mit denselben
Bausteinen wie `Program.cs` nachgestellt: Push ohne Login mit Token ergibt 200, die uebrige API ohne Login 403).
**Offen, nur auf dem Server pruefbar:** Das Cockpit laeuft mit Windows-Authentifizierung. IIS beantwortet einen Aufruf ohne
Windows-Login selbst mit 401, bevor die Anwendung ihn sieht. Damit SAP/MES per Token pushen koennen, muss IIS fuer genau
diesen Pfad (`BiDashboard/shopfloor/api/integration/push`) anonyme Authentifizierung zulassen (`<location>`-Regel) oder der
Aufrufer ein Windows-Konto mitsenden. Das ist erst fuer den zweiten Schritt (SAP) noetig. Dessen OData-Sets rufen `ApplyPush`
intern auf und brauchen den HTTP-Weg nicht.

Schutz vor Fremdaufrufen: Alle schreibenden Aufrufe (ausser Push) verlangen den Header `X-User`. Die Oberflaeche sendet ihn
immer. Eine fremde Webseite kann ihn ohne Vorabfrage (CORS) nicht setzen, damit kann sie nicht mit dem Windows-Login des
Benutzers schreiben.

Entwicklung: Mit `Security:DevelopmentBypass` heisst das Konto `DEV\TrafagDeveloper`. Zum Ausprobieren
`Shopfloor:AllowedUsers = ["TrafagDeveloper"]` setzen.

### Bearbeiter-Kennung

Das Original fragte beim ersten Start ein Kuerzel ab und schickte es als `X-User`. Jetzt gilt das **Windows-Konto ohne
Domaene, klein geschrieben** (`TRAFAG\Koi` ergibt `koi`) als Bearbeiter. Der Server ignoriert den Wert aus `X-User`. Die
Oberflaeche holt das Kuerzel beim Start von `api/whoami` und fragt nicht mehr danach. Die Schnittstelle schreibt als
`SAP-Schnittstelle`, `MES-Schnittstelle` usw. Alte Eintraege behalten ihr bisheriges Kuerzel. Fuer die Mail-Regel "wer selbst
aendert, bekommt keine Mail" muss im Personenverzeichnis das Kuerzel dem Windows-Konto entsprechen.

## 4. Datenbank und Sicherung

- Dieselben Tabellen wie im Original (`records`, `daily`, `history`, `zd05_rows`, `zd05_days`, `abc_xyz`, `zd05_week`,
  `fc_orders`, `fc_plan`, `fc_settings`, `fc_imports`, `people`, `mail_outbox`, `mail_state`). Die gelieferte `shopfloor.db`
  funktioniert unveraendert.
- **Startstand:** `Services/Shopfloor/seed/shopfloor.seed.db` (Stand Excel 2026-10-05, 2083 Eintraege, 2609 Tageswerte,
  191 ZD05-Tage) wird mit dem Deploy mitgeliefert, aber **nur kopiert, wenn die Betriebsdatenbank fehlt**. Spaetere Deploys
  ueberschreiben nie die Betriebsdatenbank, weil diese in `shopfloor\shopfloor.db` (nicht im Publish-Inhalt) liegt.
  Geprueft am Deploy-Werkzeug (`Tools/DeployConsole/DeployRunner.cs`): Es ruft `dotnet publish -o <Zielordner>` direkt auf den
  Server und lehnt Profile und `deleteexistingfiles` ab. Es loescht also nichts im Zielordner, so wie auch
  `trafag_exporter.db` den Deploy ueberlebt. Ein Probe-Publish bestaetigt: `Services/Shopfloor/shopfloor_config.json`,
  `Services/Shopfloor/seed/shopfloor.seed.db` und `wwwroot/shopfloor/*` liegen im Publish-Ergebnis, ein Ordner `shopfloor\`
  nicht. Wer den Zielordner anders deployt (Spiegeln mit Loeschen), muss `Shopfloor:DatabasePath` auf einen Pfad ausserhalb
  setzen, z. B. `D:\Data\Shopfloor\shopfloor.db`. Das Backup-Verfahren des Deploys (`DatabaseBackup`) sichert nur
  `trafag_exporter.db`, nicht die Shopfloor-Datenbank. Dafuer sorgt die Tagessicherung (siehe unten).
- Die Datenbank wird erst beim ersten Zugriff angelegt, nicht beim Start der Anwendung.
- **Sicherung:** `ShopfloorBackupService` prueft stuendlich und legt `shopfloor-JJJJ-MM-TT.db` im Sicherungsordner ab
  (SQLite-Backup, im Betrieb sicher), aeltere als `BackupKeepDays` (60) werden geloescht. Solange weder `AllowedUsers`
  noch `OpenForAll` gesetzt sind, laeuft keine Sicherung und es entsteht keine Datenbank.
- Schreibzugriffe laufen nacheinander in einer Transaktion (`BEGIN IMMEDIATE`), Lesezugriffe parallel (WAL).

## 5. Aenderungen an der Oberflaeche (`wwwroot/shopfloor/app.js`, `index.html`)

Minimal und im Kopf von `app.js` dokumentiert:

1. Alle API-Pfade relativ (`api/...` statt `/api/...`, 35 Stellen), `index.html` laedt `style.css` und `app.js` relativ. So
   loest alles unter `/BiDashboard/shopfloor/` auf.
2. Beim Start `GET api/whoami` setzt das Kuerzel; der Klick auf das Kuerzel oeffnet die Abfrage nur noch, falls das
   nicht gelang.
3. `X-User` wird immer gesendet (`-` wenn leer), weil der Server ihn als Pflichtheader prueft.
4. Der Link in der "In Outlook oeffnen"-Mail zeigt auf diese Seite (`origin + pathname`), nicht auf die Wurzel des Servers.

### 5.1 Cockpit-Design und Grafiken (Stand 2026-10-08, ersetzt das Trafag-Eigendesign aus dem Originalpaket)

Nur Frontend, keine Aenderung an C# oder an den JSON-Formen der API.

**Design.** `style.css` ist neu geschrieben: Schrift Open Sans (wie `App.razor`), Karten wie `MudPaper Outlined` (Linie, 4 bzw.
6 px Radius, kein Schatten), KPI-Kacheln wie `ll-kpi`, dichte Tabellen, Schaltflaechen und Felder mit Linie, Primaerfarbe aus
dem Cockpit. Alle Farben sind CSS-Variablen auf `:root`; Statusfarben und Hover werden per `color-mix` daraus abgeleitet.
Die festen Farbwerte in den SVG-Diagrammen von `app.js` wurden durch Klassen (`gx-*`) und Variablen ersetzt.

**Theme-Sync (`themeSync` am Anfang von `app.js`).** Die Seite laeuft im iframe derselben Herkunft und liest
`window.parent.document.documentElement`: `data-theme` (hell/dunkel) und `data-skin` (ci/classic), ersatzweise
`localStorage` `trafag-theme` / `trafag-skin`, Standard dunkel + ci. Zusaetzlich werden die echten, berechneten
`--mud-palette-*` Werte des Cockpits (background, surface, text-primary, text-secondary, lines-default, primary; damit auch der
Dimmer-Stand) auf `<html>` der Shopfloor-Seite kopiert. Aenderungen erkennt ein `MutationObserver` auf `<html>` und `<head>`
des Cockpits (MudBlazor schreibt sein Theme in ein `<style>`), dazu ein Poll alle 1,5 s. Ohne Cockpit (Direktaufruf) gelten die
Rueckfallwerte in `style.css` nach `data-theme`.

**Eingebettet.** `<html>` bekommt die Klasse `embedded`, wenn `window.parent !== window`. Dann entfaellt der Trafag-Logoblock
(das Cockpit zeigt das Logo in der Kopfleiste), die Modulnavigation bleibt. `Shopfloor.razor`: iframe `display:block`,
`height:calc(100vh - 80px)` (Kopfleiste 64 px + unteres Padding 16 px), transparenter Grund, kein Rand: keine doppelten Scrollbalken,
keine dunkle Luecke am unteren Rand.

**Grafiken** (Inline-SVG/CSS, offline, folgen dem Theme; Bausteine `gxRing`, `gxSpark`, `gxMiniBars`, `gxStack` in `app.js`):

| Ansicht | Grafik |
| --- | --- |
| Startseite, Kachel Pflichtpunkte | Fortschrittsring und je Pflichtpunkt ein Chip erfasst/offen |
| Startseite, Kachel Pendenzen | Statusband im Termin / ueberfaellig |
| Startseite, Kachel Rueckstaende | Mini-Balken SEH, TR5, TX, DW (rot = ueberfaellig Anteil) |
| Startseite, Kachel ueberfaellig | Mini-Balken der vier Module mit den meisten Ueberfaelligen |
| Startseite, Seitenleiste | Sparkline Bewertungskennzahl ZD05 (30 Importtage, Ziel gestrichelt), eigener Abruf `api/zd05/history`, optional |
| Tageswerte-Seiten (kind daily) | bis zu vier Verlaufs-Kacheln (Sparkline ueber die letzten 20 Erfassungen, Delta zum Vorwert), eigener Abruf `api/daily` der letzten 45 Tage |
| Listen mit Status | Statusband offen im Termin / ueberfaellig / erledigt ueber alle Eintraege des Moduls |
| ZD05 Einkauf | Sparkline in der Kennzahl-Kachel, Ring Positionen im Horizont, Mini-Balken Code 2 nach A/B/C, Statusband neu/unveraendert/erledigt; Verlaufsdiagramm mit beschrifteter Ziellinie 1.20 |
| Forecast | Balken Auslastung der Woche je Abteilung mit 100-%-Marke (zusaetzlich zur bestehenden Heatmap und zum Menge-gegen-Kapazitaet-Diagramm je Abteilung) |
| PPA-Grafik | Kacheln mit aktuellem Auftragsvorrat und Sparkline je Abteilung; Diagramme auf die gemeinsame Farbpalette (`--c1` ... `--c5`) umgestellt |

Nicht umgesetzt: ein Verlauf der Ueberfaelligen (die API liefert keine Historie dazu; Ersatz ist die ZD05-Kennzahl-Sparkline).
Geprueft wurde mit dem Demo-Backend des Originalpakets in Edge (hell und dunkel, alle 26 Routen ohne JS-Fehler); die echte Cockpit-Einbettung
(Aufruf `/operations`) ist noch nicht im laufenden System gesehen worden.

## 6. Konfiguration (Module, Felder, Listen) und Neuerzeugung

`Services/Shopfloor/shopfloor_config.json` wurde einmal aus `modules_config.py` erzeugt und enthaelt genau das, was
`server.py` unter `/api/config` auslieferte (Module, Auswahllisten, Agenda, Teilnehmer, geschlossene Status,
`forecast_prefix`, `notify_rules`). Die Datei wird unveraendert ausgeliefert und vom Server fuer Nummernkreise, Status,
Faelligkeit, CSV und Schnittstelle gelesen. Sie wird ab jetzt **direkt gepflegt**. Nach einer Aenderung die Anwendung neu
starten. Bestehende Daten bleiben erhalten.

Neu erzeugen aus der Python-Vorlage (nur noetig, wenn `modules_config.py` des Originalpakets neu geliefert wird):

```
python Services/Shopfloor/GenerateConfig.py <Ordner mit modules_config.py, forecast.py, notify.py> Services/Shopfloor/shopfloor_config.json
```

## 7. Nicht umgesetzt

- **Mailversand ueber Microsoft Graph oder SMTP.** Der Modus bleibt "aus" (`/api/mail` meldet `modus: aus, bereit: false`).
  Zustaendigkeits-Mails werden wie im Original in den Postausgang gelegt (Status "wartend" bzw. "keine Adresse") und lassen
  sich ueber "In Outlook oeffnen" (mailto) oder "Ansehen" (Vorschau) nutzen. Die Tagesuebersicht (`digest`) und die
  Versandschleife fehlen. **TODO:** Graph (App-Registrierung mit `Mail.Send`, Application Access Policy auf ein Postfach)
  oder SMTP-Relay als `IHostedService`, Einstellungen unter `Shopfloor:Mail`.
- **MES-Anbindung** und der MCP-Server (`connectors/mcp_shopfloor.py`, `sap_mes_sync.py`) aus dem Originalpaket. Die
  Schnittstelle `POST integration/push` ist aber vollstaendig vorhanden.
- Der Demo-Modus (`window.SF_DEMO`) ist in `app.js` noch enthalten, wird aber nie aktiviert.
- Menueeintrag fuer Nicht-Freigegebene ausblenden (siehe 3).

## 8. Zweiter Schritt: SAP-Befuellung

*Umgesetzt 2026-10-07:* `Services/Shopfloor/ShopfloorSapSync.cs` (Hintergrunddienst). Werktags zu `Shopfloor:SapSyncTimes` (05:45, 12:45) holt er
`ShopZd05Set` je Disponent aus `Shopfloor:Zd05Dispo` (001, 003; Werk `SapPlant` 1100) und `ShopAufSet` ab heute fuer `ForecastDays` (28) Tage und schreibt
beides ueber `ApplyPush(..., "SAP")`: ZD05 ersetzt den Tagesstand (Bemerkung/Liefertermin/Code uebernommen), die Auftraege ersetzen den Forecast-Bestand.
Damit entfallen der taegliche ZD05-Excel-Export und der COOIS-Export. Verbindung wie Logistik live/Einkauf (Site Einkauf, ZPOWERBI_EINKAUF_SRV).
Endpunkte: `GET sap/status`, `POST sap/sync` (Abgleich sofort). Schalter `Shopfloor:SapSyncEnabled` steht auf **false**, bis T76K912718 in P76 importiert ist.

**SAP-Seite (T76, Transport `T76K912718`):** Strukturen `ZSTR_SHOP_ZD05`, `ZSTR_SHOP_AUF`; `ZCL_ZPOWERBI_EINKAUF_MPC_EXT` DEFINE und
`ZCL_ZPOWERBI_EINKAUF_DPC_EXT` GET_ENTITYSET, Quelltext `docs/abap/ZSHOP_ADD.abap`. `ShopZd05Set` ist die Logik von ZD05 (`ZMM_CHECK_UMTERMINIERUNG`)
ohne ALV, Pflichtfilter Werks und Dispo; `ShopAufSet` = offene Planauftraege (BESKZ <> F, Eckend PEDTR) und Fertigungsauftraege (offene Menge, GLTRP),
Pflichtfilter Werks und Datum, 60 Tage. Gemessen T76: ShopAufSet 200, 300 KB, 3 s; ShopZd05Set Dispo 003 18 s, 001 14 s. Nach dem Import in P76:
`/IWFND/CACHE_CLEANUP` fuer `ZPOWERBI_EINKAUF_MDL` UND `/IWBEP/CACHE_CLEANUP` (sonst 404, siehe LEARNINGS 2026-10-07).
*Nachtrag 2026-10-08:* T76K912718 enthaelt jetzt auch den Code der Einkauf-Sets EinkKontraktSet/EinkMatLzSet; deren Strukturen liegen in T76K912724. **Import: zuerst T76K912724, dann T76K912718.**

*Vorbereitung (Stand vor dem Umsetzen):*

Die automatische Befuellung aus SAP kommt ueber neue OData-Sets `ShopZd05Set` (Report ZD05, Fehlteile) und `ShopAufSet`
(Plan-/Fertigungsauftraege). Sie rufen die Logik der Schnittstelle **ohne HTTP** auf:

```csharp
store.ApplyPush(new ShopfloorPush { Items = items }, "SAP");
```

`ShopfloorStore.ApplyPush(ShopfloorPush push, string source)` ist dieselbe Methode, die der Endpunkt `integration/push` nutzt.
Items wie im Body: `{ "module": "zd05", "day": "...", "rows": [...] }` (ersetzt den Tagesstand, Bemerkung, Liefertermin und
Code werden vom Vortag uebernommen), `{ "module": "planauftraege", "rows": [...] }`, Tageswerte mit `fields`, Listen mit
`match`/`create`. Geschrieben wird als `SAP-Schnittstelle`, die Felder erhalten den blauen SAP-Punkt. Der Store kommt
aus `ShopfloorStoreProvider.Store` (Singleton, per DI).

## 9. Abweichungen vom Python-Original

- Bearbeiter = Windows-Konto statt frei gewaehltem Kuerzel (siehe 3).
- Pflichtheader `X-User` fuer Aenderungen, Zugriffssperre, Token-Ausnahme fuer die Schnittstelle (neu).
- Ohne `settings.ini`: Token, Pfade und Sicherung stehen in `appsettings.json`. Kein Token wird mehr automatisch erzeugt.
- Mailversand und Tagesuebersicht fehlen (siehe 7).
- Unbekannte Routen unter `/shopfloor/api` liefern 404 ohne Text, nicht den SPA-Fallback der Startseite.
- Fehlertexte bei ungueltigen Zahlen (`/api/zd05/week`) sind eigene deutsche Texte statt Python-Meldungen.
- Zahlen im JSON koennen als Ganzzahl statt `x.0` erscheinen (kein Unterschied fuer die Oberflaeche).

## 10. Tests

`TrafagSalesExporter.Tests`: `ShopfloorZd05Tests` (Uebernahme Bemerkung/Liefertermin/Code, neu/erledigt, Kennzahl),
`ShopfloorForecastTests` (Abteilungszuordnung, Import-Ersatz im Datumsbereich, Planwerte), `ShopfloorStoreTests`
(Rundlauf Eintraege und Tageswerte, Push mit match/create, CSV, Mail-Postausgang, Startdatenbank, Sicherung),
`ShopfloorAccessTests` (freigegeben/nicht freigegeben, mit und ohne Domaene), `ShopfloorEndpointTests` (echter Kestrel-Host:
403, Pflichtheader, Token, Schreiben mit Windows-Konto).

Hinweis aus dem Aufbau: Lambdas fuer `MapPost` mit `async` muessen `async Task<IResult> (...)` heissen, sonst liefert die Pipeline
mit Endpoint-Filter einen leeren 200er (gemessen, vom Endpunkt-Test gefunden).

## 11. Einbau in `Program.cs` (*erledigt 2026-10-07*)

```csharp
using TrafagSalesExporter.Services.Shopfloor;                      // bei den usings
builder.Services.AddShopfloor(builder.Configuration);              // nach AddAuthorization(...)
app.UseShopfloorAccess();                                          // direkt VOR app.UseStaticFiles();
app.MapShopfloor();                                                // vor app.MapRazorComponents<...>()
```
