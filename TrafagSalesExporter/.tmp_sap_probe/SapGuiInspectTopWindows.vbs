Option Explicit
Dim a, app, c, s, i, j, k, w, found
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = "SE19" And s.Info.SystemName = "T76" And s.Info.Client = "100" Then found = True: Exit For
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2
WScript.Echo "Windows=" & s.Children.Count
For k = 0 To s.Children.Count - 1
  Set w = s.Children(CLng(k))
  WScript.Echo w.Id & " | " & w.Type & " | " & w.Text
Next
