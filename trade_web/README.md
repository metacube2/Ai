# Trading Ampel Web

Web-Version des alten Tkinter-Trading-Tools.

## Vollstaendige Dokumentation

Die aktuelle Gesamtdokumentation liegt hier:

```text
docs/TRADING_COCKPIT_DOKUMENTATION.md
```

Sie beschreibt Architektur, Bedienung, Long-/Short-Signallogik, TradingView-Formelcheck, Backtesting, Optimizer, Forecast, Docker/pDocker, Paper-Trading, Safety-Guard, Tests und bekannte Grenzen.

## Start

```bash
cd /Users/metacube/Projects/Git/trade_web
python3 -m pip install -r requirements.txt
python3 app.py
```

Danach im Browser öffnen:

```text
http://127.0.0.1:5050
```

Die App nutzt Binance-Kerzendaten und berechnet RSI, MACD, Moving Averages, Volumen, Support/Resistance, Fibonacci, W-Muster, Entry-Signal und Korrelationen selbst. Wenn Binance nicht erreichbar ist, fällt sie auf Demo-Daten zurück und zeigt eine Warnung an.

## Docker

```bash
cd /Users/metacube/Projects/Git/trade_web
docker compose build
docker compose up -d
```

Status und Logs:

```bash
docker compose ps
docker compose logs -f trading-web
```

Stoppen:

```bash
docker compose down
```

## Deployment auf pDocker

Ziel laut Homelab-Doku:

```text
root@192.168.178.113
/data/compose/trade_web
http://192.168.178.113:5050
```

Deployment:

```bash
cd /Users/metacube/Projects/Git/trade_web
chmod +x deploy_pdocker.sh
./deploy_pdocker.sh
```

## Backtesting

Im Dashboard gibt es den Button `Backtest starten`.

Direkt per API:

```text
http://192.168.178.113:5050/api/backtest?symbols=BTCUSDT,ETHUSDT&candles=260&horizon=8
```

Der Backtest nutzt historische Binance-Spot-Kerzen, simuliert Entry/Stop/Target ohne echte Orders und berechnet:

- Trades
- Winrate
- Total Return
- Profit Factor
- Max Drawdown
- Equity-/Drawdown-Kurve
- Beispiel-Trades

Die Simulation berücksichtigt Taker-Gebühren, Slippage und Spread aus `risk_management`.
Hinweis: Der Backtest nutzt echte 4h/1d-Historie. Makrodaten werden im historischen Backtest noch nicht vollständig rückwirkend rekonstruiert.

Zusätzlich ist ein Makro- und Marktstrukturfilter eingebaut:

- US-Dollar/DXY-Proxy über UUP
- Aktienmarkt Risk-on/Risk-off über SPY/QQQ
- Zins-/Realzinsdruck als TLT-Proxy
- BTC-Dominanz und Stablecoin-Liquidität über CoinGecko
- Funding Rate und Open Interest über Binance Futures
- Nachrichten-/Event-Risiko über RSS-Keyword-Scan

## Optimizer, Historie und Paper-Trading

Die View `Backtest & Optimizer` startet einen Live-Optimizer mit Fortschritt, Ranking, Out-of-sample-Score, Walk-forward-Score und Quality-Gate gegen Overfitting. Vorschläge werden nur übernehmbar, wenn die Quality-Gate-Regeln bestanden sind.

Persistente Daten liegen in SQLite:

```text
/app/data/trade_web.sqlite3
```

Wichtige Endpunkte:

```text
/api/health
/api/history
/api/paper/orders
/api/paper/order
```

Paper-Trading ist vorbereitet, echte Orders sind absichtlich blockiert:

- `execution.mode = paper`
- `execution.kill_switch = true`
- `execution.allow_live_orders = false`
- `execution.require_manual_confirm = true`

Für echte Exchange-Ausführung muss zuerst Testnet, API-Key-Scope ohne Withdrawal, Kill-Switch, manuelle Bestätigung und Order-Limitierung bewusst konfiguriert werden.

## Signal-Modi

- `Ausgewogen`: mehr Signale, weniger streng.
- `Hohe Trefferwahrscheinlichkeit`: blockiert Signale ohne Multi-Timeframe- und Risk/Reward-Bestätigung.
- `Lux-inspiriert`: Trend-, Momentum-, Volumen- und Breakout-Bestätigung. Das ist keine LuxAlgo-Kopie und nutzt keine proprietäre LuxAlgo-Logik.

## Aktueller Stand

Fertig und lauffähig:

- Web-Cockpit mit Tabs `Analyse`, `Optimieren`, `Risiko`, `Methodik`
- Entscheidungsampel mit Entry, Stop, Ziel und Risk/Reward
- Technische Signale: RSI, MACD, Moving Averages, Volumen, Trend, Support/Resistance, Fibonacci, W-Muster
- Makro-/Marktstrukturfilter über verfügbare öffentliche Daten und Proxys
- Binance-Historien-Backtest für 4h/1d-Kerzen
- Backtest mit Gebühren, Slippage und Spread aus `risk_management`
- Live-Optimizer mit Fortschritt, Ranking, Out-of-sample-Score und Walk-forward-Score
- Quality-Gate gegen offensichtliches Overfitting
- Robustheitsreport mit Train/OOS/WF-Stabilität und Monte-Carlo-Auswertung
- Equity-/Drawdown-Kurve und Candlestick-Chart mit Entry/Stop/Target-Markierungen
- SQLite-Persistenz für Backtest-/Optimizer-Runs und Paper-Orders
- Risk-Plan mit Positionsgröße, Notional, Risiko pro Trade und Blockern
- Paper-Order-Vorbereitung ohne echte Exchange-Ausführung
- Exchange-Guard mit Status-/Order-Endpunkt, der Live-Orders standardmäßig blockiert
- Health-Endpunkt und einfache Systemanzeige
- Docker/pDocker Deployment unter `http://192.168.178.113:5050`

