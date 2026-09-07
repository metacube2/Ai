# Kontext: Produktionsdatum ZZPRDAT (PP / Fertigungsauftrag)

Arbeitsstand für die Fortsetzung im CLI. Stand: 07.09.2026.

## Aktueller Kurzstand 07.09.2026

Die Loesung ist gebaut, im Paket `ZPP1`, im Transportauftrag `T76K912490`, und der Auftrag
ist bewusst **nicht freigegeben**.

**Am 07.09.2026 ist ein Konstruktionsfehler gefunden und am selben Tag behoben worden.**
`BEFORE_UPDATE` lief bei **jedem** Sichern, nicht nur bei der Freigabe, und der
Verbuchungsbaustein prueft nur, ob `AFKO-FTRMI` gefuellt und `AUFK-ZZPRDAT` leer ist. Jeder
laengst freigegebene Altauftrag mit leerem Feld haette deshalb beim naechsten beliebigen
Sichern den **heutigen** Eckendtermin bekommen, nicht den vom Tag seiner Freigabe. Das
umgeht die Entscheidung, das Nachfuellen der Altbestaende zu einem eigenen Schritt zu
machen, und liefert bei verschobenen Terminen einen falschen Wert. Aufgefallen ist es nicht,
weil alle Messungen bis dahin mit **neu** angelegten Auftraegen liefen.

Behoben durch eine Gegenprobe in `BEFORE_UPDATE`: vor dem Registrieren wird `AFKO-FTRMI`
gelesen; steht dort schon ein Wert, war der Auftrag vorher freigegeben und wird
uebersprungen. Danach ist die **komplette Reihe wiederholt** worden, alle sieben Wege plus
Altauftragsfall und Write-once, mit den Auftraegen 1241824 bis 1241830.

**Drucknachweis offen; Diagnosebericht am 07.09.2026 ebenfalls korrigiert:**

1. **Der Druckablauf gegenueber V2.** `Z_ZZPRDAT_SET` laeuft in einer eigenen
   Datenbanktransaktion nach der Standardverbuchung. Der Auftrag ist damit einen Moment lang
   gespeichert, waehrend `ZZPRDAT` noch fehlt. Ob ein unmittelbar angestossener Etiketten-
   oder Typenschilddruck das trifft, ist **nicht gemessen** und gehoert zu Marcos Pruefung.
2. **Der Diagnosebericht ist korrigiert und in T76/100 aktiviert.**
   `Z_ZZPRDAT_CHECK` beschreibt jetzt nur die Momentaufnahme: gleiches/abweichendes Datum,
   leeres Feld mit/ohne Freigabenachweis oder gefuelltes Datum ohne Freigabenachweis.
   Er behauptet weder Write-once noch einen Fehler allein aus einem leeren Altauftrag.
   Werk- und Leerfilter greifen vor der Trefferbegrenzung; `p_max <= 0` wird abgewiesen.
   Syntaxpruefung und Ruecklesung des aktiven Quelltexts bestanden. Live mit 14 vorhandenen
   Auftraegen geprueft; der Grenztest `p_max = 1`, Werk 1100, nur leere Felder liefert
   korrekt `1241819`. Der Vorher-Nachher-Test bleibt der eigentliche Write-once-Nachweis.
   Report weiterhin in Aufgabe `T76K912491`, kein Import nach P76.

Einzelheiten zu Auftrag, Objektliste, Befund und Messungen stehen in
`saptasks/ZZPRDAT_TRANSPORTPLAN.md`, Abschnitt 6a.

Produktive Objekte in T76/100:

| Typ | Objekt |
|---|---|
| `FUGR` | `ZPP_ZZPRDAT` mit Verbuchungsbaustein `Z_ZZPRDAT_SET` (V2, „Start verzoegert") |
| `SXCI` | `Z_ZZPRDAT_UPDATE`, Methoden `AT_RELEASE` und `BEFORE_UPDATE`, `AT_SAVE` leer |
| `CLAS` | `ZCL_IM__ZZPRDAT_UPDATE` |
| `ENHO` | `Z_ZZPRDAT` |
| `PROG` | `Z_ZZPRDAT_CHECK` (Nachweisreport, rein lesend) |

Die Quelltexte liegen unter `saptasks/zzprdat/produktiv/`. Die `$TMP`-Testobjekte
(`Z_ZZPRDAT_AT_RELEASE`, `ZCL_IM__ZZPRDAT_AT_RELEASE`, `Z_PP_PRDDAT_SET`, `ZTESTQQ`) sind
geloescht, damit nicht zwei Implementierungen desselben BAdI nebeneinander registrieren;
uebrig ist nur die leere Funktionsgruppe `ZPP_ZZPRDAT_TEST` in `$TMP`.

Nachtest vom 04.09.2026 mit neu angelegten Auftraegen, weil der Baustein nur schreibt, wo
`ZZPRDAT` noch initial ist: 1241817 (CO01 ohne Freigabe, dann CO02, danach Eckendtermin
verschoben — eingefroren), 1241818 (CO01 mit Freigabe beim Sichern), 1241819 (COHV
Sammelfreigabe), 1241820 (CO40 aus Planauftrag 2406063), 1241821 (Auftragsart `PP22`),
1241822 (MD04, Planauftrag 2406064 ueber „-> FertAuftr"), 1241823 (CO41 Sammelumsetzung aus
Planauftrag 2406065, danach in CO02 freigegeben). Alle gesetzt, Write-once gehalten.

**CO41 gibt nicht frei.** Die Sammelumsetzung erzeugt und sichert den Fertigungsauftrag,
loest aber keine Freigabe aus; `ZZPRDAT` bleibt dabei richtigerweise leer und entsteht erst
bei der spaeteren Freigabe. MD04 dagegen fuehrt ueber „-> FertAuftr" direkt in die
CO01-Maske und gibt beim Sichern frei wie CO40.

## Kurzstand 03.09.2026 (ueberholt, Ausgangslage der Analyse)

- Die vollstaendige Mailkette bestaetigt den fachlichen Ablauf: Das Produktionsdatum wird
  nicht schon beim Eroeffnen, sondern bei der erstmaligen Freigabe geschrieben und muss
  danach trotz Terminverschiebung unveraendert bleiben. Lucas Castro nennt
  `WORKORDER_UPDATE` ausdruecklich als Loesungsweg fuer eine dynprounabhaengige Speicherung.
- Der am 03.09.2026 erneut direkt aus T76/100 gelesene Altcode bestaetigt als Quelle
  `I_CAUFVD-GLTRP` und als damalige Triggernaeherung `I_CAUFVD-FTRMI = sy-datum`. Die
  gesamte Schreib- und Rueckgabelogik bleibt auskommentiert.
- Kundenauftrag `399566` ist kein frischer Testauftrag. Er fuehrte zum Fertigungsauftrag
  `1194970`, der bereits am 03.07.2025 freigegeben und spaeter weiter abgeschlossen wurde;
  `REL/I0002` ist heute inaktiv, es gibt keinen verbliebenen Planauftrag und
  `AUFK-ZZPRDAT` ist weiterhin leer. Der Fall belegt den Fehler, kann den erstmaligen
  Freigabetrigger aber nicht erneut testen.
- Naechster externer Input: Die Disposition stellt in T76/100 einen neuen, noch nicht
  freigegebenen Plan- oder Fertigungsauftrag bereit und bestaetigt, dass er fuer CO01/CO02-
  beziehungsweise Umsetzungs-/Freigabetests verwendet werden darf. Vor jeder ABAP-Anlage
  werden ausserdem Paket/Transportauftrag und die Schreibfreigabe fuer T76 benoetigt.

---

## 1. Ziel

Im Kopf des Fertigungsauftrags wird das kundeneigene Feld `ZZPRDAT` (Tabelle `AUFK`)
bei Freigabe **einmalig** mit dem Eckendtermin befüllt. Danach darf es nie wieder
geändert werden (write-once).

Fachlicher Grund: Verpackungslabel und Typenschild müssen dasselbe Datum tragen.
Wird der Eckendtermin später verschoben, bleibt das ursprüngliche Produktionsdatum
auf den Etiketten stehen. Auslöser war ein Qualitätsfall mit abweichenden Daten
auf Label und Typenschild.

Blocker-Kette: Marco Di Menco kann die Etiketten erst umstellen, wenn das Feld
zuverlässig gefüllt wird.

---

## 2. Systemumgebung

| | |
|---|---|
| System | `travt762` (S/4HANA, S4CORE 108, SAP_BASIS 758) |
| Test | `T76/100` |
| Produktiv | `P76` |
| Feld | `AUFK-ZZPRDAT` (Typ DATS) |
| Initialwert | `'00000000'` — erscheint in SQL-Exports nach Power BI als `1970-01-01` |

Klassische ABAP-Syntax erforderlich: keine Inline-Deklarationen (`@DATA`),
kein `ORDER BY` in Subqueries in bestimmten Kontexten.

---

## 3. Root Cause der bisherigen Lösung

> **Überholt seit 2026-07-27.** Die hier beschriebene Bedingung „nur bei
> Tab-Besuch und Freigabe" gilt für den *beabsichtigten* Code. Im System ist die
> gesamte Schreiblogik auskommentiert, es gibt keinen aktiven Pfad. Siehe
> Nachtrag am Ende der Datei.

Die Altlösung schreibt aus dem Kundensubscreen der Erweiterung `PPCO0012`
("FAUF: Anzeigen/Ändern Daten Auftragskopf", Tab "Trafag Daten",
aktiv seit 28.10.2025, Transport `T76K911110`).

Das Datum wird **nur** fortgeschrieben, wenn
1. der User im CO01/CO02 aktiv auf den Trafag-Tab springt (PAI des Subscreens läuft), **und**
2. der Auftrag freigegeben wird (Eröffnen allein genügt nicht).

Wird der Tab nicht besucht, läuft der PAI nie, die Variable bleibt leer, es wird
nichts geschrieben. Nicht user- und nicht mandantenabhängig — deshalb fand Georg
Wagner beim T76/P76-Vergleich auch keine Differenz. Es lag nie am Transport.

In der Praxis läuft die Planauftragsumsetzung meist über MD04 (CH) bzw. CO41 (CZ),
teils mit automatischer Freigabe — also ohne Dynpro, in das man springen könnte.
Ergebnis: im P76 tragen faktisch alle Sätze noch den Initialwert.

---

## 4. Lösungsansatz

BAdI `WORKORDER_UPDATE`, Methode `BEFORE_UPDATE` (Fallback: `IN_UPDATE`, siehe §6).

Kein direktes `UPDATE aufk` und **kein eigenes `COMMIT WORK`** im BAdI — das würde
von der Standard-CO-Verbuchung überschrieben und kann zu Sperrkonflikten führen.
Stattdessen Registrierung eines eigenen Verbuchungsbausteins:

```abap
CALL FUNCTION 'Z_PP_PRDDAT_SET' IN UPDATE TASK
  TABLES it_prddat = lt_prddat.
```

Übergabe als Paare `AUFNR` + `DATUM` (je Auftrag der zugehörige `GLTRP`).

Write-once wird **im Verbuchungsbaustein** erzwungen, nicht davor:

```abap
UPDATE aufk SET zzprdat = ls_prddat-datum
  WHERE aufnr   = ls_prddat-aufnr
    AND zzprdat = '00000000'.
```

Trigger: Statuswechsel nach `I0002` (REL). Im BAdI die Statusänderung erkennen
(Vorher-/Nachher-Vergleich bzw. `STATUS_CHECK` / `JEST`), nicht nur den aktuellen Status.

---

## 5. Zuerst prüfen (vor dem Coden)

Auftrag `000001214608` ist der einzige Satz mit Datum in Marcos Auszug — und die
Werte weichen ab:

```
DGLTP   = 02.12.2025
ZZPRDAT = 20.11.2025
```

Zwei mögliche Erklärungen mit gegensätzlichen Konsequenzen:

- **A)** Der Eckendtermin wurde nach dem Schreiben von 20.11 auf 02.12 verschoben.
  → Altlogik arbeitet korrekt, write-once greift, belastbarer Referenzfall.
