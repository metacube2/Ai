# NUR LESEN in P76/100 (Auftrag Ingo 2026-10-16, Meldung Ann-Katrin: B63485 Bottom-Up MK 3100, Top-Down 8475 MK 0).
# Liest die Stuecklistensaetze von B63485 (ZPOWERBI_VC_TXT), den MK-Satz (ZMD04_CALC), die VKNR zu Typencode 8475
# und den Programmstand von Z_ZLO03_TURBO2. Ziel: belegen, ob die erste Fundstelle eine Textposition ist (FIX 12).
# Aufruf: .tmp_sap_probe\P76Pruefung_ZLO03_Start.cmd (Passwort tippt Ingo, einmal, verdeckt, nirgends gespeichert).
$ErrorActionPreference = 'Continue'
$probe = Join-Path $PSScriptRoot 'bin\x86\Release\net48\SapProbe.exe'
$out = Join-Path $env:TEMP 'p76_pruefung_zlo03'
New-Item -ItemType Directory -Path $out -Force | Out-Null
$log = Join-Path $out 'pruefung.log'
$g = @('--ashost', 'trasp76s.sap.trafag.com', '--client', '100', '--quiet', '--no-password-prompt')

$sec = Read-Host 'SAP-Passwort fuer KOI in P76/100 (nur lesen)' -AsSecureString
$ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec)
try {
    $env:SAP_NCO_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
    function P([string]$titel, [string[]]$a) {
        "=== $titel ===" | Tee-Object -FilePath $log -Append
        $r = (& $probe @g @a 2>&1) | ForEach-Object { [string]$_ }
        $r | Tee-Object -FilePath $log -Append
        # Nach dem ersten Fehler abbrechen: jeder weitere Lauf waere ein weiterer Anmeldeversuch (Sperrgefahr).
        if ($r -match 'ERROR') { "ABBRUCH nach Fehler, keine weiteren Anmeldungen." | Tee-Object -FilePath $log -Append; throw 'SapProbe-Fehler' }
    }
    Remove-Item $log -ErrorAction SilentlyContinue
    P 'ZMD04_CALC B63485' @('table-read', 'ZMD04_CALC', '--fields', 'MATNR,WERKS,MKMNG,OMKWR,LABST,EXKLUSIV,EXKL_COUNT,CALC_DATE,CALC_TIME', '--where', "MATNR LIKE '%B63485%'", '--rowcount', '20')
    P 'ZPOWERBI_VC_TXT KOMPNR B63485' @('table-read', 'ZPOWERBI_VC_TXT', '--fields', 'ID,STUFE,MATNR,KOMPNR,MENGE,MENGENEINHEIT,POSTYP,BAUGRUPPE', '--where', "KOMPNR LIKE '%B63485%'", '--rowcount', '500')
    P 'MARA Typencode 8475' @('table-read', 'MARA', '--fields', 'MATNR,MTART,MSTAE,LVORM', '--where', "ZZTYP_F4 = '8475'", '--rowcount', '500')
    P 'REPOSRC Z_ZLO03_TURBO2' @('table-read', 'REPOSRC', '--fields', 'PROGNAME,R3STATE,UDAT,UTIME,UNAM', '--where', "PROGNAME = 'Z_ZLO03_TURBO2'", '--rowcount', '5')
}
finally {
    Remove-Item Env:\SAP_NCO_PASSWORD -ErrorAction SilentlyContinue
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
}
"Fertig. Protokoll: $log"
