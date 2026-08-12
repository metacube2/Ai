# Uebergabe: Produktsparten-Zuordnung Trafag AG

Stand: 2026-06-11

## Ziel

Fuer das Group-Sales-Dashboard wird eine flache Referenztabelle benoetigt:

`Materialnummer -> Produkthierarchie PAPH1 -> Produktfamilie WWPFA -> Produktsparte WWPSP`

SAP/Trafag AG bleibt fuehrend. Das Dashboard soll keine Ableitungslogik nachbauen, sondern nur per Material bzw. PAPH1 nachschlagen. Nicht zuordenbare Artikel laufen im Dashboard als "Nicht zugeordnet".

## Finale Architektur

Die fuehrende fachliche Quelle ist KEDE/KEDR, nicht Excel und nicht das Dashboard.

Finaler technischer Ablauf:

1. `Z_PRODSPARTE_MAP_BUILD` liest die KEDE/KEDR-Regeltabellen direkt.
2. Die Von-bis-Regeln werden in einzelne PAPH1-Werte expandiert.
3. Das Ergebnis wird in `ZPRODSPARTE_MAP` als flache Tabelle geschrieben:
   `PAPH1 -> WWPFA -> WWPSP`
4. `ZCL_PRODSPARTE_PROVIDER=>GET_DATA` liest `MVKE`/`MAKT` und macht einen exakten Lookup auf `ZPRODSPARTE_MAP`.
5. `Z_PRODSPARTE_ALL` und der OData-Service rufen nur noch den Provider.

Damit bleibt die Laufzeitlogik einfach: kein CSV-Import im produktiven Ablauf, keine Excel-Abhaengigkeit, keine Von-bis-Aufloesung im Dashboard.

## Warum die Architektur geaendert wurde

Der erste Ansatz war, die bereits abgeleiteten Werte direkt aus `CE11000` je Material zu lesen. Das war fuer verkaufte Artikel plausibel, aber in der Praxis nicht vollstaendig.

Test mit `sapdataexport.csv`:

- SAP-Materialzeilen: 42'232
- Zugeordnet: 34'462
- Nicht zugeordnet: 7'770
- Davon laut Excel/KEDE-Regeln fachlich abgedeckt: 7'768
- Uebrige echte Luecke: `PAPH1 = 8950`

Schlussfolgerung:

`CE11000` reicht als primaere Quelle nicht aus, weil Materialien ohne passende CO-PA-Belegableitung oder mit anderer Beleglage fehlen koennen. Die Regeln muessen aus KEDE/KEDR kommen.

## Verifizierte SAP-Quellen

Systemkontext:

- S/4HANA, S4CORE 108
- Ergebnisbereich: `ERKRS = 1000`
- KEDR-Strategie: `DERI`
- Applikationsklasse: `KE`
- Fuehrende Pflege in KEDE/KEDR

Verifizierte Tabellen aus `TKEDRS`:

- Schritt `0028`
  - `METHOD = DRULE`
  - `KEDRENV = 1000`
  - `PARAM_1 = K9RT761000002`
  - Bedeutung: `PAPH1 von-bis -> WWPFA`
- Schritt `0031`
  - `METHOD = DRULE`
  - `KEDRENV = 1000`
  - `PARAM_1 = K9RT761000003`
  - Bedeutung: `WWPFA von-bis -> WWPSP`

Feldstruktur `K9RT761000002`:

- `SOUR1_FROM` CHAR5: ProdHierarchie01-1 von
- `SOUR1_TO` CHAR5: ProdHierarchie01-1 bis
- `VALID_FROM` DATS
- `TARGET1` CHAR6: Produktfamilie
- `DELETE_FLG`
- `ADDED_BY`
- `ADDED_ON`

Feldstruktur `K9RT761000003`:

- `SOUR1_FROM` CHAR6: Produktfamilie von
- `SOUR1_TO` CHAR6: Produktfamilie bis
- `VALID_FROM` DATS
- `TARGET1` CHAR6: Produktsparte
- `DELETE_FLG`
- `ADDED_BY`
- `ADDED_ON`

Weitere relevante Tabellen:

