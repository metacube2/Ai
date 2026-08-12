# ABAP Webservice fuer ZLO03 (ZM_LZCODE20_OPT)

Stand: 2026-07-23 (numerische Materialnummern: Rohwert + MATN1 statt ALPHA)

## WICHTIG - Nachtrag 2026-07-23: numerische Materialnummern wurden nie gefunden (ALPHA war der Fehler)

Symptom: Top-Down fuer eine rein NUMERISCHE Materialnummer (z.B. `2217`)
lieferte IMMER 0 Zeilen - auch mit der 18-stelligen Form
`000000000000002217` und auch mit `include_deleted` (LVORM-Filter aus).
Alphanumerische Nummern (z.B. `D15019`) funktionierten einwandfrei.

Direkt am System verifiziert (SapProbe/RFC gegen travp762, plus
OData-Testbatterie mit den echten Service-Credentials):

- `MARA` hat `000000000000002217` mit **leerem** `LVORM` - also NICHT
  loeschvorgemerkt. Die 22d-Theorie (Loeschvormerkung) war damit falsch.
- `ZPOWERBI_VC_TXT` hat die Zeilen zu `000000000000002217` mit gefuellter
  `MENGE`/`MENGENEINHEIT` (z.B. Kompnr `D15072`, Menge 1.000, ME ST).
- `SELECT ... WHERE matnr = '000000000000002217'` matcht in beiden
  Tabellen (direkter RFC-Read).
- Trotzdem: OData Top-Down `Vknr eq '000000000000002217'` -> 0 Zeilen.

Da `include_deleted` (kein LVORM-Filter) ebenfalls 0 lieferte, MUSS
Schritt 1 (`SELECT matnr FROM mara`) leer zurueckkommen - der Wert in der
RANGE `lt_r_matnr` matcht MARA nicht. Ursache: `CONVERSION_EXIT_ALPHA_INPUT`
(Version 22c) brachte den numerischen Wert NICHT zuverlaessig auf die
zero-padded interne Form - es zerstoerte sogar die bereits gepaddete
Eingabe (live verifiziert: auch `000000000000002217` -> 0). Alphanumerische
Nummern waren nie betroffen, weil MARA sie linksbuendig speichert (keine
Konvertierung noetig).

**FIX (Version 2026-07-23), doppelt abgesichert:**
1. **C#-Seite** (`MaterialUsageDataRefreshService.NormalizeMaterialToken`,
   bereits deployt): rein numerische Materialnummern werden vor dem
   `$filter`-Bau mit fuehrenden Nullen auf 18 Stellen gebracht
   (`2217` -> `000000000000002217`, `35-40` -> beide Grenzen gepaddet).
   Alphanumerische bleiben unveraendert.
2. **ABAP-Seite** (beide Methodenruempfe): die ALPHA-Konvertierung ist
   entfernt. Stattdessen wird (a) der ROHWERT immer in die RANGE
   aufgenommen (die App schickt jetzt bereits gepaddet -> sicherer
   Treffer, voellig unabhaengig von jeder Konvertierung), und (b)
   zusaetzlich die `CONVERSION_EXIT_MATN1_INPUT`-Form fuer kurze manuelle
   Eingaben (MATN1 ist die materialnummern-spezifische Konvertierung, die
   das Materialnummern-Customizing respektiert - nicht die generische
   ALPHA).

**Nacharbeit SAP (letzter Transport dieser Serie, hoffentlich):** beide
Methodenruempfe erneut auf travt762 UND travp762 einfuegen, Klasse
aktivieren, `/IWFND/CACHE_CLEANUP`. Die C#-Seite ist bereits deployt.

## Frueherer Nachtrag 2026-07-22c: Filterwerte brauchen Konvertierung (Ursache erkannt, Fix ersetzt)

Nach dem Fix auf `ZPOWERBI_VC_TXT` (unten) lief der Full Load gegen `travp762`

## WICHTIG - Nachtrag 2026-07-22c: Filterwerte brauchen ALPHA-Konvertierung (bestaetigte Ursache)

Nach dem Fix auf `ZPOWERBI_VC_TXT` (unten) lief der Full Load gegen `travp762`
technisch durch, lieferte aber fuer `Vknr=2217`/`TOPDOWN` **0 Zeilen**. Ingo
hat das direkt am Browser nachgestellt und damit die Ursache zweifelsfrei
belegt: derselbe `$filter=Richtung eq 'TOPDOWN' and Vknr eq '2217'`
(Kurzform) lieferte 0 Treffer, `$filter=... and Vknr eq
'000000000000002217'` (18-stellig, fuehrende Nullen) lieferte die echte
Zeile (`Kompnr=C34882`, `KompnrMaktx=SCHALTHEBEL BEARBEITET`). Aus dem
Produktionslog (`AppEventLogs`, Kategorie `MaterialUsage`) zusaetzlich
bestaetigt: Der App-Full-Load hatte exakt denselben, unpadded Wert `2217`
verwendet wie der erste (fehlgeschlagene) manuelle Test - **kein**
Padding-Problem im C#-Code (`MaterialUsageDataRefreshService.cs` reicht den
Eingabewert unveraendert in den `$filter` durch, das ist korrekt so).

**Ursache (bestaetigt):** `MARA`/`ZPOWERBI_VC_TXT` speichern Materialnummern
intern 18-stellig mit fuehrenden Nullen (Standard-MATNR-Domaene). Eine
selbstgeschriebene/redefinierte `GET_ENTITYSET`-Methode bekommt
`it_filter_select_options` jedoch ROH - die sonst fuer generisch generierte
Services automatische externe->interne ALPHA-Konvertierung des Gateway-
Frameworks greift bei eigenem Code NICHT automatisch. Schritt 1
(`SELECT matnr FROM mara WHERE matnr IN lt_r_matnr`) fand die Kurzform daher
nicht, `lt_mara_sel` blieb leer, Methode brach mit 0 Zeilen ab - obwohl die
JSON-Ausgabe der Property selbst wieder in Kurzform erscheint (das ist reine
Output-Formatierung des generierten Strukturfelds, unabhaengig vom
Filter-Handling).

(Fruehere Vermutung in einer Zwischenversion dieses Dokuments, ein
sprachabhaengiger `MAKTX`-Join sei die Ursache, war eine unbestaetigte
Vermutung und ist durch diesen Befund widerlegt bzw. gegenstandslos - siehe
trotzdem den entfernten `MAKTX`-Zeilen-Drop unten als separate,
unabhaengig sinnvolle Haertung.)

