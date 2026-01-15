//
//  VictoryScene.swift
//  RollkofferSimulator
//
//  Created by Ingo K.
//

import SpriteKit

class VictoryScene: SKScene {

    // MARK: - Properties
    var finalScore: Int = 0
    var dogsCollected: Int = 0
    var humansCollected: Int = 0
    var timeRemaining: TimeInterval = 0

    private var playAgainButton: SKShapeNode!
    private var menuButton: SKShapeNode!

    // MARK: - Scene Lifecycle
    override func didMove(to view: SKView) {
        setupBackground()
        setupContent()
        setupButtons()
        startCelebration()
    }

    // MARK: - Setup
    private func setupBackground() {
        backgroundColor = SKColor(red: 0.1, green: 0.2, blue: 0.1, alpha: 1.0)

        // Add celebration particles
        for _ in 0..<30 {
            let confetti = createConfetti()
            addChild(confetti)
        }
    }

    private func createConfetti() -> SKShapeNode {
        let size = CGFloat.random(in: 8...15)
        let confetti = SKShapeNode(rectOf: CGSize(width: size, height: size * 1.5))
        confetti.fillColor = [SKColor.red, SKColor.yellow, SKColor.green,
                              SKColor.blue, SKColor.orange, SKColor.purple].randomElement()!
        confetti.strokeColor = .clear
        confetti.position = CGPoint(x: CGFloat.random(in: 0...frame.width),
                                     y: frame.height + CGFloat.random(in: 50...200))
        confetti.zPosition = Constants.ZPosition.ui - 5
        confetti.zRotation = CGFloat.random(in: 0...(.pi * 2))

        // Falling animation
        let fallDuration = Double.random(in: 3...6)
        let fall = SKAction.moveTo(y: -50, duration: fallDuration)
        let rotate = SKAction.rotate(byAngle: .pi * 4, duration: fallDuration)
        let sway = SKAction.sequence([
            SKAction.moveBy(x: 30, y: 0, duration: 0.5),
            SKAction.moveBy(x: -30, y: 0, duration: 0.5)
        ])
        let swayRepeat = SKAction.repeat(sway, count: Int(fallDuration))

        let group = SKAction.group([fall, rotate, swayRepeat])
        let reset = SKAction.run { [weak confetti, weak self] in
            confetti?.position = CGPoint(x: CGFloat.random(in: 0...(self?.frame.width ?? 400)),
                                          y: (self?.frame.height ?? 800) + 50)
        }
        let sequence = SKAction.sequence([group, reset])
        confetti.run(SKAction.repeatForever(sequence))

        return confetti
    }

