using System.Text;

namespace DeployConsole;

public sealed record FileState(string RelativePath, long Length, DateTime LastWriteUtc)
{
    public string Describe() => $"{RelativePath}  {Length:N0} Bytes  {LastWriteUtc.ToLocalTime():dd.MM.yyyy HH:mm:ss}";
}

/// <summary>
/// Metadata-only inventory of the publish target, taken before and after the publish.
/// The point is not to know what changed in the build output - that is expected to
/// change - but to prove that nothing which is NOT build output disappeared or was
/// rewritten. A publish that deletes the production database is recoverable only if
/// somebody notices immediately.
/// </summary>
public sealed class TargetSnapshot
{
    public IReadOnlyDictionary<string, FileState> Files { get; }

    private TargetSnapshot(Dictionary<string, FileState> files) => Files = files;

    public static TargetSnapshot Take(string targetDir)
    {
        var files = new Dictionary<string, FileState>(StringComparer.OrdinalIgnoreCase);
        var root = new DirectoryInfo(targetDir);
        if (!root.Exists)
        {
            return new TargetSnapshot(files);
        }
        foreach (var info in root.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(targetDir, info.FullName);
            files[rel] = new FileState(rel, info.Length, info.LastWriteTimeUtc);
        }
        return new TargetSnapshot(files);
    }

    /// <summary>Exact lookup by path relative to the target root.</summary>
    public FileState? Find(string relativePath) => Files.TryGetValue(relativePath, out var state) ? state : null;

    public IEnumerable<FileState> Matching(IEnumerable<string> patterns)
    {
        var matcher = patterns.ToList();
        return Files.Values
            .Where(f => matcher.Any(p => FitsPattern(Path.GetFileName(f.RelativePath), p)))
            .OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase);
    }

    private static bool FitsPattern(string name, string pattern)
    {
        // Only the "*.ext" and plain-name forms are used in the settings; keep the
        // matching that narrow rather than pulling in a glob library.
        if (pattern.StartsWith("*.", StringComparison.Ordinal))
        {
            return name.EndsWith(pattern[1..], StringComparison.OrdinalIgnoreCase);
        }
        return string.Equals(name, pattern, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class SnapshotDiff
{
    public List<FileState> Vanished { get; } = new();
    public List<(FileState Before, FileState After)> ProtectedChanged { get; } = new();
    public int AddedCount { get; init; }
    public int ChangedCount { get; init; }
    public int UnchangedCount { get; init; }

    public bool IsClean => Vanished.Count == 0 && ProtectedChanged.Count == 0;

    public static SnapshotDiff Compare(TargetSnapshot before, TargetSnapshot after, IEnumerable<string> protectedPatterns)
    {
        var protectedSet = before.Matching(protectedPatterns)
            .Select(f => f.RelativePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = 0;
        var changed = 0;
        var unchanged = 0;
        foreach (var (rel, afterState) in after.Files)
        {
            if (!before.Files.TryGetValue(rel, out var beforeState))
            {
                added++;
            }
            else if (beforeState.Length != afterState.Length || beforeState.LastWriteUtc != afterState.LastWriteUtc)
            {
                changed++;
            }
            else
            {
                unchanged++;
            }
        }

        var diff = new SnapshotDiff { AddedCount = added, ChangedCount = changed, UnchangedCount = unchanged };
        foreach (var (rel, beforeState) in before.Files)
        {
            if (!after.Files.TryGetValue(rel, out var afterState))
            {
                diff.Vanished.Add(beforeState);
            }
            else if (protectedSet.Contains(rel)
                     && (beforeState.Length != afterState.Length || beforeState.LastWriteUtc != afterState.LastWriteUtc))
            {
                diff.ProtectedChanged.Add((beforeState, afterState));
            }
        }
        return diff;
    }

    public string Describe()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Dateien im Ziel: {AddedCount} neu, {ChangedCount} geaendert, {UnchangedCount} unveraendert, {Vanished.Count} verschwunden.");
        foreach (var gone in Vanished)
        {
            sb.AppendLine($"  ALARM verschwunden: {gone.Describe()}");
        }
        foreach (var (b, a) in ProtectedChanged)
        {
            sb.AppendLine($"  ALARM geschuetzte Datei veraendert: {b.RelativePath}");
            sb.AppendLine($"      vorher : {b.Length:N0} Bytes  {b.LastWriteUtc.ToLocalTime():dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine($"      nachher: {a.Length:N0} Bytes  {a.LastWriteUtc.ToLocalTime():dd.MM.yyyy HH:mm:ss}");
        }
        return sb.ToString().TrimEnd();
    }
}
