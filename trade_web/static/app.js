const frameContainer = document.querySelector("#frames");
const symbolSelect = document.querySelector("#symbol");
const signalModeSelect = document.querySelector("#signal-mode");
const customSymbol = document.querySelector("#custom-symbol");
const form = document.querySelector("#symbol-form");
const refreshButton = document.querySelector("#refresh");
const demoToggle = document.querySelector("#demo");
const runBacktestButton = document.querySelector("#run-backtest");
const runOptimizerButton = document.querySelector("#run-optimizer");
const runForecastButton = document.querySelector("#run-forecast");
const cancelOptimizerButton = document.querySelector("#cancel-optimizer");
const refreshHistoryButton = document.querySelector("#refresh-history");
const refreshOrdersButton = document.querySelector("#refresh-orders");
const paperOrderButton = document.querySelector("#paper-order");
const primaryPaperOrderButton = document.querySelector("#primary-paper-order");
let optimizerJobId = null;
let optimizerPollTimer = null;
let latestAnalysis = null;

const statusColor = {
  STRONG_BUY: "green",
  BUY: "green",
  WEAK_BUY: "orange",
  STRONG_SELL: "red",
  SELL: "red",
  WEAK_SELL: "orange",
  NONE: "grey",
};

const signalModeLabels = {
  balanced: "Ausgewogen",
  high_precision: "Hohe Trefferwahrscheinlichkeit",
  lux_style: "Lux-inspiriert",
};

function money(value) {
  if (!value) return "--";
  return Number(value).toLocaleString("de-CH", { maximumFractionDigits: value > 100 ? 2 : 5 });
}

function compactNumber(value, suffix = "") {
  if (!Number.isFinite(value)) return "--";
  const abs = Math.abs(value);
  const digits = abs >= 1000 ? 0 : abs >= 100 ? 1 : abs >= 10 ? 2 : 3;
  return `${Number(value).toLocaleString("de-CH", { maximumFractionDigits: digits })}${suffix}`;
}

function formatChartTime(timestamp) {
  if (!timestamp) return "--";
  const millis = timestamp > 100000000000 ? timestamp : timestamp * 1000;
  return new Date(millis).toLocaleDateString("de-CH", { day: "2-digit", month: "2-digit" });
}

function chartTicks(min, max, count = 5) {
  if (!Number.isFinite(min) || !Number.isFinite(max)) return [];
  if (min === max) return [min];
  return Array.from({ length: count }, (_, index) => min + (index / (count - 1)) * (max - min));
}

function renderChartFrame({ width, height, padLeft, padRight, padTop, padBottom, minY, maxY, y, xTicks = [], ySuffix = "", zeroY = null, splitX = null }) {
  const plotLeft = padLeft;
  const plotRight = width - padRight;
  const plotTop = padTop;
  const plotBottom = height - padBottom;
  const yTicks = chartTicks(minY, maxY, 5);
  const horizontal = yTicks.map(value => {
    const yy = y(value);
    return `<g class="chart-grid">
      <line x1="${plotLeft}" y1="${yy.toFixed(1)}" x2="${plotRight}" y2="${yy.toFixed(1)}"></line>
      <text x="${(plotLeft - 10).toFixed(1)}" y="${(yy + 4).toFixed(1)}" text-anchor="end">${compactNumber(value, ySuffix)}</text>
    </g>`;
  }).join("");
  const vertical = xTicks.map(tick => `<g class="chart-grid">
    <line x1="${tick.x.toFixed(1)}" y1="${plotTop}" x2="${tick.x.toFixed(1)}" y2="${plotBottom}"></line>
    <text x="${tick.x.toFixed(1)}" y="${(height - 8).toFixed(1)}" text-anchor="${tick.anchor || "middle"}">${tick.label}</text>
  </g>`).join("");
  const zero = Number.isFinite(zeroY) && zeroY >= plotTop && zeroY <= plotBottom
    ? `<line class="chart-zero" x1="${plotLeft}" y1="${zeroY.toFixed(1)}" x2="${plotRight}" y2="${zeroY.toFixed(1)}"></line>`
    : "";
  const split = Number.isFinite(splitX)
    ? `<line class="chart-split" x1="${splitX.toFixed(1)}" y1="${plotTop}" x2="${splitX.toFixed(1)}" y2="${plotBottom}"></line>`
    : "";
  return `
    ${horizontal}
    ${vertical}
    ${zero}
    ${split}
    <line class="chart-axis" x1="${plotLeft}" y1="${plotBottom}" x2="${plotRight}" y2="${plotBottom}"></line>
    <line class="chart-axis" x1="${plotLeft}" y1="${plotTop}" x2="${plotLeft}" y2="${plotBottom}"></line>
  `;
}

async function loadConfig() {
  const response = await fetch("/api/config");
  const config = await response.json();
  symbolSelect.innerHTML = "";
  for (const symbol of config.available_symbols || []) {
    const option = document.createElement("option");
    option.value = symbol;
    option.textContent = symbol;
    option.selected = symbol === config.symbol;
    symbolSelect.append(option);
  }
  signalModeSelect.innerHTML = "";
  for (const mode of config.available_signal_modes || ["balanced", "high_precision", "lux_style"]) {
    const option = document.createElement("option");
    option.value = mode;
    option.textContent = signalModeLabels[mode] || mode;
    option.selected = mode === (config.signal_mode || "balanced");
    signalModeSelect.append(option);
  }
}

async function saveSettings(symbol, signalMode) {
  await fetch("/api/config", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ symbol, signal_mode: signalMode }),
  });
  await loadConfig();
}

