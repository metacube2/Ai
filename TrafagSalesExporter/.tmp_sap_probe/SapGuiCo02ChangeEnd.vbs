Option Explicit
' Verschiebt den Eckendtermin eines bestehenden Fertigungsauftrags in CO02 und
' sichert. Das ist der Write-once-Nachweis: GLTRP aendert sich, ZZPRDAT darf nicht.
'
' Aufruf: SapGuiCo02ChangeEnd.vbs <Auftragsnummer> <neuer Endtermin TT.MM.JJJJ>

Dim aufnr, endtermin, s, basis, runde, titel, ziel

aufnr = WScript.Arguments(0)
endtermin = WScript.Arguments(1)

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
  If erste Is Nothing Then WScript.Quit 4
  erste.FindById("wnd[0]/tbar[0]/okcd").Text = "/o" & tx
  erste.FindById("wnd[0]").SendVKey 0
  WScript.Sleep 2500
  Set SitzungHolen = SitzungHolen(tx)
End Function

Sub DialogeAbarbeiten(sess)
  Dim r, t, z
  For r = 1 To 12
    t = ""
    On Error Resume Next
    t = sess.FindById("wnd[1]").Text
    On Error GoTo 0
    If t = "" Then Exit Sub
    z = ""
    If InStr(1, t, "Materialstatuspr", vbTextCompare) > 0 Then z = "wnd[1]/usr/btnSPOP-VAROPTION1"
    If InStr(1, t, "Auftrag freigeben", vbTextCompare) > 0 Then z = "wnd[1]/usr/btnSPOP-VAROPTION1"
    If InStr(1, t, "Statusverarbeitung", vbTextCompare) > 0 Then z = "wnd[1]/usr/btnOPTION2"
    If UCase(t) = "INFORMATION" Then z = "wnd[1]/tbar[0]/btn[0]"
    If z = "" Then
      WScript.Echo "ABBRUCH: unbekannter Dialog '" & t & "'"
      WScript.Quit 9
    End If
    WScript.Echo "  Dialog '" & t & "' -> " & z
    On Error Resume Next
    sess.FindById(z).Press
    On Error GoTo 0
    WScript.Sleep 1500
  Next
End Sub

Set s = SitzungHolen("CO02")
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nCO02"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2500
Set s = SitzungHolen("CO02")

s.FindById("wnd[0]/usr/ctxtCAUFVD-AUFNR").Text = aufnr
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2500
DialogeAbarbeiten s
WScript.Echo "Auftrag offen: " & s.FindById("wnd[0]").Text

basis = "wnd[0]/usr/tabsTABSTRIP_0115/tabpKOZE/ssubSUBSCR_0115:SAPLCOKO1:0120"
On Error Resume Next
Err.Clear
s.FindById(basis & "/ctxtCAUFVD-GLTRP").Text = endtermin
If Err.Number <> 0 Then
  WScript.Echo "Feld GLTRP nicht erreichbar. Der Reiter oder die Bildlaufposition passt nicht."
  WScript.Quit 5
End If
On Error GoTo 0

s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2500
DialogeAbarbeiten s

s.FindById("wnd[0]/tbar[0]/btn[11]").Press
WScript.Sleep 2500
DialogeAbarbeiten s

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
WScript.Quit 0
