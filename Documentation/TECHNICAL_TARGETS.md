# Technical Targets

## Engine

- Unity 6 series
- Detected editor version: 6000.3.24f1

## Platforms

- **Primary platform:** WebGL
- **Primary distribution target:** CrazyGames
- **Secondary targets:** generic WebGL, itch.io, Android, and desktop if required

Portal SDKs must be isolated behind platform adapters. The game must remain functional when no portal SDK is installed or available.

## Rendering

The existing project is configured for Universal Render Pipeline 17.3.0. This detected configuration is retained. A render-pipeline migration is outside Phase 1 and should happen only when a measured requirement justifies its cost.

- Camera: 3D perspective
- Gameplay orientation: mobile-first portrait
- Desktop presentation: display the portrait gameplay region cleanly within wider browser windows without stretching or damaging composition

## Responsive design

UI must adapt to varying browser dimensions and respect device safe areas. Gameplay-critical information must remain legible without relying on one fixed resolution.

## Input

- Desktop: mouse drag
- Mobile: touch drag
- Optional future support: keyboard

Input implementation belongs to a later phase.

## Performance

Target stable gameplay on modest browser hardware and Chromebooks.

- Avoid expensive runtime allocations in recurring paths.
- Pool frequently spawned entities.
- Avoid unnecessary Update methods.
- Avoid excessive physics bodies and expensive collision arrangements.
- Optimize draw calls and material usage.
- Prefer lightweight shaders and materials.
- Preserve URP only as currently configured; do not add rendering features without measuring their cost.

## Web build size

Download size is a core product constraint.

- Do not add unnecessary high-resolution textures.
- Do not install unnecessary packages.
- Do not ship giant or uncompressed audio files.
- Remove unused assets before release builds.
- Review compression and stripping settings as part of build preparation.
