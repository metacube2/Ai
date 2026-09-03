Option Explicit
' Waehlt ein Element aus, das die Methode Select kennt: Reiter, Radiobutton,
' Ankreuzfeld. Fuer Knoepfe ist SapGuiPressButton.vbs zustaendig.
'
' Aufruf: SapGuiSelectElement.vbs <TX> <ElementId> [<ElementId> ...]

Dim tx, a, app, c, s, i, j, found, k, id

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
  On Error Resume Next
  Err.Clear
  s.FindById(id).Select
  If Err.Number <> 0 Then
    WScript.Echo id & " -> FEHLER: " & Err.Description
  Else
    WScript.Echo id & " -> gewaehlt"
  End If
  On Error GoTo 0
  WScript.Sleep 800
Next

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
WScript.Quit 0
