# Last Change

Stand: 2026-07-22

Diese Datei ist fuer tokenarme RAG-Nutzung komprimiert.

## Aktueller Kurzstand

- UI 2026-07-23, `268/268` Tests gruen: Erste neue Einkauf-Sicht gebaut - Balkenblock "Volumen
  nach Beschaffungsregion" (Lieferantenland LFA1.Land1 -> EKKO.SupplierCountry) im Spend-Reiter,
  neben "Volumen nach Warengruppe". Der Zusatz-Chart-Bereich von `PurchasingSection` ist von einem
  einzelnen Zweitchart auf eine generische Liste `ExtraCharts` (Model `PurchasingSectionExtraChart`)
  umgestellt, damit ABC/XYZ als weitere Bloecke sauber danebenpassen (Marcos "eine Sicht nach der
  anderen"). Neue Aggregation `RegionSpendRows` im `PurchasingDashboardService` (gleicher Filter/
  Zeitraum wie WG/Lieferant). Region-Werte fuellen sich erst mit dem naechsten Einkauf-Full-Load
  (SupplierCountry-Spalte noch leer). VknrDispo jetzt live bestaetigt (SEGW-Property angelegt +
  generiert, liefert `019`) - Datenvoraussetzung fuer den Produktgruppen-Aufriss steht, ZC23-
  Zuordnung noch offen. Details: `docs/PURCHASING_DASHBOARD_WUENSCHE_EINKAUF_2026-07-23.md`.
- SAP-FELDER + C#-LADESTRECKE 2026-07-23, `268/268` Tests gruen: Ingo hat weitere SAP-Felder
  transportiert (LFA1.Land1, MARC.Maabc, neues Set ZSTR_MAT_XYZSet fuer XYZ), alle live gegen
  travp762 verifiziert. C#-Ladestrecke (`PurchasingDataRefreshService`) angepasst: liest jetzt
  Lieferantenland (LFA1.Land1 -> neue Cache-Spalte `SupplierCountry`), ABC (MARCSet.Maabc ->
  `MaraAbc`) und XYZ (`ZSTR_MAT_XYZSet.Maxyz` -> `MaraXyz`). WICHTIG MARCSet: ignoriert
  $top/$skip/$filter (wie maracalc), deshalb EIN ungepagter Request + client-seitiger
  Werk-1100-Filter; XYZ-Set (eigener Methodenrumpf `docs/abap/ZSTR_MAT_XYZ_GET_ENTITYSET.abap`,
  von mir gebaut) pagt korrekt. XYZ-Quelle war Marcos „ITSCH-MAT-ABC-XYZ" = Tabelle
  `ZCA_MAT_ABC_XYZ`, Feld `/ITS/CA_M_MAXYZ` (XYZ ist KEIN SAP-Standard, nur ABC). Datenlage Werk
  1100: Land gefuellt; ABC 86 % leer (A/B/C echt vorhanden); XYZ-Set 4'388 Materialien, 99 %
  klassifiziert. Neue Cache-Spalten additiv; FUELLEN sich erst mit dem naechsten Einkauf-Full-Load
  (mit Marco/Andreas abstimmen). UI/Visuals bewusst noch NICHT gebaut (Marco: Sicht fuer Sicht).
  OFFEN: `VknrDispo` (Produktgruppen-Aufriss) - SE11-Struktur `ZSTR_LZCODE_USAGE` braucht noch das
  Feld `VKNR_DISPO` (DE `DISPO`), dann ZLO03-USAG-Rumpf erneut aktivieren. Details:
  `docs/PURCHASING_DASHBOARD_WUENSCHE_EINKAUF_2026-07-23.md`.
- SAP-ERWEITERUNG + LOADER-UMSTELLUNG 2026-07-23, `268/268` Tests gruen: Ingo hat `Matkl`
  (Materialstamm-Warengruppe) UND `Mstae` beide ins `MARA001Set` aufgenommen. Damit hat EIN Set
  wieder alle vom Einkauf-Loader benoetigten Materialstamm-Felder (Matnr+Mstae+Matkl); der Loader
  (`PurchasingDataRefreshService.LoadMaterialStatusMapAsync`) ist von `maracalcSet` zurueck auf
  `MARA001Set` umgestellt (`$select=Matnr,Mstae,Matkl`, ein ungepagter Request - MARA001Set
  ignoriert $top/$skip/$filter, liefert immer alle 68'125 Zeilen, live verifiziert). Vorgeschichte:
  bis 17.07. MARA001Set (hatte Mstae), 17.07. auf maracalcSet gewechselt (MARA001Set hatte Mstae
  verloren, aber kein Matkl), jetzt beide Felder in MARA001Set -> zurueck. LIVE VERIFIZIERT gegen
  travp762: MARA001Set `$select=Matnr,Mstae,Matkl` -> 200; Mstae 48,8 % mit Status (41 % `99`,
  2,4 % `98`) - MSTAE-98/99-Filter wirkt weiter; Matkl 35 % gefuellt, davon viel `01`, ~10 % echte
  Gruppen (65 % leer -> COALESCE-Fallback auf Beleg-WG). EKKO/EKPO-Loaderfelder
  (Bstyp/Bsart/Konnr/Elikz/Matkl) auf travp762 ebenfalls vorhanden -> Full Load laeuft durch (der
  2026-07-10-Blocker ist weg). NACHSORGE: `MaraMatkl` im Cache ist noch 0 % (Load-Stand 17.07.);
  wird erst mit dem naechsten Einkauf-Full-Load gefuellt - der ist mit Marco/Andreas abzustimmen
  (laufende 18-Mio-Abnahme, Datenbestand wechselt auf travp762). Details: `docs/rag/PURCHASING.md`.
- APP-AENDERUNG 2026-07-23, `268/268` Tests gruen (JETZT deployed, siehe Deploy-Eintrag): Einkauf-Reiter `Spend`
  hat ein zweites Balkendiagramm "Volumen nach Warengruppe" (PowerBI-Seite "Diagramm Vol./WG").
  Anlass: Ingo-Analyse der `li.pbix`/`x.pbix` (beide identisch, 7 Seiten) - das WG-Diagramm war in
  der App nicht als echtes Visual vorhanden, WG lebte nur als Drilldown-Ebene der Spend-Matrix.
  Bewusst im Spend-Reiter platziert (Volumenanalyse), nicht bei Lieferanten (Bewertung). Umsetzung
  rein C#/Razor: neue Aggregation `MaterialGroupSpendRows` in `PurchasingDashboardService`
  (COALESCE(MaraMatkl,Matkl,'ohne WG'), gleicher Filter/Zeitraum wie Lieferant-Matrix), zweiter
  optionaler Chart-Block in `PurchasingSection.razor`, verdrahtet im Spend-case von
  `PurchasingDashboard.razor` mit ehrlichem Datenhinweis. WICHTIGER BEFUND (an Prod-Cache
  gemessen, Load-Stand 17.07.): WG faktisch unbrauchbar bis SAP-Erweiterung - `MaraMatkl` 0 %
  gefuellt, `Matkl` zu 99,6 % in Sammelgruppe `01`; Diagramm zeigt daher aktuell fast nur eine
  Saeule (strukturell korrekt, aussagekraeftig erst nach `Matkl` im `maracalcSet`). BEWUSST NICHT
  nachgebaut aus PowerBI: Kuchen Lieferant (durch Top-Lieferanten-Balken abgedeckt), Kuchen Region
  (Lieferantenland fehlt im Cache - LFA1 laedt nur Name1, nicht Land1). Details:
  `docs/rag/PURCHASING.md`.
- ROOTCAUSE + FIX 2026-07-23, `268/268` Tests gruen: Numerische Materialnummern (z.B. `2217`)
  lieferten in der Stuecklistenanalyse IMMER 0 Zeilen, alphanumerische (`D15019`) gingen. Per
  SapProbe/RFC gegen travp762 (mit Ingos Prod-Passwort) + OData-Testbatterie eingegrenzt: MARA hat
  `000000000000002217` mit LEEREM LVORM (die 22d-Loeschvormerkungs-Theorie WAR FALSCH),
  ZPOWERBI_VC_TXT hat die Zeilen mit gefuellter Menge. Auch include_deleted (LVORM-Filter aus) gab
  0 -> Schritt 1 (SELECT FROM mara) fand die numerische Nummer nicht, weil
  CONVERSION_EXIT_ALPHA_INPUT (22c) sie NICHT zuverlaessig zero-paddete (zerstoerte sogar die
  bereits gepaddete Eingabe). FIX doppelt abgesichert: (1) C#
  (`MaterialUsageDataRefreshService.NormalizeMaterialToken`) paddet rein numerische Nummern vor dem
  $filter auf 18 Stellen; (2) ABAP (beide Methodenruempfe) nimmt den Rohwert IMMER in die RANGE auf
  (App schickt gepaddet -> sicherer Treffer) plus zusaetzlich die MATN1-Form fuer kurze manuelle
  Eingaben (CONVERSION_EXIT_MATN1_INPUT statt ALPHA). C#-Seite deployt (siehe Deploy-Eintrag
  unten); ABAP muss erneut auf travt762 UND travp762 eingefuegt/aktiviert werden. Details:
  `docs/abap/README_LZCODE_WEBSERVICE.md` Nachtrag 2026-07-23. NEBENBEFUND: MAKTX kommt beim
  Service-User (POWERBI) leer zurueck (sprachabhaengiger MAKT-Join) - Zeile wird trotzdem
  ausgegeben (22b-Haertung greift), nur der Text fehlt; fachlich unkritisch, spaeter ggf.
  sy-langu-unabhaengig lesen.
