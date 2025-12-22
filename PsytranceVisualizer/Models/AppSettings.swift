//
//  AppSettings.swift
//  PsytranceVisualizer
//
//  Persistent application settings
//

import Foundation

/// Application settings that are persisted between sessions
struct AppSettings: Codable {
    /// Selected audio input device UID
    var selectedAudioDeviceUID: String?

    /// Audio buffer size (512 or 1024 samples)
    var bufferSize: Int

    /// Last used visualization mode (1-8)
    var lastVisualizationMode: Int

    /// Reactivity slider value (0.0 - 1.0)
    var reactivity: Float

    /// Whether app was in fullscreen mode
    var isFullscreen: Bool

    /// Last window frame (for restoration)
    var windowFrame: CodableRect?

    /// Volume/gain adjustment
    var inputGain: Float

    /// Whether to show FPS counter
    var showFPS: Bool

    /// Default settings
    static var `default`: AppSettings {
        AppSettings(
            selectedAudioDeviceUID: nil,
            bufferSize: 1024,
            lastVisualizationMode: 1,
            reactivity: 0.5,
            isFullscreen: false,
            windowFrame: nil,
            inputGain: 1.0,
            showFPS: false
        )
    }

    /// Available buffer sizes
    static let availableBufferSizes = [512, 1024]

    /// Validates and clamps settings to valid ranges
    mutating func validate() {
        // Clamp buffer size to valid options
        if !AppSettings.availableBufferSizes.contains(bufferSize) {
            bufferSize = 1024
        }

        // Clamp visualization mode
        if lastVisualizationMode < 1 || lastVisualizationMode > 8 {
            lastVisualizationMode = 1
        }

        // Clamp reactivity
        reactivity = max(0.0, min(1.0, reactivity))

        // Clamp input gain
        inputGain = max(0.0, min(2.0, inputGain))
    }
}

/// Codable wrapper for CGRect
struct CodableRect: Codable {
    var x: Double
    var y: Double
    var width: Double
    var height: Double

    init(from rect: CGRect) {
        self.x = Double(rect.origin.x)
        self.y = Double(rect.origin.y)
        self.width = Double(rect.size.width)
        self.height = Double(rect.size.height)
    }

    var cgRect: CGRect {
        CGRect(x: x, y: y, width: width, height: height)
    }
}
