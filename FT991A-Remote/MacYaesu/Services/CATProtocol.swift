//
//  CATProtocol.swift
//  FT991A-Remote
//
//  CAT Protocol handler for FT-991A communication
//

import Foundation
import Combine

// MARK: - CAT Protocol

protocol CATProtocolServiceType: AnyObject {
    var radioState: RadioState { get set }
    var isPolling: Bool { get set }
    var lastCommandTime: Date? { get set }
    var pendingCommands: Int { get set }
    var commandHistory: [CommandLogEntry] { get set }
    var radioModel: RadioModel { get set }

    var radioStatePublisher: Published<RadioState>.Publisher { get }
    var commandHistoryPublisher: Published<[CommandLogEntry]>.Publisher { get }

    func ping()
    func setFrequency(_ frequency: Int, vfo: VFO)
    func changeFrequency(by step: Int)
    func setMode(_ mode: OperatingMode)
    func setAFGain(_ value: Int)
    func setRFGain(_ value: Int)
    func setSquelch(_ value: Int)
    func setMICGain(_ value: Int)
    func setPower(_ value: Int)
    func toggleNB()
    func toggleNR()
    func toggleDNF()
    func toggleContour()
    func setContourFrequency(_ value: Int)
    func toggleSplit()
    func toggleIPO()
    func setSplit(_ enabled: Bool)
    func setToneMode(_ mode: ToneMode)
    func setToneCode(index: Int, usesDCS: Bool)
    func selectVFO(_ vfo: VFO)
    func swapVFO()
    func equalizeVFO()
    func startATUTune()
    func startTransmit(dataMode: Bool)
    func stopTransmit()
    func toggleTransmit(dataMode: Bool)
    func selectBand(_ band: Band)
    func sendRaw(_ command: String)
    func clearCommandHistory()
}

class CATProtocol: ObservableObject, CATProtocolServiceType {

    // MARK: - Published Properties

    @Published var radioState = RadioState()
    @Published var isPolling = false
    @Published var lastCommandTime: Date?
    @Published var pendingCommands: Int = 0

    // Debug console
    @Published var commandHistory: [CommandLogEntry] = []
    @Published var radioModel: RadioModel = .ft991a

    // MARK: - Private Properties

    private let serialManager: any SerialPortServiceType
    private var responseQueue: [CATResponse] = []
    private var fastPollingTimer: Timer?
    private var slowPollingTimer: Timer?
    private var cancellables = Set<AnyCancellable>()

    private let commandQueue = DispatchQueue(label: "cat.command", qos: .userInitiated)
    private var commandSemaphore = DispatchSemaphore(value: 1)
    private var resumePollingWorkItem: DispatchWorkItem?
    private var meterPollPhase = 0
    private var statusPollPhase = 0

    // Polling intervals
    private let fastPollInterval: TimeInterval = 0.9
    private let slowPollInterval: TimeInterval = 1.4

    // MARK: - Initialization

