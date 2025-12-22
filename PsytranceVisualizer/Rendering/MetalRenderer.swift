//
//  MetalRenderer.swift
//  PsytranceVisualizer
//
//  Metal-based renderer for all visualization modes
//

import MetalKit
import simd

/// Uniform data passed to all shaders
struct ShaderUniforms {
    var time: Float
    var resolution: SIMD2<Float>
    var reactivity: Float

    // Audio analysis data
    var subBassEnergy: Float
    var sidechainPump: Float
    var sidechainEnvelope: Float
    var hnrRatio: Float
    var isPeak: Float
    var peakIntensity: Float
    var spectralCentroid: Float
    var rmsLevel: Float

    // Visualization mode (1-8)
    var mode: Int32

    // Padding for Metal alignment
    var padding: SIMD2<Float> = .zero
}

/// Metal renderer managing all visualization shaders
final class MetalRenderer: NSObject, ObservableObject {
    // MARK: - Properties

    private let device: MTLDevice
    private let commandQueue: MTLCommandQueue
    private var pipelineStates: [VisualizationMode: MTLRenderPipelineState] = [:]
    private var currentPipelineState: MTLRenderPipelineState?

    @Published private(set) var currentMode: VisualizationMode = .fftClassic

    // MARK: - Buffers

    private var uniformBuffer: MTLBuffer?
    private var fftBuffer: MTLBuffer?
    private var melBuffer: MTLBuffer?
    private var subBassHistoryBuffer: MTLBuffer?

    // MARK: - State

    private var startTime: CFAbsoluteTime
    private var uniforms = ShaderUniforms(
        time: 0,
        resolution: SIMD2<Float>(1920, 1080),
        reactivity: 0.5,
        subBassEnergy: 0,
        sidechainPump: 0,
        sidechainEnvelope: 0,
        hnrRatio: 0.5,
        isPeak: 0,
        peakIntensity: 0,
        spectralCentroid: 0.5,
        rmsLevel: 0,
        mode: 1
    )

    private var audioData: AudioAnalysisData = .empty

    // MARK: - Constants

    private let maxFFTSize = 1024
    private let melBandCount = 64
    private let historySize = 128

    // MARK: - Initialization

    init?(device: MTLDevice) {
        guard let queue = device.makeCommandQueue() else {
            print("[MetalRenderer] Failed to create command queue")
            return nil
        }

        self.device = device
        self.commandQueue = queue
        self.startTime = CFAbsoluteTimeGetCurrent()

        super.init()

        createBuffers()
        loadShaders()
    }

    // MARK: - Public Methods

    /// Sets the current visualization mode
    func setVisualizationMode(_ mode: VisualizationMode) {
        currentMode = mode
        currentPipelineState = pipelineStates[mode]
        uniforms.mode = Int32(mode.rawValue)
        print("[MetalRenderer] Mode changed to: \(mode.displayName)")
    }

    /// Updates audio analysis data
    func updateAudioData(_ data: AudioAnalysisData) {
        audioData = data

        // Update uniforms
        uniforms.subBassEnergy = data.subBassEnergy
        uniforms.sidechainPump = data.sidechainPumpAmount
        uniforms.sidechainEnvelope = data.sidechainEnvelope
        uniforms.hnrRatio = data.hnrRatio
        uniforms.isPeak = data.isPeak ? 1.0 : 0.0
        uniforms.peakIntensity = data.peakIntensity
        uniforms.spectralCentroid = data.spectralCentroid
        uniforms.rmsLevel = data.rmsLevel

        // Update FFT buffer
        updateFFTBuffer(data.fftMagnitudes)

        // Update Mel buffer
        updateMelBuffer(data.melBands)

        // Update sub-bass history buffer
        updateSubBassHistoryBuffer(data.subBassHistory)
    }

    /// Sets reactivity value
    func setReactivity(_ value: Float) {
        uniforms.reactivity = max(0.0, min(1.0, value))
    }

    // MARK: - Private Methods

    private func createBuffers() {
        // Uniform buffer
        uniformBuffer = device.makeBuffer(
            length: MemoryLayout<ShaderUniforms>.stride,
            options: .storageModeShared
        )

        // FFT magnitude buffer
        fftBuffer = device.makeBuffer(
            length: maxFFTSize * MemoryLayout<Float>.stride,
            options: .storageModeShared
        )

        // Mel bands buffer
        melBuffer = device.makeBuffer(
            length: melBandCount * MemoryLayout<Float>.stride,
            options: .storageModeShared
        )

        // Sub-bass history buffer
        subBassHistoryBuffer = device.makeBuffer(
            length: historySize * MemoryLayout<Float>.stride,
            options: .storageModeShared
        )
    }

