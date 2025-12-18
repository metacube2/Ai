//
//  RadioState.swift
//  FT991A-Remote
//
//  Model representing the current state of the FT-991A transceiver
//

import Foundation

// MARK: - Radio State

struct RadioState {
    // VFO Frequencies
    var vfoAFrequency: Int = 14_250_000  // Hz
    var vfoBFrequency: Int = 14_255_000  // Hz
    var activeVFO: VFO = .a

    // Operating Mode
    var mode: OperatingMode = .usb
    var filterWidth: Int = 3000  // Hz
    var filterShift: Int = 0     // Hz

    // Levels (0-255)
    var afGain: Int = 128
    var rfGain: Int = 255
    var squelch: Int = 0
    var micGain: Int = 50
    var power: Int = 100  // Watts (5-100)

    // Functions
    var noiseBlanker: Bool = false
    var noiseReduction: Bool = false
    var dnf: Bool = false
    var contour: Bool = false
    var atu: Bool = false
    var split: Bool = false
    var ipo: Bool = false

    // Metering
    var sMeter: Int = 0        // 0-255
    var powerMeter: Int = 0    // 0-255
    var swrMeter: Int = 0      // 0-255

    // TX State
    var isTransmitting: Bool = false

    // Computed Properties

    var activeFrequency: Int {
        activeVFO == .a ? vfoAFrequency : vfoBFrequency
    }

    var sMeterDB: Double {
        // S0-S9 = 0-54 dBμV, each S-unit = 6 dB
        // Above S9: +10, +20, +40, +60 dB
        let normalized = Double(sMeter) / 255.0
        if normalized <= 0.6 {
            return normalized / 0.6 * 54.0  // S0-S9
        } else {
            return 54.0 + (normalized - 0.6) / 0.4 * 60.0  // S9+60
        }
    }

    var sMeterString: String {
        let normalized = Double(sMeter) / 255.0
        if normalized <= 0.6 {
            let sUnit = Int(normalized / 0.6 * 9.0)
            return "S\(sUnit)"
        } else {
            let db = Int((normalized - 0.6) / 0.4 * 60.0)
            return "S9+\(db)"
        }
    }

    var frequencyDisplay: String {
        formatFrequency(activeFrequency)
    }

    func formatFrequency(_ freq: Int) -> String {
        let mhz = freq / 1_000_000
        let khz = (freq % 1_000_000) / 1_000
        let hz = freq % 1_000
        return String(format: "%d.%03d.%03d", mhz, khz, hz)
    }
}

// MARK: - VFO

enum VFO: String, Codable {
    case a = "A"
    case b = "B"
}

// MARK: - Operating Mode

enum OperatingMode: String, CaseIterable, Codable {
    case lsb = "LSB"
    case usb = "USB"
    case cw = "CW"
    case fm = "FM"
    case am = "AM"
    case rttyLSB = "RTTY-L"
    case cwReverse = "CW-R"
    case dataLSB = "DATA-L"
    case rttyUSB = "RTTY-U"
    case dataFM = "DATA-FM"
    case fmNarrow = "FM-N"
    case dataUSB = "DATA-U"
    case amNarrow = "AM-N"
    case c4fm = "C4FM"

    // CAT command value (MD0X)
    var catValue: String {
        switch self {
        case .lsb: return "1"
        case .usb: return "2"
        case .cw: return "3"
        case .fm: return "4"
        case .am: return "5"
        case .rttyLSB: return "6"
        case .cwReverse: return "7"
        case .dataLSB: return "8"
        case .rttyUSB: return "9"
        case .dataFM: return "A"
        case .fmNarrow: return "B"
        case .dataUSB: return "C"
        case .amNarrow: return "D"
        case .c4fm: return "E"
        }
    }

    static func from(catValue: String) -> OperatingMode? {
        allCases.first { $0.catValue == catValue }
    }

    var isDigital: Bool {
        switch self {
        case .dataLSB, .dataUSB, .dataFM, .rttyLSB, .rttyUSB, .c4fm:
            return true
        default:
            return false
        }
    }

    var defaultFilterWidth: Int {
        switch self {
        case .lsb, .usb, .dataLSB, .dataUSB: return 3000
        case .cw, .cwReverse: return 500
        case .am, .amNarrow: return 6000
        case .fm, .fmNarrow, .dataFM, .c4fm: return 15000
        case .rttyLSB, .rttyUSB: return 500
        }
    }
}

// MARK: - Frequency Step

enum FrequencyStep: Int, CaseIterable, Codable {
    case hz1 = 1
    case hz10 = 10
    case hz100 = 100
    case khz1 = 1000
    case khz5 = 5000
    case khz10 = 10000
    case khz100 = 100000
    case mhz1 = 1000000

    var displayName: String {
        switch self {
        case .hz1: return "1 Hz"
        case .hz10: return "10 Hz"
        case .hz100: return "100 Hz"
        case .khz1: return "1 kHz"
        case .khz5: return "5 kHz"
        case .khz10: return "10 kHz"
        case .khz100: return "100 kHz"
        case .mhz1: return "1 MHz"
        }
    }
}

// MARK: - Band

enum Band: String, CaseIterable {
    case m160 = "160m"
    case m80 = "80m"
    case m60 = "60m"
    case m40 = "40m"
    case m30 = "30m"
    case m20 = "20m"
    case m17 = "17m"
    case m15 = "15m"
    case m12 = "12m"
    case m10 = "10m"
    case m6 = "6m"
    case m2 = "2m"
    case cm70 = "70cm"

    var frequencyRange: ClosedRange<Int> {
        switch self {
        case .m160: return 1_800_000...2_000_000
        case .m80: return 3_500_000...4_000_000
        case .m60: return 5_351_500...5_366_500
        case .m40: return 7_000_000...7_300_000
        case .m30: return 10_100_000...10_150_000
        case .m20: return 14_000_000...14_350_000
        case .m17: return 18_068_000...18_168_000
        case .m15: return 21_000_000...21_450_000
        case .m12: return 24_890_000...24_990_000
        case .m10: return 28_000_000...29_700_000
        case .m6: return 50_000_000...54_000_000
        case .m2: return 144_000_000...148_000_000
        case .cm70: return 430_000_000...450_000_000
        }
    }

    var defaultFrequency: Int {
        switch self {
        case .m160: return 1_840_000
        case .m80: return 3_700_000
        case .m60: return 5_357_000
        case .m40: return 7_100_000
        case .m30: return 10_120_000
        case .m20: return 14_250_000
        case .m17: return 18_110_000
        case .m15: return 21_250_000
        case .m12: return 24_930_000
        case .m10: return 28_500_000
        case .m6: return 50_150_000
        case .m2: return 145_500_000
        case .cm70: return 433_500_000
        }
    }

    static func from(frequency: Int) -> Band? {
        allCases.first { $0.frequencyRange.contains(frequency) }
    }
}
