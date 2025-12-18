//
//  MainView.swift
//  FT991A-Remote
//
//  Main application window container
//

import SwiftUI

// MARK: - Main View

struct MainView: View {
    @EnvironmentObject var radioViewModel: RadioViewModel
    @EnvironmentObject var settingsController: SettingsController
    @EnvironmentObject var logViewModel: LogViewModel

    @State private var isDebugPanelDetached = false
    @State private var isLogPanelDetached = false

    var body: some View {
        NavigationSplitView {
            // Sidebar
            SidebarView()
                .frame(minWidth: 200)
        } detail: {
            // Main content
            HSplitView {
                // Radio control area
                VStack(spacing: 0) {
                    // Connection bar
                    ConnectionBar()
                        .padding(.horizontal)
                        .padding(.top, 8)

                    Divider()
                        .padding(.top, 8)

                    // Radio view based on UI style
                    if settingsController.uiStyle == .modern {
                        ModernRadioView()
                    } else {
                        SkeuomorphRadioView()
                    }
                }
                .frame(minWidth: 600)

                // Side panels
                if settingsController.showLogPanel && !isLogPanelDetached {
                    Divider()
                    LogPanel()
                        .frame(minWidth: 300, maxWidth: 400)
                }

                if settingsController.showDebugPanel && !isDebugPanelDetached {
                    Divider()
                    DebugPanel()
                        .frame(minWidth: 300, maxWidth: 400)
                }
            }
        }
        .toolbar {
            ToolbarItemGroup(placement: .primaryAction) {
                // UI Style toggle
                Picker("UI", selection: $settingsController.uiStyle) {
                    Image(systemName: "rectangle.3.group")
                        .tag(UIStyle.modern)
                    Image(systemName: "dial.medium")
                        .tag(UIStyle.skeuomorph)
                }
                .pickerStyle(.segmented)
                .help("UI-Stil wechseln")

                Divider()

                // Panel toggles
                Toggle(isOn: $settingsController.showLogPanel) {
                    Image(systemName: "list.bullet.rectangle")
                }
                .help("Log-Panel anzeigen")

                Toggle(isOn: $settingsController.showDebugPanel) {
                    Image(systemName: "terminal")
                }
                .help("Debug-Panel anzeigen")
            }
        }
        .navigationTitle("FT-991A Remote")
        .onAppear {
            setupKeyboardShortcuts()
        }
    }

    private func setupKeyboardShortcuts() {
        // Keyboard shortcuts are handled in the App commands
    }
}

// MARK: - Sidebar View

struct SidebarView: View {
    @EnvironmentObject var radioViewModel: RadioViewModel

    var body: some View {
        List {
            Section("Verbindung") {
                Label {
                    VStack(alignment: .leading) {
                        Text(radioViewModel.isConnected ? "Verbunden" : "Getrennt")
                            .font(.headline)
                        if radioViewModel.isConnected {
                            Text(radioViewModel.selectedPort)
                                .font(.caption)
                                .foregroundColor(.secondary)
                        }
                    }
                } icon: {
                    Image(systemName: radioViewModel.isConnected ? "antenna.radiowaves.left.and.right" : "antenna.radiowaves.left.and.right.slash")
                        .foregroundColor(radioViewModel.isConnected ? .green : .red)
                }
            }

            Section("Frequenz") {
                Label {
                    Text(radioViewModel.frequencyDisplay + " Hz")
                        .font(.system(.body, design: .monospaced))
                } icon: {
                    Image(systemName: "waveform")
                }

                if let band = radioViewModel.currentBand {
                    Label(band.rawValue, systemImage: "chart.bar")
                }

                Label(radioViewModel.mode.rawValue, systemImage: "waveform.path")
            }

            Section("Bänder") {
                ForEach(Band.allCases, id: \.self) { band in
                    Button {
                        radioViewModel.selectBand(band)
                    } label: {
                        Label(band.rawValue, systemImage: "antenna.radiowaves.left.and.right")
                    }
                    .disabled(!radioViewModel.isConnected)
                }
            }
        }
        .listStyle(.sidebar)
    }
}

// MARK: - Connection Bar

struct ConnectionBar: View {
    @EnvironmentObject var radioViewModel: RadioViewModel

    var body: some View {
        HStack(spacing: 12) {
            // Port selection
            Picker("Port", selection: $radioViewModel.selectedPort) {
                Text("Port wählen...").tag("")
                ForEach(radioViewModel.availablePorts) { port in
                    Text(port.name).tag(port.path)
                }
            }
            .frame(width: 200)

            // Refresh button
            Button {
                radioViewModel.refreshPorts()
            } label: {
                Image(systemName: "arrow.clockwise")
            }
            .help("Ports aktualisieren")

            // Baud rate
            Picker("Baud", selection: $radioViewModel.baudRate) {
                ForEach(SerialConfig.availableBaudRates, id: \.self) { rate in
                    Text("\(rate)").tag(rate)
                }
            }
            .frame(width: 100)

            Spacer()

            // Connection status
            HStack(spacing: 6) {
                Circle()
                    .fill(radioViewModel.isConnected ? Color.green : Color.red)
                    .frame(width: 10, height: 10)

                Text(radioViewModel.connectionState.displayString)
                    .foregroundColor(.secondary)
            }

            // Connect button
            Button {
                radioViewModel.toggleConnection()
            } label: {
                Text(radioViewModel.isConnected ? "Trennen" : "Verbinden")
            }
            .keyboardShortcut("k", modifiers: .command)
        }
        .padding(.vertical, 8)
    }
}

// MARK: - Preview

#Preview {
    MainView()
        .environmentObject(RadioViewModel())
        .environmentObject(SettingsController())
        .environmentObject(LogViewModel())
        .frame(width: 1200, height: 800)
}
