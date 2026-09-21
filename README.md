# Project Gatefront

Project Gatefront is an original, commercial-quality 3D action-runner in which the player commands and develops a tactical squad through compact battlefield missions. The project is designed first for browser delivery while keeping runtime systems independent from any single portal SDK.

Current development stage: **PHASE 6 — Shootable Moving Gates**

The current prototype uses a fixed squad combat zone with resolution-independent
horizontal drag, a mostly fixed portrait camera, a moving battlefield, recycled
track chunks, virtual level distance, pooled squad spawning, Compact plus four
tactical formations, automatic squad shooting, and a continuous one-hit horde of
pooled Rushers. Density-authored sections increase pressure as distance grows.
Surviving enemies can remove visible soldiers at the formation contact
line; lateral movement can dodge enemies, and losing every soldier stops combat in
a lightweight defeat state. Fire is distributed across nearby horde targets rather
than concentrated on one enemy. Negative shootable gates now move with the world:
each projectile charges one point, potentially converting a penalty to a bonus.
Wide mandatory gates alternate with shootable-versus-divide choices while enemies
keep spawning. A boss-distance event prepares a future Giant. Advertising, analytics,
portal SDK integration, rewards, results UI, and upgrade economy are not included.

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

The boot scene initializes persistent game-level services. The main menu remains
an empty foundation; the gameplay scene contains the Phase 6 shootable-gate combat
prototype built on the fixed-squad moving-world presentation.

## Moving-world gameplay prototype

Open Assets/_Project/Scenes/02_Gameplay.unity and press Play. Hold the left mouse
button and drag horizontally, or drag with one finger on a touchscreen. Movement
configuration is stored in Assets/_Project/Data/Configs/PlayerConfig.asset. The
player and squad remain near a fixed combat Z while five pooled track chunks move
toward them and contribute to a read-only virtual travelled distance.

The Phase 3.5 prototype can be safely created or repaired with
**Tools > Project Gatefront > Setup Phase 3.5 Moving World Prototype**. The setup
command is idempotent and replaces the old long static track in 02_Gameplay.

## Squad formation prototype

PF_Player is an invisible control root. Every visible combatant is a pooled
PF_SquadUnit beneath SquadRoot, and CurrentUnitCount equals the visible soldier
count. Compact is a centered staggered crowd; Line, Wedge, Column, and Spread remain
available for future tactical mechanics. Development controls use 1–5 to switch
formations, = to add a combatant, and - to remove one. Holding Shift changes the
add/remove amount to five.

The Phase 3 prototype can be safely created or repaired with
**Tools > Project Gatefront > Setup Phase 3 Squad Prototype**.

## Automatic shooting prototype

All active squad members are shooters; there is no hidden or leader-only shooter.
A centralized target registry selects the nearest valid forward target at a modest
refresh rate, while each soldier keeps an independently staggered weapon cooldown.
Visible kinematic projectiles use a prewarmed reusable pool and segment collision
checks against primitive test targets that move with the battlefield.

The Phase 4 prototype can be safely created or repaired with
**Tools > Project Gatefront > Setup Phase 4 Auto Shooting Prototype**. Weapon base
data lives in `PrototypeRifle.asset`; resolved runtime stats remain separate so
future permanent and run-specific upgrades never mutate the configuration asset.

## Enemy horde combat prototype

The normal gameplay scene continuously spawns one-hit Rushers in distance-authored
Row, Grid, and Cluster mini-groups rather than Phase 4 target dummies or separated
weak/medium waves. Each enemy approaches at the current world speed plus its own
configured speed, registers with the combat target registry while alive, and returns
to its pool after death, squad contact, or a safe lateral pass.

Contact uses the squad's current read-only horizontal formation bounds. An aligned
enemy applies configured casualties once and immediately despawns; an enemy outside
those bounds can pass behind without damage. Combat casualties may reduce the squad
to zero, which pauses the world, enemies, weapons, and projectiles without adding a
final results screen. The Phase 5 setup can be repaired safely with
**Tools > Project Gatefront > Setup Phase 5 Enemy Combat Prototype**.

## Shootable moving gates

Five data-authored encounters appear at virtual distances 60–430. Each valid
projectile charges a negative gate by exactly one, through zero into positive
values. Wide single gates are mandatory; paired encounters offer a left shootable
gate or a static right ÷2 gate. The chosen value changes squad size at the fixed
selection plane, clamped from one to the configured maximum. Enemies keep spawning
at reduced density around gates. Run **Tools > Project Gatefront > Setup Phase 6
Shootable Gates** to repair the scene and prefab. See
[Gate system](Documentation/GATE_SYSTEM.md).

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
Player movement details are recorded in [Documentation/PLAYER_MOVEMENT.md](Documentation/PLAYER_MOVEMENT.md).
Squad architecture is recorded in [Documentation/SQUAD_SYSTEM.md](Documentation/SQUAD_SYSTEM.md).
Moving-world architecture is recorded in [Documentation/MOVING_WORLD_ARCHITECTURE.md](Documentation/MOVING_WORLD_ARCHITECTURE.md).
Weapon and combat architecture is recorded in [Documentation/WEAPON_SYSTEM.md](Documentation/WEAPON_SYSTEM.md).
Enemy horde architecture is recorded in [Documentation/ENEMY_COMBAT_SYSTEM.md](Documentation/ENEMY_COMBAT_SYSTEM.md).
