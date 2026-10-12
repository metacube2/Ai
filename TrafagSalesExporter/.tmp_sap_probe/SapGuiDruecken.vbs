' Drueckt ein Element in einer Sitzung (Index). Aufruf: SapGuiDruecken.vbs <Sitzung> <ElementId>
Set a = GetObject("SAPGUI")
Set s = a.GetScriptingEngine.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Quit 2
s.FindById(WScript.Arguments(1)).Press
WScript.Sleep 2500
WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
