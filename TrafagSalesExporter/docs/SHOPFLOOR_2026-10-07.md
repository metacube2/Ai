# Operations (Shopfloor), Grundstand 2026-10-07, nachgefuehrt bis 2026-11-19 (Abschnitt 27: Paket Fabio 19.11., deployed `591c96c`); davor bis 2026-11-17 abends (Abschnitt 26: Entscheide Fabio 17.11., AE TX = SAP, Rueckstandslisten Excel gegen Board gemessen, Plan Paket B/C; Abschnitt 25: Ergaenzung Fabio 17.11., Schnittstellen-Seite, TX-Mengen aus dem MES, Rueckstandslisten geprueft; Abschnitt 24: CO-PA und Arbeitsplatzfilter M* ein; Abschnitt 23: Layoutfilter eingeschaltet; Abschnitt 22: Paket Fabio 13.11.)

*Titel am 2026-11-12 (Doku-Pruefung) ergaenzt; bis dahin „Operations (Shopfloor), Stand 2026-10-07“. Die Stand-Zeilen unten
stehen nicht in zeitlicher Folge; die juengste ist die vom 2026-11-17.*

Stand 2026-11-17 abends (Abschnitt 26): Fabios Entscheide vom 17.11. aufgenommen (Offen nach Abschlussstatus fuer Liste und PPD, Excel bis Go-live 01.01.2027 fuehrend, Schichtmodell, Arbeitskalender, FPY TX nach Michaels Auswahl); AE TX 12.11. stimmt mit SAP; aktuelles Excel hat 68 Eintraege mehr als das Board und 160 Eintraege mit Abschlussdatum ohne abgeschlossenen Status; Bau erst nach dem ZC22-Abgleich 18.11. *Damit ist der Stand 25 unten in den Punkten Dispo-Entscheide und Feiertage ueberholt.*

Stand 2026-11-17 nachmittags (Abschnitt 25): Schnittstellen-Seite auf dem echten Stand je Feld, Bestellnummer Einkauf Handeingabe, TX-Mengen aus dem MES (Prozedur in `TMS_MES`), Rueckstandslisten ausgewertet (alle 596 Eintraege noch Excel-Import, 123 offene mit Abschlussdatum), deployed 14:36 (`32bf356`). Davor Stand 2026-11-17 (Abschnitt 24): **CO-PA schreibt** (`SapCoPaEnabled`, Verkauf AE/Umsatz IST nach HZDAT, 02. bis 16.11. nachgeladen,
gleich Fabios Kontrollwerten ausser AE vom 12.11.) und **der Arbeitsplatzfilter `M*` ist ein** (ganzes ZC22-Layout `/ P00 TX`),
deployed 12:25 (`3298c93`), seit 12:43 im gemeinsamen Stand `2e62163`. Erster Stichtag mit dem ganzen Layout ist der 18.11.
Phase C wartet auf Fabios Antworten (Liste am Ende von Abschnitt 24). *Ueberholt damit in der Zeile darunter: „Aus bleiben
der Arbeitsplatzfilter `M*` … und `SapCoPaEnabled`“.*

Das PPA-Shopfloor (Ersatz fuer das Excel "PPA-Shop-Floor-File-2026") laeuft jetzt im Cockpit als oberster Reiter
"Operations (Shopfloor)" (Route `/operations`). Das Python-Backend wurde in C# nachgebaut, die Oberflaeche
(HTML/JS/CSS) ist unveraendert uebernommen. Auf dem Server wird kein Python benoetigt.

Stand 2026-11-16 (Abschnitt 23): `T76K912906` ist in P76. **Statusfilter wie ZC22-Layout (`Zc22StatusAusschluss`) und
Materialfilter wie ZC27-Layout (`RmMaterialAusschluss`) sind eingeschaltet**, deployed 17:06 (`7cc1a62`); Abnahme in P76 ohne
Abweichung, Logistik 02. bis 12.11. nachgeladen (Fabios 18 Kontrollwerte stehen gespeichert). Aus bleiben der Arbeitsplatzfilter
`M*` (DW liegt deshalb 540 Stk. ueber der Liste der Teamleiter) und `SapCoPaEnabled`. Erster Stichtag mit Statusfilter ist der
17.11. Davor Stand 2026-11-13 mittags (Abschnitt 22): Paket Fabio 13.11. Phase A deployed (`d754a76`), Layoutfilter gebaut und mit
Schalter aus deployed (`9100537`). *Ueberholt damit in den Zeilen darunter: „ohne Materialfilter“ (seit 13.11. 12:22 Material 1 bis
99999 fuer TR5 und DW) und „CO-PA weiter aus“ gilt nur noch fuer das Schreiben, die Vorschau rechnet mit HZDAT.*

Stand 2026-11-13 (Mail Fabio 13.11., Abschnitt 21): Vorrat, offene KW, SEH-Tagesziel, TX `plan5` und PPD-Verzug sind
**Stichtagswerte**, es gilt der erste erfolgreiche SAP-Lauf des Tages; Morgenwerte 10. bis 12.11. aus dem Verlauf zurueckgeholt;
**PPD-Verzug eingeschaltet, ohne Materialfilter**; Cache-Busting in `index.html`. Deployed 07:56 (`0e94279`). *Ueberholt damit in der
Zeile darunter: „`SapVerzugEnabled` steht auf aus“ (CO-PA weiter aus).*

Stand 2026-11-10 (Zeile eingefuegt bei der Doku-Pruefung am 2026-11-12): Paket Fabio vom 07.11. eingespielt (Patch 06 und 07,
Abschnitt 18); Auftragsvorrat aus SAP wie ZC22 seit 09.11. 15:11 eingeschaltet (`Shopfloor:SapVorratEnabled`, Abschnitt 19);
DW lieferbereit aus DLQ72, Patch 07 auch im Backend, TX `plan5`, Verzug-Materialfilter schaltbar und CO-PA (`ShopfloorSapCoPa`)
gebaut, die Schalter `SapVerzugEnabled` und `SapCoPaEnabled` stehen auf aus (Abschnitt 20). Stand 2026-11-02: PPD-Verzug aus
ZC22 gebaut, Schalter aus (Abschnitt 17).

Stand 2026-10-08: **produktiv seit 06:39** (`59d0531`), angesehen. Zugriff nur fuer `koi` (Ingo), bis die Leitung Produktion/Operations
freigibt (Abschnitt 3). SAP-Abgleich gebaut, aber ausgeschaltet bis der Transport `T76K912718` in P76 ist (Abschnitt 8).
*Ueberholt:* „nicht committet, Zugriff fuer niemanden" war der Stand des Worktrees.
*Ueberholt am 2026-10-29:* „Zugriff nur fuer `koi`" und „SAP-Abgleich ausgeschaltet": seit 2026-10-16 `OpenForAll = true` (Abschnitt 3),
`SapSyncEnabled = true` seit 2026-10-09 (Abschnitt 8), Abgleichzeiten 04:00 und 12:45 (`appsettings.json`, Abschnitt 14).

Stand 2026-11-02: Fabios Paket vom 2026-11-01 eingespielt (Patch 04 Terminverschiebungen aus den Rückstandslisten, standardmässig aus; Patch 05;
Verkauf Transmitter = TR5 + TX, erwarteter Umsatz aus Fiori statt ZC23), J4 Verkauf aus `C_SALESANALYTICSQRY_1_CDS` gebaut, KEA0 gelesen
(Ergebnisbereich 1000, `CE11000`, Produktsparte `WWPSP`). Abschnitt 16.
Stand 2026-10-15: Fabios Patches ZD05 „Zu besprechen“ und Verkauf Go-for-it uebernommen; die Schnittstelle nimmt seit `f4e3f1b` auch das Push-Format seiner Beispiele an (Abschnitt 13).
Stand 2026-10-14: Feld-Mapping aus `docs/Shopfloor_SAP_Feldmapping_2026-10-14.xlsx` uebernommen (`b613f7c`): Abteilungen ueber den
Disponenten, Quelle SAP statt MES ausser FPY, Mapping je Feld auf der Seite „Schnittstelle SAP / MES“. Einzelheiten Abschnitt 12.
*Erledigt 2026-10-14 10:04:* produktiv. Abgleich 10:05: 3'926 Auftraege im Forecast, **294 ohne Abteilung** (vorher 1'912), CZ 1'045,
SEH 656, DW 888, TX 542, TR5 501. Seit 2026-10-14 heisst der Reiter Operations > Shopfloor (Gruppe Operations mit KPI,
`docs/OPERATIONS_KPI_2026-10-14.md`).

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
Benutzer `Shopfloor:LoginUsername` (`operations`), Passwort nur als SHA-256-Hash in `Shopfloor:LoginPasswordHash` (wie HR KPI, Passwort kennt Ingo). *Ergänzt 2026-10-15:* Das Passwort setzt der Admin unter Admin Bereich > Passwörter; der Hash liegt dann in der Cockpit-DB (Tabelle `AccessPasswords`, Bereich `operations`) und hat Vorrang vor `appsettings.json`. Das Anmelde-Cookie trägt einen Fingerabdruck des Hashes, ein neues Passwort beendet also alle bestehenden Anmeldungen.
Nach der Anmeldung (`wwwroot/shopfloor/login.html`, `POST /shopfloor/api/login`) setzt der Server ein HttpOnly-Cookie `TrafagShopfloor`
(Data Protection, SameSite Strict, Pfad `/BiDashboard/shopfloor`, `LoginHours` 12 h). Ohne Zugriff leiten Seiten auf `login.html` um, die API
antwortet 403. Windows-Konten in `AllowedUsers` (`koi`) brauchen kein Login. Abmelden: `POST api/logout`. Code `Services/Shopfloor/ShopfloorLogin.cs`.
Die Regel unten (Freigabe Leitung Produktion vor Sichtbarkeit) ist damit fuer diesen Reiter durch Ingo entschieden; die Daten bleiben hinter dem Login.


Regel: Neue Reiter sind erst nach Freigabe durch die Leitung Produktion/Operations sichtbar. Deshalb ist der Standard
**gesperrt fuer alle**.

| Schluessel (appsettings, Abschnitt `Shopfloor`) | Standard | Bedeutung |
| --- | --- | --- |
| `AllowedUsers` | leer | Windows-Konten mit Zugriff. Eintrag `koi` passt auf jede Domaene, `TRAFAG\koi` nur auf diese. Gross/Klein egal. |
| `OpenForAll` | `false` | `true` = alle angemeldeten Benutzer. Erst nach der Freigabe setzen. *Seit 2026-10-15 (`8bfe424`, Wunsch Fabio):* Admin Bereich > Passwörter, Schalter „Operations: Login nötig“; der Wert liegt in der Cockpit-DB (Tabelle `AccessSettings`, Schlüssel `operations.login_required`) und hat Vorrang vor diesem Eintrag. Aus = alle Windows-Benutzer sehen und bearbeiten den Shopfloor. Freigabe der Produktionsleitung dafuer laut Ingo am 2026-10-15 erteilt; produktiv seit 12:15. *Seit 2026-10-16 07:48 (`70a02b1`, Auftrag Ingo):* `OpenForAll` = `true` in `appsettings.json`, Operations ohne Login fuer alle Windows-Benutzer (Admin-Schalter nicht gesetzt, er haette Vorrang). |
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
(Aufruf `/operations`) ist noch nicht im laufenden System gesehen worden. *(ueberholt: seit 2026-10-08 produktiv und angesehen, siehe Kopf)*

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
*Ueberholt: `SapSyncEnabled = true` seit 2026-10-09 (Nachtrag unten); Zeiten seit 2026-10-16 04:00 und 12:45 statt 05:45 (Abschnitt 14).*

**SAP-Seite (T76, Transport `T76K912718`):** Strukturen `ZSTR_SHOP_ZD05`, `ZSTR_SHOP_AUF`; `ZCL_ZPOWERBI_EINKAUF_MPC_EXT` DEFINE und
`ZCL_ZPOWERBI_EINKAUF_DPC_EXT` GET_ENTITYSET, Quelltext `docs/abap/ZSHOP_ADD.abap`. `ShopZd05Set` ist die Logik von ZD05 (`ZMM_CHECK_UMTERMINIERUNG`)
ohne ALV, Pflichtfilter Werks und Dispo; `ShopAufSet` = offene Planauftraege (BESKZ <> F, Eckend PEDTR) und Fertigungsauftraege (offene Menge, GLTRP),
Pflichtfilter Werks und Datum, 60 Tage. Gemessen T76: ShopAufSet 200, 300 KB, 3 s; ShopZd05Set Dispo 003 18 s, 001 14 s. Nach dem Import in P76:
`/IWFND/CACHE_CLEANUP` fuer `ZPOWERBI_EINKAUF_MDL` UND `/IWBEP/CACHE_CLEANUP` (sonst 404, siehe LEARNINGS 2026-10-07).
*Nachtrag 2026-10-08:* T76K912718 enthaelt jetzt auch den Code der Einkauf-Sets EinkKontraktSet/EinkMatLzSet; deren Strukturen liegen in T76K912724. **Import: zuerst T76K912724, dann T76K912718.**

*Nachtrag 2026-10-09:* Beide Auftraege von Ingo nach P76 importiert, Caches geleert. `Shopfloor:SapSyncEnabled` steht seit dem Deploy 11:32 (`2aea0ba`) auf **true**. Nach jedem App-Neustart holt der Dienst den juengsten faelligen Termin des Tages nach. Erster Lauf 11:33: `ShopZd05Set` 58 Positionen (29 fuer den Tag uebernommen), `ShopAufSet` 6'449 Auftraege, davon 3'858 im Forecast-Fenster 09.10. bis 06.11.; je Abteilung TX 539, DW 875, TR5 492, SEH 40, **ohne Abteilung („?“) 1'912**. Die Disponenten dieser Auftraege kommen in der Zuordnung `ShopfloorForecast.DefaultPrefix` nicht vor; haeufigste in der Woche ab 12.10.: 025 (91), SE1 (74), 024 (67), PKA (43), SL1 (42), SE2 (38), 019, 022, 018, SL3. Welche davon zu welcher Abteilung gehoeren, muss die Produktion festlegen (nicht geraten).
*Ueberholt 2026-10-14:* Die Zuordnung ist mit dem Feldmapping-Excel festgelegt (Disponentenliste von Fabio, 13.10.), siehe Abschnitt 12.

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

## 12. Feld-Mapping SAP/MES (Stand Excel 2026-10-14, `b613f7c`)

Quelle: `docs/Shopfloor_SAP_Feldmapping_2026-10-14.xlsx` (Kopie aus Ingos Downloads vom 2026-10-14 07:16). Blatt „Feld-Mapping“
mit 134 Schnittstellenfeldern, Blatt „Disponenten“ mit der SAP-Liste „Disponent fuer Auftrag“ (Screenshots Fabio, 13.10.).

**Festgelegt im Excel und uebernommen:**

1. **Abteilung ueber den Disponenten (Werk 1100):** SEH = S* (SB1-5, SE1-4, SK1-3, SL1-5, SPB), TR5 = EK*, TX = EL*, DW = D*,
   CZ = 017 bis 028 (exakte Codes, damit 010, 012, 013, 015, 016 nicht mitlaufen), MTS ohne Disponent. Steht in
   `forecast_prefix` von `shopfloor_config.json` (massgebend, `ShopfloorStore` liest es) und gleich in `ShopfloorForecast.DefaultPrefix`.
   *Geaendert:* 012 (PPD / Sensoren OEM) zaehlte bisher zu SEH (aus `forecast.py`), im Excel steht es auf „pruefen (SEH?)“ und ist jetzt
   ohne Abteilung. Ohne Abteilung bleiben ausserdem 001-005, 010, 012, 013, 015, 016, 030, 031, 099, A99, N15, PKA, TL1, TPT.
   Die gespeicherten Forecast-Auftraege bekommen die neue Abteilung erst beim naechsten SAP-Abgleich (05:45, 12:45 oder `POST sap/sync`; seit 2026-10-16 04:00 statt 05:45).
2. **Quelle:** aus dem MES kommen nur die fuenf FPY-Werte (`seh/tr5/tx/dw/cz.fpy`), alle anderen 129 Felder aus SAP. 26 Felder
   (Produzierte Mengen, CZ-Mengen, MTF-Verfuegbarkeit, Sensorbestaende) standen vorher auf `mes`.
3. **Mapping je Feld:** jedes Feld in `shopfloor_config.json` hat ein Objekt `sap` mit `vorschlag_tabelle`, `vorschlag_feld`,
   `vorschlag_odata` (Spalten G-I, Vorschlag nach SAP-Standard, **nicht geprueft**), `tabelle`, `feld` (Spalten J/K, Ingo), `selektion`,
   `status`, `bemerkung`. `GET integration/schema` liefert es mit, die Seite „Schnittstelle SAP / MES“ zeigt es (Vorschlaege kursiv).
   Uebernahme per Skript aus dem Excel (Zeilen ab 4, Schluessel Spalte D `modul.feld`, leere Werte weggelassen). Wenn Ingo die gelben
   Spalten J-N fuellt, das Excel neu ablegen und dieselbe Uebernahme wiederholen; die Konfiguration nicht von Hand auseinanderlaufen lassen.

**Offen (aus dem Excel):**

- Uebergabe taeglich 04:00 an `integration/push`: im Excel festgelegt, gebaut ist der Abruf durch die App (`ShopfloorSapSync`
  05:45 und 12:45 ueber OData). Ein echter Push aus SAP braucht Token und eine IIS-Ausnahme (Abschnitt 3). Entscheid Ingo offen:
  Abruf um 04:00 oder Push aus SAP. *(Entschieden 2026-10-16: Abruf durch die App um 04:00 und 12:45, Abschnitt 14 und 15)*
- Alle 134 Felder haben Status „offen“, die Spalten J-N sind leer. Fachlich ungeklaert: VkNr, Typencode, Z-Tabelle hinter ZC23 (*ueberholt 2026-11-02: umsatz_plan kommt aus Fiori statt ZC23, Abschnitt 16*),
  Aufteilung Disponent 024 (Stecker/Kabel/Print), CZ-Mengen ohne eigenen Disponenten (Ministaten, Navistaten, DTP 8180), DL790/DLQ72,
  Schichtzuordnung TX ueber die Rueckmeldezeit, Anlagenverfuegbarkeit MTF.
- Disponenten auf „pruefen“: EL1, EL4, EL5, EL6, EL8 (TX), DL6, DS1, DS2, DSN, DW1 (DW), SPB (SEH), sowie 010, 012, 013, PKA, TL1, TPT.

## 13. Paket Fabio vom 2026-10-14: Patches und Schnittstelle (Stand 2026-10-15)

Abgelegt unter `docs/shopfloor_paket_2026-10-14/` (Original aus Ingos Downloads, zwei identische ZIP-Kopien).

**Patches (nur Oberflaeche, in `wwwroot/shopfloor/app.js` und `style.css` uebernommen):** angewendet mit Fabios
`apply_patch.js` gegen die Repo-Fassung (gleich der produktiven), zuerst `--check`, jede Stelle genau einmal gefunden, Syntax ok;
die `.bak`-Dateien des Skripts wurden nicht ins Repo uebernommen.

1. **ZD05 „Zu besprechen“:** neuer Filter mit Anzahl. Zu besprechen ist eine Position, die neu seit dem letzten Import ist, noch
   keinen Code hat oder deren Liefertermin ueberschritten ist. Solche Zeilen haben einen orangen Rand und Markierungen
   („LT ueberfaellig“, „ohne Code“). Im laufenden Shop-Floor ist der Filter automatisch aktiv. Startseite Punkt ZD05 zeigt
   zusaetzlich „x ohne Code“.
