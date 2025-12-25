import SwiftUI

struct ConflictsView: View {
    @EnvironmentObject var syncCoordinator: SyncCoordinator
    @State private var selectedConflict: Conflict?
    @State private var showingDetail = false

    var body: some View {
        NavigationStack {
            Group {
                if syncCoordinator.pendingConflicts.isEmpty {
                    emptyState
                } else {
                    conflictsList
                }
            }
            .navigationTitle("Konflikte")
            .toolbar {
                ToolbarItem(placement: .topBarTrailing) {
                    Menu {
                        Button("Alle automatisch lösen") {
                            Task { await resolveAllAuto() }
                        }
                        Button("Alle ignorieren", role: .destructive) {
                            ignoreAll()
                        }
                    } label: {
                        Image(systemName: "ellipsis.circle")
                    }
                    .disabled(syncCoordinator.pendingConflicts.isEmpty)
                }
            }
            .sheet(item: $selectedConflict) { conflict in
                ConflictDetailView(conflict: conflict) { selectedReadingId in
                    Task {
                        await resolveConflict(conflict, selectedReadingId: selectedReadingId)
                    }
                }
            }
        }
    }

    // MARK: - Empty State

    private var emptyState: some View {
        VStack(spacing: 16) {
            Image(systemName: "checkmark.circle.fill")
                .font(.system(size: 64))
                .foregroundStyle(.green)

            Text("Keine Konflikte")
                .font(.title2)
                .fontWeight(.semibold)

            Text("Alle Ihre Gesundheitsdaten sind synchronisiert")
                .font(.subheadline)
                .foregroundStyle(.secondary)
                .multilineTextAlignment(.center)
        }
        .padding()
    }

    // MARK: - Conflicts List

    private var conflictsList: some View {
        List {
            ForEach(groupedConflicts.keys.sorted(by: { $0.displayName < $1.displayName }), id: \.self) { dataType in
                Section(dataType.displayName) {
                    ForEach(groupedConflicts[dataType] ?? []) { conflict in
                        ConflictRow(conflict: conflict)
                            .contentShape(Rectangle())
                            .onTapGesture {
                                selectedConflict = conflict
                            }
                    }
                }
            }
        }
        .listStyle(.insetGrouped)
    }

    private var groupedConflicts: [HealthDataType: [Conflict]] {
        Dictionary(grouping: syncCoordinator.pendingConflicts, by: { $0.dataType })
    }

    // MARK: - Actions

    private func resolveConflict(_ conflict: Conflict, selectedReadingId: UUID) async {
        do {
            try await syncCoordinator.resolveConflict(conflict, selectedReadingId: selectedReadingId)
            selectedConflict = nil
        } catch {
            print("Failed to resolve conflict: \(error)")
        }
    }

    private func resolveAllAuto() async {
        for conflict in syncCoordinator.pendingConflicts {
            if let primaryReading = conflict.primarySourceReading {
                try? await syncCoordinator.resolveConflict(conflict, selectedReadingId: primaryReading.id)
            }
        }
    }

    private func ignoreAll() {
        for conflict in syncCoordinator.pendingConflicts {
            syncCoordinator.ignoreConflict(conflict)
        }
    }
}

// MARK: - Conflict Row

struct ConflictRow: View {
    let conflict: Conflict

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack {
                Image(systemName: conflict.dataType.icon)
                    .foregroundStyle(.blue)

                Text(conflict.timeWindow.formattedRange)
                    .font(.headline)

                Spacer()

                severityBadge
            }

            HStack(spacing: 16) {
                ForEach(conflict.readings.prefix(3)) { reading in
                    VStack(alignment: .leading, spacing: 2) {
                        Text(reading.sourceName)
                            .font(.caption)
                            .foregroundStyle(.secondary)
                        Text(reading.formattedValue)
                            .font(.subheadline)
                            .fontWeight(.medium)
                    }
                }
            }

            if conflict.readings.count > 3 {
                Text("+\(conflict.readings.count - 3) weitere")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
        .padding(.vertical, 4)
    }

