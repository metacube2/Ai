# Trading Cockpit Dokumentation

Stand: 2026-05-24

Diese Dokumentation beschreibt den aktuellen technischen Stand der Trading-Cockpit-Web-App, die aus dem alten `trade.txt`/Tkinter-Programm in eine Python/Flask-Webanwendung umgebaut wurde.

## Kurzstatus

Die App ist als Web-Cockpit lauffähig und auf pDocker deployed:

```text
http://192.168.178.113:5050
```

Aktuelle Kernfunktionen:

- Live-Analyse über Binance-Spot-Kerzen
- Demo-Fallback, wenn Datenquellen nicht erreichbar sind
- Long- und Short-Signale
- Entscheidungsampel mit Entry, Stop, Ziel, R/R, Score und Confidence
- technische Indikatoren
- Makro- und Marktstrukturfilter
- Forecast-Ansicht mit Lernkurve
- Backtesting mit Gebühren, Spread und Slippage
- Optimizer mit Fortschritt, Quality-Gate, Out-of-sample und Walk-forward
- Paper-Orders
- Exchange-Guard, der echte Orders standardmäßig blockiert
- Docker/pDocker Betrieb

## Start

Lokal:

```bash
cd /Users/metacube/Projects/Git/trade_web
python3 -m pip install -r requirements.txt
python3 app.py
```

Dann öffnen:

```text
http://127.0.0.1:5050
```

Docker lokal:

```bash
docker compose build
docker compose up -d
docker compose ps
docker compose logs -f trading-web
```

## Deployment

Zielsystem:

```text
pDocker: 192.168.178.113
App-Pfad: /data/compose/trade_web
URL: http://192.168.178.113:5050
Container: trading-web
```

Deployment-Script:

```bash
./deploy_pdocker.sh
```

Beim manuellen Deployment wird `config.json` nicht überschrieben, wenn nur Codeänderungen ausgeliefert werden sollen. Die laufende Server-Konfiguration bleibt dadurch erhalten.

## Git Ablage

Lokales Entwicklungsrepo:

```text
/Users/metacube/Projects/Git/trade_web
```

GitHub-Ziel:

```text
Repository: metacube2/Ai
Pfad: trade_web/
URL: https://github.com/metacube2/Ai/tree/main/trade_web
```

Hinweis:

- Das lokale `trade_web`-Repo bleibt als eigenständiges Arbeitsrepo bestehen.
- Auf GitHub wird der Projektstand im bestehenden Repository `metacube2/Ai` unter dem Unterordner `trade_web/` abgelegt.
- Das lokale `Ai`-Repo unter `/Users/metacube/Projects/Git/Ai` kann unabhängig davon eigene offene Änderungen enthalten und wird für `trade_web` nicht automatisch verändert.

## Architektur

Wichtige Dateien:

```text
app.py                    Flask API und Routen
trading_engine.py         Indikatoren, Signalmodell, Forecast, Backtest, Optimizer
exchange.py               Exchange Safety Guard
storage.py                SQLite Persistenz
templates/index.html      HTML Layout
static/app.js             Frontend-Logik und SVG Charts
static/styles.css         UI Design
config.json               Symbol, Signalmodus, Risiko, Execution Safety
test_trading_engine.py    Regressionstests fuer Kernlogik
docker-compose.yml        Containerbetrieb
Dockerfile                Image Build
```

Persistenz:

```text
/app/data/trade_web.sqlite3
```

Gespeichert werden:

- Backtest-Runs
- Optimizer-Runs
- Paper-Orders

## UI Workflow

Tabs:

- `Analyse`: Ampel, Signal, Timeframes, Makro, Korrelationen
- `Forecast`: Forecast-Pfad, Unsicherheitsband, Lernkurve
- `Optimieren`: Backtest, Optimizer, Konvergenz, Historie
- `Risiko`: Risk-Plan, Health, Paper-Orders
- `Methodik`: Signalformel und TradingView-Formelcheck

Die Hauptampel zeigt:

- Entscheidung: Kaufen, Short, Beobachten, Short beobachten oder Warten
- Entry
- Stop
- Ziel
- Risk/Reward
- Score
- Confidence
- Signalblocker

## Datenquellen

Technische Analyse:

- Binance Spot Klines
- Timeframes: `15m`, `30m`, `4h`, `1d`

Makro/Marktstruktur:

- DXY/UUP Proxy
- SPY/QQQ Risk-on/Risk-off
- TLT als Zinsdruck-/Realzins-Proxy
- BTC-Dominanz und Stablecoin-Liquiditaet ueber CoinGecko
- Funding Rate und Open Interest ueber Binance Futures
- News/Event-Risiko ueber RSS-Keyword-Scan

Wenn externe Daten nicht erreichbar sind, wird neutral oder mit Demo-Fallback weitergerechnet. Die App soll nicht blockieren, nur weil eine externe Quelle langsam ist.

## Indikatoren

Berechnet werden:

