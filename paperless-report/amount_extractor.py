"""
Amount extraction helpers for Paperless OCR content.

The extractor is intentionally conservative: it prefers amounts near invoice
total keywords and ignores common tax-rate percentages.
"""

import re
from dataclasses import dataclass
from decimal import Decimal, InvalidOperation
from typing import Iterable, Optional


TOTAL_KEYWORDS = (
    "gesamtbetrag",
    "totalbetrag",
    "rechnungstotal",
    "rechnungsbetrag",
    "betrag zahlbar",
    "zahlbetrag",
    "zu bezahlen",
    "zu zahlen",
    "total chf",
    "total eur",
    "total",
    "gesamt",
    "summe",
)

NEGATIVE_KEYWORDS = (
    "mwst",
    "mehrwertsteuer",
    "ust",
    "vat",
    "subtotal",
    "zwischensumme",
    "rabatt",
    "skonto",
    "preis pro",
    "einzelpreis",
)

AMOUNT_PATTERN = re.compile(
    r"""
    (?:
        (?P<prefix>CHF|SFr\.?|Fr\.?|EUR|€)\s*
    )?
    (?P<amount>
        \d{1,3}(?:[ '\u2019.]\d{3})*(?:[,.]\d{2})
        |
        \d+(?:[,.]\d{2})
    )
    (?:
        \s*(?P<suffix>CHF|SFr\.?|Fr\.?|EUR|€)
    )?
    """,
    re.IGNORECASE | re.VERBOSE,
)


@dataclass
class AmountCandidate:
    amount: Decimal
    currency: str
    confidence: int
    context: str


def parse_amount(raw: str) -> Optional[Decimal]:
    value = raw.strip().replace(" ", "").replace("'", "").replace("\u2019", "")
    if re.match(r"^\d{1,3}(?:\.\d{3})+,\d{2}$", value):
        value = value.replace(".", "").replace(",", ".")
    elif "," in value and "." not in value:
        value = value.replace(",", ".")
    elif value.count(".") > 1:
        parts = value.split(".")
        value = "".join(parts[:-1]) + "." + parts[-1]

    try:
        amount = Decimal(value)
    except InvalidOperation:
        return None

    if amount <= 0:
        return None
    return amount.quantize(Decimal("0.01"))


def detect_currency(prefix: Optional[str], suffix: Optional[str], default: str = "CHF") -> str:
    marker = (prefix or suffix or default).upper().replace(".", "")
    if marker in {"€", "EUR"}:
        return "EUR"
    return "CHF"


def normalize_text(text: str) -> str:
    return re.sub(r"\s+", " ", text or "").strip()


def score_context(context: str, has_currency: bool) -> int:
    lower = context.lower()
    score = 20
    if has_currency:
        score += 20
    for keyword in TOTAL_KEYWORDS:
        if keyword in lower:
            score += 60
            break
    for keyword in NEGATIVE_KEYWORDS:
        if keyword in lower:
            score -= 35
    if "%" in lower:
        score -= 30
    if re.search(r"\b(iban|qr|referenz|konto)\b", lower):
        score -= 15
    return score


def iter_amount_candidates(text: str, default_currency: str = "CHF") -> Iterable[AmountCandidate]:
    cleaned = normalize_text(text)
    for match in AMOUNT_PATTERN.finditer(cleaned):
        amount = parse_amount(match.group("amount"))
        if amount is None:
            continue
        if amount < Decimal("1.00") or amount > Decimal("20000.00"):
            continue

        prefix = match.group("prefix")
        suffix = match.group("suffix")
        currency = detect_currency(prefix, suffix, default_currency)
        start = max(0, match.start() - 90)
        end = min(len(cleaned), match.end() + 90)
        context = cleaned[start:end]
        confidence = score_context(context, bool(prefix or suffix))
        yield AmountCandidate(amount, currency, confidence, context)


def best_amount_candidate(text: str, default_currency: str = "CHF") -> Optional[AmountCandidate]:
    candidates = list(iter_amount_candidates(text, default_currency))
    if not candidates:
        return None
    return max(candidates, key=lambda item: (item.confidence, item.amount))
