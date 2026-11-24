function-pool zzlo2.                        "MESSAGE-ID ..

data: w_matnr like mara-matnr,
      w_prefe like mape-prefe,
      w_feldname(15),
      w_tabix like sy-tabix,
      w_error(1),
      w_open(1),
      w_datum(10).

data: imape like mape occurs 0 with header line.
data: bdcdata like bdcdata occurs 0 with header line.
data: ybdcdata like bdcdata occurs 0 with header line.
data: messtab like bdcmsgcoll occurs 0 with header line.
data: i604g like t604g occurs 0 with header line.
data: i130m like t130m.
data: w_mape like mape.
