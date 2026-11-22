// Kerntests MacYaesu (Tools/tests.sh). Sinngemäss portiert aus der Windows-Fassung
// (ft991a-remote-win, src-tauri/src/kern/steuerung/tests.rs, Region „Ruecksprung“, und
// tx_ueberholt_rueckstau_und_tx0_verwirft_wartendes_tx1), dazu Trennen und 70-cm-Relaisvorgabe.
import Foundation
import Combine

// MARK: - Testgerüst

var fehlschlaege: [String] = []
var aktuellerTest = ""
var anzahlTests = 0

func pruefe(_ bedingung: @autoclosure () -> Bool, _ text: String, line: Int = #line) {
    if !bedingung() {
        fehlschlaege.append("\(aktuellerTest) (Zeile \(line)): \(text)")
    }
}

func gleich<T: Equatable>(_ ist: T, _ soll: T, _ text: String = "", line: Int = #line) {
    pruefe(ist == soll, "\(text) ist \(ist), soll \(soll)", line: line)
}

func test(_ name: String, _ koerper: () -> Void) {
    aktuellerTest = name
    anzahlTests += 1
    let vorher = fehlschlaege.count
    koerper()
    print(fehlschlaege.count == vorher ? "ok      \(name)" : "ROT     \(name)")
}

/// Hauptwarteschlange abarbeiten (CATProtocol schreibt den Zustand per main.async)
func abarbeiten(_ sekunden: TimeInterval = 0.02) {
    RunLoop.main.run(until: Date().addingTimeInterval(sekunden))
}

// MARK: - Attrappe der seriellen Verbindung

final class FakeSerial: SerialPortServiceType {
    @Published var connectionState: ConnectionState = .disconnected
    @Published var availablePorts: [SerialPort] = []
    @Published var selectedPortPath: String = ""
    var baudRate = 38400
    var radioModel: RadioModel = .ft991a
    var searchUSBPortsUntilFound = false
    var lastError: String?
    @Published var lastResponseAt: Date?
    @Published var hasCATResponse = false
    @Published var currentPortSearchIndex = 0
    @Published var currentPortSearchTotal = 0
    @Published var bytesSent: UInt64 = 0
    @Published var bytesReceived: UInt64 = 0
    var onDataReceived: ((Data) -> Void)?
    var onConnectionChanged: ((Bool) -> Void)?
    /// false: die Startreihe (requestInitialState) sendet nichts dazwischen
    var isConnected: Bool { false }

    var connectionStatePublisher: Published<ConnectionState>.Publisher { $connectionState }
    var availablePortsPublisher: Published<[SerialPort]>.Publisher { $availablePorts }
    var selectedPortPathPublisher: Published<String>.Publisher { $selectedPortPath }
    var bytesSentPublisher: Published<UInt64>.Publisher { $bytesSent }
    var bytesReceivedPublisher: Published<UInt64>.Publisher { $bytesReceived }
    var hasCATResponsePublisher: Published<Bool>.Publisher { $hasCATResponse }
    var lastResponseAtPublisher: Published<Date?>.Publisher { $lastResponseAt }
    var currentPortSearchIndexPublisher: Published<Int>.Publisher { $currentPortSearchIndex }
    var currentPortSearchTotalPublisher: Published<Int>.Publisher { $currentPortSearchTotal }

    private(set) var gesendet: [String] = []

    func refreshPorts() {}
    func connect() {}
    func disconnect() {}
    func send(_ command: CATCommand) { gesendet.append(command.command) }
    func noteCATResponse() {}

    /// Antwort des Geräts, wie sie der Port meldet (ein Rahmen mit `;`)
    func antwort(_ text: String) {
        onDataReceived?(Data((text.hasSuffix(";") ? text : text + ";").utf8))
    }
}

final class Uhr {
    var t: TimeInterval = 1000
}

/// Steuerung mit Attrappe und eigener Uhr; Abfrage-Zeitgeber laufen erst nach 0,9 s und stören die kurzen Tests nicht.
func steuerung() -> (CATProtocol, FakeSerial, Uhr) {
    let serial = FakeSerial()
    let cat = CATProtocol(serialManager: serial)
    let uhr = Uhr()
    cat.uhr = { uhr.t }
    return (cat, serial, uhr)
}

// MARK: - Kein Rücksprung nach dem Setzen

