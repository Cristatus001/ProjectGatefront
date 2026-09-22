# Project Gatefront — Weapon System

## Combat model

Squad combat is automatic. Every active `SquadUnit` owns one lightweight
`WeaponController`, and `CurrentUnitCount` therefore equals the shooter count. There
is no visible commander, hidden leader shooter, or manual fire input. The player
continues to control horizontal army positioning while weapons acquire and engage
valid targets ahead of the formation.

## WeaponConfig

`WeaponConfig` is the shared base design asset for an equipped weapon. It contains
the weapon identity, display name, tier, damage, shots per second, range, projectile
speed and lifetime, projectile radius, spread, projectile count, optional
penetration, targeting cone, and prototype visual. Fire rate is always expressed as
shots per second: a value of 4 produces a 0.25-second interval.

All soldiers reference the same configuration. Runtime systems never modify or
duplicate that ScriptableObject.

## WeaponRuntimeStats and future upgrades

`WeaponRuntimeStats` is an immutable resolved value generated from `WeaponConfig`
and `WeaponStatModifiers`. It sanitizes values and derives the firing interval,
effective projectile lifetime, and targeting cosine. This separation allows future
permanent progression and temporary run upgrades to combine with the same base
weapon without saving changes into project assets.

Normal Rushers always die to one valid bullet; future progression should
primarily improve fire rate, multi-shot, penetration, spread/coverage, weapon
type, and sustained Giant damage. It must not create normal-enemy HP tiers.

The current tier enum provides Tier 1 through Tier 5 as an organizational
foundation, not final balance. The modifier path already supports damage, fire rate,
additional projectiles, and penetration. A later equipment system can swap the
shared visual/config for all soldiers through `SquadWeaponSystem.EquipWeapon`.

Shootable gates gain exactly one value per projectile impact, regardless of weapon
damage. Higher fire rate therefore kills more normal enemies and converts negative
gates faster. Multiple projectiles per shot can add multiple gate points if each
lands; the gate consumes each hit and does not use one-hit enemy reservations.

## WeaponController and automatic firing

Each visible soldier's `WeaponController` references its existing `WeaponMount` and
`FirePoint`. The controller receives a shared target, validates range and forward
cone, advances its cooldown, rotates only the weapon mount for modest yaw aiming,
and requests projectiles from the pool. Deterministic per-soldier cooldown phases
spread launches over time so a large squad does not fire on one frame forever.

The controller does not search the scene, allocate target lists, instantiate bullets,
or rotate the soldier body. A short reusable primitive muzzle flash provides firing
feedback without material instances or per-shot object creation.

## Targeting and damage

`TargetRegistry` is the authoritative collection of `ITargetable` objects. Targets
register and unregister explicitly. `SquadWeaponSystem` scans that compact list ten
times per second. It maintains an ordered cache of up to 100 nearest valid forward
candidates and assigns distinct unclaimed targets when possible. Each fired
projectile provisionally reserves its target until impact or expiry; misses release
their reservation. If shooters outnumber targets, duplicate assignments are
balanced instead of concentrating on the nearest enemy. This reduces one-hit
horde overkill without per-soldier scene searches or sorts. Objects behind the
squad, outside weapon range, outside the targeting cone, or no longer targetable are
rejected.

Projectiles apply damage only through `IDamageable`, returning a `DamageResult`.
Combat code therefore has no dependency on a future enemy class and can later work
with infantry, heavy enemies, bosses, or destructible objects.

## Projectile pooling and collision

`ProjectilePool` prewarms a bounded capacity calculated from configured maximum
shooter count, fire
rate, projectile count, and useful projectile lifetime. It reuses inactive objects,
expands only when necessary, and removes active entries in constant time. The
prototype projectile is kinematic: it advances with `deltaTime` and performs a
segment-versus-target-sphere query between its previous and next positions to avoid
fast-projectile tunneling. It returns to the pool on impact or expiry.

Each sweep compares physical entry distances for all registered targets and resolves
only the nearest valid intersection. Shootable gates provide a thin panel volume;
normal enemies use their target sphere. Foreground enemies therefore shield a gate,
including when they move into a projectile's path after launch. An enemy outside
that projectile corridor does not block the gate. Near-equal enemy/gate surfaces
use a documented 0.01-world-unit enemy-first tie break. The Prototype Rifle stops
on its first collision; future penetration may deliberately continue from that
ordered hit.

No projectile uses a Rigidbody, per-frame `GetComponent`, scene search, or routine
`Instantiate`/`Destroy` cycle.

## Enemy registry behavior and prototype targets

Phase 5 Rusher enemies register while alive and approaching. Death, contact,
lateral passage, reset, and pooling immediately remove them from TargetRegistry, so
dead or missed enemies cannot keep receiving assignments. The distribution cache is
rebuilt on the next modest target refresh.

`CombatTargetDummy` is test content, not enemy AI. It owns configurable health and
target radius, implements both combat interfaces, registers with `TargetRegistry`,
and participates in `WorldScroller` through `ScrollItem`. At zero health it becomes
non-targetable immediately, unregisters, and deactivates. A target that passes the
squad also unregisters and deactivates without damaging the player.

The class remains available for isolated tests, but Phase 5 normal gameplay no
longer contains target-dummy scene objects.

## Pause and reset

Disabling gameplay movement also pauses weapon firing and projectile simulation.
`SquadWeaponSystem.ResetCombat` returns all active projectiles, clears and rebuilds
the registry from reset test targets, restores deterministic cooldown phases, and
forces a fresh target acquisition. This is the foundation for a later retry flow.

## Setup

Run **Tools > Project Gatefront > Setup Phase 4 Auto Shooting Prototype** to create
or repair the config, prefabs, materials, squad weapon components, combat services,
and scene test targets. The command is idempotent and does not duplicate objects.
