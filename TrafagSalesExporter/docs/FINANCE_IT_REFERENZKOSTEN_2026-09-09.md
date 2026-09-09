# TR IT Referenzkost je Artikel: Vorschlag von Andreas vom 2026-09-09

Stand: 2026-09-09. Gehoert zu `ISS-007.1` (Bewertungsmethode) und zum neuen `ISS-007.2`
(Referenzkost aus dem Bestand).

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
- Offen bleibt nur die Rueckmeldung an Paola. Der Textvorschlag steht in Abschnitt 7.

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

## 4. Was neu ist und gemessen werden muss

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

Bei Chargenbewertung fuehrt B1 die Kosten je Charge. Ein Stueckwert aus dem Artikel- oder
Lagerstamm kann es dort also nicht geben. Die beiden von Andreas gesuchten Groessen muessen
deshalb aus dem **Bestandsjournal** oder den **Chargentabellen** kommen.

**Welche Felder dort tatsaechlich existieren, steht hier bewusst nicht.** Vorrangregel 5 in
`router.md` verbietet, SAP-Feldnamen aus Erinnerung zu behaupten; genau dieser Fehler hat bei
UK-2025 und beim IT-Superlativ zugeschlagen. Die Messung beginnt daher mit einer
Metadatenabfrage und nicht mit einer Datenabfrage.

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

**Noch nicht gelaufen.** Das Firmennetz war am 2026-09-09 gegen 09:50 nicht erreichbar:
`trch-webapp-bidashboard.trafagch.local` loest nicht auf, `10.194.65.22:30015` antwortet
nicht, aktiv ist nur das private WLAN. Damit fehlt sowohl der konsistente
Konfigurationssnapshot als auch die HANA-Verbindung. Sobald das Netz steht, ist die Messung
ein Lauf von wenigen Minuten. Alternativweg, falls der Entwicklungsrechner die Quelle
dauerhaft nicht erreicht: Ausfuehrung ueber den Server nach `docs/router/plattform.md`,
Abschnitt Server-Analyse, wie bei TR IN.

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
> The new part is inventory value divided by quantity on hand. I can already rule out the
> item master as a source, and that is measured, not assumed: `PrdStdCst` is zero for all
> 40,478 items, and `AvgPrice` is empty for all 1.9 million warehouse rows, because with
> batch valuation B1 keeps the cost on the batch. So the two figures have to come from the
> inventory journal or the batch tables instead. I have the read-only queries prepared and
> will send you the numbers, including how many of the 2,082 materials we sold actually
> carry stock — that ratio decides how often the formula applies and how often we fall back
> to the document value.
>
> One point worth settling before we build anything on it: inventory value over quantity on
> hand is a weighted average of what TR IT paid. On Trafag part numbers that is the
> intercompany purchase price — we measured it at 3.48x the Swiss standard cost on average
> for the 867 items both companies carry. So this figure fits the local cost slot of the
> selling company; it should not replace the Swiss standard cost as the group manufacturing
> cost basis, otherwise the group margin measures the transfer price again. Happy to keep it
> as the local figure, as a comparison figure, or both — that is your call, and it is the
> same question as whether the latest positive document cost is enough or we need an average
> and a cut-off date.
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

## 8. Abgrenzung

Andreas hat gefragt, **ob** sich die beiden Werte einfach ziehen lassen, nicht nach einer
Funktion im Dashboard. Es ist deshalb kein Code entstanden, keine Kostenquelle umgestellt
und nichts deployed. Wenn die Messung tragfaehig ist, ist der Einbau ein eigener Auftrag mit
eigenem Entscheid nach Abschnitt 6.
