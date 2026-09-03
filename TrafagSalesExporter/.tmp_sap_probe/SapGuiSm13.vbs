Option Explicit
' Zeigt in SM13 alle Verbuchungsauftraege des heutigen Tages fuer den angegebenen
' Benutzer. Rein lesend: es wird nichts wiederholt und nichts geloescht.
' Argument 1 optional: Benutzer, Standard KOI.

Dim benutzer, a, app, c, s, i, j, found

If WScript.Arguments.Count > 0 Then benutzer = WScript.Arguments(0) Else benutzer = "KOI"

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = "SM13" And s.Info.SystemName = "T76" And s.Info.Client = "100" Then
      found = True
      Exit For
    End If
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2

If InStr(1, s.FindById("wnd[0]").Text, "Einstieg", vbTextCompare) = 0 Then
  s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSM13"
  s.FindById("wnd[0]").SendVKey 0
  WScript.Sleep 2000
End If

s.FindById("wnd[0]/usr/txtSEL_USER").Text = benutzer
s.FindById("wnd[0]/usr/radSEL_ALL").Select
s.FindById("wnd[0]").SendVKey 8
WScript.Sleep 2500

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "MELDUNGSTYP=" & s.FindById("wnd[0]/sbar").MessageType
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
WScript.Quit 0
