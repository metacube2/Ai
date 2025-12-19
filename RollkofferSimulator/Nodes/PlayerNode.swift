//
//  PlayerNode.swift
//  RollkofferSimulator
//
//  Created by Ingo K.
//

import SpriteKit

/// The player-controlled suitcase node
class PlayerNode: SKNode {

    // MARK: - Properties
    private let suitcaseBody: SKShapeNode
    private let handle: SKShapeNode
    private let wheels: [SKShapeNode]

    // MARK: - Initialization
    override init() {
        let size = Constants.suitcaseSize

        // Create suitcase body (rounded rectangle)
        let bodyRect = CGRect(x: -size.width / 2, y: -size.height / 2 + 10,
                              width: size.width, height: size.height - 15)
        suitcaseBody = SKShapeNode(rect: bodyRect, cornerRadius: 8)
        suitcaseBody.fillColor = Constants.Colors.suitcaseColor
        suitcaseBody.strokeColor = SKColor(white: 0.2, alpha: 1.0)
        suitcaseBody.lineWidth = 2

        // Create handle
        let handlePath = CGMutablePath()
        handlePath.move(to: CGPoint(x: -10, y: size.height / 2 - 5))
        handlePath.addLine(to: CGPoint(x: -10, y: size.height / 2 + 15))
        handlePath.addLine(to: CGPoint(x: 10, y: size.height / 2 + 15))
        handlePath.addLine(to: CGPoint(x: 10, y: size.height / 2 - 5))
        handle = SKShapeNode(path: handlePath)
        handle.strokeColor = SKColor(white: 0.3, alpha: 1.0)
        handle.lineWidth = 4
        handle.lineCap = .round

        // Create wheels
        var tempWheels: [SKShapeNode] = []
        let wheelPositions = [
            CGPoint(x: -size.width / 2 + 8, y: -size.height / 2 + 5),
            CGPoint(x: size.width / 2 - 8, y: -size.height / 2 + 5)
        ]
        for pos in wheelPositions {
            let wheel = SKShapeNode(circleOfRadius: 6)
            wheel.position = pos
            wheel.fillColor = SKColor.darkGray
            wheel.strokeColor = SKColor.black
            wheel.lineWidth = 1
            tempWheels.append(wheel)
        }
        wheels = tempWheels

        super.init()

        // Add decorative stripes
        let stripe1 = SKShapeNode(rect: CGRect(x: -size.width / 2 + 5, y: 0,
                                                width: size.width - 10, height: 3))
        stripe1.fillColor = SKColor(white: 0.3, alpha: 0.5)
        stripe1.strokeColor = .clear

        let stripe2 = SKShapeNode(rect: CGRect(x: -size.width / 2 + 5, y: -15,
                                                width: size.width - 10, height: 3))
        stripe2.fillColor = SKColor(white: 0.3, alpha: 0.5)
        stripe2.strokeColor = .clear

        addChild(suitcaseBody)
        addChild(handle)
        addChild(stripe1)
        addChild(stripe2)
        for wheel in wheels {
            addChild(wheel)
        }

        setupPhysics()
        self.zPosition = Constants.ZPosition.player
        self.name = "player"
    }

    required init?(coder aDecoder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    // MARK: - Physics Setup
    private func setupPhysics() {
        let size = Constants.suitcaseSize
        physicsBody = SKPhysicsBody(rectangleOf: size)
        physicsBody?.isDynamic = true
        physicsBody?.affectedByGravity = false
        physicsBody?.allowsRotation = false
        physicsBody?.categoryBitMask = Constants.PhysicsCategory.suitcase
        physicsBody?.contactTestBitMask = Constants.PhysicsCategory.all
        physicsBody?.collisionBitMask = Constants.PhysicsCategory.none
    }

    // MARK: - Visual Effects
    func startBlinking() {
        let fadeOut = SKAction.fadeAlpha(to: 0.3, duration: Constants.blinkDuration)
        let fadeIn = SKAction.fadeAlpha(to: 1.0, duration: Constants.blinkDuration)
        let blink = SKAction.sequence([fadeOut, fadeIn])
        let blinkRepeat = SKAction.repeat(blink, count: Constants.blinkCount)
        run(blinkRepeat, withKey: "blink")
    }

    func stopBlinking() {
        removeAction(forKey: "blink")
        alpha = 1.0
    }

    // MARK: - Movement
    func constrainToScreen(in frame: CGRect) {
        let halfWidth = Constants.suitcaseSize.width / 2
        let halfHeight = Constants.suitcaseSize.height / 2

        let minX = frame.minX + halfWidth + 10
        let maxX = frame.maxX - halfWidth - 10
        let minY = frame.minY + halfHeight + 10
        let maxY = frame.maxY - halfHeight - 100 // Leave space for UI

        position.x = max(minX, min(maxX, position.x))
        position.y = max(minY, min(maxY, position.y))
    }
}
