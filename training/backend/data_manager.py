"""
Data Manager für Mail Fine-Tuning App
Verwaltet SQLite Datenbank für Mails und Labels
"""

import sqlite3
import json
from datetime import datetime
from typing import List, Dict, Optional
from pathlib import Path


class DataManager:
    def __init__(self, db_path: str = "data/mails.db"):
        self.db_path = Path(db_path)
        self.db_path.parent.mkdir(parents=True, exist_ok=True)
        self.init_db()

    def init_db(self):
        """Initialisiert die Datenbank mit dem Schema"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()

        cursor.execute("""
            CREATE TABLE IF NOT EXISTS mails (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                subject TEXT,
                sender TEXT,
                recipient TEXT,
                date TEXT,
                body TEXT NOT NULL,
                original_format TEXT,
                task_type TEXT DEFAULT 'unlabeled',
                expected_output TEXT,
                status TEXT DEFAULT 'unlabeled',
                created_at TEXT DEFAULT CURRENT_TIMESTAMP,
                updated_at TEXT DEFAULT CURRENT_TIMESTAMP
            )
        """)

        cursor.execute("""
            CREATE TABLE IF NOT EXISTS training_runs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                model_name TEXT NOT NULL,
                start_time TEXT,
                end_time TEXT,
                config TEXT,
                status TEXT,
                final_train_loss REAL,
                final_val_loss REAL,
                checkpoint_path TEXT
            )
        """)

        conn.commit()
        conn.close()

    def add_mail(self, subject: str, sender: str, recipient: str,
                 date: str, body: str, original_format: str) -> int:
        """Fügt eine neue Mail hinzu"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()

        cursor.execute("""
            INSERT INTO mails (subject, sender, recipient, date, body, original_format)
            VALUES (?, ?, ?, ?, ?, ?)
        """, (subject, sender, recipient, date, body, original_format))

        mail_id = cursor.lastrowid
        conn.commit()
        conn.close()

        return mail_id

    def get_all_mails(self, status_filter: Optional[str] = None) -> List[Dict]:
        """Holt alle Mails, optional gefiltert nach Status"""
        conn = sqlite3.connect(self.db_path)
        conn.row_factory = sqlite3.Row
        cursor = conn.cursor()

        if status_filter:
            cursor.execute("SELECT * FROM mails WHERE status = ? ORDER BY id", (status_filter,))
        else:
            cursor.execute("SELECT * FROM mails ORDER BY id")

        rows = cursor.fetchall()
        mails = [dict(row) for row in rows]

        conn.close()
        return mails

    def get_mail(self, mail_id: int) -> Optional[Dict]:
        """Holt eine einzelne Mail"""
        conn = sqlite3.connect(self.db_path)
        conn.row_factory = sqlite3.Row
        cursor = conn.cursor()

        cursor.execute("SELECT * FROM mails WHERE id = ?", (mail_id,))
        row = cursor.fetchone()

        conn.close()
        return dict(row) if row else None

    def update_mail(self, mail_id: int, task_type: Optional[str] = None,
                   expected_output: Optional[str] = None,
                   status: Optional[str] = None,
                   body: Optional[str] = None) -> bool:
        """Aktualisiert eine Mail (Labeling)"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()

        updates = []
        params = []

        if task_type is not None:
            updates.append("task_type = ?")
            params.append(task_type)

        if expected_output is not None:
            updates.append("expected_output = ?")
            params.append(expected_output)

        if status is not None:
            updates.append("status = ?")
            params.append(status)

        if body is not None:
            updates.append("body = ?")
            params.append(body)

        if not updates:
            conn.close()
            return False

        updates.append("updated_at = ?")
        params.append(datetime.now().isoformat())
        params.append(mail_id)

        query = f"UPDATE mails SET {', '.join(updates)} WHERE id = ?"
        cursor.execute(query, params)

        success = cursor.rowcount > 0
        conn.commit()
        conn.close()

        return success

    def delete_mail(self, mail_id: int) -> bool:
        """Löscht eine Mail"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()

        cursor.execute("DELETE FROM mails WHERE id = ?", (mail_id,))
        success = cursor.rowcount > 0

        conn.commit()
        conn.close()

        return success

    def get_statistics(self) -> Dict:
        """Berechnet Statistiken über die Daten"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()

        # Gesamt-Anzahl
        cursor.execute("SELECT COUNT(*) FROM mails")
        total = cursor.fetchone()[0]

        # Nach Status
        cursor.execute("""
            SELECT status, COUNT(*) as count
            FROM mails
            GROUP BY status
        """)
        status_counts = {row[0]: row[1] for row in cursor.fetchall()}

        # Nach Task-Type
        cursor.execute("""
            SELECT task_type, COUNT(*) as count
            FROM mails
            WHERE status = 'labeled'
            GROUP BY task_type
        """)
        task_counts = {row[0]: row[1] for row in cursor.fetchall()}

        # Durchschnittliche Längen (nur gelabelte)
        cursor.execute("""
            SELECT
                AVG(LENGTH(body)) as avg_input_length,
                AVG(LENGTH(expected_output)) as avg_output_length
            FROM mails
            WHERE status = 'labeled'
        """)
        lengths = cursor.fetchone()

        conn.close()

        labeled_count = status_counts.get('labeled', 0)

        return {
            'total': total,
            'labeled': labeled_count,
            'unlabeled': status_counts.get('unlabeled', 0),
            'skipped': status_counts.get('skip', 0),
            'task_distribution': task_counts,
            'avg_input_length': round(lengths[0]) if lengths[0] else 0,
            'avg_output_length': round(lengths[1]) if lengths[1] else 0,
            'sufficient_data': labeled_count >= 50
        }

    def export_training_data(self, train_split: float = 0.9) -> tuple[List[Dict], List[Dict]]:
        """Exportiert gelabelte Daten für Training"""
        import random

        conn = sqlite3.connect(self.db_path)
        conn.row_factory = sqlite3.Row
        cursor = conn.cursor()

        cursor.execute("""
            SELECT body, task_type, expected_output
            FROM mails
            WHERE status = 'labeled' AND expected_output IS NOT NULL
            ORDER BY RANDOM()
        """)

        rows = cursor.fetchall()
        conn.close()

        if not rows:
            return [], []

        data = [dict(row) for row in rows]

        # Shuffle
        random.shuffle(data)

        # Split
        split_idx = int(len(data) * train_split)
        train_data = data[:split_idx]
        val_data = data[split_idx:]

        return train_data, val_data

    def save_training_run(self, model_name: str, config: Dict,
                         checkpoint_path: str) -> int:
        """Speichert einen Training-Run"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()

        cursor.execute("""
            INSERT INTO training_runs
            (model_name, start_time, config, status, checkpoint_path)
            VALUES (?, ?, ?, ?, ?)
        """, (
            model_name,
            datetime.now().isoformat(),
            json.dumps(config),
            'running',
            checkpoint_path
        ))

        run_id = cursor.lastrowid
        conn.commit()
        conn.close()

        return run_id

    def update_training_run(self, run_id: int, status: str,
                          train_loss: Optional[float] = None,
                          val_loss: Optional[float] = None):
        """Aktualisiert einen Training-Run"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()

        cursor.execute("""
            UPDATE training_runs
            SET status = ?,
                end_time = ?,
                final_train_loss = COALESCE(?, final_train_loss),
                final_val_loss = COALESCE(?, final_val_loss)
            WHERE id = ?
        """, (status, datetime.now().isoformat(), train_loss, val_loss, run_id))

        conn.commit()
        conn.close()
