Option Explicit
' Markiert Zeilen einer GuiTableControl (nicht ALV-Grid) und meldet zurueck,
' welche wirklich markiert sind.
'
' Aufruf: SapGuiTabelleMarkieren.vbs <TX> <TabellenId> [<Zeile>|alle]
' Ohne dritten Parameter werden alle sichtbaren Zeilen markiert.

Dim tx, tid, wahl, a, app, c, s, i, j, found

tx = UCase(WScript.Arguments(0))
tid = WScript.Arguments(1)
If WScript.Arguments.Count > 2 Then wahl = LCase(WScript.Arguments(2)) Else wahl = "alle"

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = tx And s.Info.SystemName = "T76" And s.Info.Client = "100" Then
      found = True
      Exit For
    End If
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2

Dim tbl, r, mark
Set tbl = s.FindById(tid)
For r = 0 To tbl.RowCount - 1
  On Error Resume Next
  Err.Clear
  If wahl = "alle" Or CStr(r) = wahl Then
    tbl.GetAbsoluteRow(r).Selected = True
  End If
  mark = "?"
  mark = CStr(tbl.GetAbsoluteRow(r).Selected)
  If Err.Number <> 0 Then mark = "keine Zeile"
  On Error GoTo 0
  WScript.Echo "ZEILE=" & r & " MARKIERT=" & mark
Next
WScript.Quit 0
