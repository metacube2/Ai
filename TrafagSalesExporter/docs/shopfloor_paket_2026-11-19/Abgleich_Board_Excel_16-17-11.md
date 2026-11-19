# Abgleich Board / Excel am 16. und 17.11.2026

Quellen: Fabios fünf Board-Screenshots (SEH, TR5, TX, DW, Verkauf) und die heruntergeladene Datei PPA-Shop-Floor-File-2026_Version 1.04 (2).xlsx. Gespeicherte Zellwerte, keine Neuberechnung. Produktion: Zeilen 329/330 für 16./17.11.; Verkauf: Zeilen 328/329. Die Datei trägt die Download-Dateisystemzeit 17.11. 15:42:05; das ist kein Nachweis des jeweiligen SAP-Ablesezeitpunkts.

Fabio bestätigt: Ingo hat das letzte Paket noch nicht hochgeladen. Die folgenden Werte beschreiben den IST-Stand vor dessen Übernahme, keine Prüfung einer bereits ausgerollten neuen Version. Die Nichtübernahme ist nicht automatisch die Ursache aller Differenzen.

## Produktion: 17.11., Board gegenüber Excel

| Bereich | Vorrat total Board | Excel D330 | Differenz | Offene KW Board | Excel K330 | Differenz |
|---|---:|---:|---:|---:|---:|---:|
| SEH | 157889 | 158229 | -340 | 8257 | 8477 | -220 |
| TR5 | 60986 | 61088 | -102 | 2468 | 2011 | +457 |
| TX | 52214 | 51449 | +765 | 4675 | 3573 | +1102 |
| DW | 49101 | 48851 | +250 | 2511 | 2047 | +464 |

Vorrat 3/2/1 Monate am 17.11., jeweils Board / Excel E330/F330/G330:

- SEH: 82900/83240; 55321/55523; 41525/41815.
- TR5: 34030/34012; 24545/24349; 17141/16880.
- TX: 36481/35716; 27417/26652; 21569/20804. Alle drei Fenster und der Gesamtvorrat weichen um dieselben +765 Stück ab. Ansatzpunkt für einen Vergleich der enthaltenen Aufträge, kein Beweis einer bestimmten Filterursache.
- DW: 34140/33818; 24313/23961; 16263/15890.

Am 16.11. Vorrat total Board / Excel D329: SEH 156078/155649 (+429), TR5 62011/61742 (+269), TX 48720/47563 (+1157), DW 49677/49473 (+204). Offene KW Board / Excel K329: SEH 10780/10495, TR5 3332/2654, TX 6486/4996, DW 3132/2938.

## Tageswerte, fehlende Eingaben und PPD

- SEH 16.11.: Tagesziel/offene Tagesmenge Board 1338, Excel L329 1520; Produktion Board 2699, Excel M329 3024. Am 17.11. Ziel 1469 identisch; Excel M330 noch leer, Board 2454. Leer nicht als null oder als Fehlernachweis gegen das Board behandeln.
- TR5-Produktion stimmt an beiden Tagen: 846/814. Excel-Ziel L329/L330 jeweils 800, Board leer. Board-Abweichung 846/814 gegenüber Excel N329/N330 46/14. Fehlendes Ziel darf nicht als null in eine scheinbar gültige Abweichung eingehen.
- TX Planmenge nächste 5 AT: 16.11. Board 7328 gegen Excel L329 6173 (+1155); 17.11. 6115 gegen L330 5350 (+765). Excel-Ziel M329/M330 jeweils 1300, Board leer. Produktion 16.11. Excel N329 1065, Board leer; 17.11. beide leer. MES-Anbindung fehlt laut Ingo.
- DW 16.11.: DL790 843 und DLQ72 721 stimmen mit Excel M329/N329. Ziel Excel L329/L330 jeweils 800, Board leer. Abweichung 16.11. Board 843 statt Excel O329 43. Am 17.11. Excel M330/N330 leer; Board 427/478. Kein belastbarer Mengenvergleich für diese leeren Excel-Zellen.
- Verzug Board / Excel, 16. und 17.11.: SEH 85/20 und 80/80; TR5 533/287 und 563/523; TX 733/337 und 545/490; DW 531/240 und 336/331. Excel-Spalten O bei SEH/TR5, P bei TX/DW. PPD-Quelle separat abgleichen.
- Terminverschiebungen Board leer; Excel 16./17.11.: SEH P329/P330 0/0; TR5 P329/P330 3863/3913; TX Q329/Q330 1186/1125; DW Q329/Q330 112/112. Alte Rückstandslisten und Abschlussregel sind bekannte offene Voraussetzungen; konkrete Ursache der leeren Werte noch nicht geprüft.