test("frequenz_alte_antwort_springt_nicht_zurueck_und_folgeschritt_stimmt") {
    let (cat, serial, _) = steuerung()
    cat.radioState.vfoAFrequency = 7_100_000
    cat.send(CAT.readVFOA)                     // Abfragezyklus, steht vor dem Setzen in der Schlange
    cat.setFrequency(14_200_000, vfo: .a)
    serial.antwort("FA007100000")              // Antwort auf die alte Abfrage kommt danach
    abarbeiten()
    gleich(cat.radioState.vfoAFrequency, 14_200_000, "VFO-A nach alter Antwort")
    cat.changeFrequency(by: 1_000)             // Mausrad-Schritt
    gleich(serial.gesendet.last ?? "", "FA014201000", "Folgeschritt")
}

test("af_pegel_bleibt_nach_alter_antwort") {
    let (cat, serial, _) = steuerung()
    cat.send(CAT.readAFGain(for: .ft991a))
    cat.setAFGain(50)
    serial.antwort("AG0110")
    abarbeiten()
    gleich(cat.radioState.afGain, 50, "AF")
}

test("mode_und_nb_bleiben_nach_alter_antwort") {
    let (cat, serial, _) = steuerung()
    cat.radioState.mode = .usb
    cat.send(CAT.readMode(for: .ft991a))
    cat.send(CAT.readNB(for: .ft991a))
    cat.setMode(.cw)
    cat.toggleNB()
    serial.antwort("MD02")
    serial.antwort("NB00")
    abarbeiten()
    gleich(cat.radioState.mode, .cw, "Mode")
    gleich(cat.radioState.noiseBlanker, true, "NB")
}

test("vfo_tausch_sv_und_ab_treffen_fa_und_fb") {
    let (cat, serial, _) = steuerung()
    cat.radioState.vfoAFrequency = 7_100_000
    cat.radioState.vfoBFrequency = 14_200_000
    cat.send(CAT.readVFOA)
    cat.send(CAT.readVFOB)
    cat.swapVFO()                              // SV: A = 14,2, B = 7,1
    serial.antwort("FA007100000")
    serial.antwort("FB014200000")
    abarbeiten()
    gleich(cat.radioState.vfoAFrequency, 14_200_000, "A nach SV")
    gleich(cat.radioState.vfoBFrequency, 7_100_000, "B nach SV")
    cat.send(CAT.readVFOB)
    cat.equalizeVFO()                          // AB: B = A = 14,2
    serial.antwort("FB007100000")
    abarbeiten()
    gleich(cat.radioState.vfoBFrequency, 14_200_000, "B nach AB")
}

test("abgelehnter_wert_kommt_mit_der_ersten_abfrage_nach_dem_setzen_zurueck") {
    let (cat, serial, _) = steuerung()
    cat.setFrequency(14_200_000, vfo: .a)
    cat.send(CAT.readVFOA)                     // Abfrage nach dem Setzen
    serial.antwort("FA007100000")              // Gerät hat abgelehnt
    abarbeiten()
    gleich(cat.radioState.vfoAFrequency, 7_100_000, "abgelehnter Wert")
}

test("nur_antworten_von_vorher_werden_verworfen") {
    let (cat, serial, _) = steuerung()
    cat.send(CAT.readVFOA)
    cat.setFrequency(14_200_000, vfo: .a)
    cat.send(CAT.readVFOA)
    serial.antwort("FA007100000")              // gehört zur Abfrage von vorher
    abarbeiten()
    gleich(cat.radioState.vfoAFrequency, 14_200_000, "nach erster Antwort")
    serial.antwort("FA014205000")              // gehört zur Abfrage danach (am Gerät verstellt)
    abarbeiten()
    gleich(cat.radioState.vfoAFrequency, 14_205_000, "nach zweiter Antwort")
    gleich(cat.ausstehendeAbfragen, 0, "ausstehend")
}

test("ausbleibende_antwort_verfaellt_nach_der_frist") {
    let (cat, serial, uhr) = steuerung()
    cat.send(CAT.readVFOA)                     // wird nie beantwortet (?; oder Überlauf)
    cat.setFrequency(14_200_000, vfo: .a)
    uhr.t += 11                                // Frist 10 s, fest im Test (nicht aus der Konstante)
    cat.send(CAT.readVFOA)
    serial.antwort("FA014210000")              // frische Antwort
    abarbeiten()
    gleich(cat.radioState.vfoAFrequency, 14_210_000, "frische Antwort nach Frist")
}

