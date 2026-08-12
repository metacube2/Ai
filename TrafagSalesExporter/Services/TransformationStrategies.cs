using System.Globalization;
using System.Text;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public sealed class CopyTransformationStrategy : ITransformationStrategy
{
    public string TransformationType => "Copy";
    public string Description => "Kopiert Source nach Target.";
    public object? Transform(object? sourceValue, string? argument) => sourceValue;
}

public sealed class UppercaseTransformationStrategy : ITransformationStrategy
{
    public string TransformationType => "Uppercase";
    public string Description => "Wandelt Text in Grossbuchstaben.";
    public object? Transform(object? sourceValue, string? argument) => sourceValue?.ToString()?.ToUpperInvariant();
}

public sealed class LowercaseTransformationStrategy : ITransformationStrategy
{
    public string TransformationType => "Lowercase";
    public string Description => "Wandelt Text in Kleinbuchstaben.";
    public object? Transform(object? sourceValue, string? argument) => sourceValue?.ToString()?.ToLowerInvariant();
}

public sealed class PrefixTransformationStrategy : ITransformationStrategy
{
    public string TransformationType => "Prefix";
    public string Description => "Stellt Argument vor den Source-Wert.";
    public object? Transform(object? sourceValue, string? argument) => $"{argument}{sourceValue}";
}

public sealed class SuffixTransformationStrategy : ITransformationStrategy
{
    public string TransformationType => "Suffix";
    public string Description => "Haengt Argument an den Source-Wert.";
    public object? Transform(object? sourceValue, string? argument) => $"{sourceValue}{argument}";
}

public sealed class ReplaceTransformationStrategy : ITransformationStrategy
{
    public string TransformationType => "Replace";
    public string Description => "Ersetzt in Text mit Syntax alt=>neu.";

    public object? Transform(object? sourceValue, string? argument)
    {
        var input = sourceValue?.ToString();
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        if (string.IsNullOrWhiteSpace(argument))
            return input;

