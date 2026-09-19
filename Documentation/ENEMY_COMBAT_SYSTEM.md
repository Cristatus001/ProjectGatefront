# Project Gatefront — Enemy Combat System

## Normal enemy rule

Every normal Rusher dies from **one valid projectile hit**, regardless of the
prototype rifle's damage value. There are no weak/medium HP tiers in gameplay.
Difficulty comes from group size, spawn interval, road coverage, and overlapping
groups. `EnemyController` is the normal-infantry implementation; a future Giant
must use a separate high-health controller and `BossConfig`, not this one-hit rule.

Normal enemies test fire rate, projectile coverage, and army size. The Giant will
test sustained boss damage. Future weapon upgrades should emphasize fire rate,
multi-shot, penetration, spread/coverage, weapon type, and boss damage rather
than raising normal-enemy HP. No upgrade economy is implemented here.

## Continuous level pressure

`LevelConfig` owns one normal-enemy config, ordered `CombatSectionDefinition`
entries, a 100-active-enemy cap, and `bossDistance`. Each section authors a
virtual distance interval, spawn interval, group-size range, Row/Grid/Cluster
pattern, spacing, lane center, and spawn depth. The prototype sections span
distance 0–600, with increasing density and a boss trigger at distance 520
(about 65 seconds at the default world speed).

`ContinuousEnemyDirector` schedules mini-groups from
`WorldScroller.DistanceTravelled`. It does **not** wait for previous groups to
die. Groups overlap, and spawning continues through the boss-distance trigger.
At the active cap, excess spawns are skipped; the scheduler still advances.
The boss event fires once per run and resets with the director. It is a
foundation event only: this correction does not spawn a Giant or implement boss
mechanics, UI, victory, gates, or rewards.

## Enemy lifetime and movement

`EnemyPool` prewarms 100 Rushers, reuses inactive enemies, and expands only to
the configured cap. `EnemyController` registers with `TargetRegistry` while
approaching and unregisters immediately on death, contact, passage, or reset.
One hit triggers a brief shrink/tilt death response, then pool release. There
are no ragdolls, physics bodies, or per-hit material instances.

`EnemyMotor` moves at `WorldScroller.CurrentScrollSpeed +
EnemyConfig.ApproachSpeed`. Enemies have no `ScrollItem`, preventing double
scrolling. Pausing world scrolling or enemy simulation stops movement.

## Target distribution and casualties

`SquadWeaponSystem` builds one ordered cache of up to 100 nearby forward targets
ten times per second. It assigns distinct unclaimed targets where available.
Projectile commitments temporarily reserve targets; reservations clear when
projectiles hit or expire, so missed targets become eligible again. Duplicate
assignments are spread when shooters outnumber targets. This is deliberately
lightweight for WebGL.

Contact is a constant-time horizontal bounds check at
`SquadController.CombatContactZ`. An aligned Rusher applies one casualty and
returns to the pool. A lateral miss passes behind and despawns safely. The
explicit `ApplyCasualties` API may reduce visible soldiers to zero; non-combat
`RemoveUnits` remains nonlethal. At zero, `SquadDefeated` fires once and
`GameplayBootstrap` pauses world, enemies, weapons, and projectiles.

`ResetPrototypeRun` restores squad count, enemy pool, spawn schedule,
boss-trigger state, projectiles, distance, and scrolling. No retry UI exists.

## Future moving world content

The fixed-Z squad never runs down a stationary gate track. Future gates are
world content that spawn ahead, move toward the squad, use horizontal position
for selection, and recycle after passage. `WorldContentCarrier` provides the
generic registered `ScrollItem` movement/pass/unregister path for pooled content;
a gate director can schedule discrete events
by the same virtual distance used by the enemy director. Gate content and
continuous enemies can coexist without stopping combat.

An illustrative timeline is combat at distance 0, moving gates near 35, 70,
110, 150, and 200, then a Giant with escort enemies near 250. These are design
examples, not implemented gate placements. The prototype's current boss trigger
is at 520 to permit a 45–90-second density test.

## Setup

**Tools > Project Gatefront > Setup Phase 5 Enemy Combat Prototype** repairs the
one-hit Rusher config, materials, prefab, continuous section asset, pool,
director, and gameplay-scene references. It is idempotent.
