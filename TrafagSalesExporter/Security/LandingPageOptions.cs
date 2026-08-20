namespace TrafagSalesExporter.Security;

public sealed class LandingPageOptions
{
    public const string SectionName = "LandingPage";

    public bool ShowWalkingLabFigure { get; set; }

    // Begruessungston beim ersten Aufruf der Startseite. Standardmaessig an, ueber
    // appsettings.json abschaltbar, ohne dass es dafuer einen Admin-UI-Schalter braucht
    // (gleiches Muster wie ShowWalkingLabFigure).
    public bool PlayWelcomeSound { get; set; } = true;
}
