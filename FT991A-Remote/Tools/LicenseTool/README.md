# LicenseTool (stillgelegt)

Seit 2026-11-06 prueft MacYaesu Lizenzschluessel per **Ed25519-Signatur**. Der private Schluessel liegt nur im
Schluesselbund des Entwickler-Macs, die App enthaelt nur den oeffentlichen. Dieses Werkzeug erzeugt keine
Schluessel mehr und gibt nur noch den Hinweis auf das neue Werkzeug aus.

Ausstellen und Pruefen:

```bash
swift ~/Projects/Git/aiscom-lizenz/lizenz.swift ausstellen macyaesu kunde@example.com
swift ~/Projects/Git/aiscom-lizenz/lizenz.swift pruefen macyaesu kunde@example.com <schluessel>
```

Schluessel des alten Verfahrens (`XXXX-XXXX-XXXX-XXXX`, 16 Hex-Zeichen) gelten nicht mehr; die App erkennt sie
und bittet um einen neuen Schluessel.
