Option Explicit
' Liest die Eintraege eines Auswahlfelds oder setzt einen davon.
'
' Aufruf:
'   SapGuiComboBox.vbs <TX> <ElementId>              listet alle Eintraege
'   SapGuiComboBox.vbs <TX> <ElementId> <Schluessel> waehlt den Eintrag

Dim tx, id, schluessel, a, app, c, s, i, j, found, cb, e

tx = UCase(WScript.Arguments(0))
id = WScript.Arguments(1)
If WScript.Arguments.Count > 2 Then schluessel = WScript.Arguments(2) Else schluessel = ""

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

Set cb = s.FindById(id)

If schluessel = "" Then
  WScript.Echo "AKTUELL=" & cb.Text & "  (Schluessel " & cb.Key & ")"
  WScript.Echo "EINTRAEGE:"
  For Each e In cb.Entries
    WScript.Echo "  " & e.Key & "  =  " & e.Value
  Next
Else
  cb.Key = schluessel
  WScript.Sleep 1200
  WScript.Echo "GESETZT=" & cb.Text & "  (Schluessel " & cb.Key & ")"
  WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
End If

WScript.Quit 0