- DEPLOYED 2026-07-22 (Commit `bacc614 Add option to include deletion-flagged materials in BOM
  analysis`, `267/267` Tests gruen, DLL `22.07.2026 14:26:01`, Laenge `3'076'096`, Port 443
  erreichbar, DB unveraendert): Loeschvorgemerkte Materialien optional einbeziehbar ist live
  (siehe Eintrag direkt darunter fuer Details). Publish nach
  `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\` via `dotnet publish -c Release
  -p:PublishProfile=FolderProfile`, `app_offline.htm` gesetzt/entfernt. NICHT Teil dieses Deploys:
  der ABAP-Fix (Richtung-Suffix ALLE, LVORM-Bypass) - Methodenrumpf liegt bereit und muss
  weiterhin manuell in SE80 auf travt762 UND travp762 eingefuegt/aktiviert werden.
- APP-AENDERUNG 2026-07-22, `267/267` Tests gruen (JETZT deployed, siehe Eintrag oben):
  Loeschvorgemerkte
  Materialien optional einbeziehbar (Wunsch Ingo, nach Live-Diagnose mit den Test-Nummern `1689,
  2163, 2217, 2286, 2366, 2367, 2434, 2537`). Live-Diagnose mit denselben Service-Credentials wie
  die App zeigte: Top-Down fuer "normales" Material (`D15019`) funktioniert, Bottom-Up fuer
  `Kompnr=C34882` findet `Vknr=2217` mit echten Daten, aber Top-Down fuer `Vknr=2217` (Kurz- UND
  Langform) liefert weiterhin 0 Zeilen - Ursache: Schritt 1 (MARA-Selektion) laesst per Default
  nur `LVORM = ' '` zu (wie Report-Default `p_lvorm=' '`), die Testnummern sind offenbar alte,
  loeschvorgemerkte Kopfmaterialien. FIX: `Richtung`-Wert akzeptiert jetzt Suffix `ALLE`
  (`TOPDOWNALLE`/`BOTTOMUPALLE`, ohne DDIC-Aenderung), neue Checkbox "Auch geloeschte Materialien"
  in `Components/Pages/BomAnalysis.razor`, neuer Parameter `includeDeleted` in
  `MaterialUsageDataRefreshService.RunFullLoadAsync`, 2 neue Tests fuer `BuildRichtungValue`.
  NACHARBEIT SAP (wie gehabt): Methodenrumpf `ZSTR_LZCODE_USAG_GET_ENTITYSET.abap` erneut auf
  travt762 UND travp762 einfuegen, aktivieren, `/IWFND/CACHE_CLEANUP`. Details:
  `docs/abap/README_LZCODE_WEBSERVICE.md` Nachtrag 2026-07-22d.
- DEPLOYED 2026-07-22 (Commit `7d061d9 Support material number ranges (35-40) in BOM analysis
  material filter`, `265/265` Tests gruen, DLL `22.07.2026 13:22:34`, Laenge `3'075'584`, Port 443
  erreichbar, DB unveraendert): Bereichs-Syntax `35-40` im Materialfeld der Stuecklistenanalyse
  (siehe Eintrag direkt darunter fuer Details) ist live. Publish nach
  `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\` via `dotnet publish -c Release
  -p:PublishProfile=FolderProfile`, `app_offline.htm` gesetzt/entfernt. NICHT Teil dieses Deploys:
  die ABAP-Fixes (ALPHA-Konvertierung, ZPOWERBI_VC_TXT-Quelltabelle) - die liegen als
  Methodenruempfe in `docs/abap/` bereit und muessen weiterhin manuell in SE80 auf travt762 UND
  travp762 eingefuegt/aktiviert werden (siehe zwei Eintraege unten).
- APP-AENDERUNG 2026-07-22, `265/265` Tests gruen (JETZT deployed, siehe Eintrag oben): Neue
  Bereichs-Syntax im Materialfeld der Stuecklistenanalyse (`Components/Pages/BomAnalysis.razor`,
  Wunsch Ingo): `35-40` neben kommagetrennten Einzelwerten. Rein C#-seitig in neuer, public
  static Methode `MaterialUsageDataRefreshService.BuildMaterialClause` (5 neue Tests) -
  Bereichs-Token werden zu `(Vknr ge 'X' and Vknr le 'Y')`, gemischt mit Einzelwerten per `or`
  verknuepft. Keine ABAP-Aenderung noetig, siehe `docs/abap/README_LZCODE_WEBSERVICE.md` Nachtrag.
- ROOTCAUSE + ABAP-FIX 2026-07-22c (kein App-Deploy; SAP-Nacharbeit durch Ingo/Lucas noetig,
  BESTAETIGTE Ursache): Zweiter Full-Load-Test gegen travp762 (nach dem ZPOWERBI_VC_TXT-Fix,
  siehe Eintrag darunter) lief technisch durch, lieferte aber fuer `Vknr=2217`/TOPDOWN 0 Zeilen.
  Ingo hat die Ursache selbst durch einen direkten Browser-Vergleichstest zweifelsfrei belegt:
  `$filter=... Vknr eq '2217'` (Kurzform) = 0 Treffer, `$filter=... Vknr eq
  '000000000000002217'` (18-stellig) = echte Treffer. Grund: `MARA`/`ZPOWERBI_VC_TXT` speichern
  Materialnummern intern padded; eine SELBSTGESCHRIEBENE GET_ENTITYSET-Methode bekommt
  `it_filter_select_options` aber ROH, die sonst automatische externe->interne
  ALPHA-Konvertierung des Gateway-Frameworks greift bei eigenem Code nicht. Produktionslog
  bestaetigte zusaetzlich: der App-Full-Load hatte exakt denselben unpadded Wert wie der erste
  fehlgeschlagene manuelle Test verwendet - kein Padding-Bug im C#-Code (der reicht Werte
  unveraendert durch, das ist korrekt so). FIX: Beide Methoden
  (`docs/abap/ZSTR_LZCODE_USAG_GET_ENTITYSET.abap`, `docs/abap/ZSTR_LZCODE_PARE_GET_ENTITYSET.abap`)
  konvertieren Low/High-Werte der Vknr/Kompnr-Filter jetzt per `CONVERSION_EXIT_ALPHA_INPUT`, bevor
  sie in die RANGE-Tabellen wandern - damit funktionieren Kurz- UND Langform gleichermassen.
  ZUSAETZLICHE HAERTUNG (Version 2026-07-22b, unabhaengiger Befund, weiterhin gueltig): der aus
  dem Report uebernommene Zeilen-Drop bei fehlendem MAKTX (`DELETE gt_ktab WHERE maktx IS
  INITIAL`) ist entfernt, weil die MAKT-Textsuche sprachabhaengig (`sy-langu`) ist und fuer einen
  Webservice keine Zeilen mit echten Bestandsdaten wegen einer fehlenden Uebersetzung verschwinden
  sollten (die urspruengliche Vermutung, DAS sei die Ursache des 0-Zeilen-Symptoms, war falsch und
  ist durch den ALPHA-Befund widerlegt - die Haertung bleibt trotzdem sinnvoll). NACHARBEIT SAP
  (wie beim vorigen Fix): Methodenruempfe erneut auf travt762 UND travp762 einfuegen, Klasse
  aktivieren, `/IWFND/CACHE_CLEANUP`. Details: `docs/abap/README_LZCODE_WEBSERVICE.md` Nachtrag
  2026-07-22c.
- ROOTCAUSE + ABAP-FIX 2026-07-22 (kein App-Deploy; SAP-Nacharbeit durch Ingo/Lucas noetig):
  Nach dem travp762-Wechsel brachen ALLE EntitySets von `ZPOWERBI_EINKAUF_SRV` auf PROD mit
  `SYNTAX_ERROR` ab (Logistik-Full-Load UND Einkauf-Full-Load `EKKOSet`; Einkauf-Cache blieb dank
  Guardrail unveraendert auf dem Stand 2026-07-17). URSACHE (von Ingo identifiziert): Die
  DPC_EXT-Methodenruempfe vom 2026-07-21 basierten auf einer ALTEN ZLO03-Fassung und lasen aus
  `ZAT_VC` — diese Tabelle existiert auf travp762 nicht, dadurch kompilierte die komplette
  DPC_EXT-Klasse nicht und riss den ganzen Service mit (deshalb auch EKKOSet betroffen). Die
  aktuelle Reportfassung liegt seit 2026-07-22 als `docs/abap/originalzlo03.txt` vor und liest aus
  `ZPOWERBI_VC_TXT`. FIX: Beide Methodenruempfe (`docs/abap/ZSTR_LZCODE_USAG_GET_ENTITYSET.abap`,
  `docs/abap/ZSTR_LZCODE_PARE_GET_ENTITYSET.abap`) auf die neue Fassung umgeschrieben —
  Quelltabelle `ZPOWERBI_VC_TXT`, plus Report-FIXES uebernommen: FIX 1 (keine Mengen-Rundung auf
  0 Dezimalen mehr), FIX 2 (Mehrfachverwendungen summieren statt deduplizieren, deterministisch
  ueber SORTED TABLE), FIX 4 (Textpositionen `postyp='T'` und Zeilen ohne MAKTX im Default raus),
  neue Baugruppen-Logik (`(VC-Baugruppe ODER MAST) UND beskz<>'F'`), Stammdaten-JOIN ohne
  LVORM-Filter. DDIC-Strukturen und C#-Seite unveraendert. NACHARBEIT (manuell): Ruempfe auf
  travt762 UND travp762 einfuegen, Klasse aktivieren, `/IWFND/CACHE_CLEANUP`; erst danach sind
  Einkauf- und Logistik-Loads gegen P wieder moeglich. Details:
  `docs/abap/README_LZCODE_WEBSERVICE.md` Nachtrag 2026-07-22.
- KONFIGURATION GEAENDERT 2026-07-22 (kein Deploy, reine DB-Konfiguration): Zentrale SAP-URL
  (`SourceSystemDefinitions.CentralServiceUrl`, Code `SAP`) von `travt762` (TEST) auf `travp762`
  (PROD) umgestellt — Anlass: Ingo wollte die neue Logistik/Stuecklistenanalyse (ZLO03-Webservice,
  siehe Eintrag darunter) mit echten Daten pruefen, `ZAT_VC` ist auf travt762 leer. Vor der
  Aenderung per Live-Query verifiziert (nicht wie in `docs/PURCHASING_DASHBOARD_VORBEREITUNG_INGO_2026-07-09.md`
  A0 angenommen "wirkt fuer alle SAP-Bereiche"): `Sites` fuer `ZSCHWEIZ` hat einen EIGENEN, bereits
  explizit gesetzten Override (`SapServiceUrl` = travt762) und ist von der zentralen Aenderung
  NICHT betroffen — Finance CH/AT bleibt unveraendert auf travt762 (TEST). Betroffen ist NUR die
  Site `PURCHASING_SAP` (kein eigener Override), die sowohl vom Einkauf-Dashboard als auch von der
  neuen Logistik/Stuecklistenanalyse gemeinsam genutzt wird — beide zeigen ab sofort auf travp762
  (PROD). Aenderung gezielt nur auf `SourceSystemDefinitions WHERE Code='SAP'` beschraenkt (kein
  Touch von `Sites`), per kleinem C#/Microsoft.Data.Sqlite-Skript direkt gegen die Produktions-DB
  (`\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\trafag_exporter.db`), analog dem
  bestehenden `spartenlogic/.tmp_update_sap_url`-Muster. Vorher Backup gezogen:
  `trafag_exporter.db.before-travp762-purchasing-switch-20260722.bak` (gleiche Konvention wie die
  bestehenden `.before-*`-Sicherungen). NACHSORGE/OFFENE RISIKEN (aus
  `docs/rag/PURCHASING.md` bereits bekannt, jetzt wirksam): (1) Einkauf-Cache enthaelt noch
  Testdaten -> Full Load noetig, sonst alte Zahlen im Spend-Reiter waehrend Marcos laufender
  18-Mio-Abnahme — mit Marco/Andreas abstimmen, bevor der naechste Full Load gefahren wird. (2)
  Direkter Basic-Auth-Test gegen travp762 gab zuletzt `HTTP 401` (Stand 2026-07-09, Status seither
  nicht erneut geprueft) — falls das weiterhin besteht, schlagen jetzt Einkauf- UND
  Logistik-Loads fehl. (3) `Bstyp`/`Bsart`/`Elikz` fehlten auf travp762 zuletzt im OData-Modell
  (Probe 2026-07-10) — betrifft die Stuecklistenanalyse nicht direkt (anderes EntitySet), verzerrt
  aber ggf. Einkauf-Zahlen (Beleg-Mix-Trennung), bis SAP das P-Modell nachzieht.
- DEPLOYED 2026-07-21 (Commit `a314881 Add ZLO03 BOM-analysis webservice: SAP entity methods,
  C# loader, Logistik tab`, `260/260` Tests gruen, DLL `21.07.2026 15:04:46`, Laenge
  `3'075'072`, Port 443 erreichbar, DB unveraendert `14:50:23`): NEUER ROOT-REITER **LOGISTIK**
  (Icon LocalShipping) mit Unterpunkt **Stuecklistenanalyse** (`Components/Pages/BomAnalysis.razor`,
  `/logistik/stuecklistenanalyse`, Seed-Keys `logistics`/`logistics-bom-analysis`) — macht den
  SAP-Report `ZM_LZCODE20_OPT` (Top-Down/Bottom-Up-Stuecklistenanalyse, bisher nur als
  Excel-Download) per Webservice ansprechbar. SAP-Seite: zwei neue OData-EntitySets am
  bestehenden Gateway-Service `ZPOWERBI_EINKAUF_SRV`, angelegt als DPC_EXT-Methodenruempfe
  OHNE eigene Klasse (`ZSTR_LZCODE_USAG_GET_ENTITYSET`/`ZSTR_LZCODE_PARE_GET_ENTITYSET`, beide
  am 2026-07-21 in SEGW fehlerfrei aktiviert) auf zwei neuen, feldweise verifizierten
  DDIC-Strukturen (`ZSTR_LZCODE_USAGE`/`ZSTR_LZCODE_PARENT`) — normalisiertes Zeilenmodell statt
  der dynamischen Pivot-Matrix des Reports, behebt dabei den in
  `docs/INGO_TODOS_180_TAGE_2026-06-18.md` genannten Nichtdeterminismus (HASHED-TABLE-Reihenfolge
  ohne SORT in `FORM get_elternmaterial`). C#-Seite (`MaterialUsageDataRefreshService`) loest die
  EntitySet-Namen dynamisch auf (SEGW hat nach den Strukturen benannt, nicht wie urspruenglich
  vorgeschlagen `MaterialUsageSet`) und schickt SAP-seitig erzwungene Materialfilter (Catch-all
  oder gezielte Liste aus der neuen UI). Live-Verifikation gegen T76/travt762 per SapProbe (RFC)
  bestaetigte vorab alle offenen Fachannahmen (`KOM_MSTAE` ist ein MATNR-Feld, `ZZLZCOD`/
  `ZZLZCODSORT` haben echte Datenelemente, `ZAT_VC`/`ZMD04_CALC` lesbar). GEGEN TEST SIND 0
  ZEILEN ERWARTET (ZAT_VC auf travt762 leer, echte Daten liegen auf travp762/PROD — bekannter
  travt/travp-Punkt aus `docs/rag/PURCHASING.md`, hier nicht angefasst). Perspektivisch auch fuer
  den Einkauf nutzbar (Exklusivitaet/Bestaende je Komponente), startet aber bewusst als eigener
  Reiter. Details: `docs/abap/README_LZCODE_WEBSERVICE.md`.
