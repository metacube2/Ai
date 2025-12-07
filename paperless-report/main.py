#!/usr/bin/env python3
"""
Paperless Finance Report Tool

CLI-Einstiegspunkt für das Paperless Finanz-Auswertungstool.
Generiert Finanzberichte aus Paperless-ngx Dokumenten.
"""

import logging
import sys
from datetime import datetime
from pathlib import Path
from typing import List, Optional

import click
from tabulate import tabulate

# Lokale Imports
from config import Config, ConfigError, get_config, reset_config
from extractor import DataAggregator, DocumentExtractor
from paperless_client import PaperlessAPIError, PaperlessClient
from report_generator import ReportGenerator

# Logger einrichten
logger = logging.getLogger('paperless_report')


def setup_logging(level: str = 'INFO', colorize: bool = True) -> None:
    """Richtet das Logging ein."""
    log_level = getattr(logging, level.upper(), logging.INFO)

    if colorize:
        try:
            import colorlog
            handler = colorlog.StreamHandler()
            handler.setFormatter(colorlog.ColoredFormatter(
                '%(log_color)s%(levelname)-8s%(reset)s %(message)s',
                log_colors={
                    'DEBUG': 'cyan',
                    'INFO': 'green',
                    'WARNING': 'yellow',
                    'ERROR': 'red',
                    'CRITICAL': 'red,bg_white',
                }
            ))
        except ImportError:
            handler = logging.StreamHandler()
            handler.setFormatter(logging.Formatter('%(levelname)-8s %(message)s'))
    else:
        handler = logging.StreamHandler()
        handler.setFormatter(logging.Formatter('%(levelname)-8s %(message)s'))

    logger.addHandler(handler)
    logger.setLevel(log_level)

    # Auch für andere Module
    logging.getLogger('paperless_report').setLevel(log_level)


def get_cache(config: Config):
    """Erstellt den Cache falls aktiviert."""
    if not config.cache_enabled:
        return None

    try:
        from diskcache import Cache
        cache_path = config.cache_path
        cache_path.mkdir(parents=True, exist_ok=True)
        return Cache(str(cache_path))
    except ImportError:
        logger.warning("diskcache nicht installiert, Cache deaktiviert")
        return None


# CLI-Gruppe
@click.group()
@click.option('--config', '-c', 'config_path', type=click.Path(exists=True),
              help='Pfad zur Konfigurationsdatei')
@click.option('--verbose', '-v', is_flag=True, help='Ausführliche Ausgabe')
@click.option('--quiet', '-q', is_flag=True, help='Nur Fehler ausgeben')
@click.pass_context
def cli(ctx, config_path: Optional[str], verbose: bool, quiet: bool):
    """
    Paperless Finance Report Tool

    Generiert Finanzberichte aus Paperless-ngx Dokumenten.

    Beispiele:

        # Jahresbericht 2024
        paperless-report report --year 2024

        # Mit Tag-Filter
        paperless-report report --year 2024 --tag rechnung

        # Jahresvergleich
        paperless-report compare 2023 2024

        # Verbindung testen
        paperless-report test
    """
    ctx.ensure_object(dict)

    # Log-Level bestimmen
    if quiet:
        log_level = 'ERROR'
    elif verbose:
        log_level = 'DEBUG'
    else:
        log_level = 'INFO'

    setup_logging(log_level)

    # Config laden
    try:
        reset_config()
        config = get_config(config_path)
        ctx.obj['config'] = config
    except ConfigError as e:
        click.echo(f"Konfigurationsfehler: {e}", err=True)
        sys.exit(1)


