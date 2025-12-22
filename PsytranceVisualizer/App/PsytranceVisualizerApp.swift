//
//  PsytranceVisualizerApp.swift
//  PsytranceVisualizer
//
//  Main application entry point
//

import AppKit

// MARK: - Main Entry Point

/// Application entry point
@main
struct PsytranceVisualizerApp {
    static func main() {
        // Create the application
        let app = NSApplication.shared

        // Set up the delegate
        let delegate = AppDelegate()
        app.delegate = delegate

        // Set activation policy
        app.setActivationPolicy(.regular)

        // Create the main menu
        setupMainMenu()

        // Run the application
        app.run()
    }

    /// Sets up the application's main menu
    private static func setupMainMenu() {
        let mainMenu = NSMenu()

        // Application menu
        let appMenuItem = NSMenuItem()
        mainMenu.addItem(appMenuItem)

        let appMenu = NSMenu()
        appMenuItem.submenu = appMenu

        appMenu.addItem(withTitle: "About Psytrance Visualizer",
                       action: #selector(AppDelegate.showAbout(_:)),
                       keyEquivalent: "")

        appMenu.addItem(NSMenuItem.separator())

        appMenu.addItem(withTitle: "Hide Psytrance Visualizer",
                       action: #selector(NSApplication.hide(_:)),
                       keyEquivalent: "h")

        let hideOthersItem = appMenu.addItem(withTitle: "Hide Others",
                                             action: #selector(NSApplication.hideOtherApplications(_:)),
                                             keyEquivalent: "h")
        hideOthersItem.keyEquivalentModifierMask = [.command, .option]

        appMenu.addItem(withTitle: "Show All",
                       action: #selector(NSApplication.unhideAllApplications(_:)),
                       keyEquivalent: "")

        appMenu.addItem(NSMenuItem.separator())

        appMenu.addItem(withTitle: "Quit Psytrance Visualizer",
                       action: #selector(NSApplication.terminate(_:)),
                       keyEquivalent: "q")

        // View menu
        let viewMenuItem = NSMenuItem()
        mainMenu.addItem(viewMenuItem)

        let viewMenu = NSMenu(title: "View")
        viewMenuItem.submenu = viewMenu

        viewMenu.addItem(withTitle: "Toggle Fullscreen",
                        action: #selector(NSWindow.toggleFullScreen(_:)),
                        keyEquivalent: "f")

        viewMenu.addItem(NSMenuItem.separator())

        // Visualization mode submenu
        let modesMenuItem = NSMenuItem(title: "Visualization Mode", action: nil, keyEquivalent: "")
        let modesMenu = NSMenu()

        for mode in VisualizationMode.allCases {
            let item = NSMenuItem(title: mode.displayName,
                                 action: nil,
                                 keyEquivalent: mode.shortcut)
            item.tag = mode.rawValue
            modesMenu.addItem(item)
        }

        modesMenuItem.submenu = modesMenu
        viewMenu.addItem(modesMenuItem)

        // Window menu
        let windowMenuItem = NSMenuItem()
        mainMenu.addItem(windowMenuItem)

        let windowMenu = NSMenu(title: "Window")
        windowMenuItem.submenu = windowMenu

        windowMenu.addItem(withTitle: "Minimize",
                          action: #selector(NSWindow.miniaturize(_:)),
                          keyEquivalent: "m")

        windowMenu.addItem(withTitle: "Zoom",
                          action: #selector(NSWindow.zoom(_:)),
                          keyEquivalent: "")

        windowMenu.addItem(NSMenuItem.separator())

        windowMenu.addItem(withTitle: "Bring All to Front",
                          action: #selector(NSApplication.arrangeInFront(_:)),
                          keyEquivalent: "")

        // Help menu
        let helpMenuItem = NSMenuItem()
        mainMenu.addItem(helpMenuItem)

        let helpMenu = NSMenu(title: "Help")
        helpMenuItem.submenu = helpMenu

        helpMenu.addItem(withTitle: "Psytrance Visualizer Help",
                        action: #selector(AppDelegate.showAbout(_:)),
                        keyEquivalent: "?")

        NSApp.mainMenu = mainMenu
        NSApp.windowsMenu = windowMenu
        NSApp.helpMenu = helpMenu
    }
}
