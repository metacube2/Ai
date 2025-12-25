import Foundation
import HealthKit

// MARK: - Health Source
struct HealthSource: Identifiable, Codable, Hashable {
    let id: String
    let bundleIdentifier: String
    let name: String
    let category: SourceCategory
    var supportedDataTypes: Set<HealthDataType>
    var lastActivityDate: Date?
    var userPriorities: [HealthDataType: Int]
    var isEnabled: Bool

    init(
        id: String = UUID().uuidString,
        bundleIdentifier: String,
        name: String,
        category: SourceCategory,
        supportedDataTypes: Set<HealthDataType> = [],
        lastActivityDate: Date? = nil,
        userPriorities: [HealthDataType: Int] = [:],
        isEnabled: Bool = true
    ) {
        self.id = id
        self.bundleIdentifier = bundleIdentifier
        self.name = name
        self.category = category
        self.supportedDataTypes = supportedDataTypes
        self.lastActivityDate = lastActivityDate
        self.userPriorities = userPriorities
        self.isEnabled = isEnabled
    }

    var displayName: String {
        if name.isEmpty {
            return bundleIdentifier.components(separatedBy: ".").last ?? bundleIdentifier
        }
        return name
    }

    var isHealthBridge: Bool {
        bundleIdentifier == HealthBridgeConstants.bundleIdentifier
    }

    func priority(for dataType: HealthDataType) -> Int {
        userPriorities[dataType] ?? category.priority
    }

    static func from(hkSource: HKSource) -> HealthSource {
        let category = classifySource(bundleId: hkSource.bundleIdentifier)
        return HealthSource(
            id: hkSource.bundleIdentifier,
            bundleIdentifier: hkSource.bundleIdentifier,
            name: hkSource.name,
            category: category
        )
    }

    private static func classifySource(bundleId: String) -> SourceCategory {
        let lowercased = bundleId.lowercased()

        if lowercased.contains("healthbridge") {
            return .healthBridge
        } else if lowercased.contains("apple.health") {
            return .iPhone
        } else if lowercased.contains("watch") || lowercased.contains("applewatch") {
            return .watch
        } else if lowercased.contains("huawei") || lowercased.contains("samsung") ||
                  lowercased.contains("fitbit") || lowercased.contains("garmin") ||
                  lowercased.contains("polar") || lowercased.contains("withings") {
            return .thirdPartyWatch
        } else {
            return .thirdPartyApp
        }
    }
}

// MARK: - Source Reading
struct SourceReading: Identifiable, Codable {
    let id: UUID
    let sourceId: String
    let sourceName: String
    let sourceCategory: SourceCategory
    let value: Double
    let secondaryValue: Double? // For blood pressure (diastolic)
    let timestamp: Date
    let originalRecordId: String?
    let quality: DataQuality

    init(
        id: UUID = UUID(),
        sourceId: String,
        sourceName: String,
        sourceCategory: SourceCategory,
        value: Double,
        secondaryValue: Double? = nil,
        timestamp: Date,
        originalRecordId: String? = nil,
        quality: DataQuality = .complete
    ) {
        self.id = id
        self.sourceId = sourceId
        self.sourceName = sourceName
        self.sourceCategory = sourceCategory
        self.value = value
        self.secondaryValue = secondaryValue
        self.timestamp = timestamp
        self.originalRecordId = originalRecordId
        self.quality = quality
    }

    var formattedValue: String {
        if value == floor(value) {
            return String(format: "%.0f", value)
        }
        return String(format: "%.1f", value)
    }
}

// MARK: - Source Health Status
struct SourceHealthStatus: Identifiable {
    let id: String
    let source: HealthSource
    let lastSync: Date?
    let recordCount: Int
    let dataGaps: [TimeWindow]
    let overallQuality: DataQuality

    var syncStatus: SyncStatus {
        guard let lastSync = lastSync else {
            return .neverSynced
        }

        let hoursSinceSync = Date().timeIntervalSince(lastSync) / 3600

        if hoursSinceSync < 1 {
            return .recentlySynced
        } else if hoursSinceSync < 24 {
            return .syncedToday
        } else if hoursSinceSync < 72 {
            return .stale
        } else {
            return .veryStale
        }
    }

    enum SyncStatus {
        case recentlySynced
        case syncedToday
        case stale
        case veryStale
        case neverSynced

        var icon: String {
            switch self {
            case .recentlySynced: return "checkmark.circle.fill"
            case .syncedToday: return "checkmark.circle"
            case .stale: return "exclamationmark.circle"
            case .veryStale: return "exclamationmark.triangle"
            case .neverSynced: return "xmark.circle"
            }
        }

        var description: String {
            switch self {
            case .recentlySynced: return "Kürzlich synchronisiert"
            case .syncedToday: return "Heute synchronisiert"
            case .stale: return "Sync überfällig"
            case .veryStale: return "Lange nicht synchronisiert"
            case .neverSynced: return "Nie synchronisiert"
            }
        }
    }
}

// MARK: - Constants
enum HealthBridgeConstants {
    static let bundleIdentifier = "com.healthbridge.merged"
    static let displayName = "HealthBridge"
    static let defaultSyncInterval: TimeInterval = 15 * 60 // 15 minutes
    static let conflictThreshold: TimeInterval = 60 // 1 minute overlap tolerance
}
