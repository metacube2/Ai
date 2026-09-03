Option Explicit
' Traegt den Rumpf der Methode AT_RELEASE ein und sichert ihn.
'
' MIT METHOD/ENDMETHOD. Am 2026-09-03 empirisch geklaert: der Editorpuffer des Class
' Builders enthaelt diese beiden Zeilen, und `SelectAll` plus `ReplaceSelection` ersetzt
' den gesamten Puffer. Ein Einfuegen nur des Rumpfes loescht sie und fuehrt zu vier
' Fehlern der Art "Zwischen CLASS ... IMPLEMENTATION und ENDCLASS duerfen nur Methoden
' definiert werden". Gleiches gilt in SE37 fuer FUNCTION/ENDFUNCTION.

Dim a, app, c, s, i, j, found, ed, src

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = "SE19" And s.Info.SystemName = "T76" And s.Info.Client = "100" Then
      found = True
      Exit For
    End If
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2

' Harte Absicherung: nur schreiben, wenn wirklich AT_RELEASE dieser Klasse offen ist.
If s.FindById("wnd[0]/usr/txtDY0200_CPDNAME").Text <> "IF_EX_WORKORDER_UPDATE~AT_RELEASE" Then WScript.Quit 5
If InStr(1, s.FindById("wnd[0]").Text, "ZCL_IM__ZZPRDAT_AT_RELEASE", vbTextCompare) = 0 Then WScript.Quit 6

src = ""
src = src & "METHOD if_ex_workorder_update~at_release." & vbCrLf
src = src & "* Prototyp T76/100, Paket $TMP. Setzt das Produktionsdatum einmalig bei der" & vbCrLf
src = src & "* erstmaligen Freigabe. Die Write-once-Regel selbst steht im Update-Baustein," & vbCrLf
src = src & "* damit sie auch bei mehrfacher Registrierung greift." & vbCrLf
src = src & vbCrLf
src = src & "  CHECK is_header_dialog-autyp = '10'." & vbCrLf
src = src & "  CHECK is_header_dialog-aufnr IS NOT INITIAL." & vbCrLf
src = src & "  CHECK is_header_dialog-gltrp IS NOT INITIAL." & vbCrLf
src = src & vbCrLf
src = src & "  CALL FUNCTION 'Z_PP_PRDDAT_SET' IN UPDATE TASK" & vbCrLf
src = src & "    EXPORTING" & vbCrLf
src = src & "      iv_aufnr  = is_header_dialog-aufnr" & vbCrLf
src = src & "      iv_prddat = is_header_dialog-gltrp." & vbCrLf
src = src & "ENDMETHOD." & vbCrLf

Set ed = s.FindById("wnd[0]/usr/subEDITORSUBSCREEN:SAPLEDITOR_START:8430/cntlEDITOR/shellcont/shell")
ed.SelectAll
ed.ReplaceSelection src
WScript.Sleep 500

' Sichern
s.FindById("wnd[0]/tbar[0]/btn[11]").Press
WScript.Sleep 1500

WScript.Echo "STATUS_TYP=" & s.FindById("wnd[0]/sbar").MessageType
WScript.Echo "STATUS=" & s.FindById("wnd[0]/sbar/pane[0]").Text
On Error Resume Next
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
WScript.Quit 0
