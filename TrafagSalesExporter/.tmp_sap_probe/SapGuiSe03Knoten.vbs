' SE03-Baumknoten per Doppelklick starten, nur in eigener T76-Sitzung (Verbindung 0 muss T76 sein).
' Knotenschluessel = Reportname, z. B. RSWBO099 (Knoten lesen: SapGuiBaumLesenSitzung.vbs). Angelegt 2026-11-06.
' Aufruf: SapGuiSe03Knoten.vbs <SesIdx> <Knoten>
Set app = GetObject("SAPGUI").GetScriptingEngine
Set s = app.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Or s.Info.Client <> "100" Then WScript.Echo "nicht T76/100" : WScript.Quit 2
If CLng(WScript.Arguments(0)) = 0 Then WScript.Echo "ses[0] ist Ingos Sitzung" : WScript.Quit 2
Set baum = s.FindById("wnd[0]/usr/cntlSPLITTER_CONTAINER/shellcont/shellcont/shell/shellcont[0]/shell")
baum.SelectNode WScript.Arguments(1)
baum.DoubleClickNode WScript.Arguments(1)
WScript.Sleep 2500
WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text & " TX=" & s.Info.Transaction & " PROG=" & s.Info.Program & " DYNPRO=" & s.Info.ScreenNumber
