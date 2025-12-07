"""
Daten-Extraktion und Aggregation für das Paperless Finance Report Tool.

Extrahiert Custom Fields aus Dokumenten und aggregiert die Daten
für verschiedene Gruppierungen.
"""

import logging
import re
from collections import defaultdict
from dataclasses import dataclass, field
from datetime import datetime
from decimal import Decimal, InvalidOperation
from typing import Any, Callable, Dict, List, Optional, Tuple, Union

from dateutil.parser import parse as parse_date

from config import Config, get_config
from paperless_client import PaperlessClient

logger = logging.getLogger(__name__)


@dataclass
class FinanceDocument:
    """Ein aufbereitetes Finanzdokument."""

    id: int
    title: str
    archive_date: Optional[datetime] = None
    created: Optional[datetime] = None
    added: Optional[datetime] = None

    # Paperless Metadata
    correspondent: Optional[str] = None
    correspondent_id: Optional[int] = None
    document_type: Optional[str] = None
    tags: List[str] = field(default_factory=list)
    tag_ids: List[int] = field(default_factory=list)

    # Custom Fields
    betrag: Optional[Decimal] = None
    rechnungsdatum: Optional[datetime] = None
    kategorie: Optional[str] = None
    zahlungsart: Optional[str] = None
    periode: Optional[str] = None
    notiz: Optional[str] = None

    # URLs
    web_url: Optional[str] = None

    # Original-Daten
    raw_data: Dict = field(default_factory=dict)

    @property
    def effective_date(self) -> Optional[datetime]:
        """Das effektive Datum (Rechnungsdatum oder Archivdatum)."""
        return self.rechnungsdatum or self.archive_date

    @property
    def year(self) -> Optional[int]:
        """Jahr des effektiven Datums."""
        date = self.effective_date
        return date.year if date else None

    @property
    def month(self) -> Optional[int]:
        """Monat des effektiven Datums."""
        date = self.effective_date
        return date.month if date else None

    @property
    def month_year(self) -> Optional[str]:
        """Monat/Jahr als String (z.B. '2024-01')."""
        date = self.effective_date
        return date.strftime('%Y-%m') if date else None

    @property
    def quarter(self) -> Optional[str]:
        """Quartal als String (z.B. 'Q1 2024')."""
        date = self.effective_date
        if not date:
            return None
        q = (date.month - 1) // 3 + 1
        return f"Q{q} {date.year}"


