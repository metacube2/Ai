//
//  DogNode.swift
//  RollkofferSimulator
//
//  Created by Ingo K.
//

import SpriteKit

/// A dog entity node
class DogNode: SKNode {

    // MARK: - Properties
    let dogType: DogType
    private let bodyNode: SKShapeNode

    // MARK: - Initialization
    init(type: DogType) {
        self.dogType = type

        let size: CGSize
        let color: SKColor
        let hasRedOutline: Bool

        switch type {
        case .smallGood:
            size = Constants.smallDogSize
            color = Constants.Colors.goodDogColor
            hasRedOutline = false
        case .bigGood:
            size = Constants.bigDogSize
            color = Constants.Colors.goodDogColor
            hasRedOutline = false
        case .bad:
            size = Constants.badDogSize
            color = Constants.Colors.goodDogColor
            hasRedOutline = true
        }

        // Create dog body (simple oval shape)
        let bodyPath = CGMutablePath()
        bodyPath.addEllipse(in: CGRect(x: -size.width / 2, y: -size.height / 2,
                                        width: size.width, height: size.height * 0.7))
        bodyNode = SKShapeNode(path: bodyPath)
        bodyNode.fillColor = color
        bodyNode.strokeColor = hasRedOutline ? Constants.Colors.badDogColor : SKColor(white: 0.3, alpha: 1.0)
        bodyNode.lineWidth = hasRedOutline ? 4 : 2

        super.init()

        addChild(bodyNode)

        // Add head
        let headSize = size.width * 0.5
        let head = SKShapeNode(circleOfRadius: headSize / 2)
        head.position = CGPoint(x: 0, y: size.height * 0.25)
        head.fillColor = color
        head.strokeColor = hasRedOutline ? Constants.Colors.badDogColor : SKColor(white: 0.3, alpha: 1.0)
        head.lineWidth = hasRedOutline ? 3 : 1.5
        addChild(head)

        // Add ears
        let earSize = headSize * 0.4
        for xOffset in [-headSize * 0.4, headSize * 0.4] {
            let ear = SKShapeNode(ellipseOf: CGSize(width: earSize, height: earSize * 1.5))
            ear.position = CGPoint(x: xOffset, y: size.height * 0.25 + headSize * 0.35)
            ear.fillColor = color.withAlphaComponent(0.8)
            ear.strokeColor = hasRedOutline ? Constants.Colors.badDogColor : SKColor(white: 0.3, alpha: 1.0)
            ear.lineWidth = hasRedOutline ? 2 : 1
            addChild(ear)
        }

        // Add eyes
        let eyeSize: CGFloat = headSize * 0.15
        for xOffset in [-headSize * 0.15, headSize * 0.15] {
            let eye = SKShapeNode(circleOfRadius: eyeSize)
            eye.position = CGPoint(x: xOffset, y: size.height * 0.28)
            eye.fillColor = hasRedOutline ? .red : .black
            eye.strokeColor = .clear
            addChild(eye)
        }

        // Add nose
        let nose = SKShapeNode(circleOfRadius: headSize * 0.1)
        nose.position = CGPoint(x: 0, y: size.height * 0.18)
        nose.fillColor = .black
        nose.strokeColor = .clear
        addChild(nose)

        // Add tail
        let tailPath = CGMutablePath()
        tailPath.move(to: CGPoint(x: 0, y: -size.height * 0.25))
        tailPath.addQuadCurve(to: CGPoint(x: size.width * 0.3, y: -size.height * 0.1),
                               control: CGPoint(x: size.width * 0.4, y: -size.height * 0.3))
        let tail = SKShapeNode(path: tailPath)
        tail.strokeColor = color
        tail.lineWidth = size.width * 0.1
        tail.lineCap = .round
        addChild(tail)

        // Add legs
        let legWidth = size.width * 0.12
        let legHeight = size.height * 0.25
        let legPositions: [CGFloat] = [-size.width * 0.25, -size.width * 0.1,
                                        size.width * 0.1, size.width * 0.25]
        for xPos in legPositions {
            let leg = SKShapeNode(rect: CGRect(x: xPos - legWidth / 2,
                                                y: -size.height * 0.35 - legHeight,
                                                width: legWidth, height: legHeight),
                                  cornerRadius: legWidth / 2)
            leg.fillColor = color
            leg.strokeColor = hasRedOutline ? Constants.Colors.badDogColor : SKColor(white: 0.3, alpha: 1.0)
            leg.lineWidth = hasRedOutline ? 2 : 1
            addChild(leg)
        }

        // Add angry expression for bad dogs
        if hasRedOutline {
            let browPath = CGMutablePath()
            browPath.move(to: CGPoint(x: -headSize * 0.3, y: size.height * 0.35))
            browPath.addLine(to: CGPoint(x: -headSize * 0.05, y: size.height * 0.32))
            let leftBrow = SKShapeNode(path: browPath)
            leftBrow.strokeColor = .black
            leftBrow.lineWidth = 2
            addChild(leftBrow)

            let browPath2 = CGMutablePath()
            browPath2.move(to: CGPoint(x: headSize * 0.3, y: size.height * 0.35))
            browPath2.addLine(to: CGPoint(x: headSize * 0.05, y: size.height * 0.32))
            let rightBrow = SKShapeNode(path: browPath2)
            rightBrow.strokeColor = .black
            rightBrow.lineWidth = 2
            addChild(rightBrow)
        }

        setupPhysics(size: size)
        self.zPosition = Constants.ZPosition.entities
        self.name = type.isHarmful ? "badDog" : "goodDog"
    }

    required init?(coder aDecoder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    // MARK: - Physics Setup
    private func setupPhysics(size: CGSize) {
        let radius = min(size.width, size.height) / 2
        physicsBody = SKPhysicsBody(circleOfRadius: radius)
        physicsBody?.isDynamic = true
        physicsBody?.affectedByGravity = false
        physicsBody?.allowsRotation = false

        if dogType.isHarmful {
            physicsBody?.categoryBitMask = Constants.PhysicsCategory.badDog
        } else {
            physicsBody?.categoryBitMask = Constants.PhysicsCategory.goodDog
        }

        physicsBody?.contactTestBitMask = Constants.PhysicsCategory.suitcase
        physicsBody?.collisionBitMask = Constants.PhysicsCategory.none
    }
}
