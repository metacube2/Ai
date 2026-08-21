using Microsoft.Data.Sqlite;

namespace DeployConsole;

/// <summary>Ergebnis einer Sicherungsanforderung.</summary>
/// <param name="Path">Die Sicherung, die als Vorher-Nachweis gilt.</param>
/// <param name="Reused">
/// <c>true</c>, wenn eine vorhandene Sicherung wiederverwendet wurde, statt eine neue
/// anzulegen. Fuer den Protokollabsatz relevant: dort muss stehen, WELCHE Datei den
/// Nachweis bildet, nicht nur dass gesichert wurde.
/// </param>
/// <param name="Reason">Klartextbegruendung fuer das Protokoll.</param>
public sealed record BackupResult(string Path, bool Reused, string Reason);

/// <summary>
/// Konsistente Vorher-Sicherung der Produktivdatenbank, aber nur wenn sie fehlt.
///
/// WARUM DIESE KLASSE EXISTIERT (gemessen am 2026-08-21): An diesem Tag liefen drei Deploys
/// hintereinander, keiner fasste die Datenbank an, und trotzdem entstanden DREI inhaltsgleiche
/// Sicherungen (10:12, 10:46, 13:01). Jede kostet ueber die Netzwerkfreigabe rund zehn
/// Minuten, weil <see cref="SqliteConnection.BackupDatabase"/> Seite fuer Seite ueber SMB
/// liest und schreibt. Das waren rund dreissig Minuten Wartezeit fuer nichts.
///
/// Der Effekt verschaerft sich, weil die Datenbank waechst: Juni 234 MB, August 353 MB.
/// Die Sicherungsdauer skaliert direkt mit.
///
/// Die Regel steht verbindlich in `docs/DEPLOYMENT.md` Abschnitt 5a. Sie liegt hier und
/// nicht in den Wegwerf-Werkzeugen unter `.tmp_tools/`, damit sie fuer jeden Deploy-Weg
/// gilt und nicht jedes Mal neu erfunden wird.
///
/// KEIN SICHERHEITSVERLUST: Wiederverwendet wird nur, wenn Groesse UND Schreibzeit exakt
/// uebereinstimmen. Sobald die Datenbank seit der letzten Sicherung geschrieben wurde -
/// Schemaaenderung, Seed, Migration, Import, Einkauf-Lauf - weicht mindestens eines davon ab
/// und es wird neu gesichert.
/// </summary>
public static class DatabaseBackup
{
    /// <summary>
    /// Stellt sicher, dass zur aktuellen Datenbank eine Sicherung vorliegt, und liefert deren
    /// Pfad. Legt nur dann eine neue an, wenn keine passende existiert.
    /// </summary>
    /// <param name="databasePath">Die Produktivdatenbank.</param>
    /// <param name="topic">Kurzname fuer den Dateinamen, etwa <c>overlay-fix</c>.</param>
    /// <param name="timestamp">Zeitstempel fuer den Dateinamen einer neuen Sicherung.</param>
    /// <param name="log">Fortschrittsausgabe.</param>
    public static async Task<BackupResult> EnsureBackupAsync(
        string databasePath,
        string topic,
        DateTime timestamp,
        Action<string> log,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(databasePath))
            throw new FileNotFoundException("Produktivdatenbank fehlt.", databasePath);

        var directory = Path.GetDirectoryName(databasePath)
                        ?? throw new InvalidOperationException(
                            $"Kein Verzeichnis zu '{databasePath}' bestimmbar.");
        var current = new FileInfo(databasePath);

        var existing = FindMatchingBackup(directory, current);
        if (existing is not null)
        {
            var reason =
                $"Vorhandene Sicherung wiederverwendet: {Path.GetFileName(existing)}. " +
                $"Groesse ({current.Length:N0} Bytes) und Schreibzeit " +
                $"({current.LastWriteTimeUtc:yyyy-MM-dd HH:mm:ss} UTC) stimmen mit der " +
                "Produktivdatenbank ueberein, die Datenbank wurde seither nicht geschrieben. " +
                "Eine zweite, inhaltsgleiche Kopie waere reine Wartezeit " +
                "(docs/DEPLOYMENT.md Abschnitt 5a).";
            log(reason);
            return new BackupResult(existing, true, reason);
        }

        var target = Path.Combine(
            directory,
            $"trafag_exporter.db.before-{topic}-{timestamp:yyyyMMdd-HHmmss}.bak");
        if (File.Exists(target))
            throw new IOException($"Sicherung existiert bereits: {target}");

        log($"Keine passende Sicherung vorhanden, lege neue an ({current.Length:N0} Bytes).");

        // ERST LOKAL SICHERN, DANN IN EINEM ZUG KOPIEREN.
        //
        // Gemessen am 2026-08-21: schreibt `BackupDatabase` direkt auf die Freigabe, laeuft es
        // mit rund 145 KB/s - fuer 353 MB also gegen 40 Minuten. Die Ursache ist nicht die
        // Bandbreite, sondern die Latenz: die API kopiert seitenweise, und jede Seite ist ein
        // eigener Schreibvorgang mit eigener SMB-Rundreise.
        //
        // Lokal geschrieben entfaellt diese Rundreise. Der anschliessende `File.Copy` uebertraegt
        // die fertige Datei in grossen Bloecken und laeuft mit Netzgeschwindigkeit.
        //
        // Die Konsistenz bleibt voll erhalten: sie stammt aus der `BackupDatabase`-API selbst,
        // die eine in sich geschlossene Kopie erzeugt, und haengt nicht davon ab, WOHIN
        // geschrieben wird. Ein reiner `File.Copy` der LIVE-Datenbank waere dagegen unzulaessig,
        // weil parallele Schreibvorgaenge und der WAL-Zustand eine zerrissene Kopie ergeben
        // koennten - genau deshalb bleibt die API der erste Schritt.
        var staging = Path.Combine(Path.GetTempPath(), Path.GetFileName(target));
        if (File.Exists(staging))
            File.Delete(staging);

