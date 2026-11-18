# RAG Project

Stand: 2026-11-18 09:35 (Deploy `8d15d48` B1-Lieferant aus Wareneingang und Spanien); davor 08:25 (Deploy `f2c0267`: Handbestellwesen-Stammdaten fuer alle, Freigabe-Rollen ueber CHF 1'000, Lieferanten-Klasse A/B/C); davor 2026-11-17 17:25 (Shopfloor Entscheide Fabio aufgenommen, Paket B nach ZC22-Abgleich 18.11.; Finance Muster und Wirkung Entscheid 1); davor 14:45 (Deploy 14:36 `32bf356`, Shopfloor Ergaenzung Fabio 17.11.); davor 12:50 (Deploy 12:43 `2e62163`, gemeinsamer Stand: Handbestellwesen AD-Abgleich Teil 2 und Shopfloor CO-PA/M*); davor 12:40 (Deploy 12:25 `3298c93` Shopfloor CO-PA schreiben und Arbeitsplatzfilter M* ein, CO-PA 02. bis 16.11. nachgeladen); davor 11:28 (Deploy `6a7de50` Handbestellwesen naechtlicher AD-Abgleich, Schreiben aus); davor 2026-11-16 abends (Deploy 17:06 `7cc1a62` Shopfloor Status- und Materialfilter ein; davor Deploys 08:45 `e10ab3a` Export-Download und 09:33 `2da2682` Sales_All Blatt Lieferantenzuordnung); davor 2026-11-13 abends (Shopfloor Paket Fabio 13.11.: Phase A `d754a76`, Layoutfilter `9100537` deployed, T76 `T76K912906` offen; davor 10:50 Finance Lieferantenzuordnung in Sales_All deployed `df48301`; davor 09:30 Einkauf Wuensche Marco deployed `052c5a3`; davor 08:15 Shopfloor Stichtagswerte und Verzug deployed `0e94279`; davor 2026-11-12 13:50: Uebergabe: Doku-Pruefung und Code-Review abgeschlossen, Entscheide offen; davor 12:00: Deploy 11:33 `e956183`; Spanien Supplier umgesetzt, nicht deployed; ZC12-Plan; Lieferantenbewertung Paket 1/2; WLAN-Stoerung; Doku-Pruefung: aeltere Kurzstand-Punkte 2026-11-06/09, 2026-11-05 und 2026-09-29 sowie die zwei Verweise auf den Juli-Abgleich als ueberholt markiert); davor 2026-11-11 (IC V1 ZICAA1 in T76 umgesetzt, T76K912884; Vorgehen „erst Plan, dann Plan ausfuehren“); davor 2026-11-09 abends (Uebergabe in `lastchange.md` oben: T76K912839 und T76K912860 in P76, Vorrat bereit zum Einschalten, Verkauf ISS-032); 2026-11-06 (ZZPRDAT: Transport T76K912490 gebuendelt); 2026-11-05 (Monthly Review, QmMeldungSet, Einkauf-Pruefung); 2026-11-02 abends (Nachtrag Shopfloor J4/KEA0, PPD-Verzug, KPI Kunden, P76-Caches); 2026-10-22 nur Archivverweise `docs/archiv/`; fachlich 2026-10-16 (Kurzstand 2026-10-16 Shopfloor-Mengen aus SAP, Operations offen, Menue, Handbuch, Stuecklisten-Gehirn; 2026-10-14/15 Operations, Startseite, Nutzung, Zusammenarbeit Fabio; HERMES-Sicht, PM-10 bis PM-12, Finance-Review vom 2026-09-29; uebriger Kurzstand vom 2026-09-07)

Live-Abgleich vom Juli fuer UK-2025, Supplier-Felder, Konzern-Standardkosten
und Einkauf-Delta: `docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md`, heute nur noch
historische Messreferenz.
Vorrang hat nach `router.md` Regel 1 immer der juengste direkt gepruefte Beleg je Thema.

## Kurzstand

- **2026-11-18 09:31 (Finance):** Deploy `8d15d48`: B1 (FR/US/IT/IN) fuellt leere Lieferanten aus dem letzten Wareneingang (FR 130 -> 2'489, US 6 -> 1'138 Zeilen), Spanien `c9208b5` produktiv (wartet auf Santis Exportlauf). `docs/FINANCE_SUPPLIER.md` 11.
- **2026-11-18 08:15 (Handbestellwesen, Einkauf):** Deploy `f2c0267`: Besteller erfassen Kreditoren, Kostenstellen und Sachkonten
  selbst (aendern/loeschen Admin); ueber CHF 1'000 stehen Kostenstellenleiter und Projektleiter in der Freigabeliste (nur mit Limit);
  Lieferanten-Klasse A/B/C auf `/einkauf/lieferanten` (Paket 1 Schritt 2). Doku Konzept 6.13, EINKAUF_SITZUNG 6.7.
- **2026-11-17 abends (Shopfloor Entscheide Fabio, Finance):** Fabios Entscheide 17.11. in `docs/SHOPFLOOR_2026-10-07.md` 26: Offen
  nach Abschlussstatus (Liste und PPD), Excel bis Go-live 01.01.2027 fuehrend, Kalender CH aus P76 vorgeschlagen, Termin-Mapping
  58613 in P76 belegt; Excel hat 160 Eintraege mit Abschlussdatum und offenem Status (vor dem Umstellen klaeren). Kein Deploy; Paket B
  nach dem ZC22-Abgleich 18.11. Finance: Muster-Arbeitsmappe fuer Andreas, „immer lokal“ waere rund 2,5 Mio. CHF weniger
  Gruppenmarge (`docs/FINANCE_SUPPLIER.md` 10).
- **2026-11-17 12:50 (Handbestellwesen, AD-Abgleich Teil 2):** Antworten Ingo umgesetzt (wieder aktiv nur mit Vermerk des
  Abgleichs, abgelaufen wie gesperrt, Altbestand mit aktivem AD-Konto wird AD-Eintrag, Reiter „Deaktivierte“ mit Neuzuweisung,
  `Schreiben` an). Deploy 12:36 `44a3ed1` nahm den Shopfloor-Deploy von 12:25 zurueck; gemeinsamer Stand `2e62163` 12:43 (gemergt
  `fd49894`). Noch nicht ausgefuehrt: ein Konto gehoert im AD einer anderen Person, Entscheid Ingo
  (`docs/HANDBESTELLWESEN_KONZEPT_2026-10-22.md` 6.12). **Produktiv `2e62163`; Spanien nur aus dem Arbeitszweig ab `fd49894`.**
- **2026-11-17 nachmittags (Shopfloor, Ergaenzung Fabio):** Deploy 14:36 `32bf356` (gemergt `ab04aa5`): Schnittstellen-Seite mit echtem Status (44 geliefert, 56 definiert, 3 in Ueberpruefung, 2 offen), Bestellnummer Einkauf Handeingabe, TX-Mengen aus dem MES (Prozedur `TMS_MES.dbo.getProdQtyPerDayOfMonthPerDisponent`, braucht MES-DB-User), Rueckstandslisten ausgewertet (alle 596 Eintraege noch Excel-Import, 123 offene mit Abschlussdatum, Liste fuer die Dispo). Spanien nur ab `ab04aa5`. `docs/SHOPFLOOR_2026-10-07.md` 25.
- **2026-11-17 mittags (Shopfloor, CO-PA und `M*` ein):** Entscheid Ingo („8. ein, 3, 4 so dass es passt schalten“). Deploy 12:25
  `3298c93` (Zweig ab `6a7de50`, gemergt `684ae25`): `SapCoPaEnabled = true`, `Zc22ArbplAusschluss = M*` (ganzes ZC22-Layout
  `/ P00 TX`; Vorschau produktiv: TX-Vorrat −457, DW −454, TR5 −246 Stk., Laufzeit 24 s). CO-PA 02. bis 16.11. nachgeladen, gleich
  Fabios Kontrollwerten ausser AE Stand 12.11. (+3'906.68, offen). Stichtag 17.11. bleibt ohne `M*`, erster mit ganzem Layout 18.11.
  Lauf 04:00 am 17.11. hat stattgefunden. **Produktiv `3298c93`; Spanien `c9208b5` weiter nicht deployed, Deploy nur aus dem
  Arbeitszweig ab `684ae25`.** *(Ueberholt 12:43: produktiv `2e62163`, Spanien nur ab `fd49894`, Zeile darueber.)* `docs/SHOPFLOOR_2026-10-07.md` 24.
- **2026-11-17 (Handbestellwesen, AD-Abgleich):** Naechtlicher Abgleich der Mitarbeitenden mit dem AD gebaut und deployed 11:28
  `6a7de50` (gemergt `0197661`): gesperrt oder nicht im AD = inaktiv, Name/Mail der AD-Eintraege nachgefuehrt, Rollen bleiben,
  nichts geloescht. Nachtlauf zuerst nur Vorschau (`Schreiben` aus). Erste Vorschau: 15 von 109 aktiven wuerden inaktiv (alle
  Altbestand, im AD gesperrt). Acht Fragen an Ingo in `docs/HANDBESTELLWESEN_KONZEPT_2026-10-22.md` 6.11. **Produktiv `6a7de50`;
  Spanien `c9208b5` weiter nicht deployed, Deploy nur aus dem Arbeitszweig ab `0197661`.** *(Ueberholt 17.11. 12:25: produktiv
  `3298c93`, Spanien nur ab `684ae25`, Zeile darueber.)*
- **2026-11-16 abends (Shopfloor, Filter eingeschaltet):** `T76K912906` ist seit 16.11. in P76 (Import Ingo). Abnahme in P76 nur
  lesend: Vorschau = ZC22 fuer Verzug und Vorrat (90 Wertepaare), Logistik 18 von 18 Kontrollwerten Fabios. Deploy 17:06 `7cc1a62`
  (gemergt `69ed8a0`): Statusfilter `FREI DRUC RÜCK VOKL*` und Materialfilter `E*,D*,F*,C*` ein; Logistik 02. bis 12.11. nachgeladen.
  Aus bleiben der Arbeitsplatzfilter `M*` (DW deshalb 540 Stk. ueber der Liste der Teamleiter, wartet auf Fabio) und
  `SapCoPaEnabled` (Vorschau trifft Fabios Werte an drei von vier Stichtagen exakt) *(ueberholt 17.11. 12:25: beide ein, Zeile
  oben)*. Erster Stichtag mit Statusfilter: 17.11.,
  den Lauf morgens pruefen. **Produktiv `7cc1a62`; Spanien `c9208b5` weiter nicht deployed, Deploy nur aus dem Arbeitszweig ab
  `69ed8a0`.** *(Ueberholt 17.11. 11:28: produktiv `6a7de50`, Spanien nur ab `0197661`, Zeile darueber.)* `docs/SHOPFLOOR_2026-10-07.md` 23.
- **2026-11-16 (Finance und Export-Seite):** Deploy 08:45 `e10ab3a`: „Excel oeffnen“ auf der Export-Seite laedt die Datei in den
  Browser, statt sie auf dem Webserver zu starten. Deploy 09:33 `2da2682`: Sales_All bekommt das Blatt `Lieferantenzuordnung`
  (wirksame Zuordnung der Gruppenmarge je Gesellschaft und Monat), weil Andreas nach den rohen Lieferantenfeldern pivotiert und
  CH dort leer ist. Nachweis am Export 12:11 erbracht (geprueft 16:10, ohne Abweichung). **Produktiv `2da2682`; Spanien `c9208b5` weiter nicht deployed, Deploy nur
  aus dem Arbeitszweig ab `70507fc`.** *(Ueberholt 16.11. 17:06: produktiv `7cc1a62`, Spanien nur ab `69ed8a0`, Zeile darueber.)* `docs/FINANCE_SUPPLIER.md` 9.
- **2026-11-13 (Shopfloor, Paket Fabio 13.11., Excel abloesen):** Phase A deployed 12:22 `d754a76` (Vorrat rollend, TR5/DW
  Material 1-99999, DW D*, plan5 kumuliert, Verzug ab 01.01.2026, Monat des letzten Arbeitstags, CZ/MTF/Sensoren entfernt). P76-Befund:
  Fabios Excel-Werte = ZC22/ZC27 mit den Filtern ihrer Standardlayouts (`/ P00 TX`, `/NIN`); Fabios Logistik-Regel waere falsch.
  Layoutfilter im Cockpit deployed 14:33 `9100537` (Schalter aus), SAP in `T76K912906` (CoPaSumSet Hzdat, ShopRmSet Matex, ShopAufSet
  Sttxt/Arbpl/Avo), wartet auf Freigabe/Import Ingo. *(Ueberholt 16.11.: importiert, Status- und Materialfilter eingeschaltet, oben.)* `docs/SHOPFLOOR_2026-10-07.md` 22.
- **2026-11-13 (Finance, Mail Andreas 13.11.):** Lieferantenzuordnung je Gesellschaft nachgemessen; Deploy 10:31 `df48301`:
  Sales_All zeigt die wirksame Zuordnung (`Supplier Type`, `Delivering Entity`, `Cost Source`). Neu `ISS-003.6` (Italien, 162
  Artikel ohne Lieferant) und `ISS-003.7` (UK-Nummern), `ISS-016` wieder offen (DE-Kunden als ID seit 09/2026). Vier
  Mailentwuerfe in Ingos Downloads; offen Export-Pruefung 12:00 und Entscheide Ingo. `docs/FINANCE_SUPPLIER.md` 9.
- **2026-11-13 (Einkauf, Mail Marco Widmer 12.11.):** Deploy 09:11 `052c5a3`: Offene Bestellungen und Kontrakte ausgeblendet,
  Spend-Aufriss ohne Kacheln/Volumenbalken, Kuchen je Einstieg, Anzahl Artikel nach XYZ, Matrix in Marcos Reihenfolge,
  Produktgruppe nach Verwendung : Lieferant statt der Sichten nach eigenem Disponenten (nur HW_ZUKAUFSORT), Region/Disponent >
  Warengruppe, LZ/Sortiment entfernt, Herstellkosten im Materialanteil. Offen: Marco informieren. `docs/rag/PURCHASING.md`.
- **2026-11-13 (Shopfloor, Mail Fabio 13.11.):** Deploy 07:56 `0e94279` (eigener Zweig, Spanien `c9208b5` weiter **nicht**
  deployed). Vorrat, offene KW, SEH-Tagesziel, TX `plan5` und PPD-Verzug sind Stichtagswerte aus dem ersten SAP-Lauf des Tages;
  Morgenwerte 10.-12.11. aus dem Verlauf zurueckgeholt; **PPD-Verzug eingeschaltet, ohne Materialfilter**; Cache-Busting.
  Offen: CO-PA (Caches P76, Freigabe Ingo), Fabio prueft DW-Verzug 9'575 und SEH 12.11. 212 %; am 13.11. lief 04:00 nicht
  (vermutlich ISS-017 Dauerbetrieb, nicht untersucht); 09.11. SEH bleibt 2'335 % bis Entscheid Ingo. `docs/SHOPFLOOR_2026-10-07.md` 21.
- **2026-11-12 (Stand 12:00, Einzelheiten `lastchange.md` oben):**
  - **Deploy 11:33 `e956183`**: Kleinigkeiten vom 10.11. und Rexx Hub vorbereitet (Schalter `HrKpi:RexxHub:Enabled` aus).
  - **Spanien Supplier (ISS-003.3)**: Andreas hat entschieden, der Lieferant des letzten Wareneingangs gilt als Supplier.
    Umgesetzt in `c9208b5` (Sage-Skripte, Pruefskript, Erbregel im Import, Seed; 1556/1556), **nicht deployed**, Santi noch
    nicht beliefert (zwei Gaenge). Plan `docs/PLAN_ES_SUPPLIER_WARENEINGANG_2026-11-12.md`.
  - **IC V1**: von Fabio in T76 getestet und fachlich freigegeben (`T76K912884`); Import P76 wartet auf seine Z999-Preise.
  - **ZC12 (PM-02)**: Plan und Einspieldatei fuer Wert 0, linksbuendige Popup-Werte und Laufzeit liegen vor
    (`saptasks/zc12/`), wartet auf Ingos Ja zur Ausfuehrung in T76.
  - **Lieferantenbewertung (Wochen_Todo 122)**: V6 ist Grundlage, Paket 1 (SAP-Performance, Q2, A/B/C je Lieferant) und
    Paket 2 (Pflege im Dashboard, Oscar einverstanden); SAP-Quellen geklaert; T76 ist S/4HANA 2023.
    `docs/EINKAUF_SITZUNG_2026-10-22.md` 6 bis 6.3.
  - **WLAN `trafag-802.1x-tel`** in Bubikon gestoert, Sache der IT; Befunde `docs/router/plattform.md`.
  - **Doku-Pruefung** aller Markdown-Dateien abgeschlossen (`9e27a70`): 217 Befunde, 156 eingearbeitet, 39 Entscheide Ingo
    offen, `docs/MD_KONSISTENZPRUEFUNG_2026-11-12.md`.
  - **Code-Review** des Anwendungscodes: 13 Befunde, 4 hoch (Source Viewer zeigt die Konfiguration, Freischalt-Cookie aus dem
    gespeicherten Hash berechenbar, schreibende Seiten ohne Freischaltung, Standortbestand nicht atomar ersetzt); nichts
    geaendert, Behebungsvorschlag in `docs/CODE_REVIEW_2026-11-12.md` Abschnitt F, Wochen_Todo 159.
  - **ZLO03 FIX 28 (13:45)**: Meldung Armin „ZLO03_Fehler in der Stücklistenmenge“ (D13006 = 2): Selbst-Stuecklisten zaehlten
    seit FIX 2 (10.03.2026) doppelt. In T76 getestet, Auftrag `T76K912902` seit 13:54 in P76, Wirkung um 14:15 belegt (5501 D13006 = 1,
    63759 H20201 0.06 CHF); offen: Einkauf-Lauf und Antwort an Armin; Wochen_Todo 160, `zlo03/FIX28_SELBSTSTUECKLISTE_2026-11-12.md`).
  - **CO-PA `T76K912866`** laut Ingo in P76; offen Gateway-Caches, Vorschau, Schalter (`saptasks/copa_verkauf/NOTES.md` 3).
  - **Uebergabe:** `lastchange.md` oben, „Uebergabe 2026-11-12 13:50“, mit allen offenen Punkten in Reihenfolge.
- **2026-11-06/09:** Vier T76-Transporte warten auf Ingo (Uebersicht `lastchange.md` „Uebergabe Stand 2026-11-09“): FIX 26 Materialanteil, QmMeldungSet, IC-Staffelbasis nach Bestellmenge (VOFM 601; *10.11.: importiert, aber in P76 noch nicht eingebunden; VOFM in P76 gesperrt, `RV62ANNN` nicht transportierbar, `RV80HGEN` bindet auch FBED 908 ein, Entscheid Ingo offen, `saptasks/ic_preisfindung/NOTES.md` 8.9*), ZZPRDAT inkl. Reiter-Fix und Georgs XCO1-Objekte (vor Import P76 pruefen). ZZPRDAT-Testnachweis fuer den Fachbereich liegt vor. *(Ueberholt, Vermerk vom 2026-11-12: Von den vier Transporten sind zwei seit dem 2026-11-09 in P76: FIX 26/27 `T76K912839` (Kopf dieser Datei) und `QmMeldungSet` `T76K912837` (`saptasks/qm_meldungen/NOTES.md` Nachtrag 2026-11-09 15:13, Q-Faelle seit 15:11 eingeschaltet). Die IC-Staffelbasis ist importiert, aber nicht eingebunden, wie oben in diesem Punkt steht. Offen bleiben ZZPRDAT `T76K912490` und, seit 10./11.11. dazugekommen, CO-PA `T76K912866` und IC V1 `T76K912884`.)*
- **2026-11-05:** Verkauf-Unterreiter **Monthly Review Gesellschaften** (Wunsch Patrick Hertach) produktiv seit 10:33 (`47abb30`),
  Q-Faelle-Abschnitt gebaut, Abruf aus bis `T76K912837` (`QmMeldungSet`, offene Q1) in P76 ist *(ueberholt: seit 2026-11-09 15:11 in P76 und eingeschaltet, `saptasks/qm_meldungen/NOTES.md` Nachtrag)*
  (`docs/monthly_review/MONTHLY_REVIEW_2026-11-04.md`). Einkauf-Pruefung im Chrome mit Fehlerliste
  (`docs/EINKAUF_PRUEFUNG_2026-11-05.md`); Materialanteil falsch wegen ignorierter Stuecklisten-Basismenge, FIX 26 offen. *(ueberholt: `T76K912839` am 2026-11-09 von Ingo nach P76 importiert, HW_ZUKAUFSORT 1.27 Mio. statt 957 Mio., `zlo03/FIX26_BASISMENGE_2026-11-05.md` Nachtrag)*
  Neu offen: IC-Preisfindung ZICAA1 fuer Fabio (*Stand 12.11.: V1 in T76/100 von Fabio getestet und fachlich freigegeben (T76K912884), Import P76 wartet auf Fabios Z999-Preise in P76, NOTES 9.1*; Wochen_Todo 153; T76/P76 lesend: Preisliste 14 kommt in der IV an, Kurs schon 0.94 ueber EURX, `saptasks/ic_preisfindung/NOTES.md`), SPC-Adapter Adrian Jenni (151, Adrian geht Ende Jahr).
  Nachmittags: Einkauf-Fixes F2-F9 und Spend-Aufriss aufgeraeumt (Matrix und alter Spend-Block weg, deployed 12:37 `785325f`), ZLO03 FIX 26 in T76 (`T76K912839`), Agenten-Definitionen `.claude/agents/`. Uebergabe: `lastchange.md` oben.
- **2026-11-03 Abschlussrunde Dashboard/Data Lake:** Einkauf nutzt aktiv, will HK/Standardpreis zum Materialanteil,
  Streamlining und Lieferantenmanagement (Logik in SAP, Dashboard zeigt an); Operations OTD/OTIF/OTP live, Wunsch
  Stammdatenfehler-Seite; HR wartet auf Rexx-Live-Daten (Sitzung mit Nadja, Alex, Artjom zum Hub; *10.11.: Entscheid Ingo, der Rexx Hub wird genommen*); Finance muss die
  Gruppenmargen noch testen. Oscar: Inventar Power BI/Smartsheet, Taskliste offener Wuensche mit Aufwand, Datenqualitaet und
  Datenverantwortliche in Trafag Projekte, abgeloeste Z-Objekte loeschen. Datenherren: Produktion Dragan, Einkauf Marco
  Widmer, Material/Debitoren Fabios Bereich, HR. `docs/SITZUNG_ABSCHLUSSRUNDE_DASHBOARD_2026-11-03.md`.

- **2026-10-16:** **Shopfloor** bekommt Mengen aus SAP: Logistik nach ZC27 (`ZPP_RM_MENGEN`), Tagesmengen SEH/TR5 und FPY SEH ueber die
  neuen Sets `ShopRmSet`/`ShopProdSet` (Transporte `T76K912740` + `T76K912738` in P76), gegen Fabios Kontrollwerte gleich, live seit 09:51
  (`saptasks/shopfloor_sap/NOTES.md`). Fabios Paket vom 16.10. eingespielt (Ampel, Lieferzeiten, Forecast DS, `SHOPFLOOR_2026-10-07.md` 14/15).
  **Operations offen fuer alle Windows-Benutzer** (`Shopfloor:OpenForAll = true`), ZD05-Abgleich **04:00 und 12:45** (Entscheid Ingo).
  Falle P76: zwei Anwendungsserver, Gateway-Cache auf `travp762` leeren (*ueberholt 2026-11-02:* SM51 zeigt nur `trasp76s`; Caches
  dort leeren, zuerst `/IWBEP`, dann `/IWFND`, LEARNINGS). **Menue:** alle Hauptbereiche farbig (gedeckte Rottoene), Admin
  grau, Home pulsiert. **Handbuch:** Kapitel Operations mit Geschaeftsprozess, Schnellueberblick und Prozessgrafik (Trafag-CI); das Handbuch
  kann jetzt Bilder. **Einkauf:** Stuecklisten-Netz „Gehirn“ (WebGL-Prototyp) zuoberst im Spend-Aufriss. Offen: J2/J3 Verkauf (KEA0),
  J6 Auftragsvorrat, Lieferzeit-Ziele, ganze Stueckliste aus SAP. *Nachtrag 2026-11-02:* KEA0 gelesen (Ergebnisbereich 1000, `CE11000`,
  `WWPSP`), J2/J3 weiter offen; J4 Verkauf (erwarteter Umsatz, Gutschriften) aus Fiori gebaut; PPD-Verzug aus ZC22 gebaut (`ShopAufSet` Typ VZ, `T76K912797` am
  02.11. in P76 importiert, deployed, Schalter `SapVerzugEnabled` noch aus bis zum Abgleich mit ZC22; *ueberholt: seit 13.11. ein, ohne Materialfilter*). **Operations KPI:** Rubrik
  Kunden (OTD/OTIF/OTP je Auftraggeber) produktiv 02.11. 13:57, Abnahme Fabio bestanden (`docs/OPERATIONS_KPI_2026-10-14.md` 5).
  Einzelheiten `docs/SHOPFLOOR_2026-10-07.md` Abschnitt 16.
- **2026-10-14/15:** **Operations** ist eine Menuegruppe mit **KPI** (Liefertreue OTD / OTIF / OTP nach Spezifikation Fabio
  Palma, live aus SAP ueber die CDS-Services, Abnahme Jan-Sep 2026 bestanden, Monatsabruf mit Wiederholung alle 2 h,
  `docs/OPERATIONS_KPI_2026-10-14.md`) und **Shopfloor** (Feldmapping aus Fabios Excel, Disponenten je Abteilung, Patches
  ZD05 „Zu besprechen“ und Verkauf Go-for-it, `docs/SHOPFLOOR_2026-10-07.md` 12/13). Der technische SAP-Benutzer des Cockpits
  heisst **`POWERBI`**; seine Uebergangsrolle `ZF_BR_SALES_MANAGER` laeuft am **31.10.** aus (schmale Rolle beim Hauptadmin).
  Startseite neu: animierte Standortgrafik mit Live-Zustand je Land, Titel „Trafag Dashboard“, Manometer per Admin-Schalter
  (Standard aus), CPU-Pegelanzeige in der Kopfleiste, Handbuch Kapitel 10 Operations. **Nutzung:** die Leute schauen hinein,
  arbeiten aber nicht damit; anonyme Nutzungsmessung seit 14.10. unter Admin > Nutzung, Massnahmen in
  `docs/NUTZUNG_2026-10-14.md`. **Zusammenarbeit Fabio:** er moechte selbst entwickeln und Ingo nur freigeben; kein Zugriff
  aufs Haupt-Repository (Passwort-Hashes in `appsettings.json`, HR/Finance-Doku, Deploy), Vorschlag Paket-Ordner, pflegbare
  Konfiguration, spaeter eigenes Repository (Wochen_Todo 99, 100). Auto-Modus-Regeln oben in `router.md`.
- **2026-09-29:** Fuer den Interims-Vorgesetzten gibt es eine **Vorhabenuebersicht nach HERMES**,
  zugeschnitten auf ein KMU: Blatt „HERMES Übersicht" in `projektmanagement/Wochen_Todo.xlsx`, Quelle
  `projektmanagement/Vorhaben_HERMES.tsv`, erzeugt mit `projektmanagement/wochen_todo_xlsx.py`. Je
  Vorhaben Stufe, Auftraggeber, Anwendervertreter, Freigabe/Abnahme/Abschluss und Ampel, dazu
  Vorlagen fuer Projektauftrag und Abnahmeprotokoll. Rollen mit „Vorschlag" sind nicht bestaetigt.
  Neu aufgenommen: PM-10 Abloesung Smartsheet, PM-11 Data-Lake-Leistung, PM-12 Power-BI-
  Anwendervertreter. Am selben Tag hat Codex das Finance-Dashboard geprueft; alle Befunde sind
  gegen den Code bestaetigt (`docs/FINANCE_REVIEW_2026-09-29.md`, `ISS-018`), repariert ist
  noch nichts. *(Erledigt am 2026-09-29, `ISS-020`, und nicht mehr dringend; Vermerk vom 2026-11-12:)* **Dringend:** Der Spanien-Import steht seit Mitte Juli, produktiv kommen keine
  neuen TRES-Zeilen an (`ISS-020`). Ursache bestaetigt (SharePoint-Liste las nur 200 Eintraege),
  Korrektur `60cc569` seit 09:25 produktiv, Nachimport beim naechsten Lauf pruefen. Railway-Gesamtsicht
  und DB4 fuer Heiko und Patrick als `ISS-019`: Umsatz-Gesamtsicht am Nachmittag geliefert und
  plausibilisiert, bestaetigt sind erst DE und ein Kunde in Indien. Die Markdown-Konsistenzbefunde von
  Codex sind behoben. Mail und Teams sind seit 29.09. direkt durchsuchbar; Funde in `docs/HR_KPI.md` 8.3
  und `docs/FINANCE_JOURNAL_KONSOLIDIERUNG_ANDREAS_2026-09-08.md` Abschnitt 9.

- **2026-09-28, nach Ingos Ferien:** Tempo der Webapp (groesster Kritikpunkt) produktiv verbessert:
  Oberflaeche erscheint sofort, Management-Cockpit lokal 41 s -> 3,3 s, Export-Dashboard 2,3 s ->
  0,07 s, Server-Einkaufsberechnung 105 s -> 24 s durch SQLite-Cache. Einkauf-Lauf fiel vom 10.09. bis
  28.09. wegen IIS-Leerlauf aus, ist per Selbstaufruf und Nachhol-Delta abgesichert und lief am 28.09.
  wieder; App-Pool-Dauerbetrieb offen als ISS-017 bei der IT. HR-Antworten von Sonja umgesetzt.
  Lagerwert-Verlauf je Woche produktiv. Finance_All-Nachtlauf weiterhin unfertig und nicht
  ausgeliefert. Details: `projektmanagement/PROJEKTSTATUS.md` PM-04, PM-07, PM-09.

- ZZPRDAT (Produktionsdatum im Fertigungsauftrag) ist gebaut und auf sieben Wegen
  nachgetestet: Paket `ZPP1`, Transportauftrag `T76K912490`, bewusst **nicht
  freigegeben**, in P76 nichts geaendert. **Am 2026-09-07 ist ein Konstruktionsfehler
  gefunden und am selben Tag behoben worden:** `BEFORE_UPDATE` lief bei jedem Sichern und
  fuellte dadurch laengst freigegebene Altauftraege mit dem heutigen Eckendtermin. Jetzt
  wird `AFKO-FTRMI` vorher gelesen; war der Auftrag schon freigegeben, passiert nichts.
  Am 2026-09-07 sind **alle sieben Wege plus der Altauftragsfall und Write-once** auf dem
  korrigierten Stand nachgemessen, direkt aus `AUFK` statt ueber den Nachweisreport, der den
  Fehler verdeckt haette; die Klasse liegt in der Aufgabe `T76K912491`. Auch der
  Diagnosebericht `Z_ZZPRDAT_CHECK` ist am 07.09.2026 korrigiert, in T76 aktiviert und
  geprueft: sachliche Momentaufnahme statt Write-once-Behauptung, Werk-/Leerfilter vor
  Trefferlimit, kein unbeschraenkter Lauf mit `p_max <= 0`. Offen bleiben das Absenden
  des Anschreibens und die fachliche Abnahme inklusive des realen Druckablaufs gegenueber
  V2. Befund und Messungen: `saptasks/ZZPRDAT_TRANSPORTPLAN.md` Abschnitt 6a. Einstieg bleibt
  dieselbe Datei. **Stand 2026-10-12:** Der Abnahmetest (geplant 12.10. 16:00) ist um zwei
  Wochen verschoben, voraussichtlich Woche ab 26.10.2026; Technik in T76 unverändert.
  **Stand 2026-10-26:** live bestätigt, Quelltext identisch mit dem Repo, Transporte offen;
  sieben Testaufträge der Benutzer TAN/FPA (1241831 bis 1241844) passen zum
  Sollverhalten, Einzelheiten im Transportplan. Ergebnis des Abnahmetests noch offen.
  **Stand 2026-11-06:** Bei der Abnahme zeigte der Reiter „TRAFAG Daten“ das Datum nicht
  (`ZXCO1U11` auskommentiert in Georgs offenem Auftrag); behoben in T76, und alle
  ZZPRDAT-Objekte liegen jetzt in `T76K912490`: Aufgabe `T76K912491` (BAdI-Loesung) und
  Aufgabe `T76K912852` (`ZXCO1U11`, `ZXCO1O01`, `ZXCO1I01`, `ZXCO1U12`, `ZXCO1TOP`, `ZXCO1U06`,
  Bild `SAPLXCO1 0100`; `T76K912851` darin verschmolzen). Mit `ZXCO1U06` geht ein zweiter
  Schreibweg auf `ZZPRDAT` mit (Entscheid Ingo/Georg offen). Weiter nicht freigegeben;
  Transportplan Abschnitt 1, Nachtrag 2026-11-06.

- *(Historischer Messstand vom 31.07., kein heutiger Zustand; der heutige Stand der Lieferantenfelder steht
  unter `ISS-003` im Issue-Log. Vermerk vom 2026-11-12.)*
  LIVE-PRUEFUNG 2026-07-31: UK-2025 ist produktiv vorhanden (1'867 Zeilen);
  `GroupStandardCosts` ist mit 63'506 TR-AG-Werten gefuellt; Supplier bleibt bei
  CH/AT/DE/ES komplett leer. Einkauf-Delta-Fix ist deployed, aber zum
  Pruefzeitpunkt gab es noch keinen produktiven Delta-Lauf nach dem Deploy.
  Details und genaue Vorrangregel:
  `docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md`. *(Ueberholt: Die Datei liefert nur noch die Messdetails vom
  Juli; die Vorrangregel steht in `router.md` Regel 1.)*

- Fuehrende App: `TrafagSalesExporter`, publiziert als `BiDashboard`.
- *Ueberholt seit 2026-10-09:* die einfache Projektverwaltung ist durch **Trafag Projekte** ersetzt
  (Board, Backlog/Sprints, Liste, Zeitachse, Kalender, Diagramme; Doku `docs/TRAFAG_PROJEKTE_2026-10-09.md`).
  Frueherer Stand: Die einfache Projektverwaltung ist als unterster Hauptnavigationseintrag
  `Projekte` unter `/projekte` eingebaut. Sie verwaltet Status, Prioritaet,
  Verantwortung, Termine, Fortschritt, Notizen und Archivierung in SQLite.
  Aktueller Funktions- und Lokalisierungsstand:
  `docs/EINKAUF_LOKALISIERUNG_PROJEKTSUITE_2026-08-01.md`.
- Aktuelle fachliche Detailstaende und offene Punkte stehen ausschliesslich in
  den Themen-Kurzdateien und im kanonischen Live-Abgleich *(ueberholt, Vermerk vom 2026-11-12: Der
  Juli-Abgleich ist nur noch historische Messreferenz, siehe Kopf dieser Datei. Der heutige Stand steht in den
  Themen-Kurzdateien und in den Statusquellen, `router.md` Zeile „Wo stehen wir gerade?“)*; alte Deploy-Nachweise
  stehen in `lastchange.md` beziehungsweise `docs/archiv/` (seit 2026-10-22).
- Management-/Roadmap-Doku neu: `docs/INGO_TODOS_180_TAGE_2026-06-18.docx`, Quelle `docs/INGO_TODOS_180_TAGE_2026-06-18.md`. Sie beschreibt Ingos 180-Tage-Fokus: Sales Management Cockpit/Data-Lake als Prioritaet 1, HR Dashboard und Einkaufs Dashboard als Prioritaet 2/3, Q3/Q4-Meilensteine, Abhaengigkeiten, Risiken und naechste Schritte.
- Abgrenzung fuer 180 Tage: S/4HANA Compatibility Check/RPC-/RFC-Themen bleiben bei Lucas; Infrastruktur/Security/Server/Netzwerk bleiben bei Alex/Ramon/Upgreat. Ingo bleibt bei Analytics, BI, Reporting-/Z-Funktionsbezug und .NET/ASP-Webseiten.
- Wichtig DE/Sparten: Alphaplan `ArtikelNummer` wird als lokale Materialnummer importiert, aber nicht als garantiert identische TR-AG-/SAP-`MATNR` normalisiert. Nicht gematchte Nummern erscheinen weiterhin als `Nicht im TR-AG-Stamm`.
- Aktuelle Finance-Schulung: `docs/FINANCE_SCHULUNG_FINANZ_2026-06-11.md` mit Prozessgrafiken fuer Exportfluss, Audit-CSV-Auswertungsquelle und Waehrungsumrechnung.
- India/TRIN: produktive Server-DB steht auf `TRIN -> SAGE -> 20.197.20.60:30015`, Schema `TRAFAG_LIVE`, User-Override `TRAFAGCONTROLS`.
- Fuer normale Weiterarbeit diese Datei plus den passenden Themen-RAG laden.

## Aktive Themen

- Finance Cockpit: `docs/rag/FINANCE.md`
- Manual Import: `docs/rag/MANUAL_IMPORT.md`
- Produktmapping: `docs/rag/PRODUCT_MAPPING.md`
- HR KPI: `docs/rag/HR_KPI.md`
- Deployment/IIS: `docs/rag/DEPLOYMENT.md`
- Admin/Startseite: `docs/rag/ADMIN.md`
- Einkauf: `docs/rag/PURCHASING.md`
- 180-Tage-Roadmap Ingo: `docs/INGO_TODOS_180_TAGE_2026-06-18.md`

## Rohquellen Nur Bei Bedarf

- kanonische Detailhistorie `HISTORY_CANONICAL.md.raw` und Originaldateien `original_history_raws.zip`:
  seit `835b317` (2026-09-01) nur noch in Git, Entscheid Ingo 2026-10-22 nicht wiederherstellen;
  Abruf und Inhalt in `docs/archiv/README.md`. Neue Archive seit 2026-10-22 unter `docs/archiv/`
- Dokumentstatus: `baum.md`
