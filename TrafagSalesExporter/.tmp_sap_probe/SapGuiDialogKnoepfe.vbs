Option Explicit
' Listet die Knoepfe eines Modaldialogs mit Tooltip.
' Aufruf: SapGuiDialogKnoepfe.vbs <TX> [Fenster]   Fenster z.B. wnd[1] oder wnd[2]
Dim a, app, c, s, i, j, k, b, tx, fenster, leiste
tx = UCase(WScript.Arguments(0))
If WScript.Arguments.Count > 1 Then fenster = WScript.Arguments(1) Else fenster = "wnd[1]"

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = tx Then
      On Error Resume Next
      WScript.Echo "Dialog=" & s.FindById(fenster).Text
      Set leiste = s.FindById(fenster & "/tbar[0]")
      If Err.Number <> 0 Then
        WScript.Echo "Keine Toolbar in " & fenster & ": " & Err.Description
        WScript.Quit 3
      End If
      For k = 0 To leiste.Children.Count - 1
        Set b = leiste.Children(CLng(k))
        WScript.Echo b.Id & " | Text=" & b.Text & " | Tooltip=" & b.Tooltip
      Next
      On Error GoTo 0
      WScript.Quit 0
    End If
  Next
Next
WScript.Quit 2
