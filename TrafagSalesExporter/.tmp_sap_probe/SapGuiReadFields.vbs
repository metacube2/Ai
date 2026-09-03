Option Explicit
' Liest Werte einzelner Bildschirmelemente aus. Fuer Ankreuzfelder liefert der
' Steuerelementbaum nur die Existenz, nicht den Zustand; dafuer ist dieses Skript da.
'
' Aufruf: SapGuiReadFields.vbs <TX> <ElementId> [<ElementId> ...]

Dim tx, a, app, c, s, i, j, found, k, id, el, wert

tx = UCase(WScript.Arguments(0))

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = tx And s.Info.SystemName = "T76" And s.Info.Client = "100" Then
      found = True
      Exit For
    End If
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2

For k = 1 To WScript.Arguments.Count - 1
  id = WScript.Arguments(k)
  wert = "?"
  On Error Resume Next
  Err.Clear
  Set el = s.FindById(id)
  If Err.Number = 0 Then
    Select Case el.Type
      Case "GuiCheckBox"
        wert = "angekreuzt=" & CStr(el.Selected) & " Text=" & el.Text
      Case "GuiRadioButton"
        wert = "gewaehlt=" & CStr(el.Selected) & " Text=" & el.Text
      Case Else
        wert = el.Text
    End Select
  Else
    wert = "NICHT GEFUNDEN"
  End If
  On Error GoTo 0
  WScript.Echo id & " -> " & wert
Next

WScript.Quit 0
