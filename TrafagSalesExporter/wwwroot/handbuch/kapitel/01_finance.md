# Finance und Management

Das Finance Cockpit ist der Bereich des Trafag Cockpits, in dem die Umsätze aller Gesellschaften zu einer vergleichbaren Konzernsicht zusammengeführt, gegen Sollwerte geprüft und für das Management aufbereitet werden. Hauptnutzer sind die Finance-Keyuser und Controller, die Finance-Leitung und die Geschäftsleitung. Die Administratoren pflegen im Admin-Bereich die technischen Grundlagen.

Im Menü liegt alles unter dem Hauptpunkt „Finance Cockpit". Dort stehen der Export Dashboard, die Gruppe „Management Analyse" (mit Schnellübersicht, Management Entscheidungen und den Expertenseiten), der Soll/Ist Vergleich, das Controlling, die Finance Schulung, die Manuellen Importe, der Journal Import und die Marktsegmente. Der Admin-Bereich ist ein eigener Hauptpunkt ausserhalb von Finance.

Die Daten stammen aus den Verkaufssystemen der Gesellschaften: SAP in der Schweiz und Österreich, SAP Business One in Frankreich, Italien, den USA und Indien, Alphaplan in Deutschland sowie Sage in Spanien und Grossbritannien. Je Standort werden sie geladen, nach festen Regeln aufbereitet (Finance-Regeln, Gutschriften mit negativem Vorzeichen, Ausschluss von Weiterberechnungen) und in der zentralen Datenbank abgelegt. Die Zahlen sind deshalb so aktuell wie der letzte Standortexport. Wie aktuell, zeigt die Seite Export Dashboard und die Seite Datenstatus.

Der Zugang zu den meisten Seiten dieses Kapitels ist durch eine Finance-Freischaltung geschützt: Export Dashboard, Management Analyse, Soll/Ist Vergleich, Finance Schulung, Standorte, Transformationen, Finance Regeln, Settings und Logs verlangen eine Freischaltung, solange diese in der Anwendung aktiviert ist. Das Controlling, die Manuellen Importe, der Journal Import und die Marktsegmente verlangen gemäss Routenprüfung keine solche Freischaltung. Mit dem Menüpunkt „Finance sperren" wird die Freischaltung wieder zurückgenommen.

Ein Grundprinzip gilt für das ganze Kapitel: Führend ist die Hauswährung des jeweiligen Landessystems. Umrechnungen nach CHF dienen der Anzeige und dem Konzernvergleich. Fehlende Werte, etwa ein fehlender Kurs oder eine fehlende Kostenbasis, werden sichtbar markiert und nie geschätzt.

## Export Dashboard

### Wozu

Das Export Dashboard ist die Steuerzentrale der Datenversorgung. Hier wird ausgelöst, dass die Verkaufsdaten der Standorte neu aus den Quellsystemen gelesen werden, und hier ist ablesbar, wie aktuell die Daten je Land sind. Ohne diese Seite lässt sich nicht beurteilen, ob eine Auswertung im Cockpit den heutigen Stand oder den von vor drei Wochen zeigt.

### Was man sieht

Oben stehen zwei Schaltflächen und eine Anzeige. „Alle Standorte laden" startet den Export für alle aktiven Standorte. „Zentrale Datei neu erzeugen" baut aus den bereits geladenen Standortdaten die zentralen Dateien neu auf, ohne die Quellsysteme erneut abzufragen. Daneben steht der nächste automatische Lauf oder der Hinweis „Timer deaktiviert". Rechts oben dreht sich ein Zeigerinstrument mit der Aufschrift „EXPORT". Es ist reine Dekoration und zeigt keine Messwerte.

Darunter erscheinen bei Bedarf zwei Hinweise. Der gelbe Hinweis nennt aktive Standorte, die noch nicht vollständig bereit sind. Der blaue Hinweis sagt, dass seit der letzten zentralen Excel-Datei mindestens ein Standort neu exportiert wurde und die zentrale Datei deshalb neu erzeugt werden sollte.

Die Tabelle führt je Standort Land, Basis (SAP Service, Server, Excel-Datei, CSV-Datei), Kürzel des Standorts (TSC), Schema, Server, Status des letzten Laufs, eine Fortschrittsmeldung, die Zeilenzahl, den Datenstand, die Dauer und zwei Schaltflächen. „Export" lädt nur diesen Standort, „Excel öffnen" öffnet die zugehörige Standortdatei. Beim Datenstand steht ein farbiges Kennzeichen, woher der jüngste Stand stammt: aus der Datenbank (grün), aus einer neueren Datei auf SharePoint (orange), aus einer lokalen Datei (blau) oder nur aus dem Export-Log (grau). Erscheint zusätzlich „CSV neuer als DB", liegt eine vorbereitete Datei vor, die noch nicht in der Datenbank ist. Unter der Tabelle zeigt der Block „Zentrale Datei" die erzeugte Sales-All-Datei mit Pfad und letzter Änderung.

### Wie gerechnet wird

Es wird nicht gerechnet, sondern verglichen. Der Datenstand eines Standorts ist der jüngste Zeitpunkt aus Datenbank, Dateien und Export-Log. Ist eine Datei neuer als die Datenbank, wird das gekennzeichnet. Die Seite aktualisiert sich während eines laufenden Exports alle drei Sekunden selbst.

### Einsatz im Arbeitsalltag

**Finance-Keyuser:** Nach jeder neu bereitgestellten Datei zuerst den betroffenen Standort exportieren und erst danach die zentrale Datei neu erzeugen. Die richtige Reihenfolge lautet: Datei bereitstellen, Standort exportieren, zentrale Datei erzeugen, Summen prüfen, Soll/Ist freigeben.

**Controller:** Vor dem Monatsabschluss in der Spalte „Datenstand" prüfen, ob jedes Land ein aktuelles Datum und die Quelle „DB" zeigt. Steht dort „SharePoint-CSV" oder „CSV neuer als DB", ist die Datenbank hinter den bereitgestellten Dateien zurück.

**Finance-Leitung:** Vor der Freigabe der Konzernzahlen sicherstellen, dass kein Standort mit Fehlersymbol dasteht. Der Fehlertext erscheint beim Daraufzeigen.

### Grenzen und Fallstricke

Eine Datei hochzuladen ändert die Finance-Zahlen noch nicht. Erst der Standortexport schreibt in die Datenbank, und erst die neu erzeugte zentrale Datei enthält diesen Stand. „Alle Standorte laden" ersetzt je Standort den bisherigen Bestand vollständig, bei Dateistandorten wie Deutschland und Spanien deshalb nur mit vollständigen Dateien. Der Zeitstempel in „Zentrale Datei" ist der lokale Dateizeitstempel des Servers, nicht der von SharePoint. Das Zeigerinstrument sagt nichts über den Zustand der Exporte aus.

## Schnellübersicht

### Wozu

Die Schnellübersicht ist die erste Seite der Management Analyse und beantwortet auf einen Blick: Ist der Umsatz des gewählten Jahres plausibel, stimmen die Länder mit den Sollwerten überein, und wie frisch sind die Daten? Sie ist für Controller und Geschäftsleitung der Einstieg am Morgen.

### Was man sieht

Über allen Reitern der Management Analyse steht ein Filterbalken mit Finance-Jahr, Land, Währung und dem Schalter „Group-Währung (CHF)". Mit „Finance Summary laden" werden die Zahlen neu geholt. Beim Öffnen wird das jüngste Jahr in den Daten vorgewählt.

