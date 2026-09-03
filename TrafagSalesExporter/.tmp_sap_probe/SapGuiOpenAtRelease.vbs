Option Explicit
' Sucht in der SE19-Methodentabelle die Zeile AT_RELEASE und oeffnet sie per Doppelklick.
' Rein lesender Einstieg: der Methodeneditor zeigt die echte Signatur des BAdI.

Dim a, app, c, s, i, j, found, tbl, base, r, name, pos, maxScroll

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = "SE19" And s.Info.SystemName = "T76" And s.Info.Client = "100" Then
      found = True
      Exit For
    End If
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2

base = "wnd[0]/usr/tabsEXIT_TAB_STRIP/tabpGOTO_CLASS/ssubSUB_SCREEN_EXIT:SAPLSEXO:0170/tblSAPLSEXOTC_METHOD"

maxScroll = 60
For pos = 0 To maxScroll
  On Error Resume Next
  Err.Clear
  Set tbl = s.FindById(base)
  If Err.Number <> 0 Then
    WScript.Echo "Methodentabelle nicht gefunden"
    WScript.Quit 3
  End If
  tbl.VerticalScrollbar.Position = pos
  Err.Clear
  Set tbl = s.FindById(base)
  On Error GoTo 0

  For r = 0 To tbl.RowCount - 1
    name = ""
    On Error Resume Next
    name = s.FindById(base & "/txtRSEXSCRN-METHO_NAME[0," & r & "]").Text
    On Error GoTo 0
    If UCase(Trim(name)) = "AT_RELEASE" Then
      WScript.Echo "GEFUNDEN scroll=" & pos & " zeile=" & r
      s.FindById(base & "/txtRSEXSCRN-METHO_NAME[0," & r & "]").SetFocus
      s.FindById(base & "/txtRSEXSCRN-METHO_NAME[0," & r & "]").CaretPosition = 0
      ' F2 entspricht dem Doppelklick und navigiert in den Methodeneditor.
      s.FindById("wnd[0]").SendVKey 2
      WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
      WScript.Echo "TRANSAKTION=" & s.Info.Transaction
      WScript.Quit 0
    End If
  Next
Next

WScript.Echo "AT_RELEASE nicht gefunden"
WScript.Quit 4
