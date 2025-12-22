//
//  AppDelegate.swift
//  PsytranceVisualizer
//
//  Application delegate handling app lifecycle
//

import AppKit
import AVFoundation

/// Application delegate
final class AppDelegate: NSObject, NSApplicationDelegate {
    // MARK: - Properties

    private var mainWindowController: MainWindowController?

    // MARK: - App Lifecycle

    func applicationDidFinishLaunching(_ notification: Notification) {
        // Request microphone permission
        requestMicrophonePermission()

        // Create and show main window
        mainWindowController = MainWindowController()
        mainWindowController?.showWindow(nil)
        mainWindowController?.window?.makeKeyAndOrderFront(nil)

        // Activate the application
        NSApp.activate(ignoringOtherApps: true)

        print("[AppDelegate] Application launched")
    }

    func applicationWillTerminate(_ notification: Notification) {
        // Save settings
        SettingsManager.shared.saveNow()

        print("[AppDelegate] Application terminating")
    }

    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool {
        return true
    }

    func applicationSupportsSecureRestorableState(_ app: NSApplication) -> Bool {
        return true
    }

    // MARK: - Permissions

    private func requestMicrophonePermission() {
        switch AVCaptureDevice.authorizationStatus(for: .audio) {
        case .authorized:
            print("[AppDelegate] Microphone access already authorized")

        case .notDetermined:
            AVCaptureDevice.requestAccess(for: .audio) { granted in
                if granted {
                    print("[AppDelegate] Microphone access granted")
                } else {
                    print("[AppDelegate] Microphone access denied")
                    self.showMicrophonePermissionAlert()
                }
            }

        case .denied, .restricted:
            print("[AppDelegate] Microphone access denied or restricted")
            showMicrophonePermissionAlert()

        @unknown default:
            break
        }
    }

    private func showMicrophonePermissionAlert() {
        DispatchQueue.main.async {
            let alert = NSAlert()
            alert.messageText = "Microphone Access Required"
            alert.informativeText = "Psytrance Visualizer needs access to your audio input to visualize music. Please enable microphone access in System Preferences > Security & Privacy > Privacy > Microphone."
            alert.alertStyle = .warning
            alert.addButton(withTitle: "Open System Preferences")
            alert.addButton(withTitle: "Cancel")

            if alert.runModal() == .alertFirstButtonReturn {
                if let url = URL(string: "x-apple.systempreferences:com.apple.preference.security?Privacy_Microphone") {
                    NSWorkspace.shared.open(url)
                }
            }
        }
    }

    // MARK: - Menu Actions

    @IBAction func showAbout(_ sender: Any) {
        let alert = NSAlert()
        alert.messageText = "Psytrance Visualizer"
        alert.informativeText = """
        An audio-reactive visualizer for psytrance music.

        8 Visualization Modes:
        1 - FFT Classic
        2 - Mel Spectrogram
        3 - Sub-Bass
        4 - Sidechain Pump
        5 - Harmonic/Noise
        6 - Mandelbrot
        7 - Tunnel Warp
        8 - DMT Geometry

        Keyboard Shortcuts:
        1-8: Switch visualization mode
        F: Toggle fullscreen
        ESC: Exit fullscreen

        Tip: Use a virtual audio device like BlackHole to route system audio.
        """
        alert.alertStyle = .informational
        alert.runModal()
    }
}
