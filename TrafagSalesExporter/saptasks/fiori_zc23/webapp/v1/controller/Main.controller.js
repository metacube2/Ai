sap.ui.define([
  "sap/ui/core/mvc/Controller",
  "sap/ui/model/json/JSONModel",
  "sap/ui/model/Filter",
  "sap/ui/model/FilterOperator",
  "sap/ui/table/Column",
  "sap/m/Label",
  "sap/m/Text",
  "sap/ui/core/format/NumberFormat",
  "sap/m/MessageBox"
], function (Controller, JSONModel, Filter, FilterOperator, Column, Label, Text, NumberFormat, MessageBox) {
  "use strict";

  // Spalten wie im ALV von ZC23 (Texte aus dem Feldkatalog von ZM_OFFENE_KDAUF). Nicht genannte Felder
  // kommen ausgeblendet dazu und lassen sich ueber das Spaltenmenue einblenden.
  var SPALTEN = [
    { f: "Edatu", t: "Liefertermin", typ: "datum", w: "7rem" },
    { f: "Vbeln", t: "Auftrag", w: "6.5rem" },
    { f: "Posnr", t: "Pos.", w: "4.5rem" },
    { f: "Kunnr", t: "Kunde", w: "6rem" },
    { f: "Name1", t: "Kundenname", w: "13rem" },
    { f: "Matnr", t: "Material", w: "8rem" },
    { f: "Maktx", t: "Materialtext", w: "15rem" },
    { f: "Omengn", t: "Neu.Menge", typ: "menge", w: "7rem" },
    { f: "Meins", t: "ME", w: "3.5rem" },
    { f: "Netwr3n", t: "Neu.Nettowert", typ: "betrag", w: "8.5rem" },
    { f: "Standardwert", t: "Standardwert", typ: "betrag", w: "8.5rem" },
    { f: "Matanteil", t: "Materialanteil", typ: "betrag", w: "8.5rem" },
    { f: "ArbeitCz", t: "Arbeit CZ", typ: "betrag", w: "8rem" },
    { f: "Aufnr", t: "Fertigungsauftrag", w: "8rem" },
    { f: "Gltrp", t: "Eckendtermin", typ: "datum", w: "7rem" },
    { f: "DispoDes", t: "Dispogruppe", w: "8rem" },
    { f: "Waers", t: "Währung", w: "4.5rem" }
  ];
  var AUSBLENDEN = { Rownr: 1, Datvon: 1, Datbis: 1, Mata: 1, Mjung: 1, Mfrem: 1, Korr: 1, Ernam: 1, __metadata: 1 };
  var BETRAG = NumberFormat.getFloatInstance({ minFractionDigits: 2, maxFractionDigits: 2, groupingEnabled: true, groupingSeparator: "'", decimalSeparator: "." });
  var GANZ = NumberFormat.getIntegerInstance({ groupingEnabled: true, groupingSeparator: "'" });
  var MENGE = NumberFormat.getFloatInstance({ maxFractionDigits: 3, groupingEnabled: true, groupingSeparator: "'", decimalSeparator: "." });

  function zahl(v) { var n = parseFloat(v); return isNaN(n) ? 0 : n; }
  // Datum kommt als JJJJ-MM-TT (Gateway-Konvertierung) oder JJJJMMTT; Initialdatum leer
  function datum(v) {
    var d = String(v || "").replace(/-/g, "");
    return d.length === 8 && d !== "00000000" ? d.substr(6, 2) + "." + d.substr(4, 2) + "." + d.substr(0, 4) : "";
  }
  function jjjjmmtt(d) { return d.getFullYear() + String(d.getMonth() + 1).padStart(2, "0") + String(d.getDate()).padStart(2, "0"); }
  function liste(s) { return (s || "").split(/[,;\s]+/).map(function (x) { return x.trim(); }).filter(Boolean); }

  return Controller.extend("trafag.zc23.controller.Main", {

    onInit: function () {
      var heute = new Date();
      var q = Math.floor(heute.getMonth() / 3);
      this.getView().setModel(new JSONModel({
        logo: sap.ui.require.toUrl("trafag/zc23/img/logo.png"),
        sel: { werk: "1100", von: new Date(heute.getFullYear(), q * 3, 1), bis: new Date(heute.getFullYear(), q * 3 + 3, 0),
               kunden: "", materialien: "", auftraege: "", korr: "V", mata: true, kalk: "F", mfrem: false },
        selOffen: true, selKurz: "", laeuft: false, stand: "Noch nicht ausgeführt", standTyp: "None",
        suche: "", nurCz: false, anzahlText: "", leerText: "Selektion wählen und „Ausführen“ drücken.", kpi: { positionen: 0 }
      }), "view");
      this.getView().setModel(new JSONModel({ alle: [], gefiltert: [] }), "daten");
      this._felder = null;
    },

    onAusfuehren: function () {
      var oV = this.getView().getModel("view");
      var s = oV.getProperty("/sel");
      if (!s.werk || !s.von || !s.bis) {
        MessageBox.warning("Bitte Werk und Liefertermin (von bis) angeben.");
        return;
      }
      if ((s.bis - s.von) / 86400000 > 366) {
        MessageBox.warning("Der Zeitraum darf höchstens ein Jahr umfassen.");
        return;
      }
      var a = [
        new Filter("Werk", FilterOperator.EQ, s.werk),
        new Filter("Datvon", FilterOperator.EQ, jjjjmmtt(s.von)),
        new Filter("Datbis", FilterOperator.EQ, jjjjmmtt(s.bis)),
        new Filter("Mata", FilterOperator.EQ, s.mata ? "X" : ""),
        new Filter("Mjung", FilterOperator.EQ, s.kalk === "J" ? "X" : ""),
        new Filter("Mfrem", FilterOperator.EQ, s.mfrem ? "X" : ""),
        new Filter("Korr", FilterOperator.EQ, s.korr)
      ];
      [["Kunnr", s.kunden], ["Matnr", s.materialien], ["Vbeln", s.auftraege]].forEach(function (p) {
        var werte = liste(p[1]);
        if (werte.length) {
          a.push(new Filter({ filters: werte.map(function (w) { return new Filter(p[0], FilterOperator.EQ, w); }), and: false }));
        }
      });
      oV.setProperty("/selKurz", "Werk " + s.werk + " · " + datum(jjjjmmtt(s.von)) + " – " + datum(jjjjmmtt(s.bis)) +
        (s.mata ? " · Materialanteil (" + (s.kalk === "J" ? "jüngste" : "freigegebene") + " Kalkulation)" : " · ohne Materialanteil"));
      oV.setProperty("/laeuft", true);
      oV.setProperty("/stand", "ZC23 läuft …");
      oV.setProperty("/standTyp", "Information");
      var t0 = Date.now();
      this.getOwnerComponent().getModel().read("/Zc23Set", {
        filters: [new Filter({ filters: a, and: true })],
        success: function (oData) {
          // Edm.Decimal kommt als Zeichenkette; als Zahl ablegen, damit das Sortieren numerisch ist
          var rows = (oData.results || []).map(function (o) {
            delete o.__metadata;
            Object.keys(o).forEach(function (k) { if (/^-?\d+\.\d+$/.test(o[k])) { o[k] = parseFloat(o[k]); } });
            return o;
          });
          this.getView().getModel("daten").setProperty("/alle", rows);
          this._spaltenAufbauen(rows);
          this.onFiltern();
          oV.setProperty("/laeuft", false);
          oV.setProperty("/selOffen", false);
          oV.setProperty("/stand", "Stand " + new Date().toLocaleTimeString("de-CH") + " · " + ((Date.now() - t0) / 1000).toFixed(1) + " s");
          oV.setProperty("/standTyp", "Success");
          oV.setProperty("/leerText", "Keine offenen Positionen für diese Selektion.");
        }.bind(this),
        error: function (oErr) {
          var sText = oErr && oErr.message;
          try { sText = JSON.parse(oErr.responseText).error.message.value; } catch (e) { /* kein JSON */ }
          oV.setProperty("/laeuft", false);
          oV.setProperty("/stand", "Fehler");
          oV.setProperty("/standTyp", "Error");
          MessageBox.error("ZC23 konnte nicht laufen: " + sText);
        }
      });
    },

    onSuchen: function () { this.onFiltern(); },

    onFiltern: function () {
      var oV = this.getView().getModel("view");
      var oD = this.getView().getModel("daten");
      var q = (oV.getProperty("/suche") || "").toLowerCase();
      var nurCz = oV.getProperty("/nurCz");
      var rows = oD.getProperty("/alle").filter(function (r) {
        if (nurCz && !zahl(r.ArbeitCz)) { return false; }
        if (!q) { return true; }
        return [r.Vbeln, r.Kunnr, r.Name1, r.Matnr, r.Maktx, r.Aufnr, r.DispoDes, r.Bismt].join(" ").toLowerCase().indexOf(q) >= 0;
      });
      oD.setProperty("/gefiltert", rows);
      this._kennzahlen(rows);
      oV.setProperty("/anzahlText", GANZ.format(rows.length) + " von " + GANZ.format(oD.getProperty("/alle").length) + " Zeilen");
    },

    _kennzahlen: function (rows) {
      var sum = function (f) { return rows.reduce(function (a, r) { return a + zahl(r[f]); }, 0); };
      var std = sum("Standardwert"), mat = sum("Matanteil"), cz = sum("ArbeitCz");
      var uniq = function (f) { return new Set(rows.map(function (r) { return r[f]; })).size; };
      var waers = rows.length ? (rows[0].Waers || "") : "";
      this.getView().getModel("view").setProperty("/kpi", {
        positionen: rows.length,
        positionenText: GANZ.format(rows.length),
        auftraegeText: GANZ.format(uniq("Vbeln")) + " Aufträge",
        offenText: BETRAG.format(sum("Netwr3n")),
        stdText: BETRAG.format(std),
        matText: BETRAG.format(mat),
        matQuote: std ? (mat / std * 100).toFixed(1) + " % vom Standardwert" : "",
        czText: BETRAG.format(cz),
        czQuote: std ? (cz / std * 100).toFixed(1) + " % vom Standardwert" : "",
        kundenText: GANZ.format(uniq("Kunnr")) + " Kunden",
        materialText: GANZ.format(uniq("Matnr")) + " Materialien",
        waehrung: waers ? "Werte in " + waers : ""
      });
    },

    _spaltenAufbauen: function (rows) {
      if (this._felder || !rows.length) { return; }
      var oT = this.byId("tabelle");
      var bekannt = {};
      var cfg = SPALTEN.map(function (c) { bekannt[c.f] = 1; return Object.assign({ v: true }, c); });
      Object.keys(rows[0]).forEach(function (f) {
        if (!bekannt[f] && !AUSBLENDEN[f]) { cfg.push({ f: f, t: f, v: false, w: "8rem" }); }
      });
      cfg.forEach(function (c) {
        var oText;
        if (c.typ === "betrag" || c.typ === "menge") {
          var fmt = c.typ === "betrag" ? BETRAG : MENGE;
          oText = new Text({ text: { path: "daten>" + c.f, formatter: function (v) { return v === undefined || v === null ? "" : fmt.format(zahl(v)); } },
                             textAlign: "End", wrapping: false }).addStyleClass("zc23Zahl");
        } else if (c.typ === "datum") {
          oText = new Text({ text: { path: "daten>" + c.f, formatter: datum }, wrapping: false });
        } else {
          oText = new Text({ text: "{daten>" + c.f + "}", wrapping: false });
        }
        oT.addColumn(new Column({
          label: new Label({ text: c.t }), template: oText, width: c.w, visible: c.v,
          hAlign: c.typ === "betrag" || c.typ === "menge" ? "End" : "Begin",
          sortProperty: c.f, filterProperty: c.f
        }));
      });
      this._felder = cfg;
    },

    onCsv: function () {
      var rows = this.getView().getModel("daten").getProperty("/gefiltert");
      var cols = (this._felder || []).filter(function (c) { return c.v; });
      var esc = function (v) { v = v === undefined || v === null ? "" : String(v); return /[";\n]/.test(v) ? "\"" + v.replace(/"/g, "\"\"") + "\"" : v; };
      var zeilen = [cols.map(function (c) { return esc(c.t); }).join(";")];
      rows.forEach(function (r) {
        zeilen.push(cols.map(function (c) {
          var v = r[c.f];
          if (c.typ === "datum") { v = datum(v); }
          if (c.typ === "betrag" || c.typ === "menge") { v = String(zahl(v)).replace(".", ","); }
          return esc(v);
        }).join(";"));
      });
      var blob = new Blob(["﻿" + zeilen.join("\r\n")], { type: "text/csv;charset=utf-8" });
      var a = document.createElement("a");
      a.href = URL.createObjectURL(blob);
      a.download = "ZC23_" + jjjjmmtt(new Date()) + ".csv";
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
    }
  });
});
