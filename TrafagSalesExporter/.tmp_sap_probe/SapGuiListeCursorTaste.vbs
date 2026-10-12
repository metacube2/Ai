' Setzt den Cursor auf ein Listenelement und drueckt einen Knopf der Anwendungsleiste.
' Aufruf: <Sitzung> <ElementId> <KnopfId>
Set a = GetObject("SAPGUI")
Set s = a.GetScriptingEngine.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Quit 2
s.FindById(WScript.Arguments(1)).SetFocus
s.FindById(WScript.Arguments(2)).Press
WScript.Sleep 2000
On Error Resume Next
For k = 1 To 3
  Err.Clear
  WScript.Echo "wnd[" & k & "]=" & s.FindById("wnd[" & k & "]").Text
  If Err.Number <> 0 Then Exit For
Next
Err.Clear
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
