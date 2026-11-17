# Shopfloor: Entscheide und SAP-Belege vom 17.11.2026

Dieser Nachtrag ersetzt widersprechende offene Punkte und Terminannahmen im Ergänzungspaket vom 17.11. Er enthält keine Softwareänderung. Grundlage: Fabios Entscheide im Chat, Ingos dort eingefügte Antwort und fünf von Fabio bereitgestellte SAP-Bildschirmbilder.

## Verbindliche Entscheide von Fabio

- Offen/abgeschlossen: einheitlicher Abschlussstatus für Rückstandsliste und PPD. Erledigte und stornierte Einträge zählen nicht offen. Widersprüche zwischen Status und Abschlussdatum sind zu bereinigen, nicht automatisch als neue Sachentscheidung aufzulösen.
- TX-Schichtmodell: monatlich auswählbare Vorgabe; bei Änderungen wochenweise oder tageweise manuell anpassbar. Umsetzungsvorschlag: Tagesausnahme vor Wochenausnahme vor Monatsvorgabe; historische Einstellungen erhalten.
- TX-Nachtschicht 23:00–06:00 zählt zum Starttag. Für diesen Tag muss der Import auch die folgenden Morgenstunden berücksichtigen; ein Lauf vor 06:00 kann die Nachtschicht noch nicht vollständig enthalten.
- Halbe Stückzahlen aus der hälftigen Aufteilung 14:00–15:00 nicht runden. Beispiel 12.11.: 549,5 + 755,5 = 1’305 Stück im Zweischichtmodell.
- Tatsächlichen Trafag-Arbeitskalender verwenden, einschliesslich gültiger Feiertage und Betriebsschliessungen. Die Annahme nur 01./02.01. ist nicht freigegeben.
- Go-live geplant 01.01.2027. Bis dahin bleibt das verwendete Shopfloor-Excel führend und kann periodisch eingelesen werden. Importhäufigkeit ist noch nicht festgelegt.
- Dispo-Abgleich der auffälligen Einträge Ende Jahr und vor Go-live. Zunächst gegen das aktuelle Excel abgleichen; keine sofortige manuelle Entscheidung über sämtliche 139 alten Importauffälligkeiten verlangen.
- SAP-Vergleich mit vollständigem ZC22-Layout /P00 TX am 18.11. früh vorgesehen. Noch nicht ausgeführt; keine automatische Terminierung eingerichtet.

## Rückstandslisten: aktuelle Quelle

Fabio bestimmt die weiterhin verwendete Datei PPA-Shop-Floor-File-2026_Version 1.04 (2).xlsx als Grundlage. Eine unveränderte Kopie liegt unter Quellen. Ingos auffällige Einträge betreffen den Board-Import vom 05.10., nicht automatisch den aktuellen Excel-Stand.

Stichprobe: Blatt 5.1 RS, Zeile 331, RS-TX-26-327, Material 58613, Auftrag 419371/10, 50 Stück. Eckendtermin I331 und Wunschtermin J331 enthalten jeweils den 27.11.2026. Fertigungsauftrag und WE geplant sind in dieser Zeile leer. Planauftrag ist mit x markiert.

Die Excel-Kopfwerte sind gefilterte SUBTOTAL-Ergebnisse und dürfen nicht als vollständige Offen-Zählung nach einer vereinheitlichten Statusregel übernommen werden. Der vollständige Einzelfallabgleich ist noch nicht abgeschlossen und gemäss Fabio für die Go-live-Vorbereitung vorgesehen.

## SAP-Bildschirmbeleg: Material 58613, Werk 1100, Auftrag 419371/10

| Ansicht | Feld / Bezug | Wert |
|---|---|---|
| MD04, Kundenauftragsdetail | Bedarfsmenge / Bedarfstermin | 50 ST / 27.11.2026 |
| Verkaufsbeleg, Beschaffung, erste Einteilung | Bestätigte Menge / Bereit.Dat / Lieferdatum | 0 ST / 27.11.2026 / 01.12.2026 |
| Verkaufsbeleg, Beschaffung, zweite Einteilung | Bestätigte Menge / Bereit.Dat / Lieferdatum | 50 ST / 04.12.2026 / 08.12.2026 |
| MD04, Planauftrag 2526829 | Listen-Datum / Menge | 27.11.2026 / 45 ST |
| Detail Planauftrag 2526829 | AuftrEndtermin / Eckstarttermin / WE-BearbZt | 25.11.2026 / 18.11.2026 / 2 |
| MD04, Planauftrag 2534647 | Listen-Datum / Menge | 02.12.2026 / 5 ST |
| Detail Planauftrag 2534647 | AuftrEndtermin / Eckstarttermin / WE-BearbZt | 30.11.2026 / 23.11.2026 / 2 |

