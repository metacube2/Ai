Option Explicit
' Zeigt eine klassische BAdI-Definition in SE18 an, rein lesend.
' Argument 1: BAdI-Name, zum Beispiel WORKORDER_UPDATE.

Dim badi, a, app, c, s, i, j, found

badi = WScript.Arguments(0)

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = "SE18" And s.Info.SystemName = "T76" And s.Info.Client = "100" Then
      found = True
      Exit For
    End If
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2

If InStr(1, s.FindById("wnd[0]").Text, "Einstieg", vbTextCompare) = 0 Then
  s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE18"
  s.FindById("wnd[0]").SendVKey 0
  WScript.Sleep 2000
End If

s.FindById("wnd[0]/usr/radG_IS_BADI").Select
s.FindById("wnd[0]/usr/ctxtG_BADINAME").Text = badi
s.FindById("wnd[0]/usr/btnPUSHBUTTON_DISPLAY").Press
WScript.Sleep 3000

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
WScript.Quit 0
