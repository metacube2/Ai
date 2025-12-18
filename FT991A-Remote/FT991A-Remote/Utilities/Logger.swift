//
//  Logger.swift
//  FT991A-Remote
//
//  Debug logging system
//

import Foundation
import os.log

// MARK: - Log Level

enum LogLevel: String, Comparable {
    case debug = "DEBUG"
    case info = "INFO"
    case warning = "WARN"
    case error = "ERROR"

    var osLogType: OSLogType {
        switch self {
        case .debug: return .debug
        case .info: return .info
        case .warning: return .default
        case .error: return .error
        }
    }

    var symbol: String {
        switch self {
        case .debug: return "🔍"
        case .info: return "ℹ️"
        case .warning: return "⚠️"
        case .error: return "❌"
        }
    }

    static func < (lhs: LogLevel, rhs: LogLevel) -> Bool {
        let order: [LogLevel] = [.debug, .info, .warning, .error]
        guard let lhsIndex = order.firstIndex(of: lhs),
              let rhsIndex = order.firstIndex(of: rhs) else { return false }
        return lhsIndex < rhsIndex
    }
}

// MARK: - Log Entry

struct LogEntry: Identifiable {
    let id = UUID()
    let timestamp: Date
    let level: LogLevel
    let message: String
    let file: String
    let function: String
    let line: Int

    var timeString: String {
        let formatter = DateFormatter()
        formatter.dateFormat = "HH:mm:ss.SSS"
        return formatter.string(from: timestamp)
    }

    var shortFile: String {
        URL(fileURLWithPath: file).lastPathComponent
    }

    var formattedMessage: String {
        "[\(timeString)] [\(level.rawValue)] \(message)"
    }

    var detailedMessage: String {
        "[\(timeString)] [\(level.rawValue)] [\(shortFile):\(line)] \(message)"
    }
}

// MARK: - Logger

class Logger: ObservableObject {

    // MARK: - Singleton

    static let shared = Logger()

    // MARK: - Published Properties

    @Published var entries: [LogEntry] = []
    @Published var minimumLevel: LogLevel = .debug
    @Published var isLoggingEnabled = true

    // MARK: - Private Properties

    private let osLog = OSLog(subsystem: "com.ft991a.remote", category: "General")
    private let queue = DispatchQueue(label: "logger.queue", qos: .utility)
    private let maxEntries = 1000

    // File logging
    private var logFileURL: URL?
    private var logFileHandle: FileHandle?

    // MARK: - Initialization

    private init() {
        setupFileLogging()
    }

    deinit {
        logFileHandle?.closeFile()
    }

    // MARK: - Logging

    func log(
        _ message: String,
        level: LogLevel = .info,
        file: String = #file,
        function: String = #function,
        line: Int = #line
    ) {
        guard isLoggingEnabled, level >= minimumLevel else { return }

        let entry = LogEntry(
            timestamp: Date(),
            level: level,
            message: message,
            file: file,
            function: function,
            line: line
        )

        // Console output
        queue.async {
            os_log("%{public}@", log: self.osLog, type: level.osLogType, entry.formattedMessage)
            #if DEBUG
            print(entry.detailedMessage)
            #endif
        }

        // In-memory storage
        DispatchQueue.main.async {
            self.entries.append(entry)
            if self.entries.count > self.maxEntries {
                self.entries.removeFirst(100)
            }
        }

        // File logging
        writeToFile(entry)
    }

    // MARK: - Convenience Methods

    func debug(_ message: String, file: String = #file, function: String = #function, line: Int = #line) {
        log(message, level: .debug, file: file, function: function, line: line)
    }

    func info(_ message: String, file: String = #file, function: String = #function, line: Int = #line) {
        log(message, level: .info, file: file, function: function, line: line)
    }

    func warning(_ message: String, file: String = #file, function: String = #function, line: Int = #line) {
        log(message, level: .warning, file: file, function: function, line: line)
    }

    func error(_ message: String, file: String = #file, function: String = #function, line: Int = #line) {
        log(message, level: .error, file: file, function: function, line: line)
    }

    // MARK: - File Logging

    private func setupFileLogging() {
        let fileManager = FileManager.default
        guard let logsDir = fileManager.urls(for: .applicationSupportDirectory, in: .userDomainMask).first else { return }

        let appLogsDir = logsDir.appendingPathComponent("FT991A-Remote/Logs", isDirectory: true)

        do {
            try fileManager.createDirectory(at: appLogsDir, withIntermediateDirectories: true)

            let formatter = DateFormatter()
            formatter.dateFormat = "yyyy-MM-dd"
            let fileName = "ft991a_\(formatter.string(from: Date())).log"

            logFileURL = appLogsDir.appendingPathComponent(fileName)

            if !fileManager.fileExists(atPath: logFileURL!.path) {
                fileManager.createFile(atPath: logFileURL!.path, contents: nil)
            }

            logFileHandle = try FileHandle(forWritingTo: logFileURL!)
            logFileHandle?.seekToEndOfFile()

            let header = "\n=== FT-991A Remote Log Started at \(Date()) ===\n"
            if let data = header.data(using: .utf8) {
                logFileHandle?.write(data)
            }
        } catch {
            print("Failed to setup file logging: \(error)")
        }
    }

    private func writeToFile(_ entry: LogEntry) {
        guard let handle = logFileHandle else { return }

        queue.async {
            if let data = (entry.detailedMessage + "\n").data(using: .utf8) {
                handle.write(data)
            }
        }
    }

    // MARK: - Management

    func clear() {
        entries.removeAll()
    }

    func exportLogs() -> String {
        entries.map { $0.detailedMessage }.joined(separator: "\n")
    }

    var filteredEntries: [LogEntry] {
        entries.filter { $0.level >= minimumLevel }
    }
}
