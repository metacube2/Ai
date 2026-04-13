namespace TrafagSalesExporter.Services;

public sealed class CopyTransformationStrategy : ITransformationStrategy
{
    public string TransformationType => "Copy";
    public object? Transform(object? sourceValue, string? argument) => sourceValue;
}

public sealed class UppercaseTransformationStrategy : ITransformationStrategy
{
    public string TransformationType => "Uppercase";
    public object? Transform(object? sourceValue, string? argument) => sourceValue?.ToString()?.ToUpperInvariant();
}

public sealed class LowercaseTransformationStrategy : ITransformationStrategy
{
    public string TransformationType => "Lowercase";
    public object? Transform(object? sourceValue, string? argument) => sourceValue?.ToString()?.ToLowerInvariant();
}

public sealed class PrefixTransformationStrategy : ITransformationStrategy
{
    public string TransformationType => "Prefix";
    public object? Transform(object? sourceValue, string? argument) => $"{argument}{sourceValue}";
}

public sealed class SuffixTransformationStrategy : ITransformationStrategy
{
    public string TransformationType => "Suffix";
    public object? Transform(object? sourceValue, string? argument) => $"{sourceValue}{argument}";
}

public sealed class ReplaceTransformationStrategy : ITransformationStrategy
{
    public string TransformationType => "Replace";

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
    public object? Transform(object? sourceValue, string? argument) => argument;
}