2. **Verkauf Go-for-it:** Kacheln oben im Verkauf: Go-for-it-Ziel des Monats (einmal pro Monat im Feld „Ziel festlegen“), Umsatz
   IST in % vom Ziel, fehlender Betrag (mit geplantem Umsatz ZC23; *seit Patch 05 vom 2026-11-02 erwarteter Umsatz aus Fiori, Abschnitt 16*), Auftragseingang mit Book to Bill. Konvention: der Eintrag vom
   Tag D ist der Stand bis D-1, der Monatserste gehoert zum Vormonat; das Ziel steht im bestehenden Tageswert `goforit` (am 2. des
   Monats bzw. auf dem Tag, der es schon traegt). Kein Backend, keine neue Tabelle.

**Schnittstelle (`1_Schnittstelle`):** `Shopfloor_SAP_Schnittstelle.xlsx` und `feldliste.json` mit allen 134 Feldern, Jobs 04:00,
Verkauf ueber CO-PA (wie KE30 Z0001-000006, Nettoerloes, Satzart A bzw. F, Buchungskreis 1100; Sparte 0003 = DW, 0005 = Transmitter),
Logistik aus ZC27 (Vortag, Gutmenge je Arbeitsplatz, MLE08 + MLE09 summiert), MES nur FPY, CZ vorerst zurueckgestellt.
Noch nicht in `shopfloor_config.json` uebernommen (dort steht das Mapping aus dem Excel vom 2026-10-14, Abschnitt 12).
*Teilweise ueberholt 2026-10-16:* Fabios Config-Patch 04 aus dem Paket vom 2026-10-16 ist uebernommen (Abschnitt 14);
die Feldliste selbst ist weiter nicht 1:1 eingespielt.

**Abweichung der Beispiele vom echten Push-Format (wichtig fuer den SAP-Job):** Fabios `beispiele/*.json` haben die Form
`{"module", "day", "source", "values"}`. Die Schnittstelle des Cockpits (`ShopfloorStore.ApplyPush`) erwartet
`{"source": "SAP", "items": [{"module": "verkauf", "day": "2026-10-01", "fields": {...}}]}`: Huelle `items`, Schluessel **`fields`**
statt `values`. Adresse ist `POST /BiDashboard/shopfloor/api/integration/push` mit `Authorization: Bearer <Shopfloor:ApiToken>`,
nicht `/api/integration/push`. `connectors/README.md` gibt es im C#-Port nicht. Fabio informieren, bevor der Job gebaut wird.
*Ueberholt 2026-10-15 (`f4e3f1b`, Entscheid Ingo „ohne Fabio korrigieren“):* Die Schnittstelle nimmt Fabios Format jetzt zusaetzlich an:
ein einzelnes Item ohne `items`-Huelle, `values` gleichwertig zu `fields`, und sie ist auch unter `/BiDashboard/api/integration/push`
erreichbar. Token-Pruefung auf beiden Adressen unveraendert (`Authorization: Bearer <Shopfloor:ApiToken>`). Fabio braucht nur noch
Server-Adresse und Token. Alle Feldnamen seiner zwei Beispiele sind in der Konfiguration bekannt (geprueft 2026-10-15). Test
`Push_Im_Format_Aus_Fabios_Paket_Ueber_Beide_Adressen`.

## 14. Paket Fabio vom 2026-10-16: Patch 03 und Config-Patch 04 (Stand 2026-10-16)

Abgelegt unter `docs/shopfloor_paket_2026-10-16/` (aus `Shopfloor_Deploy_2026-10-16.zip`). Commit `b2103a4`.
**Produktiv seit 2026-10-16 07:06**, in Ingos Chrome geprueft: Lieferzeiten-Seite, Ampel-Karte, Forecast DS (DS1/DS2) getrennt,
DW1 ohne Abteilung. Speichertest mit Freigabe Ingo produktiv: Ampel SEH Orange am 16.10. gesetzt (blieb nach Neuladen, oranger Punkt
auf der Startseite) und per API mit `null` wieder entfernt (Zeile leer, Startseite wieder „noch nicht erfasst“, Verlauf zeigt beides).
*Ueberholt:* „in der Oberflaeche nicht loeschbar“. Seit `e6c007f` (produktiv 07:37) entfernt ein zweiter Klick auf die heute
gesetzte Farbe den heutigen Status (Hinweis im Tooltip); eine Farbe aus einem Vortag wird mit dem Klick fuer heute bestaetigt. Produktiv
geprueft: Rot gesetzt, nochmals geklickt, Zeile leer, Startseite „noch nicht erfasst“. DS zeigt „Leistung fehlt“,
bis die Leistung (Stk/FTE) im Forecast erfasst ist.

**Eingespielt mit Fabios Skripten, je zuerst `--check`:**

1. `3_Config/apply_config.js` auf `Services/Shopfloor/shopfloor_config.json`: PPD ohne `verzug_mts` (Summe `verzug_total` ohne
   MTS), Verlaufskacheln (`trend`) fuer SEH/TR5/TX/DW (Produktion, Vorrat 1 Mte, Vorrat total, offen KW, Leistung; SEH auch FPY)
   und PPD (Verzug und Terminverschiebungen total/SEH/TR5/TX/DW), Liste `ampel` (gruen/orange/rot), Felder `ampel`, `ampel_pct`,
   `ampel_text` in zehn Bereichen, `lz_grund` in SEH/TR5/TX/DW, Forecast-Praefixe DW und neu DS.
2. `2_Patch/apply_patch.js` auf `wwwroot/shopfloor`: 01 und 02 uebersprungen (schon drin), 03 „Wuensche D. Tobler“ angewendet
   (Verlaufskacheln, Ampel pro Bereich auf Abteilungsseite und Startseite, Menuepunkt „Lieferzeiten“, Forecast-Zeile DS).
   Die `.bak`-Dateien der Skripte sind nicht im Repo.

**Zwei Korrekturen, weil das Paket fuer Fabios Python-Fassung geschrieben ist:**

- Der C#-Port liest die Quelle eines Felds aus `src`, nicht aus `planned_source` (das gibt es nur in der Ausgabe von
  `/api/integration/schema`). Der Patch setzte bei `seh.fpy` nur `planned_source = sap`; jetzt `src = sap`, SAP-Hinweis auf
  `C_MFGORDERITEMSCRAPAMTQRY_CDS` (Job J11). Damit 129 Felder SAP und 4 MES (nur noch FPY TR5/TX/DW/CZ).
- Fabios DW-Liste endet mit dem Praefix `DW`; damit waere DW1 (Ruestpapiere) weiter bei DW gelandet, obwohl er nach dem Deploy
  pruefen will, dass DW1 nicht mehr zugeordnet ist (seine Checkliste nennt genau diese Abhilfe). `DW` ist aus der Liste entfernt;
  DW1 und DL6 erscheinen unter „nicht zugeordnet“, DW2 bleibt DW. `ShopfloorForecast.DefaultPrefix` und `Depts` sind
  nachgezogen (DS als Abteilung).

**Tests:** `ShopfloorForecastTests` (Zuordnung DS1/DS2/DSN = DS, DW1/DL6 = ?, DW2/DB1/D10 = DW; 129 SAP / 4 MES) und neu
`Ampel_Und_Lieferzeit_Grund_Werden_Gespeichert`. Release-Worktree `1240/1240`.

**Nicht Teil dieses Einspielens (Fabios Schritte 2 bis 8a):** Die Jobs J2 bis J11 (Verkauf CO-PA, Logistik nach ZC27, Tagesmengen,
Auftragsvorrat, FPY SEH) sind weder im Paket (kein Programm, nur Excel mit Quelle und Selektion) noch im Cockpit gebaut. Deshalb
ist auch kein Erstlauf moeglich. ZD05 (J1) laeuft im Cockpit selbst (`Shopfloor:SapSyncTimes`, Werktage). *Ueberholt:* „05:45 und
12:45, Umstellung auf 04:00 offen“. Entscheid Ingo 2026-10-16: **04:00 und 12:45** (der Mittagslauf bleibt). Offen von Fabio: Lieferzeit-Ziele (Daniel Tobler), Kuerzel
Ergebnisbereich CE1xxxx. *Ueberholt 2026-11-02:* Ergebnisbereich 1000 / `CE11000` (Abschnitt 16; im Repo schon seit Juni aus dem Produktsparten-Mapping bekannt).

**Fabios Pakete ohne Nacharbeit (2026-10-16, `e6c007f`):** `ShopfloorConfig` uebernimmt ein `planned_source` am Feld nach `src` und
entfernt es (auch in `/api/config`); ohne `planned_source` wird die Datei unveraendert ausgeliefert. Test
`Planned_Source_Aus_Fabios_Paketen_Gilt_Als_Quelle`. In der abgelegten Kopie `3_Config/config_patch.js` ist die DW-Liste ohne `DW`
und FPY setzt zusaetzlich `src`; Fabio sollte diese Fassung in seine Vorlage uebernehmen. Weiter offen: Jobs J2 bis J11, Lieferzeit-Ziele.

## 15. Mengen aus SAP: Logistik (J5), Tagesmengen (J8), FPY SEH (J11) (Stand 2026-10-16)

Auftrag: Fabios Mail vom 16.10. (von Ingo weitergeleitet). Klarstellung Fabio: das Cockpit holt per OData selbst, die Jobs
J1 bis J11 sind eine Liste von Datenabfragen, keine SAP-Jobs.

