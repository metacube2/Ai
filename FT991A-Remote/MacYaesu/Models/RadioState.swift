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
    var toneMode: ToneMode = .off
    var ctcssToneIndex: Int = 0
    var dcsCodeIndex: Int = 0

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
    var contourFrequency: Int = 1000
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

// MARK: - Radio Model

enum RadioModel: String, Codable, CaseIterable, Identifiable {
    case ft991a
    case ftx1Field
    case ftx1Optima

    var id: String { rawValue }

    var displayName: String {
        profile.displayName
    }

    var serialDeviceName: String {
        profile.serialDeviceName
    }

    var familyName: String {
        profile.familyName
    }

    var capabilities: RadioCapabilities {
        profile.capabilities
    }

    var toneModeMap: [ToneMode: String] {
        profile.toneModeMap
    }

    var reverseToneModeMap: [String: ToneMode] {
        Dictionary(uniqueKeysWithValues: toneModeMap.map { ($1, $0) })
    }

    var profile: RadioProfile {
        switch self {
        case .ft991a:
            return .ft991a
        case .ftx1Field:
            return .ftx1Field
        case .ftx1Optima:
            return .ftx1Optima
        }
    }
}

struct RadioProfile {
    let displayName: String
    let serialDeviceName: String
    let familyName: String
    let capabilities: RadioCapabilities
    let toneModeMap: [ToneMode: String]

    static let ft991a = RadioProfile(
        displayName: "FT-991A",
        serialDeviceName: "FT-991A",
        familyName: "FT-991A",
        capabilities: RadioCapabilities(
            familyName: "FT-991A",
            showsPowerControl: true,
            showsATUTune: true,
            showsIPOToggle: true,
            showsToneControl: true,
            showsRepeaterSetup: true,
            showsNoiseBlanker: true,
            showsNoiseReduction: true,
            squelchRange: 0...100,
            powerRange: 5...100,
            supportedToneModes: [.off, .ctcssEncode, .ctcssEncodeDecode, .dcsEncode, .dcsEncodeDecode],
            connectionProfileTitle: "FT-991A macOS Profil",
            connectionProfileNotes: [
                "CP2105 Enhanced Port wählen",
                "CAT RATE: 38400 bps",
                "CAT TOT: 100 ms",
                "CAT RTS: ON",
                "DTR bleibt aus"
            ],
            connectionProfileSummary: "Wenn mehrere CP2105-Ports erscheinen, ist unter macOS typischerweise der Enhanced-Port der funktionierende CAT-Kanal."
        ),
        toneModeMap: [
            .off: "0",
            .ctcssEncodeDecode: "1",
            .ctcssEncode: "2",
            .dcsEncodeDecode: "3",
            .dcsEncode: "4"
        ]
    )

    static let ftx1Field = RadioProfile(
        displayName: "FTX-1 field",
        serialDeviceName: "FTX-1",
        familyName: "FTX-1",
        capabilities: RadioCapabilities(
            familyName: "FTX-1",
            showsPowerControl: true,
            showsATUTune: false,
            showsIPOToggle: true,
            showsToneControl: true,
            showsRepeaterSetup: true,
            showsNoiseBlanker: true,
            showsNoiseReduction: true,
            squelchRange: 0...255,
            powerRange: 5...10,
            supportedToneModes: [.off, .ctcssEncode, .ctcssEncodeDecode, .dcsEncode],
            connectionProfileTitle: "FTX-1 field CAT Profil",
            connectionProfileNotes: [
                "Enhanced Port = CAT-1, Standard Port = CAT-2",
                "CAT-1/CAT-3 ab Werk meist 38400 bps",
                "CAT-2 ab Werk oft 4800 bps",
                "Für CAT über CAT-2 RTS/DTR-PTT im Menü deaktiviert lassen",
                "USB-Dual-UART meldet beide Ports als CP210x",
                "Field Head liefert mit externer 13.8V-Versorgung bis 10 W"
            ],
            connectionProfileSummary: "FTX-1 field nutzt MAIN/SUB-seitige CAT-Parameter. In dieser App werden MAIN-seitige Funktionen und die 10-W-Field-Variante freigeschaltet."
        ),
        toneModeMap: [
            .off: "0",
            .ctcssEncode: "1",
            .ctcssEncodeDecode: "2",
            .dcsEncode: "3",
            .pagerFrequency: "4",
            .reverseTone: "5"
        ]
    )