    private func setupContent() {
        // Victory title
        let titleLabel = SKLabelNode(text: "🎉 GEWONNEN! 🎉")
        titleLabel.fontName = "AvenirNext-Heavy"
        titleLabel.fontSize = 46
        titleLabel.fontColor = .yellow
        titleLabel.position = CGPoint(x: frame.midX, y: frame.height * 0.82)
        titleLabel.zPosition = Constants.ZPosition.ui
        addChild(titleLabel)

        // Animate title
        let titlePulse = SKAction.sequence([
            SKAction.scale(to: 1.1, duration: 0.3),
            SKAction.scale(to: 1.0, duration: 0.3)
        ])
        titleLabel.run(SKAction.repeatForever(titlePulse))

        // Subtitle
        let subtitleLabel = SKLabelNode(text: "Du hast alle Ziele erreicht!")
        subtitleLabel.fontName = "AvenirNext-Medium"
        subtitleLabel.fontSize = 20
        subtitleLabel.fontColor = .white
        subtitleLabel.position = CGPoint(x: frame.midX, y: frame.height * 0.74)
        subtitleLabel.zPosition = Constants.ZPosition.ui
        addChild(subtitleLabel)

        // Score display
        let scoreLabel = SKLabelNode(text: "Endpunktzahl: \(finalScore)")
        scoreLabel.fontName = "AvenirNext-Bold"
        scoreLabel.fontSize = 36
        scoreLabel.fontColor = SKColor(red: 1.0, green: 0.85, blue: 0.0, alpha: 1.0)
        scoreLabel.position = CGPoint(x: frame.midX, y: frame.height * 0.62)
        scoreLabel.zPosition = Constants.ZPosition.ui
        addChild(scoreLabel)

        // Time bonus display
        let timeBonus = Int(timeRemaining) * 5
        if timeBonus > 0 {
            let timeBonusLabel = SKLabelNode(text: "⏱️ Zeitbonus: +\(timeBonus) (\(Int(timeRemaining))s übrig)")
            timeBonusLabel.fontName = "AvenirNext-Medium"
            timeBonusLabel.fontSize = 18
            timeBonusLabel.fontColor = SKColor.cyan
            timeBonusLabel.position = CGPoint(x: frame.midX, y: frame.height * 0.55)
            timeBonusLabel.zPosition = Constants.ZPosition.ui
            addChild(timeBonusLabel)
        }

        // Stats display
        let statsY = frame.height * 0.45

        let dogsLabel = SKLabelNode(text: "🐕 Hunde: \(dogsCollected)")
        dogsLabel.fontName = "AvenirNext-DemiBold"
        dogsLabel.fontSize = 22
        dogsLabel.fontColor = .white
        dogsLabel.position = CGPoint(x: frame.midX - 80, y: statsY)
        dogsLabel.zPosition = Constants.ZPosition.ui
        addChild(dogsLabel)

        let humansLabel = SKLabelNode(text: "👤 Menschen: \(humansCollected)")
        humansLabel.fontName = "AvenirNext-DemiBold"
        humansLabel.fontSize = 22
        humansLabel.fontColor = .white
        humansLabel.position = CGPoint(x: frame.midX + 80, y: statsY)
        humansLabel.zPosition = Constants.ZPosition.ui
        addChild(humansLabel)

        // Happy suitcase with collected items
        let happySuitcase = PlayerNode()
        happySuitcase.position = CGPoint(x: frame.midX, y: frame.height * 0.28)
        addChild(happySuitcase)

        // Happy face
        let happyFace = SKLabelNode(text: "😄")
        happyFace.fontSize = 30
        happyFace.position = CGPoint(x: frame.midX, y: frame.height * 0.28 + 20)
        happyFace.zPosition = Constants.ZPosition.ui
        addChild(happyFace)

        // Add small dogs and humans around suitcase
        let collectibles = [
            ("🐕", CGPoint(x: -50, y: 0)),
            ("🐕", CGPoint(x: 50, y: 0)),
            ("👤", CGPoint(x: -35, y: 30)),
            ("👤", CGPoint(x: 35, y: 30))
        ]

        for (emoji, offset) in collectibles {
            let label = SKLabelNode(text: emoji)
            label.fontSize = 24
            label.position = CGPoint(x: frame.midX + offset.x,
                                      y: frame.height * 0.28 + offset.y)
            label.zPosition = Constants.ZPosition.ui
            addChild(label)

            // Bounce animation
            let bounce = SKAction.sequence([
                SKAction.moveBy(x: 0, y: 5, duration: 0.3),
                SKAction.moveBy(x: 0, y: -5, duration: 0.3)
            ])
            label.run(SKAction.repeatForever(bounce))
        }

        // High score check
        if ScoreManager.shared.isNewHighScore(finalScore) {
            let highScoreLabel = SKLabelNode(text: "🏆 NEUER HIGHSCORE! 🏆")
            highScoreLabel.fontName = "AvenirNext-Heavy"
            highScoreLabel.fontSize = 26
            highScoreLabel.fontColor = SKColor.yellow
            highScoreLabel.position = CGPoint(x: frame.midX, y: frame.height * 0.68)
            highScoreLabel.zPosition = Constants.ZPosition.ui
            addChild(highScoreLabel)

            let glow = SKAction.sequence([
                SKAction.fadeAlpha(to: 0.6, duration: 0.4),
                SKAction.fadeAlpha(to: 1.0, duration: 0.4)
            ])
            highScoreLabel.run(SKAction.repeatForever(glow))
        }
    }

    private func setupButtons() {
        // Play Again button
        playAgainButton = createButton(text: "🎮 Nochmal spielen",
                                        color: SKColor(red: 0.2, green: 0.7, blue: 0.3, alpha: 1.0))
        playAgainButton.position = CGPoint(x: frame.midX, y: frame.height * 0.13)
        playAgainButton.name = "playAgainButton"
        addChild(playAgainButton)

        // Menu button
        menuButton = createButton(text: "🏠 Hauptmenü",
                                   color: SKColor(red: 0.3, green: 0.3, blue: 0.6, alpha: 1.0))
        menuButton.position = CGPoint(x: frame.midX, y: frame.height * 0.06)
        menuButton.name = "menuButton"
        addChild(menuButton)
    }

