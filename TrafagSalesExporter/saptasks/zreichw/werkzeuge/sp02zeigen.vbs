Option Explicit
Dim app, c, ci, s, k, o, txt, y, line, maxx
Set app = GetObject("SAPGUI").GetScriptingEngine
For ci = 0 To app.Children.Count - 1
  If app.Children(CLng(ci)).Children.Count > 0 Then
    If app.Children(CLng(ci)).Children(0).Info.SystemName = "T76" Then Set c = app.Children(CLng(ci))
  End If
Next
Set s = c.Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Or CLng(WScript.Arguments(0)) = 0 Then WScript.Echo "ABBRUCH": WScript.Quit 2
s.FindById("wnd[0]/usr/chk[1,3]").Selected = True
s.FindById("wnd[0]").SendVKey 6
WScript.Sleep 3000
WScript.Echo "F=" & s.ActiveWindow.Text & " TX=" & s.Info.Transaction & " PROG=" & s.Info.Program & " DYN=" & s.Info.ScreenNumber
On Error Resume Next
Dim usr: Set usr = s.FindById("wnd[0]/usr")
WScript.Echo "KINDER=" & usr.Children.Count
For k = 0 To usr.Children.Count - 1
  Set o = usr.Children(CLng(k))
  If k < 80 Then WScript.Echo o.Id & " [" & o.Type & "] " & Left(o.Text, 150)
Next
WScript.Echo "HSCROLL_MAX=" & usr.HorizontalScrollbar.Maximum
