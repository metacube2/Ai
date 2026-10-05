Option Explicit
' Oeffnet einen Datentyp (Struktur) in SE11 im AENDERN-Modus (F6) und meldet den Zustand.
' Angelegt 2026-10-05 fuer die Erweiterung ZSTR_LOG_TA / ZSTR_LOG_LIEF (Logistik live).
'
' Aufruf: SapGuiSe11Aendern.vbs <SitzungsIndex> <Datentyp>

Dim a, app, c, s, typ, t
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set c = app.Children(0)
Set s = c.Children(CLng(WScript.Arguments(0)))
typ = WScript.Arguments(1)

If s.Info.SystemName <> "T76" Then
  WScript.Echo "ABBRUCH: Sitzung ist nicht T76 (" & s.Info.SystemName & ")"
  WScript.Quit 2
End If

s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE11"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2000
s.FindById("wnd[0]/usr/radRSRD1-DDTYPE").Select
s.FindById("wnd[0]/usr/ctxtRSRD1-DDTYPE_VAL").Text = typ
s.FindById("wnd[0]").SendVKey 6
WScript.Sleep 2500

On Error Resume Next
t = s.FindById("wnd[1]").Text
If Err.Number = 0 Then WScript.Echo "DIALOG=" & t
Err.Clear
WScript.Echo "TITEL=" & s.FindById("wnd[0]").Text
WScript.Echo "STATUS=" & s.FindById("wnd[0]/sbar").Text
Dim tbl
Set tbl = s.FindById("wnd[0]/usr/tabsTAB_STRIP/tabpDEF/ssubTS_SCREEN:SAPLSD41:2301/tblSAPLSD41TC0")
If Err.Number = 0 Then
  WScript.Echo "TABELLE_ZEILEN=" & tbl.RowCount & " SICHTBAR=" & tbl.VisibleRowCount
  WScript.Echo "ERSTE_ZELLE_EDITIERBAR=" & tbl.GetCell(0, 0).Changeable
Else
  WScript.Echo "TABELLE nicht gefunden: " & Err.Description
End If
