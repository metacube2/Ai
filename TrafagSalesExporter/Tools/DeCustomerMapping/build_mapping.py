"""Build a bounded invoice/address bridge and an auditable DE sales workbook.

uv run --python 3.12 --with openpyxl python Tools/DeCustomerMapping/build_mapping.py
  --sales <processed.csv> --evidence <optional additional named workbook>
Never equate an internal RechnungsAdressenID with an external AdressNr.
"""
import argparse
import csv
import hashlib
import json
from collections import Counter, defaultdict
from decimal import Decimal
from pathlib import Path
import openpyxl
from openpyxl.styles import Font, PatternFill

p = argparse.ArgumentParser()
p.add_argument('--sales', required=True)
p.add_argument('--evidence', action='append', default=[])
p.add_argument('--output', default='Data/de_customer_invoice_map_2026-09-09.json')
p.add_argument('--excel', default='Bahnmarkt_DE_Kundenzuordnung_2026-09-09.xlsx')
a = p.parse_args()
def norm(v):
    return str(v).strip() if v is not None else ''
def read_csv(path):
    with open(path, encoding='utf-8-sig', newline='') as f:
        return list(csv.DictReader(f, delimiter=';'))

invoices = defaultdict(set)
evidence = defaultdict(set)
sources = sorted(set([Path('docs/2025_DataExport_DE.xlsx'), *Path('docs').glob('Sales_TRDE*.xlsx'), *map(Path, a.evidence)]))
for source in sources:
    book = openpyxl.load_workbook(source, read_only=True, data_only=True)
    for sheet in book:
        rows = iter(sheet.values)
        headers = [norm(x).lower() for x in next(rows, [])]
        if 'adressnummer-kunde' not in headers and 'customer number' not in headers:
            continue
        for row in rows:
            d = dict(zip(headers, row))
            if d.get('tsc') and d['tsc'] != 'TRDE':
                continue
            invoice = norm(d.get('belegnummer') or d.get('invoice number'))
            customer = norm(d.get('adressnummer-kunde') or d.get('customer number'))
            name = norm(d.get('name kunde') or d.get('customer name'))
            if invoice and customer and name:
                invoices[invoice].add(customer)
                evidence[(invoice, customer)].add(str(source))
    book.close()

bridge = defaultdict(set)
bridge_evidence = defaultdict(set)
for source in [Path('AlphaplanExportPackage/invoice_headers.csv'), Path('AlphaplanExportPackage/delta/invoice_headers.csv')]:
    for d in read_csv(source):
        key, invoice = norm(d.get('RechnungsAdressenID')), norm(d.get('Belegnummer'))
        for customer in invoices.get(invoice, []):
            bridge[key].add(customer)
            bridge_evidence[(key, customer)].add(invoice)

master = {}
for row in read_csv('AlphaplanExportPackage/kundenstamm_DE_20260908.csv'):
    key = row['AdressNr']
    if key in master and master[key] != row:
        raise ValueError(f'Ambiguous master address: {key}')
    master[key] = row
sales = read_csv(a.sales)
mapping = {}
audit = []
counts = Counter()
for row in sales:
    if row['TSC'] != 'TRDE':
        raise ValueError('Expected a TRDE-only processed source')
    internal, invoice = row['CustomerNumber'], row['InvoiceNumber']
    direct = invoices.get(invoice, set())
    candidates = direct if direct else bridge.get(internal, set())
    basis = 'Belegnachweis' if direct else 'Historische ID-Bruecke'
    corrected = dict(row)
    if len(candidates) == 1 and next(iter(candidates)) in master:
        customer = next(iter(candidates))
        m = master[customer]
        item = dict(InvoiceNumber=invoice, InternalId=internal, CustomerNumber=customer,
                    CustomerName=m['Name'], CustomerCountry=m['Land'], CustomerIndustry=m['Branche'], Basis=basis,
                    Evidence=sorted(evidence[(invoice, customer)]) if direct else sorted(bridge_evidence[(internal, customer)]))
        key = (invoice, internal)
        if key in mapping and mapping[key] != item:
            raise ValueError(f'Conflicting invoice mapping: {key}')
        mapping[key] = item
        corrected.update({k: item[k] for k in ['CustomerNumber', 'CustomerName', 'CustomerCountry', 'CustomerIndustry']})
    else:
        basis = 'Konflikt' if len(candidates) > 1 else 'Offen'
        corrected['CustomerNumber'] = 'ALPHAPLAN-ID:' + internal if internal else ''
    counts[basis] += 1
    audit.append((row, corrected, basis))

target = Path(a.output)
target.parent.mkdir(parents=True, exist_ok=True)
target.write_text(json.dumps(list(mapping.values()), ensure_ascii=False, indent=2), encoding='utf-8')
book = openpyxl.Workbook()
s = book.active
s.title = 'Lesehinweis'
s.append(['Pruefung', 'Ergebnis'])
s.append(['Quelle', str(Path(a.sales))])
s.append(['SHA256 Quelle', hashlib.sha256(Path(a.sales).read_bytes()).hexdigest()])
s.append(['Verkaufszeilen', len(sales)])
for key, count in counts.items():
    s.append([key, count])
