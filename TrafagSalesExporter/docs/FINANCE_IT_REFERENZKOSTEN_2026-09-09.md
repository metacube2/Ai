# TR IT Referenzkost je Artikel: Vorschlag von Andreas vom 2026-09-09

Stand: 2026-09-29, Messergebnis in Abschnitt 4a, Paolas Antwort in Abschnitt 7a, ihre Mail vom
2026-09-29 und Ingos am selben Tag versendete Antwort in Abschnitt 7b, ihre Rueckfrage zum Lagerumfang
und die Aufteilung je Lager in Abschnitt 7c.
Gehoert zu `ISS-007.1` (Bewertungsmethode) und `ISS-007.2` (Referenzkost aus dem Bestand).

Vorgaengerstand: `docs/FINANCE_IT_BEWERTUNGSMETHODE_PAOLA_2026-09-04.md`. Fachlicher
Gesamtstand der Kostenbasis: `docs/FINANCE_STANDARDKOSTEN.md`.

## 1. Was Andreas geschrieben hat

Andreas Stoller hat am 2026-09-09 auf Paolas Antwort geantwortet. Zwei Aussagen sind
fachlich relevant.

**Erstens, die Bewertungsmethode wird nicht priorisiert.** Er halte eine Umstellung in
anderen ERP-Systemen zwar fuer moeglich, wenn der Bestand auf null steht und keine offenen
bestandsrelevanten Belege existieren; selbst wenn es in B1 technisch ginge, waeren Bestand
auf null bringen, Belege bereinigen und alles wieder aufbauen aber ein erheblicher Aufwand
mit operativem Risiko. Wortlaut:

> I therefore would not give the conversion to Moving Average any priority at the moment.
> We can review the overall setup again next year.

**Zweitens, die eigentliche Frage ist eine andere.** Wortlaut:

> For now, the more relevant question is how we can easily derive one standard/reference
> cost per article for the group analysis. [...] Mathematically, the approach should be
> relatively simple: total inventory value of the article divided by the total quantity on
> hand. The question is mainly whether these two values can be extracted easily from B1 at
> article level despite the batch valuation.

Fuer Artikel ohne Bestand schlaegt er den jeweils letzten verfuegbaren Bewertungs- bzw.
Kostenwert vor, unabhaengig von der einzelnen Chargenbewertung, und bezeichnet das
ausdruecklich als pragmatische erste Version des Gruppenreportings.

## 2. Was damit entschieden ist

Paola hat am 2026-09-04 genau eine Einschaetzung erbeten: wie notwendig und wie dringend
die Angleichung aus Konzernsicht ist. **Diese Einschaetzung liegt jetzt vor: nicht
priorisiert, erneute Betrachtung im naechsten Jahr.** Damit gilt fuer `ISS-007.1`:

- Die Kostenschaetzung ist bei Italien **nicht** anzufordern; Paola wollte sie ohnehin erst
  nach geklaerter Prioritaet einholen.
- Die Intercompany-Bewertung mit Lucas Castro ist mit dem Projekt zurueckgestellt, nicht
  erledigt. Sie wird wieder gebraucht, sobald das Thema 2027 aufgerufen wird.
- ~~Offen bleibt nur die Rueckmeldung an Paola.~~ **Am 2026-09-29 versendet** (Text in Abschnitt 7b). **Sie hat am 2026-09-10 geantwortet und will die
  Referenzkost selbst pruefen; das ist erledigt und sollte ihr abgenommen werden.** Gueltiger
  Textvorschlag und ihr Wortlaut in Abschnitt 7a, nicht mehr in Abschnitt 7.

## 3. Was von Andreas' Frage bereits beantwortet ist

Andreas hat selbst geschrieben, er habe eine frueher schon gelieferte Antwort moeglicherweise
uebersehen. Das trifft auf den **Fallback fuer Artikel ohne Bestand** zu: der ist bereits die
geltende und produktive Loesung.

