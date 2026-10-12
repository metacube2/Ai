Option Explicit
' Drueckt Aktivieren (btn[27]) in der Sitzung und listet den Dialog "Inaktive Objekte", Reiter Lokal.
' Angelegt 2026-10-12. Aufruf: SapGuiAktivierenListe.vbs <SitzungsIndex>
Dim a, s, basis, tbl, r, nam, typ
Set a = GetObject("SAPGUI")
Set s = a.GetScriptingEngine.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Quit 2
s.FindById("wnd[0]/tbar[1]/btn[27]").Press
WScript.Sleep 2500
On Error Resume Next
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
s.FindById("wnd[1]/usr/tabsACT_TAB_STRIP/tabpLOCAL").Select
WScript.Sleep 800
basis = "wnd[1]/usr/tabsACT_TAB_STRIP/tabpLOCAL/ssubSCREEN_ACTIVATE:SAPLSEWORKINGAREA:0202/tblSAPLSEWORKINGAREAT_LOCAL"
Set tbl = s.FindById(basis)
For r = 0 To tbl.VisibleRowCount - 1
  nam = "" : typ = ""
  nam = Trim(s.FindById(basis & "/txtWORK_ITEMS-OBJ_NAME[2," & r & "]").Text)
  typ = Trim(s.FindById(basis & "/txtWORK_ITEMS-OBJECT[1," & r & "]").Text)
  If nam <> "" Then WScript.Echo "ZEILE " & r & " " & typ & " " & nam
Next
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