- RSI 14
- MACD 12/26/9
- EMA20
- EMA50
- SMA200
- Volumen gegen SMA20
- Support/Resistance aus den letzten 30 Kerzen
- Fibonacci-Level
- W-Pattern
- Trendstatus
- Korrelationen gegen Benchmark-Assets

TradingView/Pine-Abgleich:

- RSI entspricht `ta.rsi(close, 14)` mit Wilder-RMA
- MACD entspricht `ta.macd(close, 12, 26, 9)`
- EMA entspricht `ta.ema(close, length)`
- SMA entspricht `ta.sma(close, length)`

Ein echter 1:1-Vergleich mit TradingView ist nur moeglich, wenn Symbol, Boerse, Timeframe und Kerzenschlusszeit identisch sind.

## Signalmodell

Das Signalmodell ist bidirektional:

- bullish Setup -> Long/BUY-Kandidat
- bearish Setup -> Short/SELL-Kandidat

Moegliche Signaltypen:

```text
STRONG_BUY
BUY
WEAK_BUY
NONE
WEAK_SELL
SELL
STRONG_SELL
```

Long-Geometrie:

```text
Entry = aktueller Preis
Stop  = Support
Ziel  = Resistance
R/R   = (Ziel - Entry) / (Entry - Stop)
```

Short-Geometrie:

```text
Entry = aktueller Preis
Stop  = Resistance
Ziel  = Support
R/R   = (Entry - Ziel) / (Stop - Entry)
```

Ein Signal wird nur aktiv, wenn:

- Score die Schwelle erreicht
- Risk/Reward positiv und ausreichend ist
- Modusfilter erlaubt
- Makrofilter nicht blockiert
- Entry/Stop/Ziel geometrisch korrekt sind

## Signalmodi

`balanced`:

- weniger streng
- mehr Signale
- gut fuer Exploration und Backtestvergleich

`high_precision`:

- verlangt Multi-Timeframe-Bestaetigung
- verlangt hoeheres R/R
- blockiert ueberkaufte Longs bzw. ueberverkaufte Shorts

`lux_style`:

- trend-, momentum-, volumen- und breakout-orientiert
- long und short getrennt geprueft
- keine LuxAlgo-Kopie und keine proprietaere LuxAlgo-Logik

## Aktuelle Logikfixes

Gefundene und behobene Fehler:

- Das System konnte zuerst nur Long-Signale ausgeben. Bearishe Setups wurden dadurch faelschlich `NONE/Warten`.
- Short-Signale wurden ergaenzt: `WEAK_SELL`, `SELL`, `STRONG_SELL`.
- Short-R/R wird jetzt korrekt mit Stop oberhalb und Ziel unterhalb berechnet.
- Paper-Orders und Risk-Plan kennen jetzt `BUY` und `SELL`.
- RSI unter 50 wurde vorher teilweise long-gruen bewertet. Jetzt ist RSI < 50 bearish, RSI > 50 bullish.
- RSI bei konstantem Markt war falsch moeglich als 100. Jetzt ist flacher RSI korrekt 50.
- Backtest konnte 1d-Daten mit Zukunftsblick verwenden. Jetzt werden Tageskerzen nur bis zur aktuellen Backtest-Kerze verwendet.
- Lux/Precision Backtest wurde durch fehlende 15m/30m-Historie zu hart blockiert. Im historischen 4h-Backtest wird dafuer ein 4h-Proxy verwendet.

## Backtesting

API:

```text
/api/backtest
/api/backtest?symbols=BTCUSDT,ETHUSDT&candles=360&horizon=12
```

Der Backtest simuliert:

- Long- und Short-Trades
- Entry
- Stop
- Target
- Timeout-Exit
- Gebuehren
- Slippage
- Spread

Metriken:

- Trades
- Wins/Losses
- Winrate
- Total Return
- Average Trade
- Profit Factor
- Max Drawdown
- Equity Curve
- Drawdown Curve
- Sample Trades

Chart:

- Candlestick-Daten
- Entry-Linien
- Stop-Linien
- Target-Linien
- Trade-Marker

Wichtig: Historische Makrodaten sind nur als Proxy-Schicht vorhanden. Technischer Backtest ist nutzbar, Makro-Historie ist keine vollstaendige institutionelle Makro-Rekonstruktion.

## Optimizer

Der Optimizer testet Parameterkombinationen:

- Signal-Schwellen
- R/R-Schwellen
- RSI-Schwellen
- Indikatorgewichte
- Symbolparameter

Bewertet wird mit:

- Training Score
- Out-of-sample Score
- Walk-forward Score
- Quality-Gate
- Robustheitsreport
- Monte-Carlo-Unterseite
- Verlustserien
- Konvergenzverlauf

Ein Vorschlag wird nur uebernehmbar, wenn das Quality-Gate bestanden ist.

## Forecast

Forecast-Ansicht:

- historische 4h-Kerzen
- Forecast-Pfad
- Unsicherheitsband
- Lernkurve
- Forecast-Fehler je Lernfenster

Der Forecast ist statistisch:

- Drift aus den letzten Returns
- Volatilitaet aus den letzten Returns
- Unsicherheitsband ueber Volatilitaet und Horizont

