# Handbestellwesen: Abloesung der PHP-Anwendung durch ein Modul im Dashboard

Stand: 2026-10-22 (Nachtrag Antworten Ingo: Betrieb lokal bei Trafag, Benutzer ohne SAP-Konto, Plugin/KISS, AD,
Schreiben nach SAP). Status: **Konzept, Entscheide Ingo eingeholt, noch kein Code.** Wartet auf den MySQL-Dump.

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
3. **Schreiben nach SAP** (Wunsch Ingo). Erst klaeren, *was* in SAP entstehen soll (Bestellanforderung, Bestellung oder
   nur eine Buchung) und **ob die Lizenz das erlaubt**: Die Benutzer haben bewusst kein SAP-Konto. Schreibt das Dashboard
   in ihrem Auftrag ueber einen technischen Benutzer, kann SAP das als indirekte Nutzung werten, die lizenzpflichtig ist.
   Vor dem Bau mit dem SAP-Lizenzverantwortlichen klaeren. Technisch braeuchte es einen schreibenden Gateway-Service in
   T76, Transport nach P76 durch Ingo.
4. **Abloesung.** Kurzer Parallelbetrieb, Stichtag, PHP-Anwendung abschalten, Datenbankpasswoerter aendern.

## 5. Offene Punkte

| Nr | Punkt | Wer |
| --- | --- | --- |
| 1 | MySQL-Dump (Struktur und Daten). Im uebergebenen Ordner liegt keiner, nur das Skript `backup/dbBackup.sh`; es legt Sicherungen auf dem Server unter `…/admin/backup/<JJJJMMTT>/` ab. Dort nachsehen oder auf dem Server `mysqldump` ausfuehren (Passwort von Hand eingeben, nicht im Befehl) | Ingo |
| 2 | Fachlicher Owner und Freigabe vor Inbetriebnahme oder Ankuendigung (Einkauf? Finanz?) | Ingo |
| 3 | Wer bestellt, wer gibt frei, wer bucht Wareneingang; AD-Gruppen oder Kontenliste | Ingo / Owner |
| 4 | Werden Rechnungsbetrag und Mail beim Wareneingang noch gebraucht? | Owner |
| 5 | Datenbankpasswoerter nach der Abloesung aendern; pruefen, ob beim frueheren externen Hoster noch Daten liegen | Ingo |
| 6 | Was soll in SAP entstehen, und ist das lizenzrechtlich fuer Benutzer ohne SAP-Konto zulaessig? | Ingo / SAP-Lizenz |
| 7 | AD-Gruppen fuer Besteller, Freigeber, Wareneingang, Admin | Ingo / IT |
