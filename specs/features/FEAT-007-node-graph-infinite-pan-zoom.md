# FEAT-007: Node Graph Infinite Pan/Zoom Workspace

- Status: Implemented
- Last Updated: 2026-02-15
- Linked Architecture Specs:
  - `specs/architecture/ARCH-004-node-canvas-wire-and-preview-focus.md`
  - `specs/architecture/ARCH-005-node-graph-worldspace-navigation.md`
- Depends On:
  - `specs/features/FEAT-005-node-canvas-wires-and-preview-focus.md`
  - `specs/features/FEAT-006-node-canvas-edge-visibility-and-bounds.md`

## Problem
Current node editor behavior is viewport-bounded and does not provide Nuke-like graph navigation. Nodes can appear constrained by panel bounds instead of living in a stable, pannable/zoomable workspace.

## Scope
- Introduce world-space node coordinates and viewport pan/zoom transform.
- Support Nuke-style navigation interaction in node graph panel.
- Keep existing wire rendering and node interactions compatible with transformed view.

## Requirements
- Node positions must remain fixed in world space while viewport pans/zooms.
- Node graph viewport must support panning across arbitrary workspace extent.
- Node graph viewport must support zoom in/out centered on pointer location.
- Dragging a node updates world position and must be independent of current zoom.
- Wire endpoints and elbow routing must remain attached correctly under pan/zoom.
- Existing add/connect/parameter workflows and preview slot shortcuts must continue to work.
- Pan interaction must use middle-mouse drag only.
- New node spawn position must be viewport center in world space.

## Acceptance Criteria
- Users can pan the node graph and navigate beyond initial viewport bounds.
- Users can zoom in/out and continue selecting/dragging nodes accurately.
- Node positions are preserved when zoom changes (no drift/jumps).
- Wires remain visually connected to node edges while panning/zooming.
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.

## Out of Scope
- Minimap.
- Auto-layout.
- Persisting viewport pan/zoom state to disk.

## Open Questions
- None.

## Decisions
- Pan gesture: middle-mouse drag only.
- Zoom gesture: mouse wheel centered at cursor.
- New nodes spawn at current viewport center.
- Zoom limits: min `0.35`, max `2.5`, default `1.0`.
- Viewport clipping and four-side wire anchors were later refined in `specs/features/FEAT-008-node-graph-clipping-and-vertical-arrow-anchors.md`.

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
