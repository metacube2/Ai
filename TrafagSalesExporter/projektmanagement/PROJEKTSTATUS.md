# Projektstatus Ingo Kohler

Stand: 2026-08-14

Diese Datei ist die **fuehrende Aufgabenliste** fuer das persoenliche
Projektmanagement. Sie ersetzt `kontext.txt` (2013 Zeilen ChatGPT-Protokoll vom
05.05. bis 10.08.2026). Alle weiteren Verlaeufe und Erledigungen werden ab jetzt
hier eingetragen, nicht mehr in einem Chatverlauf.

`kontext.txt` bleibt als Rohquelle liegen, ist aber **abgeloest** und wird nicht
mehr gepflegt.

Abgrenzung: Das Finance Dashboard hat ein eigenes, feineres Issue-Log unter
`docs/Issue_Log_Konsolidiert_2026-08-12.tsv`. Diese Datei hier fuehrt die
uebergeordneten Arbeitspakete; sie verweist auf das Issue-Log, dupliziert es aber
nicht.

---

## 1. Offene Aufgaben

| ID | Thema | Verantwortlich | Prioritaet | Status | Naechster Schritt | Letztes Update |
|---|---|---|---|---|---|---|
| PM-01 | ZLO03: fehlende Materialien und falsche Mengen | Ingo | Hoch | Umsetzung liegt vor, Transport offen | Diagnoselauf `p_diag` und Regressionstest, danach Transport nach B76 | 2026-08-14 |
| PM-02 | ZC12: Fehler bei Nullmengen | Ingo | Mittel | Fehlerbild rekonstruiert, Verifikation blockiert | Vorfrage in SE93 klaeren, danach `p_debug` reaktivieren | 2026-08-14 |
| PM-03 | ZZPRDAT: Produktionsdatum am Fertigungsauftrag | Ingo | Hoch | Umsetzungsvorbereitung, fachlich blockiert | Auftrag 1214608 analysieren, Trigger und Ebene klaeren | 2026-08-14 |
| PM-04 | Einkaufsdashboard: Spend mit Drilldown | Ingo | Mittel | Weitgehend erledigt, Restpunkte in SAP | Zwei SAP-Nacharbeiten anstossen, siehe Detail | 2026-08-14 |
| PM-05 | Finance: alle Daten in einem zentralen Excel | Ingo | Mittel | Produktiv, laufende Detailarbeit | Ueber das Finance-Issue-Log weiterfuehren | 2026-08-14 |
| PM-06 | PPWR und Stoffcompliance ueber SAP-Klassifizierung | Adil, Codex | Mittel | Technische Anlage fertig, Pilotabnahme offen | Pilotmaterialien zuordnen und CL30N-Abnahme fahren | 2026-08-14 |

---

## 2. Details je offener Aufgabe

### PM-01 ZLO03: fehlende Materialien und falsche Mengen

Aufgenommen am 2026-07-27. Im Chatprotokoll stand der Punkt bis zuletzt als
„Klaerung offen". Das ist ueberholt, im Repository liegt bereits eine
vollstaendige Diagnose und ein korrigiertes Programm.

Zwei Ursachen sind belegt und behoben:

1. `ZPOWERBI_VC_TXT-MENGE` ist als `SOBJID` typisiert, also CHAR 40 ohne
   Konvertierungsexit. Die alte Zuweisung interpretierte den String nach der
   Dezimaldarstellung des jeweiligen Benutzers aus SU3. Derselbe Report lieferte
   je nach Benutzer Mengen um den Faktor 1000 daneben, ohne Fehlermeldung.
   Behoben mit `FORM parse_menge_str` als FIX 10.
2. `KOMPNR` und `MATNR` haben in derselben Tabelle unterschiedliche Domaenen.
   `MATNR` traegt den Konvertierungsexit MATN1 und damit fuehrende Nullen,
   `KOMPNR` nicht. Joins trafen bei rein numerischen Komponentennummern ins
   Leere. Aufgefallen ist es nie, weil die von Hand geprueften Komponenten
   Buchstabenpraefixe haben. Behoben mit FIX 11.