async function refresh() {
  refreshButton.disabled = true;
  refreshButton.textContent = "Lädt...";
  try {
    const demo = demoToggle.checked ? "?demo=1" : "";
    const response = await fetch(`/api/analyze${demo}`);
    const data = await response.json();
    latestAnalysis = data;
    render(data);
    loadHealth();
  } finally {
    refreshButton.disabled = false;
    refreshButton.textContent = "Aktualisieren";
  }
}

function render(data) {
  document.querySelector("#updated").textContent = `${data.symbol} · ${new Date(data.updated_at * 1000).toLocaleString("de-CH")}`;
  renderSignal(data.signal);
  renderFrames(data.frames, data.signal);
  renderPattern(data.pattern);
  renderCorrelations(data.correlations);
  renderMacro(data.macro);
  renderRiskPlan(data.risk_plan);
  renderSummaryChips(data);
  renderMethodology(data.methodology);
  renderIndicatorAudit(data.indicator_audit);
  renderDataQuality(data.data_quality);
  renderWarnings(data.warnings || []);
}

function signalLabel(type) {
  if (type === "STRONG_BUY") return "Kaufen";
  if (type === "BUY") return "Kaufen";
  if (type === "WEAK_BUY") return "Beobachten";
  if (type === "STRONG_SELL") return "Short";
  if (type === "SELL") return "Short";
  if (type === "WEAK_SELL") return "Short beobachten";
  return "Warten";
}

function decisionCopy(signal, riskPlan) {
  if (!signal) return "Noch keine Daten geladen.";
  if (riskPlan?.paper_allowed) return "Signal ist stark genug fuer Paper-Trading. Risiko und Stop sind berechnet.";
  if (signal.signal_type === "WEAK_BUY") return "Setup entsteht, aber die Bestaetigung reicht noch nicht fuer eine Order.";
  if (signal.signal_type === "WEAK_SELL") return "Bearishes Setup entsteht, aber die Bestaetigung reicht noch nicht fuer eine Short-Order.";
  if (signal.signal_type === "NONE") return "Kein sauberer Einstieg. Das System wartet auf bessere Bestaetigung.";
  return "Signal vorhanden, aber der Risk-Plan blockiert die Ausfuehrung.";
}

function renderSummaryChips(data) {
  document.querySelector("#macro-chip").textContent = data.macro?.label || "--";
  document.querySelector("#risk-chip").textContent = data.risk_plan?.paper_allowed ? "handelbar" : "blockiert";
  document.querySelector("#data-chip").textContent = data.data_quality
    ? `${data.data_quality.mode}${data.data_quality.fallbacks ? `/${data.data_quality.fallbacks}` : ""}`
    : "--";
  document.querySelector("#mode-chip").textContent = data.signal?.mode_label || "--";
}

function renderRiskPlan(plan) {
  const target = document.querySelector("#risk-plan");
  if (!target || !plan) return;
  const blockers = plan.blockers && plan.blockers.length ? plan.blockers.join(" · ") : "keine";
  target.innerHTML = `
    <div class="pattern-row"><strong>Richtung</strong><span>${plan.side || "--"}</span></div>
    <div class="pattern-row"><strong>Paper erlaubt</strong><span class="${plan.paper_allowed ? "text-green" : "text-red"}">${plan.paper_allowed ? "Ja" : "Nein"}</span></div>
    <div class="pattern-row"><strong>Menge</strong><span>${plan.quantity}</span></div>
    <div class="pattern-row"><strong>Notional</strong><span>${plan.notional}</span></div>
    <div class="pattern-row"><strong>Risiko</strong><span>${plan.risk_amount} (${plan.risk_per_trade_pct}%)</span></div>
    <div class="pattern-row"><strong>Modus</strong><span>${plan.mode}${plan.testnet ? " · Testnet" : ""}</span></div>
    <div class="pattern-row"><strong>Blocker</strong><span>${blockers}</span></div>
  `;
  paperOrderButton.disabled = !plan.paper_allowed;
  primaryPaperOrderButton.disabled = !plan.paper_allowed;
}

function renderSignal(signal) {
  const light = document.querySelector("#signal-light");
  light.className = `signal-light ${statusColor[signal.signal_type] || "grey"}`;
  document.querySelector("#signal-type").textContent = signalLabel(signal.signal_type);
  document.querySelector("#decision-copy").textContent = decisionCopy(signal, latestAnalysis?.risk_plan);
  document.querySelector("#signal-meta").textContent = `${signal.mode_label || "Ausgewogen"} · ${signal.reasons.join(" · ")}`;
  document.querySelector("#entry-price").textContent = money(signal.entry_price);
  document.querySelector("#stop-loss").textContent = money(signal.stop_loss);
  document.querySelector("#target").textContent = money(signal.target);
  document.querySelector("#risk-reward").textContent = signal.risk_reward ? signal.risk_reward.toFixed(2) : "--";
  const blockers = signal.blockers || [];
  document.querySelector("#signal-blockers").innerHTML = blockers.length
    ? blockers.map(blocker => `<span>${blocker}</span>`).join("")
    : `<span class="ok">Keine Signalblocker</span>`;
  renderDecisionGraphic(signal);
}

