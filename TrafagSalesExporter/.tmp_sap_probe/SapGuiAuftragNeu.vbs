' Im Dialog "Abfrage transportierbarer Workbench-Auftrag": neuen Auftrag anlegen, Text setzen,
' sichern, Nummer ausgeben und mit Weiter bestaetigen. Aufruf: <Sitzung> <Kurzbeschreibung>
Set a = GetObject("SAPGUI")
Set s = a.GetScriptingEngine.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Quit 2
s.FindById("wnd[1]/tbar[0]/btn[8]").Press
WScript.Sleep 1500
WScript.Echo "DIALOG2=" & s.FindById("wnd[2]").Text
s.FindById("wnd[2]/usr/txtKO013-AS4TEXT").Text = WScript.Arguments(1)
s.FindById("wnd[2]/tbar[0]/btn[0]").Press
WScript.Sleep 2000
WScript.Echo "AUFTRAG=" & s.FindById("wnd[1]/usr/ctxtKO008-TRKORR").Text
s.FindById("wnd[1]/tbar[0]/btn[0]").Press
WScript.Sleep 2000
On Error Resume Next
For k = 1 To 3
  Err.Clear
  WScript.Echo "wnd[" & k & "]=" & s.FindById("wnd[" & k & "]").Text
  If Err.Number <> 0 Then Exit For
Next
Err.Clear
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