Der von Andreas am 2026-07-27 freigegebene Weg ist der Belegkostenwert
`INV1.StockPrice` beziehungsweise `RIN1.StockPrice`, also der Lagerwert, mit dem B1 die
Position bei der Fakturierung bewertet hat. Er arbeitet auf Belegebene und ist von der
Bewertungsmethode unabhaengig. Gemessene Abdeckung fuer 2026 verkaufte Materialien:
**2'019 von 2'082, also 97.0 %**, mit `StockPrice` groesser null. Feldbelegung siehe
`docs/QUELLSYSTEME_SAP_B1.md`, Messung siehe `docs/FINANCE_STANDARDKOSTEN.md` Abschnitt 4.

Damit ist der Teil "letzter verfuegbarer Wert fuer Artikel ohne Bestand" kein offener Punkt,
sondern seit dem 2026-07-27 der Standardweg. Zu klaeren bleibt an dieser Stelle nur die
schon in Abschnitt 7 der Kostendoku gefuehrte Frage, ob der **juengste** positive Belegwert
dauerhaft gilt oder ein Durchschnitt beziehungsweise ein Stichtag noetig ist.

## 4. Was neu war und wie es gemessen wurde

Neu ist die Kennzahl **Bestandswert geteilt durch Bestandsmenge je Artikel**. Das ist genau
die "zusaetzlich kalkulierte Groesse", die Andreas am 2026-07-27 noch nicht gebraucht hat;
die Anforderung hat sich also geaendert, das ist kein Widerspruch, aber eine Erweiterung
gegenueber dem freigegebenen Weg.

**Der Artikelstamm ist als Quelle bereits ausgeschlossen**, und zwar gemessen, nicht
vermutet (live gegen `it01_p` am 2026-07-27):

| Feld | Ergebnis |
| --- | --- |
| `OITM.PrdStdCst` | 0 bei allen 40'478 Artikeln |
| `OITM.AvgPrice` | groesser null bei nur 248 von 40'478 |
| `OITW.AvgPrice` | 0 bei allen 1'902'456 Lagerzeilen |

Bei Chargenbewertung fuehrt B1 die Kosten je Charge, ein **Stueckwert** aus diesen Feldern
kann es also nicht geben. Die Vermutung vor der Messung war deshalb, die beiden Groessen
muessten aus dem Bestandsjournal oder den Chargentabellen kommen. Das war zu pessimistisch:
die Metadatenabfrage hat mit `OITM.StockValue` ein gepflegtes Feld gefunden, das die
frueheren Messungen nicht im Blick hatten, weil sie nach Stueckwerten gesucht haben und
nicht nach dem Bestandswert. Einzelheiten in Abschnitt 4a.

**Die Feldnamen sind nicht geraten, sondern gemessen.** Vorrangregel 5 in `router.md` verbietet,
SAP-Feldnamen aus Erinnerung zu behaupten; genau dieser Fehler hat bei UK-2025 und beim
IT-Superlativ zugeschlagen. Das Messpaket beginnt deshalb mit einer Metadatenabfrage und nicht
mit einer Datenabfrage. Genau die hat den Fund gebracht: die Felder in Abschnitt 4a stammen aus
`SYS.TABLE_COLUMNS`, nicht aus einer Annahme.

### Vorbereitetes Messpaket

`.tmp_tools/ItReferenceCost0909/it_referenzkosten.sql`, ausgefuehrt mit dem vorhandenen
rein lesenden Laeufer `.tmp_tools/HanaQ` (laesst ausschliesslich `SELECT`/`WITH` zu und
loest Verbindung und Schema aus der Konfiguration auf):

```text
dotnet run --project .tmp_tools/HanaQ -c Release -- TRIT .tmp_tools/ItReferenceCost0909/it_referenzkosten.sql <konsistenter-snapshot.db>
```

