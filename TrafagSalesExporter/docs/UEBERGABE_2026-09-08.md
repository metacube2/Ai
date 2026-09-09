# Uebergabe: Stand am 2026-09-09

Diese Datei ist der Einstieg fuer die naechste Sitzung, egal ob Claude oder Codex. Sie
sagt, was fertig ist, was als naechstes dran ist und welche Reihenfolge zwingend ist.
Details stehen jeweils in der genannten Datei; hier steht nur, was man wissen muss, um
weiterzumachen.

## 1. Der wichtigste Punkt: zweiter Ladelauf abgeschlossen

Nach dem Deploy um **14:02** wurden die vier B1-Gesellschaften erneut geladen: TRFR 20'170,
TRIN 265'042, TRIT 162'726 und TRUS 21'770, zusammen **469'708** Journalzeilen. Danach wurde
`Finance_All_2026-09-08.xlsx` mit lokaler Datenbankkopie neu erzeugt:

```
uv run --python 3.12 Tools/FinanceAll/finance_all_xlsx.py --lokal
```

Der vollstaendige Zellscan belegt `due date`, `fiscal year` und `period` jeweils in
469'708 von 469'708 Zeilen. `cost center` und `dimension 2` bleiben wegen leerer/inaktiver
B1-Quellen leer; `Konzernkonto` wartet auf Andreas' Mapping. Das Blatt `Feldstatus` zeigt
dies je Gesellschaft.

## 2. Was heute fertig geworden ist

| Thema | Stand | Beleg |
|---|---|---|
| Erste Bahnmarkt-Datei fuer Rohail | am 08.09. geliefert; durch die aktualisierte Mappe vom 09.09. ersetzt | `Bahnmarkt_Datenaufbereitung_2026-09-08.xlsx` |
| Branchenfund in Alphaplan, 99 Bahnadressen | dokumentiert | `docs/BAHNMARKT_DE_ANBINDUNG_2026-09-08.md` |
| Journal FR, IT, US, IN | nach Deploy neu geladen, 469'708 Zeilen | Blatt `Datenstatus` |
| `Finance_All` als Gegenstueck zu `Sales_All` | neu erzeugt, geprueft und nach `Import/Finance/Alle` hochgeladen | `Finance_All_2026-09-08.xlsx` |
| Faelligkeitsdatum aus `JDT1.DueDate` | gebaut, getestet, **produktiv seit 14:02** | Commit `646a998`, Deploy in `docs/rag/DEPLOYMENT.md` |
| Werkzeug fuer die Mappe | mit `--lokal`, `--tage`, `--seit` | `Tools/FinanceAll/finance_all_xlsx.py` |
| Zwei Teams-Nachrichten | formuliert, **nicht versandt** | `docs/TEAMS_ROHAIL_BAHNMARKT_2026-09-08.md`, `docs/TEAMS_ANDREAS_ZAHLUNGSDATEN_2026-09-08.md` |
| DE-Kundenzuordnung und Bahnbranche | 4'549 von 7'615 Zeilen direkt belegt nachgezogen; 19 DE-Railway-Kunden bestaetigt | `docs/BAHNMARKT_DE_ANBINDUNG_2026-09-08.md`, Abschnitt 8 |
| Aktualisierte Datei fuer Rohail | erzeugt und rueckgelesen, nicht versandt | `Bahnmarkt_Rohail_2026-09-09.xlsx` |
| DE-Schluessel-Pruefmappe | direkte Belege, historische Kandidaten und Restliste getrennt | `Bahnmarkt_DE_Kundenzuordnung_2026-09-09.xlsx` |

## 3. Was auf andere wartet

**Deutschland / Alphaplan** liefert fuer die verbleibenden 3'066 Verkaufszeilen den
aktuellen Abgleich `RechnungsAdressenID -> AdressNummer-Kunde`. 4'549 Zeilen sind bereits
direkt belegt nachgezogen. 2'427 weitere Zeilen lassen sich historisch ableiten, werden
wegen beobachteter Nummernwechsel aber nicht automatisch freigegeben; 624 Zeilen haben
keinen Kandidaten, 15 sind mehrdeutig. Lieferantennummern duerfen nie als Kundenschluessel
verwendet werden. Details: `docs/BAHNMARKT_DE_ANBINDUNG_2026-09-08.md`, Abschnitt 8.

**Andreas** liefert den Konzernkontenplan. Das Blatt `Konten` in `Finance_All` ist die
Arbeitsliste dafuer, 915 Konten je Gesellschaft; er fuellt nur die Spalte `Konzernkonto`.
Und er beantwortet, wofuer er ein Zahlungsdatum braucht — Faelligkeitsstaffel, Cashflow
oder Altersstruktur fuehren zu drei verschiedenen Feldern.

