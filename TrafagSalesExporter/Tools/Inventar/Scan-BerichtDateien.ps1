<#
.SYNOPSIS
    Sucht auf Laufwerken nach "Bericht-Dateien" (Excel mit Datenverbindung, Power Query, externen Verknuepfungen
    oder Makros, dazu Power-BI-Dateien) und schreibt NUR Metadaten in eine CSV-Datei.

.DESCRIPTION
    Angelegt 2026-11-03 fuer PM-14 (Inventar Power BI / Smartsheet / Excel, Wunsch Oscar in der Abschlussrunde).
    Das Skript liest keine Zelleninhalte und aendert keine Datei. Je Datei werden ausgewertet:
      - xl/connections.xml   : Art der Verbindung (ODBC, OLE DB, Text, Web ...) und Ziel (Server/Datenbank/DSN);
                               Benutzer und Passwoerter werden aus Verbindungszeichenfolgen ENTFERNT.
      - customXml (DataMashup): Power-Query-Quellen (Sql.Database, SapHana.Database, Excel.Workbook, SharePoint ...).
      - xl/externalLinks      : Pfade verknuepfter Arbeitsmappen.
      - vbaProject.bin        : nur "Makros vorhanden" (keine Auswertung des Codes).
      - .pbix                 : Power-Query-Quellen aus dem eingebetteten DataMashup.
    Alte Binaerformate (.xls, .xlsb) werden nur gelistet, nicht geoeffnet.

    Ausfuehren erst nach Freigabe durch die IT (Alex Donato) und nur auf vereinbarten Pfaden: auch reine Metadaten
    fremder Dateien sind Personen- bzw. Abteilungsdaten. Doku: projektmanagement/PROJEKTSTATUS.md PM-14,
    Tools/Inventar/README.md.

.PARAMETER Pfad
    Ein oder mehrere Startordner (lokal oder UNC), z. B. \\server\abteilungen\Einkauf.

.PARAMETER Ausgabe
    CSV-Datei (Semikolon, UTF-8 mit BOM fuer Excel).

.PARAMETER SeitTagen
    Nur Dateien, die in den letzten N Tagen geaendert wurden (Standard 730). 0 = alle.

.PARAMETER Alle
    Auch Excel-Dateien ohne Verbindung/Makro auflisten (sonst nur "Bericht-Dateien").

.EXAMPLE
    .\Scan-BerichtDateien.ps1 -Pfad '\\fileserver\Einkauf' -Ausgabe C:\temp\inventar_einkauf.csv
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string[]]$Pfad,
    [Parameter(Mandatory = $true)][string]$Ausgabe,
    [int]$SeitTagen = 730,
    [switch]$Alle,
    [int]$MaxDateien = 500000
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Continue'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$excelOffen = @('.xlsx', '.xlsm', '.xltx', '.xltm')
$excelAlt = @('.xls', '.xlsb')
$endungen = $excelOffen + $excelAlt + @('.pbix', '.pbit')
$grenze = if ($SeitTagen -gt 0) { (Get-Date).AddDays(-$SeitTagen) } else { [datetime]::MinValue }
$verbindungsArt = @{ '1' = 'ODBC'; '2' = 'DAO'; '3' = 'Datei'; '4' = 'Web'; '5' = 'OLE DB'; '6' = 'Text'; '7' = 'ADO'; '8' = 'DSP' }
$erlaubteSchluessel = @('provider', 'data source', 'server', 'initial catalog', 'database', 'dsn', 'host', 'location',
    'systemname', 'ashost', 'mshost', 'dbq', 'defaultdir', 'driver', 'catalog')

