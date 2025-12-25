import SwiftUI
import Charts

struct HealthChart: View {
    let dataType: HealthDataType
    let data: [ChartDataPoint]
    let showConflicts: Bool

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack {
                Image(systemName: dataType.icon)
                    .foregroundStyle(.blue)
                Text(dataType.displayName)
                    .font(.headline)
                Spacer()
                if let latest = data.last {
                    Text(latest.formattedValue)
                        .font(.title3)
                        .fontWeight(.semibold)
                    Text(dataType.unit)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }

            if #available(iOS 16.0, *) {
                Chart(data) { point in
                    LineMark(
                        x: .value("Zeit", point.date),
                        y: .value("Wert", point.value)
                    )
                    .foregroundStyle(.blue)

                    PointMark(
                        x: .value("Zeit", point.date),
                        y: .value("Wert", point.value)
                    )
                    .foregroundStyle(point.hasConflict && showConflicts ? .orange : .blue)
                    .symbolSize(point.hasConflict && showConflicts ? 100 : 50)
                }
                .frame(height: 150)
                .chartXAxis {
                    AxisMarks(values: .stride(by: .hour, count: 4)) { value in
                        AxisValueLabel(format: .dateTime.hour())
                    }
                }
                .chartYAxis {
                    AxisMarks(position: .leading)
                }
            } else {
                // Fallback for iOS 15
                simpleChart
            }
        }
        .padding()
        .background(Color(.secondarySystemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }

    private var simpleChart: some View {
        GeometryReader { geometry in
            let maxValue = data.map { $0.value }.max() ?? 1
            let minValue = data.map { $0.value }.min() ?? 0
            let range = maxValue - minValue

            Path { path in
                guard !data.isEmpty else { return }

                let xStep = geometry.size.width / CGFloat(max(1, data.count - 1))

                for (index, point) in data.enumerated() {
                    let x = CGFloat(index) * xStep
                    let normalizedY = range > 0 ? (point.value - minValue) / range : 0.5
                    let y = geometry.size.height * (1 - normalizedY)

                    if index == 0 {
                        path.move(to: CGPoint(x: x, y: y))
                    } else {
                        path.addLine(to: CGPoint(x: x, y: y))
                    }
                }
            }
            .stroke(Color.blue, lineWidth: 2)
        }
        .frame(height: 150)
    }
}

struct ChartDataPoint: Identifiable {
    let id = UUID()
    let date: Date
    let value: Double
    let hasConflict: Bool
    let sourceId: String?

    var formattedValue: String {
        if value == floor(value) {
            return String(format: "%.0f", value)
        }
        return String(format: "%.1f", value)
    }
}

// MARK: - Blood Pressure Chart
struct BloodPressureChart: View {
    let data: [BloodPressurePoint]

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack {
                Image(systemName: "drop.fill")
                    .foregroundStyle(.red)
                Text("Blutdruck")
                    .font(.headline)
                Spacer()
                if let latest = data.last {
                    Text("\(Int(latest.systolic))/\(Int(latest.diastolic))")
                        .font(.title3)
                        .fontWeight(.semibold)
                    Text("mmHg")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }

            if #available(iOS 16.0, *) {
                Chart(data) { point in
                    // Systolic
                    LineMark(
                        x: .value("Zeit", point.date),
                        y: .value("Systolisch", point.systolic)
                    )
                    .foregroundStyle(.red)

                    // Diastolic
                    LineMark(
                        x: .value("Zeit", point.date),
                        y: .value("Diastolisch", point.diastolic)
                    )
                    .foregroundStyle(.blue)
                }
                .frame(height: 150)
                .chartYScale(domain: 40...180)
                .chartLegend(position: .bottom)
            }
        }
        .padding()
        .background(Color(.secondarySystemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }
}

struct BloodPressurePoint: Identifiable {
    let id = UUID()
    let date: Date
    let systolic: Double
    let diastolic: Double
    let classification: BloodPressureHandler.BloodPressureClassification
}

// MARK: - Summary Ring
struct SummaryRing: View {
    let progress: Double
    let color: Color
    let icon: String
    let value: String
    let label: String

    var body: some View {
        VStack(spacing: 4) {
            ZStack {
                Circle()
                    .stroke(color.opacity(0.2), lineWidth: 8)

                Circle()
                    .trim(from: 0, to: min(progress, 1.0))
                    .stroke(color, style: StrokeStyle(lineWidth: 8, lineCap: .round))
                    .rotationEffect(.degrees(-90))

                VStack(spacing: 2) {
                    Image(systemName: icon)
                        .font(.caption)
                        .foregroundStyle(color)
                    Text(value)
                        .font(.caption2)
                        .fontWeight(.semibold)
                }
            }
            .frame(width: 60, height: 60)

            Text(label)
                .font(.caption2)
                .foregroundStyle(.secondary)
        }
    }
}

#Preview {
    VStack(spacing: 20) {
        HealthChart(
            dataType: .steps,
            data: (0..<24).map { hour in
                ChartDataPoint(
                    date: Calendar.current.date(byAdding: .hour, value: hour, to: Calendar.current.startOfDay(for: Date()))!,
                    value: Double.random(in: 0...1000),
                    hasConflict: hour % 5 == 0,
                    sourceId: nil
                )
            },
            showConflicts: true
        )

        HStack(spacing: 20) {
            SummaryRing(
                progress: 0.75,
                color: .blue,
                icon: "figure.walk",
                value: "7.5k",
                label: "Schritte"
            )

            SummaryRing(
                progress: 0.5,
                color: .red,
                icon: "heart.fill",
                value: "72",
                label: "Puls"
            )

            SummaryRing(
                progress: 1.0,
                color: .green,
                icon: "checkmark.circle",
                value: "100%",
                label: "Synced"
            )
        }
    }
    .padding()
}