    static let ftx1Optima = RadioProfile(
        displayName: "FTX-1 optima",
        serialDeviceName: "FTX-1",
        familyName: "FTX-1",
        capabilities: RadioCapabilities(
            familyName: "FTX-1",
            showsPowerControl: true,
            showsATUTune: true,
            showsIPOToggle: true,
            showsToneControl: true,
            showsRepeaterSetup: true,
            showsNoiseBlanker: true,
            showsNoiseReduction: true,
            squelchRange: 0...255,
            powerRange: 5...100,
            supportedToneModes: [.off, .ctcssEncode, .ctcssEncodeDecode, .dcsEncode],
            connectionProfileTitle: "FTX-1 optima CAT Profil",
            connectionProfileNotes: [
                "Enhanced Port = CAT-1, Standard Port = CAT-2",
                "CAT-1/CAT-3 ab Werk meist 38400 bps",
                "CAT-2 ab Werk oft 4800 bps",
                "Für CAT über CAT-2 RTS/DTR-PTT im Menü deaktiviert lassen",
                "USB-Dual-UART meldet beide Ports als CP210x",
                "optima nutzt SPA-1 und interne Tuner-/Power-Funktionen"
            ],
            connectionProfileSummary: "FTX-1 optima wird in der App als 100-W-Basisprofil mit Tuner-Unterstützung behandelt."
        ),
        toneModeMap: [
            .off: "0",
            .ctcssEncode: "1",
            .ctcssEncodeDecode: "2",
            .dcsEncode: "3",
            .pagerFrequency: "4",
            .reverseTone: "5"
        ]
    )
}

struct RadioCapabilities {
    let familyName: String
    let showsPowerControl: Bool
    let showsATUTune: Bool
    let showsIPOToggle: Bool
    let showsToneControl: Bool
    let showsRepeaterSetup: Bool
    let showsNoiseBlanker: Bool
    let showsNoiseReduction: Bool
    let squelchRange: ClosedRange<Int>
    let powerRange: ClosedRange<Int>
    let supportedToneModes: [ToneMode]
    let connectionProfileTitle: String
    let connectionProfileNotes: [String]
    let connectionProfileSummary: String
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

// MARK: - Tone Mode

enum ToneMode: String, Codable, CaseIterable {
    case off = "Aus"
    case ctcssEncodeDecode = "CTCSS Enc+Dec"
    case ctcssEncode = "CTCSS Enc"
    case dcsEncodeDecode = "DCS Enc+Dec"
    case dcsEncode = "DCS Enc"
    case pagerFrequency = "PR Freq"
    case reverseTone = "Rev Tone"

    func catValue(for model: RadioModel) -> String? {
        model.toneModeMap[self]
    }

    static func from(catValue: String, model: RadioModel) -> ToneMode? {
        model.reverseToneModeMap[catValue]
    }

    var usesDCS: Bool {
        switch self {
        case .dcsEncode, .dcsEncodeDecode:
            return true
        default:
            return false
        }
    }

    var usesTone: Bool {
        switch self {
        case .off, .pagerFrequency, .reverseTone:
            return false
        default:
            return true
        }
    }
}

// MARK: - Tone Catalog

enum ToneCatalog {
    static let ctcssFrequencies: [Double] = [
        67.0, 69.3, 71.9, 74.4, 77.0, 79.7, 82.5, 85.4, 88.5, 91.5,
        94.8, 97.4, 100.0, 103.5, 107.2, 110.9, 114.8, 118.8, 123.0, 127.3,
        131.8, 136.5, 141.3, 146.2, 151.4, 156.7, 159.8, 162.2, 165.5, 167.9,
        171.3, 173.8, 177.3, 179.9, 183.5, 186.2, 189.9, 192.8, 196.6, 199.5,
        203.5, 206.5, 210.7, 218.1, 225.7, 229.1, 233.6, 241.8, 250.3, 254.1
    ]

    static let dcsCodes: [Int] = [
        23, 25, 26, 31, 32, 36, 43, 47, 51, 53,
        54, 65, 71, 72, 73, 74, 114, 115, 116, 122,
        125, 131, 132, 134, 143, 145, 152, 155, 156, 162,
        165, 172, 174, 205, 212, 223, 225, 226, 243, 244,
        245, 246, 251, 252, 255, 261, 263, 265, 266, 271,
        274, 306, 311, 315, 325, 331, 332, 343, 346, 351,
        356, 364, 365, 371, 411, 412, 413, 423, 431, 432,
        445, 446, 452, 454, 455, 462, 464, 465, 466, 503,
        506, 516, 523, 526, 532, 546, 565, 606, 612, 624,
        627, 631, 632, 654, 662, 664, 703, 712, 723, 731,
        732, 734, 743, 754
    ]
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

// MARK: - Scan Direction

enum ScanDirection: String, Codable {
    case down = "Down"
    case up = "Up"
}

// MARK: - Repeater Shift

enum RepeaterShiftDirection: String, Codable, CaseIterable {
    case off = "Off"
    case minus = "-"
    case plus = "+"

