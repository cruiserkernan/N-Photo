# FEAT-011: Canvas-Only Node Graph and Nuke-Style Wire Dragging

- Status: Implemented
- Last Updated: 2026-02-15
- Linked Architecture Specs:
  - `specs/architecture/ARCH-005-node-graph-worldspace-navigation.md`
  - `specs/architecture/ARCH-007-nuke-style-docking-workspace.md`
  - `specs/architecture/ARCH-009-node-graph-canvas-purity-and-toolbar-actions.md`
- Depends On:
  - `specs/features/FEAT-009-nuke-style-docking-and-tabbed-workspace.md`
  - `specs/features/FEAT-010-nuke-inspired-ui-polish-pass.md`

## Problem
The node graph panel currently mixes canvas with form controls, which breaks Nuke-like focus. Connection creation is combo-based instead of direct wire dragging, and node cards can appear partially clipped in startup/layout edge cases.

## Scope
- Make the graph panel canvas-focused (nodes + wires only).
- Move Add Node actions into a toolbar group.
- Implement drag-and-drop wire creation between node ports.
- Fix node clipping behavior caused by graph viewport/layout initialization.

## Requirements
- Graph panel must not contain add/connect form controls; it should present graph canvas interactions only.
- Top toolbar must include an icon-style node creation strip usable for all available node types.
- Users must be able to start a connection drag from an output port and drop on a compatible input port to connect.
- During wire drag, UI must render a temporary wire preview from source port to pointer (or hovered target input).
- Invalid drops (empty space, incompatible direction, same-side port) must not create edges and must fail safely.
- Existing node move/pan/zoom interactions must continue to work alongside port drag interactions.
- Default node visibility must avoid partial clipping on startup/resizing in standard layout.
- Existing parameter editing, preview updates, load/export, undo/redo remain functional.
- Legacy combo-based connection controls are removed.

## Acceptance Criteria
- Graph panel shows only canvas-centric graph UI (cards/wires/port handles), without add/connect form rows.
- Add Node action works from toolbar and creates nodes as before.
- A user can connect nodes by dragging from output port handle to input port handle.
- Re-dropping onto an occupied input port replaces the previous incoming edge (engine behavior preserved).
- Node cards are fully visible after startup in default layout (no partial top clipping artifact).
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.

## Out of Scope
- Full node-search popup (`Tab`) command palette.
- Bézier wire rendering redesign.
- Multi-select/group box workflows.

## Open Questions
- None.

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