- **SAP:** neue Sets `ShopRmSet` (Logik von ZC27 `ZPP_RM_MENGEN`, in T76 gelesen) und `ShopProdSet` (Fertigungsauftraege
  je Istendtermin). In T76 aktiv und gegen ZC27 gleich (MLE01 197, MLE08/09 306/1'044, Variante `/PPA EXX72` 836).
  Transport `T76K912740` (Strukturen) und `T76K912738` (Klassen, dort noch vom ZC23-Ausbau gesperrt); Ingo transportiert.
  Einzelheiten `saptasks/shopfloor_sap/NOTES.md`.
- **Cockpit** (`2bc54bd`, produktiv 09:00): `ShopfloorSapMengen` im SAP-Abgleich (04:00, 12:45). Eintrag vom Tag D = Stand bis
  D-1, Montag Freitag bis Sonntag. Logistik MLE01/MLE02/MLM01/MLE04 je eigener ZC27-Lauf, MLE08+MLE09 zusammen; `tr5.prod` mit
  der EXX72-Gruppe (11 Arbeitsplaetze, Typencode 8*); `seh.prod` = WE-Menge S* am Vortag; `seh.fpy` = WE / Gesamt der KW des
  Vortags. Manuell erfasste Werte werden nicht ueberschrieben. Schalter `Shopfloor:SapMengenEnabled` **aus**, bis die
  Vorschau `GET shopfloor/api/sap/mengen?tag=2026-10-14` in P76 Fabios Kontrollwerte trifft (MLE01 673, MLE02 526, MLE04 542,
  MLM01 193, MLE08+09 1'065). Heute liefert sie 502 „Set noch nicht transportiert“.
- **Weitere Punkte aus der Mail:** Ampel, Ampel-Text, Erfuellung und Lieferzeit-Grund produktiv gespeichert und wieder entfernt
  (geprueft 16.10.). Versionsnummer in `index.html` ist nicht noetig: `/shopfloor/*` geht seit 2026-10-08 mit `no-cache` und
  ETag raus, der Browser holt neue Dateien ohne Strg+F5. Ein geleerter Tagessatz wird seit `180f9ef` geloescht statt leer
  zu bleiben. ZD05 bleibt auf 04:00 und 12:45 (Entscheid Ingo, obwohl Fabio 05:45 reichen wuerde).
- **Nicht gebaut:** J2/J3 Verkauf (Ergebnisbereich, Wertfelder, Merkmal Produktsparte fehlen; Fabio hat keine Berechtigung fuer
  KEA0, Ingo schaut nach), J6 Auftragsvorrat (ZC22), Historie nachladen (erst nach der Pruefung, nur leere Tage).
  *Teilweise ueberholt am 2026-11-02:* KEA0 ist gelesen (Abschnitt 16); J2/J3 bleiben ungebaut.

*Nachtrag 2026-10-16 09:51:* **produktiv.** Transporte in P76, Vorschau gleich Fabios Kontrollwerten (Logistik 13.10. alle fuenf Felder,
FPY SEH KW38 bis KW41), `Shopfloor:SapMengenEnabled = true` (`9d6b5d2`), Erstlauf fuer den 16.10. geschrieben. Falle P76: zwei
Anwendungsserver, der Gateway-Cache musste auf `travp762` geleert werden (Einzelheiten `saptasks/shopfloor_sap/NOTES.md` Abschnitt 7).
Die Aussage „Schalter aus“ oben ist damit ueberholt.

*Nachtrag 2026-10-19 10:04:* **Produktionstag statt Abfragetag** (Feedback Logistik ueber Fabio). Eintrag vom Tag D = ERSDA bzw.
Istendtermin D; die Aussage „Eintrag vom Tag D = Stand bis D-1, Montag Freitag bis Sonntag“ oben ist ueberholt. Der Abgleich
schreibt heute und die drei Tage davor; ein Bereich ohne Rueckmeldung bleibt leer statt 0. Neu `dw.prod` aus DL790. 15. bis
19.10. umgebucht. TX aus ZC22 noch nicht gebaut. Einzelheiten `saptasks/shopfloor_sap/NOTES.md` Abschnitt 8.

## 16. Paket Fabio vom 2026-11-01: Patch 04/05, Verkauf ueber Fiori (J4), KEA0 (Stand 2026-11-02)

Auftrag Ingo 02.11. („mach 1-3“), Mail Fabio mit `sf.zip`. Paket abgelegt unter `docs/shopfloor_paket_2026-11-01/` (Freigabe Ingo,
das Excel enthaelt Kontrollwerte in CHF). Commit `82f4f52`, produktiv 2026-11-02 08:21.

**1. Patches und Config, eingespielt mit Fabios Skripten, je zuerst `--check`:**

- `2_Patch/apply_patch.js` auf `wwwroot/shopfloor`: 01 bis 03 uebersprungen, **04** (PPD: TV SEH/TR5/TX/DW = Summe `menge_rs` der offenen
  Positionen von `rs_seh/tr5/tx/dw`, beim Oeffnen von PPD nur fuer heute gespeichert, Felder dann schreibgeschuetzt mit Marke „RS“) und
  **05** (Go-for-it-Kachel „erwartet (Fiori)“ statt „geplant (ZC23)“) angewendet. Patch 04 ist **aus**, bis beim Modul `ppd`
  `"tv_from_rs": true` steht (`3_Config/tv_aus_rs_schalten.js`). Fabio: erst einschalten, wenn die Dispo die Rueckstandslisten im
  Dashboard pflegt (zuletzt geaendert 05.10.).
- `3_Config/config_verkauf_fiori.js` auf `shopfloor_config.json`: `ae_tr5` und `u_tr5` entfernt (nie befuellt, Transmitter = TR5 + TX,
  Produktsparte 0005), `ae_rest`/`u_rest` ohne sie, `umsatz_plan` heisst „Erwarteter Umsatz Monat (Fiori Umsatz prüfen)“ (Schluessel
  bleibt, Verlauf bleibt). Das vom Skript gesetzte `planned_source` ist wieder entfernt (das Feld hatte schon `src: sap`); das SAP-Mapping
  von `umsatz_plan` und `gutschriften` zeigt jetzt auf J4. Damit 127 SAP- und 4 MES-Felder (Test angepasst).
- `.bak`-Dateien der Skripte nicht im Repo. Gegen die Feldnamen des Ports geprueft (LEARNINGS 2026-10-16): Patch 04 nutzt `loadRecords`,
  `todayIso`, `S.daily`, `menge_rs`, `status_field`, `tv_*`; alles vorhanden, jede Ersetzungsstelle genau einmal.

**2. J4 Verkauf aus Fiori «Umsatz prüfen»** (`Services/Shopfloor/ShopfloorSapVerkauf.cs`, im SAP-Abgleich 04:00/12:45):

- `C_SALESANALYTICSQRY_1_CDS`, Set `C_SALESANALYTICSQRY_1(P_DisplayCurrency='CHF',P_ExchangeRateType='M')/Results`, nur die vier
  Kennzahlen im `$select` (die Query verdichtet auf eine Zeile), Filter `SalesOrganization eq '1100' and BillingDocDateYearMonth eq 'JJJJMM'`.
  Gateway-Wurzel und Mandant wie Operations KPI (`KpiGatewayRoot`, `KpiSapClient`), Benutzer der Einkauf-Verbindung.
- `umsatz_plan` = `SlsVolumeNetAmtInDspCrcy` + `OpnOutbDelivsForInvcNetAmtInDC` + `OpenSalesOrdersNetAmountInDC`,
  `gutschriften` = `CustCrdtMemoNetAmtInDspCrcy`. Monat = Monat des Vortags (am 1. der Vormonat; *(Ueberholt 2026-11-13 mittags, Abschnitt 22: Monat des letzten Arbeitstags)*), geschrieben auf den Abfragetag
  (Konvention Verkauf: Eintrag D = Stand bis D-1). Manuell erfasste Werte bleiben (`ShopfloorSapMengen.BuildItems`).
- Vorschau `GET shopfloor/api/sap/verkauf?tag=JJJJ-MM-TT`, schreiben `POST` (mit `X-User`). Schalter `Shopfloor:SapVerkaufEnabled`
  stand mit `82f4f52` auf false, bis die Vorschau fuer den 01.11. Fabios Kontrollwerte traf: 10/2026 **8'827'624** bzw. **110'412** CHF.
- *Erledigt 2026-11-02 08:36:* `82f4f52` produktiv seit 08:21 (bitgleich, ohne Alarm). Vorschau 01.11. (Monat 10/2026):
  **8'827'624.28** erwarteter Umsatz (Umsatz 8'695'910.24 + offene Lieferungen 131'008.06 + offene Auftraege 705.98) und
  **110'412.21** Gutschriften, **gleich Fabios Kontrollwerten**. Heute (Monat 11/2026) per `POST` geschrieben: 6'489'963.37 und
  3'632.05, Quelle SAP, zurueckgelesen. Danach `SapVerkaufEnabled = true` im Repo; der automatische Abgleich 04:00/12:45 schreibt
  erst nach dem naechsten Deploy (Stand siehe `docs/rag/DEPLOYMENT.md`). *Erledigt 2026-11-02 10:12:* nach dem Deploy `a445f30` (10:09)
  schrieb der Abgleich den Verkauf selbst (`umsatz_plan` 6'508'620.84 CHF, Quelle SAP-Schnittstelle).
- Tests `ShopfloorSapVerkaufTests` (Monat des Vortags, URL, Summe, ohne Zeile nichts, manueller Wert bleibt).

**3. KEA0 (fuer J2/J3 AE und Umsatz IST aus CO-PA), gelesen in T76 per SapProbe am 2026-11-02:**

| Punkt | Wert |
| --- | --- |
| Ergebnisbereich | **1000** (`TKA01`: Kostenrechnungskreis 1000 „Trafag Group“) |
| Tabelle Einzelposten | **`CE11000`** (kalkulatorische Ergebnisrechnung) |
| Merkmal Produktsparte | **`WWPSP`** (`RKEG_WWPSP`); nicht `SPART` (SD-Sparte, in den Stichproben `01`) |
| Satzart | `VRGAR` (A = Auftragseingang, F = Faktura); Periode `GJAHR`/`PERDE` |
| Wertfelder Nettoerloes | `VV100` Bruttoerloes, `VV110` Frachterloes, `VV200` Wiederverkaufsrabatt, `VV210` Mengenrabatt, `VV220` Sonstige Erloesminderung, `VV250` Waehrungsausgleich, `VV230` Verkaufsprovisionen, Skonto: `VV240` „Skonto kalk.“ oder `VV245` „Skonto echt FI“ |
| Weitere Merkmale | `WWPFA` Produktfamilie, `WWTYP` Typencode, `PAPH1` Produkthierarchie, `VKORG`, `BUKRS`, `KNDNR`, `ARTNR`, `PALEDGER` Waehrungstyp |

*Nachtrag 2026-11-02 (Abgleich mit Repo und Fabios Paketen):* `CE11000` und `WWPSP` waren nicht neu, sie stehen seit Juni 2026 im
Produktsparten-Mapping (`docs/PRODUCT_SPARTEN_MAPPING_2026-05-27.md`, `docs/abap/README_PRODSPARTE.md`); die Werte 0003 = DW und
0005 = Transmitter hat Fabio am 16.10. geliefert. Waehrung: Fabio legt im Excel „Buchungskreiswaehrung CHF“ fest, und `CE11000` hat fuer
Buchungskreis 1100, Satzart F nur `PALEDGER = 01` (T76, Abfrage `PALEDGER <> '01'` ohne Treffer); keine Frage noetig.
Offen fuer J2/J3 nur noch, welches Skonto-Feld der KE30-Bericht Z0001-000006 abzieht (`VV240` oder `VV245`): **selbst entscheiden**, indem
beide Varianten in P76 gegen Fabios Kontrollwerte Sep 2026 gerechnet werden (IST 7'878'114.69, AE 6'531'197.94), keine Frage an Fabio.
Ebenso aus seinem Paket vom 16.10. (LIESMICH): nach dem ersten Lauf die Historie Verkauf ab 01.01.2026 nachladen, nur leere Tage. T76 hat nur Daten bis etwa April 2026, die
Kontrollwerte Sep/Okt lassen sich erst in P76 nachrechnen. Lesen von `CE11000` braucht ein eigenes Set oder einen Report (zu gross fuer
`RFC_READ_TABLE` ohne Verdichtung).

**Fragen an Fabio aus Punkt 5 der Mail (PPD-Verzug aus ZC22, nicht gebaut):** nur eine: `verzug_dw` mit Disponent `DW*` weicht von der
Abteilung DW = `D*` ab (DW1 Ruestpapiere waere dabei, DB1/D10 nicht); in Repo, Paketen und Sitzungsprotokollen gibt es dazu keine fruehere
Antwort (geprueft 2026-11-02). Selbst zu tun: Bedeutung der ZC22-Schalter „mit Vorgangsdaten“ und „mit Planauftraegen“ im Quelltext lesen.
Von Fabio bereits angekuendigt, nicht fragen: Selektion `verzug_seh` folgt, Untergrenze 01.01.2026 bis zur Klaerung. Mail-Entwurf an Fabio
in Ingos Downloads (`Mail_Fabio_Shopfloor_Paket_2026-11-01_Fragen.eml`). Dazu: Fabios Excel nennt
fuer J5/J8 noch „Vortag, Montag Fr+Sa+So“, bei uns gilt seit 2026-10-19 der Produktionstag (Abschnitt 15).
*Erledigt 2026-11-02 nachmittags:* Ingo hat die Mail geschickt, Fabio hat geantwortet (Definition PPD, A. Tran): DW = `D*` ohne
DW1/DL6, also wie die Abteilung; dazu Untergrenze und Vorzeichen. Der PPD-Verzug ist gebaut, siehe Abschnitt 17. Der Mail-Entwurf ist
damit erledigt.

## 17. PPD-Verzug aus ZC22 (Mail Fabio 2026-11-02, Stand 2026-11-02)

Auftrag Ingo („shopfloormail vorher“, vor der KPI-Kundensicht). Fabios Definition (Mail 02.11., Feldliste `ppd.verzug_*` im Excel
`1.xlsx`, Blatt Feld-Mapping): ZC22 (`ZM_OFFENE_FAUF`), Werk 1100, mit Planauftraegen und Vorgangsdaten, ohne erledigte, Summe offene
Menge, Abfragetag; Disponenten SEH `S*`, TR5 `EK*`, TX `EL*`, DW `D*` ohne DW1/DL6 (inkl. DS1/DS2/DSN); Material 1 bis 99999. *(Ueberholt 2026-11-13 mittags, Abschnitt 22: DW `D*` ohne Ausnahmen, Material 1-99999 nur TR5 und DW, Eckendtermin ab 01.01.2026.)*
**Mail und Excel weichen an zwei Stellen ab, die Mail gilt (neuer, ausdruecklich):** Eckendtermin *ohne* Untergrenze bis Vortag (das Excel
nennt noch 01.01.2026, wie Anh Nhi; damit fiele ab Januar 2027 der Verzug aus 2026 weg) und **positive** Werte wie bisher im Dashboard
(das Excel sagt „als negative Zahl gefuehrt“).

**Was ZC22 tatsaechlich rechnet (Quelltext `docs/abap/ZM_OFFENE_FAUF.abap` gelesen):** Die Spalte heisst „Positionsmenge“ und ist
`AFPO-PSMNG` (Gesamtmenge, nicht Gesamt minus geliefert) bzw. bei Planauftraegen `PLAF-GSMNG`. „Ohne erledigte“ = `ELIKZ` und `DNREL`
leer (erste Position je Auftrag). „Mit Planauftraegen“ = `PLAF` mit `UMSKZ = X` und `BESKZ <> F`. „Mit Vorgangsdaten“ fuellt nur
Vorgang und Arbeitsplatz und aendert die Summe nicht. Das bisherige `ShopAufSet` (Forecast) rechnet anders (PSMNG minus WEMNG, ohne
UMSKZ, nur 60 Tage) und taugt deshalb nicht als Quelle.

**SAP (T76, eigener Auftrag `T76K912797`, Aufgabe `T76K912798`, nur `ZCL_ZPOWERBI_EINKAUF_DPC_EXT` GET_ENTITYSET):** neuer Modus im
bestehenden `ShopAufSet` mit Filter `Typ eq 'VZ'`: offene Fertigungsauftraege und Planauftraege wie ZC22, Eckendtermin vor dem
Stichtag ohne Untergrenze, Zeilen mit Typ `VF`/`VP`, Menge = Positionsmenge. Kein neues Property, also keine Aenderung an der
MPC-Klasse (die liegt im offenen Auftrag `T76K912793` der Sitzung Zeichnungsaenderung) und kein Cache-Leeren. Quelltext
`docs/abap/ZSHOP_VERZUG_ADD.abap`, vorher/neu unter `saptasks/shopfloor_sap/DPC_EXT_GET_ENTITYSET_2026-11-02_verzug_*.abap`;
aktiviert ohne Fehlerdialog, rueckgelesen identisch. Schutz: Stichtag hoechstens 7 Tage zurueck, mehr als 100'000 Zeilen = Fehler.

**Abgleich in T76 am 2026-11-02** (lokaler Testreport, `SUBMIT ZM_OFFENE_FAUF` mit Fabios Selektion gegen den neuen Modus, je
Auftrag verglichen):

| Abteilung | ZC22 Auftraege / Menge | ShopAufSet VZ |
| --- | --- | --- |
| DW | 1'570 / 49'526 | gleich |
| TR5 | 786 / 41'127 | gleich |
| TX | 1'349 / 73'895 | gleich |
| SEH | 0 / 0 | gleich |

Unterschiede je Auftrag: 0. Laufzeit ZC22 rund 6 Minuten, der neue Modus 0,1 s. Gateway-Client T76: Modus VZ 200, bisheriger
Forecast-Aufruf weiterhin 200, Stichtag 01.01.2026 400 (Schutz).

**Offen, Rueckfrage an Fabio:** SEH ergibt mit „Material 1-99999“ immer 0. Alle offenen SEH-Auftraege in T76 haben alphanumerische
Materialnummern (B62622, C11095, B55118 ...), der Bereich 1 bis 99999 trifft nur numerische Fertigprodukte. Soll fuer SEH der
Materialfilter wegfallen? Bis zur Antwort rechnet das Cockpit wie das Excel. *(Beantwortet 2026-11-13: Fabio nennt nach Klaerung mit Sergio/Anh Nhi keinen Materialfilter mehr, Verzug laeuft ohne, Abschnitt 21.)*

**Cockpit (`27badb2`):** `Services/Shopfloor/ShopfloorSapVerzug.cs` (Abteilung je Disponent, Material 1-99999, Summe positiv,
Fehler statt Zahl, wenn das System den Modus nicht kennt), Aufruf im SAP-Abgleich 04:00/12:45 ueber `ShopfloorSapSync`, geschrieben
auf den Abfragetag ueber `ApplyPush` (manuell erfasste Werte bleiben), Endpunkte `GET/POST shopfloor/api/sap/verzug?tag=`, Schalter
`Shopfloor:SapVerzugEnabled` (Vorgabe aus), Config `ppd.verzug_*` mit Quelle und Selektion, Tests `ShopfloorSapVerzugTests`.
Suite 1345/1345 im Release-Worktree. *Nachtrag:* Code mit dem Deploy 2026-11-02 13:57 (`187f8a0`) produktiv, Schalter weiter aus.

**Reihenfolge bis produktiv:** (1) Ingo gibt `T76K912797` frei und importiert nach P76 (*erledigt 02.11.*); (2) Deploy (*erledigt 13:57*); (3) Vorschau
`GET shopfloor/api/sap/verzug` in P76 gegen ZC22 (Fabio/Anh Nhi); (4) `SapVerzugEnabled = true`. Vor dem Import liefert die Vorschau
die Meldung „Transport T76K912797 fehlt“, keine falsche Zahl.

*Nachtrag 2026-11-02 abends:* Ingo hat `T76K912797` und `T76K912793` (Zeivr, Sitzung 6d) nach P76 importiert. Vorschau
`GET shopfloor/api/sap/verzug?tag=2026-11-02` aus P76: 188 Zeilen, TR5 507 (13 Auftraege), TX 64 (4), DW 60 (12), SEH 0. Gateway-Caches
in P76 auf Auftrag Ingo geleert (eigene Sitzung, `trasp76s`, SM51 zeigt weiter nur diese Instanz). Gateway Client P76: `$metadata` 200
(382'030), `ShopAufSet` Typ VZ 200, `MARA001Set?$select=Matnr,Zeivr` 200, alte Abfrage `Matnr,Mstae,Matkl` 200. **Offen:** Abgleich der
Vorschau gegen ZC22 in P76 (Fabio/Anh Nhi oder auf Auftrag), danach `SapVerzugEnabled = true`.

## 18. Paket Fabio vom 2026-11-07 (SAMSTAG.zip): Patch 06/07, zwei Config-Wuensche (Stand 2026-11-09)

*Erledigt 2026-11-09 07:23:* Paket unter `docs/shopfloor_paket_2026-11-07/`. Mit Fabios `apply_patch.js` (zuerst `--check`) in `wwwroot/shopfloor/` eingespielt, 01 bis 05 korrekt uebersprungen:

- **Patch 06:** Terminverschiebungen aus den Rueckstandslisten zaehlen als offen, wenn „Datum Abschluss“ leer ist (wie der Excel-Filter „Offen“), der Status wird nicht mehr ausgewertet. Wirkt erst mit eingeschaltetem Patch 04 (`tv_from_rs`).
- **Patch 07:** Werte mit SAP/MES-Kennzeichen (`meta`) sind im Tagesraster schreibgeschuetzt (Schloss, Tooltip). Abschaltbar ohne Code mit `"lock_ext": false` in `shopfloor_config.json`.

Commit `fd345e7`, Release-Worktree 1460/1460, deployed 07:23 bitgleich ohne Alarm; auf dem Server `app.js`/`style.css` mit beiden Markern geprueft.

**Bewusst noch nicht eingespielt:** `config_logistik_dw_soll.js` (Logistik „DW SOLL Verpacken“ = `dw.lieferbereit` DLQ72 statt DL790) und `config_seh_tagesmenge_sap.js` (SEH „Offene Tagesmenge“ `planned_source` = sap). Beide Quellfelder stehen in Fabios Liste `Shopfloor_Was_fehlt_2026-11-07.xlsx` auf „definiert, fehlt, Ingo einbauen“; unser SAP-Abruf fuellt `dw.lieferbereit` nicht (kein Treffer im Code). Mit der DW-Config waere der Logistik-Sollwert heute leer. Reihenfolge: erst SAP-Abruf bauen (DLQ72 nach ZC27-Logik, Produktionstag; SEH Tagesziel aus ZC22 Bucket „Tag“, Kontrolle Fabio Mo 09.11.: 2814), dann die beiden Configs.

**Offen aus Patch 07 (Backend, Fabios Hinweis):** POST `api/daily` soll Aenderungen an Feldern mit `meta.src` SAP/MES abweisen oder ignorieren, und der naechste SAP-Lauf soll manuell ueberschriebene SAP-Felder wieder mit SAP-Wert und `meta` setzen (Beispiel SEH prod 02.11., SAP-Kennzeichen fehlt seither). Die Oberflaeche allein schuetzt nicht vor direkten API-Aufrufen.

## 19. Mail Fabio 09.11.: Verzug in P76 abgeglichen, Auftragsvorrat (Typ VR) gebaut (Stand 2026-11-09)

Fabio (Mail 09.11.) fragt nach Terminen fuer a) ZC22-Vorrat, offene KW, Offene Tagesmenge SEH (danach TR5, DW, TX) und b) PPD-Verzug.
Auftrag Ingo („1. ja 2. ja“): a) bauen, b) ZC22 in P76 lesend laufen lassen.

**b) PPD-Verzug, P76 09.11.:** ZC22 in einer eigenen P76-Sitzung (`.tmp_sap_probe/P76Zc22Lesen.vbs`, nur lesen, danach geschlossen;
die Listenzeilen gibt SAP GUI Scripting in P76 nicht heraus, deshalb Liste per `%PC` unkonvertiert in den Scratchpad gesichert).
Disponent D bis SZZ, Eckendtermin bis 08.11., Material 1-99999, mit Planauftraegen:

| Abteilung | ZC22 P76 ohne Layoutfilter | Vorschau `GET sap/verzug?tag=2026-11-09` |
| --- | --- | --- |
| DW | 9 / 185 | 9 / 185 |
| TR5 | 11 / 465 | 11 / 465 |
| TX | 11 / 310 | 11 / 310 |
| SEH | 0 | 0 |

Exakt gleich. **Zwei Befunde fuer Fabio, vor dem Einschalten (`SapVerzugEnabled` bleibt aus):**
1. Das ZC22-Standardlayout in P76 (globales Layout `/ P00 TX`, siehe `docs/abap/README_ZM_OFFENE_FAUF_BEDARFSVERURSACHER.md`) filtert auf
   **Arbeitsplatz und Systemstatus**; mit Filter zeigt ZC22 TR5 7 / 226 und TX 1 / 50. Die Definition der Produktionsplanung nennt kein
   Layout; das Cockpit rechnet ohne.
2. **Material 1-99999** (Definition PPD, A. Tran, fuer alle vier Abteilungen) laesst SEH immer bei 0: alle SEH-Materialien sind
   alphanumerisch (B…, C…), auch in P76 (*berichtigt: die Aussage im Chat vom 09.11., SEH habe in P76 numerische Materialien, war falsch*).
   Die Rueckfrage aus Abschnitt 17 ist damit weiter offen. *(Beantwortet 2026-11-13, Abschnitt 21: ohne Material- und ohne Layoutfilter.)*

**a) Auftragsvorrat (Typ VR):** Der Verzug-Modus erlaubt hoechstens einen Tag Vorschau; deshalb im selben Block von `ShopAufSet` ein Typ
`VR`: dieselbe Abfrage (offene Fertigungsauftraege und Planauftraege wie ZC22), aber Eckendtermin-Grenze `99991231`. T76, **neuer Auftrag
`T76K912860`** (Aufgabe `T76K912861`, nur `METH ZCL_ZPOWERBI_EINKAUF_DPC_EXT GET_ENTITYSET`), erst nach Freigabe von `T76K912837`
(QmMeldungSet sperrte die Methode, Entscheid Ingo). Quelltext vorher/neu `saptasks/shopfloor_sap/DPC_EXT_GET_ENTITYSET_2026-11-09_vorrat_*.abap`
(3 Stellen, 8 Zeilen), aktiviert, rueckgelesen.
- **Abgleich T76** (Testreport `saptasks/shopfloor_sap/Z_KOI_SHOP_VR_TEST.abap`, voruebergehend im `$TMP`-Report `Z_KOI_ZLO_FIX26_TEST`;
  ZC22 per `SUBMIT` mit `cl_salv_bs_runtime_info`): SEH (S00 bis SZZ) **1'877 Auftraege / 179'981 Stk., je Auftrag gleich ZC22**, 0
  Abweichungen; VR liefert fuer Werk 1100 insgesamt 10'938 Zeilen.
- **Kein Materialfilter:** Fabios Schnittstelle (`1.xlsx`, `Shopfloor_SAP_Schnittstelle_2026-11-02.xlsx`) definiert den Vorrat ohne;
  seine Kontrolle SEH 159'661 (07.11.) passt nur so (P76 09.11. ohne Filter 157'502, mit Filter 0). Die Zeile „Material 1-99999“ in
  `Shopfloor_Was_fehlt_2026-11-07.xlsx` gilt als uebernommen aus dem Verzug.
- **Cockpit (`01fceec`, produktiv 14:12, Schalter `Shopfloor:SapVorratEnabled` aus):** `Services/Shopfloor/ShopfloorSapVorrat.cs`.
  Buckets ab dem Abfragetag D, kumuliert und inkl. Ueberfaelliger: `vorrat_total`; `vorrat_1m` bis Monatsende; `vorrat_2m` bis Ende
  Folgemonat; `vorrat_3m` bis Ende uebernaechster Monat; `offen_kw` bis Sonntag der KW; nur SEH `ziel` bis D. Module seh, tr5, tx, dw. *(Ueberholt 2026-11-13 mittags, Abschnitt 22: rollend D + 1/2/3 Monate bzw. DW D + 4/8/12 Wochen; TR5 und DW Material 1-99999.)*
  Schreiben ueber `ApplyPush` (manuelle Werte bleiben), Aufruf im SAP-Abgleich 04:00/12:45, Vorschau `GET/POST shopfloor/api/sap/vorrat?tag=`,
  Tests `ShopfloorSapVorratTests` (1465/1465). Vor dem Import liefert die Vorschau 502 „Transport T76K912860 fehlt“.
- **Nicht gebaut:** TX `plan5` („Eckendtermin naechste 5 AT“, Grenze nicht eindeutig), Layoutfilter (siehe b).

**Reihenfolge bis produktiv:** (1) Ingo gibt `T76K912860` frei und importiert; (2) Gateway-Caches P76 nur falls noetig (keine neue
Property); (3) Vorschau `GET sap/vorrat` gegen ZC22 P76 (SEH heute 157'502); (4) `SapVorratEnabled = true`, danach Fabios
`config_seh_tagesmenge_sap.js`.

*Nachtrag 2026-11-09 abends:* Ingo hat `T76K912860` nach P76 importiert. Vorschau `GET sap/vorrat?tag=2026-11-09` gegen ZC22 P76 (eigene
Sitzung, ohne Layoutfilter, `%PC`): **SEH 1'914 Auftraege, total 157'556, 3 Mte 70'175, 2 Mte 47'960, 1 Mte 31'191, KW 8'840, Tag 144;
DW 1'976, 117'271 / 85'101 / 58'570 / 38'008 / 15'189 — je Bucket gleich.** Erster Abruf hatte SEH einen Auftrag (60 Stk.) mehr, der
zwischen den Abfragen erledigt wurde. TR5 62'958, TX 50'325 (gleiche Logik). SEH-Tageswert 144 statt Fabios 2'814 (Stand Samstag fuer
Montag; die Montagsauftraege sind inzwischen gemeldet). Einschalten (`SapVorratEnabled`, SEH-Config) wartet auf Ingos OK.

