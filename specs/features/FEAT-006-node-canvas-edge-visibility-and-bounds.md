# FEAT-006: Node Canvas Edge Visibility and Bounds

- Status: Deprecated
- Last Updated: 2026-02-15
- Linked Architecture Specs:
  - `specs/architecture/ARCH-003-node-canvas-interaction-model.md`
  - `specs/architecture/ARCH-004-node-canvas-wire-and-preview-focus.md`
- Depends On:
  - `specs/features/FEAT-005-node-canvas-wires-and-preview-focus.md`

## Problem
- Node cards can partially move outside the visible node canvas.
- Wire arrowheads can be hidden behind the destination card when source and destination are arranged right-to-left.

## Scope
- Keep node cards constrained fully inside the node canvas using actual card size.
- Attach wire endpoints to the card edge that faces the connection direction so arrowheads remain visible.

Note: Node boundary clamping behavior in this spec was superseded by `specs/features/FEAT-007-node-graph-infinite-pan-zoom.md`, which introduces infinite-feel world-space navigation without viewport clamping.

## Requirements
- Dragging a node must keep the entire card within canvas bounds.
- Canvas resize must re-clamp all card positions so cards remain fully visible.
- Wire source and target anchors must use side-aware edge selection:
  - left-to-right links use source right edge to target left edge
  - right-to-left links use source left edge to target right edge
- Directional arrowheads must remain visible outside the destination card for both left-to-right and right-to-left links.

## Acceptance Criteria
- A node cannot be dragged so any part of its card leaves the canvas viewport.
- After shrinking the canvas, existing nodes remain visible within bounds.
- For connections in both horizontal directions, arrowheads are visible and not hidden behind destination cards.
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.

## Out of Scope
- Curved spline wires.
- Port-level per-port y-offset routing.
- Auto-layout.

## Open Questions
- None.

## Validation
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
- `dotnet build NPhoto.slnx -c Debug` is blocked when `src/App/bin/Debug/net10.0/App.exe` is running (file lock by process `App`).
- `dotnet build NPhoto.slnx -c Debug -p:UseAppHost=false` succeeds in this environment and verifies compilation of all projects.
