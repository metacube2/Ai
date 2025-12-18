//
//  LogViewModel.swift
//  FT991A-Remote
//
//  ViewModel for QSO logging
//

import Foundation
import Combine
import SwiftUI

// MARK: - Log ViewModel

@MainActor
class LogViewModel: ObservableObject {

    // MARK: - Published Properties

    @Published var entries: [QSOEntry] = []
    @Published var selectedEntry: QSOEntry?
    @Published var searchText = ""
    @Published var sortOrder: SortOrder = .dateDescending

    // Current QSO being logged
    @Published var currentQSO = QSOEntry()

    // File management
    @Published var currentLogFile: URL?
    @Published var availableLogFiles: [URL] = []
    @Published var isSaving = false
    @Published var lastError: String?

    // MARK: - Private Properties

    private let csvManager = CSVManager()
    private var cancellables = Set<AnyCancellable>()

    // MARK: - Computed Properties

    var filteredEntries: [QSOEntry] {
        var result = entries

        // Apply search filter
        if !searchText.isEmpty {
            let search = searchText.lowercased()
            result = result.filter {
                $0.callsign.lowercased().contains(search) ||
                $0.name.lowercased().contains(search) ||
                $0.qth.lowercased().contains(search) ||
                $0.notes.lowercased().contains(search)
            }
        }

        // Apply sorting
        switch sortOrder {
        case .dateDescending:
            result.sort { $0.date > $1.date }
        case .dateAscending:
            result.sort { $0.date < $1.date }
        case .callsignAscending:
            result.sort { $0.callsign < $1.callsign }
        case .callsignDescending:
            result.sort { $0.callsign > $1.callsign }
        case .frequencyAscending:
            result.sort { $0.frequency < $1.frequency }
        case .frequencyDescending:
            result.sort { $0.frequency > $1.frequency }
        }

        return result
    }

    var totalQSOs: Int {
        entries.count
    }

    var uniqueCallsigns: Int {
        Set(entries.map { $0.callsign.uppercased() }).count
    }

    // MARK: - Initialization

    init() {
        setupBindings()
        refreshLogFiles()
        loadLatestLog()
    }

    private func setupBindings() {
        csvManager.$logEntries
            .receive(on: DispatchQueue.main)
            .assign(to: &$entries)

        csvManager.$currentLogFile
            .receive(on: DispatchQueue.main)
            .assign(to: &$currentLogFile)

        csvManager.$isSaving
            .receive(on: DispatchQueue.main)
            .assign(to: &$isSaving)

        csvManager.$lastError
            .receive(on: DispatchQueue.main)
            .assign(to: &$lastError)
    }

    // MARK: - File Management

    func refreshLogFiles() {
        availableLogFiles = csvManager.listLogFiles()
    }

    func loadLatestLog() {
        if let latest = availableLogFiles.first {
            _ = csvManager.openLogFile(latest)
        }
    }

    func openLogFile(_ url: URL) {
        _ = csvManager.openLogFile(url)
    }

    func createNewLogFile(name: String? = nil) {
        _ = csvManager.createNewLogFile(name: name)
        refreshLogFiles()
    }

    func exportToFile(_ url: URL) -> Bool {
        csvManager.exportToFile(url)
    }

    func setLogDirectory(_ path: String) {
        csvManager.setLogDirectory(path)
        refreshLogFiles()
    }

    // MARK: - QSO Management

    func addQSO() {
        guard !currentQSO.callsign.isEmpty else { return }

        var entry = currentQSO
        entry.callsign = entry.callsign.uppercased()

        csvManager.addEntry(entry)
        resetCurrentQSO()

        Logger.shared.log("Added QSO: \(entry.callsign)", level: .info)
    }

    func updateQSO(_ entry: QSOEntry) {
        csvManager.updateEntry(entry)
    }

    func deleteQSO(_ entry: QSOEntry) {
        csvManager.deleteEntry(entry)
    }

    func deleteQSOs(at offsets: IndexSet) {
        // Convert offsets from filtered to original indices
        let entriesToDelete = offsets.map { filteredEntries[$0] }
        for entry in entriesToDelete {
            csvManager.deleteEntry(entry)
        }
    }

    func resetCurrentQSO() {
        currentQSO = QSOEntry()
    }

    // MARK: - Radio Integration

    func updateFromRadio(frequency: Int, mode: OperatingMode, power: Int) {
        currentQSO.frequency = frequency
        currentQSO.mode = mode
        currentQSO.power = power
    }

    // MARK: - Statistics

    var bandStatistics: [(band: String, count: Int)] {
        var stats: [String: Int] = [:]
        for entry in entries {
            let band = entry.bandDisplay
            stats[band, default: 0] += 1
        }
        return stats.map { (band: $0.key, count: $0.value) }
            .sorted { $0.count > $1.count }
    }

    var modeStatistics: [(mode: String, count: Int)] {
        var stats: [String: Int] = [:]
        for entry in entries {
            let mode = entry.mode.rawValue
            stats[mode, default: 0] += 1
        }
        return stats.map { (mode: $0.key, count: $0.value) }
            .sorted { $0.count > $1.count }
    }

    // MARK: - Sort Order

    enum SortOrder: String, CaseIterable {
        case dateDescending = "Datum (neu → alt)"
        case dateAscending = "Datum (alt → neu)"
        case callsignAscending = "Rufzeichen (A → Z)"
        case callsignDescending = "Rufzeichen (Z → A)"
        case frequencyAscending = "Frequenz (niedrig → hoch)"
        case frequencyDescending = "Frequenz (hoch → niedrig)"
    }
}
