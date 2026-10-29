# Arbeitsstand 14. und 15. Oktober 2026

Stand: 2026-10-15, 11:30. Zusammenfassung aller Arbeiten dieser beiden Tage, auf Wunsch von Ingo. Die Einzelheiten
stehen in den verlinkten Fachdateien; gültige Statusquelle für Aufgaben bleibt `projektmanagement/Wochen_Todo.tsv`.

Legende: ✅ produktiv · ⏳ wartet auf jemanden · ➖ nicht nötig · ❌ nicht gemacht

## 1. Operations (Shopfloor und KPI)

| Was | Stand | Commit | Details |
| --- | --- | --- | --- |
| Feldmapping aus `Shopfloor_SAP_Feldmapping.xlsx`: Abteilungen über den Disponenten, Quelle SAP statt MES ausser FPY | ✅ 14.10. 10:04 | `b613f7c` | `docs/SHOPFLOOR_2026-10-07.md` Abschnitt 12 |
| Menü Operations > KPI (Übersicht, OTD/OTIF/OTP) und Operations > Shopfloor | ✅ 14.10. | `89e9c7c` | `docs/OPERATIONS_KPI_2026-10-14.md` |
| KPI Liefertreue live aus SAP, Abnahme bestanden (OTD 90.6, OTIF 89.9, OTP 73.0) | ✅ 14.10. 10:33 | `982e193` | technischer Benutzer `POWERBI`, Rolle `ZF_BR_SALES_MANAGER` befristet bis 31.10.2026 |
| Monatsabruf KPI abgesichert: Versuche überstehen Neustart, Warnung bei fehlendem Vormonat, Eintrag in den Admin-Logs | ✅ 14.10. | `55b42d9` | Wunsch Fabio |
| Fabios Patches: ZD05-Filter „Zu besprechen“, Verkauf mit Go-for-it-Kacheln | ✅ 15.10. 08:50 | `52b3b75` | Paket in `docs/shopfloor_paket_2026-10-14/` |
| Schnittstelle nimmt Fabios Push-Format an (ohne `items`-Hülle, `values` statt `fields`, Adresse `/BiDashboard/api/integration/push`) | ✅ 15.10. 11:06 | `f4e3f1b` | `docs/SHOPFLOOR_2026-10-07.md` Abschnitt 13 |
| ZD05 automatisch aus SAP | ✅ wie bisher | | Das Dashboard holt selbst, 05:45 und 12:45 *(ueberholt am 2026-10-16: 04:00 und 12:45, Entscheid Ingo, `docs/SHOPFLOOR_2026-10-07.md`)* |
| Logistik-Mengen (Job J5, ZC27) automatisch | ⏳ Fragen an Fabio | | ohne SAP-Änderung baubar, siehe Abschnitt 6 |
| Verkauf AE und Umsatz (Jobs J2/J3, CO-PA) automatisch | ⏳ Fragen an Fabio, danach SAP-Entwicklung | | neue OData-Abfrage in T76, Transport nach P76 |
| SAP-Job und Einplanung SM36 um 04:00, wie in Fabios Chat beschrieben | ➖ | | Das Dashboard holt die Daten selbst, kein Push aus SAP nötig |
| Fabios neue Feldliste (`feldliste.json`) in die Konfiguration übernehmen | ❌ offen | | Wochen_Todo Zeile 99 |
| Jobs J4, J6 bis J10 (Gutschriften/Plan, Produktion, Rückstände, Tagesmengen, Sensoren, FPY) | ⏳ bei Fabio „offen“ | | Antworten der Teamleiter ausstehend |

## 2. Startseite und Kopfleiste

| Was | Stand | Commit |
| --- | --- | --- |
| CPU-Pegelanzeige in der Kopfleiste (Server und Cockpit-Prozess) | ✅ 14.10. | `a735b41`, `5235e16` |
| Designkorrekturen Kopfleiste (Dimmer, Pegel flacher, Logo-Abstand) | ✅ 14.10. | `5b51011` |
| Neuer Untertitel der Startseite in allen Sprachen | ✅ 14.10. | `d911cb1` |
| Animierte Standortgrafik mit Live-Zustand je Land statt festem Bild | ✅ 14.10. | `ce5d27d` |
| Manometer standardmässig aus, Schalter im Admin (in der DB, übersteht Deploys); Titel „Trafag Dashboard“ | ✅ 14.10. | `cc9c6ab` |
| Überschrift „Trafag Global BI Dashboard“, Browser-Tab „Trafag Dashboard“ | ✅ 14.10. | `b5b4727` |

## 3. Admin und Zugänge

| Was | Stand | Commit |
| --- | --- | --- |
| Anonyme Nutzungsmessung, Seite Admin > Nutzung (keine Namen, Ingo zählt nicht) | ✅ 14.10. | `4615268`, Befund in `docs/NUTZUNG_2026-10-14.md` |
| Seite Admin > Passwörter: Admin, Finance, HR (auch `hradmin`), Operations, Logistik neu setzen, ohne das alte Passwort | ✅ 15.10. 10:56 | `e1afd49` |
| Passwort-Hashes in der Datenbank statt in `appsettings.json` (Deploy überschreibt sie nicht mehr) | ✅ 15.10. | `e1afd49` |
| Neues Admin-Passwort setzen | ⏳ Ingo | auf der neuen Seite, bis dahin gilt das alte |
| Alte Hashes aus `appsettings.json` im Repository entfernen | ⏳ nach dem Neusetzen | Wochen_Todo Zeile 100 |

