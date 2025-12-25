import Foundation
import HealthKit

// MARK: - Health Data Type
enum HealthDataType: String, CaseIterable, Codable, Identifiable {
    case steps = "steps"
    case heartRate = "heart_rate"
    case bloodPressureSystolic = "blood_pressure_systolic"
    case bloodPressureDiastolic = "blood_pressure_diastolic"
    case bloodOxygen = "blood_oxygen"
    case sleep = "sleep"
    case distance = "distance"
    case floorsClimbed = "floors_climbed"
    case activeEnergy = "active_energy"
    case restingHeartRate = "resting_heart_rate"
    case heartRateVariability = "hrv"
    case respiratoryRate = "respiratory_rate"

    var id: String { rawValue }

    var displayName: String {
        switch self {
        case .steps: return "Schritte"
        case .heartRate: return "Herzfrequenz"
        case .bloodPressureSystolic: return "Blutdruck (Systolisch)"
        case .bloodPressureDiastolic: return "Blutdruck (Diastolisch)"
        case .bloodOxygen: return "Blutsauerstoff (SpO2)"
        case .sleep: return "Schlaf"
        case .distance: return "Distanz"
        case .floorsClimbed: return "Stockwerke"
        case .activeEnergy: return "Aktive Energie"
        case .restingHeartRate: return "Ruhepuls"
        case .heartRateVariability: return "HRV"
        case .respiratoryRate: return "Atemfrequenz"
        }
    }

    var icon: String {
        switch self {
        case .steps: return "figure.walk"
        case .heartRate, .restingHeartRate: return "heart.fill"
        case .bloodPressureSystolic, .bloodPressureDiastolic: return "drop.fill"
        case .bloodOxygen: return "lungs.fill"
        case .sleep: return "bed.double.fill"
        case .distance: return "map.fill"
        case .floorsClimbed: return "stairs"
        case .activeEnergy: return "flame.fill"
        case .heartRateVariability: return "waveform.path.ecg"
        case .respiratoryRate: return "wind"
        }
    }

    var unit: String {
        switch self {
        case .steps: return "Schritte"
        case .heartRate, .restingHeartRate: return "bpm"
        case .bloodPressureSystolic, .bloodPressureDiastolic: return "mmHg"
        case .bloodOxygen: return "%"
        case .sleep: return "h"
        case .distance: return "km"
        case .floorsClimbed: return "Stockwerke"
        case .activeEnergy: return "kcal"
        case .heartRateVariability: return "ms"
        case .respiratoryRate: return "/min"
        }
    }

    var hkQuantityType: HKQuantityType? {
        switch self {
        case .steps:
            return HKQuantityType.quantityType(forIdentifier: .stepCount)
        case .heartRate:
            return HKQuantityType.quantityType(forIdentifier: .heartRate)
        case .bloodPressureSystolic:
            return HKQuantityType.quantityType(forIdentifier: .bloodPressureSystolic)
        case .bloodPressureDiastolic:
            return HKQuantityType.quantityType(forIdentifier: .bloodPressureDiastolic)
        case .bloodOxygen:
            return HKQuantityType.quantityType(forIdentifier: .oxygenSaturation)
        case .distance:
            return HKQuantityType.quantityType(forIdentifier: .distanceWalkingRunning)
        case .floorsClimbed:
            return HKQuantityType.quantityType(forIdentifier: .flightsClimbed)
        case .activeEnergy:
            return HKQuantityType.quantityType(forIdentifier: .activeEnergyBurned)
        case .restingHeartRate:
            return HKQuantityType.quantityType(forIdentifier: .restingHeartRate)
        case .heartRateVariability:
            return HKQuantityType.quantityType(forIdentifier: .heartRateVariabilitySDNN)
        case .respiratoryRate:
            return HKQuantityType.quantityType(forIdentifier: .respiratoryRate)
        case .sleep:
            return nil // Sleep uses category type
        }
    }

    var hkCategoryType: HKCategoryType? {
        switch self {
        case .sleep:
            return HKCategoryType.categoryType(forIdentifier: .sleepAnalysis)
        default:
            return nil
        }
    }

    var hkUnit: HKUnit {
        switch self {
        case .steps, .floorsClimbed:
            return .count()
        case .heartRate, .restingHeartRate, .respiratoryRate:
            return HKUnit.count().unitDivided(by: .minute())
        case .bloodPressureSystolic, .bloodPressureDiastolic:
            return .millimeterOfMercury()
        case .bloodOxygen:
            return .percent()
        case .sleep:
            return .hour()
        case .distance:
            return .meterUnit(with: .kilo)
        case .activeEnergy:
            return .kilocalorie()
        case .heartRateVariability:
            return .secondUnit(with: .milli)
        }
    }

