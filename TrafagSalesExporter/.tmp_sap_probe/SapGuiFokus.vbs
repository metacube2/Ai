Option Explicit
' Setzt den Cursor auf ein Element und sendet optional eine Funktionstaste.
'
' In klassischen Listen, etwa der Auftragsanzeige in SE01, haengt die Wirkung
' eines Knopfes daran, in welcher Zeile der Cursor steht. Labels lassen sich
' nicht "auswaehlen", nur fokussieren.
'
' Aufruf: SapGuiFokus.vbs <TX> <ElementId> [<VKey>]

Dim tx, id, vkey, a, app, c, s, i, j, found

tx = UCase(WScript.Arguments(0))
id = WScript.Arguments(1)
If WScript.Arguments.Count > 2 Then vkey = CLng(WScript.Arguments(2)) Else vkey = -1

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

s.FindById(id).SetFocus
WScript.Sleep 400
WScript.Echo "FOKUS_GESETZT=" & id

If vkey >= 0 Then
  s.FindById("wnd[0]").SendVKey vkey
  WScript.Sleep 2500
  WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
  WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
End If
WScript.Quit 0
