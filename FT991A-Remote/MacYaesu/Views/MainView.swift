//
//  MainView.swift
//  FT991A-Remote
//
//  Main application window container
//

import AppKit
import SwiftUI
import UniformTypeIdentifiers

// MARK: - Main View

struct MainView: View {
    @Environment(\.openSettings) private var openSettings
    @EnvironmentObject var radioViewModel: RadioViewModel
    @EnvironmentObject var settingsController: SettingsController
    @EnvironmentObject var logViewModel: LogViewModel
    @EnvironmentObject var memoryStore: MemoryStore

    @StateObject private var parrotStore = ParrotStore()
    @StateObject private var parrotAudioService = ParrotAudioService()
    @State private var isDebugPanelDetached = false
    @State private var isLogPanelDetached = false
    @State private var didApplyStartupPreferences = false
    @State private var didAttemptAutoConnect = false
    @State private var activationKeyInput = ""
    @State private var activationEmailInput = ""
    @State private var flagsMonitor: Any?
    @State private var shiftPTTIsDown = false
    @State private var shiftStartedTransmit = false

    var body: some View {
        ZStack {
            NavigationSplitView {
                // Sidebar
                SidebarView()
                    .frame(minWidth: 200)
            } detail: {
                GeometryReader { geometry in
                    let sidePanelsVisible = hasVisibleSidePanels
                    let minimumSideWidth = sidePanelsVisible ? 340.0 : 80.0
                    let maximumRadioWidth = max(620.0, geometry.size.width - minimumSideWidth)
                    let radioWidth = min(max(settingsController.radioWorkspaceWidth, 620.0), maximumRadioWidth)

                    HStack(spacing: 0) {
                        // Radio control area
                        VStack(spacing: 0) {
                            ConnectionBar()
                                .padding(.horizontal)
                                .padding(.top, 8)

                            if shouldShowSetupAssistant {
                                SetupAssistantCard()
                                    .padding(.horizontal)
                                    .padding(.top, 8)
                            }

                            MemoryManagerCard()
                                .padding(.horizontal)
                                .padding(.top, 8)

                            if radioViewModel.capabilities.showsToneControl {
                                ToneControlCard()
                                    .padding(.horizontal)
                                    .padding(.top, 8)
                            }

                            if radioViewModel.capabilities.showsRepeaterSetup {
                                RepeaterQuickSetupCard()
                                    .padding(.horizontal)
                                    .padding(.top, 8)
                            }

                            Divider()
                                .padding(.top, 8)

                            if settingsController.uiStyle == .modern {
                                ModernRadioView()
                            } else {
                                SkeuomorphRadioView()
                            }
                        }
                        .frame(width: radioWidth, alignment: .topLeading)

                        WorkspaceResizeHandle(
                            width: $settingsController.radioWorkspaceWidth,
                            minimumWidth: 620,
                            maximumWidth: maximumRadioWidth
                        )

                        if sidePanelsVisible {
                            Divider()

                            ScrollView {
                                VStack(spacing: 12) {
                                    if settingsController.showParrotPanel {
                                        ParrotPanel()
                                            .environmentObject(parrotStore)
                                            .environmentObject(parrotAudioService)
                                    }

                                    if settingsController.showLogPanel && !isLogPanelDetached {
                                        LogPanel()
                                            .frame(minHeight: 280)
                                    }

                                    if settingsController.showDebugPanel && !isDebugPanelDetached {
                                        DebugPanel()
                                            .frame(minHeight: 280)
                                    }
                                }
                                .padding(12)
                            }
                            .frame(minWidth: 340, maxWidth: 420)
                        } else {
                            Spacer(minLength: 0)
                        }
                    }
                }
            }
            
            if settingsController.isTrialExpired && !settingsController.isActivated {
                trialExpiredOverlay
            }

            GlobalKeyboardShortcutOverlay()
                .environmentObject(radioViewModel)
                .environmentObject(settingsController)
        }
        .toolbar {
            ToolbarItemGroup(placement: .primaryAction) {
                // UI Style toggle
                Picker("UI", selection: $settingsController.uiStyle) {
                    Image(systemName: "rectangle.3.group")
                        .tag(UIStyle.modern)
                    Image(systemName: "dial.medium")
                        .tag(UIStyle.skeuomorph)
                }
                .pickerStyle(.segmented)
                .help("UI-Stil wechseln")

                Divider()

                // Panel toggles
                Toggle(isOn: $settingsController.showLogPanel) {
                    Image(systemName: "list.bullet.rectangle")
                }
                .help("Log-Panel anzeigen")

                Toggle(isOn: $settingsController.showParrotPanel) {
                    Image(systemName: "mic.badge.waveform")
                }
                .help("Papagei-Panel anzeigen")

                Toggle(isOn: $settingsController.showDebugPanel) {
                    Image(systemName: "terminal")
                }
                .help("Debug-Panel anzeigen")
            }
        }
        .navigationTitle("MacYaesu")
        .onAppear {
            setupKeyboardShortcuts()
            applyStartupPreferencesIfNeeded()
            radioViewModel.setRadioModel(settingsController.selectedRadioModel)
            radioViewModel.searchUSBPortsUntilFound = settingsController.searchUSBPortsUntilFound
            radioViewModel.isUsageLocked = settingsController.isTrialExpired && !settingsController.isActivated
            parrotAudioService.refreshDevices()
        }
        .onDisappear {
            teardownKeyboardShortcuts()
        }
        .onChange(of: radioViewModel.availablePorts) { _ in
            applyStartupPreferencesIfNeeded()
            attemptAutoConnectIfNeeded()
        }
        .onChange(of: radioViewModel.selectedPort) { newValue in
            settingsController.rememberConnectionPreferences(portPath: newValue, baudRate: radioViewModel.baudRate)
        }
        .onChange(of: radioViewModel.baudRate) { newValue in
            settingsController.rememberConnectionPreferences(portPath: radioViewModel.selectedPort, baudRate: newValue)
        }
        .onChange(of: settingsController.frequencyStep) { newValue in
            radioViewModel.frequencyStep = newValue
        }
        .onChange(of: settingsController.selectedRadioModel) { newValue in
            radioViewModel.setRadioModel(newValue)
        }
        .onChange(of: settingsController.searchUSBPortsUntilFound) { newValue in
            radioViewModel.searchUSBPortsUntilFound = newValue
        }
        .onChange(of: radioViewModel.hasCATResponse) { hasResponse in
            if hasResponse {
                settingsController.setupCompleted = true
            }
        }
        .onChange(of: settingsController.isTrialExpired) { expired in
            radioViewModel.isUsageLocked = expired && !settingsController.isActivated
            if expired {
                radioViewModel.disconnect()
            }
        }
        .onChange(of: settingsController.isActivated) { isActivated in
            radioViewModel.isUsageLocked = settingsController.isTrialExpired && !isActivated
        }
        .onChange(of: radioViewModel.isConnected) { isConnected in
            if !isConnected {
                shiftPTTIsDown = false
                shiftStartedTransmit = false
            }
        }
    }