*Nachtrag 2026-11-09 15:15, eingeschaltet (Auftrag Ingo „aktiviere was geht und schalte frei“):* `Shopfloor:SapVorratEnabled = true` (`88e19f6`), Fabios `config_seh_tagesmenge_sap.js` eingespielt (`--check`, dann ohne; `seh.ziel` `planned_source: sap`, dazu Quellenbeschreibung `sap` am Feld, `4585392`; Test SAP-Felder 127 -> 128), deployed 15:11 (`4585392`, 1465/1465, bitgleich). Heute geschrieben per `POST sap/vorrat?tag=2026-11-09` (Header `X-User: claude`): SEH 157'571 / 3M 70'130 / 2M 47'615 / 1M 31'072 / KW 8'840 / Tag 144; TR5 63'921; TX 50'342; DW 117'271; keine manuellen Werte uebersprungen. Ab morgen schreibt der Abgleich 04:00 und 12:45. Verzug (`SapVerzugEnabled`) bleibt aus bis Fabios Antwort. *(Ueberholt 2026-11-13, Abschnitt 21: „schreibt 04:00 und 12:45“ ueberschrieb die Stichtagswerte tagsueber; seither gilt der erste Lauf des Tages. Verzug seit 13.11. ein.)* `T76K912858` (ZC22 Systemstatus) ebenfalls in P76 (ohne Schalter).

## 20. Tag 10.11.: DLQ72, Backend-Schutz, plan5, CE11000-Skonto (Auftrag Ingo „2-6 umsetzen durch dich“, Stand 2026-11-10)

*Erledigt 2026-11-10 08:16 (`e6f6f70`, 1475/1475, deployed, bitgleich):*
- **c) DW lieferbereit DLQ72:** `ShopfloorSapMengen.RmGroups` um `("dw", "lieferbereit", ["DLQ72"])`, ZC27-Logik wie DL790. P76 Gateway
  `ShopRmSet` DLQ72 200. Fabios `config_logistik_dw_soll.js` eingespielt (`logistik.dw_soll = ref('dw','lieferbereit')`, Label „DW SOLL
  Verpacken (lieferbereit DLQ72)“). Historie 01.10. bis 10.11. per `POST sap/mengen?tag=` nachgeladen (34 Tage, alle 33 DW-Tage mit Wert;
  Beispiele 09.11. prod 823 / lieferbereit 1'014, 05.11. 745 / 659).
- **Patch 07 im Backend:** `POST api/daily` (`ShopfloorStore.PostDaily`) laesst Felder mit `meta.src` SAP/MES unveraendert und meldet sie
  unter `gesperrt` (abschaltbar mit `"lock_ext": false`, `ShopfloorConfig.LockExternal`). Der SAP-Abgleich (`BuildItems`, alle vier Wege)
  stellt ein von Hand ueberschriebenes Feld mit `src = sap` in der Konfiguration wieder her (SAP-Wert und Kennzeichen); echte Handfelder
  bleiben. *Ueberholt fuer SAP-Felder: „manuell erfasste Werte werden nicht ueberschrieben“ (Abschnitte 15, 17, 19).*
- **TX plan5:** im Vorrat (`ShopfloorSapVorrat.Plan5Module`), **Annahme**: Eckendtermin vom Tag nach D bis zum 5. Arbeitstag (Mo-Fr, ohne
  Feiertage), ohne Ueberfaellige; heute 5'937. Von Fabio zu bestaetigen. *(Ueberholt 2026-11-13 mittags, Abschnitt 22: Fabio: kumuliert mit Ueberfaelligen und heutigen Auftraegen.)*
- **d) CE11000, Skonto-Frage geloest (P76 lesend, SE16 mit Spaltenauswahl, 71'544 Zeilen Periode 009.2026):** Buchungskreis 1100,
  `PALEDGER 01`: `VV100 + VV110 - VV200 - VV210 - VV220 - VV250 - VV230 - VV240` ergibt **F 7'878'114.69 und A 6'531'197.94, genau Fabios
  Kontrollwerte**; ohne `VV240` 303.20 bzw. 196.47 zu hoch, `VV245` ist 0. Buchungskreis 1200 hat eigene Zeilen (F 334'938, A 368'478),
  nicht in Fabios Wert. Bau offen: ein Set mit Monatssummen je Satzart und `WWPSP` (0003 DW, 0005 Transmitter) in T76, da `CE11000` fuer
  `RFC_READ_TABLE` zu gross ist.
- **b) Verzug:** unveraendert aus (Fabios Antwort zu Material 1-99999 und Layoutfilter offen). *(Ueberholt 2026-11-13: eingeschaltet ohne Filter, Abschnitt 21.)*

*Nachtrag 2026-11-10 08:46 (`436e8ad`, deployed, 1483/1483):*
- **b) Verzug-Schalter (Wunsch Ingo bis Fabios Entscheid):** `Shopfloor:VerzugMaterialFilter` (Text, kommagetrennt, Vorgabe
  `SEH,TR5,TX,DW` wie PPD; leer = ohne Materialfilter; Text, weil der Konfigurations-Binder eine leere Liste nicht als Ueberschreibung
  liest) und `Shopfloor:SapVerzugEnabled` (aus) stehen in `appsettings.json`; die Vorschau zeigt den aktiven Filter. Ein- und Umschalten
  = Wert aendern und deployen. *(Stand seit 2026-11-13: Filter leer, Schalter ein, Abschnitt 21.)*
- **d) CO-PA gebaut:** SAP `CoPaSumSet` in T76 (`T76K912866`, `saptasks/copa_verkauf/NOTES.md`), Cockpit `ShopfloorSapCoPa`
  (ae_aktuell/ae_tx 0005/ae_dw 0003 aus Satzart A, umsatz_ist/u_tx/u_dw aus F; Monat des Vortags wie Verkauf J4), Schalter
  `Shopfloor:SapCoPaEnabled` aus, Vorschau `GET/POST sap/copa?tag=`. P76 liefert bis zum Import 502 „404, Set nicht transportiert“.
  **Ingo:** `T76K912866` freigeben und importieren, danach P76-Caches, Vorschau fuer `tag=2026-10-01` gegen Fabios September, Schalter ein.
- T76 aufgeraeumt: `Z_KOI_ZLO_FIX26_TEST`, `Z_KOI_VC_TXT_KOPIE`, `Z_KOI_VC_TXT_TESTID`, `Z_KOI_ZLO03_ALT`, `Z_KOI_ZLO_APP_TEST` geloescht.
- Q-Faelle im Browser bestaetigt: MOFN offen 191, MRST zurueckgestellt 131, MIAR in Arbeit 23; Produktlinie bei 312 von 345.

## 21. Mail Fabio 13.11.: Stichtagswerte, Morgenwerte nachgeladen, Verzug ein, Cache-Busting (Stand 2026-11-13)

Fabio (Mail 13.11.): Vorrat, offene KW, Tagesmenge, Planmenge TX und DLQ72 sind drin und plausibel. Zwei Punkte: (1) Vorrat
total/3/2/1 Mte, offene KW, Offene Tagesmenge SEH und Planmenge 5 AT TX nur aus dem Lauf 04:00, danach nicht mehr ueberschreiben;
die Laeufe 12:45 usw. ueberschrieben sie, «Offene Tagesmenge» zeigte am Nachmittag den Rest statt des Tagesziels, die Tagesleistung
SEH stand bei 214 bis 2'335 %. Produzierte Mengen, Logistik und Verkauf duerfen tagsueber weiterlaufen. Morgenwerte 09. bis 12.11.
nachladen, falls moeglich. (2) PPD-Verzug geklaert (Sergio/Anh Nhi): ZC22, Eckendtermin bis Vortag ohne Untergrenze, Disponenten
S*/EK*/EL*/D* (D* wie beim Vorrat DW), mit Planauftraegen, ohne erledigte, Summe offene Menge, positiv. Plan im Chat, Entscheide Ingo
(13.11. 07:50): faellt 04:00 aus, gilt der erste erfolgreiche Lauf des Tages; Verzug ohne Materialfilter und ebenfalls Stichtagswert;
eigener Zweig ohne den Spanien-Commit `c9208b5`; umsetzen bis Deploy.

**Befund vorher (Verlauf `history` produktiv gelesen):** nicht nur 12:45 schrieb neu. Nach jedem Neustart holt `ShopfloorSapSync` den
letzten Slot sofort nach (`_lastRunSlot` startet leer), deshalb gab es zusaetzliche Laeufe nach Deploys oder Neustarts: 10.11. 08:17,
08:46, 11:28; 11.11. 16:28; 12.11. 11:34; 13.11. 07:57. Beispiel SEH-Tagesziel 11.11.: 04:01 2'655, 12:46 1'718, 16:28 408.
**Am 13.11. lief 04:00 nicht**, der erste Lauf war 06:16 (die App lief um 04:00 offenbar nicht; Ursache nicht untersucht, offen).

**Umsetzung (`0e94279`, Zweig `shopfloor-stichtag-1311` ab `e956183`, in den Arbeitszweig gemergt `e7b70f6`):**
- `ShopfloorSapMengen.Freeze` und `ShopfloorStichtag.Write`: Vorrat (`vorrat_total`, `vorrat_3m`, `vorrat_2m`, `vorrat_1m`,
  `offen_kw`, SEH `ziel`, TX `plan5`) und Verzug (`ppd.verzug_*`) werden nur geschrieben, solange das Feld fuer den Tag noch keinen
  SAP-Wert hat. Spaetere Laeufe melden die Felder unter `eingefroren`. Massgebend ist der gespeicherte Stand, nicht die Uhrzeit;
  damit greift die Regel auch nach Neustarts und beim Knopf. `POST sap/vorrat?ersetzen=1` bzw. `sap/verzug?ersetzen=1` schreibt
  bewusst neu. Mengen (prod, FPY, Logistik), Verkauf und CO-PA laufen unveraendert tagsueber nach.
- `GET/POST shopfloor/api/sap/morgenwerte?von=&bis=` (`ShopfloorStichtag.RestoreMorning`, `ShopfloorStore.FirstSapValues`): je Tag,
  Modul und Feld der erste SAP-Wert aus `history`; schreibt nur Felder zurueck, deren aktueller Wert aus SAP stammt. Neu rechnen
  geht nicht, weil ShopAufSet nur die heute offenen Auftraege kennt.
- `Shopfloor:SapVerzugEnabled = true`, `VerzugMaterialFilter = ""` (auch Vorgabe in `ShopfloorOptions`). Disponenten unveraendert
  (`ShopfloorSapVerzug.Abteilung`, die auch der Vorrat nutzt), kein Layoutfilter. Der Test
  `Verzug_Rechnet_Ohne_Materialfilter_Und_Ist_Eingeschaltet` liest beide Werte aus `appsettings.json`; wer sie umstellt, passt den Test an. *(Ueberholt 2026-11-13 mittags, Abschnitt 22: `MaterialFilter = "TR5,DW"` fuer Vorrat und Verzug, `VerzugAb = 2026-01-01`; Test heisst jetzt `Vorrat_Und_Verzug_Mit_Materialfilter_TR5_DW_Und_Untergrenze_2026`.)*
- Cache-Busting: `wwwroot/shopfloor/index.html` laedt `style.css?v=…` und `app.js?v=…`; `v` = erste 8 Zeichen SHA256 von `app.js` +
  `style.css` ohne CR. Der Test `IndexHtml_Laedt_AppJs_Und_StyleCss_Mit_Aktueller_Version` faellt, sobald eine der Dateien sich
  aendert, bis `index.html` den neuen Wert traegt (er steht in der Fehlermeldung). Fabios `apply_patch.js` fasst `index.html` nicht an.
- Tests `ShopfloorStichtagTests` (8), Shopfloor 137/137, Release-Worktree 1555/1555. Deploy 07:56 (`0e94279`, bitgleich, Alarm nur
  `-wal`/`-shm` der Haupt-DB, wieder da). Protokoll `.tmp_deploy_2026-11-13_shopfloor_stichtag.log`.

**Nach dem Deploy produktiv geprueft:**
- Neustart-Lauf 07:57: alle 22 Vorratsfelder `eingefroren` (Stand 06:16 bleibt), Verzug erstmals geschrieben: SEH 89 (4 Auftraege),
  TR5 559 (16), TX 567 (16), **DW 9'575 (84)**. DW lag am 09.11. mit Materialfilter bei 185; der Sprung kommt vermutlich vom
  weggefallenen Filter (Annahme, nicht geprueft). Gegen ZC22 P76 ohne Filter noch nicht abgeglichen, Kontrolle durch Fabio. Gespeichert
  mit SAP-Kennzeichen (07:57) und damit fuer heute eingefroren; stimmt er nicht, nach der Korrektur `POST sap/verzug?ersetzen=1`.
