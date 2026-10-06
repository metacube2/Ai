# Netzwerk und IT-Infrastruktur

Das Modul Netzwerk beantwortet aus Sicht von Geschäftsleitung und IT-Leitung drei Fragen: Sind die Systeme, von denen das Cockpit lebt, erreichbar (Verfügbarkeit und Risiko)? Welche Geräte im Unternehmen sind veraltet oder verlieren bald den Herstellersupport (Lebenszyklus und Kosten der Migration)? Und wo gibt es sicherheitsrelevante Auffälligkeiten? Es liegt im Menü unter **Netzwerk** mit zwölf Unterseiten und ist für alle sichtbar. Hauptnutzer sind IT-Leitung, Netzwerktechniker und Geschäftsleitung.

Die Daten werden nur lesend erhoben. Der Server prüft alle fünf Minuten eine feste Liste bekannter Ziele (SAP, HANA-Datenbanken, SharePoint, Microsoft 365, EZB und weitere), ohne Anmeldedaten und nur im Produktivbetrieb. Aus dem Active Directory werden ausschliesslich Computerkonten gelesen, keine Personen, keine Passwörter und keine Schlüssel. Die AD-Auswertung ist freigegeben und eingeschaltet; ist sie ausgeschaltet, zeigen die betroffenen Seiten einen entsprechenden Hinweis. Der Verlauf beginnt erst mit dem Start der Prüfung; Kurven über die Zeit erscheinen ab dem zweiten Tagesschnappschuss. Nicht möglich und bewusst nicht gebaut sind Firewall-Logs („wer spricht mit wem“), DHCP-Geräte ohne AD-Konto und Konten von Ausgetretenen; dafür bräuchte es die Freigabe von IT und Datenschutz.

Für die Geschäftsleitung am wichtigsten sind die Seiten Migration, Bericht, Verfügbarkeit und Netz oder Daten. Die übrigen Seiten sind kurz beschrieben und richten sich an die IT.

## Übersicht

### Wozu

Die Seite zeigt in einem Bild, ob die für das Cockpit wichtigen Systeme gerade erreichbar sind und wie schnell sie antworten. Sie ist der Einstieg, um bei einer Störungsmeldung in Sekunden zu sehen, ob das Netz oder eine Quelle betroffen ist.

### Was man sieht

Oben vier Kennzahlen: Anzahl erreichbarer Ziele, mittlere Antwortzeit der letzten 24 Stunden, Zeitpunkt der letzten Prüfung und Beginn des Verlaufs. Darunter eine Netzkarte mit dem Cockpit-Server in der Mitte und den Zielen im Kreis. Grüne Linien bedeuten erreichbar, gelbe langsam (Antwort über 500 Millisekunden), rot gestrichelte nicht erreichbar. Bewegte Punkte laufen, solange ein Ziel antwortet. Rechts steht die Zielliste mit Verlaufslinie der Antwortzeit und Verfügbarkeit der letzten 24 Stunden in Prozent. Die Seite liest jede Minute neu.

### Wie gerechnet wird

Die Verfügbarkeit ist der Anteil erfolgreicher Prüfungen an allen Prüfungen der letzten 24 Stunden. Die mittlere Antwortzeit ist der Durchschnitt der Antwortzeiten aller Ziele.

### Einsatz im Arbeitsalltag

- **IT-Leitung:** Bei einer Meldung „SAP ist langsam“ zuerst die Karte öffnen.
- **Geschäftsleitung:** Ein Blick genügt, um zu sehen, ob alle Ziele grün sind.

### Grenzen und Fallstricke

SAP wird nur über einen öffentlichen Ping ohne Anmeldung geprüft, HANA nur über den Verbindungsaufbau auf dem Datenbankport. Erreichbar heisst nicht, dass Anmeldung und Daten funktionieren.

## Verfügbarkeit

### Wozu

Die Seite zeigt, wie zuverlässig die Systeme über die Woche waren, und warnt, wenn SAP langsam wird, bevor es ausfällt. Sie ist die Grundlage für Gespräche über Servicequalität und für die Frage, wie gross das Ausfallrisiko ist.

