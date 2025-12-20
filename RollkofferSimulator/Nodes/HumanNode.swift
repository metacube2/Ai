//
//  HumanNode.swift
//  RollkofferSimulator
//
//  Created by Ingo K.
//

import SpriteKit

/// A human entity node
class HumanNode: SKNode {

    // MARK: - Properties
    let humanType: HumanType
    private let bodyNode: SKShapeNode

    // MARK: - Initialization
    init(type: HumanType) {
        self.humanType = type

        let size = Constants.humanSize
        let color: SKColor

        switch type {
        case .green:
            color = Constants.Colors.greenHumanColor
        case .gray:
            color = Constants.Colors.grayHumanColor
        }

        // Create body (torso)
        let torsoHeight = size.height * 0.4
        let torsoWidth = size.width * 0.6
        let torsoRect = CGRect(x: -torsoWidth / 2, y: -size.height * 0.1,
                               width: torsoWidth, height: torsoHeight)
        bodyNode = SKShapeNode(rect: torsoRect, cornerRadius: 5)
        bodyNode.fillColor = color
        bodyNode.strokeColor = color.withAlphaComponent(0.7)
        bodyNode.lineWidth = 2

        super.init()

        addChild(bodyNode)

        // Add head
        let headRadius = size.width * 0.25
        let head = SKShapeNode(circleOfRadius: headRadius)
        head.position = CGPoint(x: 0, y: size.height * 0.35)
        head.fillColor = SKColor(red: 1.0, green: 0.87, blue: 0.77, alpha: 1.0) // Skin tone
        head.strokeColor = SKColor(red: 0.9, green: 0.75, blue: 0.65, alpha: 1.0)
        head.lineWidth = 1
        addChild(head)

        // Add eyes
        let eyeSize: CGFloat = 3
        for xOffset in [-headRadius * 0.35, headRadius * 0.35] {
            let eye = SKShapeNode(circleOfRadius: eyeSize)
            eye.position = CGPoint(x: xOffset, y: size.height * 0.37)
            eye.fillColor = .black
            eye.strokeColor = .clear
            addChild(eye)
        }

        // Add mouth/expression
        if type == .green {
            // Happy smile
            let smilePath = CGMutablePath()
            smilePath.addArc(center: CGPoint(x: 0, y: size.height * 0.32),
                             radius: headRadius * 0.3,
                             startAngle: .pi * 0.2,
                             endAngle: .pi * 0.8,
                             clockwise: true)
            let smile = SKShapeNode(path: smilePath)
            smile.strokeColor = .black
            smile.lineWidth = 2
            smile.lineCap = .round
            addChild(smile)
        } else {
            // Neutral/frowning expression
            let frownPath = CGMutablePath()
            frownPath.move(to: CGPoint(x: -headRadius * 0.25, y: size.height * 0.3))
            frownPath.addLine(to: CGPoint(x: headRadius * 0.25, y: size.height * 0.3))
            let frown = SKShapeNode(path: frownPath)
            frown.strokeColor = .black
            frown.lineWidth = 2
            addChild(frown)
        }

        // Add arms
        let armWidth: CGFloat = 6
        let armLength = size.height * 0.3
        for xOffset in [-torsoWidth / 2 - armWidth / 2, torsoWidth / 2 + armWidth / 2] {
            let arm = SKShapeNode(rect: CGRect(x: xOffset - armWidth / 2,
                                                y: size.height * 0.05,
                                                width: armWidth, height: armLength),
                                  cornerRadius: armWidth / 2)
            arm.fillColor = color
            arm.strokeColor = color.withAlphaComponent(0.7)
            arm.lineWidth = 1
            addChild(arm)

            // Add hand
            let hand = SKShapeNode(circleOfRadius: armWidth * 0.8)
            hand.position = CGPoint(x: xOffset, y: size.height * 0.05)
            hand.fillColor = SKColor(red: 1.0, green: 0.87, blue: 0.77, alpha: 1.0)
            hand.strokeColor = .clear
            addChild(hand)
        }

        // Add legs
        let legWidth: CGFloat = 10
        let legHeight = size.height * 0.35
        for xOffset in [-torsoWidth * 0.25, torsoWidth * 0.25] {
            let leg = SKShapeNode(rect: CGRect(x: xOffset - legWidth / 2,
                                                y: -size.height * 0.45,
                                                width: legWidth, height: legHeight),
                                  cornerRadius: legWidth / 2)
            leg.fillColor = SKColor(red: 0.2, green: 0.2, blue: 0.4, alpha: 1.0) // Pants color
            leg.strokeColor = SKColor(red: 0.15, green: 0.15, blue: 0.3, alpha: 1.0)
            leg.lineWidth = 1
            addChild(leg)

            // Add shoe
            let shoe = SKShapeNode(rect: CGRect(x: xOffset - legWidth * 0.6,
                                                 y: -size.height * 0.48,
                                                 width: legWidth * 1.2, height: 6),
                                   cornerRadius: 2)
            shoe.fillColor = .black
            shoe.strokeColor = .clear
            addChild(shoe)
        }

        // Add indicator icon for type
        if type == .green {
            let checkmark = SKLabelNode(text: "✓")
            checkmark.fontSize = 16
            checkmark.fontColor = .white
            checkmark.position = CGPoint(x: 0, y: size.height * 0.1)
            checkmark.verticalAlignmentMode = .center
            addChild(checkmark)
        } else {
            let xMark = SKLabelNode(text: "✗")
            xMark.fontSize = 16
            xMark.fontColor = .white
            xMark.position = CGPoint(x: 0, y: size.height * 0.1)
            xMark.verticalAlignmentMode = .center
            addChild(xMark)
        }

        setupPhysics(size: size)
        self.zPosition = Constants.ZPosition.entities
        self.name = type.isHarmful ? "grayHuman" : "greenHuman"
    }

    required init?(coder aDecoder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    // MARK: - Physics Setup
    private func setupPhysics(size: CGSize) {
        physicsBody = SKPhysicsBody(rectangleOf: CGSize(width: size.width * 0.6,
                                                         height: size.height * 0.8))
        physicsBody?.isDynamic = true
        physicsBody?.affectedByGravity = false
        physicsBody?.allowsRotation = false

        if humanType.isHarmful {
            physicsBody?.categoryBitMask = Constants.PhysicsCategory.grayHuman
        } else {
            physicsBody?.categoryBitMask = Constants.PhysicsCategory.greenHuman
        }

        physicsBody?.contactTestBitMask = Constants.PhysicsCategory.suitcase
        physicsBody?.collisionBitMask = Constants.PhysicsCategory.none
    }
}
