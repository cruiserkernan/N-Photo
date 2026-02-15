# ARCH-006: Node Graph Viewport Clipping and Directed Port Topology

- Status: Implemented
- Last Updated: 2026-02-15
- Scope: Node graph viewport clipping, directed port placement, and wire geometry in Avalonia app shell

## Goals
- Keep graph visuals clipped to the node editor viewport while preserving infinite world-space navigation.
- Align node IO directionality to compositing conventions:
  - top-edge inputs
  - bottom-edge outputs
  - right-edge mask inputs on nodes that expose masking
- Improve readability of connected and unconnected ports through stronger port state visuals.

## Decisions
- Node world coordinates remain unconstrained (infinite workspace model from `ARCH-005`).
- Viewport clipping remains enforced at the graph viewport boundary.
- Wire anchors are role-based instead of axis-dominance-based:
  - standard inputs anchor on node top edge
  - outputs anchor on node bottom edge
  - mask inputs anchor on node right edge
- Orthogonal wire routing remains, but final segment direction is determined by target anchor side.
- Unconnected ports render with a subdued marker style distinct from connected/hovered ports.
- Mask support is modeled as explicit port metadata so placement logic is data-driven per port, not hard-coded per node type in the canvas renderer.

## UI Interaction Model
- Users drag connections from output ports to compatible input or mask ports.
- Drag preview wire remains dashed and snaps to nearest compatible target port.
- Mask ports are visually distinct from standard inputs to reduce accidental connections.

## Boundaries
- `Editor.Domain` owns canonical port definitions (including any new mask-port metadata).
- `App` and `App.Presentation` own visual placement, wire shape, and port state rendering.
- `Editor.Engine` remains responsible for graph validity and evaluation; no UI-layer validity rules are duplicated.

## Refactoring Guidance
- Extract port anchor and wire-geometry calculations from `MainWindow` into a presentation-layer helper/controller with direct unit tests.
- Keep node-card view assembly thin and data-driven by per-port metadata.

## Open Questions
- None.

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
- short app smoke run succeeds (`dotnet run --project src/App/App.csproj`, then exit).
