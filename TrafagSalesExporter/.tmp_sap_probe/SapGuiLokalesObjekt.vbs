Option Explicit
' Waehlt im offenen Dialog "Objektkatalogeintrag anlegen" (beliebige Fensterstufe) "Lokales Objekt" ($TMP).
' Angelegt 2026-10-05; SapGuiChooseLocalObject.vbs kennt nur wnd[1].
'
' Aufruf: SapGuiLokalesObjekt.vbs <SitzungsIndex>

Dim a, app, s, i, w, k
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set s = app.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Echo "ABBRUCH: nicht T76" : WScript.Quit 2

For i = 3 To 1 Step -1
  Set w = Nothing
  On Error Resume Next
  Set w = s.FindById("wnd[" & i & "]")
  On Error GoTo 0
  If Not w Is Nothing Then
    If InStr(1, w.Text, "Objektkatalog", vbTextCompare) > 0 Then
      s.FindById("wnd[" & i & "]/tbar[0]/btn[7]").Press
      WScript.Sleep 2500
      Exit For
    End If
  End If
Next
On Error Resume Next
For k = 1 To 3
  Err.Clear
  WScript.Echo "wnd[" & k & "]=" & s.FindById("wnd[" & k & "]").Text
  If Err.Number <> 0 Then Exit For
Next
Err.Clear
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