        var parts = argument.Split("=>", 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            return input;

        return input.Replace(parts[0], parts[1], StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class ConstantTransformationStrategy : ITransformationStrategy
{
    public string TransformationType => "Constant";
    public string Description => "Setzt das Target auf einen konstanten Wert aus Argument.";
    public object? Transform(object? sourceValue, string? argument) => argument;
}

public sealed class NormalizeCurrencyCodeTransformationStrategy : ITransformationStrategy
{
    private static readonly Dictionary<string, string> BuiltInAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["$"] = "USD",
        ["US$"] = "USD",
        ["USD"] = "USD",
        ["€"] = "EUR",
        ["EUR"] = "EUR",
        ["CHF"] = "CHF",
        ["SFR"] = "CHF",
        ["INR"] = "INR",
        ["RS"] = "INR",
        ["GBP"] = "GBP",
        ["CAD"] = "CAD"
    };

    public string TransformationType => "NormalizeCurrencyCode";
    public string Description => "Normalisiert Waehrungscodes wie $, EUR, CHF, INR auf ISO-Codes. Optionale Aliase im Argument mit alt=>neu|alt2=>neu2.";

    public object? Transform(object? sourceValue, string? argument)
    {
        var input = sourceValue?.ToString()?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var aliases = new Dictionary<string, string>(BuiltInAliases, StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in ParseMappings(argument))
            aliases[mapping.Key] = mapping.Value;

        return aliases.TryGetValue(input, out var mapped)
            ? mapped
            : input.ToUpperInvariant();
    }

    private static IEnumerable<KeyValuePair<string, string>> ParseMappings(string? argument)
    {
        if (string.IsNullOrWhiteSpace(argument))
            yield break;

        var mappings = argument.Split(['|', ';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var mapping in mappings)
        {
            var parts = mapping.Split("=>", 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]) && !string.IsNullOrWhiteSpace(parts[1]))
                yield return new KeyValuePair<string, string>(parts[0], parts[1].ToUpperInvariant());
        }
    }
}

/// <summary>
/// Normalisiert Laendernamen auf ISO-3166-Alpha-2-Codes. Anlass: Andreas' Issue-Log
/// 2026-07-28 „Customer Country code is not standardized" — Spanien (TRES) liefert
/// spanische Klartextnamen (`ESPANA`, `BRASIL`, `PERU`, `ALEMANIA`, ...) statt Codes,
/// alle anderen Gesellschaften liefern korrekte 2-Buchstaben-Codes. Befund/Zahlen:
/// docs/FINANCE_ISSUE_LOG_ANDREAS_2026-07-28.md.
///
/// Bewusste Entscheidung: Nicht zuordenbare Werte bleiben UNVERAENDERT stehen (nur
/// getrimmt), statt geraten oder geleert zu werden. So bleibt eine Luecke im Mapping
/// sichtbar und kann ergaenzt werden, anstatt still falsche Codes zu erzeugen.
/// Diakritika werden beim Vergleich ignoriert, damit `PERU`/`PERU` und `ESPANA`/`ESPANA`
/// gleich behandelt werden.
/// </summary>
public sealed class NormalizeCountryCodeTransformationStrategy : ITransformationStrategy
{
    // Schluessel bewusst OHNE Diakritika hinterlegt - der Vergleich strippt sie ebenfalls.
    private static readonly Dictionary<string, string> BuiltInAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        // Spanisch (aus Produktivdaten TRES, Stand 2026-07-27)
        ["ESPANA"] = "ES",
        ["BRASIL"] = "BR",
        ["PORTUGAL"] = "PT",
        ["PERU"] = "PE",
        ["MEXICO"] = "MX",
        ["ARGENTINA"] = "AR",
        ["COLOMBIA"] = "CO",
        ["CHILE"] = "CL",
        ["GUATEMALA"] = "GT",
        ["ECUADOR"] = "EC",
        ["ECUADOR (INC.GALAPAGOS)"] = "EC",
        ["PARAGUAY"] = "PY",
        ["EL SALVADOR"] = "SV",
        ["ALEMANIA"] = "DE",
        ["ESTADOS UNIDOS DE AMERICA"] = "US",
        ["COSTA RICA"] = "CR",
        ["FRANCIA"] = "FR",
        ["REPUBLICA DOMINICANA"] = "DO",
        ["PANAMA"] = "PA",
        ["INDIA"] = "IN",
        ["CHINA"] = "CN",
        ["AUSTRIA"] = "AT",
        ["BOLIVIA"] = "BO",
        ["ITALIA"] = "IT",
        ["SUIZA"] = "CH",
        ["REINO UNIDO"] = "GB",
        // Englisch/Deutsch als Robustheitsreserve, falls andere Quellen Klartext liefern
        ["SPAIN"] = "ES",
        ["SPANIEN"] = "ES",
        ["GERMANY"] = "DE",
        ["DEUTSCHLAND"] = "DE",
        ["SWITZERLAND"] = "CH",
        ["SCHWEIZ"] = "CH",
        ["FRANCE"] = "FR",
        ["FRANKREICH"] = "FR",
        ["ITALY"] = "IT",
        ["ITALIEN"] = "IT",
        ["UNITED KINGDOM"] = "GB",
        ["GREAT BRITAIN"] = "GB",
        ["GROSSBRITANNIEN"] = "GB",
        ["UNITED STATES"] = "US",
        ["UNITED STATES OF AMERICA"] = "US",
        ["USA"] = "US",
        ["OESTERREICH"] = "AT"
    };

    public string TransformationType => "NormalizeCountryCode";

    public string Description => "Normalisiert Laendernamen auf ISO-2-Codes (z.B. ESPANA -> ES, ALEMANIA -> DE). Bereits gueltige 2-Buchstaben-Codes werden nur gross geschrieben; unbekannte Werte bleiben unveraendert. Optionale Aliase im Argument mit alt=>neu|alt2=>neu2.";

    public object? Transform(object? sourceValue, string? argument)
    {
        var input = sourceValue?.ToString()?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var aliases = new Dictionary<string, string>(BuiltInAliases, StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in ParseMappings(argument))
            aliases[StripDiacritics(mapping.Key)] = mapping.Value;

        if (aliases.TryGetValue(StripDiacritics(input), out var mapped))
            return mapped;

        // Schon ein Code? Dann nur normalisieren, nicht anfassen.
        if (input.Length == 2 && input.All(char.IsLetter))
            return input.ToUpperInvariant();

        // Unbekannter Klartext: bewusst unveraendert lassen, damit die Luecke sichtbar bleibt.
        return input;
    }

    private static string StripDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static IEnumerable<KeyValuePair<string, string>> ParseMappings(string? argument)
    {
        if (string.IsNullOrWhiteSpace(argument))
            yield break;

        var mappings = argument.Split(['|', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var mapping in mappings)
        {
            var parts = mapping.Split("=>", 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]) && !string.IsNullOrWhiteSpace(parts[1]))
                yield return new KeyValuePair<string, string>(parts[0], parts[1].ToUpperInvariant());
        }
    }
}

public sealed class FirstNonEmptyRecordTransformationStrategy : IRecordTransformationStrategy
{
    public string TransformationType => "FirstNonEmpty";
    public string Description => "Record-Strategie: setzt Target aus dem ersten nicht-leeren Feld aus Argument, z.B. CustomerName|SupplierName|Name.";

    public void Transform(SalesRecord record, FieldTransformationRule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.TargetField) || string.IsNullOrWhiteSpace(rule.Argument))
            return;

        var propertyMap = RecordTransformationService.PropertyMap;
        if (!propertyMap.TryGetValue(rule.TargetField, out var targetProperty))
            return;

