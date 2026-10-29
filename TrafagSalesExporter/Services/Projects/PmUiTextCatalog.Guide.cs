namespace TrafagSalesExporter.Services.Projects;

// Texte: Guide (Rundgang, Assistent, Projektpruefung) und die Reiter im Projekt. Format: new("Deutsch", es, it, hi, sq, tr, tlh, gsw).
internal static partial class PmUiTextCatalog
{
    private static Entry[] GuideEntries =>
    [
        new("Raster & Gantt", "Cuadrícula y Gantt", "Griglia e Gantt", "ग्रिड और गैंट", "Rrjetë dhe Gantt", "Tablo ve Gantt", "Haw'DIch Gantt je", "Raschter & Gantt"),
        new("Risiken", "Riesgos", "Rischi", "जोखिम", "Rreziqet", "Riskler", "Qob", "Risike"),
    ];
}
