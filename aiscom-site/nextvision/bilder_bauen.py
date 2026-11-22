#!/usr/bin/env python3
"""Baut die verlorenen Bilder der alten Nextvision-Seite nach (2026-10-10).

Die Originale unter geocities.com/ikohler_2002/BILDER/ und ois.ch/soundwerk/nextvision/
sind in keinem Archiv erhalten. Grösse, Dateiname und Zweck stammen aus dem
archivierten HTML; der Inhalt ist ein Nachbau im Stil von 2002 (GIF-Palette,
JPEG-Kompression). Aufruf: <python mit Pillow> bilder_bauen.py site/
"""
import math
import random
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ZIEL = Path(sys.argv[1] if len(sys.argv) > 1 else "site")
FONTS = Path("/System/Library/Fonts/Supplemental")
EMOJI = "/System/Library/Fonts/Apple Color Emoji.ttc"
random.seed(2002)


def schrift(name: str, groesse: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(FONTS / name), groesse)


def emoji(zeichen: str, groesse: int, spiegeln: bool = False) -> Image.Image:
    bild = Image.new("RGBA", (180, 180), (0, 0, 0, 0))
    ImageDraw.Draw(bild).text((10, 10), zeichen, font=ImageFont.truetype(EMOJI, 160), embedded_color=True)
    bild = bild.crop(bild.getbbox())
    if spiegeln:
        bild = bild.transpose(Image.FLIP_LEFT_RIGHT)
    bild.thumbnail((groesse, groesse), Image.LANCZOS)
    return bild


def als_gif(bild: Image.Image, pfad: Path, farben: int = 32) -> None:
    """Wie 2002: Palette statt Echtfarben, weisser Hintergrund statt Alpha."""
    flach = Image.new("RGB", bild.size, "white")
    flach.paste(bild, mask=bild.split()[-1] if bild.mode == "RGBA" else None)
    flach.convert("P", palette=Image.ADAPTIVE, colors=farben).save(pfad)


def als_jpg(bild: Image.Image, pfad: Path, qualitaet: int = 55) -> None:
    bild.convert("RGB").save(pfad, quality=qualitaet)


def hand(pfad: Path, breite: int, hoehe: int, links: bool) -> None:
    bild = Image.new("RGBA", (breite, hoehe), (255, 255, 255, 255))
    h = emoji("👉", min(hoehe - 6, 70), spiegeln=links)
    x = breite - h.width - 4 if not links else 4
    bild.alpha_composite(h, (x, hoehe - h.height - 2))
    als_gif(bild, pfad)


def nextvision_schriftzug(pfad: Path) -> None:
    """336x75: Schriftzug wie im Logo von 2003 (NeXTvision), Psy-Farbverlauf."""
    breite, hoehe = 336, 75
    verlauf = Image.new("RGB", (breite, hoehe))
    zeichner = ImageDraw.Draw(verlauf)
    for x in range(breite):
        t = x / breite
        zeichner.line([(x, 0), (x, hoehe)], fill=(int(90 + 120 * t), int(40 + 30 * math.sin(t * 6)), int(220 - 60 * t)))
    maske = Image.new("L", (breite, hoehe), 0)
    ImageDraw.Draw(maske).text((6, 4), "NeXTvision", font=schrift("Arial Black.ttf", 52), fill=255)
    schatten = maske.filter(ImageFilter.GaussianBlur(3))
    bild = Image.new("RGB", (breite, hoehe), "white")
    bild.paste((170, 170, 190), (3, 3), schatten)
    bild.paste(verlauf, (0, 0), maske)
    als_jpg(bild, pfad, 60)


def domain(pfad: Path) -> None:
    """194x62: Domain-Schild .ch.vu"""
    bild = Image.new("RGB", (194, 62), "white")
    z = ImageDraw.Draw(bild)
    z.text((8, 10), ".ch.vu", font=schrift("Arial Bold Italic.ttf", 36), fill=(255, 66, 54))
    z.line([(10, 52), (184, 52)], fill=(111, 111, 111), width=2)
    als_gif(bild, pfad, 16)


