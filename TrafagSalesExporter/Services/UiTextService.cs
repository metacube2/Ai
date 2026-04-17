namespace TrafagSalesExporter.Services;

public interface IUiTextService
{
    string CurrentLanguage { get; }
    event Action? Changed;
    void SetLanguage(string language);
    string Text(string german, string english);
}

public sealed class UiTextService : IUiTextService
{
    private string _currentLanguage = "de";

    public string CurrentLanguage => _currentLanguage;

    public event Action? Changed;

    public void SetLanguage(string language)
    {
        var normalized = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "de";
        if (string.Equals(_currentLanguage, normalized, StringComparison.OrdinalIgnoreCase))
            return;

        _currentLanguage = normalized;
        Changed?.Invoke();
    }

    public string Text(string german, string english)
        => string.Equals(_currentLanguage, "en", StringComparison.OrdinalIgnoreCase) ? english : german;
}
