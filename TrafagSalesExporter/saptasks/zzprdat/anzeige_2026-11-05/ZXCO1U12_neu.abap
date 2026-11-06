*&---------------------------------------------------------------------*
*& Include ZXCO1U12
*&---------------------------------------------------------------------*
* ZZPRDAT 2026-11-06 (Ingo): Kundenfelder aus dem Bild unveraendert an den Auftrag zurueckgeben.
* Vorher komplett auskommentiert: beim Verlassen des Reiters TRAFAG Daten ging ein LEERES ZZPRDAT
* zurueck (Reiter danach leer, in CO02 beim Sichern geloescht). ZXCO1U11 fuellt CI_AUFK aus I_CAUFVD.
MOVE-CORRESPONDING ci_aufk TO e_coci_aufk.

** e_coci_aufk-zzprdat = gs_aufk-zzprdat.
*MOVE ci_aufk-zzprdat TO e_coci_aufk-zzprdat.
