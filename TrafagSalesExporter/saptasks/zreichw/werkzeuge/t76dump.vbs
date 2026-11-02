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
If WScript.Arguments.Count > 1 Then
  s.FindById("wnd[0]/tbar[0]/okcd").Text = WScript.Arguments(1)
  s.FindById("wnd[0]").SendVKey 0
  WScript.Sleep 2500
End If
Sub D(o, n)
  Dim k
  On Error Resume Next
  If InStr(o.Id, "/mbar") = 0 And InStr(o.Id, "/tbar") = 0 Then WScript.Echo o.Id & " [" & o.Type & "] " & o.Text
  If n > 0 Then
    For k = 0 To o.Children.Count - 1
      D o.Children(CLng(k)), n - 1
    Next
  End If
End Sub
D s.FindById("wnd[0]"), 6
WScript.Echo "TX=" & s.Info.Transaction
