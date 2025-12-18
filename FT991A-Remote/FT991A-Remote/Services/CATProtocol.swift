//
//  CATProtocol.swift
//  FT991A-Remote
//
//  CAT Protocol handler for FT-991A communication
//

import Foundation
import Combine

// MARK: - CAT Protocol

class CATProtocol: ObservableObject {

    // MARK: - Published Properties

    @Published var radioState = RadioState()
    @Published var isPolling = false
    @Published var lastCommandTime: Date?
    @Published var pendingCommands: Int = 0

    // Debug console
    @Published var commandHistory: [CommandLogEntry] = []

    // MARK: - Private Properties

    private let serialManager: SerialPortManager
    private var responseQueue: [CATResponse] = []
    private var pollingTimer: Timer?
    private var cancellables = Set<AnyCancellable>()

    private let commandQueue = DispatchQueue(label: "cat.command", qos: .userInitiated)
    private var commandSemaphore = DispatchSemaphore(value: 1)

    // Polling intervals
    private let fastPollInterval: TimeInterval = 0.1    // 100ms for meters
    private let slowPollInterval: TimeInterval = 0.5    // 500ms for frequency/mode

    // MARK: - Initialization

    init(serialManager: SerialPortManager) {
        self.serialManager = serialManager

        serialManager.onDataReceived = { [weak self] data in
            self?.handleReceivedData(data)
        }

        serialManager.onConnectionChanged = { [weak self] connected in
            if connected {
                self?.startPolling()
                self?.requestInitialState()
            } else {
                self?.stopPolling()
            }
        }
    }

    // MARK: - Command Sending

    func send(_ command: CATCommand) {
        serialManager.send(command)
        lastCommandTime = Date()

        // Log command
        let entry = CommandLogEntry(
            timestamp: Date(),
            direction: .sent,
            command: command.command,
            description: command.description
        )
        DispatchQueue.main.async {
            self.commandHistory.append(entry)
            if self.commandHistory.count > 500 {
                self.commandHistory.removeFirst(100)
            }
        }
    }

    func sendRaw(_ command: String) {
        let catCommand = CATCommand(command, description: "Manual: \(command)")
        send(catCommand)
    }

    // MARK: - Response Handling

    private func handleReceivedData(_ data: Data) {
        guard let responseString = String(data: data, encoding: .ascii) else { return }

        let response = CATResponse(rawData: responseString)

        // Log response
        let entry = CommandLogEntry(
            timestamp: Date(),
            direction: .received,
            command: response.rawData,
            description: parseResponseDescription(response)
        )
        DispatchQueue.main.async {
            self.commandHistory.append(entry)
        }

        // Update radio state
        updateState(from: response)
    }

    private func updateState(from response: CATResponse) {
        DispatchQueue.main.async {
            switch response.command {
            case "FA":
                if let freq = response.frequency {
                    self.radioState.vfoAFrequency = freq
                }
            case "FB":
                if let freq = response.frequency {
                    self.radioState.vfoBFrequency = freq
                }
            case "VS":
                if let vfo = response.vfo {
                    self.radioState.activeVFO = vfo
                }
            case "MD":
                if let mode = response.mode {
                    self.radioState.mode = mode
                }
            case "AG":
                if let level = response.levelValue {
                    self.radioState.afGain = level
                }
            case "RG":
                if let level = response.levelValue {
                    self.radioState.rfGain = level
                }
            case "SQ":
                if let level = response.levelValue {
                    self.radioState.squelch = level
                }
            case "MG":
                if let level = response.levelValue {
                    self.radioState.micGain = level
                }
            case "PC":
                if let power = response.levelValue {
                    self.radioState.power = power
                }
            case "SM":
                if let meter = response.sMeter {
                    self.radioState.sMeter = meter
                }
            case "RM":
                // RM1 = power, RM6 = SWR
                if let level = response.levelValue {
                    if response.value.hasPrefix("1") {
                        self.radioState.powerMeter = level
                    } else if response.value.hasPrefix("6") {
                        self.radioState.swrMeter = level
                    }
                }
            case "NB":
                if let enabled = response.boolValue {
                    self.radioState.noiseBlanker = enabled
                }
            case "NR":
                if let enabled = response.boolValue {
                    self.radioState.noiseReduction = enabled
                }
            case "BC":
                if let enabled = response.boolValue {
                    self.radioState.dnf = enabled
                }
            case "FT":
                if let enabled = response.boolValue {
                    self.radioState.split = enabled
                }
            case "TX":
                if response.value == "0" {
                    self.radioState.isTransmitting = false
                } else if response.value == "1" || response.value == "2" {
                    self.radioState.isTransmitting = true
                }
            default:
                break
            }
        }
    }

    private func parseResponseDescription(_ response: CATResponse) -> String {
        switch response.command {
        case "FA":
            if let freq = response.frequency {
                return "VFO-A: \(radioState.formatFrequency(freq)) Hz"
            }
        case "FB":
            if let freq = response.frequency {
                return "VFO-B: \(radioState.formatFrequency(freq)) Hz"
            }
        case "MD":
            if let mode = response.mode {
                return "Mode: \(mode.rawValue)"
            }
        case "SM":
            if let meter = response.sMeter {
                return "S-Meter: \(meter)"
            }
        case "ID":
            if response.isFT991A {
                return "FT-991A identified"
            }
        default:
            break
        }
        return response.value
    }

    // MARK: - Polling

