# Magic Leap 2 Eye Tracking Interaction Project

This Unity project facilitates advanced eye-gaze interaction for Magic Leap 2, offering customizable parameters for a target interactable sphere and multiple gaze-based interaction modes.

## Project Presentation
🎯 [View Interactive Slides](https://docs.google.com/presentation/d/e/2PACX-1vSAi8Zc74FJ02NzDNDfLjT8LAsPjZbxcAr5jSH4stKQpPJwolJWEQKDkOmvxr5pIxBwDXxM3m8qT_Qq/pub?start=true&loop=true&delayms=3000)

---

## Project Overview

- **Unity Version**: `2022.3.52f1`
- **Magic Leap SDK Version**: `2.5.0`
- **Magic Leap 2 OS Requirement**: `1.10.0` or later
- **Scenes for APK Builds**:
  - **Study_1 Scene**: `Study_1`
  - **Study_2 Scene**: `Study_2`

---

## Study_2 Scene Overview

- The **SceneManager** object allows adjustment of the cone angle and the interval between trials.
- Customizable scriptable objects for scene configurations are located in:
  ```
  Assets/Resources/SO
  ```

---

## Setup Instructions

### Device Preparation

1. **Update Device OS**  
   Ensure that your Magic Leap 2 device is running **OS version 1.10.0** or higher to support required features.

2. **Eye Tracking Calibration**  
   Calibrate the eye tracking system on the device before running the app. Refer to the [Magic Leap 2 documentation](https://developer-docs.magicleap.cloud/docs/guides/ml2-overview/) for detailed instructions.

---

### APK Installation and Debugging

1. **Install APK or Access Debug Files**  
   Use **Magic Leap Hub 3** to install the APK or download debug files. Download it [here](https://ml2-developer.magicleap.com/downloads).

2. **Debug File Locations**  
   - **General Debug File**:  
     ```
     /storage/emulated/0/Android/data/com.ColumbiaCGUI.MagicLeap2EyeTracking/files/debug_file.txt
     ```
   - **Study_2 Debug File**:  
     ```
     /storage/emulated/0/Android/data/com.ColumbiaCGUI.MagicLeap2EyeTracking/files/study_2_debug_file.txt
     ```
   - **Advanced_metrics Debug File**:   
     ```
     /storage/emulated/0/Android/data/com.ColumbiaCGUI.MagicLeap2EyeTracking/files/advanced_metrics.txt
     ```
---

## Application Overview

### UI Controls (Study_1)

- **Sliders**:
  - **R-radius**: Adjust the target sphere's radius.
  - **X, Y, Z Position**: Set the sphere's position along the respective axes.

- **Toggles**:
  - **Is Recording**: Enables recording of debug data.
  - **Gaze Sphere**: Displays a cyan sphere indicating gaze depth calculated by a custom model (used for interaction detection).
  - **ML Gaze Sphere**: Displays a yellow sphere representing gaze depth as calculated by the Magic Leap SDK.
  - **Ray Interaction**: Toggles interaction mode. When enabled, uses raycasting with combined gaze data directly from the Magic Leap SDK, bypassing the custom model.

---

### UI Controls (Study_2)

- **Buttons (A, B, C, D, E)**:
  - Configure the scene generation mode.
  - A new random scene is regenerated in the selected mode every **20 seconds** after **5 seconds** of constant gaze fixation on the target.
- **Scene Generation parameters**:
  - Overall scene depth range: 0.4–10 meters
  - Sphere size range across all scenes: 0.5–4 degrees of visual angle
  - *Scene-specific details*:
    - Scene A: Target depth range 0.4–2 m, with 2 additional distractors placed ±10 cm from the target depth.
    - Scene B: Target depth range 2–6 m, with 4 additional distractors placed ±30 cm from the target depth.
    - Scene C: Target depth range 6–10 m, with 2 additional distractors placed ±100 cm from the target depth.
    - Scene D: Target depth range 0.4–10 m, with 4 additional distractors placed from ±10 cm to ±100 cm from the target depth.
    - Scene E: Target depth range 0.4–10 m (without near distractors).

- **Important Notes**:
  - **Data Capturing**: 
  Starts automatically when the application is launched.
  - **Safe Exit**: Always exit the application using the **"Safe Exit"** button to ensure data is saved correctly in JSON format.

---

## Data Capturing
- To ensure the data is in the correct JSON format, follow these steps:

  - **1. Download the files.**
  - **2. Manually add a "[" at the very beginning of the file.**
  - **3. Remove the last comma.**
  - **4. Add "]" at the end.**
- Please note that after minimizing the app, it may take up to 30 seconds for the Magic Leap SDK to reinitialize the eye tracking (as eye tracking feature still is an experimental feature ML SDK has some bugs). To avoid this delay, it's better not to collapse the app. If you need to exit the app, use the **Safe Exit** button. When you run the app again, it will continue writing data to the files. You will only need to follow the steps for formatting the JSON data as mentioned.

**Important:** If the app was closed improperly and the streaming data stopped without a correct ending, you will need to either:

- Manually fix the structure of the JSON file, or
- Delete those files and restart the data collection process.

## Study_2 Debug JSON File Structure

The debug file contains trial details formatted as follows:

```json
{
  "StartTime": "string", // The start time of the trial in "dd.MM.yyyy HH:mm:ss" format.
  "SceneName": "string", // Name of the scene where the trial is conducted.
  "Target": {
    "Position": {
      "x": "float", // X-coordinate of the target position.
      "y": "float", // Y-coordinate of the target position.
      "z": "float"  // Z-coordinate of the target position.
    },
    "Diameter": "float" // Diameter of the target.
  },
  "Distractors": [
    {
      "Position": {
        "x": "float", // X-coordinate of the distractor position.
        "y": "float", // Y-coordinate of the distractor position.
        "z": "float"  // Z-coordinate of the distractor position.
      },
      "Diameter": "float" // Diameter of the distractor.
    }
  ],
  "GazeTracking": [
    {
      "TimeFromStartTrial": "float", // Time elapsed since the trial started (in seconds).
      "DepthError": "float", // Depth estimation error at this time point.
      "FixationStability": "float", // Stability of fixation at this time point.
      "PupilDiameterLeft": "float", // Diameter of the left pupil.
      "PupilDiameterRight": "float", // Diameter of the right pupil.
      "LeftEyePosition": {
        "x": "float", // X-coordinate of the left eye's position.
        "y": "float", // Y-coordinate of the left eye's position.
        "z": "float"  // Z-coordinate of the left eye's position.
      },
      "LeftEyeRotation": {
        "x": "float", // X-component of the left eye's rotation.
        "y": "float", // Y-component of the left eye's rotation.
        "z": "float"  // Z-component of the left eye's rotation.
      },
      "RightEyePosition": {
        "x": "float", // X-coordinate of the right eye's position.
        "y": "float", // Y-coordinate of the right eye's position.
        "z": "float"  // Z-coordinate of the right eye's position.
      },
      "RightEyeRotation": {
        "x": "float", // X-component of the right eye's rotation.
        "y": "float", // Y-component of the right eye's rotation.
        "z": "float"  // Z-component of the right eye's rotation.
      },
      "GazePosition": {
        "x": "float", // X-coordinate of the gaze position.
        "y": "float", // Y-coordinate of the gaze position.
        "z": "float"  // Z-coordinate of the gaze position.
      },
      "GazeRotation": {
        "x": "float", // X-component of the gaze rotation.
        "y": "float", // Y-component of the gaze rotation.
        "z": "float"  // Z-component of the gaze rotation.
      },
      "GazeFixationPosition": {
        "x": "float", // X-coordinate of the gaze fixation position.
        "y": "float", // Y-coordinate of the gaze fixation position.
        "z": "float"  // Z-coordinate of the gaze fixation position.
      },
      "GazeFixationDiameter": "float", // Diameter of the gaze fixation area.
      "IsGazeCollideWithTarget": "boolean", // Whether the gaze collides with the target.
      "MLGazeFixationPosition": {
        "x": "float", // X-coordinate of the ML gaze fixation position.
        "y": "float", // Y-coordinate of the ML gaze fixation position.
        "z": "float"  // Z-coordinate of the ML gaze fixation position.
      },
      "MLGazeFixationDiameter": "float", // Diameter of the ML gaze fixation area.
      "IsMLGazeCollideWithTarget": "boolean" // Whether the ML gaze collides with the target.
    }
  ]
}
```

## Advanced_metrics Debug JSON File Structure

```json
{
  "StartTime": "string",
  "SceneName": "string",
  "Metrics": [
    {
      "TimeFromStart": "float",
      "DepthError": "float",
      "FixationStability": "float",
      "VergenceAngleStability": "float",
      "TimeToStableFixation": "float",
      "PupilDiameterLeft": "float",
      "PupilDiameterRight": "float",
      "SaccadeCount": "integer"
    }
  ]
}
```
