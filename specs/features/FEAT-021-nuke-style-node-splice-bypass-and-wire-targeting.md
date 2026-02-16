# FEAT-021: Nuke-Style Node Splice, Bypass, and Wire Targeting

- Status: Implemented
- Last Updated: 2026-02-16
- Linked Architecture Specs:
  - `specs/architecture/ARCH-000-current-system-architecture.md`
  - `specs/architecture/ARCH-006-node-graph-viewport-clipping-and-four-side-anchors.md`
- Depends On:
  - `specs/features/FEAT-017-center-clipped-border-anchors-and-incoming-arrowheads.md`

## Problem
Current node-graph interaction still has gaps versus expected Nuke-style behavior:
- node-on-wire insertion is not automatic when dropping a node on an edge
- retarget drag dropped to nowhere does not commit disconnection
- elbow endpoint placement can drift in some sizing/zoom states
- wire/stub hit areas can override intended node-drag behavior at low zoom
- selected-node delete does not bypass-reconnect primary flow
- node drop-zone targeting for edge drags is too narrow
- Ctrl+move bypass behavior is missing.

## Scope
- Implement the seven requested interaction fixes:
  - drop node onto edge to splice using first input + first output
  - edge retarget drop-to-nowhere disconnect
  - elbow placement correctness fix
  - node-interior drag precedence
  - Delete-key selected-node removal with bypass reconnect
  - full node-body drop zone (+outside padding) for edge drops
  - Ctrl+drag node bypass-at-drag-start.
- Implement additional Nuke-like interactions:
  - wire hover highlight during node-over-wire insertion candidate
  - double-click wire to insert elbow and splice
  - Esc cancels active connection drag.
- Extend engine/session API with explicit remove and bypass node operations.
- Add regression tests for engine command behavior and geometry helpers.

## Requirements
- Delete and bypass use primary-stream policy:
  - primary input is first input port in node definition
  - primary output is first output port in node definition
  - reconnect upstream edge on primary input to all downstream edges on primary output
  - non-primary incident edges are disconnected
  - duplicate bypass edges are skipped
  - cycle checks remain enforced by DAG validator.
- `ImageInput` and `Output` nodes are protected from delete/bypass operations.
- Node-body pointer precedence:
  - pointer inside node body always drags node
  - outside-node areas remain wire/stub grab zones.
- Connection drop targeting:
  - stage 1: precise compatible port snap
  - stage 2: node-body fallback (full body + outside padding) resolving default compatible ports
  - precise snap always wins when available.
- Retarget drag dropped in invalid space disconnects detached edge.
- Esc cancels active connection drag with no graph mutation.
- Dropping node on intersected edge splices node into that edge with rollback on failure.
- Double-clicking a hit wire inserts elbow at pointer and splices, with rollback on failure.
- Elbow anchor placement uses stable card size fallback before measure completes.

## Acceptance Criteria
- Dragging a node onto a wire and dropping inserts it between source and destination using node first input/output.
- Dragging a detached connection and releasing where no valid target exists disconnects the original edge.
- Elbow node connected wires remain correctly attached during add/move/zoom states.
- At low zoom, clicking inside a node always starts node drag; wire/stub grabs occur only outside node body.
- Pressing `Delete` removes selected non-protected node and reconnects primary stream.
- Wire drop onto node works anywhere in node body plus padded outside zone.
- Holding Ctrl when starting node drag bypasses node primary stream and continues moving node.
- While dragging a node over a candidate wire, the wire receives visual hover/highlight.
- Double-clicking a wire inserts an elbow node and splices the wire.
- Pressing `Esc` during active connection drag cancels it without changing graph edges.
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
- Manual app smoke run confirms interaction stability.

## Out of Scope
- Bezier/spline wire redesign.
- Multi-select/group operations.
- New node types beyond elbow insertion usage.

## Implementation Notes
- Scope:
  - graph interaction behavior in `App` node-canvas partials
  - command/API extension in `Editor.Engine` and `Editor.Application`
  - focused presentation geometry helper expansion.
- Files/Modules to change:
  - `src/Editor.Engine.Abstractions/IEditorEngine.cs`
  - `src/Editor.Application/IEditorSession.cs`
  - `src/Editor.Application/EditorSession.cs`
  - `src/Editor.Engine/BootstrapEditorEngine.cs`
  - `src/Editor.Engine/Commands/RemoveNodeCommand.cs` (new)
  - `src/Editor.Engine/Commands/BypassNodePrimaryStreamCommand.cs` (new)
  - `src/Editor.Engine/Commands/NodeBypassPlanner.cs` (new)
  - `src/App/MainWindow.KeyboardRouting.cs`
  - `src/App/MainWindow.GraphCanvas.Input.cs`
  - `src/App/MainWindow.GraphCanvas.ConnectionDrag.cs`
  - `src/App/MainWindow.GraphCanvas.NodeCards.cs`
  - `src/App/MainWindow.GraphCanvas.State.cs`
  - `src/App/MainWindow.ToolbarCommands.cs`
  - `src/App.Presentation/Controllers/GraphWireGeometryController.cs`
- Tests to add/update:
  - `tests/Editor.Engine.Tests/EditorEngineTests.cs`
  - `tests/Editor.Application.Tests/EditorSessionTests.cs`
  - `tests/App.Presentation.Tests/GraphWireGeometryControllerTests.cs`
- Risks:
  - undo/redo drift for composite rewire commands
  - drag-input conflicts (double-click vs drag start)
  - ambiguous port expectations on multi-port nodes.
- Mitigations:
  - command-level execute/undo/redo tests
  - explicit click-count branching before drag start
  - primary-stream policy fixed and documented here.

## Open Questions
- None.

## Validation Plan
- Run `dotnet build NPhoto.slnx -c Debug`.
- Run `dotnet test NPhoto.slnx -c Debug --no-build`.
- Run short app smoke launch (`dotnet run --project src/App/App.csproj -c Debug --no-build`, then exit).

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
- short smoke launch executed (`dotnet run --project src/App/App.csproj -c Debug --no-build`), app started and was terminated intentionally after a few seconds (`SmokeExitCode=-1`).