class DocumentExtractor:
    """Extrahiert und verarbeitet Dokumente aus Paperless."""

    def __init__(self, client: PaperlessClient, config: Optional[Config] = None):
        """
        Initialisiert den Extractor.

        Args:
            client: Paperless API Client
            config: Konfiguration
        """
        self.client = client
        self.config = config or get_config()
        self._custom_fields_map: Dict[str, int] = {}

    def _build_custom_fields_map(self) -> None:
        """Baut ein Mapping von Feldnamen zu IDs."""
        if self._custom_fields_map:
            return

        fields = self.client.get_custom_fields()
        for field_id, field_def in fields.items():
            name = field_def['name'].lower()
            self._custom_fields_map[name] = field_id

    def _parse_decimal(self, value: Any) -> Optional[Decimal]:
        """
        Parst einen Wert zu Decimal.

        Verarbeitet verschiedene Formate:
        - 1234.56
        - 1234,56
        - 1'234.56 (Schweizer Format)
        - CHF 1234.56
        """
        if value is None:
            return None

        if isinstance(value, (int, float)):
            return Decimal(str(value))

        if isinstance(value, Decimal):
            return value

        if not isinstance(value, str):
            return None

        # String bereinigen
        value = value.strip()

        # Währungssymbole entfernen
        value = re.sub(r'^(CHF|EUR|USD|Fr\.?)\s*', '', value, flags=re.IGNORECASE)
        value = re.sub(r'\s*(CHF|EUR|USD|Fr\.?)$', '', value, flags=re.IGNORECASE)

        # Tausender-Trennzeichen entfernen (Apostroph, Punkt als Tausender)
        # Schweizer Format: 1'234.56 oder 1'234,56
        if "'" in value:
            value = value.replace("'", "")

        # Deutsches/Schweizer Format mit Punkt als Tausender: 1.234,56
        if re.match(r'^\d{1,3}(\.\d{3})+,\d{2}$', value):
            value = value.replace(".", "").replace(",", ".")
        # Komma als Dezimaltrennzeichen ohne Tausender
        elif "," in value and "." not in value:
            value = value.replace(",", ".")

        try:
            return Decimal(value)
        except InvalidOperation:
            logger.warning(f"Konnte Betrag nicht parsen: {value}")
            return None

    def _parse_date(self, value: Any) -> Optional[datetime]:
        """Parst einen Wert zu datetime."""
        if value is None:
            return None

        if isinstance(value, datetime):
            return value

        if not isinstance(value, str):
            return None

        try:
            return parse_date(value)
        except (ValueError, TypeError):
            logger.warning(f"Konnte Datum nicht parsen: {value}")
            return None

    def _get_custom_field_value(self, doc: dict, field_name: str) -> Any:
        """Holt den Wert eines Custom Fields aus einem Dokument."""
        # Aus resolved fields
        resolved = doc.get('custom_fields_resolved', {})
        if field_name in resolved:
            return resolved[field_name].get('value')

        # Aus rohen custom_fields
        self._build_custom_fields_map()
        field_name_lower = field_name.lower()

        for cf in doc.get('custom_fields', []):
            field_id = cf.get('field')
            # Prüfen ob ID zum gesuchten Feldnamen passt
            for name, fid in self._custom_fields_map.items():
                if fid == field_id and name == field_name_lower:
                    return cf.get('value')

        return None

    def extract_document(self, raw_doc: dict) -> FinanceDocument:
        """
        Extrahiert ein aufbereitetes FinanceDocument aus den Rohdaten.

        Args:
            raw_doc: Rohes Dokument-Dictionary von der API

        Returns:
            FinanceDocument-Instanz
        """
        # Custom Field Namen aus Config
        cf_names = self.config.custom_field_names

        # Basis-Daten
        doc = FinanceDocument(
            id=raw_doc['id'],
            title=raw_doc.get('title', ''),
            raw_data=raw_doc
        )

        # Datums-Felder
        doc.archive_date = self._parse_date(raw_doc.get('archive_date'))
        doc.created = self._parse_date(raw_doc.get('created'))
        doc.added = self._parse_date(raw_doc.get('added'))

        # Korrespondent
        doc.correspondent_id = raw_doc.get('correspondent')
        doc.correspondent = raw_doc.get('correspondent_name', '')

        # Dokumenttyp
        doc.document_type = raw_doc.get('document_type_name', '')

        # Tags
        doc.tag_ids = raw_doc.get('tags', [])
        doc.tags = raw_doc.get('tag_names', [])

        # URL
        doc.web_url = raw_doc.get('web_url', '')

        # Custom Fields
        betrag_name = cf_names.get('betrag', 'betrag')
        doc.betrag = self._parse_decimal(
            self._get_custom_field_value(raw_doc, betrag_name)
        )

        datum_name = cf_names.get('rechnungsdatum', 'rechnungsdatum')
        doc.rechnungsdatum = self._parse_date(
            self._get_custom_field_value(raw_doc, datum_name)
        )

        kat_name = cf_names.get('kategorie', 'kategorie')
        doc.kategorie = self._get_custom_field_value(raw_doc, kat_name)

        zahl_name = cf_names.get('zahlungsart', 'zahlungsart')
        doc.zahlungsart = self._get_custom_field_value(raw_doc, zahl_name)

        periode_name = cf_names.get('periode', 'periode')
        doc.periode = self._get_custom_field_value(raw_doc, periode_name)

        notiz_name = cf_names.get('notiz', 'notiz')
        doc.notiz = self._get_custom_field_value(raw_doc, notiz_name)

        return doc

    def extract_documents(self, raw_docs: List[dict]) -> List[FinanceDocument]:
        """
        Extrahiert mehrere Dokumente.

        Args:
            raw_docs: Liste von Roh-Dokumenten

        Returns:
            Liste von FinanceDocument-Instanzen
        """
        # Metadaten auflösen
        resolved = self.client.resolve_all_metadata(raw_docs)

        return [self.extract_document(doc) for doc in resolved]


