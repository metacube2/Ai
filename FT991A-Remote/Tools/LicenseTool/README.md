# LicenseTool

Offline-Konsolenwerkzeug fuer MacYaesu-Lizenzschluessel.

Beispiele:

```bash
swift run --package-path Tools/LicenseTool license-tool generate max@example.com
swift run --package-path Tools/LicenseTool license-tool verify max@example.com ABCD-EF12-3456-7890
```

Die App verwendet denselben Algorithmus. Ein Schluessel ist damit an die normalisierte E-Mail-Adresse gebunden.