test("frist_zaehlt_ab_dem_einreihen_nicht_kuerzer") {
    let (cat, serial, uhr) = steuerung()
    cat.send(CAT.readVFOA)
    cat.setFrequency(14_200_000, vfo: .a)
    uhr.t += 3.5                               // Rückstau von über 3 s wie im Klicktest der Windows-Fassung
    serial.antwort("FA007100000")
    abarbeiten()
    gleich(cat.radioState.vfoAFrequency, 14_200_000, "alte Antwort nach 3,5 s")
}

test("klickfolge_mit_rueckstau_endet_beim_letzten_wert") {
    let (cat, serial, uhr) = steuerung()
    cat.radioState.vfoAFrequency = 14_200_000
    var antworten: [String] = []
    for i in 1...8 {
        cat.send(CAT.readVFOA)                 // Abfrage im Zyklus
        antworten.append(String(format: "FA%09d", 14_200_000 + (i - 1) * 1_000))
        cat.changeFrequency(by: 1_000)
        cat.send(CAT.readSMeter)
        uhr.t += 0.1
    }
    for a in antworten {
        serial.antwort(a)
        serial.antwort("SM0120")
    }
    abarbeiten()
    gleich(cat.radioState.vfoAFrequency, 14_208_000, "nach 8 Schritten")
    gleich(serial.gesendet.filter { $0.hasPrefix("FA0") }.last ?? "", "FA014208000", "letzter Setzbefehl")
}

test("setzen_gleich_nach_dem_handshake") {
    let (cat, serial, _) = steuerung()
    cat.verbindungGeaendert(true)              // Handshake-FA; ging an send vorbei
    cat.setFrequency(14_200_000, vfo: .a)
    serial.antwort("FA007100000")              // Handshake-Antwort kommt erst jetzt
    abarbeiten()
    gleich(cat.radioState.vfoAFrequency, 14_200_000, "nach Handshake-Antwort")
    cat.verbindungGeaendert(false)
}

test("trennen_und_verbinden_leeren_das_buch") {
    let (cat, serial, _) = steuerung()
    cat.send(CAT.readVFOA)
    cat.setFrequency(14_200_000, vfo: .a)      // Abfrage veraltet, Antwort kommt nie
    cat.verbindungGeaendert(false)
    gleich(cat.ausstehendeAbfragen, 0, "nach Trennen")
    cat.verbindungGeaendert(true)
    gleich(cat.ausstehendeAbfragen, 1, "nach Verbinden (Handshake-FA)")
    serial.antwort("FA014250000")              // Antwort auf den neuen Handshake
    abarbeiten()
    gleich(cat.radioState.vfoAFrequency, 14_250_000, "nach Wiederverbinden")
    cat.verbindungGeaendert(false)
}

test("unverlangte_antwort_und_rohbefehl") {
    let (cat, serial, _) = steuerung()
    serial.antwort("FA003573000")              // ohne vermerkte Abfrage (z. B. Auto-Info)
    abarbeiten()
    gleich(cat.radioState.vfoAFrequency, 3_573_000, "unverlangt")
    cat.sendRaw("FA;")                         // Rohbefehl aus der Konsole zählt als Abfrage
    cat.setFrequency(7_074_000, vfo: .a)
    serial.antwort("FA003573000")
    abarbeiten()
    gleich(cat.radioState.vfoAFrequency, 7_074_000, "Rohbefehl vor dem Setzen")
}

test("ptt_alte_tx_antwort_schaltet_nicht_zurueck") {
    let (cat, serial, _) = steuerung()
    cat.send(CAT.readTXStatus)
    cat.startTransmit(dataMode: false)
    serial.antwort("TX0")
    abarbeiten()
    gleich(cat.radioState.isTransmitting, true, "TX nach alter Antwort")
}

// MARK: - Abfragebuch direkt

test("abfragebuch_schluessel") {
    gleich(CATAbfrageBuch.schluessel("FA014200000"), "FA")
    gleich(CATAbfrageBuch.schluessel("CN00012;"), "CN00")
    gleich(CATAbfrageBuch.schluessel("CO01"), "CO01")
    gleich(CATAbfrageBuch.betroffeneSchluessel("SV"), ["FA", "FB"])
    gleich(CATAbfrageBuch.betroffeneSchluessel("AB;"), ["FB"])
    var buch = CATAbfrageBuch()
    buch.vermerken("CN00", jetzt: 0)
    buch.vermerken("CN01", jetzt: 0)
    buch.vermerken("CN00015", jetzt: 0)        // setzt nur den CTCSS-Ton
    gleich(buch.antwortVeraltet("CN01023", jetzt: 0), false, "DCS")
    gleich(buch.antwortVeraltet("CN00012", jetzt: 0), true, "CTCSS")
}

