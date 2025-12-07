"""
Konfigurationsmanagement für das Paperless Finance Report Tool.

Lädt und validiert die YAML-Konfiguration.
"""

import os
import sys
from pathlib import Path
from typing import Any, Optional

import yaml


class ConfigError(Exception):
    """Fehler bei der Konfiguration."""
    pass


class Config:
    """Konfigurationsklasse für das Paperless Finance Report Tool."""

    DEFAULT_CONFIG = {
        'paperless': {
            'url': 'http://localhost:8000',
            'token': '',
            'timeout': 30,
        },
        'custom_fields': {
            'betrag': 'betrag',
            'rechnungsdatum': 'rechnungsdatum',
            'kategorie': 'kategorie',
            'zahlungsart': 'zahlungsart',
            'periode': 'periode',
            'notiz': 'notiz',
        },
        'defaults': {
            'currency': 'CHF',
            'date_field': 'archive_date',
            'invoice_tag': 'rechnung',
        },
        'tags': ['rechnung'],
        'categories': [],
        'output': {
            'format': 'html',
            'path': './output',
            'filename_pattern': 'finanzbericht_{year}',
        },
        'cache': {
            'enabled': True,
            'path': './.cache',
            'ttl': 3600,
        },
        'logging': {
            'level': 'INFO',
            'file': '',
            'colorize': True,
        },
    }

    def __init__(self, config_path: Optional[str] = None):
        """
        Initialisiert die Konfiguration.

        Args:
            config_path: Pfad zur config.yaml. Falls None, wird im aktuellen
                        Verzeichnis und im Script-Verzeichnis gesucht.
        """
        self._config = self.DEFAULT_CONFIG.copy()
        self._config_path = self._find_config(config_path)

        if self._config_path:
            self._load_config()

        self._validate_config()

    def _find_config(self, config_path: Optional[str]) -> Optional[Path]:
        """Sucht nach der Konfigurationsdatei."""
        if config_path:
            path = Path(config_path)
            if path.exists():
                return path
            raise ConfigError(f"Konfigurationsdatei nicht gefunden: {config_path}")

        # Suchpfade
        search_paths = [
            Path.cwd() / 'config.yaml',
            Path.cwd() / 'config.yml',
            Path(__file__).parent / 'config.yaml',
            Path(__file__).parent / 'config.yml',
            Path.home() / '.config' / 'paperless-report' / 'config.yaml',
        ]

        # Umgebungsvariable prüfen
        env_path = os.environ.get('PAPERLESS_REPORT_CONFIG')
        if env_path:
            search_paths.insert(0, Path(env_path))

        for path in search_paths:
            if path.exists():
                return path

        return None

    def _load_config(self) -> None:
        """Lädt die Konfiguration aus der YAML-Datei."""
        try:
            with open(self._config_path, 'r', encoding='utf-8') as f:
                user_config = yaml.safe_load(f) or {}

            # Rekursives Merge der Konfiguration
            self._config = self._deep_merge(self._config, user_config)

        except yaml.YAMLError as e:
            raise ConfigError(f"Fehler beim Parsen der Konfiguration: {e}")
        except IOError as e:
            raise ConfigError(f"Fehler beim Lesen der Konfiguration: {e}")

    def _deep_merge(self, base: dict, override: dict) -> dict:
        """Führt zwei Dictionaries rekursiv zusammen."""
        result = base.copy()

        for key, value in override.items():
            if key in result and isinstance(result[key], dict) and isinstance(value, dict):
                result[key] = self._deep_merge(result[key], value)
            else:
                result[key] = value

        return result

    def _validate_config(self) -> None:
        """Validiert die Konfiguration."""
        # Paperless URL prüfen
        url = self.get('paperless.url', '')
        if not url:
            raise ConfigError("Paperless URL muss konfiguriert werden")

        # Token prüfen (kann auch über Umgebungsvariable kommen)
        token = self.get('paperless.token', '') or os.environ.get('PAPERLESS_TOKEN', '')
        if not token:
            raise ConfigError(
                "Paperless API-Token muss konfiguriert werden.\n"
                "Setze 'paperless.token' in config.yaml oder die Umgebungsvariable PAPERLESS_TOKEN"
            )

        # Token aus Umgebungsvariable übernehmen falls nicht in Config
        if not self.get('paperless.token'):
            self._config['paperless']['token'] = token

    def get(self, key: str, default: Any = None) -> Any:
        """
        Holt einen Konfigurationswert über Punkt-Notation.

        Args:
            key: Schlüssel in Punkt-Notation, z.B. 'paperless.url'
            default: Standardwert falls Schlüssel nicht existiert

        Returns:
            Der Konfigurationswert oder der Standardwert
        """
        keys = key.split('.')
        value = self._config

        try:
            for k in keys:
                value = value[k]
            return value
        except (KeyError, TypeError):
            return default

    def __getitem__(self, key: str) -> Any:
        """Ermöglicht Zugriff via config['key']."""
        value = self.get(key)
        if value is None:
            raise KeyError(key)
        return value

    @property
    def paperless_url(self) -> str:
        """Paperless Base-URL."""
        url = self.get('paperless.url', '')
        return url.rstrip('/')

    @property
    def paperless_token(self) -> str:
        """Paperless API-Token."""
        return self.get('paperless.token', '')

    @property
    def timeout(self) -> int:
        """Request-Timeout in Sekunden."""
        return self.get('paperless.timeout', 30)

    @property
    def currency(self) -> str:
        """Standardwährung."""
        return self.get('defaults.currency', 'CHF')

    @property
    def date_field(self) -> str:
        """Datumsfeld für Filterung."""
        return self.get('defaults.date_field', 'archive_date')

    @property
    def output_format(self) -> str:
        """Standard-Ausgabeformat."""
        return self.get('output.format', 'html')

    @property
    def output_path(self) -> Path:
        """Ausgabeverzeichnis."""
        return Path(self.get('output.path', './output'))

    @property
    def cache_enabled(self) -> bool:
        """Cache aktiviert."""
        return self.get('cache.enabled', True)

    @property
    def cache_path(self) -> Path:
        """Cache-Verzeichnis."""
        return Path(self.get('cache.path', './.cache'))

    @property
    def cache_ttl(self) -> int:
        """Cache-Gültigkeit in Sekunden."""
        return self.get('cache.ttl', 3600)

    @property
    def log_level(self) -> str:
        """Log-Level."""
        return self.get('logging.level', 'INFO')

    @property
    def custom_field_names(self) -> dict:
        """Mapping der Custom Field Namen."""
        return self.get('custom_fields', {})

    def get_custom_field_name(self, internal_name: str) -> str:
        """Holt den Paperless-Feldnamen für ein internes Feld."""
        return self.get(f'custom_fields.{internal_name}', internal_name)


# Globale Config-Instanz (lazy loading)
_config: Optional[Config] = None


def get_config(config_path: Optional[str] = None) -> Config:
    """
    Holt die globale Konfiguration.

    Args:
        config_path: Optionaler Pfad zur Konfigurationsdatei

    Returns:
        Config-Instanz
    """
    global _config

    if _config is None or config_path is not None:
        _config = Config(config_path)

    return _config


def reset_config() -> None:
    """Setzt die globale Konfiguration zurück (für Tests)."""
    global _config
    _config = None
