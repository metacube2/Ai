# Audio VU Meter for macOS

A native macOS SwiftUI application that displays real-time audio levels from BlackHole (or any audio input device) as a classic VU meter, along with system resource monitoring.

![macOS](https://img.shields.io/badge/macOS-13.0+-blue.svg)
![Swift](https://img.shields.io/badge/Swift-5.0-orange.svg)
![License](https://img.shields.io/badge/License-MIT-green.svg)

## Features

### Audio VU Meter
- **Real-time audio level monitoring** - Displays Left and Right channel levels
- **dB scale display** - Shows audio levels in decibels (-60 dB to 0 dB)
- **Peak hold indicators** - Visual peak markers with configurable hold time
- **BlackHole integration** - Automatically detects and selects BlackHole virtual audio device
- **Multi-device support** - Switch between any available audio input device

### System Resource Monitors
- **CPU Usage** - Real-time CPU utilization percentage
- **RAM Usage** - Memory consumption monitoring
- **Disk Activity** - Disk I/O activity indicator
- **Network Activity** - Network throughput monitoring

## Requirements

- macOS 13.0 (Ventura) or later
- Xcode 15.0 or later (for building)
- [BlackHole](https://existential.audio/blackhole/) virtual audio driver (recommended)

## Installation

### Using BlackHole

1. Install BlackHole from [existential.audio/blackhole](https://existential.audio/blackhole/)
2. Configure BlackHole as a multi-output device in Audio MIDI Setup
3. Build and run Audio VU Meter
4. The app will automatically detect and select BlackHole

### Building from Source

1. Clone the repository
2. Open `AudioVUMeter.xcodeproj` in Xcode
3. Build and run (⌘R)

```bash
git clone <repository-url>
cd AudioVUMeter
open AudioVUMeter.xcodeproj
```

## Usage

### Main Window
- **Audio Levels**: The vertical VU meters show Left (L) and Right (R) channel audio levels
- **dB Readings**: Numeric display of current audio levels in decibels
- **System Meters**: Circular gauges showing CPU, RAM, Disk, and Network usage

### Controls
- **Start/Stop**: Toggle audio capture on/off
- **Reset**: Clear peak hold indicators
- **Settings**: Access device selection and preferences

### Settings
- **Input Device**: Select audio input source (BlackHole, microphone, etc.)
- **Reference Level**: Adjust the 0 dB reference point
- **Peak Hold Time**: Configure how long peak indicators remain visible

## Architecture

```
AudioVUMeter/
├── AudioVUMeterApp.swift    # App entry point
├── ContentView.swift        # Main UI layout
├── VUMeterView.swift        # VU meter components
├── AudioEngine.swift        # Core Audio capture engine
├── SystemMonitor.swift      # System resource monitoring
├── SettingsView.swift       # Settings window
└── Assets.xcassets/         # App icons and colors
```

### Key Components

- **AudioEngine**: Uses AVAudioEngine to capture audio from the selected input device, calculates RMS levels, and converts to dB
- **SystemMonitor**: Uses Mach kernel APIs to retrieve CPU, memory, disk, and network statistics
- **VUMeterView**: SwiftUI views for classic vertical VU meters with segment-based display
- **SystemMeterView**: Circular gauge components for system metrics

## BlackHole Setup Guide

1. **Install BlackHole**: Download and install from [existential.audio](https://existential.audio/blackhole/)

2. **Create Multi-Output Device**:
   - Open Audio MIDI Setup (Applications → Utilities)
   - Click the `+` button → Create Multi-Output Device
   - Check both your speakers and BlackHole
   - Set as default output

3. **Route Audio**:
   - System audio will now go to both speakers and BlackHole
   - Audio VU Meter captures from BlackHole input

## API Reference

### AudioEngine

```swift
// Start/stop audio capture
audioEngine.start()
audioEngine.stop()

// Reset peak indicators
audioEngine.resetPeaks()

// Switch audio device
audioEngine.selectedDeviceID = deviceID
audioEngine.switchDevice()

// Access levels
audioEngine.leftLevel      // 0.0 to 1.0
audioEngine.rightLevel     // 0.0 to 1.0
audioEngine.leftLevelDB    // -60 to 0 dB
audioEngine.rightLevelDB   // -60 to 0 dB
```

### SystemMonitor

```swift
// Start/stop monitoring
systemMonitor.startMonitoring()
systemMonitor.stopMonitoring()

// Access metrics (0-100%)
systemMonitor.cpuUsage
systemMonitor.memoryUsage
systemMonitor.diskActivity
systemMonitor.networkActivity
```

## License

MIT License - See LICENSE file for details.

## Credits

Inspired by [VU-Server](https://github.com/SasaKaranovic/VU-Server) by Sasa Karanovic.
