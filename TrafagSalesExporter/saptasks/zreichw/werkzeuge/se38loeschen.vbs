' Loescht ein LOKALES Programm (nur Z_KOI_REICHW_OPT / Z_KOI_REICHW_VGL) in der eigenen T76-Sitzung.
' Aufruf: se38loeschen.vbs <Sitzung> <Programm> start|ok
Option Explicit
Dim app, c, ci, s, prog, i, ctl
Set app = GetObject("SAPGUI").GetScriptingEngine
For ci = 0 To app.Children.Count - 1
  If app.Children(CLng(ci)).Children.Count > 0 Then
    If app.Children(CLng(ci)).Children(0).Info.SystemName = "T76" Then Set c = app.Children(CLng(ci))
  End If
Next
Set s = c.Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Or CLng(WScript.Arguments(0)) = 0 Then WScript.Echo "ABBRUCH": WScript.Quit 2
prog = UCase(WScript.Arguments(1))
If prog <> "Z_KOI_REICHW_OPT" And prog <> "Z_KOI_REICHW_VGL" Then WScript.Echo "ABBRUCH: nicht erlaubt " & prog: WScript.Quit 2
On Error Resume Next
If WScript.Arguments(2) = "start" Then
  s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE38"
  s.FindById("wnd[0]").SendVKey 0
  WScript.Sleep 2000
  s.FindById("wnd[0]/usr/ctxtRS38M-PROGRAMM").Text = prog
  s.FindById("wnd[0]").SendVKey 14
  WScript.Sleep 2500
Else
  If InStr(s.FindById("wnd[1]/usr").Text & s.FindById("wnd[1]").Text, "") = 0 Then WScript.Quit 2
  s.FindById("wnd[1]").SendVKey 0
  WScript.Sleep 2500
End If
WScript.Echo "FENSTER0=" & s.FindById("wnd[0]").Text
WScript.Echo "SBAR=" & s.FindById("wnd[0]/sbar").Text & " TYP=" & s.FindById("wnd[0]/sbar").MessageType
For i = 1 To 2
  Err.Clear
  WScript.Echo "FENSTER" & i & "=" & s.FindById("wnd[" & i & "]").Text
  If Err.Number = 0 Then
    For Each ctl In s.FindById("wnd[" & i & "]/usr").Children
      WScript.Echo "  " & ctl.Id & " | " & ctl.Type & " | " & ctl.Text
    Next
    For Each ctl In s.FindById("wnd[" & i & "]/tbar[0]").Children
      WScript.Echo "  " & ctl.Id & " | " & ctl.Text & " | " & ctl.Tooltip
    Next
  End If
Next
