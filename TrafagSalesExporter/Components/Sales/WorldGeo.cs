using System.Globalization;

namespace TrafagSalesExporter.Components.Sales;

/// <summary>
/// Stilisierte Weltkarte fuer den Reiter Verkauf (2026-10-02): grob vereinfachte Kontinentumrisse (Laengen-/Breitengrad,
/// bewusst niedrig aufgeloest, keine exakten Grenzen) und Laendermittelpunkte je ISO-2-Code. Keine externe Bibliothek.
/// Projektion: Plattkarte, Laenge -180..180 auf 0..1000, Breite 84..-58 auf 0..500.
/// </summary>
public static class WorldGeo
{
    public const double Width = 1000;
    public const double Height = 500;

    public static (double X, double Y) Project(double lat, double lon)
        => ((lon + 180) / 360 * Width, (84 - lat) / 142 * Height);

    private static readonly double[][] Continents =
    [
        // Nordamerika
        [-168, 66, -162, 70, -140, 70, -125, 72, -95, 74, -80, 73, -62, 66, -55, 52, -66, 45, -70, 41, -76, 35, -81, 31, -80, 25, -82, 29, -90, 30, -97, 26, -97, 21, -92, 18, -88, 15, -83, 9, -79, 8, -86, 12, -95, 16, -105, 20, -110, 23, -117, 32, -124, 40, -124, 48, -133, 56, -146, 60, -158, 57, -165, 60],
        // Groenland
        [-55, 60, -44, 60, -20, 70, -18, 80, -40, 83, -65, 80, -72, 77, -56, 71],
        // Suedamerika
        [-80, 9, -72, 12, -62, 11, -52, 5, -35, -6, -39, -14, -41, -22, -48, -26, -58, -34, -65, -41, -68, -50, -72, -54, -75, -48, -73, -37, -71, -18, -77, -12, -81, -5, -78, 1],
        // Eurasien
        [-10, 36, -9, 43, -1, 46, -4, 48, 2, 51, 8, 54, 5, 58, 5, 62, 15, 69, 28, 71, 42, 67, 60, 70, 80, 73, 105, 78, 140, 72, 180, 69, 178, 64, 160, 60, 156, 51, 142, 47, 140, 40, 130, 35, 122, 40, 121, 31, 118, 24, 109, 21, 106, 10, 100, 13, 103, 1, 98, 8, 92, 20, 80, 15, 77, 8, 72, 20, 66, 25, 57, 26, 52, 17, 43, 13, 35, 28, 34, 31, 36, 36, 27, 37, 23, 40, 20, 40, 18, 40, 16, 38, 12, 44, 8, 44, 3, 43, -1, 37],
        // Afrika
        [-17, 21, -16, 12, -12, 7, -5, 5, 5, 4, 9, 4, 10, -2, 13, -12, 12, -18, 15, -27, 18, -34, 26, -34, 33, -28, 35, -20, 40, -15, 40, -5, 51, 11, 43, 12, 37, 18, 32, 31, 20, 32, 10, 37, -1, 36, -6, 35, -10, 30],
        // Australien
        [114, -22, 122, -18, 130, -12, 137, -12, 142, -11, 146, -19, 153, -25, 151, -34, 145, -39, 138, -35, 131, -31, 115, -34, 114, -26],
        // Grossbritannien, Irland, Island
        [-5, 50, 1, 51, 2, 53, -1, 55, -2, 58, -5, 59, -6, 56, -3, 54],
        [-10, 52, -6, 52, -6, 55, -8, 55],
        [-24, 64, -14, 64, -15, 66, -22, 66],
        // Japan
        [130, 31, 135, 34, 140, 35, 142, 40, 141, 45, 140, 42, 136, 37, 131, 34],
        // Indonesien und Neuguinea
        [95, 5, 105, -6, 115, -8, 120, -9, 125, -8, 118, 1, 117, 7, 109, 2, 100, 1],
        [131, -1, 141, -3, 150, -10, 141, -9, 134, -4],
        // Madagaskar, Neuseeland
        [44, -25, 47, -25, 50, -15, 49, -12, 44, -16],
        [166, -46, 172, -41, 178, -38, 174, -41, 170, -46]
    ];