Das ist keine Garantie und kein Deep-Learning-Modell. Die Lernkurve zeigt, ob das gewaehlte Fenster historisch weniger Fehler erzeugt.

## Risk-Plan

Berechnet wird:

- Entry
- Stop
- Target
- Richtung
- Risiko pro Trade
- Positionsgroesse
- Notional
- Blocker

Risk-Konfiguration in `config.json`:

```json
{
  "risk_per_trade_pct": 0.5,
  "max_position_pct": 25,
  "max_daily_loss_pct": 2,
  "max_drawdown_pct": 12,
  "max_open_positions": 3,
  "cooldown_after_losses": 3,
  "slippage_bps": 5,
  "spread_bps": 4,
  "taker_fee_bps": 10,
  "maker_fee_bps": 6
}
```

## Paper-Trading

Paper-Order Endpunkte:

```text
GET  /api/paper/orders
POST /api/paper/order
```

Paper-Orders speichern:

- Symbol
- Side: `BUY` oder `SELL`
- Quantity
- Entry
- Stop
- Target
- Risk Amount
- Signal
- Risk Plan

## Live-Exchange-Sicherheit

Echte Orders sind bewusst blockiert.

Default:

```json
{
  "mode": "paper",
  "kill_switch": true,
  "allow_live_orders": false,
  "require_manual_confirm": true,
  "api_key_env": "",
  "api_secret_env": ""
}
```

Exchange-Guard:

```text
GET  /api/exchange/status
POST /api/exchange/order
```

Vor echtem Live-Trading waeren noetig:

- Testnet zuerst
- API-Key ohne Withdrawal-Rechte
- Secret-Verwaltung ueber ENV/Secret Store
- Tick-Size/Step-Size/Min-Notional Regeln
- Order-Limits
- Positionslimits
- Reconnect- und Retry-Handling
- Rate-Limit-Handling
- Audit-Log
- manuelle Freigabe oder klarer Autotrade-Modus

Aktuell ist der Live-Connector vorbereitet, sendet aber keine echten Exchange-Orders.

## API Uebersicht

```text
GET  /
GET  /api/config
POST /api/config
GET  /api/analyze
GET  /api/backtest
GET  /api/forecast
GET  /api/optimize
POST /api/optimize/start
GET  /api/optimize/status/<job_id>
POST /api/optimize/cancel/<job_id>
POST /api/optimize/apply
GET  /api/history
GET  /api/health
GET  /api/paper/orders
POST /api/paper/order
GET  /api/exchange/status
POST /api/exchange/order
```

## Tests

Vorhandene Tests:

```text
test_trading_engine.py
```

Aktuell abgedeckt:

- SMA/EMA/RSI/MACD Basics
- RSI bei flachem Markt
- Returns, Performance, Korrelation
- Long/Short-R/R gegen Signaloutput
- bearish Setup wird `STRONG_SELL`

Ausfuehrung, wenn `pytest` installiert ist:

```bash
python3 -m pytest test_trading_engine.py
```

Fallback ohne pytest:

```bash
python3 - <<'PY'
import test_trading_engine as tests
for name in sorted(dir(tests)):
    if name.startswith("test_"):
        getattr(tests, name)()
        print(name, "ok")
PY
```

Syntaxchecks:

```bash
python3 -m py_compile app.py trading_engine.py storage.py exchange.py test_trading_engine.py
node --check static/app.js
```

## Bekannte Grenzen

Die App ist ein Analyse- und Paper-Trading-System, kein garantierter Profit-Bot.

Bekannte Grenzen:

- kein vollstaendiger historischer News-/Funding-/Dominanz-Datensatz im Backtest
- Forecast ist einfach-statistisch
- kein produktiver WSGI-Server, aktuell Flask/Werkzeug im Container
- keine echte Binance/Bitget-Orderausfuehrung aktiv
- keine persistente Candle-Cache-Schicht
- Optimizer kann bei vielen Symbolen langsam werden
- keine Purged/Embargoed Cross-Validation
- keine Liquidation-Heatmap
- kein professioneller Wirtschaftskalender

## Empfohlene naechste Schritte

Prioritaet hoch:

- Gunicorn/Waitress statt Flask Dev Server
- Candle-Cache in SQLite
- mehr Regressionstests fuer Long/Short/Backtest
- UI-Anzeige fuer Long- und Short-Kandidat nebeneinander

Prioritaet mittel:

- historische Funding/OI-Daten
- historische BTC-Dominanz und Stablecoin-Liquiditaet
- Export von Backtests als CSV/JSON
- gespeicherte Optimizer-Jobs mit Resume

Prioritaet niedrig:

- Bitget-Connector
- echte Exchange-Ausfuehrung mit Testnet
- fortgeschrittene Forecast-Modelle

## Wichtige Sicherheitsregel

Live-Trading darf erst aktiviert werden, wenn Testnet, Key-Scope, Order-Limits, Fehlerhandling und manuelle Freigabe sauber umgesetzt und getestet sind.

Bis dahin bleibt das System bewusst bei Analyse, Backtest, Optimierung und Paper-Trading.
