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
    fileprivate struct CommandProfile {
        let squelchMax: Int
        let powerReadDescription: String
        let powerCommand: (Int) -> CATCommand
        let readNB: CATCommand
        let setNB: (Bool) -> CATCommand
        let readNR: CATCommand
        let setNR: (Bool) -> CATCommand
        let readSplit: CATCommand
        let setSplit: (Bool) -> [CATCommand]
        let readIPO: CATCommand
        let setIPO: (Bool) -> CATCommand
        let readATU: CATCommand
        let atuOn: CATCommand
        let startATUTune: CATCommand
    }

    fileprivate static func profile(for model: RadioModel) -> CommandProfile {
        switch model {
        case .ft991a:
            return CommandProfile(
                squelchMax: 100,
                powerReadDescription: "Read power level",
                powerCommand: { value in
                    CATCommand(String(format: "PC%03d", min(100, max(5, value))), description: "Set power to \(value)W", expectsResponse: false)
                },
                readNB: CATCommand("NB0", description: "Read NB status"),
                setNB: { enabled in
                    CATCommand("NB0\(enabled ? "1" : "0")", description: enabled ? "Enable NB" : "Disable NB", expectsResponse: false)
                },
                readNR: CATCommand("NR0", description: "Read NR status"),
                setNR: { enabled in
                    CATCommand("NR0\(enabled ? "1" : "0")", description: enabled ? "Enable NR" : "Disable NR", expectsResponse: false)
                },
                readSplit: CATCommand("FT", description: "Read TX VFO selection"),
                setSplit: { enabled in
                    [CATCommand("FT\(enabled ? "3" : "2")", description: enabled ? "Set TX to VFO-B" : "Set TX to VFO-A", expectsResponse: false)]
                },
                readIPO: CATCommand("PA0", description: "Read IPO / preamp state"),
                setIPO: { enabled in
                    CATCommand("PA0\(enabled ? "0" : "1")", description: enabled ? "Set IPO" : "Set AMP1 preamp", expectsResponse: false)
                },
                readATU: CATCommand("AC", description: "Read ATU status"),
                atuOn: CATCommand("AC001", description: "Turn ATU on", expectsResponse: false),
                startATUTune: CATCommand("AC002", description: "Start ATU tuning", expectsResponse: false)
            )
        case .ftx1Field:
            return CommandProfile(
                squelchMax: 255,
                powerReadDescription: "Read field head power level",
                powerCommand: { value in
                    CATCommand(String(format: "PC1%03d", min(10, max(5, value))), description: "Set field head power to \(value)W", expectsResponse: false)
                },
                readNB: CATCommand("NL0", description: "Read NB level"),
                setNB: { enabled in
                    CATCommand(enabled ? "NL0005" : "NL0000", description: enabled ? "Enable NB level 5" : "Disable NB", expectsResponse: false)
                },
                readNR: CATCommand("RL0", description: "Read NR level"),
                setNR: { enabled in
                    CATCommand(enabled ? "RL005" : "RL000", description: enabled ? "Enable NR level 5" : "Disable NR", expectsResponse: false)
                },
                readSplit: CATCommand("ST", description: "Read split status"),
                setSplit: { enabled in
                    [
                        CATCommand("ST\(enabled ? "1" : "0")", description: enabled ? "Enable split" : "Disable split", expectsResponse: false),
                        CATCommand("FT\(enabled ? "1" : "0")", description: enabled ? "Set TX to SUB-side" : "Set TX to MAIN-side", expectsResponse: false)
                    ]
                },
                readIPO: CATCommand("PA0", description: "Read HF/50MHz IPO / preamp state"),
                setIPO: { enabled in
                    CATCommand("PA0\(enabled ? "0" : "1")", description: enabled ? "Set IPO (HF/50)" : "Set AMP1 preamp (HF/50)", expectsResponse: false)
                },
                readATU: CATCommand("AC", description: "Read ATU status"),
                atuOn: CATCommand("AC101", description: "Turn external tuner on", expectsResponse: false),
                startATUTune: CATCommand("AC103", description: "Start external tuner tuning", expectsResponse: false)
            )
        case .ftx1Optima:
            return CommandProfile(
                squelchMax: 255,
                powerReadDescription: "Read power level (FTX-1 package dependent)",
                powerCommand: { value in
                    CATCommand(String(format: "PC2%03d", min(100, max(5, value))), description: "Set SPA-1 power to \(value)W", expectsResponse: false)
                },
                readNB: CATCommand("NL0", description: "Read NB level"),
                setNB: { enabled in
                    CATCommand(enabled ? "NL0005" : "NL0000", description: enabled ? "Enable NB level 5" : "Disable NB", expectsResponse: false)
                },
                readNR: CATCommand("RL0", description: "Read NR level"),
                setNR: { enabled in
                    CATCommand(enabled ? "RL005" : "RL000", description: enabled ? "Enable NR level 5" : "Disable NR", expectsResponse: false)
                },
                readSplit: CATCommand("ST", description: "Read split status"),
                setSplit: { enabled in
                    [
                        CATCommand("ST\(enabled ? "1" : "0")", description: enabled ? "Enable split" : "Disable split", expectsResponse: false),
                        CATCommand("FT\(enabled ? "1" : "0")", description: enabled ? "Set TX to SUB-side" : "Set TX to MAIN-side", expectsResponse: false)
                    ]
                },
                readIPO: CATCommand("PA0", description: "Read HF/50MHz IPO / preamp state"),
                setIPO: { enabled in
                    CATCommand("PA0\(enabled ? "0" : "1")", description: enabled ? "Set IPO (HF/50)" : "Set AMP1 preamp (HF/50)", expectsResponse: false)
                },
                readATU: CATCommand("AC", description: "Read ATU status"),
                atuOn: CATCommand("AC001", description: "Turn internal tuner on", expectsResponse: false),
                startATUTune: CATCommand("AC003", description: "Start internal tuner tuning", expectsResponse: false)
            )
        }
    }

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

    /// Copy VFO-A to VFO-B (A=B)
    static let copyVFOAToVFOB = CATCommand("AB", description: "Copy VFO-A to VFO-B", expectsResponse: false)

    // MARK: - Mode Commands

    /// Read operating mode
    static func readMode(for model: RadioModel) -> CATCommand {
        switch model {
        case .ft991a, .ftx1Field, .ftx1Optima:
            return CATCommand("MD0", description: "Read operating mode")
        }
    }

    /// Set operating mode
    static func setMode(_ mode: OperatingMode, model: RadioModel) -> CATCommand {
        switch model {
        case .ft991a, .ftx1Field, .ftx1Optima:
            return CATCommand("MD0\(mode.catValue)", description: "Set mode to \(mode.rawValue)", expectsResponse: false)
        }
    }

    // MARK: - Level Commands

    /// Read AF gain
    static func readAFGain(for model: RadioModel) -> CATCommand {
        switch model {
        case .ft991a, .ftx1Field, .ftx1Optima:
            return CATCommand("AG0", description: "Read AF gain")
        }
    }

    /// Set AF gain (000-255)
    static func setAFGain(_ value: Int, model: RadioModel) -> CATCommand {
        switch model {
        case .ft991a, .ftx1Field, .ftx1Optima:
            return CATCommand(String(format: "AG0%03d", min(255, max(0, value))), description: "Set AF gain", expectsResponse: false)
        }
    }

    /// Read RF gain
    static func readRFGain(for model: RadioModel) -> CATCommand {
        switch model {
        case .ft991a, .ftx1Field, .ftx1Optima:
            return CATCommand("RG0", description: "Read RF gain")
        }
    }

    /// Set RF gain (000-255)
    static func setRFGain(_ value: Int, model: RadioModel) -> CATCommand {
        switch model {
        case .ft991a, .ftx1Field, .ftx1Optima:
            return CATCommand(String(format: "RG0%03d", min(255, max(0, value))), description: "Set RF gain", expectsResponse: false)
        }
    }

    /// Read squelch
    static func readSquelch(for model: RadioModel) -> CATCommand {
        switch model {
        case .ft991a, .ftx1Field, .ftx1Optima:
            return CATCommand("SQ0", description: "Read squelch")
        }
    }

    /// Set squelch (000-100)
    static func setSquelch(_ value: Int, model: RadioModel) -> CATCommand {
        let maxValue = profile(for: model).squelchMax
        return CATCommand(String(format: "SQ0%03d", min(maxValue, max(0, value))), description: "Set squelch", expectsResponse: false)
    }

    /// Read MIC gain
    static func readMICGain(for model: RadioModel) -> CATCommand {
        switch model {
        case .ft991a, .ftx1Field, .ftx1Optima:
            return CATCommand("MG", description: "Read MIC gain")
        }
    }

    /// Set MIC gain (000-100)
    static func setMICGain(_ value: Int, model: RadioModel) -> CATCommand {
        switch model {
        case .ft991a, .ftx1Field, .ftx1Optima:
            return CATCommand(String(format: "MG%03d", min(100, max(0, value))), description: "Set MIC gain", expectsResponse: false)
        }
    }

    /// Read power level
    static func readPower(for model: RadioModel) -> CATCommand {
        CATCommand("PC", description: profile(for: model).powerReadDescription)
    }

    /// Set power level (005-100)
    static func setPower(_ value: Int, model: RadioModel) -> CATCommand {
        profile(for: model).powerCommand(value)
    }

    // MARK: - Function Commands

    /// Read Noise Blanker status
    static func readNB(for model: RadioModel) -> CATCommand {
        profile(for: model).readNB
    }

    /// Set Noise Blanker on/off
    static func setNB(_ enabled: Bool, model: RadioModel) -> CATCommand {
        profile(for: model).setNB(enabled)
    }

    /// Read Noise Reduction status
    static func readNR(for model: RadioModel) -> CATCommand {
        profile(for: model).readNR
    }

    /// Set Noise Reduction on/off
    static func setNR(_ enabled: Bool, model: RadioModel) -> CATCommand {
        profile(for: model).setNR(enabled)
    }

    /// Read Auto Notch status
    static func readDNF(for model: RadioModel) -> CATCommand {
        switch model {
        case .ft991a, .ftx1Field, .ftx1Optima:
            return CATCommand("BC0", description: "Read Auto Notch status")
        }
    }

    /// Set Auto Notch on/off
    static func setDNF(_ enabled: Bool, model: RadioModel) -> CATCommand {
        switch model {
        case .ft991a, .ftx1Field, .ftx1Optima:
            return CATCommand("BC0\(enabled ? "1" : "0")", description: enabled ? "Enable Auto Notch" : "Disable Auto Notch", expectsResponse: false)
        }
    }

    /// Read Contour status
    static func readContour(for model: RadioModel) -> CATCommand {
        switch model {
        case .ft991a, .ftx1Field, .ftx1Optima:
            return CATCommand("CO00", description: "Read Contour status")
        }
    }

    /// Read Contour frequency
    static func readContourFrequency(for model: RadioModel) -> CATCommand {
        switch model {
        case .ft991a, .ftx1Field, .ftx1Optima:
            return CATCommand("CO01", description: "Read Contour frequency")
        }
    }

    /// Set Contour on/off (P2=0, P3=0000/0001)
    static func setContour(_ enabled: Bool, model: RadioModel) -> CATCommand {
        switch model {
        case .ft991a, .ftx1Field, .ftx1Optima:
            return CATCommand(enabled ? "CO000001" : "CO000000", description: enabled ? "Enable Contour" : "Disable Contour", expectsResponse: false)
        }
    }

    /// Set Contour frequency (P2=1, 0010...3200 Hz)
    static func setContourFrequency(_ value: Int, model: RadioModel) -> CATCommand {
        switch model {
        case .ft991a, .ftx1Field, .ftx1Optima:
            return CATCommand(String(format: "CO01%04d", min(3200, max(10, value))), description: "Set Contour frequency", expectsResponse: false)
        }
    }

    /// Read ATU status
    static func readATU(for model: RadioModel) -> CATCommand {
        profile(for: model).readATU
    }

    /// Start ATU tune
    static func atuOn(for model: RadioModel) -> CATCommand {
        profile(for: model).atuOn
    }

    static func startATUTune(for model: RadioModel) -> CATCommand {
        profile(for: model).startATUTune
    }

    /// Read TX VFO selection (A/B)
    static func readSplit(for model: RadioModel) -> CATCommand {
        profile(for: model).readSplit
    }

    /// Set TX VFO selection using the documented FT command.
    static func setSplit(_ enabled: Bool, model: RadioModel) -> [CATCommand] {
        profile(for: model).setSplit(enabled)
    }

    /// Read preamp / IPO state
    static func readIPO(for model: RadioModel) -> CATCommand {
        profile(for: model).readIPO
    }

    /// Set IPO on/off using PA (0=IPO, 1=AMP1)
    static func setIPO(_ enabled: Bool, model: RadioModel) -> CATCommand {
        profile(for: model).setIPO(enabled)
    }

    // MARK: - Metering Commands

    /// Read S-Meter
    static let readSMeter = CATCommand("SM0", description: "Read S-Meter")

    /// Read Power meter
    static let readPowerMeter = CATCommand("RM1", description: "Read Power meter")

    /// Read SWR meter
    static let readSWRMeter = CATCommand("RM6", description: "Read SWR meter")

    // MARK: - PTT Commands

    /// Start transmitting via CAT
    static let txOnData = CATCommand("TX1", description: "TX on (DATA)", expectsResponse: false)

    /// Start transmitting via CAT
    static let txOn = CATCommand("TX1", description: "TX on (CAT)", expectsResponse: false)

    /// Stop transmitting
    static let txOff = CATCommand("TX0", description: "TX off", expectsResponse: false)

    /// Read TX status
    static let readTXStatus = CATCommand("TX", description: "Read TX status")

    // MARK: - Identification

    /// Read radio ID
    static let readID = CATCommand("ID", description: "Read radio ID")

    // MARK: - Information

    /// Read all status (IF command)
    static let readInfo = CATCommand("IF", description: "Read info")

    // MARK: - Tone Commands

    /// Read tone mode
    static func readToneMode(for model: RadioModel) -> CATCommand {
        switch model {
        case .ft991a, .ftx1Field, .ftx1Optima:
            return CATCommand("CT0", description: "Read tone mode")
        }
    }

    /// Set tone mode
    static func setToneMode(_ mode: ToneMode, model: RadioModel) -> CATCommand? {
        guard let value = mode.catValue(for: model) else { return nil }
        return CATCommand("CT0\(value)", description: "Set tone mode to \(mode.rawValue)", expectsResponse: false)
    }

    /// Read tone / DCS code
    static func readToneCode(usesDCS: Bool, model: RadioModel) -> CATCommand {
        switch model {
        case .ft991a, .ftx1Field, .ftx1Optima:
            return CATCommand("CN0\(usesDCS ? "1" : "0")", description: usesDCS ? "Read DCS code" : "Read CTCSS tone")
        }
    }

    /// Set tone / DCS code
    static func setToneCode(usesDCS: Bool, index: Int, model: RadioModel) -> CATCommand {
        CATCommand(
            String(format: "CN0%d%03d", usesDCS ? 1 : 0, index),
            description: usesDCS ? "Set DCS code index \(index)" : "Set CTCSS tone index \(index)",
            expectsResponse: false
        )
    }
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
    func mode(for model: RadioModel) -> OperatingMode? {
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

    func toneMode(for model: RadioModel) -> ToneMode? {
        guard command == "CT" else { return nil }
        return ToneMode.from(catValue: String(value.suffix(1)), model: model)
    }

    var toneCodeKind: Int? {
        guard command == "CN", value.count >= 5 else { return nil }
        return Int(String(value.dropFirst().prefix(1)))
    }

    var toneCodeIndex: Int? {
        guard command == "CN", value.count >= 4 else { return nil }
        return Int(String(value.suffix(3)))
    }

    var contourEnabled: Bool? {
        guard command == "CO", value.count >= 6, value.hasPrefix("00") else { return nil }
        return String(value.suffix(4)) == "0001"
    }

    var contourFrequency: Int? {
        guard command == "CO", value.count >= 6, value.hasPrefix("01") else { return nil }
        return Int(String(value.suffix(4)))
    }

    var ipoEnabled: Bool? {
        guard command == "PA", let last = value.last else { return nil }
        return last == "0"
    }

    /// Check if this is the FT-991A ID
    var isFT991A: Bool {
        command == "ID" && value == "0670"
    }

    var isFTX1: Bool {
        command == "ID" && value == "0840"
    }

    var isEchoOnly: Bool {
        if rawData.hasPrefix("INFO:") { return false }
        switch command {
        case "FA", "FB", "MD", "SM", "RM", "AG", "RG", "SQ", "NB", "NR", "NL", "RL", "BC", "CO", "AC", "FT", "ST", "TX", "PA", "ID", "CT", "CN":
            return value.isEmpty
        default:
            return false
        }
    }

    var isOverflowMessage: Bool {
        rawData.contains("Cmd overflow")
    }
}
