//
//  AudioEngine.swift
//  AudioVUMeter
//
//  Core Audio engine for capturing audio from BlackHole or any input device
//  Calculates RMS levels and converts to dB for VU meter display
//

import Foundation
import AVFoundation
import CoreAudio
import Combine

/// Represents an available audio input device
struct AudioDevice: Identifiable, Hashable {
    let id: AudioDeviceID
    let name: String
    let uid: String
    let inputChannels: Int
}

/// Main audio engine class for capturing and analyzing audio levels
class AudioEngine: ObservableObject {
    // MARK: - Published Properties

    /// Current audio levels (0.0 to 1.0)
    @Published var leftLevel: Double = 0
    @Published var rightLevel: Double = 0

    /// Peak levels with hold
    @Published var leftPeak: Double = 0
    @Published var rightPeak: Double = 0

    /// Levels in dB (-inf to 0)
    @Published var leftLevelDB: Double = -60
    @Published var rightLevelDB: Double = -60

    /// Engine state
    @Published var isRunning = false
    @Published var selectedDeviceID: AudioDeviceID = 0
    @Published var selectedDeviceName: String = "No Device"
    @Published var availableDevices: [AudioDevice] = []

    /// Settings
    @Published var referenceLevel: Double = -18 // Reference level in dB
    @Published var peakHoldTime: Double = 2.0 // Peak hold time in seconds

    // MARK: - Private Properties

    private var audioEngine: AVAudioEngine?
    private var inputNode: AVAudioInputNode?
    private var peakResetTimers: [Timer] = []

    private let levelSmoothingFactor: Double = 0.3
    private var previousLeftLevel: Double = 0
    private var previousRightLevel: Double = 0

    // MARK: - Initialization

    init() {
        refreshDeviceList()
        selectBlackHoleDevice()
    }

    // MARK: - Device Management

    /// Refresh the list of available audio input devices
    func refreshDeviceList() {
        availableDevices = getInputDevices()

        if availableDevices.isEmpty {
            selectedDeviceName = "No Input Devices"
        }
    }

    /// Get all available audio input devices
    private func getInputDevices() -> [AudioDevice] {
        var devices: [AudioDevice] = []

        var propertyAddress = AudioObjectPropertyAddress(
            mSelector: kAudioHardwarePropertyDevices,
            mScope: kAudioObjectPropertyScopeGlobal,
            mElement: kAudioObjectPropertyElementMain
        )

        var propertySize: UInt32 = 0
        var status = AudioObjectGetPropertyDataSize(
            AudioObjectID(kAudioObjectSystemObject),
            &propertyAddress,
            0,
            nil,
            &propertySize
        )

        guard status == noErr else { return devices }

        let deviceCount = Int(propertySize) / MemoryLayout<AudioDeviceID>.size
        var deviceIDs = [AudioDeviceID](repeating: 0, count: deviceCount)

        status = AudioObjectGetPropertyData(
            AudioObjectID(kAudioObjectSystemObject),
            &propertyAddress,
            0,
            nil,
            &propertySize,
            &deviceIDs
        )

        guard status == noErr else { return devices }

        for deviceID in deviceIDs {
            // Check if device has input channels
            let inputChannels = getDeviceInputChannels(deviceID: deviceID)
            guard inputChannels > 0 else { continue }

            // Get device name
            let name = getDeviceName(deviceID: deviceID)
            let uid = getDeviceUID(deviceID: deviceID)

            devices.append(AudioDevice(
                id: deviceID,
                name: name,
                uid: uid,
                inputChannels: inputChannels
            ))
        }

        return devices
    }

    /// Get device name
    private func getDeviceName(deviceID: AudioDeviceID) -> String {
        var propertyAddress = AudioObjectPropertyAddress(
            mSelector: kAudioDevicePropertyDeviceNameCFString,
            mScope: kAudioObjectPropertyScopeGlobal,
            mElement: kAudioObjectPropertyElementMain
        )

        var name: CFString = "" as CFString
        var propertySize = UInt32(MemoryLayout<CFString>.size)

        let status = AudioObjectGetPropertyData(
            deviceID,
            &propertyAddress,
            0,
            nil,
            &propertySize,
            &name
        )

        return status == noErr ? name as String : "Unknown Device"
    }