- `MVKE-PRODH`: Produkthierarchie am Material, CHAR18, bei Trafag fachlich 4-stellig gepflegt
- `MAKT`: Materialkurztext
- `T179T`: Text zur Produkthierarchie
- `T25A0`: Texte Produktfamilie `WWPFA`
- `T25A1`: Texte Produktsparte `WWPSP`
- `ZPRODSPARTE_MAP`: flache Mapping-Tabelle `PAPH1 -> WWPFA -> WWPSP`

## Von-bis-Logik

KEDE pflegt die Produktfamilie nicht nur als Einzelwerte, sondern als Bereiche.

Beispiele:

- `0104` bis `0199` -> `WWPFA = 0001`
- `8412` bis `8413` -> `WWPFA = 0028`
- `8280` ohne bis -> Einzelwert `8280` -> `WWPFA = 0032`

Regeln:

- Wenn `SOUR1_TO` leer ist, gilt die Zeile als Einzelwert.
- Numerische Bereiche werden mit fuehrenden Nullen auf die urspruengliche Breite expandiert.
- Alphanumerische 4-stellige Bereiche mit gleichem 2-stelligem Praefix werden ueber die Zeichenfolge `0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ` expandiert.
- Der Vergleich fuer die Regelauflosung bleibt CHAR-/String-basiert, passend zu KEDR.
- Zeilen mit `DELETE_FLG = X` werden ignoriert.
- Bei Ueberlappungen gewinnt die neueste `VALID_FROM`.
- Bei gleicher `VALID_FROM` gewinnt der spezifische Einzelwert gegenueber einem Bereich.

Wichtig: `ZPRODSPARTE_MAP` braucht keine Felder `PAPH1_VON`, `PAPH1_BIS` oder `GUELTAB`. Die Von-bis-Logik wird beim Build auf einzelne PAPH1-Zeilen aufgeloest.

## ABAP-Objekte

### `Z_PRODSPARTE_KEDR_K9R_FIND`

Diagnose-Report zur Ermittlung der relevanten KEDR/K9R-Tabellen.

Ergebnis:

- `K9RT761000002` ist die Regel `ProdHierarchie01-1 -> Produktfamilie`.
- `K9RT761000003` ist die Regel `Produktfamilie -> Produktsparte`.

### `Z_PRODSPARTE_MAP_BUILD`

Finaler Build-Report fuer `ZPRODSPARTE_MAP`.

Aufgaben:

- liest `K9RT761000002`
- liest `K9RT761000003`
- expandiert alle PAPH1-Von-bis-Regeln
- nimmt optional zusaetzliche reale PAPH1-Codes aus `MVKE` und `CE11000` auf
- loest `PAPH1 -> WWPFA`
- loest `WWPFA -> WWPSP`
- schreibt die flache Tabelle `ZPRODSPARTE_MAP`

Parameter:

- `P_VKORG`: optional, fuer reale Zusatzcodes aus `MVKE`
- `P_VTWEG`: optional, fuer reale Zusatzcodes aus `MVKE`
- `P_CE`: optional, liest zusaetzliche reale PAPH1 aus `CE11000`
- `P_TEST`: Testlauf ohne DB-Schreiben

Letzter Testlauf:

- KEDR-Regeln `PAPH1 -> WWPFA`: 124
- KEDR-Regeln `WWPFA -> WWPSP`: 81
- PAPH1-Codes aus KEDE-Expansion: 1'297
- Zusaetzliche reale Code-Versuche: 689
- Nicht expandierbare Bereiche: 0
- PAPH1-Codes gesamt eindeutig: 1'297
- Saetze fuer `ZPRODSPARTE_MAP`: 1'296
- PAPH1 ohne Produktfamilie: 1
- PAPH1 ohne Produktsparte: 0

Interpretation:

- Die 1'296 Mapping-Saetze entsprechen der Excel/Data(4)-Referenz.
- Der eine zusaetzliche PAPH1 ohne Produktfamilie ist die bekannte Luecke `8950`.
- `8950` kommt aus realen SAP-Daten, ist aber nicht in KEDE/Excel gepflegt.

### `Z_PRODSPARTE_MAP_EXPORT`

Exportiert die flache Tabelle `ZPRODSPARTE_MAP` fuer den Kontrollvergleich gegen Excel/Data(4).

Exportformat:

`PAPH1;WWPFA;WWPSP`

Beispiel-Datei:

`C:\temp\zprodspartesap3.csv`

