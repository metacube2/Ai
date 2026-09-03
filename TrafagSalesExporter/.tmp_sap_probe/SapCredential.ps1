<#
.SYNOPSIS
    Legt das SAP-Passwort fuer das Testsystem T76 einmalig verschluesselt ab.

.DESCRIPTION
    SapProbe fragt sonst bei jedem Lauf neu nach dem Passwort. Das kostet bei einer
    Analysesitzung schnell ein Dutzend Unterbrechungen.

    Das Passwort wird mit der Windows-Datenschutz-API (DPAPI) verschluesselt. Der
    Schluessel haengt am Windows-Benutzerkonto und am Rechner: Die Datei ist auf einem
    anderen Rechner oder unter einem anderen Konto wertlos. Sie liegt bewusst unter
    %LOCALAPPDATA% und nicht im Repository.

    ABSICHTLICHE GRENZE: Nur das Testsystem T76 wird unterstuetzt. Fuer das
    Produktivsystem P76 wird nichts gespeichert, dort bleibt es bei der Eingabe pro Lauf.
    Diese Grenze ist hart im Code verankert und nicht ueber Parameter aufhebbar.

.EXAMPLE
    . .\.tmp_sap_probe\SapCredential.ps1
    Set-SapPassword            # einmalig, fragt verdeckt nach dem Passwort
    Test-SapPassword           # prueft, ob eine Ablage existiert
    Remove-SapPassword         # loescht die Ablage wieder
#>

$script:SapCredentialSystem = 'T76'
$script:SapCredentialPath = Join-Path $env:LOCALAPPDATA 'TrafagSap\t76_credential.xml'

function Get-SapCredentialPath {
    return $script:SapCredentialPath
}

function Set-SapPassword {
    <#
    .SYNOPSIS
        Fragt das Passwort einmal ab und legt es DPAPI-verschluesselt ab.
    #>
    [CmdletBinding()]
    param(
        [string]$Benutzer = $env:USERNAME
    )

    $ordner = Split-Path -Parent $script:SapCredentialPath
    if (-not (Test-Path $ordner)) {
        New-Item -ItemType Directory -Path $ordner -Force | Out-Null
    }

    Write-Host "Passwort fuer $Benutzer im System $($script:SapCredentialSystem), Mandant 100." -ForegroundColor Yellow
    Write-Host "Es wird mit der Windows-Datenschutz-API verschluesselt und ist nur fuer" -ForegroundColor DarkGray
    Write-Host "dieses Windows-Konto auf diesem Rechner lesbar." -ForegroundColor DarkGray

    $sicher = Read-Host "SAP-Passwort" -AsSecureString
    if (-not $sicher -or $sicher.Length -eq 0) {
        throw "Kein Passwort eingegeben, es wurde nichts gespeichert."
    }

    # Plausibilitaetspruefung. Am 2026-09-03 sind beim Einfuegen 99 Zeichen mit
    # Leerzeichen in die Ablage geraten; der erste RFC-Versuch damit war eine
    # Fehlanmeldung. Wiederholte Fehlanmeldungen sperren den SAP-Benutzer, deshalb
    # wird Unsinn hier abgefangen statt ihn an SAP zu schicken.
    $zeiger = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sicher)
    try {
        $klartext = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($zeiger)
    } finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($zeiger)
    }

    if ($klartext.Length -gt 40) {
        $klartext = $null
        throw "Die Eingabe ist $($sicher.Length) Zeichen lang. Das ist kein Passwort, sondern vermutlich versehentlich eingefuegter Text. Es wurde nichts gespeichert."
    }
    if ($klartext -match '\s') {
        $klartext = $null
        throw "Die Eingabe enthaelt Leerzeichen oder Zeilenumbrueche. Bitte das Passwort tippen statt einfuegen. Es wurde nichts gespeichert."
    }
    $klartext = $null

    [pscustomobject]@{
        System   = $script:SapCredentialSystem
        Mandant  = '100'
        Benutzer = $Benutzer
        Passwort = $sicher
        Angelegt = (Get-Date)
    } | Export-Clixml -Path $script:SapCredentialPath

    Write-Host "Gespeichert unter $($script:SapCredentialPath)." -ForegroundColor Green
    Write-Host "Loeschen jederzeit mit Remove-SapPassword." -ForegroundColor DarkGray
}

function Test-SapPassword {
    if (Test-Path $script:SapCredentialPath) {
        $daten = Import-Clixml -Path $script:SapCredentialPath
        [pscustomobject]@{
            Vorhanden = $true
            System    = $daten.System
            Benutzer  = $daten.Benutzer
            Angelegt  = $daten.Angelegt
            Pfad      = $script:SapCredentialPath
        }
    } else {
        [pscustomobject]@{ Vorhanden = $false; Pfad = $script:SapCredentialPath }
    }
}

function Get-SapPassword {
    <#
    .SYNOPSIS
        Liefert das Klartextpasswort fuer den aufrufenden Prozess.
    .PARAMETER System
        Muss T76 sein. Jeder andere Wert wird abgelehnt.
    #>
    [CmdletBinding()]
    param(
        [string]$System = 'T76'
    )

    if ($System -ne $script:SapCredentialSystem) {
        throw "Fuer '$System' wird bewusst kein Passwort gespeichert. Nur $($script:SapCredentialSystem) ist vorgesehen; fuer P76 bitte pro Lauf eingeben."
    }

    if (-not (Test-Path $script:SapCredentialPath)) {
        throw "Keine Ablage gefunden. Einmalig 'Set-SapPassword' ausfuehren."
    }

    $daten = Import-Clixml -Path $script:SapCredentialPath
    if ($daten.System -ne $script:SapCredentialSystem) {
        throw "Die Ablage gehoert zu System '$($daten.System)' und nicht zu $($script:SapCredentialSystem)."
    }

    # SecureString nur fuer die Dauer des Aufrufs in Klartext wandeln.
    $zeiger = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($daten.Passwort)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($zeiger)
    } finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($zeiger)
    }
}

function Remove-SapPassword {
    if (Test-Path $script:SapCredentialPath) {
        Remove-Item -Path $script:SapCredentialPath -Force
        Write-Host "Ablage geloescht." -ForegroundColor Green
    } else {
        Write-Host "Es war nichts gespeichert." -ForegroundColor DarkGray
    }
}
