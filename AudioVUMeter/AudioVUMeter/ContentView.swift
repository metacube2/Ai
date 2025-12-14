//
//  ContentView.swift
//  AudioVUMeter
//
//  Main view containing all VU meters
//

import SwiftUI

struct ContentView: View {
    @EnvironmentObject var audioEngine: AudioEngine
    @EnvironmentObject var systemMonitor: SystemMonitor

    @State private var showSettings = false

    var body: some View {
        ZStack {
            // Background gradient
            LinearGradient(
                gradient: Gradient(colors: [
                    Color(red: 0.1, green: 0.1, blue: 0.15),
                    Color(red: 0.05, green: 0.05, blue: 0.1)
                ]),
                startPoint: .top,
                endPoint: .bottom
            )
            .ignoresSafeArea()

            VStack(spacing: 20) {
                // Header
                HStack {
                    Text("Audio VU Meter")
                        .font(.system(size: 24, weight: .bold, design: .rounded))
                        .foregroundColor(.white)

                    Spacer()

                    // Settings button
                    Button(action: { showSettings.toggle() }) {
                        Image(systemName: "gear")
                            .font(.system(size: 18))
                            .foregroundColor(.gray)
                    }
                    .buttonStyle(.plain)
                    .popover(isPresented: $showSettings) {
                        QuickSettingsView()
                            .environmentObject(audioEngine)
                    }
                }
                .padding(.horizontal)
                .padding(.top, 10)

                // Audio device info
                HStack {
                    Circle()
                        .fill(audioEngine.isRunning ? Color.green : Color.red)
                        .frame(width: 8, height: 8)

                    Text(audioEngine.selectedDeviceName)
                        .font(.system(size: 12, design: .monospaced))
                        .foregroundColor(.gray)

                    Spacer()

                    Text(audioEngine.isRunning ? "ACTIVE" : "STOPPED")
                        .font(.system(size: 10, weight: .semibold, design: .monospaced))
                        .foregroundColor(audioEngine.isRunning ? .green : .red)
                }
                .padding(.horizontal)

                Divider()
                    .background(Color.gray.opacity(0.3))

                // Audio VU Meters
                VStack(spacing: 15) {
                    Text("AUDIO LEVELS")
                        .font(.system(size: 11, weight: .semibold, design: .monospaced))
                        .foregroundColor(.gray)

                    HStack(spacing: 30) {
                        // Left Channel
                        VUMeterView(
                            level: audioEngine.leftLevel,
                            peakLevel: audioEngine.leftPeak,
                            label: "L",
                            colorScheme: .audio
                        )

                        // Right Channel
                        VUMeterView(
                            level: audioEngine.rightLevel,
                            peakLevel: audioEngine.rightPeak,
                            label: "R",
                            colorScheme: .audio
                        )
                    }

                    // dB Display
                    HStack(spacing: 40) {
                        VStack {
                            Text(String(format: "%.1f dB", audioEngine.leftLevelDB))
                                .font(.system(size: 14, weight: .bold, design: .monospaced))
                                .foregroundColor(dbColor(for: audioEngine.leftLevelDB))
                            Text("LEFT")
                                .font(.system(size: 9, design: .monospaced))
                                .foregroundColor(.gray)
                        }

                        VStack {
                            Text(String(format: "%.1f dB", audioEngine.rightLevelDB))
                                .font(.system(size: 14, weight: .bold, design: .monospaced))
                                .foregroundColor(dbColor(for: audioEngine.rightLevelDB))
                            Text("RIGHT")
                                .font(.system(size: 9, design: .monospaced))
                                .foregroundColor(.gray)
                        }
                    }
                }
                .padding()
                .background(
                    RoundedRectangle(cornerRadius: 12)
                        .fill(Color.black.opacity(0.3))
                )
                .padding(.horizontal)

                Divider()
                    .background(Color.gray.opacity(0.3))

                // System Monitors
                VStack(spacing: 15) {
                    Text("SYSTEM MONITOR")
                        .font(.system(size: 11, weight: .semibold, design: .monospaced))
                        .foregroundColor(.gray)

                    HStack(spacing: 25) {
                        // CPU Meter
                        SystemMeterView(
                            value: systemMonitor.cpuUsage,
                            label: "CPU",
                            unit: "%",
                            colorScheme: .cpu
                        )

                        // RAM Meter
                        SystemMeterView(
                            value: systemMonitor.memoryUsage,
                            label: "RAM",
                            unit: "%",
                            colorScheme: .ram
                        )

                        // Disk I/O Meter
                        SystemMeterView(
                            value: systemMonitor.diskActivity,
                            label: "DISK",
                            unit: "%",
                            colorScheme: .disk
                        )

                        // Network Meter
                        SystemMeterView(
                            value: systemMonitor.networkActivity,
                            label: "NET",
                            unit: "%",
                            colorScheme: .network
                        )
                    }
                }
                .padding()
                .background(
                    RoundedRectangle(cornerRadius: 12)
                        .fill(Color.black.opacity(0.3))
                )
                .padding(.horizontal)

                // Control buttons
                HStack(spacing: 15) {
                    Button(action: {
                        if audioEngine.isRunning {
                            audioEngine.stop()
                        } else {
                            audioEngine.start()
                        }
                    }) {
                        HStack {
                            Image(systemName: audioEngine.isRunning ? "stop.fill" : "play.fill")
                            Text(audioEngine.isRunning ? "Stop" : "Start")
                        }
                        .frame(width: 80)
                    }
                    .buttonStyle(ControlButtonStyle(color: audioEngine.isRunning ? .red : .green))

                    Button(action: {
                        audioEngine.resetPeaks()
                    }) {
                        HStack {
                            Image(systemName: "arrow.counterclockwise")
                            Text("Reset")
                        }
                        .frame(width: 80)
                    }
                    .buttonStyle(ControlButtonStyle(color: .orange))
                }
                .padding(.bottom, 15)
            }
        }
        .frame(width: 400, height: 580)
        .onAppear {
            audioEngine.start()
            systemMonitor.startMonitoring()
        }
        .onDisappear {
            audioEngine.stop()
            systemMonitor.stopMonitoring()
        }
    }