    private func setupKeyboardShortcuts() {
        guard flagsMonitor == nil else { return }

        flagsMonitor = NSEvent.addLocalMonitorForEvents(matching: .flagsChanged) { event in
            handleFlagsChanged(event)
            return event
        }
    }

    private func teardownKeyboardShortcuts() {
        if let flagsMonitor {
            NSEvent.removeMonitor(flagsMonitor)
            self.flagsMonitor = nil
        }
        shiftPTTIsDown = false
        shiftStartedTransmit = false
    }

    private func handleFlagsChanged(_ event: NSEvent) {
        guard settingsController.pttShortcutEnabled, radioViewModel.isConnected else {
            shiftPTTIsDown = false
            shiftStartedTransmit = false
            return
        }

        let shiftIsDown = event.modifierFlags.contains(.shift)
        guard shiftIsDown != shiftPTTIsDown else { return }
        shiftPTTIsDown = shiftIsDown

        if shiftIsDown {
            if !radioViewModel.isTransmitting {
                radioViewModel.startTransmit()
                shiftStartedTransmit = true
            }
        } else if shiftStartedTransmit {
            radioViewModel.stopTransmit()
            shiftStartedTransmit = false
        }
    }

    private var shouldShowSetupAssistant: Bool {
        !settingsController.setupCompleted || (!radioViewModel.isConnected && !radioViewModel.availablePorts.isEmpty)
    }

    private var hasVisibleSidePanels: Bool {
        settingsController.showParrotPanel ||
        (settingsController.showLogPanel && !isLogPanelDetached) ||
        (settingsController.showDebugPanel && !isDebugPanelDetached)
    }

    private func applyStartupPreferencesIfNeeded() {
        guard !didApplyStartupPreferences else { return }

        radioViewModel.baudRate = settingsController.defaultBaudRate
        radioViewModel.frequencyStep = settingsController.frequencyStep

        if settingsController.preferredSerialPort.isEmpty {
            didApplyStartupPreferences = true
            return
        }

        guard radioViewModel.availablePorts.contains(where: { $0.path == settingsController.preferredSerialPort }) else {
            return
        }

        radioViewModel.selectPort(settingsController.preferredSerialPort)
        didApplyStartupPreferences = true
    }

    private func attemptAutoConnectIfNeeded() {
        guard settingsController.autoConnectOnLaunch,
              !didAttemptAutoConnect,
              !radioViewModel.isConnected,
              !settingsController.isTrialExpired,
              !radioViewModel.availablePorts.isEmpty else { return }

        didAttemptAutoConnect = true

        if !settingsController.preferredSerialPort.isEmpty,
           radioViewModel.availablePorts.contains(where: { $0.path == settingsController.preferredSerialPort }) {
            radioViewModel.selectPort(settingsController.preferredSerialPort)
        }

        radioViewModel.baudRate = settingsController.defaultBaudRate
        radioViewModel.connect()
    }

    private var trialExpiredOverlay: some View {
        ZStack {
            Color.black.opacity(0.42)
                .ignoresSafeArea()

            VStack(alignment: .leading, spacing: 14) {
                Text("Testzeit beendet")
                    .font(.title2.bold())

                Text("MacYaesu wurde nach 15 Minuten ohne Aktivierung gestoppt. Gib einen Lizenzschlüssel ein, um die App weiter zu nutzen.")
                    .foregroundColor(.secondary)

                TextField("E-Mail-Adresse", text: $activationEmailInput)
                    .textFieldStyle(.roundedBorder)

                TextField("Lizenzschlüssel", text: $activationKeyInput)
                    .textFieldStyle(.roundedBorder)

                if let activationErrorMessage = settingsController.activationErrorMessage {
                    Text(activationErrorMessage)
                        .font(.caption)
                        .foregroundColor(.secondary)
                }

                HStack {
                    Button("Aktivieren") {
                        let success = settingsController.submitLicense(email: activationEmailInput, rawKey: activationKeyInput)
                        if success {
                            activationEmailInput = ""
                            activationKeyInput = ""
                        }
                    }
                    .keyboardShortcut(.defaultAction)

                    Button("Daten aus Einstellungen übernehmen") {
                        activationEmailInput = settingsController.licenseEmail
                        activationKeyInput = settingsController.licenseKey
                    }
                }
            }
            .padding(24)
            .frame(width: 440)
            .background(.regularMaterial)
            .cornerRadius(16)
            .shadow(radius: 24)
        }
    }
}

// MARK: - Sidebar View

struct SidebarView: View {
    @EnvironmentObject var radioViewModel: RadioViewModel