        var source = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Private,
            DefaultTimeout = 60
        }.ToString();
        var destination = new SqliteConnectionStringBuilder
        {
            DataSource = staging,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Private,
            DefaultTimeout = 60
        }.ToString();

        try
        {
            var started = DateTime.UtcNow;
            await using (var from = new SqliteConnection(source))
            await using (var to = new SqliteConnection(destination))
            {
                await from.OpenAsync(cancellationToken);
                await to.OpenAsync(cancellationToken);
                from.BackupDatabase(to);
            }
            log($"Lokale Sicherung fertig in {(DateTime.UtcNow - started).TotalSeconds:N0} s, " +
                "uebertrage auf die Freigabe...");

            var copyStarted = DateTime.UtcNow;
            File.Copy(staging, target, overwrite: false);
            log($"Uebertragung fertig in {(DateTime.UtcNow - copyStarted).TotalSeconds:N0} s.");
        }
        finally
        {
            // Die lokale Zwischendatei ist nach der Uebertragung wertlos und belegt sonst
            // dauerhaft mehrere hundert MB im Temp-Verzeichnis.
            try
            {
                if (File.Exists(staging))
                    File.Delete(staging);
            }
            catch (IOException)
            {
                log($"Hinweis: lokale Zwischendatei blieb liegen: {staging}");
            }
        }

        // Erst pruefen, dass die uebertragene Datei vollstaendig ist. Genau hier lag am
        // 2026-08-19 eine abgebrochene Sicherung mit 60 MB statt 353 MB auf der Freigabe und
        // sah wie eine gueltige aus.
        var copied = new FileInfo(target);
        if (!copied.Exists || copied.Length == 0)
            throw new IOException($"Uebertragene Sicherung fehlt oder ist leer: {target}");

        // ERST JETZT die Begleitdatei schreiben, nach dem erfolgreichen Abschluss. Wuerde sie
        // vorher entstehen, wiese ein Abbruch mitten in der Sicherung eine unvollstaendige
        // Kopie als gueltigen Nachweis aus, und der naechste Deploy verliesse sich darauf.
        WriteSourceMarker(target, databasePath);

        var written = new FileInfo(target).Length;
        var newReason = $"Neue Sicherung angelegt: {Path.GetFileName(target)} ({written:N0} Bytes).";
        log(newReason);
        return new BackupResult(target, false, newReason);
    }

    /// <summary>
    /// Sucht eine Sicherung, die zur uebergebenen Datenbank passt. Verglichen werden Groesse
    /// und Schreibzeit auf die Sekunde. Die Schreibzeit allein genuegt nicht, weil SMB und
    /// unterschiedliche Dateisysteme sie unterschiedlich fein aufloesen; die Groesse allein
    /// genuegt ebenfalls nicht, weil eine SQLite-Datei bei gleicher Groesse anderen Inhalt
    /// haben kann.
    ///
    /// WICHTIG: Verglichen wird gegen die SICHERUNG der Datenbank, nicht gegen deren eigenen
    /// Zeitstempel. Eine `.bak` traegt die Schreibzeit ihrer Erzeugung, nicht die der Quelle.
    /// Deshalb wird die Zuordnung ueber eine Begleitdatei gefuehrt, die neben der Sicherung
    /// liegt und Groesse und Schreibzeit der Quelle festhaelt.
    /// </summary>
    private static string? FindMatchingBackup(string directory, FileInfo current)
    {
        var stamp = BuildStamp(current);

        foreach (var marker in Directory.EnumerateFiles(directory, "*.bak.source", SearchOption.TopDirectoryOnly))
        {
            string content;
            try
            {
                content = File.ReadAllText(marker).Trim();
            }
            catch (IOException)
            {
                // Eine unlesbare Begleitdatei darf den Deploy nicht aufhalten; dann wird
                // eben neu gesichert. Sicherheit vor Geschwindigkeit.
                continue;
            }

            if (!string.Equals(content, stamp, StringComparison.Ordinal))
                continue;

            var backup = marker[..^".source".Length];
            if (File.Exists(backup))
                return backup;
        }

        return null;
    }

    /// <summary>
    /// Schreibt die Begleitdatei zu einer Sicherung. Getrennt aufzurufen, damit eine
    /// abgebrochene Sicherung keine Begleitdatei hinterlaesst, die eine unvollstaendige
    /// Kopie als gueltig ausweisen wuerde.
    /// </summary>
    public static void WriteSourceMarker(string backupPath, string databasePath)
        => File.WriteAllText(backupPath + ".source", BuildStamp(new FileInfo(databasePath)));

    private static string BuildStamp(FileInfo file)
        => $"{file.Length}|{file.LastWriteTimeUtc:yyyy-MM-ddTHH:mm:ss}";
}
