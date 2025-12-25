import Foundation
import HealthKit
import Combine

// MARK: - Source Manager
@MainActor
class SourceManager: ObservableObject {
    static let shared = SourceManager()

    @Published var sources: [HealthSource] = []
    @Published var sourceProfiles: [String: SourceProfile] = [:]
    @Published var isDiscovering = false

    private let healthKitManager = HealthKitManager.shared
    private let storage = SourceStorageManager()

    private init() {}

    // MARK: - Source Discovery

    func discoverSources() async {
        isDiscovering = true
        defer { isDiscovering = false }

        await healthKitManager.discoverSources()
        sources = healthKitManager.discoveredSources

        // Load saved source profiles and merge with discovered sources
        let savedProfiles = storage.loadSourceProfiles()
        for source in sources {
            if let savedProfile = savedProfiles[source.bundleIdentifier] {
                sourceProfiles[source.bundleIdentifier] = savedProfile
            } else {
                sourceProfiles[source.bundleIdentifier] = SourceProfile(source: source)
            }
        }
    }

    // MARK: - Source Classification

    func classifySource(_ bundleIdentifier: String) -> SourceCategory {
        let lowercased = bundleIdentifier.lowercased()

        // HealthBridge
        if lowercased.contains("healthbridge") {
            return .healthBridge
        }

        // Apple Devices
        if lowercased.contains("com.apple") {
            if lowercased.contains("watch") || lowercased.contains("nano") {
                return .watch
            }
            if lowercased.contains("health") {
                return .iPhone
            }
        }

        // Known Watch Brands
        let watchBrands = ["huawei", "samsung", "galaxy", "fitbit", "garmin", "polar",
                          "withings", "amazfit", "xiaomi", "honor", "oppo", "suunto",
                          "coros", "whoop", "oura"]
        for brand in watchBrands {
            if lowercased.contains(brand) {
                return .thirdPartyWatch
            }
        }

        // Known Health Apps
        let healthApps = ["strava", "nike", "adidas", "runtastic", "runkeeper",
                         "mapmyrun", "endomondo", "myfitnesspal", "flo", "clue"]
        for app in healthApps {
            if lowercased.contains(app) {
                return .thirdPartyApp
            }
        }

        return .unknown
    }

    // MARK: - Source Capabilities

    func getSourceCapabilities(_ source: HealthSource) -> SourceCapabilities {
        let category = source.category

        switch category {
        case .iPhone:
            return SourceCapabilities(
                canMeasureSteps: true,
                canMeasureDistance: true,
                canMeasureFloors: true,
                canMeasureHeartRate: false,
                canMeasureBloodPressure: false,
                canMeasureBloodOxygen: false,
                canMeasureSleep: false,
                canMeasureWorkouts: true,
                hasGPS: true,
                hasBarometer: true,
                hasAccelerometer: true
            )

        case .watch:
            return SourceCapabilities(
                canMeasureSteps: true,
                canMeasureDistance: true,
                canMeasureFloors: false,
                canMeasureHeartRate: true,
                canMeasureBloodPressure: false,
                canMeasureBloodOxygen: true,
                canMeasureSleep: true,
                canMeasureWorkouts: true,
                hasGPS: true,
                hasBarometer: false,
                hasAccelerometer: true
            )

        case .thirdPartyWatch:
            // Check for specific features based on name
            let name = source.name.lowercased()
            let isHuaweiD2 = name.contains("huawei") && (name.contains("d2") || name.contains("watch d"))

            return SourceCapabilities(
                canMeasureSteps: true,
                canMeasureDistance: true,
                canMeasureFloors: false,
                canMeasureHeartRate: true,
                canMeasureBloodPressure: isHuaweiD2, // Huawei Watch D2 has BP sensor
                canMeasureBloodOxygen: true,
                canMeasureSleep: true,
                canMeasureWorkouts: true,
                hasGPS: true,
                hasBarometer: false,
                hasAccelerometer: true
            )

        case .thirdPartyApp:
            return SourceCapabilities(
                canMeasureSteps: true,
                canMeasureDistance: true,
                canMeasureFloors: false,
                canMeasureHeartRate: false,
                canMeasureBloodPressure: false,
                canMeasureBloodOxygen: false,
                canMeasureSleep: false,
                canMeasureWorkouts: true,
                hasGPS: true,
                hasBarometer: false,
                hasAccelerometer: false
            )

        case .healthBridge, .unknown:
            return SourceCapabilities.empty
        }
    }

