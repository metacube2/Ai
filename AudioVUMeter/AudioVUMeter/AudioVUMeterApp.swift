//
//  AudioVUMeterApp.swift
//  AudioVUMeter
//
//  macOS Audio VU Meter with System Monitoring
//  Captures audio from BlackHole virtual audio device
//  Outputs to physical VU meter hardware via Serial/USB
//

import SwiftUI

@main
struct AudioVUMeterApp: App {
    @StateObject private var audioEngine = AudioEngine()
    @StateObject private var systemMonitor = SystemMonitor()
    @StateObject private var serialManager = SerialManager()

    // Timer for updating hardware values
    @State private var updateTimer: Timer?

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(audioEngine)
                .environmentObject(systemMonitor)
                .environmentObject(serialManager)
                .onAppear {
                    startHardwareUpdateTimer()
                }
                .onDisappear {
                    stopHardwareUpdateTimer()
                }
        }
        .windowStyle(.hiddenTitleBar)
        .windowResizability(.contentSize)

        Settings {
            SettingsView()
                .environmentObject(audioEngine)
                .environmentObject(serialManager)
        }
    }

    private func startHardwareUpdateTimer() {
        updateTimer = Timer.scheduledTimer(withTimeInterval: 1.0/30.0, repeats: true) { _ in
            serialManager.updateValues(audioEngine: audioEngine, systemMonitor: systemMonitor)
        }
    }

    private func stopHardwareUpdateTimer() {
        updateTimer?.invalidate()
        updateTimer = nil
    }
}
