Option Explicit
' Liest den Inhalt des ABAP-Editors der angegebenen Sitzung (rein lesend).
' Argument 1: Transaktionscode der Sitzung, Argument 2: Ausgabedatei.

Dim tx, outPath, a, app, c, s, i, j, k, found, dump, fso, f, node

tx = UCase(WScript.Arguments(0))
outPath = WScript.Arguments(1)

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

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text

Dim candidates, cand, txt
candidates = Array( _
  "wnd[0]/usr/subEDITORSUBSCREEN:SAPLEDITOR_START:8430/cntlEDITOR/shellcont/shell", _
  "wnd[0]/usr/cntlEDITOR_CONTAINER/shellcont/shell", _
  "wnd[0]/usr/cntlEDITOR/shellcont/shell", _
  "wnd[0]/usr/cntlEDITOR_CONTAINER/shellcont/shell/shellcont[1]/shell", _
  "wnd[0]/shellcont/shell/shellcont[0]/shell/shellcont/shell" )

txt = ""
For Each cand In candidates
  On Error Resume Next
  Err.Clear
  Set node = s.FindById(cand)
  If Err.Number = 0 Then
    txt = node.Text
    If Err.Number = 0 And Len(txt) > 0 Then
      On Error GoTo 0
      WScript.Echo "EDITOR=" & cand
      Set fso = CreateObject("Scripting.FileSystemObject")
      Set f = fso.CreateTextFile(outPath, True)
      f.Write txt
      f.Close
      WScript.Echo "ZEICHEN=" & Len(txt)
      WScript.Quit 0
    End If
  End If
  On Error GoTo 0
Next

WScript.Echo "Kein Editorinhalt gefunden"
WScript.Quit 3
