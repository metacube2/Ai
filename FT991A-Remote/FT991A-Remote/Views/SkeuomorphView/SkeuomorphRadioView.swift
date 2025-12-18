//
//  SkeuomorphRadioView.swift
//  FT991A-Remote
//
//  Skeuomorphic FT-991A front panel replica
//

import SwiftUI

// MARK: - Skeuomorph Radio View

struct SkeuomorphRadioView: View {
    @EnvironmentObject var radioViewModel: RadioViewModel

    var body: some View {
        ZStack {
            // Background - dark metal texture
            LinearGradient(
                colors: [Color(white: 0.15), Color(white: 0.1)],
                startPoint: .top,
                endPoint: .bottom
            )
            .ignoresSafeArea()

            VStack(spacing: 0) {
                // Top section - Display
                FrontPanelDisplay()
                    .padding()

                Divider()
                    .background(Color.gray.opacity(0.3))

                // Middle section - Main controls
                HStack(spacing: 30) {
                    // Left side controls
                    VStack(spacing: 20) {
                        DialKnob(label: "AF GAIN", value: Binding(
                            get: { Double(radioViewModel.afGain) / 255.0 },
                            set: { radioViewModel.setAFGain(Int($0 * 255)) }
                        ))

                        DialKnob(label: "RF GAIN", value: Binding(
                            get: { Double(radioViewModel.rfGain) / 255.0 },
                            set: { radioViewModel.setRFGain(Int($0 * 255)) }
                        ))
                    }
                    .disabled(!radioViewModel.isConnected)

                    Spacer()

                    // Center - Main VFO dial
                    MainVFODial()

                    Spacer()

                    // Right side controls
                    VStack(spacing: 20) {
                        DialKnob(label: "SQL", value: Binding(
                            get: { Double(radioViewModel.squelch) / 255.0 },
                            set: { radioViewModel.setSquelch(Int($0 * 255)) }
                        ))

                        DialKnob(label: "MIC", value: Binding(
                            get: { Double(radioViewModel.micGain) / 100.0 },
                            set: { radioViewModel.setMICGain(Int($0 * 100)) }
                        ))
                    }
                    .disabled(!radioViewModel.isConnected)
                }
                .padding(.horizontal, 40)
                .padding(.vertical, 20)

                Divider()
                    .background(Color.gray.opacity(0.3))

                // Bottom section - Buttons
                FrontPanelButtons()
                    .padding()
            }
        }
    }
}

// MARK: - Front Panel Display

struct FrontPanelDisplay: View {
    @EnvironmentObject var radioViewModel: RadioViewModel

    var body: some View {
        ZStack {
            // LCD background
            RoundedRectangle(cornerRadius: 8)
                .fill(
                    LinearGradient(
                        colors: [Color(red: 0.05, green: 0.15, blue: 0.1), Color(red: 0.02, green: 0.1, blue: 0.05)],
                        startPoint: .top,
                        endPoint: .bottom
                    )
                )
                .overlay(
                    RoundedRectangle(cornerRadius: 8)
                        .stroke(Color.gray.opacity(0.5), lineWidth: 2)
                )

            VStack(spacing: 8) {
                // Top row - Status indicators
                HStack {
                    LCDIndicator(label: "VFO-A", isActive: radioViewModel.activeVFO == .a)
                    LCDIndicator(label: "VFO-B", isActive: radioViewModel.activeVFO == .b)
                    Spacer()
                    LCDIndicator(label: radioViewModel.mode.rawValue, isActive: true, color: .cyan)
                    Spacer()
                    LCDIndicator(label: "TX", isActive: radioViewModel.isTransmitting, color: .red)
                }
                .padding(.horizontal)

                // Main frequency display
                HStack {
                    Spacer()
                    Text(radioViewModel.frequencyDisplay)
                        .font(.system(size: 56, weight: .bold, design: .monospaced))
                        .foregroundColor(Color(red: 0.3, green: 1.0, blue: 0.5))
                        .shadow(color: Color(red: 0.3, green: 1.0, blue: 0.5).opacity(0.5), radius: 10)
                    Text("Hz")
                        .font(.system(size: 20, weight: .medium, design: .monospaced))
                        .foregroundColor(Color(red: 0.3, green: 1.0, blue: 0.5).opacity(0.7))
                    Spacer()
                }

                // S-Meter
                LCDSMeter(value: Double(radioViewModel.sMeter) / 255.0)
                    .padding(.horizontal)

                // Bottom row - Additional info
                HStack {
                    Text("\(radioViewModel.power)W")
                        .foregroundColor(Color(red: 0.3, green: 1.0, blue: 0.5).opacity(0.8))
                    Spacer()
                    if let band = radioViewModel.currentBand {
                        Text(band.rawValue)
                            .foregroundColor(Color(red: 0.3, green: 1.0, blue: 0.5).opacity(0.8))
                    }
                    Spacer()
                    Text(radioViewModel.sMeterDisplay)
                        .foregroundColor(Color(red: 0.3, green: 1.0, blue: 0.5).opacity(0.8))
                }
                .font(.system(size: 14, design: .monospaced))
                .padding(.horizontal)
            }
            .padding()
        }
        .frame(height: 200)
    }
}

