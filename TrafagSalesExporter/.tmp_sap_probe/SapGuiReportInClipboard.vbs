Option Explicit
' Kopiert den Quelltext des in SE38 angezeigten Reports in die Windows-Zwischenablage.
' Der Editor laesst sich nicht auslesen (Abschnitt 2), der Weg ueber Markieren und
' "Hilfsmittel > Block/Ablage > Kopieren in Clipboard" liefert ihn trotzdem.
' Aufruf: SapGuiReportInClipboard.vbs <SitzungsIndex>
Dim a, app, s, ed
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set s = app.Children(0).Children(CLng(WScript.Arguments(0)))
Set ed = s.FindById("wnd[0]/usr/cntlEDITOR/shellcont/shell")
ed.SelectAll
s.FindById("wnd[0]/mbar/menu[3]/menu[8]/menu[3]").Select
WScript.Echo "FENSTER=" & s.ActiveWindow.Text
WScript.Echo "STATUS=" & s.FindById("wnd[0]/sbar").Text
