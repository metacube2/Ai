' RSWBO052: Funktionsgruppe waehlen, ausfuehren, Ergebniszeile per Doppelklick oeffnen, Dialog zeigen.
' Aufruf: <Sitzung> <Funktionsgruppe>
Set a = GetObject("SAPGUI")
Set s = a.GetScriptingEngine.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Quit 2
s.FindById("wnd[0]/usr/chkCHECK2").Selected = True
s.FindById("wnd[0]/usr/ctxtOBJNAME2").Text = WScript.Arguments(1)
s.FindById("wnd[0]").SendVKey 8
WScript.Sleep 2500
WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
On Error Resume Next
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
