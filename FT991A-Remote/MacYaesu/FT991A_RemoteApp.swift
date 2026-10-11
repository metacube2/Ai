//
//  FT991A_RemoteApp.swift
//  FT991A-Remote
//
//  MacYaesu for macOS
//  CAT Protocol via USB Serial (Silicon Labs CP210x)
//

import SwiftUI
import AppKit

@main
struct FT991A_RemoteApp: App {
    @StateObject private var radioViewModel: RadioViewModel
    @StateObject private var settingsController = SettingsController()
    @StateObject private var logViewModel = LogViewModel()
    @StateObject private var memoryStore = MemoryStore()
    @StateObject private var vuMeterHub = VUMeterHubService.shared

    @Environment(\.scenePhase) private var scenePhase

    init() {
        let services = RadioServiceContainer.live()
        _radioViewModel = StateObject(wrappedValue: RadioViewModel(services: services))
    }

    var body: some Scene {
        WindowGroup {
            MainView()
                .environmentObject(radioViewModel)
                .environmentObject(settingsController)
                .environmentObject(logViewModel)
                .environmentObject(memoryStore)
                .environmentObject(vuMeterHub)
                .frame(minWidth: 800, minHeight: 600)
                .onChange(of: scenePhase) { newPhase in
                    settingsController.handleScenePhase(newPhase)
                }
                .onAppear {
                    settingsController.handleScenePhase(scenePhase)
                }
                .onReceive(NotificationCenter.default.publisher(for: NSApplication.willTerminateNotification)) { _ in
                    settingsController.flushSettings()
                }
        }
        .windowStyle(.hiddenTitleBar)
        .commands {
            CommandGroup(replacing: .newItem) { }

            CommandMenu("Radio") {
                Button(radioViewModel.isConnected ? "Trennen" : "Verbinden") {
                    radioViewModel.toggleConnection()
                }
                .keyboardShortcut("k", modifiers: .command)
                .disabled(settingsController.isTrialExpired && !settingsController.isActivated && !radioViewModel.isConnected)

                Divider()

                Button("VFO A/B tauschen") {
                    radioViewModel.swapVFO()
                }
                .keyboardShortcut("s", modifiers: [.command, .shift])
                .disabled(!radioViewModel.isConnected)

                Button("A=B") {
                    radioViewModel.equalizeVFO()
                }
                .keyboardShortcut("e", modifiers: [.command, .shift])
                .disabled(!radioViewModel.isConnected)

                Divider()

                if radioViewModel.capabilities.showsATUTune {
                    Button("ATU Tune") {
                        radioViewModel.startATUTune()
                    }
                    .keyboardShortcut("t", modifiers: [.command, .shift])
                    .disabled(!radioViewModel.isConnected)
                }
            }

            CommandMenu("Ansicht") {
                Picker("UI-Stil", selection: $settingsController.uiStyle) {
                    Text("Modern").tag(UIStyle.modern)
                    Text("Frontpanel").tag(UIStyle.skeuomorph)
                }

                Divider()

                Toggle("Debug-Panel anzeigen", isOn: $settingsController.showDebugPanel)
                    .keyboardShortcut("d", modifiers: [.command, .option])

                Toggle("Log-Panel anzeigen", isOn: $settingsController.showLogPanel)
                    .keyboardShortcut("l", modifiers: [.command, .option])

                Toggle("Papagei-Panel anzeigen", isOn: $settingsController.showParrotPanel)
                    .keyboardShortcut("p", modifiers: [.command, .option])
            }

            CommandMenu("Papagei") {
                Toggle("Papagei-Panel anzeigen", isOn: $settingsController.showParrotPanel)

                Divider()

                Button("Papagei arbeitet im Seitenpanel") { }
                    .disabled(true)
            }
        }

        Settings {
            SettingsView()
                .environmentObject(radioViewModel)
                .environmentObject(settingsController)
                .environmentObject(memoryStore)
                .environmentObject(vuMeterHub)
        }

        MenuBarExtra("MacYaesu", systemImage: radioViewModel.isConnected ? "antenna.radiowaves.left.and.right" : "antenna.radiowaves.left.and.right.slash") {
            MenuBarView()
                .environmentObject(radioViewModel)
                .environmentObject(settingsController)
                .environmentObject(memoryStore)
                .environmentObject(vuMeterHub)
        }
    }
}

// MARK: - UI Style Enum

enum UIStyle: String, Codable, CaseIterable {
    case modern = "Modern"
    case skeuomorph = "Frontpanel"
}

// MARK: - Language Enum

enum AppLanguage: String, Codable, CaseIterable {
    case german = "de"
    case english = "en"

    var displayName: String {
        switch self {
        case .german: return "Deutsch"
        case .english: return "English"
        }
    }
}
