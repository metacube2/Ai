//
//  DSPEngine.swift
//  PsytranceVisualizer
//
//  Digital Signal Processing engine for audio analysis
//

import Accelerate
import AVFoundation

/// DSP Engine for real-time audio analysis
final class DSPEngine {
    // MARK: - Configuration

    private let sampleRate: Float = 44100.0
    private var fftSize: Int
    private let melBandCount: Int = 64
    private let subBassUpperFreq: Float = 100.0
    private let historySize: Int = 128

    // MARK: - FFT Setup

    private var fftSetup: vDSP_DFT_Setup?
    private var window: [Float]
    private var realPart: [Float]
    private var imagPart: [Float]
    private var magnitudes: [Float]

    // MARK: - Mel Filterbank

    private var melFilterbank: [[Float]]
    private var melOutput: [Float]

    // MARK: - Analysis State

    private var subBassHistory: [Float]
    private var previousMagnitudes: [Float]
    private var envelopeValue: Float = 0
    private var previousEnvelope: Float = 0
    private var pumpHistory: [Float]
    private var lastPeakTime: Double = 0
    private var peakThreshold: Float = 0.3

    // MARK: - Reactivity

    private var reactivity: Float = 0.5
    private var smoothingFactor: Float = 0.3

    // MARK: - Initialization

    init(bufferSize: Int = 1024) {
        self.fftSize = bufferSize

        // Initialize FFT arrays
        self.window = [Float](repeating: 0, count: fftSize)
        self.realPart = [Float](repeating: 0, count: fftSize)
        self.imagPart = [Float](repeating: 0, count: fftSize)
        self.magnitudes = [Float](repeating: 0, count: fftSize / 2)
        self.previousMagnitudes = [Float](repeating: 0, count: fftSize / 2)

        // Initialize Mel arrays
        self.melOutput = [Float](repeating: 0, count: melBandCount)
        self.melFilterbank = []

        // Initialize history arrays
        self.subBassHistory = [Float](repeating: 0, count: historySize)
        self.pumpHistory = [Float](repeating: 0, count: 64)

        // Create Hann window
        vDSP_hann_window(&window, vDSP_Length(fftSize), Int32(vDSP_HANN_NORM))

        // Create FFT setup
        fftSetup = vDSP_DFT_zop_CreateSetup(
            nil,
            vDSP_Length(fftSize),
            .FORWARD
        )

        // Build Mel filterbank
        buildMelFilterbank()
    }

    deinit {
        if let setup = fftSetup {
            vDSP_DFT_DestroySetup(setup)
        }
    }

    // MARK: - Public Methods

    /// Sets reactivity value (0.0 - 1.0)
    func setReactivity(_ value: Float) {
        reactivity = max(0.0, min(1.0, value))
        // Adjust smoothing based on reactivity (higher reactivity = less smoothing)
        smoothingFactor = 0.1 + (1.0 - reactivity) * 0.4
    }

    /// Reconfigures for new buffer size
    func setBufferSize(_ size: Int) {
        guard size != fftSize else { return }

        fftSize = size

        // Reinitialize arrays
        window = [Float](repeating: 0, count: fftSize)
        realPart = [Float](repeating: 0, count: fftSize)
        imagPart = [Float](repeating: 0, count: fftSize)
        magnitudes = [Float](repeating: 0, count: fftSize / 2)
        previousMagnitudes = [Float](repeating: 0, count: fftSize / 2)

        // Recreate window
        vDSP_hann_window(&window, vDSP_Length(fftSize), Int32(vDSP_HANN_NORM))

        // Recreate FFT setup
        if let setup = fftSetup {
            vDSP_DFT_DestroySetup(setup)
        }
        fftSetup = vDSP_DFT_zop_CreateSetup(nil, vDSP_Length(fftSize), .FORWARD)

        // Rebuild filterbank
        buildMelFilterbank()
    }

