//
//  EntityType.swift
//  RollkofferSimulator
//
//  Created by Ingo K.
//

import Foundation

/// Types of dogs in the game
enum DogType {
    case smallGood  // 40x40, gold brown, +10 points
    case bigGood    // 70x70, gold brown, +25 points
    case bad        // 55x55, red outlined, -1 life

    var points: Int {
        switch self {
        case .smallGood: return Constants.pointsSmallGoodDog
        case .bigGood: return Constants.pointsBigGoodDog
        case .bad: return 0
        }
    }

    var isHarmful: Bool {
        return self == .bad
    }

    var countsTowardGoal: Bool {
        return self == .smallGood || self == .bigGood
    }
}

/// Types of humans in the game
enum HumanType {
    case green  // 50x80, green, +15 points
    case gray   // 50x80, gray, -1 life

    var points: Int {
        switch self {
        case .green: return Constants.pointsGreenHuman
        case .gray: return 0
        }
    }

    var isHarmful: Bool {
        return self == .gray
    }

    var countsTowardGoal: Bool {
        return self == .green
    }
}

/// All entity types for spawn system
enum EntityType {
    case dog(DogType)
    case human(HumanType)
}
