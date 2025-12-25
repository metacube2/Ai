import SwiftUI

struct DashboardView: View {
    @EnvironmentObject var appState: AppState
    @EnvironmentObject var syncCoordinator: SyncCoordinator
    @StateObject private var dataReader = DataReader.shared

    @State private var dailySummary: DailySummary?
    @State private var isLoading = false
    @State private var selectedDate = Date()

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 20) {
                    // Sync Status Card
                    syncStatusCard

                    // Date Picker
                    datePicker

                    // Health Metrics
                    if let summary = dailySummary {
                        healthMetricsGrid(summary: summary)
                    } else if isLoading {
                        loadingView
                    } else {
                        emptyStateView
                    }

                    // Pending Conflicts Alert
                    if !syncCoordinator.pendingConflicts.isEmpty {
                        pendingConflictsCard
                    }

                    // Recent Sync History
                    recentSyncHistory
                }
                .padding()
            }
            .navigationTitle("HealthBridge")
            .toolbar {
                ToolbarItem(placement: .topBarTrailing) {
                    Button {
                        Task { await performSync() }
                    } label: {
                        if syncCoordinator.isSyncing {
                            ProgressView()
                                .scaleEffect(0.8)
                        } else {
                            Image(systemName: "arrow.clockwise")
                        }
                    }
                    .disabled(syncCoordinator.isSyncing)
                }
            }
            .refreshable {
                await loadData()
            }
            .task {
                await loadData()
            }
            .onChange(of: selectedDate) {
                Task { await loadData() }
            }
        }
    }

    // MARK: - Sync Status Card

    private var syncStatusCard: some View {
        VStack(spacing: 12) {
            HStack {
                VStack(alignment: .leading, spacing: 4) {
                    Text("Heute")
                        .font(.headline)
                    Text(syncCoordinator.todayStats.formattedLastSync)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                Spacer()

                if syncCoordinator.isSyncing {
                    VStack(alignment: .trailing) {
                        ProgressView(value: syncCoordinator.syncProgress)
                            .frame(width: 60)
                        Text("Synchronisiere...")
                            .font(.caption2)
                            .foregroundStyle(.secondary)
                    }
                } else {
                    Image(systemName: "checkmark.circle.fill")
                        .font(.title2)
                        .foregroundStyle(.green)
                }
            }

            Divider()

            HStack(spacing: 20) {
                StatItem(
                    value: "\(syncCoordinator.todayStats.syncCount)",
                    label: "Syncs",
                    icon: "arrow.triangle.2.circlepath"
                )

                StatItem(
                    value: "\(syncCoordinator.todayStats.totalConflicts)",
                    label: "Konflikte",
                    icon: "exclamationmark.triangle"
                )

                StatItem(
                    value: "\(Int(syncCoordinator.todayStats.resolutionRate * 100))%",
                    label: "Gelöst",
                    icon: "checkmark.circle"
                )
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 16))
        .shadow(color: .black.opacity(0.05), radius: 8, y: 4)
    }

    // MARK: - Date Picker

    private var datePicker: some View {
        DatePicker(
            "Datum",
            selection: $selectedDate,
            in: ...Date(),
            displayedComponents: .date
        )
        .datePickerStyle(.compact)
        .padding(.horizontal)
    }

    // MARK: - Health Metrics Grid

    private func healthMetricsGrid(summary: DailySummary) -> some View {
        LazyVGrid(columns: [
            GridItem(.flexible()),
            GridItem(.flexible())
        ], spacing: 16) {
            ForEach(HealthDataType.allCases) { dataType in
                if let value = summary.values[dataType], value > 0 {
                    HealthMetricCard(
                        dataType: dataType,
                        value: summary.formattedValue(for: dataType),
                        conflictCount: summary.conflictCounts[dataType] ?? 0
                    )
                }
            }
        }
    }

    // MARK: - Pending Conflicts Card

    private var pendingConflictsCard: some View {
        Button {
            appState.selectedTab = .conflicts
        } label: {
            HStack {
                Image(systemName: "exclamationmark.triangle.fill")
                    .foregroundStyle(.orange)

                VStack(alignment: .leading) {
                    Text("\(syncCoordinator.pendingConflicts.count) Konflikte zu prüfen")
                        .font(.headline)
                    Text("Tippen zum Anzeigen")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                Spacer()

                Image(systemName: "chevron.right")
                    .foregroundStyle(.secondary)
            }
            .padding()
            .background(Color.orange.opacity(0.1))
            .clipShape(RoundedRectangle(cornerRadius: 12))
        }
        .buttonStyle(.plain)
    }

    // MARK: - Recent Sync History

    private var recentSyncHistory: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Letzte Synchronisierungen")
                .font(.headline)

            ForEach(syncCoordinator.syncHistory.prefix(5)) { result in
                SyncHistoryRow(result: result)
            }

            if syncCoordinator.syncHistory.isEmpty {
                Text("Keine Synchronisierungen")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                    .frame(maxWidth: .infinity)
                    .padding()
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 16))
        .shadow(color: .black.opacity(0.05), radius: 8, y: 4)
    }

    // MARK: - Loading & Empty States

    private var loadingView: some View {
        VStack(spacing: 12) {
            ProgressView()
            Text("Lade Daten...")
                .font(.subheadline)
                .foregroundStyle(.secondary)
        }
        .frame(maxWidth: .infinity)
        .padding(.vertical, 40)
    }

    private var emptyStateView: some View {
        VStack(spacing: 12) {
            Image(systemName: "heart.text.square")
                .font(.system(size: 48))
                .foregroundStyle(.secondary)
            Text("Keine Daten verfügbar")
                .font(.headline)
            Text("Synchronisieren Sie, um Daten zu laden")
                .font(.subheadline)
                .foregroundStyle(.secondary)
        }
        .frame(maxWidth: .infinity)
        .padding(.vertical, 40)
    }

    // MARK: - Actions

    private func loadData() async {
        isLoading = true
        defer { isLoading = false }

        do {
            dailySummary = try await dataReader.fetchDailySummary(for: selectedDate)
        } catch {
            print("Failed to load data: \(error)")
        }
    }

    private func performSync() async {
        do {
            try await syncCoordinator.performSync(for: selectedDate)
            await loadData()
        } catch {
            print("Sync failed: \(error)")
        }
    }
}

