namespace TrafagSalesExporter.Services;

// Reiter Verkauf (Wunsch Ingo 2026-10-02). Eine Zeile = eine Rechnungsposition nach den Finance-Regeln
// (FinanceRuleEngine: Einschluss, Nettoumsatz, Gutschriften), ohne Konzernkunden, in CHF zum Kurs des Finance-Datums.
// Doku docs/VERKAUF_2026-10-02.md.

public sealed record SalesFact(
    DateOnly Date, string Tsc, string CountryKey, string CustomerKey, string CustomerName, string CustomerCountry,
    string Material, string Article, string Division, decimal ValueChf, decimal Quantity, string InvoiceNumber);

public sealed class SalesDataset
{
    public DateTime LoadedAt { get; init; }
    public IReadOnlyList<SalesFact> Facts { get; init; } = [];
    /// <summary>Erster Tag nach dem letzten vollstaendigen Monat; alle Zeitfenster enden hier.</summary>
    public DateOnly ReferenceEnd { get; init; }
    /// <summary>Erster Monat, ab dem alle Gesellschaften Daten haben (spaetester Datenbeginn der Gesellschaften).</summary>
    public DateOnly DataStart { get; init; }
    /// <summary>Laenge des Vorjahresvergleichs in Monaten: hoechstens 12, so dass auch der Vorjahreszeitraum ganz in den Daten liegt.</summary>
    public int CompareMonths { get; init; } = 12;
    public int MissingRateRows { get; init; }
    public int IntercompanyRowsExcluded { get; init; }
    public string? Error { get; init; }
}

public sealed record SalesCustomerSummary(
    string Key, string Name, string MainCountry, IReadOnlyList<string> Companies,
    decimal Last12, decimal Current, decimal Previous, decimal Last6, DateOnly FirstDate, DateOnly LastDate, int Invoices,
    IReadOnlyList<(string Division, decimal Value)> Divisions)
{
    /// <summary>Veraenderung des Vergleichszeitraums (Current) gegen denselben Zeitraum ein Jahr frueher (Previous).</summary>
    public decimal? ChangePercent => Previous == 0 ? null : (Current - Previous) / Previous * 100m;
}

public sealed record SalesDeclineItem(SalesCustomerSummary Customer, string Kind, decimal LostChf);

public sealed record SalesQuarterMovement(string Quarter, int NewCustomers, decimal NewRevenue12, int LostCustomers, decimal LostRevenue12, bool LostFinal, bool NewReliable,
    IReadOnlyList<string> NewNames, IReadOnlyList<string> LostNames);

public sealed record SalesConcentrationResult(IReadOnlyList<(int Rank, double CumulativeShare)> Curve, double Top1, double Top5, double Top10, double Top20,
    int CustomersFor80, double Hhi, decimal Total, int Customers, IReadOnlyList<(string Tsc, double Top10Share, int Customers)> PerCompany);

public sealed record SalesCrossSellRule(string From, string To, int Both, int FromCustomers, double Confidence, double Lift);

public sealed record SalesCrossSellOpportunity(string CustomerKey, string CustomerName, string Division, string BecauseOf, double Confidence, decimal CustomerRevenue, decimal TypicalRevenue);

public sealed record SalesPricePoint(string CustomerName, string Country, string Tsc, decimal Quantity, decimal UnitPriceChf);

public sealed record SalesPriceSpread(string Material, string Article, int Customers, decimal Quantity, decimal Revenue,
    decimal P10, decimal Median, decimal P90, decimal Min, decimal Max, IReadOnlyList<SalesPricePoint> Points)
{
    public decimal Ratio => P10 <= 0 ? 0 : P90 / P10;
}

public sealed record SalesMonthValue(DateOnly Month, decimal Value);

public sealed record SalesForecast(string Dimension, IReadOnlyList<SalesMonthValue> History, IReadOnlyList<SalesMonthValue> Forecast,
    IReadOnlyList<double> SeasonIndex, double? BacktestMape, decimal Last12, decimal Previous12);

public sealed record SalesCountryValue(string Country, decimal Last12, int Customers, IReadOnlyList<decimal> Monthly);

public sealed record SalesFlow(string FromCountry, string ToCountry, decimal Last12);
