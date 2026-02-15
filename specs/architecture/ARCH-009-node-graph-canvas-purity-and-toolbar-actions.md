# ARCH-009: Node Graph Canvas Purity and Toolbar Actions

- Status: Deprecated
- Last Updated: 2026-02-15
- Scope: Separate graph interaction surface from graph-management UI controls while preserving docking and node-editing behavior
- Superseded By: `specs/architecture/ARCH-012-shell-redesign-and-presentation-architecture.md`

## Goals
- Keep the node graph panel visually focused on the graph canvas only.
- Move node-creation controls into a toolbar model (Nuke-style action strip/menu).
- Support Nuke-style port drag-and-drop connection creation directly on node cards.
- Eliminate startup/layout clipping artifacts in node card rendering.

## Decisions
- Graph panel content will be reduced to the canvas surface (and minimal overlay chrome if needed).
- Node creation entrypoint will live in the app toolbar as an icon-style action strip.
- Port connection creation will be pointer-driven from output handle to input handle (drag-preview + drop-to-connect).
- Connection commands continue to call existing engine `Connect(...)`; incoming-port replacement behavior remains engine-owned.
- Graph-viewport sizing must be fully responsive (no fixed canvas height constraints).
- Initial view transform should avoid partial clipping of default nodes on first render.
- Existing docking architecture from `ARCH-007` remains unchanged.

## Boundaries
- `App` layer owns toolbar actions, graph panel layout, and viewport initialization behavior.
- `Editor.Domain` and `Editor.Engine` remain unchanged.

## Refactoring Guidance
- Keep graph-only concerns in `GraphPanelView`; keep action controls in toolbar or separate action view.
- Avoid duplicating node action wiring between views; centralize event wiring in `MainWindow`.
- Model transient wire-drag state in UI only (start port, current pointer world point, hover target input).

## Open Questions
- None.

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
