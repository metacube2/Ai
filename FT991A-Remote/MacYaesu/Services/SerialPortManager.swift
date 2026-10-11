//
//  SerialPortManager.swift
//  FT991A-Remote
//
//  USB Serial communication for FT-991A (Silicon Labs CP210x)
//

import Foundation
import IOKit
import IOKit.serial

// MARK: - Serial Port

struct SerialPort: Identifiable, Hashable {
    let id: String
    let path: String
    let name: String
    let vendorID: Int?
    let productID: Int?
    let isFT991A: Bool
    let shortPath: String
    let interfaceName: String?
    let preferredCATLineMode: String?

    init(path: String, name: String, vendorID: Int? = nil, productID: Int? = nil, isFT991A: Bool = false, interfaceName: String? = nil, preferredCATLineMode: String? = nil) {
        self.id = path
        self.path = path
        self.name = name
        self.vendorID = vendorID
        self.productID = productID
        self.isFT991A = isFT991A
        self.shortPath = URL(fileURLWithPath: path).lastPathComponent
        self.interfaceName = interfaceName
        self.preferredCATLineMode = preferredCATLineMode
    }
}

// MARK: - Connection State

enum ConnectionState: Equatable {
    case disconnected
    case connecting
    case connected
    case error(String)

    var isConnected: Bool {
        if case .connected = self { return true }
        return false
    }

    var displayString: String {
        switch self {
        case .disconnected: return "Getrennt"
        case .connecting: return "Verbinde..."
        case .connected: return "Verbunden"
        case .error(let msg): return "Fehler: \(msg)"
        }
    }
}

protocol SerialPortServiceType: AnyObject {
    var connectionState: ConnectionState { get set }
    var availablePorts: [SerialPort] { get set }
    var selectedPortPath: String { get set }
    var baudRate: Int { get set }
    var radioModel: RadioModel { get set }
    var searchUSBPortsUntilFound: Bool { get set }
    var lastError: String? { get set }
    var lastResponseAt: Date? { get set }
    var hasCATResponse: Bool { get set }
    var currentPortSearchIndex: Int { get set }
    var currentPortSearchTotal: Int { get set }
    var bytesSent: UInt64 { get set }
    var bytesReceived: UInt64 { get set }
    var onDataReceived: ((Data) -> Void)? { get set }
    var onConnectionChanged: ((Bool) -> Void)? { get set }
    var isConnected: Bool { get }

    var connectionStatePublisher: Published<ConnectionState>.Publisher { get }
    var availablePortsPublisher: Published<[SerialPort]>.Publisher { get }
    var selectedPortPathPublisher: Published<String>.Publisher { get }
    var bytesSentPublisher: Published<UInt64>.Publisher { get }
    var bytesReceivedPublisher: Published<UInt64>.Publisher { get }
    var hasCATResponsePublisher: Published<Bool>.Publisher { get }
    var lastResponseAtPublisher: Published<Date?>.Publisher { get }
    var currentPortSearchIndexPublisher: Published<Int>.Publisher { get }
    var currentPortSearchTotalPublisher: Published<Int>.Publisher { get }

    func refreshPorts()
    func connect()
    func disconnect()
    func send(_ command: CATCommand)
    func noteCATResponse()
}

// MARK: - Serial Port Manager

class SerialPortManager: ObservableObject, SerialPortServiceType {
    private enum CATLineMode: String {
        case rtsOff = "rts-off"
        case rtsOn = "rts-on"
    }

    // MARK: - Published Properties

    @Published var connectionState: ConnectionState = .disconnected
    @Published var availablePorts: [SerialPort] = []
    @Published var selectedPortPath: String = ""
    @Published var baudRate: Int = 38400
    @Published var radioModel: RadioModel = .ft991a
    @Published var searchUSBPortsUntilFound = false
    @Published var lastError: String?
    @Published var lastResponseAt: Date?
    @Published var hasCATResponse = false
    @Published var currentPortSearchIndex: Int = 0
    @Published var currentPortSearchTotal: Int = 0

    @Published var bytesSent: UInt64 = 0
    @Published var bytesReceived: UInt64 = 0

    // MARK: - Callbacks

    var onDataReceived: ((Data) -> Void)?
    var onConnectionChanged: ((Bool) -> Void)?

    // MARK: - Private Properties

    private var fileDescriptor: Int32 = -1
    private let writeQueue = DispatchQueue(label: "ft991a.serial.write", qos: .userInteractive)
    private let readQueue = DispatchQueue(label: "ft991a.serial.read", qos: .userInteractive)

    private var readBuffer = Data()
    private var isReading = false
    private var readSource: DispatchSourceRead?

