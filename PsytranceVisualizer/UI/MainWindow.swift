//
//  MainWindow.swift
//  PsytranceVisualizer
//
//  Main application window with keyboard handling
//

import AppKit
import Combine

/// Main window controller for the visualizer
final class MainWindowController: NSWindowController {
    // MARK: - Properties

    private var visualizerView: VisualizerView!
    private var controlPanel: ControlPanel!

    private var audioManager: AudioInputManager!
    private var dspEngine: DSPEngine!
    private var settingsManager: SettingsManager { .shared }

    private var cancellables = Set<AnyCancellable>()
    private var displayLink: CVDisplayLink?

    // MARK: - Initialization

    convenience init() {
        let window = NSWindow(
            contentRect: NSRect(x: 0, y: 0, width: 1280, height: 720),
            styleMask: [.titled, .closable, .miniaturizable, .resizable, .fullSizeContentView],
            backing: .buffered,
            defer: false
        )

        window.title = "Psytrance Visualizer"
        window.minSize = NSSize(width: 800, height: 600)
        window.titlebarAppearsTransparent = true
        window.titleVisibility = .hidden
        window.isMovableByWindowBackground = true
        window.backgroundColor = .black
        window.collectionBehavior = [.fullScreenPrimary]

        // Restore window frame if saved
        if let savedFrame = SettingsManager.shared.settings.windowFrame?.cgRect {
            window.setFrame(savedFrame, display: false)
        } else {
            window.center()
        }

        self.init(window: window)

        setupContent()
        setupAudio()
        setupKeyboardHandling()
        restoreSettings()
    }

    // MARK: - Setup

    private func setupContent() {
        guard let contentView = window?.contentView else { return }

        // Visualizer view (fills entire window)
        visualizerView = VisualizerView()
        visualizerView.translatesAutoresizingMaskIntoConstraints = false
        contentView.addSubview(visualizerView)

        // Control panel (overlay at bottom)
        controlPanel = ControlPanel()
        controlPanel.translatesAutoresizingMaskIntoConstraints = false
        controlPanel.delegate = self
        contentView.addSubview(controlPanel)

        NSLayoutConstraint.activate([
            // Visualizer fills entire window
            visualizerView.topAnchor.constraint(equalTo: contentView.topAnchor),
            visualizerView.leadingAnchor.constraint(equalTo: contentView.leadingAnchor),
            visualizerView.trailingAnchor.constraint(equalTo: contentView.trailingAnchor),
            visualizerView.bottomAnchor.constraint(equalTo: contentView.bottomAnchor),

            // Control panel at bottom
            controlPanel.leadingAnchor.constraint(equalTo: contentView.leadingAnchor),
            controlPanel.trailingAnchor.constraint(equalTo: contentView.trailingAnchor),
            controlPanel.bottomAnchor.constraint(equalTo: contentView.bottomAnchor),
            controlPanel.heightAnchor.constraint(equalToConstant: 90),
        ])

        // Mouse tracking for control panel
        setupMouseTracking()
    }

    private func setupAudio() {
        audioManager = AudioInputManager()
        dspEngine = DSPEngine(bufferSize: settingsManager.settings.bufferSize)

        // Audio buffer callback
        audioManager.onAudioBuffer = { [weak self] buffer in
            guard let self = self else { return }
            let analysisData = self.dspEngine.process(buffer: buffer)

            DispatchQueue.main.async {
                self.visualizerView.updateAudioData(analysisData)
            }
        }

        // Update control panel when devices change
        audioManager.$availableDevices
            .receive(on: DispatchQueue.main)
            .sink { [weak self] devices in
                self?.controlPanel.updateDevices(
                    devices,
                    selectedUID: self?.settingsManager.settings.selectedAudioDeviceUID
                )
            }
            .store(in: &cancellables)

        // Start audio
        audioManager.start()
    }

    private func setupKeyboardHandling() {
        // Monitor for key events
        NSEvent.addLocalMonitorForEvents(matching: .keyDown) { [weak self] event in
            if self?.handleKeyDown(event) == true {
                return nil // Event handled
            }
            return event
        }
    }

    private func setupMouseTracking() {
        guard let contentView = window?.contentView else { return }

        let options: NSTrackingArea.Options = [.mouseMoved, .activeAlways, .inVisibleRect]
        let trackingArea = NSTrackingArea(
            rect: contentView.bounds,
            options: options,
            owner: self,
            userInfo: nil
        )
        contentView.addTrackingArea(trackingArea)
    }

