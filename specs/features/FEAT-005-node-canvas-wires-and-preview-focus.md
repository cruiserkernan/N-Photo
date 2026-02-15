# FEAT-005: Node Canvas Wires and Preview Focus

- Status: Implemented
- Last Updated: 2026-02-15
- Linked Architecture Specs:
  - `specs/architecture/ARCH-003-node-canvas-interaction-model.md`
  - `specs/architecture/ARCH-004-node-canvas-wire-and-preview-focus.md`
- Depends On:
  - `specs/features/FEAT-001-mvp-node-set.md`
  - `specs/features/FEAT-004-node-canvas-drag-drop.md`

## Problem
The node canvas currently renders only draggable cards, so graph flow is hard to read. Preview also always evaluates from the output node, making it cumbersome to inspect intermediate results at a selected node.

## Scope
- Render directional connection wires with arrowheads between connected node cards.
- Preserve ability to add nodes without auto-connecting them.
- Add node preview-focus selection so viewport can show output at a selected node.

## Requirements
- Node canvas must display one wire for each graph edge using current card positions.
- Each wire must show clear source-to-target direction with an arrow indicator.
- Wire visuals must update during node drag and after connect/undo/redo operations.
- Add Node behavior must remain unconnected by default (except existing fixed bootstrap input->output edge).
- Users must be able to select a node as the active preview focus.
- Preview rendering must evaluate from selected focus node when set.
- If no focus node is selected, preview must evaluate from output node (current behavior).
- Existing connect/parameter/export flows must continue to work.

## Acceptance Criteria
- After creating and connecting nodes, directional wires are visible between connected cards in the canvas.
- Dragging any connected card updates attached wire endpoints in real time.
- Adding a node from Add Node control creates it unconnected to other processing nodes.
- Selecting an intermediate node updates viewport preview to show that stage of processing.
- Clearing focus (or selecting output) restores final output preview behavior.
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.

## Out of Scope
- Port-to-port drag wiring interaction.
- Multi-select and group operations.
- Persisting preview-focus selection to disk.
- Full visual parity with Nuke themes beyond wire readability and directionality.

## Open Questions
- None.

## Decisions
- Wire style: elbow/orthogonal with directional arrowheads.
- Preview slots: Nuke-like hotkeys `1-5` (selected node assigns+previews slot; no selected node recalls slot).
- Reset hotkey: `0` returns preview to output node.
- Disconnected focus preview displays blank/black frame.

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
