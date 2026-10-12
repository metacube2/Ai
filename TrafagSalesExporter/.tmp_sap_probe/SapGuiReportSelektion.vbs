' Startet einen Report in SE38 bis zum Selektionsbild (F8 im Einstieg). Aufruf: <Sitzung> <Programm>
Set a = GetObject("SAPGUI")
Set s = a.GetScriptingEngine.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Quit 2
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE38"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 1500
s.FindById("wnd[0]/usr/ctxtRS38M-PROGRAMM").Text = WScript.Arguments(1)
s.FindById("wnd[0]").SendVKey 8
WScript.Sleep 2500
WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
