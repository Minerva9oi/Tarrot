# Tarot Progress

Last updated: 2026-06-16

## Current Phase

Unity project foundation completed. Next phase is first visual and main menu confirmation.

## Completed

- Confirmed the game direction: 2D tarot game focused on emotional value and polished card drawing.
- Confirmed the visual style: modern minimal, clean, premium, dark-toned.
- Confirmed the engine direction: Unity with C#.
- Confirmed target platforms: Windows and macOS.
- Confirmed first demo scope:
  - Full 78-card tarot deck.
  - Daily reading.
  - Three-card reading.
  - Upright and reversed cards.
  - Local reading journal.
  - Chinese and English localization.
  - Mouse drag and wheel interaction.
- Confirmed runtime AI will not be used.
- Confirmed GitHub should be private and use major phase commits.
- Created the initial project documentation and Unity Git ignore rules.
- Connected the private GitHub repository.
- Initialized the Unity project structure.
- Added the first system skeletons and boot scene.

## Next Tasks

- Confirm the first visual direction and main menu design before UI implementation.
- Resolve the best URP setup path for Unity `6000.4.11f1` or choose a more stable Unity version/template if needed.
- Define the first demo's concrete card data format for the full 78-card deck.
- Build the first visual prototype after design confirmation.

## Working Agreement

Important product, visual, and interaction decisions are confirmed before implementation. Small engineering details are handled during development without blocking the flow.

## Change Log

### 2026-06-16

- Added project plan.
- Added progress log.
- Added decision log.
- Added Unity-specific `.gitignore`.
- Prepared the repository for local Git initialization and future GitHub remote setup.

### 2026-06-16 - Unity Foundation

- Created the Unity project in the repository root using Unity `6000.4.11f1`.
- Added the initial Unity folders:
  - `Assets/Scripts`
  - `Assets/Scenes`
  - `Assets/Data`
  - `Assets/Prefabs`
  - `Assets/Art`
  - `Assets/Localization`
- Added system skeletons for cards, readings, journal entries, localization, input, appearance themes, and game bootstrap.
- Created `Assets/Scenes/Boot.unity` and registered it in Unity Build Settings.
- Verified Unity compiles the project successfully in batch mode.
- Verified Unity generated `.meta` files and they are not ignored by Git.
- Deferred URP activation to the visual phase because Unity `6000.4.11f1` reported package-level compile errors when URP was added directly through `manifest.json` in batch mode. The project remains stable and compilable without URP for this foundation stage.