    /// Default primary source for this data type
    var defaultPrimarySource: SourceCategory {
        switch self {
        case .floorsClimbed:
            return .iPhone
        case .steps, .heartRate, .bloodPressureSystolic, .bloodPressureDiastolic,
             .bloodOxygen, .sleep, .distance, .activeEnergy, .restingHeartRate,
             .heartRateVariability, .respiratoryRate:
            return .watch
        }
    }

    /// Whether this data type typically has only one source
    var isExclusive: Bool {
        switch self {
        case .bloodPressureSystolic, .bloodPressureDiastolic, .bloodOxygen,
             .heartRate, .restingHeartRate, .heartRateVariability, .respiratoryRate, .sleep:
            return true
        default:
            return false
        }
    }
}

// MARK: - Source Category
enum SourceCategory: String, Codable, CaseIterable {
    case iPhone = "iphone"
    case watch = "watch"
    case thirdPartyWatch = "third_party_watch"
    case thirdPartyApp = "third_party_app"
    case healthBridge = "health_bridge"
    case unknown = "unknown"

    var displayName: String {
        switch self {
        case .iPhone: return "iPhone"
        case .watch: return "Apple Watch"
        case .thirdPartyWatch: return "Drittanbieter-Watch"
        case .thirdPartyApp: return "Drittanbieter-App"
        case .healthBridge: return "HealthBridge"
        case .unknown: return "Unbekannt"
        }
    }

    var icon: String {
        switch self {
        case .iPhone: return "iphone"
        case .watch: return "applewatch"
        case .thirdPartyWatch: return "applewatch.side.right"
        case .thirdPartyApp: return "app.badge"
        case .healthBridge: return "arrow.triangle.2.circlepath"
        case .unknown: return "questionmark.circle"
        }
    }

    var priority: Int {
        switch self {
        case .healthBridge: return 100
        case .watch: return 80
        case .thirdPartyWatch: return 70
        case .iPhone: return 50
        case .thirdPartyApp: return 30
        case .unknown: return 0
        }
    }
}

// MARK: - Data Quality
enum DataQuality: String, Codable {
    case complete = "complete"
    case partial = "partial"
    case missing = "missing"
    case invalid = "invalid"

    var icon: String {
        switch self {
        case .complete: return "checkmark.circle.fill"
        case .partial: return "circle.lefthalf.filled"
        case .missing: return "circle.dashed"
        case .invalid: return "xmark.circle.fill"
        }
    }

    var color: String {
        switch self {
        case .complete: return "green"
        case .partial: return "yellow"
        case .missing: return "gray"
        case .invalid: return "red"
        }
    }
}

// MARK: - Time Window
struct TimeWindow: Codable, Hashable, Identifiable {
    let start: Date
    let end: Date

    var id: String { "\(start.timeIntervalSince1970)-\(end.timeIntervalSince1970)" }

    var interval: DateInterval {
        DateInterval(start: start, end: end)
    }

    var duration: TimeInterval {
        end.timeIntervalSince(start)
    }

    var formattedRange: String {
        let formatter = DateFormatter()
        formatter.dateStyle = .none
        formatter.timeStyle = .short
        return "\(formatter.string(from: start)) - \(formatter.string(from: end))"
    }

    var formattedDate: String {
        let formatter = DateFormatter()
        formatter.dateStyle = .medium
        formatter.timeStyle = .none
        return formatter.string(from: start)
    }

    static func windows(for date: Date, intervalMinutes: Int = 15) -> [TimeWindow] {
        let calendar = Calendar.current
        let startOfDay = calendar.startOfDay(for: date)
        let endOfDay = calendar.date(byAdding: .day, value: 1, to: startOfDay)!

        var windows: [TimeWindow] = []
        var current = startOfDay

        while current < endOfDay {
            let windowEnd = calendar.date(byAdding: .minute, value: intervalMinutes, to: current)!
            windows.append(TimeWindow(start: current, end: min(windowEnd, endOfDay)))
            current = windowEnd
        }

        return windows
    }

    static func hourlyWindows(for date: Date) -> [TimeWindow] {
        windows(for: date, intervalMinutes: 60)
    }
}
