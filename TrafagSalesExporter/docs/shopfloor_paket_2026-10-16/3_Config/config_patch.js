function patchConfig(c) {
  const log = [];
  const mod = k => c.modules.find(m => m.key === k);
  const has = (m, k) => (m.fields || []).some(f => f.key === k);
  // 1) PPD: Verzug MTS entfernen, Summe ohne MTS, Verlaufskacheln Verzug + Terminverschiebungen
  const ppd = mod("ppd");
  if (ppd) {
    const n0 = ppd.fields.length; ppd.fields = ppd.fields.filter(f => f.key !== "verzug_mts"); if (ppd.fields.length < n0) log.push("ppd.verzug_mts entfernt");
    const vt = ppd.fields.find(f => f.key === "verzug_total");
    if (vt && /verzug_mts/.test(vt.calc || "")) { vt.calc = vt.calc.replace(/\s*\+\s*v\.verzug_mts\b/g, "").replace(/\bv\.verzug_mts\s*\+\s*/g, ""); log.push("ppd.verzug_total = " + vt.calc); }
    ppd.trend = ["verzug_total", "verzug_seh", "verzug_tr5", "verzug_tx", "verzug_dw", "tv_total", "tv_seh", "tv_tr5", "tv_tx", "tv_dw"].filter(k => has(ppd, k));
    log.push("ppd.trend = " + ppd.trend.join(","));
  }
  // 2) Produktionsabteilungen: Verlaufskacheln inkl. Auftragsvorrat 1 Mte
  for (const k of ["seh", "tr5", "tx", "dw"]) {
    const m = mod(k); if (!m) continue;
    if (k === "seh") {   // FPY SEH aus SAP (kein MES): WE-Menge / Gesamtmenge der Aufträge mit Istendtermin in der KW
      const fp = m.fields.find(f => f.key === "fpy");
      if (fp) { fp.planned_source = "sap"; fp.label = "FPY (SAP, KW nach Istendtermin)"; fp.digits = 1; log.push("seh.fpy -> SAP"); }
    }
    m.trend = ["prod", "vorrat_1m", "vorrat_total", "offen_kw", "leistung"].concat(k === "seh" ? ["fpy"] : []).filter(x => has(m, x));
    log.push(k + ".trend = " + m.trend.join(","));
  }
  // 3) Ampel pro Bereich
  c.lists = c.lists || {}; if (!c.lists.ampel) { c.lists.ampel = ["grün", "orange", "rot"]; log.push("lists.ampel angelegt"); }
  for (const k of ["seh", "tr5", "tx", "dw", "ppd", "logistik", "unterhalt", "verkauf", "cz", "mtf"]) {
    const m = mod(k); if (!m || has(m, "ampel")) continue;
    m.fields.push(
      { key: "ampel", label: "Status Bereich (Ampel)", type: "select", opts: "ampel", group: "Status Bereich" },
      { key: "ampel_pct", label: "Status Bereich Erfüllung", type: "num", unit: "%", group: "Status Bereich" },
      { key: "ampel_text", label: "Status Bereich Bemerkung", type: "text", group: "Status Bereich" });
    log.push(k + ": Ampel-Felder");
  }
  // 4) Lieferzeiten: Grund je Abteilung + Zielwerte
  // Zielwerte in Wochen: von Teamleitern / Daniel Tobler festzulegen und hier eintragen (null = noch offen)
  const ZIEL = { "seh.lz_tr5": null, "seh.lz_tx": null, "tr5.lz_cold": null, "tr5.lz_hot": null, "tr5.lz_8292": null, "tx.lz_cold": null, "tx.lz_hot": null, "tx.lz_838x": null, "dw.lz_std": null, "dw.lz_hybrid": null, "dw.lz_nd": null, "dw.lz_87x9": null, "dw.lz_877x": null };
  for (const k of ["seh", "tr5", "tx", "dw"]) {
    const m = mod(k); if (!m) continue;
    m.fields.forEach(f => { const z = ZIEL[k + "." + f.key]; if (z != null && f.ziel == null) { f.ziel = z; log.push(k + "." + f.key + ".ziel = " + z); } });
    if (!has(m, "lz_grund")) {
      const idx = m.fields.map(f => /^lz_/.test(f.key)).lastIndexOf(true);
      const g = { key: "lz_grund", label: "Grund lange Lieferzeit", type: "long", group: (m.fields[idx] || {}).group || "Lieferzeiten & Qualität" };
      if (idx >= 0) m.fields.splice(idx + 1, 0, g); else m.fields.push(g);
      log.push(k + ".lz_grund");
    }
  }
  // 5) Forecast: DW nur fertige Dichtewächter, Dichtesensoren als eigene Zeile DS (DW1 Rüstpapiere, DL6 Nachschub nicht zugeordnet)
  if (c.forecast_prefix) {
    c.forecast_prefix.DW = ["D10", "D11", "D12", "D13", "D20", "D21", "D22", "D23", "D30", "D31", "D40", "DB1", "DB2", "DEF", "DW2", "DW"];
    c.forecast_prefix.DS = ["DS1", "DS2", "DSN", "DS"];
    log.push("forecast_prefix.DW = " + c.forecast_prefix.DW.join(",")); log.push("forecast_prefix.DS = " + c.forecast_prefix.DS.join(","));
  }
  return log;
}
if (typeof module !== "undefined") module.exports = patchConfig;