    // Auto-reconnect
    private var reconnectTimer: Timer?
    private var shouldReconnect = false
    private var handshakeTimer: Timer?
    private var awaitingInitialCATResponse = false
    private var lastWriteTime: Date = .distantPast
    private var lineMode: CATLineMode = .rtsOff
    private var pendingPortSearchPaths: [String] = []

    // MARK: - Constants

    private static let CP210X_VENDOR_ID = 0x10C4   // Silicon Labs
    private static let CP210X_PRODUCT_ID = 0xEA60 // CP210x
    private let minimumCommandSpacing: TimeInterval = 0.11

    // MARK: - Initialization

    init() {
        refreshPorts()
    }

    deinit {
        disconnect()
    }

    var connectionStatePublisher: Published<ConnectionState>.Publisher { $connectionState }
    var availablePortsPublisher: Published<[SerialPort]>.Publisher { $availablePorts }
    var selectedPortPathPublisher: Published<String>.Publisher { $selectedPortPath }
    var bytesSentPublisher: Published<UInt64>.Publisher { $bytesSent }
    var bytesReceivedPublisher: Published<UInt64>.Publisher { $bytesReceived }
    var hasCATResponsePublisher: Published<Bool>.Publisher { $hasCATResponse }
    var lastResponseAtPublisher: Published<Date?>.Publisher { $lastResponseAt }
    var currentPortSearchIndexPublisher: Published<Int>.Publisher { $currentPortSearchIndex }
    var currentPortSearchTotalPublisher: Published<Int>.Publisher { $currentPortSearchTotal }

    // MARK: - Port Discovery

    func refreshPorts() {
        availablePorts = findSerialPorts()

        if availablePorts.contains(where: { $0.path == selectedPortPath }) {
            return
        }

        // Auto-select FT-991A port (CP210x / SLAB)
        if let ft991a = availablePorts.first(where: { $0.isFT991A }) {
            selectedPortPath = ft991a.path
        } else if selectedPortPath.isEmpty, let first = availablePorts.first {
            selectedPortPath = first.path
        } else if let first = availablePorts.first {
            selectedPortPath = first.path
        }
    }

