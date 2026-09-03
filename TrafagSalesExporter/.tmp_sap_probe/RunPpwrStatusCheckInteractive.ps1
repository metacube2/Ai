# Read-only Statuspruefung der PPWR-/Compliance-Klassifizierung in T76.
#
# Zweck: belegen, welche Merkmale (CABN) und welche Klassen (KLAH/KSML) in den
# Mandanten 090 und 100 tatsaechlich vorhanden sind. Das Anlageprotokoll vom
# 13.08.2026 meldete "FERTIG", die Klassen fehlen laut Ingo aber. Diese Messung
# ersetzt die Selbstmeldung des Reports durch eine Tabellenabfrage.
#
# Es wird ausschliesslich gelesen. Kein abap-write, kein Commit, kein P76.

param(
    # Ohne Abschlusspause laufen, etwa wenn das Skript aus einer bestehenden
    # Sitzung heraus gestartet wird und niemand ein Fenster schliessen muss.
    [switch] $NoPause
)

$ErrorActionPreference = 'Continue'

$exe = Join-Path $PSScriptRoot 'bin\x86\Release\net48\SapProbe.exe'
$log = Join-Path $PSScriptRoot 'ppwr_status_100.log'

if (-not (Test-Path -LiteralPath $exe)) {
    Write-Host "SapProbe fehlt: $exe" -ForegroundColor Red
    if (-not $NoPause) { Read-Host 'Enter zum Schliessen' }
    exit 2
}

# Ist das Kennwort schon in der Umgebung hinterlegt, nicht erneut fragen.
# So laesst sich die Messung auch ohne Eingabeaufforderung wiederholen.
$bstr = [IntPtr]::Zero
if ([string]::IsNullOrEmpty($env:SAP_NCO_PASSWORD) -and
    [string]::IsNullOrEmpty($env:SAP_T76_PASSWORD)) {
    $securePassword = Read-Host 'Passwort fuer SAP T76 Benutzer KOI' -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    $env:SAP_NCO_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    $script:PasswordWasPrompted = $true
}
else {
    Write-Host 'Kennwort aus der Umgebung uebernommen, keine Eingabe noetig.'
    $script:PasswordWasPrompted = $false
}

try {
    Start-Transcript -LiteralPath $log -Force | Out-Null

    function Invoke-Probe {
        param(
            [string]   $Client,
            [string]   $Titel,
            [string[]] $ProbeArgs
        )
        Write-Host ''
        Write-Host ">>> [$Client] $Titel" -ForegroundColor Cyan
        Write-Host ">>> SapProbe --client $Client $($ProbeArgs -join ' ')"
        try {
            & $exe --client $Client --quiet @ProbeArgs 2>&1 |
                ForEach-Object { Write-Host $_ }
            Write-Host "<<< Exit $LASTEXITCODE"
        }
        catch {
            Write-Host "<<< FEHLER: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }

    foreach ($client in @('090', '100')) {

        Write-Host ''
        Write-Host ('=' * 78)
        Write-Host "MANDANT $client"
        Write-Host ('=' * 78)

        Invoke-Probe $client 'Verbindung und System' @('system-info')

        # --- Merkmale -------------------------------------------------------
        # CABN traegt die Merkmalsdefinition. Erwartung laut Anlageprotokoll:
        # 9 Stueck ZPPWR_* und 12 Stueck ZCOMP_*, zusammen 21.
        Invoke-Probe $client 'Merkmale ZPPWR* in CABN' @(
            'table-read', 'CABN',
            '--fields', 'ATINN,ATNAM,ATFOR,ANZST,ANZDZ',
            '--where', "ATNAM LIKE 'ZPPWR%'",
            '--rowcount', '100', '--format', 'csv')

        Invoke-Probe $client 'Merkmale ZCOMP* in CABN' @(
            'table-read', 'CABN',
            '--fields', 'ATINN,ATNAM,ATFOR,ANZST,ANZDZ',
            '--where', "ATNAM LIKE 'ZCOMP%'",
            '--rowcount', '100', '--format', 'csv')

        # --- Klassen --------------------------------------------------------
        # Feldliste zuerst ausgeben, damit bei abweichenden DDIC-Namen nicht
        # geraten werden muss. Router-Regel: keine Feldnamen erfinden.
        Invoke-Probe $client 'DDIC-Felder von KLAH' @('table-fields', 'KLAH')

        Invoke-Probe $client 'Klassen Z* in KLAH (Klassenart 001)' @(
            'table-read', 'KLAH',
            '--fields', 'CLINT,KLART,CLASS',
            '--where', "CLASS LIKE 'Z%'",
            '--rowcount', '200', '--format', 'csv')

        # --- Klasse-Merkmal-Zuordnung ---------------------------------------
        Invoke-Probe $client 'DDIC-Felder von KSML' @('table-fields', 'KSML')
    }

    # --- Syntaxpruefung des geaenderten Reports ------------------------------
    # Prueft den lokalen Quelltext gegen das System, OHNE ihn zu schreiben.
    # Damit sind KLAH/KSML-Feldnamen und die neue Ruecklesephase belegt statt
    # behauptet (Router-Vorrangregel 5).
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $abap     = Join-Path $repoRoot 'docs\abap\ZPPWR_CLASS_SETUP.abap'

    if (Test-Path -LiteralPath $abap) {
        Invoke-Probe '100' 'Syntaxpruefung ZPPWR_CLASS_SETUP aus lokaler Datei' @(
            'abap-check', 'ZPPWR_CLASS_SETUP', '--source-file', $abap)
    }
    else {
        Write-Host "ABAP-Datei nicht gefunden: $abap" -ForegroundColor Yellow
    }
}
finally {
    if ($bstr -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
    # Nur aufraeumen, was dieses Skript selbst gesetzt hat. Ein vom Aufrufer
    # bereitgestelltes Kennwort bleibt unangetastet.
    if ($script:PasswordWasPrompted) {
        Remove-Item Env:SAP_NCO_PASSWORD -ErrorAction SilentlyContinue
    }
    try { Stop-Transcript | Out-Null } catch { }
}

Write-Host ''
Write-Host "Statuspruefung beendet. Protokoll: $log"
if (-not $NoPause) { Read-Host 'Enter zum Schliessen' }
