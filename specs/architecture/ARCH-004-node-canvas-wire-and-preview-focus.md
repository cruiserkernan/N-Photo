# ARCH-004: Node Canvas Wire Overlay and Preview Focus

- Status: Implemented
- Last Updated: 2026-02-15
- Scope: Visual wire rendering, node-level preview targeting, and graph editing behavior in the Avalonia app shell

## Goals
- Render graph connections directly on the node canvas as directional wires (Nuke-inspired readability).
- Allow users to keep nodes disconnected without forcing connections during node creation.
- Let users select a node as the preview focus so the viewport shows the image at that graph point.

## Decisions
- Node cards remain UI-positioned elements owned by `App` and keyed by `NodeId`.
- Wires are rendered in the canvas layer using current node card positions and current engine edge snapshot.
- Directionality is conveyed by arrowheads on wires oriented from source output to target input.
- Wire routing uses Nuke-style elbow/orthogonal segments for readability.
- Preview focus is a UI-selected `NodeId` that can target any node (including disconnected nodes).
- Engine evaluation target is parameterized by requested node id; default remains `OutputNodeId` when no explicit focus is selected.
- Disconnected or non-image-producing focus targets resolve to `null` output and must not crash rendering.
- Empty focus results are surfaced as a blank preview frame (black).

## UI Interaction Model
- Canvas has layered visuals:
  - wire layer (behind nodes)
  - node card layer (interactive cards)
- Wire endpoints are derived from node card geometry:
  - source anchor: right-center of source card
  - target anchor: left-center of target card
- Wires update when:
  - graph edges change
  - node positions change (drag)
  - canvas size changes
- Preview focus state is changed by selecting a node from UI controls.
- Preview request uses current focus node id, or defaults to output node.

## Boundaries
- `Editor.Domain` remains unchanged for layout and rendering visuals.
- `Editor.Engine` may expose preview-target render API without introducing UI dependencies.
- `App` owns visual styling and interaction for wires and focus selection.
- Existing add/connect/parameter/undo-redo semantics remain intact.

## Open Questions
- None.
