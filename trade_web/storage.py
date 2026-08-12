from __future__ import annotations

import json
import sqlite3
import time
from pathlib import Path
from typing import Any


class TradeStore:
    def __init__(self, path: Path):
        self.path = path
        self.path.parent.mkdir(parents=True, exist_ok=True)
        self._init()

    def _connect(self) -> sqlite3.Connection:
        connection = sqlite3.connect(self.path)
        connection.row_factory = sqlite3.Row
        return connection

    def _init(self) -> None:
        with self._connect() as db:
            db.execute(
                """
                create table if not exists runs (
                    id integer primary key autoincrement,
                    kind text not null,
                    created_at integer not null,
                    label text,
                    payload text not null
                )
                """
            )
            db.execute(
                """
                create table if not exists paper_orders (
                    id integer primary key autoincrement,
                    created_at integer not null,
                    symbol text not null,
                    side text not null,
                    status text not null,
                    quantity real not null,
                    entry real not null,
                    stop real not null,
                    target real not null,
                    risk_amount real not null,
                    payload text not null
                )
                """
            )

    def save_run(self, kind: str, payload: dict[str, Any], label: str | None = None) -> int:
        with self._connect() as db:
            cursor = db.execute(
                "insert into runs(kind, created_at, label, payload) values(?, ?, ?, ?)",
                (kind, int(time.time()), label, json.dumps(payload)),
            )
            return int(cursor.lastrowid)

    def recent_runs(self, kind: str | None = None, limit: int = 25) -> list[dict[str, Any]]:
        query = "select id, kind, created_at, label, payload from runs"
        params: list[Any] = []
        if kind:
            query += " where kind = ?"
            params.append(kind)
        query += " order by id desc limit ?"
        params.append(limit)
        with self._connect() as db:
            rows = db.execute(query, params).fetchall()
        return [
            {
                "id": row["id"],
                "kind": row["kind"],
                "created_at": row["created_at"],
                "label": row["label"],
                "payload": json.loads(row["payload"]),
            }
            for row in rows
        ]

    def save_paper_order(self, order: dict[str, Any]) -> int:
        with self._connect() as db:
            cursor = db.execute(
                """
                insert into paper_orders(
                    created_at, symbol, side, status, quantity, entry, stop, target, risk_amount, payload
                ) values(?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    int(time.time()),
                    order["symbol"],
                    order["side"],
                    order["status"],
                    order["quantity"],
                    order["entry"],
                    order["stop"],
                    order["target"],
                    order["risk_amount"],
                    json.dumps(order),
                ),
            )
            return int(cursor.lastrowid)

    def recent_paper_orders(self, limit: int = 25) -> list[dict[str, Any]]:
        with self._connect() as db:
            rows = db.execute(
                "select id, created_at, payload from paper_orders order by id desc limit ?",
                (limit,),
            ).fetchall()
        result = []
        for row in rows:
            payload = json.loads(row["payload"])
            payload["id"] = row["id"]
            payload["created_at"] = row["created_at"]
            result.append(payload)
        return result