| Abfrage | Was sie klaert |
| --- | --- |
| 01 | Welche Wert-, Mengen- und Kostenspalten es in `OITM`, `OITW`, `OINM`, `OIBT`, `OBTN`, `OBTQ`, `OBTW`, `OSRI`, `OSRQ` wirklich gibt |
| 02 | Welche dieser Tabellen im Schema existieren und wie viele Zeilen sie fuehren |
| 03 | Kandidat A: Menge und Wert je Artikel aus dem Lagerstamm; erwartet wird ein Wert von null, weil `AvgPrice` leer ist |
| 04, 05 | Kandidat B: Menge und Wert kumuliert aus dem Bestandsjournal, dazu die zehn wertgroessten Artikel mit abgeleiteter Referenzkost |
| 06 | **Gegenprobe:** stimmt die Menge aus dem Journal mit der Menge im Lagerstamm ueberein. Eine Referenzkost, die sich nicht gegen die Bestandsmenge abstimmen laesst, ist keine Antwort |
| 07 | Kandidat C: Menge und Artikelzahl aus den Chargentabellen |
| 08 | **Deckungsgrad auf der Reportingmenge:** wie viele der 2026 verkauften Artikel Bestand fuehren, die Formel also greift, und fuer wie viele der Fallback gilt. Die Zahlen 31'600 und 40'478 aus dem Artikelstamm beantworten eine andere Frage und wuerden den Aufwand falsch darstellen |
| 09 | Abgleich der abgeleiteten Referenzkost gegen den freigegebenen Belegwert `StockPrice` |

Die Abfragen 03 bis 09 nennen Spalten, die erst 01 und 02 bestaetigen. Der Laeufer bricht
bei einem Fehler nicht ab, sondern protokolliert ihn je Anweisung; falsch geratene Namen
fallen damit sichtbar durch und werden nicht als Befund verkauft.

**Gelaufen am 2026-09-09 gegen 10:30**, nachdem das Netz wieder stand, read-only gegen
`it01_p` auf `travtrp0:30015` als `TRAFAG_ALL`. Eine zweite Runde
(`it_referenzkosten2.sql`) hat den Fund aus der Metadatenabfrage vertieft. Rohprotokolle:
`.tmp_tools/ItReferenceCost0909/lauf1.txt` und `lauf2.txt`.

**Betriebshinweis:** Die produktive SQLite-Datei auf dem Share liess sich auch mit
stehendem VPN nicht oeffnen (`unable to open database file`), obwohl `Test-Path` wahr
liefert und der HANA-Port antwortet. Fuer HanaQ genuegt die Konfiguration, deshalb lief die
Messung mit der lokalen `trafag_exporter.db`; Host, Schema und Benutzer werden vom Werkzeug
ausgegeben und stimmen mit `docs/QUELLSYSTEME_SAP_B1.md` ueberein. Die Messwerte selbst
kommen ausschliesslich aus HANA, nicht aus der lokalen Kopie.

## 4a. Ergebnis der Messung

**Ja, beide Werte lassen sich einfach ziehen, und zwar direkt auf Artikelebene.** Der
Artikelstamm fuehrt neben den bekannten Nullfeldern zwei Spalten, die tatsaechlich gepflegt
sind: `OITM.StockValue` und `OITM.OnHand`. Eine Chargenaggregation ist nicht noetig.

| Messung | Ergebnis |
| --- | ---: |
| `OITM.StockValue` gefuellt | 1'077 von 40'720 Artikeln |
| Summe `OITM.StockValue` | 987'909.28 EUR |
| Summe `OITM.OnHand` | 62'601 Stueck |
| Bestandsjournal `OINM` kumuliert, 154'762 Zeilen | 62'601 Stueck und 987'909.28 EUR |
| Artikel mit Bestand groesser null | 1'100, davon 1'077 mit Wert |

Wert und Menge des Artikelstamms sind also **auf den Cent und das Stueck identisch mit dem
kumulierten Bestandsjournal**. Die Gegenprobe je Artikel ueber 9'323 Artikel ergibt
**0 Mengenabweichungen** zwischen Journal und Lagerstamm. Damit ist die Zahl abgestimmt und
nicht nur vorhanden.

Zwei Wege, die man erwarten wuerde und die **nicht** tragen:

- `OITW.StockValue` ist in allen 1'913'828 Lagerzeilen null, ebenso `OITW.AvgPrice`. Der
  Bestandswert wird auf Lagerebene nicht gefuehrt, nur auf Artikelebene.
