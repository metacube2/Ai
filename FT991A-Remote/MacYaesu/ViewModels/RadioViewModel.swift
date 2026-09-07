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

struct RadioServiceContainer {
    let serialManager: any SerialPortServiceType
    let catProtocol: any CATProtocolServiceType

    static func live() -> RadioServiceContainer {
        let serialManager = SerialPortManager()
        let catProtocol = CATProtocol(serialManager: serialManager)
        return RadioServiceContainer(serialManager: serialManager, catProtocol: catProtocol)
    }
}

@MainActor
class RadioViewModel: ObservableObject {

    // MARK: - Published Properties

    // Connection
    @Published var isConnected = false
    @Published var connectionState: ConnectionState = .disconnected
    @Published var availablePorts: [SerialPort] = []
    @Published var selectedPort: String = ""
    @Published var baudRate: Int = 38400
    @Published var radioModel: RadioModel = .ft991a
    @Published var searchUSBPortsUntilFound = false
    @Published var hasCATResponse = false
    @Published var lastCATResponseAt: Date?
    @Published var currentPortSearchIndex = 0
    @Published var currentPortSearchTotal = 0
    @Published var autoPingEnabled = false
    @Published var isScanning = false
    @Published var scanDirection: ScanDirection = .up
    @Published var scanLabel: String = ""

    // Radio State (mirrored for convenience)
    @Published var vfoAFrequency: Int = 14_250_000
    @Published var vfoBFrequency: Int = 14_255_000
    @Published var activeVFO: VFO = .a
    @Published var mode: OperatingMode = .usb
    @Published var frequencyStep: FrequencyStep = .khz1
    @Published var toneMode: ToneMode = .off
    @Published var ctcssToneIndex: Int = 0
    @Published var dcsCodeIndex: Int = 0

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
    @Published var contourFrequency: Int = 1000
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
    @Published var isUsageLocked = false

    // MARK: - Services

    private let serialManager: any SerialPortServiceType
    private let catProtocol: any CATProtocolServiceType
    private var cancellables = Set<AnyCancellable>()
    private var autoPingTimer: Timer?
    private var scanTimer: Timer?
    private var currentMemoryScanIndex = 0

    // MARK: - Computed Properties

    var activeFrequency: Int {
        activeVFO == .a ? vfoAFrequency : vfoBFrequency
    }

    var capabilities: RadioCapabilities {
        radioModel.capabilities
    }

    var availableToneModes: [ToneMode] {
        var modes = capabilities.supportedToneModes
        if !modes.contains(toneMode) {
            modes.append(toneMode)
        }
        return modes
    }

    var squelchRange: ClosedRange<Double> {
        Double(capabilities.squelchRange.lowerBound)...Double(capabilities.squelchRange.upperBound)
    }

