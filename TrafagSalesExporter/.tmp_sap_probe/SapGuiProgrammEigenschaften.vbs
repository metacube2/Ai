Option Explicit
' Zeigt die Eigenschaften eines Programms in SE38 und gibt Paket und Status aus.
' Rein lesend.
'
' Aufruf: SapGuiProgrammEigenschaften.vbs <Programmname>

Dim prog, s, k, el

prog = WScript.Arguments(0)

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

Set s = SitzungHolen("SE38")
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE38"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2500
Set s = SitzungHolen("SE38")

s.FindById("wnd[0]/usr/ctxtRS38M-PROGRAMM").Text = prog
s.FindById("wnd[0]/usr/radRS38M-FUNC_HEAD").Select
s.FindById("wnd[0]/usr/btnSHOP").Press
WScript.Sleep 3000

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text

On Error Resume Next
For k = 0 To s.FindById("wnd[0]/usr").Children.Count - 1
  Set el = s.FindById("wnd[0]/usr").Children(CLng(k))
  If (el.Type = "GuiTextField" Or el.Type = "GuiCTextField") And Len(Trim(el.Text)) > 0 Then
    WScript.Echo "  " & el.Name & " = " & el.Text
  End If
Next
On Error GoTo 0
WScript.Quit 0
