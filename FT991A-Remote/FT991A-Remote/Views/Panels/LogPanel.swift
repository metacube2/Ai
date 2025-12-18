//
//  LogPanel.swift
//  FT991A-Remote
//
//  QSO Log panel
//

import SwiftUI

// MARK: - Log Panel

struct LogPanel: View {
    @EnvironmentObject var logViewModel: LogViewModel
    @EnvironmentObject var radioViewModel: RadioViewModel

    @State private var isAddingQSO = false

    var body: some View {
        VStack(spacing: 0) {
            // Header
            HStack {
                Text("QSO Log")
                    .font(.headline)

                Spacer()

                Text("\(logViewModel.totalQSOs) QSOs")
                    .font(.caption)
                    .foregroundColor(.secondary)

                Button {
                    isAddingQSO = true
                } label: {
                    Image(systemName: "plus")
                }
                .help("Neues QSO hinzufügen")
            }
            .padding(.horizontal)
            .padding(.vertical, 8)
            .background(Color.secondary.opacity(0.1))

            Divider()

            // Quick entry form
            if isAddingQSO {
                QuickLogEntry(isPresented: $isAddingQSO)
                Divider()
            }

            // Search and filter
            HStack {
                Image(systemName: "magnifyingglass")
                    .foregroundColor(.secondary)

                TextField("Suchen...", text: $logViewModel.searchText)
                    .textFieldStyle(.plain)

                if !logViewModel.searchText.isEmpty {
                    Button {
                        logViewModel.searchText = ""
                    } label: {
                        Image(systemName: "xmark.circle.fill")
                            .foregroundColor(.secondary)
                    }
                    .buttonStyle(.plain)
                }

                Picker("Sortierung", selection: $logViewModel.sortOrder) {
                    ForEach(LogViewModel.SortOrder.allCases, id: \.self) { order in
                        Text(order.rawValue).tag(order)
                    }
                }
                .pickerStyle(.menu)
                .frame(width: 150)
            }
            .padding(.horizontal)
            .padding(.vertical, 6)

            Divider()

            // QSO List
            List {
                ForEach(logViewModel.filteredEntries) { entry in
                    QSORow(entry: entry)
                        .contextMenu {
                            Button("Bearbeiten") {
                                logViewModel.selectedEntry = entry
                            }
                            Button("Löschen", role: .destructive) {
                                logViewModel.deleteQSO(entry)
                            }
                        }
                }
                .onDelete(perform: logViewModel.deleteQSOs)
            }
            .listStyle(.plain)

            Divider()

            // Footer with statistics
            HStack {
                Text("\(logViewModel.uniqueCallsigns) Stationen")

                Spacer()

                if let file = logViewModel.currentLogFile {
                    Text(file.lastPathComponent)
                        .lineLimit(1)
                        .truncationMode(.middle)
                }
            }
            .font(.caption)
            .foregroundColor(.secondary)
            .padding(.horizontal)
            .padding(.vertical, 4)
        }
        .sheet(item: $logViewModel.selectedEntry) { entry in
            QSOEditSheet(entry: entry)
        }
    }
}

// MARK: - QSO Row

struct QSORow: View {
    let entry: QSOEntry

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            HStack {
                Text(entry.callsign)
                    .font(.headline)

                Spacer()

                Text(entry.dateDisplay)
                    .font(.caption)
                    .foregroundColor(.secondary)

                Text(entry.timeDisplay)
                    .font(.caption)
                    .foregroundColor(.secondary)
            }

            HStack {
                Text(entry.frequencyDisplay)
                    .font(.caption.monospacedDigit())

                Text(entry.mode.rawValue)
                    .font(.caption)
                    .padding(.horizontal, 6)
                    .padding(.vertical, 2)
                    .background(Color.accentColor.opacity(0.2))
                    .cornerRadius(4)

                Text(entry.bandDisplay)
                    .font(.caption)
                    .foregroundColor(.secondary)

                Spacer()

                Text("RST: \(entry.rstSent)/\(entry.rstReceived)")
                    .font(.caption)
                    .foregroundColor(.secondary)
            }

