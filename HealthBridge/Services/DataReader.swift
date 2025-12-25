import Foundation
import HealthKit
import Combine

// MARK: - Data Reader
@MainActor
class DataReader: ObservableObject {
    static let shared = DataReader()

    private let healthKitManager = HealthKitManager.shared
    private let sourceManager = SourceManager.shared

    @Published var isReading = false
    @Published var lastReadDate: Date?
    @Published var detectedConflicts: [Conflict] = []
    @Published var readingProgress: Double = 0

    private init() {}

    // MARK: - Fetch Data by Type and Date Range

    func fetchData(
        for dataType: HealthDataType,
        from startDate: Date,
        to endDate: Date,
        groupByWindow intervalMinutes: Int = 15
    ) async throws -> [TimeWindowData] {
        isReading = true
        defer { isReading = false }

        let samples = try await healthKitManager.fetchSamples(
            for: dataType,
            from: startDate,
            to: endDate
        )

        // Group samples by source
        let groupedBySource = groupBySource(samples: samples, dataType: dataType)

        // Create time windows
        let windows = generateTimeWindows(from: startDate, to: endDate, intervalMinutes: intervalMinutes)

        // Assign samples to windows
        var windowDataList: [TimeWindowData] = []

        for window in windows {
            let windowData = createWindowData(
                window: window,
                dataType: dataType,
                groupedBySource: groupedBySource
            )
            windowDataList.append(windowData)
        }

        lastReadDate = Date()
        return windowDataList
    }

    private func groupBySource(samples: [HKSample], dataType: HealthDataType) -> [String: [HKSample]] {
        var grouped: [String: [HKSample]] = [:]

        for sample in samples {
            let sourceId = sample.sourceRevision.source.bundleIdentifier
            if grouped[sourceId] == nil {
                grouped[sourceId] = []
            }
            grouped[sourceId]?.append(sample)
        }

        return grouped
    }

    private func generateTimeWindows(from start: Date, to end: Date, intervalMinutes: Int) -> [TimeWindow] {
        var windows: [TimeWindow] = []
        var current = start

        while current < end {
            let windowEnd = min(
                Calendar.current.date(byAdding: .minute, value: intervalMinutes, to: current)!,
                end
            )
            windows.append(TimeWindow(start: current, end: windowEnd))
            current = windowEnd
        }

        return windows
    }

    private func createWindowData(
        window: TimeWindow,
        dataType: HealthDataType,
        groupedBySource: [String: [HKSample]]
    ) -> TimeWindowData {
        var readings: [SourceReading] = []

        for (sourceId, samples) in groupedBySource {
            let windowSamples = samples.filter { sample in
                sample.startDate < window.end && sample.endDate > window.start
            }

            if !windowSamples.isEmpty {
                let reading = createReading(
                    from: windowSamples,
                    sourceId: sourceId,
                    dataType: dataType,
                    window: window
                )
                readings.append(reading)
            }
        }

        let hasConflict = detectConflict(in: readings, dataType: dataType)

        return TimeWindowData(
            timeWindow: window,
            dataType: dataType,
            readings: readings,
            hasConflict: hasConflict
        )
    }

