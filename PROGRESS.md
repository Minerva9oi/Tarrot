# Tarot Progress

Last updated: 2026-06-17

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

### 2026-06-16 - Direct Card Selection Ring

- Changed daily reading selection so the player must click a visible card directly.
- Removed automatic selection of the center card.
- Expanded the visible card ring horizontally so it spans across the screen.
- Increased ring-card size while keeping a consistent card scale across the visible ring.
- Removed center-card scale emphasis; middle cards no longer become larger than surrounding cards.
- Continued local tuning by making ring cards larger, reducing visible card count, and increasing card gaps.

### 2026-06-16 - Default Real Card Fronts

- Added the default Rider-Waite-Smith card-front images under `Assets/Art/CardDecks/Default/Fronts`.
- Confirmed the source set contains 78 front images and no matching card-back image.
- Added a `CardDeckArtData` resource so card-front sprites are mapped by tarot card ID instead of hard-coded in the reading scene.
- Updated the daily reading reveal so selected cards flip to their real front artwork.
- Kept the generated placeholder card back until a dedicated card-back design is chosen.

### 2026-06-17 - Daily Reading Visual and Text Tuning

- Enlarged the daily reading card ring again and limited the visible cards to at most seven.
- Increased card spacing by using a larger ring radius with a narrower visible arc.
- Removed the `每日运势` title and instruction text from the reading scene to reduce visual interruption.
- Replaced the repeated placeholder daily advice with local rule-based advice that varies by card, suit, number, and orientation.
- Softened the card-ring scale after review and adjusted the visible arc to show at most eight cards.
- Reduced the card-ring scale again and moved the ring downward to keep clear space from the top screen edge.

### 2026-06-17 - Shared Responsive Draw Layout

- Added a shared card draw layout profile for reusable deck-ring sizing, visible arc, and selected-card placement.
- Updated the daily reading card ring to calculate its radius and selected-card anchor from camera viewport proportions so it adapts across window sizes.
- Preserved the current card visual scale while aligning the upper deck arc and drawn-card position closer to the Unity reference photo.
- Added a public layout setter so future reading modes can reuse or override the same draw-deck proportions.
- Reduced the daily reading result text size so the explanation matches the reference card-to-text proportion more closely.
- Matched the selected and flipped card scale to the ring-card scale so the card does not visually grow after being drawn.
- Thinned the generated card-back border and center motif so the back design reads slimmer without changing the card's actual aspect ratio.
- Increased the default starfield star size multiplier so the background stars read more clearly during card drawing.
- Enlarged the shared draw-layout card scale while increasing the ring radius and keeping the visible arc unchanged so the same number of cards remains visible.
- Tuned the shared draw-layout scale and visible arc back toward a 7-8 card presentation after reviewing the in-game card size.
- Retuned the shared draw-layout proportions toward the reference photo, with slimmer card scale, a wider upper arc, and matching selected-card size.
- Restyled the daily reading result text so the card name is single-language, lightly tracked, bolded, and positioned closer beneath the selected card.
- Increased the result card-name and body text sizes so the name better matches the card-face title scale while preserving hierarchy.

### 2026-06-17 - Shared Card Draw System Preservation

- Extracted the current arc-shaped card deck interaction into a reusable shared draw controller.
- Preserved scroll rotation, drag rotation, click-to-select, responsive arc layout, and selected-card metadata for future spread modes.
- Updated daily reading to consume the shared draw controller while keeping its own result text and reveal flow.
- Kept the current draw-layout profile as the reusable baseline for future `牌阵占卜` modes.

### 2026-06-17 - Immersive Daily Draw Deck

- Added a daily-only immersive deck controller that presents the player as standing inside a circular 78-card deck.
- Kept the existing shared card draw controller unchanged for future `牌阵占卜` modes.
- Replaced daily reading's shared deck usage with the new 2.5D pseudo-perspective deck.
- Preserved mouse wheel rotation, horizontal drag rotation, and direct click-to-select behavior.
- Added a reveal flow where visible cards dissolve into star particles and streams that gather into the selected card face.
- Kept the default starfield background event hooks active during the reveal.

### 2026-06-17 - Daily Reading P1 Wrap Branch

- Created `codex/daily-p1-wrap-wind-dissolve` from `main` for visual review.
- Tuned the daily-only immersive deck toward a wider, closer p1-style half ring.
- Reworked the daily reveal so the selected card pulls to screen center and flips larger.
- Changed unselected visible cards from gathered star streams to dense wind-blown star-dust dissolution.
- Retuned the p1 branch away from an external fan shape toward an inside-the-card-circle perspective, with side cards feeling closer while preserving card gaps.
- Enlarged the daily deck cards, moved the visible deck band closer to screen center, and projected the visible cards along a longer-radius circle so larger cards keep clear spacing.
- Increased the long-radius daily deck spacing again and added subtle trapezoid deformation only to the edge cards to imply a surrounding circle without exaggerating perspective.
- Enlarged the selected daily result card again and scaled the result text up with a slightly larger, lower text area to preserve the card-to-reading proportion.
- Moved the daily result text closer to the selected card, lightened the card name by removing bold styling, and reduced the orientation label size.
- Nudged the daily result text upward again to sit closer beneath the revealed card.
