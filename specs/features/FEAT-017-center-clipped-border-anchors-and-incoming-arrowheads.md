# FEAT-017: Center-Clipped Border Anchors and Incoming Arrowheads

- Status: Implemented
- Last Updated: 2026-02-15
- Linked Architecture Specs:
  - `specs/architecture/ARCH-006-node-graph-viewport-clipping-and-four-side-anchors.md`
  - `specs/architecture/ARCH-008-nuke-inspired-visual-language.md`
- Depends On:
  - `specs/features/FEAT-016-continuous-wire-stub-ports.md`

## Problem
Current connections still rely on side-anchored endpoints/stubs. The requested behavior is border-continuous connections where effective routing uses node centers and rendered attachment points land anywhere on the node border based on direction. Incoming arrowheads should sit on the destination border and point toward the node center.

## Scope
- Route wire geometry from node center to node center, then clip both ends to node borders for rendering.
- Allow border attachment to resolve anywhere on border intersection (not restricted to static side anchors).
- Render incoming arrowheads at destination border points oriented toward destination center.
- For connected edges, render arrowheads only on destination input/mask endpoints (no source output arrowhead).
- For unconnected stubs, render arrowheads on both input/mask and output stubs.
- Keep current drag-to-connect/retarget mechanics and compatibility rules.

## Requirements
- Rendered connections attach at dynamic border intersection points derived from center-based geometry.
- Destination arrowheads (incoming) point toward destination node center.
- Connected outputs do not render arrowheads.
- Unconnected input/mask and output stubs render arrowheads.
- Drag preview line follows cursor while dragging.
- Connection snapping/commit behavior remains based on compatible input/output semantics.
- Existing pan/zoom/node drag interactions remain stable.

## Acceptance Criteria
- In app, wires visually attach on whichever node border segment intersects center-to-center direction.
- Incoming arrowheads are visible at destination border and oriented inward toward node center.
- Connected output endpoints render without arrowheads.
- Unconnected input/mask and output endpoints render arrowheads.
- Dragging connection preview remains under cursor and still supports snapping on drop.
- Existing connection creation/retargeting workflows continue to work.
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
- Short app smoke run confirms graph interaction remains stable.

## Out of Scope
- New graph validity rules or engine changes.
- Full curve/spline wire redesign.
- Visual redesign of unrelated shell panels.

## Open Questions
- None.

## Validation Plan
- Add/update tests for border-intersection math in presentation layer where practical.
- Run `dotnet build NPhoto.slnx -c Debug`.
- Run `dotnet test NPhoto.slnx -c Debug --no-build`.
- Run short smoke app launch (`dotnet run --project src/App/App.csproj`, then exit).

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
- short app smoke run succeeds (`dotnet run --project src/App/App.csproj -c Debug --no-build`, then exit).
