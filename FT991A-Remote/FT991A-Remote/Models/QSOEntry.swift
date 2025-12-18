//
//  QSOEntry.swift
//  FT991A-Remote
//
//  Model for QSO log entries
//

import Foundation

// MARK: - QSO Entry

struct QSOEntry: Identifiable, Codable, Hashable {
    let id: UUID
    var callsign: String
    var date: Date
    var frequency: Int           // Hz
    var mode: OperatingMode
    var rstSent: String          // e.g., "59", "599"
    var rstReceived: String
    var name: String
    var qth: String
    var locator: String          // Maidenhead grid
    var power: Int               // Watts
    var notes: String

    init(
        id: UUID = UUID(),
        callsign: String = "",
        date: Date = Date(),
        frequency: Int = 14_250_000,
        mode: OperatingMode = .usb,
        rstSent: String = "59",
        rstReceived: String = "59",
        name: String = "",
        qth: String = "",
        locator: String = "",
        power: Int = 100,
        notes: String = ""
    ) {
        self.id = id
        self.callsign = callsign
        self.date = date
        self.frequency = frequency
        self.mode = mode
        self.rstSent = rstSent
        self.rstReceived = rstReceived
        self.name = name
        self.qth = qth
        self.locator = locator
        self.power = power
        self.notes = notes
    }

    // MARK: - CSV Export

    static let csvHeader = "Call,Date,Time,Frequency,Mode,RST_TX,RST_RX,Name,QTH,Locator,Power,Notes"

    var csvLine: String {
        let dateFormatter = DateFormatter()
        dateFormatter.dateFormat = "yyyy-MM-dd"
        let timeFormatter = DateFormatter()
        timeFormatter.dateFormat = "HH:mm:ss"
        timeFormatter.timeZone = TimeZone(identifier: "UTC")

        let freqMHz = String(format: "%.6f", Double(frequency) / 1_000_000.0)

        // Escape fields with commas or quotes
        let escapedNotes = notes.contains(",") || notes.contains("\"")
            ? "\"\(notes.replacingOccurrences(of: "\"", with: "\"\""))\""
            : notes

        let escapedName = name.contains(",") || name.contains("\"")
            ? "\"\(name.replacingOccurrences(of: "\"", with: "\"\""))\""
            : name

        return [
            callsign,
            dateFormatter.string(from: date),
            timeFormatter.string(from: date),
            freqMHz,
            mode.rawValue,
            rstSent,
            rstReceived,
            escapedName,
            qth,
            locator,
            String(power),
            escapedNotes
        ].joined(separator: ",")
    }

    // MARK: - CSV Import

    static func from(csvLine: String) -> QSOEntry? {
        var fields: [String] = []
        var current = ""
        var inQuotes = false

        for char in csvLine {
            if char == "\"" {
                inQuotes.toggle()
            } else if char == "," && !inQuotes {
                fields.append(current)
                current = ""
            } else {
                current.append(char)
            }
        }
        fields.append(current)

        guard fields.count >= 12 else { return nil }

        let dateFormatter = DateFormatter()
        dateFormatter.dateFormat = "yyyy-MM-dd HH:mm:ss"
        dateFormatter.timeZone = TimeZone(identifier: "UTC")

        guard let date = dateFormatter.date(from: "\(fields[1]) \(fields[2])") else { return nil }
        guard let freqMHz = Double(fields[3]) else { return nil }

        let frequency = Int(freqMHz * 1_000_000)
        let mode = OperatingMode.allCases.first { $0.rawValue == fields[4] } ?? .usb

        return QSOEntry(
            callsign: fields[0],
            date: date,
            frequency: frequency,
            mode: mode,
            rstSent: fields[5],
            rstReceived: fields[6],
            name: fields[7],
            qth: fields[8],
            locator: fields[9],
            power: Int(fields[10]) ?? 100,
            notes: fields[11]
        )
    }

    // MARK: - Display Helpers

    var frequencyDisplay: String {
        let mhz = frequency / 1_000_000
        let khz = (frequency % 1_000_000) / 1_000
        let hz = frequency % 1_000
        return String(format: "%d.%03d.%03d", mhz, khz, hz)
    }

    var dateDisplay: String {
        let formatter = DateFormatter()
        formatter.dateFormat = "dd.MM.yyyy"
        return formatter.string(from: date)
    }

    var timeDisplay: String {
        let formatter = DateFormatter()
        formatter.dateFormat = "HH:mm"
        formatter.timeZone = TimeZone(identifier: "UTC")
        return formatter.string(from: date) + " UTC"
    }

    var bandDisplay: String {
        Band.from(frequency: frequency)?.rawValue ?? "?"
    }
}
