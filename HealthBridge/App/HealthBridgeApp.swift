import SwiftUI
import HealthKit
import BackgroundTasks

@main
struct HealthBridgeApp: App {
    @StateObject private var appState = AppState()
    @StateObject private var healthKitManager = HealthKitManager.shared
    @StateObject private var syncCoordinator = SyncCoordinator.shared

    init() {
        registerBackgroundTasks()
    }

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(appState)
                .environmentObject(healthKitManager)
                .environmentObject(syncCoordinator)
                .onAppear {
                    Task {
                        await requestHealthKitAuthorization()
                    }
                }
        }
    }

    private func registerBackgroundTasks() {
        BGTaskScheduler.shared.register(
            forTaskWithIdentifier: "com.healthbridge.sync",
            using: nil
        ) { task in
            guard let bgTask = task as? BGAppRefreshTask else { return }
            handleBackgroundSync(task: bgTask)
        }
    }

    private func handleBackgroundSync(task: BGAppRefreshTask) {
        scheduleNextBackgroundSync()

        let syncTask = Task {
            do {
                try await syncCoordinator.performSync()
                task.setTaskCompleted(success: true)
            } catch {
                task.setTaskCompleted(success: false)
            }
        }

        task.expirationHandler = {
            syncTask.cancel()
        }
    }

    private func scheduleNextBackgroundSync() {
        let request = BGAppRefreshTaskRequest(identifier: "com.healthbridge.sync")
        request.earliestBeginDate = Date(timeIntervalSinceNow: 15 * 60) // 15 min

        do {
            try BGTaskScheduler.shared.submit(request)
        } catch {
            print("Failed to schedule background sync: \(error)")
        }
    }

    private func requestHealthKitAuthorization() async {
        do {
            try await healthKitManager.requestAuthorization()
        } catch {
            print("HealthKit authorization failed: \(error)")
        }
    }
}

// MARK: - App State
@MainActor
class AppState: ObservableObject {
    @Published var selectedTab: Tab = .dashboard
    @Published var showingConflictDetail: Conflict?
    @Published var isLoading = false
    @Published var lastSyncDate: Date?
    @Published var pendingConflicts: [Conflict] = []

    enum Tab {
        case dashboard
        case conflicts
        case rules
        case sources
    }
}
