*&---------------------------------------------------------------------*
*& Report Z_MARA_DELTA_AUSN
*&---------------------------------------------------------------------*
*& Befuellt die Ausnahmetabelle ZLO_URSPR_AUSN monatlich automatisch
*& mit dem Delta aus MARA. Laeuft als Hintergrundjob (z.B. 1. des
*& Monats, 01:00). Der User muss neue Materialien damit NICHT mehr
*& manuell via SM30 nachpflegen.
*&
*& Delta-Logik:
*&   - Selektiert MARA-Materialien, deren ERSDA (angelegt am) in den
*&     letzten p_tage Tagen liegt (Default 31).
*&   - Stichtag = sy-datum - p_tage. Selektion: ERSDA >= Stichtag.
*&   - Werk kommt aus Parameter p_werks (in MARA nicht enthalten).
*&
*& Idempotenz:
*&   - Materialien, die fuer das Werk bereits in ZLO_URSPR_AUSN stehen,
*&     werden NICHT erneut angelegt (Delta-Modus, wie im Upload-Report).
*&   - Lauf kann gefahrlos wiederholt werden.
*&   - Bei monatlichem Job mit 31 Tagen ergibt sich eine kleine
*&     Ueberlappung -> ausgefallene Tage werden im Folgelauf nachgeholt,
*&     Dubletten faengt der Delta-Abgleich ab.
*&   - p_test = Testlauf ohne DB-Update.
*&
*& Tabellenstruktur ZLO_URSPR_AUSN:
*&   MANDT / MATNR / WERKS (Schluessel), GRUND, ERDAT, ERNAM
*&
*& System: S/4HANA, S4CORE 108, SAP_BASIS 758, ABAP 7.40+
*&---------------------------------------------------------------------*
report z_mara_delta_ausn.

tables: zlo_urspr_ausn.

include <icon>.

*-Selektionsbild-------------------------------------------------------*
selection-screen begin of block b1 with frame title text-t01.
parameters: p_werks type marc-werks obligatory memory id wrk,
            p_tage  type i default 31.       "ERSDA: letzte N Tage
select-options: s_ernam for ('ERNAM'),       "Angelegt von (optional)
                s_mtart for ('MTART'),       "Materialart  (optional)
                s_matnr for ('MATNR').       "Material     (optional)
selection-screen end of block b1.

selection-screen begin of block b2 with frame title text-t02.
parameters: p_grund type char40 default 'Auto-Delta MARA',
            p_nochk as checkbox,             "MARC-Pruefung ueberspringen
            p_test  as checkbox default 'X'. "Testlauf (kein DB-Update)
selection-screen end of block b2.

*-Datentypen-----------------------------------------------------------*
data: it_mara type standard table of mara,
      wa_mara type mara.

data: it_ausn type standard table of zlo_urspr_ausn,
      wa_ausn type zlo_urspr_ausn.

" Bereits vorhandene Materialien des Werks (Delta-Abgleich)
data: it_exist type sorted table of zlo_urspr_ausn
                with unique key matnr.

data: w_stichtag type sy-datum,   "ab diesem ERSDA wird selektiert
      w_read     type i,    "aus MARA gelesen
      w_ok       type i,    "neu aufgenommen
      w_exist    type i,    "schon vorhanden (uebersprungen)
      w_skip     type i.    "nicht in MARC (uebersprungen)

*----------------------------------------------------------------------*
* Verarbeitung
*----------------------------------------------------------------------*
start-of-selection.

  " Stichtag berechnen: heute minus p_tage
  w_stichtag = sy-datum - p_tage.

  perform delta_selektieren.
  perform daten_aufbereiten.
  perform daten_schreiben.
  perform protokoll_ausgeben.

*&---------------------------------------------------------------------*
*& Form delta_selektieren
*&  Holt das MARA-Delta: ERSDA >= Stichtag (letzte p_tage Tage).
*&---------------------------------------------------------------------*
form delta_selektieren.

  select * from mara
    into table @it_mara
    where matnr in @s_matnr
      and mtart in @s_mtart
      and ernam in @s_ernam
      and ersda ge @w_stichtag.

  describe table it_mara lines w_read.

  if it_mara is initial.
    write: / icon_information as icon,
             'Kein MARA-Delta im Selektionszeitraum gefunden.'.
  endif.

