Option Explicit
' Holt die Sitzung mit dem angegebenen Fenstertitel-Bestandteil in den Vordergrund.

Dim muster, a, app, c, s, i, j, found

muster = WScript.Arguments(0)

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If InStr(1, s.FindById("wnd[0]").Text, muster, vbTextCompare) > 0 Then
      found = True
      Exit For
    End If
  Next
  If found Then Exit For
Next
If Not found Then
  WScript.Echo "Kein Fenster mit '" & muster & "' gefunden"
  WScript.Quit 2
End If

s.FindById("wnd[0]").Restore
s.FindById("wnd[0]").Maximize
WScript.Echo "VORDERGRUND=" & s.FindById("wnd[0]").Text
WScript.Echo "SAP_SITZUNG=" & s.Info.SessionNumber
WScript.Quit 0
