<#
.SYNOPSIS
    Fuehrt SapProbe gegen T76/100 aus, ohne bei jedem Lauf nach dem Passwort zu fragen.

.DESCRIPTION
    Holt das Passwort aus der DPAPI-Ablage (siehe SapCredential.ps1). Ist keine Ablage
    vorhanden, wird einmalig danach gefragt und der Lauf trotzdem ausgefuehrt; gespeichert
    wird nur auf ausdruecklichen Wunsch mit Set-SapPassword.

    Das Passwort steht nur als Umgebungsvariable des Kindprozesses zur Verfuegung und wird
    danach wieder entfernt.

.EXAMPLE
    .\.tmp_sap_probe\RunSapProbe.ps1 table-read AUFK 5 "AUFNR EQ '000001241804'"
    .\.tmp_sap_probe\RunSapProbe.ps1 system-info
#>
[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Argumente
)

$ErrorActionPreference = 'Stop'

$probe = Join-Path $PSScriptRoot 'bin\x86\Release\net48\SapProbe.exe'
if (-not (Test-Path -LiteralPath $probe)) {
    throw "SapProbe.exe fehlt unter '$probe'. Erst bauen: dotnet build .tmp_sap_probe\SapProbe.csproj -c Release -p:Platform=x86"
}

. (Join-Path $PSScriptRoot 'SapCredential.ps1')

$passwort = $null
try {
    $passwort = Get-SapPassword -System 'T76'
    Write-Host "Passwort aus der lokalen Ablage verwendet." -ForegroundColor DarkGray
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Yellow
    Write-Host "Einmalige Eingabe fuer diesen Lauf. Dauerhaft ablegen mit Set-SapPassword." -ForegroundColor DarkGray
    $sicher = Read-Host 'SAP-Passwort fuer KOI in T76/100' -AsSecureString
    $zeiger = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sicher)
    try {
        $passwort = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($zeiger)
    } finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($zeiger)
    }
}

try {
    $env:SAP_NCO_PASSWORD = $passwort
    & $probe --no-password-prompt @Argumente
    $code = $LASTEXITCODE
} finally {
    Remove-Item Env:\SAP_NCO_PASSWORD -ErrorAction SilentlyContinue
    $passwort = $null
}

Write-Host "Exitcode: $code"
exit $code