    private func findSerialPorts() -> [SerialPort] {
        var ports: [SerialPort] = []
        var iterator: io_iterator_t = 0

        let matching = IOServiceMatching(kIOSerialBSDServiceValue)
        guard IOServiceGetMatchingServices(kIOMainPortDefault, matching, &iterator) == KERN_SUCCESS else {
            return ports
        }

        var service = IOIteratorNext(iterator)
        while service != 0 {
            defer {
                IOObjectRelease(service)
                service = IOIteratorNext(iterator)
            }

            guard let path = IORegistryEntryCreateCFProperty(
                service, kIOCalloutDeviceKey as CFString, kCFAllocatorDefault, 0
            )?.takeRetainedValue() as? String else { continue }

            // Only callout devices (cu.*)
            guard path.contains("cu.") else { continue }

            var name = path.components(separatedBy: "/").last ?? "Unknown"
            var vendorID: Int?
            var productID: Int?
            var isFT991A = false
            let shortPath = URL(fileURLWithPath: path).lastPathComponent
            var interfaceName: String?
            var preferredCATLineMode: String?

            let radioLabel = radioModel.serialDeviceName

            // Check for Silicon Labs CP210x (Yaesu CAT USB uses this)
            if path.contains("SLAB_USBtoUART") || path.contains("CP210") {
                isFT991A = true
                name = "\(radioLabel) (CP210x)"
            }

            // Walk USB registry for device info
            var parent: io_object_t = 0
            var current = service
            IOObjectRetain(current)

            for _ in 0..<10 {
                if IORegistryEntryGetParentEntry(current, kIOServicePlane, &parent) != KERN_SUCCESS { break }
                IOObjectRelease(current)
                current = parent

                if let vid = IORegistryEntryCreateCFProperty(current, "idVendor" as CFString, kCFAllocatorDefault, 0)?.takeRetainedValue() as? Int {
                    vendorID = vid
                }
                if let pid = IORegistryEntryCreateCFProperty(current, "idProduct" as CFString, kCFAllocatorDefault, 0)?.takeRetainedValue() as? Int {
                    productID = pid
                }
                if let usbName = IORegistryEntryCreateCFProperty(current, "USB Product Name" as CFString, kCFAllocatorDefault, 0)?.takeRetainedValue() as? String {
                    if !usbName.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty {
                        name = usbName
                    }
                }
                if let ifName = IORegistryEntryCreateCFProperty(current, "USB Interface Name" as CFString, kCFAllocatorDefault, 0)?.takeRetainedValue() as? String {
                    interfaceName = ifName
                }
                if let ifName = IORegistryEntryCreateCFProperty(current, "kUSBString" as CFString, kCFAllocatorDefault, 0)?.takeRetainedValue() as? String {
                    interfaceName = ifName
                }

                // Silicon Labs CP210x = likely Yaesu CAT bridge
                if vendorID == Self.CP210X_VENDOR_ID && productID == Self.CP210X_PRODUCT_ID {
                    isFT991A = true
                    name = "\(radioLabel) CAT"
                }

                if vendorID != nil && productID != nil { break }
            }
            IOObjectRelease(current)

            if name == shortPath || name.lowercased() == "usb serial" || name.lowercased().contains("usbtouart") {
                if isFT991A {
                    name = "\(radioLabel) CAT"
                } else if let vendorID, let productID {
                    name = String(format: "USB Serial [%04X:%04X]", vendorID, productID)
                } else {
                    name = "USB Serial"
                }
            }

            if let interfaceName {
                let lowered = interfaceName.lowercased()
                if lowered.contains("enhanced") {
                    name = "\(radioLabel) CAT Enhanced"
                    preferredCATLineMode = "rts-on"
                } else if lowered.contains("standard") {
                    name = "\(radioLabel) CAT Standard"
                    preferredCATLineMode = "rts-off"
                }
            } else if isFT991A && shortPath.hasSuffix("0") {
                name = "\(radioLabel) CAT Enhanced"
                preferredCATLineMode = "rts-on"
            } else if isFT991A && shortPath.hasSuffix("1") {
                name = "\(radioLabel) CAT Standard"
                preferredCATLineMode = "rts-off"
            }

            ports.append(SerialPort(
                path: path,
                name: name,
                vendorID: vendorID,
                productID: productID,
                isFT991A: isFT991A,
                interfaceName: interfaceName,
                preferredCATLineMode: preferredCATLineMode
            ))
        }

        IOObjectRelease(iterator)

        // Sort: FT-991A first, then alphabetically
        let sorted = ports.sorted {
            let lhsRank = $0.preferredCATLineMode == "rts-on" ? 0 : ($0.isFT991A ? 1 : 2)
            let rhsRank = $1.preferredCATLineMode == "rts-on" ? 0 : ($1.isFT991A ? 1 : 2)
            return (lhsRank, $0.name) < (rhsRank, $1.name)
        }

        // If multiple devices share the same USB name, disambiguate with the serial node.
        let duplicateNames = Dictionary(grouping: sorted, by: \.name)
            .filter { $0.value.count > 1 }
            .map { $0.key }

        return sorted.map { port in
            guard duplicateNames.contains(port.name) else { return port }
            return SerialPort(
                path: port.path,
                name: "\(port.name) · \(port.shortPath)",
                vendorID: port.vendorID,
                productID: port.productID,
                isFT991A: port.isFT991A,
                interfaceName: port.interfaceName,
                preferredCATLineMode: port.preferredCATLineMode
            )
        }
    }

    // MARK: - Connection

    func connect() {
        refreshPorts()

        let attemptPaths = buildConnectionAttemptPaths()
        guard let firstPath = attemptPaths.first else {
            connectionState = .error("Kein Port ausgewählt")
            return
        }

        pendingPortSearchPaths = attemptPaths
        currentPortSearchTotal = attemptPaths.count
        currentPortSearchIndex = 1
        connectToCurrentAttempt(firstPath)
    }

    private func connectToCurrentAttempt(_ path: String) {
        disconnectCurrentConnection(markDisconnected: false)
        selectedPortPath = path
        if let currentIndex = pendingPortSearchPaths.firstIndex(of: path) {
            currentPortSearchIndex = currentIndex + 1
        }
        connectionState = .connecting
        if let preferred = availablePorts.first(where: { $0.path == path })?.preferredCATLineMode,
           preferred == "rts-on" {
            lineMode = .rtsOn
        } else {
            lineMode = .rtsOff
        }

        // Open port
        fileDescriptor = open(path, O_RDWR | O_NOCTTY | O_NONBLOCK)
        guard fileDescriptor != -1 else {
            let error = String(cString: strerror(errno))
            handleAttemptFailure(error, logAsError: false)
            return
        }

        // Configure serial port
        if !configurePort() {
            close(fileDescriptor)
            fileDescriptor = -1
            let configurationError = lastError ?? "Fehler beim Konfigurieren des Ports"
            handleAttemptFailure(configurationError, logAsError: false)
            return
        }

        // Clear buffers
        tcflush(fileDescriptor, TCIOFLUSH)
        readBuffer.removeAll()
        hasCATResponse = false
        lastResponseAt = nil

        // Start reading
        startReading()
        lastError = nil
        awaitingInitialCATResponse = true
        startHandshakeTimer()
        let portName = availablePorts.first(where: { $0.path == path })?.name ?? path
        Logger.shared.startCATSession(portName: portName, portPath: path, baudRate: baudRate)
        startHandshakeProbe()

        Logger.shared.log("Port opened: \(path) at \(baudRate) baud, waiting for CAT response", level: .info)
    }