- Morgenwerte per `POST sap/morgenwerte?von=2026-11-09&bis=2026-11-13` (Header `X-User: claude`): 66 Felder zurueckgesetzt, danach 0
  offen. 09.11. bleibt mit dem Wert von 15:12 (erster Wert ueberhaupt, Einschalttag; kein Morgenwert vorhanden). 10.11. aus 04:02,
  ausser TX `plan5` (gab es erst ab 08:17, Wert 5'937). 11.11. aus 04:01, 12.11. aus 04:02. 13.11. war schon der Wert von 06:16.
  Die zurueckgesetzten Zellen tragen im Tooltip den Zeitpunkt des Zuruecksetzens (13.11. 07:58), nicht 04:0x; die Herkunft steht im
  Verlauf (`history`).
- **09.11. bleibt falsch:** ziel 144 (Rest um 15:12) / prod 3'363 = 2'335 %, Fabios Hoechstwert. Entscheid Ingo offen: so lassen,
  `ziel` leeren oder Fabios Kontrollwert 2'814 (Abschnitt 19, Stand Samstag fuer Montag) eintragen.
- Tagesleistung SEH danach: 10.11. ziel 2'217 / prod 2'258 = 102 %; 11.11. 2'655 / 2'547 = 96 %; **12.11. 1'687 / 3'572 = 212 %**
  (auch mit dem echten Morgenwert; prod lag ueber dem Tagesziel, Frage an Fabio).
- `index.html` auf dem Server mit `?v=32b837e9`, `app.js?v=32b837e9` 200.
- SAP-Badge `config_seh_tagesmenge_sap.js`: war seit 09.11. eingespielt (`seh.ziel` mit Quelle `sap`: `api/config` liefert `src: sap`,
  `api/integration/schema` `planned_source: sap`, `meta.ziel` mit `src: SAP`). Im Browser nicht angesehen (Chrome-Erweiterung nicht
  verbunden); Fabio fragen, wo ihm das Badge fehlt.

**Offen:** CO-PA (Verkauf AE/Umsatz IST, `CE11000`): `T76K912866` laut Ingo in P76; fehlen Gateway-Caches P76 (Freigabe Ingo), Vorschau
gegen Fabios September, `SapCoPaEnabled`. Warum 04:00 am 13.11. nicht lief (vermutlich App-Pool ohne Dauerbetrieb, ISS-017, nicht
untersucht). Entscheid Ingo zum 09.11. (siehe oben). Fabio: DW-Verzug 9'575 und SEH 12.11. 212 % pruefen, plan5-Annahme bestaetigen.
Antwort an Fabio noch nicht gesendet.

## 22. Paket Fabio 13.11. (`pakete.zip`): Excel der Teamleiter abloesen, Phase A deployed (Stand 2026-11-13)

*Nachtrag 2026-11-16: Import nach P76 und Einschalten stehen in Abschnitt 23. Ueberholt sind in diesem Abschnitt „Schalter aus“
fuer Status- und Materialfilter, „offen, nicht freigegeben“ fuer `T76K912906`, die Liste „Reihenfolge bis produktiv“ (erledigt bis
auf `SapCoPaEnabled` und `Zc22ArbplAusschluss`; die Caches in P76 wurden nicht geleert) und die Vermutung, der hoehere DW-Vorrat
komme von DL6/DW1.*

Fabio (per Teams, seine Mails gehen nicht mehr hinaus): Das Shopfloor-Excel soll abgeloest werden; dafuer muessen die SAP-Felder
dieselben Werte zeigen wie das Excel der Teamleiter. Er hat alle 106 SAP/MES-Felder gegen P76 und das Excel abgeglichen: 10 stimmen,
28 geliefert aber falsch, 48 fehlen mit fertiger Definition, 20 fehlen mit offener Quelle (klaert er mit den Teamleitern). Paket
unter `docs/shopfloor_paket_2026-11-13/` (Freigabe Ingo): `Feedback_Ingo_2026-11-13.md` (Befund je Punkt mit Kontrollwerten),
`Shopfloor_SAP_Felder_Status_2026-11-13.xlsx` (alle Felder), ZC22-Abzug 13.11. 09:05 (Eckendtermin bis 20.11.), CE11000-Kontrollwerte
November je Stichtag, Patch 08 und zwei Config-Skripte. Plan im Chat, Entscheide Ingo 13.11. ~11:55: Phase A (Cockpit) **und** B (SAP)
umsetzen, P76 nur lesend fuer ZC22-Layout und ZC27-Quelltext, Verzug ab 01.01.2026, Paket ins Repo.

**Phase A, deployed 12:22 (`d754a76`, eigener Zweig `shopfloor-paket-1311` ab `df48301` ohne Spanien, gemergt `7a8b97d`):**
- **Vorrat rollend (Punkt 1):** `ShopfloorSapVorrat.Grenzen(day, wochen)`: SEH, TR5, TX Eckendtermin bis D + 1 / 2 / 3 Kalendermonate
  (`AddMonths`, 31.01. + 1 = 28.02.), DW (`WochenModule`) bis D + 28 / 56 / 84 Tage, kumuliert mit Ueberfaelligen. Die Spezifikation
  vom 07.11. (Monatsende) war laut Fabio falsch. Die Feldnamen `vorrat_1m` usw. bleiben, auch fuer DW.
- **Materialfilter (Punkte 2, 3, 6):** neu `Shopfloor:MaterialFilter = "TR5,DW"` fuer Vorrat **und** Verzug (ersetzt
  `VerzugMaterialFilter`); nur Materialnummern 1 bis 99999. SEH und TX ohne Filter. Bei TR5 faellt damit auch EKB weg (nur
  alphanumerische Materialien).
- **DW (Punkt 3):** `ShopfloorSapVerzug.Abteilungen` DW = `D*` ohne Ausnahmen. Der Entscheid „D* ohne DW1/DL6, inklusive DS1/DS2/DSN“
  ist ersetzt; DS1 und DW1 fallen ueber den Materialfilter weg.
- **plan5 (Punkt 5):** kumuliert bis zum 5. Arbeitstag nach D, mit ueberfaelligen und heutigen Auftraegen (die Annahme vom 10.11.
  ohne Ueberfaellige ist ueberholt).
- **Verzug (Punkt 6):** `Shopfloor:VerzugAb = "2026-01-01"` (Eckendtermin ab 01.01.2026 bis Vortag); Vorschau zeigt `ab`,
  `vor_untergrenze`, `materialfilter`. **Ab Januar 2027 mit Fabio neu klaeren**, sonst faellt der Verzug aus 2026 weg.
- **Monat des letzten Arbeitstags (Punkt 9):** `ShopfloorSapVerkauf.LetzterArbeitstag` (Mo bis Fr ohne 01.01. und 02.01., **Annahme**,
  mit Fabio zu bestaetigen) fuer J4 (Fiori) und CO-PA. Vorschau `sap/verkauf?tag=2026-11-02` zeigt jetzt Oktober (`202610`).
- **CO-PA HZDAT vorbereitet (Punkt 8):** `ShopfloorSapCoPa.Filter(..., hzdat)` haengt `and Hzdat le 'JJJJMMTT'` (Vortag) an, Schalter
  `Shopfloor:SapCoPaHzdat` aus, bis `CoPaSumSet` die Eigenschaft kennt (Phase B). `SapCoPaEnabled` bleibt aus.
- **Patch 08 und Configs:** Fabios `apply_patch.js` (08: PPA-Grafik und Forecast ohne CZ), `config_module_entfernen.js` (Module `cz`,
  `mtf`, `sensoren` samt Agenda F1, F2, IN1; erfasste Werte bleiben in der DB) und `config_verkauf_tagesumsatz.js` (Tagesumsatz am
  Monatswechsel = Umsatz IST, wenn der Wert unter die Haelfte faellt), je zuerst `--check`. Danach 103 SAP- und 3 MES-Felder, zusammen
  Fabios 106. `index.html` `?v=a337e9cd`. Das Backend belieferte die drei Module nie; der Forecast-Dienst ordnet CZ-Auftraege weiter zu,
  die Oberflaeche zeigt sie nicht mehr. Handbuch Kapitel 10 nachgefuehrt.
- Tests: Shopfloor 147/147, Release-Worktree 1569/1569; Deploy bitgleich, ohne Alarm. Protokoll
  `.tmp_deploy_2026-11-13_shopfloor_paket_fabio.log`.

**Vorschau nach dem Deploy (nur GET, 12:25) gegen Fabios Werte (P76 13.11. ~09:00):**

| Feld | Fabio ZC22 | Vorschau neu | Bemerkung |
| --- | ---: | ---: | --- |
| SEH 1 / 2 / 3 Mte | 38'557 / 51'955 / 80'507 | 38'087 / 51'671 / 80'020 | passt (drei Stunden spaeter) |
| TR5 total / 1 Mte / KW | 61'857 / 15'477 / 864 | 62'326 / 15'506 / 902 | passt |
| DW total / 1 Mte (4 Wo) / KW | 49'092 / 16'730 / 726 | 50'491 / 16'528 / 626 | nahe; total 1'400 hoeher, offen |
| TX total / KW / plan5 | 48'898 / 846 / 7'083 | 49'407 / 1'297 / 7'360 | **weiter rund 450 bis 500 zu hoch** |
| Verzug SEH / TR5 / TX / DW | 33 / 304 / 144 / 441 | 57 / 543 / 567 / 555 | DW = Fabios Abzug (555); Rest offen |

Die Werte vom 13.11. bleiben eingefroren (Stand 06:16); die neuen Regeln schreiben ab dem ersten Lauf des naechsten Arbeitstags.
`POST sap/vorrat?ersetzen=1` heute bewusst nicht, weil es auch das SEH-Tagesziel mit dem Nachmittagsrest ueberschriebe. 09. bis 13.11.
lassen sich nicht neu rechnen (ShopAufSet kennt nur die heute offenen Auftraege).

**Befunde aus dem Nachrechnen des Pakets:**
- **Verzug-Luecke = ZC22-Layoutfilter** (*Vermutung bestaetigt durch den P76-Befund, siehe unten*). TR5 Dashboard 559 (alle Materialien) gegen Fabios Abzug 320, Luecke 239, genau wie
  am 09.11. die Luecke durch den Filter des Standardlayouts `/ P00 TX` auf Arbeitsplatz und Systemstatus (465 gegen 226, Abschnitt 19).
  In Fabios Abzug ist der aelteste TX-Auftrag vom 10.11., das Dashboard zaehlt 16 ueberfaellige TX-Auftraege. Der Entscheid vom
  13.11. morgens „ohne Layoutfilter“ waere dann der Grund der Abweichung. Pruefung in P76 laeuft (Phase B).
- **DW-Verzug:** Fabios eigener Abzug ergibt fuer D*, Material 1-99999, bis Vortag 555 (DEF 243, D30 180, DS2 120, D31 7, D20 5), die
  Vorschau ebenfalls 555; die 441 in seinem Text passen nicht zu seinem Abzug (Frage an Fabio). TR5 305, TX 144, SEH 33 stimmen mit
  seinem Text ueberein.
- **Transmitter-Sparte:** Mail 0005, Feldliste „0005 (+0006)“; entscheidet die CO-PA-Vorschau gegen die Kontrollwerte.

**Phase B (SAP): P76-Befund 13.11. 12:27 bis 12:57 (nur gelesen, `saptasks/shopfloor_sap/P76_BEFUND_2026-11-13.md`):**
- **ZC22:** Varianten koennen kein Layout mitgeben; es gilt das globale Standardlayout `/ P00 TX` mit zwei Filtern: Systemstatus
  `E CP 'FREI DRUC RÜCK VOKL*'` (alle Vorgaenge rueckgemeldet, Wareneingang fehlt) und Arbeitsplatz des naechsten Vorgangs
  (`Z_MD04_NEXT_OPERATION`) `I NP 'M*'`, Letzteres nur mit „Mit Vorgangsdaten“ (alle 18 `/PPA`-Varianten haben es). Set VZ/VR = ZC22
  ohne Filter je Auftrag. **Der Statusteil allein ergibt Fabios Verzug exakt** (TR5 304 mit Untergrenze 2026, TX 144, SEH 33), und bei SEH
  liegen Fabios „stimmt“-Werte (Vorrat total 156'679 gegen 156'723, KW 1'863 gegen 1'907) um genau die 44 Stk. der zwei ausgeblendeten
  RÜCK-Auftraege daneben. TX KW-Luecke 889 = 611 Filter + 438 und −160 Zeitversatz. Der M*-Teil ist nicht sicher belegt (Fabios
  Abzug 09:05 enthielt solche Auftraege noch). `/PPA SEH ALLE` nimmt Disponent 012 dazu, `/PPA ELX` hat die Untergrenze 01.10.2026
  (nicht nachgebildet). DW nicht gemessen.
- **ZC27:** Quelltext P76 = T76, unser `ShopRmSet` rechnet dieselbe Vorgangsregel (hoechster Vorgang an den gewaehlten Arbeitsplaetzen).
  Fabios Excel-Werte entstehen durch das ZC27-Standardlayout `/NIN`: Material `E CP` `E*`, `D*`, `F*`, `C*`. Nachweis MLE02 05.11.:
  ZC27 mit `/NIN` 644, ohne Filter 1'059 = Set; ebenso MLE01 03.11. (712/760), MLE04 03.11. (353/744), DLQ72 05.11. (547/659). **Fabios
  Vorschlag „letzter Vorgang des ganzen Auftrags“ waere falsch** (letzter Vorgang ist 9990 an MLEAUTO, Werte fielen auf null; erklaert
  auch seine Beobachtung mit dem Intervall MLE01 bis MLM01).
- **Entscheide Ingo 13.11. nachmittags:** ZC27-Materialfilter wie `/NIN` bauen (Set-Filter optional, Cockpit-Schalter); ZC22-Statusfilter
  fuer alle Abteilungen nachbilden (vor dem Einschalten DW im selben Moment gegen ZC22 pruefen); M*-Teil bauen, Schalter aus, Fabio
  fragt die Teamleiter, ob sie mit Vorgangsdaten fahren. Bedingung: Der Import aendert keine Zahl, eingeschaltet wird im Cockpit.
- **T76 fertig, Auftrag `T76K912906` (Aufgabe `T76K912907`, offen, nicht freigegeben, fuenf Objekte):**
  - `CoPaSumSet` + Filter `Hzdat` (`ZSTR_COPA_SUM-HZDAT`, Vorgabe `saptasks/copa_verkauf/ZCOPA_SUM_HZDAT_2026-11-13.abap`): ohne
    Filter Summen gleich vorher; mit `Hzdat le` per RFC gegen CE11000 gleich (15 Gruppen, drei Stichtage, 0 Abweichungen).
  - `ShopRmSet` + Filter `Matex` (Musterliste, Material wie ZC27 = erste AFPO-Position): DLQ72 14.04.2026 ohne 494/28, mit
    `E*,D*,F*,C*` 394/21 = ZC27 mit `/NIN`; MLE04 und MLE01 unveraendert.
  - `ShopAufSet` VR/VZ + `Sttxt` (immer, ZC22-Logik `status_lang`, Sprache fest D, weil das Gateway zwischen en und de wechselt),
    `Arbpl`/`Vornr` nur mit `Avo eq 'X'` (`Z_MD04_NEXT_OPERATION`, Echo `Avo = X`). Ohne `Avo` Grunddaten unveraendert (VR 10'955 Zeilen,
    MD5 vorher = nachher). Gegen ZC22 mit Vorgangsdaten fuer alle 2'197 Fertigungsauftraege 0 Abweichungen bei Status, Arbeitsplatz,
    Vorgang. Laufzeit T76: ohne `Avo` 0,2 bis 0,3 s, mit `Avo` rund 8 ms je Fertigungsauftrag (alle Seiten 25 s gegen 7 s).
  - Plan `saptasks/shopfloor_sap/PLAN_LAYOUTFILTER_2026-11-13.md`, Ergebnis `saptasks/shopfloor_sap/NOTES.md` 10, Zusatz
    `docs/abap/ZSHOP_LAYOUTFILTER_ADD.abap`.
- **Cockpit (deployed 14:33, `9100537`, Schalter aus):** `ShopfloorLayoutFilter`; `Shopfloor:Zc22StatusAusschluss`,
  `Zc22ArbplAusschluss` (fordert `Avo` an, prueft das Echo), `RmMaterialAusschluss`; Vorschau-Parameter `zc22status`, `zc22arbpl`,
  `matex` (nur GET). Fehlen die SAP-Felder, Fehler statt Zahl.
- **Reihenfolge bis produktiv:** (1) Ingo gibt `T76K912906` frei und importiert nach P76; (2) P76-Caches `/IWBEP` (alle), dann
  `/IWFND` (`ZPOWERBI_EINKAUF_MDL`); (3) Vorschau mit Probeparametern gegen ZC22/ZC27 in P76 (auch DW im selben Moment) und eine
  Laufzeitmessung `Typ eq 'VR' and Avo eq 'X'`; CO-PA-Vorschau mit `SapCoPaHzdat` gegen Fabios Kontrollwerte November (Sparte 0005
  oder 0005+0006 klaeren); (4) Schalter `Zc22StatusAusschluss = "FREI DRUC RÜCK VOKL*"`, `RmMaterialAusschluss = "E*,D*,F*,C*"`,
  `SapCoPaHzdat`/`SapCoPaEnabled` setzen und deployen; `Zc22ArbplAusschluss` erst nach Fabios Antwort; (5) Logistik-Historie mit
  `POST sap/mengen?tag=` nachladen.
- **Abnahme Logistik vor dem Einschalten:** `GET sap/mengen?tag=` fuer 02. bis 12.11. ohne und mit `matex=E*,D*,F*,C*`: Fabios 18
  Kontrollwerte (Feedback Punkt 6) muessen mit Filter stimmen, und MLE08+09, MLM01 und `tr5.prod` duerfen sich nicht bewegen (der
  Materialausschluss gilt fuer jede ShopRmSet-Gruppe). DW: der Vorrat lag in der Vorschau 12:25 rund 1'400 ueber Fabios ZC22 (DL6 und
  DW1 mit numerischem Material zaehlen jetzt mit); vor dem Einschalten im selben Moment gegen ZC22 D* mit Layout pruefen.
- **Oberflaeche nach Patch 08 (15:13):** im Chrome nicht ansehbar (Arbeits-Chrome meldete zweimal `ERR_NAME_NOT_RESOLVED` fuer den Servernamen, curl vom selben Rechner ging). Ersatzweise per HTTP geprueft: `index.html` laedt `app.js?v=a337e9cd`, die ausgelieferte `app.js` traegt den Marker „Patch 08 (2026-11-13)“ und parst fehlerfrei (Node `new Function`), `api/config` hat 23 Module (ohne `cz`, `mtf`, `sensoren`, `czch` bleibt) und 23 Agenda-Eintraege, alle mit vorhandenem Modul, Tagesumsatz mit neuer Formel. Die Seiten PPA-Grafik und Forecast im Browser sind damit **nicht** gesehen (Sichtpruefung Ingo oder Fabio).
- **Montag 04:00 stimmt noch nicht ueberall mit dem Excel** *(Nachtrag 16.11.: so eingetroffen; Filter seit 17:06 ein, erster
  Stichtag mit Statusfilter ist der 17.11., Abschnitt 23)*: richtig sind dann die rollenden Fenster, TR5/DW-Material und der DW-Verzug;
  TX, Verzug TR5/TX/SEH, die Logistik und CO-PA erst nach Import und Einschalten. Der am 02.11. gespeicherte Verkaufswert (November statt
  Oktober) bleibt, die neue Monatsregel gilt ab jetzt (Neuschreiben nur auf Wunsch Ingo/Fabio).
- Nebenwirkungen des Lesens: SE16 hat fuer KOI bei AFVC „Felder fuer Selektion: AUFPL“ gespeichert; ueber die Freigabe hinaus nur
  Anzeige (Liste im Befund, Abschnitt 6).

**Nicht gebaut (Phase C):** die 40 Listenfelder (Rueckstandslisten, TVAV, Angebote, Punkt 10; Fabios Tabellenvorschlaege ausser
`MARA-ZZTYP` ungeprueft), FPY TR5/TX aus dem MES (Punkt 11, DB-User Marco). Fabios Fragen an die Teamleiter (`Mails_2026-11-13.md`,
Mail 2) laufen bei ihm.

## 23. Layoutfilter eingeschaltet: Statusfilter ZC22 und Materialfilter ZC27 produktiv, Logistik 02. bis 12.11. nachgeladen (Stand 2026-11-16)

Anlass: Fabio per Teams am 16.11. („arbeitet Claudia weiter am Shopfloor? Ich glaube es ist nichts geaendert seit Freitag“). Ingo hat
`T76K912906` freigegeben und nach P76 importiert (Meldung 16.11. ~16:10, „du kannst dein Teil machen“, P76 fuer das Lesewerkzeug
offen). Die P76-Caches hat Ingo **nicht** geleert.

**Vor dem Einschalten (alles nur gelesen):**
- **Erster Aufruf nach dem Import:** `GET sap/verzug` mit `zc22status=…` antwortete 16:12:49 mit 502 („ShopAufSet liefert die
  Statuszeile (Sttxt) … noch nicht“); ab 16:13:33 lieferten alle Aufrufe die neuen Felder (46 eigene, 40 des Agenten). Ursache nicht
  untersucht. **Vermutung:** der erste Aufruf traf im Gateway noch den alten Stand. Fuer den Betrieb heisst das: fehlen die Felder,
  schreibt das Cockpit nichts und meldet den Fehler in `sap/status` (Fehler statt Zahl, Abschnitt 22).
- **Logistik (ZC27, Materialfilter):** `GET sap/mengen?tag=` fuer 02. bis 12.11. ohne und mit `matex=E*,D*,F*,C*`: mit Filter stimmen
  **18 von 18** Kontrollwerten Fabios (Feedback Punkt 6). Der Filter bewegt nur `logistik.tr5_mle01`, `logistik.tr5_mle02`,
  `logistik.dw_mle04`, `dw.prod` und `dw.lieferbereit`; `tx_mle08`, `tx_mlm01`, `tr5.prod`, `seh.prod`, `seh.fpy` bewegen sich nicht.
- **ZC22 (Statusfilter), Abnahme in P76 16:24 bis 16:40** (Agent `sap-t76`, eigene GUI-Sitzung, fuenf ZC22-Laeufe nur gelesen, Sitzung
  geschlossen; `saptasks/shopfloor_sap/P76_ABNAHME_2026-11-16.md`): Vorschau mit `zc22status=FREI DRUC RÜCK VOKL*` gegen ZC22 im
  selben Moment, **90 Wertepaare ohne Abweichung**. Verzug mit Filter SEH 20 (ausgeblendet 1 Auftrag / 24 Stk.), TR5 287 (4 / 239),
  TX 342 (8 / 148), DW 240 (0 / 0); Vorrat ausgeblendet SEH 4 / 214, TR5 4 / 239, TX 13 / 285, DW 0 / 0. Belegt ist die Gleichheit
  von Anzahl und Stueck je Abteilung und Feld, nicht je Auftrag (die Vorschau liefert Summen).
- **DW:** gegen ZC22 `D*` **ohne** Layout exakt gleich (1'582 Auftraege; total 49'196, 4 Wochen 15'828, laufende KW 2'598). Gegen
  ZC22 **mit** Layout `/ P00 TX` liegt das Cockpit 36 Auftraege / 540 Stk. hoeher (laufende KW 522): alles Arbeitsplatz `M*` an MLE04.
  Der Statusfilter aendert bei DW nichts. Die Vermutung vom 13.11. (DL6/DW1 mit numerischem Material) trifft nicht zu.
- **Arbeitsplatzfilter `M*` (nur gemessen, bleibt aus):** er wuerde zusaetzlich ausblenden im Vorrat TR5 19 / 588, TX 2 / 105,
  DW 36 / 540, im Verzug TR5 1 / 2, TX 1 / 5, DW 5 / 10 (Auftraege / Stk.), SEH nichts.

**Eingeschaltet, deployed 17:06 (`7cc1a62`, Zweig `shopfloor-einschalten-1611` ab `2da2682`, gemergt `69ed8a0`).** Geaendert sind nur
`appsettings.json` und der Test der Schalterstellung (`ShopfloorLayoutFilterTests`):

| Schalter | Wert | Stand |
| --- | --- | --- |
| `Shopfloor:Zc22StatusAusschluss` | `FREI DRUC RÜCK VOKL*` | **ein** (Verzug und Vorrat, alle Abteilungen) |
| `Shopfloor:RmMaterialAusschluss` | `E*,D*,F*,C*` | **ein** (jede `ShopRmSet`-Gruppe) |
| `Shopfloor:SapCoPaHzdat` | `true` | ein, wirkt vorerst nur in der Vorschau `sap/copa` |
| `Shopfloor:SapCoPaEnabled` | `false` | bleibt aus *(ueberholt 17.11. 12:25: `true`, Abschnitt 24)* |
| `Shopfloor:Zc22ArbplAusschluss` | leer | bleibt aus, wartet auf Fabio *(ueberholt 17.11. 12:25: `M*`, Entscheid Ingo, Abschnitt 24)* |

Tests: Shopfloor 164/164, Release-Worktree 1595/1595. Deploy bitgleich, ohne Alarm, 6 Dateien geaendert. Die Wochensicherung war
faellig und lief wegen 249 MB im `-wal` seitenweise (818 s, dazu 68 s Uebertragung), deshalb Start 16:51 und DLL erst 17:06:28.
Protokoll `.tmp_deploy_2026-11-16_shopfloor_filter_ein.log`.

