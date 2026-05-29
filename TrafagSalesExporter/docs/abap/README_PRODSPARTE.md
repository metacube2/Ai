# ABAP Produktsparten-Mapping

Stand: 2026-05-28

## Dateien

| Datei | Zweck |
| --- | --- |
| `ZCL_PRODSPARTE_PROVIDER.abap` | Wiederverwendbare Provider-Klasse fuer ALV und spaeter OData |
| `Z_PRODSPARTE_REPORT.abap` | Schlanker ALV-Testreport |
| `Z_PRODSPARTE_MAP_BUILD.abap` | Baut `ZPRODSPARTE_MAP` aus eindeutigen CO-PA-Kombinationen |

## Benoetigte SAP-Objekte

- Transparente Tabelle `ZPRODSPARTE_MAP`
  - `MANDT`
  - `PAPH1`
  - `WWPFA`
  - `WWPSP`
  - `CRDATE`
  - `CRUSER`
- Klasse `ZCL_PRODSPARTE_PROVIDER`
- Report `Z_PRODSPARTE_REPORT`
- Report `Z_PRODSPARTE_MAP_BUILD`

## Anlage In SAP

- `ZCL_PRODSPARTE_PROVIDER.abap` ist eine globale Klasse bzw. ein Class Pool, kein ausfuehrbarer Report.
  - In SE24 als Klasse `ZCL_PRODSPARTE_PROVIDER` anlegen und Definition/Implementation uebernehmen.
  - Alternativ in SE38/ADT als Programtyp `Class Pool` anlegen; die Datei beginnt deshalb mit `CLASS-POOL zcl_prodsparte_provider`.
- `Z_PRODSPARTE_REPORT.abap` und `Z_PRODSPARTE_MAP_BUILD.abap` sind normale ausfuehrbare Reports.

Optional fuer Gateway/DDIC:

- Struktur `ZSTR_PRODSPARTE_OUT`
- Tabellentyp `ZTT_PRODSPARTE_OUT`

## DDIC-Hinweis Zu `ZSTR_PRODSPARTE_OUT`

Wenn `ZSTR_PRODSPARTE_OUT` in SE11 angelegt wird, duerfen `PAPH1`,
`WWPFA` und `WWPSP` nicht blind als Komponententypen eingetragen werden.
In manchen SAP-Systemen sind das keine aktiven globalen Datenelemente,
sondern nur Feldnamen in der CO-PA-Tabelle `CE11000`. Dann kann die
Nametab der Struktur nicht generiert werden.

Empfohlene Anlage:

| Komponente | Komponententyp / Alternative |
| --- | --- |
| `MATNR` | `MATNR` |
| `MAKTX` | `MAKTX` |
| `PAPH1` | eigenes Datenelement `ZDE_PAPH1` mit Laenge wie `CE11000-PAPH1`; aktuell fachlich erwartet: `CHAR 5` |
| `PAPH1_TEXT` | `VTEXT` oder eigenes Text-Datenelement |
| `WWPFA` | eigenes Datenelement `ZDE_WWPFA` mit exakt gleicher Laenge wie `CE11000-WWPFA` |
| `WWPFA_TEXT` | `BEZEK` oder eigenes Text-Datenelement |
| `WWPSP` | eigenes Datenelement `ZDE_WWPSP` mit exakt gleicher Laenge wie `CE11000-WWPSP` |
| `WWPSP_TEXT` | `BEZEK` oder eigenes Text-Datenelement |
| `IS_ASSIGNED` | `BOOLE_D` oder `XFELD` |

Alternativ kann die Struktur fuer den ersten ALV-Test entfallen, weil die
Provider-Klasse intern mit `CE11000-PAPH1`, `CE11000-WWPFA` und
`CE11000-WWPSP` typisiert. Fuer Gateway/OData ist eine globale Struktur
aber sinnvoll.

## Gepruefte Anpassungen Gegenueber Erstentwurf

- Provider-Logik aus Report in globale Klasse ausgelagert.
- `MAKT` als `LEFT OUTER JOIN`, damit Materialien ohne Text nicht verloren gehen.
- `VTWEG` als optionaler Parameter.
- Bei mehreren Vertriebswegen gewinnt bewusst der kleinste `VTWEG`.
- Fallback setzt technischen Code `UNASS`, Text `Nicht zugeordnet` und `IS_ASSIGNED = abap_false`.
- `gt_ambig` im Mapping-Build ist korrekt als `ty_combo` typisiert.
- `p_erkrs` wurde entfernt, weil der Report fix aus `CE11000` liest.
- Leerschreiben von `ZPRODSPARTE_MAP` wird verhindert, wenn keine eindeutigen Saetze aufgebaut wurden.

## Noch Fachlich/Technisch Zu Pruefen

- Ist `PAPH1 = MVKE-PRODH(5)` im Trafag-System exakt korrekt?
- Sind `T25A0` fuer Produktfamilie und `T25A1` fuer Produktsparte die richtigen Texttabellen?
- Ist `CE11000` der richtige CO-PA-Einzelposten fuer den relevanten Ergebnisbereich?
- Ist Fallback-Code `UNASS` in Feld `WWPSP` lang genug/zulässig?
- Soll `VTWEG` zwingend selektiert werden statt "kleinster VTWEG gewinnt"?

## Gateway-Stand 2026-05-29

Der bestehende Gateway-Service wurde erweitert, statt einen separaten
Service zu verwenden:

- Service: `ZPOWERBI_EINKAUF_SRV`
- Service Root: `http://travt762.sap.trafag.com:8000/sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/`
- Entity Type: `ProductDivisionRef`
- Entity Set: `ProductDivisionRefSet`
- DDIC-Struktur fuer Entity Type: `ZSTR_PRODSPARTE_OUT`

Wichtig:

- `FINANZDATASCHWEI_GET_ENTITYSET` gehoert zum bestehenden Sales-EntitySet
  und muss den bisherigen `SELECT * FROM zschweiz ...` behalten.
- Produktspartenlogik gehoert in die separat generierte/redefinierte Methode
  `PRODUCTDIVISIONR_GET_ENTITYSET`.
- Wenn `/IWFND/MED/170` mit einem Servicenamen wie
  `ZPOWERBI_EINKAUF_SRVPRODUCTDIVISIONRSET` erscheint, fehlt in der URL der
  Slash zwischen Service und EntitySet.

Korrekte Test-URL:

```text
http://travt762.sap.trafag.com:8000/sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/ProductDivisionRefSet
```

Gateway-Feldnamen, wie sie im Web-Mapping verwendet werden:

| Gateway-Feld | Web-Zielfeld |
| --- | --- |
| `Matnr` | Join gegen `Z.Matnr` |
| `Paph1` | `ProductHierarchyCode` |
| `Paph1Text` | `ProductHierarchyText` |
| `Wwpfa` | `ProductFamilyCode` |
| `WwpfaText` | `ProductFamilyText` |
| `Wwpsp` | `ProductDivisionCode` |
| `WwpspText` | `ProductDivisionText` |
| `IsAssigned` | `ProductMappingAssigned` |