    /// Get device UID
    private func getDeviceUID(deviceID: AudioDeviceID) -> String {
        var propertyAddress = AudioObjectPropertyAddress(
            mSelector: kAudioDevicePropertyDeviceUID,
            mScope: kAudioObjectPropertyScopeGlobal,
            mElement: kAudioObjectPropertyElementMain
        )

        var uid: CFString = "" as CFString
        var propertySize = UInt32(MemoryLayout<CFString>.size)

        let status = AudioObjectGetPropertyData(
            deviceID,
            &propertyAddress,
            0,
            nil,
            &propertySize,
            &uid
        )

        return status == noErr ? uid as String : ""
    }

    /// Get number of input channels for a device
    private func getDeviceInputChannels(deviceID: AudioDeviceID) -> Int {
        var propertyAddress = AudioObjectPropertyAddress(
            mSelector: kAudioDevicePropertyStreamConfiguration,
            mScope: kAudioDevicePropertyScopeInput,
            mElement: kAudioObjectPropertyElementMain
        )

        var propertySize: UInt32 = 0
        var status = AudioObjectGetPropertyDataSize(
            deviceID,
            &propertyAddress,
            0,
            nil,
            &propertySize
        )

        guard status == noErr, propertySize > 0 else { return 0 }

        let bufferListPointer = UnsafeMutablePointer<AudioBufferList>.allocate(capacity: Int(propertySize))
        defer { bufferListPointer.deallocate() }

        status = AudioObjectGetPropertyData(
            deviceID,
            &propertyAddress,
            0,
            nil,
            &propertySize,
            bufferListPointer
        )

        guard status == noErr else { return 0 }

        let bufferList = bufferListPointer.pointee
        var channelCount = 0

        let buffers = UnsafeMutableAudioBufferListPointer(UnsafeMutablePointer(mutating: bufferListPointer))
        for buffer in buffers {
            channelCount += Int(buffer.mNumberChannels)
        }

        return channelCount
    }

    /// Select BlackHole device if available
    private func selectBlackHoleDevice() {
        // Try to find BlackHole device
        if let blackholeDevice = availableDevices.first(where: {
            $0.name.lowercased().contains("blackhole")
        }) {
            selectedDeviceID = blackholeDevice.id
            selectedDeviceName = blackholeDevice.name
            return
        }

        // Fall back to first available device
        if let firstDevice = availableDevices.first {
            selectedDeviceID = firstDevice.id
            selectedDeviceName = firstDevice.name
        }
    }

    /// Switch to selected audio device
    func switchDevice() {
        let wasRunning = isRunning

        if wasRunning {
            stop()
        }

        if let device = availableDevices.first(where: { $0.id == selectedDeviceID }) {
            selectedDeviceName = device.name
            setSystemInputDevice(deviceID: selectedDeviceID)
        }

        if wasRunning {
            start()
        }
    }

    /// Set the system default input device
    private func setSystemInputDevice(deviceID: AudioDeviceID) {
        var deviceID = deviceID
        var propertyAddress = AudioObjectPropertyAddress(
            mSelector: kAudioHardwarePropertyDefaultInputDevice,
            mScope: kAudioObjectPropertyScopeGlobal,
            mElement: kAudioObjectPropertyElementMain
        )

        AudioObjectSetPropertyData(
            AudioObjectID(kAudioObjectSystemObject),
            &propertyAddress,
            0,
            nil,
            UInt32(MemoryLayout<AudioDeviceID>.size),
            &deviceID
        )
    }

    // MARK: - Audio Engine Control

    /// Start audio capture
    func start() {
        guard !isRunning else { return }

        do {
            audioEngine = AVAudioEngine()
            guard let engine = audioEngine else { return }

            inputNode = engine.inputNode
            guard let input = inputNode else { return }

            let format = input.outputFormat(forBus: 0)

            // Install tap on input node to capture audio
            input.installTap(onBus: 0, bufferSize: 1024, format: format) { [weak self] buffer, _ in
                self?.processAudioBuffer(buffer)
            }

            try engine.start()
            isRunning = true

            print("Audio engine started - capturing from: \(selectedDeviceName)")
            print("Format: \(format)")

        } catch {
            print("Failed to start audio engine: \(error)")
            isRunning = false
        }
    }