endform.

*&---------------------------------------------------------------------*
*& Form daten_aufbereiten
*&  Baut neue ZLO_URSPR_AUSN-Saetze fuer Materialien, die fuer das
*&  Werk noch nicht existieren.
*&---------------------------------------------------------------------*
form daten_aufbereiten.

  " Bereits vorhandene Materialien des Werks laden (Delta-Abgleich)
  select matnr from zlo_urspr_ausn
         into corresponding fields of table @it_exist
         where werks = @p_werks.

  loop at it_mara into wa_mara.

    " Schon vorhanden (in DB oder bereits in diesem Lauf erfasst)?
    " -> nicht erneut anlegen. Faengt DB-Saetze UND Dubletten ab.
    read table it_exist transporting no fields
         with key matnr = wa_mara-matnr.
    if sy-subrc = 0.
      w_exist = w_exist + 1.
      continue.
    endif.

    " Existenzpruefung Material im Werk (per p_nochk ueberspringbar)
    if p_nochk is initial.
      select single matnr from marc into @data(l_check)
             where matnr = @wa_mara-matnr
               and werks = @p_werks.
      if sy-subrc <> 0.
        w_skip = w_skip + 1.
        write: / icon_yellow_light as icon,
                 'Material/Werk nicht in MARC:', wa_mara-matnr, p_werks.
        continue.
      endif.
    endif.

    " Tabellensatz aufbauen
    clear wa_ausn.
    wa_ausn-mandt = sy-mandt.
    wa_ausn-matnr = wa_mara-matnr.
    wa_ausn-werks = p_werks.
    wa_ausn-grund = p_grund.
    wa_ausn-erdat = sy-datum.       "Ladedatum
    wa_ausn-ernam = sy-uname.       "Job-/Sachbearbeiter-User

    append wa_ausn to it_ausn.

    " Sofort in Vorhanden-Tabelle aufnehmen -> Lauf-Dubletten weg
    insert wa_ausn into table it_exist.

  endloop.

endform.

*&---------------------------------------------------------------------*
*& Form daten_schreiben
*&  INSERT: vorhandene Saetze wurden bereits aussortiert, es werden
*&  nur echte Neuzugaenge geschrieben.
*&---------------------------------------------------------------------*
form daten_schreiben.

  describe table it_ausn lines w_ok.

  if p_test = 'X'.
    write: / icon_information as icon,
             'TESTLAUF - es wird nichts auf die Datenbank geschrieben.'.
    return.
  endif.

  if it_ausn is not initial.
    insert zlo_urspr_ausn from table it_ausn accepting duplicate keys.
    if sy-subrc = 0.
      commit work.
      write: / icon_okay as icon,
               'DB-Update erfolgreich. Neu eingefuegt:', w_ok.
    else.
      rollback work.
      write: / icon_failure as icon,
               'Fehler beim DB-Update - Rollback durchgefuehrt.'.
    endif.
  else.
    write: / icon_information as icon,
             'Keine neuen Saetze zum Einfuegen.'.
  endif.

endform.

*&---------------------------------------------------------------------*
*& Form protokoll_ausgeben
*&---------------------------------------------------------------------*
form protokoll_ausgeben.

  skip 1.
  uline.
  write: / 'Verarbeitungsprotokoll Z_MARA_DELTA_AUSN'.
  uline.
  write: / 'Werk                            :', p_werks.
  write: / 'ERSDA der letzten Tage          :', (4) p_tage.
  write: / 'Stichtag (ERSDA >=)             :', w_stichtag.
  write: / 'Aus MARA gelesen                :', (8) w_read.
  write: / 'Neu aufgenommen                 :', (8) w_ok.
  write: / 'Bereits vorhanden (uebersprungen):', (8) w_exist.
  write: / 'Nicht in MARC (uebersprungen)   :', (8) w_skip.
  if p_test = 'X'.
    write: / 'Modus                           : TESTLAUF'.
  else.
    write: / 'Modus                           : DELTA (nur Neuzugaenge)'.
  endif.
  uline.

endform.
