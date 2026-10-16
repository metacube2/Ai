# SVG -> PNG fuer die Word-Fassung des Handbuchs (Edge headless, doppelte Aufloesung).
# Aufruf: powershell -NoProfile -File Tools\HandbuchGrafik\render_png.ps1 wwwroot\handbuch\bilder\operations_prozess.svg 1400 880
param([string]$Svg, [int]$Width, [int]$Height)
$edge = @("${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe", "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
$svgFull = (Resolve-Path $Svg).Path
$png = [IO.Path]::ChangeExtension($svgFull, '.png')
$html = Join-Path $env:TEMP 'handbuch_grafik.html'
"<html><body style='margin:0'><img src='file:///$($svgFull.Replace([char]92,[char]47))' width='$Width' height='$Height'></body></html>" | Set-Content $html -Encoding UTF8
& $edge --headless=new --disable-gpu --hide-scrollbars --force-device-scale-factor=2 "--window-size=$Width,$Height" "--screenshot=$png" "file:///$($html.Replace([char]92,[char]47))" | Out-Null
Start-Sleep -Seconds 2
Get-Item $png | Select-Object Name, Length
