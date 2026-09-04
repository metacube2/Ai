Option Explicit
'=======================================================================
' Traegt Methodenquelltext ein, ohne vorher zu navigieren.
'
' Anders als SapGuiSetBadiMethodSource.vbs setzt dieses Skript voraus, dass
' SE19 bereits auf dem Interface-Reiter der gewuenschten Implementierung steht.
' Das ist noetig, solange die Implementierung noch nicht gesichert ist: ein
' Sprung auf das Einstiegsbild wuerde sie verwerfen. Genau das ist am
' 2026-09-04 bei Z_ZZPRDAT_UPDATE passiert.
'
' Aufruf: SapGuiBadiMethodeHier.vbs <Methode> <Quelltextdatei>
'=======================================================================

Dim methode, datei, s, fso, quelle

methode = WScript.Arguments(0)
datei = WScript.Arguments(1)

Set fso = CreateObject("Scripting.FileSystemObject")
If Not fso.FileExists(datei) Then
  WScript.Echo "Quelltextdatei nicht gefunden: " & datei
  WScript.Quit 2
End If
quelle = fso.OpenTextFile(datei, 1).ReadAll

If InStr(1, quelle, "METHOD", vbTextCompare) = 0 Or InStr(1, quelle, "ENDMETHOD", vbTextCompare) = 0 Then
  WScript.Echo "ABBRUCH: Datei ohne METHOD/ENDMETHOD, der Puffer wird komplett ersetzt."
  WScript.Quit 3
End If

Dim a, app, c, s2, i, j, found
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s2 = c.Children(CLng(j))
    If UCase(s2.Info.Transaction) = "SE19" And s2.Info.SystemName = "T76" And s2.Info.Client = "100" Then
      Set s = s2
      found = True
      Exit For
    End If
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 4

On Error Resume Next
s.FindById("wnd[0]/usr/tabsEXIT_TAB_STRIP/tabpGOTO_CLASS").Select
On Error GoTo 0
WScript.Sleep 1200

Dim basis, tbl, r, name, pos, gefunden
basis = "wnd[0]/usr/tabsEXIT_TAB_STRIP/tabpGOTO_CLASS/ssubSUB_SCREEN_EXIT:SAPLSEXO:0170/tblSAPLSEXOTC_METHOD"
gefunden = False
Set tbl = s.FindById(basis)
For pos = 0 To tbl.VerticalScrollbar.Maximum
  Set tbl = s.FindById(basis)
  tbl.VerticalScrollbar.Position = pos
  Set tbl = s.FindById(basis)
  For r = 0 To tbl.RowCount - 1
    name = ""
    On Error Resume Next
    name = s.FindById(basis & "/txtRSEXSCRN-METHO_NAME[0," & r & "]").Text
    On Error GoTo 0
    If UCase(Trim(name)) = UCase(methode) Then
      s.FindById(basis & "/txtRSEXSCRN-METHO_NAME[0," & r & "]").SetFocus
      s.FindById(basis & "/txtRSEXSCRN-METHO_NAME[0," & r & "]").CaretPosition = 0
      ' DoubleClickCurrentCell wird von dieser GUI nicht unterstuetzt, F2 schon.
      s.FindById("wnd[0]").SendVKey 2
      WScript.Sleep 3000
      gefunden = True
      Exit For
    End If
  Next
  If gefunden Then Exit For
Next

If Not gefunden Then
  WScript.Echo "ABBRUCH: Methode " & methode & " nicht gefunden."
  WScript.Quit 5
End If

Dim offen
offen = ""
On Error Resume Next
offen = s.FindById("wnd[0]/usr/txtDY0200_CPDNAME").Text
On Error GoTo 0
WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "OFFENE_METHODE=" & offen
If InStr(1, offen, methode, vbTextCompare) = 0 Then
  WScript.Echo "ABBRUCH: erwartet wurde " & methode
  WScript.Quit 6
End If

Dim ed
Set ed = s.FindById("wnd[0]/usr/subEDITORSUBSCREEN:SAPLEDITOR_START:8430/cntlEDITOR/shellcont/shell")
ed.SelectAll
ed.ReplaceSelection quelle
WScript.Sleep 600

s.FindById("wnd[0]/tbar[0]/btn[11]").Press
WScript.Sleep 2500
WScript.Echo "MELDUNGSTYP=" & s.FindById("wnd[0]/sbar").MessageType
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
On Error Resume Next
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
WScript.Quit 0
