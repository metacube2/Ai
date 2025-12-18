//
//  CATCommand.swift
//  FT991A-Remote
//
//  CAT Command definitions for FT-991A
//

import Foundation

// MARK: - CAT Command

struct CATCommand {
    let command: String
    let description: String
    let expectsResponse: Bool

    init(_ command: String, description: String = "", expectsResponse: Bool = true) {
        self.command = command
        self.description = description
        self.expectsResponse = expectsResponse
    }

    var data: Data {
        (command + ";").data(using: .ascii) ?? Data()
    }
}

// MARK: - CAT Commands Catalog

enum CAT {

    // MARK: - Frequency Commands

    /// Read VFO-A frequency
    static let readVFOA = CATCommand("FA", description: "Read VFO-A frequency")

    /// Set VFO-A frequency (9 digits in Hz)
    static func setVFOA(_ frequency: Int) -> CATCommand {
        CATCommand(String(format: "FA%09d", frequency), description: "Set VFO-A to \(frequency) Hz", expectsResponse: false)
    }

    /// Read VFO-B frequency
    static let readVFOB = CATCommand("FB", description: "Read VFO-B frequency")

    /// Set VFO-B frequency (9 digits in Hz)
    static func setVFOB(_ frequency: Int) -> CATCommand {
        CATCommand(String(format: "FB%09d", frequency), description: "Set VFO-B to \(frequency) Hz", expectsResponse: false)
    }

    /// Swap VFO A/B
    static let swapVFO = CATCommand("SV", description: "Swap VFO A/B", expectsResponse: false)

    /// Select VFO-A
    static let selectVFOA = CATCommand("VS0", description: "Select VFO-A", expectsResponse: false)

    /// Select VFO-B
    static let selectVFOB = CATCommand("VS1", description: "Select VFO-B", expectsResponse: false)

    /// Read active VFO
    static let readActiveVFO = CATCommand("VS", description: "Read active VFO")

    // MARK: - Mode Commands

    /// Read operating mode
    static let readMode = CATCommand("MD0", description: "Read operating mode")

    /// Set operating mode
    static func setMode(_ mode: OperatingMode) -> CATCommand {
        CATCommand("MD0\(mode.catValue)", description: "Set mode to \(mode.rawValue)", expectsResponse: false)
    }

    // MARK: - Level Commands

    /// Read AF gain
    static let readAFGain = CATCommand("AG0", description: "Read AF gain")

    /// Set AF gain (000-255)
    static func setAFGain(_ value: Int) -> CATCommand {
        CATCommand(String(format: "AG0%03d", min(255, max(0, value))), description: "Set AF gain", expectsResponse: false)
    }

    /// Read RF gain
    static let readRFGain = CATCommand("RG0", description: "Read RF gain")

    /// Set RF gain (000-255)
    static func setRFGain(_ value: Int) -> CATCommand {
        CATCommand(String(format: "RG0%03d", min(255, max(0, value))), description: "Set RF gain", expectsResponse: false)
    }

    /// Read squelch
    static let readSquelch = CATCommand("SQ0", description: "Read squelch")

    /// Set squelch (000-255)
    static func setSquelch(_ value: Int) -> CATCommand {
        CATCommand(String(format: "SQ0%03d", min(255, max(0, value))), description: "Set squelch", expectsResponse: false)
    }

    /// Read MIC gain
    static let readMICGain = CATCommand("MG", description: "Read MIC gain")

    /// Set MIC gain (000-100)
    static func setMICGain(_ value: Int) -> CATCommand {
        CATCommand(String(format: "MG%03d", min(100, max(0, value))), description: "Set MIC gain", expectsResponse: false)
    }

    /// Read power level
    static let readPower = CATCommand("PC", description: "Read power level")

    /// Set power level (005-100)
    static func setPower(_ value: Int) -> CATCommand {
        CATCommand(String(format: "PC%03d", min(100, max(5, value))), description: "Set power to \(value)W", expectsResponse: false)
    }

    // MARK: - Function Commands

    /// Read Noise Blanker status
    static let readNB = CATCommand("NB0", description: "Read NB status")

    /// Set Noise Blanker on/off
    static func setNB(_ enabled: Bool) -> CATCommand {
        CATCommand("NB0\(enabled ? "1" : "0")", description: enabled ? "Enable NB" : "Disable NB", expectsResponse: false)
    }

    /// Read Noise Reduction status
    static let readNR = CATCommand("NR0", description: "Read NR status")

