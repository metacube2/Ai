#!/usr/bin/env python3
"""Erzeugt public/funkerbier.mod: eigenes 4-Kanal-ProTracker-Stueck (M.K.), 2026-10-15.

Alles ist hier synthetisiert (Wellenformen, Drums, Noten), also frei verwendbar.
Kanaele: 1 Drums, 2 Bass, 3 Akkord-Arpeggio (Effekt 0xy), 4 Melodie.
Akkordfolge Am - F - C - G, Tempo 125 BPM, Speed 6. Die Melodie im Intro und
im Breakdown morst "CQ".

Aufruf: python3 tools/make_mod.py  (schreibt public/funkerbier.mod)
"""
import math
import random
import struct
from pathlib import Path

ZIEL = Path(__file__).resolve().parent.parent / "public" / "funkerbier.mod"
RATE = 8287  # Abspielrate bei C-2 (Periode 428)

PERIODEN = [856, 808, 762, 720, 678, 640, 604, 570, 538, 508, 480, 453,
            428, 404, 381, 360, 339, 320, 302, 285, 269, 254, 240, 226,
            214, 202, 190, 180, 170, 160, 151, 143, 135, 127, 120, 113]
NOTEN = {n: i for i, n in enumerate("C C# D D# E F F# G G# A A# B".split())}


def note(name):
    """'A-2' -> Index in PERIODEN (C-1 = 0)."""
    ton, okt = name[:-2].rstrip("-"), int(name[-1])
    return NOTEN[ton] + 12 * (okt - 1)


# --- Samples -----------------------------------------------------------------
def s8(werte):
    return bytes(max(-128, min(127, int(round(v * 127)))) & 0xFF for v in werte)


def zyklus(n, f):
    return [f(i / n) for i in range(n)]


random.seed(73)
SAMPLES = [
    # (Name, Daten, Lautstaerke, Schleife)
    ("bass", s8(zyklus(32, lambda p: 0.75 * (1 if p < 0.5 else -1) + 0.25 * (1 - 2 * p))), 52, True),
    ("arp", s8(zyklus(32, lambda p: 0.9 if p < 0.25 else -0.9)), 30, True),
    ("lead", s8(zyklus(32, lambda p: 0.8 * (1 - 2 * p) + 0.2 * math.sin(2 * math.pi * 2 * p))), 44, True),
    ("kick", s8([math.sin(2 * math.pi * (45 * i / RATE + 130 * (1 - math.exp(-i / 300)) * 300 / RATE))
                 * math.exp(-i / 900) for i in range(2200)]), 64, False),
    ("snare", s8([(random.uniform(-1, 1) * 0.8 + 0.4 * math.sin(2 * math.pi * 190 * i / RATE))
                  * math.exp(-i / 700) for i in range(2600)]), 50, False),
    ("hat", s8([random.uniform(-1, 1) * math.exp(-i / 120) for i in range(700)]), 34, False),
]
BASS, ARP, LEAD, KICK, SNARE, HAT = range(1, 7)


# --- Noten -------------------------------------------------------------------
def leer():
    return [[None] * 4 for _ in range(64)]


def setze(pat, zeile, kanal, sample=0, n=None, eff=0, par=0):
    pat[zeile][kanal] = (sample, PERIODEN[n] if n is not None else 0, eff, par)


AKKORDE = [("A", 0x37), ("F", 0x47), ("C", 0x47), ("G", 0x47)]  # Grundton, Arpeggio moll/dur


def drums(pat, voll=True):
    for takt in range(4):
        b = takt * 16
        if voll:
            for z in (0, 8) + ((10,) if takt % 2 else ()):
                setze(pat, b + z, 0, KICK, note("C-2"))
            for z in (4, 12):
                setze(pat, b + z, 0, SNARE, note("C-2"))
        for z in (2, 6, 14) if voll else (0, 4, 8, 12):
            setze(pat, b + z, 0, HAT, note("C-3"), 0xC, 0x18)


def bass(pat):
    muster = [(0, 0), (2, 0), (4, 12), (6, 0), (8, 0), (10, 12), (12, 0), (14, 7)]
    for takt, (ton, _) in enumerate(AKKORDE):
        grund = note(f"{ton}-1") if ton != "C" else note("C-2")
        for z, versatz in muster:
            setze(pat, takt * 16 + z, 1, BASS, min(grund + versatz, 35), 0xE, 0xC4)


def arpeggio(pat):
    # Arpeggio wirkt nur in Zeilen mit Effekt 0xy, darum in jeder Zeile wiederholen
    for takt, (ton, arp) in enumerate(AKKORDE):
        grund = note(f"{ton}-2") if ton != "C" else note("C-3")
        setze(pat, takt * 16, 2, ARP, grund, 0, arp)
        for z in range(1, 16):
            setze(pat, takt * 16 + z, 2, 0, None, 0, arp)


