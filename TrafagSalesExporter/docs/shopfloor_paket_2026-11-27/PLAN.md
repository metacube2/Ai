# Shopfloor Paket Fabio 27.11.2026: Bauplan Punkte 12 und 14 bis 20

Stand: 2026-11-27 15:30: SAP-Teil Punkt 12 gebaut und in P76 importiert (`T76K912964`, 14:47, `saptasks/shopfloor_sap/NOTES.md` 12); Board-Punkte noch nicht gebaut. Davor mittags: nur Plan. Grundlage: Mail Fabio 27.11. 11:36 und Paket
`Downloads\Shopfloor_Paket_Ingo_2026-11-27.zip` (Mailtext Punkte 14–20, Antwort Fabio/Marco zu Punkt 12, LIESMICH mit Entscheiden).
Code-Stand gelesen im Worktree `C:\TMP\Ai_sf2611` (Zweig `shopfloor-paket-2611`, `a2f7c97`, produktiv); im Hauptordner fehlen
`ShopfloorMeeting.cs`, `ShopfloorRadar.cs`, `ShopfloorMailSender.cs`. Abkuerzungen: cfg = `Services/Shopfloor/shopfloor_config.json`,
js = `wwwroot/shopfloor/app.js`.

## Punkt 12 (ZD05): Entscheide Fabio/Marco 27.11.

- Alle Unterdeckungen zeigen: Bedingung „offene Bestellung nach Unterdeckung“ (`CHECK lv_sh_umt`) entfernen.
- Planauftraege und Banfen zaehlen nicht als Zugang; **Fertigungsauftrag zaehlt als Zugang** wie die Bestellung.
- Disponenten 001 bis 006. QE11155 ist Marcos Zeile (E11155 erledigt ist richtig). C37815: Marco zog ZD05 um 06:50, Import 06:17.

**Pruefung „rechnet ZD05 so?“: nein.** Report `ZMM_CHECK_UMTERMINIERUNG` (Repo `saptasks/shopfloor_sap/ZMM_CHECK_UMTERMINIERUNG_T76_2026-11-27.abap`
Zeilen 142–203): Zugang nur EKET (Bestellung/Lieferplan), Fertigungsauftraege fehlen. Mit FA als Zugang zeigt das Board bei Eigenfertigung
weniger Positionen als die ZD05 (wenn ein FA vor dem Bedarf kommt). An Fabio melden.

**SAP-Aenderung (T76/100, neuer Auftrag, nicht freigeben)**, `ZCL_ZPOWERBI_EINKAUF_DPC_EXT` GET_ENTITYSET Zweig `ShopZd05Set`
(Repo-Stand `DPC_EXT_GET_ENTITYSET_2026-11-26_welle2_neu.abap` Zeilen 424–640):
1. Zeilen 550–555 (Schleife `lv_sh_umt`, `CHECK lv_sh_umt`) entfernen, Variable `lv_sh_umt` streichen.
2. Nach der EKET-Schleife (Zeile 535) offene Fertigungsauftraege als Zugang (`plumi = '1'`): AFPO mit `matnr`, `pwerk = werks`,
   `elikz = space`, `xloek = space`, AUFK `loekz = space`, ohne TECO (JEST I0045 aktiv); Termin `dgltp`, Menge `psmng - wemng` (> 0).
3. Disponenten: keine SAP-Aenderung, das Set nimmt je Aufruf einen Disponenten (`Dispo eq`).

**Board:** `appsettings.json:176` `Zd05Dispo` auf 001–006 (Server-Ueberschreibung pruefen); Schleife `ShopfloorSapSync.cs:97-100`
je Disponent mit try/catch absichern (heute bricht ein Fehler den ganzen SAP-Lauf ab); +15–20 s je Disponent. Ob 005/006 in P76
existieren: offen. Mehr Positionen = Bruch in der Code-2-Reihe (Punkt 19).

## Punkte 14 bis 20

