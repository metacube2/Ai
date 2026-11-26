# /// script
# requires-python = ">=3.12,<3.13"
# dependencies = [
#   "fastembed>=0.7.0",
#   "qdrant-client>=1.12.0",
# ]
# ///
"""Lokale Suchseite ueber den Qdrant-Index der Projektdokumentation.

Seit 2026-11-26. Anlass: `Tools/Qdrant/README.md` versprach eine Weboberflaeche unter
`http://127.0.0.1:6333/dashboard`, die bei dieser Installation fehlt (404). Selbst mit der
Qdrant-Oberflaeche liesse sich aber keine Frage in Worten suchen, weil die Frage zuerst mit
demselben Modell eingebettet werden muss wie die Dokumente. Genau das macht dieser Dienst:

* `/`                  eine einzelne HTML-Seite mit Suchfeld (inline CSS/JS, nichts Externes)
* `/api/suche?q=..&n=10&ordner=docs/rag&woerter=1`  Treffer als JSON (`woerter=1`: alle Woerter
  der Frage muessen im Abschnitt vorkommen, Vorgabe aus)
* `/api/stand`         Stand des Index, Punktzahl, Modellzustand; dient auch als Kennung

Das Schema muss zu `index_docs.py` passen (README Abschnitt 5): benannter Vektor
`fast-<modellname in Kleinbuchstaben>`, Cosine, Payload `document` und `metadata`, und die
Frage wird mit `query_embed` eingebettet, nicht mit `passage_embed`.

Der Dienst bindet ausschliesslich an 127.0.0.1, liest nur und schreibt nichts in Qdrant.
Gestartet wird er mit `Tools/Qdrant/Start-Suche.ps1`.
"""

from __future__ import annotations

import argparse
import atexit
import json
import os
import re
import socket
import sys
import threading
import time
from datetime import datetime
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import parse_qs, urlsplit

QDRANT_VERZEICHNIS = Path(os.environ.get("QDRANT_VERZEICHNIS", r"C:\Users\koi\tools\qdrant"))
# Vor dem Import von fastembed setzen, sonst laedt ein Direktstart das Modell (220 MB) nach Temp.
os.environ.setdefault("FASTEMBED_CACHE_PATH", str(QDRANT_VERZEICHNIS / "models"))

from qdrant_client import QdrantClient, models  # noqa: E402

DIENST_KENNUNG = "trafag-doku-suche"
WURZEL = Path(__file__).resolve().parents[2]
MAX_TREFFER = 50
PRUEF_PUFFER_SEKUNDEN = 120


# --------------------------------------------------------------------------------------------
# Zustand
# --------------------------------------------------------------------------------------------