    var powerRange: ClosedRange<Double> {
        Double(capabilities.powerRange.lowerBound)...Double(capabilities.powerRange.upperBound)
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
            if isSearchingPorts {
                return "Pruefe Port \(currentPortSearchIndex)/\(currentPortSearchTotal) · \(selectedPortDisplayName)"
            }
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

    var isSearchingPorts: Bool {
        searchUSBPortsUntilFound && currentPortSearchIndex > 0
    }

    var portSearchStatusText: String? {
        guard isSearchingPorts else { return nil }
        let total = max(currentPortSearchTotal, currentPortSearchIndex)
        return "USB-Port-Suche \(currentPortSearchIndex)/\(total): \(selectedPortDisplayName)"
    }

    var catLastSeenText: String {
        guard let lastCATResponseAt else { return "keine RX" }
        return lastCATResponseAt.formatted(date: .omitted, time: .standard)
    }

    var toneSummaryText: String {
        switch toneMode {
        case .off:
            return "Tone aus"
        case .ctcssEncode, .ctcssEncodeDecode:
            let index = min(max(0, ctcssToneIndex), ToneCatalog.ctcssFrequencies.count - 1)
            return "\(toneMode.rawValue) \(String(format: "%.1f", ToneCatalog.ctcssFrequencies[index])) Hz"
        case .dcsEncode, .dcsEncodeDecode:
            let index = min(max(0, dcsCodeIndex), ToneCatalog.dcsCodes.count - 1)
            return "\(toneMode.rawValue) \(String(format: "%03d", ToneCatalog.dcsCodes[index]))"
        case .pagerFrequency, .reverseTone:
            return toneMode.rawValue
        }
    }

    // MARK: - Initialization

    init(services: RadioServiceContainer = .live()) {
        serialManager = services.serialManager
        catProtocol = services.catProtocol
        setupBindings()
        refreshPorts()
    }

    private func setupBindings() {
        // Serial Manager bindings
        serialManager.connectionStatePublisher
            .receive(on: DispatchQueue.main)
            .sink { [weak self] state in
                self?.connectionState = state
                self?.isConnected = state.isConnected
                if !state.isConnected {
                    self?.stopAutoPing()
                    self?.stopScan()
                } else if self?.autoPingEnabled == true {
                    self?.startAutoPing()
                }
            }
            .store(in: &cancellables)

        serialManager.availablePortsPublisher
            .receive(on: DispatchQueue.main)
            .assign(to: &$availablePorts)

        serialManager.selectedPortPathPublisher
            .receive(on: DispatchQueue.main)
            .assign(to: &$selectedPort)

        serialManager.bytesSentPublisher
            .receive(on: DispatchQueue.main)
            .assign(to: &$bytesSent)

        serialManager.bytesReceivedPublisher
            .receive(on: DispatchQueue.main)
            .assign(to: &$bytesReceived)

        serialManager.hasCATResponsePublisher
            .receive(on: DispatchQueue.main)
            .assign(to: &$hasCATResponse)

        serialManager.lastResponseAtPublisher
            .receive(on: DispatchQueue.main)
            .assign(to: &$lastCATResponseAt)

        serialManager.currentPortSearchIndexPublisher
            .receive(on: DispatchQueue.main)
            .assign(to: &$currentPortSearchIndex)

        serialManager.currentPortSearchTotalPublisher
            .receive(on: DispatchQueue.main)
            .assign(to: &$currentPortSearchTotal)

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
        catProtocol.radioStatePublisher
            .receive(on: DispatchQueue.main)
            .sink { [weak self] state in
                self?.updateFromRadioState(state)
            }
            .store(in: &cancellables)

        catProtocol.commandHistoryPublisher
            .receive(on: DispatchQueue.main)
            .assign(to: &$commandHistory)
    }

    private func updateFromRadioState(_ state: RadioState) {
        vfoAFrequency = state.vfoAFrequency
        vfoBFrequency = state.vfoBFrequency
        activeVFO = state.activeVFO
        mode = state.mode
        toneMode = state.toneMode
        ctcssToneIndex = state.ctcssToneIndex
        dcsCodeIndex = state.dcsCodeIndex
        afGain = state.afGain
        rfGain = state.rfGain
        squelch = state.squelch
        micGain = state.micGain
        power = state.power
        noiseBlanker = state.noiseBlanker
        noiseReduction = state.noiseReduction
        dnf = state.dnf
        contour = state.contour
        contourFrequency = state.contourFrequency
        atu = state.atu
        split = state.split
        ipo = state.ipo
        sMeter = state.sMeter
        powerMeter = state.powerMeter
        swrMeter = state.swrMeter
        isTransmitting = state.isTransmitting
        VUMeterHubService.shared.updateMeters(
            signalLevel: state.sMeter,
            swrLevel: state.swrMeter,
            isTransmitting: state.isTransmitting
        )
    }

    // MARK: - Connection

    func refreshPorts() {
        serialManager.refreshPorts()
    }

    func connect() {
        guard !isUsageLocked else { return }
        serialManager.radioModel = radioModel
        serialManager.selectedPortPath = selectedPort
        serialManager.baudRate = baudRate
        serialManager.searchUSBPortsUntilFound = searchUSBPortsUntilFound
        serialManager.connect()
    }

    func disconnect() {
        stopAutoPing()
        stopScan()
        serialManager.disconnect()
    }

    func toggleConnection() {
        guard !isUsageLocked || isConnected else { return }
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
        stopScan()
        catProtocol.setFrequency(frequency, vfo: activeVFO)
    }

    func incrementFrequency() {
        stopScan()
        catProtocol.changeFrequency(by: frequencyStep.rawValue)
    }

    func decrementFrequency() {
        stopScan()
        catProtocol.changeFrequency(by: -frequencyStep.rawValue)
    }

    func incrementFrequencyFine() {
        stopScan()
        catProtocol.changeFrequency(by: fineFrequencyStep)
    }

    func decrementFrequencyFine() {
        stopScan()
        catProtocol.changeFrequency(by: -fineFrequencyStep)
    }

    func selectBand(_ band: Band) {
        stopScan()
        catProtocol.selectBand(band)
    }

    private var fineFrequencyStep: Int {
        max(10, frequencyStep.rawValue / 10)
    }

    // MARK: - VFO Control

    func selectVFO(_ vfo: VFO) {
        stopScan()
        catProtocol.selectVFO(vfo)
    }

    func swapVFO() {
        stopScan()
        catProtocol.swapVFO()
    }

    func equalizeVFO() {
        stopScan()
        catProtocol.equalizeVFO()
    }

    // MARK: - Mode Control

    func setMode(_ mode: OperatingMode) {
        stopScan()
        catProtocol.setMode(mode)
    }

    func setToneMode(_ toneMode: ToneMode) {
        stopScan()
        catProtocol.setToneMode(toneMode)
    }

    func setCTCSSToneIndex(_ index: Int) {
        stopScan()
        let clampedIndex = min(max(0, index), ToneCatalog.ctcssFrequencies.count - 1)
        catProtocol.setToneCode(index: clampedIndex, usesDCS: false)
    }

    func setDCSCodeIndex(_ index: Int) {
        stopScan()
        let clampedIndex = min(max(0, index), ToneCatalog.dcsCodes.count - 1)
        catProtocol.setToneCode(index: clampedIndex, usesDCS: true)
    }

    func configureRepeater(shift: RepeaterShiftDirection, offsetHz: Int, toneMode: ToneMode, ctcssIndex: Int? = nil, dcsIndex: Int? = nil) {
        stopScan()

        if shift == .off || offsetHz <= 0 {
            catProtocol.setSplit(false)
            split = false
        } else {
            let target = vfoAFrequency + (shift.signedMultiplier * offsetHz)
            catProtocol.setFrequency(target, vfo: .b)
            catProtocol.setSplit(true)
            vfoBFrequency = target
            split = true
        }

        catProtocol.setToneMode(toneMode)
        self.toneMode = toneMode

        if toneMode.usesDCS {
            let safeIndex = min(max(0, dcsIndex ?? dcsCodeIndex), ToneCatalog.dcsCodes.count - 1)
            catProtocol.setToneCode(index: safeIndex, usesDCS: true)
            dcsCodeIndex = safeIndex
        } else if toneMode.usesTone {
            let safeIndex = min(max(0, ctcssIndex ?? ctcssToneIndex), ToneCatalog.ctcssFrequencies.count - 1)
            catProtocol.setToneCode(index: safeIndex, usesDCS: false)
            ctcssToneIndex = safeIndex
        }
    }

    // MARK: - Level Control

    func setAFGain(_ value: Int) {
        catProtocol.setAFGain(value)
    }

    func setRFGain(_ value: Int) {
        catProtocol.setRFGain(value)
    }

    func setSquelch(_ value: Int) {
        let clamped = min(capabilities.squelchRange.upperBound, max(capabilities.squelchRange.lowerBound, value))
        catProtocol.setSquelch(clamped)
    }

    func setMICGain(_ value: Int) {
        catProtocol.setMICGain(value)
    }

    func setPower(_ value: Int) {
        let clamped = min(capabilities.powerRange.upperBound, max(capabilities.powerRange.lowerBound, value))
        catProtocol.setPower(clamped)
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

    func toggleContour() {
        catProtocol.toggleContour()
    }

    func setContourFrequency(_ value: Int) {
        catProtocol.setContourFrequency(value)
    }

    func toggleSplit() {
        catProtocol.toggleSplit()
    }

    func toggleIPO() {
        catProtocol.toggleIPO()
    }

    func startATUTune() {
        catProtocol.startATUTune()
    }

    func setRadioModel(_ model: RadioModel) {
        radioModel = model
        serialManager.radioModel = model
        serialManager.refreshPorts()
        catProtocol.radioModel = model
        let supportedModes = model.capabilities.supportedToneModes
        if !supportedModes.contains(toneMode), let fallback = supportedModes.first {
            toneMode = fallback
        }
        squelch = min(model.capabilities.squelchRange.upperBound, max(model.capabilities.squelchRange.lowerBound, squelch))
        power = min(model.capabilities.powerRange.upperBound, max(model.capabilities.powerRange.lowerBound, power))
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

    // MARK: - Scan

    func startScan(direction: ScanDirection) {
        guard isConnected else { return }
        stopScan()
        isScanning = true
        scanDirection = direction
        scanLabel = "VFO \(direction.rawValue)"
        performScanStep(direction)
        scanTimer = Timer.scheduledTimer(withTimeInterval: 0.45, repeats: true) { [weak self] _ in
            self?.performScanStep(direction)
        }
    }

    func startMemoryScan(direction: ScanDirection, entries: [MemoryEntry]) {
        guard isConnected, !entries.isEmpty else { return }
        stopScan()
        isScanning = true
        scanDirection = direction
        scanLabel = "Memory \(direction.rawValue)"
        currentMemoryScanIndex = closestMemoryIndex(in: entries)
        performMemoryScanStep(direction, entries: entries)
        scanTimer = Timer.scheduledTimer(withTimeInterval: 0.8, repeats: true) { [weak self] _ in
            self?.performMemoryScanStep(direction, entries: entries)
        }
    }

    func stopScan() {
        scanTimer?.invalidate()
        scanTimer = nil
        isScanning = false
        scanLabel = ""
    }

    func toggleScan(direction: ScanDirection) {
        if isScanning, scanDirection == direction {
            stopScan()
        } else {
            startScan(direction: direction)
        }
    }

    private func performScanStep(_ direction: ScanDirection) {
        let delta = frequencyStep.rawValue * (direction == .up ? 1 : -1)
        var nextFrequency = activeFrequency + delta

        if let band = currentBand {
            if nextFrequency > band.frequencyRange.upperBound {
                nextFrequency = band.frequencyRange.lowerBound
            } else if nextFrequency < band.frequencyRange.lowerBound {
                nextFrequency = band.frequencyRange.upperBound
            }
        }

        catProtocol.setFrequency(nextFrequency, vfo: activeVFO)
    }

    private func performMemoryScanStep(_ direction: ScanDirection, entries: [MemoryEntry]) {
        guard !entries.isEmpty else {
            stopScan()
            return
        }

        if direction == .up {
            currentMemoryScanIndex = (currentMemoryScanIndex + 1) % entries.count
        } else {
            currentMemoryScanIndex = (currentMemoryScanIndex - 1 + entries.count) % entries.count
        }

        applyMemory(entries[currentMemoryScanIndex], stopScanning: false)
        isScanning = true
        scanDirection = direction
        scanLabel = "Memory \(direction.rawValue) · \(entries[currentMemoryScanIndex].slot)"
    }

    private func closestMemoryIndex(in entries: [MemoryEntry]) -> Int {
        guard let exactMatch = entries.firstIndex(where: { $0.frequency == activeFrequency }) else {
            return -1
        }
        return exactMatch
    }

    // MARK: - Memory

    func captureCurrentMemory(slot: Int, name: String) -> MemoryEntry {
        let delta = vfoBFrequency - vfoAFrequency
        let offset = abs(delta)
        let shift: RepeaterShiftDirection

        if delta > 0 {
            shift = .plus
        } else if delta < 0 {
            shift = .minus
        } else {
            shift = .off
        }

        let cleanedName = name.trimmingCharacters(in: .whitespacesAndNewlines)

        return MemoryEntry(
            slot: slot,
            name: cleanedName.isEmpty ? "Speicher \(slot)" : cleanedName,
            frequency: vfoAFrequency,
            mode: mode,
            splitEnabled: split || offset > 0,
            vfoBFrequency: (split || offset > 0) ? vfoBFrequency : nil,
            repeaterOffsetHz: offset,
            repeaterShift: shift,
            power: power
            ,
            toneMode: toneMode,
            ctcssToneIndex: ctcssToneIndex,
            dcsCodeIndex: dcsCodeIndex
        )
    }

    func recallMemory(_ entry: MemoryEntry) {
        applyMemory(entry, stopScanning: true)
    }

    private func applyMemory(_ entry: MemoryEntry, stopScanning: Bool) {
        if stopScanning {
            stopScan()
        }
        catProtocol.selectVFO(.a)
        catProtocol.setMode(entry.mode)
        catProtocol.setFrequency(entry.frequency, vfo: .a)
        activeVFO = .a
        mode = entry.mode
        vfoAFrequency = entry.frequency

        if let targetVFOB = targetFrequencyForMemory(entry) {
            catProtocol.setFrequency(targetVFOB, vfo: .b)
            vfoBFrequency = targetVFOB
        }

        catProtocol.setSplit(entry.splitEnabled)
        catProtocol.setPower(entry.power)
        catProtocol.setToneMode(entry.toneMode)
        if entry.toneMode.usesDCS {
            catProtocol.setToneCode(index: entry.dcsCodeIndex, usesDCS: true)
        } else if entry.toneMode.usesTone {
            catProtocol.setToneCode(index: entry.ctcssToneIndex, usesDCS: false)
        }
        split = entry.splitEnabled
        power = entry.power
        toneMode = entry.toneMode
        ctcssToneIndex = entry.ctcssToneIndex
        dcsCodeIndex = entry.dcsCodeIndex
    }

    private func targetFrequencyForMemory(_ entry: MemoryEntry) -> Int? {
        if let vfoBFrequency = entry.vfoBFrequency {
            return vfoBFrequency
        }

        guard entry.repeaterShift != .off, entry.repeaterOffsetHz > 0 else {
            return nil
        }

        return entry.frequency + (entry.repeaterShift.signedMultiplier * entry.repeaterOffsetHz)
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
