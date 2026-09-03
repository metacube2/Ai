Option Explicit

Dim SapGuiAuto, application, connection, session, i, j, found
Set SapGuiAuto = GetObject("SAPGUI")
Set application = SapGuiAuto.GetScriptingEngine
found = False
For i = 0 To application.Children.Count - 1
  Set connection = application.Children(CLng(i))
  For j = 0 To connection.Children.Count - 1
    Set session = connection.Children(CLng(j))
    If session.Info.SystemName = "T76" And session.Info.Client = "100" And UCase(session.Info.Transaction) = "SE19" Then
      On Error Resume Next
      If session.FindById("wnd[1]").Text = "Objektkatalogeintrag anlegen" Then found = True
      On Error GoTo 0
      If found Then Exit For
    End If
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2

session.FindById("wnd[1]/tbar[0]/btn[7]").Press
WScript.Sleep 1500
WScript.Echo "MAIN=" & session.FindById("wnd[0]").Text
On Error Resume Next
WScript.Echo "DIALOG=" & session.FindById("wnd[1]").Text
WScript.Echo "STATUS=" & session.FindById("wnd[0]/sbar/pane[0]").Text
