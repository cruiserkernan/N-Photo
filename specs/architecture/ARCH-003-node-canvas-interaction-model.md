# ARCH-003: Node Canvas Interaction Model

- Status: Implemented
- Last Updated: 2026-02-15
- Scope: Interactive node-canvas behavior for graph editing in the Avalonia app shell

## Goals
- Introduce a node-canvas surface where users can place and move nodes via drag interactions.
- Keep graph topology authoritative in `Editor.Engine` while keeping layout concerns in UI-only state.
- Avoid coupling domain/engine to view coordinates in MVP.

## Decisions
- Node position is UI state owned by `App` (`MainWindow`) and keyed by `NodeId`.
- `Editor.Domain` and `Editor.Engine` remain unchanged for layout concerns.
- Dragging updates only node positions; graph edges and processing behavior remain managed by existing engine APIs.
- Initial node creation path remains explicit user action from existing Add Node control unless a feature spec adds palette-to-canvas creation.

## UI Interaction Model
- Canvas hosts visual node cards positioned absolutely.
- Drag operation is pointer-based capture and movement of existing node cards.
- A node card drag has states: `Idle -> Dragging -> Idle`.
- During drag, the active node card updates position continuously and raises no engine commands.
- On graph refresh, new nodes receive deterministic default placement and existing nodes keep prior positions.

## Boundaries
- `App` can read node and edge snapshots from `IEditorEngine`.
- `App` must not mutate domain model for layout-only updates.
- Undo/redo stacks in engine are unaffected by pure layout drags.

## Open Questions
- None.
