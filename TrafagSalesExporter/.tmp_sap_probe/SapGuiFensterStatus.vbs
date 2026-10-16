' NUR LESEN: Fenstertitel wnd[0..2] und Statuszeile einer Sitzung (volle Id), z. B. nach einem Schritt pruefen.
' Angelegt 2026-10-16. Aufruf: SapGuiFensterStatus.vbs </app/con[x]/ses[y]>
Set s = GetObject("SAPGUI").GetScriptingEngine.FindById(WScript.Arguments(0))
On Error Resume Next
For k = 0 To 2
  Err.Clear : t = s.FindById("wnd[" & k & "]").Text
  If Err.Number = 0 Then WScript.Echo "wnd[" & k & "]=" & t
Next
WScript.Echo "SBAR=" & s.FindById("wnd[0]/sbar/pane[0]").Text & " TX=" & s.Info.Transaction & " PROG=" & s.Info.Program
