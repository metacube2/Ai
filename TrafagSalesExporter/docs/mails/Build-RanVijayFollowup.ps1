# ============================================================================
#  UEBERHOLT AM 2026-08-05 - NICHT MEHR AUSFUEHREN, NICHT VERSENDEN
#
#  Diese Mail bittet Indien, auf 1'271 Artikeln den Preferred Vendor nachzupflegen. Das ist
#  fachlich falsch: 1'184 dieser Artikel sind Eigenfertigung (Sales Type FFM) und brauchen
#  ueberhaupt keinen Lieferanten, weitere 94 sind ueber OITM."U_Tasc_ST" bereits eindeutig.
#  Zu pflegen bleiben 66 Artikel plus 10 zu bestaetigende Widersprueche.
#
#  Gueltiger Stand:  docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md
#  Neuer Anhang:     output/TRIN_Sales_Type_Offen_2026-08-05.xlsx
#
#  Das Skript bleibt als Historie stehen (die Mail vom 2026-07-31 wurde gesendet, RanVijays
#  Antwort bezieht sich darauf). Der Guard unten verhindert ein versehentliches Ausfuehren.
# ============================================================================
#
# Erzeugt eine versandfertige Antwort an RanVijay (Trafag India) mit der Excel-Artikelliste
# als Anhang. RanVijay hat auf die erste Mail (docs/FINANCE_FELDLUECKEN_MAILS_2026-07-31.md,
# Abschnitt 3) geantwortet, dass er die Frage nicht versteht, und um einen Teams-Call gebeten.
# Diese Mail klaert die Vendor/Supplier-Verwechslung in einem Satz und haengt die konkrete
# Artikelliste an, als Alternative bzw. Vorbereitung zu einem Call.
#
# Nichts wird gesendet: -Mode Draft legt nur einen editierbaren Outlook-Entwurf mit
# Senden-Knopf an (MailItem.Save(), kein SaveAs - .msg/.oft sind auf diesem Arbeitsplatz per
# DLP gesperrt, s. docs/mails/Build-StandortMails.ps1 fuer den Befund).

param(
  [ValidateSet('Preview', 'Draft')]
  [string]$Mode = 'Preview',

  [string]$AttachmentPath = '',

  # Nur setzen, wenn die ueberholte Fassung bewusst rekonstruiert werden soll.
  [switch]$IchWeissDassDieseMailUeberholtIst
)

$ErrorActionPreference = 'Stop'

if (-not $IchWeissDassDieseMailUeberholtIst) {
  Write-Warning 'Diese Mail ist seit 2026-08-05 ueberholt und wurde NICHT erzeugt.'
  Write-Host 'Sie bittet um Pflege von 1''271 Artikeln; tatsaechlich sind es 66 plus 10 Rueckfragen.'
  Write-Host 'Gueltiger Stand: docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md'
  Write-Host 'Neuer Anhang   : output/TRIN_Sales_Type_Offen_2026-08-05.xlsx'
  Write-Host ''
  Write-Host 'Trotzdem erzeugen: -IchWeissDassDieseMailUeberholtIst'
  return
}
$RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$OutDir = Join-Path $RepoRoot '.tmp_standort_mails'
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }

if (-not $AttachmentPath) {
  $latest = Get-ChildItem (Join-Path $RepoRoot 'output') -Filter 'TRIN_Fehlende_Preferred_Vendor_*.xlsx' |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
  if (-not $latest) { throw "Keine TRIN_Fehlende_Preferred_Vendor_*.xlsx in output\ gefunden - -AttachmentPath explizit angeben." }
  $AttachmentPath = $latest.FullName
}
if (-not (Test-Path $AttachmentPath)) { throw "Anhang nicht gefunden: $AttachmentPath" }

$To      = 'RanVijay.Kumar@trafag.com'
$Cc      = 'Andreas.Stoller@trafag.com'
$Subject = 'RE: BI Dashboard - supplier missing on the item master (Trafag India) -> Supplier Name'

$Html = @"
<p>Dear RanVijay,</p>
<p>Sorry for the confusion &mdash; let me put it in one sentence and attach the concrete list, so a
call isn't strictly needed unless you'd still prefer one.</p>
<p><b>"Supplier" and "Vendor" are the same field.</b> In the item master (General &gt; Purchasing
Data tab) SAP calls it <b>Preferred Vendor</b>; in our BI Dashboard data model the same field is
called <b>Supplier</b> (database field <span style="font-family:Consolas,monospace">OITM.CardCode</span>).
There is no separate "vendor" field to fill in &mdash; it is the one field, just two names for it in
two different places.</p>
<p>Attached is the Excel list of the <b>1,271 item codes</b> from my last mail where that field is
currently empty (item code, description, number of invoice lines and value affected per item, out
of your 1,437 item codes in total). Someone on your side would need to open each item code in the
item master and fill in Preferred Vendor with the actual supplier / Trafag entity that delivers it.</p>
<p>If it's easier to walk through a few examples together, I'm happy to do a short Teams call &mdash;
just suggest a time that works for you.</p>
<p>Best regards<br>Ingo</p>
"@

switch ($Mode) {

  'Draft' {
    $ol = New-Object -ComObject Outlook.Application
    $mail = $ol.CreateItem(0)
    $sig = ''
    try { $null = $mail.GetInspector; $sig = $mail.HTMLBody } catch { $sig = '' }
    $mail.To      = $To
    $mail.CC      = $Cc
    $mail.Subject = $Subject
    $mail.HTMLBody = '<div style="font-family:Calibri,Arial,sans-serif;font-size:11pt;color:#1F1F1F;max-width:620px">' + $Html + '</div>' + $sig
    $mail.Attachments.Add($AttachmentPath) | Out-Null
    $mail.Save()
    [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($mail)
    [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($ol)
    "Entwurf angelegt: '$Subject' an $To (Cc $Cc), Anhang $([System.IO.Path]::GetFileName($AttachmentPath))."
    'Liegt im Outlook-Ordner Entwuerfe. Es wurde NICHTS gesendet.'
    if (-not $sig) { 'Signatur uebernommen: NEIN - in Outlook selbst ergaenzen.' }
  }

  default {
    $html = @"
<html><head><meta charset="utf-8"><title>Antwort RanVijay - Vorschau</title></head>
<body style="background:#E9ECEF;margin:0;padding:22px;font-family:Calibri,Arial,sans-serif">
<div style="max-width:700px;margin:0 auto">
<div style="border:1px solid #D0D0D0;background:#FFFFFF">
<table cellpadding="0" cellspacing="0" border="0" width="100%" style="border-collapse:collapse;background:#F2F5F8;border-bottom:1px solid #D0D0D0">
<tr><td style="padding:8px 14px;font:9.5pt Calibri">
<b>An:</b> $To<br><b>Cc:</b> $Cc<br><b>Betreff:</b> $Subject<br>
<b>Anhang:</b> $([System.IO.Path]::GetFileName($AttachmentPath))</td></tr>
</table>
<div style="padding:14px 18px;font-size:11pt;color:#1F1F1F;max-width:620px">$Html</div>
</div>
</div></body></html>
"@
    $previewPath = Join-Path $OutDir 'Vorschau_RanVijayFollowup.html'
    [System.IO.File]::WriteAllText($previewPath, $html, (New-Object System.Text.UTF8Encoding($true)))
    "Vorschau: $previewPath"
    "Anhang  : $AttachmentPath"
  }
}
