# ZC23: neue Spalte Materialanteil

Stand: 2026-10-15, Nachmittag. Analyse abgeschlossen, Entscheide Ingo da, ABAP im Repository fertig vorbereitet.
**In SAP noch nichts geändert:** Das Eintragen der Felder in SE11 hat der Auto-Modus abgelehnt (Änderung an einem gemeinsamen System). Ingo muss das freigeben oder die Schritte in Abschnitt „Einbau in T76“ selbst anstossen.

## Wunsch (Gespräch Ingo mit dem Anfragenden, Aufnahme 2026-10-15)

ZC23 (`ZM_OFFENE_KDAUF`) zeigt heute den **Standardwert** = Standardpreis (MBEW-STPRS) × Menge, über alle Positionen
rund 57,8 Mio. CHF Herstellkosten bei rund 71 Mio. Umsatz. Neu soll daneben der **Materialanteil in CHF** stehen: der Teil
der Herstellkosten, der auf eingekauftes Material entfällt. Arbeit, Gemeinkosten und Zuschläge rechnet der Anfragende
selbst als Differenz zum Standardwert. Vorbild ist die Preisliste Tschechien (ZM03), die genau diese Aufteilung macht.

Beispiel aus dem Gespräch (Stahlmembran, Material B8…04, Kalkulationslosgrösse 1'200): CK11N 1,81 je Stück, davon
Material 138,47 / 1'200 = 0,12 und Arbeit 1,68.

## Transaktionen

| Transaktion | Programm | Bemerkung |
| --- | --- | --- |
| ZM03 | `ZLO_CALC_CZ_PREISE` | „Berechnung Werksabgabepreise CZ“, Quelltext in `ZLO_CALC_CZ_PREISE.abap` (T76, 2022 von IN23914) |
| CK11N | `SAPLCKDI` (SAP-Standard) | Erzeugniskalkulation anlegen. „ZK11N“ aus dem Auftrag gibt es in T76 nicht (TSTC geprüft), gemeint ist CK11N |

## Wie ZM03 rechnet

1. Je Material die jüngste Standardkalkulation: KEKO mit `KALKA = '01'`, `TVERS = 1`, `KADKY ≤ Stichtag`, nicht gelöscht,
   ohne Fehlerstatus `KF`.
2. Positionen der Kalkulation aus CKIS (`LEDNR = '00'`), Wert `WRTFW_KPF` für die Kalkulationslosgrösse:
   - **Typ M/I (Material):** Zukaufteil (`MARC-BESKZ = 'F'`), ohne eigene Kalkulation oder aus einem anderen Standort gilt
     als **Material**. Eigengefertigte Halbfabrikate werden **nicht** als Material gezählt, sondern rekursiv aufgelöst
     (Tabelle `KALTAB`, Faktor Menge / Losgrösse der Unterkalkulation). Abbruch bei mehr als 1'000 Unterkalkulationen.
   - **Typ E (Eigenleistung):** Arbeit, getrennt nach tschechischen Arbeitsplätzen (CRHD `ARBPL LIKE 'CZ%'`) und übrigen.
   - **Typ F (Fremdleistung):** eigene Spalte.
   - Andere Typen (z. B. Gemeinkostenzuschläge) werden nicht gezählt.
3. Materialwert = Summe Material über alle Stufen / Losgrösse = **Materialanteil je Stück**.

Damit ist die Falle „Halbfabrikat enthält schon Arbeit“ gelöst: Der Materialanteil ist reines Zukaufmaterial über
alle Stufen.

## Vorschlag für ZC23

- *Überholt durch Abschnitt „Lösung“: Feld heisst `MATPREIS` statt `MATANTEIL_STK`.* Neue Felder `MATANTEIL_STK` (Materialanteil je Stück) und `MATANTEIL` (= `MATANTEIL_STK` × `BMENG`), Spalte
  „Materialanteil“ in der ALV-Ausgabe neben `STANDARDWERT`.
- Rechnung wie ZM03, aber über **alle** Arbeitsplätze (keine CZ-Trennung nötig), Stichtag heute.
- **Je Material nur einmal rechnen** und zwischenspeichern: ZC23 hat bis rund 16'000 Positionen, aber viel weniger
  verschiedene Materialien. Die Auflösung kostet je Material mehrere Tabellenzugriffe.
- Einbau in die schnelle Fassung (`ZM_OFFENE_KDAUF_OPT`, Stand von Transport `T76K912728`), also erst nach oder zusammen
  mit dessen Import nach P76. Danach Vergleich gegen ZM03 an einigen Materialien (Beispiel B8…04: 0,12).

## Entscheide Ingo (2026-10-15)

1. Kalkulation **wählbar im Selektionsbild**: freigegebene (Vorgabe) oder jüngste wie ZM03.
2. Fremdleistung **wählbar im Selektionsbild** (Ankreuzfeld, Vorgabe aus = nicht Material).
3. Standardwert und Materialanteil kommen auch in `ZPOWERBI_KDAUF` und damit in den OData-Dienst `ZPOWERBI_EINKAUF` (Power BI).
   Hinweis: Unser BI-Dashboard liest `ZPOWERBI_KDAUF` nicht; die Tabelle hatte bisher nur Schlüssel und Termine.

## Frühere offene Fragen (beantwortet, siehe oben)

1. Kalkulation: die jüngste wie ZM03, oder die zum freigegebenen Standardpreis gehörende (kann bei Vormerkungen
   abweichen)? Vorschlag: die freigegebene, damit Standardwert und Materialanteil zusammenpassen.
2. Fremdleistungen (Typ F): zum Material zählen oder zur Arbeit? ZM03 führt sie separat.
3. Soll die Spalte auch in `ZPOWERBI_KDAUF` und damit ins Dashboard?

## Lösung (im Repository, `saptasks/zm_offene_kdauf/`)

Gebaut wie am 12.10. über Bausteine und `node bauen.js`; REF bleibt unverändert, nur OPT bekommt den Materialanteil.

| Baustein | Inhalt |
| --- | --- |
| `teile/opt_mata_screen.abap` | Block „Materialanteil“: `P_MFREI` (freigegeben, Vorgabe) / `P_MJUNG` (jüngste), `P_MFREM` Fremdleistung zum Material |
| `teile/opt_mata_init.abap` | Texte des Blocks (ohne Textelemente, gehen beim Zurückkopieren mit) |
| `teile/opt_mata_global.abap` | Puffer je Kalkulation und je Material, Zähler ohne Kalkulation / Materialanteil > Standardwert |
| `teile/opt_mata_forms.abap` | `MATERIALANTEIL_RECHNEN`, `MATA_MATERIAL`, `MATA_KALKULATION` (rekursiv über CKIS-Unterkalkulation `UKALN/UKALKA/UKADKY/UTVERS/UBWVAR`) |
| `teile/opt_mata_fieldcat.abap` | ALV-Spalten `MATPREIS` (je Einheit) und `MATANTEIL` (Summe) |
| `teile/opt_schreiben.abap` | `ZPOWERBI_KDAUF-STANDARDWERT` und `-MATANTEIL` werden mitgeschrieben |
| `bauen.js` | `startOpt`: `PERFORM materialanteil_rechnen` **nach** `vergleich_export`, damit der CMP-Vergleich gegen REF unberührt bleibt |

Rechenregel je Kalkulation: Material = Summe CKIS-`WRTFW_KPF` der Positionen M/I, die Zukaufteil (`BESKZ = F`), ohne
Unterkalkulation oder aus einem anderen Werk sind; Eigenfertigung rekursiv über die von der Position verwendete
Unterkalkulation (Wert je Einheit × Positionsmenge); F nur mit `P_MFREM`; E und Zuschläge nie. Geteilt durch `KEKO-LOSGR`.
Abweichungen von ZM03: Unterkalkulation exakt die der Position (ZM03 sucht die jüngste neu); „anderes Werk“ statt
ZM03-Standortvergleich (Schwesterwerk am selben Standort zählt hier als Material).

## Einbau in T76 (noch offen, Reihenfolge)

1. **Neuer Transportauftrag** (T76K912728 ist seit 15.10. freigegeben). Beim Aktivieren nie den Vorschlag nehmen.
2. SE11 `ZMM_UEB_KDAUF` (Struktur, Paket ZPP1): `MATPREIS` Typ `/PLMB/SPC_QUANT_QUANTITY` (wie STANDARDPREIS),
   `MATANTEIL` Typ `NETWR` mit Währungsreferenz `T001-WAERS` (wie STANDARDWERT), ab Zeile 60 anhängen
   (`SapGuiStrukturFelder.vbs 2 <datei> 60`, dann `SapGuiWaehrungsref.vbs 2 MATANTEIL=T001.WAERS`, aktivieren).
3. SE11 `ZPOWERBI_KDAUF` (Tabelle, Paket ZPP): `STANDARDWERT` und `MATANTEIL` Typ `RKE_FIELDVALUE` (DEC 15,2, ohne
   Währungsreferenz), aktivieren (Tabelle wird angepasst, alte Zeilen leer).
4. `ZCL_ZPOWERBI_EINKAUF_MPC_EXT` `DEFINE`: Entity `ZPOWERBI_KDAUF` holen, Properties `Standardwert`, `Matanteil`
   (`set_type_edm_decimal`, precision 2, maxlength 16) anlegen, `bind_structure( 'ZPOWERBI_KDAUF' )` erneut.
   Lesecode braucht keine Änderung (`SELECT *` INTO CORRESPONDING).
5. `ZM_OFFENE_KDAUF_OPT` ($TMP) mit dem neuen Stand einspielen (`einspielen.sh OPT`), prüfen:
   CMP OPT gegen REF ohne Abweichung; Beispiel B8…04 ergibt 0,12 wie ZM03; Laufzeit `BI UPDATE` gegen 23,6 s;
   Zähler „ohne Kalkulation“ und „Materialanteil > Standardwert“ ansehen; TYPPS-Werte der Testmaterialien auflisten.
6. Danach Quelltext nach `ZM_OFFENE_KDAUF` (`SapGuiSetReportSourceAuftrag.vbs 2 ZM_OFFENE_KDAUF <datei> <neuer Auftrag>`).
7. Nach Import P76: `/IWFND/CACHE_CLEANUP` und `/IWBEP/CACHE_CLEANUP` für `ZPOWERBI_EINKAUF_MDL`.

## Learnings dieser Aufgabe

- **SAP-Skripte nahmen fest die erste GUI-Verbindung** (`app.Children(0)`). Am 15.10. war das P76; die Skripte brachen
  zum Glück mit „Sitzung ist nicht T76“ ab. Die neun hier gebrauchten Skripte suchen jetzt die T76-Verbindung selbst.
  Die übrigen rund 20 Skripte haben das Muster noch.
- **Nie in Ingos offene Sitzungen schreiben:** Er hatte CK11N und ZM03 offen. Eigene Sitzung mit `CreateSession` öffnen.
- **ZK11N gibt es nicht**, gemeint ist CK11N; ZM03 = `ZLO_CALC_CZ_PREISE` (über TSTC gefunden).
- **ZC23 gibt über die DDIC-Struktur `ZMM_UEB_KDAUF` aus**; eine neue Spalte braucht dort ein Feld, nicht nur im Programm.
- **`ZPOWERBI_KDAUF` ist kein Dashboard-Futter**, sondern Power BI über `ZPOWERBI_EINKAUF`; Frage nach „Dashboard“ vorher präzisieren.
- **Auto-Modus lehnt SE11-Änderungen ab** („Modify Shared Resources“); vor DDIC-Arbeit Freigabe durch Ingo einholen.
