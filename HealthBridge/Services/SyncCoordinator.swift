import Foundation
import Combine
import UserNotifications

// MARK: - Sync Coordinator
@MainActor
class SyncCoordinator: ObservableObject {
    static let shared = SyncCoordinator()

    private let healthKitManager = HealthKitManager.shared
    private let sourceManager = SourceManager.shared
    private let dataReader = DataReader.shared
    private let ruleEngine = RuleEngine.shared
    private let mergeEngine = MergeEngine.shared
    private let dataWriter = DataWriter.shared

    @Published var isSyncing = false
    @Published var syncProgress: Double = 0
    @Published var lastSyncDate: Date?
    @Published var lastSyncResult: SyncResult?
    @Published var pendingConflicts: [Conflict] = []
    @Published var syncHistory: [SyncResult] = []

    private let syncHistoryKey = "healthbridge.sync.history"
    private let maxHistoryItems = 50

    private init() {
        loadSyncHistory()
    }

    // MARK: - Main Sync

    func performSync(
        for date: Date = Date(),
        dataTypes: [HealthDataType] = HealthDataType.allCases
    ) async throws {
        guard !isSyncing else { return }

        isSyncing = true
        syncProgress = 0
        defer { isSyncing = false }

        let startTime = Date()
        var result = SyncResult(startedAt: startTime)

        do {
            // Step 1: Refresh sources (10%)
            syncProgress = 0.05
            await sourceManager.discoverSources()
            syncProgress = 0.1

            // Step 2: Detect conflicts (40%)
            let conflicts = try await dataReader.detectConflicts(for: date, dataTypes: dataTypes)
            result.totalConflicts = conflicts.count
            syncProgress = 0.4

            // Step 3: Process conflicts with merge engine (70%)
            let operations = await mergeEngine.processConflicts(conflicts)
            syncProgress = 0.7

            let autoResolved = operations.filter { $0.status == .resolved }
            let pendingManual = operations.filter { $0.status == .pendingManualReview }

            result.autoResolved = autoResolved.count
            result.pendingManualReview = pendingManual.count

            // Update pending conflicts
            pendingConflicts = pendingManual.map { $0.conflict }

            // Step 4: Write resolved records (90%)
            let recordsToWrite = autoResolved.compactMap { $0.mergedRecord }
            if !recordsToWrite.isEmpty {
                let writeResult = await dataWriter.writeBatch(recordsToWrite)
                result.writtenRecords = writeResult.successCount
                result.writeErrors = writeResult.failureCount
            }
            syncProgress = 0.9

            // Step 5: Finalize (100%)
            result.completedAt = Date()
            result.status = .success
            syncProgress = 1.0

        } catch {
            result.status = .failed
            result.error = error.localizedDescription
            result.completedAt = Date()
            throw error
        }

        lastSyncDate = Date()
        lastSyncResult = result
        addToHistory(result)

        // Send notification if there are pending conflicts
        if result.pendingManualReview > 0 {
            await sendConflictNotification(count: result.pendingManualReview)
        }
    }

    // MARK: - Quick Sync

    func performQuickSync() async throws {
        try await performSync(
            for: Date(),
            dataTypes: [.steps, .heartRate, .activeEnergy]
        )
    }

    // MARK: - Sync Specific Data Type

    func syncDataType(_ dataType: HealthDataType, for date: Date = Date()) async throws {
        try await performSync(for: date, dataTypes: [dataType])
    }

    // MARK: - Manual Conflict Resolution

    func resolveConflict(_ conflict: Conflict, selectedReadingId: UUID) async throws {
        guard let resolution = mergeEngine.resolveConflictManually(conflict, selectedReadingId: selectedReadingId) else {
            throw SyncError.resolutionFailed
        }

        var resolvedConflict = conflict
        resolvedConflict.status = .resolved
        resolvedConflict.resolution = resolution
        resolvedConflict.resolvedAt = Date()

        let mergedRecord = mergeEngine.createMergedRecord(from: resolvedConflict, resolution: resolution)

        // Write the record
        _ = try await dataWriter.writeRecord(mergedRecord)

        // Remove from pending
        pendingConflicts.removeAll { $0.id == conflict.id }
    }

    func ignoreConflict(_ conflict: Conflict) {
        pendingConflicts.removeAll { $0.id == conflict.id }
    }

    // MARK: - Sync History

