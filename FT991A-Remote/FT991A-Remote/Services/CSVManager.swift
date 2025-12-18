//
//  CSVManager.swift
//  FT991A-Remote
//
//  CSV Log file management
//

import Foundation

// MARK: - CSV Manager

class CSVManager: ObservableObject {

    // MARK: - Published Properties

    @Published var logEntries: [QSOEntry] = []
    @Published var currentLogFile: URL?
    @Published var lastError: String?
    @Published var isSaving = false

    // MARK: - Properties

    private let fileManager = FileManager.default
    private var logDirectory: URL

    // MARK: - Initialization

    init(logDirectory: String = "~/Documents/FT991A-Logs/") {
        let expandedPath = (logDirectory as NSString).expandingTildeInPath
        self.logDirectory = URL(fileURLWithPath: expandedPath, isDirectory: true)

        ensureDirectoryExists()
    }

    // MARK: - Directory Management

    private func ensureDirectoryExists() {
        if !fileManager.fileExists(atPath: logDirectory.path) {
            do {
                try fileManager.createDirectory(at: logDirectory, withIntermediateDirectories: true)
                Logger.shared.log("Created log directory: \(logDirectory.path)", level: .info)
            } catch {
                lastError = "Konnte Log-Verzeichnis nicht erstellen: \(error.localizedDescription)"
                Logger.shared.log(lastError!, level: .error)
            }
        }
    }

    func setLogDirectory(_ path: String) {
        let expandedPath = (path as NSString).expandingTildeInPath
        logDirectory = URL(fileURLWithPath: expandedPath, isDirectory: true)
        ensureDirectoryExists()
    }

    // MARK: - File Operations

    func createNewLogFile(name: String? = nil) -> URL {
        let fileName = name ?? generateLogFileName()
        let fileURL = logDirectory.appendingPathComponent(fileName)

        // Write header
        do {
            try QSOEntry.csvHeader.appending("\n").write(to: fileURL, atomically: true, encoding: .utf8)
            currentLogFile = fileURL
            Logger.shared.log("Created new log file: \(fileURL.lastPathComponent)", level: .info)
        } catch {
            lastError = "Konnte Log-Datei nicht erstellen: \(error.localizedDescription)"
            Logger.shared.log(lastError!, level: .error)
        }

        return fileURL
    }

    private func generateLogFileName() -> String {
        let formatter = DateFormatter()
        formatter.dateFormat = "yyyy-MM-dd_HHmmss"
        return "QSO_Log_\(formatter.string(from: Date())).csv"
    }

    func openLogFile(_ url: URL) -> Bool {
        guard fileManager.fileExists(atPath: url.path) else {
            lastError = "Datei existiert nicht: \(url.lastPathComponent)"
            return false
        }

        do {
            let content = try String(contentsOf: url, encoding: .utf8)
            logEntries = parseCSV(content)
            currentLogFile = url
            Logger.shared.log("Opened log file: \(url.lastPathComponent) with \(logEntries.count) entries", level: .info)
            return true
        } catch {
            lastError = "Konnte Datei nicht lesen: \(error.localizedDescription)"
            Logger.shared.log(lastError!, level: .error)
            return false
        }
    }

    // MARK: - Parsing

    private func parseCSV(_ content: String) -> [QSOEntry] {
        var entries: [QSOEntry] = []
        let lines = content.components(separatedBy: .newlines)

        for (index, line) in lines.enumerated() {
            // Skip header and empty lines
            guard index > 0, !line.trimmingCharacters(in: .whitespaces).isEmpty else { continue }

            if let entry = QSOEntry.from(csvLine: line) {
                entries.append(entry)
            }
        }

        return entries
    }

    // MARK: - Entry Management

    func addEntry(_ entry: QSOEntry) {
        logEntries.append(entry)
        saveCurrentLog()
    }

    func updateEntry(_ entry: QSOEntry) {
        if let index = logEntries.firstIndex(where: { $0.id == entry.id }) {
            logEntries[index] = entry
            saveCurrentLog()
        }
    }

    func deleteEntry(_ entry: QSOEntry) {
        logEntries.removeAll { $0.id == entry.id }
        saveCurrentLog()
    }

    func deleteEntries(at offsets: IndexSet) {
        logEntries.remove(atOffsets: offsets)
        saveCurrentLog()
    }

    // MARK: - Saving

    func saveCurrentLog() {
        guard let fileURL = currentLogFile else {
            // Create new file if none exists
            _ = createNewLogFile()
            guard let newURL = currentLogFile else { return }
            saveToFile(newURL)
            return
        }

        saveToFile(fileURL)
    }

    private func saveToFile(_ url: URL) {
        isSaving = true

        var content = QSOEntry.csvHeader + "\n"
        for entry in logEntries {
            content += entry.csvLine + "\n"
        }

        do {
            try content.write(to: url, atomically: true, encoding: .utf8)
            Logger.shared.log("Saved \(logEntries.count) entries to \(url.lastPathComponent)", level: .debug)
        } catch {
            lastError = "Fehler beim Speichern: \(error.localizedDescription)"
            Logger.shared.log(lastError!, level: .error)
        }

        isSaving = false
    }

    func exportToFile(_ url: URL) -> Bool {
        var content = QSOEntry.csvHeader + "\n"
        for entry in logEntries {
            content += entry.csvLine + "\n"
        }

        do {
            try content.write(to: url, atomically: true, encoding: .utf8)
            Logger.shared.log("Exported \(logEntries.count) entries to \(url.path)", level: .info)
            return true
        } catch {
            lastError = "Export fehlgeschlagen: \(error.localizedDescription)"
            Logger.shared.log(lastError!, level: .error)
            return false
        }
    }

    // MARK: - File Listing

    func listLogFiles() -> [URL] {
        do {
            let files = try fileManager.contentsOfDirectory(
                at: logDirectory,
                includingPropertiesForKeys: [.creationDateKey],
                options: [.skipsHiddenFiles]
            )
            return files
                .filter { $0.pathExtension.lowercased() == "csv" }
                .sorted { url1, url2 in
                    let date1 = (try? url1.resourceValues(forKeys: [.creationDateKey]).creationDate) ?? Date.distantPast
                    let date2 = (try? url2.resourceValues(forKeys: [.creationDateKey]).creationDate) ?? Date.distantPast
                    return date1 > date2
                }
        } catch {
            Logger.shared.log("Error listing log files: \(error)", level: .error)
            return []
        }
    }

    // MARK: - Statistics

    var totalQSOs: Int {
        logEntries.count
    }

    var uniqueCallsigns: Int {
        Set(logEntries.map { $0.callsign.uppercased() }).count
    }

    var bandStatistics: [String: Int] {
        var stats: [String: Int] = [:]
        for entry in logEntries {
            let band = entry.bandDisplay
            stats[band, default: 0] += 1
        }
        return stats
    }

    var modeStatistics: [String: Int] {
        var stats: [String: Int] = [:]
        for entry in logEntries {
            let mode = entry.mode.rawValue
            stats[mode, default: 0] += 1
        }
        return stats
    }
}
