"""
Paperless Finance Report Tool

Generiert Finanzberichte aus Paperless-ngx Dokumenten.
"""

__version__ = '1.0.0'
__author__ = 'Your Name'

from config import Config, get_config
from paperless_client import PaperlessClient, PaperlessAPIError
from extractor import DocumentExtractor, DataAggregator, FinanceDocument
from report_generator import ReportGenerator

__all__ = [
    'Config',
    'get_config',
    'PaperlessClient',
    'PaperlessAPIError',
    'DocumentExtractor',
    'DataAggregator',
    'FinanceDocument',
    'ReportGenerator',
]
