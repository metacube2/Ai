# Erzeugt versandfertige .msg-Dateien aus docs/FINANCE_FELDLUECKEN_MAILS_2026-07-31.md.
# Nichts wird gesendet: jede Datei oeffnet in Outlook als editierbare Mail mit Senden-Knopf.
#
# Grafik-Regeln (Outlook-Rendering, Word-Engine):
#   - Nur Tabellen mit bgcolor und Inline-Styles. Kein flex, kein grid, keine border-radius.
#   - KEINE Bilder: Outlook blockiert externe Bilder beim Empfaenger, CID-Anhaenge landen
#     als Dateianhang im Mailkopf. Balken werden deshalb aus Tabellenzellen gebaut.
#   - Balkenbreiten in px (fix 460), nicht in Prozent - Prozentbreiten kippen in manchen
#     Outlook-Versionen.
#   - Artikel-Balken zeigen EXAKTE Stueckzahlen, Zeilen-Balken nur Prozente: die
#     Zeilenzahlen je Kategorie sind aus gerundeten Prozenten abgeleitet und wuerden
#     eine Genauigkeit vortaeuschen, die die Messung nicht hat.

param(
  [ValidateSet('Preview', 'Docx', 'Draft')]
  [string]$Mode = 'Preview'
)

$ErrorActionPreference = 'Stop'
$OutDir = Join-Path $PSScriptRoot '..\..\.tmp_standort_mails'
$OutDir = [System.IO.Path]::GetFullPath($OutDir)
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }

$GREEN = '#2E7D32'; $RED = '#C62828'; $AMBER = '#E08A00'; $GREY = '#BDBDBD'; $NAVY = '#1F4E79'
$W = 460

function Swatch([string]$color) {
  "<table cellpadding=`"0`" cellspacing=`"0`" border=`"0`" style=`"display:inline-table;border-collapse:collapse`"><tr><td bgcolor=`"$color`" width=`"11`" height=`"11`" style=`"font-size:1px;line-height:1px`">&nbsp;</td></tr></table>"
}

function Caption([string]$text) {
  "<p style=`"margin:14px 0 4px 0;font:bold 9.5pt Calibri;color:$NAVY;letter-spacing:.3px;text-transform:uppercase`">$text</p>"
}

# Zwei-Segment-Balken. $okPct steuert nur die Breite, die Labels stehen in der Legende.
function Bar([int]$okPct, [string]$okLabel, [string]$missLabel, [string]$okColor = $GREEN, [string]$missColor = $RED) {
  $okPx = [int][math]::Round($W * $okPct / 100.0)
  if ($okPct -gt 0 -and $okPx -lt 3) { $okPx = 3 }
  if ($okPct -lt 100 -and $okPx -gt ($W - 3)) { $okPx = $W - 3 }
  $missPx = $W - $okPx
  $inOk = ''; $inMiss = ''
  if ($okPx -ge 95)   { $inOk   = "$okPct%" }
  if ($missPx -ge 95) { $inMiss = "$(100 - $okPct)%" }
  $cells = ''
  if ($okPx -gt 0)   { $cells += "<td bgcolor=`"$okColor`" width=`"$okPx`" height=`"22`" style=`"font:bold 9.5pt Calibri;color:#FFFFFF;text-align:center`">$inOk</td>" }
  if ($missPx -gt 0) { $cells += "<td bgcolor=`"$missColor`" width=`"$missPx`" height=`"22`" style=`"font:bold 9.5pt Calibri;color:#FFFFFF;text-align:center`">$inMiss</td>" }
  $bar = "<table cellpadding=`"0`" cellspacing=`"0`" border=`"0`" width=`"$W`" style=`"border-collapse:collapse;border:1px solid #909090`"><tr>$cells</tr></table>"
  $leg = "<p style=`"margin:4px 0 0 0;font:9.5pt Calibri`">$(Swatch $okColor)&nbsp;$okLabel&nbsp;&nbsp;&nbsp;$(Swatch $missColor)&nbsp;$missLabel</p>"
  $bar + $leg
}

# Monatsstreifen. $states: 12 Zeichen, r=fehlt, a=teilweise, g=vorhanden, x=Zukunft.
function MonthStrip([string]$states) {
  $names = 'J','F','M','A','M','J','J','A','S','O','N','D'
  $cw = [int]($W / 12)
  $row1 = ''; $row2 = ''
  for ($i = 0; $i -lt 12; $i++) {
    $c = switch ($states[$i]) { 'r' { $RED } 'a' { $AMBER } 'g' { $GREEN } default { '#F0F0F0' } }
    $fg = if ($states[$i] -eq 'x') { '#909090' } else { '#FFFFFF' }
    $row1 += "<td bgcolor=`"$c`" width=`"$cw`" height=`"20`" style=`"border-right:1px solid #FFFFFF;font:bold 9pt Calibri;color:$fg;text-align:center`">$($names[$i])</td>"
  }
  "<table cellpadding=`"0`" cellspacing=`"0`" border=`"0`" width=`"$W`" style=`"border-collapse:collapse;border:1px solid #909090`"><tr>$row1</tr></table>"
}