    private func buildConnectionAttemptPaths() -> [String] {
        let allPaths = availablePorts.map(\.path)

        if !selectedPortPath.isEmpty, !searchUSBPortsUntilFound {
            return allPaths.contains(selectedPortPath) ? [selectedPortPath] : []
        }

        var orderedPaths: [String] = []

        if !selectedPortPath.isEmpty, allPaths.contains(selectedPortPath) {
            orderedPaths.append(selectedPortPath)
        }

        for port in availablePorts where !orderedPaths.contains(port.path) {
            orderedPaths.append(port.path)
        }

        return orderedPaths
    }

    private func handleAttemptFailure(_ error: String, logAsError: Bool) {
        lastError = error

        if advanceToNextAttempt(after: error) {
            return
        }

        connectionState = .error(error)
        let level: LogLevel = logAsError ? .error : .info
        Logger.shared.log(error, level: level)
    }

    private func advanceToNextAttempt(after reason: String) -> Bool {
        guard searchUSBPortsUntilFound else {
            pendingPortSearchPaths.removeAll()
            currentPortSearchIndex = 0
            currentPortSearchTotal = 0
            return false
        }

        guard let currentIndex = pendingPortSearchPaths.firstIndex(of: selectedPortPath) else {
            pendingPortSearchPaths.removeAll()
            currentPortSearchIndex = 0
            currentPortSearchTotal = 0
            return false
        }

        let nextIndex = pendingPortSearchPaths.index(after: currentIndex)
        guard nextIndex < pendingPortSearchPaths.endIndex else {
            pendingPortSearchPaths.removeAll()
            currentPortSearchIndex = 0
            currentPortSearchTotal = 0
            return false
        }

        let nextPath = pendingPortSearchPaths[nextIndex]
        Logger.shared.log("No CAT on \(selectedPortPath). Trying next serial port \(nextPath). Reason: \(reason)", level: .info)
        connectToCurrentAttempt(nextPath)
        return true
    }

    private func disconnectCurrentConnection(markDisconnected: Bool) {
        stopReading()
        stopHandshakeTimer()
        awaitingInitialCATResponse = false

        if fileDescriptor != -1 {
            close(fileDescriptor)
            fileDescriptor = -1
        }

        if markDisconnected {
            stopReconnectTimer()
            pendingPortSearchPaths.removeAll()
            currentPortSearchIndex = 0
            currentPortSearchTotal = 0
            connectionState = .disconnected
            onConnectionChanged?(false)
            Logger.shared.log("Disconnected", level: .info)
        }
    }

    private func configurePort() -> Bool {
        var options = termios()
        if tcgetattr(fileDescriptor, &options) != 0 {
            connectionState = .error("Fehler beim Lesen der Port-Einstellungen")
            return false
        }

        // Set baud rate
        let speed = baudRateToSpeed(baudRate)
        cfsetispeed(&options, speed)
        cfsetospeed(&options, speed)

        // 8N1 configuration
        options.c_cflag &= ~UInt(PARENB)   // No parity
        options.c_cflag &= ~UInt(CSTOPB)   // 1 stop bit
        options.c_cflag &= ~UInt(CSIZE)    // Clear size bits
        options.c_cflag |= UInt(CS8)       // 8 data bits

        // Enable receiver, ignore modem control
        options.c_cflag |= UInt(CREAD | CLOCAL)
        options.c_cflag &= ~UInt(HUPCL)

        // No hardware flow control
        options.c_cflag &= ~UInt(CRTSCTS)

        // Raw mode (no processing)
        options.c_lflag &= ~UInt(ICANON | ECHO | ECHOE | ISIG)
        options.c_oflag &= ~UInt(OPOST)
        options.c_iflag &= ~UInt(IXON | IXOFF | IXANY | ICRNL | INLCR | IGNBRK)

        // Timeouts
        options.c_cc.16 = 0    // VMIN - minimum characters
        options.c_cc.17 = 10   // VTIME - timeout in 0.1s

        if tcsetattr(fileDescriptor, TCSANOW, &options) != 0 {
            connectionState = .error("Fehler beim Setzen der Port-Einstellungen")
            return false
        }

        applyLineMode(lineMode)

        return true
    }