**Der eigentliche Blocker** steht in `zlo03/BEFUND_SYSTEMABGLEICH_2026-08-03.md`:
Die Transaktion `ZLO03` startet nicht `ZM_LZCODE20_OPT`, sondern
`Z_ZLO03_TURBO2`. Das laufende Programm enthaelt nur die Fixes 1, 2, 4 und 5.
Die Fixes 10 bis 18 sind also geschrieben, aber **nicht produktiv wirksam**.

Kommunikationspflicht vor der Auslieferung: Die Spalte `Exklusiv` hat kuenftig
drei statt zwei Werte. Neu ist `?` fuer nicht entscheidbare Faelle. Ann-Katrin
Michel muss das wissen, bevor sie die naechste Auswertung interpretiert, denn
ein falsch gesetztes `X` fuehrt zur Kuendigung eines noch benoetigten
Mengenkontrakts. Falsch-negativ ist harmlos, falsch-positiv nicht.

Bewusst nicht behoben, weil fachlich zu klaeren: die funktionslose Spalte
Elternmaterial, die fehlende Rekursion bei der Exklusivitaet, sowie die
Differenz 12 gegen 21 VKNR im Bottom-Up. Fuer den Vergleich CS15 42 gegen ZLO03
21 fehlen Sandro Moltisantis CS15-Einstellungen, also Werk, Stichtag,
Stuecklistenverwendung und ein- oder mehrstufig.

Quellen: `zlo03/CLAUDE.md`, `zlo03/BEFUND_SYSTEMABGLEICH_2026-08-03.md`,
`zlo03/ZM_LZCODE20_OPT.abap`.

### PM-02 ZC12: Fehler bei Nullmengen

Aufgenommen am 2026-07-27, Codeanalyse nachgetragen am 2026-08-14.

**Vorfrage, die alles Weitere traegt:** Ist `ZC12` die Transaktion zu
`Z_ABGLEICH_KTSCH`? In `SE93` gegenpruefen. Falls `ZC12` ein anderes Programm
ist, etwa aus dem VC- oder Klassenumfeld, gilt der ganze folgende Abschnitt
nicht.

Unter der Annahme, dass es dasselbe Programm ist:

**Fehlerbild.** Kein Dump. `fmt_quan` entfernt die Trailing-Nullen, aus `0.000`
wird `0.` und daraus `0`. Diese nackte Null geht in das BDC-Feld `PLPOD-VGWnn`.
`CA02` quittiert das je nach Vorgabewertschluessel mit einer E-Meldung aus der
Vorgabewert- und Einheitenpruefung. Die Meldung landet in `bdc_transaction` in
`lt_msg`, `gv_err_cnt` steigt, die Zeile erscheint im Fehlerlog und es folgt ein
`continue`. Es ist also ein stilles Ueberspringen mit Eintrag im
Fehlerprotokoll, kein Abbruch.

**Blocker fuer die Verifikation: das Tracing ist tot.** In `trace_open` steht
nach dem Kommentar „Nur im Debug-Modus tracen" ein hartes `return.`, und
`p_debug` ist im Selektionsbild auskommentiert. Es existieren deshalb ueberhaupt
keine Trace-Logs zum Nachschauen. Fuer die Diagnose muss zuerst `p_debug`
reaktiviert werden.

**Tabellen und Felder.**

| Zweck | Quelle |
| --- | --- |
| IST-Menge | `PLPO-VGW01` bis `VGW04`, Einheiten `VGE01` bis `VGE04`, gelesen in `select_data` nach `gt_plpo` und `ty_row` |
| SOLL-Menge | `popup_get_zeiten` ueber `POPUP_GET_VALUES`; im Debug-Pfad `run_debug` stattdessen aus `zzpp_vc_vorgabe`, Schluessel `KTSCH`, Felder `VGW01` bis `VGW04` und `VGE01` bis `VGE04` |
| Schreibpfad | BDC auf `SAPLCPDO/1200`, Subscreen `SAPLCPDO/1211` `DEFAULTVAL`, Felder `PLPOD-VGW01` bis `VGW04` |

