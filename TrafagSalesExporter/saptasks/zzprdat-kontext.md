# Kontext: Produktionsdatum ZZPRDAT (PP / Fertigungsauftrag)

Arbeitsstand für die Fortsetzung im CLI. Stand: 27.07.2026.

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

Read-only per SapProbe (RFC/NCo), siehe `docs/RAG_ROUTER.md` Abschnitt
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
