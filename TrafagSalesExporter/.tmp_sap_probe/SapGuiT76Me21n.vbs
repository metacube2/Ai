' T76: Bestellung in ME21N erfassen (eine Position), nur in der eigenen Sitzung aus t76_eigene_sitzung.txt.
' Angelegt 2026-11-06 (IC Staffelbasis). Sichert NICHT; das macht der Aufrufer danach mit "vkey <ses> 11".
' Aufruf: SapGuiT76Me21n.vbs <ses> <Lieferant> <EKORG> <EKGRP> <BUKRS> <Material> <Menge> <Werk> <Lagerort>
Option Explicit
Dim app, con, i, ses, fso, idFile, eigeneId, idx, p, h, t, k
Set app = GetObject("SAPGUI").GetScriptingEngine
Set fso = CreateObject("Scripting.FileSystemObject")
idFile = fso.BuildPath(fso.GetParentFolderName(WScript.ScriptFullName), "t76_eigene_sitzung.txt")
Set con = Nothing
For i = 0 To app.Children.Count - 1
  If app.Children(CLng(i)).Children.Count > 0 Then
    If app.Children(CLng(i)).Children(0).Info.SystemName = "T76" And app.Children(CLng(i)).Children(0).Info.Client = "100" Then Set con = app.Children(CLng(i))
  End If
Next
If con Is Nothing Then WScript.Echo "Keine T76/100-Verbindung": WScript.Quit 1
eigeneId = Trim(fso.OpenTextFile(idFile).ReadLine)
idx = CLng(WScript.Arguments(0))
If idx = 0 Then WScript.Echo "ABBRUCH: ses[0]": WScript.Quit 1
Set ses = con.Children(CLng(idx))
If ses.Id <> eigeneId Then WScript.Echo "ABBRUCH: nicht die eigene Sitzung": WScript.Quit 1
If ses.Info.Transaction <> "ME21N" Then WScript.Echo "ABBRUCH: Sitzung steht nicht in ME21N": WScript.Quit 1

p = "wnd[0]/usr/subSUB0:SAPLMEGUI:0013/"
ses.findById(p & "subSUB0:SAPLMEGUI:0030/subSUB1:SAPLMEGUI:1105/ctxtMEPO_TOPLINE-SUPERFIELD").Text = WScript.Arguments(1)
h = p & "subSUB1:SAPLMEVIEWS:1100/subSUB2:SAPLMEVIEWS:1200/subSUB1:SAPLMEGUI:1102/tabsHEADER_DETAIL/tabpTABHDT8/ssubTABSTRIPCONTROL2SUB:SAPLMEGUI:1221/"
ses.findById(h & "ctxtMEPO1222-EKORG").Text = WScript.Arguments(2)
ses.findById(h & "ctxtMEPO1222-EKGRP").Text = WScript.Arguments(3)
ses.findById(h & "ctxtMEPO1222-BUKRS").Text = WScript.Arguments(4)
ses.findById("wnd[0]").sendVKey 0
WScript.Sleep 2000
WScript.Echo "Nach Kopf: SBAR(" & ses.findById("wnd[0]/sbar").MessageType & ")=" & ses.findById("wnd[0]/sbar").Text
t = p & "subSUB2:SAPLMEVIEWS:1100/subSUB2:SAPLMEVIEWS:1200/subSUB1:SAPLMEGUI:1211/tblSAPLMEGUITC_1211/"
ses.findById(t & "ctxtMEPO1211-EMATN[4,0]").Text = WScript.Arguments(5)
ses.findById(t & "txtMEPO1211-MENGE[6,0]").Text = WScript.Arguments(6)
ses.findById(t & "ctxtMEPO1211-NAME1[15,0]").Text = WScript.Arguments(7)
ses.findById(t & "ctxtMEPO1211-LGOBE[16,0]").Text = WScript.Arguments(8)
ses.findById("wnd[0]").sendVKey 0
WScript.Sleep 2500
WScript.Echo "Nach Position: SBAR(" & ses.findById("wnd[0]/sbar").MessageType & ")=" & ses.findById("wnd[0]/sbar").Text
On Error Resume Next
For k = 1 To 3
  Err.Clear
  WScript.Echo "wnd[" & k & "]=" & ses.findById("wnd[" & k & "]").Text
Next
Err.Clear
WScript.Echo "Pos: MAT=" & ses.findById(t & "ctxtMEPO1211-EMATN[4,0]").Text & " MENGE=" & ses.findById(t & "txtMEPO1211-MENGE[6,0]").Text & _
  " ME=" & ses.findById(t & "ctxtMEPO1211-MEINS[7,0]").Text & " NETPR=" & ses.findById(t & "txtMEPO1211-NETPR[10,0]").Text & _
  " WAERS=" & ses.findById(t & "txtMEPO1211-WAERS[11,0]").Text & " WERK=" & ses.findById(t & "ctxtMEPO1211-NAME1[15,0]").Text & _
  " LGORT=" & ses.findById(t & "ctxtMEPO1211-LGOBE[16,0]").Text & " PSTYP=" & ses.findById(t & "ctxtMEPO1211-EPSTP[3,0]").Text
