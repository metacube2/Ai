Option Explicit
'=======================================================================
' SapGuiRunFlow.vbs — fuehrt einen ganzen SAP-Vorgang in einem Aufruf aus.
'
' Statt je Knopfdruck einen eigenen Prozess zu starten, laeuft hier der
' komplette Ablauf durch und beantwortet die Zwischendialoge selbst.
'
' Aufrufe:
'   SapGuiRunFlow.vbs co01 <matnr> <werks> <auart> <menge> <endtermin> <ja|nein>
'       Legt einen Fertigungsauftrag an. Letztes Argument steuert die Freigabe.
'   SapGuiRunFlow.vbs co02release <aufnr>
'       Gibt einen bestehenden Auftrag frei und sichert ihn.
'   SapGuiRunFlow.vbs status <aufnr>
'       Liest nur den Auftragsstatus in CO03, ohne etwas zu aendern.
'
' Sicherheitsregel: Ein **unbekannter** Dialog wird nicht blind weggeklickt.
' Der Ablauf bricht ab, meldet Titel und Schaltflaechen und ueberlaesst die
' Entscheidung dem Menschen. In einem gemeinsam genutzten Testsystem ist
' Blindklicken die gefaehrlichste Abkuerzung, die es gibt.
'=======================================================================

Dim gFreigabeModus   ' "ja" oder "nein" — steuert die Antwort im Dialog "Auftrag freigeben"
Dim gSession

gFreigabeModus = "ja"

'-----------------------------------------------------------------------
' Sitzung mit der gewuenschten Transaktion holen, notfalls neu oeffnen.
'-----------------------------------------------------------------------
Function SitzungHolen(tx)
  Dim a, app, c, s, i, j, ersteSitzung
  Set a = GetObject("SAPGUI")
  Set app = a.GetScriptingEngine
  Set ersteSitzung = Nothing

  For i = 0 To app.Children.Count - 1
    Set c = app.Children(CLng(i))
    For j = 0 To c.Children.Count - 1
      Set s = c.Children(CLng(j))
      If s.Info.SystemName = "T76" And s.Info.Client = "100" Then
        If ersteSitzung Is Nothing Then Set ersteSitzung = s
        If UCase(s.Info.Transaction) = UCase(tx) Then
          Set SitzungHolen = s
          Exit Function
        End If
      End If
    Next
  Next

  If ersteSitzung Is Nothing Then
    WScript.Echo "FEHLER: keine angemeldete T76/100-Sitzung gefunden."
    WScript.Quit 2
  End If

  ' Neue Sitzung mit der gewuenschten Transaktion oeffnen.
  ersteSitzung.FindById("wnd[0]/tbar[0]/okcd").Text = "/o" & tx
  ersteSitzung.FindById("wnd[0]").SendVKey 0
  WScript.Sleep 2500

  For i = 0 To app.Children.Count - 1
    Set c = app.Children(CLng(i))
    For j = 0 To c.Children.Count - 1
      Set s = c.Children(CLng(j))
      If s.Info.SystemName = "T76" And s.Info.Client = "100" _
         And UCase(s.Info.Transaction) = UCase(tx) Then
        Set SitzungHolen = s
        Exit Function
      End If
    Next
  Next

  WScript.Echo "FEHLER: Transaktion " & tx & " liess sich nicht oeffnen."
  WScript.Quit 3
End Function

'-----------------------------------------------------------------------
' Regeltabelle: Dialogtitel -> zu drueckende Schaltflaeche.
' Gibt "" zurueck, wenn der Dialog unbekannt ist.
'-----------------------------------------------------------------------
Function DialogRegel(titel)
  Dim t
  t = UCase(titel)
  DialogRegel = ""

  If InStr(t, "MATERIALSTATUSPR") > 0 Then
    ' "Komponente trotzdem uebernehmen?" -> Ja
    DialogRegel = "wnd[1]/usr/btnSPOP-VAROPTION1"
  ElseIf InStr(t, "AUFTRAG FREIGEBEN") > 0 Then
    If gFreigabeModus = "nein" Then
      ' Abbrechen lehnt nur die Freigabe ab, der Auftrag wird trotzdem gesichert.
      DialogRegel = "wnd[1]/usr/btnCANCEL"
    Else
      DialogRegel = "wnd[1]/usr/btnSPOP-VAROPTION1"
    End If
  ElseIf InStr(t, "STATUSVERARBEITUNG") > 0 Then
    ' "Weiter"
    DialogRegel = "wnd[1]/usr/btnOPTION2"
  ElseIf t = "INFORMATION" Then
    DialogRegel = "wnd[1]/tbar[0]/btn[0]"
  End If
