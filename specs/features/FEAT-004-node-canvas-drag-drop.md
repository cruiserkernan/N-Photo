# FEAT-004: Node Canvas Drag-and-Drop

- Status: Implemented
- Last Updated: 2026-02-15
- Linked Architecture Specs:
  - `specs/architecture/ARCH-001-mvp-foundation.md`
  - `specs/architecture/ARCH-003-node-canvas-interaction-model.md`
- Depends On:
  - `specs/features/FEAT-001-mvp-node-set.md`

## Problem
Graph editing currently uses form controls only. Users cannot directly manipulate node layout with drag interactions.

## Scope
Add a node canvas to the app where existing nodes are rendered as draggable cards so users can reposition nodes with pointer drag-and-drop.

## Requirements
- Add a visible node-canvas area in the app shell.
- Render one visual card per engine node in the canvas.
- Support drag-and-drop repositioning of cards with mouse/touch pointer interaction.
- Keep node positions stable across graph refreshes in the current app session.
- Continue supporting existing add/connect/parameter workflows.
- Preserve current preview rendering and export behavior.

## Acceptance Criteria
- After adding nodes, each node appears as a draggable card in the node canvas.
- Dragging a card updates its on-screen position immediately.
- Releasing drag keeps the node at its new position.
- Using existing controls to connect nodes continues to work after dragging.
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.

## Out of Scope
- Port-to-port wire dragging/connection creation.
- Multi-select, marquee select, and group drag.
- Persisting layout to disk.

## Open Questions
- None.

## Decisions
- Initial release rendered draggable node cards without visual edge lines; this was superseded by `specs/features/FEAT-005-node-canvas-wires-and-preview-focus.md`.
- Drag positions are constrained to remain inside the canvas bounds.

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
