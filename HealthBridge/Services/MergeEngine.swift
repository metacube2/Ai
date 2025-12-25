import Foundation
import Combine

// MARK: - Merge Engine
@MainActor
class MergeEngine: ObservableObject {
    static let shared = MergeEngine()

    private let ruleEngine = RuleEngine.shared
    private let dataReader = DataReader.shared

    @Published var pendingMerges: [MergeOperation] = []
    @Published var completedMerges: [MergeOperation] = []
    @Published var isMerging = false
    @Published var mergeProgress: Double = 0

    private init() {}

    // MARK: - Analyze Window

    func analyze(windowData: TimeWindowData) -> WindowAnalysis {
        let readings = windowData.readings
        let dataType = windowData.dataType

        // No analysis needed for single reading
        if readings.count <= 1 {
            return WindowAnalysis(
                windowData: windowData,
                hasConflict: false,
                conflictSeverity: nil,
                recommendedReading: readings.first,
                alternativeReadings: [],
                confidence: .high,
                analysisNotes: readings.isEmpty ? "Keine Daten" : "Einzelne Quelle"
            )
        }

        // Apply rule to get recommendation
        let result = ruleEngine.applyRule(to: readings, dataType: dataType)

        // Calculate conflict severity
        let values = readings.map { $0.value }.filter { $0 > 0 }
        var severity: ConflictSeverity? = nil

        if values.count >= 2, let min = values.min(), let max = values.max(), min > 0 {
            let percentDiff = (max - min) / min * 100
            if percentDiff >= 5 {
                if percentDiff < 10 { severity = .minor }
                else if percentDiff < 25 { severity = .moderate }
                else if percentDiff < 50 { severity = .significant }
                else { severity = .major }
            }
        }

        let alternativeReadings = readings.filter { $0.id != result.selectedReading?.id }

        return WindowAnalysis(
            windowData: windowData,
            hasConflict: windowData.hasConflict,
            conflictSeverity: severity,
            recommendedReading: result.selectedReading,
            alternativeReadings: alternativeReadings,
            confidence: result.confidence,
            analysisNotes: result.reason
        )
    }

    // MARK: - Resolve Conflict

    func resolveConflict(_ conflict: Conflict, using result: RuleApplicationResult) -> ConflictResolution? {
        guard let selectedReading = result.selectedReading else {
            return nil
        }

        return ConflictResolution(
            resolvedValue: selectedReading.value,
            secondaryResolvedValue: selectedReading.secondaryValue,
            winningSourceId: selectedReading.sourceId,
            strategy: result.strategy,
            isManual: result.strategy == .manual
        )
    }

    func resolveConflictManually(
        _ conflict: Conflict,
        selectedReadingId: UUID
    ) -> ConflictResolution? {
        guard let selectedReading = conflict.readings.first(where: { $0.id == selectedReadingId }) else {
            return nil
        }

        return ConflictResolution(
            resolvedValue: selectedReading.value,
            secondaryResolvedValue: selectedReading.secondaryValue,
            winningSourceId: selectedReading.sourceId,
            strategy: .manual,
            isManual: true
        )
    }

    // MARK: - Create Merged Record

    func createMergedRecord(from conflict: Conflict, resolution: ConflictResolution) -> MergedRecord {
        return MergedRecord(
            id: UUID(),
            dataType: conflict.dataType,
            timeWindow: conflict.timeWindow,
            value: resolution.resolvedValue,
            secondaryValue: resolution.secondaryResolvedValue,
            originalSourceId: resolution.winningSourceId,
            strategy: resolution.strategy,
            createdAt: Date(),
            metadata: [
                "conflictId": conflict.id.uuidString,
                "originalSourceCount": String(conflict.readings.count),
                "isManualResolution": String(resolution.isManual)
            ]
        )
    }

    // MARK: - Batch Processing

