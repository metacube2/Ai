//
//  RadioViewModel.swift
//  FT991A-Remote
//
//  Main ViewModel for radio control
//

import Foundation
import Combine
import SwiftUI

// MARK: - Radio ViewModel

@MainActor
class RadioViewModel: ObservableObject {

    // MARK: - Published Properties

    // Connection
    @Published var isConnected = false
    @Published var connectionState: ConnectionState = .disconnected
    @Published var availablePorts: [SerialPort] = []
    @Published var selectedPort: String = ""
    @Published var baudRate: Int = 38400
    @Published var hasCATResponse = false
    @Published var lastCATResponseAt: Date?
    @Published var autoPingEnabled = false

    // Radio State (mirrored for convenience)
    @Published var vfoAFrequency: Int = 14_250_000
    @Published var vfoBFrequency: Int = 14_255_000
    @Published var activeVFO: VFO = .a
    @Published var mode: OperatingMode = .usb
    @Published var frequencyStep: FrequencyStep = .khz1

    // Levels
    @Published var afGain: Int = 128
    @Published var rfGain: Int = 255
    @Published var squelch: Int = 0
    @Published var micGain: Int = 50
    @Published var power: Int = 100

    // Functions
    @Published var noiseBlanker = false
    @Published var noiseReduction = false
    @Published var dnf = false
    @Published var contour = false
    @Published var atu = false
    @Published var split = false
    @Published var ipo = false

    // Metering
    @Published var sMeter: Int = 0
    @Published var powerMeter: Int = 0
    @Published var swrMeter: Int = 0
    @Published var isTransmitting = false

    // Statistics
    @Published var bytesSent: UInt64 = 0
    @Published var bytesReceived: UInt64 = 0

    // Debug
    @Published var commandHistory: [CommandLogEntry] = []

    // MARK: - Services

    private let serialManager = SerialPortManager()
    private let catProtocol: CATProtocol
    private var cancellables = Set<AnyCancellable>()
    private var autoPingTimer: Timer?

    // MARK: - Computed Properties

    var activeFrequency: Int {
        activeVFO == .a ? vfoAFrequency : vfoBFrequency
    }

    var frequencyDisplay: String {
        formatFrequency(activeFrequency)
    }

    var sMeterDisplay: String {
        let normalized = Double(sMeter) / 255.0
        if normalized <= 0.6 {
            let sUnit = Int(normalized / 0.6 * 9.0)
            return "S\(sUnit)"
        } else {
            let db = Int((normalized - 0.6) / 0.4 * 60.0)
            return "S9+\(db)"
        }
    }

    var currentBand: Band? {
        Band.from(frequency: activeFrequency)
    }

    var selectedPortDisplayName: String {
        availablePorts.first(where: { $0.path == selectedPort })?.name ?? selectedPort
    }

    var catAlive: Bool {
        guard isConnected, hasCATResponse, let lastCATResponseAt else { return false }
        return Date().timeIntervalSince(lastCATResponseAt) < 2.0
    }

    var catStatusText: String {
        if case .connecting = connectionState {
            return "CAT wartet auf Antwort"
        }
        if case .error(let message) = connectionState {
            return message
        }
        if !isConnected {
            return "CAT offline"
        }
        if catAlive, let lastCATResponseAt {
            let age = Date().timeIntervalSince(lastCATResponseAt)
            return String(format: "CAT alive%@ · RX %.1fs", autoPingEnabled ? " · Auto-Ping" : "", age)
        }
        if hasCATResponse {
            return "CAT verbunden\(autoPingEnabled ? " · Auto-Ping" : "") · keine frische Antwort"
        }
        return "Port offen\(autoPingEnabled ? " · Auto-Ping" : "") · keine CAT-Antwort"
    }

    var catLastSeenText: String {
        guard let lastCATResponseAt else { return "keine RX" }
        return lastCATResponseAt.formatted(date: .omitted, time: .standard)
    }

    // MARK: - Initialization

    init() {
        catProtocol = CATProtocol(serialManager: serialManager)
        setupBindings()
        refreshPorts()
    }

    private func setupBindings() {
        // Serial Manager bindings
        serialManager.$connectionState
            .receive(on: DispatchQueue.main)
            .sink { [weak self] state in
                self?.connectionState = state
                self?.isConnected = state.isConnected
                if !state.isConnected {
                    self?.stopAutoPing()
                } else if self?.autoPingEnabled == true {
                    self?.startAutoPing()
                }
            }
            .store(in: &cancellables)

        serialManager.$availablePorts
            .receive(on: DispatchQueue.main)
            .assign(to: &$availablePorts)

        serialManager.$selectedPortPath
            .receive(on: DispatchQueue.main)
            .assign(to: &$selectedPort)

        serialManager.$bytesSent
            .receive(on: DispatchQueue.main)
            .assign(to: &$bytesSent)

        serialManager.$bytesReceived
            .receive(on: DispatchQueue.main)
            .assign(to: &$bytesReceived)

        serialManager.$hasCATResponse
            .receive(on: DispatchQueue.main)
            .assign(to: &$hasCATResponse)

        serialManager.$lastResponseAt
            .receive(on: DispatchQueue.main)
            .assign(to: &$lastCATResponseAt)

        $autoPingEnabled
            .removeDuplicates()
            .sink { [weak self] enabled in
                guard let self else { return }
                if enabled, self.isConnected {
                    self.startAutoPing()
                } else {
                    self.stopAutoPing()
                }
            }
            .store(in: &cancellables)

        // CAT Protocol bindings
        catProtocol.$radioState
            .receive(on: DispatchQueue.main)
            .sink { [weak self] state in
                self?.updateFromRadioState(state)
            }
            .store(in: &cancellables)

        catProtocol.$commandHistory
            .receive(on: DispatchQueue.main)
            .assign(to: &$commandHistory)
    }

