# Rückmeldung an Fabio und Fabios Antwort (19.11.2026)

Stand: 2026-11-19 10:12 (Uhrzeit mit `date` gelesen). Nur Doku, im Repository und in SAP nichts geändert.
Zurück: `docs/SHOPFLOOR_2026-10-07.md` Abschnitt 27.5.

## 1. Rückmeldung Ingo an Fabio (Entwurf, nicht von Claude gesendet)

- **19.11. 09:09:** Antwort auf Fabios Mail vom 19.11. 08:17 direkt in Outlook Web (Arbeits-Chrome) als Entwurf angelegt:
  Antwort an Palma, Fabio, Betreff «AW: Shopfloor – Entscheide, Board-/Excel-Abgleich 16./17.11. und Nächster Vorgang DS».
  Inhalt: 1. Versionsstand (`591c96c` seit 09:03), 2. seit 09:03 erledigt, 3. noch abweichend mit Ursache (Vorrat TX/DW,
  Verkauf 17.11., SEH 3'024, Verzug), 4. noch nicht umgesetzt (MES, SAP-Anbindung Rückstandslisten), 5. Termine VF/WE am
  Beispiel 58613, 6. Nächster Vorgang (BAdI `ZZ_MD_ADD_COL_EZPS`, nicht `ZXM61U01`), 7. Bitte um Bestätigung (Tagesziele,
  Einlesen/155 Widersprüche), dazu die T76-Themen Prüfumfang 02/AE «Mit Bestellungen» und ZBA1/Bedingung 902.
- Vorher als Datei abgelegt: `Downloads\Mail_Fabio_Shopfloor_Paket_19-11_und_T76_Tests_2026-11-19.eml` (überholt durch den
  Outlook-Entwurf). Gesendet hat Ingo selbst; Claude sendet keine Mails.

## 2. Fabios Antwort (Paket `shopfloor-wissen.zip`, Ordner `2026-11-19_antwort_ingo`, in Ingos Downloads 10:05)

Fabios Mailtext ist ein **Entwurf mit zwei offenen Platzhaltern** (SEH 3'024, Rückbau T76). Er hat dazu in P76 selbst gelesen
(eigene Lesesitzung, SE16N). Rohdaten (CE11000-Auszüge mit Belegnummern) bleiben in den Downloads, nicht im Repo.

| Punkt | Fabios Antwort | Status |
| --- | --- | --- |
| Verkauf 17.11. | Sein KE30-Abzug lief am 17.11. zwischen 07:35:54 und 07:47:09 und enthielt schon Belege vom 17.11.: AE 9'392.51 (Belege 06:16 bis 07:33), Umsatz IST 10'408.20 (Fakturen 07:35:54). Restdifferenz 0.11 / 0.16 = Rundung des Boards. «Bis Vortag» bleibt. | **erklärt**, Board richtig |
| Nächster Vorgang | In P76 bestätigt: `SXC_ATTR` `ZZ_MD_ADD_COL_EZPS` aktiv (Definition `MD_ADD_COL_EZPS`); `ZXM61U01` gehört zu M61X0001 im Projekt `ZPP00006` (aktiv); M61X0002 in keinem CMOD-Projekt. Code von `Z_MD04_NEXT_OPERATION` nicht gelesen. | bestätigt; Abnahme gegen MD04 am 20.11. |
| Tagesziele | Einverstanden: TR5 800, DW 800, TX 1'300 bis Go-live aus dem Excel-Import, ab Go-live im Board gepflegt. | **entschieden** |
| Excel einlesen | Erste Vorschau ihm zeigen, bevor produktiv eingelesen wird. Danach liest Ingo **wöchentlich** ein, nach Fabios Freigabe der Vorschau. | **entschieden** |
| 155 Widersprüche | Bereinigen die Teamleiter der Abteilung im Excel. **Bitte an Ingo: Liste je Abteilung schicken.** | entschieden; Liste offen |
| Auftragsliste | Fabio prüft TX +765 und die Verzugssprünge 18./19.11. selbst mit der CSV gegen ZC22 /P00 TX. | wartet auf 20.11. 04:01 |
| Termine VF/WE | Mapping gut; welches Datum «Bestätigter Liefertermin (VF)» ist, klärt Fabio mit Anh Nhi. Bis dahin «Wunschtermin». | offen bei Fabio |
| SEH-Produktion 16.11. (3'024) | Platzhalter: wartet auf den Teamleiter SEH. | offen bei Fabio |
| T76 Rückbau 02/AE und T161V NB/1100 | Platzhalter: jetzt oder nach weiteren Tests? | offen bei Fabio |

## 3. Auffälligkeit in Fabios Unterlagen (nicht entscheidend)

Seine Prüfnotiz schliesst aus 0 Treffern in `MODACT` (MEMBER = M61X0002), der Exit stehe in keinem CMOD-Projekt. Seine neue
Zeile in `sap-werkzeug/wissen/SAP_LEARNINGS.md` (19.11.) sagt dagegen, `MODACT` mit MEMBER-Filter habe 0 Treffer geliefert,
obwohl der Exit wirkt; daraus dürfe man nicht «inaktiv» schliessen. Für die MD04-Spalte spielt das keine Rolle, weil sie über
das BAdI kommt; der Befund aus T76 (`saptasks/shopfloor_sap/NAECHSTER_VORGANG_ZXM61U01_2026-11-19.md`) bleibt.

## 4. Nächste Schritte

1. 20.11. nach 04:01: Auftragsliste als CSV prüfen; Forecast DS «Nächster Vorgang» gegen MD04 (C15610 1279556 bis 1279559,
   34063 1278404).
2. Excel-Import: erste Vorschau mit Fabios Datei produktiv zeigen (`apply=0`), erst nach seiner Freigabe übernehmen.
3. Liste der 155 Widersprüche je Abteilung für Fabio erstellen (aus der Vorschau bzw. dem Board, Warnzeichen).
4. Rückbau T441V 02/AE und T161V NB/1100 in T76/100 erst auf Fabios Zuruf.