    private func updateFFTBuffer(_ magnitudes: [Float]) {
        guard let buffer = fftBuffer else { return }
        let count = min(magnitudes.count, maxFFTSize)
        memcpy(buffer.contents(), magnitudes, count * MemoryLayout<Float>.stride)
    }

    private func updateMelBuffer(_ bands: [Float]) {
        guard let buffer = melBuffer else { return }
        let count = min(bands.count, melBandCount)
        memcpy(buffer.contents(), bands, count * MemoryLayout<Float>.stride)
    }

    private func updateSubBassHistoryBuffer(_ history: [Float]) {
        guard let buffer = subBassHistoryBuffer else { return }
        let count = min(history.count, historySize)
        memcpy(buffer.contents(), history, count * MemoryLayout<Float>.stride)
    }

    private func loadShaders() {
        guard let library = device.makeDefaultLibrary() else {
            print("[MetalRenderer] Failed to load shader library")
            return
        }

        // Load vertex shader (shared)
        guard let vertexFunction = library.makeFunction(name: "vertexShader") else {
            print("[MetalRenderer] Failed to load vertex shader")
            return
        }

        // Load all fragment shaders
        for mode in VisualizationMode.allCases {
            guard let fragmentFunction = library.makeFunction(name: mode.shaderFunctionName) else {
                print("[MetalRenderer] Failed to load shader: \(mode.shaderFunctionName)")
                continue
            }

            let descriptor = MTLRenderPipelineDescriptor()
            descriptor.vertexFunction = vertexFunction
            descriptor.fragmentFunction = fragmentFunction
            descriptor.colorAttachments[0].pixelFormat = .bgra8Unorm

            // Enable blending for glow effects
            descriptor.colorAttachments[0].isBlendingEnabled = true
            descriptor.colorAttachments[0].sourceRGBBlendFactor = .sourceAlpha
            descriptor.colorAttachments[0].destinationRGBBlendFactor = .oneMinusSourceAlpha
            descriptor.colorAttachments[0].sourceAlphaBlendFactor = .one
            descriptor.colorAttachments[0].destinationAlphaBlendFactor = .oneMinusSourceAlpha

            do {
                let pipelineState = try device.makeRenderPipelineState(descriptor: descriptor)
                pipelineStates[mode] = pipelineState
                print("[MetalRenderer] Loaded shader: \(mode.displayName)")
            } catch {
                print("[MetalRenderer] Failed to create pipeline state for \(mode.displayName): \(error)")
            }
        }

        // Set initial pipeline state
        currentPipelineState = pipelineStates[.fftClassic]
    }
}

// MARK: - MTKViewDelegate

extension MetalRenderer: MTKViewDelegate {
    func mtkView(_ view: MTKView, drawableSizeWillChange size: CGSize) {
        uniforms.resolution = SIMD2<Float>(Float(size.width), Float(size.height))
    }

    func draw(in view: MTKView) {
        guard let pipelineState = currentPipelineState,
              let drawable = view.currentDrawable,
              let renderPassDescriptor = view.currentRenderPassDescriptor else {
            return
        }

        // Update time
        uniforms.time = Float(CFAbsoluteTimeGetCurrent() - startTime)

        // Update uniform buffer
        if let buffer = uniformBuffer {
            memcpy(buffer.contents(), &uniforms, MemoryLayout<ShaderUniforms>.stride)
        }

        // Create command buffer
        guard let commandBuffer = commandQueue.makeCommandBuffer(),
              let renderEncoder = commandBuffer.makeRenderCommandEncoder(descriptor: renderPassDescriptor) else {
            return
        }

        // Set pipeline state
        renderEncoder.setRenderPipelineState(pipelineState)

        // Set buffers
        if let buffer = uniformBuffer {
            renderEncoder.setFragmentBuffer(buffer, offset: 0, index: 0)
        }
        if let buffer = fftBuffer {
            renderEncoder.setFragmentBuffer(buffer, offset: 0, index: 1)
        }
        if let buffer = melBuffer {
            renderEncoder.setFragmentBuffer(buffer, offset: 0, index: 2)
        }
        if let buffer = subBassHistoryBuffer {
            renderEncoder.setFragmentBuffer(buffer, offset: 0, index: 3)
        }

        // Draw fullscreen quad
        renderEncoder.drawPrimitives(type: .triangleStrip, vertexStart: 0, vertexCount: 4)

        renderEncoder.endEncoding()

        commandBuffer.present(drawable)
        commandBuffer.commit()
    }
}