# Schema: wo das Feld im B1-Artikelstamm sitzt.
function FieldSchematic([string]$missingNote) {
  @"
<table cellpadding="0" cellspacing="0" border="0" width="$W" style="border-collapse:collapse;border:1px solid #A0A0A0;border-bottom:none">
<tr>
<td bgcolor="#EFEFEF" style="padding:5px 9px;font:9pt Calibri;color:#707070;border-right:1px solid #A0A0A0">General</td>
<td bgcolor="#FFFFFF" style="padding:5px 9px;font:bold 9pt Calibri;color:$NAVY;border-right:1px solid #A0A0A0;border-bottom:2px solid $NAVY">Purchasing Data</td>
<td bgcolor="#EFEFEF" style="padding:5px 9px;font:9pt Calibri;color:#707070;border-right:1px solid #A0A0A0">Sales Data</td>
<td bgcolor="#EFEFEF" style="padding:5px 9px;font:9pt Calibri;color:#707070">Inventory Data</td>
</tr>
</table>
<table cellpadding="0" cellspacing="0" border="0" width="$W" style="border-collapse:collapse;border:1px solid #A0A0A0;background:#FCFCFC">
<tr>
<td style="padding:9px 9px 9px 9px;font:9.5pt Calibri;white-space:nowrap">Preferred Vendor</td>
<td style="padding:9px 6px"><table cellpadding="0" cellspacing="0" border="0" width="150" style="border-collapse:collapse"><tr><td bgcolor="#FFF4F4" height="20" style="border:1px solid $RED;font:9pt Calibri;color:$RED;text-align:center">&nbsp;</td></tr></table></td>
<td style="padding:9px 9px;font:9pt Calibri;color:$RED;white-space:nowrap">&#8592; $missingNote</td>
</tr>
</table>
<p style="margin:4px 0 0 0;font:8.5pt Calibri;color:#707070">Database field
<span style="font-family:Consolas,monospace">OITM.CardCode</span> &mdash; this is the only field we
read to identify the supplier of an item.</p>
"@
}

function GreyBox([string]$inner) {
  "<table cellpadding=`"0`" cellspacing=`"0`" border=`"0`" width=`"$W`" style=`"border-collapse:collapse;border-left:3px solid $GREY;background:#F7F7F7`"><tr><td style=`"padding:8px 12px`">$inner</td></tr></table>"
}

$intro = 'we have completed a field-by-field check of the sales data that feeds the group BI Dashboard, measured on the consolidated extract of 29 July 2026.'

$notNeeded = GreyBox @"
<p style="margin:0 0 6px 0;font:bold 9.5pt Calibri;color:#555555">Nothing to do on these three &mdash; please do not spend time on them</p>
<p style="margin:0;font:9.5pt Calibri;color:#555555">
<b>Product division / product family</b> &mdash; derived centrally from the Trafag AG material
master; local ERP product divisions are deliberately not used. Only the <b>material number</b> on
the invoice line has to match the Trafag AG master.<br>
<b>Exchange rates on the document</b> &mdash; currency conversion is done centrally.<br>
<b>Item costs on freight, packaging, certificate and documentation lines</b> &mdash; we checked
these and they are correctly zero.</p>
"@

$mails = @()