- ENTWURF + LIVE-VERIFIKATION 2026-07-21, EINKAUF/PRODUKTMAPPING (kein Deploy, Code noch NICHT
  committet, ausser SapProbe): Ingo bat darum, den Report `ZM_LZCODE20_OPT`/`zlo03.txt`
  (Top-Down/Bottom-Up-Stuecklistenanalyse) wie andere SAP-Tabellen per Webservice ansprechbar zu
  machen. Ergebnis ist ein Entwurfspaket fuer Lucas/SAP-Team (bewusst nicht produktiv, technische
  SAP-Anlage bleibt gemaess Abgrenzung in `docs/INGO_TODOS_180_TAGE_2026-06-18.md` beim SAP-Team):
  (1) Spezifikation `docs/abap/README_LZCODE_WEBSERVICE.md` mit normalisiertem Zeilenmodell
  `MaterialUsageSet`/`MaterialParentSet` (statt der dynamischen Pivot-Matrix des Reports) fuer den
  bestehenden Gateway-Service `ZPOWERBI_EINKAUF_SRV`. (2) Zwei ABAP-Klassenentwuerfe
  `ZCL_LZCODE_PROVIDER.abap` (mit privaten Hilfsmethoden) und `ZCL_LZCODE_PROVIDER_INLINE.abap`
  (gleiche Logik komplett in `GET_DATA` inline, falls nur eine DPC-Methode redefiniert werden
  soll) — behebt dabei den in den Ingo-Todos genannten Nichtdeterminismus (`FORM
  get_elternmaterial` haengt eine `HASHED TABLE` ohne `SORT` durch, Reihenfolge nicht definiert).
  (3) C#-Konsument `Services/MaterialUsageDataRefreshService.cs` + Schema
  (`MaterialUsageCache`/`MaterialParentCache`/`MaterialUsageSyncState`) + 2 Tests, analog
  `PurchasingDataRefreshService` — prueft EntitySet-Existenz vor dem Laden und meldet fachlich
  klar, wenn die SAP-Seite noch fehlt (kein Absturz). NACHTRAG spaeter am 2026-07-21: SAP-Seite
  ist inzwischen KOMPLETT angelegt (beide SE11-Strukturen feldweise verifiziert, beide
  DPC_EXT-Methoden `ZSTR_LZCODE_USAG/PARE_GET_ENTITYSET` fehlerfrei aktiviert — Variante 3 ohne
  eigene Klasse, Methodenruempfe in `docs/abap/ZSTR_LZCODE_*_GET_ENTITYSET.abap`). C#-Seite
  daran angepasst: EntitySet-Namen werden dynamisch aufgeloest (`ResolveEntitySetName`, SEGW hat
  nach Strukturnamen benannt), Property-Keys unterstrich-tolerant, Full Load schickt
  Guard-konforme Filter (`Vknr gt ''` Catch-all bzw. optionale Materialliste). UI seit
  2026-07-21 (Entscheid Ingo): neuer Root-Reiter LOGISTIK (Icon LocalShipping) mit Unterpunkt
  STUECKLISTENANALYSE (`Components/Pages/BomAnalysis.razor`, `/logistik/stuecklistenanalyse`,
  Seed `logistics`/`logistics-bom-analysis`) — SAP-Load mit Richtungs-Schalter und
  Materialfilter, Statusanzeige, durchsuchbare Cache-Vorschau. Daten sollen spaeter auch im
  Einkauf nutzbar sein, starten aber als eigener Reiter. `260/260` Tests gruen. INZWISCHEN
  committet und deployed, siehe Eintrag ganz oben (Commit `a314881`, 2026-07-21). Ende-zu-Ende
  gegen TEST liefert erwartungsgemaess 0 Zeilen (ZAT_VC dort leer), echter Datentest erst nach
  travt/travp-Umstellung. (4) LIVE-VERIFIKATION gegen `T76`/`travt762` (TEST) per `SapProbe`
  bestaetigt alle offenen Fachannahmen: `ZAT_VC-KOM_MSTAE` ist trotz irrefuehrenden Namens ein
  MATNR-Feld (Elternmaterial-Mapping korrekt), `MARA-ZZLZCOD`/`ZZLZCODSORT` haben echte
  Datenelemente (`CHAR 4`, keine PAPH1-Falle), `ZAT_VC`/`ZMD04_CALC` existieren und sind lesbar
  (Feldlisten passen zum Provider). (5) DDIC-ANLAGE PER TOOL GEPRUEFT UND VERWORFEN: `SapProbe`
  kann die noetigen SE11-Strukturen (`ZSTR_LZCODE_USAGE`/`ZSTR_LZCODE_PARENT`) NICHT selbst
  anlegen — `DDIF_STRU_PUT` existiert nicht (korrekt: `DDIF_TABL_PUT`/`DDIF_TABL_ACTIVATE`), und
  diese sind auf T76 nicht RFC-freigegeben (Invoke-Test: „ist nicht 'remote' aufrufbar",
  SAP-Community bestaetigt DDIF*-Bausteine generell als nicht remote-enabled). Empfehlung:
  Strukturen manuell in SE11 anlegen, Feldliste ist verifiziert und in
  `.tmp_sap_probe/ddic_lzcode/` als Kopiervorlage abgelegt.