function Open-ZipNurLesen([string]$datei) {
    $stream = [System.IO.File]::Open($datei, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
    try { return New-Object System.IO.Compression.ZipArchive($stream, [System.IO.Compression.ZipArchiveMode]::Read, $false) }
    catch { $stream.Dispose(); throw }
}

function Read-Eintrag($zip, [string]$name) {
    $e = $zip.GetEntry($name)
    if ($null -eq $e) { return $null }
    $r = New-Object System.IO.StreamReader($e.Open())
    try { return $r.ReadToEnd() } finally { $r.Dispose() }
}

function Read-EintragBytes($entry) {
    $ms = New-Object System.IO.MemoryStream
    $s = $entry.Open()
    try { $s.CopyTo($ms) } finally { $s.Dispose() }
    return $ms.ToArray()
}

# Verbindungszeichenfolge auf unkritische Teile kuerzen (keine Benutzer, keine Passwoerter).
function Get-SicheresZiel([string]$cs) {
    if ([string]::IsNullOrWhiteSpace($cs)) { return '' }
    $teile = foreach ($p in ($cs -split ';')) {
        $kv = $p -split '=', 2
        if ($kv.Count -eq 2 -and $erlaubteSchluessel -contains $kv[0].Trim().ToLowerInvariant()) {
            '{0}={1}' -f $kv[0].Trim(), $kv[1].Trim()
        }
    }
    return ($teile -join '; ')
}

# Power-Query-Quellen aus einem DataMashup-Binaerblock (Version int32, Laenge int32, Paket-ZIP mit Formulas/Section1.m).
function Get-PowerQueryQuellen([byte[]]$bytes) {
    if ($null -eq $bytes -or $bytes.Length -lt 8) { return @() }
    $laenge = [BitConverter]::ToInt32($bytes, 4)
    if ($laenge -le 0 -or (8 + $laenge) -gt $bytes.Length) { return @() }
    $ms = New-Object System.IO.MemoryStream($bytes, 8, $laenge)
    $zip = New-Object System.IO.Compression.ZipArchive($ms, [System.IO.Compression.ZipArchiveMode]::Read)
    try {
        $m = Read-Eintrag $zip 'Formulas/Section1.m'
        if ($null -eq $m) { return @() }
        $muster = '(Sql\.Databases?|SapHana\.Database|SapBusinessWarehouse\.\w+|Odbc\.(?:DataSource|Query)|OleDb\.(?:DataSource|Query)|' +
                  'Oracle\.Database|Excel\.Workbook|Csv\.Document|File\.Contents|Folder\.Files|SharePoint\.(?:Files|Contents|Tables)|' +
                  'Web\.Contents|OData\.Feed|Smartsheet\.Tables|PowerBI\.Dataflows)\s*\(\s*(?:File\.Contents\s*\(\s*)?"([^"]*)"'
        return @([regex]::Matches($m, $muster) | ForEach-Object { '{0}({1})' -f $_.Groups[1].Value, $_.Groups[2].Value } | Select-Object -Unique)
    }
    finally { $zip.Dispose() }
}

function Get-Inventar([System.IO.FileInfo]$f) {
    $ergebnis = [ordered]@{
        Pfad = $f.DirectoryName; Datei = $f.Name; Typ = $f.Extension.ToLowerInvariant().TrimStart('.')
        GroesseKB = [math]::Round($f.Length / 1KB); Geaendert = $f.LastWriteTime.ToString('yyyy-MM-dd')
        Verbindungen = 0; Verbindungsarten = ''; Ziele = ''; PowerQuery = ''; PowerQueryQuellen = ''
        ExterneVerknuepfungen = ''; Makros = ''; Bericht = ''; Hinweis = ''
    }
    $ext = $ergebnis.Typ
    if ($excelAlt -contains ".$ext") {
        $ergebnis.Hinweis = 'Binaerformat, nicht ausgewertet'
        $ergebnis.Bericht = 'unklar'
        return [pscustomobject]$ergebnis
    }
    try {
        $zip = Open-ZipNurLesen $f.FullName
    }
    catch {
        $ergebnis.Hinweis = 'nicht lesbar: ' + $_.Exception.Message.Split([Environment]::NewLine)[0]
        $ergebnis.Bericht = 'unklar'
        return [pscustomobject]$ergebnis
    }
    try {
        if ($ext -in @('pbix', 'pbit')) {
            $dm = $zip.GetEntry('DataMashup')
            if ($null -ne $dm) {
                $q = Get-PowerQueryQuellen (Read-EintragBytes $dm)
                $ergebnis.PowerQuery = 'ja'
                $ergebnis.PowerQueryQuellen = ($q -join ' | ')
            }
            elseif ($null -ne $zip.GetEntry('DataModel')) {
                # Neuere .pbix speichern die Abfragen im komprimierten DataModel; das liest dieses Skript nicht.
                $ergebnis.Hinweis = 'Abfragen im DataModel, Quellen nicht auslesbar (Power-BI-Admin-Portal nutzen)'
            }
            $ergebnis.Bericht = 'Power BI'
            return [pscustomobject]$ergebnis
        }

        $xml = Read-Eintrag $zip 'xl/connections.xml'
        if ($null -ne $xml) {
            [xml]$c = $xml
            $arten = @(); $ziele = @()
            foreach ($con in $c.connections.connection) {
                $ergebnis.Verbindungen++
                $arten += $verbindungsArt["$($con.type)"]
                # Kindelemente ueber XPath lesen: unter StrictMode bricht ein fehlendes Element als Eigenschaft ab.
                $db = $con.SelectSingleNode('*[local-name()="dbPr"]')
                $web = $con.SelectSingleNode('*[local-name()="webPr"]')
                $text = $con.SelectSingleNode('*[local-name()="textPr"]')
                if ($null -ne $db) { $ziele += Get-SicheresZiel $db.GetAttribute('connection') }
                if ($null -ne $web) { $ziele += $web.GetAttribute('url') }
                if ($null -ne $text) { $ziele += $text.GetAttribute('sourceFile') }
            }
            $ergebnis.Verbindungsarten = (@($arten | Where-Object { $_ } | Select-Object -Unique) -join ', ')
            $ergebnis.Ziele = (@($ziele | Where-Object { $_ } | Select-Object -Unique) -join ' | ')
        }

        $quellen = @()
        foreach ($e in @($zip.Entries | Where-Object { $_.FullName -like 'customXml/item*.xml' })) {
            $inhalt = Read-Eintrag $zip $e.FullName
            if ($inhalt -and $inhalt.Contains('DataMashup')) {
                $ergebnis.PowerQuery = 'ja'
                $b64 = [regex]::Match($inhalt, '>([A-Za-z0-9+/=\s]+)</[^>]*DataMashup>').Groups[1].Value
                if ($b64) {
                    try { $quellen += Get-PowerQueryQuellen ([Convert]::FromBase64String(($b64 -replace '\s', ''))) } catch { }
                }
            }
        }
        $ergebnis.PowerQueryQuellen = (@($quellen | Select-Object -Unique) -join ' | ')

        $links = @()
        foreach ($e in @($zip.Entries | Where-Object { $_.FullName -like 'xl/externalLinks/_rels/*.rels' })) {
            [xml]$r = Read-Eintrag $zip $e.FullName
            foreach ($rel in $r.Relationships.Relationship) { $links += $rel.Target }
        }
        $ergebnis.ExterneVerknuepfungen = (@($links | Select-Object -Unique) -join ' | ')

        if ($null -ne $zip.GetEntry('xl/vbaProject.bin')) { $ergebnis.Makros = 'ja' }

        $istBericht = $ergebnis.Verbindungen -gt 0 -or $ergebnis.PowerQuery -eq 'ja' -or $links.Count -gt 0 -or $ergebnis.Makros -eq 'ja'
        $ergebnis.Bericht = if ($istBericht) { 'ja' } else { 'nein' }
    }
    catch {
        $ergebnis.Hinweis = 'Auswertung fehlgeschlagen: ' + $_.Exception.Message.Split([Environment]::NewLine)[0]
        $ergebnis.Bericht = 'unklar'
    }
    finally { $zip.Dispose() }
    return [pscustomobject]$ergebnis
}

$treffer = New-Object System.Collections.Generic.List[object]
$gezaehlt = 0
foreach ($start in $Pfad) {
    if (-not (Test-Path -LiteralPath $start)) { Write-Warning "Pfad nicht erreichbar: $start"; continue }
    $dateien = Get-ChildItem -LiteralPath $start -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $endungen -contains $_.Extension.ToLowerInvariant() -and -not $_.Name.StartsWith('~$') -and $_.LastWriteTime -ge $grenze }
    foreach ($f in $dateien) {
        $gezaehlt++
        if ($gezaehlt -gt $MaxDateien) { Write-Warning "MaxDateien ($MaxDateien) erreicht, Abbruch."; break }
        if ($gezaehlt % 200 -eq 0) { Write-Progress -Activity 'Inventar' -Status "$gezaehlt Dateien, $($treffer.Count) Treffer" -CurrentOperation $f.FullName }
        $zeile = Get-Inventar $f
        if ($Alle -or $zeile.Bericht -ne 'nein') { $treffer.Add($zeile) }
    }
}
Write-Progress -Activity 'Inventar' -Completed

$treffer | Export-Csv -LiteralPath $Ausgabe -Delimiter ';' -NoTypeInformation -Encoding UTF8
$excelBerichte = @($treffer | Where-Object { $_.Bericht -eq 'ja' -and $_.Typ -notin @('pbix', 'pbit') }).Count
$powerBi = @($treffer | Where-Object { $_.Bericht -eq 'Power BI' }).Count
Write-Host ("Gepruefte Dateien: {0}; Bericht-Excel: {1}; Power BI: {2}; unklar: {3}; Ausgabe: {4}" -f `
    $gezaehlt, $excelBerichte, $powerBi, @($treffer | Where-Object { $_.Bericht -eq 'unklar' }).Count, $Ausgabe)
