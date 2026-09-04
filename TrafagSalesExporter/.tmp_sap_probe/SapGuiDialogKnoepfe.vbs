Option Explicit
Dim a, app, c, s, i, j, k, found
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = "SE37" Then found = True: Exit For
  Next
  If found Then Exit For
Next
For k = 0 To s.FindById("wnd[2]/tbar[0]").Children.Count - 1
  WScript.Echo "  " & s.FindById("wnd[2]/tbar[0]").Children(CLng(k)).Name & "  " & s.FindById("wnd[2]/tbar[0]").Children(CLng(k)).Tooltip
Next