function renderDecisionGraphic(signal) {
  const target = document.querySelector("#decision-graphic");
  if (!target) return;
  const params = signal.params || {};
  const strong = Number(params.strong_buy || 7);
  const buy = Number(params.buy || 5);
  const weak = Number(params.weak_buy || 3);
  const maxScore = Math.max(strong + 1, signal.strength || 0, 1);
  const scorePct = Math.max(0, Math.min(100, ((signal.strength || 0) / maxScore) * 100));
  const rr = Number(signal.risk_reward || 0);
  const rrTarget = Number(params.rr_good || 1.4);
  const rrPct = Math.max(0, Math.min(100, (rr / Math.max(rrTarget * 1.8, 1)) * 100));
  const confidencePct = Math.max(0, Math.min(100, Number(signal.confidence || 0)));
  const thresholds = [
    { label: "W", title: "Weak", value: weak },
    { label: "B", title: "Buy/Sell", value: buy },
    { label: "S", title: "Strong", value: strong },
  ].map(item => `<span title="${item.title}" style="left:${Math.min(100, (item.value / maxScore) * 100).toFixed(1)}%">${item.label}</span>`).join("");
  target.innerHTML = `
    <div class="decision-graphic-head">
      <strong>Signalgrafik</strong>
      <span>${signal.side || "--"} · ${signal.signal_type}</span>
    </div>
    <div class="gauge-row">
      <label>Score</label>
      <div class="gauge-track score"><span style="width:${scorePct.toFixed(1)}%"></span><div class="gauge-thresholds">${thresholds}</div></div>
      <strong>${signal.strength ?? 0}</strong>
    </div>
    <div class="gauge-row">
      <label>R/R</label>
      <div class="gauge-track rr"><span style="width:${rrPct.toFixed(1)}%"></span><i style="left:${Math.min(100, (rrTarget / Math.max(rrTarget * 1.8, 1)) * 100).toFixed(1)}%"></i></div>
      <strong>${rr ? rr.toFixed(2) : "--"}</strong>
    </div>
    <div class="gauge-row">
      <label>Conf.</label>
      <div class="gauge-track confidence"><span style="width:${confidencePct.toFixed(1)}%"></span></div>
      <strong>${signal.confidence ?? 0}%</strong>
    </div>
  `;
}

function renderFrames(frames, signal) {
  frameContainer.innerHTML = "";
  const timeframeScores = {};
  const weightedFrames = [["15m", 1], ["30m", 1], ["4h", 2], ["1d", 2]];
  const scoredFrames = weightedFrames.filter(([name]) => frames[name]).map(([name]) => name);
  (signal?.score_parts || []).forEach((part, index) => {
    timeframeScores[scoredFrames[index]] = part;
  });
  for (const [timeframe, frame] of Object.entries(frames)) {
    const article = document.createElement("article");
    article.className = "timeframe";
    const statuses = Object.values(frame.statuses || {});
    const green = statuses.filter(status => status.status === "green").length;
    const orange = statuses.filter(status => status.status === "orange").length;
    const red = statuses.filter(status => status.status === "red").length;
    const total = Math.max(statuses.length, 1);
    const score = timeframeScores[timeframe] || {};
    article.innerHTML = `
      <h3>${timeframe} <span class="muted">${money(frame.price)}</span></h3>
      <div class="timeframe-graphic">
        <div class="tf-score ${score.points > 0 ? "positive" : score.points < 0 ? "negative" : "neutral"}">
          <strong>${score.points ?? 0}</strong>
          <span>${score.label || "neutral"}</span>
        </div>
        <div class="tf-stack" aria-label="Indikatorzustand">
          <span class="green" style="width:${((green / total) * 100).toFixed(1)}%"></span>
          <span class="orange" style="width:${((orange / total) * 100).toFixed(1)}%"></span>
          <span class="red" style="width:${((red / total) * 100).toFixed(1)}%"></span>
        </div>
        <div class="tf-dots">${statuses.map(status => `<span class="dot ${status.status}" title="${status.message}"></span>`).join("")}</div>
      </div>
    `;
    for (const [name, status] of Object.entries(frame.statuses)) {
      const row = document.createElement("div");
      row.className = "status-row";
      row.innerHTML = `
        <span class="dot ${status.status}"></span>
        <div class="label">${name}<span>${status.message}</span></div>
      `;
      article.append(row);
    }
    frameContainer.append(article);
  }
}

function renderPattern(pattern) {
  const target = document.querySelector("#pattern");
  const rows = [
    ["Status", pattern.pattern_detected ? pattern.pattern_type : "Kein W-Muster"],
    ["Konfidenz", `${pattern.confidence || 0}%`],
    ["Entry", money(pattern.optimal_entry)],
    ["Stop", money(pattern.stop_loss)],
    ["Ziel", money(pattern.target_price)],
    ["R/R", pattern.risk_reward ? pattern.risk_reward.toFixed(2) : "--"],
  ];
  target.innerHTML = rows.map(([label, value]) => `<div class="pattern-row"><strong>${label}</strong><span>${value}</span></div>`).join("");
}

function renderCorrelations(rows) {
  const target = document.querySelector("#correlations");
  if (!rows.length) {
    target.textContent = "Keine Korrelationsdaten";
    return;
  }
  target.innerHTML = rows.map(row => {
    const corrClass = row.correlation >= 0 ? "text-green" : "text-red";
    const strengthClass = row.relative_strength >= 0 ? "text-green" : "text-red";
    return `<div class="corr-row">
      <strong>${row.symbol}</strong>
      <span class="${corrClass}">${row.correlation ?? "--"}</span>
      <span class="${strengthClass}">${row.relative_strength > 0 ? "+" : ""}${row.relative_strength}%</span>
    </div>`;
  }).join("");
}

function renderWarnings(warnings) {
  document.querySelector("#warnings").innerHTML = warnings.map(warning => `<div>${warning}</div>`).join("");
}

function renderDataQuality(dataQuality) {
  const updated = document.querySelector("#updated");
  if (!dataQuality) return;
  const marker = dataQuality.mode === "live" && dataQuality.fallbacks === 0 ? "Live-Daten" : `${dataQuality.mode} · ${dataQuality.fallbacks} Fallbacks`;
  updated.textContent = `${updated.textContent} · ${marker}`;
}

