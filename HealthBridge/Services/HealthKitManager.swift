import Foundation
import HealthKit
import Combine

// MARK: - HealthKit Manager
@MainActor
class HealthKitManager: ObservableObject {
    static let shared = HealthKitManager()

    private let healthStore = HKHealthStore()

    @Published var isAuthorized = false
    @Published var authorizationStatus: [HealthDataType: HKAuthorizationStatus] = [:]
    @Published var discoveredSources: [HealthSource] = []
    @Published var sourceHealthStatus: [String: SourceHealthStatus] = [:]
    @Published var lastError: Error?

    private init() {}

    // MARK: - Authorization

    var allQuantityTypes: Set<HKQuantityType> {
        var types = Set<HKQuantityType>()
        for dataType in HealthDataType.allCases {
            if let quantityType = dataType.hkQuantityType {
                types.insert(quantityType)
            }
        }
        return types
    }

    var allCategoryTypes: Set<HKCategoryType> {
        var types = Set<HKCategoryType>()
        for dataType in HealthDataType.allCases {
            if let categoryType = dataType.hkCategoryType {
                types.insert(categoryType)
            }
        }
        return types
    }

    var allSampleTypes: Set<HKSampleType> {
        var types = Set<HKSampleType>()
        allQuantityTypes.forEach { types.insert($0) }
        allCategoryTypes.forEach { types.insert($0) }
        return types
    }

    func requestAuthorization() async throws {
        guard HKHealthStore.isHealthDataAvailable() else {
            throw HealthKitError.healthDataNotAvailable
        }

        let typesToRead = allSampleTypes
        let typesToWrite = allQuantityTypes

        try await healthStore.requestAuthorization(toShare: typesToWrite, read: typesToRead)
        isAuthorized = true

        await updateAuthorizationStatus()
        await discoverSources()
    }

    func updateAuthorizationStatus() async {
        for dataType in HealthDataType.allCases {
            if let quantityType = dataType.hkQuantityType {
                let status = healthStore.authorizationStatus(for: quantityType)
                authorizationStatus[dataType] = status
            } else if let categoryType = dataType.hkCategoryType {
                let status = healthStore.authorizationStatus(for: categoryType)
                authorizationStatus[dataType] = status
            }
        }
    }

    // MARK: - Source Discovery

    func discoverSources() async {
        var allSources: [String: HealthSource] = [:]

        for dataType in HealthDataType.allCases {
            guard let sampleType = dataType.hkQuantityType ?? dataType.hkCategoryType else {
                continue
            }

            do {
                let sources = try await fetchSources(for: sampleType)
                for source in sources {
                    if var existingSource = allSources[source.bundleIdentifier] {
                        existingSource.supportedDataTypes.insert(dataType)
                        allSources[source.bundleIdentifier] = existingSource
                    } else {
                        var newSource = source
                        newSource.supportedDataTypes.insert(dataType)
                        allSources[source.bundleIdentifier] = newSource
                    }
                }
            } catch {
                print("Failed to fetch sources for \(dataType): \(error)")
            }
        }

        discoveredSources = Array(allSources.values).sorted { $0.category.priority > $1.category.priority }

        // Update source health status
        for source in discoveredSources {
            await updateSourceHealth(source)
        }
    }

    private func fetchSources(for sampleType: HKSampleType) async throws -> [HealthSource] {
        let query = HKSourceQuery(sampleType: sampleType, samplePredicate: nil) { _, sourcesOrNil, errorOrNil in
            // Handled via continuation
        }

        return try await withCheckedThrowingContinuation { continuation in
            let query = HKSourceQuery(sampleType: sampleType, samplePredicate: nil) { _, sourcesOrNil, errorOrNil in
                if let error = errorOrNil {
                    continuation.resume(throwing: error)
                    return
                }

                guard let sources = sourcesOrNil else {
                    continuation.resume(returning: [])
                    return
                }

                let healthSources = sources.map { HealthSource.from(hkSource: $0) }
                continuation.resume(returning: healthSources)
            }

            healthStore.execute(query)
        }
    }

