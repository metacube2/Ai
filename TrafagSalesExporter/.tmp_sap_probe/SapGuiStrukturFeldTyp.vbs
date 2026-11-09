Option Explicit
' Aendert in der offenen SE11-Strukturpflege (Aendern-Modus) den Komponententyp EINES vorhandenen Feldes.
' Prueft vorher, dass in der Zielzeile wirklich das genannte Feld steht. Angelegt 2026-11-09 fuer ZC22 (STTXT).
' Aufruf: SapGuiStrukturFeldTyp.vbs <SitzungsIndex> <Zeile0basiert> <FELDNAME> <NEUERTYP>
Dim app, c, s, ci_, c_, basis, tbl, zeile, feld, typ, r, ist
Set app = GetObject("SAPGUI").GetScriptingEngine
Set c = Nothing
For ci_ = 0 To app.Children.Count - 1
  Set c_ = app.Children(CLng(ci_))
  If c_.Children.Count > 0 Then
    If c_.Children(0).Info.SystemName = "T76" Then Set c = c_
  End If
Next
If c Is Nothing Then WScript.Echo "ABBRUCH: keine T76-Verbindung" : WScript.Quit 2
Set s = c.Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Echo "ABBRUCH: nicht T76" : WScript.Quit 2
zeile = CLng(WScript.Arguments(1)) : feld = UCase(WScript.Arguments(2)) : typ = UCase(WScript.Arguments(3))
basis = "wnd[0]/usr/tabsTAB_STRIP/tabpDEF/ssubTS_SCREEN:SAPLSD41:2301/tblSAPLSD41TC0"
Set tbl = s.FindById(basis)
tbl.VerticalScrollbar.Position = zeile
Set tbl = s.FindById(basis)
r = zeile - tbl.VerticalScrollbar.Position
ist = UCase(Trim(s.FindById(basis & "/txtDD03P_D-FIELDNAME[0," & r & "]").Text))
If ist <> feld Then WScript.Echo "ABBRUCH: in Zeile " & zeile & " steht " & ist & ", nicht " & feld : WScript.Quit 3
WScript.Echo "VORHER " & ist & " = " & s.FindById(basis & "/ctxtDD03P_D-ROLLNAME[2," & r & "]").Text
s.FindById(basis & "/ctxtDD03P_D-ROLLNAME[2," & r & "]").Text = typ
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 1500
Set tbl = s.FindById(basis)
r = zeile - tbl.VerticalScrollbar.Position
WScript.Echo "NACHHER " & s.FindById(basis & "/txtDD03P_D-FIELDNAME[0," & r & "]").Text & " = " & s.FindById(basis & "/ctxtDD03P_D-ROLLNAME[2," & r & "]").Text
WScript.Echo "STATUS=" & s.FindById("wnd[0]/sbar").Text
