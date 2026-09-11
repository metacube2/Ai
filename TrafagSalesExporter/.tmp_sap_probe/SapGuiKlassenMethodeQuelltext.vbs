Option Explicit
'=======================================================================
' Traegt den Quelltext einer Methode im Class Builder (SE24) ein und sichert.
'
' Voraussetzung: die Klasse ist im AENDERN-Modus offen, Reiter "Methoden".
' Die Methode wird - notfalls ueber mehrere Bildseiten - gesucht, mit F2
' geoeffnet, der Editorpuffer vollstaendig ersetzt und gesichert.
'
' Aufruf: SapGuiKlassenMethodeQuelltext.vbs <SitzungsIndex> <Methode> <Datei>
'
' Der Puffer wird KOMPLETT ersetzt, deshalb muss die Datei METHOD ... ENDMETHOD
' enthalten. Zeilenenden werden auf CRLF gebracht; reines LF hat am 2026-09-10
' an anderer Stelle dazu gefuehrt, dass still nichts geschrieben wurde.
'=======================================================================
Dim sesIdx, methode, datei, fso, quelle, a, app, c, s
Dim sub_, tab, r, txt, ziel, ed, offen, feld, pos

If WScript.Arguments.Count < 3 Then
  WScript.Echo "Aufruf: SapGuiKlassenMethodeQuelltext.vbs <SitzungsIndex> <Methode> <Datei>"
  WScript.Quit 1
End If
sesIdx = CInt(WScript.Arguments(0))
methode = UCase(Trim(WScript.Arguments(1)))
datei = WScript.Arguments(2)

Set fso = CreateObject("Scripting.FileSystemObject")
If Not fso.FileExists(datei) Then WScript.Echo "Datei fehlt: " & datei : WScript.Quit 2
quelle = fso.OpenTextFile(datei, 1).ReadAll
quelle = Replace(quelle, vbCrLf, vbLf)
quelle = Replace(quelle, vbCr, vbLf)
quelle = Replace(quelle, vbLf, vbCrLf)

If InStr(1, quelle, "METHOD", vbTextCompare) = 0 Or InStr(1, quelle, "ENDMETHOD", vbTextCompare) = 0 Then
  WScript.Echo "ABBRUCH: kein METHOD/ENDMETHOD in der Datei."
  WScript.Quit 3
End If

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set c = app.Children(0)
Set s = c.Children(CLng(sesIdx))

sub_ = "wnd[0]/usr/tabsCTS/tabpTAB_MTD/ssubCSS:SAPLSEOD:0253"
On Error Resume Next
Set tab = s.FindById(sub_ & "/tblSAPLSEODMC")
If Err.Number <> 0 Then WScript.Echo "Methodentabelle nicht gefunden" : WScript.Quit 4
Err.Clear
On Error GoTo 0

ziel = -1
pos = 0
Do While pos < tab.RowCount
  tab.VerticalScrollbar.Position = pos
  Set tab = s.FindById(sub_ & "/tblSAPLSEODMC")
  For r = 0 To tab.VisibleRowCount - 1
    txt = ""
    feld = sub_ & "/tblSAPLSEODMC/txtDY_0253-CPDNAME[0," & r & "]"
    On Error Resume Next
    txt = UCase(Trim(s.FindById(feld).Text))
    If Err.Number <> 0 Then
      Err.Clear
      feld = sub_ & "/tblSAPLSEODMC/ctxtDY_0253-CPDNAME[0," & r & "]"
      txt = UCase(Trim(s.FindById(feld).Text))
    End If
    Err.Clear
    On Error GoTo 0
    If txt = methode Then
      ziel = r
      WScript.Echo "GEFUNDEN_ABSOLUT=" & (pos + r) & " SCROLL=" & pos
      s.FindById(feld).SetFocus
      On Error Resume Next
      s.FindById(feld).CaretPosition = 0
      Err.Clear
      On Error GoTo 0
      Exit For
    End If
  Next
  If ziel >= 0 Then Exit Do
  pos = pos + tab.VisibleRowCount
Loop
If ziel < 0 Then WScript.Echo "Methode " & methode & " nicht gefunden" : WScript.Quit 5

s.FindById("wnd[0]").SendVKey 2
WScript.Sleep 3000
WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text

offen = ""
On Error Resume Next
offen = s.FindById("wnd[0]/usr/txtDY0200_CPDNAME").Text
Err.Clear
On Error GoTo 0
If InStr(1, offen, methode, vbTextCompare) = 0 Then
  WScript.Echo "ABBRUCH: offen ist '" & offen & "', erwartet " & methode
  WScript.Quit 6
End If
WScript.Echo "OFFEN=" & offen

Set ed = Nothing
On Error Resume Next
Set ed = s.FindById("wnd[0]/usr/subEDITORSUBSCREEN:SAPLEDITOR_START:8430/cntlEDITOR/shellcont/shell")
Err.Clear
On Error GoTo 0
If ed Is Nothing Then WScript.Echo "Editorsteuerelement nicht gefunden" : WScript.Quit 7

ed.SelectAll
ed.ReplaceSelection quelle
WScript.Sleep 1000

s.FindById("wnd[0]/tbar[0]/btn[11]").Press
WScript.Sleep 3000
On Error Resume Next
WScript.Echo "MELDUNGSTYP=" & s.FindById("wnd[0]/sbar").MessageType
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar").Text
WScript.Echo "STATUS=" & s.FindById("wnd[0]/usr/txtDY0200_STATUS").Text
Err.Clear
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
If Err.Number <> 0 Then WScript.Echo "DIALOG=(keiner)"
On Error GoTo 0