Folgerungen und Grenzen:

1. Der Excel-Wunschtermin entspricht in dieser Stichprobe dem MD04-Bedarfstermin, nicht dem Lieferdatum der Einteilung mit bestätigten 50 Stück. Ein bestätigtes Lieferdatum ist im Beleg der 08.12., das zugehörige Bereitstellungsdatum der 04.12.
2. Die Bilder zeigen kein ausdrücklich mit VF beschriftetes Feld und keine technischen Feldnamen. Ein technisches VF-Mapping ist damit noch nicht bewiesen. Anh Nhıs fachliche Aussage liegt vor; die Gleichsetzung VF = bestätigter Liefertermin ist anhand der Screenshots nicht haltbar als generelle Implementierungsregel.
3. Bei beiden Planaufträgen liegt der Auftragsendtermin vor dem MD04-Listen-Datum; WE-Bearbeitungszeit 2 ist sichtbar. Diese Daten nicht gleichsetzen. Für die von Anh Nhi gewünschte Produktions-Endtermin-Definition ist der Auftragsendtermin der konkrete Kandidat; die genaue Quellzuordnung muss Ingo belegen.
4. Diese Zeile hat zwei Planaufträge und keinen gezeigten Fertigungsauftrag. Sie belegt keine FA-Feldzuordnung und kein WE-Datum eines fehlenden Teils. Beide Planaufträge mit Menge und Termin erhalten; keinen einzelnen Termin stillschweigend auswählen.
5. Vorläufig keine endgültige Umbenennung in «Bestätigter Liefertermin (VF)». Ingo soll die bestehenden Excel-Werte, Bedarfs-/Bereitstellungs- und Liefertermine getrennt nachvollziehen und das vorgeschlagene Mapping zurückmelden.

## Ingos Umsetzungsstand laut seiner Antwort

- Verkauf CO-PA seit 17.11.; 02.–16.11. nachgeladen. Einzige gemeldete Abweichung AE gesamt Stand 12.11.: +CHF 3’906.68.
- Rollender Vorrat, TR5/DW-Materialfilter, DW D*, plan5 und Verzug seit 13.11.; Status ZC22 und Material ZC27 seit 16.11.; M*-Arbeitsplatzfilter seit 17.11. Vollständiger Stichtag /P00 TX erstmals 18.11.
- SAP-Anbindung Rückstandslisten noch nicht gebaut. 596 alte Board-Zeilen; 139 Auffälligkeiten laut Anlage.
- TX-Mengen aus TMS_MES, Prozedur getProdQtyPerDayOfMonthPerDisponent. MES-Lesezugang und Ausführungsrecht auf die Prozedur fehlen.
- FPY TR5/TX noch nicht gebaut; derselbe MES-Zugang fehlt. TX-Auswahl EAP1_1 bis EAP3_3 ohne EAP1_9 und EAP2_6, Referenzwert Avg Asictx aus Michaels Report. Reproduktion der Reportberechnung ist Ingos technischer Prüfauftrag, kein weiterer Entscheid von Fabio.
- Online-Dokumentation laut Ingo aktualisiert: 105 Felder, davon 44 geliefert, 56 definiert/nicht gebaut, 3 in Prüfung, 2 ohne Quelle. Dieser neue Stand wurde hier nicht erneut live verifiziert.

## Verkauf: abgeschlossener Stichtagsabgleich am 17.11.

Der spätere Live-Abgleich ersetzt den darunter dokumentierten früheren offenen Prüfstand. KE30 Z0001-000006, Buchungskreis 1100, November 2026, aktuelle Daten 17.11.2026 16:16:29: AE netto 4’326’690.09 CHF, IST netto 3’842’667.34 CHF. AE: 4’494’188.02 + 4’433.56 − 121’301.07 − 51’041.16 − (−410.74) = 4’326’690.09. IST: 4’026’350.33 + 4’172.13 − 124’582.48 − 63’202.04 − 70.60 = 3’842’667.34. Andere angezeigte Erlösminderungspositionen null. Die Selektion dieses KE30-Berichts bietet Buchungskreis, Jahr und Periode, keinen historischen HZDAT-Stichtag.

Deshalb zusätzlicher SE16N-Export CE11000: Vorgangsart A, Periode 011.2026, HZDAT bis 12.11.2026, 4’470 Zeilen bei maximal 20’000 Treffern. Export enthält 4’303 Zeilen Buchungskreis 1100 und 167 Zeilen 1200. Für die Rechnung ausschliesslich 1100 verwendet; alle diese Zeilen in CHF, Ledger B0. Datei unter Quellen/CE11000_AE_1211_Pruefung_1711.txt. Exportierte Wertspalten VV100, VV110, VV200, VV210, VV220, VV230, VV240; VV250 ist im Export mit ausgeblendeten leeren Spalten nicht enthalten. Berechnung aus den vorhandenen Erlösbestandteilen. Zeilenanzahl und Spaltenanzahl beim Parsen geprüft.

