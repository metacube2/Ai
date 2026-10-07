# Unterrouter SAP

Zurueck: `router.md`. Stand: 2026-09-11.

ABAP, ZLO03, ZZPRDAT, PPWR, Produktsparten, SAP-Kalkulation, OData/Gateway.

## Zuerst lesen, bevor irgendetwas in SAP angefasst wird

> **`saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md`**

Diese eine Datei beantwortet die Frage, mit der sonst jede SAP-Aufgabe von vorn
anfaengt. Die Kurzfassung, damit klar ist, was drinsteht:

* **Ein Agent kann SAP vollstaendig fernsteuern**, ohne einen einzigen Klick:
  Quelltext lesen und schreiben, Klassen und Funktionsbausteine aktivieren,
  DDIC-Objekte anlegen, Reports ausfuehren und ihre Listen auslesen, OData
  pruefen. Voraussetzung ist ein Befehl, den **Ingo** einmal ausfuehrt
  (`Set-SapScriptingWarnings.ps1 -Aus`).
* **Drei Zugangswege** mit unterschiedlichen Kosten: ABAP-Report (erste Wahl
  fuer alles Lesende), GUI-Scripting (fuer Schreiboperationen), SapProbe ueber
  RFC (braucht das Passwort). Dazu als vierter der Screenshot von Ingo.
* **Der ABAP-Editor ist ueber die Oberflaeche nicht auslesbar, ueber RFC schon.**
  Schreiben ueber die GUI, gegenlesen ueber `abap-read`.
* **OData laesst sich ohne Anmeldung pruefen**, ueber `/IWFND/GW_CLIENT` in der
  bestehenden Sitzung. Von aussen kommt `401`, weitere Anmeldeversuche sind
  wegen des Sperrrisikos zu unterlassen.
* Dazu der vollstaendige **Skriptbestand** unter `.tmp_sap_probe/` und fertige
  Befehlsfolgen zum Kopieren.

Wer sie ueberspringt, sucht die Antworten erneut. Am 2026-09-10 hat allein die
Frage, warum `Sessions=0` gemeldet wird, zwei Stunden gekostet; die Ursache
stand nicht am Arbeitsplatz, sondern in einem Serverparameter.

## Dateien

