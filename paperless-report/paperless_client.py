"""
Paperless-ngx API Client.

Handhabt die Kommunikation mit der Paperless REST-API inkl. Paginierung und Caching.
"""

import hashlib
import json
import logging
from datetime import datetime
from pathlib import Path
from typing import Any, Dict, Generator, List, Optional, Union
from urllib.parse import urlencode, urljoin

import requests
from requests.adapters import HTTPAdapter
from urllib3.util.retry import Retry

from config import Config, get_config

logger = logging.getLogger(__name__)


class PaperlessAPIError(Exception):
    """Fehler bei der API-Kommunikation."""

    def __init__(self, message: str, status_code: Optional[int] = None, response: Optional[dict] = None):
        super().__init__(message)
        self.status_code = status_code
        self.response = response


class PaperlessClient:
    """Client für die Paperless-ngx REST-API."""

    # API-Endpunkte
    ENDPOINTS = {
        'documents': '/api/documents/',
        'tags': '/api/tags/',
        'correspondents': '/api/correspondents/',
        'document_types': '/api/document_types/',
        'custom_fields': '/api/custom_fields/',
        'storage_paths': '/api/storage_paths/',
    }

    def __init__(self, config: Optional[Config] = None, cache: Optional[Any] = None):
        """
        Initialisiert den API-Client.

        Args:
            config: Konfigurationsobjekt. Falls None, wird globale Config verwendet.
            cache: Optionales Cache-Objekt (diskcache.Cache)
        """
        self.config = config or get_config()
        self.base_url = self.config.paperless_url
        self.token = self.config.paperless_token
        self.timeout = self.config.timeout
        self.cache = cache

        # Session mit Retry-Logik erstellen
        self.session = self._create_session()

        # Cached Metadata
        self._custom_fields_cache: Optional[Dict[int, dict]] = None
        self._tags_cache: Optional[Dict[int, dict]] = None
        self._correspondents_cache: Optional[Dict[int, dict]] = None
        self._document_types_cache: Optional[Dict[int, dict]] = None

    def _create_session(self) -> requests.Session:
        """Erstellt eine Session mit Retry-Konfiguration."""
        session = requests.Session()

        # Retry-Strategie
        retry_strategy = Retry(
            total=3,
            backoff_factor=1,
            status_forcelist=[429, 500, 502, 503, 504],
        )

        adapter = HTTPAdapter(max_retries=retry_strategy)
        session.mount('http://', adapter)
        session.mount('https://', adapter)

        # Standard-Header
        session.headers.update({
            'Authorization': f'Token {self.token}',
            'Accept': 'application/json',
            'Content-Type': 'application/json',
        })

        return session

    def _get_cache_key(self, endpoint: str, params: Optional[dict] = None) -> str:
        """Generiert einen Cache-Schlüssel."""
        key_data = f"{self.base_url}{endpoint}"
        if params:
            key_data += json.dumps(params, sort_keys=True)
        return hashlib.md5(key_data.encode()).hexdigest()

    def _request(
        self,
        method: str,
        endpoint: str,
        params: Optional[dict] = None,
        data: Optional[dict] = None,
        use_cache: bool = True
    ) -> dict:
        """
        Führt einen API-Request durch.

        Args:
            method: HTTP-Methode (GET, POST, etc.)
            endpoint: API-Endpunkt (relativ zur Base-URL)
            params: Query-Parameter
            data: Request-Body
            use_cache: Cache verwenden (nur für GET)

        Returns:
            API-Response als Dictionary
        """
        url = urljoin(self.base_url, endpoint)

        # Cache prüfen (nur GET-Requests)
        if method.upper() == 'GET' and use_cache and self.cache:
            cache_key = self._get_cache_key(endpoint, params)
            cached = self.cache.get(cache_key)
            if cached is not None:
                logger.debug(f"Cache hit für {endpoint}")
                return cached

        logger.debug(f"API Request: {method} {url} params={params}")

        try:
            response = self.session.request(
                method=method,
                url=url,
                params=params,
                json=data,
                timeout=self.timeout
            )

            response.raise_for_status()
            result = response.json()

            # In Cache speichern (nur GET)
            if method.upper() == 'GET' and use_cache and self.cache:
                self.cache.set(cache_key, result, expire=self.config.cache_ttl)

            return result

        except requests.exceptions.HTTPError as e:
            error_msg = f"HTTP-Fehler: {e}"
            try:
                error_detail = e.response.json()
                error_msg = f"{error_msg} - {error_detail}"
            except (ValueError, AttributeError):
                pass

            raise PaperlessAPIError(
                error_msg,
                status_code=e.response.status_code if e.response else None
            )

        except requests.exceptions.ConnectionError as e:
            raise PaperlessAPIError(f"Verbindungsfehler: Kann {self.base_url} nicht erreichen")

        except requests.exceptions.Timeout as e:
            raise PaperlessAPIError(f"Timeout nach {self.timeout}s")

        except requests.exceptions.RequestException as e:
            raise PaperlessAPIError(f"Request-Fehler: {e}")

    def _get_paginated(
        self,
        endpoint: str,
        params: Optional[dict] = None,
        page_size: int = 100
    ) -> Generator[dict, None, None]:
        """
        Holt alle Seiten eines paginierten Endpunkts.

        Args:
            endpoint: API-Endpunkt
            params: Zusätzliche Query-Parameter
            page_size: Anzahl Ergebnisse pro Seite

        Yields:
            Einzelne Ergebnis-Objekte
        """
        params = params or {}
        params['page_size'] = page_size
        page = 1

        while True:
            params['page'] = page
            logger.debug(f"Lade Seite {page} von {endpoint}")

            response = self._request('GET', endpoint, params=params)

            results = response.get('results', [])
            for item in results:
                yield item

            # Prüfen ob weitere Seiten existieren
            if not response.get('next'):
                break

            page += 1

    def test_connection(self) -> bool:
        """
        Testet die Verbindung zur Paperless-API.

        Returns:
            True wenn Verbindung erfolgreich
        """
        try:
            self._request('GET', self.ENDPOINTS['tags'], params={'page_size': 1})
            return True
        except PaperlessAPIError:
            return False

    # ==================== Custom Fields ====================

    def get_custom_fields(self, refresh: bool = False) -> Dict[int, dict]:
        """
        Holt alle Custom Field Definitionen.

        Args:
            refresh: Cache ignorieren und neu laden

        Returns:
            Dictionary mit Field-ID als Key und Definition als Value
        """
        if self._custom_fields_cache is not None and not refresh:
            return self._custom_fields_cache

        fields = {}
        for field in self._get_paginated(self.ENDPOINTS['custom_fields']):
            fields[field['id']] = field

        self._custom_fields_cache = fields
        logger.info(f"Geladen: {len(fields)} Custom Fields")
        return fields

    def get_custom_field_by_name(self, name: str) -> Optional[dict]:
        """
        Findet ein Custom Field anhand des Namens.

        Args:
            name: Name des Custom Fields

        Returns:
            Field-Definition oder None
        """
        fields = self.get_custom_fields()
        for field in fields.values():
            if field['name'].lower() == name.lower():
                return field
        return None

    # ==================== Tags ====================

    def get_tags(self, refresh: bool = False) -> Dict[int, dict]:
        """
        Holt alle Tags.

        Returns:
            Dictionary mit Tag-ID als Key
        """
        if self._tags_cache is not None and not refresh:
            return self._tags_cache

        tags = {}
        for tag in self._get_paginated(self.ENDPOINTS['tags']):
            tags[tag['id']] = tag

        self._tags_cache = tags
        logger.info(f"Geladen: {len(tags)} Tags")
        return tags

    def get_tag_by_name(self, name: str) -> Optional[dict]:
        """Findet einen Tag anhand des Namens."""
        tags = self.get_tags()
        for tag in tags.values():
            if tag['name'].lower() == name.lower():
                return tag
        return None

    def get_tag_id(self, name: str) -> Optional[int]:
        """Holt die ID eines Tags anhand des Namens."""
        tag = self.get_tag_by_name(name)
        return tag['id'] if tag else None

    # ==================== Correspondents ====================

    def get_correspondents(self, refresh: bool = False) -> Dict[int, dict]:
        """
        Holt alle Korrespondenten.

        Returns:
            Dictionary mit Correspondent-ID als Key
        """
        if self._correspondents_cache is not None and not refresh:
            return self._correspondents_cache

        correspondents = {}
        for corr in self._get_paginated(self.ENDPOINTS['correspondents']):
            correspondents[corr['id']] = corr

        self._correspondents_cache = correspondents
        logger.info(f"Geladen: {len(correspondents)} Korrespondenten")
        return correspondents

    def get_correspondent_name(self, correspondent_id: int) -> str:
        """Holt den Namen eines Korrespondenten."""
        correspondents = self.get_correspondents()
        corr = correspondents.get(correspondent_id)
        return corr['name'] if corr else f"Unbekannt ({correspondent_id})"

    # ==================== Document Types ====================

    def get_document_types(self, refresh: bool = False) -> Dict[int, dict]:
        """Holt alle Dokumenttypen."""
        if self._document_types_cache is not None and not refresh:
            return self._document_types_cache

        doc_types = {}
        for dt in self._get_paginated(self.ENDPOINTS['document_types']):
            doc_types[dt['id']] = dt

        self._document_types_cache = doc_types
        return doc_types

    # ==================== Documents ====================

    def get_documents(
        self,
        tags: Optional[List[Union[int, str]]] = None,
        correspondent: Optional[Union[int, str]] = None,
        document_type: Optional[Union[int, str]] = None,
        year: Optional[int] = None,
        month: Optional[int] = None,
        date_from: Optional[datetime] = None,
        date_to: Optional[datetime] = None,
        query: Optional[str] = None,
        ordering: str = '-archive_date',
        **extra_filters
    ) -> List[dict]:
        """
        Holt Dokumente mit optionalen Filtern.

        Args:
            tags: Liste von Tag-IDs oder Namen
            correspondent: Korrespondent-ID oder Name
            document_type: Dokumenttyp-ID oder Name
            year: Jahr (für archive_date)
            month: Monat (1-12, nur zusammen mit year)
            date_from: Startdatum
            date_to: Enddatum
            query: Volltextsuche
            ordering: Sortierung
            **extra_filters: Zusätzliche Filter für die API

        Returns:
            Liste von Dokumenten
        """
        params = {'ordering': ordering}

        # Tags verarbeiten
        if tags:
            tag_ids = []
            for tag in tags:
                if isinstance(tag, int):
                    tag_ids.append(tag)
                else:
                    tag_id = self.get_tag_id(tag)
                    if tag_id:
                        tag_ids.append(tag_id)
                    else:
                        logger.warning(f"Tag nicht gefunden: {tag}")

            if tag_ids:
                params['tags__id__in'] = ','.join(str(t) for t in tag_ids)

        # Korrespondent
        if correspondent:
            if isinstance(correspondent, str):
                correspondents = self.get_correspondents()
                for c in correspondents.values():
                    if c['name'].lower() == correspondent.lower():
                        params['correspondent__id'] = c['id']
                        break
            else:
                params['correspondent__id'] = correspondent

        # Dokumenttyp
        if document_type:
            if isinstance(document_type, str):
                doc_types = self.get_document_types()
                for dt in doc_types.values():
                    if dt['name'].lower() == document_type.lower():
                        params['document_type__id'] = dt['id']
                        break
            else:
                params['document_type__id'] = document_type

        # Datumsfilter
        date_field = self.config.date_field

        if year:
            if month:
                # Spezifischer Monat
                if month == 12:
                    next_year = year + 1
                    next_month = 1
                else:
                    next_year = year
                    next_month = month + 1

                params[f'{date_field}__gte'] = f'{year}-{month:02d}-01'
                params[f'{date_field}__lt'] = f'{next_year}-{next_month:02d}-01'
            else:
                # Ganzes Jahr
                params[f'{date_field}__year'] = year

        if date_from:
            params[f'{date_field}__gte'] = date_from.strftime('%Y-%m-%d')

        if date_to:
            params[f'{date_field}__lte'] = date_to.strftime('%Y-%m-%d')

        # Volltextsuche
        if query:
            params['query'] = query

        # Extra-Filter
        params.update(extra_filters)

        # Alle Dokumente abrufen
        documents = list(self._get_paginated(self.ENDPOINTS['documents'], params))
        logger.info(f"Geladen: {len(documents)} Dokumente")

        return documents

    def get_document(self, document_id: int) -> dict:
        """
        Holt ein einzelnes Dokument.

        Args:
            document_id: ID des Dokuments

        Returns:
            Dokument-Dictionary
        """
        endpoint = f"{self.ENDPOINTS['documents']}{document_id}/"
        return self._request('GET', endpoint)

    def get_document_url(self, document_id: int) -> str:
        """Generiert die Web-URL für ein Dokument."""
        return f"{self.base_url}/documents/{document_id}/details"

    def get_document_download_url(self, document_id: int) -> str:
        """Generiert die Download-URL für ein Dokument."""
        return f"{self.base_url}/api/documents/{document_id}/download/"

    # ==================== Hilfsmethoden ====================

    def resolve_all_metadata(self, documents: List[dict]) -> List[dict]:
        """
        Erweitert Dokumente um aufgelöste Metadaten (Tag-Namen, Korrespondent-Namen, etc.).

        Args:
            documents: Liste von Dokumenten

        Returns:
            Erweiterte Dokumente
        """
        tags = self.get_tags()
        correspondents = self.get_correspondents()
        doc_types = self.get_document_types()
        custom_fields = self.get_custom_fields()

        for doc in documents:
            # Tag-Namen
            doc['tag_names'] = [
                tags.get(tid, {}).get('name', f'Unknown-{tid}')
                for tid in doc.get('tags', [])
            ]

            # Korrespondent-Name
            corr_id = doc.get('correspondent')
            doc['correspondent_name'] = (
                correspondents.get(corr_id, {}).get('name', '')
                if corr_id else ''
            )

            # Dokumenttyp-Name
            dt_id = doc.get('document_type')
            doc['document_type_name'] = (
                doc_types.get(dt_id, {}).get('name', '')
                if dt_id else ''
            )

            # Custom Fields aufbereiten
            doc['custom_fields_resolved'] = {}
            for cf in doc.get('custom_fields', []):
                field_id = cf.get('field')
                field_def = custom_fields.get(field_id, {})
                field_name = field_def.get('name', f'field_{field_id}')
                doc['custom_fields_resolved'][field_name] = {
                    'value': cf.get('value'),
                    'type': field_def.get('data_type', 'string'),
                    'field_id': field_id
                }

            # URL hinzufügen
            doc['web_url'] = self.get_document_url(doc['id'])

        return documents

    def get_statistics(self) -> dict:
        """
        Holt allgemeine Statistiken.

        Returns:
            Dictionary mit Statistiken
        """
        return {
            'total_documents': len(list(self._get_paginated(
                self.ENDPOINTS['documents'],
                params={'page_size': 1}
            ))),
            'total_tags': len(self.get_tags()),
            'total_correspondents': len(self.get_correspondents()),
            'total_custom_fields': len(self.get_custom_fields()),
        }
