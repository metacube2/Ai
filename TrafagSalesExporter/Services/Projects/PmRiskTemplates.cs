using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services.Projects;

// PMO-Vorlage fuer Risiken (Sitzung 2026-10-29: „Templates zur Verfuegung stellen, damit jeder Projektleiter gleich
// bewertet, qualitativ und quantitativ“). Einheitliche Skala fuer Wahrscheinlichkeit und Auswirkung und Standardrisiken
// je Projektart. Die Standardrisiken sind ein Vorschlag zum Start, nicht verbindlich; das PMO kann sie hier anpassen.
public static class PmRiskTemplates
{
    public sealed record ScaleLevel(int Value, string German, string English);

    public sealed record StandardRisk(string Title, string Description, string Category, int Probability, int Impact, string Mitigation);

    public static readonly IReadOnlyList<ScaleLevel> ProbabilityScale =
    [
        new(1, "Sehr unwahrscheinlich (bis 10 %)", "Very unlikely (up to 10 %)"),
        new(2, "Unwahrscheinlich (bis 30 %)", "Unlikely (up to 30 %)"),
        new(3, "Möglich (bis 50 %)", "Possible (up to 50 %)"),
        new(4, "Wahrscheinlich (bis 70 %)", "Likely (up to 70 %)"),
        new(5, "Fast sicher (über 70 %)", "Almost certain (over 70 %)")
    ];

    public static readonly IReadOnlyList<ScaleLevel> ImpactScale =
    [
        new(1, "Unbedeutend: wenige Tage, unter 5'000 CHF, keine Qualitätsfolge", "Negligible: a few days, below CHF 5,000, no quality effect"),
        new(2, "Gering: bis 2 Wochen, bis 25'000 CHF, behebbar im Projekt", "Minor: up to 2 weeks, up to CHF 25,000, fixable within the project"),
        new(3, "Spürbar: bis 1 Monat, bis 100'000 CHF, Meilenstein gefährdet", "Moderate: up to 1 month, up to CHF 100,000, milestone at risk"),
        new(4, "Gross: bis 3 Monate, bis 500'000 CHF, Kunde oder Zulassung betroffen", "Major: up to 3 months, up to CHF 500,000, customer or approval affected"),
        new(5, "Existenziell: über 3 Monate oder 500'000 CHF, Projekt in Frage", "Severe: over 3 months or CHF 500,000, project in question")
    ];

    public static IReadOnlyList<StandardRisk> StandardRisks(string template) => template switch
    {
        PmTemplates.ProductDevelopment =>
        [
            new("Spezifikation unvollständig oder ändert sich spät", "Anforderungen von Kunde oder Markt sind beim Design-Freeze nicht stabil.",
                PmRiskCategories.Quality, 3, 4, "Lastenheft mit Vertrieb und Kunde abnehmen, Änderungen nach M2 nur über Entscheid."),
            new("Lieferzeit kritischer Bauteile", "Elektronik oder Sonderteile mit langer oder unsicherer Lieferzeit.",
                PmRiskCategories.Schedule, 3, 3, "Kritische Bauteile früh identifizieren, Zweitquelle prüfen, Muster vorbestellen."),
            new("Zulassung oder Zertifizierung verzögert", "Prüfungen (z. B. EMV, ATEX, Bahnnormen) bestehen nicht beim ersten Anlauf.",
                PmRiskCategories.Compliance, 2, 4, "Vorprüfung im eigenen Labor, Prüfstelle früh einbinden."),
            new("Design-FMEA zeigt kritische Fehlerarten spät", "Hohe Risikoprioritäten erst nach dem Design-Freeze erkannt.",
                PmRiskCategories.Technology, 2, 4, "Design-FMEA vor M2 abschliessen, Massnahmen in den Plan übernehmen."),
            new("Industrialisierung: Betriebsmittel nicht rechtzeitig", "Vorrichtungen, Prüfmittel oder Software für die Fertigung fehlen zum Serienstart.",
                PmRiskCategories.Resources, 3, 3, "Fertigungs- und Softwarekonzept parallel zum Design, Betriebsmittelbau früh einplanen."),
            new("First Pass Yield nach M3 unter Ziel", "Kinderkrankheiten in der Serie, Ausschuss und Nacharbeit.",
                PmRiskCategories.Quality, 3, 2, "Nullserie mit Auswertung, Optimierungsphase mit Zielwert und Verantwortlichem."),
            new("Kosten über Budget", "Herstellkosten oder Projektkosten liegen über dem Plan.",
                PmRiskCategories.Cost, 3, 3, "Kostenziel je Arbeitspaket, Soll-Ist im Projektstrukturplan monatlich prüfen.")
        ],
        PmTemplates.ItProject =>
        [
            new("Anforderungen unklar oder wechseln", "Fachbereich formuliert Anforderungen erst während der Realisierung.",
                PmRiskCategories.Quality, 3, 3, "Anwendervertreter benennen, Abnahmekriterien in der Initialisierung festlegen."),
            new("Schlüsselperson fällt aus", "Wissen liegt bei einer Person (intern oder extern).",
                PmRiskCategories.Resources, 3, 4, "Stellvertretung bestimmen, Dokumentation laufend nachführen."),
            new("Schnittstelle zu SAP oder Drittsystem", "Daten, Berechtigungen oder Firewall verzögern die Anbindung.",
                PmRiskCategories.Technology, 3, 3, "Schnittstelle früh mit Testdaten durchstechen, Berechtigungen beantragen."),
            new("Datenschutz und Informationssicherheit", "Personen- oder Geschäftsdaten werden ohne geprüfte Regeln verarbeitet.",
                PmRiskCategories.Compliance, 2, 4, "Datenklassifizierung und Rechtekonzept vor der Einführung."),
            new("Akzeptanz bei den Anwendern", "Das neue Werkzeug wird nicht genutzt.",
                PmRiskCategories.External, 3, 3, "Anwender früh testen lassen, Schulung und Ansprechpartner einplanen.")
        ],
        _ =>
        [
            new("Termin gefährdet", "Der Endtermin ist ohne Puffer geplant.", PmRiskCategories.Schedule, 3, 3, "Puffer einplanen, kritischen Pfad wöchentlich prüfen."),
            new("Ressourcen fehlen", "Beteiligte sind durch das Tagesgeschäft gebunden.", PmRiskCategories.Resources, 3, 3, "Kapazität mit den Vorgesetzten verbindlich vereinbaren.")
        ]
    };
}