**FIX (Version 2026-07-22c):** Beide Methoden konvertieren Low/High-Werte der
`Vknr`/`Kompnr`-Filter jetzt per `CONVERSION_EXIT_ALPHA_INPUT`, bevor sie in
die `RANGE OF matnr`-Tabellen wandern. Damit funktionieren Kurzform ("2217")
und 18-stellige Form gleichermassen - Nutzer/Client muessen sich um das
Format nicht kuemmern.

**Zusaetzliche Haertung (Version 2026-07-22b, unabhaengig von obigem Fix,
weiterhin gueltig):** `FIX 4` des Reports (`DELETE gt_ktab WHERE maktx IS
INITIAL`) wurde NICHT uebernommen. Die `MAKT`-Textsuche joint ueber
`t~spras = sy-langu`, also abhaengig von der SAP-Anmeldesprache des
aufrufenden Users; fuer eine Excel-Ausgabe ist das Wegfiltern leerer
Textzeilen sinnvoll, fuer einen maschinell konsumierten Webservice waere es
riskant (eine fehlende Uebersetzung koennte sonst echte Bestandsdaten
verstecken). Die Zeile wird deshalb immer ausgegeben, `KompnrMaktx` bleibt im
Zweifel leer. Der Textpositions-Ausschluss (`postyp = 'T'`) ist davon nicht
betroffen und bleibt bestehen.

**Nacharbeit SAP-Seite (wie bei den vorigen Fixes):** Methodenruempfe erneut
auf travt762 UND travp762 einfuegen, Klasse aktivieren,
`/IWFND/CACHE_CLEANUP`.

## Nachtrag 2026-07-22d: loeschvorgemerkte Materialien optional einbeziehen

Nach dem ALPHA-Fix (Version c) lieferte Top-Down fuer alte, numerische
Vknr-Werte (z.B. "2217") weiterhin 0 Zeilen. Live-Diagnose (direkte OData-Calls
mit denselben Service-Credentials wie die App) hat die Ursache eingegrenzt:

- Top-Down mit einem "normalen" Material (`D15019`) funktioniert einwandfrei.
- Bottom-Up mit `Kompnr=C34882` liefert sofort viele Treffer, DARUNTER auch
  `Vknr=2217` mit echten Daten (Bestand, Stueckkosten 0.55 usw.) - die
  Verwendung ist in `ZPOWERBI_VC_TXT` also nachweislich vorhanden.
- Top-Down mit `Vknr=2217` (Kurz- UND Langform) liefert weiterhin 0 Zeilen.

Grund: Schritt 1 (Materialselektion gegen `MARA`) laesst per Default nur
nicht-loeschvorgemerkte Materialien zu (`LVORM = ' '`) - exakt wie der
Original-Report per Default (`p_lvorm = ' '`). Die getesteten Nummern sind
offenbar alte, loeschvorgemerkte Kopfmaterialien (kurzes numerisches
Altschema); sie werden deshalb in Top-Down NICHT als gueltiges
Selektionsmaterial gefunden, obwohl ihre Verwendung als `Vknr`-Wert in
`ZPOWERBI_VC_TXT` weiterhin existiert.

**FIX (Wunsch Ingo):** Analog zur Report-Checkbox `p_lvorm` akzeptiert die
Methode jetzt einen Suffix `ALLE` am `Richtung`-Wert
(`TOPDOWNALLE`/`BOTTOMUPALLE`) - bewusst OHNE DDIC-/SEGW-Aenderung, nur ueber
den bestehenden String-Wert transportiert (weniger SAP-Nacharbeit als ein
neues Strukturfeld). Damit werden loeschvorgemerkte Materialien in Schritt 1
UND im Bottom-Up-Skip (zweiter LVORM-Check weiter unten) mit einbezogen. Das
ausgegebene `Richtung`-Feld bleibt normalisiert (`TOPDOWN`/`BOTTOMUP`, ohne
Suffix) - der Suffix ist reine Eingangssteuerung.

C#-Seite: `Components/Pages/BomAnalysis.razor` hat eine neue Checkbox "Auch
geloeschte Materialien"; `MaterialUsageDataRefreshService.RunFullLoadAsync`
hat einen neuen Parameter `includeDeleted`, `BuildRichtungValue` baut den
Suffix (2 neue Tests). Nacharbeit SAP wie gehabt: Methodenrumpf erneut auf
travt762 UND travp762 einfuegen, Klasse aktivieren, `/IWFND/CACHE_CLEANUP`.

## Nachtrag 2026-07-22: Bereichsfilter ("35-40") im Materialfeld

Auf Wunsch von Ingo unterstuetzt das Eingabefeld "Materialnummern" in
`Components/Pages/BomAnalysis.razor` jetzt neben kommagetrennten Einzelwerten
auch Bereiche in der Form `35-40`. Umgesetzt rein C#-seitig in
`MaterialUsageDataRefreshService.BuildMaterialClause` (5 neue Tests): ein
Token mit genau einem Bindestrich und nicht-leeren Seiten wird zu
`(Vknr ge 'X' and Vknr le 'Y')`, gemischt mit `Vknr eq 'Z'` fuer Einzelwerte,
alles per `or` verknuepft. KEINE ABAP-Aenderung noetig: das Gateway-Framework
fasst `ge`/`le` auf demselben Property beim Parsen von
`it_filter_select_options` bereits zu einer klassischen
Select-Options-Bereichszeile zusammen, die die bestehende generische
RANGE-Verarbeitung (inkl. der neuen ALPHA-Konvertierung aus 2026-07-22c)
unveraendert mitnimmt. Materialnummern selbst enthalten laut bisherigen
Beispielen keine Bindestriche, der Split ist daher eindeutig.

## WICHTIG - Nachtrag 2026-07-22: falsche Quelltabelle ZAT_VC, SYNTAX_ERROR auf PROD

Die Methodenruempfe vom 2026-07-21 basierten auf einer ALTEN Fassung des
Reports und lasen aus `ZAT_VC`. Die aktuelle Reportfassung (Referenz:
`docs/abap/originalzlo03.txt`, inkl. FIXES 1/2/4/5) liest aus
**`ZPOWERBI_VC_TXT`**. Auf `travp762` (PROD) existiert `ZAT_VC` nicht -
dadurch kompilierte die komplette DPC_EXT-Klasse nicht und **JEDES**
EntitySet des Service `ZPOWERBI_EINKAUF_SRV` (auch `EKKOSet`) brach mit
`SYNTAX_ERROR` ab (Befund 2026-07-22 nach dem travt/travp-URL-Wechsel;
auch der Einkauf-Full-Load war dadurch blockiert, Cache blieb dank
Guardrail unveraendert).

