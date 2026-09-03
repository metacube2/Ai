Option Explicit
' Arbeitet den Dialog "Inaktive Objekte" ab.
'
' Argument 1: Transaktionscode der Sitzung, in der der Dialog offen ist.
' Argument 2: "liste" listet nur auf, "waehle" markiert die eigenen und demarkiert alles andere.
'
' Die Auswahl erfolgt ausschliesslich ueber Namensabgleich. Direkt neben den eigenen
' Zeilen liegen fremde, unfertige Objekte desselben Benutzers; eine Auswahl ueber
' Zeilennummern wuerde diese bei jeder Listenaenderung mitaktivieren.

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

basis = "wnd[1]/usr/tabsACT_TAB_STRIP/tabpLOCAL/ssubSCREEN_ACTIVATE:SAPLSEWORKINGAREA:0202/tblSAPLSEWORKINGAREAT_LOCAL"

Function IstEigenes(name)
  Dim n
  n = UCase(Trim(name))
  IstEigenes = False
  If n = "SAPLZPP_ZZPRDAT_TEST" Then IstEigenes = True
  If n = "Z_PP_PRDDAT_SET" Then IstEigenes = True
  If Left(n, 18) = "LZPP_ZZPRDAT_TEST" & "T" Then IstEigenes = True
  If Left(n, 26) = "ZCL_IM__ZZPRDAT_AT_RELEASE" Then IstEigenes = True
End Function

' Alle Zeilen liegen auf einer Seite: RowCount ist groesser als die Trefferzahl.
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
