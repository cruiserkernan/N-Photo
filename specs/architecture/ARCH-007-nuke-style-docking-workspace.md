# ARCH-007: Nuke-Style Docking Workspace

- Status: Implemented
- Last Updated: 2026-02-15
- Scope: Extensible docked window/tab workspace management in Avalonia app shell

## Goals
- Make the editor shell feel like Foundry Nuke's pane workflow.
- Support resizable split layouts (left/right/top/bottom) and tabbed panes.
- Establish an extensible panel model so new tools can be added without rewriting layout logic.

## Decisions
- The docking implementation uses `Dock.Avalonia` as the primary layout engine because it is actively maintained and aligned with Avalonia 11/net10.
- Third-party workspace dependencies must be free and open-source (FOSS); `Dock.Avalonia` satisfies this with an MIT license.
- Each editor pane is registered through a panel descriptor contract (id, title, view key/view model, default docking hints).
- The workspace layout model is represented through Dock's dock tree (split docks + tab/document/tool docks), with an app-defined adapter layer to keep panel registration and commands explicit.
- The layout engine is UI-shell only and does not depend on `Editor.Domain` or `Editor.Engine`.
- Resizing is performed through dock splitters.
- Docking operations are supported through both:
  - drag-and-drop docking targets
  - explicit commands (move tab within stack, move to stack, split target left/right/top/bottom)
- Default startup layout should mirror Nuke-style workflow:
  - node graph panel and properties panel split beside a large viewer area
  - related tools can be stacked into tabs inside each region
- Workspace layout persistence across app restarts is out of scope for this feature.
- Floating/detached windows are out of scope for this feature and deferred.

## Boundaries
- `App` owns docking state, layout commands, persistence, and rendering.
- `Editor.Domain`, `Editor.Engine`, `Editor.Imaging`, and `Editor.IO` remain workspace-agnostic.

## Refactoring Guidance
- Extract layout composition and panel registration from `MainWindow` code-behind into dedicated workspace classes before adding new docking behaviors.
- Keep panel content controls decoupled from docking container internals.

## Open Questions
- None.

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