    private func updateSourceHealth(_ source: HealthSource) async {
        var recordCount = 0
        var lastActivity: Date?

        for dataType in source.supportedDataTypes {
            if let quantityType = dataType.hkQuantityType {
                let predicate = HKQuery.predicateForObjects(from: HKSource(bundleIdentifier: source.bundleIdentifier, name: source.name) )
                // Simplified: just get count
                if let count = try? await fetchRecordCount(for: quantityType, source: source) {
                    recordCount += count
                }
                if let date = try? await fetchLastActivityDate(for: quantityType, source: source) {
                    if lastActivity == nil || date > lastActivity! {
                        lastActivity = date
                    }
                }
            }
        }

        let status = SourceHealthStatus(
            id: source.id,
            source: source,
            lastSync: lastActivity,
            recordCount: recordCount,
            dataGaps: [], // TODO: Implement gap detection
            overallQuality: recordCount > 0 ? .complete : .missing
        )

        sourceHealthStatus[source.id] = status
    }

    private func fetchRecordCount(for sampleType: HKSampleType, source: HealthSource) async throws -> Int {
        let calendar = Calendar.current
        let now = Date()
        let startOfDay = calendar.startOfDay(for: now)

        let predicate = HKQuery.predicateForSamples(withStart: startOfDay, end: now, options: .strictStartDate)

        return try await withCheckedThrowingContinuation { continuation in
            let query = HKSampleQuery(
                sampleType: sampleType,
                predicate: predicate,
                limit: HKObjectQueryNoLimit,
                sortDescriptors: nil
            ) { _, samplesOrNil, errorOrNil in
                if let error = errorOrNil {
                    continuation.resume(throwing: error)
                    return
                }

                let samples = samplesOrNil ?? []
                let matchingSamples = samples.filter { $0.sourceRevision.source.bundleIdentifier == source.bundleIdentifier }
                continuation.resume(returning: matchingSamples.count)
            }

            self.healthStore.execute(query)
        }
    }

    private func fetchLastActivityDate(for sampleType: HKSampleType, source: HealthSource) async throws -> Date? {
        let sortDescriptor = NSSortDescriptor(key: HKSampleSortIdentifierEndDate, ascending: false)

        return try await withCheckedThrowingContinuation { continuation in
            let query = HKSampleQuery(
                sampleType: sampleType,
                predicate: nil,
                limit: 1,
                sortDescriptors: [sortDescriptor]
            ) { _, samplesOrNil, errorOrNil in
                if let error = errorOrNil {
                    continuation.resume(throwing: error)
                    return
                }

                let matchingSample = samplesOrNil?.first { $0.sourceRevision.source.bundleIdentifier == source.bundleIdentifier }
                continuation.resume(returning: matchingSample?.endDate)
            }

            self.healthStore.execute(query)
        }
    }

    // MARK: - Source Classification

    func classifySource(_ source: HKSource) -> SourceCategory {
        let bundleId = source.bundleIdentifier.lowercased()

        if bundleId.contains("healthbridge") {
            return .healthBridge
        } else if bundleId.contains("apple.health") && !bundleId.contains("watch") {
            return .iPhone
        } else if bundleId.contains("apple") && bundleId.contains("watch") {
            return .watch
        } else if bundleId.contains("huawei") {
            return .thirdPartyWatch
        } else if bundleId.contains("samsung") || bundleId.contains("galaxy") {
            return .thirdPartyWatch
        } else if bundleId.contains("fitbit") {
            return .thirdPartyWatch
        } else if bundleId.contains("garmin") {
            return .thirdPartyWatch
        } else if bundleId.contains("polar") {
            return .thirdPartyWatch
        } else if bundleId.contains("withings") {
            return .thirdPartyWatch
        } else {
            return .thirdPartyApp
        }
    }

    func getSourceCapabilities(_ source: HealthSource) -> Set<HealthDataType> {
        return source.supportedDataTypes
    }

    // MARK: - Data Fetching (Basic)

