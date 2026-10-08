# Unterrouter Einkauf

Zurueck: `router.md`. Stand: 2026-09-28.

Spend, Bestellungen, Kontrakte, Supply Chain, Logistik, Produktgruppen, ABC/XYZ.

## Zuerst laden

| Bedarf | Datei |
| --- | --- |
| Konsistenzreview und technische Reparaturen 29.09. (lokal, nicht deployed); Zuordnung bestehender Fachfragen | `docs/HR_EINKAUF_REVIEW_2026-09-29.md` |
| Kurzstand Einkauf | `docs/rag/PURCHASING.md` |
| Laufende Hauptdoku, Formeln, PBIX-Bezug, Cache und Refresh | `docs/PURCHASING_DASHBOARD_2026-06-05.md` |
| **Was ist umgesetzt, was offen, was zurueckgestellt** | `docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md` |

## Nach Thema

| Thema | Datei |
| --- | --- |
| **Offener Mengenkontraktwert wie ME3L (inkl. abgelaufen), Abgrenzung zu „Abrufbestellungen zu Kontrakten“, LZ-/Sortiments-Einstiege im Spend-Aufriss** (lokal 2026-10-08, nicht deployed) | `docs/PURCHASING_DASHBOARD_2026-06-05.md` Nachtrag 2026-10-08, Kurzfassung `docs/rag/PURCHASING.md` |
| **Lagerwert der Einkaufsteile (Wunsch Armin) und Wochenverlauf, MB5L, MBEW/MBEWH, Disponenten 001-005** | `docs/EINKAUF_LAGERWERT_2026-08-18.md` — Kachel produktiv und gespeichert (Abschnitte 10 und 12), **Verlauf je Woche produktiv seit 2026-09-28**, Einkauf-Lauf 10.09.-28.09. ausgefallen, abgesichert (Abschnitt 13.6) (Abschnitt 13), MB5L-Abgleich offen |
| Materialtext im Spend-Drilldown, MAKT/MAKTX, Sprachfilter | `docs/PURCHASING_DASHBOARD_2026-06-05.md` Nachtrag 2026-08-18 |
| Welche Indikatoren echt rechnen, welche leer sind | `docs/EINKAUF_INDIKATOREN_PRUEFUNG_2026-08-07.md` |
| Produktgruppen, ZC23/Disponent, Mehrfachverwendung, ABC/XYZ-Nutzen | `docs/PURCHASING_PRODUKTGRUPPEN_ABCXYZ_2026-08-06.md` |
| Produktgruppen direkt aus SAP OData, ZDISPO; Verwendung je Komponente (ZLO03) seit 2026-10-08 | `docs/PURCHASING_PRODUCT_GROUP_SAP_DIRECT_2026-08-11.md` |
| Supply Chain: Fehlteile, Deckung, Materialabhaengigkeit, Dispositionspruefung, Lieferperformance | `docs/EINKAUF_LOGISTIK_SUPPLY_CHAIN_REITER_2026-08-06.md` |
| **Einkauf Interaktiv**: neun animierte Ansichten auf den Bestellungen (Lieferanten-Galaxie, Rennen, Einkaufsfluss, Sonnenstrahl, Simulator, Wiederbestell-Rhythmus, Kalender, Warengruppen-Netzwerk, 3D), gleiche Bausteine wie Verkauf Interaktiv | `docs/VERKAUF_2026-10-02.md` Abschnitt „Interaktiv“ |
| **Logistik live**: Kommissionierung und Produktion aus SAP, Umschalter 3D-Lagerplatzansicht, Abruf nur bei offener Seite, Schutz fuer P76, Sets `LogTaSet`/`LogLiefSet`/`LogRueckSet` | `docs/LOGISTIK_LIVE_2026-10-01.md` |
| **Verwendung & Risiko**: mehrstufige Verwendung der LZ-Code-Komponenten, vererbtes Lieferantenrisiko; warum kein Umsatz je Komponente | `docs/LOGISTIK_STUECKLISTE_VERWENDUNG_RISIKO_2026-10-01.md` |
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