    var body: some View {
        List {
            Section("Verbindung") {
                Label {
                    VStack(alignment: .leading) {
                        Text(radioViewModel.isConnected ? "Verbunden" : "Getrennt")
                            .font(.headline)
                        Text(radioViewModel.catStatusText)
                            .font(.caption)
                            .foregroundColor(radioViewModel.catAlive ? .green : .secondary)
                        if let portSearchStatusText = radioViewModel.portSearchStatusText {
                            Text(portSearchStatusText)
                                .font(.caption2)
                                .foregroundColor(.secondary)
                        } else if radioViewModel.isConnected {
                            Text(radioViewModel.selectedPortDisplayName)
                                .font(.caption)
                                .foregroundColor(.secondary)
                        }
                    }
                } icon: {
                    Image(systemName: radioViewModel.isConnected ? "antenna.radiowaves.left.and.right" : "antenna.radiowaves.left.and.right.slash")
                        .foregroundColor(radioViewModel.isConnected ? .green : .red)
                }
            }

            Section("Frequenz") {
                Label {
                    Text(radioViewModel.frequencyDisplay + " Hz")
                        .font(.system(.body, design: .monospaced))
                } icon: {
                    Image(systemName: "waveform")
                }

                if let band = radioViewModel.currentBand {
                    Label(band.rawValue, systemImage: "chart.bar")
                }

                Label(radioViewModel.mode.rawValue, systemImage: "waveform.path")
                Label(radioViewModel.toneSummaryText, systemImage: "waveform.badge.plus")
            }

            Section("Bänder") {
                ForEach(Band.allCases, id: \.self) { band in
                    Button {
                        radioViewModel.selectBand(band)
                    } label: {
                        Label(band.rawValue, systemImage: "antenna.radiowaves.left.and.right")
                    }
                    .disabled(!radioViewModel.isConnected)
                }
            }
        }
        .listStyle(.sidebar)
    }
}

// MARK: - Connection Bar

private struct GlobalKeyboardShortcutOverlay: View {
    @EnvironmentObject var radioViewModel: RadioViewModel
    @EnvironmentObject var settingsController: SettingsController

    var body: some View {
        Group {
            Button("") {
                radioViewModel.startATUTune()
            }
            .keyboardShortcut(.upArrow, modifiers: [])
            .disabled(!settingsController.tunerShortcutEnabled || !radioViewModel.isConnected || !radioViewModel.capabilities.showsATUTune)

            Button("") {
                radioViewModel.decrementFrequency()
            }
            .keyboardShortcut(.leftArrow, modifiers: [])
            .disabled(!settingsController.arrowFrequencyEnabled || !radioViewModel.isConnected)

            Button("") {
                radioViewModel.incrementFrequency()
            }
            .keyboardShortcut(.rightArrow, modifiers: [])
            .disabled(!settingsController.arrowFrequencyEnabled || !radioViewModel.isConnected)

            Button("") {
                radioViewModel.decrementFrequency()
            }
            .keyboardShortcut("-", modifiers: [])
            .disabled(!settingsController.arrowFrequencyEnabled || !radioViewModel.isConnected)

            Button("") {
                radioViewModel.incrementFrequency()
            }
            .keyboardShortcut("+", modifiers: [])
            .disabled(!settingsController.arrowFrequencyEnabled || !radioViewModel.isConnected)
        }
        .labelsHidden()
        .opacity(0.001)
        .allowsHitTesting(false)
        .accessibilityHidden(true)
    }
}

struct ConnectionBar: View {
    @EnvironmentObject var radioViewModel: RadioViewModel

    var body: some View {
        HStack(spacing: 12) {
            // Port selection
            Picker("Port", selection: $radioViewModel.selectedPort) {
                Text("Port wählen...").tag("")
                ForEach(radioViewModel.availablePorts) { port in
                    Text(port.name).tag(port.path)
                }
            }
            .frame(width: 200)

            // Refresh button
            Button {
                radioViewModel.refreshPorts()
            } label: {
                Image(systemName: "arrow.clockwise")
            }
            .help("Ports aktualisieren")

            // Baud rate
            Picker("Baud", selection: $radioViewModel.baudRate) {
                ForEach(SerialConfig.availableBaudRates, id: \.self) { rate in
                    Text("\(rate)").tag(rate)
                }
            }
            .frame(width: 100)

            if radioViewModel.searchUSBPortsUntilFound {
                PortSearchBadge(isSearching: radioViewModel.isSearchingPorts)
            }

            Spacer()

            // Connection status
            HStack(spacing: 6) {
                Circle()
                    .fill(radioViewModel.isConnected ? Color.green : Color.red)
                    .frame(width: 10, height: 10)

                Text(radioViewModel.connectionState.displayString)
                    .foregroundColor(.secondary)
            }

            HStack(spacing: 6) {
                Circle()
                    .fill(radioViewModel.catAlive ? Color.green : (radioViewModel.isConnected ? Color.orange : Color.secondary))
                    .frame(width: 8, height: 8)

                VStack(alignment: .leading, spacing: 1) {
                    Text(radioViewModel.catStatusText)
                        .font(.caption)
                        .foregroundColor(.secondary)
                        .lineLimit(1)

                    if let portSearchStatusText = radioViewModel.portSearchStatusText {
                        Text(portSearchStatusText)
                            .font(.caption2)
                            .foregroundColor(.secondary)
                            .lineLimit(1)
                    }
                }
                .frame(width: 240, alignment: .leading)
            }

            // Connect button
            Button("Ping CAT") {
                radioViewModel.pingCAT()
            }
            .disabled(!radioViewModel.isConnected)
            .help("Sendet ID und VFO-A Abfrage an das Funkgerät")

            Toggle("Auto-Ping", isOn: $radioViewModel.autoPingEnabled)
                .toggleStyle(.button)
                .controlSize(.small)
                .disabled(!radioViewModel.isConnected)

            Button {
                radioViewModel.toggleConnection()
            } label: {
                Text(radioViewModel.isConnected ? "Trennen" : "Verbinden")
            }
            .keyboardShortcut("k", modifiers: .command)
        }
        .padding(.vertical, 8)
    }
}

