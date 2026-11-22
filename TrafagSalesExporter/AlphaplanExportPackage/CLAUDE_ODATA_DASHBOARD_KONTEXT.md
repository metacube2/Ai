# OData-/Produktsparten-Kontext fuer Claude

Diese Notiz beschreibt den realen Stand im Projekt `TrafagSalesExporter` / `BiDashboard`. Bitte nicht generisch daneben bauen, sondern in diese bestehende Architektur einpassen.

## 1. OData-Anbindung

DI-Registrierung in `Program.cs`:

```csharp
builder.Services.AddSingleton<ISapGatewayService, SapGatewayService>();
builder.Services.AddSingleton<IMappedSalesRecordComposer, MappedSalesRecordComposer>();
builder.Services.AddSingleton<ISapCompositionService, SapCompositionService>();
builder.Services.AddSingleton<IDataSourceAdapter, SapGatewayDataSourceAdapter>();
```

Relevante Klassen:

- `Services/DataSources/SapGatewayDataSourceAdapter.cs`
- `Services/SapCompositionService.cs`
- `Services/SapGatewayService.cs`
- `Services/MappedSalesRecordComposer.cs`

Ladekette:

```text
SiteExportService
  -> IDataSourceAdapterResolver
  -> SapGatewayDataSourceAdapter
  -> SapCompositionService
  -> SapGatewayService.GetEntityRowsAsync(...)
  -> MappedSalesRecordComposer.Compose(...)
```

OData-Read in `SapGatewayService.GetEntityRowsAsync(...)`:

```csharp
var query = string.IsNullOrWhiteSpace(filter)
    ? "$format=json"
    : $"$format=json&$filter={Uri.EscapeDataString(filter)}";

var requestUrl = $"{BuildServiceUri(serviceUrl)}{entitySet}?{query}";
using var response = await client.GetAsync(requestUrl, cancellationToken);
```

Authentifizierung:

- Basic Auth.
- Direkter `HttpClient`.
- Keine OData-Library wie `Simple.OData.Client` oder `Microsoft.OData`.

Auth-Code in `SapGatewayService.CreateClient(...)`:

```csharp
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
    "Basic",
    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
```

Credentials und Service-URL kommen aus SQLite-Konfiguration:

- zentral in `SourceSystemDefinition`
  - `CentralServiceUrl`
  - `CentralUsername`
  - `CentralPassword`
- pro Standort in `Site`
  - `SapServiceUrl`
  - `UsernameOverride`
  - `PasswordOverride`

Aufloesung in `Services/DataSources/DataSourceCredentials.cs`:

```csharp
Resolve(site, sourceDefinition)
ResolveSapServiceUrl(site, sourceDefinition)
```

## 2. Datenmodell Umsatzzeilen

Importmodell: `Models/SalesRecord.cs`.

Wichtige Properties:

```csharp
public string Material { get; set; } = string.Empty;
public string ProductHierarchyCode { get; set; } = string.Empty;
public string ProductHierarchyText { get; set; } = string.Empty;
public string ProductFamilyCode { get; set; } = string.Empty;
public string ProductFamilyText { get; set; } = string.Empty;
public string ProductDivisionCode { get; set; } = string.Empty;
public string ProductDivisionText { get; set; } = string.Empty;
public string ProductMappingAssigned { get; set; } = string.Empty;
```

Persistenzmodell: `Models/CentralSalesRecord.cs`.

Es hat dieselben Produktfelder:

```csharp
public string Material { get; set; } = string.Empty;
public string ProductHierarchyCode { get; set; } = string.Empty;
public string ProductHierarchyText { get; set; } = string.Empty;
public string ProductFamilyCode { get; set; } = string.Empty;
public string ProductFamilyText { get; set; } = string.Empty;
public string ProductDivisionCode { get; set; } = string.Empty;
public string ProductDivisionText { get; set; } = string.Empty;
public string ProductMappingAssigned { get; set; } = string.Empty;
```

Materialnummer:

- Feldname im Dashboard: `Material`.
- Typ: `string`.
- Beim OData-Import-Join wird aktuell nur `Trim()` gemacht.
- Fuehrende Nullen werden im Import-Join aktuell nicht entfernt.
- In der Management-Analyse wird spaeter normalisiert.

Normalisierung in `ManagementCockpitService.NormalizeMaterialKey(...)`:

```csharp
var normalized = new string(value
    .Trim()
    .ToUpperInvariant()
    .Where(ch => !char.IsWhiteSpace(ch))
    .ToArray());

var withoutLeadingZeros = normalized.TrimStart('0');
return string.IsNullOrWhiteSpace(withoutLeadingZeros) ? "0" : withoutLeadingZeros;
```

Wichtig: Wenn ein neuer OData-Service `Matnr` 18-stellig mit fuehrenden Nullen liefert, die Umsatzquelle aber ohne fuehrende Nullen kommt, kann der Import-Join aktuell scheitern. Die spaetere Analyse normalisiert, der Import-Join noch nicht.

## 3. Bestehende Produktsparten-Mapping-Logik

Die alte Logik existiert noch und ist aktiv.

Aktuelle SAP-Quellen fuer `ZSCHWEIZ` im Seed (`DatabaseSeedService`):

