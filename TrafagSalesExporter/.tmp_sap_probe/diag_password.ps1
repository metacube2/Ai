param([string]$Pass = '')
Write-Host "Length: $($Pass.Length)"
$bytes = [System.Text.Encoding]::UTF8.GetBytes($Pass)
Write-Host "UTF8 byte count: $($bytes.Length)"
Write-Host "First char code: $([int][char]$Pass[0])"
Write-Host "Last char code: $([int][char]$Pass[$Pass.Length-1])"
for ($i = 0; $i -lt $Pass.Length; $i++) {
  Write-Host ("{0}: char='{1}' code={2}" -f $i, $Pass[$i], [int][char]$Pass[$i])
}
