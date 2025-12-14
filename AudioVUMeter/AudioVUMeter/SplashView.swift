//
//  SplashView.swift
//  AudioVUMeter
//
//  Splash screen shown at app startup
//  Presented by Gnafzgi Software
//

import SwiftUI

struct SplashView: View {
    @State private var isAnimating = false
    @State private var showApp = false
    @State private var logoScale: CGFloat = 0.5
    @State private var logoOpacity: Double = 0
    @State private var textOpacity: Double = 0
    @State private var subtitleOpacity: Double = 0
    @State private var waveOffset: CGFloat = 0

    let onComplete: () -> Void

    var body: some View {
        ZStack {
            // Background gradient
            LinearGradient(
                gradient: Gradient(colors: [
                    Color(red: 0.05, green: 0.05, blue: 0.1),
                    Color(red: 0.1, green: 0.08, blue: 0.15),
                    Color(red: 0.05, green: 0.05, blue: 0.1)
                ]),
                startPoint: .topLeading,
                endPoint: .bottomTrailing
            )
            .ignoresSafeArea()

            // Animated wave background
            WaveBackground(offset: waveOffset)
                .opacity(0.3)

            VStack(spacing: 30) {
                Spacer()

                // Animated VU Meter Icon
                ZStack {
                    // Glow effect
                    Circle()
                        .fill(
                            RadialGradient(
                                gradient: Gradient(colors: [
                                    Color.green.opacity(0.4),
                                    Color.clear
                                ]),
                                center: .center,
                                startRadius: 30,
                                endRadius: 80
                            )
                        )
                        .frame(width: 160, height: 160)
                        .blur(radius: 20)
                        .scaleEffect(isAnimating ? 1.2 : 1.0)

                    // Main icon
                    Image(systemName: "waveform.circle.fill")
                        .font(.system(size: 100))
                        .foregroundStyle(
                            LinearGradient(
                                colors: [.green, .cyan, .blue],
                                startPoint: .topLeading,
                                endPoint: .bottomTrailing
                            )
                        )
                        .shadow(color: .green.opacity(0.5), radius: 20)
                }
                .scaleEffect(logoScale)
                .opacity(logoOpacity)

                // App Title
                VStack(spacing: 8) {
                    Text("Audio VU Meter")
                        .font(.system(size: 36, weight: .bold, design: .rounded))
                        .foregroundStyle(
                            LinearGradient(
                                colors: [.white, .gray.opacity(0.8)],
                                startPoint: .top,
                                endPoint: .bottom
                            )
                        )

                    Text("Professional Audio Monitoring")
                        .font(.system(size: 14, weight: .medium, design: .rounded))
                        .foregroundColor(.gray)
                }
                .opacity(textOpacity)

                Spacer()

                // Presented by
                VStack(spacing: 6) {
                    Text("presented by")
                        .font(.system(size: 11, weight: .regular, design: .rounded))
                        .foregroundColor(.gray.opacity(0.6))
                        .tracking(2)

                    Text("GNAFZGI SOFTWARE")
                        .font(.system(size: 16, weight: .bold, design: .rounded))
                        .foregroundStyle(
                            LinearGradient(
                                colors: [.cyan, .blue],
                                startPoint: .leading,
                                endPoint: .trailing
                            )
                        )
                        .tracking(3)
                }
                .opacity(subtitleOpacity)
                .padding(.bottom, 50)
            }

            // Version badge
            VStack {
                Spacer()
                HStack {
                    Spacer()
                    Text("v1.3")
                        .font(.system(size: 10, weight: .medium, design: .monospaced))
                        .foregroundColor(.gray.opacity(0.5))
                        .padding(8)
                }
            }
        }
        .frame(width: 400, height: 500)
        .onAppear {
            startAnimations()
        }
    }

    private func startAnimations() {
        // Wave animation (continuous)
        withAnimation(.linear(duration: 8).repeatForever(autoreverses: false)) {
            waveOffset = 1
        }

        // Logo animation
        withAnimation(.spring(response: 0.8, dampingFraction: 0.6).delay(0.2)) {
            logoScale = 1.0
            logoOpacity = 1.0
        }

        // Pulse animation
        withAnimation(.easeInOut(duration: 1.5).repeatForever(autoreverses: true).delay(0.5)) {
            isAnimating = true
        }

        // Title animation
        withAnimation(.easeOut(duration: 0.8).delay(0.6)) {
            textOpacity = 1.0
        }

        // Subtitle animation
        withAnimation(.easeOut(duration: 0.8).delay(1.0)) {
            subtitleOpacity = 1.0
        }

        // Auto-dismiss after delay
        DispatchQueue.main.asyncAfter(deadline: .now() + 2.5) {
            withAnimation(.easeOut(duration: 0.3)) {
                onComplete()
            }
        }
    }
}

// MARK: - Wave Background
struct WaveBackground: View {
    let offset: CGFloat

    var body: some View {
        GeometryReader { geometry in
            ZStack {
                // First wave
                WavePath(offset: offset, amplitude: 20, frequency: 1.5)
                    .stroke(
                        LinearGradient(
                            colors: [.green.opacity(0.3), .cyan.opacity(0.2)],
                            startPoint: .leading,
                            endPoint: .trailing
                        ),
                        lineWidth: 2
                    )

                // Second wave
                WavePath(offset: offset + 0.3, amplitude: 15, frequency: 2)
                    .stroke(
                        LinearGradient(
                            colors: [.blue.opacity(0.2), .purple.opacity(0.2)],
                            startPoint: .leading,
                            endPoint: .trailing
                        ),
                        lineWidth: 1.5
                    )

                // Third wave
                WavePath(offset: offset + 0.6, amplitude: 25, frequency: 1)
                    .stroke(
                        LinearGradient(
                            colors: [.cyan.opacity(0.15), .green.opacity(0.1)],
                            startPoint: .leading,
                            endPoint: .trailing
                        ),
                        lineWidth: 1
                    )
            }
        }
    }
}

// MARK: - Wave Path
struct WavePath: Shape {
    var offset: CGFloat
    var amplitude: CGFloat
    var frequency: CGFloat

    var animatableData: CGFloat {
        get { offset }
        set { offset = newValue }
    }

    func path(in rect: CGRect) -> Path {
        var path = Path()
        let midY = rect.midY

        path.move(to: CGPoint(x: 0, y: midY))

        for x in stride(from: 0, through: rect.width, by: 2) {
            let relativeX = x / rect.width
            let sine = sin((relativeX + offset) * .pi * 2 * frequency)
            let y = midY + sine * amplitude

            path.addLine(to: CGPoint(x: x, y: y))
        }

        return path
    }
}

// MARK: - Preview
#Preview {
    SplashView {
        print("Splash complete")
    }
}
