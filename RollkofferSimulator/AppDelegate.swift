//
//  AppDelegate.swift
//  RollkofferSimulator
//
//  Created by Ingo K.
//

import UIKit

@main
class AppDelegate: UIResponder, UIApplicationDelegate {

    var window: UIWindow?

    func application(_ application: UIApplication,
                     didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey: Any]?) -> Bool {
        #if targetEnvironment(macCatalyst)
        // Configure for macOS
        configureMacOS()
        #endif
        return true
    }

    func applicationWillResignActive(_ application: UIApplication) {
        // Pause the game when app goes to background
        NotificationCenter.default.post(name: .pauseGame, object: nil)
    }

    func applicationDidEnterBackground(_ application: UIApplication) {
        // Save game state if needed
    }

    func applicationWillEnterForeground(_ application: UIApplication) {
        // Restore game state if needed
    }

    func applicationDidBecomeActive(_ application: UIApplication) {
        // Resume game if needed
    }

    #if targetEnvironment(macCatalyst)
    // MARK: - macOS Configuration
    private func configureMacOS() {
        // Set minimum window size for macOS
        UIApplication.shared.connectedScenes.compactMap { $0 as? UIWindowScene }.forEach { windowScene in
            windowScene.sizeRestrictions?.minimumSize = CGSize(width: 400, height: 600)
            windowScene.sizeRestrictions?.maximumSize = CGSize(width: 600, height: 900)
        }
    }

    override func buildMenu(with builder: UIMenuBuilder) {
        super.buildMenu(with: builder)

        // Remove unnecessary menus for a game
        builder.remove(menu: .format)
        builder.remove(menu: .edit)

        // Add Game menu
        let pauseCommand = UIKeyCommand(
            title: "Pause",
            action: #selector(handlePauseCommand),
            input: "p",
            modifierFlags: .command
        )

        let restartCommand = UIKeyCommand(
            title: "Neustart",
            action: #selector(handleRestartCommand),
            input: "r",
            modifierFlags: .command
        )

        let gameMenu = UIMenu(
            title: "Spiel",
            children: [pauseCommand, restartCommand]
        )

        builder.insertSibling(gameMenu, afterMenu: .file)
    }

    @objc private func handlePauseCommand() {
        NotificationCenter.default.post(name: .pauseGame, object: nil)
    }

    @objc private func handleRestartCommand() {
        NotificationCenter.default.post(name: .restartGame, object: nil)
    }
    #endif
}

// MARK: - Notification Names
extension Notification.Name {
    static let pauseGame = Notification.Name("pauseGame")
    static let resumeGame = Notification.Name("resumeGame")
    static let restartGame = Notification.Name("restartGame")
}
