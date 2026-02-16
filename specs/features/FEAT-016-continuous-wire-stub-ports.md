# FEAT-016: Continuous Wire-Stub Ports

- Status: Implemented
- Last Updated: 2026-02-15
- Linked Architecture Specs:
  - `specs/architecture/ARCH-006-node-graph-viewport-clipping-and-four-side-anchors.md`
  - `specs/architecture/ARCH-008-nuke-inspired-visual-language.md`
- Depends On:
  - `specs/features/FEAT-015-persistent-port-arrows-and-retarget-dragging.md`

## Problem
Ports and connected edges still read as different visual types. The requested interaction is that an unconnected port is simply a short wire that can be grabbed, and a connected port is that same wire extended to another node.

## Scope
- Render ports as wire stubs that share the same visual language as connection wires.
- Keep connected and unconnected ports in one continuous endpoint model.
- Preserve drag-to-connect and retarget behavior from both output and input/mask sides.
- Keep output stub shorter than input stub while preserving usability.

## Requirements
- Unconnected ports render as short stubs at anchor positions.
- Connected edges visually continue from those same stubs without switching endpoint type.
- No separate wire arrowhead type is introduced for connected edges.
- Port interaction hit zones remain easy to target and draggable.
- Existing pan/zoom/node-drag and connect command behavior remains unchanged.
- Drag preview wire remains dashed.

## Acceptance Criteria
- In app, port endpoints and full connections read as one wire family, not two separate visual types.
- Dragging from an unconnected stub and from a connected stub both work.
- Rewiring from connected input/mask to another output still works.
- Output stubs are visibly shorter than input stubs.
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
- Short app smoke run confirms graph interaction remains stable.

## Out of Scope
- New node types, new port roles, or engine graph rules.
- Full wire routing redesign.

## Open Questions
- None.

## Validation Plan
- Add or update tests for geometry/state helpers where practical.
- Run `dotnet build NPhoto.slnx -c Debug`.
- Run `dotnet test NPhoto.slnx -c Debug --no-build`.
- Run short smoke app launch (`dotnet run --project src/App/App.csproj`, then exit).

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
- short app smoke run succeeds (`dotnet run --project src/App/App.csproj -c Debug --no-build`, then exit).
