import Foundation
import HealthKit
import Combine

// MARK: - Data Writer
@MainActor
class DataWriter: ObservableObject {
    static let shared = DataWriter()

    private let healthKitManager = HealthKitManager.shared
    private let healthStore = HKHealthStore()

    @Published var isWriting = false
    @Published var writeProgress: Double = 0
    @Published var lastWriteDate: Date?
    @Published var writtenRecords: [WrittenRecord] = []
    @Published var failedWrites: [FailedWrite] = []

    private let processedRecordsKey = "healthbridge.processed.records"

    private init() {
        loadProcessedRecords()
    }

    // MARK: - Write Single Record

    func writeRecord(_ mergedRecord: MergedRecord) async throws -> WrittenRecord {
        isWriting = true
        defer { isWriting = false }

        // Check if already written
        if isAlreadyWritten(mergedRecord) {
            throw DataWriterError.duplicateRecord
        }

        let metadata = createMetadata(from: mergedRecord)

        switch mergedRecord.dataType {
        case .bloodPressureSystolic, .bloodPressureDiastolic:
            // Blood pressure needs special handling
            guard let diastolic = mergedRecord.secondaryValue else {
                throw DataWriterError.missingSecondaryValue
            }
            try await writeBloodPressure(
                systolic: mergedRecord.value,
                diastolic: diastolic,
                date: mergedRecord.timeWindow.start,
                metadata: metadata
            )

        default:
            try await writeSample(
                dataType: mergedRecord.dataType,
                value: mergedRecord.value,
                date: mergedRecord.timeWindow.start,
                metadata: metadata
            )
        }

        let writtenRecord = WrittenRecord(
            id: UUID(),
            mergedRecordId: mergedRecord.id,
            dataType: mergedRecord.dataType,
            value: mergedRecord.value,
            secondaryValue: mergedRecord.secondaryValue,
            writtenAt: Date(),
            timeWindow: mergedRecord.timeWindow
        )

        writtenRecords.append(writtenRecord)
        markAsProcessed(mergedRecord)
        lastWriteDate = Date()

        return writtenRecord
    }

    // MARK: - Write Batch

    func writeBatch(_ mergedRecords: [MergedRecord]) async -> BatchWriteResult {
        isWriting = true
        defer { isWriting = false }

        var successful: [WrittenRecord] = []
        var failed: [FailedWrite] = []

        for (index, record) in mergedRecords.enumerated() {
            writeProgress = Double(index) / Double(mergedRecords.count)

            do {
                let writtenRecord = try await writeRecord(record)
                successful.append(writtenRecord)
            } catch {
                let failedWrite = FailedWrite(
                    mergedRecord: record,
                    error: error,
                    attemptedAt: Date()
                )
                failed.append(failedWrite)
                failedWrites.append(failedWrite)
            }
        }

        writeProgress = 1.0

        return BatchWriteResult(
            successful: successful,
            failed: failed,
            completedAt: Date()
        )
    }

    // MARK: - Private Write Methods

