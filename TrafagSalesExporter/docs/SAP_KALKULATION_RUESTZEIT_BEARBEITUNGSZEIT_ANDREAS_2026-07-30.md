# SAP-Kalkulation: Wie unterscheidet SAP Rüstzeit von Bearbeitungszeit?

**Kontext:** Frage von Andreas im Meeting vom 2026-07-30 (Adil/SAP-AVOR im Urlaub, daher konnte
die Frage nicht direkt am System geklärt werden). Andreas' Beobachtung: Rüstzeiten sind pauschal
pro Auftrag und ändern sich nicht mit der Losgrösse; Bearbeitungszeiten skalieren mit der Menge.
Offen war, **an welchem SAP-Attribut/Feld** diese Unterscheidung hängt.

**Status dieses Dokuments:** Die Grundlogik ist **am Produktivsystem P76 verifiziert**
(RFC-Lesezugriff, 2026-07-30, siehe Abschnitt 7). Die beiden vorher offenen Punkte sind damit
beantwortet: Trafag nutzt eigene Z-Formeln, und es gibt Arbeitsplätze, an denen die Rüstzeit
**nicht** losgrössenunabhängig ist. Die Transaktions- und Tabellenreferenzen sind gegen die
SAP-Dokumentation geprüft (Abschnitt 6).

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
     **Vorgangsmenge ÷ Basismenge** – deshalb skaliert sie mit der Menge.
   - **Achtung, nicht mit den SAP-Auslieferungsformeln verwechseln:** in der Literatur wird
     meist `SAP002` = `SAP_02 * SAP_09 / SAP_08 / SAP_11` genannt. Trafag verwendet diese
     Formel **nicht** – im Einsatz sind `SAP005`/`SAP006`/`SAP007` (ohne Split-Divisor) sowie
     Z-Formeln. Siehe Abschnitt 7 für die tatsächliche Konfiguration.
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

Die beiden ursprünglich offenen Punkte sind durch die Produktivprüfung in Abschnitt 7 beantwortet
(Z-Formeln: ja, im Einsatz; Ausnahmen: ja, `ZAP008` an ca. 45 Arbeitsplätzen). Verbleibend:

- Fachlich zu klären (Adil/AVOR): warum `ZAP005` (Slot 4, Leistungsart 100) an praktisch
  **jedem** Arbeitsplatz hängt und was dieser vierte Vorgabewert betriebswirtschaftlich abbildet.
- Nicht geprüft: die Kalkulationsrelevanz je Steuerschlüssel (`T430`) und die konkrete
  Bewertung fremdbearbeiteter Vorgänge in unserem System.
- Nicht geprüft: die Tarife je Leistungsart (100/200) in `KP26` – ohne die sagt die Menge allein
  nichts über die Kosten.

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

## 7. Produktivprüfung am System P76 (2026-07-30)

Read-only per RFC (`RFC_READ_TABLE`, Werkzeug `.tmp_sap_probe`), Ziel
`travp762.sap.trafag.com` / Mandant 100 → SAP-System-ID **P76**, DB HDB, Kernel 793.
Gelesene Tabellen: `TC20`, `TC21`, `TC25`, `CRCO`, `CRHD`. Keine Schreibzugriffe.

### 7.1 Die Grundlogik ist bestätigt

- `CRCO.FORML` heisst im DDIC wörtlich **"Formelschlüssel Kalkulation"** – die Formelzuordnung
  hängt tatsächlich am Arbeitsplatz je Leistungsart, nicht am Arbeitsplan-Zeitwert.
- `TC25` hat getrennte Kennzeichen `VKALK` (für Kalkulation erlaubt), `VTERM` (Terminierung),
  `VKAPA` (Kapazitätsbedarf) – die Trennung der Formeln je Anwendung ist damit belegt.
- `TC21` Schlüsselfeld ist `VGWTS` = Vorgabewertschlüssel; `TC20` ist die Parametertabelle.

### 7.2 Parameter, die Trafag tatsächlich verwendet (`TC20`)

| Parameter | Herkunft | DDIC-Feld | Einheit | Bedeutung |
|---|---|---|---|---|
| `SAP_01` | Vorgabewert | – | MIN | Rüstzeit aus dem Arbeitsplan |
| `SAP_02` | Vorgabewert | – | MIN | Maschinenzeit aus dem Arbeitsplan |
| `SAP_03` | Vorgabewert | – | MIN | Personal-/Lohnzeit aus dem Arbeitsplan |
| `ZAP_01` | Vorgabewert | – | MIN | eigener (Z-)Vorgabewert |
| `SAP_08` | Feld | `BMSCH` | – | Basismenge |
| `SAP_09` | Feld | `MGVRG` | – | Vorgangsmenge |
| `SAP_11` | Feld | `SPLIM` | – | Anzahl Splits |
| `ZBELAD` | Konstante | – | **ST** | eigener Parameter "Beladung" (Stück je Charge/Ladung) |

