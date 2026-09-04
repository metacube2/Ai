Option Explicit
' Liest einen Baum (GuiTree) vollstaendig aus, Knoten und Spalten.
'
' Gebraucht wird das vor allem in SE01/SE09, wo die Objektliste eines
' Transportauftrags ein Baum ist und sich nicht wie eine Tabelle lesen laesst.
'
' Aufruf: SapGuiBaumLesen.vbs <TX> <BaumId>
' Ohne BaumId werden nur die gefundenen Baum-Ids gemeldet.

Dim tx, baumid, a, app, c, s, i, j, found

tx = UCase(WScript.Arguments(0))
If WScript.Arguments.Count > 1 Then baumid = WScript.Arguments(1) Else baumid = ""

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = tx And s.Info.SystemName = "T76" And s.Info.Client = "100" Then
      found = True
      Exit For
    End If
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2

If baumid = "" Then
  Sub Suche(el, tiefe)
    Dim k, kind
    On Error Resume Next
    If el.Type = "GuiTree" Then WScript.Echo "BAUM=" & el.Id
    If Err.Number <> 0 Then Err.Clear
    If el.ContainerType = True Then
      For k = 0 To el.Children.Count - 1
        Set kind = el.Children(CLng(k))
        Suche kind, tiefe + 1
      Next
    End If
    On Error GoTo 0
  End Sub
  Suche s.FindById("wnd[0]"), 0
  WScript.Quit 0
End If

Dim baum, keys, key, n, txt
Set baum = s.FindById(baumid)
keys = baum.GetAllNodeKeys()
For n = 0 To keys.Count - 1
  key = keys(CLng(n))
  txt = ""
  On Error Resume Next
  txt = baum.GetNodeTextByKey(key)
  On Error GoTo 0
  WScript.Echo key & " | " & txt
Next
WScript.Quit 0
