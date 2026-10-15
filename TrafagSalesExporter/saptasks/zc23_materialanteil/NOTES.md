# ZC23: neue Spalte Materialanteil

Stand: 2026-10-15, 14:00. **In T76 fertig, Transport `T76K912736` bereit (Freigabe und Import P76: Ingo).** Schnelle Fassung mit Schalter
(Entscheid Ingo: a + b) in `ZM_OFFENE_KDAUF` aktiv, siehe „Beschleunigung und Schalter“.
*Überholt:* Stand 13:30: **In T76 eingebaut und geprüft (Freigabe Ingo):** Transport `T76K912736` mit Struktur, Tabelle und
OData-Modell; `ZM_OFFENE_KDAUF_OPT` ($TMP) mit Materialanteil aktiv, Werte stimmen. **Offen:** Laufzeit (48 s statt 5 s für
Q2 im Dialog) entscheiden, danach Quelltext in `ZM_OFFENE_KDAUF` übernehmen. Ergebnisse unten in „Einbau in T76: Protokoll“.
*Überholt:* „In SAP noch nichts geändert“ galt bis zur Freigabe am 15.10. mittags.

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

## Einbau in T76: Protokoll (2026-10-15)

| Schritt | Ergebnis |
| --- | --- |
| Neuer Transport | `T76K912736` (Aufgabe `T76K912737`), Text „ZC23 Materialanteil (ZMM_UEB_KDAUF, ZPOWERBI_KDAUF, OData)“ |
| `ZMM_UEB_KDAUF` | `MATPREIS` (Pos. 61, `/PLMB/SPC_QUANT_QUANTITY`), `MATANTEIL` (62, `NETWR`, Referenz `T001-WAERS`), aktiv |
| `ZPOWERBI_KDAUF` | `STANDARDWERT` (11), `MATANTEIL` (12), beide `RKE_FIELDVALUE`, aktiv, Datenbank angepasst |
| `ZCL_ZPOWERBI_EINKAUF_MPC_EXT` `DEFINE` | Properties `Standardwert`, `Matanteil` (Decimal 2/15), `bind_structure` erneut; vorher/nachher `MPC_EXT_DEFINE_vorher.abap` / `_neu.abap` |
| OData T76 (Gateway Client) | `$metadata` 200; `$select=Id,Standardwert,Matanteil` 200, Gegenprobe mit erfundenem Namen 404 |
| `ZM_OFFENE_KDAUF_OPT` | eingespielt und aktiv (nur dieses Objekt aus der Liste „Inaktive Objekte“ markiert) |
| Vergleich OPT gegen REF (Q2 2026, Werk 1100) | **identisch**, 4'392 Zeilen, alle Felder (`zm_offene_kdauf/lauf21_mata_cmp.txt`) |
| ZM03 nur lesend (B80004, B87804) | Material 0,12 bzw. 0,11 je Stück, wie im Gespräch |
| OPT mit Fortschreibung (Q2) | 4'392 Zeilen in `ZPOWERBI_KDAUF` (T76-Testtabelle überschrieben), Materialanteil meist 50–70 % des Standardwerts |
| Unabhängige Nachrechnung (`mata_check.py`) | B80004 0,1154, B87804 0,1115 (= ZM03); E11221 27,44 %, 44640 232,52 je Stück, B87813 0,1115 (= ZC23) |
| **Laufzeit** | Q2 im Dialog **47,7 s** statt 5,1 s; `BI UPDATE` (15'820 Zeilen) noch nicht gemessen |

Prüfskript-Falle: `table-read` mit nur einem Feld gibt keine Spaltentrenner aus; das Skript las `BESKZ` leer und
rechnete B80004 1'000-fach zu hoch. Immer mindestens zwei Felder lesen.

Bekannte Vereinfachung (wie ZM03): Positionsmenge und Mengeneinheit der Unterkalkulation werden nicht umgerechnet.
Standardwert nutzt `STPRS` ohne Preiseinheit; bei den geprüften Materialien ist `PEINH` = 1.

### Nächste Schritte

1. Laufzeit: vorschlagen ist Vorablesen von MARC (Beschaffungsart) und CKIS gesammelt statt je Position, oder ein Schalter
   „Materialanteil rechnen“. Entscheid Ingo.
2. Danach Quelltext von OPT in `ZM_OFFENE_KDAUF` (`SapGuiSetReportSourceAuftrag.vbs 2 ZM_OFFENE_KDAUF <datei> T76K912736`).
3. Transport `T76K912736` freigeben, Import P76 (Ingo), danach `/IWFND/CACHE_CLEANUP` und `/IWBEP/CACHE_CLEANUP`.
4. Variante `BI UPDATE` als Job laufen lassen und Laufzeit gegen 23,6 s festhalten.

## Beschleunigung und Schalter (2026-10-15, Entscheid Ingo: a und b)

- **a, schneller:** `MATA_LADEN` liest stufenweise gesammelt (FOR ALL ENTRIES): Kopfkalkulationen aller Listenmaterialien,
  dann je Stufe KEKO-Köpfe, CKIS-Positionen und MARC-Beschaffungsart, dann die Unterkalkulationen der Eigenfertigung als
  nächste Stufe (höchstens 30). Gerechnet wird danach nur im Speicher (`MATA_WERT`, `MATA_IST_MATERIAL`).
- **b, Schalter:** `P_MATA` „Materialanteil rechnen“, Vorgabe **ein**. Gespeicherte Varianten (z. B. `BI UPDATE`) kennen
  das Feld nicht und nehmen die Vorgabe, Power BI bekommt den Wert also weiter. Aus: Spalten leer, ZC23 so schnell wie vorher.

| Lauf (T76, Q2 2026, Werk 1100, Dialog) | Dauer |
| --- | --- |
| erste Fassung, mit Materialanteil und Fortschreibung | 47,7 s |
| schnelle Fassung, mit Materialanteil und Fortschreibung | **13,5 s** |
| schnelle Fassung, Schalter aus, ohne Fortschreibung | 7,5 s |

Werte der schnellen Fassung = erste Fassung (25 Stichproben in `ZPOWERBI_KDAUF` auf den Rappen gleich).
Übernahme: `ZM_OFFENE_KDAUF_produktiv.abap` (= OPT, nur die Kommentarzeile „Vergleichslauf (-)“ wie im echten Programm)
mit `SapGuiSetReportSourceAuftrag.vbs` in `ZM_OFFENE_KDAUF`, aktiviert über `SapGuiInaktiveMarkieren.vbs 2 aktivieren
ZM_OFFENE_KDAUF` (fremdes `Z_REICHW` in der Liste übergangen); aktive Fassung = Datei, 0 Zeilen Unterschied.
Transport `T76K912736` / Aufgabe `T76K912737`: `ZMM_UEB_KDAUF`, `ZPOWERBI_KDAUF`, `ZCL_ZPOWERBI_EINKAUF_MPC_EXT DEFINE`,
`ZM_OFFENE_KDAUF`.

**Für Ingo:** Transport freigeben und nach P76 importieren; danach in P76 `/IWFND/CACHE_CLEANUP` und `/IWBEP/CACHE_CLEANUP`
für `ZPOWERBI_EINKAUF_MDL`, dann einmal ZC23 mit einer gewohnten Variante ansehen (neue Spalten „Materialanteil je Einheit“
und „Materialanteil“, Block „Materialanteil“ im Selektionsbild). Laufzeit `BI UPDATE` als Job danach festhalten.
