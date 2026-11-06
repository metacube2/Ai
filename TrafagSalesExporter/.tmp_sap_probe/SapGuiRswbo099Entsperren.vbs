' SE03 "Objekte entsperren (Expertentool)" = Report RSWBO099 schrittweise bedienen, nur eigene T76-Sitzung
' (Verbindung 0 muss T76 sein, sonst Abbruch). Angelegt 2026-11-06 (ZZPRDAT Anzeige, Georgs T76K911783/784).
' Vorher mit SapGuiSe03Knoten.vbs <Ses> RSWBO099 starten. "fuellen" kreuzt Versionieren/Freigeben/Kopie AB.
' Achtung: Sperren haengen an der AUFGABE; den Lauf mit der Aufgabennummer wiederholen. Nachher TLOCK pruefen.
' Aufruf: SapGuiRswbo099Entsperren.vbs <SesIdx> fuellen <Auftrag> | starten | knopf <Id> | status
'   Sicherheitsabfrage: knopf wnd[1]/usr/btnSPOP-VAROPTION1 (Entsperren), Info: knopf wnd[1]/tbar[0]/btn[0]
Set app = GetObject("SAPGUI").GetScriptingEngine
Set s = app.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Or s.Info.Client <> "100" Then WScript.Echo "nicht T76/100" : WScript.Quit 2
If CLng(WScript.Arguments(0)) = 0 Then WScript.Echo "ses[0] ist Ingos Sitzung" : WScript.Quit 2
modus = LCase(WScript.Arguments(1))
Select Case modus
  Case "fuellen"
    If s.Info.Program <> "RSWBO099" Then WScript.Echo "nicht auf RSWBO099" : WScript.Quit 3
    s.FindById("wnd[0]/usr/ctxtTRKORR").Text = WScript.Arguments(2)
    s.FindById("wnd[0]/usr/chkP_VERSIO").Selected = False
    s.FindById("wnd[0]/usr/chkP_SETREL").Selected = False
    s.FindById("wnd[0]/usr/chkP_COPY_T").Selected = False
  Case "starten"
    If s.Info.Program <> "RSWBO099" Then WScript.Echo "nicht auf RSWBO099" : WScript.Quit 3
    If s.FindById("wnd[0]/usr/chkP_SETREL").Selected Or s.FindById("wnd[0]/usr/chkP_COPY_T").Selected Or s.FindById("wnd[0]/usr/chkP_VERSIO").Selected Then
      WScript.Echo "ABBRUCH: Zusatzaktion angekreuzt" : WScript.Quit 4
    End If
    s.FindById("wnd[0]").SendVKey 8
    WScript.Sleep 2500
  Case "knopf"
    s.FindById(WScript.Arguments(2)).Press
    WScript.Sleep 2500
End Select
On Error Resume Next
WScript.Echo "PROG=" & s.Info.Program & " DYNPRO=" & s.Info.ScreenNumber
WScript.Echo "TRKORR=" & s.FindById("wnd[0]/usr/ctxtTRKORR").Text
WScript.Echo "UNLOCK=" & s.FindById("wnd[0]/usr/chkP_UNLOCK").Selected & " VERSIO=" & s.FindById("wnd[0]/usr/chkP_VERSIO").Selected & " SETREL=" & s.FindById("wnd[0]/usr/chkP_SETREL").Selected & " COPY_T=" & s.FindById("wnd[0]/usr/chkP_COPY_T").Selected
Err.Clear
WScript.Echo "SBAR=" & s.FindById("wnd[0]/sbar").MessageType & " " & s.FindById("wnd[0]/sbar").Text
For k = 1 To 3
  Err.Clear
  t = s.FindById("wnd[" & k & "]").Text
  If Err.Number = 0 Then WScript.Echo "wnd[" & k & "]=" & t
Next
