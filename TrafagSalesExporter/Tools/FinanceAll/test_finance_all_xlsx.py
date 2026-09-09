"""Isolierter Exportnachweis: neue und alte DB, Datum und vollstaendiger Feldstatus."""
import sqlite3
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

from openpyxl import load_workbook


class FinanceAllTests(unittest.TestCase):
    def test_datumsfelder_export_and_legacy_database(self):
        """Neue Datumsspalten je Buchungszeile, und eine alte Produktiv-DB bleibt lesbar."""
        for with_due_date in (True, False):
            with self.subTest(with_due_date=with_due_date), tempfile.TemporaryDirectory() as tmp:
                db = Path(tmp) / "fixture.db"
                target = Path(tmp) / "finance.xlsx"
                with sqlite3.connect(db) as con:
                    con.execute("""CREATE TABLE FinancialJournalEntries (
                        Tsc TEXT, DebitAmount TEXT, CreditAmount TEXT, AccountCode TEXT,
                        AccountName TEXT, SignedAmountLocal TEXT, LocalCurrency TEXT,
                        LineMemo TEXT, PostingDate TEXT, JournalEntryId TEXT, JournalEntryLineId INTEGER,
                        FiscalYear INTEGER, FiscalPeriod INTEGER, CostCenter TEXT, Dimension2 TEXT,
                        TransactionType TEXT, SourceDocumentNumber TEXT, IsManual INTEGER,
                        IsReversal INTEGER, SourceSystem TEXT, StoredAtUtc TEXT)""")
                    con.executemany("INSERT INTO FinancialJournalEntries VALUES (" + ",".join("?" * 21) + ")", [
                        ("TRFR", "125.5", "0", "100", "Bank", "125.5", "EUR", "current", "2026-09-01",
                         "2", 0, 2026, 9, "BU1", "", "30", "", 1, 0, "BI1", "2026-09-08"),
                        ("TRFR", "0", "125.5", "100", "Bank", "-125.5", "EUR", "old", "2026-08-01",
                         "1", 0, 2026, 8, "", "", "30", "", 1, 0, "BI1", "2026-09-08"),
                    ])
                    if with_due_date:
                        for spalte in ("DueDate TEXT NULL", "ClearingDate TEXT NULL",
                                       "ReconciliationDate TEXT NULL",
                                       "ClearingReference TEXT NOT NULL DEFAULT ''",
                                       "ClearingCount INTEGER NOT NULL DEFAULT 0",
                                       "IsClearingCancelled INTEGER NOT NULL DEFAULT 0"):
                            con.execute(f"ALTER TABLE FinancialJournalEntries ADD COLUMN {spalte}")
                        con.execute("UPDATE FinancialJournalEntries SET DueDate='2026-10-15 00:00:00',"
                                    " ClearingDate='2026-10-20 00:00:00',"
                                    " ReconciliationDate='2026-10-21 00:00:00',"
                                    " ClearingReference='4711', ClearingCount=3,"
                                    " IsClearingCancelled=1"
                                    " WHERE JournalEntryId='2'")
                con.close()
                subprocess.run([sys.executable, str(Path(__file__).with_name("finance_all_xlsx.py")),
                                "--db", str(db), "--ziel", str(target), "--seit", "2026-09-01"],
                               check=True, capture_output=True, text=True)
                wb = load_workbook(target, read_only=True, data_only=True)
                detail = list(wb["Journal Detail"].values)
                self.assertEqual(2, len(detail))
                self.assertEqual(["due date", "clearing date", "recon date", "clearing ref",
                                  "Ausgleiche", "clearing storniert", "Konzernkonto"],
                                 list(detail[0][10:17]))
                self.assertEqual("2026-10-15" if with_due_date else None, detail[1][10])
                self.assertEqual("2026-10-20" if with_due_date else None, detail[1][11])
                self.assertEqual("2026-10-21" if with_due_date else None, detail[1][12])
                self.assertEqual("4711" if with_due_date else None, detail[1][13])
                self.assertEqual(3 if with_due_date else None, detail[1][14])
                self.assertEqual("ja" if with_due_date else None, detail[1][15])
                self.assertEqual("2026-09-01", detail[1][7])
                self.assertIsNone(detail[1][16])  # kein geratenes Konzernkonto
                status = {r[1]: r for r in list(wb["Feldstatus"].values)[1:]}
                self.assertEqual((2, 1 if with_due_date else 0), status["due date"][2:4])
                self.assertEqual((2, 1 if with_due_date else 0), status["clearing date"][2:4])
                self.assertEqual((2, 1 if with_due_date else 0), status["recon date"][2:4])
                self.assertEqual((2, 1 if with_due_date else 0), status["clearing ref"][2:4])
                self.assertEqual((2, 1 if with_due_date else 0), status["Ausgleiche"][2:4])
                self.assertEqual((2, 1 if with_due_date else 0), status["clearing storniert"][2:4])
                self.assertEqual((2, 1, 1), status["cost center"][2:5])
                self.assertEqual((2, 0, 2), status["Konzernkonto"][2:5])
                self.assertEqual(3, len(list(wb["Journal Summary"].values)))
                wb.close()


if __name__ == "__main__":
    unittest.main()
