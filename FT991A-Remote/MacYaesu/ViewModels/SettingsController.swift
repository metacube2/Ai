//
//  SettingsController.swift
//  FT991A-Remote
//
//  Application settings controller
//

import Foundation
import CryptoKit
import SwiftUI

@MainActor
final class SettingsController: ObservableObject {
    @Published var uiStyle: UIStyle = .modern { didSet { saveSettings() } }
    @Published var language: AppLanguage = .german { didSet { saveSettings() } }
    @Published var compactMode: Bool = true { didSet { saveSettings() } }
    @Published var showDebugPanel: Bool = false { didSet { saveSettings() } }
    @Published var showLogPanel: Bool = false { didSet { saveSettings() } }
    @Published var showParrotPanel: Bool = false { didSet { saveSettings() } }
    @Published var radioWorkspaceWidth: Double = 980 { didSet { saveSettings() } }

    @Published var selectedRadioModel: RadioModel = .ft991a { didSet { saveSettings() } }
    @Published var autoReconnect: Bool = true { didSet { saveSettings() } }
    @Published var reconnectInterval: TimeInterval = 5.0 { didSet { saveSettings() } }
    @Published var defaultBaudRate: Int = 38400 { didSet { saveSettings() } }
    @Published var autoConnectOnLaunch: Bool = false { didSet { saveSettings() } }
    @Published var searchUSBPortsUntilFound: Bool = false { didSet { saveSettings() } }
    @Published var preferredSerialPort: String = "" { didSet { saveSettings() } }
    @Published var setupCompleted: Bool = false { didSet { saveSettings() } }

    @Published var frequencyStep: FrequencyStep = .khz1 { didSet { saveSettings() } }

    @Published var logDirectory: String = "~/Documents/FT991A-Logs/" { didSet { saveSettings() } }
    @Published var autoSaveLog: Bool = true { didSet { saveSettings() } }

    @Published var audioInputDevice: String = "" { didSet { saveSettings() } }
    @Published var audioOutputDevice: String = "" { didSet { saveSettings() } }
    @Published var parrotInputDeviceUID: String = "" { didSet { saveSettings() } }
    @Published var parrotOutputDeviceUID: String = "" { didSet { saveSettings() } }
    @Published var useBlackHole: Bool = false { didSet { saveSettings() } }

    @Published var pttShortcutEnabled: Bool = true { didSet { saveSettings() } }
    @Published var arrowFrequencyEnabled: Bool = true { didSet { saveSettings() } }
    @Published var tunerShortcutEnabled: Bool = true { didSet { saveSettings() } }

    @Published var licenseEmail: String = "" { didSet { saveSettings() } }
    @Published var licenseKey: String = "" { didSet { saveSettings() } }
    @Published var isActivated: Bool = false { didSet { saveSettings() } }
    @Published var activationErrorMessage: String?
    @Published var trialSecondsRemaining: Int = SettingsController.trialDurationSeconds
    @Published var isTrialExpired: Bool = false

    private var settings: AppSettings
    private var saveDebounce: Timer?
    private var activeSince: Date?

    static let availableBaudRates = [4800, 9600, 19200, 38400, 57600, 115200]
    private static let trialDurationSeconds = 15 * 60
    /// Tester-Fassung (Compile-Schalter MACYAESU_TESTER, Tools/verteilen.sh --tester): aktiviert ohne Schluessel,
    /// fuer www.aiscom.ch/tester/. Nie verkaufen.
    #if MACYAESU_TESTER
    static let istTester = true
    #else
    static let istTester = false
    #endif

    init() {
        settings = AppSettings.load()
        loadFromSettings()
        recalculateTrialState()
    }

    var expandedLogDirectory: String {
        (logDirectory as NSString).expandingTildeInPath
    }

    func handleScenePhase(_ phase: ScenePhase) {
        switch phase {
        case .active:
            activeSince = Date()
        case .inactive, .background:
            consumeTrialIfNeeded()
            flushSettings()
        @unknown default:
            flushSettings()
        }
    }

    func flushSettings() {
        saveDebounce?.invalidate()
        saveDebounce = nil
        performSave()
    }

    func rememberConnectionPreferences(portPath: String, baudRate: Int) {
        preferredSerialPort = portPath
        defaultBaudRate = baudRate
    }

