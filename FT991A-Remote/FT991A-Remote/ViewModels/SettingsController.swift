//
//  SettingsController.swift
//  FT991A-Remote
//
//  Application settings controller
//

import Foundation
import Combine
import SwiftUI

// MARK: - Settings Controller

@MainActor
class SettingsController: ObservableObject {

    // MARK: - Published Properties

    // UI Settings
    @Published var uiStyle: UIStyle = .modern {
        didSet { saveSettings() }
    }

    @Published var language: AppLanguage = .german {
        didSet { saveSettings() }
    }

    @Published var compactMode: Bool = true {
        didSet { saveSettings() }
    }

    @Published var showDebugPanel: Bool = false {
        didSet { saveSettings() }
    }

    @Published var showLogPanel: Bool = false {
        didSet { saveSettings() }
    }

    // Connection Settings
    @Published var autoReconnect: Bool = true {
        didSet { saveSettings() }
    }

    @Published var reconnectInterval: TimeInterval = 5.0 {
        didSet { saveSettings() }
    }

    @Published var defaultBaudRate: Int = 38400 {
        didSet { saveSettings() }
    }

    // Frequency Settings
    @Published var frequencyStep: FrequencyStep = .khz1 {
        didSet { saveSettings() }
    }

    // Logging Settings
    @Published var logDirectory: String = "~/Documents/FT991A-Logs/" {
        didSet { saveSettings() }
    }

    @Published var autoSaveLog: Bool = true {
        didSet { saveSettings() }
    }

    // Audio Settings
    @Published var audioInputDevice: String = "" {
        didSet { saveSettings() }
    }

    @Published var audioOutputDevice: String = "" {
        didSet { saveSettings() }
    }

    @Published var useBlackHole: Bool = false {
        didSet { saveSettings() }
    }

    // Keyboard Settings
    @Published var pttShortcutEnabled: Bool = true {
        didSet { saveSettings() }
    }

    @Published var arrowFrequencyEnabled: Bool = true {
        didSet { saveSettings() }
    }

    @Published var tunerShortcutEnabled: Bool = true {
        didSet { saveSettings() }
    }

    // MARK: - Private Properties

    private var settings: AppSettings
    private var saveDebounce: Timer?

    // MARK: - Initialization

    init() {
        settings = AppSettings.load()
        loadFromSettings()
    }

    // MARK: - Settings Management

    private func loadFromSettings() {
        uiStyle = settings.uiStyle
        language = settings.language
        compactMode = settings.compactMode
        showDebugPanel = settings.showDebugPanel
        showLogPanel = settings.showLogPanel
        autoReconnect = settings.autoReconnect
        reconnectInterval = settings.reconnectInterval
        frequencyStep = settings.frequencyStep
        logDirectory = settings.logDirectory
        autoSaveLog = settings.autoSaveLog
        audioInputDevice = settings.audioInputDevice
        audioOutputDevice = settings.audioOutputDevice
        useBlackHole = settings.useBlackHole
        pttShortcutEnabled = settings.pttShortcutEnabled
        arrowFrequencyEnabled = settings.arrowFrequencyEnabled
        tunerShortcutEnabled = settings.tunerShortcutEnabled
        defaultBaudRate = settings.baudRate
    }

    private func saveSettings() {
        // Debounce saves to avoid excessive disk writes
        saveDebounce?.invalidate()
        saveDebounce = Timer.scheduledTimer(withTimeInterval: 0.5, repeats: false) { [weak self] _ in
            self?.performSave()
        }
    }

    private func performSave() {
        settings.uiStyle = uiStyle
        settings.language = language
        settings.compactMode = compactMode
        settings.showDebugPanel = showDebugPanel
        settings.showLogPanel = showLogPanel
        settings.autoReconnect = autoReconnect
        settings.reconnectInterval = reconnectInterval
        settings.frequencyStep = frequencyStep
        settings.logDirectory = logDirectory
        settings.autoSaveLog = autoSaveLog
        settings.audioInputDevice = audioInputDevice
        settings.audioOutputDevice = audioOutputDevice
        settings.useBlackHole = useBlackHole
        settings.pttShortcutEnabled = pttShortcutEnabled
        settings.arrowFrequencyEnabled = arrowFrequencyEnabled
        settings.tunerShortcutEnabled = tunerShortcutEnabled
        settings.baudRate = defaultBaudRate

        settings.save()
        Logger.shared.log("Settings saved", level: .debug)
    }

    func resetToDefaults() {
        settings = AppSettings.defaults
        loadFromSettings()
        settings.save()
        Logger.shared.log("Settings reset to defaults", level: .info)
    }

    // MARK: - Helpers

    var expandedLogDirectory: String {
        (logDirectory as NSString).expandingTildeInPath
    }

    static let availableBaudRates = [4800, 9600, 19200, 38400, 57600, 115200]
}
