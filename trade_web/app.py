from __future__ import annotations

from pathlib import Path
import os
import threading
import time
import uuid

from flask import Flask, jsonify, render_template, request

from exchange import ExchangeGuard
from trading_engine import ConfigStore, TradingAnalyzer
from storage import TradeStore


BASE_DIR = Path(__file__).resolve().parent
app = Flask(__name__)
config_store = ConfigStore(BASE_DIR / "config.json")
store = TradeStore(BASE_DIR / "data" / "trade_web.sqlite3")
analyzer = TradingAnalyzer()
exchange_guard = ExchangeGuard()
optimizer_jobs: dict[str, dict] = {}
optimizer_lock = threading.Lock()


@app.get("/")
def index():
    return render_template("index.html")


@app.get("/api/config")
def get_config():
    return jsonify(config_store.load())


@app.post("/api/config")
def update_config():
    config = config_store.load()
    payload = request.get_json(silent=True) or {}
    symbol = str(payload.get("symbol", config.get("symbol", "BTCUSDT"))).upper().strip()
    signal_mode = str(payload.get("signal_mode", config.get("signal_mode", "balanced"))).strip()
    if symbol:
        config["symbol"] = symbol
        if symbol not in config.get("available_symbols", []):
            config.setdefault("available_symbols", []).append(symbol)
    if signal_mode in config.get("available_signal_modes", ["balanced", "high_precision", "lux_style"]):
        config["signal_mode"] = signal_mode
    config_store.save(config)
    return jsonify(config)


@app.get("/api/analyze")
def analyze():
    config = config_store.load()
    demo = request.args.get("demo", "").lower() in {"1", "true", "yes"}
    result = analyzer.analyze(config, use_demo_data=demo)
    result["risk_plan"] = analyzer.risk_plan(config, result["signal"])
    return jsonify(result)


@app.get("/api/backtest")
def backtest():
    config = config_store.load()
    demo = request.args.get("demo", "").lower() in {"1", "true", "yes"}
    symbols = request.args.get("symbols")
    modes = request.args.get("modes")
    result = analyzer.backtest(
        config,
        symbols=[item.strip().upper() for item in symbols.split(",") if item.strip()] if symbols else None,
        modes=[item.strip() for item in modes.split(",") if item.strip()] if modes else None,
        candles=int(request.args.get("candles", "360")),
        horizon=int(request.args.get("horizon", "12")),
        use_demo_data=demo,
    )
    run_id = store.save_run("backtest", result, label=",".join(result["settings"]["symbols"]))
    result["run_id"] = run_id
    return jsonify(result)


@app.get("/api/forecast")
def forecast():
    config = config_store.load()
    demo = request.args.get("demo", "").lower() in {"1", "true", "yes"}
    return jsonify(
        analyzer.forecast(
            config,
            candles=int(request.args.get("candles", "220")),
            horizon=int(request.args.get("horizon", "24")),
            use_demo_data=demo,
        )
    )


@app.get("/api/optimize")
def optimize():
    config = config_store.load()
    demo = request.args.get("demo", "").lower() in {"1", "true", "yes"}
    symbols = request.args.get("symbols")
    result = analyzer.optimize(
        config,
        symbols=[item.strip().upper() for item in symbols.split(",") if item.strip()] if symbols else None,
        candles=int(request.args.get("candles", "320")),
        horizon=int(request.args.get("horizon", "12")),
        use_demo_data=demo,
    )
    return jsonify(result)


@app.post("/api/optimize/start")
def optimize_start():
    config = config_store.load()
    payload = request.get_json(silent=True) or {}
    symbols = payload.get("symbols")
    demo = bool(payload.get("demo"))
    job_id = uuid.uuid4().hex
    with optimizer_lock:
        optimizer_jobs[job_id] = {
            "id": job_id,
            "status": "running",
            "done": 0,
            "total": 0,
            "best": None,
            "candidates": [],
            "best_history": [],
            "result": None,
            "error": None,
            "cancel": False,
            "started_at": int(time.time()),
        }

    def progress(update: dict) -> None:
        with optimizer_lock:
            job = optimizer_jobs.get(job_id)
            if not job:
                return
            job["done"] = update.get("done", job["done"])
            job["total"] = update.get("total", job["total"])
            job["best"] = update.get("best")
            if update.get("candidate"):
                job["candidates"].append(update["candidate"])
                job["candidates"] = sorted(job["candidates"], key=lambda item: item["score"], reverse=True)[:8]
            if update.get("convergence"):
                job["best_history"].append(update["convergence"])

    def should_cancel() -> bool:
        with optimizer_lock:
            return bool(optimizer_jobs.get(job_id, {}).get("cancel"))

    def run_job() -> None:
        try:
            result = analyzer.optimize(
                config,
                symbols=[item.strip().upper() for item in symbols.split(",") if item.strip()]
                if isinstance(symbols, str)
                else None,
                candles=int(payload.get("candles", 320)),
                horizon=int(payload.get("horizon", 12)),
                use_demo_data=demo,
                progress=progress,
                should_cancel=should_cancel,
            )
            with optimizer_lock:
                job = optimizer_jobs[job_id]
                run_id = store.save_run("optimizer", result, label=",".join(result["settings"]["symbols"]))
                result["run_id"] = run_id
                job["result"] = result
                job["best"] = result.get("best")
                job["status"] = "cancelled" if job.get("cancel") else "done"
                job["finished_at"] = int(time.time())
        except Exception as exc:
            with optimizer_lock:
                job = optimizer_jobs[job_id]
                job["status"] = "error"
                job["error"] = str(exc)
                job["finished_at"] = int(time.time())

    threading.Thread(target=run_job, daemon=True).start()
    return jsonify({"job_id": job_id})