// MARK: - Supporting Views

struct StatItem: View {
    let value: String
    let label: String
    let icon: String

    var body: some View {
        VStack(spacing: 4) {
            Image(systemName: icon)
                .font(.caption)
                .foregroundStyle(.secondary)
            Text(value)
                .font(.title3)
                .fontWeight(.semibold)
            Text(label)
                .font(.caption2)
                .foregroundStyle(.secondary)
        }
        .frame(maxWidth: .infinity)
    }
}

struct HealthMetricCard: View {
    let dataType: HealthDataType
    let value: String
    let conflictCount: Int

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack {
                Image(systemName: dataType.icon)
                    .foregroundStyle(.blue)
                Spacer()
                if conflictCount > 0 {
                    Text("\(conflictCount)")
                        .font(.caption2)
                        .padding(.horizontal, 6)
                        .padding(.vertical, 2)
                        .background(Color.orange.opacity(0.2))
                        .clipShape(Capsule())
                }
            }

            Text(value)
                .font(.title2)
                .fontWeight(.semibold)

            Text(dataType.displayName)
                .font(.caption)
                .foregroundStyle(.secondary)
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(color: .black.opacity(0.05), radius: 4, y: 2)
    }
}

struct SyncHistoryRow: View {
    let result: SyncResult

    var body: some View {
        HStack {
            Image(systemName: statusIcon)
                .foregroundStyle(statusColor)

            VStack(alignment: .leading, spacing: 2) {
                Text(formattedDate)
                    .font(.subheadline)
                Text("\(result.autoResolved)/\(result.totalConflicts) gelöst")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }

            Spacer()

            Text(result.formattedDuration)
                .font(.caption)
                .foregroundStyle(.secondary)
        }
        .padding(.vertical, 4)
    }

    private var statusIcon: String {
        switch result.status {
        case .success: return "checkmark.circle.fill"
        case .partialSuccess: return "exclamationmark.circle.fill"
        case .failed: return "xmark.circle.fill"
        case .inProgress: return "arrow.clockwise"
        }
    }

    private var statusColor: Color {
        switch result.status {
        case .success: return .green
        case .partialSuccess: return .orange
        case .failed: return .red
        case .inProgress: return .blue
        }
    }

    private var formattedDate: String {
        let formatter = DateFormatter()
        formatter.dateStyle = .short
        formatter.timeStyle = .short
        return formatter.string(from: result.startedAt)
    }
}

#Preview {
    DashboardView()
        .environmentObject(AppState())
        .environmentObject(SyncCoordinator.shared)
}
