import Foundation
import UserNotifications

@MainActor
class NotificationManager: ObservableObject {
    static let shared = NotificationManager()

    @Published var isAuthorized = false
    @Published var pendingNotifications: [String] = []

    private let center = UNUserNotificationCenter.current()

    private init() {
        Task {
            await checkAuthorization()
        }
    }

    // MARK: - Authorization

    func requestAuthorization() async -> Bool {
        do {
            let granted = try await center.requestAuthorization(options: [.alert, .sound, .badge])
            isAuthorized = granted
            return granted
        } catch {
            print("Notification authorization failed: \(error)")
            return false
        }
    }

    func checkAuthorization() async {
        let settings = await center.notificationSettings()
        isAuthorized = settings.authorizationStatus == .authorized
    }

    // MARK: - Conflict Notifications

    func sendConflictNotification(count: Int) async {
        guard isAuthorized else { return }

        let content = UNMutableNotificationContent()
        content.title = "HealthBridge"

        if count == 1 {
            content.body = "1 neuer Konflikt erfordert Ihre Aufmerksamkeit"
        } else {
            content.body = "\(count) neue Konflikte erfordern Ihre Aufmerksamkeit"
        }

        content.sound = .default
        content.badge = NSNumber(value: count)
        content.categoryIdentifier = "CONFLICT"

        let request = UNNotificationRequest(
            identifier: "healthbridge.conflict.\(Date().timeIntervalSince1970)",
            content: content,
            trigger: nil
        )

        do {
            try await center.add(request)
        } catch {
            print("Failed to send notification: \(error)")
        }
    }

    // MARK: - Sync Notifications

    func sendSyncCompleteNotification(
        conflictsResolved: Int,
        pendingConflicts: Int
    ) async {
        guard isAuthorized else { return }

        let content = UNMutableNotificationContent()
        content.title = "Sync abgeschlossen"

        if pendingConflicts > 0 {
            content.body = "\(conflictsResolved) Konflikte gelöst, \(pendingConflicts) offen"
        } else {
            content.body = "Alle \(conflictsResolved) Konflikte wurden gelöst"
        }

        content.sound = .default
        content.categoryIdentifier = "SYNC_COMPLETE"

        let request = UNNotificationRequest(
            identifier: "healthbridge.sync.\(Date().timeIntervalSince1970)",
            content: content,
            trigger: nil
        )

        do {
            try await center.add(request)
        } catch {
            print("Failed to send notification: \(error)")
        }
    }

    // MARK: - Scheduled Notifications

    func scheduleReminder(at hour: Int, minute: Int) async {
        guard isAuthorized else { return }

        let content = UNMutableNotificationContent()
        content.title = "HealthBridge Erinnerung"
        content.body = "Vergessen Sie nicht, Ihre Gesundheitsdaten zu synchronisieren"
        content.sound = .default

        var dateComponents = DateComponents()
        dateComponents.hour = hour
        dateComponents.minute = minute

        let trigger = UNCalendarNotificationTrigger(dateMatching: dateComponents, repeats: true)

        let request = UNNotificationRequest(
            identifier: "healthbridge.reminder.daily",
            content: content,
            trigger: trigger
        )

        do {
            try await center.add(request)
        } catch {
            print("Failed to schedule reminder: \(error)")
        }
    }

    func cancelReminder() {
        center.removePendingNotificationRequests(withIdentifiers: ["healthbridge.reminder.daily"])
    }

    // MARK: - Badge Management

    func clearBadge() async {
        do {
            try await center.setBadgeCount(0)
        } catch {
            print("Failed to clear badge: \(error)")
        }
    }

    func updateBadge(count: Int) async {
        do {
            try await center.setBadgeCount(count)
        } catch {
            print("Failed to update badge: \(error)")
        }
    }

    // MARK: - Notification Categories

    func registerCategories() {
        // Conflict category with actions
        let resolveAction = UNNotificationAction(
            identifier: "RESOLVE_AUTO",
            title: "Automatisch lösen",
            options: []
        )

        let viewAction = UNNotificationAction(
            identifier: "VIEW_CONFLICTS",
            title: "Anzeigen",
            options: [.foreground]
        )

        let conflictCategory = UNNotificationCategory(
            identifier: "CONFLICT",
            actions: [resolveAction, viewAction],
            intentIdentifiers: [],
            options: []
        )

        // Sync complete category
        let syncCategory = UNNotificationCategory(
            identifier: "SYNC_COMPLETE",
            actions: [viewAction],
            intentIdentifiers: [],
            options: []
        )

        center.setNotificationCategories([conflictCategory, syncCategory])
    }
}
