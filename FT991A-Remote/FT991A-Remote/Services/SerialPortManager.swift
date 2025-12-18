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

    init(path: String, name: String, vendorID: Int? = nil, productID: Int? = nil, isFT991A: Bool = false) {
        self.id = path
        self.path = path
        self.name = name
        self.vendorID = vendorID
        self.productID = productID
        self.isFT991A = isFT991A
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

// MARK: - Serial Port Manager

class SerialPortManager: ObservableObject {

    // MARK: - Published Properties

    @Published var connectionState: ConnectionState = .disconnected
    @Published var availablePorts: [SerialPort] = []
    @Published var selectedPortPath: String = ""
    @Published var baudRate: Int = 38400
    @Published var lastError: String?

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

    // MARK: - Constants

    private static let CP210X_VENDOR_ID = 0x10C4   // Silicon Labs
    private static let CP210X_PRODUCT_ID = 0xEA60 // CP210x

    // MARK: - Initialization

    init() {
        refreshPorts()
    }

    deinit {
        disconnect()
    }

    // MARK: - Port Discovery

    func refreshPorts() {
        availablePorts = findSerialPorts()

        // Auto-select FT-991A port (CP210x / SLAB)
        if let ft991a = availablePorts.first(where: { $0.isFT991A }) {
            selectedPortPath = ft991a.path
        } else if selectedPortPath.isEmpty, let first = availablePorts.first {
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

            // Check for Silicon Labs CP210x (FT-991A uses this)
            if path.contains("SLAB_USBtoUART") || path.contains("CP210") {
                isFT991A = true
                name = "FT-991A (CP210x)"
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
                    if usbName.contains("CP210") || usbName.contains("UART") {
                        name = usbName
                    }
                }

                // Silicon Labs CP210x = likely FT-991A
                if vendorID == Self.CP210X_VENDOR_ID && productID == Self.CP210X_PRODUCT_ID {
                    isFT991A = true
                    name = "FT-991A (CP210x)"
                }

                if vendorID != nil && productID != nil { break }
            }
            IOObjectRelease(current)

            ports.append(SerialPort(
                path: path,
                name: name,
                vendorID: vendorID,
                productID: productID,
                isFT991A: isFT991A
            ))
        }

        IOObjectRelease(iterator)

        // Sort: FT-991A first, then alphabetically
        return ports.sorted { ($0.isFT991A ? 0 : 1, $0.name) < ($1.isFT991A ? 0 : 1, $1.name) }
    }

    // MARK: - Connection

    func connect() {
        guard !selectedPortPath.isEmpty else {
            connectionState = .error("Kein Port ausgewählt")
            return
        }

        connectionState = .connecting

        // Open port
        fileDescriptor = open(selectedPortPath, O_RDWR | O_NOCTTY | O_NONBLOCK)
        guard fileDescriptor != -1 else {
            let error = String(cString: strerror(errno))
            connectionState = .error(error)
            lastError = error
            return
        }

        // Configure serial port
        if !configurePort() {
            close(fileDescriptor)
            fileDescriptor = -1
            return
        }

        // Clear buffers
        tcflush(fileDescriptor, TCIOFLUSH)
        readBuffer.removeAll()

        // Start reading
        startReading()

        connectionState = .connected
        lastError = nil
        onConnectionChanged?(true)

        Logger.shared.log("Connected to \(selectedPortPath) at \(baudRate) baud", level: .info)
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
        stopReading()
        stopReconnectTimer()

        if fileDescriptor != -1 {
            close(fileDescriptor)
            fileDescriptor = -1
        }

        connectionState = .disconnected
        onConnectionChanged?(false)

        Logger.shared.log("Disconnected", level: .info)
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
            let responseData = readBuffer.prefix(through: semicolonIndex)
            readBuffer.removeFirst(semicolonIndex + 1)

            if let response = String(data: Data(responseData), encoding: .ascii) {
                Logger.shared.log("RX: \(response)", level: .debug)
            }

            onDataReceived?(Data(responseData))
        }
    }

    private func handleReadError() {
        let error = String(cString: strerror(errno))
        connectionState = .error(error)
        lastError = error

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
                guard let base = buffer.baseAddress else { return -1 }
                return write(self.fileDescriptor, base, data.count)
            }

            if written > 0 {
                DispatchQueue.main.async {
                    self.bytesSent += UInt64(written)
                }

                if let command = String(data: data, encoding: .ascii) {
                    Logger.shared.log("TX: \(command.trimmingCharacters(in: .whitespaces))", level: .debug)
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
            if self.availablePorts.contains(where: { $0.path == self.selectedPortPath }) {
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

    // MARK: - Statistics

    func resetStatistics() {
        bytesSent = 0
        bytesReceived = 0
    }

    var isConnected: Bool {
        connectionState.isConnected
    }
}
