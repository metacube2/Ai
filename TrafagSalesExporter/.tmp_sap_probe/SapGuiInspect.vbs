Option Explicit

Dim SapGuiAuto, application, connection, session, i, j

On Error Resume Next
Set SapGuiAuto = GetObject("SAPGUI")
If Err.Number <> 0 Then
  WScript.Echo "SAPGUI_ERROR=" & Err.Description
  WScript.Quit 1
End If
On Error GoTo 0

Set application = SapGuiAuto.GetScriptingEngine
WScript.Echo "Connections=" & application.Children.Count

For i = 0 To application.Children.Count - 1
  Set connection = application.Children(CLng(i))
  WScript.Echo "Connection[" & i & "]=" & connection.Description & " Sessions=" & connection.Children.Count
  For j = 0 To connection.Children.Count - 1
    Set session = connection.Children(CLng(j))
    WScript.Echo "Session[" & i & "," & j & "] System=" & session.Info.SystemName & " Client=" & session.Info.Client & " User=" & session.Info.User & " Transaction=" & session.Info.Transaction & " Busy=" & session.Busy
  Next
Next