// MARK: - Setup Assistant Card

struct SetupAssistantCard: View {
    @Environment(\.openSettings) private var openSettings
    @EnvironmentObject var radioViewModel: RadioViewModel
    @EnvironmentObject var settingsController: SettingsController

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                VStack(alignment: .leading, spacing: 4) {
                    Text("Setup & Support")
                        .font(.headline)
                    Text("Schneller Zugriff auf Ersteinrichtung, Verbindungsstatus und Support.")
                        .font(.caption)
                        .foregroundColor(.secondary)
                }

                Spacer()

                Label(settingsController.setupCompleted ? "Bereit" : "Einrichtung", systemImage: settingsController.setupCompleted ? "checkmark.seal.fill" : "wrench.and.screwdriver.fill")
                    .foregroundColor(settingsController.setupCompleted ? .green : .orange)
            }

            if radioViewModel.searchUSBPortsUntilFound {
                PortSearchBadge(isSearching: radioViewModel.isSearchingPorts)
            }

            HStack(alignment: .top, spacing: 24) {
                VStack(alignment: .leading, spacing: 6) {
                    Text("Empfohlen")
                        .font(.subheadline.weight(.semibold))
                    Text("Enhanced Port")
                    Text("38400 baud")
                    Text("CAT RTS an")
                    Text("DTR aus")
                }
                .font(.caption)

                VStack(alignment: .leading, spacing: 6) {
                    Text("Aktuell")
                        .font(.subheadline.weight(.semibold))
                    Text(radioViewModel.selectedPort.isEmpty ? "Kein Port gewählt" : radioViewModel.selectedPortDisplayName)
                    Text("\(radioViewModel.baudRate) baud")
                    Text(radioViewModel.catStatusText)
                    if let portSearchStatusText = radioViewModel.portSearchStatusText {
                        Text(portSearchStatusText)
                    }
                    Text(settingsController.autoConnectOnLaunch ? "Auto-Connect aktiv" : "Auto-Connect aus")
                }
                .font(.caption)
                .foregroundColor(.secondary)
            }

            HStack {
                Button("Ports aktualisieren") {
                    radioViewModel.refreshPorts()
                }

                Button(radioViewModel.isConnected ? "Trennen" : "Jetzt verbinden") {
                    radioViewModel.toggleConnection()
                }
                .disabled((radioViewModel.selectedPort.isEmpty && !radioViewModel.searchUSBPortsUntilFound) || (!radioViewModel.isConnected && radioViewModel.availablePorts.isEmpty))

                Button("Einstellungen öffnen") {
                    openSettings()
                }

                Spacer()
            }
        }
        .padding(14)
        .background(
            RoundedRectangle(cornerRadius: 14, style: .continuous)
                .fill(Color.accentColor.opacity(0.08))
        )
    }
}

struct PortSearchBadge: View {
    let isSearching: Bool

    var body: some View {
        Label(isSearching ? "Portsuche laeuft" : "Portsuche aktiv", systemImage: isSearching ? "dot.radiowaves.left.and.right" : "dot.scope")
            .font(.caption.weight(.medium))
            .padding(.horizontal, 10)
            .padding(.vertical, 5)
            .background(
                Capsule(style: .continuous)
                    .fill(isSearching ? Color.orange.opacity(0.16) : Color.accentColor.opacity(0.12))
            )
            .foregroundColor(isSearching ? .orange : .accentColor)
    }
}

struct WorkspaceResizeHandle: View {
    @Binding var width: Double
    let minimumWidth: Double
    let maximumWidth: Double
    @State private var dragStartWidth: Double?

    var body: some View {
        Rectangle()
            .fill(Color.clear)
            .frame(width: 12)
            .overlay(
                Capsule(style: .continuous)
                    .fill(Color.secondary.opacity(0.35))
                    .frame(width: 4)
                    .padding(.vertical, 16)
            )
            .contentShape(Rectangle())
            .gesture(
                DragGesture(minimumDistance: 0)
                    .onChanged { value in
                        let baseWidth = dragStartWidth ?? width
                        if dragStartWidth == nil {
                            dragStartWidth = width
                        }
                        width = min(max(baseWidth + value.translation.width, minimumWidth), maximumWidth)
                    }
                    .onEnded { _ in
                        dragStartWidth = nil
                    }
            )
            .help("Radio-Breite mit der Maus anpassen")
    }
}

struct ParrotPanel: View {
    @EnvironmentObject var radioViewModel: RadioViewModel
    @EnvironmentObject var settingsController: SettingsController
    @EnvironmentObject var parrotStore: ParrotStore
    @EnvironmentObject var parrotAudioService: ParrotAudioService

    @State private var selectedMessageID: UUID?
    @State private var draftName = "CQ CQ HB3YYU"
    @State private var statusMessage = "Bereit fuer Aufnahme oder Vorschau."
    @State private var sendInProgress = false

