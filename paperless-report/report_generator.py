"""
Report Generator für das Paperless Finance Report Tool.

Generiert Berichte in verschiedenen Formaten: CLI, HTML, PDF, JSON.
"""

import json
import logging
import os
from datetime import datetime
from decimal import Decimal
from pathlib import Path
from typing import Any, Dict, List, Optional, Union

from jinja2 import Environment, FileSystemLoader, select_autoescape

from config import Config, get_config
from extractor import AggregationResult, FinanceDocument, GroupStats

logger = logging.getLogger(__name__)


class DecimalEncoder(json.JSONEncoder):
    """JSON Encoder für Decimal-Werte."""

    def default(self, obj):
        if isinstance(obj, Decimal):
            return float(obj)
        if isinstance(obj, datetime):
            return obj.isoformat()
        if isinstance(obj, FinanceDocument):
            return {
                'id': obj.id,
                'title': obj.title,
                'betrag': float(obj.betrag) if obj.betrag else None,
                'effective_date': obj.effective_date.isoformat() if obj.effective_date else None,
                'correspondent': obj.correspondent,
                'kategorie': obj.kategorie,
                'tags': obj.tags,
                'web_url': obj.web_url,
            }
        if isinstance(obj, GroupStats):
            return {
                'name': obj.name,
                'amount': float(obj.amount),
                'count': obj.count,
                'percentage': obj.percentage,
            }
        return super().default(obj)


