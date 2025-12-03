# Beispiel-Mails für Training

Diese Beispiel-Mails können zum Testen des Mail-Imports verwendet werden.

## Enthaltene Beispiele

1. **test1.txt** - Projekt-Update
   - Typ: Status-Update
   - Empfohlen für: "Zusammenfassen"

2. **test2.txt** - Kundenanfrage
   - Typ: Support-Anfrage
   - Empfohlen für: "Antwort schreiben"

3. **test3.txt** - Meeting Notes
   - Typ: Meeting-Protokoll
   - Empfohlen für: "Action Items"

4. **test4.txt** - Out of Office
   - Typ: Automatische Antwort
   - Empfohlen für: "Kategorisieren" (als "Automatisch" oder "Skip")

## Verwendung

1. Wähle eine oder mehrere Dateien aus
2. Ziehe sie per Drag & Drop in die App
3. Die Mails werden automatisch geparst und bereinigt
4. Gehe zum Labeling und füge die erwarteten Outputs hinzu

## Beispiel-Labels

### test1.txt (Zusammenfassen)
```
Alex berichtet über erfolgreichen Abschluss der API-Integration mit 40% Performance-Verbesserung.
Nächste Woche starten Frontend-Anpassungen durch Maria und Tom.
Go-Live bleibt für Ende März geplant.
```

### test2.txt (Antwort schreiben)
```
Sehr geehrter Herr Schmidt,

vielen Dank für Ihre Anfrage zu Rechnung #2847.

Sie haben recht - hier ist uns ein Fehler unterlaufen. Der korrekte Betrag
laut Angebot beträgt 1.250€. Wir werden die Rechnung korrigieren und Ihnen
die berichtigte Version bis morgen zusenden.

Wir entschuldigen uns für die Unannehmlichkeiten.

Mit freundlichen Grüßen
Support-Team
```

### test3.txt (Action Items)
```
- Sarah: Pressemitteilung vorbereiten (Deadline: Freitag)
- Marketing-Team: Social Media Content erstellen (nächste Woche)
- Lisa: Influencer-Kontakte aufnehmen
- Design-Team: Finale Produktfotos liefern
- Location für Launch-Event buchen (1. April)
- Website-Landing-Page live schalten (bis Mittwoch)
- Feedback an Lisa bis Mittwoch
```

### test4.txt (Kategorisieren)
```
Kategorie: Automatische Antwort / Out of Office
Status: Abwesenheit vom 18.03.-25.03.2024
Vertretung: sarah.koch@company.com (Vertrieb), support@company.com (Support)
```

## Eigene Mails hinzufügen

Du kannst auch eigene .txt Dateien erstellen. Format:

```
Subject: Dein Betreff
From: absender@example.com
To: empfaenger@example.com
Date: 2024-03-15

Hier kommt der Mail-Text...
```

Die ersten Zeilen mit Subject:/From:/To:/Date: sind optional.
Wenn sie fehlen, wird der gesamte Text als Mail-Body interpretiert.
