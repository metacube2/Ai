# Handbestellwesen: Abloesung der PHP-Anwendung durch ein Modul im Dashboard

Stand: 2026-11-18 08:20 (Abschnitt 6.13: Stammdaten neu erfassen fuer alle Besteller, Freigabeliste ueber CHF 1'000 mit Kostenstellen- und Projektleiter, deployed 08:15 `f2c0267`); davor 2026-11-17 nachmittags (Abschnitt 6.12: Antworten Ingo, Teil 2 deployed 12:36 `44a3ed1`, seit 12:43 im gemeinsamen Stand `2e62163`,
`Schreiben` an, noch nicht ausgefuehrt: ein Konto gehoert im AD einer anderen Person, Entscheid Ingo). Davor 2026-11-17 (Abschnitt 6.11: naechtlicher
AD-Abgleich, deployed 11:28 `6a7de50`, Nachtlauf nur Vorschau; erste Vorschau aus der Produktion; Fragen an Ingo in 6.11). Davor 2026-11-12 (Doku-Pruefung: Kopf an die Abschnitte 6.2 bis 6.10 angeglichen;
der juengste Inhalt war damals vom 2026-10-27).
Status: **produktiv.** Das Modul ist seit dem 22.10. 13:38 deployed, die Altdaten sind am 22.10. 14:27 importiert (6.4), die
PHP-Anwendung ist abgeschaltet und der Reiter seit dem 27.10. sichtbar (6.8). Offen: Zugriff und Grundtests der Anwender *(laut Ingo am 17.11. vom Einkauf, Sandro, abgenommen)*,
Bereinigung, Rechnungsbetrag und Mail beim Wareneingang, Bestaetigung der Regeln durch den fachlichen Owner (6.8, 7).

Davor 2026-10-22 (Nachtrag Antworten Ingo: Betrieb lokal bei Trafag, Benutzer ohne SAP-Konto, Plugin/KISS, AD,
Schreiben nach SAP). Status: **Phase 1 programmiert und getestet, nicht deployed** *(ueberholt, siehe Stand oben)* (Abschnitt 6). **Entscheid 22.10. (nach der Einkaufssitzung,
mitgeteilt von Ingo): Das Handbestellwesen lebt weiter, aber die Nutzung wird festgehalten** (wer, wann, wie viel),
Abschnitt 6.1. Altdaten-Import wartet auf den
MySQL-Dump (Tobias) *(ueberholt: Dump am 22.10. erhalten, 6.2, und importiert, 6.4)*.

## 1. Auftrag und Entscheide

Ingo am 22.10.: Die alte PHP-Handbestellsoftware, die ein Student geschrieben hat und die noch im Einsatz ist, soll
„auf den neusten Stand“ gebracht werden. Danach entschieden:

| Frage | Entscheid Ingo 2026-10-22 |
| --- | --- |
| Technik | Neu in C# wie das Dashboard, moderne Technik, SAP-Anbindung wo noetig |
| Aufbau | **Modul im Dashboard** (nicht eigene Anwendung) |
| Altdaten | **Uebernehmen**; Ingo besorgt einen `mysqldump` |
| Stammdaten | **Phase 1 lokal gepflegt, Phase 2 aus SAP** |
| Ziel | **Abloesen** auf neue Technik und sicher machen; Benutzer ueber AD; KISS; Plugin-Bauweise; Dashboard-Logik mitnutzen, dazu Schreiben in die eigene DB und nach SAP |
| Betrieb heute | **von Trafag selbst gehostet, lokal**; gilt wegen des PHP-Codes als unsicher (Ingo) |
| Benutzer | Mitarbeitende **ohne SAP-Konto**, „Kruecke“ fuer Kleinmengenbestellungen ausserhalb von SAP |

## 2. Ist-Befund der PHP-Anwendung

Quelle: `handbestellwesen/Trafag-Export/` (41 Dateien, etwa 25 PHP-Seiten, kein Framework, ISO-8859-1, jQuery 1.11).
**Der Ordner bleibt ungetrackt** (in `.gitignore`), weil er Datenbankpasswoerter im Klartext enthaelt.

### 2.1 Fachlicher Ablauf

1. Bestellnummer loesen: Kreditor, Text, Bestellwert CHF, Kurzzeichen des Bestellers, Kostenstelle, Sachkonto.
2. Nummer = `E` + Jahr (2-stellig) + Kostenstelle + laufende Nummer 4-stellig, Anzeige mit Leerzeichen (`E26 875 0012`).
   Zaehler je Jahr und Kostenstelle in `tab_bestellnummer`.
3. Liegt der Wert ueber der Bestellberechtigung des Bestellers (`kurzzeichen.bestellberechtiung`), waehlt er eine
   berechtigte Person derselben Kostenstelle; gespeichert als `kurzzeichen = "Besteller,Freigeber"`.
4. Offene Bestellungen: Freigabe erteilen, Loeschen (nur ohne Wareneingang, Kopie nach `delete_archive`),
   „Waren eingegangen“ verschiebt nach `archive`. Rechnungsbetrag erfassen (aeltere Variante, in der Liste auskommentiert).
5. Filter: Volltextsuche ueber offene oder archivierte Bestellungen.
6. Stammdaten: Kreditoren (`tab_kreditorenstamm`), Mitarbeiter mit Limit und Kostenstelle (`kurzzeichen`),
   Kostenstellen mit Leiter (`tab_kostenstelle`), Sachkonten (`tab_sachkonto`).
7. E-Mail an den Besteller beim Wareneingang ist im Code vorhanden, aber auskommentiert.

Tabellen laut Code: `tab_offenebestellungen`, `archive`, `delete_archive`, `tab_bestellnummer`, `tab_kreditorenstamm`,
`kurzzeichen`, `tab_kostenstelle`, `tab_sachkonto`. Spaltentypen sind nicht bekannt; dafuer braucht es den Dump.

### 2.2 Sicherheit und Betrieb

| | Befund |
| --- | --- |
| 🔴 | Datenbankpasswort fest im Code (41 Stellen in 28 Dateien) und im Backup-Skript; ein zweites Zugangspaar fuer eine fremde Datenbank „Vertragsnummernverwaltung“. **Nach der Abloesung beide Passwoerter aendern lassen.** |
| 🟠 | Die Pfade im Code (Konto `michae60`, Domain `handbestellwesen.guentensperger.co.uk`, Hoster-Kennung in `.htaccess`) stammen aus einem frueheren Betrieb bei einem externen Hoster. **Laut Ingo laeuft die Anwendung heute lokal bei Trafag** (Korrektur 2026-10-22; die erste Analyse hatte einen privaten Hoster angenommen). Ob beim alten Hoster noch eine Kopie mit Daten liegt, ist offen. |
| 🔴 | SQL-Injection in jeder Seite (Formularwerte direkt im SQL). |
| 🔴 | Freigabelimit wird nur im Browser geprueft; Server speichert jeden Betrag. Freigeben und Loeschen kann jeder. |
| 🟠 | Ein gemeinsames Basic-Auth-Login, keine Benutzer, keine Nachvollziehbarkeit. |
| 🟠 | `phpinfo.php` offen; Datenbank-Backups unter dem Webverzeichnis. Stored XSS, kein CSRF-Schutz. |

### 2.3 Fachliche Fehler