    func processConflicts(_ conflicts: [Conflict]) async -> [MergeOperation] {
        isMerging = true
        defer { isMerging = false }

        var operations: [MergeOperation] = []

        for (index, conflict) in conflicts.enumerated() {
            mergeProgress = Double(index) / Double(conflicts.count)

            let result = ruleEngine.applyRule(to: conflict.readings, dataType: conflict.dataType)

            if result.confidence == .requiresManual ||
               ruleEngine.shouldRequestManualReview(readings: conflict.readings, dataType: conflict.dataType) {
                // Add to pending for manual review
                let operation = MergeOperation(
                    conflict: conflict,
                    status: .pendingManualReview,
                    result: result
                )
                pendingMerges.append(operation)
                operations.append(operation)
            } else if let resolution = resolveConflict(conflict, using: result) {
                // Auto-resolve
                let mergedRecord = createMergedRecord(from: conflict, resolution: resolution)
                let operation = MergeOperation(
                    conflict: conflict,
                    status: .resolved,
                    result: result,
                    resolution: resolution,
                    mergedRecord: mergedRecord
                )
                completedMerges.append(operation)
                operations.append(operation)
            }
        }

        mergeProgress = 1.0
        return operations
    }

    // MARK: - Daily Merge

    func performDailyMerge(for date: Date) async throws -> DailyMergeReport {
        let calendar = Calendar.current
        let startOfDay = calendar.startOfDay(for: date)
        let endOfDay = calendar.date(byAdding: .day, value: 1, to: startOfDay)!

        var report = DailyMergeReport(date: date)

        for dataType in HealthDataType.allCases {
            do {
                let windowData = try await dataReader.fetchData(
                    for: dataType,
                    from: startOfDay,
                    to: endOfDay
                )

                let conflictWindows = windowData.filter { $0.hasConflict }
                report.totalConflicts += conflictWindows.count

                for window in conflictWindows {
                    let analysis = analyze(windowData: window)

                    if analysis.confidence == .requiresManual {
                        report.pendingManualReview += 1
                    } else {
                        report.autoResolved += 1
                    }

                    report.analysesByType[dataType, default: []].append(analysis)
                }
            } catch {
                report.errors.append("Fehler bei \(dataType.displayName): \(error.localizedDescription)")
            }
        }

        return report
    }
}

// MARK: - Supporting Types

struct WindowAnalysis {
    let windowData: TimeWindowData
    let hasConflict: Bool
    let conflictSeverity: ConflictSeverity?
    let recommendedReading: SourceReading?
    let alternativeReadings: [SourceReading]
    let confidence: RuleConfidence
    let analysisNotes: String

    var recommendedValue: Double? {
        recommendedReading?.value
    }

    var valueDifference: Double? {
        guard let recommended = recommendedReading?.value,
              let alternative = alternativeReadings.first?.value else {
            return nil
        }
        return abs(recommended - alternative)
    }
}

struct MergeOperation: Identifiable {
    let id = UUID()
    let conflict: Conflict
    var status: MergeStatus
    let result: RuleApplicationResult
    var resolution: ConflictResolution?
    var mergedRecord: MergedRecord?
    let createdAt = Date()
    var processedAt: Date?

    enum MergeStatus {
        case pending
        case pendingManualReview
        case resolved
        case written
        case failed
    }
}

struct MergedRecord: Identifiable, Codable {
    let id: UUID
    let dataType: HealthDataType
    let timeWindow: TimeWindow
    let value: Double
    let secondaryValue: Double?
    let originalSourceId: String
    let strategy: MergeStrategy
    let createdAt: Date
    var writtenAt: Date?
    var healthKitRecordId: String?
    var metadata: [String: String]

    var formattedValue: String {
        if value == floor(value) {
            return String(format: "%.0f", value)
        }
        return String(format: "%.1f", value)
    }
}

struct DailyMergeReport {
    let date: Date
    var totalConflicts = 0
    var autoResolved = 0
    var pendingManualReview = 0
    var analysesByType: [HealthDataType: [WindowAnalysis]] = [:]
    var errors: [String] = []
    let generatedAt = Date()

    var successRate: Double {
        guard totalConflicts > 0 else { return 1.0 }
        return Double(autoResolved) / Double(totalConflicts)
    }

    var summary: String {
        if totalConflicts == 0 {
            return "Keine Konflikte gefunden"
        }
        return "\(autoResolved)/\(totalConflicts) automatisch gelöst, \(pendingManualReview) zur Prüfung"
    }
}
