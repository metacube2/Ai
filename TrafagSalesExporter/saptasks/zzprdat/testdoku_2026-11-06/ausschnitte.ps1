# Erzeugt vergroesserbare Ausschnitte aus den Vollbildern (unveraendert, nur zugeschnitten).
# Aufruf aus der Repository-Wurzel: powershell -ExecutionPolicy Bypass -File saptasks\zzprdat\testdoku_2026-11-06\ausschnitte.ps1
Add-Type -AssemblyName System.Drawing
$dir = Join-Path $PSScriptRoot 'bilder'
$out = Join-Path $dir 'ausschnitt'
New-Item -ItemType Directory -Force $out | Out-Null

# Bereiche in Originalpixeln (Bildschirm 3440 x 1368): x, y, Breite, Hoehe
$kopfFeld = @(0, 190, 1720, 470)
$termine  = @(0, 190, 1310, 775)
$warnung  = @(0, 1290, 1720, 78)

$auftraege = @(
  @('06_co01_trafag_daten_vorher_leer', $kopfFeld),
  @('08_co01_freigegeben_status_frei', $kopfFeld),
  @('11_co03_trafag_daten_nachher_27112026', $kopfFeld),
  @('13_co03_trafag_daten_nach_reiterwechsel', $kopfFeld),
  @('16_co02_trafag_daten_vor_sichern', $kopfFeld),
  @('19_co03_trafag_daten_unveraendert', $kopfFeld),
  @('02_co01_kopf_eingabe', $termine),
  @('12_co03_reiter_allgemein', $termine),
  @('14_co02_eckendtermin_neu_eingetragen', $termine),
  @('18_co03_allgemein_eckendtermin_neu', $termine),
  @('15_co02_warnung_kein_arbeitstag', $warnung),
  @('09_co01_gesichert_1241851', $warnung),
  @('17_co02_gesichert', $warnung)
)

foreach ($a in $auftraege) {
  $name = $a[0]; $r = $a[1]
  $src = [System.Drawing.Bitmap]::FromFile((Join-Path $dir ($name + '.png')))
  $rect = New-Object System.Drawing.Rectangle($r[0], $r[1], $r[2], $r[3])
  $crop = $src.Clone($rect, $src.PixelFormat)
  $crop.Save((Join-Path $out ($name + '.png')), [System.Drawing.Imaging.ImageFormat]::Png)
  $crop.Dispose(); $src.Dispose()
  Write-Output ("{0}: {1}x{2}" -f $name, $r[2], $r[3])
}
