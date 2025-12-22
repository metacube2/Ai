//
//  ControlPanel.swift
//  PsytranceVisualizer
//
//  Auto-hiding control panel with audio and visualization settings
//

import AppKit
import Combine

/// Delegate protocol for control panel actions
protocol ControlPanelDelegate: AnyObject {
    func controlPanel(_ panel: ControlPanel, didSelectDevice uid: String)
    func controlPanel(_ panel: ControlPanel, didSelectBufferSize size: Int)
    func controlPanel(_ panel: ControlPanel, didSelectMode mode: VisualizationMode)
    func controlPanel(_ panel: ControlPanel, didChangeReactivity value: Float)
    func controlPanelDidRequestFullscreen(_ panel: ControlPanel)
}

/// Auto-hiding control panel overlay
final class ControlPanel: NSView {
    // MARK: - Properties

    weak var delegate: ControlPanelDelegate?

    private var isVisible = true
    private var hideTimer: Timer?
    private let hideDelay: TimeInterval = 3.0

    private var audioDevices: [AudioDevice] = []
    private var selectedMode: VisualizationMode = .fftClassic

    // MARK: - UI Elements

    private let containerView = NSVisualEffectView()
    private let devicePopup = NSPopUpButton()
    private let bufferSizePopup = NSPopUpButton()
    private let modeSegment = NSSegmentedControl()
    private let reactivitySlider = NSSlider()
    private let reactivityLabel = NSTextField(labelWithString: "Reactivity")
    private let fullscreenButton = NSButton()

    // MARK: - Layout Constants

    private let panelHeight: CGFloat = 60
    private let padding: CGFloat = 12
    private let elementHeight: CGFloat = 24

    // MARK: - Initialization

