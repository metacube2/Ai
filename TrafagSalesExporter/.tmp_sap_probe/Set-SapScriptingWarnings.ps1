<#
.SYNOPSIS
    Schaltet die Warnfenster von SAP GUI Scripting ab oder wieder ein.

.DESCRIPTION
    Mit WarnOnAttach = 1 und WarnOnConnection = 1 fragt SAP GUI bei jedem Anhaengen eines
    Skripts nach. Bei einer laengeren Analysesitzung sind das Hunderte Klicks, und
    unbeaufsichtigtes Arbeiten ist unmoeglich.

    Dieses Skript aendert ausschliesslich die beiden Warnungen und optional die
    Scripting-Freigabe selbst. Es sind keine Administratorrechte noetig, alles liegt unter
    HKEY_CURRENT_USER.

    SICHERHEITSHINWEIS: Ohne die Warnungen haengt sich jedes Skript ohne Rueckfrage an eine
    angemeldete SAP-Sitzung an, auch an eine produktive. Deshalb am Ende der Arbeit wieder
    einschalten.

.PARAMETER Aus
    Schaltet beide Warnungen ab. Scripting bleibt eingeschaltet.

.PARAMETER Ein
    Schaltet beide Warnungen ein und setzt zusaetzlich UserScripting auf 0, also den
    vollstaendigen Ausgangszustand.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .tmp_sap_probe\Set-SapScriptingWarnings.ps1 -Aus
    powershell -ExecutionPolicy Bypass -File .tmp_sap_probe\Set-SapScriptingWarnings.ps1 -Ein
#>
[CmdletBinding(DefaultParameterSetName = 'Zeigen')]
param(
    [Parameter(ParameterSetName = 'Aus')]
    [switch]$Aus,

    [Parameter(ParameterSetName = 'Ein')]
    [switch]$Ein
)

$ErrorActionPreference = 'Stop'
$pfad = 'HKCU:\Software\SAP\SAPGUI Front\SAP Frontend Server\Security'

if (-not (Test-Path -LiteralPath $pfad)) {
    throw "Registrierungsschluessel nicht gefunden: $pfad. Ist SAP GUI installiert?"
}

function Show-Stand {
    param([string]$Titel)
    $w = Get-ItemProperty -LiteralPath $pfad
    Write-Host $Titel -ForegroundColor Cyan
    Write-Host ("  UserScripting    : {0}" -f $w.UserScripting)
    Write-Host ("  WarnOnAttach     : {0}" -f $w.WarnOnAttach)
    Write-Host ("  WarnOnConnection : {0}" -f $w.WarnOnConnection)
}

Show-Stand 'Vorher:'

if ($Aus) {
    Set-ItemProperty -LiteralPath $pfad -Name 'UserScripting'    -Value 1
    Set-ItemProperty -LiteralPath $pfad -Name 'WarnOnAttach'     -Value 0
    Set-ItemProperty -LiteralPath $pfad -Name 'WarnOnConnection' -Value 0
    Write-Host ''
    Show-Stand 'Nachher, Warnungen abgeschaltet:'
    Write-Host ''
    Write-Host 'Am Ende der SAP-Arbeit bitte mit -Ein zuruecksetzen.' -ForegroundColor Yellow
}
elseif ($Ein) {
    Set-ItemProperty -LiteralPath $pfad -Name 'WarnOnAttach'     -Value 1
    Set-ItemProperty -LiteralPath $pfad -Name 'WarnOnConnection' -Value 1
    Set-ItemProperty -LiteralPath $pfad -Name 'UserScripting'    -Value 0
    Write-Host ''
    Show-Stand 'Nachher, Ausgangszustand wiederhergestellt:'
}
else {
    Write-Host ''
    Write-Host 'Nichts geaendert. Aufruf mit -Aus oder -Ein.' -ForegroundColor DarkGray
}

Write-Host ''
Write-Host 'Hinweis: SAP GUI liest die Einstellung beim Verbindungsaufbau. Wirken die' -ForegroundColor DarkGray
Write-Host 'Warnungen weiter, hilft ein Neustart von SAP Logon samt aller Sitzungen.' -ForegroundColor DarkGray
