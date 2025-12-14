//
//  SerialManager.swift
//  AudioVUMeter
//
//  Serial communication manager for physical VU meter hardware
//  Supports multiple protocols: Raw bytes, Text commands, JSON, VU-Server compatible
//

import Foundation
import IOKit
import IOKit.serial

/// Protocol format for serial communication
enum SerialProtocol: String, CaseIterable, Identifiable {
    case rawBytes = "Raw Bytes (0-255)"
    case textCommand = "Text Commands"
    case json = "JSON Format"
    case vuServer = "VU-Server Compatible"

    var id: String { rawValue }
}

/// Represents a serial port device
struct SerialPort: Identifiable, Hashable {
    let id: String
    let path: String
    let name: String
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

/// Serial communication manager
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

    // Current dial values (0-255)
    @Published var dialValues: [Int] = [0, 0, 0, 0]

    // MARK: - Private Properties

    private var fileDescriptor: Int32 = -1
    private var writeQueue = DispatchQueue(label: "serial.write", qos: .userInteractive)
    private var updateTimer: Timer?
    private let updateInterval: TimeInterval = 1.0 / 30.0 // 30 Hz update rate

    // Smoothed values for each dial
    private var smoothedValues: [Double] = [0, 0, 0, 0]

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

    /// Refresh list of available serial ports
    func refreshPorts() {
        availablePorts = getSerialPorts()

        // Auto-select first port if none selected
        if selectedPortPath.isEmpty, let firstPort = availablePorts.first {
            selectedPortPath = firstPort.path
        }
    }

    /// Get all available serial ports
    private func getSerialPorts() -> [SerialPort] {
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

            // Get device name
            var name = pathKey.components(separatedBy: "/").last ?? "Unknown"

            // Try to get a better name from USB info
            if let usbName = IORegistryEntryCreateCFProperty(
                service,
                "USB Product Name" as CFString,
                kCFAllocatorDefault,
                0
            )?.takeRetainedValue() as? String {
                name = usbName
            }

            // Filter for common serial devices
            if pathKey.contains("cu.") {
                ports.append(SerialPort(
                    id: pathKey,
                    path: pathKey,
                    name: name
                ))
            }
        }

        IOObjectRelease(iterator)
        return ports
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
