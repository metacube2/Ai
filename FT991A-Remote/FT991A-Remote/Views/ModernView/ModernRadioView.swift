//
//  ModernRadioView.swift
//  FT991A-Remote
//
//  Modern UI style for radio control
//

import SwiftUI

// MARK: - Modern Radio View

struct ModernRadioView: View {
    @EnvironmentObject var radioViewModel: RadioViewModel
    @EnvironmentObject var settingsController: SettingsController

    var body: some View {
        ScrollView {
            VStack(spacing: 20) {
                // Frequency Section
                FrequencyView()

                // Mode & Filter Section
                HStack(spacing: 20) {
                    ModeView()
                    Spacer()
                    LevelView()
                }

                // Functions Section
                FunctionsView()

                // Metering Section
                MeteringView()

                // PTT Section
                PTTButton()

                Spacer()
            }
            .padding()
        }
    }
}

// MARK: - Frequency View

struct FrequencyView: View {
    @EnvironmentObject var radioViewModel: RadioViewModel
    @EnvironmentObject var settingsController: SettingsController

    @State private var frequencyInput = ""
    @State private var isEditing = false

    var body: some View {
        GroupBox("Frequenz") {
            VStack(spacing: 16) {
                // VFO Selection
                HStack {
                    // VFO A
                    Button {
                        radioViewModel.selectVFO(.a)
                    } label: {
                        HStack {
                            Circle()
                                .fill(radioViewModel.activeVFO == .a ? Color.green : Color.gray.opacity(0.3))
                                .frame(width: 12, height: 12)
                            Text("VFO-A")
                                .font(.headline)
                            Text(radioViewModel.formatFrequency(radioViewModel.vfoAFrequency))
                                .font(.system(.body, design: .monospaced))
                                .foregroundColor(.secondary)
                        }
                        .padding(.horizontal, 12)
                        .padding(.vertical, 8)
                        .background(radioViewModel.activeVFO == .a ? Color.accentColor.opacity(0.1) : Color.clear)
                        .cornerRadius(8)
                    }
                    .buttonStyle(.plain)
                    .disabled(!radioViewModel.isConnected)

                    Spacer()

                    // VFO controls
                    HStack(spacing: 8) {
                        Button("A/B") {
                            radioViewModel.swapVFO()
                        }
                        .help("VFO A und B tauschen")

                        Button("A=B") {
                            radioViewModel.equalizeVFO()
                        }
                        .help("VFO B auf A-Frequenz setzen")
                    }
                    .disabled(!radioViewModel.isConnected)

                    Spacer()

                    // VFO B
                    Button {
                        radioViewModel.selectVFO(.b)
                    } label: {
                        HStack {
                            Text(radioViewModel.formatFrequency(radioViewModel.vfoBFrequency))
                                .font(.system(.body, design: .monospaced))
                                .foregroundColor(.secondary)
                            Text("VFO-B")
                                .font(.headline)
                            Circle()
                                .fill(radioViewModel.activeVFO == .b ? Color.green : Color.gray.opacity(0.3))
                                .frame(width: 12, height: 12)
                        }
                        .padding(.horizontal, 12)
                        .padding(.vertical, 8)
                        .background(radioViewModel.activeVFO == .b ? Color.accentColor.opacity(0.1) : Color.clear)
                        .cornerRadius(8)
                    }
                    .buttonStyle(.plain)
                    .disabled(!radioViewModel.isConnected)
                }

                // Main frequency display
                HStack {
                    Button {
                        radioViewModel.decrementFrequency()
                    } label: {
                        Image(systemName: "minus.circle.fill")
                            .font(.title)
                    }
                    .buttonStyle(.plain)
                    .keyboardShortcut(.leftArrow, modifiers: [])
                    .disabled(!radioViewModel.isConnected)

                    Spacer()

                    // Frequency display
                    VStack(spacing: 4) {
                        Text(radioViewModel.frequencyDisplay)
                            .font(.system(size: 48, weight: .bold, design: .monospaced))
                            .foregroundColor(radioViewModel.isTransmitting ? .red : .primary)

                        Text("Hz")
                            .font(.caption)
                            .foregroundColor(.secondary)
                    }

                    Spacer()

                    Button {
                        radioViewModel.incrementFrequency()
                    } label: {
                        Image(systemName: "plus.circle.fill")
                            .font(.title)
                    }
                    .buttonStyle(.plain)
                    .keyboardShortcut(.rightArrow, modifiers: [])
                    .disabled(!radioViewModel.isConnected)
                }

                // Frequency step selector
                HStack {
                    Text("Schritt:")
                        .foregroundColor(.secondary)

                    Picker("Schritt", selection: $settingsController.frequencyStep) {
                        ForEach(FrequencyStep.allCases, id: \.self) { step in
                            Text(step.displayName).tag(step)
                        }
                    }
                    .pickerStyle(.segmented)
                    .frame(maxWidth: 500)
                }

                // Band buttons
                ScrollView(.horizontal, showsIndicators: false) {
                    HStack(spacing: 8) {
                        ForEach(Band.allCases, id: \.self) { band in
                            Button {
                                radioViewModel.selectBand(band)
                            } label: {
                                Text(band.rawValue)
                                    .font(.caption)
                                    .padding(.horizontal, 12)
                                    .padding(.vertical, 6)
                                    .background(radioViewModel.currentBand == band ? Color.accentColor : Color.secondary.opacity(0.2))
                                    .foregroundColor(radioViewModel.currentBand == band ? .white : .primary)
                                    .cornerRadius(6)
                            }
                            .buttonStyle(.plain)
                            .disabled(!radioViewModel.isConnected)
                        }
                    }
                }
            }
            .padding()
        }
    }
}