    private func restoreSettings() {
        let settings = settingsManager.settings

        // Restore visualization mode
        if let mode = VisualizationMode(rawValue: settings.lastVisualizationMode) {
            visualizerView.setVisualizationMode(mode)
            controlPanel.updateMode(mode)
        }

        // Restore reactivity
        visualizerView.setReactivity(settings.reactivity)
        dspEngine.setReactivity(settings.reactivity)
        controlPanel.updateReactivity(settings.reactivity)

        // Restore buffer size
        dspEngine.setBufferSize(settings.bufferSize)
        audioManager.setBufferSize(settings.bufferSize)
        controlPanel.updateBufferSize(settings.bufferSize)

        // Restore audio device
        if let deviceUID = settings.selectedAudioDeviceUID {
            audioManager.selectDevice(uid: deviceUID)
        }

        // Restore fullscreen state
        if settings.isFullscreen {
            DispatchQueue.main.asyncAfter(deadline: .now() + 0.5) { [weak self] in
                self?.window?.toggleFullScreen(nil)
            }
        }
    }

    // MARK: - Keyboard Handling

    private func handleKeyDown(_ event: NSEvent) -> Bool {
        // Check for visualization mode shortcuts (1-8)
        if let mode = VisualizationMode.fromKeyCode(event.keyCode) {
            setVisualizationMode(mode)
            return true
        }

        // Other keyboard shortcuts
        switch event.keyCode {
        case 3: // F key
            toggleFullscreen()
            return true
        case 53: // Escape
            if window?.styleMask.contains(.fullScreen) == true {
                window?.toggleFullScreen(nil)
            }
            return true
        case 49: // Space
            // Toggle pause (could be implemented)
            return true
        default:
            break
        }

        // Cmd+F for fullscreen
        if event.modifierFlags.contains(.command) && event.keyCode == 3 {
            toggleFullscreen()
            return true
        }

        return false
    }

    // MARK: - Mode Switching

    private func setVisualizationMode(_ mode: VisualizationMode) {
        visualizerView.setVisualizationMode(mode)
        controlPanel.updateMode(mode)
        settingsManager.setVisualizationMode(mode)
    }

    // MARK: - Fullscreen

    private func toggleFullscreen() {
        window?.toggleFullScreen(nil)
    }

    // MARK: - Mouse Events

    override func mouseMoved(with event: NSEvent) {
        controlPanel.resetHideTimer()
    }

    // MARK: - Window Events

    override func windowDidLoad() {
        super.windowDidLoad()

        // Save window frame on move/resize
        NotificationCenter.default.addObserver(
            self,
            selector: #selector(windowDidResize),
            name: NSWindow.didResizeNotification,
            object: window
        )

        NotificationCenter.default.addObserver(
            self,
            selector: #selector(windowDidMove),
            name: NSWindow.didMoveNotification,
            object: window
        )

        NotificationCenter.default.addObserver(
            self,
            selector: #selector(windowDidEnterFullScreen),
            name: NSWindow.didEnterFullScreenNotification,
            object: window
        )

        NotificationCenter.default.addObserver(
            self,
            selector: #selector(windowDidExitFullScreen),
            name: NSWindow.didExitFullScreenNotification,
            object: window
        )
    }

    @objc private func windowDidResize(_ notification: Notification) {
        if let frame = window?.frame {
            settingsManager.setWindowFrame(frame)
        }
    }

    @objc private func windowDidMove(_ notification: Notification) {
        if let frame = window?.frame {
            settingsManager.setWindowFrame(frame)
        }
    }

    @objc private func windowDidEnterFullScreen(_ notification: Notification) {
        settingsManager.setFullscreen(true)
        controlPanel.hide()
    }

    @objc private func windowDidExitFullScreen(_ notification: Notification) {
        settingsManager.setFullscreen(false)
        controlPanel.show()
    }

    // MARK: - Cleanup

    deinit {
        audioManager.stop()
        settingsManager.saveNow()
    }
}

// MARK: - ControlPanelDelegate

extension MainWindowController: ControlPanelDelegate {
    func controlPanel(_ panel: ControlPanel, didSelectDevice uid: String) {
        audioManager.selectDevice(uid: uid)
        settingsManager.setAudioDevice(uid: uid)
    }

    func controlPanel(_ panel: ControlPanel, didSelectBufferSize size: Int) {
        audioManager.setBufferSize(size)
        dspEngine.setBufferSize(size)
        settingsManager.setBufferSize(size)
    }

    func controlPanel(_ panel: ControlPanel, didSelectMode mode: VisualizationMode) {
        setVisualizationMode(mode)
    }

    func controlPanel(_ panel: ControlPanel, didChangeReactivity value: Float) {
        visualizerView.setReactivity(value)
        dspEngine.setReactivity(value)
        settingsManager.setReactivity(value)
    }

    func controlPanelDidRequestFullscreen(_ panel: ControlPanel) {
        toggleFullscreen()
    }
}
