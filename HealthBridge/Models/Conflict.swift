import Foundation

// MARK: - Conflict
struct Conflict: Identifiable, Codable {
    let id: UUID
    let dataType: HealthDataType
    let timeWindow: TimeWindow
    var readings: [SourceReading]
    var status: ConflictStatus
    var resolution: ConflictResolution?
    var appliedStrategy: MergeStrategy?
    let detectedAt: Date
    var resolvedAt: Date?

    init(
        id: UUID = UUID(),
        dataType: HealthDataType,
        timeWindow: TimeWindow,
        readings: [SourceReading],
        status: ConflictStatus = .pending,
        resolution: ConflictResolution? = nil,
        appliedStrategy: MergeStrategy? = nil,
        detectedAt: Date = Date(),
        resolvedAt: Date? = nil
    ) {
        self.id = id
        self.dataType = dataType
        self.timeWindow = timeWindow
        self.readings = readings
        self.status = status
        self.resolution = resolution
        self.appliedStrategy = appliedStrategy
        self.detectedAt = detectedAt
        self.resolvedAt = resolvedAt
    }

    var valueDifference: Double {
        guard readings.count >= 2 else { return 0 }
        let values = readings.map { $0.value }
        return (values.max() ?? 0) - (values.min() ?? 0)
    }

    var percentageDifference: Double {
        guard readings.count >= 2 else { return 0 }
        let values = readings.map { $0.value }
        guard let min = values.min(), min > 0 else { return 0 }
        guard let max = values.max() else { return 0 }
        return ((max - min) / min) * 100
    }

    var severity: ConflictSeverity {
        let pctDiff = percentageDifference
        if pctDiff < 5 { return .minor }
        if pctDiff < 20 { return .moderate }
        if pctDiff < 50 { return .significant }
        return .major
    }

    var highestValueReading: SourceReading? {
        readings.max(by: { $0.value < $1.value })
    }

    var lowestValueReading: SourceReading? {
        readings.min(by: { $0.value < $1.value })
    }

    var primarySourceReading: SourceReading? {
        readings.max(by: { $0.sourceCategory.priority < $1.sourceCategory.priority })
    }
}

// MARK: - Conflict Status
enum ConflictStatus: String, Codable {
    case pending = "pending"
    case resolved = "resolved"
    case manualReview = "manual_review"
    case ignored = "ignored"

    var displayName: String {
        switch self {
        case .pending: return "Offen"
        case .resolved: return "Gelöst"
        case .manualReview: return "Manuelle Prüfung"
        case .ignored: return "Ignoriert"
        }
    }

    var icon: String {
        switch self {
        case .pending: return "clock.fill"
        case .resolved: return "checkmark.circle.fill"
        case .manualReview: return "hand.raised.fill"
        case .ignored: return "eye.slash.fill"
        }
    }
}

// MARK: - Conflict Severity
enum ConflictSeverity: String, Codable {
    case minor = "minor"
    case moderate = "moderate"
    case significant = "significant"
    case major = "major"

    var displayName: String {
        switch self {
        case .minor: return "Gering"
        case .moderate: return "Moderat"
        case .significant: return "Erheblich"
        case .major: return "Gross"
        }
    }

    var color: String {
        switch self {
        case .minor: return "green"
        case .moderate: return "yellow"
        case .significant: return "orange"
        case .major: return "red"
        }
    }
}

// MARK: - Conflict Resolution
struct ConflictResolution: Codable {
    let resolvedValue: Double
    let secondaryResolvedValue: Double? // For blood pressure
    let winningSourceId: String
    let strategy: MergeStrategy
    let isManual: Bool
    let resolvedAt: Date
    let notes: String?

    init(
        resolvedValue: Double,
        secondaryResolvedValue: Double? = nil,
        winningSourceId: String,
        strategy: MergeStrategy,
        isManual: Bool = false,
        resolvedAt: Date = Date(),
        notes: String? = nil
    ) {
        self.resolvedValue = resolvedValue
        self.secondaryResolvedValue = secondaryResolvedValue
        self.winningSourceId = winningSourceId
        self.strategy = strategy
        self.isManual = isManual
        self.resolvedAt = resolvedAt
        self.notes = notes
    }
}

// MARK: - Merge Strategy
enum MergeStrategy: String, Codable, CaseIterable, Identifiable {
    case exclusive = "exclusive"
    case priority = "priority"
    case higherWins = "higher_wins"
    case lowerWins = "lower_wins"
    case average = "average"
    case coverage = "coverage"
    case coverageThenHigher = "coverage_then_higher"
    case manual = "manual"
    case mostRecent = "most_recent"

    var id: String { rawValue }

