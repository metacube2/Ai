# Alphaplan Discovery Exporter Guide

Stand: 2026-06-08

Zweck: Diese Anleitung dokumentiert das Phase-1-Paket fuer Deutschland/Alphaplan. Das Paket soll auf dem deutschen Alphaplan-/SQL-Server laufen, relevante SQL-Datenbanken, Tabellen und Views finden, CSV-Dateien erzeugen und diese optional per `rclone` nach SharePoint laden.

## Status

- Paket erstellt: `AlphaplanExportPackage`.
- ZIP erstellt: `AlphaplanExportPackage.zip`.
- Phase: Discovery / Rohdatenanalyse.
- BiDashboard-Import wird dadurch noch nicht angepasst.
- BiDashboard-App-Code wird fuer diesen Exporter nicht benoetigt.
- PowerShell-Syntax des Scripts wurde lokal geprueft.

## Dateien

| Datei | Zweck |
| --- | --- |
| `AlphaplanExportPackage/Run-AlphaplanDiscoveryAndUpload.ps1` | All-in-one Discovery, optional Samples, optional rclone Upload |
| `AlphaplanExportPackage/README.txt` | Kurzanleitung fuer den DE-Server |
| `AlphaplanExportPackage.zip` | Kopierbares Paket fuer den DE-Server |
| `docs/ALPHAPLAN_SQL_RCLONE_KONZEPT_DE_2026-06-08.md` | Fach-/Technikkonzept |
| `docs/ALPHAPLAN_DISCOVERY_EXPORTER_GUIDE_2026-06-08.md` | Diese Nachdokumentation |

## Zielbild

Der Export laeuft direkt auf dem Alphaplan-Server in Deutschland:

1. PowerShell startet das Script.
2. Script verbindet sich read-only mit SQL Server.
3. Script scannt SQL-Datenbanken, Tabellen und Views.
4. Script bewertet Kandidaten anhand von Namen und Spalten.
5. Script schreibt `candidate_objects.csv`.
6. Script schreibt `export_summary.csv`.
7. Optional schreibt es kleine `sample_*.csv`.
8. Optional laedt `rclone` die Dateien nach SharePoint.

## SharePoint-Ziel

Default:

```text
trafag-bi:Import/Finance/Deutschland/AlphaplanRaw
```

Dieser Rohdatenordner ist bewusst getrennt vom produktiven Deutschland-Import. So koennen die Discovery-CSV nicht versehentlich vom bestehenden Import als finale Finance-Dateien gelesen werden.

## Lokaler Zielordner auf dem DE-Server

Default:

```text
C:\Trafag\AlphaplanExport
```

Unterordner:

```text
C:\Trafag\AlphaplanExport\out
C:\Trafag\AlphaplanExport\logs
```

Pro Lauf entsteht:

```text
C:\Trafag\AlphaplanExport\out\Alphaplan_SQL_Discovery_YYYYMMDD_HHMMSS
```

## Ergebnisdateien

| Datei | Inhalt |
| --- | --- |
| `candidate_objects.csv` | Relevante SQL-Tabellen/Views mit Score, Spalten, Datums-/Betrags-/Key-Kandidaten |
| `export_summary.csv` | Status pro Datenbank und optional pro Sample-Export |
| `README.txt` | Laufbezogene Kurzbeschreibung |
| `sample_*.csv` | Optionale kleine Beispielauszuege aus Top-Kandidaten |

## Bewertung der Kandidaten

Das Script bewertet Tabellen/Views ueber Objekt- und Spaltennamen. Treffer gibt es u. a. fuer:

- Rechnung, Faktura, Invoice, Beleg
- Umsatz, Verkauf, Sales, Revenue
- Position, Zeile, Line
- Auftrag, Order
- Kunde, Debitor, Customer, Adresse
- Artikel, Material, Item, Produkt
- Betrag, Netto, Umsatz, Amount, Price, Preis, Summe
- Menge, Anzahl, Quantity, Qty
- Waehrung, Currency
- Warengruppe, Produktgruppe
- Gutschrift, Storno, Credit

Die Discovery ist kein finaler Beweis, sondern eine Vorauswahl fuer DE/IT und Finance.

## Wichtige Parameter

| Parameter | Default | Zweck |
| --- | --- | --- |
| `-ServerInstance` | `localhost` | SQL Server / Instanz |
| `-Database` | leer | leer = alle erreichbaren User-Datenbanken scannen |
| `-SqlCredential` | leer | optional SQL-Login statt Windows Integrated |
| `-BaseDirectory` | `C:\Trafag\AlphaplanExport` | lokaler Arbeitsordner |
| `-MaxCandidatesPerDatabase` | `80` | maximale Kandidaten je Datenbank |
| `-ExportSamples` | aus | kleine Beispiel-CSV erzeugen |
| `-MaxSampleObjects` | `15` | maximale Sample-Objekte |
| `-SampleRows` | `200` | Zeilen pro Sample |
| `-SkipUpload` | aus | nur lokal erzeugen, kein rclone Upload |
| `-RcloneExe` | `C:\Tools\rclone.exe` | bevorzugter rclone Pfad |
| `-RcloneRemote` | `trafag-bi` | rclone Remote |
| `-RcloneTarget` | `Import/Finance/Deutschland/AlphaplanRaw` | SharePoint-Zielpfad |

## Standardlauf lokal ohne Upload

