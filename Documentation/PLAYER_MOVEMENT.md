# Player Movement Prototype

## Coordinate system

Project Gatefront uses +Z as forward, +X as horizontal, and +Y as vertical. The
player object is a squad anchor: future squad members will arrange themselves
around this root rather than owning runner movement.

## Movement

PlayerController moves a CharacterController once per frame. Forward motion uses
configured units per second and Time.deltaTime. Horizontal input accumulates a
clamped lateral target, and Mathf.SmoothDamp moves the root toward it with a
configurable speed limit. Releasing the pointer clears residual lateral velocity.

The controller can be paused with SetMovementEnabled(false). It owns no camera,
combat, animation, audio, or UI behavior.

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

- forwardSpeed — automatic +Z speed
- lateralSensitivity — world distance for a full-screen-width drag
- lateralSmoothTime — lateral response smoothing
- trackHalfWidth — absolute X limit for the squad anchor
- maximumLateralSpeed — cap applied during smoothing

Prototype defaults are stored at
Assets/_Project/Data/Configs/PlayerConfig.asset.

## Camera

RunnerCameraController follows the prefab's CameraTarget with a world-space
offset and position smoothing. Its viewing rotation is derived from the offset
and look-ahead distance, then kept stable; horizontal drag does not add camera
yaw or sway. The camera remains separate from the player hierarchy.

## Prototype scene

02_Gameplay contains a 180-unit track, visible boundaries, 15-unit distance
markers, lightweight roadside primitives, one directional light, the PF_Player
prefab, and explicit GameplayBootstrap references.

Run **Tools > Project Gatefront > Setup Phase 2 Gameplay Prototype** to create or
repair missing generated elements. The operation reuses existing named assets
and objects and does not clear the scene.

## Future squad compatibility

PlayerRoot is the future squad leader/anchor. Visual geometry is under Visual,
and CameraTarget is a separate child. Squad formations can later follow the
anchor without changing input or root movement.