    private func createReading(
        from samples: [HKSample],
        sourceId: String,
        dataType: HealthDataType,
        window: TimeWindow
    ) -> SourceReading {
        let value: Double
        var secondaryValue: Double? = nil

        switch dataType {
        case .steps, .floorsClimbed, .activeEnergy, .distance:
            // Sum up values for cumulative types
            value = samples.compactMap { sample -> Double? in
                guard let quantitySample = sample as? HKQuantitySample else { return nil }
                return quantitySample.quantity.doubleValue(for: dataType.hkUnit)
            }.reduce(0, +)

        case .heartRate, .restingHeartRate, .respiratoryRate, .heartRateVariability, .bloodOxygen:
            // Average for rate-based types
            let values = samples.compactMap { sample -> Double? in
                guard let quantitySample = sample as? HKQuantitySample else { return nil }
                return quantitySample.quantity.doubleValue(for: dataType.hkUnit)
            }
            value = values.isEmpty ? 0 : values.reduce(0, +) / Double(values.count)

        case .bloodPressureSystolic, .bloodPressureDiastolic:
            // For blood pressure, we need to handle correlations
            let values = samples.compactMap { sample -> Double? in
                guard let quantitySample = sample as? HKQuantitySample else { return nil }
                return quantitySample.quantity.doubleValue(for: dataType.hkUnit)
            }
            value = values.isEmpty ? 0 : values.reduce(0, +) / Double(values.count)

        case .sleep:
            // Sum up sleep duration
            value = samples.reduce(0) { acc, sample in
                acc + sample.endDate.timeIntervalSince(sample.startDate) / 3600
            }
        }

        let source = sourceManager.sources.first { $0.bundleIdentifier == sourceId }
        let category = source?.category ?? sourceManager.classifySource(sourceId)

        return SourceReading(
            sourceId: sourceId,
            sourceName: source?.name ?? sourceId,
            sourceCategory: category,
            value: value,
            secondaryValue: secondaryValue,
            timestamp: window.start,
            originalRecordId: samples.first?.uuid.uuidString,
            quality: samples.isEmpty ? .missing : .complete
        )
    }

    // MARK: - Conflict Detection

    private func detectConflict(in readings: [SourceReading], dataType: HealthDataType) -> Bool {
        // No conflict if less than 2 readings
        guard readings.count >= 2 else { return false }

        // Filter out zero values (device wasn't tracking)
        let nonZeroReadings = readings.filter { $0.value > 0 }
        guard nonZeroReadings.count >= 2 else { return false }

        // Check if values differ significantly
        let values = nonZeroReadings.map { $0.value }
        guard let minVal = values.min(), let maxVal = values.max() else { return false }

        // Threshold varies by data type
        let threshold = conflictThreshold(for: dataType)

        if minVal == 0 {
            return maxVal > threshold.absoluteThreshold
        }

        let percentDiff = (maxVal - minVal) / minVal * 100
        return percentDiff > threshold.percentageThreshold
    }

    private func conflictThreshold(for dataType: HealthDataType) -> ConflictThreshold {
        switch dataType {
        case .steps:
            return ConflictThreshold(percentageThreshold: 10, absoluteThreshold: 100)
        case .distance:
            return ConflictThreshold(percentageThreshold: 10, absoluteThreshold: 0.1) // 100m
        case .heartRate:
            return ConflictThreshold(percentageThreshold: 15, absoluteThreshold: 10)
        case .bloodPressureSystolic, .bloodPressureDiastolic:
            return ConflictThreshold(percentageThreshold: 5, absoluteThreshold: 5)
        case .bloodOxygen:
            return ConflictThreshold(percentageThreshold: 2, absoluteThreshold: 2)
        case .floorsClimbed:
            return ConflictThreshold(percentageThreshold: 20, absoluteThreshold: 2)
        case .activeEnergy:
            return ConflictThreshold(percentageThreshold: 15, absoluteThreshold: 50)
        case .sleep:
            return ConflictThreshold(percentageThreshold: 10, absoluteThreshold: 0.5) // 30 min
        case .restingHeartRate:
            return ConflictThreshold(percentageThreshold: 10, absoluteThreshold: 5)
        case .heartRateVariability:
            return ConflictThreshold(percentageThreshold: 20, absoluteThreshold: 10)
        case .respiratoryRate:
            return ConflictThreshold(percentageThreshold: 15, absoluteThreshold: 2)
        }
    }

    // MARK: - Detect All Conflicts

