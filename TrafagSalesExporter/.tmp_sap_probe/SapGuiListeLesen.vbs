' Liest eine klassische Liste (lbl[x,y]) einer T76-Sitzung zeilenweise aus (nur lesen). Angelegt 2026-11-09.
' Aufruf: SapGuiListeLesen.vbs <Sitzung> [Seiten]   Seiten = wie oft mit Bild ab (82) weiterblaettern, Vorgabe 0
Set app = GetObject("SAPGUI").GetScriptingEngine
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  If c.Children.Count > 0 Then
    If c.Children(0).Info.SystemName = "T76" Then Set s = c.Children(CLng(WScript.Arguments(0)))
  End If
Next
If s.Info.SystemName <> "T76" Then WScript.Quit 2
seiten = 0
If WScript.Arguments.Count > 1 Then seiten = CInt(WScript.Arguments(1))
WScript.Echo "TITEL=" & s.FindById("wnd[0]").Text & " SBAR=" & s.FindById("wnd[0]/sbar").Text
For p = 0 To seiten
  Set zeilen = CreateObject("Scripting.Dictionary")
  For Each ch In s.FindById("wnd[0]/usr").Children
    On Error Resume Next
    t = ch.Text
    If Err.Number = 0 And Trim(t) <> "" And InStr(ch.Id, "lbl[") > 0 Then
      k = Mid(ch.Id, InStr(ch.Id, "[") + 1)
      k = Left(k, InStr(k, "]") - 1)
      y = CLng(Split(k, ",")(1))
      If zeilen.Exists(y) Then zeilen(y) = zeilen(y) & " " & Trim(t) Else zeilen.Add y, Trim(t)
    End If
    On Error GoTo 0
  Next
  For Each y In zeilen.Keys
    WScript.Echo zeilen(y)
  Next
  If p < seiten Then s.FindById("wnd[0]").SendVKey 82 : WScript.Sleep 800
Next