    /// Processes audio buffer and returns analysis data
    func process(buffer: AVAudioPCMBuffer) -> AudioAnalysisData {
        guard let channelData = buffer.floatChannelData else {
            return .empty
        }

        let frameCount = Int(buffer.frameLength)
        let channelCount = Int(buffer.format.channelCount)

        // Extract stereo channels
        var leftChannel = [Float](repeating: 0, count: frameCount)
        var rightChannel = [Float](repeating: 0, count: frameCount)

        if channelCount >= 1 {
            leftChannel = Array(UnsafeBufferPointer(start: channelData[0], count: frameCount))
        }
        if channelCount >= 2 {
            rightChannel = Array(UnsafeBufferPointer(start: channelData[1], count: frameCount))
        } else {
            rightChannel = leftChannel
        }

        // Mix to mono for analysis
        var monoBuffer = [Float](repeating: 0, count: frameCount)
        vDSP_vadd(leftChannel, 1, rightChannel, 1, &monoBuffer, 1, vDSP_Length(frameCount))
        var half: Float = 0.5
        vDSP_vsmul(monoBuffer, 1, &half, &monoBuffer, 1, vDSP_Length(frameCount))

        // Calculate RMS
        var rmsValue: Float = 0
        vDSP_rmsqv(monoBuffer, 1, &rmsValue, vDSP_Length(frameCount))

        // Perform FFT
        let fftMagnitudes = performFFT(monoBuffer)

        // Calculate Mel bands
        let melBands = calculateMelBands(from: fftMagnitudes)

        // Extract sub-bass
        let subBassEnergy = calculateSubBassEnergy(from: fftMagnitudes)

        // Update sub-bass history
        subBassHistory.removeFirst()
        subBassHistory.append(subBassEnergy)

        // Calculate sidechain envelope and pump detection
        let (envelope, pumpAmount, isPumping) = detectSidechainPump(subBassEnergy: subBassEnergy)

        // Calculate HNR
        let hnrRatio = calculateHNR(buffer: monoBuffer)

        // Detect peaks/transients
        let (isPeak, peakIntensity) = detectPeak(rms: rmsValue)

        // Calculate spectral centroid
        let spectralCentroid = calculateSpectralCentroid(magnitudes: fftMagnitudes)

        return AudioAnalysisData(
            fftMagnitudes: fftMagnitudes,
            melBands: melBands,
            subBassEnergy: subBassEnergy,
            subBassHistory: subBassHistory,
            sidechainEnvelope: envelope,
            sidechainPumpAmount: pumpAmount,
            isPumping: isPumping,
            hnrRatio: hnrRatio,
            isPeak: isPeak,
            peakIntensity: peakIntensity,
            leftChannel: leftChannel,
            rightChannel: rightChannel,
            spectralCentroid: spectralCentroid,
            rmsLevel: rmsValue
        )
    }

    // MARK: - FFT

    private func performFFT(_ buffer: [Float]) -> [Float] {
        guard let setup = fftSetup else { return magnitudes }

        let count = min(buffer.count, fftSize)

        // Apply window
        var windowedBuffer = [Float](repeating: 0, count: fftSize)
        for i in 0..<count {
            windowedBuffer[i] = buffer[i] * window[i]
        }

        // Prepare for DFT (separate into real and imaginary)
        for i in 0..<fftSize {
            realPart[i] = windowedBuffer[i]
            imagPart[i] = 0
        }

        // Perform DFT
        var outputReal = [Float](repeating: 0, count: fftSize)
        var outputImag = [Float](repeating: 0, count: fftSize)

        vDSP_DFT_Execute(setup, realPart, imagPart, &outputReal, &outputImag)

        // Calculate magnitudes
        let halfSize = fftSize / 2
        var newMagnitudes = [Float](repeating: 0, count: halfSize)

        for i in 0..<halfSize {
            let real = outputReal[i]
            let imag = outputImag[i]
            newMagnitudes[i] = sqrt(real * real + imag * imag) / Float(fftSize)
        }

        // Apply smoothing
        for i in 0..<halfSize {
            magnitudes[i] = magnitudes[i] * smoothingFactor + newMagnitudes[i] * (1.0 - smoothingFactor)
        }

        previousMagnitudes = magnitudes

        return magnitudes
    }