**Neuer Fehler oder Regression.** Aus dem Code nicht entscheidbar, weil
`fmt_quan` keine eigene Datierung traegt und die Kopfhistorie am 2026-04-30
endet. Konkret pruefbar ueber `SE38` und die Versionsverwaltung sowie ueber die
Transportauftraege zwischen dem 2026-04-30 und dem 2026-05-18.

Arbeitshypothese: keine Regression, sondern eine Testluecke. Die
Trailing-Null-Logik stammt aus dem Neuaufbau von Ende April und war bei Adils
Freigabe am 2026-05-18 bereits enthalten. Wahrscheinlicher ist, dass sein
Testfall keine Zeile mit Vorgabewert null enthielt. Zeigt die Versionsverwaltung,
dass `fmt_quan` seit dem 2026-04-30 unveraendert ist, gilt das als bestaetigt.
Dann lautet die Antwort: der Fehler bestand von Anfang an, und die
Testabdeckung ist um den Nullmengenfall zu erweitern.

Randnotiz zur Kopfhistorie: Adil erscheint dort nur mit dem Eintrag
`20.04.2026 A.Lahrach Deaktiviert wegen Fehlfunktion (alt)`.

### PM-03 ZZPRDAT: Produktionsdatum am Fertigungsauftrag

Aufgenommen am 2026-07-27, urspruenglich als „BAdI-Kennzeichenfehler". Der Punkt
ist am 2026-08-10 praezisiert worden und heisst seither ZZPRDAT.

Ziel ist, dass das Produktionsdatum unabhaengig vom Dynpro immer gespeichert
wird. Als Loesungsweg vorgesehen sind das BAdI `WORKORDER_UPDATE` und ein neuer
Baustein `Z_PP_PRDDAT_SET`.

Offene Punkte, die die Umsetzung blockieren:

- Analyse des Auftrags 1214608.
- Trigger-Klaerung mit Lucas Castro und Florian Waechter.
- Entscheidung Kopf- gegen Positionsebene mit Marco Di Menco.

Danach folgen Implementierung, Test, Transport und die Nachbefuellung der
bestehenden Auftraege.

Wichtige Einschraenkung, die aus dem Protokoll uebernommen wird: Die exakte
Signatur des BAdI ist releaseabhaengig und muss im eigenen System gelesen
werden. Ein SAP-Community-Beitrag taugt als Hinweis, nicht als Beleg.

Im Repository liegt zu ZZPRDAT bislang **kein** Dokument. Die vollstaendigen
Hintergrundinformationen wurden laut Protokoll im Claude-Konto `metacube`
abgelegt.

### PM-04 Einkaufsdashboard: Spend mit Drilldown

Aufgenommen am 2026-07-27. Der Drilldown ist eingebaut. Die verbliebene
Anforderung war, Beschaffungs-Warengruppe und Produktgruppe belastbar ueber eine
SAP-Tabelle zu etablieren statt ueber abgeleitete Logik.

**Diese Anforderung ist am 2026-08-12 erfuellt worden**, was im Chatprotokoll
noch nicht steht. Beide SAP-EntitySets sind produktiv aktiv, das produktive
`$metadata` liefert HTTP 200 mit 62 EntitySets, `ZDISPO_GRPSet` liefert 45 Zeilen
und `ZDISPO_SPARTSet` 22 Zeilen. Der produktive Einkauf-Delta lief um 10:03:42
MESZ mit `Success`. Der Cache enthaelt danach 45 Regeln aus SAP OData und null
Regeln aus Excel oder anderen Quellen. Excel ist damit als Mappingquelle
vollstaendig abgeloest.

Zwei SAP-Nacharbeiten bleiben, beide ohne Betriebsauswirkung:

1. `ZDISPO_SPART` liefert fuer die Codes D1 und D5 keinen Text, die Anwendung
   zeigt deshalb den SAP-Code an.
