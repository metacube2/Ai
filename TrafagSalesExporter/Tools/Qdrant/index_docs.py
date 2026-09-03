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
"""

from __future__ import annotations

import argparse
import os
import re
import sys
import uuid
from dataclasses import dataclass
from pathlib import Path

from fastembed import TextEmbedding
from qdrant_client import QdrantClient, models

# Verzeichnisse, die keine Projektdokumentation enthalten, sondern Build-Ausgaben,
# Exportpakete oder temporaere Sondierungen.
AUSGESCHLOSSENE_ORDNER = {
    ".git",
    ".vs",
    ".vscode",
    "node_modules",
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
    stapel: list[str] = []
    aktuelle_ueberschrift = ""
    puffer: list[str] = []
    rohabschnitte: list[tuple[str, str]] = []

    def puffer_abgeben() -> None:
        text = "\n".join(puffer).strip()
        if text:
            rohabschnitte.append((aktuelle_ueberschrift, text))
        puffer.clear()

    for zeile in inhalt.splitlines():
        treffer = ueberschrift_muster.match(zeile)
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
    argumente = parser.parse_args()

    wurzel = Path(argumente.wurzel).resolve()
    dateien = markdown_dateien(wurzel)
    if not dateien:
        print(f"Keine Markdown-Dateien unter {wurzel} gefunden.", file=sys.stderr)
        return 1

    abschnitte: list[Abschnitt] = []
    for datei in dateien:
        abschnitte.extend(datei_zerlegen(datei, wurzel))

    print(f"{len(dateien)} Dateien, {len(abschnitte)} Abschnitte.")

    modell = TextEmbedding(argumente.modell)
    # Exakt die Namenslogik aus FastEmbedProvider.get_vector_name().
    vektorname = f"fast-{argumente.modell.split('/')[-1].lower()}"
    dimension = modell._get_model_description(argumente.modell).dim
    print(f"Modell {argumente.modell}, Vektor '{vektorname}', {dimension} Dimensionen.")

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
    return 0 if anzahl == geschrieben else 2


if __name__ == "__main__":
    raise SystemExit(main())
