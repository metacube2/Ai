Option Explicit
Dim app, c, ci, s, t0
Set app = GetObject("SAPGUI").GetScriptingEngine
For ci = 0 To app.Children.Count - 1
  If app.Children(CLng(ci)).Children.Count > 0 Then
    If app.Children(CLng(ci)).Children(0).Info.SystemName = "T76" Then Set c = app.Children(CLng(ci))
  End If
Next
Set s = c.Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Or CLng(WScript.Arguments(0)) = 0 Then WScript.Echo "ABBRUCH": WScript.Quit 2
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nZREICHW"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2000
s.FindById("wnd[0]/usr/ctxtS_MATNR-LOW").Text = WScript.Arguments(1)
s.FindById("wnd[0]/usr/ctxtS_MATNR-HIGH").Text = WScript.Arguments(2)
s.FindById("wnd[0]/usr/chkP_SPOOL").Selected = (WScript.Arguments(3) = "X")
WScript.Echo "LABEL=" & s.FindById("wnd[0]/usr/txtLBL_SPO").Text
t0 = Timer
s.FindById("wnd[0]").SendVKey 8
WScript.Sleep 4000
On Error Resume Next
WScript.Echo "DAUER~" & Round(Timer - t0, 1)
WScript.Echo "FENSTER0=" & s.FindById("wnd[0]").Text & " TX=" & s.Info.Transaction & " PROG=" & s.Info.Program & " DYNPRO=" & s.Info.ScreenNumber
WScript.Echo "SBAR=" & s.FindById("wnd[0]/sbar").Text & " TYP=" & s.FindById("wnd[0]/sbar").MessageType
WScript.Echo "FENSTER1=" & s.FindById("wnd[1]").Text
