# NUR LESEN in P76/100 (Auftrag Ingo 2026-10-20, Wunsch Florian Waechter: RoHS/REACH/PFAS in ZLO03).
# Prueft, ob Klasse ZCOMP_STOFF und die Merkmale ZCOMP_* in P76 existieren und wie viele Materialien Werte haben.
# Aufruf: .tmp_sap_probe\P76Pruefung_ZCOMP_Start.cmd (Passwort tippt Ingo, einmal, verdeckt, nirgends gespeichert).
$ErrorActionPreference = 'Continue'
$probe = Join-Path $PSScriptRoot 'bin\x86\Release\net48\SapProbe.exe'
$out = Join-Path $env:TEMP 'p76_pruefung_zcomp'
New-Item -ItemType Directory -Path $out -Force | Out-Null
$log = Join-Path $out 'pruefung.log'
$g = @('--ashost', 'trasp76s.sap.trafag.com', '--client', '100', '--quiet', '--no-password-prompt')

$sec = Read-Host 'SAP-Passwort fuer KOI in P76/100 (nur lesen)' -AsSecureString
$ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec)
try {
    $env:SAP_NCO_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
    function P([string]$titel, [string[]]$a, [string]$datei) {
        "=== $titel ===" | Tee-Object -FilePath $log -Append
        $r = (& $probe @g @a 2>&1) | ForEach-Object { [string]$_ }
        if ($datei) { $r | Set-Content -Path (Join-Path $out $datei) -Encoding utf8; "Zeilen: $($r.Count) -> $datei" | Tee-Object -FilePath $log -Append }
        else { $r | Tee-Object -FilePath $log -Append }
        # Nach dem ersten Fehler abbrechen: jeder weitere Lauf waere ein weiterer Anmeldeversuch (Sperrgefahr).
        if ($r -match 'ERROR') { "ABBRUCH nach Fehler, keine weiteren Anmeldungen." | Tee-Object -FilePath $log -Append; throw 'SapProbe-Fehler' }
        return $r
    }
    Remove-Item $log -ErrorAction SilentlyContinue
    $null = P 'KLAH Klassen' @('table-read', 'KLAH', '--fields', 'CLINT,KLART,CLASS', '--where', "CLASS IN ('ZCOMP_STOFF','Z_PACK_REGULATION','ZPPWR_PACKMITTEL')", '--rowcount', '10')
    $cabn = P 'CABN Merkmale' @('table-read', 'CABN', '--fields', 'ATINN,ATNAM,ATFOR', '--where', "ATNAM LIKE 'ZCOMP%' OR ATNAM IN ('REACHCOM','PFAS')", '--format', 'csv', '--rowcount', '50')
    $atinn = @()
    foreach ($z in $cabn) { if ($z -match '^(\d{10}),(ZCOMP_(ROHS|REACH|PFAS)_(STATUS|DATE)|REACHCOM|PFAS),') { $atinn += $Matches[1] } }
    "ATINN fuer AUSP: $($atinn -join ' ')" | Tee-Object -FilePath $log -Append
    foreach ($a in $atinn) {
        $null = P "CAWN $a" @('table-read', 'CAWN', '--fields', 'ATINN,ATZHL,ATWRT', '--where', "ATINN = '$a'", '--rowcount', '30')
        $null = P "AUSP $a" @('table-read', 'AUSP', '--fields', 'OBJEK,ATINN,ATWRT,ATFLV,KLART', '--where', "ATINN = '$a'", '--format', 'csv', '--rowcount', '60000') "ausp_$a.csv"
    }
    $null = P 'REPOSRC Z_ZLO03_TURBO2' @('table-read', 'REPOSRC', '--fields', 'PROGNAME,R3STATE,UDAT,UTIME,UNAM', '--where', "PROGNAME = 'Z_ZLO03_TURBO2'", '--rowcount', '5')
}
finally {
    Remove-Item Env:\SAP_NCO_PASSWORD -ErrorAction SilentlyContinue
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
}
"Fertig. Protokoll: $log"