    private var selectedMessage: ParrotMessage? {
        parrotStore.messages.first(where: { $0.id == selectedMessageID }) ?? parrotStore.messages.first
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            HStack {
                VStack(alignment: .leading, spacing: 2) {
                    Text("Papagei")
                        .font(.headline)
                    Text("Sprachspeicher, Vorschau und Senden ueber TX-Audio.")
                        .font(.caption)
                        .foregroundColor(.secondary)
                }

                Spacer()

                Button {
                    parrotAudioService.refreshDevices()
                } label: {
                    Image(systemName: "arrow.clockwise")
                }
                .help("Audio-Geraete aktualisieren")
            }

            GroupBox("Aufnahme") {
                VStack(alignment: .leading, spacing: 10) {
                    TextField("Name der Aufnahme", text: $draftName)
                        .textFieldStyle(.roundedBorder)

                    Picker("Eingang", selection: $settingsController.parrotInputDeviceUID) {
                        Text("Systemstandard").tag("")
                        ForEach(parrotAudioService.inputDevices) { device in
                            Text(device.displayName).tag(device.uid)
                        }
                    }

                    HStack {
                        if parrotAudioService.isRecording {
                            Label("Aufnahme \(formattedDuration(parrotAudioService.elapsedTime)) / 01:00", systemImage: "record.circle.fill")
                                .foregroundColor(.red)
                        } else {
                            Label("Maximal 60 Sekunden", systemImage: "mic")
                                .foregroundColor(.secondary)
                        }

                        Spacer()

                        Button(parrotAudioService.isRecording ? "Stopp" : "Aufnehmen") {
                            if parrotAudioService.isRecording {
                                parrotAudioService.stopRecording()
                            } else {
                                startRecording()
                            }
                        }
                        .tint(parrotAudioService.isRecording ? .red : .accentColor)
                    }
                }
                .padding(.vertical, 4)
            }

            GroupBox("Senden & Vorschau") {
                VStack(alignment: .leading, spacing: 10) {
                    Picker("TX-Ausgang", selection: $settingsController.parrotOutputDeviceUID) {
                        Text("Systemstandard").tag("")
                        ForEach(parrotAudioService.outputDevices) { device in
                            Text(device.displayName).tag(device.uid)
                        }
                    }

                    if let selectedMessage {
                        VStack(alignment: .leading, spacing: 4) {
                            Text(selectedMessage.name)
                                .font(.subheadline.weight(.semibold))
                            Text("Laenge \(formattedDuration(selectedMessage.duration))")
                                .font(.caption)
                                .foregroundColor(.secondary)
                        }
                    } else {
                        Text("Noch keine Papagei-Aufnahme gespeichert.")
                            .font(.caption)
                            .foregroundColor(.secondary)
                    }

                    if parrotAudioService.isPlaying {
                        Label("\(sendInProgress ? "Sende" : "Vorschau") \(formattedDuration(parrotAudioService.elapsedTime))", systemImage: sendInProgress ? "antenna.radiowaves.left.and.right" : "speaker.wave.2.fill")
                            .foregroundColor(sendInProgress ? .orange : .accentColor)
                    }

                    HStack {
                        Button(parrotAudioService.isPlaying && !sendInProgress ? "Vorschau stoppen" : "Vorschau") {
                            togglePreview()
                        }
                        .disabled(selectedMessage == nil || (parrotAudioService.isRecording || sendInProgress))

                        Button(sendInProgress ? "Senden stoppen" : "Ueber Funk senden") {
                            toggleSend()
                        }
                        .disabled(selectedMessage == nil || !radioViewModel.isConnected || parrotAudioService.isRecording)
                    }

                    Text("Waehrend Aufnahme oder Wiedergabe wird das gewaehlte Audio-Geraet kurz als macOS-Standard genutzt.")
                        .font(.caption2)
                        .foregroundColor(.secondary)
                }
                .padding(.vertical, 4)
            }

            GroupBox("Gespeicherte Sprachspeicher") {
                VStack(alignment: .leading, spacing: 8) {
                    if parrotStore.messages.isEmpty {
                        Text("Keine gespeicherten Aufnahmen vorhanden.")
                            .font(.caption)
                            .foregroundColor(.secondary)
                            .padding(.vertical, 8)
                    } else {
                        ForEach(parrotStore.messages) { message in
                            Button {
                                selectedMessageID = message.id
                                draftName = message.name
                            } label: {
                                HStack {
                                    VStack(alignment: .leading, spacing: 2) {
                                        Text(message.name)
                                            .font(.subheadline.weight(.medium))
                                            .foregroundColor(.primary)
                                        Text(formattedDuration(message.duration))
                                            .font(.caption2)
                                            .foregroundColor(.secondary)
                                    }

                                    Spacer()

                                    if selectedMessageID == message.id || (selectedMessageID == nil && parrotStore.messages.first?.id == message.id) {
                                        Image(systemName: "checkmark.circle.fill")
                                            .foregroundColor(.accentColor)
                                    }
                                }
                                .padding(8)
                                .background(
                                    RoundedRectangle(cornerRadius: 10, style: .continuous)
                                        .fill((selectedMessageID == message.id || (selectedMessageID == nil && parrotStore.messages.first?.id == message.id)) ? Color.accentColor.opacity(0.12) : Color.secondary.opacity(0.08))
                                )
                            }
                            .buttonStyle(.plain)
                        }

                        HStack {
                            Button("Name uebernehmen") {
                                if let selectedMessage {
                                    parrotStore.renameMessage(id: selectedMessage.id, name: draftName)
                                    statusMessage = "Name aktualisiert."
                                }
                            }
                            .disabled(selectedMessage == nil)

                            Button("Loeschen", role: .destructive) {
                                deleteSelectedMessage()
                            }
                            .disabled(selectedMessage == nil)
                        }
                    }
                }
                .padding(.vertical, 4)
            }

            Label(statusMessage, systemImage: sendInProgress ? "dot.radiowaves.left.and.right" : "info.circle")
                .font(.caption)
                .foregroundColor(.secondary)

            if let lastError = parrotAudioService.lastError {
                Label(lastError, systemImage: "exclamationmark.triangle.fill")
                    .font(.caption)
                    .foregroundColor(.orange)
            }
        }
        .padding(14)
        .background(
            RoundedRectangle(cornerRadius: 14, style: .continuous)
                .fill(Color.accentColor.opacity(0.06))
        )
        .onAppear {
            parrotAudioService.refreshDevices()
            if selectedMessageID == nil {
                selectedMessageID = parrotStore.messages.first?.id
            }
        }
    }

    private func startRecording() {
        let label = draftName
        let didStart = parrotAudioService.startRecording(inputDeviceUID: settingsController.parrotInputDeviceUID) { url, duration in
            Task { @MainActor in
                do {
                    let message = try parrotStore.saveRecording(from: url, name: label, duration: duration)
                    selectedMessageID = message.id
                    draftName = message.name
                    statusMessage = "Aufnahme gespeichert: \(message.name)"
                } catch {
                    statusMessage = "Aufnahme konnte nicht gespeichert werden."
                }
                try? FileManager.default.removeItem(at: url)
            }
        }

        if didStart {
            statusMessage = "Aufnahme laeuft..."
        }
    }

    private func togglePreview() {
        if parrotAudioService.isPlaying && !sendInProgress {
            parrotAudioService.stopPlayback()
            statusMessage = "Vorschau gestoppt."
            return
        }

        guard let selectedMessage else { return }
        let url = parrotStore.fileURL(for: selectedMessage)
        let started = parrotAudioService.play(url: url, outputDeviceUID: settingsController.parrotOutputDeviceUID) { success in
            Task { @MainActor in
                statusMessage = success ? "Vorschau beendet." : "Vorschau abgebrochen."
            }
        }

        if started {
            statusMessage = "Vorschau gestartet."
        }
    }

    private func toggleSend() {
        if sendInProgress {
            parrotAudioService.stopPlayback()
            radioViewModel.stopTransmit()
            sendInProgress = false
            statusMessage = "Senden gestoppt."
            return
        }

        guard let selectedMessage else { return }
        let url = parrotStore.fileURL(for: selectedMessage)

        sendInProgress = true
        statusMessage = "PTT aktiv, sende Sprachspeicher..."
        radioViewModel.startTransmit(dataMode: radioViewModel.mode.isDigital)

        Task { @MainActor in
            try? await Task.sleep(nanoseconds: 250_000_000)
            guard sendInProgress else { return }

            let started = parrotAudioService.play(url: url, outputDeviceUID: settingsController.parrotOutputDeviceUID) { success in
                Task { @MainActor in
                    try? await Task.sleep(nanoseconds: 150_000_000)
                    radioViewModel.stopTransmit()
                    sendInProgress = false
                    statusMessage = success ? "Senden abgeschlossen." : "Senden abgebrochen."
                }
            }

            if !started {
                radioViewModel.stopTransmit()
                sendInProgress = false
                statusMessage = "Senden konnte nicht gestartet werden."
            }
        }
    }

    private func deleteSelectedMessage() {
        guard let selectedMessage else { return }
        parrotStore.deleteMessage(id: selectedMessage.id)
        selectedMessageID = parrotStore.messages.first?.id
        if let next = parrotStore.messages.first {
            draftName = next.name
        }
        statusMessage = "Sprachspeicher geloescht."
    }

    private func formattedDuration(_ value: TimeInterval) -> String {
        let totalSeconds = max(0, Int(value.rounded()))
        return String(format: "%02d:%02d", totalSeconds / 60, totalSeconds % 60)
    }
}

