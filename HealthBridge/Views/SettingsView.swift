import SwiftUI

struct SettingsView: View {
    @AppStorage("backgroundSyncEnabled") private var backgroundSyncEnabled = true
    @AppStorage("syncIntervalMinutes") private var syncIntervalMinutes = 15
    @AppStorage("notificationsEnabled") private var notificationsEnabled = true
    @AppStorage("notifyOnConflict") private var notifyOnConflict = true
    @AppStorage("notifyOnSyncComplete") private var notifyOnSyncComplete = false
    @AppStorage("autoResolveMinorConflicts") private var autoResolveMinorConflicts = true

    @StateObject private var syncCoordinator = SyncCoordinator.shared
    @StateObject private var healthKitManager = HealthKitManager.shared

    @State private var showingClearDataConfirmation = false
    @State private var showingExportSheet = false

    var body: some View {
        NavigationStack {
            Form {
                // Sync Settings
                Section("Synchronisierung") {
                    Toggle("Hintergrund-Sync", isOn: $backgroundSyncEnabled)

                    if backgroundSyncEnabled {
                        Picker("Intervall", selection: $syncIntervalMinutes) {
                            Text("15 Minuten").tag(15)
                            Text("30 Minuten").tag(30)
                            Text("1 Stunde").tag(60)
                            Text("2 Stunden").tag(120)
                        }
                    }

                    Toggle("Kleine Konflikte automatisch lösen", isOn: $autoResolveMinorConflicts)
                }

                // Notification Settings
                Section("Benachrichtigungen") {
                    Toggle("Benachrichtigungen aktivieren", isOn: $notificationsEnabled)

                    if notificationsEnabled {
                        Toggle("Bei neuen Konflikten", isOn: $notifyOnConflict)
                        Toggle("Nach Synchronisierung", isOn: $notifyOnSyncComplete)
                    }
                }

                // Health Status
                Section("HealthKit Status") {
                    HStack {
                        Text("Autorisierung")
                        Spacer()
                        if healthKitManager.isAuthorized {
                            Label("Erteilt", systemImage: "checkmark.circle.fill")
                                .foregroundStyle(.green)
                        } else {
                            Label("Ausstehend", systemImage: "exclamationmark.circle")
                                .foregroundStyle(.orange)
                        }
                    }

                    if !healthKitManager.isAuthorized {
                        Button("HealthKit-Zugriff anfordern") {
                            Task {
                                try? await healthKitManager.requestAuthorization()
                            }
                        }
                    }
                }

                // Statistics
                Section("Statistiken") {
                    StatRow(label: "Syncs heute", value: "\(syncCoordinator.todayStats.syncCount)")
                    StatRow(label: "Konflikte heute", value: "\(syncCoordinator.todayStats.totalConflicts)")
                    StatRow(label: "Automatisch gelöst", value: "\(syncCoordinator.todayStats.autoResolved)")
                    StatRow(label: "Auflösungsrate", value: "\(Int(syncCoordinator.todayStats.resolutionRate * 100))%")
                }

                // Data Management
                Section("Daten") {
                    Button("Sync-Verlauf exportieren") {
                        showingExportSheet = true
                    }

                    Button("Sync-Verlauf löschen", role: .destructive) {
                        showingClearDataConfirmation = true
                    }
                }

                // About
                Section("Info") {
                    HStack {
                        Text("Version")
                        Spacer()
                        Text("1.0.0")
                            .foregroundStyle(.secondary)
                    }

                    Link(destination: URL(string: "https://apple.com/health")!) {
                        HStack {
                            Text("Apple Health")
                            Spacer()
                            Image(systemName: "arrow.up.right.square")
                                .foregroundStyle(.secondary)
                        }
                    }
                }
            }
            .navigationTitle("Einstellungen")
            .confirmationDialog(
                "Sync-Verlauf löschen?",
                isPresented: $showingClearDataConfirmation,
                titleVisibility: .visible
            ) {
                Button("Löschen", role: .destructive) {
                    syncCoordinator.clearHistory()
                }
                Button("Abbrechen", role: .cancel) {}
            } message: {
                Text("Der gesamte Sync-Verlauf wird gelöscht. Dies kann nicht rückgängig gemacht werden.")
            }
            .sheet(isPresented: $showingExportSheet) {
                ExportView()
            }
        }
    }
}

// MARK: - Stat Row

struct StatRow: View {
    let label: String
    let value: String

    var body: some View {
        HStack {
            Text(label)
            Spacer()
            Text(value)
                .foregroundStyle(.secondary)
        }
    }
}

// MARK: - Export View

struct ExportView: View {
    @Environment(\.dismiss) private var dismiss
    @StateObject private var syncCoordinator = SyncCoordinator.shared

    @State private var exportFormat: ExportFormat = .json
    @State private var isExporting = false

    enum ExportFormat: String, CaseIterable {
        case json = "JSON"
        case csv = "CSV"
    }

    var body: some View {
        NavigationStack {
            VStack(spacing: 20) {
                Image(systemName: "square.and.arrow.up")
                    .font(.system(size: 48))
                    .foregroundStyle(.blue)

                Text("Daten exportieren")
                    .font(.title2)
                    .fontWeight(.semibold)

                Picker("Format", selection: $exportFormat) {
                    ForEach(ExportFormat.allCases, id: \.self) { format in
                        Text(format.rawValue).tag(format)
                    }
                }
                .pickerStyle(.segmented)
                .padding(.horizontal)

                Text("\(syncCoordinator.syncHistory.count) Sync-Einträge")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)

                Spacer()

                Button {
                    exportData()
                } label: {
                    Label("Exportieren", systemImage: "square.and.arrow.up")
                        .frame(maxWidth: .infinity)
                }
                .buttonStyle(.borderedProminent)
                .padding()
                .disabled(isExporting || syncCoordinator.syncHistory.isEmpty)
            }
            .padding()
            .navigationTitle("Export")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Abbrechen") {
                        dismiss()
                    }
                }
            }
        }
    }

    private func exportData() {
        isExporting = true

        // In a real app, this would create and share a file
        DispatchQueue.main.asyncAfter(deadline: .now() + 1) {
            isExporting = false
            dismiss()
        }
    }
}

#Preview {
    SettingsView()
}
