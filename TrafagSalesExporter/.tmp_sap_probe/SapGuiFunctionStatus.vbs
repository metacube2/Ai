Option Explicit
' Liest den Aktivstatus eines Funktionsbausteins in SE37, rein lesend.
' Aufruf: SapGuiFunctionStatus.vbs <Funktionsbaustein>

Dim fb, s, o, k, el

fb = WScript.Arguments(0)

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

Set s = SitzungHolen("SE37")
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE37"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2500
Set s = SitzungHolen("SE37")

s.FindById("wnd[0]/usr/ctxtRS38L-NAME").Text = fb
' Anzeigen: F7 in SE37
s.FindById("wnd[0]").SendVKey 7
WScript.Sleep 3000

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text

' Der Statustext steht als Label rechts neben dem Namensfeld.
On Error Resume Next
For k = 0 To s.FindById("wnd[0]/usr").Children.Count - 1
  Set el = s.FindById("wnd[0]/usr").Children(CLng(k))
  If InStr(1, el.Text, "aktiv", vbTextCompare) > 0 Then
    WScript.Echo "STATUSTEXT=" & el.Text & "  (" & el.Id & ")"
  End If
Next
On Error GoTo 0
WScript.Quit 0
