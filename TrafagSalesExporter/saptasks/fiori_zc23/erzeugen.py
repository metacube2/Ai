"""Erzeugt den MPC_EXT-DEFINE-Zusatz fuer Zc23Set aus der Feldliste von ZMM_UEB_KDAUF (DD03L, T76).

Aufruf (im Ordner saptasks/fiori_zc23):  python -X utf8 erzeugen.py
Ausgabe: ZC23_MPC_DEFINE_ADD.abap (wird vor ENDMETHOD in ZCL_ZPOWERBI_EINKAUF_MPC_EXT->DEFINE eingefuegt).
"""

# ERNAM (Erfasser) ist ein Personenfeld und bleibt draussen (Regel fuer den Dienst: keine Personenfelder ohne Freigabe).
AUSGELASSEN = {"ERNAM"}
# Filter, die der DPC_EXT in die Selektion von ZM_OFFENE_KDAUF uebersetzt.
FILTERBAR = {"WERK", "KUNNR", "MATNR", "VBELN", "DATVON", "DATBIS", "MATA", "MJUNG", "MFREM", "KORR"}


def extern(name):
    return "".join(t.capitalize() for t in name.lower().split("_"))


def felder():
    out = [("ROWNR", "I", 10, 0)]
    for zeile in open("zmm_ueb_kdauf_felder.txt", encoding="utf-8"):
        teile = [t.strip() for t in zeile.split("|")]
        if len(teile) < 6:
            continue
        _, name, _, typ, leng, dec = teile[:6]
        if name in AUSGELASSEN:
            continue
        out.append((name, typ, int(leng), int(dec)))
    out += [("DATVON", "D", 8, 0), ("DATBIS", "D", 8, 0), ("MATA", "C", 1, 0),
            ("MJUNG", "C", 1, 0), ("MFREM", "C", 1, 0), ("KORR", "C", 1, 0)]
    return out


def property(name, typ, leng, dec):
    z = [f"  lo_property = lo_entity_type->create_property(",
         f"                  iv_property_name  = '{extern(name)}'",
         f"                  iv_abap_fieldname = '{name}' )."]
    if name == "ROWNR":
        z.append("  lo_property->set_is_key( ).")
        z.append("  lo_property->set_type_edm_int32( ).")
    elif typ == "P":
        z.append("  lo_property->set_type_edm_decimal( ).")
        z.append(f"  lo_property->set_precison( iv_precision = {dec} ).")
        z.append(f"  lo_property->set_maxlength( iv_max_length = {leng} ).")
    else:
        # C, N und D als Zeichenkette (Datum JJJJMMTT wie ZPOWERBI_KDAUF, kein Problem mit Initialdatum)
        z.append("  lo_property->set_type_edm_string( ).")
        z.append(f"  lo_property->set_maxlength( iv_max_length = {leng} ).")
    z += ["  lo_property->set_creatable( abap_false ).",
          "  lo_property->set_updatable( abap_false ).",
          "  lo_property->set_sortable( abap_false ).",
          "  lo_property->set_nullable( abap_false ).",
          f"  lo_property->set_filterable( {'abap_true' if name in FILTERBAR else 'abap_false'} ).",
          ""]
    return z


kopf = [
    "* ---------------------------------------------------------------------",
    "* Zc23Set: ZC23 (ZM_OFFENE_KDAUF) live mit Selektion, fuer die Fiori-App ZC23",
    "* (saptasks/fiori_zc23, erzeugt mit erzeugen.py, Stand 2026-10-15).",
    "* Pflichtfilter Werk, Datvon, Datbis (EQ); der DPC_EXT startet den Report mit P_APP.",
    "* ---------------------------------------------------------------------",
    "  lo_entity_type = model->create_entity_type(",
    "                     iv_entity_type_name = 'Zc23'",
    "                     iv_def_entity_set   = abap_false ).",
    "",
]
fuss = [
    "  lo_entity_type->bind_structure( iv_structure_name   = 'ZSTR_ZC23_APP'",
    "                                  iv_bind_conversions = 'X' ).",
    "",
    "  lo_entity_set = lo_entity_type->create_entity_set( 'Zc23Set' ).",
    "  lo_entity_set->set_creatable( abap_false ).",
    "  lo_entity_set->set_updatable( abap_false ).",
    "  lo_entity_set->set_deletable( abap_false ).",
    "  lo_entity_set->set_pageable( abap_true ).",
    "  lo_entity_set->set_addressable( abap_true ).",
    "",
]
zeilen = list(kopf)
for f in felder():
    zeilen += property(*f)
zeilen += fuss
open("ZC23_MPC_DEFINE_ADD.abap", "w", encoding="utf-8", newline="\r\n").write("\n".join(zeilen) + "\n")
print(len(felder()), "Properties,", len(zeilen), "Zeilen")
