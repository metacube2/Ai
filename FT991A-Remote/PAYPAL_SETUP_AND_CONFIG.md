# PayPal- und Website-Konfiguration

## Aktueller Verkaufsfluss

Die öffentliche Seite liegt unter `https://www.aiscom.ch/macyaesu/`. Seit
2026-10-04 zeigt `https://www.aiscom.ch/` wieder die AISCOM-Firmenseite; MacYaesu
ist dort unter „Projekte im Verkauf“ verlinkt (Details: `../aiscom-site/README.md`).
Die Dateien `marketing-site/root-*` und `aiscom-index.html` sind damit überholt.
Der
Kaufbutton öffnet einen vorkonfigurierten PayPal-Checkout für:

- Produkt: `MacYaesu Vollversion`
- Artikelnummer: `MACYAESU-1.1`
- Preis: `49.00 CHF`
- Empfänger: `metacube@gmail.com`
- Lizenzbindung: die vor dem Kauf eingegebene E-Mail-Adresse

Die Website übergibt keine geheimen Zugangsdaten. Sie verwendet PayPals
gehosteten `Buy Now`-Ablauf; Anmeldung, Zahlungsart und Zahlungsbestätigung
finden ausschließlich bei PayPal statt.

## Ablauf pro Bestellung

1. Käufer gibt die E-Mail-Adresse für die Lizenz ein.
2. PayPal zeigt Produkt, Empfänger und Betrag und führt die Zahlung aus.
3. Im PayPal-Händlerkonto prüfen: Status abgeschlossen, `49.00 CHF`,
   Artikelnummer `MACYAESU-1.1` und Lizenz-E-Mail vorhanden.
4. Schlüssel im Projektordner erzeugen:

   ```bash
   ./generate-license.command kunde@example.com
   ```

5. Schlüssel an exakt diese E-Mail-Adresse schicken. Die App akzeptiert ihn
   nur zusammen mit der gebundenen Adresse.

Die Rückkehrseite zeigt nur eine Empfangsmeldung. Sie ist kein Zahlungsbeleg;
ein Schlüssel wird erst nach Kontrolle der Transaktion ausgegeben.

## Zentrale Konfiguration

`marketing-site/config.js` enthält Preis, Währung, PayPal-Empfänger,
Artikelnummer, Kontaktadressen, Download und Release-Metadaten. Bei einer
Preis- oder Versionsänderung immer gleichzeitig anpassen:

- `priceChf`
- `currency`
- `productNumber`
- `releaseVersion` und `releaseBuild`
- Website-Angebot und PayPal-Kontrollbetrag

`paypalMerchantEmail` muss zu einem aktiven PayPal-Konto gehören, das
Zahlungen in CHF empfangen kann.

## App und Download

- Die Demo erlaubt 15 Minuten aktive Nutzung.
- Ein Schlüssel wird aus normalisierter E-Mail und dem lokalen
  Lizenzverfahren erzeugt und von der App tatsächlich geprüft.
- Beliebige Texte oder ein gespeichertes Aktivierungs-Flag schalten die App
  nicht frei.
- Das aktuelle Release liegt als `MacYaesu.dmg` neben der Website.

Vor Veröffentlichung prüfen:

```bash
node --check marketing-site/config.js
node --check marketing-site/app.js
swift run --package-path Tools/LicenseTool license-tool verify \
  kunde@example.com "$(./generate-license.command kunde@example.com)"
```

Danach `index.html`, `styles.css`, `app.js`, `config.js`, `robots.txt`,
`sitemap.xml`, `main.png`, `memview.png` und `MacYaesu.dmg` nach
`/macyaesu/` ausliefern.

## Grenze dieser Variante

Lizenzversand und Zahlungsabgleich sind bewusst manuell. Eine automatische
Ausgabe darf nicht allein einer Browser-Rückleitung vertrauen. Dafür wäre ein
öffentlich erreichbares Backend mit PayPal Orders API, geheimem Client-Secret,
Webhook-Prüfung und Mailversand erforderlich.
