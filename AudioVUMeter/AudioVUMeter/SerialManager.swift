//
//  SerialManager.swift
//  AudioVUMeter
//
//  Serial communication manager for physical VU meter hardware
//  Supports multiple protocols: Raw bytes, Text commands, JSON, VU-Server compatible
//  Includes auto-probing to find connected VU meter hardware
//

import Foundation
import IOKit
import IOKit.serial
import IOKit.usb

/// Protocol format for serial communication
enum SerialProtocol: String, CaseIterable, Identifiable {
    case rawBytes = "Raw Bytes (0-255)"
    case textCommand = "Text Commands"
    case json = "JSON Format"
    case vuServer = "VU-Server Compatible"

    var id: String { rawValue }

    /// Probe command for this protocol
    var probeCommand: Data {
        switch self {
        case .rawBytes:
            // Send test pattern
            return Data([0xAA, 0x00, 0x00, 0x00, 0x00, 0x55])
        case .textCommand:
            return "PING\n".data(using: .utf8)!
        case .json:
            return "{\"cmd\":\"ping\"}\n".data(using: .utf8)!
        case .vuServer:
            return "?\n".data(using: .utf8)!  // Query command
        }
    }
}

/// Represents a serial port device with extended info
struct SerialPort: Identifiable, Hashable {
    let id: String
    let path: String
    let name: String
    let vendorID: Int?
    let productID: Int?
    let isVUMeter: Bool  // Detected as VU meter

    init(path: String, name: String, vendorID: Int? = nil, productID: Int? = nil, isVUMeter: Bool = false) {
        self.id = path
        self.path = path
        self.name = name
        self.vendorID = vendorID
        self.productID = productID
        self.isVUMeter = isVUMeter
    }
}

/// Probe result for a serial port
struct ProbeResult {
    let port: SerialPort
    let protocol_: SerialProtocol
    let baudRate: Int
    let success: Bool
    let response: String?
    let responseTime: TimeInterval
}

/// Channel assignment for physical VU meters
enum DialChannel: String, CaseIterable, Identifiable {
    case audioLeft = "Audio Left"
    case audioRight = "Audio Right"
    case cpu = "CPU Usage"
    case ram = "RAM Usage"
    case disk = "Disk Activity"
    case network = "Network Activity"
    case audioPeak = "Audio Peak"
    case audioMono = "Audio Mono (L+R)"

    var id: String { rawValue }
}

/// Configuration for a single dial
struct DialConfig: Identifiable, Codable {
    let id: Int
    var channel: String
    var minValue: Int
    var maxValue: Int
    var inverted: Bool
    var smoothing: Double

    init(id: Int, channel: DialChannel = .audioLeft) {
        self.id = id
        self.channel = channel.rawValue
        self.minValue = 0
        self.maxValue = 255
        self.inverted = false
        self.smoothing = 0.3
    }
}

/// Serial communication manager with auto-probing
class SerialManager: ObservableObject {
    // MARK: - Published Properties

    @Published var isConnected = false
    @Published var availablePorts: [SerialPort] = []
    @Published var selectedPortPath: String = ""
    @Published var selectedProtocol: SerialProtocol = .vuServer
    @Published var baudRate: Int = 115200
    @Published var dialConfigs: [DialConfig] = []
    @Published var lastError: String?
    @Published var bytesSent: UInt64 = 0

    // Auto-probe state
    @Published var isProbing = false
    @Published var probeProgress: Double = 0
    @Published var probeStatus: String = ""
    @Published var detectedDevice: SerialPort?
    @Published var probeResults: [ProbeResult] = []

    // Current dial values (0-255)
    @Published var dialValues: [Int] = [0, 0, 0, 0]

    // MARK: - Private Properties

    private var fileDescriptor: Int32 = -1
    private var writeQueue = DispatchQueue(label: "serial.write", qos: .userInteractive)
    private var probeQueue = DispatchQueue(label: "serial.probe", qos: .userInitiated)
    private var updateTimer: Timer?
    private let updateInterval: TimeInterval = 1.0 / 30.0 // 30 Hz update rate

    // Smoothed values for each dial
    private var smoothedValues: [Double] = [0, 0, 0, 0]

    // Known VU meter USB identifiers
    private let knownVUMeterDevices: [(vendorID: Int, productID: Int, name: String)] = [
        (0x1A86, 0x7523, "CH340 Serial"),           // Common CH340 USB-Serial
        (0x10C4, 0xEA60, "CP210x Serial"),          // Silicon Labs CP210x
        (0x0403, 0x6001, "FTDI Serial"),            // FTDI FT232
        (0x0403, 0x6015, "FTDI FT231X"),            // FTDI FT231X
        (0x2341, 0x0043, "Arduino Uno"),            // Arduino Uno
        (0x2341, 0x0001, "Arduino Mega"),           // Arduino Mega
        (0x1B4F, 0x9206, "SparkFun Pro Micro"),     // SparkFun
        (0x239A, 0x8014, "Adafruit Feather"),       // Adafruit
    ]

