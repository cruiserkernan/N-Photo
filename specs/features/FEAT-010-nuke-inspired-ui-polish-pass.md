# FEAT-010: Nuke-Inspired UI Polish Pass

- Status: Implemented
- Last Updated: 2026-02-15
- Linked Architecture Specs:
  - `specs/architecture/ARCH-007-nuke-style-docking-workspace.md`
  - `specs/architecture/ARCH-008-nuke-inspired-visual-language.md`
- Depends On:
  - `specs/features/FEAT-009-nuke-style-docking-and-tabbed-workspace.md`

## Problem
The current workspace is functionally dockable but visually rough: inconsistent spacing, weak hierarchy, hard-coded local colors, and minimal panel chrome reduce clarity and polish compared to Nuke-like expectations.

## Scope
- Apply a cohesive visual refresh across the existing shell and three main panels.
- Improve toolbar hierarchy and control grouping for faster scanning.
- Harmonize node canvas and property controls with the new visual language.
- Split major shell views into separate XAML files/user controls where currently inlined.
- Remove debug-oriented shell UI elements that are not required for core editing flow.
- Keep interactions and feature behavior unchanged unless a minor UX tweak is needed for clarity.

## Requirements
- App shell must use a cohesive dark visual system with consistent spacing, borders, and typography scale.
- Toolbar actions must remain available, but visual grouping should distinguish primary actions from secondary docking actions.
- Dock host and panel surfaces must have clear layered contrast (window background, panel body, panel header/tab areas).
- Node canvas and node cards must align with the app palette and improve legibility of nodes/wires.
- Property panel controls (labels, inputs, buttons, hints) must have clearer hierarchy and balanced spacing.
- `MainWindow.axaml` must be decomposed into dedicated view files for major sections (at minimum graph panel, viewer panel, properties panel, top toolbar area), with equivalent behavior preserved.
- A free, permissively licensed font must be bundled and used for primary UI typography.
- Subtle transitions must be applied for interactive states (hover/selection/press) without introducing distracting motion.
- Existing production-facing commands (load/export/undo/redo/node operations/parameter edits) must remain functional.
- No new third-party dependency may be added without explicit approval.

## Acceptance Criteria
- UI presents a coherent Nuke-inspired look with consistent dark surfaces and readable contrast in all three default panels.
- Toolbar remains fully functional and visually grouped into at least two semantic action clusters.
- Docked workspace layout and drag/drop docking behavior continue to function as before.
- Node graph interactions (pan/zoom/drag/connect) and preview rendering still work.
- Parameter editing flow remains intact and visually clearer.
- The extracted view files are wired into the workspace without regressing docking, commands, or existing event handling.
- Debug-only controls are removed from the visible shell UI while core editing controls remain available.
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.

## Out of Scope
- New panels/tools.
- Workspace persistence redesign.
- Floating window support.
- Major interaction redesign of graph editing workflows.

## Open Questions
- None.

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
