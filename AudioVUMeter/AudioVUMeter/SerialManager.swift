//
//  SerialManager.swift
//  AudioVUMeter
//
//  Direct VU1 Dials Hub communication - Native Swift
//  Protocol: >{CMD:02X}{TYPE:02X}{LEN:04X}{DATA}\r\n
//

import Foundation
import IOKit
import IOKit.serial

// MARK: - VU1 Protocol Constants

private struct VU1 {
    // Commands (from Comms_Hub_Server.py)
    static let CMD_SET_DIAL_PERC_SINGLE: UInt8 = 0x03
    static let CMD_RESCAN_BUS: UInt8 = 0x0C
    static let CMD_GET_DEVICES_MAP: UInt8 = 0x07
    
    // Data Types
    static let DATA_NONE: UInt8 = 0x01
    static let DATA_KEY_VALUE_PAIR: UInt8 = 0x04
    
    // Serial
    static let BAUD: speed_t = 115200
    static let SUFFIX = "\r\n"
}

// MARK: - Serial Protocol Enum

enum SerialProtocol: String, CaseIterable, Identifiable {
    case rawBytes = "Raw Bytes (0-255)"
    case textCommand = "Text Commands"
    case json = "JSON Format"
    case vuServer = "VU1 Direct (Native)"
    
    var id: String { rawValue }
}

// MARK: - Serial Port

struct SerialPort: Identifiable, Hashable {
    let id: String
    let path: String
    let name: String
    let vendorID: Int?
    let productID: Int?
    let isVUMeter: Bool
    
    init(path: String, name: String, vendorID: Int? = nil, productID: Int? = nil, isVUMeter: Bool = false) {
        self.id = path
        self.path = path
        self.name = name
        self.vendorID = vendorID
        self.productID = productID
        self.isVUMeter = isVUMeter
    }
}

// MARK: - Probe Result

struct ProbeResult {
    let port: SerialPort
    let protocol_: SerialProtocol
    let baudRate: Int
    let success: Bool
    let response: String?
    let responseTime: TimeInterval
}

// MARK: - Dial Channel

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

// MARK: - Dial Configuration

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
        self.maxValue = 100
        self.inverted = false
        self.smoothing = 0.3
    }
    
    var dialChannel: DialChannel {
        get { DialChannel(rawValue: channel) ?? .audioLeft }
        set { channel = newValue.rawValue }
    }
}

