//
//  Constants.swift
//  RollkofferSimulator
//
//  Created by Ingo K.
//

import SpriteKit

struct Constants {

    // MARK: - Game Settings
    static let gameTime: TimeInterval = 90.0
    static let startLives: Int = 3
    static let targetDogs: Int = 10
    static let targetHumans: Int = 5

    // MARK: - Points
    static let pointsSmallGoodDog: Int = 10
    static let pointsBigGoodDog: Int = 25
    static let pointsGreenHuman: Int = 15

    // MARK: - Spawn Settings
    static let spawnIntervalMin: TimeInterval = 0.8
    static let spawnIntervalMax: TimeInterval = 1.5
    static let scrollSpeed: CGFloat = 200.0

    // MARK: - Spawn Distribution (cumulative percentages)
    static let spawnChanceGoodDog: Int = 40      // 0-39: good dogs
    static let spawnChanceBadDog: Int = 60       // 40-59: bad dogs (20%)
    static let spawnChanceGreenHuman: Int = 85   // 60-84: green humans (25%)
    // 85-99: gray humans (15%)

    // MARK: - Sprite Sizes
    static let suitcaseSize = CGSize(width: 60, height: 80)
    static let smallDogSize = CGSize(width: 40, height: 40)
    static let bigDogSize = CGSize(width: 70, height: 70)
    static let badDogSize = CGSize(width: 55, height: 55)
    static let humanSize = CGSize(width: 50, height: 80)

    // MARK: - Physics Categories (Bitmasks)
    struct PhysicsCategory {
        static let none: UInt32       = 0
        static let suitcase: UInt32   = 0x1 << 0
        static let goodDog: UInt32    = 0x1 << 1
        static let badDog: UInt32     = 0x1 << 2
        static let greenHuman: UInt32 = 0x1 << 3
        static let grayHuman: UInt32  = 0x1 << 4

        static let collectible: UInt32 = goodDog | greenHuman
        static let harmful: UInt32 = badDog | grayHuman
        static let all: UInt32 = goodDog | badDog | greenHuman | grayHuman
    }

    // MARK: - Colors
    struct Colors {
        static let goodDogColor = SKColor(red: 0.85, green: 0.65, blue: 0.13, alpha: 1.0) // Gold brown
        static let badDogColor = SKColor.red
        static let greenHumanColor = SKColor(red: 0.2, green: 0.8, blue: 0.2, alpha: 1.0)
        static let grayHumanColor = SKColor.gray
        static let suitcaseColor = SKColor(red: 0.4, green: 0.3, blue: 0.6, alpha: 1.0)
        static let backgroundColor = SKColor(red: 0.9, green: 0.9, blue: 0.85, alpha: 1.0)
        static let floorColor = SKColor(red: 0.75, green: 0.75, blue: 0.7, alpha: 1.0)
    }

    // MARK: - Z-Positions
    struct ZPosition {
        static let background: CGFloat = 0
        static let floor: CGFloat = 1
        static let entities: CGFloat = 10
        static let player: CGFloat = 20
        static let ui: CGFloat = 100
    }

    // MARK: - Animation
    static let blinkDuration: TimeInterval = 0.1
    static let blinkCount: Int = 6
    static let invincibilityDuration: TimeInterval = 1.5
}
