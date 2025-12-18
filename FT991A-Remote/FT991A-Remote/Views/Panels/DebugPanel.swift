//
//  DebugPanel.swift
//  FT991A-Remote
//
//  CAT command console for debugging
//

import SwiftUI

// MARK: - Debug Panel

struct DebugPanel: View {
    @EnvironmentObject var radioViewModel: RadioViewModel

    @State private var commandInput = ""
    @State private var autoScroll = true
    @State private var showOnlySent = false
    @State private var showOnlyReceived = false

    var filteredHistory: [CommandLogEntry] {
        radioViewModel.commandHistory.filter { entry in
            if showOnlySent && entry.direction != .sent { return false }
            if showOnlyReceived && entry.direction != .received { return false }
            return true
        }
    }

    var body: some View {
        VStack(spacing: 0) {
            // Header
            HStack {
                Text("CAT Konsole")
                    .font(.headline)

                Spacer()

                // Filter buttons
                Toggle("TX", isOn: Binding(
                    get: { showOnlySent },
                    set: { showOnlySent = $0; if $0 { showOnlyReceived = false } }
                ))
                .toggleStyle(.button)
                .controlSize(.small)

                Toggle("RX", isOn: Binding(
                    get: { showOnlyReceived },
                    set: { showOnlyReceived = $0; if $0 { showOnlySent = false } }
                ))
                .toggleStyle(.button)
                .controlSize(.small)

                Toggle(isOn: $autoScroll) {
                    Image(systemName: "arrow.down.to.line")
                }
                .toggleStyle(.button)
                .controlSize(.small)
                .help("Auto-Scroll")

                Button {
                    radioViewModel.clearCommandHistory()
                } label: {
                    Image(systemName: "trash")
                }
                .controlSize(.small)
                .help("Verlauf löschen")
            }
            .padding(.horizontal)
            .padding(.vertical, 8)
            .background(Color.secondary.opacity(0.1))

            Divider()

            // Command history
            ScrollViewReader { proxy in
                ScrollView {
                    LazyVStack(alignment: .leading, spacing: 2) {
                        ForEach(filteredHistory) { entry in
                            CommandLogRow(entry: entry)
                                .id(entry.id)
                        }
                    }
                    .padding(.horizontal, 8)
                    .padding(.vertical, 4)
                }
                .font(.system(size: 11, design: .monospaced))
                .onChange(of: radioViewModel.commandHistory.count) { _, _ in
                    if autoScroll, let last = filteredHistory.last {
                        withAnimation {
                            proxy.scrollTo(last.id, anchor: .bottom)
                        }
                    }
                }
            }

            Divider()

            // Command input
            HStack {
                TextField("CAT-Befehl eingeben (z.B. FA;)", text: $commandInput)
                    .textFieldStyle(.plain)
                    .font(.system(size: 12, design: .monospaced))
                    .onSubmit {
                        sendCommand()
                    }

                Button("Senden") {
                    sendCommand()
                }
                .disabled(commandInput.isEmpty || !radioViewModel.isConnected)
                .keyboardShortcut(.return, modifiers: [])
            }
            .padding(8)
            .background(Color.secondary.opacity(0.1))

            // Statistics
            HStack {
                Text("TX: \(radioViewModel.bytesSent) Bytes")
                Spacer()
                Text("RX: \(radioViewModel.bytesReceived) Bytes")
                Spacer()
                Text("\(radioViewModel.commandHistory.count) Befehle")
            }
            .font(.caption)
            .foregroundColor(.secondary)
            .padding(.horizontal, 8)
            .padding(.vertical, 4)
        }
    }

    private func sendCommand() {
        guard !commandInput.isEmpty else { return }

        var cmd = commandInput.trimmingCharacters(in: .whitespaces)
        if !cmd.hasSuffix(";") {
            cmd += ";"
        }

        radioViewModel.sendRawCommand(cmd)
        commandInput = ""
    }
}

// MARK: - Command Log Row

struct CommandLogRow: View {
    let entry: CommandLogEntry

    var body: some View {
        HStack(alignment: .top, spacing: 8) {
            Text(entry.timeString)
                .foregroundColor(.secondary)
                .frame(width: 80, alignment: .leading)

            Text(entry.direction.symbol)
                .foregroundColor(entry.direction == .sent ? .blue : .green)
                .frame(width: 15)

            Text(entry.command)
                .foregroundColor(.primary)

            if !entry.description.isEmpty {
                Text("// \(entry.description)")
                    .foregroundColor(.secondary)
            }

            Spacer()
        }
        .padding(.vertical, 1)
    }
}

// MARK: - Preview

#Preview {
    DebugPanel()
        .environmentObject(RadioViewModel())
        .frame(width: 400, height: 500)
}
