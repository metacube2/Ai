//
//  AudioRouter.swift
//  FT991A-Remote
//
//  BlackHole audio routing integration for digital modes
//

import Foundation
import AVFoundation

// MARK: - Audio Device

struct AudioDevice: Identifiable, Hashable {
    let id: AudioDeviceID
    let name: String
    let uid: String
    let isInput: Bool
    let isOutput: Bool
    let isBlackHole: Bool

    var displayName: String {
        if isBlackHole {
            return "\(name) (Virtual)"
        }
        return name
    }
}

// MARK: - Audio Router

class AudioRouter: ObservableObject {

    // MARK: - Published Properties

    @Published var inputDevices: [AudioDevice] = []
    @Published var outputDevices: [AudioDevice] = []

    @Published var selectedInputDevice: AudioDeviceID?
    @Published var selectedOutputDevice: AudioDeviceID?

    @Published var blackHoleDevice: AudioDevice?
    @Published var ft991aDevice: AudioDevice?

    @Published var isBlackHoleInstalled = false
    @Published var lastError: String?

    // MARK: - Initialization

    init() {
        refreshDevices()
    }

    // MARK: - Device Discovery

    func refreshDevices() {
        inputDevices = []
        outputDevices = []

        var propertyAddress = AudioObjectPropertyAddress(
            mSelector: kAudioHardwarePropertyDevices,
            mScope: kAudioObjectPropertyScopeGlobal,
            mElement: kAudioObjectPropertyElementMain
        )

        var dataSize: UInt32 = 0
        var status = AudioObjectGetPropertyDataSize(
            AudioObjectID(kAudioObjectSystemObject),
            &propertyAddress,
            0, nil,
            &dataSize
        )

        guard status == noErr else {
            lastError = "Fehler beim Abrufen der Audio-Geräte"
            return
        }

        let deviceCount = Int(dataSize) / MemoryLayout<AudioDeviceID>.size
        var deviceIDs = [AudioDeviceID](repeating: 0, count: deviceCount)

        status = AudioObjectGetPropertyData(
            AudioObjectID(kAudioObjectSystemObject),
            &propertyAddress,
            0, nil,
            &dataSize,
            &deviceIDs
        )

        guard status == noErr else {
            lastError = "Fehler beim Laden der Audio-Geräte"
            return
        }

        for deviceID in deviceIDs {
            if let device = createAudioDevice(from: deviceID) {
                if device.isInput {
                    inputDevices.append(device)
                }
                if device.isOutput {
                    outputDevices.append(device)
                }

                // Detect BlackHole
                if device.isBlackHole && blackHoleDevice == nil {
                    blackHoleDevice = device
                    isBlackHoleInstalled = true
                }

                // Detect FT-991A (usually shows as "USB Audio CODEC")
                if device.name.contains("USB Audio") || device.name.contains("FT-991") {
                    ft991aDevice = device
                }
            }
        }

        Logger.shared.log("Found \(inputDevices.count) input and \(outputDevices.count) output devices", level: .debug)

        if isBlackHoleInstalled {
            Logger.shared.log("BlackHole detected: \(blackHoleDevice?.name ?? "Unknown")", level: .info)
        }
    }