    private func baudRateToSpeed(_ rate: Int) -> speed_t {
        switch rate {
        case 4800: return speed_t(B4800)
        case 9600: return speed_t(B9600)
        case 19200: return speed_t(B19200)
        case 38400: return speed_t(B38400)
        case 57600: return speed_t(B57600)
        case 115200: return speed_t(B115200)
        default: return speed_t(B38400)
        }
    }

    func disconnect() {
        disconnectCurrentConnection(markDisconnected: true)
    }

    func toggleConnection() {
        if connectionState.isConnected {
            disconnect()
        } else {
            connect()
        }
    }

    // MARK: - Reading

    private func startReading() {
        guard fileDescriptor != -1 else { return }

        isReading = true

        readSource = DispatchSource.makeReadSource(fileDescriptor: fileDescriptor, queue: readQueue)
        readSource?.setEventHandler { [weak self] in
            self?.readAvailableData()
        }
        readSource?.setCancelHandler { [weak self] in
            self?.isReading = false
        }
        readSource?.resume()
    }

    private func stopReading() {
        readSource?.cancel()
        readSource = nil
        isReading = false
    }

    private func readAvailableData() {
        guard fileDescriptor != -1 else { return }

        var buffer = [UInt8](repeating: 0, count: 256)
        let bytesRead = read(fileDescriptor, &buffer, buffer.count)

        guard bytesRead > 0 else {
            if bytesRead < 0 && errno != EAGAIN {
                DispatchQueue.main.async {
                    self.handleReadError()
                }
            }
            return
        }

        let data = Data(buffer[0..<bytesRead])

        DispatchQueue.main.async {
            self.bytesReceived += UInt64(bytesRead)
        }

        // Append to buffer
        readBuffer.append(data)

        // Process complete responses (terminated by ';')
        processBuffer()
    }

    private func processBuffer() {
        while let semicolonIndex = readBuffer.firstIndex(of: 0x3B) { // ';'
            let count = readBuffer.distance(from: readBuffer.startIndex, to: semicolonIndex) + 1
            guard count > 0, count <= readBuffer.count else {
                Logger.shared.log("RX buffer out of sync while parsing semicolon-terminated CAT response", level: .error)
                readBuffer.removeAll()
                return
            }

            let responseData = readBuffer.prefix(count)
            readBuffer.removeFirst(count)

            if let response = String(data: Data(responseData), encoding: .ascii) {
                Logger.shared.log("RX: \(response)", level: .debug)
                Logger.shared.catTrace("RX raw=\(response.trimmingCharacters(in: .newlines))")
            }

            onDataReceived?(Data(responseData))
        }
    }

    private func handleReadError() {
        let error = String(cString: strerror(errno))
        connectionState = .error(error)
        lastError = error
        stopHandshakeTimer()
        awaitingInitialCATResponse = false

        if shouldReconnect {
            startReconnectTimer()
        }
    }

    // MARK: - Writing

    func send(_ data: Data) {
        guard fileDescriptor != -1 else { return }

        writeQueue.async { [weak self] in
            guard let self = self, self.fileDescriptor != -1 else { return }

            let written = data.withUnsafeBytes { buffer -> Int in
                guard let rawBase = buffer.baseAddress else { return -1 }
                let base = rawBase.assumingMemoryBound(to: UInt8.self)
                var totalWritten = 0

                let elapsed = Date().timeIntervalSince(self.lastWriteTime)
                if elapsed < self.minimumCommandSpacing {
                    usleep(useconds_t((self.minimumCommandSpacing - elapsed) * 1_000_000))
                }

                while totalWritten < data.count {
                    let chunk = write(self.fileDescriptor, base.advanced(by: totalWritten), data.count - totalWritten)
                    if chunk > 0 {
                        totalWritten += chunk
                        continue
                    }
                    if chunk < 0 && errno == EAGAIN {
                        usleep(2_000)
                        continue
                    }
                    return -1
                }

                self.lastWriteTime = Date()
                return totalWritten
            }

            if written > 0 {
                DispatchQueue.main.async {
                    self.bytesSent += UInt64(written)
                }

                if let command = String(data: data, encoding: .ascii) {
                    Logger.shared.log("TX: \(command.trimmingCharacters(in: .whitespaces))", level: .debug)
                    Logger.shared.catTrace("TX raw=\(command.trimmingCharacters(in: .whitespacesAndNewlines))")
                }
            } else if written < 0 {
                DispatchQueue.main.async {
                    self.handleWriteError()
                }
            }
        }
    }

    func send(_ command: CATCommand) {
        send(command.data)
    }

