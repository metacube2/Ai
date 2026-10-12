' Sendet eine Taste an ein Fenster einer Sitzung (Index). Aufruf: SapGuiTaste.vbs <Sitzung> <Fenster> <VKey>
Set a = GetObject("SAPGUI")
Set s = a.GetScriptingEngine.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Quit 2
s.FindById("wnd[" & WScript.Arguments(1) & "]").SendVKey CInt(WScript.Arguments(2))
WScript.Sleep 800
WScript.Echo "OK"
