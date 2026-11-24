*----------------------------------------------------------------------*
***INCLUDE LZZLO2F01 .
*----------------------------------------------------------------------*

*&---------------------------------------------------------------------*
*&      Form  TRANSAKTION_MM02
*&---------------------------------------------------------------------*
form transaktion_mm02 .

  loop at imape.

    w_mape = imape.

    at first.
      move: 'MM02' to i130m-tcode.
      move 'V' to i130m-pstat.
      write w_mape-matnr to w_matnr.

      refresh ybdcdata.
      call function 'MATERIAL_BTCI_SELECTION_NEW'
        exporting
          material   = w_matnr
          selection  = i130m-pstat
          tcode      = i130m-tcode
        tables
          btci_d0070 = ybdcdata
        exceptions
          others     = 1.
      if ybdcdata[] is initial.
        move 'E' to i130m-pstat.
        call function 'MATERIAL_BTCI_SELECTION_NEW'
          exporting
            material   = w_matnr
            selection  = i130m-pstat
            tcode      = i130m-tcode
          tables
            btci_d0070 = ybdcdata
          exceptions
            others     = 1.
      endif.

      perform bdc_dynpro      using 'SAPLMGMM' '0060'.
      perform bdc_field       using 'BDC_OKCODE' 'AUSW'.
      perform bdc_field       using 'RMMG1-MATNR' w_matnr.
      loop at ybdcdata from 1 to 15.
        bdcdata = ybdcdata.
        append bdcdata.
      endloop.

      perform bdc_dynpro      using 'SAPLMGMM' '0080'.
      perform bdc_field       using 'RMMG1-WERKS' w_mape-werks.

      if i130m-pstat = 'V'.
        perform bdc_dynpro      using 'SAPLMGMM' '4000'.
        perform bdc_dynpro      using 'SAPLMGMM' '4000'.
        perform bdc_dynpro      using 'SAPLMGMM' '4000'.
        perform bdc_dynpro      using 'SAPLMGMM' '4004'.
      else.
        perform bdc_dynpro      using 'SAPLMGMM' '4000'.
        perform bdc_dynpro      using 'SAPLMGMM' '4000'.
      endif.
      perform bdc_field       using 'BDC_OKCODE' '=PB18'.

      perform bdc_dynpro      using 'SAPLMAPE' '0100'.
    endat.

    read table i604g with key gzolx = w_mape-gzolx.
    if sy-subrc = 0.
      w_tabix = sy-tabix.
      if not w_mape-prefe is initial.
        w_feldname = 'MAPE-PREFE(..)'.
        unpack w_tabix to w_feldname+11(2).
        perform bdc_field       using w_feldname w_mape-prefe.

        if not w_mape-preda is initial.
          write w_mape-preda to w_datum dd/mm/yy.
        else.
          write sy-datum to w_datum dd/mm/yy.
        endif.
        w_feldname = 'MAPE-PREDA(..)'.
        unpack w_tabix to w_feldname+11(2).
        perform bdc_field       using w_feldname w_datum.
      endif.

      if not w_mape-prene is initial.
        w_feldname = 'MAPE-PRENE(..)'.
        unpack w_tabix to w_feldname+11(2).
        perform bdc_field       using w_feldname w_mape-prene.

        if not w_mape-preng is initial.
          write w_mape-preng to w_datum dd/mm/yy.
        else.
          write sy-datum to w_datum dd/mm/yy.
        endif.
        w_feldname = 'MAPE-PRENG(..)'.
        unpack w_tabix to w_feldname+11(2).
        perform bdc_field       using w_feldname w_datum.

      endif.
    endif.

    at last.
      if i130m-pstat = 'V'.
        perform bdc_dynpro      using 'SAPLMGMM' '4004'.
      else.
        perform bdc_dynpro      using 'SAPLMGMM' '4000'.
      endif.
      perform bdc_field       using 'BDC_OKCODE' '=BU'.

      perform bdc_transaction using 'MM02'.
    endat.

  endloop.
endform.                    " TRANSAKTION_MM02

*----------------------------------------------------------------------*
*   create batchinput session                                          *
*----------------------------------------------------------------------*
form open_group.
  check w_open is initial.
  w_open = 'X'.
  call function 'BDC_OPEN_GROUP'
    exporting
      client = sy-mandt
      group  = 'ZOLL-PRAEF'
      user   = sy-uname.
endform.                    "OPEN_GROUP

*----------------------------------------------------------------------*
*   end batchinput session                                             *
*----------------------------------------------------------------------*
form close_group.
  check w_open = 'X'.
  call function 'BDC_CLOSE_GROUP'.
endform.                    "CLOSE_GROUP

*----------------------------------------------------------------------*
*        Start new transaction                                         *
*----------------------------------------------------------------------*
form bdc_transaction using tcode.
  data: l_mode.

  l_mode = 'N'.
  refresh messtab.
  call transaction tcode using bdcdata
                   mode   l_mode
                   update 'S'
                   messages into messtab.

  w_error = 'X'.
  loop at messtab.
    if ( messtab-msgnr = '801' or messtab-msgnr = '810' ) and
      messtab-msgtyp = 'S'.
      clear w_error.
    endif.
  endloop.

  if w_error = 'X'.
    perform open_group.
    call function 'BDC_INSERT'
      exporting
        tcode     = tcode
      tables
        dynprotab = bdcdata.
  endif.
  refresh bdcdata.
endform.                    "bdc_transaction

*----------------------------------------------------------------------*
*        Start new screen                                              *
*----------------------------------------------------------------------*
form bdc_dynpro using program dynpro.
  clear bdcdata.
  bdcdata-program  = program.
  bdcdata-dynpro   = dynpro.
  bdcdata-dynbegin = 'X'.
  append bdcdata.
endform.                    "bdc_dynpro

*----------------------------------------------------------------------*
*        Insert field                                                  *
*----------------------------------------------------------------------*
form bdc_field using fnam fval.
  clear bdcdata.
  bdcdata-fnam = fnam.
  bdcdata-fval = fval.
  append bdcdata.
endform.                    "bdc_field
