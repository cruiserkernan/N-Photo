# ARCH-013: Project Document Persistence and Session Restoration

- Status: Implemented
- Last Updated: 2026-02-16
- Scope: `.nphoto` project format, session graph capture/load seams, and app-level save/open lifecycle

## Goals
- Persist full editable project state beyond image export.
- Restore graph, parameter, and key UI session state deterministically.
- Keep persistence boundaries explicit between domain/engine/session/UI concerns.

## Decisions
- Added domain transfer contracts:
  - `GraphDocumentState`
  - `GraphNodeState`
- Added graph capture/load seams on both session and engine boundaries:
  - `IEditorSession.CaptureGraphDocument`
  - `IEditorSession.LoadGraphDocument`
  - `IEditorEngine.CaptureGraphDocument`
  - `IEditorEngine.LoadGraphDocument`
- Engine graph loading performs pre-validation before mutation, then applies replacement atomically within engine lock.
- Engine graph loading resets:
  - undo/redo history
  - tile cache
  - input image store
  - output cache frame
- Added project IO boundary in `Editor.IO`:
  - `IProjectDocumentStore`
  - `JsonProjectDocumentStore`
  - versioned `ProjectDocument` DTO model (`formatVersion = 1`)
- Project serialization is deterministic (sorted node/edge/parameter/ui/asset lists) so canonical signatures are stable.
- Typed parameter serialization uses an explicit discriminated shape by `kind`:
  - `Float`
  - `Integer`
  - `Boolean`
  - `Enum`
  - `Color` (`r`, `g`, `b`, `a`)
- App shell persists:
  - graph + params + edges
  - node positions
  - selected node
  - preview slot assignments + active preview slot
  - image input source bindings
- Docking layout and graph/viewer pan-zoom are intentionally excluded from v1 persistence scope.
- Asset path policy:
  - store relative path when asset is inside project directory tree
  - fallback to absolute path when outside project root
- Dirty state compares canonical serialized signatures instead of mutation counters.
- Unsaved-change prompts are integrated for `New`, `Open`, and window close with `Save`/`Discard`/`Cancel`.

## Boundaries
- `Editor.Domain`: owns graph persistence transfer types.
- `Editor.Engine`: owns graph validation/load application, history/cache reset semantics.
- `Editor.Application`: exposes app-facing capture/load pass-through.
- `Editor.IO`: owns project JSON schema validation and serialization.
- `App` + `App.Presentation`: own file command UX, dirty tracking, and UI state capture/restore.

## Refactoring Guidance
- Keep new persistent UI state fields centralized in project document mapping logic, not spread across toolbar/event handlers.
- Add future persistence fields through versioned schema evolution; do not silently reinterpret v1 payloads.
- Keep engine load semantics pre-validated and mutation-atomic as new graph attributes are introduced.

## Open Questions
- Workspace docking-layout persistence remains deferred.
- Autosave/recovery policy remains deferred.

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
- short app smoke run completed (`dotnet run --project src/App/App.csproj -c Debug --no-build`, terminated after short launch window).
