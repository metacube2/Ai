' Markiert Zeile n eines ALV-Grids und oeffnet sie per Doppelklick. Aufruf: <Sitzung> <GridId> <Zeile>
Set a = GetObject("SAPGUI")
Set s = a.GetScriptingEngine.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Quit 2
Set g = s.FindById(WScript.Arguments(1))
g.CurrentCellRow = CLng(WScript.Arguments(2))
g.SelectedRows = WScript.Arguments(2)
g.DoubleClickCurrentCell
WScript.Sleep 3000
WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
