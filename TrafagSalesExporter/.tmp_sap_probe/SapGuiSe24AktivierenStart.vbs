Option Explicit
' Startet die Aktivierung einer Klasse vom SE24-Einstiegsbild (Strg+F3 = tbar[1]/btn[27]).
' Danach steht der Dialog "Inaktive Objekte" offen; markiert und aktiviert wird mit
' SapGuiInaktiveMarkieren.vbs. Angelegt 2026-10-05 (Logistik live, T76K912658).
'
' Aufruf: SapGuiSe24AktivierenStart.vbs <SitzungsIndex> <Klasse>

Dim a, app, s, k
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set s = app.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Echo "ABBRUCH: nicht T76" : WScript.Quit 2

s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE24"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2000
s.FindById("wnd[0]/usr/ctxtSEOCLASS-CLSNAME").Text = WScript.Arguments(1)
s.FindById("wnd[0]/tbar[1]/btn[27]").Press
WScript.Sleep 3000
On Error Resume Next
For k = 1 To 2
  Err.Clear
  WScript.Echo "wnd[" & k & "]=" & s.FindById("wnd[" & k & "]").Text
  If Err.Number <> 0 Then Exit For
Next
Err.Clear
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
