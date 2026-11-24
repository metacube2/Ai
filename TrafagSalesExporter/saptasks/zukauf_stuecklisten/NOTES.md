# Zukaufteile mit Stückliste oder Arbeitsplan, Werk 1100 (P76, 2026-11-24)

## Auftrag

Ingo am 24.11.2026, 10:00: „ziehe list aus p system, er will die wicher haben“. Liste für Armin Güntensperger (Einkauf/Kalkulation):
Armin hatte eine Leiterplatte gefunden, die komplett eingekauft wird (BESKZ F), aber Stückliste mit Preisen und Arbeitsplan hatte;
die Daten sind gelöscht. Frage: Gibt es weitere solche Fälle? P76 nur lesend, ausdrücklich für diese Liste freigegeben.

Ergebnis: `C:\Users\koi\Downloads\Zukaufteile_mit_Stueckliste_Arbeitsplan_P76_2026-11-24.xlsx` (Blätter „Übersicht“, „Liste“).
Bau: `liste_bauen.py` (dieser Ordner) mit `se16lesen.py`; Rohexporte nur im Scratchpad der Sitzung (enthalten ANNAM/AENAM bzw.
ERNAM, also Benutzernamen, und EINA zusätzlich VERKF/TELF1 der Lieferanten, deshalb nicht im Repo). Zahlen im Export: Dezimalpunkt,
Tausender-Komma (MBEW-PEINH „1,000“ = 1000; erster Bau las es als 1, korrigiert, z. B. K17001 5.45 CHF je 1000).

## Lesung

P76/100, eigene Sitzung über `P76Lesen.vbs neu:SE16` (Id `/app/con[1]/ses[2]`, Ingos `ses[0]` unberührt), Exporte mit
`P76Se16Export.vbs` (SE16, `%PC`), danach mit `P76SitzungSchliessen.vbs` geschlossen. Lesezeitpunkt 24.11.2026 10:05 bis 10:13.
Kein Passwort, kein RFC, nichts geändert. SE16 kann beim ersten Aufruf einer Tabelle Selektionsreports generieren (LEARNINGS 17.11.).