### Was man sieht

Eine Heatmap der letzten 7 Tage: je Ziel eine Reihe, jedes Feld eine Stunde. Grün bedeutet alle Prüfungen erfolgreich, rot keine, gelb teilweise, grau noch nicht geprüft. Rechts steht die Verfügbarkeit der letzten 24 Stunden. Darunter die SAP-Frühwarnung der letzten 30 Tage: eine Linie für die Antwortzeit des SAP-Pings je Tag und Balken für Warnungen und Fehler der SAP-Abrufe des Cockpits (Einkauf, HR, Logistik, Journal).

### Wie gerechnet wird

Je Feld der Anteil erfolgreicher Prüfungen in dieser Stunde. Die Frühwarnung stellt Antwortzeit und Anzahl Probleme der SAP-Abrufe je Tag nebeneinander. Steigen beide, wird SAP langsam, bevor es steht.

### Einsatz im Arbeitsalltag

- **IT-Leitung:** Rote Streifen zur gleichen Uhrzeit an mehreren Tagen deuten auf geplante Wartungen oder wiederkehrende Probleme hin.
- **Geschäftsleitung:** Als Beleg gegenüber einem Dienstleister oder für Service-Level-Gespräche.
- **Controller und Einkauf:** Bei auffällig vielen SAP-Fehlern prüfen, ob Exportzahlen vorübergehend unvollständig sein können.

### Grenzen und Fallstricke

Die Prüfung läuft alle fünf Minuten; kürzere Ausfälle werden nicht erfasst. Aussagekräftig ist die Seite erst nach einigen Tagen Laufzeit. Die Verfügbarkeit gilt für die Sicht des Cockpit-Servers, nicht für alle Arbeitsplätze.

## Netz oder Daten

### Wozu

Wenn ein Datenexport eines Standorts fehlschlägt, soll die Seite zeigen, ob das Netz schuld ist oder die Daten beziehungsweise die Quelle. Sie ordnet ausserdem die Standorte nach ihrer Bedeutung für das Geschäft, damit klar wird, wo ein Ausfall am meisten kostet.

### Was man sieht

Kennzahlen: Anzahl Exportläufe seit Juni, davon fehlgeschlagen, davon Ursache Netz und Ursache Daten oder Quelle. Eine Zeitachse zeigt je Standort jeden Lauf als Punkt. Darunter die Liste der Standorte nach Kritikalität mit Umsatz 2025 in CHF, Abhängigkeit, Exportfehlern und Verfügbarkeit des Ziels, sowie die letzten Fehler im Wortlaut.

### Wie gerechnet wird

Ein Fehler gilt als „Netz“, wenn die Fehlermeldung auf Verbindung, SSL oder Zeitüberschreitung hindeutet oder das Ziel rund 15 Minuten vor oder nach dem Lauf nicht erreichbar war. Alles andere gilt als „Daten oder Quelle“. Die Kritikalität ist nach dem Umsatz 2025 des Standorts in CHF sortiert, umgerechnet zum Kurs vom 31.12.2025.

### Einsatz im Arbeitsalltag

- **Controller:** Fehlt die Zahl eines Standorts im Konzernbericht, sieht man, ob erneut exportiert werden muss oder die Quelle repariert werden muss.
- **IT-Leitung:** Wiederkehrende Netzfehler bei einem umsatzstarken Standort rechtfertigen eine bessere Anbindung.
- **Geschäftsleitung:** Zeigt, welche Standorte technisch am verwundbarsten und wirtschaftlich am wichtigsten sind.

### Grenzen und Fallstricke

Die Einordnung ist eine Faustregel nach Meldungstext und Zeitnähe, kein Beweis. Erreichbarkeitsprüfungen gibt es erst seit dem Start der Netzwerkprüfung; ältere Läufe werden nur nach dem Meldungstext eingeordnet. Standorte ohne Kurs für die Umrechnung werden lieber weggelassen als falsch gerechnet.

## Sicherheit

### Wozu

