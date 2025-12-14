//
//  SettingsView.swift
//  AudioVUMeter
//
//  Settings window for configuring audio device, hardware output, and preferences
//

import SwiftUI

struct SettingsView: View {
    @EnvironmentObject var audioEngine: AudioEngine
    @EnvironmentObject var serialManager: SerialManager
    @EnvironmentObject var vuServer: VUServer

    @AppStorage("showPeakIndicator") private var showPeakIndicator = true
    @AppStorage("meterStyle") private var meterStyle = "classic"
    @AppStorage("updateRate") private var updateRate = 30.0

    var body: some View {
        TabView {
            // Audio Settings
            Form {
                Section("Audio Device") {
                    Picker("Input Device", selection: $audioEngine.selectedDeviceID) {
                        ForEach(audioEngine.availableDevices, id: \.id) { device in
                            HStack {
                                Text(device.name)
                                Spacer()
                                Text("\(device.inputChannels) ch")
                                    .foregroundColor(.secondary)
                                    .font(.caption)
                            }
                            .tag(device.id)
                        }
                    }
                    .onChange(of: audioEngine.selectedDeviceID) { _ in
                        audioEngine.switchDevice()
                    }

                    Button("Refresh Devices") {
                        audioEngine.refreshDeviceList()
                    }
                }

                Section("Levels") {
                    HStack {
                        Text("Reference Level")
                        Spacer()
                        Text("\(Int(audioEngine.referenceLevel)) dB")
                            .foregroundColor(.secondary)
                    }
                    Slider(value: $audioEngine.referenceLevel, in: -60...0, step: 1)

                    HStack {
                        Text("Peak Hold Time")
                        Spacer()
                        Text("\(String(format: "%.1f", audioEngine.peakHoldTime)) s")
                            .foregroundColor(.secondary)
                    }
                    Slider(value: $audioEngine.peakHoldTime, in: 0.5...10, step: 0.5)
                }
            }
            .tabItem {
                Label("Audio", systemImage: "waveform")
            }

            // Hardware Settings
            HardwareSettingsView()
                .environmentObject(serialManager)
                .tabItem {
                    Label("Hardware", systemImage: "cable.connector")
                }

            // Server Settings
            ServerSettingsView()
                .environmentObject(vuServer)
                .tabItem {
                    Label("Server", systemImage: "server.rack")
                }

            // Display Settings
            Form {
                Section("Meter Display") {
                    Toggle("Show Peak Indicator", isOn: $showPeakIndicator)

                    Picker("Meter Style", selection: $meterStyle) {
                        Text("Classic").tag("classic")
                        Text("Modern").tag("modern")
                        Text("Minimal").tag("minimal")
                    }
                }

                Section("Performance") {
                    HStack {
                        Text("Update Rate")
                        Spacer()
                        Text("\(Int(updateRate)) fps")
                            .foregroundColor(.secondary)
                    }
                    Slider(value: $updateRate, in: 10...60, step: 5)
                }
            }
            .tabItem {
                Label("Display", systemImage: "display")
            }

            // About
            VStack(spacing: 20) {
                Image(systemName: "waveform.circle.fill")
                    .font(.system(size: 64))
                    .foregroundColor(.accentColor)

                Text("Audio VU Meter")
                    .font(.title)
                    .fontWeight(.bold)

                Text("Version 1.2.0")
                    .foregroundColor(.secondary)

                Divider()
                    .frame(width: 200)

                VStack(spacing: 8) {
                    Text("A macOS audio level meter with system monitoring")
                    Text("and physical VU meter hardware support.")
                }
                .multilineTextAlignment(.center)
                .foregroundColor(.secondary)
                .frame(width: 300)

                Spacer()

                VStack(spacing: 4) {
                    Text("Supports BlackHole virtual audio device,")
                    Text("USB/Serial VU meter hardware,")
                    Text("and TCP server for external apps")
                }
                .font(.caption)
                .foregroundColor(.secondary)
            }
            .padding()
            .tabItem {
                Label("About", systemImage: "info.circle")
            }
        }
        .frame(width: 500, height: 500)
    }
}

#Preview {
    SettingsView()
        .environmentObject(AudioEngine())
        .environmentObject(SerialManager())
        .environmentObject(VUServer())
}
