Option Explicit
' Markiert genau die genannten Objekte im Dialog "Inaktive Objekte" und aktiviert sie
' in DERSELBEN COM-Sitzung. Getrennte Prozesse verlieren sonst den Bildschirmzustand.
'
' Die Sitzung wird ueber ihren INDEX angesprochen, nicht ueber den Transaktionscode:
' mehrere Sitzungen koennen denselben Code tragen, und dann landen Klicks in der
' falschen. Genau das ist am 2026-09-10 passiert, nachdem /nSE38 in die ST22-Sitzung
' geschickt wurde.
'
' Aufruf: SapGuiAktivieren.vbs <SitzungsIndex> <Name> [weitere Namen...]

Dim a, app, c, s, i, r, name, treffer, wanted, basis, tbl, sesIdx

sesIdx = CInt(WScript.Arguments(0))
Set wanted = CreateObject("Scripting.Dictionary")
For i = 1 To WScript.Arguments.Count - 1
  wanted.Add UCase(Trim(WScript.Arguments(i))), True
Next

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set c = app.Children(0)
If sesIdx >= c.Children.Count Then
  WScript.Echo "Sitzungsindex " & sesIdx & " gibt es nicht, offen sind " & c.Children.Count
  WScript.Quit 2
End If
Set s = c.Children(CLng(sesIdx))
WScript.Echo "SITZUNG=" & sesIdx & " TX=" & s.Info.Transaction

On Error Resume Next
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
If Err.Number <> 0 Then
  WScript.Echo "Kein Dialog wnd[1] in dieser Sitzung"
  WScript.Quit 3
End If
Err.Clear
On Error GoTo 0

basis = "wnd[1]/usr/tabsACT_TAB_STRIP/tabpTRANSPORT/ssubSCREEN_ACTIVATE:SAPLSEWORKINGAREA:0201/"
Set tbl = Nothing
On Error Resume Next
Set tbl = s.FindById(basis & "tblSAPLSEWORKINGAREAT_TRANSPORT")
If tbl Is Nothing Then
  basis = "wnd[1]/usr/tabsACT_TAB_STRIP/tabpLOCAL/ssubSCREEN_ACTIVATE:SAPLSEWORKINGAREA:0200/"
  Set tbl = s.FindById(basis & "tblSAPLSEWORKINGAREAT_LOCAL")
End If
Err.Clear
On Error GoTo 0
If tbl Is Nothing Then WScript.Echo "Objekttabelle nicht gefunden" : WScript.Quit 4

treffer = 0
For r = 0 To tbl.RowCount - 1
  name = ""
  On Error Resume Next
  name = UCase(Trim(s.FindById(basis & "tbl" & tbl.Name & "/txtWORK_ITEMS-OBJ_NAME[2," & r & "]").Text))
  Err.Clear
  On Error GoTo 0
  If name <> "" And Left(name, 1) <> "_" Then
    If wanted.Exists(name) Then
      tbl.GetAbsoluteRow(r).Selected = True
      treffer = treffer + 1
      WScript.Echo "MARKIERT   " & name
    Else
      tbl.GetAbsoluteRow(r).Selected = False
      WScript.Echo "uebergangen " & name
    End If
  End If
Next

If treffer = 0 Then WScript.Echo "TREFFER=0, kein Aktivieren." : WScript.Quit 5
WScript.Echo "TREFFER=" & treffer

s.FindById("wnd[1]/tbar[0]/btn[0]").Press
WScript.Sleep 2000

' Der Dialog "Fehler beim Aktivieren" erscheint auch bei blossen WARNUNGEN.
' Er wird hier nur GEMELDET, nicht bestaetigt: ob trotz Meldung aktiviert wird,
' ist eine bewusste Entscheidung und gehoert nicht in ein Skript.
On Error Resume Next
WScript.Echo "STATUS=" & s.FindById("wnd[0]/sbar").Text
WScript.Echo "MELDUNGSTYP=" & s.FindById("wnd[0]/sbar").MessageType
Err.Clear
WScript.Echo "ZWEITER_DIALOG=" & s.FindById("wnd[2]").Text
If Err.Number <> 0 Then WScript.Echo "ZWEITER_DIALOG=(keiner)"
Err.Clear
WScript.Echo "PROGRAMMSTATUS=" & s.FindById("wnd[0]/usr/txtSTATUS_TEXT").Text
On Error GoTo 0