Beide Methodenruempfe (`ZSTR_LZCODE_USAG_GET_ENTITYSET.abap`,
`ZSTR_LZCODE_PARE_GET_ENTITYSET.abap`) sind am 2026-07-22 auf die neue
Reportfassung umgestellt worden:

- Quelltabelle `ZAT_VC` -> `ZPOWERBI_VC_TXT` (matnr=VKNR, kompnr, menge,
  mengeneinheit, baugruppe, postyp, kom_mstae).
- FIX 1 uebernommen: Rundung der Menge auf 0 Dezimalen ENTFERNT
  (0.070 M wurde vorher zu 0 gerundet und verschwand).
- FIX 2 uebernommen: kein Dedup je Vknr/Kompnr mehr - Mehrfachverwendungen
  derselben Komponente (verschiedene Pfade in derselben VKNR) werden
  SUMMIERT (COLLECT-Semantik des Reports); weiterhin deterministisch,
  weil ueber SORTED TABLE aggregiert wird.
- FIX 4 uebernommen: Textpositionen (`postyp = 'T'`) und Zeilen ohne
  `MAKTX` im Default ausgeschlossen (Report-Default `p_txtpo = ' '`).
- Baugruppen-Kennzeichen wie neue `fill_ktab`-Fassung:
  `(VC-Baugruppe ODER MAST-Stueckliste) UND beskz <> 'F'`.
- Stammdaten-JOIN ohne LVORM-Filter (Report laedt alle Stammsaetze;
  LVORM wirkt nur auf die Materialselektion bzw. den Bottom-Up-Skip,
  Default `p_lvorm = ' '`).

**Nacharbeit SAP-Seite (manuell, SE80/SEGW):** Beide Methodenruempfe auf
**beiden** Systemen (`travt762` UND `travp762`) neu einfuegen, Klasse
aktivieren, danach `/IWFND/CACHE_CLEANUP`. Der `SYNTAX_ERROR` auf P
verschwindet erst, wenn dort kein `ZAT_VC`-Bezug mehr in der Klasse
steht. Die DDIC-Strukturen `ZSTR_LZCODE_USAGE`/`ZSTR_LZCODE_PARENT`
bleiben unveraendert (kein SE11-Nacharbeitsbedarf); die C#-Seite
(`MaterialUsageDataRefreshService`) ist von der Umstellung nicht
betroffen (gleiche EntitySets, gleiche Felder).

Hinweis zu Feldherkunfts-Angaben weiter unten in diesem Dokument:
aeltere Abschnitte nennen noch `ZAT_VC` als Quelle - fachlich gelten
dieselben Spalten, nur aus `ZPOWERBI_VC_TXT`. Die Live-Verifikation
2026-07-21 (Feldliste/Datenelemente) wurde gegen `ZAT_VC` auf T76
gefahren; `ZZLZCOD`/`ZZLZCODSORT`/`KOM_MSTAE`-Aussagen zu MARA bleiben
gueltig, die ZAT_VC-Leere-Aussage ("0 Zeilen gegen TEST erwartet") ist
fuer `ZPOWERBI_VC_TXT` NEU ZU PRUEFEN.

Status: **Entwurf fuer Lucas / SAP-Team.** Nichts hier ist in SAP angelegt,
kompiliert oder getestet. Ingo liefert den fachlichen Bedarf und einen
Umsetzungsvorschlag; die technische Anlage in SAP (Klasse, Gateway-Service,
Aktivierung) liegt gemaess Abgrenzung in `docs/INGO_TODOS_180_TAGE_2026-06-18.md`
bei Lucas bzw. dem SAP-Team.

## Auftrag

`zlo03.txt` (Report `ZM_LZCODE20_OPT`, Top-Down/Bottom-Up-Stuecklistenanalyse)
soll wie andere SAP-Objekte per Webservice ansprechbar sein, statt nur als
Download-Report zu laufen. "Andere Tabellen" meint hier den bestehenden
Gateway-Service `ZPOWERBI_EINKAUF_SRV`, ueber den heute u.a. `EKKO`/`EKPO`/`EKET`,
`MARA`/`MBEW` (`maracalcSet`, `mbewSet`) und die Produktsparten-Zuordnung
(`ProductDivisionRefSet`, siehe `docs/abap/README_PRODSPARTE.md`) laufen und die
der .NET-Dienst per `SapGatewayService`/`SapGatewayDataSourceAdapter` abruft.

## Warum nicht die Pivot-Spalten 1:1 abbilden

`create_excel_output` in `zlo03.txt` baut eine dynamische Pivot-Matrix: pro
selektiertem Kopfmaterial (Vknr) eine eigene Spalte, dazu eine
Mat.Status-Kopfzeile, eine Stueckzahl-/ME-Zeile und `=SUMMENPRODUKT`-Formeln.
Das ist ein Excel-Layout, keine Tabellenstruktur, und laesst sich nicht sinnvoll
in ein OData-EntitySet uebersetzen (Spaltenzahl haengt von der Selektion ab).

Stattdessen wird das bereits im Report vorhandene normalisierte Zeilenmodell
exponiert: `KTAB` (eine Zeile je Komponente, inkl. aller in `fill_ktab`
berechneten Felder wie `Endbestand`, `Stueckkosten`, `Wert_*`) verknuepft mit
`MTAB` (Menge je Vknr/Komponente-Paar). Das ist inhaltlich identisch mit dem,
was im Excel steht - nur unpivotiert, eine Zeile je Vknr/Komponente-Kombination
statt eine Spalte je Vknr.

## Entity 1: MaterialUsageSet

Eine Zeile je Kombination aus Kopfmaterial (`Vknr`) und Komponente (`Kompnr`).