Die Kacheln zeigen den Net Sales Actual des gewählten Jahres, die Zahl der Länder mit Status OK („Soll/Ist ohne Abweichung"), die Zahl der Länder mit Status „Prüfen" („Abweichung oder offene Regel"), die Zahl der nicht geprüften Länder („ohne Sollwert oder ohne Daten") und die Zahl der aktiven Datenstandorte. Darunter führen vier Schaltflächen zu Management Entscheidungen, Gruppenmarge, Finance Prüfbuch und 3D Datenanalyse, dazu der Excel-Export.

Es folgen drei Tabellen. „Finance-Freigabe je Land" zeigt Status, Ist, Soll, Differenz, Datenstand und einen Hinweis wie „Freigabefähig", „Kein Sollwert gepflegt" oder „Abweichung prüfen". „Letzter Datenstand je Standort" zeigt, wann welcher Standort zuletzt Daten geliefert hat und ob ein manueller Import dahintersteht. „Sparten-Abdeckung nach Land" zeigt, welcher Anteil des Umsatzes einer Produktsparte zugeordnet werden konnte.

### Wie gerechnet wird

Der Net Sales Actual ist die Summe der Nettofakturawerte aller Zeilen, die nach den Finance-Regeln eingeschlossen sind, im gewählten Jahr. Das Jahr bestimmt das Buchungsdatum, ersatzweise das Rechnungsdatum, danach das Extraktionsdatum. Für Spanien gilt das Rechnungsdatum zuerst. Der Status eines Landes ist „OK", wenn Ist und Soll höchstens um eine Währungseinheit auseinanderliegen, sonst „Prüfen". Ohne Sollwert steht „Kein Sollwert", gibt es einen Sollwert, aber keine Ist-Zeile, steht „Keine Daten". Die Kachel „Nicht geprüft" zählt die beiden letzten Fälle.

### Einsatz im Arbeitsalltag

**Controller:** Vor dem Monatsabschluss das Jahr wählen und prüfen, ob „Zu prüfen" und „Nicht geprüft" klein sind. Eine Null bei „Länder OK" und „Zu prüfen" bedeutet nicht, dass alles sauber ist, sondern dass nichts geprüft wurde. Fehlen für das Jahr alle Sollwerte, erscheint dazu eine ausdrückliche Warnung.

**Geschäftsleitung:** Die Kachel Net Sales Actual und die Länderliste genügen für einen Überblick. Bei mehreren Währungen im Filter erscheint „Mixed"; dann ist die Summe keine umgerechnete Zahl, und der Schalter „Group-Währung (CHF)" oder ein Landfilter schafft Abhilfe.

### Grenzen und Fallstricke

Sollwerte sind nur für einzelne Jahre und Länder gepflegt, nach dem letzten dokumentierten Stand nur für 2025, und für die Schweiz fehlt der Sollwert. Für das laufende Jahr ist der Soll/Ist-Abgleich deshalb weitgehend leer. Der Status OK sagt aus, dass das Ist zum Soll passt, nicht dass die Zahl fachlich richtig ist. Die Schnellübersicht ist eine Plausibilitätssicht; die fachlich führende Sicht ist die Finance Summary.

## Management Entscheidungen

### Wozu

Diese Seite verdichtet Auffälligkeiten aus Finance, Einkauf und HR zu einer priorisierten Liste möglicher Managemententscheide. Sie soll die Frage beantworten: Wo muss jemand eine Entscheidung treffen, weil Zahlen nicht belastbar sind oder ein Risiko wächst?

### Was man sieht

Vier Kacheln zeigen die Zahl der offenen Entscheidungen, wie viele davon hohe Priorität haben, wie viele Bereiche betroffen sind und den Stand der Berechnung. Der Hinweis „aggregiert, ohne HR-Personendaten" ist ernst gemeint: Aus HR erscheinen nur Ampelwerte und Datenqualitätsthemen. Der „Entscheidungsradar" ist eine Tabelle mit Priorität (Stufe und Punktzahl), Bereich, Thema mit Quelle, Kennzahl, Auswirkung, Empfehlung, der konkreten Entscheidungsfrage, dem Verantwortlichen und einer Schaltfläche „Details", die zur passenden Seite führt. Gibt es keine Signale, steht dort „Keine offenen Management-Entscheidungen".

### Wie gerechnet wird

Die Liste entsteht aus festen Regeln. Aus Finance kommen die bis zu acht grössten Soll/Ist-Abweichungen (ab fünf Prozent Abweichung hohe, sonst mittlere Priorität), die offene Kostenbasis der Gruppenmarge (immer hohe Priorität), ein Hinweis zur Spartenzuordnung, wenn mindestens fünf Prozent des Umsatzes nicht sauber zugeordnet sind (ab 15 Prozent hoch), und die sechs häufigsten Datenqualitätsthemen. Aus dem Einkauf kommen der offene Bestellwert (ab einer Million CHF hoch), das Liefertermin-Risiko (ab 500'000 CHF hoch) und Datenqualitätsfehler (ab 1'000 hoch). Aus HR kommen rote und gelbe Ampeln und Datenqualitätsthemen. Die Punktzahl von 0 bis 100 setzt sich aus der Stufe, der Grösse des Befunds und dem betroffenen Volumen zusammen. Sortiert wird nach Stufe, dann nach Punktzahl.

### Einsatz im Arbeitsalltag

**Geschäftsleitung:** Vor der Sitzung die Zeilen mit hoher Priorität durchgehen und je Zeile klären, wer die genannte Entscheidung trifft.

**Controller:** Als Arbeitsliste für die Nacharbeit nutzen: Über „Details" gelangt man direkt in die zuständige Expertenseite, zum Beispiel Abweichungen oder Gruppenmarge.

**Einkäufer:** Die Zeilen mit Bereich „Einkauf" zeigen, welche Einkaufsthemen das Management sieht.

### Grenzen und Fallstricke

Die Seite erzeugt Hinweise, keine Beschlüsse. Fehlen Einkaufs- oder HR-Daten, erscheint stattdessen eine Info-Zeile mit der Fehlermeldung. Die Schwellen sind fest eingebaut und nicht einstellbar. Die Liste hängt vom oben gewählten Finance-Jahr ab.

## Finance Summary (Experten)

### Wozu

Die Finance Summary ist die fachlich führende Sicht auf den Net Sales Actual. Sie entspricht dem Excel-Blatt „Finance Summary" der zentralen Datei und dient dazu, Cockpit und Excel gegeneinander abzustimmen.

### Was man sieht

Vier Kacheln: Net Sales Actual, enthaltene Zeilen (Finance Include = TRUE), ausgeschlossene Zeilen und die Zahl der Länder und Währungen. Links steht die Tabelle „Summen wie im Excel-Blatt Finance Summary" mit Jahr, Land, Währung, Net Sales Actual und den Zeilenzahlen. Rechts erscheinen die Hinweise zu den angewandten Regeln. Darunter vergleicht die Tabelle „Jahresvergleich mit aktuellem Filter" die Jahre. Der Excel-Export liegt oben.

### Wie gerechnet wird

Je Zeile gilt der Nettofakturawert der Position in der Hauswährung des Landes. Gutschriften und Stornos sind negative Zeilen. Eine Zeile ist ausgeschlossen, wenn eine Finance-Regel sie aussondert (etwa Weiterberechnungen oder interne Kunden) oder wenn ihr Wert null ist. Die Summe je Land und Währung ist die Summe der eingeschlossenen Zeilen.

### Einsatz im Arbeitsalltag

**Controller:** Die Zeilenzahlen und Summen mit dem Excel-Blatt der zentralen Datei vergleichen. Stimmen sie nicht, ist meist die zentrale Datei veraltet.

**Revisor:** Die Tabelle zusammen mit den Hinweisen als Nachweis der angewandten Abgrenzung verwenden.

### Grenzen und Fallstricke

Werden mehrere Währungen gleichzeitig angezeigt, steht „Mixed", und die Gesamtsumme addiert lokale Beträge ohne Umrechnung. Die Spalte „Ausgeschlossen" enthält auch Zeilen mit Wert null, die keine Regel ausgeschlossen hat. Die Trennung zeigt die Seite Datenqualität.

## Länder Diagnose (Experten)

### Wozu

Die Länder Diagnose zeigt je Land, ob das Ist zum Soll passt, und trennt dabei konzerninterne Umsätze ab. Sie beantwortet: Wie viel vom Umsatz eines Landes geht an Konzerngesellschaften oder nahestehende Firmen, und stimmt der Rest mit dem Soll überein?

### Was man sieht

Eine Tabelle mit Status, Land, TSC, Quelle, Währung, Ist, IC/2nd-party, Ist ohne IC, Soll, Differenz sowie den Zeilenzahlen (eingeschlossen / ausgeschlossen).

### Wie gerechnet wird

IC/2nd-party ist der Umsatz mit Kunden, die einer gepflegten Liste konzerninterner und nahestehender Firmen entsprechen. Die Erkennung läuft über Namensbestandteile, zum Beispiel Trafag, Magnetic Sense oder die Gesellschaft für Sensorik, sowie über einzelne Kundennummern. „Ist ohne IC" ist Ist minus IC. Der Status entsteht wie in der Schnellübersicht aus dem Vergleich von Ist und Soll.

### Einsatz im Arbeitsalltag

**Controller:** Bei einer Abweichung prüfen, ob die Differenz verschwindet, wenn man den IC-Anteil abzieht. Dann ist die Ursache die Abgrenzung und nicht ein Datenfehler.

**Finance-Leitung:** Den IC-Anteil je Land als Grundlage für Konsolidierungsfragen heranziehen.

### Grenzen und Fallstricke

Der Abzug dient der Diagnose und ändert die Daten nicht. Die Namenserkennung ist einfach; ein ähnlich lautender Fremdkunde kann fälschlich erfasst werden. Die Liste wird im Admin-Bereich gepflegt.

## Datenstatus (Experten)

### Wozu

Der Datenstatus listet je Standort, welche Datenmenge liegt und wann sie zuletzt geliefert wurde. Er beantwortet die Frage, ob ein auffälliger Umsatz an den Daten oder am Land liegt.

### Was man sieht

Je Standort: aktiv oder nicht, Land, TSC, Quelle, zentrale Zeilen, letzter Export, Exportstatus, letzte Speicherung in der Datenbank und bei Standorten mit manueller Datei der Dateiname mit Zeitpunkt des Uploads.

### Wie gerechnet wird

Es werden Zählungen und Zeitpunkte aus der zentralen Datenbank, dem Export-Log und den Importdateien gezeigt. Die Schnellübersicht verwendet für ihren Datenstand dieselbe Logik wie das Export Dashboard.

### Einsatz im Arbeitsalltag

**Finance-Keyuser:** Bei manuellen Ländern prüfen, ob die aktuelle Datei wirklich hochgeladen wurde.

**Controller:** Bei fehlenden Umsätzen eines Landes zuerst hier den letzten Export ansehen.

### Grenzen und Fallstricke

Ein erfolgreicher Export bedeutet nicht, dass die Quelle neue Daten geliefert hat. Das zeigt erst der Daten-Heartbeat.

## Abweichungen (Experten)

### Wozu

Die Seite listet alle Länder, bei denen Ist und Soll auseinanderliegen. Sie ist die Arbeitsliste für die Abstimmung vor der Freigabe.

### Was man sieht

Status, Land, Währung, Ist, Soll, Differenz und Differenz in Prozent. Ohne Sollwerte oder ohne Abweichung erscheint der Hinweis „Keine Sollwerte oder keine Abweichungen für diese Filter".

### Wie gerechnet wird

Differenz ist Ist minus Soll. Als Abweichung gilt, was eine Währungseinheit übersteigt. Das Prozentmass setzt die Differenz ins Verhältnis zum Soll.

### Einsatz im Arbeitsalltag

**Controller:** Jede Zeile fachlich klären: Ist der Sollwert veraltet, fehlt Importumfang, oder muss eine Finance-Regel angepasst werden? Die Management Entscheidungen übernehmen die grössten Zeilen dieser Liste.

### Grenzen und Fallstricke

Die Toleranz von einer Währungseinheit gilt für alle Währungen gleich, obwohl eine Einheit bei Indischer Rupie etwas völlig anderes bedeutet als bei Schweizer Franken. Die Seite ist leer, wenn für das Jahr keine Sollwerte gepflegt sind.

## Gutschriften (Experten)

### Wozu

Die Seite zeigt Rechnungen, die wie Gutschriften aussehen. Sie hilft zu prüfen, ob Rückerstattungen richtig negativ geführt werden und welche Belege ungewöhnlich sind.

### Was man sieht

Land, TSC, Rechnungsnummer, Belegtyp, Wert, Menge und der Grund, warum die Zeile als Kandidat gilt.

### Wie gerechnet wird

Kandidat ist eine Zeile mit negativem Wert, negativem Rohwert oder einem Belegtyp beziehungsweise einer Belegnummer, die nach Gutschrift aussieht. Die Zeilen werden je Rechnung zusammengefasst.

### Einsatz im Arbeitsalltag

**Controller:** Vor dem Abschluss die grössten Gutschriften prüfen und mit Debitorenbuchhaltung oder Verkauf abgleichen.

### Grenzen und Fallstricke

Die Seite zeigt ausdrücklich technische Kandidaten und ersetzt keine landesspezifische fachliche Freigabe. Gutschriften mit Menge null sind ein bekannter offener Punkt der Fachabstimmung und verfälschen unter Umständen Mengen- und Preisanalysen.

## Datenqualität (Experten)

### Wozu

Die Seite zählt, wie viele Zeilen wichtige Angaben vermissen. Sie zeigt, wo die Quelle nachgebessert werden muss, bevor eine Auswertung belastbar ist.

### Was man sieht

Eine Tabelle mit Schweregrad, Prüfpunkt und Anzahl. Mögliche Prüfpunkte sind fehlende Materialnummer, fehlende Produktgruppe, fehlende Währung, fehlender Kunde, fehlendes Rechnungsdatum, fehlendes Buchungsdatum, Nullwerte im Finance-Wert und ausgeschlossene Zeilen. Es erscheinen nur Punkte mit mindestens einem Treffer.

### Wie gerechnet wird

Jeder Prüfpunkt ist eine einfache Zählung über die Zeilen des Filters. „Nullwerte" und „Ausgeschlossene Zeilen" schliessen sich aus: Eine Zeile, die eine Regel ausgeschlossen hat, zählt nur bei den ausgeschlossenen. Sortiert wird nach Anzahl.

### Einsatz im Arbeitsalltag

**Datenverantwortliche und Keyuser:** Die Liste als Auftrag an die Standorte verwenden. Die sechs häufigsten Punkte erscheinen zusätzlich im Entscheidungsradar.

### Grenzen und Fallstricke

Ein fehlendes Buchungsdatum ist in einzelnen Ländern normal, zum Beispiel in Spanien, wo das Rechnungsdatum führend ist. Die Anzahl allein sagt nichts über den betroffenen Umsatz.

## Finance Pivot (Experten)

### Wozu

Der Finance Pivot bildet das Excel-Blatt „piv" von Andreas nach. Er zeigt den Umsatz in CHF nach Jahr, Monat und Standort und vergleicht einen gewählten Monat Tag für Tag über die Jahre. So lässt sich erkennen, ob ein Monat im Verlauf und im Vergleich zum Vorjahr normal aussieht.

### Was man sieht

Filter für Jahr, MTD-Monat und TSC. Vier Kacheln: Jahresumsatz des gewählten Jahres, Monatsumsatz des gewählten Monats, Zahl der TSC-Spalten und die Zeilenbasis. Die Tabelle „Monate nach TSC" hat je Jahr und Monat eine Spalte pro Standort mit Gesamtergebnis. Die Tabelle „Tage im gewählten Monat" zeigt den Tagesumsatz je Jahr nebeneinander.

### Wie gerechnet wird

Alle Beträge sind in CHF. Umgerechnet wird mit dem Budgetkurs des Finance-Jahres (Standard seit 7. Oktober 2026, Kursprofil in den Settings); 2025 also mit Budget 2025, 2026 mit Budget 2026. „Jahresumsatz" summiert alle Monate des Jahres, bei einem abgeschlossenen Jahr ist das der Jahreswert.

### Einsatz im Arbeitsalltag

**Controller:** Den Monatsabschluss gegen den gleichen Monat des Vorjahres und die Tage darin vergleichen. Lücken in einzelnen Tagen deuten auf Ladeprobleme hin.

**Geschäftsleitung:** Die Spalte je Standort zeigt, welche Gesellschaft den Monat trägt.

### Grenzen und Fallstricke

Zeilen ohne CHF-Kurs für Jahr und Währung oder ohne TSC fehlen im Pivot. Eine Warnung nennt deren Anzahl, und die Zeilenbasis ist deshalb kleiner als die enthaltenen Zeilen der Finance Summary.

## Sparten-Finanzanalyse (Experten)

### Wozu

Die Sparten-Finanzanalyse zeigt den Umsatz nach Produktsparte und beantwortet, welcher Anteil des Umsatzes einer Sparte zugeordnet werden kann. Sie ist die Grundlage für jede Aussage wie „Sparte X macht 20 Prozent des Umsatzes".

### Was man sieht

Kacheln für Gesamtumsatz, zugeordneten Umsatz, Übrige, nicht zugeordnet und „Nicht im Stamm", jeweils mit Prozentwert. Die Tabelle „Umsatz nach Produktsparte" lässt sich nach Sparte, Produktfamilie oder Hierarchie gruppieren und auf die zehn grössten Zeilen beschränken. „Grösste Treiber: Nicht im Stamm" nennt die Materialien mit dem grössten Umsatz ohne Stammeintrag. „Umsatzabdeckung nach Land" zeigt je Land die Anteile und die Abdeckung.

### Wie gerechnet wird

Jede Verkaufszeile wird über die Materialnummer gegen die führende Referenz der Trafag AG geprüft. Die Spartenangaben der lokalen Systeme werden nicht verwendet. Daraus entstehen fünf Zustände: zugeordnet, Übrige (eine gültige Sammelsparte), nicht zugeordnet (Material bekannt, aber ohne Sparte), nicht im Stamm (Material in der Referenz unbekannt) und Material fehlt (Zeile ohne Materialnummer). Die Abdeckung ist der Anteil von „zugeordnet" plus „Übrige" am Gesamtumsatz.

### Einsatz im Arbeitsalltag

**Controller:** Die Abdeckung je Land prüfen, bevor Spartenzahlen weitergegeben werden. Ab fünf Prozent ungeklärtem Umsatz erscheint ein Hinweis im Entscheidungsradar.

**Produktmanagement und Einkauf:** Die Treiberliste „Nicht im Stamm" abarbeiten, um Stammdaten zu ergänzen.

### Grenzen und Fallstricke

Bei mehreren Währungen im Filter sind Prozentwerte unzuverlässig, die Seite warnt dann. Die deutschen Artikelnummern sind keine SAP-Materialnummern, deshalb ist die Spartenzuordnung für Deutschland unsicher.

## Zentrale Spartenzuordnung (Experten)

### Wozu

Die zentrale Spartenzuordnung prüft nicht den Umsatz, sondern die Materialnummern selbst: Welche Materialien der Länder lassen sich der Trafag-AG-Referenz zuordnen? Sie ist die Arbeitsgrundlage für die Stammdatenpflege.

### Was man sieht

Kacheln für Prüfzeilen, zugeordnet, Übrige, nicht zugeordnet, nicht im Stamm, Material fehlt und den Umfang der TR-AG-Referenz. Die Tabelle „Abdeckung nach Land" zeigt die Trefferquote je Land. Die Tabelle „Materialprüfung gegen TR-AG-Referenz" listet je Material Status, Land-Material und Text, die gefundene TR-AG-Nummer, Hierarchie, Produktfamilie, Sparte, Zeilen und Finance-Wert.

### Wie gerechnet wird

Eine Prüfzeile ist ein Material an einem Standort; dasselbe Material aus drei Ländern ergibt drei Zeilen, damit die Statuskacheln aufsummiert den Gesamtwert ergeben. Die Trefferquote ist der Anteil der zugeordneten und der Sammelsparten-Materialien.

### Einsatz im Arbeitsalltag

**Product Management:** Die Liste „Nicht im Stamm" sortiert nach Finance-Wert zeigt, welche Materialien zuerst in die Referenz aufgenommen werden sollten.

### Grenzen und Fallstricke

Die Referenz der Trafag AG gilt als führend. Ein Material, das nur lokal existiert, erscheint immer als „Nicht im Stamm", auch wenn es fachlich korrekt ist.

## Finance Prüfbuch (Experten)

### Wozu

Das Prüfbuch ist bewusst keine Zusammenfassung. Es zeigt die einzelnen Rechnungszeilen mit Originalbetrag, Kurs, CHF-Betrag, Lieferant, Standardkosten, Kostenbasis und Marge. Es ist das Werkzeug für die Nachrechnung einer einzelnen Zahl.

### Was man sieht

Eine breite Tabelle mit Status, Land, TSC, Jahr, Rechnung, Position, Kunde, Material, Originalbetrag, Originalwährung, Kurs nach CHF, Betrag CHF, Kursquelle, Lieferant, Typ, Standardkosten, Kostenbasis CHF, Marge CHF und Datenquelle. Jede Spalte hat einen Filter und lässt sich sortieren.

### Wie gerechnet wird

Der Betrag CHF ist der Originalbetrag mal Kurs. Die Kostenbasis ergibt sich aus der Menge mal den Standardkosten der zuständigen Quelle, die Marge aus Betrag minus Kostenbasis. Der Status folgt denselben Werten wie die Gruppenmarge: OK, Standardpreis fehlt, Lieferant unklar, Konzernkosten fehlen, Kostenwährung abweichend, Umsatz fehlt oder Kurs fehlt.

### Einsatz im Arbeitsalltag

**Revisor:** Einzelne Belege herausgreifen und Kurs, Kostenbasis und Marge nachrechnen.

**Controller:** Bei einer auffälligen Länder- oder Spartenmarge hier die Zeilen mit den Status „offen" suchen.

### Grenzen und Fallstricke

Gezeigt werden höchstens 1'000 Zeilen, die Kappung wirkt vor den Spaltenfiltern, und der Excel-Export enthält dieselben Zeilen. Vollständig ist nur das Nachweis-Excel der zentralen Datei. Das Prüfbuch rechnet CHF nach dem Kursprofil der Settings, während der Schalter „Group-Währung (CHF)" die Cockpit-Summen anders umrechnet. Die CHF-Margen können deshalb abweichen. Dieser Punkt ist offen.

## Gruppenmarge (Experten)

### Wozu

Die Gruppenmarge zeigt, was vom Umsatz nach Abzug der Standardkosten übrig bleibt, über alle Gesellschaften hinweg. Wichtig ist der Konzernblick: Wenn eine Gesellschaft Ware von der Trafag AG bezieht, zählen die Konzernkosten und nicht der interne Verrechnungspreis.

### Was man sieht

Sieben Kacheln: Umsatz, bekannte Kostenbasis, Gruppenmarge in Betrag und Prozent, interne Zeilen (mit lokal und extern), fehlende Kosten (mit „Lieferant unklar"), saubere Kostenbasis in Prozent und Deckungsbeitrag. Es folgen die Tabellen „Gruppenmarge nach Land" und „nach Land und Sparte" sowie die „Detailprüfung Kostenbasis" mit Status, Lieferant, Kostenquelle, Umsatz, Kostenbasis und Marge je Zeile.

### Wie gerechnet wird

Marge = Umsatz minus Kostenbasis, Marge in Prozent = Marge geteilt durch Umsatz. Die Kostenbasis ist Menge mal Standardkosten, gewählt in dieser Reihenfolge: Für Ware der Trafag AG die Konzern-Standardkosten aus SAP in CHF, für Ware der italienischen und indischen Gesellschaft deren Kosten aus den Belegen, für externe Lieferanten der lokale Standardpreis. Eine Zeile mit Sales Type „LRD" ohne Konzernkosten bleibt offen, weil der lokale Preis dort der interne Einkaufspreis wäre. Fehlende Werte werden markiert und nie geschätzt. Fehlt die Kostenbasis, steht die Marge auf „-". Weicht die Kostenwährung ab, wird zum aktuellen Tageskurs umgerechnet, solange der Schalter in den Settings auf „umrechnen" steht. Der Deckungsbeitrag ist vorbereitet, aber leer, weil keine Quelle fixe und variable Kosten trennt.

### Einsatz im Arbeitsalltag

**Controller:** Zuerst die Kachel „Saubere Kostenbasis" ansehen: Nur wenn sie hoch ist, ist die Marge belastbar. Danach über die Detailprüfung die offenen Zeilen nach Material und Lieferant abarbeiten.

**Einkäufer:** Die Lieferantenklassifikation (intern, lokal, extern) mit Finance klären, wenn viele Zeilen „Lieferant unklar" sind.

**Geschäftsleitung:** Die Marge nach Land und Sparte als Orientierung verwenden, nicht als Abschlusswert.

### Grenzen und Fallstricke

Die Gruppenmarge ist bis zur Fachfreigabe eine Prüfsicht und nicht der führende Abschlusswert. Die Detailtabelle zeigt höchstens 1'000 Zeilen, die Kacheln rechnen aber über den ganzen Filter. Bei Italien liegen die Kosten von Ware mit Trafag-Sachnummer über dem Schweizer Standardpreis, weil sie einen Verrechnungspreis enthalten; das ist so entschieden. Die Kostenkaskade endet nach der ersten internen Stufe.

## 3D Datenanalyse (Experten)

### Wozu

Die 3D-Ansicht stellt die Länderzahlen als drehbares Diagramm dar. Sie dient der Präsentation und dem schnellen Erkennen von Ausreissern, nicht der exakten Auswertung.

### Was man sieht

Auswahlfelder für den Indikator (zum Beispiel Ist-Umsatz mit und ohne IC, Intercompany-Wert und -Anteil, Menge, Gutschriften, Zeilenzahlen, Include- und Exclude-Quote, Sollwert) und die Grafikart (Balken, Linie, Fläche, Kreis, Sparten-Kreis je Land). Ein Schieberegler „Szenario-Faktor" von 0.5 bis 1.5 mit Knöpfen für minus und plus 10 Prozent, einer für die Beschriftungsgrösse und die 3D-Fläche selbst. Mit der linken Maustaste dreht man, mit dem Mausrad zoomt man, mit Shift und Ziehen oder der rechten Taste verschiebt man.

### Wie gerechnet wird

Die Werte stammen aus den Länder- und Jahressummen des Finance-Filters. Der Szenario-Faktor multipliziert die Werte für eine Was-wäre-wenn-Betrachtung, zum Beispiel einer Kursänderung, und zeigt Basis, Szenario und Delta. Auf Zeilenzahlen wirkt er nicht.

### Einsatz im Arbeitsalltag

**Geschäftsleitung:** In Besprechungen den Länderanteil als Kreis zeigen und mit dem Faktor die Wirkung von minus zehn Prozent eines Kurses oder Marktes veranschaulichen.

**Controller:** Zur Vorbereitung eher die Tabellen verwenden, die 3D-Sicht bietet keine Einzelwerte.

### Grenzen und Fallstricke

Der Faktor ist eine einfache Skalierung ohne Modell und keine Prognose. Eine Betrachtung über mehrere Währungen hat dieselben Einschränkungen wie die übrigen Summen.

## Rohdaten Diagnose (Experten)

### Wozu

Diese Seite ist eine technische Plausibilitätssicht auf die Rohdaten. Sie beantwortet, ob die Zahlen vor jeder fachlichen Regel überhaupt zusammenpassen. Für den verbindlichen Abgleich ist sie ausdrücklich nicht gedacht.

### Was man sieht

Zwei Blöcke. Oben kann eine vorhandene Excel-Datei gewählt, ein Summenfeld und eine Anzeigewährung bestimmt und ein „Cockpit" daraus erzeugt werden, mit Management-Aussagen, Top-Kunden, Top-Produktgruppen, Top-Sales-Ownern und Datenqualitätszählern. Darunter steht die „Zentrale Roh-Auswertung" auf der konfigurierten zentralen Datenquelle mit Filtern für Jahr, Land, TSC und Monat, einem Hauptsummenfeld, weiteren Summenfeldern und einer Anzeigewährung. Ergebnis sind Kacheln, Jahres-, Monats- und Tageswerte, Werte nach Quelle und nach Land sowie der Hinweis, wie viele Zeilen mangels Kurs nicht umgerechnet wurden.

### Wie gerechnet wird

Je Zeile wird der Quellwert mit dem Kurs zum Belegdatum in die Anzeigewährung umgerechnet. Wo kein Kurs vorhanden ist, bleibt die Zeile ohne Umrechnung und wird gezählt. Intercompany, Budget und Spartenlogik sind nicht enthalten.

### Einsatz im Arbeitsalltag

**Finance-Keyuser:** Nach einem Import die Summen je Monat und Tag ansehen und mit der Quelle vergleichen.

**Controller:** Mit zwei Summenfeldern nebeneinander prüfen, ob zum Beispiel Netto- und Bruttowerte plausibel auseinanderliegen.

### Grenzen und Fallstricke

Die Ergebnisse dürfen nicht für die Finance-Freigabe verwendet werden. Dafür gelten Soll/Ist Vergleich oder die Finance-Spalten im Excel.

## Daten-Heartbeat (Experten)

### Wozu

Der Daten-Heartbeat zeigt je Standort Tag für Tag, ob Buchungen eingegangen sind und ob der Exportlauf stattgefunden hat. Er macht das Ausbleiben von Daten sichtbar, bevor es in den Zahlen auffällt.

### Was man sieht

Je Standort eine Karte mit Ampelkennzeichen („OK", „Warn" oder „n Lücken"), dem letzten Update und dem letzten erfolgreichen Export. Das Diagramm zeigt die Zahl der Zeilen je Tag als Linie, bei eingeschaltetem „7-Tage-Summe" die gleitende Wochensumme. Unter der Linie stehen zwei Streifen: oben der Buchungsstatus je Tag, unten der Exportlauf je Tag (grün OK, rot Fehler, orange kein Lauf, hellgrau vor dem ersten Log). Wählbar ist das Fenster von 30, 60, 90 Tagen oder das laufende Jahr.

### Wie gerechnet wird

Ein Tag mit Buchungen ist OK. Ein Tag ohne Buchungen ist am Wochenende neutral. Unter der Woche wird er zur Lücke, wenn er nach dem letzten Datentag liegt und das letzte Update des Standorts mehr als zwei Tage zurückliegt. Er wird zur Warnung, wenn nach dem letzten Datentag für den Standort überhaupt kein Update-Zeitpunkt bekannt ist. Alle übrigen Tage ohne Buchungen gelten als neutral, damit gewöhnliche Feiertage oder ruhige Tage keinen Alarm auslösen. Der Gesamtstatus eines Standorts ist „Lücken", sobald ein Tag eine Lücke ist, sonst „Warn", sobald ein Tag eine Warnung ist. Der Exportstatus vergleicht je Tag die Protokolle der Läufe.

### Einsatz im Arbeitsalltag

**Finance-Keyuser:** Jeden Morgen kurz ansehen: Rote Streifen unten bedeuten fehlgeschlagene Exporte, orange Streifen fehlende Läufe.

**Controller:** Vor dem Monatsabschluss sicherstellen, dass kein Land am Monatsende eine Lücke hat.

### Grenzen und Fallstricke

Der Heartbeat prüft nur die Verkaufsstrecke; der Journal-Import wird bewusst nicht erfasst. Ein ruhiger Tag lässt sich vom Ausfall nicht trennen, solange das letzte Update jünger als zwei Tage ist. Vor dem ersten Eintrag im Export-Log ist der Exportstatus „unbekannt".

## Soll/Ist Vergleich

### Wozu

Der Soll/Ist Vergleich prüft die Net Sales Actuals der Länder gegen die gepflegten Referenzwerte und ist die Grundlage der Finance-Freigabe. Er beantwortet: Stimmt der Umsatz, den die Anwendung zeigt, mit dem Wert überein, den Finance als richtig kennt?

### Was man sieht

Oben stehen die Jahresauswahl (angeboten werden nur Jahre mit gepflegten Sollwerten), der Schalter „Ohne Ist" und die Schaltfläche „Aktualisieren". Die Tabelle zeigt je Land die Ampel, Ist, Referenz, Differenz, Währung, die Berechnung (welches Summenfeld verwendet wurde, mit Hinweis zur Länderregel) und die Zeilenzahl. Unter „Varianten" lassen sich je Land alternative Abgrenzungen aufklappen, mit Wert, Differenz, Intercompany-Anteil und Differenz ohne IC. Die bevorzugte Variante ist hervorgehoben.

### Wie gerechnet wird

Ist ist die Summe der Nettobeträge nach den Länderregeln für das Jahr. Das Jahr bestimmt das Buchungsdatum, sonst das Rechnungsdatum, sonst das Extraktionsdatum. Belegkopfwerte zählen je Beleg nur einmal. Differenz ist Ist minus Referenz. Die Ampel ist grün (OK), wenn die Differenz höchstens eine Währungseinheit beträgt, sonst gelb (Prüfen). Ohne berechenbare Differenz steht „Keine Daten". Länderspezifisch gilt zum Beispiel für Grossbritannien Sage-Netto in GBP mit negativen Gutschriften, für Italien der Ausschluss von Trafag Italia, für Deutschland der Ausschluss von Weiterberechnungen.

### Einsatz im Arbeitsalltag

**Finance-Leitung:** Die Abweichungen je Land bewerten und freigeben oder zurückgeben. Ziel ist ein Zustand, in dem Finance Summary, Finance Details und Soll/Ist zusammen plausibel sind.

**Controller:** Bei einer Abweichung die Varianten öffnen und prüfen, welche Abgrenzung das Soll trifft.

### Grenzen und Fallstricke

Sollwerte sind nur für einzelne Jahre vorhanden, für die Schweiz fehlen sie nach letztem Stand. Die Ampel kennt die Fälle ohne Sollwert nicht gesondert: Solche Länder erscheinen unter „Keine Daten". Die Toleranz von einer Einheit gilt für alle Währungen gleich.

## Controlling

### Wozu

Das Controlling beantwortet zwei Fragen: Woher kommt die Umsatzveränderung gegenüber dem Vorjahr, und wo landen wir dieses Jahr? Der Reiter wurde aus Eigeninitiative aufgebaut, nicht im Auftrag einer Fachabteilung. Er ersetzt weder Budget noch klassische Erfolgsrechnung.

### Was man sieht

Vier Kennzahlen oben: Veränderung in CHF für den aktuellen gegen den Vorjahreszeitraum, Hochrechnung des Jahres, Ist des Vorjahres und die Hochrechnung gegen das Vorjahr in Prozent. Dann die „Umsatzbrücke gegen Vorjahr" als Wasserfall: links der Vorjahresumsatz, dann die Wirkungen Menge, Mix, Preis, neue Artikel, weggefallene Artikel, Währung und Übrige, rechts der aktuelle Umsatz. Grün erhöht, rot senkt. Die Sicht lässt sich auf Gesamt, einzelne Gesellschaften oder Sparten umstellen. Eine Tabelle zeigt die Brücke je Gesellschaft. Es folgen die Hochrechnung je Gesellschaft (voller Balken Ist, schraffiert Restmonate, grauer Strich Vorjahr) und eine Monatskurve gegen das Vorjahr. Am Ende stehen die offenen Fragen. Ein Excel-Export liegt bei.

### Wie gerechnet wird

Die Basis sind dieselben Verkaufszeilen wie im Reiter Verkauf (Finance-Regeln, ohne Konzernkunden, CHF zum Belegdatum), je Gesellschaft in Lokalwährung gerechnet. Menge ist die Stückveränderung mal dem Durchschnittspreis des Vorjahres. Mix ist der Rest des Artikelmengeneffekts, also der Effekt, dass teurere oder günstigere Artikel verkauft wurden. Preis ist die Preisänderung je Artikel mal der aktuellen Menge. Neue und weggefallene Artikel sind Umsätze mit Artikeln, die nur in einem der beiden Zeiträume vorkommen. Währung ist der aktuelle Lokalumsatz mal die Kursveränderung. Übrige ist der Rest, etwa Leistungen ohne Stück, Gutschriften ohne Menge und Rundung. Die Summe aller Wirkungen ist genau die Veränderung. Die Hochrechnung nimmt den Ist-Umsatz bis zum letzten vollständigen Monat und ergänzt die Restmonate aus dem gleichen Monat des Vorjahres mal dem Wachstum seit Jahresbeginn, begrenzt auf den Bereich von 0.5 bis 2.

### Einsatz im Arbeitsalltag

**Controller:** Die Brücke in der Monatsberichterstattung verwenden, um zu erklären, ob ein Wachstum aus Menge, Preis oder Kurs stammt.

**Geschäftsleitung:** Die Hochrechnung als Orientierung dafür, wo das Jahr voraussichtlich endet. Der Vergleich der Gesellschaften zeigt, welche Einheit die Veränderung trägt.

**Vertriebsleiter:** Die Sicht je Sparte zeigt, in welchen Produktbereichen Preis oder Menge die Veränderung treiben.

### Grenzen und Fallstricke

Die Hochrechnung ist eine Rechnung und kein Budget; das Wachstum gilt für alle Restmonate gleich. Die Menge-Mix-Trennung ist grob, weil Stückzahlen über alle Artikel einer Gesellschaft summiert werden. Viele Umsätze laufen auf Artikelnummern, die nur in einem der beiden Zeiträume vorkommen, etwa Varianten. Dann sind „neue" und „weggefallene Artikel" gross, und die Aussagen zu Preis und Menge gelten nur für die Artikel, die in beiden Zeiträumen verkauft wurden. Nicht vorhanden sind Debitoren und Überfälligkeit, Vorräte und Lagerumschlag, Margenentwicklung, Erfolgsrechnung je Gesellschaft sowie Kostenstellen und Budget. Die Seite führt diese Punkte als offene Fragen auf.

## Finance Schulung

### Wozu

Die Finance Schulung ist die eingebaute Anwenderunterlage für Keyuser, Finance-Leitung und Administratoren. Sie erklärt den Ablauf vom manuellen Import bis zur zentralen Excel-Datei und das Lesen der Finance-Sichten.

### Was man sieht

Oben ein Vorschaubild des Cockpits, ein Rollenüberblick (Keyuser stellt Importe bereit und prüft Summen, Finance-Leitung gibt Soll/Ist frei, Admin konfiguriert) und der Hinweis, dass die Finance-Sicht verbindlich ist. Fünf Reiter: „Prozess" (Ziel der Finance-Sicht, Bedienreihenfolge, Prozessgrafik), „Prozessflow" (Datenfluss vom Import bis zur Freigabe, Bedeutung der Statusanzeigen, Entscheidungsregel, zentrale Dateien und SharePoint-Ablage), „Importe" (manuelle Importe und Delta-Regeln, Standortexport, zentrale Excel-Datei, Journal Import) und „Abgleich" (Finance Summary lesen, Details gegen Summary prüfen, Soll/Ist Vergleich, Länderlogik) sowie „Spartenanalyse".

### Wie gerechnet wird

Es wird nicht gerechnet. Die Schulung hält die fachlichen Regeln fest, zum Beispiel: Nach einer neuen Datei muss zuerst der Standort exportiert werden; ist „CSV neuer als DB" zu sehen, ist die Datei bereits neuer als die Datenbank; Spanien und Deutschland müssen vollständige Dateien liefern, weil ein Delta den bisherigen Stand ersetzen würde; Grossbritannien ist delta-fähig, weil Basis und Deltas zusammen gelesen werden.

### Einsatz im Arbeitsalltag

**Neue Keyuser:** Die Reihenfolge Datei bereitstellen, Standort exportieren, zentrale Datei erzeugen, Summen prüfen, Soll/Ist freigeben einüben.

**Revisor:** Die Statusanzeigen und die Tabelle der zentralen Dateien (Sales All, Nachweis-Excel, Audit-CSV) als Beschreibung des Prüfpfads nutzen.

### Grenzen und Fallstricke

Die Schulung ist Fliesstext der Seite und wird von Hand gepflegt. Bei Abweichungen zwischen Schulung und Oberfläche gilt die Oberfläche.

## Manuelle Importe

### Wozu

Für Länder ohne direkte Systemanbindung (Deutschland, Grossbritannien, Spanien) werden die Verkaufsdaten als Excel- oder CSV-Datei geliefert. Die Seite ist die Stelle, an der Keyuser diese Dateien hinterlegen und aktivieren. Technische Spaltenzuordnungen bleiben im Admin-Bereich unter Standorte.

### Was man sieht

Der Reiter „Importdateien" listet je manuellem Standort Land, TSC, den Schalter „Aktiv", Pfad oder SharePoint-Ordner, den letzten Upload und die Aktionen „Pfad prüfen", „Speichern" sowie die Dateiauswahl für einen Upload (xlsx oder csv). Der Reiter „Anleitung" zeigt die fünf Schritte: Excel bereitstellen, speichern und aktivieren, Standort exportieren, zentrale Excel erzeugen, Finance prüfen.

### Wie gerechnet wird

Es wird nicht gerechnet. Beim Standortexport wird die Datei oder der Ordner gelesen, nach den Zuordnungen und Regeln des Standorts aufbereitet und ersetzt den bisherigen Bestand des Standorts in der zentralen Datenbank.

### Einsatz im Arbeitsalltag

**Finance-Keyuser:** Die neue Jahres- oder Monatsdatei hochladen, den Pfad prüfen, speichern und anschliessend im Export Dashboard den Standort laden. Danach die zentrale Datei neu erzeugen.

**Controller:** Hier nachsehen, wann zuletzt eine Datei geliefert wurde, wenn Zahlen eines manuellen Landes stillstehen.

### Grenzen und Fallstricke

Spanien und Deutschland brauchen vollständige Dateien, eine einzelne Delta-Datei würde den Bestand verkürzen. Ein Upload allein verändert keine Finance-Zahl. Die Anleitung enthält den Hinweis, dass der Deutschland-Istwert fachlich noch zu bestätigen sei; ob dieser Hinweis noch aktuell ist, wurde nicht geprüft.

## Journal Import

### Wozu

Der Journal Import lädt die Buchungszeilen des Hauptbuchs je Gesellschaft in eine eigene Tabelle. Er ist die Grundlage für spätere Konsolidierung und buchhalterische Analysen wie Sachkonten oder Soll und Haben. Die Verkaufszahlen, die Finance Summary und der Soll/Ist-Abgleich verändern sich dadurch nicht.

### Was man sieht

Zwei Schaltflächen („Alle B1-Gesellschaften laden", „Status aktualisieren") und eine Tabelle mit Land, TSC, Schema, Quellsystem, Anzahl Journalzeilen, Buchungsdatum von und bis, letztem Load und der Aktion „Laden" je Gesellschaft.

### Wie gerechnet wird

Es wird nicht gerechnet. Ein Lauf ersetzt den gesamten Journalbestand der Gesellschaft. Liefert die Quelle keine Zeilen, bleibt der alte Bestand erhalten. Der Zeitraum folgt dem Datumsfilter der Export-Einstellungen, angewandt auf das Buchungsdatum. Quellen sind die SAP-Business-One-Gesellschaften Frankreich, Italien, USA und Indien sowie Schweiz und Österreich über SAP. Deutschland, Grossbritannien und Spanien sind nicht enthalten.

### Einsatz im Arbeitsalltag

**Controller:** Den Stand der Gesellschaften prüfen, bevor Auswertungen auf dem Hauptbuch aufgebaut werden: Wie weit reicht das Buchungsdatum, wann war der letzte Load?

**Finance-Keyuser:** Nach Änderungen im Quellsystem die betroffene Gesellschaft neu laden.

### Grenzen und Fallstricke

Der Import protokolliert in den App-Ereignissen unter „Journal" und nicht in den Export-Logs, der Daten-Heartbeat bleibt davon unberührt. Für Schweiz und Österreich funktioniert der Load nur, wenn die SAP-Seite das entsprechende Datenangebot bereitstellt; sonst meldet der Lauf einen Fehler. Das Konzernkonto-Mapping steht noch aus. Bereits sind je Buchungszeile Fälligkeitsdatum und Ausgleichsfelder vorbereitet; welche Bedeutung für „date paid" führend wird, ist offen.

## Marktsegmente

### Wozu

Die Seite ordnet Kunden Marktsegmenten zu, zum Beispiel Railway, und zeigt den Umsatz je Segment. Sie steht bewusst unter Finance und nicht im Admin-Bereich, weil die Zuordnung eine fachliche Aussage des Vertriebs ist. Sie beantwortet: Wie viel Umsatz machen wir im Bahnmarkt, und mit welchen Kunden?

### Was man sieht

Oben stehen die Jahresauswahl und der Excel-Export. Vier Reiter. „Ergebnis" zeigt den Pflegestand (bestätigte Kunden und Zeilen, offene Vorschläge, Gesamtbestand), den Umsatz je Segment, Jahr und Standort und darunter eine 3D-Analyse mit wählbarer Achse (Standort oder Segment), Wert (Umsatz, Verkaufszeilen, Kunden) und Währung. „Marktumfrage" ist die Pflege der Marktumfrage mit Suche und Filter. „Pflege" zeigt Kunden mit Zeilen, Umsatz, Zahl der Produktsparten und Segment samt Status und den Aktionen Bestätigen, Ändern oder Zuordnen und Entfernen. „Namensmuster" legt Muster fest, die Vorschläge erzeugen.

### Wie gerechnet wird

Im Ergebnis zählen nur bestätigte Zuordnungen, denn nur diese erscheinen auch im zentralen Excel. Der Umsatz steht in der jeweiligen Landeswährung, es wird weder über Währungen noch über Jahre addiert. Die Zuordnung hängt am Kunden und gilt für alle Jahre. Vorschläge entstehen aus dem Namensabgleich mit der Marktumfrage, aus Namensmustern und für Deutschland teilweise aus dem Branchenfeld des Kundenstamms. Ein Muster bestätigt nie und überschreibt keine bestätigte Zuordnung.

### Einsatz im Arbeitsalltag

**Vertriebsleiter:** Die grössten offenen Vorschläge zuerst bestätigen oder ändern, weil dort eine Zuordnung am meisten bewirkt. Die Pflegeliste ist nach Zahl der Verkaufszeilen sortiert.

**Geschäftsleitung:** Im Ergebnis den Umsatz je Segment, Jahr und Standort ablesen.

**Marktbeobachtung:** Die Marktumfrage beschreibt auch Interessenten ohne Umsatz und ist keine Umsatzquelle.

### Grenzen und Fallstricke

Kunden mit vier oder mehr Produktsparten sind orange markiert: Eine pauschale Segmentzuordnung überzeichnet bei ihnen den Umsatz. Die Namensmuster müssen genau genug sein, ein kurzes Muster wie „siemens" trifft auch andere Gesellschaften. Das Segment hängt am Kunden und nicht an der Rechnung. Die Segmentsicht wendet die spanische Jahresregel nicht an und lässt sich deshalb nicht in jedem Fall lückenlos mit Finance abstimmen. Für Deutschland hängt die Zuordnung an der Kundennummer aus Alphaplan; dort sind einige Zeilen, vor allem Gutschriften, ohne fachliche Kundennummer.

## Administration

### Wozu

Der Admin-Bereich bündelt die technischen und organisatorischen Grundlagen der ganzen Anwendung. Er richtet sich an Administratoren, nicht an Fachanwender. Die Seiten Standorte, Transformationen, Finance Regeln, Settings und Menüstruktur sind auf Administratoren beschränkt.

### Was man sieht

**Aktive Logins** zeigt die App-internen Entsperrungen von HR und Finance seit dem letzten Start mit Bereich, Name, IP-Adresse, Zeitpunkt der Entsperrung und letztem Zugriff. Wichtig ist der Hinweis, dass HR und Finance gemeinsame Logins verwenden, die Seite also den Login-Namen zeigt und nicht zwingend die Person. Dort lässt sich auch eine Animation auf der Startseite ein- und ausschalten.

**Standorte** pflegt die Gesellschaften: Land, TSC, Schema, Quellsystem, Aktivierung, die Zuordnung der Quellfelder zum zentralen Schema, SAP-Joins, Excel-Spaltenmapping für manuelle Länder und die zentrale HANA-Technik.

**Transformationen** ordnet Quellfelder Zielfeldern zu und wendet je Standort Umformungen an, etwa Währungsumrechnung oder Normalisierung. Es gibt feldweise Regeln und Regeln, die mehrere Felder eines Datensatzes verwenden.

**Finance Regeln** bestimmen, welche Zeilen in die Finance-Sicht eingehen, zum Beispiel Weiterberechnungen, interne Kunden oder besondere Belege. Sie wirken nur auf die Finance-Sicht in der zentralen Excel und im Abgleich. Rohdaten und Spaltenzuordnung bleiben unverändert. Hier wird auch die Liste der IC- und 2nd-party-Kunden gepflegt.

**Settings** enthalten den Export und Import der Konfiguration, die SharePoint-Anbindung, die Quellsysteme, die Wechselkurse (inklusive Aktualisierung der EZB-Kurse), den Zeitplan des automatischen Laufs, den Datumsfilter, die Wahl des Kursdatums und die Finance-Schalter. Zu den Finance-Schaltern gehören die Behandlung abweichender Kostenwährung (Standard: umrechnen), der Lieferanten-Fallback, die Kostenquelle bei internem Lieferanten, die Kostenbasis von Italien und Indien (jüngster Belegpreis oder Durchschnitt), das CHF-Kursprofil (aktueller Tageskurs oder Jahresendkurs), die Herstellerregel für die Schweiz sowie die Audit-CSV je Standort. Die Hilfetexte nennen jeweils Wirkzeitpunkt und Abhängigkeiten.

**Menüstruktur** erlaubt, Menüpunkte zu verschieben, umzubenennen, zu sortieren und auszublenden. Die Zielseiten bleiben unverändert.

**Logs** zeigen die Export-Logs je Land mit Filter nach Land, Status und Datum sowie die technischen Logs. Alte Logs lassen sich nach Aufbewahrungsfrist löschen.

### Wie gerechnet wird

Hier wird nicht gerechnet. Die Einstellungen beeinflussen aber die Rechnung der übrigen Seiten, besonders die Finance-Schalter, das Kursprofil, die Finance Regeln und die Transformationen.

### Einsatz im Arbeitsalltag

**Administrator:** Neue Standorte und Quellsysteme anlegen, Schalter ändern und Konfigurationen zwischen Testumgebung und Produktion übertragen, wobei der Export ohne Secrets Passwörter leer lässt. Nach einer Änderung der Kostenschalter greift die Wirkung erst beim nächsten Import oder bei der Neuberechnung.

**Finance-Leitung:** Änderungen an Finance Regeln und Kursprofil nur mit fachlicher Freigabe, weil sie die Zahlen aller Sichten verändern.

**Finance-Keyuser:** In den Logs den Grund eines fehlgeschlagenen Exports nachlesen.

### Grenzen und Fallstricke

Die Finance-Schalter verändern Ergebnisse rückwirkend für alle Sichten. Das Kursprofil und die Schalter für die Kostenbasis sollten nicht ohne Abstimmung geändert werden. Wechselkurse sind im Speicher zwischengehalten; Änderungen wirken nach dem Speichern. Die Seite Aktive Logins zeigt nur den Stand seit dem letzten Start der Anwendung.

## Kennzahlen-Glossar

| Kennzahl | Bedeutung | Berechnung in Worten |
|---|---|---|
| Net Sales Actual | Nettoumsatz nach Finance-Regeln, in der Hauswährung des Landes | Summe der Nettofakturawerte aller eingeschlossenen Zeilen des Jahres; Gutschriften negativ |
| Enthaltene Zeilen | Zeilen, die in die Summe eingehen | Zeilen, die keine Finance-Regel ausschliesst und deren Wert nicht null ist |
| Ausgeschlossene Zeilen | Zeilen ohne Wirkung auf die Summe | Zeilen, die eine Regel ausschliesst, plus Zeilen mit Wert null |
| Soll (Referenz) | Gepflegter Vergleichswert je Land und Jahr | Von Finance hinterlegter Wert; nicht für jedes Land und Jahr vorhanden |
| Differenz | Abstand zwischen Ist und Soll | Ist minus Soll |
| Status OK / Prüfen / Kein Sollwert / Keine Daten | Ergebnis des Soll/Ist-Abgleichs je Land | OK bei Differenz bis eine Währungseinheit, Prüfen darüber, Kein Sollwert ohne Referenz, Keine Daten ohne Ist-Zeile |
| Nicht geprüft | Länder ohne Prüfung | Anzahl der Länder ohne Sollwert oder ohne Daten |
| IC / 2nd-party | Umsatz mit Konzern- und nahestehenden Firmen | Umsatz der Kunden, die einer gepflegten Liste entsprechen |
| Ist ohne IC | Umsatz ohne Konzernanteil | Ist minus IC |
| Group-Währung (CHF) | Umrechnung der Cockpit-Summen nach CHF | Betrag mal Kurs; nach der Dokumentation fix zum 31.12. des Belegjahres |
| Abdeckung (Sparten) | Anteil des Umsatzes mit gültiger Sparte | Zugeordneter plus Übrige-Umsatz geteilt durch Gesamtumsatz |
| Trefferquote (Materialien) | Anteil der Materialien mit gültiger Sparte | Zugeordnete plus Übrige-Materialien geteilt durch Prüfzeilen |
| Kostenbasis | Kosten der verkauften Menge | Menge mal Standardkosten der zuständigen Kostenquelle |
| Gruppenmarge | Marge nach Abzug der Konzernkosten | Umsatz minus Kostenbasis; Prozent ist Marge geteilt durch Umsatz |
| Saubere Kostenbasis | Anteil der Zeilen mit belastbarer Kostenbasis | Zeilen mit Status OK geteilt durch alle Zeilen |
| Deckungsbeitrag (DB) | Marge nach variablen Kosten | Vorbereitet, aber leer, weil keine Quelle fixe und variable Kosten trennt |
| Umsatzbrücke | Zerlegung der Umsatzveränderung gegen das Vorjahr | Menge, Mix, Preis, neue und weggefallene Artikel, Währung und Übrige ergeben zusammen genau die Veränderung |
| Hochrechnung | Erwartetes Jahresergebnis | Ist bis letzter vollständiger Monat plus Restmonate des Vorjahres mal Wachstum seit Jahresbeginn (0.5 bis 2) |
| Heartbeat-Lücke | Ausbleibende Buchungen an einem Werktag | Tag ohne Zeilen nach dem letzten Datentag, wenn das letzte Update über zwei Tage zurückliegt |
| Datenstand | Alter der Daten eines Standorts | Jüngster Zeitpunkt aus Datenbank, Dateien und Export-Log, mit Angabe der Quelle |
