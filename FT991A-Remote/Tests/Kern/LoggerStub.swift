// Ersatz fuer Utilities/Logger.swift in den Kerntests (der echte Logger braucht AppSettings und schreibt Dateien).
import Foundation

enum LogLevel: String, Comparable {
    case debug = "DEBUG", info = "INFO", warning = "WARN", error = "ERROR"
    static func < (lhs: LogLevel, rhs: LogLevel) -> Bool { lhs.rawValue < rhs.rawValue }
}

final class Logger {
    static let shared = Logger()
    private(set) var zeilen: [String] = []
    func log(_ message: String, level: LogLevel = .info, file: String = #file, function: String = #function, line: Int = #line) {
        zeilen.append("\(level.rawValue) \(message)")
    }
    func catTrace(_ message: String) { zeilen.append("TRACE \(message)") }
    func startCATSession(portName: String, portPath: String, baudRate: Int) {}
}
