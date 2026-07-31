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
| 7 | UK | Cornell Williams | versandfertig, reine Bestätigung — **2026-07-31 korrigiert**, siehe unten |
| — | CH / AT | entfällt | kein Standortversand, SAP-intern |

---

## Prüfung aller Entwürfe gegen die Rohdaten, 2026-07-31

Jede Zahl in allen sieben Entwürfen gegen `Finance_Dashboard_Audit_All_2026-07-29.csv`
nachgemessen, nachdem eine fremde Auswertung für TRUK `0` Lieferanten zeigte. Ergebnis: **sechs
Entwürfe stimmen, einer enthielt eine falsche Aussage.**

| Behauptung im Entwurf | Gemessen | Urteil |
| --- | --- | --- |
| FR 374 von 433 Artikeln, 5 % der 2'577 Zeilen | 374/433, 134 Zeilen = 5.2 % | stimmt |
| IT 939 von 3'280 Artikeln, 71 % der 19'534 Zeilen | 939/3'280, 13'925 = 71.3 % | stimmt |
| IN 1'271 von 1'437 Artikeln, 12 %, 677 als TR AG erkannt | 1'271/1'437, 809 = 11.6 %, 677 intern | stimmt |
| IN „rund 6'100 Zeilen würden zuordenbar" | 6'181 Zeilen ohne Lieferant | stimmt (rund) |
| US 518 von 521 Artikeln, 6 von 1'504 Zeilen | 518/521, 6 Zeilen, 1'498 ohne | stimmt |
| DE Lieferant 0, Kundenname 0, Kundenland 0, Kundennummer 7'171 | exakt so | stimmt |
| DE 2'903 von 7'171 Beschreibungen mit RTF-Müll | 2'903 | stimmt |
| ES 231 Zeilen ohne jedes Datum | 231 | stimmt |
| ES „01.01. bis 27.05.2026 nie geliefert" | 2026: Mai 35 (ab 28.05.), Juni 542, Juli 381, **Jan–Apr 0** | stimmt |
| UK Lieferant auf allen 2'955 Zeilen | 2'955 mit Nummer **und** Name | stimmt |
| UK Kostendeckung 93 % | 2'762 von 2'955 = 93.5 % | stimmt |
| **UK „2025 fehlt, Daten beginnen im Januar 2026"** | **2025: 1'867 Zeilen, 2026: 1'082** | **FALSCH, ersetzt** |

Nebenbefund zur Trafag-Erkennung, geprüft mit demselben Regex wie im Code:

| TSC | mit Lieferant | intern erkannt | extern | ohne Lieferant |
| --- | --- | --- | --- | --- |
| TRFR | 134 | 83 | 51 | 2'443 |
| TRIN | 809 | 677 | 132 | 6'181 |
| TRIT | 13'925 | 6'848 | 7'077 | 5'609 |
| TRUK | 2'955 | 2'803 | 152 | 0 |
| TRUS | 6 | 2 | 4 | 1'498 |

