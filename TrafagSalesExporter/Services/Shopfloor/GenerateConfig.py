# -*- coding: utf-8 -*-
"""Erzeugt shopfloor_config.json aus der Python-Vorlage (modules_config.py des PPA-Shopfloor-Pakets).
Aufruf:  python GenerateConfig.py <Ordner mit modules_config.py, forecast.py, notify.py> <Zieldatei>
Das Ergebnis entspricht exakt dem, was server.py unter /api/config ausliefert.
Normalerweise nicht mehr noetig: die Konfiguration wird jetzt direkt in der JSON-Datei gepflegt."""
import json, sys, os
src, dst = sys.argv[1], sys.argv[2]
sys.path.insert(0, src)
import modules_config as cfg
with open(dst, "w", encoding="utf-8") as f:
    json.dump(cfg.get_config(), f, ensure_ascii=False, indent=1)
print("geschrieben", dst, os.path.getsize(dst), "Bytes")