    // MARK: - Mel Filterbank

    private func buildMelFilterbank() {
        let halfFFT = fftSize / 2
        let nyquist = sampleRate / 2.0

        // Mel scale conversion
        func hzToMel(_ hz: Float) -> Float {
            return 2595.0 * log10(1.0 + hz / 700.0)
        }

        func melToHz(_ mel: Float) -> Float {
            return 700.0 * (pow(10.0, mel / 2595.0) - 1.0)
        }

        let melMin = hzToMel(20.0)
        let melMax = hzToMel(nyquist)

        // Create mel points
        var melPoints = [Float](repeating: 0, count: melBandCount + 2)
        for i in 0..<melBandCount + 2 {
            melPoints[i] = melMin + Float(i) * (melMax - melMin) / Float(melBandCount + 1)
        }

        // Convert back to Hz
        var hzPoints = melPoints.map { melToHz($0) }

        // Convert to FFT bins
        var binPoints = hzPoints.map { Int($0 / nyquist * Float(halfFFT)) }

        // Build triangular filters
        melFilterbank = []

        for m in 1...melBandCount {
            var filter = [Float](repeating: 0, count: halfFFT)

            let startBin = binPoints[m - 1]
            let centerBin = binPoints[m]
            let endBin = binPoints[m + 1]

            // Rising edge
            for k in startBin..<centerBin {
                if centerBin != startBin {
                    filter[k] = Float(k - startBin) / Float(centerBin - startBin)
                }
            }

            // Falling edge
            for k in centerBin..<endBin {
                if endBin != centerBin {
                    filter[k] = Float(endBin - k) / Float(endBin - centerBin)
                }
            }

            melFilterbank.append(filter)
        }
    }

    private func calculateMelBands(from magnitudes: [Float]) -> [Float] {
        var result = [Float](repeating: 0, count: melBandCount)

        for (i, filter) in melFilterbank.enumerated() {
            var sum: Float = 0
            let count = min(filter.count, magnitudes.count)
            for j in 0..<count {
                sum += magnitudes[j] * filter[j]
            }
            // Apply logarithmic scaling
            result[i] = log10(1.0 + sum * 10.0) / log10(11.0)
        }

        // Apply smoothing to mel output
        for i in 0..<melBandCount {
            melOutput[i] = melOutput[i] * smoothingFactor + result[i] * (1.0 - smoothingFactor)
        }

        return melOutput
    }

    // MARK: - Sub-Bass Analysis

    private func calculateSubBassEnergy(from magnitudes: [Float]) -> Float {
        let binFrequency = sampleRate / Float(fftSize)
        let subBassBinCount = Int(subBassUpperFreq / binFrequency)

        guard subBassBinCount > 0, magnitudes.count >= subBassBinCount else { return 0 }

        var sum: Float = 0
        for i in 0..<subBassBinCount {
            sum += magnitudes[i] * magnitudes[i]
        }

        let rms = sqrt(sum / Float(subBassBinCount))

        // Normalize and apply gain
        let normalized = min(1.0, rms * 5.0 * (1.0 + reactivity))

        return normalized
    }

    // MARK: - Sidechain Pump Detection