def maus(pfad: Path, breite: int = 49, hoehe: int = 28) -> None:
    bild = Image.new("RGBA", (breite, hoehe), (255, 255, 255, 255))
    m = emoji("🖱", hoehe - 2)
    bild.alpha_composite(m, ((breite - m.width) // 2, (hoehe - m.height) // 2))
    # Kabel nach links, wie bei den Reitern
    ImageDraw.Draw(bild).line([(0, hoehe // 2), ((breite - m.width) // 2 + 2, hoehe // 2)], fill=(60, 60, 60), width=1)
    als_jpg(bild, pfad, 70)


def kachel(pfad: Path, grund: tuple, streuung: int, breite: int = 120, hoehe: int = 49) -> None:
    """Hintergrundkachel für die Reiter (blau bzw. grau), leicht verrauscht wie ein altes JPEG."""
    bild = Image.new("RGB", (breite, hoehe))
    px = bild.load()
    for y in range(hoehe):
        for x in range(breite):
            d = random.randint(-streuung, streuung) - y // 6
            px[x, y] = tuple(max(0, min(255, c + d)) for c in grund)
    als_jpg(bild, pfad, 50)


def no_tcpa(pfad: Path) -> None:
    """48x48: rotes Verbotsschild über TCPA, die Kampagne von 2002/2003."""
    gross = Image.new("RGB", (192, 192), "white")
    z = ImageDraw.Draw(gross)
    z.text((26, 70), "TCPA", font=schrift("Arial Black.ttf", 38), fill="black")
    z.ellipse([8, 8, 184, 184], outline=(220, 0, 0), width=20)
    z.line([(40, 40), (152, 152)], fill=(220, 0, 0), width=20)
    als_gif(gross.resize((48, 48), Image.LANCZOS), pfad, 16)


def weiss(pfad: Path) -> None:
    als_jpg(Image.new("RGB", (62, 99), "white"), pfad, 90)


def weltraum(pfad: Path, breite: int = 1123, hoehe: int = 917) -> None:
    """2004: dunkler Hintergrund der Intro-Seite mit Sternen und violettem Nebel."""
    bild = Image.new("RGB", (breite, hoehe))
    z = ImageDraw.Draw(bild)
    for y in range(hoehe):
        t = y / hoehe
        z.line([(0, y), (breite, y)], fill=(int(10 + 30 * t), int(8 + 10 * t), int(40 + 60 * t)))
    nebel = Image.new("RGB", (breite, hoehe), (0, 0, 0))
    nz = ImageDraw.Draw(nebel)
    for _ in range(14):
        x, y, r = random.randint(0, breite), random.randint(0, hoehe), random.randint(80, 260)
        nz.ellipse([x - r, y - r, x + r, y + r], fill=(random.randint(40, 90), 10, random.randint(70, 130)))
    bild = Image.blend(bild, nebel.filter(ImageFilter.GaussianBlur(90)), 0.45)
    z = ImageDraw.Draw(bild)
    for _ in range(700):
        x, y = random.randint(0, breite - 1), random.randint(0, hoehe - 1)
        h = random.randint(120, 255)
        z.point((x, y), fill=(h, h, min(255, h + 30)))
    als_jpg(bild, pfad, 55)


def auge(pfad: Path, breite: int = 204, hoehe: int = 129) -> None:
    """2004: blinzelndes Auge (animiertes GIF), Eingang zum Mitgliederbereich."""
    bilder = []
    for offen in (1.0, 1.0, 1.0, 1.0, 0.6, 0.15, 0.6, 1.0):
        b = Image.new("RGB", (breite, hoehe), (0, 0, 17))
        z = ImageDraw.Draw(b)
        cx, cy = breite // 2, hoehe // 2
        rx, ry = 92, int(54 * offen)
        if ry > 4:
            z.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=(235, 235, 245))
            for r, farbe in ((38, (30, 90, 200)), (30, (70, 40, 180)), (22, (140, 60, 220)), (12, (0, 0, 0))):
                z.ellipse([cx - r, cy - r, cx + r, cy + r], fill=farbe)
            z.ellipse([cx + 4, cy - 12, cx + 12, cy - 4], fill="white")
            maske = Image.new("L", (breite, hoehe), 0)
            ImageDraw.Draw(maske).ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=255)
            hinter = Image.new("RGB", (breite, hoehe), (0, 0, 17))
            b = Image.composite(b, hinter, maske)
            z = ImageDraw.Draw(b)
        z.arc([cx - rx, cy - ry - 2, cx + rx, cy + ry + 2], 180, 360, fill=(170, 120, 255), width=4)
        z.arc([cx - rx, cy - ry - 2, cx + rx, cy + ry + 2], 0, 180, fill=(110, 80, 200), width=2)
        bilder.append(b.convert("P", palette=Image.ADAPTIVE, colors=64))
    dauer = [900, 900, 900, 900, 80, 120, 80, 600]
    bilder[0].save(pfad, save_all=True, append_images=bilder[1:], duration=dauer, loop=0)


def main() -> None:
    g = ZIEL / "2002"
    (g / "BILDER").mkdir(parents=True, exist_ok=True)
    hand(g / "BILDER/HAND.gif", 123, 79, links=False)
    hand(g / "BILDER/HANDR.gif", 108, 75, links=True)
    weiss(g / "BILDER/white.jpg")
    nextvision_schriftzug(g / "BILDER/next.jpg")
    domain(g / "BILDER/dechvu.gif")
    maus(g / "BILDER/mice.jpg")
    no_tcpa(g / "BILDER/notcpa.gif")
    kachel(g / "Back.jpg", (51, 102, 204), 10)
    kachel(g / "grayback.jpg", (239, 239, 239), 4)
    v = ZIEL / "2004"
    v.mkdir(parents=True, exist_ok=True)
    weltraum(v / "back.jpg")
    auge(v / "auge.gif")
    print("Nachbau fertig:", ZIEL)


if __name__ == "__main__":
    main()