    private func createAudioDevice(from deviceID: AudioDeviceID) -> AudioDevice? {
        // Get device name
        var name: CFString = "" as CFString
        var nameSize = UInt32(MemoryLayout<CFString>.size)
        var propertyAddress = AudioObjectPropertyAddress(
            mSelector: kAudioDevicePropertyDeviceNameCFString,
            mScope: kAudioObjectPropertyScopeGlobal,
            mElement: kAudioObjectPropertyElementMain
        )

        var status = AudioObjectGetPropertyData(deviceID, &propertyAddress, 0, nil, &nameSize, &name)
        guard status == noErr else { return nil }

        // Get device UID
        var uid: CFString = "" as CFString
        var uidSize = UInt32(MemoryLayout<CFString>.size)
        propertyAddress.mSelector = kAudioDevicePropertyDeviceUID

        status = AudioObjectGetPropertyData(deviceID, &propertyAddress, 0, nil, &uidSize, &uid)
        let deviceUID = status == noErr ? uid as String : ""

        // Check for input channels
        var inputSize: UInt32 = 0
        propertyAddress.mSelector = kAudioDevicePropertyStreamConfiguration
        propertyAddress.mScope = kAudioDevicePropertyScopeInput

        _ = AudioObjectGetPropertyDataSize(deviceID, &propertyAddress, 0, nil, &inputSize)
        let hasInput = inputSize > 0

        // Check for output channels
        var outputSize: UInt32 = 0
        propertyAddress.mScope = kAudioDevicePropertyScopeOutput

        _ = AudioObjectGetPropertyDataSize(deviceID, &propertyAddress, 0, nil, &outputSize)
        let hasOutput = outputSize > 0

        let deviceName = name as String
        let isBlackHole = deviceName.lowercased().contains("blackhole")

        return AudioDevice(
            id: deviceID,
            name: deviceName,
            uid: deviceUID,
            isInput: hasInput,
            isOutput: hasOutput,
            isBlackHole: isBlackHole
        )
    }

    // MARK: - Device Selection

    func selectInputDevice(_ device: AudioDevice) {
        selectedInputDevice = device.id
        Logger.shared.log("Selected input device: \(device.name)", level: .info)
    }

    func selectOutputDevice(_ device: AudioDevice) {
        selectedOutputDevice = device.id
        Logger.shared.log("Selected output device: \(device.name)", level: .info)
    }

    func inputDevice(forUID uid: String) -> AudioDevice? {
        inputDevices.first(where: { $0.uid == uid })
    }

    func outputDevice(forUID uid: String) -> AudioDevice? {
        outputDevices.first(where: { $0.uid == uid })
    }

    // MARK: - BlackHole Setup

    func configureForDigitalModes() -> Bool {
        guard isBlackHoleInstalled, let blackHole = blackHoleDevice else {
            lastError = "BlackHole ist nicht installiert"
            return false
        }

        // Route: FT-991A USB Audio → BlackHole → Digital Mode App
        // Route back: Digital Mode App → BlackHole → FT-991A USB Audio

        if let ft991a = ft991aDevice {
            selectedInputDevice = ft991a.id   // FT-991A as input (RX audio)
            selectedOutputDevice = blackHole.id // BlackHole as output (to digital mode app)

            Logger.shared.log("Configured for digital modes: \(ft991a.name) → \(blackHole.name)", level: .info)
            return true
        } else {
            lastError = "FT-991A Audio-Gerät nicht gefunden"
            return false
        }
    }

    // MARK: - System Audio

    func setSystemDefaultInput(_ deviceID: AudioDeviceID) {
        var propertyAddress = AudioObjectPropertyAddress(
            mSelector: kAudioHardwarePropertyDefaultInputDevice,
            mScope: kAudioObjectPropertyScopeGlobal,
            mElement: kAudioObjectPropertyElementMain
        )

        var deviceIDVar = deviceID
        let status = AudioObjectSetPropertyData(
            AudioObjectID(kAudioObjectSystemObject),
            &propertyAddress,
            0, nil,
            UInt32(MemoryLayout<AudioDeviceID>.size),
            &deviceIDVar
        )

        if status != noErr {
            lastError = "Fehler beim Setzen des Standard-Eingangs"
        }
    }

    func setSystemDefaultOutput(_ deviceID: AudioDeviceID) {
        var propertyAddress = AudioObjectPropertyAddress(
            mSelector: kAudioHardwarePropertyDefaultOutputDevice,
            mScope: kAudioObjectPropertyScopeGlobal,
            mElement: kAudioObjectPropertyElementMain
        )

        var deviceIDVar = deviceID
        let status = AudioObjectSetPropertyData(
            AudioObjectID(kAudioObjectSystemObject),
            &propertyAddress,
            0, nil,
            UInt32(MemoryLayout<AudioDeviceID>.size),
            &deviceIDVar
        )

        if status != noErr {
            lastError = "Fehler beim Setzen des Standard-Ausgangs"
        }
    }

