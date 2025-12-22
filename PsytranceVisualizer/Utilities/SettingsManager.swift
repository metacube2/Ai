//
//  SettingsManager.swift
//  PsytranceVisualizer
//
//  Handles loading and saving of application settings
//

import Foundation
import Combine

/// Manages persistent storage and retrieval of application settings
final class SettingsManager: ObservableObject {
    // MARK: - Singleton

    static let shared = SettingsManager()

    // MARK: - Published Properties

    @Published private(set) var settings: AppSettings

    // MARK: - Private Properties

    private let settingsKey = "PsytranceVisualizerSettings"
    private let fileManager = FileManager.default
    private var saveWorkItem: DispatchWorkItem?

    // MARK: - Initialization

    private init() {
        self.settings = SettingsManager.loadSettings()
    }

    // MARK: - Public Methods

    /// Updates settings and triggers auto-save
    func updateSettings(_ update: (inout AppSettings) -> Void) {
        update(&settings)
        settings.validate()
        scheduleSave()
    }

    /// Updates selected audio device
    func setAudioDevice(uid: String?) {
        updateSettings { $0.selectedAudioDeviceUID = uid }
    }

    /// Updates buffer size
    func setBufferSize(_ size: Int) {
        guard AppSettings.availableBufferSizes.contains(size) else { return }
        updateSettings { $0.bufferSize = size }
    }

    /// Updates visualization mode
    func setVisualizationMode(_ mode: VisualizationMode) {
        updateSettings { $0.lastVisualizationMode = mode.rawValue }
    }

    /// Updates reactivity
    func setReactivity(_ value: Float) {
        updateSettings { $0.reactivity = max(0.0, min(1.0, value)) }
    }

    /// Updates fullscreen state
    func setFullscreen(_ isFullscreen: Bool) {
        updateSettings { $0.isFullscreen = isFullscreen }
    }

    /// Updates window frame
    func setWindowFrame(_ frame: CGRect) {
        updateSettings { $0.windowFrame = CodableRect(from: frame) }
    }

    /// Updates input gain
    func setInputGain(_ gain: Float) {
        updateSettings { $0.inputGain = max(0.0, min(2.0, gain)) }
    }

    /// Updates FPS display setting
    func setShowFPS(_ show: Bool) {
        updateSettings { $0.showFPS = show }
    }

    /// Forces immediate save
    func saveNow() {
        saveWorkItem?.cancel()
        performSave()
    }

    /// Resets to default settings
    func resetToDefaults() {
        settings = .default
        saveNow()
    }

    // MARK: - Private Methods

    /// Schedules a debounced save operation
    private func scheduleSave() {
        saveWorkItem?.cancel()

        let workItem = DispatchWorkItem { [weak self] in
            self?.performSave()
        }

        saveWorkItem = workItem
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.5, execute: workItem)
    }

    /// Performs the actual save operation
    private func performSave() {
        do {
            let encoder = JSONEncoder()
            encoder.outputFormatting = .prettyPrinted
            let data = try encoder.encode(settings)

            // Save to UserDefaults
            UserDefaults.standard.set(data, forKey: settingsKey)

            // Also save to file for backup
            if let url = settingsFileURL {
                try data.write(to: url)
            }

            print("[SettingsManager] Settings saved successfully")
        } catch {
            print("[SettingsManager] Failed to save settings: \(error)")
        }
    }

    /// Loads settings from storage
    private static func loadSettings() -> AppSettings {
        // Try UserDefaults first
        if let data = UserDefaults.standard.data(forKey: "PsytranceVisualizerSettings") {
            do {
                var settings = try JSONDecoder().decode(AppSettings.self, from: data)
                settings.validate()
                print("[SettingsManager] Settings loaded from UserDefaults")
                return settings
            } catch {
                print("[SettingsManager] Failed to decode settings from UserDefaults: \(error)")
            }
        }

        // Try file backup
        if let url = settingsFileURL,
           let data = try? Data(contentsOf: url) {
            do {
                var settings = try JSONDecoder().decode(AppSettings.self, from: data)
                settings.validate()
                print("[SettingsManager] Settings loaded from file")
                return settings
            } catch {
                print("[SettingsManager] Failed to decode settings from file: \(error)")
            }
        }

        print("[SettingsManager] Using default settings")
        return .default
    }

    /// URL for settings file backup
    private static var settingsFileURL: URL? {
        guard let appSupport = FileManager.default.urls(
            for: .applicationSupportDirectory,
            in: .userDomainMask
        ).first else {
            return nil
        }

        let appDirectory = appSupport.appendingPathComponent("PsytranceVisualizer")

        // Create directory if needed
        try? FileManager.default.createDirectory(
            at: appDirectory,
            withIntermediateDirectories: true
        )

        return appDirectory.appendingPathComponent("settings.json")
    }

    /// Current visualization mode
    var currentVisualizationMode: VisualizationMode {
        VisualizationMode(rawValue: settings.lastVisualizationMode) ?? .fftClassic
    }
}
