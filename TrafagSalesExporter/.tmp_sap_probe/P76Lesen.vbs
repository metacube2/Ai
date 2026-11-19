' Seit 2026-10-13, Doku saptasks/BERECHTIGUNG_RAHMENVERTRAG_AT_2026-10-13.md Abschnitt 5.
' Nur lesend in P76/100. Erstes Argument: "neu:<TX>" (oeffnet neue Sitzung, nur Anzeige-TX)
' oder "ses:<n>" (vorhandene Sitzung, die eine Anzeige-TX zeigt). Danach Aktionen:
'   set:<id>=<wert>   key:<vkey>   press:<id>   sleep:<ms>   dump   grid:<id>
Option Explicit
Dim allowed, a, app, c, s, i, k, before, arg, p, first, tx
allowed = "|SE16|SE16N|SUIM|S_BCE_68001425|S_BCE_68001426|PFCG|SU01D|ME33K|VA43|"
Set a = GetObject("SAPGUI") : Set app = a.GetScriptingEngine
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  If c.Children.Count > 0 Then
    If c.Children(0).Info.SystemName = "P76" And c.Children(0).Info.Client = "100" Then Exit For
  End If
  Set c = Nothing
Next
If c Is Nothing Then WScript.Echo "KEINE_P76" : WScript.Quit 2
first = WScript.Arguments(0)
If Left(first, 4) = "neu:" Then
  tx = UCase(Mid(first, 5))
  If InStr(allowed, "|" & tx & "|") = 0 Then WScript.Echo "NICHT_ERLAUBT" : WScript.Quit 4
  ' 2026-11-11: neue Sitzung ueber den Id-Vergleich vorher/nachher bestimmen. Die Collection ist nicht nach
  ' Erzeugung geordnet; Children(Count-1) traf die VT11-Sitzung des Benutzers.
  Dim vorherIds, neuId, kand
  before = c.Children.Count
  vorherIds = "|"
  For k = 0 To before - 1
    vorherIds = vorherIds & c.Children(CLng(k)).Id & "|"
  Next
  ' 2026-11-11: CreateSession statt "/o<TX>" ins OK-Feld einer Benutzersitzung. Die Benutzersitzung wird nur als
  ' Ausgangspunkt benutzt, nichts wird dort eingegeben. Ist sie gerade beschaeftigt, kurz warten.
  ' Ausgangspunkt moeglichst nicht ses[0]
  Set s = c.Children(CLng(0))
  For k = 0 To before - 1
    If Right(c.Children(CLng(k)).Id, 7) <> "/ses[0]" Then Set s = c.Children(CLng(k)) : Exit For
  Next
  For k = 1 To 40
    If Not s.Busy Then Exit For
    WScript.Sleep 250
  Next
  If s.Busy Then WScript.Echo "KEINE_NEUE_SITZUNG (Ausgangssitzung beschaeftigt)" : WScript.Quit 3
  s.CreateSession
  For k = 1 To 30
    WScript.Sleep 300
    If c.Children.Count > before Then Exit For
  Next
  If c.Children.Count <> before + 1 Then WScript.Echo "KEINE_NEUE_SITZUNG vorher=" & before & " nachher=" & c.Children.Count : WScript.Quit 3
  neuId = ""
  For k = 0 To c.Children.Count - 1
    kand = c.Children(CLng(k)).Id
    If InStr(vorherIds, "|" & kand & "|") = 0 Then neuId = kand
  Next
  If neuId = "" Then WScript.Echo "KEINE_NEUE_SITZUNG (keine neue Id gefunden)" : WScript.Quit 3
  Set s = app.FindById(neuId)
  For k = 1 To 40
    WScript.Sleep 250
    If Not s.Busy Then Exit For
  Next
  WScript.Sleep 500
  ' nur in der neuen, eigenen Sitzung tippen; sie muss P76/100 sein und darf kein Popup zeigen
  If s.Info.SystemName <> "P76" Or s.Info.Client <> "100" Then WScript.Echo "NEUE_SITZUNG_NICHT_P76 " & s.Id : WScript.Quit 6
  If s.Children.Count > 1 Then WScript.Echo "NEUE_SITZUNG_POPUP " & s.Id : WScript.Quit 6
  s.FindById("wnd[0]/tbar[0]/okcd").Text = "/n" & tx
  s.FindById("wnd[0]").SendVKey 0
  For k = 1 To 20
    If UCase(s.Info.Transaction) = tx Then Exit For
    WScript.Sleep 300
  Next
  If UCase(s.Info.Transaction) <> tx Then WScript.Echo "NEUE_SITZUNG_FALSCHE_TX " & s.Id & " " & s.Info.Transaction : WScript.Quit 6
