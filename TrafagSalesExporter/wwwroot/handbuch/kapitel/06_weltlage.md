# Weltlage

Das Modul Weltlage verknüpft öffentlich verfügbare Daten von aussen (Nachrichtenlage, Konjunktur, Rohstoffpreise, Wechselkurse) mit den eigenen Zahlen von Trafag: Umsatz je Kundenland, Einkauf je Lieferland, offene Bestellungen und Fremdwährungspositionen. Die Frage lautet nicht „Was passiert in der Welt?“, sondern „Wo trifft es uns?“. Das Modul richtet sich an Vertriebsleitung, Einkauf, Logistik, Finance und Geschäftsleitung; HR ist über die Standortländer der Gesellschaften vertreten, es werden keine Personendaten verwendet. Es liegt im Menü unter **Weltlage** mit den vier Unterseiten Radar, Länder, Rohstoffe und Währungen sowie Ereignisse und ist ohne Passwort für alle offen.

Die externen Daten stammen aus fünf freien Quellen ohne Anmeldung: GDELT (Ereignisse aus Nachrichten weltweit, neue Daten etwa alle 30 Minuten), FRED (Rohstoff- und Ölpreise, Finanzstress-Index), Eurostat (Industrieproduktion der EU-Länder), IMF (Wachstumsprognose) und die EZB (Referenzkurse der letzten 90 Tage). Eurostat, IMF, FRED und EZB werden alle sechs Stunden abgefragt. Die eigenen Zahlen decken die letzten 12 Monate ab, in Schweizer Franken. Das Modul ist ein erster Wurf mit bewusst einfachen, offen dokumentierten Regeln. Es liefert eine Richtung, keine Prognose und keine Handlungsempfehlung.

Jede Quelle meldet ihren Zustand (verbunden, Firewall blockiert, Fehler, ausstehend). Ist eine Quelle vom Server aus nicht erreichbar, bleibt die letzte gute Lieferung stehen, und die Seite sagt dies ausdrücklich. Hinweis: nicht geprüft, welche Quellen im Betrieb gerade erreichbar sind. Laut Dokumentation vom 5. Oktober 2026 waren FRED und IMF damals durch die Firewall blockiert; dann fehlen Rohstoffe, Ölpreis, Finanzstress und die Wachstumsprognose, und das Radar rechnet nur mit Ereignissen, Eurostat und Währungen.

## Radar

### Wozu

Das Radar gibt auf einen Blick Antwort auf die Frage, in welcher Abteilung die Weltlage uns gerade am meisten Gegenwind oder Rückenwind bringt. Es ist als Übersicht für Geschäftsleitung und Abteilungsleiter gedacht, bevor man in die Detailseiten geht.

### Was man sieht

Ein rundes Radar mit fünf Sektoren, einem je Abteilung: Verkauf, Einkauf, Finance, Logistik und HR. Ein Strahl dreht sich in etwa sechs Sekunden einmal um die Mitte, und jeder Punkt leuchtet auf, wenn der Strahl ihn überstreicht. Jeder Punkt ist ein Thema, das uns betrifft, zum Beispiel ein Absatzland, ein Lieferland, ein Rohstoff oder eine Währung. Je näher ein Punkt an der Mitte liegt, desto stärker ist die Wirkung. Rot bedeutet Gegenwind, grün Rückenwind, grau ruhig. Die Grösse des Punktes zeigt unseren Anteil, also wie wichtig das Thema für uns ist. Neben jedem Sektor steht die Punktezahl der Abteilung.

Rechts daneben stehen die stärksten Wirkungen als Liste, darunter der Zustand der Datenquellen. Ein Klick auf einen Sektor filtert auf die Abteilung, ein Klick auf einen Punkt oder eine Zeile zeigt die Begründung mit Signal, unserem Anteil und dem Betrag in CHF.

### Wie gerechnet wird

Für jedes Thema wird ein Signal zwischen minus 1 (schlecht für uns) und plus 1 (gut für uns) berechnet. Die Wirkung in Punkten ist unser Anteil innerhalb der Abteilung mal Signal mal 100.