- **B)** Die Altlogik schreibt das falsche Feld (Freigabedatum, `sy-datum`, o. ä.).
  → Neuimplementierung darf sich in keinem Punkt am Altcode orientieren.

Klärung über Änderungsbelege: CO03 → Änderungen, bzw. `CDHDR` / `CDPOS`
zu Objektklasse `ORDER`, Objekt-ID = Auftragsnummer.

Weitere Referenzsätze mit Datum (aus früherem Marco-Auszug):
`000001216195`, `000001214481`, `000001214062`.

---

## 6. Reihenfolge-Risiko (kann die Lösung kippen)

Der eigene Update-FB und die Standard-CO-Verbuchung landen beide in derselben
Update-Queue und werden in Registrierungsreihenfolge abgearbeitet. Läuft der
eigene FB **vor** der Standardverbuchung, überschreibt SAP den Wert wieder —
das Fehlerbild sieht dann exakt aus wie heute.

Diagnoseschritt einplanen: einmal durchlaufen lassen, `ZZPRDAT` direkt nach dem
Commit lesen. Bleibt es leer → auf `IN_UPDATE` ausweichen.

---

## 7. Altlogik entschärfen

> **Überholt seit 2026-07-27.** Der Exit ist vollständig auskommentiert und
> schreibt nichts. Dieser Abschnitt wird erst wieder relevant, wenn jemand die
> Schreiblogik dort reaktiviert. Siehe Nachtrag am Ende der Datei.

Der `PPCO0012`-Exit bleibt sonst aktiv und schreibt parallel weiter. Ohne
write-once dort überschreibt jeder spätere Tab-Besuch den eingefrorenen Wert —
genau der Qualitätsfall, der das Projekt ausgelöst hat.

Zu tun:
- Schreiblogik im Subscreen entfernen (Feld nur noch anzeigen) **oder** identische
  `IS INITIAL`-Prüfung einbauen. Beides parallel schreiben lassen ist die
  schlechteste Variante.
- Feld im Dynpro auf Anzeige setzen, sobald gefüllt — sonst ist write-once
  durch jeden User mit CO02 aushebelbar.

---

## 8. Testmatrix (T76/100, vor Transport nach P76)

| Fall | Transaktion / Pfad | Status |
|---|---|---|
| Anlegen + Freigabe | CO01 | offen |
| Ändern + Freigabe | CO02 | offen |
| Serienfertigung | CO40 | offen |
| Massenbearbeitung | COHV | offen |
| Planauftragsumsetzung | MD04 | offen |
| Umsetzung CZ | CO41 | offen |
| Automatische Freigabe | — | offen |
| Write-once: zweiter Save nach Terminverschiebung | CO02 | offen |

Die letzten vier sind die eigentlich kritischen: CH läuft über MD04, CZ teils über
CO41, automatische Freigabe ist von Marco bestätigt.

---

## 9. Offene Rückfragen

**An Lucas Castro / Florian Wächter — Trigger:**
Die Anforderung sagt "beim Auftragsstart", Adil hat "nach Freigabe" beobachtet.
Bei automatischer Freigabe fällt beides zusammen, bei manueller nicht.
Welches gilt?

**An Marco Di Menco — Kopf oder Position:**
Marcos Dump nutzt `DGLTP` (Positionsebene, `AFPO`), Georg schreibt von `AUFK`
(Kopfebene, Quelle wäre `AFKO-GLTRP`). Bei Einpositionsaufträgen identisch, bei
mehreren Positionen mit abweichenden Terminen nicht. Da das Etikett pro Auftrag
gedruckt wird, spricht alles für die Kopfebene — bestätigen lassen.

**Quellfeld `GLTRP` vs. `GLTRS`:**
Anforderung sagt wörtlich "Eck-End Termin" → `GLTRP` (Eckendtermin), nicht `GLTRS`
(terminierter Endtermin). Marcos Vergleich mit `DGLTP` stützt das. Mit Marco/Florian
final bestätigen.

**Von Lucas ausstehend:** Info-Mail zur Feldfreischaltung, CR-Referenz von Florian.

---

## 10. Nacharbeit

Adils Kopierprogramm zur nachträglichen Befüllung war Ende November 2025
freigegeben, die Daten zeigen aber, dass im P76 faktisch nichts befüllt ist —
also entweder nie gelaufen oder nur auf einem Ausschnitt.

Nach dem Fix erneut ansetzen, write-once-konform (`WHERE zzprdat = '00000000'`),
damit die bereits korrekt gefüllten Sätze nicht angefasst werden.

---

## 11. Beteiligte

| Person | Rolle |
|---|---|
| Lucas Castro | Senior Application Manager, Auftraggeber, Vorschlag WORKORDER_UPDATE |
| Marco Di Menco | Fachseite Etiketten, Business Owner, liefert Testdaten |
| Adil Lahrach | PP/VC-Seite, Kopierprogramm, Analyse Freigabe-Abhängigkeit |
| Georg Wagner | externer Berater (meey.ch), Altlösung, steht für Prüfung bereit |
| Florian Wächter | Change Request / Anforderung |
| Fabio Palma | Head of Supply Chain Ops, Dispo will bei PP-Änderungen involviert werden |

