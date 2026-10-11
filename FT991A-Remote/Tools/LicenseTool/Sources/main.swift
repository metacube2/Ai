import Foundation
import CryptoKit

private let licenseSecret = "MacYaesu-License-v1::FT991A::Offline"

private func normalizeEmail(_ raw: String) -> String {
    raw.trimmingCharacters(in: .whitespacesAndNewlines).lowercased()
}

private func generateLicenseKey(for email: String) -> String {
    let normalizedEmail = normalizeEmail(email)
    let material = "\(normalizedEmail)|\(licenseSecret)"
    let digest = SHA256.hash(data: Data(material.utf8))
    let hex = digest.map { String(format: "%02X", $0) }.joined()
    let prefix = String(hex.prefix(16))

    return stride(from: 0, to: prefix.count, by: 4).map { start in
        let s = prefix.index(prefix.startIndex, offsetBy: start)
        let e = prefix.index(s, offsetBy: 4)
        return String(prefix[s..<e])
    }.joined(separator: "-")
}

private func printUsage() {
    print("""
    Usage:
      swift run --package-path Tools/LicenseTool license-tool generate <email>
      swift run --package-path Tools/LicenseTool license-tool verify <email> <key>
    """)
}

let args = Array(CommandLine.arguments.dropFirst())

guard let command = args.first else {
    printUsage()
    exit(1)
}

switch command {
case "generate":
    guard args.count == 2 else {
        printUsage()
        exit(1)
    }
    let email = normalizeEmail(args[1])
    guard !email.isEmpty else {
        fputs("E-Mail darf nicht leer sein.\n", stderr)
        exit(2)
    }
    print(generateLicenseKey(for: email))

case "verify":
    guard args.count == 3 else {
        printUsage()
        exit(1)
    }
    let email = normalizeEmail(args[1])
    let key = args[2].trimmingCharacters(in: .whitespacesAndNewlines).uppercased()
    let valid = generateLicenseKey(for: email) == key
    print(valid ? "VALID" : "INVALID")
    exit(valid ? 0 : 3)

default:
    printUsage()
    exit(1)
}
