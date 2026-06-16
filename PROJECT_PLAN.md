# Tarot Project Plan

Last updated: 2026-06-16

## Project Vision

This project is a 2D tarot card game focused on emotional value, ritual-like card drawing, and a polished modern visual experience. The game should feel clean, premium, minimal, and dark-toned, while leaving room for players to customize decks, card backs, and backgrounds later.

The long-term goal is to release on Steam with Windows and macOS support, multilingual content, local saves, and future Steam Workshop support.

## Confirmed Direction

- Engine: Unity with C#.
- Rendering target: 2D game using a visual pipeline suitable for particles, glow, soft animation, and polished card effects.
- Platforms: Windows and macOS.
- Visual style: modern minimal, clean, premium, dark background.
- Runtime AI: not used. The game must work offline.
- Text content: tarot interpretations are generated or written during development, reviewed, and shipped as local game data.
- GitHub: private repository, with major phase commits.

## First Playable Demo Scope

The first demo should be small enough to finish, but large enough to prove the core architecture.

Included:

- Full 78-card tarot deck data.
- Upright and reversed card states.
- Daily reading mode.
- Three-card reading mode.
- Question input before non-daily readings.
- Local reading journal.
- Chinese and English localization.
- Mouse drag interaction.
- Mouse wheel browsing.
- Original placeholder visuals in the target dark minimal style.

Not included in the first demo:

- Runtime AI generation.
- Real Steam Workshop integration.
- Steam Cloud integration.
- Final commercial card art.
- Full set of advanced spreads such as Celtic Cross.
- Camera-based gesture recognition.

These should be supported by architecture where practical, but not built before the first demo proves the core experience.

## Core Gameplay

Players choose a reading mode, interact with a spread or card deck, draw cards, view a themed interpretation, and optionally save the result to their local journal.

Daily reading:

- No question required.
- Draws one card.
- Interpretation focuses on the day: reminder, mood, opportunity, risk, and gentle advice.

Three-card reading:

- Player enters a question before drawing.
- Draws three cards.
- Interpretation should be adapted to the three-card structure, not just three unrelated card meanings.

Future reading modes:

- Celtic Cross.
- Love and relationship readings.
- Career readings.
- Money readings.
- Choice and decision readings.
- Custom Workshop spreads.

## Interaction Design

The first demo uses mouse drag and mouse wheel input.

Future gesture input must not require rewriting the draw system. Input should be designed around shared actions such as:

- Browse cards.
- Highlight card.
- Select card.
- Cancel selection.
- Confirm reading.

Drag, wheel, and future gestures should all feed into the same card selection flow.

## Content and Interpretation

All tarot interpretation content is local and offline.

Each card should eventually include:

- Stable card ID.
- Chinese name.
- English name.
- Major/minor arcana category.
- Suit, when applicable.
- Number or rank.
- Upright keywords.
- Reversed keywords.
- Daily reading text.
- Three-card reading text.
- Gentle advice text.

Future modes may add mode-specific interpretation fields, for example love, career, money, and decision-making.

## Localization

The first demo supports:

- Simplified Chinese.
- English.

All user-facing text should come from localization data, including UI labels, card names, keywords, reading prompts, and interpretation text.

Future languages may include:

- Traditional Chinese.
- Japanese.
- Korean.
- French.
- German.
- Spanish.

## Journal System

After a reading, the player may save the result locally.

Each journal entry should store:

- Date and time.
- Reading mode.
- Question, when applicable.
- Drawn cards.
- Upright/reversed state.
- Final interpretation text.
- Selected deck, card back, and background identifiers.

The first version uses local saves. Steam Cloud can be added later.

## Appearance and Workshop Readiness

The first demo includes one default dark minimal theme, one default card back, and placeholder card visuals.

The project should be structured so future content packs can provide:

- Deck art.
- Card backs.
- Backgrounds.
- Localization packs.
- Reading spreads.

Steam Workshop integration is not part of the first demo, but content loading should avoid hard assumptions that would block Workshop content later.

## Collaboration Rules

Important product, visual, and interaction decisions must be discussed and confirmed before implementation.

Examples that require confirmation:

- Main menu design.
- Card deck layout.
- Draw interaction.
- Flip animation.
- Particle and glow style.
- Three-card interpretation format.
- Journal fields and browsing behavior.

Examples that do not require confirmation:

- Script class names.
- Local variable names.
- Internal folder organization.
- Button binding implementation.
- Minor refactors that do not change behavior.

## Version Control

Use Git locally and push major phase commits to a private GitHub repository.

Commit style examples:

- `docs: add initial planning docs`
- `chore: initialize unity project`
- `feat: add daily reading prototype`
- `feat: add local journal system`

Unity `.meta` files must be committed. They must not be deleted or ignored.

