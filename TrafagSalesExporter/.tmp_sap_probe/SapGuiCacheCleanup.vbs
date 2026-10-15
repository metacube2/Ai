Option Explicit
' Leert den Gateway-Modellcache fuer EIN Modell (/IWFND/CACHE_CLEANUP, nie "Alle Modelle").
' Angelegt 2026-10-05 nach der Erweiterung LogTaSet/LogLiefSet (T76K912658).
'
' Aufruf: SapGuiCacheCleanup.vbs <SitzungsIndex> <Modell-ID, z. B. ZPOWERBI_EINKAUF_MDL>

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
If cT76_ Is Nothing Then WScript.Echo "ABBRUCH: keine T76-Verbindung offen" : WScript.Quit 2
Set s = cT76_.Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Echo "ABBRUCH: nicht T76" : WScript.Quit 2

s.FindById("wnd[0]/tbar[0]/okcd").Text = "/n/IWFND/CACHE_CLEANUP"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2000
s.FindById("wnd[0]/usr/chkALLMODEL").Selected = False
s.FindById("wnd[0]/usr/chkALLPROXY").Selected = False
s.FindById("wnd[0]/usr/ctxtMODELID-LOW").Text = WScript.Arguments(1)
s.FindById("wnd[0]").SendVKey 8
WScript.Sleep 3000
On Error Resume Next
For k = 1 To 2
  Err.Clear
  WScript.Echo "wnd[" & k & "]=" & s.FindById("wnd[" & k & "]").Text
  If Err.Number <> 0 Then Exit For
Next
Err.Clear
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
