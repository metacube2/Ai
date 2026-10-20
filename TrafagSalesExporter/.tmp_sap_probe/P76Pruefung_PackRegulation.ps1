# NUR LESEN in P76/100 (Auftrag Ingo 2026-10-20, Wunsch Florian Waechter: RoHS/REACH/PFAS in ZLO03).
# Zweiter Lauf: alle Merkmale der Klasse Z_PACK_REGULATION (P76 hat keine ZCOMP_STOFF), Texte, Wertelisten,
# Werte an Materialien und Zahl der zugeordneten Materialien (KSSK).
# Aufruf: .tmp_sap_probe\P76Pruefung_ZCOMP_Start.cmd (Passwort tippt Ingo, einmal, verdeckt, nirgends gespeichert).
$ErrorActionPreference = 'Continue'
$probe = Join-Path $PSScriptRoot 'bin\x86\Release\net48\SapProbe.exe'
$out = Join-Path $env:TEMP 'p76_pruefung_packreg'
New-Item -ItemType Directory -Path $out -Force | Out-Null
$log = Join-Path $out 'pruefung.log'
$g = @('--ashost', 'trasp76s.sap.trafag.com', '--client', '100', '--quiet', '--no-password-prompt')

$sec = Read-Host 'SAP-Passwort fuer KOI in P76/100 (nur lesen)' -AsSecureString
$ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec)
try {
    $env:SAP_NCO_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
    function P([string]$titel, [string[]]$a, [string]$datei) {
        "=== $titel ===" | Out-File -FilePath $log -Append -Encoding utf8
        $r = (& $probe @g @a 2>&1) | ForEach-Object { [string]$_ }
        if ($datei) { $r | Set-Content -Path (Join-Path $out $datei) -Encoding utf8; "Zeilen: $($r.Count) -> $datei" | Out-File -FilePath $log -Append -Encoding utf8 }
        else { $r | Out-File -FilePath $log -Append -Encoding utf8 }
        $r | Select-Object -First 40 | Write-Host
        # Nach dem ersten Fehler abbrechen: jeder weitere Lauf waere ein weiterer Anmeldeversuch (Sperrgefahr).
        if ($r -match 'ERROR') { "ABBRUCH nach Fehler, keine weiteren Anmeldungen." | Out-File -FilePath $log -Append -Encoding utf8; throw 'SapProbe-Fehler' }
        return $r
    }
    Remove-Item $log -ErrorAction SilentlyContinue
    $ksml = P 'KSML Z_PACK_REGULATION' @('table-read', 'KSML', '--fields', 'CLINT,POSNR,IMERK', '--where', "CLINT = '0000000489'", '--format', 'csv', '--rowcount', '50')
    $null = P 'KSSK Zuordnungen' @('table-read', 'KSSK', '--fields', 'OBJEK,MAFID,KLART,CLINT', '--where', "CLINT = '0000000489'", '--format', 'csv', '--rowcount', '60000') 'kssk.csv'
    $atinn = @()
    foreach ($z in $ksml) { if ($z -match '^\d{10},\d+,(\d{10})') { $atinn += $Matches[1] } }
    foreach ($a in $atinn) {
        $null = P "CABN $a" @('table-read', 'CABN', '--fields', 'ATINN,ATNAM,ATFOR,ANZST', '--where', "ATINN = '$a'", '--rowcount', '2')
        $null = P "CABNT $a" @('table-read', 'CABNT', '--fields', 'ATINN,SPRAS,ATBEZ', '--where', "ATINN = '$a'", '--rowcount', '5')
        $null = P "CAWN $a" @('table-read', 'CAWN', '--fields', 'ATINN,ATZHL,ATWRT', '--where', "ATINN = '$a'", '--rowcount', '30')
        $null = P "AUSP $a" @('table-read', 'AUSP', '--fields', 'OBJEK,ATINN,ATWRT,ATFLV,KLART', '--where', "ATINN = '$a'", '--format', 'csv', '--rowcount', '60000') "ausp_$a.csv"
    }
}
finally {
    Remove-Item Env:\SAP_NCO_PASSWORD -ErrorAction SilentlyContinue
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
}
"Fertig. Protokoll: $log"
