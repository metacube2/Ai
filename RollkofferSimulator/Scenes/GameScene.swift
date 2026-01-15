//
//  GameScene.swift
//  RollkofferSimulator
//
//  Created by Ingo K.
//

import SpriteKit

class GameScene: SKScene {

    // MARK: - Game Objects
    private var player: PlayerNode!
    private var gameState: GameState!

    // MARK: - Managers
    private var spawnManager: SpawnManager!
    private var collisionManager: CollisionManager!

    // MARK: - UI Elements
    private var livesLabel: SKLabelNode!
    private var scoreLabel: SKLabelNode!
    private var dogsLabel: SKLabelNode!
    private var humansLabel: SKLabelNode!
    private var timerLabel: SKLabelNode!
    private var pauseButton: SKShapeNode!
    private var pauseOverlay: SKNode?

    // MARK: - Touch Handling
    private var touchOffset: CGPoint = .zero
    private var isDragging: Bool = false

    // MARK: - Timing
    private var lastUpdateTime: TimeInterval = 0

    // MARK: - Background Scrolling
    private var floorTiles: [SKShapeNode] = []
    private let tileSize: CGFloat = 80

    // MARK: - Scene Lifecycle
    override func didMove(to view: SKView) {
        setupGame()
    }

    // MARK: - Setup
    private func setupGame() {
        // Initialize game state
        gameState = GameState()
        gameState.reset()

        // Setup physics
        physicsWorld.gravity = .zero
        collisionManager = CollisionManager()
        collisionManager.delegate = self
        collisionManager.setScene(self)
        physicsWorld.contactDelegate = collisionManager

        setupBackground()
        setupPlayer()
        setupUI()
        setupSpawnManager()

        spawnManager.startSpawning()
    }

    private func setupBackground() {
        backgroundColor = Constants.Colors.backgroundColor

        // Create scrolling floor tiles
        let rows = Int(frame.height / tileSize) + 3
        let cols = Int(frame.width / tileSize) + 1

        for row in 0..<rows {
            for col in 0..<cols {
                let tile = SKShapeNode(rect: CGRect(x: 0, y: 0,
                                                     width: tileSize - 2, height: tileSize - 2))
                let isEven = (row + col) % 2 == 0
                tile.fillColor = isEven ?
                    SKColor(white: 0.78, alpha: 1.0) : SKColor(white: 0.72, alpha: 1.0)
                tile.strokeColor = SKColor(white: 0.65, alpha: 0.5)
                tile.lineWidth = 1
                tile.position = CGPoint(x: CGFloat(col) * tileSize,
                                         y: CGFloat(row) * tileSize)
                tile.zPosition = Constants.ZPosition.floor
                tile.name = "floorTile"
                addChild(tile)
                floorTiles.append(tile)
            }
        }

        // Add airport markings
        addAirportMarkings()
    }

    private func addAirportMarkings() {
        // Center line
        let lineWidth: CGFloat = 4
        let dashLength: CGFloat = 30
        let gapLength: CGFloat = 20

        for y in stride(from: CGFloat(0), to: frame.height, by: dashLength + gapLength) {
            let dash = SKShapeNode(rect: CGRect(x: frame.midX - lineWidth / 2, y: y,
                                                 width: lineWidth, height: dashLength))
            dash.fillColor = SKColor.yellow.withAlphaComponent(0.6)
            dash.strokeColor = .clear
            dash.zPosition = Constants.ZPosition.floor + 0.5
            dash.name = "floorMarking"
            addChild(dash)
        }
    }

    private func setupPlayer() {
        player = PlayerNode()
        player.position = CGPoint(x: frame.midX, y: frame.height * 0.15)
        addChild(player)
    }