Dieser Export ist der richtige Vergleich gegen Excel/Data(4), nicht der Materialexport.

### `ZCL_PRODSPARTE_PROVIDER`

Globale Klasse mit statischer Methode `GET_DATA`.

Wichtige Signatur:

- `IV_VKORG TYPE VKORG`
- `IV_VTWEG TYPE VTWEG OPTIONAL`
- `IV_SPRAS TYPE SPRAS DEFAULT SY-LANGU`
- `IV_FALLBACK TYPE BEZEK DEFAULT 'Nicht zugeordnet'`
- `VALUE(RT_OUT) TYPE TT_OUT`

Rueckgabefelder:

- `MATNR`
- `MAKTX`
- `PAPH1`
- `PAPH1_TEXT`
- `WWPFA`
- `WWPFA_TEXT`
- `WWPSP`
- `WWPSP_TEXT`
- `IS_ASSIGNED`

Logik:

- liest Materialdaten aus `MVKE`
- liest Materialtext aus `MAKT`
- setzt `PAPH1 = MVKE-PRODH(5)`
- liest `ZPRODSPARTE_MAP`
- setzt `IS_ASSIGNED = X`, wenn ein Mapping gefunden wurde
- setzt sonst Fallback `WWPSP = UNASS`, Text `Nicht zugeordnet`
- liest Texte aus `T179T`, `T25A0`, `T25A1`

Wichtig:

`GET_DATA` muss in SE24 als `CLASS-METHODS` angelegt sein. Sonst kommt der Fehler:

`Die Angabe "class=>method" darf nur bei statischen Methoden verwendet werden.`

### `Z_PRODSPARTE_ALL`

Ausfuehrbarer Report fuer Materialexport und ALV-Kontrolle.

Aufgaben:

- ruft `ZCL_PRODSPARTE_PROVIDER=>GET_DATA`
- zeigt optional ALV
- exportiert optional tab-getrennte CSV per `cl_gui_frontend_services=>gui_download`

Dieser Export enthaelt Materialzeilen. Er ist nicht identisch mit der Excel/Data(4)-Mappingreferenz, weil Excel auch PAPH1-Codes ohne aktuelles Material enthalten kann.

### `PRODUCTDIVISIONR_GET_ENTITYSET`

OData-GET_ENTITYSET-Methode fuer die Produktspartenreferenz.

Aufgaben:

- verwendet ohne Filter die Default-TR-AG-Verkaufsorganisation `VKORG = 1100`
- liest optional `VKORG`, falls die Entity spaeter um dieses Property erweitert wird
- liest optional `VTWEG`
- liest optional `SPRAS`
- ruft `ZCL_PRODSPARTE_PROVIDER=>GET_DATA`
- gibt die Daten per `CORRESPONDING` an das EntitySet zurueck

Wichtig: Die aktuelle Gateway-Metadata fuer `ProductDivisionRef` enthaelt kein Property `VKORG`.
Darum darf die DPC_EXT-Methode `VKORG` nicht als OData-Pflichtfilter erzwingen. Sonst kann die App
`ProductDivisionRefSet` nicht laden: ohne Filter kommt `Filter VKORG ist erforderlich`, mit Filter
kommt `Property VKORG not found in type ProductDivisionRef`.

### `Z_PRODSPARTE_MAP_IMPORT`

Nur noch Fallback/Hilfsprogramm.

Es kann CSV/Excel-Daten in die Mapping-Tabelle bringen, ist aber fuer die finale Architektur nicht die fuehrende Loesung. Fuehrend ist KEDE/KEDR ueber `Z_PRODSPARTE_MAP_BUILD`.

## Validierung

### Materialexport nach KEDR-Build

Datei:

`prodspartesap2.csv`

Ergebnis:

- Materialzeilen: 42'232
- Zugeordnet: 42'230
- Nicht zugeordnet: 2
- Nicht zugeordneter PAPH1: `8950`
- WWPFA-Abweichungen gegen Excel fuer abgedeckte Codes: 0

Interpretation:

Die Materialdaten sind bis auf die fachlich ungepflegte Luecke `8950` zugeordnet.

### Mappingexport gegen Excel/Data(4)

Dateien:

- Excel-Referenz: `exceldataexport.csv`
- SAP-Mappingexport: `C:\temp\zprodspartesap3.csv`