// MARK: - Mode View

struct ModeView: View {
    @EnvironmentObject var radioViewModel: RadioViewModel

    let commonModes: [OperatingMode] = [.lsb, .usb, .cw, .fm, .am]
    let digitalModes: [OperatingMode] = [.dataLSB, .dataUSB, .rttyLSB, .rttyUSB, .c4fm]

    var body: some View {
        GroupBox("Betriebsart") {
            VStack(alignment: .leading, spacing: 12) {
                // Common modes
                HStack(spacing: 8) {
                    ForEach(commonModes, id: \.self) { mode in
                        Button {
                            radioViewModel.setMode(mode)
                        } label: {
                            Text(mode.rawValue)
                                .font(.caption.bold())
                                .frame(width: 50)
                                .padding(.vertical, 6)
                                .background(radioViewModel.mode == mode ? Color.accentColor : Color.secondary.opacity(0.2))
                                .foregroundColor(radioViewModel.mode == mode ? .white : .primary)
                                .cornerRadius(6)
                        }
                        .buttonStyle(.plain)
                        .disabled(!radioViewModel.isConnected)
                    }
                }

                // Digital modes
                HStack(spacing: 8) {
                    ForEach(digitalModes, id: \.self) { mode in
                        Button {
                            radioViewModel.setMode(mode)
                        } label: {
                            Text(mode.rawValue)
                                .font(.caption.bold())
                                .frame(minWidth: 50)
                                .padding(.horizontal, 8)
                                .padding(.vertical, 6)
                                .background(radioViewModel.mode == mode ? Color.orange : Color.secondary.opacity(0.2))
                                .foregroundColor(radioViewModel.mode == mode ? .white : .primary)
                                .cornerRadius(6)
                        }
                        .buttonStyle(.plain)
                        .disabled(!radioViewModel.isConnected)
                    }
                }
            }
            .padding()
        }
    }
}

// MARK: - Level View

struct LevelView: View {
    @EnvironmentObject var radioViewModel: RadioViewModel

