Option Explicit
' Fuellt den Dialog "Funktionsgruppe hinzufuegen" und sichert.
' Aufruf: SapGuiFgAnlegen.vbs <Gruppe> <Kurztext>
'
' Danach fragt SAP nach dem Paket. Das wird bewusst NICHT hier beantwortet,
' damit die Paket- und Transportzuordnung ein eigener, sichtbarer Schritt bleibt.

Dim gruppe, text, a, app, c, s, i, j, found

gruppe = WScript.Arguments(0)
text = WScript.Arguments(1)

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = "SE37" And s.Info.SystemName = "T76" And s.Info.Client = "100" Then
      found = True
      Exit For
    End If
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2

If InStr(1, s.FindById("wnd[1]").Text, "Funktionsgruppe", vbTextCompare) = 0 Then
  WScript.Echo "ABBRUCH: unerwarteter Dialog " & s.FindById("wnd[1]").Text
  WScript.Quit 3
End If

s.FindById("wnd[1]/usr/ctxtTLIBG-AREA").Text = gruppe
s.FindById("wnd[1]/usr/txtTLIBT-AREAT").Text = text
s.FindById("wnd[1]/tbar[0]/btn[0]").Press
WScript.Sleep 3000

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "MELDUNGSTYP=" & s.FindById("wnd[0]/sbar").MessageType
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
On Error Resume Next
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
WScript.Quit 0
