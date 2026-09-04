Option Explicit
' Setzt ein Eingabefeld mit vorherigem Fokuswechsel und misst den Wert zurueck.
'
' Manche Felder, besonders in verschachtelten Modaldialogen, nehmen eine reine
' Text-Zuweisung nicht an. Ein SetFocus davor hilft; die Rueckmessung zeigt, ob
' der Wert wirklich angekommen ist.
'
' Aufruf: SapGuiFeldSetzen.vbs <TX> <ElementId> <Wert>

Dim tx, id, wert, a, app, c, s, i, j, found

tx = UCase(WScript.Arguments(0))
id = WScript.Arguments(1)
wert = WScript.Arguments(2)

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

On Error Resume Next
s.FindById(id).SetFocus
If Err.Number <> 0 Then WScript.Echo "SetFocus: " & Err.Description
Err.Clear
On Error GoTo 0
WScript.Sleep 400

s.FindById(id).Text = wert
WScript.Sleep 400
WScript.Echo "RUECKMESSUNG=" & s.FindById(id).Text
WScript.Quit 0
