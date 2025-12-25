import Foundation
import Combine

@MainActor
class DashboardViewModel: ObservableObject {
    private let syncCoordinator = SyncCoordinator.shared
    private let dataReader = DataReader.shared
    private let sourceManager = SourceManager.shared

    @Published var dailySummary: DailySummary?
    @Published var isLoading = false
    @Published var selectedDate = Date()
    @Published var errorMessage: String?

    private var cancellables = Set<AnyCancellable>()

    init() {
        setupBindings()
    }

    private func setupBindings() {
        $selectedDate
            .debounce(for: .milliseconds(300), scheduler: DispatchQueue.main)
            .sink { [weak self] date in
                Task {
                    await self?.loadData(for: date)
                }
            }
            .store(in: &cancellables)
    }

    func loadData(for date: Date = Date()) async {
        isLoading = true
        errorMessage = nil

        do {
            dailySummary = try await dataReader.fetchDailySummary(for: date)
        } catch {
            errorMessage = error.localizedDescription
        }

        isLoading = false
    }

    func performSync() async {
        do {
            try await syncCoordinator.performSync(for: selectedDate)
            await loadData(for: selectedDate)
        } catch {
            errorMessage = "Sync fehlgeschlagen: \(error.localizedDescription)"
        }
    }

    func refresh() async {
        await sourceManager.discoverSources()
        await loadData(for: selectedDate)
    }

    var syncStatus: SyncStatus {
        if syncCoordinator.isSyncing {
            return .syncing
        } else if let lastSync = syncCoordinator.lastSyncDate {
            let hoursSinceSync = Date().timeIntervalSince(lastSync) / 3600
            if hoursSinceSync < 1 {
                return .synced
            } else if hoursSinceSync < 24 {
                return .stale
            } else {
                return .veryStale
            }
        } else {
            return .neverSynced
        }
    }

    enum SyncStatus {
        case syncing
        case synced
        case stale
        case veryStale
        case neverSynced

        var description: String {
            switch self {
            case .syncing: return "Synchronisiere..."
            case .synced: return "Synchronisiert"
            case .stale: return "Sync empfohlen"
            case .veryStale: return "Sync überfällig"
            case .neverSynced: return "Nie synchronisiert"
            }
        }

        var icon: String {
            switch self {
            case .syncing: return "arrow.triangle.2.circlepath"
            case .synced: return "checkmark.circle.fill"
            case .stale: return "exclamationmark.circle"
            case .veryStale: return "exclamationmark.triangle"
            case .neverSynced: return "xmark.circle"
            }
        }
    }
}
