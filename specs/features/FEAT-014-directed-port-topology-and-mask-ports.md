# FEAT-014: Directed Port Topology and Mask Port Visuals

- Status: Implemented
- Last Updated: 2026-02-15
- Linked Architecture Specs:
  - `specs/architecture/ARCH-006-node-graph-viewport-clipping-and-four-side-anchors.md`
  - `specs/architecture/ARCH-012-shell-redesign-and-presentation-architecture.md`
- Depends On:
  - `specs/features/FEAT-011-canvas-only-node-graph-and-nuke-style-wire-dragging.md`

## Problem
Current graph cards render inputs on the left and outputs on the right, which does not match the requested directed compositing layout. Port state (connected vs unconnected) also needs clearer visual treatment, and many processing nodes should expose a right-edge mask port.

## Scope
- Update node card port placement and wire anchor logic to directed topology:
  - top-edge standard inputs
  - bottom-edge outputs
  - right-edge mask inputs on selected node types
- Initial mask-enabled node types: `Transform`, `ExposureContrast`, `Curves`, `HSL`, `Blur`, `Sharpen`, `Blend`.
- Update edge and port visuals to better match Nuke-style readability, including unconnected port styling.
- Keep existing graph interactions (pan/zoom/drag/connect/undo/redo) intact.

## Requirements
- Standard input ports must render on node top edge and remain valid connection targets.
- Output ports must render on node bottom edge and remain valid drag sources.
- Nodes configured for masking must render a distinct mask input on the right edge.
- Wire endpoints and arrowheads must align with top/bottom/right port anchors.
- Unconnected ports must be visually distinct from connected ports.
- Connection drag preview and snapping must continue to function with new anchors.
- Implementation must be data-driven by port metadata/role rather than per-node hard-coded layout in `MainWindow`.

## Acceptance Criteria
- Graph cards render standard inputs on top and outputs on bottom in the running app.
- Nodes with mask capability render a right-edge mask input with distinct visual styling.
- Existing edges render with correct anchor side/arrow direction under pan/zoom and after node drag.
- Connection drag from output to input/mask works and still uses engine `Connect(...)`.
- Unconnected ports are clearly visible and stylistically different from connected ports.
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
- Short app smoke run confirms graph interaction remains stable.

## Out of Scope
- New compositing math for mask-driven evaluation unless explicitly approved.
- Curved spline wire rendering.
- Full node visual redesign beyond ports/wires and directly supporting card layout changes.

## Open Questions
- None.

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
- short app smoke run succeeds (`dotnet run --project src/App/App.csproj`, then exit).
