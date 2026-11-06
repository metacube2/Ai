' NUR ANZEIGE: CO03 in eigener T76-Sitzung (vorher SapGuiOkCode.vbs <Ses> /nCO03). Angelegt 2026-11-06.
' Reiter "TRAFAG Daten" = wnd[0]/usr/tabsTABSTRIP_0115/tabp+COI, Feld darin txtCI_AUFK-ZZPRDAT (Dump: SapGuiFensterDump.vbs).
' Aufruf: SapGuiCo03Reiter.vbs <SesIdx> auftrag <Nr> | reiter <TabId>
Set app = GetObject("SAPGUI").GetScriptingEngine
Set s = app.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Or s.Info.Client <> "100" Then WScript.Echo "nicht T76/100" : WScript.Quit 2
If CLng(WScript.Arguments(0)) = 0 Then WScript.Echo "ses[0] ist Ingos Sitzung" : WScript.Quit 2
If s.Info.Transaction <> "CO03" Then WScript.Echo "nicht CO03 (" & s.Info.Transaction & ")" : WScript.Quit 3
Select Case LCase(WScript.Arguments(1))
  Case "auftrag"
    s.FindById("wnd[0]/usr/ctxtCAUFVD-AUFNR").Text = WScript.Arguments(2)
    s.FindById("wnd[0]").SendVKey 0
    WScript.Sleep 3000
  Case "reiter"
    s.FindById(WScript.Arguments(2)).Select
    WScript.Sleep 2500
End Select
WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text & " PROG=" & s.Info.Program & " DYNPRO=" & s.Info.ScreenNumber
On Error Resume Next
WScript.Echo "SBAR=" & s.FindById("wnd[0]/sbar").Text
Err.Clear
For k = 1 To 2
  Err.Clear
  t = s.FindById("wnd[" & k & "]").Text
  If Err.Number = 0 Then WScript.Echo "wnd[" & k & "]=" & t
Next