    private func writeSample(
        dataType: HealthDataType,
        value: Double,
        date: Date,
        metadata: [String: Any]
    ) async throws {
        guard let quantityType = dataType.hkQuantityType else {
            throw DataWriterError.unsupportedDataType
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

    private func writeBloodPressure(
        systolic: Double,
        diastolic: Double,
        date: Date,
        metadata: [String: Any]
    ) async throws {
        // Validate blood pressure values
        let validation = BloodPressureHandler.shared.validate(
            systolic: systolic,
            diastolic: diastolic
        )

        if !validation.isValid {
            throw DataWriterError.invalidValue(validation.issues.joined(separator: ", "))
        }

        guard let systolicType = HKQuantityType.quantityType(forIdentifier: .bloodPressureSystolic),
              let diastolicType = HKQuantityType.quantityType(forIdentifier: .bloodPressureDiastolic),
              let correlationType = HKCorrelationType.correlationType(forIdentifier: .bloodPressure) else {
            throw DataWriterError.unsupportedDataType
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

        let correlation = HKCorrelation(
            type: correlationType,
            start: date,
            end: date,
            objects: [systolicSample, diastolicSample],
            metadata: metadata
        )

        try await healthStore.save(correlation)
    }

    // MARK: - Metadata

    private func createMetadata(from record: MergedRecord) -> [String: Any] {
        var metadata: [String: Any] = [
            HKMetadataKeyWasUserEntered: false,
            "HealthBridgeSource": HealthBridgeConstants.bundleIdentifier,
            "OriginalSourceId": record.originalSourceId,
            "MergeStrategy": record.strategy.rawValue,
            "MergedRecordId": record.id.uuidString,
            "MergedAt": ISO8601DateFormatter().string(from: record.createdAt)
        ]

        for (key, value) in record.metadata {
            metadata["HB_\(key)"] = value
        }

        return metadata
    }

    // MARK: - Duplicate Prevention

    private var processedRecordIds: Set<String> = []

    private func loadProcessedRecords() {
        if let data = UserDefaults.standard.data(forKey: processedRecordsKey),
           let ids = try? JSONDecoder().decode(Set<String>.self, from: data) {
            processedRecordIds = ids
        }
    }

    private func saveProcessedRecords() {
        if let data = try? JSONEncoder().encode(processedRecordIds) {
            UserDefaults.standard.set(data, forKey: processedRecordsKey)
        }
    }

    private func isAlreadyWritten(_ record: MergedRecord) -> Bool {
        let identifier = createRecordIdentifier(record)
        return processedRecordIds.contains(identifier)
    }

    private func markAsProcessed(_ record: MergedRecord) {
        let identifier = createRecordIdentifier(record)
        processedRecordIds.insert(identifier)
        saveProcessedRecords()

        // Cleanup old records (keep last 7 days)
        cleanupOldRecords()
    }

    private func createRecordIdentifier(_ record: MergedRecord) -> String {
        let components = [
            record.dataType.rawValue,
            String(record.timeWindow.start.timeIntervalSince1970),
            String(record.value)
        ]
        return components.joined(separator: "-")
    }

    private func cleanupOldRecords() {
        // Keep only identifiers that contain recent timestamps
        let sevenDaysAgo = Date().addingTimeInterval(-7 * 24 * 60 * 60)
        let cutoffTimestamp = sevenDaysAgo.timeIntervalSince1970

        processedRecordIds = processedRecordIds.filter { identifier in
            guard let parts = identifier.split(separator: "-").dropFirst().first,
                  let timestamp = Double(parts) else {
                return false
            }
            return timestamp > cutoffTimestamp
        }

        saveProcessedRecords()
    }

    // MARK: - Delete Records

    func deleteHealthBridgeRecords(
        for dataType: HealthDataType,
        from startDate: Date,
        to endDate: Date
    ) async throws -> Int {
        guard let sampleType = dataType.hkQuantityType else {
            throw DataWriterError.unsupportedDataType
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
                sortDescriptors: nil
            ) { [weak self] _, samplesOrNil, errorOrNil in
                guard let self = self else {
                    continuation.resume(throwing: DataWriterError.unknownError)
                    return
                }

                if let error = errorOrNil {
                    continuation.resume(throwing: error)
                    return
                }

                guard let samples = samplesOrNil else {
                    continuation.resume(returning: 0)
                    return
                }

                // Filter to only HealthBridge records
                let healthBridgeSamples = samples.filter { sample in
                    if let metadata = sample.metadata,
                       let source = metadata["HealthBridgeSource"] as? String {
                        return source == HealthBridgeConstants.bundleIdentifier
                    }
                    return false
                }

                guard !healthBridgeSamples.isEmpty else {
                    continuation.resume(returning: 0)
                    return
                }

                Task {
                    do {
                        try await self.healthStore.delete(healthBridgeSamples)
                        continuation.resume(returning: healthBridgeSamples.count)
                    } catch {
                        continuation.resume(throwing: error)
                    }
                }
            }

            healthStore.execute(query)
        }
    }
}

// MARK: - Supporting Types

struct WrittenRecord: Identifiable, Codable {
    let id: UUID
    let mergedRecordId: UUID
    let dataType: HealthDataType
    let value: Double
    let secondaryValue: Double?
    let writtenAt: Date
    let timeWindow: TimeWindow
}

struct FailedWrite: Identifiable {
    let id = UUID()
    let mergedRecord: MergedRecord
    let error: Error
    let attemptedAt: Date

    var errorMessage: String {
        error.localizedDescription
    }
}

struct BatchWriteResult {
    let successful: [WrittenRecord]
    let failed: [FailedWrite]
    let completedAt: Date

    var successCount: Int { successful.count }
    var failureCount: Int { failed.count }
    var totalCount: Int { successCount + failureCount }

    var successRate: Double {
        guard totalCount > 0 else { return 1.0 }
        return Double(successCount) / Double(totalCount)
    }
}

// MARK: - Errors

enum DataWriterError: LocalizedError {
    case unsupportedDataType
    case duplicateRecord
    case missingSecondaryValue
    case invalidValue(String)
    case writeFailed(String)
    case unknownError

    var errorDescription: String? {
        switch self {
        case .unsupportedDataType:
            return "Dieser Datentyp wird nicht unterstützt"
        case .duplicateRecord:
            return "Dieser Datensatz wurde bereits geschrieben"
        case .missingSecondaryValue:
            return "Fehlender sekundärer Wert (z.B. diastolischer Blutdruck)"
        case .invalidValue(let message):
            return "Ungültiger Wert: \(message)"
        case .writeFailed(let message):
            return "Schreiben fehlgeschlagen: \(message)"
        case .unknownError:
            return "Unbekannter Fehler"
        }
    }
}
