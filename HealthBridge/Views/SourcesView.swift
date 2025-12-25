import SwiftUI

struct SourcesView: View {
    @StateObject private var sourceManager = SourceManager.shared
    @State private var selectedSource: HealthSource?
    @State private var isRefreshing = false

    var body: some View {
        NavigationStack {
            Group {
                if sourceManager.sources.isEmpty && !sourceManager.isDiscovering {
                    emptyState
                } else {
                    sourcesList
                }
            }
            .navigationTitle("Quellen")
            .toolbar {
                ToolbarItem(placement: .topBarTrailing) {
                    Button {
                        Task { await refreshSources() }
                    } label: {
                        if isRefreshing {
                            ProgressView()
                                .scaleEffect(0.8)
                        } else {
                            Image(systemName: "arrow.clockwise")
                        }
                    }
                    .disabled(isRefreshing)
                }
            }
            .sheet(item: $selectedSource) { source in
                SourceDetailView(source: source)
            }
            .task {
                if sourceManager.sources.isEmpty {
                    await refreshSources()
                }
            }
        }
    }

    // MARK: - Empty State

    private var emptyState: some View {
        VStack(spacing: 16) {
            Image(systemName: "antenna.radiowaves.left.and.right")
                .font(.system(size: 64))
                .foregroundStyle(.secondary)

            Text("Keine Quellen gefunden")
                .font(.title2)
                .fontWeight(.semibold)

            Text("Verbinden Sie Geräte mit Apple Health")
                .font(.subheadline)
                .foregroundStyle(.secondary)
                .multilineTextAlignment(.center)

            Button {
                Task { await refreshSources() }
            } label: {
                Label("Aktualisieren", systemImage: "arrow.clockwise")
            }
            .buttonStyle(.bordered)
        }
        .padding()
    }

    // MARK: - Sources List

    private var sourcesList: some View {
        List {
            ForEach(groupedSources.keys.sorted(by: { $0.priority > $1.priority }), id: \.self) { category in
                Section(category.displayName) {
                    ForEach(groupedSources[category] ?? []) { source in
                        SourceRow(source: source)
                            .contentShape(Rectangle())
                            .onTapGesture {
                                selectedSource = source
                            }
                    }
                }
            }
        }
        .listStyle(.insetGrouped)
        .refreshable {
            await refreshSources()
        }
    }

    private var groupedSources: [SourceCategory: [HealthSource]] {
        Dictionary(grouping: sourceManager.sources, by: { $0.category })
    }

    private func refreshSources() async {
        isRefreshing = true
        defer { isRefreshing = false }
        await sourceManager.discoverSources()
    }
}

// MARK: - Source Row

struct SourceRow: View {
    let source: HealthSource
    @StateObject private var sourceManager = SourceManager.shared

    var body: some View {
        HStack {
            Image(systemName: source.category.icon)
                .font(.title2)
                .foregroundStyle(.blue)
                .frame(width: 40)

            VStack(alignment: .leading, spacing: 4) {
                Text(source.displayName)
                    .font(.headline)

                Text("\(source.supportedDataTypes.count) Datentypen")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }

            Spacer()

            if let status = sourceManager.sourceHealthStatus[source.id] {
                Image(systemName: status.syncStatus.icon)
                    .foregroundStyle(statusColor(for: status.syncStatus))
            }

            Image(systemName: "chevron.right")
                .font(.caption)
                .foregroundStyle(.secondary)
        }
        .padding(.vertical, 4)
    }

    private func statusColor(for status: SourceHealthStatus.SyncStatus) -> Color {
        switch status {
        case .recentlySynced: return .green
        case .syncedToday: return .blue
        case .stale: return .orange
        case .veryStale, .neverSynced: return .red
        }
    }
}

// MARK: - Source Detail View

