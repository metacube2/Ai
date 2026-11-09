' ALV-Liste: Cursor auf ein Label setzen und Detail (&ETA) oeffnen, Feld/Wert ausgeben. 2026-11-09.
' Aufruf: SapGuiListeDetail.vbs <Sitzung> <LabelId z.B. lbl[30,3]>
Set app = GetObject("SAPGUI").GetScriptingEngine
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  If c.Children.Count > 0 Then
    If c.Children(0).Info.SystemName = "T76" Then Set s = c.Children(CLng(WScript.Arguments(0)))
  End If
Next
If s.Info.SystemName <> "T76" Then WScript.Quit 2
s.FindById("wnd[0]/usr/" & WScript.Arguments(1)).SetFocus
s.FindById("wnd[0]/tbar[0]/okcd").Text = "&ETA"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2000
On Error Resume Next
WScript.Echo "wnd1=" & s.FindById("wnd[1]").Text
Set u = s.FindById("wnd[1]/usr")
For Each ch In u.Children
  WScript.Echo ch.Id & " : " & ch.Text
  For Each g In ch.Children
    WScript.Echo "  " & g.Id & " : " & g.Text
  Next
Next