Die Seite macht Sicherheitsrisiken sichtbar, die ohne Fachwissen verständlich sind: ablaufende Zertifikate, unverschlüsselte Verbindungen und veraltete oder verwaiste Computerkonten.

### Was man sieht

Zertifikate als Balken mit Restlaufzeit (rot unter 30, gelb unter 90 Tagen), eine Tabelle zu DNS-Auflösung und Verschlüsselung je Ziel mit einer Warnung, wenn das SAP-Gateway über HTTP ohne Verschlüsselung angesprochen wird, sowie Kennzahlen zu den AD-Computerkonten (Anzahl, deaktiviert, aktiv aber seit 90 Tagen ohne Anmeldung). Ein Hinweis nennt, was noch nicht möglich ist.

### Wie gerechnet wird

Die Restlaufzeit wird aus dem Zertifikat beim letzten Verbindungsaufbau gelesen. Die Warnung zum SAP-Gateway beruht darauf, dass die Verbindung ohne Verschlüsselung aufgebaut wird; Benutzer und Passwort der Abrufe laufen dann unverschlüsselt durch das interne Netz.

### Einsatz im Arbeitsalltag

- **IT-Leitung:** Zertifikate rechtzeitig erneuern lassen, die Warnung zum SAP-Gateway in die Planung aufnehmen.
- **Geschäftsleitung:** Als Risikoübersicht für Audits.

### Grenzen und Fallstricke

HANA und eigene Ziele werden nur per Port geprüft; ob dort verschlüsselt wird, sieht die Prüfung nicht.

## Active Directory

### Wozu

Das Active Directory ist das zentrale Verzeichnis, in dem alle Computer der Domäne geführt werden. Die Seite wertet nur die Computerkonten aus, keine Personen. Sie zeigt, wie viele Geräte es gibt, wie alt ihre Betriebssysteme sind und wo aufgeräumt werden sollte.

### Was man sieht

Mehrere innere Reiter: eine 3D-Übersicht (je Organisationseinheit ein Turm, Höhe gleich Anzahl Geräte, farbig nach Zustand), Lebenszyklus der Betriebssysteme mit Datum des Supportendes, Aufräumen (aktive Konten ohne Anmeldung seit 90 oder 180 Tagen, nie angemeldete, lange deaktivierte), Sicherheit (Abdeckung der Passwortverwaltung für lokale Administratoren, Verschlüsselung, riskante Delegation), Dienste, Struktur und Subnetze, DNS und Domänencontroller sowie ein Altlasten-Score. Jede Liste lässt sich nach Excel exportieren.

### Wie gerechnet wird

Der Altlasten-Score vergibt Punkte je Computer (zum Beispiel Delegation 5, ohne Herstellersupport 3, Support endet in 180 Tagen 2, inaktiv 2, nie angemeldet 1) und summiert sie je Organisationseinheit. Als „aktiv“ gilt ein Konto mit Anmeldung in den letzten 44 Tagen. Wo Daten nicht lesbar sind, steht „nicht lesbar, IT klären“ statt einer Zahl.

### Einsatz im Arbeitsalltag

- **IT-Leitung:** Die Organisationseinheiten mit den höchsten Altlasten zuerst bereinigen.
- **Geschäftsleitung:** Der Lebenszyklus zeigt, wie viele Geräte ohne Herstellersupport laufen.

### Grenzen und Fallstricke

Die Supportdaten stammen aus dem Microsoft-Lifecycle (Stand Oktober 2026) und müssen bei neuen Versionen im Programm nachgetragen werden. Hinweis: nicht geprüft, ob das Serverkonto alle Attribute lesen darf; nicht lesbare Teile werden als solche gezeigt.

## AD-Infrastruktur

### Wozu

Die Seite zeigt den Zustand der Verzeichnisinfrastruktur selbst: Domänencontroller, ihre Synchronisation, Standorte, Domänenebene und Zertifizierungsstellen. Ein Ausfall hier würde Anmeldungen im ganzen Unternehmen betreffen.

### Was man sieht

