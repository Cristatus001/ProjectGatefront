# Player Movement Prototype

## Coordinate system

Project Gatefront uses +Z as the direction incoming content originates from, -Z as
the direction world content moves toward and past the squad, +X as horizontal, and
+Y as vertical. The player object is the fixed combat anchor for the squad.

## Movement

PlayerController moves its CharacterController laterally once per frame. Horizontal
input accumulates a clamped target, and Mathf.SmoothDamp moves the root toward it
with a configurable speed limit. Releasing the pointer clears residual lateral
velocity.

The root is initialized at combatAnchorZ and each movement update corrects any Z
drift back to that configured anchor. Player Transform.z is not level progress.
The controller can be paused with SetMovementEnabled(false). It owns no world
scrolling, camera, combat, animation, audio, or UI behavior.

## Input

IPlayerInputSource isolates PlayerController from input devices.
PointerDragInputSource reads the existing Unity Input System and supports the
primary touchscreen contact or the desktop left mouse button. The initial press
always reports zero movement, so clicking in a new location never teleports the
player.

Horizontal pointer delta is divided by Screen.width before it reaches movement
logic. A drag covering the same fraction of the screen therefore has comparable
behavior across desktop, portrait mobile, and resized browser windows.

## Configuration

PlayerConfig contains:

- combatAnchorZ — fixed world Z for the player and squad anchor
- lateralSensitivity — world distance for a full-screen-width drag
- lateralSmoothTime — lateral response smoothing
- trackHalfWidth — shared absolute X road limit
- leaderBoundaryRadius — approximate visible leader half-width
- trackBoundaryPadding — shared visual inset from the road edge
- maximumLateralSpeed — cap applied during smoothing

World forward presentation speed is configured separately in GameBalanceConfig as
worldScrollSpeed. Prototype defaults are stored under
Assets/_Project/Data/Configs.

## Camera

RunnerCameraController uses a stable world position and a fixed look point aimed
toward the incoming +Z battlefield. Its optional horizontal follow amount is zero
in the Phase 3.5 scene, so horizontal drag does not add camera drift, yaw, or sway.
The squad is framed in the lower portion of a portrait view to preserve forward
visibility.

## Prototype scene

02_Gameplay contains a World root with five 30-unit PF_TrackChunk instances,
moving boundaries, distance markers, lightweight roadside decoration, a
DynamicContent root for future scroll participants, one directional light, the
PF_Player prefab, and explicit GameplayBootstrap references.

Run **Tools > Project Gatefront > Setup Phase 3.5 Moving World Prototype** to create
or repair the generated elements. The operation is idempotent and removes the old
long static Environment track to prevent overlapping roads.

See MOVING_WORLD_ARCHITECTURE.md for scrolling, recycling, virtual distance, pause,
speed, and reset behavior.
