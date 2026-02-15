# ARCH-010: Node-Specific Properties Pane

- Status: Deprecated
- Last Updated: 2026-02-15
- Scope: Drive properties UI from selected node type with typed editors and image-input file picking
- Superseded By: `specs/architecture/ARCH-012-shell-redesign-and-presentation-architecture.md`

## Goals
- Make the Properties pane reflect the currently selected node on the graph canvas.
- Present parameter controls that match each node type instead of a generic name/value editor.
- Allow `ImageInput` node image selection directly from Properties.

## Decisions
- The active properties context is the currently selected node card (`_selectedNodeId`) in `MainWindow`.
- Properties rendering is generated from node type metadata (`NodeTypeCatalog`) plus node-specific UI overrides.
- `ImageInput` uses a dedicated file picker action in Properties that loads the image through existing `IImageLoader` and `IEditorEngine.SetInputImage(...)`.
- Global top-toolbar `Load` action is removed; image sources are chosen from node properties.
- Standard parameter editors map by `ParameterValueKind`:
  - `Float` and `Integer`: numeric input control.
  - `Boolean`: checkbox control.
  - `Enum`: combo box constrained to `EnumValues`.
- Parameter updates continue to flow through `IEditorEngine.SetParameter(...)` to preserve undo/redo and render behavior.

## Boundaries
- `App` layer owns properties-pane presentation, selection synchronization, and file picker wiring.
- `Editor.Domain` parameter definitions remain the source of truth for valid parameter ranges/types.
- `Editor.Engine` command/undo/render pipelines remain unchanged.

## Refactoring Guidance
- Keep `MainWindow` focused on orchestration and move per-field parsing/formatting helpers into small private helpers or a dedicated app-layer helper class.
- Avoid duplicating parameter mapping logic between UI event handlers.

## Open Questions
- None.
