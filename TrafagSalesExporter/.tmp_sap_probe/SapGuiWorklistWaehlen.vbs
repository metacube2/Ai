Option Explicit
' Arbeitet den Dialog "Inaktive Objekte" ab, mit den Objektnamen als Argument.
'
' Ersetzt die beiden aelteren Skripte SapGuiWorklistSelect.vbs und
' SapGuiWorklistTransport.vbs, deren Namensfilter fest auf die ZZPRDAT-Objekte
' verdrahtet sind und die fuer jede neue Aufgabe angefasst werden mussten.
'
' Aufruf: SapGuiWorklistWaehlen.vbs <TX> <LOCAL|TRANSPORT> [Name1 Name2 ...]
' Ohne Namen wird nur aufgelistet. Mit Namen werden genau diese markiert und
' alles andere demarkiert; der Vergleich ist gross-/kleinschreibungsblind.
'
' Warum ueber Namen und nicht ueber Zeilennummern: neben den eigenen Zeilen
' liegen fremde unfertige Objekte desselben Benutzers, und die Liste aendert
' ihre Reihenfolge.

Dim tx, reiter, namen, a, app, c, s, i, j, found, tbl, basis, r, nam, mark, gesamt, treffer

tx = UCase(WScript.Arguments(0))
reiter = UCase(WScript.Arguments(1))
If reiter <> "LOCAL" And reiter <> "TRANSPORT" Then
  WScript.Echo "Reiter muss LOCAL oder TRANSPORT sein."
  WScript.Quit 5
End If

namen = "|"
For i = 2 To WScript.Arguments.Count - 1
  namen = namen & UCase(Trim(WScript.Arguments(i))) & "|"
Next

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

Dim titel
On Error Resume Next
titel = s.FindById("wnd[1]").Text
If Err.Number <> 0 Or InStr(1, titel, "Inaktive Objekte", vbTextCompare) = 0 Then
  WScript.Echo "Kein Dialog 'Inaktive Objekte' offen (gefunden: " & titel & ")"
  WScript.Quit 3
End If
Err.Clear

' Der Reiter muss sichtbar sein, sonst bestaetigt der gruene Haken die Auswahl
' des anderen Reiters, also nichts, und der Dialog bleibt scheinbar wirkungslos stehen.
s.FindById("wnd[1]/usr/tabsACT_TAB_STRIP/tabp" & reiter).Select
Err.Clear
On Error GoTo 0
WScript.Sleep 800

If reiter = "LOCAL" Then
  basis = "wnd[1]/usr/tabsACT_TAB_STRIP/tabpLOCAL/ssubSCREEN_ACTIVATE:SAPLSEWORKINGAREA:0202/tblSAPLSEWORKINGAREAT_LOCAL"
Else
  basis = "wnd[1]/usr/tabsACT_TAB_STRIP/tabpTRANSPORT/ssubSCREEN_ACTIVATE:SAPLSEWORKINGAREA:0201/tblSAPLSEWORKINGAREAT_TRANSPORT"
End If

Set tbl = s.FindById(basis)
gesamt = 0
treffer = 0
For r = 0 To tbl.RowCount - 1
  nam = ""
  On Error Resume Next
  nam = s.FindById(basis & "/txtWORK_ITEMS-OBJ_NAME[2," & r & "]").Text
  On Error GoTo 0
  If Trim(nam) <> "" And Left(Trim(nam), 1) <> "_" Then
    Dim eigen
    eigen = (namen <> "|") And (InStr(namen, "|" & UCase(Trim(nam)) & "|") > 0)
    gesamt = gesamt + 1
    If namen <> "|" Then
      On Error Resume Next
      tbl.GetAbsoluteRow(r).Selected = eigen
      On Error GoTo 0
      If eigen Then treffer = treffer + 1
    End If
    mark = "?"
    On Error Resume Next
    mark = CStr(tbl.GetAbsoluteRow(r).Selected)
    On Error GoTo 0
    WScript.Echo "ZEILE=" & r & " MARKIERT=" & mark & " NAME=" & Trim(nam)
  End If
Next
WScript.Echo "ZEILEN_GESAMT=" & gesamt & " TREFFER=" & treffer
WScript.Quit 0