            if !entry.name.isEmpty || !entry.qth.isEmpty {
                HStack {
                    if !entry.name.isEmpty {
                        Text(entry.name)
                            .font(.caption)
                    }
                    if !entry.qth.isEmpty {
                        Text("- \(entry.qth)")
                            .font(.caption)
                            .foregroundColor(.secondary)
                    }
                }
            }
        }
        .padding(.vertical, 4)
    }
}

// MARK: - Quick Log Entry

struct QuickLogEntry: View {
    @EnvironmentObject var logViewModel: LogViewModel
    @EnvironmentObject var radioViewModel: RadioViewModel

    @Binding var isPresented: Bool

    var body: some View {
        VStack(spacing: 8) {
            HStack {
                TextField("Rufzeichen", text: $logViewModel.currentQSO.callsign)
                    .textFieldStyle(.roundedBorder)

                TextField("RST TX", text: $logViewModel.currentQSO.rstSent)
                    .textFieldStyle(.roundedBorder)
                    .frame(width: 50)

                TextField("RST RX", text: $logViewModel.currentQSO.rstReceived)
                    .textFieldStyle(.roundedBorder)
                    .frame(width: 50)
            }

            HStack {
                TextField("Name", text: $logViewModel.currentQSO.name)
                    .textFieldStyle(.roundedBorder)

                TextField("QTH", text: $logViewModel.currentQSO.qth)
                    .textFieldStyle(.roundedBorder)

                TextField("Locator", text: $logViewModel.currentQSO.locator)
                    .textFieldStyle(.roundedBorder)
                    .frame(width: 80)
            }

            HStack {
                Button("Von Radio") {
                    logViewModel.updateFromRadio(
                        frequency: radioViewModel.activeFrequency,
                        mode: radioViewModel.mode,
                        power: radioViewModel.power
                    )
                }
                .disabled(!radioViewModel.isConnected)

                Spacer()

                Button("Abbrechen") {
                    logViewModel.resetCurrentQSO()
                    isPresented = false
                }

                Button("Speichern") {
                    logViewModel.addQSO()
                    isPresented = false
                }
                .disabled(logViewModel.currentQSO.callsign.isEmpty)
                .keyboardShortcut(.return, modifiers: .command)
            }
        }
        .padding()
        .background(Color.secondary.opacity(0.05))
    }
}

// MARK: - QSO Edit Sheet

struct QSOEditSheet: View {
    @EnvironmentObject var logViewModel: LogViewModel
    @Environment(\.dismiss) var dismiss

    let entry: QSOEntry
    @State private var editedEntry: QSOEntry

    init(entry: QSOEntry) {
        self.entry = entry
        self._editedEntry = State(initialValue: entry)
    }

    var body: some View {
        VStack(spacing: 16) {
            Text("QSO bearbeiten")
                .font(.headline)

            Form {
                TextField("Rufzeichen", text: $editedEntry.callsign)
                TextField("Name", text: $editedEntry.name)
                TextField("QTH", text: $editedEntry.qth)
                TextField("Locator", text: $editedEntry.locator)

                HStack {
                    TextField("RST TX", text: $editedEntry.rstSent)
                    TextField("RST RX", text: $editedEntry.rstReceived)
                }

                Picker("Mode", selection: $editedEntry.mode) {
                    ForEach(OperatingMode.allCases, id: \.self) { mode in
                        Text(mode.rawValue).tag(mode)
                    }
                }

                TextField("Notizen", text: $editedEntry.notes, axis: .vertical)
                    .lineLimit(3...6)
            }
            .formStyle(.grouped)

            HStack {
                Button("Abbrechen") {
                    dismiss()
                }
                .keyboardShortcut(.escape, modifiers: [])

                Spacer()

                Button("Speichern") {
                    logViewModel.updateQSO(editedEntry)
                    dismiss()
                }
                .keyboardShortcut(.return, modifiers: .command)
            }
        }
        .padding()
        .frame(width: 400, height: 400)
    }
}

// MARK: - Preview

#Preview {
    LogPanel()
        .environmentObject(LogViewModel())
        .environmentObject(RadioViewModel())
        .frame(width: 350, height: 600)
}
