Option Explicit
' Lesender Integrationstest in T76/100. Erwartet das Selektionsbild von
' Z_ZZPRDAT_CHECK in SE38. Veraendert nur Selektionsfelder, keine Auftraege.
Dim gui, app, conn, ses, target, i, j, ausgabe, fehler
Set gui = GetObject("SAPGUI")
Set app = gui.GetScriptingEngine
Set target = Nothing
For i = 0 To app.Children.Count - 1
  Set conn = app.Children(CLng(i))
  For j = 0 To conn.Children.Count - 1
    Set ses = conn.Children(CLng(j))
    If ses.Info.SystemName = "T76" And ses.Info.Client = "100" And ses.Info.Transaction = "SE38" Then
      If InStr(ses.FindById("wnd[0]").Text, "ZZPRDAT") > 0 Then Set target = ses
    End If
  Next
Next
If target Is Nothing Then WScript.Quit 2
target.FindById("wnd[0]/usr/ctxtS_AUFNR-LOW").Text = "1241817"
target.FindById("wnd[0]/usr/ctxtS_AUFNR-HIGH").Text = "1241830"
target.FindById("wnd[0]/usr/ctxtP_ERDAT").Text = "01.09.2026"
target.FindById("wnd[0]/usr/ctxtP_WERKS").Text = "1100"
target.FindById("wnd[0]/usr/txtP_MAX").Text = "1"
target.FindById("wnd[0]/usr/chkP_LEER").Selected = True
target.FindById("wnd[0]").SendVKey 8
WScript.Sleep 1200
ausgabe = ""
Sammeln target.FindById("wnd[0]/usr")
WScript.Echo ausgabe
If InStr(ausgabe, "1241819") = 0 Or InStr(ausgabe, "1241830") > 0 Then
  WScript.Echo "FEHLER: Leerfilter vor Trefferlimit nicht nachgewiesen"
  WScript.Quit 3
End If
If InStr(ausgabe, "Leer; Freigabe vorhanden") = 0 Then WScript.Quit 4
WScript.Echo "OK: Leerer Altauftrag trotz maximal einem Treffer gefunden"

target.FindById("wnd[0]").SendVKey 3
target.FindById("wnd[0]/usr/txtP_MAX").Text = "0"
target.FindById("wnd[0]").SendVKey 8
WScript.Sleep 500
fehler = target.FindById("wnd[0]/sbar").MessageType
WScript.Echo "MAX_0_STATUS=" & fehler
WScript.Echo target.FindById("wnd[0]/sbar/pane[0]").Text
If fehler <> "E" Then WScript.Quit 5
target.FindById("wnd[0]/usr/txtP_MAX").Text = "1"
target.FindById("wnd[0]").SendVKey 8
WScript.Echo "OK: Unbegrenzte Selektion durch p_max=0 verhindert"

Sub Sammeln(element)
  Dim k
  If element.Type = "GuiLabel" Then ausgabe = ausgabe & element.Text & vbCrLf
  If element.ContainerType Then
    For k = 0 To element.Children.Count - 1
      Sammeln element.Children(CLng(k))
    Next
  End If
End Sub