// MARK: - Memory Manager Card

struct MemoryManagerCard: View {
    @EnvironmentObject var radioViewModel: RadioViewModel
    @EnvironmentObject var memoryStore: MemoryStore

    @State private var memoryName = ""
    @State private var targetSlot = 1
    @State private var selectedMemoryID: UUID?
    @State private var statusMessage = ""

    private var selectedEntry: MemoryEntry? {
        memoryStore.entries.first(where: { $0.id == selectedMemoryID })
    }

    var body: some View {
        GroupBox("Scan & Speicher") {
            VStack(alignment: .leading, spacing: 12) {
                HStack {
                    Label(radioViewModel.isScanning ? "Scan läuft \(radioViewModel.scanLabel.isEmpty ? radioViewModel.scanDirection.rawValue : radioViewModel.scanLabel)" : "Scan gestoppt", systemImage: radioViewModel.isScanning ? "dot.radiowaves.up.forward" : "pause.circle")
                        .foregroundColor(radioViewModel.isScanning ? .orange : .secondary)

                    Spacer()

                    Text("\(memoryStore.usedSlots)/\(MemoryStore.maximumEntries) Speicher belegt")
                        .font(.caption)
                        .foregroundColor(.secondary)
                }

                HStack(spacing: 8) {
                    Button("Scan Down") {
                        radioViewModel.toggleScan(direction: .down)
                    }
                    .disabled(!radioViewModel.isConnected)

                    Button("Memory Down") {
                        radioViewModel.startMemoryScan(direction: .down, entries: memoryStore.entries)
                    }
                    .disabled(!radioViewModel.isConnected || memoryStore.entries.isEmpty)

                    Button("Stop") {
                        radioViewModel.stopScan()
                    }
                    .disabled(!radioViewModel.isScanning)

                    Button("Scan Up") {
                        radioViewModel.toggleScan(direction: .up)
                    }
                    .disabled(!radioViewModel.isConnected)

                    Button("Memory Up") {
                        radioViewModel.startMemoryScan(direction: .up, entries: memoryStore.entries)
                    }
                    .disabled(!radioViewModel.isConnected || memoryStore.entries.isEmpty)

                    Divider()
                        .frame(height: 20)

                    Stepper("Slot \(targetSlot)", value: $targetSlot, in: 1...MemoryStore.maximumEntries)
                        .frame(width: 120)

                    TextField("Speichername", text: $memoryName)
                        .textFieldStyle(.roundedBorder)

                    Button("Aktuell speichern") {
                        saveCurrentMemory()
                    }
                    .disabled(!radioViewModel.isConnected)
                }

                HStack(spacing: 8) {
                    Button("Laden") {
                        guard let selectedEntry else { return }
                        radioViewModel.recallMemory(selectedEntry)
                        statusMessage = "Speicher \(selectedEntry.slot) geladen"
                    }
                    .disabled(selectedEntry == nil || !radioViewModel.isConnected)

                    Button("Auswahl löschen") {
                        guard let selectedEntry else { return }
                        radioViewModel.stopScan()
                        memoryStore.deleteEntry(slot: selectedEntry.slot)
                        statusMessage = "Speicher \(selectedEntry.slot) gelöscht"
                        selectedMemoryID = nil
                    }
                    .disabled(selectedEntry == nil)

                    Button("Importieren") {
                        importMemories()
                    }

                    Button("Exportieren") {
                        exportMemories()
                    }
                    .disabled(memoryStore.entries.isEmpty)

                    Spacer()
                }

                List(memoryStore.entries, selection: $selectedMemoryID) { entry in
                    HStack {
                        Text(String(format: "%02d", entry.slot))
                            .font(.system(.caption, design: .monospaced))
                            .frame(width: 32, alignment: .leading)

                        Text(entry.name)
                            .frame(width: 180, alignment: .leading)

                        Text(entry.frequencyDisplay)
                            .font(.system(.caption, design: .monospaced))
                            .frame(width: 120, alignment: .leading)

                        Text(entry.mode.rawValue)
                            .frame(width: 70, alignment: .leading)

                        Text(entry.offsetDisplay)
                            .frame(maxWidth: .infinity, alignment: .leading)
                            .foregroundColor(.secondary)

                        Text(entry.toneDisplay)
                            .frame(width: 170, alignment: .leading)
                            .foregroundColor(.secondary)
                    }
                    .tag(entry.id)
                }
                .frame(minHeight: 150, maxHeight: 220)

                if !statusMessage.isEmpty {
                    Text(statusMessage)
                        .font(.caption)
                        .foregroundColor(.secondary)
                }
            }
            .padding(.top, 4)
        }
        .onChange(of: selectedEntry?.slot) { newSlot in
            guard let newSlot else { return }
            targetSlot = newSlot
            if let selectedEntry {
                memoryName = selectedEntry.name
            }
        }
    }