    var displayName: String {
        switch self {
        case .exclusive: return "Exklusiv"
        case .priority: return "Priorität"
        case .higherWins: return "Höherer Wert"
        case .lowerWins: return "Niedrigerer Wert"
        case .average: return "Durchschnitt"
        case .coverage: return "Abdeckung"
        case .coverageThenHigher: return "Abdeckung + Höher"
        case .manual: return "Manuell"
        case .mostRecent: return "Neuester"
        }
    }

    var description: String {
        switch self {
        case .exclusive:
            return "Nur eine Quelle kann diesen Datentyp liefern"
        case .priority:
            return "Höchste Priorität gewinnt basierend auf Benutzereinstellungen"
        case .higherWins:
            return "Der grössere Wert wird verwendet (z.B. mehr Schritte = war aktiv)"
        case .lowerWins:
            return "Der kleinere Wert wird verwendet"
        case .average:
            return "Durchschnitt aller Quellen"
        case .coverage:
            return "Quelle mit Daten für dieses Zeitfenster"
        case .coverageThenHigher:
            return "Erst Abdeckung prüfen, dann höherer Wert bei Konflikt"
        case .manual:
            return "Benutzer entscheidet bei jedem Konflikt"
        case .mostRecent:
            return "Zuletzt erfasster Wert"
        }
    }

    var icon: String {
        switch self {
        case .exclusive: return "1.circle.fill"
        case .priority: return "list.number"
        case .higherWins: return "arrow.up.circle.fill"
        case .lowerWins: return "arrow.down.circle.fill"
        case .average: return "divide.circle.fill"
        case .coverage: return "square.fill.on.square.fill"
        case .coverageThenHigher: return "square.stack.3d.up.fill"
        case .manual: return "hand.raised.fill"
        case .mostRecent: return "clock.arrow.circlepath"
        }
    }
}

// MARK: - Merge Rule
struct MergeRule: Identifiable, Codable {
    let id: UUID
    let dataType: HealthDataType
    var strategy: MergeStrategy
    var primarySourceId: String?
    var fallbackSourceId: String?
    var sourcePriorities: [String: Int]
    var autoApply: Bool
    var thresholdForManualReview: Double? // Percentage difference threshold

    init(
        id: UUID = UUID(),
        dataType: HealthDataType,
        strategy: MergeStrategy,
        primarySourceId: String? = nil,
        fallbackSourceId: String? = nil,
        sourcePriorities: [String: Int] = [:],
        autoApply: Bool = true,
        thresholdForManualReview: Double? = nil
    ) {
        self.id = id
        self.dataType = dataType
        self.strategy = strategy
        self.primarySourceId = primarySourceId
        self.fallbackSourceId = fallbackSourceId
        self.sourcePriorities = sourcePriorities
        self.autoApply = autoApply
        self.thresholdForManualReview = thresholdForManualReview
    }

    static func defaultRule(for dataType: HealthDataType) -> MergeRule {
        switch dataType {
        case .bloodPressureSystolic, .bloodPressureDiastolic, .bloodOxygen,
             .heartRate, .restingHeartRate, .heartRateVariability, .respiratoryRate:
            return MergeRule(dataType: dataType, strategy: .exclusive)
        case .floorsClimbed:
            return MergeRule(dataType: dataType, strategy: .exclusive)
        case .steps, .distance, .activeEnergy:
            return MergeRule(dataType: dataType, strategy: .coverageThenHigher)
        case .sleep:
            return MergeRule(dataType: dataType, strategy: .priority)
        }
    }
}

// MARK: - Sync Record
struct SyncRecord: Identifiable, Codable {
    let id: UUID
    let dataType: HealthDataType
    let timeWindow: TimeWindow
    var readings: [SourceReading]
    var mergedValue: Double?
    var secondaryMergedValue: Double? // For blood pressure
    var strategy: MergeStrategy
    var status: SyncStatus
    var hasConflict: Bool
    var conflictId: UUID?
    let createdAt: Date
    var processedAt: Date?

    enum SyncStatus: String, Codable {
        case pending = "pending"
        case processing = "processing"
        case completed = "completed"
        case failed = "failed"
        case requiresManualReview = "requires_manual"
    }

    init(
        id: UUID = UUID(),
        dataType: HealthDataType,
        timeWindow: TimeWindow,
        readings: [SourceReading],
        mergedValue: Double? = nil,
        secondaryMergedValue: Double? = nil,
        strategy: MergeStrategy = .priority,
        status: SyncStatus = .pending,
        hasConflict: Bool = false,
        conflictId: UUID? = nil,
        createdAt: Date = Date(),
        processedAt: Date? = nil
    ) {
        self.id = id
        self.dataType = dataType
        self.timeWindow = timeWindow
        self.readings = readings
        self.mergedValue = mergedValue
        self.secondaryMergedValue = secondaryMergedValue
        self.strategy = strategy
        self.status = status
        self.hasConflict = hasConflict
        self.conflictId = conflictId
        self.createdAt = createdAt
        self.processedAt = processedAt
    }
}
