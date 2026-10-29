using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services.Projects;

// DTOs zu Projektstrukturplan, Decision Log und Risiko-Burndown (Sitzung 2026-10-29).

public sealed record PmRiskBurndownPoint(DateTime Day, int Active, int Critical, int TotalScore, double ExpectedValueChf, double ContingencyChf);

// Eine Zeile des Projektstrukturplans. Code wie ein PSP-Element: Projektschluessel und Gliederungsnummern (LIN.01.02).
// Own = am Element erfasst, Total = mit allen Unterelementen.
public sealed record PmWbsRow(
    int TaskId, string Code, int Level, string Key, string Type, string Title, bool HasChildren, bool IsMilestone,
    DateTime? Start, DateTime? End, double OwnHours, double OwnCostChf, double TotalHours, double TotalCostChf,
    string CostType, string AssigneeName);

public sealed record PmWbs(string ProjectKey, string ProjectName, IReadOnlyList<PmWbsRow> Rows, double TotalHours, double TotalCostChf,
    double CapexChf, double OpexChf, double OpenCostTypeChf);

public sealed record PmDecisionView(
    int Id, int Number, string Key, string Title, string Decision, string Rationale, string Alternatives, string Status,
    DateTime? DecidedOn, string DecidedBy, int? TaskId, string TaskKey, string AuthorName, DateTime UpdatedAtUtc, bool IsArchived);

public sealed class PmDecisionInput
{
    public int? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public string Alternatives { get; set; } = string.Empty;
    public string Status { get; set; } = PmDecisionStatuses.Proposed;
    public DateTime? DecidedOn { get; set; }
    public string DecidedBy { get; set; } = string.Empty;
    public int? TaskId { get; set; }
}