| Stichtag HZDAT bis 12.11. | Alte Kontroll-CSV | Aktueller Einzelpostenabgleich | Differenz |
|---|---:|---:|---:|
| AE gesamt | 3’372’899.64 | 3’376’806.32 | +3’906.68 |
| AE TX, WWPSP 0005 | 1’147’421.02 | 1’151’205.88 | +3’784.86 |
| AE DW, WWPSP 0003 | 1’381’146.94 | 1’381’146.94 | 0.00 |

Alle elf gespeicherten AE-Gesamt-Kontrollstände bis einschliesslich 11.11. stimmen exakt. Nur der 12.11. weicht ab. Die vier folgenden Belege vom 12.11. ergeben die Gesamtdifferenz exakt:

| CO-PA-Beleg | Kundenauftrag/Pos. | Material | Sparte | Netto CHF |
|---|---|---|---|---:|
| 3552602 | 419641/10 | 62166 | 0002 | 121.82 |
| 3552603 | 419642/10 | 42201 | 0005 | 727.56 |
| 3552604 | 419643/10 | 54538 | 0005 | 425.30 |
| 3552607 | 419644/10 | 56719 | 0005 | 2’632.00 |
| Summe | | | | 3’906.68 |

Die ebenfalls am Ende der nach TIMESTMP sortierten Tagesliste liegenden Paare 3552605/3552606 (−724.40/+724.40) und 3552608/3552609 (−221.30/+221.30) heben sich auf. Ohne diese letzten acht Zeilen ergibt sich exakt der alte AE-Gesamtwert. Das ist ein konkreter Hinweis auf einen früheren Stand der Kontrollbasis. Ohne historischen Einzelpostenexport ist aber nicht bewiesen, wann der ursprüngliche Abzug erfolgte oder ob es eine nachträgliche Datenänderung gab. TIMESTMP wird als Originalwert mitgeliefert; keine unbelegte Zeitzonen-/Zeitstempelumrechnung.

Ingos gemeldeter Mehrbetrag wird damit durch SAP reproduziert. Kein Anlass, seine AE-Gesamtsumme auf den alten Kontrollwert zurückzukorrigieren. Zusätzlich AE TX mit identischer Stichtagszuordnung gegen sein Board prüfen, weil die alte CSV dort ebenfalls niedriger liegt; dieser historische Board-Wert wurde nicht live ausgelesen. Die Unterlagen enthalten die acht betreffenden Belege und den Tagesvergleich. Rohdatei und beide Original-Workbooks bleiben unverändert.

### Historischer Prüfstand vor dem Live-Abgleich

Fabio nennt KE30 als Quelle seines Werts. Der lokale Prüfbericht vom 13.11. dokumentiert SAP-Abfragen am 13.11. zwischen 09:00 und 09:25. Das beweist nicht den Zeitpunkt von Fabios ursprünglichem KE30-Abzug am 12.11.

Die gespeicherte CE11000-Kontroll-CSV enthält für Stand_bis_HZDAT 2026-11-12 AE gesamt CHF 3’372’899.64. Falls sich Ingos Differenz auf genau diesen Wert bezieht, wäre sein Vergleichswert CHF 3’376’806.32; das ist nur eine Rechnung, kein gelesener Board-Wert. Zunächst muss Ingo die beiden tatsächlich verglichenen Werte und die Stichtagszuordnung nennen. Board-Anzeigetag und HZDAT-bis sind zu unterscheiden.

Prüfauftrag an Ingo: KE30-Bericht/Variante und Selektionen gegen CO-PA-Abfrage vergleichen, insbesondere Periode, Buchungskreis, Währung, Vorgangsarten und Datumsabgrenzung. Danach abweichende Belege benennen. Keine unbelegte Erklärung durch einen untertägigen Export und keine pauschale Differenzbuchung.

Zugriffsstand: SAP-GUI-Scripting scheiterte auch nach Anmeldung bei GetObject("SAPGUI") mit «Ungültige Syntax» (64- und 32-Bit). Über Bildschirmbedienung wurde eine eigene P76(2)-Lesesitzung geöffnet. Fabio lieferte daraus die beigefügten MD04-/Verkaufsbelegbilder. KE30 wurde noch nicht geprüft; die kleine Differenz ist nicht geklärt.
