FUNCTION-POOL ZZLO1.                        "MESSAGE-ID ..

tables: mara, knvv, adrc,
        ztyp_kopf, ztyp_kopf_t,
        ztyp_pos, ztyp_pos_t,
        ztyp_zub.
tables: a502, a503, a504, konp.

data: laenge type p,
      laenge2 type p,
      fak_laenge type p,
      fak_laenge2 type p,
      zwxkey like ztyp_pos-code,
      xcode like ztyp_zub-code,
      mit_loe(1).

data: w_matnr(18),
      w_search(60),
      w_bismt like mara-bismt,
      w_maktx like makt-maktx,
      w_langu like sy-langu,
      w_max type p,
      w_werks like marc-werks,
      w_vkorg like mvke-vkorg,
      w_vtweg like mvke-vtweg,
      w_kschl(4),
      w_kunnr like kna1-kunnr,
      w_spart like knvv-spart,
      w_pltyp like knvv-pltyp,
      w_waers like knvv-waers,
      w_aufsd like kna1-aufsd,
      w_adrnr like adr6-addrnumber,
      w_vmsta like mara-mstav,
      w_status,
      w_value(8).

data: zwbptab type zzbptab occurs 0 with header line.
data: zwmttab type zzmttab occurs 0 with header line.
data: zwmttab2 type zzmttab2 occurs 0 with header line.
data: zwmttab3 type zzmttab3 occurs 0 with header line.
data: zwmttab4 type zzmttab4 occurs 0 with header line.
data: zwvltab type zzvltab occurs 0 with header line.
data: zwadrtab type zzadrtab occurs 0 with header line.
data: iknvp type knvp occurs 0 with header line.

data: sditem type bapiitemin occurs 0 with header line.
data: sditemex type bapiitemex occurs 0 with header line.
data: sdpart type bapipartnr occurs 0 with header line.
data: sdcond type bapicond occurs 0 with header line.
data: sdmess type bapiret2 occurs 0 with header line.
data: ret1 type bapireturn1 occurs 0 with header line.

data: sdhead type bapisdhead.

data: begin of key,
        zwtyp(4),
        schalter(2),
        bereich(2),
        fuehler(4),
        kdwu(1),
        befest(2),
        sror(8),
      end of key.

data: begin of zbtab occurs 0,
        zub(2),
      end of zbtab.

data: zwspras like sy-langu.

DATA:   BDCDATA LIKE BDCDATA    OCCURS 0 WITH HEADER LINE.
