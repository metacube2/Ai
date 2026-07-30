# SAP-Kalkulation: Wie unterscheidet SAP Rüstzeit von Bearbeitungszeit?

**Kontext:** Frage von Andreas im Meeting vom 2026-07-30 (Adil/SAP-AVOR im Urlaub, daher konnte
die Frage nicht direkt am System geklärt werden). Andreas' Beobachtung: Rüstzeiten sind pauschal
pro Auftrag und ändern sich nicht mit der Losgrösse; Bearbeitungszeiten skalieren mit der Menge.
Offen war, **an welchem SAP-Attribut/Feld** diese Unterscheidung hängt.

**Status dieses Dokuments:** Analyse auf Basis von allgemeinem SAP-PP-Kalkulationswissen, **nicht**
gegen das Trafag-SAP-System live verifiziert (kein Zugriff auf ein SAP-System aus dieser Umgebung
heraus). Die genannten Transaktionen und Tabellen sind gegen die SAP-Dokumentation geprüft
(siehe Abschnitt 6). Bitte mit Adil nach seinem Urlaub gegenprüfen, insbesondere ob Trafag eigene
Z-Formeln statt der SAP-Standardformeln nutzt.

**Fassung an Andreas:** `docs/SAP_Kalkulation_Ruestzeit_Bearbeitungszeit_Andreas_2026-07-30.docx`
(gekürzt, ohne Tabellennamen und ohne den Automatisierungs-Ausblick).

## 1. Kurzantwort

Es gibt **kein einzelnes Feld**, das "fix" oder "mengenabhängig" markiert. Die Unterscheidung
entsteht aus dem Zusammenspiel von drei Stellen:

1. **Arbeitsplan (Routing), Vorgang, Reiter "Vorgabewerte"** – hier steht der reine **Zeitwert**
   je Vorgang für bis zu 6 Slots (z. B. "Rüsten", "Maschine", "Lohn/Personal", ...). Das ist nur
   eine Zahl + Einheit, ohne Information darüber, wie sie mit der Menge skaliert.
2. **Arbeitsplatz (Work Center), Reiter "Kalkulation"** – hier ist pro Slot eine **Formel**
   hinterlegt, die den Vorgabewert in eine **Leistungsmenge** umrechnet. Formeln existieren
   getrennt für Kalkulation, Terminierung und Kapazitätsbedarf; derselbe Vorgabewert kann in der
   Terminierung anders verrechnet werden als in der Kalkulation.
   - Die Rüstzeit-Formel verwendet den Vorgabewert **konstant**, unabhängig von der Menge.
   - Die Maschinen-/Bearbeitungszeit-Formel multipliziert ihn mit
     **Vorgangsmenge ÷ Basismenge** – deshalb skaliert sie mit der Menge. SAP-Standardformel
     `SAP002` = Vorgangsmenge × Maschinenzeit ÷ Basismenge ÷ Anzahl Splits (der Split-Divisor
     bedeutet: bei parallelen Arbeitsplätzen skaliert es nicht exakt linear).
3. **Leistungsart + Tarif (KP26)** – die Formel liefert nur die **Menge** (Stunden), nicht den
   Betrag. Die Kosten entstehen erst durch Bewertung mit dem Tarif der zugeordneten Leistungsart:
   `Kosten = Tarif/Einheit × Menge aus Formel`. Zwei Arbeitsplätze mit identischen Zeiten können
   daher unterschiedlich teuer sein.

Die "fix vs. mengenabhängig"-Logik steckt also **im Arbeitsplatz-Customizing (Formelschlüssel)**,
nicht im Arbeitsplan-Zeitwert selbst. Zwei Arbeitspläne mit identisch aussehenden Vorgabewerten
können sich unterschiedlich verhalten, wenn ihre Arbeitsplätze unterschiedliche Formeln nutzen.

**Keine absolute Aussage:** Dass Rüsten fix und Maschine mengenabhängig ist, gilt für die übliche
**Standardkonfiguration**. Entscheidend ist die konkret hinterlegte Formel – bei Z-Formeln kann die
Logik abweichen.

