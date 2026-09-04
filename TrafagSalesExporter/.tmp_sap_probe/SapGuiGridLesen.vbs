Option Explicit
' Liest ein ALV-Grid (GuiShell vom Typ GridView) vollstaendig aus.
'
' Ein ALV-Grid besteht nicht aus GuiLabel-Elementen. Der Weg ueber
' Get-SapList.ps1 findet dort nichts und meldet "Keine Listenzeilen gefunden".
' Gelesen wird stattdessen ueber RowCount, ColumnOrder und GetCellValue.
' Gebraucht zum Beispiel in der Versionsverwaltung von SE38.
'
' Aufruf: SapGuiGridLesen.vbs <TX> [GridId] [MaxZeilen]
' Ohne GridId werden nur die gefundenen Grid-Ids gemeldet.

Dim tx, gridid, maxz, a, app, c, s, i, j, found

tx = UCase(WScript.Arguments(0))
If WScript.Arguments.Count > 1 Then gridid = WScript.Arguments(1) Else gridid = ""
If WScript.Arguments.Count > 2 Then maxz = CLng(WScript.Arguments(2)) Else maxz = 200

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
If Not found Then
  WScript.Echo "KEINE_SITZUNG=" & tx
  WScript.Quit 2
End If

If gridid = "" Then
  Suche s.FindById("wnd[0]")
  WScript.Quit 0
End If

Dim grid, spalten, sp, z, zeile, wert, kopf
Set grid = s.FindById(gridid)
Set spalten = grid.ColumnOrder

kopf = ""
For sp = 0 To spalten.Count - 1
  If kopf <> "" Then kopf = kopf & " | "
  kopf = kopf & spalten.ElementAt(CLng(sp))
Next
WScript.Echo "ZEILEN=" & grid.RowCount
WScript.Echo "SPALTEN=" & kopf

For z = 0 To grid.RowCount - 1
  If z >= maxz Then Exit For
  zeile = ""
  For sp = 0 To spalten.Count - 1
    wert = ""
    On Error Resume Next
    wert = grid.GetCellValue(CLng(z), spalten.ElementAt(CLng(sp)))
    If Err.Number <> 0 Then Err.Clear
    On Error GoTo 0
    If zeile <> "" Then zeile = zeile & " | "
    zeile = zeile & wert
  Next
  WScript.Echo z & ": " & zeile
Next
WScript.Quit 0

Sub Suche(el)
  Dim k, kind, typ
  typ = ""
  On Error Resume Next
  typ = el.Type
  If typ = "GuiShell" Then
    If InStr(el.Text, "GridView") > 0 Then WScript.Echo "GRID=" & el.Id
  End If
  If Err.Number <> 0 Then Err.Clear
  If el.ContainerType = True Then
    For k = 0 To el.Children.Count - 1
      Set kind = el.Children(CLng(k))
      Suche kind
    Next
  End If
  On Error GoTo 0
End Sub