2. `ZDISPO_GRP` hat in den produktiven Metadaten nur `DISPO` als Key, obwohl
   `DISPO` in neun Gruppen mehrfach vorkommt. SEGW sollte auf den
   zusammengesetzten Key `DISPO_KZ + DISPO` korrigiert werden.

Quelle: `docs/PURCHASING_PRODUCT_GROUP_SAP_DIRECT_2026-08-11.md`.

### PM-05 Finance: alle Daten in einem zentralen Excel

Aufgenommen am 2026-07-27. Das zentrale Excel existiert produktiv und wird
taeglich erzeugt. Das Arbeitspaket ist damit im Kern erledigt, die verbleibende
Arbeit ist Detail- und Datenqualitaetsarbeit.

Diese Detailarbeit wird **nicht hier**, sondern im Finance-Issue-Log gefuehrt.
Dort stehen zwoelf Issues mit eigenem Owner und eigenem Status.

Die wichtigsten offenen Punkte von dort, nur als Verweis:

- Datenzufluss TR FR steht seit dem 2026-07-30, Antwort aus Frankreich fehlt.
- CH/AT-Herstellerregel wartet auf einen Fachentscheid von Andreas.
- Moving Average bei TR IT mit Paola, Zieldatum Ende August 2026.
- Fachfreigabe der Gruppenmarge als fuehrender Abschlusswert.

Quellen: `docs/Issue_Log_Konsolidiert_2026-08-12.tsv` als Statusquelle,
`docs/FINANCE_OFFENE_PUNKTE_2026-08-12.md` als Begruendung,
`docs/rag/FINANCE.md` als fachlicher Einstieg.

### PM-06 PPWR und Stoffcompliance ueber SAP-Klassifizierung

Von Codex am 2026-08-13 bearbeitet. Ausloeser ist die Verordnung (EU) 2025/40,
die seit dem 2026-08-12 gilt. Grundlage sind `Verpackungsverordnung.docx` und
die Mailabstimmung zwischen Fabio Palma und Florian Waechter.

Loesungsansatz ohne zusaetzliche Z-Felder im Materialstamm, stattdessen zwei
Klassen der Klassenart `001`:

- `ZPPWR_PACKMITTEL` fuer Verpackungseigenschaften, neun Merkmale.
- `ZCOMP_STOFF` fuer stoffliche Compliance, zwoelf Merkmale, ausdruecklich als
  befristete Zwischenloesung bis zur Entscheidung ueber SAP Product Compliance.
  Der Klassenkurztext muss `Interim` enthalten.

**Erledigt:** Die technische Anlage in T76/090 ist am 2026-08-13 abgeschlossen.
Der Report `ZPPWR_CLASS_SETUP` hat 21 Merkmale und beide Klassen angelegt und
per BAPI committed. Der Report ist wiederholbar, ueberspringt vorhandene Objekte
und hat eine feste Systemsperre fuer alles ausser T76/090.

**Offen:** Pilotmaterialien zuordnen, also 10 bis 20 Packmittel, und die
CL30N-Abnahme fahren.

**Gesperrt:** Transport nach P76 und jede Massenpflege, bis die Fachfreigabe
vorliegt.

Acht Entscheidungen blockieren den Produktivgang. Die drei schwersten sind, wie
die Kante Produkt zu Packmittel gepflegt wird, ob `MAGRV` wirklich die
Materialfraktion abbildet, und wer je Merkmal Data Owner ist. Ohne die erste
Kante gibt es kein Mengen-Rollup je verkauftem Sensor.

Fachlich bewusst abgegrenzt: Ein Feld `PFAS Content (%)` wurde **nicht**
angelegt, weil die PPWR ihre PFAS-Grenzwerte in `ppb` und `ppm` nennt und nur
fuer Verpackungen mit Lebensmittelkontakt gelten. Fuer Trafag ist das nicht die
passende Bewertungsbasis. Stattdessen gibt es vorlaeufig einen Status mit
Bewertungsdatum.