- Nummernvergabe mit `LIKE 'E26<Kostenstelle>%'`: Kostenstelle `87` trifft auch `875`; zwei gleichzeitige Bestellungen
  koennen dieselbe Nummer bekommen.
- Datum als englischer Text („Monday 05 January 2015 …“), „000“ als Platzhalter fuer „noch nicht“; nicht sortier- oder
  auswertbar.
- `autoarchivebydate.php` fest auf `E21%`.
- Unter PHP 8 bricht `mysqli_affected_rows()` ohne Verbindung (zwei Stellen, Archivieren mit Rechnungsbetrag).

## 3. Zielbild

Modul im Dashboard nach dem Muster **Reiter Operations (Shopfloor)**, `docs/SHOPFLOOR_2026-10-07.md`: C#-Port,
eigene SQLite-Datenbank, Zugriff ueber Windows-Konto.

| Baustein | Ziel |
| --- | --- |
| Oberflaeche | Blazor Server + MudBlazor, eigener Menuepunkt „Handbestellwesen“ (Einkauf) |
| Daten | eigene Datenbank `handbestellwesen\handbestellwesen.db`, eigener `DbContext`, EF Core; nicht in `trafag_exporter.db` |
| Anmeldung | Windows-Konto (IIS). Rollen: Besteller, Freigeber (je Kostenstelle mit Limit), Wareneingang, Admin Stammdaten. Muss ohne Dashboard-Zugang fuer alle Besteller funktionieren (vgl. `Shopfloor:OpenForAll`) |
| Nummernvergabe | Zaehler je (Jahr, Kostenstelle) mit Eindeutigkeitsregel und Transaktion; Format bleibt `E` + JJ + KoSt + NNNN |
| Freigabe | Limitpruefung auf dem Server; Freigeber muss fuer die Kostenstelle berechtigt sein und ist der angemeldete Benutzer |
| Status | Aufzaehlung offen, freigegeben, Ware eingegangen, Rechnung erfasst, geloescht; echte Datums-/Zeitspalten; Archiv ist ein Status, kein eigener Datensatz |
| Protokoll | wer hat wann angelegt, freigegeben, Wareneingang gebucht, geloescht |
| Mail | optional, wie im Original vorgesehen: Besteller bei Wareneingang benachrichtigen (Entscheid offen) |

### 3.0 Bauweise: Plugin, KISS

- Ein in sich geschlossenes Modul: eigener Ordner (`Modules/Handbestellwesen/` mit Seiten, Diensten, Datenmodell), eigene
  Datenbank, **eine** Registrierung in `Program.cs` (`AddHandbestellwesen()`), ein Schalter `Handbestellwesen:Enabled` in
  `appsettings.json`. Aus heisst: kein Menuepunkt, keine Route, keine Tabellen. So bleibt das Dashboard unberuehrt.
- Mitgenutzt wird, was es schon gibt: Windows-Anmeldung und AD-Gruppen, Layout/Menue, `SapGatewayService` fuer SAP-Lesen,
  Sicherung der Datenbank wie bei Shopfloor, Deploy-Weg.
- Keine eigenen Rahmenwerke, keine Microservices, keine Warteschlangen. Vier Tabellen fachlich (Bestellung, Zaehler,
  Stammdaten, Protokoll), wenige Seiten.

### 3.1 Oberflaeche wie im PHP (Wunsch Ingo 2026-10-22)

„Das GUI soll wie im PHP sein, mit den Buttons und Farben; wenn es die Buttons nicht genau so gibt, aehnlich.“ Nur die
Oberflaeche wird nachgebaut, die Technik darunter ist neu. Vorlage `style.css` und `navigation.php`:

| Element | Original |
| --- | --- |
| Linke Leiste 200 px, Trafag-Logo oben, rechts duenne schwarze Linie | `#navall` |
| Grosse Kacheln 180 x 100 px mit Symbol und Text | „Bestellnummer loesen“ `#FE6C30` (orange), „Offene Bestellungen“ `#1B5FAA` (blau), „Archiv“ `#01A95C` (gruen), „Filter“ `#CCCC00` (gelb) |
| Kleine Kacheln 180 x 24 px | Kreditorenstamm, Mitarbeiterverzeichnis, Kostenstelle, Sachkonten, alle `#FFAE3B` |
| Symbole | `img/appbar.*.png` (Metro-Icons), werden uebernommen |
| Schrift | Verdana, Eingabefelder 500 px breit, 30 px hoch, 18 px Schrift |
| Listen | je Bestellung eine umrandete Tabelle mit zwei Kopfzeilen; Knopf „Waren eingegangen“ orange `#FE883D` mit Schatten |
| Meldungen | gruen „Berechtigt …“, rot „Nicht berechtigt …“ unter dem Kurzzeichen |

Die Seiten des Moduls bekommen dafuer ein eigenes Layout ohne MudBlazor-Optik; die Dashboard-Navigation bleibt aussen
herum. Die Bestellnummer erscheint nicht mehr als Browser-Alert, sondern als Hinweis auf der Seite.

## 4. Phasen

1. **Grundfunktion und Uebernahme.** Datenmodell aus dem Dump, Import offener Bestellungen, Archiv, Zaehler und
   Stammdaten (Umlaute aus ISO-8859-1, Datumstexte parsen, „000“ auf leer). Seiten: Bestellung anlegen, offene
   Bestellungen, Archiv/Suche, Stammdatenpflege. Abgleich Zaehlerstaende und Anzahl Datensaetze gegen den Dump.
2. **Stammdaten aus SAP.** Kreditoren, Kostenstellen, Sachkonten ueber den vorhandenen `SapGatewayService` lesen. Vorher
   pruefen, ob die Nummern der PHP-Anwendung (z. B. dreistellige Kostenstellen wie `875`) zu SAP passen. P76 nur auf
   Auftrag Ingo. Tabellen- und Feldnamen live pruefen, nicht aus Erinnerung.
3. **Schreiben nach SAP** (Wunsch Ingo; **zurueckgestellt, Entscheid Ingo 2026-10-22: vorerst nur in die eigene
   Datenbank schreiben**, gleiche Technik wie das Dashboard, EF Core/SQLite). Erst klaeren, *was* in SAP entstehen soll (Bestellanforderung, Bestellung oder
   nur eine Buchung) und **ob die Lizenz das erlaubt**: Die Benutzer haben bewusst kein SAP-Konto. Schreibt das Dashboard
   in ihrem Auftrag ueber einen technischen Benutzer, kann SAP das als indirekte Nutzung werten, die lizenzpflichtig ist.
   Vor dem Bau mit dem SAP-Lizenzverantwortlichen klaeren. Technisch braeuchte es einen schreibenden Gateway-Service in
   T76, Transport nach P76 durch Ingo.
4. **Abloesung.** Kurzer Parallelbetrieb, Stichtag, PHP-Anwendung abschalten, Datenbankpasswoerter aendern.

## 5. Offene Punkte