    /// Set Noise Reduction on/off
    static func setNR(_ enabled: Bool) -> CATCommand {
        CATCommand("NR0\(enabled ? "1" : "0")", description: enabled ? "Enable NR" : "Disable NR", expectsResponse: false)
    }

    /// Read DNF status
    static let readDNF = CATCommand("BC0", description: "Read DNF status")

    /// Set DNF on/off
    static func setDNF(_ enabled: Bool) -> CATCommand {
        CATCommand("BC0\(enabled ? "1" : "0")", description: enabled ? "Enable DNF" : "Disable DNF", expectsResponse: false)
    }

    /// Read Contour status
    static let readContour = CATCommand("CO00", description: "Read Contour status")

    /// Read ATU status
    static let readATU = CATCommand("AC", description: "Read ATU status")

    /// Start ATU tune
    static let startATUTune = CATCommand("AC001", description: "Start ATU tune", expectsResponse: false)

    /// Read Split status
    static let readSplit = CATCommand("FT", description: "Read Split status")

    /// Set Split on/off
    static func setSplit(_ enabled: Bool) -> CATCommand {
        CATCommand("FT\(enabled ? "1" : "0")", description: enabled ? "Enable Split" : "Disable Split", expectsResponse: false)
    }

    // MARK: - Metering Commands

    /// Read S-Meter
    static let readSMeter = CATCommand("SM0", description: "Read S-Meter")

    /// Read Power meter
    static let readPowerMeter = CATCommand("RM1", description: "Read Power meter")

    /// Read SWR meter
    static let readSWRMeter = CATCommand("RM6", description: "Read SWR meter")

    // MARK: - PTT Commands

    /// Start transmitting (MIC)
    static let txOn = CATCommand("TX0", description: "TX on (MIC)", expectsResponse: false)

    /// Start transmitting (DATA)
    static let txOnData = CATCommand("TX1", description: "TX on (DATA)", expectsResponse: false)

    /// Stop transmitting
    static let txOff = CATCommand("RX", description: "TX off", expectsResponse: false)

    /// Read TX status
    static let readTXStatus = CATCommand("TX", description: "Read TX status")

    // MARK: - Identification

    /// Read radio ID
    static let readID = CATCommand("ID", description: "Read radio ID")

    // MARK: - Information

    /// Read all status (IF command)
    static let readInfo = CATCommand("IF", description: "Read info")
}

// MARK: - CAT Response

struct CATResponse {
    let command: String
    let value: String
    let rawData: String
    let timestamp: Date

    init(rawData: String) {
        self.rawData = rawData.trimmingCharacters(in: CharacterSet(charactersIn: ";\r\n"))
        self.timestamp = Date()

        // Parse command prefix (2 characters usually)
        if rawData.count >= 2 {
            let prefixEnd = rawData.index(rawData.startIndex, offsetBy: 2)
            self.command = String(rawData[..<prefixEnd])
            self.value = String(rawData[prefixEnd...]).trimmingCharacters(in: CharacterSet(charactersIn: ";\r\n"))
        } else {
            self.command = rawData
            self.value = ""
        }
    }

    // MARK: - Value Parsers

    /// Parse frequency from FA/FB response (9 digits)
    var frequency: Int? {
        guard command == "FA" || command == "FB" else { return nil }
        return Int(value)
    }

    /// Parse mode from MD0 response
    var mode: OperatingMode? {
        guard command == "MD" else { return nil }
        let modeChar = value.dropFirst()  // Remove "0" prefix
        return OperatingMode.from(catValue: String(modeChar))
    }

    /// Parse level value (3 digits)
    var levelValue: Int? {
        // Handle commands like AG0XXX, RG0XXX, SQ0XXX
        let numericPart = value.filter { $0.isNumber }
        return Int(numericPart)
    }

    /// Parse S-Meter from SM0 response
    var sMeter: Int? {
        guard command == "SM" else { return nil }
        // SM0XXX format - drop the "0" prefix
        let numericPart = value.dropFirst()
        return Int(numericPart)
    }

    /// Parse boolean status (0 or 1)
    var boolValue: Bool? {
        guard let last = value.last else { return nil }
        return last == "1"
    }

    /// Parse VFO selection
    var vfo: VFO? {
        guard command == "VS" else { return nil }
        switch value {
        case "0": return .a
        case "1": return .b
        default: return nil
        }
    }

    /// Check if this is the FT-991A ID
    var isFT991A: Bool {
        command == "ID" && value == "0670"
    }
}