    func submitLicense(email: String, rawKey: String) -> Bool {
        let normalizedEmail = Self.normalizeLicenseEmail(email)
        let normalizedKey = Self.normalizeLicenseKey(rawKey)

        guard !normalizedEmail.isEmpty, !normalizedKey.isEmpty else {
            activationErrorMessage = "E-Mail und Lizenzschlüssel dürfen nicht leer sein."
            return false
        }

        guard !LizenzPruefung.istAltesFormat(normalizedKey) else {
            activationErrorMessage = LizenzPruefung.meldungAltesFormat
            return false
        }

        guard Self.isValidLicense(email: normalizedEmail, key: normalizedKey) else {
            activationErrorMessage = "Der Lizenzschlüssel passt nicht zu dieser E-Mail-Adresse. Bitte den Schlüssel vollständig aus der E-Mail einfügen."
            return false
        }

        licenseEmail = normalizedEmail
        licenseKey = normalizedKey
        isActivated = true
        activationErrorMessage = nil
        recalculateTrialState()
        return true
    }

    private func loadFromSettings() {
        uiStyle = settings.uiStyle
        language = settings.language
        compactMode = settings.compactMode
        showDebugPanel = settings.showDebugPanel
        showLogPanel = settings.showLogPanel
        showParrotPanel = settings.showParrotPanel
        radioWorkspaceWidth = settings.radioWorkspaceWidth

        selectedRadioModel = settings.radioModel
        autoReconnect = settings.autoReconnect
        reconnectInterval = settings.reconnectInterval
        defaultBaudRate = settings.baudRate
        autoConnectOnLaunch = settings.autoConnectOnLaunch
        searchUSBPortsUntilFound = settings.searchUSBPortsUntilFound
        preferredSerialPort = settings.serialPort
        setupCompleted = settings.setupCompleted

        frequencyStep = settings.frequencyStep
        logDirectory = settings.logDirectory
        autoSaveLog = settings.autoSaveLog

        audioInputDevice = settings.audioInputDevice
        audioOutputDevice = settings.audioOutputDevice
        parrotInputDeviceUID = settings.parrotInputDeviceUID
        parrotOutputDeviceUID = settings.parrotOutputDeviceUID
        useBlackHole = settings.useBlackHole

        pttShortcutEnabled = settings.pttShortcutEnabled
        arrowFrequencyEnabled = settings.arrowFrequencyEnabled
        tunerShortcutEnabled = settings.tunerShortcutEnabled

        licenseEmail = Self.normalizeLicenseEmail(settings.licenseEmail)
        licenseKey = Self.normalizeLicenseKey(settings.licenseKey)
        isActivated = Self.istTester || Self.isValidLicense(email: licenseEmail, key: licenseKey)
        // Gespeicherter Schluessel aus dem alten Verfahren: erklaeren statt still in die Testversion fallen
        if !isActivated && LizenzPruefung.istAltesFormat(licenseKey) {
            activationErrorMessage = LizenzPruefung.meldungAltesFormat
        }
    }

    private func saveSettings() {
        saveDebounce?.invalidate()
        saveDebounce = Timer.scheduledTimer(withTimeInterval: 0.35, repeats: false) { [weak self] _ in
            self?.performSave()
        }
    }

    private func performSave() {
        settings.uiStyle = uiStyle
        settings.language = language
        settings.compactMode = compactMode
        settings.showDebugPanel = showDebugPanel
        settings.showLogPanel = showLogPanel
        settings.showParrotPanel = showParrotPanel
        settings.radioWorkspaceWidth = radioWorkspaceWidth

        settings.radioModel = selectedRadioModel
        settings.autoReconnect = autoReconnect
        settings.reconnectInterval = reconnectInterval
        settings.baudRate = defaultBaudRate
        settings.autoConnectOnLaunch = autoConnectOnLaunch
        settings.searchUSBPortsUntilFound = searchUSBPortsUntilFound
        settings.serialPort = preferredSerialPort
        settings.setupCompleted = setupCompleted

        settings.frequencyStep = frequencyStep
        settings.logDirectory = logDirectory
        settings.autoSaveLog = autoSaveLog

        settings.audioInputDevice = audioInputDevice
        settings.audioOutputDevice = audioOutputDevice
        settings.parrotInputDeviceUID = parrotInputDeviceUID
        settings.parrotOutputDeviceUID = parrotOutputDeviceUID
        settings.useBlackHole = useBlackHole

        settings.pttShortcutEnabled = pttShortcutEnabled
        settings.arrowFrequencyEnabled = arrowFrequencyEnabled
        settings.tunerShortcutEnabled = tunerShortcutEnabled

        settings.licenseEmail = licenseEmail
        settings.licenseKey = licenseKey
        settings.isActivated = isActivated

        settings.ensureLogDirectoryExists()
        settings.save()
    }