class ReportGenerator:
    """Generiert Finanzberichte in verschiedenen Formaten."""

    def __init__(self, config: Optional[Config] = None):
        """
        Initialisiert den Report Generator.

        Args:
            config: Konfiguration
        """
        self.config = config or get_config()
        self.currency = self.config.currency

        # Jinja2 Template-Umgebung
        template_dir = Path(__file__).parent / 'templates'
        self.jinja_env = Environment(
            loader=FileSystemLoader(str(template_dir)),
            autoescape=select_autoescape(['html', 'xml']),
        )

        # Custom Filter registrieren
        self.jinja_env.filters['format_amount'] = self._format_amount
        self.jinja_env.filters['format_percent'] = self._format_percent
        self.jinja_env.filters['format_date'] = self._format_date

    def _format_amount(self, value: Optional[Decimal], with_currency: bool = True) -> str:
        """Formatiert einen Betrag."""
        if value is None:
            return '-'
        formatted = f"{value:,.2f}".replace(',', "'")
        if with_currency:
            return f"{self.currency} {formatted}"
        return formatted

    def _format_percent(self, value: float) -> str:
        """Formatiert einen Prozentwert."""
        return f"{value:.1f}%"

    def _format_date(self, value: Optional[datetime], fmt: str = '%d.%m.%Y') -> str:
        """Formatiert ein Datum."""
        if value is None:
            return '-'
        return value.strftime(fmt)

    def _ensure_output_dir(self) -> Path:
        """Stellt sicher, dass das Ausgabeverzeichnis existiert."""
        output_dir = self.config.output_path
        output_dir.mkdir(parents=True, exist_ok=True)
        return output_dir

    def _get_output_filename(
        self,
        year: Optional[int] = None,
        month: Optional[int] = None,
        extension: str = 'html'
    ) -> str:
        """Generiert den Ausgabe-Dateinamen."""
        pattern = self.config.get('output.filename_pattern', 'finanzbericht_{year}')

        now = datetime.now()
        filename = pattern.format(
            year=year or now.year,
            month=month or now.month,
            date=now.strftime('%Y-%m-%d'),
            timestamp=now.strftime('%Y%m%d_%H%M%S'),
        )

        return f"{filename}.{extension}"

    # ==================== CLI Output ====================

    def generate_cli(
        self,
        result: AggregationResult,
        title: str = "Paperless Finanzbericht",
        detail: bool = False
    ) -> str:
        """
        Generiert CLI-Ausgabe.

        Args:
            result: Aggregationsergebnis
            title: Berichtstitel
            detail: Detailansicht aktivieren

        Returns:
            Formatierter String für CLI-Ausgabe
        """
        lines = []
        sep = "=" * 60

        # Header
        lines.append(sep)
        lines.append(title.center(60))
        lines.append(sep)
        lines.append("")

        # Übersicht
        lines.append(f"Dokumente gesamt:      {result.document_count}")
        lines.append(f"  - mit Betrag:        {result.documents_with_amount}")
        lines.append(f"  - ohne Betrag:       {result.documents_without_amount}")
        lines.append("")
        lines.append(f"Gesamtsumme:           {self._format_amount(result.total_amount)}")
        lines.append(f"Durchschnitt:          {self._format_amount(result.average_amount)}")
        lines.append(f"Median:                {self._format_amount(result.median_amount)}")
        lines.append(f"Minimum:               {self._format_amount(result.min_amount)}")
        lines.append(f"Maximum:               {self._format_amount(result.max_amount)}")
        lines.append("")

        # Nach Tag
        if result.by_tag:
            lines.append("-" * 60)
            lines.append("Nach Tag:")
            lines.append("-" * 60)
            for name, stats in result.by_tag.items():
                amount_str = self._format_amount(stats.amount).rjust(18)
                pct_str = f"({stats.percentage:5.1f}%)"
                lines.append(f"  {name:<25} {amount_str}  {pct_str}")
            lines.append("")

        # Nach Korrespondent
        if result.by_correspondent and detail:
            lines.append("-" * 60)
            lines.append("Nach Korrespondent:")
            lines.append("-" * 60)
            for name, stats in list(result.by_correspondent.items())[:15]:
                amount_str = self._format_amount(stats.amount).rjust(18)
                pct_str = f"({stats.percentage:5.1f}%)"
                lines.append(f"  {name[:25]:<25} {amount_str}  {pct_str}")
            if len(result.by_correspondent) > 15:
                lines.append(f"  ... und {len(result.by_correspondent) - 15} weitere")
            lines.append("")

        # Nach Kategorie
        if result.by_category:
            lines.append("-" * 60)
            lines.append("Nach Kategorie:")
            lines.append("-" * 60)
            for name, stats in result.by_category.items():
                amount_str = self._format_amount(stats.amount).rjust(18)
                pct_str = f"({stats.percentage:5.1f}%)"
                lines.append(f"  {name[:25]:<25} {amount_str}  {pct_str}")
            lines.append("")

        # Nach Monat
        if result.by_month:
            lines.append("-" * 60)
            lines.append("Nach Monat:")
            lines.append("-" * 60)
            for month, stats in result.by_month.items():
                amount_str = self._format_amount(stats.amount).rjust(18)
                lines.append(f"  {month:<10} {amount_str}  ({stats.count} Dok.)")
            lines.append("")

        # Nach Zahlungsart
        if result.by_payment_type and detail:
            lines.append("-" * 60)
            lines.append("Nach Zahlungsart:")
            lines.append("-" * 60)
            for name, stats in result.by_payment_type.items():
                amount_str = self._format_amount(stats.amount).rjust(18)
                pct_str = f"({stats.percentage:5.1f}%)"
                lines.append(f"  {name:<25} {amount_str}  {pct_str}")
            lines.append("")

        # Top-Posten
        if result.top_items and detail:
            lines.append("-" * 60)
            lines.append("Top 10 Einzelposten:")
            lines.append("-" * 60)
            for i, doc in enumerate(result.top_items[:10], 1):
                amount_str = self._format_amount(doc.betrag).rjust(18)
                title = doc.title[:35]
                lines.append(f"  {i:2}. {title:<35} {amount_str}")
            lines.append("")

        lines.append(sep)
        lines.append(f"Generiert: {datetime.now().strftime('%d.%m.%Y %H:%M')}")
        lines.append(sep)

        return "\n".join(lines)

    # ==================== HTML Output ====================

    def generate_html(
        self,
        result: AggregationResult,
        title: str = "Paperless Finanzbericht",
        year: Optional[int] = None,
        month: Optional[int] = None,
        comparison: Optional[Dict] = None
    ) -> str:
        """
        Generiert HTML-Bericht.

        Args:
            result: Aggregationsergebnis
            title: Berichtstitel
            year: Jahr für den Bericht
            month: Monat für den Bericht (optional)
            comparison: Vergleichsdaten (optional)

        Returns:
            HTML-String
        """
        template = self.jinja_env.get_template('report.html')

        # Chart-Daten vorbereiten
        tag_chart_data = self._prepare_chart_data(result.by_tag)
        category_chart_data = self._prepare_chart_data(result.by_category)
        month_chart_data = self._prepare_line_chart_data(result.by_month)
        correspondent_chart_data = self._prepare_chart_data(
            dict(list(result.by_correspondent.items())[:10])
        )

        context = {
            'title': title,
            'year': year,
            'month': month,
            'currency': self.currency,
            'generated_at': datetime.now(),
            'result': result,
            'comparison': comparison,

            # Chart-Daten als JSON
            'tag_chart_data': json.dumps(tag_chart_data),
            'category_chart_data': json.dumps(category_chart_data),
            'month_chart_data': json.dumps(month_chart_data),
            'correspondent_chart_data': json.dumps(correspondent_chart_data),
        }

        return template.render(**context)

    def _prepare_chart_data(self, groups: Dict[str, GroupStats]) -> Dict[str, Any]:
        """Bereitet Daten für ein Balken-/Kreisdiagramm vor."""
        labels = []
        values = []
        colors = self._generate_colors(len(groups))

        for name, stats in groups.items():
            labels.append(name)
            values.append(float(stats.amount))

        return {
            'labels': labels,
            'values': values,
            'colors': colors,
        }

    def _prepare_line_chart_data(self, groups: Dict[str, GroupStats]) -> Dict[str, Any]:
        """Bereitet Daten für ein Liniendiagramm vor."""
        # Nach Datum sortieren
        sorted_items = sorted(groups.items())

        labels = [item[0] for item in sorted_items]
        values = [float(item[1].amount) for item in sorted_items]

        return {
            'labels': labels,
            'values': values,
        }

    def _generate_colors(self, count: int) -> List[str]:
        """Generiert eine Farbpalette."""
        # Vordefinierte Farben
        colors = [
            '#2E86AB',  # Blau
            '#A23B72',  # Magenta
            '#F18F01',  # Orange
            '#C73E1D',  # Rot
            '#3B1F2B',  # Dunkelrot
            '#95C623',  # Grün
            '#5C5D67',  # Grau
            '#E8D21D',  # Gelb
            '#1B998B',  # Türkis
            '#7768AE',  # Lila
        ]

        # Farben wiederholen falls nötig
        while len(colors) < count:
            colors.extend(colors)

        return colors[:count]

    def save_html(
        self,
        result: AggregationResult,
        title: str = "Paperless Finanzbericht",
        year: Optional[int] = None,
        month: Optional[int] = None,
        comparison: Optional[Dict] = None,
        filename: Optional[str] = None
    ) -> Path:
        """
        Speichert HTML-Bericht als Datei.

        Returns:
            Pfad zur erstellten Datei
        """
        html = self.generate_html(result, title, year, month, comparison)

        output_dir = self._ensure_output_dir()
        if filename is None:
            filename = self._get_output_filename(year, month, 'html')

        output_path = output_dir / filename

        with open(output_path, 'w', encoding='utf-8') as f:
            f.write(html)

        logger.info(f"HTML-Bericht gespeichert: {output_path}")
        return output_path

    # ==================== PDF Output ====================

    def generate_pdf(
        self,
        result: AggregationResult,
        title: str = "Paperless Finanzbericht",
        year: Optional[int] = None,
        month: Optional[int] = None,
        comparison: Optional[Dict] = None
    ) -> bytes:
        """
        Generiert PDF-Bericht.

        Returns:
            PDF als Bytes
        """
        try:
            from weasyprint import HTML, CSS
        except ImportError:
            raise ImportError(
                "WeasyPrint ist nicht installiert. "
                "Installiere mit: pip install weasyprint"
            )

        # HTML generieren
        html_content = self.generate_html(result, title, year, month, comparison)

        # PDF generieren
        html = HTML(string=html_content)

        # Zusätzliches CSS für PDF
        pdf_css = CSS(string='''
            @page {
                size: A4;
                margin: 2cm;
            }
            body {
                font-size: 10pt;
            }
            .chart-container {
                page-break-inside: avoid;
            }
            table {
                page-break-inside: avoid;
            }
        ''')

        return html.write_pdf(stylesheets=[pdf_css])

    def save_pdf(
        self,
        result: AggregationResult,
        title: str = "Paperless Finanzbericht",
        year: Optional[int] = None,
        month: Optional[int] = None,
        comparison: Optional[Dict] = None,
        filename: Optional[str] = None
    ) -> Path:
        """
        Speichert PDF-Bericht als Datei.

        Returns:
            Pfad zur erstellten Datei
        """
        pdf_bytes = self.generate_pdf(result, title, year, month, comparison)

        output_dir = self._ensure_output_dir()
        if filename is None:
            filename = self._get_output_filename(year, month, 'pdf')

        output_path = output_dir / filename

        with open(output_path, 'wb') as f:
            f.write(pdf_bytes)

        logger.info(f"PDF-Bericht gespeichert: {output_path}")
        return output_path

    # ==================== JSON Output ====================

    def generate_json(
        self,
        result: AggregationResult,
        indent: int = 2
    ) -> str:
        """
        Generiert JSON-Ausgabe.

        Returns:
            JSON-String
        """
        data = {
            'generated_at': datetime.now().isoformat(),
            'currency': self.currency,
            'summary': {
                'total_amount': result.total_amount,
                'document_count': result.document_count,
                'documents_with_amount': result.documents_with_amount,
                'documents_without_amount': result.documents_without_amount,
                'average_amount': result.average_amount,
                'median_amount': result.median_amount,
                'min_amount': result.min_amount,
                'max_amount': result.max_amount,
            },
            'by_tag': result.by_tag,
            'by_correspondent': result.by_correspondent,
            'by_category': result.by_category,
            'by_payment_type': result.by_payment_type,
            'by_month': result.by_month,
            'top_items': result.top_items[:20],
            'documents': result.documents,
        }

        return json.dumps(data, indent=indent, cls=DecimalEncoder, ensure_ascii=False)

    def save_json(
        self,
        result: AggregationResult,
        year: Optional[int] = None,
        month: Optional[int] = None,
        filename: Optional[str] = None
    ) -> Path:
        """
        Speichert JSON-Bericht als Datei.

        Returns:
            Pfad zur erstellten Datei
        """
        json_str = self.generate_json(result)

        output_dir = self._ensure_output_dir()
        if filename is None:
            filename = self._get_output_filename(year, month, 'json')

        output_path = output_dir / filename

        with open(output_path, 'w', encoding='utf-8') as f:
            f.write(json_str)

        logger.info(f"JSON-Bericht gespeichert: {output_path}")
        return output_path

    # ==================== CSV Output ====================

    def generate_csv(
        self,
        documents: List[FinanceDocument],
        delimiter: str = ';'
    ) -> str:
        """
        Generiert CSV-Export der Dokumente.

        Returns:
            CSV-String
        """
        lines = []

        # Header
        headers = [
            'ID', 'Titel', 'Datum', 'Betrag', 'Korrespondent',
            'Kategorie', 'Zahlungsart', 'Tags', 'URL'
        ]
        lines.append(delimiter.join(headers))

        # Daten
        for doc in documents:
            row = [
                str(doc.id),
                f'"{doc.title}"' if delimiter in doc.title else doc.title,
                self._format_date(doc.effective_date),
                self._format_amount(doc.betrag, with_currency=False) if doc.betrag else '',
                doc.correspondent or '',
                doc.kategorie or '',
                doc.zahlungsart or '',
                ', '.join(doc.tags),
                doc.web_url or '',
            ]
            lines.append(delimiter.join(row))

        return '\n'.join(lines)

    def save_csv(
        self,
        documents: List[FinanceDocument],
        year: Optional[int] = None,
        month: Optional[int] = None,
        filename: Optional[str] = None
    ) -> Path:
        """Speichert CSV-Export als Datei."""
        csv_str = self.generate_csv(documents)

        output_dir = self._ensure_output_dir()
        if filename is None:
            filename = self._get_output_filename(year, month, 'csv')

        output_path = output_dir / filename

        with open(output_path, 'w', encoding='utf-8-sig') as f:  # BOM für Excel
            f.write(csv_str)

        logger.info(f"CSV-Export gespeichert: {output_path}")
        return output_path

    # ==================== Vergleichsbericht ====================

    def generate_comparison_cli(self, comparison: Dict) -> str:
        """Generiert CLI-Ausgabe für Periodenvergleich."""
        lines = []
        sep = "=" * 70

        p1 = comparison['period1']
        p2 = comparison['period2']

        lines.append(sep)
        lines.append(f"Vergleich: {p1['name']} vs {p2['name']}".center(70))
        lines.append(sep)
        lines.append("")

        # Übersicht
        lines.append(f"{'Kennzahl':<30} {p1['name']:>15} {p2['name']:>15} {'Diff':>10}")
        lines.append("-" * 70)

        lines.append(
            f"{'Gesamtsumme':<30} "
            f"{self._format_amount(p1['total'], False):>15} "
            f"{self._format_amount(p2['total'], False):>15} "
            f"{comparison['diff_percent']:>+9.1f}%"
        )

        lines.append(
            f"{'Anzahl Dokumente':<30} "
            f"{p1['count']:>15} "
            f"{p2['count']:>15} "
            f"{p2['count'] - p1['count']:>+10}"
        )

        lines.append("")
        lines.append("-" * 70)
        lines.append("Nach Kategorie:")
        lines.append("-" * 70)

        for cat, data in sorted(
            comparison['category_comparison'].items(),
            key=lambda x: abs(x[1]['diff_absolute']),
            reverse=True
        ):
            status = ""
            if data['status'] == 'new':
                status = "[NEU]"
            elif data['status'] == 'removed':
                status = "[ENTF]"

            lines.append(
                f"  {cat[:25]:<25} "
                f"{self._format_amount(data['period1'], False):>12} "
                f"{self._format_amount(data['period2'], False):>12} "
                f"{data['diff_percent']:>+8.1f}% "
                f"{status}"
            )

        lines.append("")
        lines.append(sep)

        return "\n".join(lines)
