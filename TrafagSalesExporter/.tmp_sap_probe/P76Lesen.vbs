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
  before = c.Children.Count
  Set s = c.Children(CLng(before - 1))
  s.FindById("wnd[0]/tbar[0]/okcd").Text = "/o" & tx
  s.FindById("wnd[0]").SendVKey 0
  For k = 1 To 30
    WScript.Sleep 300
    If c.Children.Count > before Then Exit For
  Next
  If c.Children.Count <= before Then WScript.Echo "KEINE_NEUE_SITZUNG" : WScript.Quit 3
  Set s = c.Children(CLng(c.Children.Count - 1))
  WScript.Sleep 800
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
