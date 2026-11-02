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
s.FindById("wnd[0]/tbar[1]/btn[27]").Press
WScript.Sleep 3500
On Error Resume Next
WScript.Echo "SBAR=" & s.FindById("wnd[0]/sbar").Text & " TYP=" & s.FindById("wnd[0]/sbar").MessageType
WScript.Echo "FENSTER1=" & s.FindById("wnd[1]").Text
WScript.Echo "FENSTER2=" & s.FindById("wnd[2]").Text
