//
//  MenuBarView.swift
//  FT991A-Remote
//
//  Menu bar extra for background operation
//

import SwiftUI

// MARK: - Menu Bar View

struct MenuBarView: View {
    @Environment(\.openSettings) private var openSettings
    @EnvironmentObject var radioViewModel: RadioViewModel
    @EnvironmentObject var settingsController: SettingsController

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            // Connection status
            HStack {
                Circle()
                    .fill(radioViewModel.isConnected ? Color.green : Color.red)
                    .frame(width: 10, height: 10)

                Text(radioViewModel.isConnected ? "Verbunden" : "Getrennt")
                    .font(.headline)

                Circle()
                    .fill(radioViewModel.catAlive ? Color.green : (radioViewModel.isConnected ? Color.orange : Color.secondary))
                    .frame(width: 8, height: 8)

                Spacer()

                Button(radioViewModel.isConnected ? "Trennen" : "Verbinden") {
                    radioViewModel.toggleConnection()
                }
                .controlSize(.small)
            }

            if radioViewModel.isConnected {
                Text(radioViewModel.catStatusText)
                    .font(.caption)
                    .foregroundColor(.secondary)

                Divider()

                // Frequency display
                VStack(alignment: .leading, spacing: 4) {
                    Text("Frequenz")
                        .font(.caption)
                        .foregroundColor(.secondary)

                    Text(radioViewModel.frequencyDisplay + " Hz")
                        .font(.system(.title3, design: .monospaced))
                }

                // Mode and Band
                HStack {
                    Text(radioViewModel.mode.rawValue)
                        .padding(.horizontal, 8)
                        .padding(.vertical, 2)
                        .background(Color.accentColor.opacity(0.2))
                        .cornerRadius(4)

                    if let band = radioViewModel.currentBand {
                        Text(band.rawValue)
                            .foregroundColor(.secondary)
                    }

                    Spacer()

                    Text(radioViewModel.sMeterDisplay)
                        .font(.caption.monospacedDigit())
                }

                Text(radioViewModel.toneSummaryText)
                    .font(.caption)
                    .foregroundColor(.secondary)

                // TX Status
                if radioViewModel.isTransmitting {
                    HStack {
                        Circle()
                            .fill(Color.red)
                            .frame(width: 10, height: 10)
                        Text("Senden")
                            .foregroundColor(.red)
                    }
                }

                Divider()

                // Quick controls
                HStack(spacing: 12) {
                    Button {
                        radioViewModel.selectVFO(radioViewModel.activeVFO == .a ? .b : .a)
                    } label: {
                        Text("VFO \(radioViewModel.activeVFO.rawValue)")
                    }
                    .controlSize(.small)

                    Button("A/B") {
                        radioViewModel.swapVFO()
                    }
                    .controlSize(.small)

                    if radioViewModel.capabilities.showsATUTune {
                        Button("ATU") {
                            radioViewModel.startATUTune()
                        }
                        .controlSize(.small)
                    }
                }
            }

            Divider()

            // App controls
            Button("Hauptfenster öffnen") {
                NSApp.activate(ignoringOtherApps: true)
                if let window = NSApp.windows.first {
                    window.makeKeyAndOrderFront(nil)
                }
            }

            Button("Einstellungen...") {
                openSettings()
            }
            .keyboardShortcut(",", modifiers: .command)

            Divider()

            Button("Beenden") {
                NSApp.terminate(nil)
            }
            .keyboardShortcut("q", modifiers: .command)
        }
        .padding()
        .frame(width: 280)
    }
}

#if DEBUG
struct MenuBarView_Previews: PreviewProvider {
    static var previews: some View {
        MenuBarView()
            .environmentObject(RadioViewModel())
            .environmentObject(SettingsController())
    }
}
#endif
