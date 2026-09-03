Option Explicit
' Markiert in der COHV-Ergebnisliste alle Zeilen und fuehrt die eingestellte
' Massenbearbeitungsfunktion aus.
'
' Setzt voraus, dass die Selektion bereits gelaufen ist und die Funktion auf dem
' Reiter "Massenbearbeitung" gewaehlt wurde.

Dim a, app, c, s, i, j, found, grid

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = "COHV" And s.Info.SystemName = "T76" And s.Info.Client = "100" Then
      found = True
      Exit For
    End If
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2

If InStr(1, s.FindById("wnd[0]").Text, "Auftragsk", vbTextCompare) = 0 Then
  WScript.Echo "ABBRUCH: keine Ergebnisliste offen, sondern " & s.FindById("wnd[0]").Text
  WScript.Quit 3
End If

Set grid = s.FindById("wnd[0]/usr/cntlCUSTOM/shellcont/shell/shellcont/shell")
WScript.Echo "Zeilen: " & grid.RowCount
grid.SelectAll
WScript.Sleep 1000
WScript.Echo "Markiert: " & grid.SelectedRows

' Massenbearbeitung -> Ausfuehren
s.FindById("wnd[0]/mbar/menu[3]/menu[0]").Select
WScript.Sleep 5000

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "MELDUNGSTYP=" & s.FindById("wnd[0]/sbar").MessageType
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
On Error Resume Next
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
WScript.Quit 0
