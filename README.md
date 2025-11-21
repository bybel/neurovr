# NeuroReachVR

A VR rehabilitation application for fine motor control training, designed for neurological rehabilitation (stroke recovery, tremor management, motor impairments). Built with Unity for Meta Quest VR headsets.

## 📋 Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Requirements](#requirements)
- [Setup Instructions](#setup-instructions)
- [Scene Configuration](#scene-configuration)
- [Building for Quest](#building-for-quest)
- [Usage Guide](#usage-guide)
- [Project Structure](#project-structure)
- [Configuration](#configuration)
- [Development](#development)
- [Troubleshooting](#troubleshooting)

## 🎯 Overview

NeuroReachVR is a VR-based rehabilitation platform that provides interactive exercises for fine motor control training. The application features adaptive difficulty, comprehensive data logging, and multimodal feedback to support patient recovery and therapist monitoring.

**Target Platform**: Meta Quest (Quest, Quest 2, Quest Pro, Quest 3, Quest 3S)  
**Application ID**: `com.elsiga.neuroreachvr`  
**Unity Version**: 2022+ (URP 17.2.0)

## ✨ Features

### Rehabilitation Tasks
- **Balloon Pop Task**: Reach-and-grasp training using hand tracking with pinch detection
- **Path Tracing Task**: Fine motor control exercises with stylus input (Line, Curve, Circle, Square paths)
- **Spiral Tracing Task**: Advanced path tracing with angular velocity tracking and progressive difficulty

### Core Systems
- **Adaptive Difficulty**: Rule-based system with hysteresis to prevent rapid difficulty changes
- **Data Logging**: Comprehensive CSV logging with kinematic time-series data (60Hz sampling)
- **Multimodal Feedback**: Haptic, visual, and audio feedback for success/error states
- **Patient Management**: Session tracking, patient data persistence, CSV export
- **Input Support**: Hand tracking (XR Hands + legacy fallback) and stylus input

### Technical Features
- Object pooling for performance optimization
- Async data logging to prevent frame drops
- Real-time accuracy tracking and visual feedback
- Movement smoothness calculation
- Tremor analysis capabilities

## 📦 Requirements

### Development Environment
- **Unity**: 2022.3 LTS or newer
- **Unity Packages**:
  - Universal Render Pipeline (URP) 17.2.0
  - Meta XR SDK 78.0.0
  - Unity XR Hands 1.7.1
  - Unity OpenXR 1.16.0
  - Unity Input System 1.14.2
  - TextMesh Pro 2.0.0

### Hardware
- **VR Headset**: Meta Quest (Quest, Quest 2, Quest Pro, Quest 3, Quest 3S)
- **Development PC**: Windows 10/11 or macOS
- **Android SDK**: Minimum SDK 32, Target SDK 32

### Software
- Android SDK (for Quest builds)
- Meta Quest Developer Hub (for deployment)
- ADB (Android Debug Bridge) for device connection

## 🚀 Setup Instructions

### 1. Clone/Download Project

```bash
git clone <repository-url>
cd NeuroVR
```

### 2. Open in Unity

1. Launch Unity Hub
2. Open the project folder (`NeuroVR`)
3. Unity will import packages automatically (may take several minutes)

### 3. Verify Package Installation

Ensure the following packages are installed (check `Packages/manifest.json`):
- `com.meta.xr.sdk.all` (version 78.0.0)
- `com.unity.xr.hands` (version 1.7.1)
- `com.unity.xr.openxr` (version 1.16.0)
- `com.unity.inputsystem` (version 1.14.2)
- `com.unity.render-pipelines.universal` (version 17.2.0)

### 4. Configure Build Settings

1. Go to **File → Build Settings**
2. Select **Android** platform
3. Click **Switch Platform** (if not already on Android)
4. Ensure **OpenXR** is selected as the XR Plugin Provider:
   - Go to **Edit → Project Settings → XR Plug-in Management → OpenXR**
   - Enable OpenXR and configure for Quest

### 5. Configure Oculus Settings

1. Go to **Edit → Project Settings → XR Plug-in Management → Oculus**
2. Verify hand tracking is enabled
3. Check **Assets/Oculus/OculusProjectConfig.asset** settings

## 🎮 Scene Configuration

### Quick Setup (Recommended)

Use the built-in Scene Setup Helper:

1. Open your scene in Unity
2. Go to **NeuroReachVR → Scene Setup Helper** in the menu bar
3. Review component status (green checkmarks = present, orange X = missing)
4. Click **"Create Core Systems GameObject"** to auto-create missing components
5. Follow the dialog instructions to complete setup

### Manual Setup

#### Required Components

Your scene must contain these components:

1. **GameManager** (`NeuroReachVR.Core.GameManager`)
   - Central orchestrator for the application
   - Assign references to: InputHandler, AdaptiveDifficultyController, DataLogger, KinematicDataCollector, MultimodalFeedback, Tasks, HUDManager

2. **InputHandler** (`NeuroReachVR.Input.InputHandler`)
   - Handles all input sources (hand tracking, stylus)
   - Assign references to: HandTrackingXRHands (left/right), HandTrackingManager (legacy), StylusInputManager

3. **HUDManager** (`HUDManager`)
   - Manages UI menus and navigation
   - Assign references to all menu GameObjects and buttons

4. **DataLogger** (`NeuroReachVR.Data.DataLogger`)
   - Handles CSV data logging
   - Configure logging settings in Inspector

5. **KinematicDataCollector** (`NeuroReachVR.Data.KinematicDataCollector`)
   - Collects movement data at 60Hz
   - Automatically finds InputHandler

6. **AdaptiveDifficultyController** (`NeuroReachVR.Core.AdaptiveDifficultyController`)
   - Manages adaptive difficulty
   - Assign DifficultyProfile ScriptableObjects (Easy, Medium, Hard)

7. **MultimodalFeedback** (`NeuroReachVR.Feedback.MultimodalFeedback`)
   - Coordinates all feedback types
   - Assign references to: HapticFeedbackManager, VisualFeedbackManager, AudioFeedbackManager

8. **Task Components**
   - **BalloonPopTask**: Assign balloon prefab, configure spawn settings
   - **PathTracingTask**: Assign TraceablePath prefab, configure path settings
   - **SpiralTracingTask**: Inherits from PathTracingTask, configure spiral parameters

#### Scene Setup Steps

1. **Create Core Systems GameObject**:
   ```
   [Core Systems]
   ├── GameManager
   ├── DataLogger
   ├── KinematicDataCollector
   ├── AdaptiveDifficultyController
   └── MultimodalFeedback
   ```

2. **Create Input GameObject**:
   ```
   [Input Systems]
   ├── InputHandler
   ├── HandTrackingXRHands (Left)
   ├── HandTrackingXRHands (Right)
   └── StylusInputManager (optional)
   ```

3. **Create UI GameObject**:
   ```
   [UI]
   └── HUDManager
   ```

4. **Create Task GameObjects**:
   ```
   [Tasks]
   ├── BalloonPopTask
   ├── PathTracingTask
   └── SpiralTracingTask
   ```

5. **Assign References**:
   - In GameManager Inspector, assign all system references
   - In InputHandler Inspector, assign hand tracking components
   - In HUDManager Inspector, assign all menu GameObjects and buttons
   - In AdaptiveDifficultyController, assign DifficultyProfile assets

6. **Create Difficulty Profiles**:
   - Create ScriptableObject assets for Easy, Medium, Hard difficulty
   - Configure parameters for each task type
   - Assign to AdaptiveDifficultyController

### Verify Setup

Use the **Pre-Deployment Verification** tool:
1. Go to **NeuroReachVR → Pre-Deployment Checklist**
2. Review all checks
3. Fix any issues highlighted in red

## 📱 Building for Quest

### Prerequisites

1. **Enable Developer Mode** on your Quest headset:
   - Open Oculus app on phone
   - Go to Settings → Developer Mode
   - Enable Developer Mode

2. **Connect Quest to PC**:
   - Use USB-C cable
   - Allow USB debugging when prompted on headset

3. **Verify ADB Connection**:
   ```bash
   adb devices
   ```
   Should show your device listed

### Build Steps

1. **Configure Build Settings**:
   - File → Build Settings
   - Platform: Android
   - Architecture: ARM64
   - Build System: Gradle (recommended)

2. **Player Settings**:
   - Company Name: `DefaultCompany` (or your company)
   - Product Name: `NeuroReachVR`
   - Package Name: `com.elsiga.neuroreachvr`
   - Minimum API Level: 32
   - Target API Level: 32

3. **Build APK**:
   - Click **Build** (or Build and Run)
   - Choose output directory
   - Wait for build to complete

4. **Deploy to Quest**:
   - If using "Build and Run", deployment is automatic
   - Otherwise, use ADB:
     ```bash
     adb install -r path/to/your.apk
     ```

### Alternative: Build via Command Line

```bash
Unity -batchmode -quit -projectPath . -buildTarget Android -buildPath ./Builds/NeuroVR.apk
```

## 🎯 Usage Guide

### For Patients

1. **Launch Application**: Start NeuroReachVR on Quest headset
2. **Login**: Enter Patient ID when prompted
3. **Select Task**: Choose from Balloon Pop, Path Tracing, or Spiral Tracing
4. **Select Difficulty**: Choose Easy, Medium, or Hard (or let adaptive system choose)
5. **Start Trial**: Begin the exercise
6. **Complete Exercise**: Follow on-screen instructions
7. **View Results**: Review score and progress

### For Therapists/Researchers

1. **Patient Management**: Set patient ID before starting session
2. **Data Export**: CSV files are saved to device storage
   - Location: `Application.persistentDataPath` (typically `/sdcard/Android/data/com.elsiga.neuroreachvr/files/`)
3. **Review Data**: Export CSV files contain:
   - Task attempts with timestamps
   - Performance metrics (accuracy, completion time)
   - Kinematic time-series data (position, velocity, acceleration)
   - Adaptive difficulty adjustments

### Input Methods

- **Hand Tracking**: Use pinch gesture to interact (Balloon Pop task)
- **Stylus**: Use stylus for precise tracing (Path/Spiral Tracing tasks)
- **Auto-Detection**: System automatically selects best available input method

## 📁 Project Structure

```
NeuroVR/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/              # Core game systems
│   │   │   ├── GameManager.cs
│   │   │   ├── AdaptiveDifficultyController.cs
│   │   │   ├── PatientDataManager.cs
│   │   │   └── ServiceLocator.cs
│   │   ├── Tasks/             # Rehabilitation tasks
│   │   │   ├── BaseTask.cs
│   │   │   ├── BalloonPopTask.cs
│   │   │   ├── PathTracingTask.cs
│   │   │   └── SpiralTracingTask.cs
│   │   ├── Input/             # Input handling
│   │   │   ├── InputHandler.cs
│   │   │   ├── HandTrackingXRHands.cs
│   │   │   └── StylusInputManager.cs
│   │   ├── Data/              # Data collection
│   │   │   ├── DataLogger.cs
│   │   │   └── KinematicDataCollector.cs
│   │   ├── Feedback/           # Feedback systems
│   │   │   ├── MultimodalFeedback.cs
│   │   │   ├── HapticFeedbackManager.cs
│   │   │   ├── VisualFeedbackManager.cs
│   │   │   └── AudioFeedbackManager.cs
│   │   ├── UI/                 # User interface
│   │   │   ├── HUDManager.cs
│   │   │   ├── MenuManager.cs
│   │   │   └── AccessibleUI.cs
│   │   ├── Utils/              # Utilities
│   │   │   ├── NeuroVRConstants.cs
│   │   │   └── ValidationHelper.cs
│   │   └── Editor/             # Editor tools
│   │       ├── SceneSetupHelper.cs
│   │       └── PreDeploymentVerification.cs
│   ├── Prefabs/                # Prefab assets
│   ├── Resources/              # Resource assets
│   └── StreamingAssets/        # Streaming assets
├── ProjectSettings/            # Unity project settings
├── Packages/                   # Package manifests
└── README.md                   # This file
```

## ⚙️ Configuration

### Difficulty Profiles

Create ScriptableObject assets for difficulty levels:

**Easy Profile**:
- Balloon Pop: Spawn distance 1.5m, Rate 1.0/s
- Path Tracing: Width 0.15m, Accuracy 0.6
- Spiral: Tightness 0.8, Angular velocity 0.3-1.5 rad/s

**Medium Profile**:
- Balloon Pop: Spawn distance 2.0m, Rate 1.5/s
- Path Tracing: Width 0.10m, Accuracy 0.7
- Spiral: Tightness 1.0, Angular velocity 0.5-2.0 rad/s

**Hard Profile**:
- Balloon Pop: Spawn distance 2.5m, Rate 2.0/s
- Path Tracing: Width 0.08m, Accuracy 0.8
- Spiral: Tightness 1.2, Angular velocity 0.8-2.5 rad/s

### Data Logging Settings

Configure in `DataLogger` component:
- **Logging Enabled**: Toggle data logging on/off
- **File Name**: Base name for CSV files
- **Append Timestamp**: Add timestamp to filename
- **Max File Size**: 10MB (auto-rotates)

### Input Settings

Configure in `InputHandler` component:
- **Preferred Mode**: Auto, Hand, or Stylus
- **Preferred Hand**: Left or Right
- **Use XR Hands Package**: Enable for improved hand tracking

## 🛠️ Development

### Code Organization

- **Namespaces**: All code uses `NeuroReachVR.*` namespace structure
- **Design Patterns**: Service Locator, Strategy, Observer, Object Pooling
- **Architecture**: Component-based with clear separation of concerns

### Adding New Tasks

1. Create new class inheriting from `BaseTask`
2. Implement abstract methods:
   - `UpdateTask()`: Main task logic
   - `OnTaskStarted()`: Initialization
   - `OnTaskEnded()`: Cleanup
3. Add task type to `TaskType` enum in `GameManager.cs`
4. Register in `GameManager.GetTask()` method
5. Create UI button in HUDManager

### Testing

Use Unity Test Runner:
- Tests should be in `Assets/Scripts/Tests/` folder
- Use Unity Test Framework package (already included)

### Debugging

- Enable debug logging in `HUDManager` (extensive logging available)
- Use Unity Profiler for performance analysis
- Check Console for warnings/errors

## 🐛 Troubleshooting

### Build Issues

**Problem**: Build fails with XR errors
- **Solution**: Verify OpenXR is enabled in Project Settings → XR Plug-in Management

**Problem**: APK installs but crashes on Quest
- **Solution**: Check Android Logcat for errors: `adb logcat | grep Unity`

### Runtime Issues

**Problem**: Hand tracking not working
- **Solution**: 
  - Verify hand tracking is enabled in Oculus settings
  - Check InputHandler has HandTrackingXRHands components assigned
  - Try legacy HandTrackingManager as fallback

**Problem**: Tasks not starting
- **Solution**:
  - Verify GameManager has all task references assigned
  - Check HUDManager has all button references
  - Review Console for null reference errors

**Problem**: Data not logging
- **Solution**:
  - Check DataLogger has `loggingEnabled` checked
  - Verify write permissions on device
  - Check file path: `Application.persistentDataPath`

### Performance Issues

**Problem**: Frame drops during tasks
- **Solution**:
  - Reduce kinematic sample rate (default 60Hz)
  - Disable real-time visual feedback in TraceablePath
  - Reduce max balloons in BalloonPopTask

## 📄 License

[Add your license information here]

## 👥 Credits

**Developed by**: [Your Name/Organization]  
**Organization**: elsiga  
**Version**: 1.0.0

## 📞 Support

For issues, questions, or contributions:
- [GitHub Issues](link-to-issues)
- [Email](your-email@example.com)

---

**Last Updated**: 2025-01-29