| Feld | Herkunft im Report | Bemerkung |
| --- | --- | --- |
| `Richtung` | Selektionsparameter `P_TOPD`/`P_BOTU` | `TOPDOWN` oder `BOTTOMUP` |
| `Vknr` | `ZAT_VC-MATNR` (Top-Down) / `ZAT_VC-KOMPNR` (Bottom-Up) | Kopfmaterial bzw. Verwendungsmaterial |
| `VknrMstae` | `VTAB-MSTAE` | nur Top-Down gefuellt |
| `VknrVerbrauch` | `VTAB-VERBR` | nur Top-Down gefuellt |
| `VknrDispo` (neu 2026-07-23b) | `MARC-DISPO` des Vknr (Werk 1100) | Disponent des Kopfmaterials; Schluessel fuer den Produktgruppen-Aufriss (Disponent -> Produktgruppe via ZC23-Referenzliste). DDIC: Feld `VKNR_DISPO`, Datenelement `DISPO`. Live verifiziert: FERT-Endprodukte haben DISPO gefuellt (z.B. `019`). |
| `Kompnr` | `ZAT_VC-KOMPNR` (Top-Down) / `ZAT_VC-MATNR` (Bottom-Up) | Komponente |
| `KompnrMaktx`, `KompnrMeins` | `KTAB-MAKTX`/`MEINS` | aus `MAKT`/`MARA` |
| `Menge` | `MTAB-MENGE` | bereits mengeneinheiten-konvertiert (`UNIT_CONVERSION_SIMPLE`) und gerundet, wie `convert_menge` |
| `Exklusiv` | `gt_exkl_cache` | nur Top-Down fachlich belegt; Bottom-Up liefert der Report laut `load_global_exclusivity` ohnehin immer leer, siehe Abschnitt Determinismus |
| `Verbrauch`, `Labst`, `FesteZugang`, `GeplZugang`, `FesteAbgang`, `GeplAbgang`, `Endbestand`, `Omeng`, `Owert`, `Mkmng`, `Omkwr` | `ZMD04_CALC` via `KTAB` | direkt aus der vorberechneten Tabelle, kein Live-MD04-Aufbau (der Report hat diesen Pfad bewusst auskommentiert) |
| `Stueckkosten` | `KTAB-STUECKKOSTEN` | `VERPR/PEINH` bei `VPRSV = 'V'`, sonst `STPRS/PEINH` |
| `WertFesteZug`, `WertGeplZug`, `WertFesteAbg`, `WertGeplAbg`, `WertEndbestand` | `KTAB-WERT_*` | `Menge * Stueckkosten`, Werte bleiben positiv (Vorzeichen ist reine Excel-Darstellung im Original) |
| `Dismm`, `Minbe`, `Disls`, `Bstfe`, `Eisbe` | `MARC` (Dispo-Sicht `1100`) | wie im Original ueber `GC_WERKS = '1100'` |
| `Mstae`, `Mstav`, `Beskz`, `Zzlzcod`, `Zzlzcodsort` | `MARA` der Komponente | |
| `Baugruppe` | `MAST` (`STLAN = '1'`, Werk `1100`) + `Beskz <> 'F'` | wie `fill_ktab` |

## Entity 2: MaterialParentSet

Eine Zeile je Komponente/Elternmaterial-Paar (nur fachlich relevant Top-Down).
Ersetzt die komma-separierte `Elternmaterial`-Spalte aus `create_excel_output`
durch normalisierte Zeilen.

| Feld | Herkunft |
| --- | --- |
| `Kompnr` | `ZAT_VC-KOMPNR` |
| `ElternMatnr` | `ZAT_VC-KOM_MSTAE` (trotz des Feldnamens ein weiteres Elternmaterial, kein Statuswert - im Original-Report so uebernommen, bitte mit Lucas verifizieren ob das Feld im Ziel-DDIC wirklich so heisst) |

## Determinismus (Bezug zu docs/INGO_TODOS_180_TAGE_2026-06-18.md Punkt 4)

Der offene Punkt "ZLO03 / ZPOWERBI_VC_TXT Non-Determinismus, SELECT SINGLE ohne
ORDER BY" bezieht sich, soweit in `zlo03.txt` nachvollziehbar, konkret auf
`FORM get_elternmaterial`: Dort wird eine `HASHED TABLE` (`lt_distinct`) ohne
`SORT` durchlaufen, um die komma-separierte Elternmaterial-Liste zu bauen - die
Durchlaufreihenfolge einer Hashed Table ist in ABAP nicht definiert, die
Reihenfolge der Werte im Textfeld kann sich also zwischen zwei Laeufen mit
identischen Daten unterscheiden. Eine echte `SELECT SINGLE` ohne `ORDER BY`
kommt in `zlo03.txt` selbst nicht vor; falls sie gemeint ist, sitzt sie
vermutlich in einem der Bausteine, die den Report speisen (`ZAT_VC`-Aufbau,
`ZMD04_CALC`-Berechnung) und muesste dort separat mit Lucas geklaert werden.

Der Entwurf in `ZCL_LZCODE_PROVIDER.abap` behebt den bekannten Fall, indem
`get_parent_materials` explizit `SORT` + `DELETE ADJACENT DUPLICATES` auf einer
`STANDARD TABLE` nutzt statt einer `HASHED TABLE`-Ausgabe, und indem das
Elternmaterial als eigenes, sortiertes EntitySet statt als String kommt (der
Client kann selbst sortieren/gruppieren, die SAP-Seite muss keine
Reihenfolge-Entscheidung mehr treffen). Alle uebrigen Dedup-Schritte
(`ZAT_VC`-Paare, Komponenten-Exklusivitaet) verwenden bereits im Original
`SORT` + `DELETE ADJACENT DUPLICATES` auf `STANDARD TABLE`n und sind damit
deterministisch; der Entwurf uebernimmt dieses Muster 1:1.

## SE11 - Benoetigte DDIC-Strukturen (VOR der Klasse anlegen)

**Fortschritt (Stand 2026-07-21): Beide Strukturen fertig angelegt und feldweise verifiziert.**
`ZSTR_LZCODE_USAGE` zeigt nach zwei Korrekturrunden (urspruenglich 8 falsche Feldtypen durch
Drag/Copy beim Eintippen) alle 38 Felder (37 aus Spezifikation + `WAERS`) mit korrektem
Datenelement/Datentyp/Laenge/Dezimalstellen. Waehrungsfeld heisst `WAERS` (Datenelement UND
Feldname), nicht `WAERUNG` wie urspruenglich vorgeschlagen - Namensabweichung ist bereits in
beide ABAP-Klassenentwuerfe (`ZCL_LZCODE_PROVIDER.abap`/`_INLINE.abap`, Feld auf `'CHF'`
konstant gesetzt) sowie in `Services/MaterialUsageDataRefreshService.cs` und das SQLite-Schema
(`MaterialUsageCache.Waers`) uebernommen; C#-Seite gebaut und getestet (`259/259` gruen).
`ZSTR_LZCODE_PARENT` (2 Felder) ebenfalls fertig - Strukturen haben in SE11 kein
Schluesselfeld-Konzept (nur Tabellen), Entity-Key ist erst ein SEGW-Thema, siehe Hinweis unten.
**Naechster Schritt (Entscheid 2026-07-21: Variante 3, keine eigene Klasse):** SEGW-Projekt hat
die Entity Types bereits aus den Strukturen importiert (generierte Stub-Methode
`ZSTR_LZCODE_USAG_GET_ENTITYSET` existiert). Jetzt die zwei Methodenruempfe aus
`docs/abap/ZSTR_LZCODE_USAG_GET_ENTITYSET.abap` und
`docs/abap/ZSTR_LZCODE_PARE_GET_ENTITYSET.abap` in die redefinierten Methoden kopieren,
aktivieren, Metadaten-Cache leeren, testen.

