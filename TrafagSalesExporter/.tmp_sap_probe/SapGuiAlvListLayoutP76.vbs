' Klassischer ALV-Listen-Layoutdialog (SAPLSKBH 0810) in einer EIGENEN P76-Sitzung.
' Aufruf: layout_dialog.vbs <SesIndex> list
'         layout_dialog.vbs <SesIndex> show "<Spaltentext>"   (aus "ausgeblendete Felder" in die Liste)
'         layout_dialog.vbs <SesIndex> hide "<Spaltentext>"   (aus der Liste Zeile 1 ausblenden)
' Bricht ab bei Sitzung 0, fremdem System oder wenn kein Dialog "Layout ??ndern" offen ist.
Option Explicit
Dim app, con, i, s, w, cmd, ziel, tblW, tblF, r, txt, gefunden, pfadW
Set app = GetObject("SAPGUI").GetScriptingEngine
Set con = Nothing
For i = 0 To app.Children.Count - 1
  If app.Children(CLng(i)).Children.Count > 0 Then
    If app.Children(CLng(i)).Children(0).Info.SystemName = "P76" Then Set con = app.Children(CLng(i))
  End If
Next
If con Is Nothing Then WScript.Echo "KEINE_P76": WScript.Quit 2
If CLng(WScript.Arguments(0)) = 0 Then WScript.Echo "ABBRUCH Sitzung 0": WScript.Quit 2
Set s = con.Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "P76" Then WScript.Echo "ABBRUCH System": WScript.Quit 2
Set w = s.FindById("wnd[1]")
If InStr(w.Text, "Layout") = 0 Then WScript.Echo "ABBRUCH kein Layoutdialog: " & w.Text: WScript.Quit 2
cmd = WScript.Arguments(1)
pfadW = "wnd[1]/usr/tabsTS_LINES/tabpLI01/ssubSUB810:SAPLSKBH:0810/tblSAPLSKBHTC_WRITE_LIST"

If cmd = "list" Then
  Set tblW = s.FindById(pfadW)
  WScript.Echo "ANGEZEIGT (" & tblW.RowCount & "):"
  For r = 0 To tblW.VisibleRowCount - 1
    On Error Resume Next
    txt = s.FindById(pfadW & "/txtGT_WRITE_LIST-SELTEXT[0," & r & "]").Text
    If Err.Number = 0 Then WScript.Echo "  " & r & ": " & txt
    Err.Clear
    On Error GoTo 0
  Next
  Set tblF = s.FindById("wnd[1]/usr/tblSAPLSKBHTC_FIELD_LIST")
  WScript.Echo "AUSGEBLENDET (" & tblF.RowCount & "), sichtbar " & tblF.VisibleRowCount
  Dim start
  For start = 0 To tblF.RowCount - 1 Step tblF.VisibleRowCount
    tblF.VerticalScrollbar.Position = start
    Set tblF = s.FindById("wnd[1]/usr/tblSAPLSKBHTC_FIELD_LIST")
    For r = 0 To tblF.VisibleRowCount - 1
      On Error Resume Next
      txt = tblF.GetCell(r, 0).Text
      If Err.Number = 0 And Len(Trim(txt)) > 0 Then WScript.Echo "  " & (start + r) & ": " & txt
      Err.Clear
      On Error GoTo 0
    Next
  Next
  tblF.VerticalScrollbar.Position = 0
  WScript.Quit 0
End If

ziel = WScript.Arguments(2)
gefunden = False
If cmd = "show" Then
  Set tblF = s.FindById("wnd[1]/usr/tblSAPLSKBHTC_FIELD_LIST")
  For start = 0 To tblF.RowCount - 1 Step tblF.VisibleRowCount
    tblF.VerticalScrollbar.Position = start
    Set tblF = s.FindById("wnd[1]/usr/tblSAPLSKBHTC_FIELD_LIST")
    For r = 0 To tblF.VisibleRowCount - 1
      On Error Resume Next
      txt = tblF.GetCell(r, 0).Text
      If Err.Number = 0 And Trim(txt) = ziel Then
        tblF.GetAbsoluteRow(start + r).Selected = True
        gefunden = True
      End If
      Err.Clear
      On Error GoTo 0
      If gefunden Then Exit For
    Next
    If gefunden Then Exit For
  Next
  If Not gefunden Then WScript.Echo "NICHT_GEFUNDEN " & ziel: WScript.Quit 3
  s.FindById("wnd[1]/usr/btnAPP_WL_SING").Press
  WScript.Echo "EINGEBLENDET " & ziel
ElseIf cmd = "hide" Then
  Set tblW = s.FindById(pfadW)
  For r = 0 To tblW.VisibleRowCount - 1
    On Error Resume Next
    txt = s.FindById(pfadW & "/txtGT_WRITE_LIST-SELTEXT[0," & r & "]").Text
    If Err.Number = 0 And Trim(txt) = ziel Then
      tblW.GetAbsoluteRow(r).Selected = True
      gefunden = True
    End If
    Err.Clear
    On Error GoTo 0
    If gefunden Then Exit For
  Next
  If Not gefunden Then WScript.Echo "NICHT_GEFUNDEN " & ziel: WScript.Quit 3
  s.FindById("wnd[1]/usr/btnAPP_FL_SING").Press
  WScript.Echo "AUSGEBLENDET " & ziel
End If

