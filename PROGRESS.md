# Tarot Progress

Last updated: 2026-06-16

## Current Phase

Main menu and default starfield background prototype completed. Next phase is visual review and refinement, then reading-mode selection flow.

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
- Added the first main menu prototype.
- Added the default starfield background prototype.

## Next Tasks

- Review the main menu and starfield prototype in Unity.
- Refine visual spacing, typography, and star density based on review.
- Resolve the best URP setup path later if built-in rendering is not enough for the desired card reveal effects.
- Define the first demo's concrete card data format for the full 78-card deck.
- Confirm the reading-mode selection flow for `牌阵占卜`.

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

### 2026-06-16 - Main Menu and Default Starfield

- Added `Tarot` as the first title screen name.
- Added main menu entries:
  - `每日运势`
  - `牌阵占卜`
  - `占卜日记`
  - `设置`
  - `退出`
- Added a default black starfield background with soft circular stars and subtle breathing animation.
- Added a replaceable background architecture:
  - `BackgroundThemeData`
  - `BackgroundManager`
  - `IReadingEffectBackground`
  - `ReadingBackgroundState`
- Added reading-effect states for future card reveal effects:
  - `Idle`
  - `Awakened`
  - `Gathering`
  - `Restoring`
- Added `DefaultStarfieldBackground` as the default advanced background that can later participate in card reveal effects.
- Added UGUI as an explicit Unity package for menu UI.
- Verified Unity compiles successfully in batch mode.

### 2026-06-16 - Starfield Visibility Fix

- Fixed the Boot scene camera position for 2D rendering by moving the camera to `z = -10`.
- Increased default starfield visibility with slightly larger, brighter, and denser soft stars.
- Kept the background architecture unchanged so the starfield remains a replaceable default theme.

### 2026-06-16 - Joshua Tree Starfield Tuning

- Tuned the default starfield toward a clear, quiet Joshua Tree night sky reference.
- Reduced overall star count to avoid dense clusters.
- Added minimum spacing attempts so stars distribute more naturally.
- Added star tiers so a small number of stars appear brighter while most remain subtle.
- Slowed the breathing effect so it feels faint and atmospheric instead of decorative.

### 2026-06-16 - Constellation-Based Starfield

- Reworked the default starfield from fully random placement to a deterministic wide-sky composition.
- Added recognizable Taurus anchors, including the Pleiades cluster, Hyades shape, Aldebaran, and horn stars.
- Added recognizable Sagittarius anchors using the Teapot asterism.
- Reduced background star count so constellation anchors read more clearly.
- Sharpened the star sprite by reducing the soft halo and keeping a clearer bright core.
- Increased breathing strength while keeping the motion slow and quiet.

### 2026-06-16 - Starfield Brightness Tuning

- Increased default star brightness while keeping the same constellation layout.
- Slightly lifted cool and warm star colors.
- Increased subtle background star alpha so the sky reads more clearly in the Game view.

### 2026-06-16 - Daily Reading Ring Prototype

- Added a full 78-card runtime tarot deck shared by future reading modes.
- Connected the `每日运势` main menu button to a playable daily reading prototype.
- Added an upper-screen circular card ring where all cards point toward the circle center.
- Limited the visible ring to the upper arc, using roughly the top 35% of the screen.
- Added mouse wheel and drag rotation for browsing the deck.
- Added focused-card selection at the center of the visible arc.
- Kept the card ring visible after selection and moved the selected card to the lower center.
- Added a prototype 2D flip reveal with upright/reversed result text.
- Verified the project compiles successfully in Unity batch mode.

### 2026-06-16 - Daily Reading Ring Spacing Tuning

- Reduced the visible card-ring arc so fewer cards appear at once.
- Increased card spacing by using a wider ring and smaller ring-card scale.
- Moved the visible card ring downward so the top edge has more breathing room.
- Kept the selected card moving to the lower center and preserved a readable selected-card size.
- Confirmed the future card-art import should use the same shared 78-card deck.