# ------------------------------------------------------------------ 1 Frankreich
$mails += [pscustomobject]@{
  File    = '1_TRFR_Frankreich_ADRESSE_FEHLT.msg'
  To      = ''
  Subject = 'BI Dashboard - supplier missing on the item master (Trafag France)'
  Html    = @"
<p>Dear colleagues,</p>
<p>$intro For Trafag France there is exactly one thing missing, and it is the smallest amount of
work of all our sites.</p>
$(Caption 'Item codes with a Preferred Vendor maintained')
$(Bar 14 '59 item codes maintained' '374 item codes missing')
$(Caption 'Invoice lines we can attribute to a supplier')
$(Bar 5 'supplier known' 'supplier unknown')
<p style="margin:4px 0 0 0;font:8.5pt Calibri;color:#707070">433 item codes and 2,577 invoice
lines in total.</p>
$(Caption 'Where the field sits')
$(FieldSchematic 'empty on 374 of your 433 items')
<p>We read the supplier from exactly that field, so an item without it produces invoice lines we
cannot classify as intercompany versus third-party purchase &mdash; which is what the group margin
depends on.</p>
<p>The lines that do carry a supplier are recognised correctly (Trafag AG and Trafag Italia), so
nothing beyond the master data is needed. Could you have those item codes reviewed? We are happy
to send you the list of affected items.</p>
$notNeeded
<p>Happy to do a short call if that is easier than email.</p>
<p>Best regards<br>Ingo</p>
"@
}

# ------------------------------------------------------------------ 2 Italien
$mails += [pscustomobject]@{
  File    = '2_TRIT_Italien_Paola.msg'
  To      = 'Paola.Castagna@trafag.com'
  Subject = 'BI Dashboard - supplier missing on the item master (Trafag Italia)'
  Html    = @"
<p>Dear Paola,</p>
<p>a separate topic from the inventory valuation discussion &mdash; this one is master data only,
it has no bearing on the moving-average question and there is no deadline attached to it. Given
the B1 upgrade on 3 August, please look at it whenever it suits you afterwards.</p>
<p>$intro You are the best-performing site on supplier data, thank you.</p>
$(Caption 'Item codes with a Preferred Vendor maintained')
$(Bar 71 '2,341 item codes maintained' '939 item codes missing')
$(Caption 'Invoice lines we can attribute to a supplier')
$(Bar 71 'supplier known' 'supplier unknown')
<p style="margin:4px 0 0 0;font:8.5pt Calibri;color:#707070">3,280 item codes and 19,534 invoice
lines in total &mdash; the highest share of all sites.</p>
$(Caption 'Where the field sits')
$(FieldSchematic 'empty on 939 of your 3,280 items')
<p>We read the supplier from exactly that field, so an item without it produces invoice lines we
cannot classify as intercompany versus third-party purchase &mdash; which is what the group margin
depends on.</p>
<p>Could you have those item codes reviewed? We can send you the list.</p>
$notNeeded
<p>Best regards<br>Ingo</p>
"@
}

# ------------------------------------------------------------------ 3 Indien
$mails += [pscustomobject]@{
  File    = '3_TRIN_Indien_RanVijay.msg'
  To      = 'RanVijay.Kumar@trafag.com'
  Subject = 'BI Dashboard - supplier missing on the item master (Trafag India)'
  Html    = @"
<p>Dear RanVijay,</p>
<p>$intro For Trafag India there is one point.</p>
$(Caption 'Item codes with a Preferred Vendor maintained')
$(Bar 12 '166 item codes maintained' '1,271 item codes missing')
$(Caption 'Invoice lines we can attribute to a supplier')
$(Bar 12 'supplier known' 'supplier unknown')
<p style="margin:4px 0 0 0;font:8.5pt Calibri;color:#707070">1,437 item codes and 6,990 invoice
lines in total.</p>
<p>The good news is that the mechanism works: of the lines that do carry a supplier, 677 are
correctly identified as Trafag AG deliveries. It is simply not maintained on most items.</p>
$(Caption 'Where the field sits')
$(FieldSchematic 'empty on 1,271 of your 1,437 items')
<p>Filling it would move roughly 6,100 invoice lines from &quot;supplier unknown&quot; into a
proper classification &mdash; which is what the group margin depends on.</p>
<p>If it helps, I can send you the list of affected item codes directly, and you can decide who on
your side should work through it.</p>
$notNeeded
<p>Best regards<br>Ingo</p>
"@
}

