Option Explicit
'=======================================================================
' Traegt den Quelltext einer BAdI-Methode ein und sichert ihn, in einem Zug:
' SE19-Einstieg -> Implementierung aendern -> Interface-Reiter -> Methode
' oeffnen -> Puffer ersetzen -> sichern.
'
' Aufruf:
'   SapGuiSetBadiMethodSource.vbs <Implementierung> <Methode> <Quelltextdatei>
'
' Die Quelltextdatei muss METHOD ... ENDMETHOD enthalten, weil SelectAll plus
' ReplaceSelection den gesamten Editorpuffer ersetzt.
'=======================================================================

Dim impl, methode, datei, s, fso, quelle

If WScript.Arguments.Count < 3 Then
  WScript.Echo "Aufruf: SapGuiSetBadiMethodSource.vbs <Implementierung> <Methode> <Datei>"
  WScript.Quit 1
End If

impl = WScript.Arguments(0)
methode = WScript.Arguments(1)
datei = WScript.Arguments(2)

Set fso = CreateObject("Scripting.FileSystemObject")
If Not fso.FileExists(datei) Then
  WScript.Echo "Quelltextdatei nicht gefunden: " & datei
  WScript.Quit 2
End If
quelle = fso.OpenTextFile(datei, 1).ReadAll

If InStr(1, quelle, "METHOD", vbTextCompare) = 0 Or InStr(1, quelle, "ENDMETHOD", vbTextCompare) = 0 Then
  WScript.Echo "ABBRUCH: Die Datei enthaelt kein METHOD/ENDMETHOD. Der Editorpuffer wird"
  WScript.Echo "vollstaendig ersetzt; ohne diese Zeilen entstehen Syntaxfehler."
  WScript.Quit 3
End If

'-----------------------------------------------------------------------
Function SitzungHolen(tx)
  Dim a, app, c, s2, i, j, erste
  Set a = GetObject("SAPGUI")
  Set app = a.GetScriptingEngine
  Set erste = Nothing
  For i = 0 To app.Children.Count - 1
    Set c = app.Children(CLng(i))
    For j = 0 To c.Children.Count - 1
      Set s2 = c.Children(CLng(j))
      If s2.Info.SystemName = "T76" And s2.Info.Client = "100" Then
        If erste Is Nothing Then Set erste = s2
        If UCase(s2.Info.Transaction) = UCase(tx) Then
          Set SitzungHolen = s2
          Exit Function
        End If
      End If
    Next
  Next
  If erste Is Nothing Then
    WScript.Echo "FEHLER: keine T76/100-Sitzung."
    WScript.Quit 4
  End If
  erste.FindById("wnd[0]/tbar[0]/okcd").Text = "/o" & tx
  erste.FindById("wnd[0]").SendVKey 0
  WScript.Sleep 2500
  Set SitzungHolen = SitzungHolen(tx)
End Function
'-----------------------------------------------------------------------

Set s = SitzungHolen("SE19")

' Auf das Einstiegsbild zurueck, damit der Ablauf reproduzierbar startet.
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE19"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2500
Set s = SitzungHolen("SE19")

' Klassisches BAdI, Implementierung eintragen, aendern.
s.FindById("wnd[0]/usr/radG_IS_CLASSIC_1").Select
s.FindById("wnd[0]/usr/ctxtRSEXSCRN-IMP_NAME").Text = impl
s.FindById("wnd[0]/usr/btnPUSHBUTTON_CHANGE_TEXT").Press
WScript.Sleep 3000
WScript.Echo "Implementierung geoeffnet: " & s.FindById("wnd[0]").Text

' Reiter Interface.
On Error Resume Next
s.FindById("wnd[0]/usr/tabsEXIT_TAB_STRIP/tabpGOTO_CLASS").Select
On Error GoTo 0
WScript.Sleep 1200

' Methode in der Tabelle suchen und mit F2 oeffnen.
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
      s.FindById("wnd[0]").SendVKey 2
      WScript.Sleep 2500
      gefunden = True
      Exit For
    End If
  Next
  If gefunden Then Exit For
Next

If Not gefunden Then
  WScript.Echo "ABBRUCH: Methode " & methode & " nicht in der Liste gefunden."
  WScript.Quit 5
End If

WScript.Echo "Methodeneditor: " & s.FindById("wnd[0]").Text

' Absicherung: wirklich die gewuenschte Methode?
Dim offen
offen = s.FindById("wnd[0]/usr/txtDY0200_CPDNAME").Text
If InStr(1, offen, methode, vbTextCompare) = 0 Then
  WScript.Echo "ABBRUCH: offen ist '" & offen & "', erwartet wurde " & methode
  WScript.Quit 6
End If
WScript.Echo "Offene Methode: " & offen & " (Status " & s.FindById("wnd[0]/usr/txtDY0200_STATUS").Text & ")"

Dim ed
Set ed = s.FindById("wnd[0]/usr/subEDITORSUBSCREEN:SAPLEDITOR_START:8430/cntlEDITOR/shellcont/shell")
ed.SelectAll
ed.ReplaceSelection quelle
WScript.Sleep 600

s.FindById("wnd[0]/tbar[0]/btn[11]").Press
WScript.Sleep 2000
WScript.Echo "MELDUNGSTYP=" & s.FindById("wnd[0]/sbar").MessageType
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
WScript.Echo "STATUS=" & s.FindById("wnd[0]/usr/txtDY0200_STATUS").Text
WScript.Quit 0