    func sendString(_ string: String) {
        if let data = string.data(using: .ascii) {
            send(data)
        }
    }

    private func handleWriteError() {
        let error = String(cString: strerror(errno))
        connectionState = .error(error)
        lastError = error
        stopHandshakeTimer()
        awaitingInitialCATResponse = false
    }

    func noteCATResponse() {
        DispatchQueue.main.async {
            self.lastResponseAt = Date()
            self.hasCATResponse = true
            self.lastError = nil

            if self.awaitingInitialCATResponse {
                self.awaitingInitialCATResponse = false
                self.stopHandshakeTimer()
                self.pendingPortSearchPaths.removeAll()
                self.currentPortSearchIndex = 0
                self.currentPortSearchTotal = 0
                self.connectionState = .connected
                self.onConnectionChanged?(true)
                Logger.shared.catTrace("HANDSHAKE success mode=\(self.lineMode.rawValue)")
                Logger.shared.log("CAT handshake confirmed on \(self.selectedPortPath)", level: .info)
            }
        }
    }

    // MARK: - Auto-Reconnect

    func enableAutoReconnect(_ enabled: Bool) {
        shouldReconnect = enabled
        if !enabled {
            stopReconnectTimer()
        }
    }

    private func startReconnectTimer() {
        stopReconnectTimer()

        reconnectTimer = Timer.scheduledTimer(withTimeInterval: 5.0, repeats: true) { [weak self] _ in
            guard let self = self else { return }

            Logger.shared.log("Attempting to reconnect...", level: .info)

            self.refreshPorts()
            if self.searchUSBPortsUntilFound || self.availablePorts.contains(where: { $0.path == self.selectedPortPath }) {
                self.connect()
                if self.connectionState.isConnected {
                    self.stopReconnectTimer()
                }
            }
        }
    }

    private func stopReconnectTimer() {
        reconnectTimer?.invalidate()
        reconnectTimer = nil
    }

    private func startHandshakeTimer() {
        stopHandshakeTimer()
        handshakeTimer = Timer.scheduledTimer(withTimeInterval: 2.0, repeats: false) { [weak self] _ in
            guard let self = self else { return }
            guard self.awaitingInitialCATResponse else { return }
            if self.lineMode == .rtsOff {
                self.retryHandshakeWithRTS()
                return
            }
            Logger.shared.catTrace("HANDSHAKE failed mode=\(self.lineMode.rawValue)")
            self.handleAttemptFailure("Keine CAT-Antwort vom Funkgerät. Port, Baudrate und FT-991A CAT-Einstellungen prüfen.", logAsError: true)
        }
    }

    private func stopHandshakeTimer() {
        handshakeTimer?.invalidate()
        handshakeTimer = nil
    }

    private func startHandshakeProbe() {
        Logger.shared.catTrace("HANDSHAKE probe mode=\(lineMode.rawValue)")
        sendString("ID;")
        sendString("FA;")
    }

    private func retryHandshakeWithRTS() {
        lineMode = .rtsOn
        readBuffer.removeAll()
        tcflush(fileDescriptor, TCIOFLUSH)
        applyLineMode(.rtsOn)
        Logger.shared.catTrace("HANDSHAKE retry mode=\(lineMode.rawValue)")
        startHandshakeTimer()
        startHandshakeProbe()
    }

    private func applyLineMode(_ mode: CATLineMode) {
        guard fileDescriptor != -1 else { return }
        var clearBits = Int(TIOCM_RTS | TIOCM_DTR)
        _ = ioctl(fileDescriptor, TIOCMBIC, &clearBits)
        if mode == .rtsOn {
            var setBits = Int(TIOCM_RTS)
            _ = ioctl(fileDescriptor, TIOCMBIS, &setBits)
        }
    }

    // MARK: - Statistics

    func resetStatistics() {
        bytesSent = 0
        bytesReceived = 0
    }

    var isConnected: Bool {
        connectionState.isConnected
    }
}

// MARK: - VU Meter Hub

struct VUMeterSerialPort: Identifiable, Hashable {
    let id: String
    let path: String
    let name: String
    let isVUMeter: Bool

    init(path: String, name: String, isVUMeter: Bool) {
        self.id = path
        self.path = path
        self.name = name
        self.isVUMeter = isVUMeter
    }
}

final class VUMeterHubService: ObservableObject {
    static let shared = VUMeterHubService()

    @Published var isConnected = false
    @Published var availablePorts: [VUMeterSerialPort] = []
    @Published var selectedPortPath: String = ""
    @Published var autoConnectEnabled = false
    @Published var lastError: String?
    @Published var signalDialValue: Int = 0
    @Published var swrDialValue: Int = 0

