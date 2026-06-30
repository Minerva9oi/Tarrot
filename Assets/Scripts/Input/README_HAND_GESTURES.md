# Hand Gesture Input

`CameraGestureInputSource` no longer uses frame-difference motion detection. The default provider is `MediaPipeHandLandmarkProvider`, which runs the MediaPipe Hand Landmarker task from the camera and supplies 21 hand landmarks.

Package dependency:

- `Packages/manifest.json` references `com.github.homuler.mediapipe` v0.16.3 from the official release `.tgz`.
- In the editor, the provider prepares `hand_landmarker.bytes` through MediaPipe's `LocalResourceManager`.
- In player builds, copy MediaPipe model assets to `StreamingAssets/MediaPipe` before shipping.

Landmark contract:

- The provider returns one valid `HandLandmarkFrame` per detected camera frame.
- Landmark positions are normalized camera coordinates in the same order as MediaPipe Hands:
  `0 wrist`, `4 thumb tip`, `8 index tip`, `12 middle tip`, `16 ring tip`, `20 pinky tip`.
- `CameraGestureInputSource` mirrors the x/y coordinates for player-facing screen control.

Runtime rules:

- Gesture toggle starts the provider.
- The player must calibrate by holding an open hand on the screen y-axis for 1 second.
- Open hand right of the calibrated axis rotates clockwise.
- Open hand left of the calibrated axis rotates counter-clockwise.
- Index finger pointing drives the soft circular gesture cursor and hovers cards.
- Three-finger pinch confirms the most recently hovered card within the short hover grace window.
- Open hand, index point, and pinch are separate phases to match the reference video.
