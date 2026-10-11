// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "LicenseTool",
    platforms: [
        .macOS(.v13)
    ],
    products: [
        .executable(name: "license-tool", targets: ["LicenseTool"])
    ],
    targets: [
        .executableTarget(
            name: "LicenseTool",
            path: "Sources"
        )
    ]
)
