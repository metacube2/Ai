Option Explicit
' Startet Z_KOI_REICHW_VGL in der eigenen T76-Sitzung und gibt die Ergebnisliste aus.
' Aufruf: vgl_lauf.vbs <Sitzung> <MatVon|-> <MatBis|-> <Optionen: Buchstaben i r a o (inkl, mitVerschr, Rueckrechnung, OPT zuerst)> [<DatumVon TT.MM.JJJJ>]
Dim app, c, ci, s, k, o, zeilen, y, x, t0, usr, key, txt
Set app = GetObject("SAPGUI").GetScriptingEngine
For ci = 0 To app.Children.Count - 1
  If app.Children(CLng(ci)).Children.Count > 0 Then
    If app.Children(CLng(ci)).Children(0).Info.SystemName = "T76" Then Set c = app.Children(CLng(ci))
  End If
Next
Set s = c.Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Or CLng(WScript.Arguments(0)) = 0 Then WScript.Echo "ABBRUCH": WScript.Quit 2

s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE38"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 1500
s.FindById("wnd[0]/usr/ctxtRS38M-PROGRAMM").Text = "Z_KOI_REICHW_VGL"
s.FindById("wnd[0]").SendVKey 8
WScript.Sleep 2000

If WScript.Arguments(1) <> "-" Then s.FindById("wnd[0]/usr/ctxtS_MATNR-LOW").Text = WScript.Arguments(1)
If WScript.Arguments(2) <> "-" Then s.FindById("wnd[0]/usr/ctxtS_MATNR-HIGH").Text = WScript.Arguments(2)
Dim opt: opt = WScript.Arguments(3)
s.FindById("wnd[0]/usr/chkP_PERINC").Selected = (InStr(opt, "i") > 0)
s.FindById("wnd[0]/usr/chkP_RWMIT").Selected = (InStr(opt, "r") > 0)
s.FindById("wnd[0]/usr/chkP_ANWREC").Selected = (InStr(opt, "a") > 0)
s.FindById("wnd[0]/usr/chkP_REIHE").Selected = (InStr(opt, "o") > 0)
s.FindById("wnd[0]/usr/chkP_LISTE").Selected = (InStr(opt, "l") > 0)
If WScript.Arguments.Count > 4 Then s.FindById("wnd[0]/usr/ctxtP_DLOW").Text = WScript.Arguments(4)

t0 = Timer
s.FindById("wnd[0]").SendVKey 8
WScript.Sleep 1000
WScript.Echo "GESAMT_S~" & Round(Timer - t0, 1)
On Error Resume Next
WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text & " SBAR=" & s.FindById("wnd[0]/sbar").Text
WScript.Echo "FENSTER1=" & s.FindById("wnd[1]").Text
Err.Clear
Set usr = s.FindById("wnd[0]/usr")
Set zeilen = CreateObject("Scripting.Dictionary")
For k = 0 To usr.Children.Count - 1
  Set o = usr.Children(CLng(k))
  If o.Type = "GuiLabel" Or o.Type = "GuiTextField" Then
    ' Id endet auf lbl[x,y]
    txt = o.Id
    x = Mid(txt, InStrRev(txt, "[") + 1)
    y = CLng(Split(Replace(x, "]", ""), ",")(1))
    If zeilen.Exists(y) Then
      zeilen(y) = zeilen(y) & " " & Trim(o.Text)
    Else
      zeilen.Add y, Trim(o.Text)
    End If
  End If
Next
For Each key In zeilen.Keys
  WScript.Echo zeilen(key)
Next

