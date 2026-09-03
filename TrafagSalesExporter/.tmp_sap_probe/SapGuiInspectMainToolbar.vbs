Option Explicit
Dim target, a, app, c, s, i, j, k, b, found
target = UCase(WScript.Arguments(0))
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = target And s.Info.SystemName = "T76" And s.Info.Client = "100" Then found = True: Exit For
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2
For k = 0 To s.FindById("wnd[0]/tbar[1]").Children.Count - 1
  Set b = s.FindById("wnd[0]/tbar[1]").Children(CLng(k))
  On Error Resume Next
  WScript.Echo b.Id & " Text=" & b.Text & " Tooltip=" & b.Tooltip & " Icon=" & b.IconName
  On Error GoTo 0
Next
