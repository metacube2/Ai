
*----------------------------------------------------------------------*
* FLUSS_BEISPIEL: beide Zeilenlisten einer abweichenden Position als Text
* (nur Pruefmodus, hoechstens einige Positionen je Paket)
*----------------------------------------------------------------------*
FORM fluss_beispiel USING iv_vbeln TYPE vbeln
                          iv_posnr TYPE posnr
                          it_orig  TYPE STANDARD TABLE
                          it_kurz  TYPE STANDARD TABLE
                    CHANGING ct_text TYPE string_table.
  DATA: ls_v   TYPE vbfa,
        lv_txt TYPE string.
  APPEND |POSITION { iv_vbeln } { iv_posnr } ORIGINAL { lines( it_orig ) } KURZ { lines( it_kurz ) }| TO ct_text.
  LOOP AT it_orig INTO ls_v.
    lv_txt = |  O { ls_v-stufe } { ls_v-vbelv } { ls_v-posnv } { ls_v-vbtyp_v } -> { ls_v-vbeln } { ls_v-posnn } { ls_v-vbtyp_n } { ls_v-erdat } { ls_v-rfmng }|.
    APPEND lv_txt TO ct_text.
  ENDLOOP.
  LOOP AT it_kurz INTO ls_v.
    lv_txt = |  K { ls_v-vbelv } { ls_v-posnv } { ls_v-vbtyp_v } -> { ls_v-vbeln } { ls_v-posnn } { ls_v-vbtyp_n } { ls_v-erdat } { ls_v-rfmng }|.
    APPEND lv_txt TO ct_text.
  ENDLOOP.
ENDFORM.
