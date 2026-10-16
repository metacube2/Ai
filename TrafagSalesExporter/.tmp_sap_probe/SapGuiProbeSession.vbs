Option Explicit
' Diagnose: warum liefert eine bestehende Verbindung keine Sitzung?
' Fragt die Engine Schritt fuer Schritt und meldet jeden Fehler im Klartext.
Dim SapGuiAuto, app, conn, i

On Error Resume Next
Set SapGuiAuto = GetObject("SAPGUI")
If Err.Number <> 0 Then
  WScript.Echo "GetObject SAPGUI FEHLER: " & Err.Number & " " & Err.Description
  WScript.Quit 1
End If
WScript.Echo "GetObject SAPGUI: ok"

Set app = SapGuiAuto.GetScriptingEngine
If Err.Number <> 0 Then
  WScript.Echo "GetScriptingEngine FEHLER: " & Err.Number & " " & Err.Description
  WScript.Quit 1
End If
WScript.Echo "GetScriptingEngine: ok"
Err.Clear

WScript.Echo "Engine.MajorVersion=" & app.MajorVersion & " Revision=" & app.Revision
If Err.Number <> 0 Then WScript.Echo "  Version nicht lesbar: " & Err.Description
Err.Clear

WScript.Echo "Connections=" & app.Children.Count
For i = 0 To app.Children.Count - 1
  Set conn = app.Children(CLng(i))
  WScript.Echo "Connection[" & i & "] Description=" & conn.Description
  If Err.Number <> 0 Then WScript.Echo "  Description Fehler: " & Err.Description
  Err.Clear
  WScript.Echo "  DisabledByServer=" & conn.DisabledByServer
  If Err.Number <> 0 Then WScript.Echo "  DisabledByServer nicht lesbar: " & Err.Description
  Err.Clear
  WScript.Echo "  ConnectionString=" & conn.ConnectionString
  Err.Clear
  WScript.Echo "  Sessions=" & conn.Children.Count
  If Err.Number <> 0 Then WScript.Echo "  Children Fehler: " & Err.Description
  Err.Clear
Next
