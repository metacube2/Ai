# TR IT Bewertungsmethode: Antwort von Paola Castagna, 2026-09-04

Stand: 2026-09-04, Nachtrag vom 2026-09-09 in Abschnitt 7. Gehoert zu `ISS-007.1`.

Diese Datei haelt die Primaerquelle fest. Der fachliche Stand steht in
`docs/FINANCE_STANDARDKOSTEN.md` Abschnitt 6, der Status im Issue-Log
`docs/Issue_Log_Konsolidiert_2026-08-12.tsv`.

## 1. Worum es ging

Ingo hat Paola Castagna am 2026-07-28 gefragt, was noetig waere, um die Lagerartikel von
Trafag Italia von Chargenbewertung auf Moving Average umzustellen, und ob dafuer ein
einmaliger Cost Run noetig ist. Ausloeser war die Messung im B1-Artikelstamm: von rund
31'900 aktiven Lagerartikeln stehen etwa 31'600 auf `B` (Charge/Serie), 296 auf `A`
(Moving Average) und 6 auf `S` (Standardpreis). Andreas Stoller hatte die Cost-Run-Frage
ausdruecklich aufgeworfen.

## 2. Der Kernbefund

Paola hat die Frage mit ihren SAP-Beratern geprueft und am 2026-09-04 geantwortet:

**Die Bewertungsmethode eines bestehenden Artikels laesst sich in B1 nicht mehr aendern,
nachdem der Artikel angelegt wurde** — unabhaengig davon, ob Bestand vorhanden ist. Die
Einstellung auf Firmenebene wirkt ausschliesslich als Vorgabe fuer kuenftig neu angelegte
Artikel und hat keine Rueckwirkung auf bestehende.

Damit gibt es **weder ein Massenupdate noch einen artikelweisen Weg**. Die frueher
notierte Einschaetzung, die Umstellung sei „technisch als Massenupdate machbar", ist damit
widerlegt.

## 3. Der einzige verbleibende Weg

Eine Angleichung an die Konzernvorgabe waere nur als vollstaendiges Neucodierungsprojekt
moeglich, in fuenf Schritten:

1. Bestaende der heutigen Artikel und Chargen auf null bringen.
2. Neue Artikelnummern mit Bewertungsmethode Moving Average anlegen.
3. Vorhandene Bestaende chargenweise mit den korrekten Kosten auf die neuen Nummern
   importieren.
4. Alle offenen Belege umstellen, die auf die alten Artikelnummern verweisen, also Kunden-
   und Lieferantenauftraege, Angebote und Vergleichbares.
5. Die Lagerbewegungen auf den neuen Nummern neu starten.

**Die Cost-Run-Frage stellt sich damit nicht mehr als eigener Schritt.** Sie ist nicht
gegenstandslos, sondern in Schritt 3 aufgegangen: Die Bewertung der Bestaende passiert beim
Import auf die neuen Nummern.

## 4. Warum das ueber eine Systemaenderung hinausgeht

Der Aufwand bleibt nicht intern. Bei rund 31'600 Artikeln muessten **alle Lieferanten
kontaktiert werden**, damit sie ihre technischen Zeichnungen unter den neuen
Artikelnummern neu ausstellen; Etiketten und Beschriftungen muessten entsprechend neu
gedruckt werden. Dazu kommt der interne Aufwand fuer Code-Mapping, Uebergangssteuerung und
Belegumstellung.

## 5. Offener Punkt, den Italien nicht beurteilen kann

Paola hat keine Sicht darauf, welche Auswirkung das auf die Artikel von Trafag und
Industrial Components haette, die bei uns codiert werden und ueber Intercompany direkt in
ihr B1 fliessen. Sie schlaegt vor, diese Bewertung gemeinsam mit **Lucas Castro** und
unseren SAP-Beratern vorzunehmen, weil sie ueber das hinausgeht, was Italien allein
einschaetzen kann.

## 6. Kosten

Eine Schaetzung ihres SAP-Beraters liegt **nicht** vor. Paola will sie erst anfordern, wenn
Umfang und Prioritaet geklaert sind, weil das Projekt auf italienischer Seite eigenes
Budget und eigene Ressourcen braeuchte.

## 7. Was Paola erwartet

Eine Einschaetzung von Ingo und Andreas, **wie notwendig und wie dringend** diese
Angleichung aus Konzernsicht ist. Der fachliche Entscheid liegt damit bei Andreas Stoller.

