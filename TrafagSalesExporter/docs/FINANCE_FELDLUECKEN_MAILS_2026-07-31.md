# Standort-Mails Feldlücken — versandfertig je Standort

Stand: 2026-07-31

Herkunft: aufgeteilt aus dem Sammeltext in `docs/FINANCE_FELDLUECKEN_STANDORTE_2026-07-30.md`
Abschnitt 6. Empfänger aus Abschnitt 5 desselben Dokuments bzw. aus `docs/ANSPRECHPARTNER.md`.
Alle Zahlen sind unverändert übernommen (Messung auf dem konsolidierten Auszug vom 29.07.2026,
95'168 Rechnungszeilen) — beim Anpassen von Formulierungen die Zahlen nicht mitverändern.

**Warum einzeln und nicht als Sammelmail:** Paola Castagna (TR IT) trägt parallel das
Bewertungsthema mit Zusage Ende August; eine Sammelmail hängt jedem Standort die Punkte der
anderen an und macht aus einer Bitte einen Rundbrief, den niemand als an sich gerichtet liest.
Der Vorspann („was wir *nicht* brauchen") steht deshalb in jeder Mail einzeln — er verhindert,
dass jemand Zeit in Produktsparten, Kurse oder Frachtkosten steckt.

| Reihenfolge | Mail | Empfänger | Status |
| --- | --- | --- | --- |
| 1 | Frankreich | *offen* | **blockiert**, nur Adresse fehlt — kleinster Aufwand aller Standorte |
| 2 | Italien | Paola Castagna | versandfertig, siehe Hinweis zum Timing |
| 3 | Indien | RanVijay Kumar | versandfertig; `tasc.co.in` im Cc ist eine offene Entscheidung |
| 4 | USA | *offen* | **blockiert**, nur Adresse fehlt |
| 5 | Deutschland | Rohail Munir | versandfertig |
| 6 | Spanien | Santi Gomez | versandfertig |
| 7 | UK | Cornell Williams | versandfertig, reine Rückfrage |
| — | CH / AT | entfällt | kein Standortversand, SAP-intern |

---

## 1. Frankreich (TRFR) — Adresse fehlt

**To:** *offen — Empfänger noch zu beschaffen*
**Subject:** BI Dashboard — supplier missing on the item master (Trafag France)

> Dear colleagues,
>
> we have completed a field-by-field check of the sales data that feeds the group BI Dashboard,
> measured on the consolidated extract of 29 July 2026. For Trafag France there is exactly one
> thing missing, and it is the smallest amount of work of all our sites.
>
> **The point: 374 of your 433 item codes** have no *Preferred Vendor* maintained on the item
> master (`OITM.CardCode`, Purchasing Data tab). Supplier information is therefore present on
> only 5% of your 2,577 invoice lines. We read the supplier from exactly that field, so an item
> without it produces invoice lines we cannot classify as intercompany versus third-party
> purchase — which is what the group margin depends on.
>
> The lines that do carry a supplier are recognised correctly (Trafag AG and Trafag Italia), so
> nothing beyond the master data is needed. Could you have those item codes reviewed? We are
> happy to send you the list of affected items.
>
> Three things we explicitly do **not** need, so nobody spends time on them:
>
> - **Product division / product family.** Derived centrally from the Trafag AG material master;
>   local ERP product divisions are deliberately not used. The only thing that matters is that
>   the **material number** on the invoice line matches the Trafag AG master.
> - **Exchange rates on the document.** Currency conversion is done centrally.
> - **Item costs on freight, packaging, certificate and documentation lines.** We checked these
>   and they are correctly zero.
>
> Happy to do a short call if that is easier than email.
>
> Best regards
> Ingo

---

## 2. Italien (TRIT)

**To:** `Paola.Castagna@trafag.com`
**Subject:** BI Dashboard — supplier missing on the item master (Trafag Italia)

Timing: B1-Upgrade Go-Live 03.08.2026, danach zwei Wochen Ferien. Der Satz zur Abgrenzung vom
Bewertungsthema ist bewusst drin — Paola bekommt zwei Anliegen von derselben Person, und ohne
diesen Satz liest sich die Mail wie eine Erinnerung an die Zusage von Ende August.

> Dear Paola,
>
> a separate topic from the inventory valuation discussion — this one is master data only, it
> has no bearing on the moving-average question and there is no deadline attached to it. Given
> the B1 upgrade on 3 August, please look at it whenever it suits you afterwards.
>
> We have completed a field-by-field check of the sales data that feeds the group BI Dashboard,
> measured on the consolidated extract of 29 July 2026. You are the best-performing site on
> supplier data, thank you: 71% of your 19,534 invoice lines carry the supplier.
>
> **The remaining gap: 939 of your 3,280 item codes** have no *Preferred Vendor* maintained on
> the item master (`OITM.CardCode`, Purchasing Data tab). We read the supplier from exactly that
> field, so an item without it produces invoice lines we cannot classify as intercompany versus
> third-party purchase — which is what the group margin depends on.
>
> Could you have those item codes reviewed? We can send you the list.
>
> Three things we explicitly do **not** need, so nobody spends time on them:
>
> - **Product division / product family.** Derived centrally from the Trafag AG material master;
>   local ERP product divisions are deliberately not used. The only thing that matters is that
>   the **material number** on the invoice line matches the Trafag AG master.
> - **Exchange rates on the document.** Currency conversion is done centrally.
> - **Item costs on freight, packaging, certificate and documentation lines.** We checked these
>   and they are correctly zero.
>
> Best regards
> Ingo

---

## 3. Indien (TRIN)

**To:** `RanVijay.Kumar@trafag.com`
**Cc:** *`agupta@tasc.co.in`, `chandra.s@tasc.co.in` — nur nach Klärung, siehe unten*
**Subject:** BI Dashboard — supplier missing on the item master (Trafag India)

**Vor dem Versand entscheiden:** `tasc.co.in` ist eine Fremddomain (externer Dienstleister). Der
Mailtext selbst ist unkritisch — Feldnamen und Zeilenzahlen. Kritisch ist erst das Angebot, die
Artikelliste zu senden: **1'271 Artikelnummern des TRIN-Stamms**. Sicherer Weg: nur an RanVijay,
und die Weiterverteilung intern entscheiden lassen. Der Textbaustein unten ist so formuliert,
dass die Liste ausdrücklich an RanVijay geht, nicht in einen Cc-Kreis.

> Dear RanVijay,
>
> we have completed a field-by-field check of the sales data that feeds the group BI Dashboard,
> measured on the consolidated extract of 29 July 2026. For Trafag India there is one point.
>
> Supplier information is present on 12% of your 6,990 invoice lines. The good news is that the
> mechanism works: of the lines that do carry a supplier, 677 are correctly identified as
> Trafag AG deliveries. It is simply not maintained on most items.
>
> **1,271 of your 1,437 item codes** have no *Preferred Vendor* maintained on the item master
> (`OITM.CardCode`, Purchasing Data tab). Filling it would move roughly 6,100 invoice lines from
> "supplier unknown" into a proper classification — which is what the group margin depends on.
>
> If it helps, I can send you the list of affected item codes directly, and you can decide who
> on your side should work through it.
>
> Three things we explicitly do **not** need, so nobody spends time on them:
>
> - **Product division / product family.** Derived centrally from the Trafag AG material master;
>   local ERP product divisions are deliberately not used. The only thing that matters is that
>   the **material number** on the invoice line matches the Trafag AG master.
> - **Exchange rates on the document.** Currency conversion is done centrally.
> - **Item costs on freight, packaging, certificate and documentation lines.** We checked these
>   and they are correctly zero.
>
> Best regards
> Ingo

---

## 4. USA (TRUS) — Adresse fehlt

**To:** *offen — Empfänger noch zu beschaffen*
**Subject:** BI Dashboard — supplier missing on the item master (Trafag USA)

> Dear colleagues,
>
> we have completed a field-by-field check of the sales data that feeds the group BI Dashboard,
> measured on the consolidated extract of 29 July 2026. For Trafag USA there is one point.
>
> Supplier information is present on 6 of your 1,504 invoice lines. **518 of your 521 item
> codes** have no *Preferred Vendor* maintained on the item master (`OITM.CardCode`, Purchasing
> Data tab). We read the supplier from exactly that field, so an item without it produces
> invoice lines we cannot classify as intercompany versus third-party purchase — which is what
> the group margin depends on.
>
> Could you have those item codes reviewed? We are happy to send you the list. As it is
> essentially the whole item master, it may be worth a short call first to agree on the most
> efficient way to fill it — for example a bulk update rather than item by item.
>
> Three things we explicitly do **not** need, so nobody spends time on them:
>
> - **Product division / product family.** Derived centrally from the Trafag AG material master;
>   local ERP product divisions are deliberately not used. The only thing that matters is that
>   the **material number** on the invoice line matches the Trafag AG master.
> - **Exchange rates on the document.** Currency conversion is done centrally.
> - **Item costs on freight, packaging, certificate and documentation lines.** We checked these
>   and they are correctly zero.
>
> Best regards
> Ingo

---

## 5. Deutschland (TRDE)

**To:** `Rohail.Munir@trafag.de`
**Subject:** BI Dashboard — three questions on the Alphaplan export (Trafag GmbH)

Falls Rohail nicht selbst der Alphaplan-/BI-Kontakt ist, ist der erste Satz die Bitte um
Weiterleitung — das ist im Text enthalten.

> Dear Rohail,
>
> we have completed a field-by-field check of the sales data that feeds the group BI Dashboard,
> measured on the consolidated extract of 29 July 2026. For Germany there are three points, and
> all three concern the Alphaplan export as it currently reaches us rather than master data
> maintenance. If someone else looks after the Alphaplan export on your side, could you please
> forward this to them?
>
> Measured on 7,171 invoice lines:
>
> 1. **No supplier information at all** — supplier number, name and country are empty on all
>    7,171 lines. Can the export be extended to include the supplier of the goods on each
>    invoice line? This is what we need to separate intercompany deliveries from third-party
>    purchases. If it is not feasible in the short term, please tell us so we can plan around it.
> 2. **No customer name and no customer country** — empty on all 7,171 lines, while the customer
>    *number* is present on all of them. German customers therefore appear in group reports as
>    bare numbers. Adding name and country to the export would fix this.
> 3. **Product descriptions contain technical formatting text** — 2,903 of 7,171 descriptions
>    (40%) begin with font-table text, for example:
>    `MS Shell Dlg, Microsoft Sans Serif, , , 9B4.4274.769.04.15.46.V3 Picostat PST4B3.44 …`
>    It looks as though a rich-text field is exported including its formatting header. For those
>    lines the product name is unusable in reports.
>
> Three things we explicitly do **not** need, so nobody spends time on them:
>
> - **Product division / product family.** Derived centrally from the Trafag AG material master;
>   local ERP product divisions are deliberately not used. The only thing that matters is that
>   the **material number** on the invoice line matches the Trafag AG master.
> - **Exchange rates on the document.** Currency conversion is done centrally.
> - **Item costs on freight, packaging, certificate and documentation lines.** We checked these
>   and they are correctly zero.
>
> Happy to set up a short call with whoever maintains the export if that is easier.
>
> Best regards
> Ingo

---

## 6. Spanien (TRES)

**To:** `Santi.Gomez@trafag.es`
**Subject:** BI Dashboard — three points on the Spanish export (Trafag Iberica)

Punkt 3 ist derselbe Range-Export, den Santi schon einmal gefahren hat — der Befehl ist in
`docs/FINANCE_BACKFILL_UK_ES_2026-07-28.md` Abschnitt „Befehl fuer Santi" dokumentiert. Der
Verweis darauf steht im Text, damit er nicht neu suchen muss.

> Dear Santi,
>
> we have completed a field-by-field check of the sales data that feeds the group BI Dashboard,
> measured on the consolidated extract of 29 July 2026. For Spain there are three points,
> measured on 5,504 invoice lines.
>
> 1. **1 January to 27 May 2026 has never reached us.** The range export we have starts on
>    28 May 2026, so the first five months of 2026 are missing from group reporting entirely.
>    Could you run and send the range export for 01.01.2026 – 27.05.2026? It is the same script
>    and the same procedure you used before — happy to resend the exact command if you no longer
>    have it to hand.
> 2. **231 lines have no date whatsoever** — neither invoice date nor posting date. Those lines
>    drop silently out of every monthly and yearly report. Could you check what kind of documents
>    these are?
> 3. **No supplier information** — empty on all 5,504 lines. Before we ask for a technical
>    change, one question: does the Sage sales/delivery data model carry a concept of "supplier"
>    on a sales document at all? This is typically a purchasing attribute rather than a sales
>    one. If it does, could it be added to the export? If it does not, please tell us, so we can
>    look at another way to identify intercompany deliveries for Spain.
>
> Point 1 is the one that matters most to us — it is a visible gap in the 2026 figures.
>
> Three things we explicitly do **not** need, so nobody spends time on them:
>
> - **Product division / product family.** Derived centrally from the Trafag AG material master;
>   local ERP product divisions are deliberately not used. The only thing that matters is that
>   the **material number** on the invoice line matches the Trafag AG master.
> - **Exchange rates on the document.** Currency conversion is done centrally.
> - **Item costs on freight, packaging, certificate and documentation lines.** We checked these
>   and they are correctly zero.
>
> Best regards
> Ingo

---

## 7. UK (TRUK)

**To:** `Cornell.Williams@trafag.com`
**Subject:** BI Dashboard — UK data is complete, one question about 2025

Reine Rückfrage, keine Bitte. Der Vorspann „was wir nicht brauchen" ist hier weggelassen — es
gibt nichts zu tun, an dem sich jemand verausgaben könnte.

> Dear Cornell,
>
> short and positive one. We have completed a field-by-field check of the sales data that feeds
> the group BI Dashboard, measured on the consolidated extract of 29 July 2026.
>
> **For the UK there is nothing to do.** Supplier information is complete on all 2,955 invoice
> lines — you are the only site where that field is fully maintained — and cost coverage is at
> 93%, which is normal given freight and service lines carry no item cost. Thank you.
>
> One open point, and only if it is needed: the UK data we hold starts in January 2026, so 2025
> is absent from group reporting. Is a 2025 export available from your side? If group reporting
> asks for the prior year, I would come back to you for it — no action needed now.
>
> Best regards
> Ingo

---

## Nach dem Versand

Hier festhalten, was wann rausgegangen ist und was zurückkommt — sonst ist beim nächsten
Durchgang nicht unterscheidbar, ob ein Standort nicht geantwortet oder nie eine Mail bekommen hat.

| Standort | Versandt am | Antwort | Ergebnis |
| --- | --- | --- | --- |
| TRFR | — | — | Adresse fehlt |
| TRIT | — | — | — |
| TRIN | — | — | — |
| TRUS | — | — | Adresse fehlt |
| TRDE | — | — | — |
| TRES | — | — | — |
| TRUK | — | — | — |
