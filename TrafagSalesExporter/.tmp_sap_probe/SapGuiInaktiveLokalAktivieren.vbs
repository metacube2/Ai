Option Explicit
' Im Dialog "Inaktive Objekte", Reiter "Lokale Objekte": entmarkiert alles, markiert NUR die genannten
' Objektnamen und aktiviert. Fremde unfertige Objekte desselben Benutzers bleiben unberuehrt.
' Angelegt 2026-10-05 (Z_LOG_KAP_TEST).
'
' Aufruf: SapGuiInaktiveLokalAktivieren.vbs <SitzungsIndex> <Name> [...]

Dim a, app, s, basis, tbl, r, nam, i, treffer, k
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set s = app.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Echo "ABBRUCH: nicht T76" : WScript.Quit 2

s.FindById("wnd[1]/usr/tabsACT_TAB_STRIP/tabpLOCAL").Select
WScript.Sleep 800
s.FindById("wnd[1]/tbar[0]/btn[21]").Press
WScript.Sleep 800
basis = "wnd[1]/usr/tabsACT_TAB_STRIP/tabpLOCAL/ssubSCREEN_ACTIVATE:SAPLSEWORKINGAREA:0202/tblSAPLSEWORKINGAREAT_LOCAL"
Set tbl = s.FindById(basis)
treffer = 0
For r = 0 To tbl.VisibleRowCount - 1
  nam = ""
  On Error Resume Next
  nam = Trim(s.FindById(basis & "/txtWORK_ITEMS-OBJ_NAME[2," & r & "]").Text)
  On Error GoTo 0
  For i = 1 To WScript.Arguments.Count - 1
    If UCase(nam) = UCase(WScript.Arguments(i)) Then
      tbl.GetAbsoluteRow(tbl.VerticalScrollbar.Position + r).Selected = True
      treffer = treffer + 1
      WScript.Echo "MARKIERT=" & nam
    End If
  Next
Next
If treffer <> WScript.Arguments.Count - 1 Then
  WScript.Echo "ABBRUCH: nicht alle Namen gefunden (" & treffer & ")"
  WScript.Quit 3
End If
s.FindById("wnd[1]/tbar[0]/btn[0]").Press
WScript.Sleep 3000
On Error Resume Next
For k = 1 To 2
  Err.Clear
  WScript.Echo "wnd[" & k & "]=" & s.FindById("wnd[" & k & "]").Text
  If Err.Number <> 0 Then Exit For
Next
Err.Clear
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