- **Länder (Verkauf, Einkauf, Logistik, HR):** Die Lage eines Landes ist der Durchschnitt aus einem Ereignissignal und einem Konjunktursignal. Das Ereignissignal vergleicht die Nachrichtenlage der letzten drei Tage mit den 27 Tagen davor (Stimmung und Anteil an Konfliktmeldungen); es gibt erst ab 20 Ereignissen in drei Tagen ein Signal. Das Konjunktursignal beruht auf der Industrieproduktion gegenüber dem Vorjahr (nur EU-Länder) und der Wachstumsprognose des IMF.
- **Verkauf:** Anteil = Umsatz des Landes am Gesamtumsatz, nach den Finance-Regeln und ohne Konzernkunden.
- **Einkauf:** Anteil = Bestellwert des Lieferlandes. Dazu kommen Rohstoffe: Je höher der Preis in den letzten 90 Tagen gestiegen ist, desto mehr Gegenwind. Die Zuordnung der Einkäufe zu Rohstoffen erfolgt über Stichworte in Warengruppe und Positionstext.
- **Logistik:** Anteil = offener Restwert der Bestellungen je Lieferland (offene Menge mal Stückpreis; ohne Mengendaten ersatzweise der Wert ganzer offener Positionen, was die Seite dann im Themennamen kenntlich macht). Zusätzlich wirkt der Ölpreis (Brent) als Frachtkostentreiber mit festem Gewicht.
- **Finance:** je Fremdwährung die Nettoposition (Verkauf minus Einkauf in dieser Währung) und die Kursbewegung gegen CHF in 90 Tagen; dazu der Finanzstress-Index mit festem Gewicht.
- **HR:** Umsatzanteil der Gesellschaft im Standortland als Grössenmass.

Die Zahl je Abteilung fasst die Punkte der Themen der Abteilung zusammen: Je Thema (Land, Rohstoff, Währung) werden die Wirkungen summiert, anschliessend wird über die Themen der Abteilung gemittelt. So entsteht ein Wert zwischen minus 100 und plus 100.

### Einsatz im Arbeitsalltag

- **Geschäftsleitung:** Vor der Quartalsbesprechung das Radar öffnen und fragen, welche Abteilung den grössten Gegenwind hat und warum. Ein Klick auf den roten Punkt liefert die Begründung.
- **Vertriebsleiter:** Prüfen, ob ein wichtiges Absatzland in der Lage kippt, bevor Planzahlen oder Preise für das Land bestätigt werden.
- **Einkäufer:** Vor einer Lieferantenverhandlung sehen, ob das Lieferland oder ein Rohstoff unter Druck steht.
- **Controller:** Die Währungs- und Finanzstresspunkte als Anlass nehmen, die Absicherung zu besprechen.

### Grenzen und Fallstricke

- Das Signal aus GDELT ist ein Stimmungsmass aus maschinell erkannten Nachrichten, mit Fehlern und Doppelungen, keine Lagebeurteilung.
- Eurostat deckt nur EU-Länder ab. Für die Schweiz, Grossbritannien, USA, Indien und China stützt sich das Land nur auf IMF und Ereignisse.
- Die Rohstoffzuordnung über Stichworte ist grob und keine Stückliste. Die Wirkung zeigt die Richtung, nicht den Betrag.
- Logistik hat keine Frachtdaten; Ersatz sind offene Bestellungen und der Ölpreis.
- Ein grauer oder fehlender Punkt kann auch bedeuten, dass die Datenquelle blockiert ist oder zu wenige Ereignisse vorliegen.

## Länder

### Wozu

Die Seite zeigt auf einer Weltkarte, in welchen Ländern wir Geschäft haben und wie die Lage dort aussieht. Sie beantwortet die Frage, wo viel Geschäft und schlechte Lage zusammentreffen.

### Was man sieht

Eine Weltkarte mit einem Kreis je Land. Die Kreisfläche ist proportional zu unserem Geschäft im Land (Umsatz, Einkauf oder die Summe), die Farbe zeigt die Lage des Landes (rot Gegenwind, grün Rückenwind, grau keine Daten). Länder mit deutlichem Gegenwind pulsieren. Ein Auswahlfeld schaltet zwischen „Verkauf und Einkauf“, „Verkauf“ und „Einkauf“ um; die Kreisgrössen bleiben in allen Ansichten auf derselben Skala. Rechts steht eine Liste der bis zu 40 grössten Länder mit Betrag und Signalwert. Ein Klick zeigt Verkauf und Einkauf in CHF und die Begründung der Lage (Ereignisse, Konfliktanteil, Industrieproduktion, Wachstumsprognose).

### Wie gerechnet wird

Kreisgrösse: Umsatz beziehungsweise Bestellwert der letzten 12 Monate in CHF. Farbe: die Lage des Landes wie im Radar, also Durchschnitt aus Ereignissignal (letzte drei Tage gegen die vier Wochen davor) und Konjunktursignal. Als „deutlicher Gegenwind“ gilt ein Signal im klar negativen Bereich; die genaue Schwelle ist nicht im Handbuch belegt.

