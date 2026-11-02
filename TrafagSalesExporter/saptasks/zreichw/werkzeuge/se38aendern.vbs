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
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE38"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2000
s.FindById("wnd[0]/usr/ctxtRS38M-PROGRAMM").Text = WScript.Arguments(1)
s.FindById("wnd[0]/usr/radRS38M-FUNC_EDIT").Select
s.FindById("wnd[0]").SendVKey 6
WScript.Sleep 3000
On Error Resume Next
WScript.Echo "FENSTER0=" & s.FindById("wnd[0]").Text
WScript.Echo "FENSTER1=" & s.FindById("wnd[1]").Text
WScript.Echo "SBAR=" & s.FindById("wnd[0]/sbar").Text
