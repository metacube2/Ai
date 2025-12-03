"""
Mail Parser für verschiedene Formate
Bereinigt und normalisiert Mail-Inhalte
"""

import email
import mailbox
import re
from bs4 import BeautifulSoup
from typing import List, Dict, Optional
from pathlib import Path
import chardet


class MailParser:
    """Parst und bereinigt Mail-Dateien"""

    # Häufige Footer/Disclaimer Pattern
    FOOTER_PATTERNS = [
        r'(?i)^--\s*$.*',  # Standard signature delimiter
        r'(?i)Diese E-Mail.*vertraulich.*',
        r'(?i)This email.*confidential.*',
        r'(?i)Disclaimer:.*',
        r'(?i)Get Outlook for.*',
        r'(?i)Sent from my iPhone.*',
        r'(?i)Von meinem.*gesendet.*',
        r'(?i)Diese Nachricht.*Virenfrei.*',
    ]

    @staticmethod
    def detect_encoding(file_path: Path) -> str:
        """Erkennt das Encoding einer Datei"""
        with open(file_path, 'rb') as f:
            raw_data = f.read()
            result = chardet.detect(raw_data)
            return result['encoding'] or 'utf-8'

    @staticmethod
    def html_to_text(html: str) -> str:
        """Konvertiert HTML zu Plain Text"""
        soup = BeautifulSoup(html, 'html.parser')

        # Entferne Script und Style Tags
        for script in soup(['script', 'style']):
            script.decompose()

        # Extrahiere Text
        text = soup.get_text()

        # Bereinige Whitespace
        lines = (line.strip() for line in text.splitlines())
        chunks = (phrase.strip() for line in lines for phrase in line.split("  "))
        text = ' '.join(chunk for chunk in chunks if chunk)

        return text

    @staticmethod
    def remove_multiple_newlines(text: str) -> str:
        """Entfernt mehrfache Leerzeilen"""
        return re.sub(r'\n{3,}', '\n\n', text)

    @staticmethod
    def remove_footers(text: str) -> str:
        """Entfernt häufige Footer und Disclaimer"""
        for pattern in MailParser.FOOTER_PATTERNS:
            # Suche Pattern und entferne alles danach
            match = re.search(pattern, text, re.MULTILINE | re.DOTALL)
            if match:
                text = text[:match.start()].strip()

        return text

    @staticmethod
    def clean_quoted_text(text: str) -> str:
        """Entfernt oder markiert quoted Text (> oder |)"""
        lines = text.split('\n')
        cleaned_lines = []

        for line in lines:
            # Überspringe Zeilen die mit > oder | beginnen (quoted text)
            if not line.strip().startswith('>') and not line.strip().startswith('|'):
                cleaned_lines.append(line)

        return '\n'.join(cleaned_lines)

    @staticmethod
    def normalize_whitespace(text: str) -> str:
        """Normalisiert Whitespace"""
        # Entferne trailing spaces
        lines = [line.rstrip() for line in text.split('\n')]
        text = '\n'.join(lines)

        # Entferne mehrfache Spaces
        text = re.sub(r' {2,}', ' ', text)

        # Entferne mehrfache Leerzeilen
        text = MailParser.remove_multiple_newlines(text)

        return text.strip()

    @staticmethod
    def clean_text(text: str, is_html: bool = False) -> str:
        """Vollständige Bereinigung eines Texts"""
        if is_html:
            text = MailParser.html_to_text(text)

        text = MailParser.remove_footers(text)
        text = MailParser.clean_quoted_text(text)
        text = MailParser.normalize_whitespace(text)

        return text

    @staticmethod
    def parse_eml(file_path: Path) -> Dict:
        """Parst eine .eml Datei"""
        encoding = MailParser.detect_encoding(file_path)

        with open(file_path, 'r', encoding=encoding, errors='ignore') as f:
            msg = email.message_from_file(f)

        subject = msg.get('Subject', 'No Subject')
        sender = msg.get('From', 'Unknown')
        recipient = msg.get('To', 'Unknown')
        date = msg.get('Date', '')

        # Body extrahieren
        body = ""
        is_html = False

        if msg.is_multipart():
            for part in msg.walk():
                content_type = part.get_content_type()
                if content_type == 'text/plain':
                    body = part.get_payload(decode=True).decode(errors='ignore')
                    break
                elif content_type == 'text/html' and not body:
                    body = part.get_payload(decode=True).decode(errors='ignore')
                    is_html = True
        else:
            body = msg.get_payload(decode=True).decode(errors='ignore')
            if msg.get_content_type() == 'text/html':
                is_html = True

        # Bereinige Body
        body = MailParser.clean_text(body, is_html)

        return {
            'subject': subject,
            'sender': sender,
            'recipient': recipient,
            'date': date,
            'body': body,
            'original_format': 'eml'
        }

    @staticmethod
    def parse_mbox(file_path: Path) -> List[Dict]:
        """Parst eine .mbox Datei"""
        mails = []

        try:
            mbox = mailbox.mbox(str(file_path))

            for message in mbox:
                subject = message.get('Subject', 'No Subject')
                sender = message.get('From', 'Unknown')
                recipient = message.get('To', 'Unknown')
                date = message.get('Date', '')

                body = ""
                is_html = False

                if message.is_multipart():
                    for part in message.walk():
                        content_type = part.get_content_type()
                        if content_type == 'text/plain':
                            payload = part.get_payload(decode=True)
                            if payload:
                                body = payload.decode(errors='ignore')
                            break
                        elif content_type == 'text/html' and not body:
                            payload = part.get_payload(decode=True)
                            if payload:
                                body = payload.decode(errors='ignore')
                                is_html = True
                else:
                    payload = message.get_payload(decode=True)
                    if payload:
                        body = payload.decode(errors='ignore')
                        if message.get_content_type() == 'text/html':
                            is_html = True

                body = MailParser.clean_text(body, is_html)

                mails.append({
                    'subject': subject,
                    'sender': sender,
                    'recipient': recipient,
                    'date': date,
                    'body': body,
                    'original_format': 'mbox'
                })

        except Exception as e:
            raise Exception(f"Error parsing mbox: {str(e)}")

        return mails

    @staticmethod
    def parse_txt(file_path: Path) -> Dict:
        """Parst eine .txt Datei (simple Mail als Text)"""
        encoding = MailParser.detect_encoding(file_path)

        with open(file_path, 'r', encoding=encoding, errors='ignore') as f:
            content = f.read()

        # Einfache Struktur: Versuche Subject/From/To zu erkennen
        lines = content.split('\n')
        subject = 'No Subject'
        sender = 'Unknown'
        recipient = 'Unknown'
        date = ''
        body_start = 0

        for i, line in enumerate(lines[:10]):  # Erste 10 Zeilen prüfen
            if line.lower().startswith('subject:'):
                subject = line[8:].strip()
                body_start = max(body_start, i + 1)
            elif line.lower().startswith('from:'):
                sender = line[5:].strip()
                body_start = max(body_start, i + 1)
            elif line.lower().startswith('to:'):
                recipient = line[3:].strip()
                body_start = max(body_start, i + 1)
            elif line.lower().startswith('date:'):
                date = line[5:].strip()
                body_start = max(body_start, i + 1)

        # Body ist der Rest
        body = '\n'.join(lines[body_start:])
        body = MailParser.clean_text(body)

        return {
            'subject': subject,
            'sender': sender,
            'recipient': recipient,
            'date': date,
            'body': body,
            'original_format': 'txt'
        }

    @staticmethod
    def parse_file(file_path: Path) -> List[Dict]:
        """Parst eine Mail-Datei basierend auf Endung"""
        suffix = file_path.suffix.lower()

        if suffix == '.eml':
            return [MailParser.parse_eml(file_path)]
        elif suffix == '.mbox':
            return MailParser.parse_mbox(file_path)
        elif suffix == '.txt':
            return [MailParser.parse_txt(file_path)]
        else:
            raise ValueError(f"Unsupported file format: {suffix}")
