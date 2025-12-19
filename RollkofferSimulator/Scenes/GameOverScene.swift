//
//  GameOverScene.swift
//  RollkofferSimulator
//
//  Created by Ingo K.
//

import SpriteKit

class GameOverScene: SKScene {

    // MARK: - Properties
    var finalScore: Int = 0
    var dogsCollected: Int = 0
    var humansCollected: Int = 0
    var isNewHighScore: Bool = false

    private var retryButton: SKShapeNode!
    private var menuButton: SKShapeNode!

    // MARK: - Scene Lifecycle
    override func didMove(to view: SKView) {
        setupBackground()
        setupContent()
        setupButtons()
        startAnimations()
    }

    // MARK: - Setup
    private func setupBackground() {
        backgroundColor = SKColor(red: 0.15, green: 0.1, blue: 0.1, alpha: 1.0)

        // Add dim pattern
        for i in 0..<20 {
            let x = CGFloat.random(in: 0...frame.width)
            let y = CGFloat.random(in: 0...frame.height)
            let size = CGFloat.random(in: 20...60)

            let shape = SKShapeNode(circleOfRadius: size)
            shape.position = CGPoint(x: x, y: y)
            shape.fillColor = SKColor.red.withAlphaComponent(0.05)
            shape.strokeColor = .clear
            shape.zPosition = 0.1
            addChild(shape)
        }
    }

    private func setupContent() {
        // Game Over title
        let titleLabel = SKLabelNode(text: "💔 GAME OVER 💔")
        titleLabel.fontName = "AvenirNext-Heavy"
        titleLabel.fontSize = 42
        titleLabel.fontColor = .red
        titleLabel.position = CGPoint(x: frame.midX, y: frame.height * 0.8)
        titleLabel.zPosition = Constants.ZPosition.ui
        addChild(titleLabel)

        // Subtitle based on reason
        let subtitleText: String
        if dogsCollected < Constants.targetDogs || humansCollected < Constants.targetHumans {
            subtitleText = "Zeit abgelaufen!"
        } else {
            subtitleText = "Keine Leben mehr!"
        }

        let subtitleLabel = SKLabelNode(text: subtitleText)
        subtitleLabel.fontName = "AvenirNext-Medium"
        subtitleLabel.fontSize = 22
        subtitleLabel.fontColor = SKColor(white: 0.8, alpha: 1.0)
        subtitleLabel.position = CGPoint(x: frame.midX, y: frame.height * 0.72)
        subtitleLabel.zPosition = Constants.ZPosition.ui
        addChild(subtitleLabel)

        // Score display
        let scoreLabel = SKLabelNode(text: "Punkte: \(finalScore)")
        scoreLabel.fontName = "AvenirNext-Bold"
        scoreLabel.fontSize = 32
        scoreLabel.fontColor = .white
        scoreLabel.position = CGPoint(x: frame.midX, y: frame.height * 0.58)
        scoreLabel.zPosition = Constants.ZPosition.ui
        addChild(scoreLabel)

        // New high score indicator
        if isNewHighScore {
            let highScoreLabel = SKLabelNode(text: "🏆 NEUER HIGHSCORE! 🏆")
            highScoreLabel.fontName = "AvenirNext-Heavy"
            highScoreLabel.fontSize = 24
            highScoreLabel.fontColor = SKColor.yellow
            highScoreLabel.position = CGPoint(x: frame.midX, y: frame.height * 0.65)
            highScoreLabel.zPosition = Constants.ZPosition.ui
            addChild(highScoreLabel)

            // Animate high score
            let scale = SKAction.sequence([
                SKAction.scale(to: 1.1, duration: 0.5),
                SKAction.scale(to: 1.0, duration: 0.5)
            ])
            highScoreLabel.run(SKAction.repeatForever(scale))
        }

        // Stats display
        let dogsText = "🐕 \(dogsCollected)/\(Constants.targetDogs)"
        let humansText = "👤 \(humansCollected)/\(Constants.targetHumans)"

        let statsLabel = SKLabelNode(text: "\(dogsText)  |  \(humansText)")
        statsLabel.fontName = "AvenirNext-Medium"
        statsLabel.fontSize = 24
        statsLabel.fontColor = SKColor(white: 0.7, alpha: 1.0)
        statsLabel.position = CGPoint(x: frame.midX, y: frame.height * 0.48)
        statsLabel.zPosition = Constants.ZPosition.ui
        addChild(statsLabel)

        // Progress indicators
        let dogsProgress = min(1.0, CGFloat(dogsCollected) / CGFloat(Constants.targetDogs))
        let humansProgress = min(1.0, CGFloat(humansCollected) / CGFloat(Constants.targetHumans))

        addProgressBar(at: CGPoint(x: frame.midX - 60, y: frame.height * 0.42),
                       progress: dogsProgress, color: .orange, label: "Hunde")
        addProgressBar(at: CGPoint(x: frame.midX + 60, y: frame.height * 0.42),
                       progress: humansProgress, color: .green, label: "Menschen")

        // Sad suitcase
        let sadSuitcase = PlayerNode()
        sadSuitcase.position = CGPoint(x: frame.midX, y: frame.height * 0.25)
        sadSuitcase.alpha = 0.6
        sadSuitcase.setScale(0.8)
        addChild(sadSuitcase)

        // Sad face on suitcase area
        let sadFace = SKLabelNode(text: "😢")
        sadFace.fontSize = 30
        sadFace.position = CGPoint(x: frame.midX, y: frame.height * 0.25 + 20)
        sadFace.zPosition = Constants.ZPosition.ui
        addChild(sadFace)
    }