    private func consumeTrialIfNeeded() {
        guard !isActivated, let activeSince else {
            self.activeSince = nil
            return
        }

        let elapsed = max(0, Int(Date().timeIntervalSince(activeSince)))
        settings.trialConsumedSeconds += elapsed
        self.activeSince = nil
        recalculateTrialState()
    }

    private func recalculateTrialState() {
        if isActivated {
            trialSecondsRemaining = Self.trialDurationSeconds
            isTrialExpired = false
            return
        }

        let consumed = max(0, settings.trialConsumedSeconds)
        trialSecondsRemaining = max(0, Self.trialDurationSeconds - consumed)
        isTrialExpired = trialSecondsRemaining == 0
    }

    private static func normalizeLicenseEmail(_ raw: String) -> String {
        raw.trimmingCharacters(in: .whitespacesAndNewlines).lowercased()
    }

    private static func normalizeLicenseKey(_ raw: String) -> String {
        LizenzPruefung.normalisiert(raw)
    }

    private static func isValidLicense(email: String, key: String) -> Bool {
        LizenzPruefung.istGueltig(email: email, schluessel: key)
    }
}

// MARK: Anfang Lizenzpruefung
/// Lizenzpruefung per Ed25519 (seit 2026-11-06). Der Schluessel ist die Signatur ueber „macyaesu|<e-mail>“
/// (E-Mail getrimmt und kleingeschrieben), Base32 nach RFC 4648 ohne Auffuellung, in Vierergruppen mit „-“.
/// Die App enthaelt nur den oeffentlichen Schluessel; ausgestellt wird mit
/// `~/Projects/Git/aiscom-lizenz/lizenz.swift ausstellen macyaesu <e-mail>` (privater Schluessel im Schluesselbund).
enum LizenzPruefung {
    static let oeffentlicherSchluesselBase64 = "TuijD63uHjx0Oinqsu/3jwzcee/IoVj6JxpTC45aNzM="
    static let meldungAltesFormat = "Dieser Schlüssel stammt aus dem alten Verfahren und gilt nicht mehr. Bitte einen neuen Schlüssel anfordern: metacube@gmail.com"

    private static let alphabet = Array("ABCDEFGHIJKLMNOPQRSTUVWXYZ234567")

    /// Nur Buchstaben und Ziffern, gross geschrieben – Bindestriche, Leerzeichen, Zeilenumbrueche fallen weg.
    /// Buchstaben ausserhalb des Base32-Alphabets bleiben stehen und machen den Schluessel ungueltig.
    static func bereinigt(_ roh: String) -> String {
        String(roh.uppercased().filter { $0.isLetter || $0.isNumber })
    }

    /// Einheitliche Schreibweise zum Speichern und Anzeigen: Vierergruppen mit „-“.
    static func normalisiert(_ roh: String) -> String {
        let zeichen = Array(bereinigt(roh))
        return stride(from: 0, to: zeichen.count, by: 4)
            .map { String(zeichen[$0..<min($0 + 4, zeichen.count)]) }
            .joined(separator: "-")
    }

    /// Altes Verfahren: XXXX-XXXX-XXXX-XXXX, 16 Hex-Zeichen.
    static func istAltesFormat(_ roh: String) -> Bool {
        let b = bereinigt(roh)
        return b.count == 16 && b.allSatisfy { $0.isHexDigit }
    }

    /// Base32 (RFC 4648) ohne Auffuellung. Nil bei fremden Zeichen oder gesetzten Restbits (nur eine Schreibweise gilt).
    static func entbase32(_ roh: String) -> Data? {
        var puffer = 0, bits = 0, aus = Data()
        for c in bereinigt(roh) {
            guard let wert = alphabet.firstIndex(of: c) else { return nil }
            puffer = ((puffer << 5) | wert) & 0xFFFF
            bits += 5
            if bits >= 8 {
                aus.append(UInt8((puffer >> (bits - 8)) & 0xFF))
                bits -= 8
            }
        }
        guard bits < 5, puffer & ((1 << bits) - 1) == 0 else { return nil }
        return aus
    }

    static func istGueltig(email: String, schluessel: String) -> Bool {
        let normEmail = email.trimmingCharacters(in: .whitespacesAndNewlines).lowercased()
        guard !normEmail.isEmpty,
              let signatur = entbase32(schluessel), signatur.count == 64,
              let rohSchluessel = Data(base64Encoded: oeffentlicherSchluesselBase64),
              let oeffentlich = try? Curve25519.Signing.PublicKey(rawRepresentation: rohSchluessel)
        else { return false }
        return oeffentlich.isValidSignature(signatur, for: Data(("macyaesu|" + normEmail).utf8))
    }
}
// MARK: Ende Lizenzpruefung
