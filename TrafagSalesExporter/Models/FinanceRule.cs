namespace TrafagSalesExporter.Models;

public class FinanceRule
{
    public int Id { get; set; }
    public string ScopeKey { get; set; } = string.Empty;
    public int? Year { get; set; }
    public string RuleType { get; set; } = FinanceRuleTypes.Exclude;
    public string FieldName { get; set; } = string.Empty;
    public string MatchType { get; set; } = FinanceRuleMatchTypes.Contains;
    public string MatchValue { get; set; } = string.Empty;
    public decimal? NumericValue { get; set; }
    public string Notes { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public static class FinanceRuleTypes
{
    public const string Exclude = "Exclude";
    public const string NegateAmount = "NegateAmount";
    public const string ForceYear = "ForceYear";
    public const string DeduplicateBlankSupplierCountry = "DeduplicateBlankSupplierCountry";

    /// <summary>
    /// Periodenzuordnung ueber das Rechnungsdatum statt ueber das Buchungsdatum. Fachentscheid
    /// Andreas Stoller vom 2026-08-26 zu ISS-004.2, ausdruecklich NUR fuer Spanien: dort ist
    /// `PostingDate` erst zu 21 % gefuellt, `InvoiceDate` zu 97 %. Bewusst als Regel und nicht
    /// hart im Code, damit der Geltungsbereich ohne Deploy aenderbar bleibt.
    /// </summary>
    public const string UseInvoiceDate = "UseInvoiceDate";

    public static readonly string[] All =
    [
        Exclude,
        NegateAmount,
        ForceYear,
        DeduplicateBlankSupplierCountry,
        UseInvoiceDate
    ];
}

public static class FinanceRuleMatchTypes
{
    public const string Always = "Always";
    public const string Equal = "Equals";
    public const string Contains = "Contains";
    public const string StartsWith = "StartsWith";
    public const string IsBlank = "IsBlank";

    public static readonly string[] All =
    [
        Always,
        Equal,
        Contains,
        StartsWith,
        IsBlank
    ];
}
