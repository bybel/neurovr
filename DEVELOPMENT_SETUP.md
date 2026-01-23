# Developer Setup & Quest Build (Quick Guide)

This file is a **lightweight setup guide** for building and running NeuroReach VR in **Unity** on **Meta Quest** devices. It complements the main README (project overview + tasks + metrics).

---

## 📦 Requirements (at a glance)

### Development environment
- **Unity:** 2022.3 LTS or newer  
- **Key Unity packages (typical)**
  - Universal Render Pipeline (**URP**) 17.2.0
  - Meta XR SDK 78.0.0
  - Unity XR Hands 1.7.1
  - Unity OpenXR 1.16.0
  - Unity Input System 1.14.2
  - TextMesh Pro 2.0.0

> Tip: Versions above are the ones the project targets. If your Unity version differs, align package versions in `Packages/manifest.json`.

### Hardware
- **VR headset:** Meta Quest (Quest / Quest 2 / Quest Pro / Quest 3 / Quest 3S)
- **Dev machine:** Windows 10/11 or macOS
- **Android SDK:** Minimum SDK 32, Target SDK 32

### Software
- Android SDK (Quest builds)
- Meta Quest Developer Hub (deployment)
- ADB (Android Debug Bridge) for device connection/logs

---

## 🚀 Setup instructions (quick)

### 1) Clone / download the project
```bash
git clone https://github.com/MixedRealityETHZ/NeuroReachVR.git
cd NeuroVR
```

### 2) Open in Unity
1. Launch **Unity Hub**
2. **Open** the project folder (`NeuroVR/`)
3. Let Unity import packages (first import can take a few minutes)

### 3) Verify packages are installed
Check `Packages/manifest.json` includes (names may vary by Unity/Meta XR versions):
- `com.meta.xr.sdk.all` (78.0.0)
- `com.unity.xr.hands` (1.7.1)
- `com.unity.xr.openxr` (1.16.0)
- `com.unity.inputsystem` (1.14.2)
- `com.unity.render-pipelines.universal` (17.2.0)

**Key takeaway:** if builds fail, mismatched OpenXR / Meta XR / URP versions are a common cause—fix those first.

### 4) Configure Android build settings
1. **File → Build Settings**
2. Select **Android** → **Switch Platform**
3. Confirm your project uses **OpenXR** for Quest:
   - **Edit → Project Settings → XR Plug-in Management → OpenXR**
   - Enable OpenXR and set the device profile/features for Quest (per your Meta XR setup)

### 5) Quest / Meta XR settings (hand tracking & permissions)
- Ensure hand tracking is enabled in the relevant XR settings.
- If your project contains an Oculus/Meta XR config asset (commonly under `Assets/.../OculusProjectConfig.asset`), confirm required flags (hand tracking, permissions, etc.).

---

## 🎮 Scene configuration (how the app is wired)

### Quick setup (recommended)
If the repository contains Editor tools like **Scene Setup Helper** and **Pre-Deployment Checklist**:
1. Open the Unity scene `(scene1)`
2. Use menu items (e.g., `NeuroReachVR → Scene Setup Helper`)
3. Let it create missing core objects and highlight missing references
4. Run the pre-deployment checklist (fix anything marked red)

**Key takeaway:** Use the helper/checklist to avoid “missing reference” runtime issues.

### Manual setup (high level)
Your scene typically needs four groups:

1. **Core systems** (orchestrates app, logging, feedback)
2. **Input systems** (hands + stylus)
3. **UI / HUD** (menus, navigation)
4. **Tasks** (BalloonPop, PathTrace, etc.)

A common hierarchy looks like:
```
[Core Systems]
[Input Systems]
[UI]
[Tasks]
```

If you follow a component-driven setup, you’ll usually:
- Add the manager components to these objects
- Assign cross-references in the Inspector (managers → tasks, input, UI, logger)
- Verify prefabs are assigned for task content (balloons, trace paths)

> Note: Exact class names/components can differ across iterations. Use **search** in the Project window to locate the relevant scripts and prefabs.

---

## 📱 Building for Quest

### Prerequisites
1. Enable **Developer Mode** on your Quest (Oculus mobile app → Settings → Developer Mode)
2. Connect headset via USB-C and accept **USB debugging**
3. Verify ADB:
```bash
adb devices
```

### Build steps (Unity UI)
1. **File → Build Settings**
   - Platform: Android
   - Architecture: **ARM64**
   - Build system: **Gradle** (recommended)
2. **Player Settings**
   - Package name: `com.<name>.neuroreachvr` (example)
   - Minimum API: 32, Target API: 32
3. Click **Build** (or **Build and Run**)

### Deploy via ADB (if not using Build and Run)
```bash
adb install -r path/to/your.apk
```

### Command line build (optional)
```bash
Unity -batchmode -quit -projectPath . -buildTarget Android -buildPath ./Builds/NeuroVR.apk
```

**Key takeaway:** Most deployment problems are either (a) Android SDK/NDK mismatch, (b) XR plugin config, or (c) missing permissions.

---

## 📁 Project structure (orientation)

Typical top-level layout:
```
NeuroVR/
├── Assets/
│   ├── Scripts/               # C# scripts (core, tasks, input, data, feedback, UI, editor)
│   ├── Prefabs/               # Prefab assets
│   ├── Resources/             # Resource assets
│   └── StreamingAssets/       # Streaming assets
├── ProjectSettings/           # Unity project settings
├── Packages/                  # Package manifests
└── README.md                  # Main project overview + tasks/metrics
```

**Key takeaway:** If you’re debugging setup issues, start with (a) `Packages/manifest.json`, (b) XR Plug-in Management settings, and (c) missing Inspector references in the scene.

---