| Nr. | Thema | Ist-Stand (Beleg) | Aenderung | Aufwand |
|---|---|---|---|---|
| 14 | Go-for-it an jedem Tag | Tagesfeld `goforit` (cfg 4872–4879), Kachel `renderGoForIt` js 1031–1063 speichert auf einen Tag; Raster zeigt nur diesen Tag | Monatswert bleibt einmal gespeichert, Raster zeigt ihn an jedem Tag berechnet; Eingabe ueberschreibt den Monatswert. Nicht auf alle Tage kopieren (`CountFilledFigures` ShopfloorStore.cs 341–344 wuerde Wochenenden als erfasst zaehlen) | S |
| 14b | Status Bereich auf allen Seiten | gleich auf allen 8 Tagesseiten (SEH, TR5, TX, DW, PPD, Logistik, Unterhalt, Verkauf); ohne Status: Einkauf/ZD05, Zustaendigkeiten, Listen. Zwei Schreibwege (Ampel und Raster `saveCell` js 1197) | Fabio bestaetigen; Einkauf hat keinen Status (relevant fuer 18) | – |
| 15 | Seite „Aenderungen“ | Tabelle `history` (ShopfloorDb.cs 56–59), lesen nur je Eintrag (LIMIT 200). Erfasst: Listen, Loeschungen (`_geloescht`), Tageswerte, ZD05-Zeilen (Quelle immer „manuell“), Forecast. **Nicht** erfasst: KW-Werte Nenner/mat_verf, ZD05-Import, ABC | `GET api/history/all` mit Filtern und Seiten, Index `history(ts)`, Route `#/aenderungen`, Links, Geloeschte markieren; optional `SaveWeek` in den Verlauf | M |
| 16 | Meeting-Ablauf | Reihenfolge cfg `agenda` (5284–5404) ist schon die gewuenschte; Vorrang `meeting_modus.zuerst` (5039–5045); kein Ueberspringen; `friday` nirgends gesetzt | `"zuerst": []` (Schluessel nicht loeschen, sonst Server-Rueckfall ShopfloorMeeting.cs 33–34), neues Attribut `"ablauf": false` fuer Highlights, Zustaendigkeiten, EA, Unterseiten; Test ShopfloorPaket2611Tests.cs 93–96 anpassen | S |
| 17 | „Freitag: Gaeste“ | nichts vorhanden; `lists.abteilung` (cfg 5113–5130) hat IT und CZ, nicht GFS/TRCZ/Masterdata | Listenmodul `gaeste` mit 4 festen Zeilen (gast, info, datum, eintrag), Agenda mit `friday: true` nach Verkauf, Knopf „Pendenz anlegen“ (Vorbelegung in `openRecord` js 724), Abteilungsliste ergaenzen | M |
| 18 | Pendenz aus Status | Pendenzen = Listenmodul (cfg 300–412), Pflicht nur im Browser; `notify_rules.pendenzen` (cfg 5469–5478) kennt neu/zust/done; kein zeitgesteuertes Ereignis | Label `massnahme` -> „Sofortmassnahme (kurzfristig)“, neu `massnahme_lang`; Dialog in `renderAmpel` bei orange/rot, Verknuepfung `ampel_pendenz` am Tagessatz; Kasten offene Pendenzen unter dem Status; Ereignis `ueberfaellig` einmal je Pendenz/Zieltermin ueber `mail_state` | L |
| 19 | Materialverfuegbarkeit statt Kennzahl | `ComputeKpi` ShopfloorZd05.cs 74 (Ziel 1.20 :16), angezeigt Startseite js 371–372/388–390/424–427, ZD05 js 1378–1396/1405; KW-Werte `History` :308–335 ohne Ziel/Farbe | cfg-Block `kennzahlen_ziele` (99.6 / 0.6), Backend liefert Kachelwert (laufend/Vorwoche), neue Kachel mit Verlauf und Linie 99,6, Farben in Tabelle und YTD, Startseite umstellen; Kennzahl weiter rechnen, nicht zeigen | M |
| 20 | Radar Ziel < 0,6 % | `ShopfloorRadar.Compute` :30–67, Anzeige js 1569–1576 ohne Ziel | `ziel_pct`, `im_ziel` je Zeile, Ueberschrift „Ziel < 0,6 %“, Farben | S |

**Befund zu 19:** Einen SAP-Nenner gibt es nur fuer abgeschlossene KWs (`NennerWochen` ShopfloorSapKdTermin.cs 206–217) und der Schalter
`SapNennerEnabled` ist noch aus. Die Kachel zeigt also praktisch immer die Vorwoche (gekennzeichnet); bis der Schalter an ist, nur Handwerte.

## Reihenfolge (Vorschlag)

1. **Welle A:** 16, 20, 14, Punkt 12 Board (Schleife absichern; Disponenten 001–006 erst mit dem neuen SAP-Set), dazu SAP-Set Punkt 12 in T76.
2. **Welle B:** 19 (sinnvoll mit `SapNennerEnabled` an), 15.
3. **Welle C:** 18, 17 (teilen Vorbelegung in `openRecord` und `lists.abteilung`).

Feste Abhaengigkeiten: 16 vor 17; Vorbelegung `openRecord` vor 17 und 18.

## Offene Fragen an Fabio

1. 14: Gehoert der 1. des Monats zum Vormonat (heutige Regel D-1) oder zum Kalendermonat?
2. 15: Quellenfilter standardmaessig „alle“ oder „manuell“? KW-Werte (Nenner) in den Verlauf aufnehmen?
3. 16: Uebersprungene Punkte auf der Startseite grau weiter zeigen?
4. 17: Ist TRCZ das bestehende „CZ“? Info je Freitag ueberschreiben (Verlauf reicht) oder archivieren?
5. 18: Wie erkennt das Board „dasselbe Thema“? Vorschlag: Verknuepfung Status -> Pendenz, solange sie offen ist. Einkauf hat keinen
   Status: soll die ZD05-Seite eine Ampel bekommen? Pendenzen-Kasten nur im Meeting oder immer?
6. 19: Laufende KW mit Handwert zeigen oder immer Vorwoche? Ist 99,60 gruen? Alte Handwerte ohne Nenner einfaerben und im YTD mitzaehlen?
7. 12: Board rechnet mit FA als Zugang anders als ZD05: so gewollt, oder soll auch der Report angepasst werden?
