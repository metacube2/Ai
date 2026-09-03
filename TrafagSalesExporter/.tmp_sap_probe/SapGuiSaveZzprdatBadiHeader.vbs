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
      If session.Info.SystemName <> "T76" Or session.Info.Client <> "100" Then WScript.Quit 3
      found = True
      Exit For
    End If
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2

If InStr(1, session.FindById("wnd[0]").Text, "Z_ZZPRDAT_AT_RELEASE", vbTextCompare) = 0 Then WScript.Quit 4

session.FindById("wnd[0]/usr/txtRSEXSCRN-IMP_TEXT").Text = "ZZPRDAT einmalig bei Freigabe (T76-Test)"
session.FindById("wnd[0]/tbar[0]/btn[11]").Press
WScript.Sleep 1500
WScript.Echo "MAIN=" & session.FindById("wnd[0]").Text
On Error Resume Next
WScript.Echo "DIALOG=" & session.FindById("wnd[1]").Text
WScript.Echo "STATUS=" & session.FindById("wnd[0]/sbar/pane[0]").Text