## Verkauf: 17.11., angezeigter Boardwert gegenüber Excel

| Kennzahl | Board CHF | Excel CHF | Board minus Excel CHF |
|---|---:|---:|---:|
| AE gesamt | 4072455 | 4081847.40 | -9392.40 |
| Umsatz IST | 3514315 | 3524723.04 | -10408.04 |
| AE Transmitter TR5 + TX | 1375255 | 1383747.20 | -8492.20 |
| AE DW | 1711890 | 1711890.38 | -0.38 |
| Umsatz Transmitter TR5 + TX | 1136200 | 1140349.00 | -4149.00 |
| Umsatz DW | 1761926 | 1762871.48 | -945.48 |
| Tagesumsatz | 98114 | 84388.30 | +13725.70 |

Excel-Zellen D329/E329/N329/O329/R329/S329/G329. Das Board zeigt ganze Franken; Differenzen hier sind aus gerundeten Anzeigewerten, keine centgenaue Backend-Prüfung. AE DW ist mit reiner Anzeige-Rundung vereinbar. Die grösseren Differenzen werden nicht durch Rundung erklärt. Tagesumsatz ist eine Differenz zweier Tagesstände; beide Tage prüfen.

Erwarteter Monatsumsatz: Board Fiori 6673807 CHF gegenüber Excel H329 5974004.35 CHF. Unterschied 699802.65 CHF, jedoch laut Entscheid vom 13.11. bewusst andere Quellen (Fiori bleibt statt ZC23). Nicht als gewöhnlichen Schnittstellenfehler oder als Pflicht zur Excel-Gleichheit behandeln.

Die am 12.11. durch Einzelposten erklärte AE-Differenz von 3906.68 CHF erklärt diese neueren Differenzen nicht automatisch. Auch der aktuelle KE30-Monatsstand vom 17.11. 16:16:29 ist nicht mit einem Boardstand bis zum Vortag gleichzusetzen.

## Abnahmeauftrag an Ingo

1. Übernommenes Paket/Versionsstand und tatsächlichen Aktivierungszeitpunkt nennen; vorbereitete Änderungen von bereits gelieferten Feldern trennen.
2. Vor-/Nachvergleich nach Paketübernahme. Gleiche SAP-Selektion, gleicher Datenstichtag, dokumentierte Abrufzeit und identischer Mengenbegriff. Historische 16./17.11.-Bestände benötigen deren Snapshots; heutige SAP-Daten rekonstruieren sie nicht automatisch.
3. ZC22 mit vollständigem Layout /P00 TX auf Auftragsebene vergleichen: nur im Board, nur im Excel/SAP-Abzug, unterschiedliche Restmenge. Keine pauschale Erklärung durch Abrufzeit ohne Beleg. Vergleich 18.11. früh vorgesehen.
4. Ziele und weitere manuelle Angaben aus dem führenden Excel übernehmen bzw. Pflege sicherstellen. Abgeleitete Kennzahlen bei fehlender Grundlage als fehlend behandeln.
5. Verkauf für 16./17.11. anhand CO-PA-Belegen und HZDAT-Abgrenzung erneut abgleichen, einschliesslich Teilbereichen und Tagesumsatz. Keine Rückkorrektur bestätigter SAP-Werte allein zur Anpassung an alte Excel-Abzüge.
6. SEH-Produktion, PPD-Verzug und Terminverschiebungen separat prüfen. Der Dispo-Bereinigungstermin Ende Jahr verschiebt nicht die Prüfung der bereits gelieferten Vorratskennzahlen.

Diese Produktions- und neueren Verkaufswerte sind noch nicht fachlich abgenommen. Bisher bestätigt sind nur die konkret geprüften Einzelwerte bzw. der separate historische CO-PA-Abgleich.