@dataclass
class AggregationResult:
    """Ergebnis einer Aggregation."""

    # Basis-Statistiken
    total_amount: Decimal = Decimal('0')
    document_count: int = 0
    documents_with_amount: int = 0
    documents_without_amount: int = 0

    # Dokumente
    documents: List[FinanceDocument] = field(default_factory=list)

    # Gruppierte Daten
    by_tag: Dict[str, 'GroupStats'] = field(default_factory=dict)
    by_correspondent: Dict[str, 'GroupStats'] = field(default_factory=dict)
    by_category: Dict[str, 'GroupStats'] = field(default_factory=dict)
    by_payment_type: Dict[str, 'GroupStats'] = field(default_factory=dict)
    by_month: Dict[str, 'GroupStats'] = field(default_factory=dict)
    by_quarter: Dict[str, 'GroupStats'] = field(default_factory=dict)
    by_year: Dict[int, 'GroupStats'] = field(default_factory=dict)

    # Zusätzliche Statistiken
    average_amount: Decimal = Decimal('0')
    median_amount: Decimal = Decimal('0')
    min_amount: Decimal = Decimal('0')
    max_amount: Decimal = Decimal('0')
    top_items: List[FinanceDocument] = field(default_factory=list)

    @property
    def total_formatted(self) -> str:
        """Formatierte Gesamtsumme."""
        return f"{self.total_amount:,.2f}".replace(',', "'")


@dataclass
class GroupStats:
    """Statistiken für eine Gruppe."""

    name: str
    amount: Decimal = Decimal('0')
    count: int = 0
    percentage: float = 0.0
    documents: List[FinanceDocument] = field(default_factory=list)

    @property
    def amount_formatted(self) -> str:
        """Formatierter Betrag."""
        return f"{self.amount:,.2f}".replace(',', "'")