Häufigste interne Werte: `TRUK / TR08 / Trafag AG / CH` (2'609), `TRIN / V0078 / Trafag AG / CH`
(677), `TRUK / TR09 / Trafag Controls India Pvt Limited / IN` (101), `TRFR / S_CH01_0070540 /
Trafag Italia S.r.l. / IT` (43). Der Filter greift also überall dort, wo überhaupt ein Lieferant
steht — er ist nicht der Engpass, das fehlende Feld ist es.

---

## Grafische Fassung und Erzeugung

Die Mails oben sind die **Textfassung**. Zusätzlich gibt es eine grafisch aufbereitete Fassung,
damit der Empfänger die Lücke sieht statt sie aus Zahlen zu rekonstruieren. Erzeugt von
`docs/mails/Build-StandortMails.ps1`:

```text
! powershell -NoProfile -ExecutionPolicy Bypass -File .\docs\mails\Build-StandortMails.ps1 -Mode Preview
! powershell -NoProfile -ExecutionPolicy Bypass -File .\docs\mails\Build-StandortMails.ps1 -Mode Draft
```

`Preview` schreibt `.tmp_standort_mails/Vorschau_Standortmails.html` (alle sieben Mails
untereinander, im Browser prüfbar, ändert nichts). `Draft` legt sie als Entwürfe in Outlook an —
schreibt ins Postfach, **sendet nichts**, Entwürfe sind einzeln löschbar.

**Warum keine `.msg`-Dateien:** `MailItem.SaveAs` ist auf diesem Arbeitsplatz gesperrt. Jedes
Format (`.msg`, `.oft`, `.txt`) und jeder Zielordner liefern `E_ABORT` (`0x80004004`), verifiziert
2026-07-31 — eine Endpoint-Security-/DLP-Regel, die Outlook das Schreiben von Nachrichtendateien
auf Platte verbietet. `MailItem.Save()` in den Entwürfe-Ordner funktioniert dagegen. Das ist keine
Skriptschwäche und lässt sich ohne Änderung an der Sicherheitsrichtlinie nicht umgehen.
`-Mode Docx` (Word-COM auf die Vorschau) **hängt** auf diesem Rechner ebenfalls — wer ein Word-
Dokument braucht, öffnet die Vorschau-HTML von Hand in Word und speichert als `.docx`.

**Zweiter Befund, wichtig für jede Prüfung per Skript: der Outlook Object Model Guard ist aktiv.**
Nach dem Anlegen lassen sich die Entwürfe **nicht programmatisch verifizieren** — `MailItem.To`
und `MailItem.HTMLBody` kommen beim *Lesen* leer zurück (Länge 0), und `Folder.GetTable()` bricht
beim `Columns.Add` mit demselben `E_ABORT` ab. Das ist dieselbe Schutzschicht wie bei `SaveAs`:
Adressen und Nachrichtentexte dürfen nicht ausgelesen werden. **Konsequenz:** dass der Text
angekommen ist, lässt sich nur indirekt über `MailItem.Size` belegen (6.9–9.8 KB je Entwurf,
skaliert exakt mit der Textlänge — ein leerer Entwurf wäre rund 1 KB). Der **Empfänger muss in
Outlook mit dem Auge geprüft werden**, dafür gibt es keinen Skriptweg auf diesem Arbeitsplatz.

**Falle beim Wiederholen:** Ein Lauf, der mitten in der Schleife scheitert, hinterlässt für die
bereits erzeugten Mails **Waisen-Entwürfe** — Outlook speichert ein freigegebenes, nicht
gespeichertes `MailItem` selbst in *Entwürfe*. Nach einem Fehlversuch also erst die Entwürfe
aufräumen, sonst liegen Dubletten im Postfach. Am 2026-07-31 waren das ein doppelter
Frankreich-Entwurf aus dem gescheiterten `SaveAs`-Lauf und ein Testentwurf; beide entfernt.

**Welche Grafiken drin sind:**

| Element | Wo | Was es zeigt |
| --- | --- | --- |
| Zwei-Segment-Balken Artikelstamm | FR, IT, IN, US | gepflegte gegen fehlende Artikelnummern, exakte Stückzahlen |
| Zwei-Segment-Balken Rechnungszeilen | FR, IT, IN, US | Anteil zuordenbarer Zeilen, **nur Prozente** |
| Feld-Schema Artikelstamm | FR, IT, IN, US | Reiter *Purchasing Data* mit leerem Feld *Preferred Vendor* und Verweis auf `OITM.CardCode` |
| Statustabelle Exportfelder | DE | fünf Feldgruppen mit Farbpunkt: komplett, leer, unbrauchbar |
| Vorher/Nachher-Kasten | DE | „what we receive" gegen „what we need" am RTF-Beispiel |
| Monatsstreifen 2026 | ES | Jan–Apr rot, Mai teilweise, Jun–Jul vorhanden, Aug–Dez offen |
| Standortvergleich | UK | UK 100 % gegen IT 71 %, IN 12 %, Rest 0–5 % |
| Jahresstreifen | UK | 2025 nicht vorhanden, 2026 komplett |

**Zwei bewusste Festlegungen in der Grafik**, beide relevant, falls jemand die Zahlen nachrechnet:

- **Artikelbalken zeigen exakte Stückzahlen, Zeilenbalken nur Prozente.** Die Zeilenzahlen je
  Kategorie wären aus gerundeten Prozentwerten abgeleitet (12 % von 6'990) und würden eine
  Genauigkeit vortäuschen, die die Messung nicht hat. Die Gesamtzahl steht jeweils als Fussnote.
- **Keine Bilder, nur Tabellen mit `bgcolor`.** Outlook blockiert externe Bilder beim Empfänger
  standardmässig, und eingebettete Bilder erscheinen zusätzlich als Dateianhang. Tabellenzellen
  rendern in jeder Outlook-Version. Deshalb auch kein `flex`, kein `grid`, keine `border-radius`
  und Balkenbreiten in Pixel statt Prozent.

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
**Subject:** BI Dashboard — UK data is complete, nothing needed from your side

**Korrigiert 2026-07-31.** Die erste Fassung fragte nach einem 2025-Export, weil die Quelltabelle
„2025 fehlt komplett" behauptete. **Das war falsch** — im Audit-CSV liegen 1'867 UK-Zeilen für
2025, der Backfill ist längst gelaufen. Der Entwurf mit dieser Frage wurde gelöscht und ersetzt;
Details der Fehlerkette in `FINANCE_FELDLUECKEN_STANDORTE_2026-07-30.md`, Abschnitt „UK ist
erledigt". Damit ist für TRUK **nichts** offen und die Mail eine reine Bestätigung — ohne den
Vorspann „was wir nicht brauchen", weil es nichts zu tun gibt.

> Dear Cornell,
>
> short and positive one. We have completed a field-by-field check of the sales data that feeds
> the group BI Dashboard, measured on the consolidated extract of 29 July 2026.
>
> **For the UK there is nothing to do.** Supplier information is complete on all 2,955 invoice
> lines — you are the only site where that field is fully maintained — and cost coverage is at
> 93%, which is normal given freight and service lines carry no item cost. Thank you.
>
> Both 2025 (1,867 lines) and 2026 to date (1,082 lines) are in, so the prior-year comparison
> works for the UK — that is not the case for every site. Nothing needed from your side; this is
> just so you know where the UK stands when group figures come up.
>
> Best regards
> Ingo

---

## Nach dem Versand

Hier festhalten, was wann rausgegangen ist und was zurückkommt — sonst ist beim nächsten
Durchgang nicht unterscheidbar, ob ein Standort nicht geantwortet oder nie eine Mail bekommen hat.

Alle sieben liegen seit 2026-07-31, 09:20 als **Entwürfe** in Outlook (`\\Ingo.Kohler@trafag.com\Entwürfe`),
Grafik enthalten, nichts gesendet.

| Standort | Entwurf | Versandt am | Antwort | Ergebnis |
| --- | --- | --- | --- | --- |
| TRFR | liegt, **An leer** | — | — | Adresse fehlt |
| TRIT | liegt | — | — | — |
| TRIN | liegt | — | — | — |
| TRUS | liegt, **An leer** | — | — | Adresse fehlt |
| TRDE | liegt | — | — | — |
| TRES | liegt | — | — | — |
| TRUK | liegt | — | — | — |