| Tabelle | Filter | Treffer |
| --- | --- | --- |
| MARC | WERKS 1100, BESKZ F | 10'906 (davon 5'939 ohne LVORM; SOBSL dort leer 5'895, 30: 44) |
| MAST | WERKS 1100 | 51'481 (STLAN 3: 51'386, 9: 73, 1: 18, 2: 3, 8: 1) |
| MAPL | WERKS 1100 | 62'180 (PLNTY N 62'178, Q 2) |
| STKO, STAS, STPO | STLTY M, STLNR aus Datei (1'226 Stücklisten) | 1'226 / 1'386 / 1'386 |
| PLKO | PLNTY N, PLNNR aus Datei (1'195 Plangruppen) | 1'274 |
| MAKT (SPRAS DE), MARA, MBEW (BWKEY 1100) | MATNR aus Datei (1'287) | je 1'287 |
| EINA | ohne Filter (SE16 bietet nur INFNR an) | 17'269 |
| EINE | INFNR aus Datei (2'796) | 2'888 |
| T460T (DE, 1100), T141T (DE) | | 3 / 14 |

Wege, die nicht gingen: `P76Se16nExport.vbs` meldet in P76 „Felder nicht gefunden: WERKS,BESKZ“ (Spaltenversatz, wie LEARNINGS
23.11.; nicht angepasst, SE16 genommen). TCA51 in SE16 bietet PLNTY nicht an (nicht gelesen). Die DD03L-Abfrage über T76-RFC
wurde vom Auto-Modus abgelehnt; die Feldnamen stammen aus den Kopfzeilen der SE16-Exporte.

## Regeln der Auswertung

- Kandidat: MARC 1100, BESKZ F, LVORM leer, und (MAST Verwendung 1 oder 3 mit STKO ohne LKENZ/LOEKZ) oder (MAPL Plantyp N,
  letzter Zähler ohne LOEKZ, PLKO letzter Zähler ohne LOEKZ). Nur Plantyp N zählt als Arbeitsplan (PLKO VERWE 1, STATU 4);
  Plantyp Q steht nur als Hinweis in der Spalte „Anderer Plan“.
- Positionen: STAS der Alternative laut MAST (alle 01), letzter Stand bis heute ohne LKENZ, dazu STPO desselben Knotens ohne LKENZ.
  In P76 haben alle 1'226 Stücklisten genau eine Alternative und einen Kopfstand, keine Position ist gelöscht oder in der Zukunft.
- Selbst-Stückliste: alle Lagerpositionen (POSTP nicht T) sind das Material selbst.
- Einstufung: SOBSL 30 → „Lohnbearbeitung, Stückliste gehört dazu“; Selbst-Stückliste ohne Arbeitsplan → „harmlos“;
  alles andere → „prüfen“, sortiert: Fremdkomponenten mit Arbeitsplan, Fremdkomponenten ohne Plan, Plan mit Selbst-Stückliste,
  Plan mit Text-Stückliste, nur Plan, nur Text-Stückliste.
- Infosatz: EINA ohne LOEKZ und EINE mit WERKS 1100 oder leer, ohne LOEKZ.

## Zählungen (P76, 24.11.2026)

| Einstufung | mit Arbeitsplan | ohne | gesamt |
| --- | --- | --- | --- |
| Stückliste/Arbeitsplan bei Zukaufteil prüfen | 1'187 | 56 | 1'243 |
| Selbst-Stückliste, harmlos | 0 | 0 | 0 |
| Lohnbearbeitung, Stückliste gehört dazu | 6 | 37 | 43 |
| Summe | 1'193 | 93 | 1'286 |

„prüfen“ nach Grund: Fremdkomponenten und Arbeitsplan 782, Fremdkomponenten ohne Plan 44, Plan mit Selbst-Stückliste 255,
Plan mit Stückliste ohne Lagerposition 86, nur Plan 64, nur Text-Stückliste 12.

Weitere Befunde:
- In den Stücklisten der Verwendung 1 und 3 dieser Materialien hat keine Position STPO-PREIS ≠ 0, und es gibt nur die
  Positionstypen L und T [B]. Das sagt nichts über die 39 F-Teile mit Stücklisten der Verwendung 9 (PCBA) und 2, deren Positionen
  nicht gelesen sind; 19 davon stehen über Verwendung 1/3 oder den Arbeitsplan trotzdem in der Liste. Nummern im Blatt
  „Verwendung 9 und 2“. Was Armin mit „Preisen“ meinte (Positionspreis, Standardpreis aus der Kalkulation?), ist offen.
- Alle 255 Selbst-Stücklisten haben einen Arbeitsplan; deshalb ist „harmlos“ leer. Auslegung des Agenten: Selbst-Stückliste plus
  Arbeitsplan zählt als „prüfen“ (wegen des Arbeitsplans); Ingo kann das umdrehen.
- Hinweis auf Armins Leiterplatte [V]: B65814 (F, ohne Löschvormerkung) hat keine Stückliste mehr in MAST, und seine
  Arbeitsplanzuordnung MAPL N 50001902/01 hat LOEKZ X mit Änderungsdatum 24.11.2026. Es ist das einzige F-Teil mit einer
  solchen Löschung seit 01.11. und steht nicht in der Liste. B65814 ist der Befund aus Plan FIX 29 (unter 31318).
- Viele Treffer sind PCBA (Kurztext „PCBA …“, z. B. B23217 mit 48 Fremdkomponenten und Arbeitsplan ganz oben).
- 824 Stücklisten haben genau eine Lagerposition, die nicht das Material selbst ist; bei 678 der 826 Materialien mit
  Fremdkomponenten beginnt die erste Komponente mit H (Rohmaterial wie H10040 unter C17389) [B Zählung, „Rohmaterial“ V].
- Alter: Stückliste angelegt 2024 bis 2026: 278 (2025 allein 178), Arbeitsplan 262. Je Grund (frühestes Anlagedatum von
  Stückliste/Arbeitsplan) sind die neuen fast nur Selbst-Stücklisten mit Arbeitsplan (226 von 255 ab 2024; passt zu „seit
  Herbst 2024 zahlreich“, LEARNINGS 12.11.). Die Fälle mit Fremdkomponenten sind Altbestand: mit Arbeitsplan 754 vor 2024,
  28 ab 2024; ohne Arbeitsplan 41 / 3. Nur Arbeitsplan 62 / 2, Text-Stückliste mit Plan 86 / 0, nur Text 9 / 3,
  Lohnbearbeitung 35 / 8 [B].
- MARC-SOBSK (Sonderbeschaffung Kalkulation) ist bei allen 1'287 leer, MBEW-VPRSV überall S; 1'198 haben einen Infosatz, 88 nicht.
- Werksstatus: 1'285 leer, einer 61; Materialart 1'284 HALB, 2 HAWA.
- Lohnbearbeitung mit Arbeitsplan: C19221, C27200, E11221, E11224, E11228, E11229.
- 39 F-Teile ohne Löschvormerkung haben Stücklisten anderer Verwendung (9: 38, 2: 1); nicht in der Liste.

Beispiel C17389 (Zentralschraube 878X Delta P): Einstufung „prüfen“, Grund „Stückliste mit Fremdkomponenten und Arbeitsplan“,
Stückliste 00019807 mit H10040 50 MM, Arbeitsplan N 50024954/01 und /02 (angelegt 16.06.2010), Standardpreis 1.58 CHF je 1,
Infosatz ja. Deckt sich mit Plan FIX 29 Abschnitt 1 (T76). C17390: H10040 53 MM, 1.54 CHF.
Selbst-Stücklisten aus LEARNINGS 12.11./FIX 26 (H20201, H17500, D13006, D85020): alle BESKZ F, in der Liste mit
„Selbst-Stückliste ja“, Arbeitsplan ja, also „prüfen“.

Die Liste hat 1'286 Zeilen; die Kandidatenmenge beim Lesen hatte 1'287, ein Material hatte nur einen Plan vom Typ Q und fällt heraus.

## Abweichung zur T76-Zählung

Gleiche Definition (F, Stückliste Verwendung 1/3): T76 1'866 / ohne Löschvormerkung 1'212 (1'169 ohne Sonderbeschaffung, 43
Lohnbearbeitung); P76 1'876 / 1'222 (1'179 / 43). Also 10 mehr in P76, passend zu Neuanlagen nach der T76-Kopie (Frühjahr 2026) [V].
Wie in T76 gezählt wurde, steht im Plan FIX 29 nicht (nur das Ergebnis); die hier verwendete Definition ist die aus dem Auftrag.

## Offen

- Die gelöschte Leiterplatte ist nicht genannt; B65814 ist nur ein Kandidat (oben). Gelöschte Stücklisten verschwinden aus
  MAST/STKO. Mit Armin bestätigen.
- Soll die Liste um Verwendung 9 (PCBA-Stücklisten) erweitert werden? Dafür STKO/STPO der 39 Stücklisten nachlesen.
- Warum F-Teile 2025/2026 noch mit Stückliste und Arbeitsplan angelegt werden (Arbeitsvorbereitung? frühere Eigenfertigung?),
  mit Armin bzw. der AV klären, bevor in Menge gelöscht wird.
- Plantyp-Text für N und Q nicht aus TCA51/TCA02 gelesen.