// MARK: - PTT überholt den Rückstau

func daten(_ text: String) -> Data { Data((text + ";").utf8) }
func texte(_ s: CATAusgangsschlange) -> [String] {
    s.befehle.map { String(decoding: $0, as: UTF8.self).replacingOccurrences(of: ";", with: "") }
}

test("tx_ueberholt_rueckstau_und_tx0_verwirft_wartendes_tx1") {
    var s = CATAusgangsschlange()
    for i in 0..<24 { s.einreihen(daten(i % 2 == 0 ? "SM0" : String(format: "FA%09d", 14_200_000 + i))) }
    s.einreihen(daten("TX1"))
    gleich(texte(s).first ?? "", "TX1", "TX1 vorne")
    gleich(s.anzahl, 25)
    s.einreihen(daten("TX0"))
    gleich(texte(s).first ?? "", "TX0", "TX0 vorne")
    gleich(texte(s).filter { $0 == "TX1" }.count, 0, "TX1 verworfen")
    gleich(s.anzahl, 25)
    gleich(texte(s)[1], "SM0", "Rückstau bleibt in Reihenfolge")
}

test("tx_aus_dann_ein_im_rueckstau_endet_mit_sender_ein") {
    var s = CATAusgangsschlange()
    s.einreihen(daten("SM0"))
    s.einreihen(daten("TX0"))                  // Shift los ...
    s.einreihen(daten("TX1"))                  // ... und 110 ms später wieder gedrückt
    gleich(texte(s), ["TX1", "SM0"], "nur der letzte Senderbefehl, vorne")
    s.einreihen(daten("TX2"))
    gleich(texte(s), ["TX2", "SM0"])
    s.einreihen(daten("TX0"))
    gleich(texte(s), ["TX0", "SM0"])
}

test("trennen_schreibt_wartendes_tx0_und_verwirft_den_rest") {
    var s = CATAusgangsschlange()
    s.einreihen(daten("SM0"))
    s.einreihen(daten("TX0"))
    gleich(s.trennen().map { String(decoding: $0, as: UTF8.self) } ?? "", "TX0;")
    gleich(s.anzahl, 0)
    s.einreihen(daten("SM0"))
    s.einreihen(daten("TX1"))
    pruefe(s.trennen() == nil, "ohne TX0 nichts sofort schreiben")
}

test("trennen_beim_senden_schickt_tx0") {
    let (cat, serial, _) = steuerung()
    cat.senderAusVorDemTrennen()
    gleich(serial.gesendet, [], "ohne Senden kein TX0")
    cat.startTransmit(dataMode: false)
    cat.senderAusVorDemTrennen()
    gleich(serial.gesendet, ["TX1", "TX0"])
    gleich(cat.radioState.isTransmitting, false)
}

test("port_tx1_ueberholt_sechs_wartende_abfragen") { portTestTxUeberholt() }
test("port_trennen_schreibt_tx0_ohne_rueckstau_100_laeufe") { portTestTrennenNachTx0(laeufe: 100, rueckstau: 0) }
test("port_trennen_schreibt_tx0_mit_rueckstau_50_laeufe") { portTestTrennenNachTx0(laeufe: 50, rueckstau: 5) }

// MARK: - 70 cm Relais

test("relais_70cm_standard_minus_7_6_mhz") {
    gleich(RepeaterPreset.fm70cm.shift, .minus)
    gleich(RepeaterPreset.fm70cm.offsetHz, 7_600_000)
    gleich(RepeaterPreset.fm70cm.sendefrequenz(zu: 438_650_000), 431_050_000)
    gleich(RepeaterPreset.fm2m.sendefrequenz(zu: 145_750_000), 145_150_000)
}

// MARK: - Ergebnis

print("")
if fehlschlaege.isEmpty {
    print("\(anzahlTests) Tests, alle gruen")
    exit(0)
} else {
    print("\(anzahlTests) Tests, \(fehlschlaege.count) Fehler:")
    fehlschlaege.forEach { print("  " + $0) }
    exit(1)
}
