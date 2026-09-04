Option Explicit
' Wie SapGuiWorklistSelect.vbs, aber fuer den Reiter "Transportierbare Objekte".
' Sobald Objekte einem echten Paket zugeordnet sind, stehen sie dort und nicht
' mehr unter "Lokale Objekte".
'
' Aufruf: SapGuiWorklistTransport.vbs <TX> waehle|liste
'
' Auswahl ausschliesslich ueber Namensabgleich: im Arbeitsvorrat stehen auch
' fremde Objekte desselben Benutzers, am 2026-09-04 etwa Z_REICHW.

Dim tx, modus, a, app, c, s, i, j, found, tbl, basis, r, nam, mark, gesamt

tx = UCase(WScript.Arguments(0))
modus = LCase(WScript.Arguments(1))

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

On Error Resume Next
Dim titel
titel = s.FindById("wnd[1]").Text
If Err.Number <> 0 Or InStr(1, titel, "Inaktive Objekte", vbTextCompare) = 0 Then
  WScript.Echo "Kein Dialog 'Inaktive Objekte' offen (gefunden: " & titel & ")"
  WScript.Quit 3
End If
On Error GoTo 0

s.FindById("wnd[1]/usr/tabsACT_TAB_STRIP/tabpTRANSPORT").Select
WScript.Sleep 900

basis = "wnd[1]/usr/tabsACT_TAB_STRIP/tabpTRANSPORT/ssubSCREEN_ACTIVATE:SAPLSEWORKINGAREA:0201/tblSAPLSEWORKINGAREAT_TRANSPORT"

Function IstEigenes(name)
  Dim n
  n = UCase(Trim(name))
  IstEigenes = False
  If InStr(n, "ZZPRDAT") > 0 Then IstEigenes = True
End Function

Set tbl = s.FindById(basis)
gesamt = 0
For r = 0 To tbl.RowCount - 1
  nam = ""
  On Error Resume Next
  nam = s.FindById(basis & "/txtWORK_ITEMS-OBJ_NAME[2," & r & "]").Text
  On Error GoTo 0
  If Trim(nam) <> "" And Left(Trim(nam), 1) <> "_" Then
    gesamt = gesamt + 1
    If modus = "waehle" Then
      On Error Resume Next
      tbl.GetAbsoluteRow(r).Selected = IstEigenes(nam)
      On Error GoTo 0
    End If
    mark = "?"
    On Error Resume Next
    mark = CStr(tbl.GetAbsoluteRow(r).Selected)
    On Error GoTo 0
    WScript.Echo "ZEILE=" & r & " MARKIERT=" & mark & " EIGEN=" & IstEigenes(nam) & " NAME=" & Trim(nam)
  End If
Next
WScript.Echo "ZEILEN_GESAMT=" & gesamt
WScript.Quit 0
