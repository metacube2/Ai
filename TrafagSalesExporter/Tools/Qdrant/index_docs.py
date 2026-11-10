# /// script
# requires-python = ">=3.12,<3.13"
# dependencies = [
#   "fastembed>=0.7.0",
#   "qdrant-client>=1.12.0",
# ]
# ///
"""Indexiert die Markdown-Dokumentation des Repositories in eine lokale Qdrant-Sammlung.

Das Schema ist bewusst identisch zu dem, was `mcp-server-qdrant` schreibt und liest:

* Payload-Schluessel `document` fuer den Text und `metadata` fuer die Zusatzangaben.
* Benannter Vektor `fast-<modellname>`, gebildet wie in `FastEmbedProvider.get_vector_name()`.
* Distanzmass Cosine.
* Dokumente werden mit `passage_embed` eingebettet, Suchanfragen spaeter mit `query_embed`.

Weicht eines dieser vier Details ab, liefert `qdrant-find` stillschweigend nichts zurueck,
obwohl jeder einzelne Schritt erfolgreich aussieht.

Seit 2026-11-10 (Wochen_Todo 118):

* `--stand DATEI` schreibt nach einem vollstaendigen Lauf Zeitpunkt, Datei-, Abschnitts- und
  Punktzahl in eine kleine JSON-Datei.
* `--pruefen` baut nichts, sondern vergleicht den Index mit den Dateien: Stimmt die Punktzahl
  der Sammlung mit der Abschnittszahl ueberein, und ist seit dem letzten Lauf eine Markdown-Datei
  geaendert worden? Exitcode 0 = aktuell, 3 = veraltet. Anlass: Am 2026-10-21 fehlte eine Woche
  Doku im Index, am 2026-10-27 blieb er nach einem Abbruch zwei Tage zu zwei Dritteln leer.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
import uuid
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path

from qdrant_client import QdrantClient, models

EXIT_VERALTET = 3

# Verzeichnisse, die keine Projektdokumentation enthalten, sondern Build-Ausgaben,
# Exportpakete oder temporaere Sondierungen.
AUSGESCHLOSSENE_ORDNER = {
    ".git",
    ".vs",
    ".vscode",
    ".venv",
    "venv",
    "node_modules",
    "site-packages",
    "bin",
    "obj",
    "packages",
    "TestResults",
}

AUSGESCHLOSSENE_MUSTER = (
    ".tmp_",
    "ExportPackage",
)

MAX_ZEICHEN = 1400
UEBERLAPPUNG = 180
NAMESPACE = uuid.UUID("6f9619ff-8b86-d011-b42d-00cf4fc964ff")


@dataclass
class Abschnitt:
    """Ein indexierbarer Textabschnitt aus einer Markdown-Datei."""

    datei: str
    ueberschrift: str
    teil: int
    text: str

    @property
    def dokument(self) -> str:
        """Der Text, den die Suche zurueckgibt.

        Datei und Ueberschrift stehen mit im Text, damit ein Treffer auch ohne Blick
        auf die Metadaten verstaendlich ist und die Herkunft sofort sichtbar wird.
        """
        kopf = f"Datei: {self.datei}"
        if self.ueberschrift:
            kopf += f"\nAbschnitt: {self.ueberschrift}"
        return f"{kopf}\n\n{self.text}"

    @property
    def punkt_id(self) -> str:
        """Stabile Kennung, damit ein erneuter Lauf denselben Punkt ueberschreibt."""
        return uuid.uuid5(NAMESPACE, f"{self.datei}#{self.teil}").hex


def ist_relevant(pfad: Path, wurzel: Path) -> bool:
    relativ = pfad.relative_to(wurzel)
    for teil in relativ.parts:
        if teil in AUSGESCHLOSSENE_ORDNER:
            return False
        if any(muster in teil for muster in AUSGESCHLOSSENE_MUSTER):
            return False
    return True


def markdown_dateien(wurzel: Path) -> list[Path]:
    return sorted(p for p in wurzel.rglob("*.md") if p.is_file() and ist_relevant(p, wurzel))


def text_teilen(text: str) -> list[str]:
    """Zerlegt einen zu langen Abschnitt in ueberlappende Stuecke an Absatzgrenzen."""
    if len(text) <= MAX_ZEICHEN:
        return [text]

    stuecke: list[str] = []
    rest = text
    while len(rest) > MAX_ZEICHEN:
        schnitt = rest.rfind("\n\n", 0, MAX_ZEICHEN)
        if schnitt < MAX_ZEICHEN // 3:
            schnitt = rest.rfind("\n", 0, MAX_ZEICHEN)
        if schnitt < MAX_ZEICHEN // 3:
            schnitt = MAX_ZEICHEN
        stuecke.append(rest[:schnitt].strip())
        rest = rest[max(0, schnitt - UEBERLAPPUNG):].strip()
    if rest:
        stuecke.append(rest)
    return [s for s in stuecke if s]


def datei_zerlegen(pfad: Path, wurzel: Path) -> list[Abschnitt]:
    """Schneidet eine Markdown-Datei an ihren Ueberschriften auf."""
    relativ = pfad.relative_to(wurzel).as_posix()
    try:
        inhalt = pfad.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        inhalt = pfad.read_text(encoding="latin-1")

    ueberschrift_muster = re.compile(r"^(#{1,6})\s+(.*)$")
    # Ein Zaun ist ``` oder ~~~, optional mit Sprachangabe dahinter.
    zaun_muster = re.compile(r"^\s{0,3}(`{3,}|~{3,})")
    stapel: list[str] = []
    aktuelle_ueberschrift = ""
    puffer: list[str] = []
    rohabschnitte: list[tuple[str, str]] = []
    im_codeblock = False

    def puffer_abgeben() -> None:
        text = "\n".join(puffer).strip()
        if text:
            rohabschnitte.append((aktuelle_ueberschrift, text))
        puffer.clear()

    for zeile in inhalt.splitlines():
        # Innerhalb eines Codeblocks ist `#` ein Kommentar, keine Ueberschrift.
        # Ohne diese Unterscheidung wurde aus `# Deklarationen: CU (public) ...`
        # in einem PowerShell-Beispiel eine H1, und alle folgenden Abschnitte der
        # Datei hingen darunter statt unter ihrer echten Ueberschrift. Am
        # 2026-09-11 an saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md aufgefallen,
        # wo rund zwanzig Kommentarzeilen so die Gliederung zerschnitten haben.
        if zaun_muster.match(zeile):
            im_codeblock = not im_codeblock
            puffer.append(zeile)
            continue

        treffer = None if im_codeblock else ueberschrift_muster.match(zeile)
        if treffer:
            puffer_abgeben()
            ebene = len(treffer.group(1))
            titel = treffer.group(2).strip()
            stapel = stapel[: ebene - 1]
            stapel.append(titel)
            aktuelle_ueberschrift = " > ".join(stapel)
            puffer.append(zeile)
        else:
            puffer.append(zeile)
    puffer_abgeben()

    if not rohabschnitte:
        return []

    abschnitte: list[Abschnitt] = []
    laufende_nummer = 0
    for ueberschrift, text in rohabschnitte:
        for stueck in text_teilen(text):
            abschnitte.append(
                Abschnitt(datei=relativ, ueberschrift=ueberschrift, teil=laufende_nummer, text=stueck)
            )
            laufende_nummer += 1
    return abschnitte


def stand_lesen(pfad: Path | None) -> dict | None:
    if pfad is None or not pfad.is_file():
        return None
    try:
        return json.loads(pfad.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return None


def stand_schreiben(pfad: Path, zeit: datetime, sammlung: str, dateien: int, abschnitte: int, punkte: int) -> None:
    pfad.parent.mkdir(parents=True, exist_ok=True)
    pfad.write_text(
        json.dumps(
            {
                "zeit": zeit.isoformat(timespec="seconds"),
                "sammlung": sammlung,
                "dateien": dateien,
                "abschnitte": abschnitte,
                "punkte": punkte,
            },
            indent=2,
        ),
        encoding="utf-8",
    )


def geaendert_seit(dateien: list[Path], zeit: datetime) -> list[Path]:
    grenze = zeit.timestamp()
    return [d for d in dateien if d.stat().st_mtime > grenze]


def pruefen(
    client: QdrantClient, sammlung: str, wurzel: Path, dateien: list[Path], abschnitte: int, stand: dict | None
) -> int:
    """Vergleicht den Index mit den Dateien, ohne etwas zu schreiben. 0 = aktuell, 3 = veraltet."""
    gruende: list[str] = []
    if not client.collection_exists(sammlung):
        gruende.append(f"Die Sammlung '{sammlung}' gibt es nicht.")
    else:
        punkte = client.count(sammlung, exact=True).count
        if punkte != abschnitte:
            gruende.append(f"Die Sammlung hat {punkte} Punkte, die Dateien ergeben {abschnitte} Abschnitte.")

    zeit = None
    if stand and stand.get("sammlung") == sammlung:
        try:
            zeit = datetime.fromisoformat(str(stand.get("zeit")))
        except ValueError:
            zeit = None
    if zeit is None:
        gruende.append("Kein Stand eines vollstaendigen Laufs vorhanden (Datei fehlt oder passt nicht zur Sammlung).")
    else:
        neu = geaendert_seit(dateien, zeit)
        if neu:
            beispiele = ", ".join(d.relative_to(wurzel).as_posix() for d in neu[:8])
            mehr = f" und {len(neu) - 8} weitere" if len(neu) > 8 else ""
            gruende.append(f"{len(neu)} Dateien seit dem letzten Lauf ({zeit:%Y-%m-%d %H:%M}) geaendert: {beispiele}{mehr}.")

    if gruende:
        print("Index VERALTET:")
        for grund in gruende:
            print(f"  - {grund}")
        return EXIT_VERALTET
    print(f"Index aktuell: {abschnitte} Abschnitte aus {len(dateien)} Dateien, letzter Lauf {zeit:%Y-%m-%d %H:%M}.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Markdown-Dokumentation nach Qdrant indexieren.")
    parser.add_argument("--wurzel", default=str(Path(__file__).resolve().parents[2]))
    parser.add_argument("--url", default=os.environ.get("QDRANT_URL", "http://127.0.0.1:6333"))
    parser.add_argument("--sammlung", default=os.environ.get("COLLECTION_NAME", "trafag_docs"))
    parser.add_argument(
        "--modell",
        default=os.environ.get("EMBEDDING_MODEL", "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2"),
    )
    parser.add_argument(
        "--stapel",
        type=int,
        default=32,
        help="Wie viele Abschnitte je Durchgang eingebettet und geschrieben werden.",
    )
    parser.add_argument(
        "--stand",
        default=os.environ.get("REINDEX_STAND", ""),
        help="JSON-Datei mit dem Stand des letzten vollstaendigen Laufs (wird geschrieben bzw. bei --pruefen gelesen).",
    )
    parser.add_argument(
        "--pruefen",
        action="store_true",
        help="Nichts schreiben, nur melden, ob der Index zu den Dateien passt (Exitcode 0 aktuell, 3 veraltet).",
    )
    argumente = parser.parse_args()
    standdatei = Path(argumente.stand) if argumente.stand else None

    wurzel = Path(argumente.wurzel).resolve()
    # Als Stand gilt der Beginn des Laufs: Was waehrend der Minuten des Einbettens geaendert wird,
    # ist beim naechsten Pruefen neuer als der Index.
    beginn = datetime.now()
    dateien = markdown_dateien(wurzel)
    if not dateien:
        print(f"Keine Markdown-Dateien unter {wurzel} gefunden.", file=sys.stderr)
        return 1

    abschnitte: list[Abschnitt] = []
    for datei in dateien:
        abschnitte.extend(datei_zerlegen(datei, wurzel))

    print(f"{len(dateien)} Dateien, {len(abschnitte)} Abschnitte.", flush=True)

    if argumente.pruefen:
        return pruefen(
            QdrantClient(url=argumente.url), argumente.sammlung, wurzel, dateien, len(abschnitte), stand_lesen(standdatei)
        )

    # Erst hier laden: fastembed braucht Sekunden und wird fuer --pruefen nicht gebraucht.
    from fastembed import TextEmbedding

    modell = TextEmbedding(argumente.modell)
    # Exakt die Namenslogik aus FastEmbedProvider.get_vector_name().
    vektorname = f"fast-{argumente.modell.split('/')[-1].lower()}"
    dimension = modell._get_model_description(argumente.modell).dim
    print(f"Modell {argumente.modell}, Vektor '{vektorname}', {dimension} Dimensionen.", flush=True)

    client = QdrantClient(url=argumente.url)
    if client.collection_exists(argumente.sammlung):
        client.delete_collection(argumente.sammlung)
    client.create_collection(
        collection_name=argumente.sammlung,
        vectors_config={
            vektorname: models.VectorParams(size=dimension, distance=models.Distance.COSINE)
        },
    )

    # Stapelweise einbetten und sofort schreiben. Alles auf einmal zu embedden hat in einem
    # Versuch am 2026-09-03 rund 24 GB Arbeitsspeicher belegt, weil fastembed die Stapel
    # auf die maximale Sequenzlaenge des Modells auffuellt.
    geschrieben = 0
    stapelgroesse = argumente.stapel
    for start in range(0, len(abschnitte), stapelgroesse):
        gruppe = abschnitte[start : start + stapelgroesse]
        vektoren = list(modell.passage_embed([a.dokument for a in gruppe], batch_size=stapelgroesse))
        punkte = [
            models.PointStruct(
                id=abschnitt.punkt_id,
                vector={vektorname: vektor.tolist()},
                payload={
                    "document": abschnitt.dokument,
                    "metadata": {
                        "datei": abschnitt.datei,
                        "abschnitt": abschnitt.ueberschrift,
                        "teil": abschnitt.teil,
                    },
                },
            )
            for abschnitt, vektor in zip(gruppe, vektoren, strict=True)
        ]
        client.upsert(collection_name=argumente.sammlung, points=punkte, wait=True)
        geschrieben += len(punkte)
        print(f"  {geschrieben}/{len(abschnitte)} Abschnitte geschrieben.", flush=True)

    anzahl = client.count(argumente.sammlung, exact=True).count
    print(f"Sammlung '{argumente.sammlung}' enthaelt jetzt {anzahl} Punkte.")
    if anzahl != geschrieben or geschrieben != len(abschnitte):
        print(
            f"FEHLER: {len(abschnitte)} Abschnitte erwartet, {geschrieben} geschrieben, {anzahl} in der Sammlung.",
            file=sys.stderr,
        )
        return 2
    if standdatei is not None:
        stand_schreiben(standdatei, beginn, argumente.sammlung, len(dateien), len(abschnitte), anzahl)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
