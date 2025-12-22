//
//  AudioAnalysisData.swift
//  PsytranceVisualizer
//
//  Audio analysis data structure containing all DSP results
//

import Foundation

/// Contains all audio analysis data computed by DSPEngine
struct AudioAnalysisData {
    // MARK: - FFT Data

    /// Raw FFT magnitude spectrum
    var fftMagnitudes: [Float]

    // MARK: - Mel Spectrogram

    /// 64 Mel frequency bands
    var melBands: [Float]

    // MARK: - Sub-Bass Analysis

    /// RMS energy below 100Hz (0.0 - 1.0)
    var subBassEnergy: Float

    /// History buffer for time-based visualization
    var subBassHistory: [Float]

    // MARK: - Sidechain Detection

    /// Current envelope follower value (0.0 - 1.0)
    var sidechainEnvelope: Float

    /// Detected pumping amount (0.0 - 1.0)
    var sidechainPumpAmount: Float

    /// Whether pump is currently active
    var isPumping: Bool

    // MARK: - Harmonic-to-Noise Ratio

    /// HNR ratio (0.0 = noise, 1.0 = pure harmonic)
    var hnrRatio: Float

    // MARK: - Transient Detection

    /// Whether a transient peak was detected
    var isPeak: Bool

    /// Intensity of the detected peak (0.0 - 1.0)
    var peakIntensity: Float

    // MARK: - Stereo Channels

    /// Left channel samples
    var leftChannel: [Float]

    /// Right channel samples
    var rightChannel: [Float]

    // MARK: - Additional Analysis

    /// Spectral centroid (brightness) normalized 0.0 - 1.0
    var spectralCentroid: Float

    /// Overall RMS level
    var rmsLevel: Float

    // MARK: - Initialization

    /// Creates an empty AudioAnalysisData with default values
    static var empty: AudioAnalysisData {
        AudioAnalysisData(
            fftMagnitudes: [],
            melBands: Array(repeating: 0, count: 64),
            subBassEnergy: 0,
            subBassHistory: [],
            sidechainEnvelope: 0,
            sidechainPumpAmount: 0,
            isPumping: false,
            hnrRatio: 0.5,
            isPeak: false,
            peakIntensity: 0,
            leftChannel: [],
            rightChannel: [],
            spectralCentroid: 0.5,
            rmsLevel: 0
        )
    }

    /// Creates AudioAnalysisData with specified FFT size
    static func create(fftSize: Int) -> AudioAnalysisData {
        AudioAnalysisData(
            fftMagnitudes: Array(repeating: 0, count: fftSize / 2),
            melBands: Array(repeating: 0, count: 64),
            subBassEnergy: 0,
            subBassHistory: Array(repeating: 0, count: 128),
            sidechainEnvelope: 0,
            sidechainPumpAmount: 0,
            isPumping: false,
            hnrRatio: 0.5,
            isPeak: false,
            peakIntensity: 0,
            leftChannel: [],
            rightChannel: [],
            spectralCentroid: 0.5,
            rmsLevel: 0
        )
    }
}
