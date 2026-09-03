# OData-Probe fuer travp762 (ZPOWERBI_EINKAUF_SRV)
# Prueft, ob und wo ein Materialtext (SAP MAKT-MAKTX) fuer den Einkauf-Materialstamm
# verfuegbar ist - Grundlage fuer "Materialtext (MAKTX aus MARA) im Drilldown
# Lieferant > Warengruppe > Material anzeigen".
# Read-only: nur GET-Requests, keine Aenderung an SAP.
# Aufruf (in der Claude-Code-Session):
#   ! powershell -NoProfile -ExecutionPolicy Bypass -File .\.tmp_sap_probe\probe_travp762_maktx.ps1
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

# 1) Ein Beispieldatensatz aus MARA001Set OHNE $select, damit alle heute
#    tatsaechlich gelieferten Felder sichtbar sind (Matnr/Mstae/Matkl bekannt,
#    Frage: liefert das Set daneben schon einen Textfeld-Wert wie Maktx?).
Probe 'MARA001Set Beispielzeile (alle Felder)' "$base/MARA001Set?`$top=1&`$format=json"

# 2) $metadata-Rohtext holen und die Property-Liste des MARA001-EntityType
#    ausgeben, damit Claude ALLE dort deklarierten Feldnamen sieht (nicht nur
#    geraten wird). Zusaetzlich gezielter Substring-Check auf gaengige
#    Materialtext-Feldnamen in der GESAMTEN Metadaten-Datei (andere Sets
#    koennten den Text unter eigenem Namen fuehren, z.B. ein MAKT-Set).
Write-Host ""
Write-Host "=== `$metadata: Property-Liste des MARA001-EntityType ==="
try {
  $m = Invoke-WebRequest -Uri "$base/`$metadata" -Headers $headers -UseBasicParsing -TimeoutSec 60
  $content = $m.Content

  # EntityType-Block finden, dessen Name "MARA001" enthaelt (Name variiert ggf. leicht
  # gegenueber dem EntitySet-Namen, deshalb ueber Regex statt exaktem String).
  $typeMatch = [regex]::Match($content, '<EntityType Name="[^"]*MARA001[^"]*"[^>]*>.*?</EntityType>', 'Singleline')
  if ($typeMatch.Success) {
    $props = [regex]::Matches($typeMatch.Value, 'Name="([^"]+)" Type="Edm\.[^"]+"')
    foreach ($p in $props) { Write-Host ("  Property: {0}" -f $p.Groups[1].Value) }
  } else {
    Write-Host "  MARA001-EntityType im Metadata-Text nicht gefunden (Regex-Miss, ggf. Namensabweichung)."
  }

  Write-Host ""
  Write-Host "=== Substring-Suche nach Materialtext-Kandidaten in der GESAMTEN `$metadata ==="
  foreach ($candidate in 'Maktx','MAKTX','Maktg','Bezei','MatBez','MaterialText','MaterialDescription','ArtikelBez') {
    $hit = $content -match [regex]::Escape($candidate)
    Write-Host ("  {0,-20} irgendwo im Modell: {1}" -f $candidate, $hit)
  }

  Write-Host ""
  Write-Host "=== Alle EntitySet-Namen (falls ein eigenes MAKT-Set existiert) ==="
  $sets = [regex]::Matches($content, '<EntitySet Name="([^"]+)"')
  foreach ($s in $sets) { Write-Host ("  EntitySet: {0}" -f $s.Groups[1].Value) }
} catch {
  Write-Host "metadata-Abruf fehlgeschlagen: $($_.Exception.Message)"
}

Write-Host ""
Write-Host "=== Fertig. Ausgabe bitte an Claude zurueckgeben. ==="
