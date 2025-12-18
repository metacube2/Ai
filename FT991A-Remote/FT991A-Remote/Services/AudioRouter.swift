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
}
