// swift-tools-version: 5.9
// The swift-tools-version declares the minimum version of Swift required to build this package.

import PackageDescription

let package = Package(
    name: "PsytranceVisualizer",
    platforms: [
        .macOS(.v13)
    ],
    products: [
        .executable(
            name: "PsytranceVisualizer",
            targets: ["PsytranceVisualizer"]
        )
    ],
    targets: [
        .executableTarget(
            name: "PsytranceVisualizer",
            path: ".",
            exclude: [
                "Package.swift",
                "README.md"
            ],
            sources: [
                "App",
                "Audio",
                "Models",
                "Rendering",
                "UI",
                "Utilities"
            ],
            resources: [
                .process("Resources")
            ],
            swiftSettings: [
                .unsafeFlags(["-enable-bare-slash-regex"])
            ]
        )
    ]
)