struct SourceDetailView: View {
    let source: HealthSource
    @Environment(\.dismiss) private var dismiss
    @StateObject private var sourceManager = SourceManager.shared
    @State private var healthReport: SourceHealthReport?
    @State private var isLoading = false

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 20) {
                    // Header
                    headerSection

                    // Capabilities
                    capabilitiesSection

                    // Data Types
                    dataTypesSection

                    // Health Report
                    if let report = healthReport {
                        healthReportSection(report)
                    }

                    // Priority Settings
                    prioritySection
                }
                .padding()
            }
            .navigationTitle(source.displayName)
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .confirmationAction) {
                    Button("Fertig") {
                        dismiss()
                    }
                }
            }
            .task {
                await loadHealthReport()
            }
        }
    }

    // MARK: - Header

    private var headerSection: some View {
        VStack(spacing: 12) {
            Image(systemName: source.category.icon)
                .font(.system(size: 48))
                .foregroundStyle(.blue)

            Text(source.displayName)
                .font(.title2)
                .fontWeight(.semibold)

            Text(source.category.displayName)
                .font(.subheadline)
                .foregroundStyle(.secondary)

            Text(source.bundleIdentifier)
                .font(.caption)
                .foregroundStyle(.secondary)
                .textSelection(.enabled)
        }
        .padding()
    }

    // MARK: - Capabilities

    private var capabilitiesSection: some View {
        let capabilities = sourceManager.getSourceCapabilities(source)

        return VStack(alignment: .leading, spacing: 12) {
            Text("Fähigkeiten")
                .font(.headline)

            LazyVGrid(columns: [GridItem(.adaptive(minimum: 100))], spacing: 8) {
                CapabilityBadge(name: "Schritte", available: capabilities.canMeasureSteps)
                CapabilityBadge(name: "Herzfrequenz", available: capabilities.canMeasureHeartRate)
                CapabilityBadge(name: "Blutdruck", available: capabilities.canMeasureBloodPressure)
                CapabilityBadge(name: "SpO2", available: capabilities.canMeasureBloodOxygen)
                CapabilityBadge(name: "Schlaf", available: capabilities.canMeasureSleep)
                CapabilityBadge(name: "GPS", available: capabilities.hasGPS)
            }
        }
        .padding()
        .background(Color(.secondarySystemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }

    // MARK: - Data Types

    private var dataTypesSection: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Unterstützte Datentypen")
                .font(.headline)

            ForEach(Array(source.supportedDataTypes).sorted(by: { $0.displayName < $1.displayName }), id: \.self) { dataType in
                HStack {
                    Image(systemName: dataType.icon)
                        .foregroundStyle(.blue)
                        .frame(width: 24)
                    Text(dataType.displayName)
                    Spacer()
                    Text(dataType.unit)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
                .padding(.vertical, 4)
            }
        }
        .padding()
        .background(Color(.secondarySystemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }

    // MARK: - Health Report

    private func healthReportSection(_ report: SourceHealthReport) -> some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Zustand")
                .font(.headline)

            HStack {
                VStack(alignment: .leading) {
                    Text("Datensätze (24h)")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    Text("\(report.totalRecordCount)")
                        .font(.title3)
                        .fontWeight(.medium)
                }

                Spacer()

                VStack(alignment: .trailing) {
                    Text("Qualität")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    Image(systemName: report.overallQuality.icon)
                        .font(.title3)
                        .foregroundStyle(qualityColor(report.overallQuality))
                }
            }

            if let lastActivity = report.lastOverallActivity {
                HStack {
                    Text("Letzte Aktivität")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    Spacer()
                    Text(formattedDate(lastActivity))
                        .font(.subheadline)
                }
            }

            if report.hasSignificantGaps {
                HStack {
                    Image(systemName: "exclamationmark.triangle.fill")
                        .foregroundStyle(.orange)
                    Text("Datenlücken erkannt")
                        .font(.subheadline)
                }
            }
        }
        .padding()
        .background(Color(.secondarySystemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }

    // MARK: - Priority Section

    private var prioritySection: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Priorität")
                .font(.headline)

            Text("Höhere Priorität bedeutet, dass Daten dieser Quelle bevorzugt werden")
                .font(.caption)
                .foregroundStyle(.secondary)

            ForEach(Array(source.supportedDataTypes).sorted(by: { $0.displayName < $1.displayName }), id: \.self) { dataType in
                HStack {
                    Text(dataType.displayName)

                    Spacer()

                    Stepper(
                        "\(sourceManager.getPriority(for: source, dataType: dataType))",
                        value: Binding(
                            get: { sourceManager.getPriority(for: source, dataType: dataType) },
                            set: { sourceManager.setPriority($0, for: source, dataType: dataType) }
                        ),
                        in: 0...100,
                        step: 10
                    )
                    .frame(width: 150)
                }
                .padding(.vertical, 4)
            }
        }
        .padding()
        .background(Color(.secondarySystemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }

    // MARK: - Helpers

    private func loadHealthReport() async {
        isLoading = true
        defer { isLoading = false }
        healthReport = await sourceManager.getSourceHealth(source)
    }

    private func qualityColor(_ quality: DataQuality) -> Color {
        switch quality {
        case .complete: return .green
        case .partial: return .yellow
        case .missing: return .gray
        case .invalid: return .red
        }
    }

    private func formattedDate(_ date: Date) -> String {
        let formatter = RelativeDateTimeFormatter()
        formatter.unitsStyle = .abbreviated
        return formatter.localizedString(for: date, relativeTo: Date())
    }
}

// MARK: - Capability Badge

struct CapabilityBadge: View {
    let name: String
    let available: Bool

    var body: some View {
        HStack(spacing: 4) {
            Image(systemName: available ? "checkmark.circle.fill" : "xmark.circle")
                .foregroundStyle(available ? .green : .secondary)
            Text(name)
                .font(.caption)
                .foregroundStyle(available ? .primary : .secondary)
        }
        .padding(.horizontal, 8)
        .padding(.vertical, 4)
        .background(available ? Color.green.opacity(0.1) : Color.gray.opacity(0.1))
        .clipShape(Capsule())
    }
}

#Preview {
    SourcesView()
}
