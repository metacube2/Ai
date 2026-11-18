"""Einstellungen in data/config.json (600). Geheimnisse verlassen den Dienst nie im Klartext."""
from __future__ import annotations

import json
import os
import re
import secrets
import threading
from pathlib import Path

GEHEIM = ("ical_url", "claude_schluessel")
STANDARD = {
    "ical_url": "",
    "ntfy_server": "http://192.168.178.113:8086",
    "ntfy_thema": "",
    "claude_schluessel": "",
    "claude_modell": "claude-haiku-5-5",
    "push_routine": True,
    "push_papier": True,
    "push_termin": True,
    "push_fokus": True,
    "termin_vorlauf_min": 15,
    "ruhe_von": "21:30",
    "ruhe_bis": "07:00",
    "hyperfokus_min": 90,
    "ton": True,
}
_sperre = threading.Lock()
UHR = re.compile(r"([01]\d|2[0-3]):[0-5]\d")


class Einstellungen:
    def __init__(self, pfad: Path):
        self.pfad = pfad
        with _sperre:
            if not pfad.exists():
                self._schreiben({**STANDARD, "ntfy_thema": "kompass-" + secrets.token_urlsafe(18).replace("_", "").replace("-", "")[:24]})

    def _schreiben(self, d: dict) -> None:
        tmp = self.pfad.with_suffix(".tmp")
        fd = os.open(tmp, os.O_WRONLY | os.O_CREAT | os.O_TRUNC, 0o600)
        with os.fdopen(fd, "w") as f:
            json.dump(d, f, indent=1)
        os.replace(tmp, self.pfad)

    def alle(self) -> dict:
        try:
            gespeichert = json.loads(self.pfad.read_text())
        except (OSError, ValueError):
            gespeichert = {}
        return {**STANDARD, **gespeichert}

    def get(self, schluessel: str):
        return self.alle()[schluessel]

    def fuer_anzeige(self) -> dict:
        d = self.alle()
        for k in GEHEIM:
            d[k] = ("…" + d[k][-4:]) if d[k] else ""
        return d

    def setzen(self, neu: dict) -> dict:
        """Uebernimmt nur bekannte Felder mit gueltigen Werten. Leeres Geheimnis = unveraendert,
        das Wort 'loeschen' leert es."""
        with _sperre:
            d = self.alle()
            for k, v in neu.items():
                if k not in STANDARD:
                    continue
                std = STANDARD[k]
                if k in GEHEIM:
                    if not isinstance(v, str) or not v.strip():
                        continue
                    v = "" if v.strip() == "loeschen" else v.strip()
                    if k == "ical_url" and v and not re.fullmatch(r"(https|webcal)://\S{10,2000}", v):
                        raise ValueError("Die iCal-Adresse muss mit https:// beginnen.")
                elif isinstance(std, bool):
                    v = bool(v)
                elif isinstance(std, int):
                    try:
                        v = int(v)
                    except (TypeError, ValueError):
                        raise ValueError(f"{k}: Zahl erwartet")
                    if not 1 <= v <= 600:
                        raise ValueError(f"{k}: zwischen 1 und 600")
                elif k in ("ruhe_von", "ruhe_bis"):
                    if not isinstance(v, str) or not UHR.fullmatch(v):
                        raise ValueError("Ruhezeit als HH:MM")
                elif k == "ntfy_thema":
                    if not isinstance(v, str) or not re.fullmatch(r"[A-Za-z0-9_-]{8,64}", v):
                        raise ValueError("ntfy-Thema: 8–64 Zeichen, Buchstaben, Ziffern, - und _")
                elif k == "ntfy_server":
                    if not isinstance(v, str) or not re.fullmatch(r"https?://[A-Za-z0-9.:-]+", v):
                        raise ValueError("ntfy-Server als http(s)://host:port")
                elif k == "claude_modell":
                    if not isinstance(v, str) or not re.fullmatch(r"claude-[a-z0-9.-]{3,60}", v):
                        raise ValueError("Unbekanntes Modell")
                d[k] = v
            self._schreiben(d)
        return self.fuer_anzeige()