    var body: some View {
        GroupBox("Pegel") {
            VStack(spacing: 12) {
                LevelSlider(label: "AF", value: Binding(
                    get: { Double(radioViewModel.afGain) },
                    set: { radioViewModel.setAFGain(Int($0)) }
                ), range: 0...255, disabled: !radioViewModel.isConnected)

                LevelSlider(label: "RF", value: Binding(
                    get: { Double(radioViewModel.rfGain) },
                    set: { radioViewModel.setRFGain(Int($0)) }
                ), range: 0...255, disabled: !radioViewModel.isConnected)

                LevelSlider(label: "SQL", value: Binding(
                    get: { Double(radioViewModel.squelch) },
                    set: { radioViewModel.setSquelch(Int($0)) }
                ), range: 0...255, disabled: !radioViewModel.isConnected)

                LevelSlider(label: "MIC", value: Binding(
                    get: { Double(radioViewModel.micGain) },
                    set: { radioViewModel.setMICGain(Int($0)) }
                ), range: 0...100, disabled: !radioViewModel.isConnected)

                LevelSlider(label: "PWR", value: Binding(
                    get: { Double(radioViewModel.power) },
                    set: { radioViewModel.setPower(Int($0)) }
                ), range: 5...100, unit: "W", disabled: !radioViewModel.isConnected)
            }
            .padding()
        }
        .frame(width: 300)
    }
}

// MARK: - Level Slider

struct LevelSlider: View {
    let label: String
    @Binding var value: Double
    let range: ClosedRange<Double>
    var unit: String = "%"
    var disabled: Bool = false

    var displayValue: String {
        if unit == "W" {
            return "\(Int(value))W"
        } else {
            let percent = (value - range.lowerBound) / (range.upperBound - range.lowerBound) * 100
            return "\(Int(percent))%"
        }
    }

    var body: some View {
        HStack {
            Text(label)
                .font(.caption.bold())
                .frame(width: 35, alignment: .leading)

            Slider(value: $value, in: range)
                .disabled(disabled)

            Text(displayValue)
                .font(.caption.monospacedDigit())
                .frame(width: 45, alignment: .trailing)
        }
    }
}

// MARK: - Functions View

struct FunctionsView: View {
    @EnvironmentObject var radioViewModel: RadioViewModel

    var body: some View {
        GroupBox("Funktionen") {
            HStack(spacing: 12) {
                FunctionButton(label: "NB", isActive: radioViewModel.noiseBlanker) {
                    radioViewModel.toggleNB()
                }
                .disabled(!radioViewModel.isConnected)

                FunctionButton(label: "NR", isActive: radioViewModel.noiseReduction) {
                    radioViewModel.toggleNR()
                }
                .disabled(!radioViewModel.isConnected)

                FunctionButton(label: "DNF", isActive: radioViewModel.dnf) {
                    radioViewModel.toggleDNF()
                }
                .disabled(!radioViewModel.isConnected)

                FunctionButton(label: "CONT", isActive: radioViewModel.contour) {
                    // Toggle contour
                }
                .disabled(!radioViewModel.isConnected)

                Divider()
                    .frame(height: 30)

                FunctionButton(label: "ATU", isActive: radioViewModel.atu, color: .orange) {
                    radioViewModel.startATUTune()
                }
                .disabled(!radioViewModel.isConnected)
                .keyboardShortcut(.upArrow, modifiers: [])

                FunctionButton(label: "SPLIT", isActive: radioViewModel.split) {
                    radioViewModel.toggleSplit()
                }
                .disabled(!radioViewModel.isConnected)

                FunctionButton(label: "IPO", isActive: radioViewModel.ipo) {
                    // Toggle IPO
                }
                .disabled(!radioViewModel.isConnected)

                Spacer()
            }
            .padding()
        }
    }
}

// MARK: - Function Button

struct FunctionButton: View {
    let label: String
    let isActive: Bool
    var color: Color = .accentColor
    let action: () -> Void

    var body: some View {
        Button(action: action) {
            Text(label)
                .font(.caption.bold())
                .padding(.horizontal, 12)
                .padding(.vertical, 8)
                .background(isActive ? color : Color.secondary.opacity(0.2))
                .foregroundColor(isActive ? .white : .primary)
                .cornerRadius(6)
        }
        .buttonStyle(.plain)
    }
}

// MARK: - Metering View

struct MeteringView: View {
    @EnvironmentObject var radioViewModel: RadioViewModel