    private var severityBadge: some View {
        Text(conflict.severity.displayName)
            .font(.caption2)
            .padding(.horizontal, 8)
            .padding(.vertical, 4)
            .background(severityColor.opacity(0.2))
            .foregroundStyle(severityColor)
            .clipShape(Capsule())
    }

    private var severityColor: Color {
        switch conflict.severity {
        case .minor: return .green
        case .moderate: return .yellow
        case .significant: return .orange
        case .major: return .red
        }
    }
}

// MARK: - Conflict Detail View

struct ConflictDetailView: View {
    let conflict: Conflict
    let onResolve: (UUID) -> Void

    @Environment(\.dismiss) private var dismiss
    @State private var selectedReadingId: UUID?

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 20) {
                    // Header
                    headerSection

                    // Readings
                    readingsSection

                    // Difference Info
                    differenceSection

                    Spacer()
                }
                .padding()
            }
            .navigationTitle("Konflikt lösen")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Abbrechen") {
                        dismiss()
                    }
                }

                ToolbarItem(placement: .confirmationAction) {
                    Button("Auswählen") {
                        if let id = selectedReadingId {
                            onResolve(id)
                        }
                    }
                    .disabled(selectedReadingId == nil)
                }
            }
        }
    }

    // MARK: - Header

    private var headerSection: some View {
        VStack(spacing: 8) {
            Image(systemName: conflict.dataType.icon)
                .font(.largeTitle)
                .foregroundStyle(.blue)

            Text(conflict.dataType.displayName)
                .font(.title2)
                .fontWeight(.semibold)

            Text(conflict.timeWindow.formattedDate)
                .font(.subheadline)
                .foregroundStyle(.secondary)

            Text(conflict.timeWindow.formattedRange)
                .font(.subheadline)
                .foregroundStyle(.secondary)
        }
        .padding()
    }

    // MARK: - Readings

    private var readingsSection: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Quellen")
                .font(.headline)

            ForEach(conflict.readings) { reading in
                ReadingCard(
                    reading: reading,
                    isSelected: selectedReadingId == reading.id,
                    dataType: conflict.dataType
                )
                .onTapGesture {
                    selectedReadingId = reading.id
                }
            }
        }
    }

    // MARK: - Difference

    private var differenceSection: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("Analyse")
                .font(.headline)

            HStack {
                VStack(alignment: .leading) {
                    Text("Differenz")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    Text(String(format: "%.1f %@", conflict.valueDifference, conflict.dataType.unit))
                        .font(.title3)
                        .fontWeight(.medium)
                }

                Spacer()

                VStack(alignment: .trailing) {
                    Text("Prozentual")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    Text(String(format: "%.1f%%", conflict.percentageDifference))
                        .font(.title3)
                        .fontWeight(.medium)
                }
            }
            .padding()
            .background(Color(.secondarySystemBackground))
            .clipShape(RoundedRectangle(cornerRadius: 12))
        }
    }
}

// MARK: - Reading Card

struct ReadingCard: View {
    let reading: SourceReading
    let isSelected: Bool
    let dataType: HealthDataType

    var body: some View {
        HStack {
            VStack(alignment: .leading, spacing: 4) {
                HStack {
                    Image(systemName: reading.sourceCategory.icon)
                        .foregroundStyle(.blue)
                    Text(reading.sourceName)
                        .font(.headline)
                }

                Text(reading.sourceCategory.displayName)
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }

            Spacer()

            VStack(alignment: .trailing, spacing: 4) {
                Text(reading.formattedValue)
                    .font(.title2)
                    .fontWeight(.semibold)
                Text(dataType.unit)
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }

            Image(systemName: isSelected ? "checkmark.circle.fill" : "circle")
                .font(.title2)
                .foregroundStyle(isSelected ? .blue : .secondary)
        }
        .padding()
        .background(isSelected ? Color.blue.opacity(0.1) : Color(.secondarySystemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .overlay(
            RoundedRectangle(cornerRadius: 12)
                .stroke(isSelected ? Color.blue : Color.clear, lineWidth: 2)
        )
    }
}

#Preview {
    ConflictsView()
        .environmentObject(SyncCoordinator.shared)
}