- `OBTN.CostTotal` sieht wie der Chargenwert aus, ist es aber nicht: 38'791'861.79 EUR bei
  62'267 offenen Stueck ergaebe rund 623 EUR je Stueck gegenueber 15.78 EUR aus den beiden
  abgestimmten Quellen. Das Feld summiert offenkundig die Kosten aller je gebuchten
  Chargenzugaenge, nicht den offenen Bestand. **Nicht verwenden.**

### Der Haken liegt nicht in der Technik, sondern in der Abdeckung

| Bezugsgroesse | Mit Bestand | Ohne Bestand |
| --- | ---: | ---: |
| 2026 verkaufte Materialien: 2'216 (Stand 09.09., im Juli waren es 2'082) | 568 (25.6 %) | 1'648 (74.4 %) |
| 2026 fakturierter Positionsumsatz: 9'254'229.87 EUR | 2'677'517.43 (28.9 %) | 6'576'712.44 (71.1 %) |

Die Formel greift also fuer rund ein Viertel der verkauften Materialien und rund 29 Prozent
des Umsatzes. Fuer die uebrigen drei Viertel gilt ohnehin der Fallback, also der heute schon
produktive Belegwert.

### Und dort, wo sie greift, sagt sie dasselbe wie der heutige Weg

Fuer die 555 Artikel, die sowohl Bestand als auch einen Belegwert 2026 haben:

| Verhaeltnis Referenzkost zu `StockPrice` | Artikel |
| --- | ---: |
| Median | **1.00** |
| zwischen 0.8 und 1.25 | 484 von 555 |
| unter 0.8 | 49 |
| ueber 1.25 bis 2 | 11 |
| ueber 2 | 11 |

Getrennt nach Nummernkreis, weil Abschnitt 5 genau dort einen Unterschied vermuten liesse:

| Nummernkreis | Artikel | Referenzkost im Mittel | `StockPrice` im Mittel | Median des Verhaeltnisses |
| --- | ---: | ---: | ---: | ---: |
| numerische Trafag-Sachnummer | 326 | 59.43 | 65.85 | 1.00 |
| italienische Hausnummer | 207 | 33.14 | 37.46 | 1.00 |
| sonstige | 22 | 74.82 | 75.08 | 1.00 |

Das ist der eigentliche Befund: Beide Wege messen dieselbe Kostenwelt, auch bei den
Trafag-Sachnummern. Die Referenzkost aus dem Bestand ist also **keine neue Kostenquelle**,
sondern eine Bestaetigung der vorhandenen, mit geringerer Abdeckung. Damit traegt sie
dieselbe Intercompany-Ladung wie der Belegwert, was die Abgrenzung in Abschnitt 5
bestaetigt statt sie zu entkraeften.

**Empfehlung:** Den Belegwert `INV1.StockPrice` als fuehrende Kostenbasis behalten und die
Referenzkost aus dem Bestand als monatliche Plausibilisierung fuehren, die zeigt, ob die
Belegwerte zum bewerteten Bestand passen. Ein Umbau der Kostenquelle wuerde bei 29 Prozent
Umsatzabdeckung und einem Median von 1.00 praktisch keine andere Zahl liefern.

## 5. Die Einschraenkung, die in jede Antwort gehoert

Bestandswert geteilt durch Bestandsmenge ist ein gewichteter Mittelwert dessen, **was
Trafag Italia bezahlt hat**. Bei Trafag-Sachnummern ist das der
Intercompany-Einkaufspreis. Genau dieser Effekt ist bereits gemessen: fuer die 867
Materialien, die Italien und die Schweiz beide fuehren, liegt der italienische Stueckwert im
Mittel beim **3.48-fachen** des Schweizer `MBEW-STPRS`, 296 davon ueber dem Dreifachen
(`docs/FINANCE_STANDARDKOSTEN.md` Abschnitt 10).

