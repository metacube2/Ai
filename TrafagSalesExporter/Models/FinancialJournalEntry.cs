namespace TrafagSalesExporter.Models;

/// <summary>
/// Eine Hauptbuch-Buchungszeile aus SAP B1 (OJDT/JDT1), getrennt von den
/// Verkaufszeilen in CentralSalesRecords. Feldumfang folgt der Finance-Prioliste
/// fuer den ersten B1-Load (Konsolidierung/Analysen), siehe
/// docs/FINANCE_B1_JOURNAL_IMPORT_2026-07-14.md.
/// </summary>
public class FinancialJournalEntry
{
    public int Id { get; set; }
    public DateTime StoredAtUtc { get; set; }

    /// <summary>Extraktionszeitpunkt aus der Quelle.</summary>
    public DateTime ExtractionDate { get; set; }

    /// <summary>Gesellschaft (TSC, z. B. TRFR).</summary>
    public string Tsc { get; set; } = string.Empty;
    public string Land { get; set; } = string.Empty;

    /// <summary>B1-Datenbankschema der Gesellschaft (z. B. fr01_p); leer bei SAP-OData-Quellen.</summary>
    public string CompanySchema { get; set; } = string.Empty;

    /// <summary>
    /// Buchungskreis / Company Code (SAP ECC `BKPF-BUKRS`). Trennt bei ZSCHWEIZ die
    /// Schweizer und die oesterreichische Gesellschaft; leer bei B1-Quellen (dort ist
    /// die Gesellschaft ueber Tsc/CompanySchema eindeutig).
    /// </summary>
    public string CompanyCode { get; set; } = string.Empty;

    /// <summary>Quellsystem-Code (BI1/SAGE fuer B1-HANA, SAP fuer OData/ECC).</summary>
    public string SourceSystem { get; set; } = string.Empty;

    /// <summary>
    /// Journal Entry ID. B1: OJDT.TransId. SAP ECC: `BUKRS/GJAHR/BELNR`, weil die
    /// Belegnummer erst zusammen mit Buchungskreis und Geschaeftsjahr eindeutig ist.
    /// </summary>
    public string JournalEntryId { get; set; } = string.Empty;

    /// <summary>Journal Entry Line ID (B1: JDT1.Line_ID; SAP: BSEG-BUZEI).</summary>
    public int JournalEntryLineId { get; set; }

    /// <summary>Buchungsdatum (B1: OJDT.RefDate; SAP: BKPF-BUDAT).</summary>
    public DateTime? PostingDate { get; set; }

    /// <summary>Geschaeftsjahr; B1 = Kalenderjahr des Buchungsdatums, SAP = BKPF-GJAHR.</summary>
    public int FiscalYear { get; set; }

    /// <summary>Buchungsperiode; B1 = Monat des Buchungsdatums, SAP = BKPF-MONAT.</summary>
    public int FiscalPeriod { get; set; }

    /// <summary>Lokales Sachkonto (JDT1.Account).</summary>
    public string AccountCode { get; set; } = string.Empty;

    /// <summary>Kontobezeichnung (OACT.AcctName).</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Sollbetrag in lokaler Waehrung (JDT1.Debit).</summary>
    public decimal DebitAmount { get; set; }

    /// <summary>Habenbetrag in lokaler Waehrung (JDT1.Credit).</summary>
    public decimal CreditAmount { get; set; }

    /// <summary>
    /// Betrag mit Vorzeichen in lokaler Waehrung: Soll positiv, Haben negativ
    /// (= Debit - Credit). Entspricht zugleich "Betrag in lokaler Waehrung".
    /// </summary>
    public decimal SignedAmountLocal { get; set; }

    /// <summary>Lokale Waehrung der Gesellschaft (OADM.MainCurncy).</summary>
    public string LocalCurrency { get; set; } = string.Empty;

    /// <summary>Transaktionswaehrung (JDT1.FCCurrency, leer bei reinen LC-Buchungen).</summary>
    public string TransactionCurrency { get; set; } = string.Empty;

    /// <summary>Betrag mit Vorzeichen in Transaktionswaehrung (FCDebit - FCCredit).</summary>
    public decimal SignedAmountTransaction { get; set; }

    /// <summary>Kostenstelle / Dimension 1 (JDT1.ProfitCode).</summary>
    public string CostCenter { get; set; } = string.Empty;

    /// <summary>Weitere Hauptdimension (JDT1.OcrCode2, z. B. Profitcenter/Business Unit).</summary>
    public string Dimension2 { get; set; } = string.Empty;

    /// <summary>Buchungstext / Line Memo (JDT1.LineMemo).</summary>
    public string LineMemo { get; set; } = string.Empty;

    /// <summary>Belegart / Source Transaction Type (OJDT.TransType, B1-ObjType als Text, z. B. 13=AR-Rechnung, 30=manuelle Buchung).</summary>
    public string TransactionType { get; set; } = string.Empty;

    /// <summary>Quelldokumentnummer fuer Drill-down (OJDT.BaseRef).</summary>
    public string SourceDocumentNumber { get; set; } = string.Empty;

    /// <summary>Manuell erzeugte Buchung (TransType 30) vs. automatisch aus Belegen.</summary>
    public bool IsManual { get; set; }

    /// <summary>Storno-/Reversal-Kennzeichen (OJDT.StornoToTr gesetzt oder AutoStorno = Y).</summary>
    public bool IsReversal { get; set; }
}
