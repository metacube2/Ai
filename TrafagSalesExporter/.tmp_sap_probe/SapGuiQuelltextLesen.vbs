' NUR LESEN (T76): Quelltext eines Programms oder Klassen-Includes per SE38 in eine UTF-8-Datei schreiben.
' Klassenmethoden als Include angeben, z. B. ZCL_ZPOWERBI_EINKAUF_DPC_EXT==CM01W (SE38 springt in den Class Builder).
' Ersatz fuer SapProbe abap-read, wenn kein Passwort hinterlegt ist. Angelegt 2026-10-16 (Shopfloor ZC27).
' Aufruf: SapGuiQuelltextLesen.vbs <Programm/Include> <Zieldatei> <SitzungsIndex in der T76-Verbindung>
Set app = GetObject("SAPGUI").GetScriptingEngine
For i = 0 To app.Children.Count - 1
  If app.Children(CLng(i)).Children.Count > 0 Then
    If app.Children(CLng(i)).Children(0).Info.SystemName = "T76" Then Set c = app.Children(CLng(i))
  End If
Next
Set s = c.Children(CLng(WScript.Arguments(2)))
If s.Info.SystemName <> "T76" Then WScript.Echo "ABBRUCH nicht T76" : WScript.Quit 2
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE38"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 1500
s.FindById("wnd[0]/usr/ctxtRS38M-PROGRAMM").Text = WScript.Arguments(0)
s.FindById("wnd[0]/usr/btnSHOP").Press
WScript.Sleep 2500
On Error Resume Next
Set ed = s.FindById("wnd[0]/usr/cntlEDITOR/shellcont/shell")
If Err.Number <> 0 Then Err.Clear : Set ed = s.FindById("wnd[0]/usr/subEDITORSUBSCREEN:SAPLEDITOR_START:8430/cntlEDITOR/shellcont/shell")
On Error GoTo 0
n = ed.GetLineCount
Set st = CreateObject("ADODB.Stream")
st.Type = 2 : st.Charset = "utf-8" : st.Open
For k = 1 To n
  st.WriteText ed.GetLineText(k) & vbLf
Next
st.SaveToFile WScript.Arguments(1), 2 : st.Close
WScript.Echo "ZEILEN=" & n & " TITEL=" & s.FindById("wnd[0]").Text
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE38"
s.FindById("wnd[0]").SendVKey 0