    private func saveCurrentMemory() {
        let entry = radioViewModel.captureCurrentMemory(slot: targetSlot, name: memoryName)
        memoryStore.saveEntry(entry)
        selectedMemoryID = memoryStore.entries.first(where: { $0.slot == targetSlot })?.id
        memoryName = entry.name
        statusMessage = "Speicher \(targetSlot) gespeichert"
    }

    private func importMemories() {
        let panel = NSOpenPanel()
        panel.canChooseFiles = true
        panel.canChooseDirectories = false
        panel.allowsMultipleSelection = false
        panel.allowedContentTypes = [.json]

        if panel.runModal() == .OK, let url = panel.url {
            do {
                radioViewModel.stopScan()
                try memoryStore.importEntries(from: url)
                statusMessage = "Speicherliste importiert"
                selectedMemoryID = nil
            } catch {
                statusMessage = "Import fehlgeschlagen: \(error.localizedDescription)"
            }
        }
    }

    private func exportMemories() {
        let panel = NSSavePanel()
        panel.nameFieldStringValue = "ft991a-memories.json"
        panel.allowedContentTypes = [.json]

        if panel.runModal() == .OK, let url = panel.url {
            do {
                try memoryStore.exportEntries(to: url)
                statusMessage = "Speicherliste exportiert"
            } catch {
                statusMessage = "Export fehlgeschlagen: \(error.localizedDescription)"
            }
        }
    }
}

// MARK: - Tone Control Card

struct ToneControlCard: View {
    @EnvironmentObject var radioViewModel: RadioViewModel

    private var ctcssBinding: Binding<Int> {
        Binding(
            get: { radioViewModel.ctcssToneIndex },
            set: { radioViewModel.setCTCSSToneIndex($0) }
        )
    }

    private var dcsBinding: Binding<Int> {
        Binding(
            get: { radioViewModel.dcsCodeIndex },
            set: { radioViewModel.setDCSCodeIndex($0) }
        )
    }

    var body: some View {
        GroupBox("Tone / CTCSS / DCS") {
            VStack(alignment: .leading, spacing: 12) {
                HStack {
                    Picker("Modus", selection: Binding(
                        get: { radioViewModel.toneMode },
                        set: { radioViewModel.setToneMode($0) }
                    )) {
                        ForEach(radioViewModel.availableToneModes, id: \.self) { mode in
                            Text(mode.rawValue).tag(mode)
                        }
                    }
                    .frame(maxWidth: 260)
                    .disabled(!radioViewModel.isConnected)

                    Spacer()

                    Text(currentToneSummary)
                        .font(.caption)
                        .foregroundColor(.secondary)
                }

                if radioViewModel.toneMode.usesDCS {
                    Picker("DCS", selection: dcsBinding) {
                        ForEach(Array(ToneCatalog.dcsCodes.enumerated()), id: \.offset) { item in
                            Text(String(format: "%03d", item.element)).tag(item.offset)
                        }
                    }
                    .frame(maxWidth: 220)
                    .disabled(!radioViewModel.isConnected)
                } else if radioViewModel.toneMode.usesTone {
                    Picker("CTCSS", selection: ctcssBinding) {
                        ForEach(Array(ToneCatalog.ctcssFrequencies.enumerated()), id: \.offset) { item in
                            Text(String(format: "%.1f Hz", item.element)).tag(item.offset)
                        }
                    }
                    .frame(maxWidth: 220)
                    .disabled(!radioViewModel.isConnected)
                }

                Text("Verfügbare Tonmodi richten sich nach dem gewählten Funkgerät. Die Auswahl wird in den 100 Memories mitgespeichert.")
                    .font(.caption)
                    .foregroundColor(.secondary)
            }
            .padding(.top, 4)
        }
    }

