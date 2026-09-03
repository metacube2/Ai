Option Explicit

Dim targetTransaction, outputPath
targetTransaction = UCase(WScript.Arguments(0))
outputPath = WScript.Arguments(1)

Dim SapGuiAuto, application, connection, session, i, j, found
Set SapGuiAuto = GetObject("SAPGUI")
Set application = SapGuiAuto.GetScriptingEngine
found = False

For i = 0 To application.Children.Count - 1
  Set connection = application.Children(CLng(i))
  For j = 0 To connection.Children.Count - 1
    Set session = connection.Children(CLng(j))
    If UCase(session.Info.Transaction) = targetTransaction Then
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
  WScript.Echo "SESSION_NOT_FOUND=" & targetTransaction
  WScript.Quit 2
End If

Dim fso, stream
Set fso = CreateObject("Scripting.FileSystemObject")
Set stream = fso.CreateTextFile(outputPath, True, True)
stream.WriteLine "System=" & session.Info.SystemName & " Client=" & session.Info.Client & " Transaction=" & session.Info.Transaction
DumpComponent session, stream, 0
stream.Close
WScript.Echo "DUMP_OK=" & outputPath

Sub DumpComponent(ByVal component, ByVal target, ByVal depth)
  On Error Resume Next
  Dim line, child, count, k
  line = String(depth * 2, " ") & component.Id & " | " & component.Type
  If Len(component.Name) > 0 Then line = line & " | Name=" & component.Name
  If Len(component.Text) > 0 Then line = line & " | Text=" & Replace(component.Text, vbCrLf, " ")
  target.WriteLine line
  Err.Clear
  count = component.Children.Count
  If Err.Number <> 0 Then
    Err.Clear
    Exit Sub
  End If
  For k = 0 To count - 1
    Set child = component.Children(CLng(k))
    DumpComponent child, target, depth + 1
  Next
  On Error GoTo 0
End Sub