- WERKZEUG-ERWEITERUNG 2026-07-20/21, COMMIT `346bea3` (SapProbe, `.tmp_sap_probe/`): Der
  RFC/NCo-Direktzugriff auf SAP (unabhaengig von der OData-Strecke der App, siehe
  `docs/RAG_ROUTER.md` Abschnitt „Werkzeug: SAP-Direktzugriff") kann jetzt `rfc-call --table
  NAME=datei.csv`/`--struct NAME=datei.csv`, um beliebige RFC-faehige Bausteine mit Tabellen-/
  Strukturparametern aus CSV zu fuellen, gesperrt hinter `--confirm-write`/`--dry-run` wie
  `abap-write`. `function-info` zeigt bei TABLE/STRUCTURE-Parametern jetzt auch die
  verschachtelten Feldnamen mit. Grenzen empirisch geklaert (s. Punkt oben): fuer DDIC-Anlage
  nicht nutzbar, weil die dafuer noetigen Bausteine auf T76 nicht RFC-freigegeben sind.

- PRODUKTIVDATEN 2026-07-17 EINKAUF (kein Code-Deploy, reiner Datenlauf gegen die Server-DB): Einkauf-Full-Load nach dem heutigen `maracalcSet`-Fix erfolgreich durchgelaufen (`EKKO=172'914, EKPO=234'083, EKET=242'734, MARA-Status=67'665, LFA1-Namen=6'747`). Verifiziert: `SupplierName` in `PurchasingEkkoCache` jetzt zu 99.99 % gefuellt (172'898/172'914), vorher 0/172'874 (letzter erfolgreicher Load war vom 07.06., vor dem LFA1-Namens-Fix; der einzige Load danach am 02.07. war am `MARA001Set`-404 gescheitert, bevor LFA1 ueberhaupt geladen wurde). Stichprobe bestaetigt echte Namen statt Nummern: `66952 -> BEPRO AG`, `70369 -> CPT Praezisionstechnik GmbH`, `66715 -> GFS`, `65058 -> HEITZ GMBH`. Der Spend-Reiter (Matrix `Kaskadierung Lieferant / Jahr`) zeigt damit ab sofort Lieferantennamen statt nur Nummern. OFFENER PUNKT (nicht angefasst, gehoert mit Marco/Andreas abgestimmt): Die zentrale SAP-Quelle fuer Einkauf zeigt weiterhin auf `travt762` (Test-Server), nicht `travp762` (Prod) — gleiches Grundthema wie das bekannte ZSCHWEIZ/2026-Problem.
- DEPLOYED 2026-07-17 (Commit `c34e593 Rename "Export all" button to clarify it reloads from source, not just DB`, `257/257` Tests gruen, DLL `17.07.2026 10:41:31`, Laenge `3'006'976`, Port 443 erreichbar, DB unveraendert): UI-TEXT (Export Dashboard, alle 5 Sprachen mitgezogen): Button „Alle exportieren"/„Export all" umbenannt in „Alle Standorte laden"/„Reload all sites" (ES „Recargar todos los sitios", IT „Ricarica tutte le sedi", HI „सभी साइटें लोड करें"). Anlass: Ingo empfand „Alle exportieren" als irrefuehrend, weil der Button nicht nur bereits geladene Daten exportiert, sondern je aktivem Standort frisch von der Quelle (SAP/HANA/manuelle Datei) liest und die DB neu befuellt — Verwechslungsgefahr mit dem daneben liegenden „Zentrale Datei neu erzeugen" (das NUR mit der DB arbeitet, nichts neu laedt). Reine Beschriftungsaenderung, keine Logikaenderung. `Services/UiTextService.cs` Uebersetzungs-Dictionary-Keys aktualisiert (Key = deutscher String), damit ES/IT/HI nicht auf Englisch zurueckfallen. `257/257` Tests gruen.
- DEPLOYED 2026-07-17 (Commit `3a4efb5 Add purchasing spend drilldown by material group, fix broken MARA status read`, `257/257` Tests gruen, DLL `17.07.2026 10:05:07`, Laenge `3'006'464`, Port 443 erreichbar, DB unveraendert — neue Spalte `PurchasingEkpoCache.MaraMatkl` wird additiv beim naechsten App-Start ergaenzt): SPEND-DRILLDOWN nach Feedback-Runde Marco/Armin — Leitplanke "ein Punkt nach dem anderen, zuerst Reiter Spend". (1) Die Matrix `Kaskadierung Lieferant / Jahr` hat eine zweite Ebene: Lieferant aufklappen zeigt Spend je Warengruppe/Jahr (Pivot-artig, Drill-Summen exakt = Lieferantenzeile, Zeitraumfilter wirkt auf beide Ebenen); neue Aggregation `ExecuteSupplierGroupYearRowsAsync`, Modell `PurchasingSpendGroupYearRow`, Toggle-UI in `PurchasingSection.razor`. (2) Warengruppe nach Marcos Vorgabe aus dem MATERIALSTAMM (`MARA-MATKL`), nicht aus dem Beleg (alte Belege = Dummy-Warengruppe): neue additive Spalte `PurchasingEkpoCache.MaraMatkl`, Drilldown nutzt `COALESCE(MaraMatkl, Matkl, 'ohne Warengruppe')` mit UI-Hinweis auf den Fallback. ABER: `Matkl` ist in KEINEM MARA-EntityType des Service vorhanden -> SAP-Erweiterungsanfrage (`maracalc` um `Matkl` ergaenzen); App-Seite fertig, danach nur `$select` erweitern. (3) WICHTIGER NEBENBEFUND, produktionskritisch: SAP hat das MARA-Set umgebaut — `MARA001Set` exponiert `Mstae` NICHT mehr (`$select=Mstae` -> 404), der bestehende Einkauf-Full-Load/Delta waere beim naechsten Lauf FEHLGESCHLAGEN. Fix: `LoadMaterialStatusMapAsync` liest jetzt das neue `maracalcSet` (verifiziert: 68'094 Zeilen, 33'242 mit Status); Achtung, das Set ignoriert `$top`/`$skip` wie `mbewSet`, deshalb bewusst EIN ungepagter Request statt Paging. (4) ABC/XYZ: Weg jetzt klar (ABC = `MARC-MAABC` Sicht O2, XYZ separate Tabelle, vorhandener Report extrahiert beides) — bewusst erst nach Spend-Abnahme. 2 neue Drilldown-Tests. NACH Deploy: Einkauf Full Load noetig (fuellt Mstae wieder; MaraMatkl bleibt leer bis SAP-Erweiterung). Doku: `docs/PURCHASING_DASHBOARD_2026-06-05.md` Nachtrag 2026-07-17.
- DEPLOYED 2026-07-17 (Commit `846e3f8 Prepare additive contribution-margin fields and document standard-cost sourcing`, `255/255` Tests gruen, DLL `17.07.2026 08:53:22`, Laenge `2'992'640`, Port 443 erreichbar, DB unveraendert — neue Spalten `StandardCostVariable`/`StandardCostFixed` werden additiv beim naechsten App-Start ergaenzt): DECKUNGSBEITRAG (DB) als rein ADDITIVE Strecke vorbereitet — auf Wunsch von Ingo nach Andreas' Fachinput (DB = Umsatz minus variable Kosten; fix/variabel-Trennung entscheidend). NICHTS Bestehendes geloescht oder umbenannt. Umfang: (1) Neue nullable Felder `StandardCostVariable`/`StandardCostFixed` (Stueckpreis, Waehrung wie `StandardCost`) auf `SalesRecord`/`CentralSalesRecord`, Schema additiv (`AddColumnIfMissing`, `TEXT NULL`), Insert/Read-Pfade inkl. `CentralSalesDataProvider` und Audit-CSV (neue Spalten AM ENDE, aeltere CSV bleiben lesbar, leer -> null). (2) Import-Strecken koennen den Split aufnehmen: Manual-Excel-Header `standardcostvariable`/`standardcostfixed`, SAP-Mapping und Manual-Mapping unterstuetzen jetzt `decimal?`-Zielfelder (leere Quelle bleibt null statt 0 — wichtig, damit der DB offen bleibt statt falsch 100 %). (3) Gemeinsame Rechenlogik `Services/ContributionMarginCalculator.cs` (Vorzeichenregel wie Margen-Kostenbasis, Waehrungsregel ueber denselben Mask/Convert-Schalter `GroupMarginCostCurrencyMode`), genutzt von `ManagementCockpitService` UND `ExcelExportService` — Dashboard und Excel identisch. (4) Anzeige: Gruppenmarge-Reiter hat neue KPI-Kachel `Deckungsbeitrag (DB)`, DB-Spalte in Laender- und Detailtabelle (immer `-`, solange kein Split geliefert); zentrales Excel `Gruppenmarge Details` hat 4 neue Spalten am Ende (`Variable Unit Cost`, `Variable Cost Basis`, `Deckungsbeitrag (DB)`, `DB %` — Spalten W-Z, bestehende Formeln unveraendert), `Gruppenmarge Summary` hat `Deckungsbeitrag (DB)` (SUMIFS ueber Y) und `DB Zeilen` (COUNTIFS `<>`). DB-Summen laufen bewusst NUR ueber Zeilen mit geliefertem Split, Anzahl wird ausgewiesen. WICHTIG: Alle DB-Werte bleiben LEER, bis eine Quelle den fix/variabel-Split tatsaechlich liefert (CH/AT braeuchte eine SAP-Erweiterung analog WAVWR/STPRS, z. B. Planpreis fix/variabel aus der Kalkulation) — nichts wird geschaetzt. 9 neue Tests (`ContributionMarginCalculatorTests`, CSV-Roundtrip inkl. Null-Fall). Schulungs-Reiter `Standardkosten & Marge` und SVG auf Stand `vorbereitet` gebracht. ZUSAETZLICH (Wunsch Ingo, gegen Rueckfragen von Andreas): Das Blatt `Finance Filter Hilfe` im zentralen `Sales_All` enthaelt jetzt eine komplette FELDDOKUMENTATION — je Feld der Gruppenmarge-Blaetter Bedeutung und Berechnungsformel (Quantity, Unit Cost, Known Cost Basis inkl. Vorzeichen- und Konzernkostenregel, Margin/%, Supplier Type, Cost Source, alle Statuswerte, die 4 neuen DB-Spalten, Summary-Formeln) plus eine Tabelle `Woher die Standardkosten je Land kommen` (CH/AT WAVWR/STPRS, DE Alphaplan-Ableitung, B1 StockPrice, ES Sage, UK offen, TR-AG-Konzernkosten). NACHTRAG selber Tag: zusaetzlicher Abschnitt `Wo finde ich die Standardkosten in dieser Datei?` als Blatt/Spalte-Tabelle — klärt direkt in der Datei, dass `Sales` (Spalte X/Y) nur den unveraenderten Rohwert zeigt, `Finance Details` GAR KEINE Standardkosten-Spalte hat, und die eigentliche Berechnung (Kostenbasis, Marge, DB) in `Gruppenmarge Details`/`Gruppenmarge Summary` steht.
- DEPLOYED 2026-07-17 (siehe Eintrag oben, gleicher Commit/Deploy): Finance-Schulung (`/finance-cockpit/schulung`) um eigenen Reiter `Standardkosten & Marge` erweitert (`Components/Pages/FinanceTraining.razor`), inkl. neuer Prozessgrafik `wwwroot/training/standardkosten-margenfluss.svg` im Stil der bestehenden Keyuser-SVGs. Inhalt: Kostenquellen je Land (CH/AT WAVWR/STPRS, DE Alphaplan-Ableitung, B1 StockPrice, ES Sage-Spalte, UK offen, TR-AG-Konzernkosten), Rechenregeln (Stueckpreis, Menge x StandardCost, Vorzeichen bei Gutschriften, Marge CHF mit Jahreskurs, Mask/Convert-Schalter), Statusverhalten bei fehlenden Feldern (Standardpreis fehlt / Lieferant unklar inkl. Befund 2026-07-17 / Kostenwaehrung abweichend / Kurs fehlt), Fundstellen im Dashboard und zentralen Excel (Sales_All-Blaetter Gruppenmarge Summary/Details, Nachweis, Pruefbuch). Zusaetzlich fachlicher Input von Andreas als eigener Abschnitt dokumentiert: Deckungsbeitrag im zweiten Schritt (Umsatz minus variable Kosten; fix/variabel-Trennung entscheidend; SAP-Struktur enthaelt Planpreis fix/variabel getrennt) — mit klarer Abgrenzung, dass die App heute mit dem GESAMTEN Standardpreis rechnet und ein DB nach variablen Kosten weder berechnet noch im zentralen Excel ausgewiesen wird (Ausbauschritt, Entscheid bei Finance). Der von Ingo vermutete Ausweis "nach Abzug im zentralen Excel" existiert also noch NICHT.
- NEUER FUND 2026-07-17, nur dokumentiert (kein Code geaendert), globales Problem: Supplier-Felder (`SupplierNumber`/`SupplierName`/`SupplierCountry`) sind je Quelle strukturell leer statt nur lueckenhaft. CH/AT (`ZSCHWEIZ`, SAP OData) und UK (Manual Excel) haben dafuer im Seed-Mapping ueberhaupt keine Spalte vorgesehen — die Quellen liefern kein Lieferantenfeld; ES ebenso ohne Mapping; DE haengt am tatsaechlichen Alphaplan-Exportspaltenumfang; FR/IT/US/IN (SAP B1/HANA) liefern nur `OITM.CardCode`, den Standardlieferanten aus dem Artikelstamm (nicht den Beleglieferanten), leer wenn im Artikel kein Default-Lieferant gepflegt ist. Fachliche Tragweite: `GroupMarginSupplierClassifier.Resolve` liefert bei drei leeren Feldern `Unklar`, und `ManagementCockpitService.ResolveGroupMarginStatus` setzt dadurch IMMER `Lieferant unklar` — unabhaengig davon, ob eine Kostenbasis vorhanden waere. Direkte Konsequenz: die am 2026-07-16 gefuellte CH/AT-Kostenbasis (WAVWR/STPRS, TRCH 96.5 %, TRAT 99.9 %) ist in der Gruppenmarge-Sicht dadurch aktuell WIRKUNGSLOS — jede ZSCHWEIZ-Zeile bleibt mangels Supplier-Feldern auf `Lieferant unklar` maskiert. Gleiches strukturell fuer UK/ES. Neue offene Fachfrage an Andreas (noch nicht auf dem Multiple-Choice-Bogen): CH/AT als selbst verkaufende Trafag AG per Regel automatisch als eigene Lieferkategorie werten, statt ueber die leeren Supplier-Textfelder zu erkennen? Details: `docs/FINANCE_GRUPPENMARGE_2026-06-16.md` Nachtrag 2026-07-17.
- DEPLOYED 2026-07-14 (Commit `8e0f51e`, `203/203` Tests gruen, DLL `14.07.2026 17:30:30`, Laenge `2'923'008`, Port 443 erreichbar, DB unveraendert): KOSTENBASIS DER GRUPPENMARGE fuer CH/AT und DEUTSCHLAND gefuellt — das alte Thema `StandardCost`, nicht das Journal. AUSGANGSLAGE (an Prod-Daten gemessen): ZSCHWEIZ 40'292 Zeilen mit 0 % Kosten (`StandardCost` war im Seed hart auf `=0` gemappt, weil der Umsatz-Service kein Kostenfeld liefert), TRDE 6'879 Zeilen mit 0 % (Mapping wartete auf eine Spalte `EinstandsPreis`, die der Alphaplan-Export gar nicht hat), TRUK 0 %, TRFR 51 %, TRSE 81 %, TRUS 92 %, TRIT 96 %, TRIN 99 %. BEWEIS AUS SAP (ABAP-Report `docs/abap/ZFIN_ANALYSE_STPRS_JOURNAL.abap`, Ausgabe in `stdpreis.txt`): `mbewSet` ist im Service `ZPOWERBI_EINKAUF_SRV` BEREITS vorhanden — kein neues SAP-Objekt noetig; Bewertungskreis 1100 (Trafag AG, CH, CHF) hat 65'447 Materialien mit 96.3 % `STPRS > 0`, Bewertungskreis 1200 (Trafag Ges.m.b.H., AT, EUR) 2'564 mit 99.6 %; von den tatsaechlich fakturierten Zeilen haben 96.5 % einen Standardpreis (`VBRP-WAVWR` waere mit 92.3 % die Alternative, ist aber im Z-Service nicht exponiert); `PEINH` ist aktuell durchgaengig 1. UMGESETZT: (1) neuer `SapGatewayStandardCostReader` liest `mbewSet` gepaged (`$top`/`$skip`, Filter auf `Bwkey`), Schluessel ist **Material UND Bewertungskreis** (sonst bekaeme die CH-Zeile den AT-Preis), Material ueber `MaterialKeyNormalizer` normalisiert (fuehrende Nullen); (2) `StandardCostEnricher` ordnet je Umsatzzeile ueber `Land` -> Bewertungskreis zu (CH=1100, AT=1200, per T001K aus dem Report bestaetigt) und setzt `StandardCost`; (3) `SapGatewayDataSourceAdapter` reichert nach dem Umsatzimport an — schlaegt das Kostenlesen fehl, laeuft der Umsatzimport weiter (Warning im Eventlog), damit ein Kostenproblem nie den Tagesexport eines Landes kippt; (4) Deutschland: `ManualExcelImportService.DeriveAlphaplanUnitCost` leitet den Einstandswert aus `NettoPreisGesamt - RohertragGesamt` ab — Alphaplan muss NICHTS liefern, das Feld war immer da und wurde nur weggeworfen. ZENTRALE FALLE (in allen drei Pfaden geloest): `StandardCost` MUSS ein STUECKpreis sein, weil `ManagementCockpitService.ResolveGroupMarginCostBasis` mit `Menge x StandardCost` rechnet. `STPRS` gilt pro `PEINH` Stueck, `WAVWR` und der Alphaplan-Rohertrag sind ZEILENSUMMEN — ohne Division durch Preiseinheit bzw. Menge waere die Kostenbasis um genau diesen Faktor zu hoch. 14 neue Tests, `203/203` gruen. NACHSORGE: naechsten Export abwarten und die Kostenquote fuer ZSCHWEIZ/TRDE gegen die SAP-Erwartung (96.5 %) pruefen; Gruppenmarge fachlich mit Andreas plausibilisieren.
- OFFEN, WICHTIG (2026-07-14, aus demselben ABAP-Report): Der Report zeigt fuer Buchungskreis 1100 **9'573 Fakturapositionen mit Datum 2026** (1200: 360) und 383'493 Buchungsbelege 2026 — unser Dashboard zeigt fuer CH/AT im Jahr 2026 aber NULL Zeilen. Die bisher dokumentierte Erklaerung "SAP liefert keine 2026-Daten" ist damit WIDERLEGT: Die Daten sind da, der Fehler liegt in unserem Weg dorthin (`FinanzdataSchweizOeSet` gibt bei `Gjahr eq '2026'` nichts zurueck). Verdacht: Die Z-View fuellt `Gjahr` nicht oder filtert hart. Eigener Arbeitsstrang, fachlich vermutlich wertvoller als die Kostenspalte, weil CH/AT dadurch das LAUFENDE JAHR nicht sieht. Zusatz: In `Sites.SapServiceUrl` steht `travt762` (Test), nicht `travp762` (Prod) — vor einer Umstellung mit Andreas abstimmen, weil sich der komplette CH/AT-Datenbestand aendern wuerde.
- DEPLOYED 2026-07-14 (Commit `935561f`, `189/189` Tests gruen, DLL `14.07.2026 11:24:26`, Laenge `2'907'136`, Port 443 erreichbar, DB unveraendert — Spalte `CompanyCode` wird additiv beim naechsten App-Start ergaenzt): CH/AT (`ZSCHWEIZ`) im Journal-Import — App-Seite komplett, SAP-Seite offen. Neuer OData-Reader `SapGatewayFinancialJournalReader` liest das ECC-Hauptbuch (`BKPF`/`BSEG`) ueber das EntitySet `FinanzJournalSet` mit Gateway-Paging (`$top`/`$skip`/`$orderby`, 1000er-Seiten) und `$filter` auf `Budat`; `FinancialJournalRefreshService` routet nach Anschlussart (HANA -> B1-Reader, SAP_GATEWAY -> OData-Reader), `IsJournalSite` akzeptiert jetzt auch Gateway-Standorte mit aufloesbarer Service-URL. Neue additive Spalte `FinancialJournalEntries.CompanyCode` (= `Bukrs`) trennt CH von AT; `JournalEntryId = Bukrs/Gjahr/Belnr`; Soll/Haben aus `Shkzg`+`Dmbtr`/`Wrbtr` (Soll positiv, Haben negativ); `TransactionCurrency` nur bei echten Fremdwaehrungsbelegen; `IsManual = Blart SA` (Annahme, Andreas bestaetigen); `IsReversal = Stblg gesetzt`; fuehrende Nullen bei Konto/Kostenstelle/Profitcenter entfernt. WICHTIGE ABHAENGIGKEIT: Das EntitySet `FinanzJournalSet` existiert auf `travp762` noch NICHT — Felddefinition, ABAP-Skizze und Abnahmekriterien fuer das SAP-Team stehen in `docs/FINANCE_JOURNAL_SAP_ODATA_SPEZ_2026-07-14.md`; bis zum SAP-Rollout prueft der Reader die Service-Metadata und meldet dem Anwender klar, dass das EntitySet fehlt (kein Datenschaden, andere Gesellschaften laden normal). Navigation/Seed-Titel von `B1 Journal Import` auf `Journal Import` verallgemeinert (Force-Update im Seed), Schulungsseite Abschnitt 7 aktualisiert. Tests: `189/189` gruen (MapRow-Vorzeichen/Composite-Key/Storno/SA-Tests, Gateway-Routing-Test, Statusliste inkl. ZSCHWEIZ).
- DEPLOYED 2026-07-14 (Commit `2977c74`, `186/186` Tests gruen, DLL `14.07.2026 10:33:06`, Laenge `2'893'824`, Port 443 erreichbar): B1-Journal-Import umfasst jetzt auch INDIEN (`TRIN`, Schema `TRAFAG_LIVE`). Klarstellung von Ingo: Indien IST SAP B1, es ist in der Konfiguration nur falsch angeschrieben (Quellsystem-Code `SAGE`, eigener HANA-Server `20.197.20.60:30015`). Die Standortauswahl grenzt deshalb nicht mehr ueber den Quellsystem-Code `BI1` ein, sondern ueber die Anschlussart HANA + vorhandenes Schema (`FinancialJournalRefreshService.IsJournalSite`, umbenannt von `IsB1JournalSite`). Damit sind FR/IT/US/IN abgedeckt; CH/AT (SAP OData) und die Manual-Excel-Laender bleiben bewusst aussen vor. Zusaetzlich: `HanaFinancialJournalReader` prueft vor dem Lesen ueber `sys.tables`, ob `OJDT`/`JDT1` im Schema existieren, und wirft sonst eine klare fachliche Meldung statt eines rohen SQL-Fehlers (wichtig, weil der Dev-PC die HANA-Ziele nicht erreicht und die Indien-Tabellen noch nicht live geprobt sind). Tests: `186/186` gruen (1 neuer Indien-Ladetest, Auswahl-/Ablehnungstests auf FR/IN/UK/ZSCHWEIZ erweitert). NACHSORGE: beim ersten Indien-Lauf bestaetigen, dass `OJDT`/`JDT1` in `TRAFAG_LIVE` vorhanden sind. NAECHSTER SCHRITT (separat, mit Fable geplant): CH/AT-Journal ueber SAP OData — braucht eigenen Reader (`BKPF`/`BSEG`/`ACDOCA`) UND ein neues EntitySet auf SAP-Seite, da der aktuelle Z-Service nur Umsatzdaten liefert.
- DEPLOYED 2026-07-14 (Commit `8db6350`, `185/185` Tests gruen, DLL `14.07.2026 08:27:29`, Laenge `2'885'120`, Port 443 erreichbar, DB unveraendert publiziert — `FinancialJournalEntries` wird additiv beim naechsten App-Start angelegt): B1-Journal-Import in separate Tabelle `FinancialJournalEntries` fuer Konsolidierung/Analysen nach der Prioliste von Andreas. Neuer Import liest je B1-Gesellschaft (FR `fr01_p`, IT `it01_p`, US `us01_p`; Quellsystem `BI1`) die Hauptbuch-Buchungszeilen aus `OJDT`/`JDT1` plus `OACT`-Kontonamen und `OADM`-Hauswaehrung — volles Hauptbuch, bewusst OHNE den IT-Umsatzkontenfilter der Sales-Strecke. Feldumfang exakt nach Prioliste inkl. Betrag mit Vorzeichen (Soll-Haben), FiscalYear/Periode aus RefDate, `IsManual` (TransType 30), `IsReversal` (StornoToTr/AutoStorno). Mechanik wie gehabt, aber eigene Tabelle: zentraler HANA-Konnektor + Credentials wie `HanaDataSourceAdapter`, Full Load mit `ExportSettings.DateFilter` auf `RefDate`, transaktionales Ersetzen je TSC, Guardrail gegen 0-Zeilen-Ueberschreiben; Logging in `AppEventLogs` Kategorie `Journal` (nicht `ExportLogs` — Heartbeat bleibt sauber). Neue Seite `Finance Cockpit > B1 Journal Import` (`/finance-journal-import`, Seed `finance-journal-import`) mit Laden je Gesellschaft/alle, Zeilenzahl, Buchungsdatum von/bis, letzter Load. Schema additiv via `EnsureFinancialJournalEntriesTable` (Create-if-not-exists + Indizes + Unique `(Tsc, JournalEntryId, JournalEntryLineId)`), keine Migration. 9 neue Tests; `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit `185/185` gruen. VOR ERSTEM PRODUKTIVLAUF: B1-Spaltennamen (`ProfitCode`, `OcrCode2`, `FCCurrency`, `StornoToTr`, `AutoStorno`) einmal live gegen `fr01_p` proben. Details/Feldmapping: `docs/FINANCE_B1_JOURNAL_IMPORT_2026-07-14.md`.
- DEPLOYED 2026-07-13 (Commits `78d2772`, `2a94395`, `176/176` Tests gruen, DLL `13.07.2026 21:03:09`, Laenge `2'836'992`, Port 443 erreichbar, DB unveraendert): Daten-Heartbeat-Ausbau (Exportlauf-Streifen aus `ExportLogs`, 7-Tage-Glaettungsschalter, erweiterter Excel-Export) und UK-Selbstfuetterungs-Fix (siehe zwei Eintraege unten fuer Details). NACHSORGE nach diesem Deploy: UK-Export einmal laufen lassen und den neuen Bestand/den Wert der Rechnung 0000043747 fachlich pruefen; ZSCHWEIZ hat weiterhin 0 Zeilen fuer 2026 (SAP-seitiges Problem, nicht Teil dieses Deploys).
- ROOTCAUSE GEFUNDEN + GEFIXT am 2026-07-13 (jetzt deployed, siehe Eintrag oben): UK/TRUK hatte nur noch 2 Zeilen, weil der Manual-Import sich SELBST fuetterte. Beweiskette aus Prod-AppEventLog: `Neueste SharePoint-Datei ausgewaehlt | Import/Finance/UK_B1/Sales_ProcessedMergeInput_TRUK_2026-07-13.csv` -> App liest ihre eigene Audit-CSV vom Vortag als "UK-Quelle" (2 Zeilen, Rechnung 0000043747), ersetzt damit `CentralSalesRecords` fuer UK (`Geloescht=2 | Neu=2`) und laedt danach wieder eine neue Audit-CSV nach `UK_B1` hoch. Aktiv seit Audit-CSV-Upload produktiv (~30.06.); echte UK-Dateien (`ddMMyy_TRUK.xlsx`, z. B. `070726_TRUK.xlsx`) verloren fast immer das "neueste Datei"-Rennen gegen die eigene CSV. Zweites Problem: auch bei korrekter Dateiwahl las der Tageslauf (ohne Importjahr) NUR die neueste Delta-Datei und ersetzte damit den ganzen UK-Bestand (ExportLogs: taeglich 2-23 Zeilen). FIX (Commit siehe unten): (1) `SharePointUploadService.IsOwnExportOutputFile` schliesst eigene Ausgaben (`Sales_ProcessedMergeInput_*`, `Sales_<TSC>_<yyyy-MM-dd>.*`) aus der Import-Kandidatenauswahl aus — SharePoint- und Lokalordner-Pfad; genuine Muster wie `070726_TRUK.xlsx` und `Sales_TRUK_2025.xlsx` (Jahresdatei) bleiben zulaessig. (2) Ordner-Import ohne explizites Jahr nutzt jetzt das Basis+Delta-Modell: neueste Jahres-/Basisdatei plus ALLE neueren datierten Deltas zusammen, generische Dedupe (`SourceLineId`, sonst Invoice/Position/Material, spaetere Datei gewinnt, `DeduplicateManualSalesRecords`); ohne Basisdatei werden alle datierten Deltas gemeinsam gelesen. 9 neue Tests; `dotnet test TrafagSalesExporter.sln` mit `176/176` gruen. NACHSORGE nach Deploy: UK-Export einmal laufen lassen und pruefen, ob `UK_B1` eine gueltige Jahres-/Basisdatei enthaelt (sonst ergibt sich der Bestand nur aus den vorhandenen Delta-Dateien); Zeile mit 130'900 GBP aus Rechnung 0000043747 fachlich pruefen (Wert koennte durch die Selbstfuetterungs-Schleife verfaelscht worden sein).
- Neu umgesetzt und getestet am 2026-07-13 (jetzt deployed, siehe Eintrag oben): Daten-Heartbeat-Ausbau nach Prod-Datenanalyse. Befund: Die vielen "Unterbrechungen" waren ueberwiegend echte Datenmuster, nicht Heartbeat-Fehler — ZSCHWEIZ hat seit Juni 0 Buchungen an jedem Tag (alle 40'292 CSV-Zeilen sind 2025; passt zu `FinanzdataSchweizOeSet Gjahr eq '2026' = 0`), TRUK ist mit 2 Zeilen faktisch leer, TRIT/TRUS/TRFR fakturieren in Batches mit vielen echten Null-Tagen; nur TRSE/TRIN liefern annaehernd taeglich. Nebenbefund: 11.07. (Sa) lief kein Timer-Export, 12.07. erst 19:46 als Catch-up — App-Pool `AlwaysRunning`/`idleTimeout=0` am Server weiterhin offen. Umgesetzt deshalb: (1) zweiter SVG-Streifen `Exportlauf` je Tag aus `ExportLogs` (`ManagementCockpitService.ApplyHeartbeatExportRuns`, pure/statisch: gruen OK-Lauf, rot nur Fehler-Laeufe, orange kein Lauf ab erstem Log im Fenster, hellgrau davor/unbekannt), Kopfzeile mit `Letzter Export OK` plus Warn-Chip fuer Tage ohne Lauf/Fehler — trennt Update-Gesundheit von Geschaeftsaktivitaet; (2) Schalter `7-Tage-Summe` glaettet Linie/Flaeche ueber `RollingRowCount7` (Berechnung in `BuildDataHeartbeatDays`); (3) Excel-Export um `RollingRowCount7`, `ExportRun`, `LastSuccessfulExportUtc`, `ExportMissedCount`, `ExportErrorCount` erweitert. 4 neue Unit-/Integrationstests; `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit `167/167` gruen. Fachlich zu eskalieren (kein Graph-Thema): ZSCHWEIZ-2026-Daten fehlen SAP-seitig komplett; UK liefert faktisch nichts.
- Neu umgesetzt, gefixt, committed und deployed am 2026-07-13: Finance-Daten-Heartbeat unter `Management Analyse > Experten > Daten-Heartbeat` (`management-cockpit?section=heartbeat`, Seed-Key `finance-heartbeat`). Der Reiter nutzt denselben zentralen Finance-Datenpfad wie Summary/Pivot (Audit-CSV bevorzugt, DB-Fallback), rendert je TSC/Land ein Inline-SVG mit Tageslinie und farbigem Heartbeat-Streifen, bietet 30/60/90 Tage/laufendes Jahr und `Export to Excel`. Statuslogik nach Live-Fix: Zeilen > 0 OK; Tage ohne Buchungen bleiben neutral, solange der Standort frisch aktualisiert wurde; bei fehlendem Freshness-Zeitstempel wird nach dem letzten Datentag `Warn` angezeigt; altes LastUpdate >2 Kalendertage erzwingt `Gap` fuer Tage nach dem letzten Datentag. `LatestStoredAtUtc` bleibt primaer, `ExtractionDate` ist Fallback fuer `Letztes Update`; `TRES`/`TRSE` wird Spanien zugeordnet. Commits: `abc59e3` Feature, `2cf227c` Routing-Fix, `aff78dd` Gap-Logik-Fix. Tests: `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit `163/163` gruen; Publish nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$`, `app_offline.htm` entfernt, Port 443 erreichbar.

## Offene Punkte aus aelteren Eintraegen (Original im Archiv)

- Server/IIS (seit 2026-07-08, nur direkt am Server moeglich, WinRM gesperrt): App-Pool `startMode=AlwaysRunning` + `processModel.idleTimeout=00:00:00` setzen, damit der 12:00-Timer ohne vorherigen HTTP-Request laeuft. Bis dahin holt `CatchUpMissedRunAsync` verpasste Tageslaeufe beim naechsten Prozessstart nach.
- Betriebshinweis DE/Alphaplan (seit 2026-07-03): Der Alphaplan-Upload nach SharePoint muss VOR dem 12:00-Timer laufen, sonst verwendet der Tagesexport noch den vorherigen ZIP-Stand.

## Aeltere Eintraege / Historie

- Kurzstand-Eintraege 2026-06-04 bis 2026-07-08 und alle Nachtrag-Abschnitte (Mai/Juni 2026): verbatim in `docs/raw_md_archive/LASTCHANGE_ARCHIV_bis_2026-07-12.md`.
- Kanonische Detailhistorie davor: `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`; Original-Volltexte: `docs/raw_md_archive/original_history_raws.zip` (nur zur Wiederherstellung).

## Einstieg / Router

- Themenrouter (zuerst laden): `docs/RAG_ROUTER.md`.
- Fuehrender Kurzkontext: `docs/rag/PROJECT.md`.
- Naechster Chat: `docs/RAG_ROUTER.md` -> diese Datei -> passende Themen-Kurzdatei aus `docs/rag/`.