        var sourceFields = rule.Argument
            .Split(['|', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var sourceField in sourceFields)
        {
            if (!propertyMap.TryGetValue(sourceField, out var sourceProperty))
                continue;

            var value = sourceProperty.GetValue(record);
            if (IsMeaningfulValue(value))
            {
                RecordTransformationService.SetPropertyValue(record, targetProperty, value);
                return;
            }
        }
    }

    private static bool IsMeaningfulValue(object? value)
    {
        if (value is null)
            return false;
        if (value is string text)
            return !string.IsNullOrWhiteSpace(text);
        if (value is DateTime date)
            return date != default;
        if (value is decimal decimalNumber)
            return decimalNumber != 0m;
        if (value is int intNumber)
            return intNumber != 0;

        return true;
    }
}

public sealed class ConvertCurrencyRecordTransformationStrategy : IRecordTransformationStrategy
{
    private readonly ICurrencyExchangeRateService _exchangeRateService;

    public ConvertCurrencyRecordTransformationStrategy(ICurrencyExchangeRateService exchangeRateService)
    {
        _exchangeRateService = exchangeRateService;
    }

    public string TransformationType => "ConvertCurrency";
    public string Description => "Record-Strategie: rechnet einen Betrag ueber die Kurstabelle in eine Zielwaehrung um. Argument z.B. amountField=SalesPriceValue;currencyField=SalesCurrency;targetCurrency=EUR;dateField=InvoiceDate;targetCurrencyField=SalesCurrency;round=2";

    public void Transform(SalesRecord record, FieldTransformationRule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.TargetField) || string.IsNullOrWhiteSpace(rule.Argument))
            return;

        var propertyMap = RecordTransformationService.PropertyMap;
        if (!propertyMap.TryGetValue(rule.TargetField, out var targetAmountProperty))
            return;

        var options = ParseOptions(rule.Argument);
        if (!options.TryGetValue("amountField", out var amountField)
            || !options.TryGetValue("currencyField", out var currencyField)
            || !options.TryGetValue("targetCurrency", out var targetCurrency)
            || !propertyMap.TryGetValue(amountField, out var sourceAmountProperty)
            || !propertyMap.TryGetValue(currencyField, out var sourceCurrencyProperty))
            return;

        var sourceAmount = ReadDecimal(record, sourceAmountProperty);
        if (sourceAmount is null)
            return;

        var sourceCurrency = _exchangeRateService.NormalizeCurrencyCode(sourceCurrencyProperty.GetValue(record)?.ToString());
        var normalizedTargetCurrency = _exchangeRateService.NormalizeCurrencyCode(targetCurrency);
        if (string.IsNullOrWhiteSpace(sourceCurrency) || string.IsNullOrWhiteSpace(normalizedTargetCurrency))
            return;

        var effectiveDate = ResolveEffectiveDate(record, options, propertyMap);
        var rate = _exchangeRateService.ResolveRate(sourceCurrency, normalizedTargetCurrency, effectiveDate);
        if (!rate.HasValue)
            return;

        var convertedAmount = sourceAmount.Value * rate.Value;
        if (options.TryGetValue("round", out var roundValue) && int.TryParse(roundValue, out var digits))
            convertedAmount = Math.Round(convertedAmount, digits, MidpointRounding.AwayFromZero);

        RecordTransformationService.SetPropertyValue(record, targetAmountProperty, convertedAmount);

        if (options.TryGetValue("targetCurrencyField", out var targetCurrencyField)
            && propertyMap.TryGetValue(targetCurrencyField, out var targetCurrencyProperty))
        {
            RecordTransformationService.SetPropertyValue(record, targetCurrencyProperty, normalizedTargetCurrency);
        }
    }

    private static Dictionary<string, string> ParseOptions(string argument)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var parts = argument.Split([';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            var pair = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (pair.Length == 2 && !string.IsNullOrWhiteSpace(pair[0]))
                result[pair[0]] = pair[1];
        }

        return result;
    }

    private static decimal? ReadDecimal(SalesRecord record, System.Reflection.PropertyInfo property)
    {
        var value = property.GetValue(record);
        if (value is decimal decimalValue)
            return decimalValue;

        return decimal.TryParse(value?.ToString(), out var parsed)
            ? parsed
            : null;
    }

    private static DateTime? ResolveEffectiveDate(
        SalesRecord record,
        IReadOnlyDictionary<string, string> options,
        IReadOnlyDictionary<string, System.Reflection.PropertyInfo> propertyMap)
    {
        if (options.TryGetValue("dateField", out var dateField)
            && propertyMap.TryGetValue(dateField, out var configuredDateProperty))
        {
            var configuredDate = configuredDateProperty.GetValue(record);
            if (configuredDate is DateTime date)
                return date;
        }

        return record.InvoiceDate ?? record.OrderDate ?? record.ExtractionDate;
    }
}
