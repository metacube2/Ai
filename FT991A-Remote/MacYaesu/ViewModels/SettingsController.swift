//
//  SettingsController.swift
//  FT991A-Remote
//
//  Application settings controller
//

import Foundation
import SwiftUI

@MainActor
final class SettingsController: ObservableObject {
    @Published var uiStyle: UIStyle = .modern { didSet { saveSettings() } }
    @Published var language: AppLanguage = .german { didSet { saveSettings() } }
    @Published var compactMode: Bool = true { didSet { saveSettings() } }
    @Published var showDebugPanel: Bool = false { didSet { saveSettings() } }
    @Published var showLogPanel: Bool = false { didSet { saveSettings() } }
    @Published var showParrotPanel: Bool = false { didSet { saveSettings() } }
    @Published var radioWorkspaceWidth: Double = 980 { didSet { saveSettings() } }

    @Published var selectedRadioModel: RadioModel = .ft991a { didSet { saveSettings() } }
    @Published var autoReconnect: Bool = true { didSet { saveSettings() } }
    @Published var reconnectInterval: TimeInterval = 5.0 { didSet { saveSettings() } }
    @Published var defaultBaudRate: Int = 38400 { didSet { saveSettings() } }
    @Published var autoConnectOnLaunch: Bool = false { didSet { saveSettings() } }
    @Published var searchUSBPortsUntilFound: Bool = false { didSet { saveSettings() } }
    @Published var preferredSerialPort: String = "" { didSet { saveSettings() } }
    @Published var setupCompleted: Bool = false { didSet { saveSettings() } }

    @Published var frequencyStep: FrequencyStep = .khz1 { didSet { saveSettings() } }

    @Published var logDirectory: String = "~/Documents/FT991A-Logs/" { didSet { saveSettings() } }
    @Published var autoSaveLog: Bool = true { didSet { saveSettings() } }

    @Published var audioInputDevice: String = "" { didSet { saveSettings() } }
    @Published var audioOutputDevice: String = "" { didSet { saveSettings() } }
    @Published var parrotInputDeviceUID: String = "" { didSet { saveSettings() } }
    @Published var parrotOutputDeviceUID: String = "" { didSet { saveSettings() } }
    @Published var useBlackHole: Bool = false { didSet { saveSettings() } }

    @Published var pttShortcutEnabled: Bool = true { didSet { saveSettings() } }
    @Published var arrowFrequencyEnabled: Bool = true { didSet { saveSettings() } }
    @Published var tunerShortcutEnabled: Bool = true { didSet { saveSettings() } }

    @Published var licenseEmail: String = "" { didSet { saveSettings() } }
    @Published var licenseKey: String = "" { didSet { saveSettings() } }
    @Published var isActivated: Bool = false { didSet { saveSettings() } }
    @Published var activationErrorMessage: String?
    @Published var trialSecondsRemaining: Int = SettingsController.trialDurationSeconds
    @Published var isTrialExpired: Bool = false

    private var settings: AppSettings
    private var saveDebounce: Timer?
    private var activeSince: Date?

    static let availableBaudRates = [4800, 9600, 19200, 38400, 57600, 115200]
    private static let trialDurationSeconds = 30 * 60

    init() {
        settings = AppSettings.load()
        loadFromSettings()
        recalculateTrialState()
    }

    var expandedLogDirectory: String {
        (logDirectory as NSString).expandingTildeInPath
    }

    func handleScenePhase(_ phase: ScenePhase) {
        switch phase {
        case .active:
            activeSince = Date()
        case .inactive, .background:
            consumeTrialIfNeeded()
            flushSettings()
        @unknown default:
            flushSettings()
        }
    }

    func flushSettings() {
        saveDebounce?.invalidate()
        saveDebounce = nil
        performSave()
    }

    func rememberConnectionPreferences(portPath: String, baudRate: Int) {
        preferredSerialPort = portPath
        defaultBaudRate = baudRate
    }

    func submitLicense(email: String, rawKey: String) -> Bool {
        let normalizedEmail = email.trimmingCharacters(in: .whitespacesAndNewlines)
        let normalizedKey = rawKey.trimmingCharacters(in: .whitespacesAndNewlines)

        guard !normalizedEmail.isEmpty, !normalizedKey.isEmpty else {
            activationErrorMessage = "E-Mail und Lizenzschlüssel dürfen nicht leer sein."
            return false
        }

        licenseEmail = normalizedEmail
        licenseKey = normalizedKey
        isActivated = true
        activationErrorMessage = nil
        recalculateTrialState()
        return true
    }

