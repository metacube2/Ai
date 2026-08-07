namespace TrafagSalesExporter.Services;

/// <summary>
/// Pausenspiel-Reiter. Drei Ebenen zum Ausblenden, von hart nach weich:
/// 1. <see cref="Enabled"/> = false -> die Seite antwortet gar nicht mehr.
/// 2. Menueintrag "pause-game" auf IsVisible = false -> Eintrag weg, Route bleibt.
/// 3. RequiredPolicy am Menueintrag -> Eintrag nur fuer bestimmte Personen.
/// </summary>
public sealed class PauseGameOptions
{
    public const string SectionName = "Pause";

    public bool Enabled { get; set; } = true;
}
