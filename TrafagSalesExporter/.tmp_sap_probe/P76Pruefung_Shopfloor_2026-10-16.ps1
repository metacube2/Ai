# NUR LESEN in P76/100 (Auftrag Ingo 2026-10-16 "alles transportiert, kannst du auf P lesen pruefen").
# Prueft nach dem Import von T76K912728, T76K912736, T76K912740, T76K912738:
# Transporte, Objektliste, Quelltext-Stand (ZM_OFFENE_KDAUF, MPC/DPC), Strukturen, Gateway-Fehlerprotokoll.
# Aufruf in der Claude-Code-Sitzung:
#   ! powershell -NoProfile -ExecutionPolicy Bypass -File .\.tmp_sap_probe\P76Pruefung_Shopfloor_2026-10-16.ps1
# Das Passwort wird einmal verdeckt abgefragt, nur fuer diese Laeufe in der Umgebung gehalten und nirgends gespeichert.
$ErrorActionPreference = 'Continue'
$probe = Join-Path $PSScriptRoot 'bin\x86\Release\net48\SapProbe.exe'
$out = Join-Path $env:TEMP 'p76_pruefung_shopfloor'
New-Item -ItemType Directory -Path $out -Force | Out-Null
$log = Join-Path $out 'pruefung.log'
$g = @('--ashost', 'travp762.sap.trafag.com', '--client', '100', '--quiet', '--no-password-prompt')

$sec = Read-Host 'SAP-Passwort fuer KOI in P76/100 (nur lesen)' -AsSecureString
$ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec)
try {
    $env:SAP_NCO_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
    function P([string]$titel, [string[]]$a) {
        "=== $titel ===" | Tee-Object -FilePath $log -Append
        (& $probe @g @a 2>&1) | ForEach-Object { [string]$_ } | Tee-Object -FilePath $log -Append
    }
    Remove-Item $log -ErrorAction SilentlyContinue
    $tr = "TRKORR IN ('T76K912728','T76K912736','T76K912738','T76K912740')"
    P 'E070 Transporte' @('table-read', 'E070', '--fields', 'TRKORR,TRFUNCTION,TRSTATUS,AS4USER,AS4DATE,AS4TIME', '--where', $tr, '--rowcount', '20')
    P 'E071 Objekte' @('table-read', 'E071', '--fields', 'TRKORR,PGMID,OBJECT,OBJ_NAME', '--where', "TRKORR IN ('T76K912737','T76K912739','T76K912741','T76K912729','T76K912738','T76K912740','T76K912736','T76K912728')", '--rowcount', '100')
    P 'REPOSRC Stand' @('table-read', 'REPOSRC', '--fields', 'PROGNAME,R3STATE,UDAT,UTIME,UNAM', '--where', "PROGNAME IN ('ZM_OFFENE_KDAUF','ZCL_ZPOWERBI_EINKAUF_MPC_EXT==CM001','ZCL_ZPOWERBI_EINKAUF_DPC_EXT==CM01W')", '--rowcount', '20')
    P 'DD02L Strukturen' @('table-read', 'DD02L', '--fields', 'TABNAME,AS4LOCAL,AS4DATE', '--where', "TABNAME IN ('ZSTR_SHOP_RM','ZSTR_SHOP_PROD','ZSTR_ZC23_APP','ZMM_UEB_KDAUF','ZPOWERBI_KDAUF')", '--rowcount', '20')
    P 'abap-read ZM_OFFENE_KDAUF' @('abap-read', 'ZM_OFFENE_KDAUF', '--out', (Join-Path $out 'ZM_OFFENE_KDAUF.abap'))
    P 'abap-read DPC GET_ENTITYSET' @('abap-read', 'ZCL_ZPOWERBI_EINKAUF_DPC_EXT==CM01W', '--out', (Join-Path $out 'DPC_CM01W.abap'))
    P 'abap-read MPC DEFINE' @('abap-read', 'ZCL_ZPOWERBI_EINKAUF_MPC_EXT==CM001', '--out', (Join-Path $out 'MPC_CM001.abap'))
    P 'Gateway-Fehlerprotokoll heute' @('table-read', '/IWFND/SU_ERRLOG', '--fields', 'TIMESTAMP,USERNAME,T100_MSGID,T100_MSGNO,ERROR_TEXT', '--where', 'TIMESTAMP > 20261016000000', '--rowcount', '30')
}
finally {
    Remove-Item Env:\SAP_NCO_PASSWORD -ErrorAction SilentlyContinue
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
}
"Fertig. Protokoll: $log"
