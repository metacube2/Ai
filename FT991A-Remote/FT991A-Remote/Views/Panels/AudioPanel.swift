//
//  AudioPanel.swift
//  FT991A-Remote
//
//  BlackHole audio routing panel
//

import SwiftUI

// MARK: - Audio Panel

struct AudioPanel: View {
    @StateObject private var audioRouter = AudioRouter()
    @EnvironmentObject var settingsController: SettingsController

    var body: some View {
        VStack(spacing: 0) {
            // Header
            HStack {
                Text("Audio Routing")
                    .font(.headline)

                Spacer()

                Button {
                    audioRouter.refreshDevices()
                } label: {
                    Image(systemName: "arrow.clockwise")
                }
                .help("Geräte aktualisieren")
            }
            .padding(.horizontal)
            .padding(.vertical, 8)
            .background(Color.secondary.opacity(0.1))

            Divider()

            ScrollView {
                VStack(alignment: .leading, spacing: 16) {
                    // BlackHole Status
                    GroupBox("BlackHole Status") {
                        HStack {
                            Circle()
                                .fill(audioRouter.isBlackHoleInstalled ? Color.green : Color.red)
                                .frame(width: 12, height: 12)

                            Text(audioRouter.isBlackHoleInstalled ? "Installiert" : "Nicht gefunden")

                            Spacer()

                            if !audioRouter.isBlackHoleInstalled {
                                Link("Installieren", destination: URL(string: "https://existential.audio/blackhole/")!)
                                    .font(.caption)
                            }
                        }
                        .padding(.vertical, 4)

                        if let device = audioRouter.blackHoleDevice {
                            Text("Gerät: \(device.name)")
                                .font(.caption)
                                .foregroundColor(.secondary)
                        }
                    }

                    // Input Device
                    GroupBox("Eingang (RX Audio)") {
                        Picker("Eingabegerät", selection: $audioRouter.selectedInputDevice) {
                            Text("Keines").tag(nil as AudioDeviceID?)
                            ForEach(audioRouter.inputDevices) { device in
                                Text(device.displayName).tag(device.id as AudioDeviceID?)
                            }
                        }
                        .pickerStyle(.menu)

                        if let ft991a = audioRouter.ft991aDevice {
                            HStack {
                                Image(systemName: "checkmark.circle.fill")
                                    .foregroundColor(.green)
                                Text("FT-991A erkannt: \(ft991a.name)")
                                    .font(.caption)
                            }
                        }
                    }

                    // Output Device
                    GroupBox("Ausgang (TX Audio)") {
                        Picker("Ausgabegerät", selection: $audioRouter.selectedOutputDevice) {
                            Text("Keines").tag(nil as AudioDeviceID?)
                            ForEach(audioRouter.outputDevices) { device in
                                Text(device.displayName).tag(device.id as AudioDeviceID?)
                            }
                        }
                        .pickerStyle(.menu)
                    }

                    // Digital Mode Configuration
                    GroupBox("Digitale Betriebsarten") {
                        VStack(alignment: .leading, spacing: 8) {
                            Text("Für FT8, WSPR, RTTY und andere digitale Modi:")
                                .font(.caption)
                                .foregroundColor(.secondary)

                            Button("Für Digimodes konfigurieren") {
                                _ = audioRouter.configureForDigitalModes()
                            }
                            .disabled(!audioRouter.isBlackHoleInstalled)

                            Toggle("BlackHole verwenden", isOn: $settingsController.useBlackHole)
                                .disabled(!audioRouter.isBlackHoleInstalled)
                        }
                        .padding(.vertical, 4)
                    }

                    // Routing Diagram
                    GroupBox("Routing-Schema") {
                        VStack(alignment: .leading, spacing: 4) {
                            Text("FT-991A USB Audio → BlackHole → WSJT-X/fldigi")
                                .font(.caption.monospaced())
                            Text("WSJT-X/fldigi → BlackHole → FT-991A USB Audio")
                                .font(.caption.monospaced())
                        }
                        .foregroundColor(.secondary)
                        .padding(.vertical, 4)
                    }

                    // Error display
                    if let error = audioRouter.lastError {
                        HStack {
                            Image(systemName: "exclamationmark.triangle")
                                .foregroundColor(.orange)
                            Text(error)
                                .font(.caption)
                        }
                    }
                }
                .padding()
            }
        }
    }
}

// MARK: - Preview

#Preview {
    AudioPanel()
        .environmentObject(SettingsController())
        .frame(width: 350, height: 500)
}
