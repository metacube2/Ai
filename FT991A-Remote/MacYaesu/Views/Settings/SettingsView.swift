//
//  SettingsView.swift
//  FT991A-Remote
//
//  Application settings view
//

import AppKit
import SwiftUI

// MARK: - Settings View

struct SettingsView: View {
    @EnvironmentObject var radioViewModel: RadioViewModel
    @EnvironmentObject var settingsController: SettingsController
    @EnvironmentObject var vuMeterHub: VUMeterHubService

    var body: some View {
        TabView {
            ActivationSettingsView()
                .tabItem {
                    Label("Aktivierung", systemImage: "key")
                }

            // Connection Settings
            ConnectionSettingsView()
                .tabItem {
                    Label("Verbindung", systemImage: "cable.connector")
                }

            VUMeterSettingsView()
                .tabItem {
                    Label("VU-Meter", systemImage: "gauge.medium")
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

// MARK: - Activation Settings

struct ActivationSettingsView: View {
    @EnvironmentObject var settingsController: SettingsController
    @State private var enteredEmail = ""
    @State private var enteredLicenseKey = ""

    var body: some View {
        Form {
            Section("Status") {
                HStack {
                    Circle()
                        .fill(settingsController.isActivated ? Color.green : (settingsController.isTrialExpired ? Color.red : Color.orange))
                        .frame(width: 10, height: 10)

                    Text(settingsController.isActivated ? "Aktiviert" : (settingsController.isTrialExpired ? "Testzeit abgelaufen" : "Testversion aktiv"))
                }

                if !settingsController.isActivated {
                    Text("Verbleibende Testzeit: \(settingsController.trialSecondsRemaining / 60)m \(settingsController.trialSecondsRemaining % 60)s")
                        .font(.caption)
                        .foregroundColor(.secondary)
                }
            }

            Section("Aktivierung") {
                TextField("E-Mail-Adresse", text: $enteredEmail)
                    .textFieldStyle(.roundedBorder)

                TextField("Lizenzschlüssel eingeben", text: $enteredLicenseKey)
                    .textFieldStyle(.roundedBorder)

                Button("Aktivieren") {
                    let emailToUse = enteredEmail.isEmpty ? settingsController.licenseEmail : enteredEmail
                    let keyToUse = enteredLicenseKey.isEmpty ? settingsController.licenseKey : enteredLicenseKey
                    if settingsController.submitLicense(email: emailToUse, rawKey: keyToUse) {
                        enteredEmail = settingsController.licenseEmail
                        enteredLicenseKey = settingsController.licenseKey
                    }
                }

                if let activationErrorMessage = settingsController.activationErrorMessage {
                    Text(activationErrorMessage)
                        .font(.caption)
                        .foregroundColor(.secondary)
                }

                Text("Für diesen lokalen Build werden Aktivierungsschlüssel offline aus E-Mail-Adresse und Schlüssel geprüft.")
                    .font(.caption)
                    .foregroundColor(.secondary)
            }
        }
        .formStyle(.grouped)
        .padding()
        .onAppear {
            enteredEmail = settingsController.licenseEmail
            enteredLicenseKey = settingsController.licenseKey
        }
    }
}

// MARK: - Connection Settings

struct ConnectionSettingsView: View {
    @EnvironmentObject var settingsController: SettingsController

    var body: some View {
        Form {
            Section("Serielle Verbindung") {
                Picker("Funkgerät", selection: $settingsController.selectedRadioModel) {
                    ForEach(RadioModel.allCases) { model in
                        Text(model.displayName).tag(model)
                    }
                }

                Toggle("Beim Start automatisch verbinden", isOn: $settingsController.autoConnectOnLaunch)
                Toggle("USB-Ports durchsuchen bis CAT antwortet", isOn: $settingsController.searchUSBPortsUntilFound)

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

                Text(settingsController.preferredSerialPort.isEmpty ? "Bevorzugter Port: noch nicht gespeichert" : "Bevorzugter Port: \(settingsController.preferredSerialPort)")
                    .font(.caption)
                    .foregroundColor(.secondary)

                if settingsController.searchUSBPortsUntilFound {
                    Text("Beim Verbinden werden serielle USB-Ports nacheinander getestet, bis ein CAT-Handshake gelingt.")
                        .font(.caption)
                        .foregroundColor(.secondary)
                }
            }

            Section(settingsController.selectedRadioModel.capabilities.connectionProfileTitle) {
                Text("Für eine stabile native macOS-Verbindung sollte das Funkgerät und die Portwahl zu diesem Profil passen:")
                    .font(.caption)
                    .foregroundColor(.secondary)

                VStack(alignment: .leading, spacing: 4) {
                    ForEach(settingsController.selectedRadioModel.capabilities.connectionProfileNotes, id: \.self) { note in
                        Text("• \(note)")
                    }
                }
                .font(.caption.monospaced())

                Text(settingsController.selectedRadioModel.capabilities.connectionProfileSummary)
                    .font(.caption)
                    .foregroundColor(.secondary)
            }

            Section("Einrichtung") {
                HStack {
                    Circle()
                        .fill(settingsController.setupCompleted ? Color.green : Color.orange)
                        .frame(width: 10, height: 10)

                    Text(settingsController.setupCompleted ? "Einrichtung abgeschlossen" : "Ersteinrichtung noch offen")
                }

                Button("Einrichtungsstatus zurücksetzen") {
                    settingsController.setupCompleted = false
                }
            }
        }
        .formStyle(.grouped)
        .padding()
    }
}

// MARK: - VU Meter Settings

struct VUMeterSettingsView: View {
    @EnvironmentObject var vuMeterHub: VUMeterHubService

    var body: some View {
        Form {
            Section("VU1 Dials Hub") {
                HStack {
                    Circle()
                        .fill(vuMeterHub.isConnected ? Color.green : Color.orange)
                        .frame(width: 10, height: 10)

                    Text(vuMeterHub.isConnected ? "Verbunden" : "Nicht verbunden")

                    Spacer()

                    Button(vuMeterHub.isConnected ? "Trennen" : "Verbinden") {
                        if vuMeterHub.isConnected {
                            vuMeterHub.disconnect()
                        } else {
                            vuMeterHub.connect()
                        }
                    }
                }

                Picker("VU-Port", selection: Binding(
                    get: { vuMeterHub.selectedPortPath },
                    set: { vuMeterHub.setSelectedPort($0) }
                )) {
                    if vuMeterHub.availablePorts.isEmpty {
                        Text("Kein Port gefunden").tag("")
                    } else {
                        ForEach(vuMeterHub.availablePorts) { port in
                            Text(port.name).tag(port.path)
                        }
                    }
                }

                Toggle("Beim Start automatisch verbinden", isOn: Binding(
                    get: { vuMeterHub.autoConnectEnabled },
                    set: { vuMeterHub.setAutoConnectEnabled($0) }
                ))

                Button("Ports aktualisieren") {
                    vuMeterHub.refreshPorts()
                }

                if let lastError = vuMeterHub.lastError, !lastError.isEmpty {
                    Text(lastError)
                        .font(.caption)
                        .foregroundColor(.secondary)
                }
            }

            Section("Zuordnung") {
                HStack {
                    Text("Dial 1")
                    Spacer()
                    Text("Signal / S-Meter")
                        .foregroundColor(.secondary)
                }

                HStack {
                    Text("Dial 2")
                    Spacer()
                    Text("SWR")
                        .foregroundColor(.secondary)
                }

                Text("Dial 3 und 4 bleiben aktuell auf 0, damit sich die Zeiger eindeutig auf Empfangssignal und SWR beziehen.")
                    .font(.caption)
                    .foregroundColor(.secondary)
            }

            Section("Live-Werte") {
                meterRow(title: "Signal", value: vuMeterHub.signalDialValue, color: .green)
                meterRow(title: "SWR", value: vuMeterHub.swrDialValue, color: vuMeterHub.swrDialValue > 60 ? .red : .orange)
            }
        }
        .formStyle(.grouped)
        .padding()
        .onAppear {
            vuMeterHub.refreshPorts()
        }
    }

    @ViewBuilder
    private func meterRow(title: String, value: Int, color: Color) -> some View {
        VStack(alignment: .leading, spacing: 6) {
            HStack {
                Text(title)
                Spacer()
                Text("\(value)%")
                    .font(.caption.monospacedDigit())
                    .foregroundColor(.secondary)
            }

            GeometryReader { geometry in
                ZStack(alignment: .leading) {
                    RoundedRectangle(cornerRadius: 4)
                        .fill(Color.secondary.opacity(0.15))

                    RoundedRectangle(cornerRadius: 4)
                        .fill(color)
                        .frame(width: geometry.size.width * CGFloat(value) / 100.0)
                }
            }
            .frame(height: 12)
        }
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

                Toggle("Pfeiltasten links/rechts = Frequenz +/-", isOn: $settingsController.arrowFrequencyEnabled)

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
                    KeyboardShortcutRow(key: "↑", action: "ATU Tune")
                    KeyboardShortcutRow(key: "←/→", action: "Frequenz +/-")
                    KeyboardShortcutRow(key: "+/-", action: "Frequenz +/-")
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
    @State private var supportExportMessage = ""

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

            Section("Support") {
                Text("Erstellt ein lokales Support-Paket mit Einstellungen, App-Log und CAT-Trace für Diagnose oder Kundensupport.")
                    .font(.caption)
                    .foregroundColor(.secondary)

                Button("Support-Bundle auf Desktop exportieren") {
                    exportSupportBundle()
                }

                if !supportExportMessage.isEmpty {
                    Text(supportExportMessage)
                        .font(.caption)
                        .foregroundColor(.secondary)
                }
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

    private func exportSupportBundle() {
        do {
            let exportURL = try Logger.shared.exportSupportBundle()
            supportExportMessage = "Exportiert nach \(exportURL.path)"
            NSWorkspace.shared.activateFileViewerSelecting([exportURL])
        } catch {
            supportExportMessage = "Export fehlgeschlagen: \(error.localizedDescription)"
        }
    }
}

#if DEBUG
struct SettingsView_Previews: PreviewProvider {
    static var previews: some View {
        SettingsView()
            .environmentObject(RadioViewModel())
            .environmentObject(SettingsController())
            .environmentObject(VUMeterHubService.shared)
    }
}
#endif
