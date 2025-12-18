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
    var serialPort: String = ""
    var baudRate: Int = 38400
    var autoReconnect: Bool = true
    var reconnectInterval: TimeInterval = 5.0

    // UI
    var uiStyle: UIStyle = .modern
    var language: AppLanguage = .german
    var showDebugPanel: Bool = false
    var showLogPanel: Bool = false
    var compactMode: Bool = true

    // Frequency
    var frequencyStep: FrequencyStep = .khz1

    // Logging
    var logDirectory: String = "~/Documents/FT991A-Logs/"
    var autoSaveLog: Bool = true

    // Audio
    var audioInputDevice: String = ""
    var audioOutputDevice: String = ""
    var useBlackHole: Bool = false

    // Keyboard
    var pttShortcutEnabled: Bool = true
    var arrowFrequencyEnabled: Bool = true
    var tunerShortcutEnabled: Bool = true

    // MARK: - Persistence

    static let defaults = AppSettings()

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
