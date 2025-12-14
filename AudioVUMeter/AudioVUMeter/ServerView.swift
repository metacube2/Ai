//
//  ServerView.swift
//  AudioVUMeter
//
//  Server configuration and status view
//  Allows enabling/disabling the VU Server for external app connections
//

import SwiftUI

// MARK: - Server Panel in Main View
struct ServerPanelView: View {
    @EnvironmentObject var vuServer: VUServer

    var body: some View {
        VStack(spacing: 12) {
            // Header
            HStack {
                Text("VU SERVER")
                    .font(.system(size: 11, weight: .semibold, design: .monospaced))
                    .foregroundColor(.gray)

                Spacer()

                // Status indicator
                HStack(spacing: 6) {
                    Circle()
                        .fill(statusColor)
                        .frame(width: 8, height: 8)

                    Text(statusText)
                        .font(.system(size: 9, weight: .semibold, design: .monospaced))
                        .foregroundColor(statusColor)
                }
            }

            // Quick info
            if vuServer.isRunning {
                HStack {
                    // Port info
                    Label(":\(vuServer.options.port)", systemImage: "network")
                        .font(.system(size: 10, design: .monospaced))
                        .foregroundColor(.cyan)

                    Spacer()

                    // Client count
                    Label("\(vuServer.connectedClients.count)", systemImage: "person.2.fill")
                        .font(.system(size: 10, design: .monospaced))
                        .foregroundColor(vuServer.connectedClients.isEmpty ? .gray : .green)

                    Spacer()

                    // External control indicator
                    if vuServer.externalControlActive {
                        Label("EXT", systemImage: "arrow.down.circle.fill")
                            .font(.system(size: 10, weight: .bold, design: .monospaced))
                            .foregroundColor(.orange)
                    }
                }
            }

            // Toggle button
            Button(action: {
                vuServer.toggle()
                vuServer.saveOptions()
            }) {
                HStack {
                    Image(systemName: vuServer.isRunning ? "stop.fill" : "play.fill")
                    Text(vuServer.isRunning ? "Stop Server" : "Start Server")
                }
                .frame(maxWidth: .infinity)
            }
            .buttonStyle(ServerButtonStyle(isRunning: vuServer.isRunning))

            // Last command (if any)
            if vuServer.isRunning && !vuServer.lastReceivedCommand.isEmpty {
                HStack {
                    Text("Last:")
                        .font(.system(size: 8, design: .monospaced))
                        .foregroundColor(.gray)

                    Text(vuServer.lastReceivedCommand.prefix(30) + (vuServer.lastReceivedCommand.count > 30 ? "..." : ""))
                        .font(.system(size: 8, design: .monospaced))
                        .foregroundColor(.cyan)
                        .lineLimit(1)

                    Spacer()
                }
            }
        }
        .padding()
        .background(
            RoundedRectangle(cornerRadius: 12)
                .fill(Color.black.opacity(0.3))
                .overlay(
                    RoundedRectangle(cornerRadius: 12)
                        .stroke(borderColor, lineWidth: 1)
                )
        )
        .padding(.horizontal)
    }

    private var statusColor: Color {
        if vuServer.isRunning {
            return vuServer.connectedClients.isEmpty ? .yellow : .green
        }
        return .gray
    }

    private var statusText: String {
        if vuServer.isRunning {
            if vuServer.connectedClients.isEmpty {
                return "LISTENING"
            }
            return "\(vuServer.connectedClients.count) CLIENT\(vuServer.connectedClients.count == 1 ? "" : "S")"
        }
        return "STOPPED"
    }

    private var borderColor: Color {
        if vuServer.externalControlActive { return .orange.opacity(0.5) }
        if vuServer.isRunning { return .cyan.opacity(0.3) }
        return .clear
    }
}

// MARK: - Server Settings View (Full)
struct ServerSettingsView: View {
    @EnvironmentObject var vuServer: VUServer
    @State private var portString: String = ""
    @State private var showAdvanced = false

