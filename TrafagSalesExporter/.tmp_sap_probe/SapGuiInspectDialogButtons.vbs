Option Explicit
Dim a, app, c, s, i, j, k, b
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = "SE19" And s.Info.SystemName = "T76" And s.Info.Client = "100" Then
      WScript.Echo "Dialog=" & s.FindById("wnd[1]").Text
      For k = 0 To s.FindById("wnd[1]/tbar[0]").Children.Count - 1
        Set b = s.FindById("wnd[1]/tbar[0]").Children(CLng(k))
        On Error Resume Next
        WScript.Echo b.Id & " Text=" & b.Text & " Tooltip=" & b.Tooltip & " Icon=" & b.IconName
        On Error GoTo 0
      Next
      WScript.Quit 0
    End If
  Next
Next
WScript.Quit 2
