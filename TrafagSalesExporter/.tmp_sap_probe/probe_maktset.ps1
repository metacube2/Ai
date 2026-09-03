# OData-Probe fuer ZPOWERBI_EINKAUF_SRV/MAKTSet (Materialtext, SAP-Tabelle MAKT).
# Ingo hat MAKTSet auf travt762 (TEST) gefunden. Zwei offene Fragen:
#   1) Enthaelt MAKTSet wirklich den Materialtext (Feldname, z.B. Maktx) und in
#      welcher Sprache (MAKT ist ueber MATNR+SPRAS sprachabhaengig - ohne Sprachfilter
#      koennen mehrere Zeilen je Material zurueckkommen)?
#   2) Existiert MAKTSet auch auf travp762 (PROD)? Der produktive Loader
#      (PurchasingDataRefreshService) spricht travp762, nicht travt762 - ein nur auf
#      Test vorhandenes Set waere fuer den produktiven Full Load wirkungslos.
# Read-only: nur GET-Requests, keine Aenderung an SAP.
#
# Aufruf je System (in der Claude-Code-Session):
#   ! powershell -NoProfile -ExecutionPolicy Bypass -File .\.tmp_sap_probe\probe_maktset.ps1 -SapHost travt762.sap.trafag.com
#   ! powershell -NoProfile -ExecutionPolicy Bypass -File .\.tmp_sap_probe\probe_maktset.ps1 -SapHost travp762.sap.trafag.com
# Passwort wird interaktiv/maskiert abgefragt und nirgends gespeichert.

param(
  [string]$SapHost = 'travp762.sap.trafag.com',
  [int]$Port       = 8000,
  [string]$User    = 'KOI',
  [string]$Service = 'ZPOWERBI_EINKAUF_SRV',
  # Beispiel-Materialnummer aus einem echten Drilldown-Fall (BEPRO AG / 10.08.00),
  # nur fuer den gezielten Join-Test unten. Bei Bedarf ueberschreiben.
  [string]$SampleMatnr = 'B64880',
  # Optional: Passwort nicht-interaktiv uebergeben (z.B. fuer einen einmaligen
  # automatisierten Lauf). Wird NICHT geloggt/gespeichert, nur fuer den Basic-Auth-
  # Header verwendet. Ohne diesen Parameter bleibt der interaktive Read-Host-Weg aktiv.
  [string]$Pass = ''
)

$ErrorActionPreference = 'Stop'
$base = "http://$SapHost`:$Port/sap/opu/odata/sap/$Service"

if ([string]::IsNullOrEmpty($Pass)) {
  $sec  = Read-Host "SAP-Passwort fuer $User auf $SapHost" -AsSecureString
  $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec)
  $pass = [Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
  [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
} else {
  $pass = $Pass
}

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

# 1) Rohe Beispielzeilen ohne Filter/Select - zeigt alle Felder inkl. Sprachschluessel
#    und ob mehrere Zeilen je Material zurueckkommen (Sprachabhaengigkeit von MAKT).
Probe 'MAKTSet erste 5 Zeilen (alle Felder)' "$base/MAKTSet?`$top=5&`$format=json"

# 2) Gezielter Join-Test auf ein bekanntes Material aus dem Drilldown.
Probe "MAKTSet gefiltert auf Matnr eq '$SampleMatnr'" "$base/MAKTSet?`$format=json&`$filter=Matnr eq '$SampleMatnr'"

# 3) $metadata: Property-Liste des MAKT-EntityType (exakte Feldnamen, u.a. Sprachfeld
#    und Textfeld) und Pruefung, ob $top/$skip/$filter serverseitig wirken (wichtig
#    fuer den Loader: MARA001Set ignoriert Paging/Filter komplett, MAKTSet evtl. nicht).
Write-Host ""
Write-Host "=== `$metadata: Property-Liste des MAKT-EntityType ==="
try {
  $m = Invoke-WebRequest -Uri "$base/`$metadata" -Headers $headers -UseBasicParsing -TimeoutSec 60
  $content = $m.Content

  $typeMatch = [regex]::Match($content, '<EntityType Name="[^"]*MAKT[^"]*"[^>]*>.*?</EntityType>', 'Singleline')
  if ($typeMatch.Success) {
    $props = [regex]::Matches($typeMatch.Value, 'Name="([^"]+)" Type="Edm\.[^"]+"')
    foreach ($p in $props) { Write-Host ("  Property: {0}" -f $p.Groups[1].Value) }

    $keyMatch = [regex]::Match($typeMatch.Value, '<Key>.*?</Key>', 'Singleline')
    if ($keyMatch.Success) {
      Write-Host ""
      Write-Host "  Schluesselfelder (Key):"
      $keys = [regex]::Matches($keyMatch.Value, 'Name="([^"]+)"')
      foreach ($k in $keys) { Write-Host ("    Key: {0}" -f $k.Groups[1].Value) }
    }
  } else {
    Write-Host "  MAKT-EntityType im Metadata-Text nicht gefunden (Regex-Miss, ggf. Namensabweichung)."
  }
} catch {
  Write-Host "metadata-Abruf fehlgeschlagen: $($_.Exception.Message)"
}

Write-Host ""
Write-Host "=== Fertig. Ausgabe bitte an Claude zurueckgeben (beide Systeme, falls travt762 UND travp762 erreichbar sind). ==="