function renderMethodology(methodology) {
  const target = document.querySelector("#methodology");
  if (!methodology) {
    target.textContent = "Keine Methodikdaten";
    return;
  }
  target.innerHTML = `
    <div class="methodology-grid">
      <div class="methodology-block">
        <strong>Modell</strong>
        <p>${methodology.model_type}</p>
        <p class="muted">${methodology.accuracy_status}</p>
      </div>
      <div class="methodology-block">
        <strong>Formel</strong>
        <ul>${methodology.signal_formula.map(item => `<li>${item}</li>`).join("")}</ul>
      </div>
      <div class="methodology-block">
        <strong>Eingebaut</strong>
        <p class="muted">${methodology.macro_status}</p>
        <ul>${(methodology.included_inputs || []).map(item => `<li>${item}</li>`).join("")}</ul>
      </div>
      <div class="methodology-block">
        <strong>Fehlt noch</strong>
        <ul>${methodology.missing_inputs.map(item => `<li>${item}</li>`).join("")}</ul>
      </div>
    </div>
  `;
}

function renderIndicatorAudit(audit) {
  const target = document.querySelector("#indicator-audit");
  if (!target || !audit) return;
  const checks = (audit.checks || []).map(check => `
    <div class="audit-row">
      <span class="dot ${check.status === "match" ? "green" : "orange"}"></span>
      <div>
        <strong>${check.name}</strong>
        <span>${check.tradingview}</span>
        <small>${check.method}</small>
        <small>${check.note}</small>
      </div>
    </div>
  `).join("");
  const values = Object.entries(audit.values || {}).map(([timeframe, row]) => `
    <div class="audit-values">
      <strong>${timeframe}</strong>
      <span>RSI ${row.rsi14 ?? "--"}</span>
      <span>MACD ${row.macd ?? "--"} / ${row.macd_signal ?? "--"}</span>
      <span>Hist ${row.macd_hist ?? "--"}</span>
      <span>EMA20 ${money(row.ema20)}</span>
      <span>EMA50 ${money(row.ema50)}</span>
      <span>SMA200 ${money(row.sma200)}</span>
    </div>
  `).join("");
  target.innerHTML = `
    <div class="methodology-block">
      <p>${audit.source}</p>
      <p class="muted">${audit.feed_warning}</p>
    </div>
    <div class="macro-grid">${checks}</div>
    <div class="audit-table">${values}</div>
  `;
}

function renderMacro(macro) {
  const target = document.querySelector("#macro");
  if (!macro) {
    target.textContent = "Keine Makrodaten";
    return;
  }
  const scoreClass = macro.status === "green" ? "text-green" : macro.status === "red" ? "text-red" : "text-orange";
  const components = (macro.components || []).map(component => `
    <div class="macro-row">
      <span class="dot ${component.status}"></span>
      <div>
        <strong>${component.name}</strong>
        <span>${component.message}</span>
      </div>
    </div>
  `).join("");
  target.innerHTML = `
    <div class="macro-summary">
      <strong class="${scoreClass}">${macro.label}</strong>
      <span>Score: ${macro.score}</span>
      <small>${macro.source_note || ""}</small>
    </div>
    <div class="macro-grid">${components}</div>
  `;
}

async function runBacktest() {
  runBacktestButton.disabled = true;
  runBacktestButton.textContent = "Backtest läuft...";
  const target = document.querySelector("#backtest");
  target.innerHTML = `<p class="muted">Historische Binance-Kerzen werden geladen und Strategien verglichen.</p>`;
  try {
    const demo = demoToggle.checked ? "?demo=1" : "";
    const response = await fetch(`/api/backtest${demo}`);
    const data = await response.json();
    renderBacktest(data);
  } finally {
    runBacktestButton.disabled = false;
    runBacktestButton.textContent = "Backtest starten";
  }
}

async function runOptimizer() {
  runOptimizerButton.disabled = true;
  cancelOptimizerButton.disabled = false;
  runOptimizerButton.textContent = "Optimiert...";
  const target = document.querySelector("#backtest");
  const progressTarget = document.querySelector("#optimizer-progress");
  target.innerHTML = `<p class="muted">Parameter-Kombinationen werden per Backtest, Out-of-sample und Walk-forward verglichen.</p>`;
  progressTarget.innerHTML = `<div class="progress-bar"><span style="width: 0%"></span></div>`;
  try {
    const response = await fetch("/api/optimize/start", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ demo: demoToggle.checked }),
    });
    const data = await response.json();
    optimizerJobId = data.job_id;
    pollOptimizer();
  } catch (error) {
    target.innerHTML = `<p class="muted">Optimizer konnte nicht gestartet werden: ${error}</p>`;
    runOptimizerButton.disabled = false;
    cancelOptimizerButton.disabled = true;
    runOptimizerButton.textContent = "Live optimieren";
  }
}

async function pollOptimizer() {
  if (!optimizerJobId) return;
  const response = await fetch(`/api/optimize/status/${optimizerJobId}`);
  const data = await response.json();
  renderOptimizerProgress(data);
  if (data.status === "running") {
    optimizerPollTimer = window.setTimeout(pollOptimizer, 1200);
    return;
  }
  runOptimizerButton.disabled = false;
  cancelOptimizerButton.disabled = true;
  runOptimizerButton.textContent = "Live optimieren";
  optimizerJobId = null;
  if (data.status === "done" || data.status === "cancelled") {
    renderOptimizer(data.result || { best: data.best, candidates: data.candidates, settings: {} });
  } else if (data.status === "error") {
    document.querySelector("#backtest").innerHTML = `<p class="muted">Optimizer-Fehler: ${data.error}</p>`;
  }
}

