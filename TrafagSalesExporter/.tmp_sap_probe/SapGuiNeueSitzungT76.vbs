' T76: neue eigene Sitzung in der T76-Verbindung oeffnen (nie Ingos Sitzung uebernehmen) und ihren Index melden.
' Angelegt 2026-10-15. Aufruf: SapGuiNeueSitzungT76.vbs
Set app = GetObject("SAPGUI").GetScriptingEngine
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  If c.Children(0).Info.SystemName = "T76" Then
    n = c.Children.Count
    c.Children(0).CreateSession
    For k = 1 To 20
      WScript.Sleep 500
      If c.Children.Count > n Then Exit For
    Next
    WScript.Echo "T76 Sitzungen=" & c.Children.Count & " neue Index=" & (c.Children.Count - 1) & " TX=" & c.Children(CLng(c.Children.Count - 1)).Info.Transaction
  End If
Next