    init(serialManager: any SerialPortServiceType) {
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

    var radioStatePublisher: Published<RadioState>.Publisher { $radioState }
    var commandHistoryPublisher: Published<[CommandLogEntry]>.Publisher { $commandHistory }

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

    private func prioritizeUserCommand(_ command: CATCommand) {
        pausePollingTemporarily()
        send(command)
    }

    func sendRaw(_ command: String) {
        let catCommand = CATCommand(command, description: "Manual: \(command)")
        send(catCommand)
    }

    func ping() {
        send(CAT.readID)
        send(CAT.readVFOA)
    }

    // MARK: - Response Handling

    private func handleReceivedData(_ data: Data) {
        guard let responseString = String(data: data, encoding: .ascii) else { return }

        let response = CATResponse(rawData: responseString)

        if response.isEchoOnly {
            Logger.shared.log("Ignoring CAT echo: \(response.rawData)", level: .debug)
            Logger.shared.catTrace("RX class=echo value=\(response.rawData)")
            return
        }

        if response.isOverflowMessage {
            Logger.shared.log("Radio reports CAT overflow", level: .warning)
            Logger.shared.catTrace("RX class=overflow value=\(response.rawData)")
        } else {
            serialManager.noteCATResponse()
            Logger.shared.catTrace("RX class=valid cmd=\(response.command) value=\(response.value)")
        }

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
            case "MD":
                if let mode = response.mode(for: self.radioModel) {
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
            case "CT":
                if let toneMode = response.toneMode(for: self.radioModel) {
                    self.radioState.toneMode = toneMode
                }
            case "CN":
                if let kind = response.toneCodeKind, let index = response.toneCodeIndex {
                    if kind == 0 {
                        self.radioState.ctcssToneIndex = index
                    } else if kind == 1 {
                        self.radioState.dcsCodeIndex = index
                    }
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
            case "NL":
                if let level = response.levelValue {
                    self.radioState.noiseBlanker = level > 0
                }
            case "NR":
                if let enabled = response.boolValue {
                    self.radioState.noiseReduction = enabled
                }
            case "RL":
                if let level = response.levelValue {
                    self.radioState.noiseReduction = level > 0
                }
            case "BC":
                if let enabled = response.boolValue {
                    self.radioState.dnf = enabled
                }
            case "CO":
                if let enabled = response.contourEnabled {
                    self.radioState.contour = enabled
                }
                if let frequency = response.contourFrequency {
                    self.radioState.contourFrequency = frequency
                }
            case "AC":
                if let enabled = response.boolValue {
                    self.radioState.atu = enabled
                }
            case "FT":
                if self.radioModel == .ft991a, let last = response.value.last {
                    self.radioState.split = (last == "3" || last == "1")
                }
            case "ST":
                if let enabled = response.boolValue {
                    self.radioState.split = enabled
                }
            case "PA":
                if let enabled = response.ipoEnabled {
                    self.radioState.ipo = enabled
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
            if let mode = response.mode(for: radioModel) {
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
            if response.isFTX1 {
                return "FTX-1 identified"
            }
        case "CT":
            if let toneMode = response.toneMode(for: radioModel) {
                return "Tone: \(toneMode.rawValue)"
            }
        case "CN":
            if let kind = response.toneCodeKind, let index = response.toneCodeIndex {
                return kind == 0 ? "CTCSS Index \(index)" : "DCS Index \(index)"
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
        fastPollingTimer = Timer.scheduledTimer(withTimeInterval: fastPollInterval, repeats: true) { [weak self] _ in
            self?.pollMeters()
        }

        // Start slow polling for frequency/mode
        slowPollingTimer = Timer.scheduledTimer(withTimeInterval: slowPollInterval, repeats: true) { [weak self] _ in
            self?.pollStatus()
        }
    }

    func stopPolling() {
        fastPollingTimer?.invalidate()
        fastPollingTimer = nil
        slowPollingTimer?.invalidate()
        slowPollingTimer = nil
        isPolling = false
    }

    private func pollMeters() {
        if radioState.isTransmitting {
            if meterPollPhase % 2 == 0 {
                send(CAT.readPowerMeter)
            } else {
                send(CAT.readSWRMeter)
            }
        } else {
            send(CAT.readSMeter)
        }
        meterPollPhase += 1
    }

    private func pollStatus() {
        let commands: [CATCommand] = [
            CAT.readVFOA,
            CAT.readVFOB,
            CAT.readMode(for: radioModel),
            CAT.readContour(for: radioModel),
            CAT.readContourFrequency(for: radioModel),
            CAT.readSplit(for: radioModel),
            CAT.readTXStatus,
            CAT.readToneMode(for: radioModel),
            CAT.readToneCode(usesDCS: radioState.toneMode.usesDCS, model: radioModel)
        ]
        let availableCommands = commands
            + (radioModel.capabilities.showsATUTune ? [CAT.readATU(for: radioModel)] : [])
            + (radioModel.capabilities.showsIPOToggle ? [CAT.readIPO(for: radioModel)] : [])
            + (radioModel.capabilities.showsNoiseBlanker ? [CAT.readNB(for: radioModel)] : [])
            + (radioModel.capabilities.showsNoiseReduction ? [CAT.readNR(for: radioModel)] : [])
        send(availableCommands[statusPollPhase % availableCommands.count])
        statusPollPhase += 1
    }

    // MARK: - Initial State

    private func requestInitialState() {
        let startupCommands: [CATCommand] = [
            CAT.readID,
            CAT.readVFOA,
            CAT.readMode(for: radioModel),
            CAT.readAFGain(for: radioModel),
            CAT.readContour(for: radioModel),
            CAT.readContourFrequency(for: radioModel),
            CAT.readSplit(for: radioModel),
            CAT.readToneMode(for: radioModel),
            CAT.readToneCode(usesDCS: false, model: radioModel),
            CAT.readToneCode(usesDCS: true, model: radioModel),
        ] + (radioModel.capabilities.showsATUTune ? [CAT.readATU(for: radioModel)] : [])
          + (radioModel.capabilities.showsIPOToggle ? [CAT.readIPO(for: radioModel)] : [])
          + (radioModel.capabilities.showsNoiseBlanker ? [CAT.readNB(for: radioModel)] : [])
          + (radioModel.capabilities.showsNoiseReduction ? [CAT.readNR(for: radioModel)] : [])

        for (index, command) in startupCommands.enumerated() {
            let delay = DispatchTime.now() + .milliseconds(index * 220)
            commandQueue.asyncAfter(deadline: delay) { [weak self] in
                self?.send(command)
            }
        }
    }

    // MARK: - Radio Control

    func setFrequency(_ frequency: Int, vfo: VFO = .a) {
        if vfo == .a {
            prioritizeUserCommand(CAT.setVFOA(frequency))
            radioState.vfoAFrequency = frequency
        } else {
            prioritizeUserCommand(CAT.setVFOB(frequency))
            radioState.vfoBFrequency = frequency
        }
    }

    func changeFrequency(by step: Int) {
        let newFreq = radioState.activeFrequency + step
        setFrequency(newFreq, vfo: radioState.activeVFO)
    }

    func setMode(_ mode: OperatingMode) {
        prioritizeUserCommand(CAT.setMode(mode, model: radioModel))
        radioState.mode = mode
    }

    func setAFGain(_ value: Int) {
        prioritizeUserCommand(CAT.setAFGain(value, model: radioModel))
        radioState.afGain = value
    }

    func setRFGain(_ value: Int) {
        prioritizeUserCommand(CAT.setRFGain(value, model: radioModel))
        radioState.rfGain = value
    }

    func setSquelch(_ value: Int) {
        prioritizeUserCommand(CAT.setSquelch(value, model: radioModel))
        radioState.squelch = value
    }

    func setMICGain(_ value: Int) {
        prioritizeUserCommand(CAT.setMICGain(value, model: radioModel))
        radioState.micGain = value
    }

    func setPower(_ value: Int) {
        prioritizeUserCommand(CAT.setPower(value, model: radioModel))
        radioState.power = value
    }

    func toggleNB() {
        let newValue = !radioState.noiseBlanker
        prioritizeUserCommand(CAT.setNB(newValue, model: radioModel))
        radioState.noiseBlanker = newValue
    }

    func toggleNR() {
        let newValue = !radioState.noiseReduction
        prioritizeUserCommand(CAT.setNR(newValue, model: radioModel))
        radioState.noiseReduction = newValue
    }

    func toggleDNF() {
        let newValue = !radioState.dnf
        prioritizeUserCommand(CAT.setDNF(newValue, model: radioModel))
        radioState.dnf = newValue
    }

    func toggleContour() {
        let newValue = !radioState.contour
        prioritizeUserCommand(CAT.setContour(newValue, model: radioModel))
        radioState.contour = newValue
    }

    func setContourFrequency(_ value: Int) {
        let clamped = min(3200, max(10, value))
        prioritizeUserCommand(CAT.setContourFrequency(clamped, model: radioModel))
        radioState.contourFrequency = clamped
    }

    func toggleSplit() {
        let newValue = !radioState.split
        pausePollingTemporarily()
        CAT.setSplit(newValue, model: radioModel).forEach(send)
        radioState.split = newValue
    }

    func toggleIPO() {
        let newValue = !radioState.ipo
        prioritizeUserCommand(CAT.setIPO(newValue, model: radioModel))
        radioState.ipo = newValue
    }

    func setSplit(_ enabled: Bool) {
        pausePollingTemporarily()
        CAT.setSplit(enabled, model: radioModel).forEach(send)
        radioState.split = enabled
    }

    func setToneMode(_ mode: ToneMode) {
        guard let command = CAT.setToneMode(mode, model: radioModel) else { return }
        prioritizeUserCommand(command)
        radioState.toneMode = mode
    }

    func setToneCode(index: Int, usesDCS: Bool) {
        prioritizeUserCommand(CAT.setToneCode(usesDCS: usesDCS, index: index, model: radioModel))
        if usesDCS {
            radioState.dcsCodeIndex = index
        } else {
            radioState.ctcssToneIndex = index
        }
    }

    func selectVFO(_ vfo: VFO) {
        // FT-991A CAT exposes direct access to FA/FB, but no documented VS read/set command.
        // The app therefore tracks which VFO the user is editing locally.
        radioState.activeVFO = vfo
    }

    func swapVFO() {
        prioritizeUserCommand(CAT.swapVFO)
        let temp = radioState.vfoAFrequency
        radioState.vfoAFrequency = radioState.vfoBFrequency
        radioState.vfoBFrequency = temp
    }

    func equalizeVFO() {
        prioritizeUserCommand(CAT.copyVFOAToVFOB)
        radioState.vfoBFrequency = radioState.vfoAFrequency
    }

    func startATUTune() {
        pausePollingTemporarily()
        send(CAT.atuOn(for: radioModel))
        commandQueue.asyncAfter(deadline: .now() + .milliseconds(180)) { [weak self] in
            guard let self else { return }
            self.send(CAT.startATUTune(for: self.radioModel))
        }
    }

    // MARK: - PTT Control

    func startTransmit(dataMode: Bool = false) {
        prioritizeUserCommand(dataMode ? CAT.txOnData : CAT.txOn)
        radioState.isTransmitting = true
    }

    func stopTransmit() {
        prioritizeUserCommand(CAT.txOff)
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

    private func pausePollingTemporarily() {
        guard isPolling else { return }
        stopPolling()
        resumePollingWorkItem?.cancel()
        let work = DispatchWorkItem { [weak self] in
            guard let self else { return }
            if self.serialManager.isConnected {
                self.startPolling()
            }
        }
        resumePollingWorkItem = work
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.8, execute: work)
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