End Function

'-----------------------------------------------------------------------
' Alle offenen Dialoge abarbeiten, bis keiner mehr da ist.
'-----------------------------------------------------------------------
Sub DialogeAbarbeiten(s)
  Dim runde, titel, ziel, k, knopfliste

  For runde = 1 To 20
    titel = ""
    On Error Resume Next
    titel = s.FindById("wnd[1]").Text
    On Error GoTo 0
    If titel = "" Then Exit Sub

    ziel = DialogRegel(titel)

    If ziel = "" Then
      WScript.Echo "ABBRUCH: unbekannter Dialog '" & titel & "'."
      knopfliste = ""
      On Error Resume Next
      For k = 0 To s.FindById("wnd[1]/tbar[0]").Children.Count - 1
        knopfliste = knopfliste & vbCrLf & "  wnd[1]/tbar[0]/" & _
                     s.FindById("wnd[1]/tbar[0]").Children(CLng(k)).Name & " Tooltip=" & _
                     s.FindById("wnd[1]/tbar[0]").Children(CLng(k)).Tooltip
      Next
      For k = 0 To s.FindById("wnd[1]/usr").Children.Count - 1
        If InStr(1, s.FindById("wnd[1]/usr").Children(CLng(k)).Type, "Button", vbTextCompare) > 0 Then
          knopfliste = knopfliste & vbCrLf & "  wnd[1]/usr/" & _
                       s.FindById("wnd[1]/usr").Children(CLng(k)).Name & " Text=" & _
                       s.FindById("wnd[1]/usr").Children(CLng(k)).Text
        End If
      Next
      On Error GoTo 0
      WScript.Echo "Schaltflaechen:" & knopfliste
      WScript.Echo "Der Ablauf klickt bewusst nicht blind weiter."
      WScript.Quit 9
    End If

    WScript.Echo "  Dialog '" & titel & "' -> " & ziel
    On Error Resume Next
    s.FindById(ziel).Press
    If Err.Number <> 0 Then
      WScript.Echo "  Schaltflaeche nicht gefunden, Maske hat sich geaendert. Neu bewerten."
      Err.Clear
      On Error GoTo 0
      Exit Sub
    End If
    On Error GoTo 0
    WScript.Sleep 1800
  Next

  WScript.Echo "ABBRUCH: mehr als 20 Dialoge hintereinander, vermutlich eine Schleife."
  WScript.Quit 10
End Sub

Sub Melden(s, was)
  WScript.Echo was & ": FENSTER=" & s.FindById("wnd[0]").Text & _
               " | TYP=" & s.FindById("wnd[0]/sbar").MessageType & _
               " | MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
End Sub

'=======================================================================
' Ablaeufe
'=======================================================================

