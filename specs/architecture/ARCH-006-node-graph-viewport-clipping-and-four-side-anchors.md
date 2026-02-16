# ARCH-006: Node Graph Viewport Clipping and Directed Port Topology

- Status: Implemented
- Last Updated: 2026-02-16
- Scope: Node graph viewport clipping, directed port placement, and wire geometry in Avalonia app shell

## Goals
- Keep graph visuals clipped to the node editor viewport while preserving infinite world-space navigation.
- Align node IO directionality to compositing conventions:
  - top-edge inputs
  - bottom-edge outputs
  - right-edge mask inputs on nodes that expose masking
- Treat each connection as center-routed geometry clipped to node borders, with border arrowheads for directional readability.

## Decisions
- Node world coordinates remain unconstrained (infinite workspace model from `ARCH-005`).
- Viewport clipping remains enforced at the graph viewport boundary.
- Wire geometry uses node-center endpoints as canonical routing inputs.
- Rendered wire segments are clipped to node border intersection points computed from center-to-center direction vectors.
- Border intersection is data-driven per node card bounds and is not constrained to only top/bottom/right side anchors.
- For incoming connections, arrowheads are rendered at the destination border and oriented toward the destination center.
- For connected edges, only destination input/mask endpoints render arrowheads; source output endpoints do not.
- Unconnected input/mask and output stubs render arrowheads.
- Connected/unconnected/hover states are communicated by styling only (stroke/opacity/width), not by switching visual types.
- Existing port roles (standard input, output, mask input) remain valid for connection compatibility and drag semantics.
- Mask support is modeled as explicit port metadata so placement logic is data-driven per port, not hard-coded per node type in the canvas renderer.
- Connection drop resolution is two-stage:
  - precise compatible port snap by pointer distance
  - fallback node-body targeting (full node interior plus outside padding) to resolve default compatible ports.
- Node-body interaction precedence is explicit:
  - pointer inside node body always starts node drag
  - wire/stub/edge grab hit zones are treated as outside-node affordances.
- Node-on-wire insertion is supported:
  - dropping a node on an edge splices node primary input/output into the edge
  - double-clicking an edge can insert an elbow node and splice immediately.
- Retarget drag cancellation/invalid drop behavior is explicit:
  - `Esc` cancels active wire drag without graph mutation
  - dropping a detached retarget drag on empty/incompatible space commits disconnection.

## UI Interaction Model
- Users can start a connection drag from either side of an edge:
  - output -> input/mask creates or retargets a connection
  - input/mask -> output retargets the incoming source for that input
- Drag preview wire remains dashed and snaps to nearest compatible target port.
- Mask ports are visually distinct from standard inputs to reduce accidental connections.
- Drag preview endpoint follows the pointer while snapped target state remains available for commit.
- Delete and bypass semantics support Nuke-style graph continuity:
  - deleting selected non-protected nodes may reconnect primary stream
  - Ctrl+node-drag can bypass node at drag start and continue moving it.

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
- short app smoke run succeeds (`dotnet run --project src/App/App.csproj -c Debug --no-build`, then exit).
