# OData-Probe fuer travp762 (ZPOWERBI_EINKAUF_SRV)
# Prueft, ob die neuen Properties Bstyp/Bsart (EKKO) und Elikz/Ktmng (EKPO)
# im Gateway-Modell vorhanden und gefuellt sind.
# Aufruf (in der Claude-Code-Session):
#   ! powershell -NoProfile -ExecutionPolicy Bypass -File .\.tmp_sap_probe\probe_travp762_odata.ps1
# Passwort wird interaktiv/maskiert abgefragt und nirgends gespeichert.

param(
  [string]$SapHost = 'travp762.sap.trafag.com',
  [int]$Port       = 8000,
  [string]$User    = 'KOI',
  [string]$Service = 'ZPOWERBI_EINKAUF_SRV'
)

$ErrorActionPreference = 'Stop'
$base = "http://$SapHost`:$Port/sap/opu/odata/sap/$Service"

$sec  = Read-Host "SAP-Passwort fuer $User" -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec)
$pass = [Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)

$token   = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("$User`:$pass"))
$headers = @{ Authorization = "Basic $token" }

function Probe($label, $url) {
  Write-Host ""
  Write-Host "=== $label ==="
  Write-Host $url
  try {
    $r = Invoke-WebRequest -Uri $url -Headers $headers -UseBasicParsing -TimeoutSec 60
    Write-Host "HTTP $($r.StatusCode)"
    Write-Host $r.Content
  } catch {
    $resp = $_.Exception.Response
    if ($resp) {
      Write-Host "HTTP $([int]$resp.StatusCode) $($resp.StatusDescription)"
      try {
        $sr = New-Object IO.StreamReader($resp.GetResponseStream())
        Write-Host $sr.ReadToEnd()
      } catch {}
    } else {
      Write-Host "FEHLER: $($_.Exception.Message)"
    }
  }
}

Write-Host "Ziel: $base  (User $User)"

Probe 'EKKO neue Felder' "$base/EKKOSet?`$top=1&`$format=json&`$select=Ebeln,Bstyp,Bsart,Konnr,Waers,Wkurs"
Probe 'EKPO neue Felder' "$base/EKPOSet?`$top=1&`$format=json&`$select=Ebeln,Ebelp,Elikz,Ktmng"

# $metadata-Check: sind die Property-Namen im Modell?
Write-Host ""
Write-Host "=== `$metadata Property-Check ==="
try {
  $m = Invoke-WebRequest -Uri "$base/`$metadata" -Headers $headers -UseBasicParsing -TimeoutSec 60
  foreach ($p in 'Bstyp','Bsart','Elikz','Ktmng') {
    $hit = ($m.Content -match "Name=`"$p`"")
    Write-Host ("  {0,-8} im Modell: {1}" -f $p, $hit)
  }
} catch {
  Write-Host "metadata-Abruf fehlgeschlagen: $($_.Exception.Message)"
}

Write-Host ""
Write-Host "=== Fertig. Ausgabe bitte an Claude zurueckgeben. ==="