class Suche:
    """Haelt Modell, Qdrant-Verbindung und zwischengespeicherte Angaben zum Index."""

    def __init__(self, url: str, sammlung: str, modell: str) -> None:
        self.url = url
        self.sammlung = sammlung
        self.modellname = modell
        # Exakt die Namenslogik aus FastEmbedProvider.get_vector_name() bzw. index_docs.py.
        self.vektorname = f"fast-{modell.split('/')[-1].lower()}"
        self.client = QdrantClient(url=url, timeout=10)
        self._modell = None
        self._modell_zustand = "laedt"
        self._modell_fehler = ""
        self._modell_sperre = threading.Lock()
        self._ordner: list[dict] = []
        self._ordner_punkte = -1
        self._ordner_sperre = threading.Lock()
        self._pruefung: dict | None = None
        self._pruefung_zeit = 0.0
        self._pruefung_sperre = threading.Lock()
        self.fastembed_version = ""
        self.qdrant_client_version = ""
        try:
            from importlib.metadata import version

            self.fastembed_version = version("fastembed")
            self.qdrant_client_version = version("qdrant-client")
        except Exception:  # noqa: BLE001 - nur Anzeige
            pass

    # -- Modell ------------------------------------------------------------------------------

    def modell_laden(self) -> None:
        """Laedt das Einbettungsmodell; laeuft im Hintergrund, damit die Seite sofort antwortet."""
        try:
            from fastembed import TextEmbedding

            modell = TextEmbedding(self.modellname)
            # Einmal durchlaufen lassen, damit die erste echte Anfrage nicht die Startzeit traegt.
            list(modell.query_embed(["Aufwaermen"]))
            self._modell = modell
            self._modell_zustand = "bereit"
            print(f"Modell {self.modellname} geladen (fastembed {self.fastembed_version}).", flush=True)
        except Exception as fehler:  # noqa: BLE001
            self._modell_zustand = "fehler"
            self._modell_fehler = f"{type(fehler).__name__}: {fehler}"
            print(f"FEHLER beim Laden des Modells: {self._modell_fehler}", file=sys.stderr, flush=True)

    def frage_einbetten(self, frage: str) -> list[float]:
        if self._modell is None:
            if self._modell_zustand == "fehler":
                raise DienstFehler(f"Das Einbettungsmodell konnte nicht geladen werden: {self._modell_fehler}", 500)
            raise DienstFehler("Das Einbettungsmodell wird noch geladen. Bitte in einigen Sekunden erneut suchen.", 503)
        with self._modell_sperre:
            # query_embed, nicht passage_embed: so bettet auch der MCP-Server die Frage ein.
            return list(self._modell.query_embed([frage]))[0].tolist()

    # -- Qdrant ------------------------------------------------------------------------------

    def punktzahl(self) -> int:
        return self.client.count(self.sammlung, exact=True).count

    def ordnerliste(self, punkte: int) -> list[dict]:
        """Ordner mit Anzahl Dateien, aus den Metadaten des Index; neu, sobald sich die Punktzahl aendert."""
        with self._ordner_sperre:
            if punkte == self._ordner_punkte:
                return self._ordner
            dateien: set[str] = set()
            naechster = None
            while True:
                stapel, naechster = self.client.scroll(
                    self.sammlung,
                    limit=2000,
                    offset=naechster,
                    with_payload=["metadata.datei"],
                    with_vectors=False,
                )
                for punkt in stapel:
                    datei = ((punkt.payload or {}).get("metadata") or {}).get("datei")
                    if datei:
                        dateien.add(datei)
                if naechster is None:
                    break
            zaehler: dict[str, int] = {}
            wurzel = 0
            for datei in dateien:
                teile = datei.split("/")
                if len(teile) == 1:
                    wurzel += 1
                    continue
                for tiefe in range(1, min(len(teile) - 1, 2) + 1):
                    ordner = "/".join(teile[:tiefe])
                    zaehler[ordner] = zaehler.get(ordner, 0) + 1
            liste = [{"ordner": "", "titel": "alle Ordner", "dateien": len(dateien)}]
            if wurzel:
                liste.append({"ordner": "/", "titel": "nur Wurzel (router.md, baum.md ...)", "dateien": wurzel})
            for ordner in sorted(zaehler, key=str.lower):
                # Unterordner nur, wenn sie mehr als eine Datei haben, sonst wird die Liste unuebersichtlich.
                if "/" in ordner and zaehler[ordner] < 2:
                    continue
                liste.append({"ordner": ordner, "titel": ordner, "dateien": zaehler[ordner]})
            self._ordner = liste
            self._ordner_punkte = punkte
            return liste

    def suchen(self, frage: str, anzahl: int, ordner: str, alle_woerter: bool = False) -> list[dict]:
        vektor = self.frage_einbetten(frage)
        bedingungen: list = []
        grenze = anzahl
        if ordner == "/":
            # Dateien in der Wurzel lassen sich in Qdrant ohne Index nicht filtern: mehr holen, hier sieben.
            grenze = 1000
        elif ordner:
            # Ohne Volltextindex prueft Qdrant MatchText als Teilzeichenkette; der Praefix wird unten exakt geprueft.
            bedingungen.append(models.FieldCondition(key="metadata.datei", match=models.MatchText(text=ordner + "/")))
            grenze = min(1000, anzahl * 5)
        if alle_woerter:
            # Zuschaltbar, Vorgabe aus: Die reine Vektorsuche (wie qdrant-find) uebersieht seltene
            # Fachwoerter wie "Handbestellwesen" (2026-11-26 gemessen). Mit diesem Schalter muss jedes
            # Wort ab drei Zeichen im Abschnitt vorkommen; die Reihenfolge bleibt die semantische.
            # MatchText ohne Volltextindex ist eine Teilzeichenkette mit Gross-/Kleinschreibung,
            # deshalb je Wort die ueblichen Schreibweisen als "oder".
            for wort in woerter_der_frage(frage):
                varianten = sorted({wort, wort.lower(), wort.upper(), wort[:1].upper() + wort[1:].lower()})
                bedingungen.append(
                    models.Filter(
                        should=[
                            models.FieldCondition(key="document", match=models.MatchText(text=v)) for v in varianten
                        ]
                    )
                )
        filter_ = models.Filter(must=bedingungen) if bedingungen else None
        antwort = self.client.query_points(
            self.sammlung,
            query=vektor,
            using=self.vektorname,
            limit=grenze,
            query_filter=filter_,
            with_payload=True,
            with_vectors=False,
        )
        treffer: list[dict] = []
        for punkt in antwort.points:
            payload = punkt.payload or {}
            meta = payload.get("metadata") or {}
            datei = str(meta.get("datei") or "")
            if ordner == "/" and "/" in datei:
                continue
            if ordner and ordner != "/" and not datei.startswith(ordner + "/"):
                continue
            treffer.append(
                {
                    "rang": len(treffer) + 1,
                    "score": round(float(punkt.score), 4),
                    "datei": datei,
                    "pfad": str(WURZEL / Path(datei)) if datei else "",
                    "abschnitt": str(meta.get("abschnitt") or ""),
                    "teil": meta.get("teil"),
                    "text": text_ohne_kopf(str(payload.get("document") or "")),
                }
            )
            if len(treffer) >= anzahl:
                break
        return treffer

    # -- Stand -------------------------------------------------------------------------------

    def stand(self) -> dict:
        ergebnis: dict = {
            "dienst": DIENST_KENNUNG,
            "pid": os.getpid(),
            "sammlung": self.sammlung,
            "qdrant_url": self.url,
            "wurzel": str(WURZEL),
            "modell": self.modellname,
            "modell_zustand": self._modell_zustand,
            "modell_fehler": self._modell_fehler,
            "fastembed": self.fastembed_version,
            "qdrant_client": self.qdrant_client_version,
            "qdrant_erreichbar": False,
            "qdrant_fehler": "",
            "punkte": None,
            "ordner": [],
            "index_stand": index_stand_lesen(self.sammlung),
            "pruefung": None,
        }
        try:
            if not self.client.collection_exists(self.sammlung):
                ergebnis["qdrant_erreichbar"] = True
                ergebnis["qdrant_fehler"] = (
                    f"Qdrant laeuft, aber die Sammlung '{self.sammlung}' fehlt. "
                    "Neu aufbauen mit .\\Tools\\Qdrant\\Reindex-Docs.ps1"
                )
                return ergebnis
            punkte = self.punktzahl()
            ergebnis["qdrant_erreichbar"] = True
            ergebnis["punkte"] = punkte
            ergebnis["ordner"] = self.ordnerliste(punkte)
        except Exception as fehler:  # noqa: BLE001
            ergebnis["qdrant_fehler"] = qdrant_meldung(self.url, fehler)
            return ergebnis
        ergebnis["pruefung"] = self.dateien_pruefen(ergebnis["index_stand"], ergebnis["punkte"])
        return ergebnis

    def _dateien_zaehlen(self) -> None:
        """Sucht geaenderte Markdown-Dateien; dauert einige Sekunden, weil das ganze Repo durchlaufen wird."""
        pruefung: dict = {"geaendert": None, "beispiele": [], "fehler": ""}
        try:
            stand = index_stand_lesen(self.sammlung)
            zeit = datetime.fromisoformat(str((stand or {}).get("zeit")))
            # Kein __pycache__ im Repo-Ordner hinterlassen.
            sys.dont_write_bytecode = True
            import index_docs  # liegt neben diesem Skript, gleiche Auswahlregeln wie der Indexer

            dateien = index_docs.markdown_dateien(WURZEL)
            neu = index_docs.geaendert_seit(dateien, zeit)
            pruefung["geaendert"] = len(neu)
            pruefung["beispiele"] = [d.relative_to(WURZEL).as_posix() for d in neu[:8]]
            pruefung["dateien_jetzt"] = len(dateien)
        except Exception as fehler:  # noqa: BLE001
            pruefung["fehler"] = f"{type(fehler).__name__}: {fehler}"
        self._pruefung = pruefung
        self._pruefung_zeit = time.monotonic()

    def dateien_pruefen(self, stand: dict | None, punkte: int | None) -> dict:
        """Wie `index_docs.py --pruefen`, aber ohne Abschnittszaehlung: geaenderte Dateien seit dem Lauf.

        Das Ergebnis wird zwei Minuten gehalten und danach im Hintergrund erneuert, damit die Seite
        nicht bei jedem Aufruf auf den Durchlauf durchs Repo warten muss.
        """
        if self._pruefung is None:
            with self._pruefung_sperre:
                if self._pruefung is None:
                    self._dateien_zaehlen()
        elif time.monotonic() - self._pruefung_zeit >= PRUEF_PUFFER_SEKUNDEN and self._pruefung_sperre.acquire(False):

            def erneuern() -> None:
                try:
                    self._dateien_zaehlen()
                finally:
                    self._pruefung_sperre.release()

            threading.Thread(target=erneuern, name="pruefung", daemon=True).start()
        pruefung = dict(self._pruefung or {})
        gruende: list[str] = []
        if stand is None:
            gruende.append("Kein Stand eines vollstaendigen Indexlaufs gefunden.")
        elif punkte is not None and stand.get("punkte") != punkte:
            gruende.append(f"Die Sammlung hat {punkte} Punkte, der letzte Lauf schrieb {stand.get('punkte')}.")
        if pruefung.get("geaendert"):
            gruende.append(f"{pruefung['geaendert']} Markdown-Dateien seit dem letzten Lauf geaendert.")
        pruefung["veraltet"] = bool(gruende)
        pruefung["gruende"] = gruende
        return pruefung