Vergleichsergebnis:

```text
Excel-Rohzeilen:              897
Excel expandiert auf PAPH1:  1296
SAP-Mapping-Zeilen:          1296
Fehlt in SAP:                   0
Zusätzlich in SAP:              0
WWPFA-Abweichungen:             0
Duplikate PAPH1:                0
Leere WWPSP:                    0
```

Fazit:

`ZPRODSPARTE_MAP` entspricht der Excel/Data(4)-Referenz vollstaendig fuer `PAPH1 -> WWPFA`. `WWPSP` ist zusaetzlich aus der zweiten KEDE-Regel `K9RT761000003` gefuellt.

## Offene fachliche Punkte

### OData-Import `ProductDivisionRefSet`

Am 2026-06-10 wurde der echte App-Import fuer `ZSCHWEIZ` getestet.

Konfiguration in der App-DB:

- Site-ID: `9`
- TSC: `ZSCHWEIZ`
- SAP-Service: `ZPOWERBI_EINKAUF_SRV`
- aktive Quelle `Z`: `FinanzdataSchweizOeSet`
- aktive Quelle `P`: `ProductDivisionRefSet`
- aktiver Join: `Z.Matnr = P.Matnr`
- Spartenfelder werden aus `P.Paph1`, `P.Wwpfa`, `P.Wwpsp`, `P.IsAssigned` gemappt

Gefundener Fehler vor ABAP-Korrektur:

```text
ProductDivisionRefSet?$format=json
HTTP 400
Filter VKORG ist erforderlich

ProductDivisionRefSet?$format=json&$filter=VKORG eq '1100'
HTTP 400
Property VKORG not found in type ProductDivisionRef
```

Metadata `ProductDivisionRef`:

```text
Matnr
Maktx
Paph1
Paph1Text
Wwpfa
WwpfaText
Wwpsp
WwpspText
IsAssigned
```

Korrektur:

- `ZCL_PRODSPARTE_DPC_EXT_PRODUCTDIVISIONR_GET_ENTITYSET.abap` setzt `lv_vkorg = '1100'` als Default.
- Die Exception `Filter VKORG ist erforderlich` wurde entfernt.
- Damit kann die bestehende App `ProductDivisionRefSet` ohne Filter laden.

Nach Aktivierung dieser ABAP-Aenderung in SAP muss der `ZSCHWEIZ`-Import erneut laufen.

Status nach Aktivierung `VKORG = 1100`:

- `ProductDivisionRefSet` liefert 42'232 Zeilen.
- Beispielzeilen sind korrekt gefuellt, z.B. `Matnr=6`, `Paph1=0414`, `WWPFA=0004`, `WWPSP=0001`, `IsAssigned=True`.
- Der gezielte App-Import fuer `ZSCHWEIZ` lief erfolgreich durch.
- Importierte CH/AT-Zeilen: 40'292
- Davon mit Spartenreferenz zugeordnet: 36'847
- `Nicht zugeordnet` mit vorhandener Referenz/`UNASS`: 0
- Ohne Treffer in `ProductDivisionRefSet`: 3'445

Aufteilung nach TSC:

```text
TSC   Rows    Assigned  Nicht zugeordnet  Kein Referenztreffer
TRCH  38'838  35'524    0                 3'314
TRAT   1'454   1'323    0                   131
```

Interpretation:

- Die eigentliche Spartenlogik im Webservice ist fuer CH/AT jetzt sauber: kein `UNASS`/`Nicht zugeordnet`.
- Die verbleibenden Zeilen sind kein Spartenregelproblem, sondern Materialnummern ohne Treffer in der TR-AG-Referenz.

## Dashboard-Fallback fuer fehlende Materialreferenzen

Es gibt Verkaufszeilen, bei denen der materialbasierte OData-Service `ProductDivisionRefSet` keinen Treffer liefert. Ein Teil dieser Zeilen hat aber in der Verkaufsquelle noch eine Produktgruppe/Produkthierarchie (`Z.Prodh`). Deshalb wurde ein zweiter, flacher OData-Service vorgesehen:

- `ProductDivisionRefSet`: Materialnummer `MATNR` -> `PAPH1/WWPFA/WWPSP`
- `ProductDivisionMapSet`: Produkthierarchie `PAPH1` -> `WWPFA/WWPSP`

