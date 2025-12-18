//
//  SettingsView.swift
//  FT991A-Remote
//
//  Application settings view
//

import SwiftUI

// MARK: - Settings View

struct SettingsView: View {
    @EnvironmentObject var radioViewModel: RadioViewModel
    @EnvironmentObject var settingsController: SettingsController

    var body: some View {
        TabView {
            // Connection Settings
            ConnectionSettingsView()
                .tabItem {
                    Label("Verbindung", systemImage: "cable.connector")
                }

            // UI Settings
            UISettingsView()
                .tabItem {
                    Label("Oberfläche", systemImage: "paintbrush")
                }

            // Audio Settings
            AudioSettingsView()
                .tabItem {
                    Label("Audio", systemImage: "speaker.wave.2")
                }

            // Keyboard Settings
            KeyboardSettingsView()
                .tabItem {
                    Label("Tastatur", systemImage: "keyboard")
                }

            // Logging Settings
            LoggingSettingsView()
                .tabItem {
                    Label("Logging", systemImage: "doc.text")
                }
        }
        .frame(width: 500, height: 400)
    }
}

// MARK: - Connection Settings

struct ConnectionSettingsView: View {
    @EnvironmentObject var settingsController: SettingsController

    var body: some View {
        Form {
            Section("Serielle Verbindung") {
                Picker("Standard-Baudrate", selection: $settingsController.defaultBaudRate) {
                    ForEach(SettingsController.availableBaudRates, id: \.self) { rate in
                        Text("\(rate) baud").tag(rate)
                    }
                }

                Toggle("Auto-Reconnect aktivieren", isOn: $settingsController.autoReconnect)

                if settingsController.autoReconnect {
                    HStack {
                        Text("Intervall:")
                        Slider(value: $settingsController.reconnectInterval, in: 1...30, step: 1)
                        Text("\(Int(settingsController.reconnectInterval))s")
                            .frame(width: 30)
                    }
                }
            }

            Section("FT-991A Einstellungen") {
                Text("Stelle sicher, dass im Radio-Menü folgende Einstellungen aktiv sind:")
                    .font(.caption)
                    .foregroundColor(.secondary)

                VStack(alignment: .leading, spacing: 4) {
                    Text("• CAT RATE: 38400 bps")
                    Text("• CAT TOT: 100 ms")
                    Text("• CAT RTS: OFF")
                }
                .font(.caption.monospaced())
            }
        }
        .formStyle(.grouped)
        .padding()
    }
}

// MARK: - UI Settings

struct UISettingsView: View {
    @EnvironmentObject var settingsController: SettingsController

    var body: some View {
        Form {
            Section("Erscheinungsbild") {
                Picker("UI-Stil", selection: $settingsController.uiStyle) {
                    Text("Modern").tag(UIStyle.modern)
                    Text("Frontpanel (Skeuomorph)").tag(UIStyle.skeuomorph)
                }

                Toggle("Kompakter Modus", isOn: $settingsController.compactMode)
            }

            Section("Sprache") {
                Picker("Sprache", selection: $settingsController.language) {
                    ForEach(AppLanguage.allCases, id: \.self) { lang in
                        Text(lang.displayName).tag(lang)
                    }
                }

                Text("Änderungen werden nach Neustart wirksam.")
                    .font(.caption)
                    .foregroundColor(.secondary)
            }

            Section("Frequenz") {
                Picker("Standard-Schrittweite", selection: $settingsController.frequencyStep) {
                    ForEach(FrequencyStep.allCases, id: \.self) { step in
                        Text(step.displayName).tag(step)
                    }
                }
            }
        }
        .formStyle(.grouped)
        .padding()
    }
}

// MARK: - Audio Settings

struct AudioSettingsView: View {
    @EnvironmentObject var settingsController: SettingsController
    @StateObject private var audioRouter = AudioRouter()

