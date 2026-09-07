//
//  ParrotStore.swift
//  FT991A-Remote
//

import Foundation

@MainActor
final class ParrotStore: ObservableObject {
    static let maximumDuration: TimeInterval = 60

    @Published private(set) var messages: [ParrotMessage] = []

    private let directoryURL: URL
    private let indexURL: URL

    init() {
        let appSupport = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask).first!
        directoryURL = appSupport
            .appendingPathComponent("FT991A-Remote", isDirectory: true)
            .appendingPathComponent("Parrot", isDirectory: true)
        indexURL = directoryURL.appendingPathComponent("messages.json")

        try? FileManager.default.createDirectory(at: directoryURL, withIntermediateDirectories: true)
        load()
    }

    func saveRecording(from temporaryURL: URL, name: String, duration: TimeInterval) throws -> ParrotMessage {
        let cleanName = name.trimmingCharacters(in: .whitespacesAndNewlines)
        let message = ParrotMessage(
            name: cleanName.isEmpty ? "Parrot \(Date.now.formatted(date: .abbreviated, time: .shortened))" : cleanName,
            fileName: UUID().uuidString + ".m4a",
            duration: min(duration, Self.maximumDuration)
        )

        let destinationURL = fileURL(for: message)
        if FileManager.default.fileExists(atPath: destinationURL.path) {
            try FileManager.default.removeItem(at: destinationURL)
        }
        try FileManager.default.copyItem(at: temporaryURL, to: destinationURL)

        messages.insert(message, at: 0)
        persist()
        return message
    }

    func renameMessage(id: UUID, name: String) {
        guard let index = messages.firstIndex(where: { $0.id == id }) else { return }
        let cleanName = name.trimmingCharacters(in: .whitespacesAndNewlines)
        messages[index].name = cleanName.isEmpty ? messages[index].name : cleanName
        messages[index].updatedAt = Date()
        sortMessages()
        persist()
    }

    func deleteMessage(id: UUID) {
        guard let index = messages.firstIndex(where: { $0.id == id }) else { return }
        let message = messages.remove(at: index)
        try? FileManager.default.removeItem(at: fileURL(for: message))
        persist()
    }

    func fileURL(for message: ParrotMessage) -> URL {
        directoryURL.appendingPathComponent(message.fileName)
    }

    private func load() {
        guard let data = try? Data(contentsOf: indexURL) else { return }
        guard let decoded = try? JSONDecoder.iso8601.decode([ParrotMessage].self, from: data) else { return }
        messages = decoded.filter { FileManager.default.fileExists(atPath: fileURL(for: $0).path) }
        sortMessages()
    }

    private func persist() {
        do {
            let data = try JSONEncoder.prettyPrinted.encode(messages)
            try data.write(to: indexURL, options: .atomic)
        } catch {
            Logger.shared.log("ParrotStore save failed: \(error.localizedDescription)", level: .error)
        }
    }

    private func sortMessages() {
        messages.sort { lhs, rhs in
            if lhs.updatedAt == rhs.updatedAt {
                return lhs.createdAt > rhs.createdAt
            }
            return lhs.updatedAt > rhs.updatedAt
        }
    }
}