### Einsatz im Arbeitsalltag

- **Vertriebsleiter:** Grosse rote Kreise sind Absatzmärkte unter Druck; sie kommen zuerst in die nächste Kundenrunde.
- **Einkäufer:** In der Ansicht „Einkauf“ erkennen, ob viel Volumen in einem Land mit schlechter Lage liegt, und Alternativlieferanten prüfen.
- **Geschäftsleitung:** Klumpenrisiken sichtbar machen, etwa ein grosser Kreis in einem pulsierenden Land.

### Grenzen und Fallstricke

Grau heisst „keine Daten“, nicht „sicher“. Länder mit wenig Nachrichtenereignissen erhalten kein Signal. Die Karte ist grob und zeigt nur Länder, die in den Verkaufs- oder Einkaufsdaten vorkommen. Konzernkunden sind im Verkauf nicht enthalten.

## Rohstoffe und Währungen

### Wozu

Die Seite zeigt, wie sich Preise und Kurse bewegen, die unsere Kosten und Erlöse beeinflussen, und wie stark wir ihnen ausgesetzt sind.

### Was man sieht

Oben stehen Karten mit Verlaufslinie für Rohstoffe, Energie und Finanzstress: Kupfer, Nickel, Aluminium, Eisenerz, Erdgas Europa, Erdöl Brent und der Finanzstress-Index. Auf jeder Karte steht die Preisänderung der letzten 90 Tage in Prozent, der letzte Wert mit Datum und ein Hinweis, wofür der Wert gebraucht wird (zum Beispiel Brent für Fracht und Kunststoffe). Beim Finanzstress-Index bedeutet 0 normal und über 1 angespannte Finanzmärkte; er ersetzt fehlende Börsenindizes, für die keine frei zugängliche Quelle gefunden wurde.

Darunter steht die Währungstabelle mit der Nettoposition der letzten 12 Monate in CHF, der Kursbewegung gegen CHF in 90 Tagen und einer Wirkung (Gegenwind, Rückenwind, ruhig). Daneben zeigt ein Balkendiagramm den Einkauf nach Rohstoff.

### Wie gerechnet wird

- **Nettoposition je Währung:** Verkauf in dieser Währung minus Einkauf in dieser Währung, umgerechnet in CHF. Positiv bedeutet, wir nehmen in der Währung mehr ein, als wir ausgeben; dann schadet ein schwächerer Kurs. Negativ bedeutet Nettoeinkauf; dann hilft ein schwächerer Kurs.
- **Kursbewegung:** Veränderung des EZB-Referenzkurses gegen CHF über 90 Tage.
- **Wirkung:** Kursbewegung mal Vorzeichen der Position. Ein schwächerer Kurs bei positiver Position ist Gegenwind.
- **Rohstoffpreis:** Änderung über 90 Tage. Steigende Preise gelten für den Einkauf als Gegenwind.
- **Einkauf nach Rohstoff:** Bestellwert, dem ein Rohstoff über Stichworte im Warengruppen- und Positionstext zugeordnet wird (zum Beispiel Edelstahl zu Nickel, Messing zu Kupfer, Kunststoff zu Erdöl).

### Einsatz im Arbeitsalltag

- **Controller:** Vor der Budget- oder Forecastrunde die Währungstabelle ansehen: Welche Währung hat die grösste Nettoposition und bewegt sich gegen uns?
- **Einkäufer:** Vor Preisverhandlungen prüfen, ob der Rohstoff zuletzt gestiegen ist, und den eigenen Einkaufsanteil daneben sehen.
- **Geschäftsleitung:** Mit dem Finanzstress-Index ein Gefühl für das Marktumfeld gewinnen.

### Grenzen und Fallstricke

- Rohstoffzuordnung per Stichwort ist eine Annahme, keine Stückliste.
- Die Preise sind Weltmarktwerte aus FRED und monatlich oder täglich aktualisiert; Rohstoffreihen können dem Monatsende hinterherhinken.
- Die Nettoposition beruht auf den Zeilenwährungen der Verkaufs- und Einkaufsdaten. Hinweis: Bei EUR zeigte die Dokumentation vom 5. Oktober 2026 eine auffällige Nettoposition, die nicht geklärt war; die Zahlen sind mit Vorsicht zu lesen.
- Es gibt keine Absicherungsempfehlung. Ohne Verbindung zu FRED bleiben die Rohstoffkarten leer.