// MARK: - LCD Indicator

struct LCDIndicator: View {
    let label: String
    let isActive: Bool
    var color: Color = .green

    var body: some View {
        Text(label)
            .font(.system(size: 12, weight: .bold, design: .monospaced))
            .foregroundColor(isActive ? color : color.opacity(0.3))
            .padding(.horizontal, 6)
            .padding(.vertical, 2)
            .background(
                RoundedRectangle(cornerRadius: 3)
                    .fill(isActive ? color.opacity(0.2) : Color.clear)
            )
    }
}

// MARK: - LCD S-Meter

struct LCDSMeter: View {
    let value: Double

    var body: some View {
        VStack(spacing: 2) {
            // Scale labels
            HStack {
                ForEach([1, 3, 5, 7, 9], id: \.self) { s in
                    Text("S\(s)")
                        .font(.system(size: 8, design: .monospaced))
                        .foregroundColor(Color(red: 0.3, green: 1.0, blue: 0.5).opacity(0.5))
                    if s < 9 { Spacer() }
                }
                Text("+20")
                    .font(.system(size: 8, design: .monospaced))
                    .foregroundColor(Color.red.opacity(0.5))
                Spacer()
                Text("+60")
                    .font(.system(size: 8, design: .monospaced))
                    .foregroundColor(Color.red.opacity(0.5))
            }

            // Bar segments
            HStack(spacing: 2) {
                ForEach(0..<20, id: \.self) { i in
                    let threshold = Double(i) / 20.0
                    let isLit = value >= threshold
                    let isRed = i >= 12 // Above S9

                    RoundedRectangle(cornerRadius: 1)
                        .fill(isLit ? (isRed ? Color.red : Color(red: 0.3, green: 1.0, blue: 0.5)) : Color.gray.opacity(0.2))
                        .frame(height: 16)
                }
            }
        }
    }
}

// MARK: - Dial Knob

struct DialKnob: View {
    let label: String
    @Binding var value: Double

    @State private var isDragging = false
    @State private var lastAngle: Double = 0

    var body: some View {
        VStack(spacing: 8) {
            Text(label)
                .font(.system(size: 10, weight: .bold))
                .foregroundColor(.gray)

            ZStack {
                // Knob base
                Circle()
                    .fill(
                        LinearGradient(
                            colors: [Color(white: 0.3), Color(white: 0.15)],
                            startPoint: .topLeading,
                            endPoint: .bottomTrailing
                        )
                    )
                    .overlay(
                        Circle()
                            .stroke(Color.gray.opacity(0.5), lineWidth: 2)
                    )
                    .shadow(color: .black.opacity(0.5), radius: 5, x: 2, y: 2)

                // Knob texture (ridges)
                ForEach(0..<12, id: \.self) { i in
                    Rectangle()
                        .fill(Color.white.opacity(0.1))
                        .frame(width: 1, height: 25)
                        .offset(y: -15)
                        .rotationEffect(.degrees(Double(i) * 30))
                }

                // Indicator line
                Rectangle()
                    .fill(Color.white)
                    .frame(width: 3, height: 15)
                    .offset(y: -20)
                    .rotationEffect(.degrees(value * 270 - 135))
            }
            .frame(width: 60, height: 60)
            .gesture(
                DragGesture()
                    .onChanged { gesture in
                        let center = CGPoint(x: 30, y: 30)
                        let location = gesture.location
                        let angle = atan2(location.y - center.y, location.x - center.x)
                        let degrees = angle * 180 / .pi + 90

                        if isDragging {
                            let delta = (degrees - lastAngle) / 270
                            value = min(1, max(0, value + delta))
                        }

                        lastAngle = degrees
                        isDragging = true
                    }
                    .onEnded { _ in
                        isDragging = false
                    }
            )

            Text("\(Int(value * 100))%")
                .font(.system(size: 10, design: .monospaced))
                .foregroundColor(.gray)
        }
    }
}

// MARK: - Main VFO Dial

struct MainVFODial: View {
    @EnvironmentObject var radioViewModel: RadioViewModel
    @EnvironmentObject var settingsController: SettingsController

    @State private var rotation: Double = 0