    // MARK: - Initialization

    init() {
        // Initialize 4 dial configurations with default assignments
        dialConfigs = [
            DialConfig(id: 0, channel: .audioLeft),
            DialConfig(id: 1, channel: .audioRight),
            DialConfig(id: 2, channel: .cpu),
            DialConfig(id: 3, channel: .ram)
        ]

        refreshPorts()
    }

    deinit {
        disconnect()
    }

    // MARK: - Port Management

    /// Refresh list of available serial ports with USB info
    func refreshPorts() {
        availablePorts = getSerialPortsWithUSBInfo()

        // Auto-select VU meter if found
        if let vuMeter = availablePorts.first(where: { $0.isVUMeter }) {
            selectedPortPath = vuMeter.path
        } else if selectedPortPath.isEmpty, let firstPort = availablePorts.first {
            selectedPortPath = firstPort.path
        }
    }

    /// Get all available serial ports with USB vendor/product info
    private func getSerialPortsWithUSBInfo() -> [SerialPort] {
        var ports: [SerialPort] = []

        var iterator: io_iterator_t = 0
        let matchingDict = IOServiceMatching(kIOSerialBSDServiceValue)

        let result = IOServiceGetMatchingServices(kIOMainPortDefault, matchingDict, &iterator)
        guard result == KERN_SUCCESS else { return ports }

        var service: io_object_t = IOIteratorNext(iterator)
        while service != 0 {
            defer {
                IOObjectRelease(service)
                service = IOIteratorNext(iterator)
            }

            // Get device path
            guard let pathKey = IORegistryEntryCreateCFProperty(
                service,
                kIOCalloutDeviceKey as CFString,
                kCFAllocatorDefault,
                0
            )?.takeRetainedValue() as? String else { continue }

            // Filter for cu.* devices (not tty.*)
            guard pathKey.contains("cu.") else { continue }

            // Get device name
            var name = pathKey.components(separatedBy: "/").last ?? "Unknown"

            // Try to get USB info by traversing the registry
            var vendorID: Int?
            var productID: Int?
            var isVUMeter = false

            // Walk up the registry to find USB device info
            var parent: io_object_t = 0
            var current = service
            IOObjectRetain(current)

            for _ in 0..<10 {  // Max depth
                if IORegistryEntryGetParentEntry(current, kIOServicePlane, &parent) != KERN_SUCCESS {
                    break
                }

                IOObjectRelease(current)
                current = parent

                // Try to get vendor ID
                if let vid = IORegistryEntryCreateCFProperty(
                    current,
                    "idVendor" as CFString,
                    kCFAllocatorDefault,
                    0
                )?.takeRetainedValue() as? Int {
                    vendorID = vid
                }

                // Try to get product ID
                if let pid = IORegistryEntryCreateCFProperty(
                    current,
                    "idProduct" as CFString,
                    kCFAllocatorDefault,
                    0
                )?.takeRetainedValue() as? Int {
                    productID = pid
                }

                // Try to get USB product name
                if let usbName = IORegistryEntryCreateCFProperty(
                    current,
                    "USB Product Name" as CFString,
                    kCFAllocatorDefault,
                    0
                )?.takeRetainedValue() as? String {
                    name = usbName
                }

                if vendorID != nil && productID != nil {
                    break
                }
            }
            IOObjectRelease(current)

            // Check if this is a known VU meter device
            if let vid = vendorID, let pid = productID {
                isVUMeter = knownVUMeterDevices.contains { $0.vendorID == vid && $0.productID == pid }

                // Also check name for VU-related keywords
                let lowerName = name.lowercased()
                if lowerName.contains("vu") || lowerName.contains("dial") || lowerName.contains("meter") {
                    isVUMeter = true
                }
            }

            ports.append(SerialPort(
                path: pathKey,
                name: name,
                vendorID: vendorID,
                productID: productID,
                isVUMeter: isVUMeter
            ))
        }

        IOObjectRelease(iterator)

        // Sort: VU meters first, then by name
        return ports.sorted { ($0.isVUMeter ? 0 : 1, $0.name) < ($1.isVUMeter ? 0 : 1, $1.name) }
    }

    // MARK: - Auto-Probing

