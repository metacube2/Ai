# Unterrouter Einkauf

Zurueck: `router.md`. Stand: 2026-08-17.

Spend, Bestellungen, Kontrakte, Supply Chain, Logistik, Produktgruppen, ABC/XYZ.

## Zuerst laden

| Bedarf | Datei |
| --- | --- |
| Kurzstand Einkauf | `docs/rag/PURCHASING.md` |
| Laufende Hauptdoku, Formeln, PBIX-Bezug, Cache und Refresh | `docs/PURCHASING_DASHBOARD_2026-06-05.md` |
| **Was ist umgesetzt, was offen, was zurueckgestellt** | `docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md` |

## Nach Thema

| Thema | Datei |
| --- | --- |
| Welche Indikatoren echt rechnen, welche leer sind | `docs/EINKAUF_INDIKATOREN_PRUEFUNG_2026-08-07.md` |
| Produktgruppen, ZC23/Disponent, Mehrfachverwendung, ABC/XYZ-Nutzen | `docs/PURCHASING_PRODUKTGRUPPEN_ABCXYZ_2026-08-06.md` |
| Produktgruppen direkt aus SAP OData, ZDISPO | `docs/PURCHASING_PRODUCT_GROUP_SAP_DIRECT_2026-08-11.md` |
| Supply Chain: Fehlteile, Deckung, Materialabhaengigkeit, Dispositionspruefung, Lieferperformance | `docs/EINKAUF_LOGISTIK_SUPPLY_CHAIN_REITER_2026-08-06.md` |
| Logistik-Stuecklisten-Dashboard, Top-Down und Bottom-Up | `docs/LOGISTIK_STUECKLISTEN_DASHBOARD_2026-08-01.md` |
| Oberflaechensprachen und Projektsuite | `docs/EINKAUF_LOKALISIERUNG_PROJEKTSUITE_2026-08-01.md` |

## Fallen in diesem Ast

Die vier SAP-Semantikfallen stehen ausfuehrlich in
`docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md` Abschnitt 2. Kurz:

- `EKKO.AEDAT` ist das **Anlagedatum**, kein Aenderungsdatum. Ein Delta darueber verpasst
  jeden Wareneingang auf aelteren Belegen.
- `EKPO.NETWR` steht in **Belegwaehrung**, nicht in CHF.
- `EKET.EINDT` ist das **geplante** Lieferdatum, nicht das Ist. Ein Wareneingangsbezug
  braucht `EKBE`/`MSEG`.
- Eine Zeitraum-Obergrenze auf heute schneidet **allen zukuenftigen Zulauf** ab und legt
  die Risiko-Buckets still.

Dazu: **Nachpflege in SAP wirkt erst nach einem Full Load** — seit dem Delta-Fix
klassifiziert das Delta den ganzen Cache, aber die Laufzeit ist ungemessen.

## Querverweise in Nachbaraeste

- ZLO03-Webservice und ABAP: `docs/router/sap.md`
- Deploy und Full Load: `docs/router/plattform.md`