function renderOptimizerProgress(data) {
  const target = document.querySelector("#optimizer-progress");
  const total = data.total || 1;
  const pct = Math.round((data.done || 0) / total * 100);
  const rows = (data.candidates || []).slice(0, 5);
  target.innerHTML = `
    <div class="progress-line">
      <div class="progress-bar"><span style="width: ${pct}%"></span></div>
      <span>${data.done || 0}/${data.total || "?"} · ${pct}%</span>
      <span>Status: ${data.status}</span>
    </div>
    <div class="optimizer-ranking">
      ${rows.map(row => `
        <div>
          <strong>Score ${row.score}</strong>
          <span>${row.trades} Trades · PF ${row.avg_profit_factor} · OOS ${row.out_of_sample_score} · WF ${row.walk_forward_score}</span>
        </div>
      `).join("")}
    </div>
    ${renderConvergenceChart(data.best_history || [])}
  `;
}

async function loadForecast() {
  const target = document.querySelector("#forecast");
  runForecastButton.disabled = true;
  runForecastButton.textContent = "Lädt...";
  target.innerHTML = `<p class="muted">Forecast wird aus den letzten 4h-Kerzen berechnet.</p>`;
  try {
    const demo = demoToggle.checked ? "?demo=1" : "";
    const response = await fetch(`/api/forecast${demo}`);
    const data = await response.json();
    renderForecast(data);
  } finally {
    runForecastButton.disabled = false;
    runForecastButton.textContent = "Forecast laden";
  }
}

function renderForecast(data) {
  const target = document.querySelector("#forecast");
  if (data.error) {
    target.innerHTML = `<p class="muted">${data.error}</p>`;
    return;
  }
  target.innerHTML = `
    <div class="backtest-meta">
      <span>${data.settings.mode}</span>
      <span>${data.settings.timeframe}</span>
      <span>${data.settings.candles} Kerzen</span>
      <span>Horizont ${data.settings.horizon}</span>
      <span>Vol ${data.metrics.volatility_pct}%</span>
    </div>
    ${renderForecastChart(data)}
    ${renderLearningCurve(data.learning_curve || [])}
  `;
}

function renderBacktest(data) {
  const target = document.querySelector("#backtest");
  const rows = data.summary || [];
  if (!rows.length) {
    target.innerHTML = `<p class="muted">Keine Trades im Backtest gefunden.</p>`;
    return;
  }
  target.innerHTML = `
    <div class="backtest-meta">
      <span>${data.settings.mode}</span>
      <span>${data.settings.candles} Kerzen</span>
      <span>Horizont ${data.settings.horizon_candles} x 4h</span>
      <span>Backtest nutzt echte 4h/1d-Historie</span>
    </div>
    <div class="backtest-table">
      <div class="backtest-row head">
        <strong>Symbol</strong><strong>Modus</strong><strong>Trades</strong><strong>Winrate</strong><strong>Return</strong><strong>PF</strong><strong>DD</strong>
      </div>
      ${rows.map(row => `
        <div class="backtest-row">
          <span>${row.symbol}</span>
          <span>${signalModeLabels[row.mode] || row.mode}</span>
          <span>${row.trades}</span>
          <span>${row.win_rate}%</span>
          <span class="${row.total_return_pct >= 0 ? "text-green" : "text-red"}">${row.total_return_pct}%</span>
          <span>${row.profit_factor}</span>
          <span>${row.max_drawdown_pct}%</span>
        </div>
      `).join("")}
    </div>
    ${renderPriceChart(rows[0]?.chart || {})}
    ${renderEquityChart(rows[0]?.equity_curve || [])}
  `;
}

function renderOptimizer(data) {
  const target = document.querySelector("#backtest");
  if (!data.best) {
    target.innerHTML = `<p class="muted">Keine optimierbaren Trades gefunden.</p>`;
    return;
  }
  const params = data.best.params;
  const quality = data.best.quality || { passed: false, flags: ["Quality-Daten fehlen"] };
  const bestRun = (data.best.best_runs || [])[0] || {};
  const applyDisabled = quality.passed ? "" : "disabled";
  target.innerHTML = `
    <div class="backtest-meta">
      <span>${data.settings.mode}</span>
      <span>Score ${data.best.score}</span>
      <span>Train ${data.best.train_score}</span>
      <span>OOS ${data.best.out_of_sample_score}</span>
      <span>WF ${data.best.walk_forward_score}</span>
      <span>${data.best.trades} Trades</span>
      <span>PF ${data.best.avg_profit_factor}</span>
      <span>Winrate ${data.best.avg_win_rate}%</span>
      <span>Return ${data.best.total_return_pct}%</span>
      <span>DD ${data.best.max_drawdown_pct}%</span>
    </div>
    <div class="quality ${quality.passed ? "pass" : "fail"}">
      <strong>${quality.passed ? "Quality-Gate bestanden" : "Quality-Gate blockiert"}</strong>
      <span>${quality.flags && quality.flags.length ? quality.flags.join(" · ") : "keine Warnungen"}</span>
    </div>
    <div class="methodology-block">
      <strong>Vorschlag</strong>
      <p class="muted">weak=${params.weak_buy}, buy=${params.buy}, strong=${params.strong_buy}, R/R=${params.rr_good}/${params.rr_excellent}, RSI=${params.rsi_oversold}/${params.rsi_bullish}/${params.rsi_overbought}</p>
      <button type="button" id="apply-optimizer" ${applyDisabled}>Vorschlag übernehmen</button>
    </div>
    ${renderConvergenceChart(data.convergence || [])}
    ${renderPriceChart(bestRun.chart || {})}
    ${renderEquityChart(bestRun.equity_curve || [])}
  `;
  const applyButton = document.querySelector("#apply-optimizer");
  if (!quality.passed) return;
  applyButton.addEventListener("click", async () => {
    await fetch("/api/optimize/apply", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ signal_params: params, symbol_params: data.best.symbol_params || {} }),
    });
    await refresh();
  });
}

