// Liefertreue Trafag AG (VKORG 1100): Datenabzug im Fiori Launchpad (eingeloggt, trasp76s.sap.trafag.com)
// Startet im Hintergrund. Ergebnis in window.__lt (done=true wenn fertig). Danach: JSON.stringify(window.__lt)
// Berichtsmonat = Vormonat (bzw. window.__ltCut = 'JJJJMM' vorher setzen)
window.__lt = {done: false, log: []};
(async () => {
const LT = window.__lt, H = {headers: {Accept: 'application/json'}};
try {
  const now = new Date();
  const pm = new Date(now.getFullYear(), now.getMonth() - 1, 1);
  const CUT = window.__ltCut || (pm.getFullYear() + String(pm.getMonth() + 1).padStart(2, '0'));
  const YEAR = CUT.slice(0, 4);
  LT.cut = CUT; LT.year = YEAR; LT.stand = now.toISOString().slice(0, 10);
  const SO = '/sap/opu/odata/sap/C_SLSORDDELIVPERFANLYTSQRY_CDS/';
  const SOR = SO + "C_SLSORDDELIVPERFANLYTSQRY(P_ExchangeRateType='M',P_DisplayCurrency='CHF')/Results?sap-client=100&$format=json";
  const DL = "/sap/opu/odata/sap/C_DELIVITMPERFANLYTSQRY_CDS/C_DELIVITMPERFANLYTSQRY(P_ShippedAsPlannedOffsetDays=0)/Results?sap-client=100&$format=json";
  const getAll = async (url, page) => { let out = [], skip = 0; while (true) { const j = await fetch(url + '&$top=' + page + '&$skip=' + skip, H).then(r => r.json()); if (!j.d) throw new Error('OData: ' + JSON.stringify(j).slice(0, 300)); out = out.concat(j.d.results); if (j.d.results.length < page) break; skip += page; } return out; };
  const vh = async (set, key, txt, term) => { const j = await fetch(SO + set + "?sap-client=100&$format=json&$top=1000&$select=" + key + ',' + txt + "&$filter=" + encodeURIComponent(`substringof('${term}',${txt})`), H).then(r => r.json()); return j.d ? j.d.results.map(r => [r[key], r[txt]]) : []; };

  // Ausschluesse (Runbook)
  const exC = new Set(['302906', '301497', '24687', '500731', '301940', '302215', '301873', '19914']);
  const certs = [].concat(await vh('Product', 'Product_ID', 'ProductText', 'EN10204'), await vh('Product', 'Product_ID', 'ProductText', 'EN 10204'));
  const exP = new Set(certs.map(p => p[0]));
  const tr = [].concat(await vh('SoldToParty', 'SoldToParty_ID', 'SoldToPartyText', 'rafag'), await vh('SoldToParty', 'SoldToParty_ID', 'SoldToPartyText', 'RAFAG'));
  const ic = new Set(tr.map(t => t[0]).filter(id => !exC.has(id)));
  LT.log.push('certs ' + exP.size + ', trafag ' + ic.size);

  // Auftragspositionen
  const sel = '&$select=SalesOrder,SalesOrderItem,SalesOrderDate,CommittedDeliveryDate,CmtdDelivDateYearMonth,ReqdDelivDateYearMonth,EarliestReqdDelivDate,DeliveryStatus,SoldToParty,Product,ProductHierarchyNode,NetAmountInDisplayCurrency,DelivToCmtdDelivDelayInDays';
  let rows = await getAll(SOR + sel + '&$filter=' + encodeURIComponent(`SalesOrganization eq '1100' and CmtdDelivDateYear eq '${YEAR}'`), 10000);
  rows = rows.concat(await getAll(SOR + sel + '&$filter=' + encodeURIComponent(`SalesOrganization eq '1100' and ReqdDelivDateYear eq '${YEAR}' and CmtdDelivDateYear ne '${YEAR}'`), 10000));
  LT.log.push('so ' + rows.length);

  // Ist-Warenausgang
  const norm = v => { if (!v) return ''; if (/^\d{8}$/.test(v)) return v; const m = /\((-?\d+)/.exec(v); if (m) { const d = new Date(+m[1]); return d.getUTCFullYear() + String(d.getUTCMonth() + 1).padStart(2, '0') + String(d.getUTCDate()).padStart(2, '0'); } return String(v).replace(/-/g, '').slice(0, 8); };
  const dl = await getAll(DL + '&$select=OutboundDelivery,OutboundDeliveryItem,ActualGoodsMovementDate,ReferenceSDDocument,ReferenceSDDocumentItem,NumberOfDeliveryItems&$filter=' + encodeURIComponent(`ActualGoodsMovementDate ge '${+YEAR - 1}0901'`), 10000);
  const gi = {}; dl.forEach(x => { const a = norm(x.ActualGoodsMovementDate); if (!a || a < '2000') return; const k = String(+x.ReferenceSDDocument) + '/' + String(+x.ReferenceSDDocumentItem); if (!gi[k] || a > gi[k]) gi[k] = a; });
  LT.log.push('deliv ' + dl.length + ', items ' + Object.keys(gi).length);

  const toD = s => new Date(+s.slice(0, 4), +s.slice(4, 6) - 1, +s.slice(6, 8));
  const at = (a, b) => { if (!a || !b || a < '1990' || b < '1990') return null; const x = toD(a), y = toD(b); const sg = y >= x ? 1 : -1; const s = sg > 0 ? x : y, e = sg > 0 ? y : x; const days = Math.round((e - s) / 864e5); const full = Math.floor(days / 7); let n = full * 5; const c = new Date(s); c.setDate(c.getDate() + full * 7); while (c < e) { c.setDate(c.getDate() + 1); const w = c.getDay(); if (w && w < 6) n++; } return sg * n; };
  const med = a => { if (!a.length) return null; const s = [...a].sort((x, y) => x - y); return s[Math.floor(s.length / 2)]; };
  const it = rows.map(r => {
    const k = r.SalesOrder + '/' + r.SalesOrderItem, done = r.DeliveryStatus === 'C', ist = done && gi[k] ? gi[k] : '', noGi = done && !gi[k];
    let lc = null, lr = null, okCs, okCt, okRs, okRt;
    if (ist) { lc = at(r.CommittedDeliveryDate, ist); lr = at(r.EarliestReqdDelivDate, ist); okCs = lc <= 0; okCt = lc < 5; okRs = lr <= 0; okRt = lr < 5; }
    else if (noGi) { const d = +r.DelivToCmtdDelivDelayInDays; okCs = okCt = okRs = okRt = d <= 0; }
    else okCs = okCt = okRs = okRt = false;
    return { so: r.SalesOrder, it: r.SalesOrderItem, cd: r.CommittedDeliveryDate, cm: r.CmtdDelivDateYearMonth, rd: r.EarliestReqdDelivDate, rm: r.ReqdDelivDateYearMonth, st: r.DeliveryStatus, cu: r.SoldToParty, pr: r.Product, pg: r.ProductHierarchyNode || '(leer)', amt: Math.round(+r.NetAmountInDisplayCurrency), ist, noGi, lc, lr, okCs, okCt, okRs, okRt,
      ex: exC.has(r.SoldToParty) ? 'K' : exP.has(r.Product) ? 'Z' : '', grp: (ic.has(r.SoldToParty) || (exC.has(r.SoldToParty) && r.SoldToParty !== '19914')) ? 'I' : 'E',
      wlz: at(r.SalesOrderDate, r.EarliestReqdDelivDate), blz: at(r.SalesOrderDate, r.CommittedDeliveryDate) };
  });
  const Y1 = YEAR + '01', inC = i => i.cm >= Y1 && i.cm <= CUT, inR = i => i.rm >= Y1 && i.rm <= CUT;
  const M = {}, P = {}, O = {};
  it.forEach(i => {
    if (i.ex) return;
    if (i.cm && i.cm.startsWith(YEAR)) { const o = O[i.so] || (O[i.so] = {max: '0', n: 0, s: 0, t: 0}); if (i.cm > o.max) o.max = i.cm; o.n++; if (i.okCs) o.s++; if (i.okCt) o.t++; }
    if (inC(i)) { const m = M[i.cm] || (M[i.cm] = {n: 0, s: 0, t: 0, e5: 0, ng: 0}); m.n++; if (i.okCs) m.s++; if (i.okCt) m.t++; if (i.lc != null && i.lc <= -5) m.e5++; if (i.noGi) m.ng++; }
    if (inR(i)) { const p = P[i.rm] || (P[i.rm] = {n: 0, s: 0, t: 0}); p.n++; if (i.okRs) p.s++; if (i.okRt) p.t++; }
  });
  const OM = {}; Object.values(O).forEach(o => { if (o.max > CUT) return; const x = OM[o.max] || (OM[o.max] = {n: 0, s: 0, t: 0}); x.n++; if (o.s === o.n) x.s++; if (o.t === o.n) x.t++; });
  LT.otd = Object.keys(M).sort().map(k => [k, M[k].n, M[k].s, M[k].t, (OM[k] || {}).n || 0, (OM[k] || {}).s || 0, (OM[k] || {}).t || 0, M[k].e5, M[k].ng].join(';'));
  LT.otp = Object.keys(P).sort().map(k => [k, P[k].n, P[k].s, P[k].t].join(';'));

  // Produktgruppen / Wunschtermin-Machbarkeit
  let T = {}; try { const j = await fetch(SO + 'ProductHierarchyNode?sap-client=100&$format=json&$top=5000', H).then(r => r.json()); (j.d ? j.d.results : []).forEach(r => T[r.ProductHierarchyNode_ID] = r.ProductHierarchyNodeText); } catch (e) {}
  const bl = {}; it.forEach(i => { if (!i.ex && inC(i) && i.blz != null) (bl[i.pg] = bl[i.pg] || []).push(i.blz); });
  const allMed = med(Object.values(bl).flat()); const ref = {}; Object.entries(bl).forEach(([k, v]) => ref[k] = v.length >= 20 ? med(v) : allMed);
  const G = {}, WM = {}, tot = {rn: 0, rok: 0, kn: 0, kok: 0};
  it.forEach(i => { if (i.ex) return; const g = G[i.pg] || (G[i.pg] = {n: 0, ok: 0, rn: 0, rok: 0, kn: 0, kok: 0, w: [], nC: 0, okC: 0});
    if (inC(i)) { g.nC++; if (i.okCt) g.okC++; }
    if (!inR(i) || i.wlz == null) return; const real = i.wlz >= (ref[i.pg] ?? allMed);
    g.n++; if (i.okRt) g.ok++; g.w.push(i.wlz); const m = WM[i.rm] || (WM[i.rm] = {rn: 0, rok: 0, kn: 0, kok: 0});
    if (real) { g.rn++; m.rn++; tot.rn++; if (i.okRt) { g.rok++; m.rok++; tot.rok++; } } else { g.kn++; m.kn++; tot.kn++; if (i.okRt) { g.kok++; m.kok++; tot.kok++; } } });
  LT.pg = {allMed, tot, m: Object.keys(WM).sort().map(k => [k, WM[k].rn, WM[k].rok, WM[k].kn, WM[k].kok].join(';')),
    g: Object.entries(G).filter(([k, g]) => g.n > 0).sort((a, b) => b[1].n - a[1].n).map(([k, g]) => [k, T[k] || '', g.n, g.ok, g.rn, g.rok, g.kn, g.kok, med(g.w), bl[k] ? med(bl[k]) : '', ref[k] ?? allMed, g.nC, g.okC, bl[k] ? bl[k].length : 0].join(';'))};

  // Abweichungsliste: zu spaet (nicht OTD) und zu frueh (>= 5 AT)
  const list = it.filter(i => inC(i) && (!i.okCt || (i.lc != null && i.lc <= -5)));
  LT.list = list.map(i => [i.so, i.it, i.cd, i.ist, i.lc == null ? '' : i.lc, i.st, i.cu, i.pr, i.amt, i.grp, i.ex, i.rd, i.lr == null ? '' : i.lr, i.noGi ? 1 : 0, i.okCt ? 'F' : 'L'].join(';'));
  const names = {c: {}, p: {}};
  for (const [set, key, txt, f, tg] of [['SoldToParty', 'SoldToParty_ID', 'SoldToPartyText', 'cu', 'c'], ['Product', 'Product_ID', 'ProductText', 'pr', 'p']]) {
    const keys = [...new Set(list.map(i => i[f]))], b = []; for (let x = 0; x < keys.length; x += 30) b.push(keys.slice(x, x + 30));
    for (let k = 0; k < b.length; k += 8) await Promise.all(b.slice(k, k + 8).map(async bb => { try { const j = await fetch(SO + set + '?sap-client=100&$format=json&$select=' + key + ',' + txt + '&$filter=' + encodeURIComponent(bb.map(x => `${key} eq '${x}'`).join(' or ')), H).then(r => r.json()); (j.d ? j.d.results : []).forEach(x => names[tg][x[key]] = x[txt]); } catch (e) {} }));
  }
  LT.names = names;
} catch (e) { LT.err = String(e); }
LT.done = true;
})();
'started';
