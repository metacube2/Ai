//
//  VisualizationMode.swift
//  PsytranceVisualizer
//
//  Enumeration of all available visualization modes
//

import Foundation

/// Available visualization modes, accessible via keyboard shortcuts 1-8
enum VisualizationMode: Int, CaseIterable, Codable {
    case fftClassic = 1
    case melSpectrogram = 2
    case subBass = 3
    case sidechainPump = 4
    case hnr = 5
    case mandelbrot = 6
    case tunnelWarp = 7
    case dmtGeometry = 8

    /// Display name for UI
    var displayName: String {
        switch self {
        case .fftClassic:
            return "FFT Classic"
        case .melSpectrogram:
            return "Mel Spektrogramm"
        case .subBass:
            return "Sub-Bass (<100Hz)"
        case .sidechainPump:
            return "Sidechain Pump"
        case .hnr:
            return "Harmonic/Noise"
        case .mandelbrot:
            return "Mandelbrot"
        case .tunnelWarp:
            return "Tunnel Warp"
        case .dmtGeometry:
            return "DMT Geometry"
        }
    }

    /// Keyboard shortcut (1-8)
    var shortcut: String {
        return "\(self.rawValue)"
    }

    /// Metal shader function name
    var shaderFunctionName: String {
        switch self {
        case .fftClassic:
            return "fftClassicFragment"
        case .melSpectrogram:
            return "melSpectrogramFragment"
        case .subBass:
            return "subBassFragment"
        case .sidechainPump:
            return "sidechainPumpFragment"
        case .hnr:
            return "hnrFragment"
        case .mandelbrot:
            return "mandelbrotFragment"
        case .tunnelWarp:
            return "tunnelWarpFragment"
        case .dmtGeometry:
            return "dmtGeometryFragment"
        }
    }

    /// Description of the visualization
    var description: String {
        switch self {
        case .fftClassic:
            return "Classic frequency spectrum bars with glow effects"
        case .melSpectrogram:
            return "64-band Mel spectrogram with scrolling waterfall display"
        case .subBass:
            return "Pulsating rings visualizing sub-bass energy below 100Hz"
        case .sidechainPump:
            return "Breathing zoom effect synchronized to sidechain pumping"
        case .hnr:
            return "Harmonic vs noise visualization with geometric shapes"
        case .mandelbrot:
            return "Audio-reactive Mandelbrot fractal with zoom and color cycling"
        case .tunnelWarp:
            return "Infinite tunnel effect with warp distortion"
        case .dmtGeometry:
            return "Sacred geometry patterns: Flower of Life, Metatron's Cube, Sri Yantra"
        }
    }

    /// Creates mode from keyboard key code
    static func fromKeyCode(_ keyCode: UInt16) -> VisualizationMode? {
        // Key codes for 1-8 on US keyboard
        let keyCodes: [UInt16: Int] = [
            18: 1,  // 1
            19: 2,  // 2
            20: 3,  // 3
            21: 4,  // 4
            23: 5,  // 5
            22: 6,  // 6
            26: 7,  // 7
            28: 8   // 8
        ]

        guard let modeNumber = keyCodes[keyCode] else { return nil }
        return VisualizationMode(rawValue: modeNumber)
    }
}
