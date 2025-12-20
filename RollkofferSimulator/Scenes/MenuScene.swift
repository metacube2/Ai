//
//  MenuScene.swift
//  RollkofferSimulator
//
//  Created by Ingo K.
//

import SpriteKit

class MenuScene: SKScene {

    // MARK: - Properties
    private var startButton: SKShapeNode!
    private var titleLabel: SKLabelNode!
    private var subtitleLabel: SKLabelNode!
    private var highScoreLabel: SKLabelNode!
    private var creditsLabel: SKLabelNode!

    // MARK: - Decorative elements
    private var decorativeSuitcase: PlayerNode!
    private var decorativeDogs: [DogNode] = []
    private var decorativeHumans: [HumanNode] = []

    // MARK: - Scene Lifecycle
    override func didMove(to view: SKView) {
        setupBackground()
        setupTitle()
        setupStartButton()
        setupHighScore()
        setupDecorations()
        setupCredits()
        startAnimations()
    }

    // MARK: - Setup
    private func setupBackground() {
        backgroundColor = Constants.Colors.backgroundColor

        // Create floor pattern
        let floorHeight = frame.height * 0.6
        let floor = SKShapeNode(rect: CGRect(x: 0, y: 0,
                                              width: frame.width, height: floorHeight))
        floor.fillColor = Constants.Colors.floorColor
        floor.strokeColor = .clear
        floor.zPosition = Constants.ZPosition.floor
        addChild(floor)

        // Add floor tiles pattern
        let tileSize: CGFloat = 80
        for x in stride(from: CGFloat(0), to: frame.width, by: tileSize) {
            for y in stride(from: CGFloat(0), to: floorHeight, by: tileSize) {
                let tile = SKShapeNode(rect: CGRect(x: x, y: y,
                                                     width: tileSize - 2, height: tileSize - 2))
                tile.fillColor = (Int((x + y) / tileSize) % 2 == 0) ?
                    SKColor(white: 0.78, alpha: 1.0) : SKColor(white: 0.72, alpha: 1.0)
                tile.strokeColor = SKColor(white: 0.65, alpha: 0.5)
                tile.lineWidth = 1
                tile.zPosition = Constants.ZPosition.floor + 0.1
                addChild(tile)
            }
        }
    }

    private func setupTitle() {
        // Main title
        titleLabel = SKLabelNode(text: "🧳 Rollkoffer Simulator 🧳")
        titleLabel.fontName = "AvenirNext-Heavy"
        titleLabel.fontSize = 36
        titleLabel.fontColor = SKColor(red: 0.3, green: 0.2, blue: 0.5, alpha: 1.0)
        titleLabel.position = CGPoint(x: frame.midX, y: frame.height * 0.85)
        titleLabel.zPosition = Constants.ZPosition.ui
        addChild(titleLabel)

        // Subtitle
        subtitleLabel = SKLabelNode(text: "Sammle Hunde & Menschen am Flughafen!")
        subtitleLabel.fontName = "AvenirNext-Medium"
        subtitleLabel.fontSize = 18
        subtitleLabel.fontColor = SKColor.darkGray
        subtitleLabel.position = CGPoint(x: frame.midX, y: frame.height * 0.78)
        subtitleLabel.zPosition = Constants.ZPosition.ui
        addChild(subtitleLabel)

        // Goal info
        let goalLabel = SKLabelNode(text: "Ziel: 🐕 10 Hunde + 👤 5 Grüne Menschen")
        goalLabel.fontName = "AvenirNext-Medium"
        goalLabel.fontSize = 16
        goalLabel.fontColor = SKColor.gray
        goalLabel.position = CGPoint(x: frame.midX, y: frame.height * 0.72)
        goalLabel.zPosition = Constants.ZPosition.ui
        addChild(goalLabel)
    }

    private func setupStartButton() {
        // Button background
        let buttonWidth: CGFloat = 200
        let buttonHeight: CGFloat = 60

        startButton = SKShapeNode(rect: CGRect(x: -buttonWidth / 2, y: -buttonHeight / 2,
                                                width: buttonWidth, height: buttonHeight),
                                  cornerRadius: 15)
        startButton.fillColor = SKColor(red: 0.2, green: 0.7, blue: 0.3, alpha: 1.0)
        startButton.strokeColor = SKColor(red: 0.15, green: 0.5, blue: 0.2, alpha: 1.0)
        startButton.lineWidth = 3
        startButton.position = CGPoint(x: frame.midX, y: frame.height * 0.5)
        startButton.zPosition = Constants.ZPosition.ui
        startButton.name = "startButton"
        addChild(startButton)

        // Button label
        let buttonLabel = SKLabelNode(text: "▶ START")
        buttonLabel.fontName = "AvenirNext-Bold"
        buttonLabel.fontSize = 28
        buttonLabel.fontColor = .white
        buttonLabel.verticalAlignmentMode = .center
        buttonLabel.position = .zero
        buttonLabel.zPosition = 1
        startButton.addChild(buttonLabel)

        // Button pulse animation
        let scaleUp = SKAction.scale(to: 1.05, duration: 0.8)
        let scaleDown = SKAction.scale(to: 1.0, duration: 0.8)
        let pulse = SKAction.sequence([scaleUp, scaleDown])
        startButton.run(SKAction.repeatForever(pulse))
    }

