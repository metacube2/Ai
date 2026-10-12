Option Explicit
' Oeffnet einen Funktionsbaustein in SE37 im Aendern-Modus (Sitzungsindex). Angelegt 2026-10-12.
' Aufruf: SapGuiFbOeffnen.vbs <SitzungsIndex> <Baustein>
Dim a, s
Set a = GetObject("SAPGUI")
Set s = a.GetScriptingEngine.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Quit 2
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE37"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2000
s.FindById("wnd[0]/usr/ctxtRS38L-NAME").Text = WScript.Arguments(1)
s.FindById("wnd[0]/usr/btnBUT4").Press
WScript.Sleep 2500
WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