@cli.command()
@click.pass_context
def test(ctx):
    """Testet die Verbindung zur Paperless-API."""
    config = ctx.obj['config']

    click.echo(f"Teste Verbindung zu {config.paperless_url}...")

    try:
        cache = get_cache(config)
        client = PaperlessClient(config, cache)

        if client.test_connection():
            click.echo(click.style("Verbindung erfolgreich!", fg='green'))

            # Statistiken anzeigen
            click.echo("\nStatistiken:")
            tags = client.get_tags()
            correspondents = client.get_correspondents()
            custom_fields = client.get_custom_fields()

            click.echo(f"  Tags: {len(tags)}")
            click.echo(f"  Korrespondenten: {len(correspondents)}")
            click.echo(f"  Custom Fields: {len(custom_fields)}")

            # Custom Fields auflisten
            if custom_fields:
                click.echo("\nCustom Fields:")
                for field_id, field in custom_fields.items():
                    click.echo(f"  - {field['name']} (Typ: {field.get('data_type', 'unknown')})")

        else:
            click.echo(click.style("Verbindung fehlgeschlagen!", fg='red'))
            sys.exit(1)

    except PaperlessAPIError as e:
        click.echo(click.style(f"API-Fehler: {e}", fg='red'), err=True)
        sys.exit(1)


@cli.command()
@click.option('--year', '-y', type=int, help='Jahr für den Bericht')
@click.option('--month', '-m', type=int, help='Monat (1-12)')
@click.option('--tag', '-t', 'tags', multiple=True, help='Nach Tag filtern (mehrfach möglich)')
@click.option('--correspondent', help='Nach Korrespondent filtern')
@click.option('--group-by', '-g', 'group_by',
              type=click.Choice(['tag', 'correspondent', 'category', 'payment_type', 'month', 'quarter', 'year']),
              multiple=True, default=['tag', 'category', 'month'],
              help='Gruppierung (mehrfach möglich)')
@click.option('--format', '-f', 'output_format',
              type=click.Choice(['cli', 'html', 'pdf', 'json', 'csv']),
              default='cli', help='Ausgabeformat')
@click.option('--output', '-o', 'output_file', type=click.Path(),
              help='Ausgabedatei (optional)')
@click.option('--detail', '-d', is_flag=True, help='Detaillierte Ausgabe')
@click.option('--no-cache', is_flag=True, help='Cache ignorieren')
@click.pass_context
def report(ctx, year: Optional[int], month: Optional[int], tags: tuple,
           correspondent: Optional[str], group_by: tuple, output_format: str,
           output_file: Optional[str], detail: bool, no_cache: bool):
    """
    Generiert einen Finanzbericht.

    Beispiele:

        # Jahresbericht 2024 als CLI
        paperless-report report --year 2024

        # HTML-Bericht mit Tag-Filter
        paperless-report report --year 2024 --tag rechnung --format html

        # Detaillierter Bericht nach Korrespondent gruppiert
        paperless-report report --year 2024 --group-by correspondent --detail

        # PDF für einen bestimmten Monat
        paperless-report report --year 2024 --month 6 --format pdf
    """
    config = ctx.obj['config']

    # Standard: aktuelles Jahr
    if not year:
        year = datetime.now().year
        click.echo(f"Kein Jahr angegeben, verwende {year}")

    try:
        cache = None if no_cache else get_cache(config)
        client = PaperlessClient(config, cache)
        extractor = DocumentExtractor(client, config)
        aggregator = DataAggregator(config)
        generator = ReportGenerator(config)

        # Dokumente abrufen
        click.echo(f"Lade Dokumente für {year}" + (f"/{month}" if month else "") + "...")

        with click.progressbar(length=1, label='API-Abfrage') as bar:
            raw_docs = client.get_documents(
                tags=list(tags) if tags else None,
                correspondent=correspondent,
                year=year,
                month=month,
            )
            bar.update(1)

        if not raw_docs:
            click.echo(click.style("Keine Dokumente gefunden.", fg='yellow'))
            return

        click.echo(f"Gefunden: {len(raw_docs)} Dokumente")

        # Dokumente extrahieren
        click.echo("Extrahiere Daten...")
        documents = extractor.extract_documents(raw_docs)

        # Aggregieren
        click.echo("Aggregiere Daten...")
        result = aggregator.aggregate(documents, list(group_by))

        # Titel generieren
        if month:
            title = f"Paperless Finanzbericht {month:02d}/{year}"
        else:
            title = f"Paperless Finanzbericht {year}"

        # Ausgabe
        if output_format == 'cli':
            output = generator.generate_cli(result, title, detail)
            click.echo()
            click.echo(output)

        elif output_format == 'html':
            if output_file:
                path = Path(output_file)
            else:
                path = generator.save_html(result, title, year, month)
            click.echo(click.style(f"HTML-Bericht gespeichert: {path}", fg='green'))

            # Bericht öffnen?
            if click.confirm("Bericht im Browser öffnen?", default=True):
                import webbrowser
                webbrowser.open(f"file://{path.absolute()}")

        elif output_format == 'pdf':
            if output_file:
                path = Path(output_file)
                pdf_bytes = generator.generate_pdf(result, title, year, month)
                with open(path, 'wb') as f:
                    f.write(pdf_bytes)
            else:
                path = generator.save_pdf(result, title, year, month)
            click.echo(click.style(f"PDF-Bericht gespeichert: {path}", fg='green'))

        elif output_format == 'json':
            if output_file:
                path = Path(output_file)
                json_str = generator.generate_json(result)
                with open(path, 'w', encoding='utf-8') as f:
                    f.write(json_str)
            else:
                path = generator.save_json(result, year, month)
            click.echo(click.style(f"JSON-Export gespeichert: {path}", fg='green'))

        elif output_format == 'csv':
            if output_file:
                path = Path(output_file)
                csv_str = generator.generate_csv(documents)
                with open(path, 'w', encoding='utf-8-sig') as f:
                    f.write(csv_str)
            else:
                path = generator.save_csv(documents, year, month)
            click.echo(click.style(f"CSV-Export gespeichert: {path}", fg='green'))

    except PaperlessAPIError as e:
        click.echo(click.style(f"API-Fehler: {e}", fg='red'), err=True)
        sys.exit(1)
    except Exception as e:
        logger.exception("Unerwarteter Fehler")
        click.echo(click.style(f"Fehler: {e}", fg='red'), err=True)
        sys.exit(1)


