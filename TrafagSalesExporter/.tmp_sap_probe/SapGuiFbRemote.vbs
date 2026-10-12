Set a = GetObject("SAPGUI")
Set s = a.GetScriptingEngine.Children(0).Children(1)
If s.Info.SystemName <> "T76" Then WScript.Quit 2
s.FindById("wnd[0]/usr/tabsFUNC_TAB_STRIP/tabpHEADER/ssubSCREEN_HEADER:SAPLSFUNCTION_BUILDER:3030/radRS38L-REMOTE").Select
WScript.Sleep 800
s.FindById("wnd[0]/tbar[0]/btn[11]").Press
WScript.Sleep 1200
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
WScript.Echo "REMOTE=" & s.FindById("wnd[0]/usr/tabsFUNC_TAB_STRIP/tabpHEADER/ssubSCREEN_HEADER:SAPLSFUNCTION_BUILDER:3030/radRS38L-REMOTE").Selected
On Error Resume Next
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
