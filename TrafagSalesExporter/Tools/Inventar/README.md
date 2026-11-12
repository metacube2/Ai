# Inventar: Bericht-Excel und Power-BI-Dateien finden

Stand: 2026-11-12 (Doku-Pruefung: Kopf nachgefuehrt; der Abschnitt „Einzelne Datei von Hand pruefen“ ist vom 2026-11-12); davor 2026-11-03. Fuer PM-14 (Wunsch Oscar in der Abschlussrunde: wo sind welche Power BI und Smartsheets, wie viele Excel
sind abgeloest, wie viele Stunden eingespart). Protokoll `docs/SITZUNG_ABSCHLUSSRUNDE_DASHBOARD_2026-11-03.md`,
Liste `projektmanagement/PM-14_Inventar_Abloesung.tsv`, Wochen_Todo 138.

## Was das Skript tut

`Scan-BerichtDateien.ps1` durchsucht Ordner nach `.xlsx/.xlsm/.xltx/.xltm/.xls/.xlsb/.pbix/.pbit` und schreibt **nur
Metadaten** in eine CSV-Datei (Semikolon, UTF-8 mit BOM, oeffnet direkt in Excel). Es liest keine Zelleninhalte und aendert
nichts. Als „Bericht-Datei“ gilt ein Excel mit mindestens einem von:

| Merkmal | Spalte |
| --- | --- |
| Datenverbindung (`xl/connections.xml`: ODBC, OLE DB, Text, Web) | `Verbindungen`, `Verbindungsarten`, `Ziele` |
| Power Query (`DataMashup` in `customXml`) | `PowerQuery`, `PowerQueryQuellen` (z. B. `Sql.Database(server)`, `SharePoint.Files(url)`) |
| Verknuepfung auf andere Arbeitsmappen (`xl/externalLinks`) | `ExterneVerknuepfungen` |
| Makros (`vbaProject.bin`) | `Makros` |

Verbindungszeichenfolgen werden auf Provider, Server, Datenbank, DSN und aehnliche Schluessel gekuerzt; **Benutzer und
Passwoerter werden nie ausgegeben**. `.xls` und `.xlsb` werden nur gelistet (`Bericht = unklar`). Bei neueren `.pbix` liegen
die Abfragen im komprimierten DataModel; dort hilft nur das Power-BI-Admin-Portal.

## Aufruf

```powershell
.\Tools\Inventar\Scan-BerichtDateien.ps1 -Pfad '\\fileserver\Einkauf','\\fileserver\Produktion' `
    -Ausgabe C:\temp\inventar.csv            # -SeitTagen 730 (Standard), 0 = alle; -Alle listet auch Excel ohne Verbindung
```

**Vor dem Lauf:** Freigabe der IT (Alex Donato) und die Liste der Pfade einholen. Auch Metadaten fremder Dateien (Pfade,
Dateinamen, Servernamen) sind vertraulich; die CSV nicht weitergeben, sondern die Treffer in PM14 (Trafag Projekte) als
INV-Aufgaben uebernehmen. Hinweis: Das Oeffnen kann den Zeitstempel „letzter Zugriff“ einer Datei setzen, falls das auf dem
Server aktiviert ist; Aenderungsdatum und Inhalt bleiben unveraendert.

## Getestet

2026-11-03 nur auf eigenen Ordnern (Repo, Downloads): 435 Dateien, u. a. `Zeichnungsänderung_DatenVonSAP.xlsx` erkannt als
OLE DB/Power Query mit `Sql.Database(tragvsqlc400)` (der Direktzugriff aus `saptasks/zeichnungsaenderung/NOTES.md`), eine
Arbeitsmappe mit externer Verknuepfung auf SharePoint, 22 `.pbix`. SharePoint und OneDrive durchsucht das Skript nicht
(dafuer Microsoft Graph oder ein Bericht der IT).

## Nicht vom Skript abgedeckt

- **Power BI im Dienst:** Workspaces, Berichte, Besitzer, Nutzung ueber das Power-BI-Admin-Portal bzw. die Admin-API (Rolle
  Fabric-/Power-BI-Admin, Alex); Pro-Lizenzen im M365-Admin-Center.
- **Smartsheet:** Export aus dem Smartsheet Admin Center (Benutzer, Lizenzen, Sheets mit Besitzer).
- **Eingesparte Stunden:** erfragen je Ablauf (Haeufigkeit mal Dauer), eintragen in den Stundenspalten der INV-Aufgaben.

## Einzelne Datei von Hand pruefen (Stand 2026-11-12)

Fuer eine einzelne Arbeitsmappe braucht es das Skript nicht. In Excel zeigen vier Stellen, ob sie ein „Bericht“ ist:

| Wo in Excel | Was man sieht |
| --- | --- |
| Daten > Abfragen und Verbindungen | Power-Query-Abfragen und Datenverbindungen; Doppelklick zeigt die Quelle (SAP, SQL, SharePoint, andere Excel) |
| Daten > Links bearbeiten (nur aktiv, wenn vorhanden) | Verknuepfungen zu anderen Arbeitsmappen |
| Entwicklertools > Makros, bzw. Endung `.xlsm` | ob Makros enthalten sind |
| Datei > Informationen | Autor, zuletzt geaendert von, Datum: wer die Datei betreut |

Fehlt alles, ist die Datei meist eine reine Eingabe- oder Kopierliste; dort steckt oft die meiste Handarbeit. Die Stunden
verraet keine Datei, sie werden bei den Verantwortlichen erfragt (Haeufigkeit mal Dauer).