# ------------------------------------------------------------------ 4 USA
$mails += [pscustomobject]@{
  File    = '4_TRUS_USA_ADRESSE_FEHLT.msg'
  To      = ''
  Subject = 'BI Dashboard - supplier missing on the item master (Trafag USA)'
  Html    = @"
<p>Dear colleagues,</p>
<p>$intro For Trafag USA there is one point.</p>
$(Caption 'Item codes with a Preferred Vendor maintained')
$(Bar 1 '3 item codes maintained' '518 item codes missing')
$(Caption 'Invoice lines we can attribute to a supplier')
$(Bar 1 '6 lines' '1,498 lines')
<p style="margin:4px 0 0 0;font:8.5pt Calibri;color:#707070">521 item codes and 1,504 invoice
lines in total &mdash; in practice the field is unused.</p>
$(Caption 'Where the field sits')
$(FieldSchematic 'empty on 518 of your 521 items')
<p>We read the supplier from exactly that field, so an item without it produces invoice lines we
cannot classify as intercompany versus third-party purchase &mdash; which is what the group margin
depends on.</p>
<p>Could you have those item codes reviewed? We are happy to send you the list. As it is
essentially the whole item master, it may be worth a short call first to agree on the most
efficient way to fill it &mdash; for example a bulk update rather than item by item.</p>
$notNeeded
<p>Best regards<br>Ingo</p>
"@
}

# ------------------------------------------------------------------ 5 Deutschland
$mails += [pscustomobject]@{
  File    = '5_TRDE_Deutschland_Rohail.msg'
  To      = 'Rohail.Munir@trafag.de'
  Subject = 'BI Dashboard - three questions on the Alphaplan export (Trafag GmbH)'
  Html    = @"
<p>Dear Rohail,</p>
<p>$intro For Germany there are three points, and all three concern the Alphaplan export as it
currently reaches us rather than master data maintenance. If someone else looks after the Alphaplan
export on your side, could you please forward this to them?</p>
$(Caption 'What arrives in the export, measured on 7,171 invoice lines')
<table cellpadding="0" cellspacing="0" border="0" width="$W" style="border-collapse:collapse;font:9.5pt Calibri">
<tr>
<td style="padding:5px 8px;border-bottom:1px solid #DDDDDD;width:210px">Material number</td>
<td style="padding:5px 8px;border-bottom:1px solid #DDDDDD">$(Swatch $GREEN)&nbsp;complete</td>
</tr>
<tr>
<td style="padding:5px 8px;border-bottom:1px solid #DDDDDD">Customer <b>number</b></td>
<td style="padding:5px 8px;border-bottom:1px solid #DDDDDD">$(Swatch $GREEN)&nbsp;complete</td>
</tr>
<tr>
<td style="padding:5px 8px;border-bottom:1px solid #DDDDDD">Supplier number / name / country</td>
<td style="padding:5px 8px;border-bottom:1px solid #DDDDDD">$(Swatch $RED)&nbsp;<b>empty on all 7,171 lines</b></td>
</tr>
<tr>
<td style="padding:5px 8px;border-bottom:1px solid #DDDDDD">Customer <b>name</b> and country</td>
<td style="padding:5px 8px;border-bottom:1px solid #DDDDDD">$(Swatch $RED)&nbsp;<b>empty on all 7,171 lines</b></td>
</tr>
<tr>
<td style="padding:5px 8px">Product description</td>
<td style="padding:5px 8px">$(Swatch $AMBER)&nbsp;2,903 of 7,171 unusable (40%)</td>
</tr>
</table>
<p style="margin:14px 0 6px 0;font:9.5pt Calibri"><b>1.</b> Can the export be extended to include
the <b>supplier of the goods</b> on each invoice line? This is what we need to separate
intercompany deliveries from third-party purchases. If it is not feasible in the short term,
please tell us so we can plan around it.</p>
<p style="margin:0 0 6px 0;font:9.5pt Calibri"><b>2.</b> Could <b>customer name and country</b> be
added? German customers currently appear in group reports as bare numbers, because only the
customer number arrives.</p>
<p style="margin:0 0 6px 0;font:9.5pt Calibri"><b>3.</b> <b>Product descriptions carry formatting
text.</b> It looks as though a rich-text field is exported including its formatting header:</p>
<table cellpadding="0" cellspacing="0" border="0" width="$W" style="border-collapse:collapse">
<tr>
<td bgcolor="#FFF4F4" style="padding:7px 9px;border:1px solid $RED;font:8.5pt Consolas,monospace;color:#7A1A1A">
<span style="font:bold 8pt Calibri;color:$RED">WHAT WE RECEIVE</span><br>
MS Shell Dlg, Microsoft Sans Serif, , , 9B4.4274.769.04.15.46.V3 Picostat PST4B3.44 &hellip;</td>
</tr>
<tr><td height="4" style="font-size:1px;line-height:1px">&nbsp;</td></tr>
<tr>
<td bgcolor="#F4FAF4" style="padding:7px 9px;border:1px solid $GREEN;font:8.5pt Consolas,monospace;color:#1B4D1F">
<span style="font:bold 8pt Calibri;color:$GREEN">WHAT WE NEED</span><br>
9B4.4274.769.04.15.46.V3 Picostat PST4B3.44</td>
</tr>
</table>
<p style="margin:6px 0 0 0;font:9.5pt Calibri">For those 2,903 lines the product name is unusable
in reports.</p>
$notNeeded
<p>Happy to set up a short call with whoever maintains the export if that is easier.</p>
<p>Best regards<br>Ingo</p>
"@
}

