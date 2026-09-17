# Squad System

## Squad anchor

PlayerRoot remains the movement-controlled squad leader. SquadRoot is a direct child
of PlayerRoot and defines the local coordinate space for formation slots:

- +Z is forward
- +X is horizontal
- +Y is vertical

Followers target positions behind the leader in negative local Z. SquadUnit smooths
world position toward its assigned local slot, which allows visible follow motion
when the anchor moves laterally.

## Leader counting convention

CurrentUnitCount is the total number of combatants **including the visible leader**.
The leader is never pooled or removed. A configured starting count of 5 therefore
creates 1 leader and 4 followers, and the minimum runtime count is 1.

## Responsibilities

- SquadController owns follower membership, pooling, stable slot assignment, count
  limits, formation changes, events, and development controls.
- FormationCalculator generates local slots only when squad size, formation, or
  configuration changes.
- SquadUnit moves cheaply toward one assigned slot and faces SquadRoot forward. It
  contains no combat, health, weapon, or specialist behavior.
- FormationSlot carries a stable index and local position.

SquadCountChanged and FormationChanged provide typed integration points without a
UI dependency. CopyActiveFollowersTo provides controlled access for future combat
systems without exposing the mutable internal list.

## Formations

- Line uses compact centered rows with a configurable row capacity.
- Wedge begins with a narrow apex, then adds left/right pairs farther back.
- Column uses one or two narrow columns and extends backward.
- Spread uses wider horizontal and longitudinal spacing across centered rows.

All formation types clamp local X to maximumFormationHalfWidth. When the requested
row width would exceed that value, row capacity or spacing adapts instead of placing
followers outside the track.

## Track boundary containment

PlayerConfig remains the single source for physical trackHalfWidth and supplies a
small shared boundary padding. The leader clamp subtracts its approximate visual
radius and that padding, preserving most of the available lateral range while
keeping the visible leader inside the road.

FormationCalculator continues to produce stable ideal local slots. SquadController
caches their horizontal extents, combines them with the leader's current world X,
and calculates one world-space X correction for the entire follower formation. The
same correction is added to every follower target, so Y, Z, and relative spacing do
not change. Individual soldiers are not clamped independently.

The follower usable limit accounts for PF_SquadUnit's approximate visual radius and
the shared track padding. Formation generation also caps its maximum half-width to
the available follower space, allowing its existing row logic to handle formations
that would otherwise be too wide. Containment is refreshed after leader movement and
whenever formation or squad size changes.

## Pooling

The pool prewarms the configured starting follower count. Removed followers are
disabled and pushed into the local pool, then reused before any new instance is
created. Repeated add/remove operations within the previous peak size do not
instantiate or destroy follower objects.

PF_SquadUnit contains shared primitive meshes with no colliders or rigidbodies. All
standard followers share MAT_PrototypeSquadUnit.

## Development controls

These controls compile only in the Unity Editor or development builds:

- 1 — Line
- 2 — Wedge
- 3 — Column
- 4 — Spread
- = — add one combatant
- - — remove one combatant
- Shift + = / - — add or remove five

An optional development overlay shows total combatant count and current formation.
Selecting SquadRoot in the Scene view displays formation-slot gizmos.

## Future compatibility

The pool accepts SquadUnit components rather than assuming a rifleman implementation.
Future specialist metadata, combat state, and alternative visuals can extend or
compose with SquadUnit without moving formation generation into individual units.
