Option Explicit
' Startet einen Report aus SE38 (F8) und fuehrt das Selektionsbild mit den Vorschlagswerten aus (F8).
' Angelegt 2026-10-05 (Z_LOG_KAP_TEST). Ergebnis danach mit Get-SapList.ps1 -Transaktion SE38 lesen.
'
' Aufruf: SapGuiReportStarten.vbs <SitzungsIndex> <Programm>

Dim a, app, s
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set s = app.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Echo "ABBRUCH: nicht T76" : WScript.Quit 2

s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE38"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2000
s.FindById("wnd[0]/usr/ctxtRS38M-PROGRAMM").Text = WScript.Arguments(1)
s.FindById("wnd[0]").SendVKey 8
WScript.Sleep 2500
WScript.Echo "SELEKTION=" & s.FindById("wnd[0]").Text
s.FindById("wnd[0]").SendVKey 8
WScript.Sleep 8000
WScript.Echo "LISTE=" & s.FindById("wnd[0]").Text
On Error Resume Next
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