    /// Auto-probe all ports to find VU meter hardware
    func startAutoProbe() {
        guard !isProbing else { return }

        isProbing = true
        probeProgress = 0
        probeStatus = "Starting auto-probe..."
        probeResults = []
        detectedDevice = nil

        probeQueue.async { [weak self] in
            self?.performAutoProbe()
        }
    }

    /// Stop auto-probing
    func stopAutoProbe() {
        isProbing = false
        DispatchQueue.main.async {
            self.probeStatus = "Probe cancelled"
        }
    }

    /// Perform the actual auto-probe
    private func performAutoProbe() {
        let ports = availablePorts
        let baudRates = [115200, 9600, 57600, 38400, 19200]  // Most common first
        let protocols = SerialProtocol.allCases

        let totalSteps = Double(ports.count * baudRates.count * protocols.count)
        var currentStep = 0

        var bestResult: ProbeResult?

        for port in ports {
            guard isProbing else { break }

            DispatchQueue.main.async {
                self.probeStatus = "Probing: \(port.name)"
            }

            for baud in baudRates {
                guard isProbing else { break }

                for proto in protocols {
                    guard isProbing else { break }

                    currentStep += 1
                    DispatchQueue.main.async {
                        self.probeProgress = Double(currentStep) / totalSteps
                    }

                    // Try to probe this combination
                    if let result = probePort(port: port, baudRate: baud, protocol_: proto) {
                        DispatchQueue.main.async {
                            self.probeResults.append(result)
                        }

                        if result.success {
                            // Found a working device!
                            if bestResult == nil || result.responseTime < bestResult!.responseTime {
                                bestResult = result
                            }

                            // If we got a response, this is very likely the device
                            if result.response != nil {
                                DispatchQueue.main.async {
                                    self.detectedDevice = port
                                    self.selectedPortPath = port.path
                                    self.selectedProtocol = proto
                                    self.baudRate = baud
                                    self.probeStatus = "Found VU Meter: \(port.name)"
                                    self.isProbing = false
                                }
                                return
                            }
                        }
                    }
                }
            }
        }

        // Probing complete
        DispatchQueue.main.async {
            self.isProbing = false
            self.probeProgress = 1.0

            if let best = bestResult {
                self.detectedDevice = best.port
                self.selectedPortPath = best.port.path
                self.selectedProtocol = best.protocol_
                self.baudRate = best.baudRate
                self.probeStatus = "Found: \(best.port.name) (\(best.protocol_.rawValue))"
            } else {
                self.probeStatus = "No VU meter found"
            }
        }
    }

    /// Probe a single port with specific settings
    private func probePort(port: SerialPort, baudRate: Int, protocol_: SerialProtocol) -> ProbeResult? {
        let fd = open(port.path, O_RDWR | O_NOCTTY | O_NONBLOCK)
        guard fd != -1 else { return nil }

        defer { close(fd) }

        // Configure port
        var options = termios()
        tcgetattr(fd, &options)

        let speed = getBaudRateConstant(baudRate)
        cfsetispeed(&options, speed)
        cfsetospeed(&options, speed)

        options.c_cflag &= ~UInt(PARENB | CSTOPB | CSIZE)
        options.c_cflag |= UInt(CS8 | CREAD | CLOCAL)
        options.c_lflag &= ~UInt(ICANON | ECHO | ECHOE | ISIG)
        options.c_oflag &= ~UInt(OPOST)

        // Set read timeout
        options.c_cc.16 = 0  // VMIN
        options.c_cc.17 = 5  // VTIME (0.5 seconds)

        tcsetattr(fd, TCSANOW, &options)
        tcflush(fd, TCIOFLUSH)

        // Send probe command
        let probeData = protocol_.probeCommand
        let startTime = Date()

        let written = probeData.withUnsafeBytes { buffer -> Int in
            guard let baseAddress = buffer.baseAddress else { return -1 }
            return write(fd, baseAddress, probeData.count)
        }

        guard written > 0 else {
            return ProbeResult(port: port, protocol_: protocol_, baudRate: baudRate,
                             success: false, response: nil, responseTime: 0)
        }

        // Wait for response
        usleep(100_000)  // 100ms

        // Try to read response
        var readBuffer = [UInt8](repeating: 0, count: 256)
        let bytesRead = read(fd, &readBuffer, readBuffer.count)

        let responseTime = Date().timeIntervalSince(startTime)

        if bytesRead > 0 {
            let response = String(bytes: readBuffer.prefix(bytesRead), encoding: .utf8)?
                .trimmingCharacters(in: .whitespacesAndNewlines)

            return ProbeResult(port: port, protocol_: protocol_, baudRate: baudRate,
                             success: true, response: response, responseTime: responseTime)
        }

        // No response, but connection succeeded - might still be valid
        // (some devices don't respond to probes but accept data)
        return ProbeResult(port: port, protocol_: protocol_, baudRate: baudRate,
                         success: written > 0, response: nil, responseTime: responseTime)
    }

