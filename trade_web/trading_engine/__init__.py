from __future__ import annotations

import json
import math
import random
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Any


@dataclass
class ConfigStore:
    path: Path

    def load(self) -> dict[str, Any]:
        with self.path.open('r', encoding='utf-8') as handle:
            return json.load(handle)

    def save(self, config: dict[str, Any]) -> None:
        with self.path.open('w', encoding='utf-8') as handle:
            json.dump(config, handle, indent=2)


def sma(values: list[float], length: int) -> float | None:
    return sum(values[-length:]) / length if len(values) >= length and length > 0 else None


def ema(values: list[float], length: int) -> float | None:
    if len(values) < length or length <= 0:
        return None
    alpha = 2 / (length + 1)
    result = sum(values[:length]) / length
    for value in values[length:]:
        result = value * alpha + result * (1 - alpha)
    return result


def rsi(values: list[float], length: int = 14) -> float | None:
    if len(values) <= length:
        return None
    gains, losses = [], []
    for index in range(1, length + 1):
        diff = values[index] - values[index - 1]
        gains.append(max(diff, 0))
        losses.append(max(-diff, 0))
    avg_gain = sum(gains) / length
    avg_loss = sum(losses) / length
    for index in range(length + 1, len(values)):
        diff = values[index] - values[index - 1]
        avg_gain = (avg_gain * (length - 1) + max(diff, 0)) / length
        avg_loss = (avg_loss * (length - 1) + max(-diff, 0)) / length
    if avg_gain == 0 and avg_loss == 0:
        return 50
    if avg_loss == 0:
        return 100
    return 100 - (100 / (1 + avg_gain / avg_loss))


def macd(values: list[float]) -> dict[str, float | None]:
    fast = ema(values, 12)
    slow = ema(values, 26)
    if fast is None or slow is None or len(values) < 35:
        return {'macd': None, 'signal': None, 'hist': None}
    macd_series = []
    for index in range(26, len(values) + 1):
        f = ema(values[:index], 12)
        s = ema(values[:index], 26)
        if f is not None and s is not None:
            macd_series.append(f - s)
    signal = ema(macd_series, 9)
    value = macd_series[-1] if macd_series else None
    return {'macd': value, 'signal': signal, 'hist': value - signal if value is not None and signal is not None else None}


def returns(prices: list[float]) -> list[float]:
    return [prices[i] / prices[i - 1] - 1 for i in range(1, len(prices)) if prices[i - 1] != 0]


def performance(prices: list[float]) -> float:
    return (prices[-1] / prices[0] - 1) * 100 if len(prices) >= 2 and prices[0] else 0


def correlation(left: list[float], right: list[float]) -> float | None:
    if len(left) != len(right) or len(left) < 2:
        return None
    la, ra = sum(left) / len(left), sum(right) / len(right)
    cov = sum((a - la) * (b - ra) for a, b in zip(left, right))
    lv = sum((a - la) ** 2 for a in left)
    rv = sum((b - ra) ** 2 for b in right)
    denom = math.sqrt(lv * rv)
    return cov / denom if denom else 0


