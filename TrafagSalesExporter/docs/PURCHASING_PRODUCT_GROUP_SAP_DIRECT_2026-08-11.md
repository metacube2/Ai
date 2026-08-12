# Einkaufs-Produktgruppen direkt aus SAP

Stand: 2026-08-11

## Produktiver Abschluss 2026-08-12

Die beiden SAP-EntitySets sind produktiv aktiv und die SAP-only-Strecke ist live
abgenommen:

- produktives `$metadata`: HTTP 200, `62` EntitySets;
- `ZDISPO_GRPSet`: HTTP 200, `45` Zeilen und `42` unterschiedliche Muster;
- `ZDISPO_SPARTSet`: HTTP 200, `22` Zeilen;
- produktiver Einkauf-Delta vom 2026-08-12: `Success`, abgeschlossen um
  `10:03:42 MESZ`;
- lokaler Produktivcache danach: `45` Regeln mit `Source = SAP OData: ...`,
  `0` Regeln mit Excel-, manueller oder anderer Nicht-SAP-Quelle;
- Spend-Aufriss und Materialdisposition liefern nach dem Delta HTTP 200.

Damit ist Excel auch im produktiven Datenbestand als aktive Mappingquelle
vollstaendig ersetzt. `zdispo_grp.xlsx` und `zdispo_spart.xlsx` sind weder
Laufzeitquelle noch Fallback noch aktive Cachequelle.

Zwei SAP-Nacharbeiten blockieren den Betrieb nicht:

1. `ZDISPO_SPART` liefert fuer die Codes `D1` und `D5` keinen Text. Die Anwendung
   zeigt deshalb gemaess Fallback den jeweiligen SAP-Code an.
2. In den produktiven OData-Metadaten hat `ZDISPO_GRP` derzeit nur `DISPO` als
   Key. Da `DISPO` in neun Gruppen mehrfach vorkommt, sollte SEGW auf den
   zusammengesetzten Key `DISPO_KZ + DISPO` korrigiert werden. Der aktuelle
   EntitySet-Read liefert trotzdem alle `45` Zeilen, und der Client verarbeitet
   sie korrekt.

## Ergebnis

Die Anwendung ist auf eine ausschliessliche SAP-Datenstrecke umgestellt:

`EKPO -> ZLO03/VknrDispo -> SAP ZDISPO_GRP -> SAP ZDISPO_SPART -> lokaler Cache -> Dashboard`

Die bisherigen Excel-Dateien sind keine Laufzeitquelle mehr:

- kein Import beim App-Start;
- keine Aufnahme von `zdispo_grp.xlsx` und `zdispo_spart.xlsx` in Build oder Publish;
- kein Excel-Fallback bei fehlendem SAP-EntitySet;
- alte Cachezeilen mit Excel-Quelle und die alte manuelle Tabelle werden von
  Spend-Aufriss und Supply-Chain-Seiten nicht mehr ausgewertet.

## Anwendung

- `PurchasingProductGroupSapReader` erkennt entweder die beiden EntitySets
  `ZDISPO_GRP`/`ZDISPO_SPART` oder ein bereits zusammengefuehrtes
  Produktgruppen-EntitySet.
- Full Load und Delta lesen die komplette kleine Referenzliste bei jedem Lauf.
- Nur eine nicht-leere, validierte SAP-Antwort ersetzt
  `PurchasingSpendDisponentRule`, und zwar atomar in derselben SQLite-Transaktion
  wie der Einkaufs-Refresh.
- Der Cache speichert die konkrete Quelle als `SAP OData: <EntitySet>`.
- Exakte Disponenten, Sternmuster, laengstes Muster und Mehrfachzuordnungen
  bleiben fachlich unveraendert. Die 1/n-Allokation bleibt summenerhaltend.
- Die Datenquellenseite zeigt die Zahl der aus SAP gecachten Produktgruppenregeln.

## SAP-Bereitstellung

Fertige Artefakte und Anleitung:

