//
//  VUMeterView.swift
//  AudioVUMeter
//
//  Classic VU Meter visualization component
//

import SwiftUI

enum MeterColorScheme {
    case audio
    case cpu
    case ram
    case disk
    case network

    var gradient: [Color] {
        switch self {
        case .audio:
            return [.green, .yellow, .orange, .red]
        case .cpu:
            return [.blue, .cyan, .yellow, .red]
        case .ram:
            return [.purple, .pink, .orange, .red]
        case .disk:
            return [.teal, .green, .yellow, .orange]
        case .network:
            return [.indigo, .blue, .cyan, .green]
        }
    }

    var accentColor: Color {
        switch self {
        case .audio: return .green
        case .cpu: return .blue
        case .ram: return .purple
        case .disk: return .teal
        case .network: return .indigo
        }
    }
}

// MARK: - Vertical VU Meter (for Audio)
struct VUMeterView: View {
    let level: Double // 0.0 to 1.0
    let peakLevel: Double
    let label: String
    let colorScheme: MeterColorScheme

    @State private var animatedLevel: Double = 0

    private let segmentCount = 20
    private let meterHeight: CGFloat = 200
    private let meterWidth: CGFloat = 35

    var body: some View {
        VStack(spacing: 8) {
            // Label
            Text(label)
                .font(.system(size: 14, weight: .bold, design: .monospaced))
                .foregroundColor(.white)

            // Meter
            ZStack(alignment: .bottom) {
                // Background
                RoundedRectangle(cornerRadius: 4)
                    .fill(Color.black.opacity(0.5))
                    .frame(width: meterWidth, height: meterHeight)

                // Segments
                VStack(spacing: 2) {
                    ForEach((0..<segmentCount).reversed(), id: \.self) { index in
                        let segmentThreshold = Double(index) / Double(segmentCount)
                        let isLit = animatedLevel > segmentThreshold

                        RoundedRectangle(cornerRadius: 2)
                            .fill(segmentColor(for: index, isLit: isLit))
                            .frame(width: meterWidth - 6, height: (meterHeight - CGFloat(segmentCount + 1) * 2) / CGFloat(segmentCount))
                            .shadow(color: isLit ? segmentColor(for: index, isLit: true).opacity(0.5) : .clear, radius: 3)
                    }
                }
                .padding(3)

                // Peak indicator
                if peakLevel > 0 {
                    let peakPosition = meterHeight * CGFloat(1 - peakLevel)
                    Rectangle()
                        .fill(Color.red)
                        .frame(width: meterWidth - 2, height: 3)
                        .offset(y: -meterHeight + peakPosition + meterHeight)
                }

                // dB Scale markers
                HStack {
                    VStack(alignment: .trailing, spacing: 0) {
                        ForEach([0, -6, -12, -20, -40, -60], id: \.self) { db in
                            Text("\(db)")
                                .font(.system(size: 8, design: .monospaced))
                                .foregroundColor(.gray)
                            if db != -60 {
                                Spacer()
                            }
                        }
                    }
                    .frame(height: meterHeight)
                    .offset(x: -meterWidth/2 - 15)

                    Spacer()
                }
            }
            .frame(width: meterWidth + 30, height: meterHeight)
        }
        .onChange(of: level) { newValue in
            withAnimation(.easeOut(duration: 0.05)) {
                animatedLevel = newValue
            }
        }
        .onAppear {
            animatedLevel = level
        }
    }

    private func segmentColor(for index: Int, isLit: Bool) -> Color {
        if !isLit {
            return Color.gray.opacity(0.2)
        }

        let position = Double(index) / Double(segmentCount)
        let colors = colorScheme.gradient

        if position > 0.9 { return colors[3] } // Red zone
        if position > 0.75 { return colors[2] } // Orange zone
        if position > 0.5 { return colors[1] } // Yellow zone
        return colors[0] // Green zone
    }
}

// MARK: - Circular System Meter
struct SystemMeterView: View {
    let value: Double // 0.0 to 100.0
    let label: String
    let unit: String
    let colorScheme: MeterColorScheme

    @State private var animatedValue: Double = 0

    private let meterSize: CGFloat = 70

    var body: some View {
        VStack(spacing: 5) {
            ZStack {
                // Background circle
                Circle()
                    .stroke(Color.gray.opacity(0.2), lineWidth: 8)
                    .frame(width: meterSize, height: meterSize)

                // Progress arc
                Circle()
                    .trim(from: 0, to: CGFloat(animatedValue / 100))
                    .stroke(
                        AngularGradient(
                            gradient: Gradient(colors: colorScheme.gradient),
                            center: .center,
                            startAngle: .degrees(0),
                            endAngle: .degrees(360)
                        ),
                        style: StrokeStyle(lineWidth: 8, lineCap: .round)
                    )
                    .frame(width: meterSize, height: meterSize)
                    .rotationEffect(.degrees(-90))

                // Value display
                VStack(spacing: 0) {
                    Text(String(format: "%.0f", animatedValue))
                        .font(.system(size: 18, weight: .bold, design: .monospaced))
                        .foregroundColor(.white)
                    Text(unit)
                        .font(.system(size: 10, design: .monospaced))
                        .foregroundColor(.gray)
                }
            }

            Text(label)
                .font(.system(size: 11, weight: .semibold, design: .monospaced))
                .foregroundColor(colorScheme.accentColor)
        }
        .onChange(of: value) { newValue in
            withAnimation(.easeOut(duration: 0.3)) {
                animatedValue = newValue
            }
        }
        .onAppear {
            animatedValue = value
        }
    }
}

// MARK: - Horizontal Bar Meter
struct HorizontalMeterView: View {
    let value: Double
    let label: String
    let colorScheme: MeterColorScheme

    @State private var animatedValue: Double = 0

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            HStack {
                Text(label)
                    .font(.system(size: 11, weight: .semibold, design: .monospaced))
                    .foregroundColor(.gray)
                Spacer()
                Text(String(format: "%.1f%%", animatedValue))
                    .font(.system(size: 11, weight: .bold, design: .monospaced))
                    .foregroundColor(.white)
            }

            GeometryReader { geometry in
                ZStack(alignment: .leading) {
                    // Background
                    RoundedRectangle(cornerRadius: 4)
                        .fill(Color.gray.opacity(0.2))

                    // Fill
                    RoundedRectangle(cornerRadius: 4)
                        .fill(
                            LinearGradient(
                                gradient: Gradient(colors: colorScheme.gradient),
                                startPoint: .leading,
                                endPoint: .trailing
                            )
                        )
                        .frame(width: geometry.size.width * CGFloat(animatedValue / 100))
                }
            }
            .frame(height: 12)
        }
        .onChange(of: value) { newValue in
            withAnimation(.easeOut(duration: 0.3)) {
                animatedValue = newValue
            }
        }
        .onAppear {
            animatedValue = value
        }
    }
}

#Preview {
    HStack(spacing: 30) {
        VUMeterView(level: 0.7, peakLevel: 0.9, label: "L", colorScheme: .audio)
        VUMeterView(level: 0.5, peakLevel: 0.8, label: "R", colorScheme: .audio)
    }
    .padding()
    .background(Color.black)
}
