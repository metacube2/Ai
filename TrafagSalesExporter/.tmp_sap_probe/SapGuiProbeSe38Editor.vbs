Option Explicit
' Probiert Lesemoeglichkeiten des SE38-Editors durch, ohne zu schreiben.
' Ergaenzt SapGuiProbeEditor.vbs, das dasselbe fuer SE19 gemacht hat.
'
' Aufruf: SapGuiProbeSe38Editor.vbs [Datei]
' Mit Datei wird ein gefundener Text dorthin geschrieben.

Dim a, app, c, s, i, j, found, ed, v, datei, fso, ausgabe

If WScript.Arguments.Count > 0 Then datei = WScript.Arguments(0) Else datei = ""

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = "SE38" And s.Info.SystemName = "T76" And s.Info.Client = "100" Then
      found = True
      Exit For
    End If
  Next
  If found Then Exit For
Next
If Not found Then
  WScript.Echo "KEINE_SE38_SITZUNG"
  WScript.Quit 2
End If

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text

On Error Resume Next
Set ed = s.FindById("wnd[0]/usr/cntlEDITOR/shellcont/shell")
If Err.Number <> 0 Then
  WScript.Echo "EDITOR_NICHT_GEFUNDEN: " & Err.Description
  Err.Clear
  WScript.Quit 3
End If

Err.Clear
v = ed.Text
If Err.Number = 0 Then
  WScript.Echo "TEXT_LEN=" & Len(v)
  ausgabe = v
Else
  WScript.Echo "Text FEHLER: " & Err.Description
  Err.Clear
End If

If Len(ausgabe) = 0 Then
  ed.SelectAll
  Err.Clear
  v = ed.SelectedText
  If Err.Number = 0 Then
    WScript.Echo "SELECTEDTEXT_LEN=" & Len(v)
    ausgabe = v
  Else
    WScript.Echo "SelectedText FEHLER: " & Err.Description
    Err.Clear
  End If
End If

If datei <> "" And Len(ausgabe) > 0 Then
  Set fso = CreateObject("Scripting.FileSystemObject")
  fso.CreateTextFile(datei, True).Write ausgabe
  WScript.Echo "GESCHRIEBEN=" & datei
End If

WScript.Quit 0