@app.get("/api/optimize/status/<job_id>")
def optimize_status(job_id: str):
    with optimizer_lock:
        job = optimizer_jobs.get(job_id)
        if not job:
            return jsonify({"error": "job nicht gefunden"}), 404
        return jsonify({key: value for key, value in job.items() if key != "cancel"})


@app.post("/api/optimize/cancel/<job_id>")
def optimize_cancel(job_id: str):
    with optimizer_lock:
        job = optimizer_jobs.get(job_id)
        if not job:
            return jsonify({"error": "job nicht gefunden"}), 404
        job["cancel"] = True
        return jsonify({"status": "cancel_requested"})


@app.post("/api/optimize/apply")
def apply_optimization():
    config = config_store.load()
    payload = request.get_json(silent=True) or {}
    params = payload.get("signal_params")
    if not isinstance(params, dict):
        return jsonify({"error": "signal_params fehlt"}), 400
    config["signal_params"] = params
    symbol_params = payload.get("symbol_params")
    if isinstance(symbol_params, dict):
        config["symbol_params"] = symbol_params
    config["optimizer_suggestion"] = {
        "applied_at": int(__import__("time").time()),
        "params": params,
        "symbol_params": config.get("symbol_params", {}),
        "source": "api/optimize",
    }
    config_store.save(config)
    return jsonify(config)


@app.get("/api/history")
def history():
    kind = request.args.get("kind")
    limit = int(request.args.get("limit", "20"))
    return jsonify({"runs": store.recent_runs(kind=kind, limit=limit)})


@app.get("/api/health")
def health():
    config = config_store.load()
    return jsonify(
        {
            "status": "ok",
            "updated_at": int(time.time()),
            "database": str(store.path),
            "execution": config.get("execution", {}),
            "risk_management": config.get("risk_management", {}),
            "exchange_guard": exchange_guard.status(config),
            "optimizer_jobs": {
                "running": sum(1 for job in optimizer_jobs.values() if job.get("status") == "running"),
                "total": len(optimizer_jobs),
            },
        }
    )


@app.get("/api/paper/orders")
def paper_orders():
    return jsonify({"orders": store.recent_paper_orders(limit=int(request.args.get("limit", "25")))})


@app.post("/api/paper/order")
def paper_order():
    config = config_store.load()
    payload = request.get_json(silent=True) or {}
    signal = payload.get("signal")
    if not isinstance(signal, dict):
        analysis = analyzer.analyze(config, use_demo_data=bool(payload.get("demo")))
        signal = analysis["signal"]
    plan = analyzer.risk_plan(config, signal)
    if not plan["paper_allowed"]:
        return jsonify({"error": "paper order blockiert", "risk_plan": plan}), 400
    order = {
        "symbol": config.get("symbol", "BTCUSDT"),
        "side": plan.get("side", signal.get("side", "BUY")),
        "status": "paper_open",
        "quantity": plan["quantity"],
        "entry": plan["entry"],
        "stop": plan["stop"],
        "target": plan["target"],
        "risk_amount": plan["risk_amount"],
        "risk_plan": plan,
        "signal": signal,
    }
    order["id"] = store.save_paper_order(order)
    return jsonify(order)


@app.get("/api/exchange/status")
def exchange_status():
    return jsonify(exchange_guard.status(config_store.load()))


@app.post("/api/exchange/order")
def exchange_order():
    config = config_store.load()
    payload = request.get_json(silent=True) or {}
    return jsonify(exchange_guard.place_order(config, payload))


if __name__ == "__main__":
    host = os.environ.get("FLASK_HOST", "127.0.0.1")
    port = int(os.environ.get("PORT", "5050"))
    debug = os.environ.get("FLASK_DEBUG", "1") == "1"
    app.run(host=host, port=port, debug=debug)