**Beantwortet am 2026-09-09.** Andreas Stoller hat im selben Mailverlauf geantwortet: der
Umstellung auf Moving Average gibt er derzeit keine Prioritaet, das Gesamtbild wird im
naechsten Jahr erneut betrachtet. Die Kostenschaetzung ist damit nicht anzufordern, die
Intercompany-Bewertung mit Lucas Castro ist zurueckgestellt. Stattdessen fragt er nach
einer Referenzkost je Artikel aus Bestandswert geteilt durch Bestandsmenge. Stand,
Messpaket und Antwortvorschlaege: `docs/FINANCE_IT_REFERENZKOSTEN_2026-09-09.md`.

## 8. Abgrenzung, die in jede Antwort gehoert

Das Reporting haengt nicht daran. Der von Andreas am 2026-07-27 freigegebene Weg ueber
`INV1.StockPrice` arbeitet auf Belegebene und funktioniert unabhaengig von der
Bewertungsmethode; von den 2026 verkauften **Materialien** haben 2'019 von 2'082 (97.0 %)
einen `INV1.StockPrice` groesser null. Das ist eine Materialzaehlung, keine Positions- oder
Zeilenzaehlung; Messung und Einschraenkungen stehen in `docs/FINANCE_STANDARDKOSTEN.md`
Abschnitt 4. Die Bewertungsmethode veraendert die Bestandsbewertung und damit die
bilanzielle COGS, und das ist nicht dasselbe wie die Reporting-Marge im Dashboard. Ohne
diese Abgrenzung entsteht bei Italien der Eindruck, das Gruppenmargen-Projekt haenge an
ihrer Bewertungsmethode.

Ebenso bestaetigt: `OITM.AvgPrice` ist bei den 31'600 chargenbewerteten Artikeln leer, weil
B1 die Kosten je Charge fuehrt. Das ist erwartungskonformes Verhalten und kein
Datenqualitaetsproblem. Es wird sich auch nicht fuellen.

## 9. Wortlaut der Antwort

> Thank you for your patience. I've now completed the technical assessment with our SAP
> consultants, and I'd like to share the full picture with you and Andreas.
>
> **What we found**
> The valuation method of an existing item in B1 cannot be changed once the item has been
> created, regardless of stock on hand. The configuration setting at company level only acts
> as a default for newly created items going forward — it has no retroactive effect on
> existing ones.
>
> This means there is no mass update, and no item-by-item update either, that can switch the
> ~31,600 items currently on batch valuation to Moving Average. The only way to align with
> group policy would be a full item recoding project, which would involve:
>
> 1. Zeroing out the stock of the current items/batches
> 2. Creating new item codes with valuation method = Moving Average
> 3. Importing existing stock onto the new codes, by batch, with the correct costs
> 4. Updating all open documents (customer/supplier orders, quotations, etc.) that reference
>    the old item codes
> 5. Restarting warehouse movements on the new codes
>
> **Why this is more complex than it looks**
> Beyond the internal effort (code mapping, transition management, document updates), this
> would also require contacting all our suppliers to reissue technical drawings under the new
> item numbers, and reprinting/relabeling accordingly. Given the number of items involved
> (~31,600), this is a significant undertaking that goes well beyond an internal system
> change — it touches our supplier base directly.
>
> There's also an open point I don't have visibility on: I don't know what the impact would be
> on the items belonging to Trafag and Industrial Components that are currently coded directly
> by you, and that our B1 receives directly from yours via intercompany. I think this
> assessment would probably need to be done together with Lucas Castro and your SAP
> consultants, since it goes beyond what we can evaluate on our side alone.
>
> **Where we stand on cost**
> I don't yet have a cost estimate for this project from our SAP consultant — I'll need to
> request that separately once we have clarity on scope and priority, since it would also
> require dedicated budget and resources on our side.
>
> Given all of this, I'd really appreciate your and Andreas's view on how necessary/urgent
> this alignment is from a group perspective.
>
> Thanks again for your understanding, and sorry for the complexity of the answer — I wanted
> to make sure you had the full picture.
>
> Best regards,
> Paola Castagna, Finance & Administration, Trafag Italia S.r.l.

Die Ausgangsanfrage von Ingo vom 2026-07-28 steckt im selben Mailverlauf; ihre fachliche
Grundlage steht in `docs/FINANCE_STANDARDKOSTEN.md` Abschnitt 4.
