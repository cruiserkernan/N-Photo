# FEAT-013: Single-Cutover Extensibility Refactor (Full Shell + ARCH-001 Engine Foundation)

- Status: Implemented
- Last Updated: 2026-02-15
- Linked Architecture Specs:
  - `specs/architecture/ARCH-011-extensible-node-runtime-and-engine-foundation.md`
  - `specs/architecture/ARCH-012-shell-redesign-and-presentation-architecture.md`
- Depends On:
  - `specs/features/FEAT-012-node-specific-properties-pane.md`

## Problem
Core behaviors were concentrated in very large files and switch-heavy runtime paths, reducing maintainability and extensibility for new nodes and UI workflows.

## Scope
- Add explicit engine/runtime abstraction seams and module-based node evaluation.
- Introduce application/presentation layers and session boundary.
- Redesign shell toolbar into grouped command clusters and searchable node-add flow.
- Add supporting test coverage for new seams.

## Requirements
- Runtime node execution must be class-based and registry-driven.
- Engine orchestration must include explicit compiler/scheduler/cache/backend seams.
- App shell must consume `IEditorSession` instead of `IEditorEngine` directly.
- New solution topology projects must be added and wired:
  - `Editor.Engine.Abstractions`
  - `Editor.Nodes`
  - `Editor.Application`
  - `App.Presentation`
- Toolbar must support search-driven node creation in addition to quick node buttons.
- Existing core workflows (add/connect/edit/render/export/undo/redo) must remain operational.

## Acceptance Criteria
- `BootstrapEditorEngine` no longer uses switch-based node evaluation dispatch.
- Built-in nodes are represented by per-node module classes under `src/Editor.Nodes/Modules/`.
- `MainWindow` depends on `IEditorSession` for editor orchestration.
- New projects are in `NPhoto.slnx` and build with the existing solution.
- New tests exist for node registry/session/presentation controller and engine foundation seams.
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.

## Out of Scope
- Runtime external plugin loading.
- Full completion of all future ARCH-001 tile/persistence features.

## Open Questions
- Additional extraction of canvas interaction logic from `MainWindow` into `App.Presentation.Controllers` remains a follow-up hardening step.

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
