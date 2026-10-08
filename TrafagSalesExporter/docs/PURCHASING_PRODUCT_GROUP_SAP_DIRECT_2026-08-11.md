# Einkaufs-Produktgruppen direkt aus SAP

Stand: 2026-10-08 (Abschnitt „Nachtrag 2026-10-08“: Verwendung je Komponente aus dem Einkauf-Lauf; die
Abschnitte darunter sind der Stand vom 2026-08-11/12, die Zuordnungsregeln gelten unveraendert)

## Nachtrag 2026-10-08: Produktgruppe kam fast nie an (Gespraech Armin, Punkt 4)

**Befund (Produktiv-DB-Sicherung 2026-10-08 09:13, nur gelesen):**

- Die Kette unten war richtig, aber der Schritt `ZLO03/VknrDispo` las nur `MaterialUsageCache`. Den fuellt die
  Seite Stuecklistenanalyse, und nur fuer dort eingegebene Nummern: **86 Zeilen** (85 Bottom-Up fuer 4 Komponenten,
  1 Top-Down). Der „Full Load“ ohne Nummer liefert wegen des SAP-Pflichtfilters 1 Zeile; Lauf 65 vom 15.09. steht
  seither auf `Running`. Damit lag praktisch der ganze Einkauf unter „ohne Produktgruppe“.
- **Obergrenze:** Bestellmaterialien, die in `ZPOWERBI_VC_TXT` als Komponente einer verkuerzten Nummer vorkommen,
  decken 2025 **24,3 von 34,0 Mio** (72 %) und 2026 **23,1 von 30,2 Mio** (77 %) ab; Positionen ohne Material
  3,3 bzw. 1,9 Mio. Mehr kann keine ZLO03-Loesung erreichen, der Rest fehlt in der ZLO03-Selektion.
- `MaterialParentCache` (25'279 Paare) taugt nicht als Abkuerzung: `ElternMatnr` ist mehrstufig (1'941 Eltern sind
  selbst Komponenten), also nicht zwingend die verkuerzte Nummer.

**Loesung (ohne SAP-Aenderung, `PurchasingComponentDispoLoader`):** Der Einkauf-Lauf (Full und Delta) fragt nach den
Kontrakten je bestelltem Material `ZSTR_LZCODE_USAGESet` mit `Richtung eq 'BOTTOMUPD'` (inklusive
loeschvorgemerkter Koepfe) und `Kompnr eq '<18-stellig>'` ab, eine Anfrage je Komponente, drei parallel, hoechstens
8 Minuten je Lauf. Ergebnis `(Kompnr, Vknr, VknrDispo)` in `PurchasingComponentDispoCache`, Abfragezeitpunkt in
`PurchasingComponentDispoState` (auch bei 0 Treffern); eine Komponente wird erst nach 7 Tagen erneut gefragt. Der
erste Aufbau (rund 7'350 Materialien, gemessen 0,25 s je Anfrage) verteilt sich auf mehrere Laeufe. Fehler oder 404
lassen den alten Stand stehen und brechen den Lauf nicht ab. Die Perspektive liest beide Quellen
(Stuecklistenanalyse und neuer Cache), doppelte Paare zaehlen einmal; Regeln, 1/n-Verteilung und Beschriftung
bleiben wie unten.

**Weitere Befunde fuer Armin/Ingo (nicht geaendert):**

1. **Umlaute:** Im Cache stehen `HW_ZUBEH�R`, `BG_DICHTEW�CH`, `FP_DICHTEW�CH` mit Ersatzzeichen.
   In T76 ist `ZDISPO_SPART` korrekt („HW_ZUBEHÖR“), andere Gateway-Texte (Lieferanten, Materialtexte) kommen ohne
   Ersatzzeichen an. Vermutlich ist der Text in P76 so gespeichert; in SM30 auf P76 pruefen.
2. **Doppelte Regeln:** `DS1` und `DS2` zeigen auf D5 und DS, `016` auf H und H2. Das sind echte Doppelnennungen
   in `ZDISPO_GRP`; der Spend wird dort 50/50 verteilt. Ob das so gewollt ist, mit Armin klaeren.
3. Materialien, die selbst verkauft werden (Handelsware ohne Stuecklistenverwendung), bekommen weiterhin keine
   Gruppe. Eine Zuordnung ueber den eigenen Disponenten waere eine neue Regel und nur nach Freigabe einzubauen.

**Pruefen nach dem Deploy:** Meldung im Einkauf-Lauf („Produktgruppen-Verwendung: … gefragt, … offen“), nach dem
Aufbau Anteil „Zugeordnet“ im Spend-Aufriss gegen die Obergrenze oben.


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

*Stand 2026-10-01:* D1/D5 haben im Dashboard Ersatztexte (`3407868`, produktiv 11:21): D1 = `FP_DICHTESEN` (DISPO_KZ DS), D5 = `FP_DICHTESEN1/2` (DS1, DS2), Vorgabe Ingo, SAP vorerst nicht pflegen; ein spaeter in SAP gepflegter Text gewinnt. `ZDISPO_SPART` hat Auslieferungsklasse `A`, Pflege also direkt im System (SM30), kein Transport. SEGW-Key: Empfehlung, nicht umzusetzen (kein Nutzen fuer das Cockpit, Neugenerierung des Service mit Einkauf, Journal und HR noetig); Entscheid Ingo offen.

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

*Ueberholt am 2026-10-08:* „ZLO03/VknrDispo“ kam bis dahin nur aus der Stuecklistenanalyse (86 Zeilen); seither
liefert der Einkauf-Lauf die Verwendung je Komponente, siehe Nachtrag oben.

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

**Nachtrag 2026-09-01:** Der aktuelle NuGet-Audit meldet fuenf High-Advisories ueber vier
Pakete. Details und Updatepfad: `docs/NUGET_SICHERHEIT_2026-09-01.md`.

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

Release- und Routennachweis: `docs/DEPLOYMENT.md`.
