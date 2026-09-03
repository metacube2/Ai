Option Explicit
Dim a, app, c, s, editor, i, j, found, source
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = "SE37" And s.Info.SystemName = "T76" And s.Info.Client = "100" Then found = True: Exit For
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2
If InStr(1, s.FindById("wnd[0]").Text, "Z_PP_PRDDAT_SET", vbTextCompare) = 0 Then WScript.Quit 4
source = "FUNCTION z_pp_prddat_set." & vbCrLf & _
         "  CHECK iv_aufnr IS NOT INITIAL." & vbCrLf & _
         "  CHECK iv_prddat IS NOT INITIAL." & vbCrLf & vbCrLf & _
         "  UPDATE aufk" & vbCrLf & _
         "    SET zzprdat = iv_prddat" & vbCrLf & _
         "    WHERE aufnr = iv_aufnr" & vbCrLf & _
         "      AND zzprdat = '00000000'." & vbCrLf & _
         "ENDFUNCTION."
Set editor = s.FindById("wnd[0]/usr/tabsFUNC_TAB_STRIP/tabpSOURCE/ssubSCREEN_HEADER:SAPLEDITOR_START:8430/cntlEDITOR/shellcont/shell")
editor.SelectAll
editor.ReplaceSelection source
s.FindById("wnd[0]/tbar[0]/btn[11]").Press
WScript.Sleep 700
WScript.Echo "STATUS=" & s.FindById("wnd[0]/sbar/pane[0]").Text
