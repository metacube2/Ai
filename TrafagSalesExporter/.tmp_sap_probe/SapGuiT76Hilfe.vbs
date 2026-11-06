' T76-Hilfe (angelegt 2026-11-06, ZZPRDAT Transport buendeln). Muster: SapGuiP76Hilfe.vbs.
' Arbeitet NUR in der selbst erzeugten Sitzung: "neu" merkt sich deren Id in t76_eigene_sitzung.txt
' (neben dem Skript), jeder andere Befehl bricht ab, wenn die Sitzung eine andere Id hat.
' Verbindung wird ueber SystemName T76 / Mandant 100 gesucht, nie ueber Children(0).
' Aufrufe:  t76.vbs neu                         -> eigene neue T76-Sitzung oeffnen, Id merken
'           t76.vbs info                        -> alle T76-Sitzungen mit Transaktion und Id
'           t76.vbs ok <ses> <okcode>           -> OK-Code
'           t76.vbs dump <ses> [fenster] [tiefe]-> Elemente eines Fensters (Id, Typ, Text, Tooltip)
'           t76.vbs press <ses> <id>            -> Knopf druecken
'           t76.vbs set <ses> <id> <wert>       -> Feld setzen (mit SetFocus, Rueckmessung)
'           t76.vbs vkey <ses> <n> [fenster]    -> Funktionstaste
'           t76.vbs select <ses> <id>           -> Element auswaehlen (Menue, Radiobutton, Reiter)
'           t76.vbs check <ses> <id> <0|1>      -> Ankreuzfeld setzen
'           t76.vbs menus <ses>                 -> Menueleiste rekursiv mit Texten
'           t76.vbs tree <ses> <baumId>         -> Baum: Knoten, Text, Spalten
'           t76.vbs treesel <ses> <baumId> <key>-> Knoten markieren
'           t76.vbs treedbl <ses> <baumId> <key>-> Knoten doppelklicken
'           t76.vbs grid <ses> <gridId>         -> ALV-Grid zeilenweise ausgeben
'           t76.vbs gridsel <ses> <gridId> <z>  -> ALV-Grid-Zeilen markieren (nullbasiert, "0,1")
'           t76.vbs zu <ses>                    -> eigene Sitzung mit /i schliessen
Option Explicit
Dim app, con, i, cmd, ses, idx, fso, idFile, eigeneId
Set app = GetObject("SAPGUI").GetScriptingEngine
Set fso = CreateObject("Scripting.FileSystemObject")
idFile = fso.BuildPath(fso.GetParentFolderName(WScript.ScriptFullName), "t76_eigene_sitzung.txt")
Set con = Nothing
For i = 0 To app.Children.Count - 1
  If app.Children(CLng(i)).Children.Count > 0 Then
    If app.Children(CLng(i)).Children(0).Info.SystemName = "T76" And app.Children(CLng(i)).Children(0).Info.Client = "100" Then Set con = app.Children(CLng(i))
  End If
Next
If con Is Nothing Then WScript.Echo "Keine T76/100-Verbindung": WScript.Quit 1
cmd = LCase(WScript.Arguments(0))

Sub Status(s)
  Dim k, t
  On Error Resume Next
  WScript.Echo "SYSTEM=" & s.Info.SystemName & "/" & s.Info.Client & " TX=" & s.Info.Transaction & " PROG=" & s.Info.Program & " DYNPRO=" & s.Info.ScreenNumber
  For k = 0 To 3
    Err.Clear: t = s.findById("wnd[" & k & "]").Text
    If Err.Number = 0 Then WScript.Echo "wnd[" & k & "]=" & t
  Next
  Err.Clear
  WScript.Echo "SBAR(" & s.findById("wnd[0]/sbar").MessageType & ")=" & s.findById("wnd[0]/sbar").Text
End Sub

Sub Dump(o, tiefe)
  Dim k, typ, txt, tt
  typ = "": txt = "": tt = ""
  On Error Resume Next
  typ = o.Type: txt = o.Text: tt = o.Tooltip
  Err.Clear
  WScript.Echo o.Id & " [" & typ & "] " & Left(txt, 80) & " {" & Left(tt, 50) & "}"
  If tiefe > 0 Then
    For k = 0 To o.Children.Count - 1
      Dump o.Children(CLng(k)), tiefe - 1
    Next
  End If
End Sub

If cmd = "neu" Then
  Dim n0, neu
  n0 = con.Children.Count
  con.Children(0).CreateSession
  For i = 1 To 20
    WScript.Sleep 500
    If con.Children.Count > n0 Then Exit For
  Next
  Set neu = con.Children(CLng(con.Children.Count - 1))
  Dim f
  Set f = fso.CreateTextFile(idFile, True)
  f.WriteLine neu.Id
  f.Close
  WScript.Echo "T76 Sitzungen vorher=" & n0 & " nachher=" & con.Children.Count & " neue Index=" & (con.Children.Count - 1) & " ID=" & neu.Id
  WScript.Quit 0