    func startPolling() {
        guard !isPolling else { return }
        isPolling = true

        // Fast polling for meters
        pollingTimer = Timer.scheduledTimer(withTimeInterval: fastPollInterval, repeats: true) { [weak self] _ in
            self?.pollMeters()
        }

        // Start slow polling for frequency/mode
        Timer.scheduledTimer(withTimeInterval: slowPollInterval, repeats: true) { [weak self] _ in
            self?.pollStatus()
        }
    }

    func stopPolling() {
        pollingTimer?.invalidate()
        pollingTimer = nil
        isPolling = false
    }

    private func pollMeters() {
        send(CAT.readSMeter)
        if radioState.isTransmitting {
            send(CAT.readPowerMeter)
            send(CAT.readSWRMeter)
        }
    }

    private func pollStatus() {
        send(CAT.readVFOA)
        send(CAT.readVFOB)
        send(CAT.readActiveVFO)
        send(CAT.readMode)
    }

    // MARK: - Initial State

    private func requestInitialState() {
        // Verify radio identity
        send(CAT.readID)

        // Request all current values
        send(CAT.readVFOA)
        send(CAT.readVFOB)
        send(CAT.readActiveVFO)
        send(CAT.readMode)
        send(CAT.readAFGain)
        send(CAT.readRFGain)
        send(CAT.readSquelch)
        send(CAT.readMICGain)
        send(CAT.readPower)
        send(CAT.readNB)
        send(CAT.readNR)
        send(CAT.readDNF)
        send(CAT.readSplit)
        send(CAT.readSMeter)
    }

    // MARK: - Radio Control

    func setFrequency(_ frequency: Int, vfo: VFO = .a) {
        if vfo == .a {
            send(CAT.setVFOA(frequency))
            radioState.vfoAFrequency = frequency
        } else {
            send(CAT.setVFOB(frequency))
            radioState.vfoBFrequency = frequency
        }
    }

    func changeFrequency(by step: Int) {
        let newFreq = radioState.activeFrequency + step
        setFrequency(newFreq, vfo: radioState.activeVFO)
    }

    func setMode(_ mode: OperatingMode) {
        send(CAT.setMode(mode))
        radioState.mode = mode
    }

    func setAFGain(_ value: Int) {
        send(CAT.setAFGain(value))
        radioState.afGain = value
    }

    func setRFGain(_ value: Int) {
        send(CAT.setRFGain(value))
        radioState.rfGain = value
    }

    func setSquelch(_ value: Int) {
        send(CAT.setSquelch(value))
        radioState.squelch = value
    }

    func setMICGain(_ value: Int) {
        send(CAT.setMICGain(value))
        radioState.micGain = value
    }

    func setPower(_ value: Int) {
        send(CAT.setPower(value))
        radioState.power = value
    }

    func toggleNB() {
        let newValue = !radioState.noiseBlanker
        send(CAT.setNB(newValue))
        radioState.noiseBlanker = newValue
    }

    func toggleNR() {
        let newValue = !radioState.noiseReduction
        send(CAT.setNR(newValue))
        radioState.noiseReduction = newValue
    }

    func toggleDNF() {
        let newValue = !radioState.dnf
        send(CAT.setDNF(newValue))
        radioState.dnf = newValue
    }

    func toggleSplit() {
        let newValue = !radioState.split
        send(CAT.setSplit(newValue))
        radioState.split = newValue
    }

    func selectVFO(_ vfo: VFO) {
        if vfo == .a {
            send(CAT.selectVFOA)
        } else {
            send(CAT.selectVFOB)
        }
        radioState.activeVFO = vfo
    }

    func swapVFO() {
        send(CAT.swapVFO)
        let temp = radioState.vfoAFrequency
        radioState.vfoAFrequency = radioState.vfoBFrequency
        radioState.vfoBFrequency = temp
    }

    func equalizeVFO() {
        // Set VFO-B to VFO-A frequency
        send(CAT.setVFOB(radioState.vfoAFrequency))
        radioState.vfoBFrequency = radioState.vfoAFrequency
    }

    func startATUTune() {
        send(CAT.startATUTune)
    }

    // MARK: - PTT Control

    func startTransmit(dataMode: Bool = false) {
        if dataMode {
            send(CAT.txOnData)
        } else {
            send(CAT.txOn)
        }
        radioState.isTransmitting = true
    }

    func stopTransmit() {
        send(CAT.txOff)
        radioState.isTransmitting = false
    }

    func toggleTransmit(dataMode: Bool = false) {
        if radioState.isTransmitting {
            stopTransmit()
        } else {
            startTransmit(dataMode: dataMode)
        }
    }

    // MARK: - Band Selection

    func selectBand(_ band: Band) {
        setFrequency(band.defaultFrequency, vfo: radioState.activeVFO)
    }

    // MARK: - Debug

    func clearCommandHistory() {
        commandHistory.removeAll()
    }
}

// MARK: - Command Log Entry

struct CommandLogEntry: Identifiable {
    let id = UUID()
    let timestamp: Date
    let direction: Direction
    let command: String
    let description: String

    enum Direction {
        case sent
        case received

        var symbol: String {
            switch self {
            case .sent: return "→"
            case .received: return "←"
            }
        }

        var color: String {
            switch self {
            case .sent: return "blue"
            case .received: return "green"
            }
        }
    }

    var timeString: String {
        let formatter = DateFormatter()
        formatter.dateFormat = "HH:mm:ss.SSS"
        return formatter.string(from: timestamp)
    }
}