    private func createButton(text: String, color: SKColor) -> SKShapeNode {
        let buttonWidth: CGFloat = 220
        let buttonHeight: CGFloat = 50

        let button = SKShapeNode(rect: CGRect(x: -buttonWidth / 2, y: -buttonHeight / 2,
                                               width: buttonWidth, height: buttonHeight),
                                 cornerRadius: 12)
        button.fillColor = color
        button.strokeColor = .white.withAlphaComponent(0.5)
        button.lineWidth = 2
        button.zPosition = Constants.ZPosition.ui

        let label = SKLabelNode(text: text)
        label.fontName = "AvenirNext-Bold"
        label.fontSize = 20
        label.fontColor = .white
        label.verticalAlignmentMode = .center
        label.zPosition = 1
        button.addChild(label)

        return button
    }

    private func startCelebration() {
        // Screen flash
        let flash = SKShapeNode(rect: frame)
        flash.fillColor = .white
        flash.strokeColor = .clear
        flash.zPosition = Constants.ZPosition.ui + 100
        flash.alpha = 0.8
        addChild(flash)

        let fadeOut = SKAction.fadeOut(withDuration: 0.5)
        let remove = SKAction.removeFromParent()
        flash.run(SKAction.sequence([fadeOut, remove]))

        // Star burst effect
        for _ in 0..<12 {
            let star = SKLabelNode(text: "⭐")
            star.fontSize = CGFloat.random(in: 20...40)
            star.position = CGPoint(x: frame.midX, y: frame.height * 0.82)
            star.zPosition = Constants.ZPosition.ui - 1
            star.alpha = 0
            addChild(star)

            let angle = CGFloat.random(in: 0...(.pi * 2))
            let distance = CGFloat.random(in: 100...200)
            let endPoint = CGPoint(x: frame.midX + cos(angle) * distance,
                                    y: frame.height * 0.82 + sin(angle) * distance)

            let fadeIn = SKAction.fadeIn(withDuration: 0.2)
            let move = SKAction.move(to: endPoint, duration: 0.5)
            let fadeOutStar = SKAction.fadeOut(withDuration: 0.3)
            let removeStar = SKAction.removeFromParent()

            let group = SKAction.group([move, SKAction.sequence([fadeIn,
                                                                  SKAction.wait(forDuration: 0.2),
                                                                  fadeOutStar])])
            star.run(SKAction.sequence([SKAction.wait(forDuration: Double.random(in: 0...0.3)),
                                         group, removeStar]))
        }
    }

    // MARK: - Touch Handling
    override func touchesBegan(_ touches: Set<UITouch>, with event: UIEvent?) {
        guard let touch = touches.first else { return }
        let location = touch.location(in: self)

        if playAgainButton.contains(location) {
            playAgain()
        } else if menuButton.contains(location) {
            returnToMenu()
        }
    }

    // MARK: - Keyboard Handling (macOS)
    #if targetEnvironment(macCatalyst)
    override var canBecomeFirstResponder: Bool { true }

    override func pressesBegan(_ presses: Set<UIPress>, with event: UIPressesEvent?) {
        guard let key = presses.first?.key else {
            super.pressesBegan(presses, with: event)
            return
        }

        switch key.keyCode {
        case .keyboardSpacebar, .keyboardReturnOrEnter:
            playAgain()
        case .keyboardEscape:
            returnToMenu()
        default:
            super.pressesBegan(presses, with: event)
        }
    }
    #endif

    private func playAgain() {
        let pressDown = SKAction.scale(to: 0.9, duration: 0.1)
        let pressUp = SKAction.scale(to: 1.0, duration: 0.1)

        playAgainButton.run(SKAction.sequence([pressDown, pressUp])) { [weak self] in
            guard let self = self else { return }

            let gameScene = GameScene(size: self.size)
            gameScene.scaleMode = self.scaleMode

            let transition = SKTransition.fade(withDuration: 0.5)
            self.view?.presentScene(gameScene, transition: transition)
        }
    }

    private func returnToMenu() {
        let pressDown = SKAction.scale(to: 0.9, duration: 0.1)
        let pressUp = SKAction.scale(to: 1.0, duration: 0.1)

        menuButton.run(SKAction.sequence([pressDown, pressUp])) { [weak self] in
            guard let self = self else { return }

            let menuScene = MenuScene(size: self.size)
            menuScene.scaleMode = self.scaleMode

            let transition = SKTransition.fade(withDuration: 0.5)
            self.view?.presentScene(menuScene, transition: transition)
        }
    }
}
