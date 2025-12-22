//
//  ColorPalette.swift
//  PsytranceVisualizer
//
//  Psytrance color palette for UI and shaders
//

import AppKit
import simd

/// Psytrance-inspired neon/UV color palette
struct PsytranceColors {
    // MARK: - Primary Colors (NSColor for UI)

    /// Neon Magenta - Primary accent color
    static let neonMagenta = NSColor(red: 1.0, green: 0.0, blue: 1.0, alpha: 1.0)

    /// Neon Cyan - Secondary accent color
    static let neonCyan = NSColor(red: 0.0, green: 1.0, blue: 1.0, alpha: 1.0)

    /// Neon Green - High energy accents
    static let neonGreen = NSColor(red: 0.224, green: 1.0, blue: 0.078, alpha: 1.0)

    /// UV Violet - Deep purple for backgrounds
    static let uvViolet = NSColor(red: 0.482, green: 0.0, blue: 1.0, alpha: 1.0)

    /// Deep Black - Background color
    static let background = NSColor(red: 0.0, green: 0.0, blue: 0.0, alpha: 1.0)

    /// Dark Purple - Alternative background
    static let darkPurple = NSColor(red: 0.1, green: 0.0, blue: 0.15, alpha: 1.0)

    /// Hot Pink - Peak indicators
    static let hotPink = NSColor(red: 1.0, green: 0.2, blue: 0.6, alpha: 1.0)

    /// Electric Blue - UI elements
    static let electricBlue = NSColor(red: 0.0, green: 0.5, blue: 1.0, alpha: 1.0)

    // MARK: - SIMD3<Float> Colors (for Metal shaders)

    struct Metal {
        static let neonMagenta = SIMD3<Float>(1.0, 0.0, 1.0)
        static let neonCyan = SIMD3<Float>(0.0, 1.0, 1.0)
        static let neonGreen = SIMD3<Float>(0.224, 1.0, 0.078)
        static let uvViolet = SIMD3<Float>(0.482, 0.0, 1.0)
        static let background = SIMD3<Float>(0.0, 0.0, 0.0)
        static let darkPurple = SIMD3<Float>(0.1, 0.0, 0.15)
        static let hotPink = SIMD3<Float>(1.0, 0.2, 0.6)
        static let electricBlue = SIMD3<Float>(0.0, 0.5, 1.0)

        /// Array of all palette colors for cycling
        static let palette: [SIMD3<Float>] = [
            neonMagenta,
            neonCyan,
            neonGreen,
            uvViolet,
            hotPink,
            electricBlue
        ]

        /// Get color from palette by index (wraps around)
        static func color(at index: Int) -> SIMD3<Float> {
            palette[index % palette.count]
        }

        /// Interpolate between two colors
        static func lerp(_ a: SIMD3<Float>, _ b: SIMD3<Float>, t: Float) -> SIMD3<Float> {
            a + (b - a) * t
        }

        /// Get rainbow color from normalized value (0-1)
        static func rainbow(_ t: Float) -> SIMD3<Float> {
            let index = Int(t * Float(palette.count))
            let nextIndex = (index + 1) % palette.count
            let localT = (t * Float(palette.count)) - Float(index)
            return lerp(palette[index % palette.count], palette[nextIndex], t: localT)
        }
    }

    // MARK: - Gradient Helpers

    /// Creates a gradient from UV Violet through Magenta to Cyan
    static var spectrumGradient: NSGradient? {
        NSGradient(colors: [uvViolet, neonMagenta, hotPink, neonCyan, neonGreen])
    }

    /// Creates a gradient for heat maps (low to high energy)
    static var heatmapGradient: NSGradient? {
        NSGradient(colors: [
            NSColor(red: 0.1, green: 0.0, blue: 0.2, alpha: 1.0),  // Dark purple (low)
            uvViolet,
            neonMagenta,
            hotPink,
            neonCyan,
            neonGreen,
            NSColor.white  // White (peak)
        ])
    }

    // MARK: - UI Theme Colors

    struct UI {
        static let panelBackground = NSColor(red: 0.05, green: 0.02, blue: 0.08, alpha: 0.9)
        static let buttonBackground = NSColor(red: 0.15, green: 0.05, blue: 0.2, alpha: 1.0)
        static let buttonHighlight = neonMagenta.withAlphaComponent(0.8)
        static let sliderTint = neonCyan
        static let labelText = NSColor.white
        static let secondaryText = NSColor(white: 0.7, alpha: 1.0)
        static let border = uvViolet.withAlphaComponent(0.5)
    }
}

// MARK: - NSColor Extension

extension NSColor {
    /// Converts NSColor to SIMD3<Float> for Metal
    var simd3: SIMD3<Float> {
        guard let rgb = usingColorSpace(.deviceRGB) else {
            return SIMD3<Float>(0, 0, 0)
        }
        return SIMD3<Float>(
            Float(rgb.redComponent),
            Float(rgb.greenComponent),
            Float(rgb.blueComponent)
        )
    }

    /// Converts NSColor to SIMD4<Float> for Metal (with alpha)
    var simd4: SIMD4<Float> {
        guard let rgb = usingColorSpace(.deviceRGB) else {
            return SIMD4<Float>(0, 0, 0, 1)
        }
        return SIMD4<Float>(
            Float(rgb.redComponent),
            Float(rgb.greenComponent),
            Float(rgb.blueComponent),
            Float(rgb.alphaComponent)
        )
    }
}
