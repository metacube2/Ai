' Meldet Fenster, Titel und Statusleiste einer T76-Sitzung (nur lesen). Angelegt 2026-11-09.
' Aufruf: SapGuiZustandT76.vbs <SitzungsIndex>
Set app = GetObject("SAPGUI").GetScriptingEngine
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  If c.Children.Count > 0 Then
    If c.Children(0).Info.SystemName = "T76" Then Set s = c.Children(CLng(WScript.Arguments(0)))
  End If
Next
WScript.Echo "TX=" & s.Info.Transaction & " PROG=" & s.Info.Program & " DYN=" & s.Info.ScreenNumber
On Error Resume Next
For w = 0 To 3
  Set f = Nothing
  Set f = s.FindById("wnd[" & w & "]")
  If Not f Is Nothing Then
    WScript.Echo "wnd[" & w & "] " & f.Text
    Set u = s.FindById("wnd[" & w & "]/usr")
    For Each ch In u.Children
      If ch.Text <> "" Then WScript.Echo "   " & ch.Id & " : " & ch.Text
    Next
  End If
Next
WScript.Echo "SBAR=" & s.FindById("wnd[0]/sbar").Text
