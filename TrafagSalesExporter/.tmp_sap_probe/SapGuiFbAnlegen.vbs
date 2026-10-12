Option Explicit
' Legt in SE37 eine Funktionsgruppe oder einen Funktionsbaustein an, ueber den Sitzungsindex.
' Danach steht meist der Dialog "Objektkatalogeintrag anlegen" offen -> SapGuiLokalesObjekt.vbs.
' Angelegt 2026-10-12 (ZKDAUF_PAR fuer ZM_OFFENE_KDAUF_OPT).
'
' Aufruf: SapGuiFbAnlegen.vbs <SitzungsIndex> gruppe <Gruppe> <Kurztext>
'         SapGuiFbAnlegen.vbs <SitzungsIndex> baustein <Baustein> <Gruppe> <Kurztext>

Dim a, app, s, k
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set s = app.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Echo "ABBRUCH: nicht T76" : WScript.Quit 2

Sub Melde(stufe)
  On Error Resume Next
  For k = 1 To 3
    Err.Clear
    WScript.Echo stufe & " wnd[" & k & "]=" & s.FindById("wnd[" & k & "]").Text
    If Err.Number <> 0 Then Exit For
  Next
  Err.Clear
  WScript.Echo stufe & " FENSTER=" & s.FindById("wnd[0]").Text
  WScript.Echo stufe & " MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
  On Error GoTo 0
End Sub

s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE37"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2000

If WScript.Arguments(1) = "gruppe" Then
  s.FindById("wnd[0]/mbar/menu[2]/menu[3]/menu[0]").Select
  WScript.Sleep 1200
  Melde "MENUE"
  s.FindById("wnd[1]/usr/ctxtTLIBG-AREA").Text = WScript.Arguments(2)
  s.FindById("wnd[1]/usr/txtTLIBT-AREAT").Text = WScript.Arguments(3)
  s.FindById("wnd[1]/tbar[0]/btn[0]").Press
  WScript.Sleep 1500
  Melde "NACH_SICHERN"
Else
  s.FindById("wnd[0]/usr/ctxtRS38L-NAME").Text = WScript.Arguments(2)
  s.FindById("wnd[0]/usr/btnBUT2").Press
  WScript.Sleep 1500
  Melde "NACH_ANLEGEN"
  s.FindById("wnd[1]/usr/ctxtRS38L-AREA").Text = WScript.Arguments(3)
  s.FindById("wnd[1]/usr/txtTFTIT-STEXT").Text = WScript.Arguments(4)
  s.FindById("wnd[1]/tbar[0]/btn[0]").Press
  WScript.Sleep 2000
  Melde "NACH_SICHERN"
End If
