"""
Qwen-Fallback fuer die Betragserkennung, wenn der Regex-Extraktor nichts findet.

Laeuft ueber den n8n-Webhook-Gateway auf dem Mac (`QWEN_GATEWAY_URL`), weil die
Qwen-Ports 11435/11436 auf dem Mac bewusst nur auf 127.0.0.1 gebunden sind und
von diesem Container aus (cDocker, anderer Host) nicht erreichbar waeren.

Die Konfidenz ist absichtlich fest und niedrig (unter dem Standard-Sync-
Schwellwert 70): ein Qwen-Vorschlag wird dadurch nie automatisch mitgeschrieben,
solange der Benutzer den Mindestscore nicht bewusst absenkt.
"""

import json
import logging
import os
from decimal import Decimal
from typing import Optional

import requests

from amount_extractor import AmountCandidate, parse_amount

logger = logging.getLogger(__name__)

QWEN_GATEWAY_URL = os.environ.get("QWEN_GATEWAY_URL", "")
QWEN_GATEWAY_KEY = os.environ.get("QWEN_GATEWAY_KEY", "")
QWEN_MODEL = os.environ.get("QWEN_MODEL", "mlx-community/Qwen3-4B-Instruct-2507-4bit")
QWEN_CONFIDENCE = 55
QWEN_TIMEOUT = 60


def qwen_amount_candidate(text: str, currency_default: str = "CHF") -> Optional[AmountCandidate]:
    if not QWEN_GATEWAY_URL or not QWEN_GATEWAY_KEY:
        return None
    if not text or not text.strip():
        return None

    prompt = (
        "Finde den zu zahlenden Gesamtbetrag (Rechnungstotal) in diesem Dokumenttext. "
        "Ignoriere Zwischensummen, MwSt-Betraege, Rabatte, IBAN- und Referenznummern. "
        'Antworte NUR als JSON: {"amount": "1234.56" oder null, "currency": "CHF oder EUR oder null"}.\n\n'
        f"Dokumenttext:\n{text[:3500]}"
    )
    payload = {
        "model": QWEN_MODEL,
        "format": "json",
        "messages": [
            {
                "role": "system",
                "content": "Du liest Rechnungen und findest nur den Gesamtbetrag. Antworte ausschliesslich mit JSON.",
            },
            {"role": "user", "content": prompt},
        ],
    }

    try:
        response = requests.post(
            QWEN_GATEWAY_URL,
            json=payload,
            headers={"X-Gateway-Key": QWEN_GATEWAY_KEY},
            timeout=QWEN_TIMEOUT,
        )
        response.raise_for_status()
        content = (response.json().get("message") or {}).get("content", "")
        parsed = json.loads(content)
    except Exception as exc:
        logger.warning("Qwen-Betragserkennung fehlgeschlagen: %s", exc)
        return None

    raw_amount = parsed.get("amount")
    if raw_amount is None or str(raw_amount).strip().lower() in ("", "null"):
        return None

    amount = parse_amount(str(raw_amount))
    if amount is None or amount < Decimal("1.00") or amount > Decimal("20000.00"):
        return None

    currency_raw = str(parsed.get("currency") or currency_default).upper().replace(".", "")
    currency = "EUR" if currency_raw in ("EUR", "€") else "CHF"

    return AmountCandidate(
        amount=amount,
        currency=currency,
        confidence=QWEN_CONFIDENCE,
        context="Qwen-Vorschlag, kein Regex-Treffer im Text.",
        source="qwen",
    )
