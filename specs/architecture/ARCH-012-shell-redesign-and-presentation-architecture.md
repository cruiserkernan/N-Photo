# ARCH-012: Shell Redesign and Presentation Architecture

- Status: Implemented
- Last Updated: 2026-02-16
- Scope: Presentation-layer structuring and shell toolbar redesign with searchable node creation

## Goals
- Reduce direct engine coupling in app shell code.
- Introduce presentation-layer state/controller types for future decomposition of UI logic.
- Redesign toolbar structure into semantic command clusters with searchable node add workflow.

## Decisions
- Added `Editor.Application` with `IEditorSession` and `EditorSession` as the app-facing orchestration boundary.
- Added `App.Presentation` project with view models and controller primitives:
  - `ShellViewModel`, `GraphViewModel`, `PropertiesViewModel`, `ViewerViewModel`
  - `WorkspaceController`, `GraphInteractionController`, `PreviewRoutingController`
- `MainWindow` now depends on `IEditorSession` instead of `IEditorEngine`.
- Preview-slot logic migrated to `PreviewRoutingController`.
- Graph bend routing now uses `GraphInteractionController` helper.
- Graph wire geometry utilities now live in `GraphWireGeometryController`.
- Toolbar redesign shipped in `TopToolbarView`:
  - grouped command clusters (`File`, `Edit`, `Node`)
  - node search box + add action
  - retained quick node strip
  - status surface preserved
- `MainWindow` shell code-behind is further decomposed into focused partials:
  - composition
  - lifecycle
  - toolbar commands
  - keyboard routing
  - selection/status
- Graph canvas code-behind is further decomposed into focused partials:
  - state/rendering
  - node card + port visuals
  - input handling
  - connection drag behavior
  - viewport utilities
- Parameter editor primitives are modularized into per-primitive files under `Controllers/ParameterEditors`.

## Boundaries
- App shell interacts with editor runtime through `IEditorSession`.
- Presentation-specific state/behavior lives in `App.Presentation`.
- Low-level graph/runtime behavior remains outside app views.

## Refactoring Guidance
- Continue extracting pointer/graph interaction logic from `MainWindow` into `App.Presentation.Controllers`.
- Keep controls binding-capable and move non-trivial UI state into view models over follow-up slices.

## Open Questions
- Full controller ownership of canvas interaction remains partially complete; additional extraction is planned.

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
