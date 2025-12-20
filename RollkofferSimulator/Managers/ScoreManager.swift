//
//  ScoreManager.swift
//  RollkofferSimulator
//
//  Created by Ingo K.
//

import Foundation

/// Manages high scores and game statistics
class ScoreManager {

    // MARK: - Singleton
    static let shared = ScoreManager()

    // MARK: - UserDefaults Keys
    private let highScoreKey = "RollkofferSimulator.HighScore"
    private let gamesPlayedKey = "RollkofferSimulator.GamesPlayed"
    private let totalDogsCollectedKey = "RollkofferSimulator.TotalDogsCollected"
    private let totalHumansCollectedKey = "RollkofferSimulator.TotalHumansCollected"
    private let victoriesKey = "RollkofferSimulator.Victories"

    // MARK: - Properties
    private let defaults = UserDefaults.standard

    var highScore: Int {
        get { defaults.integer(forKey: highScoreKey) }
        set { defaults.set(newValue, forKey: highScoreKey) }
    }

    var gamesPlayed: Int {
        get { defaults.integer(forKey: gamesPlayedKey) }
        set { defaults.set(newValue, forKey: gamesPlayedKey) }
    }

    var totalDogsCollected: Int {
        get { defaults.integer(forKey: totalDogsCollectedKey) }
        set { defaults.set(newValue, forKey: totalDogsCollectedKey) }
    }

    var totalHumansCollected: Int {
        get { defaults.integer(forKey: totalHumansCollectedKey) }
        set { defaults.set(newValue, forKey: totalHumansCollectedKey) }
    }

    var victories: Int {
        get { defaults.integer(forKey: victoriesKey) }
        set { defaults.set(newValue, forKey: victoriesKey) }
    }

    // MARK: - Initialization
    private init() {}

    // MARK: - Public Methods
    func recordGameEnd(score: Int, dogsCollected: Int, humansCollected: Int, didWin: Bool) {
        gamesPlayed += 1
        totalDogsCollected += dogsCollected
        totalHumansCollected += humansCollected

        if score > highScore {
            highScore = score
        }

        if didWin {
            victories += 1
        }
    }

    func isNewHighScore(_ score: Int) -> Bool {
        return score > highScore
    }

    func resetStatistics() {
        highScore = 0
        gamesPlayed = 0
        totalDogsCollected = 0
        totalHumansCollected = 0
        victories = 0
    }

    // MARK: - Formatted Statistics
    func getStatisticsText() -> String {
        return """
        High Score: \(highScore)
        Games Played: \(gamesPlayed)
        Victories: \(victories)
        Total Dogs: \(totalDogsCollected)
        Total Humans: \(totalHumansCollected)
        """
    }
}