    // MARK: - Source Health

    func getSourceHealth(_ source: HealthSource) async -> SourceHealthReport {
        var dataGaps: [HealthDataType: [TimeWindow]] = [:]
        var lastActivityByType: [HealthDataType: Date] = [:]
        var recordCountByType: [HealthDataType: Int] = [:]

        let calendar = Calendar.current
        let now = Date()
        let yesterday = calendar.date(byAdding: .day, value: -1, to: now)!

        for dataType in source.supportedDataTypes {
            do {
                let samples = try await healthKitManager.fetchSamples(
                    for: dataType,
                    from: yesterday,
                    to: now
                )

                let matchingSamples = samples.filter {
                    $0.sourceRevision.source.bundleIdentifier == source.bundleIdentifier
                }

                recordCountByType[dataType] = matchingSamples.count

                if let lastSample = matchingSamples.last {
                    lastActivityByType[dataType] = lastSample.endDate
                }

                // Detect gaps
                let gaps = detectDataGaps(
                    in: matchingSamples,
                    from: yesterday,
                    to: now,
                    expectedInterval: 15 * 60 // 15 minutes
                )
                if !gaps.isEmpty {
                    dataGaps[dataType] = gaps
                }
            } catch {
                print("Error checking health for \(dataType): \(error)")
            }
        }

        let overallQuality: DataQuality
        if recordCountByType.values.reduce(0, +) == 0 {
            overallQuality = .missing
        } else if dataGaps.isEmpty {
            overallQuality = .complete
        } else {
            overallQuality = .partial
        }

        return SourceHealthReport(
            source: source,
            lastActivityByType: lastActivityByType,
            recordCountByType: recordCountByType,
            dataGaps: dataGaps,
            overallQuality: overallQuality,
            checkedAt: Date()
        )
    }

    private func detectDataGaps(
        in samples: [HKSample],
        from start: Date,
        to end: Date,
        expectedInterval: TimeInterval
    ) -> [TimeWindow] {
        guard !samples.isEmpty else {
            return [TimeWindow(start: start, end: end)]
        }

        var gaps: [TimeWindow] = []
        let sortedSamples = samples.sorted { $0.startDate < $1.startDate }

        // Check gap at beginning
        if let firstSample = sortedSamples.first,
           firstSample.startDate.timeIntervalSince(start) > expectedInterval * 2 {
            gaps.append(TimeWindow(start: start, end: firstSample.startDate))
        }

        // Check gaps between samples
        for i in 0..<(sortedSamples.count - 1) {
            let currentEnd = sortedSamples[i].endDate
            let nextStart = sortedSamples[i + 1].startDate
            let gap = nextStart.timeIntervalSince(currentEnd)

            if gap > expectedInterval * 2 {
                gaps.append(TimeWindow(start: currentEnd, end: nextStart))
            }
        }

        // Check gap at end
        if let lastSample = sortedSamples.last,
           end.timeIntervalSince(lastSample.endDate) > expectedInterval * 2 {
            gaps.append(TimeWindow(start: lastSample.endDate, end: end))
        }

        return gaps
    }

    // MARK: - Priority Management

    func setPriority(_ priority: Int, for source: HealthSource, dataType: HealthDataType) {
        guard var profile = sourceProfiles[source.bundleIdentifier] else { return }
        profile.priorities[dataType] = priority
        sourceProfiles[source.bundleIdentifier] = profile
        storage.saveSourceProfiles(sourceProfiles)
    }

    func getPriority(for source: HealthSource, dataType: HealthDataType) -> Int {
        if let profile = sourceProfiles[source.bundleIdentifier],
           let priority = profile.priorities[dataType] {
            return priority
        }
        return source.category.priority
    }

    func getSourcesByPriority(for dataType: HealthDataType) -> [HealthSource] {
        return sources
            .filter { $0.supportedDataTypes.contains(dataType) }
            .sorted { getPriority(for: $0, dataType: dataType) > getPriority(for: $1, dataType: dataType) }
    }

