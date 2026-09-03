Option Explicit

Dim SapGuiAuto, application, connection, session, i, j, found
Set SapGuiAuto = GetObject("SAPGUI")
Set application = SapGuiAuto.GetScriptingEngine
found = False

For i = 0 To application.Children.Count - 1
  Set connection = application.Children(CLng(i))
  For j = 0 To connection.Children.Count - 1
    Set session = connection.Children(CLng(j))
    If UCase(session.Info.Transaction) = "SE19" Then
      If session.Info.SystemName <> "T76" Or session.Info.Client <> "100" Then
        WScript.Echo "GUARD_ERROR=" & session.Info.SystemName & "/" & session.Info.Client
        WScript.Quit 3
      End If
      found = True
      Exit For
    End If
  Next
  If found Then Exit For
Next

If Not found Then
  WScript.Echo "SE19_SESSION_NOT_FOUND"
  WScript.Quit 2
End If

session.FindById("wnd[0]/usr/radG_IS_CLASSIC_2").Select
session.FindById("wnd[0]/usr/ctxtRSEXSCRN-EXIT_NAME").Text = "WORKORDER_UPDATE"
session.FindById("wnd[0]/usr/btnPUSHBUTTON_IMPLEMENT_TEXT").Press
WScript.Sleep 1000
WScript.Echo "SCREEN=" & session.FindById("wnd[0]").Text
WScript.Echo "STATUS=" & session.FindById("wnd[0]/sbar/pane[0]").Text