    var body: some View {
        GroupBox("Messwerte") {
            VStack(spacing: 16) {
                // S-Meter
                VStack(alignment: .leading, spacing: 4) {
                    HStack {
                        Text("S-Meter")
                            .font(.caption)
                            .foregroundColor(.secondary)
                        Spacer()
                        Text(radioViewModel.sMeterDisplay)
                            .font(.caption.bold().monospacedDigit())
                    }

                    SMeterBar(value: Double(radioViewModel.sMeter) / 255.0)
                }

                // Power meter (only shown when transmitting)
                if radioViewModel.isTransmitting {
                    VStack(alignment: .leading, spacing: 4) {
                        HStack {
                            Text("Leistung")
                                .font(.caption)
                                .foregroundColor(.secondary)
                            Spacer()
                            Text("\(radioViewModel.powerMeter)W")
                                .font(.caption.bold().monospacedDigit())
                        }

                        MeterBar(value: Double(radioViewModel.powerMeter) / 100.0, color: .orange)
                    }

                    VStack(alignment: .leading, spacing: 4) {
                        HStack {
                            Text("SWR")
                                .font(.caption)
                                .foregroundColor(.secondary)
                            Spacer()
                            Text(String(format: "%.1f:1", 1.0 + Double(radioViewModel.swrMeter) / 50.0))
                                .font(.caption.bold().monospacedDigit())
                        }

                        MeterBar(value: Double(radioViewModel.swrMeter) / 255.0, color: radioViewModel.swrMeter > 100 ? .red : .green)
                    }
                }
            }
            .padding()
        }
    }
}

// MARK: - S-Meter Bar

struct SMeterBar: View {
    let value: Double

    var body: some View {
        GeometryReader { geometry in
            ZStack(alignment: .leading) {
                // Background
                RoundedRectangle(cornerRadius: 4)
                    .fill(Color.secondary.opacity(0.2))

                // S-Unit markers
                HStack(spacing: 0) {
                    ForEach(0..<10) { i in
                        Rectangle()
                            .fill(Color.secondary.opacity(0.3))
                            .frame(width: 1)
                        if i < 9 {
                            Spacer()
                        }
                    }
                }
                .padding(.horizontal, 2)

                // Value bar
                RoundedRectangle(cornerRadius: 4)
                    .fill(
                        LinearGradient(
                            colors: [.green, .yellow, .orange, .red],
                            startPoint: .leading,
                            endPoint: .trailing
                        )
                    )
                    .frame(width: max(0, geometry.size.width * value))
            }
        }
        .frame(height: 20)
    }
}

// MARK: - Meter Bar

struct MeterBar: View {
    let value: Double
    var color: Color = .green

    var body: some View {
        GeometryReader { geometry in
            ZStack(alignment: .leading) {
                RoundedRectangle(cornerRadius: 4)
                    .fill(Color.secondary.opacity(0.2))

                RoundedRectangle(cornerRadius: 4)
                    .fill(color)
                    .frame(width: max(0, geometry.size.width * min(1, value)))
            }
        }
        .frame(height: 16)
    }
}

// MARK: - PTT Button

struct PTTButton: View {
    @EnvironmentObject var radioViewModel: RadioViewModel

    @State private var isPressed = false

    var body: some View {
        GroupBox("PTT") {
            VStack(spacing: 12) {
                Button {
                    radioViewModel.toggleTransmit()
                } label: {
                    HStack {
                        Image(systemName: radioViewModel.isTransmitting ? "mic.fill" : "mic")
                        Text(radioViewModel.isTransmitting ? "EMPFANG" : "SENDEN")
                            .font(.headline)
                    }
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 16)
                    .background(radioViewModel.isTransmitting ? Color.red : Color.accentColor)
                    .foregroundColor(.white)
                    .cornerRadius(8)
                }
                .buttonStyle(.plain)
                .disabled(!radioViewModel.isConnected)

                Text("Shift-Taste gedrückt halten = PTT")
                    .font(.caption)
                    .foregroundColor(.secondary)

                // TX indicator
                HStack {
                    Circle()
                        .fill(radioViewModel.isTransmitting ? Color.red : Color.gray.opacity(0.3))
                        .frame(width: 16, height: 16)
                    Text(radioViewModel.isTransmitting ? "TX" : "RX")
                        .font(.caption.bold())
                        .foregroundColor(radioViewModel.isTransmitting ? .red : .green)
                }
            }
            .padding()
        }
    }
}

// MARK: - Preview

#Preview {
    ModernRadioView()
        .environmentObject(RadioViewModel())
        .environmentObject(SettingsController())
        .frame(width: 800, height: 900)
        .padding()
}