Ein Replikationsnetz der Domänencontroller (gelb ab 24 Stunden ohne Abgleich, rot bei Fehler), Standorte mit Subnetzen und Verbindungen, Kennzahlen der Domäne (zum Beispiel Alter des Passworts des Schlüsselkontos), Zertifizierungsstellen sowie die Gerätelandkarte mit einer Blase je Standort nach Zahl aktiver Computer.

### Einsatz im Arbeitsalltag

- **IT-Leitung:** Prüfen, ob alle Domänencontroller abgleichen und ob Standorte ohne Domänencontroller auskommen.
- **Geschäftsleitung:** Die Gerätelandkarte zeigt, wo die IT-Last sitzt.

## Gruppenrichtlinien

### Wozu

Gruppenrichtlinien steuern zentral Einstellungen auf den Computern. Die Seite zeigt, welche Richtlinien wo wirken und welche unnötig sind.

### Was man sieht

Eine Sonnenstrahl-Grafik der Organisationsstruktur (Breite gleich Anzahl Computer, rot bei gesperrter Vererbung) und eine Liste auffälliger Richtlinien: nicht verknüpft, ohne Einstellungen, ganz ausgeschaltet oder mit unterschiedlicher Version im Verzeichnis und im Freigabeordner.

### Einsatz im Arbeitsalltag

- **IT-Leitung:** Nicht verknüpfte Richtlinien aufräumen und Versionskonflikte beheben.
- **Geschäftsleitung:** Wenig Relevanz, ausser bei Revisionsfragen.

## DNS

### Wozu

DNS übersetzt Rechnernamen in Adressen. Veraltete Einträge führen zu Fehlverbindungen. Die Seite zeigt, wie gepflegt die Namensauflösung ist.

### Was man sieht

Die im Verzeichnis geführten Zonen, ob die automatische Alterung eingeschaltet ist, Anzahl statischer und dynamischer Einträge, veraltete Einträge und Adressen mit mehreren Namen.

### Einsatz im Arbeitsalltag

- **IT-Leitung:** Bei Verbindungsproblemen im Notebook-Netz auf doppelte Adressen prüfen und die Bereinigung veralteter Einträge planen.

## Verlauf

### Wozu

Die Seite zeigt die Entwicklung über die Zeit: Was ändert sich im Gerätebestand, und wann läuft welcher Support aus?

### Was man sieht

Countdown-Ringe für Produkte, deren Herstellersupport in den nächsten 12 Monaten endet, mit der Zahl aktiver Geräte. Dazu Kennzahlen je Tag als Kurven und eine Liste der Änderungen an Computerkonten der letzten 90 Tage (neu, entfernt, verschoben, deaktiviert, Betriebssystem geändert).

### Wie gerechnet wird

Einmal täglich wird ein Schnappschuss der Kennzahlen gespeichert; die Änderungen ergeben sich aus dem Vergleich der Schnappschüsse. Kurven erscheinen ab dem zweiten Tag.

### Einsatz im Arbeitsalltag

- **IT-Leitung:** Den Countdown als Terminliste für Ersatzbeschaffungen verwenden und Veränderungen im Bestand nachvollziehen.
- **Geschäftsleitung:** Frühwarnung für Investitionen, bevor der Support ausläuft.

## Migration

### Wozu

Die Seite ist der Migrationsplaner. Sie zeigt, welche aktiven Geräte ein Betriebssystem nutzen, dessen Herstellersupport abgelaufen ist oder in den nächsten zwei Jahren endet, und wie weit die Ablösung schon fortgeschritten ist. Damit liefert sie die Mengengrundlage für Budget, Beschaffung und Projektplanung der Migration. Sicherheitsrisiko und Kosten hängen direkt an diesen Zahlen: Geräte ohne Support erhalten keine Sicherheitsupdates mehr.

### Was man sieht

