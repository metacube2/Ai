using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeployConsole;

public sealed class DeploySettings
{
    public string ProjectPath { get; set; } = "";
    public string TargetDir { get; set; } = "";
    public string LocalReleaseDll { get; set; } = "";
    public string MainAssembly { get; set; } = "BiDashboard.dll";

    /// <summary>
    /// The production database, by exact name. Deliberately not "the first *.db we
    /// find": the protocol sentence "Produktiv-DB in Laenge und Schreibzeit
    /// unveraendert" is the line that gets trusted most, and a glob would happily
    /// make that claim about some other .db in a subfolder.
    /// </summary>
    public string DatabaseFile { get; set; } = "trafag_exporter.db";
    public string BaseUrl { get; set; } = "";
    public List<string> Routes { get; set; } = new();

    /// <summary>
    /// Files in the target that are NOT build output and must survive a publish
    /// untouched: the production database, its WAL/SHM sidecars and every .bak.
    /// A change here is the alarm the whole tool exists for.
    ///
    /// Deliberately NOT listed: the workbooks (check.xlsx, zdispo_grp.xlsx,
    /// zdispo_spart.xlsx). They look like data sitting in the target, but the main
    /// csproj ships them with CopyToPublishDirectory="Always", so every publish
    /// replaces them with the repository copy - measured, not assumed. Editing one
    /// of them directly on the share is therefore pointless; change it in the repo.
    /// </summary>
    public List<string> ProtectedPatterns { get; set; } = new();

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public static string DefaultPath => Path.Combine(AppContext.BaseDirectory, "deploy.settings.json");

    public static DeploySettings Load(string path)
    {
        if (!File.Exists(path))
        {
            return new DeploySettings();
        }
        try
        {
            return JsonSerializer.Deserialize<DeploySettings>(File.ReadAllText(path)) ?? new DeploySettings();
        }
        catch
        {
            return new DeploySettings();
        }
    }

    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this, Options));
}
