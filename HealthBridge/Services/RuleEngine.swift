import Foundation
import Combine

// MARK: - Rule Engine
@MainActor
class RuleEngine: ObservableObject {
    static let shared = RuleEngine()

    @Published var rules: [HealthDataType: MergeRule] = [:]
    @Published var isLoaded = false

    private let storage = RuleStorageManager()
    private let sourceManager = SourceManager.shared

    private init() {
        loadRules()
    }

    // MARK: - Rule Loading

    func loadRules() {
        let savedRules = storage.loadRules()

        if savedRules.isEmpty {
            // Initialize with defaults
            for dataType in HealthDataType.allCases {
                rules[dataType] = MergeRule.defaultRule(for: dataType)
            }
            saveRules()
        } else {
            rules = savedRules
        }

        isLoaded = true
    }

    func saveRules() {
        storage.saveRules(rules)
    }

    // MARK: - Rule Access

    func getRule(for dataType: HealthDataType) -> MergeRule {
        return rules[dataType] ?? MergeRule.defaultRule(for: dataType)
    }

    func setRule(_ rule: MergeRule, for dataType: HealthDataType) {
        rules[dataType] = rule
        saveRules()
    }

    func resetToDefault(for dataType: HealthDataType) {
        rules[dataType] = MergeRule.defaultRule(for: dataType)
        saveRules()
    }

    func resetAllToDefaults() {
        for dataType in HealthDataType.allCases {
            rules[dataType] = MergeRule.defaultRule(for: dataType)
        }
        saveRules()
    }

    // MARK: - Rule Application

    func applyRule(
        to readings: [SourceReading],
        dataType: HealthDataType
    ) -> RuleApplicationResult {
        let rule = getRule(for: dataType)

        // Filter out empty/zero readings for most strategies
        let validReadings = readings.filter { $0.value > 0 || $0.quality == .complete }

        guard !validReadings.isEmpty else {
            return RuleApplicationResult(
                selectedReading: nil,
                strategy: rule.strategy,
                confidence: .low,
                reason: "Keine gültigen Werte vorhanden"
            )
        }

        // If only one valid reading, no conflict
        if validReadings.count == 1 {
            return RuleApplicationResult(
                selectedReading: validReadings[0],
                strategy: rule.strategy,
                confidence: .high,
                reason: "Nur eine Quelle verfügbar"
            )
        }

        // Apply strategy
        switch rule.strategy {
        case .exclusive:
            return applyExclusiveStrategy(readings: validReadings, rule: rule)

        case .priority:
            return applyPriorityStrategy(readings: validReadings, rule: rule, dataType: dataType)

        case .higherWins:
            return applyHigherWinsStrategy(readings: validReadings, rule: rule)

        case .lowerWins:
            return applyLowerWinsStrategy(readings: validReadings, rule: rule)

        case .average:
            return applyAverageStrategy(readings: validReadings, rule: rule)

        case .coverage:
            return applyCoverageStrategy(readings: validReadings, rule: rule)

        case .coverageThenHigher:
            return applyCoverageThenHigherStrategy(readings: validReadings, rule: rule)

        case .mostRecent:
            return applyMostRecentStrategy(readings: validReadings, rule: rule)

        case .manual:
            return RuleApplicationResult(
                selectedReading: nil,
                strategy: .manual,
                confidence: .requiresManual,
                reason: "Manuelle Entscheidung erforderlich"
            )
        }
    }

    // MARK: - Strategy Implementations

    private func applyExclusiveStrategy(
        readings: [SourceReading],
        rule: MergeRule
    ) -> RuleApplicationResult {
        // If primary source is specified, use it
        if let primaryId = rule.primarySourceId,
           let reading = readings.first(where: { $0.sourceId == primaryId }) {
            return RuleApplicationResult(
                selectedReading: reading,
                strategy: .exclusive,
                confidence: .high,
                reason: "Exklusive Quelle: \(reading.sourceName)"
            )
        }

        // Otherwise use highest priority source
        let sorted = readings.sorted { $0.sourceCategory.priority > $1.sourceCategory.priority }
        if let first = sorted.first {
            return RuleApplicationResult(
                selectedReading: first,
                strategy: .exclusive,
                confidence: .high,
                reason: "Höchste Priorität: \(first.sourceName)"
            )
        }

        return RuleApplicationResult(
            selectedReading: nil,
            strategy: .exclusive,
            confidence: .low,
            reason: "Keine geeignete Quelle gefunden"
        )
    }

