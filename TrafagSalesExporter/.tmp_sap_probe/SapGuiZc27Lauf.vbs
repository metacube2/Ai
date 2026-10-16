' NUR LESEN (T76): ZC27 (ZPP_RM_MENGEN) mit Arbeitsplatz/Bereich, Werk 1100, Datum und optional Typencode starten,
' Zeilenzahl und Gutmenge (LMNGA) je Arbeitsplatz aus dem ALV ausgeben (Summenzeilen ohne Auftrag zaehlen nicht).
' Angelegt 2026-10-16 fuer den Abgleich ShopRmSet gegen ZC27.
' Aufruf: SapGuiZc27Lauf.vbs <Sitzung> <ARBPL oder VON,BIS> <TT.MM.JJJJ> [Typencode z. B. 8*]
Set s = GetObject("SAPGUI").GetScriptingEngine.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Quit 2
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE38"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 1200
s.FindById("wnd[0]/usr/ctxtRS38M-PROGRAMM").Text = "ZPP_RM_MENGEN"
s.FindById("wnd[0]").SendVKey 8
WScript.Sleep 2000
ap = Split(WScript.Arguments(1), ",")
s.FindById("wnd[0]/usr/ctxtS_ARBPL-LOW").Text = ap(0)
If UBound(ap) > 0 Then s.FindById("wnd[0]/usr/ctxtS_ARBPL-HIGH").Text = ap(1)
s.FindById("wnd[0]/usr/ctxtP_WERKS").Text = "1100"
s.FindById("wnd[0]/usr/ctxtS_ERSDA-LOW").Text = WScript.Arguments(2)
s.FindById("wnd[0]/usr/ctxtS_ERSDA-HIGH").Text = ""
If WScript.Arguments.Count > 3 Then s.FindById("wnd[0]/usr/txtS_TYPCD-LOW").Text = WScript.Arguments(3)
s.FindById("wnd[0]").SendVKey 8
WScript.Sleep 4000
On Error Resume Next
Set g = s.FindById("wnd[0]/usr/cntlGRID1/shellcont/shell/shellcont[1]/shell")
If Err.Number <> 0 Then WScript.Echo "KEIN GRID: " & s.FindById("wnd[0]/sbar/pane[0]").Text : WScript.Quit 1
On Error GoTo 0
n = g.RowCount : sum = 0
Set per = CreateObject("Scripting.Dictionary")
For r = 0 To n - 1
  If r Mod 20 = 0 Then g.FirstVisibleRow = r
  If Trim(g.GetCellValue(r, "AUFNR")) = "" Then v = 0 Else v = CDbl(Replace(Replace(Replace(Trim(g.GetCellValue(r, "LMNGA")), "'", ""), ".", ""), ",", "."))
  a = g.GetCellValue(r, "ARBPL")
  sum = sum + v
  per(a) = per(a) + v
Next
WScript.Echo "ZEILEN=" & n & " SUMME_LMNGA=" & sum
For Each k In per.Keys : WScript.Echo "  " & k & "=" & per(k) : Next
