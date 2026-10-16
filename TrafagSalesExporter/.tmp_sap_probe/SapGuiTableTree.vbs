Option Explicit
' Liest einen SAP.TableTreeControl (z.B. den SEGW-Projektbaum).
' Der vorhandene SapGuiBaumLesen.vbs kann diesen Typ nicht.
' Aufruf: SapGuiTableTree.vbs <SitzungsIndex> <BaumId> [Knotenschluessel zum Aufklappen]
Dim a, app, c, s, sesIdx, tree, keys, i, k, txt

sesIdx = CInt(WScript.Arguments(0))
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set c = app.Children(0)
Set s = c.Children(CLng(sesIdx))

On Error Resume Next
Set tree = s.FindById(WScript.Arguments(1))
If Err.Number <> 0 Then
  WScript.Echo "Baum nicht gefunden: " & Err.Description
  WScript.Quit 2
End If
Err.Clear

If WScript.Arguments.Count > 2 Then
  tree.ExpandNode WScript.Arguments(2)
  If Err.Number <> 0 Then WScript.Echo "ExpandNode: " & Err.Description
  Err.Clear
  WScript.Sleep 1200
End If

Set keys = tree.GetAllNodeKeys()
If Err.Number <> 0 Then
  WScript.Echo "GetAllNodeKeys: " & Err.Description
  WScript.Quit 3
End If
Err.Clear

WScript.Echo "KNOTEN=" & keys.Count
For i = 0 To keys.Count - 1
  k = keys.ElementAt(i)
  txt = ""
  txt = tree.GetNodeTextByKey(k)
  Err.Clear
  WScript.Echo k & " | " & txt
Next
On Error GoTo 0