Else
  Set s = app.FindById(Mid(first, 5))
  If InStr(allowed, "|" & UCase(s.Info.Transaction) & "|") = 0 And Left(UCase(s.Info.Program), 4) <> "RSUS" And UCase(s.Info.Transaction) <> "SE16" Then WScript.Echo "SITZUNG_NICHT_ANZEIGE " & s.Info.Transaction : WScript.Quit 5
End If
WScript.Echo "SITZUNG=" & s.Id & " TX=" & s.Info.Transaction & " PROG=" & s.Info.Program
For i = 1 To WScript.Arguments.Count - 1
  arg = WScript.Arguments(i)
  If Left(arg, 4) = "set:" Then
    p = InStr(arg, "=")
    s.FindById(Mid(arg, 5, p - 5)).Text = Mid(arg, p + 1)
  ElseIf Left(arg, 3) = "ok:" Then
    If InStr(allowed, "|" & UCase(Mid(arg, 6)) & "|") = 0 Then WScript.Echo "NICHT_ERLAUBT" : WScript.Quit 4
    s.FindById("wnd[0]/tbar[0]/okcd").Text = Mid(arg, 4)
    s.FindById("wnd[0]").SendVKey 0 : WScript.Sleep 1500
  ElseIf Left(arg, 4) = "key:" Then
    s.FindById("wnd[0]").SendVKey CInt(Mid(arg, 5)) : WScript.Sleep 1500
  ElseIf Left(arg, 6) = "press:" Then
    s.FindById(Mid(arg, 7)).Press : WScript.Sleep 1500
  ElseIf Left(arg, 6) = "sleep:" Then
    WScript.Sleep CLng(Mid(arg, 7))
  ElseIf arg = "dump1" Then
    WScript.Echo "W1=" & s.FindById("wnd[1]").Text
    Dump s.FindById("wnd[1]"), 0
  ElseIf Left(arg, 5) = "key1:" Then
    s.FindById("wnd[1]").SendVKey CInt(Mid(arg, 6)) : WScript.Sleep 1500
  ElseIf arg = "dump" Then
    WScript.Echo "TITEL=" & s.FindById("wnd[0]").Text
    WScript.Echo "STATUS=" & s.FindById("wnd[0]/sbar").Text
    Dump s.FindById("wnd[0]/usr"), 0
  ElseIf Left(arg, 5) = "grid:" Then
    Grid s.FindById(Mid(arg, 6))
  End If
Next

Sub Grid(g)
  Dim r, cc, line, cols, n
  Set cols = g.ColumnOrder
  WScript.Echo "ZEILEN=" & g.RowCount
  For r = 0 To g.RowCount - 1
    If r Mod 20 = 0 Then g.FirstVisibleRow = r
    line = ""
    For n = 0 To cols.Count - 1
      line = line & g.GetCellValue(r, cols(n)) & " | "
    Next
    WScript.Echo line
  Next
End Sub

Sub Dump(o, d)
  Dim ch, n
  If d > 12 Then Exit Sub
  On Error Resume Next
  If o.Type = "GuiTextField" Or o.Type = "GuiCTextField" Or o.Type = "GuiComboBox" Or o.Type = "GuiLabel" Or o.Type = "GuiButton" Or o.Type = "GuiCheckBox" Or o.Type = "GuiShell" Then
    WScript.Echo o.Type & " " & o.Id & " = " & o.Text
  End If
  Set ch = o.Children
  If Err.Number = 0 Then
    For n = 0 To ch.Count - 1
      Dump ch(CLng(n)), d + 1
    Next
  End If
  Err.Clear
End Sub
