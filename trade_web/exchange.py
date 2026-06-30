from __future__ import annotations

import time
from typing import Any


class ExchangeGuard:
    def status(self, config: dict[str, Any]) -> dict[str, Any]:
        execution = config.get("execution", {})
        blockers = self._blockers(execution)
        return {
            "exchange": execution.get("exchange", "binance"),
            "mode": execution.get("mode", "paper"),
            "testnet": bool(execution.get("testnet", True)),
            "live_ready": not blockers,
            "blockers": blockers,
        }

    def place_order(self, config: dict[str, Any], order: dict[str, Any]) -> dict[str, Any]:
        execution = config.get("execution", {})
        blockers = self._blockers(execution)
        if blockers:
            return {
                "status": "blocked",
                "created_at": int(time.time()),
                "blockers": blockers,
                "order": order,
                "message": "Live-Order wurde nicht gesendet. Safety-Guard ist aktiv.",
            }
        return {
            "status": "not_implemented",
            "created_at": int(time.time()),
            "order": order,
            "message": "Connector ist vorbereitet, aber echte REST-Order-Signatur ist bewusst noch nicht aktiv.",
        }

    def _blockers(self, execution: dict[str, Any]) -> list[str]:
        blockers = []
        if execution.get("mode", "paper") != "live":
            blockers.append("execution.mode ist nicht live")
        if execution.get("kill_switch", True):
            blockers.append("kill_switch aktiv")
        if not execution.get("allow_live_orders", False):
            blockers.append("allow_live_orders false")
        if execution.get("require_manual_confirm", True):
            blockers.append("manuelle Freigabe erforderlich")
        if not execution.get("api_key_env") or not execution.get("api_secret_env"):
            blockers.append("API-Key-ENV nicht konfiguriert")
        return blockers
