"""Flüssigmetall II: Personenmaske, gelernte Tiefe, FLIP-Strömung und Chrom.

- Form: Tiefenschätzung mit Depth Anything V2 (lokal), sonst geschätztes Relief.
- Bewegung: FLIP-Flüssigkeitssimulation (bildeffekte.liquid_sim) mit Schwerkraft,
  Inkompressibilität, Viskosität und Oberflächenspannung; Metall am Boden läuft in
  eine Bodenpfütze ab, deren Fläche aus dem abgeflossenen Volumen folgt.
- Hintergrund: Livebilder werden gemerkt, wo gerade niemand steht. Ist beim
  Anwählen niemand im Bild, wartet der Effekt, bis jemand hineintritt.
- Danach läuft die Aufzeichnung rückwärts bis zum Startbild und wieder vorwärts.

Der gesamte Ablauf steckt in bildeffekte/verwandlung.py und wird auch von den
Verwandlungs-Effekten (38_video_verwandlungen.py) genutzt.
"""

from bildeffekte.verwandlung import (  # noqa: F401  (für Tests und Nachbar-Plugins)
    HOLD_PUDDLE, HOLD_START, PLATE as _PLATE, BackgroundPlate, FluidVerwandlung)


class FluessigmetallII(FluidVerwandlung):
    name = "Flüssigmetall-Schmelze II"
    description = ("Die Person wird zu flüssigem Chrom und fließt physikalisch simuliert in "
                   "eine Metallpfütze – danach läuft alles rückwärts bis zum Startbild und "
                   "wieder von vorn. Beste Wirkung: kurz aus dem Bild gehen, Effekt wählen, "
                   "dann ins Bild treten – so ist der Hintergrund hinter dir bekannt. "
                   "Erneut anwählen friert ein neues Bild ein.")
