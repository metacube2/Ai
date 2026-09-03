Option Explicit
' Zeigt eine Erweiterungsimplementierung (den Container klassischer BAdI-
' Implementierungen im neuen Enhancement-Framework) in SE19 an. Rein lesend.
'
' Wichtig: Der Container hat einen eigenen Aktivstatus. Ist er inaktiv, laeuft
' keine der enthaltenen BAdI-Implementierungen, auch wenn diese selbst "aktiv"
' meldet.
'
' Aufruf: SapGuiEnhImplStatus.vbs <Erweiterungsimplementierung>

Dim name, s, k, el

name = WScript.Arguments(0)

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

Set s = SitzungHolen("SE19")
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE19"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2500
Set s = SitzungHolen("SE19")

' Oberer Block: Erweiterungsimplementierung, neues BAdI.
s.FindById("wnd[0]/usr/radG_IS_NEW_1").Select
s.FindById("wnd[0]/usr/ctxtG_ENHNAME").Text = name
s.FindById("wnd[0]/usr/btnPUSHBUTTON_DISPLAY_TEXT").Press
WScript.Sleep 3500

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "MELDUNGSTYP=" & s.FindById("wnd[0]/sbar").MessageType
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text

' Alles Textartige im Benutzerbereich ausgeben, damit der Status sichtbar wird,
' ohne dass die Feldnamen vorher bekannt sein muessen.
On Error Resume Next
For k = 0 To s.FindById("wnd[0]/usr").Children.Count - 1
  Set el = s.FindById("wnd[0]/usr").Children(CLng(k))
  If Len(Trim(el.Text)) > 0 Then
    WScript.Echo "  " & el.Type & " " & el.Name & " = " & el.Text
  End If
Next
On Error GoTo 0
WScript.Quit 0
