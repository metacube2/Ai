//
//  MenuBarView.swift
//  FT991A-Remote
//
//  Menu bar extra for background operation
//

import SwiftUI

// MARK: - Menu Bar View

struct MenuBarView: View {
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

                Spacer()

                Button(radioViewModel.isConnected ? "Trennen" : "Verbinden") {
                    radioViewModel.toggleConnection()
                }
                .controlSize(.small)
            }

            if radioViewModel.isConnected {
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

                    Button("ATU") {
                        radioViewModel.startATUTune()
                    }
                    .controlSize(.small)
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
                NSApp.sendAction(Selector(("showSettingsWindow:")), to: nil, from: nil)
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

// MARK: - Preview

#Preview {
    MenuBarView()
        .environmentObject(RadioViewModel())
        .environmentObject(SettingsController())
}
