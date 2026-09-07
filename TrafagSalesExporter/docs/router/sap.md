# Unterrouter SAP

Zurueck: `router.md`. Stand: 2026-09-07.

ABAP, ZLO03, ZZPRDAT, PPWR, Produktsparten, SAP-Kalkulation.

## Dateien

| Thema | Datei |
| --- | --- |
| **ZLO03 / Stuecklistenanalyse-Webservice** (`ZM_LZCODE20_OPT`) | `docs/abap/README_LZCODE_WEBSERVICE.md` |
| Produktsparten-Provider | `docs/abap/README_PRODSPARTE.md` |
| Produktgruppen als SAP OData (SEGW-Anleitung, Methodenruempfe) | `docs/abap/README_PRODUCT_GROUP_SAP_ODATA.md` |
| Analysereport Standardpreis und Journal (CH/AT) | `docs/abap/README_FIN_ANALYSE_STPRS_JOURNAL.md` |
| Produktsparten-Mapping fuer den Group Sales Report | `docs/PRODUCT_SPARTEN_MAPPING_2026-05-27.md` |
| Produktmapping, Kurzstand | `docs/rag/PRODUCT_MAPPING.md` |
| Uebergabe Produktsparten-Zuordnung | `spartenlogic/UEBERGABE_PRODUKTSPARTEN_ZUORDNUNG.md` |
| **PPWR und Stoffcompliance, aktueller Stand und Mandant 100** | `docs/PPWR_MANDANT_100_ANALYSE_2026-08-18.md` |
| PPWR, fachlicher Katalog und Anlageprotokoll (Erfolgsmeldung strittig, siehe Zeile darueber) | `docs/PPWR_SAP_KLASSIFIZIERUNG_ANLAGEPROTOKOLL_2026-08-13.md` |
| Wie SAP Ruestzeit von Bearbeitungszeit unterscheidet | `docs/SAP_KALKULATION_RUESTZEIT_BEARBEITUNGSZEIT_ANDREAS_2026-07-30.md` |
| **Wie man ueberhaupt effizient mit SAP arbeitet: Zugangswege, Grenzen des GUI-Scriptings, Aktivierungsfallen, Skriptbestand** | `saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md` |
| **ZZPRDAT: Transportauftrag `T76K912490`, Objektliste, Nachtest und was vor der Freigabe offen ist** | `saptasks/ZZPRDAT_TRANSPORTPLAN.md` |
| **ZZPRDAT: Loesungsdokument fuer den Fachbereich**, seit 2026-09-07 mit Kapitel „So testen Sie es selbst" (drei Testfaelle, Fehlerbilder) | `docs/ZZPRDAT_Loesung_2026-09-03.docx` |
| ZZPRDAT: Anschreiben zur Abnahme (Vorlage fuer den Outlook-Entwurf) | `docs/ZZPRDAT_Mail_Abnahme_2026-09-04.html` |
| ZZPRDAT-Arbeitsstand und vollstaendiger Analyseverlauf | `saptasks/zzprdat-kontext.md` |
| ZLO03-Systemabgleich und Codefixes | `zlo03/BEFUND_SYSTEMABGLEICH_2026-08-03.md`, `zlo03/ZM_LZCODE20_OPT_fixes.md` |

## Systeme

| System | Rolle |
| --- | --- |
| `travt762` | Test, SID `T76`, Client 100 — Default fuer SapProbe |
| `travp762` | **Produktion**, SID `P76` — nur bewusst ansteuern |

## Fallen in diesem Ast

- **Der CH/AT-Exportreport heisst im System `Z_TRAFAG_DACH_EXPORT`.** Die lokale Datei
  `docs/abap/Z_TRAFAG_SCHWEIZ_EXPORT.abap` und der `REPORT`-Kopf tragen noch den alten
  Namen, der in **keinem** System existiert. Beim Suchen nicht darauf verlassen.
  Betriebsregeln und die Warnung zur Service-URL: `docs/FINANCE_STANDARDKOSTEN.md`
  Abschnitt 8.
- **Keine Tabellen- oder Feldnamen erfinden.** Erst am System pruefen. Dieser Fehlertyp hat
  bei UK-2025 und beim IT-Superlativ zugeschlagen.
- **Numerische Materialnummern:** `ALPHA` war der falsche Konvertierungsbaustein, richtig
  ist Rohwert plus `MATN1`. Siehe `docs/abap/README_LZCODE_WEBSERVICE.md`.
- **Fuer alles Lesende zuerst einen ABAP-Report schreiben, den Ingo ausfuehrt.** Das ist
  mit Abstand der schnellste Weg. GUI-Fernsteuerung nur fuer Schreiboperationen; der
  ABAP-Editor laesst sich ueber die Scripting-Schnittstelle nicht auslesen, dafuer
  Screenshots geben lassen. Ganzer Katalog inklusive Aktivierungsfallen:
  `saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md`.
- **`JEST`/`I0002` ist kein Freigabenachweis.** Bei abgeschlossenen Auftraegen ist der
  Status inaktiv gesetzt. Verlaesslich ist `AFKO-FTRMI`.
- Werkzeuge und ihre Grenzen: `docs/router/plattform.md`, Abschnitt Live-Werkzeuge.

## Querverweise in Nachbaraeste

- Was die App aus den SAP-Daten macht: `docs/router/finance.md`
- Produktgruppen im Einkauf: `docs/router/einkauf.md`
