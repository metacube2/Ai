//
//  AudioInputManager.swift
//  PsytranceVisualizer
//
//  Manages audio input devices and captures audio buffers
//

import AVFoundation
import CoreAudio
import Combine

/// Represents an audio input device
struct AudioDevice: Identifiable, Hashable {
    let id: AudioDeviceID
    let uid: String
    let name: String
    let manufacturer: String
    let isInput: Bool

    func hash(into hasher: inout Hasher) {
        hasher.combine(uid)
    }

    static func == (lhs: AudioDevice, rhs: AudioDevice) -> Bool {
        lhs.uid == rhs.uid
    }
}

/// Manages audio input capture using AVAudioEngine
final class AudioInputManager: ObservableObject {
    // MARK: - Published Properties

    @Published private(set) var availableDevices: [AudioDevice] = []
    @Published private(set) var selectedDevice: AudioDevice?
    @Published private(set) var isRunning = false
    @Published private(set) var currentBufferSize: Int = 1024

    // MARK: - Audio Properties

    private var audioEngine: AVAudioEngine?
    private var inputNode: AVAudioInputNode?
    private let sampleRate: Double = 44100.0

    // MARK: - Callbacks

    var onAudioBuffer: ((AVAudioPCMBuffer) -> Void)?

    // MARK: - Private Properties

    private var deviceListenerBlock: AudioObjectPropertyListenerBlock?
    private let processingQueue = DispatchQueue(label: "com.psytrance.audio", qos: .userInteractive)

    // MARK: - Initialization

    init() {
        refreshDeviceList()
        setupDeviceChangeListener()
    }

    deinit {
        stop()
        removeDeviceChangeListener()
    }

    // MARK: - Public Methods

    /// Returns list of available audio input devices
    func getAvailableInputDevices() -> [AudioDevice] {
        return availableDevices
    }

    /// Refreshes the list of available audio input devices
    func refreshDeviceList() {
        var propertyAddress = AudioObjectPropertyAddress(
            mSelector: kAudioHardwarePropertyDevices,
            mScope: kAudioObjectPropertyScopeGlobal,
            mElement: kAudioObjectPropertyElementMain
        )

        var dataSize: UInt32 = 0
        var status = AudioObjectGetPropertyDataSize(
            AudioObjectID(kAudioObjectSystemObject),
            &propertyAddress,
            0,
            nil,
            &dataSize
        )

        guard status == noErr else {
            print("[AudioInputManager] Failed to get device list size: \(status)")
            return
        }

        let deviceCount = Int(dataSize) / MemoryLayout<AudioDeviceID>.size
        var deviceIDs = [AudioDeviceID](repeating: 0, count: deviceCount)

        status = AudioObjectGetPropertyData(
            AudioObjectID(kAudioObjectSystemObject),
            &propertyAddress,
            0,
            nil,
            &dataSize,
            &deviceIDs
        )

        guard status == noErr else {
            print("[AudioInputManager] Failed to get device list: \(status)")
            return
        }

        var devices: [AudioDevice] = []

        for deviceID in deviceIDs {
            if let device = getDeviceInfo(deviceID: deviceID), device.isInput {
                devices.append(device)
            }
        }

        DispatchQueue.main.async {
            self.availableDevices = devices
            print("[AudioInputManager] Found \(devices.count) input devices")
        }
    }

    /// Selects an audio input device by UID
    func selectDevice(uid: String) {
        guard let device = availableDevices.first(where: { $0.uid == uid }) else {
            print("[AudioInputManager] Device not found: \(uid)")
            return
        }

        let wasRunning = isRunning
        if wasRunning {
            stop()
        }

        selectedDevice = device
        setSystemInputDevice(deviceID: device.id)

        if wasRunning {
            start()
        }

        print("[AudioInputManager] Selected device: \(device.name)")
    }

    /// Sets the buffer size (512 or 1024)
    func setBufferSize(_ size: Int) {
        guard [512, 1024].contains(size) else {
            print("[AudioInputManager] Invalid buffer size: \(size)")
            return
        }

        let wasRunning = isRunning
        if wasRunning {
            stop()
        }

        currentBufferSize = size

        if wasRunning {
            start()
        }

        print("[AudioInputManager] Buffer size set to: \(size)")
    }

    /// Starts audio capture
    func start() {
        guard !isRunning else { return }

        do {
            // Create new audio engine
            audioEngine = AVAudioEngine()
            guard let engine = audioEngine else { return }

            inputNode = engine.inputNode

            guard let inputNode = inputNode else {
                print("[AudioInputManager] No input node available")
                return
            }

            // Get the input format
            let inputFormat = inputNode.outputFormat(forBus: 0)

            print("[AudioInputManager] Input format: \(inputFormat)")

            // Install tap on input node
            let bufferSize = AVAudioFrameCount(currentBufferSize)

            inputNode.installTap(onBus: 0, bufferSize: bufferSize, format: inputFormat) { [weak self] buffer, _ in
                self?.processingQueue.async {
                    self?.onAudioBuffer?(buffer)
                }
            }

            // Prepare and start the engine
            engine.prepare()
            try engine.start()

            DispatchQueue.main.async {
                self.isRunning = true
            }

            print("[AudioInputManager] Audio capture started")

        } catch {
            print("[AudioInputManager] Failed to start audio capture: \(error)")
        }
    }