    private var currentToneSummary: String {
        switch radioViewModel.toneMode {
        case .off:
            return "Tone aus"
        case .ctcssEncode, .ctcssEncodeDecode:
            let index = min(max(0, radioViewModel.ctcssToneIndex), ToneCatalog.ctcssFrequencies.count - 1)
            let tone = ToneCatalog.ctcssFrequencies[index]
            return "\(radioViewModel.toneMode.rawValue) \(String(format: "%.1f", tone)) Hz"
        case .dcsEncode, .dcsEncodeDecode:
            let index = min(max(0, radioViewModel.dcsCodeIndex), ToneCatalog.dcsCodes.count - 1)
            let code = ToneCatalog.dcsCodes[index]
            return "\(radioViewModel.toneMode.rawValue) \(String(format: "%03d", code))"
        case .pagerFrequency, .reverseTone:
            return radioViewModel.toneMode.rawValue
        }
    }
}

// MARK: - Repeater Quick Setup Card

struct RepeaterQuickSetupCard: View {
    @EnvironmentObject var radioViewModel: RadioViewModel

    @State private var selectedOffsetHz = 600_000
    @State private var selectedShift: RepeaterShiftDirection = .minus
    @State private var quickToneMode: ToneMode = .ctcssEncode
    @State private var quickCTCSSIndex = 16   // 114.8 Hz
    @State private var quickDCSIndex = 0

    private let commonOffsets = [600_000, 1_600_000, 5_000_000, 7_600_000]

    var body: some View {
        GroupBox("VHF/UHF Repeater") {
            VStack(alignment: .leading, spacing: 12) {
                HStack(spacing: 12) {
                    Picker("Shift", selection: $selectedShift) {
                        ForEach(RepeaterShiftDirection.allCases, id: \.self) { shift in
                            Text(shift.displayName).tag(shift)
                        }
                    }
                    .frame(width: 150)
                    .disabled(!radioViewModel.isConnected)

                    Picker("Offset", selection: $selectedOffsetHz) {
                        ForEach(commonOffsets, id: \.self) { offset in
                            Text(offsetLabel(offset)).tag(offset)
                        }
                    }
                    .frame(width: 150)
                    .disabled(!radioViewModel.isConnected || selectedShift == .off)

                    Picker("Tone", selection: $quickToneMode) {
                        ForEach(radioViewModel.capabilities.supportedToneModes, id: \.self) { mode in
                            Text(mode.rawValue).tag(mode)
                        }
                    }
                    .frame(width: 180)
                    .disabled(!radioViewModel.isConnected)

                    Spacer()

                    Button("Übernehmen") {
                        applyRepeaterPreset()
                    }
                    .disabled(!radioViewModel.isConnected)
                }

                if quickToneMode.usesDCS {
                    Picker("DCS Code", selection: $quickDCSIndex) {
                        ForEach(Array(ToneCatalog.dcsCodes.enumerated()), id: \.offset) { item in
                            Text(String(format: "%03d", item.element)).tag(item.offset)
                        }
                    }
                    .frame(maxWidth: 220)
                    .disabled(!radioViewModel.isConnected)
                } else if quickToneMode.usesTone {
                    Picker("CTCSS", selection: $quickCTCSSIndex) {
                        ForEach(Array(ToneCatalog.ctcssFrequencies.enumerated()), id: \.offset) { item in
                            Text(String(format: "%.1f Hz", item.element)).tag(item.offset)
                        }
                    }
                    .frame(maxWidth: 220)
                    .disabled(!radioViewModel.isConnected)
                }

                HStack(spacing: 8) {
                    Button("2m FM Standard") {
                        selectedShift = .minus
                        selectedOffsetHz = 600_000
                        quickToneMode = .ctcssEncode
                        quickCTCSSIndex = 16
                        applyRepeaterPreset()
                    }
                    .disabled(!radioViewModel.isConnected)

                    Button("70cm FM Standard") {
                        selectedShift = .plus
                        selectedOffsetHz = 7_600_000
                        quickToneMode = .ctcssEncode
                        quickCTCSSIndex = 16
                        applyRepeaterPreset()
                    }
                    .disabled(!radioViewModel.isConnected)

                    Button("Simplex") {
                        selectedShift = .off
                        quickToneMode = .off
                        applyRepeaterPreset()
                    }
                    .disabled(!radioViewModel.isConnected)
                }

                Text("Schnellkonfiguration für Relaisbetrieb: setzt Split/Versatz und Tone gemeinsam. Die angebotenen Tonmodi folgen dem ausgewählten Funkgerät.")
                    .font(.caption)
                    .foregroundColor(.secondary)
            }
            .padding(.top, 4)
        }
        .onAppear {
            normalizeToneSelection()
        }
        .onChange(of: radioViewModel.radioModel) { _ in
            normalizeToneSelection()
        }
    }

    private func offsetLabel(_ offset: Int) -> String {
        String(format: "%.3f MHz", Double(offset) / 1_000_000.0)
    }

    private func applyRepeaterPreset() {
        radioViewModel.configureRepeater(
            shift: selectedShift,
            offsetHz: selectedShift == .off ? 0 : selectedOffsetHz,
            toneMode: quickToneMode,
            ctcssIndex: quickCTCSSIndex,
            dcsIndex: quickDCSIndex
        )
    }

    private func normalizeToneSelection() {
        if !radioViewModel.capabilities.supportedToneModes.contains(quickToneMode),
           let fallback = radioViewModel.capabilities.supportedToneModes.first {
            quickToneMode = fallback
        }
    }
}

#if DEBUG
struct MainView_Previews: PreviewProvider {
    static var previews: some View {
        MainView()
            .environmentObject(RadioViewModel())
            .environmentObject(SettingsController())
            .environmentObject(LogViewModel())
            .environmentObject(MemoryStore())
            .frame(width: 1200, height: 800)
    }
}
#endif
