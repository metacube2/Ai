Option Explicit
'=======================================================================
' Redefiniert eine geerbte Methode in SE24 auf dem RICHTIGEN Weg:
'
' Die Methodentabelle zeigt die geerbten Komponenten bereits an. Man traegt
' den Namen NICHT in eine freie Zeile ein - das wertet SAP als Anlage einer
' NEUEN Methode und meldet "Es wird bereits ein(e) Komponente ... geerbt".
' Richtig ist, den Cursor auf die GEERBTE Zeile zu setzen und den Knopf
' "Redefinieren" zu druecken.
'
' Die Tabelle ist laenger als das Fenster (im DPC_EXT 398 Zeilen bei 25
' sichtbaren), deshalb wird blockweise gescrollt statt nur die erste Seite
' anzusehen.
'
' Aufruf: SapGuiRedefinierenZeile.vbs <SitzungsIndex> <Methodenname>
'=======================================================================
Dim a, app, c, s, sesIdx, sub_, tab, r, mtd, txt, feld, pos, ziel, sichtbar

sesIdx = CInt(WScript.Arguments(0))
mtd = UCase(Trim(WScript.Arguments(1)))

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set c = app.Children(0)
Set s = c.Children(CLng(sesIdx))

sub_ = "wnd[0]/usr/tabsCTS/tabpTAB_MTD/ssubCSS:SAPLSEOD:0253"
On Error Resume Next
Set tab = s.FindById(sub_ & "/tblSAPLSEODMC")
If Err.Number <> 0 Then WScript.Echo "Methodentabelle nicht gefunden" : WScript.Quit 2
Err.Clear
On Error GoTo 0

sichtbar = tab.VisibleRowCount
WScript.Echo "GESAMT=" & tab.RowCount & " SICHTBAR=" & sichtbar

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
    If txt = mtd Then
      ziel = r
      WScript.Echo "GEFUNDEN_ABSOLUT=" & (pos + r) & " SICHTBARE_ZEILE=" & r & " SCROLL=" & pos
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

If ziel < 0 Then WScript.Echo "Methode " & mtd & " nicht gefunden" : WScript.Quit 3

s.FindById(sub_ & "/btnPUSH_REDEFINE").Press
WScript.Sleep 2500

On Error Resume Next
WScript.Echo "STATUS=" & s.FindById("wnd[0]/sbar").Text
WScript.Echo "TYP=" & s.FindById("wnd[0]/sbar").MessageType
WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
Err.Clear
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
If Err.Number <> 0 Then WScript.Echo "DIALOG=(keiner)"
On Error GoTo 0