    public static IEnumerable<string> ContinentPaths()
    {
        foreach (var c in Continents)
        {
            var parts = new List<string>();
            for (var i = 0; i + 1 < c.Length; i += 2)
            {
                var (x, y) = Project(c[i + 1], c[i]);
                parts.Add($"{(i == 0 ? "M" : "L")}{x.ToString("0.#", CultureInfo.InvariantCulture)},{y.ToString("0.#", CultureInfo.InvariantCulture)}");
            }
            yield return string.Join(" ", parts) + " Z";
        }
    }

    /// <summary>Laendermittelpunkt (Breite, Laenge) und Name je ISO-2-Code.</summary>
    public static readonly IReadOnlyDictionary<string, (double Lat, double Lon, string Name)> Countries = new Dictionary<string, (double, double, string)>
    {
        ["CH"] = (46.8, 8.2, "Schweiz"), ["DE"] = (51.2, 10.4, "Deutschland"), ["AT"] = (47.6, 14.1, "Österreich"), ["IT"] = (42.8, 12.6, "Italien"),
        ["FR"] = (46.6, 2.4, "Frankreich"), ["ES"] = (40.2, -3.6, "Spanien"), ["PT"] = (39.6, -8.0, "Portugal"), ["GB"] = (53.0, -1.8, "Grossbritannien"),
        ["IE"] = (53.2, -8.0, "Irland"), ["NL"] = (52.2, 5.5, "Niederlande"), ["BE"] = (50.6, 4.6, "Belgien"), ["LU"] = (49.8, 6.1, "Luxemburg"),
        ["DK"] = (56.0, 9.5, "Dänemark"), ["SE"] = (62.0, 15.5, "Schweden"), ["NO"] = (61.5, 9.0, "Norwegen"), ["FI"] = (63.5, 26.5, "Finnland"),
        ["IS"] = (64.9, -18.6, "Island"), ["PL"] = (52.0, 19.4, "Polen"), ["CZ"] = (49.8, 15.5, "Tschechien"), ["SK"] = (48.7, 19.7, "Slowakei"),
        ["HU"] = (47.2, 19.4, "Ungarn"), ["SI"] = (46.1, 14.8, "Slowenien"), ["HR"] = (45.1, 15.6, "Kroatien"), ["RS"] = (44.0, 20.8, "Serbien"),
        ["BA"] = (44.2, 17.8, "Bosnien"), ["RO"] = (45.9, 25.0, "Rumänien"), ["BG"] = (42.7, 25.3, "Bulgarien"), ["GR"] = (39.1, 22.0, "Griechenland"),
        ["TR"] = (39.0, 35.2, "Türkei"), ["CY"] = (35.0, 33.2, "Zypern"), ["MT"] = (35.9, 14.4, "Malta"), ["EE"] = (58.7, 25.5, "Estland"),
        ["LV"] = (56.9, 24.6, "Lettland"), ["LT"] = (55.3, 23.9, "Litauen"), ["UA"] = (49.0, 31.4, "Ukraine"), ["BY"] = (53.7, 28.0, "Belarus"),
        ["RU"] = (60.0, 60.0, "Russland"), ["MD"] = (47.2, 28.5, "Moldau"), ["MK"] = (41.6, 21.7, "Nordmazedonien"), ["AL"] = (41.2, 20.0, "Albanien"),
        ["ME"] = (42.7, 19.3, "Montenegro"), ["LI"] = (47.2, 9.6, "Liechtenstein"), ["MC"] = (43.7, 7.4, "Monaco"),
        ["US"] = (39.5, -98.4, "USA"), ["CA"] = (56.0, -106.0, "Kanada"), ["MX"] = (23.6, -102.5, "Mexiko"), ["BR"] = (-10.8, -52.9, "Brasilien"),
        ["AR"] = (-34.0, -64.0, "Argentinien"), ["CL"] = (-33.5, -70.7, "Chile"), ["CO"] = (4.1, -73.0, "Kolumbien"), ["PE"] = (-9.2, -75.0, "Peru"),
        ["VE"] = (7.0, -66.0, "Venezuela"), ["EC"] = (-1.5, -78.4, "Ecuador"), ["UY"] = (-32.8, -56.0, "Uruguay"), ["BO"] = (-16.7, -64.6, "Bolivien"),
        ["PY"] = (-23.4, -58.4, "Paraguay"), ["CR"] = (9.9, -84.1, "Costa Rica"), ["PA"] = (8.5, -80.1, "Panama"), ["GT"] = (15.6, -90.3, "Guatemala"),
        ["DO"] = (18.9, -70.5, "Dominikanische Rep."), ["CU"] = (21.6, -79.0, "Kuba"), ["TT"] = (10.5, -61.3, "Trinidad und Tobago"),
        ["CN"] = (35.0, 104.0, "China"), ["JP"] = (36.5, 138.5, "Japan"), ["KR"] = (36.4, 127.9, "Südkorea"), ["TW"] = (23.7, 121.0, "Taiwan"),
        ["HK"] = (22.3, 114.2, "Hongkong"), ["IN"] = (22.0, 79.0, "Indien"), ["PK"] = (30.0, 70.0, "Pakistan"), ["BD"] = (23.7, 90.3, "Bangladesch"),
        ["LK"] = (7.8, 80.7, "Sri Lanka"), ["TH"] = (15.5, 101.0, "Thailand"), ["VN"] = (16.0, 107.8, "Vietnam"), ["MY"] = (3.8, 102.3, "Malaysia"),
        ["SG"] = (1.35, 103.8, "Singapur"), ["ID"] = (-2.5, 117.0, "Indonesien"), ["PH"] = (12.8, 121.8, "Philippinen"), ["MM"] = (21.0, 96.0, "Myanmar"),
        ["KZ"] = (48.0, 67.0, "Kasachstan"), ["UZ"] = (41.4, 64.6, "Usbekistan"), ["MN"] = (46.9, 103.8, "Mongolei"),
        ["AE"] = (24.2, 54.3, "VAE"), ["SA"] = (24.0, 45.0, "Saudi-Arabien"), ["QA"] = (25.3, 51.2, "Katar"), ["KW"] = (29.3, 47.6, "Kuwait"),
        ["BH"] = (26.0, 50.6, "Bahrain"), ["OM"] = (21.0, 57.0, "Oman"), ["IL"] = (31.4, 35.0, "Israel"), ["JO"] = (31.2, 36.5, "Jordanien"),
        ["LB"] = (33.9, 35.9, "Libanon"), ["IQ"] = (33.0, 44.0, "Irak"), ["IR"] = (32.4, 53.7, "Iran"), ["EG"] = (26.8, 30.8, "Ägypten"),
        ["MA"] = (31.8, -7.1, "Marokko"), ["DZ"] = (28.0, 2.6, "Algerien"), ["TN"] = (34.0, 9.5, "Tunesien"), ["LY"] = (27.0, 17.0, "Libyen"),
        ["NG"] = (9.1, 8.7, "Nigeria"), ["GH"] = (7.9, -1.0, "Ghana"), ["CI"] = (7.5, -5.5, "Elfenbeinküste"), ["SN"] = (14.5, -14.5, "Senegal"),
        ["KE"] = (0.2, 37.9, "Kenia"), ["ET"] = (9.1, 40.5, "Äthiopien"), ["TZ"] = (-6.4, 34.9, "Tansania"), ["ZA"] = (-30.6, 22.9, "Südafrika"),
        ["AO"] = (-11.2, 17.9, "Angola"), ["MZ"] = (-18.7, 35.5, "Mosambik"), ["ZM"] = (-13.1, 27.8, "Sambia"), ["ZW"] = (-19.0, 29.2, "Simbabwe"),
        ["AU"] = (-25.3, 133.8, "Australien"), ["NZ"] = (-41.0, 174.0, "Neuseeland")
    };

    public static string Name(string iso) => Countries.TryGetValue(iso, out var c) ? c.Name : iso;
}