Daraus folgt keine Ablehnung der Kennzahl, sondern eine Abgrenzung ihres Zwecks: Die
Referenzkost aus dem Bestand gehoert an die Stelle der **lokalen** Standardkosten der
verkaufenden Gesellschaft. Sie ersetzt nicht die Konzern-Herstellkostenbasis, fuer die bei
Schweizer Fertigung `MBEW-STPRS` gilt. Wird sie ohne diese Trennung in die Gruppenmarge
gestellt, misst die Marge wieder den Verrechnungspreis heraus, also genau das, was die
Gruppenmarge beseitigen soll.

Abfrage 09 des Messpakets liefert dazu die Zahl: das Verhaeltnis der abgeleiteten
Referenzkost zum Belegwert `StockPrice`. Liegt es nahe bei eins, tragen beide Wege dieselbe
Intercompany-Ladung, und die Kennzahl ist eine Praezisierung der lokalen Kosten, keine neue
Kostenquelle.

## 6. Der Entscheid, den wir dafuer brauchen

| Frage | Bei wem |
| --- | --- |
| Wird die Referenzkost aus dem Bestand die **fuehrende** lokale Kostenbasis fuer TR IT, oder eine Vergleichsgroesse neben dem Belegwert `StockPrice`? | Andreas |
| Gilt fuer Artikel ohne Bestand der juengste positive Belegwert, oder braucht es Durchschnitt und Stichtag? Diese Frage steht seit dem 2026-08-27 offen und wird durch Andreas' Vorschlag konkret | Andreas |
| Ist die Kennzahl ein Stichtagswert je Monat? Bestandswert und Bestandsmenge bewegen sich taeglich; ohne Stichtag ist die Zahl nicht reproduzierbar | Andreas / Finance |

## 7. Antwortvorschlaege, nicht versendet

Beide Texte sind Vorschlaege fuer Ingo. Es ist keine Mail verschickt worden.

### An Andreas, Englisch

> Hi Andreas,
>
> Agreed on not prioritising the valuation change — I'll close that off with Paola so she
> doesn't wait for a group decision, and we can pick the setup up again next year.
>
> On the reference cost, one part of it is already live. For articles without stock, the
> "latest available valuation" is exactly what we already use: the document-level stock
> value B1 records on the invoice line (`INV1.StockPrice`). You released that path on
> 27 July, and it is independent of the batch valuation. Coverage for the materials TR IT
> sold in 2026 is 2,019 of 2,082, or 97.0 %.
>
> On the second part: yes, both figures come out of B1 easily, and at item level, so the
> batch valuation is not in the way. The item master carries `StockValue` and `OnHand`
> directly — 1,077 items with a value, EUR 987,909.28 over 62,601 units — and that
> reconciles to the cent and the unit with the cumulated inventory journal, with zero
> quantity deviations across the 9,323 items I compared. So the mechanics are a non-issue.
>
> The two caveats are about usefulness rather than feasibility, and I think they change the
> answer:
>
> Coverage is thin. Of the 2,216 materials we invoiced in Italy in 2026, only 568 carry
> stock at all — that is 25.6 % of the materials and 28.9 % of the invoiced revenue
> (EUR 2.68m of 9.25m). For the remaining 71 % the fallback applies anyway, so the document
> value stays the workhorse either way.
>
> And where both exist, they say the same thing. For the 555 items with both stock and a
> 2026 document value, the median ratio between the two is exactly 1.00, and 484 of 555 sit
> within plus/minus 25 %. That holds separately for Trafag part numbers (326 items) and for
> Italian house numbers (207) — no systematic gap. Which also answers a question I had:
> the inventory figure carries the same intercompany loading as the document value, since
> it is a weighted average of what TR IT paid. On Trafag part numbers that is the purchase
> price, which we measured at 3.48x the Swiss standard cost. So neither figure is a group
> manufacturing cost; both belong in the local cost slot of the selling company.
>
> My suggestion, therefore: keep the document value as the leading cost basis and run
> inventory value over quantity on hand as a monthly plausibility check against it, which
> is cheap now that we know where the fields are. Rebuilding the cost source would cost
> effort and, at a median ratio of 1.00 and 29 % coverage, produce practically the same
> number. If you would rather have it as the leading figure for the items that do carry
> stock, say so and I will set it up — it is your call, and it is the same decision as
> whether the latest positive document value is enough or we need an average and a cut-off
> date.
>
> Best regards,
> Ingo

