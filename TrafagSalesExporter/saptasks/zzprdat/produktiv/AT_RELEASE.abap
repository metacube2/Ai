METHOD if_ex_workorder_update~at_release.
* Zielfassung vom 2026-09-03 fuer T76/100, Paket $TMP.
*
* Setzt das Produktionsdatum bei der Freigabe aus dem zu diesem Zeitpunkt
* gueltigen Eckendtermin. Die Write-once-Regel steht im Verbuchungsbaustein,
* damit sie auch bei mehrfacher Registrierung greift.
*
* Zwei Punkte sind teuer erarbeitet und duerfen nicht verlorengehen:
*
* 1. Die Werte werden ueber getypte Variablen uebergeben, nie als Literal.
*    Beim Registrieren serialisiert SAP die Parameter, beim Ausfuehren liest es
*    sie zurueck. Ein Zeichenliteral fuer ein DATS-Feld fuehrt dort zum Abbruch
*    CONNE_IMPORT_WRONG_FIELD_TYPE.
* 2. Der Baustein Z_ZZPRDAT_SET laeuft als V2, also "Start verzoegert".
*    Als V1 schrieb er zwar, die SAP-Standardverbuchung ueberschrieb die
*    AUFK-Zeile danach jedoch aus ihrem eigenen Puffer und deckte den Wert zu.
  DATA: lv_aufnr  TYPE aufnr,
        lv_prddat TYPE zco_gltrp.

  CHECK is_header_dialog-aufnr IS NOT INITIAL.
  CHECK is_header_dialog-gltrp IS NOT INITIAL.

  lv_aufnr  = is_header_dialog-aufnr.
  lv_prddat = is_header_dialog-gltrp.

  CALL FUNCTION 'Z_ZZPRDAT_SET' IN UPDATE TASK
    EXPORTING
      iv_aufnr  = lv_aufnr
      iv_prddat = lv_prddat.
ENDMETHOD.
