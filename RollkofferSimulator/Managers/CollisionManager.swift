//
//  CollisionManager.swift
//  RollkofferSimulator
//
//  Created by Ingo K.
//

import SpriteKit

/// Protocol for collision event handling
protocol CollisionManagerDelegate: AnyObject {
    func didCollectGoodDog(points: Int)
    func didCollectGreenHuman(points: Int)
    func didHitHarmfulEntity()
}

/// Manages collision detection and response
class CollisionManager: NSObject, SKPhysicsContactDelegate {

    // MARK: - Properties
    weak var delegate: CollisionManagerDelegate?

    // MARK: - SKPhysicsContactDelegate
    func didBegin(_ contact: SKPhysicsContact) {
        let collision = contact.bodyA.categoryBitMask | contact.bodyB.categoryBitMask

        // Check if suitcase is involved
        guard collision & Constants.PhysicsCategory.suitcase != 0 else { return }

        let otherBody: SKPhysicsBody
        if contact.bodyA.categoryBitMask == Constants.PhysicsCategory.suitcase {
            otherBody = contact.bodyB
        } else {
            otherBody = contact.bodyA
        }

        handleCollision(with: otherBody)
    }

    // MARK: - Private Methods
    private func handleCollision(with body: SKPhysicsBody) {
        guard let node = body.node else { return }

        switch body.categoryBitMask {
        case Constants.PhysicsCategory.goodDog:
            handleGoodDogCollision(node: node)

        case Constants.PhysicsCategory.badDog:
            handleBadDogCollision(node: node)

        case Constants.PhysicsCategory.greenHuman:
            handleGreenHumanCollision(node: node)

        case Constants.PhysicsCategory.grayHuman:
            handleGrayHumanCollision(node: node)

        default:
            break
        }
    }

    private func handleGoodDogCollision(node: SKNode) {
        guard let dogNode = node as? DogNode else { return }

        let points = dogNode.dogType.points
        showCollectEffect(at: node.position, color: .green, text: "+\(points)")
        node.removeFromParent()
        delegate?.didCollectGoodDog(points: points)
    }

    private func handleBadDogCollision(node: SKNode) {
        showDamageEffect(at: node.position)
        node.removeFromParent()
        delegate?.didHitHarmfulEntity()
    }

    private func handleGreenHumanCollision(node: SKNode) {
        guard let humanNode = node as? HumanNode else { return }

        let points = humanNode.humanType.points
        showCollectEffect(at: node.position, color: .green, text: "+\(points)")
        node.removeFromParent()
        delegate?.didCollectGreenHuman(points: points)
    }

    private func handleGrayHumanCollision(node: SKNode) {
        showDamageEffect(at: node.position)
        node.removeFromParent()
        delegate?.didHitHarmfulEntity()
    }

    // MARK: - Visual Effects
    private func showCollectEffect(at position: CGPoint, color: SKColor, text: String) {
        guard let scene = getScene() else { return }

        // Particle burst
        let emitter = SKEmitterNode()
        emitter.particleTexture = nil
        emitter.particleBirthRate = 50
        emitter.numParticlesToEmit = 20
        emitter.particleLifetime = 0.5
        emitter.particleSpeed = 100
        emitter.particleSpeedRange = 50
        emitter.emissionAngleRange = .pi * 2
        emitter.particleScale = 0.3
        emitter.particleScaleRange = 0.2
        emitter.particleColor = color
        emitter.particleColorBlendFactor = 1.0
        emitter.position = position
        emitter.zPosition = Constants.ZPosition.ui - 1

        // Create a simple circle shape for particles
        let shape = SKShapeNode(circleOfRadius: 5)
        shape.fillColor = color
        shape.strokeColor = .clear
        if let texture = scene.view?.texture(from: shape) {
            emitter.particleTexture = texture
        }

        scene.addChild(emitter)

        let waitAction = SKAction.wait(forDuration: 1.0)
        let removeAction = SKAction.removeFromParent()
        emitter.run(SKAction.sequence([waitAction, removeAction]))

        // Floating text
        let label = SKLabelNode(text: text)
        label.fontName = "AvenirNext-Bold"
        label.fontSize = 24
        label.fontColor = color
        label.position = position
        label.zPosition = Constants.ZPosition.ui

        scene.addChild(label)

        let moveUp = SKAction.moveBy(x: 0, y: 50, duration: 0.5)
        let fadeOut = SKAction.fadeOut(withDuration: 0.5)
        let group = SKAction.group([moveUp, fadeOut])
        let remove = SKAction.removeFromParent()
        label.run(SKAction.sequence([group, remove]))
    }

    private func showDamageEffect(at position: CGPoint) {
        guard let scene = getScene() else { return }

        // Red flash
        let flash = SKShapeNode(circleOfRadius: 30)
        flash.fillColor = .red
        flash.strokeColor = .clear
        flash.alpha = 0.7
        flash.position = position
        flash.zPosition = Constants.ZPosition.ui - 1

        scene.addChild(flash)

        let scaleUp = SKAction.scale(to: 2.0, duration: 0.2)
        let fadeOut = SKAction.fadeOut(withDuration: 0.2)
        let group = SKAction.group([scaleUp, fadeOut])
        let remove = SKAction.removeFromParent()
        flash.run(SKAction.sequence([group, remove]))

        // Floating text
        let label = SKLabelNode(text: "-1 ❤️")
        label.fontName = "AvenirNext-Bold"
        label.fontSize = 24
        label.fontColor = .red
        label.position = position
        label.zPosition = Constants.ZPosition.ui

        scene.addChild(label)

        let moveUp = SKAction.moveBy(x: 0, y: 50, duration: 0.5)
        let labelFadeOut = SKAction.fadeOut(withDuration: 0.5)
        let labelGroup = SKAction.group([moveUp, labelFadeOut])
        let labelRemove = SKAction.removeFromParent()
        label.run(SKAction.sequence([labelGroup, labelRemove]))
    }

    private func getScene() -> SKScene? {
        // This would typically be set via dependency injection
        // For simplicity, we'll use the notification pattern
        return nil
    }
}

// MARK: - Scene Reference Extension
extension CollisionManager {
    private static var sceneReference: SKScene?

    func setScene(_ scene: SKScene) {
        CollisionManager.sceneReference = scene
    }

    private func getSceneFromReference() -> SKScene? {
        return CollisionManager.sceneReference
    }
}