### An Paola, Englisch

> Hi Paola,
>
> Thank you for the thorough assessment — that is exactly the clarity we needed, and the
> answer is clear enough that we are not going to ask you for a cost estimate.
>
> Andreas has confirmed that the conversion to Moving Average is not a priority for the
> group at this point. We will review the overall setup again next year, and the
> intercompany question on the Trafag and Industrial Components items goes on hold with it
> rather than being dropped — if we revisit it, we will do that assessment together with
> Lucas Castro as you suggested.
>
> To be explicit about one thing, because it is easy to misread: the group reporting does not
> depend on your valuation method. We take the cost from the invoice line, which B1 fills
> regardless of batch valuation, so nothing on your side is blocking us.
>
> Best regards,
> Ingo

**Ueberholt seit dem 2026-09-10.** Dieser Entwurf ist geschrieben worden, bevor Paola
geantwortet hat. Er ist fachlich weiter richtig, aber unvollstaendig: er sagt nichts dazu,
dass Paola die Referenzkost selbst pruefen will. Gueltig ist der Text in Abschnitt 7a.

## 7a. Antwort von Paola vom 2026-09-10 und der neue Antwortvorschlag

Paola hat auf Andreas' Rueckmeldung geantwortet. Drei Punkte:

**Erstens, eine Klarstellung.** Ihr Befund ist das Ergebnis der Pruefung durch ihre
SAP-Berater, nicht ihre eigene Einschaetzung.

> Just to clarify: what I reported was the outcome of the analysis carried out by our SAP
> consultants, who confirmed that the valuation method of an existing item in B1 cannot be
> changed once the item has been created, regardless of stock on hand [...]

**Zweitens, ein Vorbehalt, der offen bleibt.** Sie schliesst ausdruecklich nicht aus, dass
Lucas Castro oder unsere Berater eine andere Loesung kennen.

> It's possible that Lucas or your SAP consultants (ANG) have a different solution or a
> workaround I'm not aware of — at the moment I only have the explanation I was given, so I
> can't rule that out.

Dieser Vorbehalt ist **nicht aufgeloest**. Er ist aber gegenstandslos, solange die
Umstellung nicht priorisiert ist, und ist der erste Punkt fuer ANG, falls das Thema 2027
aufgerufen wird. Ihn jetzt zu verfolgen waere Arbeit an einem zurueckgestellten Projekt.

**Drittens, und das ist der handlungsrelevante Teil:** Sie will die Referenzkost selbst
pruefen, sobald es die Arbeitslast erlaubt, und sich dann melden.

> Regarding the alternative approach you're suggesting (a mathematical reference cost per
> article), I won't be able to look into this right away given our current workload, but I
> will take it up again as soon as possible [...]

**Diese Pruefung ist erledigt und sollte ihr abgenommen werden.** Die Messung vom
2026-09-09 in Abschnitt 4a beantwortet genau ihre Frage: die Werte lassen sich trotz
Chargenbewertung auf Artikelebene ableiten. Wenn wir das nicht sagen, arbeitet Italien
unter Arbeitslast an einer Frage, die zentral schon beantwortet ist, und meldet sich in
einigen Wochen mit demselben Ergebnis.

Uebrig bleibt genau **eine** Frage an Italien, die wir nicht selbst beantworten koennen:
ob `OITM.StockValue` demselben Bestandswert entspricht, den Italien bilanziell ausweist.
Das ist billig fuer Paola und genau die Frage, die Andreas stellen wird, sobald die
Kennzahl im Gruppenreporting auftaucht.

### Gueltiger Antwortvorschlag an Paola, Andreas in Kopie

