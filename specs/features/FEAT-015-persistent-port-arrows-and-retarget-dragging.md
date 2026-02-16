# FEAT-015: Persistent Port Arrows and Retarget Dragging

- Status: Implemented
- Last Updated: 2026-02-15
- Linked Architecture Specs:
  - `specs/architecture/ARCH-006-node-graph-viewport-clipping-and-four-side-anchors.md`
  - `specs/architecture/ARCH-008-nuke-inspired-visual-language.md`
- Depends On:
  - `specs/features/FEAT-014-directed-port-topology-and-mask-ports.md`

## Problem
Connected ports currently hide their arrow glyph and appear to terminate into a different wire arrowhead shape. This prevents direct grab-and-retarget behavior from the connected input endpoint and diverges from the requested Nuke-style interaction where the same input arrow exists whether connected or not.

## Scope
- Keep port arrow glyphs visible for both connected and unconnected states.
- Support drag start from connected input/mask arrows to retarget their source output.
- Keep drag start from output arrows and existing snap/preview behavior.
- Adjust output arrow geometry to be a shorter variant in the same visual family as input arrows.
- Preserve existing node drag, pan/zoom, and engine-side connect behavior.

## Requirements
- Input and mask arrows remain visible and draggable when connected.
- Output arrows remain visible and draggable when connected.
- Dragging from a connected input or mask arrow to a compatible output updates that input to the new source.
- Port state differences use styling (fill/stroke/opacity), not visibility toggling.
- Output arrow shape is visually shorter than input arrow shape while keeping the same directional language.
- Existing connection-drag preview wire/snapping continues to work.

## Acceptance Criteria
- In the running app, connected and unconnected input arrows use the same endpoint glyph and remain draggable.
- A user can drag from a connected input arrow and drop on a different output arrow to rewire successfully.
- Output arrows render as shorter glyphs than input arrows while preserving hit area usability.
- Existing output -> input/mask drag connection flow still works.
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
- Short app smoke run confirms graph interaction remains stable.

## Out of Scope
- Full wire rendering redesign beyond this arrow-endpoint behavior.
- New graph validity rules in UI beyond existing engine checks.
- New node type or port role additions.

## Open Questions
- None.

## Validation Plan
- Add/adjust controller tests for port glyph behavior and anchor resolution where practical.
- Run `dotnet build NPhoto.slnx -c Debug`.
- Run `dotnet test NPhoto.slnx -c Debug --no-build`.
- Run a short manual app smoke test (`dotnet run --project src/App/App.csproj`, then exit).

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
- short app smoke run succeeds (`dotnet run --project src/App/App.csproj -c Debug --no-build`, exited after a few seconds).