    var body: some View {
        Form {
            // Main Server Section
            Section("Server Control") {
                // Enable/Disable toggle
                Toggle(isOn: Binding(
                    get: { vuServer.isRunning },
                    set: { newValue in
                        if newValue {
                            vuServer.start()
                        } else {
                            vuServer.stop()
                        }
                        vuServer.saveOptions()
                    }
                )) {
                    HStack {
                        Image(systemName: vuServer.isRunning ? "antenna.radiowaves.left.and.right" : "antenna.radiowaves.left.and.right.slash")
                            .foregroundColor(vuServer.isRunning ? .green : .gray)
                        Text("Server Enabled")
                    }
                }

                // Status
                if vuServer.isRunning {
                    HStack {
                        Text("Status")
                        Spacer()
                        Circle()
                            .fill(Color.green)
                            .frame(width: 8, height: 8)
                        Text("Running on port \(vuServer.options.port)")
                            .foregroundColor(.secondary)
                    }
                }

                // Error display
                if let error = vuServer.lastError {
                    HStack {
                        Image(systemName: "exclamationmark.triangle.fill")
                            .foregroundColor(.red)
                        Text(error)
                            .foregroundColor(.red)
                            .font(.caption)
                    }
                }
            }

            // Connection Settings
            Section("Connection Settings") {
                // Port
                HStack {
                    Text("Port")
                    Spacer()
                    TextField("Port", text: $portString)
                        .frame(width: 80)
                        .textFieldStyle(.roundedBorder)
                        .onAppear {
                            portString = String(vuServer.options.port)
                        }
                        .onChange(of: portString) { newValue in
                            if let port = UInt16(newValue), port > 0 {
                                vuServer.options.port = port
                                vuServer.saveOptions()
                            }
                        }
                }

                // Allow remote connections
                Toggle("Allow Remote Connections", isOn: $vuServer.options.allowRemote)
                    .onChange(of: vuServer.options.allowRemote) { _ in
                        vuServer.saveOptions()
                        if vuServer.isRunning {
                            vuServer.restart()
                        }
                    }

                if vuServer.options.allowRemote {
                    HStack {
                        Image(systemName: "exclamationmark.triangle.fill")
                            .foregroundColor(.orange)
                        Text("Warning: Remote access enabled. Anyone on your network can connect.")
                            .font(.caption)
                            .foregroundColor(.orange)
                    }
                }

                // Max clients
                Stepper("Max Clients: \(vuServer.options.maxClients)", value: $vuServer.options.maxClients, in: 1...20)
                    .onChange(of: vuServer.options.maxClients) { _ in
                        vuServer.saveOptions()
                    }
            }

            // Protocol Settings
            Section("Protocol") {
                Picker("Server Protocol", selection: $vuServer.options.serverProtocol) {
                    ForEach(ServerProtocol.allCases) { proto in
                        Text(proto.rawValue).tag(proto)
                    }
                }
                .onChange(of: vuServer.options.serverProtocol) { _ in
                    vuServer.saveOptions()
                }

                // Broadcast settings
                Toggle("Broadcast Levels to Clients", isOn: $vuServer.options.broadcastLevels)
                    .onChange(of: vuServer.options.broadcastLevels) { _ in
                        vuServer.saveOptions()
                    }

                if vuServer.options.broadcastLevels {
                    HStack {
                        Text("Broadcast Rate")
                        Slider(value: $vuServer.options.broadcastInterval, in: 0.033...1.0)
                        Text("\(Int(1.0 / vuServer.options.broadcastInterval)) Hz")
                            .frame(width: 50)
                    }
                    .onChange(of: vuServer.options.broadcastInterval) { _ in
                        vuServer.saveOptions()
                    }
                }
            }

            // Connected Clients
            Section("Connected Clients (\(vuServer.connectedClients.count))") {
                if vuServer.connectedClients.isEmpty {
                    HStack {
                        Image(systemName: "person.slash")
                            .foregroundColor(.gray)
                        Text("No clients connected")
                            .foregroundColor(.secondary)
                    }
                } else {
                    ForEach(vuServer.connectedClients) { client in
                        ClientRowView(client: client, onDisconnect: {
                            vuServer.disconnectClient(client)
                        })
                    }
                }
            }

            // Statistics
            if vuServer.isRunning {
                Section("Statistics") {
                    HStack {
                        Text("Received")
                        Spacer()
                        Text(formatBytes(vuServer.totalBytesReceived))
                            .foregroundColor(.secondary)
                            .font(.system(.body, design: .monospaced))
                    }

                    HStack {
                        Text("Sent")
                        Spacer()
                        Text(formatBytes(vuServer.totalBytesSent))
                            .foregroundColor(.secondary)
                            .font(.system(.body, design: .monospaced))
                    }

                    if vuServer.externalControlActive {
                        HStack {
                            Image(systemName: "arrow.down.circle.fill")
                                .foregroundColor(.orange)
                            Text("External Control Active")
                                .foregroundColor(.orange)
                            Spacer()
                            Button("Release") {
                                vuServer.externalControlActive = false
                                vuServer.receivedDialValues = nil
                            }
                            .buttonStyle(.borderless)
                        }
                    }

                    if !vuServer.lastReceivedCommand.isEmpty {
                        VStack(alignment: .leading) {
                            Text("Last Command")
                            Text(vuServer.lastReceivedCommand)
                                .font(.system(.caption, design: .monospaced))
                                .foregroundColor(.cyan)
                                .lineLimit(3)
                        }
                    }
                }
            }

            // Protocol Reference
            Section("Protocol Reference") {
                DisclosureGroup("VU Protocol") {
                    VStack(alignment: .leading, spacing: 4) {
                        Text("Send values: #channel:percentage")
                        Text("Example: #0:75 (dial 0 at 75%)")
                        Text("Channels: 0-3")
                        Text("Values: 0-100")
                        Text("Commands: ?, STATUS, RELEASE")
                    }
                    .font(.system(size: 11, design: .monospaced))
                    .foregroundColor(.secondary)
                }

                DisclosureGroup("JSON Protocol") {
                    VStack(alignment: .leading, spacing: 4) {
                        Text("{\"dials\":[v1,v2,v3,v4]}")
                        Text("or {\"d0\":v,\"d1\":v,...}")
                        Text("Values: 0-255")
                        Text("Commands: {\"cmd\":\"status\"}")
                    }
                    .font(.system(size: 11, design: .monospaced))
                    .foregroundColor(.secondary)
                }

                DisclosureGroup("Raw Bytes") {
                    VStack(alignment: .leading, spacing: 4) {
                        Text("Frame: [0xAA][D1][D2][D3][D4][0x55]")
                        Text("Values: 0-255 per byte")
                    }
                    .font(.system(size: 11, design: .monospaced))
                    .foregroundColor(.secondary)
                }
            }

            // Test Connection
            Section("Test") {
                VStack(alignment: .leading, spacing: 8) {
                    Text("Test with netcat:")
                        .font(.caption)
                        .foregroundColor(.secondary)

                    Text("echo '#0:50' | nc localhost \(vuServer.options.port)")
                        .font(.system(size: 10, design: .monospaced))
                        .padding(8)
                        .background(Color.black.opacity(0.3))
                        .cornerRadius(4)

                    Button("Copy Command") {
                        NSPasteboard.general.clearContents()
                        NSPasteboard.general.setString("echo '#0:50' | nc localhost \(vuServer.options.port)", forType: .string)
                    }
                    .buttonStyle(.borderless)
                }
            }
        }
        .formStyle(.grouped)
    }

