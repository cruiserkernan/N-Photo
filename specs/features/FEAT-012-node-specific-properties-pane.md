# FEAT-012: Node-Specific Properties Pane

- Status: Implemented
- Last Updated: 2026-02-15
- Linked Architecture Specs:
  - `specs/architecture/ARCH-001-mvp-foundation.md`
  - `specs/architecture/ARCH-007-nuke-style-docking-workspace.md`
  - `specs/architecture/ARCH-010-node-specific-properties-pane.md`
- Depends On:
  - `specs/features/FEAT-009-nuke-style-docking-and-tabbed-workspace.md`
  - `specs/features/FEAT-010-nuke-inspired-ui-polish-pass.md`

## Problem
The Properties pane is currently a generic `node + parameter + value` form. It does not reflect the selected node directly and does not provide task-specific controls (for example, picking an image file on `ImageInput`).

## Scope
- Bind Properties pane to the selected node on the canvas.
- Replace generic parameter-name/value editing UI with node-specific typed editors.
- Add `ImageInput` file picker workflow inside Properties.

## Requirements
- Selecting a node card updates the Properties pane context to that node.
- If no node is selected, Properties pane shows a clear empty-state hint.
- `ImageInput` properties include a control to pick an image file and load it into the input image slot.
- `Blur` properties expose an editor for blur amount (`Radius`) with integer-safe validation.
- Other node types render editors for all declared parameters using their typed definitions.
- Invalid parameter input must fail safely with a clear status message and no app crash.
- Parameter updates must continue using engine commands so undo/redo and preview refresh still work.
- Existing load/export/undo/redo, graph interactions, and docking behavior remain functional.

## Acceptance Criteria
- Clicking a node card switches Properties pane to that node without needing a separate node dropdown.
- Selecting `ImageInput` and choosing a local image file loads that image and updates preview/status.
- Selecting `Blur` shows and applies `Radius` changes from Properties.
- Selecting nodes with enum/bool/float/int parameters shows the expected control type and updates parameters.
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
- Manual run confirms docking + graph interactions still work after the properties changes.

## Out of Scope
- Multi-node batch parameter editing.
- Property keyframe/animation workflows.
- Persisting recent files or per-node source history.

## Open Questions
- None.

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
- App smoke run (`dotnet run --project src/App/App.csproj -c Debug --no-build`) was started and stopped after a few seconds with no startup exception.
