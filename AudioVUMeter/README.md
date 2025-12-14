# Audio VU Meter for macOS

A native macOS SwiftUI application that displays real-time audio levels from BlackHole (or any audio input device) as a classic VU meter, along with system resource monitoring. **Now with physical VU meter hardware support!**

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

### Physical VU Meter Hardware Support
- **4 Physical Dials** - Support for up to 4 physical VU meter dials
- **Flexible Channel Mapping** - Assign any metric to any dial:
  - Audio Left/Right channels
  - Audio Peak or Mono (L+R)
  - CPU, RAM, Disk, Network usage
- **Multiple Serial Protocols**:
  - Raw Bytes: `[0xAA] [D1] [D2] [D3] [D4] [0x55]`
  - Text Commands: `CH1:val;CH2:val;CH3:val;CH4:val\n`
  - JSON: `{"dials":[d1,d2,d3,d4]}`
  - VU-Server Compatible: `#0:val\n#1:val\n...`
- **Configurable per dial**: Min/max values, inversion, smoothing
- **Auto-detection** of USB serial devices

## Requirements

- macOS 13.0 (Ventura) or later
- Xcode 15.0 or later (for building)
- [BlackHole](https://existential.audio/blackhole/) virtual audio driver (recommended)
- USB/Serial VU meter hardware (optional)

## Installation

### Using BlackHole

1. Install BlackHole from [existential.audio/blackhole](https://existential.audio/blackhole/)
2. Configure BlackHole as a multi-output device in Audio MIDI Setup
3. Build and run Audio VU Meter
4. The app will automatically detect and select BlackHole

### Building from Source

1. Clone the repository
2. Open `AudioVUMeter.xcodeproj` in Xcode
3. Build and run (Cmd+R)

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
- **Hardware Output**: Shows status of connected physical VU meters

### Controls
- **Start/Stop**: Toggle audio capture on/off
- **Reset**: Clear peak hold indicators
- **Settings** (gear icon): Access device selection and preferences
- **Hardware** (cable icon): Configure physical VU meter connection

### Hardware Setup

1. Connect your USB/Serial VU meter hardware
2. Click the cable icon or go to Settings -> Hardware
3. Select your serial port from the dropdown
4. Choose the appropriate baud rate (default: 115200)
5. Select the communication protocol your hardware uses
6. Assign channels to each dial (Audio L, R, CPU, RAM, etc.)
7. Click "Connect"

### Settings
- **Input Device**: Select audio input source (BlackHole, microphone, etc.)
- **Reference Level**: Adjust the 0 dB reference point
- **Peak Hold Time**: Configure how long peak indicators remain visible
- **Hardware**: Serial port, protocol, and dial assignments

## Architecture

```
AudioVUMeter/
├── AudioVUMeterApp.swift    # App entry point
├── ContentView.swift        # Main UI layout
├── VUMeterView.swift        # VU meter components
├── AudioEngine.swift        # Core Audio capture engine
├── SystemMonitor.swift      # System resource monitoring
├── SerialManager.swift      # USB/Serial communication
├── HardwareView.swift       # Hardware configuration UI
├── SettingsView.swift       # Settings window
└── Assets.xcassets/         # App icons and colors
```

### Key Components

- **AudioEngine**: Uses AVAudioEngine to capture audio from the selected input device, calculates RMS levels, and converts to dB
- **SystemMonitor**: Uses Mach kernel APIs to retrieve CPU, memory, disk, and network statistics
- **SerialManager**: Handles USB/Serial communication with physical VU meter hardware
- **VUMeterView**: SwiftUI views for classic vertical VU meters with segment-based display
- **SystemMeterView**: Circular gauge components for system metrics

## Hardware Protocol Reference

### Raw Bytes Protocol
```
Start: 0xAA
Data:  [Dial1] [Dial2] [Dial3] [Dial4]  (0-255 each)
End:   0x55
```

### Text Command Protocol
```
CH1:128;CH2:64;CH3:200;CH4:32\n
```

### JSON Protocol
```json
{"dials":[128,64,200,32]}
```

### VU-Server Compatible Protocol
```
#0:50
#1:75
#2:30
#3:90
```
Values are percentages (0-100)

## BlackHole Setup Guide

1. **Install BlackHole**: Download and install from [existential.audio](https://existential.audio/blackhole/)

2. **Create Multi-Output Device**:
   - Open Audio MIDI Setup (Applications -> Utilities)
   - Click the `+` button -> Create Multi-Output Device
   - Check both your speakers and BlackHole
   - Set as default output

3. **Route Audio**:
   - System audio will now go to both speakers and BlackHole
   - Audio VU Meter captures from BlackHole input

## Compatible Hardware

This app is designed to work with:
- [VU Dials by Sasa Karanovic](https://github.com/SasaKaranovic/VU-Server)
- Arduino-based VU meters with serial interface
- Any USB/Serial device accepting the supported protocols

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

### SerialManager

```swift
// Connection
serialManager.connect()
serialManager.disconnect()

// Configuration
serialManager.selectedPortPath = "/dev/cu.usbserial-XXX"
serialManager.baudRate = 115200
serialManager.selectedProtocol = .vuServer

// Dial assignment
serialManager.dialConfigs[0].dialChannel = .audioLeft
serialManager.dialConfigs[1].dialChannel = .audioRight
serialManager.dialConfigs[2].dialChannel = .cpu
serialManager.dialConfigs[3].dialChannel = .ram
```

## License

MIT License - See LICENSE file for details.

## Credits

Inspired by [VU-Server](https://github.com/SasaKaranovic/VU-Server) by Sasa Karanovic.