class DataAggregator:
    """Aggregiert Finanzdokumente nach verschiedenen Kriterien."""

    def __init__(self, config: Optional[Config] = None):
        """
        Initialisiert den Aggregator.

        Args:
            config: Konfiguration
        """
        self.config = config or get_config()

    def aggregate(
        self,
        documents: List[FinanceDocument],
        group_by: Optional[List[str]] = None
    ) -> AggregationResult:
        """
        Aggregiert Dokumente.

        Args:
            documents: Liste von Dokumenten
            group_by: Liste von Gruppierungskriterien:
                     'tag', 'correspondent', 'category', 'payment_type',
                     'month', 'quarter', 'year'

        Returns:
            AggregationResult mit allen Statistiken
        """
        result = AggregationResult()
        result.documents = documents
        result.document_count = len(documents)

        # Beträge sammeln
        amounts: List[Decimal] = []

        for doc in documents:
            if doc.betrag is not None:
                result.total_amount += doc.betrag
                result.documents_with_amount += 1
                amounts.append(doc.betrag)
            else:
                result.documents_without_amount += 1

        # Basis-Statistiken
        if amounts:
            amounts_sorted = sorted(amounts)
            result.min_amount = amounts_sorted[0]
            result.max_amount = amounts_sorted[-1]
            result.average_amount = result.total_amount / len(amounts)

            # Median
            mid = len(amounts_sorted) // 2
            if len(amounts_sorted) % 2 == 0:
                result.median_amount = (amounts_sorted[mid - 1] + amounts_sorted[mid]) / 2
            else:
                result.median_amount = amounts_sorted[mid]

        # Top-Posten
        docs_with_amount = [d for d in documents if d.betrag is not None]
        result.top_items = sorted(
            docs_with_amount,
            key=lambda d: d.betrag or Decimal('0'),
            reverse=True
        )[:10]

        # Gruppierungen
        group_by = group_by or ['tag', 'correspondent', 'category', 'month']

        if 'tag' in group_by:
            result.by_tag = self._group_by_tags(documents, result.total_amount)

        if 'correspondent' in group_by:
            result.by_correspondent = self._group_by_field(
                documents, 'correspondent', result.total_amount
            )

        if 'category' in group_by:
            result.by_category = self._group_by_field(
                documents, 'kategorie', result.total_amount
            )

        if 'payment_type' in group_by:
            result.by_payment_type = self._group_by_field(
                documents, 'zahlungsart', result.total_amount
            )

        if 'month' in group_by:
            result.by_month = self._group_by_field(
                documents, 'month_year', result.total_amount
            )

        if 'quarter' in group_by:
            result.by_quarter = self._group_by_field(
                documents, 'quarter', result.total_amount
            )

        if 'year' in group_by:
            result.by_year = self._group_by_field(
                documents, 'year', result.total_amount
            )

        return result

    def _group_by_tags(
        self,
        documents: List[FinanceDocument],
        total: Decimal
    ) -> Dict[str, GroupStats]:
        """Gruppiert nach Tags (ein Dokument kann mehrere Tags haben)."""
        groups: Dict[str, GroupStats] = {}

        for doc in documents:
            if not doc.tags:
                tag_name = 'Ohne Tag'
                if tag_name not in groups:
                    groups[tag_name] = GroupStats(name=tag_name)
                groups[tag_name].count += 1
                if doc.betrag:
                    groups[tag_name].amount += doc.betrag
                groups[tag_name].documents.append(doc)
            else:
                for tag in doc.tags:
                    if tag not in groups:
                        groups[tag] = GroupStats(name=tag)
                    groups[tag].count += 1
                    if doc.betrag:
                        groups[tag].amount += doc.betrag
                    groups[tag].documents.append(doc)

        # Prozente berechnen
        if total > 0:
            for stats in groups.values():
                stats.percentage = float(stats.amount / total * 100)

        # Nach Betrag sortieren
        return dict(sorted(
            groups.items(),
            key=lambda x: x[1].amount,
            reverse=True
        ))

    def _group_by_field(
        self,
        documents: List[FinanceDocument],
        field: str,
        total: Decimal
    ) -> Dict[str, GroupStats]:
        """Gruppiert nach einem einzelnen Feld."""
        groups: Dict[str, GroupStats] = {}

        for doc in documents:
            value = getattr(doc, field, None)

            if value is None or value == '':
                key = 'Nicht zugeordnet'
            else:
                key = str(value)

            if key not in groups:
                groups[key] = GroupStats(name=key)

            groups[key].count += 1
            if doc.betrag:
                groups[key].amount += doc.betrag
            groups[key].documents.append(doc)

        # Prozente berechnen
        if total > 0:
            for stats in groups.values():
                stats.percentage = float(stats.amount / total * 100)

        # Nach Betrag sortieren (bei Monaten chronologisch)
        if field in ('month_year', 'quarter'):
            return dict(sorted(groups.items()))
        else:
            return dict(sorted(
                groups.items(),
                key=lambda x: x[1].amount,
                reverse=True
            ))

    def compare_periods(
        self,
        documents: List[FinanceDocument],
        period1: Union[int, str],
        period2: Union[int, str],
        period_type: str = 'year'
    ) -> Dict[str, Any]:
        """
        Vergleicht zwei Zeiträume.

        Args:
            documents: Alle Dokumente
            period1: Erste Periode (z.B. 2023)
            period2: Zweite Periode (z.B. 2024)
            period_type: 'year', 'quarter', 'month'

        Returns:
            Vergleichsergebnis
        """
        # Dokumente nach Periode filtern
        def get_period(doc: FinanceDocument) -> Optional[Union[int, str]]:
            if period_type == 'year':
                return doc.year
            elif period_type == 'quarter':
                return doc.quarter
            elif period_type == 'month':
                return doc.month_year
            return None

        docs1 = [d for d in documents if get_period(d) == period1]
        docs2 = [d for d in documents if get_period(d) == period2]

        agg1 = self.aggregate(docs1, ['tag', 'category'])
        agg2 = self.aggregate(docs2, ['tag', 'category'])

        # Differenzen berechnen
        diff_absolute = agg2.total_amount - agg1.total_amount
        diff_percent = (
            float(diff_absolute / agg1.total_amount * 100)
            if agg1.total_amount > 0 else 0
        )

        # Kategorien vergleichen
        category_comparison = {}
        all_categories = set(agg1.by_category.keys()) | set(agg2.by_category.keys())

        for cat in all_categories:
            stats1 = agg1.by_category.get(cat, GroupStats(name=cat))
            stats2 = agg2.by_category.get(cat, GroupStats(name=cat))

            diff = stats2.amount - stats1.amount
            pct_change = (
                float(diff / stats1.amount * 100)
                if stats1.amount > 0 else (100.0 if stats2.amount > 0 else 0)
            )

            category_comparison[cat] = {
                'period1': stats1.amount,
                'period2': stats2.amount,
                'diff_absolute': diff,
                'diff_percent': pct_change,
                'status': 'new' if stats1.amount == 0 else (
                    'removed' if stats2.amount == 0 else 'changed'
                )
            }

        return {
            'period1': {
                'name': str(period1),
                'total': agg1.total_amount,
                'count': agg1.document_count,
                'aggregation': agg1,
            },
            'period2': {
                'name': str(period2),
                'total': agg2.total_amount,
                'count': agg2.document_count,
                'aggregation': agg2,
            },
            'diff_absolute': diff_absolute,
            'diff_percent': diff_percent,
            'category_comparison': category_comparison,
        }