function renderForecastChart(data) {
  const history = data.history || [];
  const forecast = data.forecast || [];
  if (!history.length || !forecast.length) return "";
  const width = 760;
  const height = 330;
  const padLeft = 74;
  const padRight = 24;
  const padTop = 20;
  const padBottom = 42;
  const allValues = [
    ...history.flatMap(point => [point.high, point.low]),
    ...forecast.flatMap(point => [point.upper, point.lower, point.price]),
  ];
  const min = Math.min(...allValues);
  const max = Math.max(...allValues);
  const total = history.length + forecast.length;
  const x = index => padLeft + index * ((width - padLeft - padRight) / Math.max(total - 1, 1));
  const y = value => height - padBottom - ((value - min) / Math.max(max - min, 1)) * (height - padTop - padBottom);
  const histLine = history.map((point, index) => `${index ? "L" : "M"}${x(index).toFixed(1)} ${y(point.close).toFixed(1)}`).join(" ");
  const start = history.length - 1;
  const forecastLine = forecast.map((point, index) => `${index ? "L" : "M"}${x(start + index + 1).toFixed(1)} ${y(point.price).toFixed(1)}`).join(" ");
  const upper = forecast.map((point, index) => `${index ? "L" : "M"}${x(start + index + 1).toFixed(1)} ${y(point.upper).toFixed(1)}`).join(" ");
  const lower = forecast.map((point, index) => `L${x(start + forecast.length - index).toFixed(1)} ${y(forecast[forecast.length - 1 - index].lower).toFixed(1)}`).join(" ");
  const splitX = x(start);
  const firstTime = history[0]?.time;
  const lastHistoryTime = history[history.length - 1]?.time;
  const forecastLabel = `+${forecast[forecast.length - 1].step} Kerzen`;
  const frame = renderChartFrame({
    width,
    height,
    padLeft,
    padRight,
    padTop,
    padBottom,
    minY: min,
    maxY: max,
    y,
    xTicks: [
      { x: x(0), label: formatChartTime(firstTime), anchor: "start" },
      { x: splitX, label: formatChartTime(lastHistoryTime) },
      { x: x(total - 1), label: forecastLabel, anchor: "end" },
    ],
    splitX,
  });
  return `
    <div class="chart-card">
      <strong>Forecast-Pfad</strong>
      <svg class="forecast-chart" viewBox="0 0 ${width} ${height}" role="img" aria-label="Forecast mit Konfidenzband">
        ${frame}
        <path class="forecast-band" d="${upper} ${lower} Z"></path>
        <path class="history-line" d="${histLine}"></path>
        <path class="forecast-line" d="${forecastLine}"></path>
        <text class="chart-label" x="${(splitX + 8).toFixed(1)}" y="${(padTop + 14).toFixed(1)}">Forecast</text>
      </svg>
      <div class="chart-legend"><span>Historie</span><span class="text-green">Forecast</span><span>Band = Unsicherheit</span></div>
    </div>
  `;
}

function renderLearningCurve(points) {
  if (!points.length) return "";
  const series = points.map(point => ({ x: point.window, y: point.error_pct }));
  return renderLineCard("Lernkurve: Fehler sinkt mit mehr Historie", series, "window", "error_pct", true);
}

function renderConvergenceChart(points) {
  if (!points.length) return "";
  const series = points.map(point => ({ x: point.step, y: point.best_score }));
  return renderLineCard("Optimizer-Konvergenz: bester Score", series, "step", "best_score", false);
}

function renderLineCard(title, points, xLabel, yLabel, invertGood) {
  const width = 720;
  const height = 250;
  const padLeft = 66;
  const padRight = 22;
  const padTop = 20;
  const padBottom = 42;
  const xs = points.map(point => point.x);
  const ys = points.map(point => point.y);
  const minX = Math.min(...xs);
  const maxX = Math.max(...xs);
  const minY = Math.min(...ys);
  const maxY = Math.max(...ys);
  const x = value => padLeft + ((value - minX) / Math.max(maxX - minX, 1)) * (width - padLeft - padRight);
  const y = value => height - padBottom - ((value - minY) / Math.max(maxY - minY, 1)) * (height - padTop - padBottom);
  const line = points.map((point, index) => `${index ? "L" : "M"}${x(point.x).toFixed(1)} ${y(point.y).toFixed(1)}`).join(" ");
  const last = points[points.length - 1];
  const frame = renderChartFrame({
    width,
    height,
    padLeft,
    padRight,
    padTop,
    padBottom,
    minY,
    maxY,
    y,
    ySuffix: yLabel.includes("pct") ? "%" : "",
    zeroY: minY < 0 && maxY > 0 ? y(0) : null,
    xTicks: [
      { x: x(minX), label: compactNumber(minX), anchor: "start" },
      { x: x((minX + maxX) / 2), label: compactNumber((minX + maxX) / 2) },
      { x: x(maxX), label: compactNumber(maxX), anchor: "end" },
    ],
  });
  return `
    <div class="chart-card">
      <strong>${title}</strong>
      <svg class="metric-chart" viewBox="0 0 ${width} ${height}" role="img" aria-label="${title}">
        ${frame}
        <path class="${invertGood ? "drawdown-line" : "equity-line"}" d="${line}"></path>
        <circle class="chart-point" cx="${x(last.x).toFixed(1)}" cy="${y(last.y).toFixed(1)}" r="4"></circle>
        <text class="chart-label" x="${(padLeft + 2).toFixed(1)}" y="${(padTop + 12).toFixed(1)}">${yLabel}</text>
        <text class="chart-label" x="${(width - padRight).toFixed(1)}" y="${(height - 24).toFixed(1)}" text-anchor="end">${xLabel}</text>
      </svg>
      <div class="chart-legend"><span>${xLabel}: ${last.x}</span><span>${yLabel}: ${last.y}</span></div>
    </div>
  `;
}

