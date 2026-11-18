//
//  MemoryStore.swift
//  FT991A-Remote
//

import Foundation

@MainActor
final class MemoryStore: ObservableObject {
    static let maximumEntries = 100

    @Published private(set) var entries: [MemoryEntry] = []

    private let storageURL: URL

    init() {
        let appSupport = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask).first!
        let directory = appSupport.appendingPathComponent("FT991A-Remote", isDirectory: true)
        try? FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
        storageURL = directory.appendingPathComponent("memories.json")
        load()
    }

    var usedSlots: Int {
        entries.count
    }

    func saveEntry(_ entry: MemoryEntry) {
        let slot = min(max(1, entry.slot), Self.maximumEntries)
        var updated = entry
        updated.slot = slot
        updated.updatedAt = Date()

        if let index = entries.firstIndex(where: { $0.slot == slot }) {
            updated.createdAt = entries[index].createdAt
            entries[index] = updated
        } else {
            entries.append(updated)
        }

        sortEntries()
        persist()
    }

    func deleteEntry(slot: Int) {
        entries.removeAll { $0.slot == slot }
        persist()
    }

    func importEntries(from url: URL) throws {
        let data = try Data(contentsOf: url)
        let imported = try JSONDecoder().decode([MemoryEntry].self, from: data)
        entries = Array(imported
            .filter { (1...Self.maximumEntries).contains($0.slot) }
            .reduce(into: [Int: MemoryEntry]()) { partialResult, entry in
                partialResult[entry.slot] = entry
            }
            .values)
        sortEntries()
        persist()
    }

    func exportEntries(to url: URL) throws {
        let data = try JSONEncoder.prettyPrinted.encode(entries)
        try data.write(to: url, options: .atomic)
    }

    private func load() {
        guard let data = try? Data(contentsOf: storageURL) else { return }
        guard let decoded = try? JSONDecoder().decode([MemoryEntry].self, from: data) else { return }
        entries = decoded
        sortEntries()
    }

    private func persist() {
        do {
            let data = try JSONEncoder.prettyPrinted.encode(entries)
            try data.write(to: storageURL, options: .atomic)
        } catch {
            Logger.shared.log("MemoryStore save failed: \(error.localizedDescription)", level: .error)
        }
    }

    private func sortEntries() {
        entries.sort { lhs, rhs in
            if lhs.slot == rhs.slot {
                return lhs.updatedAt > rhs.updatedAt
            }
            return lhs.slot < rhs.slot
        }
    }
}

extension JSONEncoder {
    static var prettyPrinted: JSONEncoder {
        let encoder = JSONEncoder()
        encoder.outputFormatting = [.prettyPrinted, .sortedKeys]
        encoder.dateEncodingStrategy = .iso8601
        return encoder
    }
}

extension JSONDecoder {
    static var iso8601: JSONDecoder {
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        return decoder
    }
}