s.append(['Historische ID-Bruecke', 'Ableitung aus eindeutigen historischen Belegen; kein aktueller Stammdatenexport der internen ID. Gilt nur fuer die hier aufgefuehrten Rechnungen.'])
s.append(['Produktive Uebernahme', 'Nur Belegnachweise. Historische Ableitungen stehen ausschliesslich als Vorschlaege im eigenen Blatt; im DE-Verkaufsblatt bleiben sie unaufgeloest.'])
s.append(['Offene Nummern', 'ALPHAPLAN-ID: kennzeichnet interne IDs, keine fachlichen Kundennummern. Keine Zuordnung anhand Lieferantennummer oder Namensaehnlichkeit.'])
s.append(['Branchen', 'Nur reine Bahnbranchen sind automatisch Railway; Mischbranchen sind nicht bestaetigt.'])
s.append(['Finanzwerte', 'Aus der Quelldatei unveraendert; keine Waehrungsaddition. Dies ist der DE-Teil, keine weltweite Bahnmarkt-Gesamtsumme.'])
s.append(['Produktivstatus', 'Vorbereiteter Abgleich; Produktivnachweis separat in der Dokumentation.'])
headers = [h for h in sales[0] if h in {'TSC', 'InvoiceNumber', 'PositionOnInvoice', 'Material', 'Name', 'ProductFamilyText', 'ProductDivisionText', 'Quantity', 'CustomerNumber', 'CustomerName', 'CustomerCountry', 'CustomerIndustry', 'SalesPriceValue', 'SalesCurrency', 'PostingDate', 'InvoiceDate'}]
for title, selected in [('DE_Verkaeufe', audit), ('Offene_Zuordnungen', [x for x in audit if x[2] in ('Offen', 'Konflikt')]), ('Historisch_abgeleitet', [x for x in audit if x[2] == 'Historische ID-Bruecke'])]:
    s = book.create_sheet(title)
    s.append(['Zuordnungsnachweis', 'Interne Adress-ID', *headers])
    for original, corrected, basis in selected:
        display = corrected
        if title == 'DE_Verkaeufe' and basis == 'Historische ID-Bruecke':
            display = dict(original, CustomerNumber='ALPHAPLAN-ID:' + original['CustomerNumber'])
        numeric = {'SalesPriceValue', 'Quantity', 'PositionOnInvoice'}
        s.append([basis, original['CustomerNumber'], *[float(Decimal(display[h])) if h in numeric and display[h] else display[h] for h in headers]])
s = book.create_sheet('Railway_Umsatz')
s.append(['Jahr', 'Kundennummer', 'Kundenname', 'Waehrung', 'Verkaufszeilen', 'Umsatz', 'Nachweis'])
totals = defaultdict(lambda: [0, Decimal(0)])
for original, corrected, basis in audit:
    if basis != 'Belegnachweis' or corrected['CustomerIndustry'].strip() not in ('00 Bahn', '05 rw Railways / Bahntechnik'):
        continue
    key = ((corrected['PostingDate'] or corrected['InvoiceDate'])[:4], corrected['CustomerNumber'], corrected['CustomerName'], corrected['SalesCurrency'])
    totals[key][0] += 1
    totals[key][1] += Decimal(corrected['SalesPriceValue'])
for key, (count, amount) in sorted(totals.items()):
    s.append([*key, count, float(amount), 'Belegnachweis und reine Bahnbranche'])
s = book.create_sheet('Belegbruecke')
s.append(['Rechnung', 'Interne Adress-ID', 'Kundennummer', 'Name', 'Land', 'Branche', 'Nachweis', 'Belege oder Dateien'])
for item in mapping.values():
    s.append([item[k] for k in ['InvoiceNumber', 'InternalId', 'CustomerNumber', 'CustomerName', 'CustomerCountry', 'CustomerIndustry', 'Basis']] + [' | '.join(item['Evidence'])])
for s in book:
    s.freeze_panes = 'A2'
    s.auto_filter.ref = s.dimensions
    for cell in s[1]:
        cell.font = Font(bold=True, color='FFFFFF')
        cell.fill = PatternFill('solid', fgColor='24586B')
    for column in s.columns:
        s.column_dimensions[column[0].column_letter].width = min(65, max(14, max(len(norm(c.value)) for c in column[:100]) + 2))
    # Preserve source values literally, including text beginning with '='.
    for row in s:
        for cell in row:
            if cell.data_type == 'f':
                cell.data_type = 's'
book.save(a.excel)
print(json.dumps(dict(rows=len(sales), mapped_invoices=len(mapping), counts=dict(counts), excel=a.excel), ensure_ascii=False))