def melodie(pat, toene, kanal=3, sample=LEAD):
    """toene: Liste (Zeile, Notenname, Laenge in Zeilen). Ende per C00."""
    belegt = {z for z, _, _ in toene}
    for z, n, laenge in toene:
        setze(pat, z, kanal, sample, note(n))
        ende = z + laenge
        if ende < 64 and ende not in belegt and pat[ende][kanal] is None:
            setze(pat, ende, kanal, 0, None, 0xC, 0)


def morse(text, start=0, ton="A-3"):
    """Morsezeichen als Melodie: dit 1 Zeile, dah 3, Luecke 1, Buchstabe 3."""
    code = {"C": "-.-.", "Q": "--.-", "D": "-..", "E": ".", "K": "-.-"}
    z, toene = start, []
    for b in text:
        if b == " ":
            z += 4
            continue
        for zeichen in code[b]:
            laenge = 1 if zeichen == "." else 3
            toene.append((z, ton, laenge))
            z += laenge + 1
        z += 2
    return toene


MELODIE_A = [
    (0, "E-3", 3), (3, "G-3", 3), (6, "A-3", 2), (8, "G-3", 2), (10, "E-3", 2), (12, "D-3", 2), (14, "C-3", 2),
    (16, "C-3", 3), (19, "D-3", 3), (22, "E-3", 2), (24, "F-3", 4), (28, "E-3", 2), (30, "D-3", 2),
    (32, "E-3", 3), (35, "G-3", 3), (38, "G-3", 2), (40, "E-3", 4), (44, "D-3", 2), (46, "C-3", 2),
    (48, "D-3", 3), (51, "B-2", 3), (54, "G-2", 2), (56, "B-2", 4), (60, "D-3", 4),
]
MELODIE_B = [
    (0, "A-3", 2), (2, "A-3", 2), (4, "G-3", 2), (6, "A-3", 4), (10, "C-3", 2), (12, "E-3", 4),
    (16, "F-3", 2), (18, "F-3", 2), (20, "E-3", 2), (22, "F-3", 4), (26, "A-3", 2), (28, "G-3", 4),
    (32, "G-3", 2), (34, "G-3", 2), (36, "E-3", 2), (38, "G-3", 4), (42, "C-3", 2), (44, "E-3", 4),
    (48, "D-3", 6), (54, "B-2", 2), (56, "D-3", 2), (58, "G-3", 6),
]


def muster():
    p = []
    # 0 Intro: Hats, Bass, Arpeggio, CQ CQ gemorst; Tempo setzen
    pat = leer(); drums(pat, voll=False); bass(pat); arpeggio(pat)
    melodie(pat, morse("CQ CQ", 2))
    setze(pat, 0, 0, HAT, note("C-3"), 0xF, 6)
    p.append(pat)
    # 1 Groove ohne Melodie
    pat = leer(); drums(pat); bass(pat); arpeggio(pat); p.append(pat)
    # 2, 3 Thema A und B
    pat = leer(); drums(pat); bass(pat); arpeggio(pat); melodie(pat, MELODIE_A); p.append(pat)
    pat = leer(); drums(pat); bass(pat); arpeggio(pat); melodie(pat, MELODIE_B); p.append(pat)
    # 4 Breakdown: nur Arpeggio und gemorstes "CQ DE K"
    pat = leer(); drums(pat, voll=False); arpeggio(pat); melodie(pat, morse("CQ DE K", 0, "E-3")); p.append(pat)
    return p


def schreiben():
    pats = muster()
    reihenfolge = [0, 1, 2, 3, 2, 3, 4, 1, 2, 3]
    kopf = b"funkerbier qrv".ljust(20, b"\0")
    for i in range(31):
        if i < len(SAMPLES):
            name, daten, vol, schleife = SAMPLES[i]
            laenge = len(daten) // 2
            kopf += name.encode().ljust(22, b"\0") + struct.pack(">HBBHH", laenge, 0, vol,
                                                                 0, laenge if schleife else 1)
        else:
            kopf += b"\0" * 22 + struct.pack(">HBBHH", 0, 0, 0, 0, 1)
    kopf += bytes([len(reihenfolge), 1]) + bytes(reihenfolge).ljust(128, b"\0") + b"M.K."
    daten = b""
    for pat in pats:
        for zeile in pat:
            for zelle in zeile:
                s, per, eff, par = zelle or (0, 0, 0, 0)
                daten += bytes([(s & 0xF0) | (per >> 8), per & 0xFF, ((s & 0x0F) << 4) | eff, par])
    proben = b"".join(d[: len(d) // 2 * 2] for _, d, _, _ in SAMPLES)
    ZIEL.write_bytes(kopf + daten + proben)
    print(f"{ZIEL.name}: {len(kopf + daten + proben)} Bytes, {len(pats)} Patterns, {len(reihenfolge)} Positionen")


if __name__ == "__main__":
    schreiben()