    private func setupUI() {
        let uiY = frame.height - 50
        let uiY2 = frame.height - 80

        // Lives
        livesLabel = createUILabel(text: gameState.formattedLives, fontSize: 24)
        livesLabel.position = CGPoint(x: 60, y: uiY)
        livesLabel.horizontalAlignmentMode = .left
        addChild(livesLabel)

        // Score
        scoreLabel = createUILabel(text: gameState.formattedScore, fontSize: 20)
        scoreLabel.position = CGPoint(x: frame.midX, y: uiY)
        addChild(scoreLabel)

        // Dogs counter
        dogsLabel = createUILabel(text: gameState.formattedDogs, fontSize: 18)
        dogsLabel.position = CGPoint(x: frame.width - 120, y: uiY)
        addChild(dogsLabel)

        // Humans counter
        humansLabel = createUILabel(text: gameState.formattedHumans, fontSize: 18)
        humansLabel.position = CGPoint(x: frame.width - 50, y: uiY)
        addChild(humansLabel)

        // Timer
        timerLabel = createUILabel(text: "⏱️ \(gameState.formattedTime)", fontSize: 22)
        timerLabel.position = CGPoint(x: frame.midX, y: uiY2)
        addChild(timerLabel)

        // Pause button
        setupPauseButton()

        // UI background bar
        let uiBar = SKShapeNode(rect: CGRect(x: 0, y: frame.height - 100,
                                              width: frame.width, height: 100))
        uiBar.fillColor = SKColor.white.withAlphaComponent(0.9)
        uiBar.strokeColor = SKColor.gray.withAlphaComponent(0.5)
        uiBar.lineWidth = 1
        uiBar.zPosition = Constants.ZPosition.ui - 1
        addChild(uiBar)
    }

    private func createUILabel(text: String, fontSize: CGFloat) -> SKLabelNode {
        let label = SKLabelNode(text: text)
        label.fontName = "AvenirNext-Bold"
        label.fontSize = fontSize
        label.fontColor = .darkGray
        label.zPosition = Constants.ZPosition.ui
        label.verticalAlignmentMode = .center
        return label
    }

    private func setupPauseButton() {
        let buttonSize: CGFloat = 36
        pauseButton = SKShapeNode(rect: CGRect(x: -buttonSize / 2, y: -buttonSize / 2,
                                                width: buttonSize, height: buttonSize),
                                  cornerRadius: 8)
        pauseButton.fillColor = SKColor(white: 0.9, alpha: 1.0)
        pauseButton.strokeColor = SKColor.gray
        pauseButton.lineWidth = 2
        pauseButton.position = CGPoint(x: 40, y: frame.height - 75)
        pauseButton.zPosition = Constants.ZPosition.ui
        pauseButton.name = "pauseButton"
        addChild(pauseButton)

        // Pause icon (two vertical bars)
        let barWidth: CGFloat = 4
        let barHeight: CGFloat = 16
        let barGap: CGFloat = 5

        let leftBar = SKShapeNode(rect: CGRect(x: -barGap - barWidth / 2, y: -barHeight / 2,
                                                width: barWidth, height: barHeight))
        leftBar.fillColor = .darkGray
        leftBar.strokeColor = .clear
        pauseButton.addChild(leftBar)

        let rightBar = SKShapeNode(rect: CGRect(x: barGap - barWidth / 2, y: -barHeight / 2,
                                                 width: barWidth, height: barHeight))
        rightBar.fillColor = .darkGray
        rightBar.strokeColor = .clear
        pauseButton.addChild(rightBar)
    }

    private func setupSpawnManager() {
        spawnManager = SpawnManager(scene: self)
    }

    // MARK: - Update Loop
    override func update(_ currentTime: TimeInterval) {
        guard gameState.currentState == .playing else { return }

        // Calculate delta time
        let deltaTime = lastUpdateTime > 0 ? currentTime - lastUpdateTime : 0
        lastUpdateTime = currentTime

        // Update game time
        gameState.updateTime(delta: deltaTime)
        updateUI()

        // Update spawn manager
        spawnManager.update(deltaTime: deltaTime)

        // Update floor scrolling
        updateFloorScrolling(deltaTime: deltaTime)

        // Check game end conditions
        checkGameEndConditions()
    }

    private func updateFloorScrolling(deltaTime: TimeInterval) {
        let scrollAmount = Constants.scrollSpeed * CGFloat(deltaTime)

        for tile in floorTiles {
            tile.position.y -= scrollAmount

            // Reset tile position when it scrolls off screen
            if tile.position.y < -tileSize {
                tile.position.y += CGFloat(floorTiles.count / (Int(frame.width / tileSize) + 1)) * tileSize
            }
        }

        // Also scroll floor markings
        enumerateChildNodes(withName: "floorMarking") { node, _ in
            node.position.y -= scrollAmount
            if node.position.y < -30 {
                node.position.y += self.frame.height + 50
            }
        }
    }

