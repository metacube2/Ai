# SAP-Kalkulation: Wie unterscheidet SAP Rüstzeit von Bearbeitungszeit?

**Kontext:** Frage von Andreas im Meeting vom 2026-07-30 (Adil/SAP-AVOR im Urlaub, daher konnte
die Frage nicht direkt am System geklärt werden). Andreas' Beobachtung: Rüstzeiten sind pauschal
pro Auftrag und ändern sich nicht mit der Losgrösse; Bearbeitungszeiten skalieren mit der Menge.
Offen war, **an welchem SAP-Attribut/Feld** diese Unterscheidung hängt.

**Status dieses Dokuments:** Analyse auf Basis von allgemeinem SAP-PP-Kalkulationswissen, **nicht**
gegen das Trafag-SAP-System live verifiziert (kein Zugriff auf ein SAP-System aus dieser Umgebung
heraus). Bitte mit Adil nach seinem Urlaub gegenprüfen, insbesondere ob Trafag eigene
Z-Formeln/Custom-Konfiguration statt der SAP-Standardformeln nutzt.

## 1. Kurzantwort

Es gibt **kein einzelnes Feld**, das "fix" oder "mengenabhängig" markiert. Die Unterscheidung
entsteht aus dem Zusammenspiel von zwei Stellen:

1. **Arbeitsplan (Routing), Vorgang, Reiter "Vorgabewerte"** – hier steht der reine **Zeitwert**
   je Vorgang für bis zu 6 Slots (z. B. "Rüsten", "Maschine", "Lohn/Personal", ...). Das ist nur
   eine Zahl + Einheit, ohne Information darüber, wie sie mit der Losgrösse skaliert.
2. **Arbeitsplatz (Work Center), Reiter "Kalkulation"** – hier ist pro Slot eine **Formel**
   hinterlegt, die bestimmt, wie der Vorgabewert in einen Kalkulationsbetrag umgerechnet wird.
   - Die Rüstzeit-Formel verwendet den Vorgabewert **konstant**, unabhängig von der Losgrösse.
   - Die Maschinen-/Bearbeitungszeit-Formel multipliziert den Vorgabewert mit
     **Losgrösse ÷ Basismenge** (Bezugsmenge des Arbeitsplans) – deshalb skaliert sie mit der Menge.

Die "fix vs. mengenabhängig"-Logik steckt also **im Arbeitsplatz-Customizing (Formelschlüssel)**,
nicht im Arbeitsplan-Zeitwert selbst. Zwei Arbeitspläne mit identisch aussehenden Vorgabewerten
können sich unterschiedlich verhalten, wenn ihre Arbeitsplätze unterschiedliche Formeln nutzen.

## 2. Wo man das im System nachschauen kann

| Was | Transaktion | Worauf achten |
|---|---|---|
| Vorgabewerte je Vorgang | `CA03` (Arbeitsplan anzeigen) → Vorgang markieren → "Vorgabewerte" | Zeigt die Rohwerte (z. B. "Rüsten 0,5 Std", "Maschine 0,02 Std/Stk") |
| Formelzuordnung | `CR03` (Arbeitsplatz anzeigen) → Reiter "Kalkulation" | Zeigt die Formelschlüssel je Vorgabewert-Slot (z. B. Rüsten → Formel X, Maschine → Formel Y) |
| Formeldefinition (Klartext) | Customizing `OP54` (Formel definieren), Parameter dazu in `OP51`; IMG-Pfad Produktion → Grunddaten → Arbeitsplatz → Kalkulation → Formeln | Zeigt, ob die Formel die Losgrösse einbezieht oder nicht |
| Vorgabewertschlüssel (legt fest, was Slot 1-6 bedeuten) | Customizing `OP19` (Vorgabewertschlüssel) | Legt fest, welcher Slot "Rüsten" heisst und welcher "Maschine"/"Lohn" |

**Hinweis zu Tabellennamen:** Eine frühere Fassung dieses Dokuments nannte hier zusätzlich die
Tabellen `T412Z`/`TC33` (Formeldefinition) und `T430`/`T430T` (Vorgabewertschlüssel) sowie die
Transaktion `OP7F`. Das war **falsch** und wurde entfernt: `T430` ist der Steuerschlüssel
(Operation/Activity control key), `TC33` eine Funktionscode-Zuordnung, `T412Z` liess sich nicht
belegen, und der Vorgabewertschlüssel liegt auf `OP19`, nicht `OP7F`. Die verbleibenden
Transaktionscodes sind gegen die SAP-Dokumentation geprüft.

## 3. Praktischer Weg ohne Adil / ohne Customizing-Kenntnis

Der zuverlässigste Weg, **ohne** die Formel-Konfiguration lesen zu müssen, ist ein empirischer
Test über die Kalkulation selbst:

1. Ein beliebiges Material mit Arbeitsplan wählen.
2. Mit `CK11N` zwei Testkalkulationen rechnen – einmal mit Losgrösse 10, einmal mit Losgrösse 100
   (gleiche Kalkulationsvariante, gleiches Datum).
3. In der **Kalkulationsergebnis-Ansicht "Mengengerüst"/"Kalkulationsschema"** die Kostenarten
   "Rüsten" und "Maschine"/"Fertigung" einzeln vergleichen:
   - Bleibt ein Betrag zwischen den zwei Läufen **konstant** → das ist die Rüstzeit-Kostenart.
   - Skaliert ein Betrag **proportional zur Losgrösse** (10x bei Losgrösse 100 statt 10) → das ist
     die Bearbeitungs-/Maschinenzeit-Kostenart.

Das beantwortet die Frage direkt am System, unabhängig davon, welche Formelschlüssel/Namen
Trafag konkret nutzt, und erfordert keinen Customizing-Zugriff.

## 4. Empfehlung

- **Kurzfristig:** Schritt 3 (zwei Testkalkulationen vergleichen) durchführen – das reicht, um
  Andreas' eigentliche fachliche Frage (welche Zeitart sich wie verhält) zu beantworten.
- **Wenn die genauen Formel-Feldnamen für eine Automatisierung/Auswertung gebraucht werden**
  (z. B. um das in einem Report oder Dashboard nachzubilden): mit Adil nach seinem Urlaub die
  Reiter "Kalkulation" der relevanten Arbeitsplätze (CR03) durchgehen und die dort hinterlegten
  Formelschlüssel dokumentieren – das ist der einzige Weg, um zu wissen, ob Trafag Standard-
  (SAP-)Formeln oder eigene Z-Formeln verwendet.

## 5. Offene Punkte

- Nicht verifiziert: ob Trafag Standard-SAP-Formeln oder kundeneigene Z-Formeln im Einsatz hat.
- Nicht verifiziert: ob alle Arbeitsplätze dieselbe Formellogik verwenden, oder ob es
  Ausnahmen gibt (z. B. bei manuell/extern gefertigten Vorgängen).