class DienstFehler(Exception):
    def __init__(self, meldung: str, status: int) -> None:
        super().__init__(meldung)
        self.status = status


# Fuellwoerter zaehlen beim Schalter "alle Woerter" nicht; "den" steckt sonst in fast jedem Abschnitt.
FUELLWOERTER = {
    "und", "oder", "der", "die", "das", "den", "dem", "des", "ein", "eine", "einen", "einem", "einer",
    "mit", "von", "vom", "fuer", "für", "auf", "aus", "bei", "ist", "sind", "wie", "was", "wer", "wird",
    "nicht", "noch", "nach", "zum", "zur", "the", "and", "for",
}


def woerter_der_frage(frage: str) -> list[str]:
    """Woerter ab drei Zeichen ohne Fuellwoerter, ohne Satzzeichen am Rand, ohne Doppelte."""
    woerter: list[str] = []
    for roh in frage.split():
        wort = roh.strip(".,;:!?\"'()[]{}«»„“”")
        if len(wort) >= 3 and wort.lower() not in FUELLWOERTER and wort.lower() not in {w.lower() for w in woerter}:
            woerter.append(wort)
    return woerter[:12]


def text_ohne_kopf(dokument: str) -> str:
    """`document` beginnt mit `Datei: ...` und `Abschnitt: ...`; das steht auf der Seite schon getrennt."""
    zeilen = dokument.split("\n")
    i = 0
    while i < len(zeilen) and (zeilen[i].startswith("Datei: ") or zeilen[i].startswith("Abschnitt: ")):
        i += 1
    while i < len(zeilen) and not zeilen[i].strip():
        i += 1
    return "\n".join(zeilen[i:])