    private func setupHighScore() {
        let highScore = ScoreManager.shared.highScore

        highScoreLabel = SKLabelNode(text: "🏆 High Score: \(highScore)")
        highScoreLabel.fontName = "AvenirNext-DemiBold"
        highScoreLabel.fontSize = 20
        highScoreLabel.fontColor = SKColor(red: 0.8, green: 0.6, blue: 0.1, alpha: 1.0)
        highScoreLabel.position = CGPoint(x: frame.midX, y: frame.height * 0.38)
        highScoreLabel.zPosition = Constants.ZPosition.ui
        addChild(highScoreLabel)

        // Statistics label
        let victories = ScoreManager.shared.victories
        let gamesPlayed = ScoreManager.shared.gamesPlayed
        let statsText = "Siege: \(victories) | Spiele: \(gamesPlayed)"

        let statsLabel = SKLabelNode(text: statsText)
        statsLabel.fontName = "AvenirNext-Regular"
        statsLabel.fontSize = 14
        statsLabel.fontColor = SKColor.gray
        statsLabel.position = CGPoint(x: frame.midX, y: frame.height * 0.33)
        statsLabel.zPosition = Constants.ZPosition.ui
        addChild(statsLabel)
    }

    private func setupDecorations() {
        // Decorative suitcase
        decorativeSuitcase = PlayerNode()
        decorativeSuitcase.position = CGPoint(x: frame.midX, y: frame.height * 0.2)
        decorativeSuitcase.zPosition = Constants.ZPosition.entities
        addChild(decorativeSuitcase)

        // Add some decorative dogs
        let dogTypes: [DogType] = [.smallGood, .bigGood, .bad]
        let dogPositions: [CGPoint] = [
            CGPoint(x: frame.width * 0.15, y: frame.height * 0.25),
            CGPoint(x: frame.width * 0.85, y: frame.height * 0.22),
            CGPoint(x: frame.width * 0.25, y: frame.height * 0.12)
        ]

        for (index, type) in dogTypes.enumerated() {
            let dog = DogNode(type: type)
            dog.position = dogPositions[index]
            dog.zPosition = Constants.ZPosition.entities
            addChild(dog)
            decorativeDogs.append(dog)
        }

        // Add decorative humans
        let humanTypes: [HumanType] = [.green, .gray]
        let humanPositions: [CGPoint] = [
            CGPoint(x: frame.width * 0.75, y: frame.height * 0.15),
            CGPoint(x: frame.width * 0.6, y: frame.height * 0.1)
        ]

        for (index, type) in humanTypes.enumerated() {
            let human = HumanNode(type: type)
            human.position = humanPositions[index]
            human.zPosition = Constants.ZPosition.entities
            addChild(human)
            decorativeHumans.append(human)
        }
    }

    private func setupCredits() {
        creditsLabel = SKLabelNode(text: "Created by Ingo K.")
        creditsLabel.fontName = "AvenirNext-Italic"
        creditsLabel.fontSize = 14
        creditsLabel.fontColor = SKColor.gray
        creditsLabel.position = CGPoint(x: frame.midX, y: 30)
        creditsLabel.zPosition = Constants.ZPosition.ui
        addChild(creditsLabel)
    }

    private func startAnimations() {
        // Animate decorative suitcase
        let moveLeft = SKAction.moveBy(x: -20, y: 0, duration: 1.5)
        let moveRight = SKAction.moveBy(x: 40, y: 0, duration: 3.0)
        let moveBack = SKAction.moveBy(x: -20, y: 0, duration: 1.5)
        let suitcaseSequence = SKAction.sequence([moveLeft, moveRight, moveBack])
        decorativeSuitcase.run(SKAction.repeatForever(suitcaseSequence))

        // Animate decorative entities
        for (index, dog) in decorativeDogs.enumerated() {
            let delay = Double(index) * 0.3
            let bounce = SKAction.sequence([
                SKAction.wait(forDuration: delay),
                SKAction.moveBy(x: 0, y: 10, duration: 0.4),
                SKAction.moveBy(x: 0, y: -10, duration: 0.4)
            ])
            dog.run(SKAction.repeatForever(bounce))
        }

        for (index, human) in decorativeHumans.enumerated() {
            let delay = Double(index) * 0.4 + 0.2
            let sway = SKAction.sequence([
                SKAction.wait(forDuration: delay),
                SKAction.rotate(byAngle: 0.05, duration: 0.5),
                SKAction.rotate(byAngle: -0.1, duration: 1.0),
                SKAction.rotate(byAngle: 0.05, duration: 0.5)
            ])
            human.run(SKAction.repeatForever(sway))
        }

        // Title animation
        let titleScale = SKAction.sequence([
            SKAction.scale(to: 1.02, duration: 2.0),
            SKAction.scale(to: 1.0, duration: 2.0)
        ])
        titleLabel.run(SKAction.repeatForever(titleScale))
    }

    // MARK: - Touch Handling
    override func touchesBegan(_ touches: Set<UITouch>, with event: UIEvent?) {
        guard let touch = touches.first else { return }
        let location = touch.location(in: self)

        if startButton.contains(location) {
            startGame()
        }
    }

    private func startGame() {
        // Button press effect
        let pressDown = SKAction.scale(to: 0.9, duration: 0.1)
        let pressUp = SKAction.scale(to: 1.0, duration: 0.1)

        startButton.run(SKAction.sequence([pressDown, pressUp])) { [weak self] in
            self?.transitionToGame()
        }
    }

    private func transitionToGame() {
        let gameScene = GameScene(size: size)
        gameScene.scaleMode = scaleMode

        let transition = SKTransition.fade(withDuration: 0.5)
        view?.presentScene(gameScene, transition: transition)
    }
}