    /// Quick probe - just check if port opens and accepts data
    func quickProbe(port: SerialPort) -> Bool {
        let fd = open(port.path, O_RDWR | O_NOCTTY | O_NONBLOCK)
        guard fd != -1 else { return false }
        defer { close(fd) }

        // Try to write a simple test
        var options = termios()
        tcgetattr(fd, &options)
        let speed = getBaudRateConstant(115200)
        cfsetispeed(&options, speed)
        cfsetospeed(&options, speed)
        options.c_cflag &= ~UInt(PARENB | CSTOPB | CSIZE)
        options.c_cflag |= UInt(CS8 | CREAD | CLOCAL)
        tcsetattr(fd, TCSANOW, &options)

        let testData = Data([0xAA, 0x00, 0x00, 0x00, 0x00, 0x55])
        let written = testData.withUnsafeBytes { buffer -> Int in
            guard let baseAddress = buffer.baseAddress else { return -1 }
            return write(fd, baseAddress, testData.count)
        }

        return written > 0
    }

    // MARK: - Connection Management

    /// Connect to selected serial port
    func connect() {
        guard !selectedPortPath.isEmpty else {
            lastError = "No port selected"
            return
        }

        // Open serial port
        fileDescriptor = open(selectedPortPath, O_RDWR | O_NOCTTY | O_NONBLOCK)

        guard fileDescriptor != -1 else {
            lastError = "Failed to open port: \(String(cString: strerror(errno)))"
            return
        }

        // Configure serial port
        var options = termios()
        tcgetattr(fileDescriptor, &options)

        // Set baud rate
        let speed = getBaudRateConstant(baudRate)
        cfsetispeed(&options, speed)
        cfsetospeed(&options, speed)

        // Configure 8N1
        options.c_cflag &= ~UInt(PARENB)  // No parity
        options.c_cflag &= ~UInt(CSTOPB)  // 1 stop bit
        options.c_cflag &= ~UInt(CSIZE)
        options.c_cflag |= UInt(CS8)       // 8 data bits

        // Enable receiver, ignore modem control lines
        options.c_cflag |= UInt(CREAD | CLOCAL)

        // Raw input
        options.c_lflag &= ~UInt(ICANON | ECHO | ECHOE | ISIG)

        // Raw output
        options.c_oflag &= ~UInt(OPOST)

        // Apply settings
        tcsetattr(fileDescriptor, TCSANOW, &options)

        // Clear any pending data
        tcflush(fileDescriptor, TCIOFLUSH)

        isConnected = true
        lastError = nil

        print("Connected to \(selectedPortPath) at \(baudRate) baud")

        // Start update timer
        startUpdateTimer()
    }

    /// Auto-connect: probe and connect to first found device
    func autoConnect() {
        refreshPorts()

        // First, try already marked VU meters
        if let vuMeter = availablePorts.first(where: { $0.isVUMeter }) {
            selectedPortPath = vuMeter.path
            connect()
            if isConnected { return }
        }

        // Quick probe all ports
        for port in availablePorts {
            if quickProbe(port: port) {
                selectedPortPath = port.path
                connect()
                if isConnected {
                    print("Auto-connected to \(port.name)")
                    return
                }
            }
        }

        lastError = "No VU meter found"
    }

    /// Disconnect from serial port
    func disconnect() {
        stopUpdateTimer()

        if fileDescriptor != -1 {
            close(fileDescriptor)
            fileDescriptor = -1
        }

        isConnected = false
        print("Disconnected from serial port")
    }

    /// Toggle connection state
    func toggleConnection() {
        if isConnected {
            disconnect()
        } else {
            connect()
        }
    }

    // MARK: - Data Transmission

