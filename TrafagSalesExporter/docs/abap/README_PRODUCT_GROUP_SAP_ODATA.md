# Produktgruppen direkt aus SAP OData

Stand: 2026-08-11

## Ziel

Die Produktgruppenzuordnung des Einkaufsdashboards kommt vollstaendig aus SAP.
Die Dateien `zdispo_grp.xlsx` und `zdispo_spart.xlsx` sind weder Laufzeitquelle
noch Deploy-Artefakte.

Die fachliche Kette lautet:

`EKPO-MATNR -> ZLO03-KOMPNR -> VKNR -> MARC-DISPO -> ZDISPO_GRP -> ZDISPO_SPART`

## SAP-Objekte

Im bestehenden Gateway-Service `ZPOWERBI_EINKAUF_SRV` werden zwei kleine
EntitySets benoetigt:

| EntitySet (empfohlen) | SAP-Quelle | Felder | OData-Key |
| --- | --- | --- | --- |
| `ZDISPO_GRPSet` | `ZDISPO_GRP` | `DISPO_KZ`, `DISPO` | `DISPO_KZ` + `DISPO` |
| `ZDISPO_SPARTSet` | `ZDISPO_SPART` | `DISPO`, `DESCR` | `DISPO` |

Methodenruempfe:

- `docs/abap/ZDISPO_GRP_GET_ENTITYSET.abap`
- `docs/abap/ZDISPO_SPART_GET_ENTITYSET.abap`

Vor der Anlage in SE11 pruefen, dass die beiden Tabellennamen und Feldnamen im
Zielsystem genau so aktiv sind. Die Namen stammen aus den bislang gelieferten
ZDISPO-Listenausgaben; die Aktivierung selbst muss im SAP-System erfolgen.

## SEGW-Schritte

1. In `ZPOWERBI_EINKAUF_SRV` beide DDIC-Strukturen/Tabellen als Entity Types
   importieren und Related Entity Sets erzeugen.
2. Die oben genannten Keys setzen. `DISPO_KZ` allein ist nicht eindeutig, weil
   Mehrfachzuordnungen fachlich erhalten bleiben muessen.
3. Runtime Objects neu generieren.
4. Nur die beiden neuen `GET_ENTITYSET`-Methoden im DPC_EXT redefinieren und die
   bereitgestellten Methodenruempfe einsetzen.
5. Aktivieren und Gateway-Metadaten-Cache leeren (`/IWFND/CACHE_CLEANUP`).
6. Service Root und beide Sets testen:

```text
.../sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/$metadata
.../sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/ZDISPO_GRPSet?$format=json
.../sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/ZDISPO_SPARTSet?$format=json
```

## Verhalten der Anwendung

- Full Load und Delta lesen bei jedem Lauf beide EntitySets direkt aus SAP.
- Die Anwendung fuehrt `DISPO_KZ -> DISPO` und `DISPO -> DESCR` im Speicher
  zusammen und ersetzt den lokalen Cache atomar.
- Exakter Disponent gewinnt vor Sternmuster; das laengste passende Muster
  gewinnt. Mehrere Gruppen desselben Musters bleiben fuer die 1/n-Verteilung
  erhalten.
- Leere oder fehlende SAP-EntitySets erzeugen einen klaren Fehler. Der bestehende
  Cache wird dabei nicht geloescht.
- Nur Cachezeilen mit `Source = SAP OData: ...` werden ausgewertet. Alte Excel-
  oder manuelle Zuordnungen koennen die Anzeige deshalb nicht mehr beeinflussen.
- Alternativ akzeptiert der Client ein bereits zusammengefuehrtes EntitySet,
  dessen Name `ProductGroupMap`, `ZC23ProductGroup` oder `ZSTRProductGroup`
  enthaelt und die Felder `DisponentPattern`, `ProductGroup` und
  `ProductGroupText` liefert.
