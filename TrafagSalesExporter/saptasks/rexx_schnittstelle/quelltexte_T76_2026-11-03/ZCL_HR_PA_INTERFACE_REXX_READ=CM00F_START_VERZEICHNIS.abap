METHOD start_verzeichnis.

    "Helpfields
    DATA: lv_dirname TYPE dirname.

    CLEAR cv_dir.
    lv_dirname = iv_dir.

    SELECT        dirname
           FROM   user_dir
           UP TO  1 ROWS
           INTO   lv_dirname
           WHERE  aliass  = iv_dir.

    ENDSELECT.

    CHECK sy-subrc IS INITIAL.

    cv_dir = lv_dirname.

  ENDMETHOD.