Sub FlowCo01(matnr, werks, auart, menge, endtermin, freigabe)
  Dim s, basis, nummer, meldung
  gFreigabeModus = LCase(freigabe)

  Set s = SitzungHolen("CO01")

  If InStr(1, s.FindById("wnd[0]").Text, "Einstieg", vbTextCompare) = 0 Then
    ' Aus einem halb ausgefuellten Auftrag zurueck auf das Einstiegsbild.
    s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nCO01"
    s.FindById("wnd[0]").SendVKey 0
    WScript.Sleep 2000
    Set s = SitzungHolen("CO01")
  End If

  s.FindById("wnd[0]/usr/ctxtCAUFVD-MATNR").Text = matnr
  s.FindById("wnd[0]/usr/ctxtCAUFVD-WERKS").Text = werks
  s.FindById("wnd[0]/usr/ctxtAUFPAR-PP_AUFART").Text = auart
  s.FindById("wnd[0]").SendVKey 0
  WScript.Sleep 2500
  DialogeAbarbeiten s
  Melden s, "Kopf geoeffnet"

  basis = "wnd[0]/usr/tabsTABSTRIP_0115/tabpKOZE/ssubSUBSCR_0115:SAPLCOKO1:0120"
  On Error Resume Next
  s.FindById(basis & "/txtCAUFVD-GAMNG").Text = menge
  s.FindById(basis & "/ctxtCAUFVD-GLTRP").Text = endtermin
  On Error GoTo 0
  s.FindById("wnd[0]").SendVKey 0
  WScript.Sleep 2500
  DialogeAbarbeiten s

  If gFreigabeModus = "ja" Then
    WScript.Echo "Freigabe anstossen (Strg+F1)"
    s.FindById("wnd[0]/tbar[1]/btn[25]").Press
    WScript.Sleep 2000
    DialogeAbarbeiten s
    On Error Resume Next
    WScript.Echo "Status im Kopf: " & s.FindById("wnd[0]/usr/txtCAUFVD-STTXT").Text
    On Error GoTo 0
  End If

  WScript.Echo "Sichern"
  s.FindById("wnd[0]/tbar[0]/btn[11]").Press
  WScript.Sleep 2500
  DialogeAbarbeiten s

  meldung = s.FindById("wnd[0]/sbar/pane[0]").Text
  Melden s, "Nach dem Sichern"

  ' Auftragsnummer aus "Auftrag wurde mit der Nummer 1241802 gesichert" ziehen.
  nummer = ""
  If InStr(meldung, "Nummer") > 0 Then
    nummer = Trim(Mid(meldung, InStr(meldung, "Nummer") + 7))
    If InStr(nummer, " ") > 0 Then nummer = Left(nummer, InStr(nummer, " ") - 1)
  End If
  WScript.Echo "AUFTRAGSNUMMER=" & nummer
End Sub

Sub FlowCo02Release(aufnr)
  Dim s
  gFreigabeModus = "ja"
  Set s = SitzungHolen("CO02")

  If InStr(1, s.FindById("wnd[0]").Text, "Einstieg", vbTextCompare) = 0 Then
    s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nCO02"
    s.FindById("wnd[0]").SendVKey 0
    WScript.Sleep 2000
    Set s = SitzungHolen("CO02")
  End If

  s.FindById("wnd[0]/usr/ctxtCAUFVD-AUFNR").Text = aufnr
  s.FindById("wnd[0]").SendVKey 0
  WScript.Sleep 2500
  DialogeAbarbeiten s
  Melden s, "Auftrag geoeffnet"

  s.FindById("wnd[0]/tbar[1]/btn[25]").Press
  WScript.Sleep 2000
  DialogeAbarbeiten s
  On Error Resume Next
  WScript.Echo "Status im Kopf: " & s.FindById("wnd[0]/usr/txtCAUFVD-STTXT").Text
  On Error GoTo 0

  s.FindById("wnd[0]/tbar[0]/btn[11]").Press
  WScript.Sleep 2500
  DialogeAbarbeiten s
  Melden s, "Nach dem Sichern"
End Sub

Sub FlowStatus(aufnr)
  Dim s
  Set s = SitzungHolen("CO03")
  If InStr(1, s.FindById("wnd[0]").Text, "Einstieg", vbTextCompare) = 0 Then
    s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nCO03"
    s.FindById("wnd[0]").SendVKey 0
    WScript.Sleep 2000
    Set s = SitzungHolen("CO03")
  End If
  s.FindById("wnd[0]/usr/ctxtCAUFVD-AUFNR").Text = aufnr
  s.FindById("wnd[0]").SendVKey 0
  WScript.Sleep 2500
  DialogeAbarbeiten s
  On Error Resume Next
  WScript.Echo "STATUS=" & s.FindById("wnd[0]/usr/txtCAUFVD-STTXT").Text
  On Error GoTo 0
End Sub

'=======================================================================
' Einstieg
'=======================================================================

Dim flow
If WScript.Arguments.Count = 0 Then
  WScript.Echo "Aufruf: SapGuiRunFlow.vbs co01|co02release|status ..."
  WScript.Quit 1
End If

flow = LCase(WScript.Arguments(0))

Select Case flow
  Case "co01"
    FlowCo01 WScript.Arguments(1), WScript.Arguments(2), WScript.Arguments(3), _
             WScript.Arguments(4), WScript.Arguments(5), WScript.Arguments(6)
  Case "co02release"
    FlowCo02Release WScript.Arguments(1)
  Case "status"
    FlowStatus WScript.Arguments(1)
  Case Else
    WScript.Echo "Unbekannter Ablauf: " & flow
    WScript.Quit 1
End Select

WScript.Quit 0