    /// Update dial values from audio and system monitors
    func updateValues(audioEngine: AudioEngine, systemMonitor: SystemMonitor) {
        for (index, config) in dialConfigs.enumerated() {
            guard index < 4 else { break }

            var rawValue: Double = 0

            // Get value based on channel assignment
            switch DialChannel(rawValue: config.channel) ?? .audioLeft {
            case .audioLeft:
                rawValue = audioEngine.leftLevel * 100
            case .audioRight:
                rawValue = audioEngine.rightLevel * 100
            case .audioPeak:
                rawValue = max(audioEngine.leftPeak, audioEngine.rightPeak) * 100
            case .audioMono:
                rawValue = ((audioEngine.leftLevel + audioEngine.rightLevel) / 2) * 100
            case .cpu:
                rawValue = systemMonitor.cpuUsage
            case .ram:
                rawValue = systemMonitor.memoryUsage
            case .disk:
                rawValue = systemMonitor.diskActivity
            case .network:
                rawValue = systemMonitor.networkActivity
            }

            // Apply smoothing
            let smoothing = config.smoothing
            smoothedValues[index] = smoothedValues[index] * smoothing + rawValue * (1 - smoothing)

            // Map to dial range
            var mappedValue = Int((smoothedValues[index] / 100.0) * Double(config.maxValue - config.minValue)) + config.minValue

            // Apply inversion if needed
            if config.inverted {
                mappedValue = config.maxValue - mappedValue + config.minValue
            }

            // Clamp to valid range
            dialValues[index] = max(config.minValue, min(config.maxValue, mappedValue))
        }
    }

    /// Send current values to hardware
    func sendValues() {
        guard isConnected, fileDescriptor != -1 else { return }

        writeQueue.async { [weak self] in
            guard let self = self else { return }

            let data: Data

            switch self.selectedProtocol {
            case .rawBytes:
                data = self.formatRawBytes()
            case .textCommand:
                data = self.formatTextCommand()
            case .json:
                data = self.formatJSON()
            case .vuServer:
                data = self.formatVUServer()
            }

            self.writeData(data)
        }
    }

    // MARK: - Protocol Formatters

    /// Format as raw bytes: [0xAA, ch1, ch2, ch3, ch4, 0x55]
    private func formatRawBytes() -> Data {
        var bytes: [UInt8] = [0xAA] // Start marker
        for value in dialValues {
            bytes.append(UInt8(clamping: value))
        }
        bytes.append(0x55) // End marker
        return Data(bytes)
    }

    /// Format as text commands: "CH1:128;CH2:64;CH3:200;CH4:32\n"
    private func formatTextCommand() -> Data {
        let commands = dialValues.enumerated().map { "CH\($0 + 1):\($1)" }
        let message = commands.joined(separator: ";") + "\n"
        return message.data(using: .utf8) ?? Data()
    }

    /// Format as JSON: {"dials":[128,64,200,32]}
    private func formatJSON() -> Data {
        let json: [String: Any] = ["dials": dialValues]
        if let data = try? JSONSerialization.data(withJSONObject: json, options: []) {
            return data + "\n".data(using: .utf8)!
        }
        return Data()
    }

    /// Format for VU-Server compatible hardware
    /// Protocol: #<dial_id>:<value>\n
    private func formatVUServer() -> Data {
        var message = ""
        for (index, value) in dialValues.enumerated() {
            // VU-Server uses percentage values 0-100
            let percentage = (value * 100) / 255
            message += "#\(index):\(percentage)\n"
        }
        return message.data(using: .utf8) ?? Data()
    }

    // MARK: - Low-level I/O

    /// Write data to serial port
    private func writeData(_ data: Data) {
        guard !data.isEmpty else { return }

        data.withUnsafeBytes { buffer in
            guard let baseAddress = buffer.baseAddress else { return }
            let written = write(fileDescriptor, baseAddress, data.count)

            if written > 0 {
                DispatchQueue.main.async {
                    self.bytesSent += UInt64(written)
                }
            } else if written < 0 {
                let error = String(cString: strerror(errno))
                DispatchQueue.main.async {
                    self.lastError = "Write error: \(error)"
                }
            }
        }
    }

    // MARK: - Timer Management

    private func startUpdateTimer() {
        stopUpdateTimer()
        updateTimer = Timer.scheduledTimer(withTimeInterval: updateInterval, repeats: true) { [weak self] _ in
            self?.sendValues()
        }
    }

    private func stopUpdateTimer() {
        updateTimer?.invalidate()
        updateTimer = nil
    }

    // MARK: - Helpers

    private func getBaudRateConstant(_ rate: Int) -> speed_t {
        switch rate {
        case 9600: return speed_t(B9600)
        case 19200: return speed_t(B19200)
        case 38400: return speed_t(B38400)
        case 57600: return speed_t(B57600)
        case 115200: return speed_t(B115200)
        case 230400: return speed_t(B230400)
        default: return speed_t(B115200)
        }
    }

    /// Available baud rates
    static let availableBaudRates = [9600, 19200, 38400, 57600, 115200, 230400]
}

// MARK: - Dial Config Channel Extension
extension DialConfig {
    var dialChannel: DialChannel {
        get { DialChannel(rawValue: channel) ?? .audioLeft }
        set { channel = newValue.rawValue }
    }
}