Auf dem DE-Server im Paketordner:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\Run-AlphaplanDiscoveryAndUpload.ps1 -SkipUpload
```

Das ist der empfohlene erste Test, weil keine SharePoint-/rclone-Abhaengigkeit geprueft werden muss.

## Standardlauf mit Upload

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\Run-AlphaplanDiscoveryAndUpload.ps1
```

Das Script prueft/erstellt das SharePoint-Ziel mit `rclone mkdir`, laedt CSV/TXT hoch und verifiziert danach `candidate_objects.csv` per `rclone lsf`.

## Lauf fuer bekannte Datenbank

Wenn der Alphaplan-Datenbankname bekannt ist:

```powershell
.\Run-AlphaplanDiscoveryAndUpload.ps1 -Database "ALPHAPLAN"
```

Wenn die SQL-Instanz nicht `localhost` ist:

```powershell
.\Run-AlphaplanDiscoveryAndUpload.ps1 -ServerInstance "SERVERNAME\INSTANCE" -Database "ALPHAPLAN"
```

## Lauf mit Samples

```powershell
.\Run-AlphaplanDiscoveryAndUpload.ps1 -Database "ALPHAPLAN" -ExportSamples
```

Samples sind auf `TOP 200` pro Objekt limitiert. Sie helfen Andreas/DE IT, die Inhalte der Top-Kandidaten schnell zu beurteilen.

## SQL Authentifizierung

Windows Integrated ist Default. Fuer SQL-Login:

```powershell
$cred = Get-Credential
.\Run-AlphaplanDiscoveryAndUpload.ps1 -ServerInstance "SERVERNAME\INSTANCE" -Database "ALPHAPLAN" -SqlCredential $cred
```

Empfohlen ist ein read-only Benutzer oder Windows-Servicekonto.

## rclone Voraussetzungen

`rclone` muss auf dem DE-Server eingerichtet sein.

Erwarteter Remote-Name:

```text
trafag-bi
```

Der Remote soll auf die Dokumentbibliothek `Shared Documents` dieser Site zeigen:

```text
https://trafagag.sharepoint.com/sites/WorldwideBIPlatform
```

Tests:

```powershell
rclone lsd trafag-bi:
rclone lsd trafag-bi:"Import/Finance"
rclone lsd trafag-bi:"Import/Finance/Deutschland"
```

Das Script sucht `rclone` automatisch an diesen Stellen:

```text
- Parameter -RcloneExe
- rclone.exe im Scriptordner
- C:\Tools\rclone.exe
- C:\Tools\rclone\rclone.exe
- C:\Tools\rclone\rclone\rclone.exe
- rclone aus PATH
```

## Firewall und Berechtigungen

Vom DE-Server benoetigt:

- SQL Zugriff auf die lokale/interne Alphaplan-Datenbank.
- Ausgehend TCP 443 zu Microsoft 365/SharePoint fuer `rclone`.
- Bei Erstkonfiguration optional Browser-/Device-Login fuer Microsoft 365.

Vom BiDashboard-Server benoetigt:

- Kein direkter SQL-Zugriff auf Alphaplan fuer Phase 1.

SQL-Rechte:

- read-only
- idealerweise Zugriff nur auf benoetigte Datenbanken, Tabellen oder Views
- keine Schreibrechte

## Abgrenzung zum finalen Import

Phase 1 erzeugt noch kein finales Finance-Format. Ziel ist nur:

- richtige Datenbanken finden
- relevante Tabellen/Views finden
- Feldnamen und Beziehungen erkennen
- Beispiele fuer Andreas/DE IT bereitstellen

Der spaetere Import kann danach auf eine finale SQL-View oder ein gemapptes CSV umgestellt werden. Empfohlene finale View:

```text
vw_BiDashboard_FinanceSales_DE
```

## Checkliste fuer DE/IT

Nach dem ersten Discovery-Lauf bitte pruefen:

- Welche Datenbank ist Alphaplan produktiv?
- Welche Tabelle/View ist Rechnungs-Kopf?
- Welche Tabelle/View ist Rechnungs-Position?
- Wie werden Kopf und Position verbunden?
- Welche Spalte ist Rechnungsnummer?
- Welche Spalte ist Positionsnummer?
- Welche Spalte ist Rechnungsdatum oder Buchungsdatum?
- Welche Spalte ist Nettoumsatz auf Positionsebene?
- Welche Spalte ist Menge?
- Welche Spalte ist Artikel-/Materialnummer?
- Welche Spalten enthalten Kundenname, Kundennummer und Kundenland?
- Wie erkennt man Gutschriften?
- Wie erkennt man Stornos?
- Gibt es eine Aenderungs-/Erfassungsdatumsspalte fuer Delta?

## Bekannte Grenzen

- Discovery erkennt Kandidaten heuristisch anhand von Namen. Tabellen mit neutralen Alphaplan-Namen koennen fehlen.
- Wenn SQL-Rechte zu eng sind, sind die CSV leer oder unvollstaendig.
- `RowCountEstimate` ist eine SQL-Server-Schaetzung aus Partition-Stats.
- Sample-Dateien sind nur Auszuege und nicht fuer Summenabgleich geeignet.
- Der Upload prueft aktuell `candidate_objects.csv`, nicht jede einzelne Sample-Datei.

## Naechster Schritt

1. ZIP auf DE-Server kopieren.
2. Discovery lokal ohne Upload ausfuehren.
3. `candidate_objects.csv` pruefen.
4. Discovery mit Upload ausfuehren.
5. Andreas/DE IT markiert die relevanten Alphaplan-Objekte.
6. Danach wird der finale Alphaplan-Finance-Export oder die Importanpassung definiert.