    /// Stop audio capture
    func stop() {
        guard isRunning else { return }

        inputNode?.removeTap(onBus: 0)
        audioEngine?.stop()
        audioEngine = nil
        inputNode = nil
        isRunning = false

        // Reset levels
        DispatchQueue.main.async {
            self.leftLevel = 0
            self.rightLevel = 0
            self.leftLevelDB = -60
            self.rightLevelDB = -60
        }

        print("Audio engine stopped")
    }

    /// Reset peak indicators
    func resetPeaks() {
        DispatchQueue.main.async {
            self.leftPeak = 0
            self.rightPeak = 0
        }
    }

    // MARK: - Audio Processing

    /// Process incoming audio buffer
    private func processAudioBuffer(_ buffer: AVAudioPCMBuffer) {
        guard let floatData = buffer.floatChannelData else { return }

        let frameCount = Int(buffer.frameLength)
        let channelCount = Int(buffer.format.channelCount)

        var leftRMS: Float = 0
        var rightRMS: Float = 0

        // Calculate RMS for left channel
        let leftChannel = floatData[0]
        var leftSum: Float = 0
        for i in 0..<frameCount {
            let sample = leftChannel[i]
            leftSum += sample * sample
        }
        leftRMS = sqrt(leftSum / Float(frameCount))

        // Calculate RMS for right channel (or use left if mono)
        if channelCount > 1 {
            let rightChannel = floatData[1]
            var rightSum: Float = 0
            for i in 0..<frameCount {
                let sample = rightChannel[i]
                rightSum += sample * sample
            }
            rightRMS = sqrt(rightSum / Float(frameCount))
        } else {
            rightRMS = leftRMS
        }

        // Convert to dB
        let leftDB = 20 * log10(max(leftRMS, 1e-10))
        let rightDB = 20 * log10(max(rightRMS, 1e-10))

        // Normalize to 0-1 range (assuming -60dB is silence)
        let minDB: Float = -60
        let maxDB: Float = 0

        let normalizedLeft = Double(max(0, min(1, (leftDB - minDB) / (maxDB - minDB))))
        let normalizedRight = Double(max(0, min(1, (rightDB - minDB) / (maxDB - minDB))))

        // Apply smoothing
        let smoothedLeft = previousLeftLevel * (1 - levelSmoothingFactor) + normalizedLeft * levelSmoothingFactor
        let smoothedRight = previousRightLevel * (1 - levelSmoothingFactor) + normalizedRight * levelSmoothingFactor

        previousLeftLevel = smoothedLeft
        previousRightLevel = smoothedRight

        // Update UI on main thread
        DispatchQueue.main.async {
            self.leftLevel = smoothedLeft
            self.rightLevel = smoothedRight
            self.leftLevelDB = Double(leftDB)
            self.rightLevelDB = Double(rightDB)

            // Update peaks
            if smoothedLeft > self.leftPeak {
                self.leftPeak = smoothedLeft
                self.schedulePeakReset(channel: 0)
            }
            if smoothedRight > self.rightPeak {
                self.rightPeak = smoothedRight
                self.schedulePeakReset(channel: 1)
            }
        }
    }

    /// Schedule peak reset after hold time
    private func schedulePeakReset(channel: Int) {
        // Cancel existing timer for this channel
        if channel < peakResetTimers.count {
            peakResetTimers[channel].invalidate()
        }

        let timer = Timer.scheduledTimer(withTimeInterval: peakHoldTime, repeats: false) { [weak self] _ in
            DispatchQueue.main.async {
                if channel == 0 {
                    self?.leftPeak = self?.leftLevel ?? 0
                } else {
                    self?.rightPeak = self?.rightLevel ?? 0
                }
            }
        }

        if peakResetTimers.count > channel {
            peakResetTimers[channel] = timer
        } else {
            peakResetTimers.append(timer)
        }
    }
}