**Nach dem Deploy:**
- **Startlauf 17:07** (`sap/status` ok): Verzug und Vorrat mit `layoutfilter.status_ausschluss = FREI DRUC RÜCK VOKL*`; ausgeblendet
  genau wie in der Abnahme (Verzug SEH 1 / 24, TR5 4 / 239, TX 8 / 148, DW 0 / 0; Vorrat SEH 4 / 214, TR5 4 / 239, TX 13 / 285,
  DW 0 / 0), DW-Vorrat 49'196 / 15'828 / 2'598 wie ZC22 in der Abnahme. Damit ist auch der **konfigurierte Schalter** gegen die
  Zahlen der Abnahme gehalten (die Abnahme lief mit dem Probeparameter). Verzug 17:07: SEH 20, TR5 263, TX 342, DW 240 (bei TR5 ein
  Auftrag mit 24 Stk. weniger als in der Abnahme, 8 statt 9 Auftraege; sonst gleich). Die Mengen
  13. bis 16.11. hat der Startlauf mit Materialfilter geschrieben.
- **Stichtagswerte 16.11. bleiben eingefroren** (erster erfolgreicher Lauf 06:21, noch ohne Statusfilter: Verzug SEH 85, TR5 533,
  TX 733, DW 531). Kein `ersetzen`: es speicherte Abendwerte als Morgenwerte und ueberschriebe das SEH-Tagesziel. **Der erste
  Stichtag mit Statusfilter ist der 17.11.**