    func detectConflicts(
        for date: Date,
        dataTypes: [HealthDataType] = HealthDataType.allCases
    ) async throws -> [Conflict] {
        let calendar = Calendar.current
        let startOfDay = calendar.startOfDay(for: date)
        let endOfDay = calendar.date(byAdding: .day, value: 1, to: startOfDay)!

        var allConflicts: [Conflict] = []

        for (index, dataType) in dataTypes.enumerated() {
            readingProgress = Double(index) / Double(dataTypes.count)

            do {
                let windowData = try await fetchData(
                    for: dataType,
                    from: startOfDay,
                    to: endOfDay
                )

                let conflicts = windowData
                    .filter { $0.hasConflict }
                    .map { data in
                        Conflict(
                            dataType: dataType,
                            timeWindow: data.timeWindow,
                            readings: data.readings,
                            status: .pending
                        )
                    }

                allConflicts.append(contentsOf: conflicts)
            } catch {
                print("Failed to detect conflicts for \(dataType): \(error)")
            }
        }

        readingProgress = 1.0
        detectedConflicts = allConflicts
        return allConflicts
    }

    // MARK: - Data Gaps Detection

    func detectGaps(
        for dataType: HealthDataType,
        from startDate: Date,
        to endDate: Date,
        expectedIntervalMinutes: Int = 15
    ) async throws -> [DataGap] {
        let samples = try await healthKitManager.fetchSamples(
            for: dataType,
            from: startDate,
            to: endDate
        )

        guard !samples.isEmpty else {
            return [DataGap(
                dataType: dataType,
                timeWindow: TimeWindow(start: startDate, end: endDate),
                expectedRecordCount: 0,
                actualRecordCount: 0
            )]
        }

        let sortedSamples = samples.sorted { $0.startDate < $1.startDate }
        var gaps: [DataGap] = []
        let expectedInterval = TimeInterval(expectedIntervalMinutes * 60)

        // Check gap at start
        if let firstSample = sortedSamples.first,
           firstSample.startDate.timeIntervalSince(startDate) > expectedInterval * 2 {
            gaps.append(DataGap(
                dataType: dataType,
                timeWindow: TimeWindow(start: startDate, end: firstSample.startDate),
                expectedRecordCount: Int(firstSample.startDate.timeIntervalSince(startDate) / expectedInterval),
                actualRecordCount: 0
            ))
        }

        // Check gaps between samples
        for i in 0..<(sortedSamples.count - 1) {
            let current = sortedSamples[i]
            let next = sortedSamples[i + 1]
            let gap = next.startDate.timeIntervalSince(current.endDate)

            if gap > expectedInterval * 2 {
                gaps.append(DataGap(
                    dataType: dataType,
                    timeWindow: TimeWindow(start: current.endDate, end: next.startDate),
                    expectedRecordCount: Int(gap / expectedInterval),
                    actualRecordCount: 0
                ))
            }
        }

        // Check gap at end
        if let lastSample = sortedSamples.last,
           endDate.timeIntervalSince(lastSample.endDate) > expectedInterval * 2 {
            gaps.append(DataGap(
                dataType: dataType,
                timeWindow: TimeWindow(start: lastSample.endDate, end: endDate),
                expectedRecordCount: Int(endDate.timeIntervalSince(lastSample.endDate) / expectedInterval),
                actualRecordCount: 0
            ))
        }

        return gaps
    }

    // MARK: - Aggregated Data

    func fetchDailySummary(for date: Date) async throws -> DailySummary {
        let calendar = Calendar.current
        let startOfDay = calendar.startOfDay(for: date)
        let endOfDay = calendar.date(byAdding: .day, value: 1, to: startOfDay)!

        var summary = DailySummary(date: date)

        for dataType in HealthDataType.allCases {
            do {
                let samples = try await healthKitManager.fetchSamples(
                    for: dataType,
                    from: startOfDay,
                    to: endOfDay
                )

                let value = aggregateValue(samples: samples, dataType: dataType)
                summary.values[dataType] = value

                // Check for conflicts
                let windowData = try await fetchData(
                    for: dataType,
                    from: startOfDay,
                    to: endOfDay
                )
                let conflictCount = windowData.filter { $0.hasConflict }.count
                summary.conflictCounts[dataType] = conflictCount
            } catch {
                print("Failed to fetch \(dataType) for summary: \(error)")
            }
        }

        return summary
    }

