' Test ZC22 (ZM_OFFENE_FAUF) in einer eigenen T76-Sitzung: Auftragsbereich setzen, ausfuehren, Listenzeilen mit Status ausgeben.
' Angelegt 2026-11-09 (Systemstatus ungekuerzt). Aufruf: SapGuiZc22Test.vbs <Sitzung> <AufnrVon> <AufnrBis>
Set app = GetObject("SAPGUI").GetScriptingEngine
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  If c.Children.Count > 0 Then
    If c.Children(0).Info.SystemName = "T76" Then Set s = c.Children(CLng(WScript.Arguments(0)))
  End If
Next
If s.Info.SystemName <> "T76" Then WScript.Echo "ABBRUCH: nicht T76" : WScript.Quit 2
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nZC22"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2000
s.FindById("wnd[0]/usr/ctxtS_AUFNR-LOW").Text = WScript.Arguments(1)
s.FindById("wnd[0]/usr/ctxtS_AUFNR-HIGH").Text = WScript.Arguments(2)
s.FindById("wnd[0]/tbar[1]/btn[8]").Press
WScript.Sleep 6000
WScript.Echo "TITEL=" & s.FindById("wnd[0]").Text & " SBAR=" & s.FindById("wnd[0]/sbar").Text
Set usr = s.FindById("wnd[0]/usr")
Dim zeilen : Set zeilen = CreateObject("Scripting.Dictionary")
For Each ch In usr.Children
  On Error Resume Next
  t = ch.Text
  If Err.Number = 0 And Trim(t) <> "" Then
    p1 = InStr(ch.Id, "[") : p2 = InStr(ch.Id, "]")
    k = Mid(ch.Id, p1 + 1, p2 - p1 - 1)
    y = CLng(Split(k, ",")(1))
    If zeilen.Exists(y) Then zeilen(y) = zeilen(y) & " | " & Trim(t) Else zeilen.Add y, Trim(t)
  End If
  On Error GoTo 0
Next
For Each y In zeilen.Keys
  If y < 40 Then WScript.Echo y & ": " & zeilen(y)
Next
