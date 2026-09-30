Option Explicit
'=======================================================================
' GET im Gateway Client (/IWFND/GW_CLIENT) absetzen und Statuscode,
' Begruendung, content-length und Laufzeit ausgeben.
'
' Der Antwortrumpf ist ueber Scripting nicht lesbar (SAP_ARBEITSWEISE,
' Abschnitt "OData pruefen, ohne sich anzumelden"). Status und Laenge
' reichen fuer: gibt es das Set, liefert es Daten, laufen die anderen Sets.
'
' Aufruf: SapGuiGatewayStatus.vbs <SitzungsIndex> <RequestUri>
'=======================================================================
Dim a, app, s, ed, g, r, t0, name
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set s = app.Children(0).Children(CLng(WScript.Arguments(0)))

If InStr(s.FindById("wnd[0]").Text, "Gateway Client") = 0 Then
  WScript.Echo "ABBRUCH: /IWFND/GW_CLIENT ist nicht das aktive Bild."
  WScript.Quit 2
End If

s.FindById("wnd[0]/usr/radRB_GET").Select
Set ed = s.FindById("wnd[0]/usr/cntlURI_AREA/shellcont/shell")
ed.Text = WScript.Arguments(1)

t0 = Timer
s.FindById("wnd[0]").SendVKey 8
WScript.Echo "DAUER_S=" & FormatNumber(Timer - t0, 1)

' shellcont[1] ist der Antwortkopf, shellcont[0] der (leere) Anfragekopf.
Set g = s.FindById("wnd[0]/usr/cntlGUI_AREA/shellcont/shell/shellcont[1]/shell")
For r = 0 To g.RowCount - 1
  name = g.GetCellValue(r, "NAME")
  If name = "~status_code" Or name = "~status_reason" Or name = "content-length" Then
    WScript.Echo name & "=" & g.GetCellValue(r, "VALUE")
  End If
Next
