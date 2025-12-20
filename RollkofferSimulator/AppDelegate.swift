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
}

// MARK: - Notification Names
extension Notification.Name {
    static let pauseGame = Notification.Name("pauseGame")
    static let resumeGame = Notification.Name("resumeGame")
}
