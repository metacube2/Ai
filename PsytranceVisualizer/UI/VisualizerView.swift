//
//  VisualizerView.swift
//  PsytranceVisualizer
//
//  MTKView subclass for rendering visualizations
//

import MetalKit
import Combine

/// MTKView subclass that displays audio-reactive visualizations
final class VisualizerView: MTKView {
    // MARK: - Properties

    private var renderer: MetalRenderer?
    private var cancellables = Set<AnyCancellable>()

    // MARK: - Initialization

    init() {
        // Get default Metal device
        guard let device = MTLCreateSystemDefaultDevice() else {
            fatalError("Metal is not supported on this device")
        }

        super.init(frame: .zero, device: device)

        configure()
        setupRenderer()
    }

    required init(coder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    // MARK: - Configuration

    private func configure() {
        // Background color
        clearColor = MTLClearColor(red: 0, green: 0, blue: 0, alpha: 1)

        // Color format
        colorPixelFormat = .bgra8Unorm

        // Enable display link for smooth rendering
        isPaused = false
        enableSetNeedsDisplay = false

        // Use display refresh rate
        preferredFramesPerSecond = 120 // Will cap to display refresh

        // Layer configuration
        layer?.isOpaque = true

        // Allow high DPI
        layer?.contentsScale = NSScreen.main?.backingScaleFactor ?? 2.0
    }

    private func setupRenderer() {
        guard let device = device else { return }

        renderer = MetalRenderer(device: device)
        delegate = renderer

        // Initial size update
        if let renderer = renderer {
            let size = drawableSize
            renderer.mtkView(self, drawableSizeWillChange: size)
        }
    }

    // MARK: - Public Methods

    /// Returns the Metal renderer
    func getRenderer() -> MetalRenderer? {
        return renderer
    }

    /// Updates audio data for visualization
    func updateAudioData(_ data: AudioAnalysisData) {
        renderer?.updateAudioData(data)
    }

    /// Sets the visualization mode
    func setVisualizationMode(_ mode: VisualizationMode) {
        renderer?.setVisualizationMode(mode)
    }

    /// Sets reactivity value
    func setReactivity(_ value: Float) {
        renderer?.setReactivity(value)
    }

    /// Gets current visualization mode
    var currentMode: VisualizationMode {
        renderer?.currentMode ?? .fftClassic
    }
}

// MARK: - SwiftUI Bridge

#if canImport(SwiftUI)
import SwiftUI

/// SwiftUI wrapper for VisualizerView
struct VisualizerViewRepresentable: NSViewRepresentable {
    @Binding var audioData: AudioAnalysisData
    @Binding var mode: VisualizationMode
    @Binding var reactivity: Float

    func makeNSView(context: Context) -> VisualizerView {
        let view = VisualizerView()
        return view
    }

    func updateNSView(_ nsView: VisualizerView, context: Context) {
        nsView.updateAudioData(audioData)
        nsView.setVisualizationMode(mode)
        nsView.setReactivity(reactivity)
    }
}
#endif
