sap.ui.define([
  "sap/ui/core/mvc/Controller",
  "sap/ui/model/json/JSONModel",
  "sap/m/Column",
  "sap/m/ColumnListItem",
  "sap/m/Text"
], function (Controller, JSONModel, Column, ColumnListItem, Text) {
  "use strict";

  // Nur lesen, immer begrenzt ($top), nie das ganze Set (Learning 2026-10-01: ungefiltert 500er)
  var SET = "/ProductDivisionRefSet";
  var TOP = 50;

  return Controller.extend("trafag.erste.controller.Main", {
    onInit: function () {
      this.getView().setModel(new JSONModel({ status: "Lade Daten ...", statusTyp: "Information", zeilen: [] }), "view");
      this.onLaden();
    },

    onLaden: function () {
      var oView = this.getView();
      var oState = oView.getModel("view");
      var oOData = this.getOwnerComponent().getModel();
      oState.setProperty("/status", "Lade Daten ...");
      oState.setProperty("/statusTyp", "Information");
      oOData.read(SET, {
        urlParameters: { "$top": String(TOP) },
        success: function (oData) {
          // ProductDivisionRefSet ignoriert $top (gemessen 2026-10-15: 48'838 Zeilen statt 50); deshalb hier kuerzen
          var iGeliefert = (oData.results || []).length;
          var aZeilen = (oData.results || []).slice(0, TOP).map(function (o) {
            var r = {};
            Object.keys(o).forEach(function (k) { if (k !== "__metadata") { r[k] = o[k]; } });
            return r;
          });
          this._spaltenAufbauen(aZeilen);
          var sText = aZeilen.length + " Zeilen angezeigt aus ZPOWERBI_EINKAUF_SRV" + SET + ".";
          if (iGeliefert > TOP) {
            sText += " Achtung: Der Dienst ignoriert $top=" + TOP + " und lieferte " + iGeliefert + " Zeilen.";
          }
          oState.setProperty("/status", sText);
          oState.setProperty("/statusTyp", iGeliefert > TOP ? "Warning" : "Success");
        }.bind(this),
        error: function (oErr) {
          // Fehlertext aus der Antwort melden, nicht nur den HTTP-Code (Learning 2026-10-14)
          var sText = oErr && oErr.message;
          try { sText = JSON.parse(oErr.responseText).error.message.value; } catch (e) { /* kein JSON */ }
          oState.setProperty("/status", "Fehler " + (oErr && oErr.statusCode) + ": " + sText);
          oState.setProperty("/statusTyp", "Error");
        }
      });
    },

    _spaltenAufbauen: function (aZeilen) {
      var oTabelle = this.byId("tabelle");
      var aFelder = aZeilen.length ? Object.keys(aZeilen[0]) : [];
      oTabelle.removeAllColumns();
      oTabelle.unbindItems();
      aFelder.forEach(function (f) {
        oTabelle.addColumn(new Column({ header: new Text({ text: f }) }));
      });
      oTabelle.setModel(new JSONModel({ zeilen: aZeilen }), "daten");
      oTabelle.bindItems({
        path: "daten>/zeilen",
        template: new ColumnListItem({
          cells: aFelder.map(function (f) { return new Text({ text: "{daten>" + f + "}" }); })
        })
      });
    }
  });
});