Der neue ABAP-GET_ENTITYSET fuer `ProductDivisionMapSet` liest direkt aus `ZPRODSPARTE_MAP` und liefert eine Zeile pro `PAPH1`. Im Dashboard wird dieser Service als Quelle `M` angebunden und per Left Join auf die Verkaufsdaten gelegt:

```text
Z.Prodh = M.Paph1
```

Die Mapping-Felder verwenden danach `FirstNonEmpty(P.*, M.*)`. Das bedeutet:

- Wenn die materialbasierte Referenz `P` einen Treffer liefert, gewinnt `P`.
- Wenn `P` leer ist, aber `Z.Prodh` in `M` gefunden wird, werden Familie und Sparte aus der flachen Mapping-Tabelle genommen.
- Wenn auch `Z.Prodh` leer ist oder nicht in `ZPRODSPARTE_MAP` steht, bleibt die Zeile ohne Produktsparten-Zuordnung.

Aktueller CH/AT-Befund vor diesem Fallback:

- 3'445 Verkaufszeilen hatten keinen Materialreferenztreffer.
- 106 davon hatten `Z.Prodh = 9999`.
- `9999` ist in Excel/Data(4) und in `ZPRODSPARTE_MAP` vorhanden: `9999 -> 0043 -> 0008`.
- Diese 106 Zeilen sollten durch `ProductDivisionMapSet` zuordenbar werden.
- Die restlichen Zeilen mit leerer Produktgruppe koennen technisch nicht zugeordnet werden, solange SAP keine Produktgruppe/Materialreferenz liefert.

Verifizierter Stand nach Aktivierung von `ProductDivisionMapSet`:

- `$metadata` enthaelt `ProductDivisionMap` mit `WwpfaText` korrekt geschrieben.
- `ProductDivisionMapSet` liefert HTTP 200 und 1'296 Zeilen.
- Lokaler CH/AT-Import verwendet Quellen `Z`, `P`, `M` und zwei Left Joins.
- Ergebnis CH/AT gesamt: 40'292 Verkaufszeilen, 36'953 zugeordnet, 0 `UnassignedWithReference`.
- Gegenueber dem vorherigen Stand sind genau 106 zusaetzliche Zeilen zugeordnet.
- Rest ohne Referenz: TRCH 3'312, TRAT 27; bei diesen Zeilen ist `ProductGroup/Z.Prodh` leer.

## Komponenten-Fallback ueber `ZPOWERBI_VC_TXT`

Am 2026-06-11 wurde ein weiterer fachlicher Sonderfall analysiert:

- In CH/TRCH-Verkaufszeilen stehen nicht nur verkaufsfaehige Kopfmaterialien, sondern auch Komponenten/Einbauteile mit Buchstabenpraefix.
- Die Datei `nicht_im_stamm_TRCH_alle_jahre.csv` enthaelt 804 distinct TRCH-Materialien, die nicht in `ProductDivisionRefSet` gefunden wurden.
- Alle 804 Materialien haben Buchstabenpraefix, z.B. `E01758`, `E01752`, `E00613`, `R85012`, `B56237`, `B87813`, `C15414`, `D34604`.
- Diese Materialien sind fachlich meist Komponenten, die in einem Kopfmaterial verbaut sind.

Relevante SAP-Tabelle:

`ZPOWERBI_VC_TXT`

Wichtige Felder:

- `KOMPNR`: Komponente / Einbauteil
- `MATNR`: Kopfmaterial / Elternmaterial
- `STUFE`: Stuecklistenstufe
- `KOM_MSTAE`, `KOM_MSTAV`: Status der Komponente
- `MAT_MSTAE`, `MAT_MSTAV`: Status des Kopfmaterials
- `MAT_TXT`: Text zum Kopfmaterial

Beispiel aus SAP:

```text
KOMPNR = E01758
MATNR  = 37 / 41 / 59
STUFE  = 1 oder 2
```

Die erste Annahme `STUFE = 1` war zu eng. In echten Daten liegen relevante Komponenten auch auf `STUFE = 2` oder `STUFE = 3`, z.B. `E00613` und `R85012`.

### Minimaler ABAP-Eingriff