End If
If cmd = "info" Then
  For i = 0 To con.Children.Count - 1
    WScript.Echo "ses[" & i & "] TX=" & con.Children(CLng(i)).Info.Transaction & " ID=" & con.Children(CLng(i)).Id
  Next
  WScript.Quit 0
End If

If Not fso.FileExists(idFile) Then WScript.Echo "ABBRUCH: keine eigene Sitzung gemerkt (erst 'neu')": WScript.Quit 1
eigeneId = Trim(fso.OpenTextFile(idFile).ReadLine)
idx = CLng(WScript.Arguments(1))
If idx = 0 Then WScript.Echo "ABBRUCH: ses[0] ist Ingos Sitzung": WScript.Quit 1
If idx >= con.Children.Count Then WScript.Echo "ABBRUCH: Index zu gross": WScript.Quit 1
Set ses = con.Children(CLng(idx))
If ses.Id <> eigeneId Then WScript.Echo "ABBRUCH: Sitzung " & ses.Id & " ist nicht die eigene (" & eigeneId & ")": WScript.Quit 1
If ses.Info.SystemName <> "T76" Or ses.Info.Client <> "100" Then WScript.Echo "ABBRUCH: nicht T76/100": WScript.Quit 1

Dim w, b, tr, keys, key, cols, line, c
Select Case cmd
  Case "ok"
    ses.findById("wnd[0]/tbar[0]/okcd").Text = WScript.Arguments(2)
    ses.findById("wnd[0]").sendVKey 0
    WScript.Sleep 1500
  Case "dump"
    w = "wnd[0]"
    If WScript.Arguments.Count > 2 Then w = WScript.Arguments(2)
    Dim tf
    tf = 7
    If WScript.Arguments.Count > 3 Then tf = CLng(WScript.Arguments(3))
    Dump ses.findById(w), tf
  Case "press"
    ses.findById(WScript.Arguments(2)).press
    WScript.Sleep 1500
  Case "set"
    On Error Resume Next
    ses.findById(WScript.Arguments(2)).SetFocus
    Err.Clear
    On Error GoTo 0
    ses.findById(WScript.Arguments(2)).Text = WScript.Arguments(3)
    WScript.Echo "WERT=" & ses.findById(WScript.Arguments(2)).Text
  Case "vkey"
    w = "wnd[0]"
    If WScript.Arguments.Count > 3 Then w = WScript.Arguments(3)
    ses.findById(w).sendVKey CLng(WScript.Arguments(2))
    WScript.Sleep 1500
  Case "select"
    ses.findById(WScript.Arguments(2)).Select
    WScript.Sleep 1500
  Case "check"
    ses.findById(WScript.Arguments(2)).Selected = (WScript.Arguments(3) = "1")
    WScript.Echo "SELECTED=" & ses.findById(WScript.Arguments(2)).Selected
  Case "menus"
    Dump ses.findById("wnd[0]/mbar"), 4
  Case "tree"
    Set tr = ses.findById(WScript.Arguments(2))
    Set keys = tr.GetAllNodeKeys()
    WScript.Echo "KNOTEN=" & keys.Count
    On Error Resume Next
    Set cols = tr.GetColumnNames()
    Err.Clear
    For i = 0 To keys.Count - 1
      key = keys.ElementAt(CLng(i))
      line = key & " | " & tr.GetNodeTextByKey(key)
      If Not IsEmpty(cols) Then
        For c = 0 To cols.Count - 1
          line = line & " | " & tr.GetItemText(key, cols.ElementAt(CLng(c)))
        Next
      End If
      Err.Clear
      WScript.Echo line
    Next
    On Error GoTo 0
  Case "grid"
    Dim g, r, gc
    Set g = ses.findById(WScript.Arguments(2))
    WScript.Echo "ZEILEN=" & g.RowCount
    For r = 0 To g.RowCount - 1
      line = r & ":"
      For gc = 0 To g.ColumnOrder.Count - 1
        line = line & " " & g.ColumnOrder(CLng(gc)) & "=" & g.GetCellValue(CLng(r), g.ColumnOrder(CLng(gc)))
      Next
      WScript.Echo Left(line, 400)
    Next
  Case "gridsel"
    ' gridsel <ses> <gridId> <zeilen>  (nullbasiert, kommagetrennt; ergaenzt 2026-11-06 IC Staffelbasis)
    ses.findById(WScript.Arguments(2)).SelectedRows = WScript.Arguments(3)
    WScript.Echo "MARKIERT=" & ses.findById(WScript.Arguments(2)).SelectedRows
  Case "treesel"
    ses.findById(WScript.Arguments(2)).SelectNode WScript.Arguments(3)
    WScript.Sleep 800
  Case "treedbl"
    ses.findById(WScript.Arguments(2)).DoubleClickNode WScript.Arguments(3)
    WScript.Sleep 1500
  Case "zu"
    ses.findById("wnd[0]/tbar[0]/okcd").Text = "/i"
    ses.findById("wnd[0]").sendVKey 0
    WScript.Sleep 1000
    WScript.Echo "Sitzungen jetzt=" & con.Children.Count
    WScript.Quit 0
End Select
Status ses
