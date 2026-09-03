Option Explicit
' Liest den Aktivstatus einer klassischen BAdI-Implementierung, rein lesend.
' Aufruf: SapGuiBadiStatus.vbs <Implementierung>

Dim impl, s

impl = WScript.Arguments(0)

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

s.FindById("wnd[0]/usr/radG_IS_CLASSIC_1").Select
s.FindById("wnd[0]/usr/ctxtRSEXSCRN-IMP_NAME").Text = impl
s.FindById("wnd[0]/usr/btnPUSHBUTTON_DISPLAY_TEXT").Press
WScript.Sleep 3000

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
On Error Resume Next
WScript.Echo "IMPLEMENTIERUNG=" & s.FindById("wnd[0]/usr/ctxtRSEXSCRN-IMP_NAME").Text
WScript.Echo "AKTIV=" & s.FindById("wnd[0]/usr/txtRSEXSCRN-ACTIVE").Text
WScript.Echo "DEFINITION=" & s.FindById("wnd[0]/usr/ctxtRSEXSCRN-EXIT_NAME").Text
On Error GoTo 0
WScript.Quit 0