**Zusätzliche Bedingung – Steuerschlüssel:** Ob ein Vorgang überhaupt kalkuliert wird, hängt am
Kalkulationsrelevanz-Kennzeichen im **Steuerschlüssel** des Vorgangs (Tabelle `T430`). Der
Steuerschlüssel trägt auch das Kennzeichen für **Fremdbearbeitung** – fremdbearbeitete Vorgänge
werden abweichend über Einkaufs- bzw. Fremdleistungspreise bewertet (Kostenart in den
Vorgangsdetails), nicht über Zeit × Tarif. Das ist die Antwort auf den offenen Punkt zur
Fremdfertigung in Abschnitt 5.

## 2. Wo man das im System nachschauen kann

Der IMG-Pfad ist release-stabil, der Transaktionscode nur eine Abkürzung – deshalb Pfad zuerst.

| Was | Wo | Worauf achten |
|---|---|---|
| Zeitwerte je Vorgang | Arbeitsplan anzeigen (`CA03`) → Vorgang → Reiter "Vorgabewerte" | Die Rohwerte (z. B. "Rüsten 0,5 Std", "Maschine 0,02 Std/Stk") |
| Formel + Leistungsart je Zeitart | Arbeitsplatz anzeigen (`CR03`) → Reiter "Kalkulation" | Welche Formel und welche Leistungsart je Slot hinterlegt sind. Feld "Formelschlüssel" markieren → F1 → technische Info gibt Feld/Tabelle direkt aus |
| Formeldefinition (Klartext) | Customizing: Produktion → Grunddaten → Arbeitsplatz → Kalkulation → Formeln (`OP54`; Parameter in `OP51`) | Ob die Formel Vorgangsmenge/Basismenge einbezieht |
| Vorgabewertschlüssel (was Slot 1-6 bedeutet) | Customizing: Vorgabewertschlüssel (`OP19`) | Welcher Slot "Rüsten" heisst und welcher "Maschine"/"Lohn" |
| Tarif je Leistungsart | `KP26` | Erst hier entstehen aus Stunden Franken |
| Formel testen | `CR04` ("Arbeitsplatzformeln testen") | Menge als Parameter variieren → Skalierungsverhalten sofort sichtbar |

## 3. Praktischer Weg ohne Adil / ohne Customizing-Kenntnis

Reihenfolge vom Schnellen zum Belastbaren:

1. **`CR03`** – welche Formeln sind für Rüsten und Maschine an den relevanten Arbeitsplätzen
   hinterlegt?
2. **`CR04`** – Formel direkt durchrechnen, Menge als Parameter variieren. Zeigt das
   Skalierungsverhalten eindeutig und ist schneller als der Kalkulationsweg. Prüft aber **nur die
   Formel**, nicht das Zusammenspiel mit Leistungsart/Tarif/Kalkulationsvariante.
3. **`CK11N`** – zwei Testkalkulationen mit unterschiedlicher Losgrösse als End-to-End-Nachweis
   (Arbeitsplan + Arbeitsplatz + Leistungsart + Tarif + Mengengerüst zusammen).

**Zwei Fallstricke beim `CK11N`-Vergleich** (sonst liest man das Ergebnis falsch herum):

- **Anzeige auf Gesamtwerte stellen, nicht "pro Einheit".** Pro Stück *sinkt* der Rüstanteil mit
  steigender Losgrösse – das sieht nach "mengenabhängig" aus, obwohl der Gesamtbetrag konstant
  bleibt. Erwartung bei Gesamtwerten: Rüsten konstant, Bearbeitung steigt proportional.
- **Beide Losgrössen im selben Losgrössenintervall halten.** Sonst selektiert SAP womöglich einen
  anderen Arbeitsplan oder eine andere Stücklistenalternative, und man vergleicht zwei
  verschiedene Fertigungswege statt nur die Mengenwirkung.

## 4. Ausblick Automatisierung (nur intern, nicht in der Fassung an Andreas)

Falls die Formelzuordnung über alle Arbeitsplätze ausgelesen werden soll (offener Punkt 2):

