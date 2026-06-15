using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;
using TrafagSalesExporter.Services.DataSources;

namespace TrafagSalesExporter.Tests;

public class ManualExcelDataSourceAdapterTests
{
    [Fact]
    public async Task FetchAsync_Uses_Local_File_Directory_As_OutputDirectory()
    {
        var filePath = CreateSpainCsv();
        try
        {
            var adapter = new ManualExcelDataSourceAdapter(
                new FakeSharePointUploadService(filePath),
                new ManualExcelImportService(),
                new NoopAppEventLogService());

            var result = await adapter.FetchAsync(CreateContext(filePath));

            Assert.Single(result.Records);
            Assert.Null(result.ReferenceFilePath);
            Assert.Equal(Path.GetDirectoryName(Path.GetFullPath(filePath)), result.LocalOutputDirectoryOverride);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task FetchAsync_Uses_SharePoint_Source_Folder_As_UploadFolder()
    {
        var filePath = CreateSpainCsv();
        try
        {
            var adapter = new ManualExcelDataSourceAdapter(
                new FakeSharePointUploadService(filePath),
                new ManualExcelImportService(),
                new NoopAppEventLogService());

            var result = await adapter.FetchAsync(CreateContext("https://trafagag.sharepoint.com/sites/WorldwideBIPlatform/Import/Finance/Spanien/Spain_Sales_2025.csv"));

            Assert.Single(result.Records);
            Assert.Null(result.ReferenceFilePath);
            Assert.Equal("Import/Finance/Spanien", result.SharePointUploadFolderOverride);
            Assert.Equal(string.Empty, result.SharePointUploadLandOverride);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task FetchAsync_Uses_Latest_SharePoint_File_When_Path_Is_Folder()
    {
        var filePath = CreateSpainCsv();
        var sharePointService = new FakeSharePointUploadService(
            filePath,
            latestFileReference: "Import/Finance/UK_B1/010526_TRUK.xlsx");
        try
        {
            var adapter = new ManualExcelDataSourceAdapter(
                sharePointService,
                new ManualExcelImportService(),
                new NoopAppEventLogService());

            var result = await adapter.FetchAsync(CreateContext("https://trafagag.sharepoint.com/sites/WorldwideBIPlatform/Import/Finance/UK_B1", "TRUK", "England"));

            Assert.Single(result.Records);
            Assert.Equal("Import/Finance/UK_B1", result.SharePointUploadFolderOverride);
            Assert.Equal("Import/Finance/UK_B1/010526_TRUK.xlsx", sharePointService.LastDownloadedReference);
            Assert.Equal("TRUK", sharePointService.LastResolvedTsc);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task FetchAsync_Reads_Local_Spain_Folder_And_Deduplicates_DeltaRows()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            WriteSpainCsv(Path.Combine(folder, "Spain_Sales_2025.csv"),
                ("line-a", "1001", 10, 100m));
            WriteSpainCsv(Path.Combine(folder, "Spain_Sales_range_20260528_to_20260603.csv"),
                ("line-a", "1001", 10, 125m),
                ("line-b", "1002", 20, 50m));

            var adapter = new ManualExcelDataSourceAdapter(
                new FakeSharePointUploadService(Path.Combine(folder, "Spain_Sales_2025.csv")),
                new ManualExcelImportService(),
                new NoopAppEventLogService());

            var result = await adapter.FetchAsync(CreateContext(folder));

            Assert.Equal(2, result.Records.Count);
            Assert.Equal(125m, Assert.Single(result.Records, r => r.SourceLineId == "line-a").SalesPriceValue);
            Assert.Equal(50m, Assert.Single(result.Records, r => r.SourceLineId == "line-b").SalesPriceValue);
            Assert.Equal(folder, result.LocalOutputDirectoryOverride);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task FetchAsync_Reads_Local_Alphaplan_Full_And_Delta_Folder()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var deltaFolder = Path.Combine(folder, "delta");
        Directory.CreateDirectory(deltaFolder);
        try
        {
            WriteAlphaplanPair(folder, "RE2610696", 401613, 1464626, 100m, new DateTime(2026, 6, 8));
            WriteAlphaplanPair(deltaFolder, "RE2610696", 401613, 1464626, 125m, new DateTime(2026, 6, 9));
            WriteAlphaplanPair(deltaFolder, "RE2610697", 401614, 1464627, 50m, new DateTime(2026, 6, 9), append: true);

            var adapter = new ManualExcelDataSourceAdapter(
                new FakeSharePointUploadService(Path.Combine(folder, "invoice_lines.csv")),
                new ManualExcelImportService(),
                new NoopAppEventLogService());

            var result = await adapter.FetchAsync(CreateContext(folder, "TRDE", "Deutschland"));

            Assert.Equal(2, result.Records.Count);
            Assert.Equal(125m, Assert.Single(result.Records, r => r.InvoiceNumber == "RE2610696").SalesPriceValue);
            Assert.Equal(50m, Assert.Single(result.Records, r => r.InvoiceNumber == "RE2610697").SalesPriceValue);
            Assert.Equal(folder, result.LocalOutputDirectoryOverride);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static DataSourceFetchContext CreateContext(string manualImportPath, string tsc = "TRES", string land = "Spanien") => new()
    {
        Site = new Site
        {
            Id = 7,
            TSC = tsc,
            Land = land,
            ManualImportFilePath = manualImportPath
        },
        SourceDefinition = new SourceSystemDefinition
        {
            Code = "MANUAL_EXCEL",
            ConnectionKind = SourceSystemConnectionKinds.ManualExcel
        },
        Settings = new ExportSettings(),
        SharePointConfig = new SharePointConfig
        {
            TenantId = "tenant",
            ClientId = "client",
            ClientSecret = "secret",
            SiteUrl = "https://trafagag.sharepoint.com/sites/WorldwideBIPlatform"
        }
    };

    private static string CreateSpainCsv()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
        WriteSpainCsv(filePath, ("line-a", "20241332", 20, 265m));
        return filePath;
    }

    private static void WriteSpainCsv(string filePath, params (string SourceLineId, string InvoiceNumber, int Position, decimal SalesPriceValue)[] rows)
    {
        var csv = string.Join(Environment.NewLine,
            new[]
            {
                "\"TSC\";\"Land\";\"SourceLineId\";\"InvoiceNumber\";\"PositionOnInvoice\";\"Material\";\"Name\";\"ProductGroup\";\"Quantity\";\"CustomerNumber\";\"CustomerName\";\"CustomerCountry\";\"StandardCost\";\"StandardCostCurrency\";\"PurchaseOrderNumber\";\"SalesPriceValue\";\"SalesCurrency\";\"DocumentCurrency\";\"CompanyCurrency\";\"Incoterms2020\";\"SalesResponsibleEmployee\";\"InvoiceDate\";\"DocumentType\""
            }.Concat(rows.Select(row =>
                $"\"TRES\";\"Spanien\";\"{row.SourceLineId}\";\"{row.InvoiceNumber}\";\"{row.Position}\";\"52871\";\"ECL1.0AP\";\"TRANS\";\"1.000000\";\"302208\";\"INTRONIK AUTOMATIZACION E INST. SL\";\"ESPANA\";\"160.760000\";\"EUR\";\"PC240330\";\"{row.SalesPriceValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}\";\"EUR\";\"EUR\";\"EUR\";\"EXW\";\"1\";\"2025-01-02 00:00:00\";\"Invoice\"")));
        File.WriteAllText(filePath, csv);
    }

    private static void WriteAlphaplanPair(
        string folder,
        string invoiceNumber,
        int documentEntry,
        int positionId,
        decimal value,
        DateTime date,
        bool append = false)
    {
        var headerPath = Path.Combine(folder, "invoice_headers.csv");
        var linePath = Path.Combine(folder, "invoice_lines.csv");
        var dateText = date.ToString("yyyy-MM-dd HH:mm:ss.000", System.Globalization.CultureInfo.InvariantCulture);
        var headerLine = $"Invoice;{documentEntry};5;{invoiceNumber};{dateText};419;4;13;{value.ToString(System.Globalization.CultureInfo.InvariantCulture)};{(value * 1.19m).ToString(System.Globalization.CultureInfo.InvariantCulture)};0;0;;PO-{invoiceNumber};;;;";
        var line = $"Invoice;{invoiceNumber};{dateText};{documentEntry};{positionId};10;0;324;MAT-{invoiceNumber};;Alphaplan Material;1.0;1.0;{value.ToString(System.Globalization.CultureInfo.InvariantCulture)};{value.ToString(System.Globalization.CultureInfo.InvariantCulture)};{value.ToString(System.Globalization.CultureInfo.InvariantCulture)};{(value * 1.19m).ToString(System.Globalization.CultureInfo.InvariantCulture)};19.0;{(value * 0.19m).ToString(System.Globalization.CultureInfo.InvariantCulture)};0;;;{dateText};0";

        if (!append || !File.Exists(headerPath))
        {
            File.WriteAllText(headerPath, string.Join(Environment.NewLine,
                "DocumentType;BelegeID;BelegTyp;Belegnummer;Datum;RechnungsAdressenID;WaehrungenID;ZahlungsBedingungenID;NettoPreisEndSumme;BruttoPreisEndSumme;IstStorniert;IstArchiviert;ExterneBelegNummer;BestellNummer;IhrAuftrag;KostenStelle;KostenTraeger;UUID",
                headerLine));
            File.WriteAllText(linePath, string.Join(Environment.NewLine,
                "DocumentType;Belegnummer;BelegDatum;BelegeID;BelegePositionenID;ZeilenPosition;PositionsTyp;ArtikelID;ArtikelNummer;KundenArtikelNummer;ArtikelBezeichnung;BEAnzahl;PEAnzahl;PENettoPreis;NettoPreisEinzel;NettoPreisGesamt;BruttoPreisGesamt;MehrwertSteuerSatz;MehrwertSteuer;RohertragGesamt;KostenStelle;KostenTraeger;LieferDatum;NichtDrucken",
                line));
            return;
        }

        File.AppendAllText(headerPath, Environment.NewLine + headerLine);
        File.AppendAllText(linePath, Environment.NewLine + line);
    }

    private sealed class FakeSharePointUploadService : ISharePointUploadService
    {
        private readonly string _sourceFilePath;
        private readonly string _latestFileReference;

        public FakeSharePointUploadService(string sourceFilePath, string? latestFileReference = null)
        {
            _sourceFilePath = sourceFilePath;
            _latestFileReference = latestFileReference ?? "Import/Finance/Spanien/Spain_Sales_2025.csv";
        }

        public string LastDownloadedReference { get; private set; } = string.Empty;

        public string LastResolvedTsc { get; private set; } = string.Empty;

        public Task UploadAsync(string tenantId, string clientId, string clientSecret, string siteUrl, string exportFolder, string land, string localFilePath, bool uploadTimestampedCopyIfLocked = false)
            => Task.CompletedTask;

        public Task<string> DownloadToTempFileAsync(string tenantId, string clientId, string clientSecret, string siteUrl, string fileReference)
        {
            LastDownloadedReference = fileReference;
            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
            File.Copy(_sourceFilePath, tempPath);
            return Task.FromResult(tempPath);
        }

        public Task<SharePointFileReference> ResolveLatestFileInFolderAsync(string tenantId, string clientId, string clientSecret, string siteUrl, string folderReference, string siteTsc, int? preferredYear = null)
        {
            LastResolvedTsc = siteTsc;
            return Task.FromResult(new SharePointFileReference(_latestFileReference, new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero)));
        }

        public Task<IReadOnlyList<SharePointFileReference>> ResolveManualImportFilesInFolderAsync(string tenantId, string clientId, string clientSecret, string siteUrl, string folderReference, string siteTsc, int? preferredYear = null)
        {
            LastResolvedTsc = siteTsc;
            IReadOnlyList<SharePointFileReference> result =
            [
                new(_latestFileReference, new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero))
            ];
            return Task.FromResult(result);
        }

        public Task TestConnectionAsync(string tenantId, string clientId, string clientSecret, string siteUrl)
            => Task.CompletedTask;
    }

    private sealed class NoopAppEventLogService : IAppEventLogService
    {
        public Task WriteAsync(string category, string message, string level = "Info", int? siteId = null, string? land = null, string? details = null)
            => Task.CompletedTask;

        public Task WriteDebugAsync(string category, string message, int? siteId = null, string? land = null, string? details = null)
            => Task.CompletedTask;
    }
}
