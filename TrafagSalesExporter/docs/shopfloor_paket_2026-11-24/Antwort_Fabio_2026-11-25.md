# Paket Fabio 24.11.2026: Befunde und Antwort (Stand 2026-11-25)

Quelle: Mail Fabio 24.11.2026 17:18 „Shopfloor: Patch 10 Forecast, FPY, Rückmeldungen Teamleiter und Einkauf“ mit
`Shopfloor_Paket_Ingo_2026-11-24.zip` (in Ingos Downloads entpackt: `Antwort_Einkauf_Widmer_2026-11-24.md`,
`FPY_aus_SAP_Pruefung_2026-11-20.md`, `Mail_Ingo_2026-11-24.txt`, `Patch_10_forecast_filter/`). Auftrag Ingo 25.11.:
„bittte paket bearbeiten“, „mach alles was noetig ist“, SAP T76 und P76 offen.

## Befunde vor dem Bau (produktiv nur gelesen, GET-Aufrufe im Arbeits-Chrome, 25.11. ~06:30)

| Punkt | Befund |
| --- | --- |
| 1 Patch 10 | Alle fuenf Stellen des Pakets stehen genau einmal in der produktiven `app.js`, keine alte Stelle mehr: identisch mit dem am 20.11. 12:51 deployten Patch (`e3248a4`). Nichts zu tun. |
| 4 Code 2 im Horizont | Das Board zaehlt Code 2 schon nur im Horizont heute + 3 Arbeitstage (`ShopfloorZd05.ComputeKpi`). Positionen **ohne Unterdeckungsdatum** zaehlt es als im Horizont; das muss Fabio bestaetigen. Bis 24.11. zaehlte der Horizont Mo bis Fr ohne Feiertage, seit dem Bau nach Fabrikkalender CH. |
| 4 KW 47 Zaehler | Board-Tageswerte Code 2 vom 16. bis 20.11.: 1, 1, 1, 1, 1 (hoechster Wert 1). Marco zaehlt 4 am Donnerstag. Die Codes im Board pflegt der Einkauf offenbar nicht taeglich; der Zaehler stimmt erst, wenn die Codes im Board gesetzt werden. |
| 5 Ursachenspalten | Die Rueckstandslisten haben das Feld „Verursacher / beteiligt“ mit „Einkauf“ (Excel Spalte U) und „GFS DE“ (V). |
| 5 RS-Daten | Produktiv stehen in den Rueckstandslisten noch die Eintraege aus dem Excel-Import vom 05.10. (letzte Aenderung 2026-10-05 06:33). Der Radar zeigt erst nach dem naechsten Excel-Import aktuelle Werte. |
| 5 Prod. Menge YTD | Marcos Werte vom 29.10. sind in Fabios Excel (V1.04) genau die „Produzierte Jahresmenge kumuliert“ der Zeile **28.10.**, also Stand Vortag. Board-Summe der Tageswerte 01.01. bis 28.10.: SEH 618'243 (Marco 620'677), TR5 161'958 (161'887), TX 284'961 (304'930, TX-Tagesmengen fehlen seit Oktober, sie kommen aus dem MES), DW 149'038 (148'511). |
| 5 Verzug aus dem Excel | Fabios Excel V1.04, RS-Zeilen mit Eintrag bis 29.10.: Einkauf SEH 0, TR5 534, TX 2'654, DW 1'909 (alle = Marco); GFS SEH 1'300, DW 0 (= Marco), TR5 2'006 (Marco 1'066), TX 1'417 (Marco 1'427). Keine Zeile hat beide Ursachen; Marcos Total ist deshalb Einkauf + GFS. Die Abweichung TR5/TX GFS ist nicht geklaert (Excel-Stand 17.11. statt 29.10. moeglich). |
| 8 TX +765 am 17.11. | Auftragsgenau nicht mehr pruefbar: das Board speichert die Auftragsliste je Stichtag erst seit 20.11. 04:01, vorher nur Summen. Pruefbar ist ein aktueller Stichtag gegen ZC22 `/P00 TX`. |

## Gebaut (Code `d8e74d2`, Zweig `shopfloor-paket-2411` ab Produktivstand `8b79cb7`)

- **Entfernt:** Modul „Kommunikationstool Einkauf ↔ Produktion“ (Konfiguration, Agenda-Punkt 10.1, Mailregel, Knopf auf der ZD05-Seite),
  Feld „Offene K-Auf“ (Logistik), Aufteilung A/B/C bei „Fehlmaterial Kundenauftrag (Code 2)“. Gespeicherte Daten bleiben in der Datenbank.
  Damit entfallen „Bestellnummer“ und „WE aktuell gem. SAP“ aus den Mails vom 13./16.11. Die Schnittstellen-Seite zeigt 97 statt 99 SAP-Felder.
- **Materialverfuegbarkeit je KW** (Seite Einkauf ZD05, Tabelle „Kennzahlen pro KW“): Zaehler = hoechster Tageswert Code 2 im Horizont
  Mo bis Sa (Tag wird angezeigt), Nenner = „Materialien bezogen (201/261)“ als Eingabefeld je KW, Materialverfuegbarkeit = 1 − Zaehler ÷ Nenner,
  Zeile „Durchschnitt YTD“. Ohne Nenner bleibt der fruehere Handwert stehen. Horizont jetzt nach Fabrikkalender CH.
- **Radar Verzug Einkauf und GFS (YTD)** auf derselben Seite, Endpunkt `GET api/radar?day=`: je Sparte und Total Prod. Menge YTD (Summe der
  Tageswerte 01.01. bis Vortag), Verzug Einkauf, Verzug GFS, %. Eine Zeile mit beiden Ursachen zaehlt im Total und % einmal.
- Tests: Shopfloor 187/187 im Arbeitsbaum, Release-Worktree 1'876/1'876. **Deployed 25.11. 06:45** mit dem Werkzeug, im Board gesehen
  (Radar SEH 669'248 / 0 / 1'300 / 0,19 %, TR5 176'379 / 534 / 166 / 0,40 %, TX 285'469 / 3'653 / 974 / 1,62 %, DW 163'401 / 1'909 / 0 /
  1,17 %, Total 0,66 %; Werte mit den RS-Listen vom 05.10.).

## Vorschlag Meeting-Modus (Punkt 6, nicht gebaut)

Gemessen (Board, 01.10. bis 24.11.): Tage mit Tagesziel und Produktion gibt es erst wenige (SEH 16, TR5 8, TX 3, DW 7), weil die
Tagesziele mit dem Excel-Import kommen. Eine Schwelle laesst sich daraus noch nicht ableiten. Vorschlag:

- Im Meeting-Modus pro Abteilung nur die Tagesmenge zeigen, wenn sie **mehr als 10 % unter dem Tagesziel** liegt (gelb) oder **mehr als
  20 % darunter** (rot); ueber dem Ziel nur ab +20 % als Hinweis. Die uebrigen Werte grau und eingeklappt.
- Zusaetzlich immer hervorheben, wenn eine Abteilung **drei Arbeitstage in Folge** unter Ziel liegt, auch wenn jeder Tag unter 10 % bleibt.
- Fehlteile Code 2 und offene Rueckstaende bleiben immer sichtbar und stehen im Meeting-Ablauf vorne.
- SEH ist ein Sonderfall: dort ist das „Ziel“ die offene Tagesmenge aus SAP (Abweichungen bis 69 %), nicht ein fester Wert. Fuer SEH
  zuerst ein festes Tagesziel festlegen oder SEH ausnehmen.
- Die Schwellen kommen in die Konfiguration, damit Fabio sie ohne Programmaenderung anpassen kann.

## Nicht gebaut, offen

- **Nenner automatisch aus SAP** (MATDOC 201/261, Werk 1100, Mo bis Sa, verschiedene Materialien): braucht eine neue Quelle in
  `ZPOWERBI_EINKAUF_SRV` (T76, Transport nach P76). Zusammen mit den **VF/WE-Spalten** (Punkt 7: Wunschtermin `VBBE-MBDAT`,
  bestaetigte Bereitstellung `VBEP-MBDAT`, bestaetigter Liefertermin `VBEP-EDATU`, je Plan-/Fertigungsauftrag Endtermin und
  Verfuegbarkeit) in einem Plan und einem Transport, damit Ingo nur einmal importiert. Plan folgt nach dem laufenden T76-Auftrag.
- **Punkt 6 Meeting-Modus:** nur Vorschlag an Fabio, nichts gebaut (Fabio will die Schwelle gemeinsam festlegen).
- **Punkt 8 P76-Pruefungen** (aktueller Stichtag gegen ZC22 `/P00 TX`, Naechster Vorgang gegen MD04): nach dem laufenden T76-Auftrag.
- **Punkt 2 FPY:** nichts zu tun, MES-/OfenManagement-Zugang bei Marco Di Menco angefragt.
