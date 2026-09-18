# Moving World Architecture

## Presentation model

Phase 3.5 replaces physical auto-running with a fixed squad combat zone. The player
and squad remain around PlayerConfig.combatAnchorZ and move horizontally; battlefield
content advances toward them by decreasing world Z. The camera remains in a stable
world-space composition. This creates the intended feeling that the battlefield
comes toward the player without accumulating large player coordinates.

Coordinate conventions are:

- +Z — direction incoming content originates from
- -Z — direction content moves toward and past the player
- +X — horizontal movement
- +Y — vertical movement

## Runtime responsibilities

WorldScroller owns the current scroll speed, scrolling enabled state, registered
ScrollItem list, virtual DistanceTravelled, chunk recycling, and reset behavior. It
advances registered roots in one Update instead of giving every decoration its own
Update. Future gates, obstacles, pickups, enemies, bosses, and triggers can register
as ScrollItems without locating a global service.

ScrollItem marks one scrolling root. It stores an initial position for reset,
supports movement toward -Z, and can be either recyclable or non-recyclable.
Track chunks are recyclable; future one-shot level content can use non-recyclable
items and its own lifecycle policy.

WorldScrollMath contains pure calculations for scroll displacement, safe distance
accumulation, speed sanitization, and recycle target placement. These calculations
are covered by EditMode tests.

## Virtual distance

DistanceTravelled is a read-only logical progress value accumulated as scroll speed
multiplied by elapsed time while scrolling is enabled. Player Transform.z is never
used as mission progress. Future level progress, spawns, waves, bosses, rewards,
difficulty, and analytics should consume virtual distance.

## Track chunks and recycling

PF_TrackChunk has an exact configurable 30-unit prototype length and contains a
ground surface, orange boundaries, distance markers, and lightweight roadside
decoration. Five chunks form a small ring. When a recyclable chunk passes behind
the combat zone, WorldScroller moves its root to exactly one chunk length beyond
the furthest active chunk. Runtime recycling does not instantiate or destroy chunks.

Moving chunk geometry has no colliders. Player horizontal containment remains based
on PlayerConfig.trackHalfWidth and is independent of chunk transforms.

## Scrollable and non-scrollable content

Scrollable content includes track chunks, environment decoration, distance markers,
and future approaching level content. The player, squad, camera, persistent gameplay
systems, lighting, and UI do not scroll. Future enemies may combine base world
scrolling with independent movement owned by their own system.

The World/DynamicContent scene root is reserved for future scroll participants. No
gates, enemies, obstacles, pickups, or combat behavior are implemented in Phase 3.5.

## Camera composition

RunnerCameraController is fixed at the Phase 3.5 portrait framing position and looks
toward incoming +Z content. The squad is intended to occupy roughly the lower
20–35 percent of the screen, leaving most of the upper view available for future
enemies and gates. Optional smoothed horizontal following remains supported but is
configured to zero for the prototype.

## Pause, speed, and reset

SetScrollingEnabled stops and resumes scrolling without changing the configured
speed. SetScrollSpeed supports future temporary speed changes, and
RestoreConfiguredScrollSpeed restores GameBalanceConfig.worldScrollSpeed.
ResetScroll restores every registered item to its captured initial position, clears
virtual distance and recycle count, restores configured speed, and returns to the
configured initial scrolling state. GameplayBootstrap starts and pauses player and
world movement together.

These controls are architectural preparation for pause, results, upgrades, boss
introductions, failure, and retry. Those flows are not implemented yet.

## Setup and future integration

Run **Tools > Project Gatefront > Setup Phase 3.5 Moving World Prototype** to create
or repair GameBalanceConfig, PF_TrackChunk, the World hierarchy, fixed camera, and
GameplayBootstrap references. The utility is idempotent and removes the obsolete
long static Environment track.

Future systems should register level content with the scene's WorldScroller through
an owning spawner or bootstrap reference. They should not use service locators or
derive progress from player Z. A retry flow can call ResetScroll after resetting
future content state.
