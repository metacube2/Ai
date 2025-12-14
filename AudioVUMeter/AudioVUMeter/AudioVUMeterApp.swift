//
//  AudioVUMeterApp.swift
//  AudioVUMeter
//
//  macOS Audio VU Meter with System Monitoring
//  Captures audio from BlackHole virtual audio device
//

import SwiftUI

@main
struct AudioVUMeterApp: App {
    @StateObject private var audioEngine = AudioEngine()
    @StateObject private var systemMonitor = SystemMonitor()

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(audioEngine)
                .environmentObject(systemMonitor)
        }
        .windowStyle(.hiddenTitleBar)
        .windowResizability(.contentSize)

        Settings {
            SettingsView()
                .environmentObject(audioEngine)
        }
    }
}