- `docs/abap/README_PRODUCT_GROUP_SAP_ODATA.md`
- `docs/abap/ZDISPO_GRP_GET_ENTITYSET.abap`
- `docs/abap/ZDISPO_SPART_GET_ENTITYSET.abap`

Erwartete Felder:

| SAP-Quelle | Felder | Key |
| --- | --- | --- |
| `ZDISPO_GRP` | `DISPO_KZ`, `DISPO` | beide Felder |
| `ZDISPO_SPART` | `DISPO`, `DESCR` | `DISPO` |

Die Tabellennamen stammen aus den bisherigen SAP-Listenausgaben. Vor der
SEGW-Anlage ist einmal in SE11 zu bestaetigen, dass Tabellen und Felder im
Zielsystem exakt so heissen.

## Produktiver Live-Befund

Am 2026-08-11 wurde `$metadata` des produktiv konfigurierten Einkaufsservice
read-only abgerufen: HTTP 200, 60 EntitySets. Vorhanden sind unter anderem
`ZSTR_LZCODE_USAGESet`, `ZSTR_LZCODE_PARENTSet` und `ZSTR_MAT_XYZSet`.

Nicht vorhanden sind:

- `ZDISPO_GRPSet`;
- `ZDISPO_SPARTSet`;
- ein EntitySet mit `ProductGroupMap`, `ZC23ProductGroup` oder
  `ZSTRProductGroup` im Namen.

Eine zusaetzliche DDIC-Pruefung per RFC war mit dem vorhandenen SAP-Servicekonto
nicht moeglich: SAP verweigerte bereits `RFCPING`. Es wurde nichts in SAP
geschrieben oder aktiviert.

## Testnachweis

- sechs gezielte Reader-/Schema-/Dashboard-/Supply-Chain-Tests: gruen;
- vollstaendige Release-Regression: `464/464` gruen;
- Build-Ausgabe enthaelt beide `zdispo*.xlsx` nicht mehr;
- Regression belegt, dass Cachezeilen mit alter Excel-Quelle und die manuelle
  Legacy-Tabelle die Anzeige nicht mehr beeinflussen.

Bestehende Warnungen betreffen die bereits bekannte NuGet-Sicherheitswarnung
fuer `Microsoft.AspNetCore.Authentication.Negotiate 8.0.24`, bestehende
MudBlazor-Analyzerhinweise und zwei bestehende xUnit-Analyzerhinweise.

## Deploymentstatus und naechster Schritt (historischer Stand 2026-08-11)

Der Anwendungscode wurde am 2026-08-11 nach ausdruecklicher Nutzerfreigabe
produktiv deployed, obwohl die zwei SAP-EntitySets noch fehlen. Die bekannte
Nebenwirkung ist damit aktiv: Die `45` historischen Regeln mit Excel-Quelle stehen
noch in der Datenbank, werden vom neuen Code aber nicht ausgewertet. Bis zur
SAP-Aktivierung fehlen deshalb die Produktgruppennamen; Full Load und Nacht-Delta
koennen beim Produktgruppenabruf nicht erfolgreich abschliessen.

Reihenfolge fuer den Abschluss:

1. Lucas/Ingo bestaetigt `ZDISPO_GRP` und `ZDISPO_SPART` in SE11.
2. Beide EntitySets gemaess ABAP-Anleitung in `ZPOWERBI_EINKAUF_SRV` aktivieren.
3. `$metadata` und je eine Datenabfrage gegen beide Sets pruefen.
4. Einkauf-Delta starten und in der Produktiv-DB verifizieren:
   Regeln groesser null, `Source` beginnt mit `SAP OData:`.
5. Spend-Aufriss und Supply-Chain-Seite gegen einen bekannten Disponenten
   fachlich pruefen.

Die technischen Punkte 1 bis 4 sowie die Erreichbarkeit der Seiten aus Punkt 5
wurden am 2026-08-12 abgeschlossen. Offen bleibt eine fachliche Stichprobe gegen
einen bekannten Disponenten sowie die oben genannte SAP-Key-/Textpflege.

Release- und Routennachweis: `docs/DEPLOY_GESAMTSTAND_2026-08-11.md`.
