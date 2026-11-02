' P76-Hilfe fuer das Leeren der Gateway-Caches (Auftrag Ingo 2026-11-02). Nur eigene Sitzungen, nie ses[0].
' Aufrufe:  p76.vbs neu                      -> eigene neue P76-Sitzung oeffnen, Index melden
'           p76.vbs info                     -> alle P76-Sitzungen mit Server und Transaktion
'           p76.vbs ok <ses> <okcode>        -> OK-Code in eigener Sitzung
'           p76.vbs dump <ses> [fenster]     -> Elemente eines Fensters (Id, Typ, Text)
'           p76.vbs press <ses> <id>         -> Knopf druecken
'           p76.vbs check <ses> <id>         -> Ankreuzfeld setzen
'           p76.vbs set <ses> <id> <wert>    -> Feld setzen
'           p76.vbs vkey <ses> <n>           -> Funktionstaste
'           p76.vbs select <ses> <id>        -> Element auswaehlen (Listenzeile, Radiobutton)
Option Explicit
Dim app, con, i, cmd, ses, idx
Set app = GetObject("SAPGUI").GetScriptingEngine
Set con = Nothing
For i = 0 To app.Children.Count - 1
  If app.Children(CInt(i)).Children.Count > 0 Then
    If app.Children(CInt(i)).Children(0).Info.SystemName = "P76" Then Set con = app.Children(CInt(i))
  End If
Next
If con Is Nothing Then WScript.Echo "Keine P76-Verbindung": WScript.Quit 1
cmd = LCase(WScript.Arguments(0))

Sub Status(s)
  WScript.Echo "SYSTEM=" & s.Info.SystemName & " SERVER=" & s.Info.ApplicationServer & " TX=" & s.Info.Transaction & " FENSTER=" & s.ActiveWindow.Text
  WScript.Echo "SBAR=" & s.findById("wnd[0]/sbar").Text
End Sub

Sub Dump(o, tiefe)
  Dim k
  On Error Resume Next
  WScript.Echo o.Id & " [" & o.Type & "] " & o.Text
  If tiefe > 0 Then
    For k = 0 To o.Children.Count - 1
      Dump o.Children(CInt(k)), tiefe - 1
    Next
  End If
End Sub

If cmd = "neu" Then
  Dim n0
  n0 = con.Children.Count
  con.Children(0).CreateSession
  WScript.Sleep 3000
  WScript.Echo "P76 Sitzungen vorher=" & n0 & " nachher=" & con.Children.Count & " neue Index=" & (con.Children.Count - 1)
  WScript.Quit 0
End If
If cmd = "info" Then
  For i = 0 To con.Children.Count - 1
    WScript.Echo "ses[" & i & "] SERVER=" & con.Children(CInt(i)).Info.ApplicationServer & " TX=" & con.Children(CInt(i)).Info.Transaction & " ID=" & con.Children(CInt(i)).Id
  Next
  WScript.Quit 0
End If

idx = CInt(WScript.Arguments(1))
If idx = 0 Then WScript.Echo "ABBRUCH: ses[0] ist Ingos Sitzung": WScript.Quit 1
Set ses = con.Children(CInt(idx))
If ses.Info.SystemName <> "P76" Then WScript.Echo "ABBRUCH: nicht P76": WScript.Quit 1

Select Case cmd
  Case "ok"
    ses.findById("wnd[0]/tbar[0]/okcd").Text = WScript.Arguments(2)
    ses.findById("wnd[0]").sendVKey 0
  Case "dump"
    Dim w
    w = "wnd[0]"
    If WScript.Arguments.Count > 2 Then w = WScript.Arguments(2)
    Dump ses.findById(w), 6
  Case "press"
    ses.findById(WScript.Arguments(2)).press
  Case "check"
    ses.findById(WScript.Arguments(2)).Selected = True
    WScript.Echo "SELECTED=" & ses.findById(WScript.Arguments(2)).Selected
  Case "set"
    ses.findById(WScript.Arguments(2)).Text = WScript.Arguments(3)
  Case "vkey"
    ses.findById("wnd[0]").sendVKey CInt(WScript.Arguments(2))
  Case "select"
    ses.findById(WScript.Arguments(2)).SetFocus
    ses.findById(WScript.Arguments(2)).Select
  Case "grid"
    Dim g, r, c, line
    Set g = ses.findById(WScript.Arguments(2))
    WScript.Echo "ZEILEN=" & g.RowCount
    For r = 0 To g.RowCount - 1
      line = r & ":"
      For c = 0 To g.ColumnOrder.Count - 1
        line = line & " " & g.ColumnOrder(CInt(c)) & "=" & g.GetCellValue(CLng(r), g.ColumnOrder(CInt(c)))
      Next
      WScript.Echo Left(line, 300)
    Next
  Case "gridrow"
    ses.findById(WScript.Arguments(2)).selectedRows = WScript.Arguments(3)
    ses.findById(WScript.Arguments(2)).currentCellRow = CLng(WScript.Arguments(3))
  Case "tips"
    Dim tb, b
    Set tb = ses.findById("wnd[0]/tbar[1]")
    For b = 0 To tb.Children.Count - 1
      WScript.Echo tb.Children(CInt(b)).Id & " | " & tb.Children(CInt(b)).Tooltip
    Next
End Select
Status ses