// MARK: - Serial Manager

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
    
    @Published var isProbing = false
    @Published var probeProgress: Double = 0
    @Published var probeStatus: String = ""
    @Published var detectedDevice: SerialPort?
    @Published var probeResults: [ProbeResult] = []
    
    @Published var dialValues: [Int] = [0, 0, 0, 0]
    
    // MARK: - Private Properties
    
    private var fileDescriptor: Int32 = -1
    private var writeQueue = DispatchQueue(label: "vu1.write", qos: .userInteractive)
    private var updateTimer: Timer?
    private let updateInterval: TimeInterval = 1.0 / 30.0
    
    private var smoothedValues: [Double] = [0, 0, 0, 0]
    private var lastSentValues: [Int] = [-1, -1, -1, -1]
    
    // MARK: - Initialization
    
    init() {
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
    
    // MARK: - Port Discovery
    
    func refreshPorts() {
        availablePorts = findSerialPorts()
        
        // Auto-select VU1 Hub (usbserial)
        if let vu1 = availablePorts.first(where: { $0.isVUMeter }) {
            selectedPortPath = vu1.path
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
            
            guard path.contains("cu.") else { continue }
            
            var name = path.components(separatedBy: "/").last ?? "Unknown"
            var vendorID: Int?
            var productID: Int?
            var isVU1 = false
            
            // Check for usbserial (FT230X = VU1 Hub)
            if path.contains("usbserial") {
                isVU1 = true
                name = "VU1 Dials Hub"
            }
            
            // Walk registry for USB info
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
                    name = usbName
                }
                
                // FTDI FT230X = VU1 Hub
                if vendorID == 0x0403 && (productID == 0x6015 || productID == 0x6001) {
                    isVU1 = true
                    name = "VU1 Dials Hub"
                }
                
                if vendorID != nil && productID != nil { break }
            }
            IOObjectRelease(current)
            
            ports.append(SerialPort(path: path, name: name, vendorID: vendorID, productID: productID, isVUMeter: isVU1))
        }
        
        IOObjectRelease(iterator)
        return ports.sorted { ($0.isVUMeter ? 0 : 1, $0.name) < ($1.isVUMeter ? 0 : 1, $1.name) }
    }
    
    // MARK: - Connection
    
    func connect() {
        guard !selectedPortPath.isEmpty else {
            lastError = "No port selected"
            return
        }
        
        // Open port
        fileDescriptor = open(selectedPortPath, O_RDWR | O_NOCTTY | O_NONBLOCK)
        guard fileDescriptor != -1 else {
            lastError = "Failed to open: \(String(cString: strerror(errno)))"
            return
        }
        
        // Configure 115200 8N1
        var options = termios()
        tcgetattr(fileDescriptor, &options)
        
        cfsetispeed(&options, speed_t(B115200))
        cfsetospeed(&options, speed_t(B115200))
        
        // 8N1, no flow control
        options.c_cflag &= ~UInt(PARENB | CSTOPB | CSIZE | CRTSCTS)
        options.c_cflag |= UInt(CS8 | CREAD | CLOCAL)
        
        // Raw mode
        options.c_lflag &= ~UInt(ICANON | ECHO | ECHOE | ISIG)
        options.c_oflag &= ~UInt(OPOST)
        options.c_iflag &= ~UInt(IXON | IXOFF | IXANY | ICRNL | INLCR | IGNBRK)
        
        // Timeouts
        options.c_cc.16 = 0   // VMIN
        options.c_cc.17 = 10  // VTIME = 1 second
        
        tcsetattr(fileDescriptor, TCSANOW, &options)
        tcflush(fileDescriptor, TCIOFLUSH)
        
        isConnected = true
        lastError = nil
        lastSentValues = [-1, -1, -1, -1]
        
        print("VU1 Hub connected: \(selectedPortPath)")
        
        // Initialize: Rescan bus
        sendCommand(cmd: VU1.CMD_RESCAN_BUS, dataType: VU1.DATA_NONE, data: [])
        usleep(500_000) // Wait 500ms for rescan
        
        // Set all dials to 0
        for i in 0..<4 {
            setDialValue(dialIndex: UInt8(i), value: 0)
            usleep(20_000)
        }
        
        startUpdateTimer()
    }
    
    func disconnect() {
        stopUpdateTimer()
        
        if fileDescriptor != -1 {
            // Reset dials to 0
            for i in 0..<4 {
                setDialValue(dialIndex: UInt8(i), value: 0)
                usleep(10_000)
            }
            usleep(100_000)
            
            close(fileDescriptor)
            fileDescriptor = -1
        }
        
        isConnected = false
        print("VU1 Hub disconnected")
    }
    
    func toggleConnection() {
        if isConnected { disconnect() } else { connect() }
    }
    
    func autoConnect() {
        refreshPorts()
        if let vu1 = availablePorts.first(where: { $0.isVUMeter }) {
            selectedPortPath = vu1.path
            connect()
        } else if let first = availablePorts.first {
            selectedPortPath = first.path
            connect()
        } else {
            lastError = "No serial ports found"
        }
    }
    
    // MARK: - VU1 Protocol
    
    /// Send VU1 command: >{CMD:02X}{TYPE:02X}{LEN:04X}{DATA}\r\n
    private func sendCommand(cmd: UInt8, dataType: UInt8, data: [UInt8]) {
        guard fileDescriptor != -1 else { return }
        
        let dataLen = data.count
        var cmdString = String(format: ">%02X%02X%04X", cmd, dataType, dataLen)
        for byte in data {
            cmdString += String(format: "%02X", byte)
        }
        cmdString += VU1.SUFFIX
        
        guard let cmdData = cmdString.data(using: .ascii) else { return }
        
        let written = cmdData.withUnsafeBytes { buffer -> Int in
            guard let base = buffer.baseAddress else { return -1 }
            return write(fileDescriptor, base, cmdData.count)
        }
        
        if written > 0 {
            bytesSent += UInt64(written)
        }
    }
    
    /// Set dial value (0-100%)
    private func setDialValue(dialIndex: UInt8, value: Int) {
        let clampedValue = UInt8(max(0, min(100, value)))
        
        // CMD: 0x03 = SET_DIAL_PERC_SINGLE
        // TYPE: 0x04 = KEY_VALUE_PAIR
        // DATA: [dial_index, value]
        sendCommand(
            cmd: VU1.CMD_SET_DIAL_PERC_SINGLE,
            dataType: VU1.DATA_KEY_VALUE_PAIR,
            data: [dialIndex, clampedValue]
        )
    }
    
    // MARK: - Value Updates
    
    func updateValues(audioEngine: AudioEngine, systemMonitor: SystemMonitor) {
        for (index, config) in dialConfigs.enumerated() {
            guard index < 4 else { break }
            
            var rawValue: Double = 0
            
            switch config.dialChannel {
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
            
            // Smoothing
            smoothedValues[index] = smoothedValues[index] * config.smoothing + rawValue * (1 - config.smoothing)
            
            var value = Int(smoothedValues[index])
            if config.inverted { value = 100 - value }
            
            dialValues[index] = max(0, min(100, value))
        }
    }
    
    func sendValues() {
        guard isConnected, fileDescriptor != -1 else { return }
        
        writeQueue.async { [weak self] in
            guard let self = self else { return }
            
            for (index, value) in self.dialValues.enumerated() {
                // Only send if changed
                if value != self.lastSentValues[index] {
                    self.setDialValue(dialIndex: UInt8(index), value: value)
                    self.lastSentValues[index] = value
                    usleep(5_000)  // 5ms between commands
                }
            }
        }
    }
    
    // MARK: - Timer
    
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
    
    // MARK: - Auto-Probe
    
    func startAutoProbe() {
        isProbing = true
        probeProgress = 0
        probeStatus = "Searching for VU1 Hub..."
        
        DispatchQueue.global().async { [weak self] in
            guard let self = self else { return }
            
            for (index, port) in self.availablePorts.enumerated() {
                DispatchQueue.main.async {
                    self.probeProgress = Double(index + 1) / Double(self.availablePorts.count)
                    self.probeStatus = "Checking: \(port.name)"
                }
                
                if port.isVUMeter || port.path.contains("usbserial") {
                    DispatchQueue.main.async {
                        self.detectedDevice = port
                        self.selectedPortPath = port.path
                        self.probeStatus = "Found: \(port.name)"
                        self.isProbing = false
                    }
                    return
                }
            }
            
            DispatchQueue.main.async {
                self.isProbing = false
                self.probeStatus = "No VU1 Hub found"
            }
        }
    }
    
    func stopAutoProbe() {
        isProbing = false
    }
    
    // MARK: - Static
    
    static let availableBaudRates = [9600, 19200, 38400, 57600, 115200, 230400]
}