    func currentDefaultInput() -> AudioDeviceID? {
        currentDefaultDevice(selector: kAudioHardwarePropertyDefaultInputDevice)
    }

    func currentDefaultOutput() -> AudioDeviceID? {
        currentDefaultDevice(selector: kAudioHardwarePropertyDefaultOutputDevice)
    }

    private func currentDefaultDevice(selector: AudioObjectPropertySelector) -> AudioDeviceID? {
        var propertyAddress = AudioObjectPropertyAddress(
            mSelector: selector,
            mScope: kAudioObjectPropertyScopeGlobal,
            mElement: kAudioObjectPropertyElementMain
        )

        var deviceID = AudioDeviceID(0)
        var size = UInt32(MemoryLayout<AudioDeviceID>.size)
        let status = AudioObjectGetPropertyData(
            AudioObjectID(kAudioObjectSystemObject),
            &propertyAddress,
            0, nil,
            &size,
            &deviceID
        )

        guard status == noErr else { return nil }
        return deviceID
    }
}

// MARK: - Parrot Audio Service

final class ParrotAudioService: NSObject, ObservableObject, AVAudioRecorderDelegate, AVAudioPlayerDelegate {
    @Published private(set) var inputDevices: [AudioDevice] = []
    @Published private(set) var outputDevices: [AudioDevice] = []
    @Published private(set) var isRecording = false
    @Published private(set) var isPlaying = false
    @Published private(set) var elapsedTime: TimeInterval = 0
    @Published var lastError: String?

    private let router = AudioRouter()
    private var recorder: AVAudioRecorder?
    private var player: AVAudioPlayer?
    private var progressTimer: Timer?
    private var pendingRecordCompletion: ((URL, TimeInterval) -> Void)?
    private var pendingPlaybackCompletion: ((Bool) -> Void)?
    private var recordingURL: URL?
    private var previousInputDevice: AudioDeviceID?
    private var previousOutputDevice: AudioDeviceID?

    override init() {
        super.init()
        refreshDevices()
    }

    func refreshDevices() {
        router.refreshDevices()
        inputDevices = router.inputDevices
        outputDevices = router.outputDevices
        lastError = router.lastError
    }

    func startRecording(inputDeviceUID: String?, completion: @escaping (URL, TimeInterval) -> Void) -> Bool {
        guard !isRecording, !isPlaying else {
            lastError = "Papagei ist bereits aktiv."
            return false
        }

        refreshDevices()
        previousInputDevice = router.currentDefaultInput()

        if let inputDeviceUID, !inputDeviceUID.isEmpty, let device = router.inputDevice(forUID: inputDeviceUID) {
            router.setSystemDefaultInput(device.id)
        }

        let url = Self.tempRecordingURL()
        let settings: [String: Any] = [
            AVFormatIDKey: kAudioFormatMPEG4AAC,
            AVSampleRateKey: 44_100,
            AVNumberOfChannelsKey: 1,
            AVEncoderAudioQualityKey: AVAudioQuality.high.rawValue
        ]

        do {
            try FileManager.default.createDirectory(at: url.deletingLastPathComponent(), withIntermediateDirectories: true)
            if FileManager.default.fileExists(atPath: url.path) {
                try FileManager.default.removeItem(at: url)
            }

            let recorder = try AVAudioRecorder(url: url, settings: settings)
            recorder.delegate = self
            self.recorder = recorder
            recordingURL = url
            pendingRecordCompletion = completion
            elapsedTime = 0
            isRecording = recorder.record(forDuration: ParrotStore.maximumDuration)

            guard isRecording else {
                restoreInputDevice()
                lastError = "Aufnahme konnte nicht gestartet werden."
                return false
            }

            startProgressTimer()
            Logger.shared.log("Parrot recording started", level: .info)
            return true
        } catch {
            restoreInputDevice()
            lastError = "Aufnahmefehler: \(error.localizedDescription)"
            return false
        }
    }