Ein Termin mit Fabio kann eingeplant werden, sobald die T76-Bestandspruefung und
der Pilotkatalog bestaetigt sind.

Quellen: `docs/PPWR_SAP_KLASSIFIZIERUNG_ANLAGEPROTOKOLL_2026-08-13.md` mit
Abschnitt 14 zu den BAPI-Learnings, Quellcode unter
`docs/abap/ZPPWR_CLASS_SETUP.abap`.

---

## 3. Erledigt

Verdichtetes Archiv aus `kontext.txt`. Ein Eintrag je abgeschlossenem Punkt.

### Trafag Management Reporting

| Datum | Punkt | Ergebnis |
|---|---|---|
| 2026-05-12 | ES Lesezugriff Sage | Geloest ueber CSV-Export statt Direktzugriff, damit waren Santis Sicherheitsbedenken gegenstandslos |
| 2026-05-12 | Durchsprache Santi Gomez | Stattgefunden |
| 2026-05-12 | Rhino Zugangsdaten | Von Marco erhalten |
| 2026-05-19 | Intercompany-Abgrenzungen | Mit Andreas Stoller geklaert |
| 2026-05-27 | DE File von Rohail Munir | Geliefert, nach 22 Tagen und zweimaligem Nachfassen |
| 2026-07-02 | Wechselkurse 2025 und 2026 im zentralen Excel pruefen | Geprueft |

### HR Cockpit

| Datum | Punkt | Ergebnis |
|---|---|---|
| 2026-05-12 | Fluktuationsformel von Sonja | Erhalten, nur Arbeitnehmerkuendigungen |
| 2026-05-15 | Prozentsaetze aus REXX | Eingebaut: Krankheit, Unfaelle, Soll-Arbeitszeit |
| 2026-05-15 | Fluktuation in PowerBI einbauen | Eingebaut |
| 2026-05-27 | Fluktuation plausibilisieren | Mit Nadjas Testkriterien abgeschlossen, Vergleich moeglich |

### SAP-Entwicklung

| Datum | Punkt | Ergebnis |
|---|---|---|
| 2026-05-15 | Mandant 200, Kundenzahlen faken | Von Fabio getestet |
| 2026-05-18 | ZC12 Test | Von Adil getestet und freigegeben |
| 2026-05-28 | ZLO03 Fakenummer 9999 bei Textposition | Erledigt, 23 Tage nach der urspruenglichen Deadline vom 05.05. |
| 2026-05-28 | ZLO03 Sternchen-Filter, fehlende VKNR | Erledigt |
| 2026-06-04 | Massupload Mappe | Erledigt |
| 2026-07-02 | Preiskondition Bruttopreis 9999 fuer Fabio | Erledigt |

### Organisation

| Datum | Punkt | Ergebnis |
|---|---|---|
| 2026-05-18 | Smartsheet Vollzugriff Philip Steiger | Erledigt, war seit April offen |
| 2026-06-04 | PowerBI-Lizenzen | Erledigt durch Gunther |
| 2026-06-04 | Uebersicht fuer den SAP-Koordinator | Erstellt |
| 2026-07-02 | Punkt 4 Vorschriften Lager | Erledigt |

### Verworfen

| Datum | Punkt | Grund |
|---|---|---|
| 2026-07-02 | Fake-Namen fuer alle Programme und Tabellen | Hinfaellig, der auftraggebende Vorgesetzte hat das Unternehmen verlassen. Zustaendig war Gunther |

---

## 4. Personen

