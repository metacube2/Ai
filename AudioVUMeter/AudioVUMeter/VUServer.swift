//
//  VUServer.swift
//  AudioVUMeter
//
//  TCP Server for external applications to send VU meter data
//  Allows other apps to control the physical VU meters remotely
//

import Foundation
import Network

/// Server protocol for incoming data
enum ServerProtocol: String, CaseIterable, Identifiable {
    case vuProtocol = "VU Protocol (#channel:value)"
    case json = "JSON ({\"dials\":[...]})"
    case rawBytes = "Raw Bytes (binary)"

    var id: String { rawValue }
}

/// Connected client info
struct ConnectedClient: Identifiable {
    let id: UUID
    let address: String
    let connectedAt: Date
    var lastActivity: Date
    var bytesReceived: UInt64
    var bytesSent: UInt64

    init(address: String) {
        self.id = UUID()
        self.address = address
        self.connectedAt = Date()
        self.lastActivity = Date()
        self.bytesReceived = 0
        self.bytesSent = 0
    }
}

/// Server options
struct ServerOptions: Codable {
    var enabled: Bool = false
    var port: UInt16 = 9876
    var allowRemote: Bool = false  // Only localhost by default
    var requireAuth: Bool = false
    var authToken: String = ""
    var broadcastLevels: Bool = true  // Send current levels to clients
    var broadcastInterval: Double = 0.1  // 10 Hz
    var maxClients: Int = 5
    var protocol_: String = ServerProtocol.vuProtocol.rawValue

    var serverProtocol: ServerProtocol {
        get { ServerProtocol(rawValue: protocol_) ?? .vuProtocol }
        set { protocol_ = newValue.rawValue }
    }
}

/// VU Meter Server for external app connections
class VUServer: ObservableObject {
    // MARK: - Published Properties

    @Published var isRunning = false
    @Published var options = ServerOptions()
    @Published var connectedClients: [ConnectedClient] = []
    @Published var lastError: String?
    @Published var totalBytesReceived: UInt64 = 0
    @Published var totalBytesSent: UInt64 = 0
    @Published var lastReceivedCommand: String = ""

    // Received dial values from clients (override local values when set)
    @Published var receivedDialValues: [Int]? = nil  // nil = use local values
    @Published var externalControlActive = false

    // MARK: - Private Properties

    private var listener: NWListener?
    private var connections: [UUID: NWConnection] = [:]
    private let queue = DispatchQueue(label: "vu.server", qos: .userInteractive)
    private var broadcastTimer: Timer?

    // Reference to serial manager for broadcasting
    weak var serialManager: SerialManager?

    // MARK: - Initialization

    init() {
        loadOptions()
    }

    deinit {
        stop()
    }

    // MARK: - Server Control

    /// Start the server
    func start() {
        guard !isRunning else { return }

        do {
            // Configure parameters
            let parameters = NWParameters.tcp
            parameters.allowLocalEndpointReuse = true

            // Create listener
            let port = NWEndpoint.Port(rawValue: options.port)!
            listener = try NWListener(using: parameters, on: port)

            listener?.stateUpdateHandler = { [weak self] state in
                DispatchQueue.main.async {
                    self?.handleListenerState(state)
                }
            }

            listener?.newConnectionHandler = { [weak self] connection in
                self?.handleNewConnection(connection)
            }

            listener?.start(queue: queue)

            DispatchQueue.main.async {
                self.isRunning = true
                self.lastError = nil
            }

            // Start broadcast timer if enabled
            if options.broadcastLevels {
                startBroadcastTimer()
            }

            print("VU Server started on port \(options.port)")

        } catch {
            DispatchQueue.main.async {
                self.lastError = "Failed to start server: \(error.localizedDescription)"
                self.isRunning = false
            }
        }
    }

    /// Stop the server
    func stop() {
        stopBroadcastTimer()

        // Close all connections
        for (_, connection) in connections {
            connection.cancel()
        }
        connections.removeAll()

        // Stop listener
        listener?.cancel()
        listener = nil

        DispatchQueue.main.async {
            self.isRunning = false
            self.connectedClients.removeAll()
            self.externalControlActive = false
            self.receivedDialValues = nil
        }

        print("VU Server stopped")
    }

    /// Toggle server state
    func toggle() {
        if isRunning {
            stop()
        } else {
            start()
        }
    }

    /// Restart server (after options change)
    func restart() {
        if isRunning {
            stop()
            DispatchQueue.main.asyncAfter(deadline: .now() + 0.5) {
                self.start()
            }
        }
    }

    // MARK: - Connection Handling

    private func handleListenerState(_ state: NWListener.State) {
        switch state {
        case .ready:
            print("Server ready on port \(options.port)")
        case .failed(let error):
            lastError = "Server failed: \(error.localizedDescription)"
            isRunning = false
        case .cancelled:
            isRunning = false
        default:
            break
        }
    }

