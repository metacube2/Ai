import SwiftUI

struct RulesView: View {
    @StateObject private var ruleEngine = RuleEngine.shared
    @StateObject private var sourceManager = SourceManager.shared
    @State private var selectedDataType: HealthDataType?
    @State private var showingResetConfirmation = false

    var body: some View {
        NavigationStack {
            List {
                Section {
                    Text("Regeln bestimmen, wie Konflikte zwischen verschiedenen Datenquellen automatisch gelöst werden.")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                ForEach(HealthDataType.allCases) { dataType in
                    RuleRow(
                        dataType: dataType,
                        rule: ruleEngine.getRule(for: dataType)
                    )
                    .contentShape(Rectangle())
                    .onTapGesture {
                        selectedDataType = dataType
                    }
                }
            }
            .listStyle(.insetGrouped)
            .navigationTitle("Regeln")
            .toolbar {
                ToolbarItem(placement: .topBarTrailing) {
                    Menu {
                        Button("Alle zurücksetzen", role: .destructive) {
                            showingResetConfirmation = true
                        }
                    } label: {
                        Image(systemName: "ellipsis.circle")
                    }
                }
            }
            .sheet(item: $selectedDataType) { dataType in
                RuleEditorView(dataType: dataType)
            }
            .confirmationDialog(
                "Alle Regeln zurücksetzen?",
                isPresented: $showingResetConfirmation,
                titleVisibility: .visible
            ) {
                Button("Zurücksetzen", role: .destructive) {
                    ruleEngine.resetAllToDefaults()
                }
                Button("Abbrechen", role: .cancel) {}
            } message: {
                Text("Alle Regeln werden auf die Standardwerte zurückgesetzt.")
            }
        }
    }
}

// MARK: - Rule Row

struct RuleRow: View {
    let dataType: HealthDataType
    let rule: MergeRule

    var body: some View {
        HStack {
            Image(systemName: dataType.icon)
                .foregroundStyle(.blue)
                .frame(width: 30)

            VStack(alignment: .leading, spacing: 4) {
                Text(dataType.displayName)
                    .font(.headline)

                HStack(spacing: 8) {
                    Image(systemName: rule.strategy.icon)
                        .font(.caption)
                    Text(rule.strategy.displayName)
                        .font(.caption)
                }
                .foregroundStyle(.secondary)
            }

            Spacer()

            if !rule.autoApply {
                Image(systemName: "hand.raised.fill")
                    .foregroundStyle(.orange)
                    .font(.caption)
            }

            Image(systemName: "chevron.right")
                .font(.caption)
                .foregroundStyle(.secondary)
        }
        .padding(.vertical, 4)
    }
}

// MARK: - Rule Editor View

struct RuleEditorView: View {
    let dataType: HealthDataType
    @Environment(\.dismiss) private var dismiss
    @StateObject private var ruleEngine = RuleEngine.shared
    @StateObject private var sourceManager = SourceManager.shared

    @State private var selectedStrategy: MergeStrategy
    @State private var autoApply: Bool
    @State private var primarySourceId: String?
    @State private var thresholdForManualReview: Double?
    @State private var useThreshold: Bool

    init(dataType: HealthDataType) {
        self.dataType = dataType
        let rule = RuleEngine.shared.getRule(for: dataType)
        _selectedStrategy = State(initialValue: rule.strategy)
        _autoApply = State(initialValue: rule.autoApply)
        _primarySourceId = State(initialValue: rule.primarySourceId)
        _thresholdForManualReview = State(initialValue: rule.thresholdForManualReview)
        _useThreshold = State(initialValue: rule.thresholdForManualReview != nil)
    }

    var body: some View {
        NavigationStack {
            Form {
                // Data Type Info
                Section {
                    HStack {
                        Image(systemName: dataType.icon)
                            .font(.title2)
                            .foregroundStyle(.blue)
                        VStack(alignment: .leading) {
                            Text(dataType.displayName)
                                .font(.headline)
                            Text(dataType.unit)
                                .font(.caption)
                                .foregroundStyle(.secondary)
                        }
                    }
                }

                // Strategy Selection
                Section("Strategie") {
                    Picker("Merge-Strategie", selection: $selectedStrategy) {
                        ForEach(MergeStrategy.allCases) { strategy in
                            Label(strategy.displayName, systemImage: strategy.icon)
                                .tag(strategy)
                        }
                    }
                    .pickerStyle(.navigationLink)

                    Text(selectedStrategy.description)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                // Primary Source (for exclusive/priority)
                if selectedStrategy == .exclusive || selectedStrategy == .priority {
                    Section("Primäre Quelle") {
                        let sources = sourceManager.sources.filter {
                            $0.supportedDataTypes.contains(dataType)
                        }

                        if sources.isEmpty {
                            Text("Keine Quellen für diesen Datentyp")
                                .foregroundStyle(.secondary)
                        } else {
                            Picker("Quelle", selection: $primarySourceId) {
                                Text("Automatisch").tag(nil as String?)
                                ForEach(sources) { source in
                                    Text(source.displayName).tag(source.bundleIdentifier as String?)
                                }
                            }
                        }
                    }
                }

                // Auto Apply
                Section("Automatisierung") {
                    Toggle("Automatisch anwenden", isOn: $autoApply)

                    if autoApply {
                        Toggle("Schwellenwert für manuelle Prüfung", isOn: $useThreshold)

                        if useThreshold {
                            VStack(alignment: .leading) {
                                Text("Bei Differenz über \(Int(thresholdForManualReview ?? 20))% nachfragen")
                                    .font(.caption)
                                    .foregroundStyle(.secondary)

                                Slider(
                                    value: Binding(
                                        get: { thresholdForManualReview ?? 20 },
                                        set: { thresholdForManualReview = $0 }
                                    ),
                                    in: 5...50,
                                    step: 5
                                )
                            }
                        }
                    }
                }

                // Reset
                Section {
                    Button("Auf Standard zurücksetzen", role: .destructive) {
                        let defaultRule = MergeRule.defaultRule(for: dataType)
                        selectedStrategy = defaultRule.strategy
                        autoApply = defaultRule.autoApply
                        primarySourceId = defaultRule.primarySourceId
                        thresholdForManualReview = defaultRule.thresholdForManualReview
                        useThreshold = defaultRule.thresholdForManualReview != nil
                    }
                }
            }
            .navigationTitle("Regel bearbeiten")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Abbrechen") {
                        dismiss()
                    }
                }

                ToolbarItem(placement: .confirmationAction) {
                    Button("Speichern") {
                        saveRule()
                        dismiss()
                    }
                }
            }
        }
    }

    private func saveRule() {
        let rule = MergeRule(
            dataType: dataType,
            strategy: selectedStrategy,
            primarySourceId: primarySourceId,
            autoApply: autoApply,
            thresholdForManualReview: useThreshold ? thresholdForManualReview : nil
        )
        ruleEngine.setRule(rule, for: dataType)
    }
}

#Preview {
    RulesView()
}
