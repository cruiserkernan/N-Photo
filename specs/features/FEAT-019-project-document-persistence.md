# FEAT-019: Project Document Persistence (`.nphoto`) with Manual Save/Open Flow

- Status: Implemented
- Last Updated: 2026-02-16
- Linked Architecture Specs:
  - `specs/architecture/ARCH-000-current-system-architecture.md`
  - `specs/architecture/ARCH-013-project-document-persistence-and-session-restoration.md`
- Depends On:
  - `specs/features/FEAT-018-broad-refactor-mainwindow-presentation-imaging-and-styles.md`
- Status History:
  - `Draft` (2026-02-16)
  - `Approved` (2026-02-16)
  - `Implemented` (2026-02-16)

## Problem
The editor could export rendered images but could not save and reopen full editing sessions. This blocked iterative workflows and made graph editing state ephemeral.

## Scope
- Add project save/open support via `.nphoto` JSON documents.
- Persist graph + key UI state (node positions, selection, preview routing, image source bindings).
- Add shell commands for `New`, `Open`, `Save`, `Save As` with dirty tracking and unsaved-change prompts.
- Add engine/session graph capture/load seams required for safe restoration.

## Requirements
- Project format must be versioned (`formatVersion = 1`) and validated on load.
- Graph persistence must include:
  - node ids/types/parameters
  - edges
  - explicit input/output node ids
- UI persistence must include:
  - node positions
  - selected node id
  - preview slot mappings
  - active preview slot
- Asset persistence must include image input bindings with relative-path preference and absolute fallback.
- Loading must not partially mutate engine state if document validation fails.
- Load must reset undo/redo history and stale runtime caches/state.
- Unsaved-change prompts must guard `New`, `Open`, and window close flows.

## Acceptance Criteria
- Users can save and reopen `.nphoto` projects with graph + persisted UI state restored.
- Toolbar `File` cluster includes `New`, `Open`, `Save`, `Save As`, and existing `Export`.
- Dirty-state marker appears in window title and clears after successful save.
- Unsaved-change prompts correctly handle `Save`/`Discard`/`Cancel`.
- Relative asset paths are stored when assets are under project root, with absolute fallback for external files.
- Missing image files during open do not abort project load.
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
- short app smoke run succeeds (`dotnet run --project src/App/App.csproj -c Debug --no-build`, then exit).

## Implementation Summary
- Added graph persistence transfer types in domain:
  - `src/Editor.Domain/Graph/GraphDocumentState.cs`
  - `src/Editor.Domain/Graph/GraphNodeState.cs`
- Extended engine/session contracts and implementations with capture/load methods:
  - `src/Editor.Engine.Abstractions/IEditorEngine.cs`
  - `src/Editor.Application/IEditorSession.cs`
  - `src/Editor.Application/EditorSession.cs`
  - `src/Editor.Engine/BootstrapEditorEngine.cs`
- Added engine support utilities for load-reset flow:
  - `src/Editor.Engine/EditorCommandProcessor.cs` (`Reset`)
  - `src/Editor.Engine/InputImageStore.cs` (`Clear`)
  - `src/Editor.Domain/Graph/NodeGraph.cs` (`Clear`)
- Added project IO model + JSON store:
  - `src/Editor.IO/IProjectDocumentStore.cs`
  - `src/Editor.IO/ProjectDocument.cs`
  - `src/Editor.IO/ProjectParameterValueCodec.cs`
  - `src/Editor.IO/JsonProjectDocumentStore.cs`
- Added file workflow + dirty tracking + close prompt logic:
  - `src/App/MainWindow.ProjectDocument.cs`
  - `src/App/MainWindow.Composition.cs`
  - `src/App/MainWindow.Lifecycle.cs`
  - `src/App/MainWindow.KeyboardRouting.cs`
  - `src/App/MainWindow.GraphCanvas.State.cs`
  - `src/App/MainWindow.SelectionAndStatus.cs`
  - `src/App/MainWindow.GraphCanvas.NodeCards.cs`
- Added toolbar controls for file commands:
  - `src/App/Views/TopToolbarView.axaml`
  - `src/App/Views/TopToolbarView.axaml.cs`
- Wired default app bootstrap to include project document store:
  - `src/App/App.axaml.cs`
- Extended presentation controllers for persistence integration:
  - `src/App.Presentation/Controllers/NodeActionController.cs` (capture/restore/load image bindings + mutation callback)
  - `src/App.Presentation/Controllers/PreviewRoutingController.cs` (capture/restore state)
  - `src/App.Presentation/Controllers/PropertiesPanelController.cs` (mutation callback hook)

## Test Updates
- `tests/Editor.IO.Tests/ProjectDocumentStoreTests.cs`
- `tests/Editor.Engine.Tests/EngineFoundationTests.cs`
- `tests/Editor.Application.Tests/EditorSessionTests.cs`
- `tests/App.Presentation.Tests/NodeActionControllerTests.cs`
- `tests/App.Presentation.Tests/PropertiesPanelControllerTests.cs`

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
- short app smoke run completed (`dotnet run --project src/App/App.csproj -c Debug --no-build`, terminated after short launch window).

## Out of Scope
- Workspace docking-layout persistence.
- Viewer/graph pan-zoom persistence.
- Autosave/recovery workflows.
- Non-local asset backends.