### 7.3 Die real zugeordneten Formeln (`CRCO` + `TC25`)

Muster an praktisch **jedem** Arbeitsplatz (`ENDDA = 99991231`, Objekttyp A):

| Slot | Formel | Definition | Skaliert mit Menge? | Leistungsart |
|---|---|---|---|---|
| 0001 Rüsten | `SAP005` | `SAP_01 * SAP_11` | **nein** | 200 |
| 0002 Maschine | `SAP006` | `SAP_02 * MGVRG / BMSCH` | **ja** | 200 |
| 0003 Lohn | `SAP007` | `SAP_03 * MGVRG / BMSCH` | **ja** | 100 |
| 0004 (Z) | `ZAP005` | `ZAP_01 * SAP_11` | nein | 100 |

**Damit ist Andreas' Beobachtung am System bestätigt:** Rüsten enthält keinen Mengenterm,
Maschine und Lohn schon.

**Zwei Präzisierungen gegenüber der Literatur:**

1. Trafag nutzt **nicht** `SAP001`/`SAP002`/`SAP003`, sondern `SAP005`/`SAP006`/`SAP007`.
   Unterschied: bei Maschine/Lohn fehlt der Split-Divisor `/ SAP_11`, und beim Rüsten wird
   **mit** `SAP_11` multipliziert. Eine Aussage wie "SAP002 ist die Standardformel" trifft auf
   unser System nicht zu.
2. `SAP005`/`SAP006`/`SAP007` haben `VTERM` leer – sie sind **nur für die Kalkulation** zugelassen.
   Die Terminierung läuft über andere Formeln. Zeitwerte in der Kalkulation und in der
   Terminierung sind also nicht automatisch dieselben.

### 7.4 Wichtige Ausnahme: Rüstzeit, die doch mit der Menge skaliert

An ca. **45 Arbeitsplätzen** (67 `CRCO`-Sätze inkl. historischer Zeitscheiben) liegt auf dem
**Rüst-Slot 0001** nicht `SAP005`, sondern:

`ZAP008` = `SAP_01 * SAP_09 / ZBELAD / SAP_08` = Rüstzeit × Vorgangsmenge ÷ Beladung ÷ Basismenge

Das heisst: dort wächst die **Rüstzeit mit der Menge**, geteilt durch die Beladung (Stück je
Ladung) – klassisches Verhalten für chargen-/ladungsweise Prozesse (Ofen, Anlage, Bad), wo je
Ladung neu gerüstet wird und die Gesamtrüstzeit von der Anzahl Ladungen abhängt.

Betroffene Kostenstellen (Auszug): 409, 454, 455, 467, 483, 495, 499, 519, 523, 526, 540, 572,
618, 648, 659, 663, 666, 668, 691, 700, 711, 728, 729, 731, 740, 741, 745, 746.

**Konsequenz:** Die pauschale Regel "Rüstzeit ist losgrössenunabhängig" gilt bei Trafag **nicht
flächendeckend**. Wer Rüstkosten je Stück über die Losgrösse interpretiert, muss vorher prüfen,
welche Formel am betreffenden Arbeitsplatz hängt. Das ist auch der Grund, warum ein einzelner
`CK11N`-Test nicht auf andere Arbeitsplätze verallgemeinert werden darf.

### 7.5 Reproduktion

```
.tmp_sap_probe\bin\x86\Release\net48\SapProbe.exe table-read TC25 \
  --ashost travp762.sap.trafag.com --user KOI \
  --fields IDENT,FTEXT,VKALK,VTERM --where "VKALK = 'X'" --rowcount 100

.tmp_sap_probe\bin\x86\Release\net48\SapProbe.exe table-read CRCO \
  --ashost travp762.sap.trafag.com --user KOI \
  --fields OBJID,LANUM,KOSTL,LSTAR,FORML --where "FORML = 'ZAP008'" --rowcount 150
```

Passwort über `SAP_NCO_PASSWORD` setzen oder maskiert eingeben – das Werkzeug nimmt es
grundsätzlich nicht als Kommandozeilenargument.
