Option Explicit
Dim app, c, ci, s, g, r, cols, k, line
Set app = GetObject("SAPGUI").GetScriptingEngine
For ci = 0 To app.Children.Count - 1
  If app.Children(CLng(ci)).Children.Count > 0 Then
    If app.Children(CLng(ci)).Children(0).Info.SystemName = "T76" Then Set c = app.Children(CLng(ci))
  End If
Next
Set s = c.Children(CLng(WScript.Arguments(0)))
Set g = s.FindById("wnd[0]/shellcont/shell/shellcont[0]/shell/shellcont[0]/shell")
Set cols = g.ColumnOrder
For r = 0 To g.RowCount - 1
  line = ""
  For k = 0 To cols.Count - 1
    line = line & g.GetCellValue(r, cols(k)) & " | "
  Next
  WScript.Echo line
Next