**Patrik** prueft die 171 unbestaetigten Railway-Vorschlaege der anderen Standorte.
Produktiv bestaetigt sind jetzt 20 Kunden: 19 DE aus direktem Belegnachweis und reiner
Bahnbranche sowie der bestehende IN-Kunde. Der deutsche Kundenstamm bestaetigt keine
auslaendischen Kundenschluessel.

**Der Fachbereich** nimmt ZZPRDAT ab. Das Anschreiben liegt als Outlook-Entwurf ohne
Empfaenger und ohne Text; beides muss vor dem Versand von Hand hinein.

## 4. Was als naechstes zu tun ist, mit Reihenfolge

1. **SAP muss das CH/AT-Journal-EntitySet liefern.** Live-$metadata aus P76 belegt:
   `FinanzdataSchweizOeSet` ist ein Verkaufs-/Faktura-Set und darf nicht als Journal
   verwendet werden. `bkpfSet` enthaelt nur Koepfe, `bsisSet` nur offene Sachkontenposten
   und beide zusammen decken das Volljournal ebenfalls nicht ab. Die vollstaendige
   Pflichtfeldliste einschliesslich `Faedt` steht in `docs/FINANCE_JOURNAL.md`.
2. **Nach SAP-Bereitstellung konfigurieren und laden.** Der Gateway-Leser verwendet jetzt
   `Sites.SapEntitySet` mit Fallback `FinanzJournalSet`, prueft alle Pflichtfelder vor dem
   Abruf und uebernimmt `Faedt` nach `DueDate`; dieser Stand ist seit 15:12 produktiv.
   Den gelieferten Namen am Standort pflegen und CH/AT laden.
3. **ZC12 `p_debug` einspielen.** Vorbereitet als
   `saptasks/zc12/ZM_ABGLEICH_KTSCH_nachher.abap`, aber **nicht im System**. Der Befehl
   steht in `projektmanagement/PROJEKTSTATUS.md` unter PM-02.

## 5. Zwei Fallen, die heute Zeit gekostet haben

**Die Produktivdatenbank vor grossen Auswertungen lokal kopieren.** Ueber die Freigabe
brauchte selbst ein Zwanzigstel der Zeilen ueber zehn Minuten, mit lokaler Kopie sind es 49
Sekunden. Es bremst das Lesen ueber SMB, nicht das Schreiben der Excel. Messreihe in
`docs/router/plattform.md`.

**Wegwerftools nach Sicherheitsupdates mitziehen.** `DeployHeadless.csproj` hing noch auf
`Microsoft.Data.Sqlite 8.0.11` und brach mit `NU1605` gegen die 8.0.30 der Deploy-Konsole
ab. Nach den Paketupdates vom 01.09. betrifft das potenziell jedes Werkzeug unter
`.tmp_tools/`.

## 6. Laufende Reservierungen beim Schliessen dieser Sitzung

`docs/AGENT_COORDINATION.md` fuehrt eine offene Codex-Reservierung fuer die
ZZPRDAT-Nachdokumentation auf `lastchange.md`, `PROJEKTSTATUS.md` (nur PM-03) und
`Wochen_Todo`. Wer dort weiterarbeitet, fasst nur die eigenen Abschnitte an und erzeugt die
`.xlsx` nach TSV-Aenderungen neu.

## 7. Nachtrag Deutschland / Railway vom 09.09.2026

Commit `8a0cee6` ist seit 07:20 produktiv; 687/687 Release-Tests, DLL lokal/Server
bitgleich, Startseite, Management Cockpit, Marktsegmente und Standorte HTTP 200.
Anschliessend wurden ausschliesslich die vier Kundenfelder der direkt belegten Zeilen
und die eindeutigen DE-Bahnzuordnungen transaktional nachgezogen. Die Zeilenzahl blieb
7'615; Finanzwerte, Mengen, Kosten und Lieferantenschluessel blieben unveraendert.

Die aktuelle Dashboardquelle ist
`Sales_ProcessedMergeInput_TRDE_2026-09-09.csv`; die zugehoerige
`Sales_TRDE_2026-09-09.xlsx` liegt ebenfalls im Server-Output und im konfigurierten
SharePoint-Ordner. CSV bytegleich zurueckgelesen; bei Excel stimmen alle Zellwerte und
Formeln. Die weltweite Rohail-Mappe enthaelt acht Blaetter, 171 offene Vorschlaege,
20 bestaetigte Kunden, 286 waehrungsreine Umsatzsummenzeilen und 3'928 Umsatzdetails.
Kein Versand wurde ausgefuehrt.
