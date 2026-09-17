# Project Gatefront

Project Gatefront is an original, commercial-quality 3D action-runner in which the player commands and develops a tactical squad through compact battlefield missions. The project is designed first for browser delivery while keeping runtime systems independent from any single portal SDK.

Current development stage: **PHASE 1 — Foundation**

No gameplay, advertising, analytics, or portal SDK integration is included in this phase.

## Engine

- Unity Editor: 6000.3.24f1
- Render pipeline: Universal Render Pipeline (URP) 17.3.0, as supplied by the existing project
- Primary platform: WebGL
- Primary distribution target: CrazyGames
- Secondary targets: generic WebGL, itch.io, Android, and desktop when required

## Opening the project

1. Install Unity 6000.3.24f1 through Unity Hub.
2. Include WebGL Build Support. Add Android Build Support only when working on Android builds.
3. Open this repository folder as a Unity project.
4. In Unity, run **Tools > Project Gatefront > Setup Project** once. The command is safe to run again.

The setup command verifies required folders and serialization settings, creates missing foundation scenes, and registers them in Build Settings without removing existing scenes.

## Repository structure

- Assets/_Project — first-party game assets and source
- Assets/Plugins — future third-party plugins, when required
- Assets/Settings — existing URP settings
- Assets/Scenes — existing Unity template scene, retained for reference
- Documentation — product, technical, and coding guidance
- Packages — Unity package manifest and lock file
- ProjectSettings — shared Unity project settings

Third-party packages must remain outside Assets/_Project.

## Foundation scenes

- Assets/_Project/Scenes/00_Boot.unity
- Assets/_Project/Scenes/01_MainMenu.unity
- Assets/_Project/Scenes/02_Gameplay.unity

The boot scene initializes persistent, game-level services. The main menu and gameplay scenes are intentionally empty foundations in Phase 1.

## Branch strategy

- main — stable/release-ready work
- develop — active integration branch
- feature/<feature-name> — feature development
- fix/<bug-name> — bug fixes

Keep the process lightweight. Changes should normally merge into develop and be promoted to main when stable.

## Build targets

WebGL is the primary target. Portal-specific behavior must be accessed through ProjectGatefront.Platform abstractions so local, generic WebGL, Android, and desktop builds continue to function without a portal SDK.

## Conventions

See [Documentation/CODING_STANDARDS.md](Documentation/CODING_STANDARDS.md). Technical constraints are recorded in [Documentation/TECHNICAL_TARGETS.md](Documentation/TECHNICAL_TARGETS.md).