- Alias `P`: `ProductDivisionRefSet`
- Alias `M`: `ProductDivisionMapSet`

Aktuelle Joins:

```text
Z.Matnr = P.Matnr
Z.Prodh = M.Paph1
```

Aktuelle Feldmappings:

```csharp
ProductHierarchyCode = FirstNonEmpty(P.Paph1, M.Paph1)
ProductHierarchyText = FirstNonEmpty(P.Paph1Text, M.Paph1Text)
ProductFamilyCode = FirstNonEmpty(P.Wwpfa, M.Wwpfa)
ProductFamilyText = FirstNonEmpty(P.WwpfaText, M.WwpfaText)
ProductDivisionCode = FirstNonEmpty(P.Wwpsp, M.Wwpsp)
ProductDivisionText = FirstNonEmpty(P.WwpspText, M.WwpspText)
ProductMappingAssigned = FirstNonEmpty(P.IsAssigned, M.IsAssigned)
```

Join-Engine in `MappedSalesRecordComposer.ApplyLeftJoin(...)`:

```csharp
var rightLookup = rightRows
    .GroupBy(r => BuildKey(r, rightKeyParts))
    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
```

Das ist bereits ein In-Memory-Lookup pro rechter Quelle. Es wird nicht pro Umsatzzeile ein OData-Call gemacht.

## 4. Anzeige und Aggregation

Service:

- `Services/ManagementCockpitService.cs`

UI:

- `Components/Pages/ManagementCockpit.razor`

Aktuelle Statuswerte im Dashboard:

```csharp
Assigned = "Zugeordnet";
Unassigned = "Nicht zugeordnet";
NoReference = "Nicht im TR-AG-Stamm";
MissingMaterial = "Material fehlt";
```

Aktuelle Statusableitung in `ManagementCockpitService.BuildProductAssignmentStatus(...)`:

```csharp
if (string.IsNullOrWhiteSpace(material))
    return ProductAssignmentStatuses.MissingMaterial;
if (reference is null)
    return ProductAssignmentStatuses.NoReference;
return IsAssignedProductReference(reference)
    ? ProductAssignmentStatuses.Assigned
    : ProductAssignmentStatuses.Unassigned;
```

Aktuelle Assigned-Pruefung:

```csharp
private static bool IsAssignedProductReference(FinanceAggregationRow row)
    => IsTruthy(row.ProductMappingAssigned) &&
       !string.IsNullOrWhiteSpace(row.ProductDivisionCode) &&
       !string.Equals(row.ProductDivisionCode, "UNASS", StringComparison.OrdinalIgnoreCase);
```

Wichtig:

- `Uebrige` / `Übrige` gibt es aktuell nicht als eigenen Status.
- Ein Material ohne Referenztreffer wird aktuell separat als `Nicht im TR-AG-Stamm` ausgewiesen.
- Das soll nicht automatisch mit `Nicht zugeordnet` zusammengeworfen werden.

## 5. Refresh- und Lade-Mechanismus

Es gibt keinen globalen Product-Dictionary-Cache beim App-Start.

OData-Quellen werden beim Standortexport/Refresh geladen:

```text
SiteExportService.ExportAsync(...)
  -> adapter.FetchAsync(...)
  -> SapCompositionService.BuildSalesRecordsAsync(...)
```

`SapCompositionService` liest alle aktiven SAP-Quellen einmal pro Lauf:

```csharp
foreach (var source in activeSources)
{
    var rows = await _sapGatewayService.GetEntityRowsAsync(
        site.SapServiceUrl,
        source.EntitySet,
        username,
        password,
        filter,
        cancellationToken);

    sourceRows[source.Alias] = rows;
}
```

Danach macht `MappedSalesRecordComposer` den Join im Speicher.

Ergebnisse werden in `CentralSalesRecords` ersetzt:

- `Services/CentralSalesRecordService.cs`

Dashboard liest zentral aus:

- `CentralSalesRecords`, oder
- optional aus Audit-CSV, wenn `UseAuditCsvAsCentralSource` aktiv ist.

Provider:

- `Services/CentralSalesDataProvider.cs`

## 6. Wichtige Einordnung fuer die Umsetzung

Nicht generisch neu bauen. Bestehendes Pattern ist:

```text
SapSourceDefinition
SapJoinDefinition
SapFieldMapping
MappedSalesRecordComposer
```

Wenn ein neuer Service kommt, muss fachlich/technisch entschieden werden:

1. Ersetzt er `ProductDivisionRefSet`?
2. Oder wird er als neue Quelle/Alias ergaenzt?
3. Bleibt `ProductDivisionMapSet` als PAPH1-Fallback aktiv?
4. Muss der Import-Join fuer `Matnr` fuehrende Nullen normalisieren?
5. Wie genau wird `Übrige` geliefert?
   - ueber `Wwpsp`
   - ueber `WwpspText`
   - ueber `IsAssigned`
   - oder ueber eine Kombination

Nicht ungeprueft zusammenwerfen:

- `Nicht zugeordnet`: Referenztreffer vorhanden, aber SAP konnte keine Sparte ableiten.
- `Nicht im TR-AG-Stamm`: Material hat gar keinen Referenztreffer.
- `Übrige`: soll vermutlich eigene gueltige Kategorie werden, ist aktuell aber noch kein separater Dashboard-Status.