    func stopRecording() {
        guard isRecording else { return }
        recorder?.stop()
    }

    func play(url: URL, outputDeviceUID: String?, completion: @escaping (Bool) -> Void) -> Bool {
        guard !isRecording, !isPlaying else {
            lastError = "Papagei ist bereits aktiv."
            return false
        }

        refreshDevices()
        previousOutputDevice = router.currentDefaultOutput()

        if let outputDeviceUID, !outputDeviceUID.isEmpty, let device = router.outputDevice(forUID: outputDeviceUID) {
            router.setSystemDefaultOutput(device.id)
        }

        do {
            let player = try AVAudioPlayer(contentsOf: url)
            player.delegate = self
            player.prepareToPlay()
            self.player = player
            pendingPlaybackCompletion = completion
            elapsedTime = 0
            isPlaying = player.play()

            guard isPlaying else {
                restoreOutputDevice()
                lastError = "Wiedergabe konnte nicht gestartet werden."
                completion(false)
                return false
            }

            startProgressTimer()
            Logger.shared.log("Parrot playback started", level: .info)
            return true
        } catch {
            restoreOutputDevice()
            lastError = "Wiedergabefehler: \(error.localizedDescription)"
            completion(false)
            return false
        }
    }

    func stopPlayback() {
        guard isPlaying else { return }
        player?.stop()
        finishPlayback(successfully: false)
    }

    private func startProgressTimer() {
        progressTimer?.invalidate()
        progressTimer = Timer.scheduledTimer(withTimeInterval: 0.1, repeats: true) { [weak self] _ in
            guard let self else { return }
            if let recorder = self.recorder, self.isRecording {
                self.elapsedTime = min(recorder.currentTime, ParrotStore.maximumDuration)
            } else if let player = self.player, self.isPlaying {
                self.elapsedTime = player.currentTime
            }
        }
    }

    private func finishRecording(successfully: Bool) {
        progressTimer?.invalidate()
        progressTimer = nil
        isRecording = false
        let completedURL = recordingURL
        let duration = min(recorder?.currentTime ?? elapsedTime, ParrotStore.maximumDuration)
        recorder = nil
        recordingURL = nil
        restoreInputDevice()

        guard successfully, let completedURL else {
            pendingRecordCompletion = nil
            return
        }

        pendingRecordCompletion?(completedURL, duration)
        pendingRecordCompletion = nil
    }

    private func finishPlayback(successfully: Bool) {
        progressTimer?.invalidate()
        progressTimer = nil
        isPlaying = false
        player = nil
        restoreOutputDevice()
        pendingPlaybackCompletion?(successfully)
        pendingPlaybackCompletion = nil
    }

    private func restoreInputDevice() {
        guard let previousInputDevice else { return }
        router.setSystemDefaultInput(previousInputDevice)
        self.previousInputDevice = nil
    }

    private func restoreOutputDevice() {
        guard let previousOutputDevice else { return }
        router.setSystemDefaultOutput(previousOutputDevice)
        self.previousOutputDevice = nil
    }

    private static func tempRecordingURL() -> URL {
        let appSupport = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask).first!
        return appSupport
            .appendingPathComponent("FT991A-Remote", isDirectory: true)
            .appendingPathComponent("Parrot", isDirectory: true)
            .appendingPathComponent("recording-temp.m4a")
    }

    func audioRecorderDidFinishRecording(_ recorder: AVAudioRecorder, successfully flag: Bool) {
        finishRecording(successfully: flag)
    }

    func audioPlayerDidFinishPlaying(_ player: AVAudioPlayer, successfully flag: Bool) {
        finishPlayback(successfully: flag)
    }
}
