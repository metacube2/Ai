Option Explicit
' Markiert Zeilen in einem ALV-Grid und misst die Markierung zurueck.
'
' Ergaenzung zu SapGuiGridLesen.vbs. Gebraucht zum Beispiel in der
' Versionsverwaltung von SE38, wo "Vergleichen" genau zwei markierte
' Zeilen erwartet.
'
' Aufruf: SapGuiGridMarkieren.vbs <TX> <GridId> <Zeilen>
' Zeilen ist eine kommagetrennte Liste nullbasierter Indizes, etwa "0,1".

Dim tx, gridid, zeilen, a, app, c, s, i, j, found

tx = UCase(WScript.Arguments(0))
gridid = WScript.Arguments(1)
zeilen = WScript.Arguments(2)

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

Dim grid
Set grid = s.FindById(gridid)
grid.SelectedRows = zeilen
WScript.Echo "MARKIERT=" & grid.SelectedRows
WScript.Quit 0
