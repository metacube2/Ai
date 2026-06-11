import math

from trading_engine import TradingAnalyzer, correlation, ema, macd, performance, returns, rsi, sma


def test_indicator_basics():
    assert sma([1, 2, 3, 4, 5], 3) == 4
    assert ema([5] * 20, 10) == 5
    assert rsi(list(range(1, 40))) == 100
    assert rsi(list(range(40, 1, -1))) == 0
    assert rsi([100] * 40) == 50
    macd_data = macd(list(range(1, 80)))
    assert set(macd_data) == {"macd", "signal", "hist"}
    assert macd_data["macd"] is not None


def test_math_helpers():
    assert returns([100, 110, 99]) == [0.10000000000000009, -0.09999999999999998]
    assert correlation([1, 2, 3], [1, 2, 3]) == 1
    assert math.isclose(performance([100, 120]), 20)


def test_signal_risk_reward_matches_output():
    analyzer = TradingAnalyzer()
    config = {
        "symbol": "BTCUSDT",
        "timeframes": ["15m", "30m", "4h", "1d"],
        "benchmark_assets": ["ETHUSDT"],
        "signal_mode": "balanced",
    }
    result = analyzer.analyze(config, use_demo_data=True)
    signal = result["signal"]
    expected = 0
    if signal["side"] == "SELL" and signal["stop_loss"] > signal["entry_price"] > signal["target"]:
        expected = (signal["entry_price"] - signal["target"]) / (signal["stop_loss"] - signal["entry_price"])
    elif signal["entry_price"] > signal["stop_loss"]:
        expected = (signal["target"] - signal["entry_price"]) / (signal["entry_price"] - signal["stop_loss"])
    assert math.isclose(signal["risk_reward"], expected)


def test_bearish_setup_becomes_short_signal():
    analyzer = TradingAnalyzer()
    params = analyzer._signal_params()
    frames = {}
    for timeframe in ["15m", "30m", "4h", "1d"]:
        frames[timeframe] = {
            "price": 76676.41,
            "indicators": {
                "close": 76676.41,
                "support": 74289.6,
                "resistance": 78200,
                "rsi": 45,
                "macd": {"hist": -10},
            },
            "statuses": {
                "RSI": {"status": "red"},
                "MACD": {"status": "red"},
                "MA_Setup": {"status": "red"},
                "Volumen": {"status": "orange"},
                "Trend": {"status": "red"},
                "Support/Resist": {"status": "red"},
            },
        }
    signal = analyzer._signal(frames, {"pattern_detected": False}, {"score": 0, "label": "neutral"}, "lux_style", params)
    assert signal["signal_type"] == "STRONG_SELL"
    assert signal["side"] == "SELL"
    assert math.isclose(signal["risk_reward"], 1.5664, rel_tol=0.01)
