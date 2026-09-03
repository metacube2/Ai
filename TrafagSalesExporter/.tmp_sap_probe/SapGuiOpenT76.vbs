Option Explicit

Dim SapGuiAuto, application, connection, session

Set SapGuiAuto = GetObject("SAPGUI")
Set application = SapGuiAuto.GetScriptingEngine
Set connection = application.OpenConnection("T76 - ERP TestSystem", True)

If connection.Children.Count = 0 Then
  WScript.Echo "T76_OPENED_NO_SESSION"
  WScript.Quit 2
End If

Set session = connection.Children(0)
WScript.Echo "System=" & session.Info.SystemName & " Client=" & session.Info.Client & " User=" & session.Info.User & " Transaction=" & session.Info.Transaction
