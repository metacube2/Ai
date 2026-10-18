namespace TrafagSalesExporter.Services;

internal static class LogisticsKlingonOverrides
{
    internal static readonly IReadOnlyDictionary<string, string> All =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Top-Down Sicht"] = "Dungvo' bIngDaq legh",
            ["Bottom-Up Sicht"] = "bIngvo' DungDaq legh",
            ["Stuecklistenfluss"] = "BOM ghoS",
            ["Kopfmaterial"] = "Hap nach",
            ["Komponenten"] = "'ay'mey",
            ["Verwendungen"] = "lo'ghachmey",
            ["Kopfmaterialien"] = "Hap nachmey",
            ["Geladene Komponenten"] = "'ay'mey qemlu'bogh",
            ["im gefilterten Cache"] = "De' polDaq wIvlu'bogh",
            ["Unterschiedliche Komponenten"] = "'ay'mey pIm",
            ["Gefundene Elternmaterialien"] = "Hap wa'DIchmey tu'lu'bogh",
            ["Bauteile in den Stuecklisten"] = "BOMmeyDaq 'ay'mey",
            ["Verwendungen der Komponenten"] = "'ay'mey lo'ghachmey",
            ["Kopf-Komponenten-Beziehungen"] = "Hap nach 'ay' je rarghachmey",
            ["Mehrfach verwendete Komponenten"] = "pIj lo'lu'bogh 'ay'mey",
            ["aufgeloeste Zeilen"] = "wavlu'bogh tlheghmey",
            ["in mehr als einem Elternmaterial"] = "wa' Hap wa'DIch puSbe'Daq",
            ["Exklusive Komponenten"] = "latlhDaq lo'be'lu'bogh 'ay'mey",
            ["Nur einmal verwendete Komponenten"] = "wa'logh lo'lu'bogh 'ay'mey",
            ["Abhaengigkeit im geladenen Datenstand"] = "De' qemlu'boghDaq wuvlu'ghach",
            ["Komponentenbreite je Kopfmaterial"] = "Hap nach Hoch 'ay'mey SIch",
            ["Verwendungsbreite je Komponente"] = "'ay' Hoch lo'ghachmey SIch",
            ["Top 12 nach Anzahl unterschiedlicher Komponenten"] = "'ay'mey pIm mI' jatlhbogh 12 nIv",
            ["Top 12 nach Anzahl unterschiedlicher Elternmaterialien"] = "Hap wa'DIchmey pIm mI' jatlhbogh 12 nIv",
            ["Bestandslage der Komponenten"] = "'ay'mey polbogh mI' Dotlh",
            ["positiver Endbestand"] = "polbogh mI' Qav pagh juSbogh",
            ["Endbestand null"] = "polbogh mI' Qav pagh",
            ["negativer Endbestand"] = "polbogh mI' Qav pagh bIngDaq",
            ["Bestand nicht geliefert"] = "polbogh mI' De' Hutlh",
            ["LZ-Code Verteilung"] = "LZ ngoq wav",
            ["Unterschiedliche Komponenten je Code"] = "ngoq Hoch 'ay'mey pIm",
            ["ohne Code"] = "ngoq Hutlh",
            ["Keine Top-Down-Beziehungen im aktuellen Filter. Kopfmaterial oder Filter pruefen und bei alten Materialien gegebenenfalls 'Auch geloeschte Materialien' aktivieren."] = "DaH nejwI'Daq Dungvo' bIngDaq rarghach tu'lu'be'. Hap nach nejwI' je yInuD; Hap ngo'vaD polHa'lu'bogh Hapmey je yIwIv.",
            ["Keine Bottom-Up-Verwendungen im aktuellen Filter. Vor dem Schluss 'wird nirgends verbaut' auch geloeschte Materialien gegenpruefen."] = "DaH nejwI'Daq bIngvo' DungDaq lo'ghach tu'lu'be'. paghDaq 'ay' lo'lu' jatlhpa', polHa'lu'bogh Hapmey je yInuD.",
            ["Vom Kopfmaterial bis zum Bestandsrisiko"] = "Hap nachvo' polbogh mI' QobDaq",
            ["Von der Komponente zu allen Elternmaterialien"] = "'ay'vo' Hap wa'DIchmey HochDaq",
            ["Zeigt, wie breit eine Stueckliste aufgeloest ist, welche Komponenten exklusiv sind und wo der Endbestand kritisch wird."] = "BOM wavlu'bogh SIch, latlhDaq lo'be'lu'bogh 'ay'mey, polbogh mI' Qav Qob je 'ang.",
            ["Zeigt Wiederverwendung, Abhaengigkeit und Reichweite einer Komponente ueber alle gefundenen Elternmaterialien."] = "Hap wa'DIchmey tu'lu'bogh HochDaq 'ay' pIj lo'lu'ghach, wuvlu'ghach, SIch je 'ang.",
            ["Lesart Top-Down: {0} Kopfmaterialien enthalten {1} unterschiedliche Komponenten. Exklusivitaet und negativer Endbestand markieren die zuerst zu pruefenden Bauteile; Bestandswerte werden nicht ueber Stuecklisten summiert, damit gemeinsam verwendete Komponenten nicht doppelt bewertet werden."] = "Dungvo' bIngDaq laD: {0} Hap nachmeyDaq {1} 'ay'mey pIm tu'lu'. latlhDaq lo'be'lu'ghach polbogh mI' Qav pagh bIngDaq je 'ay'mey wa'DIch nuDlu'bogh 'ang; 'ay'mey wa' Dol cha'logh SImlu'be'meH BOMmeyDaq polbogh Huch boSbe'lu'.",
            ["Lesart Bottom-Up: {0} Komponenten fuehren zu {1} Elternmaterialien. Hohe Balken zeigen grosse Wiederverwendung und damit breite Auswirkung bei Aenderung oder Ausfall; eine Einzelverwendung zeigt dagegen konzentrierte Abhaengigkeit."] = "bIngvo' DungDaq laD: {0} 'ay'meyvo' {1} Hap wa'DIchmey tu'lu'. naQ jenmey pIj lo'lu'ghach 'ang; vaj choH pagh Qagh HochDaq SIch. wa'logh lo'lu'chugh wa' Daq neH wuvlu'."
        };
}
