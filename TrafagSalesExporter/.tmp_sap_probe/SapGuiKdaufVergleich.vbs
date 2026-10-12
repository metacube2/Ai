Option Explicit
' Startet ZM_OFFENE_KDAUF_CMP in SE38 mit Variante, Werk und Lieferdatum und wartet auf die Liste.
' Angelegt 2026-10-12. Ergebnis danach mit Get-SapList.ps1 -Transaktion SE38 lesen.
' "-" laesst ein Feld leer. Datum als TT.MM.JJJJ.
'
' Aufruf: SapGuiKdaufVergleich.vbs <SitzungsIndex> <Variante|-> <Werk|-> <DatumVon|-> <DatumBis|-> [ProgA|-] [ProgB|-] [bg|zeig|-] [Parallel]

Dim a, app, s, arg
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set s = app.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Echo "ABBRUCH: nicht T76" : WScript.Quit 2

Sub Setze(name, wert)
  If wert = "-" Then Exit Sub
  On Error Resume Next
  Err.Clear
  s.FindById("wnd[0]/usr/ctxt" & name).Text = wert
  If Err.Number <> 0 Then
    Err.Clear
    s.FindById("wnd[0]/usr/txt" & name).Text = wert
  End If
  If Err.Number <> 0 Then WScript.Echo "FELD NICHT GEFUNDEN: " & name
  On Error GoTo 0
End Sub

s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE38"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2000
s.FindById("wnd[0]/usr/ctxtRS38M-PROGRAMM").Text = "ZM_OFFENE_KDAUF_CMP"
s.FindById("wnd[0]").SendVKey 8
WScript.Sleep 2500
WScript.Echo "SELEKTION=" & s.FindById("wnd[0]").Text
Setze "P_VAR", WScript.Arguments(1)
Setze "P_WERK", WScript.Arguments(2)
Setze "S_MBDAT-LOW", WScript.Arguments(3)
Setze "S_MBDAT-HIGH", WScript.Arguments(4)
If WScript.Arguments.Count > 5 Then Setze "P_PRGA", WScript.Arguments(5)
If WScript.Arguments.Count > 6 Then Setze "P_PRGB", WScript.Arguments(6)
If WScript.Arguments.Count > 7 Then
  If WScript.Arguments(7) = "bg" Then s.FindById("wnd[0]/usr/chkP_BG").Selected = True
  If WScript.Arguments(7) = "zeig" Then s.FindById("wnd[0]/usr/chkP_ZEIG").Selected = True
End If
If WScript.Arguments.Count > 8 Then Setze "P_PTASK", WScript.Arguments(8)
WScript.Echo "START=" & Now
' F8 kehrt erst zurueck, wenn der Server die Liste geliefert hat
s.FindById("wnd[0]").SendVKey 8
WScript.Echo "ENDE=" & Now
WScript.Echo "LISTE=" & s.FindById("wnd[0]").Text
On Error Resume Next
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