# ------------------------------------------------------------------ 6 Spanien
$mails += [pscustomobject]@{
  File    = '6_TRES_Spanien_Santi.msg'
  To      = 'Santi.Gomez@trafag.es'
  Subject = 'BI Dashboard - three points on the Spanish export (Trafag Iberica)'
  Html    = @"
<p>Dear Santi,</p>
<p>$intro For Spain there are three points, measured on 5,504 invoice lines. The first one is the
most important.</p>
$(Caption '2026 data we have received from Spain')
$(MonthStrip 'rrrraggxxxxx')
<p style="margin:4px 0 0 0;font:9.5pt Calibri">
$(Swatch $RED)&nbsp;never exported&nbsp;&nbsp;&nbsp;$(Swatch $AMBER)&nbsp;partial (from 28 May)&nbsp;&nbsp;&nbsp;$(Swatch $GREEN)&nbsp;received&nbsp;&nbsp;&nbsp;$(Swatch '#F0F0F0')&nbsp;still to come</p>
<p style="margin:10px 0 6px 0;font:9.5pt Calibri"><b>1. 1 January to 27 May 2026 has never reached
us.</b> The range export we have starts on 28 May 2026, so the first five months of 2026 are
missing from group reporting entirely. Could you run and send the range export for
01.01.2026 &ndash; 27.05.2026? It is the same script and the same procedure you used before
&mdash; happy to resend the exact command if you no longer have it to hand.</p>
<p style="margin:0 0 6px 0;font:9.5pt Calibri"><b>2. 231 lines have no date whatsoever</b> &mdash;
neither invoice date nor posting date. Those lines drop silently out of every monthly and yearly
report:</p>
$(Bar 96 '5,273 lines dated' '231 lines with no date at all' $GREEN $AMBER)
<p style="margin:6px 0 6px 0;font:9.5pt Calibri">Could you check what kind of documents these
are?</p>
<p style="margin:0 0 6px 0;font:9.5pt Calibri"><b>3. No supplier information</b> &mdash; empty on
all 5,504 lines. Before we ask for a technical change, one question: does the Sage sales/delivery
data model carry a concept of &quot;supplier&quot; on a sales document at all? This is typically a
purchasing attribute rather than a sales one. If it does, could it be added to the export? If it
does not, please tell us, so we can look at another way to identify intercompany deliveries for
Spain.</p>
$notNeeded
<p>Best regards<br>Ingo</p>
"@
}

