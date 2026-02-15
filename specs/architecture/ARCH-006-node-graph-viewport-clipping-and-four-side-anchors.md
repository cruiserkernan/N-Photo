# ARCH-006: Node Graph Viewport Clipping and Four-Side Anchors

- Status: Implemented
- Last Updated: 2026-02-15
- Scope: Node graph viewport clipping guarantees and directional wire anchor selection in Avalonia app shell

## Goals
- Ensure graph visuals never draw outside the node editor viewport.
- Preserve infinite world-space behavior while constraining rendering to the viewport.
- Support directional wire arrows that correctly attach on left/right/top/bottom node edges.

## Decisions
- Node world coordinates remain unconstrained (infinite workspace model from ARCH-005).
- Viewport clipping is enforced at the node editor viewport boundary, independent of world positions.
- Wire endpoint anchors are selected from all four sides based on relative node center direction:
  - horizontal-dominant links use left/right anchors
  - vertical-dominant links use top/bottom anchors
- Orthogonal elbow routing remains, but route orientation matches chosen anchor axis.
- Arrowhead direction is derived from the final route segment into the target anchor.

## UI Interaction Model
- Nodes can be panned in/out of view, but visuals are clipped to node editor viewport.
- Wires entering a target from above or below terminate on top/bottom edge with visible arrowhead.

## Boundaries
- `Editor.Domain` and `Editor.Engine` remain unchanged.
- `App` (`MainWindow`) owns clipping and wire anchor/routing behavior.

## Open Questions
- None.