- **Logistik-Historie nachgeladen** (`POST sap/mengen?tag=` um 22:07 fuer 02., 03., 04., 05., 06., 09., 10., 11., 12.11. und um
  22:26 fuer Samstag, 07.11.; je `ok`, `geschrieben`, kein manueller Wert uebersprungen). Gespeichert = Vorschau mit Filter; **Fabios 18 Kontrollwerte stehen
  gespeichert**. Bewegt haben sich 20 Werte: `dw.lieferbereit` (8 Tage), `logistik.tr5_mle02` (4), `logistik.dw_mle04` (3),
  `dw.prod` (3, darunter Samstag 07.11. von 22 auf 0), `logistik.tr5_mle01` (1) und `logistik.tx_mle08` am 03.11. von 832 auf 831 (nicht vom Filter; die Vorschau ohne
  Filter zeigte schon 831). Sonst nichts, auch kein Vorrat-, Verzug- oder Verkauf-Feld. Den 13.11. hatte schon der Startlauf neu
  geschrieben (`dw.lieferbereit` 1'165 auf 1'024, `dw.prod` 1'028 auf 882, `logistik.dw_mle04` 932 auf 923). Der Stand vor dem
  Einschalten liegt als Sicherung im Scratchpad der Sitzung (`sf1611/mengen/daily_*.json`), nicht im Repo.
  **Der Samstag 07.11. fehlte zuerst:** nachgeladen waren nur die Arbeitstage; er fiel erst in der Gegenpruefung auf. Am Sonntag
  08.11. gibt es keine Rueckmeldungen und keinen gespeicherten Wert.
- **Vor dem 02.11. nicht nachgeladen:** Logistik- und DW-Mengen im Oktober stehen weiter ohne Materialfilter (Entscheid offen).
- **CO-PA-Vorschau mit HZDAT** (`GET sap/copa?tag=`, 22:07, schreibt nichts) gegen Fabios Kontrollwerte
  (`docs/shopfloor_paket_2026-11-13/2026-11-12_verkauf/Kontrollwerte_Verkauf_CE11000_2026-11.csv`), je sechs Werte (Auftragseingang
  und Umsatz, je gesamt, Transmitter, DW):

  | Stand bis HZDAT | Ergebnis |
  | --- | --- |
  | 09.11., 10.11., 11.11. | alle 18 Werte auf den Rappen gleich |
  | 12.11. | Umsatz dreimal gleich, Auftragseingang DW gleich; **Auftragseingang gesamt 3'376'806.32 gegen 3'372'899.64 (+3'906.68), Transmitter 1'151'205.88 gegen 1'147'421.02 (+3'784.86)** |

  Transmitter rechnet das Cockpit als Sparte 0005 allein (`ShopfloorSapCoPa.SparteTransmitter`). Das trifft Fabios Werte an diesen
  Stichtagen. Ob die Sparte 0006 im November ueberhaupt Werte traegt, ist nicht geprueft (die Vorschau liefert nur Summen); die Frage
  „0005 oder 0005 + 0006“ aus Abschnitt 22 ist nur so weit beantwortet. Die Differenz am 12.11. ist nicht untersucht; **Vermutung:**
  Fabios Datei liegt im Paketordner `2026-11-12_verkauf`, ist also am 12.11. gezogen, und die Zeile „Stand bis 12.11.“ enthaelt den
  Auftragseingang jenes Tages nur bis zum Abzug. `SapCoPaEnabled` bleibt aus bis zum Entscheid.
- **Oberflaeche (Arbeits-Chrome, 22:09 bis 22:20):** Cockpit laedt; Logistik und DW zeigen an den nachgeladenen Tagen
  „SAP-Schnittstelle 22:07“. Abgelesen: Logistik 05.11. TR5 MLE02 644 und DLQ72 547; Logistik 09.11. MLE02 290, MLE04 1'154, DLQ72 854;
  DW 10.11. DL790 813 und DLQ72 795, alles wie Fabios Kontrollwerte. Ein Tag laesst sich direkt oeffnen:
  `shopfloor/index.html#/m/<modul>?day=JJJJ-MM-TT`.
- **Zeitlicher Ablauf:** Der Deploy endete 17:06, die Pruefung danach begann erst 22:06. Die Meldung vom Deploy-Ende kam in der
  Sitzung erst 22:05 an; **warum, ist nicht untersucht** (das Ereignisprotokoll zeigt den Standby des Laptops erst ab 19:25, die
  Zeit davor ist nicht erklaert; das VPN trennte 22:04:46 und stand 22:06:34 wieder). Von 17:07 bis 22:07 (Samstag 07.11. bis
  22:26) standen deshalb die Tage 02. bis 12.11. noch ohne Materialfilter in der Datenbank.

**Offen:** *(Stand 17.11. 12:30, Abschnitt 24: Punkt 1 erledigt, Lauf 04:00 hat stattgefunden; Punkte 2 und 3 durch Entscheid Ingo
erledigt, `M*` und CO-PA ein; offen bleibt aus 3 die Differenz vom 12.11.)*
1. **17.11. morgens:** ersten Stichtagslauf mit Statusfilter pruefen (`sap/status` ok, `layoutfilter` gesetzt, Werte geschrieben),
   auch wegen des 502 beim ersten Aufruf nach dem Import.
2. **Arbeitsplatzfilter `M*`** (`Zc22ArbplAusschluss`): Frage an Fabio, ob die Teamleiter ZC22 „Mit Vorgangsdaten“ fahren. Solange er
   aus ist, liegt der Vorrat DW 540 Stk. (TR5 588, TX 105) ueber der Liste der Teamleiter.
3. **CO-PA schreiben** (`SapCoPaEnabled`): Entscheid Ingo/Fabio; vorher die Differenz im Auftragseingang vom 12.11. klaeren.
4. **Logistik-Historie vor dem 02.11.** nachladen oder stehen lassen. Bis dahin mischen die 8-Wochen-Grafik und der Verlauf auf den
   Seiten Logistik und DW den gefilterten November mit dem ungefilterten Oktober. Beim Nachladen die Samstage mitnehmen (Tage aus
   `/daily` lesen).
5. Die ZC22-Variante `/PPA DW/S ALLE` begrenzt den Eckendtermin auf 01.11.2025 bis 31.12.2026; am 16.11. lagen 908 Auftraege /
   29'509 Stk. danach (Abnahme Abschnitt 6.3, Frage an Fabio, mit welcher Selektion die Teamleiter fahren). `VerzugAb` ab Januar 2027
   neu klaeren (Abschnitt 22).
6. Phase C unveraendert (Abschnitt 22).

## 24. CO-PA schreiben und Arbeitsplatzfilter `M*` eingeschaltet, CO-PA 02. bis 16.11. nachgeladen (Stand 2026-11-17)

Anlass: Fabio per Teams, die Zahlen vom Freitag seien noch nicht drin. Entscheid Ingo 17.11. mittags: „8. ein, 3, 4 so dass es
passt schalten“ (Punkt 8 = Verkauf AE/Umsatz IST aus CO-PA, Punkte 3/4 = Vorrat DW und TX wie die Liste der Teamleiter, also das
ganze ZC22-Layout `/ P00 TX` mit Arbeitsplatzteil `M*`). Damit sind „wartet auf Fabio“ (Abschnitt 23, Offen 2) und „vorher die
Differenz vom 12.11. klaeren“ (Offen 3) durch den Entscheid ersetzt; die Differenz selbst bleibt offen (unten).

**Erster Stichtagslauf 17.11. (Abschnitt 23, Offen 1):** Der Lauf 04:00 hat stattgefunden (Werte mit SAP-Kennzeichen 04:01:35, z. B.
Verzug SEH 80, TR5 563, TX 545, DW 336; TX-Vorrat 52'214), also anders als am 13. und 16.11. puenktlich. Dass der Statusfilter um
04:01 griff, ist aus den gespeicherten Werten nicht einzeln belegt; der Schalter stand seit 17:06 des Vortags und griff im Lauf 17:07.

**Vorschau produktiv vor dem Umschalten (nur GET, 17.11. ~12:00, Status `FREI DRUC RÜCK VOKL*` gesetzt):**

| Feld | ohne `M*` | mit `M*` | Differenz |
| --- | ---: | ---: | ---: |
| Vorrat total TR5 / TX / DW | 60'437 / 54'428 / 48'984 | 60'191 / 53'971 / 48'530 | −246 / −457 / −454 |
| offene KW TR5 / TX / DW | 1'758 / 4'022 / 2'224 | 1'647 / 3'565 / 1'780 | −111 / −457 / −444 |
| TX plan5 | 5'477 | 5'020 | −457 |
| Verzug SEH / TR5 / TX / DW | 60 / 388 / 381 / 333 | 60 / 307 / 376 / 312 | 0 / −81 / −5 / −21 |
| SEH (alle Felder) | gleich | gleich | 0 |

Laufzeit mit `Avo`: Vorrat 24.4 s, Verzug 1.9 s (ohne `Avo` 2.1 s / 0.4 s); Zeitlimit des Clients 5 Minuten. TX liegt mit `M*` heute
457 Stk. tiefer, das trifft Fabios Befund „TX rund 450 bis 500 zu hoch“ (Abschnitt 22, Punkt 4). Am 16.11. waren es bei TX nur 105
Stk. (der Wert haengt davon ab, welche Auftraege gerade an einem `M*`-Arbeitsplatz stehen). Grundlage, dass Status oder `M*` genau
die ZC22-Ausblendung ergibt: P76-Abnahme 16.11. Abschnitt 8 (alle Abteilungen, 0 / 0 Abweichung). Die Vorschau mit `zc22arbpl=M*`
ist nicht im selben Moment gegen ZC22 gehalten worden (kein P76-Lauf heute).

**CO-PA-Vorschau 17.11.** (`GET sap/copa`, Stand bis 16.11.): AE 4'072'454.89, TX 1'375'254.74, DW 1'711'890.38, Umsatz IST
3'514'314.84, TX 1'136'200.25, DW 1'761'925.58.

**Umgesetzt (`3298c93`, Zweig `shopfloor-copa-arbpl-1711` ab `6a7de50` ohne Spanien, gemergt `684ae25`):** `appsettings.json`
`Shopfloor:SapCoPaEnabled = true`, `Shopfloor:Zc22ArbplAusschluss = "M*"`; Test `Schalter_Status_Arbeitsplatz_Material_Und_CoPa_Ein`
(vorher `Schalter_Seit_Dem_Import_In_P76_Status_Und_Material_Ein_Arbeitsplatz_Aus`); Kommentare in `ShopfloorOptions`. Shopfloor
164/164, Release-Worktree 1607/1607. **Deploy 12:25** (bitgleich, ohne Alarm, 6 Dateien geaendert, keine Wochensicherung faellig),
Protokoll `.tmp_deploy_2026-11-17_shopfloor_copa_arbpl.log`. Einkauf-Delta vorher geprueft (letzter Lauf 16.11. 14:34 erfolgreich,
keiner aktiv). Der Protokollabsatz des Runners traegt noch den Titel des Deploys vom 12.11.; massgebend ist die Commit-Zeile `3298c93`.

**Nach dem Deploy:**
- Startlauf 12:26 (`sap/status` ok): `copa` geschrieben (Werte wie Vorschau, nichts manuell uebersprungen); Verzug und Vorrat mit
  `layoutfilter` Status **und** `arbeitsplatz_ausschluss: M*`, alle 22 Vorrats- und 4 Verzugsfelder `eingefroren`. **Die
  Stichtagswerte vom 17.11. bleiben deshalb ohne `M*` (Stand 04:01); der erste Stichtag mit dem ganzen Layout ist der 18.11.** Kein
  `?ersetzen=1`, weil es auch das SEH-Tagesziel mit dem Rest des Mittags ueberschriebe.
- **CO-PA nachgeladen (Ja Ingo 17.11.):** `POST sap/copa?tag=` 12:28 fuer die gespeicherten Tage 02., 03., 04., 05., 06., 09., 10.,
  11., 12., 13. und 16.11.; je `ok`, `geschrieben`, nichts uebersprungen. Gegen Fabios Kontrollwerte (Eintrag Tag D = Stand bis D−1):
  03. bis 12.11. (Stand bis 02. bis 11.11., der 09.11. = Stand bis 07.11.) **alle sechs Werte auf den Rappen gleich**; 13.11. (Stand bis
  12.11.) Umsatz dreimal und AE DW gleich, **AE gesamt +3'906.68 und AE Transmitter +3'784.86** (bekannt aus Abschnitt 23, Ursache
  weiter nicht untersucht). Der 02.11. traegt nach der Monatsregel den Oktober (AE 7'363'938.76, Umsatz IST 8'585'235.01).
- Oberflaeche (Arbeits-Chrome, `#/m/verkauf?day=2026-11-13`): KW 46 zeigt AE und Umsatz je Tag mit „SAP-Schnittstelle“, Kacheln
  Umsatz IST November 3'239'687 (49.8 % vom Ziel), AE 3'376'806, Book to Bill 1.04.

**Offen:**
1. 18.11. morgens: Stichtagslauf mit dem ganzen Layout pruefen (`layoutfilter.arbeitsplatz_ausschluss`, Laufzeit, Werte geschrieben).
2. Differenz AE 12.11. (+3'906.68) mit Fabio klaeren (Vermutung weiter: seine Datei am 12.11. waehrend des Tages gezogen).
3. Logistik-Historie vor dem 02.11., Selektion `/PPA DW/S ALLE`, `VerzugAb` ab 2027, Phase C: unveraendert wie Abschnitt 23.
4. Handbuch Kapitel 10 (Verkauf) ist im Repo nachgefuehrt, live erst mit dem naechsten Deploy.

*Nachtrag 2026-11-17 12:50, Ruecksetzer:* Der Handbestellwesen-Deploy 12:36 (`44a3ed1`, Zweig ohne `3298c93`) nahm beide Schalter
zurueck; die Sitzung 6d hat `3298c93` gemergt und 12:43 `2e62163` deployed (im Arbeitszweig `fd49894`, Einzelheiten in
`docs/rag/DEPLOYMENT.md`). Geprueft 12:50 im Arbeits-Chrome: Lauf 12:43 mit `arbeitsplatz_ausschluss: M*` (Verzug und Vorrat),
`copa` geschrieben; Verkauf 12., 13., 16. und 17.11. unveraendert (AE 17.11. 4'072'454.89, Stand 12:26). Nachzuladen ist nichts; die
Stichtagswerte vom 17.11. waren schon seit 04:01 eingefroren. Das Handbuch-Kapitel ist auch in `2e62163` noch nicht enthalten.

*Nachtrag 2026-11-17 nachmittags, Phase C: was Fabio liefern muss* *(teilweise ueberholt durch Abschnitt 25: Anh Nhi, Michael und der Einkauf haben geantwortet; offen bleiben MES-DB-User, Sanel, Offene K-Auf, Handfelder)* (Frage Ingo; Grundlage `docs/shopfloor_paket_2026-11-13/2026-11-13_abgleich/Mails_2026-11-13.md` Mail 2 und Feedback „Nicht geprueft“; ob Fabio die Mail
gesendet hat und ob Antworten da sind, ist nicht dokumentiert; seine Frist an die Empfaenger war Freitag, 20.11.2026). Gebaut wird
erst, wenn die Quellen geklaert sind:
1. **40 Listenfelder (Rueckstandslisten, TVAV, Angebote):** Fabios Tabellen-/Feldvorschlaege sind ausser `MARA-ZZTYP` nicht in SAP
   geprueft. Noetig: von Anh Nhi (PPD) Bedeutung und SAP-Ort von „VkNr“, Feld und Regel fuer Wunschtermin bzw. Aufnahme in die
   Rueckstandsliste, ob „Datum WE geplant“ der Eckendtermin des FA ist; vom Einkauf Bedeutung und Schluessel von „WE aktuell gem. SAP“
   und ob die Bestellnummer Handfeld bleibt.
2. **FPY TR5 und TX aus dem MES:** eigener, nur lesender DB-User von Marco Di Menco; Fabio fragt nach dem Termin, die Abfrage folgt danach.
3. **20 Felder mit offener Quelle:** Michael (TX) Transaktion/Arbeitsplatz der Tagesmenge TX und ob Schicht 1/2 Handfelder bleiben
   (Tagesplanungstool); Sanel (DW) Report und Auswahl fuer FPY DW; Sacha (Logistik) ob „Offene K-Auf“ noch gebraucht wird.
4. **Umstellung bei den Teamleitern (keine Schnittstelle):** Mitarbeiter je Schicht, frei, krank, Lieferzeiten, Tagesziel TR5 bleiben
   Handfelder und muessen im Dashboard statt im Excel gepflegt werden.

Ausserhalb von Phase C offen bei Fabio: AE-Differenz Stand 12.11. (+3'906.68), Selektion `/PPA DW/S ALLE` (Eckendtermin bis
31.12.2026), Feiertagsannahme Monatswechsel (nur 01./02.01.), plan5 bestaetigen. Bei uns offen: 18.11. ersten Stichtag mit ganzem
Layout pruefen, Logistik-Historie vor 02.11. (Entscheid Ingo).

## 25. Ergaenzung Fabio 17.11.: Schnittstellen-Seite nachgefuehrt, TX-Mengen aus dem MES, Rueckstandslisten geprueft (Stand 2026-11-17)

Anlass: Mail Fabio 17.11. mit Paket `Shopfloor_Ergaenzung_2026-11-17.zip` (abgelegt unter `docs/shopfloor_paket_2026-11-17/`, Original
noch nicht von ihm versendet laut Kopf seines Dokuments, Ingo hat es trotzdem bekommen): Antworten von Anh Nhi (Termine) und Michael
Scalet (TX-Mengen, Schichten, FPY TX), Live-Pruefung des Boards, vier Bitten (Rueckstandslisten, TX-Mengen, FPY TX, Online-Doku).
Auftrag Ingo „mach mal alles“: die vier Punkte, soweit ohne Fabios Entscheide moeglich. *Damit ueberholt im Nachtrag „Phase C“ am
Ende von Abschnitt 24: Anh Nhi hat Wunschtermin und „Datum WE geplant“ fachlich beantwortet, Michael die Quelle der TX-Mengen und
die Schichtregeln, der Einkauf (ueber Fabio 13./16.11.) „WE aktuell gem. SAP“ und die Bestellnummer. Offen bleiben dort der
MES-DB-User (Marco Di Menco), Sanel (FPY DW), Sacha bzw. die Logistik (Offene K-Auf) und die Handfelder.*

**Umgesetzt und deployed 14:36 (`32bf356`, Zweig `shopfloor-ergaenzung-1711` ab Produktivstand `2e62163`, ohne Spanien):**
- **Seite „Schnittstelle SAP / MES“** (`shopfloor_config.json`, Bloecke `sap`; Kopftext in `app.js` `viewSchema`): Jedes der 105
  Felder traegt den echten Stand. 44 „geliefert seit …, fachliche Abnahme offen“ (Vorrat, offene KW, SEH-Tagesziel, plan5, Verzug,
  Produktion TR5/SEH/DW, DW lieferbereit, Logistik, FPY SEH, Verkauf Fiori und CO-PA), 56 „definiert, nicht gebaut“ (Listenfelder,
  FPY TR5, TX-Tagesmenge, WE aktuell), 3 „in Ueberpruefung“ (FPY TX, Schicht 1/2 TX), 2 „offen“ (FPY DW, Offene K-Auf). Quelle, Feld und
  Selektion der gelieferten Felder nach dem Code und `sap/status` (Arbeitsplaetze, Layout- und Materialfilter, Fenster). Ungepruefte
  SAP-Felder bleiben kursiv (`vorschlag_*`), auch fuer VF- und WE-Termin. Der Kopftext zaehlt die vier Stufen statt „definiert“,
  nennt den Abruf ueber das Gateway (Stichtag 04:00) statt „Push vorgesehen“ und „Stand 17.11.2026“ statt „Feldmapping 14.10.“.
  *Die Seite zeigt damit nicht mehr den Stand von Abschnitt 12 (Feldmapping-Excel 14.10.).* Vorher standen 95 von 105 Feldern auf „offen“,
  darunter die meisten der 44 gelieferten (LEARNINGS 17.11.).
- **Bestellnummer im Kommunikationstool Einkauf** ist Handeingabe (kein `src`, Fabio 13.11.).
- **TX Tagesmenge, Schicht 1 und 2** haben die Quelle MES statt SAP. Michaels `ProdMengePerDisponent.xlsm` (nur gelesen, Makros nicht
  ausgefuehrt, Modul unter `docs/shopfloor_paket_2026-11-17/auswertung_ingo/ProdMengePerDisponent_Modul1.bas`): Die Schaltflaeche
  „GetData“ ruft das Makro `ProdMenge`, das per ADODB/SQLOLEDB mit Windows-Anmeldung (kein Passwort in der Datei) auf
  `trch-app-mes.sqldb.trafagch.local`, Datenbank `TMS_MES`, die Prozedur `dbo.getProdQtyPerDayOfMonthPerDisponent` mit `@Month`,
  `@Year`, `@ShopfloorID` (Station, z. B. EL106) aufruft. Sie liefert Tag, VORNR, Dispo und 24 Stundenspalten; Tagessumme und Anteile
  rechnet Excel. Vorgang 0600 und die Disponenten (EL1 bis EL8, ELP, TL1 in den gespeicherten Daten) legt die Prozedur fest, nicht das
  Makro; ihr Text ist nicht bekannt. Nachbau braucht den MES-DB-User und das Recht auf die Prozedur (oder ihren Quelltext).
- **Rueckstandslisten:** Beschriftung „Wunschtermin“ heisst jetzt „Bestaetigter Liefertermin (VF)“ (Vorschlag Fabio, Schluessel
  `wunschtermin` unveraendert; betrifft nur Anzeige und CSV-Kopf).
- Handbuch Kapitel 10 (Schnittstelle) nachgefuehrt; dabei die noch nicht deployten Staende von Kapitel 10 (CO-PA, `b9017bb`) und
  Kapitel 01 (Export-Download) mitgenommen. Test `Konfiguration_Liefert_Nur_FPY_Aus_Dem_MES_Und_Alles_Andere_Aus_SAP`: 99 SAP + 6 MES,
  jeder Status beginnt mit einer der vier Stufen. Shopfloor/Handbuch 168/168, Release-Worktree 1610/1610.
- **Deploy 14:36** mit dem Werkzeug, bitgleich, Commit-Zeile `32bf356`; Alarm nur „geschuetzte Datei trafag_exporter.db veraendert“
  (Checkpoint). Das Einkauf-Delta lief (WAL 250 MB, 14:29) und wurde auf Ingos Anweisung („stope einkaufsdelta mach weiter“) vom
  Neustart abgebrochen; Nachholen faellig. Nach dem Deploy: `sap/status` 14:37 ok, Verzug und Vorrat mit Status- und `M*`-Filter,
  22 Vorratswerte eingefroren (Stichtag 04:01 bleibt), CO-PA geschrieben; Seite im Arbeits-Chrome gesehen (105 Felder, 44/56/3/2).

**Rueckstandslisten produktiv gelesen (nur GET `api/records`, nichts geaendert):** Alle 596 Eintraege (SEH 4, TR5 204, TX 296, DW 92)
stammen unveraendert aus dem Excel-Import vom 05.10.2026; seither hat niemand einen Eintrag im Cockpit bearbeitet. Offen gezaehlt
werden 176 (SEH 1, TR5 36, TX 86, DW 53). **123 der offenen haben ein Abschlussdatum**, ihr Status (meist „FehlMat“, „in Arbeit“) wurde
aber nie auf erledigt gesetzt. Die Listenansicht zaehlt nach dem Status (`isClosed`: beginnt mit „erl“ oder `closed_status`), die PPD-Terminverschiebungen aus den Rueckstandslisten (Patch 06, Abschnitt 18) dagegen nach leerem Abschlussdatum; fuer diese 123 Eintraege widersprechen sich die beiden Zaehlweisen. Weitere
Befunde: Text statt Datum (z. B. RS-SEH-26-004 „ERLEDIGT“ im Abschluss), mehrere Daten in einer Zelle, RS-DW-26-689 ist ein
Hinweistext von 2022, Status „storniert“, „Abgesagt“, „aktuell gesperrt, muss aussortiert werden“ zaehlen als offen. Liste fuer die
Dispo (139 Zeilen mit Befund, Spalte fuer den Entscheid): `docs/shopfloor_paket_2026-11-17/auswertung_ingo/Rueckstandslisten_Auffaellige_Eintraege_2026-11-17.xlsx`
(auch in Ingos Downloads). Nichts geloescht oder geaendert; Bereinigung erst nach Entscheid der Dispo.

**Nicht gebaut, und warum:** Listenanbindung SAP (VF- und WE-Termin: T76-Befund unten, Rueckfrage an Anh Nhi noetig), TX-Mengen und FPY TR5/TX (MES-DB-User),
Schichten (Entscheide Fabio: Schichtmodell je Tag, Datum der Nachtschicht, Rundung), FPY-TX-Abgrenzung (Aggregation von „Avg Asictx“
gegen Pass/Anzahl erst mit Zugriff auf die Reportdefinition).

**T76 nur gelesen (Agent sap-t76, 17.11. 14:52, `saptasks/shopfloor_sap/RUECKSTAND_TERMINE_T76_2026-11-17.md`; nichts in SAP geaendert,
P76 nicht gelesen, ZC22 nicht gestartet, weil die Form `KAPA_BEDARFE` in `ZCKAPA` schreiben kann):**
- **„VF-Termin“ und „WE-Termin“ gibt es in ZC22 nicht.** Die Kurztexte stehen nur im Zusatzpaket GIB: `/GIB/DC_DATE_AV`
  „Verfuegbarkeits-Termin laut MD04/MD05“ (VF-Termin) und `/GIB/DC_DATE_GR` „Wareneingangs-Termin laut MD04/MD05“ (WE-Termin). VF heisst
  also **Verfuegbarkeit**, nicht Liefertermin. *Die am 17.11. deployte Beschriftung „Bestaetigter Liefertermin (VF)“ (Vorschlag Fabio)
  passt damit vermutlich nicht; Entscheid Ingo/Fabio, bis dahin unveraendert.* Ob die Dispo GIB benutzt und wie GIB fuellt, ist offen.
- ZC22 „Wunschtermin“/„Termin bestaetigt“ kommen meist aus `VBBE-MBDAT` (739 von 741 offenen FA mit Kundenauftrag), sonst `VBEP-EDATU`;
  es zaehlt der letzte Satz ohne `ORDER BY`, `BMENG` wird nicht geprueft. Der Config-Vorschlag „VBEP-EDATU 1. Einteilung“ trifft meist die
  unbestaetigte Wunscheinteilung.
- ZC22 „Eckendtermin“ = `AFKO-GLTRP` bzw. `PLAF-PEDTR`. MD04 fuehrt den Zugang auf Eckendtermin + WE-Bearbeitungszeit (FA 1241683:
  24.04. + WEBAZ 2 = 28.04.). Vermutung: GIB-WE-Termin = Eckendtermin, GIB-VF-Termin = Eckendtermin + WEBAZ.
- Verknuepfung nur ueber `KDAUF/KDPOS` (nie Einteilung); 1'338 von 2'079 offenen FA haben keinen Kundenauftrag. Material = `AFPO-MATNR`
  bzw. `PLAF-MATNR`; VkNr ist eine Materialnummer (`ZSTR_ZLO_TOPDOWN-VKNR` mit Datenelement MATNR).
- Kein vorhandenes OData-Set liefert `VBBE-MBDAT`, Planauftraege mit Kundenauftrag oder Termine mit WEBAZ; dafuer waere ein Set-Ausbau
  in T76 noetig (Plan zuerst).
- **Offen:** Anh Nhi (ueber Fabio) mit Bildschirmfoto einer Rueckstandszeile fragen, welches Feld sie meint (GIB `/GIB/DCO`, MD04 oder
  Kundenauftrag; fuer 409313/10 drei Kandidaten: `MBDAT` 22.04./06.05., `EDATU` 24.04./08.05., Eckende + WEBAZ 28.04.); ob „Datum WE
  geplant“ den FA der Zeile oder das Fehlteil meint; welcher Auftrag gilt bei mehreren FA/Planauftraegen je Position; P76-Bestaetigung
  (Freigabe Ingo).

*Nachtrag 17.11. ~15:15:* Antwort an Fabio 17.11. gesendet (laut Ingo; Text im Chat von Claude entworfen), mit der Dispo-Liste im Anhang: Stand je Punkt; gefragt AE 12.11., Feiertage, Regel offen nach Status oder Abschlussdatum, Entscheid der Dispo je Zeile, Bildschirmfoto Anh Nhi zum VF-Termin und Bedeutung „Datum WE geplant“, Schichtmodell/Nachtschicht/Rundung TX, Termin MES-DB-User; angeboten Vergleich mit ZC22 am 18.11.
## 26. Entscheide Fabio 17.11. (`Shopfloor_Entscheide_2026-11-17.zip`): Messungen und Plan (Stand 2026-11-17 abends)

Anlass: Mail Fabio 17.11. ~16:30 mit Paket `Shopfloor_Entscheide_2026-11-17.zip` (Antwort auf Ingos Mail 17.11. 15:17). Abgelegt
unter `docs/shopfloor_paket_2026-11-17/2026-11-17_entscheide/` (Entscheide als md, Mailtext, zwei Summen-CSV). **Nicht im Repo**,
weil mit Kunden- oder Auftragsdaten: `PPA-Shop-Floor-File-2026_Version 1.04 (2).xlsx`, `CE11000_AE_1211_Pruefung_1711.txt`,
`AE_Differenz_Belege_12-11.csv`, die SAP-Bilder zu 58613 und `KE30_17-11_November.png`; sie liegen im Original-Zip in Ingos Downloads.
Auftrag Ingo „alles machen was im Text und Paket steht“; Plan am 17.11. ~16:45 freigegeben: **Paket A** sofort (Doku, Messungen,
P76 nur lesend), **Paket B** (Bau, Deploy) erst nach dem ZC22-Abgleich vom 18.11. frueh, **Paket C** wartet auf den MES-User.
*Damit ueberholt in Abschnitt 25:* die Fragen an Fabio zu AE 12.11., Feiertagen, Regel offen, Nachtschicht und Rundung (beantwortet),
die Bitte um Dispo-Entscheide zu den 139 Eintraegen (Fabio: jetzt nicht, Abgleich Ende Jahr und vor Go-live) und die Annahme
„nur 01./02.01. sind Feiertage“ (nicht freigegeben).

### 26.1 Entscheide Fabio (verbindlich laut Paket)

| Thema | Entscheid |
| --- | --- |
| Offen / abgeschlossen | Einheitlich ueber den **Abschlussstatus**, fuer Rueckstandsliste **und** PPD: erledigte und stornierte Eintraege zaehlen nicht offen. Widersprueche zwischen Status und Abschlussdatum bleiben gekennzeichnet, bis sie bereinigt sind; nicht automatisch aufloesen. |
| Rueckstandslisten | Fabios aktuelle Excel-Datei (`PPA-Shop-Floor-File-2026`, Version 1.04) bleibt bis zum Go-live **01.01.2027** fuehrend und darf periodisch eingelesen werden; Haeufigkeit offen. Dispo-Abgleich Ende Jahr und vor Go-live, zuerst gegen das aktuelle Excel. Die Kopfwerte im Excel sind gefilterte Zwischensummen (SUBTOTAL). |
| TX-Schichtmodell | Monatlich waehlbar, wochen- oder tageweise von Hand anpassbar (Vorschlag Paket: Tag vor Woche vor Monat, fruehere Einstellungen bleiben). Nachtschicht 23:00-06:00 zaehlt zum Starttag und ist erst am Folgetag 06:00 vollstaendig. Halbe Stueck aus der Aufteilung 14-15 Uhr **nicht runden** (12.11.: 549,5 + 755,5 = 1'305). |
| Arbeitskalender | Echter Trafag-Arbeitskalender mit Feiertagen und Betriebsschliessungen; die Annahme nur 01./02.01. ist nicht freigegeben. |
| FPY TX | Michaels Reportauswahl EAP1_1 bis EAP3_3 ohne EAP1_9 und EAP2_6, Referenzwert „Avg Asictx“; Nachbau der Reportberechnung ist unsere Aufgabe, kein weiterer Entscheid Fabio. MES-Zugang (Marco Di Menco): kein Termin. |
| VF-/WE-Termin | Fabios SAP-Beispiel Material 58613, Auftrag 419371/10: Excel-Wunschtermin = MD04-Bedarfstermin 27.11., bestaetigte 50 Stk. mit Bereitstellung 04.12. und Lieferung 08.12.; zwei Planauftraege mit Auftragsendtermin 25.11./30.11. gegen MD04-Listendatum 27.11./02.12. Die Termine nicht gleichsetzen, bei mehreren Plan-/Fertigungsauftraegen alle mit Menge und Termin behalten. Wir melden das Mapping zurueck; Beschriftung „Bestaetigter Liefertermin (VF)“ nicht abschliessend. **Entscheid Ingo 17.11.: Beschriftung zurueck auf „Wunschtermin“** (mit Paket B). |
| ZC22-Abgleich | 18.11. frueh mit vollstaendigem Layout `/ P00 TX`; wir liefern die Cockpit-Werte mit Stichtag und Selektion. |

### 26.2 AE TX Stichtag 12.11.: Board = SAP

Produktiv gelesen (`/shopfloor/api/daily?module=verkauf`, 17.11. 16:40): Board-Tag 13.11. (Konvention: Eintrag vom Tag D = Stand
HZDAT bis D-1) zeigt `ae_aktuell` 3'376'806.32, `ae_tx` **1'151'205.88**, `ae_dw` 1'381'146.94, geschrieben am 17.11. 12:28 (CO-PA
nachgeladen). Das sind genau Fabios SAP-Werte aus CE11000; die alte Kontroll-CSV (1'147'421.02) war ein frueherer Stand. Nichts zu
korrigieren. Board-Tag 12.11. (= HZDAT bis 11.11.) stimmt ebenfalls mit der alten CSV (3'077'914.12 / 954'314.90 / 1'308'719.11).

### 26.3 Rueckstandslisten: aktuelles Excel gegen das Board (nur gemessen)

Board produktiv gelesen (`/shopfloor/api/records?module=rs_*`, 17.11. ~16:55), Excel = Fabios Version 1.04, nur Zeilen mit Eintrag
(Datum des Eintrags, VkNr oder Menge; das Excel hat vorbelegte leere Nummern bis -999 bzw. -1989). Status „erledigt“ in allen
Schreibweisen, „storniert“, „Abgesagt“ zaehlen als abgeschlossen. Skript `scratchpad/rs_vergleich.py` (nicht im Repo).

| Liste | Eintraege Excel | Board | neu nur im Excel | nur im Board | Status geaendert | Abschlussdatum geaendert |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| SEH | 4 | 4 | 0 | 0 | 0 | 0 |
| TR5 | 225 | 204 | 21 | 0 | 0 | 2 |
| TX | 327 | 296 | 31 | 0 | 46 | 47 |
| DW | 108 | 92 | 16 | 0 | 2 | 6 |

Offen (Positionen / Stk. Menge im Rueckstand), Regel nach Abschlussdatum (heute in PPD, Patch 06) gegen Regel nach Status (Entscheid Fabio):

| Liste | Excel nach Datum | Excel nach Status | Board nach Datum | Board nach Status |
| --- | ---: | ---: | ---: | ---: |
| SEH | 0 / 0 | 1 / 300 | 0 / 0 | 1 / 300 |
| TR5 | 21 / 3'906 | 55 / 5'052 | 2 / 300 | 34 / 1'146 |
| TX | 22 / 1'143 | 85 / 2'268 | 47 / 2'956 | 86 / 3'713 |
| DW | 9 / 112 | 71 / 1'048 | 8 / 84 | 53 / 823 |
| **Total** | **52 / 5'161** | **212 / 8'668** | 57 / 3'340 | 174 / 5'982 |

Befunde: (1) Die Kopfwerte „Offen“ im Excel (TR5 3'906, TX 1'143, DW 112) sind genau die Summe nach Abschlussdatum. (2) **160
Excel-Eintraege haben ein Abschlussdatum, aber keinen abgeschlossenen Status** (FehlMat 81, in Arbeit 52, Eingeplant 20, leer 5,
In Prod 1, „aktuell gesperrt“ 1). Mit der Statusregel zaehlen sie offen: PPD stiege nach heutigem Excel von 5'161 auf 8'668 Stk. Das
ist vor dem Umschalten mit Fabio zu klaeren (Status nachfuehren oder Widerspruch kennzeichnen und trotzdem offen zaehlen). (3) Seit
dem 05.10. wurde im Excel weitergearbeitet: 68 neue Eintraege, bei TX 46 Statusaenderungen. Das Board ist entsprechend veraltet.

### 26.4 Arbeitskalender

Fabios Blatt „Kalender“ enthaelt nur die Tage des Jahres (KW, Wochentag, Datum, „x“ fuer Leerfelder), **keine** Feiertage oder
Betriebsschliessungen. Als Quelle bleibt der SAP-Fabrikkalender von Werk 1100 (Ergebnis unten, 17.11. in P76 gelesen). Heute rechnen
`ShopfloorSapVerkauf.LetzterArbeitstag` (Mo-Fr ohne 01./02.01.) und `ShopfloorSapVorrat.Plan5Ende` (Mo-Fr ohne Feiertage);
`KpiLiefertreue` (Mo-Fr, so abgenommen) bleibt unveraendert.

**Fabrikkalender, P76 gelesen 17.11. 16:47-16:57** (SAP-Agent, eigene Zusatzsitzung an Ingos GUI, nur SE16/MD04 anzeigen,
Freigabe Ingo 17.11.): `T001W-FABKL` Werk 1100 = `CH`; `TFACD` CH: Mo-Fr Arbeitstage, Feiertagskalender `CH` (1996-2040);
`TFACS` 2026: 248 Arbeitstage, 2027: 254. Mo-Fr ohne Arbeitstag 2026: 01.01., 02.01., 03.04., 06.04., 01.05., 14.05., 25.05.,
24.12., 25.12., **28.-30.12. (Sonderregel `TFAIN`, kein Feiertag, vermutlich Betriebsschliessung)**, 31.12.; 2027: 01.01., 26.03.,
29.03., 06.05., 17.05., 24.12., 31.12. Kein Wochenendtag ist Arbeitstag. **T76 weicht im Dezember 2026 ab** (Testregeln 10./11.12.
und 16.12., 245 Arbeitstage); massgebend ist P76. Vorschlag: Kalender `CH` aus P76 als fuehrende Quelle (Fabios Blatt hat keine
Feiertage); Bestaetigung Fabio/Ingo steht aus.

### 26.6 VF-/WE-Termin: Fabios Beispiel 58613 / 419371/10 in P76 gelesen

Gleiche Lesung wie 26.4 (Bericht im Scratchpad, ohne Kundennamen). Alle sieben Bildschirmwerte Fabios bestaetigt:

| Fabios Wert | P76-Feld | Wert |
| --- | --- | --- |
| MD04-Bedarfstermin / -menge | `VBBE-MBDAT` / `OMENG` der unbestaetigten Einteilung 1 (MD04 `MDPS-DAT00`) | 27.11.2026 / 50 |
| Einteilung 1: best. Menge / Bereitstellung / Lieferung | `VBEP-BMENG` / `MBDAT` / `EDATU` | 0 / 27.11. / 01.12.2026 |
| Einteilung 2: best. Menge / Bereitstellung / Lieferung | `VBEP-BMENG` / `MBDAT` / `EDATU` | 50 / 04.12. / 08.12.2026 |
| Planauftrag 2526829: Endtermin / Eckstart / WEBAZ / Menge | `PLAF-PEDTR` / `PSTTR` / `WEBAZ` / `GSMNG` | 25.11. / 18.11. / 2 / 45 |
| Planauftrag 2534647: Endtermin / Eckstart / WEBAZ / Menge | `PLAF-PEDTR` / `PSTTR` / `WEBAZ` / `GSMNG` | 30.11. / 23.11. / 2 / 5 |
| MD04-Listendatum der Planauftraege | `MDEZ-DAT00` = `PEDTR` + `WEBAZ` (Verfuegbarkeit) | 27.11. / 02.12.2026 |

Planauftraege haengen nur ueber `PLAF-KDAUF`/`KDPOS` am Kundenauftrag; keinen offenen Fertigungsauftrag (MD04; `AFPO` nicht gelesen).
Nach der ZC22-Regel aus dem T76-Quelltext nachgerechnet (Liste nicht abgelesen): Wunschtermin 27.11., Termin bestaetigt 04.12., beides
Bereitstellungsdaten (`MBDAT`); der bestaetigte Liefertermin ist `VBEP-EDATU` 08.12. **Excel-Eckendtermin 27.11. = MD04-Verfuegbarkeit
des ersten Planauftrags (`PEDTR` 25.11. + 2), nicht `PEDTR`.** Belegt ist `PEDTR` + 2 Tage; dass `WEBAZ` in Arbeitstagen des
Kalenders CH zaehlt, zeigt das Beispiel nicht (kein Wochenende dazwischen). GIB nicht gelesen.

**Mapping-Vorschlag an Fabio** (nicht gleichsetzen, je Termin eine eigene Spalte): Wunschtermin = `VBBE-MBDAT` der ersten offenen
Einteilung (wie ZC22 und Excel); bestaetigte Bereitstellung = `VBEP-MBDAT` der Einteilung mit `BMENG` > 0, bestaetigter Liefertermin
= deren `VBEP-EDATU`; je Plan- bzw. Fertigungsauftrag eine Zeile mit Menge, Endtermin (`PLAF-PEDTR` bzw. `AFKO-GLTRP`) und
Verfuegbarkeit (Endtermin + `WEBAZ`, = MD04 und Excel-Eckendtermin). „VF-Termin“ (Anh Nhi: letzter bestaetigter Liefertermin) waere
danach `VBEP-EDATU`; das widerspricht dem Excel-Wert und muss Fabio mit Anh Nhi entscheiden.

### 26.5 Plan Paket B (nach dem ZC22-Abgleich 18.11.) und Paket C

Paket B, eigener Worktree ab dem Produktivstand `32bf356`: Offen-Regel nach Abschlussstatus fuer Liste und PPD (`app.js`
`syncTvFromRs`), „storniert“/„Abgesagt“ in die abgeschlossenen Status, Widersprueche kennzeichnen; Import der Rueckstandslisten aus
Fabios Excel als Knopf mit Vorschau der Aenderungen und Sicherung vorher, nichts loeschen; Arbeitskalender nach Entscheid der Quelle in
`LetzterArbeitstag` und `Plan5Ende`; Beschriftung zurueck auf „Wunschtermin“; Bemerkungen „Offen (Fabio)“ der TX-Schichtfelder in
`shopfloor_config.json` auf die Entscheide nachfuehren. Vor dem Deploy das abgebrochene Einkauf-Delta nachholen. Paket C (wartet auf
MES-User): TX-Mengen mit Schichtmodell, FPY TR5/TX, Nachbau „Avg Asictx“.

### 26.7 Stichtag 18.11. (erster mit ganzem Layout): Cockpit-Werte fuer Fabios ZC22-Abgleich

Lauf 18.11. 04:01 ok (`sap/status`), Werte im Board vom SAP-Lauf 04:01/04:02 (produktiv gelesen 07:35). Selektion: ShopAufSet Typ VR
(ZC22-Logik), Layoutfilter Status `FREI DRUC RÜCK VOKL*` ausgeschlossen und Arbeitsplatz `M*` ausgeschlossen (wie `/ P00 TX`),
Material 1-99999 fuer TR5 und DW, DW ueber `D*`; Fenster ab 18.11. kumuliert inkl. Ueberfaellige: SEH/TR5/TX bis 18.12. / 18.01. /
18.02., DW bis 16.12. / 13.01. / 10.02. (4/8/12 Wochen), offene KW bis 22.11., SEH-Tagesziel bis 18.11., TX plan5 bis 25.11.
(5 Arbeitstage Mo-Fr); Verzug Eckendtermin 01.01.2026 bis 17.11.2026.

| Abteilung | Vorrat total | 3 Monate | 2 Monate | 1 Monat | offene KW | weiteres | Verzug (Stk.) |
| --- | ---: | ---: | ---: | ---: | ---: | --- | ---: |
| SEH | 160'672 | 85'868 | 56'409 | 41'804 | 6'451 | Tagesziel 2'082 | 155 |
| TR5 | 59'992 | 32'916 | 24'264 | 17'177 | 1'400 | | 339 |
| TX | 52'869 | 35'769 | 26'530 | 20'201 | 2'306 | plan5 5'488 | 65 |
| DW | 48'224 | 33'537 (12 W.) | 23'778 (8 W.) | 15'794 (4 W.) | 1'535 | | 264 |

Eine Vorschau um 07:40 (nur gelesen) lag schon leicht anders (z. B. TX Verzug 35, SEH Vorrat 159'118), weil SAP weiterlaeuft;
fuer den Abgleich ZC22 und Cockpit im selben Moment ziehen oder ZC22 gegen den Stand 04:00 halten.

## 27. Paket Fabio 19.11. (`Shopfloor_Paket_Ingo_2026-11-19.zip`): Messungen und Plan (Stand 2026-11-19 08:45)

Anlass: Mail Fabio 19.11. 08:17 (an Ingo) mit Paket: Entscheide 17.11., Abgleich Board/Excel 16./17.11., SAP-Beispiel 58613, neuer
Wunsch „Naechster Vorgang“ im Forecast der Dichtesensoren (Sergio) mit Frontend-Patch 09. Fabio will **je Punkt eine Rueckmeldung**
(erledigt nach Paketuebernahme, noch abweichend mit Ursache, noch nicht umgesetzt). Abgelegt unter `docs/shopfloor_paket_2026-11-19/`
(md, Mailtext, Patch 09, Kontrollsummen, Board-Bilder). **Nicht im Repo** (Kunden- oder Belegdaten, Original in Ingos Downloads):
`PPA-Shop-Floor-File-2026_Version 1.04 (2).xlsx`, `CE11000_AE_1211_Pruefung_1711.txt`, `AE_Differenz_Belege_12-11.csv`,
`Rueckstandslisten_Auffaellige_Eintraege_2026-11-17.xlsx`, SAP-Bilder 58613, KE30, MD04-Bilder Sergio. Auftrag Ingo „kannst du das paket
abarbeiten“: Schritt 1 nur lesen und messen, Bau erst nach Go. Das Paket fasst die Teile vom 17.11. (Abschnitt 26) und den
Board/Excel-Abgleich vom 18.11. zusammen; die Entscheide sind dieselben wie in 26.1.

### 27.1 Versionsstand

Produktiv seit 18.11. 17:57 `1417a3a`. **Shopfloor-Stand unveraendert seit dem Deploy 17.11. 14:36 (`32bf356`)**; danach kein
Shopfloor-Code mehr. Die produktive `wwwroot/shopfloor/app.js` ist bitgleich mit dem Repo (SHA256 `32d9567c…`) und enthaelt Patch 08.
Fabios Board-Werte vom 16./17.11. sind damit kein Vorstand eines noch fehlenden Pakets: Seit dem 17.11. ist nichts Neues fuer den
Shopfloor eingespielt. Paket B (Abschnitt 26.5) ist nicht gebaut.

### 27.2 Board-Werte produktiv gelesen (19.11. 08:35, nur GET `api/daily`, `api/sap/status`)

Fabios Board-Zahlen vom 17.11. stimmen genau mit dem gespeicherten Board ueberein (z. B. TX Vorrat 52'214, offene KW 4'675, Verkauf
AE 4'072'454.89). Die Vorratswerte des Tags D sind der SAP-Stichtag D 04:01; die Excel-Werte tragen die Teamleiter tagsueber ein.

| Abteilung | Vorrat 17.11. / 18.11. / 19.11. | offene KW 17. / 18. / 19.11. | Verzug PPD 17. / 18. / 19.11. |
| --- | --- | --- | --- |
| SEH | 157'889 / 160'672 / 159'666 | 8'257 / 6'451 / 4'656 | 80 / 155 / 662 |
| TR5 | 60'986 / 59'992 / 60'381 | 2'468 / 1'400 / 666 | 563 / 339 / 20 |
| TX | 52'214 / 52'869 / 51'306 | 4'675 / 2'306 / 1'205 | 545 / 65 / 91 |
| DW | 49'101 / 48'224 / 47'949 | 2'511 / 1'535 / 1'230 | 336 / 264 / 667 |

Auffaellig: (1) Die PPD-Zeile vom 17.11. wurde um 16:36 vom Benutzer `fpa` gespeichert (nicht vom SAP-Lauf); die Verzugswerte des
17.11. sind damit Handwerte oder von Hand bestaetigt. (2) Der Verzug springt am 18./19.11. stark (SEH 155 -> 662, TR5 339 -> 20,
DW 264 -> 667); Ursache nicht geprueft. (3) Die Verkaufszeile vom 17.11. wurde am 18.11. 08:13 von `fpa` gespeichert.

### 27.3 Befunde je Punkt (Ursache, soweit ohne P76 belegbar)

| Punkt Fabio | Befund | Stand |
| --- | --- | --- |
| Vorrat TX +765 in allen Fenstern | gleiche Differenz in total, 3, 2 und 1 Monat: die Mehrmenge liegt in Auftraegen mit Eckendtermin bis 1 Monat (inkl. ueberfaellig). Board = Stichtag 04:01, Excel = Ablesung der Teamleiter; ein Auftragsvergleich ist nicht moeglich, weil das Board nur Summen speichert, keine Auftragsliste | abweichend, Ursache offen |
| Offene KW TX/DW | gleiche Lage wie Vorrat | abweichend, Ursache offen |
| Tagesziele TR5/TX/DW | `ziel` ist bei TR5, TX und DW ein **Handfeld** ohne Quelle (nur Excel-Import 05.10.); die Abweichung `abw` verlangt nur `prod` (`need: ["prod"]`), ein fehlendes Ziel zaehlt als 0 (TR5 17.11.: 814 statt 14) | noch nicht umgesetzt; Fix klein |
| SEH Produktion 16.11. 2'699 statt 3'024 | Board = WE-Menge der Fertigungsauftraege mit Istendtermin 16.11. (`ShopProdSet`, 56 Auftraege, Stichtag 16.11. 17:07); Excel-Quelle nicht bekannt | abweichend, Ursache offen (P76) |
| PPD-Verzug | Board SAP-Lauf (ShopAufSet Typ VZ); 17.11. von Hand gespeichert (siehe oben) | abweichend, Ursache offen |
| Terminverschiebungen leer | `tv_*` stehen in keiner PPD-Zeile; sie kaemen aus den Rueckstandslisten (Patch 06), die seit dem Import vom 05.10. nicht nachgefuehrt sind | noch nicht umgesetzt (Paket B: Offen-Regel und Excel-Import) |
| Rueckstandslisten | Excel bleibt bis 01.01.2027 fuehrend, periodischer Import erlaubt (26.1, 26.3) | noch nicht umgesetzt (Paket B) |
| Termine VF/WE 58613 | in P76 am 17.11. gelesen, Mapping-Vorschlag in 26.6 | Rueckmeldung an Fabio faellig |
| TX-Mengen, Schichtmodell | braucht MES-DB-User (Marco), Regeln in 26.1 | noch nicht umgesetzt (Paket C) |
| Arbeitskalender | Kalender CH aus P76 gelesen (26.4), noch nicht eingebaut | noch nicht umgesetzt (Paket B) |
| FPY TX/TR5 | MES-DB-User fehlt | noch nicht umgesetzt (Paket C) |
| Verkauf 17.11. | Board AE 4'072'454.89 gegen Excel 4'081'847.40 (-9'392.51), Umsatz 3'514'314.84 gegen 3'524'723.04 (-10'408.20). Board-Tag 17.11. = HZDAT bis 16.11., gelesen 17.11. 04:01; vermutlich wie am 12.11. Belege mit HZDAT bis 16.11., die erst nach 04:01 gebucht wurden (nicht belegt, braucht CE11000 aus P76) | abweichend, Ursache vermutet |
| Naechster Vorgang (neu) | Patch 09 gegen eine Kopie der produktiven `app.js` geprueft: Stelle genau 1x, Syntax ok, ohne Feld unveraendert. Backend: `OrderRows` (`ShopfloorSapSync.cs`) und `OrderFields` (`ShopfloorForecast.cs`) reichen kein Zusatzfeld durch; `ShopAufSet` liefert den Wert nicht. SAP-Seite (`ZXM61U01`) in T76 in Abklaerung | noch nicht umgesetzt |

### 27.4 Gebaut und deployed 2026-11-19 09:03 (`591c96c`, Zweig `shopfloor-paket-1911`, Funktionscommit `d8217cb`)

Auftrag Ingo: Tagesziel-Fix, Paket B Rueckstandslisten, Auftragsliste je Stichtag, Naechster Vorgang; P76 nur lesen. Release-Worktree
**1'652 von 1'652** Tests gruen, Deploy mit dem Werkzeug, `app.js` produktiv bitgleich, Patch 09 drin, keine Sicherung (Wochensicherung
16.11.), kein Einkauf-Delta (Lauf 18.11. 19:29 fertig). Alarme nur `-wal`/`-shm` (alle wieder da).

- **Tagesziel:** `abw` braucht `prod` **und** `ziel` (SEH, TR5, TX, DW); ohne Ziel bleibt die Abweichung leer statt der ganzen Tagesmenge.
- **Offen-Regel nach Status** in Liste und PPD (`IsClosed`, `syncTvFromRs`): erledigt, storniert, abgesagt = geschlossen; Abschlussdatum bei offenem
  Status = offen mit Warnzeichen (155 Widersprueche nach Fabios Excel), nichts wird automatisch aufgeloest. Terminverschiebungen kommen aus den Listen.
- **Excel-Import** (`ShopfloorExcelImport`, `POST api/import/excel?apply=0|1`, Knopf „Excel einlesen“): Vorschau, Sicherung der Datenbank vorher,
  nichts loeschen, leere Zelle leert nichts. Mit Fabios Datei gegen den Seed (nur Test): TR5 +21/2 geaendert, TX +31/48 (43 Status), DW +16/8, SEH 0; Tagesziele
  TR5 31, TX 31, DW 35 Tage. **In Produktion noch nicht ausgefuehrt** (Datei in Ingos Downloads, erst Vorschau).
- **Kalender CH** 2026/2027 nach P76 (248/254 Arbeitstage) in `Plan5Ende` und `LetzterArbeitstag`; Beschriftung wieder „Wunschtermin“.
- **Auftragsliste je Stichtag** (`vorrat_auftraege`, 60 Tage, erster Lauf des Tages): `GET api/sap/vorrat/auftraege(.csv?tag=)`; **heute leer**, weil der
  04:01-Lauf vor dem Deploy lief; erster Stand am 20.11. 04:01.
- **Naechster Vorgang, Variante 0 ohne SAP-Aenderung:** Der Abgleich liest Typ VR mit `Avo eq 'X'`; `naechster_vorgang` = „VORNR / ARBPL“ nur fuer Fertigungsauftraege;
  Patch 09 in `app.js`. **Befund T76** (`saptasks/shopfloor_sap/NAECHSTER_VORGANG_ZXM61U01_2026-11-19.md`): `ZXM61U01` ist der Exit des Planungslaufs;
  die MD04 fuellt die Spalte ueber BAdI `ZZ_MD_ADD_COL_EZPS` -> `Z_MD04_NEXT_OPERATION` (erster nicht fertig rueckgemeldete Vorgang, Steuerschluessel PP00 ausgenommen).
- **P76 nur gelesen** (`saptasks/shopfloor_sap/P76_URSACHEN_2026-11-19.md`, ueber die Dashboard-Vorschauen, keine P76-Sitzung): Verkauf 17.11. = Board stimmt mit SAP (HZDAT bis 16.11.,
  Board-Zeile aus dem Startlauf 17.11. 12:26); Excel +9'392 AE / +10'408 Umsatz, vermutlich KE30 im Lauf des 17.11. gezogen (nicht belegt); SEH-Produktion 16.11. bleibt 2'699
  (WE-Menge, 56 Auftraege), die 3'024 sind nicht erklaert; Verzugssprung 18./19.11. echt (SEH/DW Auftraege ueber die Stichtagsgrenze, TR5 5 Auftraege/275 Stk. jetzt im Layoutfilter).

**Nach dem Deploy zu pruefen / offen:** Excel-Import erst als Vorschau mit Fabios Datei, dann uebernehmen; 20.11. 04:01 Auftragsliste als CSV und Forecast DS mit Naechster Vorgang
gegen MD04; Tagesziele TR5/TX/DW kommen erst mit dem Import; Bereinigung der 155 Widersprueche (Fabio); Importhaeufigkeit offen; Quelle der Tagesziele (Excel-Import) ist Annahme, nicht bestaetigt.
