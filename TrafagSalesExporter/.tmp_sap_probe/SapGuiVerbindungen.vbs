' NUR LESEN: alle SAP-GUI-Verbindungen mit System, Anwendungsserver, Verbindungsstring und Sitzungen (Transaktion).
' Angelegt 2026-10-16: P76 hat zwei Anwendungsserver (GUI trasp76s, OData travp762); der Verbindungsstring zeigt, wo man ist.
Set app = GetObject("SAPGUI").GetScriptingEngine
WScript.Echo "Verbindungen=" & app.Children.Count
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  If c.Children.Count > 0 Then
    Set f = c.Children(0).Info
    WScript.Echo i & ": " & f.SystemName & " Mandant=" & f.Client & " Server=" & f.ApplicationServer & " | " & Trim(c.ConnectionString)
  End If
  For j = 0 To c.Children.Count - 1
    WScript.Echo "   ses[" & j & "] " & c.Children(CLng(j)).Info.Transaction
  Next
Next
