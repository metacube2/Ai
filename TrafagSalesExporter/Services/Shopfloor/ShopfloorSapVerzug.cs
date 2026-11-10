using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>
/// Operations (Shopfloor), 2026-11-02, Mail Fabio Palma (Definition PPD, A. Tran): Verzug je Abteilung wie ZC22 (ZM_OFFENE_FAUF).
/// <list type="bullet">
/// <item>Quelle: ShopAufSet mit Filter Typ eq 'VZ' (docs/abap/ZSHOP_VERZUG_ADD.abap, Auftrag T76K912797): offene Fertigungs- und
/// Planauftraege mit Eckendtermin vor dem Abfragetag, ohne Untergrenze, ohne erledigte, Menge = Positionsmenge wie die ZC22-Spalte.</item>
/// <item>Abteilung ueber den Disponenten: SEH S*, TR5 EK*, TX EL*, DW D* ohne DW1 und DL6 (DS1/DS2/DSN gehoeren zu DW).</item>
/// <item>Material 1 bis 99999 (nur numerische Materialnummern), wie Fabios ZC22-Selektion.</item>
/// <item>Werte positiv, auf den Abfragetag geschrieben; manuell erfasste Werte bleiben (ShopfloorSapMengen.BuildItems).</item>
/// </list>
/// Abgleich T76 2026-11-02 gegen ZC22: DW 1'570 Auftraege / 49'526, TR5 786 / 41'127, TX 1'349 / 73'895, je Auftrag gleich.
/// Doku: docs/SHOPFLOOR_2026-10-07.md Abschnitt 17.
/// </summary>
public static class ShopfloorSapVerzug
{
    public const string Module = "ppd";
    public const int MaterialMax = 99999;

    /// <summary>Abteilung, Feld im Modul ppd, Disponenten-Praefixe und ausgenommene Disponenten.</summary>
    public static readonly (string Abteilung, string Feld, string[] Praefixe, string[] Ohne)[] Abteilungen =
    [
        ("SEH", "verzug_seh", ["S"], []),
        ("TR5", "verzug_tr5", ["EK"], []),
        ("TX", "verzug_tx", ["EL"], []),
        ("DW", "verzug_dw", ["D"], ["DW1", "DL6"])
    ];

    public static string Filter(string plant, DateOnly day)
        => $"Werks eq '{plant}' and Datum eq '{day:yyyyMMdd}' and Typ eq 'VZ'";

    /// <summary>Abteilung zum Disponenten, sonst null.</summary>
    public static string? Abteilung(string dispo)
    {
        var d = dispo.Trim().ToUpperInvariant();
        foreach (var a in Abteilungen)
            if (a.Praefixe.Any(p => d.StartsWith(p, StringComparison.Ordinal)) && !a.Ohne.Contains(d))
                return a.Abteilung;
        return null;
    }

    /// <summary>Numerische Materialnummer von 1 bis 99999 (fuehrende Nullen egal); alphanumerische zaehlen nicht.</summary>
    public static bool MaterialImBereich(string matnr)
    {
        var m = matnr.Trim();
        return m.Length > 0 && m.All(char.IsAsciiDigit)
               && long.TryParse(m.TrimStart('0') is { Length: > 0 } t ? t : "0", NumberStyles.None, CultureInfo.InvariantCulture, out var n)
               && n is >= 1 and <= MaterialMax;
    }

    /// <summary>
    /// Werte fuer den Abfragetag aus den Zeilen von ShopAufSet Typ VZ. Schreibt nichts. Zeilen ohne Typ VF/VP heissen, dass das
    /// System den Verzugsmodus noch nicht kennt (alter DPC liefert FE/PA fuer die naechsten 60 Tage): dann Fehler statt falscher Zahl.
    /// </summary>
    public static JsonObject Compute(DateOnly day, IReadOnlyList<JsonElement> rows, IReadOnlyCollection<string>? materialFilter = null)
    {
        // 2026-11-10 (Ingo): Materialfilter 1-99999 je Abteilung schaltbar (Shopfloor:VerzugMaterialFilter), Vorgabe alle vier wie PPD.
        materialFilter ??= Abteilungen.Select(a => a.Abteilung).ToArray();
        if (rows.Any(r => Text(r, "Typ") is not ("VF" or "VP")))
            throw new InvalidOperationException(
                "ShopAufSet kennt den Verzugsmodus (Typ VZ) in diesem SAP-System noch nicht; Transport T76K912797 fehlt.");
        var menge = Abteilungen.ToDictionary(a => a.Abteilung, _ => 0L, StringComparer.Ordinal);
        var auftraege = Abteilungen.ToDictionary(a => a.Abteilung, _ => 0, StringComparer.Ordinal);
        foreach (var r in rows)
        {
            if (Abteilung(Text(r, "Dispo")) is not { } abt)
                continue;
            if (materialFilter.Contains(abt) && !MaterialImBereich(Text(r, "Matnr")))
                continue;
            menge[abt] += Int(r, "Menge");
            auftraege[abt]++;
        }
        var ppd = new JsonObject();
        var detail = new JsonObject();
        foreach (var a in Abteilungen)
        {
            ppd[a.Feld] = menge[a.Abteilung];
            detail[a.Abteilung] = new JsonObject { ["auftraege"] = auftraege[a.Abteilung], ["menge"] = menge[a.Abteilung] };
        }
        return new JsonObject
        {
            ["day"] = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["zeilen"] = rows.Count,
            ["werte"] = new JsonObject { [Module] = ppd },
            ["detail"] = detail
        };
    }

    private static string Text(JsonElement x, string name)
        => x.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()?.Trim() ?? string.Empty : string.Empty;

    private static long Int(JsonElement x, string name)
        => x.TryGetProperty(name, out var v)
            ? v.ValueKind switch
            {
                JsonValueKind.Number when v.TryGetInt64(out var i) => i,
                JsonValueKind.String when long.TryParse(v.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) => s,
                _ => 0
            }
            : 0;
}