    var body: some View {
        VStack(spacing: 12) {
            Text("MAIN DIAL")
                .font(.system(size: 12, weight: .bold))
                .foregroundColor(.gray)

            ZStack {
                // Large dial
                Circle()
                    .fill(
                        LinearGradient(
                            colors: [Color(white: 0.25), Color(white: 0.1)],
                            startPoint: .topLeading,
                            endPoint: .bottomTrailing
                        )
                    )
                    .overlay(
                        Circle()
                            .stroke(Color.gray.opacity(0.5), lineWidth: 3)
                    )
                    .shadow(color: .black.opacity(0.5), radius: 10, x: 4, y: 4)

                // Dial markings
                ForEach(0..<36, id: \.self) { i in
                    Rectangle()
                        .fill(Color.white.opacity(i % 3 == 0 ? 0.3 : 0.1))
                        .frame(width: i % 3 == 0 ? 2 : 1, height: i % 3 == 0 ? 20 : 10)
                        .offset(y: -65)
                        .rotationEffect(.degrees(Double(i) * 10 + rotation))
                }

                // Center cap
                Circle()
                    .fill(Color(white: 0.2))
                    .frame(width: 40, height: 40)
                    .overlay(
                        Circle()
                            .stroke(Color.gray.opacity(0.3), lineWidth: 1)
                    )
            }
            .frame(width: 160, height: 160)
            .gesture(
                DragGesture()
                    .onChanged { gesture in
                        let delta = gesture.translation.width / 2
                        rotation += delta

                        // Convert rotation to frequency change
                        let steps = Int(delta / 10)
                        if steps != 0 {
                            for _ in 0..<abs(steps) {
                                if steps > 0 {
                                    radioViewModel.incrementFrequency()
                                } else {
                                    radioViewModel.decrementFrequency()
                                }
                            }
                        }
                    }
            )
            .disabled(!radioViewModel.isConnected)

            // Step indicator
            Picker("Step", selection: $settingsController.frequencyStep) {
                ForEach(FrequencyStep.allCases, id: \.self) { step in
                    Text(step.displayName).tag(step)
                }
            }
            .pickerStyle(.menu)
            .frame(width: 100)
        }
    }
}

// MARK: - Front Panel Buttons

struct FrontPanelButtons: View {
    @EnvironmentObject var radioViewModel: RadioViewModel

    var body: some View {
        HStack(spacing: 12) {
            // Mode buttons
            Group {
                PanelButton(label: "LSB", isActive: radioViewModel.mode == .lsb) {
                    radioViewModel.setMode(.lsb)
                }
                PanelButton(label: "USB", isActive: radioViewModel.mode == .usb) {
                    radioViewModel.setMode(.usb)
                }
                PanelButton(label: "CW", isActive: radioViewModel.mode == .cw) {
                    radioViewModel.setMode(.cw)
                }
                PanelButton(label: "FM", isActive: radioViewModel.mode == .fm) {
                    radioViewModel.setMode(.fm)
                }
                PanelButton(label: "AM", isActive: radioViewModel.mode == .am) {
                    radioViewModel.setMode(.am)
                }
            }
            .disabled(!radioViewModel.isConnected)

            Spacer()

            // Function buttons
            Group {
                PanelButton(label: "NB", isActive: radioViewModel.noiseBlanker) {
                    radioViewModel.toggleNB()
                }
                PanelButton(label: "NR", isActive: radioViewModel.noiseReduction) {
                    radioViewModel.toggleNR()
                }
                PanelButton(label: "ATU", isActive: false, color: .orange) {
                    radioViewModel.startATUTune()
                }
            }
            .disabled(!radioViewModel.isConnected)

            Spacer()

            // VFO buttons
            Group {
                PanelButton(label: "A/B", isActive: false) {
                    radioViewModel.swapVFO()
                }
                PanelButton(label: "SPLIT", isActive: radioViewModel.split) {
                    radioViewModel.toggleSplit()
                }
            }
            .disabled(!radioViewModel.isConnected)

            Spacer()

            // PTT
            PanelButton(label: radioViewModel.isTransmitting ? "RX" : "TX",
                       isActive: radioViewModel.isTransmitting,
                       color: .red,
                       size: .large) {
                radioViewModel.toggleTransmit()
            }
            .disabled(!radioViewModel.isConnected)
        }
    }
}

// MARK: - Panel Button

struct PanelButton: View {
    let label: String
    let isActive: Bool
    var color: Color = .green
    var size: Size = .normal
    let action: () -> Void

    enum Size {
        case normal, large
    }

    var body: some View {
        Button(action: action) {
            Text(label)
                .font(.system(size: size == .large ? 14 : 11, weight: .bold))
                .foregroundColor(isActive ? .white : .gray)
                .frame(width: size == .large ? 60 : 45, height: size == .large ? 40 : 30)
                .background(
                    RoundedRectangle(cornerRadius: 4)
                        .fill(isActive ? color : Color(white: 0.2))
                        .overlay(
                            RoundedRectangle(cornerRadius: 4)
                                .stroke(Color.gray.opacity(0.3), lineWidth: 1)
                        )
                        .shadow(color: isActive ? color.opacity(0.5) : .clear, radius: 5)
                )
        }
        .buttonStyle(.plain)
    }
}

// MARK: - Preview

#Preview {
    SkeuomorphRadioView()
        .environmentObject(RadioViewModel())
        .environmentObject(SettingsController())
        .frame(width: 900, height: 700)
}
