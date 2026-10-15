Option Explicit
' Oeffnet eine DATENBANKTABELLE in SE11 im AENDERN-Modus (F6) und meldet den Zustand.
' Variante von SapGuiSe11Aendern.vbs, angelegt 2026-10-15 fuer ZPOWERBI_KDAUF (ZC23 Materialanteil).
' Angelegt 2026-10-05 fuer die Erweiterung ZSTR_LOG_TA / ZSTR_LOG_LIEF (Logistik live).
'
' Aufruf: SapGuiSe11TabelleAendern.vbs <SitzungsIndex> <Tabelle>

Dim a, app, c, s, typ, t
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
' Verbindung T76 suchen statt fest die erste zu nehmen (2026-10-15: P76 war Verbindung 0)
Dim ci_, c_
Set c = Nothing
For ci_ = 0 To app.Children.Count - 1
  Set c_ = app.Children(CLng(ci_))
  If c_.Children.Count > 0 Then
    If c_.Children(0).Info.SystemName = "T76" Then Set c = c_
  End If
Next
If c Is Nothing Then
  WScript.Echo "ABBRUCH: keine T76-Verbindung offen"
  WScript.Quit 2
End If
Set s = c.Children(CLng(WScript.Arguments(0)))
typ = WScript.Arguments(1)

If s.Info.SystemName <> "T76" Then
  WScript.Echo "ABBRUCH: Sitzung ist nicht T76 (" & s.Info.SystemName & ")"
  WScript.Quit 2
End If

s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE11"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2000
s.FindById("wnd[0]/usr/radRSRD1-TBMA").Select
s.FindById("wnd[0]/usr/ctxtRSRD1-TBMA_VAL").Text = typ
s.FindById("wnd[0]").SendVKey 6
WScript.Sleep 2500

On Error Resume Next
t = s.FindById("wnd[1]").Text
If Err.Number = 0 Then WScript.Echo "DIALOG=" & t
Err.Clear
WScript.Echo "TITEL=" & s.FindById("wnd[0]").Text
WScript.Echo "STATUS=" & s.FindById("wnd[0]/sbar").Text
Dim tbl
Set tbl = s.FindById("wnd[0]/usr/tabsTAB_STRIP/tabpDEF/ssubTS_SCREEN:SAPLSD41:2201/tblSAPLSD41TC0")
If Err.Number = 0 Then
  WScript.Echo "TABELLE_ZEILEN=" & tbl.RowCount & " SICHTBAR=" & tbl.VisibleRowCount
  WScript.Echo "ERSTE_ZELLE_EDITIERBAR=" & tbl.GetCell(0, 0).Changeable
Else
  WScript.Echo "TABELLE nicht gefunden: " & Err.Description
End If