    private func addProgressBar(at position: CGPoint, progress: CGFloat, color: SKColor, label: String) {
        let barWidth: CGFloat = 80
        let barHeight: CGFloat = 12

        // Background
        let bg = SKShapeNode(rect: CGRect(x: -barWidth / 2, y: 0, width: barWidth, height: barHeight),
                             cornerRadius: 6)
        bg.position = position
        bg.fillColor = SKColor(white: 0.3, alpha: 1.0)
        bg.strokeColor = .clear
        bg.zPosition = Constants.ZPosition.ui
        addChild(bg)

        // Progress fill
        let fillWidth = barWidth * progress
        if fillWidth > 0 {
            let fill = SKShapeNode(rect: CGRect(x: -barWidth / 2, y: 0, width: fillWidth, height: barHeight),
                                   cornerRadius: 6)
            fill.position = position
            fill.fillColor = progress >= 1.0 ? .green : color
            fill.strokeColor = .clear
            fill.zPosition = Constants.ZPosition.ui + 0.1
            addChild(fill)
        }
    }

    private func setupButtons() {
        // Retry button
        retryButton = createButton(text: "🔄 Nochmal", color: SKColor(red: 0.2, green: 0.6, blue: 0.2, alpha: 1.0))
        retryButton.position = CGPoint(x: frame.midX, y: frame.height * 0.15)
        retryButton.name = "retryButton"
        addChild(retryButton)

        // Menu button
        menuButton = createButton(text: "🏠 Menü", color: SKColor(red: 0.3, green: 0.3, blue: 0.5, alpha: 1.0))
        menuButton.position = CGPoint(x: frame.midX, y: frame.height * 0.08)
        menuButton.name = "menuButton"
        addChild(menuButton)
    }

    private func createButton(text: String, color: SKColor) -> SKShapeNode {
        let buttonWidth: CGFloat = 180
        let buttonHeight: CGFloat = 50

        let button = SKShapeNode(rect: CGRect(x: -buttonWidth / 2, y: -buttonHeight / 2,
                                               width: buttonWidth, height: buttonHeight),
                                 cornerRadius: 12)
        button.fillColor = color
        button.strokeColor = color.withAlphaComponent(0.5)
        button.lineWidth = 2
        button.zPosition = Constants.ZPosition.ui

        let label = SKLabelNode(text: text)
        label.fontName = "AvenirNext-Bold"
        label.fontSize = 22
        label.fontColor = .white
        label.verticalAlignmentMode = .center
        label.zPosition = 1
        button.addChild(label)

        return button
    }

    private func startAnimations() {
        // Fade in effect
        alpha = 0
        let fadeIn = SKAction.fadeIn(withDuration: 0.5)
        run(fadeIn)

        // Button pulse
        let pulse = SKAction.sequence([
            SKAction.scale(to: 1.05, duration: 0.8),
            SKAction.scale(to: 1.0, duration: 0.8)
        ])
        retryButton.run(SKAction.repeatForever(pulse))
    }

    // MARK: - Touch Handling
    override func touchesBegan(_ touches: Set<UITouch>, with event: UIEvent?) {
        guard let touch = touches.first else { return }
        let location = touch.location(in: self)

        if retryButton.contains(location) {
            retryGame()
        } else if menuButton.contains(location) {
            returnToMenu()
        }
    }

    private func retryGame() {
        let pressDown = SKAction.scale(to: 0.9, duration: 0.1)
        let pressUp = SKAction.scale(to: 1.0, duration: 0.1)

        retryButton.run(SKAction.sequence([pressDown, pressUp])) { [weak self] in
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