    private func applyPriorityStrategy(
        readings: [SourceReading],
        rule: MergeRule,
        dataType: HealthDataType
    ) -> RuleApplicationResult {
        // Sort by user-defined priority, then by category priority
        let sorted = readings.sorted { r1, r2 in
            let p1 = rule.sourcePriorities[r1.sourceId] ?? r1.sourceCategory.priority
            let p2 = rule.sourcePriorities[r2.sourceId] ?? r2.sourceCategory.priority
            return p1 > p2
        }

        if let first = sorted.first {
            return RuleApplicationResult(
                selectedReading: first,
                strategy: .priority,
                confidence: .high,
                reason: "Höchste Priorität: \(first.sourceName)"
            )
        }

        return RuleApplicationResult(
            selectedReading: nil,
            strategy: .priority,
            confidence: .low,
            reason: "Keine Quelle mit Priorität gefunden"
        )
    }

    private func applyHigherWinsStrategy(
        readings: [SourceReading],
        rule: MergeRule
    ) -> RuleApplicationResult {
        let sorted = readings.sorted { $0.value > $1.value }

        if let highest = sorted.first {
            // Check if there's a significant difference
            let values = readings.map { $0.value }
            let spread = (values.max() ?? 0) - (values.min() ?? 0)
            let avgValue = values.reduce(0, +) / Double(values.count)
            let spreadPercent = avgValue > 0 ? (spread / avgValue) * 100 : 0

            let confidence: RuleConfidence = spreadPercent < 10 ? .high : .medium

            return RuleApplicationResult(
                selectedReading: highest,
                strategy: .higherWins,
                confidence: confidence,
                reason: "Höchster Wert: \(highest.formattedValue) von \(highest.sourceName)"
            )
        }

        return RuleApplicationResult(
            selectedReading: nil,
            strategy: .higherWins,
            confidence: .low,
            reason: "Keine Werte zum Vergleich"
        )
    }

    private func applyLowerWinsStrategy(
        readings: [SourceReading],
        rule: MergeRule
    ) -> RuleApplicationResult {
        let sorted = readings.sorted { $0.value < $1.value }

        if let lowest = sorted.first {
            return RuleApplicationResult(
                selectedReading: lowest,
                strategy: .lowerWins,
                confidence: .medium,
                reason: "Niedrigster Wert: \(lowest.formattedValue) von \(lowest.sourceName)"
            )
        }

        return RuleApplicationResult(
            selectedReading: nil,
            strategy: .lowerWins,
            confidence: .low,
            reason: "Keine Werte zum Vergleich"
        )
    }

    private func applyAverageStrategy(
        readings: [SourceReading],
        rule: MergeRule
    ) -> RuleApplicationResult {
        let values = readings.map { $0.value }
        let average = values.reduce(0, +) / Double(values.count)

        // Create a synthetic reading for the average
        let syntheticReading = SourceReading(
            sourceId: HealthBridgeConstants.bundleIdentifier,
            sourceName: "Durchschnitt",
            sourceCategory: .healthBridge,
            value: average,
            timestamp: readings.first?.timestamp ?? Date(),
            quality: .complete
        )

        return RuleApplicationResult(
            selectedReading: syntheticReading,
            strategy: .average,
            confidence: .medium,
            reason: "Durchschnitt aus \(readings.count) Quellen"
        )
    }

