//
//  Settings.swift
//  FT991A-Remote
//
//  Application settings model
//

import Foundation

// MARK: - App Settings

struct AppSettings: Codable {
    // Connection
    var radioModel: RadioModel = .ft991a
    var serialPort: String = ""
    var baudRate: Int = 38400
    var autoReconnect: Bool = true
    var reconnectInterval: TimeInterval = 5.0
    var autoConnectOnLaunch: Bool = false
    var searchUSBPortsUntilFound: Bool = false
    var preferredCATProfile: String = "ft991a-macos"
    var setupCompleted: Bool = false

    // UI
    var uiStyle: UIStyle = .modern
    var language: AppLanguage = .german
    var showDebugPanel: Bool = false
    var showLogPanel: Bool = false
    var showParrotPanel: Bool = false
    var compactMode: Bool = true
    var radioWorkspaceWidth: Double = 980

    // Frequency
    var frequencyStep: FrequencyStep = .khz1

    // Logging
    var logDirectory: String = "~/Documents/FT991A-Logs/"
    var autoSaveLog: Bool = true

    // Audio
    var audioInputDevice: String = ""
    var audioOutputDevice: String = ""
    var parrotInputDeviceUID: String = ""
    var parrotOutputDeviceUID: String = ""
    var useBlackHole: Bool = false

    // Keyboard
    var pttShortcutEnabled: Bool = true
    var arrowFrequencyEnabled: Bool = true
    var tunerShortcutEnabled: Bool = true

    // Activation
    var licenseEmail: String = ""
    var licenseKey: String = ""
    var isActivated: Bool = false
    var trialConsumedSeconds: Int = 0

    // MARK: - Persistence

    static let defaults = AppSettings()

    init() { }

    enum CodingKeys: String, CodingKey {
        case serialPort
        case radioModel
        case baudRate
        case autoReconnect
        case reconnectInterval
        case autoConnectOnLaunch
        case searchUSBPortsUntilFound
        case preferredCATProfile
        case setupCompleted
        case uiStyle
        case language
        case showDebugPanel
        case showLogPanel
        case showParrotPanel
        case compactMode
        case radioWorkspaceWidth
        case frequencyStep
        case logDirectory
        case autoSaveLog
        case audioInputDevice
        case audioOutputDevice
        case parrotInputDeviceUID
        case parrotOutputDeviceUID
        case useBlackHole
        case pttShortcutEnabled
        case arrowFrequencyEnabled
        case tunerShortcutEnabled
        case licenseEmail
        case licenseKey
        case isActivated
        case trialConsumedSeconds
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        serialPort = try container.decodeIfPresent(String.self, forKey: .serialPort) ?? ""
        radioModel = try container.decodeIfPresent(RadioModel.self, forKey: .radioModel) ?? .ft991a
        baudRate = try container.decodeIfPresent(Int.self, forKey: .baudRate) ?? 38400
        autoReconnect = try container.decodeIfPresent(Bool.self, forKey: .autoReconnect) ?? true
        reconnectInterval = try container.decodeIfPresent(TimeInterval.self, forKey: .reconnectInterval) ?? 5.0
        autoConnectOnLaunch = try container.decodeIfPresent(Bool.self, forKey: .autoConnectOnLaunch) ?? false
        searchUSBPortsUntilFound = try container.decodeIfPresent(Bool.self, forKey: .searchUSBPortsUntilFound) ?? false
        preferredCATProfile = try container.decodeIfPresent(String.self, forKey: .preferredCATProfile) ?? "ft991a-macos"
        setupCompleted = try container.decodeIfPresent(Bool.self, forKey: .setupCompleted) ?? false
        uiStyle = try container.decodeIfPresent(UIStyle.self, forKey: .uiStyle) ?? .modern
        language = try container.decodeIfPresent(AppLanguage.self, forKey: .language) ?? .german
        showDebugPanel = try container.decodeIfPresent(Bool.self, forKey: .showDebugPanel) ?? false
        showLogPanel = try container.decodeIfPresent(Bool.self, forKey: .showLogPanel) ?? false
        showParrotPanel = try container.decodeIfPresent(Bool.self, forKey: .showParrotPanel) ?? false
        compactMode = try container.decodeIfPresent(Bool.self, forKey: .compactMode) ?? true
        radioWorkspaceWidth = try container.decodeIfPresent(Double.self, forKey: .radioWorkspaceWidth) ?? 980
        frequencyStep = try container.decodeIfPresent(FrequencyStep.self, forKey: .frequencyStep) ?? .khz1
        logDirectory = try container.decodeIfPresent(String.self, forKey: .logDirectory) ?? "~/Documents/FT991A-Logs/"
        autoSaveLog = try container.decodeIfPresent(Bool.self, forKey: .autoSaveLog) ?? true
        audioInputDevice = try container.decodeIfPresent(String.self, forKey: .audioInputDevice) ?? ""
        audioOutputDevice = try container.decodeIfPresent(String.self, forKey: .audioOutputDevice) ?? ""
        parrotInputDeviceUID = try container.decodeIfPresent(String.self, forKey: .parrotInputDeviceUID) ?? ""
        parrotOutputDeviceUID = try container.decodeIfPresent(String.self, forKey: .parrotOutputDeviceUID) ?? ""
        useBlackHole = try container.decodeIfPresent(Bool.self, forKey: .useBlackHole) ?? false
        pttShortcutEnabled = try container.decodeIfPresent(Bool.self, forKey: .pttShortcutEnabled) ?? true
        arrowFrequencyEnabled = try container.decodeIfPresent(Bool.self, forKey: .arrowFrequencyEnabled) ?? true
        tunerShortcutEnabled = try container.decodeIfPresent(Bool.self, forKey: .tunerShortcutEnabled) ?? true
        licenseEmail = try container.decodeIfPresent(String.self, forKey: .licenseEmail) ?? ""
        licenseKey = try container.decodeIfPresent(String.self, forKey: .licenseKey) ?? ""
        isActivated = try container.decodeIfPresent(Bool.self, forKey: .isActivated) ?? false
        trialConsumedSeconds = try container.decodeIfPresent(Int.self, forKey: .trialConsumedSeconds) ?? 0
    }

