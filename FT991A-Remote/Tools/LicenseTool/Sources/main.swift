// MacYaesu-Lizenzschluessel werden seit 2026-11-06 nicht mehr hier erzeugt.
// Neues Verfahren: Ed25519-Signatur, privater Schluessel nur im Schluesselbund des Entwickler-Macs.
// Ausstellen:  swift ~/Projects/Git/aiscom-lizenz/lizenz.swift ausstellen macyaesu <e-mail>
// Pruefen:     swift ~/Projects/Git/aiscom-lizenz/lizenz.swift pruefen macyaesu <e-mail> <schluessel>
import Foundation

let hinweis = """
Dieses Werkzeug ist stillgelegt (altes Verfahren, Schluessel ungueltig).
Lizenzschluessel fuer MacYaesu jetzt so ausstellen:
  swift ~/Projects/Git/aiscom-lizenz/lizenz.swift ausstellen macyaesu <e-mail>
Pruefen:
  swift ~/Projects/Git/aiscom-lizenz/lizenz.swift pruefen macyaesu <e-mail> <schluessel>

"""
FileHandle.standardError.write(Data(hinweis.utf8))
exit(1)
