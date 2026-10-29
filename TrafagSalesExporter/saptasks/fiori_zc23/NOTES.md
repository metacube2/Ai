# Fiori-App ZC23 (offene Kundenaufträge, live mit Selektion)

Stand: 2026-10-15, Transportstand nachgeführt am 2026-10-29, in T76 fertig und geprüft. Transport `T76K912738` (Aufgabe `T76K912739`, offen *(überholt am 2026-10-29: am 16.10. von Ingo nach P76 importiert, Caches auf `travp762` geleert, siehe `saptasks/shopfloor_sap/NOTES.md` Abschnitt 7 und Wochen_Todo Zeile 106)*),
App `ZFI_ZC23` in `$TMP` (nicht transportiert). Wunsch Ingo: „ein cooles GUI für ZC23 mit schöner Ansicht,
Trafag-Logo“; „die Logik müsste nur referenziert werden“; „die Selektionsbild-Auswahl müsste einfach mitgegeben werden“.

## 1. Öffnen

`http://travt762.sap.trafag.com:8000/sap/bc/ui5_ui5/sap/zfi_zc23/index.html?sap-client=100`

Anmeldung im Browser mit dem OData-Login. Laut Gateway-Fehlerprotokoll (`/IWFND/SU_ERRLOG`, Feld USERNAME)
läuft der Aufruf als Benutzer **`POWERBI`**.

## 2. Aufbau: die Logik bleibt in ZC23

```
App (SAPUI5)  --$filter Werk/Datvon/Datbis/...-->  ZPOWERBI_EINKAUF_SRV / Zc23Set
  ZCL_ZPOWERBI_EINKAUF_DPC_EXT->GET_ENTITYSET (Block Zc23Set)
    SUBMIT zm_offene_kdauf WITH SELECTION-TABLE ... (P_APP = X, P_UPDBI = leer) AND RETURN
      ZM_OFFENE_KDAUF: fill_uebauf -> materialanteil_rechnen -> app_export
        EXPORT tab = gt_uebauf[] TO MEMORY ID 'ZM_OFFENE_KDAUF_APP', LEAVE PROGRAM (kein ALV, keine Fortschreibung)
    IMPORT tab FROM MEMORY ID 'ZM_OFFENE_KDAUF_APP' -> ZSTR_ZC23_APP (Zeilennummer, ZMM_UEB_KDAUF, Filterfelder)
```

- **Warum nicht `P_VGL`:** Der Vergleichs-Export läuft vor `materialanteil_rechnen`; Materialanteil und Arbeit CZ
  wären leer. `P_APP` exportiert danach. `P_VGL` und damit CMP/REF bleiben unverändert.
- Filter (alle EQ): `Werk`, `Datvon`, `Datbis` (Pflicht, JJJJMMTT, Liefertermin = `S_MBDAT` = `VBEP-EDATU`,
  höchstens 366 Tage, sonst HTTP 400 ohne Reportlauf), `Kunnr`/`Matnr`/`Vbeln` (mehrfach, ALPHA/MATN1 im DPC),
  `Mata` (X/leer), `Mjung` (X = jüngste Kalkulation), `Mfrem` (X = Fremdleistung zum Material),
  `Korr` (V korrigiert, A Absagegrund, L alte Logik). `Dispo` ist kein Serverfilter: `S_DISPO` ist `AFKO-DISPO`
  (3 Zeichen), die Spalte `DISPO` ein anderer Schlüssel; die App filtert in der Liste.
- `ERNAM` (Erfasser) ist nicht im Modell (Personenfeld).

## 3. Objekte

| Objekt | Ort | Stand |
| --- | --- | --- |
| `ZM_OFFENE_KDAUF` + `P_APP`, Form `APP_EXPORT` | `saptasks/zm_offene_kdauf/bauen.js` → `ZM_OFFENE_KDAUF_OPT.abap` / `_produktiv.abap` | aktiv T76, `T76K912738` |
| Struktur `ZSTR_ZC23_APP` (71 Zeilen: ROWNR, `.INCLUDE ZMM_UEB_KDAUF`, DATVON, DATBIS, MATA, MJUNG, MFREM, KORR), Paket `ZPP` | `ZSTR_ZC23_APP_felder.txt` | aktiv T76, `T76K912738` |
| `ZCL_ZPOWERBI_EINKAUF_MPC_EXT->DEFINE` + Entity `Zc23`/`Zc23Set` (69 Properties) | `ZC23_MPC_DEFINE_ADD.abap` (aus `erzeugen.py`), ganz `MPC_EXT_DEFINE_neu.abap`, vorher `MPC_EXT_DEFINE_vorher.abap` | aktiv T76, `T76K912738` |
| `ZCL_ZPOWERBI_EINKAUF_DPC_EXT->/IWBEP/IF_MGW_APPL_SRV_RUNTIME~GET_ENTITYSET` + Block `Zc23Set` | `ZC23_DPC_GET_ENTITYSET_ADD.abap`, ganz `DPC_EXT_GET_ENTITYSET_neu.abap`, vorher `..._vorher.abap` | aktiv T76, `T76K912738` |
| App `ZFI_ZC23` (index.html, `v2/…`, Logo aus `wwwroot/shopfloor/logo.png`) | `webapp/` | T76 `$TMP` |