async function loadHistory() {
  const response = await fetch("/api/history?limit=12");
  const data = await response.json();
  const target = document.querySelector("#history");
  target.innerHTML = (data.runs || []).map(run => {
    const payload = run.payload || {};
    const best = payload.best || (payload.summary || [])[0] || {};
    return `<div class="history-row">
      <strong>#${run.id} ${run.kind}</strong>
      <span>${new Date(run.created_at * 1000).toLocaleString("de-CH")}</span>
      <span>${run.label || "--"}</span>
      <span>Trades ${best.trades ?? "--"} · Return ${best.total_return_pct ?? "--"} · Score ${best.score ?? "--"}</span>
    </div>`;
  }).join("") || `<p class="muted">Keine gespeicherten Runs.</p>`;
}

async function loadHealth() {
  const response = await fetch("/api/health");
  const data = await response.json();
  const target = document.querySelector("#health");
  if (!target) return;
  target.innerHTML = `
    <div class="pattern-row"><strong>Status</strong><span class="text-green">${data.status}</span></div>
    <div class="pattern-row"><strong>Execution</strong><span>${data.execution.mode} · Kill ${data.execution.kill_switch ? "an" : "aus"}</span></div>
    <div class="pattern-row"><strong>Exchange</strong><span>${data.exchange_guard.live_ready ? "live bereit" : data.exchange_guard.blockers.join(" · ")}</span></div>
    <div class="pattern-row"><strong>Risk/Trade</strong><span>${data.risk_management.risk_per_trade_pct}%</span></div>
    <div class="pattern-row"><strong>Optimizer</strong><span>${data.optimizer_jobs.running} laufend · ${data.optimizer_jobs.total} total</span></div>
  `;
}

function renderPriceChart(chart) {
  const candles = chart.candles || [];
  if (!candles.length) return "";
  const trades = chart.trades || [];
  const width = 720;
  const height = 300;
  const padLeft = 72;
  const padRight = 22;
  const padTop = 20;
  const padBottom = 42;
  const lows = candles.map(candle => candle.low);
  const highs = candles.map(candle => candle.high);
  const min = Math.min(...lows);
  const max = Math.max(...highs);
  const xStep = (width - padLeft - padRight) / Math.max(candles.length, 1);
  const y = value => height - padBottom - ((value - min) / Math.max(max - min, 1)) * (height - padTop - padBottom);
  const candleSvg = candles.map((candle, index) => {
    const x = padLeft + index * xStep + xStep / 2;
    const color = candle.close >= candle.open ? "up" : "down";
    const bodyTop = y(Math.max(candle.open, candle.close));
    const bodyHeight = Math.max(2, Math.abs(y(candle.open) - y(candle.close)));
    return `<g class="candle ${color}">
      <line x1="${x.toFixed(1)}" y1="${y(candle.high).toFixed(1)}" x2="${x.toFixed(1)}" y2="${y(candle.low).toFixed(1)}"></line>
      <rect x="${(x - Math.max(2, xStep * 0.28)).toFixed(1)}" y="${bodyTop.toFixed(1)}" width="${Math.max(3, xStep * 0.56).toFixed(1)}" height="${bodyHeight.toFixed(1)}"></rect>
    </g>`;
  }).join("");
  const firstTime = candles[0].time || 0;
  const lastTime = candles[candles.length - 1].time || firstTime + 1;
  const timeX = time => padLeft + ((time - firstTime) / Math.max(lastTime - firstTime, 1)) * (width - padLeft - padRight);
  const tradeSvg = trades.map(trade => {
    const x = timeX(trade.entry_time || firstTime);
    return `<g class="trade-marker">
      <circle cx="${x.toFixed(1)}" cy="${y(trade.entry).toFixed(1)}" r="4"></circle>
      <line x1="${padLeft}" y1="${y(trade.entry).toFixed(1)}" x2="${width - padRight}" y2="${y(trade.entry).toFixed(1)}"></line>
      <line class="stop" x1="${padLeft}" y1="${y(trade.stop || trade.exit).toFixed(1)}" x2="${width - padRight}" y2="${y(trade.stop || trade.exit).toFixed(1)}"></line>
      <line class="target" x1="${padLeft}" y1="${y(trade.target || trade.exit).toFixed(1)}" x2="${width - padRight}" y2="${y(trade.target || trade.exit).toFixed(1)}"></line>
    </g>`;
  }).join("");
  const frame = renderChartFrame({
    width,
    height,
    padLeft,
    padRight,
    padTop,
    padBottom,
    minY: min,
    maxY: max,
    y,
    xTicks: [
      { x: timeX(firstTime), label: formatChartTime(firstTime), anchor: "start" },
      { x: timeX((firstTime + lastTime) / 2), label: formatChartTime((firstTime + lastTime) / 2) },
      { x: timeX(lastTime), label: formatChartTime(lastTime), anchor: "end" },
    ],
  });
  return `
    <div class="chart-card">
      <strong>Preis / Trades</strong>
      <svg class="price-chart" viewBox="0 0 ${width} ${height}" role="img" aria-label="Kerzenchart mit Trades">
        ${frame}
        ${candleSvg}
        ${tradeSvg}
        <text class="chart-label" x="${(padLeft + 2).toFixed(1)}" y="${(padTop + 12).toFixed(1)}">Preis</text>
      </svg>
      <div class="chart-legend"><span class="text-green">gruen: Aufwaertskerze</span><span class="text-red">rot: Abwaertskerze/Exit</span></div>
    </div>
  `;
}

