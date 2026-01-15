//
//  GameViewController.swift
//  RollkofferSimulator
//
//  Created by Ingo K.
//

import UIKit
import SpriteKit
import GameplayKit

class GameViewController: UIViewController {

    override func viewDidLoad() {
        super.viewDidLoad()

        // Configure the view
        guard let skView = self.view as? SKView else {
            fatalError("View is not an SKView")
        }

        // Create and configure the initial scene
        let scene = MenuScene(size: skView.bounds.size)
        scene.scaleMode = .aspectFill

        // Configure view options
        skView.ignoresSiblingOrder = true

        #if DEBUG
        skView.showsFPS = true
        skView.showsNodeCount = true
        #endif

        // Present the scene
        skView.presentScene(scene)

        // Setup notification observers
        setupNotificationObservers()

        #if targetEnvironment(macCatalyst)
        setupMacCatalyst()
        #endif
    }

    private func setupNotificationObservers() {
        NotificationCenter.default.addObserver(
            self,
            selector: #selector(handlePauseNotification),
            name: .pauseGame,
            object: nil
        )

        NotificationCenter.default.addObserver(
            self,
            selector: #selector(handleRestartNotification),
            name: .restartGame,
            object: nil
        )
    }

    @objc private func handlePauseNotification() {
        guard let skView = self.view as? SKView,
              let gameScene = skView.scene as? GameScene else {
            return
        }

        // The GameScene should handle pausing internally
        // This is just a notification that the app is going to background
    }

    @objc private func handleRestartNotification() {
        guard let skView = self.view as? SKView else { return }

        let menuScene = MenuScene(size: skView.bounds.size)
        menuScene.scaleMode = .aspectFill

        let transition = SKTransition.fade(withDuration: 0.5)
        skView.presentScene(menuScene, transition: transition)
    }

    #if targetEnvironment(macCatalyst)
    private func setupMacCatalyst() {
        // Configure window appearance for macOS
        if let windowScene = view.window?.windowScene {
            windowScene.title = "Rollkoffer Simulator"

            // Set window style
            if let titlebar = windowScene.titlebar {
                titlebar.titleVisibility = .visible
                titlebar.toolbarStyle = .unified
            }
        }
    }

    // Enable keyboard input
    override var canBecomeFirstResponder: Bool {
        return true
    }
    #endif

    override var supportedInterfaceOrientations: UIInterfaceOrientationMask {
        #if targetEnvironment(macCatalyst)
        return .all
        #else
        if UIDevice.current.userInterfaceIdiom == .phone {
            return .portrait
        } else {
            return .all
        }
        #endif
    }

    override var prefersStatusBarHidden: Bool {
        return true
    }

    override var preferredScreenEdgesDeferringSystemGestures: UIRectEdge {
        return .all
    }

    deinit {
        NotificationCenter.default.removeObserver(self)
    }
}