    private var fileDescriptor: Int32 = -1
    private let writeQueue = DispatchQueue(label: "macyaesu.vu.write", qos: .userInteractive)
    private var updateTimer: Timer?
    private let updateInterval: TimeInterval = 1.0 / 12.0

    private var targetValues: [Int] = [0, 0, 0, 0]
    private var smoothedValues: [Double] = [0, 0, 0, 0]
    private var lastSentValues: [Int] = [-1, -1, -1, -1]

    private let selectedPortDefaultsKey = "vuMeterHub.selectedPortPath"
    private let autoConnectDefaultsKey = "vuMeterHub.autoConnect"

    private init() {
        let defaults = UserDefaults.standard
        selectedPortPath = defaults.string(forKey: selectedPortDefaultsKey) ?? ""
        autoConnectEnabled = defaults.bool(forKey: autoConnectDefaultsKey)
        refreshPorts()
        if autoConnectEnabled {
            connect()
        }
    }

    deinit {
        guard fileDescriptor != -1 else { return }
        for dial in 0..<4 {
            writeDialValue(index: UInt8(dial), value: 0)
        }
        close(fileDescriptor)
    }

    func refreshPorts() {
        availablePorts = findVUMeterPorts()

        if availablePorts.contains(where: { $0.path == selectedPortPath }) {
            return
        }

        if let detected = availablePorts.first(where: { $0.isVUMeter }) {
            selectedPortPath = detected.path
        } else if let first = availablePorts.first {
            selectedPortPath = first.path
        }

        persistSettings()
    }

    func setSelectedPort(_ path: String) {
        selectedPortPath = path
        persistSettings()
    }

    func setAutoConnectEnabled(_ enabled: Bool) {
        autoConnectEnabled = enabled
        persistSettings()
        if enabled, !isConnected {
            connect()
        }
    }

    func connect() {
        refreshPorts()
        guard !selectedPortPath.isEmpty else {
            lastError = "Kein VU-Port gefunden"
            return
        }

        disconnect(resetNeedles: false)

        fileDescriptor = open(selectedPortPath, O_RDWR | O_NOCTTY | O_NONBLOCK)
        guard fileDescriptor != -1 else {
            lastError = "VU-Port konnte nicht geöffnet werden: \(String(cString: strerror(errno)))"
            return
        }

        var options = termios()
        tcgetattr(fileDescriptor, &options)
        cfsetispeed(&options, speed_t(B115200))
        cfsetospeed(&options, speed_t(B115200))
        options.c_cflag &= ~UInt(PARENB | CSTOPB | CSIZE | CRTSCTS)
        options.c_cflag |= UInt(CS8 | CREAD | CLOCAL)
        options.c_lflag &= ~UInt(ICANON | ECHO | ECHOE | ISIG)
        options.c_oflag &= ~UInt(OPOST)
        options.c_iflag &= ~UInt(IXON | IXOFF | IXANY | ICRNL | INLCR | IGNBRK)
        options.c_cc.16 = 0
        options.c_cc.17 = 10
        tcsetattr(fileDescriptor, TCSANOW, &options)
        tcflush(fileDescriptor, TCIOFLUSH)

        sendVUCommand(cmd: 0x0C, dataType: 0x01, data: [])
        usleep(500_000)
        for dial in 0..<4 {
            writeDialValue(index: UInt8(dial), value: 0)
            usleep(20_000)
        }

        lastSentValues = [-1, -1, -1, -1]
        isConnected = true
        lastError = nil
        startUpdateTimer()
        persistSettings()
    }

    func disconnect(resetNeedles: Bool = true) {
        stopUpdateTimer()

        guard fileDescriptor != -1 else {
            isConnected = false
            return
        }

        if resetNeedles {
            for dial in 0..<4 {
                writeDialValue(index: UInt8(dial), value: 0)
                usleep(10_000)
            }
            usleep(100_000)
        }

        close(fileDescriptor)
        fileDescriptor = -1
        isConnected = false
    }

    func updateMeters(signalLevel: Int, swrLevel: Int, isTransmitting: Bool) {
        signalDialValue = normalizedPercent(from: signalLevel)
        swrDialValue = isTransmitting ? normalizedPercent(from: swrLevel) : 0

        targetValues[0] = signalDialValue
        targetValues[1] = swrDialValue
        targetValues[2] = 0
        targetValues[3] = 0
    }

    private func startUpdateTimer() {
        stopUpdateTimer()
        updateTimer = Timer.scheduledTimer(withTimeInterval: updateInterval, repeats: true) { [weak self] _ in
            self?.pushLatestValues()
        }
    }