    private func updateUI() {
        livesLabel.text = gameState.formattedLives
        scoreLabel.text = gameState.formattedScore
        dogsLabel.text = gameState.formattedDogs
        humansLabel.text = gameState.formattedHumans
        timerLabel.text = "⏱️ \(gameState.formattedTime)"

        // Flash timer when low
        if gameState.timeRemaining <= 10 {
            timerLabel.fontColor = .red

            if timerLabel.action(forKey: "flash") == nil {
                let flash = SKAction.sequence([
                    SKAction.scale(to: 1.2, duration: 0.25),
                    SKAction.scale(to: 1.0, duration: 0.25)
                ])
                timerLabel.run(SKAction.repeatForever(flash), withKey: "flash")
            }
        }
    }

    private func checkGameEndConditions() {
        if gameState.hasWon {
            handleVictory()
        } else if gameState.hasLost {
            handleGameOver()
        }
    }

    // MARK: - Touch Handling
    override func touchesBegan(_ touches: Set<UITouch>, with event: UIEvent?) {
        guard let touch = touches.first else { return }
        let location = touch.location(in: self)

        // Check pause button
        if pauseButton.contains(location) {
            togglePause()
            return
        }

        // Check if touching player for dragging
        if player.contains(location) {
            isDragging = true
            touchOffset = CGPoint(x: player.position.x - location.x,
                                  y: player.position.y - location.y)
        } else {
            // Move player to touch location
            isDragging = true
            touchOffset = .zero
            player.position = location
            player.constrainToScreen(in: frame)
        }
    }

    override func touchesMoved(_ touches: Set<UITouch>, with event: UIEvent?) {
        guard isDragging, gameState.currentState == .playing,
              let touch = touches.first else { return }

        let location = touch.location(in: self)
        player.position = CGPoint(x: location.x + touchOffset.x,
                                  y: location.y + touchOffset.y)
        player.constrainToScreen(in: frame)
    }

    override func touchesEnded(_ touches: Set<UITouch>, with event: UIEvent?) {
        isDragging = false
    }

