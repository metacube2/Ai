namespace TrafagSalesExporter.Services;

internal static class LogisticsUiTextCatalog
{
    internal static readonly IReadOnlyList<(string German, string English)> All =
    [
        ("Top-Down Sicht", "Top-down view"),
        ("Bottom-Up Sicht", "Bottom-up view"),
        ("Stuecklistenfluss", "BOM flow"),
        ("Kopfmaterial", "Header material"),
        ("Komponenten", "Components"),
        ("Verwendungen", "Usages"),
        ("Kopfmaterialien", "Header materials"),
        ("Geladene Komponenten", "Loaded components"),
        ("im gefilterten Cache", "in the filtered cache"),
        ("Unterschiedliche Komponenten", "Distinct components"),
        ("Gefundene Elternmaterialien", "Parent materials found"),
        ("Bauteile in den Stuecklisten", "parts in the BOMs"),
        ("Verwendungen der Komponenten", "component usages"),
        ("Kopf-Komponenten-Beziehungen", "Header-component relations"),
        ("Mehrfach verwendete Komponenten", "Reused components"),
        ("aufgeloeste Zeilen", "exploded rows"),
        ("in mehr als einem Elternmaterial", "in more than one parent material"),
        ("Exklusive Komponenten", "Exclusive components"),
        ("Nur einmal verwendete Komponenten", "Single-use components"),
        ("Abhaengigkeit im geladenen Datenstand", "dependency in the loaded data"),
        ("Komponentenbreite je Kopfmaterial", "Component breadth by header material"),
        ("Verwendungsbreite je Komponente", "Usage breadth by component"),
        ("Top 12 nach Anzahl unterschiedlicher Komponenten", "Top 12 by distinct component count"),
        ("Top 12 nach Anzahl unterschiedlicher Elternmaterialien", "Top 12 by distinct parent-material count"),
        ("Bestandslage der Komponenten", "Component stock position"),
        ("positiver Endbestand", "positive final stock"),
        ("Endbestand null", "zero final stock"),
        ("negativer Endbestand", "negative final stock"),
        ("Bestand nicht geliefert", "stock not supplied"),
        ("LZ-Code Verteilung", "LC-code distribution"),
        ("Unterschiedliche Komponenten je Code", "Distinct components per code"),
        ("ohne Code", "without code"),
        ("Keine Top-Down-Beziehungen im aktuellen Filter. Kopfmaterial oder Filter pruefen und bei alten Materialien gegebenenfalls 'Auch geloeschte Materialien' aktivieren.", "No top-down relations in the current filter. Check the header material or filter and, for old materials, enable 'Include deleted materials' if needed."),
        ("Keine Bottom-Up-Verwendungen im aktuellen Filter. Vor dem Schluss 'wird nirgends verbaut' auch geloeschte Materialien gegenpruefen.", "No bottom-up usages in the current filter. Before concluding that a component is unused, also check deleted materials."),
        ("Vom Kopfmaterial bis zum Bestandsrisiko", "From header material to stock risk"),
        ("Von der Komponente zu allen Elternmaterialien", "From component to all parent materials"),
        ("Zeigt, wie breit eine Stueckliste aufgeloest ist, welche Komponenten exklusiv sind und wo der Endbestand kritisch wird.", "Shows how broadly a BOM is exploded, which components are exclusive and where final stock becomes critical."),
        ("Zeigt Wiederverwendung, Abhaengigkeit und Reichweite einer Komponente ueber alle gefundenen Elternmaterialien.", "Shows reuse, dependency and reach of a component across all parent materials found."),
        ("Lesart Top-Down: {0} Kopfmaterialien enthalten {1} unterschiedliche Komponenten. Exklusivitaet und negativer Endbestand markieren die zuerst zu pruefenden Bauteile; Bestandswerte werden nicht ueber Stuecklisten summiert, damit gemeinsam verwendete Komponenten nicht doppelt bewertet werden.", "Top-down reading: {0} header materials contain {1} distinct components. Exclusivity and negative final stock identify the parts to check first; stock values are not summed across BOMs so shared components are not valued twice."),
        ("Lesart Bottom-Up: {0} Komponenten fuehren zu {1} Elternmaterialien. Hohe Balken zeigen grosse Wiederverwendung und damit breite Auswirkung bei Aenderung oder Ausfall; eine Einzelverwendung zeigt dagegen konzentrierte Abhaengigkeit.", "Bottom-up reading: {0} components lead to {1} parent materials. High bars show broad reuse and therefore wide impact from a change or failure; single use instead indicates concentrated dependency.")
    ];
}
