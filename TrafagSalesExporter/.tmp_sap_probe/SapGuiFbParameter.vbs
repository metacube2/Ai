Option Explicit
' Traegt im offenen Function Builder (Aendern) einen Import- oder Exportparameter in Zeile 0 ein,
' TYPE <Typ>, Wertuebergabe, und sichert. Oder waehlt den Reiter Eigenschaften und listet ihn.
' Angelegt 2026-10-12 (Z_KDAUF_PAR_RECHNEN).
'
' Aufruf: SapGuiFbParameter.vbs <SitzungsIndex> import|export <Name> <Typ>
'         SapGuiFbParameter.vbs <SitzungsIndex> eigenschaften

Dim a, app, s, base, tab, i, c, art
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set s = app.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Echo "ABBRUCH: nicht T76" : WScript.Quit 2
art = WScript.Arguments(1)

Sub Liste(obj, tiefe)
  Dim j, ch
  On Error Resume Next
  For j = 0 To obj.Children.Count - 1
    Set ch = obj.Children(CLng(j))
    If ch.Type = "GuiRadioButton" Or ch.Type = "GuiCheckBox" Then
      WScript.Echo ch.Id & " [" & ch.Type & "] " & ch.Text & " =" & ch.Selected
    End If
    If tiefe < 8 Then Liste ch, tiefe + 1
  Next
  On Error GoTo 0
End Sub

If art = "eigenschaften" Then
  s.FindById("wnd[0]/usr/tabsFUNC_TAB_STRIP/tabpHEADER").Select
  WScript.Sleep 800
  Liste s.FindById("wnd[0]/usr/tabsFUNC_TAB_STRIP/tabpHEADER"), 0
  WScript.Quit 0
End If

If art = "import" Then
  tab = "IMPORT"
Else
  tab = "EXPORT"
End If
s.FindById("wnd[0]/usr/tabsFUNC_TAB_STRIP/tabp" & tab).Select
WScript.Sleep 800
base = "wnd[0]/usr/tabsFUNC_TAB_STRIP/tabp" & tab & "/ssubSCREEN_HEADER:SAPLSFUNCTION_BUILDER:"
' Teilbildnummer unterscheidet sich je Reiter: suchen
Dim usr, sub1, tbl
Set usr = s.FindById("wnd[0]/usr/tabsFUNC_TAB_STRIP/tabp" & tab)
For i = 0 To usr.Children.Count - 1
  Set sub1 = usr.Children(CLng(i))
  If InStr(sub1.Id, "ssubSCREEN_HEADER") > 0 Then base = Mid(sub1.Id, InStr(sub1.Id, "wnd[0]")) & "/"
Next
Set tbl = Nothing
Set sub1 = s.FindById(base)
For i = 0 To sub1.Children.Count - 1
  If sub1.Children(CLng(i)).Type = "GuiTableControl" Then Set tbl = sub1.Children(CLng(i))
Next
base = Mid(tbl.Id, InStr(tbl.Id, "wnd[0]")) & "/"
WScript.Echo "TABELLE=" & base
s.FindById(base & "txtRSFBPARA-PARAMETER[0,0]").Text = WScript.Arguments(2)
s.FindById(base & "ctxtRSFBPARA-TYPEFIELD[1,0]").Text = "TYPE"
s.FindById(base & "ctxtRSFBPARA-STRUCTURE[2,0]").Text = WScript.Arguments(3)
' Wertuebergabe: Ankreuzfeld RSFBPARA-VALUE in Zeile 0 suchen
Dim gesetzt
gesetzt = False
For i = 0 To tbl.Children.Count - 1
  Set c = tbl.Children(CLng(i))
  If InStr(c.Id, "chkRSFBPARA-VALUE[") > 0 And Right(c.Id, 3) = ",0]" Then
    c.Selected = True
    gesetzt = True
  End If
Next
WScript.Echo "WERTUEBERGABE=" & gesetzt
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 800
s.FindById("wnd[0]/tbar[0]/btn[11]").Press
WScript.Sleep 1200
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
On Error Resume Next
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