    override init(frame frameRect: NSRect) {
        super.init(frame: frameRect)
        setupUI()
        setupConstraints()
        startHideTimer()
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    // MARK: - Setup

    private func setupUI() {
        // Container with vibrancy effect
        containerView.material = .hudWindow
        containerView.blendingMode = .behindWindow
        containerView.state = .active
        containerView.wantsLayer = true
        containerView.layer?.cornerRadius = 12
        containerView.layer?.masksToBounds = true
        addSubview(containerView)

        // Device popup
        devicePopup.target = self
        devicePopup.action = #selector(deviceChanged)
        devicePopup.controlSize = .small
        devicePopup.font = .systemFont(ofSize: 11)
        containerView.addSubview(devicePopup)

        // Buffer size popup
        bufferSizePopup.target = self
        bufferSizePopup.action = #selector(bufferSizeChanged)
        bufferSizePopup.controlSize = .small
        bufferSizePopup.font = .systemFont(ofSize: 11)
        bufferSizePopup.addItems(withTitles: ["512", "1024"])
        bufferSizePopup.selectItem(withTitle: "1024")
        containerView.addSubview(bufferSizePopup)

        // Mode segment control
        modeSegment.segmentCount = 8
        for mode in VisualizationMode.allCases {
            modeSegment.setLabel(mode.shortcut, forSegment: mode.rawValue - 1)
            modeSegment.setToolTip(mode.displayName, forSegment: mode.rawValue - 1)
        }
        modeSegment.selectedSegment = 0
        modeSegment.target = self
        modeSegment.action = #selector(modeChanged)
        modeSegment.controlSize = .small
        modeSegment.segmentStyle = .capsule
        containerView.addSubview(modeSegment)

        // Reactivity label
        reactivityLabel.font = .systemFont(ofSize: 10)
        reactivityLabel.textColor = .secondaryLabelColor
        containerView.addSubview(reactivityLabel)

        // Reactivity slider
        reactivitySlider.minValue = 0.0
        reactivitySlider.maxValue = 1.0
        reactivitySlider.doubleValue = 0.5
        reactivitySlider.target = self
        reactivitySlider.action = #selector(reactivityChanged)
        reactivitySlider.controlSize = .small
        containerView.addSubview(reactivitySlider)

        // Fullscreen button
        fullscreenButton.title = "⛶"
        fullscreenButton.bezelStyle = .accessoryBarAction
        fullscreenButton.target = self
        fullscreenButton.action = #selector(fullscreenClicked)
        fullscreenButton.toolTip = "Toggle Fullscreen (F)"
        containerView.addSubview(fullscreenButton)

        // Set colors
        applyPsytranceTheme()
    }

    private func applyPsytranceTheme() {
        // Custom appearance for psytrance aesthetic
        containerView.appearance = NSAppearance(named: .darkAqua)
    }

    private func setupConstraints() {
        containerView.translatesAutoresizingMaskIntoConstraints = false
        devicePopup.translatesAutoresizingMaskIntoConstraints = false
        bufferSizePopup.translatesAutoresizingMaskIntoConstraints = false
        modeSegment.translatesAutoresizingMaskIntoConstraints = false
        reactivityLabel.translatesAutoresizingMaskIntoConstraints = false
        reactivitySlider.translatesAutoresizingMaskIntoConstraints = false
        fullscreenButton.translatesAutoresizingMaskIntoConstraints = false

        NSLayoutConstraint.activate([
            // Container
            containerView.leadingAnchor.constraint(equalTo: leadingAnchor, constant: padding),
            containerView.trailingAnchor.constraint(equalTo: trailingAnchor, constant: -padding),
            containerView.bottomAnchor.constraint(equalTo: bottomAnchor, constant: -padding),
            containerView.heightAnchor.constraint(equalToConstant: panelHeight),

            // Device popup
            devicePopup.leadingAnchor.constraint(equalTo: containerView.leadingAnchor, constant: padding),
            devicePopup.centerYAnchor.constraint(equalTo: containerView.centerYAnchor),
            devicePopup.widthAnchor.constraint(equalToConstant: 150),

            // Buffer size popup
            bufferSizePopup.leadingAnchor.constraint(equalTo: devicePopup.trailingAnchor, constant: 8),
            bufferSizePopup.centerYAnchor.constraint(equalTo: containerView.centerYAnchor),
            bufferSizePopup.widthAnchor.constraint(equalToConstant: 60),

            // Mode segment
            modeSegment.centerXAnchor.constraint(equalTo: containerView.centerXAnchor),
            modeSegment.centerYAnchor.constraint(equalTo: containerView.centerYAnchor),

            // Reactivity label
            reactivityLabel.trailingAnchor.constraint(equalTo: reactivitySlider.leadingAnchor, constant: -4),
            reactivityLabel.centerYAnchor.constraint(equalTo: containerView.centerYAnchor),

            // Reactivity slider
            reactivitySlider.trailingAnchor.constraint(equalTo: fullscreenButton.leadingAnchor, constant: -padding),
            reactivitySlider.centerYAnchor.constraint(equalTo: containerView.centerYAnchor),
            reactivitySlider.widthAnchor.constraint(equalToConstant: 80),

            // Fullscreen button
            fullscreenButton.trailingAnchor.constraint(equalTo: containerView.trailingAnchor, constant: -padding),
            fullscreenButton.centerYAnchor.constraint(equalTo: containerView.centerYAnchor),
        ])
    }

    // MARK: - Public Methods

    /// Updates the list of available audio devices
    func updateDevices(_ devices: [AudioDevice], selectedUID: String?) {
        audioDevices = devices
        devicePopup.removeAllItems()

        for device in devices {
            devicePopup.addItem(withTitle: device.name)
            devicePopup.lastItem?.representedObject = device.uid
        }

        if let uid = selectedUID,
           let index = devices.firstIndex(where: { $0.uid == uid }) {
            devicePopup.selectItem(at: index)
        }
    }

    /// Updates the selected buffer size
    func updateBufferSize(_ size: Int) {
        bufferSizePopup.selectItem(withTitle: "\(size)")
    }

    /// Updates the selected visualization mode
    func updateMode(_ mode: VisualizationMode) {
        selectedMode = mode
        modeSegment.selectedSegment = mode.rawValue - 1
    }

    /// Updates the reactivity slider
    func updateReactivity(_ value: Float) {
        reactivitySlider.doubleValue = Double(value)
    }

    /// Shows the control panel
    func show(animated: Bool = true) {
        guard !isVisible else { return }
        isVisible = true

        if animated {
            NSAnimationContext.runAnimationGroup { context in
                context.duration = 0.3
                self.animator().alphaValue = 1.0
            }
        } else {
            alphaValue = 1.0
        }

        startHideTimer()
    }

    /// Hides the control panel
    func hide(animated: Bool = true) {
        guard isVisible else { return }
        isVisible = false
        hideTimer?.invalidate()

        if animated {
            NSAnimationContext.runAnimationGroup { context in
                context.duration = 0.3
                self.animator().alphaValue = 0.0
            }
        } else {
            alphaValue = 0.0
        }
    }

    /// Resets the hide timer (call on mouse movement)
    func resetHideTimer() {
        show()
        startHideTimer()
    }

    // MARK: - Private Methods

    private func startHideTimer() {
        hideTimer?.invalidate()
        hideTimer = Timer.scheduledTimer(withTimeInterval: hideDelay, repeats: false) { [weak self] _ in
            self?.hide()
        }
    }

    // MARK: - Actions

    @objc private func deviceChanged() {
        guard let uid = devicePopup.selectedItem?.representedObject as? String else { return }
        delegate?.controlPanel(self, didSelectDevice: uid)
    }

    @objc private func bufferSizeChanged() {
        guard let title = bufferSizePopup.selectedItem?.title,
              let size = Int(title) else { return }
        delegate?.controlPanel(self, didSelectBufferSize: size)
    }

    @objc private func modeChanged() {
        let modeIndex = modeSegment.selectedSegment + 1
        guard let mode = VisualizationMode(rawValue: modeIndex) else { return }
        selectedMode = mode
        delegate?.controlPanel(self, didSelectMode: mode)
    }

    @objc private func reactivityChanged() {
        let value = Float(reactivitySlider.doubleValue)
        delegate?.controlPanel(self, didChangeReactivity: value)
    }

    @objc private func fullscreenClicked() {
        delegate?.controlPanelDidRequestFullscreen(self)
    }

    // MARK: - Mouse Tracking

    override func updateTrackingAreas() {
        super.updateTrackingAreas()

        // Remove existing tracking areas
        for area in trackingAreas {
            removeTrackingArea(area)
        }

        // Add new tracking area
        let options: NSTrackingArea.Options = [.mouseEnteredAndExited, .mouseMoved, .activeAlways]
        let trackingArea = NSTrackingArea(rect: bounds, options: options, owner: self, userInfo: nil)
        addTrackingArea(trackingArea)
    }

    override func mouseMoved(with event: NSEvent) {
        super.mouseMoved(with: event)
        resetHideTimer()
    }

    override func mouseEntered(with event: NSEvent) {
        super.mouseEntered(with: event)
        show()
    }
}