Bewusst blockiert:

- Echte Binance/Bitget-Orders sind deaktiviert.
- `execution.kill_switch = true`
- `execution.allow_live_orders = false`
- API-Keys werden nicht gespeichert.
- Withdrawals sind nicht vorgesehen.

Neu nach Umsetzung der Punkte 6, 2, 4 und 3:

- Punkt 6: Overfitting-Schutz erweitert um Monte-Carlo-Unterseite, Verlustserien und Stabilitätslücke zwischen Training, Out-of-sample und Walk-forward.
- Punkt 2: Historische Makro-Schicht nutzt UUP/SPY/QQQ/TLT-Proxys, wenn verfügbar. Wenn Quellen fehlen, wird neutral weitergetestet und eine Warnung ausgegeben.
- Punkt 4: Backtest-Ergebnisse enthalten einen Candlestick-Chart-Datensatz und die UI zeichnet Entry, Stop und Target.
- Punkt 3: Exchange-Connector ist als Safety-Guard vorhanden. Er sendet keine echten Orders, solange Live-Modus, Kill-Switch, manuelle Freigabe und API-Key-ENV nicht bewusst konfiguriert sind.

## Offene Punkte

### 1. Produktionsserver

Aktuell läuft die App im Container über den Flask/Werkzeug-Server. Für dauerhaften Betrieb sollte auf `gunicorn` oder `waitress` umgestellt werden.

Ziel:

- stabilerer Prozessbetrieb
- bessere Timeouts
- mehrere Worker
- saubere Healthchecks

### 2. Historische Makrodaten im Backtest

Historische Makrodaten sind als Proxy-Schicht eingebaut. Sie nutzt, wenn verfügbar, UUP, SPY, QQQ und TLT und synchronisiert daraus einen historischen Makro-Score.

Noch nicht vollständig historisch rekonstruiert:

- BTC-Dominanz
- Stablecoin-Liquidität
- Funding Rates
- Open Interest
- News/Event-Risiko

Der technische Backtest ist nutzbar; der Makro-Anteil ist weiterhin eine Proxy-Näherung und kein vollständiger institutioneller Makro-Datensatz.

### 3. Live-Exchange-Trading

Der Exchange-Guard ist eingebaut:

```text
/api/exchange/status
/api/exchange/order
```

Echte Orders werden weiterhin blockiert, solange Safety-Bedingungen nicht erfüllt sind.

Vor Live-Trading nötig:

- Testnet-Modus zuerst
- API-Key-Scope ohne Withdrawal
- verschlüsselte Secret-Verwaltung
- manuelle Bestätigung
- Kill-Switch bewusst deaktivieren
- `execution.mode = live`
- `execution.allow_live_orders = true`
- `execution.require_manual_confirm = false` oder ein separater manueller Bestätigungsflow
- `execution.api_key_env` und `execution.api_secret_env` setzen
- Order-Limits
- Positionslimits
- Fehler- und Reconnect-Handling
- Exchange-Regeln wie Tick-Size, Step-Size, Min-Notional

### 4. Kerzenchart mit Trades

Der Preis-Chart ist eingebaut:

- Candlestick-Chart
- Entry-Linie
- Stop-Linie
- Target-Linie
- Trade-Marker

Noch offen:

- Detailansicht pro Trade
- Export als CSV/JSON

### 5. Optimizer-Performance

Der Optimizer funktioniert, kann aber bei vielen Symbolen langsam werden.

Verbesserungen:

- Candle-Cache in SQLite
- parallele Kandidaten
- Wiederverwendung berechneter Indikatoren
- gespeicherte Optimizer-Jobs
- Abbruch und Resume über Neustarts hinweg

### 6. Overfitting-Schutz

Das Quality-Gate ist erweitert:

- Mindestanzahl Trades
- Profit-Factor
- Winrate
- Drawdown
- Out-of-sample
- Walk-forward
- Train/OOS/WF-Stabilitätslücke
- Monte-Carlo 5%-Unterseite
- Monte-Carlo Verlustserie

Noch offen:

- Parameter-Sensitivität
- Purged/embargoed Cross-Validation
- getrennte Train/Validation/Test-Zeiträume
- längere Out-of-sample-Zeiträume

### 7. Alerting und Monitoring

Noch nicht eingebaut:

- Telegram/Email/Webhook Alerts
- Signal-Benachrichtigung
- API-Ausfall-Benachrichtigung
- Drawdown-Warnung
- Datenlatenz-Anzeige
- Fehlerhistorie

### 8. User- und Key-Management

Noch nicht eingebaut:

- Login
- Rollen
- verschlüsselte API-Key-Verwaltung
- Audit-Log für Order-Aktionen
- getrennte Paper/Testnet/Live-Konfigurationen

### 9. Browser-/Viewport-Test

Syntax und API sind geprüft. Ein echter Browser-Screenshot-Test über Desktop und Mobile steht noch aus.

Zu prüfen:

- Desktop Layout
- Mobile Layout
- lange Texte in Buttons/Karten
- Chart-Skalierung
- Tab-Wechsel
- Optimizer-Fortschritt im Browser