    private func updateFromRadioState(_ state: RadioState) {
        vfoAFrequency = state.vfoAFrequency
        vfoBFrequency = state.vfoBFrequency
        activeVFO = state.activeVFO
        mode = state.mode
        afGain = state.afGain
        rfGain = state.rfGain
        squelch = state.squelch
        micGain = state.micGain
        power = state.power
        noiseBlanker = state.noiseBlanker
        noiseReduction = state.noiseReduction
        dnf = state.dnf
        contour = state.contour
        atu = state.atu
        split = state.split
        ipo = state.ipo
        sMeter = state.sMeter
        powerMeter = state.powerMeter
        swrMeter = state.swrMeter
        isTransmitting = state.isTransmitting
    }

    // MARK: - Connection

    func refreshPorts() {
        serialManager.refreshPorts()
    }

    func connect() {
        serialManager.selectedPortPath = selectedPort
        serialManager.baudRate = baudRate
        serialManager.connect()
    }

    func disconnect() {
        stopAutoPing()
        serialManager.disconnect()
    }

    func toggleConnection() {
        if isConnected {
            disconnect()
        } else {
            connect()
        }
    }

    func pingCAT() {
        guard isConnected else { return }
        catProtocol.ping()
    }

    private func startAutoPing() {
        stopAutoPing()
        autoPingTimer = Timer.scheduledTimer(withTimeInterval: 2.0, repeats: true) { [weak self] _ in
            guard let self, self.isConnected, self.autoPingEnabled else { return }
            self.catProtocol.ping()
        }
    }

    private func stopAutoPing() {
        autoPingTimer?.invalidate()
        autoPingTimer = nil
    }

    func selectPort(_ path: String) {
        selectedPort = path
        serialManager.selectedPortPath = path
    }

    // MARK: - Frequency Control

    func setFrequency(_ frequency: Int) {
        catProtocol.setFrequency(frequency, vfo: activeVFO)
    }

    func incrementFrequency() {
        catProtocol.changeFrequency(by: frequencyStep.rawValue)
    }

    func decrementFrequency() {
        catProtocol.changeFrequency(by: -frequencyStep.rawValue)
    }

    func selectBand(_ band: Band) {
        catProtocol.selectBand(band)
    }

    // MARK: - VFO Control

    func selectVFO(_ vfo: VFO) {
        catProtocol.selectVFO(vfo)
    }

    func swapVFO() {
        catProtocol.swapVFO()
    }

    func equalizeVFO() {
        catProtocol.equalizeVFO()
    }

    // MARK: - Mode Control

    func setMode(_ mode: OperatingMode) {
        catProtocol.setMode(mode)
    }

    // MARK: - Level Control

    func setAFGain(_ value: Int) {
        catProtocol.setAFGain(value)
    }

    func setRFGain(_ value: Int) {
        catProtocol.setRFGain(value)
    }

    func setSquelch(_ value: Int) {
        catProtocol.setSquelch(value)
    }

    func setMICGain(_ value: Int) {
        catProtocol.setMICGain(value)
    }

    func setPower(_ value: Int) {
        catProtocol.setPower(value)
    }

    // MARK: - Function Control

    func toggleNB() {
        catProtocol.toggleNB()
    }

    func toggleNR() {
        catProtocol.toggleNR()
    }

    func toggleDNF() {
        catProtocol.toggleDNF()
    }

    func toggleSplit() {
        catProtocol.toggleSplit()
    }

    func startATUTune() {
        catProtocol.startATUTune()
    }

    // MARK: - PTT Control

    func startTransmit(dataMode: Bool = false) {
        catProtocol.startTransmit(dataMode: dataMode)
    }

    func stopTransmit() {
        catProtocol.stopTransmit()
    }

    func toggleTransmit(dataMode: Bool = false) {
        catProtocol.toggleTransmit(dataMode: dataMode)
    }

    // MARK: - Debug

    func sendRawCommand(_ command: String) {
        catProtocol.sendRaw(command)
    }

    func clearCommandHistory() {
        catProtocol.clearCommandHistory()
    }

    // MARK: - Helpers

    func formatFrequency(_ freq: Int) -> String {
        let mhz = freq / 1_000_000
        let khz = (freq % 1_000_000) / 1_000
        let hz = freq % 1_000
        return String(format: "%d.%03d.%03d", mhz, khz, hz)
    }
}