Fünf Kennzahlen: Geräte zu migrieren (zwei Jahre), Support schon abgelaufen, endet in 30 Tagen, endet in 180 Tagen und Anzahl Wellen. Darunter eine Zeitachse von 12 Monaten zurück bis 24 Monate voraus mit einer Heute-Linie. Jede Blase ist eine Welle, also ein Produkt mit einem Supportende; die Grösse zeigt die Zahl aktiver Geräte. Links der Heute-Linie ist der Support bereits vorbei. Links in der Liste steht je Welle das Produkt, das Datum, die Restdauer oder die Tage seit Ablauf und der Fortschritt. Ein Klick zeigt die Verteilung auf die Organisationseinheiten und die einzelnen Geräte. Ohne Auswahl zeigt die rechte Seite die Wochenkurven je Welle. Eine Excel-Ausgabe der Wellen und Geräte ist vorhanden.

### Wie gerechnet wird

Eine Welle sind alle aktiven Geräte desselben Produkts mit demselben Supportende. Als aktiv gilt ein aktiviertes Konto, das nicht seit über 90 Tagen ohne Anmeldung ist. In die Wellen kommen Produkte, deren Supportende bereits vorbei ist oder in den nächsten 730 Tagen liegt. Der Fortschritt einer Welle ist der Rückgang der Geräte gegenüber dem ersten gespeicherten Tagesschnappschuss: Geräte, die seither verschwunden sind, gelten als migriert, ersetzt oder deaktiviert. Die Supportdaten stammen aus dem Microsoft-Lifecycle.

### Einsatz im Arbeitsalltag

- **IT-Leitung:** Wellen als Projektplan verwenden: zuerst abgelaufene und bald endende Produkte, die grössten Wellen mit den meisten Geräten früh einplanen.
- **Geschäftsleitung und Controller:** Die Zahl der Geräte je Welle multipliziert mit den Kosten pro Ersatz oder Upgrade ergibt das Budget der Migration. Die Seite liefert die Mengen, die Stückkosten kommen nicht aus dem Cockpit.
- **Risikoverantwortliche:** Geräte ohne Support in Produktionsnetzen als Risiko in die Risikoliste aufnehmen.

### Grenzen und Fallstricke

- Es werden nur Geräte erfasst, die im Verzeichnis geführt sind und sich angemeldet haben; nicht verbundene Geräte und Geräte ohne Konto fehlen.
- Der Fortschritt vergleicht mit dem ersten Schnappschuss; ein verschwundenes Gerät kann auch deaktiviert statt migriert sein.
- Sonderfälle wie erweiterte Sicherheitsupdates oder Langzeitversionen hängen an der Pflege der Supportdaten im Programm.
- Preise und Aufwand sind nicht enthalten.

## Bericht

### Wozu

Der Bericht fasst Netzwerk und Active Directory auf einer Seite in Managementhöhe zusammen, als Wochenbericht mit Kalenderwoche. Er eignet sich für das Wochenmeeting, für Vorgesetzte oder die Dokumentation.

### Was man sieht

Ein Knopf „Als PDF drucken“ öffnet den Druckdialog des Browsers (im Querformat), dort wählt man „Als PDF speichern“. Der Bericht enthält: Kennzahlen heute gegen vor sieben Tagen mit Veränderung, Supportende innerhalb von 90 Tagen, Zustand des Active Directory (Domänencontroller, Replikationsfehler, Gruppenrichtlinien, DNS), die fünf Organisationseinheiten mit dem grössten Aufräumbedarf, Änderungen an Computerkonten der Woche, Erreichbarkeit der Ziele in 24 Stunden und Zertifikate mit weniger als 90 Tagen Restlaufzeit.

### Wie gerechnet wird

Die Kennzahlen stammen aus den Tagesschnappschüssen; die Veränderung ist der heutige Wert minus der Wert vor sieben Tagen (ohne Vergleich, solange es noch keinen alten Schnappschuss gibt). Die Aufräum-Rangliste nutzt den Altlasten-Score. Der Bericht wird nicht versendet.

### Einsatz im Arbeitsalltag

- **IT-Leitung:** Jede Woche als PDF ablegen; ein Bericht nach dem anderen zeigt, ob der Bestand besser oder schlechter wird.
- **Geschäftsleitung:** Ein Blick auf die Veränderungen und das nahe Supportende genügt.

