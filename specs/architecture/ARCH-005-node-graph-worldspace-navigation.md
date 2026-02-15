# ARCH-005: Node Graph World-Space Navigation

- Status: Implemented
- Last Updated: 2026-02-15
- Scope: Infinite-feel node graph workspace with pan/zoom and fixed world positions in Avalonia app shell

## Goals
- Replace bounded canvas behavior with a graph workspace that feels infinite (Nuke-style navigation).
- Keep node coordinates stable in world space regardless of viewport pan/zoom.
- Preserve existing node editing semantics (add/connect/drag/wires/preview slots).

## Decisions
- Node positions are stored in graph world coordinates (`NodeId -> Point`).
- View transform is UI-only state:
  - `PanOffset` (world-to-screen translation)
  - `ZoomScale` (world-to-screen scale)
- Rendering and hit-testing use transform conversion:
  - world -> screen for drawing
  - screen -> world for pointer interactions
- Node dragging updates world position; pan/zoom updates only view transform.
- Workspace does not clamp node world positions to viewport bounds.
- Wires are routed in world space, then transformed to screen.
- Panning gesture: middle-mouse drag on graph background.
- Zoom gesture: mouse wheel zoom centered on pointer anchor.
- New node placement uses current viewport center transformed into world space.
- Zoom limits for MVP: min `0.35`, max `2.5`, default `1.0`.

## UI Interaction Model
- Pointer drag on empty graph background pans viewport.
- Pointer drag on node card moves selected node in world space.
- Mouse wheel zooms around pointer anchor (content under cursor stays stable while zooming).
- Optional reset view action restores default zoom/pan.

## Boundaries
- `Editor.Domain` and `Editor.Engine` remain unaware of pan/zoom/view transforms.
- `App` owns world-position and viewport transform state.
- Existing preview-focus and keyboard slot behavior remain unchanged.

## Open Questions
- None.