# ------------------------------------------------------------------ 7 UK
$mails += [pscustomobject]@{
  File    = '7_TRUK_UK_Cornell.msg'
  To      = 'Cornell.Williams@trafag.com'
  Subject = 'BI Dashboard - UK data is complete, one question about 2025'
  Html    = @"
<p>Dear Cornell,</p>
<p>short and positive one. $intro</p>
$(GreyBox "<p style=`"margin:0;font:bold 10pt Calibri;color:$GREEN`">$(Swatch $GREEN)&nbsp;&nbsp;For the UK there is nothing to do.</p>")
$(Caption 'How the UK compares on supplier data')
<table cellpadding="0" cellspacing="0" border="0" width="$W" style="border-collapse:collapse;font:9.5pt Calibri">
<tr>
<td width="150" style="padding:3px 8px 3px 0"><b>United Kingdom</b></td>
<td><table cellpadding="0" cellspacing="0" border="0" width="300" style="border-collapse:collapse"><tr><td bgcolor="$GREEN" height="16" width="300" style="font:bold 8.5pt Calibri;color:#FFFFFF;text-align:center">100%</td></tr></table></td>
</tr>
<tr>
<td style="padding:3px 8px 3px 0;color:#707070">Italy</td>
<td><table cellpadding="0" cellspacing="0" border="0" width="300" style="border-collapse:collapse"><tr><td bgcolor="$GREY" height="16" width="213" style="font:8.5pt Calibri;color:#FFFFFF;text-align:center">71%</td><td width="87">&nbsp;</td></tr></table></td>
</tr>
<tr>
<td style="padding:3px 8px 3px 0;color:#707070">India</td>
<td><table cellpadding="0" cellspacing="0" border="0" width="300" style="border-collapse:collapse"><tr><td bgcolor="$GREY" height="16" width="36" style="font-size:1px">&nbsp;</td><td width="264" style="padding-left:6px;font:8.5pt Calibri;color:#707070">12%</td></tr></table></td>
</tr>
<tr>
<td style="padding:3px 8px 3px 0;color:#707070">Other sites</td>
<td><table cellpadding="0" cellspacing="0" border="0" width="300" style="border-collapse:collapse"><tr><td bgcolor="$GREY" height="16" width="15" style="font-size:1px">&nbsp;</td><td width="285" style="padding-left:6px;font:8.5pt Calibri;color:#707070">0&ndash;5%</td></tr></table></td>
</tr>
</table>
<p style="margin:6px 0 0 0;font:9.5pt Calibri">Supplier information is complete on all 2,955
invoice lines &mdash; you are the only site where that field is fully maintained &mdash; and cost
coverage is at 93%, which is normal given freight and service lines carry no item cost. Thank
you.</p>
$(Caption 'Years available for group reporting')
<table cellpadding="0" cellspacing="0" border="0" width="$W" style="border-collapse:collapse;border:1px solid #909090">
<tr>
<td bgcolor="#F0F0F0" width="230" height="22" style="font:bold 9.5pt Calibri;color:#909090;text-align:center;border-right:1px solid #909090">2025 &mdash; not held</td>
<td bgcolor="$GREEN" width="229" height="22" style="font:bold 9.5pt Calibri;color:#FFFFFF;text-align:center">2026 &mdash; complete</td>
</tr>
</table>
<p style="margin:6px 0 0 0;font:9.5pt Calibri">One open point, and only if it is needed: the UK
data we hold starts in January 2026. Is a 2025 export available from your side? If group reporting
asks for the prior year, I would come back to you for it &mdash; no action needed now.</p>
<p>Best regards<br>Ingo</p>
"@
}

# ------------------------------------------------------------------ Ausgabe
#
# WARUM KEINE .msg-DATEIEN: `MailItem.SaveAs` ist auf diesem Arbeitsplatz komplett gesperrt -
# jedes Format (msg/oft/txt) und jeder Zielordner liefern `E_ABORT` (0x80004004), verifiziert
# 2026-07-31. Das ist eine Endpoint-Security-/DLP-Regel, die Outlook das Schreiben von
# Nachrichtendateien auf Platte verbietet, kein Skriptfehler. `MailItem.Save()` in den
# Entwuerfe-Ordner funktioniert dagegen. Deshalb drei Wege:
#
#   -Mode Preview  (Default) HTML-Datei mit allen Mails, im Browser pruefbar. Aendert nichts.
#   -Mode Docx     Word-Dokument aus der Vorschau, zum Weitergeben oder Ablegen.
#   -Mode Draft    Legt die Mails als Entwuerfe in Outlook an. SCHREIBT INS POSTFACH,
#                  sendet aber nichts. Entwuerfe sind einzeln loeschbar.

$wrapOpen  = '<div style="font-family:Calibri,Arial,sans-serif;font-size:11pt;color:#1F1F1F;max-width:620px">'
$wrapClose = '</div>'

switch ($Mode) {

  'Draft' {
    $ol = New-Object -ComObject Outlook.Application
    $created = @()
    $sigSeen = $false
    foreach ($m in $mails) {
      $mail = $ol.CreateItem(0)
      $sig = ''
      try { $null = $mail.GetInspector; $sig = $mail.HTMLBody } catch { $sig = '' }
      if ($sig) { $sigSeen = $true }
      if ($m.To -ne '') { $mail.To = $m.To }
      $mail.Subject  = $m.Subject
      $mail.HTMLBody = $wrapOpen + $m.Html + $wrapClose + $sig
      $mail.Save()
      $created += [pscustomobject]@{
        Entwurf = $m.Subject
        An      = $(if ($m.To) { $m.To } else { '*** LEER - Adresse fehlt ***' })
      }
      [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($mail)
    }
    [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($ol)
    $created | Format-Table -AutoSize
    'Angelegt im Outlook-Ordner Entwuerfe. Es wurde NICHTS gesendet.'
    'Signatur uebernommen: ' + $(if ($sigSeen) { 'ja' } else { 'NEIN - in Outlook selbst ergaenzen' })
  }

  default {
    $cards = ''
    foreach ($m in $mails) {
      $to = if ($m.To) { $m.To } else { '<span style="color:#C62828;font-weight:bold">LEER - Adresse fehlt</span>' }
      $cards += @"
<div style="border:1px solid #D0D0D0;margin:0 0 26px 0;background:#FFFFFF">
<table cellpadding="0" cellspacing="0" border="0" width="100%" style="border-collapse:collapse;background:#F2F5F8;border-bottom:1px solid #D0D0D0">
<tr><td style="padding:8px 14px;font:9.5pt Calibri"><b>An:</b> $to<br><b>Betreff:</b> $($m.Subject)</td></tr>
</table>
<div style="padding:14px 18px">$wrapOpen$($m.Html)$wrapClose</div>
</div>
"@
    }
    $html = @"
<html><head><meta charset="utf-8"><title>Standort-Mails Feldluecken - Vorschau</title></head>
<body style="background:#E9ECEF;margin:0;padding:22px;font-family:Calibri,Arial,sans-serif">
<div style="max-width:760px;margin:0 auto">
<h1 style="font:bold 15pt Calibri;color:$NAVY;margin:0 0 4px 0">Standort-Mails Feldluecken &mdash; Vorschau</h1>
<p style="font:9.5pt Calibri;color:#555;margin:0 0 20px 0">Erzeugt aus
docs/mails/Build-StandortMails.ps1. Quelle der Texte und Zahlen:
docs/FINANCE_FELDLUECKEN_MAILS_2026-07-31.md (Messung 29.07.2026, 95'168 Rechnungszeilen).
Diese Datei ist nur Vorschau &mdash; sie versendet nichts.</p>
$cards
</div></body></html>
"@
    $previewPath = Join-Path $OutDir 'Vorschau_Standortmails.html'
    [System.IO.File]::WriteAllText($previewPath, $html, (New-Object System.Text.UTF8Encoding($true)))
    "Vorschau : $previewPath"

    if ($Mode -eq 'Docx') {
      $word = New-Object -ComObject Word.Application
      $word.Visible = $false
      $doc = $word.Documents.Open($previewPath, $false, $true)
      $docxPath = Join-Path $OutDir 'Standortmails_Feldluecken_2026-07-31.docx'
      $doc.SaveAs2($docxPath, 16)   # wdFormatDocumentDefault
      $doc.Close($false)
      $word.Quit()
      [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($doc)
      [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($word)
      "Word     : $docxPath"
    }
  }
}