### Grenzen und Fallstricke

Beim Öffnen werden die Domänencontroller abgefragt; das kann bis zu 15 Sekunden dauern. Ist die AD-Auswertung ausgeschaltet oder nicht lesbar, fehlt der Bericht.

## Arbeitsplätze

### Wozu

Die Seite macht schwaches WLAN und Verbindungsprobleme an Arbeitsplätzen sichtbar und lässt eigene Geräte wie Rückmeldeterminals überwachen.

### Was man sieht

Verbindungsabbrüche der Browser je Netzbereich (letzte 7 Tage) und nach Tageszeit, eine Tabelle eigener Prüfziele mit optionalem Arbeitsplatz und daneben die letzte Rückmeldung aus Logistik live sowie die im Verzeichnis veröffentlichten Drucker.

### Wie gerechnet wird

Gezählt wird, wie oft die Browser-Verbindung zum Cockpit abreisst und sich innerhalb von fünf Minuten wieder aufbaut; ein geschlossener Tab zählt nicht. Viele Abbrüche je Sitzung in einem Bereich deuten auf schwaches WLAN. Gespeichert wird nur der Netzbereich, keine Person und keine volle Adresse.

### Einsatz im Arbeitsalltag

- **IT-Leitung:** Bereiche mit vielen Abbrüchen für WLAN-Verbesserungen priorisieren.
- **Logistik:** Ist ein Terminal erreichbar, es kommt aber keine Rückmeldung, liegt eher ein Stillstand vor; ist das Terminal nicht erreichbar, eher ein Netzproblem.

### Grenzen und Fallstricke

Die Zuordnung Terminal zu Arbeitsplatz ist eine eigene, von Hand geführte Liste; eine vollständige Liste der IT liegt nicht vor.

## Kennzahlen-Glossar

| Kennzahl | Bedeutung | Berechnung in Worten |
|---|---|---|
| Ziele erreichbar | Anzahl der geprüften Ziele, die zuletzt geantwortet haben | Ziele mit erfolgreicher letzter Prüfung geteilt durch alle Ziele |
| Verfügbarkeit 24 Stunden | Zuverlässigkeit eines Ziels | Erfolgreiche Prüfungen geteilt durch alle Prüfungen in 24 Stunden |
| Mittlere Antwortzeit | Geschwindigkeit der Ziele | Durchschnitt der gemessenen Antwortzeiten in Millisekunden; über 500 gilt als langsam |
| Ursache Netz | Exportfehler wegen Verbindung | Meldung deutet auf Verbindung, SSL oder Zeitüberschreitung, oder Ziel zur selben Zeit nicht erreichbar |
| Ursache Daten oder Quelle | Exportfehler ohne Netzbezug | Alle übrigen Fehler |
| Restlaufzeit Zertifikat | Tage bis zum Ablauf | Ablaufdatum minus heute; rot unter 30, gelb unter 90 Tagen |
| Aktiv | Gerät hat sich kürzlich angemeldet | Aktiviertes Konto mit Anmeldung in den letzten 44 Tagen |
| Ohne Herstellersupport | Gerät mit abgelaufenem Support | Aktives Gerät, dessen Supportende laut Microsoft-Lifecycle vorbei ist |
| Welle | Gruppe gleichartiger Geräte für die Migration | Aktive Geräte desselben Produkts mit demselben Supportende, Ende vorbei oder innerhalb von zwei Jahren |
| Fortschritt Welle | Wie weit die Ablösung ist | Rückgang der Geräteanzahl gegenüber dem ersten Tagesschnappschuss, in Prozent |
| Altlasten-Score | Aufräumbedarf je Organisationseinheit | Punkte je Computer (Delegation 5, ohne Support 3, Support endet in 180 Tagen 2, inaktiv 2, Passwort alt 2, nie angemeldet 1, lange deaktiviert 1), je Einheit summiert |
| Abbrüche je Sitzung | Verbindungsqualität eines Netzbereichs | Anzahl Verbindungsabbrüche geteilt durch Anzahl Sitzungen im Netzbereich |