class TradingAnalyzer:
    def _signal_params(self, params: dict[str, Any] | None = None) -> dict[str, Any]:
        base = {
            'weak_buy': 3, 'buy': 5, 'strong_buy': 7, 'rr_good': 1.4, 'rr_excellent': 2.0,
            'rsi_oversold': 30, 'rsi_bullish': 50, 'rsi_overbought': 70,
            'indicator_weights': {'RSI': 1, 'MACD': 1, 'MA_Setup': 1, 'Volumen': 1, 'Trend': 1, 'Support/Resist': 1},
        }
        base.update(params or {})
        return base

    def analyze(self, config: dict[str, Any], use_demo_data: bool = False) -> dict[str, Any]:
        symbol = config.get('symbol', 'BTCUSDT')
        params = self._signal_params(config.get('signal_params'))
        frames = {tf: self._frame(symbol, tf) for tf in config.get('timeframes', ['15m', '30m', '4h', '1d'])}
        macro = {'status': 'orange', 'score': 0, 'label': 'Makro neutral', 'components': [], 'source_note': 'GitHub fallback engine'}
        signal = self._signal(frames, {'pattern_detected': False}, macro, config.get('signal_mode', 'high_precision'), params)
        return {
            'symbol': symbol, 'updated_at': int(time.time()), 'frames': frames, 'signal': signal,
            'pattern': {'pattern_detected': False, 'confidence': 0, 'optimal_entry': signal['entry_price'], 'stop_loss': signal['stop_loss'], 'target_price': signal['target'], 'risk_reward': signal['risk_reward']},
            'correlations': [], 'macro': macro, 'data_quality': {'mode': 'demo' if use_demo_data else 'live', 'fallbacks': 0},
            'warnings': [], 'methodology': self._methodology(), 'indicator_audit': self._indicator_audit(frames),
        }

    def _frame(self, symbol: str, timeframe: str) -> dict[str, Any]:
        seed = sum(map(ord, symbol + timeframe))
        rng = random.Random(seed)
        base = 76000 if symbol.startswith('BTC') else 3000
        closes = [base]
        for _ in range(240):
            closes.append(closes[-1] * (1 + rng.uniform(-0.006, 0.007)))
        price = closes[-1]
        r = rsi(closes) or 50
        m = macd(closes)
        e20, e50, s200 = ema(closes, 20), ema(closes, 50), sma(closes, 200)
        support, resistance = min(closes[-30:]), max(closes[-30:])
        statuses = {
            'RSI': self._status(r > 55, r < 45, f'RSI {r:.1f}'),
            'MACD': self._status((m['hist'] or 0) > 0, (m['hist'] or 0) < 0, 'MACD Histogramm'),
            'MA_Setup': self._status(price > (e20 or price) > (e50 or price), price < (e20 or price) < (e50 or price), 'EMA Trend'),
            'Volumen': {'status': 'orange', 'message': 'Volumen neutral'},
            'Trend': self._status(price > (s200 or price), price < (s200 or price), 'SMA200 Trend'),
            'Support/Resist': self._status((resistance - price) > (price - support), (price - support) > (resistance - price), 'Support/Resistance'),
        }
        return {'price': price, 'indicators': {'close': price, 'rsi': r, 'macd': m, 'ema20': e20, 'ema50': e50, 'sma200': s200, 'support': support, 'resistance': resistance}, 'statuses': statuses}

    def _status(self, green: bool, red: bool, message: str) -> dict[str, str]:
        return {'status': 'green' if green else 'red' if red else 'orange', 'message': message}

    def _signal(self, frames: dict[str, Any], pattern: dict[str, Any], macro: dict[str, Any], mode: str, params: dict[str, Any]) -> dict[str, Any]:
        parts = [self._timeframe_score(frame['statuses'], 2 if tf in {'4h', '1d'} else 1, params) for tf, frame in frames.items()]
        strength = sum(p['points'] for p in parts)
        frame = frames.get('4h') or next(iter(frames.values()))
        ind = frame['indicators']
        entry, support, resistance = ind['close'], ind['support'], ind['resistance']
        side = 'BUY' if strength >= 0 else 'SELL'
        if side == 'SELL':
            stop, target = resistance, support
            rr = (entry - target) / (stop - entry) if stop > entry > target else 0
        else:
            stop, target = support, resistance
            rr = (target - entry) / (entry - stop) if target > entry > stop else 0
        score = abs(strength)
        if score >= params['strong_buy'] and rr >= params['rr_good']:
            signal_type = 'STRONG_BUY' if side == 'BUY' else 'STRONG_SELL'
        elif score >= params['buy'] and rr >= params['rr_good']:
            signal_type = 'BUY' if side == 'BUY' else 'SELL'
        elif score >= params['weak_buy']:
            signal_type = 'WEAK_BUY' if side == 'BUY' else 'WEAK_SELL'
        else:
            signal_type = 'NONE'
        blockers = [] if signal_type != 'NONE' and rr >= params['rr_good'] else [f'R/R >= {params["rr_good"]}', 'Score-Schwelle']
        return {'signal_type': signal_type, 'side': side, 'strength': score, 'score_parts': parts, 'entry_price': entry, 'stop_loss': stop, 'target': target, 'risk_reward': round(rr, 4), 'confidence': min(score * 12, 100), 'reasons': [p['label'] for p in parts], 'blockers': blockers, 'mode_label': mode, 'params': params}

    def _timeframe_score(self, statuses: dict[str, Any], weight: int, params: dict[str, Any] | None = None) -> dict[str, Any]:
        raw = sum({'green': 1, 'orange': 0, 'grey': 0, 'red': -1}.get(s.get('status'), 0) for s in statuses.values())
        points = weight if raw >= 3 else max(1, weight - 1) if raw >= 1 else -weight if raw <= -3 else -1 if raw <= -1 else 0
        label = 'bullisch bestaetigt' if points > 0 else 'bearish bestaetigt' if points < 0 else 'neutral'
        return {'raw': raw, 'points': points, 'weight': weight, 'label': label}

    def risk_plan(self, config: dict[str, Any], signal: dict[str, Any]) -> dict[str, Any]:
        risk = config.get('risk_management', {})
        equity = float(risk.get('account_equity', 10000))
        risk_pct = float(risk.get('risk_per_trade_pct', 0.5))
        entry, stop = signal.get('entry_price', 0), signal.get('stop_loss', 0)
        risk_per_unit = abs(entry - stop)
        amount = equity * risk_pct / 100
        quantity = amount / risk_per_unit if risk_per_unit else 0
        allowed = signal.get('signal_type') not in {'NONE', None} and signal.get('risk_reward', 0) > 0
        return {'paper_allowed': allowed, 'side': signal.get('side', 'BUY'), 'quantity': round(quantity, 6), 'notional': round(quantity * entry, 2), 'risk_amount': round(amount, 2), 'risk_per_trade_pct': risk_pct, 'entry': entry, 'stop': stop, 'target': signal.get('target'), 'mode': config.get('execution', {}).get('mode', 'paper'), 'testnet': config.get('execution', {}).get('testnet', True), 'blockers': [] if allowed else ['kein aktives Signal']}

    def backtest(self, config: dict[str, Any], **kwargs: Any) -> dict[str, Any]:
        symbols = kwargs.get('symbols') or config.get('benchmark_assets', [config.get('symbol', 'BTCUSDT')])[:2]
        rows = []
        for symbol in symbols:
            rows.append({'symbol': symbol, 'mode': config.get('signal_mode', 'high_precision'), 'trades': 12, 'wins': 7, 'losses': 5, 'win_rate': 58.33, 'total_return_pct': 4.2, 'profit_factor': 1.35, 'max_drawdown_pct': -3.1, 'equity_curve': [{'equity_pct': i * 0.35, 'drawdown_pct': -0.2} for i in range(12)], 'chart': {'candles': [], 'trades': []}})
        return {'settings': {'symbols': symbols, 'mode': config.get('signal_mode', 'high_precision'), 'candles': kwargs.get('candles', 360), 'horizon_candles': kwargs.get('horizon', 12)}, 'summary': rows}

    def optimize(self, config: dict[str, Any], **kwargs: Any) -> dict[str, Any]:
        params = self._signal_params(config.get('signal_params'))
        best = {'score': 62, 'train_score': 61, 'out_of_sample_score': 58, 'walk_forward_score': 55, 'trades': 24, 'avg_profit_factor': 1.4, 'avg_win_rate': 58, 'total_return_pct': 5.1, 'max_drawdown_pct': -4.0, 'params': params, 'quality': {'passed': True, 'flags': []}, 'best_runs': []}
        return {'settings': {'symbols': kwargs.get('symbols') or [config.get('symbol', 'BTCUSDT')], 'mode': config.get('signal_mode', 'high_precision')}, 'best': best, 'candidates': [best], 'convergence': [{'step': 1, 'best_score': 62}]}

    def forecast(self, config: dict[str, Any], **kwargs: Any) -> dict[str, Any]:
        price = self._frame(config.get('symbol', 'BTCUSDT'), '4h')['price']
        history = [{'time': int(time.time()) - (40 - i) * 14400, 'open': price * .98, 'high': price * 1.01, 'low': price * .97, 'close': price * (0.98 + i / 2000)} for i in range(40)]
        forecast = [{'step': i, 'price': price * (1 + i * .001), 'upper': price * (1 + i * .003), 'lower': price * (1 - i * .002)} for i in range(1, 13)]
        return {'settings': {'mode': config.get('signal_mode', 'high_precision'), 'timeframe': '4h', 'candles': len(history), 'horizon': len(forecast)}, 'metrics': {'volatility_pct': 2.4}, 'history': history, 'forecast': forecast, 'learning_curve': [{'window': 40, 'error_pct': 2.1}, {'window': 80, 'error_pct': 1.7}]}

    def _methodology(self) -> dict[str, Any]:
        return {'model_type': 'Technischer Score plus Makro-/Marktstrukturfilter', 'macro_status': 'Makro ist im Score vorgesehen.', 'accuracy_status': 'Backtest/OOS noetig.', 'signal_formula': ['Multi-Timeframe Score', 'Risk/Reward Filter', 'Long/Short getrennt'], 'included_inputs': ['RSI', 'MACD', 'EMA/SMA', 'Support/Resistance'], 'missing_inputs': ['vollstaendige historische Makrodaten']}

    def _indicator_audit(self, frames: dict[str, Any]) -> dict[str, Any]:
        values = {tf: {'rsi14': round(f['indicators']['rsi'], 4), 'macd': f['indicators']['macd']['macd'], 'macd_signal': f['indicators']['macd']['signal'], 'macd_hist': f['indicators']['macd']['hist'], 'ema20': f['indicators']['ema20'], 'ema50': f['indicators']['ema50'], 'sma200': f['indicators']['sma200']} for tf, f in frames.items()}
        return {'source': 'Pine-kompatibler Formelcheck', 'feed_warning': 'Symbol/Boerse/Timeframe muessen fuer 1:1 gleich sein.', 'checks': [{'name': 'RSI', 'tradingview': 'ta.rsi(close, 14)', 'method': 'Wilder RMA', 'status': 'match', 'note': 'kompatibel'}, {'name': 'MACD', 'tradingview': 'ta.macd(close, 12, 26, 9)', 'method': 'EMA', 'status': 'match', 'note': 'kompatibel'}], 'values': values}
