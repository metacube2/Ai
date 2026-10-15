Option Explicit
' Startet die Aktivierung einer Klasse vom SE24-Einstiegsbild (Strg+F3 = tbar[1]/btn[27]).
' Danach steht der Dialog "Inaktive Objekte" offen; markiert und aktiviert wird mit
' SapGuiInaktiveMarkieren.vbs. Angelegt 2026-10-05 (Logistik live, T76K912658).
'
' Aufruf: SapGuiSe24AktivierenStart.vbs <SitzungsIndex> <Klasse>

Dim a, app, s, k
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
' Verbindung T76 suchen statt fest die erste zu nehmen (2026-10-15: P76 war Verbindung 0)
Dim ci_, c_, cT76_
Set cT76_ = Nothing
For ci_ = 0 To app.Children.Count - 1
  Set c_ = app.Children(CLng(ci_))
  If c_.Children.Count > 0 Then
    If c_.Children(0).Info.SystemName = "T76" Then Set cT76_ = c_
  End If
Next
If cT76_ Is Nothing Then
  WScript.Echo "ABBRUCH: keine T76-Verbindung offen"
  WScript.Quit 2
End If
Set s = cT76_.Children(CLng(WScript.Arguments(0)))
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
