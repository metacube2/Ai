report z_koi_zzprdat_clr.
*&---------------------------------------------------------------------*
*& Wegwerfhilfe in $TMP. Setzt AUFK-ZZPRDAT auf einem der eigenen
*& Testauftraege zurueck, um den Zustand eines Altauftrags herzustellen:
*& freigegeben, Feld leer, Auftrag noch aenderbar.
*&
*& Gebraucht am 2026-09-07 fuer den Nachweis, dass die neue Freigabepruefung
*& in BEFORE_UPDATE greift. Die beiden echten Altauftraege im System
*& (9000005594, 9000005518) sind nicht mehr aenderbar und taugen deshalb nicht.
*&
*& Sicherung: Der Report akzeptiert ausschliesslich die Auftragsnummern der
*& eigenen Testreihe. Ein Tippfehler kann damit keinen fremden Auftrag treffen.
*&---------------------------------------------------------------------*

parameters: p_aufnr type aufnr obligatory default '000001241819'.

data: lv_ok      type c length 1,
      lv_zzprdat type aufk-zzprdat,
      lv_ftrmi   type afko-ftrmi.

start-of-selection.

  clear lv_ok.
  if p_aufnr between '000001241817' and '000001241823'.
    lv_ok = 'X'.
  endif.

  if lv_ok is initial.
    write: / 'ABGELEHNT: nur die eigene Testreihe 1241817 bis 1241823.'.
    write: / 'Angefragt war:', p_aufnr.
    return.
  endif.

  select single zzprdat from aufk into lv_zzprdat where aufnr = p_aufnr.
  if sy-subrc <> 0.
    write: / 'Auftrag nicht gefunden:', p_aufnr.
    return.
  endif.
  select single ftrmi from afko into lv_ftrmi where aufnr = p_aufnr.

  write: / 'Auftrag        :', p_aufnr.
  write: / 'ZZPRDAT vorher :', lv_zzprdat.
  write: / 'FTRMI          :', lv_ftrmi.

  update aufk set zzprdat = '00000000' where aufnr = p_aufnr.
  if sy-subrc = 0.
    commit work.
    select single zzprdat from aufk into lv_zzprdat where aufnr = p_aufnr.
    write: / 'ZZPRDAT nachher:', lv_zzprdat.
    write: / 'OK: Altauftragszustand hergestellt.'.
  else.
    write: / 'FEHLER: UPDATE subrc', sy-subrc.
  endif.
