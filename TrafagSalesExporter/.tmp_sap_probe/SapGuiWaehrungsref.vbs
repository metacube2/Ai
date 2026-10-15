Option Explicit
' Setzt bei Waehrungsfeldern die Referenztabelle und das Referenzfeld auf dem
' Reiter "Waehrungs-/Mengenfelder" der SE11-Strukturpflege.
' Aufruf: SapGuiWaehrungsref.vbs <SitzungsIndex> <Feld>=<RefTab>.<RefFeld> [...]
Dim a, app, c, s, sesIdx, i, r, basis, tbl, paar, teil, feld, refTab, refFeld
Dim nameId, tabId, feldId, aktName, gesetzt

sesIdx = CInt(WScript.Arguments(0))
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
' Verbindung T76 suchen statt fest die erste zu nehmen (2026-10-15: P76 war Verbindung 0)
Dim ci_, c_
Set c = Nothing
For ci_ = 0 To app.Children.Count - 1
  Set c_ = app.Children(CLng(ci_))
  If c_.Children.Count > 0 Then
    If c_.Children(0).Info.SystemName = "T76" Then Set c = c_
  End If
Next
If c Is Nothing Then
  WScript.Echo "ABBRUCH: keine T76-Verbindung offen"
  WScript.Quit 2
End If
Set s = c.Children(CLng(sesIdx))

' Reiter Waehrungs-/Mengenfelder waehlen
On Error Resume Next
s.FindById("wnd[0]/usr/tabsTAB_STRIP/tabpREFF").Select
If Err.Number <> 0 Then
  WScript.Echo "Reiter tabpREFF nicht gefunden: " & Err.Description
  WScript.Quit 2
End If
Err.Clear
On Error GoTo 0
WScript.Sleep 1200

basis = "wnd[0]/usr/tabsTAB_STRIP/tabpREFF/ssubTS_SCREEN:SAPLSD41:2103/tblSAPLSD41TC0"
On Error Resume Next
Set tbl = s.FindById(basis)
If Err.Number <> 0 Then
  WScript.Echo "Tabelle nicht gefunden unter " & basis & ": " & Err.Description
  WScript.Quit 3
End If
On Error GoTo 0

gesetzt = 0
For r = 0 To tbl.VisibleRowCount - 1
  aktName = ""
  On Error Resume Next
  aktName = Trim(s.FindById(basis & "/txtDD03P_D-FIELDNAME[0," & r & "]").Text)
  Err.Clear
  On Error GoTo 0
  If aktName <> "" Then
    For i = 1 To WScript.Arguments.Count - 1
      paar = Split(WScript.Arguments(i), "=")
      feld = UCase(Trim(paar(0)))
      If UCase(aktName) = feld Then
        teil = Split(paar(1), ".")
        refTab = teil(0)
        refFeld = teil(1)
        On Error Resume Next
        s.FindById(basis & "/txtDD03P_D-REFTABLE[4," & r & "]").Text = refTab
        s.FindById(basis & "/txtDD03P_D-REFFIELD[5," & r & "]").Text = refFeld
        If Err.Number <> 0 Then
          WScript.Echo "FEHLER bei " & feld & ": " & Err.Description
        Else
          WScript.Echo "GESETZT " & feld & " -> " & refTab & "-" & refFeld
          gesetzt = gesetzt + 1
        End If
        Err.Clear
        On Error GoTo 0
      End If
    Next
  End If
Next

WScript.Echo "GESETZT_GESAMT=" & gesetzt
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 1200
On Error Resume Next
WScript.Echo "STATUS=" & s.FindById("wnd[0]/sbar").Text
On Error GoTo 0
