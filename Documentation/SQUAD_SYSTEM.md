# Squad System

## Invisible control root

PF_Player is an invisible control prefab. Its CharacterController and
PlayerController keep the fixed combat anchor at the configured Z coordinate and
move the complete army horizontally. The direct SquadRoot child defines the local
formation coordinate space:

- +Z is forward
- +X is horizontal
- +Y is vertical

PF_Player has no rendered body. All visible soldiers are pooled PF_SquadUnit
instances parented beneath SquadRoot. Moving the control root therefore translates
every soldier by the same world-space delta immediately; individual soldiers never
chase a rendered leader.

## Equal-member count semantics

CurrentUnitCount is exactly the number of active visible soldiers. There is no
hidden or always-counted commander. A count of 1 means one pooled PF_SquadUnit is
visible, a count of 20 means twenty are visible, and the minimum count remains 1.

Every visible member uses the same prefab, scale, body proportions, material family,
facing, WeaponMount, and FirePoint hierarchy. WeaponMount and FirePoint are transform
anchors only; Phase 3.6 does not implement shooting.

## Responsibilities

- SquadController owns visible membership, one shared pool, slot assignment, count
  limits, formation changes, root movement limits, events, and development controls.
- FormationCalculator generates deterministic local slots when size or formation
  changes and centers every formation on the invisible root.
- SquadUnit settles toward its target in local space and faces the root forward.
  Root translation does not add formation lag or stretch.
- FormationSlot carries a stable index and local target position.

SquadCountChanged and FormationChanged are typed integration points without a UI
dependency. CopyActiveUnitsTo provides controlled access to all visible soldiers
without exposing the mutable internal list.

## Formations

Compact is the default. It builds a staggered hex-like candidate grid, selects
center/near-center symmetric pairs before outer pairs, and normalizes the footprint
to a slightly wider-than-deep rounded crowd. Slots exist both ahead of and behind
the centroid. No slot is reserved for a commander, no flank-first arc is used, and
growth does not append a rear tail. Recalculation after add or remove operations
fills the nearest complete layout again, so remaining units compact inward.

Line, Wedge, Column, and Spread remain available for future tactical mechanics.
They are also centered on the invisible root. Phase 3.6 does not assign combat
effects to formation choice.

## Cohesive transitions

SquadUnit changes only its local slot offset. The configured transition speed is
fast enough for short reshapes while the parent root supplies all direct lateral
motion. Direction reversal therefore moves the complete army together, with no
rendered leader, tail, or elastic world-space follow delay.

## Root-level road containment

PlayerConfig supplies the physical track half-width and shared boundary padding.
SquadConfig supplies the visible soldier radius. SquadController measures the
current formation's minimum and maximum local X extents, then calculates the legal
control-root range:

`minimum root X = -usable half-width - local minimum X`

`maximum root X = usable half-width - local maximum X`

PlayerController clamps its lateral target to that current range. At either edge,
the full formation stops together before any visible body crosses the road. Slots
are not shifted, compressed, or independently corrected, so relative spacing and
the blob centroid remain intact.

## Pooling and recompaction

The shared pool prewarms the configured starting visible count. Removed units are
disabled and returned to that pool; additions reuse pooled units before creating
new instances. Formation generation always receives the full active unit count.
Adding or removing units rebuilds a complete centroid-prioritized slot set, avoiding
permanent holes and tail-only growth.

## Development controls

These controls compile only in the Unity Editor or development builds:

- 1 — Compact
- 2 — Line
- 3 — Wedge
- 4 — Column
- 5 — Spread
- = — add one visible soldier
- - — remove one visible soldier
- Shift + = / - — add or remove five

An optional overlay shows visible soldier count and current formation. Selecting
SquadRoot in the Scene view displays formation-slot and usable-road gizmos.

## Moving-world boundary

The squad centroid remains at the existing fixed combat Z. Formation offsets add
only modest local depth. WorldScroller, virtual DistanceTravelled, track recycling,
and the fixed road-centered camera remain responsible for forward presentation.