| Nr | Punkt | Wer |
| --- | --- | --- |
| 1 | ~~MySQL-Dump (Struktur und Daten).~~ **Erhalten am 22.10. (6.2), importiert am 22.10. 14:27 (6.4).** Urspruenglich: Im uebergebenen Ordner liegt keiner, nur das Skript `backup/dbBackup.sh`; es legt Sicherungen auf dem Server unter `…/admin/backup/<JJJJMMTT>/` ab. Dort nachsehen oder auf dem Server `mysqldump` ausfuehren (Passwort von Hand eingeben, nicht im Befehl) | erledigt |
| 2 | Fachlicher Owner und Freigabe vor Inbetriebnahme oder Ankuendigung (Einkauf? Finanz?) | Ingo |
| 3 | Wer bestellt, wer gibt frei, wer bucht Wareneingang; AD-Gruppen oder Kontenliste | Ingo / Owner |
| 4 | Werden Rechnungsbetrag und Mail beim Wareneingang noch gebraucht? | Owner |
| 5 | Datenbankpasswoerter nach der Abloesung aendern; pruefen, ob beim frueheren externen Hoster noch Daten liegen | Ingo |
| 6 | Was soll in SAP entstehen, und ist das lizenzrechtlich fuer Benutzer ohne SAP-Konto zulaessig? | Ingo / SAP-Lizenz |
| 7 | AD-Gruppen fuer Besteller, Freigeber, Wareneingang, Admin | Ingo / IT |
| 8 | ~~Abschalten oder weiterbetreiben?~~ **Entschieden 22.10.: weiterbetreiben, Nutzung festhalten** | erledigt |
| 9 | Benutzer informieren, dass Aufrufe und Bestellungen namentlich ausgewertet werden (Transparenz) | Ingo / Owner |

## 6. Umsetzung Phase 1 (2026-10-22; „nicht deployed“ ueberholt, deployed 22.10. 13:38, Reiter sichtbar seit 27.10.)

Auftrag Ingo: „Ich reiche die DB-Inhalte nach, mach schon mal den Code mit neuem Reiter.“

| Teil | Datei |
| --- | --- |
| Datenmodell (Bestellung, Zaehler, Kreditor, Mitarbeiter, Kostenstelle, Sachkonto, Protokoll) | `Modules/Handbestellwesen/HbModels.cs`, `HbDbContext.cs` |
| Fachlogik, alle Pruefungen auf dem Server | `Modules/Handbestellwesen/HbService.cs` |
| Registrierung, Datenbank anlegen, Sicherung alle 6 h (eine Datei je Tag, 60 Tage) | `Modules/Handbestellwesen/HbRegistration.cs` |
| Seite `/handbestellwesen/{bereich}` mit Kacheln wie im PHP | `Modules/Handbestellwesen/HandbestellwesenPage.razor`, `wwwroot/css/handbestellwesen.css` |
| Reiter „Handbestellwesen“ auf oberster Ebene (Sortierung 31, nach Einkauf) | `Services/DatabaseSeedService.cs` |
| Eine Zeile Registrierung | `Program.cs` |
| Konfiguration `Handbestellwesen` (`Enabled`, `DatabasePath`, `AdminUsers` = `koi`, `BackupKeepDays`) | `appsettings.json` |
| Tests (9) | `TrafagSalesExporter.Tests/HandbestellwesenServiceTests.cs` |