    private func loadFromSettings() {
        uiStyle = settings.uiStyle
        language = settings.language
        compactMode = settings.compactMode
        showDebugPanel = settings.showDebugPanel
        showLogPanel = settings.showLogPanel
        showParrotPanel = settings.showParrotPanel
        radioWorkspaceWidth = settings.radioWorkspaceWidth

        selectedRadioModel = settings.radioModel
        autoReconnect = settings.autoReconnect
        reconnectInterval = settings.reconnectInterval
        defaultBaudRate = settings.baudRate
        autoConnectOnLaunch = settings.autoConnectOnLaunch
        searchUSBPortsUntilFound = settings.searchUSBPortsUntilFound
        preferredSerialPort = settings.serialPort
        setupCompleted = settings.setupCompleted

        frequencyStep = settings.frequencyStep
        logDirectory = settings.logDirectory
        autoSaveLog = settings.autoSaveLog

        audioInputDevice = settings.audioInputDevice
        audioOutputDevice = settings.audioOutputDevice
        parrotInputDeviceUID = settings.parrotInputDeviceUID
        parrotOutputDeviceUID = settings.parrotOutputDeviceUID
        useBlackHole = settings.useBlackHole

        pttShortcutEnabled = settings.pttShortcutEnabled
        arrowFrequencyEnabled = settings.arrowFrequencyEnabled
        tunerShortcutEnabled = settings.tunerShortcutEnabled

        licenseEmail = settings.licenseEmail
        licenseKey = settings.licenseKey
        isActivated = settings.isActivated
    }

    private func saveSettings() {
        saveDebounce?.invalidate()
        saveDebounce = Timer.scheduledTimer(withTimeInterval: 0.35, repeats: false) { [weak self] _ in
            self?.performSave()
        }
    }

    private func performSave() {
        settings.uiStyle = uiStyle
        settings.language = language
        settings.compactMode = compactMode
        settings.showDebugPanel = showDebugPanel
        settings.showLogPanel = showLogPanel
        settings.showParrotPanel = showParrotPanel
        settings.radioWorkspaceWidth = radioWorkspaceWidth

        settings.radioModel = selectedRadioModel
        settings.autoReconnect = autoReconnect
        settings.reconnectInterval = reconnectInterval
        settings.baudRate = defaultBaudRate
        settings.autoConnectOnLaunch = autoConnectOnLaunch
        settings.searchUSBPortsUntilFound = searchUSBPortsUntilFound
        settings.serialPort = preferredSerialPort
        settings.setupCompleted = setupCompleted

        settings.frequencyStep = frequencyStep
        settings.logDirectory = logDirectory
        settings.autoSaveLog = autoSaveLog

        settings.audioInputDevice = audioInputDevice
        settings.audioOutputDevice = audioOutputDevice
        settings.parrotInputDeviceUID = parrotInputDeviceUID
        settings.parrotOutputDeviceUID = parrotOutputDeviceUID
        settings.useBlackHole = useBlackHole

        settings.pttShortcutEnabled = pttShortcutEnabled
        settings.arrowFrequencyEnabled = arrowFrequencyEnabled
        settings.tunerShortcutEnabled = tunerShortcutEnabled

        settings.licenseEmail = licenseEmail
        settings.licenseKey = licenseKey
        settings.isActivated = isActivated

        settings.ensureLogDirectoryExists()
        settings.save()
    }

    private func consumeTrialIfNeeded() {
        guard !isActivated, let activeSince else {
            self.activeSince = nil
            return
        }

        let elapsed = max(0, Int(Date().timeIntervalSince(activeSince)))
        settings.trialConsumedSeconds += elapsed
        self.activeSince = nil
        recalculateTrialState()
    }

    private func recalculateTrialState() {
        if isActivated {
            trialSecondsRemaining = Self.trialDurationSeconds
            isTrialExpired = false
            return
        }

        let consumed = max(0, settings.trialConsumedSeconds)
        trialSecondsRemaining = max(0, Self.trialDurationSeconds - consumed)
        isTrialExpired = trialSecondsRemaining == 0
    }
}