| Person | Thema im bisherigen Verlauf |
|---|---|
| Andreas Stoller | Finance, Intercompany-Abgrenzung, Standardkosten, offene Fachentscheide zur Gruppenmarge |
| Santi Gomez | Spanien, Sage, CSV-Export |
| Rohail Munir | Deutschland, DE File |
| Marco | Rhino, CHF-Konsolidierung |
| Marco Di Menco | ZZPRDAT, Entscheidung Kopf- gegen Positionsebene |
| Nadja | HR Cockpit, Plausibilisierung der Fluktuation |
| Sonja | HR, Fluktuationsformel |
| Gunther | PowerBI-Lizenzen, ehemals Fake-Namen |
| Fabio | Test Mandant 200, Preiskondition Bruttopreis |
| Adil | Test ZC12 |
| Philip Steiger | Smartsheet |
| Ann-Katrin Michel | Fachanwenderin ZLO03, Phase-Out-Prozess |
| Sandro Moltisanti | ZLO03, Meldung CS15 42 gegen ZLO03 21 |
| Lucas Castro | ZZPRDAT, Trigger-Klaerung |
| Florian Waechter | ZZPRDAT, Trigger-Klaerung |
| Paola | Italien, Moving Average und Cost Run |

Belegte Laenderzuordnung gibt es nur fuer Spanien mit Santi und Deutschland mit
Rohail. Fuer die uebrigen Personen ist im Protokoll kein Land genannt. Die
Standortempfaenger stehen gepflegt in `docs/ANSPRECHPARTNER.md`.

---

## 5. Erfahrungen aus dem bisherigen Verlauf

Diese Punkte haben im Chatverlauf wiederholt Zeit gekostet und gehoeren deshalb
festgehalten.

**Ein Chatverlauf ist keine Aufgabenliste.** Genau deshalb existiert diese Datei.
Im Protokoll sind Punkte mehrfach neu aufgerollt worden, Erledigtes tauchte
wieder als offen auf, und die Statusfrage musste jedes Mal neu beantwortet
werden.

**Datumsangaben nie raten.** Am 2026-05-16 gab es einen Konflikt zwischen der
Nutzerangabe und dem Systemzeitstempel, richtig war der 18.05. Bei einer
Statusaussage gehoert das Datum belegt, nicht geschaetzt.

**Anforderung vor Code.** Mehrere Punkte, etwa Massupload Mappe und
Preiskondition 9999, waren zunaechst nur ein Stichwort. Erst die Rueckfragen
haben den Auftrag brauchbar gemacht. Dieselbe Regel steht als „Diagnose vor
Code" auch in `zlo03/CLAUDE.md`.

**Statusangaben gegen die Quelle pruefen.** Beim Abgleich am 2026-08-14 war
PM-04 laengst erledigt und PM-01 deutlich weiter, als das Protokoll auswies. Ein
Punkt gilt erst dann als offen, wenn die Quelle das bestaetigt.

**Ticketvokabular ist nicht Codevokabular.** Eine Repository-Suche nach den
Begriffen aus einem Ticket geht regelmaessig ins Leere, weil der Code andere
Woerter benutzt. Bei PM-02 heisst „Nullmenge" im Coding schlicht `VGW01` bis
`VGW04` gleich null, und die betroffene Routine heisst `fmt_quan`. Ein Personen-
name wie „Adil" steht ohnehin nur in der Kopfhistorie. Bei einer erfolglosen
Suche also zuerst fragen, wie der Sachverhalt im Code heissen wuerde, statt auf
„nicht vorhanden" zu schliessen.

---

## 6. Pflege dieser Datei

1. Statusaenderung immer mit Datum in der Tabelle unter Abschnitt 1 eintragen.
2. Ist ein Punkt erledigt, die Zeile aus Abschnitt 1 entfernen und als eine Zeile
   in Abschnitt 3 mit Datum und Ergebnis ablegen. Der Detailblock aus Abschnitt 2
   entfaellt dabei.
3. Neue Aufgaben bekommen die naechste freie Nummer, also ab PM-06. Nummern
   werden nicht wiederverwendet.
4. Finance-Details gehoeren in
   `docs/Issue_Log_Konsolidiert_2026-08-12.tsv`, nicht hierher.
5. Vor Arbeiten im Repository gilt zusaetzlich die Pflicht aus `CLAUDE.md`, also
   zuerst `docs/AGENT_COORDINATION.md` lesen und den eigenen Bereich eintragen.
