Option Explicit
' Markiert im Dialog "Inaktive Objekte" ausschliesslich die Objekte der eigenen
' Testfunktionsgruppe und demarkiert alles andere. Bestaetigt NICHT.
'
' Die Auswahl erfolgt bewusst ueber Namensabgleich und nie ueber Zeilennummern:
' direkt neben unseren Zeilen liegen fremde, unfertige Objekte desselben Benutzers.

Dim a, app, c, s, i, j, found, tbl, base, r, nam, pos, gewaehlt, abgewaehlt

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

base = "wnd[1]/usr/tabsACT_TAB_STRIP/tabpLOCAL/ssubSCREEN_ACTIVATE:SAPLSEWORKINGAREA:0202/tblSAPLSEWORKINGAREAT_LOCAL"

Function IstEigenes(name)
  Dim n
  n = UCase(Trim(name))
  IstEigenes = False
  If n = "SAPLZPP_ZZPRDAT_TEST" Then IstEigenes = True
  If n = "Z_PP_PRDDAT_SET" Then IstEigenes = True
  If Left(n, 20) = "LZPP_ZZPRDAT_TESTTOP" Then IstEigenes = True
End Function

gewaehlt = 0
abgewaehlt = 0

Set tbl = s.FindById(base)
For pos = 0 To tbl.VerticalScrollbar.Maximum
  Set tbl = s.FindById(base)
  tbl.VerticalScrollbar.Position = pos
  Set tbl = s.FindById(base)
  For r = 0 To tbl.RowCount - 1
    nam = ""
    On Error Resume Next
    nam = s.FindById(base & "/txtWORK_ITEMS-OBJ_NAME[2," & r & "]").Text
    On Error GoTo 0
    If Trim(nam) <> "" And Left(Trim(nam), 1) <> "_" Then
      On Error Resume Next
      If IstEigenes(nam) Then
        tbl.GetAbsoluteRow(pos + r).Selected = True
        gewaehlt = gewaehlt + 1
      Else
        tbl.GetAbsoluteRow(pos + r).Selected = False
        abgewaehlt = abgewaehlt + 1
      End If
      On Error GoTo 0
    End If
  Next
Next

WScript.Echo "MARKIERT_GESETZT=" & gewaehlt
WScript.Echo "DEMARKIERT=" & abgewaehlt
WScript.Quit 0
