Option Explicit
' Traegt den Quelltext eines Funktionsbausteins aus einer Datei ein und sichert.
'
' Aufruf: SapGuiSetFunctionSourceFromFile.vbs <Funktionsbaustein> <Quelltextdatei>
'
' Die Datei muss FUNCTION ... ENDFUNCTION enthalten, weil SelectAll plus
' ReplaceSelection den gesamten Editorpuffer ersetzt.

Dim fb, datei, s, fso, quelle, editor

fb = WScript.Arguments(0)
datei = WScript.Arguments(1)

Set fso = CreateObject("Scripting.FileSystemObject")
If Not fso.FileExists(datei) Then
  WScript.Echo "Datei nicht gefunden: " & datei
  WScript.Quit 2
End If
quelle = fso.OpenTextFile(datei, 1).ReadAll

If InStr(1, quelle, "FUNCTION", vbTextCompare) = 0 Or InStr(1, quelle, "ENDFUNCTION", vbTextCompare) = 0 Then
  WScript.Echo "ABBRUCH: Die Datei enthaelt kein FUNCTION/ENDFUNCTION."
  WScript.Quit 3
End If

Function SitzungHolen(tx)
  Dim a, app, c, s2, i, j, erste
  Set a = GetObject("SAPGUI")
  Set app = a.GetScriptingEngine
  Set erste = Nothing
  For i = 0 To app.Children.Count - 1
    Set c = app.Children(CLng(i))
    For j = 0 To c.Children.Count - 1
      Set s2 = c.Children(CLng(j))
      If s2.Info.SystemName = "T76" And s2.Info.Client = "100" Then
        If erste Is Nothing Then Set erste = s2
        If UCase(s2.Info.Transaction) = UCase(tx) Then
          Set SitzungHolen = s2
          Exit Function
        End If
      End If
    Next
  Next
  If erste Is Nothing Then WScript.Quit 4
  erste.FindById("wnd[0]/tbar[0]/okcd").Text = "/o" & tx
  erste.FindById("wnd[0]").SendVKey 0
  WScript.Sleep 2500
  Set SitzungHolen = SitzungHolen(tx)
End Function

Set s = SitzungHolen("SE37")
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE37"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2500
Set s = SitzungHolen("SE37")

s.FindById("wnd[0]/usr/ctxtRS38L-NAME").Text = fb
s.FindById("wnd[0]/usr/btnBUT4").Press   ' Aendern
WScript.Sleep 3000

If InStr(1, s.FindById("wnd[0]").Text, fb, vbTextCompare) = 0 Then
  WScript.Echo "ABBRUCH: unerwartetes Fenster " & s.FindById("wnd[0]").Text
  WScript.Quit 5
End If

s.FindById("wnd[0]/usr/tabsFUNC_TAB_STRIP/tabpSOURCE").Select
WScript.Sleep 1200

Set editor = s.FindById("wnd[0]/usr/tabsFUNC_TAB_STRIP/tabpSOURCE/ssubSCREEN_HEADER:SAPLEDITOR_START:8430/cntlEDITOR/shellcont/shell")
editor.SelectAll
editor.ReplaceSelection quelle
WScript.Sleep 600

s.FindById("wnd[0]/tbar[0]/btn[11]").Press
WScript.Sleep 2000
WScript.Echo "MELDUNGSTYP=" & s.FindById("wnd[0]/sbar").MessageType
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
WScript.Quit 0