`GET_DATA`/`GET_PARENT_MATERIALS` geben aktuell klassenlokale `TYPES` zurueck. Fuer SEGW
("Entity Type aus DDIC-Struktur importieren") muessen das **globale Strukturen** sein - exakt
das gleiche Muster wie `ZSTR_PRODSPARTE_OUT` in `docs/abap/README_PRODSPARTE.md`. Reihenfolge:
zuerst diese zwei Strukturen in SE11 anlegen und aktivieren, DANACH die Klasse (die Klasse
referenziert die Strukturnamen direkt).

### ZSTR_LZCODE_USAGE (fuer MaterialUsageSet / GET_DATA)

| Komponente | Komponententyp | Bemerkung |
| --- | --- | --- |
| `RICHTUNG` | Vordefinierter Typ: `CHAR`, Laenge 10 | 'TOPDOWN'/'BOTTOMUP' |
| `VKNR` | `MATNR` | |
| `VKNR_MSTAE` | `MSTAE` | |
| `VKNR_VERBRAUCH` | `MENGE_D` | |
| `KOMPNR` | `MATNR` | |
| `KOMPNR_MAKTX` | `MAKTX` | |
| `KOMPNR_MEINS` | `MEINS` | |
| `MENGE` | `MENGE_D` | |
| `EXKLUSIV` | **nicht `ABAP_BOOL`** - siehe Warnung unten, stattdessen `BOOLE_D` oder `XFELD` | |
| `VERBRAUCH` | `MENGE_D` | |
| `LABST` | `LABST` | |
| `FESTE_ZUGANG` | `MENGE_D` | |
| `GEPL_ZUGANG` | `MENGE_D` | |
| `FESTE_ABGANG` | `MENGE_D` | |
| `GEPL_ABGANG` | `MENGE_D` | |
| `ENDBESTAND` | `MENGE_D` | |
| `OMENG` | `MENGE_D` | |
| `MKMNG` | `MENGE_D` | |
| `STUECKKOSTEN` | Vordefinierter Typ: `DEC`, Laenge 11, Dezimalstellen 2 | |
| `WERT_FESTE_ZUG` | Vordefinierter Typ: `DEC`, Laenge 11, Dezimalstellen 2 | |
| `WERT_GEPL_ZUG` | Vordefinierter Typ: `DEC`, Laenge 11, Dezimalstellen 2 | |
| `WERT_FESTE_ABG` | Vordefinierter Typ: `DEC`, Laenge 11, Dezimalstellen 2 | |
| `WERT_GEPL_ABG` | Vordefinierter Typ: `DEC`, Laenge 11, Dezimalstellen 2 | |
| `WERT_ENDBESTAND` | Vordefinierter Typ: `DEC`, Laenge 11, Dezimalstellen 2 | |
| `OWERT` | `SALK3` | Referenzfeld `WAERUNG` (s. u.), CURR-Pflichtfeld |
| `OMKWR` | `SALK3` | Referenzfeld `WAERUNG` (s. u.), CURR-Pflichtfeld |
| `WAERUNG` | `WAERS` | NEU 2026-07-21: Waehrungsschluessel, im Provider fest `'CHF'` (Werk 1100 = Trafag AG/CH/CHF laut `docs/FINANCE_STANDARDKOSTEN_2026-07-14.md`) |
| `DISMM` | `DISMM` | |
| `MINBE` | `MINBE` | |
| `DISLS` | `DISLS` | |
| `BSTFE` | `BSTFE` | |
| `EISBE` | `EISBE` | |
| `MSTAE` | `MSTAE` | Materialstatus Komponente |
| `MSTAV` | `MSTAV` | |
| `BESKZ` | `BESKZ` | |
| `ZZLZCOD` | `ZZLZCOD` (verifiziert, s. u.) | Datenelement existiert, `CHAR 4`, „Lebenszykluscode" |
| `ZZLZCODSORT` | `ZZLZCODSORT` (verifiziert, s. u.) | Datenelement existiert, `CHAR 4`, „Lebenszykluscode Sortiment" |
| `BAUGRUPPE` | **nicht `ABAP_BOOL`**, siehe Warnung unten | |

### ZSTR_LZCODE_PARENT (fuer MaterialParentSet / GET_PARENT_MATERIALS)

**Status 2026-07-21: angelegt und verifiziert, beide Felder korrekt (`MATNR`/`CHAR 40`).**

| Komponente | Komponententyp |
| --- | --- |
| `KOMPNR` | `MATNR` |
| `ELTERN_MATNR` | `MATNR` |

Hinweis: Strukturen (im Unterschied zu transparenten Tabellen) haben in SE11 KEIN
Schluesselfeld-Haekchen - das gibt es nur bei Datenbanktabellen. Die Festlegung, welche
Eigenschaft(en) den **Entity Key** bilden, passiert erst spaeter in SEGW beim Import als Entity
Type (`MaterialParentSet` braucht dort `Kompnr`+`ElternMatnr` oder mindestens `Kompnr` als Key,
je nachdem ob Kompnr pro Struktur eindeutig sein soll - das entscheidet Lucas bei der
Gateway-Anlage, nicht Teil der SE11-Struktur-Pflege).

### Drei Warnungen, bevor du in SE11 tippst (gleiche Fallen wie bei ZSTR_PRODSPARTE_OUT)

1. **„Vordefinierter Typ" ist eine eigene Checkbox/Spalte in der Komponenten-Pflege, nicht die
   `Komponententyp`-Spalte.** Fuer `RICHTUNG` und die sechs `DEC`-Felder gibt es kein
   Datenelement - in `Komponententyp` einfach `DEC` oder `CHAR` einzutippen funktioniert NICHT
   (SE11 sucht dann ein Datenelement mit diesem Namen und findet keins). Stattdessen: Haekchen
   „Vordefinierter Typ" in der jeweiligen Zeile setzen, danach erscheinen separate Felder
   `Datentyp`/`Laenge`/`Dezimalstellen`. Der DDIC-Datentyp fuer gepackte Zahlen heisst dort
   `DEC` (nicht `P` - `P` ist nur der interne ABAP-Typname, im DDIC-Katalog gibt es nur `DEC`).
