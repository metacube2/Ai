//
//  HardwareView.swift
//  AudioVUMeter
//
//  Hardware configuration and monitoring view for physical VU meters
//  Includes auto-probe functionality to detect connected hardware
//

import SwiftUI

// MARK: - Hardware Panel in Main View
struct HardwarePanelView: View {
    @EnvironmentObject var serialManager: SerialManager

    var body: some View {
        VStack(spacing: 15) {
            // Header
            HStack {
                Text("HARDWARE OUTPUT")
                    .font(.system(size: 11, weight: .semibold, design: .monospaced))
                    .foregroundColor(.gray)

                Spacer()

                // Connection status
                HStack(spacing: 6) {
                    Circle()
                        .fill(statusColor)
                        .frame(width: 8, height: 8)

                    Text(statusText)
                        .font(.system(size: 9, weight: .semibold, design: .monospaced))
                        .foregroundColor(statusColor)
                }
            }

            // Probing progress
            if serialManager.isProbing {
                VStack(spacing: 8) {
                    ProgressView(value: serialManager.probeProgress)
                        .progressViewStyle(.linear)

                    Text(serialManager.probeStatus)
                        .font(.system(size: 10, design: .monospaced))
                        .foregroundColor(.orange)
                }
            } else {
                // 4 Physical Dial Indicators
                HStack(spacing: 15) {
                    ForEach(0..<4) { index in
                        DialIndicatorView(
                            dialNumber: index + 1,
                            value: serialManager.dialValues[index],
                            channelName: shortChannelName(serialManager.dialConfigs[index].dialChannel),
                            isConnected: serialManager.isConnected
                        )
                    }
                }
            }

            // Buttons
            HStack(spacing: 10) {
                // Auto-probe button
                Button(action: {
                    if serialManager.isProbing {
                        serialManager.stopAutoProbe()
                    } else {
                        serialManager.startAutoProbe()
                    }
                }) {
                    HStack {
                        Image(systemName: serialManager.isProbing ? "stop.fill" : "magnifyingglass")
                        Text(serialManager.isProbing ? "Stop" : "Auto-Find")
                    }
                    .frame(maxWidth: .infinity)
                }
                .buttonStyle(ProbeButtonStyle(isProbing: serialManager.isProbing))
                .disabled(serialManager.isConnected)

                // Connect button
                Button(action: {
                    serialManager.toggleConnection()
                }) {
                    HStack {
                        Image(systemName: serialManager.isConnected ? "antenna.radiowaves.left.and.right.slash" : "antenna.radiowaves.left.and.right")
                        Text(serialManager.isConnected ? "Disconnect" : "Connect")
                    }
                    .frame(maxWidth: .infinity)
                }
                .buttonStyle(HardwareButtonStyle(isConnected: serialManager.isConnected))
                .disabled(serialManager.isProbing)
            }

            // Stats / Device info
            if serialManager.isConnected {
                HStack {
                    Text("TX: \(formatBytes(serialManager.bytesSent))")
                        .font(.system(size: 9, design: .monospaced))
                        .foregroundColor(.gray)

                    Spacer()

                    Text(serialManager.selectedPortPath.components(separatedBy: "/").last ?? "")
                        .font(.system(size: 9, design: .monospaced))
                        .foregroundColor(.gray)
                }
            } else if let detected = serialManager.detectedDevice {
                HStack {
                    Image(systemName: "checkmark.circle.fill")
                        .foregroundColor(.green)
                        .font(.system(size: 10))

                    Text("Found: \(detected.name)")
                        .font(.system(size: 9, design: .monospaced))
                        .foregroundColor(.green)

                    Spacer()

                    if let vid = detected.vendorID, let pid = detected.productID {
                        Text(String(format: "%04X:%04X", vid, pid))
                            .font(.system(size: 8, design: .monospaced))
                            .foregroundColor(.gray)
                    }
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
        if serialManager.isProbing { return .orange }
        if serialManager.isConnected { return .green }
        return .red
    }

    private var statusText: String {
        if serialManager.isProbing { return "PROBING" }
        if serialManager.isConnected { return "CONNECTED" }
        return "DISCONNECTED"
    }

    private var borderColor: Color {
        if serialManager.isProbing { return .orange.opacity(0.3) }
        if serialManager.isConnected { return .green.opacity(0.3) }
        return .clear
    }

    private func shortChannelName(_ channel: DialChannel) -> String {
        switch channel {
        case .audioLeft: return "L"
        case .audioRight: return "R"
        case .audioPeak: return "PK"
        case .audioMono: return "M"
        case .cpu: return "CPU"
        case .ram: return "RAM"
        case .disk: return "DSK"
        case .network: return "NET"
        }
    }

    private func formatBytes(_ bytes: UInt64) -> String {
        if bytes < 1024 { return "\(bytes) B" }
        if bytes < 1024 * 1024 { return String(format: "%.1f KB", Double(bytes) / 1024) }
        return String(format: "%.1f MB", Double(bytes) / (1024 * 1024))
    }
}

// MARK: - Single Dial Indicator
struct DialIndicatorView: View {
    let dialNumber: Int
    let value: Int
    let channelName: String
    let isConnected: Bool

    var body: some View {
        VStack(spacing: 4) {
            // Dial number
            Text("D\(dialNumber)")
                .font(.system(size: 10, weight: .bold, design: .monospaced))
                .foregroundColor(.white.opacity(0.7))

            // Value arc
            ZStack {
                // Background arc
                Circle()
                    .trim(from: 0.25, to: 0.75)
                    .stroke(Color.gray.opacity(0.2), lineWidth: 4)
                    .frame(width: 50, height: 50)
                    .rotationEffect(.degrees(180))

                // Value arc
                Circle()
                    .trim(from: 0.25, to: 0.25 + (Double(value) / 255.0) * 0.5)
                    .stroke(
                        isConnected ? dialColor(for: value) : Color.gray,
                        style: StrokeStyle(lineWidth: 4, lineCap: .round)
                    )
                    .frame(width: 50, height: 50)
                    .rotationEffect(.degrees(180))

                // Value text
                VStack(spacing: 0) {
                    Text("\(value)")
                        .font(.system(size: 14, weight: .bold, design: .monospaced))
                        .foregroundColor(isConnected ? .white : .gray)
                }
            }

            // Channel name
            Text(channelName)
                .font(.system(size: 9, weight: .semibold, design: .monospaced))
                .foregroundColor(channelColor(channelName))
        }
    }

    private func dialColor(for value: Int) -> Color {
        let ratio = Double(value) / 255.0
        if ratio > 0.9 { return .red }
        if ratio > 0.75 { return .orange }
        if ratio > 0.5 { return .yellow }
        return .green
    }

    private func channelColor(_ name: String) -> Color {
        switch name {
        case "L", "R", "PK", "M": return .green
        case "CPU": return .blue
        case "RAM": return .purple
        case "DSK": return .teal
        case "NET": return .indigo
        default: return .gray
        }
    }
}

// MARK: - Hardware Settings View
struct HardwareSettingsView: View {
    @EnvironmentObject var serialManager: SerialManager

    var body: some View {
        Form {
            // Auto-Probe Section
            Section("Auto-Detect Hardware") {
                HStack {
                    Button(action: {
                        if serialManager.isProbing {
                            serialManager.stopAutoProbe()
                        } else {
                            serialManager.startAutoProbe()
                        }
                    }) {
                        HStack {
                            Image(systemName: serialManager.isProbing ? "stop.fill" : "magnifyingglass.circle.fill")
                            Text(serialManager.isProbing ? "Stop Probing" : "Auto-Detect VU Meter")
                        }
                    }
                    .disabled(serialManager.isConnected)

                    Spacer()

                    Button("Quick Connect") {
                        serialManager.autoConnect()
                    }
                    .disabled(serialManager.isConnected || serialManager.isProbing)
                }

                if serialManager.isProbing {
                    VStack(alignment: .leading, spacing: 8) {
                        ProgressView(value: serialManager.probeProgress) {
                            Text(serialManager.probeStatus)
                                .font(.caption)
                        }
                    }
                }

                if let detected = serialManager.detectedDevice {
                    HStack {
                        Image(systemName: "checkmark.circle.fill")
                            .foregroundColor(.green)
                        VStack(alignment: .leading) {
                            Text("Detected: \(detected.name)")
                                .font(.headline)
                            if let vid = detected.vendorID, let pid = detected.productID {
                                Text(String(format: "USB ID: %04X:%04X", vid, pid))
                                    .font(.caption)
                                    .foregroundColor(.secondary)
                            }
                        }
                    }
                }
            }

            // Connection Section
            Section("Serial Connection") {
                // Port selection with USB info
                Picker("Port", selection: $serialManager.selectedPortPath) {
                    Text("Select Port...").tag("")
                    ForEach(serialManager.availablePorts) { port in
                        HStack {
                            if port.isVUMeter {
                                Image(systemName: "star.fill")
                                    .foregroundColor(.yellow)
                            }
                            Text(port.name)
                            if let vid = port.vendorID, let pid = port.productID {
                                Text(String(format: "(%04X:%04X)", vid, pid))
                                    .foregroundColor(.secondary)
                                    .font(.caption)
                            }
                        }
                        .tag(port.path)
                    }
                }

                HStack {
                    Button(action: { serialManager.refreshPorts() }) {
                        Label("Refresh", systemImage: "arrow.clockwise")
                    }
                    .buttonStyle(.borderless)

                    Spacer()

                    Text("\(serialManager.availablePorts.count) ports found")
                        .font(.caption)
                        .foregroundColor(.secondary)
                }

                // Baud rate
                Picker("Baud Rate", selection: $serialManager.baudRate) {
                    ForEach(SerialManager.availableBaudRates, id: \.self) { rate in
                        Text("\(rate)").tag(rate)
                    }
                }

                // Protocol
                Picker("Protocol", selection: $serialManager.selectedProtocol) {
                    ForEach(SerialProtocol.allCases) { proto in
                        Text(proto.rawValue).tag(proto)
                    }
                }

                // Connect button
                Button(action: { serialManager.toggleConnection() }) {
                    HStack {
                        Image(systemName: serialManager.isConnected ? "bolt.slash.fill" : "bolt.fill")
                        Text(serialManager.isConnected ? "Disconnect" : "Connect")
                    }
                }
                .foregroundColor(serialManager.isConnected ? .red : .green)
                .disabled(serialManager.isProbing)
            }

            // Dial Configuration Section
            Section("Dial Assignments") {
                ForEach(0..<4) { index in
                    DialConfigRow(
                        dialNumber: index + 1,
                        config: $serialManager.dialConfigs[index]
                    )
                }
            }

            // Advanced Settings
            Section("Advanced") {
                ForEach(0..<4) { index in
                    DisclosureGroup("Dial \(index + 1) Settings") {
                        HStack {
                            Text("Min Value")
                            Spacer()
                            TextField("0", value: $serialManager.dialConfigs[index].minValue, format: .number)
                                .frame(width: 60)
                                .textFieldStyle(.roundedBorder)
                        }

                        HStack {
                            Text("Max Value")
                            Spacer()
                            TextField("255", value: $serialManager.dialConfigs[index].maxValue, format: .number)
                                .frame(width: 60)
                                .textFieldStyle(.roundedBorder)
                        }

                        Toggle("Invert", isOn: $serialManager.dialConfigs[index].inverted)

                        HStack {
                            Text("Smoothing")
                            Slider(value: $serialManager.dialConfigs[index].smoothing, in: 0...0.9)
                            Text("\(Int(serialManager.dialConfigs[index].smoothing * 100))%")
                                .frame(width: 40)
                        }
                    }
                }
            }

            // Probe Results (for debugging)
            if !serialManager.probeResults.isEmpty {
                Section("Probe Results") {
                    ForEach(serialManager.probeResults.indices, id: \.self) { index in
                        let result = serialManager.probeResults[index]
                        HStack {
                            Image(systemName: result.success ? "checkmark.circle" : "xmark.circle")
                                .foregroundColor(result.success ? .green : .red)
                            VStack(alignment: .leading) {
                                Text(result.port.name)
                                    .font(.caption)
                                Text("\(result.baudRate) baud - \(result.protocol_.rawValue)")
                                    .font(.caption2)
                                    .foregroundColor(.secondary)
                            }
                            Spacer()
                            if let response = result.response {
                                Text(response.prefix(20) + "...")
                                    .font(.caption2)
                                    .foregroundColor(.green)
                            }
                        }
                    }

                    Button("Clear Results") {
                        serialManager.probeResults.removeAll()
                    }
                }
            }

            // Protocol Info
            Section("Protocol Reference") {
                VStack(alignment: .leading, spacing: 8) {
                    protocolInfo
                }
                .font(.system(size: 11, design: .monospaced))
                .foregroundColor(.secondary)
            }
        }
        .formStyle(.grouped)
    }

    @ViewBuilder
    private var protocolInfo: some View {
        switch serialManager.selectedProtocol {
        case .rawBytes:
            Text("Format: [0xAA] [D1] [D2] [D3] [D4] [0x55]")
            Text("Values: 0-255 per dial")
        case .textCommand:
            Text("Format: CH1:val;CH2:val;CH3:val;CH4:val\\n")
            Text("Values: 0-255 per channel")
        case .json:
            Text("Format: {\"dials\":[d1,d2,d3,d4]}\\n")
            Text("Values: 0-255 array")
        case .vuServer:
            Text("Format: #0:val\\n#1:val\\n#2:val\\n#3:val\\n")
            Text("Values: 0-100 percentage per dial")
        }
    }
}

// MARK: - Dial Config Row
struct DialConfigRow: View {
    let dialNumber: Int
    @Binding var config: DialConfig

    var body: some View {
        HStack {
            Text("Dial \(dialNumber)")
                .font(.system(.body, design: .monospaced))
                .frame(width: 60, alignment: .leading)

            Picker("", selection: $config.dialChannel) {
                ForEach(DialChannel.allCases) { channel in
                    Text(channel.rawValue).tag(channel)
                }
            }
            .labelsHidden()
        }
    }
}

// MARK: - Button Styles
struct HardwareButtonStyle: ButtonStyle {
    let isConnected: Bool

    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .font(.system(size: 12, weight: .semibold))
            .foregroundColor(.white)
            .padding(.vertical, 8)
            .background(
                RoundedRectangle(cornerRadius: 8)
                    .fill(isConnected ? Color.red.opacity(0.7) : Color.green.opacity(0.7))
                    .opacity(configuration.isPressed ? 0.6 : 1.0)
            )
    }
}

struct ProbeButtonStyle: ButtonStyle {
    let isProbing: Bool

    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .font(.system(size: 12, weight: .semibold))
            .foregroundColor(.white)
            .padding(.vertical, 8)
            .background(
                RoundedRectangle(cornerRadius: 8)
                    .fill(isProbing ? Color.orange.opacity(0.7) : Color.blue.opacity(0.7))
                    .opacity(configuration.isPressed ? 0.6 : 1.0)
            )
    }
}

// MARK: - Preview
#Preview {
    HardwareSettingsView()
        .environmentObject(SerialManager())
        .frame(width: 500, height: 700)
}