    func fetchSamples(
        for dataType: HealthDataType,
        from startDate: Date,
        to endDate: Date
    ) async throws -> [HKSample] {
        guard let sampleType = dataType.hkQuantityType ?? dataType.hkCategoryType else {
            throw HealthKitError.unsupportedDataType
        }

        let predicate = HKQuery.predicateForSamples(
            withStart: startDate,
            end: endDate,
            options: .strictStartDate
        )

        return try await withCheckedThrowingContinuation { continuation in
            let query = HKSampleQuery(
                sampleType: sampleType,
                predicate: predicate,
                limit: HKObjectQueryNoLimit,
                sortDescriptors: [NSSortDescriptor(key: HKSampleSortIdentifierStartDate, ascending: true)]
            ) { _, samplesOrNil, errorOrNil in
                if let error = errorOrNil {
                    continuation.resume(throwing: error)
                    return
                }
                continuation.resume(returning: samplesOrNil ?? [])
            }

            self.healthStore.execute(query)
        }
    }

    // MARK: - Data Writing

    func writeSample(
        dataType: HealthDataType,
        value: Double,
        secondaryValue: Double? = nil,
        date: Date,
        metadata: [String: Any]? = nil
    ) async throws {
        guard let quantityType = dataType.hkQuantityType else {
            throw HealthKitError.unsupportedDataType
        }

        let quantity = HKQuantity(unit: dataType.hkUnit, doubleValue: value)
        let sample = HKQuantitySample(
            type: quantityType,
            quantity: quantity,
            start: date,
            end: date,
            metadata: metadata
        )

        try await healthStore.save(sample)
    }

    func writeBloodPressure(
        systolic: Double,
        diastolic: Double,
        date: Date,
        metadata: [String: Any]? = nil
    ) async throws {
        guard let systolicType = HKQuantityType.quantityType(forIdentifier: .bloodPressureSystolic),
              let diastolicType = HKQuantityType.quantityType(forIdentifier: .bloodPressureDiastolic) else {
            throw HealthKitError.unsupportedDataType
        }

        let systolicQuantity = HKQuantity(unit: .millimeterOfMercury(), doubleValue: systolic)
        let diastolicQuantity = HKQuantity(unit: .millimeterOfMercury(), doubleValue: diastolic)

        let systolicSample = HKQuantitySample(
            type: systolicType,
            quantity: systolicQuantity,
            start: date,
            end: date,
            metadata: metadata
        )

        let diastolicSample = HKQuantitySample(
            type: diastolicType,
            quantity: diastolicQuantity,
            start: date,
            end: date,
            metadata: metadata
        )

        // Create correlation for blood pressure
        guard let correlationType = HKCorrelationType.correlationType(forIdentifier: .bloodPressure) else {
            throw HealthKitError.unsupportedDataType
        }

        let correlation = HKCorrelation(
            type: correlationType,
            start: date,
            end: date,
            objects: [systolicSample, diastolicSample],
            metadata: metadata
        )

        try await healthStore.save(correlation)
    }
}

// MARK: - HealthKit Errors
enum HealthKitError: LocalizedError {
    case healthDataNotAvailable
    case authorizationDenied
    case unsupportedDataType
    case noDataFound
    case writeFailed(Error)
    case queryFailed(Error)

    var errorDescription: String? {
        switch self {
        case .healthDataNotAvailable:
            return "Health-Daten sind auf diesem Gerät nicht verfügbar"
        case .authorizationDenied:
            return "Zugriff auf Health-Daten wurde verweigert"
        case .unsupportedDataType:
            return "Dieser Datentyp wird nicht unterstützt"
        case .noDataFound:
            return "Keine Daten gefunden"
        case .writeFailed(let error):
            return "Schreiben fehlgeschlagen: \(error.localizedDescription)"
        case .queryFailed(let error):
            return "Abfrage fehlgeschlagen: \(error.localizedDescription)"
        }
    }
}

// MARK: - HKSource Extension
extension HKSource {
    convenience init(bundleIdentifier: String, name: String) {
        // Note: This is a workaround since HKSource doesn't have a public initializer
        // In production, sources come from HealthKit queries
        fatalError("HKSource cannot be initialized directly - use source from HKSample")
    }
}