| Thema | Datei |
| --- | --- |
| **Wie man SAP ueberhaupt bedient: Zugangswege, Grenzen, Aktivierungsfallen, Gateway/OData, Skriptbestand** | `saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md` |
| **ZLO03 / Stuecklistenanalyse-Webservice** (`ZM_LZCODE20_OPT`) | `docs/abap/README_LZCODE_WEBSERVICE.md` |
| Produktsparten-Provider | `docs/abap/README_PRODSPARTE.md` |
| Produktgruppen als SAP OData (SEGW-Anleitung, Methodenruempfe) | `docs/abap/README_PRODUCT_GROUP_SAP_ODATA.md` |
| Analysereport Standardpreis und Journal (CH/AT) | `docs/abap/README_FIN_ANALYSE_STPRS_JOURNAL.md` |
| **Journal-EntitySet CH/AT: Messung, Fuellgrade und Bauplan** (seit 30.09. teilt es den Transport `T76K912530` mit `HrKpiSet`) | `docs/abap/README_FIN_JOURNAL_ENTITYSET.md` |
| **HR-EntitySet `HrKpiSet`: Bauplan, Stand T76, offene Schritte** | `docs/HR_KPI.md` Abschnitt 8.6 |
| **HR-EntitySet `HrAbsenzSet` (Abwesenheiten je Fall, Transport `T76K912644`)** | `docs/HR_KPI.md` Abschnitt 8.7 |
| **Logistik-Live-Sets `LogTaSet`, `LogLiefSet`, `LogRueckSet` (Transport `T76K912650`)** | `docs/LOGISTIK_LIVE_2026-10-01.md` |
| **`ZM_OFFENE_FAUF`: Bedarfsverursacher (MD_PEGGING_NODIALOG) als einblendbare Felder, Transport `T76K912714`** | `docs/abap/README_ZM_OFFENE_FAUF_BEDARFSVERURSACHER.md` |
| **Operations (Shopfloor): `ShopZd05Set` (ZD05) und `ShopAufSet` (Plan-/Fertigungsauftraege), Transport `T76K912718`** | `docs/SHOPFLOOR_2026-10-07.md` Abschnitt 8, Code `docs/abap/ZSHOP_ADD.abap` |
| **Konditionsart ZRL2 (Mengenrabatt Lieferant) AT, Transport und Mandantenfalle** | `docs/SAP_ZRL2_MENGENRABATT_AT_2026-09-30.md` |
| Produktsparten-Mapping fuer den Group Sales Report | `docs/PRODUCT_SPARTEN_MAPPING_2026-05-27.md` |
| Produktmapping, Kurzstand | `docs/rag/PRODUCT_MAPPING.md` |
| Uebergabe Produktsparten-Zuordnung | `spartenlogic/UEBERGABE_PRODUKTSPARTEN_ZUORDNUNG.md` |
| **PPWR und Stoffcompliance, aktueller Stand und Mandant 100** | `docs/PPWR_MANDANT_100_ANALYSE_2026-08-18.md` |
| PPWR, fachlicher Katalog und Anlageprotokoll (Erfolgsmeldung strittig, siehe Zeile darueber) | `docs/PPWR_SAP_KLASSIFIZIERUNG_ANLAGEPROTOKOLL_2026-08-13.md` |
| Wie SAP Ruestzeit von Bearbeitungszeit unterscheidet | `docs/SAP_KALKULATION_RUESTZEIT_BEARBEITUNGSZEIT_ANDREAS_2026-07-30.md` |
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
  mit Abstand der schnellste Weg. GUI-Fernsteuerung nur fuer Schreiboperationen.
  Ganzer Katalog inklusive Aktivierungsfallen:
  `saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md`.
- **Korrigiert am 2026-09-11: ABAP aus Klassen IST lesbar.** Frueher stand hier, der
  ABAP-Editor lasse sich nicht auslesen. Das gilt nur fuer das **GUI-Steuerelement**.
  Ueber RFC geht es sehr wohl, auch fuer Klassen
  (`abap-read ZCL_...==CM001`, Deklarationen `==CU`/`==CO`/`==CI`). Damit ist
  Gegenlesen nach jeder Aenderung moeglich — schreiben ueber die GUI, pruefen ueber RFC.
  Die alte Formulierung hat am 2026-09-10 dazu gefuehrt, dass eine Klassenaenderung
  ungeprueft blieb.
- **Gateway/OData, drei Fallen, die je eine Stunde gekostet haben:**
  `bind_structure( )` legt **keine** Properties an, jede Property will einzeln
  ueber `create_property( )` angelegt sein; `sy-langu` ist in einem OData-Aufruf
  nicht zwangslaeufig Deutsch, sprachabhaengige Texte brauchen einen Rueckfall;
  und **zwei Bedingungen auf demselben Feld** liefert das Gateway gar nicht erst
  als Filteroption aus, der Filter faellt dann lautlos komplett weg. Alle drei
  mit Messung in `saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md` und
  `docs/abap/README_FIN_JOURNAL_ENTITYSET.md`.
- **`JEST`/`I0002` ist kein Freigabenachweis.** Bei abgeschlossenen Auftraegen ist der
  Status inaktiv gesetzt. Verlaesslich ist `AFKO-FTRMI`.
- **`T76` ist eine rund sechs Monate alte Kopie.** Im Jahr 2026 stehen dort nur die
  Perioden 01 bis 04. Wer gegen einen spaeteren Monat misst, misst einen leeren
  Zeitraum und haelt das Ergebnis fuer einen Erfolg.
- Werkzeuge und ihre Grenzen: `docs/router/plattform.md`, Abschnitt Live-Werkzeuge.

## Querverweise in Nachbaraeste

- Was die App aus den SAP-Daten macht: `docs/router/finance.md`
- Produktgruppen im Einkauf: `docs/router/einkauf.md`