## Ereignisse

### Wozu

Die Seite zeigt die meistbeachteten Konflikt- und Negativmeldungen der letzten 24 Stunden in den Ländern, in denen wir Geschäft haben. Sie liefert den Nachrichtenhintergrund zu den Signalen im Radar.

### Was man sieht

Links eine Liste der Ereignisse mit Land, Art (zum Beispiel Protest, Drohung, Angriff, Kampfhandlung), Ort, Anzahl der Erwähnungen und einem Link zur Quelle. Die Farbe zeigt die Wirkung auf die Stabilität (rot destabilisierend). Ein Schalter beschränkt die Liste auf Länder mit unserem Geschäft; er ist standardmässig an. Rechts steht für die wichtigsten Länder der Konfliktanteil der letzten 14 Tage als Balkenreihe, mit dem aktuellen Wert in Prozent.

### Wie gerechnet wird

GDELT erkennt Ereignisse automatisch aus Nachrichten weltweit. Gezeigt werden Konflikte und stark negative Ereignisse, sortiert nach Erwähnungen. Der Konfliktanteil eines Landes ist der Anteil der Konflikt-Ereignisse an allen erkannten Ereignissen des Tages. Die Länder sind nach unserem Geschäftsvolumen (Verkauf plus Einkauf) sortiert; der heutige, erst teilweise gezählte Tag ist ausgenommen.

### Einsatz im Arbeitsalltag

- **Einkäufer:** Bei rotem Signal eines Lieferlandes nachsehen, welches Ereignis dahintersteht, und gegebenenfalls beim Lieferanten nachfragen.
- **Logistiker:** Ereignisse in Ländern mit offenen Bestellungen beachten, etwa Proteste oder Konflikte, die Transporte verzögern könnten.
- **Vertrieb:** Vor Reisen oder Kundengesprächen die Lage im Land ansehen.

### Grenzen und Fallstricke

Die Ereignisse sind maschinell erkannt und können falsch, doppelt oder falsch verortet sein. Gespeichert wird nur der Link, kein Artikeltext. Die Liste der Einzelereignisse wird nach einem Neustart des Servers aus GDELT neu aufgebaut und ist nur bei erreichbarer Quelle gefüllt. Eine Meldung ist keine bestätigte Lagebeurteilung.

## Kennzahlen-Glossar

| Kennzahl | Bedeutung | Berechnung in Worten |
|---|---|---|
| Signal | Wirkung eines Themas auf uns, von minus 1 (schlecht) bis plus 1 (gut) | Je nach Thema aus Ereignislage, Konjunktur, Preisänderung oder Kursänderung berechnet und auf den Bereich minus 1 bis plus 1 begrenzt |
| Wirkung (Punkte) | Gewichtete Stärke eines Themas für die Abteilung | Unser Anteil innerhalb der Abteilung mal Signal mal 100 |
| Punkte je Abteilung | Gesamtbild einer Abteilung, minus 100 bis plus 100 | Wirkungen je Thema summiert, danach über die Themen der Abteilung gemittelt |
| Lage des Landes | Zustand eines Landes | Durchschnitt aus Ereignissignal und Konjunktursignal |
| Ereignissignal | Nachrichtenlage eines Landes | Letzte drei Tage gegen die 27 Tage davor: Veränderung von Stimmung und Konfliktanteil, Abzug bei Konfliktanteil über 25 Prozent; erst ab 20 Ereignissen |
| Konjunktursignal | Wirtschaftslage eines Landes | Industrieproduktion gegenüber Vorjahr und IMF-Wachstumsprognose, Durchschnitt der vorhandenen Werte |
| Konfliktanteil | Anteil Konfliktmeldungen | Konfliktereignisse geteilt durch alle erkannten Ereignisse des Landes |
| Nettoposition Währung | Währungsrisiko in CHF | Verkauf in der Währung minus Einkauf in der Währung, 12 Monate |
| Kursbewegung 90 Tage | Veränderung des Kurses gegen CHF | Heutiger EZB-Referenzkurs gegen den Kurs vor 90 Tagen, in Prozent |
| Finanzstress-Index | Spannung an den Finanzmärkten | Wöchentlicher Index der US-Notenbank St. Louis (FRED); 0 normal, über 1 angespannt |
| Offene Bestellungen | Noch nicht gelieferter Teil der Einkaufsbestellungen | Offene Menge mal Stückpreis, ohne bereits als geliefert gekennzeichnete Positionen |
