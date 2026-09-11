Option Explicit
'=======================================================================
' Schreibt Quelltext in einen BEREITS OFFENEN Methodeneditor (SE24) und
' sichert ihn.
'
' Warum getrennt von SapGuiKlassenMethodeQuelltext.vbs: nach dem
' Redefinieren springt der Class Builder von selbst in den Methodeneditor.
' Die Methodentabelle ist dann gar nicht mehr am Bildschirm, und ein Skript,
' das sie sucht, bricht mit "Methodentabelle nicht gefunden" ab, obwohl
' alles in Ordnung ist. Am 2026-09-10 genau so passiert.
'
' Der erwartete Methodenname wird gegengeprueft, damit nie in eine falsche
' Methode geschrieben wird.
'
' Aufruf: SapGuiMethodeneditorSchreiben.vbs <SitzungsIndex> <Methode> <Datei>
'=======================================================================
Dim sesIdx, methode, datei, fso, quelle, a, app, c, s, ed, offen

If WScript.Arguments.Count < 3 Then
  WScript.Echo "Aufruf: SapGuiMethodeneditorSchreiben.vbs <SitzungsIndex> <Methode> <Datei>"
  WScript.Quit 1
End If
sesIdx  = CInt(WScript.Arguments(0))
methode = UCase(Trim(WScript.Arguments(1)))
datei   = WScript.Arguments(2)

Set fso = CreateObject("Scripting.FileSystemObject")
If Not fso.FileExists(datei) Then WScript.Echo "Datei fehlt: " & datei : WScript.Quit 2
quelle = fso.OpenTextFile(datei, 1).ReadAll
quelle = Replace(quelle, vbCrLf, vbLf)
quelle = Replace(quelle, vbCr, vbLf)
quelle = Replace(quelle, vbLf, vbCrLf)

If InStr(1, quelle, "METHOD", vbTextCompare) = 0 Or InStr(1, quelle, "ENDMETHOD", vbTextCompare) = 0 Then
  WScript.Echo "ABBRUCH: kein METHOD/ENDMETHOD in der Datei."
  WScript.Quit 3
End If

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set c = app.Children(0)
Set s = c.Children(CLng(sesIdx))

offen = ""
On Error Resume Next
offen = UCase(Trim(s.FindById("wnd[0]/usr/txtDY0200_CPDNAME").Text))
If Err.Number <> 0 Then
  WScript.Echo "ABBRUCH: kein Methodeneditor offen."
  WScript.Quit 4
End If
Err.Clear
On Error GoTo 0

If offen <> methode Then
  WScript.Echo "ABBRUCH: offen ist '" & offen & "', erwartet '" & methode & "'."
  WScript.Quit 5
End If
WScript.Echo "OFFEN=" & offen & " STATUS_VORHER=" & s.FindById("wnd[0]/usr/txtDY0200_STATUS").Text

Set ed = Nothing
On Error Resume Next
Set ed = s.FindById("wnd[0]/usr/subEDITORSUBSCREEN:SAPLEDITOR_START:8430/cntlEDITOR/shellcont/shell")
Err.Clear
On Error GoTo 0
If ed Is Nothing Then WScript.Echo "Editorsteuerelement nicht gefunden" : WScript.Quit 6

ed.SelectAll
ed.ReplaceSelection quelle
WScript.Sleep 1200

s.FindById("wnd[0]/tbar[0]/btn[11]").Press
WScript.Sleep 3000

On Error Resume Next
WScript.Echo "MELDUNGSTYP=" & s.FindById("wnd[0]/sbar").MessageType
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar").Text
WScript.Echo "STATUS_NACHHER=" & s.FindById("wnd[0]/usr/txtDY0200_STATUS").Text
Err.Clear
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
If Err.Number <> 0 Then WScript.Echo "DIALOG=(keiner)"
On Error GoTo 0