Die ganzen Methoden baut `methoden_bauen.py` aus dem Live-Stand (abap-read) plus Zusatz.

## 4. Prüfung (2026-10-15, T76)

| Prüfung | Ergebnis |
| --- | --- |
| Ohne Pflichtfilter | HTTP 400 „Zc23Set braucht Werk, Datvon und Datbis …“, kein Reportlauf |
| Bestehende Sets nach der Änderung | `ZPOWERBI_KDAUFSet`, `LogTaSet`, `ProductDivisionRefSet` je 200 |
| Q2 Werk 1100, Mata X, freigegeben, Korr V | 4'392 Zeilen, Standardwert 19'818'530.44, Materialanteil 6'721'286.93, Arbeit CZ 849'762.01, **gleich wie `ZPOWERBI_KDAUF`** (gefüllt mit derselben Selektion); 12,5 bis 13,7 s |
| App in Chrome | Logo, Kacheln, Liste, Suche, Spaltenmenü; Seite flüssig (16 ms je Bild) |

## 5. Protokoll und Fallen

1. Transport neu im Sichern-Dialog von SE38 (`SapGuiAuftragNeu.vbs`), `T76K912738`.
2. SE11 per `SapGuiStrukturAnlegen.vbs` + `SapGuiStrukturFelder.vbs`; `.INCLUDE;ZMM_UEB_KDAUF` funktioniert als Zeile.
   `SapGuiStrukturAnlegen.vbs` und `SapGuiCacheCleanup.vbs` nahmen fest Verbindung 0 (= P76) und sind jetzt auf
   T76-Suche umgestellt (ihre T76-Prüfung hätte abgebrochen, nicht geschrieben).
3. `SapGuiInaktiveMarkieren.vbs` vergleicht nur den Objektnamen bis zum ersten Leerzeichen: Klassennamen angeben,
   nicht „Klasse  Methode“.
4. Erster Lauf: HTTP 500, `/IWFND/CM_CONSUMER 122` „Eigenschaft 'Edatu' hat ungültigen Wert '2026-04-01'“
   (`/IWFND/SU_ERRLOG`, kein Kurzdump). Datumsfelder (DATS) als `Edm.String` Länge 8 mit `iv_bind_conversions = 'X'`.
   Länge 10 allein half nicht; mit `iv_bind_conversions = ' '` läuft es. Das Gateway liefert DATS trotzdem als
   `JJJJ-MM-TT`; die App versteht beide Formen. MATNR/KUNNR/VBELN/AUFNR/POSNR kommen roh, die App kürzt Nullen.
5. Der Report lief jedes Mal durch (Fehler kam erst beim Schreiben der Antwort), ein Kurzdump entstand nicht.
6. Schreibschritte in SAP lehnt der Auto-Modus ab; Ingo schaltet nur für den einzelnen Schreibblock auf manuell.
7. App-Cache: Dateien im Versionsordner (`v1` → `v2`); ein Upload im Deltamodus löscht den alten Ordner selbst.
8. Screenshots aus Chrome hängen, wenn der Tab im Hintergrund ist (`document.hidden`); das ist kein Einfrieren der App.

## 6. Offen

- Ingo sieht die App an. Auffällig in Q2: `Neu.Menge`/`Neu.Nettowert` zeigen in vielen Zeilen 0; das ist der Wert aus
  ZC23 (`OMENGN`/`NETWR3N`), nicht von der App; fachlich nicht geprüft.
- P76: `T76K912738` erst freigeben, wenn die App fachlich passt. Danach in P76 beide Gateway-Caches leeren.
  *Überholt am 2026-10-29: Der Auftrag ging am 16.10. zusammen mit den Shopfloor-Sets nach P76 (die Klassen waren dort gesperrt), Caches geleert; siehe `saptasks/shopfloor_sap/NOTES.md` Abschnitte 1 und 7. Offen bleibt nur die App selbst.*
  Die App selbst liegt in `$TMP`; für P76 bräuchte sie Paket und Transport (`IV_PACKAGE`, `IV_TRANSPORT_REQUEST`).
- Eventuell Launchpad-Kachel.