@cli.command()
@click.argument('period1', type=int)
@click.argument('period2', type=int)
@click.option('--tag', '-t', 'tags', multiple=True, help='Nach Tag filtern')
@click.option('--format', '-f', 'output_format',
              type=click.Choice(['cli', 'html']), default='cli',
              help='Ausgabeformat')
@click.option('--output', '-o', 'output_file', type=click.Path(),
              help='Ausgabedatei')
@click.pass_context
def compare(ctx, period1: int, period2: int, tags: tuple,
            output_format: str, output_file: Optional[str]):
    """
    Vergleicht zwei Zeiträume (Jahre).

    Beispiele:

        # Jahresvergleich 2023 vs 2024
        paperless-report compare 2023 2024

        # Mit Tag-Filter
        paperless-report compare 2023 2024 --tag rechnung

        # Als HTML
        paperless-report compare 2023 2024 --format html
    """
    config = ctx.obj['config']

    try:
        cache = get_cache(config)
        client = PaperlessClient(config, cache)
        extractor = DocumentExtractor(client, config)
        aggregator = DataAggregator(config)
        generator = ReportGenerator(config)

        # Dokumente für beide Perioden laden
        click.echo(f"Lade Dokumente für {period1} und {period2}...")

        raw_docs_1 = client.get_documents(
            tags=list(tags) if tags else None,
            year=period1
        )
        raw_docs_2 = client.get_documents(
            tags=list(tags) if tags else None,
            year=period2
        )

        click.echo(f"Gefunden: {len(raw_docs_1)} ({period1}) / {len(raw_docs_2)} ({period2})")

        # Dokumente zusammenführen und extrahieren
        all_raw_docs = raw_docs_1 + raw_docs_2
        all_docs = extractor.extract_documents(all_raw_docs)

        # Vergleich
        click.echo("Vergleiche Perioden...")
        comparison = aggregator.compare_periods(all_docs, period1, period2)

        if output_format == 'cli':
            output = generator.generate_comparison_cli(comparison)
            click.echo()
            click.echo(output)

        elif output_format == 'html':
            # Aggregation für das neuere Jahr als Basis
            docs_2 = [d for d in all_docs if d.year == period2]
            result = aggregator.aggregate(docs_2, ['tag', 'category', 'month'])

            title = f"Vergleich {period1} vs {period2}"

            if output_file:
                path = Path(output_file)
                html = generator.generate_html(result, title, period2, comparison=comparison)
                with open(path, 'w', encoding='utf-8') as f:
                    f.write(html)
            else:
                path = generator.save_html(result, title, period2, comparison=comparison)

            click.echo(click.style(f"Vergleichsbericht gespeichert: {path}", fg='green'))

    except PaperlessAPIError as e:
        click.echo(click.style(f"API-Fehler: {e}", fg='red'), err=True)
        sys.exit(1)


