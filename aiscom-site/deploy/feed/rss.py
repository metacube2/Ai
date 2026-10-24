"""Gemeinsame Bausteine fuer die RSS-Feeds (oeffentlich /feed.xml, Tester /tester/feed.xml).

Ein Feed-Eintrag ist eine Veroeffentlichung: App, Plattform, Version, Build, Datum, Link, Text. Die Eintraege werden in
einer Verlaufsdatei (JSON, im Repo) gesammelt; neue Versionen kommen oben dazu, alte bleiben stehen. Kennung (guid) ist
„<app>-<plattform>-<version>-<build>“, so meldet ein Feed-Leser jede Version genau einmal."""
import json
from datetime import datetime, timezone
from email.utils import format_datetime
from html import escape
from pathlib import Path


def verlauf_laden(p: Path) -> list:
    return json.loads(p.read_text(encoding="utf-8")) if p.exists() else []


def verlauf_ergaenzen(p: Path, neu: list) -> list:
    """neu: Liste von dicts mit guid, titel, link, text, datum (ISO). Bekannte guids werden nicht doppelt aufgenommen."""
    alt = verlauf_laden(p)
    bekannt = {e["guid"] for e in alt}
    zugang = [e for e in neu if e["guid"] not in bekannt]
    alle = sorted(alt + zugang, key=lambda e: e["datum"], reverse=True)
    p.write_text(json.dumps(alle, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")
    return alle


def feed(titel: str, link: str, selbst: str, beschreibung: str, eintraege: list, anzahl: int = 40) -> str:
    jetzt = format_datetime(datetime.now(timezone.utc))
    items = []
    for e in eintraege[:anzahl]:
        d = format_datetime(datetime.fromisoformat(e["datum"]))
        items.append(f"""  <item>
    <title>{escape(e['titel'])}</title>
    <link>{escape(e['link'])}</link>
    <guid isPermaLink="false">{escape(e['guid'])}</guid>
    <pubDate>{d}</pubDate>
    <description>{escape(e['text'])}</description>
  </item>""")
    return f"""<?xml version="1.0" encoding="UTF-8"?>
<rss version="2.0" xmlns:atom="http://www.w3.org/2005/Atom">
<channel>
  <title>{escape(titel)}</title>
  <link>{escape(link)}</link>
  <atom:link href="{escape(selbst)}" rel="self" type="application/rss+xml"/>
  <description>{escape(beschreibung)}</description>
  <language>de-ch</language>
  <lastBuildDate>{jetzt}</lastBuildDate>
{chr(10).join(items)}
</channel>
</rss>
"""