def index_stand_lesen(sammlung: str) -> dict | None:
    pfad = QDRANT_VERZEICHNIS / f"reindex_stand_{sammlung}.json"
    try:
        daten = json.loads(pfad.read_text(encoding="utf-8"))
        daten["datei"] = str(pfad)
        return daten
    except (OSError, ValueError):
        return None


def qdrant_meldung(url: str, fehler: Exception) -> str:
    text = f"{type(fehler).__name__}: {fehler}"
    if re.search(r"connect|refused|verweigert|timed out|Timeout|10061", text, re.IGNORECASE):
        return (
            f"Qdrant antwortet nicht unter {url}. Starten mit .\\Tools\\Qdrant\\Start-Qdrant.ps1 "
            "(oder .\\Tools\\Qdrant\\Start-Suche.ps1, das Qdrant bei Bedarf mitstartet) und die Seite neu laden."
        )
    return f"Qdrant-Fehler: {text}"


# --------------------------------------------------------------------------------------------
# HTTP
# --------------------------------------------------------------------------------------------


class Server(ThreadingHTTPServer):
    # Unter Windows erlaubt SO_REUSEADDR einer zweiten Instanz, denselben Port still mitzubelegen.
    # Dann funktionieren weder "naechster freier Port" noch "laeuft schon". Deshalb exklusiv binden.
    allow_reuse_address = False
    daemon_threads = True

    def server_bind(self) -> None:
        if hasattr(socket, "SO_EXCLUSIVEADDRUSE"):
            self.socket.setsockopt(socket.SOL_SOCKET, socket.SO_EXCLUSIVEADDRUSE, 1)
        super().server_bind()


def handler_bauen(suche: Suche, port: int):
    erlaubte_hosts = {f"127.0.0.1:{port}", f"localhost:{port}"}

    class Handler(BaseHTTPRequestHandler):
        server_version = "TrafagDokuSuche/1.0"

        def log_message(self, format: str, *args) -> None:  # noqa: A002 - Signatur der Basisklasse
            sys.stderr.write(f"{datetime.now():%Y-%m-%d %H:%M:%S} {self.address_string()} {format % args}\n")

        def senden(self, status: int, inhalt: bytes, art: str) -> None:
            self.send_response(status)
            self.send_header("Content-Type", art)
            self.send_header("Content-Length", str(len(inhalt)))
            self.send_header("Cache-Control", "no-store")
            self.send_header("X-Content-Type-Options", "nosniff")
            self.send_header("Referrer-Policy", "no-referrer")
            self.end_headers()
            self.wfile.write(inhalt)

        def json_senden(self, status: int, daten: dict) -> None:
            self.senden(status, json.dumps(daten, ensure_ascii=False).encode("utf-8"), "application/json; charset=utf-8")

        def do_GET(self) -> None:  # noqa: N802 - Name der Basisklasse
            # Nur Aufrufe ueber 127.0.0.1/localhost; schuetzt gegen DNS-Rebinding fremder Seiten.
            if self.headers.get("Host", "") not in erlaubte_hosts:
                self.senden(403, "Nur ueber 127.0.0.1 erreichbar.".encode("utf-8"), "text/plain; charset=utf-8")
                return
            teile = urlsplit(self.path)
            parameter = parse_qs(teile.query)
            if teile.path in ("/", "/index.html"):
                self.senden(200, SEITE.encode("utf-8"), "text/html; charset=utf-8")
            elif teile.path == "/api/stand":
                self.json_senden(200, suche.stand())
            elif teile.path == "/api/suche":
                self.suche_beantworten(parameter)
            elif teile.path == "/favicon.ico":
                self.senden(204, b"", "image/x-icon")
            else:
                self.json_senden(404, {"fehler": f"Unbekannter Pfad {teile.path}"})

        def suche_beantworten(self, parameter: dict[str, list[str]]) -> None:
            frage = (parameter.get("q") or [""])[0].strip()
            ordner = (parameter.get("ordner") or [""])[0].strip().strip("/")
            if (parameter.get("ordner") or [""])[0].strip() == "/":
                ordner = "/"
            try:
                anzahl = int((parameter.get("n") or ["10"])[0])
            except ValueError:
                anzahl = 10
            anzahl = max(1, min(MAX_TREFFER, anzahl))
            alle_woerter = (parameter.get("woerter") or ["0"])[0].strip().lower() in ("1", "ja", "true", "on")
            if not frage:
                self.json_senden(400, {"fehler": "Bitte eine Frage eingeben (Parameter q)."})
                return
            if len(frage) > 1000:
                frage = frage[:1000]
            beginn = time.perf_counter()
            try:
                treffer = suche.suchen(frage, anzahl, ordner, alle_woerter)
            except DienstFehler as fehler:
                self.json_senden(fehler.status, {"fehler": str(fehler)})
                return
            except Exception as fehler:  # noqa: BLE001
                self.json_senden(503, {"fehler": qdrant_meldung(suche.url, fehler)})
                return
            self.json_senden(
                200,
                {
                    "frage": frage,
                    "n": anzahl,
                    "ordner": ordner,
                    "alle_woerter": alle_woerter,
                    "dauer_ms": round((time.perf_counter() - beginn) * 1000),
                    "treffer": treffer,
                },
            )

    return Handler