2. **`ABAP_BOOL` ist in klassischen DDIC-Strukturen oft nicht direkt waehlbar/aktivierbar**
   (es ist primaer ein ABAP-OO-Typ, kein klassisches Datenelement in jedem System). Fuer
   `EXKLUSIV` und `BAUGRUPPE` in SE11 stattdessen `BOOLE_D` oder `XFELD` verwenden - exakt die
   Alternative, die `docs/abap/README_PRODSPARTE.md` fuer `IS_ASSIGNED` empfiehlt. Die Klasse
   setzt `abap_true`/`abap_false` ('X'/''), das passt technisch auf beide Alternativen.
3. **`ZZLZCOD`/`ZZLZCODSORT`** — 2026-07-21 am Live-System geklaert (s. u.): Beide haben ECHTE
   gleichnamige Datenelemente (`CHAR 4`), keine PAPH1-Falle. In SE11 direkt als `ROLLNAME
   ZZLZCOD` bzw. `ZZLZCODSORT` referenzierbar.
4. **`QUAN`/`CURR`-Felder brauchen zwingend `Referenztabelle`/`Referenzfeld`, sonst keine
   Aktivierung.** Referenztabelle bleibt LEER, wenn das Einheits-/Waehrungsfeld in derselben
   Struktur liegt (unser Fall). Betroffen:
   - Alle Mengenfelder (`QUAN`): `VKNR_VERBRAUCH`, `MENGE`, `VERBRAUCH`, `LABST`,
     `FESTE_ZUGANG`, `GEPL_ZUGANG`, `FESTE_ABGANG`, `GEPL_ABGANG`, `ENDBESTAND`, `OMENG`,
     `MKMNG`, `MINBE`, `BSTFE`, `EISBE` -> Referenzfeld `KOMPNR_MEINS`. Ausnahme:
     `VKNR_VERBRAUCH` ist fachlich die Einheit des Kopfmaterials (Vknr), nicht der Komponente -
     `KOMPNR_MEINS` ist hier eine bewusste Vereinfachung; fuer echte Praezision zusaetzliches
     Feld `VKNR_MEINS` (Datenelement `MEINS`) ergaenzen und dort referenzieren.
   - Beide Wertfelder (`CURR`): `OWERT`, `OMKWR` -> Referenzfeld `WAERUNG` (neues Feld, s. o.).
   - `STUECKKOSTEN`/`WERT_*` sind bewusst `DEC` (nicht `CURR`) und brauchen deshalb KEINE
     Referenz - das war ein zusaetzlicher Grund fuer die Wahl von `DEC` statt `CURR`.
   - Alle anderen Felder (`CHAR`/`NUMC`-Typen wie `MSTAE`, `BESKZ`, `ZZLZCOD`, `RICHTUNG`,
     `BOOLE_D`-Felder) brauchen keine Referenz.

## Live-Verifikation 2026-07-21 (SapProbe gegen T76/travt762 TEST)

Alle offenen Annahmen dieser Doku wurden per SapProbe (RFC/NCo) direkt am System geprueft:

