' NUR LESEN: Baum (GuiTree) ueber den Sitzungsindex lesen (Verbindung 0 muss T76 sein). Angelegt 2026-11-06,
' weil SapGuiBaumLesen.vbs mit "Bad index type" abbricht (keys(CLng(n)) statt keys.ElementAt(n)).
' Aufruf: SapGuiBaumLesenSitzung.vbs <SesIdx> <BaumId>
Set app = GetObject("SAPGUI").GetScriptingEngine
Set c = app.Children(0)
Set s = c.Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Echo "nicht T76" : WScript.Quit 2
Set baum = s.FindById(WScript.Arguments(1))
Set keys = baum.GetAllNodeKeys()
WScript.Echo "KNOTEN=" & keys.Count
For n = 0 To keys.Count - 1
  key = keys.ElementAt(CLng(n))
  txt = ""
  On Error Resume Next
  txt = baum.GetNodeTextByKey(key)
  If txt = "" Then txt = baum.GetItemText(key, "&Hierarchy")
  Err.Clear
  On Error GoTo 0
  WScript.Echo key & " | " & txt
Next
