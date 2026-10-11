# OData-Probe gegen das PRODUKTIVsystem travp762 (ZPOWERBI_EINKAUF_SRV)
#
# Prueft drei Dinge:
#   1. STANDARDPREIS CH/AT: Gibt es mbewSet und ist STPRS (Standardpreis) gefuellt?
#      -> Wenn ja, koennen wir die 40'292 ZSCHWEIZ-Zeilen mit Kosten fuellen,
#         ohne dass SAP etwas Neues bauen muss.
#   2. JOURNAL: Gibt es bkpfSet / bsisSet bereits?
#      -> Wenn ja, ist die Spezifikation fuer ein neues FinanzJournalSet
#         moeglicherweise ueberfluessig.
#   3. GEGENPROBE TESTSYSTEM: In der Produktiv-DB ist fuer ZSCHWEIZ travt762
#      (Test!) hinterlegt. Wir zaehlen die 2026er-Umsatzzeilen auf PROD gegen,
#      um zu sehen, ob die fehlenden 2026-Daten am falschen System liegen.
#
# Aufruf (in der Claude-Code-Session):
#   ! powershell -NoProfile -ExecutionPolicy Bypass -File .\.tmp_sap_probe\probe_travp762_stprs.ps1
#
# Das Passwort wird maskiert abgefragt und nirgends gespeichert.

param(
  [string]$SapHost = 'travp762.sap.trafag.com',
  [int]$Port       = 8000,
  [string]$User    = 'KOI',
  [string]$Service = 'ZPOWERBI_EINKAUF_SRV'
)

$ErrorActionPreference = 'Continue'
$base = "http://$SapHost`:$Port/sap/opu/odata/sap/$Service"

$sec  = Read-Host "SAP-Passwort fuer $User (PROD $SapHost)" -AsSecureString
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
    $r = Invoke-WebRequest -Uri $url -Headers $headers -UseBasicParsing -TimeoutSec 90
    Write-Host "HTTP $($r.StatusCode)"
    $c = $r.Content
    if ($c.Length -gt 4000) { $c = $c.Substring(0, 4000) + " ...[gekuerzt]" }
    Write-Host $c
  } catch {
    $resp = $_.Exception.Response
    if ($resp) {
      Write-Host "HTTP $([int]$resp.StatusCode) $($resp.StatusDescription)"
      try {
        $sr = New-Object IO.StreamReader($resp.GetResponseStream())
        $body = $sr.ReadToEnd()
        if ($body.Length -gt 1500) { $body = $body.Substring(0, 1500) + " ...[gekuerzt]" }
        Write-Host $body
      } catch {}
    } else {
      Write-Host "FEHLER: $($_.Exception.Message)"
    }
  }
}

Write-Host "Ziel: $base   (User $User)"
Write-Host "ACHTUNG: das ist das PRODUKTIVsystem (travp), nicht travt."

# --- 1. Welche EntitySets gibt es wirklich auf PROD? ---
Write-Host ""
Write-Host "=== EntitySets + Property-Check aus `$metadata ==="
try {
  $m = Invoke-WebRequest -Uri "$base/`$metadata" -Headers $headers -UseBasicParsing -TimeoutSec 120
  $sets = [regex]::Matches($m.Content, 'EntitySet Name="([^"]+)"') |
          ForEach-Object { $_.Groups[1].Value } | Sort-Object
  Write-Host ("Anzahl EntitySets: {0}" -f $sets.Count)
  foreach ($want in 'mbewSet','bkpfSet','bsisSet','bsegSet','marcSet','MARCSet','VBRPSet','FinanzdataSchweizOeSet') {
    $hit = $sets -contains $want
    Write-Host ("  {0,-24} vorhanden: {1}" -f $want, $hit)
  }
  Write-Host ""
  Write-Host "Property-Check (Standardpreis-relevant):"
  foreach ($p in 'Stprs','Matnr','Bwkey','Peinh','Vprsv','Waers','Verpr','Bklas') {
    $hit = ($m.Content -match "Name=`"$p`"")
    Write-Host ("  {0,-8} im Modell: {1}" -f $p, $hit)
  }
  Write-Host ""
  Write-Host "Alle EntitySets auf PROD:"
  Write-Host ("  " + ($sets -join ', '))
} catch {
  Write-Host "metadata-Abruf fehlgeschlagen: $($_.Exception.Message)"
}

# --- 2. Standardpreis: ist STPRS wirklich gefuellt? ---
Probe 'MBEW Standardpreis (Stichprobe)' "$base/mbewSet?`$top=5&`$format=json"

# --- 3. Journal: sind BKPF/BSIS schon da? ---
Probe 'BKPF Belegkopf (Stichprobe)' "$base/bkpfSet?`$top=1&`$format=json"
Probe 'BSIS Sachkonto-Einzelposten (Stichprobe)' "$base/bsisSet?`$top=1&`$format=json"

# --- 4. Gegenprobe: hat PROD die 2026er-Umsatzdaten, die auf travt fehlen? ---
Probe 'Umsatz CH/AT 2026 auf PROD (Stichprobe)' "$base/FinanzdataSchweizOeSet?`$top=3&`$format=json&`$filter=Gjahr eq '2026'"

Write-Host ""
Write-Host "=== Fertig. Bitte die komplette Ausgabe an Claude zurueckgeben. ==="