Datenbank: `<ContentRoot>\hbdaten\handbestellwesen.db` (WAL), Sicherungen unter `hbdaten\backup\`. Lokal in `.gitignore`.

Regeln, wie umgesetzt (zur Bestaetigung durch den fachlichen Owner):

- **Wer bin ich:** Windows-Konto -> Mitarbeitereintrag ueber das Feld `Konto`. Ohne Eintrag kann man nichts bestellen.
  Die Altdaten haben kein Konto; nach dem Import muss es ein Admin je Kurzzeichen eintragen. Admin sind die Konten in
  `Handbestellwesen:AdminUsers` und Mitarbeiter mit Haken „Admin“.
- **Besteller** ist der Angemeldete selbst; nur ein Admin darf ein anderes Kurzzeichen waehlen (im PHP konnte jeder fuer
  jeden bestellen).
- **Limit:** Liegt der Wert ueber der Berechtigung des Bestellers, muss eine Person derselben Kostenstelle mit genuegend
  Berechtigung gewaehlt werden (wie `checkberechtigung.php`), jetzt auf dem Server geprueft. Status „Freigabe offen“.
  *(Ueberholt am 2026-10-27, vermerkt am 2026-11-12: Freigabe wie PHP durch jede aktive Person mit genuegend Limit; die strenge
  Regel „nur dieselbe Kostenstelle“ gilt nur mit `Handbestellwesen:FreigabeNurGleicheKostenstelle` = `true`, Abschnitt 6.8.)*
- **Freigeben** darf nur die gewaehlte Person oder ein Admin. **Neu:** Wareneingang erst nach der Freigabe.
- **Wareneingang** buchen Besteller, Mitarbeiter mit Haken „Wareneingang“ oder Admin; die Bestellung geht ins Archiv.
- **Loeschen** nur ohne Wareneingang, durch Besteller oder Admin; geloescht wird nicht physisch, sondern mit Status.
- **Nummer:** Zaehler je (Jahr, Kostenstelle) in einer Transaktion, eindeutiger Index; 20 gleichzeitige Bestellungen
  ergeben im Test 20 verschiedene Nummern, `87` und `875` zaehlen getrennt.
- **Suche** findet die Bestellnummer auch mit Leerzeichen (im PHP musste man sie entfernen).
- **Rechnungsbetrag** ist als Feld vorhanden und wird angezeigt, aber noch nicht erfassbar (offener Punkt 4).

Tests: 9/9 Handbestellwesen, ganze Suite 1274/1274 gruen. **Nicht im Browser angesehen:** ein lokaler Start haette die
Hintergrunddienste (SAP-Abrufe, Exporte) mitgestartet. Ansehen nach Entscheid Ingo, siehe Abschnitt 7.

### 6.1 Nutzung festhalten (Entscheid 2026-10-22)

„Es lebt weiter, aber loggen, wer es braucht, wann, wie viel.“ Umgesetzt:

- **Aufrufe:** Wer das Handbestellwesen oeffnet, wird einmal je Konto und Tag im Protokoll festgehalten (Aktion
  `Aufruf`). So sieht man auch, wer nur schaut und nicht bestellt.
- **Bestellungen** tragen Besteller, Zeit und Betrag ohnehin; jede Aktion steht im Protokoll.
- **Kachel „Nutzung“** (nur Admin), Zeitraum waehlbar, Standard laufendes Jahr: Summe Bestellungen und CHF, Anzahl
  Personen; Tabellen je Person (Bestellungen, Summe, Aufrufe, erste/letzte Nutzung), je Kostenstelle, je Monat.
  Geloeschte Bestellungen zaehlen nicht.
- Test `Nutzung_Je_Person_Kostenstelle_Monat_Ohne_Geloeschte_Mit_Aufrufen`; Suite 1275/1275.

Hinweis: Die Auswertung ist namentlich. Benutzer sollten wissen, dass Aufrufe und Bestellungen ausgewertet werden
(offener Punkt 9).

### 6.2 Dump und neueste PHP-Dateien erhalten (2026-10-22 nachmittags)

Beides liegt ungetrackt in `handbestellwesen/` (`db.sql`, `public.zip` entpackt nach `public_2026-10-22/`).

- **Dump** (`db.sql`, HeidiSQL, MariaDB 12.3.2 unter Windows, Datenbank `trag_bestellwesen`, utf8mb4): alle 8 Tabellen.
  `archive` 11'044, `delete_archive` 340, `kurzzeichen` 109, `tab_bestellnummer` 392, `tab_kostenstelle` 55,
  `tab_kreditorenstamm` 4'111, `tab_offenebestellungen` 1'290 (teils seit 2022 offen), `tab_sachkonto` 62.
  Spalten wie im Code vermutet: Betraege und Datumsangaben als Text, `bestellberechtiung` als Ganzzahl, `kurzzeichen` der
  Bestellung als „Besteller,Freigeber“.
- **Neueste PHP-Dateien:** Betrieb lokal unter `C:/webapps/handbestellwesen`, Zugangsdaten jetzt zentral in
  `db_config.php` (neuer DB-Benutzer, MariaDB auf 127.0.0.1). Sonst nur kleine Aenderungen (z. B. `?? ""` fuer PHP 8).
  Unveraendert: SQL-Injection, `LIKE`-Nummernvergabe, `E21%`, viermal `mysqli_affected_rows()` ohne Verbindung,
  alte Passwoerter weiter in `backup/dbBackup.sh` und `autoarchive/deletevertrag.---`. Die Datei `.htaccess` heisst jetzt
  `htaccess` (ohne Punkt) und verweist auf den alten Hoster-Pfad: **der Basic-Auth-Schutz ist damit nicht aktiv**; ob der
  Webserver anders schuetzt, ist offen (Ingo).

### 6.3 Altdaten-Import (2026-10-22)

`Modules/Handbestellwesen/HbImport.cs`: `HbDumpParser` liest die INSERT-Befehle (HeidiSQL/mysqldump, MySQL-Escapes),
`HbImportService` rechnet um und schreibt nur in eine **leere** Datenbank, nur als Admin. Bedienung: Admin-Kachel
„Altdaten-Import“, Quelle `hbdaten\import\db.sql` auf dem Server, erst „Pruefen“ (schreibt nichts), dann „Importieren“.

Fehler der PHP-Anwendung werden beim Import korrigiert, nicht uebernommen (Wunsch Ingo):

| PHP-Fehler | Im Import |
| --- | --- |
| Platzhalter „Kreditor“/„Kostenstelle“/„Sachkonto“ gespeichert, wenn nichts gewaehlt wurde (erste `<option>` ohne value) | leer, Anzeige „ohne Kreditor“ |
| Text im Betragsfeld („KV“, „?“, „Service-Vertrag“) | Betrag 0, Text in neuer Spalte `WertText` erhalten und angezeigt |
| Datum als englischer Text, „000“/„---“ als „nicht gesetzt“ | echte Datumswerte bzw. leer |
| `kurzzeichen` = „Besteller,Freigeber“ | zwei Felder |
| Bestellnummern doppelt (Archiv, offen, geloescht) | eine Bestellung je Nummer, Archiv vor offen vor geloescht |
| Wareneingang gebucht, aber nie ins Archiv verschoben | Status „eingegangen“ |
| Zaehler `tab_bestellnummer` mit LIKE-Praefix | je Jahr und Kostenstelle, nie kleiner als die hoechste uebernommene Nummer |
| HTML-Entitaeten, doppelte Kreditor-Nummern/Kurzzeichen | aufgeloest, letzter Stand |

Windows-Konto der Mitarbeiter wird mit dem Kurzzeichen vorbelegt (Trafag-Konten = Kurzzeichen, z. B. `koi`); bitte
stichprobenweise pruefen.

Probelauf mit dem echten Dump in eine Temp-DB (3,6 s): 1'290 offen, 11'044 eingegangen, 322 geloescht, 4'092 Kreditoren,
109 Mitarbeiter, 55 Kostenstellen, 62 Sachkonten, 392 Zaehler; offene Bestellungen zusammen CHF 1,21 Mio. Hinweise: 383
Betraege als Text (KV 169x), 564 ohne Kreditor, 45 doppelte Nummern, 22 leere Datensaetze (Datum 01.01.2000), 50 offene
Bestellungen mit Besteller ohne Mitarbeitereintrag, **930 offene Bestellungen aelter als ein Jahr** (vermutlich nie als
eingegangen gebucht; Entscheid Ingo, ob nach dem Import gesammelt archivieren). Tests `HandbestellwesenImportTests` (6).

### 6.4 Import produktiv und Datenpruefung (2026-10-22 14:27)

Ingo hat um 14:27 importiert (Protokoll „Import Altdaten“, 12'629 Bestellungen). Pruefung auf einer Kopie der Modul-DB:

| Pruefung | Ergebnis |
| --- | --- |
| Bestand | 1'289 offen (CHF 1,21 Mio.), 11'018 eingegangen, 322 geloescht; 4'092 Kreditoren, 109 Mitarbeiter, 55 KoSt, 62 Sachkonten, 392 Zaehler |
| Doppelte Bestellnummern | **0** |
| Gleiche Bestellung zweimal (Besteller, Kreditor, Text, Wert, Tag) | 28 Paare, davon 25 innerhalb von 5 Minuten (Doppelklick im PHP, zwei Nummern) |
| Kreditoren mit gleichem Namen und verschiedener Nummer | 93 Namen |
| Mitarbeiter | keine doppelten Namen oder Konten; 97 von 109 ohne Kostenstelle, 61 mit Limit 0 |
| Offene Bestellungen mit unbekannter KoSt / Sachkonto / Besteller / ohne Kreditor | 13 / 13 / 50 / 61 |
| Nummer passt nicht zur Kostenstelle (nachtraeglich geaendert) | 234 |
| Zaehler kleiner als hoechste Nummer | 0 |
| Datum Eingang vor Anlage, Datum in der Zukunft | 0, 0 |
| Ausreisser | eine geloeschte Bestellung mit CHF 10'000'000'000'000 (zaehlt nirgends) |
| Offene nach Alter | < 1 Jahr 359, 1-3 Jahre 499, > 3 Jahre 431 |

Korrigiert: Die Pruef-Anzeige zaehlte eingegangene Bestellungen vor dem Doubletten-Filter (11'044 statt 11'018); die
Daten selbst waren richtig. Offen: Entscheid Ingo zu Doppelklick-Doubletten, alten offenen Bestellungen und Kreditor-Dubletten.

### 6.5 Bereinigung (Auftrag Ingo 2026-10-22: bereinige die Datensaetze)

Admin-Kachel „Bereinigung“, erst „Vorschau“, dann je ein Knopf. Nichts wird physisch geloescht, jede Aenderung steht im
Protokoll (`Bereinigung: ...`). `HbService.BereinigungVorschauAsync` / `BereinigenAsync`, Test
`Bereinigung_Storniert_Doppelklick_Und_Bucht_Alte_Offene_Aus`.

| Fall | Regel | Erwartet nach Datenpruefung 6.4 |
| --- | --- | --- |
| Doppelklick-Doubletten | gleiche Bestellung (Besteller, Kreditor, Text, Wert, Wert-Text) innerhalb von 5 Min zweimal gespeichert; behalten wird eingegangen vor offen, sonst die fruehere; die andere -> geloescht | 25 Paare (19 beide eingegangen, 3 beide offen, 3 gemischt) |
| Alte offene Bestellungen | angelegt vor dem Stichtag (Standard heute minus 1 Jahr), nie als eingegangen gebucht -> neuer Status **Ausgebucht**, erscheint im Archiv | rund 930 |

Nicht automatisch bereinigt, weil fachlich zu entscheiden: 93 Kreditor-Namen mit mehreren Nummern (in Phase 2 kommen
Kreditoren aus SAP), Mitarbeiter ohne Kostenstelle/Limit (Pflege durch Admin), 50 offene Bestellungen von Bestellern ohne
Mitarbeitereintrag (ehemalige Mitarbeitende; werden mit dem Stichtag meist ausgebucht), 3 inhaltlich gleiche Bestellungen mit
mehr als 5 Minuten Abstand (koennen echte Nachbestellungen sein).

Deploy: 2026-10-22 14:40 (37e5d7b), bitgleich, zehn Routen 200, Alarm nur WAL/SHM-Checkpoint beim Herunterfahren. Die Knoepfe drueckt Ingo; die Vorschau zeigt jede betroffene Nummer.

### 6.6 Moderne Oberflaeche `/bestellwesen` (Wunsch Ingo 2026-10-22)

„Design ueberarbeiten auf einer neuen Seite, moderne Komponenten, Geschaeftslogik gleich, evtl. besserer Workflow.“
`Modules/Handbestellwesen/BestellwesenModern.razor`, MudBlazor wie das Dashboard, ruft nur `HbService` auf (keine neue
Geschaeftslogik). Die klassische Seite `/handbestellwesen` bleibt (Import, Bereinigung, PHP-Optik).

| Bereich | Neu gegenueber PHP/klassisch |
| --- | --- |
| Kopf | Kacheln „Meine offenen“, „Wartet auf meine Freigabe“, „Offene Bestellungen“, „Diesen Monat bestellt“; Klick filtert die Liste |
| Neue Bestellung | Kreditor-Suche (Nummer, Name, Ort) statt Liste mit 4'092 Eintraegen; Kostenstelle mit der eigenen vorbelegt; Pruefung live (selbst berechtigt oder Freigabe noetig); bei genau einer berechtigten Person ist sie vorgeschlagen; Hinweis, wenn dieselbe Bestellung heute schon gespeichert wurde (gegen Doppelklick-Doubletten); fehlende Angaben werden genannt |
| Nach dem Speichern | grosse Nummer, „Nummer kopieren“, „Aehnlich bestellen“ (Felder bleiben), „Neue Bestellung“ |
| Offene | Filter Alle / Meine / Wartet auf meine Freigabe / Bereit fuer Wareneingang, Suche, Status-Chips, Aktionen nur wo erlaubt, mit Rueckfrage |
| Archiv | Suche, hoechstens 2'000 Treffer |
| Admin | Stammdaten mit Suche und Bearbeiten-Feld, Kostenstellen zeigen ihre Freigeber; Nutzung mit Monatsbalken |

Deploy 2026-10-22 14:52 (`d7478cd`), ohne Alarm, `/bestellwesen` HTTP 200. Nicht im Browser angesehen (Erweiterung nicht
verbunden). Der Reiter im Menue zeigt weiter auf `/handbestellwesen`; welche Seite er oeffnen soll, entscheidet Ingo.

Nachtrag 2026-10-27 (Wunsch Ingo): Die klassische Seite hat oben unter dem Logo einen blauen Link „Zur neuen Version“ auf
`/bestellwesen` (Commit `9ef7787`, Suite 1290/1290). Die Gegenrichtung „Klassische Ansicht“ gab es schon. Der Reiter im
Menue ist weiterhin unsichtbar und zeigt auf `/handbestellwesen`; Einblenden unter Admin › Menuestruktur (Ingo).

### 6.7 Admins

`Handbestellwesen:AdminUsers` = `koi`, `gun` (Armin Guentensperger, Auftrag Ingo 2026-10-22, deployed 15:22). Armin ist
auch Mitarbeiter (Limit CHF 7000, KoSt 900). Die Dashboard-Sperre `Security:Enabled` ist aus; jedes Windows-Konto erreicht
die Seiten, Rechte im Modul kommen aus Mitarbeitereintrag und Admin-Liste.

### 6.8 Betriebsbereitschaft und Freigabe-Regel (2026-10-27)

Befund auf einer Kopie der Tagessicherung vom 27.10. 02:38 (nur gelesen): seit dem Import keine neue Bestellung, Bereinigung
(6.5) noch nicht ausgefuehrt (1'289 offen), alle 109 Mitarbeitenden mit Konto, aber nur 12 mit Kostenstelle und 48 mit
Limit > 0. Mit der strengen Freigabe-Regel (Freigeber nur aus derselben Kostenstelle) haetten **929 von 1'032 Bestellungen
der letzten 12 Monate keinen Freigeber** gefunden; die PHP-Anwendung pruefte das Limit nur im Browser.

Entscheid Ingo 2026-10-27 (Variante 1a): **Freigabe wie PHP**, jede aktive Person mit genuegend Limit, dieselbe
Kostenstelle steht zuerst in der Auswahl. Umschaltbar auf die strenge Regel mit `Handbestellwesen:FreigabeNurGleicheKostenstelle`
= `true` in `appsettings.json` (Variante 1b), sobald Kostenstellen gepflegt sind. Commit `a0030e8`, Suite 1291/1291.

Die PHP-Anwendung ist abgeschaltet (Auskunft Ingo 2026-10-27, wegen einer PHP-Luecke im Internet). Damit gibt es keine
parallele Nummernvergabe mehr. Nicht geprueft: ob zwischen dem Dump (22.10.) und dem Abschalten in PHP noch bestellt wurde;
solche Nummern fehlen in der neuen Datenbank und koennten erneut vergeben werden. Der Menue-Reiter ist seit dem 27.10.
sichtbar (Ingo).

Weiter offen fuer den Betrieb: Rechnungsbetrag erfassen und Mail beim Wareneingang (Punkt 4), Bereinigung ausfuehren,
Zugriff und Grundtests (Mail Ingo an die Beteiligten am 27.10. versendet: Wer darf zugreifen, eigenes Windows-Konto statt
gemeinsamem Login, Liste Name/KoSt/Limit/Freigabe/Wareneingang, ein bis zwei Personen fuer Grundtests; Antwort ausstehend).

### 6.9 Stammdaten loeschen, Mitarbeitende aus dem AD (2026-10-27)

Wunsch Ingo: Kreditorenstamm, Mitarbeiterverzeichnis, Kostenstelle und Sachkonten muessen hinzugefuegt und geloescht
werden koennen (wie im PHP), Namen aus dem AD, neue Leute legt die IT im AD an. Hinzufuegen und Bearbeiten gab es schon.

- **Loeschen** (nur Admin, in beiden Masken, mit Rueckfrage, im Protokoll): Kreditor, Kostenstelle und Sachkonto nur, wenn
  keine Bestellung sie nutzt; die Kostenstelle zusaetzlich nur ohne eingetragene Mitarbeitende (ihr Nummernzaehler geht mit).
  Mitarbeitende ohne Bestellungen werden geloescht, mit Bestellungen **deaktiviert** (bleiben fuer Archiv und Suche). Den
  eigenen Eintrag kann man nicht loeschen. Grund: physisches Loeschen im PHP hinterliess z. B. 50 offene Bestellungen
  ohne Mitarbeitereintrag. Entscheid durch Claude, von Ingo uebersteuerbar.
- **Aus dem AD uebernehmen** (Mitarbeiter neu): Suche ueber Name oder Kuerzel unter den aktiven AD-Konten
  (`HbAdSuche`, gleicher Filter wie die Personensuche in Trafag Projekte), „Uebernehmen“ fuellt Vorname, Nachname, Mail
  und Windows-Konto; Kurzzeichen = Konto (so bei allen 109 Eintraegen, Stand 27.10.). Kostenstelle und Limit traegt der
  Admin ein. Ist das Konto schon erfasst, kommt ein Hinweis. Kein automatischer Abgleich mit dem AD *(ueberholt 2026-11-17: naechtlicher Abgleich, 6.11)*: Wer neu im AD ist,
  wird beim Erfassen gesucht und uebernommen.
- Commit `a8e9eec`, Suite 1293/1293. Offen: ob IT-Konten Mitarbeitende pflegen duerfen, ohne Admin zu sein (heute nur
  Admins `koi`, `gun` und Mitarbeitende mit Haken „Admin“).

### 6.10 AD- und Altbestand-Mitarbeitende (2026-10-27)

Entscheid Ingo: „Unterscheidung AD-User und DB-User; DB-User pflegbar, aber keine neuen erstellbar, nur noch via AD neue
erstellbar und dort wartbar.“ Damit ist die Formulierung in 6.9 („Kurzzeichen und Nachname sind Pflicht“, Erfassen von Hand)
fuer neue Eintraege ueberholt.

- Neue Spalte `Mitarbeiter.AusAd` (beim Start per `ALTER TABLE` nachgezogen, Altbestand = 0). Anzeige „Herkunft“ AD oder
  Altbestand in beiden Masken.
- **Neu nur aus dem AD:** Das Formular fuer neue Mitarbeitende zeigt erst nach „Übernehmen“ aus der AD-Suche Felder. Der
  Server schlaegt das Konto beim Speichern selbst im AD nach (nur aktive Konten) und nimmt Konto, Kurzzeichen, Vor-, Nachname
  und Mail von dort, nicht aus dem Formular.
- **AD-Eintraege:** Konto, Kurzzeichen, Name und Mail gesperrt (Pflege im AD), Knopf „Aus AD aktualisieren“; Limit,
  Kostenstelle, Wareneingang, Admin und aktiv bleiben im Dashboard pflegbar.
- **Altbestand** (109 Eintraege aus der PHP-Datenbank): voll pflegbar wie bisher, auch loeschen/deaktivieren (6.9).
- Commit `3ff16f5`, Suite 1294/1294, deployed 2026-10-27 13:45. Folge: Faellt die AD-Suche auf dem Server aus, kann niemand Neues erfasst werden.

### 6.11 Naechtlicher AD-Abgleich (2026-11-17, deployed, Nachtlauf nur Vorschau; *Regeln teils ueberholt durch 6.12*)

Auftrag Ingo 2026-11-17 nach der Sitzung mit Oscar: „Es soll einen naechtlichen Abgleich geben. Gesperrte User oder User, die
nicht im AD sind, sollen automatisch auf inaktiv gesetzt werden, geloeschte User im AD auch auf geloescht im Tool. Sync jede
Nacht, Rollen lassen wir.“ Am selben Tag: Die Benutzerdaten kommen aus dem AD; Entra ist nur lesend und wird selbst aus dem AD
gespeist (Wochen_Todo 165).

**Gebaut** (Zweig `hb-adabgleich-1711` ab `7cc1a62`, Commit `6a7de50`, gemergt `0197661`; `Modules/Handbestellwesen/HbAdAbgleich.cs`):

- **AD lesen:** alle Personenkonten, auch gesperrte, seitenweise ohne Trefferbegrenzung (`HbAdVerzeichnis`). Bewusst nicht ueber
  die Personensuche, die bei Fehlern leer liefert, hoechstens 15 Treffer zeigt und nur aktive Konten kennt. Ein Fehler oder
  weniger als `MinAdKonten` (50) Konten heisst „AD nicht lesbar bzw. unvollstaendig“: Abbruch ohne Aenderung, nie „niemand im AD“.
- **Regeln**, nur fuer aktive Eintraege mit Konto (Konto ohne Domaene, Gross und Klein egal):

  | Befund | Wirkung |
  | --- | --- |
  | Konto im AD gesperrt (deaktiviert; nicht die voruebergehende Sperre nach falschen Passwoertern) | inaktiv |
  | Altbestand, Konto nicht im AD | inaktiv („nicht im AD“) |
  | AD-Eintrag, Konto nicht mehr im AD | inaktiv („im AD geloescht“), **nicht geloescht** (Frage 1) |
  | AD-Eintrag, Vorname, Nachname oder Mail im AD geaendert | nachgefuehrt; Konto und Kurzzeichen bleiben, Bestellungen verweisen darauf |
  | Konto im AD abgelaufen, aber nicht gesperrt | nur Hinweis (Frage 4) |
  | Altbestand, Konto im AD aktiv | unveraendert (Frage 3) |

  Limit, Kostenstelle, Admin und Wareneingang bleiben („Rollen lassen wir“). Niemand wird automatisch wieder aktiv (Frage 2).
  Jede Aenderung steht im Protokoll (Benutzer `ad-abgleich` oder das Admin-Konto), bei Deaktivierten mit den offenen Bestellungen
  als Besteller und den offenen Freigaben.
- **Nachtlauf** (`HbAdAbgleichJob`): ab `Uhrzeit` (02:30) einmal am Tag. Ein verpasster Lauf wird am selben Tag nachgeholt (Pruefung
  alle 15 Minuten), nach einem AD-Fehler fruehestens zwei Stunden spaeter. Er aendert nichts, wenn mehr als `MaxDeaktivierungen`
  (10) Personen deaktiviert wuerden („gestoppt“, gilt als erledigt fuer den Tag).
- **Schalter** `Handbestellwesen:AdAbgleich`: `Enabled` true, **`Schreiben` false** (der Nachtlauf schreibt nur die Vorschau ins
  Protokoll und aendert nichts), `Uhrzeit`, `MaxDeaktivierungen`, `MinAdKonten`. `Schreiben` an braucht einen Deploy.
- **Oberflaeche:** klassische Ansicht, Admin-Bereich „AD-Abgleich“ (`/handbestellwesen/adabgleich`; Link auch in der neuen Ansicht,
  Reiter Nutzung): Vorschau mit Befund, Aenderung und offenen Bestellungen je Person, „Jetzt ausfuehren“ mit Rueckfrage (ohne die
  Grenze, nach Sicht der Vorschau), die letzten zehn Laeufe.
- **Tests:** 12 neue (`HandbestellwesenAdAbgleichTests`), Handbestellwesen 33/33 (zehnmal hintereinander gruen). Ganze Suite im
  Worktree: erster Lauf 1606/1607, der Name des roten Tests ist nicht festgehalten (LEARNINGS 2026-11-17), vier Wiederholungen
  1607/1607; Release-Worktree 1607/1607.

**Deploy 2026-11-17 11:28** (`6a7de50`, aus dem Zweig, nicht aus dem Arbeitszweig mit Spanien): bitgleich, zehn Routen 200, Alarm
nur Checkpoint (`trafag_exporter.db` gleich gross, neue Zeit; `-wal`/`-shm` 11:28 wieder da). Keine Wochensicherung (juengste vom
16.11.). Vorher kein Einkauf-Lauf (WAL 634 KB).

**Erste Vorschau aus der Produktion (2026-11-17 kurz nach dem Deploy, im Chrome, nichts geaendert):** Das AD ist vom Server aus
lesbar, **1'539 Personenkonten**. **109 aktive Mitarbeitende, 15 wuerden inaktiv**, alle aus dem Altbestand und alle mit Befund
„im AD gesperrt“; kein Eintrag „nicht im AD“ und keine Namensaenderung. Fuenf der 15 haben zusammen 12 offene Bestellungen als
Besteller, keine offene Freigabe. 15 liegt ueber der Grenze 10: auch mit `Schreiben` an wuerde der erste Nachtlauf stoppen. Die
Liste mit Namen steht nur im Dashboard, nicht hier.

*Nachtrag 11:33:* Der Nachtjob laeuft auf dem Server. Um 11:30 holte er den Lauf des Tages nach und stoppte wie vorgesehen (15 ueber
der Grenze 10; Protokoll `AD-Abgleich gestoppt`, Benutzer `ad-abgleich`, nichts geaendert). Damit ist der 17.11. erledigt, der
naechste Lauf ist am 18.11. ab 02:30. Weil die Grenze vor dem Schalter `Schreiben` geprueft wird, heisst ein solcher Lauf
„gestoppt“ und nicht „Vorschau“. **Achtung:** „Jetzt ausfuehren“ schreibt unabhaengig von `Schreiben` und steht jedem Admin
offen (`koi`, `gun` und alle mit Haken „Admin“). Bestaetigt sind in der Produktion bisher nur der Fall „Altbestand, im AD
gesperrt“ und der Nachtjob; Namensnachfuehrung und „im AD geloescht“ sind nur durch Tests belegt, die Seite wurde als Text
gelesen, nicht als Bild angesehen.

**Fragen an Ingo** (Stand 2026-11-17; bis zur Antwort gilt die vorsichtige Variante). *Beantwortet am selben Tag, siehe 6.12.*

1. „Geloescht im Tool“: physisch loeschen wie der Knopf Loeschen (nur ohne Bestellungen, sonst deaktivieren) oder deaktivieren mit
   Vermerk (gebaut)? Das AD unterscheidet „geloescht“ nicht von „nie vorhanden“; gebaut ist die Unterscheidung ueber die Herkunft.
2. Wird ein Konto im AD wieder aktiv: automatisch wieder aktiv im Tool oder von Hand (gebaut)? Von Hand Deaktivierte duerften dabei
   nicht mitkommen.
3. Altbestand (109 aus PHP), dessen Konto im AD aktiv ist: in AD-Eintraege umwandeln (Name und Mail aus dem AD, gesperrt) oder
   lassen (gebaut)?
4. Abgelaufene Konten (Ablaufdatum im AD vorbei, nicht gesperrt): wie gesperrte behandeln?
5. Offene Bestellungen Deaktivierter (12 bei fuenf Personen): was soll passieren? Heute nur in Vorschau und Protokoll sichtbar.
6. Grenze 10 je Nacht und Meldung: passend? Soll jemand das Ergebnis per Mail bekommen (heute Protokoll und Bereich AD-Abgleich)?
7. Ablauf zum Scharfschalten: die 15 einmal von Hand ausfuehren, dann `Schreiben` an (naechster Deploy)?
8. Austritte: Sperrt die IT das AD-Konto am Austrittstag? Sonst bleibt die Person so lange aktiv; spaeter allenfalls Austritt aus
   dem Rexx Hub.

### 6.12 Antworten Ingo und Teil 2 des AD-Abgleichs (2026-11-17 nachmittags, deployed, noch nicht ausgefuehrt)

**Antworten Ingo** auf die Frageliste im Chat (dort anders nummeriert als 6.11, hier nach Inhalt):

| Frage | Antwort Ingo |
| --- | --- |
| Geloescht im AD | deaktivieren wie gebaut |
| Offene Bestellungen Deaktivierter | Hinweis an Admin, Neuzuweisung durch Admin |
| Scharfschalten | „ja mach scharf“ |
| Im AD wieder aktiv | automatisch wieder aktiv |
| Altbestand mit aktivem AD-Konto | wird AD-Eintrag, Name und Mail aus dem AD; „niemals etwas im AD schreiben, nur lesen“ |
| Abgelaufene Konten | wie gesperrt |
| Meldung | neuer Reiter „Deaktivierte“, sichtbar fuer Admins (statt Mail) |
| Neue AD-Konten | „Liste wer reinkommen soll ins Tool, viele brauchen es ja nicht“: gelesen als weiter nur von Hand, nichts gebaut |

Weiter offen: die Grenze von 10 Deaktivierungen je Nacht (nicht beantwortet, bleibt) und ob die IT das AD-Konto am Austrittstag
sperrt (nicht gefragt).

**Gebaut** (Zweig `hb-adabgleich-1711` ab `6a7de50`, Commit `44a3ed1`, gemergt `ffe19e2`):

- **Vermerk am Mitarbeitereintrag** (neue Spalten `AdDeaktiviert`, `InaktivSeit`, `InaktivGrund`, beim Start per `ALTER TABLE`):
  der Abgleich setzt ihn beim Deaktivieren; wer von Hand aktiv oder inaktiv gesetzt wird, verliert ihn (der Server nimmt ihn aus dem
  gespeicherten Eintrag, nicht aus dem Formular). **Wieder aktiv** macht der Abgleich nur Eintraege mit Vermerk, und nur wenn das
  Konto im AD aktiv und nicht abgelaufen ist; von Hand Deaktivierte bleiben inaktiv.
- **Abgelaufen** = wie gesperrt: deaktivieren.
- **Altbestand mit aktivem AD-Konto** wird AD-Eintrag: Vorname, Nachname und Mail aus dem AD, `AusAd` an; Kurzzeichen und Konto
  bleiben (Bestellungen verweisen auf das Kurzzeichen, z. B. Kurzzeichen in Grossbuchstaben zu einem Konto in Kleinbuchstaben).
  Dazu aendert auch „Aus AD aktualisieren“ das Kurzzeichen nicht mehr (vorher setzte es Kurzzeichen = Konto).
- **Reiter „Deaktivierte“** in der neuen Ansicht (`/bestellwesen/deaktivierte`, nur Admins): offene Bestellungen, deren Besteller
  oder offene Freigabe an einer inaktiven Person haengt, mit Neuzuweisung (Besteller: jede aktive Person; Freigabe: aktive Person
  mit genuegend Limit, nicht der Besteller, bei `FreigabeNurGleicheKostenstelle` nur dieselbe Kostenstelle; jede Zuweisung im
  Protokoll), darunter alle inaktiven Personen mit „inaktiv seit“ und Grund. **Hinweis** oben auf der Seite fuer Admins, sobald eine
  offene Bestellung an einer inaktiven Person haengt.
- **`Schreiben` an.** Grenze 10 bleibt; „Jetzt ausfuehren“ eines Admins wirkt weiter ohne Grenze.
- **Nur lesen im AD:** im Modul gibt es keinen schreibenden Verzeichnisaufruf (geprueft per Suche nach `CommitChanges`,
  Eigenschaftszuweisungen und `Invoke`; nur `DirectorySearcher`).
- Tests: 15 im Abgleich (vorher 12), Handbestellwesen 36/36, Suite 1610/1610 mit Ergebnisdatei, Release-Worktree 1610/1610.

**Deploy 2026-11-17 12:36** (`44a3ed1`, aus dem Zweig): bitgleich, zehn Routen 200, Alarm nur die Schreibzeit von
`trafag_exporter.db-wal` (Groesse gleich, DB unveraendert); vorher gewartet, bis der Laenderexport von 12:00 durch war
(Sales_All 12:10). Keine Wochensicherung faellig. Die Sicherung des Moduls vom 17.11. 05:06 liegt vor.

*Nachtrag 12:50:* Dieser Deploy nahm den Shopfloor-Deploy der anderen Sitzung von 12:25 (`3298c93`, CO-PA schreiben und
Arbeitsplatzfilter M*) zurueck, weil mein Zweig ihn nicht enthielt (beide ab `6a7de50`). Bemerkt 12:40 beim Nachfuehren der Doku;
gemeinsamer Stand `2e62163` (= `44a3ed1` + `3298c93`, Suite 1610/1610 samt Schaltertest der Shopfloor-Sitzung) deployed 12:43,
bitgleich, DB unveraendert, gemergt `fd49894`. Die Shopfloor-Sitzung ist informiert und prueft ihre Werte; die CO-PA-Werte liegen
in `shopfloor.db` und werden von einem Lauf ohne CO-PA nicht ueberschrieben (Auskunft der Shopfloor-Sitzung); deren Pruefung 12:50: nichts nachzuladen. LEARNINGS 2026-11-17.
**Produktiv ist damit `2e62163` (Handbestellwesen `44a3ed1` und Shopfloor `3298c93`); Spanien `c9208b5` nur aus dem Arbeitszweig ab `fd49894` oder juenger.**

**Vorschau vor dem Ausfuehren (12:40, nichts geaendert):** 1'539 AD-Konten, 109 aktive (also hatte niemand den alten Knopf
gedrueckt). 15 wuerden inaktiv, genau die 15 aus dem Protokoll von 11:30 (alle Altbestand, im AD gesperrt; fuenf davon mit zusammen
12 offenen Bestellungen als Besteller). 94 Altbestand-Eintraege wuerden AD-Eintraege; keiner verliert dabei eine gefuellte Mail.
Bei 10 aendert sich der Name: neun Schreibweisen (Umlaute, Akzente, Leerzeichen) oder ein neuer Nachname bei gleichem Vornamen,
**einer ist eine andere Person**: Vor- und Nachname im AD stimmen nicht mit dem Altbestand ueberein, das Konto wurde offenbar neu
vergeben. Ihn automatisch zu uebernehmen, schriebe die alten Bestellungen dieser Kurzzeichen der neuen Person zu. **Darum nicht
ausgefuehrt; Entscheid Ingo offen.** Der Nachtlauf aendert vorerst nichts: der 17.11. ist mit dem gestoppten Lauf von 11:30
erledigt, und am 18.11. stoppt er wieder an der Grenze (15 > 10).

### 6.13 Stammdaten fuer alle Besteller, Freigabe ueber CHF 1'000 mit Rollen (2026-11-18, deployed 08:15 `f2c0267`)

Anlass: Mails Einkauf 17./18.11. (Sandro nach dem Test: Bestellung, Limit, Wareneingang; Vinc: PCBWin fehlt als Kreditor, ueber
1'000 CHF muessen Kostenstellenleiter und Projektleiter in der Freigabe stehen; Alex: Mitarbeitende sollen Kreditoren,
Kostenstellen und Sachkonten selbst erfassen wie im alten Tool). Entscheide Ingo 18.11.: Projektleiter als Haken am Mitarbeiter;
freigeben nur mit genuegend Limit; Kreditoren, Kostenstellen **und** Sachkonten neu anlegen fuer alle.
*Damit ueberholt:* „Stammdaten nur Admin“ (Abschnitt 2.2/6) gilt nur noch fuer Aendern, Loeschen und Mitarbeitende.

- **Neu anlegen:** jede aktive Person mit Mitarbeitereintrag und jeder Admin (`HbService.NurErfasser`); aendern und loeschen nur
  Admin (`NurAdmin`, Meldung „Stammdaten ändern oder löschen darf nur ein Admin“). Reiter „Stammdaten“ fuer alle Erfasser sichtbar,
  ohne Unterreiter Mitarbeiter, ohne Bearbeiten-Stift und Loeschen; Hinweis oben. Neuer Kreditor mit gleichem Namen (nur Buchstaben
  und Ziffern, ohne Rechtsform am Ende wie AG/GmbH) wird abgelehnt und nennt den vorhandenen.
- **Freigabe ueber CHF 1'000** (`HbOptions.FreigabeRollenAbBetrag`, Vorgabe 1000): Die Liste zeigt zuerst den Leiter der
  Bestell-Kostenstelle (`HbKostenstelle.Leiter`), dann alle Projektleiter (neue Spalte `Mitarbeiter.IstProjektleiter`, beim Start per
  `ALTER TABLE`, Schalter im Mitarbeiter-Formular), dann alle uebrigen Berechtigten. Wer zu wenig Limit hat, steht grau und ist nicht
  waehlbar („Limit CHF … zu tief“); die Pruefung beim Speichern ist unveraendert (`FreigeberAsync`). Bis CHF 1'000 wie bisher.
  **Ohne gepflegte Leiter und Projektleiter-Haken aendert sich nichts**: Ingo/Admin muss Leiter je Kostenstelle und die Haken setzen.
- **Wareneingang:** Meldung jetzt „Bestellung E26 … ist gebucht und archiviert.“ (Wunsch Sandro).
- Tests: Handbestellwesen 41/41 (fuenf neu), Suite 1617/1617, Release-Worktree 1617/1617.
- **Offen:** Sandros Punkt 2 („Limit 1'000 CHF ueber dem Betrag nicht moeglich“) ist unklar, Rueckfrage; PCBWin kann jetzt jeder
  Besteller selbst anlegen; Oberflaeche nach dem Deploy nicht im Browser geprueft (Windows-Chrome war nicht verbunden).

## 7. Naechste Schritte

1. ~~Dump pruefen und importieren~~ erledigt 22.10. (6.4). Offen: klaeren, ob nach dem Dump noch in PHP bestellt wurde.
2. ~~Oberflaeche ansehen / Deploy~~ deployed, Reiter seit 27.10. sichtbar; Grundtests durch die Anwender stehen aus.
3. Fachlichen Owner die Regeln aus Abschnitt 6 bestaetigen lassen.
4. Zugriff fuer Mitarbeitende ohne Dashboard-Zugang (`Security:AccessGroups`), AD-Gruppe mit IT klaeren.
5. AD-Abgleich (6.11): Liste der Vorschau pruefen, Fragen 1 bis 8 beantworten, die 15 von Hand ausfuehren, dann `Schreiben` an
   (braucht einen Deploy). *Stand 6.12: Fragen beantwortet, `Schreiben` an; offen ist der Entscheid zum Konto, das im AD einer
   anderen Person gehoert, danach einmal „Jetzt ausfuehren“.*