    private func formatBytes(_ bytes: UInt64) -> String {
        if bytes < 1024 { return "\(bytes) B" }
        if bytes < 1024 * 1024 { return String(format: "%.1f KB", Double(bytes) / 1024) }
        return String(format: "%.1f MB", Double(bytes) / (1024 * 1024))
    }
}

// MARK: - Client Row View
struct ClientRowView: View {
    let client: ConnectedClient
    let onDisconnect: () -> Void

    var body: some View {
        HStack {
            VStack(alignment: .leading, spacing: 2) {
                Text(client.address)
                    .font(.system(.body, design: .monospaced))

                HStack(spacing: 10) {
                    Text("RX: \(formatBytes(client.bytesReceived))")
                    Text("TX: \(formatBytes(client.bytesSent))")
                }
                .font(.caption)
                .foregroundColor(.secondary)
            }

            Spacer()

            Text(timeAgo(client.lastActivity))
                .font(.caption)
                .foregroundColor(.secondary)

            Button(action: onDisconnect) {
                Image(systemName: "xmark.circle.fill")
                    .foregroundColor(.red)
            }
            .buttonStyle(.borderless)
        }
    }

    private func formatBytes(_ bytes: UInt64) -> String {
        if bytes < 1024 { return "\(bytes)B" }
        if bytes < 1024 * 1024 { return String(format: "%.1fK", Double(bytes) / 1024) }
        return String(format: "%.1fM", Double(bytes) / (1024 * 1024))
    }

    private func timeAgo(_ date: Date) -> String {
        let seconds = Int(-date.timeIntervalSinceNow)
        if seconds < 60 { return "\(seconds)s" }
        if seconds < 3600 { return "\(seconds / 60)m" }
        return "\(seconds / 3600)h"
    }
}

// MARK: - Server Settings Sheet
struct ServerSettingsSheet: View {
    @EnvironmentObject var vuServer: VUServer
    @Environment(\.dismiss) var dismiss

    var body: some View {
        VStack(spacing: 0) {
            // Header
            HStack {
                Text("VU Server Settings")
                    .font(.headline)
                Spacer()
                Button("Done") { dismiss() }
            }
            .padding()
            .background(Color(nsColor: .windowBackgroundColor))

            Divider()

            // Settings content
            ServerSettingsView()
                .environmentObject(vuServer)
        }
        .frame(width: 500, height: 650)
    }
}

// MARK: - Button Style
struct ServerButtonStyle: ButtonStyle {
    let isRunning: Bool

    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .font(.system(size: 12, weight: .semibold))
            .foregroundColor(.white)
            .padding(.vertical, 8)
            .background(
                RoundedRectangle(cornerRadius: 8)
                    .fill(isRunning ? Color.red.opacity(0.7) : Color.cyan.opacity(0.7))
                    .opacity(configuration.isPressed ? 0.6 : 1.0)
            )
    }
}

// MARK: - Preview
#Preview {
    ServerSettingsView()
        .environmentObject(VUServer())
        .frame(width: 500, height: 700)
}