    private func aggregateValue(samples: [HKSample], dataType: HealthDataType) -> Double {
        switch dataType {
        case .steps, .floorsClimbed, .activeEnergy, .distance:
            return samples.compactMap { sample -> Double? in
                guard let quantitySample = sample as? HKQuantitySample else { return nil }
                return quantitySample.quantity.doubleValue(for: dataType.hkUnit)
            }.reduce(0, +)

        case .heartRate, .restingHeartRate, .respiratoryRate, .heartRateVariability,
             .bloodOxygen, .bloodPressureSystolic, .bloodPressureDiastolic:
            let values = samples.compactMap { sample -> Double? in
                guard let quantitySample = sample as? HKQuantitySample else { return nil }
                return quantitySample.quantity.doubleValue(for: dataType.hkUnit)
            }
            return values.isEmpty ? 0 : values.reduce(0, +) / Double(values.count)

        case .sleep:
            return samples.reduce(0) { acc, sample in
                acc + sample.endDate.timeIntervalSince(sample.startDate) / 3600
            }
        }
    }
}

// MARK: - Supporting Types

struct TimeWindowData: Identifiable {
    let id = UUID()
    let timeWindow: TimeWindow
    let dataType: HealthDataType
    let readings: [SourceReading]
    let hasConflict: Bool

    var primaryReading: SourceReading? {
        readings.max { $0.sourceCategory.priority < $1.sourceCategory.priority }
    }

    var conflictSeverity: ConflictSeverity? {
        guard hasConflict, readings.count >= 2 else { return nil }

        let values = readings.map { $0.value }.filter { $0 > 0 }
        guard let min = values.min(), let max = values.max(), min > 0 else { return nil }

        let percentDiff = (max - min) / min * 100

        if percentDiff < 5 { return .minor }
        if percentDiff < 20 { return .moderate }
        if percentDiff < 50 { return .significant }
        return .major
    }
}

struct ConflictThreshold {
    let percentageThreshold: Double
    let absoluteThreshold: Double
}

struct DataGap: Identifiable {
    let id = UUID()
    let dataType: HealthDataType
    let timeWindow: TimeWindow
    let expectedRecordCount: Int
    let actualRecordCount: Int

    var severity: GapSeverity {
        let duration = timeWindow.duration
        if duration < 3600 { return .minor } // < 1 hour
        if duration < 4 * 3600 { return .moderate } // < 4 hours
        if duration < 12 * 3600 { return .significant } // < 12 hours
        return .major
    }

    enum GapSeverity {
        case minor, moderate, significant, major
    }
}

struct DailySummary {
    let date: Date
    var values: [HealthDataType: Double] = [:]
    var conflictCounts: [HealthDataType: Int] = [:]
    var lastUpdated = Date()

    var totalConflicts: Int {
        conflictCounts.values.reduce(0, +)
    }

    func formattedValue(for dataType: HealthDataType) -> String {
        guard let value = values[dataType] else { return "–" }

        switch dataType {
        case .steps, .floorsClimbed:
            return String(format: "%.0f", value)
        case .distance:
            return String(format: "%.2f km", value)
        case .heartRate, .restingHeartRate, .respiratoryRate:
            return String(format: "%.0f %@", value, dataType.unit)
        case .bloodPressureSystolic, .bloodPressureDiastolic:
            return String(format: "%.0f mmHg", value)
        case .bloodOxygen:
            return String(format: "%.0f%%", value * 100)
        case .activeEnergy:
            return String(format: "%.0f kcal", value)
        case .sleep:
            let hours = Int(value)
            let minutes = Int((value - Double(hours)) * 60)
            return "\(hours)h \(minutes)min"
        case .heartRateVariability:
            return String(format: "%.0f ms", value)
        }
    }
}