    private func handleNewConnection(_ connection: NWConnection) {
        let clientID = UUID()

        // Check max clients
        if connections.count >= options.maxClients {
            connection.cancel()
            return
        }

        // Check if remote connections are allowed
        if !options.allowRemote {
            if let endpoint = connection.currentPath?.remoteEndpoint,
               case let .hostPort(host, _) = endpoint {
                let hostStr = "\(host)"
                if !hostStr.contains("127.0.0.1") && !hostStr.contains("localhost") && !hostStr.contains("::1") {
                    connection.cancel()
                    return
                }
            }
        }

        connections[clientID] = connection

        // Get client address
        var address = "Unknown"
        if let endpoint = connection.currentPath?.remoteEndpoint,
           case let .hostPort(host, port) = endpoint {
            address = "\(host):\(port)"
        }

        let client = ConnectedClient(address: address)

        DispatchQueue.main.async {
            self.connectedClients.append(client)
        }

        connection.stateUpdateHandler = { [weak self] state in
            self?.handleConnectionState(clientID: clientID, state: state)
        }

        connection.start(queue: queue)

        // Start receiving data
        receiveData(clientID: clientID, connection: connection)

        // Send welcome message
        let welcome = "VU-Server Ready\n"
        sendData(welcome.data(using: .utf8)!, to: clientID)
    }

    private func handleConnectionState(clientID: UUID, state: NWConnection.State) {
        switch state {
        case .ready:
            print("Client connected: \(clientID)")
        case .failed(_), .cancelled:
            removeClient(clientID: clientID)
        default:
            break
        }
    }

    private func removeClient(clientID: UUID) {
        connections[clientID]?.cancel()
        connections.removeValue(forKey: clientID)

        DispatchQueue.main.async {
            self.connectedClients.removeAll { $0.id == clientID }

            // If no more clients, disable external control
            if self.connectedClients.isEmpty {
                self.externalControlActive = false
                self.receivedDialValues = nil
            }
        }
    }

    // MARK: - Data Handling

    private func receiveData(clientID: UUID, connection: NWConnection) {
        connection.receive(minimumIncompleteLength: 1, maximumLength: 1024) { [weak self] data, _, isComplete, error in
            guard let self = self else { return }

            if let data = data, !data.isEmpty {
                DispatchQueue.main.async {
                    self.totalBytesReceived += UInt64(data.count)

                    if let index = self.connectedClients.firstIndex(where: { $0.id == clientID }) {
                        self.connectedClients[index].bytesReceived += UInt64(data.count)
                        self.connectedClients[index].lastActivity = Date()
                    }
                }

                self.processReceivedData(data, from: clientID)
            }

            if let error = error {
                print("Receive error: \(error)")
                self.removeClient(clientID: clientID)
                return
            }

            if isComplete {
                self.removeClient(clientID: clientID)
                return
            }

            // Continue receiving
            self.receiveData(clientID: clientID, connection: connection)
        }
    }

    private func processReceivedData(_ data: Data, from clientID: UUID) {
        // Try to parse based on protocol
        switch options.serverProtocol {
        case .vuProtocol:
            parseVUProtocol(data, from: clientID)
        case .json:
            parseJSON(data, from: clientID)
        case .rawBytes:
            parseRawBytes(data, from: clientID)
        }
    }

    /// Parse VU Protocol: #channel:value\n
    private func parseVUProtocol(_ data: Data, from clientID: UUID) {
        guard let string = String(data: data, encoding: .utf8) else { return }

        DispatchQueue.main.async {
            self.lastReceivedCommand = string.trimmingCharacters(in: .whitespacesAndNewlines)
        }

        // Parse commands line by line
        let lines = string.components(separatedBy: .newlines)
        var values = receivedDialValues ?? [0, 0, 0, 0]
        var hasUpdate = false

        for line in lines {
            let trimmed = line.trimmingCharacters(in: .whitespaces)

            // Handle special commands
            if trimmed == "?" || trimmed == "STATUS" {
                sendStatus(to: clientID)
                continue
            }

            if trimmed == "RELEASE" {
                // Release external control
                DispatchQueue.main.async {
                    self.externalControlActive = false
                    self.receivedDialValues = nil
                }
                sendData("OK:RELEASED\n".data(using: .utf8)!, to: clientID)
                continue
            }

            // Parse #channel:value format
            if trimmed.hasPrefix("#") {
                let parts = trimmed.dropFirst().components(separatedBy: ":")
                if parts.count == 2,
                   let channel = Int(parts[0]),
                   let value = Int(parts[1]),
                   channel >= 0 && channel < 4 {
                    // Value is percentage 0-100, convert to 0-255
                    let byteValue = (value * 255) / 100
                    values[channel] = max(0, min(255, byteValue))
                    hasUpdate = true
                }
            }

            // Also support CH1:value format
            if trimmed.hasPrefix("CH") {
                let parts = trimmed.dropFirst(2).components(separatedBy: ":")
                if parts.count == 2,
                   let channel = Int(parts[0]),
                   let value = Int(parts[1]),
                   channel >= 1 && channel <= 4 {
                    values[channel - 1] = max(0, min(255, value))
                    hasUpdate = true
                }
            }
        }

        if hasUpdate {
            DispatchQueue.main.async {
                self.receivedDialValues = values
                self.externalControlActive = true
            }
        }
    }

