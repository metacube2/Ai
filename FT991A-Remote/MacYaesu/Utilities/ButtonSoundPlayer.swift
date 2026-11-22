//
//  ButtonSoundPlayer.swift
//  FT991A-Remote
//

import AVFAudio
import Foundation

final class ButtonSoundPlayer {
    static let shared = ButtonSoundPlayer()

    private var player: AVAudioPlayer?
    private var didLogMissingFile = false

    private init() { }

    func playPress() {
        guard let soundURL = resolvePressSoundURL() else {
            if !didLogMissingFile {
                Logger.shared.log("press.wav not found. Place it in the app bundle or Application Support/FT991A-Remote/press.wav.", level: .warning)
                didLogMissingFile = true
            }
            return
        }

        do {
            player = try AVAudioPlayer(contentsOf: soundURL)
            player?.volume = 0.9
            player?.prepareToPlay()
            player?.play()
        } catch {
            Logger.shared.log("Failed to play press.wav: \(error.localizedDescription)", level: .error)
        }
    }

    private func resolvePressSoundURL() -> URL? {
        if let bundled = Bundle.main.url(forResource: "press", withExtension: "wav") {
            return bundled
        }

        let appSupport = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask).first
        let supportURL = appSupport?
            .appendingPathComponent("FT991A-Remote", isDirectory: true)
            .appendingPathComponent("press.wav")

        if let supportURL, FileManager.default.fileExists(atPath: supportURL.path) {
            return supportURL
        }

        return nil
    }
}