`PLPO` (Vorgang: `VGW01`–`VGW06`, `VGE01`–`VGE06`, `BMSCH` Basismenge, `LAR01`–`LAR06`
Leistungsarten, `ARBID` Arbeitsplatz-Verweis) → `CRHD` (Arbeitsplatzkopf) → `CRCO`
(Arbeitsplatz-/Kostenstellen-/Leistungsart- und Formelzuordnung) → `TC25` (Formeln).

**Nicht überschätzen:** Damit lässt sich die Formelzuordnung *inventarisieren*. Eine vollständige
Nachbildung der Kalkulation braucht zusätzlich Arbeitsplanalternative + Gültigkeit, Vorgangs- und
Basismenge, Mengeneinheiten-Umrechnungen, Vorgabewertschlüssel, Formelparameter, Splits,
Kostenstelle/Leistungsart, gültigen Tarif je Periode/Geschäftsjahr, Kalkulations- und
Bewertungsvariante sowie die Kalkulationsrelevanz aus dem Steuerschlüssel. Das ist kein
Fünf-Minuten-Job.

## 5. Offene Punkte

- Nicht verifiziert: ob Trafag Standard-SAP-Formeln oder kundeneigene Z-Formeln im Einsatz hat.
- Nicht verifiziert: ob alle Arbeitsplätze dieselbe Formellogik verwenden, oder ob es
  Ausnahmen gibt (z. B. bei manuell/extern gefertigten Vorgängen). Siehe Steuerschlüssel-Hinweis
  in Abschnitt 1.

## 6. Geprüfte Referenzen und korrigierte Fehler

Gegen die SAP-Dokumentation geprüft (Transaktionen/Tabellen, nicht die Trafag-Konfiguration):

| Objekt | Bedeutung | Status |
|---|---|---|
| `CA03` / `CR03` / `CR04` / `CK11N` / `KP26` | Arbeitsplan anz. / Arbeitsplatz anz. / Formeln testen / Kalkulation / Tarife | bestätigt |
| `OP54`, `OPCS`, `OPCY` | Formel definieren | alle bestätigt – **mehrere IMG-Einstiegspunkte** auf dasselbe Customizing-Objekt (PP- vs. PS-Menü) |
| `OP51`, `OPCR`, `OPCX` | Formelparameter | bestätigt |
| `OP19`, `OPCM`, `OPJQ` | Vorgabewertschlüssel | alle bestätigt (dito mehrere Einstiegspunkte) |
| `TC20`/`TC20T` | Formelparameter | bestätigt |
| `TC21`/`TC21T` | Vorgabewertschlüssel | bestätigt |
| `TC25`/`TC25T` | Arbeitsplatzformeln | bestätigt |
| `T430`/`T430T` | **Steuerschlüssel** des Vorgangs | bestätigt |
| `PLPO`, `CRHD`, `CRCO` | Vorgang / Arbeitsplatzkopf / Kostenzuordnung | bestätigt |

**Korrigierte Fehler früherer Fassungen dieses Dokuments:**

- `T430`/`T430T` wurde als *Vorgabewertschlüssel* bezeichnet – falsch, das ist der
  **Steuerschlüssel**. Der Vorgabewertschlüssel liegt auf `TC21`.
- `TC33` wurde als *Formeldefinition* bezeichnet – falsch, TC33 ist eine Funktionscode-Zuordnung.
  Arbeitsplatzformeln liegen auf `TC25`.
- `T412Z` wurde als Formelquelle genannt – **liess sich nicht belegen**, entfernt.
- `OP7F` wurde als Vorgabewertschlüssel-Transaktion genannt – **nicht belegbar**; korrekt sind
  `OP19` / `OPCM` / `OPJQ`.
- Formulierung "die Formel ergibt einen Kalkulationsbetrag" – **fachlich unpräzise**. Die Formel
  ergibt eine Leistungsmenge; der Betrag entsteht erst über den Tarif (siehe Abschnitt 1.3).
- Rüstzeit-fix / Bearbeitungszeit-mengenabhängig wurde als absolute Regel formuliert – korrekt ist
  "in der üblichen Standardkonfiguration", weil Z-Formeln abweichen können.