---

## 12. Nächster Schritt

1. Änderungsbelege zu `1214608` prüfen (§5) — entscheidet, ob der Altcode als
   Referenz taugt.
2. Rückfragen §9 an Lucas/Marco raus.
3. Erst dann BAdI-Implementierung + Verbuchungsbaustein bauen.

---

## Nachtrag 2026-07-27 (SapProbe-Live-Verifikation T76 + P76)

Read-only per SapProbe (RFC/NCo), siehe `router.md` Abschnitt
„Werkzeug: SAP-Direktzugriff (SapProbe)". Keine Schreibzugriffe, nichts verändert.

### §9 Kopf vs. Position — für Einpositionsaufträge beantwortet

Für alle vier Referenzaufträge (`1214608`, `1216195`, `1214481`, `1214062`) sind
`AFKO-GLTRP` (Kopf) und `AFPO-DGLTP` (Position `0001`) **identisch** — sowohl auf
T76 als auch auf P76 (Client 100). Bei Einpositionsaufträgen ist „Kopf oder
Position" also irrelevant; als Quelle bleibt trotzdem `AFKO-GLTRP` (Kopfebene)
sinnvoll, weil das Etikett pro Auftrag gedruckt wird. Mehrpositionsfälle mit
abweichenden Terminen sind damit **nicht** geprüft — kein solcher Fall unter den
vier Referenzaufträgen.

### §5 Referenzfall `1214608` — neuer Widerspruch, weiterhin ungelöst

Live-Stand P76 (2026-07-27, Client 100) für Auftrag `000001214608`:

| Feld | Live-Wert P76 (2026-07-27) | Wert laut Marcos Auszug (§5) |
| --- | --- | --- |
| `AFKO-GLTRP` (Eckendtermin, Kopf) | 08.01.2026 | — |
| `AFPO-DGLTP` (Position) | 08.01.2026 | 02.12.2025 |
| `AUFK-ZZPRDAT` | **00000000 (leer)** | 20.11.2025 |

Das Feld steht aktuell auf initial — nicht auf dem in Marcos Auszug genannten
Wert, und der Eckendtermin ist inzwischen weiter auf Januar 2026 gelaufen. Zwei
Erklärungen, keine bestätigt:
- Der Auftrag wurde nach Marcos Auszug weiterbearbeitet und `ZZPRDAT` wurde dabei
  zurückgesetzt/überschrieben — write-once hätte das verhindern sollen, tat es
  aber nicht → spräche für Szenario B (Altlogik unzuverlässig).
- Marcos Auszug hatte für genau diese Auftragsnummer einen Fehler (falscher
  Join/falsche Nummer) — dann ist der Referenzfall von Anfang an ungeeignet.

`CDPOS` (Objektklasse `ORDER`, Objekt-ID `000001214608`) liefert **keine Zeilen**
— weder generell für `TABNAME = 'AUFK'` (T76) noch gezielt für `FNAME = 'ZZPRDAT'`
(T76 und P76). Damit scheiden Änderungsbelege als Nachweisquelle für §5 aus:
entweder ist für `AUFK-ZZPRDAT` gar kein Änderungsbeleg-Objekt aktiv, oder es gab
nie eine darüber protokollierte Änderung.

**Neue offene Rückfrage an Marco:** Woher stammt der Auszug mit
`ZZPRDAT = 20.11.2025` für `1214608` (Ziehungsdatum, Quelle/Report)? Der aktuelle
Live-Stand in P76 zeigt für diesen Auftrag ein leeres Feld — die Referenz trägt
in der jetzigen Form nicht, ohne diese Klärung.

**Nebenbefund:** T76 und P76 zeigten zum Prüfzeitpunkt für alle vier Aufträge
identische `GLTRP`/`DGLTP`/`ZZPRDAT`-Werte — möglicherweise wurde T76 kürzlich
aus P76 aktualisiert (System-Refresh).

---

## Nachtrag 2026-07-27: Quelltext-Prüfung — Schreiblogik ist komplett auskommentiert

Mit `abap-read` (SapProbe, read-only) alle Includes des `PPCO0012`-Exits aus
Transport `T76K911110` gelesen (CMOD-Projekt `ZPP00012`). Lokale Kopien:
`.tmp_sap_probe/ppco0012_source/*.abap`.

| Include | Rolle | Zustand |
| --- | --- | --- |
| `ZXCO1U11` | User-Exit `EXIT_SAPLCOKO1_001` — soll laut §3/§4 `AUFK-ZZPRDAT` aus `GLTRP` mit `IS INITIAL`-Prüfung befüllen | **Komplett auskommentiert** (jede Zeile mit `"`) |
| `ZXCO1U12` | Rückgabe von `ci_aufk-zzprdat` an die Struktur | **Komplett auskommentiert** |
| `ZXCO1O01` (PBO, Subscreen) | `FORM Fill_Prod_Date` | Leerer Rumpf, einzige Anweisung ebenfalls auskommentiert |
| `ZXCO1I01` (PAI, Subscreen) | `MODULE user_command_0100 INPUT` | `MOVE-CORRESPONDING ci_aufk TO ci_aufk` — No-op (Struktur auf sich selbst) |
| `ZXCO1F01`/`ZXCO1F02` | andere Forms (Standortwechsel, Kanban-Job) | Unrelated, kein ZZPRDAT-Bezug |

