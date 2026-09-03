Option Explicit
' Listet den Dialog "Inaktive Objekte" vollstaendig auf, inklusive Markierungsstand.
' Rein lesend: es wird nichts markiert, nichts bestaetigt und nichts aktiviert.

Dim a, app, c, s, i, j, found, tbl, base, r, abs, total, seen, key

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = "SE37" And s.Info.SystemName = "T76" And s.Info.Client = "100" Then
      found = True
      Exit For
    End If
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2

On Error Resume Next
If s.FindById("wnd[1]").Text = "" Then
End If
If Err.Number <> 0 Then
  WScript.Echo "Kein Dialog offen"
  WScript.Quit 3
End If
On Error GoTo 0

base = "wnd[1]/usr/tabsACT_TAB_STRIP/tabpLOCAL/ssubSCREEN_ACTIVATE:SAPLSEWORKINGAREA:0202/tblSAPLSEWORKINGAREAT_LOCAL"
Set tbl = s.FindById(base)
total = tbl.RowCount
WScript.Echo "SICHTBARE_ZEILEN=" & total
WScript.Echo "GESAMT=" & tbl.VerticalScrollbar.Maximum + 1

Set seen = CreateObject("Scripting.Dictionary")

Dim pos
For pos = 0 To tbl.VerticalScrollbar.Maximum
  Set tbl = s.FindById(base)
  tbl.VerticalScrollbar.Position = pos
  Set tbl = s.FindById(base)
  For r = 0 To tbl.RowCount - 1
    Dim typ, nam, usr, mark
    typ = "" : nam = "" : usr = "" : mark = "?"
    On Error Resume Next
    typ = s.FindById(base & "/txtWORK_ITEMS-OBJECT[1," & r & "]").Text
    nam = s.FindById(base & "/txtWORK_ITEMS-OBJ_NAME[2," & r & "]").Text
    usr = s.FindById(base & "/txtWORK_ITEMS-UNAME[3," & r & "]").Text
    mark = CStr(tbl.GetAbsoluteRow(pos + r).Selected)
    On Error GoTo 0
    If Trim(typ) <> "" Or Trim(nam) <> "" Then
      key = (pos + r) & "|" & Trim(typ) & "|" & Trim(nam)
      If Not seen.Exists(key) Then
        seen.Add key, True
        WScript.Echo "ZEILE=" & (pos + r) & " MARKIERT=" & mark & " TYP=" & Trim(typ) & " NAME=" & Trim(nam)
      End If
    End If
  Next
Next
WScript.Quit 0