    /// Parse JSON: {"dials":[v1,v2,v3,v4]} or {"d0":v,"d1":v,...}
    private func parseJSON(_ data: Data, from clientID: UUID) {
        guard let json = try? JSONSerialization.jsonObject(with: data) as? [String: Any] else { return }

        DispatchQueue.main.async {
            if let str = String(data: data, encoding: .utf8) {
                self.lastReceivedCommand = str.trimmingCharacters(in: .whitespacesAndNewlines)
            }
        }

        var values = receivedDialValues ?? [0, 0, 0, 0]
        var hasUpdate = false

        // Array format
        if let dials = json["dials"] as? [Int] {
            for (i, v) in dials.prefix(4).enumerated() {
                values[i] = max(0, min(255, v))
            }
            hasUpdate = true
        }

        // Individual dial format
        for i in 0..<4 {
            if let v = json["d\(i)"] as? Int {
                values[i] = max(0, min(255, v))
                hasUpdate = true
            }
        }

        // Command handling
        if let cmd = json["cmd"] as? String {
            switch cmd {
            case "status":
                sendStatus(to: clientID)
            case "release":
                DispatchQueue.main.async {
                    self.externalControlActive = false
                    self.receivedDialValues = nil
                }
            default:
                break
            }
        }

        if hasUpdate {
            DispatchQueue.main.async {
                self.receivedDialValues = values
                self.externalControlActive = true
            }
        }
    }

    /// Parse raw bytes: [0xAA, d1, d2, d3, d4, 0x55]
    private func parseRawBytes(_ data: Data, from clientID: UUID) {
        DispatchQueue.main.async {
            self.lastReceivedCommand = data.map { String(format: "%02X", $0) }.joined(separator: " ")
        }

        // Look for frame: 0xAA ... 0x55
        var values = receivedDialValues ?? [0, 0, 0, 0]
        let bytes = Array(data)

        var i = 0
        while i < bytes.count {
            if bytes[i] == 0xAA && i + 5 < bytes.count && bytes[i + 5] == 0x55 {
                // Found valid frame
                for j in 0..<4 {
                    values[j] = Int(bytes[i + 1 + j])
                }

                DispatchQueue.main.async {
                    self.receivedDialValues = values
                    self.externalControlActive = true
                }

                i += 6
            } else {
                i += 1
            }
        }
    }

    // MARK: - Send Data

    private func sendData(_ data: Data, to clientID: UUID) {
        guard let connection = connections[clientID] else { return }

        connection.send(content: data, completion: .contentProcessed { [weak self] error in
            if error == nil {
                DispatchQueue.main.async {
                    self?.totalBytesSent += UInt64(data.count)

                    if let index = self?.connectedClients.firstIndex(where: { $0.id == clientID }) {
                        self?.connectedClients[index].bytesSent += UInt64(data.count)
                    }
                }
            }
        })
    }

    private func sendStatus(to clientID: UUID) {
        guard let sm = serialManager else { return }

        let status: [String: Any] = [
            "connected": sm.isConnected,
            "dials": sm.dialValues,
            "port": sm.selectedPortPath,
            "external": externalControlActive
        ]

        if let data = try? JSONSerialization.data(withJSONObject: status, options: []),
           let string = String(data: data, encoding: .utf8) {
            sendData((string + "\n").data(using: .utf8)!, to: clientID)
        }
    }

    /// Broadcast current levels to all clients
    func broadcastLevels() {
        guard !connections.isEmpty, let sm = serialManager else { return }

        let message: String

        switch options.serverProtocol {
        case .vuProtocol:
            message = sm.dialValues.enumerated()
                .map { "#\($0.offset):\(($0.element * 100) / 255)" }
                .joined(separator: "\n") + "\n"
        case .json:
            message = "{\"dials\":[\(sm.dialValues.map(String.init).joined(separator: ","))]}\n"
        case .rawBytes:
            // Don't broadcast for raw bytes
            return
        }

        guard let data = message.data(using: .utf8) else { return }

        for clientID in connections.keys {
            sendData(data, to: clientID)
        }
    }

    // MARK: - Broadcast Timer

    private func startBroadcastTimer() {
        stopBroadcastTimer()

        DispatchQueue.main.async {
            self.broadcastTimer = Timer.scheduledTimer(withTimeInterval: self.options.broadcastInterval, repeats: true) { [weak self] _ in
                self?.broadcastLevels()
            }
        }
    }

    private func stopBroadcastTimer() {
        broadcastTimer?.invalidate()
        broadcastTimer = nil
    }

    // MARK: - Persistence

    private func loadOptions() {
        if let data = UserDefaults.standard.data(forKey: "VUServerOptions"),
           let loaded = try? JSONDecoder().decode(ServerOptions.self, from: data) {
            options = loaded
        }
    }

    func saveOptions() {
        if let data = try? JSONEncoder().encode(options) {
            UserDefaults.standard.set(data, forKey: "VUServerOptions")
        }
    }

    // MARK: - Disconnect Client

    func disconnectClient(_ client: ConnectedClient) {
        removeClient(clientID: client.id)
    }
}
