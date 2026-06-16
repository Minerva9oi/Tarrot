# Tarot Decisions

This file records long-term decisions that affect project direction. It should not be used for tiny UI adjustments, implementation details, or daily progress notes.

## 2026-06-16 - Use Unity as the Game Engine

Reason:

- The project prioritizes visual polish, animation, particles, glow, and future commercial game production.
- Unity has strong tooling for 2D games, effects, build pipelines, and Steam-related workflows.

Rejected:

- Godot 4 as the first engine.
- Web or Electron as the main game runtime.

Impact:

- Main development language is C#.
- Project structure must follow Unity conventions.
- Unity licensing should be reviewed again before Steam release.

## 2026-06-16 - Support Windows and macOS

Reason:

- The game should be accessible on the two most important desktop platforms for the intended Steam release.

Rejected:

- Windows-only first release.
- Mobile-first development.

Impact:

- Input design should prioritize PC and desktop usage.
- Saves and file paths must work on both Windows and macOS.
- Build checks should include both platforms before public release.

## 2026-06-16 - Do Not Use Runtime AI

Reason:

- The game must work without an internet connection.
- Offline content is more stable for Steam release.
- No runtime API cost, account dependency, or privacy concern.
- Tarot explanations can be reviewed and tuned for tone before shipping.

Rejected:

- Calling OpenAI, Claude, DeepSeek, or other AI APIs during gameplay.
- Requiring internet access for card interpretations.

Impact:

- All tarot interpretations are stored as local game data.
- AI may be used only during development to draft text, followed by review and packaging.
- The game reads local JSON, ScriptableObject, or equivalent content data at runtime.

## 2026-06-16 - Use Major Phase Git Commits

Reason:

- The project needs clear restoration points without creating noisy history for every tiny change.
- Major phases are easier for a new developer to understand.

Rejected:

- Commit every tiny implementation detail.
- Keep the project only in local files without version control.

Impact:

- Each meaningful stage should update `PROGRESS.md`.
- Each complete stage should be committed with a clear commit message.
- GitHub should be private during early commercial development.

## 2026-06-16 - Commit Unity `.meta` Files

Reason:

- Unity `.meta` files preserve asset GUIDs and references.
- Missing or regenerated `.meta` files can break scenes, prefabs, materials, and asset links.

Rejected:

- Ignoring `.meta` files.
- Deleting `.meta` files during cleanup.

Impact:

- `.gitignore` must not ignore `.meta` files.
- `Assets/`, `Packages/`, `ProjectSettings/`, and Unity `.meta` files must be committed.
- Generated folders such as `Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, `Logs/`, and `UserSettings/` are ignored.

## 2026-06-16 - Treat Starfield as a Replaceable Background Theme

Reason:

- The default visual identity should be a black starfield with soft breathing stars.
- Players should eventually be able to choose different backgrounds.
- Future Steam Workshop backgrounds should not require rewriting the reading flow.

Rejected:

- Hardcoding the starfield as the only game background.
- Making card reveal effects depend on one specific background implementation.

Impact:

- The default starfield is implemented as a background theme.
- Advanced backgrounds may respond to reading events such as awaken, gather, and restore.
- Simpler custom backgrounds can skip advanced effects and rely on generic foreground particles later.
