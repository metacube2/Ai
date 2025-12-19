//
//  SpawnManager.swift
//  RollkofferSimulator
//
//  Created by Ingo K.
//

import SpriteKit

/// Manages spawning of entities at the top of the screen
class SpawnManager {

    // MARK: - Properties
    private weak var scene: SKScene?
    private var spawnTimer: TimeInterval = 0
    private var nextSpawnInterval: TimeInterval = 0
    private var isSpawning: Bool = false

    // MARK: - Initialization
    init(scene: SKScene) {
        self.scene = scene
        resetSpawnInterval()
    }

    // MARK: - Public Methods
    func startSpawning() {
        isSpawning = true
        resetSpawnInterval()
    }

    func stopSpawning() {
        isSpawning = false
    }

    func update(deltaTime: TimeInterval) {
        guard isSpawning else { return }

        spawnTimer += deltaTime

        if spawnTimer >= nextSpawnInterval {
            spawnEntity()
            spawnTimer = 0
            resetSpawnInterval()
        }
    }

    // MARK: - Private Methods
    private func resetSpawnInterval() {
        nextSpawnInterval = TimeInterval.random(in: Constants.spawnIntervalMin...Constants.spawnIntervalMax)
    }

    private func spawnEntity() {
        guard let scene = scene else { return }

        let entityType = determineEntityType()
        let entity = createEntity(type: entityType)

        // Random x position
        let margin: CGFloat = 60
        let minX = margin
        let maxX = scene.frame.width - margin
        let randomX = CGFloat.random(in: minX...maxX)

        // Spawn above screen
        entity.position = CGPoint(x: randomX, y: scene.frame.height + 50)

        scene.addChild(entity)

        // Move entity down
        let moveDistance = scene.frame.height + 200
        let moveDuration = moveDistance / Constants.scrollSpeed
        let moveAction = SKAction.moveBy(x: 0, y: -moveDistance, duration: moveDuration)
        let removeAction = SKAction.removeFromParent()
        entity.run(SKAction.sequence([moveAction, removeAction]))
    }

    private func determineEntityType() -> EntityType {
        let roll = Int.random(in: 0..<100)

        if roll < Constants.spawnChanceGoodDog {
            // 40% good dogs (split between small and big)
            let isSmall = Bool.random()
            return .dog(isSmall ? .smallGood : .bigGood)
        } else if roll < Constants.spawnChanceBadDog {
            // 20% bad dogs
            return .dog(.bad)
        } else if roll < Constants.spawnChanceGreenHuman {
            // 25% green humans
            return .human(.green)
        } else {
            // 15% gray humans
            return .human(.gray)
        }
    }

    private func createEntity(type: EntityType) -> SKNode {
        switch type {
        case .dog(let dogType):
            return DogNode(type: dogType)
        case .human(let humanType):
            return HumanNode(type: humanType)
        }
    }
}
