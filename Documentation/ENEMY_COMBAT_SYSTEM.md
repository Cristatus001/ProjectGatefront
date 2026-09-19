# Project Gatefront — Enemy Combat System

## Overview

Phase 5 replaces normal-scene target dummies with deterministic waves of pooled
Rusher enemies. The squad remains fixed in Z, the battlefield continues to scroll,
and the player controls only cohesive horizontal positioning. Enemies provide the
first combat pressure: kill them before contact, or move the army out of their path.

## EnemyConfig

`EnemyConfig` is the existing shared base asset for an enemy archetype. It now
stores identity, display name, maximum health, independent approach speed,
casualties on contact, target radius, a reserved reward value, and visual scale.
Runtime enemies read but never mutate these assets. Weak and medium Rusher configs
use the same code and prefab; stronger, faster, or visually larger future variants
can remain data-driven.

## EnemyController and EnemyMotor

`EnemyController` owns runtime health and implements `ITargetable` and
`IDamageable`. It registers while approaching, ignores damage after death, and
unregisters immediately on death, contact, lateral passage, reset, or pooling. A
brief transform-scale pulse communicates hits; death uses a short shrink/tilt before
pool release. Neither path creates material instances, ragdolls, or rigidbodies.

`EnemyMotor` applies one explicit movement model:

`effective speed = WorldScroller.CurrentScrollSpeed + EnemyConfig.ApproachSpeed`

Enemies do not have `ScrollItem`, so world motion is never applied twice. Movement
requires both enemy simulation and world scrolling to be enabled.

## EnemyPool

`EnemyPool` prewarms 60 Rushers for the prototype, reuses inactive enemies, expands
only when needed, and enforces the level's active-enemy cap of 100. One pool update
ticks the compact active list; individual enemies do not perform scene searches.
Health, feedback, wave ownership, target registration, and contact state reset on
reuse. Spawned, killed, contacted, and removed events are UI-independent hooks for
future audio, VFX, rewards, and analytics.

## Authored wave data

`LevelConfig` owns serialized `EnemyWaveDefinition` entries and the active-enemy
safety limit. Each wave defines virtual spawn distance, enemy config, count, Row,
Grid, or Cluster pattern, row count, spacing, lane center, and spawn depth.
`EnemyWaveDirector` orders entries by distance and schedules them from
`WorldScroller.DistanceTravelled`; it does not use player Transform Z or random
endless spawning.

The prototype sequence contains six waves across roughly 40 seconds: 5 weak, 10
weak, 15 weak, 10 medium, a 25-enemy weak horde, and a final 40-enemy medium horde.
A wave completes after every spawned member is killed, contacts the squad, or passes
behind. World scrolling does not wait for completion.

## Target distribution

Enemies use the Phase 4 `TargetRegistry`. Ten times per second,
`SquadWeaponSystem` builds one ordered cache of at most 32 nearest valid forward
targets and assigns shooters round-robin. Fifty soldiers therefore spread fire over
a horde without fifty independent sorts or scene queries.

## Contact, casualties, and dodging

Contact occurs when an enemy's leading edge reaches `SquadController.CombatContactZ`.
A constant-time interval overlap compares the enemy radius and small padding with
the squad's current world-space horizontal formation bounds. No per-soldier physics
or collision scan is needed.

An overlap applies configured casualties exactly once, makes the enemy non-targetable,
and immediately returns it to the pool. A horizontal miss becomes non-targetable,
continues behind the combat line, and returns to the pool without casualties. This
makes lateral movement an actual defensive decision.

`ApplyCasualties` is distinct from non-lethal `RemoveUnits`: it can remove every
visible soldier. Remaining soldiers receive a newly calculated complete formation
and recompact smoothly.

## SquadDefeated and reset

When the squad reaches zero, `SquadDefeated` fires once per run.
`GameplayBootstrap` stops player movement, camera follow updates, world scrolling,
enemy simulation, firing, and projectile advancement, then clears active
projectiles. No final game-over UI is implemented.

`ResetPrototypeRun` provides the future retry foundation: active enemies return to
the pool, target/projectile state clears, the squad restores its configured starting
count, world distance resets, wave scheduling restarts, and combat resumes. It is a
runtime API only; Phase 5 does not add retry UI, rewards, currency, or persistence.

## Future archetypes

Heavy, shielded, fast, ranged, and boss enemies can use additional configs and
specialized components while retaining the same pool, wave, target, damage, contact,
casualty, event, and defeat contracts. Those behaviors are outside Phase 5.

## Setup

Run **Tools > Project Gatefront > Setup Phase 5 Enemy Combat Prototype** to create
or repair configs, level data, materials, the Rusher prefab, enemy pool/director,
and gameplay-scene references. The command is idempotent and removes the old Phase 4
dummy group from normal gameplay.
