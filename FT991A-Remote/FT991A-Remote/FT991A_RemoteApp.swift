//
//  FT991A_RemoteApp.swift
//  FT991A-Remote
//
//  Yaesu FT-991A Remote Control Application for macOS
//  CAT Protocol via USB Serial (Silicon Labs CP210x)
//

import SwiftUI

@main
struct FT991A_RemoteApp: App {
    @StateObject private var radioViewModel = RadioViewModel()
    @StateObject private var settingsController = SettingsController()
    @StateObject private var logViewModel = LogViewModel()

    @Environment(\.scenePhase) private var scenePhase

    var body: some Scene {
        WindowGroup {
            MainView()
                .environmentObject(radioViewModel)
                .environmentObject(settingsController)
                .environmentObject(logViewModel)
                .frame(minWidth: 800, minHeight: 600)
        }
        .windowStyle(.hiddenTitleBar)
        .commands {
            CommandGroup(replacing: .newItem) { }

            CommandMenu("Radio") {
                Button(radioViewModel.isConnected ? "Trennen" : "Verbinden") {
                    radioViewModel.toggleConnection()
                }
                .keyboardShortcut("k", modifiers: .command)

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

                Button("ATU Tune") {
                    radioViewModel.startATUTune()
                }
                .keyboardShortcut("t", modifiers: [.command, .shift])
                .disabled(!radioViewModel.isConnected)
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
            }
        }

        Settings {
            SettingsView()
                .environmentObject(radioViewModel)
                .environmentObject(settingsController)
        }

        MenuBarExtra("FT-991A", systemImage: radioViewModel.isConnected ? "antenna.radiowaves.left.and.right" : "antenna.radiowaves.left.and.right.slash") {
            MenuBarView()
                .environmentObject(radioViewModel)
                .environmentObject(settingsController)
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
