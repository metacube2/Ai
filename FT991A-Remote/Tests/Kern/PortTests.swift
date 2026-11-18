// Schreibweg des SerialPortManager über ein pty (Naht testPortUebernehmen, nur mit -D KERNTEST):
// send → Ausgangsschlange → writeQueue → write, und TX0 beim Trennen.
import Foundation
import Darwin

/// pty-Paar im Rohmodus; `halten` bleibt offen, damit der Master nach dem Schliessen des Ports noch lesen kann
func ptyPaar() -> (master: Int32, slave: Int32, halten: Int32)? {
    var m: Int32 = -1, sl: Int32 = -1
    guard openpty(&m, &sl, nil, nil, nil) == 0 else { return nil }
    var t = termios()
    tcgetattr(sl, &t)
    cfmakeraw(&t)
    tcsetattr(sl, TCSANOW, &t)
    _ = fcntl(m, F_SETFL, fcntl(m, F_GETFL) | O_NONBLOCK)
    return (m, sl, dup(sl))
}

/// Liest vom Master, bis `dauer` verstrichen ist (Hauptwarteschlange läuft dabei weiter)
func leseMaster(_ fd: Int32, dauer: TimeInterval) -> String {
    var text = ""
    let ende = Date().addingTimeInterval(dauer)
    var puffer = [UInt8](repeating: 0, count: 256)
    while Date() < ende {
        let n = read(fd, &puffer, puffer.count)
        if n > 0 { text += String(decoding: puffer[0..<n], as: UTF8.self) } else { abarbeiten(0.01) }
    }
    return text
}

func rahmen(_ text: String) -> [String] { text.split(separator: ";").map(String.init) }

/// Sechs Abfragen im Rückstau, dann TX1: TX1 muss unter den ersten zwei Rahmen auf der Leitung sein
func portTestTxUeberholt() {
    guard let p = ptyPaar() else { pruefe(false, "kein pty"); return }
    let port = SerialPortManager()
    port.testPortUebernehmen(p.slave)
    for _ in 0..<6 { port.send(Data("SM0;".utf8)) }
    port.send(Data("TX1;".utf8))
    let r = rahmen(leseMaster(p.master, dauer: 1.2))
    gleich(r.count, 7, "Rahmen auf der Leitung")
    pruefe((r.firstIndex(of: "TX1") ?? 99) <= 1, "TX1 an Stelle \(r.firstIndex(of: "TX1") ?? -1): \(r)")
    port.disconnect()
    close(p.halten); close(p.master)
}

/// TX0 senden und sofort trennen, ohne und mit Rückstau: TX0 kommt jedes Mal an, danach nichts mehr
func portTestTrennenNachTx0(laeufe: Int, rueckstau: Int) {
    var verloren = 0, danach = 0
    for _ in 0..<laeufe {
        guard let p = ptyPaar() else { pruefe(false, "kein pty"); return }
        let port = SerialPortManager()
        port.testPortUebernehmen(p.slave)
        for _ in 0..<rueckstau { port.send(Data("SM0;".utf8)) }
        port.send(Data("TX0;".utf8))
        port.disconnect()
        let r = rahmen(leseMaster(p.master, dauer: 0.03))
        if !r.contains("TX0") { verloren += 1 } else if r.last != "TX0" { danach += 1 }
        close(p.halten); close(p.master)
    }
    gleich(verloren, 0, "TX0 verloren (\(laeufe) Läufe, Rückstau \(rueckstau))")
    gleich(danach, 0, "nach TX0 noch geschrieben")
}