    private func applyCoverageStrategy(
        readings: [SourceReading],
        rule: MergeRule
    ) -> RuleApplicationResult {
        // Prefer readings with complete quality
        let completeReadings = readings.filter { $0.quality == .complete }

        if completeReadings.count == 1 {
            return RuleApplicationResult(
                selectedReading: completeReadings[0],
                strategy: .coverage,
                confidence: .high,
                reason: "Einzige Quelle mit vollständigen Daten: \(completeReadings[0].sourceName)"
            )
        }

        // If multiple complete readings, fall back to priority
        if !completeReadings.isEmpty {
            let sorted = completeReadings.sorted { $0.sourceCategory.priority > $1.sourceCategory.priority }
            if let first = sorted.first {
                return RuleApplicationResult(
                    selectedReading: first,
                    strategy: .coverage,
                    confidence: .medium,
                    reason: "Mehrere Quellen verfügbar, gewählt: \(first.sourceName)"
                )
            }
        }

        // No complete readings, use any reading with highest priority
        let sorted = readings.sorted { $0.sourceCategory.priority > $1.sourceCategory.priority }
        if let first = sorted.first {
            return RuleApplicationResult(
                selectedReading: first,
                strategy: .coverage,
                confidence: .low,
                reason: "Keine vollständigen Daten, gewählt: \(first.sourceName)"
            )
        }

        return RuleApplicationResult(
            selectedReading: nil,
            strategy: .coverage,
            confidence: .low,
            reason: "Keine Quelle mit Daten gefunden"
        )
    }

    private func applyCoverageThenHigherStrategy(
        readings: [SourceReading],
        rule: MergeRule
    ) -> RuleApplicationResult {
        // First check if one source has data and others don't (coverage)
        let nonZeroReadings = readings.filter { $0.value > 0 }
        let zeroReadings = readings.filter { $0.value == 0 }

        // If only one source has data, it wins on coverage
        if nonZeroReadings.count == 1 && !zeroReadings.isEmpty {
            return RuleApplicationResult(
                selectedReading: nonZeroReadings[0],
                strategy: .coverageThenHigher,
                confidence: .high,
                reason: "Einzige Quelle mit Daten: \(nonZeroReadings[0].sourceName)"
            )
        }

        // Multiple sources have data, use higher wins
        if nonZeroReadings.count > 1 {
            let sorted = nonZeroReadings.sorted { $0.value > $1.value }
            if let highest = sorted.first {
                return RuleApplicationResult(
                    selectedReading: highest,
                    strategy: .coverageThenHigher,
                    confidence: .medium,
                    reason: "Höherer Wert bei Konflikt: \(highest.formattedValue) von \(highest.sourceName)"
                )
            }
        }

        // Fallback
        if let first = readings.first {
            return RuleApplicationResult(
                selectedReading: first,
                strategy: .coverageThenHigher,
                confidence: .low,
                reason: "Fallback auf erste Quelle"
            )
        }

        return RuleApplicationResult(
            selectedReading: nil,
            strategy: .coverageThenHigher,
            confidence: .low,
            reason: "Keine Daten verfügbar"
        )
    }

    private func applyMostRecentStrategy(
        readings: [SourceReading],
        rule: MergeRule
    ) -> RuleApplicationResult {
        let sorted = readings.sorted { $0.timestamp > $1.timestamp }

        if let mostRecent = sorted.first {
            return RuleApplicationResult(
                selectedReading: mostRecent,
                strategy: .mostRecent,
                confidence: .high,
                reason: "Neuester Wert von \(mostRecent.sourceName)"
            )
        }

        return RuleApplicationResult(
            selectedReading: nil,
            strategy: .mostRecent,
            confidence: .low,
            reason: "Keine Zeitstempel verfügbar"
        )
    }

    // MARK: - Threshold Check

    func shouldRequestManualReview(
        readings: [SourceReading],
        dataType: HealthDataType
    ) -> Bool {
        let rule = getRule(for: dataType)

        guard let threshold = rule.thresholdForManualReview else {
            return rule.strategy == .manual
        }

        let values = readings.map { $0.value }.filter { $0 > 0 }
        guard values.count >= 2,
              let min = values.min(),
              let max = values.max(),
              min > 0 else {
            return false
        }

        let percentDiff = (max - min) / min * 100
        return percentDiff > threshold
    }
}

// MARK: - Rule Application Result
struct RuleApplicationResult {
    let selectedReading: SourceReading?
    let strategy: MergeStrategy
    let confidence: RuleConfidence
    let reason: String

    var resolvedValue: Double? {
        selectedReading?.value
    }

    var winningSourceId: String? {
        selectedReading?.sourceId
    }
}