> Hi Paola,
>
> Thanks for the clarification - and to be clear, nobody is questioning your consultants'
> finding. Two things from our side.
>
> On the valuation method: Andreas has taken the conversion off the priority list for now,
> and we will look at the overall setup again next year. So there is no need to chase a
> possible workaround with Lucas or ANG at this point. If we do revisit it, that assessment
> belongs in the same round, together with the intercompany question on the Trafag and
> Industrial Components items.
>
> On the reference cost, please don't spend any time on it - we measured it centrally today,
> read-only against your database, and it works. The item master carries StockValue and
> OnHand, and both are populated: 1,077 items with a value, EUR 987,909.28 over 62,601
> units. That reconciles to the cent with the cumulated inventory journal, and the
> quantities match the warehouse records for all 9,323 items I compared. So the figure can
> be derived at article level despite the batch valuation, from our side, with no work on
> yours.
>
> There is one thing where your view would help, and it is the only thing I need: does
> StockValue in the item master match the inventory value your finance side reports, for
> example at the last month end? If it does, we can build on it with confidence. If your
> balance sheet figure comes from a different source, I would rather know that now than
> after a group figure is built on it.
>
> For context, the limitation turned out to be coverage rather than mechanics: only about a
> quarter of the articles you invoiced in 2026 carry stock at all, so for the rest we keep
> using the cost B1 records on the invoice line, which is what the group reporting already
> does today.
>
> Best regards,
> Ingo

**Ob dieser Text versendet wurde, ist nicht dokumentiert.** Paolas Mail vom 2026-09-29 in
Abschnitt 7b spricht eher dagegen.

## 7b. Mail von Paola vom 2026-09-29: Italien baut die Abfrage selbst

Paola schreibt nach erneuter Rueckfrage bei ihren SAP-Beratern zwei Dinge. Erstens ist die
Umstellung auf Moving Average am bestehenden Artikel doch moeglich, wenn der Bestand auf null
steht und die verknuepften Belege abgeschlossen sind; das kostet einen Betriebsunterbruch und
eine Inventur. Das betrifft `ISS-007.1` und steht mit Wortlaut in
`docs/FINANCE_IT_BEWERTUNGSMETHODE_PAOLA_2026-09-04.md` Abschnitt 7. Zweitens, und das
betrifft diese Datei:

> [...] together with the SAP consultants we've also looked at an alternative: building an
> automated query that derives the Moving Average value directly from the data already
> available in B1, without physically changing the valuation method or touching the stock.
> This would let us get to the same result — a reliable Moving Average figure at article
> level — without the disruption of the full conversion. [...] we'll continue working on it
> from our side.

**Das ist die Frage, die Abschnitt 4a am 2026-09-09 schon beantwortet hat.** Der Bestandswert
je Artikel ergibt sich aus `OITM.StockValue / OITM.OnHand` ohne Chargenaggregation und ist
gegen das Bestandsjournal abgestimmt. Wenn Italien das mit seinen Beratern nachbaut, zahlt
Italien Beraterstunden fuer ein vorhandenes Ergebnis. Die Rueckmeldung aus Abschnitt 7a ist
deshalb jetzt dringlich und nicht mehr nur hoeflich.

Einen moeglichen Mehrwert hat die Abfrage der Berater, falls sie einen echten gleitenden
Durchschnitt ueber die Bewegungshistorie in `OINM` nachrechnet: dann bekaemen auch Artikel
ohne heutigen Bestand einen Wert, naemlich den letzten Durchschnitt vor dem Abgang auf null,
und die Abdeckungsluecke aus Abschnitt 4a wuerde kleiner. Das ist eine Vermutung; die Mail
sagt nicht, wie die Abfrage rechnen soll. Weil `INV1.StockPrice` bereits 97 Prozent der
verkauften Materialien abdeckt, waere der Gewinn gering.

### Antwort an Paola, Andreas in Kopie, von Ingo am 2026-09-29 versendet

Ingo hat am 2026-09-29 bestaetigt, dass die Mail an Paola raus ist; der folgende Text war der
vorbereitete Entwurf. Offen ist jetzt nur noch Paolas Rueckmeldung, ob `OITM.StockValue` dem
bilanziellen Bestandswert entspricht, und ob ihre Berater etwas anderes rechnen.

Ersetzt den Text aus Abschnitt 7a, falls dieser nicht verschickt wurde.

