Option Explicit
Dim a, app, c, s, grid, i, j, row, found, targetRow
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = "SE19" And s.Info.SystemName = "T76" And s.Info.Client = "100" Then found = True: Exit For
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2

Set grid = s.FindById("wnd[1]/usr/cntlG_CC_ALV_ENH_SHOW_CREATE/shellcont/shell")
targetRow = -1
For row = 0 To grid.RowCount - 1
  If grid.GetCellValue(row, "ENHNAME") = "Z_ZZPRDAT_REL_TEST" And grid.GetCellValue(row, "DEVCLASS") = "$TMP" Then targetRow = row
Next
If targetRow < 0 Then WScript.Quit 4
grid.SetCurrentCell targetRow, "ENHNAME"
grid.SelectedRows = CStr(targetRow)
s.FindById("wnd[1]/tbar[0]/btn[0]").Press
WScript.Sleep 1500
WScript.Echo "MAIN=" & s.FindById("wnd[0]").Text
On Error Resume Next
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
WScript.Echo "STATUS=" & s.FindById("wnd[0]/sbar/pane[0]").Text
