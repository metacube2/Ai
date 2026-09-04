Option Explicit
' Sucht in einer GuiTableControl seitenweise nach einem Text und meldet die
' gefundene Zeile. Optional wird der Cursor dorthin gesetzt und eine Taste gesendet.
'
' Tabellen wie die Bedarfs-/Bestandsliste in MD04 zeigen nur wenige Zeilen
' gleichzeitig; ein Dump sieht dann so aus, als gaebe es die gesuchte Zeile nicht.
'
' Aufruf: SapGuiTabelleSuchen.vbs <TX> <TabellenId> <Suchtext> [<Feldname>] [<VKey>]
'   Feldname: Name des Textfelds in der Zeile, Standard MDEZ-EXTRA
'   VKey:     wenn angegeben, wird der Cursor gesetzt und die Taste gesendet

Dim tx, tid, such, feld, vkey, a, app, c, s, i, j, found

tx = UCase(WScript.Arguments(0))
tid = WScript.Arguments(1)
such = UCase(WScript.Arguments(2))
If WScript.Arguments.Count > 3 Then feld = WScript.Arguments(3) Else feld = "MDEZ-EXTRA"
If WScript.Arguments.Count > 4 Then vkey = CLng(WScript.Arguments(4)) Else vkey = -1

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

Dim tbl, pos, r, wert, maxpos, treffer
Set tbl = s.FindById(tid)
maxpos = tbl.VerticalScrollbar.Maximum
treffer = False

For pos = 0 To maxpos
  Set tbl = s.FindById(tid)
  tbl.VerticalScrollbar.Position = pos
  WScript.Sleep 250
  Set tbl = s.FindById(tid)
  For r = 0 To tbl.RowCount - 1
    wert = ""
    On Error Resume Next
    wert = s.FindById(tid & "/txt" & feld & "[5," & r & "]").Text
    On Error GoTo 0
    If wert <> "" And InStr(UCase(wert), such) > 0 Then
      WScript.Echo "GEFUNDEN Position=" & pos & " Zeile=" & r & " Wert=" & Trim(wert)
      treffer = True
      If vkey >= 0 Then
        s.FindById(tid & "/txt" & feld & "[5," & r & "]").SetFocus
        WScript.Sleep 300
        s.FindById("wnd[0]").SendVKey vkey
        WScript.Sleep 2500
        WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
        On Error Resume Next
        WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
        On Error GoTo 0
      End If
      Exit For
    End If
  Next
  If treffer Then Exit For
Next

If Not treffer Then
  WScript.Echo "NICHT GEFUNDEN: " & such & " (Positionen 0 bis " & maxpos & ")"
  WScript.Quit 3
End If
WScript.Quit 0
