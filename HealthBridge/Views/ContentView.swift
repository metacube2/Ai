import SwiftUI

struct ContentView: View {
    @EnvironmentObject var appState: AppState
    @EnvironmentObject var syncCoordinator: SyncCoordinator

    var body: some View {
        TabView(selection: $appState.selectedTab) {
            DashboardView()
                .tabItem {
                    Label("Dashboard", systemImage: "chart.bar.fill")
                }
                .tag(AppState.Tab.dashboard)

            ConflictsView()
                .tabItem {
                    Label("Konflikte", systemImage: "arrow.triangle.2.circlepath")
                }
                .tag(AppState.Tab.conflicts)
                .badge(syncCoordinator.pendingConflicts.count)

            RulesView()
                .tabItem {
                    Label("Regeln", systemImage: "slider.horizontal.3")
                }
                .tag(AppState.Tab.rules)

            SourcesView()
                .tabItem {
                    Label("Quellen", systemImage: "antenna.radiowaves.left.and.right")
                }
                .tag(AppState.Tab.sources)
        }
        .tint(.blue)
    }
}

#Preview {
    ContentView()
        .environmentObject(AppState())
        .environmentObject(SyncCoordinator.shared)
}