    /// Stops audio capture
    func stop() {
        guard isRunning else { return }

        inputNode?.removeTap(onBus: 0)
        audioEngine?.stop()
        audioEngine = nil
        inputNode = nil

        DispatchQueue.main.async {
            self.isRunning = false
        }

        print("[AudioInputManager] Audio capture stopped")
    }

    // MARK: - Private Methods

    /// Gets device info for a specific device ID
    private func getDeviceInfo(deviceID: AudioDeviceID) -> AudioDevice? {
        // Check if device has input channels
        var propertyAddress = AudioObjectPropertyAddress(
            mSelector: kAudioDevicePropertyStreamConfiguration,
            mScope: kAudioDevicePropertyScopeInput,
            mElement: kAudioObjectPropertyElementMain
        )

        var dataSize: UInt32 = 0
        var status = AudioObjectGetPropertyDataSize(deviceID, &propertyAddress, 0, nil, &dataSize)

        guard status == noErr, dataSize > 0 else { return nil }

        let bufferListPointer = UnsafeMutablePointer<AudioBufferList>.allocate(capacity: Int(dataSize))
        defer { bufferListPointer.deallocate() }

        status = AudioObjectGetPropertyData(deviceID, &propertyAddress, 0, nil, &dataSize, bufferListPointer)

        guard status == noErr else { return nil }

        let bufferList = UnsafeMutableAudioBufferListPointer(bufferListPointer)
        var inputChannelCount: UInt32 = 0
        for buffer in bufferList {
            inputChannelCount += buffer.mNumberChannels
        }

        guard inputChannelCount > 0 else { return nil }

        // Get device UID
        var uid: CFString = "" as CFString
        var uidSize = UInt32(MemoryLayout<CFString>.size)
        propertyAddress.mSelector = kAudioDevicePropertyDeviceUID
        propertyAddress.mScope = kAudioObjectPropertyScopeGlobal

        status = AudioObjectGetPropertyData(deviceID, &propertyAddress, 0, nil, &uidSize, &uid)
        guard status == noErr else { return nil }

        // Get device name
        var name: CFString = "" as CFString
        var nameSize = UInt32(MemoryLayout<CFString>.size)
        propertyAddress.mSelector = kAudioDevicePropertyDeviceNameCFString

        status = AudioObjectGetPropertyData(deviceID, &propertyAddress, 0, nil, &nameSize, &name)
        guard status == noErr else { return nil }

        // Get manufacturer
        var manufacturer: CFString = "" as CFString
        var manufacturerSize = UInt32(MemoryLayout<CFString>.size)
        propertyAddress.mSelector = kAudioDevicePropertyDeviceManufacturerCFString

        AudioObjectGetPropertyData(deviceID, &propertyAddress, 0, nil, &manufacturerSize, &manufacturer)

        return AudioDevice(
            id: deviceID,
            uid: uid as String,
            name: name as String,
            manufacturer: manufacturer as String,
            isInput: true
        )
    }

    /// Sets the system default input device
    private func setSystemInputDevice(deviceID: AudioDeviceID) {
        var deviceIDCopy = deviceID
        var propertyAddress = AudioObjectPropertyAddress(
            mSelector: kAudioHardwarePropertyDefaultInputDevice,
            mScope: kAudioObjectPropertyScopeGlobal,
            mElement: kAudioObjectPropertyElementMain
        )

        let status = AudioObjectSetPropertyData(
            AudioObjectID(kAudioObjectSystemObject),
            &propertyAddress,
            0,
            nil,
            UInt32(MemoryLayout<AudioDeviceID>.size),
            &deviceIDCopy
        )

        if status != noErr {
            print("[AudioInputManager] Failed to set input device: \(status)")
        }
    }

    /// Sets up listener for device changes
    private func setupDeviceChangeListener() {
        var propertyAddress = AudioObjectPropertyAddress(
            mSelector: kAudioHardwarePropertyDevices,
            mScope: kAudioObjectPropertyScopeGlobal,
            mElement: kAudioObjectPropertyElementMain
        )

        deviceListenerBlock = { [weak self] _, _ in
            DispatchQueue.main.async {
                self?.refreshDeviceList()
            }
        }

        if let block = deviceListenerBlock {
            AudioObjectAddPropertyListenerBlock(
                AudioObjectID(kAudioObjectSystemObject),
                &propertyAddress,
                DispatchQueue.main,
                block
            )
        }
    }

    /// Removes device change listener
    private func removeDeviceChangeListener() {
        guard let block = deviceListenerBlock else { return }

        var propertyAddress = AudioObjectPropertyAddress(
            mSelector: kAudioHardwarePropertyDevices,
            mScope: kAudioObjectPropertyScopeGlobal,
            mElement: kAudioObjectPropertyElementMain
        )

        AudioObjectRemovePropertyListenerBlock(
            AudioObjectID(kAudioObjectSystemObject),
            &propertyAddress,
            DispatchQueue.main,
            block
        )
    }
}
