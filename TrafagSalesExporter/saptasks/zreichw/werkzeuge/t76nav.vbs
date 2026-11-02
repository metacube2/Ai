Option Explicit
Dim app, c, ci, s
Set app = GetObject("SAPGUI").GetScriptingEngine
For ci = 0 To app.Children.Count - 1
  If app.Children(CLng(ci)).Children.Count > 0 Then
    If app.Children(CLng(ci)).Children(0).Info.SystemName = "T76" Then Set c = app.Children(CLng(ci))
  End If
Next
Set s = c.Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Or CLng(WScript.Arguments(0)) = 0 Then WScript.Echo "ABBRUCH": WScript.Quit 2
WScript.Echo "ID=" & s.Id & " BUSY=" & s.Busy & " TX=" & s.Info.Transaction
s.FindById("wnd[0]").SendVKey 3
WScript.Sleep 1500
WScript.Echo "nach F3 TX=" & s.Info.Transaction & " F=" & s.ActiveWindow.Text
s.FindById("wnd[0]/tbar[0]/okcd").Text = WScript.Arguments(1)
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2500
WScript.Echo "nach OK TX=" & s.Info.Transaction & " F=" & s.ActiveWindow.Text & " SBAR=" & s.FindById("wnd[0]/sbar").Text
