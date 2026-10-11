*&---------------------------------------------------------------------*
*& SAP OData: Disponent-/Produktgruppen-Zuordnung aus ZDISPO_GRP
*& Service: ZPOWERBI_EINKAUF_SRV
*& Stand: 2026-08-11
*&
*& SEGW-Voraussetzung:
*& - Entity Type aus DDIC-Tabelle/-Struktur ZDISPO_GRP importieren
*& - Properties DISPO_KZ und DISPO exponieren
*& - Composite Key DISPO_KZ + DISPO setzen, weil ein Muster mehreren
*&   Produktgruppen zugeordnet sein kann (z.B. DS1/DS2)
*& - Related Entity Set anlegen, empfohlen: ZDISPO_GRPSet
*& - generierte GET_ENTITYSET-Methode im DPC_EXT redefinieren
*&
*& Der C#-Client loest den EntitySet-Namen tolerant auf; entscheidend ist,
*& dass der normalisierte Name ZDISPOGRP enthaelt.
*&---------------------------------------------------------------------*

METHOD zdispo_grpset_get_entityset.

  SELECT dispo_kz
         dispo
    FROM zdispo_grp
    INTO CORRESPONDING FIELDS OF TABLE et_entityset.

  SORT et_entityset BY dispo_kz dispo.
  DELETE ADJACENT DUPLICATES FROM et_entityset COMPARING dispo_kz dispo.

ENDMETHOD.