> Hi Paola,
>
> Thanks for following up with the consultants, and good to have it confirmed that the
> conversion is possible in principle. As Andreas said, we won't pursue it for now.
>
> On the automated query, please hold off before your consultants invest time in it. We
> already ran this centrally, read-only against your B1 database: the item master carries
> StockValue and OnHand at article level despite the batch valuation. That gives
> EUR 987,909.28 over 62,601 units for 1,077 items, reconciling to the cent with the
> inventory journal. Where an item carries stock, the result matches the invoice-line cost we
> already use (median ratio 1.00).
>
> The one thing only you can confirm: does StockValue match the inventory value your finance
> side reports at month end? If your consultants' query does something beyond this, for
> example a historical moving average for items without current stock, I'd be glad to compare
> before you build it.
>
> Best regards,
> Ingo


## 7c. Paolas Rueckfrage vom 2026-09-29 und Aufteilung je Lager

Paola fragt, ob wir dieselbe Quelle und denselben Umfang nutzen: ihr Wert kommt aus dem SAP
**Inventory Audit Report**, eingeschraenkt auf die realen, physischen Lager von TR IT.

Gemessen am 2026-09-29 rein lesend gegen `it01_p` (`.tmp_tools/ItStockWarehouse0929/lager.sql`,
Protokoll `lauf.txt`): Das Bestandsjournal `OINM` ergibt heute ueber **alle** Lager
**1'010'088.55 EUR bei 63'965 Stueck** (am 09.09. waren es 987'909.28 EUR). `OITM.StockValue` ist
die Summe ueber alle Lager; der Inventory Audit Report liest dasselbe Journal, der Unterschied
kann also nur in der Lagerauswahl liegen.

| Lager | Bedeutung | Wert EUR |
| --- | --- | ---: |
| `Mc` | Magazzino centrale | 841'651.04 |
| `Mc_CRES` | Customer Reservation | 142'523.02 |
| `Ms...` (14 Lager) | Bestand bei Lohnfertigern, dazu Laboratorio und Lavorazioni | rund 21'400 |
| `Mc_Cvis`, `Mc_Fiere`, `Mc_DENI`, `Mc_Sosti`, `Mc_MARIN` | C/visione, Messen, weitere | rund 8'500 |
| `Mc_Resi` | Retouren, **negativer** Wert | -3'976.25 |

Auffaellig: `Mc_Resi` (-3'976.25 EUR bei 125 Stueck) und `Ms PIXSY` (-571.18 EUR bei 2 Stueck)
haben negative Bestandswerte. Drei Lager sind in B1 als nicht disponierbar markiert (`Nettable = N`:
`Mc_Cvis`, `Mc_DENI`, `Mc_MARIN`).

### Antwortvorschlag an Paola, nicht versendet

> Hi Paola,
>
> Good question, and it is the right one to settle first. The source is the same, the scope is
> probably not. Our figure is the item master StockValue, which is the inventory journal summed over
> **all** TRIT warehouses; the Inventory Audit Report reads the same journal, so the only possible
> difference is the warehouse selection.
>
> As of today we see EUR 1,010,088.55 in total. By warehouse: Mc (Magazzino centrale) 841,651.04;
> Mc_CRES (customer reservation) 142,523.02; stock at subcontractors (the Ms... warehouses) about
> 21,400; the remaining Mc_ warehouses (C/visione, Fiere, DENI, Sostituzioni) about 8,500; and
> Mc_Resi at minus 3,976.25.
>
> Could you tell me which warehouses count as "real/physical" in your report? Then I will restrict
> our figure to exactly those and we can compare at month end. Two small things I noticed on the
> way: Mc_Resi and Ms PIXSY carry negative stock values, which you may want to look at anyway.
>
> Best regards,
> Ingo

## 8. Abgrenzung

Andreas hat gefragt, **ob** sich die beiden Werte einfach ziehen lassen, nicht nach einer
Funktion im Dashboard. Es ist deshalb kein Code entstanden, keine Kostenquelle umgestellt
und nichts deployed. Wenn die Messung tragfaehig ist, ist der Einbau ein eigener Auftrag mit
eigenem Entscheid nach Abschnitt 6.