    var body: some View {
        Form {
            Section("Audio-Geräte") {
                Picker("Eingabegerät", selection: $settingsController.audioInputDevice) {
                    Text("Standard").tag("")
                    ForEach(audioRouter.inputDevices) { device in
                        Text(device.name).tag(device.uid)
                    }
                }

                Picker("Ausgabegerät", selection: $settingsController.audioOutputDevice) {
                    Text("Standard").tag("")
                    ForEach(audioRouter.outputDevices) { device in
                        Text(device.name).tag(device.uid)
                    }
                }
            }

            Section("BlackHole Integration") {
                HStack {
                    Circle()
                        .fill(audioRouter.isBlackHoleInstalled ? Color.green : Color.red)
                        .frame(width: 10, height: 10)
                    Text(audioRouter.isBlackHoleInstalled ? "BlackHole installiert" : "BlackHole nicht gefunden")
                }

                Toggle("BlackHole für Digimodes verwenden", isOn: $settingsController.useBlackHole)
                    .disabled(!audioRouter.isBlackHoleInstalled)

                if !audioRouter.isBlackHoleInstalled {
                    Link("BlackHole herunterladen", destination: URL(string: "https://existential.audio/blackhole/")!)
                }
            }
        }
        .formStyle(.grouped)
        .padding()
        .onAppear {
            audioRouter.refreshDevices()
        }
    }
}

// MARK: - Keyboard Settings

struct KeyboardSettingsView: View {
    @EnvironmentObject var settingsController: SettingsController

    var body: some View {
        Form {
            Section("Tastaturkürzel") {
                Toggle("Shift = PTT (Push-to-Talk)", isOn: $settingsController.pttShortcutEnabled)

                Toggle("Pfeiltasten = Frequenz ändern", isOn: $settingsController.arrowFrequencyEnabled)

                Toggle("Pfeil hoch = ATU Tune", isOn: $settingsController.tunerShortcutEnabled)
            }

            Section("Übersicht") {
                VStack(alignment: .leading, spacing: 8) {
                    KeyboardShortcutRow(key: "⌘K", action: "Verbinden/Trennen")
                    KeyboardShortcutRow(key: "⇧⌘S", action: "VFO A/B tauschen")
                    KeyboardShortcutRow(key: "⇧⌘E", action: "A=B")
                    KeyboardShortcutRow(key: "⇧⌘T", action: "ATU Tune")
                    KeyboardShortcutRow(key: "⌥⌘D", action: "Debug-Panel")
                    KeyboardShortcutRow(key: "⌥⌘L", action: "Log-Panel")
                    Divider()
                    KeyboardShortcutRow(key: "←/→", action: "Frequenz +/-")
                    KeyboardShortcutRow(key: "↑", action: "ATU Tune")
                    KeyboardShortcutRow(key: "Shift", action: "PTT (halten)")
                }
            }
        }
        .formStyle(.grouped)
        .padding()
    }
}

// MARK: - Keyboard Shortcut Row

struct KeyboardShortcutRow: View {
    let key: String
    let action: String

    var body: some View {
        HStack {
            Text(key)
                .font(.system(.caption, design: .monospaced))
                .padding(.horizontal, 6)
                .padding(.vertical, 2)
                .background(Color.secondary.opacity(0.2))
                .cornerRadius(4)
                .frame(width: 70, alignment: .leading)

            Text(action)
                .font(.caption)
        }
    }
}

// MARK: - Logging Settings

struct LoggingSettingsView: View {
    @EnvironmentObject var settingsController: SettingsController

    var body: some View {
        Form {
            Section("Log-Speicherort") {
                HStack {
                    TextField("Verzeichnis", text: $settingsController.logDirectory)
                        .textFieldStyle(.roundedBorder)

                    Button("Wählen...") {
                        selectDirectory()
                    }
                }

                Text("Aktueller Pfad: \(settingsController.expandedLogDirectory)")
                    .font(.caption)
                    .foregroundColor(.secondary)
            }

            Section("Automatisches Speichern") {
                Toggle("Log automatisch speichern", isOn: $settingsController.autoSaveLog)

                Text("Speichert QSOs automatisch nach jeder Eingabe.")
                    .font(.caption)
                    .foregroundColor(.secondary)
            }

            Section("CSV-Format") {
                Text("Felder: Call, Datum, Zeit, Frequenz, Mode, RST TX/RX, Name, QTH, Locator, Power, Notizen")
                    .font(.caption)
                    .foregroundColor(.secondary)
            }
        }
        .formStyle(.grouped)
        .padding()
    }

    private func selectDirectory() {
        let panel = NSOpenPanel()
        panel.canChooseFiles = false
        panel.canChooseDirectories = true
        panel.allowsMultipleSelection = false
        panel.canCreateDirectories = true
        panel.prompt = "Auswählen"

        if panel.runModal() == .OK, let url = panel.url {
            settingsController.logDirectory = url.path
        }
    }
}

// MARK: - Preview

#Preview {
    SettingsView()
        .environmentObject(RadioViewModel())
        .environmentObject(SettingsController())
}