    private func stopUpdateTimer() {
        updateTimer?.invalidate()
        updateTimer = nil
    }

    private func pushLatestValues() {
        guard isConnected, fileDescriptor != -1 else { return }

        let targets = targetValues
        writeQueue.async { [weak self] in
            guard let self else { return }

            for index in targets.indices {
                let target = Double(targets[index])
                self.smoothedValues[index] = self.smoothedValues[index] * 0.55 + target * 0.45
                let nextValue = max(0, min(100, Int(self.smoothedValues[index].rounded())))

                if nextValue != self.lastSentValues[index] {
                    self.writeDialValue(index: UInt8(index), value: nextValue)
                    self.lastSentValues[index] = nextValue
                    usleep(5_000)
                }
            }
        }
    }

    private func normalizedPercent(from rawValue: Int) -> Int {
        let clamped = max(0, min(255, rawValue))
        return Int((Double(clamped) / 255.0 * 100.0).rounded())
    }

    private func persistSettings() {
        let defaults = UserDefaults.standard
        defaults.set(selectedPortPath, forKey: selectedPortDefaultsKey)
        defaults.set(autoConnectEnabled, forKey: autoConnectDefaultsKey)
    }

    private func sendVUCommand(cmd: UInt8, dataType: UInt8, data: [UInt8]) {
        guard fileDescriptor != -1 else { return }

        var command = String(format: ">%02X%02X%04X", cmd, dataType, data.count)
        for byte in data {
            command += String(format: "%02X", byte)
        }
        command += "\r\n"

        guard let payload = command.data(using: .ascii) else { return }
        _ = payload.withUnsafeBytes { buffer in
            guard let baseAddress = buffer.baseAddress else { return -1 }
            return write(fileDescriptor, baseAddress, payload.count)
        }
    }

    private func writeDialValue(index: UInt8, value: Int) {
        let clampedValue = UInt8(max(0, min(100, value)))
        sendVUCommand(cmd: 0x03, dataType: 0x04, data: [index, clampedValue])
    }

    private func findVUMeterPorts() -> [VUMeterSerialPort] {
        var ports: [VUMeterSerialPort] = []
        var iterator: io_iterator_t = 0
        let matching = IOServiceMatching(kIOSerialBSDServiceValue)

        guard IOServiceGetMatchingServices(kIOMainPortDefault, matching, &iterator) == KERN_SUCCESS else {
            return ports
        }

        var service = IOIteratorNext(iterator)
        while service != 0 {
            defer {
                IOObjectRelease(service)
                service = IOIteratorNext(iterator)
            }

            guard let path = IORegistryEntryCreateCFProperty(
                service, kIOCalloutDeviceKey as CFString, kCFAllocatorDefault, 0
            )?.takeRetainedValue() as? String else { continue }

            guard path.contains("cu.") else { continue }

            let shortPath = URL(fileURLWithPath: path).lastPathComponent
            var name = shortPath
            var vendorID: Int?
            var productID: Int?
            var isVUMeter = path.contains("usbserial")

            var parent: io_object_t = 0
            var current = service
            IOObjectRetain(current)

            for _ in 0..<10 {
                if IORegistryEntryGetParentEntry(current, kIOServicePlane, &parent) != KERN_SUCCESS { break }
                IOObjectRelease(current)
                current = parent

                if let vid = IORegistryEntryCreateCFProperty(current, "idVendor" as CFString, kCFAllocatorDefault, 0)?.takeRetainedValue() as? Int {
                    vendorID = vid
                }
                if let pid = IORegistryEntryCreateCFProperty(current, "idProduct" as CFString, kCFAllocatorDefault, 0)?.takeRetainedValue() as? Int {
                    productID = pid
                }
                if let usbName = IORegistryEntryCreateCFProperty(current, "USB Product Name" as CFString, kCFAllocatorDefault, 0)?.takeRetainedValue() as? String,
                   !usbName.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty {
                    name = usbName
                }

                if vendorID == 0x0403 && (productID == 0x6015 || productID == 0x6001) {
                    isVUMeter = true
                }

                if vendorID != nil && productID != nil { break }
            }
            IOObjectRelease(current)

            if isVUMeter {
                name = "VU1 Dials Hub"
            }

            ports.append(VUMeterSerialPort(path: path, name: isVUMeter ? "\(name) · \(shortPath)" : name, isVUMeter: isVUMeter))
        }

        IOObjectRelease(iterator)

        return ports.sorted {
            let lhsRank = $0.isVUMeter ? 0 : 1
            let rhsRank = $1.isVUMeter ? 0 : 1
            return (lhsRank, $0.name) < (rhsRank, $1.name)
        }
    }
}