async function loadPaperOrders() {
  const response = await fetch("/api/paper/orders");
  const data = await response.json();
  const target = document.querySelector("#paper-orders");
  target.innerHTML = (data.orders || []).map(order => `
    <div class="history-row">
      <strong>#${order.id} ${order.symbol}</strong>
      <span>${order.side} · ${order.status}</span>
      <span>Menge ${order.quantity}</span>
      <span>Entry ${money(order.entry)} · Stop ${money(order.stop)} · Ziel ${money(order.target)}</span>
    </div>
  `).join("") || `<p class="muted">Keine Paper Orders.</p>`;
}

async function createPaperOrder() {
  if (!latestAnalysis) return;
  const response = await fetch("/api/paper/order", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ signal: latestAnalysis.signal }),
  });
  if (!response.ok) {
    const data = await response.json();
    document.querySelector("#paper-orders").innerHTML = `<p class="muted">${data.error}: ${(data.risk_plan?.blockers || []).join(" · ")}</p>`;
    return;
  }
  await loadPaperOrders();
}

function renderEquityChart(points) {
  if (!points.length) return "";
  const width = 720;
  const height = 250;
  const padLeft = 66;
  const padRight = 22;
  const padTop = 20;
  const padBottom = 42;
  const values = points.map(point => point.equity_pct);
  const dds = points.map(point => -Math.abs(point.drawdown_pct || 0));
  const min = Math.min(...values, ...dds, -1);
  const max = Math.max(...values, 1);
  const x = index => padLeft + index * ((width - padLeft - padRight) / Math.max(points.length - 1, 1));
  const y = value => height - padBottom - ((value - min) / Math.max(max - min, 1)) * (height - padTop - padBottom);
  const equityLine = points.map((point, index) => `${index ? "L" : "M"}${x(index).toFixed(1)} ${y(point.equity_pct).toFixed(1)}`).join(" ");
  const drawdownLine = points.map((point, index) => `${index ? "L" : "M"}${x(index).toFixed(1)} ${y(-Math.abs(point.drawdown_pct || 0)).toFixed(1)}`).join(" ");
  const frame = renderChartFrame({
    width,
    height,
    padLeft,
    padRight,
    padTop,
    padBottom,
    minY: min,
    maxY: max,
    y,
    ySuffix: "%",
    zeroY: y(0),
    xTicks: [
      { x: x(0), label: "Start", anchor: "start" },
      { x: x(Math.floor((points.length - 1) / 2)), label: "Mitte" },
      { x: x(points.length - 1), label: "Ende", anchor: "end" },
    ],
  });
  return `
    <div class="chart-card">
      <strong>Equity / Drawdown</strong>
      <svg viewBox="0 0 ${width} ${height}" role="img" aria-label="Equity und Drawdown">
        ${frame}
        <path class="equity-line" d="${equityLine}"></path>
        <path class="drawdown-line" d="${drawdownLine}"></path>
        <text class="chart-label" x="${(padLeft + 2).toFixed(1)}" y="${(padTop + 12).toFixed(1)}">%</text>
      </svg>
      <div class="chart-legend"><span class="text-green">Equity %</span><span class="text-red">Drawdown %</span></div>
    </div>
  `;
}

function activateTab(name) {
  document.querySelectorAll(".tab-button").forEach(button => {
    button.classList.toggle("active", button.dataset.tab === name);
  });
  document.querySelectorAll(".tab-panel").forEach(panel => {
    panel.classList.toggle("active", panel.id === `tab-${name}`);
  });
  if (name === "optimizer") loadHistory();
  if (name === "forecast") loadForecast();
  if (name === "risk") {
    loadHealth();
    loadPaperOrders();
  }
}

form.addEventListener("submit", async event => {
  event.preventDefault();
  const symbol = (customSymbol.value || symbolSelect.value).trim().toUpperCase();
  const signalMode = signalModeSelect.value;
  if (!symbol) return;
  await saveSettings(symbol, signalMode);
  customSymbol.value = "";
  await refresh();
});

refreshButton.addEventListener("click", refresh);
runBacktestButton.addEventListener("click", runBacktest);
runOptimizerButton.addEventListener("click", runOptimizer);
runForecastButton.addEventListener("click", loadForecast);
refreshHistoryButton.addEventListener("click", loadHistory);
refreshOrdersButton.addEventListener("click", loadPaperOrders);
paperOrderButton.addEventListener("click", createPaperOrder);
primaryPaperOrderButton.addEventListener("click", createPaperOrder);
cancelOptimizerButton.addEventListener("click", async () => {
  if (!optimizerJobId) return;
  await fetch(`/api/optimize/cancel/${optimizerJobId}`, { method: "POST" });
  cancelOptimizerButton.disabled = true;
});
demoToggle.addEventListener("change", refresh);
signalModeSelect.addEventListener("change", async () => {
  await saveSettings(symbolSelect.value, signalModeSelect.value);
  await refresh();
});
document.querySelectorAll(".tab-button").forEach(button => {
  button.addEventListener("click", () => activateTab(button.dataset.tab));
});

loadConfig().then(refresh);