def binden(start: int, versuche: int) -> tuple[Server, int]:
    letzter: OSError | None = None
    for port in range(start, start + versuche):
        try:
            # Handler wird erst nach dem Binden gesetzt, weil er den tatsaechlichen Port kennen muss.
            return Server(("127.0.0.1", port), BaseHTTPRequestHandler), port
        except OSError as fehler:
            letzter = fehler
    raise SystemExit(f"Kein freier Port zwischen {start} und {start + versuche - 1}: {letzter}")


def main() -> int:
    parser = argparse.ArgumentParser(description="Lokale Suchseite ueber den Qdrant-Index der Doku.")
    parser.add_argument("--port", type=int, default=int(os.environ.get("SUCHE_PORT", "6340")))
    parser.add_argument("--url", default=os.environ.get("QDRANT_URL", "http://127.0.0.1:6333"))
    parser.add_argument("--sammlung", default=os.environ.get("COLLECTION_NAME", "trafag_docs"))
    parser.add_argument(
        "--modell",
        default=os.environ.get("EMBEDDING_MODEL", "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2"),
    )
    parser.add_argument(
        "--zustand",
        default=str(QDRANT_VERZEICHNIS / "suche_dienst.json"),
        help="Datei, in die Port und PID geschrieben werden (liest Start-Suche.ps1).",
    )
    argumente = parser.parse_args()

    server, port = binden(argumente.port, 10)
    suche = Suche(argumente.url, argumente.sammlung, argumente.modell)
    server.RequestHandlerClass = handler_bauen(suche, port)

    zustand = Path(argumente.zustand)
    adresse = f"http://127.0.0.1:{port}/"
    try:
        zustand.write_text(
            json.dumps(
                {
                    "dienst": DIENST_KENNUNG,
                    "pid": os.getpid(),
                    "port": port,
                    "adresse": adresse,
                    "gestartet": datetime.now().isoformat(timespec="seconds"),
                },
                indent=2,
            ),
            encoding="utf-8",
        )

        def zustand_loeschen() -> None:
            try:
                if json.loads(zustand.read_text(encoding="utf-8")).get("pid") == os.getpid():
                    zustand.unlink()
            except (OSError, ValueError):
                pass

        atexit.register(zustand_loeschen)
    except OSError as fehler:
        print(f"Zustandsdatei {zustand} nicht schreibbar: {fehler}", file=sys.stderr, flush=True)

    threading.Thread(target=suche.modell_laden, name="modell", daemon=True).start()
    # Dateipruefung vorwaermen, damit der erste Seitenaufruf nicht Sekunden wartet.
    threading.Thread(target=suche.dateien_pruefen, args=(None, None), name="pruefung", daemon=True).start()
    print(f"Doku-Suche laeuft unter {adresse} (Qdrant {argumente.url}, Sammlung {argumente.sammlung}).", flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()
    return 0


# --------------------------------------------------------------------------------------------
# Seite
# --------------------------------------------------------------------------------------------

SEITE = r"""<!doctype html>
<html lang="de-CH">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Doku-Suche</title>
<style>
:root {
  --bg: #f6f7f9; --fl: #ffffff; --tx: #1c2430; --tx2: #5a6575; --rand: #d9dee6;
  --akz: #1f5fbf; --akz-bg: #e8f0fc; --ok: #1d7a3e; --ok-bg: #e5f4ea;
  --warn: #8a5a00; --warn-bg: #fdf3dc; --err: #a32020; --err-bg: #fbe7e7; --mark: #fff0a8;
}
@media (prefers-color-scheme: dark) {
  :root {
    --bg: #14181e; --fl: #1d232b; --tx: #e3e8ef; --tx2: #9aa6b5; --rand: #323b47;
    --akz: #7fb0ff; --akz-bg: #1e2d45; --ok: #7fd49a; --ok-bg: #17301f;
    --warn: #f0c060; --warn-bg: #3a2f14; --err: #ff9a9a; --err-bg: #3d1c1c; --mark: #5a4b10;
  }
}
* { box-sizing: border-box; }
body { margin: 0; background: var(--bg); color: var(--tx);
  font: 15px/1.5 "Segoe UI", system-ui, -apple-system, sans-serif; }
main { max-width: 1000px; margin: 0 auto; padding: 18px 16px 48px; }
h1 { font-size: 20px; margin: 0 0 4px; }
.unter { color: var(--tx2); font-size: 13px; margin: 0 0 14px; }
.stand { border: 1px solid var(--rand); background: var(--fl); border-radius: 8px;
  padding: 10px 12px; font-size: 13px; margin-bottom: 14px; }
.stand.ok { border-color: var(--ok); background: var(--ok-bg); }
.stand.warn { border-color: var(--warn); background: var(--warn-bg); }
.stand.err { border-color: var(--err); background: var(--err-bg); }
.stand b.ok { color: var(--ok); } .stand b.warn { color: var(--warn); } .stand b.err { color: var(--err); }
.stand ul { margin: 4px 0 0 18px; padding: 0; }
form { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; margin-bottom: 6px; }
input[type=search] { flex: 1 1 380px; min-width: 0; font: inherit; font-size: 16px; padding: 9px 11px;
  border: 1px solid var(--rand); border-radius: 6px; background: var(--fl); color: var(--tx); }
select, button { font: inherit; padding: 8px 10px; border: 1px solid var(--rand); border-radius: 6px;
  background: var(--fl); color: var(--tx); }
button.haupt { background: var(--akz); border-color: var(--akz); color: #fff; font-weight: 600; cursor: pointer; }
@media (prefers-color-scheme: dark) { button.haupt { color: #0b1526; } }
label { font-size: 13px; color: var(--tx2); }
.info { color: var(--tx2); font-size: 13px; margin: 6px 0 12px; min-height: 1.5em; }
.fehler { color: var(--err); background: var(--err-bg); border: 1px solid var(--err); border-radius: 6px;
  padding: 8px 10px; margin: 8px 0; }
.treffer { background: var(--fl); border: 1px solid var(--rand); border-radius: 8px; padding: 10px 12px; margin: 10px 0; }
.kopf { display: flex; flex-wrap: wrap; gap: 6px 10px; align-items: baseline; }
.rang { color: var(--tx2); font-size: 13px; min-width: 1.6em; }
.score { font-variant-numeric: tabular-nums; font-size: 12px; color: var(--akz); background: var(--akz-bg);
  border-radius: 10px; padding: 1px 8px; }
.datei { font-family: Consolas, "Cascadia Mono", monospace; font-size: 13.5px; color: var(--akz);
  background: none; border: 0; padding: 0; cursor: pointer; text-align: left; word-break: break-all; }
.datei:hover { text-decoration: underline; }
.kopiert { font-size: 12px; color: var(--ok); }
.abschnitt { font-weight: 600; margin: 3px 0 4px; word-break: break-word; }
.text { white-space: pre-wrap; word-break: break-word; font-size: 13.5px; color: var(--tx); margin: 0;
  font-family: inherit; }
.mehr { background: none; border: 0; color: var(--akz); padding: 2px 0; cursor: pointer; font-size: 13px; }
mark { background: var(--mark); color: inherit; border-radius: 2px; }
.fuss { color: var(--tx2); font-size: 12px; margin-top: 24px; }
code { font-family: Consolas, "Cascadia Mono", monospace; font-size: 12.5px; }
</style>
</head>
<body>
<main>
<h1>Doku-Suche TrafagSalesExporter</h1>
<p class="unter">Semantische Suche über alle Markdown-Dateien (wie <code>qdrant-find</code>). Treffer sind Hinweise, keine Belege: die gefundene Datei danach vollständig lesen.</p>
<div id="stand" class="stand">Stand des Index wird geladen …</div>
<form id="formular" autocomplete="off">
  <input id="frage" type="search" placeholder="Frage in Worten, z. B. Wie wird produktiv deployed?" autofocus>
  <label>Ordner <select id="ordner"><option value="">alle Ordner</option></select></label>
  <label title="Jedes Wort ab drei Zeichen (ohne Füllwörter wie „den“, „und“) muss im Abschnitt vorkommen; hilft bei seltenen Fachwörtern, die die reine Vektorsuche übersieht."><input id="woerter" type="checkbox"> alle Wörter müssen vorkommen</label>
  <label>Treffer <select id="anzahl"><option>5</option><option selected>10</option><option>20</option><option>50</option></select></label>
  <button class="haupt" type="submit">Suchen</button>
</form>
<div id="info" class="info"></div>
<div id="liste"></div>
<p class="fuss">Klick auf den Dateinamen kopiert den vollständigen Pfad (der Browser darf lokale Dateien von hier aus nicht öffnen). Findet die Suche ein seltenes Fachwort nicht, den Haken „alle Wörter müssen vorkommen“ setzen. Schnittstelle: <code>/api/suche?q=…&amp;n=10&amp;ordner=docs/rag&amp;woerter=1</code>. Index neu aufbauen: <code>.\Tools\Qdrant\Reindex-Docs.ps1 -WennVeraltet</code></p>
</main>
<script>
"use strict";
const $ = (id) => document.getElementById(id);
const zahl = (n) => (n === null || n === undefined) ? "?" : Number(n).toLocaleString("de-CH");
let laufend = null;
const FUELL = new Set(["und","oder","der","die","das","den","dem","des","ein","eine","einen","einem","einer","mit","von","vom","fuer","für","auf","aus","bei","ist","sind","wie","was","wer","wird","nicht","noch","nach","zum","zur","the","and","for"]);

function el(tag, klasse, text) {
  const e = document.createElement(tag);
  if (klasse) e.className = klasse;
  if (text !== undefined) e.textContent = text;
  return e;
}

async function standLaden() {
  const box = $("stand");
  try {
    const r = await fetch("/api/stand", { cache: "no-store" });
    const s = await r.json();
    box.textContent = "";
    if (!s.qdrant_erreichbar || s.qdrant_fehler) {
      box.className = "stand err";
      const b = el("b", "err", "Qdrant nicht verfügbar. ");
      box.append(b, s.qdrant_fehler || "Unbekannter Fehler.");
      return s;
    }
    const st = s.index_stand;
    const p = s.pruefung || {};
    box.className = "stand " + (p.veraltet ? "warn" : "ok");
    box.append(el("b", p.veraltet ? "warn" : "ok", p.veraltet ? "Index möglicherweise veraltet. " : "Index aktuell. "));
    let t = "Sammlung " + s.sammlung + ": " + zahl(s.punkte) + " Punkte";
    if (st) {
      const z = new Date(st.zeit);
      t += " · letzter vollständiger Lauf " + z.toLocaleString("de-CH", { dateStyle: "medium", timeStyle: "short" })
        + " (" + zahl(st.dateien) + " Dateien, " + zahl(st.abschnitte) + " Abschnitte)";
    }
    if (s.modell_zustand !== "bereit") {
      t += " · Modell: " + (s.modell_zustand === "laedt" ? "wird geladen …" : "Fehler " + s.modell_fehler);
    }
    box.append(t);
    if (p.veraltet) {
      const ul = el("ul");
      for (const g of p.gruende || []) ul.append(el("li", "", g));
      if (p.beispiele && p.beispiele.length) ul.append(el("li", "", "z. B. " + p.beispiele.join(", ")));
      ul.append(el("li", "", "Neu aufbauen: .\\Tools\\Qdrant\\Reindex-Docs.ps1 -WennVeraltet"));
      box.append(ul);
    }
    const sel = $("ordner");
    const gewaehlt = sel.value || new URLSearchParams(location.search).get("ordner") || "";
    if (s.ordner && s.ordner.length) {
      sel.textContent = "";
      for (const o of s.ordner) {
        const opt = el("option", "", o.titel + " (" + zahl(o.dateien) + ")");
        opt.value = o.ordner;
        sel.append(opt);
      }
      sel.value = gewaehlt;
      if (sel.value !== gewaehlt) sel.value = "";
    }
    if (s.modell_zustand === "laedt") setTimeout(standLaden, 3000);
    return s;
  } catch (e) {
    box.className = "stand err";
    box.textContent = "Der Suchdienst antwortet nicht. Neu starten mit .\\Tools\\Qdrant\\Start-Suche.ps1";
    return null;
  }
}

function markieren(ziel, text, woerter) {
  if (!woerter.length) { ziel.textContent = text; return; }
  const muster = new RegExp("(" + woerter.map((w) => w.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")).join("|") + ")", "gi");
  let pos = 0;
  for (const m of text.matchAll(muster)) {
    if (m.index > pos) ziel.append(document.createTextNode(text.slice(pos, m.index)));
    ziel.append(el("mark", "", m[0]));
    pos = m.index + m[0].length;
  }
  if (pos < text.length) ziel.append(document.createTextNode(text.slice(pos)));
}

async function kopieren(text, knopf) {
  let ok = false;
  try { await navigator.clipboard.writeText(text); ok = true; } catch (e) {
    const ta = el("textarea"); ta.value = text; document.body.append(ta); ta.select();
    try { ok = document.execCommand("copy"); } catch (e2) { ok = false; }
    ta.remove();
  }
  const hinweis = el("span", "kopiert", ok ? "Pfad kopiert" : "Kopieren nicht möglich: " + text);
  knopf.after(hinweis);
  setTimeout(() => hinweis.remove(), 2500);
}

function trefferZeigen(daten, woerter) {
  const liste = $("liste");
  liste.textContent = "";
  for (const t of daten.treffer) {
    const karte = el("div", "treffer");
    const kopf = el("div", "kopf");
    kopf.append(el("span", "rang", t.rang + "."));
    kopf.append(el("span", "score", "Score " + t.score.toFixed(3)));
    const datei = el("button", "datei", t.datei);
    datei.type = "button";
    datei.title = "Pfad kopieren: " + t.pfad;
    datei.addEventListener("click", () => kopieren(t.pfad, datei));
    kopf.append(datei);
    karte.append(kopf);
    if (t.abschnitt) karte.append(el("div", "abschnitt", t.abschnitt));
    const kurz = t.text.length > 600;
    const text = el("pre", "text");
    markieren(text, kurz ? t.text.slice(0, 600).trimEnd() + " …" : t.text, woerter);
    karte.append(text);
    if (kurz) {
      const mehr = el("button", "mehr", "ganzen Abschnitt zeigen");
      mehr.type = "button";
      let offen = false;
      mehr.addEventListener("click", () => {
        offen = !offen;
        text.textContent = "";
        markieren(text, offen ? t.text : t.text.slice(0, 600).trimEnd() + " …", woerter);
        mehr.textContent = offen ? "kürzen" : "ganzen Abschnitt zeigen";
      });
      karte.append(mehr);
    }
    liste.append(karte);
  }
}

async function suchen() {
  const frage = $("frage").value.trim();
  const n = $("anzahl").value;
  const ordner = $("ordner").value;
  const woerter = $("woerter").checked;
  const info = $("info");
  const url = new URL(location.href);
  url.search = "";
  if (frage) url.searchParams.set("q", frage);
  if (n !== "10") url.searchParams.set("n", n);
  if (ordner) url.searchParams.set("ordner", ordner);
  if (woerter) url.searchParams.set("woerter", "1");
  history.replaceState(null, "", url);
  if (!frage) { info.textContent = ""; $("liste").textContent = ""; return; }
  if (laufend) laufend.abort();
  laufend = new AbortController();
  info.textContent = "Suche läuft …";
  try {
    const q = new URLSearchParams({ q: frage, n: n });
    if (ordner) q.set("ordner", ordner);
    if (woerter) q.set("woerter", "1");
    const r = await fetch("/api/suche?" + q.toString(), { signal: laufend.signal, cache: "no-store" });
    const d = await r.json();
    if (!r.ok || d.fehler) {
      info.textContent = "";
      $("liste").textContent = "";
      $("liste").append(el("div", "fehler", d.fehler || ("Fehler " + r.status)));
      if (r.status === 503) standLaden();
      return;
    }
    info.textContent = d.treffer.length + " Treffer in " + d.dauer_ms + " ms"
      + (d.ordner ? " · Ordner " + (d.ordner === "/" ? "Wurzel" : d.ordner) : "")
      + (d.alle_woerter ? " · nur Abschnitte mit allen Wörtern" : "");
    if (!d.treffer.length && d.alle_woerter) info.textContent += " – ohne Haken bei „alle Wörter“ erneut versuchen";
    const markWoerter = frage.split(/\s+/).filter((w) => w.length >= 3 && !FUELL.has(w.toLowerCase()));
    trefferZeigen(d, markWoerter);
  } catch (e) {
    if (e.name === "AbortError") return;
    info.textContent = "";
    $("liste").textContent = "";
    $("liste").append(el("div", "fehler", "Der Suchdienst antwortet nicht. Neu starten mit .\\Tools\\Qdrant\\Start-Suche.ps1"));
  }
}

$("formular").addEventListener("submit", (e) => { e.preventDefault(); suchen(); });
$("ordner").addEventListener("change", () => { if ($("frage").value.trim()) suchen(); });
$("anzahl").addEventListener("change", () => { if ($("frage").value.trim()) suchen(); });
$("woerter").addEventListener("change", () => { if ($("frage").value.trim()) suchen(); });

(async () => {
  const p = new URLSearchParams(location.search);
  if (p.get("n")) $("anzahl").value = p.get("n");
  if (p.get("woerter") === "1") $("woerter").checked = true;
  await standLaden();
  if (p.get("ordner") !== null) $("ordner").value = p.get("ordner");
  if (p.get("q")) { $("frage").value = p.get("q"); suchen(); }
})();
</script>
</body>
</html>
"""


if __name__ == "__main__":
    raise SystemExit(main())
