Option Explicit
'=======================================================================
' Setzt eine GET-Anfrage im SAP Gateway Client (/IWFND/GW_CLIENT) ab und
' schreibt die Antwort in eine Datei.
'
' Warum dieser Weg: OData von aussen scheitert an der Anmeldung. Weder
' Basic-Auth noch -UseDefaultCredentials kommen durch, beide liefern 401
' (dokumentiert seit 2026-08-18, erneut bestaetigt am 2026-09-10). Der
' Gateway Client laeuft INNERHALB der bestehenden SAP-Sitzung und braucht
' deshalb gar keine zweite Anmeldung. Fuer jede Gegenprobe an einem Service
' ist er der guenstigste Weg.
'
' Aufruf: SapGuiGatewayClient.vbs <SitzungsIndex> <RequestUri> <Ausgabedatei>
'=======================================================================
Dim sesIdx, uri, ziel, a, app, c, s, ed, fso, aus, txt, i, sh

sesIdx = CInt(WScript.Arguments(0))
uri    = WScript.Arguments(1)
ziel   = WScript.Arguments(2)

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set c = app.Children(0)
Set s = c.Children(CLng(sesIdx))

If InStr(s.FindById("wnd[0]").Text, "Gateway Client") = 0 Then
  WScript.Echo "ABBRUCH: /IWFND/GW_CLIENT ist nicht das aktive Bild."
  WScript.Quit 2
End If

s.FindById("wnd[0]/usr/radRB_GET").Select
Set ed = s.FindById("wnd[0]/usr/cntlURI_AREA/shellcont/shell")
ed.Text = uri

WScript.Sleep 800

' F8 sendet die Anfrage.
s.FindById("wnd[0]").SendVKey 8
WScript.Sleep 6000

On Error Resume Next
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar").Text
WScript.Echo "TYP=" & s.FindById("wnd[0]/sbar").MessageType
Err.Clear
WScript.Echo "STATUSCODE=" & s.FindById("wnd[0]/usr/txtGS_SC-STATUS_CODE").Text
Err.Clear
On Error GoTo 0

' Die Antwort steht in einem der Editorbereiche des Splitters.
aus = ""
For i = 0 To 3
  txt = ""
  On Error Resume Next
  Set sh = s.FindById("wnd[0]/usr/cntlGUI_AREA/shellcont/shell/shellcont[" & i & "]/shell/shellcont/shell")
  txt = sh.Text
  Err.Clear
  On Error GoTo 0
  If Len(txt) > Len(aus) Then aus = txt
Next

If aus = "" Then
  WScript.Echo "WARNUNG: kein Antworttext aus den Editorbereichen gelesen."
Else
  Set fso = CreateObject("Scripting.FileSystemObject")
  fso.CreateTextFile(ziel, True).Write aus
  WScript.Echo "GESCHRIEBEN=" & ziel & " ZEICHEN=" & Len(aus)
End If