    private func detectSidechainPump(subBassEnergy: Float) -> (envelope: Float, pumpAmount: Float, isPumping: Bool) {
        // Envelope follower with fast attack, slow release
        let attackTime: Float = 0.005  // 5ms attack
        let releaseTime: Float = 0.15  // 150ms release

        let attackCoeff = exp(-1.0 / (sampleRate * attackTime))
        let releaseCoeff = exp(-1.0 / (sampleRate * releaseTime))

        if subBassEnergy > envelopeValue {
            envelopeValue = attackCoeff * envelopeValue + (1.0 - attackCoeff) * subBassEnergy
        } else {
            envelopeValue = releaseCoeff * envelopeValue + (1.0 - releaseCoeff) * subBassEnergy
        }

        // Update pump history
        pumpHistory.removeFirst()
        pumpHistory.append(envelopeValue)

        // Analyze pump periodicity
        var pumpAmount: Float = 0
        var isPumping = false

        // Look for characteristic pump pattern (rise and fall)
        let derivative = envelopeValue - previousEnvelope
        previousEnvelope = envelopeValue

        // Detect pump by finding periodic envelope variations
        if pumpHistory.count >= 32 {
            let recent = Array(pumpHistory.suffix(32))
            var variance: Float = 0
            let mean = recent.reduce(0, +) / Float(recent.count)

            for value in recent {
                variance += (value - mean) * (value - mean)
            }
            variance /= Float(recent.count)

            // Higher variance = more pumping
            pumpAmount = min(1.0, sqrt(variance) * 4.0)
            isPumping = pumpAmount > 0.3 && abs(derivative) > 0.02
        }

        return (envelopeValue, pumpAmount, isPumping)
    }

    // MARK: - HNR Calculation

    private func calculateHNR(buffer: [Float]) -> Float {
        // Use autocorrelation to estimate harmonicity
        let frameSize = min(buffer.count, 512)
        var autocorr = [Float](repeating: 0, count: frameSize)

        // Compute autocorrelation
        vDSP_conv(buffer, 1, buffer, 1, &autocorr, 1, vDSP_Length(frameSize), vDSP_Length(frameSize))

        // Find the peak in autocorrelation (excluding lag 0)
        let minLag = 20  // Minimum lag to avoid DC component
        let maxLag = min(frameSize - 1, 400)  // Maximum lag

        guard maxLag > minLag else { return 0.5 }

        var maxValue: Float = 0
        var maxIndex: vDSP_Length = 0

        let searchRange = Array(autocorr[minLag...maxLag])
        vDSP_maxvi(searchRange, 1, &maxValue, &maxIndex, vDSP_Length(searchRange.count))

        // Calculate HNR as ratio of peak to first value
        let noiseFloor = autocorr.suffix(from: maxLag).reduce(0) { $0 + abs($1) } / Float(frameSize - maxLag)

        let harmonicPower = maxValue
        let noisePower = max(noiseFloor, 0.0001)

        // Convert to 0-1 range
        let hnr = harmonicPower / (harmonicPower + noisePower)

        return max(0.0, min(1.0, hnr))
    }

    // MARK: - Peak Detection

    private var previousRMS: Float = 0
    private var rmsHistory: [Float] = Array(repeating: 0, count: 16)

    private func detectPeak(rms: Float) -> (isPeak: Bool, intensity: Float) {
        // Update history
        rmsHistory.removeFirst()
        rmsHistory.append(rms)

        // Calculate moving average
        let average = rmsHistory.reduce(0, +) / Float(rmsHistory.count)

        // Detect sudden increase
        let increase = rms - previousRMS
        let threshold = average * (0.5 + reactivity * 0.5)

        previousRMS = rms

        let isPeak = increase > threshold && rms > average * 1.5
        let intensity = isPeak ? min(1.0, increase / max(average, 0.01) * 2.0) : 0

        return (isPeak, intensity)
    }

    // MARK: - Spectral Centroid

    private func calculateSpectralCentroid(magnitudes: [Float]) -> Float {
        var weightedSum: Float = 0
        var sum: Float = 0

        for (i, mag) in magnitudes.enumerated() {
            weightedSum += Float(i) * mag
            sum += mag
        }

        guard sum > 0 else { return 0.5 }

        let centroid = weightedSum / sum
        let normalized = centroid / Float(magnitudes.count)

        return max(0.0, min(1.0, normalized))
    }
}