    override func touchesCancelled(_ touches: Set<UITouch>, with event: UIEvent?) {
        isDragging = false
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
        case .keyboardEscape:
            togglePause()
        case .keyboardSpacebar:
            if gameState.currentState == .paused {
                resumeGame()
            }
        default:
            super.pressesBegan(presses, with: event)
        }
    }
    #endif

    // MARK: - Pause Handling
    private func togglePause() {
        if gameState.currentState == .playing {
            pauseGame()
        } else if gameState.currentState == .paused {
            resumeGame()
        }
    }

    private func pauseGame() {
        gameState.setState(.paused)
        spawnManager.stopSpawning()
        isPaused = true

        showPauseOverlay()
    }

    private func resumeGame() {
        hidePauseOverlay()

        gameState.setState(.playing)
        spawnManager.startSpawning()
        isPaused = false
        lastUpdateTime = 0
    }

    private func showPauseOverlay() {
        pauseOverlay = SKNode()
        pauseOverlay?.zPosition = Constants.ZPosition.ui + 10

        // Dimmed background
        let dim = SKShapeNode(rect: frame)
        dim.fillColor = SKColor.black.withAlphaComponent(0.6)
        dim.strokeColor = .clear
        pauseOverlay?.addChild(dim)

        // Pause text
        let pauseText = SKLabelNode(text: "⏸️ PAUSIERT")
        pauseText.fontName = "AvenirNext-Heavy"
        pauseText.fontSize = 40
        pauseText.fontColor = .white
        pauseText.position = CGPoint(x: frame.midX, y: frame.midY + 40)
        pauseOverlay?.addChild(pauseText)

        // Resume button
        let resumeButton = createButton(text: "▶️ Weiter", at: CGPoint(x: frame.midX, y: frame.midY - 20))
        resumeButton.name = "resumeButton"
        pauseOverlay?.addChild(resumeButton)

        // Menu button
        let menuButton = createButton(text: "🏠 Menü", at: CGPoint(x: frame.midX, y: frame.midY - 80))
        menuButton.name = "menuButton"
        pauseOverlay?.addChild(menuButton)

        addChild(pauseOverlay!)
    }

    private func createButton(text: String, at position: CGPoint) -> SKNode {
        let container = SKNode()
        container.position = position

        let bg = SKShapeNode(rect: CGRect(x: -80, y: -25, width: 160, height: 50),
                             cornerRadius: 10)
        bg.fillColor = SKColor(white: 0.2, alpha: 0.9)
        bg.strokeColor = .white
        bg.lineWidth = 2
        container.addChild(bg)

        let label = SKLabelNode(text: text)
        label.fontName = "AvenirNext-Bold"
        label.fontSize = 22
        label.fontColor = .white
        label.verticalAlignmentMode = .center
        container.addChild(label)

        return container
    }

    private func hidePauseOverlay() {
        pauseOverlay?.removeFromParent()
        pauseOverlay = nil
    }

    // MARK: - Handle pause overlay touches (override to handle when paused)
    func handlePauseOverlayTouch(at location: CGPoint) {
        guard let overlay = pauseOverlay else { return }

        if let resumeButton = overlay.childNode(withName: "resumeButton"),
           resumeButton.contains(location) {
            resumeGame()
        } else if let menuButton = overlay.childNode(withName: "menuButton"),
                  menuButton.contains(location) {
            returnToMenu()
        }
    }

    // Override touchesBegan to handle pause overlay
    func handleTouchInPausedState(_ touches: Set<UITouch>) {
        guard let touch = touches.first else { return }
        let location = touch.location(in: self)
        handlePauseOverlayTouch(at: location)
    }

    // MARK: - Game End Handling
    private func handleDamage() {
        guard !gameState.isInvincible else { return }

        if gameState.loseLife() {
            // Start invincibility
            gameState.isInvincible = true
            player.startBlinking()

            // End invincibility after duration
            let wait = SKAction.wait(forDuration: Constants.invincibilityDuration)
            run(wait) { [weak self] in
                self?.gameState.isInvincible = false
                self?.player.stopBlinking()
            }
        }
    }

    private func handleVictory() {
        gameState.setState(.victory)
        spawnManager.stopSpawning()

        // Record score
        ScoreManager.shared.recordGameEnd(
            score: gameState.score,
            dogsCollected: gameState.dogsCollected,
            humansCollected: gameState.humansCollected,
            didWin: true
        )

        // Transition to victory scene
        let victoryScene = VictoryScene(size: size)
        victoryScene.scaleMode = scaleMode
        victoryScene.finalScore = gameState.score
        victoryScene.dogsCollected = gameState.dogsCollected
        victoryScene.humansCollected = gameState.humansCollected
        victoryScene.timeRemaining = gameState.timeRemaining

        let transition = SKTransition.fade(withDuration: 0.5)
        view?.presentScene(victoryScene, transition: transition)
    }

    private func handleGameOver() {
        gameState.setState(.gameOver)
        spawnManager.stopSpawning()

        // Record score
        ScoreManager.shared.recordGameEnd(
            score: gameState.score,
            dogsCollected: gameState.dogsCollected,
            humansCollected: gameState.humansCollected,
            didWin: false
        )

        // Transition to game over scene
        let gameOverScene = GameOverScene(size: size)
        gameOverScene.scaleMode = scaleMode
        gameOverScene.finalScore = gameState.score
        gameOverScene.dogsCollected = gameState.dogsCollected
        gameOverScene.humansCollected = gameState.humansCollected
        gameOverScene.isNewHighScore = ScoreManager.shared.isNewHighScore(gameState.score)

        let transition = SKTransition.fade(withDuration: 0.5)
        view?.presentScene(gameOverScene, transition: transition)
    }

    private func returnToMenu() {
        let menuScene = MenuScene(size: size)
        menuScene.scaleMode = scaleMode

        let transition = SKTransition.fade(withDuration: 0.5)
        view?.presentScene(menuScene, transition: transition)
    }
}

// MARK: - CollisionManagerDelegate
extension GameScene: CollisionManagerDelegate {
    func didCollectGoodDog(points: Int) {
        gameState.addPoints(points)
        gameState.collectDog()

        // Show collection effect
        showPointsEffect(points: points, at: player.position)
    }

    func didCollectGreenHuman(points: Int) {
        gameState.addPoints(points)
        gameState.collectHuman()

        // Show collection effect
        showPointsEffect(points: points, at: player.position)
    }

    func didHitHarmfulEntity() {
        handleDamage()
    }

    private func showPointsEffect(points: Int, at position: CGPoint) {
        let label = SKLabelNode(text: "+\(points)")
        label.fontName = "AvenirNext-Bold"
        label.fontSize = 24
        label.fontColor = .green
        label.position = CGPoint(x: position.x, y: position.y + 50)
        label.zPosition = Constants.ZPosition.ui

        addChild(label)

        let moveUp = SKAction.moveBy(x: 0, y: 40, duration: 0.5)
        let fadeOut = SKAction.fadeOut(withDuration: 0.5)
        let group = SKAction.group([moveUp, fadeOut])
        let remove = SKAction.removeFromParent()
        label.run(SKAction.sequence([group, remove]))
    }
}
