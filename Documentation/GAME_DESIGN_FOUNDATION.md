# Project Gatefront — Game Design Foundation

## Product concept

Project Gatefront is a fast, accessible 3D action-runner where the player commands a growing tactical squad through compact battlefield missions. It uses familiar genre conventions only as a starting vocabulary; its identity will come from formation tactics, specialist composition, varied gates, and short combat encounters built around readable decisions.

## Core loop

RUN
→ MAKE GATE DECISIONS
→ GROW / MODIFY SQUAD
→ COLLECT OR UPGRADE WEAPONS
→ FIGHT ENEMY GROUPS
→ MAKE TACTICAL DECISIONS
→ DEFEAT FINAL ENCOUNTER
→ EARN RESOURCES
→ UPGRADE
→ NEXT MISSION

## Core presentation and interaction

The squad remains visually anchored near the lower portion of the portrait screen
while battlefield content advances toward it. The primary player interaction is
horizontal drag: the player positions the complete army across the road while
incoming track content supplies forward motion and progression. The player controls
the army as one unit through an invisible pivot; there is no persistent visible
commander. All active soldiers are visually equivalent, and horizontal drag shifts
the full squad together.

The future baseline combat interaction is automatic shooting. Gates, enemies,
weapons, rewards, and progression are intentionally not implemented in Phase 3.5.

The intended future progression loop is:

ATTEMPT LEVEL
→ FIGHT
→ FAIL OR PROGRESS
→ EARN RESOURCES
→ UPGRADE WEAPONS
→ RETRY
→ COMPLETE LEVEL

## Distinguishing mechanics

### Formation system

Players will eventually unlock tactical formations whose gameplay tradeoffs are meaningful:

- **Compact:** dense default combat presentation with concentrated coverage.
- **Line:** balanced coverage and general-purpose performance.
- **Wedge:** high frontal damage with more exposed flanks.
- **Column:** compact movement through narrow obstacles and lanes.
- **Spread:** improved survival against area attacks at the cost of concentration.

Formation selection is not intended to remain cosmetic. It should eventually affect
positioning, exposure, target coverage, and the kinds of encounters a squad handles
well. Phase 3.6 implements the spatial formations and unified army movement only;
tactical combat effects are future work.

### Specialist units

Future squads may combine Rifleman, Heavy, Medic, and Grenadier roles. Each specialist must contribute a readable tactical function rather than being a simple visual variation. Specialists are not implemented in Phase 1.

### Tactical gates

Gates should present choices beyond basic addition and multiplication. Future gates may alter squad size, weapon type, fire rate, damage, formation, specialist chance, armor, or risk/reward conditions. Gate pairs should create understandable tradeoffs rather than a single obviously correct arithmetic answer.

### Combat arenas

Runner segments should transition naturally into short combat arenas. Arenas provide a change of rhythm and a place for formation, specialist, and target-priority decisions to matter.

### Bosses

Boss encounters should use mechanics rather than relying on oversized health pools. Potential building blocks include telegraphed attacks, weak points, add waves, cover phases, and area attacks. Each boss should test a small, learnable set of skills.

### Short web-friendly sessions

Missions should target approximately 2–5 minutes, with fast restarts, immediate gameplay, and minimal menu friction. A failed attempt should return the player to a useful decision quickly.

## ORIGINALITY / IP RULES

Project Gatefront must never reproduce another game's:

- name or branding
- logos
- characters
- models
- animations
- textures
- sounds or music
- levels or exact level layouts
- UI or icons
- source code
- dialogue
- marketing art
- exact balancing

The team may study broad genre conventions and player expectations, but every implementation, asset, layout, progression decision, and piece of presentation must be original or properly licensed for this project.