    private func dbColor(for db: Double) -> Color {
        if db > -3 { return .red }
        if db > -10 { return .orange }
        if db > -20 { return .yellow }
        return .green
    }
}

// MARK: - Quick Settings Popover
struct QuickSettingsView: View {
    @EnvironmentObject var audioEngine: AudioEngine

    var body: some View {
        VStack(alignment: .leading, spacing: 15) {
            Text("Audio Device")
                .font(.headline)

            Picker("Device", selection: $audioEngine.selectedDeviceID) {
                ForEach(audioEngine.availableDevices, id: \.id) { device in
                    Text(device.name).tag(device.id)
                }
            }
            .labelsHidden()
            .frame(width: 250)
            .onChange(of: audioEngine.selectedDeviceID) { _ in
                audioEngine.switchDevice()
            }

            Divider()

            Text("Reference Level")
                .font(.headline)

            HStack {
                Text("-60 dB")
                    .font(.caption)
                Slider(value: $audioEngine.referenceLevel, in: -60...0)
                Text("0 dB")
                    .font(.caption)
            }

            Text("Peak Hold Time: \(Int(audioEngine.peakHoldTime))s")
                .font(.caption)

            Slider(value: $audioEngine.peakHoldTime, in: 0.5...5.0)
        }
        .padding()
        .frame(width: 300)
    }
}

// MARK: - Control Button Style
struct ControlButtonStyle: ButtonStyle {
    let color: Color

    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .font(.system(size: 12, weight: .semibold))
            .foregroundColor(.white)
            .padding(.horizontal, 15)
            .padding(.vertical, 8)
            .background(
                RoundedRectangle(cornerRadius: 8)
                    .fill(color.opacity(configuration.isPressed ? 0.6 : 0.8))
            )
    }
}

#Preview {
    ContentView()
        .environmentObject(AudioEngine())
        .environmentObject(SystemMonitor())
}