## 4. Doku und Projektführung

| Was | Stand | Commit |
| --- | --- | --- |
| Handbuch: Kapitel 10 Operations, Rollenführer Produktion, Kopfleiste, Admin-Seite Passwörter | ✅ | `f38020f`, `1519cb3` |
| `router.md`: Abschnitt, wie der Auto-Modus ohne Unterbruch weiterläuft | ✅ 14.10. | `af622af` |
| Wochen_Todo durchgesehen (Zeilen 36, 87, 95, 96 nachgeführt, neu 97 bis 102) | ✅ TSV | `a9f8638`, `856b4d6`; `Wochen_Todo.xlsx` war in Excel offen und ist noch nicht neu erzeugt *(ueberholt: seither mehrfach neu erzeugt, zuletzt mit Commit `affb3b5` am 2026-10-27)* |
| RAG PROJECT und ARCHITECTURE auf Stand 15.10. | ✅ | `dbe700b` |
| Themenlandkarte als SVG mit Generator | ✅ | `545d6e0`, `docs/THEMENLANDKARTE_2026-10-15.svg` |
| Datenbanksicherung 14.10. 11:36, Deploy-Werkzeug mit Schalter `--only-backup` | ✅ | `7013249` |

## 5. Deploys

14.10.: mehrere Deploys zwischen 10:04 und 14:18 (Einzelheiten in `docs/rag/DEPLOYMENT.md`). Beim Deploy um ~13:30 lief
vermutlich ein Einkauf-Delta, das der Neustart abgebrochen hat; seither wird vor jedem Deploy geprüft oder gefragt
(`docs/rag/LEARNINGS.md`, Wochen_Todo Zeile 98).

15.10.: 08:50 Fabios Patches, 10:56 Passwörter, 11:06 Push-Format. Jeweils bitgleich, alle Prüfseiten HTTP 200.

## 6. Was Fabio noch liefern muss

Logistik (J5, ZC27):

1. Meint „Rückmeldedatum“ das Buchungsdatum (AFRU-BUDAT) oder das Erfassungsdatum (AFRU-ERSDA)?
2. Werden stornierte Rückmeldungen abgezogen wie in ZC27?
3. Montag: Vortag (Sonntag) oder letzter Arbeitstag (Freitag)?
4. Zuordnung Arbeitsplatz zu Feld bestätigen (MLE01, MLE02, MLE04, MLM01 einzeln, MLE08 + MLE09 summiert).
5. Kontrollwerte für einen Tag mit mehreren Arbeitsplätzen (bekannt nur MLE01 am 13.10. = 673).

Verkauf (J2/J3, CO-PA):

6. Name des Ergebnisbereichs, Tabelle `CE1xxxx`.
7. Technische Namen der Wertfelder (VV…) für die Nettoerlös-Formel.
8. Technischer Name des Merkmals Produktsparte.
9. Periode nach Buchungsperiode oder Fakturadatum, und was am 1. eines Monats gilt.

Allgemein:

10. Muss der Stand um 04:00 da sein, oder reicht 05:45 wie bei ZD05?
11. Jobs J4, J6 bis J10 erst, wenn sie im Blatt „Jobs 04-00“ auf „definiert“ stehen.

Noch ungeklärt: Fabio sah ZD05-Lieferungen um 14:45 (13.10.) und 14:21 (14.10.), die nicht zu den eingestellten
Zeiten 05:45 und 12:45 passen. Nicht nachgeprüft. *(Die Zeiten sind seit 2026-10-16 04:00 und 12:45.)*

## 7. Offene Punkte bei Ingo

- Admin-Passwort auf der Seite Admin > Passwörter neu setzen.
- Fabio Server-Adresse und Token für die Schnittstelle geben, Fragen aus Abschnitt 6 an ihn schicken.
- Schmale SAP-Rolle für `POWERBI` beim Hauptadmin bestellen, bevor `ZF_BR_SALES_MANAGER` am 31.10.2026 abläuft.
- Transport `T76K912728` (ZC23) freigeben und nach P76 importieren. *(ueberholt am 2026-10-29: am 15.10. freigegeben und von Ingo nach P76 importiert, am 16.10. in P76 geprueft, `saptasks/shopfloor_sap/NOTES.md`, `saptasks/zc23_materialanteil/NOTES.md`)*
- Entscheide aus Wochen_Todo: 98 (Einkauf-Delta 14.10.), 99 (Ablauf mit Fabio), 100 (Hashes aus dem Repository). *(98 erledigt am 22.10.: Delta 14.10. erfolgreich, Lauf 15.10. sofort wiederholt)*
- `Wochen_Todo.xlsx` schliessen, damit sie neu erzeugt werden kann. *(erledigt, siehe Abschnitt 4)*
