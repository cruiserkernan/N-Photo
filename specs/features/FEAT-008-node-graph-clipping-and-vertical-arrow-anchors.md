# FEAT-008: Node Graph Clipping and Vertical Arrow Anchors

- Status: Implemented
- Last Updated: 2026-02-15
- Linked Architecture Specs:
  - `specs/architecture/ARCH-005-node-graph-worldspace-navigation.md`
  - `specs/architecture/ARCH-006-node-graph-viewport-clipping-and-four-side-anchors.md`
- Depends On:
  - `specs/features/FEAT-007-node-graph-infinite-pan-zoom.md`

## Problem
- Graph visuals can render outside the visible node editor area.
- Wire arrowheads are incorrect for links that should terminate on top/bottom target edges.

## Scope
- Enforce viewport clipping for graph visuals in node editor panel.
- Add four-side anchor selection for wires with correct top/bottom arrow behavior.

## Requirements
- Node cards and wires must be visually clipped to the node editor viewport bounds.
- World-space node positions must remain unconstrained (no reintroduction of viewport clamping).
- Wire target anchor must correctly use top or bottom edge when connection direction is vertically dominant.
- Wire source anchor selection and elbow route orientation must match target-side selection.
- Arrowhead must point into the target edge for left/right/top/bottom entries.

## Acceptance Criteria
- No node or wire visual appears outside the node editor viewport.
- Vertical connections (source above or below target) render arrowheads on target top/bottom edges.
- Horizontal connections keep existing correct side-edge behavior.
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.

## Out of Scope
- Curved spline routing.
- Port-level anchor offsets.

## Open Questions
- None.

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