    private func loadSyncHistory() {
        guard let data = UserDefaults.standard.data(forKey: syncHistoryKey),
              let history = try? JSONDecoder().decode([SyncResult].self, from: data) else {
            return
        }
        syncHistory = history
    }

    private func addToHistory(_ result: SyncResult) {
        syncHistory.insert(result, at: 0)
        if syncHistory.count > maxHistoryItems {
            syncHistory = Array(syncHistory.prefix(maxHistoryItems))
        }
        saveSyncHistory()
    }

    private func saveSyncHistory() {
        guard let data = try? JSONEncoder().encode(syncHistory) else { return }
        UserDefaults.standard.set(data, forKey: syncHistoryKey)
    }

    func clearHistory() {
        syncHistory.removeAll()
        UserDefaults.standard.removeObject(forKey: syncHistoryKey)
    }

    // MARK: - Notifications

    private func sendConflictNotification(count: Int) async {
        let center = UNUserNotificationCenter.current()

        let settings = await center.notificationSettings()
        guard settings.authorizationStatus == .authorized else { return }

        let content = UNMutableNotificationContent()
        content.title = "HealthBridge"
        content.body = count == 1
            ? "1 Konflikt erfordert Ihre Aufmerksamkeit"
            : "\(count) Konflikte erfordern Ihre Aufmerksamkeit"
        content.sound = .default
        content.badge = NSNumber(value: count)

        let request = UNNotificationRequest(
            identifier: "healthbridge.conflicts",
            content: content,
            trigger: nil
        )

        try? await center.add(request)
    }

    func requestNotificationPermission() async -> Bool {
        let center = UNUserNotificationCenter.current()

        do {
            return try await center.requestAuthorization(options: [.alert, .sound, .badge])
        } catch {
            return false
        }
    }

    // MARK: - Statistics

    var todayStats: TodayStats {
        let today = Calendar.current.startOfDay(for: Date())
        let todaySyncs = syncHistory.filter {
            Calendar.current.isDate($0.startedAt, inSameDayAs: today)
        }

        return TodayStats(
            syncCount: todaySyncs.count,
            totalConflicts: todaySyncs.reduce(0) { $0 + $1.totalConflicts },
            autoResolved: todaySyncs.reduce(0) { $0 + $1.autoResolved },
            pendingManual: pendingConflicts.count,
            lastSync: lastSyncDate
        )
    }
}

// MARK: - Supporting Types

struct SyncResult: Identifiable, Codable {
    let id = UUID()
    let startedAt: Date
    var completedAt: Date?
    var status: SyncStatus = .inProgress
    var totalConflicts = 0
    var autoResolved = 0
    var pendingManualReview = 0
    var writtenRecords = 0
    var writeErrors = 0
    var error: String?

    enum SyncStatus: String, Codable {
        case inProgress = "in_progress"
        case success = "success"
        case partialSuccess = "partial_success"
        case failed = "failed"
    }

    var duration: TimeInterval? {
        guard let completed = completedAt else { return nil }
        return completed.timeIntervalSince(startedAt)
    }

    var formattedDuration: String {
        guard let duration = duration else { return "–" }
        if duration < 1 {
            return "< 1s"
        }
        return String(format: "%.1fs", duration)
    }

    var successRate: Double {
        guard totalConflicts > 0 else { return 1.0 }
        return Double(autoResolved) / Double(totalConflicts)
    }
}

struct TodayStats {
    let syncCount: Int
    let totalConflicts: Int
    let autoResolved: Int
    let pendingManual: Int
    let lastSync: Date?

    var resolutionRate: Double {
        guard totalConflicts > 0 else { return 1.0 }
        return Double(autoResolved) / Double(totalConflicts)
    }

    var formattedLastSync: String {
        guard let date = lastSync else { return "Nie" }

        let formatter = RelativeDateTimeFormatter()
        formatter.unitsStyle = .abbreviated
        return formatter.localizedString(for: date, relativeTo: Date())
    }
}

enum SyncError: LocalizedError {
    case notAuthorized
    case syncInProgress
    case resolutionFailed
    case writeFailed

    var errorDescription: String? {
        switch self {
        case .notAuthorized:
            return "Keine Berechtigung für HealthKit"
        case .syncInProgress:
            return "Synchronisierung läuft bereits"
        case .resolutionFailed:
            return "Konfliktauflösung fehlgeschlagen"
        case .writeFailed:
            return "Schreiben der Daten fehlgeschlagen"
        }
    }
}
