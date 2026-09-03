Option Explicit

Dim SapGuiAuto, application, connection, session, grid, i, j, found, columns, col
Set SapGuiAuto = GetObject("SAPGUI")
Set application = SapGuiAuto.GetScriptingEngine
found = False
For i = 0 To application.Children.Count - 1
  Set connection = application.Children(CLng(i))
  For j = 0 To connection.Children.Count - 1
    Set session = connection.Children(CLng(j))
    If UCase(session.Info.Transaction) = "SE19" And session.Info.SystemName = "T76" And session.Info.Client = "100" Then
      found = True
      Exit For
    End If
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2
WScript.Echo "Dialog=" & session.FindById("wnd[1]").Text

Set grid = session.FindById("wnd[1]/usr/cntlG_CC_ALV_ENH_SHOW_CREATE/shellcont/shell")
WScript.Echo "Rows=" & grid.RowCount & " Columns=" & grid.ColumnCount
Set columns = grid.ColumnOrder
For Each col In columns
  WScript.Echo "Column=" & col
Next
For i = 0 To grid.RowCount - 1
  WScript.Echo "ROW " & i
  For Each col In columns
    WScript.Echo "  " & col & "=" & grid.GetCellValue(i, col)
  Next
Next
