//
//  GameState.swift
//  RollkofferSimulator
//
//  Created by Ingo K.
//

import Foundation

/// Possible game states
enum GameStateType {
    case startScreen
    case playing
    case paused
    case gameOver
    case victory
}

/// Manages the current game state and statistics
class GameState {

    // MARK: - Properties
    private(set) var currentState: GameStateType = .startScreen
    private(set) var score: Int = 0
    private(set) var lives: Int = Constants.startLives
    private(set) var dogsCollected: Int = 0
    private(set) var humansCollected: Int = 0
    private(set) var timeRemaining: TimeInterval = Constants.gameTime

    var isInvincible: Bool = false

    // MARK: - Computed Properties
    var hasWon: Bool {
        return dogsCollected >= Constants.targetDogs &&
               humansCollected >= Constants.targetHumans
    }

    var hasLost: Bool {
        return lives <= 0 || (timeRemaining <= 0 && !hasWon)
    }

    // MARK: - State Management
    func setState(_ state: GameStateType) {
        currentState = state
    }

    func reset() {
        score = 0
        lives = Constants.startLives
        dogsCollected = 0
        humansCollected = 0
        timeRemaining = Constants.gameTime
        isInvincible = false
        currentState = .playing
    }

    // MARK: - Score Management
    func addPoints(_ points: Int) {
        score += points
    }

    func collectDog() {
        dogsCollected += 1
    }

    func collectHuman() {
        humansCollected += 1
    }

    // MARK: - Lives Management
    func loseLife() -> Bool {
        guard !isInvincible else { return false }
        lives -= 1
        return true
    }

    // MARK: - Time Management
    func updateTime(delta: TimeInterval) {
        timeRemaining = max(0, timeRemaining - delta)
    }

    // MARK: - Formatted Strings
    var formattedTime: String {
        let seconds = Int(timeRemaining)
        return "\(seconds)s"
    }

    var formattedScore: String {
        return "SCORE: \(score)"
    }

    var formattedDogs: String {
        return "🐕 \(dogsCollected)/\(Constants.targetDogs)"
    }

    var formattedHumans: String {
        return "👤 \(humansCollected)/\(Constants.targetHumans)"
    }

    var formattedLives: String {
        return String(repeating: "❤️", count: lives)
    }
}
