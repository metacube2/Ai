' NUR LESEN: erstes ALV-Grid (GridView) einer Sitzung vollstaendig ausgeben, Spalten in Anzeigereihenfolge.
' Angelegt 2026-10-16. Aufruf: SapGuiGridAusgeben.vbs <volle Sitzungs-Id, z. B. /app/con[0]/ses[1]>
Set s = GetObject("SAPGUI").GetScriptingEngine.FindById(WScript.Arguments(0))
Set g = Nothing
Sub Find(o, d)
  Dim i
  On Error Resume Next
  If g Is Nothing Then
    If o.Type = "GuiShell" Then If o.SubType = "GridView" Then Set g = o
  End If
  If d > 10 Then Exit Sub
  For i = 0 To o.Children.Count - 1 : Find o.Children(CLng(i)), d + 1 : Next
End Sub
Find s.FindById("wnd[0]/usr"), 0
If g Is Nothing Then WScript.Echo "KEIN GRID: " & s.FindById("wnd[0]/sbar/pane[0]").Text : WScript.Quit 1
Set cc = g.ColumnOrder
h = "" : For x = 0 To cc.Count - 1 : h = h & cc(CLng(x)) & " | " : Next
WScript.Echo "ZEILEN=" & g.RowCount & " :: " & h
For r = 0 To g.RowCount - 1
  If r Mod 20 = 0 Then g.FirstVisibleRow = r
  l = ""
  For x = 0 To cc.Count - 1 : l = l & Trim(g.GetCellValue(r, cc(CLng(x)))) & " | " : Next
  WScript.Echo l
Next
