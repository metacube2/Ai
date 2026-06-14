# PayPal Und Website-Konfiguration

Diese Datei beschreibt, wie `MacYaesu` für den einfachen Direktverkauf eingerichtet ist und was angepasst werden muss, damit Zahlungen und Downloads live funktionieren.

## Aktueller Stand

Die Landingpage liegt unter:

- `marketing-site/index.html`

Die zentrale Konfiguration liegt unter:

- `marketing-site/config.js`

Die Website liest daraus automatisch:

- Preis
- PayPal-Link
- Support-/Lizenz-Mail
- DMG-Datei
- Demo-Laufzeit

## Bereits eingetragen

In `marketing-site/config.js` ist aktuell gesetzt:

- `priceChf: 49`
- `supportEmail: "metacube@gmail.com"`
- `licenseEmail: "metacube@gmail.com"`
- `pricingEmail: "metacube@gmail.com"`
- `downloadFile: "../MacYaesu.dmg"`
- `downloadFileName: "MacYaesu.dmg"`
- `demoMinutes: 15`

Der PayPal-Link ist noch Platzhalter:

- `paypalUrl: "https://www.paypal.com/"`

## Was noch zu tun ist

### 1. PayPal-Link eintragen

Empfohlene einfache Variante:

1. PayPal-Konto öffnen
2. `PayPal.Me` einrichten
3. Link mit festem Betrag verwenden

Beispiel:

```js
paypalUrl: "https://paypal.me/DEINNAME/49CHF"
```

Alternativ kann auch ein echter PayPal-Checkout- oder Button-Link eingetragen werden.

### 2. DMG-Datei ablegen

Die Website erwartet aktuell:

- `MacYaesu.dmg`

Speicherort:

- direkt im Projekt-Hauptordner neben `marketing-site/`

Also hier:

- `MacYaesu.dmg`
- `marketing-site/`

Wenn die Datei anders heißt, in `marketing-site/config.js` anpassen:

```js
downloadFile: "../AndererName.dmg",
downloadFileName: "AndererName.dmg"
```

## Verkaufsablauf

Der aktuelle geplante Ablauf ist bewusst simpel:

1. Besucher lädt Demo herunter
2. Demo läuft 15 Minuten
3. Besucher klickt auf Kauf per PayPal
4. Nach Zahlung wird der Lizenzschlüssel manuell per E-Mail verschickt
5. Käufer aktiviert die App lokal mit dem Key

## Preis

Aktuell gesetzt:

- `49 CHF` einmalig

Regelmäßige Updates sind im Text als kostenlos enthalten kommuniziert.

## Wo man die Werte ändert

Alles Relevante an einer Stelle:

- `marketing-site/config.js`

Typische spätere Änderungen:

- Preis ändern
- PayPal-Link einsetzen
- E-Mail-Adresse ändern
- DMG-Dateiname ändern
- Demo-Dauer ändern

## App-Aktivierung

Die App ist aktuell lokal so gebaut:

- Demo läuft 15 Minuten aktive Nutzung
- danach stoppt die App ohne Aktivierung
- Lizenzschlüssel schaltet frei

Die Aktivierungslogik liegt in:

- `MacYaesu/ViewModels/SettingsController.swift`

## Nächster Schritt

Wenn später ein echter Verkauf startet:

1. echten PayPal-Link in `marketing-site/config.js` eintragen
2. `MacYaesu.dmg` ins Projekt legen
3. Website veröffentlichen
4. Lizenzschlüssel manuell per Mail verschicken