Die Aenderung gehoert bewusst in `ZCL_PRODSPARTE_PROVIDER=>GET_DATA`, nicht in KEDE/KEDR und nicht in `ZPRODSPARTE_MAP`.

Logik:

1. Normale Referenz wie bisher bauen:
   `MVKE-MATNR -> MVKE-PRODH(5) -> ZPRODSPARTE_MAP`.
2. Danach Komponenten aus `ZPOWERBI_VC_TXT` lesen:
   `KOMPNR -> MATNR`.
3. Wenn `MATNR` als Kopfmaterial bereits im Provider assigned ist, wird die Komponente als zusaetzliche `ProductDivisionRefSet`-Zeile erzeugt.
4. Es werden alle `STUFE` beruecksichtigt.
5. Automatische Uebernahme erfolgt nur, wenn alle gefundenen Kopfmaterialien dieselbe `WWPSP` ergeben.
6. Mehrdeutige Komponenten werden bewusst nicht automatisch zugeordnet.

Damit bleibt die OData-Schnittstelle unveraendert:

```text
ProductDivisionRefSet
Matnr / Maktx / Paph1 / Wwpfa / Wwpsp / IsAssigned
```

SEGW und Web-App brauchen fuer diese Minimalvariante keine Metadatenaenderung. Der bestehende Join bleibt:

```text
Z.Matnr = P.Matnr
```

### Aktuelle Verifikation 2026-06-11

Prod-URL nach Transport:

```text
http://travp762.sap.trafag.com:8000/sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/
```

Prod-Metadata enthaelt wieder:

```text
FinanzdataSchweizOeSet
ProductDivisionRefSet
ProductDivisionMapSet
```

Live-Pruefung gegen `nicht_im_stamm_TRCH_alle_jahre.csv`:

```text
ProductDivisionRefSet rows: 42'486
Alpha-MATNR rows:          4'433
CSV materials:               804
Jetzt gefunden:                0
Weiterhin fehlend:           804
```

Interpretation:

- Der Prod-Service ist erreichbar und die EntitySets sind registriert.
- `ProductDivisionRefSet` liefert 254 Zeilen mehr als Test (`travt762`: 42'232), aber diese Mehrzeilen sind numerische Materialien, nicht die Komponenten aus der CSV.
- Der Komponenten-Fallback ist in der aktuell abgefragten Prod-Ausgabe noch nicht wirksam.

Naechste SAP-Pruefung:

1. In SAP `ZCL_PRODSPARTE_PROVIDER=>GET_DATA` bzw. `Z_PRODSPARTE_ALL` direkt ausfuehren.
2. Nach Beispielkomponenten suchen, z.B. `E01758`, `E01752`, `E00613`, `R85012`.
3. Wenn sie im Provider fehlen: Fallback-Code/Tabellenzugriff `ZPOWERBI_VC_TXT` pruefen.
4. Wenn sie im Provider vorhanden sind, aber im OData fehlen: Gateway-Implementierung/Cache pruefen.
5. Danach erneut `ProductDivisionRefSet` gegen `nicht_im_stamm_TRCH_alle_jahre.csv` abgleichen.

### `PAPH1 = 8950`

`8950` taucht in echten SAP-Daten auf, ist aber nicht in KEDE/Excel gepflegt.

Auswirkung:

- Im Mappingexport fehlt `8950` korrekt, weil Excel/KEDE es auch nicht enthaelt.
- Im Materialexport bleibt `8950` als nicht zugeordnet.

Moegliche Ursachen:

- Material-PRODH ist falsch oder veraltet.
- Finance hat fuer `8950` noch keine KEDE-Regel gepflegt.
- Der Code soll fachlich bewusst nicht zugeordnet sein.

Naechster Schritt:

Finance/SAP-Verantwortliche muessen entscheiden, ob fuer `8950` eine KEDE-Regel gepflegt wird oder ob der Materialstamm korrigiert wird.

## Bedienablauf fuer naechste Pruefung

1. `Z_PRODSPARTE_MAP_BUILD` mit `P_TEST = X` laufen lassen.
2. Pruefen:
   - `Nicht expandierbare Bereiche = 0`
   - `PAPH1 ohne Produktsparte = 0`
   - `PAPH1 ohne Produktfamilie` nur bekannte Luecken, aktuell `8950`
3. `Z_PRODSPARTE_MAP_BUILD` ohne `P_TEST` laufen lassen.
4. `Z_PRODSPARTE_MAP_EXPORT` laufen lassen.
5. Export gegen Excel/Data(4) vergleichen.
6. `Z_PRODSPARTE_ALL` laufen lassen, um Materialzuordnung zu pruefen.
7. OData-Service testen.

## Wichtige Unterscheidung der CSV-Dateien

`prodspartesap2.csv` oder Export aus `Z_PRODSPARTE_ALL`:

- Materialexport
- eine Zeile pro Material
- darf weniger PAPH1-Codes enthalten als Excel, weil nicht jeder Referenzcode ein Material haben muss
- dient zur Dashboard-/Materialpruefung

`zprodspartesap3.csv` oder Export aus `Z_PRODSPARTE_MAP_EXPORT`:

- Mappingexport
- eine Zeile pro PAPH1
- muss gegen Excel/Data(4) identisch sein bei `PAPH1 -> WWPFA`
- ist die richtige Datei fuer den Regelabgleich

## Bekannte technische Stolperstellen

- `GET_DATA` muss statisch sein, wenn mit `zcl_prodsparte_provider=>get_data` aufgerufen wird.
- `TEXT-001` im Report `Z_PRODSPARTE_ALL` muss als Textsymbol existieren.
- CSV-Downloads mit `cl_gui_frontend_services=>gui_download` funktionieren nur im SAP GUI.
- `CORRESPONDING #( lt_data )` im OData-Code funktioniert nur bei passenden Feld-/Property-Namen.
- `PAPH1 = MVKE-PRODH(5)` ist aktuell korrekt, weil KEDE `SOUR1_FROM` in `K9RT761000002` CHAR5 ist.
- `T179T-PRODH` ist CHAR18; falls `PAPH1_TEXT` leer bleibt, muss der Text-Key linksbuendig/padding-geprueft werden.
- `DELETE ADJACENT DUPLICATES COMPARING matnr` im Provider nimmt bei mehreren Vertriebswegen den ersten sortierten Satz.

## Dateien im Ordner `spartenlogic`

- `Z_PRODSPARTE_KEDR_K9R_FIND.abap`: Diagnose der TKEDRS/K9R-Regeltabellen
- `Z_PRODSPARTE_MAP_BUILD.abap`: finaler Build aus KEDE/KEDR nach `ZPRODSPARTE_MAP`
- `Z_PRODSPARTE_MAP_EXPORT.abap`: Export der flachen Mapping-Tabelle
- `ZCL_PRODSPARTE_PROVIDER.abap`: Provider fuer Materialdaten und Mapping-Lookup
- `Z_PRODSPARTE_ALL.abap`: ALV/CSV-Materialexport
- `ZCL_PRODSPARTE_DPC_EXT_PRODUCTDIVISIONR_GET_ENTITYSET.abap`: OData-GET_ENTITYSET-Methode
- `ZCL_PRODSPARTE_DPC_EXT_PRODUCTDIVISIONM_GET_ENTITYSET.abap`: OData-GET_ENTITYSET fuer die flache PAPH1-Mappingquelle `ProductDivisionMapSet`
- `Z_PRODSPARTE_MAP_IMPORT.abap`: Fallback-Import, nicht fuehrend
- `exceldataexport.csv`: Excel/Data(4)-Referenzexport
- `prodspartesap2.csv`: Materialexport nach KEDR-Build
- `markregell.png`: Screenshot der KEDE-von/bis-Regel
- `ruleresult.txt`: Diagnoseergebnis aus SAP

## Fazit

Die Produktspartenlogik ist fachlich und technisch final auf KEDE/KEDR als Quelle ausgerichtet. Die flache Tabelle `ZPRODSPARTE_MAP` wird aus den SAP-Regeln aufgebaut und wurde gegen Excel/Data(4) ohne Abweichung validiert. Der Dashboard-/OData-Pfad nutzt diese Tabelle als Lookup: zuerst materialbasiert ueber `ProductDivisionRefSet`, danach als Fallback ueber `ProductDivisionMapSet` anhand `Z.Prodh`. Die einzige bekannte fachliche Luecke ist `PAPH1 = 8950`; Zeilen ohne Produktgruppe bleiben mangels Schluessel nicht zuordenbar.