    static var settingsURL: URL {
        let appSupport = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask).first!
        let appFolder = appSupport.appendingPathComponent("FT991A-Remote", isDirectory: true)

        try? FileManager.default.createDirectory(at: appFolder, withIntermediateDirectories: true)

        return appFolder.appendingPathComponent("settings.json")
    }

    static func load() -> AppSettings {
        guard FileManager.default.fileExists(atPath: settingsURL.path) else {
            return defaults
        }

        do {
            let data = try Data(contentsOf: settingsURL)
            return try JSONDecoder().decode(AppSettings.self, from: data)
        } catch {
            print("Failed to load settings: \(error)")
            return defaults
        }
    }

    func save() {
        do {
            let data = try JSONEncoder().encode(self)
            try data.write(to: AppSettings.settingsURL)
        } catch {
            print("Failed to save settings: \(error)")
        }
    }

    // MARK: - Log Directory

    var expandedLogDirectory: String {
        (logDirectory as NSString).expandingTildeInPath
    }

    mutating func ensureLogDirectoryExists() {
        let path = expandedLogDirectory
        if !FileManager.default.fileExists(atPath: path) {
            try? FileManager.default.createDirectory(atPath: path, withIntermediateDirectories: true)
        }
    }
}

// MARK: - Serial Port Configuration

struct SerialConfig: Codable {
    var baudRate: Int = 38400
    var dataBits: Int = 8
    var stopBits: Int = 1
    var parity: Parity = .none
    var flowControl: FlowControl = .none

    enum Parity: String, Codable, CaseIterable {
        case none = "None"
        case odd = "Odd"
        case even = "Even"
    }

    enum FlowControl: String, Codable, CaseIterable {
        case none = "None"
        case hardware = "RTS/CTS"
        case software = "XON/XOFF"
    }

    static let ft991aDefault = SerialConfig(
        baudRate: 38400,
        dataBits: 8,
        stopBits: 1,
        parity: .none,
        flowControl: .none
    )

    static let availableBaudRates = [4800, 9600, 19200, 38400, 57600, 115200]
}

// MARK: - Parrot Message

struct ParrotMessage: Identifiable, Codable, Hashable {
    var id = UUID()
    var name: String
    var fileName: String
    var duration: TimeInterval
    var createdAt: Date = Date()
    var updatedAt: Date = Date()
}