@cli.command()
@click.option('--tag', '-t', 'tags', multiple=True, help='Nach Tag filtern')
@click.option('--year', '-y', type=int, help='Jahr')
@click.option('--limit', '-l', type=int, default=20, help='Anzahl Dokumente')
@click.pass_context
def list_docs(ctx, tags: tuple, year: Optional[int], limit: int):
    """
    Listet Dokumente auf.

    Beispiele:

        # Letzte 20 Dokumente
        paperless-report list-docs

        # Mit Tag-Filter
        paperless-report list-docs --tag rechnung --limit 50
    """
    config = ctx.obj['config']

    try:
        cache = get_cache(config)
        client = PaperlessClient(config, cache)
        extractor = DocumentExtractor(client, config)

        raw_docs = client.get_documents(
            tags=list(tags) if tags else None,
            year=year
        )

        if not raw_docs:
            click.echo("Keine Dokumente gefunden.")
            return

        documents = extractor.extract_documents(raw_docs[:limit])

        # Tabelle erstellen
        table_data = []
        for doc in documents:
            table_data.append([
                doc.id,
                (doc.effective_date.strftime('%d.%m.%Y')
                 if doc.effective_date else '-'),
                doc.title[:40] + ('...' if len(doc.title) > 40 else ''),
                doc.correspondent[:20] if doc.correspondent else '-',
                (f"{config.currency} {doc.betrag:,.2f}".replace(',', "'")
                 if doc.betrag else '-'),
            ])

        headers = ['ID', 'Datum', 'Titel', 'Korrespondent', 'Betrag']
        click.echo(tabulate(table_data, headers=headers, tablefmt='simple'))
        click.echo(f"\nGesamt: {len(raw_docs)} Dokumente (zeige {min(limit, len(raw_docs))})")

    except PaperlessAPIError as e:
        click.echo(click.style(f"API-Fehler: {e}", fg='red'), err=True)
        sys.exit(1)


@cli.command()
@click.pass_context
def clear_cache(ctx):
    """Löscht den Cache."""
    config = ctx.obj['config']

    cache_path = config.cache_path
    if cache_path.exists():
        import shutil
        shutil.rmtree(cache_path)
        click.echo(click.style("Cache gelöscht.", fg='green'))
    else:
        click.echo("Kein Cache vorhanden.")


@cli.command()
@click.pass_context
def init(ctx):
    """Erstellt eine Beispiel-Konfigurationsdatei."""
    config_file = Path.cwd() / 'config.yaml'

    if config_file.exists():
        if not click.confirm(f"{config_file} existiert bereits. Überschreiben?"):
            return

    # Beispiel-Config kopieren
    example_config = Path(__file__).parent / 'config.yaml.example'
    if example_config.exists():
        import shutil
        shutil.copy(example_config, config_file)
        click.echo(click.style(f"Konfiguration erstellt: {config_file}", fg='green'))
        click.echo("\nBitte bearbeite die Datei und setze:")
        click.echo("  - paperless.url: URL deiner Paperless-Installation")
        click.echo("  - paperless.token: API-Token")
    else:
        click.echo(click.style("Beispiel-Konfiguration nicht gefunden.", fg='red'))


def main():
    """Haupteinstiegspunkt."""
    cli(obj={})


if __name__ == '__main__':
    main()
