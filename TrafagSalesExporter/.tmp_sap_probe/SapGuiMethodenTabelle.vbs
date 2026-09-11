Option Explicit
' Liest die Methodentabelle in SE24 aus: Gesamtzeilen, sichtbare Zeilen und je
' sichtbarer Zeile Methodenname, Art und Beschreibung. Optional wird vorher
' gescrollt oder nach einem Namen gesucht.
'
' Aufruf: SapGuiMethodenTabelle.vbs <SitzungsIndex> [ScrollPosition] [Suchname]
Dim a, app, c, s, sesIdx, sub_, tab, r, pos, such, txt, gefunden, id, k

sesIdx = CInt(WScript.Arguments(0))
pos = 0
If WScript.Arguments.Count > 1 Then pos = CLng(WScript.Arguments(1))
such = ""
If WScript.Arguments.Count > 2 Then such = UCase(Trim(WScript.Arguments(2)))

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set c = app.Children(0)
Set s = c.Children(CLng(sesIdx))

sub_ = "wnd[0]/usr/tabsCTS/tabpTAB_MTD/ssubCSS:SAPLSEOD:0253"
On Error Resume Next
Set tab = s.FindById(sub_ & "/tblSAPLSEODMC")
If Err.Number <> 0 Then
  WScript.Echo "Methodentabelle nicht gefunden: " & Err.Description
  WScript.Quit 2
End If
Err.Clear
On Error GoTo 0

WScript.Echo "GESAMT=" & tab.RowCount & " SICHTBAR=" & tab.VisibleRowCount

If such <> "" Then
  ' Blockweise durch die gesamte Tabelle blaettern und suchen
  gefunden = -1
  pos = 0
  Do While pos < tab.RowCount
    tab.VerticalScrollbar.Position = pos
    Set tab = s.FindById(sub_ & "/tblSAPLSEODMC")
    For r = 0 To tab.VisibleRowCount - 1
      txt = ""
      On Error Resume Next
      txt = UCase(Trim(s.FindById(sub_ & "/tblSAPLSEODMC/txtDY_0253-CPDNAME[0," & r & "]").Text))
      If txt = "" Then txt = UCase(Trim(s.FindById(sub_ & "/tblSAPLSEODMC/ctxtDY_0253-CPDNAME[0," & r & "]").Text))
      Err.Clear
      On Error GoTo 0
      If txt = such Then
        WScript.Echo "TREFFER_ABSOLUT=" & (pos + r) & " ZEILE_SICHTBAR=" & r & " SCROLL=" & pos
        gefunden = pos + r
        pos = tab.RowCount
        Exit For
      End If
    Next
    pos = pos + tab.VisibleRowCount
  Loop
  If gefunden < 0 Then WScript.Echo "TREFFER=(keiner)"
  WScript.Quit 0
End If

If pos > 0 Then
  tab.VerticalScrollbar.Position = pos
  Set tab = s.FindById(sub_ & "/tblSAPLSEODMC")
End If
WScript.Echo "SCROLL=" & tab.VerticalScrollbar.Position

For r = 0 To tab.VisibleRowCount - 1
  txt = ""
  On Error Resume Next
  txt = Trim(s.FindById(sub_ & "/tblSAPLSEODMC/txtDY_0253-CPDNAME[0," & r & "]").Text)
  Err.Clear
  If txt = "" Then txt = Trim(s.FindById(sub_ & "/tblSAPLSEODMC/ctxtDY_0253-CPDNAME[0," & r & "]").Text)
  Err.Clear
  k = ""
  k = Trim(s.FindById(sub_ & "/tblSAPLSEODMC/txtDY_0253-DESCRIPT[4," & r & "]").Text)
  Err.Clear
  On Error GoTo 0
  WScript.Echo "ZEILE " & r & " | " & txt & " | " & k
Next
