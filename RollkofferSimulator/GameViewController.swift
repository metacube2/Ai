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
    }

    private func setupNotificationObservers() {
        NotificationCenter.default.addObserver(
            self,
            selector: #selector(handlePauseNotification),
            name: .pauseGame,
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

    override var supportedInterfaceOrientations: UIInterfaceOrientationMask {
        if UIDevice.current.userInterfaceIdiom == .phone {
            return .portrait
        } else {
            return .all
        }
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