    // MARK: - Source Enable/Disable

    func setEnabled(_ enabled: Bool, for source: HealthSource) {
        guard var profile = sourceProfiles[source.bundleIdentifier] else { return }
        profile.isEnabled = enabled
        sourceProfiles[source.bundleIdentifier] = profile
        storage.saveSourceProfiles(sourceProfiles)
    }

    func isEnabled(_ source: HealthSource) -> Bool {
        return sourceProfiles[source.bundleIdentifier]?.isEnabled ?? true
    }
}

// MARK: - Source Profile
struct SourceProfile: Codable {
    let bundleIdentifier: String
    var priorities: [HealthDataType: Int]
    var isEnabled: Bool
    var customName: String?
    var notes: String?
    let addedAt: Date

    init(source: HealthSource) {
        self.bundleIdentifier = source.bundleIdentifier
        self.priorities = [:]
        self.isEnabled = true
        self.customName = nil
        self.notes = nil
        self.addedAt = Date()
    }
}

// MARK: - Source Capabilities
struct SourceCapabilities {
    let canMeasureSteps: Bool
    let canMeasureDistance: Bool
    let canMeasureFloors: Bool
    let canMeasureHeartRate: Bool
    let canMeasureBloodPressure: Bool
    let canMeasureBloodOxygen: Bool
    let canMeasureSleep: Bool
    let canMeasureWorkouts: Bool
    let hasGPS: Bool
    let hasBarometer: Bool
    let hasAccelerometer: Bool

    static let empty = SourceCapabilities(
        canMeasureSteps: false,
        canMeasureDistance: false,
        canMeasureFloors: false,
        canMeasureHeartRate: false,
        canMeasureBloodPressure: false,
        canMeasureBloodOxygen: false,
        canMeasureSleep: false,
        canMeasureWorkouts: false,
        hasGPS: false,
        hasBarometer: false,
        hasAccelerometer: false
    )

    var supportedDataTypes: Set<HealthDataType> {
        var types = Set<HealthDataType>()
        if canMeasureSteps { types.insert(.steps) }
        if canMeasureDistance { types.insert(.distance) }
        if canMeasureFloors { types.insert(.floorsClimbed) }
        if canMeasureHeartRate {
            types.insert(.heartRate)
            types.insert(.restingHeartRate)
        }
        if canMeasureBloodPressure {
            types.insert(.bloodPressureSystolic)
            types.insert(.bloodPressureDiastolic)
        }
        if canMeasureBloodOxygen { types.insert(.bloodOxygen) }
        if canMeasureSleep { types.insert(.sleep) }
        return types
    }
}

// MARK: - Source Health Report
struct SourceHealthReport {
    let source: HealthSource
    let lastActivityByType: [HealthDataType: Date]
    let recordCountByType: [HealthDataType: Int]
    let dataGaps: [HealthDataType: [TimeWindow]]
    let overallQuality: DataQuality
    let checkedAt: Date

    var lastOverallActivity: Date? {
        lastActivityByType.values.max()
    }

    var totalRecordCount: Int {
        recordCountByType.values.reduce(0, +)
    }

    var hasSignificantGaps: Bool {
        dataGaps.values.flatMap { $0 }.contains { $0.duration > 3600 } // > 1 hour gap
    }
}

// MARK: - Source Storage Manager
class SourceStorageManager {
    private let userDefaults = UserDefaults.standard
    private let profilesKey = "healthbridge.source.profiles"

    func saveSourceProfiles(_ profiles: [String: SourceProfile]) {
        do {
            let data = try JSONEncoder().encode(profiles)
            userDefaults.set(data, forKey: profilesKey)
        } catch {
            print("Failed to save source profiles: \(error)")
        }
    }

    func loadSourceProfiles() -> [String: SourceProfile] {
        guard let data = userDefaults.data(forKey: profilesKey) else {
            return [:]
        }

        do {
            return try JSONDecoder().decode([String: SourceProfile].self, from: data)
        } catch {
            print("Failed to load source profiles: \(error)")
            return [:]
        }
    }
}