- **`KOM_MSTAE` ist ein Materialnummer-Feld** (`table-fields ZAT_VC KOM_MSTAE`: `DATATYPE CHAR
  40`, `ROLLNAME MATNR`, Text „Materialnummer"). Der irrefuehrende Name ist damit geklaert —
  die Zuordnung `MaterialParentSet.ElternMatnr = ZAT_VC-KOM_MSTAE` ist korrekt. Nebenbefund:
  im ZAT_VC-Feldkatalog sind auch `MATNR`, `KOMPNR`, `MAT_MSTAE`, `KOM_MSTAV` alle MATNR-typisiert;
  `MENGE` ist `CHAR` (Datenelement `SOBJID`), was das „Menge als Charfeld halten" im Provider
  bestaetigt.
- **`ZZLZCOD`/`ZZLZCODSORT`**: echte Datenelemente, je `CHAR 4` (s. o.).
- **`ZAT_VC` und `ZMD04_CALC` existieren und sind lesbar**; Feldlisten decken sich mit dem, was
  der Provider liest (`ZMD04_CALC`: `MATNR`+`WERKS`, `LABST`, `FESTE_ZUG`, `GEPL_ZUG`,
  `FESTE_ABG`, `GEPL_ABG`, `VERBR`, `OMENG`, `OWERT`, `MKMNG`, `OMKWR`). `ZAT_VC` war auf TEST
  leer (Daten liegen auf PROD `travp762`), die Feld-Existenz ist aber bestaetigt.

### DDIC-Anlage per Tool: geprueft und verworfen

`SapProbe` kann die zwei Strukturen NICHT selbst anlegen: `DDIF_STRU_PUT` existiert nicht
(korrekt ist `DDIF_TABL_PUT`), und `DDIF_TABL_PUT`/`DDIF_TABL_ACTIVATE` sind auf T76 **nicht
RFC-freigegeben** (Invoke-Test: „ist nicht 'remote' aufrufbar"). Der von der SAP-Community
genannte Remote-Weg (eigener RFC-faehiger Z-Wrapper-FM um `DDIF_TABL_PUT`) waere fuer 2
einmalige Strukturen mehr Aufwand als die manuelle SE11-Anlage. **Empfehlung: die beiden
Strukturen von Hand in SE11 anlegen** — die Feldliste oben ist jetzt vollstaendig verifiziert,
also risikoarm abzutippen. `.tmp_sap_probe/ddic_lzcode/usage_fields.csv` enthaelt dieselbe Liste
maschinenlesbar als Kopiervorlage.

### Danach: Klasse anpassen

Sobald beide Strukturen aktiv sind, referenzieren `ZCL_LZCODE_PROVIDER.abap` und
`ZCL_LZCODE_PROVIDER_INLINE.abap` sie direkt (`TYPES tt_out TYPE STANDARD TABLE OF
zstr_lzcode_usage ...` statt einer lokalen `TYPES: BEGIN OF ty_out ...`) - das ist in beiden
Dateien bereits so vorbereitet.

## Benoetigte SAP-Objekte

**GEWAEHLTER WEG (2026-07-21, Entscheid Ingo): Variante 3 - KEINE eigene Klasse.** Die Logik
kommt direkt als Methodenrumpf in die redefinierten DPC_EXT-Methoden:

- `docs/abap/ZSTR_LZCODE_USAG_GET_ENTITYSET.abap` - kompletter Rumpf fuer die
  Usage-EntitySet-Methode (die gesamte zlo03-Logik: Schritte 1-8 wie in der Inline-Klasse,
  plus NEU: `$filter`-Auslesen aus `it_filter_select_options` fuer `Richtung`/`Vknr`/`Kompnr`,
  Pflicht-Filter-Guard analog Report-Meldung "Bitte Selektion eingeben", `$skip`/`$top`,
  Uebertrag per `MOVE-CORRESPONDING` nach `et_entityset`). Zwischen `METHOD.`/`ENDMETHOD.`
  einfuegen, keine CLASS-Statements enthalten.
- `docs/abap/ZSTR_LZCODE_PARE_GET_ENTITYSET.abap` - Rumpf fuer die Parent-EntitySet-Methode
  (Methodenname am generierten Stub pruefen, 30-Zeichen-Kuerzung). Einschraenkung dokumentiert:
  kein Vknr-Property am Entity, daher keine VKNR-Begrenzung wie im Report - Client filtert.
- UNVERIFIZIERT daran ist nur der Gateway-Rand (Filter-Parsing, `et_entityset`-Uebertrag) -
  der Kern ist der mehrfach gegengepruefte Inline-Code.

Alternativ (nicht gewaehlt, bleiben als Referenz): Klasse `ZCL_LZCODE_PROVIDER` - zwei
gleichwertige Entwuerfe, fachlich identisch mit den Methodenruempfen:
  - `docs/abap/ZCL_LZCODE_PROVIDER.abap` - v2 (Fable-Korrekturen: kein `CLASS-POOL`-Statement,
    `Menge`/`Meins` als Charfelder, `FOR ALL ENTRIES` ohne Range-`-low`, `it_vknr`-Parameter,
    deterministisches Dedup, sortierte Exklusivitaetspruefung). `GET_DATA` ruft private
    Methoden `load_stammdaten`/`load_md04`/`convert_menge`.
  - `docs/abap/ZCL_LZCODE_PROVIDER_INLINE.abap` - dieselbe Logik, aber `GET_DATA` ohne private
    Hilfsmethoden: alles inline in einer Methode.
  - Public, Final, Create Public, wie `ZCL_PRODSPARTE_PROVIDER`
  - Methoden `GET_DATA` (Entity 1) und `GET_PARENT_MATERIALS` (Entity 2)

**EntitySet-Name: GELOEST per dynamischer Aufloesung (2026-07-21).** SEGW hat die Entities nach
den DDIC-Strukturen benannt (`ZSTR_LZCODE_USAGE...`), nicht `MaterialUsageSet` wie urspruenglich
vorgeschlagen - die exakte Schreibweise (Set-Suffix, Kuerzung) ist offen. Die C#-Seite raet
deshalb NICHT mehr: `MaterialUsageDataRefreshService.ResolveEntitySetName` holt die
EntitySet-Liste vom Gateway und matcht normalisiert (nur Buchstaben/Ziffern, lowercase) auf
`lzcodeusage`/`lzcodeparent` bzw. `materialusage`/`materialparent` - findet also
`ZSTR_LZCODE_USAGESet`, `ZSTR_LZCODE_USAGE` und `MaterialUsageSet` gleichermassen. Auch die
OData-Property-Namen sind tolerant: `ParseRows` legt die Feldnamen ohne Unterstriche ab, damit
sowohl SEGW-CamelCase (`VknrMstae`, Konvention wie `WavwrDc`) als auch rohe Strukturnamen
(`VKNR_MSTAE`) auf dieselben Reads treffen.
- Voraussetzung: `ZAT_VC` und `ZMD04_CALC` muessen im Gateway-System lesbar
  sein (im Report selbst direkt per `SELECT` angesprochen, keine RFC-Huelle
  bekannt - bitte mit Lucas pruefen, ob das im Gateway-System 1:1 so gilt).
- Optional fuer DDIC/Gateway: Strukturen `ZSTR_LZCODE_USAGE` und
  `ZSTR_LZCODE_PARENT` fuer die beiden EntityTypes.

## Vorschlag Gateway-Anlage (analog ProductDivisionRefSet)

**Konkrete SEGW-Schrittfolge (Stand 2026-07-21, beide DDIC-Strukturen bereits aktiv):**

1. **Entity Types importieren:** Im Datenmodell-Baum -> Import -> DDIC Structure.
   `ZSTR_LZCODE_USAGE` -> Entity Type `MaterialUsage`; `ZSTR_LZCODE_PARENT` -> Entity Type
   `MaterialParent`. SEGW verlangt je Entity Type mindestens eine Key-Property (das ist ein
   SEGW-/OData-Konzept, KEIN SE11-Struktur-Schluessel):
   - `MaterialUsage`: Key auf `Richtung` + `Vknr` + `Kompnr` (eindeutige Kombination je Zeile).
   - `MaterialParent`: Key auf `Kompnr` + `ElternMatnr`.
2. **Entity Sets generieren** - meist automatisch beim Import (Haekchen „Create Related Entity
   Set"): `MaterialUsageSet`, `MaterialParentSet`.
3. **Laufzeitobjekte generieren** (`Strg+F6`) - legt in der `..._DPC_EXT`-Klasse leere
   Stub-Methoden `MATERIALUSAGESET_GET_ENTITYSET`/`MATERIALPARENTSET_GET_ENTITYSET` an.
4. **Nur die zwei NEUEN Stub-Methoden redefinieren** - genau wie bei `ProductDivisionRefSet`
   dokumentiert: `FINANZDATASCHWEI_GET_ENTITYSET` (bestehender Sales-EntitySet) NICHT anfassen.
   Fuer beide Methoden liegen fertige Methodenruempfe zum Reinkopieren bereit (Variante 3,
   ohne eigene Klasse - siehe Abschnitt "Benoetigte SAP-Objekte"):
   `docs/abap/ZSTR_LZCODE_USAG_GET_ENTITYSET.abap` und
   `docs/abap/ZSTR_LZCODE_PARE_GET_ENTITYSET.abap`.
5. **Aktivieren + Metadaten-Cache leeren**, damit `$metadata` den neuen Stand zeigt
   (`/IWFND/MAINT_SERVICE` pruefen, Cache-Cleanup).
- Selektionsparameter aus dem Report als `$filter`/Query-Parameter abbilden:
  - `Richtung` (`TOPDOWN`/`BOTTOMUP`) statt Radiobutton `P_TOPD`/`P_BOTU`
  - `Vknr`/`Kompnr` IN-Filter statt `S_MATNR`
  - `ZzTypCd`-Filter statt `S_TYPCD`
  - `P_DAYS` (Verbrauchszeitraum) bleibt vorerst Report-intern/ZMD04_CALC-seitig,
    da `ZMD04_CALC` bereits vorberechnet ist und der Zeitraum dort fest hinterlegt
    sein duerfte - mit Lucas klaeren, ob `ZMD04_CALC` ueberhaupt parametrisierbar ist.
- Testaufruf-Muster (nach Anlage, URL an echten Service-Root anpassen):

```text
http://travp762.sap.trafag.com:8000/sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/MaterialUsageSet?$filter=Richtung eq 'TOPDOWN'
```

Hinweis: `docs/rag/PURCHASING.md` dokumentiert bereits, dass die zentrale
SAP-Quelle fuer Einkauf aktuell auf `travt762` (Test) statt `travp762` (Prod)
zeigt - diese Umstellung betrifft auch einen neuen `MaterialUsageSet`-Aufruf
und sollte nicht isoliert fuer dieses Feature vorgezogen werden.

## C#-Seite (Stand 2026-07-21, an SEGW-Anlage angepasst)

`Services/MaterialUsageDataRefreshService.cs` (+ `IMaterialUsageDataRefreshService.cs`),
analog zu `PurchasingDataRefreshService`: liest die beiden EntitySets gepaged (`$top`/`$skip`)
ueber dieselbe SAP-Verbindung wie der Einkauf Full Load (Site `PURCHASING_SAP`), cached in
`MaterialUsageCache`/`MaterialParentCache` (SQLite, Schema in
`DatabaseSchemaMaintenanceService.EnsureMaterialUsageCacheTables`).

Anpassungen 2026-07-21 nach der SEGW-Aktivierung:

- **EntitySet-Namen dynamisch aufgeloest** (`ResolveEntitySetName`, s. o.) statt hart
  `MaterialUsageSet` - findet die SEGW-Strukturnamen automatisch. Fehlt beides, kommt weiterhin
  eine klare Fehlermeldung statt eines Absturzes (getestet).
- **Property-Namen tolerant**: `ParseRows` strippt Unterstriche aus den JSON-Keys, damit die
  Feld-Reads (`VknrMstae` usw.) sowohl SEGW-CamelCase als auch rohe Strukturnamen treffen.
- **Guard-Zusammenspiel**: Die SAP-Seite erzwingt einen `Vknr`/`Kompnr`-Filter. Der Full Load
  schickt deshalb ohne Materialliste bewusst `Vknr gt ''` (bzw. `Kompnr gt ''` fuers
  Parent-Set) als explizites Catch-all mit; `RunFullLoadAsync(materialFilter:)` nimmt optional
  eine kommagetrennte Materialliste, die als `eq`-Oder-Kette durchgereicht wird.
- **0 Zeilen gegen TEST ist korrekt**: `ZAT_VC` auf `travt762` ist leer (live verifiziert
  2026-07-21) - die Erfolgsmeldung weist bei 0/0 explizit darauf hin. Echte Daten erst gegen
  `travp762` (PROD; Umstellung der Site-URL gehoert zum bekannten offenen travt/travp-Punkt).

Wichtig, bevor das als "fertig" gilt:

- **UI vorhanden seit 2026-07-21, Dashboard erweitert am 2026-08-01** (Entscheid Ingo): neuer Root-Navigationsreiter **Logistik**
  (Icon LocalShipping) mit Unterpunkt **Stuecklistenanalyse**
  (`Components/Pages/BomAnalysis.razor`, Route `/logistik/stuecklistenanalyse`, Seed-Keys
  `logistics`/`logistics-bom-analysis`). Die Seite bietet: SAP-Load (Richtung Top-Down/Bottom-Up,
  optionaler kommagetrennter Materialfilter), Statusanzeige des letzten Laufs,
  richtungsabhaengige Kennzahlen, Top-12-Verwendungsbreite, Bestandslage und
  LZ-Code-Verteilung sowie eine durchsuchbare Cache-Vorschau (max. 200 Zeilen,
  `GetCachedUsageRowsAsync`). Die Dashboard-Aggregate werten dagegen den gesamten
  gefilterten Cache aus (`GetCachedAnalysisAsync`). Details und fachliche Grenzen:
  `docs/LOGISTIK_STUECKLISTEN_DASHBOARD_2026-08-01.md`. Die Daten sollen
  spaeter auch im Einkauf nutzbar sein (Exklusivitaet/Bestaende je Komponente), starten aber
  bewusst als eigener Logistik-Reiter.
- **Performance-Vorbehalt beim Catch-all-Full-Load**: Die DPC-Methode berechnet je `$skip`-Seite
  das GESAMTE Ergebnis neu (Standardverhalten der klassischen Paging-Implementierung). Bei
  grossen ZAT_VC-Bestaenden auf PROD heisst das Seitenzahl x Vollberechnung - vor einem echten
  PROD-Full-Load pruefen bzw. mit Materialfilter in Teilmengen laden.
- Full Load nur (kein Delta): Fuer ein sinnvolles Delta braeuchte es ein Aenderungsdatum-Feld
  auf SAP-Seite, das der Report/Provider bisher nicht liefert - absichtlich nicht spekulativ
  vorgebaut.

## Offene Punkte / noch zu klaeren

- ~~`ZAT_VC-KOM_MSTAE` verifizieren~~ ERLEDIGT 2026-07-21: ist ein MATNR-Feld
  (siehe Live-Verifikation) - Elternmaterial-Mapping bestaetigt.
- ~~`ZAT_VC`/`ZMD04_CALC` im Gateway-System selektierbar?~~ ERLEDIGT: beide Methoden
  am 2026-07-21 fehlerfrei aktiviert (direkter SELECT funktioniert im DPC).
- **Naechster Meilenstein: Ende-zu-Ende-Test.** Browser-Aufruf gegen den Service
  (`.../ZPOWERBI_EINKAUF_SRV/<UsageSet>?$filter=Richtung eq 'TOPDOWN' and Vknr eq '<MATNR>'`),
  danach C#-Load ueber `MaterialUsageDataRefreshService.RunFullLoadAsync` - gegen TEST sind
  0 Zeilen erwartet (ZAT_VC leer), gegen PROD erst nach travt/travp-Umstellung.
- Bottom-Up-Modus fachlich mit Einkauf/Lucas absichern, ob er ueberhaupt
  webservice-seitig gebraucht wird (Prioritaet 1 laut Report-Historie scheint
  Top-Down/PowerBI-Komponentenanalyse zu sein).
- `P_WVKNR` ("Weitere VKNRs anzeigen") und `P_TRANS` (transponierte Ausgabe)
  aus dem Report sind reine Excel-Darstellungsoptionen und bewusst NICHT
  Teil des Webservice-Entwurfs.
- ~~UI-Anbindung des C#-Dienstes~~ ERLEDIGT 2026-07-21: Root-Reiter Logistik >
  Stuecklistenanalyse (`BomAnalysis.razor`). Offen bleibt nur ein optionaler Timer/Auto-Load.
