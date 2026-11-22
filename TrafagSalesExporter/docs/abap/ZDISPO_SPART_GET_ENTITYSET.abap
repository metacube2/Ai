*&---------------------------------------------------------------------*
*& SAP OData: Produktgruppenbezeichnungen aus ZDISPO_SPART
*& Service: ZPOWERBI_EINKAUF_SRV
*& Stand: 2026-08-11
*&
*& SEGW-Voraussetzung:
*& - Entity Type aus DDIC-Tabelle/-Struktur ZDISPO_SPART importieren
*& - Properties DISPO und DESCR exponieren
*& - Key DISPO setzen
*& - Related Entity Set anlegen, empfohlen: ZDISPO_SPARTSet
*& - generierte GET_ENTITYSET-Methode im DPC_EXT redefinieren
*&
*& Der C#-Client loest den EntitySet-Namen tolerant auf; entscheidend ist,
*& dass der normalisierte Name ZDISPOSPART enthaelt.
*&---------------------------------------------------------------------*

METHOD zdispo_spartset_get_entityset.

  SELECT dispo
         descr
    FROM zdispo_spart
    INTO CORRESPONDING FIELDS OF TABLE et_entityset.

  SORT et_entityset BY dispo.
  DELETE ADJACENT DUPLICATES FROM et_entityset COMPARING dispo.

ENDMETHOD.