    var signedMultiplier: Int {
        switch self {
        case .off: return 0
        case .minus: return -1
        case .plus: return 1
        }
    }

    var displayName: String {
        switch self {
        case .off: return "Simplex"
        case .minus: return "- Shift"
        case .plus: return "+ Shift"
        }
    }
}

// MARK: - Memory Entry

struct MemoryEntry: Identifiable, Codable, Equatable {
    let id: UUID
    var slot: Int
    var name: String
    var frequency: Int
    var mode: OperatingMode
    var splitEnabled: Bool
    var vfoBFrequency: Int?
    var repeaterOffsetHz: Int
    var repeaterShift: RepeaterShiftDirection
    var power: Int
    var toneMode: ToneMode
    var ctcssToneIndex: Int
    var dcsCodeIndex: Int
    var createdAt: Date
    var updatedAt: Date

    enum CodingKeys: String, CodingKey {
        case id
        case slot
        case name
        case frequency
        case mode
        case splitEnabled
        case vfoBFrequency
        case repeaterOffsetHz
        case repeaterShift
        case power
        case toneMode
        case ctcssToneIndex
        case dcsCodeIndex
        case createdAt
        case updatedAt
    }

    init(
        id: UUID = UUID(),
        slot: Int,
        name: String,
        frequency: Int,
        mode: OperatingMode,
        splitEnabled: Bool,
        vfoBFrequency: Int?,
        repeaterOffsetHz: Int,
        repeaterShift: RepeaterShiftDirection,
        power: Int,
        toneMode: ToneMode,
        ctcssToneIndex: Int,
        dcsCodeIndex: Int,
        createdAt: Date = Date(),
        updatedAt: Date = Date()
    ) {
        self.id = id
        self.slot = slot
        self.name = name
        self.frequency = frequency
        self.mode = mode
        self.splitEnabled = splitEnabled
        self.vfoBFrequency = vfoBFrequency
        self.repeaterOffsetHz = repeaterOffsetHz
        self.repeaterShift = repeaterShift
        self.power = power
        self.toneMode = toneMode
        self.ctcssToneIndex = ctcssToneIndex
        self.dcsCodeIndex = dcsCodeIndex
        self.createdAt = createdAt
        self.updatedAt = updatedAt
    }

    var frequencyDisplay: String {
        let mhz = frequency / 1_000_000
        let khz = (frequency % 1_000_000) / 1_000
        let hz = frequency % 1_000
        return String(format: "%d.%03d.%03d", mhz, khz, hz)
    }

    var offsetDisplay: String {
        guard repeaterShift != .off, repeaterOffsetHz > 0 else {
            return splitEnabled ? "Split" : "Simplex"
        }

        let mhz = Double(repeaterOffsetHz) / 1_000_000.0
        return String(format: "%@ %.3f MHz", repeaterShift.rawValue, mhz)
    }

    var toneDisplay: String {
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

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        id = try container.decodeIfPresent(UUID.self, forKey: .id) ?? UUID()
        slot = try container.decode(Int.self, forKey: .slot)
        name = try container.decode(String.self, forKey: .name)
        frequency = try container.decode(Int.self, forKey: .frequency)
        mode = try container.decode(OperatingMode.self, forKey: .mode)
        splitEnabled = try container.decodeIfPresent(Bool.self, forKey: .splitEnabled) ?? false
        vfoBFrequency = try container.decodeIfPresent(Int.self, forKey: .vfoBFrequency)
        repeaterOffsetHz = try container.decodeIfPresent(Int.self, forKey: .repeaterOffsetHz) ?? 0
        repeaterShift = try container.decodeIfPresent(RepeaterShiftDirection.self, forKey: .repeaterShift) ?? .off
        power = try container.decodeIfPresent(Int.self, forKey: .power) ?? 100
        toneMode = try container.decodeIfPresent(ToneMode.self, forKey: .toneMode) ?? .off
        ctcssToneIndex = try container.decodeIfPresent(Int.self, forKey: .ctcssToneIndex) ?? 0
        dcsCodeIndex = try container.decodeIfPresent(Int.self, forKey: .dcsCodeIndex) ?? 0
        createdAt = try container.decodeIfPresent(Date.self, forKey: .createdAt) ?? Date()
        updatedAt = try container.decodeIfPresent(Date.self, forKey: .updatedAt) ?? Date()
    }
}
