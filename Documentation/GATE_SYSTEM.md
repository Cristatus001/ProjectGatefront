# Shootable Moving Gates

Phase 6 gates are distance-authored in `PrototypeCombatLevel` and travel toward
the fixed-Z squad through `WorldContentCarrier`. `GateDirector` prewarms reusable
instances of `PF_ShootableGate`; no gate is instantiated during the run.

## Values and impacts

Each shootable gate copies its starting, minimum, and maximum values into runtime
state. A valid projectile impact increments the current value by exactly one,
irrespective of weapon damage, then consumes the projectile. For example,
`-1 → 0 → +1`. At the configured maximum, further hits have no effect. The
display uses signed precomputed strings and shared red, neutral, and green
materials, with lightweight pulse feedback on impacts and milestones. The level
asset is never modified by charging.

## Layouts and selection

`SingleLarge` fills almost all usable road width and resolves automatically.
`ShootableVsDivide` places the shootable gate on the left and a static,
unshootable `÷2` gate on the right. On crossing the squad contact plane, the
nearest gate center to SquadRoot X wins (exact center ties left). Crossing is
checked with current and previous positions, not physics triggers, so a long
frame cannot skip it. The selected value locks before application; the other
gate is disabled. `GateResolved` reports the selected type, starting and final
values, and squad counts.

Shootable values are additive: negative removes soldiers, zero changes nothing,
and positive adds soldiers. Divide uses floor integer division. Pure `GateMath`
also supports add, subtract, multiply, and divide for future authoring. Existing
`SquadController.AddUnits`/`RemoveUnits` maintain formation and clamp effects to
one through configured maximum; gate effects cannot directly defeat the squad.

## Combat and progression

When aligned and in engagement range, a shootable gate receives priority over
nonurgent enemies. Critically close unclaimed enemies remain first. Multiple
shooters may focus a gate; gate impacts do not create one-hit reservations.
Outside alignment, normal one-hit enemy distribution continues.

The prototype schedule crosses at virtual distances approximately 60, 140, 230,
330, and 430, with starting values -10 through -50. Encounters 3 and 5 are
paired with ÷2; the others are wide mandatory gates. `ContinuousEnemyDirector`
keeps spawning during gates but spaces groups farther apart in authored
readability windows, then restores normal cadence. Boss-distance signaling
remains at 520; Giant combat is not implemented here.

Prototype reset deactivates all gates, clears charge and locks, rewinds the
timeline, and restores enemy spawn density. Events are ready for later audio,
effects, UI, and analytics. Future gate effects may change weapons, fire rate,
formations, specialists, armor, or other risk/reward state without altering this
Phase 6 squad-size mechanic.
