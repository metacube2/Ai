Option Explicit
' Setzt eine Auswahlliste ueber den Schluessel, nicht ueber den Anzeigetext.
' Der Anzeigetext enthaelt oft Umlaute und geht auf dem Weg ueber die
' PowerShell-Kommandozeile kaputt; der Schluessel ist stabil.
'
' Aufruf: SapGuiComboSetzen.vbs <TX> <ElementId> <Schluessel>

Dim tx, id, schluessel, a, app, c, s, i, j, found

tx = UCase(WScript.Arguments(0))
id = WScript.Arguments(1)
schluessel = WScript.Arguments(2)

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

s.FindById(id).Key = schluessel
WScript.Sleep 400
WScript.Echo "SCHLUESSEL=" & s.FindById(id).Key
WScript.Echo "TEXT=" & s.FindById(id).Text
WScript.Quit 0
