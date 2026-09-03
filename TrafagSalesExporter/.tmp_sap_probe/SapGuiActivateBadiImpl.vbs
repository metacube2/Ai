Option Explicit
' Aktiviert die BAdI-Implementierung in SE19 und liest den Aktivkennzeichen-Status zurueck.
' Der Statustext RSEXSCRN-ACTIVE ist die einzige verlaessliche Auskunft: die Statusleiste
' meldet auch dann nur eine Warnung, wenn die Aktivierung gar nicht stattgefunden hat.

Dim a, app, c, s, i, j, found

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = "SE19" And s.Info.SystemName = "T76" And s.Info.Client = "100" Then
      found = True
      Exit For
    End If
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2

If s.FindById("wnd[0]/usr/ctxtRSEXSCRN-IMP_NAME").Text <> "Z_ZZPRDAT_AT_RELEASE" Then WScript.Quit 5

WScript.Echo "VORHER=" & s.FindById("wnd[0]/usr/txtRSEXSCRN-ACTIVE").Text
s.FindById("wnd[0]/tbar[1]/btn[27]").Press
WScript.Sleep 3000

On Error Resume Next
Dim dlg
dlg = s.FindById("wnd[1]").Text
If Err.Number = 0 Then
  WScript.Echo "DIALOG=" & dlg
End If
Err.Clear
On Error GoTo 0

WScript.Echo "MELDUNGSTYP=" & s.FindById("wnd[0]/sbar").MessageType
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
WScript.Echo "NACHHER=" & s.FindById("wnd[0]/usr/txtRSEXSCRN-ACTIVE").Text
WScript.Quit 0