**Kernaussage:** Es gibt aktuell **keinen einzigen aktiven Code-Pfad**, der
`AUFK-ZZPRDAT` schreibt — weder bei Tab-Besuch noch bei Freigabe. Die in §3
beschriebene Bedingung („nur wenn Tab besucht + freigegeben") beschreibt den
ursprünglich *beabsichtigten* Code, der im System aber vollständig deaktiviert
ist. Das erklärt auch den Widerspruch im Nachtrag oben: Marcos Auszug
(`ZZPRDAT = 20.11.2025` für `1214608`) kann nicht aus diesem Exit stammen, da er
nie schreibt — passt zeitlich eher zu §10 (Adils Kopierprogramm, Ende November
2025 freigegeben) als Quelle für einmalig gefüllte Werte.

**Konsequenz für die BAdI-Implementierung (§4):** Der bestehende Exit-Code ist
**keine verwertbare Referenz** — weder für die Trigger-Logik noch für die
Feldzuordnung. Die Neuimplementierung muss komplett neu aus den Anforderungen
(§1) abgeleitet werden, nicht aus dem, was `PPCO0012` heute (nicht) tut.

---

## Nachtrag 2026-09-03: Mailabgleich und erneute Live-Pruefung T76/100

Die von Ingo bereitgestellte Mailkette Oktober 2025 bis Maerz 2026 bestaetigt:

1. Das Feld soll ein einheitliches Produktionsdatum pro Auftrag konservieren. Eine
   spaetere Verschiebung des Eckendtermins darf den gespeicherten Wert nicht mehr aendern.
2. Adil beobachtete am 06.01.2026 in T76, dass das Eroeffnen allein nicht genuegt und die
   Fortschreibung nach der Freigabe erfolgt.
3. Georg reproduzierte am 13.03.2026, dass die alte PPCO0012-Loesung nur bei Besuch des
   Trafag-Dynpros plus anschliessender Freigabe/Sicherung schreibt. CO40, COHV, MD04, CO41
   und automatische Freigaben werden dadurch nicht verlaesslich abgedeckt.
4. Lucas schlug deshalb am 31.03.2026 das BAdI `WORKORDER_UPDATE` fuer eine vom Dynpro
   unabhaengige Speicherung vor. Georg bestaetigte zuletzt, dass sein bisheriger Stand im
   Projekt `ZPP00012` liegt und der Code in den User-Exit-Bausteinen nicht aktiv ist.
5. Fabio verlangt, dass Disposition beziehungsweise Supply Chain bei PP-Aenderungen und
   Tests einbezogen werden. Schreibtests in P76 ohne abgestimmten Auftrag sind damit
   ausgeschlossen.

Read-only am 03.09.2026 mit SapProbe gegen `travt762`, Client 100, erneut verifiziert:

| Pruefung | Live-Ergebnis |
| --- | --- |
| System | T76, HANA, Verbindung erfolgreich |
| Feld | `AUFK-ZZPRDAT`, DATS(8), Datenelement `ZCO_GLTRP`, Text `Produktionsdatum` |
| BAdI-Definition | `WORKORDER_UPDATE` als `SXSD` und `ENHS` im Paket `COBADI` vorhanden |
| CMOD | `ZPP00012` enthaelt `PPCO0012` |
| Exit-Code | `ZXCO1U11`/`ZXCO1U12` weiterhin auskommentiert; PBO ohne Fuelllogik; PAI No-op |
| Kundenauftrag | `399566`, Auftragsart `TA`, angelegt 02.07.2025 |
| resultierender FAUF | `1194970`, Typ `PP21`, Material `36385`, Position `10` des Kundenauftrags |
| Termine FAUF | `GSTRP 09.07.2025`, `GLTRP 23.07.2025`, `GLTRS 22.07.2025` |
| Freigabe/Ende | `FTRMI 03.07.2025`, `GETRI 23.07.2025`; `I0002/REL` heute inaktiv |
| Produktionsdatum | `AUFK-ZZPRDAT = 00000000` |
| Planauftraege zu 399566 | keine verbliebenen Zeilen in `PLAF` |

Damit ist `1194970` ein belastbarer Fehlerbeleg: Er wurde freigegeben, aber das
Produktionsdatum blieb leer. Fuer den neuen BAdI-Schreibtest ist er ungeeignet, weil der
erstmalige REL-Uebergang laengst vorbei ist. Benoetigt wird ein frischer, von der Disposition
freigegebener T76-Testfall.

---

## Arbeitsuebergabe 2026-09-03: Prototyp in T76/100 begonnen (UEBERHOLT)

> **Ueberholt am 2026-09-03 durch den Abschnitt „Arbeitsstand 2026-09-03, Fortsetzung
> durch Claude" am Ende dieser Datei.** Die dort genannten offenen Punkte 1 bis 4 sind
> erledigt: Hilfsbaustein, Klasse und BAdI-Implementierung sind aktiv, und
> `IS_HEADER_DIALOG-GLTRP` ist von der SAP-Syntaxpruefung bestaetigt. Der Abschnitt bleibt
> als datierter Verlauf stehen, ist aber nicht mehr der Handlungsstand.

Ingo hat die Anlage eines separaten Testauftrags sowie die Anlage/Aktivierung der
benoetigten Objekte **ausschliesslich in T76/100 und als lokale `$TMP`-Objekte**
freigegeben. P76 und Transporte bleiben ausdruecklich ausgeschlossen.

### Aktueller SAP-Stand

Folgende Objekte wurden in T76/100 neu angelegt:

| Objekt | Typ | Paket | Stand |
| --- | --- | --- | --- |
| `Z_ZZPRDAT_REL_TEST` | Erweiterungsimplementierung/Container | `$TMP` / LOCAL | angelegt |
| `Z_ZZPRDAT_AT_RELEASE` | klassische BAdI-Implementierung fuer `WORKORDER_UPDATE` | `$TMP` / LOCAL | gespeichert, **inaktiv** |
| `ZCL_IM__ZZPRDAT_AT_RELEASE` | von SE19 generierte Implementierungsklasse | lokal | noch ohne Methodencode, **inaktiv** |
| `ZPP_ZZPRDAT_TEST` | Funktionsgruppe | `$TMP` / LOCAL | angelegt |
| `Z_PP_PRDDAT_SET` | Update-Funktionsbaustein | `$TMP` / LOCAL | Schnittstelle, Attribute und Code gespeichert; Syntax OK; Aktivierung noch nicht bestaetigt |

`Z_PP_PRDDAT_SET` besitzt diese Importparameter (Wertuebergabe):

- `IV_AUFNR TYPE AUFNR`
- `IV_PRDDAT TYPE ZCO_GLTRP`

Eigenschaft: **Verbuchungsbaustein, Start sofort**. Gespeicherter Code:

```abap
FUNCTION z_pp_prddat_set.
  CHECK iv_aufnr IS NOT INITIAL.
  CHECK iv_prddat IS NOT INITIAL.

  UPDATE aufk
    SET zzprdat = iv_prddat
    WHERE aufnr = iv_aufnr
      AND zzprdat = '00000000'.
ENDFUNCTION.
```

Die SAP-Syntaxpruefung meldete fuer den Funktionsbaustein ausdruecklich:
`Es wurde kein Syntaxfehler gefunden` (Statusart S).

Beim anschliessenden Aktivieren wurde nur der Dialog **Inaktive Objekte von KOI**
geoeffnet. Die Auswahl/Aktivierung wurde wegen der Uebergabe an Claude nicht mehr
bestaetigt. Vor dem Weiterarbeiten deshalb zuerst pruefen, ob der Funktionsbaustein
noch `inaktiv` ist, und im Aktivierungsdialog ausschliesslich die Objekte der neuen
Funktionsgruppe aktivieren. Keine anderen inaktiven KOI-Objekte mitaktivieren.

### Geplante BAdI-Methode (noch nicht eingetragen)

In SE19 ist die Methodenliste der Klasse `ZCL_IM__ZZPRDAT_AT_RELEASE` sichtbar.
`AT_RELEASE` steht in Zeile 8 und ist im System beschrieben als:
`Freigabe Auftrag Zeitpunkt: Nach SAP Pruefungen, vor Freigabe`.

Vorgesehener Code:

```abap
METHOD if_ex_workorder_update~at_release.
  CHECK is_header_dialog-autyp = '10'.
  CHECK is_header_dialog-aufnr IS NOT INITIAL.
  CHECK is_header_dialog-gltrp IS NOT INITIAL.

  CALL FUNCTION 'Z_PP_PRDDAT_SET' IN UPDATE TASK
    EXPORTING
      iv_aufnr  = is_header_dialog-aufnr
      iv_prddat = is_header_dialog-gltrp.
ENDMETHOD.
```

Wichtig: `IS_HEADER_DIALOG-AUFNR` und `-AUTYP` sind bestaetigt.
`IS_HEADER_DIALOG-GLTRP` konnte per RFC nicht aufgeloest werden und muss beim
Eintragen einmal durch die SAP-Syntaxpruefung bestaetigt werden. Falls die Komponente
nicht existiert, **keinen Ersatzwert verwenden**; dann auf `BEFORE_UPDATE` und die
Kopfdaten aus `IT_HEADER` wechseln und deren echte Zeilenstruktur im Methodeneditor
pruefen.

### Offene SAP-GUI-Sitzungen bei der Uebergabe

Alle Sitzungen sind T76, Mandant 100, Benutzer KOI:

1. Session 0: CO02 (war bereits offen, wurde nicht veraendert).
2. Session 1: SE19, Implementierung `Z_ZZPRDAT_AT_RELEASE`, Reiter Interface,
   Implementierung weiterhin inaktiv.
3. Session 2: SE37, `Z_PP_PRDDAT_SET`, Quelltextreiter; Aktivierungsansicht
   `Inaktive Objekte von KOI` kann noch offen sein.

SAP GUI Scripting war im Benutzerprofil deaktiviert. Fuer diese Arbeit wurde nur
`HKCU\Software\SAP\SAPGUI Front\SAP Frontend Server\Security\UserScripting`
von `0` auf `1` gesetzt. `WarnOnAttach` und `WarnOnConnection` blieben auf `1`.
Nach Abschluss der SAP-Arbeit den Wert wieder auf `0` setzen, sofern Scripting nicht
bewusst dauerhaft verwendet werden soll.

### Lokales Testwerkzeug

Unter `.tmp_sap_probe/ZzprdatOrderTool/` liegt ein x86/.NET-Framework-NCo-Werkzeug.
Es hat einen harten Guard auf **T76/100** und getrennte Bestaetigungsschalter fuer
Schreibaktionen. Build:

```powershell
dotnet build .tmp_sap_probe\ZzprdatOrderTool\ZzprdatOrderTool.csproj -c Release -p:Platform=x86
```

Der Build lief mit 0 Fehlern und 0 Warnungen. Unterstuetzte Aktionen:

- ohne Argument beziehungsweise `metadata`: nur BAPI-Metadaten lesen
- `verify --order <Nummer>`: AUFK/AFKO read-only lesen
- `create --confirm-create [--start yyyyMMdd] [--end yyyyMMdd]`
- `release --order <Nummer> --confirm-release`
- `change-end --order <Nummer> --end yyyyMMdd --confirm-change`

Der Create-Pfad verwendet die live bestaetigten Referenzdaten von Auftrag `1194970`:
Material `36385`, Werk/Planungswerk `1100`, Auftragsart `PP21`, Menge `1 ST`,
Produktionsversion `1001`, Lagerort `0001`. Er arbeitet in einem stateful NCo-Kontext,
rollt bei BAPI-Fehlern zurueck und committed sonst explizit. **Noch nicht mit create
ausgefuehrt.**

Grund fuer das bewusste Warten: `PP21` kann laut Mail/Referenz automatisch freigeben.
Ein vor Aktivierung des BAdI angelegter Auftrag koennte den entscheidenden ersten
REL-Uebergang verbrauchen. Deshalb erst Hilfsbaustein und BAdI fehlerfrei aktivieren,
dann den separaten Auftrag anlegen.

### Exakte naechste Schritte fuer Claude

1. In Session 2 nur `Z_PP_PRDDAT_SET` und die dazugehoerigen neuen
   `ZPP_ZZPRDAT_TEST`-Includes aktivieren; keine fremden inaktiven Objekte.
2. In Session 1 die Zeile `AT_RELEASE` oeffnen und den obigen Methodencode eintragen.
3. Syntaxpruefung ausfuehren. Bei unbekanntem `GLTRP` wie oben beschrieben stoppen
   und die echte Headerstruktur klaeren.
4. Nur bei fehlerfreier Syntax Klasse und BAdI-Implementierung aktivieren. Noch keinen
   Produktivtransport und keine P76-Aktion.
5. Erst jetzt den separaten PP21-Testauftrag in T76/100 anlegen. Wegen moeglicher
   automatischer Freigabe direkt danach AUFK/AFKO und Status pruefen.
6. Erwartung beim ersten REL: `AUFK-ZZPRDAT = AFKO-GLTRP` des Freigabezeitpunkts,
   ohne Besuch des Trafag-Tabs.
7. Eckendtermin aendern und erneut speichern. Erwartung: `AFKO-GLTRP` aendert sich,
   `AUFK-ZZPRDAT` bleibt unveraendert.
8. SM13/ST22 pruefen. Wenn der Auftrag beim Anlegen automatisch freigegeben wird und
   `ZZPRDAT` trotzdem leer bleibt, kann der in `AT_RELEASE` registrierte Update-Task
   vor dem Standard-AUFK-Insert laufen. Dann diese Variante nicht weiterverwenden,
   sondern `IN_UPDATE`/`BEFORE_UPDATE` mit den echten Kopfstrukturen untersuchen.
9. Danach einen zweiten Test ueber den realen Umsetzungsweg (MD04/CO40; fuer CZ auch
   CO41/COHV) mit Disposition abstimmen.

### Sicherheits- und Scope-Nachweis

- Keine Verbindung oder Aenderung in P76 waehrend dieses Arbeitsblocks.
- Kein Transportauftrag angelegt.
- Kein Fertigungsauftrag angelegt, freigegeben oder terminiert.
- Bestehende Objekte `Z_IKO_CHECK_CO`, `Z_IKO_WORKORDER_CHECK_SAVE` und
  `ZPP00012/PPCO0012` wurden nicht veraendert.
- Alle neuen SAP-Objekte sind testbezogen benannt und lokal (`$TMP`).

## Arbeitsstand 2026-09-03, Fortsetzung durch Claude

Uebernahme von Codex, nachdem dessen Sitzung am Nutzungslimit abbrach. Freigabe von Ingo
unveraendert: **nur T76/100, Paket `$TMP`, kein Transport, kein P76.**

### Der Prototyp ist in T76/100 aktiv

| Objekt | Stand |
|---|---|
| `ZPP_ZZPRDAT_TEST` (Funktionsgruppe) | aktiv |
| `Z_PP_PRDDAT_SET` (Verbuchungsbaustein, Start sofort) | **aktiv** |
| `ZCL_IM__ZZPRDAT_AT_RELEASE` | **aktiv, alle 13 Interface-Methoden** |
| `Z_ZZPRDAT_AT_RELEASE` (BAdI-Implementierung) | **aktiv** |
| `Z_ZZPRDAT_REL_TEST` (Container) | angelegt |

**Rueckfall in einem Schritt:** SE19, Implementierung `Z_ZZPRDAT_AT_RELEASE`, `Strg+F4`
(Deaktivieren). Danach verhaelt sich T76 wieder wie vorher.

### Gelaufener Code

`Z_PP_PRDDAT_SET`, am Bildschirm als `aktiv` bestaetigt:

```abap
FUNCTION z_pp_prddat_set.
  CHECK iv_aufnr IS NOT INITIAL.
  CHECK iv_prddat IS NOT INITIAL.

  UPDATE aufk
    SET zzprdat = iv_prddat
    WHERE aufnr = iv_aufnr
      AND zzprdat = '00000000'.
ENDFUNCTION.
```

`ZCL_IM__ZZPRDAT_AT_RELEASE`, Methode `IF_EX_WORKORDER_UPDATE~AT_RELEASE`, von der
SAP-Syntaxpruefung mit **0 Fehlern** abgenommen:

```abap
METHOD if_ex_workorder_update~at_release.
  CHECK is_header_dialog-autyp = '10'.
  CHECK is_header_dialog-aufnr IS NOT INITIAL.
  CHECK is_header_dialog-gltrp IS NOT INITIAL.

  CALL FUNCTION 'Z_PP_PRDDAT_SET' IN UPDATE TASK
    EXPORTING
      iv_aufnr  = is_header_dialog-aufnr
      iv_prddat = is_header_dialog-gltrp.
ENDMETHOD.
```

### Die offene Frage aus der Uebergabe ist beantwortet

Codex konnte `IS_HEADER_DIALOG-GLTRP` nicht aufloesen. Die Signatur ist jetzt belegt,
abgelesen im Class Builder:

| Art | Parameter | Typisierung | Beschreibung |
|---|---|---|---|
| Importing | `I_AKTYP` | `RC27S-AKTYP` | Transaktionstyp |
| Importing | `I_NO_DIALOG` | `C` | Kennzeichen: kein Dialog |
| Importing | `I_FLG_COL_RELEASE` | `C` | **Kennzeichen: Sammelfreigabe** |
| Importing | `IS_HEADER_DIALOG` | `COBAI_S_HEADER_DIALOG` | Auftragskopf in Dialogstruktur |
| Exception | `FREE_FAILED_ERROR` | | Fehlermeldung aufgetreten |

Die Syntaxpruefung akzeptiert `-GLTRP` und `-AUTYP`. Ein Wechsel auf `BEFORE_UPDATE` ist
damit **nicht** noetig. Fachlich wichtig ist `I_FLG_COL_RELEASE`: die Methode kennt die
Sammelfreigabe ausdruecklich, also genau den CO40- und COHV-Weg, an dem die alte
Dynpro-Loesung gescheitert ist.

### Nachweisreport `Z_ZZPRDAT_CHECK`

Vorlage: `saptasks/zzprdat/produktiv/Z_ZZPRDAT_CHECK.abap`. Rein lesend, SE38. Er lief bis
zum 04.09.2026 unter dem Namen `ZTESTQQ` in `$TMP`; seither heisst er im System
`Z_ZZPRDAT_CHECK` und liegt im Paket `ZPP1`.
Er stellt je Auftrag `AFKO-GLTRP`, `AUFK-ZZPRDAT` und den Freigabestatus gegenueber und
faellt ein Urteil: `gesetzt, gleich GLTRP` / `eingefroren, GLTRP verschoben` /
`FEHLT trotz Freigabe` / `noch nicht freigegeben`.

**Gemessene Nulllinie am 2026-09-03**, Marcos 34 Auftraege `1214601` bis `1214634` in
T76/100: 32 sind freigegeben und haben trotzdem `ZZPRDAT = 00.00.0000`, die uebrigen zwei
sind Sonderfaelle mit inaktivem `I0002`. **Kein einziger Auftrag traegt ein
Produktionsdatum.** Das ist der Fehler aus der Mailkette in voller Breite.

`1214608`, der Auftrag, fuer den Marco einen Auszug mit `ZZPRDAT = 20.11.2025` geschickt
hatte, steht in T76 auf `00.00.0000`.

Nebenbefund, der in den Report eingearbeitet ist: **`JEST`/`I0002` allein ist kein
Freigabenachweis**, weil der Status bei abgeschlossenen Auftraegen inaktiv gesetzt wird.
`AFKO-FTRMI` ist verlaesslich. Ein Report, der nur JEST liest, meldet gerade die
interessanten Altfaelle faelschlich als nie freigegeben.

Zweiter Nebenbefund: **Auftragsart `PP21` traegt den Kurztext „Simulationsauftrag", ist
aber der reale Fertigungsauftragstyp.** Von den 34 Auftraegen sind 24 `PP21` und 10 `PP22`.
`PP22` gehoert spaeter als zweiter Testfall dazu.

### Was jetzt aussteht

1. **Testauftrag in T76/100 anlegen**: CO01, Material `36385`, Werk `1100`, Art `PP21`,
   Menge `1`. Den Reiter **Trafag Daten nicht oeffnen**, freigeben, sichern.
2. `Z_ZZPRDAT_CHECK` mit dieser Auftragsnummer. Erwartung `gesetzt, gleich GLTRP`.
3. CO02, Eckendtermin verschieben, sichern, Report erneut. Erwartung
   `eingefroren, GLTRP verschoben`. Das ist der Write-once-Nachweis, den Marco fuer die
   Etiketten braucht.
4. Bleibt `ZZPRDAT` nach Schritt 2 leer: **nicht weiterprobieren**, sondern SM13 und ST22
   ansehen. Dann laeuft der registrierte Verbuchungsbaustein vor dem Standard-Insert in
   `AUFK` und die Variante muss auf `IN_UPDATE` wechseln.
5. Danach zweiter Fall ueber den realen Umsetzungsweg (MD04 fuer CH, CO41 fuer CZ, CO40
   und COHV fuer die Sammelfreigabe) mit der Disposition abstimmen. Fabio hat in der
   Mailkette ausdruecklich verlangt, bei PP-Aenderungen einbezogen zu werden.
6. Erst danach fachliche Abnahme durch Lucas, Florian und Marco, dann getrennt entscheiden
   ueber Transport nach P76 und ueber das Nachfuellen der Altbestaende.

### Offener Punkt am Arbeitsplatz

`HKCU\Software\SAP\SAPGUI Front\SAP Frontend Server\Security\UserScripting` steht auf `1`.
Codex hatte es fuer diese Arbeit von `0` heraufgesetzt. **Nach Abschluss der SAP-Arbeit
wieder auf `0` setzen**, sofern GUI-Scripting nicht bewusst dauerhaft gewuenscht ist.

### Wie man hier effizient weiterarbeitet

Der Werkzeug- und Fallenkatalog steht in `saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md`.
Die Kurzfassung: **Fuer alles Lesende zuerst einen ABAP-Report schreiben, den Ingo
ausfuehrt.** GUI-Fernsteuerung nur fuer Schreiboperationen, und was am Bildschirm steht,
aber nicht aus der Scripting-Schnittstelle kommt, per Screenshot geben lassen.

### Erster Livetest am 2026-09-03: der Prototyp schreibt nicht

Zwei Testauftraege in T76/100, Material `36385`, Werk `1100`, Art `PP21`, Menge `1`, der
Reiter „Trafag Daten" wurde nie geoeffnet:

| Auftrag | Weg | GLTRP | FTRMI | ZZPRDAT | Urteil |
|---|---|---|---|---|---|
| `1241802` | CO01 anlegen **und** freigeben in einem Vorgang | 05.10.2026 | 03.09.2026 | `00.00.0000` | FEHLT trotz Freigabe |
| `1241803` | CO01 ohne Freigabe gesichert, danach **CO02 freigegeben** | 02.10.2026 | 03.09.2026 | `00.00.0000` | FEHLT trotz Freigabe |

Beide Auftraege sind nachweislich freigegeben: Status `FREI FMAT ABRV`, `FTRMI` gesetzt,
`REL` im Report gesetzt.

**Die naheliegende Erklaerung ist widerlegt.** Beim Anlegen mit CO01 heisst der Auftrag im
Freigabedialog `%00000000001`, also temporaer; das haette die erste Zeile erklaert. Bei
`1241803` existierte die echte Nummer beim Freigeben jedoch bereits, und auch dort blieb
`ZZPRDAT` leer. Die Reihenfolge der Verbuchung allein ist damit nicht die Ursache.

Verbleibende Verdachtsmomente, noch nicht gemessen:

1. **`CHECK is_header_dialog-autyp = '10'` greift nicht.** Die Syntaxpruefung belegt nur,
   dass das Feld existiert, nicht welchen Wert es zur Laufzeit traegt.
2. **`CHECK is_header_dialog-gltrp IS NOT INITIAL` greift nicht,** weil die Dialogstruktur
   das Feld an dieser Stelle nicht gefuellt hat.
3. **Formatunterschied bei der Auftragsnummer.** Traegt `IS_HEADER_DIALOG-AUFNR` die externe
   Darstellung ohne fuehrende Nullen, findet `WHERE aufnr = iv_aufnr` keinen Satz, weil
   `AUFK` intern mit fuehrenden Nullen speichert.
4. **`AT_RELEASE` wird gar nicht aufgerufen.** Zu pruefen ist, ob `WORKORDER_UPDATE` in
   diesem System weitere aktive Implementierungen hat und wie sie sich zueinander verhalten.

**Naechster Schritt ist Messen statt Raten.** Die Methode soll die tatsaechlichen Werte
protokollieren, bevor irgendein `CHECK` greift, zum Beispiel per
`EXPORT ... TO DATABASE indx(zz) ID '...'` und einem kleinen Lesereport. Ohne dieses
Protokoll ist jede weitere Codeaenderung geraten.

Nicht vergessen: Jeder Testauftrag verbraucht genau einen erstmaligen Freigabeuebergang.
Fuer jeden neuen Versuch wird ein neuer Auftrag gebraucht.

### Diagnoselauf 2026-09-03: AT_RELEASE wird nicht gerufen

Nach dem negativen Erstversuch wurde die Methode auf eine Diagnosefassung umgestellt
(`saptasks/zzprdat/WORKORDER_UPDATE_AT_RELEASE_DIAGNOSE.abap`): **alle drei `CHECK`-Zeilen
entfernt**, die Auftragsnummer per `CONVERSION_EXIT_ALPHA_INPUT` normalisiert, und als
Ersatzwert bei leerem `GLTRP` das fachlich unmoegliche Datum `01.01.1900` als Marke.

Damit konnte das Ergebnis nur noch drei Bedeutungen haben. Gemessen wurde die dritte.

| Auftrag | Weg | ZZPRDAT | Marke `01.01.1900`? |
|---|---|---|---|
| `1241807` | CO01 anlegen und freigeben | leer | nein |
| `1241806` | vorher gesichert, dann CO02 freigeben | leer | nein |

Beide sind freigegeben (`FREI FMAT ABRV` beziehungsweise `FREI FMAT VOKL ABRV`, `FTRMI`
gesetzt). Bei `1241806` existierte die echte Auftragsnummer beim Freigeben, der ALPHA-Fall
ist also ebenfalls ausgeschlossen.

**Damit ist belegt: `IF_EX_WORKORDER_UPDATE~AT_RELEASE` wird in diesen Freigabewegen nicht
aufgerufen.** Es liegt weder an einer Vorbedingung noch am Zahlenformat noch am
Zeitpunkt der Nummernvergabe.

Zwei weitere Erklaerungen wurden geprueft und ausgeschlossen:

* **Abgebrochene Verbuchung.** SM13 fuer Benutzer KOI, Auswahl „Alle", zeigt fuer den
  2026-09-03 keinen einzigen Verbuchungsauftrag. Es wurde also nie einer registriert.
* **Verdraengung durch eine andere Implementierung.** SE18 zeigt fuer `WORKORDER_UPDATE`:
  **Mehrfach nutzbar = ja, filterabhaengig = nein.** Alle aktiven Implementierungen laufen,
  unsere wird von keiner anderen verdraengt.

### Naechster Schritt

Die Loesung muss in eine Methode wandern, die beim Sichern sicher laeuft: `BEFORE_UPDATE`,
`IN_UPDATE` oder `AT_SAVE`. Dafuer wird die jeweilige Signatur gebraucht; sie steht im
Class Builder hinter dem Knopf **Signatur** (`Umschalt+F9`) und laesst sich ueber die
Scripting-Schnittstelle bisher nicht auslesen.

`AT_RELEASE` bleibt als Kandidat fuer den Sammelfreigabeweg im Blick: Der Parameter
`I_FLG_COL_RELEASE` deutet darauf hin, dass die Methode fuer CO40 und COHV gedacht ist und
im Einzeldialog von CO01/CO02 gar nicht vorgesehen war.

### Testauftraege in T76/100 aus diesem Lauf

`1241802`, `1241803`, `1241804`, `1241805` (nicht freigegeben), `1241806`, `1241807`.
Alle Material `36385`, Werk `1100`, Art `PP21`, Menge 1. Jeder verbraucht genau einen
erstmaligen Freigabeuebergang und ist danach als Trigger-Test verbraucht.

### Fehler in der eigenen Diagnose, 2026-09-03

Die Diagnosefassungen markierten nur das **Datum**, nicht die **Auftragsnummer**. Das ist zu
schwach, und zwar auf eine Art, die falsche Sicherheit erzeugt:

Ist `IS_HEADER_DIALOG-AUFNR` an der betreffenden Stelle leer, macht
`CONVERSION_EXIT_ALPHA_INPUT` daraus zwoelf Nullen. `'000000000000'` ist in einem
CHAR-Feld **nicht initial**, die Nichtinitial-Pruefung im Verbuchungsbaustein greift also,
das `UPDATE` laeuft und trifft null Saetze. Von aussen ist das nicht davon zu
unterscheiden, dass die Methode nie gelaufen waere.

Aus den Laeufen mit `AT_RELEASE` und `AT_SAVE` folgt deshalb **nicht**, dass diese Methoden
nicht aufgerufen werden. Bewiesen ist nur, dass unter der jeweiligen Fassung nichts
geschrieben wurde. Die frueheren Abschnitte dieser Datei sind in diesem Punkt zu
weitgehend formuliert.

**Konsequenz fuer kuenftige Diagnosen: Eine Sonde darf nicht von den Eingabedaten
abhaengen, die sie gerade pruefen soll.** Die aktuelle Fassung schreibt deshalb in einen
festen, bekannten Auftrag:

* `SONDE_AT_SAVE.abap` schreibt `02.01.1900` nach `1241805`
* `SONDE_AT_RELEASE.abap` schreibt `01.01.1900` nach `1241802`

Beide sind am 2026-09-03 aktiviert. Ein einziger gesicherter Fertigungsauftrag beantwortet
damit beide Fragen auf einmal, unabhaengig davon, was in der Dialogstruktur steht.

### Signaturen der uebrigen Methoden, live abgelesen

| Methode | Signatur | Bemerkung |
|---|---|---|
| `AT_SAVE` | `IS_HEADER_DIALOG TYPE COBAI_S_HEADER_DIALOG`, Exception `ERROR_WITH_MESSAGE` | **identisch zu `AT_RELEASE`**, der bereits syntaxgepruefte Code laeuft unveraendert |
| `IN_UPDATE` | 24 Tabellenparameter, darunter `IT_HEADER TYPE COBAI_T_HEADER` und `IT_HEADER_OLD` | **kein** `IT_STATUS` |
| `BEFORE_UPDATE` | 31 Tabellenparameter, darunter `IT_HEADER`, **`IT_STATUS TYPE COBAI_T_STATUS`** und **`IT_STATUS_OLD`** | die einzige Methode mit Statustabellen |

**`BEFORE_UPDATE` ist damit fachlich die richtige Zielstelle.** Nur dort laesst sich der
erstmalige Uebergang auf `REL` sauber am Statusunterschied erkennen, statt ihn zu raten.
Fuer die Umsetzung werden die Zeilenstrukturen von `COBAI_T_HEADER` und `COBAI_T_STATUS`
gebraucht; sie sind noch nicht abgelesen.

### Weiter ausgeschlossen

* **Erweiterungsimplementierung `Z_ZZPRDAT_REL_TEST`: Aktiv.** Der Container klassischer
  BAdI-Implementierungen im neuen Enhancement-Framework hat einen eigenen Aktivstatus; waere
  er inaktiv, liefe nichts darin. Er ist es nicht.
* **Laufzeitverhalten der Implementierung: „Implementierung wird aufgerufen".** SAP selbst
  haelt sie fuer aufrufbar.
* **`Z_PP_PRDDAT_SET`: aktiv.** SE37 bestaetigt.

## Durchbruch 2026-09-03: ZZPRDAT wird gesetzt und bleibt stehen

**Auftrag `1241812` in T76/100 ist der erste Nachweis.**

| Schritt | GLTRP | ZZPRDAT | Urteil |
|---|---|---|---|
| Anlegen ohne Freigabe, dann Freigabe in CO02 | 20.11.2026 | **20.11.2026** | gesetzt, gleich GLTRP |
| Eckendtermin auf 09.12.2026 verschoben, gesichert | 09.12.2026 | **20.11.2026** | **eingefroren, Write-once nachgewiesen** |

Der Reiter „Trafag Daten" wurde nie geoeffnet. Damit ist die fachliche Anforderung aus der
Mailkette erfuellt: das Produktionsdatum entsteht mit der Freigabe und ueberlebt eine
spaetere Terminverschiebung.

### Es waren zwei Ursachen, nicht eine

Beide muessen behoben sein, sonst passiert gar nichts, und keine der beiden ist am Code
sichtbar.

**Erstens: der Standard ueberschrieb unseren Wert.** `Z_PP_PRDDAT_SET` lief als V1
(`Start sofort`) und schrieb korrekt. Unmittelbar danach schrieb die SAP-Standardverbuchung
die komplette `AUFK`-Zeile aus ihrem eigenen Puffer und deckte `ZZPRDAT` wieder zu. Der
Wechsel auf **V2 (`Start verzoegert`)** loest das: V2-Bausteine laufen erst, wenn alle
V1-Bausteine durch sind.

Das erklaert auch, warum die Diagnosesonden mit festem Zielauftrag funktionierten: `1241802`
und `1241805` waren nicht die gerade gesicherten Auftraege, standen also nicht im
Standardpuffer.

**Zweitens: Parameter niemals als Literal uebergeben.** Beim `CALL FUNCTION ... IN UPDATE
TASK` serialisiert SAP die Parameter und liest sie beim Ausfuehren zurueck. Ein
Zeichenliteral `'19000101'` fuer den DATS-Parameter `IV_PRDDAT` fuehrt dort zu
`CONNE_IMPORT_WRONG_FIELD_TYPE` und reisst die ganze Verbuchung mit. Immer getypte
Variablen verwenden.

### Was noch offen war: der CO01-Weg (GELOEST, siehe Abschnitt am Ende)

`1241811`, ueber CO01 in einem Vorgang angelegt **und** freigegeben, bleibt leer.
Grund ist mit hoher Wahrscheinlichkeit die temporaere Auftragsnummer: Im Freigabedialog
heisst der Auftrag `%00000000001`. Das Feld ist nicht initial, die Diagnosesonde meldete
deshalb faelschlich „AUFNR gefuellt", aber das `UPDATE` findet keinen Satz.

Ansatz dafuer ist die Methode **`NUMBER_SWITCH`** des BAdI, die genau fuer den Wechsel von
der temporaeren auf die endgueltige Nummer vorgesehen ist. Alternativ in `AT_RELEASE` auf
ein fuehrendes `%` pruefen und die Registrierung in dem Fall nach `BEFORE_UPDATE` oder
`IN_UPDATE` verlagern, wo `IT_HEADER` den endgueltigen Kopf enthaelt.

**Fachlich ist das kein Randfall.** Marcos Aufträge entstehen aus der Umsetzung von
Planauftraegen ueber MD04 und CO41, nicht ueber CO01. Ob dort die endgueltige Nummer
vorliegt, ist noch nicht gemessen und muss vor jeder Aussage gegenueber der Disposition
geprueft werden.

### Zielfassung, Stand 2026-09-03

`Z_PP_PRDDAT_SET`, Verbuchungsbaustein, **Start verzoegert (V2)**, aktiv:

```abap
FUNCTION z_pp_prddat_set.
  CHECK iv_aufnr IS NOT INITIAL.
  CHECK iv_prddat IS NOT INITIAL.

  UPDATE aufk
    SET zzprdat = iv_prddat
    WHERE aufnr = iv_aufnr
      AND zzprdat = '00000000'.
ENDFUNCTION.
```

`IF_EX_WORKORDER_UPDATE~AT_RELEASE` (Vorlage `saptasks/zzprdat/AT_RELEASE_ZIEL.abap`):

```abap
METHOD if_ex_workorder_update~at_release.
  DATA: lv_aufnr  TYPE aufnr,
        lv_prddat TYPE zco_gltrp.

  CHECK is_header_dialog-aufnr IS NOT INITIAL.
  CHECK is_header_dialog-gltrp IS NOT INITIAL.

  lv_aufnr  = is_header_dialog-aufnr.
  lv_prddat = is_header_dialog-gltrp.

  CALL FUNCTION 'Z_PP_PRDDAT_SET' IN UPDATE TASK
    EXPORTING
      iv_aufnr  = lv_aufnr
      iv_prddat = lv_prddat.
ENDMETHOD.
```

`IF_EX_WORKORDER_UPDATE~AT_SAVE` ist wieder leer; das Produktionsdatum entsteht mit der
Freigabe, nicht mit jedem Sichern.

### Testauftraege dieses Laufs

`1241802` bis `1241812`. Die Aufträge `1241802` und `1241805` tragen Diagnosedaten
(`04.01.1900` und `04.01.1901`) und sind fachlich wertlos. `1241812` ist der Nachweisfall.

## Abschluss 2026-09-03: alle Wege abgedeckt

Der zuvor offene CO01-Fall ist geloest. **Auftrag `1241813`** wurde in einem einzigen
Vorgang ueber CO01 angelegt **und** freigegeben, ohne den Reiter „Trafag Daten":

| Schritt | GLTRP | ZZPRDAT | Urteil |
|---|---|---|---|
| CO01 anlegen und freigeben | 27.11.2026 | **27.11.2026** | gesetzt, gleich GLTRP |
| Eckendtermin auf 18.12.2026 verschoben | 18.12.2026 | **27.11.2026** | eingefroren |

Zusammen mit `1241812` (Freigabe ueber CO02) sind damit beide Dialogwege nachgewiesen.

### Die Loesung in drei Teilen

**1. `BEFORE_UPDATE` statt nur `AT_RELEASE`.** `AT_RELEASE` bekommt beim Anlegen und
Freigeben in einem Vorgang die temporaere Auftragsnummer `%00000000001`; das `UPDATE`
findet damit keinen Satz. `BEFORE_UPDATE` laeuft unmittelbar vor der Verbuchung und hat in
`IT_HEADER` die endgueltigen Koepfe. `AT_RELEASE` bleibt zusaetzlich aktiv; die
Write-once-Regel macht die doppelte Registrierung unschaedlich.

**2. Dynamischer Komponentenzugriff.** Die Zeilenstruktur von `COBAI_T_HEADER` liegt in
einer Typgruppe und war weder ueber die GUI noch ueber die Signaturtabelle auslesbar.
`ASSIGN COMPONENT 'AUFNR' OF STRUCTURE` loest den Feldnamen zur Laufzeit auf; der
Uebersetzer braucht die Struktur nicht. Ein fehlendes Feld fuehrt zu `sy-subrc <> 0` statt
zum Abbruch. Vorlage: `saptasks/zzprdat/BEFORE_UPDATE_ZIEL.abap`.

**3. Die Freigabepruefung wanderte in den Verbuchungsbaustein.** Dort ist sie an
`AFKO-FTRMI` messbar, und weil der Baustein als V2 nach der Standardverbuchung laeuft, ist
`FTRMI` zu diesem Zeitpunkt bereits festgeschrieben. Damit greift die Regel unabhaengig
davon, aus welcher BAdI-Methode registriert wurde. Vorlage:
`saptasks/zzprdat/Z_PP_PRDDAT_SET_V2.abap`.

### Endstand der Objekte in T76/100, Paket `$TMP`

| Objekt | Art | Stand |
|---|---|---|
| `Z_PP_PRDDAT_SET` | Verbuchungsbaustein, **Start verzoegert (V2)** | aktiv |
| `ZPP_ZZPRDAT_TEST` | Funktionsgruppe | aktiv |
| `ZCL_IM__ZZPRDAT_AT_RELEASE` | Implementierungsklasse | aktiv |
| `Z_ZZPRDAT_AT_RELEASE` | BAdI-Implementierung zu `WORKORDER_UPDATE` | aktiv |
| `Z_ZZPRDAT_REL_TEST` | Erweiterungsimplementierung | aktiv |
| `ZTESTQQ` | Nachweisreport, Vorlage `Z_ZZPRDAT_CHECK.abap` | aktiv |

Belegte Methoden: `AT_RELEASE` und `BEFORE_UPDATE` registrieren den Baustein, `AT_SAVE` ist
bewusst leer.

**Rueckfall in einem Schritt:** SE19, `Z_ZZPRDAT_AT_RELEASE`, `Strg+F4`.

### Testauftraege und ihr Zustand

`1241802` bis `1241813`, alle Material `36385`, Werk `1100`, Art `PP21`, Menge 1.
`1241802` (`04.01.1900`) und `1241805` (`04.01.1901`) tragen Diagnosewerte aus den Sonden
und sind fachlich wertlos. `1241812` und `1241813` sind die Nachweisfaelle.

### Was fachlich noch aussteht

1. Sammelfreigabe ueber CO40 und COHV messen, mit der Disposition abgestimmt.
2. Umsetzung von Planauftraegen ueber MD04 (CH) und CO41 (CZ) messen. Adil Lahrach hat
   diese Wege als die realen benannt.
3. Auftragsart `PP22` als zweiten Typ testen; von Marcos 34 Auftraegen sind 10 vom Typ PP22.
4. Marco prueft, ob Verpackungsetikett und Typenschild dasselbe Feld verwenden.
5. Erst danach ueber den Transport nach P76 entscheiden.
6. Getrennt davon und erst nach dem Transport: Altbestaende nachfuellen, ausschliesslich
   fuer leere Felder.

## Vier Wege gemessen, 2026-09-03

| Auftrag | Weg | GLTRP | ZZPRDAT | Urteil |
|---|---|---|---|---|
| `1241813` | **CO01** anlegen und freigeben in einem Vorgang | 27.11.2026 | 27.11.2026 | gesetzt |
| `1241812` | **CO02** bestehenden Auftrag freigeben | 20.11.2026 | 20.11.2026 | gesetzt |
| `1241814`, `1241815` | **COHV** Sammelfreigabe, Funktion 130 | 04.12.2026 | 04.12.2026 | gesetzt |
| `1241816` | **CO40** Planauftrag `2406062` umsetzen und freigeben | 09.12.2026 | 09.12.2026 | gesetzt |

Bei `1241812` und `1241813` wurde zusaetzlich der Eckendtermin verschoben; `ZZPRDAT` blieb
in beiden Faellen stehen. Der Reiter „Trafag Daten" wurde in keinem Fall geoeffnet.

**COHV ist der wichtigste dieser Nachweise.** In der Massenbearbeitung gibt es kein Dynpro,
das man besuchen koennte; genau daran musste die alte Loesung scheitern. Dass es dort
funktioniert, zeigt, dass die Abhaengigkeit vom Bildschirm vollstaendig aufgeloest ist.

### Vorgehen bei COHV

Selektion auf dem Reiter `SEL_00`, Auftragsnummernbereich. Auf dem Reiter `MVE_00` die
Funktion **130 = Freigabe** im Auswahlfeld `COWORK_FCT_SETUP-FUNCT`. Nach `F8` in der
Ergebnisliste alle Zeilen markieren und **Massenbearbeitung → Ausfuehren**. Skripte:
`SapGuiCohvSammelfreigabe.vbs` und `SapGuiCohvAusfuehren.vbs`.

### Vorgehen bei CO40

Ein Planauftrag ist die Vorbedingung. Angelegt mit **MD11**, Profil `LA` (Lagerauftrag),
Material `36385`, Werk und Dispobereich `1100`, Menge 1. Der Dispobereich ist ein Mussfeld
und entspricht hier dem Werk. Danach CO40 mit Planauftragsnummer und Auftragsart `PP21`.
Skripte: `SapGuiMd11Anlegen.vbs` und `SapGuiCo40Umsetzen.vbs`.

**Das Planauftragsprofil `LA` ist bewusst als neutraler technischer Test gewaehlt.** Ob der
reale Ablauf in CH mit `LA` oder `KD` arbeitet, ist eine Stammdatenfrage und gehoert zu
Fabio Palma und Daniel Tobler. Fuer die technische Frage, ob die Umsetzung das Datum
schreibt, ist das Profil unerheblich.

### Was damit noch offen ist

**MD04 und CO41** sind nicht einzeln gemessen. Beide setzen Planauftraege um und enden im
Sichern eines Fertigungsauftrags, also in demselben Pfad, den CO40 durchlaufen hat. Das ist
ein begruendeter Schluss, aber keine Messung; wer ihn zusagt, sollte das kenntlich machen.

Ebenfalls offen: die Auftragsart **`PP22`**, die bei 10 der 34 Auftraege aus Marcos Liste
vorkommt.