enum RuleConfidence: String, Codable {
    case high = "high"
    case medium = "medium"
    case low = "low"
    case requiresManual = "requires_manual"

    var displayName: String {
        switch self {
        case .high: return "Hohe Sicherheit"
        case .medium: return "Mittlere Sicherheit"
        case .low: return "Geringe Sicherheit"
        case .requiresManual: return "Manuelle Prüfung"
        }
    }

    var icon: String {
        switch self {
        case .high: return "checkmark.seal.fill"
        case .medium: return "checkmark.seal"
        case .low: return "questionmark.circle"
        case .requiresManual: return "hand.raised.fill"
        }
    }
}

// MARK: - Rule Storage Manager
class RuleStorageManager {
    private let userDefaults = UserDefaults.standard
    private let rulesKey = "healthbridge.merge.rules"

    func saveRules(_ rules: [HealthDataType: MergeRule]) {
        do {
            let data = try JSONEncoder().encode(rules)
            userDefaults.set(data, forKey: rulesKey)
        } catch {
            print("Failed to save rules: \(error)")
        }
    }

    func loadRules() -> [HealthDataType: MergeRule] {
        guard let data = userDefaults.data(forKey: rulesKey) else {
            return [:]
        }

        do {
            return try JSONDecoder().decode([HealthDataType: MergeRule].self, from: data)
        } catch {
            print("Failed to load rules: \(error)")
            return [:]
        }
    }
}

// MARK: - Blood Pressure Handler
class BloodPressureHandler {
    static let shared = BloodPressureHandler()

    struct ValidationResult {
        let isValid: Bool
        let issues: [String]
    }

    func validate(systolic: Double, diastolic: Double) -> ValidationResult {
        var issues: [String] = []

        // Range validation
        if systolic < 70 || systolic > 200 {
            issues.append("Systolischer Wert ausserhalb des Normalbereichs (70-200 mmHg)")
        }

        if diastolic < 40 || diastolic > 130 {
            issues.append("Diastolischer Wert ausserhalb des Normalbereichs (40-130 mmHg)")
        }

        // Plausibility check
        if diastolic >= systolic {
            issues.append("Diastolischer Wert muss kleiner als systolischer Wert sein")
        }

        if systolic - diastolic < 20 {
            issues.append("Pulsdruck zu gering (< 20 mmHg)")
        }

        if systolic - diastolic > 100 {
            issues.append("Pulsdruck zu hoch (> 100 mmHg)")
        }

        return ValidationResult(isValid: issues.isEmpty, issues: issues)
    }

    func classifyBloodPressure(systolic: Double, diastolic: Double) -> BloodPressureClassification {
        if systolic < 120 && diastolic < 80 {
            return .normal
        } else if systolic < 130 && diastolic < 80 {
            return .elevated
        } else if systolic < 140 || diastolic < 90 {
            return .hypertensionStage1
        } else if systolic < 180 || diastolic < 120 {
            return .hypertensionStage2
        } else {
            return .hypertensiveCrisis
        }
    }

    enum BloodPressureClassification: String {
        case normal = "Normal"
        case elevated = "Erhöht"
        case hypertensionStage1 = "Bluthochdruck Stufe 1"
        case hypertensionStage2 = "Bluthochdruck Stufe 2"
        case hypertensiveCrisis = "Hypertensive Krise"

        var color: String {
            switch self {
            case .normal: return "green"
            case .elevated: return "yellow"
            case .hypertensionStage1: return "orange"
            case .hypertensionStage2: return "red"
            case .hypertensiveCrisis: return "purple"
            }
        }

        var recommendation: String {
            switch self {
            case .normal:
                return "Weiter so! Regelmässige Kontrolle empfohlen."
            case .elevated:
                return "Lebensstiländerungen empfohlen. Mehr Bewegung, weniger Salz."
            case .hypertensionStage1:
                return "Arztbesuch empfohlen. Möglicherweise Medikation erforderlich."
            case .hypertensionStage2:
                return "Zeitnaher Arztbesuch erforderlich. Medikation wahrscheinlich notwendig."
            case .hypertensiveCrisis:
                return "SOFORT medizinische Hilfe aufsuchen!"
            }
        }
    }
}
