//
//  AudioVUMeterApp.swift
//  AudioVUMeter
//
//  macOS Audio VU Meter with System Monitoring
//  Captures audio from BlackHole virtual audio device
//  Outputs to physical VU meter hardware via Serial/USB
//  Includes VU Server for external app connections
//

import SwiftUI

@main
struct AudioVUMeterApp: App {
    @StateObject private var audioEngine = AudioEngine()
    @StateObject private var systemMonitor = SystemMonitor()
    @StateObject private var serialManager = SerialManager()
    @StateObject private var vuServer = VUServer()

    // Timer for updating hardware values
    @State private var updateTimer: Timer?

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(audioEngine)
                .environmentObject(systemMonitor)
                .environmentObject(serialManager)
                .environmentObject(vuServer)
                .onAppear {
                    setupServer()
                    startHardwareUpdateTimer()
                }
                .onDisappear {
                    stopHardwareUpdateTimer()
                    vuServer.stop()
                }
        }
        .windowStyle(.hiddenTitleBar)
        .windowResizability(.contentSize)

        Settings {
            SettingsView()
                .environmentObject(audioEngine)
                .environmentObject(serialManager)
                .environmentObject(vuServer)
        }
    }

    private func setupServer() {
        // Link server to serial manager for broadcasting
        vuServer.serialManager = serialManager

        // Auto-start server if it was enabled
        if vuServer.options.enabled {
            vuServer.start()
        }
    }

    private func startHardwareUpdateTimer() {
        updateTimer = Timer.scheduledTimer(withTimeInterval: 1.0/30.0, repeats: true) { _ in
            // Check if external control is active from VU Server
            if vuServer.externalControlActive, let externalValues = vuServer.receivedDialValues {
                // Use values from external app
                serialManager.dialValues = externalValues
            } else {
                // Use local audio/system values
                serialManager.updateValues(audioEngine: audioEngine, systemMonitor: systemMonitor)
            }
        }
    }

    private func stopHardwareUpdateTimer() {
        updateTimer?.invalidate()
        updateTimer = nil
    }
}
